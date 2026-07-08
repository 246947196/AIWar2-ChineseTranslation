using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class Hacking_BreachNexus : HackingImplementation_WithMenu<MalwareBreach, MalwareBreach>
    {
        [ThreadStatic]
        private ArcenCharacterBuffer _buffer;

        private ArcenCharacterBuffer Buffer
        {
            get
            {
                if (_buffer == null)
                    _buffer = ArcenCharacterBuffer.Create_WillNeverBeGC();

                _buffer.Clear();
                return _buffer;
            }
        }

        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            ApkalluFactionBaseInfo apkalluBase = HackerFaction.TryGetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if ( apkalluBase == null )
            {
                RejectionReasonDescription = "只有 Apkallu 才能入侵 Nexus。";
                return Hackable.NeverCanBeHacked_Hide;
            }

            bool flagshipOnPlanet = false;
            foreach ( GameEntity_Squad flagship in apkalluBase.Flagships.DisplaySquads() )
            {
                if ( flagship.Planet == Target.Planet )
                {
                    flagshipOnPlanet = true;
                    break;
                }
            }

            if ( !flagshipOnPlanet )
            {
                RejectionReasonDescription = "Apkallu 旗舰必须在此星球上才能入侵 Nexus。";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }

        public override MalwareBreach GetItemFromWrapper( MalwareBreach wrapper, out bool found )
        {
            found = true;
            return wrapper;
        }

        public override void PopulateItemsToShow( List<MalwareBreach> listToPopulate, GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, HackingType HackType )
        {
            MalwarePerUnitBaseInfo data = TargetIfShip?.TryGetExternalBaseInfoAs<MalwarePerUnitBaseInfo>();
            if ( data == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Null MalwarePerUnitBaseInfo on " + TargetIfShip?.ToStringWithPlanetAndOwner(), Verbosity.DoNotShow );
                return;
            }
            listToPopulate.CopyFrom( data.Breaches );
        }

        public override Hackable GetCanHackForThisItem( MalwareBreach item, out string rejectionReason, GameEntity_Squad Target, Planet planet, HackingType Type )
        {
            MalwareFactionBaseInfo malwareBase = Target?.PlanetFaction?.Faction?.TryGetExternalBaseInfoAs<MalwareFactionBaseInfo>();
            if ( malwareBase != null && malwareBase.HasActiveBreach( planet ) )
            {
                rejectionReason = "此星球已有一个活跃的入侵。";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            if ( item.AdditionalHapCost > 0 || item.AdditionalResourceOneCost > 0 ||
                 item.AdditionalResourceTwoCost > 0 || item.AdditionalResourceThreeCost > 0 )
            {
                Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                if ( localFaction != null )
                {
                    FInt totalHap = Type.GetHackPointCostForTarget( Target ) + (FInt)item.AdditionalHapCost * Type.GetHapCostScale( Target, true );
                    bool hapShort   = item.AdditionalHapCost          > 0 && totalHap                          > localFaction.StoredHacking;
                    bool res1Short  = item.AdditionalResourceOneCost   > 0 && (FInt)item.AdditionalResourceOneCost   > localFaction.StoredFactionResourceOne;
                    bool res2Short  = item.AdditionalResourceTwoCost   > 0 && (FInt)item.AdditionalResourceTwoCost   > localFaction.StoredFactionResourceTwo;
                    bool res3Short  = item.AdditionalResourceThreeCost > 0 && (FInt)item.AdditionalResourceThreeCost > localFaction.StoredFactionResourceThree;

                    if ( hapShort || res1Short || res2Short || res3Short )
                    {
                        PlayerTypeData pData = localFaction.PlayerTypeDataOrNull_ModeratelyExpensive;

                        string res1Name = pData?.Resource1DisplayName ?? "资源";
                        string res2Name = pData?.Resource2DisplayName ?? "资源";
                        string res3Name = pData?.Resource3DisplayName ?? "资源";
                        var buffer = this.Buffer;
                        buffer.Add( "不足 " );
                        bool first = true;
                        if ( hapShort )  { if ( !first ) buffer.Add( ", " ); buffer.StartHacking( false ).Add( "黑客点" ).EndColor();   first = false; }
                        if ( res1Short ) { if ( !first ) buffer.Add( ", " ); buffer.StartResourceOne( false ).Add( res1Name ).EndColor(); first = false; }
                        if ( res2Short ) { if ( !first ) buffer.Add( ", " ); buffer.AddResourceTwo( res2Name, true );                        first = false; }
                        if ( res3Short ) { if ( !first ) buffer.Add( ", " ); buffer.AddResourceThree( res3Name, true );                     first = false; }
                        rejectionReason = buffer.ToString();
                        return Hackable.NeverBeHacked_ButStillShow;
                    }
                }
            }

            rejectionReason = "";
            return Hackable.CanBeHacked;
        }
        // public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
        // {

        // }
        public override void GetDisplayNameForItem( ArcenCharacterBufferBase Buffer, MalwareBreach breach )
        {
            Buffer.Add( breach.DisplayName );
        }

        public override void GetExtraLabelInfoForItem( ArcenCharacterBufferBase Buffer, MalwareBreach breach, GameEntity_Squad Target, Planet planet, HackingType HackType )
        {
            if ( breach.Difficulty != MalwareBreachDifficulty.None )
            {
                string color;
                switch ( breach.Difficulty )
                {
                    case MalwareBreachDifficulty.Trivial:  color = "#aaffaa"; break;
                    case MalwareBreachDifficulty.Easier:   color = "#88ddff"; break;
                    case MalwareBreachDifficulty.Moderate: color = "#ffff88"; break;
                    case MalwareBreachDifficulty.Difficult: color = "#ffaa44"; break;
                    default:                               color = "#ff6666"; break; // Brutal
                }
                Buffer.Add( "   " );
                Buffer.Add( "<color=" ).Add( color ).Add( ">" );
                Buffer.Add( breach.Difficulty.ToString() );
                Buffer.Add( " (" ).Add( breach.TotalEnemyMetal ).Add( ")" );
                Buffer.Add( "</color>" );
            }

            bool hasAnyCost = breach.AdditionalAIPCost > 0 || breach.AdditionalHapCost > 0 ||
                              breach.AdditionalResourceOneCost > 0 || breach.AdditionalResourceTwoCost > 0 ||
                              breach.AdditionalResourceThreeCost > 0;
            if ( !hasAnyCost )
                return;
            Buffer.Add( "   " );
            if ( breach.AdditionalAIPCost > 0 )
                Buffer.AddAIP( breach.AdditionalAIPCost, true );
            if ( breach.AdditionalHapCost > 0 )
                Buffer.AddHacking( (int)( breach.AdditionalHapCost * HackType.GetHapCostScale( Target, true ) ), true );
            if ( breach.AdditionalResourceOneCost > 0 )
                Buffer.AddResourceOne( breach.AdditionalResourceOneCost, true );
            if ( breach.AdditionalResourceTwoCost > 0 )
                Buffer.AddResourceTwo( breach.AdditionalResourceTwoCost, true );
            if ( breach.AdditionalResourceThreeCost > 0 )
                Buffer.AddResourceThree( breach.AdditionalResourceThreeCost, true );
        }

        public override void GetTooltipForItem( ArcenCharacterBufferBase buffer, MalwareBreach breach, GameEntity_Squad Target, Planet planet, HackingType Type )
        {
            buffer.Add( breach.Description );
            if ( !string.IsNullOrEmpty( breach.StartText ) )
            {
                buffer.Add( "\n\n" );
                buffer.Add( breach.StartText );
            }

            if ( breach.AdditionalAIPCost > 0 || breach.AdditionalHapCost > 0 ||
                 breach.AdditionalResourceOneCost > 0 || breach.AdditionalResourceTwoCost > 0 ||
                 breach.AdditionalResourceThreeCost > 0 )
            {
                buffer.Pad().NewLineIfNeeded().Add( "此黑客花费 " );
                int counter = 0;
                if ( breach.AdditionalAIPCost > 0 )
                {
                    if ( counter > 0 ) buffer.Add( Text.ThinSpace );
                    counter++;
                    buffer.AddAIP( breach.AdditionalAIPCost, true );
                }
                if ( breach.AdditionalHapCost > 0 )
                {
                    if ( counter > 0 ) buffer.Add( Text.ThinSpace );
                    counter++;
                    buffer.AddHacking( (int)( breach.AdditionalHapCost * Type.GetHapCostScale( Target, true ) ), true );
                }
                if ( breach.AdditionalResourceOneCost > 0 )
                {
                    if ( counter > 0 ) buffer.Add( Text.ThinSpace );
                    counter++;
                    buffer.AddResourceOne( breach.AdditionalResourceOneCost, true );
                }
                if ( breach.AdditionalResourceTwoCost > 0 )
                {
                    if ( counter > 0 ) buffer.Add( Text.ThinSpace );
                    counter++;
                    buffer.AddResourceTwo( breach.AdditionalResourceTwoCost, true );
                }
                if ( breach.AdditionalResourceThreeCost > 0 )
                {
                    if ( counter > 0 ) buffer.Add( Text.ThinSpace );
                    counter++;
                    buffer.AddResourceThree( breach.AdditionalResourceThreeCost, true );
                }
                buffer.Add( "\n" );
            }

            if ( breach.UnlockRequirements.Count > 0 )
                buffer.Add("\n解锁要求：\n");
            for ( int i = 0; i < breach.UnlockRequirements.Count; i++ )
            {
                buffer.Add("\t" + breach.UnlockRequirements[i]);
            }

            if ( breach.ExcludedIfCompleted.Count > 0 )
                buffer.Add("\n如果你已完成这些入侵，此入侵将不会出现：\n");
            for ( int i = 0; i < breach.ExcludedIfCompleted.Count; i++ )
            {
                buffer.Add("\t" + breach.ExcludedIfCompleted[i]);
            }
        }

        public override void DoHack( MalwareBreach breach, GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, HackingType HackType )
        {
            string lastRejectionReason = "";
            HackingUtils.TryDoHack( ref lastRejectionReason, TargetIfShip, TargetIfPlanet, HackType,
                breach.name, -1, null );
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            MalwareFactionBaseInfo malwareBase = Target.PlanetFaction.Faction.TryGetExternalBaseInfoAs<MalwareFactionBaseInfo>();
            if ( malwareBase == null )
                return false;

            MalwarePerUnitBaseInfo nexusData = Target.TryGetExternalBaseInfoAs<MalwarePerUnitBaseInfo>();
            if ( nexusData == null )
                throw new Exception( "Tried to breach nexus " + Target.ToStringWithPlanetAndOwner() + " but it had no MalwarePerUnitBaseInfo." );

            MalwareBreach breach = null;
            if ( !string.IsNullOrEmpty( Event.RelatedStringOrNull ) )
                breach = nexusData.Breaches.Find( b => b.name == Event.RelatedStringOrNull );

            if ( breach == null )
                throw new Exception( "Could not find breach to execute! We were looking for " + Event.RelatedStringOrNull + "." );

            if ( breach.AdditionalAIPCost > 0 )
            {
                Event.AIPchanged += (FInt)breach.AdditionalAIPCost;
                GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)breach.AdditionalAIPCost, AIPChangeReason.Hacking, Target.TypeData, null, Hacker.GetFactionIndex_Safe(), planet.Index, -1 );
            }
            if ( breach.AdditionalHapCost > 0 )
            {
                FInt hapAdded = (FInt)breach.AdditionalHapCost * type.GetHapCostScale( Target, true );
                Event.HackingPointsSpent += hapAdded;
                Event.HackingPointsLeftAfterHack -= hapAdded;
                Hacker.PlanetFaction.Faction.StoredHacking -= hapAdded;
                Target.GetFactionOrNull_Safe().HackingPointsUsedAgainstThisFaction += hapAdded;
            }
            if ( breach.AdditionalResourceOneCost > 0 )
                Hacker.PlanetFaction.Faction.StoredFactionResourceOne -= breach.AdditionalResourceOneCost;
            if ( breach.AdditionalResourceTwoCost > 0 )
                Hacker.PlanetFaction.Faction.StoredFactionResourceTwo -= (FInt)breach.AdditionalResourceTwoCost;
            if ( breach.AdditionalResourceThreeCost > 0 )
                Hacker.PlanetFaction.Faction.StoredFactionResourceThree -= (FInt)breach.AdditionalResourceThreeCost;

            malwareBase.OnNexusBreached( Target, planet, Hacker, Context, breach );
            return true;
        }
    }

    public class Hacking_MultiPhasicConduit : BaseHackingImplementation
    {
        public override void DoOneSecondOfHackingLogic_HackSpecificLogic(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            if ( Hacker.ActiveHack_DurationThusFar == 1 )
            {
                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "Apkallu 联络",
                    "我们……感知到你了。请保持你的黑客信标激活，以便我们逃脱。\n\n\n<size=80%>详情请见日志。</size>", "确定" );
                Faction apkallu = FactionUtilityMethods.Instance.GetApkalluFaction();
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "Apkallu_FirstContact", string.Empty, apkallu, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }

        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            Faction apkalluFaction = FactionUtilityMethods.Instance.GetApkalluFaction();
            ApkalluFactionBaseInfo apkalluBase = apkalluFaction?.TryGetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if ( apkalluBase != null )
                apkalluBase.DuruSpawnTime = World_AIW2.Instance.GameSecond + 1;
            return true;
        }
    }
    public class Hacking_CorruptedZigguratCapture : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet,
            Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull,
            out string RejectionReasonDescription )
        {
            if ( Target.CurrentStateOfMatter == StateOfMatterTypeDataTable.Instance.DefaultRow )
            {
                RejectionReasonDescription = "此 Ziggurat 已被黑客入侵";
                return Hackable.NeverCanBeHacked_Hide;
            }
            ApkalluFactionBaseInfo apkalluBase = HackerFaction.TryGetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if ( apkalluBase != null && apkalluBase.Durus.GetDisplayList().Count == 0 )
            {
                RejectionReasonDescription = "你不知道如何与这个奇怪的外星建筑互动。";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra(
            GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            if ( Target == null )
                return false;
            // Exit solo-phase: the Corrupted Ziggurat is now a combat entity the player must defeat
            Target.CurrentStateOfMatter = StateOfMatterTypeDataTable.Instance.DefaultRow;
            Target.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
            return true;
        }
    }

    /// <summary>
    /// Hack performed on the Lamassu to resync its weapon modules with the current
    /// Ziggurat Summoner Structures on this planet. Only usable on planets that have
    /// an Apkallu Ziggurat.
    /// </summary>
    public class Hacking_RefreshLamassuModules : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet,
            Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull,
            out string RejectionReasonDescription )
        {
            if ( !Target.TypeData.GetHasTag( "ApkalluLamassu" ) )
            {
                RejectionReasonDescription = string.Empty;
                return Hackable.NeverCanBeHacked_Hide;
            }
            ApkalluFactionBaseInfo apkalluBase = HackerFaction.TryGetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if ( apkalluBase == null || !apkalluBase.DoesPlanetHaveZiggurat( planet ) )
            {
                RejectionReasonDescription = "此星球上必须有一座 Ziggurat 才能重新同步 Lamassu 的模块。";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            if ( !apkalluBase.LamassuPrimaryKeyIDsOutOfSync.Contains( Target.PrimaryKeyID ) )
            {
                RejectionReasonDescription = string.Empty;
                return Hackable.NeverCanBeHacked_Hide;
            }
            RejectionReasonDescription = string.Empty;
            return Hackable.CanBeHacked;
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet,
            GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            ApkalluFactionBaseInfo apkalluBase = Hacker.PlanetFaction.Faction.TryGetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if ( apkalluBase != null && !apkalluBase.LamassusNeedingModuleRefresh.Contains( Target.PrimaryKeyID ) )
                apkalluBase.LamassusNeedingModuleRefresh.Add( Target.PrimaryKeyID );
            return true;
        }
    }

    public class Hacking_TransformApkalluFlagship : HackingImplementation_WithMenu<MalwareBreach, MalwareBreach>
    {
        public override MalwareBreach GetItemFromWrapper( MalwareBreach wrapper, out bool found )
        {
            found = true;
            return wrapper;
        }

        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( !ApkalluFactionBaseInfo.GetIsThisAnApkalluFaction( HackerFaction ) )
            {
                RejectionReasonDescription = "只有 Apkallu 才能转换他们的旗舰。";
                return Hackable.NeverCanBeHacked_Hide;
            }
            ApkalluFactionBaseInfo apkalluInfo = HackerFaction.TryGetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            bool anyUnlocked = false;
            if ( apkalluInfo != null )
            {
                for ( int i = 0; i < apkalluInfo.CompletedBreaches.Count; i++ )
                {
                    if ( apkalluInfo.CompletedBreaches[i]?.UnlockFlagshipForm != null )
                    {
                        anyUnlocked = true;
                        break;
                    }
                }
            }
            if ( !anyUnlocked )
            {
                RejectionReasonDescription = "尚未解锁旗舰形态。完成 Malware Nexus 入侵以解锁形态。";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( Target.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength > 500 )
            {
                RejectionReasonDescription = "无法在有大量敌人力量的行星上进行转换。";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }

        public override void PopulateItemsToShow( List<MalwareBreach> listToPopulate, GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, HackingType HackType )
        {
            Faction faction = TargetIfShip?.PlanetFaction?.Faction;
            ApkalluFactionBaseInfo apkalluInfo = faction?.TryGetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if ( apkalluInfo == null )
                return;
            for ( int i = 0; i < apkalluInfo.CompletedBreaches.Count; i++ )
            {
                MalwareBreach breach = apkalluInfo.CompletedBreaches[i];
                if ( breach?.UnlockFlagshipForm == null )
                    continue;
                if ( TargetIfShip != null && breach.UnlockFlagshipForm == TargetIfShip.TypeData )
                    continue; // don't show current form
                listToPopulate.Add( breach );
            }
        }

        public override Hackable GetCanHackForThisItem( MalwareBreach breach, out string rejectionReason, GameEntity_Squad Target, Planet planet, HackingType Type )
        {
            if ( Target != null && breach.FlagshipFormTransformMinMarkLevel > 1 && Target.CurrentMarkLevel < breach.FlagshipFormTransformMinMarkLevel )
            {
                rejectionReason = "需要旗舰标记等级 " + breach.FlagshipFormTransformMinMarkLevel + "。";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            Faction faction = Target?.PlanetFaction?.Faction;
            if ( faction != null && faction.StoredFactionResourceOne < breach.FlagshipFormTransformCostResourceOne )
            {
                rejectionReason = "资源一不足（需要 " + breach.FlagshipFormTransformCostResourceOne + "）。";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            rejectionReason = "";
            return Hackable.CanBeHacked;
        }

        public override void GetDisplayNameForItem( ArcenCharacterBufferBase buffer, MalwareBreach breach )
        {
            buffer.Add( breach.UnlockFlagshipForm?.DisplayName ?? breach.DisplayName );
        }

        public override void GetExtraLabelInfoForItem( ArcenCharacterBufferBase buffer, MalwareBreach breach, GameEntity_Squad Target, Planet planet, HackingType Type )
        {
            if ( breach.FlagshipFormTransformCostResourceOne > 0 )
            {
                buffer.Add( "   " );
                buffer.AddResourceOne( breach.FlagshipFormTransformCostResourceOne, true );
            }
            if ( breach.FlagshipFormTransformMinMarkLevel > 1 )
                buffer.Add( "  [Mk" ).Add( breach.FlagshipFormTransformMinMarkLevel ).Add( "]" );
        }

        public override void GetTooltipForItem( ArcenCharacterBufferBase buffer, MalwareBreach breach, GameEntity_Squad Target, Planet planet, HackingType Type )
        {
            if ( breach.UnlockFlagshipForm != null )
                buffer.Add( breach.UnlockFlagshipForm.Description );
            if ( breach.FlagshipFormTransformCostResourceOne > 0 )
            {
                buffer.Add( "\n\n转换成本： " );
                buffer.AddResourceOne( breach.FlagshipFormTransformCostResourceOne, true );
            }
            if ( breach.FlagshipFormTransformMinMarkLevel > 1 )
                buffer.Add( "\n需要旗舰标记等级 " ).Add( breach.FlagshipFormTransformMinMarkLevel ).Add( "。" );
        }

        public override void DoHack( MalwareBreach breach, GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, HackingType HackType )
        {
            if ( TargetIfShip == null )
                return;
            GameCommand command = GameCommand.Create(
                BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransformApkalluFlagship],
                GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            command.RelatedEntityIDs.Add( TargetIfShip.PrimaryKeyID );
            command.RelatedString2 = breach.name;
            World_AIW2.Instance.QueueGameCommand(
                World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
        }
    }

    public class Hacking_ClaimTemen : BaseHackingImplementation
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            //return "\nIn particular, this hack will upgrade <color=#a1ffa1>" + target.FleetMembership.Fleet.GetName() + "</color> to mark level " + (target.CurrentMarkLevel + 1) + ".";
            return "";
        }
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( Target.PlanetFaction.Faction == HackerFaction )
            {
                RejectionReasonDescription = "你已占领此建筑";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( planet.GetControllingFaction().GetIsHostileTowards( HackerFaction ) )
            {
                RejectionReasonDescription = "星球被敌人控制";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            ApkalluFactionBaseInfo apkalluBase = HackerFaction.TryGetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
            if ( apkalluBase == null || apkalluBase.Ziggurats.Count == 0 )
            {
                RejectionReasonDescription = "Temen 需要 Ziggurat 才能运作";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            //CALL SpawnApkalluTemen
            Faction hackFaction = Hacker.PlanetFaction.Faction;
            GameEntityTypeData typeData = Target.TypeData;
            ArcenPoint spawnLocation = Target.WorldLocation;
            PlanetFaction pFaction = Hacker.PlanetFaction;
            Target.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
            
            GameEntity_Squad temen = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typeData, 1,
                pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Apkallu-ClaimTemenViaHacking" );


            Fleet newApkalluStarbaseFleet = temen.FleetMembership.Fleet;


            newApkalluStarbaseFleet.NameRaw = "Temen " + planet.Name;
            newApkalluStarbaseFleet.FleetQualifier = "Apkallu";

            ApkalluCityFleetBaseInfo apkalluCityFleetInfo = newApkalluStarbaseFleet.CreateExternalBaseInfo<ApkalluCityFleetBaseInfo>("ApkalluCityFleetBaseInfo");

            apkalluCityFleetInfo.CanChangeBolsteredFleet = true;
            return true;
        }
    }
}
