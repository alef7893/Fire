using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FenceBoundaryGenerator))]
public sealed class FenceBoundaryGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FenceBoundaryGenerator generator = (FenceBoundaryGenerator)target;
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            $"Approximate enclosed area: {generator.ApproximateWidth:0.#} x {generator.ApproximateDepth:0.#} units.",
            MessageType.Info);

        if (GUILayout.Button("Generate Fence"))
        {
            FenceBoundaryGeneratorUtility.Generate(generator);
        }

        if (GUILayout.Button("Clear Generated Fence"))
        {
            FenceBoundaryGeneratorUtility.Clear(generator);
        }
    }
}
