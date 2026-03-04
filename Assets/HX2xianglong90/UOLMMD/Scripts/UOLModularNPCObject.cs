
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace HX2xianglong90.UOLMMD
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class UOLModularNPCObject : UdonSharpBehaviour
{
    // Blendshape Setter
    [SerializeField]private SkinnedMeshRenderer[] targetMeshRenderers;
    [SerializeField]private string[] targetBlendshapeNames;
    [SerializeField]private float[] targetBlendshapeValues;

    // Material Setter
    [SerializeField]private Renderer[] targetRenderers;
    [SerializeField]private int[] targetMaterialIndices;
    [SerializeField]private Material[] targetMaterials;

    // store active
    private bool isActiveAtStart = true;
    // original settings storage
    public float[] originalBlendshapeValues; // store original blendshape values for resetting
    public Material[] originalMaterials; // store original materials for resetting

    public void InitObject()
    {
        RecordOriginalSettings(); // record original settings on first enable
        isActiveAtStart = this.gameObject.activeSelf; // store initial active state
        Debug.Log("NPC Object Initialized: " + this.gameObject.name);
    }
    void OnEnable()
    {
        if(!isActiveAtStart)
        {
        ApplyNewSettings(); 
        }
        else
        {
        ApplyOriginalSettings(); 
        }
        
    }

    void OnDisable()
    {
        if(isActiveAtStart)
        {
        ApplyNewSettings(); 
        }
        else
        {
        ApplyOriginalSettings(); 
        }
    }

    private void RecordOriginalSettings()
    {
        // This method can be called to record original settings before any changes are made, if needed
        if(originalBlendshapeValues == null || originalBlendshapeValues.Length != targetBlendshapeValues.Length)
        {
            originalBlendshapeValues = new float[targetBlendshapeValues.Length];
            for(int i = 0; i < targetBlendshapeValues.Length; i++)            {
                if(i < targetMeshRenderers.Length && targetMeshRenderers[i] != null && i < targetBlendshapeNames.Length)
                {
                    int blendshapeIndex = targetMeshRenderers[i].sharedMesh.GetBlendShapeIndex(targetBlendshapeNames[i]);
                    if(blendshapeIndex >= 0)
                    {
                        originalBlendshapeValues[i] = targetMeshRenderers[i].GetBlendShapeWeight(blendshapeIndex);
                    }
                }
            }
        }
        if(originalMaterials == null || originalMaterials.Length != targetRenderers.Length)
        {
            originalMaterials = new Material[targetRenderers.Length];
            for(int i = 0; i < targetRenderers.Length; i++)            {
                if(i < targetRenderers.Length && targetRenderers[i] != null)                {
                    originalMaterials[i] = targetRenderers[i].material; // store original material
                }
            }
        }
    }

    private void ApplyNewSettings()
    {
        // Traverse renderers, set BlendShape and material (same as original solution 3)
        for(int i = 0; i < targetMeshRenderers.Length; i++)
        {
            SkinnedMeshRenderer smr = targetMeshRenderers[i];      
            if(smr != null && i < targetBlendshapeNames.Length && i < targetBlendshapeValues.Length)
            {
                int blendshapeIndex = smr.sharedMesh.GetBlendShapeIndex(targetBlendshapeNames[i]);
                if(blendshapeIndex >= 0)
                {
                    smr.SetBlendShapeWeight(blendshapeIndex, targetBlendshapeValues[i]);
                }
            }
        }
        // Set materials
        for(int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer rend = targetRenderers[i];
            if(rend != null && i < targetMaterialIndices.Length && i < targetMaterials.Length)
            {
                Material[] mats = rend.materials;
                int matIndex = targetMaterialIndices[i];
                if(matIndex >= 0 && matIndex < mats.Length)
                {
                    mats[matIndex] = targetMaterials[i];
                    rend.materials = mats;
                }
            }
        }
    }

    private void ApplyOriginalSettings()
    {
        // Reset BlendShapes to original values
        for(int i = 0; i < targetMeshRenderers.Length; i++)
        {
            SkinnedMeshRenderer smr = targetMeshRenderers[i];      
            if(smr != null && i < targetBlendshapeNames.Length && i < originalBlendshapeValues.Length)
            {
                int blendshapeIndex = smr.sharedMesh.GetBlendShapeIndex(targetBlendshapeNames[i]);
                if(blendshapeIndex >= 0)
                {
                    smr.SetBlendShapeWeight(blendshapeIndex, originalBlendshapeValues[i]);
                }
            }
        }
        // Reset materials to original
        for(int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer rend = targetRenderers[i];
            if(rend != null && i < originalMaterials.Length)
            {
                Material[] mats = rend.materials;
                int matIndex = targetMaterialIndices[i];
                if(matIndex >= 0 && matIndex < mats.Length)
                {
                    mats[matIndex] = originalMaterials[i];
                    rend.materials = mats;
                }
            }
        }
    }
}
}