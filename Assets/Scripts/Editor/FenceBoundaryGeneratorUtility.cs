using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FenceBoundaryGeneratorUtility
{
    private const string GeneratedRootName = "GeneratedFence";
    private const string VisualRootName = "VisualFenceModules";
    private const string BoundaryRootName = "InvisibleBoundary";

    public static void Generate(FenceBoundaryGenerator generator)
    {
        if (generator == null)
        {
            throw new ArgumentNullException(nameof(generator));
        }

        if (generator.fencePrefabs == null || generator.fencePrefabs.Length == 0)
        {
            throw new InvalidOperationException("Assign at least one modular fence prefab.");
        }

        Clear(generator);

        Transform generatedRoot = CreateChild(generator.transform, GeneratedRootName);
        Transform visualRoot = CreateChild(generatedRoot, VisualRootName);

        float halfWidth = generator.ApproximateWidth * 0.5f;
        float halfDepth = generator.ApproximateDepth * 0.5f;

        for (int index = 0; index < generator.modulesAlongX; index++)
        {
            float x = GetModulePosition(
                index,
                generator.modulesAlongX,
                halfWidth,
                generator.moduleSpacing,
                generator.cornerClosureOffset);

            PlaceSideModule(
                generator,
                visualRoot,
                FenceBoundarySide.South,
                index,
                generator.modulesAlongX,
                new Vector3(x, 0f, -halfDepth),
                0f,
                index);
            PlaceSideModule(
                generator,
                visualRoot,
                FenceBoundarySide.North,
                index,
                generator.modulesAlongX,
                new Vector3(x, 0f, halfDepth),
                180f,
                index + 1);
        }

        for (int index = 0; index < generator.modulesAlongZ; index++)
        {
            float z = GetModulePosition(
                index,
                generator.modulesAlongZ,
                halfDepth,
                generator.moduleSpacing,
                generator.cornerClosureOffset);
            PlaceSideModule(
                generator,
                visualRoot,
                FenceBoundarySide.West,
                index,
                generator.modulesAlongZ,
                new Vector3(-halfWidth, 0f, z),
                90f,
                index + 2);
            PlaceSideModule(
                generator,
                visualRoot,
                FenceBoundarySide.East,
                index,
                generator.modulesAlongZ,
                new Vector3(halfWidth, 0f, z),
                -90f,
                index + 3);
        }

        if (generator.createInvisibleBoundary)
        {
            CreateInvisibleBoundary(generator, generatedRoot, halfWidth, halfDepth);
        }

        EditorUtility.SetDirty(generator);
        EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
    }

    private static void PlaceSideModule(
        FenceBoundaryGenerator generator,
        Transform parent,
        FenceBoundarySide side,
        int index,
        int moduleCount,
        Vector3 localPosition,
        float localYaw,
        int prefabVariantIndex)
    {
        int gateStart = Mathf.Clamp(generator.gateStartModule, 0, moduleCount - 1);
        int gateSpan = Mathf.Clamp(generator.gateModuleSpan, 1, moduleCount - gateStart);
        bool isGateSide = generator.placeGate && generator.gatePrefab != null && generator.gateSide == side;
        bool insideGateSpan = isGateSide && index >= gateStart && index < gateStart + gateSpan;

        if (!insideGateSpan)
        {
            PlaceModule(
                generator,
                parent,
                SelectFencePrefab(generator, prefabVariantIndex),
                localPosition,
                localYaw);
            return;
        }

        if (index != gateStart)
        {
            return;
        }

        int gateEnd = gateStart + gateSpan - 1;
        float firstPosition = GetModulePosition(
            gateStart,
            moduleCount,
            side == FenceBoundarySide.South || side == FenceBoundarySide.North
                ? generator.ApproximateWidth * 0.5f
                : generator.ApproximateDepth * 0.5f,
            generator.moduleSpacing,
            generator.cornerClosureOffset);
        float lastPosition = GetModulePosition(
            gateEnd,
            moduleCount,
            side == FenceBoundarySide.South || side == FenceBoundarySide.North
                ? generator.ApproximateWidth * 0.5f
                : generator.ApproximateDepth * 0.5f,
            generator.moduleSpacing,
            generator.cornerClosureOffset);
        float gateCenter = (firstPosition + lastPosition) * 0.5f;

        if (side == FenceBoundarySide.South || side == FenceBoundarySide.North)
        {
            localPosition.x = gateCenter;
        }
        else
        {
            localPosition.z = gateCenter;
        }

        PlaceModule(generator, parent, generator.gatePrefab, localPosition, localYaw, true);
    }

    private static float GetModulePosition(
        int index,
        int moduleCount,
        float halfExtent,
        float moduleSpacing,
        float cornerClosureOffset)
    {
        float basePosition = -halfExtent + moduleSpacing * (index + 0.5f);
        float normalizedPosition = moduleCount <= 1 ? 0.5f : index / (moduleCount - 1f);
        float closureAdjustment = Mathf.Lerp(-cornerClosureOffset, cornerClosureOffset, normalizedPosition);
        return basePosition + closureAdjustment;
    }

    public static void Clear(FenceBoundaryGenerator generator)
    {
        if (generator == null)
        {
            return;
        }

        Transform existing = generator.transform.Find(GeneratedRootName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }
    }

    private static GameObject SelectFencePrefab(FenceBoundaryGenerator generator, int index)
    {
        GameObject prefab = generator.fencePrefabs[Math.Abs(index) % generator.fencePrefabs.Length];
        if (prefab == null)
        {
            throw new InvalidOperationException("The modular fence prefab list contains an empty reference.");
        }

        return prefab;
    }

    private static void PlaceModule(
        FenceBoundaryGenerator generator,
        Transform parent,
        GameObject prefab,
        Vector3 localPosition,
        float localYaw,
        bool isGate = false)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, generator.gameObject.scene);
        Undo.RegisterCreatedObjectUndo(instance, "Generate modular fence");
        instance.name = isGate ? "GeneratedGate" : $"FenceModule_{parent.childCount:000}";
        instance.transform.SetParent(parent, false);

        Vector3 worldPosition = generator.transform.TransformPoint(localPosition);
        worldPosition.y = SampleHeight(generator, worldPosition) + generator.verticalOffset;
        instance.transform.position = worldPosition;
        instance.transform.rotation = generator.transform.rotation * Quaternion.Euler(0f, localYaw, 0f);

        if (generator.disablePrefabColliders)
        {
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
        }
    }

    private static float SampleHeight(FenceBoundaryGenerator generator, Vector3 worldPosition)
    {
        if (generator.targetTerrain == null)
        {
            return generator.transform.position.y;
        }

        return generator.targetTerrain.SampleHeight(worldPosition) + generator.targetTerrain.transform.position.y;
    }

    private static void CreateInvisibleBoundary(
        FenceBoundaryGenerator generator,
        Transform generatedRoot,
        float halfWidth,
        float halfDepth)
    {
        Transform boundaryRoot = CreateChild(generatedRoot, BoundaryRootName);
        float centerY = generator.boundaryHeight * 0.5f;

        CreateBoundaryWall(
            boundaryRoot,
            "Boundary_South",
            new Vector3(0f, centerY, -halfDepth),
            new Vector3(generator.ApproximateWidth, generator.boundaryHeight, generator.boundaryThickness));
        CreateBoundaryWall(
            boundaryRoot,
            "Boundary_North",
            new Vector3(0f, centerY, halfDepth),
            new Vector3(generator.ApproximateWidth, generator.boundaryHeight, generator.boundaryThickness));
        CreateBoundaryWall(
            boundaryRoot,
            "Boundary_West",
            new Vector3(-halfWidth, centerY, 0f),
            new Vector3(generator.boundaryThickness, generator.boundaryHeight, generator.ApproximateDepth));
        CreateBoundaryWall(
            boundaryRoot,
            "Boundary_East",
            new Vector3(halfWidth, centerY, 0f),
            new Vector3(generator.boundaryThickness, generator.boundaryHeight, generator.ApproximateDepth));
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(child, "Generate modular fence");
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static void CreateBoundaryWall(Transform parent, string name, Vector3 localPosition, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(wall, "Generate invisible boundary");
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = localPosition;
        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;
    }
}
