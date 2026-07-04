using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Diagnostics;

namespace Arcen.AIW2.External
{
    public class Window_BottomLeftSelfUpdatingTextWindow : WindowControllerAbstractBase
    {
        public static Window_BottomLeftSelfUpdatingTextWindow Instance;
        public Window_BottomLeftSelfUpdatingTextWindow()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true;
        }

        private bool IsOpen;
        private UpdaterDelegate updater;
        private string headerText;
        private string lastBodyText;
        private ArcenDoubleCharacterBuffer bodyTextBuffer = new ArcenDoubleCharacterBuffer( "Window_BottomLeftSelfUpdatingTextWindow-bodyTextBuffer" );
        private DateTime lastUpdate = DateTime.Now;
        private DateTime lastPositiveResultTime = DateTime.Now;
        private float secondsBetweenUpdates = 1;
        //how long to wait, keeping on getting a false result, before we show the "nothing to show" result.  Otherwise keep showing the old data
        private float secondsBeforeUpdateToNothing = 2f;
        private string ReasonForOpen = string.Empty;
        public void Open( string ReasonForOpen, float SecondsBetweenUpdates, float SecondsBeforeUpdateToNothing, string HeaderText, UpdaterDelegate Updater )
        {
            this.ReasonForOpen = ReasonForOpen;
            this.IsOpen = true;
            this.updater = Updater;
            this.headerText = HeaderText;
            this.secondsBetweenUpdates = SecondsBetweenUpdates;
            this.secondsBeforeUpdateToNothing = SecondsBeforeUpdateToNothing;

            this.updater( bodyTextBuffer ); //we don't care about the bool result in this particular case
            this.lastBodyText = bodyTextBuffer.GetStringAndResetForNextUpdate();
            this.lastUpdate = DateTime.Now;
            this.lastPositiveResultTime = DateTime.Now;
        }

        public void Close()
        {
            this.IsOpen = false;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( !this.IsOpen )
                return false;
            return true;
        }

        public bool GetIsOpenForReason( string ReasonForOpen )
        {
            return this.IsOpen && this.ReasonForOpen == ReasonForOpen;
        }

        public bool GetIsOpen()
        {
            return this.IsOpen;
        }

        public override void OnShowAfterNotShowing()
        {
            if ( tBodyText.bodyTextTransform )
            {
                UnityEngine.Vector3 pos = tBodyText.bodyTextTransform.localPosition;
                pos.y = 0;
                tBodyText.bodyTextTransform.localPosition = pos;
            }
        }

        public class customParent : CustomUIAbstractBase
        {
            public override void OnUpdate()
            {
                this.WindowController.myScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );
                this.WindowController.myXPositionScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" );
                this.WindowController.myYPositionScale = GameSettings.Current.GetFloatBySetting( "SidebarScale" ) * Window_BottomLeftGalaxyMap.SCALE_MULTIPLIER;
                this.Element.Window.MaxDeltaTimeBeforeUpdates = 0;

                if ( Engine_AIW2.Instance.CurrentGameViewMode != GameViewMode.GalaxyMapView )
                    this.WindowController.ExtraOffsetY = 0; //not on the galaxy map.  Sink to the bottom.
                else
                {
                    //yes on the galaxy map.  Rise above the Window_BottomLeftGalaxyMap
                    this.WindowController.ExtraOffsetY = -( Window_BottomLeftGalaxyMap.Instance.Window.UISpaceHeight - 15 );
                }

                if ( ( DateTime.Now - Instance.lastUpdate ).TotalSeconds >= Instance.secondsBetweenUpdates )
                {
                    if ( Instance.updater( Instance.bodyTextBuffer ) )
                    {
                        //we had some positive text to write -- yay!
                        Instance.lastBodyText = Instance.bodyTextBuffer.GetStringAndResetForNextUpdate();
                        Instance.lastUpdate = DateTime.Now;
                        Instance.lastPositiveResultTime = DateTime.Now;
                    }
                    else
                    {
                        //do another check in 1/4 of the normal interval, since we got a negative result
                        Instance.lastUpdate = DateTime.Now.AddSeconds( -Instance.secondsBetweenUpdates / 4f );
                        //meanwhile, keep showing the old thing, I guess
                        if ( (DateTime.Now - Instance.lastPositiveResultTime).TotalSeconds >= Instance.secondsBeforeUpdateToNothing )
                        {
                            //...unless it has been "too long", in which case update to show the negative result
                            Instance.lastBodyText = Instance.bodyTextBuffer.GetStringAndResetForNextUpdate();
                        }
                        else
                        {
                            //if we really are ignoring it, then make sure the buffer gets reset
                            Instance.bodyTextBuffer.ResetForNextUpdate();
                        }
                    }
                }
            }
        }

        public class bClose : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close();
                return MouseHandlingResult.None;
            }
        }

        public class tBodyText : TextAbstractBase
        {
            public static UnityEngine.RectTransform bodyTextTransform = null;
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( Instance.lastBodyText );
                Buffer.Add( "\n\n\n\n\n \n" ); //add extra spacing to prevent clipping
            }
            public override void OnUpdate()
            {
                if ( bodyTextTransform == null )
                    bodyTextTransform = this.Element.RelevantRect;
            }
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( Instance.headerText );
            }
            public override void OnUpdate() { }
        }

        public delegate bool UpdaterDelegate( ArcenDoubleCharacterBuffer BufferToWriteTo );
    }
}