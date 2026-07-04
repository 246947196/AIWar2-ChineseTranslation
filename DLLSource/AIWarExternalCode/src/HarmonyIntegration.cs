using Arcen.Universal;
using HarmonyLib;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// Thin convenience layer over the Harmony patching library that lets mods
    /// replace or inject into arbitrary methods in the shipped AIW2 DLLs without
    /// having to set up Harmony themselves. Modders just decorate a class with
    /// [HarmonyPatch(...)] and call <see cref="ApplyPatchesFromAssembly"/> from
    /// their startup code.
    ///
    /// IMPORTANT 鈥?Unity Mono runtime quirk: HarmonyLib types must NEVER appear
    /// in this class's metadata-level surface (field types, property types,
    /// method parameter/return types). They are only safe to reference inside
    /// method bodies. Mono eagerly resolves field types when the class is
    /// loaded, and that resolution does NOT consult AppDomain.AssemblyResolve
    /// the way the desktop CLR does, NOR does it see assemblies loaded via
    /// Assembly.LoadFrom (those go to a separate "LoadFrom load context" the
    /// type loader's binder ignores). Putting a Harmony-typed field on this
    /// class therefore produces a TypeLoadException the first time anything
    /// touches HarmonyIntegration, even with the resolver registered and
    /// 0Harmony.dll already eager-loaded. Method bodies, in contrast, are
    /// JIT'd lazily 鈥?by the time they execute, HarmonyDLLLoader has already
    /// loaded 0Harmony, so HarmonyLib types resolve cleanly inside them.
    /// </summary>
    public static class HarmonyIntegration
    {
        private const string SHARED_HARMONY_ID = "arcen.aiw2.shared";

        //Stored as object to keep the field type out of class metadata; cast back
        //to HarmonyLib.Harmony inside method bodies. See class comment for why.
        private static object sharedInstanceObj = null;
        private static bool isInitialized = false;

        public static bool IsInitialized => isInitialized;

        /// <summary>
        /// The shared Harmony instance AIW2 uses for any patches it applies on
        /// behalf of mods through <see cref="ApplyPatchesFromAssembly"/>. Returned
        /// as object for the metadata-level reasons described in the class comment;
        /// cast to <c>HarmonyLib.Harmony</c> at the call site if you need direct
        /// access. Modders who want their own Harmony instance should just
        /// construct one with their own ID 鈥?that keeps unpatching scoped to
        /// their own mod and avoids stepping on patches another mod registered.
        /// </summary>
        public static object GetSharedHarmonyInstance() => sharedInstanceObj;

        #region Initialize
        public static void Initialize()
        {
            if ( isInitialized )
                return;

            try
            {
                sharedInstanceObj = new Harmony( SHARED_HARMONY_ID );
                isInitialized = true;
                ArcenDebugging.LogSingleLine( "HarmonyIntegration: initialized shared Harmony instance '" + SHARED_HARMONY_ID + "' (Harmony version " + GetHarmonyVersionString() + ").", Verbosity.DoNotShow );
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine( "HarmonyIntegration.Initialize: failed to construct Harmony instance. Mod method-patching will not be available. Error: " + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region ApplyPatchesFromAssembly
        /// <summary>
        /// Scans the given assembly for classes annotated with [HarmonyPatch]
        /// and applies them via the shared AIW2 Harmony instance. Returns the
        /// number of methods successfully patched.
        ///
        /// Patches are applied one [HarmonyPatch] class at a time, with each
        /// class's Prefix/Postfix/Transpiler/Finalizer body pre-JITted before
        /// the patch is registered. That pre-JIT is the loud-failure mechanism
        /// for the most common type of mod-vs-host-version drift: a mod whose
        /// patch body's IL references a host-game member (method, field,
        /// property) that has been renamed or removed since the mod was last
        /// rebuilt. Without the pre-JIT, those references resolve only when
        /// Harmony's dynamic wrapper first calls into the patch 鈥?i.e. far
        /// later, in-game, in a code path the player happens to exercise 鈥?        /// and the resulting MissingMethodException doesn't name the mod or
        /// the patch class. With it, the same failure surfaces here at
        /// startup, with the mod ID, the patch class, and the offending
        /// member all in the log line.
        ///
        /// On any per-class failure (pre-JIT throws, or Harmony's own Patch()
        /// throws because the [HarmonyPatch] target method no longer exists
        /// or has different parameters), this method logs a ShowAsError line
        /// naming the mod and the broken patch class, then SKIPS that one
        /// class and continues with the rest. An aggregate "MOD X is broken:
        /// N of M classes failed" summary is logged at the end if anything
        /// failed. Mods do not need to do anything beyond calling this 鈥?the
        /// failure surface is fully owned here.
        /// </summary>
        public static int ApplyPatchesFromAssembly( Assembly modAssembly, string modIdForLogging )
        {
            if ( !isInitialized )
            {
                //Lazily construct the shared Harmony instance on first use.
                //Initialize() is idempotent, and the AssemblyResolve handler for
                //0Harmony was registered by ArcenAIW2Core's HarmonyDLLLoader during
                //Engine_AIW2.InitializeIntegrationsBeforeTCCheck, long before any
                //mod loads. AIWarExternalCode's InitialSetupForDLL also calls
                //Initialize() eagerly to give a deterministic init log line, but
                //this lazy path covers any ordering edge case.
                Initialize();
            }
            if ( !isInitialized )
            {
                ArcenDebugging.LogSingleLine( "HarmonyIntegration.ApplyPatchesFromAssembly: cannot apply patches for mod '" + modIdForLogging + "' because HarmonyIntegration failed to initialize (could not construct the shared Harmony instance).", Verbosity.ShowAsError );
                return 0;
            }
            if ( modAssembly == null )
            {
                ArcenDebugging.LogSingleLine( "HarmonyIntegration.ApplyPatchesFromAssembly: null assembly passed for mod '" + modIdForLogging + "'.", Verbosity.ShowAsError );
                return 0;
            }

            Harmony harmony = (Harmony)sharedInstanceObj;
            string modAssemblyName = modAssembly.GetName().Name;

            //GetTypes can throw ReflectionTypeLoadException if any type in the assembly
            //fails to load (e.g. it inherits from / has a field of a host-game type that
            //has since been removed). Capture whichever types DID load and continue;
            //the failed slots will be null in rtle.Types, which we filter below. Surface
            //the underlying loader errors first so the modder sees them before the
            //per-class failures that follow.
            Type[] types;
            try
            {
                types = modAssembly.GetTypes();
            }
            catch ( ReflectionTypeLoadException rtle )
            {
                types = rtle.Types ?? new Type[0];
                ArcenDebugging.LogSingleLine(
                    "HarmonyIntegration: MOD '" + modIdForLogging + "' (assembly '" + modAssemblyName + "'): " +
                    "ReflectionTypeLoadException while scanning the assembly for [HarmonyPatch] classes. " +
                    "Some types could not be loaded 鈥?usually this means the mod was built against an older " +
                    "version of the host game and references types that have since been renamed or removed. " +
                    "The mod will be partially or fully non-functional; please report to the mod author. " +
                    "Details: " + DescribeReflectionTypeLoadException( rtle ),
                    Verbosity.ShowAsError );
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine(
                    "HarmonyIntegration: MOD '" + modIdForLogging + "' (assembly '" + modAssemblyName + "'): " +
                    "could not enumerate types in the mod assembly. The mod will not work; please report to " +
                    "the mod author. Details: " + e,
                    Verbosity.ShowAsError );
                return 0;
            }

            int patchClassesAttempted = 0;
            int patchClassesApplied = 0;
            int methodsPatchedTotal = 0;
            //A plain BCL list 鈥?this collection is small, transient, and only exists
            //for the duration of this method, so the GC concern that drives the Arcen
            //collections elsewhere doesn't apply.
            System.Collections.Generic.List<string> failureSummaries = new System.Collections.Generic.List<string>();

            foreach ( Type type in types )
            {
                if ( type == null )
                    continue;

                bool hasHarmonyAttr;
                try
                {
                    //HarmonyAttribute is the base class for [HarmonyPatch] /
                    //[HarmonyPatchAll] / [HarmonyPriority] / etc. Checking the base
                    //covers any class Harmony's PatchAll would have picked up,
                    //including the rare classes that use [HarmonyPatchAll] without
                    //[HarmonyPatch].
                    hasHarmonyAttr = type.IsDefined( typeof( HarmonyAttribute ), inherit: true );
                }
                catch ( Exception )
                {
                    //IsDefined can itself throw if the type's attributes reference a
                    //missing assembly. Treat that as "not a patch class" 鈥?we can't
                    //tell what attribute it has, and the underlying loader error has
                    //already been surfaced via ReflectionTypeLoadException above.
                    continue;
                }
                if ( !hasHarmonyAttr )
                    continue;

                patchClassesAttempted++;

                //Pre-JIT every Prefix/Postfix/Transpiler/Finalizer (and TargetMethod /
                //TargetMethods, since Harmony invokes those during Patch()) so the JIT
                //resolves all method/field tokens NOW. Any unresolved reference throws
                //here, with the class+method+exception attached to the mod's identity,
                //instead of much later in gameplay when the patched method is first
                //hit.
                MethodInfo preJitFailedMethod = null;
                Exception preJitFailureException = null;
                MethodInfo[] candidates;
                try
                {
                    candidates = type.GetMethods( BindingFlags.Public | BindingFlags.NonPublic |
                                                  BindingFlags.Static | BindingFlags.Instance |
                                                  BindingFlags.DeclaredOnly );
                }
                catch ( Exception e )
                {
                    LogPatchClassFailure( modIdForLogging, modAssemblyName, type, "metadata read", e );
                    failureSummaries.Add( "    " + type.FullName + " (metadata read failure)" );
                    continue;
                }

                foreach ( MethodInfo m in candidates )
                {
                    if ( !IsHarmonyPatchMethodName( m.Name ) )
                        continue;
                    try
                    {
                        RuntimeHelpers.PrepareMethod( m.MethodHandle );
                    }
                    catch ( Exception e )
                    {
                        preJitFailedMethod = m;
                        preJitFailureException = e;
                        break;
                    }
                }

                if ( preJitFailureException != null )
                {
                    ArcenDebugging.LogSingleLine(
                        "HarmonyIntegration: MOD '" + modIdForLogging + "' has a broken patch 鈥?class " +
                        type.FullName + ", method " + preJitFailedMethod.Name + ". The body of this patch " +
                        "method cannot be JIT-compiled because its IL references a member (method, field, or " +
                        "property) that no longer exists in the host game. Most often the mod was built " +
                        "against an older version of AIW2 that has since renamed or removed that member. " +
                        "This patch will be SKIPPED (the rest of the mod's patches, if any, will still be " +
                        "applied). Please report this to the mod author. Details: " + preJitFailureException,
                        Verbosity.ShowAsError );
                    failureSummaries.Add( "    " + type.FullName + "." + preJitFailedMethod.Name + " (patch body IL references a missing member)" );
                    continue;
                }

                //Apply the single patch class. CreateClassProcessor lets us scope failure
                //handling to one class at a time 鈥?Harmony's bulk PatchAll throws on the
                //first bad class and leaves any later classes in the assembly unpatched
                //with no clear log line tying the failure to a specific patch.
                try
                {
                    System.Collections.Generic.List<MethodInfo> patched = harmony.CreateClassProcessor( type ).Patch();
                    patchClassesApplied++;
                    if ( patched != null )
                        methodsPatchedTotal += patched.Count;
                }
                catch ( Exception e )
                {
                    ArcenDebugging.LogSingleLine(
                        "HarmonyIntegration: MOD '" + modIdForLogging + "' has a broken patch 鈥?class " +
                        type.FullName + " could not be applied. Most often this means the [HarmonyPatch(...)] " +
                        "attribute on the class points at a method that no longer exists, has been renamed, " +
                        "moved to a nested type, or has different parameters than the patch declares. " +
                        "This patch will be SKIPPED (the rest of the mod's patches, if any, will still be " +
                        "applied). Please report this to the mod author. Details: " + e,
                        Verbosity.ShowAsError );
                    failureSummaries.Add( "    " + type.FullName + " (Harmony Patch() threw 鈥?likely a missing or moved target method)" );
                }
            }

            if ( failureSummaries.Count > 0 )
            {
                StringBuilder sb = new StringBuilder();
                sb.Append( "HarmonyIntegration: MOD '" ).Append( modIdForLogging ).Append( "' is broken: " )
                  .Append( failureSummaries.Count ).Append( " of " ).Append( patchClassesAttempted )
                  .Append( " [HarmonyPatch] class(es) failed to apply. The mod will not behave as intended; " )
                  .Append( "please report to the mod author. Per-class details were logged above. Failed patches:" )
                  .Append( Environment.NewLine );
                for ( int i = 0; i < failureSummaries.Count; i++ )
                    sb.Append( failureSummaries[i] ).Append( Environment.NewLine );
                ArcenDebugging.LogSingleLine( sb.ToString(), Verbosity.ShowAsError );
            }
            else
            {
                ArcenDebugging.LogSingleLine(
                    "HarmonyIntegration.ApplyPatchesFromAssembly: mod '" + modIdForLogging +
                    "' (assembly '" + modAssemblyName + "') applied " + patchClassesApplied +
                    " [HarmonyPatch] class(es), patching " + methodsPatchedTotal + " method(s).",
                    Verbosity.DoNotShow );
            }

            //Returns the method-level patch count. The number isn't used by the
            //bundled mods 鈥?they trust the framework's own per-class log lines
            //rather than asserting a count themselves 鈥?but it's part of the
            //long-standing public API, so callers that do want to inspect it
            //get the more useful method-level total (vs. the class count) for
            //free.
            return methodsPatchedTotal;
        }
        #endregion

        #region IsHarmonyPatchMethodName
        //The set of well-known Harmony patch-method names. Harmony's PatchClassProcessor
        //recognizes these by name (or by [HarmonyPrefix] / [HarmonyPostfix] / etc.
        //attributes, but those just rename a method that ends up in this same list of
        //roles). We pre-JIT all of them; pre-JITing a method that doesn't end up being
        //called by Harmony is harmless.
        private static bool IsHarmonyPatchMethodName( string name )
        {
            switch ( name )
            {
                case "Prefix":
                case "Postfix":
                case "Transpiler":
                case "Finalizer":
                case "TargetMethod":
                case "TargetMethods":
                case "Prepare":
                case "Cleanup":
                    return true;
                default:
                    return false;
            }
        }
        #endregion

        #region LogPatchClassFailure
        private static void LogPatchClassFailure( string modIdForLogging, string modAssemblyName, Type type, string stage, Exception e )
        {
            ArcenDebugging.LogSingleLine(
                "HarmonyIntegration: MOD '" + modIdForLogging + "' (assembly '" + modAssemblyName + "'): " +
                "patch class " + type.FullName + " failed during " + stage + ". This patch will be SKIPPED. " +
                "Please report to the mod author. Details: " + e,
                Verbosity.ShowAsError );
        }
        #endregion

        #region DescribeReflectionTypeLoadException
        //ReflectionTypeLoadException's default ToString() doesn't include LoaderExceptions,
        //which is where the actually useful "could not load type X" / "missing method Y"
        //messages live. Concatenate them so the modder gets the full picture in one log line.
        private static string DescribeReflectionTypeLoadException( ReflectionTypeLoadException rtle )
        {
            StringBuilder sb = new StringBuilder();
            sb.Append( rtle.GetType().FullName ).Append( ": " ).Append( rtle.Message );
            if ( rtle.LoaderExceptions != null )
            {
                int n = rtle.LoaderExceptions.Length;
                int shown = 0;
                for ( int i = 0; i < n; i++ )
                {
                    Exception le = rtle.LoaderExceptions[i];
                    if ( le == null )
                        continue;
                    sb.Append( Environment.NewLine ).Append( "  LoaderException[" ).Append( i ).Append( "]: " ).Append( le );
                    shown++;
                    if ( shown >= 8 )
                    {
                        sb.Append( Environment.NewLine ).Append( "  (" ).Append( n - shown ).Append( " more loader exception(s) suppressed)" );
                        break;
                    }
                }
            }
            return sb.ToString();
        }
        #endregion

        #region GetHarmonyVersionString
        private static string GetHarmonyVersionString()
        {
            try
            {
                AssemblyName name = typeof( Harmony ).Assembly.GetName();
                return name.Version != null ? name.Version.ToString() : "unknown";
            }
            catch ( Exception )
            {
                return "unknown";
            }
        }
        #endregion
    }
}
