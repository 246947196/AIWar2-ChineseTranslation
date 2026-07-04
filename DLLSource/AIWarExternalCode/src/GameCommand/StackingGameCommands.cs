using System;
using Arcen.Universal;
using Arcen.AIW2.Core;
using System.Threading;

namespace Arcen.AIW2.External
{
    public class GameCommand_StackUnits : BaseGameCommand
    {
        #region Types
        
        /// <summary>
        /// Just a book-keeping helper for merging death-effect damage
        /// used internally.
        /// </summary>
        struct DamageDealer
        {
            public DeathEffectType Type;
            public int FactionIdx;
            public int Amount;
            
            public DamageDealer(DeathEffectType type, int fid, int amt)
            {
                Type = type;
                FactionIdx = fid;
                Amount = amt;
            }
        }
        
        #endregion

        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            int debugstage = 0;
            List<SafeSquadWrapper> workingSquads = null;
            StructList<DamageDealer> workingDeathFxDealers = null;
            try
            {
                debugstage = 100;
                workingSquads = GameEntity_Squad.GetTemporarySquadList( "GameCommand_StackUnits-workingSquads", 10f );
                if ( workingSquads == null ) //tearing down
                    return;

                if ( command.RelatedEntityIDs == null )
                    return;

                debugstage = 300;

                Planet firstPlanet = null;
                PlanetFaction firstPFaction = null;
                GameEntityTypeData firstTypeData = null;
                foreach ( int id in command.RelatedEntityIDs )
                {
                    var squad = World_AIW2.Instance.GetEntityByID_Squad( id );
                    if ( squad == null || squad.TypeData == null || squad.Planet == null || squad.PlanetFaction == null )
                        continue;

                    debugstage = 400;

                    if ( squad.HasBeenRemovedFromSim || squad.HasNotYetBeenFullyClaimed ||
                        squad.SecondsSpentAsRemains > 0 || squad.ToBeRemovedAtEndOfThisFrame || squad.HasDoneOnDeathSinceLastClaimed )
                        continue;

                    debugstage = 500;

                    if ( workingSquads.Count == 0 )
                    {
                        workingSquads.Add( squad );
                        firstPlanet = squad.Planet;
                        firstPFaction = squad.PlanetFaction;
                        firstTypeData = squad.TypeData;
                    }
                    else
                    {
                        if ( squad.PlanetFaction != firstPFaction || squad.Planet != firstPlanet || squad.TypeData != firstTypeData )
                            continue; //whoops!  This doesn't match, but that's ok -- just skip it; it probably got recycled as a unit or changed planets

                        workingSquads.Add( squad );
                    }
                }

                //UnityEngine.Debug.Log( ( firstPlanet == null ? "NULL" : firstPlanet.Name ) + " Do stack: " + ( firstTypeData == null ? "NULL" : firstTypeData.DisplayName ) + 
                //    " " + command.RelatedEntityIDs.Count + " workingSquads.Count: " + workingSquads.Count );

                debugstage = 600;

                //we have enough living and present specimins to do the stack!
                if ( workingSquads.Count > 1 )
                {
                    int finalStackCount = 0;
                    GameEntity_Squad squadToKeep = null;
                    int totalTransformationTime = 0;
                    int totalShieldPointsLost = 0;
                    int totalHullPointsLost = 0;
                    int totalCurrentParalysisSeconds = 0;
                    int totalCurrentEngineStunSeconds = 0;
                    int totalCurrentWeaponAddedReloadSeconds = 0;

                    debugstage = 700;

                    for ( int i = 0; i < workingSquads.Count; i++ )
                    {
                        GameEntity_Squad squad = workingSquads[i].GetSquad();
                        if ( squad == null )
                            continue;

                        //stack this unit plus any it had stacked in itself
                        finalStackCount += squad.ExtraStackedSquadsInThis + 1;
                        if ( squad.SecondsTillTransformation > 0 )
                            totalTransformationTime += squad.SecondsTillTransformation * (squad.ExtraStackedSquadsInThis + 1);
                        if ( squad.ShieldPointsLost > 0 )
                            totalShieldPointsLost += squad.ShieldPointsLost;
                        if ( squad.HullPointsLost > 0 )
                            totalHullPointsLost += squad.HullPointsLost;
                        if ( squad.CurrentParalysisSeconds > 0 )
                            totalCurrentParalysisSeconds += squad.CurrentParalysisSeconds * (squad.ExtraStackedSquadsInThis + 1);
                        if ( squad.CurrentEngineStunSeconds > 0 )
                            totalCurrentEngineStunSeconds += squad.CurrentEngineStunSeconds * (squad.ExtraStackedSquadsInThis + 1);
                        if ( squad.CurrentWeaponAddedReloadSeconds > 0 )
                            totalCurrentWeaponAddedReloadSeconds += squad.CurrentWeaponAddedReloadSeconds * (squad.ExtraStackedSquadsInThis + 1);

                        debugstage = 800;

                        if ( squadToKeep == null )
                        {
                            squadToKeep = squad;
                        }

                        // Keep track the sum of all death effect damage, per faction dealing it, per death effect type
                        // from all squads being stacked together.
                        foreach ( var pair in squad.DeathEffectCausingDamageReceivedToEntity )
                        {
                            var dfx = pair.Key;
                            var amt = pair.Value;
                            if ( amt <= 0 )
                                continue;

                            int fid;
                            if ( !squad.DeathEffectDamageLastDoneByFactionIndex.TryGetValue( dfx, out fid ) )
                                fid = -1;

                            if ( workingDeathFxDealers == null )
                                workingDeathFxDealers = StructList<DamageDealer>.Get();

                            for ( int k = 0; k < workingDeathFxDealers.Count; k++ )
                            {
                                var itm = workingDeathFxDealers[k];
                                if ( itm.FactionIdx == fid && itm.Type == dfx )
                                {
                                    itm.Amount += amt;
                                    workingDeathFxDealers[k] = itm;

                                    goto done;
                                }
                            }

                            var item = new DamageDealer( dfx, fid, amt );
                            workingDeathFxDealers.Add( item );

                        done:
                            { }
                        }

                        debugstage = 900;

                        if ( squad != squadToKeep )
                        {
                            debugstage = 1000;

                            for ( int j = 0; j < firstTypeData.ChargeTypes.Count; j++ )
                            {
                                var charge = squadToKeep.ChargeAmounts[j];
                                var amt2 = squad.ChargeAmounts[j].Amount;
                                if ( amt2 > charge.Amount )
                                {
                                    charge.Amount = amt2;
                                    squadToKeep.ChargeAmounts[j] = charge;
                                }
                            }

                            debugstage = 1100;

                            if ( squadToKeep.BaseInfo != null && squad.BaseInfo != null )
                                squadToKeep.BaseInfo.DoAfterSingleOtherShipMergedIntoOurStack( squad.BaseInfo );

                            debugstage = 1200;

                            if ( squadToKeep.DeepInfo != null && squad.DeepInfo != null )
                                squadToKeep.DeepInfo.DoAfterSingleOtherShipMergedIntoOurStack( squad.DeepInfo );

                            debugstage = 1300;

                            // Note that this check is too strict; there's code to handle old save games and properly update the MinorFactionStackingID in the SimStep code,
                            // but that can race with the stacking code (so the stacking code sees "Oh, still -1. We can stack  together", then the Sim code assigns the StackingId
                            // after the Stack Command is generated)
                            // if ( squad.MinorFactionStackingID != squadToKeep.MinorFactionStackingID )
                            //     throw new Exception ("Trying to stack together " + squad.ToStringWithPlanetAndOwner() + " id " + squad.MinorFactionStackingID + " and " + squadToKeep.ToStringWithPlanetAndOwner() + " id " + squadToKeep.MinorFactionStackingID );

                            // Despawn the one being merged in.

                            squad.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.RemovedToBeAddedToStack );
                            squad.despawnVis = DespawnVisualization.Transformation;
                            squad.AddOrSetExtraStackedSquadsInThis( 0, true );
                        }
                    }

                    debugstage = 1400;

                    if ( squadToKeep != null )
                    {
                        debugstage = 1500;

                        bool stackingDebug = false;
                        if ( stackingDebug && squadToKeep.Planet.Index == Engine_AIW2.Instance.NonSim_GetPlanetIndexBeingCurrentlyViewed() )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Creating a new stack of " + finalStackCount + " " + squadToKeep.TypeData.InternalName + " on " + squadToKeep.GetPlanetName_Safe(), Verbosity.DoNotShow );

                        squadToKeep.AddOrSetExtraStackedSquadsInThis( (Int16)(finalStackCount - 1), true );

                        if ( totalTransformationTime > 0 )
                            squadToKeep.SecondsTillTransformation = (short)(totalTransformationTime / finalStackCount);
                        if ( totalHullPointsLost > 0 )
                            squadToKeep.TakeDamageDirectly( totalHullPointsLost, null, null, DamageSource.SelfDamageFromMyOwnWeapons, context );
                        if ( totalShieldPointsLost > 0 )
                            squadToKeep.ShieldPointsLost = Math.Min( totalShieldPointsLost, squadToKeep.GetMaxShieldPoints() );
                        if ( totalCurrentParalysisSeconds > 0 )
                            squadToKeep.CurrentParalysisSeconds = (short)(totalCurrentParalysisSeconds / finalStackCount);
                        if ( totalCurrentEngineStunSeconds > 0 )
                            squadToKeep.CurrentEngineStunSeconds = (short)(totalCurrentEngineStunSeconds / finalStackCount);
                        if ( totalCurrentWeaponAddedReloadSeconds > 0 )
                            squadToKeep.CurrentWeaponAddedReloadSeconds = (short)(totalCurrentWeaponAddedReloadSeconds / finalStackCount);

                        debugstage = 1600;

                        // The stacked squad will have the sum of all death effect damage dealt to the original
                        // squads being stacked. Since that damage could have different factions that dealt it
                        // on those original squads, we keep the faction having dealt the most.
                        if ( workingDeathFxDealers != null )
                        {
                            while ( workingDeathFxDealers.Count > 0 )
                            {
                                debugstage = 1700;

                                var item = workingDeathFxDealers.PopLast();

                                int fid_greatest = item.FactionIdx;
                                int amt_greatest = item.Amount;

                                for ( int i = 0; i < workingDeathFxDealers.Count; i++ )
                                {
                                    var itr = workingDeathFxDealers[i];
                                    if ( itr.Type == item.Type )
                                    {
                                        if ( itr.Amount > amt_greatest )
                                        {
                                            fid_greatest = itr.FactionIdx;
                                            amt_greatest = itr.Amount;
                                        }

                                        item.Amount += itr.Amount;

                                        if ( !itr.Type.AmountsAreUncapped )
                                        {
                                            int max = itr.Type.Scale * squadToKeep.ShipCount;
                                            if ( item.Amount > max )
                                                item.Amount = max;
                                        }

                                        workingDeathFxDealers.RemoveAt( i );
                                        i--;
                                    }
                                }

                                item.FactionIdx = fid_greatest;

                                squadToKeep.DeathEffectCausingDamageReceivedToEntity[item.Type] = item.Amount;
                                squadToKeep.DeathEffectDamageLastDoneByFactionIndex[item.Type] = item.FactionIdx;
                            }
                        }
                    }
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    LOG.Err( "Exception occured in {0}() at debugstage {1}:\n{2}", this.TypeNameAndMethod(), debugstage, e );
            }
            finally
            {
                GameEntity_Squad.ReleaseTemporarySquadList( workingSquads );
                workingDeathFxDealers?.Return();
            }
        }
    }

    public class GameCommand_SplitStack : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            if ( command.RelatedEntityIDs == null )
                return;
            foreach ( int id in command.RelatedEntityIDs )
            {
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( id );
                if ( entity == null || entity.TypeData == null || entity.Planet == null )
                    continue;

                
            }
        }
    }
}
