# Task 12 Report

## Status: Complete

## Strings translated
- **Damage modifier labels (abbreviated)**: 17 strings (max hull health, current hull health, max personal shields, current missing personal shields, current missing hull health, max bubble forcefield strength, current personal shields, is, current speed, armor, energy usage, albedo, mass, engine power, has been here for, have been here for, type)
- **Damage modifier labels (non-abbreviated)**: 17 strings (has max hull health of, has current hull health of, has missing current personal shields of, etc.)
- **Damage modifier conditions**: 4 strings (less than, more than, at most, at least → 小于/大于/最多/至少)
- **Damage modifier x/mm suffix strings**: 12 strings (x</color> damage to target, mm</color> of target armor, x</color> extra damage, etc.)
- **the above weapon does / the attacker does**: 4 strings (abbreviated + non-abbreviated, outgoing + incoming)
- **Module stat adjuster**: 4 strings (Hull Health Multiplied By, Shield Health Multiplied By, Bubble Forcefield added with, Bubble Forcefield replaces)
- **Cloaking system**: 6 strings (Max Cloaking Points, Cloaking, : This ship has, Every time this ship fires, After ..., seconds of not losing...)
- **Tractor beam**: 1 string (They can still move freely)
- **Gravity**: 1 string (infinite range)
- **Crippled info**: 3 strings (all variants of "Crippled - will not die...")
- **State of matter**: 2 strings (Must be, state of matter to function)
- **Spawn group labels**: 12 strings (Primary through Denary, Group)
- **Spawn group Between/and**: 2 strings
- **Command texts**: 14 strings (Assist, Attack, Load into, Unload, Go Attack All, Go Attack Move, Stop Moving, Move On, Attack Targets, Go to, unknown planet, Go via, Decollide, Move to)
- **Unknown Target**: 1 string
- **Construction/repair throughput**: 12 strings (Construct fleet units, Assist construction, Boost factory, Claim neutral units, Rebuild remains, Repairs allied, Hull/Shield/Engines, Hull, Shield, Engines, </color>)., </color>))

**Total: ~110 strings translated**

## DO NOT TRANSLATE
- Damage abort codes (x16 - kept English)

## Build Result
All 4 DLLs built successfully with 0 errors.

## Concerns
- Property labels (Strength, Hull, Shield, Metal, Energy, Argon, Radon, Xenon, "This will expire after", "This will expire in", "Could not find a") were not found in this file — they exist in Window_InGameHoverEntityInfo.cs instead, suggesting they may have been translated already in a prior task.
- "multiples of" in the abbreviated comparison section kept English since the abbreviated context uses symbolic operators (<, >, <=, >=) and only "multiples of" appears as text — the non-abbreviated section was translated to "倍数".
- " have " in FactionNetEnergy abbreviated case left as English since it's not in the brief mapping.
