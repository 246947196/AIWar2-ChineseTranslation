using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class MyPlanetAttackNotifier : NotifierBaseDataSingleton
    {
        #region Static Data
        public static MyPlanetAttackNotifier Instance = new MyPlanetAttackNotifier();
        private static UnityEngine.Sprite sprite_AttackHomePlanet;
        private static UnityEngine.Sprite sprite_AttackNonHome;
        private static UnityEngine.Sprite sprite_DeadCommandStation;
        private static bool hasInitialized = false;
        #endregion
        
        #region GetPlanetName_Safe
        public string GetPlanetName_Safe( NotifierFillData Data )
        {
            Planet plan = Data.Planet;
            if ( plan == null )
                return "[null planet]";
            return plan.Name;
        }
        #endregion

        #region InitIfNeeded
        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;

            sprite_AttackHomePlanet = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/attackhomeplanet.png" );
            sprite_AttackNonHome = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/attacknonhome.png" );
            sprite_DeadCommandStation = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/ruinedcity.png" );
        }
        #endregion
        
        #region ClickHandler
        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            Planet planet = Data.Planet;
            if ( planet == null )
                return MouseHandlingResult.PlayClickDeniedSound;
            if ( planet == Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() )
                return MouseHandlingResult.DoNotPlayClickSound;
            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( planet, false );
            else
                World_AIW2.Instance.SwitchViewToPlanet( planet );

            return MouseHandlingResult.None;
        }
        #endregion
        
        #region MouseoverHandler
        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            Planet planet = Data.Planet;
            if ( planet == null )
                return false;

            //galaxy map hover
            World_AIW2.Instance.FocusedPlanetForMapDarkening = planet;

            tooltipBuffer.Clear();
            if ( Data.doHumansHaveADeadCommandStationHere )
                tooltipBuffer.Add( planet.Name ).Add( " 已经失守！请在那里重建指挥站，或者废弃它。" );
            else if ( Data.doHumansHaveACrippledCityHere )
                tooltipBuffer.Add( planet.Name ).Add( " 已经失守！请修复那里的城市。" );
            else
                tooltipBuffer.Add( planet.Name ).Add( " 正在遭受攻击！" );

            #region Attackers
            tooltipBuffer.Add( "\n\n<b>攻击者</b>\n" );
            PlanetFaction pFac;
            for ( int i = 0; i < planet.Factions.Count; i++ )
            {
                pFac = planet.Factions[i];
                if ( pFac == null )
                    continue;
                Faction fac = pFac.Faction;
                if ( fac == null )
                    continue;
                if ( fac.Type == FactionType.NaturalObject || pFac.GetIsFriendlyToLocalFaction() )
                    continue;
                ArcenExternalUIUtilities.WriteSideContents( pFac, tooltipBuffer, true );
            }
            #endregion

            #region Defenders
            tooltipBuffer.Add( "\n<b>防御者</b>\n" );
            for ( int i = 0; i < planet.Factions.Count; i++ )
            {
                pFac = planet.Factions[i];
                Faction fac = pFac.Faction;
                if ( fac == null )
                    continue;
                if ( fac.Type == FactionType.NaturalObject || !pFac.GetIsFriendlyToLocalFaction() )
                    continue;
                ArcenExternalUIUtilities.WriteSideContents( pFac, tooltipBuffer, !pFac.GetIsLocalFaction() );
            }
            #endregion

            #region Critical Infrastructure
            tooltipBuffer.Add( "\n<b>关键基础设施</b>\n" );

            Dictionary<GameEntityTypeData, int> infrastructure = GameEntityTypeData.GetTemporaryGameEntityTypeDataIntDict( "PrivateMyPlanetAttackNotifier-MouseoverHandler-infrastructure", 10f );
            if ( infrastructure == null ) //blocked for teardown/shutdown; bail
                return false;

            for ( int i = 0; i < planet.Factions.Count; i++ )
            {
                pFac = planet.Factions[i];
                Faction fac = pFac.Faction;
                if ( fac == null )
                    continue;
                if ( fac.Type == FactionType.NaturalObject || !pFac.GetIsFriendlyToLocalFaction() )
                    continue;
                foreach ( GameEntity_Squad entity in pFac.Entities.Squads( EntityRollupType.CriticalInfrastructure ) )
                {
                    if ( entity == null )
                        continue;
                    if ( infrastructure.ContainsKey( entity.TypeData ) )
                        infrastructure[entity.TypeData] += 1;
                    else
                        infrastructure[entity.TypeData] = 1;
                }
            }
            if ( infrastructure.Count == 0 )
                tooltipBuffer.Add( "\t无\n" );
            bool isFirst = true;
            foreach ( KeyValuePair<GameEntityTypeData, int> kv in infrastructure )
            {
                if ( isFirst )
                    isFirst = false;
                else
                    tooltipBuffer.Add( ",   " );

                tooltipBuffer.Add( kv.Key.DisplayName );
                if ( kv.Value > 1 )
                    tooltipBuffer.Add( " x" ).Add( kv.Value );
            }

            GameEntityTypeData.ReleaseTemporaryGameEntityTypeDataIntDict( infrastructure );
            #endregion

            Faction localPlayer = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( localPlayer ) )
            {
                NecromancerEmpireFactionBaseInfo localBase = localPlayer.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
                if ( localBase != null )
                {
                    Dictionary<GameEntityTypeData, int> harvest = localBase.BattleHarvest[planet];
                    if ( harvest != null && harvest.Count > 0 )
                    {
                        tooltipBuffer.Add( "\n\n<b>" ).Add( "死灵法师在本场战斗中召唤的舰船：", "ffa1a1" ).Add( "</b>\n" );
                        int size = 100;
                        if ( harvest.Count > 20 )
                            size = 50;
                        else if ( harvest.Count > 10 )
                            size = 70;
                        else if ( harvest.Count > 5 )
                            size = 80;

                        foreach ( KeyValuePair<GameEntityTypeData, int> pair in harvest )
                        {
                            tooltipBuffer.Add( "\t<size=" + size + "%>" ).Add( pair.Key.GetDisplayName(), "a1a1ff" ).Add( ": " ).Add( pair.Value ).Add( "</size>\n" );
                        }
                    }
                    if ( planet.NecromancerScienceEarned > 1 )
                    {
                        tooltipBuffer.Add( "本场战斗获得的死灵法师科技：" );
                        tooltipBuffer.Add( planet.NecromancerScienceEarned ).Add(" ");
                        tooltipBuffer.Add( ArcenExternalUIUtilities.ScienceTextColorAndIcon );
                        tooltipBuffer.Add( "\n" );
                    }
                    if ( planet.NecromancerHackingEarned > 1 )
                    {
                        tooltipBuffer.Add( "本场战斗获得的死灵法师入侵点数：" );
                        tooltipBuffer.Add( planet.NecromancerHackingEarned ).Add(" ");
                        tooltipBuffer.Add( ArcenExternalUIUtilities.HackingTextColorAndIcon );
                        tooltipBuffer.Add( "\n" );
                    }
                    if ( planet.NecromancerEssenceEarned > 1 )
                    {
                        tooltipBuffer.Add( "本场战斗获得的死灵法师精华：" );
                        tooltipBuffer.Add( planet.NecromancerEssenceEarned ).Add(" ");
                        Faction _localFac = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                        tooltipBuffer.Add( _localFac != null && _localFac.Resource1TextColorAndIcon.Length > 0 ? _localFac.Resource1TextColorAndIcon : World_AIW2.Instance.Resource1TextColorAndIcon );
                        tooltipBuffer.Add( "\n" );
                    }

                }
            }

            if ( planet.FriendlyMetalLost > 1000 && planet.HostileMetalLost > 1000 )
            {
                tooltipBuffer.Add( "\n\n" );
                tooltipBuffer.Add( "本场战斗损失的友方金属：" );
                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( tooltipBuffer, (int)planet.FriendlyMetalLost, true, false );
                tooltipBuffer.Add( ArcenExternalUIUtilities.MetalTextColorAndIcon );
                tooltipBuffer.Add( "\n" );
                tooltipBuffer.Add( "本场战斗损失的敌方金属：" );
                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( tooltipBuffer, (int)planet.HostileMetalLost, true, false );
                tooltipBuffer.Add( ArcenExternalUIUtilities.MetalTextColorAndIcon );
                tooltipBuffer.Add( "\n" );
            }

            ArcenExternalUIUtilities.ShowTooltipWide( tooltipBuffer.GetStringAndResetForNextUpdate() );
            return true;
        }
        #endregion
        
        #region GetShouldBeHidden
        public override bool GetShouldBeHidden( NotifierFillData Data )
        {
            return false;
        }
        #endregion
        
        #region ContentGetter
        public override bool ContentGetter( NotifierFillData Data, ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
        {
            int debugStage = -1;
            try
            {
                debugStage = 0;
                InitIfNeeded();

                debugStage = 1;
                Planet planet = Data.Planet;
                if ( planet == null )
                    return false;

                debugStage = 0;
                if ( Data.doPlayersLoseIfUnitLostHere )
                    Image.UpdateWith( sprite_AttackHomePlanet, true, "Attack" );
                else if ( Data.doHumansHaveADeadCommandStationHere || Data.doHumansHaveACrippledCityHere )
                    Image.UpdateWith( sprite_DeadCommandStation, true, "Attack" );
                else
                    Image.UpdateWith( sprite_AttackNonHome, true, "Attack" );

                debugStage = 20;

                ArcenDoubleCharacterBuffer buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 30;

                //enemy strength
                int enemyStr = Data.enemyStrength;
                //my and friendly strength
                int myStr = Data.myAndAlliedStrength;

                int strengthAsInt = 0;
                string colorString = string.Empty;

                if ( enemyStr > myStr )
                {
                    strengthAsInt = enemyStr;
                    colorString = Data.attackerColorHex;
                }
                else
                {
                    strengthAsInt = myStr;
                    colorString = Data.ownerColorHex;
                }

                buffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
                ArcenExternalUIUtilities.GUI_WriteStrengthIconWithColor( buffer, colorString );
                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, strengthAsInt, true, true );
                buffer.Add( "</color>\n" );

                debugStage = 40;

                if ( enemyStr <= myStr )
                {
                    strengthAsInt = enemyStr;
                    colorString = Data.attackerColorHex;
                }
                else
                {
                    strengthAsInt = myStr;
                    colorString = Data.ownerColorHex;
                }

                if ( strengthAsInt > 0 )
                {
                    buffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
                    ArcenExternalUIUtilities.GUI_WriteStrengthIconWithColor( buffer, colorString );
                    ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, strengthAsInt, true, true );
                }
                else
                {
                    buffer.Add( " " );
                }

                debugStage = 50;

                SubTexts[1].Text.FinishWritingToBuffer();

                debugStage = 60;
                
                buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( this.GetPlanetName_Safe( Data ) );

                debugStage = 70;
                SubTexts[0].Text.FinishWritingToBuffer();

                debugStage = 1000;
            }
            catch ( Exception e )
            {
                LOG.Err("Exception in {0}.ContentGetter at debugstage={1}:\n{2}", this.GetType().Name, debugStage, e);
                return false;
            }
            
            return true;
        }
        #endregion
    }
}
