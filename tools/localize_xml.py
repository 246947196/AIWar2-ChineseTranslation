#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""翻译记忆（TM）与 XML 本地化生成器。

为什么要有这个：汉化包过去是「把整份 XML 复制一份再手改成中文」，等于把游戏
数据分叉到某个版本。游戏每次更新（Arcen 一周 2~3 个 build），分叉里的旧数值
就会覆盖新数值，新加的字段/单位直接丢失 —— 只能靠人工三方合并，一旦停手，
包就废了。

现在的做法：
    zh_tm.tsv        唯一真源，一行 = 一条「英文原文 -> 中文」
    localize_xml.py build   每次现读**当前**游戏 XML，只替换命中的文本属性，
                             其余字节原样照抄，输出到 build_output/

这样数值、新字段、新单位永远跟上游走，翻译只需维护 TM；游戏改写了某句英文，
它会以「未命中」的形式出现在缺口清单里，而不是静默用旧译文。

用法：
    python tools/localize_xml.py extract            # 从项目现有中文抽取 TM
    python tools/localize_xml.py build              # 生成 build_output/
    python tools/localize_xml.py stats              # 只看覆盖率

约定：
    - 只替换 SAFE_ATTRS 里的文本属性；category/value/text 这类容易与技术标识
      撞车的属性，仅当 TM 里记录过同 (tag, attr) 上下文时才替换
    - 命中按英文原文匹配，因此元素改名/换文件/被复制都不会丢译文
"""
import argparse
import collections
import json
import os
import shutil
import shutil
import re
import sys
import xml.etree.ElementTree as ET
from xml.sax.saxutils import escape, unescape

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

TM_FILE = os.path.join(PROJECT, 'zh_tm.tsv')
OUT_DIR = os.path.join(PROJECT, 'build_output')

CJK = re.compile(r'[\u4e00-\u9fff]')

# 永远视为玩家可见文本的属性
SAFE_ATTRS = {
    'display_name', 'display_name_for_sidebar', 'default_display_name',
    'description', 'description_short', 'description_full', 'description_prefix',
    'description_of_effects', 'description_damage_name', 'description_conditions',
    'tooltip', 'full_text', 'sidebar_text', 'chat_text', 'chat_message_from',
    'short_name', 'extra_notes', 'placeholder_text', 'custom_NameForLobby',
    'random_option_description', 'random_option_display_name', 'choice_text',
    'planet_by_name', 'name_for_city_sockets', 'name_for_city_sockets_short',
    'text_to_show', 'Description',
}

# 可能被代码当作标识符匹配的属性，必须按上下文放行
RISKY_ATTRS = {'category', 'text', 'value', 'abbreviation', 'default_option',
               'arbitrary_options', 'name_for_city_sockets_short'}

# 这些属性的值是**选项标识/枚举**，代码会拿它做比较（例如银河设置的选项值被当作
# Devourer 的 allegiance），翻了就会抛 unknown allegiance。一律保留英文。
NEVER_ATTRS = {'field_value', 'default_option', 'arbitrary_options',
               'minor_faction_allegiance', 'allegiance'}

_ENTITIES = {'&quot;': '"', '&apos;': "'", '&lt;': '<', '&gt;': '>', '&amp;': '&'}


def _normcrlf(s):
    # 同一段文本在不同文件里可能 CRLF / LF 混用，归一再查表，否则同一句会分裂成两个键
    return s.replace('\r\n', '\n').replace('\r', '\n')


def unesc(s):
    return unescape(s)


def esc(s):
    return escape(s, {'"': '&quot;'})


# ---------------------------------------------------------------- 扫描器

class Attr:
    __slots__ = ('tag', 'name', 'start', 'end', 'raw', 'path')

    def __init__(self, tag, name, start, end, raw, path):
        self.tag = tag
        self.name = name
        self.start = start
        self.end = end
        self.raw = raw
        self.path = path

    @property
    def ctx(self):
        return f'{self.tag}:{self.name}'


_KEY_ATTRS = ('name', 'id', 'key', 'internal_name')


def iter_attrs(text):
    """遍历 XML 属性，返回带原文区间与元素路径的 Attr。

    必须走文本扫描而不是 ElementTree：游戏 XML 的描述里有**字面换行**，
    而任何符合规范的 XML 解析器都会把属性值里的换行正规化成空格，
    于是 en 与 zh 的键永远对不上（多行描述全部漏翻）。
    path 用「祖先链+name」表示，用于按元素身份配对。
    """
    n = len(text)
    i = 0
    stack = []
    while i < n:
        if text.startswith('<!--', i):
            j = text.find('-->', i)
            i = n if j < 0 else j + 3
            continue
        if text.startswith('<![CDATA[', i):
            j = text.find(']]>', i)
            i = n if j < 0 else j + 3
            continue
        if text[i] != '<':
            i += 1
            continue
        j = i + 1
        closing = j < n and text[j] == '/'
        if closing:
            j += 1
        k = j
        while k < n and text[k] not in ' \t\r\n/>':
            k += 1
        tag = text[j:k]
        if not tag or tag.startswith('?') or tag.startswith('!'):
            close = text.find('>', k)
            i = n if close < 0 else close + 1
            continue
        i = k
        attrs = []
        self_closing = False
        while i < n:
            while i < n and text[i] in ' \t\r\n':
                i += 1
            if i >= n:
                break
            if text.startswith('/>', i):
                self_closing = True
                i += 2
                break
            if text[i] == '>':
                i += 1
                break
            m = i
            while m < n and text[m] not in ' \t\r\n=/>':
                m += 1
            name = text[i:m]
            while m < n and text[m] in ' \t\r\n':
                m += 1
            if m < n and text[m] == '=':
                m += 1
                while m < n and text[m] in ' \t\r\n':
                    m += 1
                if m < n and text[m] in '"\'':
                    q = text[m]
                    vs = m + 1
                    ve = text.find(q, vs)
                    if ve < 0:
                        ve = n
                    if name:
                        attrs.append((name, vs, ve))
                    i = ve + 1
                    continue
            i = max(m, i + 1)

        if closing:
            if stack and (stack[-1] == tag or stack[-1].startswith(tag + '#')):
                stack.pop()
            continue

        ident = None
        for a, vs, ve in attrs:
            if a in _KEY_ATTRS:
                ident = f'{tag}#{text[vs:ve]}'
                break
        ident = ident or tag
        path = tuple(stack) + (ident,)
        for a, vs, ve in attrs:
            yield Attr(tag, a, vs, ve, text[vs:ve], path)
        if not self_closing:
            stack.append(ident)


# ---------------------------------------------------------------- 路径映射

def game_root():
    return os.path.dirname(PROJECT)


def counterpart(rel, root):
    """项目内相对路径 -> 游戏内对应文件（不存在返回 None）。

    历史包袱：DLC 译文存放在 GameData/Configuration/Expansions/<exp>/GameData/...，
    而游戏真实路径是 <root>/Expansions/<exp>/GameData/...，需要剥掉中间前缀。
    """
    rel = rel.replace('\\', '/')
    prefix = 'GameData/Configuration/Expansions/'
    if rel.startswith(prefix):
        rel = 'Expansions/' + rel[len(prefix):]
    p = os.path.join(root, rel.replace('/', os.sep))
    return p if os.path.exists(p) else None


def walk_xml(base):
    for dp, _, fn in os.walk(base):
        for f in fn:
            if f.lower().endswith('.xml'):
                yield os.path.join(dp, f)


def key_of_el(el, path):
    for a in ('name', 'id', 'key', 'internal_name'):
        v = el.get(a)
        if v:
            return path + (f'{el.tag}#{v}',)
    return path + (el.tag,)


def index_el(root):
    d = {}

    def walk(el, path):
        here = key_of_el(el, path)
        if here not in d:
            d[here] = el
        for ch in el:
            walk(ch, here)

    walk(root, ())
    return d


# ---------------------------------------------------------------- extract

def scan_map(path):
    """(元素路径, 属性名) -> 原文值（保留字面换行）"""
    d = {}
    text = open(path, encoding='utf-8', newline='').read()
    for at in iter_attrs(text):
        d.setdefault((at.path, at.name), at.raw)
    return d


def cmd_extract(args):
    root = game_root()
    buckets = collections.defaultdict(lambda: collections.defaultdict(set))
    pair = 0
    skipped_no_orig = 0
    for rel_base in ('GameData', 'XMLMods'):
        base = os.path.join(PROJECT, rel_base)
        if not os.path.isdir(base):
            continue
        for p in walk_xml(base):
            rel = os.path.relpath(p, PROJECT)
            orig = counterpart(rel, root)
            if not orig:
                skipped_no_orig += 1
                continue
            try:
                ET.parse(p)
                ET.parse(orig)
            except ET.ParseError:
                continue
            cur = scan_map(p)
            ref = scan_map(orig)
            for (path, a), cru in cur.items():
                gru = ref.get((path, a))
                if gru is None:
                    continue
                cv, gv = unesc(cru), unesc(gru)
                if not cv or not gv:
                    continue
                if not CJK.search(cv) or CJK.search(gv) or cv == gv:
                    continue
                ctx = f'{path[-1].split("#")[0]}:{a}'
                buckets[(gv, cv)][ctx].add(rel)
                pair += 1

    rows = []
    for (en, zh), ctxmap in buckets.items():
        for ctx in ctxmap:
            rows.append((en, zh, ctx))
    rows.sort()

    with open(TM_FILE, 'w', encoding='utf-8', newline='\n') as f:
        f.write('# AI War 2 汉化翻译记忆  en<TAB>zh<TAB>context\n')
        f.write('# 由 tools/localize_xml.py extract 生成；手工改这个文件即可，'
                'build 时按英文原文匹配\n')
        for en, zh, ctx in rows:
            f.write(f'{_cell(en)}\t{_cell(zh)}\t{ctx}\n')

    conflicts = collections.defaultdict(set)
    for (en, zh), _ in buckets.items():
        conflicts[en].add(zh)
    multi = {k: v for k, v in conflicts.items() if len(v) > 1}

    print(f'配对命中: {pair} 条  唯一英文: {len(conflicts)}  TM 行数: {len(rows)}')
    print(f'写入 {os.path.relpath(TM_FILE, PROJECT)}')
    print(f'同一英文多种译法（需人工统一）: {len(multi)}')
    for en, zs in list(multi.items())[:10]:
        print(f'  {en[:40]!r} -> {sorted(zs)[:3]}')
    if skipped_no_orig:
        print(f'无游戏原件可比（自建模组，跳过）: {skipped_no_orig}')
    return 0


def _cell(s):
    return s.replace('\\', '\\\\').replace('\t', '\\t').replace('\r', '\\r').replace('\n', '\\n')


def _uncell(s):
    out = []
    i = 0
    while i < len(s):
        c = s[i]
        if c == '\\' and i + 1 < len(s):
            n = s[i + 1]
            out.append({'t': '\t', 'r': '\r', 'n': '\n', '\\': '\\'}.get(n, '\\' + n))
            i += 2
            continue
        out.append(c)
        i += 1
    return ''.join(out)


# ---------------------------------------------------------------- build

IGNORE_FILE = os.path.join(PROJECT, 'translation_ignore.txt')


def load_ignore():
    pats = []
    if os.path.exists(IGNORE_FILE):
        for line in open(IGNORE_FILE, encoding='utf-8'):
            line = line.strip()
            if not line or line.startswith('#'):
                continue
            try:
                pats.append(re.compile(line))
            except re.error:
                pass
    return pats


KEYSTR_FILE = os.path.join(PROJECT, 'tools', 'key_strings.txt')


# 当前版本解析器不认、但 7 月分叉里塞进去的属性：生成时直接删掉，
# 否则游戏会弹 "Unrequested attribute 'Description'"。
DEPRECATED_ATTRS = {'Description'}


def sanitize(rel, text):
    rel_l = rel.replace('\\', '/')
    if 'DarkZenithResourceConversion' not in rel_l:
        return text
    for a in DEPRECATED_ATTRS:
        text = re.sub(r'\s+' + a + r'="[^"]*"', '', text)
    return text


# 当前版本解析器不认、但 7 月分叉里塞进去的属性：生成时直接删掉，
# 否则游戏会弹 "Unrequested attribute 'Description'"。
DEPRECATED_ATTRS = {'Description'}


def sanitize(rel, text):
    rel_l = rel.replace('\\', '/')
    if 'DarkZenithResourceConversion' not in rel_l:
        return text
    for a in DEPRECATED_ATTRS:
        text = re.sub(r'\s+' + a + r'="[^"]*"', '', text)
    return text


def load_keystrings():
    """C# 里参与 ==/!=/Equals/case 比较的字面量。

    这个游戏把显示字符串当代码用；这些值一旦翻成中文，代码里的比较就会失配
    （轻则表项丢失，重则空列表崩溃），所以它们在 XML 里必须保留英文。
    """
    ks = set()
    if os.path.exists(KEYSTR_FILE):
        for line in open(KEYSTR_FILE, encoding='utf-8'):
            line = line.rstrip('\n')
            if line and not line.startswith('#'):
                ks.add(line.replace('\\n', '\n'))
    return ks


def ignored(val, pats):
    return any(p.search(val) for p in pats)


IGNORE_FILE = os.path.join(PROJECT, 'translation_ignore.txt')


def ignored(val, pats):
    return any(p.search(val) for p in pats)


def load_tm():
    """en -> {ctx: {zh: count}}"""
    tm = {}
    with open(TM_FILE, encoding='utf-8') as f:
        for line in f:
            if not line.strip() or line.startswith('#'):
                continue
            parts = line.rstrip('\n').split('\t')
            if len(parts) < 3:
                continue
            en, zh, ctx = _normcrlf(_uncell(parts[0])), _uncell(parts[1]), parts[2]
            tm.setdefault(en, {}).setdefault(ctx, {}).setdefault(zh, 0)
            tm[en][ctx][zh] += 1
            # 缺口 TSV 里换行被存成字面量 \\n，早期手工补译时留下了这种键。
            # 这里给同一句再登记一个「反斜杠还原成真换行」的别名，两种写法都能命中。
            en2 = _normcrlf(en.replace('\\r\\n', '\n').replace('\\n', '\n').replace('\\t', '\t'))
            if en2 != en:
                tm.setdefault(en2, {}).setdefault(ctx, {}).setdefault(zh, 0)
                tm[en2][ctx][zh] += 1
    return tm


def pick(entry, ctx):
    """给定上下文取译文：优先同上下文，其次全局最高频。"""
    if ctx in entry:
        return max(entry[ctx].items(), key=lambda kv: kv[1])[0]
    tally = collections.Counter()
    for zn in entry.values():
        for zh, c in zn.items():
            tally[zh] += c
    return tally.most_common(1)[0][0] if tally else None


def _collect_from_dir(root):
    """从游戏目录读取 (label, rel, text)。"""
    items = []

    def add(label, base):
        if not os.path.isdir(base):
            return
        for p in walk_xml(base):
            rel = os.path.relpath(p, base)
            # 跳过 7 月遗留的「嵌套镜像」GameData/Configuration/Expansions/<exp>/...
            # 它是旧部署布局的残留，游戏会重复加载（日志里 Overwrote old entry 就是它）
            if label == 'GameData' and rel.replace('\\', '/').startswith('Configuration/Expansions/'):
                continue
            try:
                text = open(p, encoding='utf-8', newline='').read()
            except OSError:
                continue
            items.append((label, rel, sanitize(rel, text)))

    add('GameData', os.path.join(root, 'GameData'))
    exp_dir = os.path.join(root, 'Expansions')
    if os.path.isdir(exp_dir):
        for exp in sorted(os.listdir(exp_dir)):
            add(f'Expansions/{exp}/GameData', os.path.join(exp_dir, exp, 'GameData'))
    add('XMLMods', os.path.join(root, 'XMLMods'))
    return items


def _collect_from_zip(zpath):
    """从备份 zip 读取 (label, rel, text)——部署之后游戏目录已是中文，必须用备份当源。"""
    import zipfile
    items = []
    with zipfile.ZipFile(zpath) as z:
        for name in z.namelist():
            if not name.lower().endswith('.xml'):
                continue
            parts = name.split('/')
            if parts[0] == 'GameData' and len(parts) > 2:
                # 跳过旧布局的嵌套镜像：GameData/Configuration/Expansions/<exp>/...
                if parts[1] == 'Configuration' and len(parts) > 3 and parts[2] == 'Expansions':
                    continue
                label, rel = 'GameData', '/'.join(parts[1:])
            elif parts[0] == 'XMLMods' and len(parts) > 2:
                label, rel = 'XMLMods', '/'.join(parts[1:])
            elif parts[0] == 'Expansions' and len(parts) > 3 and parts[2] == 'GameData':
                label, rel = f'Expansions/{parts[1]}/GameData', '/'.join(parts[3:])
            else:
                continue
            try:
                items.append((label, rel, z.read(name).decode('utf-8')))
            except (UnicodeDecodeError, KeyError):
                continue
    return items


def _looks_deployed(items, limit=60):
    """抽样判断源目录是不是已经是中文（部署过），避免把中文当原文。"""
    sampled = translated = 0
    for _, _, text in items[:limit]:
        for at in iter_attrs(text):
            if at.name not in SAFE_ATTRS:
                continue
            val = unesc(at.raw)
            if not (_prose(val) or CJK.search(val)):
                continue
            sampled += 1
            if CJK.search(val):
                translated += 1
    return sampled > 0 and translated / sampled > 0.2, sampled, translated


def cmd_build(args):
    tm = load_tm()
    root = game_root()
    out_root = args.out or OUT_DIR

    if getattr(args, 'from_zip', None):
        items = _collect_from_zip(args.from_zip)
        print(f'源: 备份 {os.path.basename(args.from_zip)}（{len(items)} 个 XML）')
    else:
        items = _collect_from_dir(root)
        deployed, sampled, translated = _looks_deployed(items)
        if deployed:
            print('!! 游戏目录里的文本已经是中文（多半是上一轮部署的结果）。')
            print(f'!! 抽样 {sampled} 条文本属性，其中 {translated} 条是中文。')
            print('!! 直接 build 会把中文当原文，命中率极低。用备份当源：')
            print('!!   python tools\\localize_xml.py build --from-zip _backup\\game_xxx.zip')
            return 1

    if os.path.isdir(out_root) and not args.keep:
        shutil.rmtree(out_root)
    ignore = load_ignore()
    keystr = load_keystrings()
    stats = collections.Counter()
    gaps = []
    files_out = []

    for label, rel, text in items:
            text = sanitize(rel, text)
            pieces = []
            pos = 0
            hits = 0
            for at in iter_attrs(text):
                attr = at.name
                if attr in NEVER_ATTRS:
                    stats['kept_en'] += 1
                    continue
                if attr in NEVER_ATTRS:
                    stats['kept_en'] += 1
                    continue
                safe = attr in SAFE_ATTRS
                if not safe and attr not in RISKY_ATTRS:
                    continue
                val = _normcrlf(unesc(at.raw))
                if getattr(args, 'avoid_keys', False) and val in keystr:
                    # 只挡「被代码比较」的值。默认关闭：实测代码比较的多是 InternalName，
                    # 与 display_name 无关，全局封锁会把阵营名、地图选项名一起挡掉。
                    stats['kept_en'] += 1
                    continue
                entry = tm.get(val)
                if not entry:
                    if safe and _prose(val) and not ignored(val, ignore):
                        stats['gap'] += 1
                        gaps.append((f'{label}/{rel}', at.tag, attr, val))
                    continue
                ctx = at.ctx
                zh = pick(entry, ctx)
                if zh is None:
                    continue
                if not safe and ctx not in entry:
                    # 有译法但没有这个上下文，宁可不换，避免把技术值翻掉
                    continue
                pieces.append(text[pos:at.start])
                pieces.append(esc(zh))
                pos = at.end
                hits += 1
                stats['hit'] += 1
            if not hits:
                continue
            pieces.append(text[pos:])
            dst = os.path.join(out_root, label.replace('/', os.sep), rel.replace('/', os.sep))
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            with open(dst, 'w', encoding='utf-8', newline='') as f:
                f.write(''.join(pieces))
            files_out.append((label, rel, hits))
            stats['file'] += 1

    print(f'生成 {stats["file"]} 个文件，替换 {stats["hit"]} 处，'
          f'未命中文本 {stats["gap"]} 处，'
          f'因被代码比较而保留英文 {stats["kept_en"]} 处')
    print(f'输出目录: {os.path.relpath(out_root, PROJECT)}')

    gaps.sort()
    gap_file = os.path.join(PROJECT, 'translation_gaps.tsv')
    with open(gap_file, 'w', encoding='utf-8', newline='\n') as f:
        f.write('# 未命中的玩家可见英文（要补进 zh_tm.tsv）\n')
        f.write('file\ttag\tattr\tenglish\n')
        for g in gaps:
            f.write('\t'.join(_cell(x) for x in g) + '\n')
    print(f'缺口清单: {os.path.relpath(gap_file, PROJECT)}')

    by_label = collections.Counter()
    for label, _, hits in files_out:
        by_label[label] += hits
    for label, c in by_label.most_common():
        print(f'  {label}: {c} 处')

    # 输出自检：全部必须可解析
    bad = 0
    for label, rel, _ in files_out:
        p = os.path.join(out_root, label.replace('/', os.sep), rel)
        try:
            ET.parse(p)
        except ET.ParseError as e:
            bad += 1
            print(f'  FAIL {label}/{rel}: {e}')
    print(f'自检：{len(files_out)} 个输出文件，解析失败 {bad}')
    return 1 if bad else 0


def _prose(v):
    if CJK.search(v) or len(v) < 12 or ' ' not in v:
        return False
    if '\\' in v or '://' in v:
        return False
    return len(re.findall(r'[A-Za-z]{2,}', v)) >= 2


def cmd_stats(args):
    tm = load_tm()
    root = game_root()
    total = hit = 0
    for label, base in [('GameData', os.path.join(root, 'GameData'))]:
        for p in walk_xml(base):
            try:
                text = open(p, encoding='utf-8', newline='').read()
            except OSError:
                continue
            for at in iter_attrs(text):
                if at.name not in SAFE_ATTRS:
                    continue
                val = unesc(at.raw)
                if not _prose(val) and not CJK.search(val):
                    continue
                total += 1
                if val in tm:
                    hit += 1
    print(f'TM 英文条目: {len(tm)}')
    print(f'基础游戏文本属性: {total}  命中 {hit}  ({hit / max(1, total) * 100:.1f}%)')
    return 0


def main():
    ap = argparse.ArgumentParser(description='AI War 2 汉化 TM / XML 生成器')
    sub = ap.add_subparsers(dest='cmd', required=True)
    sub.add_parser('extract')
    b = sub.add_parser('build')
    b.add_argument('--out', default=None)
    b.add_argument('--keep', action='store_true', help='保留上次输出（默认先清空）')
    b.add_argument('--avoid-keys', dest='avoid_keys', action='store_true',
                   help='对 tools/key_strings.txt 里的值保留英文（默认关闭：多数比较用的是 InternalName）')
    b.add_argument('--from-zip', dest='from_zip', default=None,
                   help='用备份 zip 当源（部署过之后游戏目录已是中文，必须用这个）')
    sub.add_parser('stats')
    args = ap.parse_args()
    if args.cmd == 'extract':
        return cmd_extract(args)
    if args.cmd == 'build':
        return cmd_build(args)
    return cmd_stats(args)


if __name__ == '__main__':
    sys.exit(main())
