using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class TestChamberMapPopulator : IMapPopulator
    {
        private Log _log;
        public Log Log
        {
            get
            {
                if (MapgenLogger.IsActive)
                {
                    if (_log == null)
                        _log = Arcen.Universal.Log.Yes;
                    return _log;
                }
                
                return null;
            }
        }

        public TestChamberMapPopulator()
        {

        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        public void DetermineAiHomeworlds( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration MapConfig, MapTypeData mapType )
        {

        }
        public void SetInitialPlayerVision( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration MapConfig, MapTypeData mapType )
        {

        }
        public void AddNomadPlanetsIfNecessary( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {

        }

        public void DetermineAiOwnership( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration MapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "Test Chamber DetermineAiOwnership" );

            //bool debug = false;
            ThrowawayListCanMemLeak<Faction> factionsThatAreAI = new ThrowawayListCanMemLeak<Faction>( 300 );
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction faction = World_AIW2.Instance.Factions[i];
                if ( faction.Type == FactionType.AI )
                    factionsThatAreAI.Add( faction );
            }
            if ( factionsThatAreAI.Count > 0 )
            {
                foreach ( Planet planet in galaxy.Planets( true ) )
                {
                    planet.InitialOwningAIFactionIndex = factionsThatAreAI[0].FactionIndex;
                }
            }
        }
        public void CalculateAllTheAsteroidCountsAndScienceAndHackingPerPlanet( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
        }

        public void SeedNormalEntities( Planet planet, ArcenHostOnlySimContext Context, MapTypeData mapType, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull )
        {
            Log?.Msg("{0}() called.", this.TypeNameAndMethod());
            Log?.Msg("There are {0}x TestChamber rows.", TestChamberTable.Instance.Rows.Count);
            
            //in the test chamber, let us see all things always
            planet.GrantIntel( PlanetIntelLevel.PermanentlyWatched );

            if ( TestChamberTable.Instance.Rows.Count <= 0 )
                return;

            TestChamber chamber = TestChamberTable.Instance.Rows[0];

            planet.GravWellSize = chamber.GravWellSize;

            Log?.Msg("There are {0}x instructions.", chamber.Instructions.Count);
            
            for ( int i = 0; i < chamber.Instructions.Count; i++ )
            {
                int debugStage = 10;
                TestChamberInstruction instruction = chamber.Instructions[i];

                Log?.Msg("Processing instruction #{0}: {1}.", i+1, instruction);
                
                try
                {
                    debugStage = 20;
                    PlanetFaction pfaction = planet.GetFirstFactionMatchingLookupNameForTutorialsAndSuch( instruction.FactionNameToMatch );
                    if ( pfaction == null )
                    {
                        ArcenDebugging.ArcenDebugLog(
                            "Could not find faction with lookup name for tutorials and such matching '" +
                            instruction.FactionNameToMatch + "'", Verbosity.ShowAsError );

                        continue;
                    }

                    debugStage = 30;

                    if (instruction.FleetDesign != null)
                    {
                        debugStage = 31;
                        instruction.FleetDesign.Instanciate( pfaction, Engine_AIW2.Instance.CombatCenter, Context );
                        continue;
                    }
                    
                    if (instruction.EntityType == null)
                    {
                        LOG.Msg("testchamber instruction with null EntityType...");
                        continue;
                    }

                    debugStage = 32;
                    int stack_size = instruction.ShipCount;
                    if ( instruction.SquadCount > 1 )
                    {
                        debugStage = 33;
                        stack_size /= instruction.SquadCount;
                    }
                    else
                    {
                        debugStage = 34;
                        if (instruction.EntityType.CannotBeStacked)
                        {
                            stack_size = 1;
                        }
                        else
                        {
                            debugStage = 35;
                            
                            int cap = 0;
                            if (pfaction.Faction.Type == FactionType.Player)
                                cap = AIWar2GalaxySettingQuickAccess.StackingCutoffPlayers;
                            else
                                cap = AIWar2GalaxySettingQuickAccess.StackingCutoffNPCs;

                            stack_size = instruction.ShipCount / cap;
                            if (stack_size < 1)
                                stack_size = 1;
                        }
                    }

                    debugStage = 40;
                    int rem = instruction.ShipCount;
                    while (rem > 0)
                    {
                        debugStage = 41;
                        int count_to_spawn = stack_size;
                        if (count_to_spawn > rem)
                            count_to_spawn = rem;

                        debugStage = 50;
                        ArcenPoint point = Engine_AIW2.Instance.CombatCenter;
                        point.X += instruction.SpawnXOffset;
                        point.Y += instruction.SpawnYOffset;
                        
                        Log?.Msg( "Trying to spawn {0} x{1} at {2}", instruction.EntityType, count_to_spawn, point );
                        
                        debugStage = 60;
                        point = planet.GetSafePlacementPoint_SpecificPoint( Context,
                            instruction.EntityType, point, 100, 1000 );

                        Log?.Msg( "Safe placement point is {0}", point );
                        
                        debugStage = 70;
                        var e = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pfaction, instruction.EntityType,
                            instruction.MarkLevel, pfaction.FleetUsedAtPlanet, 0, point, Context,
                            "Mapgen-TestChamber" );
                        debugStage = 80;
                        e.SetShipCount( count_to_spawn );
                        debugStage = 90;
                        
                        e.Orders.SetBehaviorDirectlyInSim(instruction.Behavior);
                        
                        Log?.Msg( "Actual entity pos is {0}", e.WorldLocation );
                        
                        //Log?.Msg("spawning {0}", e.ToStringSquadSizeAndFaction());
                        
                        debugStage = 100;
                        rem -= count_to_spawn;
                    }
                }
                catch ( Exception e )
                {
                    LOG.Err( "Exception in {0}() at debugstage={1} during instruction #{2}: {3}.\n{4}", this.TypeNameAndMethod(), debugStage, i+1, instruction, e );
                }
            }
        }

        public void SeedSpecialEntities_EarlyPreMajorFactionClaims( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData MapData )
        {
            // nice idea, but doesn't work
            /*
            TestChamber chamber = TestChamberTable.Instance.Rows[0];
            for ( int i = 0; i < chamber.Instructions.Count; i++ )
            {
                var instruction = chamber.Instructions[i];
                var name = instruction.FactionNameToMatch;
                
                Faction F = null;
                for ( int j = 0; j < World_AIW2.Instance.Setup.FactionConfigurations.Count; j++ )
                {
                    var C = World_AIW2.Instance.Setup.FactionConfigurations[j];
                    if ( C.LookupNameForTutorialsAndSuch == name )
                    {
                        F = World_AIW2.Instance.Factions[C.FactionIndex];
                        LOG.Msg("faction {0} found", F.FactionNameOrEmpty);
                        break;
                    }
                }
                 
                if (F == null)
                {
                    LOG.Msg("faction {0} not found so creating it", instruction.FactionNameToMatch);
                    
                    //F = World_AIW2.Instance.Faction( instruction.FactionNameToMatch, null );    
                }
            }
            */
        }

        public void SeedSpecialEntities_MiddlePostMajorFactionClaims( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData MapData )
        {
        }

        public void SeedSpecialEntities_LateAfterAllFactionSeeding( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData MapData )
        {
        }

        public void SetTurretAllowancesOnAIPlanets( Galaxy galaxy, ArcenHostOnlySimContext Context )
        {
        }

        public void SeedFirstReinforcementsAtVeryEnd( Planet planet, ArcenHostOnlySimContext Context, MapTypeData mapType, Tutorial.TutorialPlanetOwnershipAndSpawning TutorialPlanetOrNull )
        {
        }

        void IMapPopulator.DeterminePlayerHomeworlds( Galaxy galaxy, ArcenHostOnlySimContext detailsContext, MapConfiguration mapConfig, MapTypeData mapType )
        {
        }

        bool IMapPopulator.TryAssignHumanHomeworld( Galaxy galaxy, short Index, Faction faction, string Reason, bool ErrorOutIfFails )
        {
            return false;
        }

        bool IMapPopulator.TryAssignAIHomeworld( Galaxy galaxy, short Index, Faction faction, string reason, bool ErrorOutIfFails )
        {
            return false;
        }
    }
}
