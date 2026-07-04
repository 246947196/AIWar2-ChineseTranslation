using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class GameCommand_CreateNewPlanet : BaseGameCommand
    {
        /* TEACHING_MOMENT: Creating a new planet
         * We have to create planets in the GameCommand context lest we have funky races/desyncs.
           See https://wiki.arcengames.com/index.php?title=AI_War_2:_Discussion_Of_Multiplayer_Desyncs_And_Primary_Keys for context.

           This is used for the Dark Zenith and the Malware.

        This code checks the PlanetPopulationType passed in to know if it's doing anything interesting. 

        Note that the first Point must be the "center" of the new planets. */

        private readonly List<Planet> newPlanets = List<Planet>.Create_WillNeverBeGCed( 40, "GameCommand_CreateNewPlanet-newPlanets" );
        private readonly List<Planet> oldPlanets = List<Planet>.Create_WillNeverBeGCed( 40, "GameCommand_CreateNewPlanet-oldPlanets" );

        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //no running these on clients!

            if ( command.RelatedPoints.Count <= 1 )
                throw new Exception( "The CreateNewPlanet command requires some points" );
            int debugCode = 0;
            bool debug = false;
            try
            {
                PlanetPopulationType type = (PlanetPopulationType)command.RelatedMagnitude;
                newPlanets.Clear();
                oldPlanets.Clear();
                debugCode = 100;
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    //Build a list of pre-existing planets so we can find a good planet to link our new planets to
                    if ( planet.TypeData.Type == PlanetType.Nomad )
                        continue; //don't link to a nomad
                    oldPlanets.Add( planet );
                }
                if ( oldPlanets.Count == 0 )
                {
                    //this can happen if the 'all planets are nomads' setting is enabled
                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        oldPlanets.Add( planet );
                    }
                }
                debugCode = 1000;
                Faction faction = World_AIW2.Instance.GetFactionByIndex( command.RelatedFactionIndex );
                ArcenPoint centerOfNewPlanets = command.RelatedPoints.First;
                int _outerI = -1;
                foreach ( var _rp_v in command.RelatedPoints )
                {
                    _outerI++;
                    if ( _outerI == 0 ) continue;
                    debugCode = 2000;
                    World_AIW2.Instance.Setup.ShouldSeedDetailsYet = true; //this causes it to seed the planetfaction entries and such.


                    Planet planet = World_AIW2.Instance.CurrentGalaxy.AddPlanet( PlanetType.Normal, _rp_v,
                        World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( context.RandomToUse, type ) );
                    planet.PopulationType = type;
                    debugCode = 2100;
                    if ( Engine_AIW2.Instance.GetIsCurrentlyInSpectatorMode() )
                        planet.GrantIntel( PlanetIntelLevel.CurrentlyWatched );

                    //ArcenDebugging.ArcenDebugLogSingleLine( "DZ planet spawned: " + planet.Name + " " +
                    //    World_AIW2.Instance.CurrentGalaxy.GetTotalPlanetCount() + " hasbeendest: " + planet.HasPlanetBeenDestroyed + " tobedest: " + planet.IsPlanetToBeDestroyed, Verbosity.Chat );
                    int AIPforPlanets = 0;
                    if ( faction.Type == FactionType.Player )
                    {
                        AIPforPlanets = 12;
                    }
                    if ( type == PlanetPopulationType.DarkZenith )
                    {
                        debugCode = 2200;
                        DZPopulatePlanet( faction, planet, _outerI - 1, context.GetHostOnlyContext() );
                        for ( int j = 0; j < planet.Factions.Count; j++ )
                        {
                            planet.Factions[j].AIPLeftFromCommandStation = AIPforPlanets;
                            planet.Factions[j].AIPLeftFromWarpGate = 0;
                        }
                        if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "EnableFimbulwinter", true ) ) //this is for the fimbulwinter
                            planet.IsFimbulwintered = true;
                        //these planets are worth extra Science. Possibly this should actually be "less Science"?
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Processing new planet " + planet.Name + " at " + planet.GalaxyLocation, Verbosity.DoNotShow );
                        planet.OverrideScienceAmount = 2300;
                        newPlanets.Add( planet );
                        if ( faction.Type == FactionType.Player )
                        {
                            DarkZenithSidekickFactionBaseInfo gData = faction.GetExternalBaseInfoAs<DarkZenithSidekickFactionBaseInfo>();
                            gData.OriginalPlanets.Add( planet );
                        }
                        else
                        {
                            DarkZenithFactionBaseInfo gData = faction.GetExternalBaseInfoAs<DarkZenithFactionBaseInfo>();
                            gData.OriginalPlanets.Add( planet );
                        }
                    }
                    if ( type == PlanetPopulationType.Malware )
                    {
                        debugCode = 2200;
                        MalwarePopulatePlanet( faction, planet, _outerI - 1, context.GetHostOnlyContext() );
                        for ( int j = 0; j < planet.Factions.Count; j++ )
                        {
                            planet.Factions[j].AIPLeftFromCommandStation = 0;
                            planet.Factions[j].AIPLeftFromWarpGate = 0;
                        }

                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Processing new planet " + planet.Name + " at " + planet.GalaxyLocation, Verbosity.DoNotShow );
                        planet.OverrideScienceAmount = 2300;
                        newPlanets.Add( planet );
                    }
                }
                debugCode = 3000;
                if ( newPlanets.Count > 0 )
                    BadgerUtilityMethods.createGabrielGraph( newPlanets );

                MapPopulatorTypeData mapPopulator = World_AIW2.Instance.GetMapPopulator();

                debugCode = 4000;
                World_AIW2.Instance.Setup.ShouldSeedDetailsYet = true;
                for ( int i = 0; i < newPlanets.Count; i++ )
                {
                    debugCode = 4100;
                    //for metal generators, wormholes, etc...
                    newPlanets[i].Mapgen_WorkingAsteroidCount = context.RandomToUse.NextInclus( 6, 10 );
                    mapPopulator.Populator.SeedNormalEntities( newPlanets[i], context.GetHostOnlyContext(), World_AIW2.Instance.Setup.MapConfig.MapType, null );
                }

                //ArcenDebugging.ArcenDebugLogSingleLine( "DZ planets spawned: " + newPlanets.Count, Verbosity.Chat );

                debugCode = 5000;
                
                if ( type != PlanetPopulationType.DarkZenith && type != PlanetPopulationType.Malware ) //dark zenith and malware planets are linked in later
                {
                    BadgerUtilityMethods.linkPlanetLists( World_AIW2.Instance.CurrentGalaxy, oldPlanets, newPlanets, centerOfNewPlanets, false, 2, false, context.GetHostOnlyContext(), true );

                    //We've placed wormholes on the new planets, but we need to make sure we have wormholes on the
                    //already existing planets that go to the new planets
                    foreach ( Planet planet in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
                    {
                        debugCode = 5100;
                        if ( newPlanets.Contains( planet ) )
                            continue;
                        for ( int i = 0; i < newPlanets.Count; i++ )
                        {
                            debugCode = 5200;
                            Planet newPlanet = newPlanets[i];
                            if ( planet.GetIsDirectlyLinkedTo( false, newPlanet ) )
                            {
                                debugCode = 5300;
                                //if we need a new wormhole, add one
                                int largerIndex = Math.Max( newPlanet.Index, planet.Index );
                                int smallerIndex = Math.Min( newPlanet.Index, planet.Index );
                                int seed = (largerIndex << 16) + smallerIndex;

                                IWormholePlacer placer = new WormholePlacer_Default( FInt.FromParts( 0, 250 ), FInt.FromParts( 0, 750 ) );

                                ArcenPoint wormholePoint = placer.GetPointForWormhole( context.GetHostOnlyContext(), planet, newPlanet );
                                PlanetFaction pFaction = planet.GetFirstFactionOfType( FactionType.NaturalObject );
                                GameEntity_Other wormhole = GameEntity_Other.CreateOtherNew_CallFromHostOnly( pFaction, GameEntityTypeDataTable.Instance.DefaultWormholeType, wormholePoint, context.GetHostOnlyContext() );
                                wormhole.SetLinkedPlanetIndex( newPlanet.Index );
                            }
                        }
                    }
                }
                World_AIW2.Instance.Setup.ShouldSeedDetailsYet = false;
                World_AIW2.Instance.CurrentGalaxy.RecomputeDestinationIndexToWormholeMapping();
                World_AIW2.Instance.CurrentGalaxy.RecomputePlanetDistances();
                World_AIW2.Instance.CurrentGalaxy.RecomputeLinkedPathfindables();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in GameCommand CreateNewPlanet. debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        private void DZPopulatePlanet( Faction faction, Planet planet, int planetNum, ArcenHostOnlySimContext Context )
        {
            //Spawn units for the Dark Zenith
            int debugCode = 0;
            try
            {
                debugCode = 1000;
                int intensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
                DarkZenithDifficulty Difficulty = DarkZenithDifficultyTable.Instance.GetRowByIntensity( intensity, faction );
                if ( Difficulty == null )
                    throw new Exception( "Invalid DZDifficulty found" );
                ArcenDebugging.ArcenDebugLogSingleLine( "DZ Invasion with difficulty: " + Difficulty.name, Verbosity.DoNotShow );
                AIDifficulty aiDifficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty_AsDifficulty();
                bool isPlayer = faction.Type == FactionType.Player;
                debugCode = 1100;
                bool isEmpire = false; //Empire player types start extra strong since they have no friends
                if ( isPlayer )
                {
                    PlayerTypeData playerType = faction.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( playerType != null && playerType.GetHasTag("DarkZenithEmpire"))
                    {
                        isEmpire = true;
                    }
                }
                GameEntityTypeData entityData = null;

                //If the DZ and DS are allied, DZ planets all get VGs

                //Lets just make a terminus to start
                entityData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "WarpingInDZTerminus" );
                if ( entityData == null )
                    throw new Exception( "No suitable Terminus Found\n" );
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                if ( pFaction == null )
                    throw new Exception( "Could not find planetFaction for DZ for this new planet" );
                debugCode = 1200;
                ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                debugCode = 1210;
                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                                         pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );
                newEntity.TransformsIntoAfterTime = "DZMetalTerminus";
                newEntity.SecondsTillTransformation = (Int16)Context.RandomToUse.Next( 1, 5 );
                debugCode = 1220;
                DarkZenithPerUnitBaseInfo data = newEntity.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                data.Resource = DZResource.Metal;
                debugCode = 1230;
                int numDefensives = 2;
                if ( isEmpire )
                    numDefensives += 5;
                ArcenDebugging.ArcenDebugLogSingleLine( "spawning " + numDefensives + " defensive structures near structures", Verbosity.DoNotShow );
                //spawn  defensive structures near the terminus
                for ( int i = 0; i < numDefensives; i++ )
                {
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZDefensiveStructureWeak" );
                    spawnLocation = planet.GetSafePlacementPoint_AroundEntity( Context, entityData, newEntity, FInt.FromParts( 0, 15 ), FInt.FromParts( 0, 35 ) ); //this uses the location of the previous entity created
                    newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, (byte)Context.RandomToUse.Next( 3, 7 ),
                                                                             pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );
                }

                debugCode = 1240;
                //Also spawn an additional random terminus
                // spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter(Context, entityData, Engine_AIW2.Instance.CombatCenter, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                // newEntity = GameEntity_Squad.CreateNew( pFaction, entityData, 1,
                //                                         pFaction.Faction.LooseFleet, 0, spawnLocation, Context );

                // DZResource resource = SpecialFaction_DarkZenith.GetRandomResourceType(planet);
                // newEntity.TransformsIntoAfterTime = SpecialFaction_DarkZenith.GetTerminusTagFromResource(resource);
                // newEntity.SecondsTillTransformation = (short)Context.RandomToUse.Next(1, 5);
                // data = newEntity.GetDarkZenithPerUnitBaseInfoExt();
                // data.Resource = resource;
                debugCode = 1250;
                if ( isEmpire && planetNum == 1 )
                {
                    entityData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "DZKing" );
                    if ( entityData == null )
                        throw new Exception("Could not find DZKing");
                    spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                    debugCode = 1210;
                    newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                        pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );
                    if ( newEntity == null )
                        throw new Exception("Could not spawn DZKing");

                }
                debugCode = 1300;
                entityData = GameEntityTypeDataTable.Instance.GetRowByName( "DZHarvester" );
                spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                            pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );
                newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                            pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );
                newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                //And then also spawn two Transports
                debugCode = 1400;
                entityData = GameEntityTypeDataTable.Instance.GetRowByName( "DZTransport" );

                spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                            pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );
                newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                        pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );
                newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;

                //And then also spawn a Warping In Epistyle
                debugCode = 1500;
                entityData = GameEntityTypeDataTable.Instance.GetRowByName( "DZEpistyle" );

                spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                        pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );

                DarkZenithPerUnitBaseInfo epistyleData = newEntity.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                epistyleData.AddStartingResources( Difficulty.InitialMetal, Difficulty.InitialOtherResources );
                //spawn  defensive structures near the epistyle
                int defensiveStructuresPerEpistyle = 2;
                if ( isEmpire )
                    defensiveStructuresPerEpistyle += 3;
                for ( int i = 0; i < defensiveStructuresPerEpistyle; i++ )
                {
                    debugCode = 1510;
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZDefensiveStructureWeak" );
                    spawnLocation = planet.GetSafePlacementPoint_AroundEntity( Context, entityData, newEntity, FInt.FromParts( 0, 15 ), FInt.FromParts( 0, 35 ) ); //this uses the location of the previous entity created
                    newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, (byte)Context.RandomToUse.Next( 3, 7 ),
                                                                             pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );
                }
                if ( planetNum == 1 )
                {
                    if ( World_AIW2.Instance.IsFuelEnabled )
                    {
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MajorArgon" );
                        
                        spawnLocation = planet.GetSafePlacementPoint_AroundEntity( Context, entityData, newEntity, FInt.FromParts( 0, 15 ), FInt.FromParts( 0, 35 ) ); //this uses the location of the previous entity created
                        newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, (byte)Context.RandomToUse.Next( 3, 7 ),
                                                                                     pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );
                    }
                }
                if ( planetNum <= 1 )
                {
                    debugCode = 1520;
                    if ( !isPlayer ) {
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZLibrary" );
                        spawnLocation = planet.GetSafePlacementPoint_AroundEntity( Context, entityData, newEntity, FInt.FromParts( 0, 15 ), FInt.FromParts( 0, 35 ) ); //this uses the location of the previous entity created
                        newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, (byte)Context.RandomToUse.Next( 3, 7 ),
                                pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );
                    }
                    //Also a second epistyle
                    entityData = GameEntityTypeDataTable.Instance.GetRowByName( "DZEpistyle" );

                    spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                    newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                            pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );

                    epistyleData = newEntity.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    epistyleData.AddStartingResources( Difficulty.InitialMetal, Difficulty.InitialOtherResources );
                }
                if ( planetNum == 2 )
                {
                    if ( !isPlayer ) {
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZAthenaeum" );
                        spawnLocation = planet.GetSafePlacementPoint_AroundEntity( Context, entityData, newEntity, FInt.FromParts( 0, 15 ), FInt.FromParts( 0, 35 ) ); //this uses the location of the previous entity created
                        newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, (byte)Context.RandomToUse.Next( 3, 7 ),
                                pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );
                    }
                }
                debugCode = 1530;
                //Create a Jormugandr if appropriate
                if ( planetNum < Difficulty.NumJormugandrToSpawn && !isPlayer)
                {
                    //The DZ Sidekick gets its flagship from the DeepInfo code
                    //                    ArcenDebugging.ArcenDebugLogSingleLine("Spawning J for planet " + planetNum + " limit " + Difficulty.NumJormugandrToSpawn, Verbosity.DoNotShow );
                    debugCode = 1600;
                    entityData = GameEntityTypeDataTable.Instance.GetRowByName( "DZJormugandr" );

                    spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                    newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                        pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );
                    newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                    DarkZenithPerUnitBaseInfo jormugandrData = newEntity.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    jormugandrData.IsJormugandr = true;
                    jormugandrData.IsDormant = true;
                    jormugandrData.SecondsUntilDormancyMove = Difficulty.TimeBetweenJormugandrDormantMoves;
                }
                FInt strength = Difficulty.BaseInvasionStrengthPerPlanet;
                ArcenDebugging.ArcenDebugLogSingleLine( "DZ base invasion strength per planet: " + strength, Verbosity.DoNotShow );
                FInt powerLevel = FactionUtilityMethods.Instance.GetOverallPowerLevelOfEnemies( faction );
                if ( powerLevel >= FInt.One )
                    strength *= (Difficulty.StrengthMultiplierPerEnemyPowerLevel * powerLevel);
                if ( strength <= 10 )
                    throw new Exception( "Problematic DZ difficulty " + Difficulty.ToString() );
                ArcenDebugging.ArcenDebugLogSingleLine( "DZ invasion strength after factoring in overall power level: " + strength, Verbosity.DoNotShow );
                //The DZ has a failsafe: if it's calculated value is too weak then just update itself based on the player's current strength
                int currentPlayerStrength = StrengthHelper.GetMaxPossibleStrengthOfAllHumanFlagships() / 1000; //player strength is scaled for UI
                FInt playerRatio = FInt.FromParts( 0, 500 );
                if ( strength < (currentPlayerStrength * playerRatio).IntValue )
                    strength = (currentPlayerStrength * playerRatio); //if we are too much weaker than the player, just match the player. The player strength is already divided by 1K since it's for the UI
                ArcenDebugging.ArcenDebugLogSingleLine( "DZ invasion strength after factoring current player strength (" + currentPlayerStrength + "): " + strength, Verbosity.DoNotShow );
                if ( isEmpire )
                {
                    FInt empireMultiplier = FInt.FromParts(2, 000 );
                    strength = (empireMultiplier * strength); //Player Empires need to be a bit stronger since they have no friends
                    ArcenDebugging.ArcenDebugLogSingleLine( "DZ invasion strength after factoring in empire (" + empireMultiplier + "): " + strength, Verbosity.DoNotShow );
                }

                FInt origStrength = strength; //set here for logging
                int difficultyOverPoint = aiDifficulty.Difficulty - Difficulty.AIDifficultyIncreasePoint;
                if ( difficultyOverPoint > 0 )
                {
                    FInt multiplier = FInt.One + (Difficulty.StrengthMultiplierIncreasePerDifficultyOverPoint * difficultyOverPoint);
                    strength *= multiplier;
                    //                    ArcenDebugging.ArcenDebugLogSingleLine("using bonus strength multiplier " + multiplier +" ("+Difficulty.StrengthMultiplierIncreasePerDifficultyOverPoint+"), " + origStrength + " -> " + strength, Verbosity.DoNotShow );
                }
                ArcenDebugging.ArcenDebugLogSingleLine( "DZ invasion strength after factoring in ai difficulty: " + strength, Verbosity.DoNotShow );
                int retries = 100;
                int numShips = 0;
                origStrength = strength; //and now for tracking unit creation
                debugCode = 1700;
                int percentTierZero = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkZenith_PercentTierZeroForInvasion" );
                int percentTierOne = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkZenith_PercentTierOneForInvasion" );
                int percentTierTwo = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkZenith_PercentTierTwoForInvasion" );
                int percentTierThree = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkZenith_PercentTierThreeForInvasion" );
                if ( percentTierZero + percentTierOne + percentTierTwo + percentTierThree != 100 )
                    throw new Exception( "Problem with DZ invasion XML; the percent tiers do not add to 100" );
                while ( strength > 0 && retries > 0 )
                {
                    debugCode = 1800;

                    int random = Context.RandomToUse.Next( 0, 100 );
                    if ( random < percentTierZero )
                    {
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZTierZeroBase" );
                    }
                    else if ( random + percentTierZero < percentTierOne )
                    {
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZTierOneBase" );
                    }
                    else if ( random + percentTierZero + percentTierOne < percentTierTwo )
                    {
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZTierTwoBase" );
                    }
                    else
                    {
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "DZTierThreeBase" );
                    }
                    if ( strength < entityData.CostForAIToPurchase )
                    {
                        retries--;
                        continue;
                    }
                    strength -= entityData.CostForAIToPurchase;
                    spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                    newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                            pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );
                    newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                    newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                    numShips++;
                }
                ArcenDebugging.ArcenDebugLogSingleLine( "Spawning " + origStrength.ReadableString + " strength, " + numShips + " units against overall power level " + powerLevel, Verbosity.DoNotShow );
                debugCode = 1900;
                Faction dsFaction = FactionUtilityMethods.Instance.GetDarkSpireFaction();
                if ( dsFaction != null && faction.BaseInfo.Allegiance ==
                     dsFaction.BaseInfo.Allegiance )
                {
                    //spawn some DS VGs if necessary, and also have the Dark Spire in conquest mode
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "VengeanceGeneratorLocus" );
                    spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                    pFaction = planet.GetPlanetFactionForFaction( dsFaction );
                    newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                            pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );
                    DarkSpireFactionBaseInfo dsdata = dsFaction.TryGetExternalBaseInfoAs<DarkSpireFactionBaseInfo>();
                    if ( !dsdata.ConquestMode )
                    {
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DarkSpireConquestModeFromDarkAlliance", string.Empty, dsFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                        dsdata.ConquestMode = true;
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in DZPopulatePlanet code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }

        }
        private void MalwarePopulatePlanet( Faction faction, Planet planet, int planetNum, ArcenHostOnlySimContext Context )
        {
            //Spawn units for the Dark Zenith
            int debugCode = 0;
            try
            {
                debugCode = 1000;
                int intensity = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();
                MalwareFactionBaseInfo baseInfo = faction.GetExternalBaseInfoAs<MalwareFactionBaseInfo>();
                MalwareDifficulty Difficulty = baseInfo.Difficulty;
                if ( Difficulty == null )
                    throw new Exception( "Invalid Malware Difficulty found" );
                ArcenDebugging.ArcenDebugLogSingleLine( "DZ Invasion with difficulty: " + Difficulty.name, Verbosity.DoNotShow );
                AIDifficulty aiDifficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty_AsDifficulty();

                GameEntityTypeData entityData = null;

                //Lets just make a terminus to start
                entityData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "CthonicKernel" );
                if ( entityData == null )
                    throw new Exception( "No suitable kernel Found\n" );
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                if ( pFaction == null )
                    throw new Exception( "Could not find planetFaction for DZ for this new planet" );
                debugCode = 1200;
                ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                debugCode = 1210;
                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                                         pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "MalwarePopulatePlanet" );
                int numDefensives = 7;
                ArcenDebugging.ArcenDebugLogSingleLine( "spawning " + numDefensives + " defensive structures near structures", Verbosity.DoNotShow );
                //spawn  defensive structures near the terminus
                for ( int i = 0; i < numDefensives; i++ )
                {
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MalwareDaemonSpike" );
                    spawnLocation = planet.GetSafePlacementPoint_AroundEntity( Context, entityData, newEntity, FInt.FromParts( 0, 15 ), FInt.FromParts( 0, 35 ) ); //this uses the location of the previous entity created
                    newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, (byte)Context.RandomToUse.Next( 3, 7 ),
                                                                             pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "MalwarePopulatePlanet" );
                }

                debugCode = 1240;
                debugCode = 1250;
                if ( planetNum == 1 )
                {
                    entityData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "MalwareKing" );
                    if ( entityData == null )
                        throw new Exception("Could not find MalwareKing");
                    spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                    debugCode = 1210;
                    newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                        pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "MalwarePopulatePlanet" );
                    if ( newEntity == null )
                        throw new Exception("Could not spawn MalwareKing");

                    entityData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "MalwareHarbinger" );
                    if ( entityData != null )
                    {
                        for ( int k = 0; k < 2; k++ )
                        {
                            spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                            GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "MalwarePopulatePlanet" );
                        }
                    }

                    entityData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "MalwareAegis" );
                    if ( entityData != null )
                    {
                        for ( int k = 0; k < 2; k++ )
                        {
                            spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                            GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "MalwarePopulatePlanet" );
                        }
                    }

                }
                debugCode = 1530;

                int strength = Difficulty.BaseInvasionStrengthPerPlanet;
                ArcenDebugging.ArcenDebugLogSingleLine( "malware base invasion strength per planet: " + strength, Verbosity.DoNotShow );
                FInt powerLevel = FactionUtilityMethods.Instance.GetOverallPowerLevelOfEnemies( faction );
                if ( powerLevel >= FInt.One )
                    strength *= (Difficulty.StrengthMultiplierPerEnemyPowerLevel * powerLevel).IntValue;
                if ( strength <= 10 )
                    throw new Exception( "Problematic malware difficulty " + Difficulty.ToString() );
                ArcenDebugging.ArcenDebugLogSingleLine( "malware invasion strength after factoring in overall power level: " + strength, Verbosity.DoNotShow );
                //The DZ has a failsafe: if it's calculated value is too weak then just update itself based on the player's current strength
                int currentPlayerStrength = StrengthHelper.GetMaxPossibleStrengthOfAllHumanFlagships() / 1000; //player strength is scaled for UI
                FInt playerRatio = FInt.FromParts( 0, 500 );
                if ( strength < (currentPlayerStrength * playerRatio).IntValue )
                    strength = (currentPlayerStrength * playerRatio).IntValue; //if we are too much weaker than the player, just match the player. The player strength is already divided by 1K since it's for the UI
                ArcenDebugging.ArcenDebugLogSingleLine( "malware invasion strength after factoring current player strength (" + currentPlayerStrength + "): " + strength, Verbosity.DoNotShow );

                FInt origStrength = FInt.FromParts(strength, 000); //set here for logging
                int difficultyOverPoint = aiDifficulty.Difficulty - Difficulty.AIDifficultyIncreasePoint;
                if ( difficultyOverPoint > 0 )
                {
                    FInt multiplier = FInt.One + (Difficulty.StrengthMultiplierIncreasePerDifficultyOverPoint * difficultyOverPoint);
                    strength = (strength*multiplier).IntValue;
                    //                    ArcenDebugging.ArcenDebugLogSingleLine("using bonus strength multiplier " + multiplier +" ("+Difficulty.StrengthMultiplierIncreasePerDifficultyOverPoint+"), " + origStrength + " -> " + strength, Verbosity.DoNotShow );
                }
                ArcenDebugging.ArcenDebugLogSingleLine( "malware invasion strength after factoring in ai difficulty: " + strength, Verbosity.DoNotShow );
                int retries = 100;
                int numShips = 0;
                origStrength = FInt.FromParts(strength, 000); //and now for tracking unit creation
                debugCode = 1700;
                int percentTierOne = 40;
                int percentTierTwo = 40;
                int percentTierThree = 20;
                if ( percentTierOne + percentTierTwo + percentTierThree != 100 )
                    throw new Exception( "Problem with malware percentages; the percent tiers do not add to 100" );
                while ( strength > 0 && retries > 0 )
                {
                    debugCode = 1800;

                    int random = Context.RandomToUse.Next( 0, 100 );
                    if ( random < percentTierOne )
                    {
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MalwareHostTierOne" ); //substitute: MalwareInvasionTierZero
                    }
                    else if ( random + percentTierOne < percentTierTwo )
                    {
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MalwareHostTierTwo" ); //substitute: MalwareInvasionTierOne
                    }
                    else
                    {
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MalwareHostTierThree" );
                    }
                    if ( strength < entityData.CostForAIToPurchase )
                    {
                        retries--;
                        continue;
                    }
                    strength -= entityData.CostForAIToPurchase;
                    spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                    newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                            pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );
                    newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                    newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                    numShips++;
                }
                ArcenDebugging.ArcenDebugLogSingleLine( "Spawning " + origStrength.ReadableString + " strength, " + numShips + " units against overall power level " + powerLevel, Verbosity.DoNotShow );
                debugCode = 1900;
                Faction dsFaction = FactionUtilityMethods.Instance.GetDarkSpireFaction();
                if ( dsFaction != null && faction.BaseInfo.Allegiance ==
                     dsFaction.BaseInfo.Allegiance )
                {
                    //spawn some DS VGs if necessary, and also have the Dark Spire in conquest mode
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "VengeanceGeneratorLocus" );
                    spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 350 ) );
                    pFaction = planet.GetPlanetFactionForFaction( dsFaction );
                    newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                            pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "DZPopulatePlanet" );
                    DarkSpireFactionBaseInfo dsdata = dsFaction.TryGetExternalBaseInfoAs<DarkSpireFactionBaseInfo>();
                    if ( !dsdata.ConquestMode )
                    {
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                            World_AIW2.Instance.QueueLogJournalEntryToSidebar( "DarkSpireConquestModeFromDarkAlliance", string.Empty, dsFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                        dsdata.ConquestMode = true;
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in MalwarePopulatePlanet code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }

        }
    }

    public class GameCommand_LinkPlanets : BaseGameCommand
    {
        //This takes as arguments two integers (planet indices), then creates a wormhole link between them
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //no running these on clients!

            if ( command.RelatedIntegers.Count != 2 )
                throw new Exception( "The LinkPlanets command requires exactly two planet indices" );
            int debugCode = 0;
            //bool debug = false;
            try
            {
                debugCode = 100;
                int _ri0 = 0, _ri1 = 0;
                {
                    int _i = 0;
                    foreach ( var _v in command.RelatedIntegers )
                    {
                        if ( _i == 0 ) _ri0 = _v;
                        else if ( _i == 1 ) { _ri1 = _v; break; }
                        _i++;
                    }
                }
                Planet one = World_AIW2.Instance.GetPlanetByIndex( (short)_ri0 );
                Planet two = World_AIW2.Instance.GetPlanetByIndex( (short)_ri1 );
                one.AddLinkTo( two );
                debugCode = 200;
                debugCode = 300;
                //now link planet one to two
                int largerIndex = Math.Max( one.Index, two.Index );
                int smallerIndex = Math.Min( one.Index, two.Index );
                int seed = (largerIndex << 16) + smallerIndex;
                debugCode = 310;
                IWormholePlacer placer = null;
                ArcenPoint wormholePoint;
                if (command.RelatedPoints.Count > 0)
                    wormholePoint = command.RelatedPoints.First;
                else
                {
                    placer = new WormholePlacer_Default( FInt.FromParts( 0, 250 ), FInt.FromParts( 0, 750 ) );
                    wormholePoint = placer.GetPointForWormhole( context.GetHostOnlyContext(), one, two );
                }
                PlanetFaction pFaction = one.GetFirstFactionOfType( FactionType.NaturalObject );
                GameEntity_Other wormhole = GameEntity_Other.CreateOtherNew_CallFromHostOnly( pFaction, GameEntityTypeDataTable.Instance.DefaultWormholeType, wormholePoint, context.GetHostOnlyContext() );
                if ( wormhole != null )
                    wormhole.SetLinkedPlanetIndex( two.Index );
                debugCode = 400;
                //now link planet two to one
                largerIndex = Math.Max( two.Index, one.Index );
                smallerIndex = Math.Min( two.Index, one.Index );
                seed = (largerIndex << 16) + smallerIndex;
                if (command.RelatedPoints.Count > 1)
                {
                    ArcenPoint _rp_1 = ArcenPoint.ZeroZeroPoint;
                    int _rp_i = 0;
                    foreach ( var _rp_v in command.RelatedPoints )
                    {
                        if ( _rp_i == 1 ) { _rp_1 = _rp_v; break; }
                        _rp_i++;
                    }
                    wormholePoint = _rp_1;
                }
                else
                    wormholePoint = placer.GetPointForWormhole( context.GetHostOnlyContext(), two, one );
                GameEntityTypeData wormholeType = GameEntityTypeDataTable.Instance.DefaultWormholeType;
                if (!string.IsNullOrEmpty(command.RelatedString))
                {
                    wormholeType = GameEntityTypeDataTable.Instance.GetRowByName(command.RelatedString);
                    if (wormholeType == null)
                        throw new Exception(string.Format("Expected to find a GameEntityTypeData named \"{0}\" but none exist!", command.RelatedString));
                }
                pFaction = two.GetFirstFactionOfType( FactionType.NaturalObject );
                wormhole = GameEntity_Other.CreateOtherNew_CallFromHostOnly( pFaction, wormholeType, wormholePoint, context.GetHostOnlyContext() );
                if ( wormhole != null )
                    wormhole.SetLinkedPlanetIndex( one.Index );

                //Regenerate galaxy map and so on
                World_AIW2.Instance.CurrentGalaxy.RecomputeDestinationIndexToWormholeMapping();
                World_AIW2.Instance.CurrentGalaxy.RecomputeLinkedPathfindables();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in GameCommand CreateNewPlanet. debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

    }
    public class GameCommand_UnlinkPlanets : BaseGameCommand
    {
        //This takes as arguments two integers (planet indices), then creates a wormhole link between them
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //no running these on clients!

            if ( command.RelatedIntegers.Count != 2 )
                throw new Exception( "The UnlinkPlanets command requires exactly two planet indices" );
            int debugCode = 0;
            //bool debug = false;
            try
            {
                debugCode = 100;
                int _ri0 = 0, _ri1 = 0;
                {
                    int _i = 0;
                    foreach ( var _v in command.RelatedIntegers )
                    {
                        if ( _i == 0 ) _ri0 = _v;
                        else if ( _i == 1 ) { _ri1 = _v; break; }
                        _i++;
                    }
                }
                Planet one = World_AIW2.Instance.GetPlanetByIndex( (short)_ri0 );
                Planet two = World_AIW2.Instance.GetPlanetByIndex( (short)_ri1 );
                if ( one == null || two == null )
                    throw new Exception("UnlinkPlanets was unable to find a planet; we were looking for planets " + _ri0 + " and " + _ri1);
                GameEntity_Other wormholeOne = one.GetWormholeTo( two );
                GameEntity_Other wormholeTwo = two.GetWormholeTo( one );
                //Now we remove all the links and wormholes
                debugCode = 200;
                one.RemoveLinkTo( two );
                if ( wormholeOne != null )
                    wormholeOne.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.RemoveWormholes );
                if ( wormholeTwo != null )
                    wormholeTwo.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.RemoveWormholes );
                debugCode = 300;
                //Regenerate galaxy map and so on
                World_AIW2.Instance.CurrentGalaxy.RecomputeDestinationIndexToWormholeMapping();
                one.Network_HostOnly_NeedToSyncWormholesToClients = true;
                one.Network_HostOnly_NeedToSyncPlanetPositionToClients = true;
                two.Network_HostOnly_NeedToSyncWormholesToClients = true;
                two.Network_HostOnly_NeedToSyncPlanetPositionToClients = true;

                World_AIW2.Instance.CurrentGalaxy.RecomputeLinkedPathfindables();
                one.NomadHasMoved = true;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in GameCommand UnlinkPlanets. debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

    }
}
