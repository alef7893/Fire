using UnityEngine;

public enum FenceBoundarySide
{
    South,
    North,
    West,
    East
}

public sealed class FenceBoundaryGenerator : MonoBehaviour
{
    [Header("Modular Pieces")]
    public GameObject[] fencePrefabs;
    public GameObject gatePrefab;

    [Header("Approximate Size")]
    [Min(2)] public int modulesAlongX = 18;
    [Min(2)] public int modulesAlongZ = 18;
    [Min(0.1f)] public float moduleSpacing = 5f;
    [Min(0f)] public float cornerClosureOffset = 2.5f;
    public bool placeGate = true;
    public FenceBoundarySide gateSide = FenceBoundarySide.South;
    [Min(0)] public int gateStartModule;
    [Min(1)] public int gateModuleSpan = 1;

    [Header("Placement")]
    public Terrain targetTerrain;
    public float verticalOffset;
    public bool disablePrefabColliders = true;

    [Header("Invisible Boundary")]
    public bool createInvisibleBoundary = true;
    [Min(0.1f)] public float boundaryHeight = 4f;
    [Min(0.05f)] public float boundaryThickness = 0.5f;

    public float ApproximateWidth => modulesAlongX * moduleSpacing;
    public float ApproximateDepth => modulesAlongZ * moduleSpacing;
}
