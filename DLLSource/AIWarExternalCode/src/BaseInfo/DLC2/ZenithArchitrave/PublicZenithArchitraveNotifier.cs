using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicZenithArchitraveNotifier : NotifierBaseDataSingleton
    {
        public static PublicZenithArchitraveNotifier Instance = new PublicZenithArchitraveNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_ZenithArchitrave;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_ZenithArchitrave = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/zenitharchitrave.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            //nothing to do on galaxy map hover
            //World_AIW2.Instance.FocusedPlanetForMapDarkening = Data.Planet;

            tooltipBuffer.Clear();
            bool debug = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" );
            string debugOutput = "";
            string nameToUse = "this one";
            string architravePlurality = "拱顶石阵营拥有";
            tooltipBuffer.Add( "一旦某个天顶拱顶石占据过多星球，其他拱顶石将联合对抗它。\n" ).Add( "目前" );
            List<Faction> tooLargeFactions = Data.FactionList;
            List<Faction> smallerFactions = Data.FactionList2;
            if ( tooLargeFactions.Count == 1 )
            {
                ZenithArchitraveFactionBaseInfo globaldata = tooLargeFactions[0].TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                tooltipBuffer.Add( "有" ).Add( tooLargeFactions[0].StartFactionColourForLog() + "天顶拱顶石" ).Add( "</color>，" );
                architravePlurality = "拱顶石阵营拥有";
                if ( smallerFactions.Count == 1 )
                {
                    tooltipBuffer.Add( "另一个" );
                    tooltipBuffer.Add( smallerFactions[0].StartFactionColourForLog() + "天顶拱顶石</color> " );
                    if ( globaldata.TimeUntilOtherArchitravesShouldAttackMe > 0 )
                        tooltipBuffer.Add( "将在" ).AddHoursAndMinutes( globaldata.TimeUntilOtherArchitravesShouldAttackMe, "ff0000" ).Add( "后进攻。" );
                    else
                        tooltipBuffer.Add( "正在进攻。" );
                }
                else
                {
                    tooltipBuffer.Add( "剩余" ).Add( smallerFactions.Count, "a1ffa1" ).Add( " (" );
                    for ( int i = 0; i < smallerFactions.Count; i++ )
                    {
                        if ( debug ) debugOutput = " " + smallerFactions[i].FactionIndex;
                        nameToUse = "this";
                        if ( smallerFactions[i].FactionNameOrEmpty != "" )
                            nameToUse = smallerFactions[i].FactionNameOrEmpty;

                        tooltipBuffer.Add( smallerFactions[i].StartFactionColourForLog() + nameToUse + debugOutput + "</color>" );
                        if ( i != smallerFactions.Count - 1 )
                            tooltipBuffer.Add( ", " );
                        if ( i == smallerFactions.Count - 2 )
                            tooltipBuffer.Add( "和" );
                    }
                    tooltipBuffer.Add( ") 个拱顶石阵营" );
                    if ( globaldata.TimeUntilOtherArchitravesShouldAttackMe > 0 )
                        tooltipBuffer.Add( "将在" ).AddHoursAndMinutes( globaldata.TimeUntilOtherArchitravesShouldAttackMe, "ff0000" ).Add( "后联合对抗。" );
                    else
                        tooltipBuffer.Add( "正在联合对抗。" );

                }
            }
            else
            {
                if ( Data.WarStarted )
                {
                    tooltipBuffer.Add( "超过限制的拱顶石阵营有：\n" );
                    for ( int i = 0; i < tooLargeFactions.Count; i++ )
                    {
                        ZenithArchitraveFactionBaseInfo globaldata = tooLargeFactions[i].TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                        if ( debug ) debugOutput = " " + tooLargeFactions[i].FactionIndex;
                        nameToUse = "this one";
                        if ( tooLargeFactions[i].FactionNameOrEmpty != "" )
                            nameToUse = tooLargeFactions[i].FactionNameOrEmpty;
                        tooltipBuffer.Add( tooLargeFactions[i].StartFactionColourForLog() + "\t" + nameToUse + " " + debugOutput ).Add( "</color> " );
                        if ( globaldata.TimeUntilOtherArchitravesShouldAttackMe > 0 )
                        {
                            string timerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime( globaldata.TimeUntilOtherArchitravesShouldAttackMe ); //timerColor gets more red the sooner the pioneers will appear
                            tooltipBuffer.Add( "(" ).Add( globaldata.TimeUntilOtherArchitravesShouldAttackMe.ToString(), timerColor ).Add( ")" );
                        }
                        tooltipBuffer.Add( "\n" );
                    }
                    if ( smallerFactions.Count <= 1 )
                        tooltipBuffer.Add( "所有拱顶石阵营处于混战状态。" );
                    else
                    {
                        tooltipBuffer.Add( "剩余" ).Add( smallerFactions.Count, "a1ffa1" ).Add( " (" );
                        for ( int i = 0; i < smallerFactions.Count; i++ )
                        {
                            if ( debug ) debugOutput = " " + smallerFactions[i].FactionIndex;
                            nameToUse = "this";
                            if ( tooLargeFactions[i].FactionNameOrEmpty != "" )
                                nameToUse = tooLargeFactions[i].FactionNameOrEmpty;

                            tooltipBuffer.Add( smallerFactions[i].StartFactionColourForLog() + nameToUse + debugOutput + "</color>" );
                            if ( i != smallerFactions.Count - 1 )
                                tooltipBuffer.Add( ", " );
                            if ( i == smallerFactions.Count - 2 )
                                tooltipBuffer.Add( "和" );
                        }
                        tooltipBuffer.Add( ") 个拱顶石阵营正在联合对抗它们。" );
                    }
                }
                else
                {
                    tooltipBuffer.Add( "即将超过限制的拱顶石阵营有：\n" );
                    for ( int i = 0; i < tooLargeFactions.Count; i++ )
                    {
                        ZenithArchitraveFactionBaseInfo globaldata = tooLargeFactions[i].TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                        if ( debug ) debugOutput = " " + tooLargeFactions[i].FactionIndex;
                        tooltipBuffer.Add( tooLargeFactions[i].StartFactionColourForLog() + "\t这个" + debugOutput ).Add( "</color> " );
                        if ( globaldata.TimeUntilOtherArchitravesShouldAttackMe > 0 )
                        {
                            string timerColor = ArcenExternalUIUtilities.GetColorForNomadMoveTime( globaldata.TimeUntilOtherArchitravesShouldAttackMe ); //timerColor gets more red the sooner the pioneers will appear
                            tooltipBuffer.Add( "(" ).Add( globaldata.TimeUntilOtherArchitravesShouldAttackMe.ToString(), timerColor ).Add( "" );
                        }
                        tooltipBuffer.Add( "\n" );
                    }
                    if ( smallerFactions.Count <= 1 )
                        tooltipBuffer.Add( "所有拱顶石阵营将进入混战状态。" );
                    else
                    {
                        tooltipBuffer.Add( "剩余" ).Add( smallerFactions.Count, "a1ffa1" ).Add( " (" );
                        for ( int i = 0; i < smallerFactions.Count; i++ )
                        {
                            if ( debug ) debugOutput = " " + smallerFactions[i].FactionIndex;
                            tooltipBuffer.Add( smallerFactions[i].StartFactionColourForLog() + "this" + debugOutput + "</color>" );
                            if ( i != smallerFactions.Count - 1 )
                                tooltipBuffer.Add( ", " );
                            if ( i == smallerFactions.Count - 2 )
                                tooltipBuffer.Add( "和" );
                        }
                        tooltipBuffer.Add( ") 个拱顶石阵营将联合对抗它们。" );
                    }
                }
            }

            tooltipBuffer.Add( "\n内战中的拱顶石阵营异常强大，会生产大量魔像互相战斗。一旦挑起战争的" ).Add( architravePlurality ).Add( "被削弱，战争将结束，其他拱顶石阵营将撤回各自的领地。\n您的单位和星球可能会被卷入战火。" );

            if ( Data.anyTruce )
                tooltipBuffer.Add( "\n在内战结束之前，拱顶石阵营将无视与您的任何休战协议。" );
            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
            return true;
        }

        public override bool GetShouldBeHidden( NotifierFillData Data )
        {
            return false;
        }

        public override bool ContentGetter( NotifierFillData Data, ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
        {
            int debugStage = -1;
            try
            {
                debugStage = 0;
                InitIfNeeded();

                debugStage = 10;
                Image.UpdateWith( sprite_ZenithArchitrave, true, "ZA" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "内战" );
                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                debugStage = 30;
                if ( !Data.WarStarted )
                {
                    List<Faction> tooLargeFactions = Data.FactionList;
                    List<Faction> smallerFactions = Data.FactionList2;
                    int shortestTime = -1;
                    for ( int i = 0; i < tooLargeFactions.Count; i++ )
                    {
                        ZenithArchitraveFactionBaseInfo globaldata = tooLargeFactions[i].TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                        if ( shortestTime == -1 ||
                             globaldata.TimeUntilOtherArchitravesShouldAttackMe < shortestTime )
                            shortestTime = globaldata.TimeUntilOtherArchitravesShouldAttackMe;
                    }
                    buffer.AddSecondsRemaining( shortestTime, TimeIntensity.TenMinutes );
                }
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicZenithArchitraveNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
