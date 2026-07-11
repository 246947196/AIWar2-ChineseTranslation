using System;
using System.IO;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorldTMPFontPatch;

[BepInPlugin("aiwar2.chinesetranslation.worldtmpfont", "World TMP Font Patch", "1.0.0")]
public class WorldTMPFontPatchPlugin : BaseUnityPlugin
{
    private static TMP_FontAsset cachedTMPFont;
    private static Font cachedLegacyFont;
    private static bool fontInitialized;

    private void Awake()
    {
        Harmony harmony = new Harmony("aiwar2.chinesetranslation.worldtmpfont");
        harmony.PatchAll(typeof(WorldTMPFontPatchPlugin));
        Logger.LogInfo("World TMP Font Patch loaded");
        EnsureFonts();
    }

    private static void EnsureFonts()
    {
        if (fontInitialized)
            return;
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
                return;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(fontPath);
            if (bundle == null)
                return;

            cachedLegacyFont = bundle.LoadAsset<Font>(fontName);
            cachedTMPFont = bundle.LoadAsset<TMP_FontAsset>($"{fontName} SDF");
            if (cachedTMPFont == null)
            {
                TMP_FontAsset[] all = bundle.LoadAllAssets<TMP_FontAsset>();
                if (all != null && all.Length > 0)
                    cachedTMPFont = all[0];
            }

            if (cachedTMPFont != null)
                Debug.Log($"[WorldTMPFontPatch] Loaded TMP font: {cachedTMPFont.name}");
            if (cachedLegacyFont != null)
                Debug.Log($"[WorldTMPFontPatch] Loaded legacy font: {cachedLegacyFont.name}");

            bundle.Unload(false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WorldTMPFontPatch] Error: {ex.Message}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TextMeshPro), "OnEnable")]
    private static void OnTextMeshProEnable(TextMeshPro __instance)
    {
        if (cachedTMPFont == null) return;
        if (__instance.font == cachedTMPFont) return;
        __instance.font = cachedTMPFont;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TextMeshPro), "InternalUpdate")]
    private static void OnTextMeshProInternalUpdate(TextMeshPro __instance)
    {
        if (cachedTMPFont == null) return;
        if (__instance.font == cachedTMPFont) return;
        __instance.font = cachedTMPFont;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
    private static void OnTextMeshProUGUIEnable(TextMeshProUGUI __instance)
    {
        if (cachedTMPFont == null) return;
        if (__instance.font == cachedTMPFont) return;
        __instance.font = cachedTMPFont;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TextMeshProUGUI), "InternalUpdate")]
    private static void OnTextMeshProUGUIInternalUpdate(TextMeshProUGUI __instance)
    {
        if (cachedTMPFont == null) return;
        if (__instance.font == cachedTMPFont) return;
        __instance.font = cachedTMPFont;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Text), "OnEnable")]
    private static void OnLegacyTextEnable(Text __instance)
    {
        if (cachedLegacyFont == null) return;
        if (__instance.font == cachedLegacyFont) return;
        __instance.font = cachedLegacyFont;
    }
}
