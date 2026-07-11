using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Hacking_SpireArchive : BaseHackingImplementation
    {
    }

    public class Hacking_RerollARS : BaseHackingImplementation
    {
    }

    public class Hacking_AccessOutguardBeacon : HackingImplementation_WithMenu<OutguardGroupData, OutguardGroupData>
    {
        public override bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            /*
            LOG.Msg(
                "Hacking_AccessOutguardBeacon.DoSuccessfulCompletionLogic_Extra() called.\nTarget={0}, planet={1}, Hacker={2}, Event.RelatedString={3}",
                Target.OrNull(), planet.OrNull(), Hacker.OrNull(), (Event?.RelatedStringOrNull).OrNull() );
            */
            OutguardFactionBaseInfo.Instance.SetOutguardBeaconToHacked( Target, Context, Hacker, Event );
            return true;
        }

        public override string GetDynamicDescription(
            GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            Planet plan = target == null ? planet : target.Planet;
            List<OutguardGroupData> eligibleTargets = OutguardGroupData.GetTemporaryOutguardGroupDataList(
                "Hacking_AccessOutguardBeacon-GetDynamicDescription-eligibleTargets", 10f );
            if ( eligibleTargets == null ) //blocked for teardown/shutdown; bail
                return null;
            OutguardBeaconStateForPlanet.GetAvailableGroupsForBeaconOnPlanet( eligibleTargets, target.Planet );
            int minCost = 999;
            int maxCost = 0;

            ArcenCharacterBuffer buffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "Hacking_AccessOutguardBeacon-GetDynamicDescription" );

            buffer.StartColor( "9cbad3" );
            if ( eligibleTargets.Count > 0 )
            {
                buffer.Add( eligibleTargets.Count > 1 ? "将联系其中之一： " : "将联系： " );
                int index = 0;
                foreach ( OutguardGroupData eligibleTarget in eligibleTargets )
                {
                    var cost = eligibleTarget.AIPCostOnContact;
                    if ( cost < minCost )
                        minCost = cost;
                    if ( cost > maxCost )
                        maxCost = cost;

                    if ( index > 0 )
                    {
                        buffer.Add( ", " );
                    }

                    buffer.Add( eligibleTarget.DisplayName );
                    index++;
                }

                buffer.Add( "." );
                if ( eligibleTargets.Count > 1 )
                {
                    buffer.Add( "  你可以在发起黑客后选择要联系的对象。" );
                    if ( minCost != maxCost )
                        buffer.Add( "  联系他们的 AI 进度成本从 " ).Add( minCost ).Add( " 到 " ).Add( maxCost )
                              .Add( "，取决于你的选择。" );
                    else
                        buffer.Add( "  联系他们的 AI 进度成本为 " ).Add( minCost ).Add( "。" );
                }

                if ( AIWar2GalaxySettingQuickAccess.OutguardAlsoCostXenon && AIWar2GalaxySettingQuickAccess.EnableFuel )
                {
                    if ( minCost != maxCost )
                        buffer.Add( "  联系他们的永久损失 Xenon 成本从 " ).AddNumberMoreReadable( (minCost * 5000) )
                              .Add( " 到 " ).AddNumberMoreReadable( (maxCost * 5000) ).Add( "，取决于你的选择。" );
                    else
                        buffer.Add( "  联系他们的永久损失 Xenon 成本为 " ).AddNumberMoreReadable( (minCost * 5000) ).Add( "。" );
                }
            }
            else
            {
                buffer.Add( "嗯！在 " ).Add( (plan == null ? "null" : plan.Name) )
                      .Add( " 上没有可联系的前哨小队？这几乎肯定是一个错误。  " );
            }

            buffer.EndColor();

            OutguardGroupData.ReleaseTemporaryOutguardGroupDataList( eligibleTargets );

            return buffer.ToStringAndReturnToPool();
        }

        public override OutguardGroupData GetItemFromWrapper( OutguardGroupData wrapper, out bool found )
        {
            found = true;
            return wrapper;
        }

        public override void PopulateItemsToShow(
            List<OutguardGroupData> listToPopulate, GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, HackingType HackType )
        {
            OutguardBeaconStateForPlanet.GetAvailableGroupsForBeaconOnPlanet( listToPopulate, TargetIfShip.Planet );
        }

        public override void GetDisplayNameForItem( ArcenCharacterBufferBase buffer, OutguardGroupData item )
        {
            buffer.Add(item.DisplayName);
        }

        public override void GetExtraLabelInfoForItem(
            ArcenCharacterBufferBase buffer, OutguardGroupData item, GameEntity_Squad Target, Planet planet, HackingType HackType )
        {
            var cost = item.AIPCostOnContact;
            buffer.AddAIP( cost, true );
            if ( AIWar2GalaxySettingQuickAccess.OutguardAlsoCostXenon && AIWar2GalaxySettingQuickAccess.EnableFuel )
            {
                buffer.Add( " " );
                buffer.AddXenon_MoreReadable( cost * 5000, true );
            }
        }
        public override bool HasDetailsOfContents { get { return true; } }
        public override MouseHandlingResult ViewDetailsOfContents(OutguardGroupData item)
        {
            EntityText.ShowContents( World_AIW2.Instance.GetOutguardState( item ) );
            return MouseHandlingResult.None;
        }

        public override void DoHack(OutguardGroupData target, GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, HackingType HackType)
        {
            if ( target == null )
                return;
            if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                return;

            string rejectionReason = "";
            HackingUtils.TryDoHack( ref rejectionReason, TargetIfShip, TargetIfPlanet, HackType,
                                    target.InternalName, -1, null );
        }

        public override Hackable GetCanHackForThisItem(
            OutguardGroupData target, out string rejectionReason, GameEntity_Squad Target, Planet planet, HackingType Type )
        {
            if ( !Target.TypeData.GetHasTag( "OutguardBeacon" ) )
            {
                rejectionReason = "Not an outguard becaon!";
                return Hackable.NeverCanBeHacked_Hide;
            }

            if ( OutguardBeaconStateForPlanet.GetHasBeaconBeenHacked( Target ) )
            {
                rejectionReason = "Already been hacked!";
                return Hackable.AlreadyHasBeenHacked_Hide;
            }

            if ( World_AIW2.Instance.GetOutguardState( target ).HasBeenContacted )
            {
                rejectionReason = "无法再次联系；已经联系过了！";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            rejectionReason = "";

            return Hackable.CanBeHacked;
        }

        public override void GetTooltipForItem(
            ArcenCharacterBufferBase buffer, OutguardGroupData target, GameEntity_Squad Target, Planet planet, HackingType Type )
        {
            target.WriteTooltipNameBasedOnHack( buffer, false );
            target.WriteTooltipInfo( buffer );

            var cost = target.AIPCostOnContact;

            if ( cost > 0 )
            {
                buffer.Add( "If you make contact with this outguard faction, the AI Progress will go up by " ).Add( cost )
                      .Add( " right after you complete this hack.  " );

                if ( AIWar2GalaxySettingQuickAccess.OutguardAlsoCostXenon && AIWar2GalaxySettingQuickAccess.EnableFuel )
                {
                    buffer.Add( "Also, there is a cost in permanently-lost Xenon of " ).AddNumberMoreReadable( (cost * 5000) ).Add( "." );
                }
            }

            EntityText.Write_Tooltip_Hotkeys_Footer( buffer, false, false, "This Group" );
        }
    }

    public class Hacking_HackSomeFactionsBeacon : HackingImplementation_WithMenu<BeaconFactionOptionHackChoice, BeaconFactionOptionHackChoice>
    {
        public override bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return true; //only do this logic on the host, since we don't want double gamecommands generated

            //bring in the faction, whoever they are
            if ( !BeaconFactionOptionTable.Instance.FullListOfBeaconOptionsByName.ContainsKey( Event.RelatedStringOrNull ) )
            {
                ArcenDebugging.ArcenDebugLogSingleLine(
                    "Warning! The beacon lookup name '" + Event.RelatedStringOrNull + "' was missing!  We don't know how to bring in the faction you specified",
                    Verbosity.ShowAsError );
                return false;
            }

            //this gamecommand will be run on the client and the host
            GameCommand command = GameCommand.Create(
                GameCommandTypeTable.CoreFunctions[CoreFunction.BelatedlyCreateFactionsFromBeacon], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            command.RelatedString = Event.RelatedStringOrNull;
            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

            return true;
        }

        public override string GetDynamicDescription(
            GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            ArcenCharacterBuffer buffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "Hacking_HackSomeFactionsBeacon-GetDynamicDescription" );

            buffer.StartColor( "9cbad3" );
            buffer.Add( target.TypeData.Description );
            buffer.EndColor();

            return buffer.ToStringAndReturnToPool();
        }

        public override BeaconFactionOptionHackChoice GetItemFromWrapper( BeaconFactionOptionHackChoice wrapper, out bool found )
        {
            found = true;
            return wrapper;
        }

        public override Hackable GetCanHackForThisItem(
            BeaconFactionOptionHackChoice item, out string rejectionReason, GameEntity_Squad Target, Planet planet, HackingType Type )
        {
            if ( !Target.TypeData.GetHasTag( "Beacon" ) )
            {
                rejectionReason = "Not a faction becaon!";
                return Hackable.NeverCanBeHacked_Hide;
            }

            rejectionReason = "";
            return Hackable.CanBeHacked;
        }

        public override void PopulateItemsToShow(
            List<BeaconFactionOptionHackChoice> listToPopulate, GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, HackingType HackType )
        {
            for ( int i = 0; i < BeaconFactionOptionTable.Instance.Rows.Count; i++ )
            {
                BeaconFactionOption option = BeaconFactionOptionTable.Instance.Rows[i];
                if ( option == null )
                    continue;
                if ( TargetIfShip.TypeData.GetHasTag( option.TagFromWhichToSeedOneBeacon ) )
                {
                    //this one is valid!  There can be multiple valid ones
                    foreach ( BeaconFactionOptionHackChoice choice in option.HackChoices )
                        listToPopulate.Add( choice );
                }
            }
        }

        public override void GetDisplayNameForItem( ArcenCharacterBufferBase buffer, BeaconFactionOptionHackChoice item )
        {
            buffer.Add(item.DisplayName);
        }
        public override void DoHack(BeaconFactionOptionHackChoice item, GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, HackingType HackType)
        {
            if ( item == null )
                return;
            if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                return;

            string lastRejectionReason = ""; // FIXME: unused
            HackingUtils.TryDoHack( ref lastRejectionReason, TargetIfShip, TargetIfPlanet, HackType,
                                    item.LookupName, -1, null );
        }

        public override void GetTooltipForItem(
            ArcenCharacterBufferBase buffer, BeaconFactionOptionHackChoice item, GameEntity_Squad target, Planet planet, HackingType Type )
        {
            buffer.Add( item.Description );

            if ( target != null )
            {
                buffer.Add( "\n\n<b><u>关于此信标： " ).Add( target.TypeData.DisplayName ).Add( "</u></b>\n" );
                buffer.Add( target.TypeData.Description );
            }
        }
    }

    public class Hacking_ProvokeVengeanceStrike : BaseHackingImplementation
    {
        public override FInt GetHackingLevelMultiplier( Faction targetFaction, int bonusHackingPointsForCalculation = 0 )
        {
            FInt modifiedHackingLevel = FInt.FromParts( 0, 100 );
            if ( modifiedHackingLevel * (targetFaction.HackingPointsUsedAgainstThisFaction + bonusHackingPointsForCalculation) < FInt.One )
                return FInt.One;
            else
                return modifiedHackingLevel * (targetFaction.HackingPointsUsedAgainstThisFaction + bonusHackingPointsForCalculation);
        }

        public override void DoOneSecondOfHackingLogic_HackSpecificLogic(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            FInt multiplier = GetHackingLevelMultiplier( Target.PlanetFaction.Faction );
            if ( Hacker.ActiveHack_DurationThusFar % type.GetPrimaryHackResponseInterval() == 0 )
            {
                DarkSpireFactionBaseInfo dsdata = FactionUtilityMethods.Instance.GetDarkSpireFactionBaseInfo();
                DarkSpirePerPlanet perPlanet = dsdata.PerPlanet[Hacker.Planet.Index];
                perPlanet.NetEnergy += perPlanet.EnergyThresholdForAttack * type.PrimaryResponseStrengthPerInterval * multiplier;
            }

            if ( (type.GetSecondaryHackResponseInterval() > 0) && Hacker.ActiveHack_DurationThusFar % type.GetSecondaryHackResponseInterval() == 0 )
            {
                DarkSpireFactionBaseInfo dsdata = FactionUtilityMethods.Instance.GetDarkSpireFactionBaseInfo();
                DarkSpirePerPlanet perPlanet = dsdata.PerPlanet[Hacker.Planet.Index];
                perPlanet.NetEnergy += perPlanet.EnergyThresholdForAttack * type.SecondaryResponseStrengthPerInterval * multiplier;
            }
        }

        public override bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            FInt multiplier = GetHackingLevelMultiplier( Target.PlanetFaction.Faction );
            DarkSpireFactionBaseInfo dsdata = FactionUtilityMethods.Instance.GetDarkSpireFactionBaseInfo();
            DarkSpirePerPlanet perPlanet = dsdata.PerPlanet[Hacker.Planet.Index];
            perPlanet.NetEnergy += perPlanet.EnergyThresholdForAttack * multiplier;

            DarkSpireFactionBaseInfo.Instance.PerformVengeanceStrike();
            Event.RelatedEntityTypeData.Add( Target.TypeData );
            Event.RelatedInt.Add( 1 );
            if ( Target.TypeData.GetHasTag( "VengeanceGeneratorConquestSpawn" ) )
            {
                //turn this into a regular VG
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "VengeanceGeneratorNormalSpawn" );
                GameEntity_Squad.CreateNew_ReturnNullIfMPClient(
                    Target.PlanetFaction, entityData, entityData.MarkFor( Target.PlanetFaction ), Target.PlanetFaction.FleetUsedAtPlanet, 0,
                    Target.WorldLocation, Context, "Hacked-VGConquest" ); //part of sim, so fine

                Target.Despawn( Context, true, InstancedRendererDeactivationReason.WasHackedFully );
            }

            return true;
        }
    }

    public class Hacking_RenderVengeanceGeneratorVulnerable : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked(
            GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull,
            int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Hackable result = base.GetCanBeHacked(
                Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
            if ( result != Hackable.CanBeHacked )
                return result;
            return Hackable.CanBeHacked;
        }

        public override FInt GetHackingLevelMultiplier( Faction targetFaction, int bonusHackingPointsForCalculation = 0 )
        {
            FInt modifiedHackingLevel = FInt.FromParts( 0, 100 );
            if ( modifiedHackingLevel * (targetFaction.HackingPointsUsedAgainstThisFaction + bonusHackingPointsForCalculation) < FInt.One )
                return FInt.One;
            else
                return modifiedHackingLevel * (targetFaction.HackingPointsUsedAgainstThisFaction + bonusHackingPointsForCalculation);
        }

        public override void DoOneSecondOfHackingLogic_HackSpecificLogic(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            FInt multiplier = GetHackingLevelMultiplier( Target.PlanetFaction.Faction );
            if ( Hacker.ActiveHack_DurationThusFar % type.GetPrimaryHackResponseInterval() == 0 )
            {
                DarkSpireFactionBaseInfo dsdata = FactionUtilityMethods.Instance.GetDarkSpireFactionBaseInfo();
                DarkSpirePerPlanet perPlanet = dsdata.PerPlanet[Hacker.Planet.Index];
                perPlanet.NetEnergy += perPlanet.EnergyThresholdForAttack * type.PrimaryResponseStrengthPerInterval * multiplier;
            }

            if ( (type.GetSecondaryHackResponseInterval() > 0) && Hacker.ActiveHack_DurationThusFar % type.GetSecondaryHackResponseInterval() == 0 )
            {
                DarkSpireFactionBaseInfo dsdata = FactionUtilityMethods.Instance.GetDarkSpireFactionBaseInfo();
                DarkSpirePerPlanet perPlanet = dsdata.PerPlanet[Hacker.Planet.Index];
                perPlanet.NetEnergy += perPlanet.EnergyThresholdForAttack * type.SecondaryResponseStrengthPerInterval * multiplier;
            }
        }

        public override bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            //Also give the player a ship type
            DarkSpireFactionBaseInfo dsdata = FactionUtilityMethods.Instance.GetDarkSpireFactionBaseInfo();
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "VengeanceGeneratorVulnerable" );
            PlanetFaction pFaction = Target.PlanetFaction;
            ArcenPoint Location = Target.WorldLocation;
            Target.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );

            GameEntity_Squad vulnerableVG = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(
                pFaction, entityData, entityData.MarkFor( pFaction ), pFaction.Faction.LooseFleet, 0, Location, Context, "Hacked-VGVulnerable" );

            dsdata.DarkSpireGeneratesEnergy = true;
            if ( ArcenNetworkAuthority.GetIsHostMode() )
            {
                SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( vulnerableVG );

                World_AIW2.Instance.QueueChatMessageOrCommand(
                    "此复仇发生器现已脆弱。黑暗螺旋已苏醒，即使星球上没有战斗，它也会随机间隔生成攻击能量。同时触发了一次复仇打击",
                    ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
            }

            DarkSpireFactionBaseInfo.Instance.PerformVengeanceStrike();
            return true;
        }
    }

    public class Hacking_SubvertSuperTerminal : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked(
            GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull,
            int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Hackable result = base.GetCanBeHacked(
                Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
            if ( result != Hackable.CanBeHacked )
                return result;
            return Hackable.CanBeHacked;
        }
        public override bool CheckIfHackIsDone_OnlyIfPerSecondStyleCost( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type )
        {
            Faction facOrNull = Hacker.GetFactionOrNull_Safe();
            if ( facOrNull != null & facOrNull.StoredHacking <= 0 ) //Stops the hack if you hit 0 hacking
            {
                return true;
            }

            //you can just do the superterminal hack until the AI forces break you or hit zero hacking
            return false;
        }

        public override void DoOneSecondOfHackingLogic_HackSpecificLogic(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            //If this is called every second then just move the Hacking Level up by 1 second
            FInt AIPPerTick = type.EffectPerSecond;
            Faction targetFaction = Target.GetFactionOrNull_Safe();
            FInt hackingMultiplier = GetHackingLevelMultiplier( targetFaction );
            int multiplierFromMultipleHumanFactions = Math.Max( 1, World_AIW2.Instance.EmpireStylePlayerFactions.Count );
            FInt primaryResponseStrength = type.PrimaryResponseStrengthPerInterval * multiplierFromMultipleHumanFactions;
            FInt secondaryResponseStrength = type.SecondaryResponseStrengthPerInterval * multiplierFromMultipleHumanFactions;
            FInt tertiaryResponseStrength = type.TertiaryResponseStrengthPerInterval * multiplierFromMultipleHumanFactions;
            if ( type.GetPrimaryHackResponseInterval() <= 0 )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLog( "BUG: type.PrimaryHackResponseInterval was " + type.GetPrimaryHackResponseInterval(), Verbosity.ShowAsError );
            }

            int ticksThusFar = Hacker.ActiveHack_DurationThusFar / type.GetPrimaryHackResponseInterval();

            if ( Hacker.ActiveHack_DurationThusFar % 3 == 0 )
            {
                GlobalAIWorldBaseInfo.Instance.ChangeAIP(
                    -AIPPerTick * 3, AIPChangeReason.Hacking, Target.TypeData, Hacker.GetFactionIndex_Safe(), Target.Planet.Index,
                    Target.GetFactionIndex_Safe() );
                Event.AIPchanged -= (AIPPerTick * 3);
            }

            FInt extraResponseMultiplier = type.ResponseStrengthMultiplierPerEffect;
            for ( int i = 1; i < ticksThusFar; i++ )
            {
                extraResponseMultiplier *= type.ResponseStrengthMultiplierPerEffect;
            }

            //linear increases if desired
            primaryResponseStrength += (type.PrimaryResponseStrengthIncreasePerEffect * multiplierFromMultipleHumanFactions) * ticksThusFar;
            secondaryResponseStrength += (type.SecondaryResponseStrengthIncreasePerEffect * multiplierFromMultipleHumanFactions) * ticksThusFar;
            tertiaryResponseStrength += (type.TertiaryResponseStrengthIncreasePerEffect * multiplierFromMultipleHumanFactions) * ticksThusFar;

            if ( Hacker.ActiveHack_DurationThusFar % type.GetPrimaryHackResponseInterval() == 0 )
            {
                targetFaction.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly(
                    Target, ((primaryResponseStrength * extraResponseMultiplier) * hackingMultiplier * Hacker.TypeData.HackingEffectMultiplier), Context,
                    Event );
            }

            if ( Hacker.ActiveHack_DurationThusFar % type.GetSecondaryHackResponseInterval() == 0 )
            {
                Faction facOrNull = Target.GetFactionOrNull_Safe();
                if ( facOrNull != null )
                    facOrNull.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly(
                        Target, (secondaryResponseStrength * hackingMultiplier * extraResponseMultiplier * Hacker.TypeData.HackingEffectMultiplier), Context,
                        Event );
            }

            if ( Hacker.ActiveHack_DurationThusFar % type.GetTertiaryHackResponseInterval() == 0 )
            {
                Faction facOrNull = Target.GetFactionOrNull_Safe();
                if ( facOrNull != null )
                    facOrNull.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly(
                        Target, (tertiaryResponseStrength * hackingMultiplier * extraResponseMultiplier * Hacker.TypeData.HackingEffectMultiplier), Context,
                        Event );
            }

            if ( type.PeriodicExoStrength > FInt.Zero )
            {
                if ( Hacker.ActiveHack_DurationThusFar % type.PeriodicExoInterval == 0 && Hacker.ActiveHack_DurationThusFar > 0 )
                {
                    if ( targetFaction.Type != FactionType.AI )
                        targetFaction = World_AIW2.GetRandomAIFaction( Context );

                    GameEntity_Squad king = Hacker.GetFirstMatchingInFaction( EntityRollupType.KingUnitsOnly, false, false );
                    AISentinelsCoreData factionExternal = targetFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
                    int exoStrength =
                        (type.PeriodicExoStrength * factionExternal.AIDifficulty.BaseHackingWaveSize * hackingMultiplier * extraResponseMultiplier).IntValue;
                    ExoOptions options = ExoOptions.CreateWithDefaults( king, exoStrength, targetFaction, targetFaction );
                    ExoGalacticDeepLinkRoot.Instance.SendExoGalacticAttack( options, Context );
                }
            }
        }

        public override bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            //nothing to do here!
            return true;
        }
    }

    public class Hacking_ScienceExtractionFromNeutralPlanet : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked(
            GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull,
            int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( planet.GetControllingFactionType() != FactionType.NaturalObject )
            {
                RejectionReasonDescription = "This hack only works against neutral planets";
                return Hackable.NeverCanBeHacked_Hide;
            }

            if ( planet.GetScienceLeftForHumans() <= FInt.Zero )
            {
                RejectionReasonDescription = "You have already extracted all the science";
                return Hackable.AlreadyHasBeenHacked_Hide;
            }

            if ( World_AIW2.Instance.EmpireStylePlayerFactions.Count == 0 )
            {
                RejectionReasonDescription = "There are no human empires present in this game";
                return Hackable.NeverCanBeHacked_Hide;
            }

            return base.GetCanBeHacked(
                Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }

        public override bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            Faction hackingFaction = Hacker.GetFactionOrNull_Safe();
            FInt scienceLeft = planet.GetScienceLeftForHumans();
            planet.ScienceGatheredByHumans += scienceLeft;
            Event.ScienceGained += (Int16) scienceLeft.IntValue;
            foreach ( Faction fac in World_AIW2.Instance.EmpireStylePlayerFactions )
                fac.StoredScience += scienceLeft;
            return true;
        }
    }

    public class Hacking_DeploySpyNanites_Watch : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked(
            GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull,
            int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( planet.IntelLevel >= PlanetIntelLevel.PermanentlyWatched )
            {
                RejectionReasonDescription = "This planet is already permanently Watched";
                return Hackable.AlreadyHasBeenHacked_Hide;
            }

            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
            {
                RejectionReasonDescription = "You must have Explored this planet before you can watch it";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            RejectionReasonDescription = string.Empty;
            return Hackable.CanBeHacked;
        }

        public override bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            planet.GrantIntel( PlanetIntelLevel.PermanentlyWatched );
            planet.GameSecondLastHadVisionBase = World_AIW2.Instance.GameSecond;
            return true;
        }
    }

    public class Hacking_DeploySpyNanites_Explore : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked(
            GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull,
            int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( planet.IntelLevel > PlanetIntelLevel.Unexplored )
            {
                RejectionReasonDescription = "This planet is already Explored";
                return Hackable.AlreadyHasBeenHacked_Hide;
            }

            bool hasNeighboringVision = false;
            foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
            {
                    if ( neighbor.IntelLevel > PlanetIntelLevel.Unexplored )
                    {
                        hasNeighboringVision = true;
                        break;
                    }
            }
            if ( !hasNeighboringVision )
            {
                RejectionReasonDescription = "You must have Explored a neighboring planet before you can Explore this one";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            RejectionReasonDescription = string.Empty;
            return Hackable.CanBeHacked;
        }

        public override bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            planet.GrantIntel( PlanetIntelLevel.ExploredByDistantHacking );
            planet.GameSecondLastHadVisionBase = World_AIW2.Instance.GameSecond;
            return true;
        }
    }

    public class Hacking_WeakenTurretsOnAIWorld : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked(
            GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull,
            int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
            {
                RejectionReasonDescription = "This planet has not yet been explored!";
                return Hackable.NeverCanBeHacked_Hide;
            }

            Faction ownerFaction = planet.GetControllingOrInfluencingFaction();
            if ( ownerFaction == null || ownerFaction.Type != FactionType.AI )
            {
                RejectionReasonDescription = "This planet is not owned by an AI!";
                return Hackable.NeverCanBeHacked_Hide;
            }

            if ( planet.AreAITurretsWeakenedByHack )
            {
                RejectionReasonDescription = "This planet has already had its turrets weakened by a previous hack!";
                return Hackable.AlreadyHasBeenHacked_ButStillShow;
            }

            RejectionReasonDescription = string.Empty;
            return Hackable.CanBeHacked;
        }

        public override bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            planet.AreAITurretsWeakenedByHack = true;
            return true;
        }
    }

    public class Hacking_WeakenGuardPostsOnAIWorld : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked(
            GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull,
            int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( planet.IntelLevel <= PlanetIntelLevel.Unexplored )
            {
                RejectionReasonDescription = "This planet has not yet been explored!";
                return Hackable.NeverCanBeHacked_Hide;
            }

            Faction ownerFaction = planet.GetControllingOrInfluencingFaction();
            if ( ownerFaction == null || ownerFaction.Type != FactionType.AI )
            {
                RejectionReasonDescription = "This planet is not owned by an AI!";
                return Hackable.NeverCanBeHacked_Hide;
            }

            if ( planet.AreAIGuardPostsWeakenedByHack )
            {
                RejectionReasonDescription = "This planet has already had its guard posts weakened by a previous hack!";
                return Hackable.AlreadyHasBeenHacked_ButStillShow;
            }

            RejectionReasonDescription = string.Empty;
            return Hackable.CanBeHacked;
        }

        public override bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            planet.AreAIGuardPostsWeakenedByHack = true;
            return true;
        }
    }

    public class Hacking_GrantShipLine : BaseHackingImplementation
    {
        //this is the same the "GrantShipLine_DontDestroyTarget" hack below; please duplicate any changes
        public override Hackable GetCanBeHacked(
            GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull,
            int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            //            Hackable result = base.GetCanBeHacked( Target, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
            if ( !Target.TypeData.GrantsStuffToBeAddedToPlayerFleets && !Target.TypeData.HackableForCommandStationsAndBattleStations_DSSStyle )
            {
                RejectionReasonDescription = "This hack grants ship lines from the appropriate structures";
                return Hackable.NeverCanBeHacked_Hide;
            }

            if ( HackerOrNull == null )
            {
                RejectionReasonDescription = "No valid hackers here.";

                return Hackable.NeverBeHacked_ButStillShow;
            }

            if ( HackerOrNull.TypeData.SpecialType == SpecialEntityType.MobileSupportFleetFlagship && !Type.HackerCanBeSupportFleet )
            {
                RejectionReasonDescription = "No valid hackers here.";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            if ( (HackerOrNull.TypeData.SpecialType == SpecialEntityType.BattlestationBasic ||
                  HackerOrNull.TypeData.SpecialType == SpecialEntityType.BattlestationCitadel) &&
                 (!Type.HackerCanBeBattlestation && !Type.HackerMustBeBattlestation) )
            {
                RejectionReasonDescription = "No valid hackers here.";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            if ( HackerOrNull.TypeData.SpecialType == SpecialEntityType.MobileCustomCityFedFleetFlagship )
            {
                RejectionReasonDescription = "No valid hackers here.";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            if ( Type.NumberOfTimesIndividualUnitCanBeHacked > 0 &&
                 Target.GetNumberOfTimesHacked(Type) >= Type.NumberOfTimesIndividualUnitCanBeHacked )
            {
                RejectionReasonDescription = "We have already hacked as many times as we can (" + Type.NumberOfTimesIndividualUnitCanBeHacked + " out of " +
                                             Target.GetNumberOfTimesHacked( Type ) + ")";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            //if not checking a SPECIFIC option, see if we can run this hack at all
            if ( RelatedStringOrNull == null || RelatedStringOrNull.Length <= 0 )
            {
            }
            else //RelatedStringOrNull is an actual value, so check a specific value
            {
                GameEntityTypeData typeDataToMatch = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( RelatedStringOrNull );
                if ( typeDataToMatch == null )
                {
                    RejectionReasonDescription = "Could not find the matching GameEntityTypeData for '" + RelatedStringOrNull +
                                                 "'!  This is a bug, please report it.";
                    return Hackable.NeverBeHacked_ButStillShow;
                }

                if ( HackerOrNull.TypeData.IsBattlestation && typeDataToMatch.IsBlockedFromDSSGrantToBattlestationsAndCitadels )
                {
                    RejectionReasonDescription = "This type of defensive line cannot be given to battlestations or citadels.  Only command stations.";
                    return Hackable.NeverBeHacked_ButStillShow;
                }

                if ( HackerOrNull.TypeData.IsCommandStation && typeDataToMatch.IsBlockedFromDSSGrantToCommandStations )
                {
                    RejectionReasonDescription = "This type of defensive line cannot be given to command stations.  Only battlestations or citadels.";
                    return Hackable.NeverBeHacked_ButStillShow;
                }

                if ( typeDataToMatch.IsElite && HackerOrNull != null && HackerOrNull.CalculateIsFleetEliteSlotFilled_Safe() )
                {
                    RejectionReasonDescription = "The hacker fleet already has a filled elite slot, so cannot hack to gain another elite.";
                    return Hackable.NeverBeHacked_ButStillShow;
                }
            }

            if ( HackerOrNull != null &&
                 (HackerOrNull.CalculateFleetRemainingShipLineSlotCount_Safe() <= 0) )
            {
                RejectionReasonDescription = "This " + Target.TypeData.GetDisplayName() + " cannot grant a ship line to a flagship with " +
                                             ExternalConstants.Instance.Balance_MaxShipLinesPerFleet + " ship lines. The flagship " +
                                             HackerOrNull.GetFleetName_Safe() + " has " + (HackerOrNull.CalculateFleetNumRealShipLines_Safe()) +
                                             ". This hack grants ships to the closest Flagship.";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            RejectionReasonDescription = "";
            return Hackable.CanBeHacked;
        }

        public override bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                Faction facOrNull = Hacker.GetFactionOrNull_Safe();
                if ( facOrNull != null )
                    facOrNull.NumARSsHacked_ForUI++;
                debugCode = 200;
                bool doDSSStyleReroll = false;
                for ( int i = 0; i < Target.ShipGrantsList.Count; i++ )
                {
                    debugCode = 300;
                    ShipLineEntry entry = Target.ShipGrantsList[i];
                    if ( entry.BaseNumShips <= 0 )
                        continue;
                    debugCode = 400;
                    if ( Event.RelatedStringOrNull != null && Event.RelatedStringOrNull.Length > 0 )
                    {
                        if ( entry.TypeData.InternalName != Event.RelatedStringOrNull )
                            continue; //when limiting it to one type, just add the one type!
                    }

                    debugCode = 500;
                    int finalNumberShips = entry.GetNumShipsForHackAndHacker( Hacker, type );
                    debugCode = 600;
                    FleetMembership membership = null;
                    if ( Target.TypeData.HackableForCommandStationsAndBattleStations_DSSStyle && type.IsForGrantingToCommandStationsAndBattlestations )
                    {
                        debugCode = 1000;
                        doDSSStyleReroll = true;
                        //DSS normal
                        Faction hackerFaction = Hacker.GetFactionOrNull_Safe();
                        debugCode = 1100;
                        bool addedToExisting = false;
                        for ( int j = 0; j < hackerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent.Count; j++ )
                        {
                            debugCode = 1200;
                            KeyValuePair<GameEntityTypeData, int> kv = hackerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent[j];
                            if ( kv.Key == entry.TypeData )
                            {
                                addedToExisting = true;
                                hackerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent[j] =
                                    new KeyValuePair<GameEntityTypeData, int>( kv.Key, kv.Value + finalNumberShips );
                            }
                        }

                        debugCode = 1300;
                        if ( !addedToExisting )
                            hackerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent.Add(
                                new KeyValuePair<GameEntityTypeData, int>( entry.TypeData, finalNumberShips ) );

                        //for ( int j = 0; j < hackerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent.Count; j++ )
                        //{
                        //    ArcenDebugging.ArcenDebugLogSingleLine( hackerFaction.GetDisplayName() + ": has " + hackerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent[j].LeftItem.DisplayName +
                        //           " x" + hackerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent[j].RightItem, Verbosity.DoNotShow );
                        //}
                    }
                    else if ( Hacker.TypeData.IsBattlestation || Hacker.TypeData.IsCommandStation )
                    {
                        debugCode = 2000;
                        doDSSStyleReroll = true;
                        //DSS bonus added to just the hacker -- do NOT add a new ship line if we don't have to.
                        Fleet hackerFleet = Hacker.GetFleetOrNull_Safe();
                        if ( hackerFleet != null )
                        {
                            membership = hackerFleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( entry.TypeData );
                            debugCode = 2100;
                            if ( membership.ExplicitBaseSquadCap <= 0 )
                                membership.ExplicitBaseSquadCap = finalNumberShips;
                            else
                                membership.ExplicitBaseSquadCap += finalNumberShips;
                        }

                        //ArcenDebugging.ArcenDebugLogSingleLine( membership.Fleet.GetName() + ": got " + membership.TypeData.DisplayName +
                        //    " x" + finalNumberShips + " (" + entry.BaseNumShips + ")", Verbosity.DoNotShow );
                    }
                    else //ARS and FRS
                    {
                        debugCode = 2500;

                        // ARS style hack for ship lines can also be marked to be given to command stations, rather than the hacker-fleet.
                        if ( type.IsForGrantingToCommandStationsAndBattlestations )
                        {
                            Faction hackerFaction = Hacker.GetFactionOrNull_Safe();
                            debugCode = 2600;
                            bool addedToExisting = false;
                            for ( int j = 0; j < hackerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent.Count; j++ )
                            {
                                debugCode = 2700;
                                KeyValuePair<GameEntityTypeData, int> kv = hackerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent[j];
                                if ( kv.Key == entry.TypeData )
                                {
                                    addedToExisting = true;
                                    hackerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent[j] =
                                        new KeyValuePair<GameEntityTypeData, int>( kv.Key, kv.Value + finalNumberShips );
                                }
                            }

                            debugCode = 2800;
                            if ( !addedToExisting )
                                hackerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent.Add(
                                    new KeyValuePair<GameEntityTypeData, int>( entry.TypeData, finalNumberShips ) );
                        }
                        else
                        {
                            debugCode = 3000;

                            //Add this to the flagship doing the hacking
                            Fleet hackerFleet = Hacker.GetFleetOrNull_Safe();
                            if ( hackerFleet != null )
                            {
                                int nextUniqueID = hackerFleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( entry.TypeData );
                                debugCode = 3100;
                                membership = hackerFleet.GetOrAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates( entry.TypeData, nextUniqueID );
                                debugCode = 3200;
                                if ( membership.ExplicitBaseSquadCap <= 0 )
                                    membership.ExplicitBaseSquadCap = finalNumberShips;
                                else
                                    membership.ExplicitBaseSquadCap += finalNumberShips;
                            }

                            //ArcenDebugging.ArcenDebugLogSingleLine( "ARS or FRS: " + membership.Fleet.GetName() + ": got " + membership.TypeData.DisplayName +
                            //    " x" + finalNumberShips + " (" + entry.BaseNumShips + ")", Verbosity.DoNotShow );
                        }
                    }

                    debugCode = 4000;
                    if ( membership != null && membership.TypeData.AIPWhenGrantedByHack > FInt.Zero )
                    {
                        debugCode = 4100;
                        GlobalAIWorldBaseInfo.Instance.ChangeAIP(
                            membership.TypeData.AIPWhenGrantedByHack, AIPChangeReason.EntityClaim, membership.TypeData, Hacker.GetFactionIndex_Safe(),
                            Hacker.Planet.Index, Hacker.GetFactionIndex_Safe() );
                    }

                    debugCode = 5000;
                    Event.RelatedEntityTypeData.Add( entry.TypeData );
                    Event.RelatedInt.Add( finalNumberShips );
                }

                debugCode = 6000;
                //Now Re-Roll the target in case we can hack it again
                Target.ClearShipGrantList();
                if ( Target.HackingSeed == 0 )
                    Target.HackingSeed = Target.PrimaryKeyID + Target.Planet.RandomSeed;
                else
                    Target.HackingSeed++;
                debugCode = 7000;
                MersenneTwister rand = new MersenneTwister( Target.HackingSeed );

                Target.ClearShipGrantList();

                debugCode = 8000;
                if ( doDSSStyleReroll )
                {
                    FleetDesignTemplateTable.Instance.GetAddedToCommandStationsForEntity_DSSStyle( rand, Target.ShipGrantsList, Target.TypeData );
                }
                else
                {
                    FleetDesignTemplateTable.Instance.GetAddedToFleetsForEntity(
                        rand, Target.ShipGrantsList, Target.TypeData.GrantsStuffToBeAddedToPlayerFleets_StrikecraftOptions,
                        Target.TypeData.GrantsStuffToBeAddedToPlayerFleets_FrigateOptions, Target.TypeData.GrantsStuffToBeAddedToPlayerFleets_RequiredTag,
                        Target.TypeData.GrantsStuffToBeAddedToPlayerFleets_PercentChanceFrigatesAreStrikecraft, null, null ); //no limiting on techs
                }

                Target.FlagForForcedFullSyncToClients_FromHost();
            }
            catch ( Exception e )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine(
                        "Exception hit during Hacking_GrantShipLine DoSuccessfulCompletionLogic_Extra debug code " + debugCode + " " + e.ToString(),
                        Verbosity.ShowAsError );
                return false;
            }

            return true;
        }
    }

    public class Hacking_GrantShipLine_DontDestroyTarget : BaseHackingImplementation
    {
        //this essentially the same as the "GrantShipLine" hack above; please duplicate necessary changes.
        //Note that this is used by a bunch of minor factions, many of which have different responses to being hacked.
        public override Hackable GetCanBeHacked(
            GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull,
            int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            //            Hackable result = base.GetCanBeHacked( Target, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
            int debugStage = 0;
            try
            {
                debugStage = 100;
                if ( Target == null )
                {
                    RejectionReasonDescription = "Null target";
                    return Hackable.NeverCanBeHacked_Hide;
                }

                debugStage = 200;
                GameEntityTypeData typeData = Target.TypeData;
                if ( typeData == null )
                {
                    RejectionReasonDescription = "Null typeData";
                    return Hackable.NeverCanBeHacked_Hide;
                }

                debugStage = 250;
                if ( Type.NumberOfTimesIndividualUnitCanBeHacked > 0 &&
                     Target.GetNumberOfTimesHacked(Type) >= Type.NumberOfTimesIndividualUnitCanBeHacked )
                {
                    RejectionReasonDescription = "We have already hacked as many times as we can (" + Type.NumberOfTimesIndividualUnitCanBeHacked + " out of " +
                                                 Target.GetNumberOfTimesHacked( Type ) + ")";
                    return Hackable.NeverBeHacked_ButStillShow;
                }

                debugStage = 300;
                if ( !typeData.GrantsStuffToBeAddedToPlayerFleets )
                {
                    RejectionReasonDescription = "This hack grants ship lines from the appropriate structures";
                    return Hackable.NeverCanBeHacked_Hide;
                }

                debugStage = 400;
                if ( HackerOrNull == null )
                {
                    if ( Type.HackerMustBeBattlestation )
                        RejectionReasonDescription = "There are no battlestations/citadels on this planet, and one of those is required for this hack";
                    else
                        RejectionReasonDescription = "There are no units that can hack on this planet";
                    return Hackable.NeverBeHacked_ButStillShow;
                }

                debugStage = 500;
                GameEntityTypeData hackerTypeData = HackerOrNull.TypeData;
                if ( hackerTypeData == null )
                {
                    RejectionReasonDescription = "Hacker typedata is null";
                    return Hackable.NeverBeHacked_ButStillShow;
                }

                debugStage = 600;
                if ( hackerTypeData.SpecialType == SpecialEntityType.MobileSupportFleetFlagship )
                {
                    RejectionReasonDescription = "Only combat fleets can hack an ARS";
                    return Hackable.NeverBeHacked_ButStillShow;
                }

                debugStage = 100;
                debugStage = 700;
                if ( typeData.GetHasTag( "ZenithArchitravePortal" ) || typeData.GetHasTag( "ArchitraveCastra" ) )
                {
                    Faction targetFacOrNull = Target.GetFactionOrNull_Safe();
                    if ( targetFacOrNull != null )
                    {
                        if ( ArcenStrings.Equals( targetFacOrNull.BaseInfo.Allegiance, "Friendly To Players" ) )
                        {
                            RejectionReasonDescription = "You cannot hack your allies";
                            return Hackable.NeverBeHacked_ButStillShow;
                        }
                    }
                }

                debugStage = 900;
                if ( typeData.GetHasTag( "VengeanceGenerator" ) )
                {
                    DarkSpireFactionBaseInfo dsdata = FactionUtilityMethods.Instance.GetDarkSpireFactionBaseInfo();
                    DarkSpirePerPlanet perPlanet = dsdata.PerPlanet[Target.Planet.Index];
                    if ( perPlanet.FactionsWhichHaveDownloadedShipDesign.Contains( HackerFaction.FactionIndex ) &&
                         DarkSpireFactionBaseInfo.ShipDesignsDownloadble <= dsdata.PerPlanet[Target.Planet.Index].numShipDesignsDownloaded )
                    {
                        //So in principle we can do X ship designs, but if this player hasn't downloaded any ship designs then they are
                        //always allowed to get 1
                        RejectionReasonDescription = "No more ship designs available";
                        return Hackable.AlreadyHasBeenHacked_ButStillShow;
                    }
                }

                debugStage = 1000;
                //if not checking a SPECIFIC option, see if we can run this hack at all
                if ( RelatedStringOrNull == null || RelatedStringOrNull.Length <= 0 )
                {
                }
                else //RelatedStringOrNull is an actual value, so check a specific value
                {
                    debugStage = 1100;
                    GameEntityTypeData typeDataToMatch = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( RelatedStringOrNull );
                    if ( typeDataToMatch == null )
                    {
                        RejectionReasonDescription = "Could not find the matching GameEntityTypeData for '" + RelatedStringOrNull +
                                                     "'!  This is a bug, please report it.";
                        return Hackable.NeverBeHacked_ButStillShow;
                    }

                    debugStage = 1200;
                    if ( HackerOrNull.TypeData.IsBattlestation && typeDataToMatch.IsBlockedFromDSSGrantToBattlestationsAndCitadels )
                    {
                        RejectionReasonDescription = "This type of defensive line cannot be given to battlestations or citadels.  Only command stations.";
                        return Hackable.NeverBeHacked_ButStillShow;
                    }

                    debugStage = 1300;
                    if ( HackerOrNull.TypeData.IsCommandStation && typeDataToMatch.IsBlockedFromDSSGrantToCommandStations )
                    {
                        RejectionReasonDescription = "This type of defensive line cannot be given to command stations.  Only battlestations or citadels.";
                        return Hackable.NeverBeHacked_ButStillShow;
                    }

                    debugStage = 1400;
                    if ( typeDataToMatch.IsElite && HackerOrNull != null && HackerOrNull.CalculateIsFleetEliteSlotFilled_Safe() )
                    {
                        RejectionReasonDescription = "The hacker fleet already has a filled elite slot, so cannot hack to gain another elite.";
                        return Hackable.NeverBeHacked_ButStillShow;
                    }
                }

                debugStage = 1500;
                if ( HackerOrNull != null && (HackerOrNull.CalculateFleetRemainingShipLineSlotCount_Safe() <= 0) )
                {
                    RejectionReasonDescription = "This " + Target.TypeData.GetDisplayName() + " cannot grant a ship line to a flagship with " +
                                                 ExternalConstants.Instance.Balance_MaxShipLinesPerFleet + " ship lines. The flagship " +
                                                 HackerOrNull.GetFleetName_Safe() + " has " + (HackerOrNull.CalculateFleetNumRealShipLines_Safe()) +
                                                 ". This hack grants ships to the closest Flagship.";
                    return Hackable.NeverBeHacked_ButStillShow;
                }
            }
            catch
            {
                RejectionReasonDescription = "Error at debug stage " + debugStage;
                ArcenDebugging.ArcenDebugLogSingleLine(
                    "Hacking_GrantShipLine_DontDestroyTarget.GetCanBeHacked: Error at debug stage " + debugStage, Verbosity.DoNotShow );
                return Hackable.NeverBeHacked_ButStillShow;
            }

            RejectionReasonDescription = "";
            return Hackable.CanBeHacked;
        }
        public override int EstimateTotalDifficulty( HackingType type, GameEntity_Squad TargetOrNull, Planet PlanetOrNull, out int totalResponseStrengthEstimate, out ArcenCharacterBuffer debugLogOrNull, bool assumeAIStrengthLevels )
        {
            //If "assumeAIStrengthLevels" then we allow for estimateing non-AI strucures by just guesstimating based on a random AI's Wave strength
            //We can use this override for any time we've vetted how the response is calculated by passing in true for assumeAIStrengthLevels
            if ( TargetOrNull == null ||  !TargetOrNull.TypeData.GetHasTag("DZLibrary") )
                return base.EstimateTotalDifficulty( type, TargetOrNull, PlanetOrNull, out totalResponseStrengthEstimate, out debugLogOrNull, false );
            else
                return base.EstimateTotalDifficulty( type, TargetOrNull, PlanetOrNull, out totalResponseStrengthEstimate, out debugLogOrNull, true ); //give an estimate based off the AI response
        }

        public override bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            int debugcode = 0;
            try
            {
                bool debug = GameSettings.Current.GetBoolBySetting( "HackingDebug" );

                debugcode = 100;
                Faction facOrNull = Hacker.GetFactionOrNull_Safe();
                if ( facOrNull != null )
                    facOrNull.NumARSsHacked_ForUI++;

                debugcode = 200;
                for ( int i = 0; i < Target.ShipGrantsList.Count; i++ )
                {
                    ShipLineEntry entry = Target.ShipGrantsList[i];
                    if ( entry.BaseNumShips <= 0 )
                        continue;

                    debugcode = 210;
                    if ( Event.RelatedStringOrNull != null && Event.RelatedStringOrNull.Length > 0 )
                    {
                        if ( entry.TypeData.InternalName != Event.RelatedStringOrNull )
                            continue; //when limiting it to one type, just add the one type!
                    }

                    debugcode = 220;
                    int finalNumberShips = entry.GetNumShipsForHackAndHacker( Hacker, type );
                    FleetMembership membership = null;

                    debugcode = 230;
                    if ( Target.TypeData.HackableForCommandStationsAndBattleStations_DSSStyle && 
                         type.IsForGrantingToCommandStationsAndBattlestations )
                    {
                        debugcode = 240;
                        //DSS normal
                        Faction hackerFaction = Hacker.GetFactionOrNull_Safe();
                        bool addedToExisting = false;
                        for ( int j = 0; j < hackerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent.Count; j++ )
                        {
                            debugcode = 250;
                            KeyValuePair<GameEntityTypeData, int> kv = hackerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent[j];
                            if ( kv.Key == entry.TypeData )
                            {
                                addedToExisting = true;
                                hackerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent[j] =
                                    new KeyValuePair<GameEntityTypeData, int>( kv.Key, kv.Value + finalNumberShips );
                            }
                        }

                        debugcode = 260;
                        if ( !addedToExisting )
                            hackerFaction.OnePlayer_AddedToCommandStationsAndBattlestations_Permanent.Add(
                                new KeyValuePair<GameEntityTypeData, int>( entry.TypeData, finalNumberShips ) );
                    }
                    else 
                    if ( Hacker.TypeData.IsBattlestation || 
                         Hacker.TypeData.IsCommandStation )
                    {
                        debugcode = 300;

                        //DSS bonus added to just the hacker -- do NOT add a new ship line if we don't have to.
                        Fleet hackerFleet = Hacker.GetFleetOrNull_Safe();
                        if ( hackerFleet != null )
                        {
                            membership = hackerFleet.GetOrAddMembershipGroupBasedOnSquadType_AssumeNoDuplicates( entry.TypeData );
                            if ( membership.ExplicitBaseSquadCap <= 0 )
                                membership.ExplicitBaseSquadCap = finalNumberShips;
                            else
                                membership.ExplicitBaseSquadCap += finalNumberShips;
                        }
                    }
                    else //ARS and FRS
                    {
                        debugcode = 400;

                        //Add this to the flagship doing the hacking
                        Fleet hackerFleet = Hacker.GetFleetOrNull_Safe();
                        if ( hackerFleet != null )
                        {
                            int nextUniqueID = hackerFleet.GetNextUniqueIntToUseOfMatchingMembershipGroupsBasedOnSquadType( entry.TypeData );
                            membership = hackerFleet.GetOrAddMembershipGroupBasedOnSquadType_WithUniqueIDForDuplicates( entry.TypeData, nextUniqueID );
                            if ( membership.ExplicitBaseSquadCap <= 0 )
                                membership.ExplicitBaseSquadCap = finalNumberShips;
                            else
                                membership.ExplicitBaseSquadCap += finalNumberShips;
                        }
                    }

                    debugcode = 500;

                    if ( membership != null && membership.TypeData.AIPWhenGrantedByHack > FInt.Zero )
                    {
                        debugcode = 510;
                        GlobalAIWorldBaseInfo.Instance.ChangeAIP(
                            membership.TypeData.AIPWhenGrantedByHack, AIPChangeReason.EntityClaim, membership.TypeData, Hacker.GetFactionIndex_Safe(),
                            Hacker.Planet.Index, Hacker.GetFactionIndex_Safe() );
                    }

                    debugcode = 520;
                    Event.RelatedEntityTypeData.Add( entry.TypeData );
                    Event.RelatedInt.Add( finalNumberShips );
                }

                debugcode = 600;
                if ( Target.TypeData.GetHasTag( "ZenithArchitravePortal" ) )
                {
                    debugcode = 610;

                    var zabase = Target.GetFactionBaseInfoOrNullAs_Safe<ZenithArchitraveFactionBaseInfo>();
                    if ( zabase != null )
                    {
                        debugcode = 620;

                        zabase.MaxTerritorySize++;

                        ZenithArchitraveFactionBaseInfo.ConvertPortalToHackedPortal( Target, Context );

                        return true;
                    }
                }

                debugcode = 700;

                if ( Target.TypeData.GetHasTag( "VengeanceGenerator" ) )
                {
                    debugcode = 710;

                    DarkSpireFactionBaseInfo dsdata = FactionUtilityMethods.Instance.GetDarkSpireFactionBaseInfo();
                    if ( dsdata != null )
                    {
                        debugcode = 715;

                        dsdata.PerPlanet[Target.Planet.Index].VGGeneratesEnergy = true;
                        dsdata.PerPlanet[Target.Planet.Index].numShipDesignsDownloaded++;
                        dsdata.PerPlanet[Target.Planet.Index].FactionsWhichHaveDownloadedShipDesign.Add( Hacker.GetFactionIndex_Safe() );

                        debugcode = 720;

                        if ( Target.TypeData.GetHasTag( "VengeanceGeneratorConquestSpawn" ) )
                        {
                            debugcode = 730;

                            //turn this into a regular VG
                            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "VengeanceGeneratorNormalSpawn" );
                            GameEntity_Squad.CreateNew_ReturnNullIfMPClient(
                                Target.PlanetFaction, entityData, entityData.MarkFor( Target.PlanetFaction ), Target.PlanetFaction.FleetUsedAtPlanet, 0,
                                Target.WorldLocation, Context, "Hacking-VGNormal" ); //part of sim, so fine

                            Target.Despawn( Context, true, InstancedRendererDeactivationReason.WasHackedFully );

                            debugcode = 740;

                            return true;
                        }
                    }
                }

                debugcode = 800;

                //Now Re-Roll the target in case we can hack it again
                Target.ClearShipGrantList();
                if ( Target.HackingSeed == 0 )
                    Target.HackingSeed = Target.PrimaryKeyID + Target.Planet.RandomSeed;
                else
                    Target.HackingSeed++;

                debugcode = 900;

                return true;
            }
            catch ( Exception e )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    LOG.Err( "Exception in Hacking_GrantShipLine_DontDestroyTarget.DoSuccessfulCompletionLogic_Extra() at debugcode {0}.\n{1}", debugcode, e );
                return false;
            }
        }
    }

    public class Hacking_NullHackHandler : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked(
            GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull,
            int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            RejectionReasonDescription = "Never can be done!";
            return Hackable.NeverCanBeHacked_Hide;
        }
    }

    public abstract class Hacking_GrantTech : HackingImplementation_WithMenu<TechUpgrade, TechUpgrade>
    {
        public override Hackable GetCanHackForThisItem( TechUpgrade item, out string rejectionReason, GameEntity_Squad Target, Planet planet, HackingType Type )
        {
            if ( !Target.TypeData.GrantsTechs )
            {
                rejectionReason = "This hack grants tech from the appropriate structures";
                return Hackable.NeverCanBeHacked_Hide;
            }

            rejectionReason = "";
            return Hackable.CanBeHacked;
        }

        public override bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad TargetOrNull, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            TechUpgrade upgrade = TechUpgradeTable.Instance.GetRowByNameOrNullIfNotFound( Event.RelatedStringOrNull );
            if ( upgrade == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine(
                        "Could not find tech '" + (Event.RelatedStringOrNull == null ? "null" : Event.RelatedStringOrNull) + "' for Hacking_GrantTech.",
                        Verbosity.ShowAsError );
                return false;
            }

            Faction facOrNull = Hacker.GetFactionOrNull_Safe();
            if ( facOrNull != null )
            {
                int cost;
                ArcenRejectionReason rejectionReason = facOrNull.GetCanUnlockTech( upgrade, false, out cost, false );
                if ( rejectionReason == ArcenRejectionReason.Unknown )
                {
                    facOrNull.UnlockTech( upgrade, false, false );
                    Event.RelatedTech.Add( upgrade );
                }
            }

            return true;
        }

        public abstract void CalculateListOfTechs( List<TechUpgrade> ListToFill, GameEntity_Squad target, Faction hackerFaction );

        public override string GetDynamicDescription(
            GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            string output = "此黑客将让你从以下科技的升级中选择一项： ";

            List<TechUpgrade> techs = TechUpgrade.GetTemporaryTechUpgradeList( "Hacking_GrantTech-GetDynamicDescription-techs", 10f );
            if ( techs == null ) //blocked for teardown/shutdown; bail
                return null;
            CalculateListOfTechs( techs, target, hackerFaction );
            for ( int i = 0; i < techs.Count; i++ )
            {
                TechUpgrade tech = techs[i];
                if ( i > 0 )
                    output += ", ";
                output += tech.DisplayName;
            }

            output += ".";
            TechUpgrade.ReleaseTemporaryTechUpgradeList( techs );
            return output;
        }

        public override void PopulateItemsToShow( List<TechUpgrade> listToPopulate, GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, HackingType HackType )
        {
            Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
            CalculateListOfTechs( listToPopulate, TargetIfShip, localFaction );
        }
        
        public override TechUpgrade GetItemFromWrapper( TechUpgrade wrapper, out bool found )
        {
            found = true;
            return wrapper;
        }

        public override void GetDisplayNameForItem(ArcenCharacterBufferBase buffer, TechUpgrade item)
        {
            buffer.Add(item.DisplayName);
        }

        public override bool HasDetailsOfContents
        {
            get { return true; }
        }

        public override MouseHandlingResult ViewDetailsOfContents( TechUpgrade upgrade )
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null )
                return MouseHandlingResult.PlayClickDeniedSound;
            int upgradesSoFar = localFaction.TechUnlocks[upgrade.RowIndexNonSim] + localFaction.FreeTechUnlocks[upgrade.RowIndexNonSim];
            Window_InGameSidebarScience.btnScienceTech.ShowDetailsOfATechUpgradeContents( upgrade, 0, -1, upgradesSoFar );
            return MouseHandlingResult.None;
        }

        public override void DoHack(TechUpgrade upgrade, GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, HackingType HackType)
        {
            if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                return;

            string lastRejectionReason = ""; // FIXME: unused
            HackingUtils.TryDoHack( ref lastRejectionReason, TargetIfShip, TargetIfPlanet, HackType,
                    upgrade.InternalName, -1, null );
        }

        public override void GetTooltipForItem( ArcenCharacterBufferBase buffer, TechUpgrade upgrade, GameEntity_Squad Target, Planet planet, HackingType Type )
        {
            Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();

            int upgradesSoFar = localFaction.TechUnlocks[upgrade.RowIndexNonSim] + localFaction.FreeTechUnlocks[upgrade.RowIndexNonSim];

            Window_InGameSidebarScience.btnScienceTech.WriteTechTooltip( upgrade, upgradesSoFar, 0, -1, true, buffer );
        }
    }

    public class Hacking_GrantTech_TechVault : Hacking_GrantTech
    {
        public override bool WriteAnySpecialDisplayCodeForHackedShipTooltip(
            GameEntity_Squad Target, Faction HackerFaction, ArcenCharacterBufferBase Buffer, BaseTooltipDetail TooltipDetail )
        {
            if (EntityText.Use == WriterToUse.Formatted)
            {
                return Write_Formatted(Target, HackerFaction, Buffer);
            }

            var upgrades = TechUpgrade.GetTemporaryTechUpgradeList(
                "Hacking_GrantTech_TechVault-WriteAnySpecialDisplayCodeForHackedShipTooltip-upgrades", 10f );
            if ( upgrades == null ) //blocked for teardown/shutdown; bail
                return false;
            CalculateListOfTechs( upgrades, Target, HackerFaction );

            if ( upgrades.Count <= 0 )
            {
                TechUpgrade.ReleaseTemporaryTechUpgradeList( upgrades );
                //Buffer.Add( "ERROR: Found no available Tech(s) to grant. This is a bug, please report it with a savegame.", TooltipColors.Error );
                return true;
            }

            Buffer.Add( "从以下选择： " );

            for ( int i = 0; i < upgrades.Count; i++ )
            {
                if ( i > 0 )
                    Buffer.Add( ", " );

                string displayName = string.Empty;
                try
                {
                    displayName = upgrades[i].DisplayName;
                }
                catch
                {
                }

                Buffer.Add( displayName );
            }

            Buffer.Add( ". " );

            TechUpgrade.ReleaseTemporaryTechUpgradeList( upgrades );

            return true;
        }

        private bool Write_Formatted( GameEntity_Squad Target, Faction HackerFaction, ArcenCharacterBufferBase Buffer )
        {
            if ( Target.IsFakeEntity )
            {
                Buffer.Add( " <color=#cdcdcd><size=70%>从以下选择：</size></color> " );
                Buffer.Add( "3 个科技选项中的 1 个以供标记。" );
                return true;
            }

            var upgrades = TechUpgrade.GetTemporaryTechUpgradeList(
                "Hacking_GrantTech_TechVault-WriteAnySpecialDisplayCodeForHackedShipTooltip-upgrades", 10f );
            if ( upgrades == null ) //blocked for teardown/shutdown; bail
                return false;
            CalculateListOfTechs( upgrades, Target, HackerFaction );

            if ( upgrades.Count <= 0 )
            {
                TechUpgrade.ReleaseTemporaryTechUpgradeList( upgrades );
                //Buffer.Add( "ERROR: Found no available Tech(s) to grant. This is a bug, please report it with a savegame.", TooltipColors.Error );
                return true;
            }

            Buffer.Add( " <color=#cdcdcd><size=70%>choose from:</size></color> " );

            for ( int i = 0; i < upgrades.Count; i++ )
            {
                if ( i > 0 )
                    Buffer.Add( ", " );

                string displayName = string.Empty;
                try
                {
                    displayName = upgrades[i].DisplayName;
                }
                catch
                {
                }

                Buffer.Add( displayName );
            }

            Buffer.Add( " " );

            TechUpgrade.ReleaseTemporaryTechUpgradeList( upgrades );

            return true;
        }

        public override void CalculateListOfTechs( List<TechUpgrade> ListToFill, GameEntity_Squad target, Faction hackerFaction )
        {
            HackingUtils.CalculateListOfTechs_TechVault( ListToFill, target, hackerFaction );
        }
    }

    public class Hacking_DestabilizeWormhole : BaseHackingImplementation
    {
    }

    public class Hacking_TameTelium : BaseHackingImplementation
    {
        public override int EstimateTotalDifficulty( HackingType type, GameEntity_Squad TargetOrNull, Planet PlanetOrNull, out int totalResponseStrengthEstimate, out ArcenCharacterBuffer debugLogOrNull, bool assumeAIStrengthLevels )
        {
            debugLogOrNull = null;
            int strength = 0;
            GameEntityTypeData enragedHarvester = null;
            Faction facOrNull = TargetOrNull?.GetFactionOrNull_Safe();
            if ( facOrNull != null )
            {
                if ( facOrNull.HasObtainedSpireDebris ) // If the Wild Macrophage has acquired Debris from Fallen Spire, chance to spawn an Enraged Spire Harvester.
                {
                    // Get the highest strength Macrophage possible; overshoot the estimate.
                    GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( MacrophageFactionBaseInfo.EnragedHarvesterTag ).ForEach( workingHarvester =>
                    {
                        if ( enragedHarvester == null || workingHarvester.GetForMark( 1 ).StrengthPerSquad_CalculatedWithNullFleetMembership > enragedHarvester.GetForMark( 1 ).StrengthPerSquad_CalculatedWithNullFleetMembership )
                            enragedHarvester = workingHarvester;
                    } );
                }
                else
                {
                    // Get the highest strength Macrophage possible; overshoot the estimate.
                    GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( MacrophageFactionBaseInfo.RegularEnragedHarvesterTag ).ForEach(
                        workingHarvester =>
                        {
                            if ( enragedHarvester == null || workingHarvester.GetForMark( 1 ).StrengthPerSquad_CalculatedWithNullFleetMembership >
                                enragedHarvester.GetForMark( 1 ).StrengthPerSquad_CalculatedWithNullFleetMembership )
                                enragedHarvester = workingHarvester;
                        } );
                }
            }
            else
            {
                // Get the highest strength Macrophage possible; overshoot the estimate.
                GameEntityTypeDataTable.Instance.GetAllRowsWithTagOrNull( MacrophageFactionBaseInfo.RegularEnragedHarvesterTag ).ForEach(
                    workingHarvester =>
                    {
                        if ( enragedHarvester == null || workingHarvester.GetForMark( 1 ).StrengthPerSquad_CalculatedWithNullFleetMembership >
                            enragedHarvester.GetForMark( 1 ).StrengthPerSquad_CalculatedWithNullFleetMembership )
                            enragedHarvester = workingHarvester;
                    } );
            }

            if ( enragedHarvester == null )
            {
                // Can happen in early loading. Harmless.
                totalResponseStrengthEstimate = 0;
                return 0;
            }

            int duration = type.GetEffectiveHackDuration( TargetOrNull, PlanetOrNull );
            for ( int second = 0; second < duration; second++ )
            {
                if ( second % type.GetPrimaryHackResponseInterval() == 0 )
                    strength += enragedHarvester.GetForMark( (byte)Math.Min( 7, (second / type.GetPrimaryHackResponseInterval() * type.PrimaryResponseStrengthIncreasePerEffect).GetNearestIntPreferringHigher() ) ).StrengthPerSquad_CalculatedWithNullFleetMembership * type.PrimaryResponseStrengthPerInterval.GetNearestIntPreferringHigher();

                if ( second % type.GetSecondaryHackResponseInterval() == 0 )
                    strength += enragedHarvester.GetForMark( (byte)Math.Min( 7, (second / type.GetSecondaryHackResponseInterval() * type.SecondaryResponseStrengthIncreasePerEffect).GetNearestIntPreferringHigher() ) ).StrengthPerSquad_CalculatedWithNullFleetMembership * type.SecondaryResponseStrengthPerInterval.GetNearestIntPreferringHigher();

                if ( second % type.GetTertiaryHackResponseInterval() == 0 )
                    strength += enragedHarvester.GetForMark( (byte)Math.Min( 7, (second / type.GetTertiaryHackResponseInterval() * type.TertiaryResponseStrengthIncreasePerEffect).GetNearestIntPreferringHigher() ) ).StrengthPerSquad_CalculatedWithNullFleetMembership * type.TertiaryResponseStrengthPerInterval.GetNearestIntPreferringHigher();
            }

            totalResponseStrengthEstimate = strength;
            return strength;
        }

        public override Hackable GetCanBeHacked(
            GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull,
            int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Faction facOrNull = Target.GetFactionOrNull_Safe();
            if ( facOrNull != null )
            {
                MacrophageFactionBaseInfo macrophageBaseInfo = facOrNull.TryGetExternalBaseInfoAs<MacrophageFactionBaseInfo>();
                if ( macrophageBaseInfo != null )
                {
                    // Must not already be tamed, must have an existing tamed faction (no old saves allowed), and must not already be set to player allied in the lobby.
                    if ( Target.TypeData.GetHasTag( MacrophageFactionBaseInfoCore.TeliumTag ) && MacrophageTamedFactionBaseInfo.Instance != null &&
                         !macrophageBaseInfo.humanAllied )
                    {
                        RejectionReasonDescription = string.Empty;
                        return Hackable.CanBeHacked;
                    }
                }
            }


            RejectionReasonDescription = "The target is not a wild Telium.";
            return Hackable.NeverCanBeHacked_Hide;
        }

        public override void DoOneSecondOfHackingLogic_HackSpecificLogic(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            GameEntityTypeData enragedHarvester;
            Faction facOrNull = Hacker.GetFactionOrNull_Safe();
            if ( facOrNull != null )
            {
                if ( facOrNull.HasObtainedSpireDebris ) // If the Wild Macrophage has acquired Debris from Fallen Spire, chance to spawn an Enraged Spire Harvester.
                    enragedHarvester = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, MacrophageFactionBaseInfo.EnragedHarvesterTag );
                else
                    enragedHarvester = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, MacrophageFactionBaseInfo.RegularEnragedHarvesterTag );
            }
            else
                enragedHarvester = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, MacrophageFactionBaseInfo.RegularEnragedHarvesterTag );

            if ( Hacker.ActiveHack_DurationThusFar % type.GetPrimaryHackResponseInterval() == 0 )
            {
                for ( int x = 0; x < type.PrimaryResponseStrengthPerInterval; x++ )
                {
                    Faction spawnFaction = MacrophageEnragedFactionBaseInfo.Instance.AttachedFaction;
                    GameEntity_Squad guardianHarvester = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( planet.GetPlanetFactionForFaction( spawnFaction ),
                        enragedHarvester, (byte)Math.Min( 7, (Hacker.ActiveHack_DurationThusFar / type.GetPrimaryHackResponseInterval() * type.PrimaryResponseStrengthIncreasePerEffect).GetNearestIntPreferringHigher() ), planet.GetPlanetFactionForFaction( spawnFaction ).FleetUsedAtPlanet,
                        0, Target.WorldLocation, Context, "Hacking-TeliumReaction" );
                    if ( guardianHarvester != null )
                        guardianHarvester.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                }
            }

            if ( Hacker.ActiveHack_DurationThusFar % type.GetSecondaryHackResponseInterval() == 0 )
            {
                for ( int x = 0; x < type.SecondaryResponseStrengthPerInterval; x++ )
                {
                    Faction spawnFaction = MacrophageEnragedFactionBaseInfo.Instance.AttachedFaction;
                    GameEntity_Squad guardianHarvester = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( planet.GetPlanetFactionForFaction( spawnFaction ),
                        enragedHarvester, (byte)Math.Min( 7, (Hacker.ActiveHack_DurationThusFar / type.GetSecondaryHackResponseInterval() * type.SecondaryResponseStrengthIncreasePerEffect).GetNearestIntPreferringHigher() ), planet.GetPlanetFactionForFaction( spawnFaction ).FleetUsedAtPlanet,
                        0, Target.WorldLocation, Context, "Hacking-TeliumReaction" );
                    if ( guardianHarvester != null )
                        guardianHarvester.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                }
            }

            if ( Hacker.ActiveHack_DurationThusFar % type.GetTertiaryHackResponseInterval() == 0 )
            {
                for ( int x = 0; x < type.TertiaryResponseStrengthPerInterval; x++ )
                {
                    Faction spawnFaction = MacrophageEnragedFactionBaseInfo.Instance.AttachedFaction;
                    GameEntity_Squad guardianHarvester = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( planet.GetPlanetFactionForFaction( spawnFaction ),
                        enragedHarvester, (byte)Math.Min( 7, (Hacker.ActiveHack_DurationThusFar / type.GetTertiaryHackResponseInterval() * type.TertiaryResponseStrengthIncreasePerEffect).GetNearestIntPreferringHigher() ), planet.GetPlanetFactionForFaction( spawnFaction ).FleetUsedAtPlanet,
                        0, Target.WorldLocation, Context, "Hacking-TeliumReaction" );
                    if ( guardianHarvester != null )
                        guardianHarvester.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                }
            }
        }

        public override bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, MacrophageFactionBaseInfoCore.TeliumTag );
            ArcenPoint spawnLocation = Target.WorldLocation;
            Faction tamedFaction = MacrophageTamedFactionBaseInfo.Instance.AttachedFaction;
            PlanetFaction pFaction = Target.Planet.GetPlanetFactionForFaction( tamedFaction );
            if ( entityData == null )
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: no Telium tag found", Verbosity.DoNotShow );
            GameEntity_Squad.CreateNew_ReturnNullIfMPClient(
                pFaction, entityData, entityData.MarkFor( pFaction ), pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "Hacking-TeliumPostReaction" );
            Target.Despawn( Context, true, InstancedRendererDeactivationReason.IAmTransforming );

            return true;
        }
    }
}
