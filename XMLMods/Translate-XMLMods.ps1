param(
    [string]$SourceBase = "D:\Steam\steamapps\common\AI War 2\XMLMods",
    [string]$TargetBase = "D:\Steam\steamapps\common\AI War 2\AIWar2_ChineseTranslation\XMLMods"
)

function ConvertTo-Chinese {
    param([string]$text)
    if ([string]::IsNullOrEmpty($text)) { return $text }
    
    $result = $text
    
    # === General ships/units ===
    $result = $result -replace '(?<![a-zA-Z])Leere(?![a-zA-Z])', '戾尔'
    $result = $result -replace '(?<![a-zA-Z])Knallen(?![a-zA-Z])', '克纳伦'
    $result = $result -replace 'Void(?![a-zA-Z])', '虚空'
    $result = $result -replace 'Detonation', '引爆'
    $result = $result -replace 'Frigate', '护卫舰'
    $result = $result -replace 'Flagship', '旗舰'
    $result = $result -replace 'Turret', '炮塔'
    $result = $result -replace 'Shield(?!ing)', '护盾'
    $result = $result -replace 'Hack(?!er)', '入侵'
    $result = $result -replace 'Drone(?!s)', '无人机'
    $result = $result -replace 'Drones', '无人机'
    $result = $result -replace 'Guardian', '守护者'
    $result = $result -replace 'Bomber', '轰炸机'
    $result = $result -replace 'Fighter', '战斗机'
    $result = $result -replace 'Corvette', '护卫舰'
    $result = $result -replace 'Destroyer', '驱逐舰'
    $result = $result -replace 'Cruiser', '巡洋舰'
    $result = $result -replace 'Battleship', '战列舰'
    $result = $result -replace 'Dreadnought', '无畏舰'
    $result = $result -replace 'Carrier', '航母'
    $result = $result -replace 'Minefield', '雷区'
    $result = $result -replace 'Missile', '导弹'
    $result = $result -replace 'Battery', '炮台'
    $result = $result -replace 'Cannon', '加农炮'
    $result = $result -replace 'Beam', '光束'
    $result = $result -replace 'Laser', '激光'
    $result = $result -replace 'Rocket', '火箭'
    $result = $result -replace 'Torpedo', '鱼雷'
    $result = $result -replace 'Warhead', '弹头'
    $result = $result -replace 'Cloak(?:ing)?', '隐形'
    $result = $result -replace 'Tractor', '牵引'
    $result = $result -replace 'Fortress', '堡垒'
    $result = $result -replace 'Ark', '方舟'
    $result = $result -replace 'Castle', '城堡'
    $result = $result -replace 'Citadel', '要塞'
    $result = $result -replace 'Bastion', '堡垒'
    $result = $result -replace 'Mine(?![^a-zA-Z])', '水雷'
    $result = $result -replace 'Strikecraft', '星战机甲'
    $result = $result -replace 'Strike Craft', '星战机甲'
    $result = $result -replace 'Strike', '突击'
    $result = $result -replace 'Raider', '突击者'
    $result = $result -replace 'Support', '支援'
    $result = $result -replace 'Defense', '防御'
    $result = $result -replace 'Attack', '攻击'
    $result = $result -replace 'Escort(?=[^e])', '护航'
    $result = $result -replace 'Interceptor', '拦截机'
    $result = $result -replace 'Patrol', '巡逻'
    $result = $result -replace 'Outpost', '前哨'
    $result = $result -replace 'HQ', '总部'
    $result = $result -replace 'Shipyard', '船坞'
    $result = $result -replace 'Barracks', '兵营'
    $result = $result -replace 'Factory', '工厂'
    $result = $result -replace 'Station', '站'
    $result = $result -replace 'Capital', '首都'
    $result = $result -replace 'Outpost', '前哨'
    $result = $result -replace 'DZ ', '死亡区 '
    $result = $result -replace 'Sting', '螫刺'
    $result = $result -replace 'Rampage', '狂怒'
    $result = $result -replace 'Bulk', '堡垒'
    $result = $result -replace 'Lightshow', '光秀'
    $result = $result -replace 'Battlestation', '要塞'
    $result = $result -replace 'Sniper', '狙击手'
    $result = $result -replace 'Siege', '攻城'
    $result = $result -replace 'Pulsar', '脉冲星'
    $result = $result -replace 'Golem', '魔像'
    $result = $result -replace 'Raid', '突袭'
    $result = $result -replace 'Blitz', '闪击'
    $result = $result -replace 'Death', '死亡'
    $result = $result -replace 'Killer', '杀手'
    $result = $result -replace 'Cloaked', '隐形'
    $result = $result -replace 'Mercurial', '汞变'
    $result = $result -replace 'Plated', '镀层'
    $result = $result -replace 'Armored', '装甲'
    $result = $result -replace 'Guard\b', '守卫'
    $result = $result -replace 'Protector', '保护者'
    $result = $result -replace 'Amplifier', '放大器'
    $result = $result -replace 'Tachyon', '快子'
    $result = $result -replace 'Phase', '相位'
    $result = $result -replace 'Refracting', '折射'
    $result = $result -replace 'Lucent', '光莹'
    $result = $result -replace 'Charged', '充能'
    $result = $result -replace 'Radiant', '光辉'
    $result = $result -replace 'Efficient', '高效'
    $result = $result -replace 'Infused', '灌注'
    $result = $result -replace 'Goo', '粘液'
    $result = $result -replace 'Shrike', '伯劳'
    $result = $result -replace 'Needle', '飞针'
    $result = $result -replace 'Mortar', '迫击炮'
    $result = $result -replace 'Acidic', '酸性'
    $result = $result -replace 'Disrupter', '干扰者'
    $result = $result -replace 'Hunter', '猎人'
    $result = $result -replace 'Coil', '线圈'
    $result = $result -replace 'Corrosive', '腐蚀'
    $result = $result -replace 'Breach', '突破'
    $result = $result -replace 'Blockade', '封锁'
    $result = $result -replace 'Consumer', '消耗者'
    $result = $result -replace 'Adaptor', '适配者'
    $result = $result -replace 'Forceshield', '力场盾'
    $result = $result -replace 'Chain', '链式'
    $result = $result -replace 'Spire', '尖塔'
    $result = $result -replace 'Zenith', '天顶'
    $result = $result -replace 'Neinzul', '宁苏'
    $result = $result -replace 'Dyson', '戴森'
    $result = $result -replace 'Nanocaust', '纳米浩劫'
    $result = $result -replace 'Marauder(?!s)', '掠夺者'
    $result = $result -replace 'Marauders', '掠夺者'
    $result = $result -replace 'Dark Spire', '黑暗尖塔'
    $result = $result -replace 'Dark ', '黑暗'
    $result = $result -replace ' AIP', ' AIP'
    $result = $result -replace ' ARS', ' ARS'
    $result = $result -replace ' TSS', ' TSS'
    $result = $result -replace ' AI ', ' AI '
    $result = $result -replace 'Human', '人类'
    $result = $result -replace 'AI ', 'AI '
    $result = $result -replace 'AIP ', 'AIP '
    
    return $result
}

function Get-TranslatedText {
    param([string]$text)
    if ([string]::IsNullOrEmpty($text)) { return $text }
    
    # Skip pure code references, numbers, or abbreviations
    if ($text -match '^[A-Z0-9_.\s-]+$' -and $text.Length -gt 3) { return $text }
    if ($text -match '^[\d.]+$') { return $text }
    
    return ConvertTo-Chinese $text
}

function Process-XmlAttribute {
    param([string]$content, [string]$attrName)
    
    # Use singleline mode to handle multi-line attributes
    # Match: attrName="CONTENT" where CONTENT can span lines, 
    # and closing " is followed by space+word=/space+/>/space+>
    $pattern = "(?s)($attrName\s*=\s*"")((?:[^""\\]|\\.)*?)(""\s*(?:\w+=|/>|>|$))"
    
    return [regex]::Replace($content, $pattern, {
        param($match)
        $prefix = $match.Groups[1].Value
        $text = $match.Groups[2].Value
        $suffix = $match.Groups[3].Value
        
        $translated = Get-TranslatedText $text
        if ($translated -eq $text) { return $match.Value }
        return $prefix + $translated + $suffix
    })
}

$mods = @(
    @{Name="Leere"},
    @{Name="DarkSphere"},
    @{Name="AMU"},
    @{Name="MoreSystemDefenders"},
    @{Name="AIShieldGenerators"},
    @{Name="KaizersMarauders"},
    @{Name="LostHumans"},
    @{Name="SKCivilianIndustry"}
)

$totalAll = 0; $totalTran = 0

foreach ($mod in $mods) {
    $src = "$SourceBase\$($mod.Name)"
    $tgt = "$TargetBase\$($mod.Name)"
    
    Write-Host "`n=== $($mod.Name) ===" -ForegroundColor Cyan
    
    if (-not (Test-Path $src)) { Write-Host "  SKIP: source not found"; continue }
    
    $files = Get-ChildItem -Recurse -Filter "*.xml" $src | Where-Object {
        $_.FullName -notmatch '\\Source\\' -and $_.FullName -notmatch '\\Unused\\'
    }
    
    $cnt = 0; $tran = 0
    
    foreach ($file in $files) {
        $rel = $file.FullName.Substring($src.Length).TrimStart('\')
        $target = Join-Path $tgt $rel
        $targetDir = Split-Path $target -Parent
        
        if (-not (Test-Path $targetDir)) { New-Item -ItemType Directory -Path $targetDir -Force | Out-Null }
        
        $raw = Get-Content -Path $file.FullName -Raw -Encoding UTF8
        
        # Check if file has translatable attributes
        $has = $raw -match '(display_name|description|sidebar_text|chat_text|full_text|tooltip|choice_text|short_name|display_name_for_sidebar|description_short)\s*='
        
        if (-not $has) {
            Copy-Item $file.FullName $target -Force
            $cnt++; continue
        }
        
        $content = $raw
        
        # Process each translatable attribute
        $content = Process-XmlAttribute $content 'display_name_for_sidebar'
        $content = Process-XmlAttribute $content 'description_short'
        $content = Process-XmlAttribute $content 'display_name'
        $content = Process-XmlAttribute $content 'description'
        $content = Process-XmlAttribute $content 'sidebar_text'
        $content = Process-XmlAttribute $content 'chat_text'
        $content = Process-XmlAttribute $content 'full_text'
        $content = Process-XmlAttribute $content 'tooltip'
        $content = Process-XmlAttribute $content 'choice_text'
        $content = Process-XmlAttribute $content 'short_name'
        
        # Handle &lt; and &gt; escape sequences
        $content = $content -replace '&lt;', '<'
        $content = $content -replace '&gt;', '>'
        
        $utf8 = New-Object System.Text.UTF8Encoding $true
        [System.IO.File]::WriteAllText($target, $content, $utf8)
        
        Write-Host "  OK: $rel"
        $tran++; $cnt++
    }
    
    Write-Host "  Files: $cnt | Translated: $tran" -ForegroundColor Yellow
    $totalAll += $cnt; $totalTran += $tran
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "DONE! Total files: $totalAll | With translations: $totalTran" -ForegroundColor Green
