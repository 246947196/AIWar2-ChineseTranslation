
using Arcen.Universal;
using System;

using System.Diagnostics;
using System.Threading;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;

namespace Arcen.AIW2.External
{
    public class NanocaustPersonalNotificationGenerator : IExternalPersonalNotificationGenerator
    {
        private struct Item
        {
            public Faction Faction;
            public int TimeLeft;
        }

        private int CompareItems(Item a, Item b)
        {
            int cmp = a.Faction.GetDisplayName().CompareTo(b.Faction.GetDisplayName());
            if (cmp == 0)
                a.TimeLeft.CompareTo(b.TimeLeft);
            return cmp;
        }

        private List<Item> Items = List<Item>.Create_WillNeverBeGCed(100, "NanocaustPersonalNotificationGenerator-Items");

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            Items.Clear();
        }
        public void GeneratePersonalNotificationOnClientOrHost_BackgroundThread( Faction focalFaction, ArcenSimContextAnyStatus Context )
        {
             //ArcenDebugging.ArcenDebugLog( string.Format("NanocaustPersonalNotificationGenerator.GeneratePersonalNotificationOnClientOrHost_BackgroundThread called."), Verbosity.DoNotShow );

            int debugStage = 1;
            try
            {
                Items.Clear();

                foreach ( Faction f in World_AIW2.Instance.Factions )
                    {
                        if (f.SpecialFactionData == null)
                            continue;
                        if (f.SpecialFactionData.InternalName != "Nanocaust")
                            continue;

                        var tillInvasion = f.InvasionTime - World_AIW2.Instance.GameSecond;

                        //ArcenDebugging.ArcenDebugLog( string.Format("Found a Nanocaust faction with {0} till invasion.", tillInvasion), Verbosity.DoNotShow );

                        if (tillInvasion > 0 && tillInvasion < (8 * 60)) // 8 minutes, based on when the DZ notification appears...
                        {
                            var newItem = new Item();
                            newItem.Faction = f;
                            newItem.TimeLeft = tillInvasion;
                            Items.Add(newItem);
                        }
                    }

                Items.StableSort(CompareItems);

                foreach (var item in Items)
                {
                    NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                    fillData.ObjectList.Add(item.Faction);
                    fillData.Int64List.Add(item.TimeLeft);
                    fillData.Entity.Clear();
                    
                    debugStage = 300;
                    
                    SortedNotificationPriorityLevel priority = SortedNotificationPriorityLevel.Minor;
                    if (item.TimeLeft <= 60)
                        priority += 2;

                    NotificationNonSim notification = new NotificationNonSim();

                    notification.Assign( NanocaustNotifier.Instance, fillData, "", 0, "NanocaustNotifier", priority );

                    debugStage = 400;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "NanocaustPersonalNotificationGenerator.GeneratePersonalNotificationOnClientOrHost_BackgroundThread Error at debug stage " + 
                    debugStage + ":\n" + e, Verbosity.ShowAsError );
            }
        }
    }
}

