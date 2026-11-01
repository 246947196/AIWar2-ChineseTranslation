using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine.UI;

namespace WorldTMPFontPatch
{
    /// <summary>
    /// 运行时采集：把真正送进渲染管线、且**仍是英文**的字符串记下来。
    ///
    /// 用途：DLL 里剩下几千条英文，靠静态分析分不清哪些玩家真的会看到。
    /// 玩一局之后，这份清单就是"还值得翻"的真实列表。
    /// 同时可以一并发现字体缺字（含中日韩字符但渲染异常的文本会在日志里体现）。
    /// </summary>
    internal static class TextCapture
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, int> Seen = new Dictionary<string, int>();
        private static readonly Regex Cjk = new Regex(@"[\u4e00-\u9fff]", RegexOptions.Compiled);
        private static readonly Regex HasLatin = new Regex(@"[A-Za-z]{2,}", RegexOptions.Compiled);
        private static readonly Regex NumberOnly = new Regex(@"^[\s\d\.,:%+\-*/()\[\]]+$", RegexOptions.Compiled);
        private static string _outPath;
        private static int _dirty;

        public static void Init()
        {
            try
            {
                _outPath = Path.Combine(Paths.BepInExRootPath, "plugins", "ChineseTranslation", "captured_en.tsv");
                File.WriteAllText(_outPath, "# 运行时采集：渲染过但仍是英文的字符串（count<TAB>text）\n", new UTF8Encoding(false));
                Harmony.CreateAndPatchAll(typeof(TextCapture));
                Logger.LogInfo("[TextCapture] 采集已启用 -> " + _outPath);
            }
            catch (Exception e)
            {
                Logger.LogWarning("[TextCapture] 启动失败: " + e.Message);
            }
        }

        private static BepInEx.Logging.ManualLogSource _logger;
        private static BepInEx.Logging.ManualLogSource Logger
        {
            get
            {
                if (_logger == null) _logger = BepInEx.Logging.Logger.CreateLogSource("TextCapture");
                return _logger;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TMP_Text), "set_text")]
        private static void OnSetText(string value)
        {
            Record(value, "TMP");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UnityEngine.UI.Text), "set_text")]
        private static void OnLegacySetText(string value)
        {
            Record(value, "UI.Text");
        }

        /// <summary>记录一条：value 是最终渲染的字符串，src 标记走了哪条渲染路径。</summary>
        private static void Record(string value, string src)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 2) return;
            if (Cjk.IsMatch(value)) return;               // 已经译好
            if (!HasLatin.IsMatch(value)) return;         // 纯数字/符号
            if (NumberOnly.IsMatch(value)) return;

            string caller = "";
            try
            {
                var st = new System.Diagnostics.StackTrace(3, false);   // 跳过 Harmony 的 detour 包装
                var f = st.GetFrame(0);
                if (f != null && f.GetMethod() != null)
                {
                    var m = f.GetMethod();
                    caller = (m.DeclaringType != null ? m.DeclaringType.Name : "") + "." + m.Name;
                }
            }
            catch { }

            string key = value + "\u0001" + src + "\u0001" + caller;
            lock (Gate)
            {
                int c;
                Seen.TryGetValue(key, out c);
                Seen[key] = c + 1;
                if (++_dirty >= 60) FlushLocked();
            }
        }

        public static void Flush()
        {
            lock (Gate) { FlushLocked(); }
        }

        private static void FlushLocked()
        {
            if (_outPath == null) return;
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# 运行时采集：渲染过但仍是英文的字符串\tcount\t路径\t调用方\ttext");
                foreach (var kv in Seen)
                {
                    string[] parts = kv.Key.Split('\u0001');
                    string t = parts[0].Replace("\r", "").Replace("\n", "\\n").Replace("\t", "\\t");
                    string src = parts.Length > 1 ? parts[1] : "";
                    string caller = parts.Length > 2 ? parts[2] : "";
                    sb.Append(kv.Value).Append('\t').Append(src).Append('\t')
                      .Append(caller).Append('\t').AppendLine(t);
                }
                File.WriteAllText(_outPath, sb.ToString(), new UTF8Encoding(false));
                _dirty = 0;
            }
            catch { }
        }
    }
}
