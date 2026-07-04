using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Diagnostics;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace Arcen.AIW2.External
{
    public class Window_InGameSidebarDirectBuild : Window_InGameSidebarBase
    {
        public static Window_InGameSidebarDirectBuild Instance;
        public Window_InGameSidebarDirectBuild()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return Current == InGameSidebarType.DirectBuild;
        }
        
        private static PoolableGUIGroup.GroupPool<DirectPlacementCategory> DirectPlacementCategoryPool;
        
        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_InGameSidebarDirectBuild.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.TimeSinceStartF;
            }

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                AdjustHeightToScreenMax( 60, "NotificationsScale", "SidebarScale", "ResourceBarScale", Window_InGameSidebarShips.CustomParentInstance,
                    Window_InGameSidebarFleets.CustomParentInstance, Window_InGameSidebarDirectBuild.CustomParentInstance,
                    Window_InGameSidebarScience.CustomParentInstance, Window_InGameSidebarHacking.CustomParentInstance,
                    Window_InGameSidebarOutguard.CustomParentInstance, Window_InGameSidebarObjectives.CustomParentInstance,
                    Window_InGameSidebarJournal.CustomParentInstance, Window_InGameSidebarTips.CustomParentInstance );

                if ( Window_InGameSidebarDirectBuild.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( btnDirectPlacementCategory.Original != null )
                        {
                            hasGlobalInitialized = true;
                            DirectPlacementCategoryPool = new PoolableGUIGroup.GroupPool<DirectPlacementCategory>( new DirectPlacementCategory( btnDirectPlacementCategory.Original ), 6 );
                        }
                    }
                    #endregion
                }

                float currentY = 0; //the position of the first entry

                this.OnUpdateDirectPlacement( ref currentY );

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)btnDirectPlacementCategory.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = Mat.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            public const float TEXT_ROW_HEIGHTS_UPPER = 25f;
            public const float TEXT_ROW_HEIGHTS_LOWER = 26.04f;
            public const float TEXT_ROW_HEIGHTS_LOWER_ITEMS = 14.7f;
            public const float ROW_ADVANCE_WHEN_CLOSED = 5f;

            #region OnUpdateDirectPlacement
            public void OnUpdateDirectPlacement( ref float currentY )
            {
                int debugStage = 0;
                try
                {
                    bool debug = false;
                    debugStage = 1000;
                    
                    int remainingSubItemsCanAdd = 10;

                    Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                    if ( localFaction == null )
                        return;
                    PlayerTypeData playerTypeOrNull = localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;

                    Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                    if ( planet == null )
                        return;

                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("Start of OnUpdateDirectPlacement", Verbosity.DoNotShow );
                    
                    debugStage = 1100;
                    DirectPlacementCategoryPool.Clear( 5 );
                    
                    debugStage = 2000;
                    BuildSidebarCategoryTable.ClearAllNonSim_BuildSidebarWorking();

                    #region The Normal Build Menu Logic
                    foreach ( GameEntity_Squad builder in planet.Squads( EntityRollupType.BuildMenuPopulator ) )
                    {
                        // ArcenDebugging.ArcenDebugLogSingleLine( builder.TypeData.InternalName + " " + builder.TypeData.SpecialType + " " +
                        //     builder..MemberGroups.Count + " " + builder.GetFleetID_Safe() + " " + builder..Category, Verbosity.DoNotShow );

                        debugStage = 2100;
                        //either must be my faction or a third party who will sell to me, either way
                        if ( builder.PlanetFaction.Faction != localFaction && builder.TypeData.SpecialType != SpecialEntityType.ThirdPartySellerToPlayers )
                            continue;
                        debugStage = 2200; 
                        if ( builder.SecondsSpentAsRemains > 0 || builder.SelfBuildingMetalRemaining > FInt.Zero || builder.GetIsCrippled() || builder.GetIsNonFunctional() )
                            continue; //don't include DirectBuilders that are under Construction or remains

                        builder.FlagForRequestedForcedFullSyncToAllClients_FromAnyClient(); //request frequent updates for whatever planet we are on

                        debugStage = 2300;
                        FleetMembership builderMem = builder.FleetMembership;
                        if ( builderMem == null )
                            continue;
                        debugStage = 2310;
                        Fleet builderFleet = builderMem.Fleet;
                        if ( builderFleet == null )
                            continue;
                        debugStage = 2320;

                        if ( builder.TypeData.SpecialType == SpecialEntityType.CityCenter ) {
                            debugStage = 2330;
                            int totalCityPoints = builderFleet.CalculateTotalCitySockets();
                            if ( totalCityPoints > 0 )
                            {
                                debugStage = 2340;
                                int spentCityPoints = builderFleet.CalculateSpentCitySockets();
                                DirectPlacementCategory directPlacementCat = DirectPlacementCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                                if ( directPlacementCat != null ) {
                                    debugStage = 2350;
                                    directPlacementCat.Category = null;
                                    directPlacementCat.Button.OverridingTextToShow = delegate ( ArcenCharacterBufferBase buffer )  {
                                        buffer.Add( "<pos=0><size=80%>" );
                                        buffer.Add( builderFleet.GetName() );
                                        buffer.Add( "</pos></size><line-height=0>\n<size=60%><align=\"right\">" );
                                        //TODO: update the colour if all sockets are filled, then update the socket cost to say "Can't buidl this, no available sockets"
                                        debugStage = 2360;
                                        if ( spentCityPoints == totalCityPoints )
                                            buffer.StartColor(Color.red);
                                        buffer.Add( builder.TypeData.NameForCitySockets_Short_Plural ).Add( " 已用：" );
                                        buffer.Add( spentCityPoints ).Add( "/" ).Add( totalCityPoints );
                                        if ( spentCityPoints == totalCityPoints )
                                            buffer.EndColor();

                                        buffer.Add( "</align></size>" );
                                        buffer.EndSize();
                                    };
                                }
                            }
                        }
                        debugStage = 2400;
                        foreach ( FleetMembership mem in builderFleet.MemberGroupsSorted_NonSim_ForUIOnly_SeriouslyDoNotCallFromBGCode() )
                        {
                            debugStage = 3000;
                            if ( mem.ForMark == null )
                                continue;
                            debugStage = 3100;
                            //sometimes things are in our squad but we're not allowed to build them explicitly.  These should not show here!
                            if ( mem.EffectiveSquadCap <= 0 )
                                continue;
                            debugStage = 3200;
                            //if this is not a self-building entity, then skip it
                            if (!mem.TypeData.SelfConstructs) {
                                continue;
                            }
                            debugStage = 3300;
                            //be sure to skip any non-command-station fleet leaders (centerpieces)
                            //if ( mem.TypeData.IsFleetLeader && !mem.TypeData.IsCommandStation )
                            //    continue;

                            if ( mem.TypeData.BuildSidebarCategoriesIAmPartOf.Count <= 0 )
                            { }//   BuildSidebarCategoryTable.Uncategorized.NonSim_BuildSidebarWorking.Add( mem );
                            else
                            {
                                debugStage = 3500;
                                DirectBuildable buildable = DirectBuild_FleetMembership.Create( mem, localFaction );
                                if ( !buildable.GetIsNull() ) {
                                    for ( int i = 0; i < mem.TypeData.BuildSidebarCategoriesIAmPartOf.Count; i++ )
                                    {
                                        debugStage = 3600;
                                        BuildSidebarCategory cat = mem.TypeData.BuildSidebarCategoriesIAmPartOf[i];
                                        if ( cat.GrantedByAnyMobileFleet || cat.GrantedByAnyCommandStation) {
                                            // We use differerent logic for choosing when to display these.
                                            continue;
                                        }
                                        if ( cat.HideEntriesWhereCannotBuildAnother && mem.GetCanBuildAnother( false, -1, ExtraFromStacks.IncludePrecalc ) != ArcenRejectionReason.Unknown )
                                            continue;
                                        if ( debug )
                                            ArcenDebugging.ArcenDebugLogSingleLine("For " + mem.TypeData.GetDisplayName() + " we have a category, " + cat.ToString(), Verbosity.DoNotShow );
                                        cat.NonSim_BuildSidebarWorking.Add( buildable );
                                    }
                                }
                            }
                            debugStage = 4000;
                        }
                    }
                    #endregion

                    debugStage = 7000;
                    BuildSidebarCategory sidebarCat;
                    #region The Granted-By-Any-Whatever Build Menu Logic
                    //add in global stuff that isn't baked into ships that would grant categories
                    //this is mostly for command stations and command station upgrades
                    for ( int i = 0; i < BuildSidebarCategoryTable.Instance.SortedCategories.Count; i++ )
                    {
                        debugStage = 7100;
                        sidebarCat = BuildSidebarCategoryTable.Instance.SortedCategories[i];
                        debugStage = 7200;
                        if ( sidebarCat.NonSim_BuildSidebarWorking.Count > 0 )
                            continue; //if already had any types granted, then skip it

                        debugStage = 8000;
                        bool added = false;
                        if ( sidebarCat.GrantedByAnyMobileFleet )
                        {
                            debugStage = 8100;
                            foreach ( GameEntity_Squad flagship in planet.Squads( EntityRollupType.MobileFleetFlagships ) )
                            {
                                debugStage = 8200;
                                if ( flagship.PlanetFaction.Faction != localFaction )
                                    continue;
                                debugStage = 8300;
                                if ( flagship.SecondsSpentAsRemains > 0 || flagship.SelfBuildingMetalRemaining > FInt.Zero || flagship.GetIsCrippled() || flagship.GetIsNonFunctional() )
                                    continue; //don't include mobile fleet flagships that are under Construction or remains or crippled

                                debugStage = 8400;
                                GameEntityTypeData typeDataForPurchase;
                                for ( int j = 0; j < sidebarCat.Items.Count; j++ )
                                {
                                    debugStage = 8500;
                                    typeDataForPurchase = sidebarCat.Items[j];
                                    debugStage = 8600;
                                    DirectBuildable buildable = sidebarCat.CreateDirectBuildable(planet, localFaction, typeDataForPurchase);
                                    debugStage = 8650;

                                    debugStage = 8800;
                                    if ( !buildable.GetIsNull() ) {
                                        sidebarCat.NonSim_BuildSidebarWorking.Add( buildable );
                                    }
                                }

                                added=true;

                                //once we found at least a single mobile fleet flagship, then stop looking for more!
                                break;
                            }
                        }

                        debugStage = 11000;
                        if ( !added && sidebarCat.GrantedByAnyCommandStation )
                        {
                            debugStage = 11100;
                            foreach ( GameEntity_Squad commandStation in planet.Squads( EntityRollupType.CommandStation ) )
                            {
                                debugStage = 11200;
                                if ( commandStation.PlanetFaction.Faction != localFaction )
                                    continue;
                                debugStage = 11300;
                                if ( commandStation.SecondsSpentAsRemains > 0 || commandStation.SelfBuildingMetalRemaining > FInt.Zero || commandStation.GetIsCrippled() || commandStation.GetIsNonFunctional() )
                                    continue; //don't include mobile fleet flagships that are under Construction or remains or crippled

                                debugStage = 11400;
                                GameEntityTypeData typeDataForPurchase;
                                for ( int j = 0; j < sidebarCat.Items.Count; j++ )
                                {
                                    typeDataForPurchase = sidebarCat.Items[j];
                                    debugStage = 11500;
                                    //ArcenDebugging.ArcenDebugLogSingleLine("processing category " + sidebarCat.ToString() + " and ship type " + typeDataForPurchase.GetDisplayName() + " granted by " + sidebarCat.GrantedByAnyCommandStationForPlayerType , Verbosity.DoNotShow );
                                    DirectBuildable buildable = sidebarCat.CreateDirectBuildable(planet, localFaction, typeDataForPurchase);
                                    debugStage = 11550;
                                    if ( !buildable.GetIsNull() ) {
                                        sidebarCat.NonSim_BuildSidebarWorking.Add( buildable );
                                    }
                                }
                                //once we found at least a single command station, then stop looking for more!
                                break;
                            }
                        }
                    }
                    #endregion The Granted-By-Any-Whatever Build Menu Logic

                    debugStage = 13000;
                    BuildSidebarCategoryTable.SortAllNonSim_BuildSidebarWorking();

                    debugStage = 13100;
                    PlanetFaction controllingPFaction = planet.GetControllingPlanetFaction();
                    if ( controllingPFaction == null )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "Null controllingPFaction for this planet!", Verbosity.ShowAsError );
                        return;
                    }
                    if ( controllingPFaction.Faction == null )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "Null Faction for this controllingPFaction: " + controllingPFaction.FactionIndex, Verbosity.ShowAsError );
                        return;
                    }
                    debugStage = 14000;
                    for ( int i = 0; i < BuildSidebarCategoryTable.Instance.SortedCategories.Count; i++ )
                    {
                        debugStage = 14100;
                        sidebarCat = BuildSidebarCategoryTable.Instance.SortedCategories[i];
                        debugStage = 14200;
                        //if it's an empty category, then just skip it
                        if ( sidebarCat.NonSim_BuildSidebarWorking.Count <= 0 )
                            continue;
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine("Category " + sidebarCat.GetDisplayName() + " has " + sidebarCat.NonSim_BuildSidebarWorking.Count + " entries", Verbosity.DoNotShow );
                        debugStage = 14300;
                        if ( !sidebarCat.GetShouldShow(planet, localFaction) ) {
                            continue;
                        }

                        debugStage = 14400;
                        //if it's supposed to only be non-home planets of the player, then check all that
                        if ( sidebarCat.OnlyShowsWhenNotAPlayerHomePlanet )
                        {
                            if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld )
                            {
                                debugStage = 14500;
                                GameEntity_Squad commandStationSquad = planet.GetCommandStationOrNull();
                                //if the player lost their home planet and then recaptured it, then allow this to work as if it's not a human homeworld anymore
                                if ( commandStationSquad != null && commandStationSquad.TypeData.SpecialType == SpecialEntityType.HumanHomeCommand )
                                    continue;
                            }
                        }
                        debugStage = 14600;
                        //if it must be the local player owning this, then check THAT
                        if ( sidebarCat.OnlyShowsWhenPlanetOwnedByLocalPlayer )
                        {
                            if ( controllingPFaction.Faction != localFaction )
                                continue;
                        }
                        debugStage = 14700;
                        //I guess we have something to draw, so let's do that!
                        DirectPlacementCategory directPlacementCat = DirectPlacementCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( directPlacementCat == null )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine("Category " + sidebarCat.GetDisplayName() + " just got timesliced!", Verbosity.DoNotShow );
                            
                            break; //time slicing, too many added right now
                        }
                        directPlacementCat.Category = sidebarCat;
                        directPlacementCat.OptionsInCategory.Clear();
                        directPlacementCat.OptionsInCategory.AddRange( sidebarCat.NonSim_BuildSidebarWorking );
                    }

                    debugStage = 16000;
                    List<DirectPlacementCategory> cats = DirectPlacementCategoryPool.GetInUseList();
                    debugStage = 16100;
                    for ( int i = 0; i < cats.Count; i++ )
                        cats[i].Update( ref currentY, ref remainingSubItemsCanAdd );
                    
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine("End of OnUpdateDirectPlacement", Verbosity.DoNotShow );
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "BuildSidebar.OnUpdateDirectPlacement error at debugStage " + debugStage + ": " + e, Verbosity.ShowAsError );
                }
            }
            #endregion
        }

        #region btnDirectPlacementCategory
        public class btnDirectPlacementCategory : ButtonAbstractBase
        {
            public static btnDirectPlacementCategory Original;
            public btnDirectPlacementCategory() { if ( Original == null ) Original = this; }

            public ToDisplayString OverridingTextToShow;
            public DirectPlacementCategory ParentCategory;
            private string lastDisplayName = string.Empty;

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.OverridingTextToShow != null ) {
                    return MouseHandlingResult.None;
                }
                if ( this.ParentCategory != null )
                    this.ParentCategory.ThisCategoryIsOpen = !this.ParentCategory.ThisCategoryIsOpen;
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( this.OverridingTextToShow != null ) {
                    Buffer.Add( this.OverridingTextToShow );
                    return;
                }
                if ( this.ParentCategory == null || this.ParentCategory.ThisCategoryIsOpen )
                    Buffer.Add( "(-) " );
                else
                    Buffer.Add( "(+) " );

                Buffer.Add( this.ParentCategory == null || this.ParentCategory.Category == null ? lastDisplayName : lastDisplayName = this.ParentCategory.Category.DisplayName )
                    .Add( "   x" );
                if ( this.ParentCategory == null )
                    Buffer.Add( "??" );
                else
                    Buffer.Add( this.ParentCategory.OptionsInCategory.Count );
            }
            public override void HandleMouseover()
            {
                if ( this.OverridingTextToShow != null ) {
                    return;
                }
                if ( this.ParentCategory == null || this.ParentCategory.Category == null )
                    return;
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( this.ParentCategory.Category.Description, "GeneralTooltipScale" );
            }
            public override bool GetShouldBeHidden()
            {
                if ( this.OverridingTextToShow != null ) {
                    return false;
                }
                return this.ParentCategory == null || this.ParentCategory.Category == null;
            }
        }
        #endregion

        #region btnDirectPlacement
        public class btnDirectPlacement : ImageButtonAbstractBase
        {
            public static btnDirectPlacement Original;
            public btnDirectPlacement() { if ( Original == null ) Original = this; }

            public DirectBuildable DirectBuildable;
            public DirectPlacementCategory ParentCategory;

            public ArcenUIWrapperedUnityImage ShipIcon;
            public ArcenUIWrapperedUnityImage ShipIconTrim;
            public ArcenUIWrapperedUnityImage ShipIconOverlay;

            private bool hasInitialized = false;
            public void InitIfNeeded( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                if ( hasInitialized )
                    return;
                hasInitialized = true;

                ShipIcon = SubImages[0].WrapperedImage;
                ShipIconTrim = SubImages[1].WrapperedImage;
                ShipIconOverlay = SubImages[2].WrapperedImage;
            }

            public override void Clear()
            {
                this.DirectBuildable = DirectBuildable.CreateBlank();
                this.ParentCategory = null;
            }

            public bool DebugUpdateContentFromVolatile = false;
            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                if ( this.DirectBuildable.GetIsNull() )
                    return;

                int debugStage = 0;

                InitIfNeeded( Image, SubImages, SubTexts );
                RenderIcon();

                debugStage = 1;
                try
                {
                    debugStage = 5;

                    debugStage = 14;
                    Faction localPlayerFactionOrNull = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                    PlayerTypeData playerTypeOrNull = localPlayerFactionOrNull == null ? null : localPlayerFactionOrNull.PlayerTypeDataOrNull_ModeratelyExpensive;
                    debugStage = 15;
                    int fullCap = this.DirectBuildable.BuildCap;
                    if ( fullCap < 0 )
                        fullCap = 0;
                    int builtCount = 0;
                    builtCount = this.DirectBuildable.CurrentlyBuilt;
                    if ( builtCount < 0 )
                        builtCount = 0;

                    int galaxyCap = this.DirectBuildable.TypeData.CalculateEffectiveGalaxyWideCapForPlayersConstructing(localPlayerFactionOrNull);
                    int galaxyBuiltCount = -1; // Use -1 as default so < galaxyCap when galaxyCap == 0
                    if (galaxyCap > 0) {
                        galaxyBuiltCount = 0;
                        if ( localPlayerFactionOrNull != null )
                        {
                            foreach ( GameEntity_Squad squad in localPlayerFactionOrNull.Squads( EntityRollupType.HasGalaxyWideCapForPlayersConstructing ) )
                            {
                                if ( squad.TypeData.GalaxyWideCapMatchString == this.DirectBuildable.TypeData.GalaxyWideCapMatchString )
                                    galaxyBuiltCount++;
                            }
                        }
                    };

                    bool availableSockets = true;
                    int availableSocketCount = 0;
                    if ( this.DirectBuildable.Builder != null &&
                         this.DirectBuildable.TypeData.CitySocketCost > 0 )
                    {
                        int totalSockets = this.DirectBuildable.Builder.FleetMembership.Fleet.CalculateTotalCitySockets();
                        int spentSockets = this.DirectBuildable.Builder.FleetMembership.Fleet.CalculateSpentCitySockets();
                        availableSocketCount = totalSockets - spentSockets;
                        if ( spentSockets >= totalSockets )
                            availableSockets = false;
                    }

                    bool enoughDistricts = true; //for the dyson sidekick
                    if ( this.DirectBuildable.Builder != null &&
                         this.DirectBuildable.TypeData.GetHasTag("DysonSidekickSphere") )
                    {
                        DysonSidekickFactionBaseInfo BaseInfo = this.DirectBuildable.Builder.PlanetFaction.Faction.GetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
                        if ( BaseInfo != null &&
                             !BaseInfo.HasAdequateDistrictsToBuild(this.DirectBuildable.TypeData) &&
                             !BaseInfo.StartWithAllUpgrades)
                        {
                            enoughDistricts = false;
                        }

                    }

                    bool canBuildAny = (fullCap == 0 || builtCount < fullCap) && (galaxyBuiltCount < galaxyCap) && availableSockets && enoughDistricts;

                    debugStage = 20;

                    //main text - name of the structure, and its mark, colorized
                    SubTexts[0].Text.StartWritingToBuffer().Add( "<size=90%>" ).Add( this.DirectBuildable.ForMark.MarkLevel.MapDisplayWithColor.Length > 0 ? this.DirectBuildable.ForMark.MarkLevel.MapDisplayWithColor :
                            string.Empty ).Add( this.DirectBuildable.ForMark.MarkLevel.MapDisplayWithColor.Length > 0 ? " </color>" : string.Empty ).StartColor( canBuildAny ? Color.white : Color.gray ).Add( this.DirectBuildable.TypeData.DisplayNameForSidebar );
                    debugStage = 21;
                    SubTexts[0].Text.FinishWritingToBuffer();

                    debugStage = 30;
                    //ship cap text
                    Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
                    ArcenDoubleCharacterBuffer capText = SubTexts[1].Text.StartWritingToBuffer();
                    bool didOverrideCap = this.DirectBuildable.AddOverridingCapText(capText);
                    if (didOverrideCap) 
                    {
                        // Nothing to do
                    } 
                    else 
                    {
                        if (fullCap > 0 && (galaxyCap == 0 || fullCap < galaxyCap)) 
                        {
                            Color textColor = Color.white;
                            if ( builtCount < fullCap && availableSockets )
                                textColor = Color.white;
                            else if ( builtCount == fullCap || !availableSockets )
                                textColor = Color.grey;
                            else
                                textColor = Color.red;
                            capText.StartColor( textColor ).Add(
                                    builtCount ).Add( "/" ).Add( fullCap ).EndColor();
                        }

                        if (playerTypeOrNull != null && playerTypeOrNull.UsesMetal && this.DirectBuildable.TypeData.CitySocketCost > 0) 
                        {
                            if ( !availableSockets )
                                capText.StartColor( Color.grey );
                            if ( availableSocketCount < this.DirectBuildable.TypeData.CitySocketCost )
                                capText.StartColor( Color.red );
                            capText.Add(" ").Add(this.DirectBuildable.TypeData.NameForCitySockets_Short_Plural).Add(": ");
                            capText.Add(this.DirectBuildable.TypeData.CitySocketCost);
                            if ( !availableSockets )
                                capText.EndColor();
                        }

                        if (galaxyCap > 0) 
                        {
                            capText.StartColor( galaxyBuiltCount < galaxyCap ? Color.white : ( galaxyBuiltCount == galaxyCap ? Color.gray : Color.red ) );

                            if (fullCap < galaxyCap && fullCap > 0)
                                capText.Add(" (");
                            
                            capText.Add("星系：").Add( galaxyBuiltCount ).Add( "/" ).Add( galaxyCap );

                            if (fullCap < galaxyCap && fullCap > 0)
                                capText.Add(")");
                            
                            capText.EndColor();
                        }
                    }
                    debugStage = 31;
                    SubTexts[1].Text.FinishWritingToBuffer();

                    debugStage = 40;
                    ArcenDoubleCharacterBuffer buffer = SubTexts[2].Text.StartWritingToBuffer();
                    if ( playerTypeOrNull != null )
                    {
                        //ship cost text
                        if (playerTypeOrNull.UsesMetal)
                        {
                            if ( localPlayerFactionOrNull != null && localPlayerFactionOrNull.StoredMetal < this.DirectBuildable.GetMetalCost() )
                                buffer.StartColor( Color.red );
                            else if ( builtCount >= fullCap )
                                buffer.StartColor( Color.gray );
                            else
                                buffer.StartColor( ArcenExternalUIUtilities.Metal.Color );
                            
                            buffer.Add(ArcenExternalUIUtilities.Metal);
                            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, this.DirectBuildable.GetMetalCost(), false, false );
                        }
                        else if (this.DirectBuildable.TypeData.CitySocketCost > 0)
                        {
                            if ( !availableSockets )
                                buffer.StartColor( Color.grey );
                            if ( availableSocketCount < this.DirectBuildable.TypeData.CitySocketCost )
                                buffer.StartColor( Color.red );
                            buffer.Add(" ").Add(this.DirectBuildable.TypeData.NameForCitySockets_Short_Plural).Add(": ");
                            buffer.Add(this.DirectBuildable.TypeData.CitySocketCost);
                            if ( !availableSockets )
                                buffer.EndColor();
                        }
                    }
                    debugStage = 41;
                    if ( this.DirectBuildable.TypeData.CostInResourceOne > 0 )
                    {
                        buffer.Add( " " );
                        ArcenExternalUIUtilities.AddSizeAndVOssetToText( buffer, localPlayerFactionOrNull != null && localPlayerFactionOrNull.Resource1TextColorAndIcon.Length > 0 ? localPlayerFactionOrNull.Resource1TextColorAndIcon : World_AIW2.Instance.Resource1TextColorAndIcon, 7, -1 );
                        if ( localPlayerFactionOrNull != null && localPlayerFactionOrNull.StoredFactionResourceOne < this.DirectBuildable.TypeData.CostInResourceOne )
                            buffer.StartColor( Color.red );
                        else
                            buffer.StartColor( Color.gray );
                        ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, this.DirectBuildable.TypeData.CostInResourceOne, false, false );
                    }
                    if ( this.DirectBuildable.TypeData.CostInResourceTwo > 0 )
                    {
                        buffer.Add( " " );
                        ArcenExternalUIUtilities.AddSizeAndVOssetToText( buffer, World_AIW2.Instance.Resource2TextColorAndIcon, 7, -1 );
                        if ( localPlayerFactionOrNull != null && localPlayerFactionOrNull.StoredFactionResourceTwo < this.DirectBuildable.TypeData.CostInResourceTwo )
                            buffer.StartColor( Color.red );
                        else
                            buffer.StartColor( Color.gray );
                        ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, this.DirectBuildable.TypeData.CostInResourceTwo, false, false );
                    }
                    if ( this.DirectBuildable.TypeData.CostInResourceThree > 0 )
                    {
                        buffer.Add( " " );
                        ArcenExternalUIUtilities.AddSizeAndVOssetToText( buffer, World_AIW2.Instance.Resource3TextColorAndIcon, 7, -1 );
                        if ( localPlayerFactionOrNull != null && localPlayerFactionOrNull.StoredFactionResourceThree < this.DirectBuildable.TypeData.CostInResourceThree )
                            buffer.StartColor( Color.red );
                        else
                            buffer.StartColor( Color.gray );
                        ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, this.DirectBuildable.TypeData.CostInResourceThree, false, false );
                    }
                    debugStage = 42;
                    SubTexts[2].Text.FinishWritingToBuffer();

                    debugStage = 50;
                    //fleet text
                    buffer = SubTexts[3].Text.StartWritingToBuffer();
                    if ( this.DirectBuildable.TypeData.SpecialType == SpecialEntityType.MobileCustomUnattachedFleetFlagship )
                    {
                        buffer.StartColor( "ee9dfa" ).Add( "新建自定义舰队" );
                    }
                    else
                    {
                        this.DirectBuildable.AddFleetDescriptor( buffer );
                    }
                    debugStage = 51;
                    SubTexts[3].Text.FinishWritingToBuffer();
                    debugStage = 20;
                    debugStage = 21;
                }
                catch ( Exception e )
                {
                    if ( DebugUpdateContentFromVolatile )
                        ArcenDebugging.ArcenDebugLog( "Exception in btnDirectPlacementIcon.UpdateContentFromVolatile at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                }
            }
            
            private static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_InGameSidebarDirectBuild-btnDirectPlacement-tooltipBuffer" );
            public override void HandleMouseover()
            {
                World_AIW2.Instance.FocusedEntityTypeDataForMapDarkening = this.DirectBuildable.TypeData;
                if ( this.DirectBuildable.GetIsNull() ) {
                    return;
                }
                // a buildable thing is not an entity
                //GameEntityTypeData.SetCurrentlyHoveredOver();
                this.DirectBuildable.GetHoverText(tooltipBuffer);
                EntityText.Write_Tooltip_Hotkeys_Footer( tooltipBuffer, true, true, null );
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
            }
            public override MouseHandlingResult HandleClick( MouseHandlingInput input )
            {
                if ( this.DirectBuildable.GetIsNull() ) {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "BUG: BuildTypeData is null", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                {
                    World_AIW2.Instance.QueueChatMessageOrCommand( "观察者模式下无法建造。", ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                return this.DirectBuildable.HandleButtonClick(input);
            }

            public override bool GetShouldBeHidden()
            {
                return this.DirectBuildable.GetIsNull() || this.ParentCategory == null || !this.ParentCategory.ThisCategoryIsOpen ||
                    this.ParentCategory.Button == null || this.ParentCategory.Button.GetShouldBeHidden();
            }

            public bool DebugRenderIcon = false;
            public void RenderIcon()
            {
                if ( this.DirectBuildable.GetIsNull() )
                    return;

                int debugStage = -1;
                try
                {
                    debugStage = 0;
                    GameEntityTypeData EffectiveTypeData = this.DirectBuildable.TypeData;
                    Color factionCenterColor = ColorMath.White;
                    Color factionTrimColor = ColorMath.Black;

                    debugStage = 4;

                    Faction localGlobalFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    factionCenterColor = localGlobalFaction.FactionCenterColor.TeamColor;
                    factionTrimColor = localGlobalFaction.FactionTrimColor.TeamColor;

                    debugStage = 10;
                    if ( ShipIcon != null && EffectiveTypeData != null && EffectiveTypeData.GUISprite_Icon != null )
                    {
                        ShipIcon.UpdateWith( EffectiveTypeData.GUISprite_Icon, false );
                        debugStage = 11;
                        ShipIcon.SetColor( factionCenterColor );
                    }

                    debugStage = 15;
                    if ( ShipIconTrim != null && EffectiveTypeData != null && EffectiveTypeData.GUISprite_IconBorder != null )
                    {
                        ShipIconTrim.UpdateWith( EffectiveTypeData.GUISprite_IconBorder, false );
                        debugStage = 16;
                        ShipIconTrim.SetColor( factionTrimColor );
                    }

                    debugStage = 20;

                    if ( ShipIconOverlay != null && EffectiveTypeData != null )
                    {
                        if ( EffectiveTypeData.GUISprite_IconOverlay != null )
                            ShipIconOverlay.UpdateWith( EffectiveTypeData.GUISprite_IconOverlay, false );
                        else
                            ShipIconOverlay.UpdateWith( ExternalConstants.Instance.BlankGUISprite, false );
                        debugStage = 21;
                    }

                    debugStage = 25;
                }
                catch ( Exception e )
                {
                    if ( DebugRenderIcon )
                        ArcenDebugging.ArcenDebugLog( $"Exception in {nameof(btnDirectPlacement)}.{nameof(RenderIcon)} at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                }
            }
        }
        #endregion

        #region DirectPlacementCategory
        public class DirectPlacementCategory : PoolableGUIGroup
        {
            public bool ThisCategoryIsOpen = true;

            public btnDirectPlacementCategory Button;
            public BuildSidebarCategory Category;
            public readonly List<DirectBuildable> OptionsInCategory = List<DirectBuildable>.Create_WillNeverBeGCed( 300, "Window_InGameSidebarDirectBuild-DirectPlacementCategory-OptionsInCategory" );
            
            private ImageButtonAbstractBase.ImageButtonPool<btnDirectPlacement> btnDirectPlacementPool;

            public DirectPlacementCategory( btnDirectPlacementCategory But )
            {
                this.Button = But;
                this.Button.ParentCategory = this;
            }

            public override void PostInit()
            {
                btnDirectPlacementPool = new ImageButtonAbstractBase.ImageButtonPool<btnDirectPlacement>( btnDirectPlacement.Original, 1 );
            }

            public bool DebugUpdate = true;
            public override void Update()
            {
                throw new Exception( "Not really using this update method..." );
            }

            public const float TEXT_ROW_HEIGHTS = 25f;
            public const float ROW_ADVANCE_WHEN_CLOSED = 5f;

            public void Update( ref float currentY, ref int RemainingSubItemsCanAdd )
            {
                int debugStage = 0;
                try
                {
                    debugStage = 10;
                    btnDirectPlacementPool.Clear( RemainingSubItemsCanAdd );
                    if ( this.Category == null && this.Button.OverridingTextToShow == null )
                        return;

                    debugStage = 11;
                    Faction localPlayerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                    debugStage = 12;
                    if ( localPlayerFaction == null )
                        return;
                    
                    DirectBuildable item = DirectBuildable.CreateBlank();
                    if ( RemainingSubItemsCanAdd > 0 )
                    {
                        for ( int j = 0; j < this.OptionsInCategory.Count; j++ )
                        {
                            item = this.OptionsInCategory[j];
                            debugStage = 22;
                            if ( item.GetIsNull() )
                                continue;
                            debugStage = 23;
                            btnDirectPlacement itemButton = btnDirectPlacementPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( itemButton == null )
                                break; //time slicing, too many added right now
                            debugStage = 25;
                            itemButton.DirectBuildable = item;
                            itemButton.ParentCategory = this;
                            debugStage = 26;
                        }
                    }

                    RemainingSubItemsCanAdd = btnDirectPlacementPool.GetRemainingAllowedToAddBeforeNextClear();

                    debugStage = 41;
                }
                catch ( Exception e )
                {
                    if ( DebugUpdate )
                        ArcenDebugging.ArcenDebugLog( "Exception in DirectPlacementCategory.DebugUpdate at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                }

                #region Positioning Logic
                RectTransform rTran = null;
                {
                    rTran = this.Button.Element.RelevantRect;
                    rTran.anchoredPosition = new Vector2( 0, currentY );
                    currentY -= TEXT_ROW_HEIGHTS;

                    if ( this.ThisCategoryIsOpen )
                        btnDirectPlacementPool.ApplyItemsInRows( 0, ref currentY, 32f, 180, 30f );
                    else
                        currentY -= ROW_ADVANCE_WHEN_CLOSED;
                }
                #endregion
            }

            public override void Clear()
            {
                this.Category = null;
                this.Button.OverridingTextToShow = null;
                this.OptionsInCategory.Clear();
            }
            public override PoolableGUIGroup DuplicateSelf()
            {
                btnDirectPlacementCategory newCategoryBut = (btnDirectPlacementCategory)this.Button.DuplicateSelf();
                //int siblingIndex = btnQuickDefenses.Instance.Element.transform.GetSiblingIndex();
                //newCategoryBut.Element.GO.transform.SetSiblingIndex( siblingIndex );
                return new DirectPlacementCategory( newCategoryBut );
            }
        }
        #endregion
    }
}
