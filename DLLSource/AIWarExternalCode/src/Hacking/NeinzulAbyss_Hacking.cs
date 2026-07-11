using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{ 
    /* Necromancer Hacks */
    public class Hacking_UpgradeNecromancerUnit : BaseHackingImplementation
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            return "\nIn particular, this hack will upgrade <color=#a1ffa1>" + target.FleetMembership.Fleet.GetName() + "</color> to mark level " + (target.CurrentMarkLevel + 1) + ".";
        }
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( Target.CurrentMarkLevel >= 7 )
            {
                RejectionReasonDescription = "此 " + Target.TypeData.GetDisplayName() +" 已处于最高等级";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            Fleet targetFleet = Target.GetFleetOrNull_Safe();
            if ( targetFleet == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Could not find targetFleet!", Verbosity.ShowAsError );
                return false;
            }
            targetFleet.AddedMarkLevelsForFleet_FromScience++;
            Target.SetCurrentMarkLevel( (byte)(Target.CurrentMarkLevel + 1) ); //make the upgrade happen faster
            return true;
        }
    }
    public class Hacking_ClaimAmplifier : BaseHackingImplementation
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
                RejectionReasonDescription = "你已经认领了此建筑";
                return Hackable.NeverCanBeHacked_Hide;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event ){
            NecromancerEmpireFactionBaseInfo baseInfo = Hacker.PlanetFaction.Faction.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            GameEntity_Squad necropolis = null;
            foreach ( GameEntity_Squad city in baseInfo.Necropoleis.DisplaySquads() )
            {
                if ( city.Planet == Target.Planet )
                {
                    necropolis = city;
                    break;
                }
            }
            if ( necropolis != null )
            {
                GameEntity_Squad amplifier = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( necropolis.PlanetFaction, Target.TypeData, 1,
                                                                                              necropolis.FleetMembership.Fleet, 0, Target.WorldLocation, Context, "Necromancer-HackedAmplifier" );
                Target.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
            }
            else
                Target.PlanetFaction.SwitchToFaction( Target, Hacker.PlanetFaction, false, "Amplifier Was Hacked" );

            return true;
        }
    }
    public class Hacking_ClaimShowdownDevice : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( HackerOrNull != null &&
                 Target.PlanetFaction.Faction == HackerOrNull.PlanetFaction.Faction )
            {
                RejectionReasonDescription = "你已经认领了此建筑";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( HackerOrNull != null && NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( HackerOrNull.PlanetFaction.Faction ) )
            {
                GameEntity_Squad necropolis = null;
                NecromancerEmpireFactionBaseInfo baseInfo = HackerOrNull.PlanetFaction.Faction.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
                foreach ( GameEntity_Squad city in baseInfo.Necropoleis.DisplaySquads() )
                {
                    if ( city.Planet == Target.Planet )
                    {
                        necropolis = city;
                        break;
                    }
                }
                if ( necropolis == null )
                {
                    RejectionReasonDescription = "你必须在此星球上有一座死灵城";
                    return Hackable.NeverCanBeHacked_Hide;
                }
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event ){
            GameEntity_Squad necropolis = null;
            NecromancerEmpireFactionBaseInfo baseInfo = Hacker.PlanetFaction.Faction.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            foreach ( GameEntity_Squad city in baseInfo.Necropoleis.DisplaySquads() )
            {
                if ( city.Planet == Target.Planet )
                {
                    necropolis = city;
                    break;
                }
            }
            if ( necropolis != null )
            {
                GameEntity_Squad device = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( necropolis.PlanetFaction, Target.TypeData, 1,
                                                                                       necropolis.FleetMembership.Fleet, 0, Target.WorldLocation, Context, "Necromancer-ClaimShowdownDevice" );
                Target.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
            }

            return true;
        }
    }
    public class Hacking_AbandonShowdownDevice : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( HackerOrNull != null &&
                 Target.PlanetFaction.Faction != HackerOrNull.PlanetFaction.Faction )
            {
                RejectionReasonDescription = "你必须已经拥有此建筑";
                return Hackable.NeverCanBeHacked_Hide;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event ){

            Target.PlanetFaction.SwitchToFaction( Target, Target.Planet.GetPlanetFactionForFaction( World_AIW2.Instance.GetNeutralFaction() ), false, "Showdown Devic Was Abandoned" );

            return true;
        }
    }
    public class Hacking_MovePhylactery : BaseHackingImplementation
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            //return "\nIn particular, this hack will upgrade this " + target.TypeData.GetDisplayName() + " to mark level " + (target.CurrentMarkLevel + 1) + ".";
            return "";
        }
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( Target == null )
            {
                RejectionReasonDescription = "No phylactery?";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            Faction faction = Target.PlanetFaction.Faction;
            if ( !NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( faction ) )
                throw new Exception("The Phylactery hack was against " + Target.ToStringWithPlanetAndOwner() + " faction " + faction.GetDisplayName() + ", and should only have been against a necromancer phylactery");
            NecromancerEmpireFactionBaseInfo baseInfo = Target.PlanetFaction.Faction.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            bool foundValidNecropolis = false;
            foreach ( GameEntity_Squad city in baseInfo.Necropoleis.DisplaySquads() )
            {
                if ( city.TypeData.GetHasTag("Phylactery") )
                    continue;
                if ( city.GetIsCrippled() )
                    continue;
                foundValidNecropolis = true;
            }
            if ( !foundValidNecropolis )
            {
                RejectionReasonDescription = "你必须在某处有一座未残废的死灵城来交换此圣匣";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            Fleet targetFleet = Target.GetFleetOrNull_Safe();
            if ( targetFleet == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Could not find targetFleet!", Verbosity.ShowAsError );
                return false;
            }
            GameEntity_Squad swappingNecropolis = World_AIW2.Instance.GetEntityByID_Squad(Event.RelatedIntOrNull);
            if ( swappingNecropolis == null )
                throw new Exception("No target found for the phylactery swap");
            NecromancerEmpireFactionBaseInfo baseInfo = Target.PlanetFaction.Faction.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            baseInfo.SwapNecropoleis( Target, swappingNecropolis, Context );
            return true;
        }
    }
    public class Hacking_MoveNecropolis : BaseHackingImplementation
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            //return "\nIn particular, this hack will upgrade this " + target.TypeData.GetDisplayName() + " to mark level " + (target.CurrentMarkLevel + 1) + ".";
            return "";
        }
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( Target == null )
            {
                RejectionReasonDescription = "No necropolis?";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            if ( !NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( Target.PlanetFaction.Faction ) )
            {
                RejectionReasonDescription = "这不是死灵法师目标";
                return Hackable.NeverCanBeHacked_Hide;
            }
            NecromancerEmpireFactionBaseInfo baseInfo = Target.PlanetFaction.Faction.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            bool foundValidNecropolis = false;
            foreach ( GameEntity_Squad city in baseInfo.Necropoleis.DisplaySquads() )
            {
                if ( city.TypeData.GetHasTag("Phylactery") )
                    continue;
                if ( city.GetIsCrippled() )
                    continue;
                foundValidNecropolis = true;
            }
            if ( !foundValidNecropolis )
            {
                RejectionReasonDescription = "你必须在某处有一座未残废的死灵城来交换此死灵城";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            Fleet targetFleet = Target.GetFleetOrNull_Safe();
            if ( targetFleet == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Could not find targetFleet!", Verbosity.ShowAsError );
                return false;
            }
            GameEntity_Squad swappingNecropolis = World_AIW2.Instance.GetEntityByID_Squad(Event.RelatedIntOrNull);
            if ( swappingNecropolis == null )
                throw new Exception("No target found for the phylactery swap; we were looking for " + Event.RelatedIntOrNull);
            NecromancerEmpireFactionBaseInfo baseInfo = Target.PlanetFaction.Faction.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            baseInfo.SwapNecropoleis( Target, swappingNecropolis, Context );
            return true;
        }
    }
    public class Hacking_TransformNecromancerFlagship : BaseHackingImplementation
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            return "此入侵将改造 <color=#a1a1a1>" + target.FleetMembership.Fleet.GetName() + "</color> (" + target.TypeData.GetDisplayName() + ")";
        }

        public override void GetMinAndMaxCostToHackForSidebar( GameEntity_Squad Target, Planet planet, Faction hackerFaction, HackingType Type, out FInt MinCost, out FInt MaxCost )
        {
            if (!NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction(Target.PlanetFaction.Faction))
            {
                //Only the necromancer can see these hacks
                MinCost = FInt.Zero;
                MaxCost = FInt.Zero;
                return;
            }

            NecromancerEmpireFactionBaseInfo baseInfo = Target.PlanetFaction.Faction.TryGetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            List<NecromancerUpgrade> workingBlueprints = baseInfo.AvailableBlueprints.GetDisplayList();

            if (workingBlueprints.Count == 0) {
                MinCost = FInt.Zero;
                MaxCost = FInt.Zero;
                return;
            }

            MaxCost = FInt.Zero;
            MinCost = (FInt)999;
            foreach (NecromancerUpgrade upgrade in workingBlueprints) {
                if ( upgrade.BlueprintTransformCostInHacking > 0 ) {
                    if ( upgrade.BlueprintTransformCostInHacking < MinCost )
                        MinCost = upgrade.BlueprintTransformCostInHacking;
                    if ( upgrade.BlueprintTransformCostInHacking > MaxCost )
                        MaxCost = upgrade.BlueprintTransformCostInHacking;
                }
            }
        }

        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( HackerOrNull == null )
            {
                RejectionReasonDescription = "There are no units that can hack on this planet";
                return Hackable.NeverCanBeHacked_Hide;
            }

            if ( !NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( HackerOrNull.PlanetFaction.Faction ) )
            {
                //Only the necromancer can see these hacks
                RejectionReasonDescription = "Only the necromancer can hack";
                return Hackable.NeverCanBeHacked_Hide;
            }

            NecromancerEmpireFactionBaseInfo gData = HackerOrNull.PlanetFaction.Faction.TryGetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            int count = gData.AvailableBlueprints.GetDisplayList().Count;

            if ( count <= 0 )
            {
                //Hide until you have some blueprints
                RejectionReasonDescription = "No blueprints!";
                return Hackable.NeverCanBeHacked_Hide;
            }

            if ( Target.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength > 500 )
            {
                RejectionReasonDescription = "你无法在敌方力量显著的星球上进行改造。";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            RejectionReasonDescription = "";
            return Hackable.CanBeHacked;
        }
        public override bool GetDoesHackRequireASinglularChoiceFromASubmenu()
        {
            return true;
        }
        public override void DoOneSecondOfHackingLogic_HackSpecificLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            // Handle hacking reaction manually, forcing the faction to be an ai faction.
            int secondsSoFar = Hacker.ActiveHack_DurationThusFar;
            if ( secondsSoFar == 0 )
                return;

            Faction targetFaction = Target.PlanetFaction.Faction;
            if ( targetFaction == null ) // Shouldn't be able to occur, but you never know.
                return;
            if ( Hacker.ActiveHack_DurationThusFar % type.GetPrimaryHackResponseInterval() == 0 )
                targetFaction.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, FInt.One, Context, Event, targetFaction );
        }

        private ArcenCachedExternalTypeDirect type_bCustomHackingOptionButton = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bCustomHackingOptionButton ) );
        private static readonly List<NecromancerUpgrade> listOfItemsLastUsedToPopulateCustomButton = List<NecromancerUpgrade>.Create_WillNeverBeGCed( 60, "Hacking_TransformNecromancerFlagship-listOfItemsLastUsedToPopulateCustomButton" );
        private static Hacking_TransformNecromancerFlagship Implementation = null;
        private static HackCustomButtonListInfo Info = new HackCustomButtonListInfo();
        public override void AddAllButtonsForSingularChoiceInSubMenu( GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, Faction HackerFaction, HackingType HackType, ArcenUI_SetOfCreateElementDirectives Set,                                                                                                                                                            ref float runningY, UnityEngine.Rect firstBounds, float rowBuffer, AddHackButtonToWindow WindowAdder )
        {
            Implementation = this;
            //TODO: make sure we can add blueprints for the NecromancerPerUnitData
            //then show a list of the available blueprints for transformation
            //once we pick then we turn our flagship into a TransformingFlagship
            Info.Update( TargetIfShip, TargetIfPlanet, HackerFaction, HackType );
            listOfItemsLastUsedToPopulateCustomButton.Clear();
            NecromancerEmpireFactionBaseInfo gData = HackerFaction.TryGetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();

            List<NecromancerUpgrade> workingBlueprints = gData.AvailableBlueprints.GetDisplayList();

            for ( int i = 0; i < workingBlueprints.Count; i++ )
            {
                listOfItemsLastUsedToPopulateCustomButton.Add( workingBlueprints[i] );
                //this part is the same for any hack
                HackingUtils.PopulateOneCustomHackingOptionButton( Set, ref runningY, ref firstBounds, rowBuffer, WindowAdder,
                                                                   //this part differs for each hack type, telling it how to tell each option apart
                                                                   string.Empty, i, i,
                                                                   //and this part is also different for each hack type, pointing to its own unique button implementation.
                                                                   //they can all be called bCustomHackingOptionButton, since they are a subclass of the hacking implementation class.
                                                                   type_bCustomHackingOptionButton );
            }
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                Faction facOrNull = Hacker.GetFactionOrNull_Safe();
                NecromancerEmpireFactionBaseInfo gData = facOrNull.TryGetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
                List<NecromancerUpgrade> workingBlueprints = gData.AvailableBlueprints.GetDisplayList();
                NecromancerUpgrade upgrade = null;
                for ( int i = 0; i < workingBlueprints.Count; i++ )
                {
                    if ( Event.RelatedStringOrNull != null && Event.RelatedStringOrNull.Length > 0 )
                    {
                        if ( workingBlueprints[i].InternalName == Event.RelatedStringOrNull )
                        {
                            upgrade = workingBlueprints[i];
                            break;
                        }
                    }
                }
                if ( upgrade == null )
                {
                    throw new Exception("Could not find upgrade to grant! We were looking for " + Event.RelatedStringOrNull + ".");
                }
                //ArcenDebugging.ArcenDebugLogSingleLine("Transforming " + Target.TypeData.GetDisplayName() + " of " + Target.FleetMembership.Fleet.GetName() + " into " + upgrade.RelatedShip.GetDisplayName() + " hacker " + Hacker.FleetMembership.Fleet.GetName(), Verbosity.DoNotShow );
                Target.TransformInto( Context, upgrade.RelatedShip, 1, true );
                return true;
            }
            catch ( Exception e )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Exception hit during Hacking_TransformNecromancerFlagship DoSuccessfulCompletionLogic_Extra debug code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
        }
        
        #region bCustomHackingOptionButton
        public class bCustomHackingOptionButton : ButtonAbstractBase
        {
           #region GetTechUpgradeFromElement
           public static NecromancerUpgrade GetNecromancerUpgradeFromElement( ArcenUI_Element element )
           {
               if ( element == null || listOfItemsLastUsedToPopulateCustomButton == null )
                   return null;
               if ( element.CreatedByCodeDirective == null )
                   return null;
               int index = element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
               if ( index < 0 || index >= listOfItemsLastUsedToPopulateCustomButton.Count )
                   return null;
               return listOfItemsLastUsedToPopulateCustomButton[index];
           }
           #endregion

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                NecromancerUpgrade upgrade = GetNecromancerUpgradeFromElement( this.Element );
                if ( upgrade == null )
                {
                    Buffer.Add( "<color=#c74639>Null NecromancerUpgrade</color>" );
                    return;
                }

                if ( !this.GetCanHackForThisItem( upgrade ) )
                {
Buffer.Add( "<color=#c74639>不可行：</color> " );
                }
                
                GameEntityTypeData typeData = upgrade.RelatedShip;
                if ( typeData == null || typeData.TexEmbedSprite_Icon == null )
                    typeData = upgrade.ShipForCapIncrease;

                Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                Buffer.AddShipIconInline(typeData, localFaction );
                Buffer.Add( upgrade.RelatedShip.GetDisplayName() );

                int essCost = upgrade.BlueprintTransformCostInResourceOne.IntValue;
                int hapCost = upgrade.BlueprintTransformCostInHacking.IntValue;
                if (essCost > 0 || hapCost > 0)
                {
                    Buffer.Add(" ");
                    if (essCost > 0)
                        Buffer.AddResourceOne(upgrade.BlueprintTransformCostInResourceOne.IntValue, true);
                    if (hapCost > 0)
                        Buffer.AddHacking(upgrade.BlueprintTransformCostInHacking.IntValue, true);    
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                NecromancerUpgrade upgrade = GetNecromancerUpgradeFromElement( this.Element );
                if ( upgrade == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                if ( !this.GetCanHackForThisItem( upgrade ) )
                    return MouseHandlingResult.PlayClickDeniedSound;
                // #region instead of normal click behavior, show details
                // if ( InputCaching.CalculateHoldAndClickToViewDetailsOfContents() )
                // {
                //     int upgradesSoFar = localFaction.TechUnlocks[upgrade.RowIndexNonSim] + localFaction.FreeTechUnlocks[upgrade.RowIndexNonSim];
                //     Window_InGameSidebarScience.btnScienceTech.ShowDetailsOfATechUpgradeContents( upgrade, 0, -1, upgradesSoFar );
                //     return MouseHandlingResult.None;    // }
                // #endregion

                // if ( !this.GetCanHackForThisItem( upgrade ) )
                //     return MouseHandlingResult.PlayClickDeniedSound;

                 bool doAreYouSurePrompt = GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) && !InputCaching.CalculateHoldAndSuppressTechUpgradePrompt();
                 if ( doAreYouSurePrompt )
                 {
                    GameEntity_Squad hacker = HackingUtils.GetPreferredHacker( Info.TargetShip, Info.HackingType, Info.TargetPlanet, true );
                    if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                        return MouseHandlingResult.PlayClickDeniedSound;
                    ModalPopupData.CreateAndLogYesNoStyle( DoHack, null, "确定吗", "你确定要对 " + upgrade.DisplayName + " 执行黑客 " + Info.HackingType.DisplayName +
                         " 吗？\n \n" + "<color=#888888>要禁用此提示，请进入游戏设置，在游戏选项卡下将其关闭。或者按住 " +
                         InputActionTypeDataTable.GetActionByName_FairlySlow( "SuppressTechUpgradePrompt" ).GetHumanReadableKeyCombo() + " 同时点击升级按钮以跳过一次。</color>", "是，黑客", "不，不要" );
                 }
                 else
                     DoHack();

                return MouseHandlingResult.None;
            }

            public void DoHack()
            {
                NecromancerUpgrade upgrade = GetNecromancerUpgradeFromElement( this.Element );
                if ( upgrade == null )
                    return;
                if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                    return;

                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransformNecrofleet], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedEntityIDs.Add( Info.TargetShip.PrimaryKeyID );
                command.RelatedString2 = upgrade.InternalName;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                Window_HackChoicesSidebarPopout.Instance.Close();
            }

            private string lastRejectionReason = string.Empty;
            public bool GetCanHackForThisItem( NecromancerUpgrade item )
            {
                int debugCode = 0;
                try{
                    debugCode = 100;
                    if ( Info == null )
                    {
                        lastRejectionReason = "Info was null";
                        return false;
                    }
                    debugCode = 110;
                    GameEntity_Squad hacker = Info.TargetShip; //this is handled above, in terms of true cases
                    if ( hacker == null )
                    {
                        lastRejectionReason = "No hacker found";
                        return false;
                    }
                    debugCode = 115;
                    PlanetFaction pFaction = hacker.PlanetFaction;
                    if ( pFaction == null )
                    {
                        lastRejectionReason = "No pfaction found";
                        return false;
                    }
                    debugCode = 120;
                    Faction faction = pFaction.Faction;
                    if ( faction == null )
                    {
                        lastRejectionReason = "No faction found";
                        return false;
                    }
                    debugCode = 130;
                    FInt costForActiveHacks = HackingUtils.CalculateActiveHackingCosts(faction);
                    if ( hacker == null )
                    {
                        lastRejectionReason = "No hacker available at the moment";
                        return false;
                    }
                    debugCode = 200;
                    if ( item == null )
                    {
                        throw new Exception("How is item null?");
                    }
                    debugCode = 250;
                    if ( hacker.TypeData == item.RelatedShip )
                    {
                        lastRejectionReason = "你的旗舰已经是此类型";
                        return false;
                    }
                    debugCode = 300;
                    if (  Info.TargetShip.CurrentMarkLevel < item.MinFlagshipLevel )
                    {
                        lastRejectionReason = "Flagship is at too low mark level. This item requires a flagship with mark level " + item.MinFlagshipLevel;
                        return false;
                    }
                    debugCode = 400;
                    if ( faction.StoredHacking < item.BlueprintTransformCostInHacking )
                    {
                        lastRejectionReason = "你没有足够的入侵点";
                        return false;
                    }
                    if ( faction.StoredFactionResourceOne < item.BlueprintTransformCostInResourceOne )
                    {
                        if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( faction ) )
                            lastRejectionReason = "你没有足够的 Essence。你需要入侵裂隙或与远古生物战斗来获取更多 Essence";
                        else
                            lastRejectionReason = "你没有足够的资源一";
                        return false;
                    }
                    if ( faction.StoredHacking < item.BlueprintTransformCostInHacking + costForActiveHacks )
                    {
                        lastRejectionReason = "由于你正在进行的入侵，你没有足够的入侵点";
                        return false;
                    }
                    debugCode = 500;
                    return Implementation.GetCanBeHacked( Info.TargetShip, hacker,
                                                          Info.TargetPlanet, hacker.GetFactionOrNull_Safe(), Info.HackingType,
                                                          this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString, -1, out lastRejectionReason ) == Hackable.CanBeHacked;
                } catch(Exception e)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit excpetion in transform flagship GetCanHackForThisItem debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
                }

                return false;
            }

            private static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Hacking_TransformNecromancerFlagship-bCustomHackingOptionButton-tooltipBuffer" );
            public override void HandleMouseover()
            {
                int debugCode = 0;
                NecromancerUpgrade upgrade = GetNecromancerUpgradeFromElement( this.Element );
                try{
                    debugCode = 100;
                    if ( upgrade == null )
                    {
                        Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "Null NecromancerUpgrade, this is a bug." );
                        return;
                    }
                    else
                    {
                        debugCode = 200;
                        Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                        debugCode = 300;
                        tooltipBuffer.Add( "<b><u>黑客： " ).Add( Info.HackingType.DisplayName ).Add( "</u></b>\n" );
                        tooltipBuffer.Add("此升级允许你将任何合适标记等级的旗舰转变为 ").Add(upgrade.RelatedShip.GetDisplayName(), "a1ffa1").Add("，消耗精华。\n");

                        if ( upgrade.MinFlagshipLevel  > 1 )
                        {
                            tooltipBuffer.Add("\n\n").Add("此升级仅适用于标记等级为 ").Add( upgrade.MinFlagshipLevel, "ffa1a1" ).Add(" 或以上的旗舰。\n");
                        }
                        debugCode = 400;
                        if ( !this.GetCanHackForThisItem( upgrade ) )
                        {
                            tooltipBuffer.Add( "<color=#c74639>Not Possible:</color> " );
                        }
                        debugCode = 500;
                        GameEntityTypeData typeData = upgrade.RelatedShip;
                        if ( typeData == null || typeData.TexEmbedSprite_Icon == null )
                        {
                            typeData = upgrade.RelatedShip;
                        }
                        debugCode = 600;
                        if ( typeData != null && typeData.TexEmbedSprite_Icon != null )
                        {
                            debugCode = 700;
                            //Write unit tooltip if appropriate
                            byte markLevel = localFaction.GetGlobalMarkLevelForShipLine( typeData );
                            tooltipBuffer.Add("\n");
                            EntityText.GetTooltip( tooltipBuffer, null, null, typeData, 1,
                                                                           localFaction, Info.TargetShip.CurrentMarkLevel, FromSidebarType.Sidebar_MultipleUnits,
                                                                           ShipExtraDetailFlags.BuildInfo | ShipExtraDetailFlags.AnyGrantHackInfo | ShipExtraDetailFlags.WindowHackChoicesSidebarPopoutForData, 1f, false );

                            EntityText.Write_Tooltip_Hotkeys_Footer( tooltipBuffer, false, true, null );
                        }
                        debugCode = 800;
                        if ( !this.GetCanHackForThisItem( upgrade ) )
                            tooltipBuffer.Add( "\n\n<color=#c74639>无法选择该选项： " + this.lastRejectionReason + "。</color>" );
                        else
                        {
                            if ( GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) )
                            {
                                tooltipBuffer.Add( "\n\n<color=#3f6c9e>按住 </color><color=#4486d1>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "SuppressTechUpgradePrompt" ) )
                                    .Add( "</color> <color=#3f6c9e>以跳过确认提示。</color>  " );
                            }
                        }

                        Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
                    }
                } catch( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in mouseover for a transformation: " + upgrade.ToString() + ". debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
                }
            }
        }
        #endregion
    }

    public class Hacking_CorruptCastle : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            // if ( Target.CurrentMarkLevel >= 7 )
            // {
            //     RejectionReasonDescription = "此 " + Target.TypeData.GetDisplayName() +" 已处于最高等级";
            //     return Hackable.NeverBeHacked_ButStillShow;
            // }

            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            GameEntityTypeData droneData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "NecromancerDroneProducer");
            if ( droneData == null )
                throw new Exception("no drone producer for necromancer found");
            GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( Hacker.PlanetFaction, droneData, (byte)1,
                                                                                       Hacker.PlanetFaction.Faction.LooseFleet, 0, Target.WorldLocation, Context, "Hacking-NecroDronePst" );  //is fine, main sim thread
            Target.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
            return true;
        }
    }

    public class Hacking_TransformElderling : HackingImplementation_WithTargetMenu
    {
        public override Hackable GetCanHackForThisTarget(out string RejectionReasonDescription, GameEntity_Squad target, Planet planet, HackingType Type)
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction != null && !NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction(localFaction) )
            {
                //Only the necromancer can see these hacks
                RejectionReasonDescription = "Only the necromancer can hack";
                return Hackable.NeverCanBeHacked_Hide;
            }
            DLC3GameEntityTypeDataExtension Target_DLC3TypeData = target.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
            if ( Target_DLC3TypeData == null )
            {
                RejectionReasonDescription = "invalid unit type";
                return Hackable.NeverCanBeHacked_Hide;
            }
            GameEntityTypeData transformedUnit = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( Target_DLC3TypeData.NameForElderlingTransformation );
            if ( transformedUnit == null ) {
                ArcenDebugging.ArcenDebugLogSingleLine( "Unit " + target.TypeData.InternalName + " is eligible for the TransformElderling hack, but does not have a type to be transformed into.", Verbosity.ShowAsError );
                RejectionReasonDescription = "No type to be transformed into.";
                return Hackable.NeverCanBeHacked_Hide;
            }
            NecromancerEmpireFactionBaseInfo gData = localFaction?.TryGetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            DLC3GameEntityTypeDataExtension transformedUnit_DLC3TypeData = transformedUnit.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
            if ( transformedUnit_DLC3TypeData != null && gData.NecromancerCompletedUpgrades.Contains( transformedUnit_DLC3TypeData.NecromancerUpgradeToGrantOnDeath ) )
            {
                RejectionReasonDescription = "你已经解锁了此蓝图";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            
            //TODO: if we have already gotten this blueprint, don't show anymore
            RejectionReasonDescription = "";
            return Hackable.CanBeHacked;
        }
        public override void GetTooltipForTarget(ArcenCharacterBufferBase buffer, GameEntity_Squad target, Planet planet, HackingType Type)
        {
            buffer.Add("\n此黑客将转变 <color=#a1ffa1>" + target.TypeData.GetDisplayName() + "</color>。");
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            DLC3GameEntityTypeDataExtension Target_DLC3TypeData = target.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
            GameEntityTypeData transformedUnit = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( Target_DLC3TypeData?.NameForElderlingTransformation );
            DLC3GameEntityTypeDataExtension transformedUnit_DLC3TypeData = transformedUnit.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
            NecromancerUpgrade upgrade = transformedUnit_DLC3TypeData?.NecromancerUpgradeToGrantOnDeath;
            if (upgrade != null && upgrade.Type == NecromancerUpgradeType.ClaimBlueprints) {
                buffer.Add("\n");
                buffer.Add("当转变后的长老被击杀时，");
                if (transformedUnit.AIPOnDeath > 0) {
                    buffer.Add( "AI 进度 (AIP) 将<color=#ffdf72>增加 " ).Add( transformedUnit.AIPOnDeath ).Add( "</color>，且" );
                }
                buffer.Add("你将能够转变任何标记等级为 ").Add(upgrade.MinFlagshipLevel);
                buffer.Add(" 的旗舰为 ").Add(upgrade.RelatedShip.GetDisplayName(), "a1ffa1").Add("，消耗黑客点和精华。");
                buffer.Add("\n\n");
                EntityText.GetTooltip( buffer, null, null, upgrade.RelatedShip, 1,
                        localFaction, (byte)upgrade.MinFlagshipLevel, FromSidebarType.Sidebar_MultipleUnits,
                        ShipExtraDetailFlags.BuildInfo | ShipExtraDetailFlags.AnyGrantHackInfo | ShipExtraDetailFlags.WindowHackChoicesSidebarPopoutForData, 1f, false );

            }
        }

        public override void DoOneSecondOfHackingLogic_HackSpecificLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            // Handle hacking reaction manually, forcing the faction to be an ai faction.
            int secondsSoFar = Hacker.ActiveHack_DurationThusFar;
            if ( secondsSoFar == 0 )
                return;

            Faction targetFaction = Target.PlanetFaction.Faction;
            if ( targetFaction == null ) // Shouldn't be able to occur, but you never know.
                return;
            if ( Hacker.ActiveHack_DurationThusFar % type.GetPrimaryHackResponseInterval() == 0 )
                targetFaction.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, FInt.One, Context, Event, targetFaction );
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                DLC3GameEntityTypeDataExtension Target_DLC3TypeData = Target.TypeData.TryGetDataExtensionAs<DLC3GameEntityTypeDataExtension>( "DLC3" );
                if ( Target_DLC3TypeData == null && ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                    return false;
                GameEntityTypeData transformedUnit = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( Target_DLC3TypeData.NameForElderlingTransformation );
                if ( transformedUnit == null )
                    throw new Exception("No elderling transformation unit specified in XML");
                bool keepDamageAndDebuffs = true;
                GameEntity_Squad newUnit = Target.TransformInto( Context, transformedUnit, 1, keepDamageAndDebuffs );
                ElderlingsPerUnitBaseInfo oldData = Target.GetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();
                ElderlingsPerUnitBaseInfo newData = newUnit.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
                newData.UpdateForTransformedElderling(oldData);
                //TODO: make this slower
                return true;
            }
            catch ( Exception e )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Exception hit during Hacking_TransformNecromancerFlagship DoSuccessfulCompletionLogic_Extra debug code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
        }
    }
    public class Hacking_LureElderling : BaseHackingImplementation
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            if ( hackerOrNull != null )
            {
                if ( hackerOrNull.FleetMembership.Fleet != null )
                    return "\nThis hack will be done by " + hackerOrNull.TypeData.GetDisplayName() + " fleet " + hackerOrNull.FleetMembership.Fleet.GetName()+ ".";
                else
                    return "\nThis hack will be done by " + hackerOrNull.TypeData.GetDisplayName() + ".";
            }
            return "";
        }
        bool appliesToOnlyTrackedElderlings = true;

        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( HackerOrNull == null )
            {
                RejectionReasonDescription = "There are no units that can hack on this planet";
                return Hackable.NeverCanBeHacked_Hide;
            }
            //We may choose to give this to human empire players?
            // if ( !NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( HackerOrNull.PlanetFaction.Faction ) )
            // {
            //     //Only the necromancer can see these hacks
            //     RejectionReasonDescription = "Only the necromancer can hack";
            // }
            int allElderlings = FactionUtilityMethods.Instance.HostileElderlingsWithThisPlanetInTerritory( HackerFaction, planet, !appliesToOnlyTrackedElderlings );
            if ( allElderlings == 0 )
            {
                //It would just clutter otherwise
                RejectionReasonDescription = "No Elderlings at all";
                return Hackable.NeverCanBeHacked_Hide;
            }
            int lurableElderlings = FactionUtilityMethods.Instance.HostileElderlingsWithThisPlanetInTerritory( HackerFaction, planet, appliesToOnlyTrackedElderlings );
            if ( lurableElderlings == 0 )
            {
                RejectionReasonDescription = "你只能引诱你追踪过的远古生物。";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            RejectionReasonDescription = "";
            return Hackable.CanBeHacked;

            //return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override void DoOneSecondOfHackingLogic_HackSpecificLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
        }
        public override void DoOnCancel_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type, HackingEvent Event, 
                                                      ArcenHostOnlySimContext Context )
        {
            //de-lure any elderlings
            FactionUtilityMethods.Instance.LureHostileElderlings( Hacker.PlanetFaction.Faction, planet, false, appliesToOnlyTrackedElderlings );
        }
        public override bool CheckIfHackIsDone_OnlyIfPerSecondStyleCost( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type )
        {
            Faction facOrNull = Hacker.GetFactionOrNull_Safe();
            if ( facOrNull != null & facOrNull.StoredHacking <= 0 ) //Stops the hack if you hit 0 hacking
            {
                return true;
            }
            int lurableElderlings = FactionUtilityMethods.Instance.HostileElderlingsWithThisPlanetInTerritory( Hacker.PlanetFaction.Faction, planet, appliesToOnlyTrackedElderlings );
            if ( lurableElderlings == 0 )
            {
                //no more elderlings for this planet
                return true;
            }
            //you can just do the Lure hack until the Elderling forces break you or hit zero hacking
            return false;
        }

        public override void DoOnFail_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, HackingType type, HackingEvent Event, 
                                                      ArcenHostOnlySimContext Context )
        {
            //de-lure any elderlings
            FactionUtilityMethods.Instance.LureHostileElderlings( Hacker.PlanetFaction.Faction, planet, false, appliesToOnlyTrackedElderlings );
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                bool debug = GameSettings.Current.GetBoolBySetting( "HackingDebug" );
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Luring Elderlings to " + planet.Name + ". duration? " + Hacker.ActiveHack_DurationThusFar + " and interval: " + type.GetPrimaryHackResponseInterval(), Verbosity.DoNotShow );
                FactionUtilityMethods.Instance.LureHostileElderlings( Hacker.PlanetFaction.Faction, planet, true, appliesToOnlyTrackedElderlings );
                Faction targetFaction = FactionUtilityMethods.Instance.GetTemplarFaction();
                if ( targetFaction == null ) // Shouldn't be able to occur, but you never know.
                {
                    return false;
                }
                if ( Hacker.ActiveHack_DurationThusFar % type.GetPrimaryHackResponseInterval() == 0 )
                {
                    targetFaction.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, FInt.One, Context, Event, targetFaction );
                }
                return true;
            }
            catch ( Exception e )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Exception hit during Hacking_LureElderling DoSuccessfulCompletionLogic_Extra debug code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
        }
    }
    public class Hacking_TrackElderling : HackingImplementation_WithTargetMenu
    {
        public override Hackable GetCanHackForThisTarget(out string RejectionReasonDescription, GameEntity_Squad Target, Planet planet, HackingType Type)
        {
            ElderlingsPerUnitBaseInfo data = Target.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
            if ( data.TrackedByPlayer ) {
                RejectionReasonDescription = "此单位已被入侵";
                return Hackable.NeverCanBeHacked_Hide;
            }
            //TODO: if we have already gotten this blueprint, don't show anymore
            RejectionReasonDescription = "";
            return Hackable.CanBeHacked;
        }
        public override void GetTooltipForTarget(ArcenCharacterBufferBase buffer, GameEntity_Squad target, Planet planet, HackingType Type)
        {
            buffer.Add("\n此黑客将追踪 <color=#a1ffa1>" + target.TypeData.GetDisplayName() + "</color>。");
        }
        public override void DoOneSecondOfHackingLogic_HackSpecificLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            // Handle hacking reaction manually, forcing the faction to be an ai faction.
            int secondsSoFar = Hacker.ActiveHack_DurationThusFar;
            if ( secondsSoFar == 0 )
                return;

            Faction targetFaction = Target.PlanetFaction.Faction;
            if ( targetFaction == null ) // Shouldn't be able to occur, but you never know.
                return;
            if ( Hacker.ActiveHack_DurationThusFar % type.GetPrimaryHackResponseInterval() == 0 )
                targetFaction.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, FInt.One, Context, Event, targetFaction );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                ElderlingsPerUnitBaseInfo data = Target.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
                data.TrackedByPlayer = true;
                return true;
            }
            catch ( Exception e )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Exception hit during Hacking_TrackElderling DoSuccessfulCompletionLogic_Extra debug code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
        }
    }

    public class Hacking_RiftHack : HackingImplementation_WithMenu<NecromancerUpgrade, NecromancerUpgrade>
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
        
        public override NecromancerUpgrade GetItemFromWrapper(NecromancerUpgrade wrapper, out bool found) {
            found = true;
            return wrapper;
        }
        public override void PopulateItemsToShow(List<NecromancerUpgrade> listToPopulate, GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, HackingType HackType)
        {
            TemplarPerUnitBaseInfo data = TargetIfShip?.TryGetExternalBaseInfoAs<TemplarPerUnitBaseInfo>();
            if ( data == null ) {
                ArcenDebugging.ArcenDebugLogSingleLine("Null TemplarPerUnitBaseInfo on " + TargetIfShip.ToStringWithPlanetAndOwner(), Verbosity.DoNotShow );
                return;
            }
            listToPopulate.CopyFrom(data.AvailableUpgrades);
        }
        public override Hackable GetCanHackForThisItem(NecromancerUpgrade item, out string rejectionReason, GameEntity_Squad Target, Planet planet, HackingType Type)
        {
            int debugCode = 0;
            try {
                debugCode = 100;
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction != null && !NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction(localFaction) ) {
                    //Only the necromancer can see these hacks
                    rejectionReason = "Only the necromancer can hack";
                    return Hackable.NeverCanBeHacked_Hide;
                }
                
                NecromancerEmpireFactionBaseInfo baseInfo = localFaction.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
                if ( baseInfo == null ) {
                    throw new Exception("Could not find necromancer base info for local faction ");
                }
                
                debugCode = 500;
                if ( baseInfo.GetHighestNecropolisMarkLevel() < item.MustHaveAnyNecropolisAtThisLevel )
                {
                    rejectionReason = "你必须至少有一座等级为 " + item.MustHaveAnyNecropolisAtThisLevel + " 的死灵城才能认领此升级，你最高等级的死灵城是 " + baseInfo.GetHighestNecropolisMarkLevel();
                    return Hackable.NeverBeHacked_ButStillShow;
                }
                
                GameEntity_Squad hacker = HackingUtils.CalculateHackerForHack( Target, planet, Type, false ); //this is handled above, in terms of true cases
                if ( hacker != null && hacker.CurrentMarkLevel < item.MinFlagshipLevel )
                {
                    rejectionReason = "Hacker is at too low mark level; this can only be hacked by a flagship at mark level " + item.MinFlagshipLevel;
                    return Hackable.NeverBeHacked_ButStillShow;
                }
                
                if (item.AdditionalHapCost > 0 || item.AdditionalEssenceCost > 0)
                {
                    var hapCost = Type.GetHackPointCostForTarget(Target) + item.AdditionalHapCost * Type.GetHapCostScale(Target, true);
                    var essCost = Type.GetResourceOneCostForTarget(Target) + item.AdditionalEssenceCost;

                    bool hapShort = hapCost > localFaction.StoredHacking;
                    bool essShort = essCost > localFaction.StoredFactionResourceOne;
                    
                    if (hapShort || essShort)
                    {
                        var buffer = this.Buffer;
buffer.Add("不足 ");
                             
                         if (hapShort)
                             buffer.StartHacking(false).Add("黑客点").EndColor();
                                 
                         if (essShort)
                         {
                             if (hapShort)
                                 buffer.Add(" 或 ");
                             buffer.StartResourceOne(false).Add("精华").EndColor();
                         }
                        
                        rejectionReason =  buffer.ToString();
                        
                        return Hackable.NeverBeHacked_ButStillShow;
                    }
                }
                
            } catch(Exception e) {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit excpetion in rift hack GetCanHackForThisItem debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
                rejectionReason = "Exception in rift hack GetCanHackForThisItem";
                return Hackable.NeverCanBeHacked_Hide;
            }

            rejectionReason = "";
            return Hackable.CanBeHacked;
        }
        public override void GetDisplayNameForItem(ArcenCharacterBufferBase Buffer, NecromancerUpgrade upgrade)
        {
            GameEntityTypeData typeData = upgrade.RelatedShip;
            if ( typeData == null || typeData.TexEmbedSprite_Icon == null )
                typeData = upgrade.ShipForCapIncrease;
            
            Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();

            if (typeData != null)
                Buffer.AddShipIconInline( typeData, localFaction );
            Buffer.Add( upgrade.GetShortDisplayName() );
                
            if ( upgrade.Type == NecromancerUpgradeType.IncreaseSkeletonCap ||
                 upgrade.Type == NecromancerUpgradeType.IncreaseWightCap )
            {
                Buffer.Add( " 为舰队" );
            }
        }
        public override void GetExtraLabelInfoForItem(ArcenCharacterBufferBase Buffer, NecromancerUpgrade upgrade, GameEntity_Squad Target, Planet planet, HackingType HackType)
        {
            if ( upgrade.AdditionalAIPCost > 0 || upgrade.AdditionalEssenceCost > 0 || upgrade.AdditionalHapCost > 0)
                Buffer.Add("   ");
            
            if ( upgrade.AdditionalAIPCost > 0 )
            {
                Buffer.AddAIP(upgrade.AdditionalAIPCost + HackType.AIPOnCompletion, true);
            }
            if ( upgrade.AdditionalEssenceCost > 0 )
            {
                Buffer.AddResourceOne(upgrade.AdditionalEssenceCost + HackType.GetResourceOneCostForTarget(Target), true);
            }
            if ( upgrade.AdditionalHapCost > 0 )
            {
                Buffer.AddHacking((upgrade.AdditionalHapCost * HackType.GetHapCostScale(Target, true)).IntValue, true);
            }
        }
        
        public override void GetTooltipForItem(ArcenCharacterBufferBase buffer, NecromancerUpgrade upgrade, GameEntity_Squad Target, Planet planet, HackingType Type)
        {
            int debugCode = 0;
            try {
                debugCode = 200;
                Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                NecromancerEmpireFactionBaseInfo gData = localFaction.TryGetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
                int upgradeCount = 0;
                if ( gData != null )
                {
                    for ( int i = 0; i < gData.NecromancerCompletedUpgrades.Count; i++ )
                    {
                        if ( gData.NecromancerCompletedUpgrades[i] == upgrade )
                            upgradeCount++;
                    }
                }
                if ( upgrade.Type == NecromancerUpgradeType.GrantEssence )
                {
                    buffer.Add("此升级将从裂隙中收集 ").Add( localFaction != null && localFaction.Resource1TextColorAndIcon.Length > 0 ? localFaction.Resource1TextColorAndIcon : World_AIW2.Instance.Resource1TextColorAndIcon ).Add(" ").Add( upgrade.RelatedResource ).Add(" 精华；它将允许你建造或升级亡者之城，或升级旗舰。\n");
                }
                else if ( upgrade.Type == NecromancerUpgradeType.UnlockSkeletonType ||
                        upgrade.Type == NecromancerUpgradeType.UnlockWightType ||
                        upgrade.Type == NecromancerUpgradeType.UnlockMummyType )
                {
                    debugCode = 310;
                    buffer.Add("此升级将允许在任何亡者之城额外建造 ").Add( upgrade.CapIncrease, "ffa1a1" ).Add(" 艘 ").Add(upgrade.ShipForCapIncrease.GetDisplayName() ).Add( "。这将使该亡者之城的旗舰能够在击杀合适敌人时获取 ").Add(upgrade.RelatedShip.GetDisplayName(), "a1ffa1");
                    if ( upgrade.GalaxyCapIncrease != 0 )
                        buffer.Add( "，并增加全银河上限 ").Add( upgrade.GalaxyCapIncrease, "ffa1a1" );
                    buffer.Add("。\n");
                    if ( upgradeCount > 0 )
                    {
                        string attempts = "time";
                        if ( upgradeCount > 1 )
                            attempts = "times";
                        buffer.Add("\n<size=90%>\n你已经解锁此升级 ").Add( upgradeCount, "a1a1ff" ).Add(" 次。多次解锁意味着你可以在每个亡者之城建造更多 ").Add( upgrade.ShipForCapIncrease.GetDisplayName() ).Add("，这很有用。\n</size>");
                    }
                    // TODO: Mention bodyguards
                }
                else if ( upgrade.Type == NecromancerUpgradeType.UnlockNewShip )
                {
                    debugCode = 320;
                    buffer.Add("此升级将允许在任何亡者之城额外建造 ").Add( upgrade.CapIncrease, "ffa1a1" ).Add(" 艘 ").Add(upgrade.ShipForCapIncrease.GetDisplayName() ).Add("。该亡者之城的旗舰随后将能在任何死灵法师船坞建造 ").Add(upgrade.RelatedShip.GetDisplayName(), "a1ffa1");
                    if ( upgrade.GalaxyCapIncrease != 0 )
                        buffer.Add( " and increases the galaxy-wide cap by " ).Add( upgrade.GalaxyCapIncrease, "ffa1a1" );
                    buffer.Add(".\n");
                    if ( upgradeCount > 0 )
                    {
                        string attempts = "time";
                        if ( upgradeCount > 1 )
                            attempts = "times";

                        buffer.Add("<size=90%>\n你已经解锁此升级 ").Add( upgradeCount, "a1a1ff" ).Add(" 次。多次解锁意味着你可以在每个亡者之城建造更多 ").Add( upgrade.ShipForCapIncrease.GetDisplayName() ).Add("，这很有用。\n</size>");
                    }

                }
                else if ( upgrade.Type == NecromancerUpgradeType.IncreaseSkeletonCap || upgrade.Type == NecromancerUpgradeType.IncreaseWightCap )
                {
                    debugCode = 330;
                    buffer.Add("此升级将增加每类 ").Add(upgrade.ShipForCapIncrease.GetDisplayName(), "a1ffa1").Add(" 的舰船上限 " ).Add( upgrade.CapIncrease, "a1ffa1" ).Add("，仅限此旗舰。\n");
                }
                else if ( upgrade.Type == NecromancerUpgradeType.IncreaseSkeletonSoftCap )
                {
                    debugCode = 330;
                    buffer.Add("此升级将增加此旗舰的骷髅软上限 ").Add( upgrade.CapIncrease, "a1ffa1" ).Add("。\n");
                }
                else if ( upgrade.Type == NecromancerUpgradeType.IncreaseWightSoftCap )
                {
                    debugCode = 340;
                    buffer.Add("此升级将增加此旗舰的怨灵软上限 ").Add( upgrade.CapIncrease, "a1ffa1" ).Add("。\n");
                }
                else if ( upgrade.Type == NecromancerUpgradeType.ClaimBlueprints )
                {
                    debugCode = 340;
                    buffer.Add("此升级允许你转变任何合适标记等级的旗舰为 ").AddShipIconNameShort(upgrade.RelatedShip).Add("，消耗从裂隙中收集的精华。\n");
                }
                else
                {
                    buffer.Add("未知升级\n");
                }
                
                int count = 0;
                if ( upgrade.MinFlagshipLevel  > 1 )
                {
                    if (count == 0) buffer.Pad();
                    count++;
                    Buffer.Add("此升级仅适用于标记等级为 ").Add( upgrade.MinFlagshipLevel, "ffa1a1" ).Add(" 或以上的旗舰。\n");
                }
                if ( upgrade.MustHaveAnyNecropolisAtThisLevel > 1 )
                {
                    if (count == 0) buffer.Pad();
                    count++;
                    buffer.Add("此升级仅适用于标记等级为 ").Add( upgrade.MustHaveAnyNecropolisAtThisLevel, "ffa1a1" ).Add(" 或以上的亡者之城。\n");
                }

                if ( upgrade.AdditionalAIPCost > 0 || upgrade.AdditionalEssenceCost > 0 || upgrade.AdditionalHapCost > 0)
                {
                    buffer.Pad().NewLineIfNeeded().Add("此黑客花费 ");
                    
                    int counter = 0;
                    if ( upgrade.AdditionalAIPCost > 0 )
                    {
                        if (counter > 0) buffer.Add(Text.ThinSpace);
                        counter++;
                        buffer.AddAIP(upgrade.AdditionalAIPCost, true);
                    }
                    if ( upgrade.AdditionalEssenceCost > 0 )
                    {
                        if (counter > 0) buffer.Add(Text.ThinSpace);
                        counter++;
                        buffer.AddResourceOne(Type.GetResourceOneCostForTarget(Target) + upgrade.AdditionalEssenceCost, true);
                    }
                    if ( upgrade.AdditionalHapCost > 0 )
                    {
                        if (counter > 0) buffer.Add(Text.ThinSpace);
                        counter++;
                        buffer.AddHacking((Type.GetHackPointCostForTarget(Target) + upgrade.AdditionalHapCost * Type.GetHapCostScale(Target, true)).IntValue, true);
                    }
            
                    buffer.Add( "\n" );
                }


                debugCode = 500;
                GameEntityTypeData typeData = upgrade.ShipForCapIncrease ?? upgrade.RelatedShip;
                debugCode = 600;
                if ( typeData != null && typeData.TexEmbedSprite_Icon != null )
                {
                    debugCode = 700;
                    //Write unit tooltip if appropriate
                    byte markLevel = localFaction.GetGlobalMarkLevelForShipLine( typeData );
                    buffer.Add("\n");
                    EntityText.GetTooltip( buffer, null, null, typeData, 1,
                            localFaction, markLevel, FromSidebarType.Sidebar_MultipleUnits,
                            ShipExtraDetailFlags.BuildInfo | ShipExtraDetailFlags.AnyGrantHackInfo | ShipExtraDetailFlags.WindowHackChoicesSidebarPopoutForData, 1f, false );

                    EntityText.Write_Tooltip_Hotkeys_Footer( buffer, false, true, null );
                }
            } catch( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in mouseover for a hacking upgrade " + upgrade.ToString() + ". debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        public override void DoHack(NecromancerUpgrade upgrade, GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, HackingType HackType)
        {
            string lastRejectionReason = "";
            HackingUtils.TryDoHack( ref lastRejectionReason, TargetIfShip, TargetIfPlanet, HackType,
                    upgrade.InternalName, -1, null );
        }

        public override void DoOneSecondOfHackingLogic_HackSpecificLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            // Handle hacking reaction manually, forcing the faction to be an ai faction.
            int secondsSoFar = Hacker.ActiveHack_DurationThusFar;
            if ( secondsSoFar == 0 )
                return;

            Faction targetFaction = Target.PlanetFaction.Faction;
            if ( targetFaction == null ) // Shouldn't be able to occur, but you never know.
                return;
            if ( Hacker.ActiveHack_DurationThusFar % type.GetPrimaryHackResponseInterval() == 0 )
                targetFaction.Safe_DeepInfo_ReactToHacking_AsPartOfMainSim_HostOnly( Target, FInt.One, Context, Event, targetFaction );
        }
        
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                var facOrNull = Hacker.GetFactionOrNull_Safe();
                var data = Target.TryGetExternalBaseInfoAs<TemplarPerUnitBaseInfo>();
                if ( data == null )
                    throw new Exception("Tried to hack " + Target.ToStringWithPlanetAndOwner() + " but it had no templar data");
                
                NecromancerUpgrade upgrade = null;
                
                debugCode = 200;
                if (!string.IsNullOrEmpty(Event.RelatedStringOrNull))
                    upgrade = data.AvailableUpgrades.Find((u)=>u.InternalName == Event.RelatedStringOrNull);
                
                if ( upgrade == null )
                    throw new Exception("Could not find upgrade to grant! We were looking for " + Event.RelatedStringOrNull + ".");

                debugCode = 400;
                if ( upgrade.AdditionalEssenceCost > 0)
                    facOrNull.StoredFactionResourceOne -= upgrade.AdditionalEssenceCost;

                if ( upgrade.AdditionalAIPCost > 0 )
                {
                    Event.AIPchanged += (FInt)upgrade.AdditionalAIPCost;
                    GlobalAIWorldBaseInfo.Instance.ChangeAIP( (FInt)upgrade.AdditionalAIPCost, AIPChangeReason.Hacking, Target.TypeData, upgrade.RelatedShip, Hacker.GetFactionIndex_Safe(), planet.Index, -1 );
                }

                if ( upgrade.AdditionalHapCost > 0 )
                {
                    FInt hapAdded = upgrade.AdditionalHapCost.ToFInt();
                    if (Event.IsAgainstPlanet)
                        hapAdded *= type.GetHapCostScale(planet, true);
                    else
                        hapAdded *= type.GetHapCostScale(Target, true);
                    
                    Event.HackingPointsSpent += hapAdded;
                    Event.HackingPointsLeftAfterHack -= hapAdded;
                    facOrNull.StoredHacking -= hapAdded;
                    Target.GetFactionOrNull_Safe().HackingPointsUsedAgainstThisFaction += hapAdded;
                }
                
                var gData = facOrNull.TryGetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
                gData.NumRiftsHacked++;
                
                var thisEvent = NecromancerUpgradeEvent.Create( facOrNull.FactionIndex, Target.PlanetFaction.Faction.FactionIndex, Target.Planet.Index, upgrade.Index, -1, Target.TypeData);
                thisEvent.HackingPointsSpent = Event.HackingPointsSpent;
                thisEvent.HackingPointsLeftAfterHack = Event.HackingPointsLeftAfterHack;
                thisEvent.AIPchanged = Event.AIPchanged;
                thisEvent.EssenceSpent = (short)(Event.HackType.GetResourceOneCostForTarget(Target).ToInt() + upgrade.AdditionalEssenceCost);
                
                if ( upgrade.Type == NecromancerUpgradeType.GrantEssence )
                {
                    debugCode = 500;
                    Hacker.PlanetFaction.Faction.StoredFactionResourceOne += upgrade.RelatedResource;
                    
                    return true;
                }
                
                switch (upgrade.Target) 
                {
                    case NecromancerUpgradeTarget.Faction: 
                    {
                        debugCode = 600;
                        //ArcenDebugging.ArcenDebugLogSingleLine("Granting " + upgrade.ToString() + " to a faction" , Verbosity.DoNotShow );
                        
                        gData.NecromancerCompletedUpgrades.Add( upgrade );
                        gData.NecromancerHistory.Add( thisEvent );
                        
                        return true;
                    }
                    case NecromancerUpgradeTarget.Fleet: 
                    {
                        debugCode = 700;
                        //ArcenDebugging.ArcenDebugLogSingleLine("Granting " + upgrade.ToString() + " to a fleet" , Verbosity.DoNotShow );

                        var fleet = Hacker.FleetMembership.Fleet;
                        var fleetInfo = fleet.GetExternalBaseInfoAs<NecromancerMobileFleetBaseInfo>();
                        thisEvent.RelatedFleetId = fleet.FleetID;
                        
                        fleetInfo.NecromancerCompletedUpgrades.Add(upgrade);
                        gData.NecromancerHistory.Add( thisEvent );
                        
                        return true;
                    }
                    default: 
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "Unknown NecromancerUpgradeTarget " + upgrade.Target +" in Hacking_RiftHack DoSuccessfulCompletionLogic_Extra.", Verbosity.ShowAsError );
                        return false;
                    }
                }
            }
            catch ( Exception e )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Exception hit during Hacking_RiftHack DoSuccessfulCompletionLogic_Extra debug code " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            
            return false;
        }
        public override System.Collections.Generic.IEnumerable<GrantedShip> EnumeratePossibleShipGrants(GameEntity_Squad Target)
        {
            var data = Target?.TryGetExternalBaseInfoAs<TemplarPerUnitBaseInfo>();
            if ( data == null )
                yield break;
            
            foreach (var up in data.AvailableUpgrades)
            {
                if (up != null)
                {
                    // We want searches for "Wight" or "Vengeful Wight" to show us (TemplarRift)
                    // as a location hackable for it, even though what we actually grant
                    // is a "Vengeful Wight Home". 
                    // 
                    // And the count we want to show is "1" because
                    // this is a single location offering them.
                    //
                    // The actual count of wights you get depends on how many homes you build, after all.
                    //
                    if ( up.RelatedShip != null )
                        yield return new GrantedShip(up.RelatedShip, 1);
                    // We also want searches for "Wight" or "Bodyguard" to show the hacks which increase 
                    // a wight bodyguard cap.
                    else
                    if ( up.ShipTypeNameForCapIncrease != null )
                        yield return new GrantedShip(up.ShipForCapIncrease, 1);
                    // We also want the wight/skeleton softcap increase to appear for "wight" or "skeleton" searches.
                    else
                    if ( up.Type == NecromancerUpgradeType.IncreaseSkeletonSoftCap )
                        yield return new GrantedShip(GameEntityTypeDataTable.Instance.GetRowByName("BaseSkeleton"), 1);
                    else
                    if ( up.Type == NecromancerUpgradeType.IncreaseWightSoftCap )
                        yield return new GrantedShip(GameEntityTypeDataTable.Instance.GetRowByName("BaseWight"), 1);
                }
            }
        }
        
        protected override void Internal_GetMinAndMaxCostToHackForSidebar( List<NecromancerUpgrade> Items, GameEntity_Squad Target, Planet planet, Faction hackerFaction, HackingType Type, out FInt MinCost, out FInt MaxCost )
        {
            FInt? min = null;
            FInt? max = null;
            
            foreach (var u in Items)
            {
                var res = GetCanHackForThisItem(u, out _, Target, planet, Type);
                var cost = Type.GetHackPointCostForTarget(Target) + u.AdditionalHapCost * Type.GetHapCostScale(Target, true);
                
                if (res.IsHidden())
                    continue;

                if (!min.HasValue || cost < min.Value)
                    min = cost;
                if (!max.HasValue || cost > max.Value)
                    max = cost;
            }
            
            MinCost = FInt.Zero;
            if (min.HasValue)
                MinCost = min.Value;
            
            MaxCost = FInt.Zero;
            if (max.HasValue)
                MaxCost = max.Value;
        }
    }
    public class Hacking_SummonElderling : BaseHackingImplementation
    {
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            ElderlingsFactionBaseInfo BaseInfo = Target.PlanetFaction.Faction.TryGetExternalBaseInfoAs<ElderlingsFactionBaseInfo>();

            Planet newPlanet = BaseInfo.GetTranscendantPlanet_HostOnly( Context );
            String tag = "TranscendantElderling";
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, tag );

            PlanetFaction pFaction = newPlanet.GetPlanetFactionForFaction( Target.PlanetFaction.Faction );
            if ( entityData == null )
                throw new Exception("Could not find elderling with tag " +  tag );
            ArcenPoint spawnLocation = newPlanet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 200 ), FInt.FromParts( 0, 550 ) );
            GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, (byte)1,
                                                                                       pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Hacking-ElderlingPost" );  //is fine, main sim thread
            ElderlingsPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
            data.ExperienceRequired = 0;
            data.FullyUpgraded = true;
            data.NextEggLayingTime = World_AIW2.Instance.GameSecond + 600;
            return true;
        }
    }

    public class Hacking_InitiateShowdown : BaseHackingImplementation
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            return "你必须至少控制 " + (GlobalAIWorldBaseInfo.Instance.DevicesToSpawn - 1) + " 个决战装置在入侵期间。";
        }
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            int playerOwnedDevices  = 0;
            if ( Target.PlanetFaction.Faction.Type != FactionType.Player )
            {
                RejectionReasonDescription = "A player must own the structure to see the hack";
                return Hackable.NeverCanBeHacked_Hide;
            }
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "ShowdownDevice" ) )
            {
                if ( entity.PlanetFaction.Faction.Type == FactionType.Player )
                    playerOwnedDevices++;
            }
            if ( playerOwnedDevices < GlobalAIWorldBaseInfo.Instance.DevicesToSpawn - 1 )
            {
                RejectionReasonDescription = "你和你的盟友必须至少控制 " + (GlobalAIWorldBaseInfo.Instance.DevicesToSpawn - 1) + " 个装置才能触发决战；你拥有 "+ playerOwnedDevices;
                return Hackable.NeverBeHacked_ButStillShow;

            }
            if ( GlobalAIWorldBaseInfo.Instance.CrisisTriggered ||
                 GlobalAIWorldBaseInfo.Instance.CrisisCountdownTriggered ||
                 GlobalAIWorldBaseInfo.Instance.CrisisFailed )
            {
                RejectionReasonDescription = "决战危机已经被触发";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            GlobalAIWorldBaseInfo.Instance.CrisisCountdownTriggered = true;
            return true;
        }
    }

}
