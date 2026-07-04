using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Diagnostics;
using UnityEngine;
using System.Linq;

namespace Arcen.AIW2.External
{
    public class Window_InGameSidebarShips : Window_InGameSidebarBase
    {
        public static Window_InGameSidebarShips Instance;
        public Window_InGameSidebarShips()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return Current == InGameSidebarType.Ships;
        }

        private static ButtonAbstractBase.ButtonPool<txtHeader> txtHeaderPool;
        private static ImageButtonAbstractBase.ImageButtonPool<btnFleet> btnFleetPool_Local;
        private static ImageButtonAbstractBase.ImageButtonPool<btnFleet> btnFleetPool_Watched;

        #region SidebarGroups
        private static int nextSidebarIndex = 0;
        private static readonly List<ShipSidebarGroup> allSidebarGroups = List<ShipSidebarGroup>.Create_WillNeverBeGCed( 60, "Window_InGameSidebarShips-allSidebarGroups" );
        private static readonly List<ShipSidebarGroup> currentSidebarGroups = List<ShipSidebarGroup>.Create_WillNeverBeGCed( 60, "Window_InGameSidebarShips-currentSidebarGroups" );

        private static void ClearSidebarGroups()
        {
            nextSidebarIndex = 0;
            foreach ( ShipSidebarGroup group in currentSidebarGroups )
                group.Clear();
            currentSidebarGroups.Clear();
        }

        private static ShipSidebarGroup GetNextSidebarGroupForCurrent()
        {
            if ( nextSidebarIndex < allSidebarGroups.Count )
            {
                ShipSidebarGroup group = allSidebarGroups[nextSidebarIndex];
                currentSidebarGroups.Add( group );
                nextSidebarIndex++;
                return group;
            }
            else
            {
                ShipSidebarGroup group = new ShipSidebarGroup();
                allSidebarGroups.Add( group );
                currentSidebarGroups.Add( group );
                nextSidebarIndex++;
                return group;
            }
        }
        #endregion SidebarGroup

        public static readonly List<Fleet> fleetsAtLocalPlanet = List<Fleet>.Create_WillNeverBeGCed( 300, "Window_InGameSidebarShips-fleetsAtLocalPlanet" );
        public static readonly List<Fleet> fleetsBeingWatched = List<Fleet>.Create_WillNeverBeGCed( 300, "Window_InGameSidebarShips-fleetsBeingWatched" );

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_InGameSidebarShips.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.TimeSinceStartF;
            }

            public static GenericOpenCloseMenuStatusHolder open_YourShips = new GenericOpenCloseMenuStatusHolder();
            public static GenericOpenCloseMenuStatusHolder open_AlliedShips = new GenericOpenCloseMenuStatusHolder();
            public static GenericOpenCloseMenuStatusHolder open_EnemyShips = new GenericOpenCloseMenuStatusHolder();
            public static GenericOpenCloseMenuStatusHolder open_NobodyShips = new GenericOpenCloseMenuStatusHolder();

            public static bool open_LocalFleetList = true;
            public static bool open_WatchedFleetList = true;
            public static bool open_Options = false;

            public static bool hasVisionOfPlanet = false;
            public static bool everHadVisionOfPlanet = false;

            private PlanetFaction wasLastFor_PlanetFaction = null;
            private float nextRecalculateFleetItems = 0;
            private float nextRecalculateShipGroupItems = 0;

            private bool hasGlobalInitialized = false;
            //private Planet lastPlanet = null;
            public override void OnUpdate()
            {
                AdjustHeightToScreenMax( 60, "NotificationsScale", "SidebarScale", "ResourceBarScale", Window_InGameSidebarShips.CustomParentInstance,
                    Window_InGameSidebarFleets.CustomParentInstance, Window_InGameSidebarDirectBuild.CustomParentInstance,
                    Window_InGameSidebarScience.CustomParentInstance, Window_InGameSidebarHacking.CustomParentInstance,
                    Window_InGameSidebarOutguard.CustomParentInstance, Window_InGameSidebarObjectives.CustomParentInstance,
                    Window_InGameSidebarJournal.CustomParentInstance, Window_InGameSidebarTips.CustomParentInstance );

                if ( Window_InGameSidebarShips.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        this.Element.Window.MinDeltaTimeBeforeUpdates = 0.2f;
                        this.Element.Window.MaxDeltaTimeBeforeUpdates = 0.25f;

                        if ( bShipIcon.Original != null && btnFleet.Original != null && btnTextWithIcon.Original != null && txtHeader.Original != null )
                        {
                            hasGlobalInitialized = true;
                            txtHeaderPool = new ButtonAbstractBase.ButtonPool<txtHeader>( txtHeader.Original, 5 );
                            btnFleetPool_Local = new ImageButtonAbstractBase.ImageButtonPool<btnFleet>( btnFleet.Original, 5 );
                            btnFleetPool_Watched = new ImageButtonAbstractBase.ImageButtonPool<btnFleet>( btnFleet.Original, 5 );
                        }
                    }
                    #endregion
                }

                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( planet == null )
                    return;

                txtHeaderPool.Clear( 5 );
                btnFleetPool_Local.Clear( 5 );
                btnFleetPool_Watched.Clear( 5 );

                bShipIconPool.ClearAll( 10 );

                ShowShipsAsText showShipsAs = GetShowShipsAsText();

                hasVisionOfPlanet = CalculateHasVisionOfPlanet();
                everHadVisionOfPlanet = hasVisionOfPlanet || planet.IntelLevel > PlanetIntelLevel.Unexplored;
                int gameSecondLastHadVision = planet.GetGameSecondLastHadVision();
                if ( gameSecondLastHadVision == 0 )
                    gameSecondLastHadVision = -1;
                if ( planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    gameSecondLastHadVision = World_AIW2.Instance.GameSecond;

                Faction localFactionG = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFactionG == null )
                    return;
                PlanetFaction localFaction = planet.GetPlanetFactionForFaction( localFactionG );
                if ( localFaction == null )
                    return;

                PlayerAccount localAccount = PlayerAccount.Local;
                if ( localAccount == null )
                    return;

                byte localAccountID = localAccount.PlayerPrimaryKeyID;

                if ( wasLastFor_PlanetFaction != localFaction )
                {
                    wasLastFor_PlanetFaction = localFaction;
                    nextRecalculateFleetItems = 0;
                    nextRecalculateShipGroupItems = 0;
                }

                if ( nextRecalculateFleetItems < ArcenTime.TimeSinceStartF )
                {
                    nextRecalculateFleetItems = ArcenTime.TimeSinceStartF + Engine_Universal.PermanentQualityRandom.NextFloat( 0.1f, 0.3f );
                    fleetsAtLocalPlanet.Clear();
                    fleetsBeingWatched.Clear();

                    #region Distribute All the Fleets that we have into fleetsAtLocalPlanet or fleetsBeingWatched if needed
                    foreach ( Fleet fleet in World_AIW2.Instance.Fleets( FactionType.Player, FleetStatus.CenterpieceMustLive ) )
                    {
                        if ( fleet == null )
                            continue;
                        if ( !GetShowAlliedFleets() && fleet.Faction != localFactionG )
                            continue;
                        switch ( fleet.Category )
                        {
                        //skip these
                        default:
                            case Core.FleetCategory.NonPlayerDrone:
                            case Core.FleetCategory.NPC:
                            case Core.FleetCategory.PlayerLoose:
                                continue;
                        //show these
                        case Core.FleetCategory.PlayerMobile:
                            case Core.FleetCategory.PlayerCustomCityFedMobile:
                            case Core.FleetCategory.PlayerCustomUnattachedMobile:
                            case Core.FleetCategory.PlayerBattlestation:
                            case Core.FleetCategory.PlayerPlanetaryCommand:
                            case Core.FleetCategory.PlayerCustomCity:
                                break;
                        }

                        GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                    //skip when there's no centerpiece
                    if ( centerpiece == null )
                            continue;

                        if ( planet != null && centerpiece.Planet == planet )
                        {
                            fleetsAtLocalPlanet.Add( fleet );
                        }
                        if ( fleet.GetIsFleetOnPlayerWatchlist( localAccountID ) || centerpiece.GetIsCrippled() )
                            fleetsBeingWatched.Add( fleet );
                    }
                    #endregion
                }

                RectTransform rTran = null;
                float currentY = 0;

                bool hasAddedAnyCategoriesOfShips = false;

                if ( !everHadVisionOfPlanet )
                {
                    txtHeader header = txtHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    ClearSidebarGroups();
                    if ( header != null )
                    {
                        header.Purpose = PlanetSidebarHeaderPurpose.NeverHadVisionHere;
                        rTran = header.Element.RelevantRect;
                        rTran.anchoredPosition = new Vector2( 0, currentY );
                        currentY -= TEXT_ROW_HEIGHTS;
                        hasAddedAnyCategoriesOfShips = true;
                    }
                }
                else
                {
                    ShipsGroupBy current = GetShipsGroupBy();

                    if ( nextRecalculateShipGroupItems < ArcenTime.TimeSinceStartF )
                    {
                        ClearSidebarGroups();
                        nextRecalculateShipGroupItems = ArcenTime.TimeSinceStartF + Engine_Universal.PermanentQualityRandom.NextFloat( 0.1f, 0.3f );

                        switch ( current )
                        {
                            case ShipsGroupBy.ByFaction:
                                {
                                    WriteAllFactionsWithACertainRelationshipStyle( localFactionG, planet, localFaction, FactionRelationship.Self, false, showShipsAs );
                                    WriteAllFactionsWithACertainRelationshipStyle( localFactionG, planet, localFaction, FactionRelationship.FactionsIAmFriendlyTowards, false, showShipsAs );
                                    WriteAllFactionsWithACertainRelationshipStyle( localFactionG, planet, localFaction, FactionRelationship.FactionsIAmHostileTowards, false, showShipsAs );
                                }
                                break;
                            case ShipsGroupBy.ByFactionWithSplitForMobileAndStationary:
                                {
                                    WriteAllFactionsWithACertainRelationshipStyle( localFactionG, planet, localFaction, FactionRelationship.Self, true, showShipsAs );
                                    WriteAllFactionsWithACertainRelationshipStyle( localFactionG, planet, localFaction, FactionRelationship.FactionsIAmFriendlyTowards, true, showShipsAs );
                                    WriteAllFactionsWithACertainRelationshipStyle( localFactionG, planet, localFaction, FactionRelationship.FactionsIAmHostileTowards, true, showShipsAs );
                                }
                                break;
                            case ShipsGroupBy.ByRelationship:
                                #region ByRelationship
                                {
                                    //handle the self
                                    ShipSidebarGroup shipGroup = GetNextSidebarGroupForCurrent();
                                    shipGroup.HeaderText = "你";
                                    shipGroup.LaterTextStartOfSentence = "你的";
                                    shipGroup.LaterTextMidSentence = "你的";
                                    shipGroup.GiveVisionWarningText = false;
                                    shipGroup.Opener = open_YourShips;
                                    shipGroup.IsYou = true;
                                    shipGroup.IsAllied = false;
                                    shipGroup.IsEnemy = false;
                                    CalculateSquadAndShipCounts( localFactionG, planet, localFaction, shipGroup, PlanetSidebarFillType.ByRelationship, FactionRelationship.Self, localFactionG,
                                        false, false, showShipsAs );

                                    //handle allies
                                    shipGroup = GetNextSidebarGroupForCurrent();
                                    shipGroup.HeaderText = "盟友";
                                    shipGroup.LaterTextStartOfSentence = "盟军";
                                    shipGroup.LaterTextMidSentence = "盟军";
                                    shipGroup.Opener = open_AlliedShips;
                                    shipGroup.IsYou = false;
                                    shipGroup.IsAllied = true;
                                    shipGroup.IsEnemy = false;
                                    CalculateSquadAndShipCounts( localFactionG, planet, localFaction, shipGroup, PlanetSidebarFillType.ByRelationship, FactionRelationship.FactionsIAmFriendlyTowards, localFactionG,
                                        true, false, showShipsAs );

                                    //handle enemies
                                    shipGroup = GetNextSidebarGroupForCurrent();
                                    if ( !customParent.hasVisionOfPlanet )
                                        shipGroup.HeaderText = "旧情报";
                                    else
                                        shipGroup.HeaderText = "敌人";
                                    shipGroup.LaterTextStartOfSentence = "敌方";
                                    shipGroup.LaterTextMidSentence = "敌方";
                                    shipGroup.Opener = open_EnemyShips;
                                    shipGroup.IsYou = false;
                                    shipGroup.IsAllied = false;
                                    shipGroup.IsEnemy = true;
                                    CalculateSquadAndShipCounts( localFactionG, planet, localFaction, shipGroup, PlanetSidebarFillType.ByRelationship, FactionRelationship.FactionsIAmHostileTowards, localFactionG,
                                        true, false, showShipsAs );
                                }
                                #endregion
                                break;
                        }

                        #region The Neutral Group
                        
                        //PlanetFaction nobodyFaction = planet.GetFirstFactionOfType( FactionType.NaturalObject );
                        //if ( nobodyFaction != null )
                        {
                            //handle nobody
                            ShipSidebarGroup shipGroup = GetNextSidebarGroupForCurrent();
                            shipGroup.HeaderText = "中立";
                            shipGroup.LaterTextStartOfSentence = "中立";
                            shipGroup.LaterTextMidSentence = "中立";
                            shipGroup.ShowStrengthSummary = false;
                            shipGroup.Opener = open_NobodyShips;
                            shipGroup.IsYou = false;
                            shipGroup.IsAllied = false;
                            shipGroup.IsEnemy = false;
                            shipGroup.IsVisible = true;
                            
                            CalculateSquadAndShipCounts( 
                                localFactionG, planet, localFaction, shipGroup, 
                                PlanetSidebarFillType.ByRelationship, FactionRelationship.FactionsThatAreNeutralTowardsMe, 
                                null, true, true, showShipsAs );
                        }
                        #endregion
                    }
                }

                ShowLocalFleets localFleets = GetShowLocalFleets();
                ShowWatchedFleets watchedFleets = GetShowWatchedFleets();

                if ( watchedFleets == ShowWatchedFleets.AboveShipList )
                    WriteFleetList( planet, localFleets, true, ref currentY );
                if ( localFleets == ShowLocalFleets.AboveShipList )
                    WriteFleetList( planet, localFleets, false, ref currentY );

                if ( showShipsAs == ShowShipsAsText.AsIcon )
                {
                    #region Ship Groups Insertion (Icon Version)
                    foreach ( ShipSidebarGroup group in currentSidebarGroups )
                    {
                        if ( group.Squads_Total <= 0 && group.Noncombatants_Total <= 0 )
                            continue; //if nothing to show, then skip it
                        hasAddedAnyCategoriesOfShips = true;
                        group.IsVisible = true;

                        {
                            txtHeader header = txtHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( header == null )
                                break; //time slicing, too many added right now
                            header.ShipGroup = group;
                            header.Purpose = PlanetSidebarHeaderPurpose.ShipGroup_Normal;
                            rTran = header.Element.RelevantRect;
                            rTran.anchoredPosition = new Vector2( 0, currentY );
                            currentY -= TEXT_ROW_HEIGHTS;
                        }

                        if ( group.Opener.GetIsOpenForMenu() )
                        {
                            // cloaked ships info merged into normal ships header
                            /*
                            if ( group.Squads_Cloaked > 0 )
                            {
                                txtHeader cloaked = txtHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                                if ( cloaked == null )
                                    break; //time slicing, too many added right now
                                cloaked.ShipGroup = group;
                                cloaked.Purpose = PlanetSidebarHeaderPurpose.ShipGroup_CloakedShips;
                                rTran = cloaked.Element.RelevantRect;
                                currentY += 9; //hike it up a bit
                                rTran.anchoredPosition = new Vector2( 0, currentY );
                                currentY -= TEXT_ROW_HEIGHTS;
                            }
                            */

                            currentY -= ROW_ADVANCE_ICON;
                            group.shipIconPool.Sort();
                            ApplyShipsInGrid_Icon( ref currentY, group.shipIconPool.GetInUseList_Icon() );
                        }
                        else
                        {
                            currentY -= ROW_ADVANCE_WHEN_CLOSED;
                            group.shipIconPool.ClearSingle();
                        }
                    }

                    if ( !hasAddedAnyCategoriesOfShips )
                    {
                        txtHeader header = txtHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( header != null )
                        {
                            header.Purpose = PlanetSidebarHeaderPurpose.StaleIntelHere;
                            rTran = header.Element.RelevantRect;
                            rTran.anchoredPosition = new Vector2( 0, currentY );
                            currentY -= TEXT_ROW_HEIGHTS;
                        }
                    }
                    #endregion
                }
                else
                {
                    #region Ship Groups Insertion (Text Version)
                    foreach ( ShipSidebarGroup group in currentSidebarGroups )
                    {
                        if ( group.Squads_Total <= 0 && group.Noncombatants_Total <= 0 )
                            continue; //if nothing to show, then skip it
                        hasAddedAnyCategoriesOfShips = true;
                        group.IsVisible = true;

                        {
                            txtHeader header = txtHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( header == null )
                                break; //time slicing, too many added right now
                            header.ShipGroup = group;
                            header.Purpose = PlanetSidebarHeaderPurpose.ShipGroup_Normal;
                            rTran = header.Element.RelevantRect;
                            rTran.anchoredPosition = new Vector2( 0, currentY );
                            currentY -= TEXT_ROW_HEIGHTS;
                        }

                        if ( group.Opener.GetIsOpenForMenu() )
                        {
                            // cloaked ships info merged into normal ships header
                            /*
                            if ( group.Squads_Cloaked > 0 )
                            {
                                txtHeader cloaked = txtHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                                if ( cloaked == null )
                                    break; //time slicing, too many added right now
                                cloaked.ShipGroup = group;
                                cloaked.Purpose = PlanetSidebarHeaderPurpose.ShipGroup_CloakedShips;
                                rTran = cloaked.Element.RelevantRect;
                                currentY += 9; //hike it up a bit
                                rTran.anchoredPosition = new Vector2( 0, currentY );
                                currentY -= TEXT_ROW_HEIGHTS;
                            }
                            */
                            group.shipIconPool.Sort();
                            ApplyShipsInColumn_Text( ref currentY, group.shipIconPool.GetInUseList_Text() );
                        }
                        else
                        {
                            currentY -= ROW_ADVANCE_WHEN_CLOSED;
                            group.shipIconPool.ClearSingle();
                        }
                    }

                    if ( !hasAddedAnyCategoriesOfShips )
                    {
                        txtHeader header = txtHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( header != null )
                        {
                            header.Purpose = PlanetSidebarHeaderPurpose.StaleIntelHere;
                            rTran = header.Element.RelevantRect;
                            rTran.anchoredPosition = new Vector2( 0, currentY );
                            currentY -= TEXT_ROW_HEIGHTS;
                        }
                    }
                    #endregion
                }

                if ( watchedFleets == ShowWatchedFleets.BelowShipList )
                    WriteFleetList( planet, localFleets, true, ref currentY );
                if ( localFleets == ShowLocalFleets.BelowShipList )
                    WriteFleetList( planet, localFleets, false, ref currentY );

                #region Options Section
                if ( hasAddedAnyCategoriesOfShips && everHadVisionOfPlanet )
                {
                    txtHeader header = txtHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( header != null )
                    {
                        header.Purpose = PlanetSidebarHeaderPurpose.Options;
                        rTran = header.Element.RelevantRect;
                        rTran.anchoredPosition = new Vector2( 0, currentY );
                        currentY -= TEXT_ROW_HEIGHTS;

                        if ( open_Options )
                        {
                            txtHeader option = txtHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( option != null )
                            {
                                option.Purpose = PlanetSidebarHeaderPurpose.Option_ShipsGroupBy;
                                rTran = option.Element.RelevantRect;
                                rTran.anchoredPosition = new Vector2( 0, currentY );
                                currentY -= TEXT_ROW_HEIGHTS;
                            }

                            option = txtHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( option != null )
                            {
                                option.Purpose = PlanetSidebarHeaderPurpose.Option_ShowShipsAsText;
                                rTran = option.Element.RelevantRect;
                                rTran.anchoredPosition = new Vector2( 0, currentY );
                                currentY -= TEXT_ROW_HEIGHTS;
                            }

                            option = txtHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( option != null )
                            {
                                option.Purpose = PlanetSidebarHeaderPurpose.Option_ShowLocalFleets;
                                rTran = option.Element.RelevantRect;
                                rTran.anchoredPosition = new Vector2( 0, currentY );
                                currentY -= TEXT_ROW_HEIGHTS;
                            }

                            option = txtHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( option != null )
                            {
                                option.Purpose = PlanetSidebarHeaderPurpose.Option_ShowWatchedFleets;
                                rTran = option.Element.RelevantRect;
                                rTran.anchoredPosition = new Vector2( 0, currentY );
                                currentY -= TEXT_ROW_HEIGHTS;
                                if ( World_AIW2.Instance.AllPlayerFactions.Count > 1 )
                                {
                                    //only show this option if we have multiple players
                                    option = txtHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                                    if ( option != null )
                                    {
                                        option.Purpose = PlanetSidebarHeaderPurpose.Option_ShowAlliedFleets;
                                        rTran = option.Element.RelevantRect;
                                        rTran.anchoredPosition = new Vector2( 0, currentY );
                                        currentY -= TEXT_ROW_HEIGHTS;
                                    }
                                }
                            }

                            if ( dpsHudExists )
                            {
                                option = txtHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                                if ( option != null )
                                {
                                    option.Purpose = PlanetSidebarHeaderPurpose.Option_ShowDps;
                                    rTran = option.Element.RelevantRect;
                                    rTran.anchoredPosition = new Vector2( 0, currentY );
                                    currentY -= TEXT_ROW_HEIGHTS;
                                }
                            }
                        }
                    }
                }
                #endregion

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                rTran = (RectTransform)bShipIcon.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            #region ApplyShipsInGrid_Icon
            private void ApplyShipsInGrid_Icon( ref float currentY, List<bShipIcon> ships )
            {
                bShipIcon ship;
                int currentColumn = 0;
                RectTransform rTran;
                for ( int i = 0; i < ships.Count; i++ )
                {
                    ship = ships[i];
                    if ( ship.GetShouldBeHidden() )
                        continue;

                    if ( currentColumn >= COLUMN_COUNT )
                    {
                        currentColumn = 0;
                        currentY -= ROW_ADVANCE_ICON;
                    }

                    rTran = ship.Element.RelevantRect;
                    rTran.anchoredPosition = new Vector2( currentColumn * COLUMN_ADVANCE_ICON, currentY );
                    rTran.localScale = Mat.V3_One;
                    rTran.localRotation = Mat.Quater_Ident;
                    //rTran.sizeDelta = new Vector2( WIDTH, HEIGHT );
                    currentColumn++;
                }

                //if we came partly across, then make us go down another row
                if ( currentColumn > 0 )
                    currentY -= ROW_ADVANCE_ICON;

                //currentY -= ROW_ADVANCE;
            }
            #endregion

            #region ApplyShipsInColumn_Text
            private void ApplyShipsInColumn_Text( ref float currentY, List<btnTextWithIcon> ships )
            {
                btnTextWithIcon ship;
                RectTransform rTran;
                for ( int i = 0; i < ships.Count; i++ )
                {
                    ship = ships[i];
                    if ( ship.GetShouldBeHidden() )
                        continue;

                    rTran = ship.Element.RelevantRect;
                    rTran.anchoredPosition = new Vector2( 0, currentY );
                    rTran.localScale = Mat.V3_One;
                    rTran.localRotation = Mat.Quater_Ident;
                    //rTran.sizeDelta = new Vector2( WIDTH, HEIGHT );
                    currentY -= ROW_ADVANCE_TEXT;
                }
            }
            #endregion

            #region CalculateHasVisionOfPlanet
            public bool CalculateHasVisionOfPlanet()
            {
                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                if ( planet == null )
                    return false;
                return planet.GetDoHumansHaveVision();
            }
            #endregion

            #region WriteAllFactionsWithACertainRelationshipStyle
            private static readonly List<PlanetFaction> planetFactionsToHandleAndSort = List<PlanetFaction>.Create_WillNeverBeGCed( 500, "Window_InGameSidebarShips-customParent-planetFactionsToHandleAndSort" );
            public static void WriteAllFactionsWithACertainRelationshipStyle( Faction localFactionG, Planet planet, PlanetFaction localFaction,
                FactionRelationship relationship, bool SplitByMobileAndStatic, ShowShipsAsText showShipsAs )
            {
                planetFactionsToHandleAndSort.Clear();
                foreach ( PlanetFaction faction in localFaction.RelatedFactions( relationship ) )
                {
                    if ( relationship == FactionRelationship.FactionsIAmFriendlyTowards && faction == localFaction )
                        continue; // when counting "ally" units, don't count my own units
                    if ( faction.Entities.SquadCount <= 0 )
                        continue; // if no units, don't bother us!
                    planetFactionsToHandleAndSort.Add( faction );
                }

                if ( planetFactionsToHandleAndSort.Count <= 0 )
                    return;
                //now sort them if there are multiple
                if ( planetFactionsToHandleAndSort.Count > 1 )
                {
                    planetFactionsToHandleAndSort.Sort( static delegate ( PlanetFaction Left, PlanetFaction Right )
                        {
                            int val = 0;

                        //AIs first in a given group
                        val = (Right.Faction.Type == FactionType.AI).CompareTo( Left.Faction.Type == FactionType.AI );
                            if ( val != 0 )
                                return val;

                        //players first in a given group
                        val = (Right.Faction.Type == FactionType.Player).CompareTo( Left.Faction.Type == FactionType.Player );
                            if ( val != 0 )
                                return val;

                        //lastly by display name
                        val = Left.Faction.GetDisplayName_Short(100).CompareTo( Right.Faction.GetDisplayName_Short(100) );
                            return val;
                        } );
                }

                foreach ( PlanetFaction faction in planetFactionsToHandleAndSort )
                {
                    if ( SplitByMobileAndStatic )
                    {
                        if ( faction.MenuState_Mobile == null )
                            faction.MenuState_Mobile = new GenericOpenCloseMenuStatusHolder();
                        if ( faction.MenuState_Stationary == null )
                            faction.MenuState_Stationary = new GenericOpenCloseMenuStatusHolder();
                    }

                    if ( faction == localFaction )
                    {
                        if ( SplitByMobileAndStatic )
                        {
                            //handle the self
                            ShipSidebarGroup shipGroup = GetNextSidebarGroupForCurrent();
                            shipGroup.HeaderText = "你 <voffset=0.07em><size=70%><color=#ffb96d>（移动）</color></size></voffset>";
                            shipGroup.LaterTextStartOfSentence = "你的移动";
                            shipGroup.LaterTextMidSentence = "你的移动";
                            shipGroup.GiveVisionWarningText = false;
                            shipGroup.Opener = faction.MenuState_Mobile;
                            shipGroup.IsYou = true;
                            shipGroup.IsAllied = false;
                            shipGroup.IsEnemy = false;
                            CalculateSquadAndShipCounts( localFactionG, planet, localFaction, shipGroup, PlanetSidebarFillType.ByFactionMobileOnly, FactionRelationship.Length, faction.Faction,
                                false, false, showShipsAs );

                            shipGroup = GetNextSidebarGroupForCurrent();
                            shipGroup.HeaderText = "你 <voffset=0.07em><size=70%><color=#e56dff>（固定）</color></size></voffset>";
                            shipGroup.LaterTextStartOfSentence = "你的固定";
                            shipGroup.LaterTextMidSentence = "你的固定";
                            shipGroup.GiveVisionWarningText = false;
                            shipGroup.Opener = faction.MenuState_Stationary;
                            shipGroup.IsYou = true;
                            shipGroup.IsAllied = false;
                            shipGroup.IsEnemy = false;
                            CalculateSquadAndShipCounts( localFactionG, planet, localFaction, shipGroup, PlanetSidebarFillType.ByFactionStationaryOnly, FactionRelationship.Length, faction.Faction,
                                false, false, showShipsAs );
                        }
                        else
                        {
                            //handle the self
                            ShipSidebarGroup shipGroup = GetNextSidebarGroupForCurrent();
                            shipGroup.HeaderText = "You";
                            shipGroup.LaterTextStartOfSentence = "Your";
                            shipGroup.LaterTextMidSentence = "your";
                            shipGroup.GiveVisionWarningText = false;
                            shipGroup.Opener = faction;
                            shipGroup.IsYou = true;
                            shipGroup.IsAllied = false;
                            shipGroup.IsEnemy = false;
                            CalculateSquadAndShipCounts( localFactionG, planet, localFaction, shipGroup, PlanetSidebarFillType.ByFaction, FactionRelationship.Length, faction.Faction,
                                false, false, showShipsAs );
                        }
                    }
                    else
                    {
                        if ( SplitByMobileAndStatic )
                        {
                            //handle any other faction
                            ShipSidebarGroup shipGroup = GetNextSidebarGroupForCurrent();
                                    shipGroup.HeaderText = faction.Faction.GetDisplayName_Short( 15 ) + " <voffset=0.07em><size=70%><color=#ffb96d>（移动）</color></size></voffset>";
                                    shipGroup.LaterTextStartOfSentence = faction.Faction.GetDisplayName() + " 移动";
                            shipGroup.LaterTextMidSentence = shipGroup.LaterTextStartOfSentence;
                            shipGroup.GiveVisionWarningText = false;
                            shipGroup.Opener = faction.MenuState_Mobile;
                            shipGroup.IsYou = false;
                            shipGroup.IsAllied = faction.GetIsFriendlyTowards(localFactionG);
                            shipGroup.IsEnemy = faction.GetIsHostileTowards(localFactionG);
                            CalculateSquadAndShipCounts( localFactionG, planet, localFaction, shipGroup, PlanetSidebarFillType.ByFactionMobileOnly, FactionRelationship.Length, faction.Faction,
                                true, false, showShipsAs );

                            shipGroup = GetNextSidebarGroupForCurrent();
                                    shipGroup.HeaderText = faction.Faction.GetDisplayName_Short( 15 ) + " <voffset=0.07em><size=70%><color=#e56dff>（固定）</color></size></voffset>";
                                    shipGroup.LaterTextStartOfSentence = faction.Faction.GetDisplayName() + " 固定";
                            shipGroup.LaterTextMidSentence = shipGroup.LaterTextStartOfSentence;
                            shipGroup.GiveVisionWarningText = false;
                            shipGroup.Opener = faction.MenuState_Stationary;
                            shipGroup.IsYou = false;
                            shipGroup.IsAllied = faction.GetIsFriendlyTowards(localFactionG);
                            shipGroup.IsEnemy = faction.GetIsHostileTowards(localFactionG);
                            CalculateSquadAndShipCounts( localFactionG, planet, localFaction, shipGroup, PlanetSidebarFillType.ByFactionStationaryOnly, FactionRelationship.Length, faction.Faction,
                                true, false, showShipsAs );
                        }
                        else
                        {
                            //handle any other faction
                            ShipSidebarGroup shipGroup = GetNextSidebarGroupForCurrent();
                            shipGroup.HeaderText = faction.Faction.GetDisplayName_Short( 15 );
                            shipGroup.LaterTextStartOfSentence = faction.Faction.GetDisplayName();
                            shipGroup.LaterTextMidSentence = shipGroup.LaterTextStartOfSentence;
                            shipGroup.GiveVisionWarningText = false;
                            shipGroup.Opener = faction;
                            shipGroup.IsYou = false;
                            shipGroup.IsAllied = faction.GetIsFriendlyTowards(localFactionG);
                            shipGroup.IsEnemy = faction.GetIsHostileTowards(localFactionG);
                            CalculateSquadAndShipCounts( localFactionG, planet, localFaction, shipGroup, PlanetSidebarFillType.ByFaction, FactionRelationship.Length, faction.Faction,
                                true, false, showShipsAs );
                        }
                    }
                }
            }
            #endregion

            #region CalculateSquadAndShipCounts
            public static void CalculateSquadAndShipCounts( Faction localFactionG, Planet planet, PlanetFaction localFaction, 
                ShipSidebarGroup GroupToFill, PlanetSidebarFillType FillType, FactionRelationship relationship, Faction ForFactionOrNull,
                bool CareAboutLastHadVisionTime, bool CountDeadAndDisabledThings, ShowShipsAsText showShipsAs )
            {
                int debugCode = 0;
                try
                {
                    debugCode = 100;
                    if ( planet == null || localFaction == null )
                    {
                        GroupToFill.Strength_Total = 0;
                        GroupToFill.Squads_Total = 0;
                        GroupToFill.Noncombatants_Total = 0;
                        GroupToFill.Strength_Cloaked = 0;
                        GroupToFill.Squads_Cloaked = 0;
                        GroupToFill.Noncombatants_Cloaked = 0;

                        return;
                    }
                    debugCode = 200;
                    
                    debugCode = 300;
                    int nonCombatantCount_Total = 0;
                    int nonCombatantCount_Cloaked = 0;
                    int shipResult_Total = 0;
                    int shipResult_Cloaked = 0;
                    FInt strength_Total = FInt.Zero;
                    FInt strength_Cloaked = FInt.Zero;

                    bool mustBeMobile = false;
                    bool mustBeStationary = false;

                    switch ( FillType )
                    {
                        case PlanetSidebarFillType.ByFactionMobileOnly:
                            mustBeMobile = true;
                            break;
                        case PlanetSidebarFillType.ByFactionStationaryOnly:
                            mustBeStationary = true;
                            break;
                    }
                    debugCode = 400;
                    //Local function (C# 7+) instead of an explicit ProcessorDelegate. When called
                    //directly (not converted to a delegate) it does NOT allocate a closure — the
                    //captured locals (debugCode, mustBeMobile, the accumulators, etc.) are passed
                    //via a compiler-generated struct on the stack. Earlier this was a
                    //`GameEntity_Squad.ProcessorDelegate shipCounter = delegate(ship) { ... }`
                    //which boxed all those captures into a <>c__DisplayClass21_1 heap object per
                    //CalculateSquadAndShipCounts call — profiler caught it at ~0.9 KB/frame.
                    void CountShip( GameEntity_Squad ship )
                    {
                        debugCode = 500;
                        if ( ship == null || ship.TypeData == null || ship.DataForMark == null )
                            return;
                        //ignore us if we arrived at this planet after the last time there was vision at the planet
                        if ( CareAboutLastHadVisionTime && !ship.GetShouldBeVisibleBasedOnPlanetIntel() )
                            return;


                        bool isRemains = ship.SecondsSpentAsRemains >= 0;  //don't include dead things in the strength count
                        bool isCrippled = ship.GetIsCrippled();  //don't include crippled things in the strength count
                        Faction faction = ship.GetFactionOrNull_Safe();

                        //first count the ship itself
                        Helper_DoCounting( ref debugCode, ship.TypeData, ship, null, faction, ship.CurrentMarkLevel,
                                           mustBeMobile, mustBeStationary, isRemains, isCrippled, CountDeadAndDisabledThings, showShipsAs, GroupToFill,
                            1,
                            ref nonCombatantCount_Total, ref nonCombatantCount_Cloaked, ref shipResult_Total, ref shipResult_Cloaked, ref strength_Total, ref strength_Cloaked, ContentsType.NotLoaded );

                        //if the ship is a guard post with ships inside, then count those separately
                        if ( ship.AIReinforcementPointContents != null )
                        {
                            #region AIReinforcementPointContents
                            byte markLevel = ship.CurrentMarkLevel;
                            foreach ( RefPair<GameEntityTypeData, int> kv in ship.AIReinforcementPointContents )
                            {
                                if ( kv.LeftItem == null || kv.RightItem <= 0 )
                                    continue;

                                Helper_DoCounting( ref debugCode, kv.LeftItem, null, null, faction, markLevel,
                                                   mustBeMobile, mustBeStationary, false, false, CountDeadAndDisabledThings, showShipsAs, GroupToFill,
                                    kv.RightItem, //how many ships
                                    ref nonCombatantCount_Total, ref nonCombatantCount_Cloaked, ref shipResult_Total, ref shipResult_Cloaked, ref strength_Total, ref strength_Cloaked, ContentsType.InGuardPost );
                            }
                            #endregion
                        }

                        if ( ship.TypeData.IsTransport && ship.GetIsFleetInTransportLoadMode_Safe() )
                        {
                            #region transport that has loaded contents
                            Fleet fleet = ship.GetFleetOrNull_Safe();
                            if ( fleet != null )
                            {
                                foreach ( FleetMembership mem in fleet.MobileFleetTransportContents() )
                                {
                                    if ( mem == null || mem.TypeData == null || mem.TransportContents.Count == 0 )
                                        continue;

                                    Helper_DoCounting( ref debugCode, mem.TypeData, null, mem, faction, mem.EffectiveMark,
                                                       mustBeMobile, mustBeStationary, false, false, CountDeadAndDisabledThings, showShipsAs, GroupToFill,
                                        mem.TransportContents.Count, //how many ships
                                        ref nonCombatantCount_Total, ref nonCombatantCount_Cloaked, ref shipResult_Total, ref shipResult_Cloaked, ref strength_Total, ref strength_Cloaked, ContentsType.InTransport );
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            // even if its not a 'transport' it can still have things loaded, in particular drones that it produces
                            // note that transports can also have drones, but they have already been handled above

                            #region non-transport that has loaded contents
                            Fleet fleet = ship.GetFleetOrNull_Safe();
                            //There's a MP client only problem where transported drones are being miscounted (as high numbers), causing the player to see a huge number of enemies where there shouldn't be.
                            //On investigation, going through this loop each time we hit any ship in the fleet (so if we have 10 drones, we are triggering this 11 times (once for the "flagship", typically
                            //a Carrier Guardian or something like that, and 10 times for each drone. I think we should instead only call this once (for the "flagship").
                            //However, this does work on the Host, so by the principle of "minimal interference", I'm having this code apply only to the MP client (where it does resolve the "runaway drone count" problem)
                            if ( fleet != null &&
                                 (ArcenNetworkAuthority.GetIsClientMode() && !ship.TypeData.IsDrone) )
                            {
                                foreach ( FleetMembership mem in fleet.DroneContentsContents() )
                                {
                                    if ( mem == null || mem.TypeData == null || mem.NumberCreatedButNotDeployed == 0 )
                                        continue;
                                    Helper_DoCounting( ref debugCode, mem.TypeData, null, mem, faction, mem.EffectiveMark,
                                                       mustBeMobile, mustBeStationary, false, false, CountDeadAndDisabledThings, showShipsAs, GroupToFill,
                                        mem.NumberCreatedButNotDeployed, //how many ships
                                        ref nonCombatantCount_Total, ref nonCombatantCount_Cloaked, ref shipResult_Total, ref shipResult_Cloaked, ref strength_Total, ref strength_Cloaked, ContentsType.Drone );
                                }
                            }
                            #endregion
                        }
                    }
                    debugCode = 1000;
                    switch ( FillType )
                    {
                        case PlanetSidebarFillType.ByRelationship:
                            foreach ( PlanetFaction faction in localFaction.RelatedFactions( relationship ) )
                            {
                                if ( relationship == FactionRelationship.FactionsIAmFriendlyTowards && faction == localFaction )
                                    continue; // when counting "ally" units, don't count my own units
                                foreach ( GameEntity_Squad ship in faction.Entities.Squads() )
                                    CountShip( ship );
                            }
                            break;
                        case PlanetSidebarFillType.ByFaction:
                        case PlanetSidebarFillType.ByFactionMobileOnly:
                        case PlanetSidebarFillType.ByFactionStationaryOnly:
                            if ( ForFactionOrNull != null )
                            {
                                PlanetFaction forPFaction = planet.GetPlanetFactionForFaction( ForFactionOrNull );
                                if ( forPFaction != null )
                                    foreach ( GameEntity_Squad ship in forPFaction.Entities.Squads() )
                                        CountShip( ship );
                            }
                            break;
                    }
                    debugCode = 1200;
                    GroupToFill.Strength_Total = strength_Total.IntValue;
                    GroupToFill.Strength_Cloaked = strength_Cloaked.IntValue;
                    GroupToFill.Squads_Total = shipResult_Total;
                    GroupToFill.Squads_Cloaked = shipResult_Cloaked;
                    GroupToFill.Noncombatants_Total = nonCombatantCount_Total;
                    GroupToFill.Noncombatants_Cloaked = nonCombatantCount_Cloaked;

                    if ( shipResult_Total > 0 && GroupToFill.Strength_Total < 100 )
                        GroupToFill.Strength_Total = 100; //make sure it will show 0.1 if there are any ships present

                    if ( shipResult_Cloaked > 0 && GroupToFill.Strength_Cloaked < 100 )
                        GroupToFill.Strength_Cloaked = 100; //make sure it will show 0.1 if there are any ships present
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in CalculateSquadAndShipCounts debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
                }
            }

            public enum ContentsType
            {
                NotLoaded,
                InGuardPost,
                InTransport,
                Drone,
            }

            private static void Helper_DoCounting( ref int debugCode, GameEntityTypeData TypeData, GameEntity_Squad shipOrNull, 
                FleetMembership fMemOrMull, Faction Faction, byte markLevel, bool mustBeMobile, 
                bool mustBeStationary, bool isRemains, bool isCrippled, bool CountDeadAndDisabledThings, ShowShipsAsText showShipsAs, ShipSidebarGroup GroupToFill, 
                int CountOfThis, 
                ref int nonCombatantCount_Total, ref int nonCombatantCount_Cloaked, ref int shipResult_Total, ref int shipResult_Cloaked, ref FInt strength_Total, ref FInt strength_Cloaked, 
                ContentsType contentsType )
            {
                bool isMobile = TypeData.IsMobile;
                if ( isMobile )
                {
                    switch ( TypeData.SpecialType )
                    {
                        case SpecialEntityType.MobileSupportFleetFlagship:
                        case SpecialEntityType.BattlestationBasic:
                        case SpecialEntityType.BattlestationCitadel:
                        case SpecialEntityType.CityCenter:
                            isMobile = false;
                            break;
                    }
                    if ( TypeData.FleetMembershipStyle == FleetMembershipStyle.Planetary )
                        isMobile = false;
                }
                debugCode = 600;
                if ( mustBeMobile && !isMobile )
                    return;
                if ( mustBeStationary && isMobile )
                    return;

                if ( shipOrNull != null )
                    markLevel = shipOrNull.CurrentMarkLevel;

                GameEntityTypeData.MarkLevelStats markStats = shipOrNull != null ? shipOrNull.DataForMark : null;
                if ( markStats == null )
                    markStats = TypeData.GetForMark( markLevel );

                #region The Counting Part
                debugCode = 700;
                if ( !TypeData.IsCombatant )
                {
                    nonCombatantCount_Total += CountOfThis + (shipOrNull == null ? 0 : shipOrNull.ExtraStackedSquadsInThis );
                    // TODO: Should we check other nuno-functional reasons here?
                    if (!isRemains && !isCrippled) {
                        strength_Total += (shipOrNull != null ? shipOrNull.GetStrengthOfStack() : markStats.StrengthPerSquad_CalculatedWithNullFleetMembership );
                    }
                    if ( shipOrNull != null && shipOrNull.GetCurrentCloakingPoints() > 0 )
                    {
                        nonCombatantCount_Cloaked += CountOfThis + (shipOrNull == null ? 0 : shipOrNull.ExtraStackedSquadsInThis);
                        if (!isRemains && !isCrippled) {
                            strength_Cloaked += (shipOrNull != null ? shipOrNull.GetStrengthOfStack() : markStats.StrengthPerSquad_CalculatedWithNullFleetMembership);
                        }
                    }
                }
                else
                {
                    bool isCloaked = false;
                    if ( shipOrNull != null && shipOrNull.GetCurrentCloakingPoints() > 0 )
                        isCloaked = true;
                    int transportedCount = 0;
                    if ( shipOrNull == null && fMemOrMull != null )
                    {
                        if (contentsType == ContentsType.InTransport)
                        {
                            transportedCount = fMemOrMull.CalculateTransportedContentsCount();
                            strength_Total += transportedCount * fMemOrMull.GetStrengthPerSquad_PlayerFleetsOnly();
                            if ( isCloaked )
                                strength_Cloaked += transportedCount * fMemOrMull.GetStrengthPerSquad_PlayerFleetsOnly();
                        }
                        else if (contentsType == ContentsType.Drone)
                        {
                            transportedCount = 0;
                            strength_Total += CountOfThis * fMemOrMull.GetStrengthPerSquad_PlayerFleetsOnly();
                            if ( isCloaked )
                                strength_Cloaked += CountOfThis * fMemOrMull.GetStrengthPerSquad_PlayerFleetsOnly();
                        }
                    }
                    else if ( shipOrNull != null &&
                            ( !shipOrNull.HasDoneOnDeathSinceLastClaimed && !shipOrNull.HasNotYetBeenFullyClaimed && 
                              !shipOrNull.GetIsCrippled() && shipOrNull.SelfBuildingMetalRemaining <= 0 &&
                              !shipOrNull.GetIsNonFunctional() || CountDeadAndDisabledThings ) )
                    {
                        // Only count the strength of things constructed, claimed, and non-crippled, but still count them as units for sidebar
                        strength_Total += shipOrNull.GetStrengthOfStack();
                        if ( isCloaked )
                            strength_Cloaked += shipOrNull.GetStrengthOfStack();
                    }
                    shipResult_Total += CountOfThis + (shipOrNull == null ? 0 : shipOrNull.ExtraStackedSquadsInThis) + transportedCount;
                    if ( isCloaked )
                        shipResult_Cloaked += CountOfThis + (shipOrNull == null ? 0 : shipOrNull.ExtraStackedSquadsInThis) + transportedCount;
                }
                #endregion
                debugCode = 800;

                #region The Icon Creation Part
                if ( !TypeData.DoesNotNeedSidebarIcon && TypeData.GUISprite_Icon != null )
                {
                    if ( shipOrNull != null && 
                        shipOrNull.GetCurrentCloakingPoints() > 0 && !shipOrNull.GetIsFriendlyToLocalFactionSafe() )
                    {
                        switch (shipOrNull.TypeData.SpecialType )
                        {
                            case SpecialEntityType.GuardPost:
                            case SpecialEntityType.DireGuardPost:
                                break; //show guard posts no matter what
                            default:
                                if ( shipOrNull.TypeData.IsFleetLeader )
                                    break; //show fleet leaders no matter what
                                else
                                    return; //hide non-friendly cloaked ships
                        }
                    }

                    debugCode = 900;
                    ShipIconStatus shipStatus = ShipIconStatus.Alive;
                    if ( isRemains )
                        shipStatus = ShipIconStatus.Remains;
                    else if ( shipOrNull != null && shipOrNull.SelfBuildingMetalRemaining > FInt.Zero )
                        shipStatus = ShipIconStatus.UnderConstruction;
                    else if ( shipOrNull != null && shipOrNull.SelfBuildingMetalRemaining > FInt.Zero && !shipOrNull.HasNotYetBeenFullyClaimed/* && shipOrNull.GetFactionTypeSafe() == FactionType.NaturalObject*/ )
                        shipStatus = ShipIconStatus.BeingClaimed;
                    else if ( shipOrNull != null && shipOrNull.GetIsCrippled() )
                        shipStatus = ShipIconStatus.Crippled;
                    else if ( shipOrNull == null )
                    {
                        if ( contentsType == ContentsType.InGuardPost )
                            shipStatus = ShipIconStatus.InGuardPost;
                        else if ( contentsType == ContentsType.InTransport )
                            shipStatus = ShipIconStatus.BeingTransported;
                        else if ( contentsType == ContentsType.Drone )
                            shipStatus = ShipIconStatus.LoadedDrone;
                    }

                    if ( showShipsAs == ShowShipsAsText.AsIcon )
                    {
                        debugCode = 950;
                        bShipIcon icon = GroupToFill.shipIconPool.GetOrAddEntryBasedOnMatch_OrNullIfTimeSlicingTooManyAdds_Icon( TypeData, shipStatus, Faction, shipOrNull?.FleetMembership );
                        if ( icon == null )
                            return; //time slicing, too many added right now

                        icon.EntityCount++;
                        icon.EntityCountIncludingStack += CountOfThis + (shipOrNull == null ? 0 : shipOrNull.ExtraStackedSquadsInThis );
                        if ( shipOrNull != null )
                            icon.ActualEntities.Add( shipOrNull );
                        if ( icon.HighestMarkLevelData == null )
                            icon.HighestMarkLevelData = markStats;
                        else if ( icon.HighestMarkLevelData.MarkLevel.Ordinal < markStats.MarkLevel.Ordinal )
                            icon.HighestMarkLevelData = markStats;
                        icon.HighestMarkLevel = icon.HighestMarkLevelData.MarkLevel.Ordinal;
                    }
                    else if ( showShipsAs == ShowShipsAsText.AsText )
                    {
                        debugCode = 975;
                        btnTextWithIcon text = GroupToFill.shipIconPool.GetOrAddEntryBasedOnMatch_OrNullIfTimeSlicingTooManyAdds_Text( TypeData, shipStatus, Faction, shipOrNull?.FleetMembership );
                        if ( text == null )
                            return; //time slicing, too many added right now

                        text.EntityCount++;
                        text.EntityCountIncludingStack += CountOfThis + (shipOrNull == null ? 0 : shipOrNull.ExtraStackedSquadsInThis);
                        if ( shipOrNull != null )
                            text.ActualEntities.Add( shipOrNull );
                        if ( text.HighestMarkLevelData == null )
                            text.HighestMarkLevelData = markStats;
                        else if ( text.HighestMarkLevelData.MarkLevel.Ordinal < markStats.MarkLevel.Ordinal )
                            text.HighestMarkLevelData = markStats;
                        text.HighestMarkLevel = text.HighestMarkLevelData.MarkLevel.Ordinal;
                    }
                }
                #endregion
            }
            #endregion

            #region WriteFleetList
            public static void WriteFleetList( Planet CurrentPlanet, ShowLocalFleets localFleets, bool IsForWatchedFleets, ref float currentY )
            {
                List<Fleet> fleets = IsForWatchedFleets ? fleetsBeingWatched : fleetsAtLocalPlanet;
                if ( fleets == null || fleets.Count <= 0 )
                    return;

                byte localAccountID = PlayerAccount.Local.PlayerPrimaryKeyID;

                bool hadAny = false;
                foreach ( Fleet fleet in fleets )
                {
                    if ( fleet == null )
                        continue;
                    if ( fleet.GetIsFleetOnPlayerWatchlist( localAccountID ) && !IsForWatchedFleets &&
                         open_LocalFleetList && localFleets != ShowLocalFleets.NotShown )
                    {
                        GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                        if ( centerpiece != null && centerpiece.Planet == CurrentPlanet )
                            continue;
                        //if a watched fleet is on the current planet, and the current planet local fleets are shown and open, then we still only show the fleet
                        //under Watched. Otherwise it's hard if you are always looking at your "Watched" fleets but sometimes need to look at Local fleets.
                        //Fleets that are Watched and and on the local planet have their hotkey coloured to make it stand out
                    }

                    hadAny = true;
                    break;
                }
                if ( !hadAny )
                    return;

                ImageButtonAbstractBase.ImageButtonPool<btnFleet> btnFleetPool = IsForWatchedFleets ? btnFleetPool_Watched : btnFleetPool_Local;

                txtHeader header = txtHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( header == null )
                    return; //time slicing, too many added right now
                header.Purpose = ( IsForWatchedFleets ? PlanetSidebarHeaderPurpose.WatchedFleetList : PlanetSidebarHeaderPurpose.LocalFleetList );
                header.RelatedCount = fleets.Count;
                RectTransform rTran = header.Element.RelevantRect;
                rTran.anchoredPosition = new Vector2( 0, currentY );
                currentY -= TEXT_ROW_HEIGHTS;

                if ( IsForWatchedFleets )
                {
                    if ( !open_WatchedFleetList )
                        return;
                }
                else
                {
                    if ( !open_LocalFleetList )
                        return;
                }

                foreach ( Fleet fleet in fleets )
                {
                    if ( fleet == null )
                        continue;
                    if ( fleet.GetIsFleetOnPlayerWatchlist( localAccountID ) && !IsForWatchedFleets &&
                         open_LocalFleetList && localFleets != ShowLocalFleets.NotShown )
                    {
                        GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                        if ( centerpiece != null && centerpiece.Planet == CurrentPlanet )
                            continue;
                        //if a watched fleet is on the current planet, and the current planet local fleets are shown and open, then we still only show the fleet
                        //under Watched. Otherwise it's hard if you are always looking at your "Watched" fleets but sometimes need to look at Local fleets.
                        //Fleets that are Watched and and on the local planet have their hotkey coloured to make it stand out
                    }
                    btnFleet itemButton = btnFleetPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( itemButton == null )
                        break; //time slicing, too many added right now
                    itemButton.FleetShown = fleet;
                    itemButton.IsForLocalFleets = !IsForWatchedFleets;
                }

                btnFleetPool.ApplyItemsInRows( 0, ref currentY, 32f, 180, 30f );

                currentY -= 2f;
            }
            #endregion
        }

        public const int COLUMN_COUNT = 6;

        public const float WIDTH_TEXT = 180f;
        public const float HEIGHT_TEXT = 18f;
        public const float ROW_ADVANCE_TEXT = HEIGHT_TEXT + 1f;

        public const float WIDTH_ICON = 28f;
        public const float HEIGHT_ICON = 35.7f;
        public const float COLUMN_ADVANCE_ICON = WIDTH_ICON + 2f;
        public const float ROW_ADVANCE_ICON = HEIGHT_ICON + 2f;

        public const float ROW_ADVANCE_WHEN_CLOSED = 5f;
        public const float TEXT_ROW_HEIGHTS = 25f;

        #region txtHeader
        public class txtHeader : ButtonAbstractBase
        {
            public static txtHeader Original;
            public txtHeader() { if ( Original == null ) Original = this; }

            public ShipSidebarGroup ShipGroup = null;
            public PlanetSidebarHeaderPurpose Purpose = PlanetSidebarHeaderPurpose.Invisible;
            public int RelatedCount;

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                switch ( this.Purpose )
                {
                    case PlanetSidebarHeaderPurpose.ShipGroup_Normal:
                        if ( this.ShipGroup != null )
                            this.ShipGroup.Opener.SetIsOpenForMenu( !this.ShipGroup.Opener.GetIsOpenForMenu() );
                        break;
                    case PlanetSidebarHeaderPurpose.Options:
                        customParent.open_Options = !customParent.open_Options;
                        break;
                    case PlanetSidebarHeaderPurpose.LocalFleetList:
                        customParent.open_LocalFleetList = !customParent.open_LocalFleetList;
                        break;
                    case PlanetSidebarHeaderPurpose.WatchedFleetList:
                        customParent.open_WatchedFleetList = !customParent.open_WatchedFleetList;
                        break;
                    case PlanetSidebarHeaderPurpose.NeverHadVisionHere:
                    case PlanetSidebarHeaderPurpose.StaleIntelHere:
                    case PlanetSidebarHeaderPurpose.ShipGroup_CloakedShips:
                        break;
                    case PlanetSidebarHeaderPurpose.Option_ShipsGroupBy:
                        IncrementShipsGroupBy();
                        break;
                    case PlanetSidebarHeaderPurpose.Option_ShowShipsAsText:
                        IncrementShowShipsAsText();
                        break;
                    case PlanetSidebarHeaderPurpose.Option_ShowLocalFleets:
                        IncrementShowLocalFleets();
                        break;
                    case PlanetSidebarHeaderPurpose.Option_ShowWatchedFleets:
                        IncrementShowWatchedFleets();
                        break;
                    case PlanetSidebarHeaderPurpose.Option_ShowAlliedFleets:
                        UpdateShowAlliedFleets();
                        break;
                    case PlanetSidebarHeaderPurpose.Option_ShowDps:
                        UpdateShowDps(cycle:true);
                        break;

                    default:
                        return MouseHandlingResult.PlayClickDeniedSound;
                }
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                bool isOpen = false;
                bool drawOpenClose = true;
                switch ( this.Purpose )
                {
                    case PlanetSidebarHeaderPurpose.ShipGroup_Normal:
                        if ( this.ShipGroup != null )
                            isOpen = this.ShipGroup.Opener.GetIsOpenForMenu();
                        break;
                    case PlanetSidebarHeaderPurpose.Options:
                        isOpen = customParent.open_Options;
                        break;
                    case PlanetSidebarHeaderPurpose.LocalFleetList:
                        isOpen = customParent.open_LocalFleetList;
                        break;
                    case PlanetSidebarHeaderPurpose.WatchedFleetList:
                        isOpen = customParent.open_WatchedFleetList;
                        break;
                    case PlanetSidebarHeaderPurpose.NeverHadVisionHere:
                    case PlanetSidebarHeaderPurpose.StaleIntelHere:
                    case PlanetSidebarHeaderPurpose.Option_ShipsGroupBy:
                    case PlanetSidebarHeaderPurpose.Option_ShowLocalFleets:
                    case PlanetSidebarHeaderPurpose.Option_ShowWatchedFleets:
                    case PlanetSidebarHeaderPurpose.Option_ShowAlliedFleets:
                    case PlanetSidebarHeaderPurpose.Option_ShowShipsAsText:
                    case PlanetSidebarHeaderPurpose.Option_ShowDps:
                    case PlanetSidebarHeaderPurpose.ShipGroup_CloakedShips:
                        drawOpenClose = false;
                        break;
                }

                if ( drawOpenClose )
                {
                    if ( isOpen )
                        Buffer.Add( "<voffset=0.11em><size=60%>(-) </size></voffset>" );
                    else
                        Buffer.Add( "<voffset=0.11em><size=60%>(+) </size></voffset>" );
                }

                switch ( this.Purpose )
                {
                    case PlanetSidebarHeaderPurpose.ShipGroup_Normal:
                        if ( this.ShipGroup != null )
                        {
                            Buffer.Add( this.ShipGroup.HeaderText );
                            if ( this.ShipGroup.ShowStrengthSummary )
                            {
                                Buffer.Add( "  " );
                                string colorString = "ffffff"; //white for neutral
                                if ( this.ShipGroup.IsYou )
                                    colorString = ArcenExternalUIUtilities.Strength.Color;
                                else if ( this.ShipGroup.IsAllied )
                                    colorString = "f8ff33"; //yellow for allied
                                else if ( this.ShipGroup.IsEnemy )
                                    colorString = "ff3380"; //pink red for enemy

                                Buffer.StartColor( colorString );
                                Buffer.Add( ArcenExternalUIUtilities.Strength, colorString );
                                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( Buffer, this.ShipGroup.Strength_Total, true, true );
                            }
                            Buffer.EndColor();
                            Buffer.Add( "  <voffset=0.1em><size=80%>" );
                            Buffer.AddNumberMoreReadable( this.ShipGroup.Squads_Total ).StartColor( QuickColors.HeaderDull ).Add( " 艘" ).EndColor();
                            if ( this.ShipGroup.Squads_Cloaked > 0)
                            {
                                Buffer.StartColor( "cc6dfe" );
                                Buffer.Add( " " ).Add( this.ShipGroup.Squads_Cloaked ).Add(" (C)");
                                Buffer.EndColor();
                            }
                            Buffer.Add( "</voffset></size>" );
                        }
                        break;
                    case PlanetSidebarHeaderPurpose.ShipGroup_CloakedShips:
                        // combined into the above case PlanetSidebarHeaderPurpose.ShipGroup_Normal
                        break;
                    case PlanetSidebarHeaderPurpose.Options:
                        Buffer.Add( "<color=#aaaaaa>选项</color>" );
                        break;
                    case PlanetSidebarHeaderPurpose.LocalFleetList:
                        Buffer.Add( "本地舰队 (" ).Add( RelatedCount ).Add( ")" );
                        break;
                    case PlanetSidebarHeaderPurpose.WatchedFleetList:
                        Buffer.Add( "监视舰队 (" ).Add( RelatedCount ).Add( ")" );
                        break;
                    case PlanetSidebarHeaderPurpose.NeverHadVisionHere:
                        Buffer.Add( "此处无视野" );
                        break;
                    case PlanetSidebarHeaderPurpose.StaleIntelHere:
                        Buffer.Add( "旧扫描显示无内容" );
                        break;
                    case PlanetSidebarHeaderPurpose.Option_ShipsGroupBy:
                        {
                            ShipsGroupBy current = GetShipsGroupBy();
                            switch ( current )
                            {
                                case ShipsGroupBy.ByFaction:
                                    Buffer.Add( "按阵营分组" );
                                    break;
                                case ShipsGroupBy.ByFactionWithSplitForMobileAndStationary:
                                    Buffer.Add( "按阵营和移动方式分组" );
                                    break;
                                case ShipsGroupBy.ByRelationship:
                                    Buffer.Add( "按关系分组" );
                                    break;
                            }
                        }
                        break;
                    case PlanetSidebarHeaderPurpose.Option_ShowLocalFleets:
                        {
                            ShowLocalFleets current = GetShowLocalFleets();
                            switch ( current )
                            {
                                case ShowLocalFleets.AboveShipList:
                                    Buffer.Add( "本地舰队在飞船列表上方" );
                                    break;
                                case ShowLocalFleets.BelowShipList:
                                    Buffer.Add( "本地舰队在飞船列表下方" );
                                    break;
                                case ShowLocalFleets.NotShown:
                                    Buffer.Add( "不显示本地舰队" );
                                    break;
                            }
                        }
                        break;
                    case PlanetSidebarHeaderPurpose.Option_ShowWatchedFleets:
                        {
                            ShowWatchedFleets current = GetShowWatchedFleets();
                            switch ( current )
                            {
                                case ShowWatchedFleets.AboveShipList:
                                    Buffer.Add( "监视舰队在飞船列表上方" );
                                    break;
                                case ShowWatchedFleets.BelowShipList:
                                    Buffer.Add( "监视舰队在飞船列表下方" );
                                    break;
                                case ShowWatchedFleets.NotShown:
                                    Buffer.Add( "不显示监视舰队" );
                                    break;
                            }
                        }
                        break;
                    case PlanetSidebarHeaderPurpose.Option_ShowAlliedFleets:
                        {
                            bool shouldShowFleets = GetShowAlliedFleets();
                            if ( shouldShowFleets )
                                Buffer.Add("显示盟军舰队");
                            else
                                Buffer.Add("隐藏盟军舰队");
                        }
                        break;

                    case PlanetSidebarHeaderPurpose.Option_ShowShipsAsText:
                        {
                            ShowShipsAsText current = GetShowShipsAsText();
                            switch ( current )
                            {
                                case ShowShipsAsText.AsIcon:
                                    Buffer.Add( "飞船以图标显示" );
                                    break;
                                case ShowShipsAsText.AsText:
                                    Buffer.Add( "飞船以文本显示" );
                                    break;
                            }
                        }
                        break;

                    case PlanetSidebarHeaderPurpose.Option_ShowDps:
                        {
                            var current = GetShowDps();
                            if ( current == DpsHud.Mode.Off )
                                Buffer.Add( "不显示飞船伤害");
                            else if ( current == DpsHud.Mode.Damage )
                                Buffer.Add( "显示飞船伤害" );
                            else if ( current == DpsHud.Mode.MetalLoss )
                                Buffer.Add( "显示飞船金属损失" );
                        }
                        break;
                }
            }

            private static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_InGameSidebarShips-txtHeader-tooltipBuffer" );
            public override void HandleMouseover()
            {
                tooltipBuffer.Clear();
                switch ( this.Purpose )
                {
                    case PlanetSidebarHeaderPurpose.Invisible:
                        tooltipBuffer.Add( "you should not be seeing this." );
                        break;
                    case PlanetSidebarHeaderPurpose.ShipGroup_Normal:
                    case PlanetSidebarHeaderPurpose.ShipGroup_CloakedShips:
                        //Nobody
                        //Unowned lower down
                        if ( this.ShipGroup == null )
                        {
                            tooltipBuffer.Add( "Null ShipGroup?" );
                            break; //nothing to draw!
                        }
                        if ( !customParent.everHadVisionOfPlanet && this.ShipGroup.GiveVisionWarningText )
                        tooltipBuffer.Add( "你从未获得过此星球的视野。" );
                        else
                        {
                            if ( this.ShipGroup.IsYou )
                            { }//   colorString = ArcenExternalUIUtilities.StrengthTextColor;
                            else if ( this.ShipGroup.IsAllied )
                                tooltipBuffer.StartColor( "f8ff33" ).Add( this.ShipGroup.LaterTextStartOfSentence ).Add( " 是你的盟友。\n" ).EndColor();
                            else if ( this.ShipGroup.IsEnemy )
                                tooltipBuffer.StartColor( "ff3380" ).Add( this.ShipGroup.LaterTextStartOfSentence ).Add( " 是你的敌人。\n" ).EndColor();
                            else
                                tooltipBuffer.StartColor( "ffffff" ).Add( this.ShipGroup.LaterTextStartOfSentence ).Add( " 对你保持中立。\n" ).EndColor();

                            if ( !customParent.hasVisionOfPlanet && this.ShipGroup.GiveVisionWarningText )
                            {
                                tooltipBuffer.Add( "你过去曾看到过这个星球，并且仍然能够监控你在看到它们时该星球上存在的所有飞船。但是，任何新进入该星球的飞船对你来说是不可见的。" );

                                Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();

                                tooltipBuffer.Add( "\n\n此星球距今 " ).Add( planet == null ? "???" : (World_AIW2.Instance.GameSecond - planet.GetGameSecondLastHadVision()).ToString() ).Add( " 秒的情报：" );
                            }
                            else
                                tooltipBuffer.Add( "在此星球上：" );

                            if ( this.ShipGroup.ShowStrengthSummary )
                            {
                                tooltipBuffer.Add( "\n" ).Add( this.ShipGroup.LaterTextMidSentence ).Add( " 部队强度：" );
                                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( tooltipBuffer, this.ShipGroup.Strength_Total, true, true );
                                if ( this.ShipGroup.Strength_Cloaked > 0 )
                                {
                                    tooltipBuffer.Add( "（" );
                                    ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( tooltipBuffer, this.ShipGroup.Strength_Cloaked, true, true );
                                    tooltipBuffer.Add( " 隐形）" );
                                }
                            }
                            tooltipBuffer.Add( "\n" ).Add( this.ShipGroup.LaterTextStartOfSentence ).Add( " 飞船：" ).AddNumberMoreReadable( this.ShipGroup.Squads_Total );
                            if ( this.ShipGroup.Squads_Cloaked > 0 )
                            {
                                tooltipBuffer.Add( "（" );
                                tooltipBuffer.AddNumberMoreReadable( this.ShipGroup.Squads_Cloaked );
                                tooltipBuffer.Add( " 隐形）" );
                            }
                            tooltipBuffer.Add( "\n" ).Add( this.ShipGroup.LaterTextStartOfSentence ).Add( " 非战斗单位：" ).AddNumberMoreReadable( this.ShipGroup.Noncombatants_Total );
                            if ( this.ShipGroup.Noncombatants_Cloaked > 0 )
                            {
                                tooltipBuffer.Add( "（" );
                                tooltipBuffer.AddNumberMoreReadable( this.ShipGroup.Noncombatants_Cloaked );
                                tooltipBuffer.Add( " 隐形）" );
                            }
                        }
                        break;
                    case PlanetSidebarHeaderPurpose.Options:
                        tooltipBuffer.Add( "用于配置侧边栏的选项。" );
                        break;
                    case PlanetSidebarHeaderPurpose.LocalFleetList:
                        tooltipBuffer.Add( "你在此星球上的舰队。" );
                        break;
                    case PlanetSidebarHeaderPurpose.WatchedFleetList:
                        tooltipBuffer.Add( "你正在关注的舰队。" );
                        break;
                    case PlanetSidebarHeaderPurpose.NeverHadVisionHere:
                        tooltipBuffer.Add( "你从未获得过此星球的视野。" );
                        break;
                    case PlanetSidebarHeaderPurpose.StaleIntelHere:
                        tooltipBuffer.Add( "你对此星系的初始扫描并不全面，没有发现任何东西。这里可能有东西，但如果是的话，你需要派间谍或其他飞船来调查。\n\n如果这个星球真的连金属矿藏都没有，那一定发生了什么可怕的事情。" );
                        break;
                    case PlanetSidebarHeaderPurpose.Option_ShipsGroupBy:
                        tooltipBuffer.Add( "飞船图标及其强度汇总可以按与你的关系、按阵营、或按阵营并区分固定和移动单位进行分组。" );
                        break;
                    case PlanetSidebarHeaderPurpose.Option_ShowLocalFleets:
                        tooltipBuffer.Add( "本地舰队是你个人控制的、位于你正在查看的星球上的舰队。你可以将它们及其状态显示在主飞船列表的上方或下方，或完全不显示。" );
                        break;
                    case PlanetSidebarHeaderPurpose.Option_ShowWatchedFleets:
                        tooltipBuffer.Add( "监视舰队是你个人控制的、位于此星球以外的星球上、且你标记为想要监控的舰队（通常是为了关注其状态）。你可以将它们及其状态显示在主飞船列表的上方或下方，或完全不显示。" );
                        break;

                    case PlanetSidebarHeaderPurpose.Option_ShowAlliedFleets:
                        tooltipBuffer.Add( "你也可以选择显示属于其他玩家的舰队。" );
                        break;

                    case PlanetSidebarHeaderPurpose.Option_ShowShipsAsText:
                        tooltipBuffer.Add( "飞船可以以网格中的图标形式列出，也可以以行中的更宽文本条目形式列出。图标是经典视图，但文本更容易视觉理解，也是新的默认设置。" );
                        break;

                    case PlanetSidebarHeaderPurpose.Option_ShowDps:
                        tooltipBuffer.Add( "飞船可以显示或不显示造成的伤害。" );
                        break;

                    default:
                        tooltipBuffer.Add( "未知用途：" ).Add( Purpose.ToString() );
                        break;
                }
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "GeneralTooltipScale" );
            }

            public override void Clear()
            {
                this.ShipGroup = null;
                this.Purpose = PlanetSidebarHeaderPurpose.Invisible;
            }

            public override bool GetShouldBeHidden()
            {
                switch ( this.Purpose )
                {
                    case PlanetSidebarHeaderPurpose.Invisible:
                        return true;
                    case PlanetSidebarHeaderPurpose.ShipGroup_Normal:
                    case PlanetSidebarHeaderPurpose.ShipGroup_CloakedShips:
                        if ( this.ShipGroup == null || !customParent.everHadVisionOfPlanet || !this.ShipGroup.IsVisible )
                            return true;
                        break;
                }
                return false;
            }
        }
        #endregion

        #region bShipIcon
        public class bShipIcon : ImageButtonAbstractBase, IShipSidebarButton
        { 
            public static bShipIcon Original;
            public bShipIcon() { if ( Original == null ) Original = this; }

            //public ShipIconFactionType FactionType;
            public GameEntityTypeData EffectiveTypeData = null;
            public ShipIconStatus IconStatus = ShipIconStatus.Alive;
            public Faction FactionG = null;
            public GameEntityTypeData.MarkLevelStats HighestMarkLevelData = null;
            public FleetMembership MemIfModular = null;
            public int EntityCount = 0;
            public int EntityCountIncludingStack = 0;
            public byte HighestMarkLevel = 0;
            public readonly List<SafeSquadWrapper> ActualEntities = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 2000, "Window_InGameSidebarShips-bShipIcon-ActualEntities" );

            public ArcenUIWrapperedTMProText TextBelow;
            public ArcenUIWrapperedUnityImage ShipIcon;
            public ArcenUIWrapperedUnityImage ShipIconBorder;
            public ArcenUIWrapperedUnityImage ShipIconOverlay;

            public override MouseHandlingResult HandleClick( MouseHandlingInput input )
            {
                return bShipIcon.HandleShipSidebarClick( this, this.EffectiveTypeData, this.ActualEntities, input );
            }

            #region HandleShipSidebarClick
            public static MouseHandlingResult HandleShipSidebarClick( IShipSidebarButton Button, GameEntityTypeData EffectiveTypeData, List<SafeSquadWrapper> ActualEntities,
                MouseHandlingInput input )
            {
                if ( input.LeftButtonClicked )
                    return HandleShipSidebarLeftClick( Button, EffectiveTypeData, ActualEntities );

                if ( input.RightButtonClicked )
                {
                    if ( ActualEntities.Count > 0 )
                    {
                        if ( GameSettings.Current.GetBoolBySetting( "ChainRightClickOrders", false ) )
                        {
                            var res = EndpointFunctions.SecondaryClickSquads( ActualEntities );
                            if ( res == SecondaryClickResult.DidAnyNeededActions )
                                return MouseHandlingResult.None;
                        }
                        else
                        {
                            var res = EndpointFunctions.SecondaryClickSquad( ActualEntities[0].GetSquad() );
                            if ( res == SecondaryClickResult.DidAnyNeededActions )
                                return MouseHandlingResult.None;
                        }
                    }
                }

                return MouseHandlingResult.PlayClickDeniedSound;
            }

            public static MouseHandlingResult HandleShipSidebarLeftClick( IShipSidebarButton Button, GameEntityTypeData EffectiveTypeData, List<SafeSquadWrapper> ActualEntities )
            {
                #region instead of normal click behavior, show details
                if ( InputCaching.CalculateHoldAndClickToViewDetailsOfContents() && ActualEntities.Count >= 1 &&
                     EntityText.GetHasContentsToView( ActualEntities[0].GetSquad() ) != null )
                {
                    EntityText.ShowContents( ActualEntities[0].GetSquad() );
                    return MouseHandlingResult.None;
                }
                #endregion

                if ( EffectiveTypeData != null )
                {
                    bool clearSelectionFirst = false;
                    bool unselectingInstead = false;
                    if ( Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Additive ) )
                    { }
                    else if ( Engine_AIW2.Instance.PresentationLayer.GetAreInputFlagsActive( ArcenInputFlags.Subtractive ) )
                    {
                        unselectingInstead = true;
                    }
                    else
                    {
                        clearSelectionFirst = true;
                    }

                    Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                    bool foundOne = false;

                    //Walk the planet's live squads filtered by the button's bound entity list,
                    //matching the semantics of the (now-retired) Button.DFShips wrapper.
                    List<SafeSquadWrapper> actualEntities = Button.GetActualEntities();
                    foreach ( GameEntity_Squad entity in planet.Squads() )
                    {
                        if ( entity == null || entity.TypeData == null || entity.DataForMark == null )
                            continue;
                        if ( !actualEntities.Contains( entity ) )
                            continue; // just making sure it's still on the planet, alive, etc
                        if ( entity.Planet != planet )
                            continue;
                        if ( !foundOne )
                        {
                            foundOne = true;
                            if ( clearSelectionFirst )
                            {
                                if ( !entity.GetMayBeSelected( UnitSelectionType.DirectClick ) || entity.GetIsSelected() )
                                    Engine_AIW2.Instance.PresentationLayer.CenterPlanetViewOnEntity( entity, false );
                                Engine_AIW2.Instance.ClearSelection( true, true );
                            }
                        }
                        if ( entity.GetMayBeSelected( UnitSelectionType.DirectClick ) )
                        {
                            if ( unselectingInstead )
                                entity.Unselect( false, "UnselectingByShipSidebar" );
                            else
                                entity.Select( false, "SelectingByShipSidebar" );
                        }
                    }
                    return MouseHandlingResult.None;
                }
                return MouseHandlingResult.None;
            }
            #endregion

            #region HandleShipSidebarMouseover
            private static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_InGameSidebarShips-bShipIcon-tooltipBuffer" );
            public static void HandleShipSidebarMouseover( GameEntityTypeData EffectiveTypeData, List<SafeSquadWrapper> ActualEntities, 
                Faction FactionG, ShipIconStatus IconStatus, int EntityCountIncludingStack, byte HighestMarkLevel )
            {
                World_AIW2.Instance.FocusedEntityTypeDataForMapDarkening = EffectiveTypeData;
                if ( InputCaching.CalculateShouldTooltipsBeSuppressed() )
                {
                    //Engine_Universal.DebugText = "suppress " + DateTime.Now;
                    Window_AtMouseTooltipPanelBesideSidebar.WindowControllerInstance.ClearMyself();
                    return;
                }
                
                var sidebarType = FromSidebarType.Sidebar_SingleUnit;
                if (ActualEntities.Count > 1)
                    sidebarType = FromSidebarType.Sidebar_MultipleUnits;
                
                GameEntity_Squad firstEntity = null;
                if (ActualEntities.Count > 0)
                {
                    firstEntity = ActualEntities[0].GetSquad();
                    if (firstEntity != null &&
                        firstEntity.ShipCount > 1)
                    {
                        sidebarType = FromSidebarType.Sidebar_MultipleUnits;
                    }
                }
                
                if (firstEntity == null)
                {
                    ShipExtraDetailFlags flags = ShipExtraDetailFlags.None;
                    switch ( IconStatus )
                    {
                        case ShipIconStatus.InGuardPost:
                            flags = ShipExtraDetailFlags.InGuardPost;
                            break;
                        case ShipIconStatus.BeingTransported:
                            flags = ShipExtraDetailFlags.BeingTransported;
                            break;
                        case ShipIconStatus.LoadedDrone:
                            flags = ShipExtraDetailFlags.LoadedDrone;
                            break;
                    }
                    EntityText.GetTooltip( tooltipBuffer, null, null,
                        EffectiveTypeData, EntityCountIncludingStack, FactionG, HighestMarkLevel, sidebarType, flags, 1f, false );
                    EntityText.Write_Tooltip_Hotkeys_Footer( tooltipBuffer, true, true, string.Empty );
                    //Engine_Universal.DebugText = "text " + DateTime.Now + " " + tooltipBuffer.GetStringWithoutResettingForNextUpdate();
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
                    
                    return;
                }

                var moreContentString = EntityText.GetHasContentsToView( firstEntity );
                GameEntity_Base.SetCurrentlyHoveredOver( firstEntity, sidebarType );
                
                EntityText.GetTooltip( tooltipBuffer, firstEntity, null,
                    null, EntityCountIncludingStack, null, 0, sidebarType, ShipExtraDetailFlags.None, 1f, false );
                
                EntityText.Write_Tooltip_Hotkeys_Footer( tooltipBuffer, true, true, moreContentString );
                //Engine_Universal.DebugText = "text " + DateTime.Now + " " + tooltipBuffer.GetStringWithoutResettingForNextUpdate();
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
            }
            #endregion

            public override void HandleMouseover()
            {
                bShipIcon.HandleShipSidebarMouseover( this.EffectiveTypeData, this.ActualEntities, this.FactionG, this.IconStatus, this.EntityCountIncludingStack, this.HighestMarkLevel );
            }

            public List<SafeSquadWrapper> GetActualEntities() => this.ActualEntities;

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                InitIfNeeded( Image, SubImages, SubTexts );
                RenderContents();
            }

            public const float WIDTH = 28f;
            public const float HEIGHT = 35.7f;

            private bool hasInitialized = false;
            public void InitIfNeeded( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                if ( hasInitialized )
                    return;
                hasInitialized = true;

                TextBelow = SubTexts[0].Text;
                ShipIcon = SubImages[0].WrapperedImage;
                ShipIconBorder = SubImages[1].WrapperedImage;
                ShipIconOverlay = SubImages[2].WrapperedImage;

                RectTransform rTran = TextBelow.ReferenceText.rectTransform;
                rTran.anchoredPosition = new Vector2( 0, 5.55f );
                rTran.localScale = Mat.V3_One;
                rTran.localRotation = Mat.Quater_Ident;

                rTran = (RectTransform)ShipIcon.GO.transform;
                rTran.anchoredPosition = new Vector2( 0, 0 );
                rTran.localScale = Mat.V3_One;
                rTran.localRotation = Mat.Quater_Ident;

                rTran = this.Element.RelevantRect;
                rTran.localScale = Mat.V3_One;
                rTran.localRotation = Mat.Quater_Ident;
                //UnityEngine.Debug.Log( this.Element.name + " v1: " + rTran.sizeDelta );
                //rTran.sizeDelta = new Vector2( WIDTH, HEIGHT );
                //UnityEngine.Debug.Log( this.Element.name + " v2: " + rTran.sizeDelta );
            }

            public bool DebugRenderContents = false;
            public void RenderContents()
            {
                if ( EffectiveTypeData == null )
                    return;

                int debugStage = -1;
                try
                {
                    debugStage = 0;
                    if ( TextBelow != null )
                    {
                        ArcenDoubleCharacterBuffer buffer = TextBelow.StartWritingToBuffer();

                        if ( GetShowDps() != DpsHud.Mode.Off )
                        {
                            var dpshud = ExternalWorldBaseInfoSourceTable.Instance.GetRowByName("DpsHudBaseInfo")?.Singleton as DpsHud.IDpsHud;
                            if (dpshud != null)
                            {
                                // Presumably this window doesn't combine multiple squads into a single
                                // button, unless they are the same type.
                                // Since we collect damage dealt per type, we really could use any of
                                // them.
                                int num = 0;
                                for ( int i = 0; i < this.ActualEntities.Count; i++ )
                                {
                                    var squad = ActualEntities[i].GetSquad();

                                    float a;
                                    num = dpshud.GetValue( squad, GetShowDps(), out a );
                                    if ( num > 0.0f )
                                        break;
                                }
                            
                                if ( num >= 1000 )
                                {
                                    if (GetShowDps() == DpsHud.Mode.Damage)
                                    {
                                        buffer.StartColor( ArcenExternalUIUtilities.Damage.Color );
                                        buffer.AddDamageNumbers(num);
                                        buffer.EndColor();
                                    }
                                    else if (GetShowDps() == DpsHud.Mode.MetalLoss)
                                    {
                                        buffer.StartColor( ArcenExternalUIUtilities.Metal.Color );
                                        buffer.AddDamageNumbers(num);
                                        buffer.EndColor();
                                    }
                                }
                            }
                        }
                        // Show ship mark level and count (and in GP).
                        else
                        {
                            if ( this.HighestMarkLevelData.MarkLevel != null )
                                buffer.Add( "<color=#" ).Add( this.HighestMarkLevelData.MarkLevel.ColorHex ).Add( ">" );
                            buffer.Add( this.EntityCountIncludingStack );
                            switch ( IconStatus )
                            {
                                case ShipIconStatus.InGuardPost:
                                    buffer.EndColor().Add( " (GP)", "ff622b" );
                                    break;
                                case ShipIconStatus.BeingTransported:
                                    buffer.EndColor().Add( " (T)", "59d2ff" );
                                    break;
                                case ShipIconStatus.LoadedDrone:
                                    buffer.EndColor().Add( " (D)", "ff622b" );
                                    break;
                            }
                        }

                        TextBelow.FinishWritingToBuffer();
                    }
                    debugStage = 2;

                    Color factionCenterColor = ColorMath.White;
                    Color factionTrimColor = ColorMath.Black;
                    GameEntity_Squad mainEntity = null;

                    debugStage = 4;

                    mainEntity = this.ActualEntities[0].GetSquad();
                    if (mainEntity != null)
                    {
                        factionCenterColor = mainEntity.GetFactionCenterColor().TeamColor;
                        factionTrimColor = mainEntity.GetFactionTrimColor().TeamColor;
                    }
                    else
                    {
                        factionCenterColor = this.FactionG.FactionCenterColor.TeamColor;
                        factionTrimColor = this.FactionG.FactionTrimColor.TeamColor;
                    }
                        
                    debugStage = 6;
                    
                    debugStage = 10;
                    if ( ShipIcon != null && 
                         EffectiveTypeData != null && 
                         EffectiveTypeData.GUISprite_Icon != null )
                    {
                        ShipIcon.UpdateWith( EffectiveTypeData.GUISprite_Icon, false );
                        debugStage = 11;
                        ShipIcon.SetColor( factionCenterColor );
                    }

                    debugStage = 15;
                    if ( ShipIconBorder != null && 
                         EffectiveTypeData != null && 
                         EffectiveTypeData.GUISprite_IconBorder != null )
                    {
                        ShipIconBorder.UpdateWith( EffectiveTypeData.GUISprite_IconBorder, false );
                        debugStage = 16;
                        ShipIconBorder.SetColor( factionTrimColor );
                    }

                    debugStage = 20;

                    if ( ShipIconOverlay != null && EffectiveTypeData != null )
                    {
                        if ( EffectiveTypeData.GUISprite_IconOverlay != null )
                        {
                            ShipIconOverlay.UpdateWith( EffectiveTypeData.GUISprite_IconOverlay, false );
                        }
                        else
                        {
                            //icons for the sidebar to include control group as optional overlay when other overlay not present
                            if ( mainEntity != null && 
                                 EffectiveTypeData.IsFleetLeader && 
                                 mainEntity.GetFactionTypeSafe() == FactionType.Player )
                            {
                                Fleet mainFleet = mainEntity.GetFleetOrNull_Safe();
                                int controlGroup = mainFleet == null ? -1 : mainFleet.GetTiedToKeybindIndexForDisplay();
                                if ( controlGroup >= 0 && controlGroup < ExternalConstants.Instance.UISprite_ControlGroupNumbers.Length )
                                    ShipIconOverlay.UpdateWith( ExternalConstants.Instance.UISprite_ControlGroupNumbers[controlGroup] );
                                else
                                    ShipIconOverlay.UpdateWith( ExternalConstants.Instance.BlankGUISprite, false );
                            }
                            else
                                ShipIconOverlay.UpdateWith( ExternalConstants.Instance.BlankGUISprite, false );
                        }
                        debugStage = 21;
                    }

                    debugStage = 25;
                }
                catch ( Exception e )
                {
                    if ( DebugRenderContents )
                        ArcenDebugging.ArcenDebugLog( "Exception in bShipIcon.RenderContents at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                }
            }

            public override bool GetShouldBeHidden()
            {
                return EffectiveTypeData == null;
            }

            public override void Clear()
            {
                this.EffectiveTypeData = null;
                this.HighestMarkLevelData = null;
                this.MemIfModular = null;
                this.EntityCount = 0;
                this.EntityCountIncludingStack = 0;
                this.HighestMarkLevel = 0;
                this.ActualEntities.Clear();
            }
        }
        #endregion

        #region btnTextWithIcon
        public class btnTextWithIcon : ButtonAbstractBase, IShipSidebarButton
        {
            #region Data
            public static btnTextWithIcon Original;
            public btnTextWithIcon() { if ( Original == null ) Original = this; }

            public GameEntityTypeData EffectiveTypeData = null;
            public ShipIconStatus IconStatus = ShipIconStatus.Alive;
            public Faction FactionG = null;
            public GameEntityTypeData.MarkLevelStats HighestMarkLevelData = null;
            public FleetMembership MemIfModular = null;
            public int EntityCount = 0;
            public int EntityCountIncludingStack = 0;
            public byte HighestMarkLevel = 0;
            public readonly List<SafeSquadWrapper> ActualEntities = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 40, "Window_InGameSidebarShips-btnTextWithIcon-ActualEntities", 40 );

            private ArcenUIImageArray relatedImages = null;
            #endregion

            public override bool GetShouldBeHidden()
            {
                return EffectiveTypeData == null;
            }

            public override void Clear()
            {
                this.EffectiveTypeData = null;
                this.HighestMarkLevelData = null;
                this.MemIfModular = null;
                this.EntityCount = 0;
                this.EntityCountIncludingStack = 0;
                this.HighestMarkLevel = 0;
                this.ActualEntities.Clear();
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( this.EffectiveTypeData == null )
                    return;
                int debugCode = 0;
                try
                {
                    debugCode = 100;
                    #region Images Part

                    InitIfPossible();
                    if (relatedImages != null)
                    {
                        debugCode = 200;
                        Color factionCenterColor = ColorMath.White;
                        Color factionTrimColor = ColorMath.Black;
                        var mainEntity = this.ActualEntities[0].GetSquad();
                        debugCode = 210;
                        if (mainEntity != null)
                        {
                            debugCode = 220;
                            factionCenterColor = mainEntity.GetFactionCenterColor().TeamColor;
                            factionTrimColor = mainEntity.GetFactionTrimColor().TeamColor;
                        }
                        else if ( FactionG != null )
                        {
                            debugCode = 230;
                            factionCenterColor = FactionG.FactionCenterColor.TeamColor;
                            factionTrimColor = FactionG.FactionTrimColor.TeamColor;
                        }
                        debugCode = 300;
                        if (EffectiveTypeData != null && EffectiveTypeData.GUISprite_Icon != null)
                        {
                            relatedImages.SetSpriteAndColor(0, EffectiveTypeData.GUISprite_Icon, factionCenterColor);
                        }

                        if (EffectiveTypeData != null && EffectiveTypeData.GUISprite_IconBorder != null)
                        {
                            relatedImages.SetSpriteAndColor(1, EffectiveTypeData.GUISprite_IconBorder, factionTrimColor);
                        }
                        debugCode = 400;
                        if (EffectiveTypeData != null && EffectiveTypeData.GUISprite_IconOverlay != null)
                            relatedImages.SetSpriteAndColor(2, EffectiveTypeData.GUISprite_IconOverlay, Color.white);
                        else
                            relatedImages.SetSpriteAndColor(2, ExternalConstants.Instance.BlankGUISprite, Color.white);
                    }
                    #endregion
                    debugCode = 500;
                    Buffer.Add("<align=\"left\">");

                    if (this.MemIfModular != null)
                        Buffer.Add(this.MemIfModular.GetDisplayNameForSidebar());
                    else
                        Buffer.Add(this.EffectiveTypeData.DisplayNameForSidebar);
                    debugCode = 600;
                    // Ship mark level and count
                    {
                        debugCode = 700;
                        Buffer.Add("<sub>");

                        if (this.HighestMarkLevelData.MarkLevel != null)
                        {
                            Buffer.Add("<color=#").Add(this.HighestMarkLevelData.MarkLevel.ColorHex).Add(">");
                            Buffer.Add(" ").Add(this.HighestMarkLevelData.MarkLevel.MapDisplay);
                            Buffer.Add("</color>");
                        }

                        Buffer.Add("</sub>");
                        debugCode = 800;
                        Buffer.Add("<size=80%>");
                        {
                            debugCode = 900;
                            if (this.EntityCountIncludingStack > 1)
                            {
                                Buffer.Add(" x");
                                Buffer.Add(this.EntityCountIncludingStack);
                            }
                            debugCode = 1000;
                            Buffer.Add("<size=80%>");
                            switch (IconStatus)
                            {
                                case ShipIconStatus.InGuardPost:
                                    Buffer.EndColor().Add(" (GP)", "ff622b");
                                    break;
                                case ShipIconStatus.LoadedDrone:
                                    Buffer.EndColor().Add(" (L)", "ff622b");
                                    break;
                                case ShipIconStatus.BeingTransported:
                                    Buffer.EndColor().Add(" (T)", "59d2ff");
                                    break;
                                case ShipIconStatus.Remains:
                                    Buffer.EndColor().Add(" (D)", "808080");
                                    break;
                                case ShipIconStatus.Crippled:
                                    Buffer.EndColor().Add(" (D)", "808080");
                                    break;
                                case ShipIconStatus.UnderConstruction:
                                    Buffer.EndColor().Add(" (B)", "ffd966");
                                    break;
                                case ShipIconStatus.BeingClaimed:
                                    Buffer.EndColor().Add(" (C)", "ffd966");
                                    break;
                                    //case ShipIconStatus.Alive:
                                    //Buffer.Add( "(A)" );
                                    //break;
                                    //default:
                                    //Buffer.Add( "(?)" );
                                    //break;
                            }
                            debugCode = 1100;
                            Buffer.Add("</size>");

                        }
                        Buffer.Add("</size>");
                    }
                    
                    debugCode = 2000;
                    // Ship damage dealt
                    if (GetShowDps() != DpsHud.Mode.Off)
                    {
                        debugCode = 2100;
                        var dpshud = ExternalWorldBaseInfoSourceTable.Instance.GetRowByName("DpsHudBaseInfo")?.Singleton as DpsHud.IDpsHud;
                        if (dpshud != null)
                        {
                            debugCode = 2200;
                            // Presumably this window doesn't combine multiple squads into a single
                            // button, unless they are the same type.
                            // Since we collect damage dealt per type, we really could use any of
                            // them.
                            int num = 0;
                            for (int i = 0; i < this.ActualEntities.Count; i++)
                            {
                                var squad = ActualEntities[i].GetSquad();

                                float a;
                                num = dpshud.GetValue(squad, GetShowDps(), out a);
                                if (num > 0.0f)
                                    break;
                            }
                            debugCode = 2300;
                            //if ( num >= 1000 )
                            {
                                Buffer.Add("<line-height=0>");
                                Buffer.NewLine();
                                Buffer.Add("<align=\"right\">").Add("<margin-right=5>");//.Add( "<size=80%>" );

                                if (num > 1000)
                                {
                                    if (GetShowDps() == DpsHud.Mode.Damage)
                                    {
                                        Buffer.Add(ArcenExternalUIUtilities.Damage);
                                        Buffer.AddDamageNumbers(num);
                                    }
                                    else if (GetShowDps() == DpsHud.Mode.MetalLoss)
                                    {
                                        Buffer.Add(ArcenExternalUIUtilities.Metal);
                                        Buffer.AddDamageNumbers(num);
                                    }
                                }
                                else
                                    Buffer.Add(" ");

                                Buffer.Add("</align></line-height>");

                            }
                        }
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.LogSingleLine("Hit exception in btnTextWithIcon.GetTextToShowFromVolatile debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
                }
            }

            private void InitIfPossible()
            {
                if ( relatedImages == null && this.Element != null )
                {
                    relatedImages = new ArcenUIImageArray( ExternalConstants.Instance.BlankGUISprite );
                    relatedImages.InitializeFrom_ArcenUI_Button( this.Element );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return bShipIcon.HandleShipSidebarClick( this, this.EffectiveTypeData, this.ActualEntities, input );
            }

            public override void HandleMouseover()
            {
                bShipIcon.HandleShipSidebarMouseover( this.EffectiveTypeData, this.ActualEntities, this.FactionG, this.IconStatus, this.EntityCountIncludingStack, this.HighestMarkLevel );
            }

            public List<SafeSquadWrapper> GetActualEntities() => this.ActualEntities;
        }
        #endregion

        public interface IShipSidebarButton
        {
            //The button's currently-bound entity list. Callers that want to iterate the live
            //"on the planet, alive" subset should walk the planet's squads and filter via
            //GetActualEntities().Contains( entity ), the same way the (now-retired) DFShips wrapper did.
            List<SafeSquadWrapper> GetActualEntities();
        }

        #region btnFleet
        public class btnFleet : ImageButtonAbstractBase
        {
            public static btnFleet Original;
            public btnFleet() { if ( Original == null ) Original = this; }

            public Fleet FleetShown;
            public bool IsForLocalFleets = false;

            public override void Clear()
            {
                this.FleetShown = null;
            }

            private int CurrentHealthTicks = -4;
            private int CurrentShieldTicks = -4;

            public bool DebugUpdateContentFromVolatile = false;
            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {

                int debugStage = 0;
                try
                {
                    debugStage = 5;

                    Fleet fleet = this.FleetShown;

                    if ( fleet == null )
                    {
                        debugStage = 10;
                        SubTexts[0].Text.StartWritingToBuffer().Add( "???" );
                        debugStage = 11;
                    }
                    else
                    {
                        debugStage = 14;
                        //Faction localGlobalFaction = World_AIW2.Instance.GetLocalPlayerFaction();
                        //PlanetFaction localPlanetFaction = planet.GetPlanetFactionForFaction( localGlobalFaction );
                        debugStage = 15;
                        int strength = 0;
                        bool mobileOnly = true;
                        if ( !fleet.Faction.FleetsOnlyShowMobileStrengthInSidebar_Safe() )
                            strength = fleet.CalculateEffectiveCurrentFleetStrength_PlayerFleetsOnly( !mobileOnly ); //normal player path
                        else
                            strength = fleet.CalculateEffectiveCurrentFleetStrength_PlayerFleetsOnly( mobileOnly ); //for necromancers et al
                        debugStage = 151;
                        int fullCap = fleet.GetMaxCountOfFleet_ForUIOnly( false );
                        debugStage = 152;
                        int builtCount = fleet.GetCurrentCountOfFleet_ForUIOnly();
                        debugStage = 153;
                        long maxHealth = fleet.GetMaxHullOfFleet_ForUIOnly( false );
                        debugStage = 154;
                        long currentHealth = fleet.GetCurrentHullOfFleet_ForUIOnly();
                        debugStage = 16;
                        GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                        debugStage = 161;
                        Planet planet = centerpiece == null ? fleet.Planet : centerpiece.Planet;
                        if ( planet == null )
                        {
                            debugStage = 162;
                            if ( fleet.Planet != null )
                                planet = fleet.Planet;
                        }

                        if ( maxHealth < 0 )
                            maxHealth = 1;

                        debugStage = 17;
                        double healthPercent = (double)currentHealth / (double)maxHealth;

                        debugStage = 20;

                        bool isOpenFleetManagementWindow = Window_FleetManagementSidebarPopout.Instance.Is_Showing( fleet );

                        //main text - name of the fleet
                        {
                            ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                            //do some text resizing for longer fleet names
                            int fleetNameLen = fleet.GetName().Length;
                            if ( fleet.IsFleetInTransportLoadMode )
                                fleetNameLen += 4;
                            if ( fleet.Behavior == FleetBehavior.WardenMode || fleet.Behavior == FleetBehavior.HunterMode )
                                fleetNameLen += 3;
                            if ( fleetNameLen >= 17 )
                                buffer.Add("<size=75%>");
                            else if ( fleetNameLen >= 20 )
                                buffer.Add("<size=70%>");
                            else if ( fleetNameLen >= 23 )
                                buffer.Add("<size=65%>");
                            buffer.StartColor( isOpenFleetManagementWindow ? ColorMath.BrightLeaf :
                                (healthPercent < 0.33 ? ColorMath.Orange : (healthPercent < 0.66 ? ColorMath.LightYellow : Color.white)) )
                                .Add( fleet.GetName() );

                            if ( fleet.IsFleetInTransportLoadMode )
                            {
                                string color = "54b1ff";
                                if ( centerpiece != null && GameSettings.Current.GetBoolBySetting( "Fleets_ShowPercentLoadByColor" ))
                                {
                                    int percentTransported = centerpiece.CalculateUnitPercentTransported();
                                    Color col = EntityText.GetProportionalStrengthColor( (float)percentTransported / 100 );
                                    color = col.GetHexCode();
                                }
                                buffer.EndColor().StartColor( color ).Add( "  <voffset=0.2em><b>(T)</b></voffset>" ).EndColor();
                            }
                            if ( fleet.Behavior == FleetBehavior.WardenMode )
                                buffer.EndColor().Add( "  <voffset=0.2em><b><color=#5599ff>W</color></b></voffset>" );
                            else if ( fleet.Behavior == FleetBehavior.HunterMode )
                                buffer.EndColor().Add( "  <voffset=0.2em><b><color=#ff5555>H</color></b></voffset>" );
                            if ( fleetNameLen >= 15 )
                                buffer.Add("</size>");
                            debugStage = 21;
                            SubTexts[0].Text.FinishWritingToBuffer();
                        }

                        debugStage = 30;
                        if ( InputCaching.CalculateShouldTooltipsBeSuppressed() )
                        {
                            //planet name text
                            ArcenDoubleCharacterBuffer buffer = SubTexts[1].Text.StartWritingToBuffer();
                            if ( planet == null )
                                buffer.Add( "???" );
                            else
                            {
                                Faction controllingFaction = planet.GetControllingFaction();
                                if ( controllingFaction != null )
                                    buffer.StartColor( controllingFaction.FactionCenterColor.TeamColorBrighter );
                                buffer.Add( planet.Name );
                            }

                            SubTexts[1].Text.FinishWritingToBuffer();
                        }
                        else
                        {
                            //friendly and enemy strength if relevant
                            ArcenDoubleCharacterBuffer buffer = SubTexts[1].Text.StartWritingToBuffer();
                            int friendlyStr = fleet.Centerpiece.GetSquad().PlanetFaction.DataByStance[FactionStance.Self].TotalStrength + fleet.Centerpiece.GetSquad().PlanetFaction.DataByStance[FactionStance.Friendly].TotalStrength;
                            int enemyStr = fleet.Centerpiece.GetSquad().PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                            if ( enemyStr > 0 )
                            {
                                buffer.StartColor( "00aaff" );
                                buffer.Add( ArcenExternalUIUtilities.Strength, "00aaff" );
                                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, friendlyStr, true, true );
                                buffer.EndColor();
                                buffer.Add("  ");
                                buffer.StartColor( "ff0000" );
                                buffer.Add( ArcenExternalUIUtilities.Strength, "ff0000" );
                                ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, enemyStr, true, true );
                                buffer.EndColor();
                            }
                            //originally we showed the number of actual ships you had, but that's not as important as the Strength of your ships,
                            //so I'm repurposing this to show enemy strength
                            // SubTexts[1].Text.StartWritingToBuffer().StartColor( builtCount < fullCap ? Color.white : ColorMath.LightGreen ).Add(
                            //     builtCount ).Add( "/" ).Add( fullCap );
                            debugStage = 31;
                            SubTexts[1].Text.FinishWritingToBuffer();
                        }

                        debugStage = 40;

                        if ( centerpiece != null && centerpiece.GetIsCrippled() )
                        {
                            //crippled text
                            SubTexts[2].Text.StartWritingToBuffer().StartColor( ColorMath.Gray ).Add( "CRIPPLED" );
                            debugStage = 41;
                            SubTexts[2].Text.FinishWritingToBuffer();
                        }
                        else
                        {
                            //health text
                            ArcenDoubleCharacterBuffer buffer = SubTexts[2].Text.StartWritingToBuffer();

                            buffer.Add( ArcenExternalUIUtilities.Strength );
                            buffer.StartColor( ArcenExternalUIUtilities.Strength.Color );
                            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, strength, true, true );
                            buffer.EndColor().Add( "</size></voffset>   " );

                            buffer.StartColor( healthPercent < 0.33 ? ColorMath.LightOrange : (healthPercent < 0.66 ? ColorMath.LightYellow : ColorMath.LightGreen) )
                                .Add( (int)System.Math.Round( healthPercent * 100 ) ).Add( "%" );
                            debugStage = 41;
                            SubTexts[2].Text.FinishWritingToBuffer();
                        }

                        ArcenUI_ImageButton imageParent = this.Element as ArcenUI_ImageButton;

                        if ( centerpiece != null )
                        {
                            #region HealthTicks
                            bool showCrippledAsNoHealth = false; //we report whether the flagship is crippled elsewhere; showing the actual
                                                                 //health ticks allows the player to monitor repairs
                            int newHealthTicks = centerpiece.GetHealthTicks( showCrippledAsNoHealth );
                            if ( newHealthTicks != this.CurrentHealthTicks )
                            {
                                this.CurrentHealthTicks = newHealthTicks;
                                if ( newHealthTicks < 0 || newHealthTicks >= 10 )
                                    SubImages[0].SetSpriteIfNeeded( imageParent.ExtraSpriteDict[21] ); //blank
                                else
                                    SubImages[0].SetSpriteIfNeeded( imageParent.ExtraSpriteDict[newHealthTicks] );
                            }
                            #endregion

                            debugStage = 5000;

                            #region ShieldTicks
                            int newShieldTicks = centerpiece.GetShieldTicks();
                            if ( newShieldTicks != this.CurrentShieldTicks )
                            {
                                this.CurrentShieldTicks = newShieldTicks;
                                if ( newShieldTicks < 0 || newShieldTicks >= 10 )
                                    SubImages[1].SetSpriteIfNeeded( imageParent.ExtraSpriteDict[21] ); //blank
                                else
                                    SubImages[1].SetSpriteIfNeeded( imageParent.ExtraSpriteDict[10 + newShieldTicks] );
                            }
                            #endregion
                        }

                        debugStage = 60;
                        //keybind text
                        int keybindIndexForDisplay = fleet.GetTiedToKeybindIndexForDisplay();
                        if ( keybindIndexForDisplay < 0 )
                            SubTexts[4].Text.StartWritingToBuffer().Add( string.Empty ); //no hotkey
                        else if ( centerpiece.Planet != Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed() )
                        {
                            SubTexts[4].Text.StartWritingToBuffer().Add( keybindIndexForDisplay ); //hotkey but not on local planet
                        }
                        else
                        {
                            SubTexts[4].Text.StartWritingToBuffer().Add( keybindIndexForDisplay, "00cc60" ); //hotkey and on local planet, so show with colour
                        }
                        debugStage = 61;
                        SubTexts[4].Text.FinishWritingToBuffer();
                    }
                    debugStage = 20;
                    debugStage = 21;
                }
                catch ( Exception e )
                {
                    if ( DebugUpdateContentFromVolatile )
                        ArcenDebugging.ArcenDebugLog( "Exception in btnFleetIcon.UpdateContentFromVolatile at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                }
            }

            private static readonly List<FleetMembership> SortedShipLines = List<FleetMembership>.Create_WillNeverBeGCed( 3000, "Window_InGameSidebarShips-btnFleet-SortedShipLines" );
            public static void WriteFleetHealth( Fleet fleet, ArcenDoubleCharacterBuffer buffer )
            {
                int debugCode = 0;
                try{
                  if ( fleet == null )
                      return;
                  if ( fleet.Centerpiece.GetSquad() == null )
                      return;
                  Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                  if ( localFaction == null )
                      return;

                  debugCode = 100;
                  int allMetalCosts = 0;
                  bool showVerboseDetails = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1();
                  bool showFleetEffectiveness = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key2();
                  bool showEnemyMetrics = showFleetEffectiveness && InputCaching.CalculateHoldToSeeShipStrengthsAndWeaknesses();
                  if ( showFleetEffectiveness )
                  {
                      if ( showVerboseDetails )
                          (fleet.BaseInfo as FleetMetricsBaseInfo)?.AddMetricsToTooltipForFleet( buffer );
                      else if ( showEnemyMetrics )
                          (fleet.BaseInfo as FleetMetricsBaseInfo)?.AppendEnemyMetricsCompact( buffer );
                      else
                          (fleet.BaseInfo as FleetMetricsBaseInfo)?.AppendFleetMetricsCompact( buffer );
                      return;
                  }
                    int currentStrength = fleet.GetCurrentStrengthOfFleet_ForUIOnly( false );
                  int maxStrength = fleet.GetMaxStrengthOfFleet_ForUIOnly( false );
                  float ratio = (float)currentStrength / maxStrength;
                  Color fleetColor = EntityText.GetProportionalStrengthColor( ratio );
                  bool exitAfterStatus = false;
                  buffer.StartColor( fleetColor );
                  buffer.Add( fleet.GetName() ).EndColor();
                  GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                  if ( centerpiece == null )
                  {
                      buffer.Add("Centerpiece is null?");
                      return;
                  }
                  Faction controllingFaction = centerpiece.PlanetFaction.Faction;
                  bool localPlayerOwns = (controllingFaction == localFaction);
                  if ( fleet.TiedToKeybindIndexOneIndexed >= 0 )
                  {
                      buffer.Add( " (" ).Add( fleet.TiedToKeybindIndexOneIndexed ).Add( ")" );
                  }
                  debugCode = 200;
                  buffer.Add( " 在 " ).Add( centerpiece.GetPlanetName_Safe(), centerpiece.Planet.GetControllingOrInfluencingFaction().FactionCenterColor.ColorHexBrighter ).Add( " " );
                  debugCode = 225;
                  Planet enRouteDestOrNull = centerpiece.Orders?.GetFinalDestinationOrNull();
                  if ( enRouteDestOrNull == centerpiece.Planet )
                      enRouteDestOrNull = null;
                  Color fullHealthColor = EntityText.GetProportionalStrengthColor( 1 );
                  exitAfterStatus = false;
                  if ( currentStrength == maxStrength )
                  {
                      debugCode = 300;
                      buffer.Add( "满 " ).Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon ).Add( ": " ).StartColor( fleetColor );
                      ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, maxStrength, true, true );
                      buffer.EndColor();
                      exitAfterStatus = true;
                  }
                  else
                  {
                      debugCode = 350;
                      buffer.StartColor( fleetColor ).Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon ).Add(" ");
                      if ( showVerboseDetails )
                          buffer.Add("存活飞船强度：");
                      ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, currentStrength, true, true );
                      {
                          buffer.Add( "/" ).EndColor().StartColor( fullHealthColor );
                          ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, maxStrength, true, true );
                      }
                      buffer.EndColor();
                      if ( showVerboseDetails )
                      {
                          long currentHealth = fleet.GetCurrentHullOfFleet_ForUIOnly();
                          long maxHealth = fleet.GetMaxHullOfFleet_ForUIOnly( false );
                          double healthPercent = (double)currentHealth / (double)maxHealth;
                          buffer.Add(". ").StartColor( (healthPercent < 0.33 ? ColorMath.Orange : (healthPercent < 0.66 ? ColorMath.LightYellow : ColorMath.LightGreen)) )
                              .Add( " 舰队总体状态：").Add( (int)System.Math.Round( healthPercent * 100 ) ).Add( "%" ).EndColor();
                      }
                  }
                  debugCode = 400;
                  buffer.Add( "." );
                  buffer.Add( " 旗舰" );
                  if ( centerpiece.GetIsCrippled() )
                      buffer.Add( " 已损坏", "ffa1a1" );
                  else
                  {
                      float centerpieceRatio = (float)centerpiece.GetCurrentHullPoints() / centerpiece.GetMaxHullPoints();
                      Color centerpieceColor = EntityText.GetProportionalStrengthColor( centerpieceRatio );
                      buffer.Add( " at " );
                      buffer.StartColor( centerpieceColor );
                      buffer.Add( (int)(centerpieceRatio * 100) );
                      buffer.Add( "%" );
                      buffer.EndColor();
                      if ( ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() )
                          ArcenExternalUIUtilities.AppendBar( buffer, (int)(centerpieceRatio * 100), centerpieceColor, 8 );

                  }
                  debugCode = 500;
                  if ( centerpiece.ActiveHack != null )
                  {
                      buffer.Add( " 且正在入侵", "a1ffa1" );
                  }
                  //string typeDataColor = "c0c0c0";
                  if ( fleet.SupportingFactoriesInRange.Count == 0 )
                  {
                      buffer.Add( " 且超出建造范围", "8a8a8a" );
                      //typeDataColor = "8a8a8a";
                  };
                  if ( fleet.IsFleetConstructionPaused )
                  {
                      buffer.Add( " 已暂停建造。" );
                  }
                  else if ( fleet.IsFleetConstructionBlocked )
                  {
                        buffer.Add( " 建造被阻止。" );
                  }
                  buffer.Add( ". " );
                  if ( enRouteDestOrNull != null )
                  {
                      int hops = centerpiece.Planet.GetHopsTo( enRouteDestOrNull );
                      buffer.Add( "\n正在前往 " )
                          .Add( enRouteDestOrNull.Name, enRouteDestOrNull.GetControllingOrInfluencingFaction().FactionCenterColor.ColorHexBrighter )
                          .Add( "（" ).Add( hops ).Add( hops == 1 ? " 次跳跃" : " 次跳跃" ).Add( "）。" );
                  }
                  if ( controllingFaction != localFaction )
                  {
                      buffer.Add( "此舰队属于 " ).Add( fleet.Faction.GetDisplayName(), fleet.Faction.FactionCenterColor.ColorHexBrighter ).Add( "。" );
                  }
                  debugCode = 600;
                  bool foundAnyToBuild = false;
                  int metalToRebuild = 0;

                  if ( showVerboseDetails )
                      buffer.Add( "\n飞船生产线详情：\n" );
                  SortedShipLines.Clear();
                  debugCode = 700;
                  foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                  {
                      debugCode = 800;
                      debugCode = 5912;
                      if ( mem == null )
                          continue;
                      if ( mem.TypeData.IsDrone || mem.TypeData.SelfConstructs )
                          continue;
                      if ( mem.TypeData.IsFleetLeader )
                          continue;
                      if ( mem.EffectiveSquadCap <= 0 )
                          continue;
                      SortedShipLines.Add(mem);
                  }
                  debugCode = 900;
                  debugCode = 1000;
                  SortedShipLines.Sort( static delegate ( FleetMembership L, FleetMembership R )
                  {
                      return R.GetStrengthPerSquad_PlayerFleetsOnly().CompareTo( L.GetStrengthPerSquad_PlayerFleetsOnly() );
                  } );
                  for ( int j = 0; j < SortedShipLines.Count; j++ )
                  {
                      debugCode = 1100;
                      FleetMembership mem = SortedShipLines[j];
                      foundAnyToBuild = true;
                      int remainingShips = mem.EffectiveSquadCap - mem.GetRemainingCap( false, -1, ExtraFromStacks.IncludePrecalc );
                      if ( mem.EffectiveSquadCap > 0 &&
                           remainingShips > mem.EffectiveSquadCap )
                          remainingShips = mem.EffectiveSquadCap;
                      int costToRebuildThisShipLine = mem.GetRemainingCap( false, -1, ExtraFromStacks.IncludePrecalc ) * mem.GetMetalCost();
                      debugCode = 1200;
                      if ( !mem.IsFleetMembershipConstructionPaused && fleet.Faction == localFaction )
                      {
                          debugCode = 1300;
                          //only include non-paused fleets that we own
                          metalToRebuild += costToRebuildThisShipLine;
                          allMetalCosts += costToRebuildThisShipLine;
                      }
                      debugCode = 1400;
                      string markColor = mem.ForMark.MarkLevel.ColorHex;

                      if( !showVerboseDetails )
                      {
                          if ( j == 0 || j % 3 == 0 )
                              buffer.Add(" \n");
                          else
                              buffer.Add(" \t");
                      }

                      int memStrength = mem.GetStrengthPerSquad_PlayerFleetsOnly() * remainingShips;

                      buffer.AddShipIconInline(mem.TypeData, controllingFaction);

                      if ( showVerboseDetails )
                          buffer.Add( mem.TypeData.DisplayNameForSidebar, markColor ).Add(": ");
                      float memRatio = (float)remainingShips / mem.EffectiveSquadCap;
                      Color memColor = EntityText.GetProportionalStrengthColor( memRatio );
                      buffer.Add("  ");

                      if ( remainingShips == mem.EffectiveSquadCap )
                      {
                          buffer.Add( remainingShips, memColor);
                          if ( showVerboseDetails )
                              buffer.Add(".");
                      }
                      else
                      {
                          buffer.StartColor( memColor ).Add( remainingShips ).Add( "/" ).Add( mem.EffectiveSquadCap ).EndColor();
                          if ( showVerboseDetails )
                              buffer.Add(", ");
                          else if ( mem.MetalSpentConstructingCurrentReplacement > 0 )
                          {
                              buffer.Add("<size=80%>").StartColor( ColorMath.LightLeafGreen ).Add(" (");
                              buffer.Add( (((float)mem.MetalSpentConstructingCurrentReplacement / (float)mem.GetMetalCost()) * 100f).ToString( "00.0" ) );
                              buffer.Add( "%" ).Add( ") " ).EndColor();
                              buffer.Add("</size>");
                          }
                      }

                      if ( showVerboseDetails )
                      {
                          FInt fintratio = (FInt)remainingShips / (FInt)mem.EffectiveSquadCap;
                          if ( fintratio < 1 )
                              buffer.Add("\t").StartColor(memColor).Add(" ").Add( (fintratio*100).ReadableString ).Add("%").EndColor();

                          buffer.Add("  ");

                          buffer.Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon );
                          ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, memStrength, true, true );

                          if ( mem.IsFleetMembershipConstructionPaused )
                              buffer.Add( " (已暂停) " );
                      }
                      if ( showVerboseDetails || j == (SortedShipLines.Count - 1) )
                          buffer.Add("\n");
                  }
                  debugCode = 2000;
                  {
                      if ( !foundAnyToBuild )
                          buffer.Add( "已完成所有建造\n" );
                      else if ( metalToRebuild > 0 )
                      {
                          buffer.Add( "重建损失飞船所需金属：" );
                          buffer.StartColor( "ccccee" );
                          buffer.Add( "<b>" );
                          ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, metalToRebuild, true, false );
                          buffer.Add( "</b>" );
                          buffer.EndColor();
                          buffer.Add( "." );
                      }
                  }
                  buffer.Add( "\n\n" );
                  debugCode = 2100;
                  if ( exitAfterStatus )
                      buffer.Add( "\n\n" ); //add the newlines we missed earlier

                  if ( localPlayerOwns )
                  {
                      if ( allMetalCosts > 0 && allMetalCosts < localFaction.StoredMetal)
                      {
                          buffer.Add( "重建损失后将剩余 ", "47b247" );
                          buffer.StartColor( "ccccee" );
                          ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, (localFaction.StoredMetal - allMetalCosts).IntValue, true, false );
                          buffer.EndColor();
                          buffer.Add( " 金属可用于重建损失。\n\n", "47b247" );
                      }
                      else if ( allMetalCosts > localFaction.StoredMetal )
                      {
                          buffer.Add( "你缺少 ", "b24747" );
                          buffer.StartColor( "ccccee" );
                          ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, (allMetalCosts - localFaction.StoredMetal).IntValue, true, false );
                          buffer.EndColor();
                          buffer.Add( " 金属不足以重建损失。\n\n", "b24747" );
                      }
                  }
                  debugCode = 2200;
                  //first print our and enemy strength, then set up for the fleet line details
                  int enemyStr = centerpiece.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                  int myStr = centerpiece.PlanetFaction.DataByStance[FactionStance.Self].TotalStrength + centerpiece.PlanetFaction.DataByStance[FactionStance.Friendly].TotalStrength;
                  if ( enemyStr > 0 )
                  {
                      buffer.Add( "\n\n你在 " + centerpiece.GetPlanetName_Safe() +" 上的友军总强度：");
                      buffer.Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon );
                      buffer.StartColor( fleet.Faction.FactionCenterColor.ColorHexBrighter );
                      ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, myStr, true, true );
                      buffer.EndColor();
                      buffer.Add( "\n敌方强度：" ).Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon );
                      buffer.StartColor( "ff0000" );
                      ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, enemyStr, true, true );
                      buffer.EndColor();
                      buffer.Add( "\n\n " );
                  }

                  buffer.Add( FontSizes.SLIGHTLY_SMALLER_SIZE_STRING );
                  buffer.Add( "<color=#d18444>按住 <color=#996f4c>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" ) ).Add( "</color> 查看各飞船生产线详情。</color>\n" );
                  {
                      if ( ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() )
                      {
                          string key2 = InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key2" );
                          string key3 = InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "HoldToSeeShipStrengthsAndWeaknesses" );
                          buffer.Add( "<color=#d18444>按住 <color=#996f4c>" ).Add( key2 ).Add( "</color> 查看舰队效能指标。</color>\n" );
                          buffer.Add( "<color=#d18444>按住 <color=#996f4c>" ).Add( key2 ).Add( " + " ).Add( key3 ).Add( "</color> 查看敌方指标。</color>\n" );
                          buffer.Add( "<color=#d18444>按住 <color=#996f4c>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" ) ).Add( " + " ).Add( key2 ).Add( "</color> 查看完整详细指标。</color>\n" );
                      }
                  }
                }catch(Exception e)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in WriteFleetHealth debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
                }
            }

            private readonly static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_InGameSidebarShips-btnFleet-tooltipBuffer" );
            public override void HandleMouseover()
            {
                Fleet fleet = this.FleetShown;
                if ( fleet == null )
                    return;

                World_AIW2.Instance.FocusedSquadForMapDarkening = fleet.Centerpiece.GetSquad();

                if ( Window_FleetManagementSidebarPopout.Instance.Is_Showing( fleet ) )
                    return; //don't show the tooltip for the fleet we've got the management window open for

                WriteFleetHealth( fleet, tooltipBuffer );
                if ( !InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key2() )
                {
                    tooltipBuffer.Add( FontSizes.MUCH_SMALLER_SIZE_PLUS_A_TAD_STRING ).Add( "<color=#3f6c9e>按住 ").Add(InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" ) ).Add("</color><color=#4486d1> 并左键点击可切换此舰队的监视状态。  " ).Add("\n");
                    tooltipBuffer.Add( "<size=80%><color=#3f6c9e>右键点击</color><color=#4486d1> 可选择此舰队（按住Shift可将其添加到当前选择中）。\n" );
                    tooltipBuffer.Add( "<color=#3f6c9e>中键点击</color> 可将视图中心对准舰队旗舰。</color></size>  " );
                }

                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
            }
            public override MouseHandlingResult HandleClick( MouseHandlingInput input )
            {
                Fleet fleet = this.FleetShown;
                if ( fleet == null )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "BUG: FleetShown is null", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                //when right-clicking, select the fleet
                if ( input.RightButtonClicked )
                {
                    Input_FleetHandler.SelectFleetGroup( -1, fleet, FleetSelectionType.SelectOnly, GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    return MouseHandlingResult.None;
                }
                //when middle-clicking, center on the fleet
                if ( input.MiddleButtonClicked )
                {
                    Input_FleetHandler.SelectFleetGroup( -1, fleet, FleetSelectionType.CenterOnly, GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    return MouseHandlingResult.None;
                }

                if ( InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1() )
                {
                    //toggle this fleet's Watched status
                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditFleetData], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    command.RelatedIntegers.Add( fleet.FleetID );
                    command.RelatedIntegers.Add( PlayerAccount.Local.PlayerPrimaryKeyID );
                    command.RelatedString = "ToggleIsFleetOnPlayerWatchlist";
                    command.RelatedBool = !fleet.GetIsFleetOnPlayerWatchlist( PlayerAccount.Local.PlayerPrimaryKeyID );
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                    return MouseHandlingResult.None;
                }

                //Current = InGameSidebarType.Fleets;
                Window_FleetManagementSidebarPopout.Instance.Open( fleet );
                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                return this.FleetShown == null || ( this.IsForLocalFleets ? !customParent.open_LocalFleetList : !customParent.open_WatchedFleetList );
            }
        }
        #endregion

        #region bShipIconPool
        public class bShipIconPool
        {
            private static readonly List<bShipIcon> globalPoolList_Icon = List<bShipIcon>.Create_WillNeverBeGCed( 3000, "Window_InGameSidebarShips-bShipIconPool-globalPoolList_Icon" );
            private static readonly  List<btnTextWithIcon> globalPoolList_Text = List<btnTextWithIcon>.Create_WillNeverBeGCed( 3000, "Window_InGameSidebarShips-bShipIconPool-globalPoolList_Text" );
            private static int currentGlobalIndex_Icon = -1;
            private static int currentGlobalIndex_Text = -1;

            private readonly List<bShipIcon> inUseList_Icon = List<bShipIcon>.Create_WillNeverBeGCed( 3000, "Window_InGameSidebarShips-bShipIconPool-inUseList_Icon" );
            private readonly List<btnTextWithIcon> inUseList_Text = List<btnTextWithIcon>.Create_WillNeverBeGCed( 3000, "Window_InGameSidebarShips-bShipIconPool-inUseList_Text" );

            public bShipIconPool( int MinEntries )
            {
                if ( globalPoolList_Icon.Count == 0 )
                    globalPoolList_Icon.Add( bShipIcon.Original );

                if ( globalPoolList_Text.Count == 0 )
                    globalPoolList_Text.Add( btnTextWithIcon.Original );

                while ( globalPoolList_Icon.Count < MinEntries )
                    Create_bShipIcon();

                while ( globalPoolList_Text.Count < MinEntries )
                    Create_btnTextWithIcon();
            }

            private static int maxRemainingAllowedToAddBeforeNextClear = 0;
            public static void ClearAll( int MaxToAddAtASingleTime )
            {
                maxRemainingAllowedToAddBeforeNextClear = MaxToAddAtASingleTime;

                currentGlobalIndex_Icon = 0;// -1; SKIP THE FIRST ONE!  It should always be unused, because... reasons?  Its sizing goes nuts, at any rate
                currentGlobalIndex_Text = 0;// -1; SKIP THE FIRST ONE!  It should always be unused, because... reasons?  Its sizing goes nuts, at any rate
            }

            public void ClearSingle()
            {
                for ( int i = 0; i < inUseList_Icon.Count; i++ )
                    inUseList_Icon[i].Clear();
                inUseList_Icon.Clear();

                for ( int i = 0; i < inUseList_Text.Count; i++ )
                    inUseList_Text[i].Clear();
                inUseList_Text.Clear();
            }

            public List<bShipIcon> GetInUseList_Icon()
            {
                return this.inUseList_Icon;
            }

            public List<btnTextWithIcon> GetInUseList_Text()
            {
                return this.inUseList_Text;
            }

            public int GetInUseCount_Icon()
            {
                return this.inUseList_Icon.Count;
            }

            public int GetInUseCount_Text()
            {
                return this.inUseList_Text.Count;
            }

            public bShipIcon GetOrAddEntryBasedOnMatch_OrNullIfTimeSlicingTooManyAdds_Icon( GameEntityTypeData TypeData, ShipIconStatus IconStatus, Faction FactionG, FleetMembership MemIfModular )
            {
                if ( FactionG == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Warning: FactionG is null",Verbosity.DoNotShow );
                }
                if ( TypeData == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Warning: TypeData is null",Verbosity.DoNotShow );
                }

                if ( !TypeData.IsModular )
                    MemIfModular = null;

                bShipIcon icon;
                if ( TypeData != null && !TypeData.IsFleetLeader ) //fleet leaders should never stack
                {
                    for ( int i = 0; i < inUseList_Icon.Count; i++ )
                    {
                        icon = inUseList_Icon[i];
                        if ( icon.EffectiveTypeData == TypeData &&
                            icon.IconStatus == IconStatus &&
                            icon.FactionG == FactionG &&
                            ( ( icon.MemIfModular == null && MemIfModular == null ) ||
                            icon.MemIfModular == MemIfModular ) )
                            return icon;
                    }
                }

                //didn't find it, so...
                currentGlobalIndex_Icon++;
                if ( currentGlobalIndex_Icon < globalPoolList_Icon.Count )
                {
                    //had enough items in pool to just grab one
                    icon = globalPoolList_Icon[currentGlobalIndex_Icon];
                    icon.EffectiveTypeData = TypeData;
                    icon.IconStatus = IconStatus;
                    icon.FactionG = FactionG;
                    icon.MemIfModular = MemIfModular;
                    inUseList_Icon.Add( icon );
                    return icon;
                }

                maxRemainingAllowedToAddBeforeNextClear--;
                if ( maxRemainingAllowedToAddBeforeNextClear <= 0 )
                    return null;

                //did NOT have enough items in pool, so creating a duplicate now                
                icon = Create_bShipIcon();
                icon.EffectiveTypeData = TypeData;
                icon.IconStatus = IconStatus;
                icon.FactionG = FactionG;
                icon.MemIfModular = MemIfModular;
                this.inUseList_Icon.Add( icon );
                return icon;
            }

            public btnTextWithIcon GetOrAddEntryBasedOnMatch_OrNullIfTimeSlicingTooManyAdds_Text( GameEntityTypeData TypeData, ShipIconStatus IconStatus, Faction FactionG, FleetMembership MemIfModular )
            {
                if ( FactionG == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Warning: FactionG is null",Verbosity.DoNotShow );
                }
                if ( TypeData == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Warning: TypeData is null",Verbosity.DoNotShow );
                }

                if ( !TypeData.IsModular )
                    MemIfModular = null;

                btnTextWithIcon text;
                if ( TypeData != null && !TypeData.IsFleetLeader ) //fleet leaders should never stack
                {
                    for ( int i = 0; i < inUseList_Text.Count; i++ )
                    {
                        text = inUseList_Text[i];
                        if ( text.EffectiveTypeData == TypeData &&
                            text.IconStatus == IconStatus &&
                            text.FactionG == FactionG &&
                            ((text.MemIfModular == null && MemIfModular == null) ||
                            text.MemIfModular == MemIfModular) )
                            return text;
                    }
                }

                //didn't find it, so...
                currentGlobalIndex_Text++;
                if ( currentGlobalIndex_Text < globalPoolList_Text.Count )
                {
                    //had enough items in pool to just grab one
                    text = globalPoolList_Text[currentGlobalIndex_Text];
                    text.EffectiveTypeData = TypeData;
                    text.IconStatus = IconStatus;
                    text.FactionG = FactionG;
                    text.MemIfModular = MemIfModular;
                    inUseList_Text.Add( text );
                    return text;
                }

                maxRemainingAllowedToAddBeforeNextClear--;
                if ( maxRemainingAllowedToAddBeforeNextClear <= 0 )
                    return null;

                //did NOT have enough items in pool, so creating a duplicate now                
                text = Create_btnTextWithIcon();
                text.EffectiveTypeData = TypeData;
                text.IconStatus = IconStatus;
                text.FactionG = FactionG;
                text.MemIfModular = MemIfModular;
                this.inUseList_Text.Add( text );
                return text;
            }

            private static bShipIcon Create_bShipIcon()
            {
                ArcenUI_ImageButton button = (ArcenUI_ImageButton)bShipIcon.Original.Element.DuplicateSelf();
                bShipIcon icon = (bShipIcon)button.Controller;
                globalPoolList_Icon.Add( icon );
                return icon;
            }

            private static btnTextWithIcon Create_btnTextWithIcon()
            {
                ArcenUI_Button button = (ArcenUI_Button)btnTextWithIcon.Original.Element.DuplicateSelf();
                btnTextWithIcon icon = (btnTextWithIcon)button.Controller;
                globalPoolList_Text.Add( icon );
                return icon;
            }

            public const int PREFER_LEFT = -1;
            public const int PREFER_RIGHT = 1;
            public const int PREFER_NEITHER = 0;

            public void Sort()
            {
                if ( this.inUseList_Icon.Count > 1 )
                {
                    this.inUseList_Icon.StableSort( static delegate ( bShipIcon Left, bShipIcon Right )
                    {
                        int val = 0;
                        //sort command stations first, desc
                        val = Right.EffectiveTypeData.IsCommandStation.CompareTo( Left.EffectiveTypeData.IsCommandStation );
                        if ( val != 0 )
                            return val;

                        //then fleet leaders, desc
                        val = Right.EffectiveTypeData.IsFleetLeader.CompareTo( Left.EffectiveTypeData.IsFleetLeader );
                        if ( val != 0 )
                            return val;

                        //then major AI structures, desc
                        val = Right.EffectiveTypeData.IsMajorAIStructure.CompareTo( Left.EffectiveTypeData.IsMajorAIStructure );
                        if ( val != 0 )
                            return val;

                        //then noncombatants, desc
                        val = Right.EffectiveTypeData.IsCombatant.CompareTo( Left.EffectiveTypeData.IsCombatant );
                        if ( val != 0 )
                            return val;

                        //then sort by faction, asc
                        val = Left.FactionG.GetDisplayName().CompareTo( Right.FactionG.GetDisplayName() );
                        if ( val != 0 )
                            return val;
                        else 
                        {
                            //if the faction name is identical, sort by faction id
                            val = Left.FactionG.FactionIndex.CompareTo( Right.FactionG.FactionIndex );
                            if ( val != 0 )
                                return val;
                        }

                        //then other stationary objects (mobile asc)
                        val = Right.EffectiveTypeData.IsMobile.CompareTo( Left.EffectiveTypeData.IsMobile );
                        if ( val != 0 )
                            return val;

                        //last is turrets, asc (not-turrets first)
                        val = Left.EffectiveTypeData.IsTurret.CompareTo( Right.EffectiveTypeData.IsTurret );
                        if ( val != 0 )
                            return val;

                        //then elites
                        val = Right.EffectiveTypeData.IsElite.CompareTo( Left.EffectiveTypeData.IsElite );
                        if ( val != 0 )
                            return val;

                        //then large ships
                        val = Right.EffectiveTypeData.IsLargeShip.CompareTo( Left.EffectiveTypeData.IsLargeShip );
                        if ( val != 0 )
                            return val;

                        //last is strikecraft, asc (not-strikecraft first)
                        val = Left.EffectiveTypeData.IsStrikecraft.CompareTo( Right.EffectiveTypeData.IsStrikecraft );
                        if ( val != 0 )
                            return val;

                        ////sort by strength, strongest to weakest?
                        //val = Right.HighestMarkLevelData.StrengthPerSquad_CalculatedWithNullFleetMembership.CompareTo( Left.HighestMarkLevelData.StrengthPerSquad_CalculatedWithNullFleetMembership );
                        //if ( val != 0 )
                        //    return val;

                        //then sort by name
                        val = Left.EffectiveTypeData.DisplayNameForSidebar.CompareTo( Right.EffectiveTypeData.DisplayNameForSidebar );
                        if ( val != 0 )
                            return val;

                        //then sort by status, desc
                        val = Right.IconStatus.CompareTo( Left.IconStatus );
                        if ( val != 0 )
                            return val;

                        //finally sort by row index, just to make sure it's absolute unique even if things have the same name
                        val = Left.EffectiveTypeData.RowIndexNonSim.CompareTo( Right.EffectiveTypeData.RowIndexNonSim );
                        if ( val != 0 )
                            return val;

                        //wait, we still did not find it?  Sort by highes mark level
                        val = Left.HighestMarkLevel.CompareTo( Right.HighestMarkLevel );
                        if ( val != 0 )
                            return val;

                        //wait, we still did not find it?  Sort by unit counts
                        val = Right.ActualEntities.Count.CompareTo( Left.ActualEntities.Count );
                        if ( val != 0 )
                            return val;
                        return 0;
                    } );
                }

                if ( this.inUseList_Text.Count > 1 )
                {
                    this.inUseList_Text.StableSort( static delegate ( btnTextWithIcon Left, btnTextWithIcon Right )
                    {
                        int val = 0;
                        //sort command stations first, desc
                        val = Right.EffectiveTypeData.IsCommandStation.CompareTo( Left.EffectiveTypeData.IsCommandStation );
                        if ( val != 0 )
                            return val;

                        //then fleet leaders, desc
                        val = Right.EffectiveTypeData.IsFleetLeader.CompareTo( Left.EffectiveTypeData.IsFleetLeader );
                        if ( val != 0 )
                            return val;

                        //then major AI structures, desc
                        val = Right.EffectiveTypeData.IsMajorAIStructure.CompareTo( Left.EffectiveTypeData.IsMajorAIStructure );
                        if ( val != 0 )
                            return val;

                        //then noncombatants, desc
                        val = Right.EffectiveTypeData.IsCombatant.CompareTo( Left.EffectiveTypeData.IsCombatant );
                        if ( val != 0 )
                            return val;

                        //then sort by faction, asc
                        val = Left.FactionG.GetDisplayName().CompareTo( Right.FactionG.GetDisplayName() );
                        if ( val != 0 )
                            return val;
                        else
                        {
                            //if the faction name is identical, sort by faction id
                            val = Left.FactionG.FactionIndex.CompareTo( Right.FactionG.FactionIndex );
                            if ( val != 0 )
                                return val;
                        }

                        //then other stationary objects (mobile asc)
                        val = Right.EffectiveTypeData.IsMobile.CompareTo( Left.EffectiveTypeData.IsMobile );
                        if ( val != 0 )
                            return val;

                        //last is turrets, asc (not-turrets first)
                        val = Left.EffectiveTypeData.IsTurret.CompareTo( Right.EffectiveTypeData.IsTurret );
                        if ( val != 0 )
                            return val;

                        //then elites
                        val = Right.EffectiveTypeData.IsElite.CompareTo( Left.EffectiveTypeData.IsElite );
                        if ( val != 0 )
                            return val;

                        //then large ships
                        val = Right.EffectiveTypeData.IsLargeShip.CompareTo( Left.EffectiveTypeData.IsLargeShip );
                        if ( val != 0 )
                            return val;

                        //last is strikecraft, asc (not-strikecraft first)
                        val = Left.EffectiveTypeData.IsStrikecraft.CompareTo( Right.EffectiveTypeData.IsStrikecraft );
                        if ( val != 0 )
                            return val;

                        ////sort by strength, strongest to weakest?
                        //val = Right.HighestMarkLevelData.StrengthPerSquad_CalculatedWithNullFleetMembership.CompareTo( Left.HighestMarkLevelData.StrengthPerSquad_CalculatedWithNullFleetMembership );
                        //if ( val != 0 )
                        //    return val;

                        //then sort by name
                        val = Left.EffectiveTypeData.DisplayNameForSidebar.CompareTo( Right.EffectiveTypeData.DisplayNameForSidebar );
                        if ( val != 0 )
                            return val;

                        //then sort by status, desc
                        val = Right.IconStatus.CompareTo( Left.IconStatus );
                        if ( val != 0 )
                            return val;

                        //finally sort by row index, just to make sure it's absolute unique even if things have the same name
                        val = Left.EffectiveTypeData.RowIndexNonSim.CompareTo( Right.EffectiveTypeData.RowIndexNonSim );
                        if ( val != 0 )
                            return val;

                        //wait, we still did not find it?  Sort by highes mark level
                        val = Left.HighestMarkLevel.CompareTo( Right.HighestMarkLevel );
                        if ( val != 0 )
                            return val;

                        //wait, we still did not find it?  Sort by unit counts
                        val = Right.ActualEntities.Count.CompareTo( Left.ActualEntities.Count );
                        if ( val != 0 )
                            return val;
                        return 0;
                    } );
                    
                    // Now that they are sorted, find consecutive items with the same TypeData
                    // ... and mark only the first to show Dps
                    /*
                    if (Window_InGameSidebarShips.ShowDpsMode != DpsHud.Mode.Off)
                    {
                        GameEntityTypeData lastType = null;
                        foreach (var item in this.inUseList_Text)
                        {
                            if (item.IconStatus == ShipIconStatus.InGuardPost)
                            {
                                lastType = null;
                                continue;
                            }

                            if (item.EffectiveTypeData == lastType)
                                item.ShowDps = false;
                            else
                                item.ShowDps = true;

                            lastType = item.EffectiveTypeData;
                        }
                    }
                    */
                }
            }
        }
        #endregion

        public enum PlanetSidebarFillType
        {
            ByRelationship,
            ByFaction,
            ByFactionMobileOnly,
            ByFactionStationaryOnly,
        }

        public enum PlanetSidebarHeaderPurpose
        {
            Invisible = 0,
            ShipGroup_Normal,
            ShipGroup_CloakedShips,
            LocalFleetList,
            WatchedFleetList,
            Options,
            NeverHadVisionHere,
            StaleIntelHere,
            Option_ShipsGroupBy,
            Option_ShowLocalFleets,
            Option_ShowWatchedFleets,
            Option_ShowAlliedFleets,
            Option_ShowShipsAsText,
            Option_ShowDps
        }

        public class ShipSidebarGroup
        {
            public string HeaderText = string.Empty;
            public string LaterTextStartOfSentence = string.Empty;
            public string LaterTextMidSentence = string.Empty;
            public bool GiveVisionWarningText = true;
            public bool ShowStrengthSummary = true;
            public OpenCloseMenuItem Opener = null;
            public bool IsVisible = false;

            public int Strength_Total = 0;
            public int Strength_Cloaked = 0;
            public int Squads_Total = 0;
            public int Squads_Cloaked = 0;
            public int Noncombatants_Total = 0;
            public int Noncombatants_Cloaked = 0;
            public bool IsYou = false;
            public bool IsAllied = false;
            public bool IsEnemy = false;

            public bShipIconPool shipIconPool;

            public ShipSidebarGroup()
            {
                shipIconPool = new bShipIconPool( 1 );
            }

            public void Clear()
            {
                this.HeaderText = string.Empty;
                this.LaterTextStartOfSentence = string.Empty;
                this.LaterTextMidSentence = string.Empty;
                this.GiveVisionWarningText = true;
                this.ShowStrengthSummary = true;
                this.IsVisible = false;
                this.Strength_Total = 0;
                this.Strength_Cloaked = 0;
                this.Squads_Total = 0;
                this.Squads_Cloaked = 0;
                this.Noncombatants_Total = 0;
                this.Noncombatants_Cloaked = 0;
                this.IsYou = false;
                this.IsAllied = false;
                this.IsEnemy = false;
                this.Opener = null;

                this.shipIconPool.ClearSingle();
            }
        }

        #region Option - ShipsGroupBy
        public enum ShipsGroupBy
        {
            ByRelationship = 0,
            ByFaction,
            ByFactionWithSplitForMobileAndStationary,
            Length
        }

        private static ShipsGroupBy lastShipsGroupBy = ShipsGroupBy.Length;
        public static ShipsGroupBy GetShipsGroupBy()
        {
            if ( lastShipsGroupBy < ShipsGroupBy.Length )
                return lastShipsGroupBy;
            lastShipsGroupBy = (ShipsGroupBy)GameSettings.Current.GetIntBySetting( "PlanetSidebar_Option_ShipsGroupBy" );
            return lastShipsGroupBy;
        }

        public static void IncrementShipsGroupBy()
        {
            if ( lastShipsGroupBy >= ShipsGroupBy.Length )
                lastShipsGroupBy = (ShipsGroupBy)GameSettings.Current.GetIntBySetting( "PlanetSidebar_Option_ShipsGroupBy" );
            lastShipsGroupBy++;
            if ( lastShipsGroupBy >= ShipsGroupBy.Length )
                lastShipsGroupBy = (ShipsGroupBy)0;
            GameSettings.Current.SetIntBySetting( "PlanetSidebar_Option_ShipsGroupBy", (int)lastShipsGroupBy );
        }
        #endregion

        #region Option - ShowLocalFleets
        public enum ShowLocalFleets
        {
            BelowShipList = 0,
            AboveShipList,
            NotShown,
            Length
        }

        private static ShowLocalFleets lastShowLocalFleets = ShowLocalFleets.Length;
        public static ShowLocalFleets GetShowLocalFleets()
        {
            if ( lastShowLocalFleets < ShowLocalFleets.Length )
                return lastShowLocalFleets;
            lastShowLocalFleets = (ShowLocalFleets)GameSettings.Current.GetIntBySetting( "PlanetSidebar_Option_ShowLocalFleets" );
            return lastShowLocalFleets;
        }

        public static void IncrementShowLocalFleets()
        {
            if ( lastShowLocalFleets >= ShowLocalFleets.Length )
                lastShowLocalFleets = (ShowLocalFleets)GameSettings.Current.GetIntBySetting( "PlanetSidebar_Option_ShowLocalFleets" );
            lastShowLocalFleets++;
            if ( lastShowLocalFleets >= ShowLocalFleets.Length )
                lastShowLocalFleets = (ShowLocalFleets)0;
            GameSettings.Current.SetIntBySetting( "PlanetSidebar_Option_ShowLocalFleets", (int)lastShowLocalFleets );
        }

        #endregion
        
        #region Option - ShowWatchedFleets
        public enum ShowWatchedFleets
        {
            BelowShipList = 0,
            AboveShipList,
            NotShown,
            Length
        }

        private static ShowWatchedFleets lastShowWatchedFleets = ShowWatchedFleets.Length;
        public static ShowWatchedFleets GetShowWatchedFleets()
        {
            if ( lastShowWatchedFleets < ShowWatchedFleets.Length )
                return lastShowWatchedFleets;
            lastShowWatchedFleets = (ShowWatchedFleets)GameSettings.Current.GetIntBySetting( "PlanetSidebar_Option_ShowWatchedFleets" );
            return lastShowWatchedFleets;
        }

        public static void IncrementShowWatchedFleets()
        {
            if ( lastShowWatchedFleets >= ShowWatchedFleets.Length )
                lastShowWatchedFleets = (ShowWatchedFleets)GameSettings.Current.GetIntBySetting( "PlanetSidebar_Option_ShowWatchedFleets" );
            lastShowWatchedFleets++;
            if ( lastShowWatchedFleets >= ShowWatchedFleets.Length )
                lastShowWatchedFleets = (ShowWatchedFleets)0;
            GameSettings.Current.SetIntBySetting( "PlanetSidebar_Option_ShowWatchedFleets", (int)lastShowWatchedFleets );
        }
        #endregion

        #region Option - ShowShipsAsText
        public enum ShowShipsAsText
        {
            AsText = 0,
            AsIcon,
            Length
        }

        private static ShowShipsAsText lastShowShipsAsText = ShowShipsAsText.Length;
        public static ShowShipsAsText GetShowShipsAsText()
        {
            if ( lastShowShipsAsText < ShowShipsAsText.Length )
                return lastShowShipsAsText;
            lastShowShipsAsText = (ShowShipsAsText)GameSettings.Current.GetIntBySetting( "PlanetSidebar_Option_ShowShipsAsText" );
            return lastShowShipsAsText;
        }

        public static void IncrementShowShipsAsText()
        {
            if ( lastShowShipsAsText >= ShowShipsAsText.Length )
                lastShowShipsAsText = (ShowShipsAsText)GameSettings.Current.GetIntBySetting( "PlanetSidebar_Option_ShowShipsAsText" );
            lastShowShipsAsText++;
            if ( lastShowShipsAsText >= ShowShipsAsText.Length )
                lastShowShipsAsText = (ShowShipsAsText)0;
            GameSettings.Current.SetIntBySetting( "PlanetSidebar_Option_ShowShipsAsText", (int)lastShowShipsAsText );
        }
        #endregion

        #region Option - ShowAlliedFleets
        private static bool ShowAlliedFleets = false;
        private static bool hasUpdatedAlliedFleets = false;
        public static bool GetShowAlliedFleets()
        {
            if ( !hasUpdatedAlliedFleets )
            {
                ShowAlliedFleets = GameSettings.Current.GetBoolBySetting( "PlanetSidebar_Option_ShowAlliedFleets" );
                hasUpdatedAlliedFleets = true;
            }
            return ShowAlliedFleets;
        }
        public static void UpdateShowAlliedFleets()
        {
            ShowAlliedFleets = ! GameSettings.Current.GetBoolBySetting( "PlanetSidebar_Option_ShowAlliedFleets" );
            GameSettings.Current.SetBoolBySetting( "PlanetSidebar_Option_ShowAlliedFleets", ShowAlliedFleets );
        }
        #endregion

        #region Option - ShowDps

        private static bool dpsHudExists = false;
        private static DpsHud.Mode ShowDpsMode = DpsHud.Mode.Off;
        private static bool hasUpdatedShowDpsMode = false;
        public static DpsHud.Mode GetShowDps()
        {
            // we aren't caching this because the mod can be turned on post startup
            dpsHudExists = GameSettings.Current.GetBoolBySetting( "Mod_Enab_DpsHud" );

            if (!dpsHudExists)
                return DpsHud.Mode.Off;

            if ( !hasUpdatedShowDpsMode )
            {
                ShowDpsMode = (DpsHud.Mode)GameSettings.Current.GetIntBySetting( "PlanetSidebar_Option_ShowDpsMode" );
                hasUpdatedShowDpsMode = true;
            }

            return ShowDpsMode;
        }
        public static void UpdateShowDps(bool cycle)
        {
            var cur = (DpsHud.Mode)GameSettings.Current.GetIntBySetting( "PlanetSidebar_Option_ShowDpsMode" );

            if ( cycle )
            {
                if ( cur == DpsHud.Mode.Off )
                    cur = DpsHud.Mode.Damage;
                else if ( cur == DpsHud.Mode.Damage )
                    cur = DpsHud.Mode.MetalLoss;
                else
                    cur = DpsHud.Mode.Off;
            }
            // toggle between metal/damage
            else
            { 
                if ( cur == DpsHud.Mode.Damage )
                    cur = DpsHud.Mode.MetalLoss;
                else if ( cur == DpsHud.Mode.MetalLoss )
                    cur = DpsHud.Mode.Damage;
            }


            ShowDpsMode = cur;
            GameSettings.Current.SetIntBySetting( "PlanetSidebar_Option_ShowDpsMode", (int)ShowDpsMode );
        }

        #endregion        
    }

    #region Damage Display Helpers

    public static class ArcenBufferExtensions
    {
        public static readonly string Color_Damage_Gratuitous = "<color=#39FF14>"; // neon green 
        public static readonly string Color_Damage_Obscene = "<color=#39FF14>"; //
        public static readonly string Color_Damage_Extreme = "<color=#ffc60a>"; // bright gold
        public static readonly string Color_Damage_High = "<color=#ED12DF>"; // fushia-ish
        public static readonly string Color_Damage_Mid = "<color=#F44336>"; // red-ish
        public static readonly string Color_Damage_Low = "<color=#808080>"; // gray-ish

        // we have 4 characters only, to show any number
        //
        // 0-999
        // 1k-999k
        // 1m - 999m
        // 10m - 999m
        //

        // 1k   - 99k   shown as 1.0k - 99.0k
        // 100k - 999k  shown as 100k - 999k
        // 1m   - 10m   shown as 1.0m - 10.0m
        // 11m  - 99m   shown as 11.0m - 99.0m
        // 100m - 999m  shown as 100m - 999m
        // 1b   - 99b   shown as 1.0b - 99.0b
        // 100b - ?     shown as 100b

        // 51m - 999m shown as 51 m - 999 m
        public static ArcenCharacterBufferBase AddDamageNumbers( this ArcenCharacterBufferBase Buffer, int number, bool nocolor = false )
        {
            UInt64 u = (UInt64)number;
            double f = 0.0f;
            string col = Color_Damage_Low;
            int p = 0;
            string suf = "";

            // less than 1000 dont even show?
            if ( u < 1000 ) 
            {
                col = Color_Damage_Low;
                f = u;
                p = 0;
                suf = " ";
                return Buffer;
            }
            // K range; less than 10k
            else if ( u < 10000 )
            {
                col = Color_Damage_Low;
                f = u / 1000.0;
                p = 0;
                suf = "k";
            }
            // K range; less than 100k
            else if ( u < 100000 )
            {
                col = Color_Damage_Low;
                f = u / 1000.0;
                p = 0;
                suf = "k";
            }
            // K range; less than 1m
            else if ( u < 1000000 )
            {
                col = Color_Damage_Mid;
                f = u / 1000.0;
                p = 0;
                suf = "k";
            }
            // M range; less than 10m
            else if ( u < 10000000 )
            {
                col = Color_Damage_High;
                f = u / 1000000.0;
                p = 1;
                suf = "m";
            }
            // M range; less than 1b
            else if ( u < 1000000000 )
            {
                col = Color_Damage_Gratuitous;
                f = u / 1000000.0;
                p = 0;
                suf = "m";
            }
            // B range; less than 10b
            else if ( u < 10000000000 )
            {
                col = Color_Damage_Gratuitous;
                f = u / 1000000000.0;
                p = 1;
                suf = "B";
            }
            else // numbers over 10b
            {
                col = Color_Damage_Gratuitous;
                f = u / 1000000000.0;
                p = 0;
                suf = "B";
            }

            Buffer.Add( col ).Add(Math.Round(f,p)).Add("<sub>").Add(suf).Add("</sub>").Add("</color>");

            return Buffer;
        }
        
        public static ArcenCharacterBufferBase AddStrengthNumbers( this ArcenCharacterBufferBase Buffer, int number )
        {
            UInt64 u = (UInt64)number;
            double f = 0.0f;
            string col = Color_Damage_Low;
            int p = 0;
            string suf = "";

            // less than 1000; just show ~1
            if ( u < 1000 ) 
            {
                return Buffer.Add("~1");
            }
            
            // K range; less than 10k
            else if ( u < 10000 )
            {
                col = Color_Damage_Low;
                f = u / 1000.0;
                p = 0;
                suf = "";
            }
            // K range; less than 100k
            else if ( u < 100000 )
            {
                col = Color_Damage_Low;
                f = u / 1000.0;
                p = 0;
                suf = "";
            }
            // K range; less than 1m
            else if ( u < 1000000 )
            {
                col = Color_Damage_Mid;
                f = u / 1000.0;
                p = 0;
                suf = "";
            }
            // M range; less than 10m
            else if ( u < 10000000 )
            {
                col = Color_Damage_High;
                f = u / 1000000.0;
                p = 1;
                suf = "k";
            }
            // M range; less than 1b
            else if ( u < 1000000000 )
            {
                col = Color_Damage_Gratuitous;
                f = u / 1000000.0;
                p = 0;
                suf = "k";
            }
            // B range; less than 10b
            else if ( u < 10000000000 )
            {
                col = Color_Damage_Gratuitous;
                f = u / 1000000000.0;
                p = 1;
                suf = "m";
            }
            else // numbers over 10b
            {
                col = Color_Damage_Gratuitous;
                f = u / 1000000000.0;
                p = 0;
                suf = "m";
            }

            Buffer
                //.Add( col )
                .Add(Math.Round(f, p))
                .Add("<sub>").Add(suf).Add("</sub>")
                //.Add("</color>")
                ;

            return Buffer;
        }
    }

    #endregion
}
