import json

filepath = r'D:\Steam\steamapps\common\AI War 2\AIWar2_ChineseTranslation\tools\ilpatch\ArcenAIW2Core.part3.json'

data = json.load(open(filepath, 'r', encoding='utf-8'))

existing_translated = {
    " [[ All Factions ]] ",
    " [[ Player Factions ]] ",
    " [[ Allied Factions ]] ",
    " [[ Hostile Factions ]] ",
    " [[ Faction Beacons ]] ",
    " [[ Normally Invisible ]] ",
    "Everyone present in the galaxy.",
    "Any human player factions, all of whom are allied.",
    "These factions won't shoot at you, at least for now.",
    "These factions will shoot at you on sight.",
    "These factions could be called into the game by hacking a beacon, but are not currently not active in this galaxy.",
    "These factions are typically subordinate parts of other factions, and not something you'd see in your list of factions.  They may not even be relevant to your current game unless you hack a specific beacon.",
    "Sorry, but there can only be one player-type faction, and it must be named Human!  If you want to make a variant, you need to actually implement a PlayerType.",
    "Must be >= 1",
    "{0}x {1} mk{2} of {3}",
    "We Died And Reverted To Neutral",
    "</color> fizzled on  ",
    ", because it was paralyzed when it tried to detonate.",
    "</color> has destroyed  ",
    "</color> has ravaged  ",
    ", destroying most of the available resources.",
    "</color> has detonated on ",
    ", destroying pretty much everything there.",
    ", destroying pretty much everything there... and the same for the ",
}

# Keys that were incorrectly translated (they are debug fragments)
to_revert = {
    ' * multiplier ',
    ' at (',
    ' at index ',
    ' has a system of type ',
    ' has a system with a DataForMark of ordinal ',
    ' has a system with a DataForMark of type ',
    ' rather than a system of ordinal ',
    ' rather than a system of type ',
    ' rows?',
    '. Ship line ',
    ': this.MaxHullFinal (',
    '[empty!?]',
    '[null?]',
    '{0}x {1} #{2}',
}

for k in to_revert:
    if k in data:
        data[k] = ""

with open(filepath, 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=2)

# Verify
empty = sum(1 for v in data.values() if v == '')
total = len(data)
print(f"Total keys: {total}")
print(f"Empty (non-translated): {empty}")
print(f"Translated: {total - empty}")
print("JSON valid: OK")