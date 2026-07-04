using System;
using Arcen.Universal;
using Arcen.AIW2.Core;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public struct MetalFlowForDisplay
    {
        public MetalFlowPurpose For;
        public EntityMetalFlowEntry Data;

        public MetalFlowForDisplay( MetalFlowPurpose purpose, EntityMetalFlowEntry data )
        {
            this.For = purpose;
            this.Data = data;
        }
    }
    
    public class EntityTextWriter : IConcurrentPoolable<EntityTextWriter>, IDisposable
    {
        #region Pooling
        private static ReferenceTracker RefTracker;
        private EntityTextWriter()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "EntityTextWriter" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<EntityTextWriter> Pool = new ConcurrentPool<EntityTextWriter>( "EntityTextWriter", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new EntityTextWriter(); } );

        [DoNotClear]
        private bool isInPool = false;
        
        public void ReturnToPool()
        {
            if (Buffer != null)
                Buffer.UserObj = _prev;
            
            Pool.ReturnToPool( this );
        }
        
        void IConcurrentPoolable<EntityTextWriter>.SetInPoolStatus( bool IsInPool )
        {
            this.isInPool = IsInPool;
        }
        bool IConcurrentPoolable<EntityTextWriter>.GetInPoolStatus()
        {
            return this.isInPool;
        }

        void IConcurrentPoolable<EntityTextWriter>.DoEarlyCleanupWhenGoingBackIntoPool()
        {
            this.SetToDefaults();
        }

        void IConcurrentPoolable<EntityTextWriter>.DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        private static ArcenTypeAnalyzer<EntityTextWriter> typeAnalyzer;
        private void SetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<EntityTextWriter>( new EntityTextWriter() );
            typeAnalyzer.ApplyDefaults( this );
        }
        
        void IDisposable.Dispose()
        {
            this.ReturnToPool();
        }
        
        #endregion
        
        public EntityText.Config Config;
        public ArcenCharacterBufferBase Buffer;
        public GameEntity_Squad Squad;
        
        public TextTerm Open_Term;
        public TermUse Open_Term_Usage;
        
        // maybe add a Queue<styles> here?
        
        private object _prev;
        
        public static EntityTextWriter Get(ArcenCharacterBufferBase buffer)
        {
            var writer = Pool.GetFromPoolOrCreate();   
            writer.Config = EntityText.Setup;
            writer.Buffer = buffer;
            writer._prev = buffer.UserObj;
            
            buffer.UserObj = writer;
            
            return writer;
        }

        public bool Write( 
            ArcenCharacterBufferBase buffer,
            GameEntity_Squad relatedSquadOrNull,
            FleetMembership MembershipBase,
            GameEntityTypeData TypeDataOrNull,
            Fleet FleetToUseOrNull,
            string AltTextColorIfUsed,
            string AltTextInPlaceOfFleetAndOwnerOrBlank,
            int OptionalCountToShow,
            Faction ForFactionOrNull,
            byte OptionalForMarkLevel,
            FromSidebarType IsFromSidebarType,
            ShipExtraDetailFlags DetailFlags,
            float PositionScaleMultiplier,
            bool IsBeingDrawnInPopupWindowRatherThanTooltip )
        {
            this.Buffer = buffer;
            buffer.UserObj = this;
            
            int debugstage = 0;
            EntitySystemCollection systems = null;
            EntitySystemCollection sys_mods = null;
            EntitySystemCollection stat_mods = null;
            EntityAttributeCollection attributes = null;
            try
            {
                #region Config

                debugstage = 1;
                var config = EntityText.Setup;

                debugstage = 2;
                // Chris: Now that we have ultrawide displays, this does not seem to be needed.
                if ( PositionScaleMultiplier != 1f )
                    PositionScaleMultiplier = 1f; 
                var cpi = new CharacterPosInfo( (float) Math.Round( 100 * PositionScaleMultiplier ) );

                //IsBeingDrawnInPopupWindowRatherThanTooltip = false;

                debugstage = 3;
                
                config.ExtraFlags = DetailFlags;

                debugstage = 4;
                
                if ((config.ExtraFlags & ShipExtraDetailFlags.HighestDetail) > 0)
                    config.Detail = TooltipDetail.Full;

                var detailLevel = config.Detail;
                var isShowingStrengthsAndWeaknesses = config.ShowingCounters;
                var useIcons = config.UseIcons;
                var useText = config.UseText;

                var showEntityId = config.ShowEntityId;
                var showDebugInfo = config.ShowDebugInfo;

                debugstage = 5;

                FleetMembership relatedMembershipOrNull =
                    relatedSquadOrNull == null ? MembershipBase : relatedSquadOrNull.FleetMembership;

                GameEntityTypeData relatedEntityTypeData = relatedSquadOrNull == null
                    ? (MembershipBase == null ? TypeDataOrNull : MembershipBase.TypeData)
                    : relatedSquadOrNull.TypeData;
                
                if ( relatedEntityTypeData == null )
                {
                    buffer.Add( "null relatedEntityData" );
                    return false;
                }

                if ( relatedEntityTypeData.Category != GameEntityCategory.Ship )
                {
                    buffer.Add( "relatedEntityData not a ship" );
                    return false;
                }

                if ( relatedSquadOrNull != null &&
                     ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                {
                    relatedSquadOrNull.FlagForRequestedForcedFullSyncToAllClients_FromAnyClient();
                }

                debugstage = 10;

                config.From = IsFromSidebarType;

                Window_InGameHoverEntityInfo.Mode panelMode;
                {
                    if ( DetailFlags.HasFlag( ShipExtraDetailFlags.BuildInfo ) )
                        panelMode = Window_InGameHoverEntityInfo.Mode.Build;
                    else
                    {
                        switch ( IsFromSidebarType )
                        {
                            default:
                            {
                                panelMode = Window_InGameHoverEntityInfo.Mode.SingleUnit;
                                break;
                            }
                            case FromSidebarType.Sidebar_MultipleUnits:
                            case FromSidebarType.NonSidebar_MultipleUnits:
                            case FromSidebarType.SelectionWindow_MultipleUnits:
                            {
                                panelMode = Window_InGameHoverEntityInfo.Mode.UnitGroupOnSidebar;
                                break;
                            }
                        }
                    }
                }
                config.PanelMode = panelMode;
                config.ForMultipleShips = panelMode == Window_InGameHoverEntityInfo.Mode.UnitGroupOnSidebar;
                config.OptShipCount = OptionalCountToShow;
                if ( OptionalCountToShow > 1 )
                    config.ForMultipleShips = true;

                debugstage = 13;
                bool modeHasSpecificUnitOrFaction = relatedSquadOrNull != null;
                bool isFromHackSidebarPopoutWindow =
                    DetailFlags.HasFlag( ShipExtraDetailFlags.WindowHackChoicesSidebarPopoutForData );
                HackingType hackBeingDoneAgainstUs = isFromHackSidebarPopoutWindow
                    ? Window_HackChoicesSidebarPopout.Instance.HackTypeToChooseFor
                    : null;
                GameEntity_Squad hackerBeingUsedAgainstUs = isFromHackSidebarPopoutWindow
                    ? Window_HackChoicesSidebarPopout.Instance.HackerToUseOrNullIfNoneHere
                    : null;
                config.ActiveHackAgainstUs = hackBeingDoneAgainstUs;
                config.ActiveHackerAgainstUs = hackerBeingUsedAgainstUs;

                debugstage = 15;
                if ( hackBeingDoneAgainstUs != null && hackBeingDoneAgainstUs.IsAGrantShipStyleHack )
                {
                    if ( relatedEntityTypeData.IsBlockedFromDSSGrantToCommandStations )
                        buffer.Add( "Will not be granted to command stations; only battlestations and citadels.\n" );
                    if ( relatedEntityTypeData.IsBlockedFromDSSGrantToBattlestationsAndCitadels )
                        buffer.Add( "Will not be granted to battlestations or citadels; only command stations.\n" );
                }

                debugstage = 17;
                Faction owningFactionOrNull = ForFactionOrNull;
                if (owningFactionOrNull == null)
                {
                    if (relatedSquadOrNull != null)
                        owningFactionOrNull = relatedSquadOrNull.PlanetFaction?.Faction;
                    else if ( (config.ExtraFlags & ShipExtraDetailFlags.Encyclopedia) == 0 )
                        owningFactionOrNull = config.LocalPlayerFaction;
                }
                debugstage = 18;
                Planet thisPlanetOrNull = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                debugstage = 19;
                PlanetFaction localPlayerPlanetFactionOrNull =
                    (thisPlanetOrNull == null || config.LocalPlayerFaction == null)
                        ? null
                        : thisPlanetOrNull.GetPlanetFactionForFaction( config.LocalPlayerFaction );
                
                debugstage = 20;
                PlayerTypeData localPlayerTypeOrNull = config.LocalPlayerFaction?.PlayerTypeDataOrNull_ModeratelyExpensive;
                PlayerTypeData owningPlayerTypeOrNull = owningFactionOrNull?.PlayerTypeDataOrNull_ModeratelyExpensive;

                bool showMetalCost = true;
                bool showMetalOther = true;
                bool showAnyFuel = true;
                bool showEnergyProduction = true;
                bool showEnergyConsumption = true;

                if ( owningPlayerTypeOrNull != null )
                {
                    if ( !owningPlayerTypeOrNull.UsesMetal )
                        showMetalOther = false;

                    if ( !owningPlayerTypeOrNull.UsesEnergyAndFuel )
                        showEnergyProduction = false;
                }

                if ( config.LocalPlayerFaction == owningFactionOrNull )
                {
                    if ( localPlayerTypeOrNull != null && !localPlayerTypeOrNull.UsesMetal )
                        showMetalCost = false;
                }

                if ( localPlayerTypeOrNull != null && 
                     !localPlayerTypeOrNull.UsesEnergyAndFuel )
                {
                    showAnyFuel = false;
                }

                config.ShowMetalCost = showMetalCost;
                config.ShowMetalOther = showMetalOther;
                config.ShowAnyFuel = showAnyFuel;
                config.ShowEnergyProduction = showEnergyProduction;
                config.ShowEnergyConsumption = showEnergyConsumption;

                Fleet relatedMemFleetOrNull = null;
                if ( relatedMembershipOrNull != null )
                    relatedMemFleetOrNull = relatedMembershipOrNull.Fleet;
                if ( FleetToUseOrNull != null )
                    relatedMemFleetOrNull = FleetToUseOrNull;

                config.OptFleet = relatedMemFleetOrNull;
                config.OptMembership = relatedMembershipOrNull;
                
                config.OptNameForOwner = AltTextInPlaceOfFleetAndOwnerOrBlank;
                config.OptColorForOwner = AltTextColorIfUsed;

                this.Config = config;
                #endregion

                #region More Setup
                Fleet fedFromCityFleetOrNull = relatedMembershipOrNull?.GetFedHereFromCityFleetOrNull();

                //buffer.Add( "\nowningFaction: " ).Add( owningFaction.GetDisplayName() )
                //    .Add( " ForSFactionOrNull: " ).Add( ForFactionOrNull == null ? "[null]" : ForFactionOrNull.GetDisplayName() )
                //    .Add( " Fleet: " ).Add( relatedMemFleetOrNull == null ? "[null]" : relatedMemFleetOrNull.GetName() )
                //    .Add( "\n" );

                debugstage = 9;
                //if ( relatedMembership == null )
                //{
                //    buffer.Add( "NULL relatedMembership" ).Add( "\n" );
                //    buffer.Add( "relatedEntity: " ).Add( relatedEntity == null ? "null" : relatedrelatedSquadOrNull.TypeData.InternalName ).Add( "\n" );
                //    buffer.Add( "localPlayerFaction: " ).Add( localPlayerFaction == null ? "null" : localPlayerFaction.GetDisplayName() ).Add( "\n" );
                //    buffer.Add( "owningFaction: " ).Add( owningFaction == null ? "null" : owningFaction.GetDisplayName() ).Add( "\n" );
                //    buffer.Add( "thisPlanet: " ).Add( thisPlanet == null ? "null" : thisPlanet.Name ).Add( "\n" );
                //    buffer.Add( "localPlayerPlanetFaction: " ).Add( localPlayerPlanetFaction == null ? "null" : localPlayerGetFactionDisplayNameSafe() ).Add( "\n" );
                //    return false;
                //}

                PlanetFaction relatedSquadPlanetFactionOrNull = null;
                Faction relatedSquadFactionOrNull = null;

                byte effectiveMarkLevel = OptionalForMarkLevel;
                if ( effectiveMarkLevel <= 0 )
                {
                    if ( relatedMembershipOrNull != null )
                        effectiveMarkLevel = relatedMembershipOrNull.EffectiveMark;
                    else
                        effectiveMarkLevel = relatedEntityTypeData.StartingMarkLevel.Ordinal;
                }
                if ( effectiveMarkLevel > relatedEntityTypeData.MaxMarkLevel )
                    effectiveMarkLevel = relatedEntityTypeData.MaxMarkLevel;
                
                debugstage = 10;
                var forMark = relatedEntityTypeData.MarkStatsFor( effectiveMarkLevel );
                
                //This little section is required for AI ships to show their correct values.  Same with other non-player ships.
                if ( relatedSquadOrNull != null )
                {
                    forMark = relatedSquadOrNull.DataForMark;
                    effectiveMarkLevel = forMark.MarkLevel.Ordinal;
                    relatedSquadPlanetFactionOrNull = relatedSquadOrNull.PlanetFaction;
                    relatedSquadFactionOrNull = relatedSquadPlanetFactionOrNull == null ? null : relatedSquadPlanetFactionOrNull.Faction;
                }
                
                debugstage = 11;

                //========Start Chris adapter for SirLimbo
                //GameEntity_Squad entity = relatedSquadOrNull;
                GameEntityTypeData entityType = relatedEntityTypeData;
                Balance_MarkLevel markLevel = Balance_MarkLevelTable.Instance.RowsByOrdinal[effectiveMarkLevel];
                FleetMembership fleetMembershipOrNull = relatedMembershipOrNull;

                debugstage = 102;
                //=========fakeSquadForBuildMode is set up as an adapter from Chris to make life easier for all of us, but specifically for SirLimbo's stuff.
                //if ( fakeSquadForBuildMode == null )
                    //fakeSquadForBuildMode = GameEntity_Squad.CreateNew_ForFakeInUITooltipsOnlyDoNotUseThisAnywhereElse();

                debugstage = 103;
                if ( relatedSquadOrNull == null )
                {
                    //this keeps entity from being null and lets it work properly, but is probably just for build menus and wave contents tooltips, etc
                    debugstage = 105;
                    relatedSquadOrNull = EntityText.GetFakeEntity( entityType, effectiveMarkLevel, owningFactionOrNull, thisPlanetOrNull );
                    //fakeSquadForBuildMode.SetInfoForFakeInUITooltipsOnlyDoNotUseThisAnywhereElse( entityType, effectiveMarkLevel, thisPlanetOrNull, owningFactionOrNull );
                }

                //these are conveniences
                PlanetFaction entityPFaction = relatedSquadOrNull.PlanetFaction;
                Faction entityFaction = entityPFaction?.Faction;
                bool entityCanBeClaimed;
                {
                    var facType = relatedSquadOrNull.GetFactionTypeSafe();
                    entityCanBeClaimed = relatedSquadOrNull.GetMetalToClaimRemaining() > 0 && 
                                         (relatedSquadOrNull.IsFakeEntity || facType == FactionType.NaturalObject || facType == FactionType.Player);
                }
                
                var isUnderConstruction = relatedSquadOrNull.SelfBuildingMetalRemaining > FInt.Zero;
                var isCenterpiece = relatedSquadOrNull.FleetMembership?.Fleet?.Centerpiece.GetSquad() == relatedSquadOrNull;
                
                int entityTimeOnPlanet = relatedSquadOrNull.GetSecondsSinceEnteringThisPlanet();
                FInt speedMultiplierWhileHacking = ExternalConstants.Instance.SpeedMultiplierWhileHacking;
                
                this.Squad = relatedSquadOrNull;
                
                #endregion

                debugstage = 106;
                buffer.Open(TextStyle.TooltipBase);

                #region FirstRow: Entity Name, Faction, etc
                {
                    buffer.AddVarReplace( TextVarMap.Tooltip_Header_Ship_Format, AppendVar_Ship_Header );
                }
                #endregion

                config.CpiBaseOffset = 50 * PositionScaleMultiplier;

                debugstage = 107;
                if (relatedEntityTypeData.HideStatBlock)
                {
                    buffer.NewLine();
                    buffer.ToPos(cpi);
                }

                #region Stat Block
                
                //var statsText = new EntityStatBlock(relatedSquadOrNull, config);
                //statsText.Write(buffer, ref cpi);
                
                #endregion
                
                if ( isShowingStrengthsAndWeaknesses && 
                     owningFactionOrNull != null && 
                     config.LocalPlayerFaction != null )
                {
                    if ( owningFactionOrNull.GetIsHostileTowards( config.LocalPlayerFaction ) )
                        WriteWeakAgainst( buffer, relatedEntityTypeData, forMark );
                    else
                        WriteStrongAgainst( buffer, relatedEntityTypeData, thisPlanetOrNull );

                    return true;
                }

                buffer.BeginStatement(TextStyle.Status_Block);

                #region Status Lines
                
                #region Behavior/Claim, Orders
                {
                    //only do this for real entities
                    if ( !relatedSquadOrNull.IsFakeEntity ) 
                    {
                        buffer.Open(TextStyle.Status_Line);
                        
                        if ( entityCanBeClaimed && 
                             owningFactionOrNull != null )
                        {
                            #region Claiming
                            
                            buffer.Add( "Claim", TextStyle.Behavior_Label ).Add(": ");
                            
                            if ( relatedSquadOrNull.IsInHoldFireMode )
                            {
                                buffer.Add( "Paused", "cccccc" );
                            } 
                            else
                            {
                                Faction forFaction;
                                if ( owningFactionOrNull.Type != FactionType.Player )
                                {
                                    forFaction = config.LocalPlayerFaction;
                                } 
                                else
                                {
                                    forFaction = owningFactionOrNull;
                                }
                                
                                if ( forFaction != null )
                                {
                                    FactionType planetController = relatedSquadOrNull.Planet == null ? FactionType.NaturalObject : relatedSquadOrNull.Planet.GetControllingFactionType();
                                    if ( planetController == FactionType.NaturalObject )
                                    {
                                        buffer.Add( "Requires Planet Control", "bbbbbb" );
                                    } 
                                    else 
                                    if ( planetController != FactionType.Player )
                                    {
                                        buffer.Add( "Blocked by enemy", "aa3333" );
                                    } 
                                    else 
                                    if ( forFaction.SecondsSinceBrownout > 0 )
                                    {
                                        buffer.Add( "Stopped (Brownout)", "ff2222" );
                                    } 
                                    else 
                                    if ( forFaction.NetEnergy < entityType.EnergyUsage )
                                    {
                                        buffer.Add( "Stopped (Power Lacking)", "cc2222" );
                                    } 
                                    else 
                                    if ( World_AIW2.Instance.IsFuelEnabled && 
                                         relatedEntityTypeData.FuelUseType == ResourceType.FuelArgon && 
                                         forFaction.NetFuelArgon < entityType.FuelUse )
                                    {
                                        buffer.Add( "Stopped (Argon Lacking)", "cc2222" );
                                    } 
                                    else 
                                    if ( World_AIW2.Instance.IsFuelEnabled && 
                                         relatedEntityTypeData.FuelUseType == ResourceType.FuelRadon && 
                                         forFaction.FuelRadonConsumption < entityType.FuelUse )
                                    {
                                        buffer.Add( "Stopped (Radon Lacking)", "cc2222" );
                                    } 
                                    else 
                                    if ( World_AIW2.Instance.IsFuelEnabled && 
                                         relatedEntityTypeData.FuelUseType == ResourceType.FuelXenon && 
                                         forFaction.FuelXenonConsumption < entityType.FuelUse )
                                    {
                                        buffer.Add( "Stopped (Xenon Lacking)", "cc2222" );
                                    } 
                                    else 
                                    if ( relatedSquadOrNull.GetCurrentHullPoints() == 1 )
                                    {
                                        buffer.Add( "Ready", "55dd55" );
                                    }
                                    else
                                    {
                                        var hullPerc = relatedSquadOrNull.GetHullPercent().ToFInt();
                                        buffer.AddPercentageInColor( hullPerc, true, false ).Add( " done" );
                                        if ( detailLevel == TooltipDetail.Full )
                                        {
                                            buffer.Add( " (" );
                                            if ( useText )
                                                buffer.Add( "Metal: " );
                                            
                                            var metalToClaimRem = relatedSquadOrNull.GetMetalToClaimRemaining();
                                            buffer.WrapMetalTruncated( metalToClaimRem * hullPerc / 100, useIcons, false ).Add( " / " ).WrapMetalTruncated( relatedSquadOrNull.DataForMark.MetalCostToClaim, false, false ).Add( ")" );
                                        }
                                    }
                                }
                            }

                            #endregion
                        }
                        else 
                        if ( relatedSquadOrNull.SelfBuildingMetalRemaining > FInt.Zero )
                        {
                            #region Construction
                            
                            var metalCost = relatedSquadOrNull.GetMetalCost();
                            
                            buffer.Add( "Constructing", TextStyle.Behavior_Label ).Add(": ");
                            
                            if ( relatedSquadOrNull.IsInHoldFireMode )
                            {
                                buffer.Add( "Paused", "cccccc" );
                            } 
                            else
                            {
                                buffer.AddPercentageInColor( (metalCost - relatedSquadOrNull.SelfBuildingMetalRemaining).ToPercent( metalCost ), true, false );
                            }
                            /*
                            if ( detailLevel == TooltipDetail.Full )
                            {
                                buffer.Add( " (" );
                                
                                if ( useText )
                                    buffer.Add( "Metal: " );
                                
                                buffer
                                    .WrapMetalTruncated( metalCost - relatedSquadOrNull.SelfBuildingMetalRemaining, useIcons, false )
                                    .Add( " / " )
                                    .WrapMetalTruncated( metalCost, false, false )
                                    .Add( ")" );
                            }
                            */
                            
                            #endregion
                        }
                        else
                        {
                            #region Behavior
                            
                            // immobile entities don't really have different behaviors, they just sit there
                            // ... unless they are player owned and its useful to know if they standing down, anyway just show it

                            //     though thats already conveyed in several other ways (the button state, the selection circle color...)
                            
                            buffer.Add( "Behavior", TextStyle.Behavior_Label).Add(": ");
                            
                            var beh = relatedSquadOrNull.Orders.Behavior;
                            if ( relatedSquadOrNull.IsInHoldFireMode )
                            {
                                buffer.Add( "Stand Down", "444499" );
                            } 
                            else 
                            if (beh == EntityBehaviorType.Attacker_Full)
                            {
                                if ( forMark.Computed_BaseLongestWeaponRange <= 0 )
                                {
                                    if ( entityFaction != null && 
                                         entityFaction.Type == FactionType.Player )
                                    {
                                        buffer.Add( "Pursuit Mode", Window_InGameSelectionInfo.color_Pursuit );
                                    }
                                    else
                                    {
                                        buffer.Add( "Roaming", "eeee88" );
                                    }
                                }
                                else
                                {
                                    if ( entityFaction != null && 
                                         entityFaction.Type == FactionType.Player )
                                    {
                                        buffer.Add( "Pursuit Mode", Window_InGameSelectionInfo.color_Pursuit );
                                    }
                                    else
                                    {
                                        buffer.Add( "Attacking All", "ee8888" );
                                    }
                                }
                            }
                            else
                            if (beh == EntityBehaviorType.Attacker_PursueOnlyInRange)
                            {
                                buffer.Add( "Attack Move", Window_InGameSelectionInfo.color_AttackMove );
                                        
                            }
                            else
                            if (beh == EntityBehaviorType.Guard_FleetShip)
                            {
                                buffer.Add( "Protect Ally", "88ee88" );
                            }
                            else
                            if (beh == EntityBehaviorType.Guard_Guardian_Patrolling)
                            {
                                buffer.Add( "Patrol Sector", "88ee88" );
                            }
                            else
                            if (beh == EntityBehaviorType.Guard_Guardian_Anchored)
                            {
                                buffer.Add( "Defend Position", "88ee88" );
                            }
                            else
                            if (beh == EntityBehaviorType.Stationary ||
                                beh == EntityBehaviorType.None )
                            {
                               if ( relatedSquadOrNull.TypeData.IsMobile == false )
                                {
                                    buffer.Add( "Stationary", "777777" );
                                }
                                else
                                {
                                    buffer.Add( "Defending", "88ee88" );
                                }
                            }

                            #endregion
                            
                            //cpi.CurrentPos(pos="12em")
                            //buffer.ToPos( cpi.AddStep().AddStep() );
                            //buffer.Add("<pos=\"12em\")

                            #region Orders
                            {
                                EntityOrder order = relatedSquadOrNull.Orders.GetQueuedOrderAtIndex_OrNull( 0 );
                                if ( order.TypeData != null )
                                {
                                    buffer.Add( "Orders", TextStyle.Orders_Label ).Add(": ");
                                    
                                    int queuedOrderCount = relatedSquadOrNull.Orders.GetQueuedOrderCount();
                                    this.WriteEntityOrder( relatedSquadOrNull, order, buffer );

                                    int count = GameSettings.Current.GetIntBySetting( "MaxDisplayedOrdersInTooltips" );
                                    int consecutivePathing = 0;
                                    short lastCyclePlanetIndex = -1;
                                    if ( order.TypeData.Type == EntityOrderType.Wormhole )
                                    {
                                        consecutivePathing = 1;
                                    }
                                    int i = 1;
                                    for ( ; i < count || consecutivePathing > 0; i++ )
                                    {
                                        order = relatedSquadOrNull.Orders.GetQueuedOrderAtIndex_OrNull( i );
                                        if ( order.TypeData == null )
                                        {
                                            if ( consecutivePathing > 1 )
                                            {
                                                buffer.Add( " (" ).Add( consecutivePathing ).Add( " hops)" );
                                            }
                                            break;
                                        }

                                        if ( order.TypeData.Type == EntityOrderType.Wormhole )
                                        {
                                            if ( lastCyclePlanetIndex == order.RelatedPlanetIndex )//apparently this can happen when moving to a planet, and setting a new move command to a different planet...
                                            {
                                                count++;
                                                continue;
                                            }
                                            if ( consecutivePathing == 0 )
                                            {
                                                buffer.Add( ", " );
                                                this.WriteEntityOrder( relatedSquadOrNull, order, buffer );
                                            }
                                            consecutivePathing++;
                                            count++;
                                            lastCyclePlanetIndex = order.RelatedPlanetIndex;
                                            continue;//skip ahead, no need to show every planet individually!
                                        }
                                        {
                                            if ( consecutivePathing > 1 )
                                            {
                                                buffer.Add( " (" ).Add( consecutivePathing ).Add( " hops)" );
                                            }
                                            consecutivePathing = 0;
                                        }
                                        if ( i >= count )
                                        {
                                            break;
                                        }
                                        buffer.Add( ", " );
                                        this.WriteEntityOrder( relatedSquadOrNull, order, buffer );
                                    }
                                    if ( queuedOrderCount - i > 0 )
                                    {
                                        buffer.Add( " + " ).Add( queuedOrderCount - i ).Add( " more" );
                                    }
                                }
                            }
                            #endregion
                        }
                        
                        buffer.Close(TextStyle.Status_Line);
                    }
                }
                #endregion
                
                debugstage = 15001;

                #region FithRow: Buffs
                
                if ( detailLevel >= TooltipDetail.Medium && 
                     !Squad.IsFakeEntity )
                {
                    buffer.Open(TextStyle.Status_Line);
                    
                    debugstage = 15001801;
                    bool wroteBuffStart = false;

                    debugstage = 15001821;

                    #region Hull
                    bool wroteHullBuffStart = false;
                    if ( forMark.BaseHullPoints > 0 )
                    {
                        debugstage = 15001831;
                        
                        if ( fleetMembershipOrNull != null && 
                             fleetMembershipOrNull.Fleet.HullMultiplier > FInt.One && 
                             !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            this.WriteHullBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteHullBuffStart );
                            buffer.Add( "Fleet Amp: +" ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.HullMultiplier - 1) * 100 );
                        }
                    }
                    #endregion

                    debugstage = 15001841;

                    #region Shield
                    if ( forMark.BaseShieldPoints > 0 )
                    {
                        debugstage = 15001851;

                        bool wroteShieldBuffStart = false;
                        if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.ShieldsMultiplier > FInt.One && !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            this.WriteShieldBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteShieldBuffStart );
                            buffer.Add( "Fleet Amp: +" ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.ShieldsMultiplier - 1) * 100 );
                        }
                    }
                    #endregion

                    debugstage = 15001861;

                    #region Damage
                    if ( forMark.Computed_BaseLongestWeaponRange > 0 || forMark.AttritionDamagePreFleetModifiers > 0 )
                    {
                        debugstage = 15001871;

                        bool wroteDamageBuffStart = false;
                        if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.AttackPowerMultiplier > FInt.One && !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            this.WriteDamageBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteDamageBuffStart );
                            buffer.Add( "Fleet Amp: +" ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.AttackPowerMultiplier - 1) * 100 );
                        }

                        if ( forMark.Computed_BaseLongestWeaponRange > 0 && relatedSquadOrNull.PlanetFaction != null && relatedSquadOrNull.PlanetFaction.CurrentAttackMultiplier > FInt.One )
                        {
                            this.WriteDamageBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteDamageBuffStart );
                            buffer.Add( "Faction Planet Amp: +" ).AddPercentRoundedDynamically( (relatedSquadOrNull.PlanetFaction.CurrentAttackMultiplier - 1) * 100 );
                        }

                        if ( relatedSquadOrNull.TypeData.AmountAddedToDamagePerShipOfThisTypeOnPlanet > 0 && relatedSquadOrNull.CalculatedAddedDamage > 0 )
                        {
                            this.WriteDamageBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteDamageBuffStart );
                            buffer.Add( "Network: +" ).AddNumberTruncated( relatedSquadOrNull.CalculatedAddedDamage );
                        }

                        if ( relatedSquadOrNull.NumberOfWeaponPoints > 0 )
                        {
                            this.WriteDamageBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteDamageBuffStart );
                            buffer.Add( "Weapon Points: " ).AddNumberMoreReadable( relatedSquadOrNull.NumberOfWeaponPoints );
                        }
                    }
                    #endregion

                    debugstage = 15001881;

                    #region Speed
                    if ( entityType.IsMobile )
                    {
                        debugstage = 15001891;

                        bool wroteSpeedBuffStart = false;
                        if ( relatedSquadOrNull.SpeedLimitFromGroupMove > 0 )
                        {
                            if ( relatedSquadOrNull.SpeedLimitFromGroupMove > forMark.Speed )
                            {
                                this.WriteSpeedBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteSpeedBuffStart );
                                if ( owningFactionOrNull != null && owningFactionOrNull.Type == FactionType.Player )
                                {
                                    buffer.Add( "Group Move: +" );
                                } else
                                {
                                    buffer.Add( "Speed Group: +" );
                                }
                                buffer.AddNumberMoreReadable( relatedSquadOrNull.SpeedLimitFromGroupMove - forMark.Speed );
                            }
                        } else if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.OverridingMinSpeed > forMark.Speed &&
                            !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet && !isCenterpiece )
                        {
                            this.WriteSpeedBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "Fleet Amp: +" ).AddNumberMoreReadable( fleetMembershipOrNull.Fleet.OverridingMinSpeed - forMark.Speed );
                        }

                        if ( relatedSquadOrNull.PlanetFaction != null && (relatedSquadOrNull.PlanetFaction.CurrentSpeedMultiplier > FInt.One || relatedSquadOrNull.PlanetFaction.CurrentSpeedFlatBonus > 0) )
                        {
                            this.WriteSpeedBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "Faction Planet Amp: " );
                            if ( relatedSquadOrNull.PlanetFaction.CurrentSpeedMultiplier > FInt.One )
                            {
                                buffer.Add( "+ " ).AddPercentRoundedDynamically( (relatedSquadOrNull.PlanetFaction.CurrentSpeedMultiplier - 1) * 100 );
                                if ( relatedSquadOrNull.PlanetFaction.CurrentSpeedFlatBonus > 0 )
                                    buffer.Add( ", " );
                            }
                            if ( relatedSquadOrNull.PlanetFaction.CurrentSpeedFlatBonus > 0 )
                                buffer.Add( "+ " ).Add( relatedSquadOrNull.PlanetFaction.CurrentSpeedFlatBonus );
                        }

                        if ( relatedSquadOrNull.Planet != null && relatedSquadOrNull.Planet.UnitSpeedupPercentage > 0 )
                        {
                            this.WriteSpeedBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "Planet Amp: " ).AddPercentRoundedDynamically( relatedSquadOrNull.Planet.UnitSpeedupPercentage, 100 );
                        }

                        if ( entityType.SpeedMultiplierFirst5SecondsOnPlanet > FInt.One && entityTimeOnPlanet <= 5 )
                        {
                            this.WriteSpeedBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "Rapid Deployment: +" ).AddPercentRoundedDynamically( (entityType.SpeedMultiplierFirst5SecondsOnPlanet - 1) * 100 );
                            if ( detailLevel == TooltipDetail.Full )
                            {
                                buffer.Add( " (" ).Add( 6 - entityTimeOnPlanet ).Add( "s)" );
                            }
                        }

                        if ( !isCenterpiece && !relatedEntityTypeData.IsDrone )
                        {
                            EntityOrder order = relatedSquadOrNull.Orders.GetQueuedOrderAtIndex_OrNull( 0 );
                            if ( order.TypeData != null && order.TypeData.Type == EntityOrderType.GetIntoTransport )
                            {
                                this.WriteSpeedBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteSpeedBuffStart );
                                buffer.Add( "Loading: +" ).AddPercentRoundedDynamically( 2, 1 );
                            }
                        }

                        if ( isCenterpiece && relatedSquadOrNull.ActiveHack != null && speedMultiplierWhileHacking > FInt.One )
                        {
                            this.WriteSpeedBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "Hacking: " ).AddPercentRoundedDynamically( (speedMultiplierWhileHacking - 1) * 100 );
                        }

                        if ( owningFactionOrNull != null && relatedSquadOrNull.Planet != null && relatedSquadOrNull.Planet.IsFimbulwintered && owningFactionOrNull.BenefitsFromFimbulwinter )
                        {
                            this.WriteSpeedBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "Fimbulwinter: +" ).AddPercentRoundedDynamically( ExternalConstants.Instance.FimbulwinterSpeedupPercent, 100 );
                        }
                    }

                    #endregion

                    debugstage = 15001901;

                    #region Range
                    if ( relatedSquadOrNull.TypeData.AmountAddedToRangePerShipOfThisTypeOnPlanet > 0 )
                    {
                        debugstage = 15001911;
                        bool wroteRangeBuffStart = false;
                        if ( fleetMembershipOrNull != null && relatedSquadOrNull.CalculatedAddedRange > 0 )
                        {
                            this.WriteRangeBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteRangeBuffStart );
                            buffer.Add( "Network: +" ).AddNumberTruncated( relatedSquadOrNull.CalculatedAddedRange );
                        }
                    }
                    #endregion

                    debugstage = 15001921;

                    #region Cloak
                    if ( relatedSquadOrNull.GetMightPossiblyBeCloaked() )
                    {
                        debugstage = 15001931;

                        bool wroteCloakBuffStart = false;
                        int cloak = relatedSquadOrNull.GetCurrentCloakingPoints();
                        int cloakMax = relatedSquadOrNull.GetMaxCloakingPoints();

                        this.WriteCloakBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteCloakBuffStart );
                        buffer.AddNumberTruncated( cloak ).Add( " / " ).AddNumberTruncated( cloakMax );
                    }
                    #endregion
                    
                    buffer.Close(TextStyle.Status_Line);
                }
                #endregion

                debugstage = 16001;

                #region SixthRow: Debuffs
                if ( detailLevel >= TooltipDetail.Medium && 
                     !Squad.IsFakeEntity )
                {
                    buffer.Open(TextStyle.Status_Line);
                    
                    bool wroteDebuffStart = false;
                    
                    #region Acid
                    if ( relatedSquadOrNull.IncomingDamageAmplifiedDuration_Max15 > 0 && (relatedSquadOrNull.IncomingDamageAmplifiedByFlat > 0 || relatedSquadOrNull.IncomingDamageAmplifiedByMult > FInt.One) )
                    {
                        this.WriteDebuffsStartIfNeeded( buffer, ref wroteDebuffStart );
                        buffer.Add( "Acid: (" ).Add( relatedSquadOrNull.IncomingDamageAmplifiedDuration_Max15 ).Add( "s, +" );
                        if ( relatedSquadOrNull.IncomingDamageAmplifiedByFlat > 0 )
                        {
                            buffer.AddNumberMoreReadable( relatedSquadOrNull.IncomingDamageAmplifiedByFlat );
                        }
                        if ( relatedSquadOrNull.IncomingDamageAmplifiedByMult > FInt.One )
                        {
                            if ( relatedSquadOrNull.IncomingDamageAmplifiedByFlat > 0 )
                            {
                                buffer.Add( " / +" );
                            }
                            buffer.AddPercentRoundedDynamically( (relatedSquadOrNull.IncomingDamageAmplifiedByMult * 100) - 100 );
                        }
                        buffer.Add( ")" );
                    }
                    #endregion

                    #region Corrosion
                    if ( relatedSquadOrNull.CorrosionDamageToBeAppliedToMe > 0 )
                    {
                        this.WriteDebuffsStartIfNeeded( buffer, ref wroteDebuffStart );
                        buffer.Add( "Corrosion: " ).Add( relatedSquadOrNull.CorrosionDamageToBeAppliedToMe );
                    }
                    #endregion

                    #region Paralysis
                    if ( relatedSquadOrNull.CurrentParalysisSeconds > 0 )
                    {
                        this.WriteDebuffsStartIfNeeded( buffer, ref wroteDebuffStart );
                        buffer.Add( "Paralysis: (" ).Add( relatedSquadOrNull.CurrentParalysisSeconds ).Add( "s)" );
                    }
                    #endregion

                    #region ReloadSlow
                    if ( relatedSquadOrNull.CurrentWeaponAddedReloadSeconds > 0 )
                    {
                        this.WriteDebuffsStartIfNeeded( buffer, ref wroteDebuffStart );
                        buffer.Add( "Reload Slow: (" ).Add( relatedSquadOrNull.CurrentWeaponAddedReloadSeconds ).Add( "s)" );
                    }
                    #endregion
                    
                    #region Hull
                    bool wroteHullDebuffStart = false;
                    if ( forMark.BaseHullPoints > 0 )
                    {
                        if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.HullMultiplier < FInt.One && fleetMembershipOrNull.Fleet.HullMultiplier != FInt.Zero &&
                            !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            this.WriteHullDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteHullDebuffStart );
                            buffer.Add( "Fleet Damp: " ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.HullMultiplier * 100) - 100 );
                        }
                    }
                    #endregion

                    #region Shield
                    if ( forMark.BaseShieldPoints > 0 )
                    {
                        bool wroteShieldDebuffStart = false;
                        if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.ShieldsMultiplier < FInt.One && fleetMembershipOrNull.Fleet.ShieldsMultiplier != FInt.Zero &&
                            !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            this.WriteShieldDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteShieldDebuffStart );
                            buffer.Add( "Fleet Damp: " ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.ShieldsMultiplier * 100) - 100 );
                        }
                    }
                    #endregion

                    #region Damage
                    if ( forMark.Computed_BaseLongestWeaponRange > 0 || forMark.AttritionDamagePreFleetModifiers > 0 )
                    {
                        bool wroteDamageDebuffStart = false;
                        if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.AttackPowerMultiplier < FInt.One && fleetMembershipOrNull.Fleet.AttackPowerMultiplier != FInt.Zero &&
                            !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            this.WriteDamageDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteDamageDebuffStart );
                            buffer.Add( "Fleet Damp: " ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.AttackPowerMultiplier * 100) - 100 );
                        }

                        if ( forMark.Computed_BaseLongestWeaponRange > 0 && relatedSquadOrNull.PlanetFaction != null && relatedSquadOrNull.PlanetFaction.CurrentAttackMultiplier < FInt.One && relatedSquadOrNull.PlanetFaction.CurrentAttackMultiplier != FInt.Zero &&
                            !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            this.WriteDamageDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteDamageDebuffStart );
                            buffer.Add( "Faction Planet Damp: " ).AddPercentRoundedDynamically( (relatedSquadOrNull.PlanetFaction.CurrentAttackMultiplier * 100) - 100 );
                        }

                        if ( !relatedSquadOrNull.IsFakeEntity && 
                             forMark.Computed_BaseLongestWeaponRange > 0 &&
                            (relatedSquadOrNull.IsUnderDamageReducingShield.Display || 
                             relatedSquadOrNull.GetIsSelfEmittingProtectingShield_ReduceDamage()) )
                        {
                            EntitySystemTypeData systemType;
                            bool foundAny = false;
                            bool toEveryWeapon = true;

                            List<string> SystemsAffected = ArcenStrings.GetTemporaryStringList( "Window_InGameHoverEntityInfo-SystemsAffected", 10f );
                            if ( SystemsAffected == null ) //blocked for teardown/shutdown; bail
                                return false;

                            for ( int i = 0; i < entityType.SystemTypes.Count; i++ )
                            {
                                systemType = entityType.SystemTypes[i];
                                if ( systemType.DamageModifierWhileUnderForcefield > FInt.One )//early out ASAP
                                {
                                    toEveryWeapon = false;
                                    continue;
                                }
                                if ( systemType.Category != EntitySystemCategory.Weapon )
                                {
                                    continue;
                                }
                                if ( systemType.MaxMarkLevelToFunction < markLevel.Ordinal || 
                                     systemType.MinMarkLevelToFunction > markLevel.Ordinal )
                                {
                                    continue;
                                }
                                if ( systemType.IsModule && 
                                     !systemType.IsModuleOn( relatedMembershipOrNull ) )
                                {
                                    continue;
                                }
                                if ( systemType.MustBeThisStateOfMatterToBeEnabled != null && 
                                     systemType.MustBeThisStateOfMatterToBeEnabled != relatedSquadOrNull.CurrentStateOfMatter )
                                {
                                    continue;
                                }
                                if ( systemType.ShotTypeData.Category != GameEntityCategory.Shot )
                                {
                                    continue;
                                }
                                if ( systemType.SystemIsHiddenForUI )
                                {
                                    continue;
                                }
                                if ( detailLevel == TooltipDetail.Full )
                                {
                                    SystemsAffected.Add( systemType.DisplayName );
                                }
                                foundAny = true;
                            }
                            
                            if ( foundAny )
                            {
                                this.WriteDamageDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteDamageDebuffStart );
                                buffer.Add( "Under Forcefield: -50%" );
                                if ( !toEveryWeapon )
                                {
                                    buffer.Add( " to " );
                                    if ( detailLevel == TooltipDetail.Full )
                                    {
                                        bool isFirst = true;
                                        for ( int i = 0; i < SystemsAffected.Count; i++ )
                                        {
                                            if ( !isFirst )
                                            {
                                                buffer.Add( ", " );
                                                isFirst = false;
                                            }
                                            buffer.Add( SystemsAffected[i] );
                                        }
                                    } 
                                    else
                                    {
                                        buffer.Add( "some weapons" );
                                    }
                                }
                            }

                            ArcenStrings.ReleaseTemporaryStringList( SystemsAffected );
                        }
                    }
                    #endregion

                    #region Speed
                    if ( entityType.IsMobile )
                    {
                        bool wroteSpeedDebuffStart = false;

                        #region EngineSlow
                        if ( relatedSquadOrNull.CurrentEngineStunSeconds > 0 )
                        {
                            this.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                            int index = relatedSquadOrNull.CurrentEngineStunSeconds;
                            if ( index >= ExternalConstants.Instance.EngineStunMultipliersByStunSeconds.Count )
                            {
                                index = ExternalConstants.Instance.EngineStunMultipliersByStunSeconds.Count - 1;
                            }
                            FInt slowFactor = ExternalConstants.Instance.EngineStunMultipliersByStunSeconds[index];
                            if ( slowFactor <= FInt.Zero )
                            {
                                buffer.Add( "Engine Stun: (" ).Add( relatedSquadOrNull.CurrentEngineStunSeconds ).Add( "s, -100%)" );
                            } else
                            {
                                buffer.Add( "Engine Slow: (" ).Add( relatedSquadOrNull.CurrentEngineStunSeconds ).Add( "s, " ).AddPercentRoundedDynamically( (slowFactor * 100) - 100 ).Add( ")" );
                            }
                        }
                        #endregion

                        if ( relatedSquadOrNull.CurrentCountOfTractorsPullingOnThis > 0 )
                        {
                            this.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                            buffer.Add( "Caught in Tractor Beam: -100%" );
                        } else
                        {
                            if ( relatedSquadOrNull.SpeedLimitFromGroupMove > 0 && relatedSquadOrNull.SpeedLimitFromGroupMove < forMark.Speed )
                            {
                                this.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                if ( owningFactionOrNull != null && owningFactionOrNull.Type == FactionType.Player )
                                {
                                    buffer.Add( "Group Move: " );
                                } else
                                {
                                    buffer.Add( "Speed Group: " );
                                }
                                buffer.AddNumberMoreReadable( relatedSquadOrNull.SpeedLimitFromGroupMove - forMark.Speed );
                            }

                            if ( relatedSquadOrNull.PlanetFaction != null && (relatedSquadOrNull.PlanetFaction.CurrentSpeedMultiplier < FInt.One || relatedSquadOrNull.PlanetFaction.CurrentSpeedFlatBonus < 0) )
                            {
                                this.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                buffer.Add( "Faction Planet Damp: " );
                                if ( relatedSquadOrNull.PlanetFaction.CurrentSpeedMultiplier < FInt.One )
                                {
                                    buffer.Add( " " ).AddPercentRoundedDynamically( (relatedSquadOrNull.PlanetFaction.CurrentSpeedMultiplier * 100) - 100 );
                                    if ( relatedSquadOrNull.PlanetFaction.CurrentSpeedFlatBonus > 0 )
                                        buffer.Add( ", " );
                                }
                                if ( relatedSquadOrNull.PlanetFaction.CurrentSpeedFlatBonus > 0 )
                                    buffer.Add( " " ).Add( relatedSquadOrNull.PlanetFaction.CurrentSpeedFlatBonus );
                            }

                            if ( relatedSquadOrNull.Planet != null && relatedSquadOrNull.Planet.UnitSlowPercentage > 0 )
                            {
                                this.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                buffer.Add( "Planet Damp: " ).AddPercentRoundedDynamically( -relatedSquadOrNull.Planet.UnitSlowPercentage, 100 );
                            }

                            if ( entityType.SpeedMultiplierFirst5SecondsOnPlanet < FInt.One && entityType.SpeedMultiplierFirst5SecondsOnPlanet != FInt.Zero && entityTimeOnPlanet <= 5 )
                            {
                                this.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                buffer.Add( "Arrival: " ).AddPercentRoundedDynamically( (entityType.SpeedMultiplierFirst5SecondsOnPlanet * 100) - 100 );
                                if ( detailLevel == TooltipDetail.Full )
                                {
                                    buffer.Add( " (for " ).Add( 6 - entityTimeOnPlanet ).Add( "s)" );
                                }
                            }

                            if ( isCenterpiece && relatedSquadOrNull.ActiveHack != null && speedMultiplierWhileHacking < FInt.One )
                            {
                                this.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                buffer.Add( "Hacking: " ).AddPercentRoundedDynamically( (speedMultiplierWhileHacking - 1) * 100 );
                            }

                            if ( owningFactionOrNull != null && relatedSquadOrNull.Planet != null && relatedSquadOrNull.Planet.IsFimbulwintered && !owningFactionOrNull.BenefitsFromFimbulwinter )
                            {
                                this.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                buffer.Add( "Fimbulwinter: " ).AddPercentRoundedDynamically( -ExternalConstants.Instance.FimbulwinterSlowdownPercent, 100 );
                            }

                            FInt grav = relatedSquadOrNull.CurrentGravitySpeedMultiplier.Display;
                            if ( grav < FInt.One && grav != FInt.Zero )
                            {
                                this.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                buffer.Add( "Gravity: " ).AddPercentRoundedDynamically( grav * 100 );
                            }
                        }
                    }

                    #endregion

                    #region Cloak
                    if ( relatedSquadOrNull.IsFakeEntity == false && 
                         relatedSquadOrNull.GetMightPossiblyBeCloaked() )
                    {
                        bool wroteCloakBuffStart = false;
                        int cloak = relatedSquadOrNull.GetCurrentCloakingPoints();
                        int cloakMax = relatedSquadOrNull.GetMaxCloakingPoints();
                        if ( cloak == 0 && cloakMax > 0 )
                        {
                            this.WriteDecloakDebuffsStartIfNeeded( buffer, useIcons, true, ref wroteDebuffStart, ref wroteCloakBuffStart );
                            if ( relatedSquadOrNull.ActiveHack != null )
                            {
                                buffer.Add( ", Disabled by Hack" );
                            } else if ( relatedSquadOrNull.GetIsCrippled() )
                            {
                                buffer.Add( ", Crippled" );
                            } else if ( detailLevel == TooltipDetail.Full )
                            {
                                buffer.Add( " (Regen: " ).Add( 1 + ExternalConstants.Instance.SecondsToWaitBeforeRecloaking - (World_AIW2.Instance.GameSecond - relatedSquadOrNull.GameSecondOfLastCloakingPointLoss) )
                                    .Add( "s)" );
                            }
                        } 
                        else if ( World_AIW2.Instance.GameSecond - relatedSquadOrNull.GameSecondOfLastCloakingPointLoss < 2 )
                        {
                            this.WriteDecloakDebuffsStartIfNeeded( buffer, useIcons, false, ref wroteDebuffStart, ref wroteCloakBuffStart );
                        }
                    }
                    #endregion

                    #region Range Reduction (Turrets)
                    if ( relatedEntityTypeData.IsTurret && 
                         AIWar2GalaxySettingQuickAccess.UseHarshTurretRangesOnNonHumanPlanets &&
                         owningFactionOrNull?.Type == FactionType.Player && 
                         thisPlanetOrNull?.GetControllingFactionType() != FactionType.Player )
                    {
                        this.WriteDebuffsStartIfNeeded( buffer, ref wroteDebuffStart );
                        buffer.WrapSpeed("Range: ", false, false).Add("limited on hostile planet");
                    }
                    #endregion
                    
                    buffer.Close(TextStyle.Status_Line);
                }
                #endregion

                buffer.EndStatement(TextStyle.Status_Block);
                
                #endregion
                
                debugstage = 17001;

                #region SeventhRow: Build Stats
                if ( panelMode == Window_InGameHoverEntityInfo.Mode.Build || 
                     entityCanBeClaimed ||
                     (Config.ExtraFlags & ShipExtraDetailFlags.Encyclopedia) > 0)
                {
                    buffer.Pad();
                    
                    (new EntityBuildStatText(this.Squad, 1, this.Config)).Write(buffer);
                    
                    int forCount = 0;
                    {
                        if ( OptionalCountToShow > 1 )
                        {
                            forCount = OptionalCountToShow;
                        } 
                        else 
                        if ( relatedSquadOrNull.ExtraStackedSquadsInThis > 0 )
                        {
                            forCount = relatedSquadOrNull.ExtraStackedSquadsInThis + 1;
                        } 
                        else 
                        if ( fleetMembershipOrNull?.EffectiveSquadCap > 1 )
                        {
                            forCount = fleetMembershipOrNull.EffectiveSquadCap;
                        }
                    }

                    if ( forCount > 1 )
                        (new EntityBuildStatText(this.Squad, forCount, this.Config)).Write(buffer);
                    
                    buffer.Pad();
                }
                #endregion

                debugstage = 18001;

                #region EightRow: Ship Class
                
                (new ShipClassText(relatedSquadOrNull, config)).Write(buffer);

                #endregion

                #region Debug Block
                if (!relatedSquadOrNull.IsFakeEntity)
                {
                    buffer.BeginStatement(TextStyle.Newline_NoLabel);
                    
                    debugstage = 19001;
                    if ( !string.IsNullOrEmpty(relatedSquadOrNull.DebugText) )
                    {
                        buffer.Add( relatedSquadOrNull.DebugText );
                    }

                    #region ids
                    debugstage = 17;
                    if ( Config.ShowEntityId || Config.ShowFleetId || Config.ShowLocationCoords || Config.ShowDebugInfo )
                    {
                        buffer.Open(TextStyle.Newline_NoLabel);
                        
                        //if ( Config.ShowEntityId)
                            buffer.Add( "ID-" ).Add( Squad.PrimaryKeyID, TextStyle.Brighter );

                        //if ( Config.ShowFleetId )
                            buffer.Add(" | ", TextStyle.Color_Gray).Add( "FLEET-" ).Add( Squad.GetFleetID_Safe(), TextStyle.Brighter );

                        debugstage = 305391;
                        if (relatedMemFleetOrNull != null)
                            buffer.Add(" | ", TextStyle.Color_Gray).Add( Extensions.ToString(relatedMemFleetOrNull.Category), TextStyle.Brighter );

                        //if ( Config.ShowLocationCoords )
                        {
                            buffer
                                .Add(" | ", TextStyle.Color_Gray)
                                .Open(TextStyle.Brighter)
                                .Add( "{" )
                                .Add( Squad.WorldLocation.X - 400000 )
                                .Add( "," ).Add( Squad.WorldLocation.Y - 400000 )
                                .Add("}")
                                .Close(TextStyle.Brighter);
                        }
                        
                        debugstage = 305394;
                        
                        /*
                        debugstage = 305392;
                        bool isThisSelectedVisually = Squad.GetIsSelected();
                        
                        debugstage = 305393;
                        bool isSelectedInListOfSquads = World_AIW2.Instance.LocalPlayerSelectedSquads_CanGiveOrders.Contains( Squad ) ||
                            World_AIW2.Instance.LocalPlayerSelectedSquads_CannotGiveOrders.Contains( Squad );
                        

                        if ( relatedMemFleetOrNull != null )
                        {
                            bool isSelectedByFleet = relatedMemFleetOrNull.IsConsideredSelected_NonSim;
                            debugstage = 305395;
                            buffer.Add(" | ", TextStyle.Color_Gray).Add( "Sel: " ).Add( !isThisSelectedVisually ? "Vis_No" : "Vis_Yes" ).Add( ", " )
                                .Add( !isSelectedInListOfSquads ? "SList_No" : "SList_Yes" ).Add( ", " )
                                .Add( !isSelectedByFleet ? "Fl_No" : "Fl_Yes" ).NewLine();
                        }
                        */
                        
                        debugstage = 305396;
                        ArcenRejectionReason disabledReason = Squad.ComputeDisabledReason();
                        if ( disabledReason != ArcenRejectionReason.Unknown )
                            buffer.Add(" | ", TextStyle.Color_Gray).Add(Extensions.ToString(disabledReason), TextStyle.Color_Warn);
                        
                        //if (Squad.Orders.BehaviorRelatedFactionIndex != -1)
                        //{
                        //    var fac = World_AIW2.Instance.GetFactionByIndex(Squad.Orders.BehaviorRelatedFactionIndex);
                        //    buffer.Add(" | ", TextStyle.Color_Gray).Add("Against ").Add(fac.OrNull(), fac.FactionCenterColor.TeamColorBrighter);
                        //}
                        
                        buffer.Close(TextStyle.Newline_NoLabel);

                        debugstage = 3075;
                        if ( Squad.GetFactionTypeSafe() == FactionType.AI &&
                             Squad.TypeData.IsMobile && 
                             Squad.TypeData.IsCombatant )
                        {
                            debugstage = 3076;
                            
                            // this seems sus to me
                            //if ( Squad.GuardedUnit.GetSquad() == null )
                            {
                                debugstage = 3077;

                                var etype = Squad.TypeData;
                                var orders = Squad.Orders;
                                var againstFaction = World_AIW2.Instance.GetFactionByIndex( orders.BehaviorRelatedFactionIndex );
                                var againstPlanet = World_AIW2.Instance.GetPlanetByIndex( Squad.WaitingAgainstPlanetIndex );
                                var aidif = Squad.GetFactionOrNull_Safe()?.TryGetAISentinelsCoreData()?.SentinelInfo?.AIDifficulty;
                                
                                int secondsIHaveBeenThreat = -1;
                                if (Squad.BecameThreatfleetAtGameSecond > -1 && 
                                    Squad.BecameThreatfleetAtGameSecond < World_AIW2.Instance.GameSecond)
                                {
                                    secondsIHaveBeenThreat = World_AIW2.Instance.GameSecond - Squad.BecameThreatfleetAtGameSecond;
                                }
                                
                                int secondsIHaveBeenWaiting = -1;
                                if (Squad.StartedWaitingAtGameSecond > -1 && 
                                    Squad.StartedWaitingAtGameSecond < World_AIW2.Instance.GameSecond)
                                {
                                    secondsIHaveBeenWaiting = World_AIW2.Instance.GameSecond - Squad.StartedWaitingAtGameSecond;
                                }
                                
                                if (secondsIHaveBeenThreat > -1 && orders != null)
                                {
                                    buffer.Open(TextStyle.Newline_NoLabel);
                                    buffer.Add( "This ship has been Threat against " );
                                    if (againstFaction != null)
                                        buffer.AddFactionNameInItsColor(againstFaction, true);
                                    else
                                        buffer.Add( "Players", TextStyle.Brighter);
                                    buffer.Add(" for ").AddMinutesAndSeconds(secondsIHaveBeenThreat).Add( "." );
                                    buffer.Close(TextStyle.Newline_NoLabel);
                                }
                                
                                if ( againstPlanet != null && secondsIHaveBeenWaiting > -1 )
                                {
                                    buffer.Open(TextStyle.Newline_NoLabel)
                                          .Add( "This ship has been waiting to attack " )
                                          .AddPlanetNameFormated(againstPlanet,true)
                                          .Add(" for ").AddMinutesAndSeconds(secondsIHaveBeenWaiting)
                                          .Close(TextStyle.Newline_NoLabel);
                                }
                                
                                if ( secondsIHaveBeenThreat > -1 )
                                {
                                    if ( etype.NotEligibleToJoinHunterFleet || etype.IsDrone )
                                    {
                                        buffer.Open(TextStyle.Newline_NoLabel).Add( "This ship will not join Hunter, because of its type." ).Close(TextStyle.Newline_NoLabel);
                                    }
                                    else
                                    if ( againstFaction != null && againstFaction.Type != FactionType.Player )
                                    {
                                        buffer.Open(TextStyle.Newline_NoLabel).Add( "This ship will not join Hunter, because not targeting players." ).Close(TextStyle.Newline_NoLabel);
                                    }
                                    else
                                    if ( aidif == null )
                                    {
                                        buffer.Open(TextStyle.Newline_NoLabel).Add( "This ship will not join Hunter, because not connected to sentinels." ).Close(TextStyle.Newline_NoLabel);
                                    }
                                    else
                                    {
                                        int hunterFleetWaitThreshold = aidif.SecondsThreatWaitsBeforeJoiningHunterFleet;
                                        int hunterFleetExistsThreshold = aidif.SecondsThreatExistsAsThreatBeforeJoiningHunterFleet;

                                        int remSecTillMaxWait = -1;
                                        if (secondsIHaveBeenWaiting > -1 && secondsIHaveBeenWaiting < hunterFleetWaitThreshold)
                                            remSecTillMaxWait = hunterFleetWaitThreshold - secondsIHaveBeenWaiting;
                                        
                                        int remSecTillMaxAlive = -1;
                                        if (secondsIHaveBeenThreat > -1 && secondsIHaveBeenThreat < hunterFleetExistsThreshold)
                                            remSecTillMaxAlive = hunterFleetExistsThreshold - secondsIHaveBeenThreat;
                                        
                                        if (remSecTillMaxWait > -1)
                                            buffer.Open(TextStyle.Newline_NoLabel).Add( "This ship will join Hunter if still waiting to attack, in ").AddMinutesAndSeconds(remSecTillMaxWait).Add(".").Close(TextStyle.Newline_NoLabel);
                                        if (remSecTillMaxAlive > -1)
                                            buffer.Open(TextStyle.Newline_NoLabel).Add( "This ship will join Hunter if alive and threat, in ").AddMinutesAndSeconds(remSecTillMaxAlive).Add(".").Close(TextStyle.Newline_NoLabel);
                                    }
                                }
                            }
                            
                            debugstage = 3079;
                            var exoTargetPlanet = World_AIW2.Instance.GetPlanetByIndex( Squad.ExoGalacticAttackPlanetIdx );
                            var exoTargetSquad = Squad.ExoGalacticAttackTarget.GetSquad();
                            if ( exoTargetPlanet != null || exoTargetSquad != null)
                            {
                                buffer.Open(TextStyle.Newline_NoLabel);
                                buffer.Add( "Part of an Exostrike against " );
                                if (exoTargetSquad == null)
                                    buffer.AddPlanetNameFormated(exoTargetPlanet, true);
                                else
                                {
                                    buffer.AddColor(exoTargetSquad.GetDisplayName(true), exoTargetSquad.GetFactionCenterColorHexBrighter_Safe())
                                          .Add(" #").Add(exoTargetSquad.PrimaryKeyID);
                                
                                    if (exoTargetSquad.Planet != null)
                                    {
                                        buffer.Add(" on ");
                                        buffer.AddPlanetNameFormated(exoTargetSquad.Planet, true);
                                    }
                                }
                                buffer.Add(".");
                                buffer.Close(TextStyle.Newline_NoLabel);
                            }
                            
                            /*
                            GameEntity_Squad guarded = Squad.GuardedUnit.GetSquad();
                            if ( guarded != null )
                            {
                                buffer.Open(TextStyle.Newline_NoLabel);
                                    
                                buffer.Add( "Guarding : " ).Add( guarded );
                                if ( Squad.GuardOrPatrolOffsetPoints.Count == 0 )
                                    buffer.Add( " No Guarding Offsets." );
                                else
                                    buffer.Add( " The first of " ).Add( Squad.GuardOrPatrolOffsetPoints.Count ).Add( " offsets is " ).Add( Squad.GuardOrPatrolOffsetPoints[0] );
                            }
                            */
                        }

                        #region LOD-Debug
                        debugstage = 8400;
                        if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.MainGameView &&
                             GameSettings.Current.GetBoolBySetting( "Debug_ShowLODInfoInShipTooltips" ) )
                        {
                            Planet planet = Squad.Planet;
                            if ( Squad.InstancedRenderer == null )
                            {
                                buffer.Add( "No InstancedRenderer on this ship", TextStyle.WarnText );
                            } 
                            else 
                            if ( planet == null )
                            {
                                buffer.Add( "No planet related to this ship?", TextStyle.WarnText );
                            } 
                            else
                            {
                                buffer.Open(TextStyle.Newline_NoLabel);
                                
                                float distanceFromCamera;
                                int currentLOD = Squad.InstancedRenderer.GetCurrentLODAndLastDistanceFromCamera( out distanceFromCamera );

                                float lodDivisor = planet.GravWellSize.GeneralMultiplier;
                                bool hasLODDivisor = false;
                                if ( Math.Round( lodDivisor, 1 ) != 1f )
                                {
                                    hasLODDivisor = true;
                                    buffer.NewLineIfNeeded().Add( "<pos=40><color=#c79462>Current Visual Scale Is Offset:</color> " ).Add( 1 / lodDivisor ).Add( "x<pos=350><color=#c79462>Do NOT set LOD values in xml from this planet!</color>" );
                                } 
                                else
                                    lodDivisor = 1f;

                                buffer.NewLineIfNeeded().Add( "<pos=40><color=#99aa99>Current LOD:</color> " ).Add( currentLOD ).Add( "<pos=350><color=#99aa99>Distance From Camera:</color> " ).AddFixedDecimalThousands( distanceFromCamera, 1 );
                                if ( Squad.TypeData.LODDistancesFromVis == null )
                                {
                                    buffer.NewLineIfNeeded().Add( "<pos=40>LODDistancesFromVis Not Set For Some Reason!" );
                                } 
                                else 
                                if ( Squad.TypeData.LODMeshesFromVis == null )
                                {
                                    buffer.NewLineIfNeeded().Add( "<pos=40>LODMeshesFromVis Not Set For Some Reason!" );
                                } 
                                else
                                {
                                    if ( Squad.TypeData.LODDistancesFromVis.Length != Squad.TypeData.LODMeshesFromVis.Length )
                                        buffer.NewLineIfNeeded().Add( "<pos=40>LODDistancesFromVis.Length != LODMeshesFromVis.Length!" );

                                    for ( int i = 0; i < Squad.TypeData.LODDistancesFromVis.Length; i++ )
                                    {
                                        buffer.NewLineIfNeeded().Add( "<pos=40><color=#aaaaaa>LOD" ).Add( i ).Add( " Lasts Until Dist:</color> " );
                                        if ( i < Squad.TypeData.LODDistanceOverrides.Count ) //use the override if present
                                        {
                                            buffer.AddNumberMoreReadable( ((Squad.TypeData.LODDistanceOverrides[i] * Squad.TypeData.LODDistanceMultiplier) / lodDivisor) );
                                            buffer.Add( "   <color=#aaaaaa>(Overriding: " ).AddNumberMoreReadable( (Squad.TypeData.LODDistancesFromVis[i] / lodDivisor) ).Add( ")</color>" );
                                            if ( hasLODDivisor )
                                            {
                                                buffer.Add( "   <color=#c79462>(Orig: " )
                                                    .AddNumberMoreReadable( (Squad.TypeData.LODDistanceOverrides[i] * Squad.TypeData.LODDistanceMultiplier) )
                                                    .Add( " / " )
                                                    .AddNumberMoreReadable( Squad.TypeData.LODDistancesFromVis[i] )
                                                    .Add( ")</color>" );
                                            }
                                        } 
                                        else
                                        {
                                            buffer.AddNumberMoreReadable( ((Squad.TypeData.LODDistancesFromVis[i] * Squad.TypeData.LODDistanceMultiplier) / lodDivisor) );
                                            if ( hasLODDivisor )
                                            {
                                                buffer.Add( "   <color=#c79462>(Orig: " )
                                                    .AddNumberMoreReadable( (Squad.TypeData.LODDistancesFromVis[i] * Squad.TypeData.LODDistanceMultiplier) )
                                                    .Add( ")</color>" );
                                            }
                                        }
                                        buffer.Add( "<pos=350><color=#aaaaaa>Mesh Vertex Count:</color> " ).AddNumberMoreReadable( Squad.TypeData.LODMeshesFromVis[i].vertexCount );
                                    }
                                }
                            }
                            
                            buffer.Close(TextStyle.Newline_NoLabel);
                        }
                        #endregion
                    }
                    #endregion
                    
                    buffer.EndStatement(TextStyle.Newline_NoLabel);
                }
                #endregion
                
                /*
                debugStage = 50;
                if ( panelMode == Mode.Build )
                {
                    buffer.NewLine();
                    cpi.ResetPos();
                    if ( OptionalCountToShow > 0 )
                    {
                        buffer.Add( "<b>Count</b>:  " );
                        buffer.Add( " " ).AddNumberMoreReadable( OptionalCountToShow ).Add( "     " );
                    }

                    buffer.Add( "     " );
                    ArcenExternalUIUtilities.AddSizeAndVOssetToText( buffer, ArcenExternalUIUtilities.Strength, 12, 2 );
                    AddSingleValueStrengthOnly( buffer, relatedMembershipOrNull != null && relatedMemFleetOrNull.IsPlayerStyleFleet ? relatedMembershipOrNull.GetStrengthPerSquad_PlayerFleetsOnly() : forMark.StrengthPerSquad_CalculatedWithNullFleetMembership );
                    buffer.EndColor();

                    if ( relatedMembershipOrNull != null && localPlayerFaction.NetEnergy < relatedMembershipOrNull.GetEnergyUsage() && relatedMembershipOrNull.GetEnergyUsage() > 0 )
                        buffer.Add( "" ).StartColor( QuickColors.Danger ).Add( "Ship requires " ).Add( relatedMembershipOrNull.GetEnergyUsage() )
                            .Add( " energy to function, but you only have " ).Add( localPlayerFaction.NetEnergy ).Add( " available." ).EndColor();
                }*/
                
                debugstage = 80;
                (new EntityDescText(relatedSquadOrNull, config)).Write(buffer);

                #region Systems
                
                debugstage = 97;
                systems = EntitySystemCollection.GetTemporary("Tooltip.systems", 5.0f);
                if ( systems == null ) //blocked for teardown/shutdown; bail
                    return false;
                //sys_mods = EntitySystemCollection.GetTemporary("Tooltip.sys_mods", 5.0f);
                stat_mods = EntitySystemCollection.GetTemporary("Tooltip.stat_mods", 5.0f);
                if ( stat_mods == null ) //blocked for teardown/shutdown; bail
                    return false;

                EntityText.EnumerateSystems(relatedSquadOrNull, systems, sys_mods, stat_mods);
                
                #region All Non-ModuleStatAdjust Systems
                
                foreach ( var sys in systems)
                {
                    this.WriteSystem(sys);
                }

                #endregion

                #region ModuleStatAdjust Systems
                
                if (stat_mods.Count > 0)
                {
                    buffer.BeginStatement(TextStyle.System_Line);
                    
                    buffer.AddModuleTag(true).Add( "Stat-Boost", TextStyle.Attr_Label );
                    buffer.Add(": bonus ");
                            
                    int counter = 0;
                    foreach ( var sys in stat_mods )
                    {
                        if (counter > 0)
                            buffer.Add(", ");
                        //LOG.Msg("[{0}] {1}", counter, sys.TypeData.ModuleStatAdjuster);
                        this.WriteSystem(sys);
                        
                        counter++;
                    }
                    
                    buffer.Add(".");
                        
                    buffer.EndStatement(TextStyle.System_Line);
                }
                
                #endregion
                
                #endregion

                #region Attributes
                
                attributes = EntityAttributeCollection.GetTemporary("Tooltip.attributes", 5.0f);
                if ( attributes == null ) //blocked for teardown/shutdown; bail
                    return false;
                EntityText.EnumerateAttributes(relatedSquadOrNull, attributes, config);
                
                //buffer.Open(TextStyle.Attr_Line);
                foreach (var attr in attributes)
                {
                    //buffer.Pad(TextStyle.Attr_Pad);
                    attr.Write(this);
                    //buffer.Pad(TextStyle.Attr_Pad);
                }
                //buffer.Close(TextStyle.Attr_Line);
                
                #endregion
                
                buffer.Close(TextStyle.TooltipBase);
                
                #region DLC/Mod
                if (!config.ExtraFlags.HasFlag(ShipExtraDetailFlags.ShownInsideAnother))
                {
                    if ( config.Detail < TooltipDetail.Full )
                        buffer.AddDlcMod( relatedSquadOrNull.TypeData, TextStyle.DlcMod_Full );
                    else
                        buffer.AddDlcMod( relatedSquadOrNull.TypeData, "This unit was added by", TextStyle.DlcMod_Full );
                }
                #endregion

                debugstage = 9010;

                #region MP-Client Latency
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client && 
                     !relatedSquadOrNull.IsFakeEntity )
                {
                    if ( relatedSquadOrNull.Network_ClientOnly_SimCyclesSinceLastHostUpdate > 50 )
                    {
                        
                        buffer.Open(TextStyle.Newline_NoLabel)
                            .Add( "<pos=40><color=#ff6767>It has been </color> " )
                            .AddFixedDecimalThousands( (relatedSquadOrNull.Network_ClientOnly_SimCyclesSinceLastHostUpdate / 10f), 1 )
                            .Add( " seconds since this unit has been checked by the host!</color>" ).Close(TextStyle.Newline_NoLabel);
                    } 
                    else
                    {
                        if ( config.Detail == TooltipDetail.Full )
                        {
                            buffer.Open(TextStyle.Newline_NoLabel)
                                .Add( "<pos=40><color=#aaaaaa></color> " )
                                .AddFixedDecimalThousands( (relatedSquadOrNull.Network_ClientOnly_SimCyclesSinceLastHostUpdate / 10f), 1 )
                                .Add( "s since unit was checked by host.</color>" ).Close(TextStyle.Newline_NoLabel);
                        }
                    }
                }
                #endregion
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    LOG.Err("Error in EntityTextWriter.Write at debugstage {0} from:\n{1}", debugstage, e);

                buffer.Add( "Exception during generation of tooltip!" );

                return false;
            }
            finally
            {
                if (systems != null)
                {
                    EntitySystemCollection.ReleaseTemporary(systems);
                }
                if (sys_mods != null)
                {
                    EntitySystemCollection.ReleaseTemporary(sys_mods);
                }
                if (stat_mods != null)
                {
                    EntitySystemCollection.ReleaseTemporary(stat_mods);
                }
                if (attributes != null)
                {
                    EntityAttributeCollection.ReleaseTemporary(attributes);
                }
                
                if (relatedSquadOrNull?.IsFakeEntity??false)
                    EntityText.ReleaseFakeEntity(relatedSquadOrNull);
                //if (baseStyle != null)
                //{
                //    buffer.Close(baseStyle);
                //}
            }

            return true;
        }

        private void AppendVar_Ship_Header( string key, TextStyle style, ArcenCharacterBufferBase buffer, object args )
        {
            int debugstage = 0;
            try
            {
                if (Squad.GetMaxShieldPoints() == 0)
                {
                    if (key.Equals("shields"))
                    {
                        key = "hull";
                    }
                    else 
                    if (key.Equals("hull"))
                    {
                        return;
                    }
                }
                    
                debugstage = 100;
                if ( key.Equals("icon") )
                {
                    debugstage = 110;
                    if (buffer == null)
                        throw new ArgumentNullException("buffer");
                    if (style == null)
                        throw new ArgumentNullException("style");
                    
                    buffer.AddShipIconInline(this.Squad, style);
                    
                    return;
                }

                debugstage = 120;
                if (key.Equals("displayname"))
                {
                    debugstage = 130;

                    if (!this.Squad.IsFakeEntity &&
                        this.Squad.FleetMembership != null &&
                        this.Squad.TypeData.IsModular)
                    {
                        foreach (var sys in this.Squad.Systems)
                        {
                            if (sys.TypeData.IsModuleOn(this.Squad.FleetMembership) &&
                                !string.IsNullOrEmpty(sys.TypeData.ModularFullNamePrefix))
                            {
                                buffer.Add( sys.TypeData.ModularFullNamePrefix );
                            }
                        }
                    }
                    
                    buffer.Add(this.Squad.GetTypeDisplayNameSafe());
                    
                    return;
                }
                
                debugstage = 140;
                if (key.Equals("marklevel"))
                {
                    debugstage = 150;
                    var mark_level = this.Squad.DataForMark.MarkLevel;
                    if ( mark_level.Ordinal != 0 )
                    {
                        buffer.Add( " " ).AddMarkLevelFormated( mark_level );
                    }

                    return;
                }
                
                if (key.Equals("count"))
                {
                    if ( Config.ExtraFlags.HasFlag( ShipExtraDetailFlags.InGuardPost ) )
                        buffer.Add( "Loaded ", "ff622b" );
                    else if ( Config.ExtraFlags.HasFlag( ShipExtraDetailFlags.BeingTransported ) )
                        buffer.Add( "Loaded ", "59d2ff" );
                    else if ( Config.ExtraFlags.HasFlag( ShipExtraDetailFlags.LoadedDrone ) )
                        buffer.Add( "Loaded ", "59d2ff" );

                    if ( Config.OptShipCount > 1 )
                    {
                        buffer.AddColor( Config.OptShipCount, "dbef21" ).Add( "脳 " );
                    } 
                    else if ( Squad.ShipCount > 1 )
                    {
                        buffer.AddColor( Squad.ShipCount, "dbef21" ).Add( "脳 " );
                    }
                    
                    if ( Squad.SecondsSpentAsRemains >= 0 )
                    {
                        buffer.AddSize_Large().AddColor( "REMAINS ", "ff4b21" ).EndSize();
                    }

                    return;
                }
                
                if (key.Equals("owner"))
                {
                    if (Squad.TypeData.HideFactionName)
                        return;
                    
                    void AppendOwner(ArcenCharacterBufferBase buff)
                    {
                        debugstage = 170;

                        if ( !string.IsNullOrEmpty( Config.OptNameForOwner ) )
                        {
                            buff.StartColor( Config.OptColorForOwner ).Add( Config.OptNameForOwner ).EndColor();
                        } 
                        else 
                        if ( Squad.GetFactionOrNull_Safe() != null )
                        {
                            var fac = Squad.GetFactionOrNull_Safe();
                            
                            buff
                                .Add( " of " )
                                .StartColor( fac.FactionCenterColor.ColorHexBrighter );
                            
                            if ( fac.Type == FactionType.AI || 
                                 FactionUtilityMethods.Instance.IsACoreAISubFaction( fac ) )
                            {
                                var sentinelData = fac.TryGetAISentinelsCoreData()?.SentinelInfo;
                                if ( sentinelData != null )
                                {
                                    bool secretFactionDetails = AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "FactionDetailsAreSecret" ) && 
                                                                World.Instance.ConclusionType == CampaignConclusionType.NotConcluded;
                                    var showRandomAiType = GameSettings.Current.GetBoolBySetting( "ShowRandomAIType" );

                                    if (!secretFactionDetails)
                                    {
                                        if ( sentinelData.WasRandomAIType )
                                        {
                                            if ( showRandomAiType )
                                            {
                                                buff.Add( sentinelData.AIType.DisplayName ).Add( " " );
                                            } 
                                            else
                                            {
                                                buff.Add( "Random " );
                                            }
                                        } 
                                        else 
                                        if ( sentinelData.AdaptiveAIDifficulty != TypeDifficulty.Unset )
                                        {
                                            if ( showRandomAiType )
                                            {
                                                buff.Add( sentinelData.AIType.DisplayName ).Add( " " );
                                            } 
                                            else
                                            {
                                                buff.Add( "Adaptive " );
                                            }
                                        } 
                                        else
                                        {
                                            buff.Add( sentinelData.AIType.DisplayName ).Add( " " );
                                        }
                                    }
                                }
                            }
                            
                            debugstage = 16;
                            if ( !string.IsNullOrEmpty(Squad.TypeData.OverrideFactionName) )
                                buff.Add( Squad.TypeData.OverrideFactionName );
                            else
                                buff.Add( fac.GetDisplayNameInternal(true, false ) );

                            if ( Squad.ExoGalacticAttackPlanetIdx != -1 )
                                buff.Add( " (Exo-Strike)" );
                                    
                            buff.EndColor();
                            
                            if ( fac.Type == FactionType.Player )
                            {
                                var fleet = Squad.FleetMembership?.Fleet;
                                if ( fleet != null )
                                    buff.Add( ", " ).AddFactionColoredString( fleet.GetName(), fac );
                                /*
                                var fed = Squad.FleetMembership?.GetFedHereFromCityFleetOrNull();
                                if ( fed != null )
                                    buff.Add( " from " ).AddFactionColoredString( fed.GetName(), fac );
                                */
                            }
                        }
                    }

                    AppendOwner(buffer);
                    /*
                     var tmp = ArcenCharacterBuffer.GetFromPoolOrCreate("EntityTextWriter.TempForFirstLine");
                    AppendOwner(tmp);
                    tmp.RemoveAllTags();
                    var txt = tmp.ToString();
                    LOG.Msg("FirstLine='{0}' len={1}", txt, txt.Length);
                    int lim = ExternalConstants.Instance.GetCustomInt32_Slow("tooltip_character_limit");
                    string sz = ExternalConstants.Instance.GetCustomString_Slow("tooltip_limit_size");
                    if (txt.Length > lim)
                    {
                        buffer.StartSize(sz);
                        AppendOwner(buffer);
                        buffer.EndSize();
                    }
                    else
                    {
                        AppendOwner(buffer);
                    }
                    tmp.ReturnToPool();
                    */
                    return;
                }
                
                if (key.Equals("hull"))
                {
                    int cur = Squad.GetCurrentHullPoints();
                    int max = Squad.GetMaxHullPoints();
                    int perc = Squad.GetHullPercent();

                    if ( max == 0 )
                    {
                        //buffer.AddVarReplace( TextVarMap.Stat_NA_Format );
                    }
                    else
                    {
                        buffer.Open( TextTerm.Hull, TermUse.Icon ).Add(" ");
                        buffer.Add("<space=0.15em>");
                                                
                        //if ( Squad.IsFakeEntity ||
                        //     Squad.GetFactionTypeSafe() == FactionType.NaturalObject )
                        //{
                        //    buffer.AddNumber( max, null, TextStyle.Empty );
                        //}
                        //else
                        {
                            buffer.AddNumber( cur, null, TextStyle.Empty );

                            if ( ( Config.Detail > TooltipDetail.SuperShort || 
                                   Squad.GetFactionTypeSafe() == FactionType.NaturalObject ) &&
                                 cur != max )
                            {
                                buffer
                                    .Open(TextStyle.Fraction_Gray)
                                    .Add( "/" )
                                    .AddNumber( max, null, TextStyle.Empty )
                                    .Close(TextStyle.Fraction_Gray);
                                
                                buffer.Add("<size=80%> ");
                                buffer.AddPercentageInColor(perc.ToFInt(), true, false);
                                //buffer.StartPercentageInColor(perc.ToFInt(), true, false );
                                //buffer.AddVarReplace(TextVarMap.Parenthetical, (a,b,c,d)=>c.AddPercentRoundedDynamically( perc.ToFInt() ), null);
                                //buffer.EndColor();
                                buffer.Add("</size>");
                            }
                        }

                        buffer.Close( TextTerm.Hull );

                        if ( !Squad.IsFakeEntity &&
                             ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() &&
                             GameSettings.Current.GetBoolBySetting( "ShowTooltipProgressBars" ) )
                        {
                            buffer.Add( " " );
                            ArcenExternalUIUtilities.AppendBar( buffer, perc, ColorMath.LightGreen, 18, 35, ColorMath.DarkRed, max );
                        }
                    }

                    return;
                }

                if (key.Equals("shields"))
                {
                    int cur = Squad.GetCurrentShieldPoints();
                    int max = Squad.GetMaxShieldPoints();
                    int perc = Squad.GetShieldPercent();

                    if ( max == 0 )
                    {
                        
                    }
                    else
                    {
                        buffer.Open( TextTerm.Shields, TermUse.Icon ).Add(" ");

                        if ( Squad.IsFakeEntity ||
                             Squad.GetFactionTypeSafe() == FactionType.NaturalObject )
                        {
                            buffer.AddNumber( max, null, TextStyle.Empty );
                        }
                        else
                        {
                            buffer.AddNumber( cur, null, TextStyle.Empty );
                            
                            if ( Config.Detail > TooltipDetail.SuperShort &&
                                 cur != max )
                            {
                                buffer
                                    .Open(TextStyle.Fraction_Gray)
                                    .Add( "/" )
                                    .AddNumber( max, null, TextStyle.Empty )
                                    .Close(TextStyle.Fraction_Gray);
                                
                                buffer.Add("<size=80%> ");
                                buffer.AddPercentageInColor(perc.ToFInt(), true, false);
                                //buffer.StartPercentageInColor(perc.ToFInt(), true, false );
                                //buffer.AddVarReplace(TextVarMap.Parenthetical, (a,b,c,d)=>c.AddPercentRoundedDynamically( perc.ToFInt() ), null);
                                //buffer.EndColor();
                                buffer.Add("</size>");
                            }
                        }

                        buffer.Close( TextTerm.Shields );

                        if ( !Squad.IsFakeEntity &&
                             ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() &&
                             GameSettings.Current.GetBoolBySetting( "ShowTooltipProgressBars" ) )
                        {
                            buffer.Add( " " );
                            ArcenExternalUIUtilities.AppendBar( buffer, perc, ColorMath.IceBlue, 18, 35, ColorMath.DarkRed, max );
                        }
                    }

                    return;
                }

                if (key.Equals("speed"))
                {
                    int cur = Squad.CalculateSpeed( false );
                    int max = Squad.DataForMark.Speed;
                    
                    if ( Squad.TypeData.DegreesToOrbitPerSecond != FInt.Zero )
                    {
                        buffer
                            .Open( TextTerm.Speed, TermUse.Icon ).Add(" ")
                            .AddNumber( Squad.TypeData.DegreesToOrbitPerSecond, "掳/s", TextStyle.Empty )
                            .Close( TextTerm.Speed );
                    }
                    else if (max == 0)
                    {
                        
                    }
                    else
                    {
                        buffer.Open( TextTerm.Speed, TermUse.Icon ).Add(" ");

                        if ( Squad.IsFakeEntity ||
                            Squad.GetFactionTypeSafe() == FactionType.NaturalObject )
                        {
                            buffer.AddNumber( max, null, TextStyle.Empty );
                        }
                        else
                        {
                            buffer.AddNumber( cur, null, TextStyle.Empty );

                            if ( Config.Detail > TooltipDetail.SuperShort &&
                                 cur != max )
                            {
                                int perc = perc = (int)(((float)cur / (float)max) * 100);
                                
                                buffer
                                    .Open(TextStyle.Fraction_Gray)
                                    .Add( "/" )
                                    .AddNumber( max, null, TextStyle.Empty )
                                    .Close(TextStyle.Fraction_Gray);
                                
                                buffer.Add("<size=80%> ");
                                buffer.AddPercentageInColor(perc.ToFInt(), true, false);
                                //buffer.StartPercentageInColor(perc.ToFInt(), true, false );
                                //buffer.AddVarReplace(TextVarMap.Parenthetical, (a,b,c,d)=>c.AddPercentRoundedDynamically( perc.ToFInt() ), null);
                                //buffer.EndColor();
                                buffer.Add("</size>");
                            }
                        }

                        buffer.Close( TextTerm.Speed );
                    }
                    
                    return;
                }
                
                if (key.Equals("metal"))
                {
                    if ( Config.ShowMetalCost )
                    {
                        var val = Squad.GetMetalCost();

                        if ( val == 0 )
                        {
                            //buffer.AddVarReplace( TextVarMap.Stat_NA_Format );
                        }
                        else
                        {
                            buffer.Open( TextTerm.Metal, TermUse.Icon ).Add(" ");
                            buffer.AddNumber( val, null, TextStyle.Empty );
                            buffer.Close( TextTerm.Metal );
                        }
                    }
                    
                    return;
                }

                if (key.Equals("energy"))
                {
                    if ( Config.ShowEnergyConsumption )
                    {
                        var val = Squad.GetEnergyUsage();

                        if ( val == 0 )
                        {
                            //buffer.AddVarReplace( TextVarMap.Stat_NA_Format );
                        }
                        else
                        {
                            buffer.Open( TextTerm.Energy, TermUse.Icon ).Add(" ");
                            buffer.AddNumber( val, null, TextStyle.Empty );
                            buffer.Close( TextTerm.Energy );
                        }
                    }
                    
                    return;
                }

                #region Fuel
#if false
                debugstage = 8500;
                cpi.AddStep_EightyPercent();
                //if ( Config.UseText )
                    //cpi.AddStep_Half();
                buffer.ToPos( cpi );

                
                if ( World_AIW2.Instance.IsFuelEnabled )
                {
                    var val = Squad.TypeData.FuelUse;

                    if ( val == 0)
                    {
                        //buffer.AddVarReplace( TextVarMap.Stat_NA_Format );
                    }
                    else
                    {
                        var term = Squad.TypeData.FuelUseType.Term();
                        buffer.Open(term, TermUse.Icon);
                        buffer.AddNumber(val);
                        buffer.Close(term);
                    }
                }
#endif
                #endregion

                if (key.Equals("aip"))
                {
                    var facType = Squad.GetFactionTypeSafe();
                    var entityCanBeClaimed = Squad.HasNotYetBeenFullyClaimed && 
                                             (facType == FactionType.NaturalObject || facType == FactionType.Player);
                    
                    if ( Squad.TypeData.AIPToClaim != FInt.Zero &&
                         entityCanBeClaimed )
                    {
                        //if ( Config.UseText )
                            //buffer.Add( "Claim AIP: " );

                        if ( Squad.TypeData.AIPToClaim > FInt.Zero )
                            buffer.WrapAIPMoreReadable( Squad.TypeData.AIPToClaim, Config.UseIcons, false );
                        else
                            buffer.WrapAIPReductionMoreReadable( Squad.TypeData.AIPToClaim, Config.UseIcons, false );
                    }

                    if ( Squad.TypeData.AIPWhenGrantedByHack != FInt.Zero &&
                         Squad.IsFakeEntity )
                    {
                        //if ( Config.UseText )
                            //buffer.Add( "Hack AIP: " );

                        if ( Squad.TypeData.AIPWhenGrantedByHack > FInt.Zero )
                            buffer.WrapAIPMoreReadable( Squad.TypeData.AIPWhenGrantedByHack, Config.UseIcons, false );
                        else
                            buffer.WrapAIPReductionMoreReadable( Squad.TypeData.AIPWhenGrantedByHack, Config.UseIcons, false );
                    }
                    
                    return;
                }

                if (key.Equals("strength"))
                {
                    int strengthCurr = Squad.GetStrengthPerSquad( false );
                    int strengthTotal = 0;
                    string strengthMode = null;
                    string strengthModeColorHex = null;
                    if ( !Config.InPopup )
                    {
                        if ( Config.OptShipCount > 1 )
                        {
                            strengthTotal = strengthCurr;
                            strengthCurr *= Config.OptShipCount;
                            strengthMode = "脳1";
                            strengthModeColorHex = "ffffff";
                        }
                        else
                        if ( Squad.ShipCount > 1 )
                        {
                            strengthTotal = strengthCurr;
                            strengthCurr *= Squad.ShipCount;
                            strengthMode = "×1";
                            strengthModeColorHex = "ffffff";
                        }
                        else
                        {
                            strengthTotal = Squad.GetStrengthOfContentsIfAny();
                            if ( strengthTotal > 0 )
                            {
                                strengthMode = "+T";
                                strengthModeColorHex = ColorMath.IceBlue.GetHexCode();
                            }
                        }
                    }

                    debugstage = 11000;
                    //if ( Config.UseText )
                        //buffer.Add( "Strength: " );

                    buffer
                        .Open( TextTerm.Strength, TermUse.Icon, TextStyle.Empty ).Add(" ")
                        .AddNumber( FInt.Create( strengthCurr, false ), null, TextStyle.Empty )
                        //.Close( TextTerm.Strength );
                        ;

                    if ( strengthMode != null )
                    {
                        buffer
                            .Add( " " )
                            .AddSize_VerySmall()
                            .AddVarReplace(TextVarMap.Parenthetical, 
                                (a,b,c,d)=>
                                    {
                                        c.AddNumber( FInt.Create( strengthTotal, false ), null, TextStyle.Empty );
                                        c.AddColor( strengthMode, strengthModeColorHex );
                                    } )
                            .EndSize();
                    }

                    buffer.Close( TextTerm.Strength );
                    
                    return;
                }

                if (key.Equals("engine"))
                {
                    var val = Squad.TypeData.Engine_gx;
                    buffer.Open( TextTerm.Engine_gX, TermUse.Icon ).Add(" ");
                    buffer.AddNumber( val, null, TextStyle.Empty );
                    buffer.Close( TextTerm.Engine_gX );

                    return;
                }

                if (key.Equals("mass"))
                {
                    var val = Squad.TypeData.Mass_tX;
                    buffer.Open( TextTerm.Mass_tX, TermUse.Icon ).Add(" ");
                    buffer.AddNumber( val, null, TextStyle.Empty );
                    buffer.Close( TextTerm.Mass_tX );

                    return;
                }
                
                if (key.Equals("albedo"))
                {
                    var val = Squad.DataForMark.Albedo;
                    buffer.Open( TextTerm.Albedo, TermUse.Icon ).Add(" ");
                    buffer.AddNumber( val, null, TextStyle.Empty );
                    buffer.Close( TextTerm.Albedo );

                    return;
                }

                if (key.Equals("armor"))
                {
                    var val = Squad.TypeData.Armor_mm;
                    buffer.Open( TextTerm.Armor_mm, TermUse.Icon ).Add(" ");
                    buffer.AddNumber( val, null, TextStyle.Empty );
                    buffer.Close( TextTerm.Armor_mm );

                    return;
                }

                // not found
                {
                    debugstage = 260;
                    buffer.Add("{").Add(key).Add("}");   
                }
                
                debugstage = 500;
            }
            catch (Exception e)
            {
                LOG.Err("exception at debugstage {0}\n{1}", debugstage, e);
            }
        }
        
        #region Orders
        
        public void WriteEntityOrder(GameEntity_Squad entity, EntityOrder order, ArcenCharacterBufferBase buffer)
        {
            switch(order.TypeData.Type)
            {
                case EntityOrderType.Assist:
                    buffer.Add( "Assist " );
                    WriteEntityForOrder( order.RelatedSquad.GetSquad(), buffer );
                    break;
                case EntityOrderType.Attack:
                    buffer.Add( "Attack " );
                    WriteEntityForOrder( order.RelatedSquad.GetSquad(), buffer );
                    break;
                case EntityOrderType.GetIntoTransport:
                    buffer.Add( "Load into " );
                    WriteEntityForOrder( order.RelatedSquad.GetSquad(), buffer );
                    break;
                case EntityOrderType.Unload_Transport:
                    buffer.Add( "Unload" );
                    break;
                case EntityOrderType.SetBehavior_Attacker_Full:
                    buffer.Add( "Go Attack All" );
                    break;
                case EntityOrderType.SetBehavior_Attacker_PursueOnlyInRange:
                    buffer.Add( "Go Attack Move" );
                    break;
                case EntityOrderType.SetBehavior_Stationary:
                    buffer.Add( "Stop Moving" );
                    break;
                case EntityOrderType.SetBehavior_StopToShootAnySeenTargets_Off:
                    buffer.Add( "Move On" );
                    break;
                case EntityOrderType.SetBehavior_StopToShootAnySeenTargets_On:
                    buffer.Add( "Attack Targets" );
                    break;
                case EntityOrderType.Wormhole:
                    Int16 finalDestinationPlanetIndex = entity.CalculateFinalDestinationPlanetIndex_Safe();
                    if ( finalDestinationPlanetIndex == entity.CalculateNextHopPlanetIndex_Safe() )
                    {
                        Planet planet = World_AIW2.Instance.GetPlanetByIndex( order.RelatedPlanetIndex );
                        buffer.Add( "Go to " );
                        if ( planet == null )
                        {
                            buffer.Add( "unknown planet" );
                        } else
                        {
                            buffer.AddPlanetNameFormated( planet, false );
                        }
                    } else
                    {
                        Planet planet1 = World_AIW2.Instance.GetPlanetByIndex( order.RelatedPlanetIndex );
                        Planet planet2 = World_AIW2.Instance.GetPlanetByIndex( finalDestinationPlanetIndex );
                        buffer.Add( "Go via " );
                        if ( planet1 == null )
                        {
                            buffer.Add( "unknown planet" );
                        } else
                        {
                            buffer.AddPlanetNameFormated( planet1, false );
                        }
                        buffer.Add( " to " );
                        if ( planet2 == null )
                        {
                            buffer.Add( "unknown planet" );
                        } else
                        {
                            buffer.AddPlanetNameFormated( planet2, false );
                        }
                    }
                    break;
                case EntityOrderType.Move_Decollision:
                    buffer.Add( "Decollide" );
                    break;
                case EntityOrderType.Move_Normal:
                    buffer.Add( "Move to " ).Add(order.RelatedPoint.X - 400000).Add(" / ").Add(order.RelatedPoint.Y - 400000);
                    break;
                case EntityOrderType.Custom:
                    order.CustomOrder.GetText(buffer);
                    break;
            }
        }

        public void WriteEntityForOrder( GameEntity_Squad entity, ArcenCharacterBufferBase buffer )
        {
            if ( entity == null )
            {
                buffer.Add( "???" );
                return;
            }
            
            buffer.AddColor( entity.GetDisplayName(true), entity.GetFactionCenterColorHexBrighter_Safe() );
        }

        #endregion
        
        #region WriteTech
        public void WriteTech( ArcenCharacterBufferBase buffer, TechUpgrade upgrade, Faction for_faction, bool abbrev, bool num_lines_affected, ref int debugstage)
        {
            if (upgrade == null)
                throw new ArgumentNullException("upgrade");

            debugstage = 15010101;
            
            //LOG.Msg("WriteTech() called; upgrade={0}, for_faction={1}", upgrade.OrNull(), for_faction.OrNull());
            
            TooltipDetail detailLevel = Config.Detail;
            
            int cur_mark = 0;
            
            if (for_faction != null &&
                for_faction.TechUnlocks != null &&
                for_faction.FreeTechUnlocks != null)
            {
                //if (for_faction.TechUnlocks == null)
                //    throw new ArgumentNullException("for_faction.TechUnlocks", string.Format("for_faction '{0}'", for_faction.ToString()));
                //if (for_faction.FreeTechUnlocks == null)
                //    throw new ArgumentNullException("for_faction.FreeTechUnlocks");
            
                cur_mark = 
                    for_faction.TechUnlocks[upgrade.RowIndexNonSim] + 
                    for_faction.FreeTechUnlocks[upgrade.RowIndexNonSim];
            }

            debugstage = 15010102;
            
            var marks = Balance_MarkLevelTable.Instance.RowsByOrdinal;
            
            int idx = cur_mark >= 0 ? cur_mark + 1 : 0;
            if ( idx < 0 )
                idx = 0;
            if ( idx >= marks.Length )
                idx = marks.Length - 1;

            var upgradeCosts = upgrade.GetScienceOrOtherResourceCostsPerTimeUnlocked();
            var markstat = marks[idx];
            
            if (abbrev)
                buffer.Add( upgrade.GetShortDisplayName(), markstat.ColorHex );
            else
                buffer.Add( upgrade.GetDisplayName(), markstat.ColorHex );
            
            bool will_show_lines = num_lines_affected && 
                                   (upgrade.UIOnly_Tech_ShipLinesAffected > 0 || 
                                    upgrade.UIOnly_Tech_DefensiveLinesAffected > 0);
            
            if (for_faction != null)
            {
                debugstage = 15010103;
                if (cur_mark > 0)
                {
                    buffer.StartColor( markstat.ColorHex );
                    
                    if (!will_show_lines)
                        buffer.Add( " <size=50%>Mk</size>" );
                        
                    buffer
                        .Add( cur_mark )
                        .Add( "" )
                        .Open(TextStyle.Fraction_Gray)
                        .Add( "/" )
                        .Add( upgradeCosts.Count )
                        .Close(TextStyle.Fraction_Gray)
                        .EndColor();
                }
                
                if (will_show_lines)
                {
                    if ( detailLevel <= TooltipDetail.Medium )
                    {
                        buffer
                            .Add("<voffset=0.05em>(</voffset>")
                            .AddColor( upgrade.UIOnly_Tech_ShipLinesAffected, ArcenExternalUIUtilities.ShipLineIncreaseColor )
                            .Add( " " )
                            .AddColor( upgrade.UIOnly_Tech_DefensiveLinesAffected, ArcenExternalUIUtilities.DefenseLineIncreaseColor )
                            .Add("<voffset=0.05em>)</voffset>");
                    }
                    else 
                    //if ( detailLevel > TooltipDetail.SuperShort )
                    {
                        buffer
                            .Add("<voffset=0.05em>(</voffset>")
                            .AddColor( upgrade.UIOnly_Tech_ShipLinesAffected, ArcenExternalUIUtilities.ShipLineIncreaseColor )
                            .Add( " ship " )
                            .AddColor( upgrade.UIOnly_Tech_DefensiveLinesAffected, ArcenExternalUIUtilities.DefenseLineIncreaseColor )
                            .Add(" defense<voffset=0.05em>)</voffset>");
                    }
                }
            }
        }
        #endregion

        #region WriteShips (Lists)

        // todo: move to ShipsForDisplay.AddRange
        private void AppendListForDisplay( Fleet fleet, ShipsForDisplay results )
        {
            var cfg = this.Config;
            var detail = Config.Detail;
            //var showDupLines = Config.ShowIndividualFleetMemberships;
            var isCity = fleet.Category == FleetCategory.PlayerCustomCity || fleet.Category == FleetCategory.PlayerCustomCityFedMobile;
            
            foreach ( FleetMembership Mem in fleet.MemberGroupsSorted_NonSim_ForUIOnly_SeriouslyDoNotCallFromBGCode() )
            {
                //int effectiveHere = Mem.GetCountPresent( true, ExtraFromStacks.IncludePrecalc );
                //if ( detail < TooltipDetail.Medium && isCity && effectiveHere <= 0 )
                    //continue;

                bool isExcludedSpecialType = false;
                switch ( Mem.TypeData.SpecialType )
                {
                    case SpecialEntityType.AICommandStationOriginal:
                    case SpecialEntityType.AICommandStationReconquest:
                    case SpecialEntityType.BattlestationBasic:
                    case SpecialEntityType.BattlestationCitadel:
                    case SpecialEntityType.CityCenter:
                    case SpecialEntityType.HumanHomeCommand:
                    case SpecialEntityType.MobileOfficerCombatFleetFlagship:
                    case SpecialEntityType.MobileStrikeCombatFleetFlagship:
                    case SpecialEntityType.MobileCustomUnattachedFleetFlagship:
                    case SpecialEntityType.MobileSupportFleetFlagship:
                    case SpecialEntityType.NormalHumanCommandStation:
                    case SpecialEntityType.NPCFactionCenterpiece:
                    case SpecialEntityType.WardenSecretNinjaHideout:
                        isExcludedSpecialType = true;
                        break;
                }
                if ( isExcludedSpecialType )
                    continue;

                var item = new ShipForDisplay(Mem, Config);
                results.Add(item);
            }
        }

        // todo: move to ShipsForDisplay.AddRange
        private void AppendListForDisplay( List<GameEntity_Squad> squads, ShipsForDisplay results )
        {
            foreach (var e in squads)
                results.Add( new ShipForDisplay( e, this.Config ) );
        }

        // todo: move to ShipsForDisplay.AddRange
        private void AppendListForDisplay( List<LazyLoadSquadWrapper> squads, ShipsForDisplay results )
        {
            foreach (var obj in squads)
            {
                var e = obj.GetSquad();
                if (e == null)
                    continue;
                
                results.Add( new ShipForDisplay( e, this.Config ) );
            }
        }
        
        public void WriteShips( List<LazyLoadSquadWrapper> ships )
        {
            using (var list = ShipsForDisplay.Get())
            {
                AppendListForDisplay(ships, list);
                WriteShips(list);
            }
        }
        
        public void WriteShips( List<GameEntity_Squad> ships, List<GameEntity_Squad> ships2 = null )
        {
            using (var list = ShipsForDisplay.Get())
            {
                AppendListForDisplay(ships, list);
                if (ships2 != null)
                    AppendListForDisplay(ships2, list);
                
                WriteShips(list);
            }
        }
        
        public void WriteShips( ShipsForDisplay ships, Predicate<ShipForDisplay> show = null )
        {
            ships.Sort(0);
            
            int counter = 0;
            var list = ships.Ships;
            
            for ( int i = 0; i < list.Count; i++ )
            {
                var item = list[i];

                if (show != null)
                {
                    if (!show(item))
                        continue;
                }
                
                int current = item.Count;
                int max = item.Cap;
                if ( max < current )
                    max = current;
                
                if ( current <= 0 && max <= 0 )
                    continue;
                
                if ( counter > 0 )
                    this.Buffer.Add( ships.Delimiter ?? ", " );
                counter++;
                
                this.Buffer.AddVarReplace( ships.Varmap ?? TextVarMap.Inline_Ship_Format, item.AppendVarValue );
            }
        }
        
        public void WriteShips( Fleet fleet )
        {
            using (var ships = ShipsForDisplay.Get())
            {
                AppendListForDisplay(fleet, ships);
                WriteShips(ships);
            }
        }

        public void WriteShip( LazyLoadSquadWrapper ship, ShipExtraDetailFlags flags=ShipExtraDetailFlags.None )
        {
            WriteShip(ship.GetSquad(), flags);
        }
        
        public void WriteShip( GameEntity_Squad e, ShipExtraDetailFlags flags=ShipExtraDetailFlags.None )
        {
            if (e == null)
                return;
            
            var config = this.Config;
            config.ExtraFlags |= flags;

            var ship = new ShipForDisplay(e, config);
            ship.Write(this.Buffer, TextVarMap.Inline_Ship_Format);
        }
        
        // Write this type of ship, as if it were spawned from the current ship
        // we are writing for. So, it inherits the same faction, fleet, mark ...
        public void WriteSpawn( GameEntityTypeData spawn_type, int spawn_count=1, TextStyle spawn_icon_style=null)
        {
            var e = EntityText.GetFakeEntity(spawn_type, this.Squad.CurrentMarkLevel, this.Squad.GetFactionOrNull_Safe(), this.Squad.Planet);
            e.SetShipCount(spawn_count);
            
            var ship = new ShipForDisplay(e, this.Config);
            ship.IconStyleOverride = spawn_icon_style;
            
            ship.Write(this.Buffer, TextVarMap.Inline_Ship_Format);
            
            EntityText.ReleaseFakeEntity(e);
        }

        #endregion
        
        #region WriteSystem
        public void WriteSystem( EntitySystem entitySystem )
        {
            var txt = new SystemText( entitySystem, this.Config );
            txt.Write( this );
        }
        #endregion

        #region WriteFleet
        public void WriteFleet( GameEntity_Squad squad )
        {
            var txt = new FleetText( squad, this.Config );
            txt.Write( this );
        }
        #endregion

        #region Write (DrawBag)
        
        public void Write( EntityTypeDrawingBag bag )
        {
            var buffer = this.Buffer;
            
            if (bag.DisplayText != null)
            {
                buffer.Add( bag.DisplayText );
                return;
            }
            
            if( EntityTypeDrawingBag.IsNullOrInvalid(bag) )
            {
                /*
                if(ForDebug)
                {
                    buffer.Add( "Spawns nothing (any more) because it is disabled" );
                }
                */
                return;
            }
            
            ListOfLists<GameEntityTypeData> temp = null;
            try
            {
                var faction = Squad.GetFactionOrNull_Safe();
                var planet = Squad.Planet;
            
                temp = GameEntityTypeData.GetTemporaryGameEntityTypeDataListOfLists("EntityText.Extensions.Write(bag).temp", 2);
                if ( temp == null ) //blocked for teardown/shutdown; bail
                    return;

                bag.FillAllPossibleEntityTypes(temp, faction);
            
                var config = this.Config;
                if (temp.TotalCount < 3)
                    config.ExtraFlags |= ShipExtraDetailFlags.HighestDetail;
                
                for ( int i = 0; i < bag.Count; i++ )
                {
                    if (i > 0)
                        buffer.Add( ", " );
                    
                    if (bag.SpawnValue_Min[i] == bag.SpawnValue_Max[i])
                        buffer.Add( bag.SpawnValue_Min[i] );
                    else
                        buffer.Add( bag.SpawnValue_Min[i] ).Add( "-" ).Add( bag.SpawnValue_Max[i] );
                    
                    var type = bag.CountTypeList[i];
                    switch(type)
                    {
                        case EntityTypeDrawingBag_SpawnMode.RawCount:
                            buffer.Add( "× " );
                            break;
                        case EntityTypeDrawingBag_SpawnMode.AIBudget:
                            buffer.Add( " AI budget worth of " );
                            break;
                        case EntityTypeDrawingBag_SpawnMode.Strength_BaseMark:
                            buffer.Add( " base strength worth of " );
                            break;
                        case EntityTypeDrawingBag_SpawnMode.Strength_CurrentMark:
                            buffer.Add( " strength worth of " );
                            break;
                        case EntityTypeDrawingBag_SpawnMode.MetalCost:
                            buffer.Add( " metal worth of " );
                            break;
                        case EntityTypeDrawingBag_SpawnMode.EnergyCost:
                            buffer.Add( " energy worth of " );
                            break;
                        default:
                            buffer.Add( " unhandled value '" ).Add(Extensions.ToString(type)).Add("' ");
                            break;
                    }
                    
                    //buffer.Add("[ ");
                    var types = temp[i];
                    for (int j = 0; j < types.Count; j++)
                    {
                        if (j > 0)
                            buffer.Add(", ");
                        
                        var e = EntityText.GetFakeEntity(types[j], this.Squad.CurrentMarkLevel, faction, planet);
                        var ship = new ShipForDisplay(e, config);
                        ship.Write(buffer, TextVarMap.Inline_Ship_Format);
                        EntityText.ReleaseFakeEntity(e);
                    }
                    //buffer.Add("  ]");
                    
                    continue;

                    #region Unused
                    #if false
                    switch ( bag.ModeList[i])
                    {
                        case EntityTypeDrawingBag_ParseMode.EntityName:
                        {
                            //var e = EntityText.GetFakeEntity(bag.EntityList[i], )
                            //writer.WriteShip()
                            buffer.Add( bag.EntityList[i].DisplayName );
                            break;
                        }
                        
                        case EntityTypeDrawingBag_ParseMode.Tag:
                        {
                            buffer.Add( bag.TagList[i] );
                            break;
                        }
                        
                        case EntityTypeDrawingBag_ParseMode.AIShipGroup:
                        {
                            
                            buffer.Add( EntityTypeDrawingBag.GetName_PreferDisplay( bag.AIShipGroupList[i] ) );
                            break;
                        }
                        
                        case EntityTypeDrawingBag_ParseMode.AIShipGroupCategory:
                        {
                            buffer.Add( EntityTypeDrawingBag.GetName_PreferDisplay( bag.AIShipGroupCategoryList[i] ) );
                            break;
                        }
                        
                        case EntityTypeDrawingBag_ParseMode.AIBudgetCategory:
                        {
                            switch(bag.AIBudgetSubCategoryList[i])
                            {
                                case EntityTypeDrawingBag_AIBudgetSubCategory.NormalAIShipGroup:
                                    buffer.Add( "Normal" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetSubCategory.GuardPostAIShipGroup:
                                    buffer.Add( "Guard Post" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetSubCategory.DireGuardPostAIShipGroup:
                                    buffer.Add( "Dire Guard Post" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetSubCategory.UnarmedGuardPostAIShipGroup:
                                    buffer.Add( "Unarmed Guard Post" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetSubCategory.TurretAIShipGroup:
                                    buffer.Add( "Turret" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetSubCategory.NonTurretDefenseAIShipGroup:
                                    buffer.Add( "Non-Turret Defense" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetSubCategory.GuardianAIShipGroup:
                                    buffer.Add( "Guardian" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetSubCategory.WormholeSentinelAIShipGroup:
                                    buffer.Add( "Wormhole Sentinel" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetSubCategory.ForcefieldGuardianAIShipGroup:
                                    buffer.Add( "Forcefield Guardian" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetSubCategory.DecloakerAIShipGroup:
                                    buffer.Add( "Decloaker" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetSubCategory.DireGuardianAIShipGroup:
                                    buffer.Add( "Dire Guardian" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetSubCategory.RegularSingularFreakySurprisesAIShipGroup:
                                    buffer.Add( "Singular Freaky Surprise" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetSubCategory.DireSingularFreakySurprisesAIShipGroup:
                                    buffer.Add( "Dire Singular Freaky Surprise" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetSubCategory.ExoLeaderAIShipGroup:
                                    buffer.Add( "Exo Strike Leader" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetSubCategory.All:
                                    buffer.Add( "any" );
                                    break;
                                default://just in case I forget something, or something new is implemented
                                    throw new NotImplementedException( "Error: Unimplemented AIBudgetSubCategory type: " + bag.AIBudgetSubCategoryList[i] + "!" );
                            }
                            
                            buffer.Add( " AI ship group from " );
                            
                            switch(bag.AIBudgetCategoryList[i])
                            {
                                case EntityTypeDrawingBag_AIBudgetCategory.Reinforcement:
                                    buffer.Add( "Reinforcement" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetCategory.Warden:
                                    buffer.Add( "Warden" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetCategory.PraetorianGuard:
                                    buffer.Add( "Praetorian" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetCategory.HunterFleet:
                                    buffer.Add( "Hunter" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetCategory.Wave:
                                    buffer.Add( "Wave" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetCategory.BorderAggression:
                                    buffer.Add( "Border Aggression" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetCategory.CPA:
                                    buffer.Add( "CPA" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetCategory.Reconquest:
                                    buffer.Add( "Reconquest" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetCategory.WormholeInvasion:
                                    buffer.Add( "Wormhole Invasion" );
                                    break;
                                case EntityTypeDrawingBag_AIBudgetCategory.All:
                                    buffer.Add( "any" );
                                    break;
                                default://just in case I forget something, or something new is implemented
                                    throw new NotImplementedException( "Error: Unimplemented AIBudgetCategory type: " + bag.AIBudgetCategoryList[i] + "!" );
                            }
                            buffer.Add( " AI budget group" );
                            
                            break;
                        }
                        
                        case EntityTypeDrawingBag_ParseMode.FleetDesignTemplate:
                        {
                            switch (bag.FleetDesignTemplateSubCategoryList[i])
                            {
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.Strike:
                                    buffer.Add( "Strike Craft" );
                                    break;
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.Frigate:
                                    buffer.Add( "Frigate" );
                                    break;
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.Turret:
                                    buffer.Add( "Turret" );
                                    break;
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.OtherDefense:
                                    buffer.Add( "Non-Turret Defense" );
                                    break;
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.Centerpiece:
                                    buffer.Add( "Centerpiece" );
                                    break;
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.Civilian:
                                    buffer.Add( "Civilian" );
                                    break;
                                case EntityTypeDrawingBag_FleetDesignTemplateSubCategory.All:
                                    buffer.Add( "any" );
                                    break;
                                default://just in case I forget something, or something new is implemented
                                    throw new NotImplementedException( "Error: Unimplemented Fleet Template Sub Category type: " + bag.FleetDesignTemplateSubCategoryList[i] + "!" );
                            }
                            buffer.Add( " from " ).Add( EntityTypeDrawingBag.GetName_PreferDisplay( bag.FleetDesignTemplateList[i] ) );
                            
                            break;
                        }
                        case EntityTypeDrawingBag_ParseMode.SpecialEntityType:
                        {
                            buffer.Add( bag.SpecialEntityTypeList[i] );
                            break;
                        }
                        default://just in case I forget something, or something new is implemented
                            throw new NotImplementedException( "Error: Unimplemented ParseMode type: " + bag.ModeList[i] + "!" );
                    }
                    #endif
                    #endregion
                }
                
                //if ( ForSpawner != null && 
                //     ForSpawner.DeathSpawnHappensOnOwnPlanetAndAllAdjacentPlanets )
                //{
                //    buffer.Add( " on this planet, and each adjacent planet." );
                //}
            }
            finally
            {
                GameEntityTypeData.ReleaseTemporaryGameEntityTypeDataListOfLists(temp);
            }
        }
        
        #endregion
        
        #region Write (DamageModifier)

        public void Write( DamageModifier modifier )
        {
            int debugstage = 0;
            try
            {
                var buffer = this.Buffer;
                var config = this.Config;
                debugstage = 100;
                var mark = this.Squad.CurrentMarkLevel;
                debugstage = 101;
                var multiplier = modifier.MultiplierForMark[mark];
                debugstage = 102;
                buffer.Open(TextStyle.System_Line2);
                
                buffer.Open(TextStyle.System_Label2);
                debugstage = 103;
                if (modifier.ComparisonType == DamageModifierComparisonType.MultiplesOf)
                {
                    buffer.Add("Scaling-");
                }
                debugstage = 104;
                if ( modifier.IsForOutgoingDamage )
                {
                    if ( multiplier >= FInt.One )
                        buffer.Add( "Bonus");
                    else
                        buffer.Add( "Penalty");
                }
                else
                {
                    if ( multiplier < FInt.One )
                        buffer.Add( "Defense-Bonus");
                    else
                        buffer.Add( "Defense-Penalty");
                }
                buffer.Close(TextStyle.System_Label2);
                debugstage = 105;
                buffer.Add(": ");

                if ( modifier.IsForOutgoingDamage )
                    buffer.Add( "Deals ");
                else
                    buffer.Add( "Takes ");
                
                if (modifier.NeedsToGetMultiples)
                    multiplier += 1;
                /*
                if ( modifier.AppliesTo == DamageModifierAppliesTo.AllShields ||
                     modifier.AppliesTo == DamageModifierAppliesTo.PersonalShieldOnly)
                {
                    buffer.Add(TextTerm.Shields, TermUse.Icon).Add("");
                }
                else
                if ( modifier.AppliesTo == DamageModifierAppliesTo.HullOnly)
                {
                    buffer.Add(TextTerm.Hull, TermUse.Icon).Add("");
                }
                */
                debugstage = 105;
                buffer.Open(TextTerm.Damage, TermUse.Icon);
                if ( !modifier.IsForOutgoingDamage )
                    buffer.Add(multiplier * 100).Add("%");
                else
                    buffer.Add("×").Add( multiplier );
                buffer.Close(TextTerm.Damage);
                
                debugstage = 106;
                if (modifier.NeedsToGetMultiples && 
                    modifier.MaxMultiplier != int.MaxValue && 
                    modifier.MaxMultiplier > 0)
                {
                    // (x10 max)
                    buffer.Add(" (").Add("×").Add(modifier.MaxMultiplier).Add(" max)");
                }
                
                debugstage = 107;
                if ( modifier.BasedOn == DamageModifierBasedOn.MaxBubbleForcefield )
                {
                    debugstage = 108;
                    buffer.Add(" to ").Open(TextTerm.Shields, TermUse.Color).Add("Bubble-Shields").Close(TextTerm.Shields).Add(".");
                    buffer.Close(TextStyle.System_Line2);
                    debugstage = 109;
                    return;
                }
                
                if ( modifier.AppliesTo == DamageModifierAppliesTo.AllShields ||
                     modifier.AppliesTo == DamageModifierAppliesTo.PersonalShieldOnly)
                {
                    buffer.Add(" to ").Add(TextTerm.Shields, TermUse.Icon_Name).Add("");
                }
                else
                if ( modifier.AppliesTo == DamageModifierAppliesTo.HullOnly)
                {
                    buffer.Add(" to ").Add(TextTerm.Hull, TermUse.Icon_Name).Add("");
                }

                //buffer.AddNumber( multiplier, "脳", TextTerm.Damage, TermUse.Name );

                if ( modifier.BasedOn == DamageModifierBasedOn.Always )
                {
                    debugstage = 110;
                    
                    if ( modifier.IsForOutgoingDamage )
                        buffer.Add( " to all targets." );
                    else
                        buffer.Add( " from all targets." );

                    buffer.Close(TextStyle.System_Line);
                    
                    debugstage = 111;
                    
                    return;
                }
                
                debugstage = 112;
                
                FInt comp_val;
                if ( modifier.UsesInt )
                    comp_val = modifier.ComparedToInt.ToFInt();
                else
                    comp_val = modifier.ComparedToFInt;
                
                debugstage = 113;
                
                bool max = false;
                bool perc = false;
                bool missing = false;
                bool remaining = false;
                TermUse term_use = TermUse.Icon;
                TextTerm comp_term = TextTerm.Null;
                
                switch ( modifier.BasedOn )
                {
                    case DamageModifierBasedOn.MaxHull:
                        comp_term = TextTerm.Hull;
                        max = true;
                        break;
                    case DamageModifierBasedOn.TargetHullPercentageMissing:
                        comp_term = TextTerm.Hull;
                        missing = true;
                        perc = true;
                        break;
                    case DamageModifierBasedOn.CurrentHullPercentage:
                        comp_term = TextTerm.Hull;
                        perc = true;
                        remaining = true;
                        break;
                    case DamageModifierBasedOn.MyCurrentHullPercentage:
                        comp_term = TextTerm.Hull;
                        perc = true;
                        remaining = true;
                        break;
                    case DamageModifierBasedOn.MyHullPercentageMissing:
                        comp_term = TextTerm.Hull;
                        missing = true;
                        perc = true;
                        break;
                    case DamageModifierBasedOn.MaxPersonalShield:
                        comp_term = TextTerm.Shields;
                        max = true;
                        break;
                    case DamageModifierBasedOn.TargetShieldPercentageMissing:
                        comp_term = TextTerm.Shields;
                        missing = true;
                        perc = true;
                        break;
                    case DamageModifierBasedOn.MyShieldPercentageMissing:
                        comp_term = TextTerm.Shields;
                        missing = true;
                        perc = true;
                        break;
                    case DamageModifierBasedOn.MaxBubbleForcefield:
                        comp_term = TextTerm.Shields;
                        max = true;
                        break;
                    case DamageModifierBasedOn.CurrentPersonalShieldPercentage:
                        comp_term = TextTerm.Shields;
                        perc = true;
                        remaining = true;
                        break;
                    case DamageModifierBasedOn.MyCurrentPersonalShieldPercentage:
                        comp_term = TextTerm.Shields;
                        perc = true;
                        remaining = true;
                        break;
                    case DamageModifierBasedOn.AttackDistance:
                        comp_term = TextTerm.Range;
                        break;
                    case DamageModifierBasedOn.CurrentSpeedIfMoving:
                        comp_term = TextTerm.Speed;
                        break;
                    case DamageModifierBasedOn.Armor_mm:
                        comp_term = TextTerm.Armor_mm;
                        break;
                    case DamageModifierBasedOn.EnergyUsage:
                        comp_term = TextTerm.Energy;
                        break;
                    case DamageModifierBasedOn.Albedo:
                        comp_term = TextTerm.Albedo;
                        break;
                    case DamageModifierBasedOn.Mass_tX:
                        comp_term = TextTerm.Mass_tX;
                        break;
                    case DamageModifierBasedOn.Engine_gx:
                        comp_term = TextTerm.Engine_gX;
                        break;
                    case DamageModifierBasedOn.TargetTimeAtPlanet:
                        break;
                    case DamageModifierBasedOn.MyTimeAtPlanet:
                        break;
                    case DamageModifierBasedOn.FactionNetEnergy:
                        break;
                    case DamageModifierBasedOn.TargetMobility:
                        break;
                    case DamageModifierBasedOn.TargetIsDrone:
                        break;
                    default:
                        buffer.Add( "Unknown DamageModifierBasedOn." ).Add( EnumNameCache.GetName( modifier.BasedOn ) );
                        break;
                }

                debugstage = 114;
                
                string prefix = null;
                string suffix = null;
                switch ( modifier.ComparisonType )
                {
                    case DamageModifierComparisonType.LessThan:
                        prefix = "<<space=0.1em>";
                        break;
                    case DamageModifierComparisonType.GreaterThan:
                        prefix = "><space=0.1em>";
                        break;
                    case DamageModifierComparisonType.AtMost:
                        prefix = "鈮?space=0.1em>";
                        break;
                    case DamageModifierComparisonType.AtLeast:
                        //prefix = "";// " 鈮?";
                        suffix = "+";
                        break;
                    case DamageModifierComparisonType.MultiplesOf:
                        prefix = "";//"脳<space=0.1em>";
                        break;
                    case DamageModifierComparisonType.Equals:
                        prefix = "=<space=0.1em>";
                        break;
                    case DamageModifierComparisonType.NotEquals:
                        prefix = "鈮?space=0.1em>";
                        break;
                }
                
                debugstage = 115;

                if ( modifier.ComparisonType == DamageModifierComparisonType.MultiplesOf )
                {
                    buffer.Add( " per " );//.AddNumber(comp_val, Text.Multiply).Add(" ").Add(comp_term, TermUse.Icon);
                    
                    if ( modifier.BasedOn == DamageModifierBasedOn.MyTimeAtPlanet ||
                         modifier.BasedOn == DamageModifierBasedOn.TargetTimeAtPlanet )
                    {
                        debugstage = 116;
                        
                        buffer.AddMinutesAndSeconds(comp_val);
                        if (modifier.ComparisonRefersToMyself)
                            buffer.Add(" of our ");
                        else
                            buffer.Add(" of target ");
                        buffer.Add("time on planet", TextStyle.Brighter);
                        buffer.Add(".");
                        buffer.Close(TextStyle.System_Line2);

                        debugstage = 117;
                        
                        return;
                    }
                    
                    if ( modifier.BasedOn == DamageModifierBasedOn.AttackDistance )
                    {
                        debugstage = 118;
                        
                        buffer.Open(TextTerm.Range, TermUse.Color);
                        buffer.Add(comp_val).Add(" Range");
                        buffer.Close(TextTerm.Range);
                        buffer.Add(" away.");
                        buffer.Close(TextStyle.System_Line2);

                        return;
                    }
                }
                else
                {
                    if ( modifier.ComparisonRefersToMyself )
                    {
                        buffer.Add( " if our " );
                    }
                    else
                    {
                        buffer.Add( " if target " );
                    }
                }
                
                debugstage = 119;
                
                if ( modifier.BasedOn == DamageModifierBasedOn.TargetMobility)
                {
                    debugstage = 120;
                    
                    buffer.Add("is ").Add( modifier.ComparedToInt > 0 ? "'Mobile'" : "'Stationary'", TextStyle.Brighter).Add(".");
                    buffer.Close(TextStyle.System_Line2);
                    
                    debugstage = 121;
                    
                    return;
                }
                
                if ( modifier.BasedOn == DamageModifierBasedOn.TargetIsDrone)
                {
                    debugstage = 122;
                    
                    buffer.Add("is ").Add( modifier.ComparedToInt > 0 ? "'Drone'" : "'Not Drone'", TextStyle.Brighter).Add(".");
                    buffer.Close(TextStyle.System_Line2);
                    
                    debugstage = 123;
                    
                    return;
                }
                
                if (modifier.BasedOn == DamageModifierBasedOn.TargetTimeAtPlanet ||
                    modifier.BasedOn == DamageModifierBasedOn.MyTimeAtPlanet)
                {
                    debugstage = 124;
                    
                    buffer.Add("time on planet ", TextStyle.Brighter);
                    
                    buffer.Open(TextStyle.MinutesAndSeconds);
                    
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        buffer.Add(prefix, TextCaps.Normal);
                    }
                    
                    buffer.AddMinutesAndSeconds(comp_val);
                    
                    if (!string.IsNullOrEmpty(suffix))
                    {
                        buffer.Add(suffix, TextCaps.Normal);
                    }
                    
                    buffer.Close(TextStyle.MinutesAndSeconds);
                    buffer.Add(".");
                    buffer.Close(TextStyle.System_Line2);
                    
                    debugstage = 125;
                    
                    return;
                }
                
                debugstage = 126;
                
                buffer.AddNumber(
                    ()=>
                    {
                        debugstage = 127;
                        
                        buffer.Space("0.05em");
                        if (max)
                            buffer.Add("max", TextStyle.Sub);
                            //buffer.Add("鈫?);
                        if (prefix != null)
                            buffer.Add(prefix);
                        
                        debugstage = 128;
                        
                        buffer.AddNumber(comp_val, null, TextStyle.Empty);
                        if (perc)
                            buffer.Add("%");
                        
                        if (suffix != null)
                            buffer.Add(suffix);
                        
                        debugstage = 129;
                    },
                    null, comp_term, term_use, null);
                
                debugstage = 130;
                
                if (missing)
                    buffer.Add(" missing");
                if (remaining)
                    buffer.Add(" remaining");

                //if (missing)
                    //buffer.Add(" missing on ");
                //if (Modifier.ComparisonRefersToMyself)
                //    buffer.Add("this ship.");
                //else
                //    buffer.Add("target ship.");

                debugstage = 131;
                
                if ( modifier.ComparisonType == DamageModifierComparisonType.MultiplesOf )
                {
                    if ( modifier.ComparisonRefersToMyself )
                    {
                        buffer.Add( " of this ship." );
                    }
                    else
                    {
                        buffer.Add( " of the target." );
                    }
                }
                else
                {
                    buffer.Add(".");
                }
                
                debugstage = 132;
                buffer.Close(TextStyle.System_Line2);
                
                debugstage = 133;
            }
            catch (Exception e)
            {
                LOG.Err("exception at debugstage {0}\n{1}", debugstage, e);
            }
        }

        #endregion

        #region Buffs, ShipClass

        public void WriteBuffsStartIfNeeded( ArcenCharacterBufferBase buffer, ref bool alreadyWroteBuffsStart )
        {
            if ( alreadyWroteBuffsStart )
            {
                buffer.Add( ", " );
            } 
            else
            {
                buffer.Add( "Buffs", TextStyle.Buffs_Label ).Add(": ");
                alreadyWroteBuffsStart = true;
            }
        }

        public void WriteHullBuffsStartIfNeeded( ArcenCharacterBufferBase buffer, bool useIcons, ref bool alreadyWroteBuffsStart, ref bool alreadyWroteHullBuffsStart )
        {
            if ( alreadyWroteHullBuffsStart )
            {
                buffer.Add( ", " );
            } 
            else
            {
                WriteBuffsStartIfNeeded( buffer, ref alreadyWroteBuffsStart );
                buffer.WrapHull( "Hull: ", false, false );
                alreadyWroteHullBuffsStart = true;
            }
        }

        public void WriteShieldBuffsStartIfNeeded( ArcenCharacterBufferBase buffer, bool useIcons, ref bool alreadyWroteBuffsStart, ref bool alreadyWroteShieldBuffsStart )
        {
            if ( alreadyWroteShieldBuffsStart )
            {
                buffer.Add( ", " );
            } else
            {
                WriteBuffsStartIfNeeded( buffer, ref alreadyWroteBuffsStart );
                buffer.WrapShield( "Shield: ", false, false );
                alreadyWroteShieldBuffsStart = true;
            }
        }

        public void WriteDamageBuffsStartIfNeeded( ArcenCharacterBufferBase buffer, bool useIcons, ref bool alreadyWroteBuffsStart, ref bool alreadyWroteDamageBuffsStart )
        {
            if ( alreadyWroteDamageBuffsStart )
            {
                buffer.Add( ", " );
            } else
            {
                WriteBuffsStartIfNeeded( buffer, ref alreadyWroteBuffsStart );
                buffer.WrapDamage( "Damage: ", false, false );
                alreadyWroteDamageBuffsStart = true;
            }
        }

        public void WriteSpeedBuffsStartIfNeeded( ArcenCharacterBufferBase buffer, bool useIcons, ref bool alreadyWroteBuffsStart, ref bool alreadyWroteSpeedBuffsStart )
        {
            if ( alreadyWroteSpeedBuffsStart )
            {
                buffer.Add( ", " );
            } else
            {
                WriteBuffsStartIfNeeded( buffer, ref alreadyWroteBuffsStart );
                buffer.WrapSpeed( "Speed: ", false, false );
                alreadyWroteSpeedBuffsStart = true;
            }
        }

        public void WriteRangeBuffsStartIfNeeded( ArcenCharacterBufferBase buffer, bool useIcons, ref bool alreadyWroteBuffsStart, ref bool alreadyWroteRangeBuffsStart )
        {
            if ( alreadyWroteRangeBuffsStart )
            {
                buffer.Add( ", " );
            } else
            {
                WriteBuffsStartIfNeeded( buffer, ref alreadyWroteBuffsStart );
                buffer.WrapRange( "Range: ", false, false );
                alreadyWroteRangeBuffsStart = true;
            }
        }

        public void WriteCloakBuffsStartIfNeeded( ArcenCharacterBufferBase buffer, bool useIcons, ref bool alreadyWroteBuffsStart, ref bool alreadyWroteCloakBuffsStart )
        {
            if ( alreadyWroteCloakBuffsStart )
            {
                buffer.Add( ", " );
            } else
            {
                WriteBuffsStartIfNeeded( buffer, ref alreadyWroteBuffsStart );
                buffer.WrapCloak( "Cloak: ", false, false );
                alreadyWroteCloakBuffsStart = true;
            }
        }
        
        public void WriteDebuffsStartIfNeeded( ArcenCharacterBufferBase buffer, ref bool alreadyWroteDebuffsStart )
        {
            if ( alreadyWroteDebuffsStart )
            {
                buffer.Add( ", " );
            } 
            else
            {
                buffer.Add( "Debuffs", TextStyle.Debuffs_Label ).Add(": ");
                alreadyWroteDebuffsStart = true;
            }
        }

        public void WriteHullDebuffsStartIfNeeded( ArcenCharacterBufferBase Debuffer, bool useIcons, ref bool alreadyWroteDebuffsStart, ref bool alreadyWroteHullDebuffsStart )
        {
            if ( alreadyWroteHullDebuffsStart )
            {
                Debuffer.Add( ", " );
            } else
            {
                WriteDebuffsStartIfNeeded( Debuffer, ref alreadyWroteDebuffsStart );
                Debuffer.WrapHull( "Hull: ", false, false );
                alreadyWroteHullDebuffsStart = true;
            }
        }

        public void WriteShieldDebuffsStartIfNeeded( ArcenCharacterBufferBase Debuffer, bool useIcons, ref bool alreadyWroteDebuffsStart, ref bool alreadyWroteShieldDebuffsStart )
        {
            if ( alreadyWroteShieldDebuffsStart )
            {
                Debuffer.Add( ", " );
            } else
            {
                WriteDebuffsStartIfNeeded( Debuffer, ref alreadyWroteDebuffsStart );
                Debuffer.WrapShield( "Shield: ", false, false );
                alreadyWroteShieldDebuffsStart = true;
            }
        }

        public void WriteDamageDebuffsStartIfNeeded( ArcenCharacterBufferBase Debuffer, bool useIcons, ref bool alreadyWroteDebuffsStart, ref bool alreadyWroteDamageDebuffsStart )
        {
            if ( alreadyWroteDamageDebuffsStart )
            {
                Debuffer.Add( ", " );
            } else
            {
                WriteDebuffsStartIfNeeded( Debuffer, ref alreadyWroteDebuffsStart );
                Debuffer.WrapDamage( "Damage: ", false, false );
                alreadyWroteDamageDebuffsStart = true;
            }
        }

        public void WriteSpeedDebuffsStartIfNeeded( ArcenCharacterBufferBase Debuffer, bool useIcons, ref bool alreadyWroteDebuffsStart, ref bool alreadyWroteSpeedDebuffsStart )
        {
            if ( alreadyWroteSpeedDebuffsStart )
            {
                Debuffer.Add( ", " );
            } else
            {
                WriteDebuffsStartIfNeeded( Debuffer, ref alreadyWroteDebuffsStart );
                Debuffer.WrapSpeed( "Speed: ", false, false );
                alreadyWroteSpeedDebuffsStart = true;
            }
        }

        public void WriteDecloakDebuffsStartIfNeeded( ArcenCharacterBufferBase Debuffer, bool useIcons, bool isAlreadyDecloaked, ref bool alreadyWroteDebuffsStart, ref bool alreadyWroteCloakDebuffsStart )
        {
            if ( alreadyWroteCloakDebuffsStart )
            {
                Debuffer.Add( ", " );
            } 
            else
            {
                WriteDebuffsStartIfNeeded( Debuffer, ref alreadyWroteDebuffsStart );
                if ( isAlreadyDecloaked )
                {
                    Debuffer.WrapCloak( "Already Decloaked!", false, false );
                } else
                {
                    Debuffer.WrapCloak( "Tachyon Radiation Decloaking Us", false, false );
                }
                alreadyWroteCloakDebuffsStart = true;
            }
        }

        public ShipClassData_ModifierData_Type LastGreaterCategoryType;
        public ShipClassData_LesserCategoryType LastLesserCategoryType;

        public void WriteShipClass_GreaterCategoryFinalEnd(ArcenCharacterBufferBase Buffer, ref bool AlreadyWroteStart )
        {
            if (!AlreadyWroteStart)
                return;
            
            Buffer.EndColor().EndSize();
            AlreadyWroteStart = false;
        }

        public void WriteShipClass_GreaterCategoryStartIfNeeded( ArcenCharacterBufferBase Buffer, ShipClassData ShipClass, ref bool AlreadyWroteStart,
            ref bool AlreadyWroteAbsoluteStart, TooltipDetail DetailLevel, ShipClassData_ModifierData_Type GreaterCategory )
        {
            WriteShipClass_AbsoluteStart( Buffer, ShipClass, DetailLevel, ref AlreadyWroteAbsoluteStart );

            if ( AlreadyWroteStart )
            {
                if (LastGreaterCategoryType != GreaterCategory)
                {
                    WriteShipClass_GreaterCategoryFinalEnd(Buffer, ref AlreadyWroteStart);
                    Buffer.Add("| ");
                }
                
                return;
            }
            
            //if 
            //else
            //    AlreadyWroteStart = true;

            LastGreaterCategoryType = GreaterCategory;

            if ( GreaterCategory == ShipClassData_ModifierData_Type.Vulnerability )
            {
                Buffer.StartColor( ArcenExternalUIUtilities.Debuffs.Color );
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( "Vulnerabilities: " );
                else if ( DetailLevel == TooltipDetail.Medium )
                    Buffer.Add( "Vul: " );
            }
            else if ( GreaterCategory == ShipClassData_ModifierData_Type.Resistance )
            {
                Buffer.StartColor( ArcenExternalUIUtilities.Buffs.Color );
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( "Resistances: " );
                else if ( DetailLevel == TooltipDetail.Medium )
                    Buffer.Add( "Res: " );
            }
            else if ( GreaterCategory == ShipClassData_ModifierData_Type.Immunity )
            {
                Buffer.StartColor( ArcenExternalUIUtilities.Immunities.Color );
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( "Immunities: " );
                else if ( DetailLevel == TooltipDetail.Medium )
                    Buffer.Add( "Imu: " );
            }
            else if ( GreaterCategory == ShipClassData_ModifierData_Type.Mixed )
            {
                Buffer.StartColor( ArcenExternalUIUtilities.Mixed.Color );
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( "Other: " );
                else if ( DetailLevel == TooltipDetail.Medium )
                    Buffer.Add( "Oth: " );
            }
            else
            {
                throw new Exception( "Error: Unimplemented ShipClassData_GreaterCategoryType: " + GreaterCategory );
            }
            Buffer.EndColor().AddSize_Small();
        }

        public void WriteShipClass_LesserCategoryFinalEnd( ArcenCharacterBufferBase Buffer, bool AlreadyWroteStart )
        {
            if (!AlreadyWroteStart)
                return;
            
            Buffer.Add("</color> ");
        }

        public void WriteShipClass_LesserCategoryStartIfNeeded( ArcenCharacterBufferBase Buffer, ref bool AlreadyWroteStart, TooltipDetail DetailLevel,
            ShipClassData_ModifierData_Type GreaterCategory, ShipClassData_LesserCategoryType LesserCategory, bool SkipLesserText )
        {
            if ( DetailLevel <= TooltipDetail.Medium || SkipLesserText )
                return;
            
            if ( AlreadyWroteStart )
            {
                if (LastLesserCategoryType != LesserCategory)
                {
                    WriteShipClass_LesserCategoryFinalEnd(Buffer, AlreadyWroteStart);
                    AlreadyWroteStart = false;
                    Buffer.Add("- ");
                } 
                else
                {
                    Buffer.Add("  ");
                }
            }

            LastLesserCategoryType = LesserCategory;

            if ( GreaterCategory == ShipClassData_ModifierData_Type.Vulnerability )
                Buffer.StartColor( ArcenExternalUIUtilities.DebuffsLight.Color );
            else 
            if( GreaterCategory == ShipClassData_ModifierData_Type.Mixed )
                Buffer.StartColor( ArcenExternalUIUtilities.MixedLight.Color );
            else 
            if( GreaterCategory == ShipClassData_ModifierData_Type.Resistance )
                Buffer.StartColor( ArcenExternalUIUtilities.BuffsLight.Color );
            else
                Buffer.StartColor( ArcenExternalUIUtilities.ImmunitiesLight.Color );
            
            if ( LesserCategory == ShipClassData_LesserCategoryType.Debuff )
            {
                Buffer.Add( "Debuffs: " );
            } 
            else 
            if ( LesserCategory == ShipClassData_LesserCategoryType.DeathEffect )
            {
                Buffer.Add( "Death Effects: " );
            } 
            else 
            if ( LesserCategory == ShipClassData_LesserCategoryType.AmmoType )
            {
                Buffer.Add( "Ammo Types: " );
            } 
            else 
            if ( LesserCategory == ShipClassData_LesserCategoryType.ExoticDamage )
            {
                Buffer.Add( "Exotic Damage: " );
            } 
            else 
            if ( LesserCategory == ShipClassData_LesserCategoryType.GeneralDamage )
            {
                Buffer.Add( "General Damage: " );
            } 
            else 
            if ( LesserCategory == ShipClassData_LesserCategoryType.AllDamage )
            {
                //there is no "all damage" category, only total damage immunity. No need for an overhead
            } 
            else 
            if ( LesserCategory == ShipClassData_LesserCategoryType.SpecialMechanic )
            {
                Buffer.Add( "Special Mechanic: " );
            } 
            else
            {
                throw new Exception( "Error: Unimplemented ShipClassData_LesserCategoryType: " + LesserCategory );
            }
            
            Buffer.EndColor();
        }

        public void WriteShipClass_ModifierData( ArcenCharacterBufferBase Buffer, ShipClassData ShipClass, string ModifierNameLong, string ModifierNameShort,
            ShipClassData_ModifierData Data, ShipClassData_ModifiedUnit DataType, TooltipDetail DetailLevel, ref bool AlreadyWroteLesserStart, ref bool AlreadyWroteGreaterStart, ref bool AlreadyWroteAbsoluteStart,
            ShipClassData_ModifierData_Type GreaterCategory, ShipClassData_LesserCategoryType LesserCategory, bool SkipLesserText )
        {
            WriteShipClass_GreaterCategoryStartIfNeeded( Buffer, ShipClass, ref AlreadyWroteGreaterStart, ref AlreadyWroteAbsoluteStart, DetailLevel, GreaterCategory );
            WriteShipClass_LesserCategoryStartIfNeeded( Buffer, ref AlreadyWroteLesserStart, DetailLevel, GreaterCategory, LesserCategory, SkipLesserText );

            if ( DetailLevel == TooltipDetail.Full )
                Buffer.Add( ModifierNameLong );
            else
                Buffer.Add( ModifierNameShort );
            
            if ( Data.ModifierType == ShipClassData_ModifierData_Type.Immunity || Data.ModifierType == ShipClassData_ModifierData_Type.NoModifier )
            {
                return;
            }

            if ( DataType == ShipClassData_ModifiedUnit.None )
            {
                Buffer.Add( " " );
            } else
            {
                if ( DetailLevel == TooltipDetail.Full )
                {
                    if ( DataType == ShipClassData_ModifiedUnit.TimeBased )
                        Buffer.Add( " duration " );
                    else
                        Buffer.Add( " damage " );
                }
                else if ( DetailLevel == TooltipDetail.Medium )
                {
                    if ( DataType == ShipClassData_ModifiedUnit.TimeBased )
                        Buffer.Add( " dur " );
                    else
                        Buffer.Add( " dmg " );
                }
            }

            if ( Data.Multiplier != FInt.One )
            {
                Buffer.Add( Data.Multiplier ).Add( "脳" );
                if ( Data.AddedCount != 0 )
                    Buffer.Add( ", " );
            }
            if ( Data.AddedCount != 0 )
            {
                if ( Data.AddedCount > 0 )
                    Buffer.Add( "+" );
                Buffer.Add( Data.AddedCount );
                if ( DataType == ShipClassData_ModifiedUnit.TimeBased )
                    Buffer.Add( "s" );
            }
        }

        public void WriteShipClass_GeneralCategory( ArcenCharacterBufferBase Buffer, ShipClassData ShipClass, int EntityBaseSpeed, TooltipDetail DetailLevel,
            ShipClassData_ModifierData_Type GreaterCategory, ref bool AlreadyWroteGeneralStart, ref bool AlreadyWroteAbsoluteStart )
        {
            bool AlreadyWroteLesserStart = false;
            ShipClassData_ModifierData data;

            if( !ShipClass.CanReceiveDebuffs && GreaterCategory == ShipClassData_ModifierData_Type.Immunity )
            {
                WriteShipClass_GreaterCategoryStartIfNeeded( Buffer, ShipClass, ref AlreadyWroteGeneralStart, ref AlreadyWroteAbsoluteStart, DetailLevel, GreaterCategory );
                WriteShipClass_LesserCategoryStartIfNeeded( Buffer, ref AlreadyWroteLesserStart, DetailLevel, GreaterCategory, ShipClassData_LesserCategoryType.Debuff, true );
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( "All Debuffs" );
                else
                    Buffer.Add( "Debuffs" );
            } 
            else 
            if(ShipClass.HasAnyDebuffModifiers)
            {
                data = ShipClass.DebuffModifiers[0];
                if ( data.ModifierType == GreaterCategory && EntityBaseSpeed > 0 )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "Engine Slow", "E Slow", data, ShipClassData_ModifiedUnit.TimeBased, DetailLevel, ref AlreadyWroteLesserStart, ref AlreadyWroteGeneralStart,
                        ref AlreadyWroteAbsoluteStart, GreaterCategory, ShipClassData_LesserCategoryType.Debuff, false );
                }
                data = ShipClass.DebuffModifiers[1];
                if ( data.ModifierType == GreaterCategory )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "Weapon Slow", "W Slow", data, ShipClassData_ModifiedUnit.TimeBased, DetailLevel, ref AlreadyWroteLesserStart, ref AlreadyWroteGeneralStart,
                        ref AlreadyWroteAbsoluteStart, GreaterCategory, ShipClassData_LesserCategoryType.Debuff, false );
                }
                data = ShipClass.DebuffModifiers[2];
                if ( data.ModifierType == GreaterCategory )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "Paralysis", "Stun", data, ShipClassData_ModifiedUnit.TimeBased, DetailLevel, ref AlreadyWroteLesserStart, ref AlreadyWroteGeneralStart,
                        ref AlreadyWroteAbsoluteStart, GreaterCategory, ShipClassData_LesserCategoryType.Debuff, false );
                }
                data = ShipClass.DebuffModifiers[3];
                if ( data.ModifierType == GreaterCategory )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "Acid", "Acid", data, ShipClassData_ModifiedUnit.TimeBased, DetailLevel, ref AlreadyWroteLesserStart, ref AlreadyWroteGeneralStart,
                        ref AlreadyWroteAbsoluteStart, GreaterCategory,ShipClassData_LesserCategoryType.Debuff, false );
                }
                data = ShipClass.DebuffModifiers[4];
                if ( data.ModifierType == GreaterCategory )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "Weapon Phasing", "Phase", data, ShipClassData_ModifiedUnit.TimeBased, DetailLevel, ref AlreadyWroteLesserStart, ref AlreadyWroteGeneralStart,
                        ref AlreadyWroteAbsoluteStart, GreaterCategory,ShipClassData_LesserCategoryType.Debuff, false );
                }
                data = ShipClass.DebuffModifiers[5];
                if ( data.ModifierType == GreaterCategory && EntityBaseSpeed > 0 )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "Knockback", "Knock", data, ShipClassData_ModifiedUnit.TimeBased, DetailLevel, ref AlreadyWroteLesserStart, ref AlreadyWroteGeneralStart,
                        ref AlreadyWroteAbsoluteStart, GreaterCategory,ShipClassData_LesserCategoryType.Debuff, false );
                }
                data = ShipClass.DebuffModifiers[6];
                if ( data.ModifierType == GreaterCategory )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "Tachyon Beams", "Tach", data, ShipClassData_ModifiedUnit.TimeBased, DetailLevel, ref AlreadyWroteLesserStart, ref AlreadyWroteGeneralStart,
                        ref AlreadyWroteAbsoluteStart, GreaterCategory,ShipClassData_LesserCategoryType.Debuff, false );
                }
                data = ShipClass.GraviticCoreModifier;
                if ( data.ModifierType == GreaterCategory && EntityBaseSpeed > 0 )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "Gravitic Cores", "Grav", data, ShipClassData_ModifiedUnit.None, DetailLevel, ref AlreadyWroteLesserStart, ref AlreadyWroteGeneralStart,
                        ref AlreadyWroteAbsoluteStart, GreaterCategory,ShipClassData_LesserCategoryType.Debuff, false );
                }
            }

            if ( !ShipClass.CanReceiveDeathEffects && GreaterCategory == ShipClassData_ModifierData_Type.Immunity )
            {
                WriteShipClass_GreaterCategoryStartIfNeeded( Buffer, ShipClass, ref AlreadyWroteGeneralStart, ref AlreadyWroteAbsoluteStart, DetailLevel, GreaterCategory );
                WriteShipClass_LesserCategoryStartIfNeeded( Buffer, ref AlreadyWroteLesserStart, DetailLevel, GreaterCategory, ShipClassData_LesserCategoryType.DeathEffect, true );
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( "All Death Effects" );
                else
                    Buffer.Add( "Death Effects" );
            } 
            /* Since a *lot* of things have zombification immunity being all they use from the ShipClass this is not displayed here but in the main tooltip of the unit
             * Less visual clutter this way

            else if ( !ShipClass.CanBeZombifiedAndSimilar && GreaterCategory == ShipClassData_ModifierData_Type.Immunity )
            {
                //Do not individually list zombification-type death effects for immunity, just that one info is sufficient
                WriteShipClass_GreaterCategoryStartIfNeeded( Buffer, ref AlreadyWroteGeneralStart, DetailLevel, GreaterCategory );
                WriteShipClass_LesserCategoryStartIfNeeded( Buffer, ref AlreadyWroteLesserStart, DetailLevel, GreaterCategory, ShipClassData_LesserCategoryType.DeathEffect, false );
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( "All Zombifying Types" );
                else
                    Buffer.Add( "Zombifying Types" );
            }*/
            if ( ShipClass.HasAnyDeathEffectModifiers )
            {
                DeathEffectType deathEffect;
                for ( int i = 0; i < ShipClass.DeathEffectModifiers.Length; i++ )
                {
                    data = ShipClass.DeathEffectModifiers[i];
                    deathEffect = DeathEffectTypeTable.Instance.Rows[i];

                    /* This code existed to filter out things already displayed in the "immune to all zombifying types" text
                    
                    if ( data.ModifierType == GreaterCategory &&
                        !(GreaterCategory == ShipClassData_ModifierData_Type.Immunity && deathEffect.IsSomeKindOfNormalZombification && !ShipClass.CanBeZombifiedAndSimilar) )*/

                    if( data.ModifierType == GreaterCategory && !(deathEffect.IsSomeKindOfNormalZombification && EntityBaseSpeed <= 0) )
                    {
                        //TODO Death Effect short names
                        WriteShipClass_ModifierData( Buffer, ShipClass, deathEffect.InternalName, deathEffect.InternalName, data, ShipClassData_ModifiedUnit.None, DetailLevel,
                            ref AlreadyWroteLesserStart, ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory,ShipClassData_LesserCategoryType.DeathEffect, false );
                    }
                }
            }

            if ( !ShipClass.CanBeDamaged )
            {
                if ( GreaterCategory == ShipClassData_ModifierData_Type.Immunity )
                {
                    WriteShipClass_GreaterCategoryStartIfNeeded( Buffer, ShipClass, ref AlreadyWroteGeneralStart, ref AlreadyWroteAbsoluteStart, DetailLevel, GreaterCategory );
                    WriteShipClass_LesserCategoryStartIfNeeded( Buffer, ref AlreadyWroteLesserStart, DetailLevel, GreaterCategory, ShipClassData_LesserCategoryType.GeneralDamage, true );
                    if ( DetailLevel == TooltipDetail.Full )
                        Buffer.Add( "Immune to all damage" );
                    else
                        Buffer.Add( "Invulnerable" );
                }
            } 
            else
            {
                if ( ShipClass.NonExoticDamageModifier.ModifierType == GreaterCategory )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "All Shots", "All", ShipClass.NonExoticDamageModifier, ShipClassData_ModifiedUnit.None, DetailLevel,
                        ref AlreadyWroteLesserStart, ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory,ShipClassData_LesserCategoryType.AmmoType, false );
                }
                if ( ShipClass.HasAnyAmmoDamageModifiers )
                {
                    for ( int i = 0; i < ShipClass.AmmoDamageModifiers.Length; i++ )
                    {
                        data = ShipClass.AmmoDamageModifiers[i];
                        AmmoTypeData ammo = AmmoTypeDataTable.Instance.Rows[i];
                        if ( data.ModifierType == GreaterCategory )
                        {
                            //TODO Ammo Type short names
                            WriteShipClass_ModifierData( Buffer, ShipClass, ammo.DisplayName, ammo.DisplayName, data, ShipClassData_ModifiedUnit.None, DetailLevel,
                                ref AlreadyWroteLesserStart, ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory,ShipClassData_LesserCategoryType.AmmoType, false );
                        }
                    }
                }

                if ( ShipClass.AllExoticDamageModifier.ModifierType == GreaterCategory )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "All Exotic Damage", "Exotic Damage", ShipClass.AllExoticDamageModifier, ShipClassData_ModifiedUnit.None,
                        DetailLevel, ref AlreadyWroteLesserStart, ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory, ShipClassData_LesserCategoryType.ExoticDamage, true );
                } 
                else 
                if ( ShipClass.HasAnyExoticDamageModifiers )
                {
                    data = ShipClass.ExoticDamageModifiers[0];
                    if ( data.ModifierType == GreaterCategory && EntityBaseSpeed > 0 )
                    {
                        WriteShipClass_ModifierData( Buffer, ShipClass, "Attrition", "Attr", data, ShipClassData_ModifiedUnit.None, DetailLevel,
                            ref AlreadyWroteLesserStart, ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory,ShipClassData_LesserCategoryType.ExoticDamage, false );
                    }
                    data = ShipClass.ExoticDamageModifiers[1];
                    if ( data.ModifierType == GreaterCategory )
                    {
                        WriteShipClass_ModifierData( Buffer, ShipClass, "Electrotoxicity", "ETox", data, ShipClassData_ModifiedUnit.None, DetailLevel,
                            ref AlreadyWroteLesserStart, ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory,ShipClassData_LesserCategoryType.ExoticDamage, false );
                    }
                    data = ShipClass.ExoticDamageModifiers[2];
                    if ( data.ModifierType == GreaterCategory )
                    {
                        WriteShipClass_ModifierData( Buffer, ShipClass, "Revenge Shots", "Veng", data, ShipClassData_ModifiedUnit.None, DetailLevel,
                            ref AlreadyWroteLesserStart, ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory,ShipClassData_LesserCategoryType.ExoticDamage, false );
                    }
                    data = ShipClass.ExoticDamageModifiers[3];
                    if ( data.ModifierType == GreaterCategory )
                    {
                        WriteShipClass_ModifierData( Buffer, ShipClass, "Ion Cannon", "Ion", data, ShipClassData_ModifiedUnit.None, DetailLevel,
                            ref AlreadyWroteLesserStart, ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory,ShipClassData_LesserCategoryType.ExoticDamage, false );
                    }
                }
            }

            if (GreaterCategory == ShipClassData_ModifierData_Type.Immunity)
            {
                if ( !ShipClass.CanBeSubjectedToSpecialMechanics )
                {
                    WriteShipClass_GreaterCategoryStartIfNeeded( Buffer, ShipClass, ref AlreadyWroteGeneralStart, ref AlreadyWroteAbsoluteStart, DetailLevel, GreaterCategory );
                    WriteShipClass_LesserCategoryStartIfNeeded( Buffer, ref AlreadyWroteLesserStart, DetailLevel, GreaterCategory, ShipClassData_LesserCategoryType.SpecialMechanic, true );
                    if ( DetailLevel == TooltipDetail.Full )
                        Buffer.Add( "Tractor Beams  Black Hole Machines  Getting Devoured  Getting Infested" );
                    else
                        Buffer.Add( "All Special Mechanics" );
                } else
                {
                    if ( !(ShipClass.CanBeTractored && ShipClass.CanBeBlackHoleMachineBlocked && ShipClass.CanBeDevoured && ShipClass.CanBeInfested) )
                    {
                        WriteShipClass_GreaterCategoryStartIfNeeded( Buffer, ShipClass, ref AlreadyWroteGeneralStart, ref AlreadyWroteAbsoluteStart, DetailLevel, GreaterCategory );
                        WriteShipClass_LesserCategoryStartIfNeeded( Buffer, ref AlreadyWroteLesserStart, DetailLevel, GreaterCategory, ShipClassData_LesserCategoryType.SpecialMechanic,
                            false );
                        if ( !ShipClass.CanBeTractored )
                        {
                            if(DetailLevel == TooltipDetail.Full)
                                Buffer.Add( "Tractor Beams " );
                            else
                                Buffer.Add( "Tractors " );
                            if ( !ShipClass.CanBeBlackHoleMachineBlocked )
                                Buffer.Add( " " );
                        }
                        if ( !ShipClass.CanBeBlackHoleMachineBlocked )
                        {
                            if ( DetailLevel == TooltipDetail.Full )
                                Buffer.Add( "Black Hole Machines " );
                            else
                                Buffer.Add( "Black Holes " );
                            if ( !ShipClass.CanBeDevoured )
                                Buffer.Add( " " );
                        }
                        if ( !ShipClass.CanBeDevoured )
                            if ( DetailLevel == TooltipDetail.Full )
                                Buffer.Add( "Getting Devoured " );
                            else
                                Buffer.Add( "Devouring " );
                        if ( !ShipClass.CanBeInfested )
                            if ( DetailLevel == TooltipDetail.Full )
                                Buffer.Add( "Getting Infested " );
                            else
                                Buffer.Add( "Infestation " );
                    }
                }
            }

            WriteShipClass_LesserCategoryFinalEnd(Buffer, AlreadyWroteLesserStart);
        }

        public void WriteShipClass_BuffLimitStartIfNeeded( ArcenCharacterBufferBase Buffer, ref bool AlreadyWroteStart,
            ref bool AlreadyWroteAbsoluteStart, ShipClassData ShipClass, TooltipDetail DetailLevel, bool SkipInitialStart )
        {
            if ( AlreadyWroteStart )
                return;

            AlreadyWroteStart = true;
            if ( AlreadyWroteAbsoluteStart )
                Buffer.Add( "| " );
            else
                WriteShipClass_AbsoluteStart( Buffer, ShipClass, DetailLevel, ref AlreadyWroteAbsoluteStart );
            if ( !SkipInitialStart )
                Buffer.StartColor( ArcenExternalUIUtilities.Limits.Color ).Add( "Buff Limits:" ).EndColor().StartColor( ArcenExternalUIUtilities.LimitsLight.Color ).AddSize_Small();
        }

        public void WriteShipClass_BuffLimitEndIfNeeded( ArcenCharacterBufferBase Buffer, bool AlreadyWroteStart )
        {
            if ( AlreadyWroteStart )
                Buffer.EndColor();
        }

        public void WriteShipClass_SuperchargingStartIfNeeded( ArcenCharacterBufferBase Buffer, ref bool AlreadyWroteStart, ref bool WrotePreviousStart, bool SkipInitialStart )
        {
            if ( AlreadyWroteStart )
                return;

            AlreadyWroteStart = true;
            if ( WrotePreviousStart )
            {
                WriteShipClass_BuffLimitOrSuperchargeEndIfNeeded( Buffer, ref WrotePreviousStart );
                Buffer.Add( " |" );
            }
            if ( !SkipInitialStart )
                Buffer.StartColor( ArcenExternalUIUtilities.Limits.Color ).Add( " Cannot Supercharge:" ).EndColor().StartColor( ArcenExternalUIUtilities.LimitsLight.Color ).AddSize_Small();
        }

        public void WriteShipClass_BuffLimitOrSuperchargeEndIfNeeded( ArcenCharacterBufferBase Buffer, ref bool AlreadyWroteStart )
        {
            if ( !AlreadyWroteStart )
                return;
            Buffer.EndSize().EndColor();
        }

        public void WriteShipClass_BuffLimitIfNeeded( ArcenCharacterBufferBase Buffer, ShipClassData ShipClass, ref bool AlreadyWroteAbsoluteStart, ref bool AlreadyWroteStart, TooltipDetail DetailLevel,
            ShipClassData_BuffType BuffType, FInt BuffLimit_Default, FInt BuffLimit_Current, int SpeedBuffLimit_Default = 0, int SpeedBuffLimit_Current = 0, int EntityBaseSpeed = 0)
        {
            if ( BuffLimit_Default == BuffLimit_Current && SpeedBuffLimit_Default == SpeedBuffLimit_Current )
                return;

            WriteShipClass_BuffLimitStartIfNeeded( Buffer, ref AlreadyWroteStart, ref AlreadyWroteAbsoluteStart, ShipClass, DetailLevel, false );
            if ( BuffType == ShipClassData_BuffType.Damage )
            {
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( " Damage: " );
                else
                    Buffer.Add( " Dmg " );
                Buffer.AddFIntTruncated( BuffLimit_Current ).Add("x ");
            } else if( BuffType == ShipClassData_BuffType.Hull )
            {
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( " Hull: " );
                else
                    Buffer.Add( " Hull " );
                Buffer.AddFIntTruncated( BuffLimit_Current ).Add( "x " );
            } else if( BuffType == ShipClassData_BuffType.Shield )
            {
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( " Shield: " );
                else
                    Buffer.Add( " Shd " );
                Buffer.AddFIntTruncated( BuffLimit_Current ).Add( "x " );
            } else if( BuffType == ShipClassData_BuffType.Speed )
            {
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( " Speed: " );
                else
                    Buffer.Add( " Spd " );
                Buffer.AddNumberMoreReadable( ShipClass.GetMaxSpeedForShip( EntityBaseSpeed ) );
            }
            WriteShipClass_BuffLimitEndIfNeeded( Buffer, AlreadyWroteStart );
        }

        public void WriteShipClass_BuffLimitsAndSuperchargingSection( ArcenCharacterBufferBase Buffer, ShipClassData ShipClass, ref bool AlreadyWroteAbsoluteStart,
            TooltipDetail DetailLevel, FactionType ForFactionType, int EntityBaseSpeed )
        {

            bool alreadyWroteBuffLimitStart = false;
            if ( !ShipClass.CanBeBuffed )
            {
                WriteShipClass_BuffLimitStartIfNeeded( Buffer, ref alreadyWroteBuffLimitStart, ref AlreadyWroteAbsoluteStart, ShipClass, DetailLevel, true );
                Buffer.StartColor( ArcenExternalUIUtilities.Limits.Color ).Add( " Cannot be buffed. " ).EndColor();
            } else
            {
                ShipClassData defaultHull = ShipClassDataTable.Instance.DefaultRow;
                WriteShipClass_BuffLimitIfNeeded( Buffer, ShipClass, ref alreadyWroteBuffLimitStart, ref AlreadyWroteAbsoluteStart, DetailLevel, ShipClassData_BuffType.Damage, defaultHull.DamageBuff_MaxMultiplier,
                    ShipClass.DamageBuff_MaxMultiplier );
                WriteShipClass_BuffLimitIfNeeded( Buffer, ShipClass, ref alreadyWroteBuffLimitStart, ref AlreadyWroteAbsoluteStart, DetailLevel, ShipClassData_BuffType.Hull, defaultHull.HullBuff_MaxMultiplier,
                    ShipClass.HullBuff_MaxMultiplier );
                WriteShipClass_BuffLimitIfNeeded( Buffer, ShipClass, ref alreadyWroteBuffLimitStart, ref AlreadyWroteAbsoluteStart, DetailLevel, ShipClassData_BuffType.Shield, defaultHull.ShieldBuff_MaxMultiplier,
                    ShipClass.ShieldBuff_MaxMultiplier );
                if ( EntityBaseSpeed > 0 )
                    WriteShipClass_BuffLimitIfNeeded( Buffer, ShipClass, ref alreadyWroteBuffLimitStart, ref AlreadyWroteAbsoluteStart, DetailLevel, ShipClassData_BuffType.Speed, defaultHull.SpeedBuff_MaxMultiplier,
                        ShipClass.SpeedBuff_MaxMultiplier, defaultHull.SpeedBuff_BaseBonus, ShipClass.SpeedBuff_BaseBonus, EntityBaseSpeed );
            }

            bool alreadyWroteSuperchargeLimitStart = false;
            if ( ForFactionType == FactionType.Player )
            {
                if ( !ShipClass.CanBeFleetSupercharged )
                {
                    //Similar to how zombification/nanocaustation immunity is not in here this is also not in the ship class row, for its overabundance
                    //WriteShipClass_SuperchargingStartIfNeeded( Buffer, ref alreadyWroteSuperchargeLimitStart, ref AlreadyWroteAbsoluteStart, true );
                    //Buffer.Add( ArcenExternalUIUtilities.Limits.ColorStart ).Add( " Cannot be supercharged. " ).EndColor();
                } else
                {
                    if ( ShipClass.DamageBuff_MaxMultiplier > 1 && !ShipClass.CanBeSuperchargedByFleet( ShipClassData_BuffType.Damage ) )
                    {
                        WriteShipClass_SuperchargingStartIfNeeded( Buffer, ref alreadyWroteSuperchargeLimitStart, ref alreadyWroteBuffLimitStart, false );
                        Buffer.Add( " Damage " );
                    }
                    if ( ShipClass.HullBuff_MaxMultiplier > 1 && !ShipClass.CanBeSuperchargedByFleet( ShipClassData_BuffType.Hull ) )
                    {
                        WriteShipClass_SuperchargingStartIfNeeded( Buffer, ref alreadyWroteSuperchargeLimitStart, ref alreadyWroteBuffLimitStart, false );
                        Buffer.Add( " Hull " );
                    }
                    if ( ShipClass.ShieldBuff_MaxMultiplier > 1 && !ShipClass.CanBeSuperchargedByFleet( ShipClassData_BuffType.Shield ) )
                    {
                        WriteShipClass_SuperchargingStartIfNeeded( Buffer, ref alreadyWroteSuperchargeLimitStart, ref alreadyWroteBuffLimitStart, false );
                        Buffer.Add( " Shield " );
                    }
                    if ( EntityBaseSpeed > 0 && (ShipClass.SpeedBuff_MaxMultiplier > 1 || ShipClass.SpeedBuff_BaseBonus > 0) &&
                        !ShipClass.CanBeSuperchargedByFleet( ShipClassData_BuffType.Speed ) )
                    {
                        WriteShipClass_SuperchargingStartIfNeeded( Buffer, ref alreadyWroteSuperchargeLimitStart, ref alreadyWroteBuffLimitStart, false );
                        Buffer.Add( " Speed " );
                    }
                }
            }

            bool anyEndNeeded = alreadyWroteBuffLimitStart || alreadyWroteSuperchargeLimitStart;
            WriteShipClass_BuffLimitOrSuperchargeEndIfNeeded( Buffer, ref anyEndNeeded );
        }

        public void WriteShipClass_AbsoluteStart( ArcenCharacterBufferBase Buffer, ShipClassData ShipClass, TooltipDetail DetailLevel, ref bool AlreadyWroteAbsoluteStart )
        {
            if ( AlreadyWroteAbsoluteStart )
                return;
            AlreadyWroteAbsoluteStart = true;

            Buffer.Add( "\n<b>" ).StartColor( "fa7bff" );
            if ( DetailLevel >= TooltipDetail.Medium )
                Buffer.Add( ShipClass.DisplayName );
            else
            {
                Buffer.Add( ShipClass.ShortDisplayName ).Add( "</color></b>" );
                return;
            }
            Buffer.Add( ":</color></b> " );
        }

        public void WriteShipClass_Complete( ArcenCharacterBufferBase Buffer, ShipClassData ShipClass, TooltipDetail DetailLevel, FactionType ForFactionType,
            int EntityBaseSpeed )
        {
            if ( ShipClass.IsDefault || !ShipClass.RequiresTooltipAtAll )
                return;

            //if ( DetailLevel == TooltipDetail.Full && !string.IsNullOrEmpty( ShipClass.OverrideDescriptionTextLongTooltip ) )
            //{
            //    Buffer.StartColor( "999999" ).Add( ShipClass.OverrideDescriptionTextLongTooltip ).Add( " " ).EndColor();
            //    return;
            //}
            //if ( DetailLevel == TooltipDetail.Medium && !string.IsNullOrEmpty( ShipClass.OverrideDescriptionTextMediumTooltip ) )
            //{
            //    Buffer.StartColor( "999999" ).Add( ShipClass.OverrideDescriptionTextMediumTooltip ).Add( " " ).EndColor();
            //    return;
            //}
            //if ( DetailLevel == TooltipDetail.SuperShort && !string.IsNullOrEmpty( ShipClass.OverrideDescriptionTextShortTooltip ) )
            //{
            //    Buffer.StartColor( "999999" ).Add( ShipClass.OverrideDescriptionTextShortTooltip ).Add(" ").EndColor();
            //    return;
            //}

            bool AlreadyWroteAbsoluteStart = false;
            bool AlreadyWroteGreaterStart = false;

            WriteShipClass_GeneralCategory( Buffer, ShipClass, EntityBaseSpeed, DetailLevel, ShipClassData_ModifierData_Type.Vulnerability, ref AlreadyWroteGreaterStart, ref AlreadyWroteAbsoluteStart );

            WriteShipClass_GeneralCategory( Buffer, ShipClass, EntityBaseSpeed, DetailLevel, ShipClassData_ModifierData_Type.Mixed, ref AlreadyWroteGreaterStart, ref AlreadyWroteAbsoluteStart );

            WriteShipClass_GeneralCategory( Buffer, ShipClass, EntityBaseSpeed, DetailLevel, ShipClassData_ModifierData_Type.Resistance, ref AlreadyWroteGreaterStart, ref AlreadyWroteAbsoluteStart );

            WriteShipClass_GeneralCategory( Buffer, ShipClass, EntityBaseSpeed, DetailLevel, ShipClassData_ModifierData_Type.Immunity, ref AlreadyWroteGreaterStart, ref AlreadyWroteAbsoluteStart );

            WriteShipClass_GreaterCategoryFinalEnd( Buffer, ref AlreadyWroteGreaterStart );

            WriteShipClass_BuffLimitsAndSuperchargingSection( Buffer, ShipClass, ref AlreadyWroteAbsoluteStart, DetailLevel, ForFactionType, EntityBaseSpeed );
        }

        #endregion
        
        #region Unorganized Crap
        
        private static readonly SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark> weakAgainst_Ships_ThatYouHave =
            SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark>.Create_WillNeverBeGCed( 30, delegate { return new ShipDataByMark(); }, "Window_InGameHoverEntityInfo-weakAgainst_Ships_ThatYouHave" );

        private static readonly SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataForSingleMark> weakAgainst_Ships_ThatYouCanCapture =
            SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataForSingleMark>.Create_WillNeverBeGCed( 30, delegate { return new ShipDataForSingleMark(); }, "Window_InGameHoverEntityInfo-weakAgainst_Ships_ThatYouCanCapture" );

        private static readonly SortedDictionary<GameEntityTypeData, RefThreeTuple<string, int, FInt>> weakAgainst_multipliersDealtByShipName =
            SortedDictionary<GameEntityTypeData, RefThreeTuple<string, int, FInt>>.Create_WillNeverBeGCed( 300, "Window_InGameHoverEntityInfo-weakAgainst_multipliersDealtByShipName" );
        
        private static readonly SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark> strongAgainst_Ships_EnemiesHave =
            SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark>.Create_WillNeverBeGCed( 30, delegate { return new ShipDataByMark(); }, "Window_InGameHoverEntityInfo-strongAgainst_Ships_EnemiesHave" );

        private static readonly SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark> strongAgainst_Ships_EnemiesAtThisPlanet =
            SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark>.Create_WillNeverBeGCed( 30, delegate { return new ShipDataByMark(); }, "Window_InGameHoverEntityInfo-strongAgainst_Ships_EnemiesAtThisPlanet" );

        private static readonly SortedDictionary<GameEntityTypeData, RefThreeTuple<string, int, FInt>> strong_multipliersDealtByShipName =
            SortedDictionary<GameEntityTypeData, RefThreeTuple<string, int, FInt>>.Create_WillNeverBeGCed( 300, "Window_InGameHoverEntityInfo-strong_multipliersDealtByShipName" );
        
        private static FInt CalculateMultipleOfMultiplier( DamageModifier playerShipDamageModifier, GameEntityTypeData ShipTypeData, byte mark )
        {
            if ( playerShipDamageModifier.NeedsToGetMultiples )
            {
                FInt multiples;
                multiples = playerShipDamageModifier.GetMultiplierForResourceForType( ShipTypeData, mark ) * playerShipDamageModifier.MultiplierForMark[mark];
                if ( playerShipDamageModifier.MaxMultiplier != int.MaxValue && multiples > playerShipDamageModifier.MaxMultiplier )
                    multiples = FInt.Create( playerShipDamageModifier.MaxMultiplier, true );
                if ( playerShipDamageModifier.MultiplierIsAdditive )
                    multiples += FInt.One; //add 1x base damage

                return multiples;
            }
            return FInt.One;
        }

        private static void WriteWeakAgainst( ArcenCharacterBufferBase buffer, GameEntityTypeData enemyShipTypeData, GameEntityTypeData.MarkLevelStats enemyShipMarkStats )
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;
                
                #region Weak against
                debugStage = 200;

                ShipListerUtils.CalculateShipsThatYouHave( ((x) => true), weakAgainst_Ships_ThatYouHave, false, true );
                debugStage = 300;
                ShipListerUtils.CalculateShipsThatYouCanCapture( ((x) => true), weakAgainst_Ships_ThatYouCanCapture, null, false );

                int comparisonInt = 0;
                FInt comparisonFInt = FInt.Zero;
                bool forceDoesNotMeetCriteria = false;

                debugStage = 400;
                buffer.Add( "<color=#ff7150>\nYour ships have the following damage multipliers against this unit:</color>\n" );

                bool wroteAny = false;
                foreach ( KeyValuePair<GameEntityTypeData, ShipDataByMark> pair in weakAgainst_Ships_ThatYouHave )
                {
                    debugStage = 500;
                    GameEntityTypeData playerShipType = pair.Key;
                    byte maxMarkLevel = 0;
                    int countAcrossAllMarks = 0;
                    int singleLineCount = 0;
                    for ( byte i = 0; i < pair.Value.CountsByMarkLength(); i++ )
                    {
                        singleLineCount = pair.Value.GetCountByMark( i );
                        if ( singleLineCount > 0 )
                        {
                            maxMarkLevel = i;
                            countAcrossAllMarks += singleLineCount;
                        }
                    }
                    if ( countAcrossAllMarks <= 0 )
                        continue;

                    debugStage = 600;
                    for ( int j = 0; j < playerShipType.SystemTypes.Count; j++ )
                    {
                        debugStage = 700;
                        EntitySystemTypeData systemData = playerShipType.SystemTypes[j];
                        for ( int k = 0; k < systemData.OutgoingDamageModifiers_FullList.Count; k++ )
                        {
                            debugStage = 800;
                            DamageModifier playerShipDamageModifier = systemData.OutgoingDamageModifiers_FullList[k];
                            debugStage = 900;
                            if ( playerShipDamageModifier.CalculateDoesMeetCriteriaStatic( enemyShipTypeData, enemyShipMarkStats, ref comparisonInt, ref comparisonFInt, ref forceDoesNotMeetCriteria )
                                || playerShipDamageModifier.CalculateDoesMeetCriteriaTypeOnly( enemyShipTypeData, enemyShipMarkStats, ref comparisonInt, ref comparisonFInt, ref forceDoesNotMeetCriteria ) )
                            {
                                debugStage = 1000;
                                if ( !forceDoesNotMeetCriteria && playerShipDamageModifier.CompareWithValues( comparisonInt, comparisonFInt ) )
                                {
                                    debugStage = 1100;
                                    wroteAny = true;
                                    debugStage = 1200;
                                    GameEntityTypeData.MarkLevelStats shipMarkStats = playerShipType.MarkStatsFor( maxMarkLevel );
                                    RefThreeTuple<string, int, FInt> multiplier = null;
                                    debugStage = 1300;
                                    if ( !weakAgainst_multipliersDealtByShipName.TryGetValue( playerShipType, out multiplier ) )
                                    {
                                        debugStage = 1400;
                                        multiplier = RefThreeTuple<string, int, FInt>.Create(
                                            $"{playerShipType.DisplayName} <color=#{shipMarkStats.MarkLevel.ColorHex}>{shipMarkStats.MarkLevel.Abbreviation}</color>",
                                            0, FInt.Zero );
                                        weakAgainst_multipliersDealtByShipName[playerShipType] = multiplier;
                                    }
                                    debugStage = 1500;

                                    multiplier.SecondItem += countAcrossAllMarks;

                                    if ( multiplier.ThirdItem == FInt.Zero )
                                        multiplier.ThirdItem = playerShipDamageModifier.MultiplierForMark[maxMarkLevel];
                                    else
                                        multiplier.ThirdItem *= playerShipDamageModifier.MultiplierForMark[maxMarkLevel];
                                    multiplier.ThirdItem *= CalculateMultipleOfMultiplier( playerShipDamageModifier, enemyShipTypeData, maxMarkLevel );
                                }
                            }
                        }
                    }
                }

                debugStage = 3000;
                if ( !wroteAny )
                    buffer.Add( "None" );

                debugStage = 3100;
                WriteMultipliersDealtByShipName( buffer, null, weakAgainst_multipliersDealtByShipName );
                buffer.Add( "\n" );

                debugStage = 3200;
                buffer.Add( "<color=#ffe450>\nThese ships you could capture have the following damage multipliers against this unit:</color>\n" );

                wroteAny = false;

                debugStage = 4000;
                weakAgainst_multipliersDealtByShipName.Clear();
                debugStage = 4100;
                foreach ( KeyValuePair<GameEntityTypeData, ShipDataForSingleMark> pair in weakAgainst_Ships_ThatYouCanCapture )
                {
                    GameEntityTypeData shipType = pair.Key;
                    bool usesCaps = ShipListerUtils.GetUsesShipCaps( shipType );
                    debugStage = 4200;
                    for ( int j = 0; j < shipType.SystemTypes.Count; j++ )
                    {
                        debugStage = 4300;
                        EntitySystemTypeData systemData = shipType.SystemTypes[j];
                        for ( int k = 0; k < systemData.OutgoingDamageModifiers_FullList.Count; k++ )
                        {
                            debugStage = 4400;
                            DamageModifier damageModifier = systemData.OutgoingDamageModifiers_FullList[k];
                            debugStage = 4500;
                            if ( damageModifier.CalculateDoesMeetCriteriaStatic( enemyShipTypeData, enemyShipMarkStats, ref comparisonInt, ref comparisonFInt, ref forceDoesNotMeetCriteria )
                                || damageModifier.CalculateDoesMeetCriteriaTypeOnly( enemyShipTypeData, enemyShipMarkStats, ref comparisonInt, ref comparisonFInt, ref forceDoesNotMeetCriteria ) )
                            {
                                debugStage = 4600;
                                if ( !forceDoesNotMeetCriteria && damageModifier.CompareWithValues( comparisonInt, comparisonFInt ) )
                                {
                                    debugStage = 4700;
                                    wroteAny = true;
                                    byte maxMarkLevel = pair.Value.MarkLevel;
                                    debugStage = 4800;
                                    GameEntityTypeData.MarkLevelStats shipMarkStats = shipType.MarkStatsFor( maxMarkLevel );
                                    debugStage = 4900;
                                    RefThreeTuple<string, int, FInt> multiplier = null;
                                    debugStage = 5000;
                                    if ( !weakAgainst_multipliersDealtByShipName.TryGetValue( shipType, out multiplier ) )
                                    {
                                        debugStage = 5100;
                                        multiplier = RefThreeTuple<string, int, FInt>.Create(
                                            $"{shipType.DisplayName} <color=#{shipMarkStats.MarkLevel.ColorHex}>{shipMarkStats.MarkLevel.Abbreviation}</color>",
                                            0, FInt.Zero );
                                        debugStage = 5200;
                                        weakAgainst_multipliersDealtByShipName[shipType] = multiplier;
                                    }
                                    debugStage = 5300;

                                    int shipCount = pair.Value.GetCountToCapture();
                                    if ( shipCount > 0 && usesCaps )
                                        shipCount = shipType.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( shipType, shipCount, shipCount, maxMarkLevel );

                                    multiplier.SecondItem += shipCount;

                                    if ( multiplier.ThirdItem == FInt.Zero )
                                        multiplier.ThirdItem = damageModifier.MultiplierForMark[maxMarkLevel];
                                    else
                                        multiplier.ThirdItem *= damageModifier.MultiplierForMark[maxMarkLevel];
                                    multiplier.ThirdItem *= CalculateMultipleOfMultiplier( damageModifier, enemyShipTypeData, maxMarkLevel );
                                }
                            }
                        }
                    }
                }

                debugStage = 6000;
                if ( !wroteAny )
                    buffer.Add( "None" );

                debugStage = 6100;
                WriteMultipliersDealtByShipName( buffer, null, weakAgainst_multipliersDealtByShipName );
                buffer.Add( "\n" );

                #endregion
            } 
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "WriteWeakAgainst exception at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }
        }

        private static void WriteStrongAgainst( ArcenCharacterBufferBase buffer, GameEntityTypeData playerShipTypeData, Planet planetBeingViewedOrNull )
        {
            ShipListerUtils.CalculateKnownShipsThatEnemiesHave( strongAgainst_Ships_EnemiesHave, x => true,
                x => true, x => true, false );
            //ShipListerUtils.CalculateKnownShipsThatEnemiesHave( strongAgainst_Ships_EnemiesAtThisPlanet, x => (planetBeingViewedOrNull == null || x.Planet == planetBeingViewedOrNull),
            //    x => true, x => true, false, strongAgainst_Counters_EnemiesAtThisPlanet );
            strong_multipliersDealtByShipName.Clear();

            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();

            int comparisonInt = 0;
            FInt comparisonFInt = FInt.Zero;
            bool forceDoesNotMeetCriteria = false;

            buffer.Add( "<color=#50abff>\nHas the following damage multipliers against enemy ships:</color>\n" );

            bool wroteAny = false;
            foreach ( KeyValuePair<GameEntityTypeData, ShipDataByMark> pair in strongAgainst_Ships_EnemiesHave )
            {
                GameEntityTypeData enemyShipType = pair.Key;

                byte enemyShipMaxMarkLevelFound = 0;
                int countAcrossAllMarks = 0;
                int singleLineCount = 0;
                for ( byte i = 0; i < pair.Value.CountsByMarkLength(); i++ )
                {
                    singleLineCount = pair.Value.GetCountByMark( i );
                    if ( singleLineCount > 0 )
                    {
                        enemyShipMaxMarkLevelFound = i;
                        countAcrossAllMarks += singleLineCount;
                    }
                }
                if ( countAcrossAllMarks <= 0 )
                    continue;

                GameEntityTypeData.MarkLevelStats enemyShipMarkStats = enemyShipType.MarkStatsFor( enemyShipMaxMarkLevelFound );
                for ( int j = 0; j < playerShipTypeData.SystemTypes.Count; j++ )
                {
                    //Debug.Log($"Strong against: comparing human {playerShipTypeData.DisplayName} with enemy {enemyShipType}");
                    EntitySystemTypeData playerShipSystemData = playerShipTypeData.SystemTypes[j];
                    for ( int k = 0; k < playerShipSystemData.OutgoingDamageModifiers_FullList.Count; k++ )
                    {
                        DamageModifier playerShipDamageModifier = playerShipSystemData.OutgoingDamageModifiers_FullList[k];
                        if ( playerShipDamageModifier.CalculateDoesMeetCriteriaStatic( enemyShipType, enemyShipMarkStats, ref comparisonInt, ref comparisonFInt, ref forceDoesNotMeetCriteria )
                            || playerShipDamageModifier.CalculateDoesMeetCriteriaTypeOnly( enemyShipType, enemyShipMarkStats, ref comparisonInt, ref comparisonFInt, ref forceDoesNotMeetCriteria ) )
                        {
                            if ( !forceDoesNotMeetCriteria &&
                                playerShipDamageModifier.CompareWithValues( comparisonInt, comparisonFInt ) )
                            {
                                wroteAny = true;

                                RefThreeTuple<string, int, FInt> multiplier = null;
                                if ( !strong_multipliersDealtByShipName.TryGetValue( enemyShipType, out multiplier ) )
                                {
                                    multiplier = RefThreeTuple<string, int, FInt>.Create(
                                        $"{enemyShipType.DisplayName} <color=#{enemyShipMarkStats.MarkLevel.ColorHex}>{enemyShipMarkStats.MarkLevel.Abbreviation}</color>",
                                        0, FInt.Zero );
                                    strong_multipliersDealtByShipName[enemyShipType] = multiplier;
                                }

                                multiplier.SecondItem += countAcrossAllMarks;

                                byte playerMark = 0;
                                if ( playerFaction != null )
                                    playerMark = playerFaction.GetGlobalMarkLevelForShipLine( playerShipTypeData );

                                if ( multiplier.ThirdItem == FInt.Zero )
                                    multiplier.ThirdItem = playerShipDamageModifier.MultiplierForMark[playerMark];
                                else
                                    multiplier.ThirdItem *= playerShipDamageModifier.MultiplierForMark[playerMark];
                                multiplier.ThirdItem *= CalculateMultipleOfMultiplier( playerShipDamageModifier, enemyShipType, playerMark );
                            }
                        }
                    }
                }
            }

            if ( !wroteAny )
                buffer.Add( "None" );

            WriteMultipliersDealtByShipName( buffer, null, strong_multipliersDealtByShipName );
            buffer.Add( "\n" );
        }

        private static void WriteMultipliersDealtByShipName( ArcenCharacterBufferBase buffer,
            SortedDictionary<GameEntityTypeData, RefThreeTuple<string, int, FInt>> multipliersDealtByShip_LocalPlanetOrNull,
            SortedDictionary<GameEntityTypeData, RefThreeTuple<string, int, FInt>> multipliersDealtByShip_GeneralOrNull )
        {
            if ( multipliersDealtByShip_LocalPlanetOrNull != null )
            {
                multipliersDealtByShip_LocalPlanetOrNull.SortIntoList( delegate ( KeyValuePair<GameEntityTypeData, RefThreeTuple<string, int, FInt>> Left,
                    KeyValuePair<GameEntityTypeData, RefThreeTuple<string, int, FInt>> Right )
                {
                    //desceding by bonus amount
                    int val = Right.Value.ThirdItem.CompareTo( Left.Value.ThirdItem );
                    if ( val != 0 )
                        return val;
                    //if same bonus amount, then by name
                    return Left.Key.DisplayName.CompareTo( Right.Key.DisplayName );
                } );

                int count = 0;
                foreach ( KeyValuePair<GameEntityTypeData, RefThreeTuple<string, int, FInt>> kv in multipliersDealtByShip_LocalPlanetOrNull )
                {
                    string name = kv.Value.SecondItem + " " + kv.Value.FirstItem;
                    FInt value = kv.Value.ThirdItem;
                    buffer.Add( $"<b><size=120%>{name} (<color=#ffdf72>{value.ToFloatNonSim():#.#}x</color>)</size></b>" );

                    count++;
                    if ( count < multipliersDealtByShip_LocalPlanetOrNull.Count )
                    {
                        buffer.Add( ", " );
                    } else
                    {
                        buffer.Add( ". " );
                    }
                }
            }

            if ( multipliersDealtByShip_GeneralOrNull != null )
            {
                multipliersDealtByShip_GeneralOrNull.SortIntoList( delegate ( KeyValuePair<GameEntityTypeData, RefThreeTuple<string, int, FInt>> Left,
                    KeyValuePair<GameEntityTypeData, RefThreeTuple<string, int, FInt>> Right )
                {
                    //desceding by bonus amount
                    int val = Right.Value.ThirdItem.CompareTo( Left.Value.ThirdItem );
                    if ( val != 0 )
                        return val;
                    //if same bonus amount, then by name
                    return Left.Key.DisplayName.CompareTo( Right.Key.DisplayName );
                } );

                int count = 0;
                foreach ( KeyValuePair<GameEntityTypeData, RefThreeTuple<string, int, FInt>> kv in multipliersDealtByShip_GeneralOrNull )
                {
                    string name = kv.Value.SecondItem + " " + kv.Value.FirstItem;
                    FInt value = kv.Value.ThirdItem;
                    buffer.Add( $"{name} (<color=#ffdf72>{value.ToFloatNonSim():#.#}x</color>)" );

                    count++;
                    if ( count < multipliersDealtByShip_GeneralOrNull.Count )
                    {
                        buffer.Add( ", " );
                    } else
                    {
                        buffer.Add( ". " );
                    }
                }
            }
        }

        #region WriteContents (Outguard)
        public void Write_Contents( OutguardInfo OutguardInfo )
        {
            var buffer = this.Buffer;
            
            bool WriteHeader = true;
            float PositionScaleMultiplier = 1.0f;
            Faction Faction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
            bool IsBeingDrawnInPopupWindowRatherThanTooltip = true;
            
            if ( WriteHeader )
            {
                EntityText.Write_Tooltip_Hotkeys_Footer( buffer, false, true, null);//, OutguardInfo.GroupData );
                buffer.Add( "\n\n" );
            }

            buffer.Add( $"<u><b><size=17>{OutguardInfo.GroupData.GetDisplayName()} Outguard Group:</size></b></u>" );

            OutguardInfo.GroupData.WriteTooltipInfo( buffer );

            buffer.Add( "\n\n" );

            try
            {
                EntityTypeDrawingBag unitBag = OutguardInfo.GroupData.UnitBag;
                if ( !EntityTypeDrawingBag.IsNullOrInvalid( unitBag ) )
                {
                    // units can be by tag or by name, if tag, list every possible outcome
                    ListOfLists<GameEntityTypeData> unitData = GameEntityTypeData.GetTemporaryGameEntityTypeDataListOfLists( "Window_InGameHoverEntityInfo-unitData", 10f );
                    if ( unitData == null ) //blocked for teardown/shutdown; bail
                        return;
                    unitBag.FillAllPossibleEntityTypes( unitData, World_AIW2.Instance.AIFactions[0] );

                    for ( int i = 0; i < unitData.OuterListCount; i++ )
                    {
                        buffer.AddSize_Large();
                        switch ( i )
                        {
                            case 0:
                                buffer.Add( "Primary: " );
                                break;
                            case 1:
                                buffer.Add( "Secondary: " );
                                break;
                            case 2:
                                buffer.Add( "Tertiary: " );
                                break;
                            case 3:
                                buffer.Add( "Quaternary: " );
                                break;
                            case 4:
                                buffer.Add( "Quinary: " );
                                break;
                            case 5:
                                buffer.Add( "Senary: " );
                                break;
                            case 6:
                                buffer.Add( "Septenary: " );
                                break;
                            case 7:
                                buffer.Add( "Octonary: " );
                                break;
                            case 8:
                                buffer.Add( "Nonary: " );
                                break;
                            case 9:
                                buffer.Add( "Denary: " );
                                break;
                            default:
                                buffer.Add( "Group " ).Add( i - 1 ).Add( ": " );
                                break;
                        }
                        
                        EntityTypeDrawingBag_SpawnMode spawnMode = unitBag.CountTypeList[i];
                        bool variableAmount = unitBag.SpawnValue_Min[i] != unitBag.SpawnValue_Max[i];
                        if ( variableAmount )
                        {
                            switch ( spawnMode )
                            {
                                case EntityTypeDrawingBag_SpawnMode.RawCount:
                                    buffer.Add( "Between " ).AddNumberMoreReadable( unitBag.SpawnValue_Min[i] ).Add( " and " ).AddNumberMoreReadable( unitBag.SpawnValue_Max[i] ).Add( "脳" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.AIBudget:
                                    buffer.Add( "Between " ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " and " ).AddNumberTruncated( unitBag.SpawnValue_Max[i] )
                                        .Add( " AI budget worth" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.Strength_BaseMark:
                                    buffer.Add( "Between " ).StartStrengthWrapper( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " and " )
                                        .AddNumberTruncated( unitBag.SpawnValue_Max[i] ).Add( " base strength (= at Mk1)" ).EndColor().Add( " worth of" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.Strength_CurrentMark:
                                    buffer.Add( "Between " ).StartStrengthWrapper( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " and " )
                                        .AddNumberTruncated( unitBag.SpawnValue_Max[i] ).Add( " strength " ).EndColor().Add( " worth of" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.MetalCost:
                                    buffer.Add( "Between " ).StartMetalWrapper( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " and " )
                                        .AddNumberTruncated( unitBag.SpawnValue_Max[i] ).Add( " metal " ).EndColor().Add( " worth of" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.EnergyCost:
                                    buffer.Add( "Between " ).StartEnergyWrapper( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " and " )
                                        .AddNumberTruncated( unitBag.SpawnValue_Max[i] ).Add( " energy " ).EndColor().Add( " worth of" );
                                    break;
                            }
                        } 
                        else
                        {
                            switch ( spawnMode )
                            {
                                case EntityTypeDrawingBag_SpawnMode.RawCount:
                                    buffer.AddNumberMoreReadable( unitBag.SpawnValue_Min[i] ).Add( "脳" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.AIBudget:
                                    buffer.AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " AI budget worth" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.Strength_BaseMark:
                                    buffer.StartStrengthWrapper( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " base strength (= at Mk1)" ).EndColor().Add( " worth of" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.Strength_CurrentMark:
                                    buffer.StartStrengthWrapper( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " strength " ).EndColor().Add( " worth of" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.MetalCost:
                                    buffer.StartMetalWrapper( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " metal " ).EndColor().Add( " worth of" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.EnergyCost:
                                    buffer.StartEnergyWrapper( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " energy " ).EndColor().Add( " worth of" );
                                    break;
                            }
                        }
                        if ( unitData[i].Count > 1 )
                            buffer.Add( " the following potential units:" );
                        buffer.Add( "\n\n" ).EndSize();
                        for ( int j = 0; j < unitData[i].Count; j++ )
                        {
                            EntityText.GetTooltip( buffer, null, null, unitData[i][j], 0, null, Faction.GetGlobalMarkLevelForShipLine( unitData[i][j] ),
                                FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None, PositionScaleMultiplier, IsBeingDrawnInPopupWindowRatherThanTooltip );
                            buffer.Add( "\n\n" );
                        }
                    }
                    buffer.Add( "\n" );

                    GameEntityTypeData.ReleaseTemporaryGameEntityTypeDataListOfLists( unitData );
                }
            } 
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLog( "Error in Window_InGameHoverEntityInfo, outguard unit display: " + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region WriteContents (Squad)
        public bool Write_Contents( GameEntity_Squad squad )
        {
            this.Squad = squad;
            
            float PositionScaleMultiplier = 1.0f;
            bool IsBeingDrawnInPopupWindowRatherThanTooltip = true;
            var buffer = this.Buffer;
            
            EntityText.Write_Tooltip_Hotkeys_Footer( buffer, false, true, null );
            buffer.Add( "\n\n" );

            buffer.Add( "<u>Main Ship:</u>\n" );
            EntityText.GetTooltip( buffer, squad, squad.FleetMembership,
                null, -1, null, 0, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None, PositionScaleMultiplier, IsBeingDrawnInPopupWindowRatherThanTooltip );
            buffer.Add( "\n\n" );

            #region show DSS contents
            if ( squad.TypeData.HackableForCommandStationsAndBattleStations_DSSStyle )
            {
                if ( squad.ShipGrantsList != null && squad.ShipGrantsList.Count > 0 )
                {
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    ShipLineEntry entry = null;
                    buffer.Add( "<u>Defensive Lines For Theft:</u>\n" );
                    for ( int i = 0; i < squad.ShipGrantsList.Count; i++ )
                    {
                        entry = squad.ShipGrantsList[i];
                        byte markLevel = localFaction.GetGlobalMarkLevelForShipLine( entry.TypeData );

                        GameEntityTypeData.MarkLevelStats markStatsForDisplay = entry.TypeData.MarkStatsFor( markLevel );
                        int cap = entry.TypeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( entry.TypeData, entry.BaseNumShips, entry.BaseNumShips, markStatsForDisplay.MarkLevel );

                        EntityText.GetTooltip( buffer, null, null,
                                entry.TypeData, null, localFaction.FactionCenterColor.ColorHexBrighter, " - Hack To Choose One Option", cap,
                                localFaction, markLevel, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.AIPCostOnGrant,
                                PositionScaleMultiplier, IsBeingDrawnInPopupWindowRatherThanTooltip );
                        buffer.Add( "\n\n" );
                    }
                }
            }
            #endregion

            #region show ARS or FRS contents
            if ( squad.TypeData.GrantsStuffToBeAddedToPlayerFleets )
            {
                if ( squad.ShipGrantsList != null && squad.ShipGrantsList.Count > 0 )
                {
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    ShipLineEntry entry = null;
                    buffer.Add( "<u>Ships Available For Theft:</u>\n" );
                    for ( int i = 0; i < squad.ShipGrantsList.Count; i++ )
                    {
                        entry = squad.ShipGrantsList[i];
                        byte markLevel = localFaction.GetGlobalMarkLevelForShipLine( entry.TypeData );

                        GameEntityTypeData.MarkLevelStats markStatsForDisplay = entry.TypeData.MarkStatsFor( markLevel );
                        int cap = entry.TypeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( entry.TypeData, entry.BaseNumShips, entry.BaseNumShips, markStatsForDisplay.MarkLevel );

                        EntityText.GetTooltip( buffer, null, null,
                                entry.TypeData, null, localFaction.FactionCenterColor.ColorHexBrighter, " - Hack To Choose One Option", cap,
                                localFaction, markLevel, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.AIPCostOnGrant,
                                PositionScaleMultiplier, IsBeingDrawnInPopupWindowRatherThanTooltip );
                        buffer.Add( "\n\n" );
                    }
                }
            }
            #endregion

            if ( squad.TypeData.IsFleetLeader )
            {
                #region show any fleets -- what they have in them
                Fleet squadFleet = squad.GetFleetOrNull_Safe();
                //if ( squad.HasNotYetBeenFullyClaimed || squad.GetFactionTypeSafe() != FactionType.Player )
                if ( squadFleet != null )
                {
                    bool isForCapture = squadFleet.GetFactionType_Safe() == FactionType.NaturalObject;
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();

                    buffer.Add( "<u>Ship Lines In Fleet:</u>\n" );
                    foreach ( FleetMembership mem in squadFleet.MemberGroupsSorted_NonSim_ForUIOnly_SeriouslyDoNotCallFromBGCode() )
                    {
                        if ( mem.TypeData.IsFleetLeader )
                            continue;

                        if ( isForCapture )
                        {
                            if ( mem.ExplicitBaseSquadCap <= 0 )
                                continue;
                            byte markLevel = localFaction.GetGlobalMarkLevelForShipLine( mem.TypeData );

                            GameEntityTypeData.MarkLevelStats markStatsForDisplay = mem.TypeData.MarkStatsFor( markLevel );
                            int cap = mem.TypeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( mem.TypeData, mem.ExplicitBaseSquadCap, mem.ExplicitBaseSquadCap, markStatsForDisplay.MarkLevel );

                            EntityText.GetTooltip( buffer, null, null,
                                mem.TypeData, null, localFaction.FactionCenterColor.ColorHexBrighter, " - Captured When Fleet Leader Is Claimed", cap, null,
                                markStatsForDisplay.MarkLevel.Ordinal, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None,
                                PositionScaleMultiplier, IsBeingDrawnInPopupWindowRatherThanTooltip );
                        } else //an actual one!
                        {
                            if ( mem.EffectiveSquadCap <= 0 )
                                continue;

                            EntityText.GetTooltip( buffer, null, mem,
                                null, mem.EffectiveSquadCap, null, mem.ForMark.MarkLevel.Ordinal, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None,
                                PositionScaleMultiplier, IsBeingDrawnInPopupWindowRatherThanTooltip );
                        }
                        buffer.Add( "\n\n" );
                    }
                }
                #endregion show any fleets -- what they have in them

                #region show the contents of fleet leaders that are transports
                if ( squadFleet != null && squadFleet.GetHasAnyMobileFleetTransportContents() )
                {
                    buffer.Add( "<u>Ships Transported In Flagship:</u>\n" );
                    foreach ( FleetMembership mem in squadFleet.MemberGroupsSorted_NonSim_ForUIOnly_SeriouslyDoNotCallFromBGCode() )
                    {
                        if ( mem.TypeData.IsFleetLeader )
                            continue;
                        if ( mem.TransportContents.Count == 0 )
                            continue;

                        EntityText.GetTooltip( buffer, null, mem,
                            null, mem.CalculateTransportedContentsCount(), null, 0, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None,
                            PositionScaleMultiplier, IsBeingDrawnInPopupWindowRatherThanTooltip );
                        buffer.Add( "\n\n" );
                    }
                }
                #endregion show the contents of fleet leaders that are transports
            }

            if ( squad.AIReinforcementPointContents != null && squad.AIReinforcementPointContents.Count > 0 )
            {
                byte markLevelOfContents = squad.GetMarkLevelOfContents();

                #region show ai reinforcement point contents
                buffer.Add( "<u>Ships Contained In This AI Reinforcement Point:</u>\n" );
                RefPair<GameEntityTypeData, int> pair;
                for ( int i = 0; i < squad.AIReinforcementPointContents.Count; i++ )
                {
                    pair = squad.AIReinforcementPointContents[i];
                    if ( pair != null && pair.RightItem > 0 )
                    {
                        EntityText.GetTooltip( buffer, null, null,
                            pair.LeftItem, pair.RightItem, squad.GetFactionOrNull_Safe(), markLevelOfContents, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None,
                            PositionScaleMultiplier, IsBeingDrawnInPopupWindowRatherThanTooltip );
                        buffer.Add( "\n\n" );
                    }
                }
                #endregion show ai reinforcement point contents
            }

            #region show outguard unit details
            if ( squad.TypeData.GetHasTag( OutguardBeaconStateForPlanet.OutguardBeaconTag ) )
            {
                List<OutguardGroupData> givenGroups = OutguardGroupData.GetTemporaryOutguardGroupDataList( "Window_InGameHoverEntityInfo-givenGroups", 10f );
                if ( givenGroups == null ) //blocked for teardown/shutdown; bail
                    return false;

                // list out details for each Outguard group

                bool hacked = false; // unused here, but needed as the function requires it
                OutguardBeaconStateForPlanet.GetAvailableGroupsForBeaconOnPlanet( squad.Planet, givenGroups, ref hacked );

                for ( int x = 0; x < givenGroups.Count; x++ )
                {
                    OutguardGroupData groupData = givenGroups[x];
                    Write_Contents( World_AIW2.Instance.GetOutguardState( groupData ) );
                }

                OutguardGroupData.ReleaseTemporaryOutguardGroupDataList( givenGroups );
            }
            #endregion

            return true;
        }
        #endregion

        public void WriteHullOrShieldsNumber( ArcenCharacterBufferBase buffer, int Number )
        {
            if ( Number >= 1000000 )
                buffer.AddFixedDecimal( (Number / 1000000f), 2 ).Add( "m" );
            else if ( Number >= 10000 )
                buffer.Add( (int)System.Math.Round( Number / 1000f ) ).Add( "k" );
            else
                buffer.AddNumberMoreReadable( Number );
        }

        //private static void HandleNewline( ArcenDoubleCharacterBuffer buffer, ref bool haveDoneNewLine )
        //{
        //    if ( haveDoneNewLine )
        //        return;
        //    haveDoneNewLine = true;
        //    buffer.Add( "\n" );
        //}

        private static void HandleNewlineAndSize( ArcenCharacterBufferBase buffer, ref bool haveDoneNewLineAndSize )
        {
            if ( haveDoneNewLineAndSize )
                return;
            haveDoneNewLineAndSize = true;
            buffer.Add( "\n" ).Add( FontSizes.SLIGHTLY_SMALLER_SIZE_V2_STRING );
        }

        private static void AddSingleValue( ArcenCharacterBufferBase buffer, string newVal )
        {
            buffer.Add( newVal );
        }

        public void AddSingleValueStrengthOnly( ArcenCharacterBufferBase buffer, int Strength )
        {
            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, Strength, true, true );
        }

        public void WriteRangedSupportMetalFlowStartOrSeparator( ArcenCharacterBufferBase Buffer, GameEntityTypeData.MarkLevelStats markStats, ref bool WroteStart )
        {
            if ( WroteStart )
            {
                Buffer.Add( ", " );
                return;
            }
            WroteStart = true;
            Buffer.Add( "Within " ).WrapRangeMoreReadable( markStats.AssistRange, false, true ).Add( ": " );
        }

        public PlannedMetalFlow GetMetalFlowForPurposeOrDefault( GameEntity_Squad squad, MetalFlowPurpose purpose )
        {
            List<PlannedMetalFlow> flows = squad.SquadPlannedFlows.GetDisplayList();
            PlannedMetalFlow plannedFlow;
            for (int i = 0; i < flows.Count; i++ )
            {
                plannedFlow = flows[i];
                if ( flows[i].Purpose == purpose )
                {
                    if ( plannedFlow.FromEntity != null && plannedFlow.IsInRangeAtTheMoment )
                        return flows[i];
                    else
                        break;
                }
            }
            return PlannedMetalFlow.Create( null, MetalFlowPurpose.None );
        }

        public void WriteSupportMetalFlows(ArcenCharacterBufferBase buffer)
        {
            using (var list = StructList<MetalFlowForDisplay>.Get())
            {
                EnumerateMetalFlows(list);
                
                if (list.Count == 0)
                    return;
                
                foreach (var item in list)
                {
                    if (item.For == MetalFlowPurpose.FactoryConstructionForPlayerMobileFleets)
                    {
                        
                        break;
                    }
                }
                
                buffer.BeginStatement(TextStyle.Attr_Line);
                
                buffer.Add("Engineer", TextStyle.Attr_Label).Add(": ");
                buffer.Add(" assist in ").AddNumber(Squad.DataForMark.AssistRange, TextTerm.Range, TermUse.Name);

                buffer.Open(TextStyle.Attr_Sub_Lines);
                
                foreach (var item in list)
                {
                    if (item.For == MetalFlowPurpose.FactoryConstructionForPlayerMobileFleets)
                    {
                        buffer.Open(TextStyle.Assist_Item);
                        buffer.Add( "Construct Fleet", TextStyle.Assist_Label );
                        buffer.Add( " " ).AddNumber( item.Data.EffectiveThroughput, null, TextTerm.Metal, TermUse.Icon, null ).Add(" /sec", TextStyle.Fraction_Gray);
                        
                        PlannedMetalFlow flow = GetMetalFlowForPurposeOrDefault( Squad, item.For );
                        if ( flow.FromEntity != null )
                            WritePlannedMetalFlowBriefTargetInfo( buffer, flow, "Build", Config.Detail >= TooltipDetail.Full );
                        
                        buffer.Close(TextStyle.Assist_Item);
                        
                        continue;
                    }

                    if (item.For == MetalFlowPurpose.AssistSelfConstruction)
                    {
                        buffer.Open(TextStyle.Assist_Item);
                        buffer.Add( "Construction", TextStyle.Assist_Label );
                        buffer.Add( " " ).AddNumber( item.Data.EffectiveThroughput, null, TextTerm.Metal, TermUse.Icon, null ).Add(" /sec", TextStyle.Fraction_Gray);
                        
                        PlannedMetalFlow flow = GetMetalFlowForPurposeOrDefault( Squad, item.For );
                        if ( flow.FromEntity != null )
                            WritePlannedMetalFlowBriefTargetInfo( buffer, flow, "Assist", Config.Detail >= TooltipDetail.Full );
                        
                        buffer.Add("");
                        buffer.Close(TextStyle.Assist_Item);
                        
                        continue;
                    }
                    
                    if (item.For == MetalFlowPurpose.AssistFactoryConstruction)
                    {
                        buffer.Open(TextStyle.Assist_Item);
                        buffer.Add( "Factory Construction", TextStyle.Assist_Label );
                        buffer.Add( " " ).AddNumber( item.Data.EffectiveThroughput, null, TextTerm.Metal, TermUse.Icon, null ).Add(" /sec", TextStyle.Fraction_Gray);

                        PlannedMetalFlow flow = GetMetalFlowForPurposeOrDefault( Squad, item.For );
                        if ( flow.FromEntity != null )
                            WritePlannedMetalFlowBriefTargetInfo( buffer, flow, "Boost", Config.Detail >= TooltipDetail.Full );
                        
                        buffer.Add("");
                        buffer.Close(TextStyle.Assist_Item);
                        
                        continue;
                    }
                    
                    if (item.For == MetalFlowPurpose.ClaimingNeutrals)
                    {
                        buffer.Open(TextStyle.Assist_Item);

                        buffer.Add( "Claim Neutrals", TextStyle.Assist_Label );
                        buffer.Add( " " ).AddNumber( item.Data.EffectiveThroughput, null, TextTerm.Metal, TermUse.Icon, null ).Add(" /sec", TextStyle.Fraction_Gray);
                        
                        PlannedMetalFlow flow = GetMetalFlowForPurposeOrDefault( Squad, item.For );
                        if ( flow.FromEntity != null )
                            WritePlannedMetalFlowBriefTargetInfo( buffer, flow, "Claim", Config.Detail >= TooltipDetail.Full );
                        
                        buffer.Add("");
                        buffer.Close(TextStyle.Assist_Item);
                        
                        continue;
                    }
                    
                    if (item.For == MetalFlowPurpose.RebuildingRemains)
                    {
                        buffer.Open(TextStyle.Assist_Item);

                        buffer.Add( "Rebuild Remains", TextStyle.Assist_Label );
                        buffer.Add( " " ).AddNumber( item.Data.EffectiveThroughput, null, TextTerm.Metal, TermUse.Icon, null ).Add(" /sec", TextStyle.Fraction_Gray);
                        
                        PlannedMetalFlow flow = GetMetalFlowForPurposeOrDefault( Squad, item.For );
                        if ( flow.FromEntity != null )
                            WritePlannedMetalFlowBriefTargetInfo( buffer, flow, "Rebuild", Config.Detail >= TooltipDetail.Full );
                        
                        buffer.Add("");
                        buffer.Close(TextStyle.Assist_Item);
                        
                        continue;
                    }
                    
                    if (item.For == MetalFlowPurpose.RepairingHullsOfFriendlies)
                    {
                        buffer.Open(TextStyle.Assist_Item);

                        buffer.Add("Repair Hull", TextStyle.Assist_Label);
                        buffer.Add( " " ).AddNumber( item.Data.EffectiveThroughput, null, TextTerm.Metal, TermUse.Icon, null ).Add(" /sec", TextStyle.Fraction_Gray);
                        
                        PlannedMetalFlow flow = GetMetalFlowForPurposeOrDefault( Squad, item.For );
                        if ( flow.FromEntity != null )
                            WritePlannedMetalFlowBriefTargetInfo( buffer, flow, "Repair", Config.Detail >= TooltipDetail.Full );
                        
                        buffer.Add("");
                        buffer.Close(TextStyle.Assist_Item);
                        
                        continue;
                    }
                    
                    if (item.For == MetalFlowPurpose.RepairingShieldsOfFriendlies)
                    {
                        buffer.Open(TextStyle.Assist_Item);

                        buffer.Add("Repair Shields", TextStyle.Assist_Label);
                        buffer.Add( " " ).AddNumber( item.Data.EffectiveThroughput, null, TextTerm.Metal, TermUse.Icon, null ).Add(" /sec", TextStyle.Fraction_Gray);
                        
                        PlannedMetalFlow flow = GetMetalFlowForPurposeOrDefault( Squad, item.For );
                        if ( flow.FromEntity != null )
                            WritePlannedMetalFlowBriefTargetInfo( buffer, flow, "Repair", Config.Detail >= TooltipDetail.Full );
                        
                        buffer.Add("");
                        buffer.Close(TextStyle.Assist_Item);
                        
                        continue;
                    }
                    
                    if (item.For == MetalFlowPurpose.RepairingEnginesOfFriendlies)
                    {
                        buffer.Open(TextStyle.Assist_Item);

                        buffer.Add("Repair Engines", TextStyle.Assist_Label);
                        buffer.Add( " " ).AddNumber( item.Data.EffectiveThroughput, null, TextTerm.Metal, TermUse.Icon, null ).Add(" /sec", TextStyle.Fraction_Gray);
                        
                        PlannedMetalFlow flow = GetMetalFlowForPurposeOrDefault( Squad, item.For );
                        if ( flow.FromEntity != null )
                            WritePlannedMetalFlowBriefTargetInfo( buffer, flow, "Repair", Config.Detail >= TooltipDetail.Full );
                        
                        buffer.Add("");
                        buffer.Close(TextStyle.Assist_Item);
                        
                        continue;
                    }
                }
                
                buffer.Close(TextStyle.Attr_Sub_Lines);
                
                buffer.EndStatement(TextStyle.Newline_NoLabel);
            }
        }

        private static bool WritePlannedMetalFlowBriefTargetInfo( ArcenCharacterBufferBase buffer, PlannedMetalFlow flow, string Prefix, bool WriteFull )
        {
            if ( flow.FromEntity == null )
                return false;
            GameEntity_Squad squad = flow.SquadRecipient;
            if ( squad == null )
                return false;

            buffer.StartColor( "70ff59" ); //bright green
            buffer.Add( " (" );
            if ( Prefix != null && Prefix.Length > 0 )
                buffer.Add( Prefix ).Add( ": " );
            buffer.StartColor( squad.GetFactionCenterColorHexBrighter_Safe() );
            buffer.Add( WriteFull ? squad.TypeData.DisplayName : squad.FleetMembership?.GetDisplayNameForSidebar() ).EndColor();
            if ( WriteFull && squad.CurrentMarkLevel > 0 )
                buffer.Add( " " ).StartColor( squad.DataForMark.MarkLevel.ColorHex ).Add( squad.DataForMark.MarkLevel.Abbreviation ).EndColor();
            buffer.Add( ")" ).EndColor();
            return true;
        }

        public void WriteAmplifierOrInhibitorCategoryStartIfNeeded( ArcenCharacterBufferBase Buffer, bool IsAmplifier, ref bool AlreadyWroteStart, ref bool NeedsSeparator )
        {
            if ( AlreadyWroteStart )
            {
                if ( NeedsSeparator )
                {
                    NeedsSeparator = false;
                    Buffer.Add( ", " );
                }
                return;
            }
            AlreadyWroteStart = true;
            if( IsAmplifier )
                Buffer.Add( "<color=#f25e1c>PLANETARY AMPLIFIER:</color> On this planet boosts " );
            else
                Buffer.Add( "<color=#f25e1c>PLANETARY INHIBITOR:</color> On this planet impairs " );
        }

        public void WriteAmplifierOrInhibitorSubCategoryStartIfNeeded( ArcenCharacterBufferBase Buffer, bool IsForAllies, ref bool AlreadyWroteStart )
        {
            if ( AlreadyWroteStart )
            {
                Buffer.Add( ", " );
                return;
            }
            AlreadyWroteStart = true;
            if( IsForAllies )
                Buffer.Add( "its owners and allies with " );
            else
                Buffer.Add( "its enemies with " );
        }

        public void WriteAmplifierOrInhibitorSubCategory( ArcenCharacterBufferBase Buffer, GameEntityTypeData.MarkLevelStats MarkData,
            bool IsAmplifier, bool IsForAllies, bool UseIcons, bool UseText, ref bool NeedsSeparator, ref bool WroteCategoryStart )
        {
            bool writeAttack;
            FInt attack;
            bool writeSpeed_Mult;
            FInt speed_Mult;
            bool writeSpeed_Flat;
            int speed_Flat;
            if(IsForAllies)
            {
                attack = MarkData.AlliedAttackMultiplier;
                speed_Mult = MarkData.AlliedSpeedMultiplier;
                speed_Flat = MarkData.AlliedSpeedFlatBonus;
            } else
            {
                attack = MarkData.HostileAttackMultiplier;
                speed_Mult = MarkData.HostileSpeedMultiplier;
                speed_Flat = MarkData.HostileSpeedFlatBonus;
            }
            if ( IsAmplifier )
            {
                writeAttack = attack > FInt.One;
                writeSpeed_Mult = speed_Mult > FInt.One;
                writeSpeed_Flat = speed_Flat > 0;
            } else
            {
                writeAttack = attack < FInt.One && attack > FInt.Zero;
                writeSpeed_Mult = speed_Mult < FInt.One && speed_Mult > FInt.Zero;
                writeSpeed_Flat = speed_Flat < 0;
            }

            bool wroteSubCategoryStart = false;
            if ( writeAttack )
            {
                WriteAmplifierOrInhibitorCategoryStartIfNeeded( Buffer, IsAmplifier, ref WroteCategoryStart, ref NeedsSeparator );
                WriteAmplifierOrInhibitorSubCategoryStartIfNeeded( Buffer, IsForAllies, ref wroteSubCategoryStart );
                Buffer.StartDamageWrapper( UseIcons ).Add( attack ).Add( "脳" ).EndDamageWrapper( UseText );
            }
            if ( writeSpeed_Mult || writeSpeed_Flat )
            {
                WriteAmplifierOrInhibitorCategoryStartIfNeeded( Buffer, IsAmplifier, ref WroteCategoryStart, ref NeedsSeparator );
                WriteAmplifierOrInhibitorSubCategoryStartIfNeeded( Buffer, IsForAllies, ref wroteSubCategoryStart );
                Buffer.StartSpeedWrapper( UseIcons );
                if ( writeSpeed_Mult )
                    Buffer.Add( speed_Mult ).Add( "脳" );
                if ( writeSpeed_Flat )
                {
                    if ( writeSpeed_Mult )
                        Buffer.Add( ", " );
                    if ( IsAmplifier )
                        Buffer.Add( "+" );
                    Buffer.Add( speed_Flat );
                }
                Buffer.EndSpeedWrapper( UseText );
            }
        }

        public void WriteAmplifierAndInhibitorData( ArcenCharacterBufferBase Buffer, GameEntityTypeData.MarkLevelStats MarkData, bool UseIcons, bool UseText )
        {
            if ( MarkData == null )
                return;

            bool NeedsSeparator = false;
            bool WroteAmplifierStart = false;
            bool WroteInhibitorStart = false;

            WriteAmplifierOrInhibitorSubCategory( Buffer, MarkData, true, true, UseIcons, UseText, ref NeedsSeparator, ref WroteAmplifierStart );
            WriteAmplifierOrInhibitorSubCategory( Buffer, MarkData, false, true, UseIcons, UseText, ref NeedsSeparator, ref WroteAmplifierStart );
            if ( WroteAmplifierStart )
                Buffer.Add( ".  " );

            WriteAmplifierOrInhibitorSubCategory( Buffer, MarkData, true, false, UseIcons, UseText, ref NeedsSeparator, ref WroteInhibitorStart );
            WriteAmplifierOrInhibitorSubCategory( Buffer, MarkData, false, false, UseIcons, UseText, ref NeedsSeparator, ref WroteInhibitorStart );
            if ( WroteInhibitorStart )
                Buffer.Add( ".  " );
        }
        
        /// <summary>
        /// Enumerate and add to the passed collection, all metal-flows of 'e' that should be displayed/visible to the player.
        /// </summary>
        public void EnumerateMetalFlows( StructList<MetalFlowForDisplay> flows )
        {
            if ( Config.Detail < TooltipDetail.Medium )
                return;

            for ( var flow = MetalFlowPurpose.None+1; flow < MetalFlowPurpose.Length; flow++ )
            {
                //only shown in full mode
                if (flow == MetalFlowPurpose.ClaimingNeutrals || 
                    flow == MetalFlowPurpose.RebuildingRemains || 
                    flow == MetalFlowPurpose.SelfConstruction)
                {
                    if ( Config.Detail < TooltipDetail.Full )
                        continue;
                }

                // we don't actually handle showing this well currently
                // so just dont
                if (flow == MetalFlowPurpose.SelfConstruction)
                    continue;
                
                var metalFlow = Squad.DataForMark.GetMetalFlow( flow );
                if ( !metalFlow.HasRealData )
                    continue;
                
                if ( metalFlow.EffectiveThroughput <= FInt.Zero )
                    continue;

                flows.Add( new MetalFlowForDisplay(flow, metalFlow) );
            }
        }
        
        #endregion
    }
}
