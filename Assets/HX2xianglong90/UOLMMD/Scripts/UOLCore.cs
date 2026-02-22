
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDK3.Video.Components;

namespace HX2xianglong90.UOLMMD
{
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class UOLCore : UdonSharpBehaviour
{
    // flags (NETWORKED)
    [UdonSynced,SerializeField,HideInInspector]private bool isStarted = false; // whether the start dance has pressed
    [UdonSynced,SerializeField,HideInInspector]private bool isVideoReady = false; // whether the video is ready to play
    [UdonSynced,SerializeField,HideInInspector]private bool isPlaying = false; // whether the video/dance is currently playing
    //data
    [UdonSynced,SerializeField] private int currentSongIndex = -1; // NETWORKED, -1 means no song selected
    // playback sync (NETWORKED)
    [UdonSynced,SerializeField,HideInInspector] private double playbackStartTime = 0.0; // server time in seconds when playback started
    [UdonSynced,SerializeField,HideInInspector] private bool playbackIsPlaying = false; // whether playback has been started by owner

    // data
    [SerializeField] private UOLSongData uolSongData;
    private string[] padsPlayerData; //strores which player enters the pads
    private bool[] playersVideoReady; // tracks which players have loaded their video

    // ui
    private GameObject[] stateInfos;
    private TMP_Text songTitleText;
    private TMP_Text songInfoText;
    private TMP_Text songLinkText;

    private TMP_Text playerInfoText;

    private TMP_Text LogText;

    // pads
    private GameObject padEnterParent; // parent object of pads
    private GameObject[] pads; // pads in the scene

    //song menu
    [SerializeField] private SimplePageMenu.SimpleButtonPages songMenu;

    //stations
    
    [SerializeField]private VRCStation stationResetTracking; // a station with empty animation to reset player position when entering

    //video player
    private VRCUnityVideoPlayer videoPlayer;
    [SerializeField] private UOLVideoPlayer uolVideoPlayer;

    // NPC
    [SerializeField] private UOLNPCManager npcManager;
    private GameObject[] npcObjects; // strores the instantiated NPC objects for each pad, will be null if no NPC is summoned for that pad
    private Transform SpawnedNPCParent; // parent object for spawned NPCs to keep the hierarchy organized
    private VRCUrl lastLoadedUrl; // cache the last loaded URL to avoid repeated loads

    // public float coldingDownCounter = 0.0f; // a counter to prevent multible video start calls, will reset after 5 seconds of the first call

    void Start()
    {
        //initialize UI elements
        songTitleText = this.transform.Find("MainPanel/SongTitle").GetComponent<TMP_Text>();
        songInfoText = this.transform.Find("MainPanel/SongInfo").GetComponent<TMP_Text>();
        songLinkText = this.transform.Find("MainPanel/SongLink").GetComponent<TMP_Text>(); 
        playerInfoText = this.transform.Find("MainPanel/PlayerInfo").GetComponent<TMP_Text>();
        LogText = this.transform.Find("LogPanel/LogTextMask/LogText").GetComponent<TMP_Text>();
        //initialize state buttons
        stateInfos = new GameObject[5];
        stateInfos[0] = this.transform.Find("MainPanel/StartButton").gameObject;
        stateInfos[1] = this.transform.Find("MainPanel/Loading").gameObject;
        stateInfos[2] = this.transform.Find("MainPanel/Playing").gameObject;
        // gameobject for video error state
        stateInfos[3] = this.transform.Find("MainPanel/VideoError").gameObject;

        // colding down
        // stateInfos[4] = this.transform.Find("MainPanel/ColdingDown").gameObject;

        //initialize pads
        padEnterParent = this.transform.Find("EnterPads").gameObject;
        pads = new GameObject[padEnterParent.transform.childCount];
        for (int i = 0; i < pads.Length; i++)
        {
            pads[i] = padEnterParent.transform.GetChild(i).gameObject;
        }

        //initialize video player
        videoPlayer = uolVideoPlayer.gameObject.GetComponent<VRCUnityVideoPlayer>();

        //initialize pads data
        padsPlayerData = new string[pads.Length];
        ClearPadsPlayerData();
        UpdatePlayerInfoText(); // update once to refresh player info text
        
        //initialize players video ready tracking
        playersVideoReady = new bool[pads.Length];
        ClearPlayersVideoReady();
        
        //initialize npc objects array
        npcObjects = new GameObject[pads.Length];

        //initialize spawned NPC parent
        SpawnedNPCParent = this.transform.Find("SpawnedNPCs");
        
        // dump data to menu
        songMenu.SetItem(uolSongData.songTitle, uolSongData.songInfo); // no need to show values
    }

    override public void OnDeserialization()
    {

        //close pad enter trigger when start, open when reset or end
        UpdatePadEnterParent();

        //update state UI
        UpdateStateUI();
        //open pads according to song data
        UpdatePads(currentSongIndex);
        //update UI when data is deserialized
        UpdateSongInfoUI();

        //update player info text
        UpdatePlayerInfoText();

        if(isStarted){
            npcManager.SetSelectNPCButtonsActive(false); // disable NPC select buttons when started to prevent changes during playback
        }else{
            npcManager.SetSelectNPCButtonsActive(true); // enable NPC select buttons when not started
        }

        // If started: non-pad local players should load the URL but not play yet
        if (isStarted && !IsLocalInPad())
        {
            VRCUrl url = uolSongData.GetSongLink(currentSongIndex);
            if (!VRCUrl.IsNullOrEmpty(url) && url != lastLoadedUrl)
            {
                videoPlayer.LoadURL(url); // load but do not play, will be started when playbackIsPlaying becomes true
                lastLoadedUrl = url;
                Log("[UOLCore] Non-pad player loaded URL: " + url);
            }
        }

        // If owner already started playback, sync to current playback time and play
        if (playbackIsPlaying)
        {
            double now = Networking.GetServerTimeInMilliseconds() / 1000.0;
            float elapsed = (float)(now - playbackStartTime);
            if (elapsed < 0f) elapsed = 0f;
            videoPlayer.SetTime(elapsed);
            videoPlayer.Play();
            Log("[UOLCore] Synced playback on deserialization, elapsed: " + elapsed);
        }
    }

    // ------ Flag Related ------
    private void UpdatePadEnterParent()
    {
        //close pad enter trigger when start, open when reset or end
        if (isStarted)
        {
            padEnterParent.SetActive(false);
        }else
        {
            padEnterParent.SetActive(true);
        }
    }

    
    
    // -------- Pad Related ------
    private void ClearPadsPlayerData()
    {
        for(int i = 0; i < padsPlayerData.Length; i++)
        {
            padsPlayerData[i] = "";
        }
    }
    
    private void ClearPlayersVideoReady()
    {
        for(int i = 0; i < playersVideoReady.Length; i++)
        {
            playersVideoReady[i] = false;
        }
    }
    
    private int GetActivePadCount()
    {
        int count = 0;
        for(int i = 0; i < padsPlayerData.Length; i++)
        {
            if(padsPlayerData[i] != "")
            {
                count++;
            }
        }
        return count;
    }
    
    private bool AreAllPlayersVideoReady()
    {
        for(int i = 0; i < padsPlayerData.Length; i++)
        {
            if(padsPlayerData[i] != "" && !playersVideoReady[i])
            {
                return false;
            }
        }
        return true;
    }
    private void CloseAllPads()
    {
        foreach (GameObject pad in pads)
        {
            pad.SetActive(false);
        }
    }
    private void UpdatePads(int songIndex) //trun on pads according to song data
    {
        if (songIndex == -1) { // no song selected
            CloseAllPads();
            return; 
        }
        else{                
            //get pad ammount from song data
            int padAmmount = int.Parse(uolSongData.GetSongData(songIndex, mode: 1));

            //close all pads first
            CloseAllPads();

            //open pads
            for (int i = 0; i < padAmmount && i < pads.Length; i++){
                pads[i].SetActive(true);
            }
        }
    }

    private void UpdatePlayerInfoText()
    {
        string info = "Players in Pads:\n";
        for(int i = 0; i < padsPlayerData.Length; i++)
        {
            info += "Pad [" + i + "] : " + (padsPlayerData[i] == "" ? "NPC" : padsPlayerData[i]) + "\n";
        }
        playerInfoText.text = info;
    }

    public void UpdatePadsData(int padIndex, string enteredPlayerName)
    {
            padsPlayerData[padIndex] = enteredPlayerName;
            UpdatePlayerInfoText();
    }

    

    // ------ Start Button Related ------
    public void OnStartButtonPressed() // NETWORKEVENT called when start button is pressed
    {
        //set ownership to the player who pressed start
        Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        Networking.SetOwner(Networking.LocalPlayer, uolVideoPlayer.gameObject); // also set owner of video player to ensure sync

        isStarted = true;


        RequestSerialization();

        //update UI
        UpdateStateUI();
        Log("[UOLCore] Start button pressed. isStarted set to true.");

        // disable NPC select buttons when started to prevent changes during playback
        if(isStarted){
            npcManager.SetSelectNPCButtonsActive(false); // disable NPC select buttons when started to prevent changes during playback
        }else{
            npcManager.SetSelectNPCButtonsActive(true); // enable NPC select buttons when not started
        }

        //load url
        videoPlayer.LoadURL(uolSongData.GetSongLink(currentSongIndex));
    }

    // when video ready, wait for all players to load before moving them to stations
    public void OnVideoStarted() // called by video player when video is ready
    {
        if (!isStarted) return; // prevent video ready before start
        if (isVideoReady) return; // prevent multiple calls

        npcManager.SetSelectNPCButtonsActive(false); // disable NPC select buttons when video starts to prevent changes during playback
        
        isVideoReady = true;
        
        // reset players video ready status
        ClearPlayersVideoReady();

        // mark pads that have no player as already ready (no need to wait)
        for (int i = 0; i < padsPlayerData.Length; i++)
        {
            if (string.IsNullOrEmpty(padsPlayerData[i]))
            {
                playersVideoReady[i] = true;
            }
        }

        RequestSerialization();
        
        videoPlayer.SetTime(0); // ensure video starts from beginning
        
        // notify all players that video is ready and they should confirm their readiness
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(OnPlayerVideoReady));

        //update UI
        UpdateStateUI();

        Log("[UOLCore] Video started. isVideoReady set to true.");

        // If this instance is owned by the local player, decide when to start playback
        if (Networking.IsOwner(this.gameObject))
        {
            int activePadCount = GetActivePadCount();
            
            // If no pad players exist, start immediately (all-spectator or all-NPC scenario)
            if (activePadCount == 0)
            {
                Log("[UOLCore] No pad players detected. Starting playback immediately.");
                StartPlaybackByOwner();
            }
            // If pad players exist and all are ready, start playback
            else if (AreAllPlayersVideoReady())
            {
                Log("[UOLCore] All pad players ready. Starting playback.");
                StartPlaybackByOwner();
            }
            // Otherwise wait for players to report ready
            else
            {
                Log("[UOLCore] Waiting for pad players to confirm video readiness.");
            }
        }

    }
    
    // called when a player's video has loaded
    [NetworkCallable]
    public void OnPlayerVideoReady()
    {
        if (!isVideoReady) return; // video not ready yet
        
        // mark this player as ready
        for(int i = 0; i < padsPlayerData.Length; i++)
        {
            if(padsPlayerData[i] == Networking.LocalPlayer.displayName)
            {
                playersVideoReady[i] = true;
                Debug.Log("[UOLCore] Player " + padsPlayerData[i] + " video ready.");
                break;
            }
        }
        RequestSerialization();
        
        // check if all players are ready
        if(AreAllPlayersVideoReady())
        {
            Debug.Log("[UOLCore] All players video ready. Moving players to stations.");
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(LocalUseStation));
        }
    }

    [NetworkCallable]
    public void LocalUseStation() // move local player to station according to pads data, or play as spectator if not in pad
    {
        bool isInPad = false;
        
        // Check if local player is registered in pads
        for(int i = 0; i < padsPlayerData.Length; i++)
        {
            if(padsPlayerData[i] == Networking.LocalPlayer.displayName)
            {
                // Pad player: enter station and play
                videoPlayer.Play();
                VRCStation station = uolSongData.GetStation(currentSongIndex, i);
                station.UseStation(Networking.LocalPlayer);
                SetCameraAnimator();
                isInPad = true;
                break;
            }
        }
        
        // If local player is not in pads, treat as spectator and just play video
        if (!isInPad)
        {
            videoPlayer.Play();
            SetCameraAnimator(); // apply global camera animator
            Log("[UOLCore] Non-pad player started watching video as spectator.");
        }

        // Summon NPCs for empty pads after players are moved
        SummonNPCsForCurrentSong();
    }

    public void UpdateStateUI(){
        stateInfos[0].SetActive(!isStarted); // start button active when not started
        stateInfos[1].SetActive(isStarted && !isVideoReady); // loading active when started but video not ready
        stateInfos[2].SetActive(isStarted && isVideoReady); // playing active when started and video ready
        stateInfos[3].SetActive(isStarted && !isVideoReady && uolVideoPlayer.IsError()); // video error active when started, video not ready and video player has error
    }
    


    // ------ Reset Button Related ------
    public void OnResetButtonPressed() // NETWORKEVENT called when reset button is pressed
    {
        // Transfer ownership to the local player if not already owner
        if (!Networking.IsOwner(this.gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
            Networking.SetOwner(Networking.LocalPlayer, uolVideoPlayer.gameObject);
        }
        
        videoPlayer.Stop(); // cancel video playing
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(VideoEnd)); // send to all clients
    }
    [NetworkCallable]
    public void VideoEnd() // called by video player when video ends ,reseted, or error occurs
    {

        //leave stations
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(LocalLeaveStation));

        // Destroy NPCs to clear animations
        DestroyNPCs();

        //reset current song index
        currentSongIndex = -1;

        //reset flags
        isStarted = false;
        isVideoReady = false;
        isPlaying = false;
        npcManager.SetSelectNPCButtonsActive(true); // enable NPC select buttons when video is reset

        // reset players video ready status
        ClearPlayersVideoReady();

        // reset last loaded URL for spectators
        lastLoadedUrl = VRCUrl.Empty;

        RequestSerialization();

        // turn on pads
        UpdatePadEnterParent();

        //open pads according to song data
        UpdatePads(currentSongIndex);
        //update UI
        UpdatePlayerInfoText();
        //update UI
        UpdateSongInfoUI();
        //update state UI
        UpdateStateUI();

        //set camera animator according to song index
        SetCameraAnimator();
    }
    [NetworkCallable]
    public void LocalLeaveStation() // called when local player leaves station
    {
        DestroyNPCs();

        //check player id in the pads data
        for (int i = 0; i < padsPlayerData.Length; i++)
        {
            if(padsPlayerData[i] == Networking.LocalPlayer.displayName)
            {
                VRCStation station = uolSongData.GetStation(currentSongIndex, i);
                if(station != null)
                {
                    station.ExitStation(Networking.LocalPlayer);
                }
                break;
            }
        }
        
        // Only reset tracking for pad players to avoid moving spectators unexpectedly
        if (IsLocalInPad())
        {
            stationResetTracking.UseStation(Networking.LocalPlayer);
            stationResetTracking.ExitStation(Networking.LocalPlayer);
        }
        
        // Stop video playback for all players (including spectators)
        videoPlayer.Stop();
        videoPlayer.SetTime(0); // ensure video starts from beginning
        
        
        ClearPadsPlayerData();
    }
    
    

    // ------ NPC Related ------
    // summon NPCs according to song data, called in UpdatePads() when song is selected or reset
    private void SummonNPC(int padIndex, VRCStation targetStation)
    {
        // Get the preview NPC from manager and move it to the pad position
        npcObjects[padIndex] = npcManager.GetNPCObject(padIndex, true, targetStation.animatorController);
        if (npcObjects[padIndex] != null)
        {
            npcObjects[padIndex].transform.position = targetStation.stationEnterPlayerLocation.position;
            npcObjects[padIndex].transform.rotation = targetStation.stationEnterPlayerLocation.rotation;
            npcObjects[padIndex].transform.SetParent(SpawnedNPCParent); // set parent to keep hierarchy organized
            npcObjects[padIndex].SetActive(true);
        }
    }

    private void SummonNPCsForCurrentSong()
        {
            for (int i = 0; i < padsPlayerData.Length; i++)
            {
                if (string.IsNullOrEmpty(padsPlayerData[i]) && npcObjects[i] == null)
                {
                    VRCStation station = uolSongData.GetStation(currentSongIndex, i);
                    if (station != null)
                    {
                        SummonNPC(i, station);
                        Log("[UOLCore] Summoned NPC for empty pad " + i);
                    }
                }
            }
        }

    private void DestroyNPCs()
    {
        // Move NPCs back to spawn points instead of destroying, to preserve preview NPCs
        for (int i = 0; i < npcObjects.Length; i++)
        {
            if (npcObjects[i] != null)
            {
                // Move back to spawn point or hide
                Transform spawnPoint = npcManager.GetSpawnPoint(i);
                if (spawnPoint != null)
                {
                    //clear animation
                    npcObjects[i].GetComponent<Animator>().runtimeAnimatorController = null;
                    npcObjects[i].transform.position = spawnPoint.position;
                    npcObjects[i].transform.rotation = spawnPoint.rotation;
                    npcObjects[i].transform.SetParent(null); // remove from SpawnedNPCParent
                    npcObjects[i].SetActive(true); // keep active for preview
                }
                else
                {
                    npcObjects[i].SetActive(false); // hide if no spawn point
                }
            }
        }
        npcObjects = new GameObject[padsPlayerData.Length]; // reset npc objects array, but preview NPCs remain
    }

    // ------ Song Selection Related ------
    private void SetCameraAnimator()
    {
        uolSongData.cameraAnimator.SetInteger("SongIndex", currentSongIndex);
    }
    private void SongSelect(int songIndex) //NETWORKEVENT called when a song is selected
    {
        //set song index
        currentSongIndex = songIndex;
        RequestSerialization();
        Debug.Log("[UOLCore] Song selected: " + uolSongData.GetSongTitle(currentSongIndex));
        Log("[UOLCore] Song selected: " + uolSongData.GetSongTitle(currentSongIndex));
        //open pads according to song data
        UpdatePads(currentSongIndex);
        //update UI
        UpdateSongInfoUI();
    }
    private void OnSongSelectButtonPressed(int buttonIndex) //called when a song select button is pressed
    {
        // get song from button index
        int si = uolSongData.FetchSongIndexByTitle(songMenu.GetItemInfoByButtonIndex(buttonIndex)[0]);
        //select song
        SongSelect(si);
    }
    //  Song Select Button Handlers 
    // {
    public void OnSongSelectButton0Pressed(){OnSongSelectButtonPressed(0);}
    public void OnSongSelectButton1Pressed(){OnSongSelectButtonPressed(1);}
    public void OnSongSelectButton2Pressed(){OnSongSelectButtonPressed(2);}
    public void OnSongSelectButton3Pressed(){OnSongSelectButtonPressed(3);}
    public void OnSongSelectButton4Pressed(){OnSongSelectButtonPressed(4);}
    public void OnSongSelectButton5Pressed(){OnSongSelectButtonPressed(5);}
    public void OnSongSelectButton6Pressed(){OnSongSelectButtonPressed(6);}
    public void OnSongSelectButton7Pressed(){OnSongSelectButtonPressed(7);}
    public void OnSongSelectButton8Pressed(){OnSongSelectButtonPressed(8);}
    // }
    
    // ------ Flags and Data Getters ------
    public int getPadAmmount()
    {
        return this.transform.Find("EnterPads").childCount;
    }

    public bool GetIsStarted() 
    {
        return isStarted;
    }
    public string[] GetPadsPlayerData()
    {
        return padsPlayerData;
    }
    public bool[] GetPlayersVideoReady()
    {
        return playersVideoReady;
    }

    // return true if local player is registered in pads list
    public bool IsLocalInPad()
    {
        for (int i = 0; i < padsPlayerData.Length; i++)
        {
            if (padsPlayerData[i] == Networking.LocalPlayer.displayName) return true;
        }
        return false;
    }

    // Owner-only: start global playback by writing a server timestamp and flag, then notify all clients to enter stations
    public void StartPlaybackByOwner()
    {
        if (!Networking.IsOwner(this.gameObject)) return;

        playbackStartTime = Networking.GetServerTimeInMilliseconds() / 1000.0;
        playbackIsPlaying = true;
        RequestSerialization();

        // Move pad players to stations (they will start playback locally) and notify everyone
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(LocalUseStation));

        Log("[UOLCore] Owner started playback at " + playbackStartTime);
    }

    // Owner-only: Force start playback immediately (manual trigger for NPC or timeout scenarios)
    public void ForceStartPlayback()
    {
        if (!Networking.IsOwner(this.gameObject)) return;
        
        Log("[UOLCore] Force starting playback (Owner manual trigger).");
        StartPlaybackByOwner();
    }

    
    // ------ UI Update Related ------
    private void UpdateSongInfoUI()
    {
        if(currentSongIndex >= 0 && currentSongIndex < uolSongData.GetSongAmount())
        {
            songTitleText.text = uolSongData.GetSongTitle(currentSongIndex);
            songInfoText.text = uolSongData.GetSongInfo(currentSongIndex);
            songLinkText.text = uolSongData.GetSongLink(currentSongIndex).ToString();
        }
        else
        {
            songTitleText.text = "No Song Selected";
            songInfoText.text = "";
            songLinkText.text = "";
        }
    }
    // ------ Log Related ------
    public void Log(string message){
        Debug.Log(message);
        LogText.text += message + "\n";
    }
}
}
