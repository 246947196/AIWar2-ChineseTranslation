using System;

using System.Linq;
using System.Text;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    // Enraged Macrophage
    // Normal logic is pretty much disregarded for angry bugs. They just want to kill.
    public sealed class MacrophageEnragedFactionDeepInfo : MacrophageFactionDeepInfoBase, IExternalDeepInfo_Singleton
    {
        public MacrophageEnragedFactionBaseInfo BaseInfo;
        public static MacrophageEnragedFactionDeepInfo Instance = null;
        public override void SubDoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<MacrophageEnragedFactionBaseInfo>();
            Instance = this;
        }

        protected override void SubCleanup()
        {
            Instance = null;
            BaseInfo = null;
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 3;
        
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            AllegianceHelper.EnemyThisFactionToAll( AttachedFaction );
        }
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            bool debug = false;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    if ( entity.CalculateFinalDestinationPlanetIndex_Safe() != -1 &&
                         entity.CalculateFinalDestinationPlanetIndex_Safe() != entity.GetPlanetIndexSafe() )
                        continue; // this unit is en route to another planet
                    int homeworldModifier = 1;
                    if ( entity.Planet.GetCommandStationOrNull() != null )
                    {
                        switch ( entity.Planet.GetCommandStationOrNull().TypeData.SpecialType )
                        {
                            case SpecialEntityType.AIKingCommandStation:
                            case SpecialEntityType.AIKingMobile:
                            case SpecialEntityType.HumanHomeCommand:
                                homeworldModifier = 3;
                                break;
                            default:
                                homeworldModifier = 1;
                                break;
                        }
                    }
                    // Snack time is a random value between MinSnackTime + markModifier and MaxSnackTime + markModifier
                    // Break time is the same, but using the Break values.
                    int markModifier = (entity.CurrentMarkLevel - 1) * BaseInfo.SnackTimeIncreasePerMark;
                    if ( World_AIW2.Instance.GameSecond - entity.GameSecondCreated < Context.RandomToUse.Next( (BaseInfo.MinimumSnackTimeOnSpawn + markModifier) * homeworldModifier, (BaseInfo.MaximumSnackTimeOnSpawn + markModifier) * homeworldModifier ) )
                        continue; // this unit was just spawned in, and should hang out and chomp anything on its own planet for a while

                    if ( entity.PlanetFaction.DataByStance[FactionStance.Hostile].ThreatStrength > 1000 &&
                        World_AIW2.Instance.GameSecond - entity.GameSecondEnteredThisPlanet < Context.RandomToUse.Next( (BaseInfo.MinimumSnackTimeOnNewPlanet + markModifier) * homeworldModifier, (BaseInfo.MaximumSnackTimeOnNewPlanet + markModifier) * homeworldModifier ) )
                        continue; // this unit should rage out on its planet for a while

                    markModifier = (entity.CurrentMarkLevel - 1) * BaseInfo.BreakTimeIncreasePerMark;
                    if ( World_AIW2.Instance.GameSecond - entity.GameSecondEnteredThisPlanet < Context.RandomToUse.Next( (BaseInfo.MinimumBreakTime + markModifier) * homeworldModifier, (BaseInfo.MaximumBreakTime + markModifier) * homeworldModifier ) )
                        continue; // this unit should wait for a short time to see if anything it can eat arrives before moving on
                    if ( entity.TypeData.GetHasTag( MacrophageFactionBaseInfo.EnragedHarvesterTag ) )
                    {
                        Planet kingPlanet = null;
                        // Start with a 50% chance to attack the ai; increases up to 90% once there are only Tamed hives in the galaxy.
                        int attackAIChance = 50;
                        int globalCount = MacrophageFactionBaseInfoCore.GlobalTeliaCount();
                        if ( MacrophageTamedFactionBaseInfo.Instance != null && globalCount > 0 )
                            attackAIChance += (FInt.Create( 40, false ) * MacrophageTamedFactionBaseInfo.Instance.Telia.Count / globalCount).ToInt();
                        if ( Context.RandomToUse.Next( 0, 100 ) < attackAIChance )
                            kingPlanet = FactionUtilityMethods.Instance.findAIKing( false );
                        else
                            kingPlanet = FactionUtilityMethods.Instance.findHumanKing( false );
                        if ( kingPlanet == null )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "The king is dead, so just chill", Verbosity.DoNotShow );
                            continue;
                        }
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Enraged Macrophage " + entity.PrimaryKeyID + " on " + entity.GetPlanetName_Safe() + " is heading for a human king at " + kingPlanet.Name, Verbosity.DoNotShow );
                        PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "MacrophageEnragedLRP", entity.Planet, kingPlanet, PathingMode.Default, Context, pathingCacheData );
                        if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                        {
                            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
                            command.RelatedString = "Phage_Rage";
                            command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                            for ( int k = 0; k < pathCache.PathToReadOnly.Count && command.RelatedIntegers.Count < Context.RandomToUse.Next( 2, 5 ); k++ )
                                command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                        }
                    }

                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Macrophage Enraged LRP error: " + e, Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();
            }
        }
    }
}
