using UnityEditor;
using UnityEngine;

public static class SupercyanUrpMaterialFixer
{
    private const string ForestPackageRoot = "Assets/ImportedPackages/Supercyan Free Forest Sample";
    private const string ItemPackRoot = "Assets/ImportedPackages/Supercyan";
    private const string UrpLitShaderName = "Universal Render Pipeline/Lit";

    [MenuItem("Tools/Supercyan/Convert Forest Materials To URP")]
    public static void ConvertForestMaterialsToUrp()
    {
        ConvertMaterialsToUrp(ForestPackageRoot, skipSkybox: true, "Supercyan forest");
    }

    [MenuItem("Tools/Supercyan/Convert Item Pack Materials To URP")]
    public static void ConvertItemPackMaterialsToUrp()
    {
        ConvertMaterialsToUrp(ItemPackRoot, skipSkybox: true, "Supercyan item pack");
    }

    private static void ConvertMaterialsToUrp(string packageRoot, bool skipSkybox, string logName)
    {
        Shader urpLit = Shader.Find(UrpLitShaderName);
        if (urpLit == null)
        {
            Debug.LogError($"Could not find shader '{UrpLitShaderName}'. Check that URP is installed.");
            return;
        }

        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { packageRoot });
        int convertedCount = 0;

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || (skipSkybox && path.Contains("/Skybox/")))
            {
                continue;
            }

            Texture mainTexture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            Color emissionColor = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
            float smoothness = material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : 0.0f;
            float metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0.0f;

            material.shader = urpLit;
            if (mainTexture != null && material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", mainTexture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", Mathf.Min(smoothness, 0.25f));
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (emissionColor.maxColorComponent > 0.0f && material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", emissionColor);
                material.EnableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(material);
            convertedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Converted {convertedCount} {logName} materials to URP/Lit.");
    }
}
