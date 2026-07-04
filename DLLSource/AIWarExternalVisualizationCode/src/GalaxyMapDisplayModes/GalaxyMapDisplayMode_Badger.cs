using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using UnityEngine;
using Arcen.AIW2.External;

namespace Arcen.AIW2.ExternalVisualization
{
    public class GalaxyMapDisplayMode_Reducers : BaseGalaxyMapDisplayMode
    {
        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            int currentIndex = 0;
            UtilityMethodsFor_GalaxyMapDisplayMode.AddPlayerFleetsToGalaxyMapOtherThingsToShow( planet, EntityToSkip, ArrayToFill, ref currentIndex );

            UtilityMethodsFor_GalaxyMapDisplayMode.BasePickGalaxyMapOtherThingsToShow( ref currentIndex, planet, EntityToSkip, ArrayToFill,
                delegate ( GameEntity_Squad entity )
                {
                    if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                         return false;

                    return entity.TypeData.GetHasTag( "ProgressReducer" ); 
                } );
        }
    }

    public class GalaxyMapDisplayMode_Targets : BaseGalaxyMapDisplayMode
    {
        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            int currentIndex = 0;
            UtilityMethodsFor_GalaxyMapDisplayMode.AddPlayerFleetsToGalaxyMapOtherThingsToShow( planet, EntityToSkip, ArrayToFill, ref currentIndex );

            UtilityMethodsFor_GalaxyMapDisplayMode.BasePickGalaxyMapOtherThingsToShow( ref currentIndex, planet, EntityToSkip, ArrayToFill,
                delegate ( GameEntity_Squad entity ) 
                {
                    if ( entity.GetIsPlayerUnit() )
                        return false;
                    
                    if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                         return false;

                    if ( entity.TypeData.IsFleetLeader && entity.GetIsNaturalObjectUnit() )
                        return true;
                    
                    if (entity.TypeData.GetHasTag( "Capturable" ) || entity.TypeData.GetHasTag( "HackAsIfCapturable" ))
                        return true;

                    return false;
                } );    
        }
    }

    public class GalaxyMapDisplayMode_AIDefenses : BaseGalaxyMapDisplayMode
    {
        private static Int64 LastSelectionStrengthUpdateSecondsPlayed_NonSim;
        private static FInt LastSelectionStrength;

        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;
            int currentIndex = 0;
            UtilityMethodsFor_GalaxyMapDisplayMode.AddPlayerFleetsToGalaxyMapOtherThingsToShow( planet, EntityToSkip, ArrayToFill, ref currentIndex );

            UtilityMethodsFor_GalaxyMapDisplayMode.BasePickGalaxyMapOtherThingsToShow( ref currentIndex, planet, EntityToSkip, ArrayToFill,
                delegate ( GameEntity_Squad entity )
                {
                    if ( entity.GetIsPlayerUnit() )
                        return false;
                    if ( entity.TypeData.GetHasTag( "NormalPlanetNastyPick" ) )
                        return true;
                    if ( entity.TypeData.GetHasTag( "AIOverlord_AnyTypeOrPhase" ) )
                        return true;
                    return false;
                } );
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;
            Faction localFactionG = World_AIW2.Instance.GetPlayerFactionForUIOrNull();

            PlanetFaction localFaction = planet.GetPlanetFactionForFaction( localFactionG );
            int totalStrengthInt = localFaction.DataByStance[FactionStance.Hostile].TotalStrengthVisible;
            int totalStrengthIntIncludingInvisible = localFaction.DataByStance[FactionStance.Hostile].TotalStrength;

            if ( Mat.Abs( World.Instance.CampaignRealSecondsPlayed_NonSim - LastSelectionStrengthUpdateSecondsPlayed_NonSim ) > 0 )
            {
                LastSelectionStrengthUpdateSecondsPlayed_NonSim = World.Instance.CampaignRealSecondsPlayed_NonSim;
                FInt localSelectionStrength = FInt.Zero;

                foreach ( GameEntity_Squad selected in Engine_AIW2.Instance.SelectedSquadsICanGiveOrdersTo )
                {
                    localSelectionStrength += selected.GetStrengthOfSelfAndContents();
                }
                LastSelectionStrength = localSelectionStrength;
            }

            if ( totalStrengthInt <= 0 && totalStrengthIntIncludingInvisible <= 0 )
                return;

            Color color;
            if ( totalStrengthInt > LastSelectionStrength << 1 )
                color = ColorMath.OrangeRed;
            else if ( totalStrengthInt > LastSelectionStrength )
                color = ColorMath.Orange;
            else if ( totalStrengthInt < ( LastSelectionStrength >> 1 ) )
                color = ColorMath.DeepSkyBlue;
            else
                color = ColorMath.White;

            //RIGHT ONLY
            RightBuffer.StartColor( color );
            ArcenExternalUIUtilities.ByPlanet_WriteStrengthIconWithColor( RightBuffer, color.GetHexCode() );
            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( RightBuffer, totalStrengthInt, true, true );
            if ( totalStrengthIntIncludingInvisible > totalStrengthInt )
                RightBuffer.Add( "<size=12>+</size>" );
        }
    }
    public class GalaxyMapDisplayMode_DeepstrikeIntegration : BaseGalaxyMapDisplayMode
    {
        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            GalaxyMapDisplayMode_Normal.ShowNormalIcons( planet, EntityToSkip, ArrayToFill, OnlyShowThingsThatShouldBeInFarZoom );
        }
        
        public static void ShowNormalIcons( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            //can't skip mobile units
            if ( EntityToSkip != null && EntityToSkip.TypeData.IsMobile )
                EntityToSkip = null;

            int currentIndex = 0;
            UtilityMethodsFor_GalaxyMapDisplayMode.BasePickGalaxyMapOtherThingsToShow( ref currentIndex, planet, EntityToSkip, ArrayToFill,
             delegate ( GameEntity_Squad entity )
             {
                 if ( OnlyShowThingsThatShouldBeInFarZoom )
                 {
                     if ( !entity.GetIsSelected() )
                         return false; //if in far zoom, only show selected entities
                 }
                 if ( entity.TypeData.DrawInGalaxyView || entity.TypeData.GetHasTag( "ShowsOnNormalDisplayMode" ) || entity.TypeData.GetHasTag( "ProgressReducer" ) || entity.TypeData.GetHasTag( "Capturable" ) )
                 {
                     //this unit is eligible to be shown
                     if ( entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                         return true;
                 }
                 return false;
             } );
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;
            bool isMinimalFogOfWar = AIWar2GalaxySettingQuickAccess.GalaxyMinimalFogOfWar;

            int threatStrengthInt, hostileStrengthMinusThreatInt, myTotalStrength, myMobileStrength, myAndAlliedTotalStrength, myAndAlliedMobileStrength;
            Faction hostileFaction;
            Faction myOrAlliedFaction;
            Faction alliedFaction;
            Window_InGameHoverPlanetInfo.GetPlanetFactionalData( planet, out threatStrengthInt, out hostileStrengthMinusThreatInt, out myTotalStrength, out myMobileStrength,
                out myAndAlliedTotalStrength, out myAndAlliedMobileStrength, out myOrAlliedFaction, out alliedFaction, out hostileFaction, isMinimalFogOfWar );

            var myOrAlliedColor = myOrAlliedFaction?.FactionCenterColor.GetColorHexBrighter( false ) ?? QuickColors.White;
            var alliedColor = alliedFaction?.FactionCenterColor.GetColorHexBrighter( false ) ?? QuickColors.White;

            //LEFT
            if ( myAndAlliedTotalStrength > 0 )
            {
                string textColor = "ffffff";
                if ( myMobileStrength > 0 || myAndAlliedMobileStrength == myMobileStrength )
                    textColor = myOrAlliedColor;
                else
                    textColor = alliedColor;
                if (string.IsNullOrWhiteSpace(textColor))
                     textColor = "ffffff";
                
                LeftBuffer.StartColor( textColor );
                int myAndAlliedImmobileStrength = Math.Max( myAndAlliedTotalStrength - myAndAlliedMobileStrength, 0 );
                ArcenExternalUIUtilities.ByPlanet_WriteStrengthIconWithColor( LeftBuffer, textColor );
                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( LeftBuffer, myAndAlliedMobileStrength, true, true );
                LeftBuffer.Add( "\n" );
                ArcenExternalUIUtilities.ByPlanet_WriteStrengthIconWithColor( LeftBuffer, textColor );
                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( LeftBuffer, myAndAlliedImmobileStrength, true, true );
            }

            //RIGHT LINE 1
            if ( hostileStrengthMinusThreatInt > 0 )
            {
                string textColor = "ffffff";
                if ( hostileFaction != null )
                    textColor = hostileFaction.FactionCenterColor.ColorHexLinearBrighter;
                if (string.IsNullOrWhiteSpace(textColor))
                     textColor = "ffffff";
                
                RightBuffer.StartColor( textColor );
                ArcenExternalUIUtilities.ByPlanet_WriteStrengthIconWithColor( RightBuffer, textColor );
                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( RightBuffer, hostileStrengthMinusThreatInt, true, true );
                if ( !isMinimalFogOfWar && planet.IntelLevel < PlanetIntelLevel.CurrentlyWatched )
                    RightBuffer.Add( "<size=12>?</size>" );


                if ( threatStrengthInt > 0 )
                    RightBuffer.Add( "</color>\n" );
            }

            //RIGHT LINE 2
            if ( planet.IsEligibleForDeepStrike )
            {
                RightBuffer.Add("\n");
                RightBuffer.StartColor( "ff3939" ).Add("<size=70%>深袭</size>").EndColor();
                    
            }
        }
    }
    public class GalaxyMapDisplayMode_CuendillarAmount : BaseGalaxyMapDisplayMode
    {
        public override bool GetShouldBeShownInCurrentCampaign()
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( DysonSidekickFactionBaseInfo.GetIsThisADysonFaction( localFaction ) )
                return true;
            return false;
        }
        public override bool GetShouldReplaceNormalPlanetTooltip()
        {
            return true;
        }
        public override void WriteToPlanetTooltip( Planet planet, ArcenDoubleCharacterBuffer Buffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            Buffer.Add( "\n剩余昆达拉：" ).Add( planet.ResourceOneRemainingForAnyPlayer, "a1ffa1" );
        }

        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            GalaxyMapDisplayMode_Normal.ShowNormalIcons( planet, EntityToSkip, ArrayToFill, OnlyShowThingsThatShouldBeInFarZoom );
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;
            if ( planet.IsRavaged )
                LeftBuffer.Add("被蹂躏");
            RightBuffer.Add( "<size=120%>" );

            RightBuffer.StartColor( "af9380" );

            RightBuffer.Add( planet.ResourceOneRemainingForAnyPlayer );
            RightBuffer.EndColor();
        }
    }
    public class GalaxyMapDisplayMode_ShowNetMetal : BaseGalaxyMapDisplayMode
    {
        private const int MAX_ICONS_AVAILABLE_OVERALL = 12; //12 is the max we can have
        private const int MAX_ICONS_WE_WANT = 3; //12 is the max we can have, but let's only choose the top 3

        private RefPair<SafeSquadWrapper, int>[] ArrayOfTopItems = new RefPair<SafeSquadWrapper, int>[MAX_ICONS_WE_WANT];
        public override bool GetShouldBeShownInCurrentCampaign()
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            PlayerTypeData playerTypeData = localFaction == null ? null : localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( playerTypeData == null || playerTypeData.UsesMetal )
                return true;
            return false;
        }
        public override bool GetShouldReplaceNormalPlanetTooltip()
        {
            return true;
        }
        public override void WriteToPlanetTooltip( Planet planet, ArcenDoubleCharacterBuffer Buffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null )
                return;
            if ( planet.GetControllingOrInfluencingFaction() != localFaction)
                return;
            Buffer.Add( "\n金属收入：" ).Add( planet.LocalPlayer_MetalIncomePerPlanet_ForUI_Final, "a1ffa1" );
            Buffer.Add( "\n金属支出：" ).Add( planet.LocalPlayer_MetalOutflowPerPlanet_ForUI_Final, "ffa1ff" );
        }

        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
         if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            //first get the player squads
            int newOtherGimbalIndex = 0;
            UtilityMethodsFor_GalaxyMapDisplayMode.AddPlayerFleetsToGalaxyMapOtherThingsToShow( planet, EntityToSkip, ArrayToFill, ref newOtherGimbalIndex );

            //reset our list of top items
            UtilityMethodsFor_GalaxyMapDisplayMode.ResetForCollectTopmostSquads( ArrayOfTopItems );

            int effectiveMaxCount = Math.Min( MAX_ICONS_WE_WANT, MAX_ICONS_AVAILABLE_OVERALL - newOtherGimbalIndex );

            FactionFilter currentFilter = FactionFilter.GetFactionFilterByIndex( PlayerAccount_AIW2.GetCurrentGalaxyMapDisplayMode_FactionIndexSafe() );
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.MetalProducers ) )
            {
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                if ( !entity.GetMatchesFactionFilterSafe( currentFilter ) )
                    continue;
                if ( !entity.GetIsPlayerUnit() )
                    continue; //only player units use energy!

                //is this among the top 12 strongest types of squads present for this filter?  If so, include it.
                UtilityMethodsFor_GalaxyMapDisplayMode.CollectTopmostSquads( ArrayOfTopItems, entity, entity.GetEnergyUsage(), true, true, effectiveMaxCount );

            }

            //whatever we found for the top items, add them
            foreach ( RefPair<SafeSquadWrapper, int> kv in ArrayOfTopItems )
            {
                GameEntity_Squad squad = kv.LeftItem.GetSquad();
                if ( squad == null )
                    continue;
                ArrayToFill[newOtherGimbalIndex] = squad;
                newOtherGimbalIndex++;
                if ( newOtherGimbalIndex >= ArrayToFill.Length )
                    break;
            }
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null )
                return;
            if ( planet.GetControllingOrInfluencingFaction() != localFaction)
                return;

            RightBuffer.Add( "<size=120%>" );

            RightBuffer.Add( ArcenExternalUIUtilities.ByPlanet_MetalTextColorAndIcon );

            int netMetal = planet.LocalPlayer_MetalIncomePerPlanet_ForUI_Final - planet.LocalPlayer_MetalOutflowPerPlanet_ForUI_Final;
            RightBuffer.Add( netMetal );
            RightBuffer.EndColor();
        }
    }
    public class GalaxyMapDisplayMode_ShowNetEnergy : BaseGalaxyMapDisplayMode
    {

        private RefPair<SafeSquadWrapper, int>[] ArrayOfTopItems = new RefPair<SafeSquadWrapper, int>[MAX_ICONS_WE_WANT];
        public override bool GetShouldBeShownInCurrentCampaign()
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            PlayerTypeData playerType = localFaction == null ? null : localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( playerType == null || playerType.UsesEnergyAndFuel )
                return true;
            return false;
        }

        private const int MAX_ICONS_AVAILABLE_OVERALL = 12; //12 is the max we can have
        private const int MAX_ICONS_WE_WANT = 3; //12 is the max we can have, but let's only choose the top 3

        public override void PickOtherThingsToShow( Planet planet, GameEntity_Squad EntityToSkip, GameEntity_Squad[] ArrayToFill, bool OnlyShowThingsThatShouldBeInFarZoom )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            //first get the player squads
            int newOtherGimbalIndex = 0;
            UtilityMethodsFor_GalaxyMapDisplayMode.AddPlayerFleetsToGalaxyMapOtherThingsToShow( planet, EntityToSkip, ArrayToFill, ref newOtherGimbalIndex );

            //reset our list of top items
            UtilityMethodsFor_GalaxyMapDisplayMode.ResetForCollectTopmostSquads( ArrayOfTopItems );

            int effectiveMaxCount = Math.Min( MAX_ICONS_WE_WANT, MAX_ICONS_AVAILABLE_OVERALL - newOtherGimbalIndex );

            FactionFilter currentFilter = FactionFilter.GetFactionFilterByIndex( PlayerAccount_AIW2.GetCurrentGalaxyMapDisplayMode_FactionIndexSafe() );
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.EnergyConsumers ) )
            {
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                if ( !entity.GetMatchesFactionFilterSafe( currentFilter ) )
                    continue;
                if ( !entity.GetIsPlayerUnit() )
                    continue; //only player units use energy!

                //is this among the top 12 strongest types of squads present for this filter?  If so, include it.
                UtilityMethodsFor_GalaxyMapDisplayMode.CollectTopmostSquads( ArrayOfTopItems, entity, entity.GetEnergyUsage(), true, true, effectiveMaxCount );

            }

            //whatever we found for the top items, add them
            foreach ( RefPair<SafeSquadWrapper, int> kv in ArrayOfTopItems )
            {
                GameEntity_Squad squad = kv.LeftItem.GetSquad();
                if ( squad == null )
                    continue;
                ArrayToFill[newOtherGimbalIndex] = squad;
                newOtherGimbalIndex++;
                if ( newOtherGimbalIndex >= ArrayToFill.Length )
                    break;
            }
        }

        public override void WriteLeftRightTextPerDisplayModeType( Planet planet, ArcenDoubleCharacterBuffer LeftBuffer, ArcenDoubleCharacterBuffer RightBuffer )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                return;

            FactionFilter currentFilter = FactionFilter.GetFactionFilterByIndex( PlayerAccount_AIW2.GetCurrentGalaxyMapDisplayMode_FactionIndexSafe() );

            FInt energyUsed = FInt.Zero;
            foreach ( GameEntity_Squad entity in planet.Squads() )
            {
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                if ( !entity.GetIsPlayerUnit() )
                    continue; //only player units use energy!

                if ( entity.GetMatchesFactionFilterSafe( currentFilter ) )
                {
                    energyUsed += entity.GetEnergyUsage() + entity.GetEnergyCostOfContentsIfAny();
                }
            }
            FInt netFlow = planet.LocalPlayer_EnergyProducedPerPlanet_ForUIOnly_Final - energyUsed;
            //RIGHT ONLY
            if ( netFlow != FInt.Zero )
                RightBuffer.Add( ArcenExternalUIUtilities.ByPlanet_EnergyTextColorAndIcon )
                    .StartColor( ArcenExternalUIUtilities.EnergyTextColor ).Add( " " ).AddNumberMoreReadable( netFlow.IntValue ).EndColor().Add( "\n" );

        }
    }
}
