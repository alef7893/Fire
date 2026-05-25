using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FireGraphBuilder : EditorWindow
{
    private static bool drawSceneEdges = true;
    private bool includeInactiveNodes = true;

    [MenuItem("Tools/Fire Simulation/Validate Runtime Fire Graph")]
    public static void ShowWindow()
    {
        GetWindow<FireGraphBuilder>("Fire Graph");
        SceneView.duringSceneGui -= OnSceneGUIStatic;
        SceneView.duringSceneGui += OnSceneGUIStatic;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUIStatic;
    }

    private void OnGUI()
    {
        includeInactiveNodes = EditorGUILayout.Toggle("Include Inactive Nodes", includeInactiveNodes);
        drawSceneEdges = EditorGUILayout.Toggle("Show Scene Edges", drawSceneEdges);

        if (GUILayout.Button("Validate Graph"))
        {
            ValidateGraph(includeInactiveNodes);
        }

        if (GUILayout.Button("Refresh Scene View"))
        {
            SceneView.RepaintAll();
        }
    }

    private static void OnSceneGUIStatic(SceneView view)
    {
        if (!drawSceneEdges)
        {
            return;
        }

        FireEdge[] fireEdges = Object.FindObjectsOfType<FireEdge>(true);
        foreach (FireEdge edge in fireEdges)
        {
            if (edge == null || !edge.showGizmo || !edge.IsValid())
            {
                continue;
            }

            Handles.color = edge.edgeColor;
            Handles.DrawLine(edge.source.transform.position, edge.target.transform.position);
        }
    }

    private static void ValidateGraph(bool includeInactive)
    {
        FireNode[] fireObjects = Object.FindObjectsOfType<FireNode>(includeInactive);
        FireEdge[] fireEdges = Object.FindObjectsOfType<FireEdge>(includeInactive);
        HashSet<string> ids = new HashSet<string>();
        int explicitEdgeCount = 0;
        int invalidEdgeCount = 0;
        int duplicatedIds = 0;

        foreach (FireEdge edge in fireEdges)
        {
            if (edge != null && edge.IsValid())
            {
                explicitEdgeCount++;
            }
            else
            {
                invalidEdgeCount++;
                Debug.LogWarning($"Invalid fire edge: {edge?.gameObject.name}", edge);
            }
        }

        foreach (FireNode fireNode in fireObjects)
        {
            string id = fireNode.gameObject.name;
            if (!ids.Add(id))
            {
                duplicatedIds++;
                Debug.LogWarning($"Duplicated fire node id: {id}", fireNode);
            }

        }

        Debug.Log($"Runtime fire graph validation: {fireObjects.Length} nodes, {explicitEdgeCount} explicit edges, {invalidEdgeCount} invalid edges, {duplicatedIds} duplicated ids.");
    }
}
