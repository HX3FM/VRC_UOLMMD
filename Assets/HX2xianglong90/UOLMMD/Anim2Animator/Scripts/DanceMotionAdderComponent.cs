using UnityEngine;
using System.IO;
using System.Linq;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;
using HX2xianglong90.UOLMMD;

namespace HX2xianglong90.UOLMMDTools
{
[ExecuteInEditMode]
public class DanceMotionAdderComponent : MonoBehaviour
{
    public string inputPath = "Assets/HX2xianglong90/UOLMMD/Anim2Animator/Input";
    public string outputPath = "Assets/HX2xianglong90/UOLMMD/Anim2Animator/Output";

    //UOLSongData
    public UOLSongData songData;

    //position variable
    public Transform enterSpot;
    public Transform exitSpot;
    public Transform stationsObject;

    [ContextMenu("Generate Dance Animators")]
    public void GenerateDanceAnimators()
    {
#if UNITY_EDITOR
        //Execute the generation logic in the editor
        string inputFullPath = Path.Combine(Application.dataPath, inputPath.Substring(7));
        if (!Directory.Exists(inputFullPath))
        {
            UnityEditor.EditorUtility.DisplayDialog("错误", $"输入目录不存在: {inputPath}", "确定");
            return;
        }

        //clear stationsObject children
        if(stationsObject != null){
            for(int i = stationsObject.childCount - 1; i >= 0; i--){
                var child = stationsObject.GetChild(i);
                if(Application.isEditor){
                    UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
                } else {
                    DestroyImmediate(child.gameObject);
                }
            }
        }


        // generate stations
        string outputFullPath = Path.Combine(Application.dataPath, outputPath.Substring(7));
        if (!Directory.Exists(outputFullPath)) Directory.CreateDirectory(outputFullPath);
        // clear output directory
        DirectoryInfo outputDir = new DirectoryInfo(outputFullPath);
        foreach (var file in outputDir.GetFiles()) file.Delete();
        foreach (var dir in outputDir.GetDirectories()) dir.Delete(true);

        var inputDir = new DirectoryInfo(inputFullPath);
        var subFolders = inputDir.GetDirectories().OrderBy(d => d.Name).ToArray();

        int folderIndex = 0;
        songData.songTitle = new string[subFolders.Length];
        songData.songInfo = new string[subFolders.Length];
        songData.songLink = new VRCUrl[subFolders.Length];
        foreach (var folder in subFolders)
        {
            var animFiles = folder.GetFiles("*.anim").OrderBy(f => f.Name).ToArray();
            int fileIndex = 0;
            foreach (var animFile in animFiles)
            {
                string outputName = $"{folderIndex}-{fileIndex}";
                CreateAnimatorForAnimation(animFile.FullName, outputName);
                fileIndex++;
            }
            // Add songTiltle according to each folder name
            if(songData != null){
                songData.songTitle[folderIndex] = folder.Name;
            }
            // Add songInfo according to info.txt in each folder\
            string infoFilePath = Path.Combine(folder.FullName, "info.txt");
            if(File.Exists(infoFilePath)){
                string infoContent = File.ReadAllText(infoFilePath);
                if(songData != null){
                    songData.songInfo[folderIndex] = infoContent;
                }
            }
            
            //Add songUrl according to link.txt in each folder
            string linkFilePath = Path.Combine(folder.FullName, "link.txt");
            if(File.Exists(linkFilePath)){
                string linkContent = File.ReadAllText(linkFilePath);
                if(songData != null){
                    songData.songLink[folderIndex] = new VRCUrl(linkContent);
                }
            }
            folderIndex++;
        }

        // Add stations to songData
        if(songData != null){
            var stationList = stationsObject.GetComponentsInChildren<VRC.SDK3.Components.VRCStation>(true).ToList();
            songData.stations = stationList.ToArray();
        }

        UnityEditor.AssetDatabase.Refresh();
        UnityEditor.EditorUtility.DisplayDialog("成功", "舞蹈动画器生成完成！", "确定");
#else
        Debug.LogWarning("GenerateDanceAnimators 只能在 Unity 编辑器中运行。");
#endif
    }

#if UNITY_EDITOR
    private void CreateAnimatorForAnimation(string animFilePath, string outputName)
    {
        string projectAssetsPath = Path.GetFullPath(Application.dataPath).Replace("\\", "/");
        string fullAnimPath = Path.GetFullPath(animFilePath).Replace("\\", "/");
        if (!fullAnimPath.StartsWith(projectAssetsPath))
        {
            Debug.LogError($"动画文件不在项目 Assets 目录中: {fullAnimPath}");
            return;
        }
        string relativeAnimPath = "Assets" + fullAnimPath.Substring(projectAssetsPath.Length);
        relativeAnimPath = relativeAnimPath.Replace("\\", "/");

        var animClip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(relativeAnimPath);
        if (animClip == null)
        {
            Debug.LogError($"无法加载动画文件: {relativeAnimPath}");
            return;
        }

        string controllerPath = Path.Combine(outputPath, $"{outputName}.controller").Replace("\\", "/");
        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        var layer = controller.layers[0];
        var stateMachine = layer.stateMachine;

        var defaultState = stateMachine.AddState("Default");
        stateMachine.defaultState = defaultState;

        AddVRCAnimatorTrackingControl(defaultState);

        var danceState = stateMachine.AddState("Dance");
        danceState.motion = animClip;
        danceState.writeDefaultValues = true;

        var transition = defaultState.AddTransition(danceState);
        transition.hasExitTime = true;
        transition.exitTime = 0f; 
        transition.duration = 0;

        UnityEditor.EditorUtility.SetDirty(controller);
        Debug.Log($"已为 {animClip.name} 创建动画器: {outputName}");

        // 在场景中为该 animator 创建一个 station 对象并添加 VRCStation
        try
        {
            GameObject go = new GameObject(outputName);
            if (stationsObject != null)
            {
                go.transform.SetParent(stationsObject, false);
            }
            // 注册撤销操作
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Station");

            // 使用 VRC.SDK3.Components.VRCStation 添加组件并设置字段
            try
            {
                var stationComp = go.AddComponent<VRC.SDK3.Components.VRCStation>();
                if (stationComp != null)
                {
                    stationComp.seated = false;
                    stationComp.disableStationExit = true;
                    stationComp.canUseStationFromStation = false;
                    if (enterSpot != null) stationComp.stationEnterPlayerLocation = enterSpot;
                    if (exitSpot != null) stationComp.stationExitPlayerLocation = exitSpot;
                    stationComp.animatorController = controller;
                    Debug.Log($"已为 {outputName} 创建 VRCStation 组件并设置动画器");

                    // Add Animator component and set applyRootMotion to false
                    var animator = go.AddComponent<Animator>();
                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;
                }
            }
            catch (System.Exception)
            {
                Debug.LogWarning("未能添加 VRCStation 组件，请确认已安装并启用 VRC SDK 3（VRCStation 类型）。");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"创建 VRCStation 时出错: {e.Message}");
        }
    }

    private void AddVRCAnimatorTrackingControl(UnityEditor.Animations.AnimatorState state)
    {
        try
        {
            var behavior = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorTrackingControl>();
            if (behavior != null)
            {
                // 如果需要，可在这里设置 behavior 的字段来勾选 All
                behavior.trackingEyes = VRC.SDK3.Avatars.Components.VRCAnimatorTrackingControl.TrackingType.Animation;
                behavior.trackingHead = VRC.SDK3.Avatars.Components.VRCAnimatorTrackingControl.TrackingType.Animation;
                behavior.trackingLeftHand = VRC.SDK3.Avatars.Components.VRCAnimatorTrackingControl.TrackingType.Animation;
                behavior.trackingRightHand = VRC.SDK3.Avatars.Components.VRCAnimatorTrackingControl.TrackingType.Animation;
                behavior.trackingHip = VRC.SDK3.Avatars.Components.VRCAnimatorTrackingControl.TrackingType.Animation;
                behavior.trackingLeftFoot = VRC.SDK3.Avatars.Components.VRCAnimatorTrackingControl.TrackingType.Animation;
                behavior.trackingRightFoot = VRC.SDK3.Avatars.Components.VRCAnimatorTrackingControl.TrackingType.Animation;
                behavior.trackingLeftFingers = VRC.SDK3.Avatars.Components.VRCAnimatorTrackingControl.TrackingType.Animation;
                behavior.trackingRightFingers = VRC.SDK3.Avatars.Components.VRCAnimatorTrackingControl.TrackingType.Animation;
                behavior.trackingMouth = VRC.SDK3.Avatars.Components.VRCAnimatorTrackingControl.TrackingType.Animation;
                Debug.Log("已添加 VRC Animator Tracking Control");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"添加 VRC 行为时出错: {e.Message}");
        }
    }
#endif
}
}