using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class ApkalluObjectivesGenerator
    {
        public static void CheckForApkalluObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( !ApkalluFactionBaseInfo.GetIsThisAnApkalluFaction( localFaction ) )
                    return;
                GenerateApkalluJourneyObjectives();
                GenerateDivesObjectives();
                GenerateConvergenceObjectives();
                GenerateZigguratObjectives();
                GenerateFlagshipProgressionObjectives();
                GenerateInfrastructureObjectives();
                GenerateResourceIncomeObjective();
                // GenerateZenithPowerGeneratorObjectives();
                // GenerateZenithMatterConverterObjectives();
                // GenerateGrantsAddedToCommandStationObjectives();
                // GenerateSpireArchiveObjectives();
                // GenerateAcquireHackingObjectives();
                // GenerateAcquireScienceByDestructionObjectives();
                // GenerateAcquireHackingByDestructionObjectives();
                // GenerateAcquireTechObjectives();
                // GenerateAcquireScienceAndHackingByDestructionObjectives();            
            }
            catch (Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in ApkalluObjectivesGenerator generation: " + e, Verbosity.ShowAsError );
            }
        }

        private static Faction GetApkalluPlayerFaction()
        {
            Faction f = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( f == null || !ApkalluFactionBaseInfo.GetIsThisAnApkalluFaction( f ) )
                return null;
            return f;
        }

        private static void GenerateApkalluJourneyObjectives()
        {
            Faction playerFaction = GetApkalluPlayerFaction();
            if ( playerFaction == null )
                return;
            ApkalluFactionBaseInfo info = playerFaction.GetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if ( info == null )
                return;

            int zigguratCount = 0;
            foreach ( GameEntity_Squad _ in info.Ziggurats.DisplaySquads() )
                zigguratCount++;

            if ( zigguratCount == 0 )
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "AnchorTheApkallu" );
                ObjectiveCategory.AddActualObjective( obj );
            }
            else if ( zigguratCount == 1 )
            {
                ActualObjective obj1 = ActualObjective.GetFromPoolOrCreate();
                obj1.SetHook( "ResourcesOfDeep" );
                ObjectiveCategory.AddActualObjective( obj1 );
                ActualObjective obj2 = ActualObjective.GetFromPoolOrCreate();
                obj2.SetHook( "ReclaimDepths" );
                ObjectiveCategory.AddActualObjective( obj2 );
                ActualObjective obj3 = ActualObjective.GetFromPoolOrCreate();
                obj3.SetHook( "RallyScatteredFleet" );
                ObjectiveCategory.AddActualObjective( obj3 );
            }
            else if ( zigguratCount == 2 )
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "UncoverInvasion" );
                ObjectiveCategory.AddActualObjective( obj );
                ActualObjective obj2 = ActualObjective.GetFromPoolOrCreate();
                obj2.SetHook( "RallyScatteredFleet" );
                ObjectiveCategory.AddActualObjective( obj2 );
            }
            else
            {
                ActualObjective obj = ActualObjective.GetFromPoolOrCreate();
                obj.SetHook( "BuildDestabilizer" );
                ObjectiveCategory.AddActualObjective( obj );
                ActualObjective obj2 = ActualObjective.GetFromPoolOrCreate();
                obj2.SetHook( "RallyScatteredFleet" );
                ObjectiveCategory.AddActualObjective( obj2 );
            }
        }

        private static void GenerateDivesObjectives()
        {
            Faction malware = FactionUtilityMethods.Instance.GetMalwareForApkalluFaction();
            if ( malware == null )
                return;
            MalwareFactionBaseInfo mBaseInfo = malware.GetExternalBaseInfoAs<MalwareFactionBaseInfo>();
            if ( mBaseInfo == null )
                return;

            foreach ( GameEntity_Squad nexus in mBaseInfo.Nexuses.DisplaySquads() )
            {
                if ( !nexus.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "BreachMalwareNexus" );
                objective.RelatedEntity1 = nexus;
                objective.RelatedPlanet1 = nexus.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }

            foreach ( GameEntity_Squad fissure in mBaseInfo.Fissures.DisplaySquads() )
            {
                if ( !fissure.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "MalwareFissure" );
                objective.RelatedEntity1 = fissure;
                objective.RelatedPlanet1 = fissure.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateConvergenceObjectives()
        {
            Faction malware = FactionUtilityMethods.Instance.GetMalwareForApkalluFaction();
            if ( malware == null )
                return;
            MalwareFactionBaseInfo mBaseInfo = malware.GetExternalBaseInfoAs<MalwareFactionBaseInfo>();
            if ( mBaseInfo == null )
                return;

            // Convergence is only "live" while the countdown is running or the staggered
            // main strike is still pending. Outside that window there is nothing to show.
            bool countingDown = mBaseInfo.ConvergenceCountdownEndTime != -1;
            bool mainStrikePending = mBaseInfo.ConvergenceMainStrikeTime != -1;
            if ( !countingDown && !mainStrikePending )
                return;

            // Always-shown header so the player learns about the threat even if no
            // Phasic generator is on an explored planet yet.
            ActualObjective header = ActualObjective.GetFromPoolOrCreate();
            header.SetHook( "MalwareConvergence" );
            ObjectiveCategory.AddActualObjective( header );

            // Per-generator entries (only while counting down; generators are despawned
            // once the final wave launches) so the player can route to and kill them.
            if ( !countingDown )
                return;
            foreach ( GameEntity_Squad generator in mBaseInfo.ConvergenceGenerators.DisplaySquads() )
            {
                if ( generator == null )
                    continue;
                if ( !generator.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "DestroyPhasicGenerator" );
                objective.RelatedEntity1 = generator;
                objective.RelatedPlanet1 = generator.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateZigguratObjectives()
        {
            Faction playerFaction = GetApkalluPlayerFaction();
            if ( playerFaction == null )
                return;
            ApkalluFactionBaseInfo info = playerFaction.GetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if ( info == null )
                return;

            Faction malware = FactionUtilityMethods.Instance.GetMalwareForApkalluFaction();
            if ( malware == null )
                return;
            MalwareFactionBaseInfo mBaseInfo = malware.GetExternalBaseInfoAs<MalwareFactionBaseInfo>();
            if ( mBaseInfo == null )
                return;

            int claimedCount = 0;
            foreach ( GameEntity_Squad _ in info.Ziggurats.DisplaySquads() )
                claimedCount++;

            bool anyVisible = false;
            foreach ( GameEntity_Squad ziggurat in mBaseInfo.CorruptedZiggurats.DisplaySquads() )
            {
                if ( !ziggurat.GetShouldBeVisibleBasedOnPlanetIntel() )
                    continue;
                anyVisible = true;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "CorruptZiggurat" );
                objective.RelatedPlanet1 = ziggurat.Planet;
                ObjectiveCategory.AddActualObjective( objective );
            }

            if ( !anyVisible && claimedCount < 3 )
            {
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "FindZiggurat" );
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateFlagshipProgressionObjectives()
        {
            Faction playerFaction = GetApkalluPlayerFaction();
            if ( playerFaction == null )
                return;
            ApkalluFactionBaseInfo info = playerFaction.GetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if ( info == null )
                return;

            int zigguratCount = 0;
            foreach ( GameEntity_Squad _ in info.Ziggurats.DisplaySquads() )
                zigguratCount++;

            bool hasT2 = false;
            bool hasT3 = false;
            bool hasMarkOneFlagship = false;
            foreach ( GameEntity_Squad flagship in info.Flagships.DisplaySquads() )
            {
                if ( flagship.TypeData.GetHasTag( "TierTwo" ) )
                    hasT2 = true;
                if ( flagship.TypeData.GetHasTag( "TierThree" ) )
                    hasT3 = true;
                if ( flagship.CurrentMarkLevel == 1 )
                    hasMarkOneFlagship = true;
            }

            if ( zigguratCount >= 1 && !hasT2 && !hasT3 )
            {
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "FlagshipProgressionT2" );
                ObjectiveCategory.AddActualObjective( objective );
            }

            if ( zigguratCount >= 2 && !hasT3 )
            {
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "FlagshipProgressionT3" );
                ObjectiveCategory.AddActualObjective( objective );
            }

            if ( hasMarkOneFlagship )
            {
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "UpgradeFlagship" );
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateInfrastructureObjectives()
        {
            Faction playerFaction = GetApkalluPlayerFaction();
            if ( playerFaction == null )
                return;
            ApkalluFactionBaseInfo info = playerFaction.GetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if ( info == null )
                return;

            foreach ( GameEntity_Squad duru in info.Durus.DisplaySquads() )
            {
                if ( duru.FleetMembership == null || duru.FleetMembership.Fleet == null )
                    continue;
                if ( duru.FleetMembership.Fleet.CalculateRemainingCitySockets() >= 1 )
                {
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "ApkalluUnusedSockets" );
                    objective.RelatedPlanet1 = duru.Planet;
                    ObjectiveCategory.AddActualObjective( objective );
                }
            }

            foreach ( GameEntity_Squad lamassu in info.Lamassus.DisplaySquads() )
            {
                // Sockets belong to the Ziggurat fleet, not the Lamassu 鈥?find the owner Ziggurat.
                GameEntity_Squad ownerZiggurat = null;
                foreach ( GameEntity_Squad ziggurat in info.Ziggurats.DisplaySquads() )
                {
                    ApkalluPerUnitBaseInfo zigUnit = ziggurat.TryGetExternalBaseInfoAs<ApkalluPerUnitBaseInfo>();
                    if ( zigUnit != null && zigUnit.LamassuEntityPrimaryKeyID == lamassu.PrimaryKeyID )
                    {
                        ownerZiggurat = ziggurat;
                        break;
                    }
                }
                if ( ownerZiggurat == null || ownerZiggurat.FleetMembership == null || ownerZiggurat.FleetMembership.Fleet == null )
                    continue;
                if ( ownerZiggurat.FleetMembership.Fleet.CalculateRemainingCitySockets() >= 1 )
                {
                    ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                    objective.SetHook( "LamassuUnusedSockets" );
                    objective.RelatedEntity1 = lamassu;
                    objective.RelatedPlanet1 = lamassu.Planet;
                    ObjectiveCategory.AddActualObjective( objective );
                }
            }

            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "ApkalluTemen" ) )
            {
                if ( entity.Planet.IntelLevel <= PlanetIntelLevel.Unexplored )
                    continue;
                if ( entity.PlanetFaction.Faction == playerFaction )
                    continue;
                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                objective.SetHook( "ClaimTemen" );
                objective.RelatedEntity1 = entity;
                ObjectiveCategory.AddActualObjective( objective );
            }
        }

        private static void GenerateResourceIncomeObjective()
        {
            ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
            objective.SetHook( "ApkalluResourceIncome" );
            ObjectiveCategory.AddActualObjective( objective );
        }
    }


    public class BreachMalwareNexus : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedEntity1 == null )
            {
                buffer.Add( "Bug in BreachMalwareNexus: null entity" );
                return;
            }
            try
            {
                buffer.AddObjectiveEntityHeader( Objective.RelatedEntity1, Objective.RelatedEntity1.GetFactionCenterColorHexBrighter_Safe() );
                buffer.Add( "在" ).Add( Objective.RelatedEntity1.Planet?.Name ?? "?", ObjectiveColors.Reward ).Add( "上，它正在从阿普苏召入增援。通过入侵突破枢纽将允许阿普卡卢短暂潜入阿普苏并对恶意软件发起打击。\n\n" );

                MalwarePerUnitBaseInfo data = Objective.RelatedEntity1.TryGetExternalBaseInfoAs<MalwarePerUnitBaseInfo>();
                if ( data == null || data.Breaches.Count == 0 )
                    return;

                buffer.Add( "可用突破：\n", ObjectiveColors.Hint );
                for ( int i = 0; i < data.Breaches.Count; i++ )
                {
                    MalwareBreach breach = data.Breaches[i];
                    buffer.Add( "\t" ).Add( breach.DisplayName, "ffffff" );
                    if ( breach.Difficulty != MalwareBreachDifficulty.None )
                        buffer.Add( "  [" ).Add( breach.Difficulty.ToString(), "ffaaaa" ).Add( "]" );
                    if ( breach.UnlockFactionResource != ApkalluFactionResource.None )
                        buffer.Add( "  鈫? ", ObjectiveColors.Muted ).Add( breach.UnlockFactionResource.ToString(), ObjectiveColors.Reward );
                    else if ( breach.UnlockZigguratStructure != null )
                        buffer.Add( "  鈫? ", ObjectiveColors.Muted ).Add( breach.UnlockZigguratStructure.DisplayName, ObjectiveColors.Reward );
                    else if ( breach.UnlockDuruStructures.Count > 0 )
                        buffer.Add( "  鈫? ", ObjectiveColors.Muted ).Add( breach.UnlockDuruStructures[0].DisplayName, ObjectiveColors.Reward );
                    buffer.Add( "\n" );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in BreachMalwareNexus.TooltipHandler: " + e, Verbosity.ShowAsError );
            }
        }
    }

    public class MalwareFissure : IObjectiveHookManager
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
                buffer.Add( "Bug in MalwareFissure: null entity" );
                return;
            }
            try
            {
                buffer.AddObjectiveEntityHeader( Objective.RelatedEntity1, Objective.RelatedEntity1.GetFactionCenterColorHexBrighter_Safe() );
                buffer.Add( "在" ).Add( Objective.RelatedEntity1.Planet?.Name ?? "?", ObjectiveColors.Reward ).Add( "上，它正在让恶意软件从阿普苏召入增援。\n\n通过入侵突破它将允许你潜入阿普苏并对恶意软件发起打击。\n\n" );

                MalwarePerUnitBaseInfo data = Objective.RelatedEntity1.TryGetExternalBaseInfoAs<MalwarePerUnitBaseInfo>();
                if ( data == null || data.Breaches.Count == 0 )
                    return;

                buffer.Add( "可用突破：\n", ObjectiveColors.Hint );
                for ( int i = 0; i < data.Breaches.Count; i++ )
                {
                    MalwareBreach breach = data.Breaches[i];
                    buffer.Add( "\t" ).Add( breach.DisplayName, "ffffff" );
                    if ( breach.Difficulty != MalwareBreachDifficulty.None )
                        buffer.Add( "  [" ).Add( breach.Difficulty.ToString(), "ffaaaa" ).Add( "]" );
                    if ( breach.UnlockFactionResource != ApkalluFactionResource.None )
                        buffer.Add( "  鈫? ", ObjectiveColors.Muted ).Add( breach.UnlockFactionResource.ToString(), ObjectiveColors.Reward );
                    else if ( breach.UnlockZigguratStructure != null )
                        buffer.Add( "  鈫? ", ObjectiveColors.Muted ).Add( breach.UnlockZigguratStructure.DisplayName, ObjectiveColors.Reward );
                    else if ( breach.UnlockDuruStructures.Count > 0 )
                        buffer.Add( "  鈫? ", ObjectiveColors.Muted ).Add( breach.UnlockDuruStructures[0].DisplayName, ObjectiveColors.Reward );
                    buffer.Add( "\n" );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in MalwareFissure.TooltipHandler: " + e, Verbosity.ShowAsError );
            }
        }
    }

    public class MalwareConvergence : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            try
            {
                Faction malware = FactionUtilityMethods.Instance.GetMalwareForApkalluFaction();
                MalwareFactionBaseInfo mBaseInfo = malware?.GetExternalBaseInfoAs<MalwareFactionBaseInfo>();
                if ( mBaseInfo == null )
                    return;
                int now = World_AIW2.Instance.GameSecond;

                buffer.Add( "恶意软件正在引导" ).Add( "多相位汇聚", "ff5555" )
                    .Add( "：一场通过他们在银河各处升起的相位生成器汇集的大规模打击。\n\n" );

                if ( mBaseInfo.ConvergenceCountdownEndTime != -1 )
                {
                    int remaining = mBaseInfo.ConvergenceCountdownEndTime - now;
                    if ( remaining > 0 )
                    {
                        string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( remaining );
                        buffer.Add( "汇聚在" ).Add( remaining.ToString(), color ).Add( "秒后完成。\n\n" );
                    }
                    else
                        buffer.Add( "汇聚现在即将完成！\n\n" );

                    buffer.Add( "摧毁" ).Add( "相位谐振器和导管", "ffaaaa" )
                        .Add( "以缩小来袭波次。摧毁所有则汇聚完全取消。它们单独列在下面。", ObjectiveColors.Hint );
                }
                else if ( mBaseInfo.ConvergenceMainStrikeTime != -1 )
                {
                    int remaining = mBaseInfo.ConvergenceMainStrikeTime - now;
                    buffer.Add( "汇聚波次已发射。次要打击已命中；针对你发展最完善的金字形神塔的主力冲击将在" )
                        .Add( remaining > 0 ? remaining.ToString() : "0", "ff8888" ).Add( "秒后到达。准备好你的防御。", ObjectiveColors.Hint );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in MalwareConvergence.TooltipHandler: " + e, Verbosity.ShowAsError );
            }
        }
    }

    public class DestroyPhasicGenerator : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedEntity1 == null )
            {
                buffer.Add( "Bug in DestroyPhasicGenerator: null entity" );
                return;
            }
            try
            {
                bool isConduit = Objective.RelatedEntity1.TypeData.GetHasTag( "MalwarePhasicConduit" );
                buffer.Add( isConduit ? "恶意软件相位导管" : "恶意软件相位谐振器" )
                    .Add( "位于" ).Add( Objective.RelatedEntity1.Planet?.Name ?? "?", ObjectiveColors.Reward ).Add( "。\n\n" );
                buffer.Add( isConduit
                        ? "导管提供来袭波次的最大份额。"
                        : "每个谐振器都会增加来袭波次。" );
                buffer.Add( "在汇聚完成前摧毁它，以缩小针对你金字形神塔的多相位打击。", ObjectiveColors.Hint );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception in DestroyPhasicGenerator.TooltipHandler: " + e, Verbosity.ShowAsError );
            }
        }
    }

    public class CorruptZiggurat : IObjectiveHookManager
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
            if ( Objective.RelatedPlanet1 == null )
            {
                buffer.Add( "Bug in CorruptZiggurat: null planet" );
                return;
            }
            buffer.Add( "阿普卡卢丢失的金字形神塔之一位于" ).Add( Objective.RelatedPlanet1.Name, ObjectiveColors.Reward ).Add( "，已被恶意软件占领。\n\n通过将其恢复到正常空间并击败感染，阿普卡卢将因获得更高级资源和单位而大大增强。\n\n当从独相状态带出后，金字形神塔将永久留在这个星球上。" );
        }
    }

    public class ApkalluUnusedSockets : IObjectiveHookManager
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
            if ( Objective.RelatedPlanet1 == null )
            {
                buffer.Add( "Bug in UnusedSockets: null planet" );
                return;
            }
            buffer.Add( "你的杜鲁" ).Add( Objective.RelatedPlanet1.Name, ObjectiveColors.Reward ).Add( "上有未使用的建筑插槽。\n\n在杜鲁的插槽中建造建筑可增强你的舰队并解锁新能力。" );
        }
    }

    public class ClaimTemen : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedEntity1 == null )
            {
                buffer.Add( "Bug in ClaimTemen: null entity" );
                return;
            }
            buffer.Add( "一个未认领的特门位于" ).Add( Objective.RelatedEntity1.GetPlanetName_Safe(), ObjectiveColors.Reward )
                .Add( "。入侵它将为阿普卡卢认领它，允许在那里建造建筑并提升来访朝圣者的资源。朝圣者旅行越久、访问的特门越多，获得的资源就越多。" );
        }
    }

    public class RallyScatteredFleet : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "伏击散布的不仅仅是金字形神塔。阿普卡卢舰队的一部分在打击中被困在阿普苏，与恶意软件主力舰队殊死搏斗。\n\n" );
            buffer.Add( "通过恶意软件枢纽潜入以援助他们。将他们带回正常空间将拯救他们脱离恶意软件，并让他们协助你的战斗。\n\n" );
            buffer.Add( "即使在最好的时候，它也是一支杂牌舰队。", "ffbb88" ).Add( "不是每个人都能成功。", "aa7755" );
        }
    }

    public class AnchorTheApkallu : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "阿普卡卢的贤者们遭到了恶意软件的伏击，他们的舰队被击散，金字形神塔被夺走。帮助他们生存并恢复族人。\n\n" );
            buffer.Add( "占领金字形神塔将大大增强阿普卡卢并解锁新能力。在地图上寻找被腐化的金字形神塔。", ObjectiveColors.Hint );
        }
    }

    public class ResourcesOfDeep : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "阿普卡卢远离家乡，必须在敌对的银河中维持他们的舰队。\n\n" );
            buffer.Add( "突破恶意软件裂缝是他们的主要资源来源。营救朝圣者很重要，他们会在你认领的特门之间旅行，并带着我们银河的资源返回金字形神塔。", ObjectiveColors.Hint );
        }
    }

    public class ReclaimDepths : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "第二个金字形神塔仍被恶意软件控制。阿普卡卢需要它；占领它可能揭示恶意软件在这个银河中真正在做什么。\n\n" );
            buffer.Add( "找到并认领下一个被腐化的金字形神塔。", "ffaa66" );
        }
    }

    public class UncoverInvasion : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "随着两个金字形神塔被恢复，阿普卡卢开始理解恶意软件的模式。他们不仅仅是袭击，而是在增强AI基础设施。\n\n" );
            buffer.Add( "认领第三个也是最后一个金字形神塔，以完全了解恶意软件如何进入这个银河，以及如何阻止他们。", "ff9944" );
        }
    }

    public class BuildDestabilizer : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "阿普卡卢相信去稳定器可以使星云连接（恶意软件工程化的与阿普苏的连接）过载，切断他们将部队送入这个银河的能力。\n\n" );
            buffer.Add( "警告：", "ff4400" ).Add( "建造去稳定器将触发最终决战。恶意软件将全力反击。做好相应准备。\n\n", "ffaa88" );
            buffer.Add( "阿普卡卢请求你的帮助。他们没有说这会让他们付出什么代价。", ObjectiveColors.Muted );
        }
    }

    public class FindZiggurat : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "银河其他地方还有阿普卡卢的金字形神塔，被恶意软件控制。目前从你已探索的星球上看不到任何一座。\n\n" );
            buffer.Add( "进一步探索以找到你可以收复的被腐化金字形神塔。", "bb77ee" );
        }
    }

    public class FlagshipProgressionT2 : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "随着金字形神塔被恢复，阿普卡卢现在可以解锁2级旗舰；战斗力的重大提升。\n\n" );
            buffer.Add( "使用你的枢纽突破奖励来解锁T2旗舰形态。", "44ccff" );
        }
    }

    public class FlagshipProgressionT3 : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "两个金字形神塔已被恢复。阿普卡卢最强大的旗舰形态现在触手可及。\n\n" );
            buffer.Add( "使用你的枢纽突破奖励来解锁T3旗舰形态。", "33bbff" );
        }
    }

    public class UpgradeFlagship : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            buffer.Add( "你的旗舰在Mark 1。消耗天青石升级其等级将显著提升其战斗效能。", "66ddff" );
        }
    }

    public class LamassuUnusedSockets : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            if ( Objective.RelatedEntity1 == null )
            {
                buffer.Add( "Bug in LamassuUnusedSockets: null entity" );
                return;
            }
            buffer.Add( "你的拉玛苏" ).Add( Objective.RelatedEntity1.GetPlanetName_Safe(), ObjectiveColors.Reward )
                .Add( "可以通过在其金字形神塔建造来升级。打开该星球上的建造菜单查看可建造内容。" );
        }
    }

    public class ApkalluResourceIncome : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
        public MouseHandlingResult ClickHandler( ActualObjective Objective ) { return MouseHandlingResult.None; }
        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            GameEntityTypeData fissureType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "ApsuFissure" );
            GameEntityTypeData temenType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "ApkalluTemen" );
            GameEntityTypeData duruType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "MajorDuru" );
            GameEntityTypeData pilgrimType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "ApkalluPilgrimTierOne" );

            buffer.Add( "阿普卡卢通过多种方式产生资源：\n\n" );

            if ( fissureType != null ) buffer.AddShipIconInline( fissureType, localFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
            buffer.Add( "裂缝入侵：", ObjectiveColors.Header ).Add( "\n\n通过入侵突破恶意软件裂缝可以在完成后获得资源。这是主要的收入来源。\n\n" );

            if ( temenType != null ) buffer.AddShipIconInline( temenType, localFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
            buffer.Add( "特门：", ObjectiveColors.Header ).Add( "\n\n已认领的特门产生被动收入并提升朝圣者奖励。\n\n" );

            if ( duruType != null ) buffer.AddShipIconInline( duruType, localFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
            buffer.Add( "杜鲁建筑：", ObjectiveColors.Header ).Add( "\n\n在杜鲁插槽中建造的某些建筑有助于资源生产。\n\n" );

            if ( pilgrimType != null ) buffer.AddShipIconInline( pilgrimType, localFaction, TextStyle.Ship_Sprite_Ency ).Add( " " );
            buffer.Add( "朝圣者：", ObjectiveColors.Header ).Add( "\n\n在星球间旅行的次级和高级朝圣者在到达金字形神塔时获得资源。" );
        }
    }

}
