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

            Debug.Log($"[WorldTMPFontPatch] EnsureFonts: fontName={fontName}");

            string fontPath = Path.Combine(Paths.PluginPath, "I18NFont4UnityGame", fontName);
            if (!File.Exists(fontPath))
            {
                Debug.LogWarning($"[WorldTMPFontPatch] Font not found: {fontPath}");
                return;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(fontPath);
            if (bundle == null)
            {
                Debug.LogWarning($"[WorldTMPFontPatch] Failed to load bundle");
                return;
            }

            Debug.Log($"[WorldTMPFontPatch] Bundle loaded, size={new FileInfo(fontPath).Length}");

            // Load Font for legacy text components
            cachedLegacyFont = bundle.LoadAsset<Font>(fontName);
            Debug.Log($"[WorldTMPFontPatch] Bundle Font: {(cachedLegacyFont != null ? cachedLegacyFont.name : "NULL")}");

            // Load and repair the broken TMP_FontAsset
            cachedTMPFont = bundle.LoadAsset<TMP_FontAsset>($"{fontName} SDF");
            if (cachedTMPFont == null)
            {
                TMP_FontAsset[] all = bundle.LoadAllAssets<TMP_FontAsset>();
                if (all != null && all.Length > 0)
                    cachedTMPFont = all[0];
            }

            if (cachedTMPFont != null)
            {
                Debug.Log($"[WorldTMPFontPatch] Loaded TMP font: {cachedTMPFont.name}");

                // Force repair: set to dynamic mode via reflection
                var bf = typeof(TMP_FontAsset).GetField("m_AtlasPopulationMode", BindingFlags.NonPublic | BindingFlags.Instance);
                if (bf != null)
                {
                    bf.SetValue(cachedTMPFont, 1); // 1 = Dynamic
                    Debug.Log($"[WorldTMPFontPatch] Set atlas mode to Dynamic");
                }
                var mf = typeof(TMP_FontAsset).GetField("m_IsMultiAtlasTexturesEnabled", BindingFlags.NonPublic | BindingFlags.Instance);
                if (mf != null)
                    mf.SetValue(cachedTMPFont, true);

                cachedTMPFont.ReadFontAssetDefinition();
                Debug.Log($"[WorldTMPFontPatch] ReadFontAssetDefinition() called");
            }

            bundle.Unload(false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WorldTMPFontPatch] Error: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TMP_Text), "set_font")]
    private static void OnSetFont(TMP_Text __instance, TMP_FontAsset value)
    {
        EnsureFonts();
        if (cachedTMPFont == null)
            return;
        if (value == cachedTMPFont)
        {
            Debug.Log($"[WorldTMPFontPatch] TMP_Text.set_font SKIP (already CJK): {__instance.GetType().Name} '{__instance.name}' gameObject='{__instance.gameObject.name}' parent='{__instance.transform.parent?.name}'");
            return;
        }
        Debug.Log($"[WorldTMPFontPatch] TMP_Text.set_font REPLACE: {__instance.GetType().Name} '{__instance.name}' in '{__instance.gameObject.name}' (parent={__instance.transform.parent?.name}) font={value?.name}");
        __instance.font = cachedTMPFont;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Text), "set_font")]
    private static void OnLegacySetFont(Text __instance, Font value)
    {
        EnsureFonts();
        if (cachedLegacyFont == null)
            return;
        if (value == cachedLegacyFont)
        {
            Debug.Log($"[WorldTMPFontPatch] Text.set_font SKIP (already CJK): '{__instance.name}' parent='{__instance.transform.parent?.name}'");
            return;
        }
        Debug.Log($"[WorldTMPFontPatch] Text.set_font REPLACE: '{__instance.name}' in '{__instance.gameObject.name}' (parent={__instance.transform.parent?.name}) font={value?.name}");
        __instance.font = cachedLegacyFont;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TextMeshProUGUI), "Awake")]
    private static void OnTextMeshProUGUIAwake(TextMeshProUGUI __instance)
    {
        EnsureFonts();
        if (cachedTMPFont == null)
            return;
        if (__instance.font == cachedTMPFont)
            return;
        Debug.Log($"[WorldTMPFontPatch] TMPUGUI.Awake replace: '{__instance.name}' parent='{__instance.transform.parent?.name}'");
        __instance.font = cachedTMPFont;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
    private static void OnTextMeshProUGUIEnable(TextMeshProUGUI __instance)
    {
        EnsureFonts();
        if (cachedTMPFont == null)
            return;
        if (__instance.font == cachedTMPFont)
            return;
        Debug.Log($"[WorldTMPFontPatch] TMPUGUI.OnEnable replace: '{__instance.name}' parent='{__instance.transform.parent?.name}'");
        __instance.font = cachedTMPFont;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TextMeshProUGUI), "InternalUpdate")]
    private static void OnTextMeshProUGUIInternalUpdate(TextMeshProUGUI __instance)
    {
        if (cachedTMPFont == null)
            return;
        if (__instance.font == cachedTMPFont)
            return;
        Debug.Log($"[WorldTMPFontPatch] TMPUGUI.InternalUpdate catch: '{__instance.name}' parent='{__instance.transform.parent?.name}'");
        __instance.font = cachedTMPFont;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Text), "OnEnable")]
    private static void OnLegacyTextEnable(Text __instance)
    {
        EnsureFonts();
        if (cachedLegacyFont == null)
            return;
        if (__instance.font == cachedLegacyFont)
            return;
        Debug.Log($"[WorldTMPFontPatch] Text.OnEnable replace: '{__instance.name}' parent='{__instance.transform.parent?.name}'");
        __instance.font = cachedLegacyFont;
    }
}
