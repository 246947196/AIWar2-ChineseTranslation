import json

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

data = json.load(open(r'D:\Steam\steamapps\common\AI War 2\AIWar2_ChineseTranslation\tools\ilpatch\ArcenAIW2Core.part3.json', 'r', encoding='utf-8'))

print("Newly translated entries (mistakes to revert):")
for k, v in sorted(data.items()):
    if v and k not in existing_translated:
        print(f"  {repr(k)} -> {repr(v)}")