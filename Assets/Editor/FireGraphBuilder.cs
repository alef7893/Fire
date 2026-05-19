using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class FireGraphBuilder : EditorWindow
{
    private float connectionThreshold = 10.0f;

    [MenuItem("Tools/Build Fire Graph")]
    public static void ShowWindow()
    {
        GetWindow<FireGraphBuilder>("Fire Graph Builder");
        SceneView.duringSceneGui -= OnSceneGUIStatic;
        SceneView.duringSceneGui += OnSceneGUIStatic;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUIStatic;
    }

    private void OnGUI()
    {
        connectionThreshold = EditorGUILayout.FloatField("Max Connection Distance", connectionThreshold);

        if (GUILayout.Button("Generate Fire Graph"))
        {
            GenerateGraph();
            SceneView.RepaintAll(); // Immediately reflect changes
        }

        if (GUILayout.Button("Refresh Visualization"))
        {
            SceneView.RepaintAll();
        }
    }

    private static void OnSceneGUIStatic(SceneView view)
    {
        TextAsset json = Resources.Load<TextAsset>("fire_graph");
        if (json == null) return;

        FireGraph graph = JsonUtility.FromJson<FireGraph>(json.text);
        if (graph == null || graph.nodes == null)
        {
            return;
        }

        Dictionary<string, FireGraphNode> node_lookup = new Dictionary<string, FireGraphNode>();

        foreach (var node in graph.nodes)
        {
            node_lookup[node.id] = node;
        }

        Handles.color = Color.yellow;

        foreach (var node in graph.nodes)
        {
            Handles.SphereHandleCap(0, node.position, Quaternion.identity, 0.3f, EventType.Repaint);
            foreach (var edge in node.edges)
            {
                if (node_lookup.TryGetValue(edge.targetId, out FireGraphNode n_node))
                {
                    Handles.DrawLine(node.position, n_node.position);
                }
            }
        }
    }

    void GenerateGraph()
    {
        FireObject[] fire_objects = GameObject.FindObjectsOfType<FireObject>();
        FireGraph graph = new FireGraph();

        foreach (FireObject fireobject in fire_objects)
        {
            FireGraphNode node = new FireGraphNode(fireobject.gameObject.name, fireobject.transform.position);
            graph.nodes.Add(node);
        }

        foreach (var a in graph.nodes)
        {
            foreach (var b in graph.nodes)
            {
                if (a == b) continue;

                float dist = Vector3.Distance(a.position, b.position);
                if (dist <= connectionThreshold)
                {
                    a.edges.Add(new FireGraphEdge(b.id, dist));
                }
            }
        }

        string json = JsonUtility.ToJson(graph, true);
        string resourcesPath = Path.Combine(Application.dataPath, "Resources");
        Directory.CreateDirectory(resourcesPath);

        string path = Path.Combine(resourcesPath, "fire_graph.json");
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();
        Debug.Log("Fire graph has been saved to /Resources/fire_graph.json!");
    }
}
