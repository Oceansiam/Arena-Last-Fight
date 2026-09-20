using UnityEditor;
using UnityEngine;

public static class QuickURPMaterialFixer
{
    [MenuItem("Tools/Rift Arena/Fix Pink Materials (Built-in to URP)")]
    private static void FixPinkMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("找不到 Universal Render Pipeline/Lit shader，確認專案已安裝 URP 套件。");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int fixedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null) continue;

            bool isBuiltIn = mat.shader.name == "Standard"
                || mat.shader.name == "Standard (Specular setup)"
                || mat.shader.name.StartsWith("Legacy Shaders/")
                || mat.shader.name == "Diffuse";

            if (!isBuiltIn) continue;

            Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            Texture bumpTex = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
            float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
            float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;

            Undo.RecordObject(mat, "Convert to URP Lit");
            mat.shader = urpLit;

            if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
            mat.SetColor("_BaseColor", color);
            if (bumpTex != null) mat.SetTexture("_BumpMap", bumpTex);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);

            EditorUtility.SetDirty(mat);
            fixedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"完成，共轉換 {fixedCount} 個材質成 URP Lit。");
    }
}
