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
                    tooltipBuffer.Add("The Dyson Sphere will come fully online in ").Add(data.TimeTillDysonSphereWin.ToString(), color).Add(" seconds.");
                }
                else
                {
                    tooltipBuffer.Add("The Dyson Sphere is online and producing Golems.");
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
                buffer.Add( "Dyson\n" );
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
                    buffer.Add( "Complete", "a1ffa1" );
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
                    tooltipBuffer.Add("The planet " ).Add( drill.Planet.Name, "a1ffa1" ).Add(" will be totally destroyed and removed from the galaxy network in " ).Add( data.TimeTillPlanetOverloaded.ToString(), color ).Add(" seconds.");
                }
                else if ( isAsteroidDrill )
                {
                    int cuendillarRemaining = (int)Data.Int64List[0];
                    string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.TimeTillPlanetDrilled );
                    if ( target.TypeData.GetHasTag("CuendillarAsteroid") )
                    {
                        tooltipBuffer.Add("An Asteroid on ");
                    }
                    else if ( target.TypeData.GetHasTag("CuendillarPlanetoid"))
                        tooltipBuffer.Add("A Planetoid on ");
                    else
                        tooltipBuffer.Add("A Chrysalis on ");

                    tooltipBuffer.Add(drill.Planet.Name, "a1ffa1").Add(" is being mined ; There is ").Add( cuendillarRemaining, "ff4444" ).Add(" cuendillar left");
                    if ( !target.TypeData.GetHasTag("ReaperChrysalis") )
                        tooltipBuffer.Add(", and it will be destroyed in ").Add( data.TimeTillPlanetDrilled.ToString( ), color).Add(" seconds.");
                    else
                        tooltipBuffer.Add(". It will be destroyed due to lack of cuendillar in ").Add( data.TimeTillPlanetDrilled.ToString( ), color).Add(" seconds. If it hatches first, you'll have a fight on your hands.");

                    tooltipBuffer.Add("\n").Add("A new transport will be dispatched in ").Add( (data.TimeForNextTransport - World_AIW2.Instance.GameSecond), "0044ff" ).Add(" seconds.");
                    if (data.TotalCuendillarDrilled > 0)
                    {
                        tooltipBuffer.Add("\n").Add("So far you have mined ").Add( (data.TotalCuendillarDrilled), "ff4444" ).Add(" cuendillar ").Add(" and dispatched ").Add( data.TransportsSent, "a1ffa1" ).Add(" transports from this ");

                        if ( target.TypeData.GetHasTag("CuendillarPlanetoid"))
                            tooltipBuffer.Add("Planetoid.");
                        else if ( target.TypeData.GetHasTag("CuendillarAsteroid") )
                            tooltipBuffer.Add("Asteroid.");
                        else
                            tooltipBuffer.Add("Chrysalis.");
                    }
                }
                else
                {
                    int cuendillarRemaining = (int)Data.Int64List[0];
                    string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.TimeTillPlanetDrilled );
                    tooltipBuffer.Add("The planet " ).Add( drill.Planet.Name, "a1ffa1" ).Add(" is being mined; There is ").Add( cuendillarRemaining, "ff4444" ).Add(" cuendillar left, and the planet will ravage in ").Add( data.TimeTillPlanetDrilled.ToString( ), color).Add(" seconds.");
                    tooltipBuffer.Add("\n").Add("A new transport will be dispatched in ").Add( (data.TimeForNextTransport - World_AIW2.Instance.GameSecond), "0044ff" ).Add(" seconds. ");
                    if ( data.TotalCuendillarDrilled > 0 )
                        tooltipBuffer.Add("\n").Add("So far you have mined ").Add( (data.TotalCuendillarDrilled), "ff4444" ).Add(" cuendillar ").Add(" and dispatched " ).Add( data.TransportsSent, "a1ffa1" ).Add(" transports from this planet.");
                    tooltipBuffer.Add("\n\n").Add("Warning!", "ffa1a1").Add(" When the drilling is complete, the planet will be Ravaged; this releases a vast amount of energy which will destroy almost all ships and structures (Including Metal Generators, ARS, etc) on the planet." );
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
                    buffer.Add( "Overload\n" );
                else
                    buffer.Add( "Drill\n" );
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
                tooltipBuffer.Add("The AI is drilling for Cuendillar on ").Add(drill.Planet.Name, "a1ffa1").Add(". The drill will periodically send Cuendillar Transports home; these can be attacked to steal the cuendillar.");
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
                tooltipBuffer.Add("The reapers will launch an assault in ").Add( time.ToString(), color ).Add(" seconds.\n");
                tooltipBuffer.Add("\tRavagers spawned by this assault will attempt to drill and ravage planets, producing swarms of new enemy ships in the process.");
            }
            else
                tooltipBuffer.Add("The reapers will launch an assault soon.");
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
                buffer.Add( "Assault\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 20;

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                int time = -1;
                if ( Data.Int64List.Count > 0 )
                    time = (int)Data.Int64List[0];
                if ( time < 0 )
                    buffer.Add("Soon");
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
                tooltipBuffer.Add("The reapers will launch a Lunar Invasion in ").Add( time.ToString(), color ).Add(" seconds.\n");
                tooltipBuffer.Add("\tA gateway and a Reaper Moon will spawn. Defeating the Moon will allow you to claim your own moon");
            }
            else
                tooltipBuffer.Add("The reapers will launch a Lunar Invasion soon.");
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
                buffer.Add( "Lunar\n" );
                SubTexts[0].Text.FinishWritingToBuffer();
                debugStage = 20;

                buffer = SubTexts[1].Text.StartWritingToBuffer();
                int time = -1;
                if ( Data.Int64List.Count > 0 )
                    time = (int)Data.Int64List[0];
                if ( time < 0 )
                    buffer.Add("Soon");
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
            tooltipBuffer.Add("There are ravagers on the map! They will ravage planets to deny you their cuendillar, and produce swarms of ships in the process.\n");
            tooltipBuffer.Add("Here are the ravagers:\n");
            for ( int i = 0; i < Data.EntityList.Count; i++ )
            {
                GameEntity_Squad ravager = Data.EntityList[i].GetSquad();
                if ( ravager == null )
                    continue;
                if ( ravager.TypeData.IsMobile )
                    tooltipBuffer.Add("\tOn ").Add(ravager.Planet.Name, "a1ffa1").Add(" in transit to a planet to ravage.\n");
                else
                {
                    ReapersPerUnitBaseInfo data = ravager.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                    if ( data == null )
                        tooltipBuffer.Add("\tOn ").Add(ravager.Planet.Name, "ffbba1").Add(" currently drilling and producing enemy ships.\n");
                    else
                    {
                        string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.SecondsTillRavage );
                        tooltipBuffer.Add("\tPlanet ").Add(ravager.Planet.Name, "ffbba1" ).Add(" will be ravaged in ").Add( data.SecondsTillRavage.ToString(), color ).Add( " seconds, and will next produce ships in ").Add( data.SecondsTillTroopSpawn, "a1a1ff" ).Add(" seconds.\n");
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
                buffer.Add( "Ravager\n" );
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
            tooltipBuffer.Add("There are Reaper Larvae on the map! They will turn into Chrysalises if not killed.\n");
            tooltipBuffer.Add("Here are the Larvae you can see:\n");
            for ( int i = 0; i < Data.EntityList.Count; i++ )
            {
                GameEntity_Squad larva = Data.EntityList[i].GetSquad();
                if ( larva == null )
                    continue;
                if ( larva.Planet.IntelLevel == PlanetIntelLevel.Unexplored)
                    continue;
                if ( larva.TypeData.IsMobile )
                    tooltipBuffer.Add("\tOn ").Add(larva.Planet.Name, "a1ffa1");
                Planet dest = larva.GetDestinationPlanet();
                if ( larva.Planet != dest )
                    tooltipBuffer.Add(" and is en route to ").Add(dest.Name, "ffa1a1");
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
                buffer.Add( "Larva\n" );
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
                tooltipBuffer.Add("There is a ").Add( Data.EntityList.Count, "ffcc22" ).Add(" Reaper Chrysalis on the map, preparing to unleash a Gateway!\n");
                tooltipBuffer.Add("Here is the Chrysalis:\n\n");
            }
            else
            {
                tooltipBuffer.Add("There are ").Add( Data.EntityList.Count, "ffcc22" ).Add(" Reaper Chrysalises on the map, preparing to unleash Gateways!\n");
                tooltipBuffer.Add("Here are the Chrysalis:\n\n");
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

                    tooltipBuffer.Add("Chrysalis ").Add(chrysalis.Planet.Name, "ffbba1").Add("\n");
                    if (spawnTime >= 0)
                    {
                        string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( spawnTime );
                        tooltipBuffer.Add("\tWill hatch in ").Add(spawnTime.ToString(), color).Add(" seconds, spawning a Gateway and many other hostile ships.\n");
                    }
                    else
                        tooltipBuffer.Add("\tIt will hatch soon, spawning a Gateway and many other hostile ships.\n");
                    tooltipBuffer.Add("\tIt has ").Add( data.CuendillarRemaining, "ff4444" ).Add(" cuendillar remaining\n");
                    if ( Data.BoolList.Count > 0 &&
                         Data.BoolList[0] )
                    {
                        tooltipBuffer.Add("\tThe AI is currently extracting Cuendillar from this Chrysalis\n\t\tThis Cuendillar will be turned into dreadful warships for the AI.");
                    }
                }
            }
            tooltipBuffer.Add("\n\nUsing Minor Drills to extract Cuendillar will weaken a Chrysalis, reducing the strength of enemies that will spawn at hatch time.");
            tooltipBuffer.Add("\nIf you can drill out all the Cuendillar, it will collapse.");

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
                buffer.Add( "Chrysalis\n" );
                if ( Data.BoolList.Count > 0 &&
                     Data.BoolList[0] )
                    buffer.Add( "AI Drill\n" );
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
