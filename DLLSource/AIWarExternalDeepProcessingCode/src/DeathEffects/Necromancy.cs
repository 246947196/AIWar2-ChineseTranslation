using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class DeathEffect_Necromancy : IDeathEffectImplementation
    {
        public DeathEffectType Type;
        public void SetType(DeathEffectType type)
        {
            Type = type;
        }

        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "DeathEffect_Necromancys" );
        public DeathEffect_Necromancy()
        {
            RefTracker.IncrementObjectCount();
        }
        public override string ToString()
        {
            return "Necromancy"; //makes exports more brief and clear
        }
        public void HandleDeathWithEffectApplied_AfterFullDeathOrPartOfStackDeath( bool IsFromOnlyPartOfStackDying, GameEntity_Squad Entity, ref int ThisDeathEffectDamageSustained, 
            Faction FactionThatDidTheKilling, Faction FactionResponsibleForTheDeathEffect, int NumShipsDying, ArcenSimContextAnyStatus Context )
        {
            bool debug = false;

            int debugstage = 0;
            try
            {
                debugstage = 1;
                var hostCtx = Context?.GetHostOnlyContext();
                if ( hostCtx == null ) //client
                    return;
                debugstage = 2;
                if ( FactionResponsibleForTheDeathEffect == null )
                    return;
                if ( !Entity.TypeData.IsMobileCombatant )
                    return;
                if ( Entity.TypeData.IsKingUnit )
                    return;
                if ( Entity.TypeData.IsBattlestation )
                    return;
                if ( Entity.TypeData.IsMobileFleetFlagship )
                    return;

                debugstage = 3;
                var dlc3_data = Entity.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
                if ( dlc3_data != null )
                {
                    if ( dlc3_data.ImmuneToNecromancy )
                        return;
                }

                debugstage = 4;
                
                if ( debug )ArcenDebugging.ArcenDebugLogSingleLine("Triggering necromancy on " + Entity.ToStringWithPlanet() + " caused by " + FactionResponsibleForTheDeathEffect.GetDisplayName(), Verbosity.DoNotShow );

                debugstage = 10;

                if ( FactionResponsibleForTheDeathEffect.Type != FactionType.Player )
                {
                    if (FactionResponsibleForTheDeathEffect.Type == FactionType.AI)
                    {
                        // The AI somehow got necromancer units. 
                        // todo: maybe handle this or let the ai type...
                        //var sentinalData = FactionResponsibleForTheDeathEffect.TryGetAISentinelsCoreData();
                        //sentinalData.SentinelInfo.AIType.Implementation.HandleNecromancy( Entity, Context );
                    }
                    else if (FactionResponsibleForTheDeathEffect.SpecialFactionData.InternalName == "Reapers")
                    {
                        if ( debug ) ArcenDebugging.LogSingleLine("reapers: " + FactionResponsibleForTheDeathEffect.GetDisplayName(), Verbosity.DoNotShow );
                        ReapersFactionDeepInfo.HandleReaperNecromancy( Entity, hostCtx );
                    }

                    return;
                }

                debugstage = 20;

                Faction destinationFaction = null;
                if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( FactionResponsibleForTheDeathEffect ) )
                    destinationFaction = FactionResponsibleForTheDeathEffect;
                else
                    destinationFaction = FactionUtilityMethods.Instance.GetStrongestNecromancerFactionOnPlanet( Entity.Planet );
                if ( destinationFaction == null )
                    return;

                debugstage = 30;

                if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine("donating ship to  " + destinationFaction.GetDisplayName(), Verbosity.DoNotShow );
            
                //if ( Entity.GetSpecialFactionIsImmuneToZombification_Safe() )
                //    return false; //DO necromance this faction
                Fleet fleet = NecromancerEmpireFactionDeepInfo.GetBestMobileFleetForShip( Entity, destinationFaction, hostCtx );
                if ( fleet == null )
                {
                    if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine("didnt find a suitable fleet", Verbosity.DoNotShow );

                    return; //no good fleet for me
                }

                debugstage = 40;

                GameEntityTypeData EntityTypeToCreate = NecromancerEmpireFactionDeepInfo.GetShipToSummonViaNecromancy( Entity, fleet, hostCtx );
                if (EntityTypeToCreate == null)
                {
                    LOG.Msg("Warning: dying ship '{0}' returned unknown/null type to create for necromancy.", Entity.GetTypeDisplayNameSafe());
                    return;
                }

                GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                PlanetFaction pFactionForNewEntity = Entity.Planet.GetPlanetFactionForFaction( destinationFaction );
                if (pFactionForNewEntity == null)
                {
                    LOG.Msg("Warning: necromancy-handledeath for dying ship '{0}' but dstfaction {1} appears to have no PlanetFaction on {2}.", 
                        Entity.ToStringWithPlanetAndOwner(), destinationFaction.ToString(), Entity.Planet?.Name.OrNull());

                    return;
                }

                ArcenPoint spawnPoint = Entity.WorldLocation;

                debugstage = 50;

                // if ( Entity.Planet != centerpiece.Planet ) //just spwan where you were killed; that makes more sense
                // {
                //     GameEntity_Squad flagship = fleet.Centerpiece.GetSquad();

                //     spawnPoint = flagship.Planet.GetSafePlacementPoint_AroundEntity( Context, EntityTypeToCreate, flagship,
                //                                                                      FInt.FromParts(0, 020), FInt.FromParts(0, 100) );
                // }

                int shipsToCreate = NecromancerEmpireFactionDeepInfo.GetNumShipsToCreate( EntityTypeToCreate, destinationFaction, fleet, hostCtx);
                if ( EntityTypeToCreate == null )
                    return; //no ship line found to create?

                debugstage = 60;

                if ( debug ) ArcenDebugging.ArcenDebugLogSingleLine("spawning some " + EntityTypeToCreate.GetDisplayName(), Verbosity.DoNotShow );

                {
                    //Handle journals
                    if ( EntityTypeToCreate.GetHasTag("NecromancerBottomTier") )
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Skeletons", string.Empty, destinationFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    if ( EntityTypeToCreate.GetHasTag("NecromancerMidTier") )
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Wights", string.Empty, destinationFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    if ( EntityTypeToCreate.GetHasTag("NecromancerHighTier") )
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Necromancer_Mummies", string.Empty, destinationFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                }

                debugstage = 70;

                if ( shipsToCreate > 1 )
                {
                    NecromancerMobileFleetBaseInfo necroMobileFleetInfo = fleet.GetExternalBaseInfoAs<NecromancerMobileFleetBaseInfo>();
                    if ( EntityTypeToCreate.GetHasTag("NecromancerBottomTier") )
                    {
                        necroMobileFleetInfo.BonusSkeletonsEarned += (shipsToCreate - 1);
                    }
                    if ( EntityTypeToCreate.GetHasTag("NecromancerMidTier") )
                    {
                        necroMobileFleetInfo.BonusWightsEarned += (shipsToCreate - 1);
                    }
                    if ( EntityTypeToCreate.GetHasTag("NecromancerHighTier") )
                    {
                        necroMobileFleetInfo.BonusMummiesEarned += (shipsToCreate - 1);
                    }
                }
                
                debugstage = 80;

                byte markLevel = destinationFaction.GetGlobalMarkLevelForShipLine( Entity.TypeData );
            
                //If this is a "remote create" (ie not on the planet with the centerpiece of the destination fleet)
                //then create the new ship at the centerpiece. Otherwise a Necromancer player can wind up with players making zombies all over the map and
                //not knowing where they are half the time. Very frustrating
                NecromancerEmpireFactionBaseInfo localBase = destinationFaction.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
                localBase.BattleHarvest[Entity.Planet][EntityTypeToCreate] += shipsToCreate;
                localBase.BattleHarvest[Entity.Planet][EntityTypeToCreate] += shipsToCreate;

                debugstage = 90;

                for ( int i = 0; i < shipsToCreate; i++ )
                {
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFactionForNewEntity, EntityTypeToCreate, markLevel,
                       fleet, 0, spawnPoint, hostCtx, "DeathEffect-Necromancy" );
                
                    if ( newEntity == null )
                        return; //client

                    if ( Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Necromancer ) )
                        ArcenDebugging.ArcenDebugLogSingleLine("Necromancy Practiced (necromancer path) Spawned a " + newEntity.TypeData.GetDisplayName() + ", unit " + i + " of " + shipsToCreate + " being created on " + newEntity.GetPlanetName_Safe() + " for fleet " + fleet.GetName() + " that has flagship " + fleet.Centerpiece.GetSquad().ToStringWithPlanet() , Verbosity.DoNotShow );

                    debugstage = 100;

                    newEntity.AddOrSetExtraStackedSquadsInThis( 0, true );
                    //since damage builds up on a stack, reduce it
                    ThisDeathEffectDamageSustained -= Type.Scale;
                    //for each stacked entity that we killed that we can afford, add it in
                    if ( i == 0 )
                    {
                        //only the first skeleton can get stacks; the bonus ones don't come in stacked
                        for ( int j = 0; j < (NumShipsDying-1); j++ )
                        {
                            if ( ThisDeathEffectDamageSustained <= 0 || ThisDeathEffectDamageSustained < Type.Scale )
                                break;
                            newEntity.AddOrSetExtraStackedSquadsInThis( 1, false );
                            //zombify only the number we can based on the damage total on there
                            ThisDeathEffectDamageSustained -= Type.Scale;
                        }
                    }
                
                    debugstage = 110;

                    newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;
                    newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                    newEntity.NumTimesZombified++; //so necromancer/nanocaust can't trade units back and forth endlessly
                    centerpiece?.Orders.CopyTo( centerpiece, newEntity, newEntity.Orders, true, true, true );
                    if ( EntityTypeToCreate.GetHasTag("WightVariant") )
                    {
                        newEntity.SecondsTillTransformation = (short)(60 * AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "WightDecayInterval" ) );
                        newEntity.TransformsIntoAfterTime = "BaseWight";
                    }
                    if ( EntityTypeToCreate.GetHasTag("NecromancerWight") )
                    {
                        newEntity.SecondsTillTransformation = (short)(60 * (short)AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "WightDecayInterval" ) );
                        newEntity.TransformsIntoAfterTime = "WightAttritioner";
                    }
                    if ( EntityTypeToCreate.GetHasTag("SkeletonVariant") )
                    {
                        newEntity.SecondsTillTransformation = (short)(60 * AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "SkeletonDecayInterval" ));
                        newEntity.TransformsIntoAfterTime = "BaseSkeleton";
                    }
                    if ( EntityTypeToCreate.GetHasTag("NecromancerBaseSkeleton") )
                    {
                        newEntity.SecondsTillTransformation = (short)(60 * AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "SkeletonDecayInterval" ));
                        newEntity.TransformsIntoAfterTime = "SkeletonAttritioner";
                    }

                    debugstage = 120;
                }
            }
            catch (Exception e)
            {
                LOG.Err("exception at debugstage {0}:\n{1}", debugstage, e);
            }
        }
    }
}
