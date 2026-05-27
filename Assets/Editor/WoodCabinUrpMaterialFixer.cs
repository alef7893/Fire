using UnityEditor;
using UnityEngine;

public static class WoodCabinUrpMaterialFixer
{
    private const string PackageRoot = "Assets/ImportedPackages/the_wood_cabin";
    private const string UrpLitShaderName = "Universal Render Pipeline/Lit";

    [MenuItem("Tools/Wood Cabin/Fix Materials For URP")]
    public static void FixMaterialsForUrp()
    {
        Shader urpLit = Shader.Find(UrpLitShaderName);
        if (urpLit == null)
        {
            Debug.LogError($"Could not find shader '{UrpLitShaderName}'. Check that URP is installed.");
            return;
        }

        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { PackageRoot });
        int convertedCount = 0;

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                continue;
            }

            MaterialSnapshot snapshot = MaterialSnapshot.Capture(material);
            material.shader = urpLit;
            ApplySnapshot(material, snapshot);

            if (IsGlassMaterial(material.name))
            {
                ConfigureTransparentMaterial(material, snapshot.color);
            }
            else
            {
                ConfigureOpaqueMaterial(material);
            }

            EditorUtility.SetDirty(material);
            convertedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Converted {convertedCount} Wood Cabin materials to URP/Lit.");
    }

    private static void ApplySnapshot(Material material, MaterialSnapshot snapshot)
    {
        SetTexture(material, "_BaseMap", snapshot.mainTexture);
        SetTexture(material, "_MainTex", snapshot.mainTexture);
        SetTexture(material, "_BumpMap", snapshot.normalMap);
        SetTexture(material, "_MetallicGlossMap", snapshot.metallicMap);
        SetTexture(material, "_EmissionMap", snapshot.emissionMap);

        SetColor(material, "_BaseColor", snapshot.color);
        SetColor(material, "_Color", snapshot.color);

        SetFloat(material, "_Metallic", snapshot.metallic);
        SetFloat(material, "_Smoothness", snapshot.smoothness);
        SetFloat(material, "_BumpScale", snapshot.bumpScale);
    }

    private static void ConfigureOpaqueMaterial(Material material)
    {
        SetFloat(material, "_Surface", 0.0f);
        SetFloat(material, "_Blend", 0.0f);
        SetFloat(material, "_AlphaClip", 0.0f);
        material.renderQueue = -1;
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }

    private static void ConfigureTransparentMaterial(Material material, Color color)
    {
        color.a = Mathf.Min(color.a, 0.45f);
        SetColor(material, "_BaseColor", color);
        SetColor(material, "_Color", color);
        SetFloat(material, "_Surface", 1.0f);
        SetFloat(material, "_Blend", 0.0f);
        SetFloat(material, "_AlphaClip", 0.0f);
        SetFloat(material, "_Smoothness", 0.75f);
        material.renderQueue = 3000;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }

    private static bool IsGlassMaterial(string materialName)
    {
        return materialName.ToLowerInvariant().Contains("glass");
    }

    private static void SetTexture(Material material, string propertyName, Texture texture)
    {
        if (texture != null && material.HasProperty(propertyName))
        {
            material.SetTexture(propertyName, texture);
        }
    }

    private static void SetColor(Material material, string propertyName, Color color)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, color);
        }
    }

    private static void SetFloat(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private struct MaterialSnapshot
    {
        public Texture mainTexture;
        public Texture normalMap;
        public Texture metallicMap;
        public Texture emissionMap;
        public Color color;
        public float metallic;
        public float smoothness;
        public float bumpScale;

        public static MaterialSnapshot Capture(Material material)
        {
            return new MaterialSnapshot
            {
                mainTexture = GetTexture(material, "_MainTex"),
                normalMap = GetTexture(material, "_BumpMap"),
                metallicMap = GetTexture(material, "_MetallicGlossMap"),
                emissionMap = GetTexture(material, "_EmissionMap"),
                color = GetColor(material, "_Color", Color.white),
                metallic = GetFloat(material, "_Metallic", 0.0f),
                smoothness = GetFloat(material, "_Glossiness", 0.25f),
                bumpScale = GetFloat(material, "_BumpScale", 1.0f)
            };
        }

        private static Texture GetTexture(Material material, string propertyName)
        {
            return material.HasProperty(propertyName) ? material.GetTexture(propertyName) : null;
        }

        private static Color GetColor(Material material, string propertyName, Color fallback)
        {
            return material.HasProperty(propertyName) ? material.GetColor(propertyName) : fallback;
        }

        private static float GetFloat(Material material, string propertyName, float fallback)
        {
            return material.HasProperty(propertyName) ? material.GetFloat(propertyName) : fallback;
        }
    }
}
