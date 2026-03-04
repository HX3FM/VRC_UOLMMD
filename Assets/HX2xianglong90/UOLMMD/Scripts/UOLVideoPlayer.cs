
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Video.Components;
using VRC.SDK3.Components.Video;

namespace HX2xianglong90.UOLMMD
{
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class UOLVideoPlayer : UdonSharpBehaviour
{
    [SerializeField] private UOLCore uolCore;
    private VRCUnityVideoPlayer videoPlayer;
    private bool hasError = false;
    void Start()
    {
        videoPlayer = this.GetComponent<VRCUnityVideoPlayer>();
    }
    override public void OnVideoEnd()
    {
        Debug.Log("[UOLVideoPlayer] Video ended.");
        uolCore.Log("[UOLVideoPlayer] Video ended.");
        uolCore.OnResetButtonPressed(); // video end => reset
        videoPlayer.SetTime(0); // ensure video starts from beginning
    }
    override public void OnVideoError(VRC.SDK3.Components.Video.VideoError error)
    {
        Debug.Log("[UOLVideoPlayer] Video error: " + error);
        uolCore.Log("[UOLVideoPlayer] Video error: " + error);
        hasError = true;
        uolCore.UpdateStateUI();
        videoPlayer.SetTime(0); // ensure video starts from beginning
        hasError = false; // reset error state after handling
    }
    override public void OnVideoPlay()
    {
        Debug.Log("[UOLVideoPlayer] Video started.");
        uolCore.Log("[UOLVideoPlayer] Video started. Notifying UOLCore.");
    }
    override public void OnVideoReady()
    {
        hasError = false; // reset error state when video is ready

        Debug.Log("[UOLVideoPlayer] Video is ready.");
        uolCore.Log("[UOLVideoPlayer] Video is ready. Notifying UOLCore.");
        uolCore.OnVideoStarted(); // notify core that video is ready
        
        // notify core that this player's video has loaded
        uolCore.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(OnPlayerVideoReady));
        
        //set time to 0 to prevent video from starting at a random time due to sync issues
        videoPlayer.SetTime(0); // ensure video starts from beginning
    }
    public void OnPlayerVideoReady(){
        uolCore.OnPlayerVideoReady();
    }
    public bool IsError(){
        return hasError;
    }


}
}
