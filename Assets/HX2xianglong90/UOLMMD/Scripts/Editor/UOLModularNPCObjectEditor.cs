using UnityEngine;
using UnityEditor;
using UdonSharp;

namespace HX2xianglong90.UOLMMD
{

[CustomEditor(typeof(UOLModularNPCObject))]

public class UOLModularNPCObjectEditor : Editor
{
    private SerializedProperty targetMeshRenderers;
    private SerializedProperty targetBlendshapeNames;
    private SerializedProperty targetBlendshapeValues;
    private SerializedProperty targetRenderers;
    private SerializedProperty targetMaterialIndices;
    private SerializedProperty targetMaterials;

    private void OnEnable()
    {
        targetMeshRenderers = serializedObject.FindProperty("targetMeshRenderers");
        targetBlendshapeNames = serializedObject.FindProperty("targetBlendshapeNames");
        targetBlendshapeValues = serializedObject.FindProperty("targetBlendshapeValues");
        targetRenderers = serializedObject.FindProperty("targetRenderers");
        targetMaterialIndices = serializedObject.FindProperty("targetMaterialIndices");
        targetMaterials = serializedObject.FindProperty("targetMaterials");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Blendshape Setter Section
        EditorGUILayout.LabelField("Blendshape Setter", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(targetMeshRenderers, new GUIContent("Target Mesh Renderers"), true);
        EditorGUILayout.PropertyField(targetBlendshapeValues, new GUIContent("Target Blendshape Values"), true);

        // Adjust array size for targetBlendshapeNames
        int blendshapeCount = EditorGUILayout.IntField("Blendshape Count", targetBlendshapeNames.arraySize);
        targetBlendshapeNames.arraySize = blendshapeCount;

        // Custom dropdown for targetBlendshapeNames
        EditorGUILayout.LabelField("Target Blendshape Names");
        for (int i = 0; i < targetBlendshapeNames.arraySize; i++)
        {
            SerializedProperty nameProp = targetBlendshapeNames.GetArrayElementAtIndex(i);
            string[] options = GetBlendShapeOptions(i);
            int selectedIndex = Mathf.Max(0, System.Array.IndexOf(options, nameProp.stringValue));
            selectedIndex = EditorGUILayout.Popup($"Blendshape Name {i}", selectedIndex, options);
            if (selectedIndex >= 0 && selectedIndex < options.Length)
            {
                nameProp.stringValue = options[selectedIndex];
            }
        }

        // Separator
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // Material Setter Section
        EditorGUILayout.LabelField("Material Setter", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(targetRenderers, new GUIContent("Target Renderers"), true);
        EditorGUILayout.PropertyField(targetMaterials, new GUIContent("Target Materials"), true);

        // Adjust array size for targetMaterialIndices
        int materialCount = EditorGUILayout.IntField("Material Count", targetMaterialIndices.arraySize);
        targetMaterialIndices.arraySize = materialCount;

        // Custom dropdown for targetMaterialIndices
        EditorGUILayout.LabelField("Target Material Indices");
        for (int i = 0; i < targetMaterialIndices.arraySize; i++)
        {
            SerializedProperty indexProp = targetMaterialIndices.GetArrayElementAtIndex(i);
            string[] options = GetMaterialIndexOptions(i);
            int selectedIndex = Mathf.Max(0, System.Array.IndexOf(options, indexProp.intValue.ToString()));
            selectedIndex = EditorGUILayout.Popup($"Material Index {i}", selectedIndex, options);
            if (selectedIndex >= 0 && selectedIndex < options.Length)
            {
                indexProp.intValue = int.Parse(options[selectedIndex]);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private string[] GetBlendShapeOptions(int index)
    {
        if (index >= targetMeshRenderers.arraySize) return new string[] { "N/A" };

        SerializedProperty rendererProp = targetMeshRenderers.GetArrayElementAtIndex(index);
        SkinnedMeshRenderer smr = rendererProp.objectReferenceValue as SkinnedMeshRenderer;
        if (smr == null || smr.sharedMesh == null) return new string[] { "No Mesh" };

        int count = smr.sharedMesh.blendShapeCount;
        string[] options = new string[count];
        for (int i = 0; i < count; i++)
        {
            options[i] = smr.sharedMesh.GetBlendShapeName(i);
        }
        return options.Length > 0 ? options : new string[] { "No BlendShapes" };
    }

    private string[] GetMaterialIndexOptions(int index)
    {
        if (index >= targetRenderers.arraySize) return new string[] { "N/A" };

        SerializedProperty rendererProp = targetRenderers.GetArrayElementAtIndex(index);
        Renderer rend = rendererProp.objectReferenceValue as Renderer;
        if (rend == null) return new string[] { "No Renderer" };

        int count = rend.sharedMaterials.Length;
        string[] options = new string[count];
        for (int i = 0; i < count; i++)
        {
            options[i] = i.ToString();
        }
        return options.Length > 0 ? options : new string[] { "No Materials" };
    }
}
}