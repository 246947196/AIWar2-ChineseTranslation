using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class IAttackAPlanetNotifier : NotifierBaseDataSingleton
    {
        #region Static Data
        public static IAttackAPlanetNotifier Instance = new IAttackAPlanetNotifier();
        private static UnityEngine.Sprite sprite_AttackEnemy;
        private static bool hasInitialized = false;
        #endregion

        #region InitIfNeeded
        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;

            sprite_AttackEnemy = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/attackenemy.png" );
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

        #region GetShouldBeHidden
        public override bool GetShouldBeHidden( NotifierFillData Data )
        {
            return false;
        }
        #endregion

        #region MouseoverHandler
        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            int debugStage = 0;
            try
            {
                debugStage = 1000;
                Faction controllingFactionOrNull = Data.controllingFaction;

                debugStage = 2000;
                Planet planet = Data.Planet;
                if ( planet == null )
                    return false;

                debugStage = 3000;
                //galaxy map hover
                World_AIW2.Instance.FocusedPlanetForMapDarkening = planet;

                debugStage = 4000;
                tooltipBuffer.Clear();
                if ( Data.isNeutralWorld )
                    tooltipBuffer.Add( "We are fighting over the neutral world " ).Add( planet.Name );
                else
                    tooltipBuffer.Add( "We are attacking the world " ).Add( planet.Name );
                debugStage = 5000;
                if ( controllingFactionOrNull != null )
                    tooltipBuffer.Add( ", which is controlled by " ).Add( controllingFactionOrNull.GetDisplayName() );

                PlanetFaction faction;

                debugStage = 6000;
                #region Allied Attackers
                tooltipBuffer.Add( "\n<b>Allied Attackers</b>\n" );
                for ( int i = 0; i < planet.Factions.Count; i++ )
                {
                    debugStage = 6100;
                    faction = planet.Factions[i];
                    debugStage = 6200;
                    if ( faction.Faction.Type == FactionType.NaturalObject || !faction.GetIsFriendlyToLocalFaction() )
                        continue;
                    debugStage = 6300;
                    ArcenExternalUIUtilities.WriteSideContents( faction, tooltipBuffer, !faction.GetIsLocalFaction() );
                }
                #endregion

                debugStage = 7000;
                #region Enemy Defenders
                tooltipBuffer.Add( "\n\n<b>Enemy Defenders</b>\n" );
                for ( int i = 0; i < planet.Factions.Count; i++ )
                {
                    debugStage = 7100;
                    faction = planet.Factions[i];
                    debugStage = 7200;
                    if ( faction.Faction.Type == FactionType.NaturalObject || faction.GetIsFriendlyToLocalFaction() )
                        continue;
                    debugStage = 7300;
                    ArcenExternalUIUtilities.WriteSideContents( faction, tooltipBuffer, true );
                }
                #endregion

                debugStage = 8000;
                #region Capturables
                tooltipBuffer.Add( "\n<b>Capturables</b>\n" );

                Dictionary<GameEntityTypeData, int> capturables = GameEntityTypeData.GetTemporaryGameEntityTypeDataIntDict( "IAttackAPlanetNotifier-MouseoverHandler-capturables", 10f );
                if ( capturables == null ) //blocked for teardown/shutdown; bail
                    return false;

                debugStage = 8100;
                for ( int i = 0; i < planet.Factions.Count; i++ )
                {
                    debugStage = 8200;
                    faction = planet.Factions[i];
                    debugStage = 8300;
                    if ( faction.Faction.Type != FactionType.NaturalObject && faction.GetIsFriendlyToLocalFaction() )
                        continue; //skip stuff we already have
                    debugStage = 8400;
                    foreach ( GameEntity_Squad entity in faction.Entities.Squads( "Capturable" ) )
                    {
                        debugStage = 8500;
                        if ( capturables.ContainsKey( entity.TypeData ) )
                            capturables[entity.TypeData] += 1;
                        else
                            capturables[entity.TypeData] = 1;
                    }
                    debugStage = 8800;
                    foreach ( GameEntity_Squad entity in faction.Entities.Squads( "HackAsIfCapturable" ) )
                    {
                        debugStage = 8900;
                        if ( capturables.ContainsKey( entity.TypeData ) )
                            capturables[entity.TypeData] += 1;
                        else
                            capturables[entity.TypeData] = 1;
                    }
                }
                debugStage = 9100;
                if ( capturables.Count == 0 )
                    tooltipBuffer.Add( "\tNone\n" );
                debugStage = 9200;
                bool isFirst = true;
                foreach ( KeyValuePair<GameEntityTypeData, int> kv in capturables )
                {
                    debugStage = 9300;
                    if ( isFirst )
                        isFirst = false;
                    else
                        tooltipBuffer.Add( ",   " );

                    debugStage = 9400;
                    tooltipBuffer.Add( kv.Key.DisplayName );
                    debugStage = 9500;
                    if ( kv.Value > 1 )
                        tooltipBuffer.Add( " x" ).Add( kv.Value );
                }

                debugStage = 9900;
                GameEntityTypeData.ReleaseTemporaryGameEntityTypeDataIntDict( capturables );
                #endregion

                debugStage = 11200;
                Faction localPlayer = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                debugStage = 11300;
                if ( localPlayer != null && NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( localPlayer ) )
                {
                    debugStage = 11400;
                    NecromancerEmpireFactionBaseInfo localBase = localPlayer.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
                    debugStage = 11500;
                    if ( localBase != null )
                    {
                        debugStage = 11600;
                        Dictionary<GameEntityTypeData, int> harvest = localBase.BattleHarvest[planet];
                        tooltipBuffer.Add( "\n\n" );
                        debugStage = 11700;
                        if ( harvest.Count > 0 )
                        {
                            debugStage = 11800;
                            tooltipBuffer.Add( "\n\n<b>" ).Add( "Necromancer Ships Raised During This Battle:", "ffa1a1" ).Add( "</b>\n" );
                            int size = 100;
                            if ( harvest.Count > 20 )
                                size = 50;
                            else if ( harvest.Count > 10 )
                                size = 70;
                            else if ( harvest.Count > 5 )
                                size = 80;

                            debugStage = 12100;
                            foreach ( KeyValuePair<GameEntityTypeData, int> pair in harvest )
                            {
                                debugStage = 12200;
                                tooltipBuffer.Add( "\t<size=" + size + "%>" ).Add( pair.Key.GetDisplayName(), "a1a1ff" ).Add( ": " ).Add( pair.Value ).Add( "</size>\n" );
                            }
                        }
                        if ( planet.NecromancerScienceEarned > 1 )
                        {
                            tooltipBuffer.Add( "Necromancer Science earned this battle: " );
                            tooltipBuffer.Add( planet.NecromancerScienceEarned ).Add(" ");
                            tooltipBuffer.Add( ArcenExternalUIUtilities.ScienceTextColorAndIcon );
                            tooltipBuffer.Add( "\n" );
                        }
                        if ( planet.NecromancerHackingEarned > 1 )
                        {
                            tooltipBuffer.Add( "Necromancer Hacking earned this battle: " );
                            tooltipBuffer.Add( planet.NecromancerHackingEarned ).Add(" ");
                            tooltipBuffer.Add( ArcenExternalUIUtilities.HackingTextColorAndIcon );
                            tooltipBuffer.Add( "\n" );
                        }
                        if ( planet.NecromancerEssenceEarned > 1 )
                        {
                            tooltipBuffer.Add( "Necromancer Essence earned this battle: " );
                            tooltipBuffer.Add( planet.NecromancerEssenceEarned ).Add(" ");
                            Faction _localFac = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                            tooltipBuffer.Add( _localFac != null && _localFac.Resource1TextColorAndIcon.Length > 0 ? _localFac.Resource1TextColorAndIcon : World_AIW2.Instance.Resource1TextColorAndIcon );
                            tooltipBuffer.Add( "\n" );
                        }

                    }
                }

                debugStage = 13100;
                if ( planet.FriendlyMetalLost > 1000 && planet.HostileMetalLost > 1000 )
                {
                    debugStage = 13200;
                    tooltipBuffer.Add( "\n\n" );
                    tooltipBuffer.Add( "Friendly metal lost this battle: " );
                    ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( tooltipBuffer, (int)planet.FriendlyMetalLost, true, false );
                    debugStage = 13300;
                    tooltipBuffer.Add( ArcenExternalUIUtilities.MetalTextColorAndIcon );
                    tooltipBuffer.Add( "\n" );
                    tooltipBuffer.Add( "Hostile metal lost this battle: " );
                    ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( tooltipBuffer, (int)planet.HostileMetalLost, true, false );
                    tooltipBuffer.Add( ArcenExternalUIUtilities.MetalTextColorAndIcon );
                    tooltipBuffer.Add( "\n" );
                }

                debugStage = 14100;
                ArcenExternalUIUtilities.ShowTooltipWide( tooltipBuffer.GetStringAndResetForNextUpdate() );
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Exception IAttackAPlanetNotifier.MouseoverHandler exception at debugStage " + debugStage + ": " + e, Verbosity.ShowAsError );
            }
            return true;
        }
        #endregion

        #region GetPlanetName_Safe
        public string GetPlanetName_Safe( NotifierFillData Data )
        {
            Planet plan = Data.Planet;
            if ( plan == null )
                return String.Empty;
            return plan.Name;
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
                
                debugStage = 0;
                Image.UpdateWith( sprite_AttackEnemy, true, "Human_Fin" );
                
                debugStage = 3;

                debugStage = 4;
                ArcenDoubleCharacterBuffer buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 10;

                //enemy strength
                int enemyStr = Data.enemyStrength;
                //my and friendly strength
                int myStr = Data.myAndAlliedStrength;

                int strengthAsInt = 0;
                string colorString = string.Empty;

                if ( enemyStr > myStr )
                {
                    strengthAsInt = enemyStr;
                    colorString = Data.enemyOwnerColorHex;
                }
                else
                {
                    strengthAsInt = myStr;
                    colorString = Data.alliedAttackerColorHex;
                }
                if ( colorString.Length > 0 )
                    buffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
                ArcenExternalUIUtilities.GUI_WriteStrengthIconWithColor( buffer, colorString );
                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, strengthAsInt, true, true );
                if ( colorString.Length > 0 )
                    buffer.Add( "</color>" );
                buffer.Add( "\n" );

                debugStage = 20;

                if ( enemyStr <= myStr )
                {
                    strengthAsInt = enemyStr;
                    colorString = Data.enemyOwnerColorHex;
                }
                else
                {
                    strengthAsInt = myStr;
                    colorString = Data.alliedAttackerColorHex;
                }

                if ( colorString.Length > 0 )
                    buffer.Add( "<color=#" ).Add( colorString ).Add( ">" );
                ArcenExternalUIUtilities.GUI_WriteStrengthIconWithColor( buffer, colorString );
                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, strengthAsInt, true, true );

                debugStage = 30;

                SubTexts[1].Text.FinishWritingToBuffer();

                debugStage = 31;

                //planet name
                buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( this.GetPlanetName_Safe( Data ) );

                debugStage = 40;
                SubTexts[0].Text.FinishWritingToBuffer();

                debugStage = 1000;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in IAttackAPlanetNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            
            return true;
        }
        #endregion
    }
}
