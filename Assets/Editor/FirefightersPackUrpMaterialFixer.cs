using UnityEditor;
using UnityEngine;

public static class FirefightersPackUrpMaterialFixer
{
    private const string PackageRoot = "Assets/ImportedPackages/FirefightersPack";
    private const string ExportsRoot = PackageRoot + "/Models/Exports";
    private const string MaterialPath = PackageRoot + "/Materials/Material.mat";
    private const string LightsMaterialPath = PackageRoot + "/Materials/Lights.mat";
    private const string PaletteTexturePath = PackageRoot + "/Textures/Palette-Gradient.png";
    private const string UrpLitShaderName = "Universal Render Pipeline/Lit";
    private const string UrpUnlitShaderName = "Universal Render Pipeline/Unlit";

    [MenuItem("Tools/Firefighters Pack/Fix Materials For URP")]
    public static void FixMaterialsForUrp()
    {
        Shader urpLit = Shader.Find(UrpLitShaderName);
        Shader urpUnlit = Shader.Find(UrpUnlitShaderName);
        if (urpLit == null)
        {
            Debug.LogError($"Could not find shader '{UrpLitShaderName}'. Check that URP is installed.");
            return;
        }

        Material mainMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Material lightsMaterial = AssetDatabase.LoadAssetAtPath<Material>(LightsMaterialPath);
        Texture paletteTexture = AssetDatabase.LoadAssetAtPath<Texture>(PaletteTexturePath);

        if (mainMaterial == null || lightsMaterial == null || paletteTexture == null)
        {
            Debug.LogError("FirefightersPack material fix failed. Missing Material.mat, Lights.mat, or Palette-Gradient.png.");
            return;
        }

        ConfigureMainMaterial(mainMaterial, urpLit, paletteTexture);
        ConfigureLightsMaterial(lightsMaterial, urpUnlit != null ? urpUnlit : urpLit);

        int remappedModels = RemapModelMaterials(mainMaterial, lightsMaterial);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"FirefightersPack materials converted to URP. Remapped {remappedModels} model importers.");
    }

    private static void ConfigureMainMaterial(Material material, Shader shader, Texture paletteTexture)
    {
        material.shader = shader;
        SetColor(material, Color.white);
        SetTexture(material, paletteTexture);
        SetFloatIfPresent(material, "_Smoothness", 0.0f);
        SetFloatIfPresent(material, "_Metallic", 0.0f);
        EditorUtility.SetDirty(material);
    }

    private static void ConfigureLightsMaterial(Material material, Shader shader)
    {
        Color lightColor = new Color(0.0f, 0.45842028f, 1.0f, 1.0f);
        material.shader = shader;
        SetColor(material, lightColor);
        SetEmission(material, lightColor * 1.5f);
        SetFloatIfPresent(material, "_Smoothness", 0.0f);
        SetFloatIfPresent(material, "_Metallic", 0.0f);
        EditorUtility.SetDirty(material);
    }

    private static int RemapModelMaterials(Material mainMaterial, Material lightsMaterial)
    {
        string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { ExportsRoot });
        int remappedCount = 0;

        foreach (string guid in modelGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                continue;
            }

            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.External;
            importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            importer.materialSearch = ModelImporterMaterialSearch.Local;

            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Material"), mainMaterial);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Lights"), lightsMaterial);
            importer.SaveAndReimport();
            remappedCount++;
        }

        return remappedCount;
    }

    private static void SetColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private static void SetTexture(Material material, Texture texture)
    {
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }
    }

    private static void SetEmission(Material material, Color color)
    {
        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color);
        }

        material.EnableKeyword("_EMISSION");
    }

    private static void SetFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }
}
