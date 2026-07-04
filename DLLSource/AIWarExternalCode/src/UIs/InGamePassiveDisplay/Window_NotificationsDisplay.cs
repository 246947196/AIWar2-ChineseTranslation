using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Diagnostics;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_NotificationsDisplay : WindowControllerAbstractBase
    {
        public static Window_NotificationsDisplay Instance;
        public Window_NotificationsDisplay()
        {
            Instance = this;
            this.OnlyShowInGame = true;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true;
        }

        private static ImageButtonAbstractBase.ImageButtonPool<btnNotification> btnNotificationPool;

        public static float CurrentRowOffsetInView = 0;

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_InGameSidebarOutguard.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( btnNotification.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnNotificationPool = new ImageButtonAbstractBase.ImageButtonPool<btnNotification>( btnNotification.Original, 5 );
                        }
                    }
                    #endregion
                }

                //this.Element.Window.MaxDeltaTimeBeforeUpdates = 0;

                this.WindowController.myXPositionScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );
                this.WindowController.myYPositionScale = GameSettings.Current.GetFloatBySetting( "ResourceBarScale" );
                this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( "NotificationsScale" );

                this.OnUpdateNotifications();
            }

            //private const int COLUMN_COUNT = 9;
            private const int ROW_ADVANCE = 68;
            private const float COLUMN_ADVANCE = 41;

            public struct WaveOrNotificationSorter //struct so this lives on the stack!  No heap garbage generated
            {
                public WaveDisplay Wave;
                public NotificationNonSim Note;
                public SortedNotificationPriorityLevel Priority;
                public int OrderAdded;

                #region AddWave
                public static WaveOrNotificationSorter AddWave( WaveDisplay Wave )
                {
                    WaveOrNotificationSorter sorter;
                    sorter.OrderAdded = stagingSortingList.Count;
                    sorter.Wave = Wave;
                    sorter.Note = new NotificationNonSim();
                    sorter.Priority = Wave.CalculatePriorityForNotification();
                    return sorter;
                }
                #endregion

                #region AddNote
                public static WaveOrNotificationSorter AddNote( NotificationNonSim Note )
                {
                    WaveOrNotificationSorter sorter;
                    sorter.OrderAdded = stagingSortingList.Count;
                    sorter.Wave = null;
                    sorter.Note = Note;
                    sorter.Priority = Note.CalculatePriorityForNotification();
                    return sorter;
                }
                #endregion
            }

            public static readonly List<WaveOrNotificationSorter> stagingSortingList = List<WaveOrNotificationSorter>.Create_WillNeverBeGCed( 2000, "Window_NotificationsDisplay-stagingSortingList" );

            public static readonly ProtectedList<WaveDisplay> WavesForDisplay = ProtectedList<WaveDisplay>.Create_WillNeverBeGCed( 200, "Window_NotificationsDisplay-WavesForDisplay" );

            #region OnUpdateNotifications
            //private float lastTimeDone = 0;
            //private int lastNoteCount = 0;
            public void OnUpdateNotifications()
            {
                CurrentRowOffsetInView = 0;
                if ( !hasGlobalInitialized )
                    return;

                ////if it's been less than a second and the notification count is different, return just in case.  It blanks out for a frame or so now.
                //if ( ArcenTime.TimeSinceStartF - lastTimeDone < 1f && NotificationNonSim.Complete.Count != lastNoteCount )
                //    return;
                //lastTimeDone = ArcenTime.TimeSinceStartF;
                //lastNoteCount = NotificationNonSim.Complete.Count;

                stagingSortingList.Clear();

                //clear wave statuses on every planet
                foreach ( Planet plan in World_AIW2.Instance.Planets( true ) )
                {
                    plan.IsWaveIncoming = false;
                }

                //this is a hacky place to put it, but darn it it's the last line of defense.
                //waves keep getting created, so we can at least destroy them now.
                bool isTutorial = World_AIW2.Instance.GetIsTutorial() && ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client;
                bool shouldTutorialSkipAllWaves = isTutorial && World_AIW2.Instance.TutorialOrNull.SkipAllWaves;
                if ( shouldTutorialSkipAllWaves )
                    WaveUtils.ClearAllWaves();

                //prepare to have a fresh list of waves put in here
                WavesForDisplay.Clear( true );

                foreach ( PlannedWave wave in WaveUtils.KnownWavesAgainstHumanWorlds )
                {
                    WaveDisplay displayWave = WaveDisplay.GetFromPoolOrCreate();
                    displayWave.CopyFromPlannedWave( wave );
                    WavesForDisplay.Add( displayWave );
                }

                foreach ( WaveDisplay wave in WavesForDisplay )
                {
                    WaveOrNotificationSorter sorter = WaveOrNotificationSorter.AddWave( wave );
                    stagingSortingList.Add( sorter );

                    Planet waveTarget = World_AIW2.Instance.GetPlanetByIndex( wave.targetPlanetIdx );
                    if ( waveTarget != null )
                        waveTarget.IsWaveIncoming = true;
                }

                {
                    List<NotificationNonSim> notes = Notification.Complete;

                    //ArcenDebugging.ArcenDebugLogSingleLine( "notes: " + notes.Count, Verbosity.DoNotShow );
                    for ( int i = 0; i < notes.Count; i++ )
                    {
                        NotificationNonSim note = notes[i];
                        if ( note.SingletonHandler == null || note.GetShouldBeHidden() )
                            continue;
                        WaveOrNotificationSorter sorter = WaveOrNotificationSorter.AddNote( note );
                        stagingSortingList.Add( sorter );

                        //btnNotification item = btnNotificationPool.GetOrAddEntry();
                        //item.Assign( null, notes[i], "note" );
                    }
                }

                #region Sort the stagingSortingList
                stagingSortingList.Sort( static delegate ( WaveOrNotificationSorter L, WaveOrNotificationSorter R )
                {
                    int val = R.Priority.CompareTo( L.Priority ); //desc
                    if ( val != 0 )
                        return val;
                    if ( L.Wave == null && R.Wave == null )
                    {
                        //neither is a wave!
                        if ( L.Note.SingletonHandler != null && R.Note.SingletonHandler != null )
                        {
                            //both of these are notes

                            //if both notes, first sort by string comparison
                            val = L.Note.StringForComparison1.CompareTo( R.Note.StringForComparison1 ); //asc
                            if ( val != 0 )
                                return val;
                            //if both notes, second sort by int comparison
                            val = L.Note.IntForComparison2.CompareTo( R.Note.IntForComparison2 ); //asc
                            if ( val != 0 )
                                return val;
                        }
                        else //one of these is not a note
                        {
                            //prefer to put filled notes before blanks
                            val = (R.Note.SingletonHandler == null).CompareTo( L.Note.SingletonHandler == null ); //desc
                            if ( val != 0 )
                                return val;
                        }
                    }
                    else //at least one of these is a wave
                    {
                        //prefer to put non-waves before waves
                        val = (L.Wave == null).CompareTo( R.Wave == null ); //asc
                        if ( val != 0 )
                            return val;
                        if ( L.Wave != null && R.Wave != null )
                        {
                            //both of these are waves

                            //if both waves, first sort by time left until wave launches
                            val = R.Wave.gameTimeInSecondsForLaunchWave.CompareTo( L.Wave.gameTimeInSecondsForLaunchWave ); //desc
                            if ( val != 0 )
                                return val;

                            //if both waves, second sort by if against a human homeworld or not if time is the same
                            val = R.Wave.NonSimIsAgainstAHumanHomeworld.CompareTo( L.Wave.NonSimIsAgainstAHumanHomeworld ); //desc
                            if ( val != 0 )
                                return val;

                            //if both waves, third sort by if actually a CPA or not if time is the same
                            val = R.Wave.isActuallyACrossPlanetAttack.CompareTo( L.Wave.isActuallyACrossPlanetAttack ); //desc
                            if ( val != 0 )
                                return val;
                        }
                    }
                    //if all ELSE fails, and there is no differentiator, then sort by the order in which they were added, which should be stable
                    return L.OrderAdded.CompareTo( R.OrderAdded ); //asc
                } );
                #endregion stagingSortingList

                //NOW add the buttons.  This is way more efficient in terms of the UI dirtying
                btnNotificationPool.Clear( 5 );
                foreach ( WaveOrNotificationSorter sorter in stagingSortingList )
                {
                    if ( sorter.Wave != null )
                    {
                        btnNotification item = btnNotificationPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( item != null )
                            item.Assign( sorter.Wave, new NotificationNonSim(), sorter.Priority, "faction wave" );
                    }
                    else if ( sorter.Note.SingletonHandler != null && !sorter.Note.GetShouldBeHidden() )
                    {
                        btnNotification item = btnNotificationPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( item != null )
                            item.Assign( null, sorter.Note, sorter.Priority, "note" );
                    }
                }

                {
                    List<btnNotification> notes = btnNotificationPool.GetInUseList();
                    btnNotification note;
                    int currentColumn = 0;
                    RectTransform rTran = null;
                    float currentY = 0;

                    //bump down some if a display line showing.
                    //if ( World.Instance.IsPaused )
                    //    currentY = -20;
                    //else if ( World.Instance.ConclusionType != CampaignConclusionType.NotConcluded )
                    //    currentY = -20;
                    //else if ( Engine_AIW2.Instance.PresentationLayer.GetIsFreelook( false ) )
                    //    currentY = -20;

                    float width = Screen.width;
                    width -= ( ( COLUMN_ADVANCE + COLUMN_ADVANCE ) * width / 1200f );
                    if ( notes.Count > 0 )
                        CurrentRowOffsetInView++;

                    for ( int i = 0; i < notes.Count; i++ )
                    {
                        note = notes[i];

                        if ( currentColumn > 0 )
                        {
                            Vector3 screenPos = ArcenUI.Instance.guiCamera.WorldToScreenPoint( rTran.transform.position );
                            if ( screenPos.x >= width )
                            {
                                currentColumn = 0;
                                currentY -= ROW_ADVANCE;
                                CurrentRowOffsetInView++;
                            }
                        }

                        rTran = note.Element.RelevantRect;
                        rTran.anchoredPosition = new Vector2( ( currentColumn * COLUMN_ADVANCE ), currentY );
                        rTran.localScale = Mat.V3_One;
                        rTran.localRotation = Mat.Quater_Ident;
                        //rTran.sizeDelta = new Vector2( WIDTH, HEIGHT );
                        currentColumn++;
                    }

                    CurrentRowOffsetInView *= ROW_ADVANCE;
                    CurrentRowOffsetInView *= this.WindowController.myScale;
                }
            }
            #endregion
        }

        #region btnNotification
        public class btnNotification : ImageButtonAbstractBase
        {
            public static btnNotification Original;

            public static int TotalNotificationsEver = 0;
            public readonly int NotificationID;
            public btnNotification() 
            { 
                if ( Original == null ) Original = this; 

                this.NotificationID = System.Threading.Interlocked.Add( ref TotalNotificationsEver, 1 );
            }

            private WaveDisplay _Wave = null;
            private NotificationNonSim _Note = new NotificationNonSim();
            private SortedNotificationPriorityLevel _priority = SortedNotificationPriorityLevel.Unknown;
            private string lastSetBy = string.Empty;

            private static UnityEngine.Sprite sprite_WaveHome;
            private static UnityEngine.Sprite sprite_WaveNormal;
            private static UnityEngine.Sprite sprite_WaveReconquest;
            private static UnityEngine.Sprite sprite_CrossPlanetAttack;
            private static bool hasInitialized = false;

            private static UnityEngine.Sprite sprite_BG_Minor;
            private static UnityEngine.Sprite sprite_BG_Medium;
            private static UnityEngine.Sprite sprite_BG_Major;
            private static UnityEngine.Sprite sprite_BG_OMG;
            private static UnityEngine.Sprite sprite_BG_Informational;
            private static UnityEngine.Sprite sprite_BG_Hacking;

            public static void InitIfNeeded()
            {
                if ( hasInitialized )
                    return;
                hasInitialized = true;

                sprite_WaveHome = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/wavehome.png" );
                sprite_WaveNormal = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/wavenormal.png" );
                sprite_WaveReconquest = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/wavereconquest.psd" );
                sprite_CrossPlanetAttack = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/crossplanetattack.png" );

                sprite_BG_Minor = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/windowbackgrounds/notificationbgs/notification_minor.jpg" );
                sprite_BG_Medium = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/windowbackgrounds/notificationbgs/notification_medium.jpg" );
                sprite_BG_Major = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/windowbackgrounds/notificationbgs/notification_major.jpg" );
                sprite_BG_OMG = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/windowbackgrounds/notificationbgs/notification_omg.jpg" );
                sprite_BG_Informational = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/windowbackgrounds/notificationbgs/notification_informational.jpg" );
                sprite_BG_Hacking = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/windowbackgrounds/notificationbgs/notification_hacking.jpg" );
            }

            public void Assign( WaveDisplay W, NotificationNonSim N, SortedNotificationPriorityLevel priority, string LastSetBy )
            {
                this._Wave = W;
                this._Note = N;
                this.lastSetBy = LastSetBy;
                InitIfNeeded();
                this._priority = priority;

                //ArcenDebugging.ArcenDebugLogSingleLine( "Assign " + this.NotificationID + " Wave: " + (_Wave != null) + " Note: " + (_Note != null), Verbosity.DoNotShow );
            }

            public override bool GetShouldBeHidden()
            {
                if ( InputCaching.CalculateShouldTooltipsBeSuppressed() )
                    return true;

                if ( _Note.SingletonHandler != null && _Note.GetShouldBeHidden() )
                    return true;

                //ArcenDebugging.ArcenDebugLogSingleLine( "GetShouldBeHiddenIfBothFalse " + this.NotificationID + " Wave: " + (_Wave != null) + " Note: " + (_Note != null), Verbosity.DoNotShow );
                return _Wave == null && _Note.SingletonHandler == null;
            }

            public override void Clear()
            {
                //if ( this.Wave != null )
                //    UnityEngine.Debug.Log( "Clear!" );
                this._Wave = null;
                this._Note = new NotificationNonSim();
            }


            private SortedNotificationPriorityLevel lastDrawnPriority = SortedNotificationPriorityLevel.Unknown;
            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                WaveDisplay Wave = this._Wave;
                NotificationNonSim Note = this._Note;
                SortedNotificationPriorityLevel priority = this._priority;

                if ( Wave == null && Note.SingletonHandler == null )
                    return; //try to prevent some flickering
                
                //ArcenDebugging.ArcenDebugLogSingleLine( "UpdateContentFromVolatile " + this.NotificationID + " Wave: " + (_Wave != null) + " Note: " + (_Note != null), Verbosity.DoNotShow );

                //make sure we're starting withthe defaults
                //Image.UpdateWith( null, true, "LastImageClear" );
                Image.SetColor( Color.white );

                int bgDebugStage = 1;
                try
                {
                    #region Change the background image based on priority
                    if ( lastDrawnPriority != priority )
                    {
                        bgDebugStage = 100;
                        this.lastDrawnPriority = priority;
                        Sprite imageForBG = null;
                        Color color = Color.white;
                        switch ( priority )
                        {
                            case SortedNotificationPriorityLevel.OMG:
                                imageForBG = sprite_BG_OMG;
                                break;
                            case SortedNotificationPriorityLevel.Major:
                                imageForBG = sprite_BG_Major;
                                break;
                            case SortedNotificationPriorityLevel.Medium:
                                imageForBG = sprite_BG_Medium;
                                break;
                            case SortedNotificationPriorityLevel.Minor:
                                imageForBG = sprite_BG_Minor;
                                break;
                            case SortedNotificationPriorityLevel.Hacking:
                                imageForBG = sprite_BG_Hacking;
                                break;
                            case SortedNotificationPriorityLevel.Informational:
                                imageForBG = sprite_BG_Informational;
                                color = ColorMath.DarkGray;
                                break;
                        }
                        bgDebugStage = 200;
                        //if ( imageForBG != null )
                        UnityEngine.UI.Image img = SubImages[0].Img;
                        img.sprite = imageForBG;
                        img.color = color;
                    }
                    #endregion
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "B: Exception in Notification BG Set at stage " + bgDebugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                }

                try
                {
                    if ( Wave == null )
                    {
                        if ( Note.SingletonHandler != null )
                        {
                            if ( !Note.UpdateContentFromVolatile( Image, SubImages, SubTexts ) )
                            {
                                //Chris says: just draw what we were drawing!
                                //ArcenDebugging.ArcenDebugLogSingleLine( "UpdateContentFromVolatile " + this.NotificationID + " NoteFail", Verbosity.DoNotShow );
                                //SubTexts[1].Text.StartWritingToBuffer().Add( "Empty" );
                                //SubTexts[1].Text.FinishWritingToBuffer();
                                return;
                            }
                        }
                        else
                        {
                            //ArcenDebugging.ArcenDebugLogSingleLine( "UpdateContentFromVolatile " + this.NotificationID + " Wave And Note Fail", Verbosity.DoNotShow );
                            SubTexts[1].Text.StartWritingToBuffer().Add( "空" );
                            SubTexts[1].Text.FinishWritingToBuffer();
                        }
                        return;
                    }
                }
                catch
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine( "UpdateContentFromVolatile " + this.NotificationID + " Error", Verbosity.DoNotShow );
                    SubTexts[1].Text.StartWritingToBuffer().Add( "错误" );
                    SubTexts[1].Text.FinishWritingToBuffer();
                    return;
                }

                int debugStage = -1;

                try
                {
                    debugStage = 0;
                    Color colorForWave = Color.white;
                    UnityEngine.Sprite newSpriteType = sprite_WaveNormal;
                    debugStage = 1;
                    if ( Wave.isActuallyACrossPlanetAttack )
                        newSpriteType = sprite_CrossPlanetAttack;
                    else
                    {
                        debugStage = 2;
                        if ( Wave.isReconquestWave )
                            newSpriteType = sprite_WaveReconquest;
                        else if ( Wave.NonSimIsAgainstAHumanHomeworld )
                            newSpriteType = sprite_WaveHome;

                        {
                            debugStage = 4;
                            //there has been an odd Null reference exception in this function right after a game load,
                            //so include some code to bail out here just in case
                            try
                            {
                                if ( World_AIW2.Instance.Factions == null )
                                    return;
                                Faction targetFaction = World_AIW2.Instance.Factions[Wave.TargetFactionIndex];
                                Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex( Wave.targetPlanetIdx );
                                //If this wave is against a non-player planet or a non-player faction, change the colour to show that
                                if ( (targetFaction != null && targetFaction.Type != FactionType.Player ) || (targetPlanet != null && targetPlanet.GetControllingFactionType() != FactionType.Player) )
                                    colorForWave = Color.gray;
                            }
                            catch //( Exception e )
                            {
                                //ArcenDebugging.ArcenDebugLog( "C: Exception in Notification BG Set at stage " + bgDebugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                            } //that nullref right after game load was still persisting, so this just forcibly ignores those and bails out
                        }
                    }

                    debugStage = 10;
                    Image.UpdateWith( newSpriteType, true, "Wave" );
                    debugStage = 110;
                    Image.SetColor( colorForWave );

                    debugStage = 160;
                    ArcenDoubleCharacterBuffer buffer = SubTexts[1].Text.StartWritingToBuffer();
                    debugStage = 170;

                    if ( Wave != null )
                    {
                        try
                        {
                            buffer.WriteWaveStrength( Wave ).NewLine().WriteWaveSecondsRemaining( Wave );
                        }
                        catch //( Exception e )
                        { 
                            buffer.Add( "空波次" );
                            //ArcenDebugging.ArcenDebugLog( "D: Exception in Notification BG Set at stage " + bgDebugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                        };
                    }

                    debugStage = 200;
                    SubTexts[1].Text.FinishWritingToBuffer();

                    debugStage = 250;

                    buffer = SubTexts[0].Text.StartWritingToBuffer();
                    buffer.Add( Wave.NonSimPlanetName );

                    SubTexts[0].Text.FinishWritingToBuffer();
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "E: Exception in ShipIconImageBase.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                }
            }

            public static Faction GetFirstAIFaction()
            {
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction faction = World_AIW2.Instance.Factions[i];
                    if ( faction.Type == FactionType.AI )
                        return faction;
                    //{
                    //    if ( highestDifficultySoFar == null || highestDifficultySoFar.Difficulty < faction.GetSentinelsExternal().AIDifficulty.Difficulty )
                    //        highestDifficultySoFar = faction.GetSentinelsExternal().AIDifficulty;
                    //}
                }
                return null;
            }

            public override MouseHandlingResult HandleClick( MouseHandlingInput input )
            {
                WaveDisplay Wave = this._Wave;
                NotificationNonSim Note = this._Note;
                SortedNotificationPriorityLevel priority = this._priority;

                if ( Wave == null && Note.SingletonHandler == null )
                    return MouseHandlingResult.PlayClickDeniedSound; //prevent exception

                #region instead of normal click behavior, show details
                if ( InputCaching.CalculateHoldAndClickToViewDetailsOfContents() && Wave != null && !Wave.isActuallyACrossPlanetAttack )
                {
                    ShowDetailsOfAWavesContents( Wave );
                    return MouseHandlingResult.None;
                }
                #endregion

                if ( Note.SingletonHandler != null )
                    return Note.HandleClick();
                Planet planet = World_AIW2.Instance.GetPlanetByIndex( Wave.targetPlanetIdx );
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

            private static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_NotificationsDisplay-btnNotification-tooltipBuffer" );
            public override void HandleMouseover()
            {
                WaveDisplay Wave = this._Wave;
                NotificationNonSim Note = this._Note;
                SortedNotificationPriorityLevel priority = this._priority;
                int debugStage = 0;
                try
                {
                    debugStage = 100;
                    if ( Wave == null && Note.SingletonHandler == null )
                        return; //prevent exception

                    debugStage = 200;
                    if ( Note.SingletonHandler != null )
                    {
                        debugStage = 300;
                        if ( !Note.HandleMouseover() )
                        {
                            debugStage = 400;
                            //string errorText = "btnNotification.HandleMouseover Notification Note-Failure: Failed notification HandleMouseover lastSetBy:" + lastSetBy + " StringForComparison1: " +
                            //    Note.StringForComparison1 + "  Type: " + Note.GetType().ToString() + "  IDStringForDebugging: " + Note.IDStringForDebugging;
                            //ArcenDebugging.ArcenDebugLogSingleLine( errorText, Verbosity.ShowAsError );
                            //Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, errorText );
                        }
                        return;
                    }
                    debugStage = 500;
                    if ( Wave == null )
                    {
                        debugStage = 600;
                        Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, "空通知和空波次！！上次设置者:" + lastSetBy );
                        return;
                    }

                    debugStage = 700;
                    Planet targetPlanet = World_AIW2.Instance.GetPlanetByIndex( Wave.targetPlanetIdx );
                    debugStage = 800;
                    if ( targetPlanet != null )
                    {
                        World_AIW2.Instance.FocusedPlanetForMapDarkening = targetPlanet; //galaxy map hover
                    }
                    debugStage = 900;
                    Wave.AppendStateForInterfaceDisplay( tooltipBuffer );
                    debugStage = 1100;
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    debugStage = 1200;
                    if ( targetPlanet != null && targetPlanet.GetControllingFaction() == localFaction )
                    {
                        debugStage = 1300;
                        PlanetFaction pFaction = targetPlanet.GetPlanetFactionForFaction( localFaction );
                        debugStage = 1400;
                        if ( pFaction != null )
                        {
                            debugStage = 1500;
                            int defenseStrength = (pFaction.DataByStance[FactionStance.Friendly].TotalStrength + pFaction.DataByStance[FactionStance.Self].TotalStrength) / 1000;
                            debugStage = 1600;
                            tooltipBuffer.Add( "\n\n你的防御力量为 <color=#" ).Add( localFaction.FactionCenterColor.ColorHexBrighter ).Add( ">" ).Add( defenseStrength ).Add( "</color> 。" );
                        }
                        debugStage = 2100;
                    }
                    debugStage = 2200;
                    Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( this.Element, tooltipBuffer.GetStringAndResetForNextUpdate() );
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "Exception in btnNotification.HandleMouseover at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                }
            }

            #region WriteDetailsOfAWaveContents
            public static bool WriteDetailsOfAWaveContents( ArcenDoubleCharacterBuffer buffer, AbstractWaveBase wave, float PositionScaleMultiplier )
            {
                if ( wave.isActuallyACrossPlanetAttack )
                    return false;
                
                EntityText.Write_Tooltip_Hotkeys_Footer( buffer, false, true, null );
                buffer.Add( "\n\n" );

                buffer.Add( "<u>波次信息：</u>\n" );
                wave.AppendStateForInterfaceDisplay( buffer );
                buffer.Add( "\n\n" );

                {
                    buffer.Add( "<u>波次中的飞船：</u>\n" );
                    Faction sendingFaction = World_AIW2.Instance.GetFactionByIndex( wave.SendingFactionIndex );

                    foreach ( KeyValuePair<GameEntityTypeData, int> pair in wave.FinalComposition )
                    {
                        EntityText.GetTooltip( buffer, null, null,
                            pair.Key, pair.Value, sendingFaction, sendingFaction.CurrentGeneralMarkLevel, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None, PositionScaleMultiplier, true );
                        buffer.Add( "\n\n" );
                    } 
                }

                return true;
            }
            #endregion

            public static void ShowDetailsOfAWavesContents( WaveDisplay Wave )
            {
                if ( Wave == null )
                    return;
                if ( Wave.isActuallyACrossPlanetAttack )
                    return;

                float centerPopupScale = GameSettings.Current.GetFloatBySetting( "CentralPopupTextScale" );
                
                EntityText.ShowDetails(
                    0.25f, 2f, 
                    "攻击详情", "关闭",
                    (b)=>WriteDetailsOfAWaveContents( b, Wave, centerPopupScale ));
            }
        }
        #endregion
    }
}
