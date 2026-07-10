using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class PublicDysonBuildingNotifier : NotifierBaseDataSingleton
    {
        public static PublicDysonBuildingNotifier Instance = new PublicDysonBuildingNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Building;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Building = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/spireimperial.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            return MouseHandlingResult.None;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Clear();
            if ( Data.EntityList.Count == 0 )
                return true;
            GameEntity_Squad sphere = Data.EntityList[0].GetSquad();
            if ( sphere == null )
                return true;
            DysonSidekickPerUnitBaseInfo data = sphere.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();

            int debugCode = 0;
            try
            {
                debugCode = 100;
                if (data.TimeTillDysonSphereWin > 0)
                {
                    string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime(data.TimeTillDysonSphereWin);
                    tooltipBuffer.Add("戴森球将在 ").Add(data.TimeTillDysonSphereWin.ToString(), color).Add(" 秒后完全上线。");
                }
                else
                {
                    tooltipBuffer.Add("戴森球已上线，正在生产魔像。");
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in dyson building notification mouseover handler, code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
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
                Image.UpdateWith( sprite_Building, true, "DysonBuilding" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "戴森球\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 20;
                if ( Data.EntityList.Count == 0 )
                    return true;
                GameEntity_Squad sphere = Data.EntityList[0].GetSquad();
                if ( sphere == null )
                    return true;
                debugStage = 30;
                DysonSidekickPerUnitBaseInfo data = sphere.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                buffer = SubTexts[1].Text.StartWritingToBuffer();
                debugStage = 40;
                if (data != null && data.TimeTillDysonSphereWin > 0)
                {
                    string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime(data.TimeTillDysonSphereWin);
                    buffer.Add(data.TimeTillDysonSphereWin.ToString(), color);
                }
                else
                {
                    buffer.Add( "完成", "a1ffa1" );
                }
                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicDysonBuildingNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
    public class PublicDysonDrillNotifier : NotifierBaseDataSingleton
    {
        public static PublicDysonDrillNotifier Instance = new PublicDysonDrillNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Drilling;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Drilling = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/drill.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            return MouseHandlingResult.None;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Clear();
            if ( Data.Int64List.Count == 0 )
                return true;
            GameEntity_Squad drill = Data.EntityList[0].GetSquad();
            if ( drill == null )
                return true;
            bool isAsteroidDrill = drill.TypeData.GetHasTag("DysonAsteroidDrill");
            GameEntity_Squad target = null; 
            if (isAsteroidDrill)
            {
                if ( Data.EntityList.Count < 2 )
                    return true;
                target = Data.EntityList[1].GetSquad(); // 
                if (target == null)
                    return true;
            }

            DysonSidekickPerUnitBaseInfo data = drill.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();

            int debugCode = 0;
            try
            {
                debugCode = 100;
                if ( data.TimeTillPlanetOverloaded > 0 )
                {
                    string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.TimeTillPlanetOverloaded );
                    tooltipBuffer.Add("星球 " ).Add( drill.Planet.Name, "a1ffa1" ).Add(" 将被完全摧毁并从银河网络中移除，剩余 " ).Add( data.TimeTillPlanetOverloaded.ToString(), color ).Add(" 秒。");
                }
                else if ( isAsteroidDrill )
                {
                    int cuendillarRemaining = (int)Data.Int64List[0];
                    string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.TimeTillPlanetDrilled );
                    if ( target.TypeData.GetHasTag("CuendillarAsteroid") )
                    {
                        tooltipBuffer.Add("小行星带 ");
                    }
                    else if ( target.TypeData.GetHasTag("CuendillarPlanetoid"))
                        tooltipBuffer.Add("小行星 ");
                    else
                        tooltipBuffer.Add("茧 ");

                    tooltipBuffer.Add(drill.Planet.Name, "a1ffa1").Add(" 正在被开采；剩余 ").Add( cuendillarRemaining, "ff4444" ).Add(" 库恩达");
                    if ( !target.TypeData.GetHasTag("ReaperChrysalis") )
                        tooltipBuffer.Add("，将在 ").Add( data.TimeTillPlanetDrilled.ToString( ), color).Add(" 秒后被摧毁。");
                    else
                        tooltipBuffer.Add("。它将在 ").Add( data.TimeTillPlanetDrilled.ToString( ), color).Add(" 秒后因缺乏库恩达而崩溃。如果它先孵化，你将面临一场战斗。");

                    tooltipBuffer.Add("\n").Add("新的运输船将在 ").Add( (data.TimeForNextTransport - World_AIW2.Instance.GameSecond), "0044ff" ).Add(" 秒后派遣。");
                    if (data.TotalCuendillarDrilled > 0)
                    {
                        tooltipBuffer.Add("\n").Add("到目前为止你已开采 ").Add( (data.TotalCuendillarDrilled), "ff4444" ).Add(" 库恩达 ").Add(" 并派遣了 ").Add( data.TransportsSent, "a1ffa1" ).Add(" 艘运输船从这个 ");

                        if ( target.TypeData.GetHasTag("CuendillarPlanetoid"))
                            tooltipBuffer.Add("小行星。");
                        else if ( target.TypeData.GetHasTag("CuendillarAsteroid") )
                            tooltipBuffer.Add("小行星带。");
                        else
                            tooltipBuffer.Add("茧。");
                    }
                }
                else
                {
                    int cuendillarRemaining = (int)Data.Int64List[0];
                    string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.TimeTillPlanetDrilled );
                    tooltipBuffer.Add("星球 " ).Add( drill.Planet.Name, "a1ffa1" ).Add(" 正在被开采；剩余 ").Add( cuendillarRemaining, "ff4444" ).Add(" 库恩达，星球将在 ").Add( data.TimeTillPlanetDrilled.ToString( ), color).Add(" 秒后被蹂躏。");
                    tooltipBuffer.Add("\n").Add("新的运输船将在 ").Add( (data.TimeForNextTransport - World_AIW2.Instance.GameSecond), "0044ff" ).Add(" 秒后派遣。 ");
                    if ( data.TotalCuendillarDrilled > 0 )
                        tooltipBuffer.Add("\n").Add("到目前为止你已开采 ").Add( (data.TotalCuendillarDrilled), "ff4444" ).Add(" 库恩达 ").Add(" 并派遣了 " ).Add( data.TransportsSent, "a1ffa1" ).Add(" 艘运输船从这个星球。");
                    tooltipBuffer.Add("\n\n").Add("警告！", "ffa1a1").Add(" 钻探完成后，星球将被蹂躏；这将释放巨大能量，摧毁星球上几乎所有的舰船和建筑（包括金属发生器、ARS 等）。" );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in dyson drill notification mouseover handler, code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
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
                Image.UpdateWith( sprite_Drilling, true, "DysonDrilling" );
                if ( Data.EntityList.Count == 0 )
                    return true;
                GameEntity_Squad drill = Data.EntityList[0].GetSquad();
                if ( drill == null )
                    return true;
                DysonSidekickPerUnitBaseInfo data = drill.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                if ( data == null )
                    return true;
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                if ( data.TimeTillPlanetOverloaded > 0 )
                    buffer.Add( "过载\n" );
                else
                    buffer.Add( "钻探\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 20;
                if ( Data.EntityList.Count == 0 )
                    return true;
                buffer = SubTexts[1].Text.StartWritingToBuffer();
                if ( data.TimeTillPlanetDrilled > 0 )
                {
                    string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.TimeTillPlanetDrilled );
                    buffer.Add( data.TimeTillPlanetDrilled.ToString(), color );
                }
                else
                {
                    string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.TimeTillPlanetOverloaded );
                    buffer.Add( data.TimeTillPlanetOverloaded.ToString(), color );
                }
                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicDysonDrillNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
    public class PublicAIDrillNotifier : NotifierBaseDataSingleton
    {
        public static PublicAIDrillNotifier Instance = new PublicAIDrillNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Drilling;
        private static bool hasInitialized = false;
        private static int index = 0;
        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            var dict = ExternalIconDictionaryTable.Instance.GetRowByName("Ships3");
            sprite_Drilling = dict.GetGUISpriteByName("ZenithMinerProbe");
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            if ( index >= Data.EntityList.Count )
                index = 0; //since index is a static it could be a stale value from before, so check here if the index is reasonable

            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( Data.EntityList[index].Planet, false );
            else
                World_AIW2.Instance.SwitchViewToPlanet( Data.EntityList[index].Planet );
            index++;
            if ( index >= Data.EntityList.Count )
                index = 0;
            return MouseHandlingResult.None;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Clear();
            if (Data.EntityList.Count == 0 )
            {
                return true;
            }
            GameEntity_Squad drill = Data.EntityList[0].GetSquad();
            if (drill == null)
            {
                return true;
            }

            int debugCode = 0;
            try
            {
                debugCode = 100;
                tooltipBuffer.Add("AI 正在 ").Add(drill.Planet.Name, "a1ffa1").Add(" 上钻探库恩达。钻机将定期派遣库恩达运输船返回基地；这些运输船可以被攻击以窃取库恩达。");
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in ai drill notification mouseover handler, code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
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
                Image.UpdateWith( sprite_Drilling, true, "AIDrilling" );
                if ( Data.EntityList.Count == 0 )
                    return true;
                GameEntity_Squad drill = Data.EntityList[0].GetSquad();
                if ( drill == null )
                    return true;

                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add(drill.Planet.Name);
                SubTexts[0].Text.FinishWritingToBuffer();
                buffer = SubTexts[1].Text.StartWritingToBuffer();
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicAIDrillNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
    public class PublicDysonRavagerAssaultNotifier : NotifierBaseDataSingleton
    {
        public static PublicDysonRavagerAssaultNotifier Instance = new PublicDysonRavagerAssaultNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Assault;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Assault = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/volcanicwarning.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            return MouseHandlingResult.None;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Clear();
            if ( Data.Int64List.Count == 0 )
                return true;
            int time = (int)Data.Int64List[0];
            if ( time > 0 )
            {
                string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( time );
                tooltipBuffer.Add("收割者将在 ").Add( time.ToString(), color ).Add(" 秒后发动攻击。\n");
                tooltipBuffer.Add("\t此攻击产生的蹂躏者将试图钻探和蹂躏星球，在此过程中产生大量新的敌方舰船。");
            }
            else
                tooltipBuffer.Add("收割者即将发动攻击。");
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
                Image.UpdateWith( sprite_Assault, true, "RavagerAssault" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "攻击\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 20;

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                int time = -1;
                if ( Data.Int64List.Count > 0 )
                    time = (int)Data.Int64List[0];
                if ( time < 0 )
                    buffer.Add("即将");
                else
                {
                    string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( time );
                    buffer.Add( time.ToString(), color );
                }

                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicSpireDebrisNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
    public class PublicDysonLunarInvasionNotifier : NotifierBaseDataSingleton
    {
        public static PublicDysonLunarInvasionNotifier Instance = new PublicDysonLunarInvasionNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_LunarInvasion;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_LunarInvasion = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/nomadcrash.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            return MouseHandlingResult.None;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Clear();
            if ( Data.Int64List.Count == 0 )
                return true;
            int time = (int)Data.Int64List[0];
            if ( time > 0 )
            {
                string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( time );
                tooltipBuffer.Add("收割者将在 ").Add( time.ToString(), color ).Add(" 秒后发动月球入侵。\n");
                tooltipBuffer.Add("\t一扇传送门和一个收割者月球将生成。击败月球将允许你拥有自己的月球");
            }
            else
                tooltipBuffer.Add("收割者即将发动月球入侵。");
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
                Image.UpdateWith( sprite_LunarInvasion, true, "LunarInvasion" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "月球\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 20;

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                int time = -1;
                if ( Data.Int64List.Count > 0 )
                    time = (int)Data.Int64List[0];
                if ( time < 0 )
                    buffer.Add("即将");
                else
                {
                    string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( time );
                    buffer.Add( time.ToString(), color );
                }

                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicSpireDebrisNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
    
    public class PublicRavagerNotifier : NotifierBaseDataSingleton
    {
        public static PublicRavagerNotifier Instance = new PublicRavagerNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Ravager;
        private static bool hasInitialized = false;
        private static int index = 0;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Ravager = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/activelymining.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            if ( index >= Data.EntityList.Count )
                index = 0; //since index is a static it could be a stale value from before, so check here if the index is reasonable

            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( Data.EntityList[index].Planet, false );
            else
                World_AIW2.Instance.SwitchViewToPlanet( Data.EntityList[index].Planet );
            index++;
            if ( index >= Data.EntityList.Count )
                index = 0;
            return MouseHandlingResult.None;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Clear();
            if ( Data.EntityList.Count == 0 )
                return true;
            tooltipBuffer.Add("地图上有蹂躏者！它们将蹂躏星球，使你无法获得库恩达，并在此过程中产生大量舰船。\n");
            tooltipBuffer.Add("以下是蹂躏者：\n");
            for ( int i = 0; i < Data.EntityList.Count; i++ )
            {
                GameEntity_Squad ravager = Data.EntityList[i].GetSquad();
                if ( ravager == null )
                    continue;
                if ( ravager.TypeData.IsMobile )
                    tooltipBuffer.Add("\t在 ").Add(ravager.Planet.Name, "a1ffa1").Add(" 正在前往一个星球进行蹂躏。\n");
                else
                {
                    ReapersPerUnitBaseInfo data = ravager.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                    if ( data == null )
                        tooltipBuffer.Add("\t在 ").Add(ravager.Planet.Name, "ffbba1").Add(" 当前正在钻探并生产敌方舰船。\n");
                    else
                    {
                        string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.SecondsTillRavage );
                        tooltipBuffer.Add("\t星球 ").Add(ravager.Planet.Name, "ffbba1" ).Add(" 将在 ").Add( data.SecondsTillRavage.ToString(), color ).Add( " 秒后被蹂躏，并将在 ").Add( data.SecondsTillTroopSpawn, "a1a1ff" ).Add(" 秒后下次生产舰船。\n");
                    }
                }
                    
            }
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
                Image.UpdateWith( sprite_Ravager, true, "Ravager" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "蹂躏者\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 20;

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                if ( Data.EntityList.Count > 0 )
                {
                    buffer.Add( Data.EntityList.Count );
                }
                else
                {
                    GameEntity_Squad ravager = Data.EntityList[0].GetSquad();
                    if ( ravager != null )
                        buffer.Add(ravager.Planet.Name );
                    else
                        buffer.Add("1");
                }

                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicSpireDebrisNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
    public class PublicLarvaNotifier : NotifierBaseDataSingleton
    {
        public static PublicLarvaNotifier Instance = new PublicLarvaNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Larva;
        private static bool hasInitialized = false;
        private static int index = 0;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Larva = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/meteor1.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            if ( index >= Data.EntityList.Count )
                index = 0; //since index is a static it could be a stale value from before, so check here if the index is reasonable

            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( Data.EntityList[index].Planet, false );
            else
                World_AIW2.Instance.SwitchViewToPlanet( Data.EntityList[index].Planet );
            index++;
            if ( index >= Data.EntityList.Count )
                index = 0;
            return MouseHandlingResult.None;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Clear();
            if ( Data.EntityList.Count == 0 )
                return true;
            tooltipBuffer.Add("地图上有收割者幼虫！如果不消灭它们，它们将变成茧。\n");
            tooltipBuffer.Add("以下是你能看到的幼虫：\n");
            for ( int i = 0; i < Data.EntityList.Count; i++ )
            {
                GameEntity_Squad larva = Data.EntityList[i].GetSquad();
                if ( larva == null )
                    continue;
                if ( larva.Planet.IntelLevel == PlanetIntelLevel.Unexplored)
                    continue;
                if ( larva.TypeData.IsMobile )
                    tooltipBuffer.Add("\t在 ").Add(larva.Planet.Name, "a1ffa1");
                Planet dest = larva.GetDestinationPlanet();
                if ( larva.Planet != dest )
                    tooltipBuffer.Add(" 并正在前往 ").Add(dest.Name, "ffa1a1");
                tooltipBuffer.Add("\n");
            }
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
                Image.UpdateWith( sprite_Larva, true, "Larva" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "幼虫\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 20;

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                if ( Data.EntityList.Count > 0 )
                {
                    buffer.Add( Data.EntityList.Count );
                }
                else
                {
                    GameEntity_Squad larva = Data.EntityList[0].GetSquad();
                    if ( larva != null )
                        buffer.Add(larva.Planet.Name );
                    else
                        buffer.Add("1");
                }

                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicSpireDebrisNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
    public class PublicReaperChrysalisNotifier : NotifierBaseDataSingleton
    {
        public static PublicReaperChrysalisNotifier Instance = new PublicReaperChrysalisNotifier();

        //these are just data from asset bundles, that's definitely okay to be static
        private static UnityEngine.Sprite sprite_Chrysalis;
        private static bool hasInitialized = false;
        private static int index = 0;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite_Chrysalis = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/wormholeinvasion.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            if ( index >= Data.EntityList.Count )
                index = 0; //since index is a static it could be a stale value from before, so check here if the index is reasonable

            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                Engine_AIW2.Instance.PresentationLayer.CenterGalaxyViewOnPlanet( Data.EntityList[index].Planet, false );
            else
                World_AIW2.Instance.SwitchViewToPlanet( Data.EntityList[index].Planet );
            index++;
            if ( index >= Data.EntityList.Count )
                index = 0;
            return MouseHandlingResult.None;
        }

        //this is ok IF we assume that only one could possibly be hovered over at a time.  For a brief part of a second if you somehow hover over two, you'd get garbled text.
        //so I'm going to call this ok.
        public override bool MouseoverHandler( NotifierFillData Data )
        {
            tooltipBuffer.Clear();
            if ( Data.EntityList.Count == 0 )
                return true;
            if (Data.EntityList.Count == 1)
            {
                tooltipBuffer.Add("有 ").Add( Data.EntityList.Count, "ffcc22" ).Add(" 个收割者茧在地图上，正准备释放传送门！\n");
                tooltipBuffer.Add("以下是该茧：\n\n");
            }
            else
            {
                tooltipBuffer.Add("有 ").Add( Data.EntityList.Count, "ffcc22" ).Add(" 个收割者茧在地图上，正准备释放传送门！\n");
                tooltipBuffer.Add("以下是这些茧：\n\n");
            }
            for ( int i = 0; i < Data.EntityList.Count; i++ )
            {
                GameEntity_Squad chrysalis = Data.EntityList[i].GetSquad();
                if ( chrysalis == null )
                    continue;
                ReapersPerUnitBaseInfo data = chrysalis.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                if ( data != null )
                {
                    int spawnTime = data.ChrysalisHatchTime - World_AIW2.Instance.GameSecond;

                    tooltipBuffer.Add("茧 ").Add(chrysalis.Planet.Name, "ffbba1").Add("\n");
                    if (spawnTime >= 0)
                    {
                        string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( spawnTime );
                        tooltipBuffer.Add("\t将在 ").Add(spawnTime.ToString(), color).Add(" 秒后孵化，生成传送门和许多其他敌方舰船。\n");
                    }
                    else
                        tooltipBuffer.Add("\t它即将孵化，生成传送门和许多其他敌方舰船。\n");
                    tooltipBuffer.Add("\t剩余 ").Add( data.CuendillarRemaining, "ff4444" ).Add(" 库恩达\n");
                    if ( Data.BoolList.Count > 0 &&
                         Data.BoolList[0] )
                    {
                        tooltipBuffer.Add("\tAI 目前正在从这个茧提取库恩达\n\t\t这些库恩达将被转化为 AI 的可怕战舰。");
                    }
                }
            }
            tooltipBuffer.Add("\n\n使用小型钻机提取库恩达将削弱茧，降低孵化时生成敌人的强度。");
            tooltipBuffer.Add("\n如果你能钻取所有库恩达，它将崩溃。");

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
                Image.UpdateWith( sprite_Chrysalis, true, "Chrysalis" );
                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                buffer.Add( "茧\n" );
                if ( Data.BoolList.Count > 0 &&
                     Data.BoolList[0] )
                    buffer.Add( "AI 钻探\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 20;

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                if ( Data.EntityList.Count > 1 )
                {
                    buffer.Add( Data.EntityList.Count );
                }
                else
                {
                    GameEntity_Squad chrysalis = Data.EntityList[0].GetSquad();
                    if (chrysalis != null)
                    {
                        ReapersPerUnitBaseInfo data = chrysalis.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                        if (data != null)
                        {
                            int spawnTime = data.ChrysalisHatchTime - World_AIW2.Instance.GameSecond;
                            string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( spawnTime );
                            buffer.Add(chrysalis.Planet.Name, color );
                        }
                        else
                            buffer.Add("1");
                    }
                    else
                        buffer.Add("1");
                }

                debugStage = 30;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in PublicReaperChrysalisNotifier.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }

            return true;
        }
    }
}
