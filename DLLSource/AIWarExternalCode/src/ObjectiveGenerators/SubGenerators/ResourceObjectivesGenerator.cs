using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class ResourceObjectivesGenerator
    {
        public static void CheckForResourceObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                GenerateAcquireScienceObjectives();
                GenerateZenithPowerGeneratorObjectives();
                GenerateZenithMatterConverterObjectives();
                GenerateGrantsAddedToCommandStationObjectives();
                GenerateSpireArchiveObjectives();
                GenerateAcquireHackingObjectives();
                GenerateAcquireScienceByDestructionObjectives();
                GenerateAcquireEssenceByDestructionObjectives();
                GenerateAcquireHackingByDestructionObjectives();
                GenerateAcquireTechObjectives();
                GenerateAcquireScienceAndHackingByDestructionObjectives();
                GenerateSocketIncreaserObjectives();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in ResourceObjectivesGenerator generation: " + e, Verbosity.ShowAsError );
            }
        }

        private static void GenerateSpireArchiveObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( playerFaction ) )
                return;

            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "SpireArchive" ) )
            {
                if(entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored)
                    continue;
                {
                    #region Spire Archive
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "HackSpireArchive" );
                    objective.RelatedEntity1 = entity;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
        private static void GenerateAcquireTechObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( playerFaction ) )
                return;

            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "TechVault" ) )
            {
                if(entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored)
                    continue;
                {
                    #region Intra-Galactic Coordinator
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "HackTechVault" );
                    objective.DisplayNameBase = "Hack ";
                    objective.RelatedEntity1 = entity;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
        private static void GenerateGrantsAddedToCommandStationObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( playerFaction ) )
                return;

            //DSS Style
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.HackableForCommandStationsAndBattleStations_DSSStyle ) )
            {
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                {
                    #region TSS or ODSS Command Augmenter
                    {
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        if ( entity.TypeData.HackableForCommandStationsAndBattleStations_TurretCount > 0 )
                            objective.SetHook( "HackTSS" );
                        else
                            objective.SetHook( "HackODSS" );
                        objective.RelatedEntity1 = entity;
                        ObjectiveCategory.AddActualObjective( objective );
                    }
                    #region Check for optional objective types that have been enabled in settings to show specific unit types (aoe, sniper, melee, etc)
                    foreach ( ObjectiveCategory objectiveCategory in ObjectiveCategoryTable.Instance.Rows )
                    {
                        if ( !objectiveCategory.IsFilteredUnitAquisitionObjective )
                            continue;
                        bool foundMatchingUnitType = false;
                        for ( int i = 0; i < entity.ShipGrantsList.Count; i++ )
                        {
                            GameEntityTypeData entityType = entity.ShipGrantsList[i].TypeData;
                            if ( !AIObjectivesGenerator.GetEntityMatchesObjectiveCategory( entityType, objectiveCategory ) )
                                continue;
                            foundMatchingUnitType = true;
                            break;
                        }
                        if ( !foundMatchingUnitType )
                            continue;
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        if ( entity.TypeData.HackableForCommandStationsAndBattleStations_TurretCount > 0 )
                            objective.SetHook( "TSS" + objectiveCategory.InternalName );
                        else
                            objective.SetHook( "ODSS" + objectiveCategory.InternalName );
                        objective.RelatedEntity1 = entity;
                        ObjectiveCategory.AddActualObjective( objective );
                    }
                    #endregion
                    #endregion
                }

            }
        }

        private static void GenerateZenithPowerGeneratorObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( playerFaction ) )
                return;

            Faction neutralFaction = World_AIW2.Instance.GetNeutralFaction();
            foreach ( GameEntity_Squad entity in neutralFaction.Squads( "ZenithPowerGenerator" ) )
            {
                if(entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored)
                    continue;
                {
                    #region Reclaim ZPG
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "CaptureZenithPowerGenerator" );
                    objective.RelatedEntity1 = entity;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }

        private static void GenerateZenithMatterConverterObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( playerFaction ) )
                return;

            Faction neutralFaction = World_AIW2.Instance.GetNeutralFaction();
            foreach ( GameEntity_Squad entity in neutralFaction.Squads( "ZenithMatterConverter" ) )
            {
                if(entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored)
                    continue;
                {
                    #region claim structure
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "CaptureZenithMatterConverter" );
                    objective.RelatedEntity1 = entity;
                    ObjectiveCategory.AddActualObjective( objective );
                    #endregion
                }
            }
        }
        // Reused per call to tally how many destruction-reward units sit on each planet, so we can
        // emit one aggregated objective per planet instead of one per unit. Without this, factions
        // with many roaming reward-units (e.g. Elderlings) flood the intel menu with dozens of
        // near-identical "destroy this" objectives that are individually useless.
        private static readonly Dictionary<Planet, int> perPlanetDestructionCounts =
            Dictionary<Planet, int>.Create_WillNeverBeGCed( 200, "ResourceObjectivesGenerator-perPlanetDestructionCounts" );

        // Emits one objective per planet present in perPlanetDestructionCounts (count carried in
        // RelatedInt1 and shown in the name), then clears the working dictionary. The hook's tooltip
        // recomputes exact reward totals live from the planet's surviving units.
        private static void EmitPerPlanetDestructionObjectives( string hook, string displayNameBase )
        {
            foreach ( KeyValuePair<Planet, int> pair in perPlanetDestructionCounts )
            {
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( hook );
                objective.DisplayNameBase = displayNameBase + "(" + pair.Value + ") ";
                objective.RelatedPlanet1 = pair.Key;
                objective.RelatedInt1 = pair.Value;
                ObjectiveCategory.AddActualObjective( objective );
            }
            perPlanetDestructionCounts.Clear();
        }

        private static void AddToPerPlanetDestructionCount( GameEntity_Squad entity )
        {
            int existing;
            perPlanetDestructionCounts.TryGetValue( entity.Planet, out existing );
            perPlanetDestructionCounts[entity.Planet] = existing + 1;
        }

        private static void GenerateAcquireScienceByDestructionObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( playerFaction ) )
                return;

            perPlanetDestructionCounts.Clear();
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.IncreasesScienceOnDeath ) )
            {
                if ( entity == null || entity.Planet == null )
                    continue;
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                AddToPerPlanetDestructionCount( entity );
            }
            EmitPerPlanetDestructionObjectives( "AcquireScienceByDestructionOnPlanet", "Science by Destruction " );
        }
        private static void GenerateAcquireEssenceByDestructionObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( !NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( playerFaction ) )
                return;

            perPlanetDestructionCounts.Clear();
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "EssenceGranter" ) )
            {
                if ( entity == null || entity.Planet == null )
                    continue;
                DLC3GameEntityTypeDataExtension entity_DLC3TypeData = entity.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
                if ( entity_DLC3TypeData == null )
                    continue;
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                AddToPerPlanetDestructionCount( entity );
            }
            EmitPerPlanetDestructionObjectives( "AcquireEssenceByDestructionOnPlanet", "Essence by Destruction " );
        }
        private static void GenerateAcquireHackingByDestructionObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( playerFaction ) )
                return;

            perPlanetDestructionCounts.Clear();
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.IncreasesHackingOnDeath ) )
            {
                if ( entity == null || entity.Planet == null )
                    continue;
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                AddToPerPlanetDestructionCount( entity );
            }
            EmitPerPlanetDestructionObjectives( "AcquireHackingByDestructionOnPlanet", "HaP by Destruction " );
        }
        private static void GenerateAcquireScienceAndHackingByDestructionObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( playerFaction ) )
                return;

            perPlanetDestructionCounts.Clear();
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.IncreasesScienceAndHackingOnDeath ) )
            {
                if ( entity == null || entity.Planet == null )
                    continue;
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                if ( !entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                AddToPerPlanetDestructionCount( entity );
            }
            EmitPerPlanetDestructionObjectives( "AcquireScienceAndHackingByDestructionOnPlanet", "Science & HaP by Destruction " );
        }
        private static void GenerateAcquireScienceObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( playerFaction ) )
                return;

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                if ( planet.GetControllingFaction().GetIsHostileTowards( playerFaction ) )
                    continue;

                if ( planet.GetScienceLeftForHumans().IntValue > 0 )
                {
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction(playerFaction);
                    if ( pFaction.AIPLeftFromCommandStation > 0 )
                        continue; //don't include planets sniped by marauders
                    {
                        #region Acquire Science
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "AcquireScienceFromPlanet" );
                        objective.RelatedPlanet1 = planet;
                        objective.RelatedInt1 = planet.GetScienceLeftForHumans().IntValue;
                        ObjectiveCategory.AddActualObjective( objective );
                        #endregion
                    }
                }
            }
        }
        private static void GenerateAcquireHackingObjectives()
        {
            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( playerFaction == null )
                return;
            if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( playerFaction ) )
                return;
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                if ( planet.GetControllingFaction().GetIsHostileTowards( playerFaction ) )
                    continue;

                if ( planet.GetHackingLeftForHumans().IntValue > 0 )
                {
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction(playerFaction);
                    if ( pFaction.AIPLeftFromCommandStation > 0 )
                        continue; //don't include planets sniped by marauders or something
                    {
                        #region Acquire HaP
                        ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                        objective.SetHook( "AcquireHackingFromPlanet" );
                        objective.RelatedPlanet1 = planet;
                        objective.RelatedInt1 = planet.GetHackingLeftForHumans().IntValue;
                        ObjectiveCategory.AddActualObjective( objective );
                        #endregion
                    }
                }
            }
        }

        private static void GenerateSocketIncreaserObjectives()
        {
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "MinorSocketIncreaser" ) )
            {
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "ClaimMinorForge" );
                objective.RelatedEntity1 = entity;
                ObjectiveCategory.AddActualObjective( objective );
            }
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "MajorSocketIncreaser" ) )
            {
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "ClaimMajorForge" );
                objective.RelatedEntity1 = entity;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        internal enum DestructionResourceKind { Science, Hacking, ScienceAndHacking, Essence }

        // Working state reused across tooltip renders (tooltips render one at a time on the UI thread).
        private static readonly Dictionary<GameEntityTypeData, int> killCounts = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed( 64, "ResourceObjectivesGenerator-killCounts" );
        private static readonly Dictionary<GameEntityTypeData, long> killWeights = Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 64, "ResourceObjectivesGenerator-killWeights" );
        private static readonly Dictionary<GameEntityTypeData, FInt> killEssence = Dictionary<GameEntityTypeData, FInt>.Create_WillNeverBeGCed( 64, "ResourceObjectivesGenerator-killEssence" );
        private static readonly List<GameEntityTypeData> killTypes = List<GameEntityTypeData>.Create_WillNeverBeGCed( 64, "ResourceObjectivesGenerator-killTypes" );

        // Shared renderer for the per-planet by-destruction tooltips: a summary line plus, below it,
        // up to 4 ship types you can kill for this resource (most granted first), each with its icon,
        // count, and resource total. One pass over the planet's matching units.
        internal static void AppendDestructionTooltip( ArcenDoubleCharacterBuffer buffer, Planet planet, DestructionResourceKind kind )
        {
            killCounts.Clear();
            killWeights.Clear();
            killEssence.Clear();
            killTypes.Clear();

            int count = 0, science = 0, hacking = 0, aip = 0;
            FInt essence = FInt.Zero;

            if ( kind == DestructionResourceKind.Essence )
            {
                foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "EssenceGranter" ) )
                {
                    if ( entity == null || entity.Planet != planet )
                        continue;
                    DLC3GameEntityTypeDataExtension ext = entity.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
                    if ( ext == null )
                        continue;
                    ext.GetNecromancerResourcesToGrantOnDeath( entity, out _, out _, out FInt entityEssence );
                    count++;
                    essence += entityEssence;
                    aip += entity.TypeData.AIPOnDeath;
                    AccumulateKill( entity.TypeData, entityEssence.IntValue );
                    FInt prev;
                    killEssence.TryGetValue( entity.TypeData, out prev );
                    killEssence[entity.TypeData] = prev + entityEssence;
                }
            }
            else
            {
                EntityRollupType rollup = kind == DestructionResourceKind.Hacking ? EntityRollupType.IncreasesHackingOnDeath
                    : kind == DestructionResourceKind.ScienceAndHacking ? EntityRollupType.IncreasesScienceAndHackingOnDeath
                    : EntityRollupType.IncreasesScienceOnDeath;
                foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( rollup ) )
                {
                    if ( entity == null || entity.Planet != planet )
                        continue;
                    int s = entity.TypeData.ScienceToGrantOnDeath;
                    int h = entity.TypeData.HackingToGrantOnDeath;
                    count++;
                    aip += entity.TypeData.AIPOnDeath;
                    if ( kind == DestructionResourceKind.Hacking )
                    {
                        hacking += h;
                        AccumulateKill( entity.TypeData, h );
                    }
                    else if ( kind == DestructionResourceKind.ScienceAndHacking )
                    {
                        science += s;
                        hacking += h;
                        AccumulateKill( entity.TypeData, (long)s + h );
                    }
                    else
                    {
                        science += s;
                        AccumulateKill( entity.TypeData, s );
                    }
                }
            }

            buffer.Add( "摧毁" ).Add( count, ObjectiveColors.Reward ).Add( DescribeTargets( kind, count ) )
                .Add( planet.Name, planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter )
                .Add( "总共将获得" );
            switch ( kind )
            {
                case DestructionResourceKind.Hacking:
                    buffer.Add( hacking, ObjectiveColors.Reward ).Add( "入侵点数（HaP）。" );
                    break;
                case DestructionResourceKind.ScienceAndHacking:
                    buffer.Add( science, ObjectiveColors.Reward ).Add( "科技和" ).Add( hacking, ObjectiveColors.Reward ).Add( "入侵点数（HaP）。" );
                    break;
                case DestructionResourceKind.Essence:
                    buffer.AddResourceOne_MoreReadable( essence, true );
                    break;
                default:
                    buffer.Add( science, ObjectiveColors.Reward ).Add( "科技。" );
                    break;
            }

            AppendKillBreakdown( buffer, kind );

            if ( aip > 0 )
                buffer.Add( "\n\n摧毁全部也会使AI进程增加" ).Add( aip, ObjectiveColors.AIP ).Add( "。" );
        }

        private static void AccumulateKill( GameEntityTypeData type, long weight )
        {
            int c;
            killCounts.TryGetValue( type, out c );
            killCounts[type] = c + 1;
            long w;
            killWeights.TryGetValue( type, out w );
            killWeights[type] = w + weight;
        }

        // Lists the top (up to 4) ship types by total resource granted, with icon, count, and amount.
        private static void AppendKillBreakdown( ArcenDoubleCharacterBuffer buffer, DestructionResourceKind kind )
        {
            if ( killCounts.Count == 0 )
                return;
            foreach ( KeyValuePair<GameEntityTypeData, int> pair in killCounts )
                killTypes.Add( pair.Key );
            killTypes.Sort( static delegate ( GameEntityTypeData a, GameEntityTypeData b ) { return killWeights[b].CompareTo( killWeights[a] ); } );

            Faction viewing = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( viewing == null )
                viewing = World_AIW2.Instance.GetFirstPlayerFactionOrNull();

            int shown = Math.Min( 4, killTypes.Count );
            buffer.Add( "\n\n" ).Add( "最佳目标：", ObjectiveColors.Header );
            for ( int i = 0; i < shown; i++ )
            {
                GameEntityTypeData type = killTypes[i];
                int c = killCounts[type];
                buffer.Add( "\n  " );
                if ( viewing != null )
                    buffer.AddShipIconInline( type, viewing, TextStyle.Ship_Sprite_Ency ).Add( "  " );
                buffer.Add( "脳" + c, ObjectiveColors.Muted ).Add( " " ).Add( type.GetDisplayName(), ObjectiveColors.Keyword ).Add( ": " );
                AppendKillAmount( buffer, type, c, kind );
            }
            if ( killTypes.Count > shown )
                buffer.Add( "\n  " ).Add( "……以及另外" + ( killTypes.Count - shown ) + "种类型", ObjectiveColors.Muted );
        }

        private static void AppendKillAmount( ArcenDoubleCharacterBuffer buffer, GameEntityTypeData type, int count, DestructionResourceKind kind )
        {
            switch ( kind )
            {
                case DestructionResourceKind.Hacking:
                    buffer.Add( count * type.HackingToGrantOnDeath, ObjectiveColors.Reward ).Add( " HaP" );
                    break;
                case DestructionResourceKind.ScienceAndHacking:
                    buffer.Add( count * type.ScienceToGrantOnDeath, ObjectiveColors.Reward ).Add( " science, " )
                        .Add( count * type.HackingToGrantOnDeath, ObjectiveColors.Reward ).Add( " HaP" );
                    break;
                case DestructionResourceKind.Essence:
                    FInt e;
                    killEssence.TryGetValue( type, out e );
                    buffer.AddResourceOne_MoreReadable( e, true );
                    break;
                default:
                    buffer.Add( count * type.ScienceToGrantOnDeath, ObjectiveColors.Reward ).Add( " science" );
                    break;
            }
        }

        private static string DescribeTargets( DestructionResourceKind kind, int count )
        {
            bool one = count == 1;
            switch ( kind )
            {
                case DestructionResourceKind.Hacking:
                    return one ? "个提供HaP的目标位于" : "个提供HaP的目标位于";
                case DestructionResourceKind.ScienceAndHacking:
                    return one ? "个目标位于" : "个目标位于";
                case DestructionResourceKind.Essence:
                    return one ? "个提供精华的目标位于" : "个提供精华的目标位于";
                default:
                    return one ? "个提供科技的目标位于" : "个提供科技的目标位于";
            }
        }

        // Shared "how many do I get" note for the command-station defensive-grant tooltips (GCA / TSS / ODSS).
        // "military"/"economic" are colored as category accents (martial red, economic blue) rather than
        // via the semantic palette, the same way faction-accent colors stay as literals.
        private const string MilitaryStationColor = "ff6b6b";
        private const string EconomicStationColor = "78c8ff";
        internal static void AppendDefensiveCapMultiplierNote( ArcenDoubleCharacterBuffer buffer )
        {
            buffer.Add( "\n\n" ).Add( "军事", MilitaryStationColor ).Add( "指挥站获得最多，" )
                .Add( "经济", EconomicStationColor ).Add( "指挥站获得最少。" );
            buffer.Add( "\n<size=85%>" ).Add( "（按每个指挥站的防御建筑容量倍数缩放）", ObjectiveColors.Muted ).Add( "</size>" );
        }
    }

    public class CaptureZenithPowerGenerator : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "天顶能量发电机", ObjectiveColors.Keyword ).Add( "（ZPG）在占领后提供大量" ).Add( "能量", ObjectiveColors.Reward ).Add( "。占领并守住它。" );
        }
    }

    public class CaptureZenithMatterConverter : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "天顶物质转换器", ObjectiveColors.Keyword ).Add( "（ZMC）在占领后提供大量" ).Add( "金属", ObjectiveColors.Reward ).Add( "。占领并守住它。" );
        }
    }

    public class CaptureGCA : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            GameEntity_Squad entity = Objective.RelatedEntity1;
            if ( entity != null )
                buffer.AddObjectiveEntityHeader( entity, entity.GetFactionCenterColorHexBrighter_Safe() );
            buffer.Add( "占领它让你所有的指挥站都能建造" ).Add( "更多防御建筑", ObjectiveColors.Keyword ).Add( "，如下所列。" );
            ResourceObjectivesGenerator.AppendDefensiveCapMultiplierNote( buffer );
            if ( Objective.RelatedEntity1 == null || Objective.RelatedEntity1.ShipGrantsList.Count <= 0 )
                buffer.Add( "\n\n这个没有提供任何炮塔或其他建筑！原因不明。（这是一个 bug，请用存档报告。）" );
            else
            {
                buffer.Add( "\n\n" ).Add( "<color=#8092ff>" ).Add( Objective.RelatedEntity1.ShipGrantsList.Count ).Add( "</color>" ).Add( " 已获得的结构" );
                if ( Objective.Hook.Category != null && Objective.Hook.Category.IsFilteredUnitAquisitionObjective )
                    buffer.Add( ", including" );
                buffer.Add( ":\n" );

                ShipLineEntry entry = null;
                Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( playerFaction == null )
                    playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                for ( int i = 0; i < Objective.RelatedEntity1.ShipGrantsList.Count; i++ )
                {
                    entry = Objective.RelatedEntity1.ShipGrantsList[i];
                    if ( Objective.Hook.Category != null && !AIObjectivesGenerator.GetEntityMatchesObjectiveCategory( entry.TypeData, Objective.Hook.Category ) )
                        continue;

                    int entitiesGranted = entry.GetNumShipsForHackAndHacker( null, null );
                    byte mark = playerFaction.GetGlobalMarkLevelForShipLine( entry.TypeData );
                    GameEntityTypeData.MarkLevelStats markStats = entry.TypeData.MarkStatsFor( mark );
                    
                    buffer.AddShipIconInline(entry.TypeData, playerFaction, TextStyle.Ship_Sprite_Ency).Add( "  " );
                    buffer.Add( "<color=#" ).Add( markStats.MarkLevel.ColorHex ).Add( ">" ).Add( entry.TypeData == null ? "null" : entry.TypeData.GetDisplayName() ).Add( "</color>" );
                    buffer.Add( " x" ).Add( "<color=#" ).Add( entry.GetColorForShipLineScore() ).Add( ">" ).Add( entitiesGranted ).Add( "</color>" );
                    buffer.Add("\n");
                    List<TechUpgrade> techsForLine = entry.TypeData.TechUpgradesThatBenefitMe;
                    if ( techsForLine != null && techsForLine.Count > 0 )
                    {
                        buffer.Add( "         Techs: ", "ffeecc" );
                        for ( int t = 0; t < techsForLine.Count; t++ )
                        {
                            if ( t > 0 ) buffer.Add( ", " );
                            TechUpgrade tech = techsForLine[t];
                            int upgradesSoFar = playerFaction == null ? 0 : playerFaction.TechUnlocks[tech.RowIndexNonSim] + playerFaction.FreeTechUnlocks[tech.RowIndexNonSim];
                            int upgradeIndex = upgradesSoFar < 0 ? 0 : upgradesSoFar + 1;
                            if ( upgradeIndex >= Balance_MarkLevelTable.Instance.RowsByOrdinal.Length )
                                upgradeIndex = Balance_MarkLevelTable.Instance.RowsByOrdinal.Length - 1;
                            buffer.Add( tech.DisplayName, Balance_MarkLevelTable.Instance.RowsByOrdinal[upgradeIndex].ColorHex );
                        }
                        buffer.Add( "\n" );
                    }
                    buffer.Add( "\n" );
                }
            }
        }
    }

    public class CaptureDSS : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedEntity1 != null )
                buffer.AddObjectiveEntityHeader( Objective.RelatedEntity1, Objective.RelatedEntity1.GetFactionCenterColorHexBrighter_Safe() );
            buffer.Add( "入侵它解锁一种新的" )
                .Add( Objective.RelatedEntity1.TypeData.HackableForCommandStationsAndBattleStations_TurretCount > 0 ? "炮塔线" : "防御线", ObjectiveColors.Keyword )
                .Add( "，你所有的指挥站、战斗空间站和堡垒都可以建造。" );
            ResourceObjectivesGenerator.AppendDefensiveCapMultiplierNote( buffer );
            if ( Objective.RelatedEntity1 == null || Objective.RelatedEntity1.ShipGrantsList.Count <= 0 )
                buffer.Add( "\n\n这个没有提供任何炮塔或其他防御物品！原因不明。（这是一个 bug，请用存档报告。）" );
            else
            {
                buffer.Add( "\n\n" ).Add( "<color=#8092ff>" ).Add( Objective.RelatedEntity1.ShipGrantsList.Count ).Add( "</color>" ).Add( " 防御线可供获取" );
                if ( Objective.Hook.Category != null && Objective.Hook.Category.IsFilteredUnitAquisitionObjective )
                    buffer.Add( ", including" );
                buffer.Add( ":\n" );
                ShipLineEntry entry = null;
                Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( playerFaction == null )
                    playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
                for ( int i = 0; i < Objective.RelatedEntity1.ShipGrantsList.Count; i++ )
                {
                    entry = Objective.RelatedEntity1.ShipGrantsList[i];
                    if ( Objective.Hook.Category != null && !AIObjectivesGenerator.GetEntityMatchesObjectiveCategory( entry.TypeData, Objective.Hook.Category ) )
                        continue;

                    int entitiesGranted = entry.GetNumShipsForHackAndHacker( null, null );
                    byte mark = playerFaction.GetGlobalMarkLevelForShipLine( entry.TypeData );
                    GameEntityTypeData.MarkLevelStats markStats = entry.TypeData.MarkStatsFor( mark );

                    buffer.AddShipIconInline(entry.TypeData, playerFaction, TextStyle.Ship_Sprite_Ency).Add( "  " );
                    buffer.Add( "<color=#" ).Add( markStats.MarkLevel.ColorHex ).Add( ">" ).Add( entry.TypeData == null ? "null" : entry.TypeData.GetDisplayName() ).Add( "</color>" );
                    buffer.Add( " x" ).Add( "<color=#" ).Add( entry.GetColorForShipLineScore() ).Add( ">" ).Add( entitiesGranted ).Add( "</color>" );
                    buffer.Add("\n");
                    List<TechUpgrade> techsForLine = entry.TypeData.TechUpgradesThatBenefitMe;
                    if ( techsForLine != null && techsForLine.Count > 0 )
                    {
                        buffer.Add( "         Techs: ", "ffeecc" );
                        for ( int t = 0; t < techsForLine.Count; t++ )
                        {
                            if ( t > 0 ) buffer.Add( ", " );
                            TechUpgrade tech = techsForLine[t];
                            int upgradesSoFar = playerFaction == null ? 0 : playerFaction.TechUnlocks[tech.RowIndexNonSim] + playerFaction.FreeTechUnlocks[tech.RowIndexNonSim];
                            int upgradeIndex = upgradesSoFar < 0 ? 0 : upgradesSoFar + 1;
                            if ( upgradeIndex >= Balance_MarkLevelTable.Instance.RowsByOrdinal.Length )
                                upgradeIndex = Balance_MarkLevelTable.Instance.RowsByOrdinal.Length - 1;
                            buffer.Add( tech.DisplayName, Balance_MarkLevelTable.Instance.RowsByOrdinal[upgradeIndex].ColorHex );
                        }
                        buffer.Add( "\n" );
                    }
                    buffer.Add( "\n" );
                }
            }
        }
    }

    public class HackSpireArchive : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "入侵尖塔档案将获得大量科技，但如果入侵失败，将产生大量AIP。" );
        }
    }

    public class AcquireScienceByDestruction : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            string color = Objective.RelatedEntity1.GetFactionCenterColorHexBrighter_Safe();
            buffer.Add( "摧毁此" ).Add( Objective.RelatedEntity1.TypeData.GetDisplayName(), color ).Add( "将获得" ).Add( Objective.RelatedEntity1.TypeData.ScienceToGrantOnDeath, ObjectiveColors.Reward ).Add( "科技。" );
            if ( Objective.RelatedEntity1.TypeData.AIPOnDeath > 0 )
                buffer.Add( "但摧毁它也会使AI进程增加" ).Add( Objective.RelatedEntity1.TypeData.AIPOnDeath, ObjectiveColors.AIP ).Add( "。" );
        }
    }
    public class AcquireEssenceByDestruction : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            string color = Objective.RelatedEntity1.GetFactionCenterColorHexBrighter_Safe();
            DLC3GameEntityTypeDataExtension entity_DLC3TypeData = Objective.RelatedEntity1.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );

            entity_DLC3TypeData.GetNecromancerResourcesToGrantOnDeath(Objective.RelatedEntity1,
                    out _, out _, out FInt essence);

            buffer.Add( "摧毁此" ).Add( Objective.RelatedEntity1.TypeData.GetDisplayName(), color ).Add( "将获得" ).AddResourceOne_MoreReadable( essence, true );
            if ( Objective.RelatedEntity1.TypeData.AIPOnDeath > 0 )
                buffer.Add( "但摧毁它也会使AI进程增加" ).Add( Objective.RelatedEntity1.TypeData.AIPOnDeath, ObjectiveColors.AIP ).Add( "。" );
        }
    }
    public class AcquireHackingByDestruction : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "摧毁此" ).Add( Objective.RelatedEntity1.TypeData.GetDisplayName(), Objective.RelatedEntity1.GetFactionCenterColorHexBrighter_Safe() ).Add( "将获得" ).Add( Objective.RelatedEntity1.TypeData.HackingToGrantOnDeath, ObjectiveColors.Reward ).Add( "入侵点数（HaP）。" );
            if ( Objective.RelatedEntity1.TypeData.AIPOnDeath > 0 )
                buffer.Add( "但摧毁它也会使AI进程增加" ).Add( Objective.RelatedEntity1.TypeData.AIPOnDeath, ObjectiveColors.AIP ).Add( "。" );
        }
    }

    public class AcquireScienceAndHackingByDestruction : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            string color = Objective.RelatedEntity1.GetFactionCenterColorHexBrighter_Safe();
            buffer.Add( "摧毁此" ).Add( Objective.RelatedEntity1.TypeData.GetDisplayName(), color ).Add( "将获得" ).Add( Objective.RelatedEntity1.TypeData.ScienceToGrantOnDeath, ObjectiveColors.Reward ).Add( "科技和" ).Add( Objective.RelatedEntity1.TypeData.HackingToGrantOnDeath, ObjectiveColors.Reward ).Add( "入侵点数（HaP）。" );
            if ( Objective.RelatedEntity1.TypeData.AIPOnDeath > 0 )
                buffer.Add( "但摧毁它也会使AI进程增加" ).Add( Objective.RelatedEntity1.TypeData.AIPOnDeath, ObjectiveColors.AIP ).Add( "。" );
        }
    }

    // Per-planet aggregated variants of the by-destruction objectives. One objective covers every
    // reward unit on a planet; the shared renderer sums live totals and lists the best ship targets.
    public class AcquireScienceByDestructionOnPlanet : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedPlanet1 == null ) { buffer.Add( "Bug in AcquireScienceByDestructionOnPlanet: null planet" ); return; }
            ResourceObjectivesGenerator.AppendDestructionTooltip( buffer, Objective.RelatedPlanet1, ResourceObjectivesGenerator.DestructionResourceKind.Science );
        }
    }

    public class AcquireHackingByDestructionOnPlanet : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedPlanet1 == null ) { buffer.Add( "Bug in AcquireHackingByDestructionOnPlanet: null planet" ); return; }
            ResourceObjectivesGenerator.AppendDestructionTooltip( buffer, Objective.RelatedPlanet1, ResourceObjectivesGenerator.DestructionResourceKind.Hacking );
        }
    }

    public class AcquireScienceAndHackingByDestructionOnPlanet : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedPlanet1 == null ) { buffer.Add( "Bug in AcquireScienceAndHackingByDestructionOnPlanet: null planet" ); return; }
            ResourceObjectivesGenerator.AppendDestructionTooltip( buffer, Objective.RelatedPlanet1, ResourceObjectivesGenerator.DestructionResourceKind.ScienceAndHacking );
        }
    }

    public class AcquireEssenceByDestructionOnPlanet : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedPlanet1 == null ) { buffer.Add( "Bug in AcquireEssenceByDestructionOnPlanet: null planet" ); return; }
            ResourceObjectivesGenerator.AppendDestructionTooltip( buffer, Objective.RelatedPlanet1, ResourceObjectivesGenerator.DestructionResourceKind.Essence );
        }
    }

    public class AcquireScienceFromPlanet : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "有" ).Add( Objective.RelatedInt1, ObjectiveColors.Reward )
                .Add( "剩余科技可在" ).Add( Objective.RelatedPlanet1.Name, Objective.RelatedPlanet1.GetControllingFaction().FactionCenterColor.ColorHexBrighter )
                .Add( "上收集。在此建造并守住指挥站直到全部收集完毕。你也可以通过入侵中立星球来收集科技。" );
        }
    }

    public class AcquireHackingFromPlanet : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "有" ).Add( Objective.RelatedInt1, ObjectiveColors.Reward ).Add( "剩余入侵点数（HaP）可在" )
                .Add( Objective.RelatedPlanet1.Name, Objective.RelatedPlanet1.GetControllingFaction().FactionCenterColor.ColorHexBrighter )
                .Add( "上收集。在此建造并守住指挥站直到全部收集完毕。不幸的是，你不能通过入侵星球来获得更多入侵点数……" );

        }
    }

    public class HackTechVault : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        private static readonly SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark> shipsThatBenefit_ThatYouHave = 
            SortedDictionaryOfCustomPooledData<GameEntityTypeData, ShipDataByMark>.Create_WillNeverBeGCed( 30, delegate { return new ShipDataByMark(); }, "ResourceObjectivesGenerator-HackTechVault-shipsThatBenefit_ThatYouHave" );
        private static float techUpgrade_Ships_ThatYouHave_NextTime = 0;

        private static ArcenDoubleCharacterBuffer CachedBuffer = new ArcenDoubleCharacterBuffer( "ResourceObjectiveGenerator-HackTechVault-tooltip" );
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            string output = "";
            if ( ArcenTime.TimeSinceStartF < techUpgrade_Ships_ThatYouHave_NextTime )
            {
                output = CachedBuffer.GetStringAndResetForNextUpdate();
                CachedBuffer.Add( output );
                buffer.Add( output );
                return;
            }
            
            techUpgrade_Ships_ThatYouHave_NextTime = ArcenTime.TimeSinceStartF + Engine_Universal.PermanentQualityRandom.NextFloat( 0.8f, 1.2f );

            Faction playerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( playerFaction == null )
                playerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();

                    
            List<TechUpgrade> upgrades = TechUpgrade.GetTemporaryTechUpgradeList( "ResourceObjectivesGenerator-HackTechVault-upgrades", 10f );
            if ( upgrades == null ) //blocked for teardown/shutdown; bail
                return;
            HackingUtils.CalculateListOfTechs_TechVault( upgrades, Objective.RelatedEntity1, playerFaction );
            buffer.Add( "入侵此" + Objective.RelatedEntity1.TypeData.GetDisplayName() + "将获得以下升级之一：\n");
            for ( int i = 0; i < upgrades.Count; i++ )
            {
                
                {
                    ShipListerUtils.CalculateShipsThatYouHave( //this function clears the shipsThatBenefit_ThatYouHave list each time
                        mem => mem.TypeData.TechUpgradesThatBenefitMe.Contains( upgrades[i] ),
                        shipsThatBenefit_ThatYouHave, true, true
                        );
                }
                int shipLinesForThisTech = shipsThatBenefit_ThatYouHave.GetCountOfLists();
                string shipListStr = "";
                bool isFirst = true;
                foreach ( KeyValuePair<GameEntityTypeData, ShipDataByMark> pair in shipsThatBenefit_ThatYouHave )
                {
                    for ( byte mark = 0; mark < pair.Value.CountsByMarkLength(); mark++ )
                    {
                        int count = pair.Value.GetCountByMark( mark );
                        if ( count <= 0 )
                            continue;
                        GameEntityTypeData.MarkLevelStats markStats = pair.Key.MarkStatsFor( mark );
                        if ( isFirst )
                            isFirst = false;
                        else
                            shipListStr += ", ";
                        shipListStr += "<color=#" + markStats.MarkLevel.ColorHex + ">" + pair.Key.GetDisplayName()
                            + " " + markStats.MarkLevel.MapDisplay + "</color>";
                    }
                }
                buffer.Add( "+1 ", "ffa1a1").Add(" <color=#a1ffa1>" + upgrades[i].DisplayName + "</color>\n");
                buffer.Add("<size=80%>\t你当前有 <color=#a1ffa1>" + shipLinesForThisTech + "</color> 条舰船线将从中受益。" );
                if ( shipLinesForThisTech > 1 )
                    buffer.Add( "\n\t\t这些舰船是 " + shipListStr );
                else if ( shipLinesForThisTech == 1 )
                    buffer.Add( "\n\t\t该舰船是 " + shipListStr );
                buffer.Add("</size>\n");
            }
            output = buffer.GetStringAndResetForNextUpdate();
            CachedBuffer.ResetForNextUpdate();
            CachedBuffer.Add( output );
            buffer.Add( output );
            TechUpgrade.ReleaseTemporaryTechUpgradeList( upgrades );
        }
    }

    public class ClaimSocketIncreaser : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedEntity1 == null )
            {
                buffer.Add( "Bug in ClaimSocketIncreaser: null entity" );
                return;
            }
            bool isMajor = Objective.RelatedEntity1.TypeData.GetHasTag( "MajorSocketIncreaser" );
            buffer.AddShipIconInline( Objective.RelatedEntity1, TextStyle.Ship_Sprite_Ency ).Add( " " )
                .Add( isMajor ? "主要熔炉" : "次要熔炉", ObjectiveColors.Keyword ).Add( "\n\n" );
            buffer.Add( "位于" ).Add( Objective.RelatedEntity1.GetPlanetName_Safe(), ObjectiveColors.Reward ).Add( "。\n\n" );
            buffer.Add( "入侵它以在该星球上获得额外的" ).Add( "建筑插槽", ObjectiveColors.Reward )
                .Add( "，让你在那里建造更多" ).Add( "建筑", ObjectiveColors.Keyword ).Add( "。" );
            if ( isMajor )
                buffer.Add( "  " ).Add( "主要熔炉", ObjectiveColors.Keyword ).Add( "比次要熔炉提供更多插槽。" );
        }
    }

}
