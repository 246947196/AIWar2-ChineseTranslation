using System;
using System.IO;
using Arcen.Universal;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace ArcenUIAssetRedirect;

[BepInPlugin("aiwar2.chinesetranslation.arcenuiredirect", "ArcenUI Asset Redirect", "1.0.0")]
public class ArcenUIRedirectPlugin : BaseUnityPlugin
{
    private static readonly System.Collections.Generic.Dictionary<string, ArcenAssetBundleCache> OurCache = new();

    private void Awake()
    {
        Harmony.CreateAndPatchAll(typeof(ArcenUIRedirectPlugin));
        Logger.LogInfo("ArcenUI Asset Redirect plugin loaded");
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ArcenAssetBundleManager), "LoadOrRetrieveBundle")]
    private static bool Prefix(string Filename, ref ArcenAssetBundleCache __result)
    {
        if (Filename != "arcenui")
            return true;

        if (OurCache.TryGetValue(Filename, out var cached))
        {
            __result = cached;
            return false;
        }

        string customPath = Path.Combine(Paths.PluginPath, "ChineseTranslation", "AssetBundles_Win", "arcenui");

        if (!File.Exists(customPath))
            return true;

        Debug.Log($"[ArcenUIAssetRedirect] Redirecting arcenui -> {customPath}");

        AssetBundle bundle = AssetBundle.LoadFromFile(customPath);
        if (bundle == null)
        {
            Debug.LogWarning("[ArcenUIAssetRedirect] Failed to load custom arcenui bundle");
            return true;
        }

        var cache = new ArcenAssetBundleCache("arcenui", bundle);
        OurCache[Filename] = cache;
        __result = cache;
        Debug.Log("[ArcenUIAssetRedirect] arcenui redirect successful");
        return false;
    }
}
