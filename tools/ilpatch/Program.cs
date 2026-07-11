using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

// AI War 2 IL-level localization tool.
//
// Modes:
//   inspect    <dll> [contains]   dump unique Ldstr strings (optionally filtered)
//   extract    <dll> <out.json>   dump translatable Ldstr as {"text":""} skeleton
//   dump-ldstr <dll> <out.json>   dump ALL unique Ldstr as ["str1","str2",...] (unfiltered)
//   patch      <dll> <dict.json>  replace Ldstr via dictionary, write in place (with .bak)

class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage:\n  ilpatch inspect <dll> [contains]\n  ilpatch extract <dll> <out.json>\n  ilpatch dump-ldstr <dll> <out.json>\n  ilpatch patch <dll> <dict.json> [--dry]");
            return 1;
        }

        var mode = args[0].ToLowerInvariant();
        try
        {
            switch (mode)
            {
                case "inspect": return Inspect(args);
                case "extract": return Extract(args);
                case "dump-ldstr": return DumpLdstr(args);
                case "patch": return Patch(args);
                default:
                    Console.WriteLine($"Unknown mode: {mode}");
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("ERROR: " + ex.Message);
            return 2;
        }
    }

    static IEnumerable<TypeDef> AllTypes(ModuleDefMD module)
    {
        var list = new List<TypeDef>();
        foreach (var t in module.Types) AddTypeRecursive(t, list);
        return list;
    }

    static void AddTypeRecursive(TypeDef t, List<TypeDef> list)
    {
        list.Add(t);
        foreach (var nt in t.NestedTypes) AddTypeRecursive(nt, list);
    }

    static IEnumerable<string> UniqueLdstrs(ModuleDefMD module)
    {
        var seen = new HashSet<string>();
        foreach (var type in AllTypes(module))
            foreach (var method in type.Methods)
            {
                if (method.Body == null) continue;
                foreach (var instr in method.Body.Instructions)
                    if (instr.OpCode == OpCodes.Ldstr && instr.Operand is string s && seen.Add(s))
                        yield return s;
            }
    }

    static int Inspect(string[] args)
    {
        if (args.Length < 2) { Console.WriteLine("need dll"); return 1; }
        var dll = args[1];
        var filter = args.Length > 2 ? args[2] : null;
        using var module = ModuleDefMD.Load(dll);
        int total = 0, shown = 0;
        foreach (var s in UniqueLdstrs(module))
        {
            total++;
            if (filter == null || s.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(s);
                shown++;
            }
        }
        Console.Error.WriteLine($"[inspect] {Path.GetFileName(dll)}: {total} unique, {shown} shown");
        return 0;
    }

    static int Extract(string[] args)
    {
        if (args.Length < 3) { Console.WriteLine("need dll + out.json"); return 1; }
        using var module = ModuleDefMD.Load(args[1]);
        var dict = new Dictionary<string, string>();
        foreach (var s in UniqueLdstrs(module))
            if (LooksTranslatable(s))
                dict[s] = "";
        File.WriteAllText(args[2], JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        Console.Error.WriteLine($"[extract] wrote {dict.Count} entries to {args[2]}");
        return 0;
    }

    static int DumpLdstr(string[] args)
    {
        if (args.Length < 3) { Console.WriteLine("need dll + out.json"); return 1; }
        using var module = ModuleDefMD.Load(args[1]);
        var list = new List<string>();
        foreach (var s in UniqueLdstrs(module))
            list.Add(s);
        var opts = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        File.WriteAllText(args[2], JsonSerializer.Serialize(list, opts));
        Console.Error.WriteLine($"[dump-ldstr] wrote {list.Count} entries to {args[2]}");
        return 0;
    }

    static int Patch(string[] args)
    {
        if (args.Length < 3) { Console.WriteLine("need dll + dict.json"); return 1; }
        var dll = args[1];
        var dictPath = args[2];
        var dry = args.Any(a => a == "--dry");

        if (!File.Exists(dll)) { Console.Error.WriteLine($"dll not found: {dll}"); return 2; }
        if (!File.Exists(dictPath)) { Console.Error.WriteLine($"dict not found: {dictPath}"); return 2; }

        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(dictPath));
        if (dict == null) { Console.Error.WriteLine("dict parse failed"); return 2; }

        using var module = ModuleDefMD.Load(dll);

        int replaced = 0, skipped = 0;
        var skippedList = new List<string>();
        foreach (var type in AllTypes(module))
            foreach (var method in type.Methods)
            {
                if (method.Body == null) continue;
                foreach (var instr in method.Body.Instructions)
                {
                    if (instr.OpCode != OpCodes.Ldstr || instr.Operand is not string original) continue;
                    if (dict.TryGetValue(original, out var zh) && !string.IsNullOrEmpty(zh))
                    {
                        instr.Operand = zh;
                        replaced++;
                    }
                    else
                    {
                        skipped++;
                        if (skippedList.Count < 50) skippedList.Add(original);
                    }
                }
            }

        if (dry)
        {
            Console.Error.WriteLine($"[dry] {Path.GetFileName(dll)}: would replace {replaced}, skip {skipped}");
            return 0;
        }

        // backup
        var bak = dll + ".bak";
        if (!File.Exists(bak)) File.Copy(dll, bak, false);

        var opts = new ModuleWriterOptions(module);
        opts.MetadataOptions.Flags |= MetadataFlags.KeepOldMaxStack;
        var outPath = dll + ".patched.tmp";
        module.Write(outPath, opts);

        // round-trip verify
        using (var verify = ModuleDefMD.Load(outPath))
        {
            if (verify.Types.Count == 0) throw new Exception("written module failed to reload");
        }

        bool overwrote = false;
        try
        {
            // Write to a temp file first, then copy over the target. File.Move's
            // replace-delete can be blocked by file watchers (explorer/steam); Copy
            // overwrites the contents without requiring exclusive delete rights.
            File.Copy(outPath, dll, true);
            overwrote = true;
        }
        catch (IOException)
        {
            FallbackSideBySide(outPath, dll);
        }
        catch (UnauthorizedAccessException)
        {
            FallbackSideBySide(outPath, dll);
        }
        finally
        {
            if (File.Exists(outPath)) File.Delete(outPath);
        }

        if (overwrote)
            Console.Error.WriteLine($"[patch] {Path.GetFileName(dll)}: replaced {replaced}, skipped {skipped}");
        if (skippedList.Count > 0)
            Console.Error.WriteLine("[patch] sample skipped (no dict entry):\n  " + string.Join("\n  ", skippedList));
        return 0;
    }

    static void FallbackSideBySide(string outPath, string dll)
    {
        var side = Path.ChangeExtension(dll, "new.dll");
        File.Move(outPath, side, true);
        Console.Error.WriteLine($"[patch] target locked; wrote side-by-side: {side}");
        Console.Error.WriteLine($"[patch] close the program holding it, then replace {Path.GetFileName(dll)} with {Path.GetFileName(side)}");
    }

    // Heuristic: keep only player-visible text, skip debug/log/identifier strings.
    static bool LooksTranslatable(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        var t = s.Trim();
        if (t.Length < 3) return false;
        if (!t.Any(char.IsLetter)) return false;                       // must contain letters
        if (t.Length <= 2) return false;

        // skip pure debug / log markers
        if (t.Contains("debugStage") || t.Contains("debugCode")) return false;
        if (t.StartsWith("START ") || t.StartsWith("FINISH ")) return false;
        if (t.Contains("Inner Error") || t.Contains(" exception") || t.Contains("Exception")) return false;
        if (t.Contains("error at ") || t.Contains("Hit error") || t.Contains("ERROR:")) return false;

        // skip all-caps identifier-like tokens (no lowercase, has underscores)
        if (t.Contains("_") && !t.Any(char.IsLower)) return false;

        // skip format-only strings (just {0}, {1}, etc.)
        if (System.Text.RegularExpressions.Regex.IsMatch(t, @"^\{\w+\}$")) return false;

        // require at least one space OR sentence punctuation OR it's a clear phrase
        bool looksLikePhrase = t.Contains(' ') || t.Contains('.') || t.Contains(':') ||
                               t.Contains('?') || t.Contains('!') || t.Contains(',') ||
                               t.Contains('\'') || t.Contains('(');
        return looksLikePhrase;
    }
}
