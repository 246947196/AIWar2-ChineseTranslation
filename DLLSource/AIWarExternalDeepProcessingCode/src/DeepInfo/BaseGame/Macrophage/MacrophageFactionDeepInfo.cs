using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    // Regular Macrophage
    public class MacrophageFactionDeepInfo : MacrophageFactionDeepInfoBase //not sealed since the tamed version needs to inherit from them, too
    {
        public MacrophageFactionBaseInfoCore BaseInfo;
        public override void SubDoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<MacrophageFactionBaseInfo>();
        }

        protected override void SubCleanup() 
        {
            this.BaseInfo = null;
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 3;

        #region SeedStartingEntities_EarlyMajorFactionClaimsOnly
        public override void SeedStartingEntities_EarlyMajorFactionClaimsOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            Tutorial tutorialData = World_AIW2.Instance.TutorialOrNull;
            if ( tutorialData != null && tutorialData.SkipMacrophageTelium )
                return;
            bool isClustered = false;
            bool isLoner = false;

            switch ( AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "SpawningOptions", true ) )
            {
                case "Clustered Telia":
                    isClustered = true;
                    break;
                case "Lone Telium":
                    isLoner = true;
                    break;
                default:
                    break;
            }

            //infrequent enough that caching is pointless
            int intensity = AttachedFaction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
            int minToSeed = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MinInitialTelium" );
            int maxToSeed = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_Macrophage_MaxInitialTelium" );
            int numToSeed = (BaseInfo.EffectiveIntensity * maxToSeed) / 10;
            if ( numToSeed < minToSeed )
                numToSeed = minToSeed;
            if ( numToSeed > maxToSeed )
                numToSeed = maxToSeed;

            if ( isLoner )
                numToSeed = 1;
            else if ( isClustered )
                numToSeed /= 2;
            ThrowawayListCanMemLeak<Planet> planetsSeededOn = null;
            if ( AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "SeedOnNomadIfPossible", false ) ) //this field won't exist unless DLC2 is installed
                planetsSeededOn = StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, AttachedFaction, SpecialEntityType.None, MacrophageFactionBaseInfo.TeliumTag, SeedingType.HardcodedCount, numToSeed,
                                                                          MapGenCountPerPlanet.One, MapGenSeedStyle.FullUseByFactionOnNomadIfPossible, 3, 3, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal );
            else
                planetsSeededOn = StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, AttachedFaction, SpecialEntityType.None, MacrophageFactionBaseInfo.TeliumTag, SeedingType.HardcodedCount, numToSeed,
                                                                          MapGenCountPerPlanet.One, MapGenSeedStyle.FullUseByFaction, 3, 3, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal );
            //StandardMapPopulator.ClearAllUnitsNotBelongingToThisFaction( planetsSeededOn, faction, true );

            if ( isClustered && planetsSeededOn != null )
            {
                GameEntityTypeData teliumData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, MacrophageFactionBaseInfo.TeliumTag );
                for ( int x = 0; x < planetsSeededOn.Count; x++ )
                {
                    Planet workingPlanet = planetsSeededOn[x];
                    workingPlanet.Mapgen_SeedEntity( Context, AttachedFaction, teliumData, PlanetSeedingZone.MostAnywhere );
                }
            }
        }
        #endregion

        //Chris says: I honestly don't understand the meaning of this helper method, because I don't know what "valid" means in this context.
        //Based on a comment elsewhere, it seems like maybe this was just a way to deal with old data from old saves?  If so, we can remove it, yes?
        private static bool Helper_DoBasicEntityLRPValidityChecks( GameEntity_Squad entity )
        {
            //if ( entity.TypeData.GetHasTag( MacrophageFactionBaseInfo.TeliumTag ) || entity.TypeData.GetHasTag( MacrophageFactionBaseInfo.SpireTeliumTag ) )
            //    return DelReturn.Continue; Chris notes: this does not seem needed!  BADGER_TODO: please review
            if ( entity.CalculateFinalDestinationPlanetIndex_Safe() != -1 &&
                 entity.CalculateFinalDestinationPlanetIndex_Safe() != entity.GetPlanetIndexSafe() )
                return false; // this unit is en route to another planet
            return true;
        }

        //only used in this one thread, and is cleared there, so great!
        private static readonly List<ArcenPoint> WorkingMetalGeneratorList_Full = List<ArcenPoint>.Create_WillNeverBeGCed( 300, "MacrophageFactionDeepInfo-WorkingMetalGeneratorList_Full" );
        private static readonly List<ArcenPoint> WorkingMetalGeneratorList_ThatIAmNotNear = List<ArcenPoint>.Create_WillNeverBeGCed( 300, "MacrophageFactionDeepInfo-WorkingMetalGeneratorList_ThatIAmNotNear" );
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            int debugStage = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                debugStage = 100;
                debugStage = 1000;
                // If needed, recalculate valid spore gathering points planets.
                if ( BaseInfo.isUsingSmartExpansionLogic && (!GatheringPointsUpdated || SporeGatheringPoints == null) )
                {
                    CalculateGatheringPoints_NotThreadsafe( AttachedFaction, Context, pathingCacheData );
                    GatheringPointsUpdated = true;
                }

                List<Planet> workingPlanets = Planet.GetTemporaryPlanetList( "Macroph-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-workingPlanets", 10f );
                if ( workingPlanets == null ) //blocked for teardown/shutdown; bail
                    return;

                List<Planet> sporeGatheringPoints = this.SporeGatheringPoints.GetDisplayList();
                debugStage = 2000;
                //SporeTag
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( MacrophageFactionBaseInfo.SporeTag ) )
                {
                    debugStage = 2100;
                    if ( !Helper_DoBasicEntityLRPValidityChecks( entity ) )
                        continue;
                    if ( entity.Orders.GetQueuedOrderCount() > 0 )
                        continue; // this unit is en route on this planet

                    debugStage = 2200;
                    Planet destination = null;

                    // if we're within bounds for smart expansion logic, move to gathering points, otherwise, move randomly
                    if ( BaseInfo.isUsingSmartExpansionLogic && SporeGatheringPoints != null && SporeGatheringPoints.Count > 0 )
                    {
                        // move towards a nearby gathering point, prefering closer
                        workingPlanets.Clear();
                        int lowestHop = 10; // stop searching 2 hops beyond our first found planet; to allow variation
                        int workingHops = 1;

                        while ( workingHops - lowestHop < 1 && workingHops < 10 )
                        {
                            for ( int x = 0; x < sporeGatheringPoints.Count; x++ )
                            {
                                Planet workingPlanet = sporeGatheringPoints[x];
                                if ( entity.Planet == null || workingPlanet == null )
                                    continue;
                                if ( entity.Planet.GetHopsTo( workingPlanet ) <= workingHops )
                                {
                                    workingPlanets.Add( workingPlanet );
                                    lowestHop = Math.Min( entity.Planet.GetHopsTo( workingPlanet ), lowestHop );
                                }
                            }
                            workingHops++;
                        }

                        if ( workingPlanets.Count > 0 )
                            destination = workingPlanets[Context.RandomToUse.Next( workingPlanets.Count )];
                    }

                    debugStage = 2300;
                    // if no destination, pick one at random
                    if ( destination == null || destination == entity.Planet )
                    {
                        //picking at random in a fashion that doesn't use a List
                        foreach ( Planet neighbor in entity.Planet.LinkedNeighbors( false ) )
                        {
                                if ( Context.RandomToUse.Next( 0, 4 ) % 2 == 0 )
                                {
                                    destination = neighbor;
                                    break;
                                }
                        }
                    }
                    debugStage = 2400;
                    if ( destination == null )
                        continue;
                    debugStage = 2500;
                    PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "MacrophageLRP1", entity.Planet, destination, PathingMode.Default, Context, pathingCacheData );
                    if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                    {
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
                        command.RelatedString = "Phage_Spore";
                        command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                        for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                    }
                } //end MacrophageFactionBaseInfo.SporeTag

                Planet.ReleaseTemporaryPlanetList( workingPlanets );
                List<Planet> possiblePlanets = Planet.GetTemporaryPlanetList( "Macroph-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-possiblePlanets", 10f );
                if ( possiblePlanets == null ) //blocked for teardown/shutdown; bail
                    return;

                debugStage = 3000;
                //MacrophageFactionBaseInfo.HarvesterTag
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( MacrophageFactionBaseInfo.HarvesterTag ) )
                {
                    debugStage = 3100;
                    if ( !Helper_DoBasicEntityLRPValidityChecks( entity ) )
                        continue;
                    debugStage = 3200;
                    MacrophagePerHarvesterBaseInfo hData = entity.TryGetExternalBaseInfoAs<MacrophagePerHarvesterBaseInfo>();
                    debugStage = 3300;
                    //if we aren't doing something important, see if we're allowed a break
                    if ( !hData.IsHeadingTowardMetalGeneratorOnPlanet && !hData.ReturningToTelium )
                    {
                        int markModifier = (entity.CurrentMarkLevel - 1) * BaseInfo.SnackTimeIncreasePerMark;
                        if ( World_AIW2.Instance.GameSecond - entity.GameSecondCreated < Context.RandomToUse.Next( BaseInfo.MinimumSnackTimeOnSpawn + markModifier, BaseInfo.MaximumSnackTimeOnSpawn + markModifier ) )
                            continue; // this unit was just spawned in, and should hang out and chomp anything on its own planet for a while
                        if ( entity.PlanetFaction.DataByStance[FactionStance.Hostile].ThreatStrength > 1000 &&
                            World_AIW2.Instance.GameSecond - entity.GameSecondEnteredThisPlanet < Context.RandomToUse.Next( BaseInfo.MinimumSnackTimeOnNewPlanet + markModifier, BaseInfo.MaximumSnackTimeOnNewPlanet + markModifier ) )
                            continue; // this unit should rage out on its planet for a while
                        markModifier = (entity.CurrentMarkLevel - 1) * BaseInfo.BreakTimeIncreasePerMark;
                        if ( World_AIW2.Instance.GameSecond - entity.GameSecondEnteredThisPlanet < Context.RandomToUse.Next( BaseInfo.MinimumBreakTime + markModifier, BaseInfo.MaximumBreakTime + markModifier ) )
                            continue; // this unit should wait for a short time to see if anything it can eat arrives before moving on
                    }
                    debugStage = 3400;
                    if ( hData.ReturningToTelium )
                    {
                        //Go to the Telium's planet if we aren't there
                        //If we are on the Telium's planet, go right to the telium
                        GameEntity_Squad telium = GetTeliumForHarvester( entity, hData );
                        if ( telium == null )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "BUG: no telium for harvester on " + entity.GetPlanetName_Safe(), Verbosity.DoNotShow );
                            continue;
                        }
                        if ( entity.Planet == telium.Planet )
                        {
                            if ( MacrophageFactionBaseInfo.debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Harvester " + entity.PrimaryKeyID + " heading to telium on this planet", Verbosity.DoNotShow );
                            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
                            command.ToBeQueued = false;
                            command.RelatedPoints.Add( telium.WorldLocation );
                            command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                            bool playAudioEffectForCommand = false;
                            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, playAudioEffectForCommand );
                            continue;
                        }
                        else
                        {
                            if ( MacrophageFactionBaseInfo.debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Harvester " + entity.PrimaryKeyID + " heading to telium on " + telium.GetPlanetName_Safe(), Verbosity.DoNotShow );

                            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "MacrophageLRP2", entity.Planet, telium.Planet, PathingMode.Default, Context, pathingCacheData );
                            if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                            {
                                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
                                command.ToBeQueued = false;
                                command.RelatedString = "Phage_ToTelium";
                                command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                                for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                                    command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                            }
                            continue;
                        }
                    }
                    debugStage = 3500;
                    if ( hData.HasVisitedMetalGeneratorOnPlanet )
                    {
                        // Allow Harvesters to traverse X/2 hops from their Telium, where X is equal the number of harvesters that telium owns
                        int hopLimit = 0;
                        Planet teliumPlanet = null;

                        GameEntity_Squad telium = GetTeliumForHarvester( entity, hData );
                        if ( telium != null )
                        {
                            MacrophagePerTeliumBaseInfo tData = telium.TryGetExternalBaseInfoAs<MacrophagePerTeliumBaseInfo>();
                            int divisor = 2;
                            // If we're a loner, further restrict our hop limit.
                            if ( BaseInfo.isLoner )
                                divisor = 4;
                            hopLimit = Math.Max( 0, tData.CurrentHarvesters / divisor );
                            teliumPlanet = telium.Planet;
                        }

                        if ( hopLimit == 0 && telium != null )
                        {
                            if ( entity.Planet == telium.Planet )
                            {
                                // If we are restricted to our telium planet, and are already on said planet, reset our mine booleans to continue moving around our current planet.
                                hData.HasVisitedMetalGeneratorOnPlanet = false;
                                if ( MacrophageFactionBaseInfo.debug )
                                    if ( BaseInfo.isLoner )
                                        ArcenDebugging.ArcenDebugLogSingleLine( "Harvester " + entity.PrimaryKeyID + " on " + entity.GetPlanetName_Safe() + " is staying on its telium's planet due to it being a loner with only a few harvesters", Verbosity.DoNotShow );
                                    else
                                        ArcenDebugging.ArcenDebugLogSingleLine( "Harvester " + entity.PrimaryKeyID + " on " + entity.GetPlanetName_Safe() + " is the only harvester for its telium, and is remaining on its telium's planet", Verbosity.DoNotShow );
                                continue;
                            }
                            else
                            {
                                // If we are restricted to our telium planet, and are not on our telium's planet, move to it.
                                hData.ReturningToTelium = true;
                                if ( MacrophageFactionBaseInfo.debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Harvester " + entity.PrimaryKeyID + " on " + entity.GetPlanetName_Safe() + " is the only harvester for its telium, and is returning to its telium's planet", Verbosity.DoNotShow );
                                continue;
                            }
                        }
                        else
                        {
                            // We're free from our Telium's home planet. Attempt to find an adjacent planet thats within our hop limit.

                            // ArcenDebugging.ArcenDebugLogSingleLine("The macrophage on " + entity.GetPlanetName_Safe() + " has visited a metal generator, so head toward a new planet", Verbosity.DoNotShow );
                            //Go to the next adjacent planet
                            Planet destination = null;
                            possiblePlanets.Clear();
                            KeyValuePair<Planet, int> weakestPlanet = new KeyValuePair<Planet, int>( null, 0 );
                            foreach ( Planet neighbor in entity.Planet.LinkedNeighbors( false ) )
                            {
                                if ( telium == null || telium.Planet.GetHopsTo( neighbor ) <= hopLimit )
                                {
                                    bool isPlayerHome = false;
                                    // Do not enter a planet if its mark is over twice ours. Consider the human home command a mark of 7 for this purpose.
                                    if ( entity.CurrentMarkLevel >= 4 )
                                        // If we're at least mark 4, we're brave enough to traverse anywhere.
                                        possiblePlanets.Add( neighbor );
                                    else
                                    {
                                        int markOfPlanet = 0;
                                        switch ( neighbor.GetControllingFactionType() )
                                        {
                                            case FactionType.Player:
                                                if ( neighbor.GetCommandStationOrNull() != null && neighbor.GetCommandStationOrNull().TypeData.SpecialType == SpecialEntityType.HumanHomeCommand )
                                                {
                                                    markOfPlanet = 7;
                                                    isPlayerHome = true;
                                                }
                                                else
                                                    markOfPlanet = 4;
                                                break;
                                            case FactionType.AI:
                                                markOfPlanet = neighbor.MarkLevelForAIOnly.Ordinal;
                                                break;
                                            default:
                                                markOfPlanet = 0;
                                                break;
                                        }
                                        if ( entity.CurrentMarkLevel * 2 >= markOfPlanet )
                                            possiblePlanets.Add( neighbor );
                                    }
                                    // If we don't have any possible planets, update our weakest planet.
                                    if ( possiblePlanets.Count == 0 && !isPlayerHome )
                                    {
                                        int threat = neighbor.GetDataByStanceForFaction( AttachedFaction, FactionStance.Hostile ).TotalStrength;
                                        if ( weakestPlanet.Key == null )
                                        {
                                            // No entry yet; default to this no matter what.
                                            weakestPlanet = new KeyValuePair<Planet, int>( neighbor, threat );
                                        }
                                        else if ( threat < weakestPlanet.Value )
                                        {
                                            // This planet is weaker than our current weakest. Update it.
                                            weakestPlanet = new KeyValuePair<Planet, int>( neighbor, threat );
                                        }
                                    }
                                }
                            }

                            // If no valid planets, pick the lowest strength adjacent planet instead.
                            if ( possiblePlanets.Count == 0 )
                            {
                                // In the very rare case where we do not have a valid target, default to our telium.
                                if ( weakestPlanet.Key == null )
                                    destination = telium.Planet;
                                else
                                    destination = weakestPlanet.Key;
                            }
                            else
                                destination = possiblePlanets[Context.RandomToUse.Next( possiblePlanets.Count )];

                            if ( destination == null )
                            {
                                if ( MacrophageFactionBaseInfo.debug )
                                {
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Harvester " + entity.PrimaryKeyID + " on " + entity.GetPlanetName_Safe() + " just chill, we are only flipping coins to move", Verbosity.DoNotShow );
                                }
                                continue;
                            }
                            if ( MacrophageFactionBaseInfo.debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Harvester " + entity.PrimaryKeyID + " on " + entity.GetPlanetName_Safe() + " choosing random planet " + destination.Name, Verbosity.DoNotShow );

                            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
                            command.RelatedString = "Phage_Harvest";
                            command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                            command.RelatedIntegers.Add( destination.Index );
                            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                            hData.HasVisitedMetalGeneratorOnPlanet = false; //for the new planet
                            continue;
                        }
                    }
                    debugStage = 3600;
                    if ( !hData.HasVisitedMetalGeneratorOnPlanet && hData.IsHeadingTowardMetalGeneratorOnPlanet == false )
                    {
                        //    ArcenDebugging.ArcenDebugLogSingleLine("The macrophage on " + entity.GetPlanetName_Safe() + " has not visited a metal generator yet, so head toward one", Verbosity.DoNotShow );
                        //Head toward a random metal harvester. TODO: replace this with a call to GetRandomMetalGeneratorOnPlanet()
                        WorkingMetalGeneratorList_Full.Clear();
                        WorkingMetalGeneratorList_ThatIAmNotNear.Clear();
                        foreach ( GameEntity_Squad generator in entity.Planet.Squads( "MetalGenerator" ) ) //note: this DOES work for both distributed economy mode and regular
                        {
                            WorkingMetalGeneratorList_Full.Add( generator.WorldLocation );
                            if ( generator.WorldLocation.GetExtremelyRoughDistanceTo( entity.WorldLocation ) >= 5000 )
                                WorkingMetalGeneratorList_ThatIAmNotNear.Add( generator.WorldLocation );
                        }
                        if ( WorkingMetalGeneratorList_Full.Count == 0 )
                        {
                            //ArcenDebugging.ArcenDebugLogSingleLine( "Potential bug: planet " + entity.GetPlanetName_Safe() + " has no metal generators. This is unexpected", Verbosity.DoNotShow );
                            hData.HasVisitedMetalGeneratorOnPlanet = true;
                            continue;
                        }
                        if ( WorkingMetalGeneratorList_ThatIAmNotNear.Count == 0 ) //I'm satisfied, stay near stuff
                        {
                            hData.HasVisitedMetalGeneratorOnPlanet = true;
                            continue;
                        }
                        ArcenPoint chosenPoint = WorkingMetalGeneratorList_ThatIAmNotNear[Context.RandomToUse.Next( 0, WorkingMetalGeneratorList_ThatIAmNotNear.Count )];
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCWander], GameCommandSource.AnythingElse );
                        command.ToBeQueued = true;
                        command.RelatedPoints.Add( chosenPoint );
                        command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                        hData.IsHeadingTowardMetalGeneratorOnPlanet = true;
                        continue;
                    }
                    debugStage = 3700;
                    if ( !hData.HasVisitedMetalGeneratorOnPlanet && hData.IsHeadingTowardMetalGeneratorOnPlanet == true )
                    {
                        //check if we have reached that metal harvester
                        int rangeForMetalGenerator = 400;
                        if ( Mat.DistanceBetweenPointsImprecise( entity.WorldLocation, entity.CalculateDestinationPoint_Safe() ) < rangeForMetalGenerator )
                        {
                            //        ArcenDebugging.ArcenDebugLogSingleLine("The macrophage on " + entity.GetPlanetName_Safe() + " has just reached a metal generator", Verbosity.DoNotShow );
                            hData.HasVisitedMetalGeneratorOnPlanet = true;
                            hData.IsHeadingTowardMetalGeneratorOnPlanet = false;
                            hData.CurrentMetal += BaseInfo.MetalGainedFromVisitingMine;
                        }
                        continue;
                    }
                } //endMacrophageFactionBaseInfo.HarvesterTag

                Planet.ReleaseTemporaryPlanetList( possiblePlanets );

                debugStage = 4000;
                // EnragedHarvesterTag
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( MacrophageFactionBaseInfo.EnragedHarvesterTag ) )
                {
                    debugStage = 4100;
                    if ( !Helper_DoBasicEntityLRPValidityChecks( entity ) )
                        continue;
                    debugStage = 4200;
                    // Simplistic fallback logic for enraged harvester movement. This should only ever occur on old saves.
                    if ( World_AIW2.Instance.GameSecond - entity.GameSecondCreated < Context.RandomToUse.Next( BaseInfo.MinimumSnackTimeOnSpawn, BaseInfo.MaximumSnackTimeOnSpawn ) )
                        continue; // this unit was just spawned in, and should hang out and chomp anything on its own planet for a while
                    if ( entity.PlanetFaction.DataByStance[FactionStance.Hostile].ThreatStrength > 1000 &&
                        World_AIW2.Instance.GameSecond - entity.GameSecondEnteredThisPlanet < Context.RandomToUse.Next( BaseInfo.MinimumSnackTimeOnNewPlanet, BaseInfo.MaximumSnackTimeOnNewPlanet ) )
                        continue; // this unit should rage out on its planet for a while
                    if ( World_AIW2.Instance.GameSecond - entity.GameSecondEnteredThisPlanet < Context.RandomToUse.Next( BaseInfo.MinimumBreakTime, BaseInfo.MaximumBreakTime ) )
                        continue; // this unit should wait for a short time to see if anything it can eat arrives before moving on
                    debugStage = 4300;
                    Planet kingPlanet = null;
                    if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                        kingPlanet = FactionUtilityMethods.Instance.findHumanKing( false );
                    else
                        kingPlanet = FactionUtilityMethods.Instance.findAIKing( false );
                    debugStage = 4400;
                    if ( kingPlanet == null )
                    {
                        if ( MacrophageFactionBaseInfo.debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "The king is dead, so just chill", Verbosity.DoNotShow );
                        continue;
                    }
                    if ( MacrophageFactionBaseInfo.debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Enraged Macrophage " + entity.PrimaryKeyID + " on " + entity.GetPlanetName_Safe() + " is heading towards a king at " + kingPlanet.Name, Verbosity.DoNotShow );
                    debugStage = 4500;
                    PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "MacrophageLRP3", entity.Planet, kingPlanet, PathingMode.Default, Context, pathingCacheData );
                    debugStage = 4600;
                    if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                    {
                        debugStage = 4700;
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
                        command.RelatedString = "Phage_Rage";
                        command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                        for ( int k = 0; k < pathCache.PathToReadOnly.Count && command.RelatedIntegers.Count < 2; k++ )
                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                    }
                } //end EnragedHarvesterTag
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Error in Macrophage long range planning at debugStage " + debugStage + " Error: " + e, Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();
            }
        }

        public override void DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly( ref int debugStage, GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull,
              Faction factionThatKilledEntity, Faction entityOwningFaction, int numExtraStacksKilled, ArcenHostOnlySimContext Context )
        {
            if ( this.BaseInfo.Harvesters.Count <= 0 && entity == null )
                return; // Do not process if we have no consumers active.
            try
            {
                //Handle Macrophage for any ship that dies, not just ours.  But make the super basic check very quick
                if ( FiringSystemOrNull != null && MacrophageFactionBaseInfo.AllRegularMacrophageFactions.Count > 0 )
                {
                    debugStage = 5900;
                    //if the Macrophage is enabled and the dying unit is a ship
                    GameEntity_Squad EntityThatKilledTarget = FiringSystemOrNull.ParentEntity;
                    //if WE were the faction who killed this ship that is dying, then check further.  Otherwise ignore this death.
                    if ( EntityThatKilledTarget != null && EntityThatKilledTarget.GetFactionOrNull_Safe() == AttachedFaction )
                    {
                        debugStage = 6000;
                        if ( EntityThatKilledTarget.TypeData.GetHasTag( MacrophageFactionBaseInfo.HarvesterTag ) )
                        {
                            debugStage = 6100;
                            MacrophagePerHarvesterBaseInfo pData = EntityThatKilledTarget.GetExternalBaseInfoAs<MacrophagePerHarvesterBaseInfo>();
                            if ( pData != null )
                            {
                                debugStage = 6110;
                                int metalGained = entity.GetStrengthPerSquad() + entity.GetStrengthPerSquad() * numExtraStacksKilled;
                                metalGained *= BaseInfo.HarvesterMetalFromKillMultiplier;
                                pData.CurrentMetal += metalGained;
                                pData.TotalMetalEverCollected += metalGained;
                            }
                        }
                        else if ( EntityThatKilledTarget.TypeData.GetHasTag( MacrophageFactionBaseInfo.SpireTeliumTag ) )
                        {
                            debugStage = 6200;
                            MacrophagePerTeliumBaseInfo tData = EntityThatKilledTarget.GetExternalBaseInfoAs<MacrophagePerTeliumBaseInfo>();
                            if ( tData != null )
                            {
                                debugStage = 6210;
                                int metalGained = entity.GetStrengthPerSquad() + entity.GetStrengthPerSquad() * numExtraStacksKilled;
                                metalGained *= BaseInfo.HarvesterMetalFromKillMultiplier;
                                tData.CurrentMetal += metalGained;
                                tData.TotalMetalEverCollected += metalGained;
                            }
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in SpecialFaction_Macrophage.DoOnAnyDeathLogic_HostOnly_FromCentralLoop_NotJustMyOwnShips stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
        }
        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            if ( entity == null )
                return;

            int debugStage = 0;
            try
            {
                debugStage = 100;
                // Telium died; tell the game to update our spore gathering points.
                if ( entity.TypeData.GetHasTag( MacrophageFactionBaseInfo.TeliumTag ) )
                    GatheringPointsUpdated = false;
                debugStage = 200;
                if ( entity.TypeData.GetHasTag( MacrophageFactionBaseInfo.SpireTeliumTag ) )
                {
                    debugStage = 300;
                    Faction facOrNull = entity.GetFactionOrNull_Safe();
                    if ( facOrNull != null )
                        facOrNull.HasObtainedSpireDebris = false;
                    GatheringPointsUpdated = false;
                    debugStage = 400;
                    if ( ArcenNetworkAuthority.GetIsHostMode() )
                        World_AIW2.Instance.QueueChatMessageOrCommand( entity.StartFactionColourForLog_Safe() + "Macrophage</color> have lost access to Debris, and can no longer produce Spire Infested Harvesters.", 
                            ChatType.LogToCentralChat, null );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in SpecialFaction_Macrophage.DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
        }
        public override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
			// If we're effectively dead, stop processing. Still initialize just in case we somehow come back via rogue Spore.
            if ( BaseInfo.Telia.Count <= 0 )
                return;

            //we only want to call SetExternalData when something has changed
            //bool harvesterExternalDataUpdateRequired = false;
            //bool sporeExternalDataUpdateRequired = false;
            int rangeForMetalDeposit = 600;

            if ( BaseInfo.MetalHarvesterCanHold <= 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: failed to parse MetalHarvesterCanHold: " + BaseInfo.MetalHarvesterCanHold + " forevent " + BaseInfo.GetEventCost( 0, null ) + " this indicates something wrong with the ExternalConstants xml", Verbosity.DoNotShow );
                return;
            }
            if ( BaseInfo.HarvesterLimitBeforeEnraging < BaseInfo.HarvestersToEnrage )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "HarvestersToEnrage must be < HarvesterLimit", Verbosity.DoNotShow );
                return;
            }
            if ( BaseInfo.SpireHarvesterLimitBeforeEnraging < BaseInfo.SpireHarvestersToEnrage )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "SpireHarvestersToEnrage must be < SpireHarvesterLimit", Verbosity.DoNotShow );
                return;
            }

            // Reset all of our Harvester data.
            List<SafeSquadWrapper> harvesters = this.BaseInfo.Harvesters.GetDisplayList();
            for (int i = 0; i < harvesters.Count; i++ )
            {
                GameEntity_Squad entity = harvesters[i].GetSquad();
                if (entity == null )
                    continue;
                MacrophagePerHarvesterBaseInfo hData = entity.GetExternalBaseInfoAs<MacrophagePerHarvesterBaseInfo>();

                hData.HasLivingTelium = false; //we check later to see if this has a living Telium. If not then Enrage it
                if ( hData.CurrentMetal >= BaseInfo.MetalHarvesterCanHold )
                {
                    if ( MacrophageFactionBaseInfo.debug && !hData.ReturningToTelium )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Harvester " + entity.PrimaryKeyID + " has " + hData.CurrentMetal + " which is >= " + BaseInfo.MetalHarvesterCanHold + " head back to telium", Verbosity.DoNotShow );
                    hData.ReturningToTelium = true;
                }
            }

            // If we're not Tamed, see if we should berserk.
            if ( !this.BaseInfo.IsTamed && BaseInfo.canBerserk )
            {
                // Berserk if:
                // We have less than 3 Telium left in our faction.
                // The Tamed subfaction has more Telia than us.
                // We're a Lone Telium subfaction.
                if ( BaseInfo.isLoner )
                    BaseInfo.isBerserk = true;
                else if ( BaseInfo.Telia.Count < 3 )
                {
                    BaseInfo.isBerserk = true;
                    if ( World_AIW2.Instance.GameSecond - BaseInfo.GameSecondForLastMessage > 600 )
                    {
                        BaseInfo.GameSecondForLastMessage = World_AIW2.Instance.GameSecond;
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                            World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + 
                                "Macrophage</color> have gone berserk due to their dwindling Telium population, reducing their metal costs by 75%!", ChatType.LogToCentralChat, null );
                    }
                }
                else if ( MacrophageTamedFactionBaseInfo.Instance != null && MacrophageTamedFactionBaseInfo.Instance.Telia.Count > BaseInfo.Telia.Count )
                {
                    BaseInfo.isBerserk = true;
                    if ( World_AIW2.Instance.GameSecond - BaseInfo.GameSecondForLastMessage > 600 )
                    {
                        BaseInfo.GameSecondForLastMessage = World_AIW2.Instance.GameSecond;
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                            World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + 
                                "Macrophage</color> have gone berserk due to the expansion of the Tamed Macrophage, reducing their metal costs by 75%!", ChatType.LogToCentralChat, null );
                    }
                }
                else
                {
                    if ( BaseInfo.isBerserk )
                    {
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                            World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Macrophage</color> are no longer berserk.", ChatType.LogToCentralChat, null );
                        BaseInfo.isBerserk = false;
                    }
                }
            }

            // If we can, see if we should be using smart expansion logic.
            if ( BaseInfo.canUseSmartExpansionLogic && BaseInfo.Telia.Count < BaseInfo.MaxTeliaForSmartExpansionLogic )
                BaseInfo.isUsingSmartExpansionLogic = true;
            else
                BaseInfo.isUsingSmartExpansionLogic = false;

            // If we don't yet have one, and we should have one, convert a Telium into a Spire Telium.
            if ( BaseInfo.SpireTelia.Count == 0 && AttachedFaction.HasObtainedSpireDebris )
            {
                GameEntityTypeData spireTelium = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, MacrophageFactionBaseInfo.SpireTeliumTag );
                if ( spireTelium == null )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Error, unable to find Spire Infested Telium.", Verbosity.ShowAsError );
                else
                {
                    GameEntity_Squad oldTelium = BaseInfo.Telia.GetDisplayList()[Context.RandomToUse.Next( BaseInfo.Telia.Count )].GetSquad();
                    if ( oldTelium != null )
                    {
                        GameEntity_Squad newTelium = oldTelium.TransformInto( Context, spireTelium, 1, oldTelium.TypeData.KeepDamageAndDebuffsOnTransformation );
                        oldTelium.Die( Context, true );
                        newTelium.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                            if ( chatHandlerOrNull != null )
                                chatHandlerOrNull.PlanetToView = newTelium.Planet;

                            World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Macrophage</color> have infested one of their Telium on " +
                                newTelium.GetPlanetName_Safe() + "!", ChatType.LogToCentralChat, chatHandlerOrNull );
                        }
                        //TEACHING_MOMENT: we have a new spire telia, and this telia above just died.  But we didn't update any lists!
                        //That's okay, that's the ideal of how this should work.  Next time Stage2 runs, it will update both.
                        //We have zero mechanisms for updating that in a threadsafe way right now, but it doesn't matter because we
                        //won't be making any choices based on "hey we need another spire telium because we have none" until after the next Stage2 run.
                        //If we WERE going to make choices based on that, then we'd have to do list Zenith Architrave and have temporary
                        //working lists that we first fill from the Stage2 results, and that we manage on our thread here to know where we stand.
                        //It's so much nicer when we don't have to do that, but either pattern is fine.
                    }
                }
            }

            // Keep track of how many Harvesters we enrage to let the player know.
            int knownEnraged = 0, unknownEnraged = 0;
            //Now that the Telia list is set, handle its harvesters
            List<SafeSquadWrapper> telia = this.BaseInfo.Telia.GetDisplayList();
            for ( int i = 0; i < telia.Count; i++ )
            {
                GameEntity_Squad telium = telia[i].GetSquad();
                if ( telium == null )
                    continue;
                MacrophagePerTeliumBaseInfo tData = telium.CreateExternalBaseInfo<MacrophagePerTeliumBaseInfo>( "MacrophagePerTeliumBaseInfo" );
                int numHarvestersForThisTelium = 0;
                bool preferToSpawnHarvester = false; //if no harvesters and we have metal, spawn one

                for ( int j = 0; j < harvesters.Count; j++ )
                {
                    GameEntity_Squad harvester = harvesters[j].GetSquad();
                    if ( harvester == null )
                        continue;
                    MacrophagePerHarvesterBaseInfo hData = harvester.GetExternalBaseInfoAs<MacrophagePerHarvesterBaseInfo>();

                    if ( hData.TeliumID == telium.PrimaryKeyID )
                    {
                        //If this harvester belongs to this Telium, remove it from the general list
                        //and add it to the specific list for this telium
                        hData.HasLivingTelium = true;
                        if ( Mat.DistanceBetweenPointsImprecise( harvester.WorldLocation, telium.WorldLocation ) < rangeForMetalDeposit )
                        {
                            // Macrophage has returned to its Telium with enough metal. Transfer metal, attempt to mark up, and restore shields.
                            tData.TotalMetalEverCollected += hData.CurrentMetal;
                            tData.CurrentMetal += hData.CurrentMetal;
                            if ( MacrophageFactionBaseInfo.debug && hData.CurrentMetal > 0 )
                                ArcenDebugging.ArcenDebugLogSingleLine( "FLAGFLAGFLAG harvester " + harvester.PrimaryKeyID + " has arrived to the telium at " + harvester.GetPlanetName_Safe() + " to deposit " + hData.CurrentMetal + ". The telium now has " + tData.CurrentMetal, Verbosity.DoNotShow );

                            // If a full delivery, restore shields and let them have a chance to mark up. Default chance of 50%, reduced by 9% for every mark above 1.
                            if ( hData.CurrentMetal >= BaseInfo.MetalHarvesterCanHold )
                            {
                                if ( harvester.CurrentMarkLevel < 7 )
                                {
                                    // Roll our virtual 100 sided die.
                                    byte chance = (byte)(BaseInfo.BasePercentChanceToMarkUp - ((harvester.CurrentMarkLevel - 1) * BaseInfo.ReductionPerMarkForPercentChanceToMarkUp) );
                                    if ( Context.RandomToUse.Next( 0, 100 ) < chance )
                                    {
                                        harvester.SetCurrentMarkLevel( (byte)(harvester.CurrentMarkLevel + 1) );
                                        if ( MacrophageFactionBaseInfo.debug )
                                            ArcenDebugging.ArcenDebugLogSingleLine( "Harvester " + harvester.PrimaryKeyID + " has marked up with a chance of " + chance + "%", Verbosity.DoNotShow );
                                    }
                                    else if ( MacrophageFactionBaseInfo.debug )
                                        ArcenDebugging.ArcenDebugLogSingleLine( "Harvester " + harvester.PrimaryKeyID + " has failed to mark up with a chance of " + chance + "%", Verbosity.DoNotShow );
                                }

                                // Restore its shields
                                harvester.TakeShieldRepair( harvester.TypeData.GetForMark( harvester.CurrentMarkLevel ).BaseShieldPoints );
                            }

                            hData.CurrentMetal = 0;
                            hData.ReturningToTelium = false;
                        }
                        numHarvestersForThisTelium++;
                    }
                }

                tData.CurrentHarvesters = numHarvestersForThisTelium;
                if ( numHarvestersForThisTelium > 0 )
                {
                    tData.TimeLastHadHarvester = World_AIW2.Instance.GameSecond;
                    int harvesterLimit;
                    if ( BaseInfo.isLoner )
                        harvesterLimit = 99999;
                    else if ( telium.TypeData.GetHasTag( MacrophageFactionBaseInfo.SpireTeliumTag ) )
                        harvesterLimit = BaseInfo.SpireHarvesterLimitBeforeEnraging;
                    else
                        harvesterLimit = BaseInfo.HarvesterLimitBeforeEnraging;
                    if ( numHarvestersForThisTelium >= harvesterLimit )
                    {
                        int harvestersToEnrage;
                        if ( telium.TypeData.GetHasTag( MacrophageFactionBaseInfo.SpireTeliumTag ) )
                            harvestersToEnrage = BaseInfo.SpireHarvestersToEnrage;
                        else
                            harvestersToEnrage = BaseInfo.HarvestersToEnrage;
                        for ( int j = 0; j < harvesters.Count; j++ )
                        {
                            GameEntity_Squad harvester = harvesters[j].GetSquad();
                            if ( harvester == null )
                                continue;
                            MacrophagePerHarvesterBaseInfo hData = harvester.GetExternalBaseInfoAs<MacrophagePerHarvesterBaseInfo>();

                            if ( hData.TeliumID == telium.PrimaryKeyID )
                            {
                                if ( MacrophageFactionBaseInfo.debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "telium " + telium.PrimaryKeyID + " on " + telium.GetPlanetName_Safe() + " is now enraging " + harvestersToEnrage + " harvesters", Verbosity.DoNotShow );

                                if ( harvester.Planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                                    knownEnraged++;
                                else
                                    unknownEnraged++;
                                EnrageHarvester( Context, harvester );
                                harvestersToEnrage--;
                            }
                            if ( harvestersToEnrage <= 0 )
                                break;
                        }
                    }
                }
                else
                {
                    preferToSpawnHarvester = true;
                }
                if ( numHarvestersForThisTelium > 0 )
                    tData.CurrentMetal += (BaseInfo.MetalGenerationPerSecond * BaseInfo.MetalGenerationMultiplierWithLivingHarvesters).IntValue;
                else
                    tData.CurrentMetal += BaseInfo.MetalGenerationPerSecond;
                // Get our event cost, and save it to our telium for display purposes.
                int cost = BaseInfo.GetEventCost( numHarvestersForThisTelium, telium );
                tData.MetalForNextBuild = cost;

                //If we have enough metal at the Telium, spawn spores or a new Harvester
                if ( tData.CurrentMetal >= cost )
                {
                    // Spore chance:
                    // 0% if no harvesters
                    // 50% + 10% for every Harvester after the first
                    // -2% per Telia in the galaxy
                    // Clamped between 25% and 75%
                    int sporeChance = BaseInfo.GetSporeChance( numHarvestersForThisTelium );
                    if ( BaseInfo.isLoner )
                        sporeChance = -1; // Don't try to expand if we're a loner.
                    if ( Context.RandomToUse.Next( 0, 100 ) < sporeChance && !preferToSpawnHarvester )
                    {
                        int sporesToRelease;
                        if ( telium.TypeData.GetHasTag( MacrophageFactionBaseInfo.SpireTeliumTag ) )
                            sporesToRelease = BaseInfo.SpireSporesPerRelease;
                        else
                            sporesToRelease = BaseInfo.SporesPerRelease;
                        if ( MacrophageFactionBaseInfo.debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "We have " + tData.CurrentMetal + " let's spawn " + BaseInfo.SporesPerRelease + " spores", Verbosity.DoNotShow );
                        SpawnSpores( Context, sporesToRelease, telium );
                        tData.CurrentMetal = 0;
                    }
                    else
                    {
                        if ( MacrophageFactionBaseInfo.debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "We have " + tData.CurrentMetal + " let's spawn " + 1 + " harvester", Verbosity.DoNotShow );
                        /*GameEntity harvester = */
                        SpawnNewHarvester( Context, telium, MacrophageFactionBaseInfo.debug );
                        tData.CurrentMetal = 0;
                    }
                }
                //If we want to spawn a harvester before the accumulation of metal would typically allow, handle that  here
                //this is set in the XML
                if ( World_AIW2.Instance.GameSecond == BaseInfo.EarlyHarvesterSpawnTime )
                {
                    SpawnNewHarvester( Context, telium, MacrophageFactionBaseInfo.debug );
                }
            }
            for ( int j = 0; j < harvesters.Count; j++ )
            {
                GameEntity_Squad harvester = harvesters[j].GetSquad();
                if ( harvester == null )
                    continue;
                MacrophagePerHarvesterBaseInfo hData = harvester.GetExternalBaseInfoAs<MacrophagePerHarvesterBaseInfo>();
                if ( hData.HasLivingTelium == false )
                {
                    if ( MacrophageFactionBaseInfo.debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Macrophage harvester " + harvester.PrimaryKeyID + " on " + harvester.GetPlanetName_Safe() + " previously belonging to " + hData.TeliumID + " has just discovered its Telium is dead. Enrage it", Verbosity.DoNotShow );
                    if ( harvester.Planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                        knownEnraged++;
                    else
                        unknownEnraged++;
                    EnrageHarvester( Context, harvester );
                }
            }

            // If needed, inform player about angry harvesters. Do it once to cut down on spam.
            if (unknownEnraged > 0 || knownEnraged > 0 )
            {
                if ( ArcenNetworkAuthority.GetIsHostMode() ) //message gets sent from host to all clients, so we only send on host to not make duplicates
                {
                    if ( knownEnraged <= 0 )
                        World_AIW2.Instance.QueueChatMessageOrCommand( "An unknown number of " + AttachedFaction.StartFactionColourForLog() + "Macrophage</color> have Enraged!",
                            ChatType.LogToCentralChat, "ArkChiefOfStaff_MacrophageAttacking", null );
                    else
                        World_AIW2.Instance.QueueChatMessageOrCommand( "At least " + knownEnraged + " " + AttachedFaction.StartFactionColourForLog() + "Macrophage</color> have Enraged!", 
                            ChatType.LogToCentralChat, "ArkChiefOfStaff_MacrophageAttacking", null );
                }
            }

            SpawnTeliumIfAble( Context );
        }

        
        public void SpawnNewHarvester( ArcenHostOnlySimContext Context, GameEntity_Squad telium, bool debug, byte markLevel = 1 )
        {
            GameEntityTypeData entityData;
            if ( telium.TypeData.GetHasTag( MacrophageFactionBaseInfo.SpireTeliumTag ) ) // Fallen Spire enabled, and a Spire Telium. Always a Spire Harvester.
                entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, MacrophageFactionBaseInfo.SpireHarvesterTag );
            else if ( AttachedFaction.HasObtainedSpireDebris ) // Fallen Spire enabled, and debris acquired. Chance of being a Spire Harvester.
                entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context,MacrophageFactionBaseInfo.HarvesterTag );
            else
                entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, MacrophageFactionBaseInfo.RegularHarvesterTag );
            ArcenPoint spawnLocation = telium.WorldLocation;
            PlanetFaction pFaction = telium.Planet.GetPlanetFactionForFaction( AttachedFaction );
            if ( entityData == null )
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: no MacrophageHarvestern tag found", Verbosity.DoNotShow );
            GameEntity_Squad harvester = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, markLevel,
                pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "Macrophage-Harvester" );
            if ( harvester == null )
                return;

            harvester.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
            MacrophagePerHarvesterBaseInfo hData = harvester.CreateExternalBaseInfo<MacrophagePerHarvesterBaseInfo>( "MacrophagePerHarvesterBaseInfo" );
            hData.TeliumID = telium.PrimaryKeyID;
            hData.TotalMetalEverCollected = 0;
            hData.CurrentMetal = 0;
            harvester.ShouldNotBeConsideredAsThreatToHumanTeam = true;
            if ( MacrophageFactionBaseInfo.debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Spawned a new harvester " + harvester.PrimaryKeyID + " for telium " + telium.PrimaryKeyID, Verbosity.DoNotShow );
        }

        private void EnrageHarvester( ArcenHostOnlySimContext Context, GameEntity_Squad harvester )
        {
            //TEACHING_MOMENT: This "MacrophageFactionDeepInfo" that we are in right now might be a regular faction,
            //Or it might be a tamed or enraged faction.  How do we tell?  Call BaseInfo.IsEnraged or BaseInfo.IsTamed, and boom you now know.
            //But usually we don't care.  That said, sometimes we want to get a reference to the enraged faction specifically, or the tamed one.
            //Well, unlike the central macrophage, of which there can be many instances, there is only a single one of each of those.
            //So you can super easily just say MacrophageEnragedFactionBaseInfo.Instance and talk about the enraged version.  If that is null
            //then we have a fundamental setup error and that just needs to be fixed in the xml once and we're good.  It's not the sort of thing.
            //that we need to worry about suddenly happening during runtime.  It's actually pretty paranoid of me to check it below like this.
            if ( MacrophageEnragedFactionBaseInfo.Instance == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "No MacrophageEnragedFactionBaseInfo.Instance!  Can't enrage a harvester...", Verbosity.ShowAsError );
                return;
            }
            GameEntityTypeData entityData;
            if ( harvester.TypeData.GetHasTag( MacrophageFactionBaseInfo.SpireHarvesterTag ) ) // Fallen Spire enabled and this harvester is a Spire Harvester, spawn its Enraged varient.
                entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, MacrophageFactionBaseInfo.SpireEnragedHarvesterTag );
            else
                entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, MacrophageFactionBaseInfo.RegularEnragedHarvesterTag );
            ArcenPoint spawnLocation = harvester.WorldLocation;
            Faction enragedFaction = MacrophageEnragedFactionBaseInfo.Instance.AttachedFaction;
            PlanetFaction pFaction = harvester.Planet.GetPlanetFactionForFaction( enragedFaction );
            if ( entityData == null )
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: no MacrophageEnragedHarvester tag found", Verbosity.DoNotShow );
            GameEntity_Squad enragedHarvester = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, harvester.CurrentMarkLevel,
                                                   pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "Macrophage-EnrageHarvester" );
            if ( enragedHarvester != null )
            {
                enragedHarvester.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                enragedHarvester.ShouldNotBeConsideredAsThreatToHumanTeam = true;
            }
            harvester.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );
        }

        private void SpawnSpores( ArcenHostOnlySimContext Context, int numSpores, GameEntity_Squad telium )
        {
            if ( telium == null )
                return;
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, MacrophageFactionBaseInfo.SporeTag );
            ArcenPoint spawnLocation = telium.WorldLocation;
            PlanetFaction pFaction = telium.Planet.GetPlanetFactionForFaction( AttachedFaction );
            if ( entityData == null )
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: no MacrophageSpore tag found", Verbosity.DoNotShow );
            for ( int i = 0; i < numSpores; i++ )
            {
                GameEntity_Squad spore = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                    pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "Macrophage-Spores" );
                if ( spore != null )
                {
                    MacrophagePerSporeBaseInfo sData = spore.CreateExternalBaseInfo<MacrophagePerSporeBaseInfo>( "MacrophagePerSporeBaseInfo" );
                    sData.TeliumID = telium.PrimaryKeyID;
                    sData.SpawnTime = World_AIW2.Instance.GameSecond;
                }
            }
        }
        private GameEntity_Squad GetTeliumForHarvester( GameEntity_Squad harvester, MacrophagePerHarvesterBaseInfo hData )
        {
            if ( harvester == null )
                return null;
            MacrophagePerHarvesterBaseInfo harvesterInfo = harvester.GetExternalBaseInfoAs<MacrophagePerHarvesterBaseInfo>();
            return World_AIW2.Instance.GetEntityByID_Squad( harvesterInfo.TeliumID );
        }
    }
}
