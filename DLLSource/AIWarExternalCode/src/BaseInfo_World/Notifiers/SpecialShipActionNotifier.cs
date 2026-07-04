
using System;
using System.Text;
using Arcen.AIW2.Core;
using Arcen.Universal;
using Arcen.Universal.Sprites;

namespace Arcen.AIW2.External
{
    public class SpecialShipActionNotifier : NotifierBaseDataSingleton
    {
        public static SpecialShipActionNotifier Instance = new SpecialShipActionNotifier();

        private static UnityEngine.Sprite sprite;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            sprite = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( "arcenui", "assets/arcenui/images/notificationbar/UnspentFuelPoints.png" );
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            var cust = (ExternalData_CustomSystem)Data.ObjectList[0];
            cust.UserToggle();
            
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        public override bool MouseoverHandler( NotifierFillData Data )
        {
            var cust = (ExternalData_CustomSystem)Data.ObjectList[0];
            //var timeLeft = (int)Data.Int64List[0];

            var buffer = tooltipBuffer;
            
            buffer.Clear();

            buffer
                .AddFactionColoredString(cust.Entity.GetTypeDisplayNameSafe(), cust.Entity.GetFactionOrNull_Safe())
                .NewLine();
            
            cust.AppendText(tooltipBuffer);
            
            //tooltipBuffer.Add( " " ).Add( cust.Type.GetShortDisplayName() ).Add(" will warp out in ").AddSecondsRemaining(timeLeft);

            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
            
            return true;
        }

        public override bool GetShouldBeHidden( NotifierFillData Data )
        {
            if (Data.ObjectList.Count == 0 || Data.ObjectList[0] == null)
                return true;

            return false;
        }

        public override bool ContentGetter( NotifierFillData Data, ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
        {
            int debugStage = -1;
            try
            {
                InitIfNeeded();

                var cust = (ExternalData_CustomSystem)Data.ObjectList[0];
                //var timeLeft = (int)Data.Int64List[0];

                if (cust == null)
                {
                    return false;
                }

                debugStage = 0;
                debugStage = 1;

                Image.UpdateWith( sprite, true, "SpecialShipActionNotifier_sprite" );
                
                // alternately, we could use the specific outguard ships icon
                /*
                var entityTypeForSprite = GameEntityTypeDataTable.Instance.GetRowByName( "OutguardFlagship" );
                if (grp.UnitBag != null && grp.UnitBag.EntityList != null && grp.UnitBag.EntityList.Count > 0)
                    entityTypeForSprite = grp.UnitBag.EntityList[0];

                var arcen_sprite = entityTypeForSprite.SpriteIcon;
                var sprite = arcen_sprite.Parent.GetGUISpriteByName( arcen_sprite.Name.Substring( arcen_sprite.Name.IndexOf( '/' ) + 1 ) );
                Image.UpdateWith( sprite, true, "OutguardPartyNotifier_sprite" );
                Image.SetColor( OutguardFactionBaseInfo.Instance.AttachedFaction.FactionCenterColor.TeamColor );
                */
                
                debugStage = 3;

                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                
                buffer.Add( cust.Type.GetShortDisplayName() );

                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 6;
                
                var status = cust.Status;
                if (status == CustomSystemStatus.On || status == CustomSystemStatus.OnTill)
                {
                    buffer.Add("开启", TooltipColors.CustomSystem_Status_On);
                }
                else if (status == CustomSystemStatus.Ready)
                {
                    buffer.Add("就绪", TooltipColors.CustomSystem_Status_Ready);
                }
                else // Cooldown
                {
                    buffer.Add("充能中", TooltipColors.CustomSystem_Status_Cooldown);
                }
                
                buffer.NewLine();
                
                if (status == CustomSystemStatus.Cooldown || status == CustomSystemStatus.OnTill)
                    buffer.AddSecondsRemaining( cust.TimeLeft, TimeIntensity.OneMinute );

                debugStage = 13;
                SubTexts[1].Text.FinishWritingToBuffer();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in OutguardPartyNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
    
    public class SpecialShipActionNotificationGenerator : IExternalPersonalNotificationGenerator
    {
        private struct Item
        {
            //public string SystemId;
            public int TimeLeft;
            //public EntitySystem System;
            public CustomSystem CustomSystem;
        }

        private int CompareItems(Item a, Item b)
        {
            int cmp = a.CustomSystem.Type.GetShortDisplayName().CompareTo(b.CustomSystem.Type.GetShortDisplayName());
            if (cmp == 0)
                a.TimeLeft.CompareTo(b.TimeLeft);
            return cmp;
        }

        private List<Item> Items = List<Item>.Create_WillNeverBeGCed(100, "SpecialShipActionNotificationGenerator-Items");

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            Items.Clear();
        }
        public void GeneratePersonalNotificationOnClientOrHost_BackgroundThread( Faction focalFaction, ArcenSimContextAnyStatus Context )
        {
            //LOG.Msg("SpecialShipActionNotificationGenerator");

            int debugStage = 1;
            try
            {
                // ships controlled by the local player
                // ...that have custom systems
                
                var localPlayerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if (localPlayerFaction == null)
                {
                    //LOG.Msg("No local player faction.");
                    return;
                }
                
                Items.Clear();
                
                foreach ( GameEntity_Squad e in localPlayerFaction.Squads() )
                {
                            for (int i = 0; i < e.Systems.Count; i++)
                            {
                                var sys = e.Systems[i];
                                if (sys == null)
                                    continue;
                                var cust = sys.CustomSystem;
                                if (cust == null)
                                    continue;
                                
                                var newItem = new Item();
                                newItem.CustomSystem = cust;
                                newItem.TimeLeft = cust.TimeLeft;
                                
                                Items.Add(newItem);
                            }
                        }
                
                //LOG.Msg("Found {0} CustomSystem(s).", Items.Count);
                
                Items.StableSort(CompareItems);

                foreach (var item in Items)
                {
                    var fillData = NotifierFillData.GetFromPoolOrCreate();
                    fillData.ObjectList.Add(item.CustomSystem);
                    fillData.Int64List.Add(item.TimeLeft);
                    fillData.Entity.Clear();

                    debugStage = 300;
                    
                    var priority = SortedNotificationPriorityLevel.Minor;
                    if (item.TimeLeft <= 60)
                        priority += 1;
                    if (item.TimeLeft <= 30)
                        priority += 1;
                    
                    var notification = new NotificationNonSim();

                    notification.Assign( SpecialShipActionNotifier.Instance, fillData, "", 0, "SpecialShipAction", priority );

                    debugStage = 400;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "SpecialShipActionNotificationGenerator.GeneratePersonalNotificationOnClientOrHost_BackgroundThread Error at debug stage " + 
                    debugStage + ":\n" + e, Verbosity.ShowAsError );
            }
            finally
            {
                //ArcenDebugging.ArcenDebugLogSingleLine(string.Format("finally: debugStage='{0}'", debugStage), Verbosity.DoNotShow);
            }
        }
    }
}

