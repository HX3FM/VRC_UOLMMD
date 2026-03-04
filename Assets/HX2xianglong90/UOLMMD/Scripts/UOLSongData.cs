
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace HX2xianglong90.UOLMMD
{
[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class UOLSongData : UdonSharpBehaviour
{
    public String[] songTitle; // stores song/dance titles
    public String[] songInfo; // stores song information (author, motion creator, etc)
    public String[] songData; // stores song data (pad ammount; danceAnimatorSettings; CameraAnimatorSettings; tags, etc)
    public VRCUrl[] songLink; // stores song links
    public VRCStation[] stations; // stations for each song
    private string[] stationNames; // names of stations for each song
    public Animator cameraAnimator; // stores camera animator
    void Start()
    {
        // validate data lengths
        if (songTitle.Length != songInfo.Length || songTitle.Length != songData.Length || songTitle.Length != songLink.Length)
        {
            Debug.LogError("[UOLSongData] Song data arrays have mismatched lengths!");
        }
        // dump station names from stations
        stationNames = new string[stations.Length];
        for(int i = 0; i < stations.Length; i++){
            stationNames[i] = stations[i].name;
        }
    }

    public int GetSongAmount()
    {
        return songTitle.Length;
    }

    public int FetchSongIndexByTitle(string title)
    {
        return Array.IndexOf(songTitle, title); // get index of the song by title, returns -1 if not found
    }
    public string GetSongTitle(int index)
    {
        if (index >= 0 && index < songTitle.Length)
        {
            return songTitle[index];
        }
        return "";
    }
    public string GetSongInfo(int index){
        if (index >= 0 && index < songInfo.Length)
        {
            return songInfo[index];
        }
        return "";
    
    }
    public string GetSongData(int index, int mode = 0){ // mode 0: full data, mode 1: pad ammount

        switch(mode){
            case 1: //get pad ammount
                if (index >= 0 && index < songData.Length)
                {
                    return songData[index].Split(';')[0];
                }
                break;
            default:
                if (index >= 0 && index < songData.Length)
                {
                    return songData[index];
                }
                break;
        }
        return "";
    }
    public VRCUrl GetSongLink(int index){
        if (index >= 0 && index < songLink.Length)
        {
            return songLink[index];
        }
        return VRCUrl.Empty;
    }
    public VRCStation GetStation(int songIndex, int songPadIndex){
        if (songIndex >= 0 && songIndex < stations.Length && songPadIndex >= 0 && songPadIndex < stationNames.Length)
        {
            // retrn null if the station name doesn't match the expected format to prevent errors when accessing the stations array
            // check name of station
            int stationIndex = Array.IndexOf(stationNames, songIndex.ToString() + "-" + songPadIndex.ToString());
            if (stationIndex == -1) return null;
            return stations[stationIndex];
        }
        return null;
    }
}
}