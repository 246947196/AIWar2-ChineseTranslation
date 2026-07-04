using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class WavesHelper
    {
        public static readonly WavesHelper Instance = new WavesHelper();

        #region GetPlanetToUseForWavesFromPlanet
        /// <summary>
        /// Sometimes we're trying to launch from a planet that doesn't have wave stuff set up.
        /// In those cases, look outward to find the real place to use.
        /// </summary>
        public Planet GetPlanetToUseForWavesFromPlanet( Planet PlanetIIntendToUse, bool ForceUseAdjacentPlanet, bool ForceUseRandomPlanet, ArcenHostOnlySimContext Context )
        {
            List<Planet> planetToUseForWaveWorkingList = Planet.GetTemporaryPlanetList( "WavesHelper-GetPlanetToUseForWavesFromPlanet-planetToUseForWaveWorkingList", 10f );
            if ( planetToUseForWaveWorkingList == null ) //blocked for teardown/shutdown; bail
                return null;

            //if the current planet has nothing, or we want to force using an adjacent planet, then do that now
            if ( PlanetIIntendToUse != null && (ForceUseAdjacentPlanet || PlanetIIntendToUse.ShipGroup_WavesFromHere_Normal == null) )
            {
                foreach ( Planet neighbor in PlanetIIntendToUse.LinkedNeighbors( false ) )
                {
                    if ( neighbor.ShipGroup_WavesFromHere_Normal != null )
                        planetToUseForWaveWorkingList.Add( neighbor );
                }

                //did we find something valid next to us?  If so, then use one of them at random
                if ( planetToUseForWaveWorkingList.Count > 0 )
                {
                    Planet ret = planetToUseForWaveWorkingList[Context.RandomToUse.Next( 0, planetToUseForWaveWorkingList.Count )];
                    Planet.ReleaseTemporaryPlanetList( planetToUseForWaveWorkingList );
                    return ret;
                }
            }
            //if the current planet has nothing and we didn't find the thing above, or we want to force using a completely random planet, then do that now
            if ( PlanetIIntendToUse == null || ForceUseRandomPlanet || PlanetIIntendToUse.ShipGroup_WavesFromHere_Normal == null )
            {
                foreach ( Planet otherPlanet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( otherPlanet.ShipGroup_WavesFromHere_Normal != null )
                    {
                        if ( otherPlanet.MarkLevelForAIOnly.Ordinal >= 7 )
                            continue; //skip ones that are that high level!
                        planetToUseForWaveWorkingList.Add( otherPlanet );
                    }
                }

                //did we find something valid anywhere?  If so, then use one of them at random
                if ( planetToUseForWaveWorkingList.Count > 0 )
                {
                    Planet ret = planetToUseForWaveWorkingList[Context.RandomToUse.Next( 0, planetToUseForWaveWorkingList.Count )];
                    Planet.ReleaseTemporaryPlanetList( planetToUseForWaveWorkingList );
                    return ret;
                }
            }
            Planet.ReleaseTemporaryPlanetList( planetToUseForWaveWorkingList );
            //I guess we're using what we intended to...
            return PlanetIIntendToUse;
        }
        #endregion

        #region GetWaveComposition
        //Arguments: numDifferentShipsToSpawn controls how many different types of ships to spawn
        //those ships are drawn from AIShipGroup
        //this can be overriden by passing in overrideUnitsToSpawn (currently only done by hacking code)
        //targetPlanet is passed in to allow us eventually to analyze the planet's defenses and
        //produce a wave optimized for that planet (perhaps on low difficulty, waves are crafted to be weaker
        //against the defenses, whereas on high difficulty waves are crafted to be stronger)
        //NOTA BENE: this is also used by the AI Reserves, which is why its static
        public void GetWaveComposition( Dictionary<GameEntityTypeData, int> DictToFill, Faction faction, ArcenHostOnlySimContext Context, int budget, out int budgetSpent, int numDifferentShipsToSpawn,
            int maxGuardianTypes, PlannedWaveOptions Options, Planet planetToUseForSpawningTypes, bool tracing, ArcenCharacterBuffer tracingBuffer, GameEntityTypeData mustIncludeOneOf = null )
        {
            int debugCode = 0;
            budgetSpent = 0;
            try
            {
                DictToFill.Clear();
                debugCode = 100;
                AISentinelsCoreData factionExternal = faction.TryGetAISentinelsCoreData()?.SentinelInfo;

                AITypeData aiType = factionExternal == null ? null : factionExternal.AIType;
                //AIBudgetItem reinforcementBudgetItem = aiType == null ? null : aiType.BudgetItems[AIBudgetType.Reinforcement];
                AIBudgetItem waveBudgetItem = aiType == null ? null : aiType.BudgetItems[AIBudgetType.Wave];

                ThrowawayDrawBagCanMemLeak<GameEntityTypeData> workingUnitsToSpawn = ThrowawayDrawBagCanMemLeak<GameEntityTypeData>.Create_WillActuallyBeGCed( 30 );
                ThrowawayDrawBagCanMemLeak<GameEntityTypeData> workingBag = ThrowawayDrawBagCanMemLeak<GameEntityTypeData>.Create_WillActuallyBeGCed( 30 );
                debugCode = 200;
                GameEntityTypeData forcedDire = null;
                if ( Options.overrideSpawnUnits == null )
                {
                    debugCode = 300;
                    //This loop chooses the ship types we will use
                    if ( tracing ) tracingBuffer.Add( "getWaveComposition: budget: " + budget + " no ships specified, so go with the defaults from the planet\n" );
                    workingBag.Clear();
                    AIShipGroup shipGroup = planetToUseForSpawningTypes.ShipGroup_WavesFromHere_Normal;
                    //if ( tracing ) tracingBuffer.Add( "getWaveComposition: maxGuardianTypes = " + maxGuardianTypes + ", minGuardianTypes = " + minGuardianTypes + ", total types = " + numDifferentShipsToSpawn + "\n" );

                    if ( waveBudgetItem != null && !waveBudgetItem.NormalAIShipGroup.DrawBag.GetHasItem( shipGroup ) )
                    {
                        //this planet has a type set that we don't use!  This probably means mutliple AIs.  That's okay!  Just set it to something we DO have.
                        shipGroup = waveBudgetItem.NormalAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                    }

                    #region Seed Dire Guardians if necessary
                    debugCode = 400;
                    int percentDireIfAllowed = 100;

                    if ( Options.allowDireGuardians && Context.RandomToUse.Next( 0, 100 ) < percentDireIfAllowed )
                    {
                        debugCode = 500;
                        //ArcenDebugging.ArcenDebugLogSingleLine("dire A", Verbosity.DoNotShow );
                        AIShipGroup direGuardianShipGroup = planetToUseForSpawningTypes.ShipGroup_WavesFromHere_DireGuardians;
                        if ( direGuardianShipGroup != null )
                        {
                            if ( waveBudgetItem != null && !waveBudgetItem.DireGuardianAIShipGroup.DrawBag.GetHasItem( direGuardianShipGroup ) )
                            {
                                //this planet has a type set that we don't use!  This probably means mutliple AIs.  That's okay!  Just set it to something we DO have.
                                direGuardianShipGroup = waveBudgetItem.DireGuardianAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                            }

                            if ( direGuardianShipGroup != null )
                            {
                                //ArcenDebugging.ArcenDebugLogSingleLine("dire B", Verbosity.DoNotShow );
                                workingBag.Clear(); // Unsure if needed, never hurts though
                                workingBag.CopyFrom( direGuardianShipGroup.DrawBag );
                                Helper_RemoveWaveForbiddenItemsFromWorkingDrawBag( workingBag );
                                forcedDire = workingBag.PickRandomItemAndDoNotReplace( Context.RandomToUse );
                            }
                        }
                    }
                    #endregion

                    #region Seed Guardians into wave
                    // Do guardian seeding if the wave's drawbag isn't full yet and we have yet to reach the minimum number of 
                    //Add a minumum amount of guardians to the mix, guaranteed (unless the wave runs out of ship types first)
                    debugCode = 600;
                    int minGuardianTypes = 0;
                    if ( maxGuardianTypes > 0 )
                        minGuardianTypes = Context.RandomToUse.NextWithInclusiveUpperBound( 1, maxGuardianTypes );
                    if ( Options.allowGuardians && minGuardianTypes == 0 )
                    {
                        minGuardianTypes++;
                    }
                    int numGuardianTypesUsed = 0;
                    if ( numGuardianTypesUsed < minGuardianTypes )
                    {
                        debugCode = 700;
                        if ( tracing ) tracingBuffer.Add( "getWaveComposition: Starting guardian selection for incoming fleet. We currently have " + numGuardianTypesUsed + " guardian types used, and we want a minumum of" + minGuardianTypes + " guardian types\n" );

                        workingBag.Clear(); // Unsure if needed, never hurts though
                        AIShipGroup guardianShipGroup = planetToUseForSpawningTypes.ShipGroup_WavesFromHere_Guardians;
                        if ( planetToUseForSpawningTypes.MarkLevelForAIOnly.Ordinal >= 7 && planetToUseForSpawningTypes.ShipGroup_WavesFromHere_DireGuardians != null )
                        {
                            guardianShipGroup = planetToUseForSpawningTypes.ShipGroup_WavesFromHere_DireGuardians;

                            if ( waveBudgetItem != null && !waveBudgetItem.DireGuardianAIShipGroup.DrawBag.GetHasItem( guardianShipGroup ) )
                            {
                                //this planet has a type set that we don't use!  This probably means mutliple AIs.  That's okay!  Just set it to something we DO have.
                                guardianShipGroup = waveBudgetItem.DireGuardianAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                            }
                        }
                        else
                        {
                            if ( waveBudgetItem != null && !waveBudgetItem.GuardianAIShipGroup.DrawBag.GetHasItem( guardianShipGroup ) )
                            {
                                //this planet has a type set that we don't use!  This probably means mutliple AIs.  That's okay!  Just set it to something we DO have.
                                guardianShipGroup = waveBudgetItem.GuardianAIShipGroup.DrawBag.PickRandomItemAndReplace( Context.RandomToUse );
                            }
                        }
                        if ( guardianShipGroup != null )
                            workingBag.CopyFrom( guardianShipGroup.DrawBag );
                        Helper_RemoveWaveForbiddenItemsFromWorkingDrawBag( workingBag );
                        debugCode = 800;
                        while ( numGuardianTypesUsed < minGuardianTypes )
                        {
                            debugCode = 900;
                            if ( !workingBag.GetHasItems() )
                            {
                                // Refill the guardian bag if we ran out and still don't have enough
                                workingBag.Clear();
                                if ( guardianShipGroup != null )
                                {
                                    workingBag.CopyFrom( guardianShipGroup.DrawBag );
                                    Helper_RemoveWaveForbiddenItemsFromWorkingDrawBag( workingBag );
                                }
                                else
                                    break;
                            }
                            debugCode = 1000;
                            GameEntityTypeData type = workingBag.PickRandomItemAndDoNotReplace( Context.RandomToUse );
                            if (type == null)
                            {
                                if ( tracing ) tracingBuffer.Add( "getWaveComposition: cannot add more guardians because empty bag\n" );
                                break;
                            }
                            workingUnitsToSpawn.AddItem( type, 1 );
                            if ( tracing ) tracingBuffer.Add( "getWaveComposition: Adding " + type.InternalName + " as a build choice for the incoming fleet. We have now chosen " + numGuardianTypesUsed + " of our max of " + minGuardianTypes + " different guardian types\n" );
                            numGuardianTypesUsed++;
                        }
                    }
                    #endregion
                    debugCode = 1100;
                    workingBag.Clear();
                    if ( shipGroup != null )
                        workingBag.CopyFrom( shipGroup.DrawBag );
                    Helper_RemoveWaveForbiddenItemsFromWorkingDrawBag( workingBag );
                    debugCode = 1200;
                    int chosenStrikecraftTypes = 0;
                    while ( chosenStrikecraftTypes < numDifferentShipsToSpawn )
                    {
                        debugCode = 1300;
                        if ( !workingBag.GetHasItems() )
                        {
                            numDifferentShipsToSpawn = chosenStrikecraftTypes;
                            break;
                        }
                        GameEntityTypeData type = workingBag.PickRandomItemAndDoNotReplace( Context.RandomToUse );
                        //if there are guardians in a normal strikecraft-type wave, then we don't care; this is for royal guardians, etc.
                        //if ( type.SpecialType == SpecialEntityType.AIGuardian && maxGuardianTypes != -1 )
                        //{
                        //    if ( numGuardianTypesUsed > maxGuardianTypes || maxGuardianTypes == 0 )
                        //        continue;
                        //    numGuardianTypesUsed++;
                        //}
                        debugCode = 1400;
                        workingUnitsToSpawn.AddItem( type, 1 );
                        if ( tracing ) tracingBuffer.Add( "getWaveComposition: Adding " + type.InternalName + " as a build choice for the incoming fleet. AI cost: " + type.CostForAIToPurchase + " strength " + type.BaseMark.StrengthPerSquad_CalculatedWithNullFleetMembership + ". We have now chosen " + chosenStrikecraftTypes + " of our max of " + numDifferentShipsToSpawn + " different ship types\n" );
                        chosenStrikecraftTypes++;
                    }
                }
                else
                {
                    debugCode = 1500;
                    if ( tracing ) tracingBuffer.Add( "getWaveComposition: buying budget: " + budget + " worth of " + Options.overrideSpawnUnits.InternalName + "\n" );
                    workingUnitsToSpawn.AddItem( Options.overrideSpawnUnits, 1 );
                }
                debugCode = 1600;
                if ( mustIncludeOneOf != null )
                {
                    debugCode = 1700;
                    DictToFill[mustIncludeOneOf] = 1;
                    budget -= mustIncludeOneOf.CostForAIToPurchase;
                    budgetSpent += mustIncludeOneOf.CostForAIToPurchase;
                    if ( tracing ) tracingBuffer.Add( "getWaveComposition: including a: " + mustIncludeOneOf.InternalName + " of strength  " + mustIncludeOneOf.MarkStatsFor( faction.CurrentGeneralMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership + " purchase cose " + mustIncludeOneOf.CostForAIToPurchase + " remaining budget after that: " + budget + "\n" );
                }
                debugCode = 1800;
                if ( forcedDire != null )
                {
                    debugCode = 1900;
                    DictToFill[forcedDire] = 1;
                    budget -= forcedDire.CostForAIToPurchase;
                    budgetSpent += forcedDire.CostForAIToPurchase;
                    if ( tracing ) tracingBuffer.Add( "getWaveComposition: including a: " + forcedDire.InternalName + " of strength  " + forcedDire.MarkStatsFor( faction.CurrentGeneralMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership + " purchase cose " + forcedDire.CostForAIToPurchase + " remaining budget after that: " + budget + "\n" );
                }
                
                debugCode = 2000;
                int MaxGuardianStrength = -1;
                if ( factionExternal == null )
                    MaxGuardianStrength = (budget / 2); //Defensive code
                else
                {
                    debugCode = 2100;
                    if ( factionExternal.AIDifficulty.PercentOfWaveBudgetThatCanBeGuardian >= FInt.Zero )
                    {
                        MaxGuardianStrength = (budget * factionExternal.AIDifficulty.PercentOfWaveBudgetThatCanBeGuardian).GetNearestIntPreferringHigher();
                    }
                    if ( factionExternal.AIType.IgnoreGuardianRestrictionsInWaves )
                        MaxGuardianStrength = -1; //nevermind, let it be the whole amount!
                }
                
                debugCode = 2200;
                int aiCostSpent = faction.FillComposition( Context, budget, MaxGuardianStrength, DictToFill, workingUnitsToSpawn, faction.CurrentGeneralMarkLevel, 0 );
                budgetSpent += aiCostSpent;
                debugCode = 2300;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in Getwavecomposition debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion end GetWaveComposition

        public void Helper_RemoveWaveForbiddenItemsFromWorkingDrawBag( ThrowawayDrawBagCanMemLeak<GameEntityTypeData> BagThatIsNotAPermanentOne )
        {
            for ( int i = BagThatIsNotAPermanentOne.InternalListSize - 1; i >= 0; i-- )
            {
                GameEntityTypeData type = BagThatIsNotAPermanentOne.GetItemByIndex( i );
                if ( type.IsDisallowedFromSpawningInWaves )
                    BagThatIsNotAPermanentOne.RemoveAnyItemsMatching( type );
            }
        }
    }
}
