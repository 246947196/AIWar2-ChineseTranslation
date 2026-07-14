using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class MiSansFontBuilder
{
    const string TTF_PATH = "Assets/Fonts/mi_sans.ttf";
    const string GLYPH_DATA_PATH = "Assets/Fonts/mi_sans_glyph_data.json";
    const string FONT_ASSET_PATH = "Assets/Fonts/mi_sans SDF.asset";
    const string BUNDLE_NAME = "mi_sans";

    [MenuItem("Chinese Translation/Step 1: Generate Mi Sans SDF Font Asset")]
    public static void GenerateFontAsset()
    {
        Font miSans = AssetDatabase.LoadAssetAtPath<Font>(TTF_PATH);
        if (miSans == null)
        {
            Debug.LogError($"Mi Sans TTF not found at {TTF_PATH}. Import mi_sans.ttf first.");
            return;
        }

        UnityEngine.Debug.Log($"Loaded font: {miSans.name}");

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            miSans,
            90,
            5,
            GlyphRenderMode.SDFAA,
            4096,
            4096,
            AtlasPopulationMode.Dynamic
        );

        if (fontAsset == null)
        {
            Debug.LogError("Failed to create TMP_FontAsset");
            return;
        }

        fontAsset.name = "mi_sans SDF";
        UnityEngine.Debug.Log($"Created font asset: {fontAsset.name}");

        string dir = Path.GetDirectoryName(FONT_ASSET_PATH);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        AssetDatabase.CreateAsset(fontAsset, FONT_ASSET_PATH);
        AssetDatabase.SaveAssets();

        uint[] unicodes = LoadCharacterSet();
        UnityEngine.Debug.Log($"Adding {unicodes.Length} characters to font...");

        uint[] missing;
        bool allAdded = fontAsset.TryAddCharacters(unicodes, out missing, true);

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();

        UnityEngine.Debug.Log($"Added {unicodes.Length - (missing?.Length ?? 0)}/{unicodes.Length} characters. Missing: {missing?.Length ?? 0}");
        if (missing != null && missing.Length > 0)
        {
            UnityEngine.Debug.LogWarning($"Missing {missing.Length} characters (not in font file):");
            for (int i = 0; i < Mathf.Min(10, missing.Length); i++)
                UnityEngine.Debug.Log($"  U+{missing[i]:X4}");
        }

        UnityEngine.Debug.Log($"Font asset saved to {FONT_ASSET_PATH}");
    }

    [MenuItem("Chinese Translation/Step 2: Build Mi Sans AssetBundle")]
    public static void BuildFontBundle()
    {
        string assetPath = FONT_ASSET_PATH;
        if (!File.Exists(assetPath))
        {
            Debug.LogError($"Font asset not found at {assetPath}. Run Step 1 first.");
            return;
        }

        AssetImporter importer = AssetImporter.GetAtPath(assetPath);
        if (importer == null)
        {
            Debug.LogError($"Cannot get AssetImporter for {assetPath}");
            return;
        }

        importer.assetBundleName = BUNDLE_NAME;
        importer.SaveAndReimport();

        string outputDir = Path.GetFullPath("../Bundles");
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        AssetBundleBuild[] builds = new AssetBundleBuild[]
        {
            new AssetBundleBuild
            {
                assetBundleName = BUNDLE_NAME,
                assetNames = new string[] { assetPath }
            }
        };

        BuildPipeline.BuildAssetBundles(
            outputDir,
            builds,
            BuildAssetBundleOptions.UncompressedAssetBundle | BuildAssetBundleOptions.ForceRebuildAssetBundle,
            BuildTarget.StandaloneWindows
        );

        AssetDatabase.Refresh();

        string bundleFile = Path.Combine(outputDir, BUNDLE_NAME);
        if (File.Exists(bundleFile))
        {
            long size = new FileInfo(bundleFile).Length;
            UnityEngine.Debug.Log($"AssetBundle built: {bundleFile} ({size / 1024} KB)");
        }
        else
        {
            Debug.LogError($"Failed to build AssetBundle at {bundleFile}");
        }

        string deployDir = Path.GetFullPath("../BepInEx/plugins/I18NFont4UnityGame");
        if (Directory.Exists(deployDir))
        {
            string dest = Path.Combine(deployDir, BUNDLE_NAME);
            File.Copy(bundleFile, dest, true);
            File.Copy(bundleFile + ".manifest", dest + ".manifest", true);
            UnityEngine.Debug.Log($"Deployed bundle to {dest}");
        }
    }

    [MenuItem("Chinese Translation/Build All (Step 1 + 2)")]
    public static void BuildAll()
    {
        GenerateFontAsset();
        BuildFontBundle();
    }

    static uint[] LoadCharacterSet()
    {
        string jsonPath = Path.Combine(Application.dataPath, "../" + GLYPH_DATA_PATH);
        if (File.Exists(GLYPH_DATA_PATH))
        {
            try
            {
                string json = File.ReadAllText(GLYPH_DATA_PATH);
                var data = JsonUtility.FromJson<GlyphDataFile>(json);
                if (data?.glyphs != null && data.glyphs.Count > 0)
                {
                    var chars = data.glyphs.Select(g => (uint)g.unicode).Distinct().ToList();
                    UnityEngine.Debug.Log($"Loaded {chars.Count} unique characters from glyph data");
                    return chars.ToArray();
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"Failed to parse glyph data: {e.Message}, using built-in set");
            }
        }

        return GenerateStandardCharacterSet();
    }

    static uint[] GenerateStandardCharacterSet()
    {
        var chars = new List<uint>();

        for (uint i = 0x20; i <= 0x7E; i++) chars.Add(i);
        for (uint i = 0xA0; i <= 0xFF; i++) chars.Add(i);
        for (uint i = 0x4E00; i <= 0x9FFF; i++) chars.Add(i);

        return chars.Distinct().ToArray();
    }

    [System.Serializable]
    class GlyphEntry
    {
        public int unicode;
    }

    [System.Serializable]
    class GlyphDataFile
    {
        public List<GlyphEntry> glyphs;
    }
}
