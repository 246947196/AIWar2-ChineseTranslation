using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Arcen.Universal;
using HarmonyLib;

namespace WorldTMPFontPatch;

internal static class PatchCharMapping
{
    private static bool initialized;
    private static readonly object initLock = new();

    internal static void Ensure()
    {
        if (initialized) return;
        initialized = true;
        lock (initLock) ApplyPatches();
    }

    private static void ApplyPatches()
    {
        Harmony harmony = new Harmony("aiwar2.chinesetranslation.charpatches");

        // ===== No patches currently active =====
        // All serialization-layer approaches tried and confirmed failed
        // (see AGENTS.md lines 294-313):
        //
        // - SupportedCharList expansion (any size): BaseBits boundary lock
        // - FullUnicode marker bypass: breaks old saves, not backward-compatible
        // - FullUnicode full replacement: canary mismatch, corrupts saves
        // - Dynamic BaseBits (Postfix on InitializeStringHandling): hardcode is
        //   structural; any bit-width change breaks read/write encoding consistency
        //
        // Only viable approach: SupportedCharList ≤ 128 (109 ASCII + ≤19 CJK)
    }
}
