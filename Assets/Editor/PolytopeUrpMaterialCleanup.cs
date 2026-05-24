using UnityEditor;
using UnityEngine;

public static class PolytopeUrpMaterialCleanup
{
    private static readonly string[] MaterialPaths =
    {
        "Assets/ImportedPackages/Polytope Studio/Lowpoly_Demos/Environment_Free/Helpers/Plane mat.mat",
        "Assets/ImportedPackages/Polytope Studio/Lowpoly_Village/Sources/Materials/PT_mat.mat",
        "Assets/ImportedPackages/Polytope Studio/Lowpoly_Environments/Sources/Materials/PT_Terrain_mat.mat",
    };

    [MenuItem("Tools/Polytope Studio/Fix Remaining URP Materials")]
    public static void FixRemainingUrpMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Shader terrainLit = Shader.Find("Universal Render Pipeline/Terrain/Lit");

        if (urpLit == null)
        {
            Debug.LogError("Could not find Universal Render Pipeline/Lit.");
            return;
        }

        int convertedCount = 0;
        foreach (string path in MaterialPaths)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Debug.LogWarning($"Material not found: {path}");
                continue;
            }

            Texture mainTexture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;

            bool isTerrainMaterial = path.EndsWith("PT_Terrain_mat.mat");
            material.shader = isTerrainMaterial && terrainLit != null ? terrainLit : urpLit;

            SetTextureIfPresent(material, "_BaseMap", mainTexture);
            SetTextureIfPresent(material, "_MainTex", mainTexture);
            SetColorIfPresent(material, "_BaseColor", color);
            SetColorIfPresent(material, "_Color", color);
            SetFloatIfPresent(material, "_Metallic", 0.0f);
            SetFloatIfPresent(material, "_Smoothness", 0.2f);

            EditorUtility.SetDirty(material);
            convertedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Converted {convertedCount} remaining Polytope materials to URP-compatible shaders.");
    }

    private static void SetTextureIfPresent(Material material, string propertyName, Texture texture)
    {
        if (texture != null && material.HasProperty(propertyName))
        {
            material.SetTexture(propertyName, texture);
        }
    }

    private static void SetColorIfPresent(Material material, string propertyName, Color color)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, color);
        }
    }

    private static void SetFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }
}
