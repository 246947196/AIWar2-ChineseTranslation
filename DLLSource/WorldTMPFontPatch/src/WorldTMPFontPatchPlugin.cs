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
            try
            {
                Font sysFont = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 14);
                if (sysFont != null) { cachedLegacyFont = sysFont; Log("Font: Microsoft YaHei"); return; }
            }
            catch { }

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
            if (!File.Exists(fontPath)) { Debug.LogWarning($"[WorldTMPFontPatch] Font not found: {fontPath}"); return; }

            AssetBundle bundle = AssetBundle.LoadFromFile(fontPath);
            if (bundle == null) return;
            Font bundleFont = bundle.LoadAsset<Font>(fontName);
            bundle.Unload(false);
            cachedLegacyFont = bundleFont;
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

    // ========== Legacy Text font replacement ==========
    [HarmonyPostfix]
    [HarmonyPatch(typeof(UnityEngine.UI.Text), "OnEnable")]
    private static void OnLegacyTextEnable(UnityEngine.UI.Text __instance)
    {
        if (cachedLegacyFont == null) return;
        if (__instance.font == cachedLegacyFont) return;
        __instance.font = cachedLegacyFont;
    }

    // ========== ChatLog overlay (child of TMP, auto-scrolls, Viewport clips) ==========
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
    private static void OnChatLogOverlay(TextMeshProUGUI __instance)
    {
        if (cachedLegacyFont == null) return;
        if (__instance.name != "ChatLog") return;
        Transform existing = __instance.transform.Find("TMP_LegacyOverlay");
        if (existing != null) { Log("OVERLAY: already exists"); return; }

        // Leave TMP visible for debugging comparison

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
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;

        // Fill TMP rect with left padding
        var rt = t.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        int leftPad = 0;
        rt.offsetMin = new Vector2(leftPad, 0);
        rt.offsetMax = Vector2.zero;

        t.text = "[覆盖层]";
        Log($"OVERLAY: created as child, fontSize={fontSize}, leftPad={leftPad}");

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

    // ========== LAYER: set_text display interception ==========
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TMP_Text), "set_text")]
    private static void OnTMPTextSetText(TMP_Text __instance, string value)
    {
        if (cachedLegacyFont == null) return;
        if (__instance.name != "ChatLog") return;

        // Find overlay as child of TMP
        Transform overlay = __instance.transform.Find("TMP_LegacyOverlay");
        UnityEngine.UI.Text legacy = overlay?.GetComponent<UnityEngine.UI.Text>();

        if (legacy == null)
        {
            Log($"RENDER#{renderCounter}: NO OVERLAY");
            return;
        }

        renderCounter++;
        string text = value ?? "";

        if (text.Contains("_"))
        {
            Log($"RENDER#{renderCounter}: HAS_UNDERSCORES len={text.Length}");
            Log($"RENDER#{renderCounter}: FULL_TEXT_BEGIN>>>>");
            Log(text);
            Log($"RENDER#{renderCounter}: FULL_TEXT_END<<<<");

            var matches = EntryContentRegex.Matches(text);
            Log($"RENDER#{renderCounter}: REGEX_FOUND {matches.Count} entries");
            foreach (Match m in matches)
            {
                string content = m.Groups["content"].Value;
                string flag = content.Contains("_") ? " [HAS_]" : " [OK]";
                string type = ClassifyEntry(content);
                Log($"RENDER#{renderCounter}:  entry len={content.Length}{flag} type={type} preview=\"{Truncate(content, 100)}\"");
            }

            string restored = RestoreChatLogText(text);
            if (restored == text)
                Log($"RENDER#{renderCounter}: NO_RESTORE_NEEDED (or failed)");

            string finalText = StripRichTextRegex.Replace(restored, "");
            Log($"RENDER#{renderCounter}: FINAL_LEGACY_TEXT_BEGIN>>>>");
            Log(finalText);
            Log($"RENDER#{renderCounter}: FINAL_LEGACY_TEXT_END<<<<");
            legacy.text = "[OVERLAY]" + finalText;
        }
        else
        {
            Log($"RENDER#{renderCounter}: CLEAN (no underscores) len={text.Length}");
            string finalText = StripRichTextRegex.Replace(text, "");
            Log($"RENDER#{renderCounter}: CLEAN_FINAL_TEXT=\"{Truncate(finalText, 200)}\"");
            legacy.text = "[OVERLAY]" + finalText;
        }
    }

    private static readonly Regex EntryContentRegex = new Regex(
        @"<link=\d+><u>[^<]+</u>\s+(?<content>.*?)</link>",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static string RestoreChatLogText(string fullText)
    {
        int matchCount = 0, underscoreCount = 0, restoredCount = 0;
        string result = EntryContentRegex.Replace(fullText, match =>
        {
            matchCount++;
            string content = match.Groups["content"].Value;
            if (!content.Contains("_")) return match.Value;
            underscoreCount++;

            string restored = TryRestoreContent(content);
            if (restored == content)
            {
                Log($"RENDER#{renderCounter}: MATCH_FAILED content=\"{Truncate(content, 120)}\"");
                return match.Value;
            }

            restoredCount++;
            string type = ClassifyEntry(content);
            int prefixLen = match.Value.Length - content.Length;
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
