using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcen.AIW2.External
{
    public static class ShipListerUtils
    {
        #region CalculateShipsThatBenefit_ThatYouHave
        public static void CalculateShipsThatBenefit_ThatYouHave( Predicate<FleetMembership> extraFilterCriteria,
            SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark> result,
            bool AlsoSort, Fleet fleetBeingUpgraded, bool UpgradeEntireFleet )
        {
            result.Clear();

            if ( UpgradeEntireFleet )
            {
                ShipListerUtils.CalculateShipsThatYouHave(
                    ( mem ) =>
                    {
                        if ( mem == null )
                            return false;
                        Fleet fleet = mem.Fleet;
                        if ( fleet == null )
                            return false;
                        GameEntityTypeData.MarkLevelStats forMark = mem.ForMark;
                        if ( forMark == null )
                            return false;

                        return fleet == fleetBeingUpgraded && 
                               mem.TypeData.StartingMarkLevel.Ordinal > 0 &&
                               forMark.MarkLevel.Ordinal > 0 && 
                               extraFilterCriteria( mem ) &&
                               (mem.EffectiveSquadCap >= 1 || mem.EntitiesOfFMem.Count > 0);
                    },
                    result, AlsoSort, true );
            }
            else
            {
                ShipListerUtils.CalculateShipsThatYouHave(
                    delegate( FleetMembership fleetMembership )
                    {
                        if ( fleetMembership == null )
                            return false;
                        Fleet fleet = fleetMembership.Fleet;
                        if ( fleet == null )
                            return false;
                        GameEntityTypeData.MarkLevelStats forMark = fleetMembership.ForMark;
                        if ( forMark == null )
                            return false;

                        return fleet == fleetBeingUpgraded && fleetMembership.TypeData.StartingMarkLevel.Ordinal > 0 &&
                               forMark.MarkLevel.Ordinal > 0 && extraFilterCriteria( fleetMembership )
                               && ((fleet.Centerpiece.GetSquad() != null &&
                                    fleetMembership.TypeData == fleet.Centerpiece.GetSquad().TypeData) ||
                                   fleetMembership.TypeData.IsDrone) &&
                               (fleetMembership.EffectiveSquadCap >= 1 || fleetMembership.EntitiesOfFMem.Count > 0 ||
                                fleetMembership.TransportContents.Count > 0);
                    },
                    result, AlsoSort, true
                );
            }
        }
        #endregion

        #region CalculateShipsThatBenefit_ThatYouHave
        public static void CalculateShipsThatBenefit_ThatYouHave( TechUpgrade Tech, SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark> result,
            bool AlsoSort )
        {
            result.Clear();

            ShipListerUtils.CalculateShipsThatYouHave(
                fleetMembership => fleetMembership.TypeData.TechUpgradesThatBenefitMe.Contains( Tech ) && fleetMembership.TypeData.StartingMarkLevel.Ordinal > 0 &&
                (fleetMembership.EffectiveSquadCap >= 1 || fleetMembership.EntitiesOfFMem.Count > 0 || fleetMembership.TransportContents.Count > 0 ),
                result, AlsoSort, false
                );

        }
        #endregion

        #region CalculateShipsThatBenefit_ThatYouCanCapture
        public static void CalculateShipsThatBenefit_ThatYouCanCapture( TechUpgrade Tech, SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataForSingleMark> result,
            Dictionary<GameEntityTypeData, bool> shipsToExcludeOrNull, bool AlsoSort )
        {
            result.Clear();
            if ( Tech == null )
                return;

            ShipListerUtils.CalculateShipsThatYouCanCapture(
                typeData => typeData.TechUpgradesThatBenefitMe.Contains( Tech ) && typeData.StartingMarkLevel.Ordinal > 0,
                result, shipsToExcludeOrNull, AlsoSort
                );
        }
        #endregion

        #region CalculateShipsThatYouHave
        public static void CalculateShipsThatYouHave( Predicate<FleetMembership> filterCriteria, SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark> result, bool AlsoSort, bool OnlyLookAtOwnFleets )
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;
                result.Clear();

                debugStage = 200;

                debugStage = 300;
                Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();

                debugStage = 500;
                if ( localFaction != null )
                {
                    debugStage = 1500;
                    //do this for all of my own fleets
                    foreach ( Fleet fleet in World_AIW2.Instance.Fleets( localFaction, FleetStatus.CenterpieceMustLiveOrLooseFleet ) )
                    {
                        foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                        {
                            if ( filterCriteria( mem ) )
                            {
                                ShipDataByMark byMark = result[mem.TypeData];
                                if ( byMark.TypeData == null )
                                    byMark.TypeData = mem.TypeData;
                                if ( byMark.TypeData != mem.TypeData )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Incorrect mark mapping A! : " + byMark.TypeData.InternalName + " vs " + mem.TypeData.InternalName, Verbosity.ShowAsError );

                                bool useCurrentCount = false;
                                if (GameSettings.Current.GetBoolBySetting("DisplayBuiltDefenseStength")) {
                                    useCurrentCount = mem.TypeData.SelfConstructs;
                                }

                                int addedCount = mem.EffectiveSquadCap;
                                if ( addedCount < 0 )
                                    addedCount = 0;
                                int shipsHere = mem.GetCountPresent( true, ExtraFromStacks.IncludePrecalc );
                                if ( useCurrentCount || shipsHere > addedCount )
                                    addedCount = shipsHere;
                                byMark.AddToCountByMark( mem.EffectiveMark, addedCount );
                                if ( byMark.FleetToUse == null )
                                    byMark.FleetToUse = fleet;
                                continue;
                            }
                        }
                    }
                }

                if ( !OnlyLookAtOwnFleets )
                {
                    foreach ( Faction npcAlly in World_AIW2.Instance.Factions )
                    {
                        if ( npcAlly.Type != FactionType.SpecialFaction || !npcAlly.InheritsTechUpgradesFromPlayerFactions )
                            continue; //skip anything that is not inheriting from our tech upgrades

                        //do this for these allied fleets
                        foreach ( Fleet fleet in World_AIW2.Instance.Fleets( npcAlly, FleetStatus.AnyStatus ) )
                        {
                            foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                            {
                                if ( filterCriteria( mem ) )
                                {
                                    ShipDataByMark byMark = result[mem.TypeData];
                                    if ( byMark.TypeData == null )
                                        byMark.TypeData = mem.TypeData;
                                    if ( byMark.TypeData != mem.TypeData )
                                        ArcenDebugging.ArcenDebugLogSingleLine( "Incorrect mark mapping B! : " + byMark.TypeData.InternalName + " vs " + mem.TypeData.InternalName, Verbosity.ShowAsError );
                                    int actualCount = mem.GetCountPresent( true, ExtraFromStacks.IncludePrecalc );
                                    if ( actualCount < 0 )
                                        actualCount = 0;
                                    byMark.AddToCountByMark( mem.EffectiveMark, actualCount );
                                    if ( byMark.FleetToUse == null )
                                        byMark.FleetToUse = fleet;
                                    continue;
                                }
                            }
                        }
                    }
                }

                debugStage = 3500;
                if ( AlsoSort )
                {
                    debugStage = 4500;
                    //sort the entity types
                    List<KeyValuePair<GameEntityTypeData, ShipDataByMark>> sortedResult =
                        result.SortIntoList( delegate ( KeyValuePair<GameEntityTypeData, ShipDataByMark> Left, KeyValuePair<GameEntityTypeData, ShipDataByMark> Right )
                        {
                            //put fleet leaders first
                            int val = Right.Key.IsFleetLeader.CompareTo( Left.Key.IsFleetLeader );
                            if ( val != 0 )
                                return val;
                            //sort by putting mobile ships first, then structures. Then sort by alphabetical order
                            val = Right.Key.IsMobile.CompareTo( Left.Key.IsMobile );
                            if ( val != 0 )
                                return val;
                            //then by name
                            return Left.Key.DisplayName.CompareTo( Right.Key.DisplayName );
                        } );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "CalculateShipsThatYouHave exception at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
            }
        }
        #endregion

        #region CalculateShipsThatYouCanCapture
        public static void CalculateShipsThatYouCanCapture( Predicate<GameEntityTypeData> filterCriteria,
            SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataForSingleMark> result, Dictionary<GameEntityTypeData, bool> shipsToExcludeOrNull, bool AlsoSort )
        {
            Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
            result.Clear();

            if ( localFaction != null )
            {
                for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
                {
                    Faction aiFaction = World_AIW2.Instance.AIFactions[i];
                    if ( aiFaction == null )
                        continue;
                    foreach ( GameEntity_Squad entity in aiFaction.Squads( EntityRollupType.GrantsStuffToPlayers ) )
                    {
                        if ( entity == null )
                            continue;
                        Planet plan = entity.Planet;
                        if ( plan == null )
                            continue;
                        if ( plan.IntelLevel <= PlanetIntelLevel.Unexplored )
                            continue;
                        for ( int j = 0; j < entity.ShipGrantsList.Count; j++ )
                        {
                            ShipLineEntry entry = entity.ShipGrantsList[j];
                            if ( entry == null )
                                continue;
                            GameEntityTypeData typeData = entry.TypeData;
                            if ( typeData == null )
                                continue;
                            if ( entry.BaseNumShips <= 0 )
                                continue;
                            if ( filterCriteria( typeData ) )
                            {
                                if ( (shipsToExcludeOrNull == null || !shipsToExcludeOrNull.ContainsKey( typeData ) ))
                                {
                                    ShipDataForSingleMark shipData = result[typeData];
                                    if ( shipData.TypeData == null )
                                        shipData.TypeData = typeData;
                                    if ( shipData.TypeData != typeData )
                                        ArcenDebugging.ArcenDebugLogSingleLine( "Incorrect markless mapping MA! : " + shipData.TypeData.InternalName + " vs " + typeData.InternalName, Verbosity.ShowAsError );

                                    if ( shipData.MarkLevel == 0 )
                                        shipData.MarkLevel = localFaction.GetGlobalMarkLevelForShipLine( typeData );

                                    if ( entry.BaseNumShips > 0 )
                                        shipData.AddToCountToCapture( entry.BaseNumShips );
                                    //if ( shipData.FleetToUse == null ) //leave it null in this case
                                    //    shipData.FleetToUse = fleet;
                                }
                            }
                        }
                    }
                }
                foreach ( Fleet fleet in World_AIW2.Instance.Fleets( FleetStatus.CenterpieceMustLive ) )
                {
                    if ( fleet == null )
                        continue;
                    switch ( fleet.Category )
                    {
                        case FleetCategory.PlayerMobile:
                        case FleetCategory.PlayerBattlestation:
                        case FleetCategory.PlayerCustomCityFedMobile:
                        case FleetCategory.PlayerCustomUnattachedMobile:
                            break;
                        default:
                            continue;
                    }
                    Faction fac = fleet.Faction;
                    if ( fac == null )
                        continue;
                    if ( fac.Type != FactionType.NaturalObject )
                        continue;
                    GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                    if ( centerpiece == null )
                        continue; //don't know about this yet!
                    Planet centerPlanet = centerpiece.Planet;
                    if ( centerPlanet == null )
                        continue;
                    if ( centerPlanet.IntelLevel <= PlanetIntelLevel.Unexplored )
                        continue; //don't know about this yet!

                    foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
                    {
                        if ( mem == null )
                            continue;
                        GameEntityTypeData typeData = mem.TypeData;
                        if ( typeData == null )
                            continue;
                        if ( filterCriteria( typeData ) )
                        {
                            if ( (shipsToExcludeOrNull == null || !shipsToExcludeOrNull.ContainsKey( typeData ) ) )
                            {
                                byte effectiveMark = mem.EffectiveMark;
                                try
                                {
                                    byte markLevelToUse = localFaction.GetGlobalMarkLevelForShipLine( mem.TypeData );
                                    effectiveMark = Math.Max( mem.EffectiveMark, markLevelToUse );
                                }
                                catch { }

                                ShipDataForSingleMark shipData = result[typeData];
                                if ( shipData.TypeData == null )
                                    shipData.TypeData = typeData;
                                if ( shipData.TypeData != typeData )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Incorrect markless mapping MB! : " + shipData.TypeData.InternalName + " vs " + typeData.InternalName, Verbosity.ShowAsError );

                                if ( shipData.MarkLevel < effectiveMark )
                                    shipData.MarkLevel = effectiveMark;


                                int baseShipCap = mem.GetBaseSquadCapWithAdditions();
                                if ( baseShipCap <= 0 )
                                    baseShipCap = mem.EntitiesOfFMem.Count;

                                if ( baseShipCap > 0 )
                                    shipData.AddToCountToCapture( baseShipCap );
                                if ( shipData.FleetToUse == null )
                                    shipData.FleetToUse = mem.Fleet;
                            }
                        }
                    }
                }
            }

            if ( AlsoSort )
            {
                result.SortIntoList( 
                    (a,b)=>
                    {
                        //sort by putting mobile ships first, then structures. Then sort by alphabetical order
                        int val = b.Key.IsMobile.CompareTo( a.Key.IsMobile );
                        if ( val != 0 )
                            return val;
                        return a.Key.DisplayName.CompareTo( b.Key.DisplayName );
                    } );
            }
        }
        #endregion

        #region CalculateKnownShipsThatEnemiesHave
        public static void CalculateKnownShipsThatEnemiesHave( SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark> result,
            Predicate<SafeSquadWrapper> overallfilterCriteriaThatBlocksEverythinig,
            Predicate<SafeSquadWrapper> filterCriteriaForSquads, Predicate<GameEntityTypeData> filterCriteriaForShipsInStorage, bool AlsoSort )
        {
            result.Clear();

            foreach ( Faction fac in World_AIW2.Instance.Factions )
            {
                if ( !fac.GetIsHostileToLocalFaction() )
                    continue;

                foreach ( GameEntity_Squad squad in fac.Squads() )
                {
                    if ( !squad.GetShouldBeVisibleBasedOnPlanetIntel() )
                        continue;

                    SafeSquadWrapper squadWrapper = SafeSquadWrapper.Create( squad );

                    if ( overallfilterCriteriaThatBlocksEverythinig( squadWrapper ) )
                    {
                        byte markLevel = squad.CurrentMarkLevel;
                        if ( filterCriteriaForSquads( squadWrapper ) )
                        {
                            ShipDataByMark byMark = result[squad.TypeData];
                            if ( byMark.TypeData == null )
                                byMark.TypeData = squad.TypeData;
                            if ( byMark.TypeData != squad.TypeData )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Incorrect enemy mark mapping EA! : " + byMark.TypeData.InternalName + " vs " + squad.TypeData.InternalName, Verbosity.ShowAsError );
                            int shipsHere = 1 + squad.ExtraStackedSquadsInThis;
                            if ( shipsHere < 1 )
                                shipsHere = 1;
                            byMark.AddToCountByMark( squad.CurrentMarkLevel, shipsHere );
                            if ( byMark.FleetToUse == null )
                                byMark.FleetToUse = squad.GetFleetOrNull_Safe();
                        }

                        if ( squad.AIReinforcementPointContents != null )
                        {
                            foreach ( RefPair<GameEntityTypeData, int> reinforcement in squad.AIReinforcementPointContents )
                            {
                                if ( reinforcement.RightItem <= 0 )
                                    continue;
                                GameEntityTypeData reinforcementTypeData = reinforcement.LeftItem;
                                if ( filterCriteriaForShipsInStorage( reinforcementTypeData ) )
                                {
                                    ShipDataByMark byMark = result[reinforcementTypeData];
                                    if ( byMark.TypeData == null )
                                        byMark.TypeData = reinforcementTypeData;
                                    if ( byMark.TypeData != reinforcementTypeData )
                                        ArcenDebugging.ArcenDebugLogSingleLine( "Incorrect enemy mark mapping EA! : " + byMark.TypeData.InternalName + " vs " + reinforcementTypeData.InternalName, Verbosity.ShowAsError );
                                    int shipsHere = reinforcement.RightItem;
                                    if ( shipsHere < 1 )
                                        shipsHere = 1;
                                    byMark.AddToCountByMark( markLevel, shipsHere );
                                    if ( byMark.FleetToUse == null )
                                        byMark.FleetToUse = squad.GetFleetOrNull_Safe();
                                }
                            }
                        }
                    }
                }
            }

            if ( AlsoSort )
            {
                result.SortIntoList( delegate ( KeyValuePair<GameEntityTypeData, ShipDataByMark> Left, KeyValuePair<GameEntityTypeData, ShipDataByMark> Right )
                {
                    //put fleet leaders first
                    int val = Right.Key.IsFleetLeader.CompareTo( Left.Key.IsFleetLeader );
                    if ( val != 0 )
                        return val;
                    //sort by putting mobile ships first, then structures. Then sort by alphabetical order
                    val = Right.Key.IsMobile.CompareTo( Left.Key.IsMobile );
                    if ( val != 0 )
                        return val;
                    return Left.Key.DisplayName.CompareTo( Right.Key.DisplayName );
                } );
            }
        }
        #endregion

        #region GetUsesShipCaps
        public static bool GetUsesShipCaps( GameEntityTypeData TypeData )
        {
            if ( TypeData.IsCommandStation || TypeData.SkipsDrawingShipCap || TypeData.IsFleetLeader || TypeData.IsDrone )
                return false;
            return true;
        }
        #endregion

        #region WriteCountsAndStrengthsForEachMarkLevelFrom_ForTechUpgradeOrDowngrade
        public static void WriteCountsAndStrengthsForEachMarkLevelFrom_ForTechUpgradeOrDowngrade( GameEntityTypeData TypeData, ShipDataByMark DataByMark,
                                                                                                  ArcenCharacterBufferBase Buffer, int MarksBeingAddedOrRemoved, Faction playerFaction, bool showIcons )
        {
            if ( DataByMark.TypeData != TypeData )
                ArcenDebugging.ArcenDebugLogSingleLine( "Incorrect mark mapping C! : " + DataByMark.TypeData.InternalName + " vs " + TypeData.InternalName, Verbosity.ShowAsError );

            bool foundAny = false;
            for ( byte mark = 0; mark < DataByMark.CountsByMarkLength(); mark++ )
            {
                int count = DataByMark.GetCountByMark( mark );
                if ( count <= 0 )
                    continue;
                if ( !foundAny )
                    foundAny = true;
                else
                    Buffer.Add( ", " );

                int oldCount = count;
                int newCount;

                GameEntityTypeData.MarkLevelStats markStatsOld = TypeData.MarkStatsFor( mark );
                GameEntityTypeData.MarkLevelStats markStatsNew = TypeData.MarkStatsFor( (byte)(mark + MarksBeingAddedOrRemoved) );

                Fleet fleetToUse = DataByMark.FleetToUse;
                Faction factionToUse = fleetToUse != null ? fleetToUse.Faction : null;
                if ( factionToUse == null )
                    factionToUse = playerFaction;

                if ( factionToUse.Type != FactionType.Player )
                {
                    newCount = oldCount; //npc ship counts don't go up from upgrades
                }
                else
                {
                    newCount = TypeData.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouDoNOTKnowBaseCap( TypeData, oldCount,
                         false, markStatsOld.MarkLevel, markStatsNew.MarkLevel );
                    PlayerTypeData playerType = factionToUse.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if ( playerType != null && !playerType.ShipCountsGoUpFromUpgrades )
                        newCount = oldCount; //necromancers don't get more units from tech upgrades, for one main example
                }

                Balance_MarkLevel markLevelVisuals = markStatsOld.MarkLevel;
                Buffer.Add( "<color=#" + markLevelVisuals.ColorHex + ">" ).Add( TypeData.DisplayName ).Add( " " ).Add( markLevelVisuals.MapDisplay ).Add( "</color>" );

                Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                if (showIcons && localFaction != null)
                    Buffer.AddShipIconInline(TypeData, localFaction);

                if ( oldCount != newCount )
                {
                    Buffer.Add( " x" ).Add( newCount ).Add( " <size=80%>(x" ).Add( oldCount ).Add( ")</size>" );
                    //if ( UseTheoretical )
                    //    Buffer.Add( " UseTheoretical" );
                    //if ( usesCaps )
                    //    Buffer.Add( " usesCaps" );
                    //Buffer.Add( " ActualPresent " ).Add( mark.ActualPresent );
                    //Buffer.Add( " EffectiveCap " ).Add( mark.EffectiveCap );
                    //Buffer.Add( mark.DebugText );
                }
                else
                    Buffer.Add( " x" ).Add( oldCount );

                int oldStrength = markStatsOld.StrengthPerSquad_CalculatedWithNullFleetMembership * oldCount;
                int newStrength = markStatsNew.StrengthPerSquad_CalculatedWithNullFleetMembership * newCount;

                Buffer.Add( " " );
                Buffer.Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon ).StartColor( ArcenExternalUIUtilities.StrengthTextColor );
                EntityText.AddSingleValueStrengthOnly( Buffer, newStrength );
                Buffer.EndColor();
                if ( oldStrength != newStrength )
                {
                    Buffer.Add( " <size=80%>(" );
                    Buffer.Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon ).StartColor( ArcenExternalUIUtilities.StrengthTextColor );
                    EntityText.AddSingleValueStrengthOnly( Buffer, oldStrength );
                    Buffer.EndColor();
                    Buffer.Add( ")" ).Add( "</size>" );
                }
            }

            if ( !foundAny )
                Buffer.Add( TypeData.DisplayName ).Add( " x?" );
        }
        #endregion

        #region WriteCountsAndStrengthsForEachMarkLevelFrom_ForTechUpgradeOrDowngrade_DataSingleMark
        public static void WriteCountsAndStrengthsForEachMarkLevelFrom_ForTechUpgradeOrDowngrade_DataSingleMark(ShipDataForSingleMark DataSingleMark, GameEntityTypeData ship,
                                                                                                                 ArcenCharacterBufferBase Buffer, int MarksBeingAddedOrRemoved, bool showIcons )
        {
            {
                bool usesCaps = GetUsesShipCaps( ship );
                int oldCount;
                int newCount;
                GameEntityTypeData.MarkLevelStats oldStats;
                GameEntityTypeData.MarkLevelStats newStats;
                bool isFirst = true;
                {
                    int baseCount = DataSingleMark.GetCountToCapture();
                    oldCount = baseCount;
                    if ( oldCount > 0 && usesCaps )
                        oldCount = ship.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( ship, baseCount, baseCount, DataSingleMark.MarkLevel );

                    if ( oldCount == 0 )
                        return;
                    if ( isFirst )
                        isFirst = false;
                    else
                        Buffer.Add( ", " );

                    oldStats = ship.MarkStatsFor( DataSingleMark.MarkLevel );
                    newStats = ship.MarkStatsFor( (byte)(DataSingleMark.MarkLevel + MarksBeingAddedOrRemoved) );

                    if ( oldStats.MarkLevel.Ordinal != newStats.MarkLevel.Ordinal )
                        newCount = !usesCaps ? baseCount : ship.MarkScaleStyle.GetResultingCapFromSquadCapMultiplierList_WhenYouKnowBaseCap( ship, baseCount, baseCount, newStats.MarkLevel );
                    else //already max mark for this ship
                        newCount = oldCount;

                    Balance_MarkLevel markLevelVisuals = Balance_MarkLevelTable.Instance.RowsByOrdinal[DataSingleMark.MarkLevel];
                    Buffer.Add( "<color=#" + markLevelVisuals.ColorHex + ">" ).Add( ship.DisplayName ).Add( " " ).Add( markLevelVisuals.MapDisplay ).Add( "</color>" );
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    if (showIcons && localFaction != null )
                        Buffer.AddShipIconInline(ship, localFaction);

                    if ( oldCount != newCount )
                    {
                        Buffer.Add( " x" ).Add( newCount ).Add( " <size=80%>(x" ).Add( oldCount ).Add( ")</size>" );
                    }
                    else
                        Buffer.Add( " x" ).Add( oldCount );

                    int oldStrength = oldStats.StrengthPerSquad_CalculatedWithNullFleetMembership * oldCount;
                    int newStrength = newStats.StrengthPerSquad_CalculatedWithNullFleetMembership * newCount;

                    Buffer.Add( " " );
                    Buffer.Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon ).StartColor( ArcenExternalUIUtilities.StrengthTextColor );
                    EntityText.AddSingleValueStrengthOnly( Buffer, newStrength );
                    Buffer.EndColor();
                    if ( oldStrength != newStrength )   
                    {
                        Buffer.Add( " <size=80%>(");
                        Buffer.Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon ).StartColor( ArcenExternalUIUtilities.StrengthTextColor );
                        EntityText.AddSingleValueStrengthOnly( Buffer, oldStrength );
                        Buffer.EndColor();
                        Buffer.Add( ")" ).Add("</size>");
                    }
                }
                if ( isFirst )
                {
                    //never found anything!
                    Buffer.Add( ship.DisplayName ).Add( " x???" );
                }
            }
        }
        #endregion
    }

    public class ShipDataByMark : ICustomInternallyPooledData
    {
        public GameEntityTypeData TypeData;
        private int[] CountsByMark = new int[8];
        public Fleet FleetToUse = null;

        public int CountsByMarkLength()
        {
            return this.CountsByMark.Length;
        }

        public void AddToCountByMark( int Index, int Amount )
        {
            int current = this.CountsByMark[Index];
            if ( current < 0 )
                current = 0;
            if ( Amount > 0 )
                current += Amount;

            this.CountsByMark[Index] = current;
        }

        public int GetCountByMark( int Index )
        {
            return this.CountsByMark[Index];
        }

        public void Reset()
        {
            for ( int i = 0; i < CountsByMark.Length; i++ )
                CountsByMark[i] = 0;

            this.FleetToUse = null;
            this.TypeData = null;
        }
    }

    public class ShipDataForSingleMark : ICustomInternallyPooledData
    {
        public GameEntityTypeData TypeData;
        private int CountToCapture;
        public Fleet FleetToUse = null;
        public byte MarkLevel = 0;

        public void AddToCountToCapture( int Amount )
        {
            if ( this.CountToCapture < 0 )
                this.CountToCapture = 0;
            if ( Amount > 0 )
                this.CountToCapture += Amount;
        }

        public int GetCountToCapture()
        {
            return this.CountToCapture;
        }

        public void Reset()
        {
            this.CountToCapture = 0;
            this.MarkLevel = 0;

            this.FleetToUse = null;
            this.TypeData = null;
        }
    }
}
