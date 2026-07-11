using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{

    public class Window_PrototypeInGameHoverEntityInfo : WindowControllerAbstractBase
    {
        [ThreadStatic] //this one use of ThreadStatic is ok - it's highly unlikely to be on multiple threads, but if it is, then this is important
        private static GameEntity_Squad fakeSquadForBuildMode;
        [ThreadStatic]
        private static List<Window_PrototypeInGameHoverEntityInfoUtils.FleetShipStatsForDisplay> fleetInfoList;

        public static bool _GetTextForEntity( ArcenCharacterBufferBase buffer, GameEntity_Squad relatedSquadOrNull, FleetMembership MembershipBase,
            GameEntityTypeData TypeDataOrNull, int OptionalCountToShow, Faction ForFactionOrNull, byte OptionalForMarkLevel, FromSidebarType IsFromSidebarType,
            ShipExtraDetailFlags DetailFlags, float PositionScaleMultiplier, bool IsBeingDrawnInPopupWindowRatherThanTooltip )
        {
            return _GetTextForEntity( buffer, relatedSquadOrNull, MembershipBase,
                TypeDataOrNull, null, "ffffff", string.Empty, OptionalCountToShow, ForFactionOrNull, OptionalForMarkLevel, IsFromSidebarType,
                DetailFlags, PositionScaleMultiplier, IsBeingDrawnInPopupWindowRatherThanTooltip );
        }

        public static bool _GetTextForEntity( ArcenCharacterBufferBase buffer, GameEntity_Squad relatedSquadOrNull, FleetMembership MembershipBase,
            GameEntityTypeData TypeDataOrNull, Fleet FleetToUseOrNull, string AltTextColorIfUsed, string AltTextInPlaceOfFleetAndOwnerOrBlank, int OptionalCountToShow,
            Faction ForFactionOrNull, byte OptionalForMarkLevel, FromSidebarType IsFromSidebarType,
            ShipExtraDetailFlags DetailFlags, float PositionScaleMultiplier, bool IsBeingDrawnInPopupWindowRatherThanTooltip )
        {
            int debugStage = 0;
            try
            {
                Window_InGameHoverEntityInfo.IsDrawing = false;
                debugStage = 1;

                if ( PositionScaleMultiplier != 1f )
                    PositionScaleMultiplier = 1f; //from Chris: now that we have ultrawide displays, this does not seem to be needed.

                TooltipDetail detailLevel = EntityText.Detail;
                bool isShowingStrengthsAndWeaknesses = InputCaching.CalculateHoldToSeeShipStrengthsAndWeaknesses();

                FleetMembership relatedMembershipOrNull = relatedSquadOrNull == null ? MembershipBase : relatedSquadOrNull.FleetMembership;
                GameEntityTypeData relatedEntityTypeData = relatedSquadOrNull == null ? (MembershipBase == null ? TypeDataOrNull : MembershipBase.TypeData) : relatedSquadOrNull.TypeData;
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

                bool showDebugInfoInTooltip = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" );

                if ( relatedSquadOrNull != null && ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                {
                    relatedSquadOrNull.FlagForRequestedForcedFullSyncToAllClients_FromAnyClient();
                }

                debugStage = 2;
                
                Window_InGameHoverEntityInfo.Mode panelMode;
                
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

                if (DetailFlags.HasFlag(ShipExtraDetailFlags.BuildInfo))
                    panelMode = Window_InGameHoverEntityInfo.Mode.Build;
                
                bool modeHasSpecificUnitOrFaction = relatedSquadOrNull != null;
                bool isForMultipleUnits = panelMode == Window_InGameHoverEntityInfo.Mode.UnitGroupOnSidebar;

                bool isFromHackSidebarPopoutWindow = DetailFlags.HasFlag( ShipExtraDetailFlags.WindowHackChoicesSidebarPopoutForData );
                
                HackingType hackBeingDoneAgainstUs = null;
                GameEntity_Squad hackerBeingUsedAgainstUs = null;
                if (isFromHackSidebarPopoutWindow)
                {
                    hackBeingDoneAgainstUs = Window_HackChoicesSidebarPopout.Instance.HackTypeToChooseFor;
                    hackerBeingUsedAgainstUs = Window_HackChoicesSidebarPopout.Instance.HackerToUseOrNullIfNoneHere;
                }
                
                if ( hackBeingDoneAgainstUs != null && hackBeingDoneAgainstUs.IsAGrantShipStyleHack )
                {
                    if ( relatedEntityTypeData.IsBlockedFromDSSGrantToCommandStations )
                        buffer.Add( "不会授予指挥站；仅限战斗堡垒和城堡。\n" );
                    if ( relatedEntityTypeData.IsBlockedFromDSSGrantToBattlestationsAndCitadels )
                        buffer.Add( "不会授予战斗堡垒或城堡；仅限指挥站。\n" );
                }

                debugStage = 3;

                debugStage = 4;
                Faction localPlayerFactionOrNull = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                debugStage = 5;
                Faction owningFactionOrNull = (ForFactionOrNull != null ? ForFactionOrNull : (modeHasSpecificUnitOrFaction ? relatedSquadOrNull?.PlanetFaction?.Faction : localPlayerFactionOrNull));
                debugStage = 6;
                Planet thisPlanetOrNull = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                debugStage = 7;
                PlanetFaction localPlayerPlanetFactionOrNull = (thisPlanetOrNull == null || localPlayerFactionOrNull == null ? null : thisPlanetOrNull.GetPlanetFactionForFaction( localPlayerFactionOrNull ));
                debugStage = 8;

                PlayerTypeData localPlayerTypeOrNull = localPlayerFactionOrNull == null ? null : localPlayerFactionOrNull.PlayerTypeDataOrNull_ModeratelyExpensive;
                PlayerTypeData owningPlayerTypeOrNull = owningFactionOrNull == null ? null : owningFactionOrNull.PlayerTypeDataOrNull_ModeratelyExpensive;

                bool showMetalCost = true;
                bool showMetalOther = true;
                bool showAnyFuel = true;
                bool showEnergyProduction = true;
                bool showEnergyConsumption = true;

                if ( owningPlayerTypeOrNull != null )
                {
                    if ( !owningPlayerTypeOrNull.UsesMetal )
                    {
                        showMetalOther = false;
                    }

                    if ( !owningPlayerTypeOrNull.UsesEnergyAndFuel )
                    {
                        showEnergyProduction = false;
                    }
                }
                if ( localPlayerFactionOrNull == owningFactionOrNull )
                {
                    if ( localPlayerTypeOrNull != null && !localPlayerTypeOrNull.UsesMetal )
                    {
                        showMetalCost = false;
                    }
                }

                if ( localPlayerTypeOrNull != null && !localPlayerTypeOrNull.UsesEnergyAndFuel )
                {
                    showAnyFuel = false;
                }

                Fleet relatedMemFleetOrNull = null;
                if ( relatedMembershipOrNull != null )
                    relatedMemFleetOrNull = relatedMembershipOrNull.Fleet;
                if ( FleetToUseOrNull != null )
                    relatedMemFleetOrNull = FleetToUseOrNull;

                Fleet fedFromCityFleetOrNull = relatedMembershipOrNull?.GetFedHereFromCityFleetOrNull();

                bool isToBeClaimed = false;
                if ( relatedSquadOrNull != null && 
                     relatedSquadOrNull.HasNotYetBeenFullyClaimed &&
                     (relatedSquadOrNull.GetFactionTypeSafe() == FactionType.NaturalObject || relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player ) )
                {
                    isToBeClaimed = true;
                }

                //buffer.Add( "\nowningFaction: " ).Add( owningFaction.GetDisplayName() )
                //    .Add( " ForFactionOrNull: " ).Add( ForFactionOrNull == null ? "[null]" : ForFactionOrNull.GetDisplayName() )
                //    .Add( " Fleet: " ).Add( relatedMemFleetOrNull == null ? "[null]" : relatedMemFleetOrNull.GetName() )
                //    .Add( "\n" );

                debugStage = 9;
                //if ( relatedMembership == null )
                //{
                //    buffer.Add( "NULL relatedMembership" ).Add( "\n" );
                //    buffer.Add( "relatedEntity: " ).Add( relatedEntity == null ? "null" : relatedEntity.TypeData.InternalName ).Add( "\n" );
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
                debugStage = 10;
                GameEntityTypeData.MarkLevelStats relatedMarkLevelData = relatedEntityTypeData.MarkStatsFor( effectiveMarkLevel );
                //This little section is required for AI ships to show their correct values.  Same with other non-player ships.
                if ( relatedSquadOrNull != null )
                {
                    relatedMarkLevelData = relatedSquadOrNull.DataForMark;
                    effectiveMarkLevel = relatedMarkLevelData.MarkLevel.Ordinal;
                    relatedSquadPlanetFactionOrNull = relatedSquadOrNull.PlanetFaction;
                    relatedSquadFactionOrNull = relatedSquadPlanetFactionOrNull == null ? null : relatedSquadPlanetFactionOrNull.Faction;
                }

                Window_InGameHoverEntityInfo.IsDrawing = true;

                float baseOffsetBeforeSteps = 0;
                if ( relatedEntityTypeData.TexEmbedSprite_Icon != null )
                {
                    var default_faction = SpecialFactionDataTable.Instance.DefaultRow;
                    var centerColor = default_faction.DefaultFactionCenterColor;
                    var trimColor = default_faction.DefaultFactionTrimColor;
                    
                    debugStage = 25;
                    if (PlayerProfile_AIW2.Local != null)
                    {
                        centerColor = PlayerProfile_AIW2.Local.DefaultFactionCenterColor;
                        trimColor = PlayerProfile_AIW2.Local.DefaultFactionTrimColor;
                    }
                    
                    if (owningFactionOrNull != null)
                    {
                        centerColor = owningFactionOrNull.FactionCenterColor;
                        trimColor = owningFactionOrNull.FactionTrimColor;
                    }
                    
                    if (relatedEntityTypeData.OverrideFactionColor_Center != null)
                        centerColor = relatedEntityTypeData.OverrideFactionColor_Center;
                    if (relatedEntityTypeData.OverrideFactionColor_Trim != null)
                        trimColor = relatedEntityTypeData.OverrideFactionColor_Trim;

                    //note: mspace has been recoded by Chris to be a "do not advance the x offset" function.  It is not monospace like it otherwise would be.
                    //      this allows for drawing sprites on top of one another in the stacked fashion, and then having other stuff come after it
                    buffer.Add( "<mspace><sprite=\"" ).Add( relatedEntityTypeData.TexEmbedSprite_Icon.InternalName )
                        .Add( "\" color=\"#" ).Add( centerColor.ColorHex ).Add( "\">" );
                    if ( relatedEntityTypeData.TexEmbedSprite_IconBorder != null )
                    {
                        buffer.Add( "<sprite=\"" ).Add( relatedEntityTypeData.TexEmbedSprite_IconBorder.InternalName )
                            .Add( "\" color=\"#" ).Add( trimColor.ColorHex ).Add( "\">" );
                    }
                    if ( relatedEntityTypeData.TexEmbedSprite_IconOverlay != null )
                    {
                        buffer.Add( "<sprite=\"" ).Add( relatedEntityTypeData.TexEmbedSprite_IconOverlay.InternalName ).Add( "\">" );
                    }
                    buffer.Add( "</mspace><pos=50>" );
                    baseOffsetBeforeSteps = 50 * PositionScaleMultiplier;
                    buffer.SetPositionOffset( baseOffsetBeforeSteps );
                }
                debugStage = 11;

                //========Start Chris adapter for SirLimbo
                GameEntity_Squad entity = relatedSquadOrNull;
                GameEntityTypeData entityType = relatedEntityTypeData;
                Balance_MarkLevel markLevel = Balance_MarkLevelTable.Instance.RowsByOrdinal[effectiveMarkLevel];
                FleetMembership fleetMembershipOrNull = relatedMembershipOrNull;
                debugStage = 101;
                bool isUsingFakeEntityAsStandin = false;

                debugStage = 102;
                //=========fakeSquadForBuildMode is set up as an adapter from Chris to make life easier for all of us, but specifically for SirLimbo's stuff.
                if ( fakeSquadForBuildMode == null )
                    fakeSquadForBuildMode = GameEntity_Squad.CreateNew_ForFakeInUITooltipsOnlyDoNotUseThisAnywhereElse();

                debugStage = 103;
                if ( entity == null )
                {
                    debugStage = 104;
                    isUsingFakeEntityAsStandin = true; //this keeps entity from being null and lets it work properly, but is probably just for build menus and wave contents tooltips, etc
                    entity = fakeSquadForBuildMode;
                    debugStage = 105;
                    fakeSquadForBuildMode.SetInfoForFakeInUITooltipsOnlyDoNotUseThisAnywhereElse( entityType, effectiveMarkLevel, thisPlanetOrNull,
                        owningFactionOrNull );
                }

                debugStage = 106;
                //these are conveniences
                PlanetFaction entityPFaction = entity == null ? null : entity.PlanetFaction;
                Faction entityFaction = entityPFaction == null ? null : entityPFaction.Faction;

                debugStage = 107;
                //========Start SirLimbo Bits
                float posSteps = (float) Math.Round( 100 * PositionScaleMultiplier );
                CharacterPosInfo cpi = new CharacterPosInfo( posSteps );
                bool useIcons;
                bool useText;
                switch (detailLevel)
                {
                    case TooltipDetail.SuperShortBecauseShowingOtherStuff:
                    case TooltipDetail.SuperShort:
                        useIcons = GameSettings.Current.GetBoolBySetting( "Tooltip_UseIcons_Short" );
                        break;
                    case TooltipDetail.Medium:
                        useIcons = GameSettings.Current.GetBoolBySetting( "Tooltip_UseIcons_Medium" );
                        break;
                    case TooltipDetail.Full:
                        useIcons = GameSettings.Current.GetBoolBySetting( "Tooltip_UseIcons_Full" );
                        break;
                    default:
                        throw new Exception( "Error: Unimplemented TooltipDetail mode: " + detailLevel );

                }
                useText = !useIcons;

                GameEntityTypeData.MarkLevelStats markStats = relatedMarkLevelData;

                if (relatedEntityTypeData.HideStatBlock)
                {
                    buffer.NewLine();
                    buffer.SetPositionOffset( baseOffsetBeforeSteps );
                }

                #region FirstRow: Entity Name, Faction, etc
                debugStage = 13;
                buffer.Add( "<b>" );

                if ( DetailFlags.HasFlag( ShipExtraDetailFlags.InGuardPost ) )
                    buffer.Add( "已装载 ", "ff622b" );
                else if ( DetailFlags.HasFlag( ShipExtraDetailFlags.BeingTransported ) )
                    buffer.Add( "已装载 ", "59d2ff" );
                else if ( DetailFlags.HasFlag( ShipExtraDetailFlags.LoadedDrone ) )
                    buffer.Add( "已装载 ", "59d2ff" );

                if ( OptionalCountToShow > 1 )
                {
                    buffer.Add( OptionalCountToShow, "dbef21" ).Add( "x " );
                } 
                else if ( entity.ExtraStackedSquadsInThis > 0 )
                {
                    buffer.Add( entity.ExtraStackedSquadsInThis + 1, "dbef21" ).Add( "x " );
                }
                debugStage = 14;

                buffer.AddSize_Large();

                if ( entity.SecondsSpentAsRemains >= 0 )
                {
                    buffer.Add( "残骸 ", "ff4b21" );
                }
                if ( entityType.IsModular && relatedMembershipOrNull != null )
                {
                    foreach ( EntitySystemTypeData systemData in entityType.SystemTypes )
                    {
                        if ( !systemData.IsModule || systemData.ModularFullNamePrefix == null || systemData.ModularFullNamePrefix.Length <= 0 )
                            continue; //skip any non-modules, or things that don't add to the prefix
                        if ( systemData.IsModule && !systemData.IsModuleOn( relatedMembershipOrNull ) )
                            continue; //skip any that are not enabled
                        if ( !systemData.ForMark[effectiveMarkLevel].IsFunctionalAtThisMarkLevel )
                            continue;

                        buffer.Add( systemData.ModularFullNamePrefix );
                    }
                }
                buffer.Add( entityType.DisplayName );
                if ( markLevel.Ordinal != 0 )
                {
                    buffer.Add( " " ).AddMarkLevelFormated( markLevel );
                }
                debugStage = 15;
                if ( AltTextInPlaceOfFleetAndOwnerOrBlank != null && AltTextInPlaceOfFleetAndOwnerOrBlank.Length > 0 )
                {
                    buffer.StartColor( AltTextColorIfUsed ).Add( AltTextInPlaceOfFleetAndOwnerOrBlank ).EndColor();
                } else if ( owningFactionOrNull != null )
                {
                    if (entityType.HideFactionName)
                    { }
                    else
                    {
                        buffer.Add( " of " ).StartColor( owningFactionOrNull.FactionCenterColor.ColorHexBrighter );
                        if ( owningFactionOrNull.Type == FactionType.AI || FactionUtilityMethods.Instance.IsACoreAISubFaction( owningFactionOrNull ) )
                        {
                            AISentinelsCoreData sentinelData = owningFactionOrNull.TryGetAISentinelsCoreData()?.SentinelInfo;
                            if ( sentinelData != null )
                            {
                                bool secretFactionDetails = AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "FactionDetailsAreSecret" ) && World.Instance.ConclusionType == CampaignConclusionType.NotConcluded;
                                var showRandomAiType = GameSettings.Current.GetBoolBySetting( "ShowRandomAIType" );

                                if (!secretFactionDetails)
                                {
                                    if ( sentinelData.WasRandomAIType )
                                    {
                                        if ( showRandomAiType )
                                        {
                                            buffer.Add( sentinelData.AIType.DisplayName ).Add( " " );
                                        } 
                                        else
                                        {
                                            buffer.Add( "随机 " );
                                        }
                                    } 
                                    else if ( sentinelData.AdaptiveAIDifficulty != TypeDifficulty.Unset )
                                    {
                                        if ( showRandomAiType )
                                        {
                                            buffer.Add( sentinelData.AIType.DisplayName ).Add( " " );
                                        } 
                                        else
                                        {
                                            buffer.Add( "自适应 " );
                                        }
                                    } 
                                    else
                                    {
                                        buffer.Add( sentinelData.AIType.DisplayName ).Add( " " );
                                    }
                                }
                            }
                        }
                        debugStage = 16;
                        if ( entityType.OverrideFactionName.Length > 0 )
                        {
                            buffer.Add( entityType.OverrideFactionName );
                        } else
                        {
                            buffer.Add( owningFactionOrNull.GetDisplayName() );
                        }
                        if ( owningFactionOrNull.Type == FactionType.Player )
                        {
                            if ( relatedMemFleetOrNull != null )
                                buffer.EndColor().Add( " 舰队 " ).AddFactionColoredString( relatedMemFleetOrNull.GetName(), owningFactionOrNull );
                            if ( fedFromCityFleetOrNull != null )
                            {
                                buffer.EndColor().Add( " 来自 " ).AddFactionColoredString( fedFromCityFleetOrNull.GetName(), owningFactionOrNull );
                            }
                        }
                        if ( entity.ExoGalacticAttackPlanetIdx != -1 )
                            buffer.Add( " (星系外打击部队)" );
                        buffer.EndColor();
                    }
                }
                debugStage = 17;
                if ( GameSettings.Current.GetBoolBySetting( "ShowEntityIDInHovertext" ) )
                {
                    buffer.Add( FontSizes.BASE_SIZE_STRING ).StartColor( "33dd33" ).Add( " ID-" ).Add( entity.PrimaryKeyID ).EndColor().EndSize();
                }
                if ( GameSettings.Current.GetBoolBySetting( "Debug_WriteFleetIDInTooltips" ) )
                {
                    buffer.Add( FontSizes.BASE_SIZE_STRING ).StartColor( "3333dd" ).Add( " FLEET-" ).Add( entity.GetFleetID_Safe() ).EndColor().EndSize();
                }

                if ( GameSettings.Current.GetBoolBySetting( "ShowEntityLocationInHovertext" ) )
                {
                    buffer.Add( FontSizes.SLIGHTLY_SMALLER_SIZE_STRING ).StartColor( "87dd33" ).Add( " Loc: " ).Add( entity.WorldLocation.X - 400000 )
                        .Add( "," ).Add( entity.WorldLocation.Y - 400000 ).EndColor().EndSize();
                }
                #endregion

                debugStage = 18;
                buffer.Add( "</b>" ).EndSize().NewLine();

                if ( isShowingStrengthsAndWeaknesses && owningFactionOrNull != null && localPlayerFactionOrNull != null )
                {
                    buffer.Add( "\n" );

                    if ( owningFactionOrNull.GetIsHostileTowards( localPlayerFactionOrNull ) )
                    {
                        WriteWeakAgainst( buffer, relatedEntityTypeData, markStats );
                    } else
                    {
                        WriteStrongAgainst( buffer, relatedEntityTypeData, thisPlanetOrNull );
                    }
                    return true;
                }
                debugStage = 1801;

                cpi.ResetPos();
                buffer.ToPos( cpi.AddCustomFloat( baseOffsetBeforeSteps ) );

                #region Vars
                int hullMax;
                int shieldMax;
                int hullCurr;
                int shieldCurr;
                FInt hullPercent;
                FInt shieldPercent;
                int metalCurr;
                int energyCurr;
                int storedMetal = int.MaxValue;
                int storedEnergy = int.MaxValue;
                int storedArgon = int.MaxValue;
                int storedRadon = int.MaxValue;
                int storedXenon = int.MaxValue;
                bool entityCanBeClaimed;
                bool isUnderConstruction;
                bool isCenterpiece;
                debugStage = 1802;
                entityCanBeClaimed = entity.HasNotYetBeenFullyClaimed && (owningFactionOrNull == null || owningFactionOrNull.Type == FactionType.NaturalObject || owningFactionOrNull.Type == FactionType.Player);
                debugStage = 1803;
                isUnderConstruction = entity.SelfBuildingMetalRemaining > FInt.Zero;
                debugStage = 1804;
                isCenterpiece = fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.Centerpiece.GetSquad() == entity;
                debugStage = 1805;
                hullCurr = entity.GetCurrentHullPoints();
                shieldCurr = entity.GetCurrentShieldPoints();
                if ( IsBeingDrawnInPopupWindowRatherThanTooltip )
                {
                    hullMax = markStats.BaseHullPoints;
                    shieldMax = markStats.BaseShieldPoints;
                }
                else
                {
                    hullMax = entity.GetMaxHullPoints();
                    shieldMax = entity.GetMaxShieldPoints();
                }

                hullPercent = FInt.Create( hullCurr, true ).ToPercent( hullMax );
                shieldPercent = FInt.Create( shieldCurr, true ).ToPercent( shieldMax );
                if ( entityCanBeClaimed )
                {
                    metalCurr = entity.DataForMark.MetalCostToClaim;
                } else
                {
                    metalCurr = entity.GetMetalCost();
                }

                energyCurr = entity.GetEnergyUsage();
                if ( localPlayerFactionOrNull != null && (entityCanBeClaimed || localPlayerFactionOrNull.NetEnergy < 0 || panelMode == Window_InGameHoverEntityInfo.Mode.Build) )
                {
                    storedMetal = localPlayerFactionOrNull.StoredMetal.IntValue;
                    storedEnergy = localPlayerFactionOrNull.NetEnergy;
                    storedArgon = localPlayerFactionOrNull.NetFuelArgon;
                    storedRadon = localPlayerFactionOrNull.NetFuelRadon;
                    storedXenon = localPlayerFactionOrNull.NetFuelXenon;
                }

                int strengthCurr = entity.GetStrengthPerSquad( false );
                int strengthTotal = 0;
                string strengthMode = null;
                string strengthModeColorHex = null;
                if ( !isUsingFakeEntityAsStandin && !IsBeingDrawnInPopupWindowRatherThanTooltip )
                {
                    if ( OptionalCountToShow > 1 ) {
                        strengthTotal = strengthCurr;
                        strengthCurr *= OptionalCountToShow;
                        strengthMode = "1x";
                        strengthModeColorHex = "ffffff";
                    } 
                    else if ( entity.ExtraStackedSquadsInThis > 0 )
                    {
                        strengthTotal = strengthCurr;
                        strengthCurr *= 1 + entity.ExtraStackedSquadsInThis;
                        strengthMode = "1x";
                        strengthModeColorHex = "ffffff";
                    } 
                    else
                    {
                        strengthTotal = entity.GetStrengthOfContentsIfAny();
                        if ( strengthTotal > 0 )
                        {
                            strengthMode = "+T";
                            strengthModeColorHex = ColorMath.IceBlue.GetHexCode();
                        }
                    }
                }
                #endregion

                debugStage = 5000;

                if (relatedEntityTypeData.HideStatBlock)
                {
                    
                }
                else
                {
                    #region SecondRow: Useful Stats

                    #region Hull
                    if ( useText )
                    {
                        buffer.Add( "船体: " );
                    }
                    if (hullMax == 0)
                    {
                        buffer.WrapHull( "-", useIcons, false );
                    }
                    else
                    {
                        buffer.WrapHullTruncated( hullCurr, useIcons, false );
                        if ( detailLevel >= TooltipDetail.Medium && !isUsingFakeEntityAsStandin )
                        {
                            buffer.Add( " / " ).WrapHullTruncated( hullMax, false, false );
                        }
                        buffer.Add( " " ).AddSize_Tiny().AddPercentageInColor( hullPercent, true, false ).EndSize();
                    }
                    #endregion

                    debugStage = 6000;

                    cpi.AddStep_HundredFiftyPercent();
                    if ( useText )
                        cpi.AddStep_Quarter();
                    buffer.ToPos( cpi );

                    #region Shield
                    if ( useText )
                    {
                        buffer.Add( "护盾: " );
                    }
                    if (shieldMax == 0)
                    {
                        buffer.WrapShield( "-", useIcons, false );
                    }
                    else
                    {
                        buffer.WrapShieldTruncated( shieldCurr, useIcons, false );
                        if ( detailLevel >= TooltipDetail.Medium && !isUsingFakeEntityAsStandin )
                        {
                            buffer.Add( " / " ).WrapShieldTruncated( shieldMax, false, false );
                        }
                        buffer.Add( " " ).AddSize_Tiny().AddPercentageInColor( shieldPercent, true, true ).EndSize();
                    }
                    #endregion

                    cpi.AddStep_HundredTwentyFivePercent();
                    if ( useText )
                        cpi.AddStep_FourtyPercent();
                    if ( detailLevel >= TooltipDetail.Medium && !( panelMode == Window_InGameHoverEntityInfo.Mode.Build ) )
                        cpi.AddStep_Half();
                    buffer.ToPos( cpi );


                    if ( IsBeingDrawnInPopupWindowRatherThanTooltip || entityCanBeClaimed || panelMode == Window_InGameHoverEntityInfo.Mode.Build )
                    {
                        #region Popup Display Mass/Armor
                        buffer.AddSize_Small();

                        if ( useText )
                        {
                            buffer.Add( "质量: " );
                        }
                        buffer.StartMassWrapper( useIcons ).Add( entityType.Mass_tX ).Add( " tX" ).EndMassWrapper( false );

                        cpi.AddStep().AddStep_Tenth();
                        if ( useText )
                            cpi.AddStep_FourtyPercent();
                        buffer.ToPos( cpi );

                        if ( useText )
                        {
                            buffer.Add( "护甲: " );
                        }
                        buffer.StartArmorWrapper( useIcons ).Add( entityType.Armor_mm ).Add( " mm" ).EndArmorWrapper( false );

                        #endregion
                    
                        buffer.EndSize();
                    } else
                    {
                        #region Metal
                        if ( showMetalCost )
                        {
                            if ( useText )
                            {
                                buffer.Add( "金属: " );
                            }
                            if ( metalCurr == 0 )
                            {
                                buffer.WrapMetal( "-", useIcons, false );
                            } else
                            {
                                buffer.WrapMetalTruncated( metalCurr, useIcons, false );
                            }
                        }
                        #endregion

                        debugStage = 8000;

                        cpi.AddStep_EightyPercent();
                        if ( useText )
                            cpi.AddStep_FourtyPercent();
                        buffer.ToPos( cpi );

                        #region Energy
                        if ( showEnergyConsumption )
                        {
                            if ( useText )
                            {
                                buffer.Add( "能量: " );
                            }
                            if ( energyCurr == 0 )
                            {
                                buffer.Add( "-" );
                            } else
                            {
                                buffer.WrapEnergyTruncated( energyCurr, useIcons, false );
                            }
                        }
                        #endregion

                        debugStage = 8500;
                    
                        cpi.AddStep_EightyPercent();
                        if ( useText )
                            cpi.AddStep_Half();
                        buffer.ToPos( cpi );

                        #region Fuel
                        if ( World_AIW2.Instance.IsFuelEnabled && relatedEntityTypeData.FuelUse > 0 )
                        {
                            if ( useText )
                            {
                                if ( relatedEntityTypeData.FuelUseType == ResourceType.FuelArgon )
                                {
                                    buffer.Add( "氩气: " );
                                } else if ( relatedEntityTypeData.FuelUseType == ResourceType.FuelRadon )
                                {
                                    buffer.Add( "氡气: " );
                                } else if ( relatedEntityTypeData.FuelUseType == ResourceType.FuelXenon )
                                {
                                    buffer.Add( "氙气: " );
                                }
                            }
                            if ( energyCurr == 0 )
                            {
                                buffer.WrapGenericResource( "-", relatedEntityTypeData.FuelUseType, useIcons, false );
                            } else
                            {
                                buffer.WrapGenericResourceTruncated( relatedEntityTypeData.FuelUse, relatedEntityTypeData.FuelUseType, useIcons, false );
                            }
                        }
                        #endregion
                    }
                    /* Labels are now controlled by settings instead of detail level
                    if ( detailLevel < TooltipDetail.Medium )
                    {
                        cpi.AddStep_SixtyPercent();
                        if ( useText )
                            cpi.AddStep_Half();
                        buffer.ToPos( cpi );
                        buffer.Add( "<size=80%><color=#525d66>Hold <color=#577287>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" ) );
                        buffer.Add( "</color> + <color=#577287>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key2" ) );
                        buffer.Add( "</color> to see field labels.</color></size>" );
                    }*/
                    #endregion
                    debugStage = 9000;

                    buffer.NewLine();
                    cpi.ResetPos();
                    buffer.ToPos( cpi.AddCustomFloat( baseOffsetBeforeSteps ) );

                    #region ThridRow: Strength, Speed but also the 4 Useless Stats
                    #region AIP/Strength
                    if ( panelMode == Window_InGameHoverEntityInfo.Mode.Build || entityCanBeClaimed )
                    {
                        #region AIP
                        if ( entityCanBeClaimed )
                        {
                            if ( entityType.AIPToClaim != FInt.Zero )
                            {
                                if ( useText )
                                {
                                    buffer.Add( "占领AIP: " );
                                }
                                if ( entityType.AIPToClaim > FInt.Zero )
                                {
                                    buffer.WrapAIPMoreReadable( entityType.AIPToClaim, useIcons, false );
                                } else
                                {
                                    buffer.WrapAIPReductionMoreReadable( entityType.AIPToClaim, useIcons, false );
                                }
                            }
                        } 
                        else if ( entityType.AIPWhenGrantedByHack != FInt.Zero )
                        {
                            if ( useText )
                            {
                                buffer.Add( "入侵AIP: " );
                            }
                            if ( entityType.AIPWhenGrantedByHack > FInt.Zero )
                            {
                                buffer.WrapAIPMoreReadable( entityType.AIPWhenGrantedByHack, useIcons, false );
                            } else
                            {
                                buffer.WrapAIPReductionMoreReadable( entityType.AIPWhenGrantedByHack, useIcons, false );
                            }
                        }
                        #endregion
                    } 
                    else
                    {
                        debugStage = 11000;
                        #region Strength
                        if ( useText )
                        {
                            buffer.Add( "战斗力: " );
                        }
                        buffer.WrapStrengthTruncated( strengthCurr, useIcons, false );
                        if ( strengthMode != null )
                        {
                            buffer.Add( " " ).AddSize_Tiny().Add( "(" ).StartStrengthWrapper( useIcons ).AddNumberTruncated( FInt.Create( strengthTotal, false ) ).EndColor().Add( " " ).Add( strengthMode ).Add( ")" ).EndSize();
                        }
                        #endregion
                    }
                    #endregion

                    cpi.AddStep_HundredFiftyPercent();
                    if ( useText )
                        cpi.AddStep_Quarter();
                    buffer.ToPos( cpi );

                    debugStage = 12000;

                    #region Speed
                    int speedCurr;
                    int speedBase;
                    FInt speedPercent;
                    if ( isUsingFakeEntityAsStandin )
                    {
                        speedCurr = entity.CalculateSpeed( false );//since these ships don't calculate speed on their own
                    } else
                    {
                        speedCurr = entity.CalculatedSpeed;
                    }
                    speedBase = markStats.Speed;
                    if ( speedBase > 0 )
                    {
                        if ( useText )
                            buffer.Add( "速度: " );
                        buffer.WrapSpeedMoreReadable( speedCurr, useIcons, false );
                        if ( speedCurr != speedBase && detailLevel >= TooltipDetail.Medium && !( IsBeingDrawnInPopupWindowRatherThanTooltip || panelMode == Window_InGameHoverEntityInfo.Mode.Build ) )
                        {
                            speedPercent = FInt.Create( speedCurr, true ).ToPercent( speedBase );
                            if ( detailLevel >= TooltipDetail.Medium )
                            {
                                buffer.Add( " / " ).WrapSpeedMoreReadable( speedBase, false, false );
                            }
                            buffer.Add( " " ).AddSize_Tiny().AddPercentageInColor( speedPercent, true, false ).EndSize();
                        }
                    } else if ( relatedEntityTypeData.OrbitsGravityWellCenterAtItsCurrentRadius ||
                        relatedEntityTypeData.OrbitsParentAtRange > 0 || relatedEntityTypeData.OrbitsFlagshipAtRange > 0 )
                    {
                        if ( useText )
                            buffer.Add( "轨道: " );
                        buffer.StartSpeedWrapper( false ).AddFIntTruncated( relatedEntityTypeData.DegreesToOrbitPerSecond );
                        if ( useText )
                        {
                            buffer.Add( " deg/s" );
                        } else
                        {
                            buffer.Add( " °" );
                        }
                        buffer.EndSpeedWrapper( false );
                    } else
                    {
                        if ( useText )
                            buffer.Add( "速度: " );
                        buffer.WrapSpeed( "-", useIcons, false );
                    }
                    #endregion

                    debugStage = 13000;

                    cpi.AddStep_HundredTwentyFivePercent();
                    if ( useText )
                        cpi.AddStep_FourtyPercent();
                    if ( detailLevel >= TooltipDetail.Medium && !(IsBeingDrawnInPopupWindowRatherThanTooltip || panelMode == Window_InGameHoverEntityInfo.Mode.Build) )
                        cpi.AddStep_Half();
                    buffer.ToPos( cpi );

                    #region Useless Stats
                    if ( IsBeingDrawnInPopupWindowRatherThanTooltip || entityCanBeClaimed || panelMode == Window_InGameHoverEntityInfo.Mode.Build )
                    {
                        buffer.AddSize_Small();
                        if ( useText )
                        {
                            buffer.Add( "引擎: " );
                        }
                        buffer.StartEngineWrapper( useIcons );
                        if ( entityType.Engine_gx > 0 )
                        {
                            buffer.Add( entityType.Engine_gx ).Add( " gX" );
                        } else
                        {
                            buffer.Add( " -" );
                        }
                        buffer.EndEngineWrapper( false );

                        cpi.AddStep().AddStep_Tenth();
                        if ( useText )
                            cpi.AddStep_FourtyPercent();
                        buffer.ToPos( cpi );

                        if ( useText )
                        {
                            buffer.Add( "反照率: " );
                        }
                        buffer.WrapAlbedo( markStats.Albedo, useIcons, false );
                    
                    } else
                    {
                        buffer.AddSize_Small();
                        if ( useText )
                        {
                            buffer.Add( "引擎: " );
                        }
                        buffer.StartEngineWrapper( useIcons );
                        if ( entityType.Engine_gx > 0 )
                        {
                            buffer.Add( entityType.Engine_gx ).Add( " gX" );
                        } else
                        {
                            buffer.Add( " -" );
                        }
                        buffer.EndEngineWrapper( false );

                        cpi.AddStep_EightyPercent();
                        if ( useText )
                            cpi.AddStep_FourtyPercent();
                        buffer.ToPos( cpi );

                        if ( useText )
                        {
                            buffer.Add( "质量: " );
                        }
                        buffer.StartMassWrapper( useIcons ).Add( entityType.Mass_tX ).Add( " tX" ).EndMassWrapper( false );

                        cpi.AddStepFraction( 0.62f );
                        if ( useText )
                            cpi.AddStep_TwentyPercent();
                        buffer.ToPos( cpi );

                        if ( useText )
                        {
                            buffer.Add( "反照率: " );
                        }
                        buffer.WrapAlbedo( markStats.Albedo, useIcons, false );

                        cpi.AddStep_SixtyPercent();
                        if ( useText )
                            cpi.AddStepFraction( 0.23f );
                        buffer.ToPos( cpi );

                        if ( useText )
                        {
                            buffer.Add( "护甲: " );
                        }
                        buffer.StartArmorWrapper( useIcons ).Add( entityType.Armor_mm ).Add( " mm" ).EndArmorWrapper( false );
                    }
                    #endregion

                    buffer.EndSize();
                        #endregion
                }

                debugStage = 14001;

                #region FourthRow: Claim/Construction printouts, orders
                //if ( detailLevel >= TooltipDetail.Medium )
                {
                    #region Claiming
                    if ( !isUsingFakeEntityAsStandin ) //only do this for real entities.  The build menu stuff happens here for fake ones
                    {
                        if ( entityCanBeClaimed && owningFactionOrNull != null )
                        {
                            buffer.EndColor().NewLine();
                            cpi.ResetPos();
                            buffer.Add( "占领: " ).AddSize_Small();
                            if ( entity.IsInHoldFireMode )
                            {
                                buffer.Add( "已暂停", "cccccc" );
                            } else
                            {
                                Faction forFaction;
                                if ( owningFactionOrNull.Type != FactionType.Player )
                                {
                                    forFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                                } else
                                {
                                    forFaction = owningFactionOrNull;
                                }
                                if ( forFaction != null )
                                {
                                    FactionType planetController = entity.Planet == null ? FactionType.NaturalObject : entity.Planet.GetControllingFactionType();
                                    if ( planetController == FactionType.NaturalObject )
                                    {
                                        buffer.Add( "需要星球控制权", "bbbbbb" );
                                    } else if ( planetController != FactionType.Player )
                                    {
                                        buffer.Add( "被敌人阻断", "aa3333" );
                                    } else if ( forFaction.SecondsSinceBrownout > 0 )
                                    {
                                        buffer.Add( "已停止（电力不足）", "ff2222" );
                                    } else if ( forFaction.NetEnergy < entityType.EnergyUsage )
                                    {
                                        buffer.Add( "已停止（能量不足）", "cc2222" );
                                    } else if ( World_AIW2.Instance.IsFuelEnabled && relatedEntityTypeData.FuelUseType == ResourceType.FuelArgon && forFaction.NetFuelArgon < entityType.FuelUse )
                                    {
                                        buffer.Add( "已停止（氩气不足）", "cc2222" );
                                    } else if ( World_AIW2.Instance.IsFuelEnabled && relatedEntityTypeData.FuelUseType == ResourceType.FuelRadon && forFaction.FuelRadonConsumption < entityType.FuelUse )
                                    {
                                        buffer.Add( "已停止（氡气不足）", "cc2222" );
                                    } else if ( World_AIW2.Instance.IsFuelEnabled && relatedEntityTypeData.FuelUseType == ResourceType.FuelXenon && forFaction.FuelXenonConsumption < entityType.FuelUse )
                                    {
                                        buffer.Add( "已停止（氙气不足）", "cc2222" );
                                    } else if ( hullCurr == 1 )
                                    {
                                        buffer.Add( "就绪", "55dd55" );
                                    } else
                                    {
                                buffer.AddPercentageInColor( hullPercent, true, false ).Add( " 完成" );
                                if ( detailLevel == TooltipDetail.Full )
                                {
                                    buffer.Add( " (" );
                                    if ( useText )
                                        buffer.Add( "金属: " );
                                            buffer.WrapMetalTruncated( metalCurr * hullPercent / 100, useIcons, false ).Add( " / " ).WrapMetalTruncated( metalCurr, false, false ).Add( ")" );
                                        }
                                    }
                                }
                            }
                            buffer.EndSize();
                        }
                        #endregion
                        #region Construction
                        else if ( isUnderConstruction )
                        {
                            buffer.EndColor().NewLine();
                            cpi.ResetPos();
                            buffer.Add( "建造中: " ).AddSize_Small();
                            if ( entity.IsInHoldFireMode )
                            {
                                buffer.Add( "已暂停", "cccccc" );
                            } else
                            {
                                buffer.AddPercentageInColor( (metalCurr - entity.SelfBuildingMetalRemaining).ToPercent( metalCurr ), true, false ).Add( " 完成" );
                            }
                            if ( detailLevel == TooltipDetail.Full )
                            {
                                buffer.Add( " (" );
                                if ( useText )
                                    buffer.Add( "金属: " );
                                buffer.WrapMetalTruncated( metalCurr - entity.SelfBuildingMetalRemaining, useIcons, false ).Add( " / " ).WrapMetalTruncated( metalCurr, false, false ).Add( ")" );
                            }
                            buffer.EndSize();
                        }
                        #endregion
                        #region Behavior
                        // immobile entities don't really have different behaviors, they just sit there
                        // ... unless they are player owned and its useful to know if they standing down
                        //     though thats already conveyed in several other ways (the button state, the selection circle color...)
                        else
                        {
                            buffer.EndColor().NewLine();
                            cpi.ResetPos();
                            buffer.Add( "行为: " ).AddSize_Small();
                            if ( entity.IsInHoldFireMode )
                            {
                                buffer.Add( "待命", "444499" );
                            } 
                            else
                            {
                                switch ( entity.Orders.Behavior )
                                {
                                    case EntityBehaviorType.Attacker_Full:
                                        if ( markStats.Computed_BaseLongestWeaponRange <= 0 )
                                        {
                                            if ( isCenterpiece )
                                            {
                                                buffer.Add( "指挥中", "eeee88" );
                                            } 
                                            else
                                            {
                                                if ( entityFaction != null && entityFaction.Type == FactionType.Player )
                                                    buffer.Add( "追击模式", Window_InGameSelectionInfo.color_Pursuit );
                                                else
                                                    buffer.Add( "巡逻", "eeee88" );
                                            }
                                        } 
                                        else
                                        {
                                            if ( entityFaction != null && entityFaction.Type == FactionType.Player )
                                                buffer.Add( "追击模式", Window_InGameSelectionInfo.color_Pursuit );
                                            else
                                                buffer.Add( "全面攻击", "ee8888" );
                                        }
                                        break;
                                    case EntityBehaviorType.Attacker_PursueOnlyInRange:
                                        buffer.Add( "攻击移动", Window_InGameSelectionInfo.color_AttackMove );
                                        break;
                                    case EntityBehaviorType.Guard_FleetShip:
                                        buffer.Add( "保护盟友", "88ee88" );
                                        break;
                                    case EntityBehaviorType.Guard_Guardian_Patrolling:
                                        buffer.Add( "巡逻区域", "88ee88" );
                                        break;
                                    case EntityBehaviorType.Guard_Guardian_Anchored:
                                        buffer.Add( "防御阵地", "88ee88" );
                                        break;
                                    case EntityBehaviorType.Stationary:
                                    case EntityBehaviorType.None:
                                        if ( entity.TypeData.IsMobile == false )
                                        {
                                            buffer.Add( "静止", "777777" );
                                        }
                                        else
                                        {
                                            buffer.Add( "防御中", "88ee88" );
                                        }
                                        break;
                                }
                            }
                            buffer.EndSize();
                            //if (detailLevel == TooltipDetail.Full )
                                //buffer.ToPos( cpi.AddStep_HundredFiftyPercent() );
                            //else
                                buffer.ToPos( cpi.AddStep().AddStep() );
                            #endregion
                            #region Orders
                            //if ( detailLevel >= TooltipDetail.Medium )
                            {
                                EntityOrder order = entity.Orders.GetQueuedOrderAtIndex_OrNull( 0 );
                                if ( order.TypeData != null )
                                {
                                    buffer.Add( "指令: " ).AddSize_Small();
                                    int queuedOrderCount = entity.Orders.GetQueuedOrderCount();
                                    Window_PrototypeInGameHoverEntityInfoUtils.WriteEntityOrder( entity, order, buffer );

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
                                        order = entity.Orders.GetQueuedOrderAtIndex_OrNull( i );
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
                                                Window_PrototypeInGameHoverEntityInfoUtils.WriteEntityOrder( entity, order, buffer );
                                            }
                                            consecutivePathing++;
                                            count++;
                                            lastCyclePlanetIndex = order.RelatedPlanetIndex;
                                            continue;//skip ahead, no need to show every planet individually!
                                        } else
                                        {
                                            if ( consecutivePathing > 1 )
                                            {
                                                buffer.Add( " (" ).Add( consecutivePathing ).Add( " 跳跃)" );
                                            }
                                            consecutivePathing = 0;
                                        }
                                        if ( i >= count )
                                        {
                                            break;
                                        }
                                        buffer.Add( ", " );
                                        Window_PrototypeInGameHoverEntityInfoUtils.WriteEntityOrder( entity, order, buffer );
                                    }
                                    if ( queuedOrderCount - i > 0 )
                                    {
                                        buffer.Add( " + " ).Add( queuedOrderCount - i ).Add( " 更多" );
                                    }
                                    buffer.EndSize();
                                }
                            }
                            #endregion
                        }
                    }
                }
                #endregion

                debugStage = 15001;

                #region FithRow: Buffs
                int entityTimeOnPlanet = entity.GetSecondsSinceEnteringThisPlanet();//reuse in later stages for weapon damage modifiers, if needed
                FInt speedMultiplierWhileHacking = ExternalConstants.Instance.SpeedMultiplierWhileHacking;
                if ( detailLevel >= TooltipDetail.Medium )
                {
                    debugStage = 15001801;
                    bool wroteBuffStart = false;
                    #region Lye
                    //Lye = negative damage amplification, which is called Acid here
                    if ( entity.IncomingDamageAmplifiedDuration_Max15 > 0 &&
                        (entity.IncomingDamageAmplifiedByFlat < 0 || entity.IncomingDamageAmplifiedByMult > FInt.Zero && entity.IncomingDamageAmplifiedByMult < FInt.One) )
                    {
                        debugStage = 15001811;
                        Window_PrototypeInGameHoverEntityInfoUtils.WriteBuffsStartIfNeeded( buffer, ref wroteBuffStart );
                        buffer.Add( "腐蚀液: (" ).Add( entity.IncomingDamageAmplifiedDuration_Max15 ).Add( "s, " );
                        if ( entity.IncomingDamageAmplifiedByFlat < 0 )
                        {
                            buffer.AddNumberMoreReadable( entity.IncomingDamageAmplifiedByFlat );
                        }
                        if ( entity.IncomingDamageAmplifiedByMult > FInt.Zero && entity.IncomingDamageAmplifiedByMult < FInt.One )
                        {
                            if ( entity.IncomingDamageAmplifiedByFlat < 0 )
                            {
                                buffer.Add( " / " );
                            }
                            buffer.AddPercentRoundedDynamically( (entity.IncomingDamageAmplifiedByMult - 1) * 100 );
                        }
                        buffer.Add( ")" );
                    }
                    #endregion

                    debugStage = 15001821;

                    #region Hull
                    bool wroteHullBuffStart = false;
                    if ( markStats.BaseHullPoints > 0 )
                    {
                        debugStage = 15001831;
                        if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.HullMultiplier > FInt.One && !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteHullBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteHullBuffStart );
                            buffer.Add( "舰队加成: +" ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.HullMultiplier - 1) * 100 );
                        }
                    }
                    #endregion

                    debugStage = 15001841;

                    #region Shield
                    if ( markStats.BaseShieldPoints > 0 )
                    {
                        debugStage = 15001851;

                        bool wroteShieldBuffStart = false;
                        if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.ShieldsMultiplier > FInt.One && !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteShieldBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteShieldBuffStart );
                            buffer.Add( "舰队加成: +" ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.ShieldsMultiplier - 1) * 100 );
                        }
                    }
                    #endregion

                    debugStage = 15001861;

                    #region Damage
                    if ( markStats.Computed_BaseLongestWeaponRange > 0 || markStats.AttritionDamagePreFleetModifiers > 0 )
                    {
                        debugStage = 15001871;

                        bool wroteDamageBuffStart = false;
                        if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.AttackPowerMultiplier > FInt.One && !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDamageBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteDamageBuffStart );
                            buffer.Add( "舰队加成: +" ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.AttackPowerMultiplier - 1) * 100 );
                        }

                        if ( markStats.Computed_BaseLongestWeaponRange > 0 && entity.PlanetFaction != null && entity.PlanetFaction.CurrentAttackMultiplier > FInt.One )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDamageBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteDamageBuffStart );
                            buffer.Add( "阵营星球加成: +" ).AddPercentRoundedDynamically( (entity.PlanetFaction.CurrentAttackMultiplier - 1) * 100 );
                        }

                        if ( entity.TypeData.AmountAddedToDamagePerShipOfThisTypeOnPlanet > 0 && entity.CalculatedAddedDamage > 0 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDamageBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteDamageBuffStart );
                            buffer.Add( "网络: +" ).AddNumberTruncated( entity.CalculatedAddedDamage );
                        }

                        if ( entity.NumberOfWeaponPoints > 0 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDamageBuffsStartIfNeeded( buffer, false, ref wroteBuffStart, ref wroteDamageBuffStart );
                            buffer.Add( "武器点数: " ).AddNumberMoreReadable( entity.NumberOfWeaponPoints );
                        }
                    }
                    #endregion

                    debugStage = 15001881;

                    #region Speed
                    if ( entityType.IsMobile )
                    {
                        debugStage = 15001891;

                        bool wroteSpeedBuffStart = false;
                        if ( entity.SpeedLimitFromGroupMove > 0 )
                        {
                            if ( entity.SpeedLimitFromGroupMove > markStats.Speed )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteSpeedBuffStart );
                                if ( owningFactionOrNull != null && owningFactionOrNull.Type == FactionType.Player )
                                {
                                    buffer.Add( "编队移动: +" );
                                } else
                                {
                                    buffer.Add( "速度编队: +" );
                                }
                                buffer.AddNumberMoreReadable( entity.SpeedLimitFromGroupMove - markStats.Speed );
                            }
                        } else if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.OverridingMinSpeed > markStats.Speed &&
                            !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet && !isCenterpiece )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "舰队加成: +" ).AddNumberMoreReadable( fleetMembershipOrNull.Fleet.OverridingMinSpeed - markStats.Speed );
                        }

                        if ( entity.PlanetFaction != null && (entity.PlanetFaction.CurrentSpeedMultiplier > FInt.One || entity.PlanetFaction.CurrentSpeedFlatBonus > 0) )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "阵营星球加成: " );
                            if ( entity.PlanetFaction.CurrentSpeedMultiplier > FInt.One )
                            {
                                buffer.Add( "+ " ).AddPercentRoundedDynamically( (entity.PlanetFaction.CurrentSpeedMultiplier - 1) * 100 );
                                if ( entity.PlanetFaction.CurrentSpeedFlatBonus > 0 )
                                    buffer.Add( ", " );
                            }
                            if ( entity.PlanetFaction.CurrentSpeedFlatBonus > 0 )
                                buffer.Add( "+ " ).Add( entity.PlanetFaction.CurrentSpeedFlatBonus );
                        }

                        if ( entity.Planet != null && entity.Planet.UnitSpeedupPercentage > 0 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "星球加成: " ).AddPercentRoundedDynamically( entity.Planet.UnitSpeedupPercentage, 100 );
                        }

                        if ( entityType.SpeedMultiplierFirst5SecondsOnPlanet > FInt.One && entityTimeOnPlanet <= 5 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "快速部署: +" ).AddPercentRoundedDynamically( (entityType.SpeedMultiplierFirst5SecondsOnPlanet - 1) * 100 );
                            if ( detailLevel == TooltipDetail.Full )
                            {
                                buffer.Add( " (" ).Add( 6 - entityTimeOnPlanet ).Add( "s)" );
                            }
                        }

                        if ( !isCenterpiece && !relatedEntityTypeData.IsDrone )
                        {
                            EntityOrder order = entity.Orders.GetQueuedOrderAtIndex_OrNull( 0 );
                            if ( order.TypeData != null && order.TypeData.Type == EntityOrderType.GetIntoTransport )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteSpeedBuffStart );
                                buffer.Add( "装载中: +" ).AddPercentRoundedDynamically( 2, 1 );
                            }
                        }

                        if ( isCenterpiece && entity.ActiveHack != null && speedMultiplierWhileHacking > FInt.One )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "黑客加成: " ).AddPercentRoundedDynamically( (speedMultiplierWhileHacking - 1) * 100 );
                        }

                        if ( owningFactionOrNull != null && entity.Planet != null && entity.Planet.IsFimbulwintered && owningFactionOrNull.BenefitsFromFimbulwinter )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteSpeedBuffStart );
                            buffer.Add( "芬布尔之冬: +" ).AddPercentRoundedDynamically( ExternalConstants.Instance.FimbulwinterSpeedupPercent, 100 );
                        }
                    }

                    #endregion

                    debugStage = 15001901;

                    #region Range
                    if ( entity.TypeData.AmountAddedToRangePerShipOfThisTypeOnPlanet > 0 )
                    {
                        debugStage = 15001911;
                        bool wroteRangeBuffStart = false;
                        if ( fleetMembershipOrNull != null && entity.CalculatedAddedRange > 0 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteRangeBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteRangeBuffStart );
                            buffer.Add( "网络: +" ).AddNumberTruncated( entity.CalculatedAddedRange );
                        }
                    }
                    #endregion

                    debugStage = 15001921;

                    #region Cloak
                    if ( entity.GetMightPossiblyBeCloaked() )
                    {
                        debugStage = 15001931;

                        bool wroteCloakBuffStart = false;
                        int cloak = entity.GetCurrentCloakingPoints();
                        int cloakMax = entity.GetMaxCloakingPoints();

                        Window_PrototypeInGameHoverEntityInfoUtils.WriteCloakBuffsStartIfNeeded( buffer, useIcons, ref wroteBuffStart, ref wroteCloakBuffStart );
                        buffer.AddNumberTruncated( cloak ).Add( " / " ).AddNumberTruncated( cloakMax );
                    }
                    #endregion

                    if ( wroteBuffStart )
                    {
                        buffer.EndSize();
                    }
                }
                #endregion

                debugStage = 16001;

                #region SixthRow: Debuffs
                if ( detailLevel >= TooltipDetail.Medium )
                {
                    bool wroteDebuffStart = false;
                    #region Acid
                    if ( entity.IncomingDamageAmplifiedDuration_Max15 > 0 && (entity.IncomingDamageAmplifiedByFlat > 0 || entity.IncomingDamageAmplifiedByMult > FInt.One) )
                    {
                        Window_PrototypeInGameHoverEntityInfoUtils.WriteDebuffsStartIfNeeded( buffer, ref wroteDebuffStart );
                        buffer.Add( "酸蚀: (" ).Add( entity.IncomingDamageAmplifiedDuration_Max15 ).Add( "s, +" );
                        if ( entity.IncomingDamageAmplifiedByFlat > 0 )
                        {
                            buffer.AddNumberMoreReadable( entity.IncomingDamageAmplifiedByFlat );
                        }
                        if ( entity.IncomingDamageAmplifiedByMult > FInt.One )
                        {
                            if ( entity.IncomingDamageAmplifiedByFlat > 0 )
                            {
                                buffer.Add( " / +" );
                            }
                            buffer.AddPercentRoundedDynamically( (entity.IncomingDamageAmplifiedByMult * 100) - 100 );
                        }
                        buffer.Add( ")" );
                    }
                    #endregion

                    #region Corrosion
                    if ( entity.CorrosionDamageToBeAppliedToMe > 0 )
                    {
                        Window_PrototypeInGameHoverEntityInfoUtils.WriteDebuffsStartIfNeeded( buffer, ref wroteDebuffStart );
                        buffer.Add( "腐蚀: " ).Add( entity.CorrosionDamageToBeAppliedToMe );
                    }
                    #endregion

                    #region Paralysis
                    if ( entity.CurrentParalysisSeconds > 0 )
                    {
                        Window_PrototypeInGameHoverEntityInfoUtils.WriteDebuffsStartIfNeeded( buffer, ref wroteDebuffStart );
                        buffer.Add( "麻痹: (" ).Add( entity.CurrentParalysisSeconds ).Add( "s)" );
                    }
                    #endregion

                    #region ReloadSlow
                    if ( entity.CurrentWeaponAddedReloadSeconds > 0 )
                    {
                        Window_PrototypeInGameHoverEntityInfoUtils.WriteDebuffsStartIfNeeded( buffer, ref wroteDebuffStart );
                        buffer.Add( "装填减速: (" ).Add( entity.CurrentWeaponAddedReloadSeconds ).Add( "s)" );
                    }
                    #endregion


                    #region Hull
                    bool wroteHullDebuffStart = false;
                    if ( markStats.BaseHullPoints > 0 )
                    {
                        if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.HullMultiplier < FInt.One && fleetMembershipOrNull.Fleet.HullMultiplier != FInt.Zero &&
                            !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteHullDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteHullDebuffStart );
                            buffer.Add( "舰队削弱: " ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.HullMultiplier * 100) - 100 );
                        }
                    }
                    #endregion

                    #region Shield
                    if ( markStats.BaseShieldPoints > 0 )
                    {
                        bool wroteShieldDebuffStart = false;
                        if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.ShieldsMultiplier < FInt.One && fleetMembershipOrNull.Fleet.ShieldsMultiplier != FInt.Zero &&
                            !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteShieldDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteShieldDebuffStart );
                            buffer.Add( "舰队削弱: " ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.ShieldsMultiplier * 100) - 100 );
                        }
                    }
                    #endregion

                    #region Damage
                    if ( markStats.Computed_BaseLongestWeaponRange > 0 || markStats.AttritionDamagePreFleetModifiers > 0 )
                    {
                        bool wroteDamageDebuffStart = false;
                        if ( fleetMembershipOrNull != null && fleetMembershipOrNull.Fleet.AttackPowerMultiplier < FInt.One && fleetMembershipOrNull.Fleet.AttackPowerMultiplier != FInt.Zero &&
                            !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDamageDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteDamageDebuffStart );
                            buffer.Add( "舰队削弱: " ).AddPercentRoundedDynamically( (fleetMembershipOrNull.Fleet.AttackPowerMultiplier * 100) - 100 );
                        }

                        if ( markStats.Computed_BaseLongestWeaponRange > 0 && entity.PlanetFaction != null && entity.PlanetFaction.CurrentAttackMultiplier < FInt.One && entity.PlanetFaction.CurrentAttackMultiplier != FInt.Zero &&
                            !relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDamageDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteDamageDebuffStart );
                            buffer.Add( "阵营星球削弱: " ).AddPercentRoundedDynamically( (entity.PlanetFaction.CurrentAttackMultiplier * 100) - 100 );
                        }

                        if ( !isUsingFakeEntityAsStandin && markStats.Computed_BaseLongestWeaponRange > 0 &&
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
                                if ( systemType.MaxMarkLevelToFunction < markLevel.Ordinal || systemType.MinMarkLevelToFunction > markLevel.Ordinal )
                                {
                                    continue;
                                }
                                if ( systemType.IsModule && !systemType.IsModuleOn( relatedMembershipOrNull ) )
                                {
                                    continue;
                                }
                                if ( systemType.MustBeThisStateOfMatterToBeEnabled != null && systemType.MustBeThisStateOfMatterToBeEnabled != entity.CurrentStateOfMatter )
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
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteDamageDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteDamageDebuffStart );
                                buffer.Add( "力场护盾下: -50%" );
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
                                    } else
                                    {
                                        buffer.Add( "某些武器" );
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
                        if ( entity.CurrentEngineStunSeconds > 0 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                            int index = entity.CurrentEngineStunSeconds;
                            if ( index >= ExternalConstants.Instance.EngineStunMultipliersByStunSeconds.Count )
                            {
                                index = ExternalConstants.Instance.EngineStunMultipliersByStunSeconds.Count - 1;
                            }
                            FInt slowFactor = ExternalConstants.Instance.EngineStunMultipliersByStunSeconds[index];
                            if ( slowFactor <= FInt.Zero )
                            {
                                buffer.Add( "引擎眩晕: (" ).Add( entity.CurrentEngineStunSeconds ).Add( "s, -100%)" );
                            } else
                            {
                                buffer.Add( "引擎减速: (" ).Add( entity.CurrentEngineStunSeconds ).Add( "s, " ).AddPercentRoundedDynamically( (slowFactor * 100) - 100 ).Add( ")" );
                            }
                        }
                        #endregion

                        if ( entity.CurrentCountOfTractorsPullingOnThis > 0 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                            buffer.Add( "被牵引光束捕获: -100%" );
                        } else
                        {
                            if ( entity.SpeedLimitFromGroupMove > 0 && entity.SpeedLimitFromGroupMove < markStats.Speed )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                if ( owningFactionOrNull != null && owningFactionOrNull.Type == FactionType.Player )
                                {
                                    buffer.Add( "编队移动: " );
                                } else
                                {
                                    buffer.Add( "速度编队: " );
                                }
                                buffer.AddNumberMoreReadable( entity.SpeedLimitFromGroupMove - markStats.Speed );
                            }

                            if ( entity.PlanetFaction != null && (entity.PlanetFaction.CurrentSpeedMultiplier < FInt.One || entity.PlanetFaction.CurrentSpeedFlatBonus < 0) )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                buffer.Add( "阵营星球削弱: " );
                                if ( entity.PlanetFaction.CurrentSpeedMultiplier < FInt.One )
                                {
                                    buffer.Add( " " ).AddPercentRoundedDynamically( (entity.PlanetFaction.CurrentSpeedMultiplier * 100) - 100 );
                                    if ( entity.PlanetFaction.CurrentSpeedFlatBonus > 0 )
                                        buffer.Add( ", " );
                                }
                                if ( entity.PlanetFaction.CurrentSpeedFlatBonus > 0 )
                                    buffer.Add( " " ).Add( entity.PlanetFaction.CurrentSpeedFlatBonus );
                            }

                            if ( entity.Planet != null && entity.Planet.UnitSlowPercentage > 0 )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                buffer.Add( "星球削弱: " ).AddPercentRoundedDynamically( -entity.Planet.UnitSlowPercentage, 100 );
                            }

                            if ( entityType.SpeedMultiplierFirst5SecondsOnPlanet < FInt.One && entityType.SpeedMultiplierFirst5SecondsOnPlanet != FInt.Zero && entityTimeOnPlanet <= 5 )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                buffer.Add( "抵达: " ).AddPercentRoundedDynamically( (entityType.SpeedMultiplierFirst5SecondsOnPlanet * 100) - 100 );
                                if ( detailLevel == TooltipDetail.Full )
                                {
                                    buffer.Add( " (for " ).Add( 6 - entityTimeOnPlanet ).Add( "s)" );
                                }
                            }

                            if ( isCenterpiece && entity.ActiveHack != null && speedMultiplierWhileHacking < FInt.One )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                            buffer.Add( "入侵: " ).AddPercentRoundedDynamically( (speedMultiplierWhileHacking - 1) * 100 );
                            }

                            if ( owningFactionOrNull != null && entity.Planet != null && entity.Planet.IsFimbulwintered && !owningFactionOrNull.BenefitsFromFimbulwinter )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                buffer.Add( "芬布尔之冬: " ).AddPercentRoundedDynamically( -ExternalConstants.Instance.FimbulwinterSlowdownPercent, 100 );
                            }

                            FInt grav = entity.CurrentGravitySpeedMultiplier.Display;
                            if ( grav < FInt.One && grav != FInt.Zero )
                            {
                                Window_PrototypeInGameHoverEntityInfoUtils.WriteSpeedDebuffsStartIfNeeded( buffer, useIcons, ref wroteDebuffStart, ref wroteSpeedDebuffStart );
                                buffer.Add( "重力: " ).AddPercentRoundedDynamically( grav * 100 );
                            }
                        }
                    }

                    #endregion

                    #region Cloak
                    if ( entity.IsFakeEntity == false && 
                         entity.GetMightPossiblyBeCloaked() )
                    {
                        bool wroteCloakBuffStart = false;
                        int cloak = entity.GetCurrentCloakingPoints();
                        int cloakMax = entity.GetMaxCloakingPoints();
                        if ( cloak == 0 && cloakMax > 0 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDecloakDebuffsStartIfNeeded( buffer, useIcons, true, ref wroteDebuffStart, ref wroteCloakBuffStart );
                            if ( entity.ActiveHack != null )
                            {
                                buffer.Add( ", 因入侵被禁用" );
                            } else if ( entity.GetIsCrippled() )
                            {
                                buffer.Add( ", 已残废" );
                            } else if ( detailLevel == TooltipDetail.Full )
                            {
                                buffer.Add( " (Regen: " ).Add( 1 + ExternalConstants.Instance.SecondsToWaitBeforeRecloaking - (World_AIW2.Instance.GameSecond - entity.GameSecondOfLastCloakingPointLoss) )
                                    .Add( "s)" );
                            }
                        } 
                        else if ( World_AIW2.Instance.GameSecond - entity.GameSecondOfLastCloakingPointLoss < 2 )
                        {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDecloakDebuffsStartIfNeeded( buffer, useIcons, false, ref wroteDebuffStart, ref wroteCloakBuffStart );
                        }
                    }
                    #endregion

                    if (relatedEntityTypeData.IsTurret && AIWar2GalaxySettingQuickAccess.UseHarshTurretRangesOnNonHumanPlanets ) {
                        if ( owningFactionOrNull?.Type == FactionType.Player && thisPlanetOrNull?.GetControllingFactionType() != FactionType.Player  ) {
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteDebuffsStartIfNeeded( buffer, ref wroteDebuffStart );
                                buffer.WrapSpeed("射程: ", false, false).Add("在敌方星球上受限");
                        }
                    }

                    if ( wroteDebuffStart )
                    {
                        buffer.EndSize();
                    }
                }
                #endregion

                debugStage = 17001;

                #region SeventhRow: Build Stats
                if ( panelMode == Window_InGameHoverEntityInfo.Mode.Build || entityCanBeClaimed )
                {
                    int fuelUse = entityType.FuelUse;
                    int fuelMax = 0;
                    if ( World_AIW2.Instance.IsFuelEnabled )
                    {
                        if ( entityType.FuelUseType == ResourceType.FuelArgon )
                            fuelMax = storedArgon;
                        else if ( entityType.FuelUseType == ResourceType.FuelRadon )
                            fuelMax = storedRadon;
                        else if ( entityType.FuelUseType == ResourceType.FuelXenon )
                            fuelMax = storedXenon;
                    }
                    WriteBuildStatRow( buffer, showMetalCost, showEnergyConsumption, cpi, 1, strengthCurr, hullMax, shieldMax, metalCurr, storedMetal, storedEnergy, energyCurr,
                        entityType.FuelUseType, fuelUse, fuelMax, useIcons, useText );

                    int forCount = 0;
                    if ( OptionalCountToShow > 1 )
                    {
                        forCount = OptionalCountToShow;
                    } else if ( entity.ExtraStackedSquadsInThis > 0 )
                    {
                        forCount = entity.ExtraStackedSquadsInThis + 1;
                    } else if ( fleetMembershipOrNull?.EffectiveSquadCap > 1 )
                    {
                        forCount = fleetMembershipOrNull.EffectiveSquadCap;
                    }

                    if ( forCount > 1 )
                    {
                        WriteBuildStatRow( buffer, showMetalCost, showEnergyConsumption, cpi, forCount, strengthCurr, hullMax, shieldMax, metalCurr, storedMetal, storedEnergy, energyCurr,
                            entityType.FuelUseType, fuelUse, fuelMax, useIcons, useText );
                    }
                }
                #endregion

                debugStage = 18001;

                #region EightRow: Ship Class
                if ( !relatedEntityTypeData.ShipClass.IsDefault && relatedEntityTypeData.ShipClass.WriteDescriptionSoonerInTooltip )
                {
                    FactionType fType = FactionType.Player;
                    if ( entityFaction != null )
                        fType = entityFaction.Type;
                    Window_PrototypeInGameHoverEntityInfoUtils.WriteShipClass_Complete( buffer, relatedEntityTypeData.ShipClass, detailLevel, fType, markStats.Speed );
                }
                #endregion

                debugStage = 19001;

                //========End SirLimbo Bits

                //note: The code expects you to be missing a newline here, and it will add one after it.
                //      If you end on a newline, you'll get an extra space.

                //note: This DebugText is meant for programmers to be able to add random text in as they need to.
                //      It isn't meant to ever be shown to end users, but it still needs to be here.
                if ( relatedSquadOrNull != null && relatedSquadOrNull.DebugText.Length > 0 )
                    buffer.Add( "\n" ).Add( relatedSquadOrNull.DebugText );

                Color healthColor = ColorMath.LightGreen;
                Color shieldsColor = ColorMath.LightCyan;
                //Color squadShipCountColor = ColorMath.LightBlue;
                Color cloakColor = ColorMath.PaleVioletRed;
                Color armorColor = ColorMath.LighterRed;
                Color massColor = ColorMath.LightOrange;
                Color albedoColor = ColorMath.LightPurple;
                Color speedColor = ColorMath.LightSkyBlue;
                Color engineColor = ColorMath.LightWhipBlue;

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
                    AddSingleValueStrengthOnly( buffer, relatedMembershipOrNull != null && relatedMemFleetOrNull.IsPlayerStyleFleet ? relatedMembershipOrNull.GetStrengthPerSquad_PlayerFleetsOnly() : markStats.StrengthPerSquad_CalculatedWithNullFleetMembership );
                    buffer.EndColor();

                    if ( relatedMembershipOrNull != null && localPlayerFaction.NetEnergy < relatedMembershipOrNull.GetEnergyUsage() && relatedMembershipOrNull.GetEnergyUsage() > 0 )
                        buffer.Add( "\n" ).StartColor( QuickColors.Danger ).Add( "该舰船需要 " ).Add( relatedMembershipOrNull.GetEnergyUsage() )
                            .Add( " 能量才能运作，但你只有 " ).Add( localPlayerFaction.NetEnergy ).Add( " available." ).EndColor();
                }*/

                debugStage = 60;

                debugStage = 80;

                debugStage = 97;

                int weaponActivityDetails = GameSettings.Current.GetIntBySetting( "Tooltip_WeaponActivityDetails" );
                bool showWeaponAbortCode = GameSettings.Current.GetBoolBySetting( "Debug_WeaponAbortCode" );
                bool showWeaponTargetInfo = GameSettings.Current.GetBoolBySetting( "Debug_WeaponTargetInfo" );
                int weaponTargetInfoListCount = GameSettings.Current.GetIntBySetting( "Debug_WeaponTargetInfoCount" );

                debugStage = 140;
                int cloakSystem = -1;
                int tachyonSystem = -1;
                int tractorSystem = -1;
                int gravitySystem = -1;
                int attractantSystem = -1;
                for ( int i = 0; i < relatedEntityTypeData.SystemTypes.Count; i++ )
                {
                    EntitySystemTypeData systemData = relatedEntityTypeData.SystemTypes[i];
                    if ( systemData.MaxMarkLevelToFunction < effectiveMarkLevel || systemData.MinMarkLevelToFunction > effectiveMarkLevel || systemData.SystemIsHiddenForUI )
                        continue;
                    if ( systemData.CareAboutStateOfMatterToBeEnabled && systemData.IsInvisibleInTooltipsIfNotAMatchByStateOfMatter )
                    {
                        if ( relatedSquadOrNull != null && relatedSquadOrNull.CurrentStateOfMatter != systemData.MustBeThisStateOfMatterToBeEnabled )
                            continue; //skip completely, since this will be invisible AND disabled
                    }
                    if ( systemData.IsModule && !systemData.IsModuleOn( relatedMembershipOrNull ) )
                        continue;
                    EntitySystemTypeData.MarkLevelStats systemForMarkLevel = systemData.ForMark[effectiveMarkLevel];
                    if ( cloakSystem < 0 && systemData.ForMark[effectiveMarkLevel].CloakingPoints > 0 )
                        cloakSystem = i;
                    if ( tachyonSystem < 0 && (systemData.TachyonHitsAlbedoLessThan > FInt.Zero || systemData.TachyonHitsAlbedoMoreThan > FInt.Zero) )
                        tachyonSystem = i;
                    if ( tractorSystem < 0 && systemData.ForMark[effectiveMarkLevel].TractorCount > 0 )
                        tractorSystem = i;
                    if ( gravitySystem < 0 && systemData.GravityHitsEngine_gxLessThan > 0 )
                        gravitySystem = i;
                    if ( attractantSystem < 0 && systemData.ForMark[effectiveMarkLevel].AttractRangeForShotsAgainstAllies > 0 )
                        attractantSystem = i;
                }

                bool hasSystemWithBonusFromAttackingUnderForcefields = false;

                debugStage = 180;
                for ( int i = 0; i < relatedEntityTypeData.SystemTypes.Count; i++ )
                {
                    debugStage = 182;
                    EntitySystemTypeData systemData = relatedEntityTypeData.SystemTypes[i];
                    if ( systemData.MaxMarkLevelToFunction < effectiveMarkLevel || systemData.MinMarkLevelToFunction > effectiveMarkLevel || systemData.SystemIsHiddenForUI )
                        continue;
                    if ( systemData.CareAboutStateOfMatterToBeEnabled && systemData.IsInvisibleInTooltipsIfNotAMatchByStateOfMatter )
                    {
                        if ( relatedSquadOrNull != null && relatedSquadOrNull.CurrentStateOfMatter != systemData.MustBeThisStateOfMatterToBeEnabled )
                            continue; //skip completely, since this will be invisible AND disabled
                    }
                    if (!isUsingFakeEntityAsStandin)
                    {
                        if ( systemData.IsModule && !systemData.IsModuleOn( relatedMembershipOrNull ) )
                            continue;
                    }
                    
                    WriteSystemInfo( buffer, i, relatedSquadOrNull, relatedMembershipOrNull, entityFaction, thisPlanetOrNull, systemData, effectiveMarkLevel, true, false, detailLevel, isForMultipleUnits,
                        weaponActivityDetails, showWeaponAbortCode, showWeaponTargetInfo, weaponTargetInfoListCount, ref hasSystemWithBonusFromAttackingUnderForcefields, useIcons, useText );
                }

                debugStage = 300;
                buffer.Add( FontSizes.SLIGHTLY_SMALLER_SIZE_STRING ).Add( "\n" );
                if ( relatedSquadOrNull != null && relatedSquadOrNull.SecondsSpentAsRemains >= 0 )
                {
                    if ( detailLevel < TooltipDetail.Full )
                        buffer.Add( "<color=#ff4b21>这仅是单位的破损残骸。</color>" );
                    else
                        buffer.Add( "<color=#ff4b21>这仅仅是该单位的残骸。残骸本身不会产生任何作用，但可以由重建者单位进行修复重建。</color>" );
                }
                debugStage = 305;
                if ( detailLevel < TooltipDetail.Full && relatedEntityTypeData.DescriptionShort.Length > 0 &&
                    (relatedEntityTypeData.DescriptionShort.Length != 3 || relatedEntityTypeData.DescriptionShort != "~*~") )
                {
                    buffer.StartColor( "aaaaaa" ).Add( relatedEntityTypeData.DescriptionShort ).Add( "</color>\n" );
                } else
                {
                    if ( relatedEntityTypeData.Description.Length > 0 && (relatedEntityTypeData.Description.Length != 3 || relatedEntityTypeData.Description != "~*~") )
                        buffer.StartColor( "aaaaaa" ).Add( relatedEntityTypeData.Description ).Add( "</color>\n" );
                }
                debugStage = 30510;

                relatedEntityTypeData.ForAnyDataExtensions_AddToTooltip_MidSection_ForEntity( buffer, relatedSquadOrNull, relatedMembershipOrNull, relatedEntityTypeData, detailLevel );

                if ( (GameSettings.Current.GetBoolBySetting( "ShowEntityIDInHovertext" ) || showDebugInfoInTooltip)
                    && relatedSquadOrNull != null )
                {
                        buffer.Add( " primaryKeyID " ).Add( relatedSquadOrNull.PrimaryKeyID, "22ff22" ).Add( ". " );
                    if ( relatedSquadOrNull.FireteamId > 0 )
                        buffer.Add( "fireteamId " ).Add( relatedSquadOrNull.FireteamId, "22ff22" ).Add( ". " );
                    if ( relatedSquadOrNull.FireteamSpecificationOrNull != null && relatedSquadOrNull.FireteamSpecificationOrNull.IsActive() )
                    {
                        buffer.Add("<color=#22ff22>");
                        relatedSquadOrNull.FireteamSpecificationOrNull.ToDisplayString(buffer);
                        buffer.Add( "</color>. ");
                    }
                }
                debugStage = 30530;
                if ( relatedSquadOrNull != null && relatedSquadOrNull.FireteamId > 0 )
                {
                    debugStage = 30531;
                    if ( (relatedSquadOrNull.GetIsFriendlyToLocalFaction_Safe() && detailLevel >= TooltipDetail.Full)
                       || GameSettings.Current.GetBoolBySetting( "ShowFireteamHistory" ) )
                    {
                        debugStage = 30532;
                        Fireteam team = null;
                        ExternalFactionBaseInfo baseInfo = relatedSquadOrNull.GetFactionBaseInfoOrNull_Safe();
                        if ( baseInfo != null )
                            team = (Fireteam) baseInfo.GetFireteamBaseById( relatedSquadOrNull.FireteamId );

                        if ( team != null )
                        {
                            //team can be null if we are racing with the long range planning code that clears/rebuilds the team list
                            debugStage = 30533;
                            buffer.Add( "\n火力小组 " ).Add( team.FireTeamID, "10ffdd" ).Add( " 状态: " );
                            team.GetStatusForDisplay( buffer );
                            buffer.Add( ". " );
                            if ( team.Target != null && showDebugInfoInTooltip )
                                buffer.Add( "目标是 " + team.Target.ToStringWithPlanetAndOwner() ).Add( "\n" );
                            team.GetSpecificationForDisplay( buffer );
                            if ( team != null && team.History != null && team.History.Count > 0 && team.FireTeamID > 0 )
                            {
                                buffer.Add( "单位火力小组历史:\n " );
                                for ( int i = team.History.Count - 1; i >= 0; i-- )
                                {
                                    buffer.Add( "\t" + team.History[i].ToDisplayString( team ) + "\n" );
                                }
                            }
                        } else
                            buffer.Add( "<color=#d57aff>Null fireteam despite fireteam ID " + relatedSquadOrNull.FireteamId + "?</color>  " );
                    }
                }
                debugStage = 30539;

                if ( showDebugInfoInTooltip && relatedSquadOrNull != null )
                {
                    debugStage = 305391;
                    buffer.Add( "舰队类别: " ).Add( relatedMemFleetOrNull == null ? "null" : EnumNameCache.GetName( relatedMemFleetOrNull.Category ) ).Add( ".  " );
                    debugStage = 305392;
                    bool isThisSelectedVisually = relatedSquadOrNull.GetIsSelected();
                    debugStage = 305393;
                    bool isSelectedInListOfSquads = World_AIW2.Instance.LocalPlayerSelectedSquads_CanGiveOrders.Contains( relatedSquadOrNull ) ||
                        World_AIW2.Instance.LocalPlayerSelectedSquads_CannotGiveOrders.Contains( relatedSquadOrNull ); ;
                    debugStage = 305394;
                    if ( relatedMemFleetOrNull != null )
                    {
                        bool isSelectedByFleet = relatedMemFleetOrNull.IsConsideredSelected_NonSim;
                        debugStage = 305395;
                        buffer.Add( "已选中: " ).Add( !isThisSelectedVisually ? "视觉_否" : "视觉_是" ).Add( ", " )
                            .Add( !isSelectedInListOfSquads ? "列表_否" : "列表_是" ).Add( ", " )
                            .Add( !isSelectedByFleet ? "舰队_否" : "舰队_是" ).Add( ".  " );
                    }
                    debugStage = 305396;
                    ArcenRejectionReason disabledReason = relatedSquadOrNull.ComputeDisabledReason();
                    if ( disabledReason != ArcenRejectionReason.Unknown ) //unknown is "not disabled", so don't bother showing that!
                        buffer.Add( "ComputeDisabledReason: " ).Add( EnumNameCache.GetName( disabledReason ) ).Add( ".  " );
                }
                debugStage = 30540;

                if ( detailLevel >= TooltipDetail.Full )
                {
                    if ( relatedEntityTypeData.IsCrippledInsteadOfDying && (relatedSquadOrNull == null || relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player) )
                    {
                        FInt extraCost = relatedEntityTypeData.GetExtraCostWhileCrippled();
                        buffer.StartColor( "999999" ).Add( "如果此单位残废，维修费用将增加 " ).AddFixedDecimal( extraCost.ToFloatNonSim(), 2 ).Add( "x，直到它不再残废（恢复满血）。  " );

                        int hackingPointsLost = relatedEntityTypeData.GetHackingPointsLostWhenCrippled();
                        if ( hackingPointsLost > 0 )
                        {
                            buffer.Add( "每次残废时你还将损失 " ).WrapHacking( hackingPointsLost, useIcons, useText ).Add( "。  " );
                        }
                        buffer.EndColor();
                    }
                    
                    if (relatedEntityTypeData.ShipClass.CanBeDamaged && relatedSquadFactionOrNull != null && relatedSquadFactionOrNull.Type == FactionType.Player)
                    {
                        int time = relatedEntityTypeData.CustomRepairImpossibleForSecondsAfterDamagedByEnemy_Max15 > 0 ? relatedEntityTypeData.CustomRepairImpossibleForSecondsAfterDamagedByEnemy_Max15 : ExternalConstants.Instance.Balance_RepairImpossibleForSecondsAfterDamagedByEnemy;
                        buffer.StartColor( "999999" ).Add( "此单位在被敌人伤害后 " ).Add( time, "a45e5e" ).Add( "s 内无法恢复或维修。  " ).EndColor();
                    }
                }

                if ( relatedSquadOrNull != null )
                {
                    if ( relatedSquadOrNull.TypeData.SelfAttritionsXPercentPerSecondIfParentShipNotOnPlanet > 0 )
                    {
                        if ( detailLevel >= TooltipDetail.Full )
                            buffer.StartColor( "aaaaaa" ).Add( "此单位因自我损耗而永远无法维修。  " ).EndColor();
                        else
                            buffer.StartColor( "aaaaaa" ).Add( "此单位永远无法维修。  " );
                    } else if ( relatedSquadOrNull.RepairImpossibleForSeconds > 0 )
                    {
                        if ( relatedSquadOrNull.TypeData.ImmuneToRepairs )
                        {
                            if ( relatedSquadOrNull.TypeData.SecondsToFullyRegenerateHull > FInt.Zero )
                                buffer.StartColor( "aaaaaa" ).Add( "此单位将在 " ).Add( relatedSquadOrNull.RepairImpossibleForSeconds, "aa3434" ).Add( "秒后可恢复生命值。  " ).EndColor();
                        } else
                        {
                            if ( relatedSquadOrNull.TypeData.SecondsToFullyRegenerateHull > FInt.Zero )
                                buffer.StartColor( "aaaaaa" ).Add( "此单位将在 " ).Add( relatedSquadOrNull.RepairImpossibleForSeconds, "aa3434" ).Add( "秒后可恢复生命值或被维修。  " ).EndColor();
                            else
                                buffer.StartColor( "aaaaaa" ).Add( "此单位将在 " ).Add( relatedSquadOrNull.RepairImpossibleForSeconds, "aa3434" ).Add( "秒后可被维修。  " ).EndColor();
                        }
                    }
                    //if ( relatedSquadOrNull.RepairExtraCostForSeconds > 0 )
                    //{
                    //    if ( !relatedSquadOrNull.TypeData.ImmuneToRepairs )
                    //    {
                    //        buffer.StartColor( "aaaaaa" ).Add( "修复此单位将花费 " ).Add(
                    //        ExternalConstants.Instance.Balance_MultiplierForShipsThatHaveExtraRepairCostTemporarily.ToFloatNonSim().ToString( "0.##" )
                    //        ).Add( "倍，持续 " ).Add( relatedSquadOrNull.RepairExtraCostForSeconds, "aa3434" ).Add( "秒。  " ).EndColor();
                    //    }
                    //}
                    //if ( relatedSquadOrNull.GetIsCrippled() )
                    //{
                    //    if ( !relatedSquadOrNull.TypeData.ImmuneToRepairs )
                    //    {
                    //        FInt extraCost = relatedEntityTypeData.GetExtraCostWhileCrippled();
                    //        buffer.StartColor( "aaaaaa" ).Add( "修复此单位需要 " ).Add( extraCost.ToFloatNonSim().ToString( "0.##" )
                    //        ).Add( "倍，持续 it is no longer crippled (when it reaches full hull health).  " ).EndColor();
                    //    }
                    //}
                } else
                {
                    if ( relatedEntityTypeData.SelfAttritionsXPercentPerSecondIfParentShipNotOnPlanet > 0 )
                    {
                        if ( detailLevel >= TooltipDetail.Full )
                            buffer.StartColor( "aaaaaa" ).Add( "此单位永远不会被维修，因为它会自行消耗。  " ).EndColor();
                        else
                            buffer.StartColor( "aaaaaa" ).Add( "此单位永远不会被维修。  " ).EndColor();
                    }
                }
                float secondsPerSimFrame = World_AIW2.Instance.SimulationProfile.SecondsPerFrameNonSim;
                float simFrameMultiplier = 1f / secondsPerSimFrame;

                //Main description items
                //--------------------------------------
                debugStage = 310;
                Window_InGameHoverEntityInfo.WriteTransformsInto(buffer, relatedSquadOrNull);
                
                #region Assistance Items
                Window_PrototypeInGameHoverEntityInfoUtils.WriteSupportMetalFlows( buffer, relatedSquadOrNull, markStats, detailLevel, ref debugStage );
                #endregion

                debugStage = 330;

                #region Drones
                if ( relatedEntityTypeData.FleetDesignTemplateIUseForDrones != null &&
                    (relatedEntityTypeData.FleetDesignTemplateIUseForDrones.Strikecraft.DrawBag.GetHasItems() ||
                    relatedEntityTypeData.FleetDesignTemplateIUseForDrones.Frigates.DrawBag.GetHasItems()) )
                {
                    debugStage = 340;
                    if ( detailLevel < TooltipDetail.Full )
                        buffer.Add( "自动建造以下类型的无人机: <color=#ffdf72>" );
                    else
                        buffer.Add( "自动建造以下类型的无人机，并在受到威胁时释放: <color=#ffdf72>" );
                    bool isFirst = true;

                    debugStage = 350;
                    DrawBag<FleetItem> drones = relatedEntityTypeData.FleetDesignTemplateIUseForDrones.Strikecraft.DrawBag;
                    if ( drones != null )
                    {
                        for ( int j = 0; j < drones.InternalListSize; j++ )
                        {
                            FleetItem droneItem = drones.GetInternalListItemAtIndex( j );
                            if ( droneItem == null )
                                continue;
                            if ( isFirst )
                                isFirst = false;
                            else
                                buffer.Add( ", " );
                            buffer.Add( droneItem.TypeData.DisplayName );
                            buffer.Add( " x" );
                            int cap = droneItem.Cap;
                            if ( relatedEntityTypeData.MultipliedNonFrigateShipCapForDrones > FInt.Zero )
                                cap = (cap * relatedEntityTypeData.MultipliedNonFrigateShipCapForDrones).GetNearestIntPreferringHigher();
                            buffer.Add( cap );
                        }
                    }
                    drones = relatedEntityTypeData.FleetDesignTemplateIUseForDrones.Frigates.DrawBag;
                    if ( drones != null )
                    {
                        for ( int j = 0; j < drones.InternalListSize; j++ )
                        {
                            FleetItem droneItem = drones.GetInternalListItemAtIndex( j );
                            if ( droneItem == null )
                                continue;
                            if ( isFirst )
                                isFirst = false;
                            else
                                buffer.Add( ", " );
                            buffer.Add( droneItem.TypeData.DisplayName );
                            buffer.Add( " x" );
                            int cap = droneItem.Cap;
                            if ( relatedEntityTypeData.MultipliedFrigateShipCapForDrones > FInt.Zero )
                                cap = (cap * relatedEntityTypeData.MultipliedFrigateShipCapForDrones).GetNearestIntPreferringHigher();
                            buffer.Add( cap );
                        }
                    }
                    buffer.Add( "</color>.  " );
                }
                #endregion

                debugStage = 360;

                #region Resource Generation
                {
                    bool handledMetalStorage = false;
                    FInt resourceProductionPreBonuses;
                    for ( ResourceType resource = ResourceType.None + 1; resource < ResourceType.Length; resource++ )
                    {
                        resourceProductionPreBonuses = markStats.GetResourceProductionBeforeAnyBonuses( resource );
                        if ( resourceProductionPreBonuses == FInt.Zero )
                            continue;

                        switch ( resource )
                        {
                            case ResourceType.Metal:
                                if ( !showMetalOther )
                                    continue;
                                break;
                            case ResourceType.Energy:
                                if ( !showEnergyProduction )
                                    continue;
                                break;
                            case ResourceType.FuelArgon:
                            case ResourceType.FuelRadon:
                            case ResourceType.FuelXenon:
                                if ( !World_AIW2.Instance.IsFuelEnabled || !showAnyFuel )
                                    continue;
                                break;
                        }

                        if ( detailLevel < TooltipDetail.Full && (resource == ResourceType.Science || resource == ResourceType.Hacking) )
                            continue;  //science and hacking only shown in verbose mode

                        FInt productionFinal = resourceProductionPreBonuses;
                        switch ( resource )
                        {
                            case ResourceType.Metal:
                                if ( relatedSquadOrNull != null )
                                    productionFinal = relatedSquadOrNull.GetFullyMultipliedMetalToProduce();
                                else if ( thisPlanetOrNull != null ) //make it correct in build mode!
                                    productionFinal = GameEntity_Squad.DoMultiplierOfMetalAtPlanet( relatedEntityTypeData, productionFinal, thisPlanetOrNull );
                                break;
                            case ResourceType.Energy:
                                if ( relatedSquadOrNull != null )
                                    productionFinal = relatedSquadOrNull.GetFullyMultipliedEnergyToProduce();
                                else if ( thisPlanetOrNull != null ) //make it correct in build mode!
                                    productionFinal = GameEntity_Squad.DoMultiplierOfEnergyAtPlanet( relatedEntityTypeData, productionFinal, thisPlanetOrNull );
                                break;
                        }

                        switch ( resource )
                        {
                            case ResourceType.Metal:
                            case ResourceType.FuelArgon:
                            case ResourceType.FuelRadon:
                            case ResourceType.FuelXenon:
                                buffer.Add( "生产 " );
                                break;
                            case ResourceType.Energy:
                                buffer.Add( "生成 " );
                                break;
                            case ResourceType.Hacking:
                            case ResourceType.Science:
                                buffer.Add( "采集 " );
                                break;
                            default:
                                continue;
                        }

                        if ( detailLevel == TooltipDetail.Full )
                            buffer.WrapGenericResourceMoreReadable( productionFinal, resource, useIcons, useText );
                        else
                            buffer.WrapGenericResourceTruncated( productionFinal, resource, useIcons, useText );

                        switch ( resource )
                        {
                            case ResourceType.Metal:
                                buffer.Add( " /秒" );
                                if ( markStats.MetalStorage > 0 )
                                {
                                    handledMetalStorage = true;
                                    buffer.Add( " 并存储 " ).WrapMetalTruncated( markStats.MetalStorage, useIcons, useText );
                                }
                                buffer.Add( "。  " );
                                break;
                            case ResourceType.Energy:
                                buffer.Add( "。  " );
                                break;
                            case ResourceType.Hacking:
                                buffer.Add( " /秒（当停留在有剩余黑客点的行星上时）。  " );
                                break;
                            case ResourceType.Science:
                                buffer.Add( " /秒（当停留在有剩余科学的行星上时）。  " );
                                break;
                            case ResourceType.FuelArgon:
                                buffer.Add( "（用于主力作战舰船）。  " );
                                break;
                            case ResourceType.FuelRadon:
                                buffer.Add( "（用于炮塔和力场）。  " );
                                break;
                            case ResourceType.FuelXenon:
                                buffer.Add( "（用于军官和精英单位）。  " );
                                break;
                            default:
                                continue;
                        }
                    }
                    if ( !handledMetalStorage )
                    {
                        if ( markStats.MetalStorage > 0 )
                        {
                            handledMetalStorage = true;
                            buffer.Add( "存储 " ).WrapMetalTruncated( markStats.MetalStorage, useIcons, useText ).Add( "。  " );
                        }
                    }
                }
                #endregion

                debugStage = 370;

                if ( World_AIW2.Instance.PlayersAreInDistributedResourceGenerationMode )
                {
                    bool showMetal = showMetalOther && relatedEntityTypeData.MetalProductionMultiplierToAsteroids_DistributedModeOnly > FInt.Zero &&
                                relatedEntityTypeData.MetalProductionMultiplierToAsteroids_DistributedModeOnly != FInt.One;
                    bool showEnergy = showEnergyProduction && relatedEntityTypeData.EnergyProductionMultiplierToAsteroids_DistributedModeOnly > FInt.Zero &&
                                relatedEntityTypeData.EnergyProductionMultiplierToAsteroids_DistributedModeOnly != FInt.One;
                    if ( showMetal )
                    {
                        buffer.Add( "增加小行星发电站产量：" ).StartMetalWrapper( useIcons ).AddNumberMoreReadable( relatedEntityTypeData.MetalProductionMultiplierToAsteroids_DistributedModeOnly )
                            .Add( "倍" ).EndMetalWrapper( useText ).Add("/s");
                    }

                    if ( showEnergy )
                    {
                        if ( showMetal )
                            buffer.Add( ", " );
                        else
                            buffer.Add( "增加小行星发电站产量：" );
                        buffer.StartEnergyWrapper( useIcons ).AddNumberMoreReadable( relatedEntityTypeData.EnergyProductionMultiplierToAsteroids_DistributedModeOnly )
                            .Add( "倍" ).EndEnergyWrapper( useText ).Add( "/秒" );
                    }

                    if ( showMetal || showEnergy )
                        buffer.Add( ".  " );
                }

                #region Resource Multipliers
                {
                    FInt resourceMultiplier;
                    for ( ResourceType resource = ResourceType.None + 1; resource < ResourceType.Length; resource++ )
                    {
                        resourceMultiplier = markStats.GetResourceProductionMultiplier( resource );
                        if ( resourceMultiplier == FInt.Zero )
                            continue;
                        switch ( resource )
                        {
                            case ResourceType.Metal:
                                if ( !showMetalOther )
                                    continue;
                                break;
                            case ResourceType.Energy:
                                if ( !showEnergyProduction )
                                    continue;
                                break;
                            case ResourceType.FuelArgon:
                            case ResourceType.FuelRadon:
                            case ResourceType.FuelXenon:
                                if ( !World_AIW2.Instance.IsFuelEnabled || !showAnyFuel )
                                    continue;
                                break;
                        }

                        debugStage = 372;

                        buffer.Add( "提供 <color=#ffdf72>" ).AddNumberTruncated( resourceMultiplier ).Add( "x</color> 加成于所有 " ).StartGenericResourceWrapper( resource, false );

                        switch ( resource )
                        {
                            case ResourceType.Metal:
                                buffer.Add( "金属/秒产量" );
                                break;
                            case ResourceType.Energy:
                                buffer.Add( "能源生成量" );
                                break;
                            case ResourceType.Hacking:
                                buffer.Add( "黑客采集量" );
                                break;
                            case ResourceType.Science:
                                buffer.Add( "科学采集量" );
                                break;
                            case ResourceType.FuelArgon:
                                buffer.Add( "氩气产量" );
                                break;
                            case ResourceType.FuelRadon:
                                buffer.Add( "氡气产量" );
                                break;
                            case ResourceType.FuelXenon:
                                buffer.Add( "氙气产量" );
                                break;
                            default:
                                continue;
                        }
                        buffer.EndGenericResourceWrapper( resource, false ).Add( "（在行星上）。  " );
                    }
                }
                #endregion

                debugStage = 375;

                #region Resource Multipliers After Time Being Here And Not Crippled
                if ( relatedEntityTypeData.TimeRequiredToBeHereAndNonCrippledToBoostMetalOrEnergyProduction > 0 &&
                    (relatedEntityTypeData.BoostToPlanetaryMetalProductionIfHaveBeenHereAndNonCrippledForXTime > FInt.One || relatedEntityTypeData.BoostToPlanetaryEnergyProductionIfHaveBeenHereAndNonCrippledForXTime > FInt.One) )
                {
                    bool areBonusesOn = false;
                    bool canBoostMetal = relatedEntityTypeData.BoostToPlanetaryMetalProductionIfHaveBeenHereAndNonCrippledForXTime > FInt.One;
                    bool canBoostEnergy = relatedEntityTypeData.BoostToPlanetaryEnergyProductionIfHaveBeenHereAndNonCrippledForXTime > FInt.One;
                    int timeHasBeenHereAndNotCrippled = 0;
                    bool displayExtraData = relatedSquadOrNull != null && ForFactionOrNull != null && (ForFactionOrNull.Type == FactionType.Player || ForFactionOrNull.Type == FactionType.NaturalObject);
                    bool metalBoostFailed = relatedSquadOrNull != null && relatedSquadOrNull.NonSim_PlanetaryMetalBoostFailedFromOthersBeingPresent;
                    bool energyBoostFailed = relatedSquadOrNull != null && relatedSquadOrNull.NonSim_PlanetaryEnergyBoostFailedFromOthersBeingPresent;
                    bool hasEqualBoosts = relatedEntityTypeData.BoostToPlanetaryMetalProductionIfHaveBeenHereAndNonCrippledForXTime == relatedEntityTypeData.BoostToPlanetaryEnergyProductionIfHaveBeenHereAndNonCrippledForXTime;
                    if ( displayExtraData && ForFactionOrNull.Type == FactionType.Player )
                    {
                        timeHasBeenHereAndNotCrippled = relatedSquadOrNull.GetSecondsSinceEnteringThisPlanetOrLastCrippled();
                        areBonusesOn = timeHasBeenHereAndNotCrippled >= relatedEntityTypeData.TimeRequiredToBeHereAndNonCrippledToBoostMetalOrEnergyProduction &&
                            (!relatedSquadOrNull.NonSim_PlanetaryMetalBoostFailedFromOthersBeingPresent || relatedSquadOrNull.NonSim_PlanetaryEnergyBoostFailedFromOthersBeingPresent);
                        if ( areBonusesOn )
                            buffer.Add( "<color=#ffed52>ON: </color>" );
                        else
                            buffer.Add( "<color=#ff8f52>OFF: </color>" );
                    }

                    //Combined numbers:
                    if ( hasEqualBoosts && metalBoostFailed == energyBoostFailed )
                    {
                        if ( metalBoostFailed )
                            buffer.Add( "可能提供 <color=#ffdf72>" );
                        else
                            buffer.Add( "提供 <color=#ffdf72>" );
                        buffer.AddNumberTruncated( relatedEntityTypeData.BoostToPlanetaryMetalProductionIfHaveBeenHereAndNonCrippledForXTime ).Add( "x</color> 加成于金属/秒产量和能源生成" );
                    } else
                    {//Separated numbers
                        if ( canBoostMetal )
                        {
                            if ( metalBoostFailed )
                                buffer.Add( "可能提供 " );
                            else
                                buffer.Add( "提供 " );
                            buffer.AddNumberTruncated( relatedEntityTypeData.BoostToPlanetaryMetalProductionIfHaveBeenHereAndNonCrippledForXTime ).Add( "x</color> 加成于金属/秒产量" );
                            if ( metalBoostFailed && !energyBoostFailed )
                                buffer.Add( "（但被更强大的增幅器覆盖）" );
                        }
                        if ( canBoostEnergy )
                        {
                            if ( canBoostMetal )
                            {
                                buffer.Add( " 和 " );
                                if ( energyBoostFailed )
                                    buffer.Add( "可能提供 " );
                                else
                                    buffer.Add( "提供 " );
                            } else
                            {
                                if ( energyBoostFailed )
                                    buffer.Add( "可能提供 " );
                                else
                                    buffer.Add( "提供 " );
                            }
                            buffer.AddNumberTruncated( relatedEntityTypeData.BoostToPlanetaryEnergyProductionIfHaveBeenHereAndNonCrippledForXTime ).Add( "x</color> 加成于能源生成" );
                            if ( energyBoostFailed )
                            {
                                if ( metalBoostFailed )
                                    buffer.Add( "（但两者都被更强大的增幅器覆盖）" );
                                else
                                    buffer.Add( "（但被更强大的增幅器覆盖）" );
                            }
                        }
                    }
                    buffer.Add( " 到其所在行星" );
                    if ( displayExtraData )
                    {
                        if ( areBonusesOn )
                            buffer.Add( "，自从该单位在此行星上且未被击伤超过 " )
                                .AddHoursAndMinutes( relatedEntityTypeData.TimeRequiredToBeHereAndNonCrippledToBoostMetalOrEnergyProduction ).Add( " 后已激活。  " );
                        else
                            buffer.Add( "，一旦该单位在此行星上且未被击伤至少 " )
                                .AddHoursAndMinutes( relatedEntityTypeData.TimeRequiredToBeHereAndNonCrippledToBoostMetalOrEnergyProduction - timeHasBeenHereAndNotCrippled ).Add( " 后将激活。  " );
                    } else
                    {
                        buffer.Add( "，当该单位在行星上且未被击伤超过 " )
                            .AddHoursAndMinutes( relatedEntityTypeData.TimeRequiredToBeHereAndNonCrippledToBoostMetalOrEnergyProduction ).Add( " 后。  " );
                    }
                }
                #endregion

                debugStage = 380;
                if ( relatedEntityTypeData.HackableForCommandStationsAndBattleStations_DSSStyle && relatedSquadOrNull != null )
                {
                    debugStage = 3800001;
                    HackingType grantHack = null;
                    foreach ( HackingType hack in relatedEntityTypeData.GetListOfHacks() )
                    {
                        if ( hack == null )
                            continue;
                        debugStage = 3800101;
                        if ( hack.IsAGrantShipStyleHack )
                        {
                            grantHack = hack;
                            break;
                        }
                    }
                    debugStage = 3800201;
                    if ( grantHack != null && grantHack.NumberOfTimesIndividualUnitCanBeHacked > 1 )
                    {
                        debugStage = 3800301;
                        int timesHacked = relatedSquadOrNull == null ? 0 : relatedSquadOrNull.GetNumberOfTimesHacked( grantHack );
                        int remaining = grantHack.NumberOfTimesIndividualUnitCanBeHacked - timesHacked;

                        debugStage = 3800401;
buffer.Add( "<color=#f25e1c>可入侵（" ).Add( remaining ).Add( " / " ).Add( grantHack.NumberOfTimesIndividualUnitCanBeHacked ).Add( " 剩余）</color>: " );
                    } else
                        buffer.Add( "<color=#f25e1c>可入侵（单次）</color>: " );
                    debugStage = 3800501;
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    debugStage = 3800501;
                    if ( relatedSquadOrNull.ShipGrantsList.Count <= 0 )
                        buffer.Add( "该单位因某种原因不提供舰船！（这是一个bug，请用存档报告。）  " );
                    else
                    {
                        debugStage = 3800601;
                        buffer.Add( "  可授予的舰船线路（选择一个）: " );
                        ShipLineEntry entry = null;
                        for ( int i = 0; i < relatedSquadOrNull.ShipGrantsList.Count; i++ )
                        {
                            debugStage = 3800701;
                            entry = relatedSquadOrNull.ShipGrantsList[i];
                            if ( i > 0 )
                                buffer.Add( ", " );

                            debugStage = 3800801;
                            byte markLevelAddedAt = localFaction.GetGlobalMarkLevelForShipLine( entry.TypeData );
                            GameEntityTypeData.MarkLevelStats markStatsForDisplay = entry.TypeData.MarkStatsFor( markLevelAddedAt );
                            if ( markStatsForDisplay == null )
                                buffer.Add( "<color=#ffa1a1>" );
                            else
                                buffer.StartColor( markStatsForDisplay.MarkLevel.ColorHex );

                            debugStage = 3800901;
                            int numShips = entry.GetNumShipsForHackAndHacker( hackerBeingUsedAgainstUs, hackBeingDoneAgainstUs == null ? grantHack : hackBeingDoneAgainstUs );
                            buffer.Add( entry.TypeData == null ? "null" : entry.TypeData.GetDisplayName() ).Add( "</color>" ).Add( " x" )
                                //Chris says: these are theoretical adds, so this is the base cap and that gets marked up.  This is okay.
                                .Add( entry.TypeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( entry.TypeData, numShips, numShips, markStatsForDisplay.MarkLevel ), entry.GetColorForShipLineScore() );

                            debugStage = 3801101;
                            if ( detailLevel == TooltipDetail.Full &&
                                 entry.TypeData.TechUpgradesThatBenefitMe.Count > 0 )
                            {
                                debugStage = 3801201;
                                buffer.StartColor( Color.grey ).Add( "<size=60%>" );
                                for ( int j = 0; j < entry.TypeData.TechUpgradesThatBenefitMe.Count; j++ )
                                {
                                    debugStage = 3801301;
                                    buffer.Add( " " ).Add( entry.TypeData.TechUpgradesThatBenefitMe[j].DisplayName );
                                }
                                buffer.Add( "</size>" ).EndColor();
                            }
                            debugStage = 3801401;
                            if ( entry.TypeData.AIPWhenGrantedByHack > 0 )
                                buffer.Add( "（消耗 " ).WrapAIPMoreReadable( entry.TypeData.AIPWhenGrantedByHack, useIcons, useText ).Add( "）" );
                        }
                        buffer.Add( ". " );
                    }
                }
                debugStage = 3850;
                if ( relatedEntityTypeData.GrantsStuffToBeAddedToPlayerFleets && relatedSquadOrNull != null )
                {
                    debugStage = 3851;
                    HackingType grantHack = null;
                    foreach ( HackingType hack in relatedEntityTypeData.GetListOfHacks() )
                    {
                        debugStage = 3852;
                        if ( hack.IsAGrantShipStyleHack )
                        {
                            grantHack = hack;
                            break;
                        }
                    }
                    debugStage = 3853;
                    if ( grantHack != null && grantHack.NumberOfTimesIndividualUnitCanBeHacked > 1 )
                    {
                        debugStage = 3854;
                        int timesHacked = relatedSquadOrNull == null ? 0 : relatedSquadOrNull.GetNumberOfTimesHacked( grantHack );
                        int remaining = grantHack.NumberOfTimesIndividualUnitCanBeHacked - timesHacked;

                        buffer.Add( "<color=#f25e1c>可入侵（" ).Add( remaining ).Add( " / " ).Add( grantHack.NumberOfTimesIndividualUnitCanBeHacked ).Add( " 剩余）</color>: " );
                    } else
                        buffer.Add( "<color=#f25e1c>可入侵（单次）</color>: " );
                    debugStage = 3855;
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    if ( relatedSquadOrNull.ShipGrantsList.Count <= 0 )
                        buffer.Add( "该单位因某种原因不提供舰船！（这是一个bug，请用存档报告。）  " );
                    else
                    {
                        debugStage = 3856;
                        buffer.Add( "  可授予的舰船线路（选择一个）: " );
                        ShipLineEntry entry = null;
                        for ( int i = 0; i < relatedSquadOrNull.ShipGrantsList.Count; i++ )
                        {
                            debugStage = 3857;
                            entry = relatedSquadOrNull.ShipGrantsList[i];
                            if ( i > 0 )
                                buffer.Add( ", " );

                            byte markLevelAddedAt = localFaction.GetGlobalMarkLevelForShipLine( entry.TypeData );
                            GameEntityTypeData.MarkLevelStats markStatsForDisplay = entry.TypeData.MarkStatsFor( markLevelAddedAt );

                            // todo: Use ship icons here...
                            //       Even better would be a unified system for outputing a list/fleet/set of ship lines.
                            //buffer.AddShipIconInline(entry.TypeData, localFaction, markStatsForDisplay.MarkLevel);

                            if ( markStatsForDisplay == null )
                                buffer.Add( "<color=#ffa1a1>" );
                            else
                                buffer.StartColor( markStatsForDisplay.MarkLevel.ColorHex );

                            debugStage = 3858;
                            int numShips = entry.GetNumShipsForHackAndHacker( hackerBeingUsedAgainstUs, hackBeingDoneAgainstUs == null ? grantHack : hackBeingDoneAgainstUs );
                            buffer.Add( entry.TypeData == null ? "null" : entry.TypeData.GetDisplayName() ).Add( "</color>" ).Add( " x" )
                                //Chris says: these are theoretical adds, so this is the base cap and that gets marked up.  This is okay.
                                .Add( entry.TypeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( entry.TypeData, numShips, numShips, markStatsForDisplay.MarkLevel ), entry.GetColorForShipLineScore() );
                            debugStage = 3859;
                            if ( detailLevel == TooltipDetail.Full &&
                                 entry.TypeData.TechUpgradesThatBenefitMe.Count > 0 )
                            {
                                buffer.StartColor( Color.grey ).Add( "<size=60%>" );
                                for ( int j = 0; j < entry.TypeData.TechUpgradesThatBenefitMe.Count; j++ )
                                {
                                    buffer.Add( " " ).Add( entry.TypeData.TechUpgradesThatBenefitMe[j].DisplayName );
                                }
                                buffer.Add( "</size>" ).EndColor();
                            }

                            if ( entry.TypeData.AIPWhenGrantedByHack > 0 )
                                buffer.Add("(").AddAIP( entry.TypeData.AIPWhenGrantedByHack.IntValue, true).Add(")");
                        }
                        buffer.Add( ". " );
                    }
                }
                debugStage = 386;
                if ( relatedEntityTypeData.GetIsEligibleForAnyHack() && relatedSquadOrNull != null && localPlayerFactionOrNull != null )
                {
                    List<HackingType> hacksAgainst = relatedEntityTypeData.GetListOfHacks();
                    if (hacksAgainst != null)
                    {
                        foreach ( HackingType hack in hacksAgainst )
                        {
                            if ( !hack.GetIsHackValidAgainst( relatedSquadOrNull, false ) )
                                continue;
                            try
                            {
                                hack.Implementation.WriteAnySpecialDisplayCodeForHackedShipTooltip( relatedSquadOrNull, localPlayerFactionOrNull, buffer, (BaseTooltipDetail) detailLevel );
                            } 
                            catch ( Exception e )
                            {
                                ArcenDebugging.ArcenDebugLogSingleLine( "Error in WriteAnySpecialDisplayCodeForHackedShipTooltip for '" +
                                    hack.InternalName + "': " + e, Verbosity.ShowAsError );
                            }
                        }
                    }
                }
                debugStage = 390;
                if ( relatedEntityTypeData.RegeneratesDyingShipsAtThisHealthCostRatio > FInt.Zero )
                    buffer.Add( "当此星球上的友方单位将要死亡时，此舰船改为减少自身生命值来复活它们。效率为每 <color=#ffdf72>" )
                        .Add( relatedEntityTypeData.RegeneratesDyingShipsAtThisHealthCostRatio.ReadableString ).Add( " 生命恢复</color>。  " );

                debugStage = 395;

                //Later ship class tooltip
                if ( !relatedEntityTypeData.ShipClass.IsDefault && !relatedEntityTypeData.ShipClass.WriteDescriptionSoonerInTooltip )
                {
                    FactionType fType = FactionType.Player;
                    if ( entityFaction != null )
                        fType = entityFaction.Type;
                    Window_PrototypeInGameHoverEntityInfoUtils.WriteShipClass_Complete( buffer, relatedEntityTypeData.ShipClass, detailLevel, fType, markStats.Speed );
                }

                if ( detailLevel >= TooltipDetail.Full )
                {
                    if ( entity.GetImmuneToCapture() )
                        buffer.Add( "不能被其他阵营捕获。", "999999" );
                }

                if ( !relatedEntityTypeData.ShipClass.CanBeFleetSupercharged && !AIWar2GalaxySettingQuickAccess.DisableFleetWideBonuses )
                    buffer.Add( "不能超载充能。", "999999" );

                debugStage = 400;

                if ( relatedEntityTypeData.SpeedMultiplierFirst5SecondsOnPlanet > FInt.One )
                    buffer.Add( "<color=#f25e1c>快速部署</color>: 此单位在通过虫洞或生成后前5秒内拥有 " ).Add( relatedEntityTypeData.SpeedMultiplierFirst5SecondsOnPlanet ).Add( " 倍速度加成  " );

                if ( relatedEntityTypeData.BuildPointsPerDamageDealt > FInt.Zero && relatedEntityTypeData.UnitToMakeWithBuildPoints_TypeData != null )
                {
                    buffer.Add( "<color=#f25e1c>冯·诺依曼</color>: 建造额外的 " );
                    if ( relatedEntityTypeData.UnitToMakeWithBuildPoints_TypeData.DisplayName.Equals( relatedEntityTypeData.DisplayName ) )
                    {
                        buffer.Add( "自身副本" );
                    } else
                    {
                        buffer.Add( relatedEntityTypeData.UnitToMakeWithBuildPoints_TypeData.DisplayName ).Add( " 单位" );
                    }
                    buffer.Add( "，一旦对敌人造成足够伤害，数量等于其默认舰队编制。  " );
                }

                debugStage = 410;

                if ( relatedEntityTypeData.AIReinforcementMultiplier > FInt.One )
                    buffer.Add( "<color=#f25e1c>AI加速器</color>: 增强此星球的AI增援，倍率为 <color=#ffdf72>" ).Add(
                        relatedEntityTypeData.AIReinforcementMultiplier.ReadableString ).Add( "倍</color>。  " );

                debugStage = 420;

                if ( relatedEntityTypeData.AddsBlackHoleEffectForEntitiesWithEngine_gxLessThan > 0 )
                    buffer.Add( "<color=#f25e1c>黑洞效应</color>: 引擎功率低于 <color=#ffdf72>" ).Add(
                        relatedEntityTypeData.AddsBlackHoleEffectForEntitiesWithEngine_gxLessThan ).Add( "gx</color> 的敌舰无法离开此星球。致残单位无论引擎功率如何均可离开。 " );
                else if ( relatedEntityTypeData.AddsBlackHoleEffectForAllEntitiesPeriod )
                    buffer.Add( "<color=#f25e1c>超级黑洞效应</color>: 任何舰船都无法离开此星球。无论友军、敌军、致残单位，均不例外。 " );

                debugStage = 425;

                if ( relatedEntityTypeData.CannotTargetOrAlertAIReinforcementSpots )
                {
                    buffer.Add( "<color=#f25e1c>AGGRO-隐形</color>: 敌方目标舰船和结构（主要是守卫哨站）中若包含守卫，则无法侦测到本舰，本舰也无法攻击这些目标。 " );
                    if ( detailLevel >= TooltipDetail.Full )
                        buffer.Add( "一旦这些目标被警戒并释放其守卫参战，本舰便可攻击这些目标。  " );
                }

                debugStage = 430;

                if ( relatedEntityTypeData.PeriodicSpawn_InitialDelay > 0 )
                {
                    buffer.Add( "<color=#f25e1c>" );
                    if ( relatedEntityTypeData.PeriodicallySpawnsUnits )
                    {
                        buffer.Add( "巢穴：</color> " );
                        relatedEntityTypeData.PeriodicSpawn_EntityTypeDrawingBag.Value.WriteToBuffer( buffer, relatedEntityTypeData, false );

                        var faction = relatedSquadFactionOrNull ?? relatedSquadOrNull?.GetFactionOrNull_Safe();
                        if (faction != null)
                        {
                            if (faction.Type != FactionType.AI)
                                faction = relatedSquadOrNull.Planet.GetInitialControllingAIFactionOrNull();
                            if (faction != null)
                            {
                                var sentinalData = faction.GetAISentinelsCoreData();
                                if (sentinalData != null)
                                    buffer.Add( " 属于 " ).AddFactionNameInItsColor( sentinalData.GetAiSubFaction(relatedEntityTypeData.Periodic_SpawnFactionForUnit) );
                            }
                        }
                    } else
                    {
                        if ( relatedEntityTypeData.PeriodicSpawn_CreatesWave && relatedEntityTypeData.PeriodicSpawn_CreatesExoStrike )
                        {
                            buffer.Add( "EXO / 突袭引擎：</color> 生成波次和EXO打击" );
                        } else if ( relatedEntityTypeData.PeriodicSpawn_CreatesExoStrike )
                        {
                            buffer.Add( "EXO引擎：</color> 生成EXO打击" );
                        } else
                        {
                            buffer.Add( "突袭引擎：</color> 生成波次" );
                        }
                        buffer.Add( "，强度为 <color=#ffdf72>" ).Add( relatedEntityTypeData.PeriodicSpawn_WaveOrExoSizeMultiplier.ReadableString ).Add( "x </color> 标准" );
                    }

                    buffer.Add( " 每<color=#ffdf72>" ).Add( relatedEntityTypeData.PeriodicSpawn_DelayBetweenSpawns ).Add( " 秒</color>" );
                    if ( relatedEntityTypeData.PeriodicSpawn_InitialDelay > 0 )
                        buffer.Add( "，初始延迟 <color=#ffdf72>" ).Add( relatedEntityTypeData.PeriodicSpawn_InitialDelay ).Add( " 秒</color>" );
                    else
                        buffer.Add( "，立即开始" );

                    buffer.Add( " 当" );
                    if ( relatedEntityTypeData.PeriodicSpawn_MinHostileStrengthToTrigger > 0 )
                        buffer.Add( " 至少有 " ).WrapStrengthTruncated( relatedEntityTypeData.PeriodicSpawn_MinHostileStrengthToTrigger, useIcons, useText ).Add( " 强度的敌军" );
                    if ( relatedEntityTypeData.PeriodicSpawn_OnlyTriggerAgainstPlayer )
                        buffer.Add( " 玩家" );
                    buffer.Add( " 存在被探测到" );
                    if ( relatedEntityTypeData.PeriodicSpawn_OnlyTriggerOnOccupation )
                    {
                        buffer.Add( " 占领星球" );
                    } else
                    {
                        buffer.Add( " 在此星球上" );
                        if ( relatedEntityTypeData.PeriodicSpawn_MaxHopsToTrigger > 0 )
                        {
                            buffer.Add( " 并且" );
                        }
                    }
                    if ( relatedEntityTypeData.PeriodicSpawn_MaxHopsToTrigger > 0 )
                    {
                        buffer.Add( " 在 <color=#ffdf72>" ).Add( relatedEntityTypeData.PeriodicSpawn_MaxHopsToTrigger ).Add( "</color> 次跳跃范围内" );
                    }
                    if ( relatedEntityTypeData.PeriodicSpawn_NeverStopOnceTriggered )
                        buffer.Add( "。一旦首次生成，只要银河系中存在有效敌人，它将不会停止" );
                    buffer.Add( "。  " );
                }

                debugStage = 440;

                if ( relatedEntityTypeData.FreesGuardsWhenLocalForceRatioIsWorseThan > FInt.Zero )
                {
                    debugStage = 450;
                    if ( relatedEntityTypeData.NumberOfHopsOutToFreeGuards > 0 )
                        buffer.Add( "<color=#f25e1c>求援</color>: 此星球 <color=#ffdf72>" ).Add(
                            relatedEntityTypeData.NumberOfHopsOutToFreeGuards ).Add( " 次虫洞跳跃</color>范围内的所有守卫单位将变成威胁（并可能加入猎杀舰队），如果攻击的敌军强度超过此星球AI部队的 <color=#ffdf72> " ).Add(
                            relatedEntityTypeData.FreesGuardsWhenLocalForceRatioIsWorseThan.ReadableString ).Add( "倍</color>。  " );
                    else
                        buffer.Add( "<color=#f25e1c>分散守卫</color>: 此星球上所有守卫单位将变成威胁（并可能加入猎杀舰队），如果此星球AI强度低于敌军的 <color=#ffdf72> " ).Add(
                            relatedEntityTypeData.FreesGuardsWhenLocalForceRatioIsWorseThan.ReadableString ).Add( "倍</color>。  " );
                }
                debugStage = 451;
                if ( relatedEntityTypeData.WatchPlanetsAtXHops > 0 && (relatedSquadOrNull == null || relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player) )
                {
                    debugStage = 455;

                    int hopCount = relatedEntityTypeData.WatchPlanetsAtXHops + (relatedMembershipOrNull == null ? 0 : relatedMembershipOrNull.Hacked_ExtraWatchPlanetsAtXHops);
                    buffer.Add( "<color=#f25e1c>侦察</color>: 监视 " ).Add( hopCount, "ffdf72" );
                    if ( hopCount > 1 )
                        buffer.Add( " 次跳跃范围内的所有星球。  " );
                    else
                        buffer.Add( " 次跳跃范围内的所有星球。  " );
                }

                debugStage = 460;

                if ( relatedEntityTypeData.PushesEnemyShields )
                    buffer.Add( "<color=#f25e1c>诺里斯效应</color>: 移入敌方时会位移敌方泡状力场发生器。  " );

                debugStage = 465;

                if ( relatedEntityTypeData.DisallowKiting && detailLevel >= TooltipDetail.Full )
                    buffer.Add( "不允许放风筝。  " );

                debugStage = 470;

                bool shouldShowDefensiveStructureCapMultiple = relatedEntityTypeData.IsCommandStation && (relatedSquadOrNull == null || relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player);
                string addedBy = "TSSes";
                string postExplanation = " 以 TSS 自身所示数值为基准。不同指挥站、战斗站和堡垒类型根据其性质获得加成或减益。  ";
                if ( World.Instance.GetWasWorldStartedOnGameVersionAtLeastThisVersionOrNewer( 2, 739 ) ) //2739_DSSHacks
                {
                    if ( relatedEntityTypeData.HackableForCommandStationsAndBattleStations_TurretCount <= 0 )
                    {
                        addedBy = "ODSSes";
                        postExplanation = " 以 ODSS 自身所示数值为基准。不同指挥站、战斗站和堡垒类型根据其性质获得加成或减益。  ";
                    }
                    if ( !shouldShowDefensiveStructureCapMultiple )
                    {
                        //this is not a player-owned command station.  So... is it a battlestation or a citadel owned by anyone?
                        if ( relatedEntityTypeData.IsBattlestation )
                            shouldShowDefensiveStructureCapMultiple = true;
                    }
                } else //pre-DSS
                {
                    addedBy = "GCAs";
                    postExplanation = " of the stated amount on the GCA itself.  Different command stations types get bonuses or penalties based on their nature.  ";
                }

                if ( shouldShowDefensiveStructureCapMultiple )
                {
                    string colorForDefensiveCap = "72ffbe"; //very green
                    if ( relatedEntityTypeData.DefensiveStructureCap_Multiplier < FInt.One )
                        colorForDefensiveCap = "ffc872"; //bright orange

                    if ( detailLevel < TooltipDetail.Full )
                    {
                        buffer.StartColor( colorForDefensiveCap ).Add( "防御结构上限: " );
                        buffer.Add( (relatedEntityTypeData.DefensiveStructureCap_Multiplier * 100).GetNearestIntPreferringHigher() );
                        buffer.Add( "%  " ).EndColor();
                    } else
                    {
                        buffer.StartColor( colorForDefensiveCap ).Add( "防御结构上限: " ).EndColor().Add( "由 " )
                            .Add( addedBy ).Add( " 添加的炮塔和其他防御选项数量为 " ).StartColor( colorForDefensiveCap );
                        buffer.Add( (relatedEntityTypeData.DefensiveStructureCap_Multiplier * 100).GetNearestIntPreferringHigher() );
                        buffer.Add( "%" ).EndColor().Add( postExplanation );
                    }
                }
                if ( relatedSquadOrNull != null && relatedSquadOrNull.SpeedLimitFromGroupMove > 0 && detailLevel == TooltipDetail.Full )
                {
                    SpeedGroup groupOnHost = relatedSquadOrNull.GroupMoveSpeed_HostOnly;
                    if ( groupOnHost != null )
                        buffer.Add( " 在速度组 " ).Add( groupOnHost.SpeedGroupID );
                    else
                        buffer.Add( " 在速度组" );
                    buffer.Add( " 中，组速度为 " ).WrapSpeedMoreReadable( relatedSquadOrNull.SpeedLimitFromGroupMove, false, false ).Add( "，计算速度为 " )
                        .WrapSpeedMoreReadable( relatedSquadOrNull.SpeedLimitFromGroupMove, false, false ).Add( "" );
                    if ( groupOnHost != null && groupOnHost.OverrideSpeedLimit > 0 )
                        buffer.Add( "。" ).WrapSpeedMoreReadable( groupOnHost.OverrideSpeedLimit, false, false ).Add( " 覆写速度" );
                    buffer.Add( "，相比 " ).WrapSpeedMoreReadable( relatedMarkLevelData.Speed, false, false ).Add( " 原始速度" );
                    buffer.Add( "。  " );
                }

                debugStage = 480;

                if ( relatedEntityTypeData.OrbitsGravityWellCenterAtItsCurrentRadius ||
                    relatedEntityTypeData.OrbitsParentAtRange > 0 || relatedEntityTypeData.OrbitsFlagshipAtRange > 0 )
                {
                    buffer.Add( "环绕 " );
                    if ( relatedEntityTypeData.OrbitsGravityWellCenterAtItsCurrentRadius )
                    {
                        buffer.Add( "引力井中心" );
                    } else if ( relatedEntityTypeData.OrbitsParentAtRange > 0 )
                    {
                        buffer.Add( "其祖先单位" );
                    } else if ( relatedEntityTypeData.OrbitsFlagshipAtRange > 0 )
                    {
                        buffer.Add( "旗舰" );
                    }
                    buffer.Add( " at " ).StartSpeedWrapper( false ).AddNumberMoreReadable( relatedEntityTypeData.DegreesToOrbitPerSecond );
                    if ( useText )
                        buffer.Add( "度/秒" );
                    else
                        buffer.Add( "°/s" );
                    buffer.EndSpeedWrapper( false );
                    if ( detailLevel >= TooltipDetail.Full )
                    {
                        buffer.Add( " and a distance of " );
                        if ( relatedEntityTypeData.OrbitsGravityWellCenterAtItsCurrentRadius )
                        {
                            if ( relatedSquadOrNull != null )
                                buffer.WrapRangeMoreReadable( relatedSquadOrNull.WorldLocation.GetDistanceTo( Engine_AIW2.Instance.CombatCenter, false ), false, false );
                        } else
                        {
                            buffer.WrapRangeMoreReadable( relatedEntityTypeData.OrbitsParentAtRange, false, false );
                        }
                    }
                    buffer.Add( ".  " );
                }

                debugStage = 490;

                int effectiveShieldRadius = (relatedSquadOrNull != null ? relatedSquadOrNull.GetEffectiveMaxForcefieldRadius() : markStats.ShieldRadius);
                if ( effectiveShieldRadius > 0 )
                {
                    #region Forcefield
                    buffer.Add( "<color=#f25e1c>" );
                    if ( relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack > FInt.Zero )
                    {
                        buffer.StartDamageWrapper( false ).AddPercentFormated( relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack * 100 ).EndDamageWrapper( false );
                        buffer.Add( " 电毒素 " );
                    }
                    if ( relatedEntityTypeData.MyForcefieldDoesNotShrink )
                        buffer.Add( "强化 " );
                    if ( relatedEntityTypeData.MyForcefieldHasNoPenaltyForEnemiesFiringOutOfIt )
                        buffer.Add( "巨力场" );
                    buffer.Add( " 半径</color> " );
                    if ( detailLevel < TooltipDetail.Full )
                    {
                        buffer.WrapRangeMoreReadable( effectiveShieldRadius, false, false );
                        buffer.Add( "。  " );
                    } else if ( detailLevel >= TooltipDetail.Medium )
                    {
                        buffer.Add( " 发生器</color>: 投射半径为 " );
                        buffer.WrapRangeMoreReadable( effectiveShieldRadius, false, false );
                        buffer.Add( " 的泡状力场，总强度为 " );
                        buffer.WrapShieldMoreReadable( (relatedMembershipOrNull != null && relatedMemFleetOrNull != null && relatedMemFleetOrNull.IsPlayerStyleFleet ? relatedMembershipOrNull.GetMaxShieldPoints_PlayerFleetsOnly() : markStats.BaseShieldPoints), useIcons, false );
                        buffer.Add( "。力场内的友舰不会受到伤害，敌舰也无法穿透力场。  " );
                        if ( relatedEntityTypeData.MyForcefieldDoesNotShrink )
                            buffer.Add( "强化力场不会随着护盾生命值下降而缩小。  " );
                        if ( relatedEntityTypeData.MyForcefieldHasNoPenaltyForEnemiesFiringOutOfIt )
                            buffer.Add( "巨力场不会使护盾下的友方射击受到伤害惩罚。  " );
                        else
                            buffer.Add( "任何从护盾下向外射击的友方单位只造成一半伤害。  " );
                        if ( relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack > FInt.Zero )
                        {
                            buffer.Add( "此电毒力场将 " ).StartDamageWrapper( false )
                                .AddPercentFormated( relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack * 100 ).EndDamageWrapper( false )
                                .Add( " 接受到的伤害反弹给射击它的单位。这被视为 <color=#dfff72>特殊伤害</color>。  " );
                        }
                    }
                    buffer.Add( ".  " );
                    #endregion
                } else
                {
                    #region Non-Forcefield
                    if ( relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack > FInt.Zero )
                    {
                        buffer.Add( "<color=#f25e1c>" );
                        buffer.StartDamageWrapper( false ).AddPercentFormated( relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack * 100 ).EndDamageWrapper( false );
                        buffer.Add( " 电毒素船体：</color> " );
                        if ( detailLevel >= TooltipDetail.Medium )
                        {
                            if ( relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack > FInt.Zero )
                            {
                                buffer.Add( "此单位的电毒船体造成 " ).StartDamageWrapper( false )
                                    .AddPercentFormated( relatedEntityTypeData.ReturnsThisPercentageOfDamageIfDamagedByEnemyAttack * 100 ).EndDamageWrapper( false )
                                    .Add( " of any damage it receives back to the unit that shot it. This is considered <color=#dfff72>exotic damage</color>. " );
                            }
                        } else
                            buffer.Add( "返还所受伤害。  " );
                    }
                    #endregion
                }

                debugStage = 500;

                Window_PrototypeInGameHoverEntityInfoUtils.WriteAmplifierAndInhibitorData( buffer, markStats, useIcons, useText );

                debugStage = 520;

                if ( tachyonSystem >= 0 )
                    WriteSystemInfo( buffer, tachyonSystem, relatedSquadOrNull, relatedMembershipOrNull, entityFaction, thisPlanetOrNull, relatedEntityTypeData, effectiveMarkLevel, false, false, detailLevel,
                        isForMultipleUnits, weaponActivityDetails, showWeaponAbortCode, showWeaponTargetInfo, weaponTargetInfoListCount, useIcons, useText );

                debugStage = 530;

                #region tractorSystem
                if ( tractorSystem >= 0 )
                    WriteSystemInfo( buffer, tractorSystem, relatedSquadOrNull, relatedMembershipOrNull, entityFaction, thisPlanetOrNull, relatedEntityTypeData, effectiveMarkLevel, false, false, detailLevel,
                        isForMultipleUnits, weaponActivityDetails, showWeaponAbortCode, showWeaponTargetInfo, weaponTargetInfoListCount, useIcons, useText );
                #endregion

                debugStage = 540;

                #region gravitySystem
                if ( gravitySystem >= 0 )
                    WriteSystemInfo( buffer, gravitySystem, relatedSquadOrNull, relatedMembershipOrNull, entityFaction, thisPlanetOrNull, relatedEntityTypeData, effectiveMarkLevel, false, false, detailLevel,
                        isForMultipleUnits, weaponActivityDetails, showWeaponAbortCode, showWeaponTargetInfo, weaponTargetInfoListCount, useIcons, useText );
                #endregion

                debugStage = 545;

                #region attractantSystem
                if ( attractantSystem >= 0 )
                    WriteSystemInfo( buffer, attractantSystem, relatedSquadOrNull, relatedMembershipOrNull, entityFaction, thisPlanetOrNull, relatedEntityTypeData, effectiveMarkLevel, false, false, detailLevel,
                        isForMultipleUnits, weaponActivityDetails, showWeaponAbortCode, showWeaponTargetInfo, weaponTargetInfoListCount, useIcons, useText );
                #endregion

                debugStage = 550;

                #region cloakSystem
                /*if ( relatedEntityTypeData != null )
                {
                    int cloakingPoints = relatedSquadOrNull.GetCurrentCloakingPoints();
                    if ( cloakingPoints > 0 || cloakSystem >= 0 )
                    {
                        buffer.Add( "当前隐形点数：" ).StartColor( cloakColor ).AddNumberMoreReadable( cloakingPoints ).EndColor().Add( "  " );
                    }
                }*/

                if ( cloakSystem >= 0 )
                    WriteSystemInfo( buffer, cloakSystem, relatedSquadOrNull, relatedMembershipOrNull, entityFaction, thisPlanetOrNull, relatedEntityTypeData, effectiveMarkLevel, false, false, detailLevel,
                        isForMultipleUnits, weaponActivityDetails, showWeaponAbortCode, showWeaponTargetInfo, weaponTargetInfoListCount, useIcons, useText );
                #endregion

                debugStage = 555;

                #region Attrition
                if ( relatedMarkLevelData != null && relatedMarkLevelData.AttritionDamagePreFleetModifiers > 0 )
                {
                    debugStage = 55501;
                    int attritionDamage = relatedMarkLevelData.AttritionDamagePreFleetModifiers;
                    if ( relatedSquadOrNull != null )
                        attritionDamage = relatedSquadOrNull.GetAttritionDamage();
                    debugStage = 55502;
                    if ( detailLevel < TooltipDetail.Full )
                        buffer.Add( "<color=#f25e1c>消耗器：</color>: 每秒 " ).Add( attritionDamage, "a1ffa1" ).Add( "/s " );
                    else
                    {
                        debugStage = 55503;
                        buffer.Add( "<color=#f25e1c>消耗器：</color>: 对移动中的敌方单位每秒造成 " ).Add( attritionDamage, "a1ffa1" )
                            .Add( " 伤害。这被视为 <color=#dfff72>特殊伤害</color>。 " );
                        if ( relatedMarkLevelData.MaxAttritionDamagePreFleetModifiers > 0 )
                        {
                            debugStage = 55504;
                            int maxAttritionDamage;
                            if ( relatedSquadOrNull == null )
                                maxAttritionDamage = relatedMarkLevelData.MaxAttritionDamagePreFleetModifiers;
                            else
                                maxAttritionDamage = relatedSquadOrNull.GetMaxAttritionDamageOrZeroForUnlimited();
                            buffer.Add( " 最高每秒 " ).AddNumberMoreReadable( maxAttritionDamage ).Add( "。 " );
                        } else
                        {
                            buffer.Add( "。 " );
                        }
                    }
                }
                #endregion

                debugStage = 560;

                #region Hardened
                if ( relatedEntityTypeData.DamageTakenCannotBeMoreThanThisPercentageOfMaxHealth > FInt.Zero &&
                    relatedEntityTypeData.DamageTakenCannotBeMoreThanThisPercentageOfMaxHealth < FInt.One )
                {
                    int percentageAsInteger = (relatedEntityTypeData.DamageTakenCannotBeMoreThanThisPercentageOfMaxHealth * 100).GetNearestIntPreferringHigher();
                    if ( detailLevel < TooltipDetail.Full )
                        buffer.Add( "<color=#f25e1c>硬化：</color>: 单次承受伤害从不超过最大船体生命值的 " ).Add( percentageAsInteger ).Add( "%。 " );
                    else
                        buffer.Add( "<color=#f25e1c>硬化：</color>: 来自单一来源的任何伤害（爆炸、射击、消耗等），如果超过此单位最大船体生命值的 " )
                            .Add( percentageAsInteger ).Add( "%，将被降低至该值。可抵御大型火炮、离子炮、质量驱动器等。 " );
                }
                #endregion

                debugStage = 562;

                #region CreatesCeasefireOnPlanet
                if ( relatedEntityTypeData.CreatesCeasefireOnPlanet )
                {
                    buffer.Add( "<color=#f25e1c>停火：</color> 阻止所有单位在与其同星球时开火。 " );
                }
                #endregion

                #region BlocksCeasefireOnPlanet
                if ( relatedEntityTypeData.BlocksCeasefireOnPlanet )
                {
                    buffer.Add( "<color=#f25e1c>停火破坏者：</color> 如果此单位在某个星球上，则不可能停火。 " );
                }
                #endregion

                debugStage = 564;

                #region Harmonic
                if ( relatedMarkLevelData.AmountAddedToDamagePerShipOfThisTypeOnPlanet > 0 )
                {
                    buffer.Add( "<color=#f25e1c>谐波：</color>所有武器获得 " )
                        .WrapDamageMoreReadable( relatedMarkLevelData.AmountAddedToDamagePerShipOfThisTypeOnPlanet, useIcons, useText )
                            .Add( " 行星上每单位此类单位每次射击的伤害" );
                    if ( detailLevel >= TooltipDetail.Full )
                        buffer.Add( "（包括堆叠中的单位）" );
                    if ( relatedEntityTypeData.MaxAmountAddedToDamagePerShipOfThisTypeOnPlanet > 0 )
                        buffer.Add( ", up to an extra " ).WrapDamageMoreReadable( relatedEntityTypeData.MaxAmountAddedToDamagePerShipOfThisTypeOnPlanet, useIcons, useText );
                    buffer.Add( ".  " );
                }
                #endregion

                debugStage = 570;

                //any modular stat boosts
                if ( relatedEntityTypeData.IsModular )
                {
                    for ( int i = 0; i < relatedEntityTypeData.SystemTypes.Count; i++ )
                    {
                        debugStage = 571;
                        EntitySystemTypeData systemData = relatedEntityTypeData.SystemTypes[i];
                        if ( systemData.MaxMarkLevelToFunction < effectiveMarkLevel || systemData.MinMarkLevelToFunction > effectiveMarkLevel || systemData.SystemIsHiddenForUI )
                            continue;
                        if ( systemData.CareAboutStateOfMatterToBeEnabled && systemData.IsInvisibleInTooltipsIfNotAMatchByStateOfMatter )
                        {
                            if ( relatedSquadOrNull != null && relatedSquadOrNull.CurrentStateOfMatter != systemData.MustBeThisStateOfMatterToBeEnabled )
                                continue; //skip completely, since this will be invisible AND disabled
                        }
                        if ( systemData.IsModule && !systemData.IsModuleOn( relatedMembershipOrNull ) )
                            continue;

                        WriteSystemInfo( buffer, i, relatedSquadOrNull, relatedMembershipOrNull, entityFaction, thisPlanetOrNull, systemData, effectiveMarkLevel, false, true, detailLevel, isForMultipleUnits,
                            weaponActivityDetails, showWeaponAbortCode, showWeaponTargetInfo, weaponTargetInfoListCount, ref hasSystemWithBonusFromAttackingUnderForcefields, useIcons, useText );
                    }
                }

                debugStage = 700;
                if ( relatedEntityTypeData.HackingEffectMultiplier != FInt.One )
                {
                    if ( relatedEntityTypeData.HackingEffectMultiplier < FInt.One )
                        buffer.Add( "<color=#f25e1c>黑客加成：</color>" );
                    else
                        buffer.Add( "<color=#f25e1c>黑客惩罚：</color>" );
                    buffer.Add( " 此单位进行的所有黑客入侵的AI反应乘以 <color=#ffdf72>" ).AddNumberMoreReadable( relatedEntityTypeData.HackingEffectMultiplier ).Add( "倍</color>。  " );
                }

                ////////////TODO STOPPED HERE

                if ( !AIWar2GalaxySettingQuickAccess.DisableFleetWideBonuses )
                {
                    int superchargeBonusLimiter = 0;
                    bool isSuperchargeLimited = false;
                    string fleetBonusPrefix = "<color=#7cf21c>舰队加成：</color> <color=#9ce066>";
                    if ( relatedEntityTypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess > 0 && relatedMembershipOrNull != null && relatedMemFleetOrNull != null )
                    {
                        superchargeBonusLimiter = relatedMemFleetOrNull.GetCountOfShipLinesForSuperchargePurposes( null );
                        if ( superchargeBonusLimiter > relatedEntityTypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess )
                        {
                            isSuperchargeLimited = true;
                            fleetBonusPrefix = "<color=#f2ae1c>舰队加成关闭：</color> <color=#e0c266>";
                        } else
                        {
                            fleetBonusPrefix = "<color=#7cf21c>舰队加成开启：</color> <color=#9ce066>";
                        }
                    }

                    if ( relatedEntityTypeData.SuperchargesMinSpeedOfRestOfPlayerFleet )
                    {
                        buffer.Add( fleetBonusPrefix ).Add( "使舰队中所有非旗舰成员的速度至少与自身相同。</color>  " );
                    }
                    if ( relatedEntityTypeData.SuperchargesHullOfRestOfPlayerFleet > FInt.One )
                    {
                        buffer.Add( fleetBonusPrefix ).Add( "为舰队中所有成员（包括旗舰）的船体强度提供 " ).AddFixedDecimal( relatedEntityTypeData.SuperchargesHullOfRestOfPlayerFleet.ToDoubleNonSim(), 2 ).Add( " 倍加成。</color>  " );
                    }
                    if ( relatedEntityTypeData.SuperchargesShieldsOfRestOfPlayerFleet > FInt.One )
                    {
                        buffer.Add( fleetBonusPrefix ).Add( "为舰队中所有成员（包括旗舰）的护盾强度提供 " ).AddFixedDecimal( relatedEntityTypeData.SuperchargesShieldsOfRestOfPlayerFleet.ToDoubleNonSim(), 2 ).Add( " 倍加成。</color>  " );
                    }
                    if ( relatedEntityTypeData.SuperchargesAttackPowerOfRestOfPlayerFleet > FInt.One )
                    {
                        buffer.Add( fleetBonusPrefix ).Add( "为舰队中所有成员（包括旗舰）的攻击强度提供 " ).AddFixedDecimal( relatedEntityTypeData.SuperchargesAttackPowerOfRestOfPlayerFleet.ToDoubleNonSim(), 2 ).Add( " 倍加成。</color>  " );
                    }
                    if ( relatedEntityTypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess > 0 )
                    {
                        if ( isSuperchargeLimited )
                            buffer.Add( "<color=#f2ae1c>舰队加成限制已超：</color> <color=#e0c266>舰队范围加成仅在拥有 " ).Add(
                                relatedEntityTypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess )
                                .Add( " 条或更少非旗舰、非精英舰船编制的舰队中生效，但此舰队有 " ).Add( superchargeBonusLimiter ).Add( " 条。</color>  " );
                        else if ( relatedMembershipOrNull == null || detailLevel >= TooltipDetail.Full )
                        {
                            buffer.Add( "<color=#7cf21c>舰队加成限制：</color> <color=#9ce066>舰队范围加成仅在拥有 " ).Add(
                                    relatedEntityTypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess );

                            if ( relatedMembershipOrNull != null )
                                buffer.Add( " 条或更少非旗舰、非精英舰船编制的舰队中生效，此舰队仅有 " ).Add( superchargeBonusLimiter ).Add( " 条。</color>  " );
                            else
                                buffer.Add( " 条或更少非旗舰、非精英舰船编制的舰队中生效。</color>  " );
                        }
                    }
                    if ( relatedEntityTypeData.CannotBeSuperchargedWhenInSuperchargedFleet && detailLevel >= TooltipDetail.Full )
                    {
                        buffer.Add( "<color=#f2ae1c>舰队加成不适用：</color> <color=#e0c266>无论何种情况，舰队范围加成对此特定单位无帮助。</color>  " );
                    }
                } else
                {
                    bool shouldDrawFleetWideBonusNote = false;
                    if ( relatedEntityTypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess > 0 && relatedMembershipOrNull != null && relatedMemFleetOrNull != null )
                        shouldDrawFleetWideBonusNote = true;
                    else if ( relatedEntityTypeData.SuperchargesMinSpeedOfRestOfPlayerFleet )
                        shouldDrawFleetWideBonusNote = true;
                    else if ( relatedEntityTypeData.SuperchargesHullOfRestOfPlayerFleet > FInt.One )
                        shouldDrawFleetWideBonusNote = true;
                    else if ( relatedEntityTypeData.SuperchargesShieldsOfRestOfPlayerFleet > FInt.One )
                        shouldDrawFleetWideBonusNote = true;
                    else if ( relatedEntityTypeData.SuperchargesAttackPowerOfRestOfPlayerFleet > FInt.One )
                        shouldDrawFleetWideBonusNote = true;
                    else if ( relatedEntityTypeData.SuperchargesOnlyApplyWhenFleetLineCountIsXOrLess > 0 )
                        shouldDrawFleetWideBonusNote = true;

                    if ( shouldDrawFleetWideBonusNote )
                        buffer.Add( "<color=#f2ae1c>FLEET WIDE BONUS DISABLED:</color> <color=#e0c266>Because of galaxy options, potentially relating to your campaign type, fleet-wide bonues are not allowed.</color>  " );
                }
                // && !this.TypeData.CannotBeSuperchargedWhenInSuperchargedFleet

                //Pretty late note items
                //--------------------------------------

                //int cloakingPoints = relatedEntity.GetCurrentCloakingPoints();
                //if ( cloakingPoints > 0 )
                //    buffer.StartColor( cloakColor ).Add( "Cloaking Points: " ).Add( cloakingPoints ).EndColor();

                debugStage = 900;

                if ( relatedEntityTypeData.IncomingDamageModifiers_FullList.Count > 0 )
                {
                    for ( int k = 0; k < relatedEntityTypeData.IncomingDamageModifiers_FullList.Count; k++ )
                    {
                        WriteDamageModifierData( buffer, markStats.MarkLevel.Ordinal,
                            relatedEntityTypeData.IncomingDamageModifiers_FullList[k], null );
                    }
                }

                debugStage = 950;

                if ( relatedEntityTypeData.ExternalInvulnerabilityUnitRequiredCount > 0 )
                {
                    string protectorStringMax = GetProtectorString( relatedEntityTypeData.ExternalInvulnerabilityUnitType,
                            relatedEntityTypeData.ExternalInvulnerabilityUnitTag, relatedEntityTypeData.ExternalInvulnerabilityUnitRequiredCount );
                    if (relatedSquadOrNull == null)
                    {
                        buffer.Add( "<color=#f25e1c>无敌</color>: 需要至少 <color=#cfd988>" )
                            .Add( relatedEntityTypeData.ExternalInvulnerabilityUnitRequiredCount ).Add( "</color> " ).Add( protectorStringMax ).EndColor();
                        if ( relatedEntityTypeData.InvulnerabilityRegion == ExternalInvulnerabilityRegion.ThisPlanet )
                            buffer.Add( " 在此星球上" );
                        else
                            buffer.Add( " 在银河系中" );
                       buffer.Add( " 才可免疫所有伤害。  " );
                    } else
                    {
                        if( relatedSquadOrNull.CountOfEntitiesProvidingExternalInvulnerability >= relatedEntityTypeData.ExternalInvulnerabilityUnitRequiredCount )
                        {
                            buffer.Add( "<color=#f25e1c>INVINCIBLE</color>: " );
                            if ( detailLevel >= TooltipDetail.Medium )
                                buffer.Add( "所需 " );
                        } else
                        {
                            buffer.Add( "<color=#07e83c>VULNERABLE</color>: ");
                            if ( detailLevel >= TooltipDetail.Medium )
                                buffer.Add( "仅 " );
                        }
                        buffer.StartColor( "cfd988" ).Add( relatedSquadOrNull.CountOfEntitiesProvidingExternalInvulnerability );
                        if ( detailLevel >= TooltipDetail.Medium )
                            buffer.Add( " 分之 " );
                        else
                            buffer.Add( " / " );
                        buffer.Add( relatedEntityTypeData.ExternalInvulnerabilityUnitRequiredCount ).Add( "</color> " ).Add( protectorStringMax );
                        if ( relatedSquadOrNull.CountOfEntitiesProvidingExternalInvulnerability < relatedEntityTypeData.ExternalInvulnerabilityUnitRequiredCount )
                            buffer.Add( " 所需" );
                        if ( relatedSquadOrNull.CountOfEntitiesProvidingExternalInvulnerability == 1 )
                            buffer.Add( " 已存在" );
                        else
                            buffer.Add( " 已存在" );
                        if ( detailLevel >= TooltipDetail.Medium )
                        {
                            if ( relatedEntityTypeData.InvulnerabilityRegion == ExternalInvulnerabilityRegion.ThisPlanet )
                                buffer.Add( " 在此星球上。  " );
                            else
                                buffer.Add( " 在银河系中。  " );
                        } else
                        {
                            buffer.Add( "。  " );
                        }
                    }
                }

                debugStage = 1001;

                if ( !EntityTypeDrawingBag.IsNullOrInvalid( relatedEntityTypeData.SpawnOnDeath_EntityTypeDrawingBag.Value ) )
                {
                    buffer.Add( "<color=#f25e1c>死亡时</color>: " );
                    relatedEntityTypeData.SpawnOnDeath_EntityTypeDrawingBag.Value.WriteToBuffer( buffer, relatedEntityTypeData, false );
                    buffer.Add( ".  " );
                }

                // Puffin Note Hydra Regeneration
                if ( relatedEntityTypeData.SecondsToFullyRegenerateHull > FInt.Zero )
                {
                    if ( relatedSquadOrNull != null )
                    {
                        buffer.Add( "<color=#f25e1c>再生</color>: 在 <color=#ffdf72>" ).Add( relatedEntityTypeData.SecondsToFullyRegenerateHull )
                        .Add( "</color> 秒内恢复船体，前提是不受攻击。 " );
                    }
                }

                debugStage = 1010;

                if( relatedEntityTypeData.SpecialType == SpecialEntityType.AIKingCommandStation || relatedEntityTypeData.SpecialType == SpecialEntityType.AIKingMobile )
                {
                    buffer.Add( "AI 进程 (AIP) 将<color=#ffdf72>上升 " ).Add( relatedEntityTypeData.AIPOnDeath ).Add( "</color> 如果此单位死亡。  " ); //this is the normal case
                } else if ( (relatedEntityTypeData.SpecialType == SpecialEntityType.AICommandStationReconquest && localPlayerPlanetFactionOrNull != null && localPlayerPlanetFactionOrNull.AIPLeftFromCommandStation > 0) )
                {
                    debugStage = 1011;
                    //special case: reconquest command station that you haven't paid the AIP price for yet (ie some minor faction non-allied to you killed the first command station, so you still get charged)
                    buffer.Add( "由于你尚未为此星球支付 AI 进程 (AIP) 代价，AIP 将<color=#ffdf72>上升 " ).Add( localPlayerPlanetFactionOrNull.AIPLeftFromCommandStation ).Add( "</color> if this dies.  " ); //this is the normal case
                } else if ( relatedEntityTypeData.AIPOnDeath > 0 )
                {
                    debugStage = 1012;
                    bool wasPriceAlreadyPaid = false;
                    bool noPriceToPay = false;
                    if ( relatedSquadOrNull != null )
                    {
                        debugStage = 1013;
                        if ( relatedEntityTypeData.AIPOnDeathOnlyWhenOwnedByAI &&
                             relatedSquadOrNull.PlanetFaction.Faction.Type != FactionType.AI )
                            noPriceToPay = true;
                        Faction factionToUse = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                        if ( factionToUse != null )
                        {
                            Planet relatedPlanet = relatedSquadOrNull == null ? null : relatedSquadOrNull.Planet;
                            PlanetFaction localPlanetFaction = relatedPlanet == null ? null : relatedPlanet.GetPlanetFactionForFaction( factionToUse );
                            if ( localPlanetFaction != null && relatedSquadOrNull.GetMatches_SemiSlow( EntityRollupType.WarpEntryPoints ) && localPlanetFaction.AIPLeftFromWarpGate == 0 )
                                wasPriceAlreadyPaid = true; //the AIP price has already been paid for destroying this structure, so AIP won't increase
                        }
                    }
                    if ( !wasPriceAlreadyPaid && !noPriceToPay )
                        buffer.Add( "AI 进程 (AIP) 将<color=#ffdf72>上升 " ).Add( relatedEntityTypeData.AIPOnDeath ).Add( "</color> 如果此单位死亡。  " ); //this is the normal case
                } else if ( relatedEntityTypeData.AIPOnDeath < 0 )
                    buffer.Add( "AI 进程 (AIP) 将<color=#ffdf72>减少 " ).Add( -relatedEntityTypeData.AIPOnDeath ).Add( "</color> 如果此单位死亡。  " );
                debugStage = 1015;

                relatedEntityTypeData.ForAnyDataExtensions_AddToTooltip_GainsSection_ForEntity( buffer, relatedSquadOrNull, relatedMembershipOrNull, relatedEntityTypeData, detailLevel );
                string playerClarification = " ";
                if ( FactionUtilityMethods.Instance.AnyNecromancerFactions() )
                    playerClarification = " 人类帝国 ";
                if ( relatedEntityTypeData.MetalToGrantOnDeath > 0 && showMetalOther )
                {
                    buffer.Add( "如果" ).Add( playerClarification ).Add( "玩家击杀此单位，将获得 <color=#ffdf72>" ).Add( relatedEntityTypeData.MetalToGrantOnDeath ).Add( " 金属。 </color>" );
                }
                if ( relatedEntityTypeData.ScienceToGrantOnDeath > 0 && relatedEntityTypeData.HackingToGrantOnDeath > 0 )
                {
                    buffer.Add( "如果" ).Add( playerClarification ).Add( "玩家击杀此单位，将获得 <color=#ffdf72>" ).Add( relatedEntityTypeData.ScienceToGrantOnDeath ).Add( " 科学 </color>和 <color=#ffdf72>" ).Add( relatedEntityTypeData.HackingToGrantOnDeath ).Add( " 黑客点数。 </color>" );
                } else if ( relatedEntityTypeData.ScienceToGrantOnDeath > 0 )
                {
                    buffer.Add( "如果" ).Add( playerClarification ).Add( "玩家击杀此单位，将获得 <color=#ffdf72>" ).Add( relatedEntityTypeData.ScienceToGrantOnDeath ).Add( " 科学。 </color>" );
                } else if ( relatedEntityTypeData.HackingToGrantOnDeath > 0 )
                {
                    buffer.Add( "如果" ).Add( playerClarification ).Add( "玩家击杀此单位，将获得 <color=#ffdf72>" ).Add( relatedEntityTypeData.HackingToGrantOnDeath ).Add( " 黑客点数。 </color>" );
                }
                debugStage = 1020;

                if ( relatedEntityTypeData.AIPOnDeathWhenNoneLeft > 0 )
                    buffer.Add( "如果剩余的所有 <color=#ffdf72>" )
                        .Add( BaseScenario.GetCountOfMatchingAIPOnDeathWhenNoneLeft( relatedEntityTypeData ) ).Add( "</color> 个单位全部死亡，AI进度（AIP）将<color=#ffdf72>增加 " ).Add( relatedEntityTypeData.AIPOnDeathWhenNoneLeft ).Add( "</color>。  " );
                else if ( relatedEntityTypeData.AIPOnDeathWhenNoneLeft < 0 )
                    buffer.Add( "如果剩余的所有 <color=#ffdf72>" )
                        .Add( BaseScenario.GetCountOfMatchingAIPOnDeathWhenNoneLeft( relatedEntityTypeData ) ).Add( "</color> 个单位全部死亡，AI进度（AIP）将<color=#ffdf72>减少 " ).Add( -relatedEntityTypeData.AIPOnDeathWhenNoneLeft ).Add( "</color>。  " );

                debugStage = 1030;

                if ( DetailFlags.HasFlag( ShipExtraDetailFlags.AIPCostOnGrant ) || DetailFlags.HasFlag( ShipExtraDetailFlags.AnyGrantHackInfo ) )
                {
                    if ( relatedEntityTypeData.AIPWhenGrantedByHack > 0 )
                        buffer.Add( "\n<color=#ff9072>如果你通过入侵获取此单位，AI进度（AIP）将增加 " ).Add( relatedEntityTypeData.AIPWhenGrantedByHack.ReadableString ).Add( "。</color>\n" );
                }

                debugStage = 1500;

                #region Tech Upgrades!

                if ( detailLevel >= TooltipDetail.Medium )
                {
                    Faction factionToUse = null;
                    if ( entity != null )
                        factionToUse = entity.GetFactionOrNull_Safe();

                    if ( factionToUse == null )
                        factionToUse = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    
                    // only show this on ships owned/ownable by the player
                    if (factionToUse == null || 
                        factionToUse.Type == FactionType.Player || 
                        factionToUse.Type == FactionType.NaturalObject)
                    {
                        if ( markStats.MarkLevel.Ordinal <= 0 )
                        {
                            // This is redundant. If you don't see the techs that upgrade it, it isn't upgradable.
                            //buffer.Add( "无法升级。  " );
                        }
                        else
                        {
                            buffer.NewLineIfNeeded();
                            
                            if ( relatedEntityTypeData.StartingMarkLevel.Ordinal > 1 )
                                buffer.Add( "起始等级 " ).StartColor( relatedEntityTypeData.StartingMarkLevel.ColorHex )
                                    .Add( "Mark " ).Add( relatedEntityTypeData.StartingMarkLevel.Ordinal ).EndColor().Add( ".  " );

                            if ( relatedEntityTypeData.TechUpgradesThatBenefitMe == null || relatedEntityTypeData.TechUpgradesThatBenefitMe.Count == 0 )
                            {
                                // This is redundant. If you don't see the techs that upgrade it, it isn't upgradable.
                                //buffer.Add( "未被任何科技升级。  " );
                            }
                            else
                            {
                                debugStage = 1510;
                                bool alsoShowShipLineCountWithSameTech = false;
                                if ( IsFromSidebarType == FromSidebarType.Sidebar_MultipleUnits )
                                    alsoShowShipLineCountWithSameTech = true;
                                
                                if ( relatedEntityTypeData.TechUpgradesThatBenefitMe.Count == 1 )
                                {
                                    debugStage = 1520;
                                    TechUpgrade upgrade = relatedEntityTypeData.TechUpgradesThatBenefitMe[0];
                                    //this is for Unused or UpgradeMeFromFleetOnly
                                    if ( !upgrade.IsVisibleOnMenus )
                                    {
                                        debugStage = 1530;
                                        WriteTechThatBenefits( buffer, upgrade, factionToUse, alsoShowShipLineCountWithSameTech, ref debugStage );
                                        buffer.Add( "  " );
                                    } 
                                    else
                                    {
                                        debugStage = 1540;
                                        if ( detailLevel == TooltipDetail.Full )
                                            buffer.Add( "科技升级：" );
                                        else
                                            buffer.Add( "科技：" );
                                        
                                        WriteTechThatBenefits( buffer, upgrade, factionToUse, alsoShowShipLineCountWithSameTech, ref debugStage );
                                        buffer.Add( "  " );
                                    }
                                } 
                                else
                                {
                                    debugStage = 1550;

                                    if ( detailLevel == TooltipDetail.Full )
                                        buffer.Add( "科技升级：" );
                                    else
                                        buffer.Add( "科技：" );
                                    
                                    for ( int i = 0; i < relatedEntityTypeData.TechUpgradesThatBenefitMe.Count; i++ )
                                    {
                                        if ( i > 0 )
                                            buffer.Add( ", " );
                                        WriteTechThatBenefits( buffer, relatedEntityTypeData.TechUpgradesThatBenefitMe[i], factionToUse, alsoShowShipLineCountWithSameTech, ref debugStage );
                                    }
                                    buffer.Add( "  " );
                                }
                            }
                        }
                    }
                }
                #endregion

                //Very late note items
                //--------------------------------------

                debugStage = 2000;

                //only show this for non-player ships, or ships that are not yet fully claimed
                if ( isToBeClaimed )
                {
                    debugStage = 2010;
                    if ( relatedEntityTypeData.MarkStatsFor( effectiveMarkLevel ).MetalCostToClaim > 0 )
                    {
                        debugStage = 2020;
                        if ( relatedEntityTypeData.AIPToClaim > 0 )
                        {
                            if ( detailLevel >= TooltipDetail.Medium )
                                buffer.Add( "<color=#ff5bf2>占领成本：</color>要占领此单位，你必须控制行星，消耗 <color=#ffdf72>" ).AddNumberMoreReadable( relatedEntityTypeData.MarkStatsFor( effectiveMarkLevel ).MetalCostToClaim )
                                    .Add( " 金属</color>，同时AI进度（AIP）将增加 <color=#ff620c>" ).Add( relatedEntityTypeData.AIPToClaim.ReadableString ).Add( "</color>。  " );
                            else
                                buffer.Add( "<color=#ff5bf2>占领成本：</color> <color=#ffdf72>" ).AddNumberMoreReadable( relatedEntityTypeData.MarkStatsFor( effectiveMarkLevel ).MetalCostToClaim )
                                    .Add( " 金属</color> 和 <color=#ff620c>" ).Add( relatedEntityTypeData.AIPToClaim.ReadableString ).Add( " AIP</color>。  " );
                        } else
                        {
                            if ( detailLevel >= TooltipDetail.Medium )
                                buffer.Add( "<color=#ff5bf2>占领成本：</color>要占领此单位，你必须控制行星，消耗 <color=#ffdf72>" ).AddNumberMoreReadable( relatedEntityTypeData.MarkStatsFor( effectiveMarkLevel ).MetalCostToClaim ).Add( " 金属</color>。  " );
                            else
                                buffer.Add( "<color=#ff5bf2>占领成本：</color> <color=#ffdf72>" ).AddNumberMoreReadable( relatedEntityTypeData.MarkStatsFor( effectiveMarkLevel ).MetalCostToClaim ).Add( " 金属</color>。  " );
                        }
                    } else if ( relatedEntityTypeData.AIPToClaim > 0 )
                    {
                        if ( detailLevel >= TooltipDetail.Medium )
                            buffer.Add( "<color=#ff5bf2>占领成本：</color>要占领此单位，你必须控制行星，AI进度（AIP）将增加 <color=#ff620c>" ).Add( relatedEntityTypeData.AIPToClaim.ReadableString ).Add( "</color>。  " );
                        else
                            buffer.Add( "<color=#ff5bf2>占领成本：</color> <color=#ff620c>" ).Add( relatedEntityTypeData.AIPToClaim.ReadableString ).Add( " AIP</color>。  " );
                    } else if ( relatedEntityTypeData.AIPToClaim < 0 )
                    {
                        if ( detailLevel >= TooltipDetail.Medium )
                            buffer.Add( "<color=#5be6ff>占领奖励：</color>要占领此单位，你必须控制行星，AI进度（AIP）将减少 <color=#72fff7>" ).Add( relatedEntityTypeData.AIPToClaim.ReadableString ).Add( "</color>（非常好！）。  " );
                        else
                            buffer.Add( "<color=#5be6ff>占领奖励：</color> <color=#72fff7>-" ).Add( relatedEntityTypeData.AIPToClaim.ReadableString ).Add( " AIP</color>。  " );
                    }
                }

                //only show this for actual ships
                if ( relatedSquadOrNull != null )
                {
                    if ( detailLevel >= TooltipDetail.Medium )
                    {
                        List<HackingType> hacksAgainst = relatedEntityTypeData.GetListOfHacks();
                        if ( hacksAgainst.Count > 0 )
                        {
                            bool hasDoneHeader = false;
                            foreach ( HackingType hack in hacksAgainst )
                            {
                                if ( hack.GetShouldSkipHackInEntityTooltip( relatedSquadOrNull ) )
                                    continue;
                                if ( !hasDoneHeader )
                                {
                                    hasDoneHeader = true;
                                    buffer.Add( "<color=#f25e1c>你可入侵：</color> " );
                                } else
                                {
                                    buffer.Add( ", " );
                                }
                                buffer.Add( hack.GetDisplayName( relatedSquadOrNull.TypeData, true ) );
                            }
                            if ( hasDoneHeader )
                                buffer.Add( "  " );
                        }
                    }
                }

                //parent stuff
                if ( relatedSquadOrNull != null )
                {
                    GameEntity_Squad parent = relatedSquadOrNull.ParentGameEntity.GetSquad();
                    if ( parent != null )
                    {
                        buffer.Add( "<color=#f25e1c>祖先</color>: " );
                        buffer.Add( "祖先单位：" ).Add( parent.GetTypeDisplayNameSafe() );
                        if ( relatedEntityTypeData.DiesIfParentDies )
                            buffer.Add( "  （如果祖先死亡则此单位也会死亡）" );
                        buffer.Add( "。  " );
                    }
                } else
                {
                    if ( relatedEntityTypeData.DiesIfParentDies )
                    {
                    buffer.Add( "<color=#f25e1c>祖先</color>: " );
                    buffer.Add( "通常有祖先单位，如果祖先死亡则此单位也会死亡。  " );
                    }
                }

                //child stuff
                if ( relatedEntityTypeData.BuildPointsPerSecond > 0 )
                {
                    buffer.Add( "<color=#f25e1c>祖代</color>: " );
                    float perSecondRate = relatedEntityTypeData.BuildPointCostForPerSecondConstruction <= 0 ? -1 :
                        ((float) relatedEntityTypeData.BuildPointCostForPerSecondConstruction / (float) relatedEntityTypeData.BuildPointsPerSecond);
                    buffer.Add( "最多建造 " ).Add( relatedEntityTypeData.PersonalShipCapForPerSecondBuildPointConstruction )
                        .Add( " 个后代" );

                    if ( relatedSquadOrNull != null )
                    {
                        buffer.Add( "（当前：" ).Add( relatedSquadOrNull.ChildSquads.Count ).Add( "）" );
                    }

                    buffer.Add( "，每 " ).AddFixedDecimal( perSecondRate, 2 ).Add( "秒建造一个。  " );

                    List<GameEntityTypeData> list = GameEntityTypeDataTable.Instance.RowsByTag[relatedEntityTypeData.TagToSpawnFromForBuildPointForPerSecondConstruction];
                    if ( list == null || list.Count == 0 )
                        buffer.Add( "错误！没有可建造的后代！" );
                    else if ( list.Count == 1 )
                        buffer.Add( "后代为：" ).Add( list[0].GetDisplayName() ).Add( "  " );
                    else
                    {
                        buffer.Add( "后代混合了：" );
                        for ( int i = 0; i < list.Count; i++ )
                        {
                            if ( i > 0 )
                            {
                                if ( i == list.Count - 1 )
                                {
                                    if ( i == 1 )
                                        buffer.Add( " 和 " );
                                    else
                                        buffer.Add( "，和 " );
                                } else
                                    buffer.Add( ", " );
                            }
                            GameEntityTypeData childType = list[i];
                            buffer.Add( childType.GetDisplayName() );

                            int currentCount = 0;
                            if ( relatedSquadOrNull != null )
                            {
                                foreach ( LazyLoadSquadWrapper child in relatedSquadOrNull.ChildSquads )
                                {
                                    GameEntity_Squad childSquad = child.GetSquad();
                                    if ( childSquad != null && childSquad.TypeData == childType )
                                        currentCount++;
                                }
                            }
                            if ( currentCount > 0 )
                                buffer.Add( " (" ).Add( currentCount ).Add( detailLevel >= TooltipDetail.Full ? " 当前)" : ")" );
                        }
                        buffer.Add( ".  " );
                    }

                    if ( relatedSquadOrNull != null && detailLevel >= TooltipDetail.Full )
                    {
                    buffer.Add( "（进度：" ).AddNumberMoreReadable( relatedSquadOrNull.BuildPoints ).Add( " / " )
                        .AddNumberMoreReadable( relatedEntityTypeData.BuildPointCostForPerSecondConstruction )
                        .Add( "）  " ); ;
                    }
                }

                //Last note items
                //--------------------------------------
                debugStage = 3000;

                buffer.StartColor( "888888" );

                debugStage = 3010;

                if ( (relatedEntityTypeData.IsElite) && detailLevel >= TooltipDetail.Medium )
                    buffer.Add( "精英：每支舰队只能添加一条精英舰船线。  " );

                debugStage = 3020;

                if ( (relatedEntityTypeData.ProvidesAIWarpEntryPoint || relatedEntityTypeData.IsWarpBeacon) && detailLevel >= TooltipDetail.Medium )
                    buffer.Add( "允许 AI 舰船跃迁至此。  " );

                debugStage = 3030;

                if ( relatedEntityTypeData.IsMobile && relatedEntityTypeData.FleetMembershipStyle == FleetMembershipStyle.Planetary && detailLevel >= TooltipDetail.Medium )
                    buffer.Add( "无法穿越虫洞。  " );

                debugStage = 3030;

                if ( relatedEntityTypeData.AutomaticallyDiesWithCommandStation )
                    buffer.Add( "如果指挥站被摧毁则自毁。  " );

                debugStage = 3040;

                if ( relatedEntityTypeData.DiesAfterLifetimeSec > 0 )
                    buffer.Add("在 ").AddMinutesAndSeconds( relatedEntityTypeData.DiesAfterLifetimeSec, "ffa1a1").Add( " 后过期。" );
                
                if ( relatedEntityTypeData.SelfAttritionsXPercentPerSecondIfParentShipNotOnPlanet > FInt.Zero && !relatedEntityTypeData.AlwaysSelfAttritions && detailLevel >= TooltipDetail.Full )
                    buffer.Add( "每秒损失 <color=#ffdf72>" ).Add( relatedEntityTypeData.SelfAttritionsXPercentPerSecondIfParentShipNotOnPlanet.ReadableString )
                        .Add( "%</color> 船体生命值，当其不在母舰所在星球时。  " );

                debugStage = 3045;

                if ( relatedEntityTypeData.AlwaysSelfAttritions && detailLevel >= TooltipDetail.Full )
                    buffer.Add( "每秒损失 <color=#ffdf72>" ).Add( relatedEntityTypeData.SelfAttritionsXPercentPerSecondIfParentShipNotOnPlanet.ReadableString )
                        .Add( "%</color> 船体生命值。  " );

                debugStage = 3050;

                // This is already described in the ship class line...
                /*
                if ( !relatedEntityTypeData.ShipClass.CanBeDamaged )
                    buffer.Add( "免疫所有伤害。  " );
                */

                debugStage = 3051;

                if ( relatedEntityTypeData.ImmuneToRepairs )
                {
                    if ( detailLevel >= TooltipDetail.Full )
                        buffer.Add( "无法修复 -- 我们不知道这东西怎么修。  " );
                    else
                        buffer.Add( "无法修复。  " );
                }

                debugStage = 3052;

                // disabled this message, it is redundant on anything not player owned
                /*
                if ( detailLevel >= TooltipDetail.Full && !relatedEntityTypeData.IsScrappingByPlayerDisallowed ) {
                    int percentageToReturnOfMetalAsInt = AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "ScrapRefundsOnFriendlyPlanets" );
                    FInt percentageToReturnOfMetalAsFIntMult = (FInt)percentageToReturnOfMetalAsInt / FInt.OneHundred;
                    if (percentageToReturnOfMetalAsInt > 0) {
                        int metalCostForScrapping = (markStats.MetalCost * relatedEntityTypeData.MetalCostMultiplierForScrapping * percentageToReturnOfMetalAsFIntMult).GetNearestIntPreferringHigher();
                        if ( relatedEntityTypeData.MetalCostMultiplierForScrapping == FInt.Zero ) {
                            buffer.Add( "Scrapping this unit gives no metal.  ");
                        } else {
                            buffer.Add( "在友方星球拆解此单位返还 ").AddMetal_MoreReadable(metalCostForScrapping, true).Add(".  ");
                        }
                    }
                }
                */

                debugStage = 3053;

                if ( relatedEntityTypeData.ImmuneToProtectionByForcefields )
                {
                    if ( detailLevel >= TooltipDetail.Full )
                        buffer.Add( "由于与现实结构产生奇怪交互，无法被力场保护。  " );
                    else
                        buffer.Add( "无法被力场保护。  " );
                }

                if ( relatedEntityTypeData.ImmuneToBonusDamage )
                {
                    buffer.Add( "免疫敌方武器系统加成伤害。  " );
                }

                if ( relatedEntityTypeData.CanPassThroughEnemyForcefields )
                {
                    if ( detailLevel >= TooltipDetail.Full )
                        buffer.Add( "<color=#f25e1c>力场谐波</color>: 可匹配护盾谐波以穿过敌方力场，但其武器射击仍会击中力场本身。  " );
                    else
                        buffer.Add( "<color=#f25e1c>力场谐波</color>: 可穿过敌方力场。  " );
                }

                debugStage = 30531100;

                if ( relatedSquadOrNull != null && relatedSquadOrNull.CurrentStateOfMatter.ShouldShowDescriptionInTooltips )
                {
                    buffer.Add( "<color=#ff5bf2>" ).Add( relatedSquadOrNull.CurrentStateOfMatter.DisplayName ).Add( " 物质状态" );
                    if ( detailLevel >= TooltipDetail.Full )
                    {
                        buffer.Add( ": " );
                        buffer.Add( relatedSquadOrNull.CurrentStateOfMatter.DescriptionFull );
                        buffer.Add( "</color>  " );
                    } else if ( detailLevel >= TooltipDetail.Medium )
                    {
                        buffer.Add( ": " );
                        buffer.Add( relatedSquadOrNull.CurrentStateOfMatter.DescriptionShort );
                        buffer.Add( "</color>  " );
                    } else
                        buffer.Add( "</color>  " );
                }

                debugStage = 30531200;

                if ( relatedEntityTypeData.AlternativeStateOfMatter != null && relatedEntityTypeData.PhasesToOtherAlternativeStateOfMatterAfterSeconds > 0 )
                {
                    int returnTime = relatedEntityTypeData.PhasesBackToDefaultStateOfMatterAfterSeconds;
                    if ( returnTime <= 0 )
                        returnTime = relatedEntityTypeData.PhasesToOtherAlternativeStateOfMatterAfterSeconds;

                    if ( detailLevel >= TooltipDetail.Full )
                    {
                        buffer.Add( "与时空结构的奇怪交互导致此单位仅部分时间存在于正常位面" );
                        if ( relatedEntityTypeData.AlternativeStateOfMatter.CanTargetOtherUnitsInThisState )
                            buffer.Add( "，使其在另一种状态下无敌且隐形。  " );
                        else
                            buffer.Add( "，使其在另一种状态下无敌且隐形，但对于同样处于该状态的其他单位除外。  " );
                    }

                    buffer.Add( "相位切换至 " ).Add( relatedEntityTypeData.AlternativeStateOfMatter.DisplayName ).Add( " 每 " )
                        .Add( relatedEntityTypeData.PhasesToOtherAlternativeStateOfMatterAfterSeconds )
                        .Add( " 秒，然后 " )
                        .Add( returnTime ).Add( " 秒后切回。  " );
                }

                debugStage = 30531300;

                if ( relatedEntityTypeData.StateOfMatterToBecomeOnWormholeExit != null )
                {
                    int returnTime = relatedEntityTypeData.ReturnsToDefaultStateOfMatterAfterSecondsFromWormholeExit;
                    buffer.Add( "<color=#f25e1c>相位跃迁</color>: 每当通过虫洞时，相位切换至 " ).Add( relatedEntityTypeData.StateOfMatterToBecomeOnWormholeExit.DisplayName );
                    if ( returnTime > 0 )
                        buffer.Add( "，然后在 " ).Add( returnTime ).Add( " 秒后恢复正常。  " );
                    else
                        buffer.Add( "，且不计划恢复正常。  " );
                }

                debugStage = 30531400;

                if ( relatedEntityTypeData.ForcedToBeThisStateOfMatterWhenOnFormerOrCurrentAIHomeworldOrBastionWorldsUnlessEnemyKingPresent != null && relatedEntityTypeData.ForcedToBeThisStateOfMatterWhenOnFormerOrCurrentAIHomeworldOrBastionWorldsUnlessEnemyKingPresent.ShouldShowDescriptionInTooltips)
                {
                    buffer.Add( "<color=#f25e1c>AI 核心相位</color>: 当处于当前或前 AI 母星或 AI 堡垒世界时，相位切换至 " ).Add( relatedEntityTypeData.ForcedToBeThisStateOfMatterWhenOnFormerOrCurrentAIHomeworldOrBastionWorldsUnlessEnemyKingPresent.DisplayName )
                        .Add( "（除非有敌方首领在场）。  " );
                }

                debugStage = 3060;

                if ( relatedEntityTypeData.ImmuneToAllDamageForSecondsAfterCreation > 0 && detailLevel >= TooltipDetail.Medium )
                {
                    if ( relatedSquadOrNull != null )
                    {
                        if ( relatedEntityTypeData.ImmuneToAllDamageForSecondsAfterCreation > relatedSquadOrNull.GetSecondsSinceCreation() )
                            buffer.StartColor( QuickColors.NewValue ).Add( "免疫所有伤害，持续 " ).Add( relatedEntityTypeData.ImmuneToAllDamageForSecondsAfterCreation -
                                relatedSquadOrNull.GetSecondsSinceCreation() ).Add( " 秒。  " ).EndColor();
                    } else
                        buffer.Add( "免疫所有伤害，持续 " ).StartColor( QuickColors.NewValue )
                            .Add( relatedEntityTypeData.ImmuneToAllDamageForSecondsAfterCreation ).Add( "s</color> after creation.  " );
                }
                
                debugStage = 3061;

                if (relatedEntityTypeData.UnitToMakeWithBuildPointsFromDamageTaken != null)
                {
                    buffer.Add("生产 ")
                        .AddFactionColoredString(relatedEntityTypeData.UnitToMakeWithBuildPointsFromDamageTaken.GetShortDisplayName(), relatedSquadFactionOrNull);

                    if (relatedSquadOrNull != null)
                    {
                        // this should be accessed from the entity... or exposed as a tag... or something...
                        var costToSpawn = ((relatedSquadOrNull.GetMaxHullPoints() + relatedSquadOrNull.GetMaxShieldPoints()) / (relatedSquadOrNull.CurrentMarkLevel + 1));
                        buffer.StartColor(QuickColors.HeaderBright).Add("（").AddPercentRoundedDynamically(relatedSquadOrNull.BuildPoints, costToSpawn).Add("）").EndColor();
                    }

                    buffer.Add(" 当其受到伤害时。");
                }

                debugStage = 3062;
                // Puffin Note Neinzul Fireflies
                if ( relatedSquadOrNull != null && relatedSquadOrNull.NumberOfWeaponPoints > FInt.Zero )
                {
                    if ( relatedSquadOrNull != null )
                    {
                        buffer.Add( "当前拥有 <color=#ffdf72>" ).Add( relatedSquadOrNull.NumberOfWeaponPoints )
                        .Add( "</color> 武器点。 " );
                    }
                }

                debugStage = 3063;
                // For Death Effects. Lists each one a unit has been hit by, the current amount, and the amount required.
                // pair.Value = current amount, pair.Key.Scale = amount required (scale being from the XML for each death effect).
                if ( detailLevel >= TooltipDetail.Medium )
                {
                    if ( relatedSquadOrNull != null && relatedSquadOrNull.DeathEffectCausingDamageReceivedToEntity.Count > 0 )
                    {
                        int pairCount = relatedSquadOrNull.DeathEffectCausingDamageReceivedToEntity.Count;
                        foreach ( KeyValuePair<DeathEffectType, int> pair in relatedSquadOrNull.DeathEffectCausingDamageReceivedToEntity )
                        {
                            buffer.Add( "\nCurrently has <color=#ffdf72>" ).Add( pair.Value ).Add( " " )
                                .Add( pair.Key.DescriptionDamageName ).Add( "</color> damage out of " ).Add( pair.Key.Scale ).Add( " needed for effect. " );
                            continue;
                        }
                    }
                }

                debugStage = 3070;

                if ( World_AIW2.Instance.IsFuelEnabled && relatedSquadOrNull != null )
                {
                    if ( relatedSquadOrNull.GetIsOutguardUnit() )
                    {
                        if ( AIWar2GalaxySettingQuickAccess.OutguardAlsoCostXenon )
                        {
                            FInt worstXenonRatio = FInt.One;
                            foreach ( Faction player in World_AIW2.Instance.AllPlayerFactions )
                            {
                                if ( worstXenonRatio > player.FuelXenonOveruseRatio )
                                    worstXenonRatio = player.FuelXenonOveruseRatio;
                            }
                            if ( worstXenonRatio < FInt.One )
                                buffer.Add( "\n<color=#ff6935>Currently health and shields are reduced by the worst player Xenon ratio, which is " )
                                    .AddFixedDecimal( worstXenonRatio.ToFloatNonSim(), 2 ).Add( "x normal.</color>  " );
                        }
                    } else
                    {
                        switch ( relatedSquadOrNull.TypeData.FuelUseType )
                        {
                            case ResourceType.FuelArgon:
                                {
                                    if ( relatedSquadFactionOrNull != null && relatedSquadFactionOrNull.FuelArgonOveruseRatio < FInt.One )
                                        buffer.Add( "\n<color=#ff6935>Currently health and shields are reduced by your Argon ratio, which is " )
                                            .AddFixedDecimal( relatedSquadFactionOrNull.FuelArgonOveruseRatio.ToFloatNonSim(), 2 ).Add( "x normal.</color>  " );
                                }
                                break;
                            case ResourceType.FuelRadon:
                                {
                                    if ( relatedSquadFactionOrNull != null && relatedSquadFactionOrNull.FuelRadonOveruseRatio < FInt.One )
                                        buffer.Add( "\n<color=#ff6935>Currently health and shields are reduced by your Radon ratio, which is " )
                                            .AddFixedDecimal( relatedSquadFactionOrNull.FuelRadonOveruseRatio.ToFloatNonSim(), 2 ).Add( "x normal.</color>  " );
                                }
                                break;
                            case ResourceType.FuelXenon:
                                {
                                    if ( relatedSquadFactionOrNull != null && relatedSquadFactionOrNull.FuelXenonOveruseRatio < FInt.One )
                                        buffer.Add( "\n<color=#ff6935>Currently health and shields are reduced by your Xenon ratio, which is " )
                                            .AddFixedDecimal( relatedSquadFactionOrNull.FuelXenonOveruseRatio.ToFloatNonSim(), 2 ).Add( "x normal.</color>  " );
                                }
                                break;
                        }
                    }
                }

                if ( detailLevel >= TooltipDetail.Full )
                {
                    if ( relatedEntityTypeData.IsCrippledInsteadOfDying && (relatedSquadOrNull == null || relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player ||
                        relatedSquadOrNull.GetFactionTypeSafe() == FactionType.NaturalObject) )
                    {
                        buffer.Add( "不会死亡，而是在 1 HP 时变为重创状态。  " );
                        if ( relatedEntityTypeData.ForcedToBailOutOnCripple_Any )
                            buffer.Add( "重创时将使用弹射功能前往友方星球。  " );
                        else if ( relatedEntityTypeData.ForcedToBailOutOnCripple_DeepstrikeOnly )
                            buffer.Add( "在深袭区域重创时将使用弹射功能前往友方星球。  " );
                    } else if ( relatedEntityTypeData.DiesToRemains && (relatedSquadOrNull == null || relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player) )
                        buffer.Add( "由人类控制时，死亡变为可重建的残骸。  " );
                    else if ( relatedEntityTypeData.RevertsToNeutralOnDeathIfPermadeathSettingIsFalse != null && relatedEntityTypeData.RevertsToNeutralOnDeathIfPermadeathSettingIsFalse.Length > 0 )
                    {
                        if ( !World_AIW2.Instance.Setup.GetBoolBySetting( relatedEntityTypeData.RevertsToNeutralOnDeathIfPermadeathSettingIsFalse ) )
                            buffer.Add( "死亡时恢复为中立状态，而非真正死亡。  " );
                    }
                }
                debugStage = 30710;
                EntityOrderCollection orders = relatedSquadOrNull?.Orders;
                debugStage = 30711;
                EntityOrder firstOrder = orders == null ? new EntityOrder( -1 ) : orders.GetQueuedOrderAtIndex_OrNull( 0 );
                EntityOrder lastOrder = orders == null ? new EntityOrder( -1 ) : orders.GetLastQueuedOrder_OrNull();
                debugStage = 30712;

                if ( relatedSquadOrNull != null && relatedSquadOrNull.IsAfterANonHumanTeam_NonSim )
                {
                    buffer.Add( "\n<color=#ff5bf2>不是追你" );
                    if ( detailLevel >= TooltipDetail.Full )
                    {
                        buffer.Add( "：</color>如果你靠近该舰船它会向你开火，但除此之外它会忽略你。它正忙于追捕 " );
                        buffer.Add( relatedSquadOrNull.CalculateFactionIsChasingInsteadOfHumans() );
                        buffer.Add( ".  " );
                    } else if ( detailLevel >= TooltipDetail.Medium )
                    {
                        buffer.Add( ":</color> Hunting " );
                        buffer.Add( relatedSquadOrNull.CalculateFactionIsChasingInsteadOfHumans() );
                        buffer.Add( ".  " );
                    } else
                        buffer.Add( "</color>  " );
                }

                if ( showDebugInfoInTooltip && relatedSquadOrNull != null )
                {
                    debugStage = 3072;
                    if ( orders != null )
                    {
                        debugStage = 3073;
                        buffer.Add( "行为：" + orders.Behavior + "。" );
                        if ( orders.GetQueuedOrderCount() == 0 || firstOrder.TypeData == null )
                            buffer.Add( "无排队命令\n" );
                        else
                        {
                            debugStage = 3074;
                            if ( firstOrder.TypeData.Type == EntityOrderType.Wormhole && detailLevel >= TooltipDetail.Full )
                            {
                                buffer.Add( "此单位有 " + orders.GetQueuedOrderCount() + " 个排队命令，" + firstOrder.TypeData.Type + "。" + World_AIW2.Instance.GetPlanetByIndex( firstOrder.RelatedPlanetIndex ).Name );
                                debugStage = 30741;
                                if ( orders.GetQueuedOrderCount() > 1 && lastOrder.TypeData != null )
                                {
                                    Planet planet = World_AIW2.Instance.GetPlanetByIndex( lastOrder.RelatedPlanetIndex );
                                    if ( planet != null )
                                        buffer.Add( " --> " ).Add( planet.Name );
                                }
                                buffer.Add( "。" );
                            } else
                                buffer.Add( "此单位有 " + orders.GetQueuedOrderCount() + " 个排队命令，第一个为 " + firstOrder.TypeData.Type + "。" );
                        }
                    }
                    debugStage = 3075;
                    if ( relatedSquadOrNull.GetFactionTypeSafe() == FactionType.AI &&
                       relatedSquadOrNull.TypeData.IsMobile && relatedSquadOrNull.TypeData.IsCombatant )
                    {
                        //                        bool isAttacker = ( relatedEntity.GetEffectiveOrders().Behavior == EntityBehaviorType.Attacker_Full );
                        //                        if ( isAttacker && relatedEntity.Guarding.GetSquad() == null)
                        debugStage = 3076;
                        if ( relatedSquadOrNull.GuardedUnit.GetSquad() == null )
                        {
                            debugStage = 3077;
                            buffer.Add( "威胁 " );
                            Faction againstFaction = null;
                            if ( orders != null )
                            {
                                buffer.Add( " 对抗 " ).Add( orders.BehaviorRelatedFactionIndex );
                                againstFaction = World_AIW2.Instance.GetFactionByIndex( orders.BehaviorRelatedFactionIndex );
                                if ( againstFaction != null )
                                {
                                    if ( againstFaction.Type == FactionType.SpecialFaction )
                                    {
                                        buffer.Add( ": " ).Add( againstFaction.GetDisplayName() );
                                    } else
                                        buffer.Add( ": " ).Add( EnumNameCache.GetName( againstFaction.Type ) );
                                } else {
                                    buffer.Add(": players");
                                }
                                buffer.Add( ". " );
                            }
                            if ( relatedSquadOrNull.WaitingAgainstPlanetIndex != -1 )
                            {
                                debugStage = 3078;
                                int secondsIHaveBeenWaiting = World_AIW2.Instance.GameSecond - relatedSquadOrNull.StartedWaitingAtGameSecond;
                                buffer.Add( "正在等待对抗 " + World_AIW2.Instance.GetPlanetByIndex( relatedSquadOrNull.WaitingAgainstPlanetIndex ).Name +
                                    "，已等待 " + secondsIHaveBeenWaiting + " 秒。 " );
                            }
                            if ( relatedSquadOrNull.TypeData.NotEligibleToJoinHunterFleet || relatedSquadOrNull.TypeData.IsDrone )
                                buffer.Add( "此类舰船将留在威胁舰队中，而不会加入猎手。 " );
                            else if ( againstFaction != null && againstFaction.Type != FactionType.Player )
                                buffer.Add( "此舰船将留在威胁舰队中，因为它不针对人类。 " );
                            else
                            {
                                Faction facOrNull = relatedSquadOrNull.GetFactionOrNull_Safe();
                                AISentinelsCoreData factionExternal = facOrNull == null ? null : facOrNull.TryGetAISentinelsCoreData()?.SentinelInfo;
                                if ( factionExternal == null )
                                    buffer.Add( "此舰船将留在威胁舰队中，因其未链接到哨兵蜂巢思维。 " );
                                else
                                {
                                    int secondsIHaveBeenWaiting = World_AIW2.Instance.GameSecond - relatedSquadOrNull.StartedWaitingAtGameSecond;
                                    if ( secondsIHaveBeenWaiting < 0 )
                                        secondsIHaveBeenWaiting = 0;

                                    int hunterFleetWaitThreshold = factionExternal.AIDifficulty.SecondsThreatWaitsBeforeJoiningHunterFleet;

                                    int secondsIHaveBeenThreatfleet = relatedSquadOrNull.BecameThreatfleetAtGameSecond <= 0 ? 0 : World_AIW2.Instance.GameSecond - relatedSquadOrNull.BecameThreatfleetAtGameSecond;
                                    int hunterFleetExistsThreshold = factionExternal.AIDifficulty.SecondsThreatExistsAsThreatBeforeJoiningHunterFleet;

                                    buffer.Add( "此舰船将在 " ).Add( hunterFleetWaitThreshold - secondsIHaveBeenWaiting )
                                        .Add( " 秒的待机攻击时间后离开威胁舰队加入猎手，或在经过 " ).Add( hunterFleetExistsThreshold - secondsIHaveBeenThreatfleet )
                                        .Add( " 秒的存在时间后离开。 " );
                                }
                            }

                        }
                        debugStage = 3079;
                        if ( relatedSquadOrNull.ExoGalacticAttackPlanetIdx != -1 )
                        {
                            GameEntity_Squad target = relatedSquadOrNull.ExoGalacticAttackTarget.GetSquad();
                            if ( target != null )
                                buffer.Add( "属于外银河打击部队，目标为 " + target.TypeData.GetDisplayName() + " 位于 " + target.GetPlanetName_Safe() + "。 " );
                            else
                                buffer.Add( "属于外银河打击部队，目标为 " + World_AIW2.Instance.GetPlanetByIndex( relatedSquadOrNull.ExoGalacticAttackPlanetIdx ).Name + " 但无明确目标。 " );
                        }
                        GameEntity_Squad guarded = relatedSquadOrNull.GuardedUnit.GetSquad();
                        if ( guarded != null )
                            buffer.Add( "守卫 " + guarded.TypeData.InternalName );

                    }
                }
                debugStage = 30712;
                if ( relatedSquadOrNull != null && relatedSquadOrNull.DespawnsInXSeconds > 0 &&
                     detailLevel >= TooltipDetail.Medium )
                    buffer.Add( "此实体将在 " + relatedSquadOrNull.DespawnsInXSeconds + " 秒后消失。 ", "7486d1" );

                debugStage = 3080;

                buffer.EndColor();

                //Now to the appender
                //--------------------------------------

                debugStage = 5000;
                if ( relatedEntityTypeData.DescriptionAppender != null && relatedSquadOrNull != null && !relatedSquadOrNull.IsFakeEntity )
                    relatedEntityTypeData.DescriptionAppender.AddToDescriptionBuffer( relatedSquadOrNull, relatedEntityTypeData, buffer );

                //And then the "what am I doing right now" appender
                //--------------------------------------

                debugStage = 5400;

                if ( owningFactionOrNull?.Type == FactionType.Player )
                {
                    var effectiveGalaxyCap = relatedEntityTypeData.CalculateEffectiveGalaxyWideCapForPlayersConstructing(owningFactionOrNull);
                    if (effectiveGalaxyCap > 0)
                    {
                        int countOfExisting = 0;
                        foreach ( GameEntity_Squad squad in owningFactionOrNull.Squads( EntityRollupType.HasGalaxyWideCapForPlayersConstructing ) )
                        {
                            if ( squad.TypeData.GalaxyWideCapMatchString == relatedEntityTypeData.GalaxyWideCapMatchString )
                                countOfExisting++;
                        }

                        buffer.Add( "     Galaxy-Wide Cap: " ).Add( countOfExisting );
                        buffer.Add( "/" ).Add( effectiveGalaxyCap );

                        if ( detailLevel >= TooltipDetail.Full )
                        {
                            if ( relatedEntityTypeData.GalaxyWideCapForPlayersConstructingXPlanetVariable > 0 &&
                                 relatedEntityTypeData.AddedGalaxyWideCapForPlayersConstructingPerXPlanets > 0 )
                            {
                                buffer.Add( "<size=80%>" );
                                buffer.Add( " (" ).Add( relatedEntityTypeData.BaseGalaxyWideCapForPlayersConstructing );
                                buffer.Add( " + " ).Add( relatedEntityTypeData.AddedGalaxyWideCapForPlayersConstructingPerXPlanets );
                                buffer.Add( " per " ).Add( relatedEntityTypeData.GalaxyWideCapForPlayersConstructingXPlanetVariable );
                                buffer.Add( " player-owned planets. ").Add(World_AIW2.Instance.PlayerOwnedPlanets, "a1ffa1" ).Add(" Player planets Owned)" );
                                buffer.Add( "</size>  " );
                            }
                        }
                        else
                            buffer.Add( "  " );
                    }
                }

                if ( relatedSquadOrNull != null )
                {
                    #region Fleet Centerpiece Things
                    debugStage = 5460;
                    if ( relatedMembershipOrNull == null || relatedMemFleetOrNull == null )
                    {
                        debugStage = 546100;
                        if ( relatedSquadOrNull.HasBeenRemovedFromSim || relatedSquadOrNull.ToBeRemovedAtEndOfThisFrame )
                            buffer.Add( "<color=#ff5842>This ship has died.</color>  " );
                        else
                            buffer.Add( "<color=#ff5842>ERROR: my FleetMembership is null!</color>  " );
                    } else if ( relatedMemFleetOrNull.Centerpiece.GetSquad() == relatedSquadOrNull )
                    {
                        bool isCity = relatedSquadOrNull.TypeData.SpecialType == SpecialEntityType.CityCenter;

                        debugStage = 546200;
                        if ( detailLevel < TooltipDetail.Full )
                            buffer.Add( "是 " ).Add( isCity ? relatedSquadOrNull.TypeData.NameForCityCenter : "旗舰" ).Add( " 的舰队 " )
                                .Add( relatedMemFleetOrNull.GetName() )
                                .Add( " 拥有 " );
                        else
                            buffer.Add( "我是 " ).Add( isCity ? relatedSquadOrNull.TypeData.NameForCityCenter : "旗舰" ).Add( " 的舰队 " )
                                .Add( relatedMemFleetOrNull.GetName() )
                                .Add( " with " );

                        debugStage = 546300;
                        //useLocalUIValues is calculated inside here.
                        {
                            bool isForPlayer = relatedSquadFactionOrNull != null && ( relatedSquadFactionOrNull.Type == FactionType.NaturalObject);
                            int currentStrength = relatedMemFleetOrNull.GetCurrentStrengthOfFleet_ForUIOnly( isForPlayer );
                            int maxStrength = relatedMemFleetOrNull.GetMaxStrengthOfFleet_ForUIOnly( isForPlayer );
                            int currentUnits = relatedMemFleetOrNull.GetCurrentCountOfFleet_ForUIOnly();
                            int maxUnits = relatedMemFleetOrNull.GetMaxCountOfFleet_ForUIOnly( isForPlayer );
                            FInt estimatedFleetPowerForColor = (FInt.OneHundred * currentStrength * currentUnits) / (maxStrength * maxUnits);
                            buffer.WrapStrengthTruncated( currentStrength, useIcons, false ).Add( " / " ).WrapStrengthTruncated( maxStrength, false, useText ).Add( " (" )
                                .StartPercentageInColor( estimatedFleetPowerForColor, true, false).AddNumberMoreReadable( currentUnits ).Add( " / " ).AddNumberMoreReadable( maxUnits )
                                .Add( " units)" ).EndColor();
                        }
                        buffer.Add( ".  " );
                        if ( relatedMembershipOrNull != null && relatedMemFleetOrNull != null &&
                             relatedMemFleetOrNull.TimesCrippled_UIOnly > 0 &&
                             detailLevel >= TooltipDetail.Full )
                        {
                            buffer.Add( "此旗舰已被重创 " ).Add( relatedMemFleetOrNull.TimesCrippled_UIOnly, "a1ffa1" ).Add( " 次。  " );
                        }

                        debugStage = 546400;

                        if ( isCity && relatedMemFleetOrNull != null )
                        {
                            int totalCityPoints = relatedMemFleetOrNull.CalculateTotalCitySockets();
                            if ( totalCityPoints > 0 )
                            {
                                int spentCityPoints = relatedMemFleetOrNull.CalculateSpentCitySockets();
                                buffer.Add( relatedEntityTypeData.NameForCitySockets_Plural ).Add( " used: " ).Add( spentCityPoints ).Add( "/" ).Add( totalCityPoints ).Add( ".  " );
                            }
                        }
                        if ( relatedSquadOrNull == null || relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player )
                        {
                            if ( relatedEntityTypeData.FiringDelayForTransportedShips > FInt.Zero && !isCity )
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                    buffer.Add( "卸载的舰船有 " ).Add( relatedEntityTypeData.FiringDelayForTransportedShips, "a1ffa1" ).Add( " 秒的开火延迟。  " );
                                else if ( detailLevel >= TooltipDetail.Medium )
                                    buffer.Add( "从此旗舰卸载的舰船有 " ).Add( relatedEntityTypeData.FiringDelayForTransportedShips, "a1ffa1" ).Add( " 秒后才能开火。  " );
                            }
                        }
                        if ( detailLevel >= TooltipDetail.Full && relatedMemFleetOrNull != null && relatedMemFleetOrNull.FleetOnFriendlyPlanet && !isCity )
                        {
                            buffer.Add( "此舰队位于友方星球，可以更快重建舰船。  " );
                        }

                        if ( relatedMemFleetOrNull != null && relatedMemFleetOrNull.IsFleetInTransportLoadMode && !isCity )
                        {
                            if ( detailLevel < TooltipDetail.Full )
                                buffer.Add( "运输就绪模式。  " );
                            else
                                buffer.Add( "我的舰队处于运输就绪模式，所有舰船正试图进入舱位。  " );
                        }

                        debugStage = 5465;
                        bool isForCapture = relatedMemFleetOrNull != null && relatedMemFleetOrNull.GetFactionType_Safe() == FactionType.NaturalObject;
                        //buffer.Add( " useLocalUIValues = " + useLocalUIValues + " " + relatedEntity.GetFleetFactionType_Safe() );

                        debugStage = 5462;
                        //if ( detailLevel >= TooltipDetail.Medium )
                        if ( relatedMemFleetOrNull != null && localPlayerFactionOrNull != null )
                        {
                            ExternalFleetBaseInfo fleetBaseInfoOrNull = relatedMemFleetOrNull.BaseInfo;
                            if ( fleetBaseInfoOrNull != null )
                                fleetBaseInfoOrNull.AddToTooltipForFleet( buffer, detailLevel );

                            buffer.NewLine();
                            if ( fleetInfoList == null )
                                fleetInfoList = List<Window_PrototypeInGameHoverEntityInfoUtils.FleetShipStatsForDisplay>.Create_WillNeverBeGCed( 40, "Window_PrototypeInGameHoverEntityInfo-FleetInfoList" );
                            Window_PrototypeInGameHoverEntityInfoUtils.BuildFleetShipStatsForDisplay( relatedMemFleetOrNull, fleetInfoList, isCity,
                                GameSettings.Current.GetBoolBySetting( "Tooltip_ShowIndividualFleetMemberships" ), isForCapture, localPlayerFactionOrNull, detailLevel );
                            Window_PrototypeInGameHoverEntityInfoUtils.WriteFleetMembershipTooltip( buffer, fleetInfoList, isForCapture, useIcons, useText );
                        }
                    }

                    debugStage = 87394000;
                    if ( relatedSquadOrNull.ActiveHack != null )
                    {
                        debugStage = 87394010;
                        buffer.Add( "此单位正在黑客入侵；它将引擎能量转移到黑客矩阵，因此速度降低且无法离开星球。 " );
                        if ( relatedSquadOrNull.GetMaxCloakingPoints() > 0 )
                            buffer.Add( "黑客入侵会禁用舰船的隐形系统。 " );
                    }
                    #endregion

                    debugStage = 87396000;
                    #region Error Checks For Strange Things
                    if ( showDebugInfoInTooltip )
                    {
                        if ( relatedMembershipOrNull != null && !relatedMembershipOrNull.DoesShipLineContainThisExactShip( relatedSquadOrNull ) )
                            buffer.Add( "<color=#ff731e>ERROR!  The fleet membership for this ship does not actually contain it!</color>  " );
                        if ( relatedSquadOrNull.ToBeRemovedAtEndOfThisFrame )
                            buffer.Add( "<color=#ff731e>ERROR!  ToBeRemovedAtEndOfThisFrame = true!</color>  " );
                        if ( relatedSquadOrNull.HasBeenRemovedFromSim )
                            buffer.Add( "<color=#ff731e>ERROR!  HasBeenRemovedFromSim = true!</color>  " );
                        if ( relatedSquadOrNull.IHaveDiedSoPleaseDisregardAnyRequestForANewVisualObject )
                            buffer.Add( "<color=#ff731e>ERROR!  IHaveDiedSoPleaseDisregardAnyRequestForANewVisualObject = true!</color>  " );
                        if ( relatedSquadOrNull.HasDoneOnDeathSinceLastClaimed )
                            buffer.Add( "<color=#ff731e>ERROR!  HasDoneOnDeathSinceLastClaimed = true!</color>  " );
                        if ( Engine_AIW2.Instance.CurrentGameViewMode != GameViewMode.GalaxyMapView )
                        {
                            if ( relatedSquadOrNull.InstancedRenderer == null )
                                buffer.Add( "<color=#ff731e>ERROR!  InstancedRenderer is null!</color>  " );
                            if ( relatedSquadOrNull.Planet != Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() )
                                buffer.Add( "<color=#ff731e>ERROR!  Planet of this ship is " ).Add( relatedSquadOrNull.Planet == null ? "null" : relatedSquadOrNull.GetPlanetName_Safe() ).Add( " instead of planet being viewed.</color>  " );
                        }

                        List<PlannedMetalFlow> flows = relatedSquadOrNull.SquadPlannedFlows.GetDisplayList();
                        if ( flows.Count > 0 )
                        {
                            buffer.Add( "<color=#6bffec>Metal Flows: " ).Add( flows.Count );
                            if ( detailLevel >= TooltipDetail.Full )
                            {
                                for ( int flowIndex = 0; flowIndex < flows.Count; flowIndex++ )
                                {
                                    if ( flowIndex > 0 )
                                        buffer.Add( ", " );
                                    else
                                        buffer.Add( ": " );
                                    PlannedMetalFlow plannedFlow = flows[flowIndex];
                                    if ( plannedFlow.FromEntity == null )
                                    {
                                        buffer.Add( "[null flow]" );
                                        continue;
                                    }
                                    if ( !plannedFlow.IsInRangeAtTheMoment )
                                        buffer.Add( "(Out of Range) " );
                                    switch ( plannedFlow.Purpose )
                                    {
                                        case MetalFlowPurpose.AssistSelfConstruction:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Help Build", true );
                                            break;
                                        case MetalFlowPurpose.AssistFactoryConstruction:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Assist Factory", true );
                                            break;
                                        case MetalFlowPurpose.ClaimingNeutrals:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Claim", true );
                                            break;
                                        case MetalFlowPurpose.RebuildingRemains:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Rebuild", true );
                                            break;
                                        case MetalFlowPurpose.RepairingEnginesOfFriendlies:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Rep Eng", true );
                                            break;
                                        case MetalFlowPurpose.RepairingHullsOfFriendlies:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Rep Hull", true );
                                            break;
                                        case MetalFlowPurpose.RepairingShieldsOfFriendlies:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Rep Shld", true );
                                            break;
                                        case MetalFlowPurpose.BuildingDronesInternally:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Build Drones", true );
                                            break;
                                        case MetalFlowPurpose.FactoryConstructionForPlayerMobileFleets:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Factory Work", true );
                                            break;
                                        case MetalFlowPurpose.SelfConstruction:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, "Self Build", true );
                                            break;
                                        default:
                                            WritePlannedMetalFlowFullTargetInfo( buffer, plannedFlow, relatedSquadOrNull, EnumNameCache.GetName( plannedFlow.Purpose ), true );
                                            break;
                                    }
                                }
                            }
                            buffer.Add( "</color>  " );
                        }
                    }
                    #endregion

                    debugStage = 87398000;

                    #region Codehooks
                    {
                        var codeHook = ArcenExternalCodeHookTable.Instance.GetRowByNameOrNullIfNotFound( "GlobalEntityDescriptionAppender" );
                        if ( codeHook != null )
                            codeHook.HandleAllSubscribedHooks( buffer, relatedSquadOrNull, null, Engine_AIW2.Instance.MainThreadContext_ClientOrHost );
                        //else
                            //ArcenDebugging.ArcenDebugLog( "Could not find GlobalEntityDescriptionAppender", Verbosity.ShowAsError );
                    }
                    #endregion

                    debugStage = 87399000;

                    #region Exo stuff
                    if ( relatedEntityTypeData.ExoGenerationDifficulty > 0 )
                    {
                        AIDifficulty highestDifficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty_AsDifficulty();
                        if ( highestDifficulty.Difficulty >= relatedEntityTypeData.ExoGenerationDifficulty )
                        {
                            buffer.Add( "如果你占领此建筑，AI 将派出外银河打击部队对付你。 " );
                        }
                    }
                    #endregion

                    debugStage = 87401000;

                    if ( relatedEntityTypeData.OnlyCloakedWhenOwningPlanet && detailLevel > TooltipDetail.Medium )
                        buffer.Add( "此单位仅在其阵营控制该星球时保持隐形。 " );

                    debugStage = 87402000;

                    #region AIReinforcementPointContents
                    if ( relatedSquadOrNull.AIReinforcementPointContents != null && relatedSquadOrNull.AIReinforcementPointContents.Count > 0 )
                    {
                        int countOfItemTypes = 0;
                        int countOfItems = 0;
                        int strengthOfItems = 0;
                        RefPair<GameEntityTypeData, int> content;
                        for ( int i = 0; i < relatedSquadOrNull.AIReinforcementPointContents.Count; i++ )
                        {
                            content = relatedSquadOrNull.AIReinforcementPointContents[i];
                            if ( content.RightItem > 0 )
                            {
                                countOfItemTypes++;
                                countOfItems += content.RightItem;
                                strengthOfItems += (content.RightItem * content.LeftItem.MarkStatsFor( relatedSquadOrNull.CurrentMarkLevel ).StrengthPerSquad_CalculatedWithNullFleetMembership);
                            }
                        }
                        if ( countOfItemTypes > 0 )
                        {
                            if ( detailLevel >= TooltipDetail.Medium )
                                buffer.Add( "此 AI 增援点包含 " ).StartColor( QuickColors.NewValue )
                                     .Add( countOfItems ).Add( " 艘舰船</color>，总强度 " );
                            else

                                buffer.Add( "包含 " ).StartColor( QuickColors.NewValue )
                                     .Add( countOfItems ).Add( " 艘舰船</color>，强度 " );
                            buffer.Add( ArcenExternalUIUtilities.Strength );
                            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, strengthOfItems, true, true );
                            buffer.Add( "</color>." );

                            if ( detailLevel >= TooltipDetail.Medium )
                            {
                                buffer.Add( "  They are of the types: " );
                                bool isFirst = true;
                                for ( int i = 0; i < relatedSquadOrNull.AIReinforcementPointContents.Count; i++ )
                                {
                                    content = relatedSquadOrNull.AIReinforcementPointContents[i];
                                    if ( content.RightItem > 0 )
                                    {
                                        if ( isFirst )
                                            isFirst = false;
                                        else
                                            buffer.Add( ", " );
                                        buffer.Add( content.RightItem ).Add( "x " ).StartColor( QuickColors.NewValue ).Add( content.LeftItem.DisplayName ).Add( "</color>" );
                                    }
                                }
                                buffer.Add( "  If perturbed, it may choose to let these out.  " );
                            }
                        }
                    }
                    #endregion

                    debugStage = 87403000;

                    #region AIReinforcementPointReasonCodesForDebugging
                    if ( relatedSquadOrNull.AIReinforcementPointReasonCodesForDebugging != null && relatedSquadOrNull.AIReinforcementPointReasonCodesForDebugging.Count > 0 )
                    {
                        if ( detailLevel >= TooltipDetail.Medium )
                        {
                            buffer.Add( "增援调试原因代码：" );
                            bool isFirst = true;
                            RefPair<string, int> content;
                            for ( int i = 0; i < relatedSquadOrNull.AIReinforcementPointReasonCodesForDebugging.Count; i++ )
                            {
                                content = relatedSquadOrNull.AIReinforcementPointReasonCodesForDebugging[i];
                                if ( content.RightItem > 0 )
                                {
                                    if ( isFirst )
                                        isFirst = false;
                                    else
                                        buffer.Add( ", " );
                                    buffer.Add( content.RightItem ).Add( "x " ).StartColor( QuickColors.NewValue ).Add( content.LeftItem ).Add( "</color>" );
                                }
                            }
                            buffer.Add( ".  " );
                        }
                    }
                    #endregion

                    #region Factory Things
                    debugStage = 5900;
                    if ( relatedSquadOrNull.TypeData.HasFactoryFlows )
                    {
                        if ( relatedSquadOrNull.GetIsCrippled() )
                            buffer.Add( "受损工厂在修复前无法消耗金属。 " );
                        else if ( relatedSquadOrNull.GetIsNonFunctional() )
                            buffer.Add( "失效工厂在功能恢复前无法消耗金属。 " );
                        else if ( relatedSquadOrNull.ComputeDisabledReason( ArcenRejectionReason.Unknown ) != ArcenRejectionReason.Unknown )
                            buffer.Add( "已禁用的工厂在重新启用前无法消耗金属。 " );
                        else
                        {
                            debugStage = 5901;
                            bool foundAnyFleets = false;
                            bool wroteAboutAnyFleets = false;
                            bool hasSupportingFactoriesInRangeEverBeenSet = false;
                            foreach ( Fleet fleet in World_AIW2.Instance.Fleets( FleetStatus.AnyStatus ) )
                            {
                                debugStage = 5910;
                                if ( fleet == null )
                                    break;
                                if ( fleet.IsFleetConstructionPaused )
                                    continue; //are these turned off?
                                if ( fleet.IsFleetConstructionBlocked )
                                    continue;
                                if ( fleet.Faction == null || fleet.Faction.Type != FactionType.Player )
                                    continue;
                                //debugStage = 59102;
                                //if ( fleet.Category != FleetCategory.PlayerMobile && fleet.Category != FleetCategory.PlayerCustomMobile )
                                //    continue;
                                debugStage = 59103;
                                if ( fleet.SupportingFactoriesInRange.Count <= 0 )
                                    continue;
                                //no need to check for special type match, because supporting factories in range already does

                                debugStage = 59104;
                                bool foundMyself = false;
                                List<SafeSquadWrapper> supportingFactories = fleet.SupportingFactoriesInRange.GetDisplayList();
                                for ( int i = 0; i < supportingFactories.Count; i++ )
                                {
                                    try
                                    {
                                        debugStage = 59105;
                                        if ( supportingFactories[i].GetPrimaryKeyID() == relatedSquadOrNull.PrimaryKeyID )
                                        {
                                            foundMyself = true;
                                            break;
                                        }
                                    } catch ( Exception ) { } //cross-threading issues
                                }
                                hasSupportingFactoriesInRangeEverBeenSet = fleet.HasSupportingFactoriesInRangeEverBeenSet;
                                debugStage = 59106;
                                if ( !foundMyself )
                                    continue;

                                foundAnyFleets = true;
                                debugStage = 5911;
                                bool isFirst = true;
                                bool areAllAtShipCap = true;
                                foreach ( FleetMembership mem in fleet.MemberGroupsSorted_NonSim_ForUIOnly_SeriouslyDoNotCallFromBGCode() )
                                {
                                    debugStage = 5912;
                                    if ( mem.TypeData.IsDrone || mem.TypeData.SelfConstructs )
                                        continue;
                                    if ( mem.EffectiveSquadCap <= mem.EntitiesOfFMem.Count )
                                        continue; //skip us if we are at or above cap

                                    if ( mem.GetCanBuildAnother( true, -1, ExtraFromStacks.IncludePrecalc ) != ArcenRejectionReason.Unknown )
                                        continue; //something is blocking us other than cap!

                                    areAllAtShipCap = false;
                                    if ( isFirst )
                                    {
                                        wroteAboutAnyFleets = true;
                                        isFirst = false;
                                        Faction fleetFaction = fleet.Faction;
                                        //When display the fleet's name, colour code it so a player can see at a glance how "healthy" that fleet is through this interface
                                        bool isForPlayer = fleetFaction != null && (fleetFaction == World_AIW2.Instance.GetLocalPlayerFactionOrNull() || fleetFaction.Type == FactionType.NaturalObject);
                                        int currentStrength = fleet.GetCurrentStrengthOfFleet_ForUIOnly( isForPlayer );
                                        int maxStrength = fleet.GetMaxStrengthOfFleet_ForUIOnly( isForPlayer );
                                        int currentUnits = fleet.GetCurrentCountOfFleet_ForUIOnly();
                                        int maxUnits = fleet.GetMaxCountOfFleet_ForUIOnly( isForPlayer );
                                        FInt percent = (FInt.OneHundred * currentStrength * currentUnits) / (maxStrength * maxUnits);
                                        buffer.Add( "\nBuilding for fleet " ).AddPercentageInColor( fleet.GetName(), percent, true, false ).Add(":  " );
                                    } else
                                        buffer.Add( ", " );
                                    buffer.Add( mem.TypeData.DisplayName, "c0c0c0" );
                                    buffer.Add( " (x" ).Add( mem.GetRemainingCap( true, -1, ExtraFromStacks.IncludePrecalc ) ).Add( "  " ).StartColor( ColorMath.LightLeafGreen );
                                    buffer.Add( (((float) mem.MetalSpentConstructingCurrentReplacement / (float) mem.GetMetalCost()) * 100f).ToString( "00.0" ) );
                                    buffer.Add( "%" ).EndColor().Add( ")" );

                                    if ( mem.Fleet.Faction.Type == FactionType.Player )
                                    {
                                        if ( mem.TypeData.EnergyUsage > 0 && mem.Fleet.Faction.NetEnergy - mem.GetEnergyUsage() < 0 && mem.GetEnergyUsage() > 0 )
                                            buffer.StartColor( ColorMath.LightRed ).Add( " 能量短缺" ).EndColor();
                                    }
                                }

                                if ( isFirst )
                                {
                                    wroteAboutAnyFleets = true;
                                    if ( areAllAtShipCap )
                                        buffer.Add( "\nFinished all construction for fleet " ).StartColor( GetProportionalStrengthColor( 1 ) ).Add( fleet.GetName() ).EndColor().Add( ".  " );
                                    else
                                    {
                                        Faction fleetFaction = fleet.Faction;
                                        if ( fleetFaction != null && fleetFaction.NetEnergy <= 0 )
                                            buffer.Add( "\nUnable to build for fleet " ).StartColor( GetProportionalStrengthColor( 1 ) ).Add( fleet.GetName() ).EndColor()
                                            .Add( " because of insufficient Energy.  " );
                                        else
                                            buffer.Add( "\nUnable to build for fleet " ).StartColor( GetProportionalStrengthColor( 1 ) ).Add( fleet.GetName() ).EndColor().Add( " because some ship lines blocked.  Check energy availability.  " );
                                    }
                                }

                            }
                            if ( foundAnyFleets && !wroteAboutAnyFleets )
                                buffer.Add( "一个或多个相关舰队在此工厂范围内，但已达舰船上限，无需行动。  " );
                            else if ( !foundAnyFleets )
                            {
                                if ( !hasSupportingFactoriesInRangeEverBeenSet )
                                {
                                    buffer.Add( "你必须短暂取消暂停才能查看支持工厂的信息。  " );
                                } else
                                {
                                    if ( relatedEntityTypeData.SpecialFactoryType != null && relatedEntityTypeData.SpecialFactoryType.Length > 0 )
                                        buffer.Add( "没有（未受损的）" ).Add( relatedEntityTypeData.SpecialFactoryType ).Add( " 类型的我方舰队在此星球或邻近星球上，因此无法为任何人建造。  " );
                                    else
                                        buffer.Add( "没有（未受损的）我方机动舰队在此或邻近星球上，因此无法为任何人建造。  " );
                                }
                            }
                        }
                    }
                    #endregion

                    debugStage = 5950;
                    var constructionBlockedReason = relatedSquadOrNull.GetIsSelfConstructionBlocked();
                    if ( constructionBlockedReason != ArcenRejectionReason.Unknown )
                    {
                        debugStage = 5951;
                        if ( relatedMemFleetOrNull != null && relatedMemFleetOrNull.Category == FleetCategory.PlayerPlanetaryCommand )
                            buffer.Add( "<color=#ff5842>Cannot be constructed without fully-built command station here!</color>  " );
                        else
                            buffer.Add( "<color=#ff5842>Cannot be constructed without its flagship being here and non-crippled.</color>  " );

                        if ( GameSettings.Current.GetBoolBySetting( "Debug_ShowConstructionBlockedReason" ) )
                            buffer.Add( "调试：建造被阻止：" ).Add( Extensions.ToString(constructionBlockedReason) ).Add( "  " );
                    }
                    debugStage = 5952;
                } else //relatedEntity IS NULL
                {
                    #region Fleet Centerpiece Things For Not-Yet-Existing Fleets
                    debugStage = 6460;
                    if ( relatedEntityTypeData.FleetDesignTemplatesIAlwaysGrant != null && relatedEntityTypeData.FleetDesignTemplatesIAlwaysGrant.Count > 0 )
                    {
                        debugStage = 6461;
                        buffer.Add( "I am the centerpiece of what would become a fleet with the following items:  " );

                        debugStage = 6462;
                        for ( int j = 0; j < relatedEntityTypeData.FleetDesignTemplatesIAlwaysGrant.Count; j++ )
                        {
                            FleetDesignTemplate template = relatedEntityTypeData.FleetDesignTemplatesIAlwaysGrant[j];
                            foreach ( FleetItem Item in template.FleetItems() )
                            {
                                if ( Item.TypeData == relatedEntityTypeData )
                                    continue;
                                bool isExcludedSpecialType = false;
                                switch ( Item.TypeData.SpecialType )
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


                                buffer.Add( Item.TypeData == null ? "nulltype" : Item.TypeData.DisplayName ).Add( " " );
                                //Chris says: these are theoretical adds, so this is the base cap and that gets marked up.  This is okay.
                                int CapToShow = Item.TypeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( Item.TypeData, Item.Cap, Item.Cap, markStats.MarkLevel );

                                buffer.StartColor( QuickColors.NewValue );
                                buffer.Add( " x" ).Add( CapToShow );
                                buffer.EndColor();

                                buffer.Add( "  " );
                            }
                        }
                    }
                    #endregion
                }
                debugStage = 6470;
                if ( relatedEntityTypeData.CitySocketCost > 0 && panelMode == Window_InGameHoverEntityInfo.Mode.Build )
                {
                    buffer.Add( relatedEntityTypeData.NameForCitySockets_Plural ).Add( " Required: " ).Add( relatedEntityTypeData.CitySocketCost );
                    if (relatedMemFleetOrNull != null) {
                        buffer.Add( " (out of " ).Add( relatedMemFleetOrNull.CalculateRemainingCitySockets() ).Add( " available)");
                    }
                    debugStage = 6471;
                    buffer.Add(".  " );
                    if ( relatedEntityTypeData.MinimumRequiredCityLevelForConstruction > 1 )
                        buffer.Add( "必须达到标记等级 " ).Add( relatedEntityTypeData.MinimumRequiredCityLevelForConstruction, "a1ffa1" ).Add( " 才能建造此建筑。 " );

                }
                debugStage = 6472;
                //only tell about added points if they come from not-the-hub
                if ( relatedMarkLevelData.CitySockets > 0 )
                    buffer.Add( relatedEntityTypeData.NameForCitySockets_Plural ).Add( " Provided: " ).Add( relatedMarkLevelData.CitySockets ).Add( ".  " );

                debugStage = 6473;
                if ( relatedEntityTypeData != null &&
                     relatedEntityTypeData.ShipTypeNameToGrantMoreOfInCustomFleet != string.Empty )
                {
                    debugStage = 6474;
                    GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRowByName( relatedEntityTypeData.ShipTypeNameToGrantMoreOfInCustomFleet );
                    buffer.Add( "</color>This structure will grant you " ).Add( relatedEntityTypeData.ShipTypeCountToGrantMoreOfInCustomFleet, "ffa1a1" ).Add( " " ).Add( typeData.GetDisplayName(), "a1ffa1" );
                    if ( detailLevel >= TooltipDetail.Medium )
                    {
                        debugStage = 6475;
                        buffer.Add( ": " ).NewLine();
                        byte markLevelToUse = localPlayerFactionOrNull?.GetGlobalMarkLevelForShipLine( typeData ) ?? 1;
                        EntityText.GetTooltip( buffer, null, null,
                             typeData, -1, null, markLevelToUse, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None, PositionScaleMultiplier, IsBeingDrawnInPopupWindowRatherThanTooltip );
                    } else
                        buffer.Add( ". " );

                }
                debugStage = 6476;

                buffer.Add( FontSizes.MUCH_SMALLER_SIZE_STRING );

                if ( relatedSquadOrNull != null )
                {
                    debugStage = 5450;
                    if ( relatedSquadOrNull.ExtraStackedSquadsInThis > 0 && detailLevel >= TooltipDetail.Medium )
                    {
                        buffer.StartColor( QuickColors.HeaderDark ).Add( "这是一个堆叠，包含 " ).StartColor( QuickColors.NewValue )
                            .Add( relatedSquadOrNull.ExtraStackedSquadsInThis + 1 ).Add( " 艘舰船</color>。当前单位死亡后，计数减少并弹出下一单位。  " );
                        if ( detailLevel >= TooltipDetail.Full )
                        {
                            int extraCount = 1 + relatedSquadOrNull.ExtraStackedSquadsInThis;
                            buffer.Add( "此堆叠正常承受伤害，但发射 " ).StartColor( QuickColors.OldValue ).Add( extraCount ).Add( "倍</color> 的正常射击量。  " );
                        }
                        buffer.EndColor();

                    }
                }

                debugStage = 5500;

                if ( relatedSquadOrNull != null && detailLevel >= TooltipDetail.SuperShort )
                {
                    debugStage = 5501;
                    if ( relatedSquadOrNull.TypeData.IsDefault &&
                        relatedSquadOrNull.CalculateShouldOrderBeIgnoredByFlagshipAsIfWasStationaryMode( false ) && Engine_AIW2.Instance.CurrentGameViewMode != GameViewMode.GalaxyMapView )
                    {
                        debugStage = 5502;
                        buffer.StartColor( "ee3198" ).Add( "Stationary Flagship Mode!  " );
                        if ( detailLevel >= TooltipDetail.Medium )
                        {
                            debugStage = 5503;
                            buffer.Add( "按住 " ).Add( InputActionTypeDataTable.GetActionByName_FairlySlow( "HoldToGiveOrdersToStationaryFlagships" ).GetHumanReadableKeyCombo() )
                                .Add( " 使此旗舰听取命令。否则，舰队其余单位执行命令，旗舰持有命令，从旗舰出现的新舰船继承这些命令。  " );
                            if ( detailLevel >= TooltipDetail.Full )
                            {
                                buffer.Add( "你可以在舰队侧边栏页签中了解更多信息并更改其模式。找到此旗舰的舰队并点击它。  " );
                            }
                        }
                        buffer.EndColor();
                    }
                }
                debugStage = 5504;
                if ( relatedSquadOrNull != null && orders != null && relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player && detailLevel >= TooltipDetail.Medium )
                {
                    debugStage = 5505;
                    if ( relatedSquadOrNull.StopToShootAnySeenTargets )
                        buffer.StartColor( Window_InGameSelectionInfo.color_StopToShoot ).Add( "Stop-To-Shoot Mode!  " ).EndColor();
                    if ( relatedSquadOrNull.SpeedLimitFromGroupMove > 0 )
                        buffer.StartColor( Window_InGameSelectionInfo.color_GroupMove ).Add( "Group Move Mode!  Speed: " ).Add( relatedSquadOrNull.SpeedLimitFromGroupMove ).Add( "  " ).EndColor();
                    if ( relatedSquadOrNull.PreferredEntityTypeDataForTargeting != null )
                        buffer.StartColor( Window_InGameSelectionInfo.color_AttackMove ).Add( "Prefers To Target: " ).Add( relatedSquadOrNull.PreferredEntityTypeDataForTargeting.GetDisplayName() ).Add( "  " ).EndColor();
                }
                debugStage = 5510;
                if ( showDebugInfoInTooltip && relatedSquadOrNull != null && relatedSquadOrNull.DecollisionMoveTarget != ArcenPoint.ZeroZeroPoint && !isForMultipleUnits && detailLevel >= TooltipDetail.Medium )
                {
                    #region Write Decollision Move Logic
                    buffer.StartColor( QuickColors.HeaderDull ).Add( "Decollision Detour To: " );
                    if ( relatedSquadOrNull.DecollisionMoveTarget == ArcenPoint.OutOfRange )
                        buffer.Add( "BUG!  OutOfRange!" );
                    else
                    {
                        ArcenPoint targetPoint = relatedSquadOrNull.DecollisionMoveTarget;
                        targetPoint -= Engine_AIW2.Instance.CombatCenter;
                        buffer.Add( targetPoint.X ).Add( "," ).Add( targetPoint.Y );
                    }
                    buffer.EndColor();
                    buffer.Add( "  " );
                    #endregion
                }
                debugStage = 5520;

                if ( relatedSquadOrNull != null )
                {
                    int estimatedRemainingDurability = relatedSquadOrNull.EstimateRemainingDurabilityAfterAllShots( 0 );
                    int originalDurability = relatedSquadOrNull.GetAbsoluteDurabilityOfMyselfAndStack();
                    if ( estimatedRemainingDurability < originalDurability )
                    {
                        int damage = (originalDurability - estimatedRemainingDurability);
                        buffer.StartColor( QuickColors.OldValue ).Add( "\nIncoming Damage Expected: " ).AddNumberMoreReadable( damage ).EndColor().Add( "  " );
                        if ( estimatedRemainingDurability <= 0 )
                            buffer.StartColor( QuickColors.OldValue ).Add( "(Death Is Expected)" ).EndColor().Add( "  " );
                    } else
                    {
                        int incomingShotCount = relatedSquadOrNull.GetIncomingShotCount();
                        if ( incomingShotCount > 0 )
                            buffer.StartColor( QuickColors.OldValue ).Add( "\nIncoming Shots: " ).AddNumberMoreReadable( incomingShotCount ).Add( " (But Their Calculated Damage Is Zero??)  " ).EndColor();
                    }
                }

                //Now to any status effects
                //--------------------------------------

                debugStage = 6000;


                //we must be talking about a specific entity, now
                if ( relatedSquadOrNull != null && !isForMultipleUnits )
                {
                    /*
                    //bool haveDoneNewLine = false;
                    if ( relatedSquadOrNull.CurrentEngineStunSeconds > 0 )
                    {
                        //HandleNewline( buffer, ref haveDoneNewLine );

                        if ( detailLevel < TooltipDetail.Full )
                            buffer.StartColor( QuickColors.OldValue ).Add( "Engine-stunned for " ).Add( 
                            relatedSquadOrNull.CurrentEngineStunSeconds ).Add( " more seconds" ).EndColor().Add( ", speed reduced to " )
                            .StartColor( QuickColors.OldValue ).AddNumberMoreReadable( relatedSquadOrNull.CalculatedSpeed ).EndColor().Add( " for now.  " );
                        else
                            buffer.StartColor( QuickColors.OldValue ).Add( "This ship is engine-stunned for " ).Add(
                            relatedSquadOrNull.CurrentEngineStunSeconds ).Add( " more seconds" ).EndColor().Add( ", thus having its speed reduced to " )
                            .StartColor( QuickColors.OldValue ).AddNumberMoreReadable( relatedSquadOrNull.CalculatedSpeed ).EndColor().Add( " for now.  " );
                    }
                    debugStage = 6010;

                    FInt grav = relatedSquadOrNull.CurrentGravitySpeedMultiplier.Display;
                    if ( grav > FInt.Zero && grav != FInt.One )
                    {
                        //HandleNewline( buffer, ref haveDoneNewLine );

                        if ( detailLevel < TooltipDetail.Full )
                            buffer.StartColor( QuickColors.OldValue ).Add( "In gravity field(s), speed reduced to " ).Add(
                            grav.ReadableString ).Add( "x normal" ).EndColor().Add( ", which is " )
                            .StartColor( QuickColors.OldValue ).AddNumberMoreReadable( relatedSquadOrNull.CalculatedSpeed ).EndColor().Add( " for now.  " );
                        else
                            buffer.StartColor( QuickColors.OldValue ).Add( "This ship is in one or more gravity fields, which combine to reduce its speed to " ).Add(
                            grav.ReadableString ).Add( "x normal" ).EndColor().Add( ", thus having its total speed reduced to " )
                            .StartColor( QuickColors.OldValue ).AddNumberMoreReadable( relatedSquadOrNull.CalculatedSpeed ).EndColor().Add( " for now.  " );
                    }*/

                    debugStage = 6015;

                    if ( relatedSquadOrNull.IsBlackHoledAtMoment.Display )
                    {
                        if ( detailLevel < TooltipDetail.Full )
                            buffer.Add( " Cannot leave planet due to Black Hole Machine. " );
                        else
                            buffer.Add( " This ship is affected by a Black Hole Machine and cannot leave the planet. You must destroy the Black Hole Machine to escape. " );
                    }

                    debugStage = 6020;

                    /*
                    if ( relatedSquadOrNull.CurrentParalysisSeconds > 0 )
                    {
                        //HandleNewline( buffer, ref haveDoneNewLine );
                        if ( detailLevel < TooltipDetail.Full )
                            buffer.StartColor( QuickColors.OldValue ).Add( "Paralyzed for " ).Add(
                            relatedSquadOrNull.CurrentParalysisSeconds ).Add( " more seconds" ).Add( ".  " ).EndColor();
                        else
                            buffer.StartColor( QuickColors.OldValue ).Add( "This ship is completely paralyzed for " ).Add(
                            relatedSquadOrNull.CurrentParalysisSeconds ).Add( " more seconds" ).Add( ", making it completely useless for now.  " ).EndColor();
                    }*/

                    debugStage = 6022;

                    if ( relatedSquadOrNull.IsBlackHoledAtMoment.Display )
                    {
                        buffer.StartColor( QuickColors.OldValue ).Add( "Black Holed: Cannot Leave This Planet" ).EndColor();
                    }
                    debugStage = 6030;

                    /*
                    if ( relatedSquadOrNull.CurrentCountOfTractorsPullingOnThis > 0 )
                    {
                        //HandleNewline( buffer, ref haveDoneNewLine );
                        if ( detailLevel < TooltipDetail.Full )
                            buffer.StartColor( QuickColors.OldValue ).Add( "Caught in tractor field and unable to move.  " ).EndColor();
                        else
                        {
                            buffer.StartColor( QuickColors.OldValue ).Add( "This ship is currently caught in a tractor field that is preventing it from moving.  " );

                            buffer.EndColor();
                        }
                    }

                    debugStage = 6040;

                    if ( relatedSquadOrNull.CurrentWeaponAddedReloadSeconds > 0 )
                    {
                        //HandleNewline( buffer, ref haveDoneNewLine );

                        buffer.StartColor( QuickColors.OldValue ).Add( "All weapons on this ship currently take an extra " ).Add(
                            relatedSquadOrNull.CurrentWeaponAddedReloadSeconds ).Add( " seconds to reload per salvo.  " ).EndColor();
                    }

                    debugStage = 6045;

                    if ( (relatedSquadOrNull.ProtectingShields_ReduceDamage.Count > 0 || relatedSquadOrNull.GetIsSelfEmittingProtectingShield_ReduceDamage()) &&
                        !hasSystemWithBonusFromAttackingUnderForcefields)
                    {
                        //HandleNewline( buffer, ref haveDoneNewLine );

                        if ( detailLevel >= TooltipDetail.Medium )
                            buffer.StartColor( QuickColors.OldValue ).Add( "Deals half normal damage due to firing from under forcefield.  " ).EndColor();
                    }
                    else
                    {
                        if ( relatedSquadOrNull.GetIsProtectedByAnyForcefield() || 
                            relatedSquadOrNull.GetIsSelfEmittingProtectingShieldNoDamageReduction() )
                        {
                            if ( detailLevel >= TooltipDetail.Medium )
                                buffer.StartColor( QuickColors.NewValue ).Add( "Under forcefield.  " ).EndColor();
                        }
                    }*/

                    if ( relatedSquadOrNull.AreaBoosting_CurrentCount.Display > 0 )
                        buffer.StartColor( QuickColors.NewValue ).Add( "Boosting/protecting " ).Add( relatedSquadOrNull.AreaBoosting_CurrentCount.Display ).Add( " units.  " ).EndColor();

                    if ( relatedSquadOrNull.AreaBoosters_ShotEvaluated.Count > 0 /*|| relatedSquadOrNull.AreaBoosters_NotShotEvaluated.Count > 0*/ )
                    {
                        buffer.StartColor( QuickColors.NewValue ).Add( "Boosted/protected by: " );
                        List<GameEntity_Squad> protectors = relatedSquadOrNull.AreaBoosters_ShotEvaluated.GetDisplayList();
                        bool isFirst = true;
                        foreach ( GameEntity_Squad squad in protectors )
                        {
                            if ( isFirst )
                                isFirst = false;
                            else
                                buffer.Add( ", " );
                            buffer.Add( squad.TypeData.DisplayName );
                        }
                        // jcf: unused, but could be
                        /*
                        protectors = relatedSquadOrNull.AreaBoosters_NotShotEvaluated.GetDisplayList();
                        foreach ( GameEntity_Squad squad in protectors )
                        {
                            if ( isFirst )
                                isFirst = false;
                            else
                                buffer.Add( ", " );
                            buffer.Add( squad.TypeData.DisplayName );
                        }
                        */
                        buffer.Add( "  " ).EndColor();
                    }

                    debugStage = 6046;
                    if ( GameSettings.Current.GetBoolBySetting( "Debug_ShowShipCoordinates" ) && relatedSquadOrNull != null )
                    {
                        buffer.Add( "Current ship coordinates: " ).Add( relatedSquadOrNull.WorldLocation ).Add( "." );
                        if ( relatedSquadOrNull.GuardOrPatrolOffsetPoints.Count == 0 )
                            buffer.Add( " No Guarding Offsets." );
                        else
                            buffer.Add( " The first of " ).Add( relatedSquadOrNull.GuardOrPatrolOffsetPoints.Count ).Add( " offsets is " ).Add( relatedSquadOrNull.GuardOrPatrolOffsetPoints[0] );
                    }
                    debugStage = 6050;

                    if ( detailLevel >= TooltipDetail.Full && relatedSquadPlanetFactionOrNull != null && relatedSquadPlanetFactionOrNull.GetIsLocalFaction() && !relatedSquadOrNull.TypeData.IsFleetLeader &&
                        relatedSquadOrNull.TypeData.FleetMembershipStyle != FleetMembershipStyle.Planetary )
                    {
                        CannotTransportReason noTransportBase = relatedSquadOrNull.TypeData.GetCanBeTransported();
                        if ( noTransportBase != CannotTransportReason.TranportingIsFine )
                        {
                            //Don't bother telling me about that, actually.  buffer.StartColor( QuickColors.HeaderDull ).Add( "该舰船永远无法被运输。  " ).EndColor();
                        } else
                        {
                            CannotTransportReason noTransport = relatedSquadOrNull.GetCanBeTransportedRightNow();
                            if ( noTransport != CannotTransportReason.TranportingIsFine )
                            {
                                //HandleNewline( buffer, ref haveDoneNewLine );

                                buffer.StartColor( QuickColors.OldValue ).Add( "This ship normally can be transported, but cannot be loaded into any transport right now because " ).Add(
                                    EnumNameCache.GetName( noTransport ) ).Add( ".  " ).EndColor();
                            }
                        }
                    }

                    debugStage = 8000;

                    ArcenRejectionReason rejectionReason = relatedSquadOrNull.ComputeDisabledReason( ArcenRejectionReason.Unknown );
                    bool alreadyWroteCrippledInfo = false;
                    if ( rejectionReason != ArcenRejectionReason.Unknown )
                    {
                        bool skipBecauseWrittenElsewhere = false;
                        switch ( rejectionReason )
                        {
                            case ArcenRejectionReason.EntityIsParalyzed:
                            case ArcenRejectionReason.EntityIsUnrebuiltRemains:
                                skipBecauseWrittenElsewhere = true;
                                break;
                        }
                        if ( !skipBecauseWrittenElsewhere )
                        {
                            //HandleNewline( buffer, ref haveDoneNewLine );
                            buffer.StartColor( QuickColors.OldValue ).Add( "Ship Disabled: " );

                            switch ( rejectionReason )
                            {
                                case ArcenRejectionReason.CrippledInseadOfDead:
                                    WriteCrippledInfo( buffer, relatedSquadOrNull );
                                    alreadyWroteCrippledInfo = true;
                                    break;
                                case ArcenRejectionReason.NonFunctionalWhenNotOnPlanetOwnedByMyFaction:
                                    buffer.Add( "失效 - 所属阵营必须控制此星球！" );
                                    break;
                                case ArcenRejectionReason.InUnexploredSpace:
                                    buffer.Add( "在未探索空间 - 没有侦察兵到过此地则无法运作！" );
                                    break;
                                //case ArcenRejectionReason.EntityConsideredOutOfSupplyOfParentFleetCenterpiece:
                                //    buffer.Add( "无补给——未在舰队核心船的同一或相邻母星上。" );
                                //    break;
                                case ArcenRejectionReason.EntityHasNotYetBeenFullyClaimed:
                                    if ( relatedSquadOrNull.IsInHoldFireMode )
                                        buffer.Add( "尚未完全占领，且已暂停，因此不会被占领。" );
                                    else
                                        buffer.Add( "尚未完全占领。" );
                                    break;
                                case ArcenRejectionReason.EntityIsInHoldFireMode:
                                    if ( relatedEntityTypeData.IsCombatant )
                                        buffer.Add( "停火模式。" );
                                    else
                                        buffer.Add( "功能暂停模式。" );
                                    break;
                                case ArcenRejectionReason.EntityIsSelfBuilding:
                                    {
                                        float percent = (1f - ((float) relatedSquadOrNull.SelfBuildingMetalRemaining / (float) relatedSquadOrNull.GetMetalCost())) * 100;
                                        buffer.Add( "仍在建造中（" );
                                        buffer.AddFixedDecimalThousands( percent, 1 );
                                        //buffer.Add( " " ).AddFixedDecimalThousands( relatedSquadOrNull.SelfBuildingMetalRemaining.ToFloatNonSim(), 1 ).Add( " metal left" );
                                        buffer.Add( "%)" );
                                        if ( relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player )
                                        {
                                            Faction facOrNull = relatedSquadOrNull.GetFactionOrNull_Safe();
                                            if ( relatedSquadOrNull.TypeData.EnergyUsage > 0 && (facOrNull != null && facOrNull.NetEnergy < 0) &&
                                                 relatedSquadOrNull.GetEnergyUsage() > 0 )
                                                buffer.StartColor( ColorMath.LightRed ).Add( " 能量短缺" ).EndColor();
                                        }
                                        buffer.Add( "." );
                                    }
                                    break;
                                //case ArcenRejectionReason.EntityIsUnrebuiltRemains:
                                //    buffer.Add( "已被摧毁，尚未重建。" );
                                //    break;
                                case ArcenRejectionReason.FactionDoesNotControlThisPlanet:
                                    buffer.Add( "阵营未控制此星球。" );
                                    break;
                                case ArcenRejectionReason.FactionDoesNotHaveEnoughEnergy:
                                    buffer.Add( "阵营能量不足。" );
                                    break;
                                case ArcenRejectionReason.NotEnoughCitySockets:
                                    buffer.Add( "不足 " ).Add( relatedSquadOrNull.TypeData.NameForCitySockets_Plural ).Add( "，在此星球。" );
                                    break;
                                case ArcenRejectionReason.MetalIsZero:
                                    buffer.Add( "存储金属为零。" );
                                    break;
                                default:
                                    buffer.Add( "ERROR_WRITE_NICE_TEXT: " ).Add( EnumNameCache.GetName( rejectionReason ) );
                                    break;
                            }

                            buffer.EndColor();
                            buffer.Add( "  " );
                        } else
                        {
                        }
                    }

                    if ( !alreadyWroteCrippledInfo && relatedSquadOrNull.GetIsCrippled() )
                        WriteCrippledInfo( buffer, relatedSquadOrNull );

                    debugStage = 8050;

                    if ( relatedSquadOrNull != null && relatedSquadOrNull.SecondsSpentAsRemains >= 0 )
                    {
                        rejectionReason = relatedSquadOrNull.GetCannotRebuildRemainsReason();
                        if ( rejectionReason != ArcenRejectionReason.Unknown )
                        {//HandleNewline( buffer, ref haveDoneNewLine );
                            buffer.StartColor( QuickColors.OldValue ).Add( "无法重建残骸：" );

                            switch ( rejectionReason )
                            {
                                case ArcenRejectionReason.CannotRebuild_IsNotremains:
                                    buffer.Add( "不是残骸！" );
                                    break;
                                case ArcenRejectionReason.CannotRebuild_YesEnemiesHereAndNotBeenLongEnough:
                                    buffer.Add( "必须等待 " )
                                        .AddHoursAndMinutes( ExternalConstants.Instance.Balance_SecondsAfterDeathBeforeRebuilding - relatedSquadOrNull.SecondsSpentAsRemains )
                                        .Add( " while enemies are here." );
                                    break;
                                case ArcenRejectionReason.CannotRebuild_NoEnemiesHereAndNotBeenLongEnough:
                                    buffer.Add( "只需再等待 " )
                                        .AddHoursAndMinutes( ExternalConstants.Instance.Balance_SecondsAfterDeathBeforeRebuildingNoEnemies - relatedSquadOrNull.SecondsSpentAsRemains )
                                        .Add( " since no enemies present." );
                                    break;
                                case ArcenRejectionReason.CannotRebuild_CommandStationOnAIPlanet:
                                    buffer.Add( "指挥站不能在敌方星球重建。" );
                                    break;
                                case ArcenRejectionReason.CannotRebuild_WouldPutUsIntoBrownout:
                                    buffer.Add( "重建会导致能量为负。" );
                                    break;
                                default:
                                    buffer.Add( "ERROR_WRITE_NICE_TEXT: " ).Add( EnumNameCache.GetName( rejectionReason ) );
                                    break;
                            }
                            buffer.EndColor();
                            buffer.Add( "  " );
                        }
                    }

                    debugStage = 8100;

                    {
                        Faction facOrNull = relatedSquadOrNull.GetFactionOrNull_Safe();
                        if ( relatedSquadOrNull.GetHasBubbleForcefieldRightNow() && relatedSquadOrNull.GetFactionTypeSafe() == FactionType.Player &&
                            facOrNull != null && facOrNull.SecondsSinceBrownout >= 0 )
                        {
                            buffer.StartColor( QuickColors.OldValue );
                            if ( detailLevel < TooltipDetail.Full )
                                buffer.Add( "电力不足：气泡力场将在 " ).Add(
                                            ExternalConstants.Instance.SecondsToWaitBeforeBrownoutEnds - facOrNull.SecondsSinceBrownout ).Add( "秒后关闭。  " );
                            else
                                buffer.Add( "电力不足：能量平衡为负！你的气泡力场将在 " ).Add(
                                        ExternalConstants.Instance.SecondsToWaitBeforeBrownoutEnds - facOrNull.SecondsSinceBrownout ).Add( "秒内无法展开。  " );
                            buffer.EndColor();
                        }
                    }

                    debugStage = 8200;

                    int secondsUntilClaim = localPlayerPlanetFactionOrNull == null ? 0 : relatedSquadOrNull.GetRemainingSecondsBeforeCanClaim( localPlayerPlanetFactionOrNull );
                    if ( secondsUntilClaim > 0 )
                    {
                        buffer.StartColor( QuickColors.OldValue )
                            .Add( "无法被占领，还需要 " ).AddHoursAndMinutes( secondsUntilClaim ).Add( "。这取决于敌方存在以及上次被摧毁后经过的时间。  " );
                        buffer.EndColor();
                    }

                    debugStage = 8300;

                    if ( relatedSquadOrNull.HasNotYetBeenFullyClaimed && localPlayerFactionOrNull != null &&
                         relatedSquadOrNull.TypeData.EnergyUsage > 0 && localPlayerFactionOrNull.NetEnergy - relatedSquadOrNull.TypeData.EnergyUsage < 0 )
                    {
                        buffer.StartColor( QuickColors.OldValue )
                            .Add( "无法认领，因为你需要 " ).Add( -(localPlayerFactionOrNull.NetEnergy - relatedSquadOrNull.TypeData.EnergyUsage) ).Add( " 更多能量来运行此实体。  " );
                        buffer.EndColor();
                    }

                    debugStage = 8400;

                    if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.MainGameView && GameSettings.Current.GetBoolBySetting( "Debug_ShowLODInfoInShipTooltips" ) )
                    {
                        Planet planet = relatedSquadOrNull.Planet;
                        if ( relatedSquadOrNull.InstancedRenderer == null )
                        {
                            buffer.StartColor( QuickColors.OldValue )
                            .Add( "No InstancedRenderer on this ship.  " );
                            buffer.EndColor();
                        } else if ( planet == null )
                        {
                            buffer.StartColor( QuickColors.OldValue )
                              .Add( "No planet related to this ship?  " );
                            buffer.EndColor();
                        } else
                        {
                            float distanceFromCamera;
                            int currentLOD = relatedSquadOrNull.InstancedRenderer.GetCurrentLODAndLastDistanceFromCamera( out distanceFromCamera );

                            float lodDivisor = planet.GravWellSize.GeneralMultiplier;
                            bool hasLODDivisor = false;
                            if ( Math.Round( lodDivisor, 1 ) != 1f )
                            {
                                hasLODDivisor = true;
                                buffer.Add( "\n<pos=40><color=#c79462>Current Visual Scale Is Offset:</color> " ).Add( 1 / lodDivisor ).Add( "x<pos=350><color=#c79462>Do NOT set LOD values in xml from this planet!</color>" );
                            } else
                                lodDivisor = 1f;

                            buffer.Add( "\n<pos=40><color=#99aa99>Current LOD:</color> " ).Add( currentLOD ).Add( "<pos=350><color=#99aa99>Distance From Camera:</color> " ).AddFixedDecimalThousands( distanceFromCamera, 1 );
                            if ( relatedEntityTypeData.LODDistancesFromVis == null )
                            {
                                buffer.Add( "\n<pos=40>LODDistancesFromVis Not Set For Some Reason!" );
                            } else if ( relatedEntityTypeData.LODMeshesFromVis == null )
                            {
                                buffer.Add( "\n<pos=40>LODMeshesFromVis Not Set For Some Reason!" );
                            } else
                            {
                                if ( relatedEntityTypeData.LODDistancesFromVis.Length != relatedEntityTypeData.LODMeshesFromVis.Length )
                                    buffer.Add( "\n<pos=40>LODDistancesFromVis.Length != LODMeshesFromVis.Length!" );

                                for ( int i = 0; i < relatedEntityTypeData.LODDistancesFromVis.Length; i++ )
                                {
                                    buffer.Add( "\n<pos=40><color=#aaaaaa>LOD" ).Add( i ).Add( " Lasts Until Dist:</color> " );
                                    if ( i < relatedEntityTypeData.LODDistanceOverrides.Count ) //use the override if present
                                    {
                                        buffer.AddNumberMoreReadable( ((relatedEntityTypeData.LODDistanceOverrides[i] * relatedEntityTypeData.LODDistanceMultiplier) / lodDivisor) );
                                        buffer.Add( "   <color=#aaaaaa>(Overriding: " ).AddNumberMoreReadable( (relatedEntityTypeData.LODDistancesFromVis[i] / lodDivisor) ).Add( ")</color>" );
                                        if ( hasLODDivisor )
                                        {
                                            buffer.Add( "   <color=#c79462>(Orig: " )
                                                .AddNumberMoreReadable( (relatedEntityTypeData.LODDistanceOverrides[i] * relatedEntityTypeData.LODDistanceMultiplier) )
                                                .Add( " / " )
                                                .AddNumberMoreReadable( relatedEntityTypeData.LODDistancesFromVis[i] )
                                                .Add( ")</color>" );
                                        }
                                    } else
                                    {
                                        buffer.AddNumberMoreReadable( ((relatedEntityTypeData.LODDistancesFromVis[i] * relatedEntityTypeData.LODDistanceMultiplier) / lodDivisor) );
                                        if ( hasLODDivisor )
                                        {
                                            buffer.Add( "   <color=#c79462>(Orig: " )
                                                .AddNumberMoreReadable( (relatedEntityTypeData.LODDistancesFromVis[i] * relatedEntityTypeData.LODDistanceMultiplier) )
                                                .Add( ")</color>" );
                                        }
                                    }
                                    buffer.Add( "<pos=350><color=#aaaaaa>Mesh Vertex Count:</color> " ).AddNumberMoreReadable( relatedEntityTypeData.LODMeshesFromVis[i].vertexCount );
                                }
                            }
                        }
                    }
                } //end if ( relatedEntity != null )


                buffer.Add( "</size>" );
                buffer.Add( "</size>" );

                //truly the last thing before network stuff
                debugStage = 9005;

                buffer.Add( "<size=50%>" );
                if ( detailLevel < TooltipDetail.Full )
                    buffer.AddDlcMod(relatedEntityTypeData);
                else
                    buffer.AddDlcMod(relatedEntityTypeData, "This unit was added by" );

                buffer.Add( "</size>" );

                debugStage = 9010;

                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client && relatedSquadOrNull != null )
                {
                    if ( relatedSquadOrNull.Network_ClientOnly_SimCyclesSinceLastHostUpdate > 50 )
                    {
                        buffer.Add( "\n<pos=40><color=#ff6767>It has been </color> " ).AddFixedDecimalThousands( (relatedSquadOrNull.Network_ClientOnly_SimCyclesSinceLastHostUpdate / 10f), 1 )
                            .Add( " seconds since this unit has been checked by the host!</color>  " );
                    } else
                    {
                        if ( detailLevel == TooltipDetail.Full )
                            buffer.Add( "\n<pos=40><color=#aaaaaa></color> " ).AddFixedDecimalThousands( (relatedSquadOrNull.Network_ClientOnly_SimCyclesSinceLastHostUpdate / 10f), 1 )
                                .Add( "s since unit was checked by host.</color>  " );
                    }
                }

                debugStage = 9020;
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLog( "Exception in prototype entity tooltip text generation at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                buffer.Add( "Tooltip生成异常！" );
                return false;
            }
            return true;
        }

        #region WriteSystemInfo
        public static void WriteSystemInfo( ArcenCharacterBufferBase buffer, int systemIndex, GameEntity_Squad relatedSquadOrNull, FleetMembership relatedMembershipOrNull, Faction facOrNull, Planet relatedPlanet, GameEntityTypeData relatedEntityTypeData,
            byte effectiveMarkLevel, bool OnlyWriteIfWeapon, bool OnlyWriteIfModuleStatBooster, TooltipDetail detailLevel, bool isForMultipleUnits, int WeaponActivityDetails, bool ShowAbortCode, bool ShowTargetInfo, int MaxDisplayedTargets, bool useIcons, bool useText )
        {
            bool unused = false;
            WriteSystemInfo( buffer, systemIndex, relatedSquadOrNull, relatedMembershipOrNull, facOrNull, relatedPlanet, relatedEntityTypeData.SystemTypes[systemIndex],
                effectiveMarkLevel, OnlyWriteIfWeapon, OnlyWriteIfModuleStatBooster, detailLevel, isForMultipleUnits, WeaponActivityDetails, ShowAbortCode, ShowTargetInfo, MaxDisplayedTargets, ref unused, useIcons, useText );
            if ( unused ) { }
        }

        public static void WriteSystemInfo( ArcenCharacterBufferBase buffer, int systemIndex, GameEntity_Squad relatedSquadOrNull, FleetMembership relatedMembershipOrNull, Faction facOrNull, Planet relatedPlanet, EntitySystemTypeData systemData,
            byte effectiveMarkLevel, bool OnlyWriteIfWeapon, bool OnlyWriteIfModuleStatBooster, TooltipDetail detailLevel, bool isForMultipleUnits, int WeaponActivityDetails, bool ShowAbortCode, bool ShowTargetInfo, int MaxDisplayedTargets, ref bool hasSystemWithBonusFromAttackingUnderForcefields,
            bool useIcons, bool useText )
        {
            var detail = EntityText.Detail;
            bool useFullNames = detail == TooltipDetail.Full;
            bool useIconsAndNames = detail == TooltipDetail.Full && GameSettings.Current.GetBoolBySetting( "Tooltip_ShipNames_Full" );

            Color damageColor = ColorMath.LightRed;
            int debugStage = 0;
            try
            {
                EntitySystemTypeData.MarkLevelStats systemStats = systemData.ForMark[effectiveMarkLevel];
                EntitySystem actualEntitySystemOrNull = null;
                if ( relatedSquadOrNull != null )
                    actualEntitySystemOrNull = relatedSquadOrNull.Systems[systemIndex];

                string display_name = systemData.DisplayName;
                if (systemData.CustomType != null)
                    display_name = systemData.CustomType.DisplayName;
                
                int display_shotdamage = systemStats.DamagePerShot;
                if (actualEntitySystemOrNull != null)
                    display_shotdamage = actualEntitySystemOrNull.GetShotDamage();
                
                int display_shotcorrosiondamage = systemStats.CorrosionDamage;
                if (systemData.AllDamageIsCorrosive)
                    display_shotcorrosiondamage += display_shotdamage;
                if (actualEntitySystemOrNull != null)
                    display_shotcorrosiondamage = actualEntitySystemOrNull.GetShotDamage_OfCorrosion();
                
                if (systemData.AllDamageIsCorrosive)
                    display_shotdamage = 0;

                int display_salvodps = systemStats.DamagePerSecond.GetNearestIntPreferringHigher();
                if (actualEntitySystemOrNull != null)
                    display_salvodps = actualEntitySystemOrNull.GetSalvoDps_Max().GetNearestIntPreferringHigher();

                #region If A Weapon
                if ( systemData.Category == EntitySystemCategory.Weapon )
                {
                    if ( OnlyWriteIfModuleStatBooster )
                    {
                        return;
                    }
                    
                    debugStage = 183;
                    bool isRetaliatory = systemData.FiringTiming == FiringTiming.WhenParentEntityHit;
                    bool isMirrorShot = isRetaliatory && systemData.ReturnsThisPercentageOfDamageWhenFiringRetaliatoryShot > FInt.Zero;
                    bool isOnDeath = systemData.OnlyFiresOnDeath;

                    buffer.Add( "\n" ).Add( "<b>" ).Add( display_name ).Add( "</b> " );
                    debugStage = 185;
                    
                    int effectiveMaxTargets = 1;
                    if ( systemData.IsCoilbeam )
                        effectiveMaxTargets *= 2;
                    else if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                        effectiveMaxTargets *= systemStats.AOEMaximumNumberOfTargetsHitPerShot;
                    
                    if ( systemData.NumberBeamsToFire > 0 )
                        effectiveMaxTargets *= systemData.NumberBeamsToFire;

                    var basePreModifierDPS = display_salvodps;//systemStats.CalculateShipOrFleetMemDamagePerSecond( relatedSquadOrNull, relatedMembershipOrNull );
                    var maxPreModifierDPS = basePreModifierDPS * effectiveMaxTargets;//decimals are just confusing here

                    if ( systemData.ShotTypeData != null && systemData.ShotTypeData.Category == GameEntityCategory.Ship )
                    {
                        GameEntityTypeData spawnType = systemData.ShotTypeData;
                        byte subMark = effectiveMarkLevel;
                        if ( subMark > spawnType.MaxMarkLevel )
                            subMark = spawnType.MaxMarkLevel;
                        if ( subMark < spawnType.BaseMark.MarkLevel.Ordinal )
                            subMark = spawnType.BaseMark.MarkLevel.Ordinal;
                        Faction toDisplayFor = facOrNull;
                        if ( toDisplayFor == null )
                            toDisplayFor = spawnType.EncyclopediaOnly_LastFactionForColor.Display;
                        
                        buffer.AddShipIconInline( spawnType, toDisplayFor, null );
                        if (useIconsAndNames)
                            buffer.Add( spawnType.GetDisplayName() );

                    }
                    else 
                    if ( systemData.CanDevour )
                    {
                        buffer.WrapDamage( systemData.DevourFunctionName, false, false );
                    }
                    else 
                    if ( systemData.IonDamageToAlbedoLessThan > FInt.Zero )
                    {
                        buffer.StartExoticDamageWrapper( useIcons ).Add( systemData.IonPercentagePerMarkLevelLower * FInt.OneHundred ).Add( "%" );
                        if ( useText )
                            buffer.Add( "离子异能伤害" );
                        buffer.EndColor();
                    } 
                    else 
                    if ( isMirrorShot )
                    {
                        buffer.StartExoticDamageWrapper( useIcons ).Add( systemData.ReturnsThisPercentageOfDamageWhenFiringRetaliatoryShot * FInt.OneHundred ).Add( "%" );
                        if ( useText )
                            buffer.Add( " 异能伤害" );
                        buffer.EndColor();
                    } 
                    else 
                    if ( systemData.OverdrivesShields )
                    {
                        buffer.WrapDamage( "100%", useIcons, useText ).Add( " 目标护盾" );
                    } 
                    else
                    {
                        if ( isRetaliatory )
                            buffer.Add( "反击 " );
                        bool wrotePotentialIcon = false;
                        if ( display_shotdamage > 0 )
                        {
                            if ( isRetaliatory )
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                    buffer.WrapExoticDamageTruncated( display_shotdamage, useIcons, useText );
                                else
                                    buffer.WrapExoticDamageMoreReadable( display_shotdamage, useIcons, useText );
                            } else
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                    buffer.WrapDamageTruncated( display_shotdamage, useIcons, useText );
                                else
                                    buffer.WrapDamageMoreReadable( display_shotdamage, useIcons, useText );
                            }
                            wrotePotentialIcon = true;
                        }
                        if ( display_shotcorrosiondamage > 0 )
                        {
                            if ( display_shotdamage > 0 )
                                if ( detailLevel < TooltipDetail.Full )
                                    buffer.Add( " + " );
                                else
                                    buffer.Add( " and " );
                            if ( isRetaliatory )
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                    buffer.WrapExoticDamageTruncated( display_shotcorrosiondamage, useIcons && !wrotePotentialIcon, false );
                                else
                                    buffer.WrapDamageMoreReadable( display_shotcorrosiondamage, useIcons && !wrotePotentialIcon, false );
                            } else
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                    buffer.WrapCorrosiveDamageTruncated( display_shotcorrosiondamage, useIcons && !wrotePotentialIcon, false );
                                else
                                    buffer.WrapCorrosiveDamageMoreReadable( display_shotcorrosiondamage, useIcons && !wrotePotentialIcon, false );
                            }
                            if ( useText )
                                buffer.Add( " 腐蚀伤害" );
                        }
                    }
                    debugStage = 200;
                    if ( isOnDeath )
                    {
                        buffer.Add(" on ").WrapReload( "Death", false, false );
                    }
                    else if ( isRetaliatory )
                    {
                        buffer.Add( " on " ).WrapReload( "Hit", false, false );
                    }
                    else
                    {
                        //if ( detailLevel >= TooltipDetail.Medium )
                        {
                            int secondsPerSalvoRightNow = systemStats.SecondsPerSalvo;
                            int forAnotherXSalvos = -1;
                            if ( actualEntitySystemOrNull != null )
                            {
                                if ( actualEntitySystemOrNull.IsInAltSalvoFiringMode )
                                    secondsPerSalvoRightNow = systemStats.AltSecondsPerSalvo;
                                if ( systemStats.AltSecondsPerSalvo > 0 )
                                {
                                    if ( actualEntitySystemOrNull.IsInAltSalvoFiringMode )
                                        forAnotherXSalvos = systemData.UseAlternateRateOfFireForXShotsBeforeReverting - actualEntitySystemOrNull.ShotsSinceLastSwitchingSalvoFiringMode;
                                    else
                                        forAnotherXSalvos = systemData.UseAlternateRateOfFireAfterXShots - actualEntitySystemOrNull.ShotsSinceLastSwitchingSalvoFiringMode;
                                }
                            }

                            if ( systemStats.ShotsPerSalvo > 1 )
                                buffer.StartMultishotWrapper( false ).Add("  x").Add( systemStats.ShotsPerSalvo ).Add( "</color> " );

                            buffer.Add( "<size=80%> / </size>" ).StartReloadWrapper( false ).Add( secondsPerSalvoRightNow ).Add( "s</color>" );
                            if ( forAnotherXSalvos > 0 && (detailLevel >= TooltipDetail.Full || isForMultipleUnits) )
                                buffer.Add( " for another " ).WrapReload( forAnotherXSalvos, false, false ).Add( " salvos" );
                        }
                        
                        if ( systemStats.AltSecondsPerSalvo > 0 )
                        {
                            buffer.Add( "爆发射击" );
                            if ( detailLevel >= TooltipDetail.Medium )
                            {
                                buffer.Add( ": " );
                                if ( detailLevel >= TooltipDetail.Full )
                                    buffer.WrapReload( systemData.UseAlternateRateOfFireAfterXShots, false, false ).Add( " salvos fired at " )
                                        .WrapReload( systemStats.SecondsPerSalvo, false, false ).Add( "秒/轮后切换至" )
                                        .WrapReload( systemData.UseAlternateRateOfFireForXShotsBeforeReverting, false, false ).Add( " salvos fired at " )
                                        .WrapReload( systemStats.AltSecondsPerSalvo, false, false ).Add( "s per salvo (and then switching back)" );
                                else
                                    buffer.WrapReload( systemData.UseAlternateRateOfFireAfterXShots, false, false ).Add( " at " )
                                        .WrapReload( systemStats.SecondsPerSalvo, false, false ).Add( "秒后 " )
                                        .WrapReload( systemData.UseAlternateRateOfFireForXShotsBeforeReverting, false, false ).Add( " at " )
                                        .WrapReload( systemStats.AltSecondsPerSalvo, false, false ).Add( "s" );
                            }
                        }
                    }
                    
                    if ( basePreModifierDPS > 0 && !isRetaliatory && !isOnDeath)
                    {
                        buffer.Add( " => " );
                        if ( detailLevel < TooltipDetail.Full )
                        {
                            buffer.WrapDamagePerSecondTruncated( basePreModifierDPS, false, false );
                            if ( effectiveMaxTargets > 1 )
                                buffer.Add( " to " ).WrapDamagePerSecondTruncated( maxPreModifierDPS, false, false );
                        } else
                        {
                            buffer.WrapDamagePerSecondMoreReadable( basePreModifierDPS, false, false );
                            if ( effectiveMaxTargets > 1 )
                                buffer.Add( " to " ).WrapDamagePerSecondMoreReadable( maxPreModifierDPS, false, false );
                        }
                        buffer.Add( " DPS" );
                    }

                    debugStage = 205;

                    if ( isRetaliatory || ( systemData.ShotTypeData != null && systemData.ShotTypeData.Category == GameEntityCategory.Ship ) )
                    { }
                    else if ( systemData.ShotsDetonateImmediately &&
                        //beam weapons detonate immediately but still have a range
                        systemData.BeamLengthMultiplier <= FInt.Zero )
                    {
                        //buffer.Add(" at ").WrapRange( "从舰船引爆", false, false );
                    }
                    else if ( systemData.IsMelee )
                        buffer.Add( " at " ).WrapRange( "近战范围", false, false );
                    else
                    {
                        int actualRange;
                        if ( relatedSquadOrNull != null ) {
                            actualRange = systemStats.CalculateActualRange( relatedSquadOrNull );
                        } else {
                            actualRange = systemStats.CalculateActualRange( facOrNull, relatedPlanet, 0 );
                        }
                        buffer.Add( " at " );
                        if ( actualRange > 99999 ) {
                            //FiresFromAnyRange actually just has to do with targeting logic, and doesn't seem
                            //like something that we should be surfacing to players here.  Just doesn't seem relevant.
                            buffer.WrapRange( "无限射程", false, false );
                        } else if ( detailLevel < TooltipDetail.Full )
                        {
                            buffer.WrapRangeTruncated( actualRange, false, true );
                        } else
                        {
                            buffer.WrapRangeMoreReadable( actualRange, false, true );
                        }
                    }

                    if ( systemData.CareAboutStateOfMatterToBeEnabled )
                    {
                        buffer.Add( " - " );
                        if( relatedSquadOrNull != null )
                        {
                            if ( systemData.MustBeThisStateOfMatterToBeEnabled == relatedSquadOrNull.CurrentStateOfMatter )
                            {
                                buffer.Add( "已启用", Color.green ).Add( "，因为处于 " ).Add( systemData.MustBeThisStateOfMatterToBeEnabled.DisplayName, "aaaaaa" ).Add( " 态" );
                            } else
                            {
                                buffer.Add( "已禁用", Color.red ).Add( "，因为不处于 " ).Add( systemData.MustBeThisStateOfMatterToBeEnabled.DisplayName, "aaaaaa" ).Add( " 态" );
                            }
                        } else
                        {
                            buffer.Add( "必须处于 " ).Add( systemData.MustBeThisStateOfMatterToBeEnabled.DisplayName, "aaaaaa" ).Add( " 态才能启用" );
                        }
                    }
                    //WriteSystemStateOfMatterSuffix( buffer, systemData, relatedSquadOrNull );

                    debugStage = 210;

                    #region ShowWeaponActivityDetails
                    if ( relatedSquadOrNull != null && !isForMultipleUnits && WeaponActivityDetails > 0 &&
                        detailLevel >= TooltipDetail.Medium )
                    {
                        if( actualEntitySystemOrNull != null )
                        {
                            buffer.AddSize_Small();
                            if ( detailLevel <= TooltipDetail.Medium )
                                buffer.Add( " " ); //no newline for abbreviated version
                            else
                                buffer.Add( "\n" );
                            bool writeSeparator = false;
                            if ( !isRetaliatory && !isOnDeath && ( WeaponActivityDetails == 1 || WeaponActivityDetails == 3 ) )
                            {
                                if ( actualEntitySystemOrNull.TimeUntilNextShot <= FInt.Zero )
                                    buffer.Add( "准备 " ).WrapReload( "开火", false, false );
                                else
                                    buffer.Add( "就绪于 " ).StartReloadWrapper( false ).AddNumberTruncated( actualEntitySystemOrNull.TimeUntilNextShot.ToRounded( 1 ) ).Add( "s" ).EndColor();
                                writeSeparator = true;
                            }
                            if ( actualEntitySystemOrNull.LastGameSecondIFiredShot > 0 && ( WeaponActivityDetails == 2 || WeaponActivityDetails == 3 ) )
                            {
                                if ( writeSeparator )
                                    buffer.Add( " " );
                                
                                buffer.Add( "上次开火 " );

                                buffer.StartColor( QuickColors.OldValue ).Add( World_AIW2.Instance.GameSecond - actualEntitySystemOrNull.LastGameSecondIFiredShot ).Add( "s" ).EndColor()
                                    .Add( " ago" );
                                if ( actualEntitySystemOrNull.LastGameSecondMyShotHit > 0 )
                                    buffer.Add( "造成 " ).WrapDamage( actualEntitySystemOrNull.LastTotalDamageMyShotDidCaused, useIcons, useText );
                            }
                            buffer.EndSize();
                        }
                    }
                    #endregion ShowWeaponActivityDetails

                    //if there is a description for the weapon
                    {
                        bool haveDoneNewLineAndSize = false;

                        debugStage = 215;

                        #region AOE
                        if ( systemStats.ShotAreaOfEffect > 0 )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            //lighting-style burst out of ship
                            if ( systemData.ShotsDetonateImmediately )
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                {
                                    buffer.Add( "<color=#f25e1c>直接范围伤害</color>: radius <color=#ffdf72>" );
                                    buffer.AddNumberMoreReadable( systemStats.ShotAreaOfEffect );
                                    buffer.Add( "</color>, " );
                                    if ( systemData.AOESpreadsDamageAmongAvailableTargets )
                                        buffer.Add( "伤害在目标间分配，" );
                                    if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                        buffer.Add( "最多 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> 个目标" );
                                    else
                                        buffer.Add( "范围内所有目标" );

                                    if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                    {
                                        buffer.Add( " and " );
                                        buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                        buffer.Add( "%伤害对非主要目标" );
                                    }

                                    if ( systemData.AOEHitsFriendlyTargets )
                                        buffer.Add( "，友军伤害。  " );
                                    else
                                        buffer.Add( ".  " );
                                }
                                else
                                {
                                    buffer.Add( "<color=#f25e1c>AOE</color>: A blast emanates directly out of this ship with a radius of <color=#ffdf72>" );
                                    buffer.AddNumberMoreReadable( systemStats.ShotAreaOfEffect );
                                    buffer.Add( "</color>, " );
                                    if ( systemData.AOESpreadsDamageAmongAvailableTargets )
                                    {
                                        buffer.Add( "伤害作用于多个目标，" );
                                        if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                            buffer.Add( "最多 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> 个目标" );
                                        else
                                            buffer.Add( "范围内所有目标" );
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( ", with " );
                                            buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                            buffer.Add( "%伤害对主要目标外的目标" );
                                        }
                                    }
                                    else
                                    {
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( "对主要目标造成全额伤害，并对 " );
                                            buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                            buffer.Add( "% 伤害作用于 " );
                                        }
                                        else
                                            buffer.Add( "对所有目标造成全额伤害 " );
                                        if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                            buffer.Add( "最多 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> 个目标" );
                                        else
                                            buffer.Add( "范围内所有目标" );
                                    }
                                    if ( systemData.AOEHitsFriendlyTargets )
                                        buffer.Add( "包括爆炸范围内的任何友军。  " );
                                    else
                                        buffer.Add( ".  " );
                                }
                            }
                            else //AOE at destination
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                {
                                    buffer.Add( "<color=#f25e1c>目标处范围伤害</color>: radius <color=#ffdf72>" );
                                    buffer.AddNumberMoreReadable( systemStats.ShotAreaOfEffect );
                                    buffer.Add( "</color>, " );
                                    if ( systemData.AOESpreadsDamageAmongAvailableTargets )
                                        buffer.Add( "伤害在目标间分配，" );
                                    if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                        buffer.Add( "最多 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> 个目标" );
                                    else
                                        buffer.Add( "范围内所有目标" );

                                    if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                    {
                                        buffer.Add( " and " );
                                        buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                        buffer.Add( "%伤害对非主要目标" );
                                    }

                                    if ( systemData.AOEHitsFriendlyTargets )
                                        buffer.Add( "，友军伤害。  " );
                                    else
                                        buffer.Add( ".  " );
                                }
                                else
                                {
                                    buffer.Add( "<color=#f25e1c>AOE</color>: Shots from the above weapon explode with a radius of <color=#ffdf72>" );
                                    buffer.AddNumberMoreReadable( systemStats.ShotAreaOfEffect );
                                    buffer.Add( "</color> on impact, " );
                                    if ( systemData.AOESpreadsDamageAmongAvailableTargets )
                                    {
                                        buffer.Add( "伤害分散于多个目标，" );
                                        if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                            buffer.Add( "最多 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> 个目标" );
                                        else
                                            buffer.Add( "范围内所有目标" );
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( ", with " );
                                            buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                            buffer.Add( "%伤害对主要目标外的目标" );
                                        }
                                    }
                                    else
                                    {
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( "对主要目标造成全额伤害，并对 " );
                                            buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                            buffer.Add( "% 伤害作用于 " );
                                        }
                                        else
                                            buffer.Add( "对所有目标造成全额伤害 " );
                                        if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot > 0 )
                                            buffer.Add( "最多 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> 个目标" );
                                        else
                                            buffer.Add( "范围内所有目标" );
                                    }
                                    if ( systemData.AOEHitsFriendlyTargets )
                                        buffer.Add( "包括爆炸范围内的任何友军。  " );
                                    else
                                        buffer.Add( ".  " );
                                }
                            }
                        }
                        #endregion

                        debugStage = 220;

                        #region Beam Weapon
                        if ( systemData.BeamLengthMultiplier > FInt.Zero )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( systemData.IsCoilbeam )
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                {
                                    if ( systemData.NumberBeamsToFire == 1 )
                                        buffer.Add( "<color=#f25e1c>线圈光束武器</color>: length <color=#ffdf72>" );
                                    else
                                    {
                                        buffer.Add( "<color=#f25e1c>线圈光束阵列武器</color>: <color=#ffdf72>" );
                                        buffer.Add( systemData.NumberBeamsToFire );
                                        buffer.Add( "</color> beams, length <color=#ffdf72>" );
                                    }
                                    buffer.AddNumberMoreReadable( (systemStats.CalculateActualRange( relatedSquadOrNull ) * systemData.BeamLengthMultiplier).IntValue );


                                    if ( systemData.NumberBeamsToFire == 1 )
                                    {
                                        buffer.Add( "</color>" );
                                    }
                                    else
                                        buffer.Add( "</color> with 1/" ).Add( systemData.NumberBeamsToFire ).Add( " DMG/beam" );
                                    if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot >= 1 )
                                    {
                                        buffer.Add( "，击中主要目标全额伤害，然后 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot );
                                        buffer.Add( "</color> targets with " );
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                            buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() )
                                                .Add( "%伤害" );
                                        else
                                            buffer.Add( "伤害" );
                                        buffer.Add( " divided evenly, max damage per beam <color=#ffdf72>" ).Add( (systemData.ForMark[effectiveMarkLevel].CalculateShipOrFleetMemDamagePerShot( relatedSquadOrNull, relatedMembershipOrNull ) * 2) ).Add( "</color>" );
                                    }
                                    else
                                    {
                                        buffer.Add( "，击中主要目标全额伤害，其余目标造成" );
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                            buffer.Add( "第二份" ).Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() )
                                                .Add( "%的伤害" );
                                        else
                                            buffer.Add( "第二份伤害" );
                                        buffer.Add( " divided evenly, max damage per beam <color=#ffdf72>" )
                                            .Add( (systemData.ForMark[effectiveMarkLevel].CalculateShipOrFleetMemDamagePerShot( relatedSquadOrNull, relatedMembershipOrNull ) * 2) ).Add( "</color>" );
                                    }

                                    if ( systemData.AOEHitsFriendlyTargets )
                                        buffer.Add( "，友军伤害。  " );
                                    else
                                        buffer.Add( ".  " );
                                }
                                else
                                {
                                    if ( systemData.NumberBeamsToFire == 1 )
                                        buffer.Add( "<color=#f25e1c>线圈光束武器</color>: Attacks from the above weapon are in the form of a fan of a beam of length <color=#ffdf72>" );
                                    else
                                    {
                                        buffer.Add( "<color=#f25e1c>线圈光束阵列武器</color>: Attacks from the above weapon are in the form of a fan of <color=#ffdf72>" );
                                        buffer.Add( systemData.NumberBeamsToFire );
                                        buffer.Add( "</color> beams of length <color=#ffdf72>" );
                                    }
                                    buffer.AddNumberMoreReadable( (systemStats.CalculateActualRange( relatedSquadOrNull ) * systemData.BeamLengthMultiplier).IntValue );
                                    if ( systemData.NumberBeamsToFire == 1 )
                                    {
                                        buffer.Add( "</color>, " );
                                    }
                                    else
                                    {
                                        buffer.Add( "</color> with <color=#ffdf72>" );
                                        buffer.AddFixedDecimal( systemData.DegreesOffsetPerBeam, 1 );
                                        buffer.Add( " degrees</color> between each beam, " );
                                    }
                                    if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot >= 1 )
                                    {
                                        buffer.Add( "击中主要目标造成全额伤害，然后击中 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( " 个目标，造成第二份伤害并在所有目标间平均分摊，每束最大伤害 <color=#ffdf72>" ).Add( (systemData.ForMark[effectiveMarkLevel].CalculateShipOrFleetMemDamagePerShot( relatedSquadOrNull, relatedMembershipOrNull ) * 2) ).Add( "</color>。" );
                                    }
                                    else
                                        buffer.Add( "击中主要目标造成全额伤害，然后对所有其他目标造成第二份伤害，平均分摊，每束最大伤害 <color=#ffdf72>" ).Add( (systemData.ForMark[effectiveMarkLevel].CalculateShipOrFleetMemDamagePerShot( relatedSquadOrNull, relatedMembershipOrNull ) * 2) ).Add( "</color>。" );
                                    if ( systemData.NumberBeamsToFire > 1 )
                                        buffer.Add( "注意：由于多束武器可能同时命中同一目标，较近或较大的目标往往会受到更多伤害。  " );
                                }
                            }
                            else 
                            if ( !systemData.HitsAllIntersectingTargets )
                            {
                                if ( systemData.BeamChainsOutToTargetsXTimes > 0 && systemData.BeamChainsOutToTargetsXRange > 0 )
                                {
                                    if ( systemData.DistanceFromCenterForBeamEmission > 0 )
                                        buffer.Add( "<color=#f25e1c>径向连锁闪电武器</color>: <color=#ffdf72>" );
                                    else
                                        buffer.Add( "<color=#f25e1c>连锁闪电武器</color>: Hits " );

                                    if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot >= 1 )
                                    {
                                        buffer.Add( "最多 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( " 个总目标 " );
                                    }
                                    else
                                        buffer.Add( "任意数量的总目标 " );

                                    buffer.Add( "链式闪电攻击，跳跃 <color=#ffdf72>" ).AddNumberMoreReadable( systemData.BeamChainsOutToTargetsXTimes ).Add( " 次 " );
                                    buffer.Add( "链式范围 <color=#ffdf72>" ).AddNumberMoreReadable( systemData.BeamChainsOutToTargetsXRange ).Add( "</color>。" );

                                    if ( detailLevel < TooltipDetail.Full )
                                    { }
                                    else
                                    {
                                        buffer.Add( "每次闪电跳跃可击中 " );
                                        if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot >= 1 )
                                        {
                                            buffer.Add( "最多 <color=#ffdf72>" ).Add( systemData.BeamChainsOutToMaxTargetsFromEachSource ).Add( "</color> 个目标在下一周期。  " );
                                        }
                                        else
                                            buffer.Add( "任意数量目标在下一周期。" );
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( "主要目标后的每个目标承受 " ).Add(
                                                systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() ).Add( "% 的常规伤害。" );
                                        }
                                        else
                                            buffer.Add( "主要目标后的每个目标承受全额伤害。" );
                                    }
                                }
                                else
                                {
                                    if ( detailLevel < TooltipDetail.Full )
                                        buffer.Add( "<color=#f25e1c>点光束武器</color>: Hits one target. " );
                                    else
                                        buffer.Add( "<color=#f25e1c>点光束武器</color>: Attacks from the above weapon are in the form of beam that hits only the target it was aimed at. " );
                                }
                            }
                            else
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                {
                                    if ( systemData.NumberBeamsToFire == 1 )
                                        buffer.Add( "<color=#f25e1c>光束武器</color>: length <color=#ffdf72>" );
                                    else
                                    {
                                        if ( systemData.DistanceFromCenterForBeamEmission > 0 )
                                            buffer.Add( "<color=#f25e1c>径向光束阵列武器</color>: <color=#ffdf72>" );
                                        else
                                            buffer.Add( "<color=#f25e1c>光束阵列武器</color>: <color=#ffdf72>" );
                                        buffer.Add( systemData.NumberBeamsToFire );
                                        buffer.Add( "</color> beams, length <color=#ffdf72>" );
                                    }
                                    buffer.AddNumberMoreReadable( (systemStats.CalculateActualRange( relatedSquadOrNull ) * systemData.BeamLengthMultiplier).IntValue );

                                    if ( systemData.NumberBeamsToFire == 1 )
                                    {
                                        buffer.Add( "</color>" );
                                    }
                                    else
                                        buffer.Add( "</color> with 1/" ).Add( systemData.NumberBeamsToFire ).Add( " DMG/beam" );
                                    if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot >= 1 )
                                        buffer.Add( "，击中 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( " 个目标，每束最大伤害 <color=#ffdf72>" ).Add( (systemData.ForMark[effectiveMarkLevel].CalculateShipOrFleetMemDamagePerShot( relatedSquadOrNull, relatedMembershipOrNull ) * systemStats.AOEMaximumNumberOfTargetsHitPerShot / systemData.NumberBeamsToFire) ).Add( "</color>" );
                                    if ( systemData.AOEHitsFriendlyTargets )
                                        buffer.Add( "，友军火力" );

                                    if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                    {
                                        buffer.Add( ", " );
                                        buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                        buffer.Add( "%伤害对非主要目标" );
                                    }
                                    buffer.Add( ".  " );
                                }
                                else
                                {
                                    if ( systemData.NumberBeamsToFire == 1 )
                                        buffer.Add( "<color=#f25e1c>光束武器</color>: Attacks from the above weapon are in the form of a fan of a beam of length <color=#ffdf72>" );
                                    else
                                    {
                                        if ( systemData.DistanceFromCenterForBeamEmission > 0 )
                                            buffer.Add( "<color=#f25e1c>径向光束阵列武器</color>: <color=#ffdf72>" );
                                        else
                                            buffer.Add( "<color=#f25e1c>光束阵列武器</color>: Attacks from the above weapon are in the form of a fan of <color=#ffdf72>" );
                                        buffer.Add( systemData.NumberBeamsToFire );
                                        buffer.Add( "</color> beams of length <color=#ffdf72>" );
                                    }
                                    buffer.AddNumberMoreReadable( (systemStats.CalculateActualRange( relatedSquadOrNull ) * systemData.BeamLengthMultiplier).IntValue );
                                    if ( systemData.NumberBeamsToFire == 1 )
                                    {
                                        buffer.Add( "</color>, " );
                                    }
                                    else
                                    {
                                        buffer.Add( "</color> with <color=#ffdf72>" );
                                        buffer.AddFixedDecimal( systemData.DegreesOffsetPerBeam, 1 );
                                        buffer.Add( " degrees</color> between each beam, " );
                                    }
                                    if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot >= 1 )
                                    {
                                        buffer.Add( "击中 <color=#ffdf72>" ).Add( systemStats.AOEMaximumNumberOfTargetsHitPerShot ).Add( "</color> 个目标" );
                                    }
                                    else
                                        buffer.Add( "击中光束路径上的所有目标" );
                                    if ( systemData.NumberBeamsToFire == 1 )
                                    {
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( " with " );
                                            buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                            buffer.Add( "%伤害对主要目标外的目标" );
                                        }
                                        else
                                            buffer.Add( " with its full damage" );
                                    }
                                    else
                                    {
                                        buffer.Add( " with 1/" ).Add( systemData.NumberBeamsToFire ).Add( " its damage per beam that strikes them" );
                                        if ( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget > FInt.Zero )
                                        {
                                            buffer.Add( " and " );
                                            buffer.Add( systemData.AOEAndBeamDamageMultiplierToNonPrimaryTarget.GetNearestIntPreferringHigher() );
                                            buffer.Add( "%伤害对主要目标外的目标" );
                                        }
                                    }
                                    if ( systemData.AOEHitsFriendlyTargets )
                                        buffer.Add( "包括爆炸范围内的任何友军。  " );
                                    else
                                        buffer.Add( ".  " );
                                    if ( systemData.NumberBeamsToFire > 1 )
                                        buffer.Add( "注意：由于多束武器可能同时命中同一目标，较近或较大的目标往往会受到更多伤害。  " );
                                }
                            }
                        }
                        #endregion

                        debugStage = 223;

                        #region ShotsPerSalvo
                        if ( systemData.ForMark[effectiveMarkLevel].ShotsPerSalvo > 1 && !isMirrorShot )
                        {
                            if ( systemData.BeamLengthMultiplier > FInt.Zero )
                            {
                                HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );
                                if ( detailLevel < TooltipDetail.Full )
                                {
                                    buffer.Add( "<color=#f25e1c>多光束</color>: up to <color=#ffdf72>" ).Add( systemData.ForMark[effectiveMarkLevel].ShotsPerSalvo ).Add( "</color> beams" );
                                    if ( !systemData.HitsAllIntersectingTargets )
                                        buffer.Add( "，每束一个目标。  " );
                                    else if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot >= 1 )
                                        buffer.Add( "，对交叉目标全额伤害。  " );
                                } else
                                {
                                    buffer.Add( "<color=#f25e1c>多光束</color>: Up to <color=#ffdf72>" ).Add( systemData.ForMark[effectiveMarkLevel].ShotsPerSalvo )
                                        .Add( "</color> beams can be fired at a time. " );
                                    if ( systemData.ForMark[effectiveMarkLevel].ShotsPerSalvo > systemData.ForMark[effectiveMarkLevel].ShotsPerTarget )
                                        buffer.Add( "仅 " ).Add( systemData.ForMark[effectiveMarkLevel].ShotsPerTarget ).Add( " 可对每个目标和堆叠发射。" );
                                    else
                                        buffer.Add( "均可瞄准同一目标。" );
                                    if ( !systemData.HitsAllIntersectingTargets )
                                        buffer.Add( "单个目标可能被多束相交光束击中。每束光束造成全额伤害。" );
                                    else if ( systemStats.AOEMaximumNumberOfTargetsHitPerShot >= 1 )
                                        buffer.Add( "每束光束造成全额伤害。" );
                                }
                            } else
                            {
                                if ( detailLevel >= TooltipDetail.Full )
                                {
                                    HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );
                                    buffer.Add( "<color=#f25e1c>多发</color>: Up to <color=#ffdf72>" ).Add( systemData.ForMark[effectiveMarkLevel].ShotsPerSalvo )
                                        .Add( "</color> shots can be fired at a time. " );
                                    if ( systemData.ForMark[effectiveMarkLevel].ShotsPerSalvo > systemData.ForMark[effectiveMarkLevel].ShotsPerTarget )
                                        buffer.Add( "仅 " ).Add( systemData.ForMark[effectiveMarkLevel].ShotsPerTarget ).Add( " 可对每个目标和堆叠发射" );
                                    else
                                        buffer.Add( "均可瞄准同一目标" );
                                    buffer.Add( ", and each shot does the full damage listed above.  " );
                                }
                            }
                        }
                        #endregion

                        debugStage = 220;
                        #region isMirrorShot
                        if ( isMirrorShot )
                        {
                            buffer.Add( "<color=#f25e1c>" );
                            buffer.Add( (systemData.ReturnsThisPercentageOfDamageWhenFiringRetaliatoryShot * 100).GetNearestIntPreferringHigher() );
                            buffer.Add( "% 镜像伤害:</color> " );
                            if ( detailLevel >= TooltipDetail.Medium )
                            {
                                buffer.Add( "此镜面武器反射一枚射弹，拥有击中母舰的 " ).Add(
                                    (systemData.ReturnsThisPercentageOfDamageWhenFiringRetaliatoryShot * 100).GetNearestIntPreferringHigher() )
                                    .Add( "% 威力，返回给射击者。这被视为 <color=#dfff72>异能伤害</color>。" );
                            }
                            else
                                buffer.Add( "基于击中母舰的冲击力反射一枚射弹。" );
                        }
                        #endregion
                        debugStage = 221;
                        #region MobileImmobile
                        if ( systemData.OnlyTargetsStaticUnits )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                                buffer.Add( "仅限目标建筑物。  " );
                        }
                        if ( systemData.OnlyTargetsMobileUnits )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                                buffer.Add( "仅限目标移动单位。  " );
                        }
                        if ( systemData.OnlyTargetsStrikecraftAndFrigates )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                                buffer.Add( "仅限目标攻击艇和护卫舰。  " );
                        }
                        #endregion
                        debugStage = 225;


                        #region EngineStun
                        if ( systemData.EngineStunToEngine_gxLessThan > 0 )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                if ( systemData.MaxEngineStunSeconds > 0 )
                                    buffer.Add( "<color=#f25e1c>引擎减速</color>: <color=#ffdf72>" );
                                else
                                    buffer.Add( "<color=#f25e1c>引擎眩晕</color>: <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.EngineStunPerShot );
                                buffer.Add( "秒</color> 如果目标引擎 < <color=#ffdf72>" );
                                buffer.Add( systemData.EngineStunToEngine_gxLessThan );
                                buffer.Add( " gx</color>" );
                                if ( systemData.MaxEngineStunSeconds > 0 )
                                {
                                    if ( systemData.MaxEngineStunSeconds >= ExternalConstants.Instance.EngineStunMultipliersByStunSeconds.Count )
                                        buffer.Add( ".  " ); //fully stunned
                                    else //partially stunned
                                        buffer.Add( ", max " ).AddNumberMoreReadable( systemData.MaxEngineStunSeconds ).Add( "s.  " );
                                }
                                else
                                    buffer.Add( ".  " );
                            }
                            else
                            {
                                if ( systemData.MaxEngineStunSeconds > 0 )
                                    buffer.Add( "<color=#f25e1c>引擎减速</color>: Shots from the above weapon slow enemy engines for <color=#ffdf72>" );
                                else
                                    buffer.Add( "<color=#f25e1c>引擎眩晕</color>: Shots from the above weapon stun enemy engines for <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.EngineStunPerShot );
                                buffer.Add( "秒</color> 如果目标引擎动力低于 <color=#ffdf72>" );
                                buffer.Add( systemData.EngineStunToEngine_gxLessThan );
                                buffer.Add( " gx</color>.  " );
                                if ( systemData.MaxEngineStunSeconds > 0 )
                                {
                                    FInt engineSpeed = FInt.Zero;
                                    if ( systemData.MaxEngineStunSeconds >= ExternalConstants.Instance.EngineStunMultipliersByStunSeconds.Count )
                                    { } //fully stunned
                                    else //partially stunned
                                        engineSpeed = ExternalConstants.Instance.EngineStunMultipliersByStunSeconds[systemData.MaxEngineStunSeconds];

                                    buffer.Add( "目标可被减速最多 " ).AddNumberMoreReadable( systemData.MaxEngineStunSeconds )
                                        .Add( "秒，届时其移动速度仅为 <color=#ffdf72>" ).Add( engineSpeed.ReadableString ).Add( "倍</color> 正常速度。" );
                                }
                                else
                                    buffer.Add( "目标累积的眩晕秒数越多，速度越慢。4秒 = 50%移动速度，7秒以上 = 无法移动。" );
                            }
                        }
                        #endregion

                        debugStage = 235;

                        #region Paralysis
                        if ( systemData.ParalysisToShipsMass_tXLessThan > 0 )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#f25e1c>麻痹</color>: <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.ParalysisSecondsPerShot );
                                buffer.Add( "秒</color> 如果目标质量 < <color=#ffdf72>" );
                                buffer.Add( systemData.ParalysisToShipsMass_tXLessThan.ReadableString );
                                buffer.Add( " tX</color>.  " );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>麻痹</color>: Shots from the above weapon completely shut down enemy ships for <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.ParalysisSecondsPerShot );
                                buffer.Add( "秒</color> 如果目标质量低于 <color=#ffdf72>" );
                                buffer.Add( systemData.ParalysisToShipsMass_tXLessThan.ReadableString );
                                buffer.Add( " tX</color>.  " );
                            }
                        }
                        #endregion
                        debugStage = 236;

                        #region FiresThroughEnemyShields
                        if ( systemData.FiresThroughEnemyShields )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#f25e1c>穿透力场射击</color>  " );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>穿透力场射击</color>: Shots from the above weapon completely ignore forcefields as if they were not there, damaging ships under them like normal.  " );
                            }
                        }
                        #endregion

                        #region SetsTargetToRandomLocation
                        if ( systemData.SetsTargetToRandomLocation )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#f25e1c>目标传送</color>  " );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>目标传送</color>: Shots from the above weapon cause enemies that are hit to be moved to a completely random spot in the planet's gravity well.  " );
                            }
                        }
                        #endregion

                        debugStage = 237;
                        #region StackingDamage
                        if ( systemData.MaxStacksToKill != 1 )
                        {
                            buffer.Add( " <color=#f25e1c>多次击杀</color>: 每次射击可从一个编队中击杀 <color=#ffdf72>" + systemData.MaxStacksToKill + "</color> 个单位。 " );
                        }
                        #endregion

                        debugStage = 238;

                        #region TractorDamage
                        if ( systemStats.DamageMultiplierToTractoredUnits > FInt.Zero )

                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );
                            buffer.Add( "<color=#f25e1c>攻击加成</color>: 上述武器的射击造成 <color=#ffdf72>" ).Add( systemStats.DamageMultiplierToTractoredUnits.ReadableString, "a1ffa1" ).Add( "x</color>伤害对牵引光束中的单位。" );
                        }
                        #endregion

                        debugStage = 239;

                        #region ForcefieldDamageMultiplier
                        if ( systemData.DamageModifierWhileUnderForcefield != FInt.FromParts( 0, 500 ) )
                        {
                            if ( systemData.DamageModifierWhileUnderForcefield > FInt.One )
                                hasSystemWithBonusFromAttackingUnderForcefields = true;

                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );
                            buffer.Add( "<color=#f25e1c>攻击加成</color>: 上述武器的射击造成 <color=#ffdf72>" ).Add( systemData.DamageModifierWhileUnderForcefield.ReadableString, "a1ffa1" ).Add( "x</color>伤害如果此单位在力场下，忽略正常惩罚。" );
                        }
                        #endregion

                        debugStage = 240;

                        #region WeaponPoints
                        // The case where it doesn't consume Weapon Points on firing, i.e Neinzul Firefly style.
                        if ( systemData.AdditionalDamageModifierPerWeaponPoint != FInt.Zero && systemData.NumberOfWeaponPointsToConsumeOnFiring == 0 )

                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                                buffer.Add( "<color=#f25e1c>攻击加成</color>: Additional <color=#ffdf72>" ).Add( systemData.AdditionalDamageModifierPerWeaponPoint.ReadableString, "a1ffa1" ).Add( "x</color>每当前武器点数。" );
                            else
                                buffer.Add( "<color=#f25e1c>攻击加成</color>: 上述武器的射击造成 an additional <color=#ffdf72>" ).Add( systemData.AdditionalDamageModifierPerWeaponPoint.ReadableString, "a1ffa1" ).Add( "x</color>伤害每当前拥有的武器点数。" );
                        }

                        // The case where it DOES consume Weapon Points on firing, i.e Powerslaver style.
                        if ( systemData.AdditionalDamageModifierPerWeaponPoint != FInt.Zero && systemData.NumberOfWeaponPointsToConsumeOnFiring != 0 )

                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                                buffer.Add( "<color=#f25e1c>攻击加成</color>: Consumes <color=#ffdf72>" + systemData.NumberOfWeaponPointsToConsumeOnFiring + "</color> Weapon Points, additional <color=#ffdf72>" ).Add( systemData.AdditionalDamageModifierPerWeaponPoint.ReadableString, "a1ffa1" ).Add( "x</color>伤害每消耗的武器点数。" );
                            else
                                buffer.Add( "<color=#f25e1c>攻击加成</color>: Shots from the above weapon consume <color=#ffdf72>" + systemData.NumberOfWeaponPointsToConsumeOnFiring + "</color> Weapon Points per salvo, doing an additional <color=#ffdf72>" ).Add( systemData.AdditionalDamageModifierPerWeaponPoint.ReadableString, "a1ffa1" ).Add( "x</color>伤害每消耗的武器点数。" );
                        }

                        if ( systemData.NumberOfWeaponPointsToGainOnFiring != FInt.Zero )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            buffer.Add( "<color=#f25e1c>获得武器点数</color>: Shots from the above weapon increase this units Weapon Points by <color=#ffdf72>" + systemData.NumberOfWeaponPointsToGainOnFiring + "</color>, up to max of <color=#ffdf72>" + systemData.ParentEntityTypeData.MaxNumberOfWeaponPoints + "</color>. " );
                        }
                        #endregion

                        debugStage = 241;

                        #region Devour
                        if ( systemData.CanDevour )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( systemData.DevourFunctionNameUpper.Length <= 0 )
                                systemData.DevourFunctionNameUpper = systemData.DevourFunctionName.ToUpper();

                            buffer.Add( "<color=#f25e1c>" ).Add( systemData.DevourFunctionNameUpper ).Add( "</color>：如果目标舰船同级或更低，且质量低于 <color=#ffdf72>" )
                                .Add( systemData.DevourMassLowerThan.ReadableString ).Add( "tX</color>，则秒杀目标舰船。" );
                        }
                        #endregion

                        debugStage = 242;

                        #region Infestation
                        if ( systemData.UnitToSpawnOnInfestation.Length > 0 )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( systemData.InfestationFunctionNameUpper.Length <= 0 )
                                systemData.InfestationFunctionNameUpper = systemData.InfestationFunctionName.ToUpper();

                            GameEntityTypeData thingToSpawnFromInfestedUnit = GameEntityTypeDataTable.Instance.GetRowByName( systemData.UnitToSpawnOnInfestation );

                            buffer.Add( "<color=#f25e1c>" ).Add( systemData.InfestationFunctionNameUpper ).Add( ":</color> Whenever this unit deals the killing blow to a foe, it will spawn a " + (thingToSpawnFromInfestedUnit?.DisplayName ?? systemData.UnitToSpawnOnInfestation) + " at the foe's location. " );
                        }
                        #endregion

                        debugStage = 250;

                        #region Death Effects
                        if ( systemData.HasAnyDeathEffectOffenses )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            for ( int k = 0; k < DeathEffectTypeTable.Instance.Rows.Count; k++ )
                            {
                                DeathEffectType row = DeathEffectTypeTable.Instance.Rows[k];
                                int damageAmount = systemStats.DeathEffectDamagePerShotByType[row];
                                if ( damageAmount <= 0 )
                                    continue;

                                if ( detailLevel < TooltipDetail.Full )
                                {
                                    buffer.Add( "<color=#f25e1c> " ).Add( row.DescriptionPrefix ).Add( "</color>: <color=#ffdf72>" );
                                    buffer.AddNumberMoreReadable( damageAmount );
                                    buffer.Add( "</color> " ).Add( row.DescriptionDamageName ).Add( " damage" )
                                        .Add( row.DescriptionConditions != null && row.DescriptionConditions.Length > 0 ? " " : "" ).Add( row.DescriptionConditions )
                                        .Add( ".  " ).AddNumberMoreReadable( row.Scale ).Add( " total damage required for effect.  " );
                                    //only for player ships or things that are being contemplated for construction
                                    if ( relatedSquadOrNull == null || relatedSquadOrNull.GetFactionTypeSafe() != FactionType.Player )
                                    {
                                        if ( row.DescriptionAddedNoteForPlayers != null && row.DescriptionAddedNoteForPlayers.Length > 0 )
                                            buffer.Add( row.DescriptionAddedNoteForPlayers ).Add( "  " );
                                    }
                                }
                                else
                                {
                                    buffer.Add( "<color=#f25e1c> " ).Add( row.DescriptionPrefix ).Add( "</color>：上述造成 <color=#ffdf72>" );
                                    buffer.AddNumberMoreReadable( damageAmount );
                                    buffer.Add( "</color> " ).Add( row.DescriptionDamageName ).Add( " damage" )
                                        .Add( row.DescriptionConditions != null && row.DescriptionConditions.Length > 0 ? " " : "" ).Add( row.DescriptionConditions )
                                        .Add( "。如果至少 " ).AddNumberMoreReadable( row.Scale ).Add( " " ).Add( row.DescriptionDamageName )
                                        .Add( " damage has been done to the target when it dies, then " ).Add( row.DescriptionOfEffects ).Add( ".  " );
                                    //only for player ships or things that are being contemplated for construction
                                    if ( relatedSquadOrNull == null || relatedSquadOrNull.GetFactionTypeSafe() != FactionType.Player )
                                    {
                                        if ( row.DescriptionAddedNoteForPlayers != null && row.DescriptionAddedNoteForPlayers.Length > 0 )
                                            buffer.Add( row.DescriptionAddedNoteForPlayers ).Add( "  " );
                                    }
                                }
                            }
                        }
                        #endregion

                        debugStage = 260;

                        #region IsMelee
                        if ( systemData.IsMelee && detailLevel >= TooltipDetail.Full )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            buffer.Add( " <color=#f25e1c>近战武器</color>: 此舰必须进入目标接触范围内才能攻击。  " );
                        }
                        #endregion

                        debugStage = 265;

                        #region EnemyWeaponReloadSlowing
                        if ( systemData.EnemyWeaponReloadSlowingSecondsArmor_mmLessThan > 0 )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#f25e1c>武器干扰器</color>: target reload +<color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.EnemyWeaponReloadSlowingSecondsPerShot );
buffer.Add( "秒</color> 如果护甲 < <color=#ffdf72>" );
                                 buffer.Add( systemData.EnemyWeaponReloadSlowingSecondsArmor_mmLessThan );
                                 buffer.Add( "毫米</color>，最大 " );
                                 if ( systemData.MaxEnemyWeaponReloadSlowingSeconds > 0 )
                                     buffer.Add( systemData.MaxEnemyWeaponReloadSlowingSeconds );
                                 else
                                     buffer.Add( ExternalConstants.Instance.MaxWeaponAddedReloadSeconds );
                                 buffer.Add( "秒。" );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>武器干扰器</color>: Shots from the above weapon add to the reload times of enemies they hit by <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.EnemyWeaponReloadSlowingSecondsPerShot );
buffer.Add( "秒</color> 如果目标护甲厚度低于 <color=#ffdf72>" );
                                 buffer.Add( systemData.EnemyWeaponReloadSlowingSecondsArmor_mmLessThan );
                                 buffer.Add( "毫米</color>。每个目标可施加的最大额外装填时间为 " );
                                 if ( systemData.MaxEnemyWeaponReloadSlowingSeconds > 0 )
                                     buffer.Add( systemData.MaxEnemyWeaponReloadSlowingSeconds );
                                 else
                                     buffer.Add( ExternalConstants.Instance.MaxWeaponAddedReloadSeconds );
                                 buffer.Add( "秒。" );
                            }
                        }
                        #endregion

                        debugStage = 270;

                        #region PercentDamageBypassesPersonalShields
                        if ( systemStats.PercentDamageBypassesPersonalShields > FInt.Zero )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#f25e1c>聚变反应</color>: <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemStats.PercentDamageBypassesPersonalShields.ToFloatNonSim() * 100f ) );
                                buffer.Add( "%</color> direct to target hull.  " );
                            }
                            else if ( !systemData.FiresThroughEnemyShields )
                            {
                                buffer.Add( "<color=#f25e1c>聚变反应</color>: 上述武器的射击造成 <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemStats.PercentDamageBypassesPersonalShields.ToFloatNonSim() * 100f ) );
                                buffer.Add( "%</color> of their damage directly to the hull of their target, bypassing any personal shields (NOT bubble forcefields).  " );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>聚变反应</color>: 上述武器的射击造成 <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemStats.PercentDamageBypassesPersonalShields.ToFloatNonSim() * 100f ) );
                                buffer.Add( "%</color> 的伤害直接作用于目标船体，绕过所有个人护盾。  " );
                            }
                        }
                        #endregion

                        debugStage = 280;

                        #region KnockbackToTarget
                        if ( systemData.KnockbackPerShotToShipsMass_tXLessThan > 0 )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );
                            #region Abbreviated
                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#f25e1c>击退</color>: " );
                                if ( systemStats.KnockbackPerShot > 0 )
                                {
                                    if ( !systemData.KnockbackAtTargetLocation )
                                    {
                                        buffer.Add( "目标被推开 <color=#ffdf72>" );
                                        buffer.AddNumberMoreReadable( systemStats.KnockbackPerShot );
                                        buffer.Add( "</color> 被推开远离此舰船，如果质量 ≤ <color=#ffdf72>" );
                                        buffer.Add( systemData.KnockbackPerShotToShipsMass_tXLessThan );
                                        buffer.Add( " 吨</color>。质量越大，击退效果越弱。" );
                                    }
                                    else
                                    {
                                        buffer.Add( "被AOE击中的目标被推开 <color=#ffdf72>" );
                                        buffer.AddNumberMoreReadable( systemStats.KnockbackPerShot );
                                        buffer.Add( "</color> 被推离AOE中心，如果质量 ≤ <color=#ffdf72>" );
                                        buffer.Add( systemData.KnockbackPerShotToShipsMass_tXLessThan );
                                        buffer.Add( " 吨</color>。质量越大，击退效果越弱。" );
                                    }
                                }
                                else
                                {
                                    if ( !systemData.KnockbackAtTargetLocation )
                                    {
                                        buffer.Add( "目标被拉向 <color=#ffdf72>" );
                                        buffer.AddNumberMoreReadable( systemStats.KnockbackPerShot );
                                        buffer.Add( "</color> 被拉向此舰船，如果质量 ≤ <color=#ffdf72>" );
                                        buffer.Add( systemData.KnockbackPerShotToShipsMass_tXLessThan );
                                        buffer.Add( " 吨</color>。质量越大，击退效果越弱。" );
                                    }
                                    else
                                    {
                                        buffer.Add( "被AOE击中的目标被拉向 <color=#ffdf72>" );
                                        buffer.AddNumberMoreReadable( systemStats.KnockbackPerShot );
                                        buffer.Add( "</color> 被拉向AOE中心，如果质量 ≤ <color=#ffdf72>" );
                                        buffer.Add( systemData.KnockbackPerShotToShipsMass_tXLessThan );
                                        buffer.Add( " 吨</color>。质量越大，击退效果越弱。" );
                                    }
                                }
                            }
                            #endregion

                            #region Non-Abbreviated
                            else
                            {
                                buffer.Add( "<color=#f25e1c>击退</color>: Enemies hit by this weapon are " );
                                if ( systemStats.KnockbackPerShot > 0 )
                                    buffer.Add( "被推开远离 " );
                                else
                                    buffer.Add( "被拉向 " );
                                if ( !systemData.KnockbackAtTargetLocation )
                                    buffer.Add( "此舰船。" );
                                else
                                    buffer.Add( "此舰船射击的AOE中心。" );
                                if ( systemStats.KnockbackPerShot > 0 )
                                    buffer.Add( "舰船可被推开的距离上限为 <color=#ffdf72>" );
                                else
                                    buffer.Add( "舰船可被拉近的距离上限为 <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.KnockbackPerShot );
                                buffer.Add( "</color>, decreasing as the target's mass approaches the max mass of <color=#ffdf72>" );
                                buffer.Add( systemData.KnockbackPerShotToShipsMass_tXLessThan );
                                buffer.Add( "倍</color>。" );
                            }
                            #endregion
                        }
                        #endregion

                        debugStage = 285;

                        #region DamageAmplification
                        if ( systemData.DamageAmplification && systemData.DamageAmplificationDuration_Max15 > 0 && systemStats.DamageAmplificationFlat > 0 )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#f25e1c>伤害增幅</color>: <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemStats.DamageAmplificationFlat ) );
                                buffer.Add( "</color> extra damage taken by target for <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemData.DamageAmplificationDuration_Max15 ) );
                                buffer.Add( "</color>s.  " );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>伤害增幅</color>: Shots from the above weapon cause the target to take <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemStats.DamageAmplificationFlat ) );
                                buffer.Add( "</color> more damage from every shot that hits it for <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemData.DamageAmplificationDuration_Max15 ) );
                                buffer.Add( "</color> seconds.  " );
                            }
                        }

                        if ( systemData.DamageAmplification && systemData.DamageAmplificationDuration_Max15 > 0 && systemData.DamageAmplificationMult != 1 )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#f25e1c>伤害增幅</color>: Target takes <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemData.DamageAmplificationMult.ToFloatNonSim() * 100f ) );
                                buffer.Add( "%</color> of normal damage for <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemData.DamageAmplificationDuration_Max15 ) );
                                buffer.Add( "</color>s.  " );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>伤害增幅</color>: Shots from the above weapon cause the target to take <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemData.DamageAmplificationMult.ToFloatNonSim() * 100f ) );
                                buffer.Add( "%</color> of normal damage from every shot that hits it for <color=#ffdf72>" );
                                buffer.Add( Mathf.RoundToInt( systemData.DamageAmplificationDuration_Max15 ) );
                                buffer.Add( "</color> seconds.  " );
                            }
                        }
                        #endregion

                        debugStage = 290;

                        #region Vampirism/SelfDamage //Damage-based Vampirism/Self-Damage
                        if ( systemData.HealthChangePerDamageDealt != FInt.Zero )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                if ( systemData.HealthChangePerDamageDealt > FInt.Zero )
                                {
                                    buffer.Add( "<color=#f25e1c>吸血</color>: repairs <color=#ffdf72>" );
                                }
                                else
                                {
                                    buffer.Add( "<color=#f25e1c>自伤</color>: damages itself by <color=#ffdf72>" );
                                }
                                buffer.AddNumberMoreReadable( systemData.HealthChangePerDamageDealt );
                                buffer.Add( " health</color> per damage dealt.  " );
                            }
                            else
                            {
                                if ( systemData.HealthChangePerDamageDealt > FInt.Zero )
                                {
                                    buffer.Add( "<color=#f25e1c>吸血</color>: repairs itself by <color=#ffdf72>" );
                                }
                                else
                                {
                                    buffer.Add( "<color=#f25e1c>自伤</color>: damages itself by <color=#ffdf72>" );
                                }
                                buffer.AddNumberMoreReadable( systemData.HealthChangePerDamageDealt );
                                buffer.Add( " health</color> for every 1 damage it has dealt.  " );
                            }
                        }

                        //Max-Health-based Vampirism/Self-Damage
                        if ( systemData.HealthChangeByMaxHealthDividedByThisPerAttack != FInt.Zero  && !isOnDeath )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );

                            if ( detailLevel < TooltipDetail.Full )
                            {
                                if ( systemData.HealthChangeByMaxHealthDividedByThisPerAttack >= FInt.One * -1 && systemData.HealthChangeByMaxHealthDividedByThisPerAttack < FInt.Zero )
                                {
                                    buffer.Add( "<color=#f25e1c>自毁</color>: kills itself to attack.  " );
                                }
                                else
                                {
                                    if ( systemData.HealthChangeByMaxHealthDividedByThisPerAttack > FInt.Zero )
                                    {
                                        buffer.Add( "<color=#f25e1c>自组装</color>: repairs <color=#ffdf72>" );
                                    }
                                    else
                                    {
                                        buffer.Add( "<color=#f25e1c>分解</color>: damages itself by <color=#ffdf72>" );
                                    }
                                    buffer.AddPercentFormated( ( 1 / systemData.HealthChangeByMaxHealthDividedByThisPerAttack).ToPercent( 1 ) );
                                    buffer.Add( " health</color> each attack.  " );
                                }
                            }
                            else
                            {
                                if ( systemData.HealthChangeByMaxHealthDividedByThisPerAttack >= FInt.One * -1 && systemData.HealthChangeByMaxHealthDividedByThisPerAttack < FInt.Zero )
                                {
                                    buffer.Add( "<color=#f25e1c>自毁</color>: kills itself to attack.  " );
                                }
                                else
                                {
                                    if ( systemData.HealthChangeByMaxHealthDividedByThisPerAttack > FInt.Zero )
                                    {
                                        buffer.Add( "<color=#f25e1c>自组装</color>: repairs itself by <color=#ffdf72>" );
                                    }
                                    else
                                    {
                                        buffer.Add( "<color=#f25e1c>分解</color>: damages itself by <color=#ffdf72>" );
                                    }
                                    buffer.AddPercentFormated( (1 / systemData.HealthChangeByMaxHealthDividedByThisPerAttack).ToPercent( 1 ) );
                                    buffer.Add( " health</color> each attack.  " );
                                }
                            }
                        }
                        #endregion

                        debugStage = 300;

                        #region StateOfMatter-Inflicting

                        if ( systemData.InflictsStateOfMatterOnTargetForSeconds > 0 && systemData.StateOfMatterForTargetToBecome != null )
                        {
                            buffer.Add( "<color=#f25e1c>相位武器</color>: Inflicts " ).Add( systemData.StateOfMatterForTargetToBecome.DisplayName, "aaaaaa" ).Add( " for " )
                                .StartReloadWrapper( false ).Add( systemData.InflictsStateOfMatterOnTargetForSeconds ).Add( "s</color>" );
                            if ( systemData.CannotInflictStateOfMatterIfTargetHasAnyShieldsUp )
                            {
                                if ( detailLevel < TooltipDetail.Full )
                                    buffer.Add( " if target " ).WrapHull( "hull struck", false, false );
                                else
                                    buffer.Add( " if the target's " ).WrapHull( "hull is struck", false, false );
                                if ( systemData.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast > 0 )
                                {
                                    if ( detailLevel < TooltipDetail.Full )
                                        buffer.Add( " and consumes < " ).WrapEnergyTruncated( systemData.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast, useIcons, useText );
                                    else
                                        buffer.Add( " and the target uses less than " ).WrapEnergyMoreReadable( systemData.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast, useIcons, useText );
                                }
                            } else if ( systemData.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast > 0 )
                            {
                                if ( systemData.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast > 0 )
                                {
                                    if ( detailLevel < TooltipDetail.Full )
                                        buffer.Add( " if target consumes < " ).WrapEnergyTruncated( systemData.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast, useIcons, useText );
                                    else
                                        buffer.Add( " if the target uses less than " ).WrapEnergyMoreReadable( systemData.CannotInflictStateOfMatterIfTargetHasEnergyUsageOfAtLeast, useIcons, useText );
                                }
                            }
                        }
                        #endregion

                        debugStage = 310;

                        for ( int k = 0; k < systemData.OutgoingDamageModifiers_FullList.Count; k++ )
                        {
                            HandleNewlineAndSize( buffer, ref haveDoneNewLineAndSize );
                            WriteDamageModifierData( buffer, effectiveMarkLevel,
                                systemData.OutgoingDamageModifiers_FullList[k], systemStats );
                        }

                        if ( haveDoneNewLineAndSize )
                            buffer.Add( "</size>" );
                    }

                    if( ShowAbortCode && relatedSquadOrNull != null && !isForMultipleUnits && WeaponActivityDetails > 0 && actualEntitySystemOrNull != null )
                    {
                        if ( actualEntitySystemOrNull.LastGameSecondMyShotHit > 0 )
                            WriteLastAbortCode( buffer, actualEntitySystemOrNull);
                    }
                    if( ShowTargetInfo && relatedSquadOrNull != null && !isForMultipleUnits && WeaponActivityDetails > 0 && actualEntitySystemOrNull != null )
                    {
                        GameEntity_Squad targeted = actualEntitySystemOrNull.GetTargetOrNull();
                        buffer.Add( "\nPrimary Target: " );
                        if ( targeted != null )
                            buffer.Add( targeted.TypeData.InternalName ).Add( " " ).Add( targeted.PrimaryKeyID ).Add( ", Priority " ).Add( actualEntitySystemOrNull.CurrentFRDPriority )
                                .Add( ", Valid = " ).Add( actualEntitySystemOrNull.GetIsTargetValid( targeted ) ).Add( ", Expires in " ).Add( actualEntitySystemOrNull.TimeFRDTargetExpires );
                        else
                            buffer.Add( "无" );
                        bool added = false;
                        bool first = true;
                        int count = 0;
                        buffer.AddSize_VerySmall().Add( "\nTargets by priority: " );
                        foreach ( LazyLoadSquadWrapper Wrapper in actualEntitySystemOrNull.PotentialTargetsByPriorityForSystem() )
                        {
                            if ( !added )
                                added = true;
                            if ( count >= MaxDisplayedTargets )
                                break;
                            else
                                count++;
                            if ( first )
                                first = false;
                            else
                                buffer.Add( " | " );
                            buffer.Add( count - 1 ).Add( ": " ).Add( Wrapper.TypeData.InternalName ).Add( " " ).Add( Wrapper.PrimaryKeyID );
                            if ( Wrapper.Planet != relatedSquadOrNull.Planet )
                                buffer.Add( " on " ).Add( Wrapper.GetPlanetName_Safe() );
                            if ( !actualEntitySystemOrNull.GetIsTargetInRange( Wrapper.GetSquad(), RangeCheckType.ForActualFiring ) )
                                buffer.Add( ", not in range" );
                        }
                        if ( !added )
                            buffer.Add( "无" );
                        //buffer.Add( "\nIn sim planning group " ).Add( relatedSquadOrNull.GetFactionTargetPlanningGroupSafe() );
                    }
                } //end "must be weapon" IF statement
                #endregion If A Weapon
                else //if not a weapon
                {
                    if ( OnlyWriteIfWeapon )
                    {
                        //buffer.Add( "OnlyWriteIfWeapon" );
                        return;
                    }

                    if ( systemData.ModuleStatAdjuster != ModularStatAdjustment.None )
                    {
                        buffer.Add( "<color=#ffee63>" ).Add( systemData.DisplayName ).Add( ":</color> <color=#eee8b2>" );
                        switch ( systemData.ModuleStatAdjuster )
                        {
                            case ModularStatAdjustment.HullHealth:
                                if ( detailLevel < TooltipDetail.Full )
                                    buffer.AddFixedDecimal( systemData.ModuleStatAdjusterMultiplier.ToFloatNonSim(), 3 ).Add( "x" );
                                else
                                    buffer.Add( "船体生命值倍率 ").AddFixedDecimal( systemData.ModuleStatAdjusterMultiplier.ToFloatNonSim(), 3 ).Add( "x" );
                                break;
                            case ModularStatAdjustment.ShieldHealth:
                                if ( detailLevel < TooltipDetail.Full )
                                    buffer.AddFixedDecimal( systemData.ModuleStatAdjusterMultiplier.ToFloatNonSim(), 3 ).Add( "x" );
                                else
                                    buffer.Add( "护盾生命值倍率 " ).AddFixedDecimal( systemData.ModuleStatAdjusterMultiplier.ToFloatNonSim(), 3 ).Add( "x" );
                                break;
                            case ModularStatAdjustment.BubbleForcefield:
                                if ( systemData.ModuleStatAdjusterMultiplier != FInt.One )
                                {
                                    if ( detailLevel < TooltipDetail.Full )
                                        buffer.AddFixedDecimal( systemData.ModuleStatAdjusterMultiplier.ToFloatNonSim(), 3 ).Add( "x" );
                                    else
                                        buffer.Add( "气泡力场添加，" ).AddFixedDecimal( systemData.ModuleStatAdjusterMultiplier.ToFloatNonSim(), 3 ).Add( "x 倍标准个人护盾评级。" );
                                }
                                else
                                {
                                    if ( detailLevel >= TooltipDetail.Full )
                                        buffer.Add( "气泡力场替换个人护盾；护盾强度不变。" );
                                }
                                break;
                        }
                        buffer.Add( "</color>  " );
                    }
                    else
                    {
                        //not a module stat adjuster
                        if ( OnlyWriteIfModuleStatBooster )
                        {
                            //buffer.Add( "OnlyWriteIfModuleStatBooster 2" );
                            return;
                        }
                    }

                    #region Tachyon System
                    if ( systemData.TachyonHitsAlbedoLessThan > FInt.Zero || systemData.TachyonHitsAlbedoMoreThan > FInt.Zero )
                    {
                        float secondsPerSimFrame = World_AIW2.Instance.SimulationProfile.SecondsPerFrameNonSim;
                        float simFrameMultiplier = 1f / secondsPerSimFrame;

                        if ( detailLevel < TooltipDetail.Full )
                        {
                            buffer.Add( "<color=#f25e1c>反隐形</color>: range <color=#ffdf72>" );
                            buffer.AddNumberMoreReadable( systemStats.TachyonRange );
                            buffer.Add( "</color>, strength <color=#ffdf72>" );
                            //we use simFrameMultiplier to get the actual number per second, rather than the number per frame
                            buffer.AddNumberMoreReadable( Mathf.RoundToInt( systemStats.TachyonPoints * simFrameMultiplier ) );
                            buffer.Add( "</color>, " );

                            WriteMinMaxOrVariant( buffer, "any albedo", "only albedo", true, "ffdf72", systemData.TachyonHitsAlbedoMoreThan, systemData.TachyonHitsAlbedoLessThan );
                        }
                        else
                        {
                            buffer.Add( "<color=#f25e1c>反隐形</color>: Cloaked enemy ships within a range of <color=#ffdf72>" );
                            buffer.AddNumberMoreReadable( systemStats.TachyonRange );
                            buffer.Add( "</color> are saturated with a tachyon field that drains <color=#ffdf72>" );
                            //we use simFrameMultiplier to get the actual number per second, rather than the number per frame
                            buffer.AddNumberMoreReadable( Mathf.RoundToInt( systemStats.TachyonPoints * simFrameMultiplier ) );
                            buffer.Add( "</color> cloaking points per second from enemy ships with " );

                            WriteMinMaxOrVariant( buffer, "any albedo", "an albedo", true, "ffdf72", systemData.TachyonHitsAlbedoMoreThan, systemData.TachyonHitsAlbedoLessThan );
                        }
                        if ( systemData.CareAboutStateOfMatterToBeEnabled )
                            WriteSystemStateOfMatterSuffix( buffer, systemData, relatedSquadOrNull );
                    }
                    #endregion Tachyon System
                    #region Cloaking System
                    else if ( systemStats.CloakingPoints > 0 )
                    {
                        FInt maxWeaponCloakingReductionCost = FInt.Zero;
                        for ( int i = 0; i < systemData.ParentEntityTypeData.SystemTypes.Count; i++ )
                        {
                            EntitySystemTypeData otherSystem = systemData.ParentEntityTypeData.SystemTypes[i];
                            if ( otherSystem.MaxMarkLevelToFunction < effectiveMarkLevel || otherSystem.MinMarkLevelToFunction > effectiveMarkLevel || otherSystem.SystemIsHiddenForUI )
                                continue;
                            if ( otherSystem.CareAboutStateOfMatterToBeEnabled && otherSystem.IsInvisibleInTooltipsIfNotAMatchByStateOfMatter )
                            {
                                if ( relatedSquadOrNull != null && relatedSquadOrNull.CurrentStateOfMatter != otherSystem.MustBeThisStateOfMatterToBeEnabled )
                                    continue; //skip completely, since this will be invisible AND disabled
                            }
                            if ( otherSystem.IsModule && !otherSystem.IsModuleOn( relatedMembershipOrNull ) )
                                continue;
                            if ( otherSystem.Category == EntitySystemCategory.Weapon )
                            {
                                if ( maxWeaponCloakingReductionCost < otherSystem.CloakingPercentLossFromFiring )
                                    maxWeaponCloakingReductionCost = otherSystem.CloakingPercentLossFromFiring;
                            }
                        }

                        Color cloakColor = ColorMath.PaleVioletRed;
                        if ( detailLevel < TooltipDetail.Full )
                        {
                            buffer.Add( "最大隐形点数：<color=#ffdf72>" );
                            buffer.AddNumberMoreReadable( systemStats.CloakingPoints );
                            buffer.Add( "</color>.  " );
                        }
                        else
                        {
                            buffer.Add( "隐形", cloakColor ).Add( "：此舰船有 <color=#ffdf72>" );
                            buffer.AddNumberMoreReadable( systemStats.CloakingPoints );
                            buffer.Add( "</color> 最大隐形点数。只要当前隐形点数大于零，就会对敌人隐形（但会被探测到）。" );
                            if ( systemData.ParentEntityTypeData.IsCombatant )
                                buffer.Add( "此舰船每次开火将消耗 " )
                                    .Add( Mathf.RoundToInt( maxWeaponCloakingReductionCost.ToFloatNonSim() * 100f ) )
                                    .Add( "% 的隐形点数。" );
                            buffer.Add( "在 " ).Add( ExternalConstants.Instance.SecondsToWaitBeforeRecloaking ).Add( " 秒未损失隐形点数后，此舰船将恢复所有损失的点数。" );
                        }
                        if ( systemData.CareAboutStateOfMatterToBeEnabled )
                            WriteSystemStateOfMatterSuffix( buffer, systemData, relatedSquadOrNull );
                    }
                    #endregion Cloaking System
                    #region Tractor System
                    else if ( systemStats.TractorCount > 0 )
                    {
                        if ( systemData.IsReverseTractorBeam )
                        {
                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#ffdf72>" );
                                buffer.Add( systemStats.TractorCount );
                                buffer.Add( "</color>x <color=#f25e1c>反向牵引光束</color>: range <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.TractorRange );
                                buffer.Add( "</color>" );
                                WriteTractorRangeInfo( buffer, systemData, false );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>反向牵引光束</color>: A maximum of <color=#ffdf72>" );
                                buffer.Add( systemStats.TractorCount );
                                buffer.Add( "</color> enemy units can be grappled, pulling this unit with them, if they are within <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.TractorRange );
                                buffer.Add( "</color> range" );
                                WriteTractorRangeInfo( buffer, systemData, true );
                                buffer.Add( "它们仍可自由移动，拖着此单位，但无法离开当前星球。" );
                            }
                        }
                        else
                        {
                            if ( detailLevel < TooltipDetail.Full )
                            {
                                buffer.Add( "<color=#ffdf72>" );
                                buffer.Add( systemStats.TractorCount );
                                buffer.Add( "</color>x <color=#f25e1c>牵引光束</color>: range <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.TractorRange );
                                buffer.Add( "</color>" );
                                WriteTractorRangeInfo( buffer, systemData, false );
                            }
                            else
                            {
                                buffer.Add( "<color=#f25e1c>牵引光束</color>: A maximum of <color=#ffdf72>" );
                                buffer.Add( systemStats.TractorCount );
                                buffer.Add( "</color> enemy squads can be frozen in place if they are within <color=#ffdf72>" );
                                buffer.AddNumberMoreReadable( systemStats.TractorRange );
                                buffer.Add( "</color> range" );
                                WriteTractorRangeInfo( buffer, systemData, true );
                            }
                        }
                        if ( systemData.CareAboutStateOfMatterToBeEnabled )
                            WriteSystemStateOfMatterSuffix( buffer, systemData, relatedSquadOrNull );
                    }
                    #endregion Tractor System
                    #region Gravity System
                    else if ( systemData.GravityHitsEngine_gxLessThan > 0 )
                    {
                        buffer.Add( "<color=#f25e1c>重力场</color>: ");
                        if ( detailLevel < TooltipDetail.Full )
                        {
                            if (systemStats.GravityRange > 99999) {
                                buffer.Add("无限范围" );
                            } else {
                                buffer.Add("范围 " );
                                buffer.AddNumberMoreReadable( systemStats.GravityRange, "ffdf72" );
                            }
                            buffer.Add( ", slows to <color=#ffdf72>" );
                            buffer.Add( systemStats.GravitySpeedMultiplier.ReadableString );
                            buffer.Add( "x</color>, only target engines < <color=#ffdf72>" );
                            buffer.Add( systemData.GravityHitsEngine_gxLessThan );
                            buffer.Add( " gx</color>.  " );
                        }
                        else
                        {
                            if (systemStats.GravityRange > 99999) {
                                buffer.Add( "星球上所有敌方小队" );
                            } else {
                                buffer.Add( "范围内所有敌方小队 " );
                                buffer.AddNumberMoreReadable( systemStats.GravityRange, "ffdf72" );
                            }
                            buffer.Add( " are slowed to <color=#ffdf72>" );
                            buffer.Add( systemStats.GravitySpeedMultiplier.ReadableString );
                            buffer.Add( "x</color> their normal speed if they have an engine power less than <color=#ffdf72>" );
                            buffer.Add( systemData.GravityHitsEngine_gxLessThan );
                            buffer.Add( " gx</color>.  " );
                        }
                        if ( systemData.CareAboutStateOfMatterToBeEnabled )
                            WriteSystemStateOfMatterSuffix( buffer, systemData, relatedSquadOrNull );
                    }
                    #endregion Gravity System
                    #region Attractant System
                    else if ( systemStats.AttractRangeForShotsAgainstAllies > 0 )
                    {
                        buffer.Add( "<color=#f25e1c>吸引场</color>: All shots fired against friendly units within range <color=#ffdf72>" );
                        buffer.AddNumberMoreReadable( systemStats.AttractRangeForShotsAgainstAllies );
                        buffer.Add( "</color> are automatically redirected at this unit, instead.  " );
                        if ( systemData.CareAboutStateOfMatterToBeEnabled )
                            WriteSystemStateOfMatterSuffix( buffer, systemData, relatedSquadOrNull );
                    }
                    #endregion Attractant System
                }
            }
            catch ( Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in WriteSystemInfo at debugStage: " + debugStage  + "\n Exception: " + e, Verbosity.ShowAsError );
            }
        } //end WriteSystemInfo
        #endregion WriteSystemInfo

        #region WriteDamageModifierData
        public static void WriteDamageModifierData( ArcenCharacterBufferBase buffer, byte markLevel, DamageModifier Modifier, EntitySystemTypeData.MarkLevelStats SystemStatsOrNull )
        {
            FInt multiplier = Modifier.MultiplierForMark[markLevel];
            TooltipDetail detailLevel = EntityText.Detail;
            // The normal style of damage bonus.
            if ( Modifier.ComparisonType != DamageModifierComparisonType.MultiplesOf )
            {
                bool limitedToBubble = false;
                // Puffin Note: Abbreviated normal! Puffin Note 1
                if ( detailLevel < TooltipDetail.Full )
                {
                    if ( Modifier.IsForOutgoingDamage )
                    {
                        if ( multiplier >= FInt.One )
                        {
                            if ( Modifier.ComparisonRefersToMyself )
                                buffer.Add( "<color=#f25e1c>攻击加成</color>: If  " );
                            else
                                buffer.Add( "<color=#f25e1c>攻击加成</color>: If target " );
                        } else
                        {
                            if ( Modifier.ComparisonRefersToMyself )
                                buffer.Add( "<color=#f25e1c>攻击减益</color>: If " );
                            else
                                buffer.Add( "<color=#f25e1c>攻击减益</color>: If target " );
                        }
                    } else
                    {
                        if ( multiplier < FInt.One )
                        {
                            if ( Modifier.ComparisonRefersToMyself )
                                buffer.Add( "<color=#f25e1c>防御加成</color>: If " );
                            else
                                buffer.Add( "<color=#f25e1c>防御加成</color>: If target " );
                        } else
                        {
                            if ( Modifier.ComparisonRefersToMyself )
                                buffer.Add( "<color=#f25e1c>防御减益</color>: If " );
                            else
                                buffer.Add( "<color=#f25e1c>防御减益</color>: If attacker " );
                        }

                    }
                    switch ( Modifier.BasedOn )
                    {
                        case DamageModifierBasedOn.MaxHull:
                            buffer.Add( "最大船体生命值" );
                            break;
                        case DamageModifierBasedOn.CurrentHullPercentage:
                        case DamageModifierBasedOn.MyCurrentHullPercentage:
                            buffer.Add( "当前船体生命值" );
                            break;
                        case DamageModifierBasedOn.MaxPersonalShield:
                            buffer.Add( "最大个人护盾" );
                            break;
                        case DamageModifierBasedOn.TargetShieldPercentageMissing:
                        case DamageModifierBasedOn.MyShieldPercentageMissing:
                            buffer.Add( "当前缺失的个人护盾" );
                            break;
                        case DamageModifierBasedOn.TargetHullPercentageMissing:
                        case DamageModifierBasedOn.MyHullPercentageMissing:
                            buffer.Add( "当前缺失的船体生命值" );
                            break;
                        case DamageModifierBasedOn.MaxBubbleForcefield:
                            buffer.Add( "最大气泡力场强度" );
                            limitedToBubble = true;
                            break;
                        case DamageModifierBasedOn.CurrentPersonalShieldPercentage:
                        case DamageModifierBasedOn.MyCurrentPersonalShieldPercentage:
                            buffer.Add( "当前个人护盾" );
                            break;
                        case DamageModifierBasedOn.AttackDistance:
                            buffer.Add( "是" );
                            break;
                        case DamageModifierBasedOn.CurrentSpeedIfMoving:
                            buffer.Add( "当前速度" );
                            break;
                        case DamageModifierBasedOn.Armor_mm:
                            buffer.Add( "护甲" );
                            break;
                        case DamageModifierBasedOn.EnergyUsage:
                            buffer.Add( "能量消耗" );
                            break;
                        case DamageModifierBasedOn.Albedo:
                            buffer.Add( "反照率" );
                            break;
                        case DamageModifierBasedOn.Mass_tX:
                            buffer.Add( "质量" );
                            break;
                        case DamageModifierBasedOn.Engine_gx:
                            buffer.Add( "引擎动力" );
                            break;
                        case DamageModifierBasedOn.TargetTimeAtPlanet:
                            buffer.Add( "已在此停留" );
                            break;
                        case DamageModifierBasedOn.MyTimeAtPlanet:
                            buffer.Add( "已在此停留" );
                            break;
                        case DamageModifierBasedOn.FactionNetEnergy:
                            buffer.Add( " have " );
                            break;
                        case DamageModifierBasedOn.TargetMobility:
                            buffer.Add( "类型" );
                            break;
                        case DamageModifierBasedOn.TargetIsDrone:
                            buffer.Add( "类型" );
                            break;

                        default:
                            buffer.Add( "Unknown DamageModifierBasedOn." ).Add( EnumNameCache.GetName( Modifier.BasedOn ) );
                            break;
                    }
                    buffer.Add( " " );
                    switch ( Modifier.ComparisonType )
                    {
                        case DamageModifierComparisonType.LessThan:
                            buffer.Add( "<" );
                            break;
                        case DamageModifierComparisonType.GreaterThan:
                            buffer.Add( ">" );
                            break;
                        case DamageModifierComparisonType.AtMost:
                            buffer.Add( "<=" );
                            break;
                        case DamageModifierComparisonType.AtLeast:
                            buffer.Add( ">=" );
                            break;
                        case DamageModifierComparisonType.MultiplesOf:
                            buffer.Add( "multiples of" );
                            break;
                        case DamageModifierComparisonType.Equals:
                            buffer.Add( "是" );
                            break;
                        default: break;
                    }
                    buffer.Add( " <color=#ffdf72>" );
                    if ( Modifier.BasedOn == DamageModifierBasedOn.TargetMobility)
                        buffer.Add( Modifier.ComparedToInt > 0 ? "Mobile" : "Stationary");
                    else if ( Modifier.BasedOn == DamageModifierBasedOn.TargetIsDrone)
                        buffer.Add( Modifier.ComparedToInt > 0 ? "Drone" : "Not Drone");
                    else if ( Modifier.UsesInt )
                        buffer.AddNumberMoreReadable( Modifier.ComparedToInt );
                    else
                        buffer.Add( Modifier.ComparedToFInt.ReadableString );
                    switch ( Modifier.BasedOn )
                    {
                        case DamageModifierBasedOn.MaxHull:
                        case DamageModifierBasedOn.MaxPersonalShield:
                        case DamageModifierBasedOn.MaxBubbleForcefield:
                        case DamageModifierBasedOn.TargetMobility:
                        case DamageModifierBasedOn.TargetIsDrone:
                            break;
                        case DamageModifierBasedOn.CurrentHullPercentage:
                        case DamageModifierBasedOn.CurrentPersonalShieldPercentage:
                        case DamageModifierBasedOn.MyCurrentHullPercentage:
                        case DamageModifierBasedOn.MyCurrentPersonalShieldPercentage:
                        case DamageModifierBasedOn.TargetShieldPercentageMissing:
                        case DamageModifierBasedOn.MyShieldPercentageMissing:
                        case DamageModifierBasedOn.TargetHullPercentageMissing:
                        case DamageModifierBasedOn.MyHullPercentageMissing:
                            buffer.Add( "%" );
                            break;
                        case DamageModifierBasedOn.AttackDistance:
                            buffer.Add( " away" );
                            break;
                        case DamageModifierBasedOn.CurrentSpeedIfMoving:
                            break;
                        case DamageModifierBasedOn.Armor_mm:
                            buffer.Add( "mm" );
                            break;
                        case DamageModifierBasedOn.EnergyUsage:
                            break;
                        case DamageModifierBasedOn.Albedo:
                            break;
                        case DamageModifierBasedOn.Mass_tX:
                            buffer.Add( " tX" );
                            break;
                        case DamageModifierBasedOn.Engine_gx:
                            buffer.Add( " gx" );
                            break;
                        case DamageModifierBasedOn.MyTimeAtPlanet:
                        case DamageModifierBasedOn.TargetTimeAtPlanet:
                            buffer.Add( " seconds" );
                            break;
                        case DamageModifierBasedOn.FactionNetEnergy:
                            buffer.Add( " net energy " );
                            break;
                        default:
                            buffer.Add( "Unknown DamageModifierBasedOn." ).Add( EnumNameCache.GetName( Modifier.BasedOn ) );
                            break;
                    }
                    buffer.Add( "</color>, " );
                    if ( Modifier.IsForOutgoingDamage )
                        buffer.Add( "<color=#ffdf72>" );
                    else
                        buffer.Add( "<color=#ffdf72>" );

                    buffer.Add( multiplier.ReadableString );

                    if ( Modifier.IsForOutgoingDamage )
                        buffer.Add( "倍</color> 伤害对目标" );
                    else
                        buffer.Add( "倍</color> 伤害对此" );

                    switch ( Modifier.BasedOn )
                    {
                        case DamageModifierBasedOn.FactionNetEnergy:
                            buffer.Add( " for each such multiple." );
                            break;
                        default:
                            break;
                    }
                    switch ( Modifier.AppliesTo )
                    {
                        case DamageModifierAppliesTo.Everything:
                            buffer.Add( ".  " );
                            break;
                        case DamageModifierAppliesTo.PersonalShieldOnly:
                            if ( limitedToBubble )
                                buffer.Add( "'s bubble-forcefield.  " );
                            else
                                buffer.Add( "'s personal shields.  " );
                            break;
                        case DamageModifierAppliesTo.HullOnly:
                            if ( SystemStatsOrNull != null && SystemStatsOrNull.PercentDamageBypassesPersonalShields > FInt.Zero )
                                buffer.Add( "'s hull.  " );
                            else
                                buffer.Add( "'s hull (assuming shields are down).  " );
                            break;
                        case DamageModifierAppliesTo.AllShields:
                            if ( limitedToBubble )
                                buffer.Add( "'s bubble-forcefield.  " );
                            else
                                buffer.Add( "'s shields (personal or bubble-forcefield).  " );
                            break;
                        default:
                            buffer.Add( "Unknown DamageModifierAppliesTo." ).Add( EnumNameCache.GetName( Modifier.AppliesTo ) );
                            break;
                    }

                } else // Puffin Note: Normal Non-Abbreviated! Puffin Note 2
                {
                    if ( Modifier.IsForOutgoingDamage )
                    {
                        if ( multiplier >= FInt.One )
                        {
                            if ( Modifier.ComparisonRefersToMyself )
                                buffer.Add( "<color=#f25e1c>攻击加成</color>: If this ship " );
                            else
                                buffer.Add( "<color=#f25e1c>攻击加成</color>: If the target " );
                        } else
                        {
                            if ( Modifier.ComparisonRefersToMyself )
                                buffer.Add( "<color=#f25e1c>攻击减益</color>: If this ship " );
                            else
                                buffer.Add( "<color=#f25e1c>攻击减益</color>: If the target " );
                        }
                    } else
                    {
                        if ( multiplier < FInt.One )
                        {
                            if ( Modifier.ComparisonRefersToMyself )
                                buffer.Add( "<color=#f25e1c>防御加成</color>: If this ship " );
                            else
                                buffer.Add( "<color=#f25e1c>防御加成</color>: If the target " );
                        } else
                        {
                            if ( Modifier.ComparisonRefersToMyself )
                                buffer.Add( "<color=#f25e1c>防御减益</color>: If this ship " );
                            else
                                buffer.Add( "<color=#f25e1c>防御减益</color>: If the attacker " );
                        }

                    }
                    switch ( Modifier.BasedOn )
                    {
                        case DamageModifierBasedOn.MaxHull:
                            buffer.Add( "最大船体生命值为" );
                            break;
                        case DamageModifierBasedOn.MyCurrentHullPercentage:
                        case DamageModifierBasedOn.CurrentHullPercentage:
                            buffer.Add( "当前船体生命值为" );
                            break;
                        case DamageModifierBasedOn.TargetShieldPercentageMissing:
                        case DamageModifierBasedOn.MyShieldPercentageMissing:
                            buffer.Add( "当前缺失的个人护盾为" );
                            break;
                        case DamageModifierBasedOn.TargetHullPercentageMissing:
                        case DamageModifierBasedOn.MyHullPercentageMissing:
                            buffer.Add( "当前缺失的船体生命值为" );
                            break;
                        case DamageModifierBasedOn.MaxPersonalShield:
                            buffer.Add( "最大个人护盾为" );
                            break;
                        case DamageModifierBasedOn.MaxBubbleForcefield:
                            buffer.Add( "最大气泡力场强度为" );
                            limitedToBubble = true;
                            break;
                        case DamageModifierBasedOn.MyCurrentPersonalShieldPercentage:
                        case DamageModifierBasedOn.CurrentPersonalShieldPercentage:
                            buffer.Add( "当前个人护盾为" );
                            break;
                        case DamageModifierBasedOn.AttackDistance:
                            buffer.Add( "距离为" );
                            break;
                        case DamageModifierBasedOn.CurrentSpeedIfMoving:
                            buffer.Add( "当前移动速度为" );
                            break;
                        case DamageModifierBasedOn.Armor_mm:
                            buffer.Add( "护甲为" );
                            break;
                        case DamageModifierBasedOn.EnergyUsage:
                            buffer.Add( "能量消耗为" );
                            break;
                        case DamageModifierBasedOn.Albedo:
                            buffer.Add( "反照率为" );
                            break;
                        case DamageModifierBasedOn.Mass_tX:
                            buffer.Add( "质量为" );
                            break;
                        case DamageModifierBasedOn.Engine_gx:
                            buffer.Add( "引擎动力为" );
                            break;
                        case DamageModifierBasedOn.TargetTimeAtPlanet:
                            buffer.Add( "在此星球停留了" );
                            break;
                        case DamageModifierBasedOn.MyTimeAtPlanet:
                            buffer.Add( "在此星球停留了" );
                            break;
                        case DamageModifierBasedOn.FactionNetEnergy:
                            buffer.Add( " 拥有 " );
                            break;
                        case DamageModifierBasedOn.TargetMobility:
                            buffer.Add( "类型" );
                            break;
                        case DamageModifierBasedOn.TargetIsDrone:
                            buffer.Add( "类型" );
                            break;

                        default:
                            buffer.Add( "Unknown DamageModifierBasedOn." ).Add( EnumNameCache.GetName( Modifier.BasedOn ) );
                            break;
                    }
                    buffer.Add( " " );
                    switch ( Modifier.ComparisonType )
                    {
                        case DamageModifierComparisonType.LessThan:
                            buffer.Add( "小于" );
                            break;
                        case DamageModifierComparisonType.GreaterThan:
                            buffer.Add( "大于" );
                            break;
                        case DamageModifierComparisonType.AtMost:
                            buffer.Add( "最多" );
                            break;
                        case DamageModifierComparisonType.AtLeast:
                            buffer.Add( "至少" );
                            break;
                        case DamageModifierComparisonType.MultiplesOf:
                            buffer.Add( "倍数" );
                            break;
                        case DamageModifierComparisonType.Equals:
                            buffer.Add("是");
                            break;
                        default: break;
                    }
                    buffer.Add( " <color=#ffdf72>" );
                    if ( Modifier.BasedOn == DamageModifierBasedOn.TargetMobility)
                        buffer.Add( Modifier.ComparedToInt > 0 ? "Mobile" : "Stationary");
                    else if ( Modifier.BasedOn == DamageModifierBasedOn.TargetIsDrone)
                        buffer.Add( Modifier.ComparedToInt > 0 ? "Drone" : "Not Drone");
                    else if ( Modifier.UsesInt )
                        buffer.AddNumberMoreReadable( Modifier.ComparedToInt );
                    else
                        buffer.Add( Modifier.ComparedToFInt.ReadableString );
                    switch ( Modifier.BasedOn )
                    {
                        case DamageModifierBasedOn.MaxHull:
                        case DamageModifierBasedOn.MaxPersonalShield:
                        case DamageModifierBasedOn.MaxBubbleForcefield:
                        case DamageModifierBasedOn.TargetMobility:
                        case DamageModifierBasedOn.TargetIsDrone:
                            break;
                        case DamageModifierBasedOn.CurrentHullPercentage:
                        case DamageModifierBasedOn.CurrentPersonalShieldPercentage:
                        case DamageModifierBasedOn.MyCurrentHullPercentage:
                        case DamageModifierBasedOn.MyCurrentPersonalShieldPercentage:
                        case DamageModifierBasedOn.TargetShieldPercentageMissing:
                        case DamageModifierBasedOn.MyShieldPercentageMissing:
                        case DamageModifierBasedOn.TargetHullPercentageMissing:
                        case DamageModifierBasedOn.MyHullPercentageMissing:
                            buffer.Add( "%" );
                            break;
                        case DamageModifierBasedOn.AttackDistance:
                            buffer.Add( " away from this ship" );
                            break;
                        case DamageModifierBasedOn.CurrentSpeedIfMoving:
                            break;
                        case DamageModifierBasedOn.Armor_mm:
                            buffer.Add( "mm" );
                            break;
                        case DamageModifierBasedOn.EnergyUsage:
                            break;
                        case DamageModifierBasedOn.Albedo:
                            break;
                        case DamageModifierBasedOn.Mass_tX:
                            buffer.Add( " tX" );
                            break;
                        case DamageModifierBasedOn.Engine_gx:
                            buffer.Add( " gx" );
                            break;
                        case DamageModifierBasedOn.MyTimeAtPlanet:
                        case DamageModifierBasedOn.TargetTimeAtPlanet:
                            buffer.Add( " seconds" );
                            break;
                        case DamageModifierBasedOn.FactionNetEnergy:
                            buffer.Add( " net energy " );
                            break;
                        default:
                            buffer.Add( "Unknown DamageModifierBasedOn." ).Add( EnumNameCache.GetName( Modifier.BasedOn ) );
                            break;
                    }
                    buffer.Add( "</color>, then " );
                    if ( Modifier.IsForOutgoingDamage )
                        buffer.Add( "上述武器造成 <color=#ffdf72>" );
                    else
                        buffer.Add( "攻击者造成 <color=#ffdf72>" );

                    buffer.Add( multiplier.ReadableString );

                    if ( Modifier.IsForOutgoingDamage )
                        buffer.Add( "倍</color> 伤害对目标" );
                    else
                        buffer.Add( "倍</color> 伤害对此舰船" );

                    switch ( Modifier.BasedOn )
                    {
                        case DamageModifierBasedOn.FactionNetEnergy:
                            buffer.Add( " for each such multiple." );
                            break;
                        default:
                            break;
                    }
                    switch ( Modifier.AppliesTo )
                    {
                        case DamageModifierAppliesTo.Everything:
                            buffer.Add( ".  " );
                            break;
                        case DamageModifierAppliesTo.PersonalShieldOnly:
                            if ( limitedToBubble )
                                buffer.Add( "'s bubble-forcefield.  " );
                            else
                                buffer.Add( "'s personal shields.  " );
                            break;
                        case DamageModifierAppliesTo.HullOnly:
                            if ( SystemStatsOrNull != null && SystemStatsOrNull.PercentDamageBypassesPersonalShields > FInt.Zero )
                                buffer.Add( "'s hull.  " );
                            else
                                buffer.Add( "'s hull (assuming shields are down).  " );
                            break;
                        case DamageModifierAppliesTo.AllShields:
                            if ( limitedToBubble )
                                buffer.Add( "'s bubble-forcefield.  " );
                            else
                                buffer.Add( "'s shields (personal or bubble-forcefield).  " );
                            break;
                        default:
                            buffer.Add( "Unknown DamageModifierAppliesTo." ).Add( EnumNameCache.GetName( Modifier.AppliesTo ) );
                            break;
                    }

                }
            }
            // If the bonus is using MultiplesOf, as the normal style of tooltip makes little sense when applied to it.
            else
            {
                if ( detailLevel < TooltipDetail.Full ) // Abbreviated multiples of, Puffin Note 3
                {
                    if ( Modifier.IsForOutgoingDamage )
                    {
                        if ( multiplier > FInt.Zero )
                        {
                            if ( Modifier.ComparisonRefersToMyself )
                                buffer.Add( "<color=#f25e1c>攻击加成</color>: For every " );
                            else
                                buffer.Add( "<color=#f25e1c>攻击加成</color>: For every " );
                        } else
                        {
                            if ( Modifier.ComparisonRefersToMyself )
                                buffer.Add( "<color=#f25e1c>攻击减益</color>: For every " );
                            else
                                buffer.Add( "<color=#f25e1c>攻击减益</color>: For every " );
                        }
                    } else
                    {
                        if ( multiplier <= FInt.One )
                        {
                            if ( Modifier.ComparisonRefersToMyself )
                                buffer.Add( "<color=#f25e1c>防御加成</color>: For every " );
                            else
                                buffer.Add( "<color=#f25e1c>防御加成</color>: For every " );
                        } else
                        {
                            if ( Modifier.ComparisonRefersToMyself )
                                buffer.Add( "<color=#f25e1c>防御减益</color>: For every " );
                            else
                                buffer.Add( "<color=#f25e1c>防御减益</color>: For every " );
                        }

                    }
                    buffer.Add( " <color=#ffdf72>" );
                    if ( Modifier.UsesInt )
                        buffer.AddNumberMoreReadable( Modifier.ComparedToInt );
                    else
                        buffer.Add( Modifier.ComparedToFInt.ReadableString );
                    switch ( Modifier.BasedOn )
                    {
                        case DamageModifierBasedOn.MaxHull:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( "</color> maximum hull health of target" );
                            else
                                buffer.Add( "</color> maximum hull health of this" );
                            break;
                        case DamageModifierBasedOn.MaxPersonalShield:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( "</color> maximum personal shield health of target" );
                            else
                                buffer.Add( "</color> maximum personal shield health of this" );
                            break;
                        case DamageModifierBasedOn.MaxBubbleForcefield:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( "</color> maximum forcefield health of target" );
                            else
                                buffer.Add( "</color> maximum forcefield health this ship has" );
                            break;
                        case DamageModifierBasedOn.CurrentHullPercentage:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( "%</color> of target remaining hull" );
                            else
                                buffer.Add( "%</color> of remaining hull" );
                            break;
                        case DamageModifierBasedOn.CurrentPersonalShieldPercentage:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( "%</color> of target remaining personal shields" );
                            else
                                buffer.Add( "%</color> of remaining personal shields" );
                            break;
                        case DamageModifierBasedOn.MyCurrentHullPercentage:
                            buffer.Add( "%</color> of remaining hull" );
                            break;
                        case DamageModifierBasedOn.MyCurrentPersonalShieldPercentage:
                            buffer.Add( "%</color> of remaining personal shields" );
                            break;
                        case DamageModifierBasedOn.AttackDistance:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( " range</color> target is from this" );
                            else
                                buffer.Add( " range</color> this is from attacker" );
                            break;
                        case DamageModifierBasedOn.CurrentSpeedIfMoving:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( "</color> current speed of target" );
                            else
                                buffer.Add( "</color> current speed of this" );
                            break;
                        case DamageModifierBasedOn.Armor_mm:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( "毫米</color> 目标护甲" );
                            else
                                buffer.Add( "毫米</color> 此舰船护甲" );
                            break;
                        case DamageModifierBasedOn.EnergyUsage:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( " GW</color> of target energy usage" );
                            else
                                buffer.Add( " GW</color> of energy usage this has" );
                            break;
                        case DamageModifierBasedOn.Albedo:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( " albedo</color> of target" );
                            else
                                buffer.Add( " albedo</color> this has" );
                            break;
                        case DamageModifierBasedOn.Mass_tX:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( " tX</color> of target" );
                            else
                                buffer.Add( " tX</color> this has" );
                            break;
                        case DamageModifierBasedOn.Engine_gx:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( " gx</color> of target" );
                            else
                                buffer.Add( " gx</color> this has" );
                            break;
                        case DamageModifierBasedOn.MyTimeAtPlanet:
                            buffer.Add( " seconds</color> this has been here" );
                            break;
                        case DamageModifierBasedOn.TargetTimeAtPlanet:
                            buffer.Add( " seconds</color> target has been here" );
                            break;
                        case DamageModifierBasedOn.TargetHullPercentageMissing:
                            buffer.Add( "%</color> of hull target is missing" );
                            break;
                        case DamageModifierBasedOn.MyHullPercentageMissing:
                            buffer.Add( "%</color> of hull this is missing" );
                            break;
                        case DamageModifierBasedOn.TargetShieldPercentageMissing:
                            buffer.Add( "%</color> of shield target is missing" );
                            break;
                        case DamageModifierBasedOn.MyShieldPercentageMissing:
                            buffer.Add( "%</color> of shield this is missing" );
                            break;
                        case DamageModifierBasedOn.FactionNetEnergy:
                            buffer.Add( " net energy this ships faction has " );
                            break;
                        default:
                            buffer.Add( "Unknown DamageModifierBasedOn." ).Add( EnumNameCache.GetName( Modifier.BasedOn ) );
                            break;
                    }

                    if ( Modifier.IsForOutgoingDamage )
                        buffer.Add( " , do <color=#ffdf72>" );
                    else
                        buffer.Add( " , attacker does <color=#ffdf72>" );



                    buffer.Add( multiplier.ReadableString );

                    if ( Modifier.IsForOutgoingDamage )
                        if ( Modifier.MultiplierIsAdditive )
                            buffer.Add( "倍</color> 额外伤害对目标" );
                        else
                            buffer.Add( "倍</color> 伤害对目标" );
                    else
                        if ( Modifier.MultiplierIsAdditive )
                        buffer.Add( "倍</color> 额外伤害对此舰船" );
                    else
                        buffer.Add( "倍</color> 伤害对此舰船" );

                    switch ( Modifier.AppliesTo )
                    {
                        case DamageModifierAppliesTo.Everything:
                            break;
                        case DamageModifierAppliesTo.PersonalShieldOnly:
                            buffer.Add( "'s personal shields" );
                            break;
                        case DamageModifierAppliesTo.HullOnly:
                            if ( SystemStatsOrNull != null && SystemStatsOrNull.PercentDamageBypassesPersonalShields > FInt.Zero )
                                buffer.Add( "'s hull" );
                            else
                                buffer.Add( "'s hull (assuming shields are down)" );
                            break;
                        case DamageModifierAppliesTo.AllShields:
                            buffer.Add( "'s shields (personal or bubble-forcefield)" );
                            break;
                        default:
                            buffer.Add( "Unknown DamageModifierAppliesTo" ).Add( EnumNameCache.GetName( Modifier.AppliesTo ) );
                            break;
                    }

                    if ( Modifier.MaxMultiplier != int.MaxValue )//if it's not unlimited. Note that setting it to unlimited can be done by setting it to <= 0
                    {
                        if ( Modifier.MultiplierIsAdditive )
                            buffer.Add( ", but no more than extra <color=#ffdf72>" );
                        else
                            buffer.Add( ", but no more than <color=#ffdf72>" );
                        buffer.Add( Modifier.MaxMultiplier );
                        buffer.Add( "</color>x.  " );
                    } else
                        buffer.Add( ".  " );
                } else // Puffin Note 4 non abbreviated multiples of
                {
                    if ( Modifier.IsForOutgoingDamage )
                    {
                        if ( multiplier > FInt.Zero )
                        {
                            if ( Modifier.ComparisonRefersToMyself )
                                buffer.Add( "<color=#f25e1c>攻击加成</color>: For every " );
                            else
                                buffer.Add( "<color=#f25e1c>攻击加成</color>: For every " );
                        } else
                        {
                            if ( Modifier.ComparisonRefersToMyself )
                                buffer.Add( "<color=#f25e1c>攻击减益</color>: For every " );
                            else
                                buffer.Add( "<color=#f25e1c>攻击减益</color>: For every " );
                        }
                    } else
                    {
                        if ( multiplier <= FInt.One )
                        {
                            if ( Modifier.ComparisonRefersToMyself )
                                buffer.Add( "<color=#f25e1c>防御加成</color>: For every " );
                            else
                                buffer.Add( "<color=#f25e1c>防御加成</color>: For every " );
                        } else
                        {
                            if ( Modifier.ComparisonRefersToMyself )
                                buffer.Add( "<color=#f25e1c>防御减益</color>: For every " );
                            else
                                buffer.Add( "<color=#f25e1c>防御减益</color>: For every " );
                        }

                    }
                    buffer.Add( " <color=#ffdf72>" );
                    if ( Modifier.UsesInt )
                        buffer.AddNumberMoreReadable( Modifier.ComparedToInt );
                    else
                        buffer.Add( Modifier.ComparedToFInt.ReadableString );
                    switch ( Modifier.BasedOn )
                    {
                        case DamageModifierBasedOn.MaxHull:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( "</color> units of maximum hull health the target has" );
                            else
                                buffer.Add( "</color> units of maximum hull health this ship has" );
                            break;
                        case DamageModifierBasedOn.MaxPersonalShield:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( "</color> units of maximum personal shield health the target has" );
                            else
                                buffer.Add( "</color> units of maximum personal shield health this ship has" );
                            break;
                        case DamageModifierBasedOn.MaxBubbleForcefield:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( "</color> units of maximum forcefield health the target has" );
                            else
                                buffer.Add( "</color> units of maximum forcefield health this ship has" );
                            break;
                        case DamageModifierBasedOn.CurrentHullPercentage:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( "%</color> of hull health the target has remaining" );
                            else
                                buffer.Add( "%</color> of hull health this ship has remaining" );
                            break;
                        case DamageModifierBasedOn.CurrentPersonalShieldPercentage:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( "%</color> of personal shields the target has remaining" );
                            else
                                buffer.Add( "%</color> of personal shields this ship has remaining" );
                            break;
                        case DamageModifierBasedOn.MyCurrentHullPercentage:
                            buffer.Add( "%</color> of hull health this ship has remaining" );
                            break;
                        case DamageModifierBasedOn.MyCurrentPersonalShieldPercentage:
                            buffer.Add( "%</color> of personal shields this ship has remaining" );
                            break;
                        case DamageModifierBasedOn.AttackDistance:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( " range</color> the target is from this ship" );
                            else
                                buffer.Add( " range</color> this ship is from the attacker" );
                            break;
                        case DamageModifierBasedOn.CurrentSpeedIfMoving:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( "</color> speed units the target is currently moving" );
                            else
                                buffer.Add( "</color> speed units this ship is currently moving" );
                            break;
                        case DamageModifierBasedOn.Armor_mm:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( "毫米</color> 目标护甲" );
                            else
                                buffer.Add( "毫米</color> 此舰船护甲" );
                            break;
                        case DamageModifierBasedOn.EnergyUsage:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( " GW </color> of energy usage the target has" );
                            else
                                buffer.Add( " GW </color> of energy usage this ship has" );
                            break;
                        case DamageModifierBasedOn.Albedo:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( " albedo </color> the target has" );
                            else
                                buffer.Add( " albedo </color> this ship has" );
                            break;
                        case DamageModifierBasedOn.Mass_tX:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( " tX </color> the target has" );
                            else
                                buffer.Add( " tX </color> this ship has" );
                            break;
                        case DamageModifierBasedOn.Engine_gx:
                            if ( Modifier.IsForOutgoingDamage )
                                buffer.Add( " gx </color> the target has" );
                            else
                                buffer.Add( " gx</color> this ship has" );
                            break;
                        case DamageModifierBasedOn.MyTimeAtPlanet:
                            buffer.Add( " seconds </color> this unit has been at this planet" );
                            break;
                        case DamageModifierBasedOn.TargetTimeAtPlanet:
                            buffer.Add( " seconds </color> the target has been at this planet" );
                            break;
                        case DamageModifierBasedOn.TargetHullPercentageMissing:
                            buffer.Add( "% </color> of hull health the target is missing" );
                            break;
                        case DamageModifierBasedOn.MyHullPercentageMissing:
                            buffer.Add( "% </color> of hull health this ship is missing" );
                            break;
                        case DamageModifierBasedOn.TargetShieldPercentageMissing:
                            buffer.Add( "% </color> of shield health the target is missing" );
                            break;
                        case DamageModifierBasedOn.MyShieldPercentageMissing:
                            buffer.Add( "% </color> of shield health this ship is missing" );
                            break;
                        case DamageModifierBasedOn.FactionNetEnergy:
                            buffer.Add( " net energy this ships faction has " );
                            break;
                        default:
                            buffer.Add( "Unknown DamageModifierBasedOn." ).Add( EnumNameCache.GetName( Modifier.BasedOn ) );
                            break;
                    }

                    if ( Modifier.IsForOutgoingDamage )
                        buffer.Add( " 上述武器造成 <color=#ffdf72>" );
                    else
                        buffer.Add( " 攻击者造成 <color=#ffdf72>" );



                    buffer.Add( multiplier.ReadableString );

                    if ( Modifier.IsForOutgoingDamage )
                        if ( Modifier.MultiplierIsAdditive )
                            buffer.Add( "倍</color> 额外伤害对目标" );
                        else
                            buffer.Add( "倍</color> 伤害对目标" );
                    else
                        if ( Modifier.MultiplierIsAdditive )
                        buffer.Add( "倍</color> 额外伤害对此舰船" );
                    else
                        buffer.Add( "倍</color> 伤害对此舰船" );

                    switch ( Modifier.AppliesTo )
                    {
                        case DamageModifierAppliesTo.Everything:
                            break;
                        case DamageModifierAppliesTo.PersonalShieldOnly:
                            buffer.Add( "'s personal shields" );
                            break;
                        case DamageModifierAppliesTo.HullOnly:
                            if ( SystemStatsOrNull != null && SystemStatsOrNull.PercentDamageBypassesPersonalShields > FInt.Zero )
                                buffer.Add( "'s hull.  " );
                            else
                                buffer.Add( "'s hull (assuming shields are down)" );
                            break;
                        case DamageModifierAppliesTo.AllShields:
                            buffer.Add( "'s shields (personal or bubble-forcefield)" );
                            break;
                        default:
                            buffer.Add( "Unknown DamageModifierAppliesTo" ).Add( EnumNameCache.GetName( Modifier.AppliesTo ) );
                            break;
                    }

                    if ( Modifier.MaxMultiplier != int.MaxValue )//if it's not unlimited. Note that setting it to unlimited can be done by setting it to <= 0
                    {
                        if ( Modifier.MultiplierIsAdditive )
                            buffer.Add( ", but no more than extra <color=#ffdf72>" );
                        else
                            buffer.Add( ", but no more than <color=#ffdf72>" );
                        buffer.Add( Modifier.MaxMultiplier );
                        buffer.Add( "</color>x.  " );
                    } else
                        buffer.Add( ".  " );
                }
            }
        }
        #endregion
        
        private static void WriteLastAbortCode( ArcenCharacterBufferBase buffer, EntitySystem relatedSystem )
        {
            if ( relatedSystem.LastDamageAbortCode != 0 || relatedSystem.LastTotalDamageMyShotDidCaused == 0 )
            {
                buffer.NewLine().StartColor( QuickColors.OldValue ).Add( "DMG ABORT: " );
                switch ( (int)relatedSystem.LastDamageAbortCode )
                {
                    case 1:
                        buffer.Add( "Immune to All Damage" );
                        break;
                    case 2:
                        buffer.Add( "Newly-Created Immunity To Damage" );
                        break;
                    case 3:
                        buffer.Add( "External Invulnerability" );
                        break;
                    case 4:
                        buffer.Add( "foundProtectorButCouldNotHitDueToFiniteHitCountAOE" );
                        break;
                    case 5:
                        buffer.Add( "Debug_IgnoresDamage" );
                        break;
                    case 6:
                        buffer.Add( "HonorFiniteHitCountAOE and not in list" );
                        break;
                    case 7:
                        buffer.Add( "Calculated Zero Damage!" );
                        break;
                    case 8:
                        buffer.Add( "Damage-Drop-During-Hit" );
                        break;
                    case 9:
                        buffer.Add( "Health-Of-Target-Zero" );
                        break;
                    case 10:
                        buffer.Add( "Overdrives-Shields-No-Shields" );
                        break;
                    case 11:
                        buffer.Add( "Shooting Dead Target" );
                        break;
                    default:
                        buffer.Add( "CODE " ).Add( (int)relatedSystem.LastDamageAbortCode );
                        break;
                }
                buffer.EndColor().Add( "  " );
            }
        }

        private static void WriteCrippledInfo( ArcenCharacterBufferBase buffer, GameEntity_Squad relatedSquadOrNull )
        {
            buffer.StartColor( QuickColors.OldValue );
            if ( !relatedSquadOrNull.TypeData.ImmuneToRepairs )
            {
                FInt extraCost = relatedSquadOrNull.TypeData.GetExtraCostWhileCrippled();
                if ( extraCost > FInt.One )
                    buffer.Add( "重创 - 不会死亡，但需要修复（" ).AddFixedDecimal( extraCost.ToFloatNonSim(), 2 ).Add( "倍正常费用）至满血才能恢复功能！" );
                else
                    buffer.Add( "重创 - 不会死亡，但需要修复至满血才能恢复功能！" );
            } else
                buffer.Add( "重创 - 不会死亡，但需要修复至满血才能恢复功能！" );
            buffer.EndColor();
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

        private static bool WritePlannedMetalFlowFullTargetInfo( ArcenCharacterBufferBase buffer, PlannedMetalFlow flow, GameEntity_Squad entity, string Prefix, bool WriteFull )
        {
            if ( flow.FromEntity == null )
            {
                buffer.Add( "[null flow]" );
                return false;
            }
            GameEntity_Squad squad = flow.SquadRecipient;
            if ( Prefix != null && Prefix.Length > 0 )
                buffer.Add( Prefix ).Add( ": " );

            bool addedRecipient = false;
            if ( squad != null )
            {
                buffer.StartColor( squad.GetFactionCenterColorHexBrighter_Safe() );
                buffer.Add( WriteFull ? squad.TypeData.DisplayName : squad.FleetMembership?.GetDisplayNameForSidebar() ).EndColor();
                if ( WriteFull && squad.CurrentMarkLevel > 0 )
                {
                    buffer.Add( " " ).StartColor( squad.DataForMark.MarkLevel.ColorHex ).Add( squad.DataForMark.MarkLevel.Abbreviation ).EndColor();
                }
                if ( WriteFull && GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" ) )
                {
                    buffer.Add( " (dist " ).Add( flow.FromEntity.GetDistanceTo_ExpensiveAccurate( squad, RadiusCheck.SubtractRadiiFromDistance, true ) ).Add( ")" );
                }
                addedRecipient = true;
            }
            switch ( flow.Purpose )
            {
                case MetalFlowPurpose.FactoryConstructionForPlayerMobileFleets:
                    {
                        if ( entity == null )
                            break;
                        PlanetFaction pFaction = entity.PlanetFaction;
                        if ( pFaction == null )
                            break;
                        DoubleBufferedList<FleetMembership> factoryTargets = pFaction.FactoryBoostsBySpecialFactoryType.GetListForOrNull( entity.TypeData.SpecialFactoryType );
                        if ( factoryTargets == null || factoryTargets.Count == 0 )
                            break;
                        List<FleetMembership> factoryTargetsFinal = factoryTargets.GetDisplayList();
                        if ( factoryTargetsFinal == null || factoryTargetsFinal.Count == 0 )
                            break;

                        for ( int i = 0; i < factoryTargetsFinal.Count; i++ )
                        {
                            FleetMembership fMem = factoryTargetsFinal[i];
                            if ( i > 0 )
                                buffer.Add( ", " );
                            buffer.StartColor( fMem.Fleet.Faction.FactionCenterColor.ColorHexBrighter );
                            buffer.Add( WriteFull ? fMem.TypeData.DisplayName : fMem.GetDisplayNameForSidebar() ).EndColor();
                            addedRecipient = true;
                        }
                    }
                    break;
            }
            if ( !addedRecipient )
                buffer.Add( "[no recipients]" );
            return true;
        }

        private static void WriteTractorRangeInfo( ArcenCharacterBufferBase buffer, EntitySystemTypeData tractorSystem, bool WriteFull )
        {
            bool haveWrittenFirst = false;

            if ( WriteSingleStatPairInfo( buffer, tractorSystem.TractorHitsAlbedoLessThan, tractorSystem.TractorHitsAlbedoGreaterThan, FInt.One, WriteFull,
                WriteFull ? "an albedo" : "albedo", string.Empty, haveWrittenFirst ) )
                haveWrittenFirst = true;

            if ( WriteSingleStatPairInfo( buffer, (FInt) tractorSystem.TractorHitsEngine_gxLessThan, (FInt) tractorSystem.TractorHitsEngine_gxGreaterThan, (FInt) 999, WriteFull,
                WriteFull ? "an engine power" : "engine", " gx", haveWrittenFirst ) )
                haveWrittenFirst = true;

            if ( WriteSingleStatPairInfo( buffer, tractorSystem.TractorHitsMassLessThan, tractorSystem.TractorHitsMassGreaterThan, (FInt) 999, WriteFull,
                WriteFull ? "a mass" : "mass", " tX", haveWrittenFirst ) )
                haveWrittenFirst = true;

            buffer.Add( ".  " );
        }

        private static bool WriteSingleStatPairInfo( ArcenCharacterBufferBase buffer, FInt lessThanVal, FInt greaterThanVal, FInt OutOfRangeValue, bool WriteFull,
            string UnitPrefix, string UnitSuffix, bool WriteAndInsteadOfComma )
        {
            bool writeLessThan = lessThanVal > FInt.Zero && lessThanVal < OutOfRangeValue;
            bool writeGreaterThan = greaterThanVal > FInt.Zero && greaterThanVal < OutOfRangeValue;
            if ( !writeLessThan && !writeGreaterThan )
                return false;
            if ( WriteFull )
            {
                if ( WriteAndInsteadOfComma )
                    buffer.Add( " and " );
                else
                    buffer.Add( ", if they have " );
            } else
            {
                if ( WriteAndInsteadOfComma )
                    buffer.Add( " and " );
                else
                    buffer.Add( ", only target " );
            }
            buffer.Add( UnitPrefix );

            if ( writeLessThan )
            {
                if ( writeGreaterThan )
                {
                    if ( WriteFull )
                    {
                        buffer.Add( " between " );
                    } else
                    {

                    }
                    buffer.Add( "<color=#ffdf72>" );
                    buffer.AddFixedDecimal( greaterThanVal.ToFloatNonSim(), 2 );
                    buffer.Add( "-" );
                    buffer.AddFixedDecimal( lessThanVal.ToFloatNonSim(), 2 );
                    buffer.Add( UnitSuffix );
                    buffer.Add( "</color>" );
                } else //no range, just less than
                {
                    if ( WriteFull )
                    {
                        buffer.Add( " < " );

                    } else
                    {
                        buffer.Add( " less than " );
                    }
                    buffer.Add( "<color=#ffdf72>" );
                    buffer.AddFixedDecimal( lessThanVal.ToFloatNonSim(), 2 );
                    buffer.Add( UnitSuffix );
                    buffer.Add( "</color>" );
                }
            } else //no range, just greater than
            {
                if ( WriteFull )
                {
                    buffer.Add( " > " );
                } else
                {
                    buffer.Add( " greater than " );
                }
                buffer.Add( "<color=#ffdf72>" );
                buffer.AddFixedDecimal( greaterThanVal.ToFloatNonSim(), 2 );
                buffer.Add( UnitSuffix );
                buffer.Add( "</color>" );
            }
            return true;
        }

        private static string GetProtectorString( SpecialEntityType Type, string Tag, int Count )
        {
            bool shouldBePlural = Count > 1;
            string protectorString = "";
            if ( Type != SpecialEntityType.None )
            {
                switch ( Type )
                {
                    case SpecialEntityType.GuardPost:
                        if ( shouldBePlural )
                            protectorString = "guard posts";
                        else
                            protectorString = "guard post";
                        break;
                    case SpecialEntityType.DireGuardPost:
                        if ( shouldBePlural )
                            protectorString = "dire guard posts";
                        else
                            protectorString = "dire guard post";
                        break;
                    default:
                        if ( shouldBePlural )
                            protectorString = Type.ToString() + "s";
                        else
                            protectorString = Type.ToString();
                        break;
                }
            } else
            {
                if ( Tag == "AICommandStationOriginal" )
                {
                    protectorString = "AI Command Station";
                }
                else
                {
                    if ( shouldBePlural )
                        protectorString = Tag + "s";
                    else
                        protectorString = Tag;
                }
            }
            return protectorString;
        }

        private static void WriteSystemStateOfMatterSuffix( ArcenCharacterBufferBase buffer, EntitySystemTypeData systemData, GameEntity_Squad relatedSquadOrNull )
        {
            if ( systemData.CareAboutStateOfMatterToBeEnabled )
            {
                buffer.Add( "必须为 " ).Add( systemData.MustBeThisStateOfMatterToBeEnabled.DisplayName ).Add( " 物质状态才能运作。" );
                if ( relatedSquadOrNull != null && !systemData.IsInvisibleInTooltipsIfNotAMatchByStateOfMatter )
                {
                    if ( relatedSquadOrNull.CurrentStateOfMatter != systemData.MustBeThisStateOfMatterToBeEnabled )
                        buffer.Add( "(Disabled)" );
                    else
                        buffer.Add( "(Enabled)" );
                }
            }
        }

        private static void WriteTechThatBenefits( ArcenCharacterBufferBase buffer, TechUpgrade upgrade, Faction localFactionOrNull, bool alsoShowShipLineCountWithSameTech, ref int debugStage )
        {
            debugStage = 15010101;
            TooltipDetail detailLevel = EntityText.Detail;
            int upgradesSoFar = localFactionOrNull == null ? 0 : localFactionOrNull.TechUnlocks[upgrade.RowIndexNonSim] + localFactionOrNull.FreeTechUnlocks[upgrade.RowIndexNonSim];//okay to use here, as it will be consistent per run
            debugStage = 15010102;
            int upgradeIndex = upgradesSoFar < 0 ? 0 : upgradesSoFar + 1;
            if ( upgradeIndex < 0 )
                upgradeIndex = 0;
            if ( upgradeIndex >= Balance_MarkLevelTable.Instance.RowsByOrdinal.Length )
                upgradeIndex = Balance_MarkLevelTable.Instance.RowsByOrdinal.Length - 1;

            List<int> upgradeCosts = upgrade.GetScienceOrOtherResourceCostsPerTimeUnlocked();

            Balance_MarkLevel markByOrdinal = Balance_MarkLevelTable.Instance.RowsByOrdinal[upgradeIndex];
            debugStage = 15010103;
            buffer.Add( upgrade.DisplayName ).Add( markByOrdinal.ColorHexStart ).Add( " (" ).Add(
              upgradesSoFar ).Add( "/" ).Add( upgradeCosts.Count );
            if ( alsoShowShipLineCountWithSameTech && (upgrade.UIOnly_Tech_ShipLinesAffected > 0 || upgrade.UIOnly_Tech_DefensiveLinesAffected > 0) )
            {
                buffer.Add( " - " );
                if ( detailLevel < TooltipDetail.Medium )
                    buffer.Add( upgrade.UIOnly_Tech_ShipLinesAffected, ArcenExternalUIUtilities.ShipLineIncreaseColor ).Add( ", " ).Add( upgrade.UIOnly_Tech_DefensiveLinesAffected, ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add( "" );
                else if ( detailLevel < TooltipDetail.Medium )
                    buffer.Add( upgrade.UIOnly_Tech_ShipLinesAffected, ArcenExternalUIUtilities.ShipLineIncreaseColor ).Add( " lines, " ).Add( upgrade.UIOnly_Tech_DefensiveLinesAffected, ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add( " lines" );
                else
                    buffer.Add( upgrade.UIOnly_Tech_ShipLinesAffected, ArcenExternalUIUtilities.ShipLineIncreaseColor ).Add( " ship lines and " ).Add( upgrade.UIOnly_Tech_DefensiveLinesAffected, ArcenExternalUIUtilities.DefenseLineIncreaseColor ).Add( " defensive lines" );
            }
            buffer.Add( ")" ).EndColor();
        }

        private static readonly SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark> weakAgainst_Ships_ThatYouHave =
            SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark>.Create_WillNeverBeGCed( 30, delegate { return new ShipDataByMark(); }, "Window_InGameHoverEntityInfo-weakAgainst_Ships_ThatYouHave" );

        private static readonly SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataForSingleMark> weakAgainst_Ships_ThatYouCanCapture =
            SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataForSingleMark>.Create_WillNeverBeGCed( 30, delegate { return new ShipDataForSingleMark(); }, "Window_InGameHoverEntityInfo-weakAgainst_Ships_ThatYouCanCapture" );

        private static readonly SortedDictionary<GameEntityTypeData, RefThreeTuple<string, int, FInt>> weakAgainst_multipliersDealtByShipName =
            SortedDictionary<GameEntityTypeData, RefThreeTuple<string, int, FInt>>.Create_WillNeverBeGCed( 300, "Window_InGameHoverEntityInfo-weakAgainst_multipliersDealtByShipName" );

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

                ShipListerUtils.CalculateShipsThatYouHave( x => true, weakAgainst_Ships_ThatYouHave, false, true );
                debugStage = 300;
                ShipListerUtils.CalculateShipsThatYouCanCapture( x => true, weakAgainst_Ships_ThatYouCanCapture, null, false );

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
                    buffer.Add( "无" );

                debugStage = 3100;
                WriteMultipliersDealtByShipName( buffer, null, weakAgainst_multipliersDealtByShipName );
                buffer.Add( "\n" );

                debugStage = 3200;
                buffer.Add( "<color=#ffe450>\n这些你可以占领的舰船对此单位具有以下伤害倍率：</color>\n" );

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
                    buffer.Add( "无" );

                debugStage = 6100;
                WriteMultipliersDealtByShipName( buffer, null, weakAgainst_multipliersDealtByShipName );
                buffer.Add( "\n" );

                #endregion
            } catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "WriteWeakAgainst exception at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }
        }

        private static readonly SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark> strongAgainst_Ships_EnemiesHave =
            SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark>.Create_WillNeverBeGCed( 30, delegate { return new ShipDataByMark(); }, "Window_InGameHoverEntityInfo-strongAgainst_Ships_EnemiesHave" );

        private static readonly SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark> strongAgainst_Ships_EnemiesAtThisPlanet =
            SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark>.Create_WillNeverBeGCed( 30, delegate { return new ShipDataByMark(); }, "Window_InGameHoverEntityInfo-strongAgainst_Ships_EnemiesAtThisPlanet" );

        private static readonly SortedDictionary<GameEntityTypeData, RefThreeTuple<string, int, FInt>> strong_multipliersDealtByShipName =
            SortedDictionary<GameEntityTypeData, RefThreeTuple<string, int, FInt>>.Create_WillNeverBeGCed( 300, "Window_InGameHoverEntityInfo-strong_multipliersDealtByShipName" );

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
                buffer.Add( "无" );

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

        #region GetEntityContainsSomethingToSeeMoreOf
        public static string GetEntityContainsSomethingToSeeMoreOf( GameEntity_Squad squad )
        {
            //also update WriteDetailsOfAllShipContents with anything from here!!
            if ( squad == null )
                return null;
            //show TSS contents
            if ( squad.TypeData.HackableForCommandStationsAndBattleStations_DSSStyle )
            {
                if ( squad.ShipGrantsList != null && squad.ShipGrantsList.Count > 0 )
                    return "details of the defensive lines available here";
            }
            //show ARS contents
            if ( squad.TypeData.GrantsStuffToBeAddedToPlayerFleets )
            {
                if ( squad.ShipGrantsList != null && squad.ShipGrantsList.Count > 0 )
                    return "details of the ships available here";
            }
            if ( squad.TypeData.IsFleetLeader )
            {
                //show any fleets -- what they have in them
                //if ( squad.HasNotYetBeenFullyClaimed || squad.GetFactionTypeSafe() != FactionType.Player )
                return "details of the ships that are part of this fleet";
            }
            if ( squad.AIReinforcementPointContents != null && squad.AIReinforcementPointContents.Count > 0 )
            {
                //show ai reinforcement point contents
                return "AI reinforcement point contents";
            }
            if ( squad.TypeData.GetHasTag( OutguardBeaconStateForPlanet.OutguardBeaconTag ) )
            {
                //show Outguard unit details
                return "details for each Outguard group that you can contact from here";
            }
            return null;
        }
        #endregion

        #region WriteDetailsOfAnOutguardGroupContents
        public static bool WriteDetailsOfAnOutguardGroupContents( ArcenDoubleCharacterBuffer buffer, OutguardInfo OutguardInfo, bool WriteHeader, Faction Fac,
            float PositionScaleMultiplier, bool IsBeingDrawnInPopupWindowRatherThanTooltip )
        {
            if ( WriteHeader )
            {
                EntityText.Write_Tooltip_Hotkeys_Footer( buffer, false, true, null );
                buffer.Add( "\n\n" );
            }

            buffer.Add( "<u><b><size=17>" + OutguardInfo.GroupData.GetDisplayName() + " Outguard Group:</size></b></u>" );

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
                        return false;
                    unitBag.FillAllPossibleEntityTypes( unitData, World_AIW2.Instance.AIFactions[0] );

                    for ( int i = 0; i < unitData.OuterListCount; i++ )
                    {
                        buffer.AddSize_Large();
                        switch ( i )
                        {
                            case 0:
                                buffer.Add( "主要：" );
                                break;
                            case 1:
                                buffer.Add( "次要：" );
                                break;
                            case 2:
                                buffer.Add( "第三：" );
                                break;
                            case 3:
                                buffer.Add( "第四：" );
                                break;
                            case 4:
                                buffer.Add( "第五：" );
                                break;
                            case 5:
                                buffer.Add( "第六：" );
                                break;
                            case 6:
                                buffer.Add( "第七：" );
                                break;
                            case 7:
                                buffer.Add( "第八：" );
                                break;
                            case 8:
                                buffer.Add( "第九：" );
                                break;
                            case 9:
                                buffer.Add( "第十：" );
                                break;
                            default:
                                buffer.Add( "组 " ).Add( i - 1 ).Add( "：" );
                                break;
                        }
                        EntityTypeDrawingBag_SpawnMode spawnMode = unitBag.CountTypeList[i];
                        bool variableAmount = unitBag.SpawnValue_Min[i] != unitBag.SpawnValue_Max[i];
                        if ( variableAmount )
                        {
                            switch ( spawnMode )
                            {
                                case EntityTypeDrawingBag_SpawnMode.RawCount:
                                    buffer.Add( "介于 " ).AddNumberMoreReadable( unitBag.SpawnValue_Min[i] ).Add( " 和 " ).AddNumberMoreReadable( unitBag.SpawnValue_Max[i] ).Add( "x" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.AIBudget:
                                    buffer.Add( "介于 " ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " 和 " ).AddNumberTruncated( unitBag.SpawnValue_Max[i] )
                                        .Add( " AI budget worth" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.Strength_BaseMark:
                                    buffer.Add( "介于 " ).StartStrengthWrapper( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " 和 " )
                                        .AddNumberTruncated( unitBag.SpawnValue_Max[i] ).Add( " base strength (= at Mk1)" ).EndColor().Add( " worth of" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.Strength_CurrentMark:
                                    buffer.Add( "介于 " ).StartStrengthWrapper( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " 和 " )
                                        .AddNumberTruncated( unitBag.SpawnValue_Max[i] ).Add( " strength " ).EndColor().Add( " worth of" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.MetalCost:
                                    buffer.Add( "介于 " ).StartMetalWrapper( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " 和 " )
                                        .AddNumberTruncated( unitBag.SpawnValue_Max[i] ).Add( " metal " ).EndColor().Add( " worth of" );
                                    break;
                                case EntityTypeDrawingBag_SpawnMode.EnergyCost:
                                    buffer.Add( "介于 " ).StartEnergyWrapper( false ).AddNumberTruncated( unitBag.SpawnValue_Min[i] ).Add( " 和 " )
                                        .AddNumberTruncated( unitBag.SpawnValue_Max[i] ).Add( " energy " ).EndColor().Add( " worth of" );
                                    break;
                            }
                        } else
                        {
                            switch ( spawnMode )
                            {
                                case EntityTypeDrawingBag_SpawnMode.RawCount:
                                    buffer.AddNumberMoreReadable( unitBag.SpawnValue_Min[i] ).Add( "x" );
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
                            EntityText.GetTooltip( buffer, null, null, unitData[i][j], 0, null, Fac.GetGlobalMarkLevelForShipLine( unitData[i][j] ),
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
            return true;
        }
        #endregion

        #region WriteDetailsOfAllShipContents
        public static bool WriteDetailsOfAllShipContents( ArcenDoubleCharacterBuffer buffer, GameEntity_Squad squad, float PositionScaleMultiplier, bool IsBeingDrawnInPopupWindowRatherThanTooltip )
        {
            //also update GetEntityContainsSomethingToSeeMoreOf with anything from here!!
            EntityText.Write_Tooltip_Hotkeys_Footer( buffer, false, true, null );
            buffer.Add( "\n\n" );

            buffer.Add( "<u>主舰：</u>\n" );
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
                    buffer.Add( "<u>可窃取的防御线：</u>\n" );
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
            #endregion show DSS contents

            #region show ARS or FRS contents
            if ( squad.TypeData.GrantsStuffToBeAddedToPlayerFleets )
            {
                if ( squad.ShipGrantsList != null && squad.ShipGrantsList.Count > 0 )
                {
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    ShipLineEntry entry = null;
                    buffer.Add( "<u>可窃取的舰船：</u>\n" );
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
            #endregion show ARS or ARS contents

            if ( squad.TypeData.IsFleetLeader )
            {
                #region show any fleets -- what they have in them
                Fleet squadFleet = squad.GetFleetOrNull_Safe();
                //if ( squad.HasNotYetBeenFullyClaimed || squad.GetFactionTypeSafe() != FactionType.Player )
                if ( squadFleet != null )
                {
                    bool isForCapture = squadFleet.GetFactionType_Safe() == FactionType.NaturalObject;
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();

                    buffer.Add( "<u>舰队中的舰船线：</u>\n" );
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
                    buffer.Add( "<u>旗舰运输的舰船：</u>\n" );
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
                buffer.Add( "<u>此AI增援点包含的舰船：</u>\n" );
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
                    WriteDetailsOfAnOutguardGroupContents( buffer, World_AIW2.Instance.GetOutguardState( groupData ),
                        false, squad.PlanetFaction.Faction, PositionScaleMultiplier, IsBeingDrawnInPopupWindowRatherThanTooltip );
                }

                OutguardGroupData.ReleaseTemporaryOutguardGroupDataList( givenGroups );
            }
            #endregion

            return true;
        }
        #endregion

        public static void ShowDetailsOfAShipsContents( GameEntity_Squad squad )
        {
            if ( squad == null )
                return;

            if ( squad.TypeData.IsFleetLeader )
            {
                if ( squad.PlanetFaction != null && squad.GetIsLocalFaction_Safe() )
                {
                    //if we C-clicked the fleet leader of a squad we own, then open the fleet panel instead.
                    //Window_InGameSidebarBase.Current = InGameSidebarType.Fleets;
                    Window_FleetManagementSidebarPopout.Instance.Open( squad.GetFleetOrNull_Safe() );
                    return;
                }
            }

            float centerPopupScale = GameSettings.Current.GetFloatBySetting( "CentralPopupTextScale" );
            Window_ModalSelfUpdatingTextWindow_Wide.Instance.Open( 0.25f, 2f, "多舰船线路详情", "关闭",
                delegate ( ArcenDoubleCharacterBuffer Buffer ) { return WriteDetailsOfAllShipContents( Buffer, squad, centerPopupScale, true ); } );
        }

        public static void ShowDetailsOfAnOutguardGroupContents( OutguardInfo OutguardInfo )
        {
            if ( OutguardInfo == null || OutguardInfo.GroupData == null )
                return;

            Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();

            float centerPopupScale = GameSettings.Current.GetFloatBySetting( "CentralPopupTextScale" );
            Window_ModalSelfUpdatingTextWindow_Wide.Instance.Open( 0.25f, 2f, "外卫部队组详情", "关闭",
                delegate ( ArcenDoubleCharacterBuffer Buffer ) { return WriteDetailsOfAnOutguardGroupContents( Buffer, OutguardInfo, true, localFaction, centerPopupScale, true ); } );
        }

        public static void WriteMinMaxOrVariant( ArcenCharacterBufferBase buffer, string AnyName, string OnlyName, bool IsBrief, string Color, FInt min, FInt max )
        {
            if ( min <= FInt.Zero && max <= FInt.Zero )
            {
                buffer.Add( AnyName );
                buffer.Add( ".  " );
                return;
            } else
            {
                buffer.Add( OnlyName );
                if ( min > FInt.Zero )
                {
                    if ( IsBrief )
                        buffer.Add( " > " );
                    else
                        buffer.Add( " greater than " );
                    buffer.StartColor( Color );
                    buffer.AddFixedDecimal( min.ToFloatNonSim(), 2 );
                    buffer.EndColor();

                    if ( max > FInt.Zero )
                    {
                        if ( IsBrief )
                            buffer.Add( " and < " );
                        else
                            buffer.Add( " and less than " );
                        buffer.StartColor( Color );
                        buffer.AddFixedDecimal( max.ToFloatNonSim(), 2 );
                    }
                    buffer.Add( "</color>.  " );
                } else //no min
                {
                    if ( IsBrief )
                        buffer.Add( " < " );
                    else
                        buffer.Add( " less than " );
                    buffer.StartColor( Color );
                    buffer.AddFixedDecimal( max.ToFloatNonSim(), 2 );
                    buffer.Add( "</color>.  " );
                }
            }
        }

        public static void WriteBuildStatRow( ArcenCharacterBufferBase buffer, bool usesMetal, bool usesEnergy, CharacterPosInfo cpi, int forCount, int strengthCurr,
            int hullMax, int shieldMax, int metalCurr, int storedMetal, int storedEnergy, int energyCurr, ResourceType fuelType, int fuelCurr, int storedFuel, bool useIcons, bool useText )
        {
            buffer.NewLine();
            cpi.ResetPos();
            if ( useText )//for full tooltips we have to basically go small size all the way...
                buffer.AddSize_Small();
            buffer.Add( "<color=#dbef21>" ).Add( forCount ).Add( "</color> " );
            if ( forCount == 1 )
                buffer.Add( "单位:" );
            else
                buffer.Add( "单位:" );

            buffer.ToPos( cpi.AddStep_EightyPercent() );

            if ( useText )
                buffer.Add( "战斗力: " );
            else
                buffer.AddSize_Small();
            buffer.WrapStrengthTruncated( strengthCurr * forCount, useIcons, false );
            if ( useIcons )
                buffer.EndSize();

            cpi.AddStep_EightyPercent();
            if ( useText )
                cpi.AddStep_Half();
            buffer.ToPos( cpi );

            if ( useText )
                buffer.Add( "船体: " );
            else
                buffer.AddSize_Small();
            buffer.WrapHullTruncated( hullMax * forCount, useIcons, false );
            if ( useIcons )
                buffer.EndSize();

            cpi.AddStep_EightyPercent();
            if ( useText )
                cpi.AddStep_Third();
            buffer.ToPos( cpi );

            if ( useText )
                buffer.Add( "护盾: " );
            else
                buffer.AddSize_Small();
            buffer.WrapShieldTruncated( shieldMax * forCount, useIcons, false );
            if ( useIcons )
                buffer.EndSize();

            cpi.AddStep_EightyPercent();
            if ( useText )
                cpi.AddStep_FourtyPercent();
            buffer.ToPos( cpi );

            if ( usesMetal )
            {
                if ( useText )
                    buffer.Add( "金属: " );
                if ( metalCurr == 0 )
                {
                    buffer.StartMetalWrapper( useIcons );
                    if ( useIcons )
                        buffer.AddSize_Small();
                    buffer.Add( "-" );
                } else if ( storedMetal < metalCurr * forCount )
                {
                    buffer.StartMetalWrapper( useIcons, "ef3219" );
                    if ( useIcons )
                        buffer.AddSize_Small();
                    buffer.AddNumberTruncated( metalCurr * forCount );
                } else
                {
                    buffer.StartMetalWrapper( useIcons );
                    if ( useIcons )
                        buffer.AddSize_Small();
                    buffer.AddNumberTruncated( metalCurr * forCount );
                }
                if ( useText )
                    buffer.Add( " U" );
                else
                    buffer.EndSize();
                buffer.EndMetalWrapper( false );

                cpi.AddStep_EightyPercent();
                if ( useText )
                    cpi.AddStep_ThirtyPercent();
                buffer.ToPos( cpi );
            }

            if ( usesEnergy )
            {
                if ( useText )
                    buffer.Add( "能量: " );
                if ( energyCurr == 0 )
                {
                    buffer.StartEnergyWrapper( useIcons );
                    if ( useIcons )
                        buffer.AddSize_Small();
                    buffer.Add( "-" );
                } else if ( storedEnergy < energyCurr * forCount )
                {
                    buffer.StartEnergyWrapper( useIcons, "ef3219" );
                    if ( useIcons )
                        buffer.AddSize_Small();
                    buffer.AddNumberTruncated( energyCurr * forCount );
                } else
                {
                    buffer.StartEnergyWrapper( useIcons );
                    if ( useIcons )
                        buffer.AddSize_Small();
                    buffer.AddNumberTruncated( energyCurr * forCount );
                }
                if ( useText )
                    buffer.Add( " GW" );
                else
                    buffer.EndSize();
                buffer.EndEnergyWrapper( false );

                cpi.AddStep_EightyPercent();
                if ( useText )
                    cpi.AddStep_Half();
                buffer.ToPos( cpi );
            }

            if ( !World_AIW2.Instance.IsFuelEnabled || fuelCurr <= 0 )
            {
                if ( useText )
                    buffer.EndSize();
                return;
            }

            if ( useText )
            {
                if ( fuelType == ResourceType.FuelArgon )
                    buffer.Add( "氩气: " );
                else if ( fuelType == ResourceType.FuelRadon )
                    buffer.Add( "氡气: " );
                else if ( fuelType == ResourceType.FuelXenon )
                    buffer.Add( "氙气: " );
            }
            if ( fuelCurr == 0 )
            {
                if ( fuelType == ResourceType.FuelArgon )
                    buffer.StartArgonWrapper( useIcons );
                else if ( fuelType == ResourceType.FuelRadon )
                    buffer.StartRadonWrapper( useIcons );
                else if ( fuelType == ResourceType.FuelXenon )
                    buffer.StartXenonWrapper( useIcons );
                if ( useIcons )
                    buffer.AddSize_Small();
                buffer.Add( "-" );
            } else if ( storedFuel < fuelCurr * forCount )
            {
                if ( fuelType == ResourceType.FuelArgon )
                    buffer.StartArgonWrapper( useIcons, "ef3219" );
                else if ( fuelType == ResourceType.FuelRadon )
                    buffer.StartRadonWrapper( useIcons, "ef3219" );
                else if ( fuelType == ResourceType.FuelXenon )
                    buffer.StartXenonWrapper( useIcons, "ef3219" );
                if ( useIcons )
                    buffer.AddSize_Small();
                buffer.AddNumberTruncated( fuelCurr * forCount );
            } else
            {
                if ( fuelType == ResourceType.FuelArgon )
                    buffer.StartArgonWrapper( useIcons );
                else if ( fuelType == ResourceType.FuelRadon )
                    buffer.StartRadonWrapper( useIcons );
                else if ( fuelType == ResourceType.FuelXenon )
                    buffer.StartXenonWrapper( useIcons );
                if ( useIcons )
                    buffer.AddSize_Small();
                buffer.AddNumberTruncated( fuelCurr * forCount );
            }
            if ( fuelType == ResourceType.FuelArgon )
                buffer.EndArgonWrapper( false );
            else if ( fuelType == ResourceType.FuelRadon )
                buffer.EndRadonWrapper( false );
            else if ( fuelType == ResourceType.FuelXenon )
                buffer.EndXenonWrapper( false );

            buffer.EndSize();
        }

        public static void WriteHullOrShieldsNumber( ArcenCharacterBufferBase buffer, int Number )
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

        public static void AddSingleValueStrengthOnly( ArcenCharacterBufferBase buffer, int Strength )
        {
            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, Strength, true, true );
        }
        public static Color GetProportionalStrengthColor( float ratio )
        {
            //returns a color that indicates how much of the total available strength this unit has
            Color lowHealthFleet = ColorMath.FromRGB( 246, 50, 50 );
            Color highHealthFleet = ColorMath.FromRGB( 75, 244, 170 );
            return Color.Lerp( lowHealthFleet, highHealthFleet, ratio );
        }
    }

    public class Window_PrototypeInGameHoverEntityInfoUtils
    {
        protected static FInt currentPercentageTest = FInt.Zero;

        public static void WriteEntityOrder(GameEntity_Squad entity, EntityOrder order, ArcenCharacterBufferBase buffer)
        {
            switch(order.TypeData.Type)
            {
                case EntityOrderType.Assist:
                    buffer.Add( "协助 " );
                    WriteEntityForOrder( order.RelatedSquad.GetSquad(), buffer );
                    break;
                case EntityOrderType.Attack:
                    buffer.Add( "攻击 " );
                    WriteEntityForOrder( order.RelatedSquad.GetSquad(), buffer );
                    break;
                case EntityOrderType.GetIntoTransport:
                    buffer.Add( "装载入 " );
                    WriteEntityForOrder( order.RelatedSquad.GetSquad(), buffer );
                    break;
                case EntityOrderType.Unload_Transport:
                    buffer.Add( "卸载" );
                    break;
                case EntityOrderType.SetBehavior_Attacker_Full:
                    buffer.Add( "全部攻击" );
                    break;
                case EntityOrderType.SetBehavior_Attacker_PursueOnlyInRange:
                    buffer.Add( "攻击移动" );
                    break;
                case EntityOrderType.SetBehavior_Stationary:
                    buffer.Add( "停止移动" );
                    break;
                case EntityOrderType.SetBehavior_StopToShootAnySeenTargets_Off:
                    buffer.Add( "继续移动" );
                    break;
                case EntityOrderType.SetBehavior_StopToShootAnySeenTargets_On:
                    buffer.Add( "攻击目标" );
                    break;
                case EntityOrderType.Wormhole:
                    Int16 finalDestinationPlanetIndex = entity.CalculateFinalDestinationPlanetIndex_Safe();
                    if ( finalDestinationPlanetIndex == entity.CalculateNextHopPlanetIndex_Safe() )
                    {
                        Planet planet = World_AIW2.Instance.GetPlanetByIndex( order.RelatedPlanetIndex );
                        buffer.Add( "前往 " );
                        if ( planet == null )
                        {
                            buffer.Add( "未知星球" );
                        } else
                        {
                            buffer.AddPlanetNameFormated( planet, false );
                        }
                    } else
                    {
                        Planet planet1 = World_AIW2.Instance.GetPlanetByIndex( order.RelatedPlanetIndex );
                        Planet planet2 = World_AIW2.Instance.GetPlanetByIndex( finalDestinationPlanetIndex );
                        buffer.Add( "经由 " );
                        if ( planet1 == null )
                        {
                            buffer.Add( "未知星球" );
                        } else
                        {
                            buffer.AddPlanetNameFormated( planet1, false );
                        }
                        buffer.Add( " 到 " );
                        if ( planet2 == null )
                        {
                            buffer.Add( "未知星球" );
                        } else
                        {
                            buffer.AddPlanetNameFormated( planet2, false );
                        }
                    }
                    break;
                case EntityOrderType.Move_Decollision:
                    buffer.Add( "解除碰撞" );
                    break;
                case EntityOrderType.Move_Normal:
                    buffer.Add( "移动到 " ).Add(order.RelatedPoint.X - 400000).Add(" / ").Add(order.RelatedPoint.Y - 400000);
                    break;
                case EntityOrderType.Custom:
                    order.CustomOrder.GetText(buffer);
                    break;
            }
        }

        public static void WriteEntityForOrder( GameEntity_Squad entity, ArcenCharacterBufferBase buffer )
        {
            if ( entity == null )
            {
                buffer.Add( "未知目标" );
            } else
            {
                buffer.AddFactionColoredString( entity.TypeData.DisplayName, entity.GetFactionOrNull_Safe() );
            }
        }

        public static void WriteBuffsStartIfNeeded( ArcenCharacterBufferBase buffer, ref bool alreadyWroteBuffsStart )
        {
            if ( alreadyWroteBuffsStart )
            {
                buffer.Add( ", " );
            } else
            {
                buffer.NewLine().Add( "增益: ", "aaff22" ).AddSize_Small();
                alreadyWroteBuffsStart = true;
            }
        }

        public static void WriteHullBuffsStartIfNeeded( ArcenCharacterBufferBase buffer, bool useIcons, ref bool alreadyWroteBuffsStart, ref bool alreadyWroteHullBuffsStart )
        {
            if ( alreadyWroteHullBuffsStart )
            {
                buffer.Add( ", " );
            } else
            {
                WriteBuffsStartIfNeeded( buffer, ref alreadyWroteBuffsStart );
                buffer.WrapHull( "Hull: ", false, false );
                alreadyWroteHullBuffsStart = true;
            }
        }

        public static void WriteShieldBuffsStartIfNeeded( ArcenCharacterBufferBase buffer, bool useIcons, ref bool alreadyWroteBuffsStart, ref bool alreadyWroteShieldBuffsStart )
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

        public static void WriteDamageBuffsStartIfNeeded( ArcenCharacterBufferBase buffer, bool useIcons, ref bool alreadyWroteBuffsStart, ref bool alreadyWroteDamageBuffsStart )
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

        public static void WriteSpeedBuffsStartIfNeeded( ArcenCharacterBufferBase buffer, bool useIcons, ref bool alreadyWroteBuffsStart, ref bool alreadyWroteSpeedBuffsStart )
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

        public static void WriteRangeBuffsStartIfNeeded( ArcenCharacterBufferBase buffer, bool useIcons, ref bool alreadyWroteBuffsStart, ref bool alreadyWroteRangeBuffsStart )
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

        public static void WriteCloakBuffsStartIfNeeded( ArcenCharacterBufferBase buffer, bool useIcons, ref bool alreadyWroteBuffsStart, ref bool alreadyWroteCloakBuffsStart )
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



        public static void WriteDebuffsStartIfNeeded( ArcenCharacterBufferBase Debuffer, ref bool alreadyWroteDebuffsStart )
        {
            if ( alreadyWroteDebuffsStart )
            {
                Debuffer.Add( ", " );
            } else
            {
                Debuffer.NewLine().Add( "减益: ", "ff22aa" ).AddSize_Small();
                alreadyWroteDebuffsStart = true;
            }
        }

        public static void WriteHullDebuffsStartIfNeeded( ArcenCharacterBufferBase Debuffer, bool useIcons, ref bool alreadyWroteDebuffsStart, ref bool alreadyWroteHullDebuffsStart )
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

        public static void WriteShieldDebuffsStartIfNeeded( ArcenCharacterBufferBase Debuffer, bool useIcons, ref bool alreadyWroteDebuffsStart, ref bool alreadyWroteShieldDebuffsStart )
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

        public static void WriteDamageDebuffsStartIfNeeded( ArcenCharacterBufferBase Debuffer, bool useIcons, ref bool alreadyWroteDebuffsStart, ref bool alreadyWroteDamageDebuffsStart )
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

        public static void WriteSpeedDebuffsStartIfNeeded( ArcenCharacterBufferBase Debuffer, bool useIcons, ref bool alreadyWroteDebuffsStart, ref bool alreadyWroteSpeedDebuffsStart )
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

        public static void WriteDecloakDebuffsStartIfNeeded( ArcenCharacterBufferBase Debuffer, bool useIcons, bool isAlreadyDecloaked, ref bool alreadyWroteDebuffsStart, ref bool alreadyWroteCloakDebuffsStart )
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
        
        public static ShipClassData_ModifierData_Type LastGreaterCategoryType;
        public static ShipClassData_LesserCategoryType LastLesserCategoryType;

        public static void WriteShipClass_GreaterCategoryFinalEnd(ArcenCharacterBufferBase Buffer, bool AlreadyWroteStart )
        {
            if (!AlreadyWroteStart)
                return;
            Buffer.EndColor().EndSize();
        }

        public static void WriteShipClass_GreaterCategoryStartIfNeeded( ArcenCharacterBufferBase Buffer, ShipClassData ShipClass, ref bool AlreadyWroteStart,
            ref bool AlreadyWroteAbsoluteStart, TooltipDetail DetailLevel, ShipClassData_ModifierData_Type GreaterCategory )
        {
            WriteShipClass_AbsoluteStart( Buffer, ShipClass, DetailLevel, ref AlreadyWroteAbsoluteStart );

            if ( AlreadyWroteStart )
            {
                if (LastGreaterCategoryType != GreaterCategory)
                {
                    WriteShipClass_GreaterCategoryFinalEnd(Buffer, AlreadyWroteStart);
                    Buffer.Add("| ");
                } else
                    return;
            }
            else
                AlreadyWroteStart = true;

            LastGreaterCategoryType = GreaterCategory;

            if ( GreaterCategory == ShipClassData_ModifierData_Type.Vulnerability )
            {
                Buffer.StartColor( ArcenExternalUIUtilities.Debuffs.Color );
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( "弱点：" );
                else if ( DetailLevel == TooltipDetail.Medium )
                    Buffer.Add( "弱：" );
            }
            else if ( GreaterCategory == ShipClassData_ModifierData_Type.Resistance )
            {
                Buffer.StartColor( ArcenExternalUIUtilities.Buffs.Color );
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( "抗性：" );
                else if ( DetailLevel == TooltipDetail.Medium )
                    Buffer.Add( "抗：" );
            }
            else if ( GreaterCategory == ShipClassData_ModifierData_Type.Immunity )
            {
                Buffer.StartColor( ArcenExternalUIUtilities.Immunities.Color );
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( "免疫：" );
                else if ( DetailLevel == TooltipDetail.Medium )
                    Buffer.Add( "免：" );
            }
            else if ( GreaterCategory == ShipClassData_ModifierData_Type.Mixed )
            {
                Buffer.StartColor( ArcenExternalUIUtilities.Mixed.Color );
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( "其他：" );
                else if ( DetailLevel == TooltipDetail.Medium )
                    Buffer.Add( "他：" );
            }
            else
            {
                throw new Exception( "Error: Unimplemented ShipClassData_GreaterCategoryType: " + GreaterCategory );
            }
            Buffer.EndColor().AddSize_Small();
        }

        public static void WriteShipClass_LesserCategoryFinalEnd( ArcenCharacterBufferBase Buffer, bool AlreadyWroteStart )
        {
            if (!AlreadyWroteStart)
                return;
            Buffer.Add("</color> ");
        }

        public static void WriteShipClass_LesserCategoryStartIfNeeded( ArcenCharacterBufferBase Buffer, ref bool AlreadyWroteStart, TooltipDetail DetailLevel,
            ShipClassData_ModifierData_Type GreaterCategory, ShipClassData_LesserCategoryType LesserCategory, bool SkipLesserText )
        {
            if ( AlreadyWroteStart )
            {
                if (LastLesserCategoryType != LesserCategory)
                {
                    WriteShipClass_LesserCategoryFinalEnd(Buffer, AlreadyWroteStart);
                    Buffer.Add("- ");
                } else
                {
                    Buffer.Add("  ");
                    return;
                }
            }
            else
                AlreadyWroteStart = true;

            LastLesserCategoryType = LesserCategory;

            if ( GreaterCategory == ShipClassData_ModifierData_Type.Vulnerability )
                Buffer.StartColor( ArcenExternalUIUtilities.DebuffsLight.Color );
            else if( GreaterCategory == ShipClassData_ModifierData_Type.Mixed )
                Buffer.StartColor( ArcenExternalUIUtilities.MixedLight.Color );
            else if( GreaterCategory == ShipClassData_ModifierData_Type.Resistance )
                Buffer.StartColor( ArcenExternalUIUtilities.BuffsLight.Color );
            else
                Buffer.StartColor( ArcenExternalUIUtilities.ImmunitiesLight.Color );

            if ( DetailLevel <= TooltipDetail.Medium || SkipLesserText )
                return;

            if ( LesserCategory == ShipClassData_LesserCategoryType.Debuff )
            {
                Buffer.Add( "减益：" );
            } else if ( LesserCategory == ShipClassData_LesserCategoryType.DeathEffect )
            {
                Buffer.Add( "死亡效果：" );
            } else if ( LesserCategory == ShipClassData_LesserCategoryType.AmmoType )
            {
                Buffer.Add( "弹药类型：" );
            } else if ( LesserCategory == ShipClassData_LesserCategoryType.ExoticDamage )
            {
                Buffer.Add( "异种伤害：" );
            } else if ( LesserCategory == ShipClassData_LesserCategoryType.GeneralDamage )
            {
                Buffer.Add( "通用伤害：" );
            } else if ( LesserCategory == ShipClassData_LesserCategoryType.AllDamage )
            {
                //there is no "all damage" category, only total damage immunity. No need for an overhead
            } else if ( LesserCategory == ShipClassData_LesserCategoryType.SpecialMechanic )
            {
                Buffer.Add( "特殊机制：" );
            } else
            {
                throw new Exception( "Error: Unimplemented ShipClassData_LesserCategoryType: " + LesserCategory );
            }
        }

        public static void WriteShipClass_ModifierData( ArcenCharacterBufferBase Buffer, ShipClassData ShipClass, string ModifierNameLong, string ModifierNameShort,
            ShipClassData_ModifierData Data, ShipClassData_ModifiedUnit DataType, TooltipDetail DetailLevel, ref bool AlreadyWroteGreaterStart, ref bool AlreadyWroteAbsoluteStart,
            ShipClassData_ModifierData_Type GreaterCategory, ref bool AlreadyWroteLesserStart, ShipClassData_LesserCategoryType LesserCategory, bool SkipLesserText )
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
                        Buffer.Add( " 持续时间 " );
                    else
                        Buffer.Add( " 伤害 " );
                }
                else if ( DetailLevel == TooltipDetail.Medium )
                {
                    if ( DataType == ShipClassData_ModifiedUnit.TimeBased )
                        Buffer.Add( " 持续 " );
                    else
                        Buffer.Add( " 伤 " );
                }
            }

            if ( Data.Multiplier != FInt.One )
            {
                Buffer.Add( Data.Multiplier ).Add( "倍" );
                if ( Data.AddedCount != 0 )
                    Buffer.Add( ", " );
            }
            if ( Data.AddedCount != 0 )
            {
                if ( Data.AddedCount > 0 )
                    Buffer.Add( "+" );
                Buffer.Add( Data.AddedCount );
                if ( DataType == ShipClassData_ModifiedUnit.TimeBased )
                    Buffer.Add( "秒" );
            }
        }

        public static void WriteShipClass_GeneralCategory( ArcenCharacterBufferBase Buffer, ShipClassData ShipClass, int EntityBaseSpeed, TooltipDetail DetailLevel,
            ShipClassData_ModifierData_Type GreaterCategory, ref bool AlreadyWroteGeneralStart, ref bool AlreadyWroteAbsoluteStart )
        {
            bool AlreadyWroteLesserStart = false;
            ShipClassData_ModifierData data;

            if( !ShipClass.CanReceiveDebuffs && GreaterCategory == ShipClassData_ModifierData_Type.Immunity )
            {
                WriteShipClass_GreaterCategoryStartIfNeeded( Buffer, ShipClass, ref AlreadyWroteGeneralStart, ref AlreadyWroteAbsoluteStart, DetailLevel, GreaterCategory );
                WriteShipClass_LesserCategoryStartIfNeeded( Buffer, ref AlreadyWroteLesserStart, DetailLevel, GreaterCategory, ShipClassData_LesserCategoryType.Debuff, true );
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( "全部减益" );
                else
                    Buffer.Add( "减益" );
            } else if(ShipClass.HasAnyDebuffModifiers)
            {
                data = ShipClass.DebuffModifiers[0];
                if ( data.ModifierType == GreaterCategory && EntityBaseSpeed > 0 )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "引擎减速", "引减", data, ShipClassData_ModifiedUnit.TimeBased, DetailLevel, ref AlreadyWroteGeneralStart,
                        ref AlreadyWroteAbsoluteStart, GreaterCategory, ref AlreadyWroteLesserStart, ShipClassData_LesserCategoryType.Debuff, false );
                }
                data = ShipClass.DebuffModifiers[1];
                if ( data.ModifierType == GreaterCategory )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "武器减速", "武减", data, ShipClassData_ModifiedUnit.TimeBased, DetailLevel, ref AlreadyWroteGeneralStart,
                        ref AlreadyWroteAbsoluteStart, GreaterCategory, ref AlreadyWroteLesserStart, ShipClassData_LesserCategoryType.Debuff, false );
                }
                data = ShipClass.DebuffModifiers[2];
                if ( data.ModifierType == GreaterCategory )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "瘫痪", "晕眩", data, ShipClassData_ModifiedUnit.TimeBased, DetailLevel, ref AlreadyWroteGeneralStart,
                        ref AlreadyWroteAbsoluteStart, GreaterCategory, ref AlreadyWroteLesserStart, ShipClassData_LesserCategoryType.Debuff, false );
                }
                data = ShipClass.DebuffModifiers[3];
                if ( data.ModifierType == GreaterCategory )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "腐蚀", "腐蚀", data, ShipClassData_ModifiedUnit.TimeBased, DetailLevel, ref AlreadyWroteGeneralStart,
                        ref AlreadyWroteAbsoluteStart, GreaterCategory, ref AlreadyWroteLesserStart, ShipClassData_LesserCategoryType.Debuff, false );
                }
                data = ShipClass.DebuffModifiers[4];
                if ( data.ModifierType == GreaterCategory )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "武器相位", "相位", data, ShipClassData_ModifiedUnit.TimeBased, DetailLevel, ref AlreadyWroteGeneralStart,
                        ref AlreadyWroteAbsoluteStart, GreaterCategory, ref AlreadyWroteLesserStart, ShipClassData_LesserCategoryType.Debuff, false );
                }
                data = ShipClass.DebuffModifiers[5];
                if ( data.ModifierType == GreaterCategory && EntityBaseSpeed > 0 )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "击退", "击退", data, ShipClassData_ModifiedUnit.TimeBased, DetailLevel, ref AlreadyWroteGeneralStart,
                        ref AlreadyWroteAbsoluteStart, GreaterCategory, ref AlreadyWroteLesserStart, ShipClassData_LesserCategoryType.Debuff, false );
                }
                data = ShipClass.DebuffModifiers[6];
                if ( data.ModifierType == GreaterCategory )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "超光速粒子束", "粒子", data, ShipClassData_ModifiedUnit.TimeBased, DetailLevel, ref AlreadyWroteGeneralStart,
                        ref AlreadyWroteAbsoluteStart, GreaterCategory, ref AlreadyWroteLesserStart, ShipClassData_LesserCategoryType.Debuff, false );
                }
                data = ShipClass.GraviticCoreModifier;
                if ( data.ModifierType == GreaterCategory && EntityBaseSpeed > 0 )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "引力核心", "引力", data, ShipClassData_ModifiedUnit.None, DetailLevel, ref AlreadyWroteGeneralStart,
                        ref AlreadyWroteAbsoluteStart, GreaterCategory, ref AlreadyWroteLesserStart, ShipClassData_LesserCategoryType.Debuff, false );
                }
            }

            if ( !ShipClass.CanReceiveDeathEffects && GreaterCategory == ShipClassData_ModifierData_Type.Immunity )
            {
                WriteShipClass_GreaterCategoryStartIfNeeded( Buffer, ShipClass, ref AlreadyWroteGeneralStart, ref AlreadyWroteAbsoluteStart, DetailLevel, GreaterCategory );
                WriteShipClass_LesserCategoryStartIfNeeded( Buffer, ref AlreadyWroteLesserStart, DetailLevel, GreaterCategory, ShipClassData_LesserCategoryType.DeathEffect, true );
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( "全部死亡效果" );
                else
                    Buffer.Add( "死亡效果" );
            } 
            /* Since a *lot* of things have zombification immunity being all they use from the ShipClass this is not displayed here but in the main tooltip of the unit
             * Less visual clutter this way

            else if ( !ShipClass.CanBeZombifiedAndSimilar && GreaterCategory == ShipClassData_ModifierData_Type.Immunity )
            {
                //Do not individually list zombification-type death effects for immunity, just that one info is sufficient
                WriteShipClass_GreaterCategoryStartIfNeeded( Buffer, ref AlreadyWroteGeneralStart, DetailLevel, GreaterCategory );
                WriteShipClass_LesserCategoryStartIfNeeded( Buffer, ref AlreadyWroteLesserStart, DetailLevel, GreaterCategory, ShipClassData_LesserCategoryType.DeathEffect, false );
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( "全部僵尸化类型" );
                else
                    Buffer.Add( "僵尸化类型" );
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
                            ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory, ref AlreadyWroteLesserStart, ShipClassData_LesserCategoryType.DeathEffect, false );
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
                        Buffer.Add( "免疫所有伤害" );
                    else
                        Buffer.Add( "无敌" );
                }
            } else
            {
                if ( ShipClass.NonExoticDamageModifier.ModifierType == GreaterCategory )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "All Shots", "All", ShipClass.NonExoticDamageModifier, ShipClassData_ModifiedUnit.None, DetailLevel,
                        ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory, ref AlreadyWroteLesserStart, ShipClassData_LesserCategoryType.AmmoType, false );
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
                                ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory, ref AlreadyWroteLesserStart, ShipClassData_LesserCategoryType.AmmoType, false );
                        }
                    }
                }

                if ( ShipClass.AllExoticDamageModifier.ModifierType == GreaterCategory )
                {
                    WriteShipClass_ModifierData( Buffer, ShipClass, "全部异种伤害", "异种伤害", ShipClass.AllExoticDamageModifier, ShipClassData_ModifiedUnit.None,
                        DetailLevel, ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory, ref AlreadyWroteLesserStart,
                        ShipClassData_LesserCategoryType.ExoticDamage, true );
                } else if ( ShipClass.HasAnyExoticDamageModifiers )
                {
                    data = ShipClass.ExoticDamageModifiers[0];
                    if ( data.ModifierType == GreaterCategory && EntityBaseSpeed > 0 )
                    {
                        WriteShipClass_ModifierData( Buffer, ShipClass, "磨损", "磨损", data, ShipClassData_ModifiedUnit.None, DetailLevel,
                            ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory, ref AlreadyWroteLesserStart, ShipClassData_LesserCategoryType.ExoticDamage, false );
                    }
                    data = ShipClass.ExoticDamageModifiers[1];
                    if ( data.ModifierType == GreaterCategory )
                    {
                        WriteShipClass_ModifierData( Buffer, ShipClass, "电毒性", "电毒", data, ShipClassData_ModifiedUnit.None, DetailLevel,
                            ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory, ref AlreadyWroteLesserStart, ShipClassData_LesserCategoryType.ExoticDamage, false );
                    }
                    data = ShipClass.ExoticDamageModifiers[2];
                    if ( data.ModifierType == GreaterCategory )
                    {
                        WriteShipClass_ModifierData( Buffer, ShipClass, "复仇射击", "复仇", data, ShipClassData_ModifiedUnit.None, DetailLevel,
                            ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory, ref AlreadyWroteLesserStart, ShipClassData_LesserCategoryType.ExoticDamage, false );
                    }
                    data = ShipClass.ExoticDamageModifiers[3];
                    if ( data.ModifierType == GreaterCategory )
                    {
                        WriteShipClass_ModifierData( Buffer, ShipClass, "离子炮", "离子", data, ShipClassData_ModifiedUnit.None, DetailLevel,
                            ref AlreadyWroteAbsoluteStart, ref AlreadyWroteGeneralStart, GreaterCategory, ref AlreadyWroteLesserStart, ShipClassData_LesserCategoryType.ExoticDamage, false );
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
                        Buffer.Add( "牵引光束  黑洞机器  被吞噬  被感染" );
                    else
                        Buffer.Add( "全部特殊机制" );
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
                                Buffer.Add( "牵引光束 " );
                            else
                                Buffer.Add( "牵引 " );
                            if ( !ShipClass.CanBeBlackHoleMachineBlocked )
                                Buffer.Add( " " );
                        }
                        if ( !ShipClass.CanBeBlackHoleMachineBlocked )
                        {
                            if ( DetailLevel == TooltipDetail.Full )
                                Buffer.Add( "黑洞机器 " );
                            else
                                Buffer.Add( "黑洞 " );
                            if ( !ShipClass.CanBeDevoured )
                                Buffer.Add( " " );
                        }
                        if ( !ShipClass.CanBeDevoured )
                            if ( DetailLevel == TooltipDetail.Full )
                                Buffer.Add( "被吞噬 " );
                            else
                                Buffer.Add( "吞噬 " );
                        if ( !ShipClass.CanBeInfested )
                            if ( DetailLevel == TooltipDetail.Full )
                                Buffer.Add( "被感染 " );
                            else
                                Buffer.Add( "感染 " );
                    }
                }
            }

            WriteShipClass_LesserCategoryFinalEnd(Buffer, AlreadyWroteLesserStart);
        }

        public static void WriteShipClass_BuffLimitStartIfNeeded( ArcenCharacterBufferBase Buffer, ref bool AlreadyWroteStart,
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
                Buffer.StartColor( ArcenExternalUIUtilities.Limits.Color ).Add( "增益上限：" ).EndColor().StartColor( ArcenExternalUIUtilities.LimitsLight.Color ).AddSize_Small();
        }

        public static void WriteShipClass_BuffLimitEndIfNeeded( ArcenCharacterBufferBase Buffer, bool AlreadyWroteStart )
        {
            if ( AlreadyWroteStart )
                Buffer.EndColor();
        }

        public static void WriteShipClass_SuperchargingStartIfNeeded( ArcenCharacterBufferBase Buffer, ref bool AlreadyWroteStart, ref bool WrotePreviousStart, bool SkipInitialStart )
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
                Buffer.StartColor( ArcenExternalUIUtilities.Limits.Color ).Add( " 无法超载：" ).EndColor().StartColor( ArcenExternalUIUtilities.LimitsLight.Color ).AddSize_Small();
        }

        public static void WriteShipClass_BuffLimitOrSuperchargeEndIfNeeded( ArcenCharacterBufferBase Buffer, ref bool AlreadyWroteStart )
        {
            if ( !AlreadyWroteStart )
                return;
            Buffer.EndSize().EndColor();
        }

        public static void WriteShipClass_BuffLimitIfNeeded( ArcenCharacterBufferBase Buffer, ShipClassData ShipClass, ref bool AlreadyWroteAbsoluteStart, ref bool AlreadyWroteStart, TooltipDetail DetailLevel,
            ShipClassData_BuffType BuffType, FInt BuffLimit_Default, FInt BuffLimit_Current, int SpeedBuffLimit_Default = 0, int SpeedBuffLimit_Current = 0, int EntityBaseSpeed = 0)
        {
            if ( BuffLimit_Default == BuffLimit_Current && SpeedBuffLimit_Default == SpeedBuffLimit_Current )
                return;

            WriteShipClass_BuffLimitStartIfNeeded( Buffer, ref AlreadyWroteStart, ref AlreadyWroteAbsoluteStart, ShipClass, DetailLevel, false );
            if ( BuffType == ShipClassData_BuffType.Damage )
            {
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( " 伤害：" );
                else
                    Buffer.Add( " 伤 " );
                Buffer.AddFIntTruncated( BuffLimit_Current ).Add("x ");
            } else if( BuffType == ShipClassData_BuffType.Hull )
            {
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( " 船体：" );
                else
                    Buffer.Add( " Hull " );
                Buffer.AddFIntTruncated( BuffLimit_Current ).Add( "x " );
            } else if( BuffType == ShipClassData_BuffType.Shield )
            {
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( " 护盾：" );
                else
                    Buffer.Add( " 护盾 " );
                Buffer.AddFIntTruncated( BuffLimit_Current ).Add( "x " );
            } else if( BuffType == ShipClassData_BuffType.Speed )
            {
                if ( DetailLevel == TooltipDetail.Full )
                    Buffer.Add( " 速度：" );
                else
                    Buffer.Add( " 速度 " );
                Buffer.AddNumberMoreReadable( ShipClass.GetMaxSpeedForShip( EntityBaseSpeed ) );
            }
            WriteShipClass_BuffLimitEndIfNeeded( Buffer, AlreadyWroteStart );
        }

        public static void WriteShipClass_BuffLimitsAndSuperchargingSection( ArcenCharacterBufferBase Buffer, ShipClassData ShipClass, ref bool AlreadyWroteAbsoluteStart,
            TooltipDetail DetailLevel, FactionType ForFactionType, int EntityBaseSpeed )
        {

            bool alreadyWroteBuffLimitStart = false;
            if ( !ShipClass.CanBeBuffed )
            {
                WriteShipClass_BuffLimitStartIfNeeded( Buffer, ref alreadyWroteBuffLimitStart, ref AlreadyWroteAbsoluteStart, ShipClass, DetailLevel, true );
                Buffer.StartColor( ArcenExternalUIUtilities.Limits.Color ).Add( " 无法获得增益。 " ).EndColor();
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
                        Buffer.Add( " 伤害 " );
                    }
                    if ( ShipClass.HullBuff_MaxMultiplier > 1 && !ShipClass.CanBeSuperchargedByFleet( ShipClassData_BuffType.Hull ) )
                    {
                        WriteShipClass_SuperchargingStartIfNeeded( Buffer, ref alreadyWroteSuperchargeLimitStart, ref alreadyWroteBuffLimitStart, false );
                    Buffer.Add( " 船体 " );
                    }
                    if ( ShipClass.ShieldBuff_MaxMultiplier > 1 && !ShipClass.CanBeSuperchargedByFleet( ShipClassData_BuffType.Shield ) )
                    {
                        WriteShipClass_SuperchargingStartIfNeeded( Buffer, ref alreadyWroteSuperchargeLimitStart, ref alreadyWroteBuffLimitStart, false );
                        Buffer.Add( " 护盾 " );
                    }
                    if ( EntityBaseSpeed > 0 && (ShipClass.SpeedBuff_MaxMultiplier > 1 || ShipClass.SpeedBuff_BaseBonus > 0) &&
                        !ShipClass.CanBeSuperchargedByFleet( ShipClassData_BuffType.Speed ) )
                    {
                        WriteShipClass_SuperchargingStartIfNeeded( Buffer, ref alreadyWroteSuperchargeLimitStart, ref alreadyWroteBuffLimitStart, false );
                        Buffer.Add( " 速度 " );
                    }
                }
            }

            bool anyEndNeeded = alreadyWroteBuffLimitStart || alreadyWroteSuperchargeLimitStart;
            WriteShipClass_BuffLimitOrSuperchargeEndIfNeeded( Buffer, ref anyEndNeeded );
        }

        public static void WriteShipClass_AbsoluteStart( ArcenCharacterBufferBase Buffer, ShipClassData ShipClass, TooltipDetail DetailLevel, ref bool AlreadyWroteAbsoluteStart )
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

        public static void WriteShipClass_Complete( ArcenCharacterBufferBase Buffer, ShipClassData ShipClass, TooltipDetail DetailLevel, FactionType ForFactionType,
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

            WriteShipClass_GreaterCategoryFinalEnd( Buffer, AlreadyWroteGreaterStart );

            WriteShipClass_BuffLimitsAndSuperchargingSection( Buffer, ShipClass, ref AlreadyWroteAbsoluteStart, DetailLevel, ForFactionType, EntityBaseSpeed );
        }

        public static void WriteRangedSupportMetalFlowStartOrSeparator( ArcenCharacterBufferBase Buffer, GameEntityTypeData.MarkLevelStats markStats, ref bool WroteStart )
        {
            if ( WroteStart )
            {
                Buffer.Add( ", " );
                return;
            }
            WroteStart = true;
            Buffer.Add( "范围内 " ).WrapRangeMoreReadable( markStats.AssistRange, false, true ).Add( ": " );
        }

        public static PlannedMetalFlow GetMetalFlowForPurposeOrDefault( GameEntity_Squad squad, MetalFlowPurpose purpose )
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

        public static void WriteSupportMetalFlows(ArcenCharacterBufferBase buffer, GameEntity_Squad squadOrNull, GameEntityTypeData.MarkLevelStats markStats, TooltipDetail detailLevel, ref int debugStage )
        {
            if ( detailLevel < TooltipDetail.Medium || markStats == null )
                return;
            debugStage = 1000;
            EntityMetalFlowEntry metalFlow = markStats.GetMetalFlow( MetalFlowPurpose.FactoryConstructionForPlayerMobileFleets );
            if(metalFlow.HasRealData && metalFlow.EffectiveThroughput > FInt.Zero)
            {
                buffer.Add( "建造舰队单位（速度 <color=#ffdf72>" ).Add( metalFlow.EffectiveThroughput.ReadableString ).Add( "</color>）。" );
            }
            bool wroteRangeStart = false;
            for ( MetalFlowPurpose purpose = MetalFlowPurpose.None + 1; purpose < MetalFlowPurpose.Length; purpose++ )
            {
                if ( purpose == MetalFlowPurpose.FactoryConstructionForPlayerMobileFleets )//this already gets written at the very start, before range, since it doesn't have the same range restriction
                    continue;
                if ( detailLevel < TooltipDetail.Full && (purpose == MetalFlowPurpose.ClaimingNeutrals || purpose == MetalFlowPurpose.RebuildingRemains) )
                    continue;  //claim neutral units and rebuild remains only shown in full mode
                if ( purpose == MetalFlowPurpose.RepairingHullsOfFriendlies || purpose == MetalFlowPurpose.RepairingShieldsOfFriendlies ||
                    purpose == MetalFlowPurpose.RepairingEnginesOfFriendlies )//these are also somewhat special and can be organized better when combined
                    continue;
                metalFlow = markStats.GetMetalFlow( purpose );
                if ( !metalFlow.HasRealData )
                    continue;
                if ( metalFlow.EffectiveThroughput <= FInt.Zero )
                    continue;

                debugStage = 2000;
                switch ( purpose )
                {
                    case MetalFlowPurpose.AssistSelfConstruction:
                        debugStage = 3000;
                        WriteRangedSupportMetalFlowStartOrSeparator( buffer, markStats, ref wroteRangeStart );
                        buffer.Add( "协助建造（速度 <color=#ffdf72>" ).Add( metalFlow.EffectiveThroughput.ReadableString ).Add( "</color>）" );
                        if ( squadOrNull != null )
                        {
                            PlannedMetalFlow flow = GetMetalFlowForPurposeOrDefault( squadOrNull, purpose );
                            if(flow.FromEntity != null )
                                WritePlannedMetalFlowBriefTargetInfo( buffer, flow, "Build", detailLevel >= TooltipDetail.Full );
                        }
                        break;
                    case MetalFlowPurpose.AssistFactoryConstruction:
                        debugStage = 4000;
                        WriteRangedSupportMetalFlowStartOrSeparator( buffer, markStats, ref wroteRangeStart );
                        buffer.Add( "加速工厂（速度 <color=#ffdf72>" ).Add( metalFlow.EffectiveThroughput.ReadableString ).Add( "</color>）" );
                        if ( squadOrNull != null )
                        {
                            PlannedMetalFlow flow = GetMetalFlowForPurposeOrDefault( squadOrNull, purpose );
                            if ( flow.FromEntity != null )
                                WritePlannedMetalFlowBriefTargetInfo( buffer, flow, "Boost", detailLevel >= TooltipDetail.Full );
                        }
                        break;
                    case MetalFlowPurpose.ClaimingNeutrals:
                        debugStage = 5000;
                        WriteRangedSupportMetalFlowStartOrSeparator( buffer, markStats, ref wroteRangeStart );
                        buffer.Add( "占领中立单位（速度 <color=#ffdf72>" ).Add( metalFlow.EffectiveThroughput.ReadableString ).Add( "</color>）" );
                        if ( squadOrNull != null )
                        {
                            PlannedMetalFlow flow = GetMetalFlowForPurposeOrDefault( squadOrNull, purpose );
                            if ( flow.FromEntity != null )
                                WritePlannedMetalFlowBriefTargetInfo( buffer, flow, "Claim", detailLevel >= TooltipDetail.Full );
                        }
                        break;
                    case MetalFlowPurpose.RebuildingRemains:
                        debugStage = 6000;
                        WriteRangedSupportMetalFlowStartOrSeparator( buffer, markStats, ref wroteRangeStart );
                        buffer.Add( "重建残骸（速度 <color=#ffdf72>" ).Add( metalFlow.EffectiveThroughput.ReadableString ).Add( "</color>）" );
                        if ( squadOrNull != null )
                        {
                            PlannedMetalFlow flow = GetMetalFlowForPurposeOrDefault( squadOrNull, purpose );
                            if ( flow.FromEntity != null )
                                WritePlannedMetalFlowBriefTargetInfo( buffer, flow, "Rebuild", detailLevel >= TooltipDetail.Full );
                        }
                        break;
                    default:
                        continue;
                }
                debugStage = 7000;
            }

            debugStage = 8000;
            EntityMetalFlowEntry repairHull = markStats.GetMetalFlow( MetalFlowPurpose.RepairingHullsOfFriendlies );
            EntityMetalFlowEntry repairShield = markStats.GetMetalFlow( MetalFlowPurpose.RepairingShieldsOfFriendlies );
            EntityMetalFlowEntry repairEngine = markStats.GetMetalFlow( MetalFlowPurpose.RepairingEnginesOfFriendlies );
            if( (repairHull.HasRealData && repairHull.EffectiveThroughput > FInt.Zero ) || (repairShield.HasRealData && repairShield.EffectiveThroughput > FInt.Zero) ||
                (repairEngine.HasRealData && repairEngine.EffectiveThroughput > FInt.Zero) )
            {
                debugStage = 9000;
                WriteRangedSupportMetalFlowStartOrSeparator( buffer, markStats, ref wroteRangeStart );
                buffer.Add( "修理友方 " );

                if ( repairHull.HasRealData && repairShield.HasRealData && repairEngine.HasRealData && repairHull.EffectiveThroughput == repairShield.EffectiveThroughput &&
                    repairHull.EffectiveThroughput == repairEngine.EffectiveThroughput )//if all flows are valid and have the same value, shorten the tooltip some
                {
                    buffer.Add( "船体 / 护盾 / 引擎（速度 <color=#ffdf72>" ).Add( repairHull.EffectiveThroughput.ReadableString ).Add( "</color>）" );
                } else
                {
                    if ( repairHull.HasRealData && repairHull.EffectiveThroughput > FInt.Zero )
                        buffer.Add( "船体（速度 <color=#ffdf72>" ).Add( repairHull.EffectiveThroughput.ReadableString ).Add( "</color>）" );
                    if ( repairShield.HasRealData && repairShield.EffectiveThroughput > FInt.Zero )
                        buffer.Add( "护盾（速度 <color=#ffdf72>" ).Add( repairShield.EffectiveThroughput.ReadableString ).Add( "</color>）" );
                    if ( repairEngine.HasRealData && repairEngine.EffectiveThroughput > FInt.Zero )
                        buffer.Add( "引擎（速度 <color=#ffdf72>" ).Add( repairEngine.EffectiveThroughput.ReadableString ).Add( "</color>）" );
                }
            }
            if ( wroteRangeStart )
                buffer.Add( ".  " );
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

        public static void WriteAmplifierOrInhibitorCategoryStartIfNeeded( ArcenCharacterBufferBase Buffer, bool IsAmplifier, ref bool AlreadyWroteStart, ref bool NeedsSeparator )
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
                Buffer.Add( "<color=#f25e1c>行星增幅器:</color> On this planet boosts " );
            else
                Buffer.Add( "<color=#f25e1c>行星抑制器:</color> On this planet impairs " );
        }

        public static void WriteAmplifierOrInhibitorSubCategoryStartIfNeeded( ArcenCharacterBufferBase Buffer, bool IsForAllies, ref bool AlreadyWroteStart )
        {
            if ( AlreadyWroteStart )
            {
                Buffer.Add( ", " );
                return;
            }
            AlreadyWroteStart = true;
            if( IsForAllies )
                Buffer.Add( "其拥有者和盟友 " );
            else
                Buffer.Add( "其敌人 " );
        }

        public static void WriteAmplifierOrInhibitorSubCategory( ArcenCharacterBufferBase Buffer, GameEntityTypeData.MarkLevelStats MarkData,
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
                Buffer.StartDamageWrapper( UseIcons ).Add( attack ).Add( "x" ).EndDamageWrapper( UseText );
            }
            if ( writeSpeed_Mult || writeSpeed_Flat )
            {
                WriteAmplifierOrInhibitorCategoryStartIfNeeded( Buffer, IsAmplifier, ref WroteCategoryStart, ref NeedsSeparator );
                WriteAmplifierOrInhibitorSubCategoryStartIfNeeded( Buffer, IsForAllies, ref wroteSubCategoryStart );
                Buffer.StartSpeedWrapper( UseIcons );
                if ( writeSpeed_Mult )
                    Buffer.Add( speed_Mult ).Add( "x" );
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

        public static void WriteAmplifierAndInhibitorData( ArcenCharacterBufferBase Buffer, GameEntityTypeData.MarkLevelStats MarkData, bool UseIcons, bool UseText )
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

        public struct FleetShipStatsForDisplay
        {
            public FleetMembership Membership;//this only needs to hold info for the type, mark and strength of the thing
            public Balance_MarkLevel Mark;
            public int Current;
            public int Max;
            public int Transported;
            public int Lines;

            public FleetShipStatsForDisplay( FleetMembership Mem, bool IsForCapture, Faction LocalPlayerOrNull )
            {
                if ( Mem == null )
                    throw new Exception( "Error: Cannot create FleetShipStatsForDisplay with null fleet membership!" );
                Membership = Mem;
                if ( IsForCapture && LocalPlayerOrNull != null )
                {
                    Mark = Balance_MarkLevelTable.Instance.RowsByOrdinal[LocalPlayerOrNull.GetGlobalMarkLevelForShipLine( Mem.TypeData )];
                } else
                {
                    Mark = Mem.ForMark?.MarkLevel;
                    if( Mark == null )//apparently there is a way to get an error here if looking at a JUST created centerpiece - ForMark is null. Thus the attempts to repair.
                    {
                        int ordinal = Mem.EffectiveMark;
                        if ( ordinal >= 0 )
                            Mark = Balance_MarkLevelTable.Instance.RowsByOrdinal[ordinal];
                        else
                            Mark = Balance_MarkLevelTable.Instance.DefaultRow;
                    }
                }
                Current = Mem.GetCurrentTotalCount_ForUIOnly();
                if ( Current < 0 )
                    Current = 0;
                Max = Mem.GetMaxTotalCount_ForUIOnly( IsForCapture );
                if ( Max < Current )
                    Max = Current;
                Transported = Mem.CalculateTransportedContentsCount() + Mem.NumberCreatedButNotDeployed;
                if ( Transported < 0 )
                    Transported = 0;
                if ( Transported > Current )
                    Transported = Current;
                Lines = 1;
            }

            public FleetShipStatsForDisplay( FleetShipStatsForDisplay ToBeAddedTo, FleetMembership Mem, bool IsForCapture )
            {
                Membership = ToBeAddedTo.Membership;
                Mark = ToBeAddedTo.Mark;
                int currentMembershipCurrent = Mem.GetCurrentTotalCount_ForUIOnly();
                Current = ToBeAddedTo.Current + currentMembershipCurrent;
                int currentMembershipMax = Mem.GetMaxTotalCount_ForUIOnly( IsForCapture );
                if ( currentMembershipMax < currentMembershipCurrent )
                    currentMembershipMax = currentMembershipCurrent;
                Max = ToBeAddedTo.Max + currentMembershipMax;
                int currentTransported = Mem.CalculateTransportedContentsCount() + Mem.NumberCreatedButNotDeployed;
                if ( currentTransported < 0 )
                    currentTransported = 0;
                if ( currentTransported > currentMembershipCurrent )
                    currentTransported = currentMembershipCurrent;
                Transported = ToBeAddedTo.Transported + currentTransported;
                Lines = ToBeAddedTo.Lines + 1;
            }

            public override string ToString()
            {
                return Membership.TypeData.InternalName + " Mk " + Mark.Ordinal + ": " + Current + " / " + Max + " (" + Transported + " T)" + " in " + Lines;
            }
        }

        public static void BuildFleetShipStatsForDisplay( Fleet ForFleet, List<FleetShipStatsForDisplay> ToFill, bool IsCity, bool DuplicateSameShipLines, bool IsForCapture, Faction LocalPlayerOrNull, TooltipDetail DetailLevel )
        {
            ToFill.Clear();
            FleetShipStatsForDisplay stat;
            foreach ( FleetMembership Mem in ForFleet.MemberGroupsSorted_NonSim_ForUIOnly_SeriouslyDoNotCallFromBGCode() )
            {
                int effectiveHere = Mem.GetCountPresent( true, ExtraFromStacks.IncludePrecalc );
                if ( DetailLevel < TooltipDetail.Medium && IsCity && effectiveHere <= 0 )
                    continue;

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

                if (DuplicateSameShipLines)
                {
                    ToFill.Add( new FleetShipStatsForDisplay( Mem, IsForCapture, LocalPlayerOrNull ) );
                    continue;
                }
                bool merged = false;
                for(int i = 0; i < ToFill.Count; i++ )
                {
                    stat = ToFill[i];
                    if (stat.Membership.TypeData == Mem.TypeData && stat.Membership.EffectiveMark == Mem.EffectiveMark )
                    {
                        ToFill[i] = new FleetShipStatsForDisplay( stat, Mem, IsForCapture );
                        merged = true;
                        break;
                    }
                }
                if ( merged )
                    continue;
                ToFill.Add( new FleetShipStatsForDisplay( Mem, IsForCapture, LocalPlayerOrNull ) );
            }

            SortFleetShipStatusForDisplay( ToFill );
        }

        public static void SortFleetShipStatusForDisplay( List<FleetShipStatsForDisplay> ToSort )
        {
            int sortMode = GameSettings.Current.GetIntBySetting( "Tooltip_FleetMembershipSortPriority" );
            if ( sortMode == 0 ) //Type > Name > Strength > Mark Level > Current Unit Count > Max Unit Count > Transporting Count
            {
                ToSort.Sort( static delegate ( FleetShipStatsForDisplay L, FleetShipStatsForDisplay R )
                {
                    FleetMembership l = L.Membership;
                    FleetMembership r = R.Membership;
                    int x;
                    if ( l.TypeData != r.TypeData )
                    {
                        x = CompareType( l.TypeData, r.TypeData );
                        if ( x != 0 )
                            return x;
                        x = CompareName( l.TypeData, r.TypeData );
                        if ( x != 0 )
                            return x;
                    }
                    if ( l.TypeData != r.TypeData || l.EffectiveMark != r.EffectiveMark )
                    {
                        x = CompareStrength( L, R );
                        if ( x != 0 )
                            return x;
                    }
                    x = CompareMark( l.EffectiveMark, r.EffectiveMark );
                    if ( x != 0 )
                        return x;
                    x = CompareCount( L.Current, R.Current );
                    if ( x != 0 )
                        return x;
                    x = CompareCount( L.Max, R.Max );
                    if ( x != 0 )
                        return x;
                    return CompareCount( L.Transported, R.Transported );
                } );
            } else if ( sortMode == 1 ) //Type > Strength > Name > Mark Level > Current Unit Count > Max Unit Count > Transporting Count
            {
                ToSort.Sort( static delegate ( FleetShipStatsForDisplay L, FleetShipStatsForDisplay R )
                {
                    FleetMembership l = L.Membership;
                    FleetMembership r = R.Membership;
                    int x;
                    if ( l.TypeData != r.TypeData )
                    {
                        x = CompareType( l.TypeData, r.TypeData );
                        if ( x != 0 )
                            return x;
                    }
                    if ( l.TypeData != r.TypeData || l.EffectiveMark != r.EffectiveMark )
                    {
                        x = CompareStrength( L, R );
                        if ( x != 0 )
                            return x;
                    }
                    if ( l.TypeData != r.TypeData )
                    {
                        x = CompareName( l.TypeData, r.TypeData );
                        if ( x != 0 )
                            return x;
                    }
                    x = CompareMark( l.EffectiveMark, r.EffectiveMark );
                    if ( x != 0 )
                        return x;
                    x = CompareCount( L.Current, R.Current );
                    if ( x != 0 )
                        return x;
                    x = CompareCount( L.Max, R.Max );
                    if ( x != 0 )
                        return x;
                    return CompareCount( L.Transported, R.Transported );
                } );
            } else if ( sortMode == 2 ) //Strength > Name > Type > Mark Level > Current Unit Count > Max Unit Count > Transporting Count
            {
                ToSort.Sort( static delegate ( FleetShipStatsForDisplay L, FleetShipStatsForDisplay R )
                {
                    FleetMembership l = L.Membership;
                    FleetMembership r = R.Membership;
                    int x;
                    if ( l.TypeData != r.TypeData || l.EffectiveMark != r.EffectiveMark )
                    {
                        x = CompareStrength( L, R );
                        if ( x != 0 )
                            return x;
                    }
                    if ( l.TypeData != r.TypeData )
                    {
                        x = CompareName( l.TypeData, r.TypeData );
                        if ( x != 0 )
                            return x;
                        x = CompareType( l.TypeData, r.TypeData );
                        if ( x != 0 )
                            return x;
                    }
                    x = CompareMark( l.EffectiveMark, r.EffectiveMark );
                    if ( x != 0 )
                        return x;
                    x = CompareCount( L.Current, R.Current );
                    if ( x != 0 )
                        return x;
                    x = CompareCount( L.Max, R.Max );
                    if ( x != 0 )
                        return x;
                    return CompareCount( L.Transported, R.Transported );
                } );
            } else if ( sortMode == 3 ) //Strength > Type > Name > Mark Level > Current Unit Count > Max Unit Count > Transporting Count
            {
                ToSort.Sort( static delegate ( FleetShipStatsForDisplay L, FleetShipStatsForDisplay R )
                {
                    FleetMembership l = L.Membership;
                    FleetMembership r = R.Membership;
                    int x;
                    if ( l.TypeData != r.TypeData || l.EffectiveMark != r.EffectiveMark )
                    {
                        x = CompareStrength( L, R );
                        if ( x != 0 )
                            return x;
                    }
                    if ( l.TypeData != r.TypeData )
                    {
                        x = CompareType( l.TypeData, r.TypeData );
                        if ( x != 0 )
                            return x;
                        x = CompareName( l.TypeData, r.TypeData );
                        if ( x != 0 )
                            return x;
                    }
                    x = CompareMark( l.EffectiveMark, r.EffectiveMark );
                    if ( x != 0 )
                        return x;
                    x = CompareCount( L.Current, R.Current );
                    if ( x != 0 )
                        return x;
                    x = CompareCount( L.Max, R.Max );
                    if ( x != 0 )
                        return x;
                    return CompareCount( L.Transported, R.Transported );
                } );
            } else if ( sortMode == 4 ) //Name > Strength > Type > Mark Level > Current Unit Count > Max Unit Count > Transporting Count
            {
                ToSort.Sort( static delegate ( FleetShipStatsForDisplay L, FleetShipStatsForDisplay R )
                {
                    FleetMembership l = L.Membership;
                    FleetMembership r = R.Membership;
                    int x;
                    if ( l.TypeData != r.TypeData )
                    {
                        x = CompareName( l.TypeData, r.TypeData );
                        if ( x != 0 )
                            return x;
                    }
                    if ( l.TypeData != r.TypeData || l.EffectiveMark != r.EffectiveMark )
                    {
                        x = CompareStrength( L, R );
                        if ( x != 0 )
                            return x;
                    }
                    if ( l.TypeData != r.TypeData )
                    {
                        x = CompareType( l.TypeData, r.TypeData );
                        if ( x != 0 )
                            return x;
                    }
                    x = CompareMark( l.EffectiveMark, r.EffectiveMark );
                    if ( x != 0 )
                        return x;
                    x = CompareCount( L.Current, R.Current );
                    if ( x != 0 )
                        return x;
                    x = CompareCount( L.Max, R.Max );
                    if ( x != 0 )
                        return x;
                    return CompareCount( L.Transported, R.Transported );
                } );
            } else if ( sortMode == 5 ) //Name > Type > Strength > Mark Level > Current Unit Count > Max Unit Count > Transporting Count
            {
                ToSort.Sort( static delegate ( FleetShipStatsForDisplay L, FleetShipStatsForDisplay R )
                {
                    FleetMembership l = L.Membership;
                    FleetMembership r = R.Membership;
                    int x;
                    if ( l.TypeData != r.TypeData )
                    {
                        x = CompareName( l.TypeData, r.TypeData );
                        if ( x != 0 )
                            return x;
                        x = CompareType( l.TypeData, r.TypeData );
                        if ( x != 0 )
                            return x;
                    }
                    if ( l.TypeData != r.TypeData || l.EffectiveMark != r.EffectiveMark )
                    {
                        x = CompareStrength( L, R );
                        if ( x != 0 )
                            return x;
                    }
                    x = CompareMark( l.EffectiveMark, r.EffectiveMark );
                    if ( x != 0 )
                        return x;
                    x = CompareCount( L.Current, R.Current );
                    if ( x != 0 )
                        return x;
                    x = CompareCount( L.Max, R.Max );
                    if ( x != 0 )
                        return x;
                    return CompareCount( L.Transported, R.Transported );
                } );
            } else
            {
                throw new Exception( "Error: Unimpelemnted sort mode index " + sortMode );
            }
        }

        public static int CompareType( GameEntityTypeData L, GameEntityTypeData R )
        {
            if ( L.IsDrone && !R.IsDrone )
                return 1;
            if ( !L.IsDrone && R.IsDrone )
                return -1;
            if ( L.IsTurret && !R.IsTurret )
                return 1;
            if ( !L.IsTurret && R.IsTurret )
                return -1;
            if ( L.IsStrikecraft && !R.IsStrikecraft )
                return 1;
            if ( !L.IsStrikecraft && R.IsStrikecraft )
                return -1;
            if ( L.SpecialType == SpecialEntityType.Cruiser && R.SpecialType != SpecialEntityType.Cruiser )
                return -1;
            if ( L.SpecialType != SpecialEntityType.Cruiser && R.SpecialType == SpecialEntityType.Cruiser )
                return 1;
            if ( L.SpecialType == SpecialEntityType.Destroyer && R.SpecialType != SpecialEntityType.Destroyer )
                return -1;
            if ( L.SpecialType != SpecialEntityType.Destroyer && R.SpecialType == SpecialEntityType.Destroyer )
                return 1;
            if ( L.IsGuardian && !R.IsGuardian )
                return -1;
            if ( !L.IsGuardian && R.IsGuardian )
                return 1;
            return 0;
        }

        public static int CompareName( GameEntityTypeData L, GameEntityTypeData R )
        {
            return L.DisplayName.CompareTo( R.DisplayName );
        }

        public static int CompareStrength( FleetShipStatsForDisplay L, FleetShipStatsForDisplay R )
        {
            return (R.Membership.ForMark.GetCalculatedStrengthPerSquadForFleetOrNull( R.Membership ) * R.Max)
                .CompareTo( (L.Membership.ForMark.GetCalculatedStrengthPerSquadForFleetOrNull( L.Membership ) * L.Max) );
        }

        public static int CompareMark( byte L, byte R )
        {
            return R.CompareTo( L );
        }

        public static int CompareCount( int L, int R )
        {
            return R.CompareTo( L );
        }

        public static void WriteFleetMembershipTooltip( ArcenCharacterBufferBase Buffer, List<FleetShipStatsForDisplay> InfoList, bool IsForCapture, bool UseIcons, bool UseText )
        {
            FleetShipStatsForDisplay Info;
            bool WriteSeparator = false;
            for ( int i = 0; i < InfoList.Count; i++ )
            {
                Info = InfoList[i];
                FleetMembership Mem = Info.Membership;
                if ( Mem == null )
                    continue;
                GameEntityTypeData type = Mem.TypeData;
                if ( type == null )
                    continue;

                int current = Info.Current;
                int max;
                max = Info.Max;
                if ( max < current )
                    max = current;
                if ( current <= 0 && max <= 0 )
                    continue;

                if ( WriteSeparator )
                    Buffer.Add( "| " );
                else
                    WriteSeparator = true;

                bool iconAndName = EntityText.Detail == TooltipDetail.Full && GameSettings.Current.GetBoolBySetting( "Tooltip_ShipNames_Full" );

                Balance_MarkLevel markLevel = Info.Mark;
                if (markLevel == null)
                {
                    if (!UseIcons)
                    {
                        Buffer.Add( type.GetShortDisplayName() );
                    }
                    else
                    {
                        if (iconAndName)
                            Buffer.AddShipIconInline( type, Mem.Fleet?.Faction, null ).Add( type.GetShortDisplayName() );
                        else
                            Buffer.AddShipIconInline( type, Mem.Fleet?.Faction, null );
                    }
                }
                else
                {
                    if (!UseIcons)
                    {
                        Buffer.Add( markLevel.ColorHexStart ).Add( type.GetShortDisplayName() ).Add( " " ).Add( markLevel.Abbreviation ).EndColor();
                    }
                    else
                    {
                        if (iconAndName)
                            Buffer.AddShipIconInline( type, Mem.Fleet?.Faction, null ).Add( markLevel.ColorHexStart ).Add( type.GetShortDisplayName() ).Add( " " ).Add( markLevel.Abbreviation ).EndColor();
                        else
                            Buffer.AddShipIconInline( type, Mem.Fleet?.Faction, markLevel );
                    }
                }

                Buffer.Add( " " );

                if ( IsForCapture )
                {
                    if ( max > 0 )
                        Buffer.Add( max ).Add( "x " );
                } else
                {
                    if ( current > 0 || max > 0 )
                    {
                        if ( max == 0 )
                            Buffer.StartPercentageInColor( FInt.OneHundred, true, false );
                        else
                            Buffer.StartPercentageInColor( (FInt.OneHundred * current) / max, true, false );
                        Buffer.Add( current );
                        if ( max != current )
                        {
                            Buffer.Add( " / " ).Add( max );
                        }
                        Buffer.EndColor();
                        int transported = Info.Transported;
                        if ( current > 0 && transported > 0 )
                        {
                            Buffer.Add( " (" );
                            if ( transported == current )
                            {
                                Buffer.Add( "T", ArcenExternalUIUtilities.Buffs.Color );
                            } else if ( transported > 0 )
                            {
                                Buffer.StartColor( ArcenExternalUIUtilities.Debuffs.Color ).Add( transported ).Add( " T" ).EndColor();
                            }
                            Buffer.Add( ")" );
                        }
                        Buffer.Add( " " );
                    }

                    if ( type.IsDrone && Mem.MetalSpentConstructingCurrentReplacement != 0)
                    {
                        var fval = FInt.CreateFromDoubleNonSim((double)Mem.MetalSpentConstructingCurrentReplacement / (double)Mem.ForMark.MetalCost * 100).ToRounded(0);
                        Buffer.Add("(");
                        Buffer.AddPercentageInColor( fval, true, false );
                        Buffer.Add(") ");
                    }
                }
            }
        }
    }
}
