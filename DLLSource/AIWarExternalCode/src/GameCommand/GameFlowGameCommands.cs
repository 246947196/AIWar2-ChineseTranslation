using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class GameCommand_FlushReinforcementPoints : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            Planet planet = World_AIW2.Instance.GetPlanetByIndex( command.PlanetOrderWasIssuedFrom );
            if ( planet == null )
                return;
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.ReinforcementLocations ) )
            {
                entity.DeployAIReinforcementContents( context.GetHostOnlyContext() );
            }
        }
    }

    public class GameCommand_HandleConclusion : BaseGameCommand
    {
        //this exists so that the host can tell clients when the game is over
        //clients are terrible at figuring this out for themselves
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedBool )
                World.Instance.ConclusionType = CampaignConclusionType.Won;
            else
                World.Instance.ConclusionType = CampaignConclusionType.Lost;
        }
    }

    public class GameCommand_TransferEntitiesToFaction : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            foreach ( GameEntity_Squad entity in command.RelatedSquads() )
            {
                //Nota Bene: there's a weirdness where if you send a unit a SetWormholePath game command
                //and a transfer entities game command in the same LRP cycle, the transferred unit can get the (no longer valid) orders
                //To deal with this, set the RelatedFactionIndex when sending the WormholeCommand so we can ignore the transferred unit.
                EndpointFunctions.TransferEntityToFaction( entity, command.GetRelatedFaction(), "GameCommand Called: TransferEntitiesToFaction" );
            }
        }
    }    

    /// <summary>
    /// This is only really used for the first unpause of the game, after generating the real world
    /// </summary>
    public class GameCommand_UnpauseOnly : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            //ArcenDebugging.ArcenDebugLogSingleLine( "GameCommand_UnpauseOnly: " + World.Instance.IsPaused, Verbosity.Chat );
            //our work here is done!
            if ( !World.Instance.IsPaused )
                return;
            World.Instance.IsPaused = false;
            World_AIW2.Instance.Setup.ShouldSeedDetailsYet = true;
        }
    }

    /// <summary>
    /// This is only really used for when the game-over screens arehit
    /// </summary>
    public class GameCommand_PauseOnly : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            //ArcenDebugging.ArcenDebugLogSingleLine( "GameCommand_UnpauseOnly: " + World.Instance.IsPaused, Verbosity.Chat );
            //our work here is done!
            if ( World.Instance.IsPaused )
                return;
            World.Instance.IsPaused = true;
        }
    }

    public class GameCommand_ScrapUnits : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedEntityIDs == null )
                return;

            bool scrapContentsOnly = command.RelatedBool;
            FInt maxAIPurchaseCostOfContentsToScrap = (FInt)command.RelatedMagnitude;
            bool thereIsAMaxAIPurchaseCostToScrap = maxAIPurchaseCostOfContentsToScrap > 0;
            FInt aiPurchaseCostScrapped = FInt.Zero;
            #region Tracing
            bool tracing = Engine_AIW2.TraceAtAll && command.GetRelatedFaction() != null && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.EntityRemoval );
            ArcenCharacterBuffer tracingBuffer = null;
            if ( tracing ) tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "GameCommand_ScrapUnits-trace", 10f ) : null;
            if ( tracing ) tracingBuffer.Add( "executing" ).Add( command.GetRelatedFaction().SpecialFactionData.InternalName ).Add( " scrap command" );
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "maxAIPurchaseCostOfContentsToScrap => " ).Add( maxAIPurchaseCostOfContentsToScrap.ReadableString );
            #endregion

            int percentageToReturnOfMetalAsInt = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "ScrapRefundsOnFriendlyPlanets" );
            FInt percentageToReturnOfMetalAsFIntMult = (FInt)percentageToReturnOfMetalAsInt / FInt.OneHundred;

            foreach ( int id in command.RelatedEntityIDs )
            {
                GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( id );
                if ( entity == null )
                    continue;

                if (Engine_AIW2.Instance.IsTestChamber == false)
                {
                    switch ( entity.TypeData.SpecialType )
                    {
                        case SpecialEntityType.HumanHomeCommand:
                            if ( command.ShouldGenerateLocalUIFeedback )
                                World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot scrap your home command station!", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                            continue;
                        case SpecialEntityType.BattlestationBasic:
                        case SpecialEntityType.BattlestationCitadel:
                            if ( command.ShouldGenerateLocalUIFeedback )
                                World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot scrap a Battlestation!", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                            continue;
                        case SpecialEntityType.CityCenter:
                            if ( command.ShouldGenerateLocalUIFeedback )
                                World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot scrap a " + entity.TypeData.NameForCityCenter + "!", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                            continue;
                        case SpecialEntityType.MobileOfficerCombatFleetFlagship:
                        case SpecialEntityType.MobileStrikeCombatFleetFlagship:
                        case SpecialEntityType.MobileCustomUnattachedFleetFlagship:
                        case SpecialEntityType.MobileSupportFleetFlagship:
                        case SpecialEntityType.MobileCustomCityFedFleetFlagship:
                            if ( command.ShouldGenerateLocalUIFeedback )
                                World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot scrap a Flagship!", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                            continue;
                        case SpecialEntityType.LoneGolem:
                            if ( command.ShouldGenerateLocalUIFeedback )
                                World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot scrap a Golem!", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                            continue;
                    }
                    if ( entity.TypeData.IsScrappingByPlayerDisallowed && !entity.TypeData.IsScrappingByPlayerToTurnUnclaimed )
                    {
                        if ( command.ShouldGenerateLocalUIFeedback )
                            World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot scrap your " + entity.TypeData.DisplayName + "!", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                        continue;
                    }
                }
                //Chris note: being able to scrap and move these is useful.  It will prevent you from building another type anyhow other than the type you scrapped.
                //if ( entity.TypeData.IsCommandStation )
                //{
                //    if ( entity.Planet.GetIsPlayerCommandStationPinned() )
                //    {
                //        if ( command.ShouldGenerateLocalUIFeedback )
                //            World_AIW2.Instance.QueueChatMessageOrCommand( "Cannot scrap your " + entity.TypeData.DisplayName + 
                //                ".  It is currently pinned by either an incoming wave, or 1+ enemy strength on the planet.", ChatType.ShowLocallyOnly, "CannotDoThatThing" );
                //        continue;
                //    }
                //}
                if ( !scrapContentsOnly )
                    aiPurchaseCostScrapped += entity.TypeData.CostForAIToPurchase + entity.GetCostForAIToPurchaseOfContentsIfAny();
                else
                {
                    if ( entity.AIReinforcementPointContents != null )
                    {
                        for ( int j = 0; j < entity.AIReinforcementPointContents.Count; j++ )
                        {
                            RefPair<GameEntityTypeData, int> record = entity.AIReinforcementPointContents[j];
                            int purchaseCostPerItem = record.LeftItem.CostForAIToPurchase;
                            //FInt totalPurchaseCostInThisItem = aiPurchaseCostPerItem * record.NumberContained;
                            int numberToScrap = record.RightItem;
                            if ( thereIsAMaxAIPurchaseCostToScrap && purchaseCostPerItem > FInt.Zero )
                                numberToScrap = ((maxAIPurchaseCostOfContentsToScrap - aiPurchaseCostScrapped) / purchaseCostPerItem).GetNearestIntPreferringHigher();
                            if ( numberToScrap <= 0 )
                                break;
                            entity.AddToAIReinforcementPointContents( record.LeftItem, -numberToScrap, "ScrapUnits", string.Empty );
                            if ( record.RightItem <= 0 )
                                j--;
                            aiPurchaseCostScrapped += purchaseCostPerItem * numberToScrap;
                        }
                    }
                }

                // note: do this before the call to TakeDamageDirectly/Despawn so we know if it was actually alive.
                for ( int x = 0; x < ExternalWorldDeepInfoSourceTable.Instance.Rows.Count; x++ )
                {
                    ExternalWorldDeepInfo info = ExternalWorldDeepInfoSourceTable.Instance.Rows[x].Singleton;
                    if ( info == null )
                        continue;

                    if ( info.GetShouldIBeInUse() )
                        info.Safe_DoOnPlayerScrapped( entity, entity.PlanetFaction.Faction, context.GetHostOnlyContext() );
                }

                if ( entity.TypeData.IsScrappingByPlayerToTurnUnclaimed )
                {
                    if ( percentageToReturnOfMetalAsInt > 0 )
                        Helper_RefundScrapAmountForPlayer( entity, percentageToReturnOfMetalAsFIntMult );
                    entity.AddOrSetExtraStackedSquadsInThis( 0, true );
                    entity.TakeDamageDirectly( entity.GetCurrentHullPoints() + entity.GetCurrentShieldPoints() + 100, null, null, DamageSource.BeingScrapped, context );
                }
                else
                {
                    if ( !scrapContentsOnly )
                    {
                        if ( entity.GetIsPlayerUnit() )
                        {
                            if ( percentageToReturnOfMetalAsInt > 0 )
                                Helper_RefundScrapAmountForPlayer( entity, percentageToReturnOfMetalAsFIntMult );

                            entity.AddOrSetExtraStackedSquadsInThis( 0, true );
                            entity.TakeDamageDirectly( entity.GetCurrentHullPoints() + entity.GetCurrentShieldPoints() + 100, null, null, DamageSource.BeingScrapped, context );
                        }
                        else //NPCs
                        {
                            entity.Despawn( context, entity.GetFactionTypeSafe() != FactionType.Player, InstancedRendererDeactivationReason.PlayerIsScrappingMe );

                            //this doesn't happen on this route, but it does on all the others
                            if ( entity.TypeData.AIPOnDeath != 0 && entity.SelfBuildingMetalRemaining <= FInt.Zero )
                            {
                                GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)entity.TypeData.AIPOnDeath, AIPChangeReason.EntityDeath, entity.TypeData, -1, entity.Planet.Index, entity.GetFactionIndex_Safe() );
                            }
                        }
                    }
                }

                if ( thereIsAMaxAIPurchaseCostToScrap && aiPurchaseCostScrapped > maxAIPurchaseCostOfContentsToScrap )
                    break;
            }
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "end of executing scrap command" );
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToStringAndReturnToPool(), Verbosity.Chat );
            }
            #endregion
        }

        private void Helper_RefundScrapAmountForPlayer( GameEntity_Squad entity, FInt percentageToReturnOfMetalAsFIntMult )
        {
            if ( entity == null )
                return;
            Faction owningFaction = entity.GetFactionOrNull_Safe();
            if ( owningFaction == null || owningFaction.Type != FactionType.Player )
                return; //only works for players

            Planet planet = entity.Planet;
            if ( planet == null )
                return;
            Faction controllingFaction = planet.GetControllingOrInfluencingFaction();
            if ( controllingFaction == null || !controllingFaction.GetIsFriendlyTowards( owningFaction ) )
                return; //only applise on planets owned by factions friendly to the human player

            int refundAmount = 0;

            FInt hullHealthPercentage = (FInt)entity.GetCurrentHullPoints() / (FInt)entity.GetMaxHullPoints();

            int metalCostForScrapping = (entity.DataForMark.MetalCost * entity.TypeData.MetalCostMultiplierForScrapping).GetNearestIntPreferringHigher();

            //refund the main entity only at the percentage of hull health it has.
            refundAmount += (hullHealthPercentage * metalCostForScrapping * percentageToReturnOfMetalAsFIntMult).GetNearestIntPreferringHigher();

            //refund each squad in the stack beyond the first at full value since they are not damaged
            if ( entity.ExtraStackedSquadsInThis > 0 )
                refundAmount += (entity.ExtraStackedSquadsInThis * metalCostForScrapping * percentageToReturnOfMetalAsFIntMult).GetNearestIntPreferringHigher();

            if ( refundAmount > 0 )
                owningFaction.StoredMetal += refundAmount;
        }
    }

    public class GameCommand_TransformUnits : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedEntityIDs == null )
                return;
            int debugCode = 0;
            try
            {
                int[] relatedIntegersSnapshot = null;
                if ( command.RelatedIntegers != null )
                {
                    relatedIntegersSnapshot = new int[command.RelatedIntegers.Count];
                    command.RelatedIntegers.CopyTo( relatedIntegersSnapshot, 0 );
                }
                int i = 0;
                foreach ( int id in command.RelatedEntityIDs )
                {
                    debugCode = 1;
                    debugCode = 2;
                    GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( id );
                    debugCode = 3;
                    int numEntitiesToSpawn = 0;
                    if ( relatedIntegersSnapshot != null && relatedIntegersSnapshot.Length > i )
                    {
                        debugCode = 4;
                        numEntitiesToSpawn = relatedIntegersSnapshot[i];
                    }
                    i++;
                    if ( numEntitiesToSpawn == 0 )
                        numEntitiesToSpawn++;
                    if ( entity == null )
                        continue;
                    debugCode = 5;
                    GameEntityTypeData newType = GameEntityTypeDataTable.Instance.GetRowByName( command.RelatedString2 );
                    if ( newType.HackingCostForOtherFleetLeadersOfSameTypeToBecomeMe > 0 )
                    {
                        Faction faction = entity.GetFactionOrNull_Safe();
                        if ( faction.StoredHacking < newType.HackingCostForOtherFleetLeadersOfSameTypeToBecomeMe )
                        {
                            if ( faction.GetIsLocalFaction() )
                                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, '黑客点数不足！',
                                    '派系 ' + faction.GetDisplayName() + ' 没有足够的黑客点数来转化 ' + entity.TypeData.DisplayName, '确定' );
                            return;
                        }
                        if ( entity.GetIsCrippled() )
                        {
                            if ( faction.GetIsLocalFaction() )
                                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, '转化目标已瘫痪！',
                                    entity.TypeData.DisplayName + ' 已瘫痪，派系 ' + faction.GetDisplayName() + ' 没有转化它。', '确定' );
                            return;
                        }
                        if ( entity.SelfBuildingMetalRemaining > FInt.Zero )
                        {
                            if ( faction.GetIsLocalFaction() )
                                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, '转化目标未完成建造！',
                                    entity.TypeData.DisplayName + ' 仍在建造中，派系 ' + faction.GetDisplayName() + ' 没有转化它。', '确定' );
                            return;
                        }
                        if ( entity.HasNotYetBeenFullyClaimed )
                        {
                            if ( faction.GetIsLocalFaction() )
                                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, '转化目标尚未被占领！',
                                    entity.TypeData.DisplayName + ' 尚未被完全占领，派系 ' + faction.GetDisplayName() + ' 没有转化它。', '确定' );
                            return;
                        }

                        //Fleet fleetOrNull = entity.GetFleetOrNull_Safe();

                        GameEntityTypeData oldType = entity.TypeData;
                        GameEntity_Squad newEntity = entity.TransformInto( context.GetHostOnlyContext(), newType, numEntitiesToSpawn, true );
                        if ( newEntity != null )
                        {
                            HackingType hackToDo = HackingTypeTable.Instance.GetRowByName( "TransformShip" );

                            HackingEvent hackEvent = HackingEvent.Create( faction.FactionIndex, faction.FactionIndex, -1,
                                hackToDo, null, false, entity.TypeData.DisplayName + " into " + newType.DisplayName, -1 );

                            faction.StoredHacking -= newType.HackingCostForOtherFleetLeadersOfSameTypeToBecomeMe;

                            FInt respeccedHacking = FInt.Zero;

                            if ( newEntity.TypeData.IsFleetLeader )
                            {
                                if ( newEntity.TypeData.TechUpgradeThatIsUsedForMyDirectScience != oldType.TechUpgradeThatIsUsedForMyDirectScience )
                                {
                                    //these costs different amounts to upgrade, so refund them!
                                    Fleet newFleet = newEntity.GetFleetOrNull_Safe();
                                    if ( newFleet != null && newFleet.AddedMarkLevelsForFleet_FromScience > 0 )
                                    {
                                        Faction newFleetFaction = newFleet.Faction;
                                        if ( newFleetFaction != null )
                                            newFleetFaction.RefundFleetIfPossible( newFleet, false );
                                    }
                                }
                                if ( entity.FleetMembership.Hacked_HullHealthMultiplier > FInt.One || entity.FleetMembership.Hacked_ShieldHealthMultiplier > FInt.One ||
                                    entity.FleetMembership.Hacked_WeaponsMultiplier > FInt.One || entity.FleetMembership.Hacked_ExtraWatchPlanetsAtXHops > 0 )
                                {
                                    FInt currentHackCost;
                                    FInt costMultiplier;
                                    int timesHacked;

                                    Dictionary<HackingType, FInt> costsByHack = TempCollectionHelper.GetTemporaryHackingTypeFIntDict("GameCommand_TransformUnits-costsByHack", 10f );
                                    if ( costsByHack == null ) //blocked for teardown/shutdown; bail
                                        return;

                                    List<HackingType> hacks = entity.TypeData.GetListOfHacks();
                                    HackData hack;
                                    for ( int j = 0; j < hacks.Count; j++ )
                                    {
                                        if ( !hacks[j].Implementation.GetIsHackOfSelfUnit() )
                                        {
                                            continue;
                                        }
                                        timesHacked = entity.GetNumberOfTimesHacked( hacks[j] );
                                        if ( timesHacked == 0 )
                                        {
                                            continue;
                                        }
                                        hack = entity.TypeData.GetHackDataOrNull( hacks[j] );
                                        costMultiplier = FInt.One;
                                        currentHackCost = FInt.Zero;
                                        for ( int k = 0; k < timesHacked; k++ )
                                        {
                                            currentHackCost += (FInt)(costMultiplier * hack.OverridingCostInHackingPoints).GetNearestIntPreferringHigher();
                                            costMultiplier *= hack.OverridingCostInHackingPointsMultiplierPerTimesSameUnitHacked;
                                        }
                                        respeccedHacking += currentHackCost;
                                        costsByHack[hacks[j]] = currentHackCost;
                                    }
                                    hacks = newEntity.TypeData.GetListOfHacks();
                                    faction.StoredHacking += respeccedHacking;//since in the next stage the hacks will be "repeated" they will cost hacking points, so these points must be respecced now
                                    for ( int j = 0; j < hacks.Count; j++ )//apply the hacks already done, as far as possible
                                    {
                                        if ( !costsByHack.ContainsKey( hacks[j] ) )
                                        {
                                            continue;
                                        }
                                        hack = newEntity.TypeData.GetHackDataOrNull( hacks[j] );
                                        costMultiplier = FInt.One;
                                        for ( int k = 0; k < hack.MaxTimesSingleUnitCanBeHacked; k++ )
                                        {
                                            currentHackCost = (FInt)(costMultiplier * hack.OverridingCostInHackingPoints).GetNearestIntPreferringHigher();
                                            if ( currentHackCost > costsByHack[hacks[j]] )
                                            {
                                                break;
                                            }
                                            hacks[j].Implementation.DoSuccessfulCompletionLogic_CalledFromMainSimOnly( newEntity, newEntity.Planet, newEntity, context.GetHostOnlyContext(), hacks[j], hackEvent );
                                            costsByHack[hacks[j]] -= currentHackCost;
                                            respeccedHacking -= currentHackCost;
                                            costMultiplier *= hack.OverridingCostInHackingPointsMultiplierPerTimesSameUnitHacked;
                                        }
                                    }

                                    TempCollectionHelper.ReleaseTemporaryHackingTypeFIntDict( costsByHack );
                                }
                                hackEvent.HackingPointsSpent = newType.HackingCostForOtherFleetLeadersOfSameTypeToBecomeMe - respeccedHacking;
                                faction.HackingHistory.Add( hackEvent );
                                if ( respeccedHacking > FInt.Zero )
                                {
                                    World_AIW2.Instance.QueueChatMessageOrCommand( "Transforming the " + entity.GetTypeDisplayNameSafe() + " into a " + newEntity.GetTypeDisplayNameSafe() +
                                        " respecced " + ArcenExternalUIUtilities.HackingTextColorAndIcon + " " + respeccedHacking +
                                        "</color> because of incompatible self-hacking between the types", ChatType.ShowLocallyOnly, null );
                                }
                            }
                        }
                    }
                    else
                        entity.TransformInto( context.GetHostOnlyContext(), newType, numEntitiesToSpawn, entity.TypeData.KeepDamageAndDebuffsOnTransformation );
                }
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Execute_TransformUnits: debug code " + debugCode + "\n Exception " + e + "\n", Verbosity.DoNotShow );
            }
        }
    }

    public class GameCommand_CreateSpeedGroup : BaseGameCommand
    {
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            if ( command.RelatedEntityIDs == null )
                return;
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //host only!

            int debugStage = 0;
            try
            {
                debugStage = 1000;
                if ( command.RelatedString == "DestroySpeedGroups" )
                {
                    debugStage = 1100;
                    //remove but don't create a new one
                    foreach ( int id in command.RelatedEntityIDs )
                    {
                        debugStage = 1200;
                        debugStage = 1300;
                        GameEntity_Squad ship = World_AIW2.Instance.GetEntityByID_Squad( id );
                        debugStage = 1400;
                        if ( ship == null || ship.TypeData == null || ship.Planet == null )
                            continue;
                        debugStage = 1500;
                        SpeedGroup group = ship.GroupMoveSpeed_HostOnly;
                        if ( group != null )
                        {
                            group.RemoveEntity_HostOnly( ship );
                            group = null; //never do this without first calling RemoveEntity
                            ship.SpeedLimitFromGroupMove = 0;
                        }
                    }
                    return;
                }

                debugStage = 3000;

                bool isPlayerStyle = false;
                if ( command.RelatedString == "PlayerStyle" )
                    isPlayerStyle = true;

                debugStage = 3100;

                //we need to create it later when we have a first faction
                SpeedGroup newSpeedGroup = null;

                debugStage = 5000;

                foreach ( int id in command.RelatedEntityIDs )
                {
                    debugStage = 5100;
                    debugStage = 5200;
                    GameEntity_Squad ship = World_AIW2.Instance.GetEntityByID_Squad( id );
                    if ( ship == null || ship.TypeData == null || ship.Planet == null )
                        continue;
                    debugStage = 5300;
                    SpeedGroup group = ship.GroupMoveSpeed_HostOnly;
                    if ( group != null )
                    {
                        group.RemoveEntity_HostOnly( ship );
                        group = null; //never do this without first calling RemoveEntity
                        ship.SpeedLimitFromGroupMove = 0;
                    }
                    debugStage = 5400;

                    if ( newSpeedGroup == null )
                        newSpeedGroup = SpeedGroup.Create_CallFromHostOnly( ship.GetFactionOrNull_Safe(),
                            isPlayerStyle );

                    if ( newSpeedGroup != null )
                    {
                        debugStage = 5500;
                        newSpeedGroup.AddEntity_HostOnly( ship, false );
                    }
                }
                debugStage = 5700;
                if ( newSpeedGroup != null )
                {
                    debugStage = 5800;
                    newSpeedGroup.CalculateUnitSpeedLimits_HostOnly();
                    debugStage = 5900;
                }
                debugStage = 8000;
                //override the speed limit
                if ( command.RelatedIntegers.Count > 0 )
                {
                    debugStage = 8100;
                    int newValue = command.RelatedIntegers.First;
                    debugStage = 8200;
                    if ( newValue > Int16.MaxValue - 1 )
                        newValue = Int16.MaxValue - 1;
                    debugStage = 8300;
                    try
                    {
                        if ( newSpeedGroup != null )
                            newSpeedGroup.SetOverrideSpeedLimit( (Int16)newValue );
                    }
                    catch { } //if this somehow manages to error, we don't care
                    //ArcenDebugging.ArcenDebugLogSingleLine("overriding speed limit for group " + newSpeedGroup.SpeedGroupID + " to " + newSpeedGroup.OverrideSpeedLimit, Verbosity.DoNotShow );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in GameCommand_CreateSpeedGroup debugStage " + debugStage + ". Error: " + e.ToString(), Verbosity.ShowAsError );
            }
        }
    }

    public class GameCommand_CancelHack : BaseGameCommand
    {
        //This takes as arguments a faction and undoes all the fireteams for that faction
        //High impact. Used as a testing tool for fireteam logic,
        //though vassals could perhaps use this too?
        public override void Execute( GameCommand command, ArcenClientOrHostSimContextCore context )
        {
            int squadID = command.RelatedIntegers.First;
            GameEntity_Squad entity = World_AIW2.Instance.GetEntityByID_Squad( squadID );
            if ( entity == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "Could ship of which to cancel hack!", ChatType.ShowLocallyOnly, null );
                return;
            }
            HackingType hackType = entity.ActiveHack;
            if ( hackType == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "Could not find hack on entity to cancel!", ChatType.ShowLocallyOnly, null );
                return;
            }
            HackingEvent hackEvent = entity.ActiveHackEvent;
            if ( hackEvent == null )
            {
                World_AIW2.Instance.QueueChatMessageOrCommand( "Could not find hack event on entity to cancel!", ChatType.ShowLocallyOnly, null );
                return;
            }

            string addedText = string.Empty;
            if ( hackType.RelatedStringIsAShipType && hackEvent.RelatedStringOrNull != null && hackEvent.RelatedStringOrNull.Length > 0 )
            {
                GameEntityTypeData relatedTypeData = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( entity.ActiveHackEvent.RelatedStringOrNull );
                addedText = " (" + (relatedTypeData == null ? "null" : relatedTypeData.DisplayName) + ")";
            }
            if ( ArcenNetworkAuthority.GetIsHostMode() )
                World_AIW2.Instance.QueueChatMessageOrCommand( entity.ActiveHack.DisplayName + addedText + " Hack canceled!", ChatType.LogToCentralChat, null ); //this can null reference if ActiveHack = null during the DoOnCancel
            GameEntity_Squad target = World_AIW2.Instance.GetEntityByID_Squad( entity.ActiveHack_Target );
            Planet planet = World_AIW2.Instance.GetPlanetByIndex( entity.ActiveHack_Planet );
            hackType.Implementation.DoOnCancel_CalledFromMainSimOnly( target, planet, entity,
                hackType, hackEvent, Engine_AIW2.Instance.MainThreadContext_ClientOrHost.GetHostOnlyContext() );
            entity.ActiveHack = null;
            entity.ActiveHack_Target = 0;
            entity.ActiveHack_DurationThusFar = 0;
            hackEvent.HackEndTime = World_AIW2.Instance.GameSecond;
            Faction entityFacOrNull = entity.GetFactionOrNull_Safe();
            if ( entityFacOrNull != null )
                entityFacOrNull.HackingHistory.Add( hackEvent );
            entity.ActiveHackEvent = null;
        }
    }
}
