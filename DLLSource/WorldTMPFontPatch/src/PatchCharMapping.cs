using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Arcen.Universal;
using BepInEx;
using HarmonyLib;

namespace WorldTMPFontPatch;

/// <summary>
/// Root fix for non-ASCII text (planet names, chat, notes) being destroyed by the
/// game's condensed string serialization.
///
/// Verified root cause (dnlib IL analysis of ArcenUniversal.dll, 2026-07-18):
/// - AddString_Condensed -> InternalAdd -> AddRaw -> WriteBits_Char(c, fullUnicode:false)
///   -> WriteBits_InnerHelperUltraEfficient(CharUETypeData, GetCharIndexFromMapping(c))
/// - SupportedCharList is a hardcoded 109-char table (max char code U+2205).
/// - GetCharIndexFromMapping returns fallback index 4 for any char beyond the table
///   (all of CJK), and index 0 for in-bounds-but-unlisted chars (e.g. U+2014).
/// - CharUETypeData is hardcoded to UltraEfficientStyle 12 (E_0_To_127, 7 bits per char).
/// - Read side maps index 4 -> SupportedCharList[4] == '_', index 0 -> '?'.
/// So Chinese chars are irreversibly flattened to '_' at WRITE time; the save file
/// itself no longer contains the original text.
///
/// The bit format (16-bit length + 7-bit char indices, no per-string style marker)
/// cannot be changed without breaking every existing save (all earlier attempts at
/// expanding the charset or forcing a full-unicode mode failed for this reason).
///
/// This codec instead losslessly escapes unsupported chars into the SUPPORTED alphabet:
///   '~'                       -> '~~'
///   any other unsupported ch  -> '~u' + 4 uppercase hex digits (e.g. 中 -> ~u4E2D)
/// Every char in the encoded output exists in SupportedCharList, so the bitstream stays
/// 100% legacy-format: old saves load unchanged, new saves round-trip arbitrary Unicode.
/// </summary>
internal static class CondensedStringEscape
{
    private const char EscapeChar = '~';

    // Hardcoded fallback copy of the game's 109-char SupportedCharList
    // (extracted from ArcenUniversal.dll static init data, verified 2026-07-18).
    private const string FallbackSupportedChars =
        "?207_SixthUEeralzonDmGupTgAIMFcCHRsvdwkybPOWNLZ498f-315jVBJK6q 'Yüö<=#>:/.,%!'äXßQ@$^&*()[]{}+\\|\"“”‘`´~\n\r;∅ˌˉ";

    private static HashSet<char> supported;
    private static bool initDone;

    private static void EnsureInit()
    {
        if (initDone) return;
        initDone = true;
        try
        {
            FieldInfo field = typeof(ArcenSerializationBuffer).GetField(
                "SupportedCharList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (field?.GetValue(null) is char[] list && list.Length > 0)
                supported = new HashSet<char>(list);
        }
        catch (Exception ex)
        {
            try { UnityEngine.Debug.LogWarning($"[CondensedStringEscape] Failed to read SupportedCharList: {ex.Message}"); }
            catch { }
        }
        if (supported == null || supported.Count == 0)
            supported = new HashSet<char>(FallbackSupportedChars);
        try { UnityEngine.Debug.Log($"[CondensedStringEscape] Initialized with {supported.Count} supported chars"); }
        catch { }
    }

    private static bool IsSupported(char c)
    {
        // Fast path: all printable ASCII except '~' is in the table.
        if (c >= 32 && c <= 126 && c != EscapeChar) return true;
        return supported.Contains(c);
    }

    /// <summary>Encode a string so it survives the condensed 7-bit charset losslessly.</summary>
    internal static string Encode(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        EnsureInit();

        StringBuilder sb = null;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == EscapeChar || !IsSupported(c))
            {
                if (sb == null)
                {
                    sb = new StringBuilder(s.Length + 16);
                    sb.Append(s, 0, i);
                }
                if (c == EscapeChar)
                    sb.Append(EscapeChar).Append(EscapeChar);
                else
                    sb.Append(EscapeChar).Append('u').Append(((int)c).ToString("X4"));
            }
            else
            {
                sb?.Append(c);
            }
        }
        return sb?.ToString() ?? s;
    }

    /// <summary>Decode a string read from the condensed format back to the original.</summary>
    internal static string Decode(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        if (s.IndexOf(EscapeChar) < 0) return s;

        StringBuilder sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c != EscapeChar)
            {
                sb.Append(c);
                continue;
            }
            // '~~' -> literal '~'
            if (i + 1 < s.Length && s[i + 1] == EscapeChar)
            {
                sb.Append(EscapeChar);
                i++;
                continue;
            }
            // '~uXXXX' -> unicode char
            if (i + 5 < s.Length && s[i + 1] == 'u'
                && IsHex(s[i + 2]) && IsHex(s[i + 3]) && IsHex(s[i + 4]) && IsHex(s[i + 5]))
            {
                int code = (HexVal(s[i + 2]) << 12) | (HexVal(s[i + 3]) << 8) | (HexVal(s[i + 4]) << 4) | HexVal(s[i + 5]);
                sb.Append((char)code);
                i += 5;
                continue;
            }
            // Malformed / legacy data (old saves): leave the '~' untouched.
            sb.Append(EscapeChar);
        }
        return sb.ToString();
    }

    private static bool IsHex(char c) =>
        (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');

    private static int HexVal(char c)
    {
        if (c >= '0' && c <= '9') return c - '0';
        if (c >= 'A' && c <= 'F') return c - 'A' + 10;
        return c - 'a' + 10;
    }
}

internal static class PatchCharMapping
{
    private static bool initialized;
    private static readonly object initLock = new();
    private static string LogPath;

    internal static void Ensure()
    {
        if (initialized) return;
        initialized = true;
        lock (initLock) ApplyPatches();
    }

    private static void Log(string msg)
    {
        try
        {
            LogPath ??= Path.Combine(Paths.BepInExRootPath, "UnicodeEscape.txt");
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {msg}\n");
        }
        catch { }
    }

    private static void ApplyPatches()
    {
        try { File.WriteAllText(Path.Combine(Paths.BepInExRootPath, "UnicodeEscape.txt"), $"[{DateTime.Now:HH:mm:ss}] Patch init\n"); }
        catch { }

        Harmony harmony = new("aiwar2.chinesetranslation.charpatches");

        // WRITE: escape unsupported chars into the supported alphabet before the game
        // flattens them to '_'. Priority.Low so the chat-capture prefix (default priority)
        // sees the ORIGINAL text first.
        MethodInfo writeMethod = typeof(ArcenSerializationBuffer).GetMethod("AddString_Condensed");
        harmony.Patch(writeMethod,
            prefix: new HarmonyMethod(typeof(PatchCharMapping), nameof(AddStringCondensed_EncodePrefix))
            { priority = Priority.Low });

        // READ: decode escapes back to the original Unicode text.
        MethodInfo readMethod = typeof(ArcenDeserializationBufferModern).GetMethod("ReadString_Condensed");
        harmony.Patch(readMethod,
            postfix: new HarmonyMethod(typeof(PatchCharMapping), nameof(ReadStringCondensed_DecodePostfix)));

        Log("Patches applied: AddString_Condensed(encode prefix) + ReadString_Condensed(decode postfix)");
    }

    private static void AddStringCondensed_EncodePrefix(ref string Item, string FieldNameForErrors)
    {
        string original = Item;
        Item = CondensedStringEscape.Encode(Item);
        if (!ReferenceEquals(Item, original))
            Log($"ENCODE [{FieldNameForErrors}]: \"{Truncate(original)}\" -> \"{Truncate(Item)}\"");
    }

    private static void ReadStringCondensed_DecodePostfix(ref string __result, string FieldNameForErrors)
    {
        string original = __result;
        __result = CondensedStringEscape.Decode(__result);
        if (!ReferenceEquals(__result, original))
            Log($"DECODE [{FieldNameForErrors}]: \"{Truncate(original)}\" -> \"{Truncate(__result)}\"");
    }

    private static string Truncate(string s, int max = 60)
    {
        if (s == null) return "null";
        return s.Length <= max ? s : s.Substring(0, max) + "...";
    }
}
