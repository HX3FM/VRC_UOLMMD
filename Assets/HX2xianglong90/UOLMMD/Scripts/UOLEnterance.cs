
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace HX2xianglong90.UOLMMD
{
[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class UOLEnterance : UdonSharpBehaviour
{
    [SerializeField]private UOLCore uolCore;
    private Material defaultMat; // default material for the pad when no player is on it
    [SerializeField]private Material occupiedMat; // material for the pad when a player is on it
    private int padIndex;

    void Start()
    {
        uolCore.Log("[UOLEnterance" + this.gameObject.name + "] Initialized.");
        padIndex = int.Parse(this.gameObject.name.Replace("Pad",""));
        defaultMat = this.GetComponent<Renderer>().material; // store default material
    }
    //reset entered player when turned on
    void OnEnable()
    {
        uolCore.Log("[UOLEnterance" + this.gameObject.name + "] Enabled. Resetting pad state.");
        this.GetComponent<Renderer>().material = defaultMat; // reset material to default when enabled
        uolCore.UpdatePadsData(padIndex, ""); // reset player data for this pad
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (uolCore.GetIsStarted()) // if the song has started, ignore player entry
        {
            uolCore.Log("[UOLEnterance" + this.gameObject.name + "] Player " + player.displayName + " entered trigger, but song has started. Ignoring.");
            return;
        }
        uolCore.Log("[UOLEnterance" + this.gameObject.name + "] Player " + player.displayName + " entered trigger.");
        this.GetComponent<Renderer>().material = occupiedMat; // change material to occupied when player enters
        uolCore.UpdatePadsData(padIndex, player.displayName);
        Debug.Log("[UOLEnterance" + this.gameObject.name + "] Player " + player.displayName + " entered trigger.");
    }
    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (uolCore.GetIsStarted()) // if the song has started, ignore player exit
        {
            uolCore.Log("[UOLEnterance" + this.gameObject.name + "] Player " + player.displayName + " exited trigger, but song has started. Ignoring.");
            return;
        }

        uolCore.Log("[UOLEnterance" + this.gameObject.name + "] Player " + player.displayName + " exited trigger.");
        this.GetComponent<Renderer>().material = defaultMat; // change material back to default when player exits
        uolCore.UpdatePadsData(padIndex, "");
        Debug.Log("[UOLEnterance" + this.gameObject.name + "] Player " + player.displayName + " exited trigger.");
    }
}
}
