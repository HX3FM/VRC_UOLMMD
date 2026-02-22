

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace HX2xianglong90.UOLMMD
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class UOLNPC : UdonSharpBehaviour
{
    public string npcName;
    public string npcInfo;
    public GameObject[] accessoryObjects; // assign the accessory object in inspector
    private bool[] accessoryStatus; // track current status of each accessory
    public void InitNPC()
    {
        //init NPC
        if(accessoryObjects == null)
        {
            accessoryStatus = new bool[0];
            return;
        }
        accessoryStatus = new bool[accessoryObjects.Length];
        for(int i = 0; i < accessoryStatus.Length; i++)
        {
            accessoryStatus[i] = accessoryObjects[i].activeSelf; // initialize status based on current active state of accessory objects
        }
        Debug.Log("NPC " + npcName + " initialized with " + accessoryObjects.Length + " accessories.");
        //  init NPC scripts
        foreach(GameObject accessory in accessoryObjects)
        {
            UOLModularNPCObject npcObject = accessory.GetComponent<UOLModularNPCObject>();
            if(npcObject != null)
            {
                npcObject.InitObject(); // call InitObject on each accessory's UOLModularNPCObject script to ensure they are properly initialized
            }
        }
    }
    public string[] GetAccessoryNames()
    {
        if(accessoryObjects == null || accessoryObjects.Length == 0) return new string[0]; // if no accessories, return empty array
        string[] names = new string[accessoryObjects.Length];
        for(int i = 0; i < accessoryObjects.Length; i++)
        {
            names[i] = accessoryObjects[i].name;
        }
        return names;
    }
    public bool[] GetAccessoryStatus()
    {
        if(accessoryObjects == null || accessoryObjects.Length == 0) return new bool[0]; // if no accessories, return empty array
        return accessoryStatus;
    }
    public string[] GetAccessoryStatusStr(string onStr = "On", string offStr = "Off")
    {
        if(accessoryObjects == null || accessoryObjects.Length == 0) return new string[0]; // if no accessories, return empty array
        string[] statusStr = new string[accessoryStatus.Length];
        for(int i = 0; i < accessoryStatus.Length; i++)
        {
            statusStr[i] = accessoryStatus[i] ? onStr : offStr;
        }
        return statusStr;
    }
    public void SetAccessoryStatus(int index, bool status)
    {
        if(accessoryObjects == null || accessoryObjects.Length == 0) return; // if no accessories, do nothing
        if(index >= 0 && index < accessoryStatus.Length)
        {
            accessoryStatus[index] = status;
            accessoryObjects[index].SetActive(status); // update the active state of the accessory object   
        }
    }
    public void SetAccessoriesStatus(bool[] statusArray)
    {
        if(accessoryObjects == null || accessoryObjects.Length == 0) return; // if no accessories, do nothing
        for(int i = 0; i < accessoryStatus.Length && i < statusArray.Length; i++)
        {
            accessoryStatus[i] = statusArray[i];
            accessoryObjects[i].SetActive(statusArray[i]); // update the active state of the accessory object   
        }
    }
    public void ToggleAccessoryStatus(int index)
    {   
        if(accessoryObjects == null || accessoryStatus.Length == 0) return; // if no accessories, do nothing
        if(index >= 0 && index < accessoryStatus.Length)
        {
            accessoryStatus[index] = !accessoryStatus[index];
            accessoryObjects[index].SetActive(accessoryStatus[index]); // update the active state of the accessory object   
        }
    }
}
}