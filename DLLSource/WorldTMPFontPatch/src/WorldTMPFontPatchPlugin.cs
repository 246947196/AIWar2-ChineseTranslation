using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Arcen.Universal;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorldTMPFontPatch;

[BepInPlugin("aiwar2.chinesetranslation.worldtmpfont", "World TMP Font Patch", "1.0.0")]
public class WorldTMPFontPatchPlugin : BaseUnityPlugin
{
    private static Font cachedLegacyFont;
    private static TMP_FontAsset cachedTMPFont;
    private static bool fontInitialized;

    private static readonly object captureLock = new object();
    private static System.Collections.Generic.List<string> capturedMessages = new System.Collections.Generic.List<string>();
    private const int MaxCaptures = 500;
    private static string LogPath;
    private static int renderCounter = 0;

    private void Awake()
    {
        LogPath = Path.Combine(Paths.BepInExRootPath, "ChatLogRestore.txt");
        try { File.WriteAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] Plugin Awake\n"); }
        catch { }

        PatchCharMapping.Ensure();
        Harmony.CreateAndPatchAll(typeof(WorldTMPFontPatchPlugin));
        EnsureFonts();
    }

    private static void Log(string msg)
    {
        try { File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {msg}\n"); }
        catch { }
    }

    private static void EnsureFonts()
    {
        if (fontInitialized) return;
        fontInitialized = true;
        try
        {
            // Legacy font for overlay
            try
            {
                Font sysFont = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 14);
                if (sysFont != null) { cachedLegacyFont = sysFont; Log("Font: Microsoft YaHei"); }
            }
            catch { }

            // TMP font for world-space / canvas text (loading screen, map, planet names)
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
                        if (eq > 0) fontName = t.Substring(eq + 1).Trim();
                        break;
                    }
                }
            }

            string fontPath = Path.Combine(Paths.PluginPath, "I18NFont4UnityGame", fontName);
            if (!File.Exists(fontPath))
            {
                Debug.LogWarning($"[WorldTMPFontPatch] Font bundle not found: {fontPath}");
                return;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(fontPath);
            if (bundle == null) return;

            if (cachedLegacyFont == null)
            {
                Font bundleFont = bundle.LoadAsset<Font>(fontName);
                if (bundleFont != null) cachedLegacyFont = bundleFont;
            }

            cachedTMPFont = bundle.LoadAsset<TMP_FontAsset>($"{fontName} SDF");
            if (cachedTMPFont != null) Log($"TMP Font: {fontName} SDF loaded");
            else Debug.LogWarning($"[WorldTMPFontPatch] TMP_FontAsset '{fontName} SDF' not found in bundle");

            bundle.Unload(false);
        }
        catch (Exception ex) { Debug.LogError($"[WorldTMPFontPatch] Font error: {ex.Message}"); }
    }

    // ========== LAYER: Capture serialization ==========
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ArcenSerializationBuffer), "AddString_Condensed")]
    private static void OnAddStringCondensed(string Item, string FieldNameForErrors)
    {
        if (FieldNameForErrors != "RelatedString") return;
        if (Item == null || Item.Length < 3) return;

        // Check for non-ASCII
        bool hasNonAscii = false;
        int nonAsciiCount = 0;
        for (int i = 0; i < Item.Length; i++)
        {
            if (Item[i] > 127) { hasNonAscii = true; nonAsciiCount++; }
        }
        if (!hasNonAscii) return;

        lock (captureLock)
        {
            capturedMessages.Add(Item);
            if (capturedMessages.Count > MaxCaptures)
                capturedMessages.RemoveRange(0, capturedMessages.Count - MaxCaptures);
            Log($"SERIALIZE(CAPTURE) #{capturedMessages.Count} len={Item.Length} nonAscii={nonAsciiCount}: {Item}");
        }
    }

    // ========== Content matching ==========
    private static bool IsContentMatch(string original, string damaged)
    {
        if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(damaged)) return false;
        if (original.Length != damaged.Length) return false;
        bool hasDiff = false;
        for (int i = 0; i < original.Length; i++)
        {
            if (damaged[i] != original[i])
            {
                // Character differs: original must be non-ASCII (Chinese/punctuation that got mangled)
                if (original[i] <= 127) return false;
                hasDiff = true;
            }
        }
        return hasDiff;
    }

    private static string Truncate(string s, int max = 80)
    {
        if (s == null) return "null";
        return s.Length <= max ? s : s.Substring(0, max) + "...";
    }

    // ========== Rich text stripping ==========
    private static readonly Regex StripRichTextRegex = new Regex(
        @"</?(?:link|u|s|mark|size|voffset|cspace|mspace|sub|sup)[^>]*>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // ========== Target detection (matches ChatLog and OngoingMessage TMP components) ==========
    // OngoingMessage uses "BasicText" (NOT "BasicTextUnderlay" — that's the prefab name, not the instance)
    private static bool NeedsChineseOverlay(TMP_Text instance)
    {
        string name = instance.name;
        return name == "ChatLog" || name == "BasicText";
    }

    private static bool IsChatLogTarget(TMP_Text instance) => instance.name == "ChatLog";
    private static bool IsOngoingTarget(TMP_Text instance) => instance.name == "BasicText";

    // ========== Legacy Text font replacement ==========
    [HarmonyPostfix]
    [HarmonyPatch(typeof(UnityEngine.UI.Text), "OnEnable")]
    private static void OnLegacyTextEnable(UnityEngine.UI.Text __instance)
    {
        if (cachedLegacyFont == null) return;
        if (__instance.font == cachedLegacyFont) return;
        __instance.font = cachedLegacyFont;
    }

    // ========== TMP font replacement (world-space: planet names, loading screen) ==========
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TextMeshPro), "OnEnable")]
    private static void OnTextMeshProEnable(TextMeshPro __instance)
    {
        if (cachedTMPFont == null) return;
        if (__instance.font == cachedTMPFont) return;
        __instance.font = cachedTMPFont;
        __instance.UpdateFontAsset();
    }

    // ========== TMP font fallback (canvas UI, catches I18NFont4UnityGame timing gap) ==========
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TextMeshProUGUI), "InternalUpdate")]
    private static void OnTextMeshProUGUIInternalUpdate(TextMeshProUGUI __instance)
    {
        if (cachedTMPFont == null) return;
        if (__instance.font == cachedTMPFont) return;
        if (NeedsChineseOverlay(__instance)) return;
        __instance.font = cachedTMPFont;
        __instance.UpdateFontAsset();
    }

    // ========== ChatLog overlay (child of TMP, pixelation accepted) ==========
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
    private static void OnChatLogOverlay(TextMeshProUGUI __instance)
    {
        if (cachedLegacyFont == null) return;
        if (!NeedsChineseOverlay(__instance)) return;

        Transform existing = __instance.transform.Find("TMP_LegacyOverlay");
        if (existing != null) { Log("OVERLAY: already exists"); return; }

        __instance.color = new Color(0, 0, 0, 0);

        int fontSize = Math.Max(11, (int)(__instance.fontSize * 0.65f));

        var go = new GameObject("TMP_LegacyOverlay", typeof(RectTransform));
        go.transform.SetParent(__instance.transform, false);

        UnityEngine.UI.Text t = null;
        try { t = go.AddComponent<UnityEngine.UI.Text>(); }
        catch { t = go.GetComponent<UnityEngine.UI.Text>(); }
        if (t == null) { UnityEngine.Object.Destroy(go); Log("OVERLAY: AddComponent FAILED"); return; }
        t.font = cachedLegacyFont;
        t.fontSize = fontSize;
        t.lineSpacing = 0.9664f;
        t.alignment = TextAnchor.UpperLeft;
        t.supportRichText = true;
        t.color = Color.white;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.raycastTarget = false;

        var rt = t.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Log($"OVERLAY: created as child, fontSize={fontSize}");
    }

    // ========== Message type classification ==========
    private static string ClassifyEntry(string content)
    {
        if (content.Contains("#3BF6D7")) return "JOURNAL";
        if (content.Contains("#ffba36")) return "TIP";
        if (content.Contains("#d35800")) return "WARDEN";
        if (content.Contains("#ffba00")) return "PLAYER_CHAT";
        if (content.Contains("#")) return "COLORED";
        return "PLAIN";
    }

    // ========== Overlay creation helper ==========
    private static UnityEngine.UI.Text EnsureOverlay(TMP_Text tmp)
    {
        Transform existing = tmp.transform.Find("TMP_LegacyOverlay");
        UnityEngine.UI.Text legacy = existing?.GetComponent<UnityEngine.UI.Text>();
        if (legacy != null) return legacy;

        // Create on-demand
        tmp.color = new Color(0, 0, 0, 0);
        int fontSize = Math.Max(11, (int)(tmp.fontSize * 0.65f));

        var go = new GameObject("TMP_LegacyOverlay", typeof(RectTransform));
        go.transform.SetParent(tmp.transform, false);

        try { legacy = go.AddComponent<UnityEngine.UI.Text>(); }
        catch { legacy = go.GetComponent<UnityEngine.UI.Text>(); }
        if (legacy == null) { UnityEngine.Object.Destroy(go); return null; }

        legacy.font = cachedLegacyFont;
        legacy.fontSize = fontSize;
        legacy.lineSpacing = 0.9664f;
        legacy.alignment = TextAnchor.UpperLeft;
        legacy.supportRichText = true;
        legacy.color = Color.white;
        legacy.verticalOverflow = VerticalWrapMode.Overflow;
        legacy.horizontalOverflow = HorizontalWrapMode.Wrap;
        legacy.raycastTarget = false;

        var rt = legacy.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Log($"OVERLAY: on-demand created for '{tmp.name}', fontSize={fontSize}");
        return legacy;
    }

    // ========== LAYER: set_text display interception ==========
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TMP_Text), "set_text")]
    private static void OnTMPTextSetText(TMP_Text __instance, string value)
    {
        if (cachedLegacyFont == null) return;
        string text = value ?? "";
        bool hasDamage = text.Contains("_") || text.Contains("?");

        // Fast path: known targets
        if (NeedsChineseOverlay(__instance))
        {
            UnityEngine.UI.Text legacy = EnsureOverlay(__instance);
            if (legacy == null) return;

            renderCounter++;
            if (hasDamage)
            {
                string restored = IsChatLogTarget(__instance)
                    ? RestoreChatLogText(text) : RestoreOngoingText(text);
                if (restored == text)
                    Log($"RENDER#{renderCounter}: NO_RESTORE_NEEDED (or failed)");
                legacy.text = StripRichTextRegex.Replace(restored, "");
            }
            else
            {
                legacy.text = StripRichTextRegex.Replace(text, "");
            }
            return;
        }
    }

    private static readonly Regex EntryContentRegex = new Regex(
        @"<link=\d+><u>[^<]+</u>\s+(?<content>.*?)</link>",
        RegexOptions.Compiled | RegexOptions.Singleline);

    // OngoingMessage format: <link=N>content</link> (no <u> time prefix)
    private static readonly Regex OngoingEntryRegex = new Regex(
        @"<link=\d+>(?<content>.*?)</link>",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static string RestoreChatLogText(string fullText)
    {
        int matchCount = 0, underscoreCount = 0, restoredCount = 0;
        string result = EntryContentRegex.Replace(fullText, match =>
        {
            matchCount++;
            string rawContent = match.Groups["content"].Value;
            string content = StripRichTextRegex.Replace(rawContent, "");
            if (!content.Contains("_") && !content.Contains("?")) return match.Value;
            underscoreCount++;

            string restored = TryRestoreContent(content);
            if (restored == content)
            {
                Log($"RENDER#{renderCounter}: MATCH_FAILED raw=\"{Truncate(rawContent, 120)}\" stripped=\"{Truncate(content, 80)}\"");
                return match.Value;
            }

            restoredCount++;
            string type = ClassifyEntry(content);
            int prefixLen = match.Value.Length - rawContent.Length;
            string prefix = match.Value.Substring(0, prefixLen);
            string assembled = prefix + restored;
            // Fix duplicate <color= from regex eating it into prefix
            if (prefix.Length >= 7 && prefix.EndsWith("<color=") && restored.StartsWith("<color="))
                assembled = prefix.Substring(0, prefixLen - 7) + restored;
            Log($"RENDER#{renderCounter}: MATCH_OK type={type}");
            Log($"RENDER#{renderCounter}:  prefix[{prefixLen}]=\"{prefix}\"");
            Log($"RENDER#{renderCounter}:  content[{content.Length}]->\"{Truncate(restored, 80)}\"");
            Log($"RENDER#{renderCounter}:  assembled=\"{Truncate(assembled, 120)}\"");
            return assembled;
        });

        Log($"RENDER#{renderCounter}: RESTORE_SUMMARY matches={matchCount} underscored={underscoreCount} restored={restoredCount} captures_in_list={capturedMessages.Count}");
        return result;
    }

    private static string RestoreOngoingText(string fullText)
    {
        int matchCount = 0, underscoreCount = 0, restoredCount = 0;
        string result = OngoingEntryRegex.Replace(fullText, match =>
        {
            matchCount++;
            string rawContent = match.Groups["content"].Value;
            string content = StripRichTextRegex.Replace(rawContent, "");
            if (!content.Contains("_") && !content.Contains("?")) return match.Value;
            underscoreCount++;

            string restored = TryRestoreContent(content);
            if (restored == content) return match.Value;

            restoredCount++;
            int prefixLen = match.Value.Length - rawContent.Length;
            string prefix = match.Value.Substring(0, prefixLen);
            Log($"RENDER#{renderCounter}: ONGOING_MATCH_OK content->\"{Truncate(restored, 80)}\"");
            return prefix + restored;
        });

        Log($"RENDER#{renderCounter}: ONGOING_RESTORE_SUMMARY matches={matchCount} underscored={underscoreCount} restored={restoredCount} captures_in_list={capturedMessages.Count}");
        return result;
    }

    private static string TryRestoreContent(string content)
    {
        lock (captureLock)
        {
            // Log what we're searching with
            Log($"RENDER#{renderCounter}:  TRY_MATCH for content=\"{Truncate(content, 80)}\"");
            for (int i = capturedMessages.Count - 1; i >= 0; i--)
            {
                bool match = IsContentMatch(capturedMessages[i], content);
                if (match)
                {
                    Log($"RENDER#{renderCounter}:  FOUND_MATCH at index {i}: \"{Truncate(capturedMessages[i], 80)}\"");
                    return capturedMessages[i];
                }
            }
            Log($"RENDER#{renderCounter}:  NO_MATCH in {capturedMessages.Count} captures");
        }
        return content;
    }
}
