using UnityEditor;
using UnityEngine;

namespace HX2xianglong90.UOLMMDTools
{
[CustomEditor(typeof(DanceMotionAdderComponent))]
[CanEditMultipleObjects]
public class DanceMotionAdderComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 绘制默认 Inspector
        DrawDefaultInspector();

        EditorGUILayout.Space();

        // 添加文件夹选择按钮
        if (GUILayout.Button("Select Input Folder"))
        {
            string path = EditorUtility.OpenFolderPanel("Select Input Folder", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
                serializedObject.FindProperty("inputPath").stringValue = path;
                serializedObject.ApplyModifiedProperties();
            }
        }

        if (GUILayout.Button("Select Output Folder"))
        {
            string path = EditorUtility.OpenFolderPanel("Select Output Folder", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
                serializedObject.FindProperty("outputPath").stringValue = path;
                serializedObject.ApplyModifiedProperties();
            }
        }

        EditorGUILayout.Space();

        // 添加执行按钮
        if (GUILayout.Button("Generate Dance Animators"))
        {
            // 对选中的每个组件执行生成
            foreach (var t in targets)
            {
                var comp = t as DanceMotionAdderComponent;
                if (comp == null) continue;
                comp.GenerateDanceAnimators();
            }
        }
    }
}
}