
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using HX2xianglong90.SimplePageMenu;
using UnityEngine.Animations;

namespace HX2xianglong90.UOLMMD
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class UOLNPCManager : UdonSharpBehaviour
{
    [SerializeField]private UOLCore uolCore;
    [UdonSynced,SerializeField]private string[] padNPCSettings; // format: "NPCName:Accesory status(0 or 1)"
    private int selectedPadIndex = 0; // track which pad is currently being edited in the menu
    [UdonSynced,SerializeField]private int[] padToNpcIndex; // map pad index to NPC index

    // Whether follow Player Hand when start
    public bool followPlayerHandWhenPlaying = true;

    public ParentConstraint handFollowConstraint; // assign the ParentConstraint component used for following player hand in inspector
    public ScaleConstraint handFollowScaleConstraint; // assign the ScaleConstraint component used for following player hand in inspector

    // Preset NPCs
    public UOLNPC[] uolNPCs;
    private string[] npcNames; // store NPC names for each pad
    private GameObject[] npcObjects; // assign NPC game objects in inspector


    // Preview NPCs
    private UOLNPC[] previewNPCs; // preview NPC scripts for each pad, used to apply settings and get accessory references
    private GameObject[] previewNPCObjects; // preview NPC objects for each pad, instantiated and active for trigger to work
    [SerializeField] private Transform npcPreviewSpawnPointsParent; // parent object containing spawn points for preview NPCs
    private Transform[] npcPreviewSpawnPoints; // spawn points for preview NPCs


    //ui
    [SerializeField] private SimpleButtonPages padNpcSettingsMenu; // menu for selecting the pad which assigning NPCs
    [SerializeField] private SimpleButtonPages npcPageMenu; // menu for assigning NPCs to pads
    [SerializeField] private SimpleButtonPages accessoryPageMenu; // menu for turning on/off accessories
    void Start()
    {
        //init npcs
        for(int i = 0; i < uolNPCs.Length; i++)
        {
            if(uolNPCs[i] != null)
            {
                uolNPCs[i].InitNPC();
            }
        }

        if(uolCore == null)
        {
            Debug.LogError("UOLCore is not assigned");
            return;
        }
        if(uolNPCs == null || uolNPCs.Length == 0)
        {
            Debug.LogError("UOLNPCs array is empty or null");
            return;
        }
        padNPCSettings = new string[uolCore.getPadAmmount()]; // initialize pad NPC settings array
        padToNpcIndex = new int[padNPCSettings.Length]; // initialize pad to NPC index mapping
        previewNPCObjects = new GameObject[padNPCSettings.Length]; // initialize preview NPCs array

        // initialize preview NPC array and spawn points
        previewNPCs = new UOLNPC[padNPCSettings.Length];
        previewNPCObjects = new GameObject[padNPCSettings.Length];
        if(npcPreviewSpawnPointsParent == null)
        {
            Debug.LogError("NPC Preview Spawn Points Parent is not assigned");
            return;
        }
        npcPreviewSpawnPoints = new Transform[npcPreviewSpawnPointsParent.childCount];
        for(int i = 0; i < npcPreviewSpawnPoints.Length; i++)        {
            npcPreviewSpawnPoints[i] = npcPreviewSpawnPointsParent.GetChild(i);
        }

        // initialize NPC data arrays based on the number of UOLNPC objects
        npcNames = new string[uolNPCs.Length];
        string[] npcInfo = new string[uolNPCs.Length];
        npcObjects = new GameObject[uolNPCs.Length];

        for(int i = 0; i < uolNPCs.Length; i++)
        {
            if(uolNPCs[i] == null)
            {
                Debug.LogError("UOLNPC at index " + i + " is null");
                continue;
            }
            npcNames[i] = uolNPCs[i].npcName;
            npcInfo[i] = uolNPCs[i].npcInfo;
            npcObjects[i] = uolNPCs[i].gameObject;
        }

        // initialize default pad NPC settings (first npc, get accessory status from UOLNPC)
        for(int i = 0; i < padNPCSettings.Length; i++)
        {
            int npcIndex = 0; // default to first NPC
            if(uolNPCs[npcIndex] == null)
            {
                Debug.LogError("Default NPC is null");
                continue;
            }
            padToNpcIndex[i] = npcIndex; // set mapping
            bool[] accessoryStatus = uolNPCs[npcIndex].GetAccessoryStatus();


            // convert accessory status bool array to string of 1s and 0s
            string accessoryStatusString = "";
            if(accessoryStatus != null)
            {
                for(int j = 0; j < accessoryStatus.Length; j++)
                {
                    accessoryStatusString += accessoryStatus[j] ? "●" : "○"; // convert bool array to string of "●" and "○" for display in menu
                }
            }
            padNPCSettings[i] = npcNames[npcIndex] + ":" + accessoryStatusString;
        }
        
        // dump data to menus 
        npcPageMenu.SetItem(npcNames,npcInfo);

        RefreshAccessoryPageMenu(); // initialize accessory menu with first pad's NPC data
        RefreshPadNPCSettingsMenu(); // initialize pad NPC settings menu with current settings
    }

    override public void OnDeserialization()
    {
        //check the NPC Spwaned or not, if spawned then update the preview NPCs to match the synced settings,
        // if not spawned, spawn according to the synced settings, this is to ensure the preview NPCs are 
        // always up to date with the synced settings when players join in or when settings are changed by other players
        RefreshPadToNPCIndex(); // update pad to NPC index mapping based on synced padNPCSettings
        for (int i = 0; i < padNPCSettings.Length; i++)
        {
            if (previewNPCObjects[i] == null || previewNPCs[i].npcName != npcNames[padToNpcIndex[i]])//Need to change to check whether the names match
            {
                DestroyPreviewNPC(i); // destroy existing preview NPCs to avoid duplicates or mismatches
                // spawn preview NPC according to synced settings
                int npcIndex = padToNpcIndex[i];
                if(npcIndex >= 0 && npcIndex < npcObjects.Length)
                {
                    previewNPCObjects[i] = Instantiate(npcObjects[npcIndex]);
                    previewNPCs[i] = previewNPCObjects[i].GetComponent<UOLNPC>();
                    previewNPCs[i].InitNPC(); // initialize the new preview NPC to ensure it's ready to have settings applied
                    previewNPCObjects[i].SetActive(true); // ensure active for triggers to work
                    // Move to the corresponding spawn point for player observation
                    if (i < npcPreviewSpawnPoints.Length && npcPreviewSpawnPoints[i] != null)
                    {
                        previewNPCObjects[i].transform.position = npcPreviewSpawnPoints[i].position;
                        previewNPCObjects[i].transform.rotation = npcPreviewSpawnPoints[i].rotation;
                    }
                    else
                    {
                        previewNPCObjects[i].transform.position = new Vector3(0, -1000, 0); // fallback hidden spot
                    }
                }
            }
        }

        // Update preview NPCs to match synced settings
        PadNPCSettingDataToPreview();
        // Refresh UI after deserialization to reflect synced padNPCSettings
        RefreshPadNPCSettingsMenu();
        RefreshAccessoryPageMenu();
    }

    // ------ Getters and Setters for NPC data ------

    // set npc for each pad
    public string[] GetPadNPCSettings()
    {
        return padNPCSettings;
    }
    
    // get NPC GameObjects with accessories applied based on pad settings
    public GameObject GetNPCObject(int padIndex, bool needAnimated = true, RuntimeAnimatorController animatorController = null)
    {
        if (padIndex < 0 || padIndex >= previewNPCObjects.Length || previewNPCObjects[padIndex] == null)
        {
            Debug.LogError("Preview NPC for pad " + padIndex + " is not available");
            return null;
        }
        GameObject npcObject = previewNPCObjects[padIndex];
        if (!needAnimated)
        {
            return npcObject;
        }
        if (needAnimated && animatorController != null)
        {
            Animator animator = npcObject.GetComponent<Animator>();
            if (animator != null) animator.runtimeAnimatorController = animatorController;
        }
        return npcObject;
    }


    // ------------------ UI Refreshers ------------------

    // refresh pad npc settings info on menu, call this after updating padNPCSettings array
    //display npc name and accessory status for each pad in the menu
    // using SetItem() Name: NPCname, Value: accessory status
    private void RefreshPadNPCSettingsMenu(){
        //split the padNPCSettings string to get npc name and accessory status, put then into the menu
        string[] npcNames = new string[padNPCSettings.Length];
        string[] accessoryStatuses = new string[padNPCSettings.Length];
        for(int i = 0; i < padNPCSettings.Length; i++)
        {
            string setting = padNPCSettings[i];
            string[] parts = setting.Split(':');
            if(parts.Length != 2)
            {
                Debug.LogError("Invalid pad NPC setting format at index " + i + ": " + setting);
                npcNames[i] = "Invalid Setting";
                accessoryStatuses[i] = "";
                continue;
            }
            npcNames[i] = parts[0];
            accessoryStatuses[i] = parts[1];
        }
        padNpcSettingsMenu.SetItem(npcNames, accessoryStatuses);
        padNpcSettingsMenu.SwitchSelection(selectedPadIndex); // keep the current selection highlighted
    }

    private void RefreshAccessoryPageMenu() {    //called when selecting a pad to edit in the menu
        //dump current data to menu
        int npcIndex = padToNpcIndex[selectedPadIndex]; // get the NPC index for the selected pad
        if(uolNPCs[npcIndex] == null)
        {
            Debug.LogError("UOLNPC at index " + npcIndex + " is null");
            return;
        }
        accessoryPageMenu.SetItem(uolNPCs[npcIndex].GetAccessoryNames(),
        GetAccessoryStatusStrForPad(selectedPadIndex));
    }

    // ------ Button event handlers ------
    private void OnSelectPad(int index)
    {
        if(index >= 0 && index < padNPCSettings.Length)
        {
            selectedPadIndex = index;
            padNpcSettingsMenu.SwitchSelection(selectedPadIndex); // keep the current selection highlighted
            accessoryPageMenu.GoToPage(1); // reset to page 1 when updating menu
            RefreshAccessoryPageMenu(); // update accessory menu to show accessories for the newly selected pad
            RefreshPadNPCSettingsMenu(); // update pad NPC settings menu to reflect the change
        }
    }


    // Assign NPC to specific pad, update preview NPC, need to update AccessoryPageMenu
    private void OnSelectNPC(int padIndex,int npcIndex)
    {
        // Transfer ownership if not owner
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        if(padIndex < 0 || padIndex >= padNPCSettings.Length)
        {
            Debug.LogError("Invalid pad index: " + padIndex);
            return;
        }
        if(npcIndex < 0 || npcIndex >= npcNames.Length)
        {
            Debug.LogError("Invalid NPC index: " + npcIndex);
            return;
        }
        if(uolNPCs[npcIndex] == null)
        {
            Debug.LogError("UOLNPC at index " + npcIndex + " is null");
            return;
        }


        // Destroy existing preview NPC if any
        if (previewNPCObjects[padIndex] != null)
        {
            Destroy(previewNPCObjects[padIndex]);
        }


        // Instantiate new preview NPC as active
        previewNPCObjects[padIndex] = Instantiate(npcObjects[npcIndex]);
        previewNPCs[padIndex] = previewNPCObjects[padIndex].GetComponent<UOLNPC>(); // get the UOLNPC script reference for the new preview NPC
        previewNPCs[padIndex].InitNPC(); // initialize the new preview NPC to ensure it's ready to have settings applied
        previewNPCObjects[padIndex].SetActive(true); // ensure active for triggers to work
        // Initialize UOLModularNPCObject components in the new preview NPC
        UOLModularNPCObject[] modularObjects = previewNPCObjects[padIndex].GetComponentsInChildren<UOLModularNPCObject>();
        foreach (UOLModularNPCObject obj in modularObjects)
        {
            obj.InitObject();
        }
        // Move to the corresponding spawn point for player observation
        if (padIndex < npcPreviewSpawnPoints.Length && npcPreviewSpawnPoints[padIndex] != null)
        {
            previewNPCObjects[padIndex].transform.position = npcPreviewSpawnPoints[padIndex].position;
            previewNPCObjects[padIndex].transform.rotation = npcPreviewSpawnPoints[padIndex].rotation;
        }
        else
        {
            previewNPCObjects[padIndex].transform.position = new Vector3(0, -1000, 0); // fallback hidden spot
        }

        // update mapping and settings
        padToNpcIndex[padIndex] = npcIndex; // update mapping

        // get current accessory status for the selected NPC
        bool[] accessoryStatus = previewNPCs[padIndex].GetAccessoryStatus();
        // convert accessory status bool array to string of 1s and 0s
        string accessoryStatusString = "";
        if(accessoryStatus != null && accessoryStatus.Length != 0) { // if no accessories, set to empty string
        for(int j = 0; j < accessoryStatus.Length; j++)
        {
            accessoryStatusString += accessoryStatus[j] ? "●" : "○"; // convert bool array to string of "●" and "○" for display in menu
        }
        }
        
        // update pad NPC setting with new NPC and its accessory status
        padNPCSettings[padIndex] = npcNames[npcIndex] + ":" + accessoryStatusString;
        
        RequestSerialization(); // ensure immediate sync of padNPCSettings

        PadNPCSettingDataToPreview(); // apply accessory status to preview NPC
        accessoryPageMenu.GoToPage(1); // reset to page 1 when updating menu
        RefreshAccessoryPageMenu(); // update accessory menu to show accessories for the newly selected NPC
        RefreshPadNPCSettingsMenu(); // update pad NPC settings menu to reflect the change
    }


    // ------ For buttons turning on/off accessories ------

    private void PadNPCSettingDataToPreview()
    {
        //get accesory status from padNPCSettings and apply to preview NPC
        // apply accessory status to preview NPC
        for(int i = 0; i < padNPCSettings.Length; i++)
        {
            if(i < previewNPCs.Length && previewNPCs[i] != null)
            {
                previewNPCs[i].SetAccessoriesStatus(ParseAccessoryStatusString(padNPCSettings[i].Split(':')[1])); // get accessory status string, convert to bool array and apply to preview NPC
            }
        }
    }

    private void ToggleAccessory(int padIndex,int accessoryIndex)
    {
        // Transfer ownership if not owner
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        // validate indices and references
        if(padIndex < 0 || padIndex >= padNPCSettings.Length)
        {
            Debug.LogError("Invalid pad index: " + padIndex);
            return;
        }
        if (previewNPCObjects[padIndex] == null)
        {
            Debug.LogError("Preview NPC for pad " + padIndex + " is null");
            return;
        }
        
        int npcIndex = padToNpcIndex[padIndex];
        if (npcIndex < 0 || npcIndex >= uolNPCs.Length || uolNPCs[npcIndex] == null)
        {
            Debug.LogError("Invalid NPC index: " + npcIndex);
            return;
        }

        // Toggle accessory by method
        previewNPCs[padIndex].ToggleAccessoryStatus(accessoryIndex);

        // update the padNPCSettings string to reflect the change
        padNPCSettings[padIndex] = npcNames[npcIndex] + ":" + string.Join("", GetAccessoryStatusStrForPad(padIndex)); // update the padNPCSettings with new accessory status

        RequestSerialization(); // ensure immediate sync of padNPCSettings
        RefreshPadNPCSettingsMenu(); // update pad NPC settings menu to reflect the change
        RefreshAccessoryPageMenu(); // update menu to reflect new accessory status
    }

    private void RefreshPadToNPCIndex(){
        for(int i = 0; i < padNPCSettings.Length; i++)
        {
            string setting = padNPCSettings[i];
            string[] parts = setting.Split(':');
            if(parts.Length != 2)
            {
                Debug.LogError("Invalid pad NPC setting format at index " + i + ": " + setting);
                continue;
            }
            string npcName = parts[0];
            int npcIndex = System.Array.IndexOf(npcNames, npcName);
            if(npcIndex >= 0)
            {
                padToNpcIndex[i] = npcIndex;
            }
            else
            {
                Debug.LogError("NPC name not found in npcNames array: " + npcName);
            }
        }
    }

    private string[] GetAccessoryStatusStrForPad(int padIndex)
    {
        //if no preciew NPC, use preset npc data to get accessory status
        if(previewNPCs[padIndex] == null)        {
        string[] statusStr = uolNPCs[padToNpcIndex[padIndex]].GetAccessoryStatusStr("●", "○"); 
        return statusStr;
        }else{
        string[] statusStr = previewNPCs[padIndex].GetAccessoryStatusStr("●", "○"); 
        return statusStr;
        }
    }

    private bool[] ParseAccessoryStatusString(string statusString)
    {
        bool[] statusArray = new bool[statusString.Length];
        for(int i = 0; i < statusString.Length; i++)
        {
            statusArray[i] = statusString[i] == '●'; // convert "●" to true, "○" to false
        }
        return statusArray;
    }

    // inactive select NPC buttons when started
    public void SetSelectNPCButtonsActive(bool active,bool followPlayerHand = true, bool hideNPCPageWhenInactive = true)
    {
        if(active)
        {
            npcPageMenu.EnableAllButtons();
            if (hideNPCPageWhenInactive)
            {
                npcPageMenu.gameObject.SetActive(true);
            }
            if(followPlayerHand)
            {
                StopFollowingPlayerHand();
            }
        }
        else
        {
            npcPageMenu.DisableAllButtons();
            if (hideNPCPageWhenInactive)
            {
                npcPageMenu.gameObject.SetActive(false);
            }
            if(followPlayerHand)
            {
                FollowPlayerHand();
            }
        }
    }
    // destroy specific preview NPC
    private void DestroyPreviewNPC(int padIndex)
    {
        if (padIndex >= 0 && padIndex < previewNPCObjects.Length && previewNPCObjects[padIndex] != null)
        {
            Destroy(previewNPCObjects[padIndex]);
            previewNPCObjects[padIndex] = null;
        }
    }
    // Public method to get spawn point for a pad
    public Transform GetSpawnPoint(int padIndex)
    {
        if (padIndex >= 0 && padIndex < npcPreviewSpawnPoints.Length)
        {
            return npcPreviewSpawnPoints[padIndex];
        }
        return null;
    }

    private void FollowPlayerHand()
    {
        if (handFollowConstraint != null)
        {
            handFollowConstraint.weight = 1f;
        }
        if (handFollowScaleConstraint != null)
        {
            handFollowScaleConstraint.weight = 1f;
        }
    }
    private void StopFollowingPlayerHand()
    {
        if (handFollowConstraint != null)
        {
            handFollowConstraint.weight = 0f;
        }
        if (handFollowScaleConstraint != null)
        {
            handFollowScaleConstraint.weight = 0f;
        }
    }


    // ------Button event handlers for menu buttons, call the corresponding methods with the correct indices ------

    //for Pad select buttons
    public void OnSelectPad0(){ OnSelectPad(0); }
    public void OnSelectPad1(){ OnSelectPad(1); }
    public void OnSelectPad2(){ OnSelectPad(2); }
    public void OnSelectPad3(){ OnSelectPad(3); }
    public void OnSelectPad4(){ OnSelectPad(4); }
    //for NPC select buttons
    public void OnSelectNPC0(){ OnSelectNPC(selectedPadIndex,npcPageMenu.GetItemIndexByButtonIndex(0)); }
    public void OnSelectNPC1(){ OnSelectNPC(selectedPadIndex,npcPageMenu.GetItemIndexByButtonIndex(1)); }
    public void OnSelectNPC2(){ OnSelectNPC(selectedPadIndex,npcPageMenu.GetItemIndexByButtonIndex(2)); }
    public void OnSelectNPC3(){ OnSelectNPC(selectedPadIndex,npcPageMenu.GetItemIndexByButtonIndex(3)); }
    public void OnSelectNPC4(){ OnSelectNPC(selectedPadIndex,npcPageMenu.GetItemIndexByButtonIndex(4)); }
    public void OnSelectNPC5(){ OnSelectNPC(selectedPadIndex,npcPageMenu.GetItemIndexByButtonIndex(5)); }

    //for Accessory toggle buttons
    public void OnToggleAccessory0(){ ToggleAccessory(selectedPadIndex,accessoryPageMenu.GetItemIndexByButtonIndex(0)); }
    public void OnToggleAccessory1(){ ToggleAccessory(selectedPadIndex,accessoryPageMenu.GetItemIndexByButtonIndex(1)); }
    public void OnToggleAccessory2(){ ToggleAccessory(selectedPadIndex,accessoryPageMenu.GetItemIndexByButtonIndex(2)); }
    public void OnToggleAccessory3(){ ToggleAccessory(selectedPadIndex,accessoryPageMenu.GetItemIndexByButtonIndex(3)); }
    public void OnToggleAccessory4(){ ToggleAccessory(selectedPadIndex,accessoryPageMenu.GetItemIndexByButtonIndex(4)); }



}
}