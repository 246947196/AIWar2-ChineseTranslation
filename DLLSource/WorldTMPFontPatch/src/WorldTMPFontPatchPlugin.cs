using System;
using System.IO;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace WorldTMPFontPatch;

[BepInPlugin("aiwar2.chinesetranslation.worldtmpfont", "World TMP Font Patch", "1.0.0")]
public class WorldTMPFontPatchPlugin : BaseUnityPlugin
{
    private static TMP_FontAsset cachedFont;
    private static bool fontInitialized;

    private void Awake()
    {
        Harmony.CreateAndPatchAll(typeof(WorldTMPFontPatchPlugin));
        Logger.LogInfo("World TMP Font Patch loaded");
    }

    private static TMP_FontAsset GetFont()
    {
        if (fontInitialized)
            return cachedFont;

        fontInitialized = true;

        try
        {
            string fontName = "mi_sans";
            string configPath = Path.Combine(Paths.ConfigPath, "xiaoye97.I18NFont4UnityGame.cfg");
            if (File.Exists(configPath))
            {
                foreach (string line in File.ReadAllLines(configPath))
                {
                    string t = line.Trim();
                    if (t.StartsWith("FontName", StringComparison.OrdinalIgnoreCase))
                    {
                        int eq = t.IndexOf('=');
                        if (eq > 0)
                            fontName = t.Substring(eq + 1).Trim();
                        break;
                    }
                }
            }

            string fontPath = Path.Combine(Paths.PluginPath, "I18NFont4UnityGame", fontName);
            if (!File.Exists(fontPath))
            {
                Debug.LogWarning($"[WorldTMPFontPatch] Font not found: {fontPath}");
                return null;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(fontPath);
            if (bundle == null)
            {
                Debug.LogWarning($"[WorldTMPFontPatch] Failed to load AssetBundle: {fontPath}");
                return null;
            }

            cachedFont = bundle.LoadAsset<TMP_FontAsset>($"{fontName} SDF");
            if (cachedFont == null)
            {
                TMP_FontAsset[] all = bundle.LoadAllAssets<TMP_FontAsset>();
                if (all != null && all.Length > 0)
                    cachedFont = all[0];
            }

            bundle.Unload(false);

            if (cachedFont != null)
                Debug.Log($"[WorldTMPFontPatch] Loaded TMP font: {cachedFont.name}");
            else
                Debug.LogWarning($"[WorldTMPFontPatch] No TMP_FontAsset found in {fontPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WorldTMPFontPatch] Error: {ex.Message}");
        }

        return cachedFont;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TextMeshPro), "OnEnable")]
    private static void OnTextMeshProEnable(TextMeshPro __instance)
    {
        TMP_FontAsset font = GetFont();
        if (font != null)
            __instance.font = font;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TextMeshPro), "InternalUpdate")]
    private static void OnTextMeshProInternalUpdate(TextMeshPro __instance)
    {
        if (!fontInitialized)
            return;
        if (cachedFont == null)
            return;
        if (__instance.font == cachedFont)
            return;

        __instance.font = cachedFont;
    }
}
