using UnityEditor;
using UnityEngine;

public static class LowPolyRockPackUrpMaterialFixer
{
    private const string PackageRoot = "Assets/ImportedPackages/LowPolyRockPack";
    private const string UrpLitShaderName = "Universal Render Pipeline/Lit";

    [MenuItem("Tools/Low Poly Rock Pack/Fix Materials For URP")]
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

            Texture mainTexture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;

            material.shader = urpLit;

            if (material.HasProperty("_BaseMap") && mainTexture != null)
            {
                material.SetTexture("_BaseMap", mainTexture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0.0f);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.15f);
            }

            EditorUtility.SetDirty(material);
            convertedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Converted {convertedCount} LowPolyRockPack materials to URP/Lit.");
    }
}
