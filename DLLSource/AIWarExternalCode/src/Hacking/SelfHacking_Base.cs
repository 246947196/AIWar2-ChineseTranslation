using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public abstract class Hacking_HackSelfUnit_Base : BaseHackingImplementation
    {
        #region GetNumberOfTimesCanBeHacked
        public int GetNumberOfTimesCanBeHacked( GameEntity_Squad Target, HackingType hackingType )
        {
            int timesCanBeHacked = 1;
            HackData hackData = this.GetHackDataForTargetOrNull( Target, hackingType );
            if ( hackData != null && hackData.MaxTimesSingleUnitCanBeHacked > 1 )
                timesCanBeHacked = hackData.MaxTimesSingleUnitCanBeHacked;
            return timesCanBeHacked;
        }
        #endregion

        #region GetCanBeHacked for Hacking_HackSelfUnit_Base
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, 
            string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( Target.GetFactionTypeSafe() != FactionType.Player )
            {
                RejectionReasonDescription = "This hack only works against units owned by a player.";
                return Hackable.NeverCanBeHacked_Hide;
            }
            Faction controllingFaction = Target.Planet.GetControllingFaction();
            if ( controllingFaction == null || controllingFaction.Type != FactionType.Player )
            {
                RejectionReasonDescription = "This hack can only be performed on planets owned by a player.";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            if ( HackerOrNull == null )
            {
                if ( Type.HackerMustBeBattlestation )
                    RejectionReasonDescription = "There are no battlestations/citadels on this planet, and one of those is required for this hack";
                else
                    RejectionReasonDescription = "There are no units that can hack on this planet";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            int numberOfTimesDone = this.GetNumberOfTimesHacked( Target, Type );
            int maxTimesCanBeDone = this.GetNumberOfTimesCanBeHacked( Target, Type );

            if ( numberOfTimesDone >= maxTimesCanBeDone )
            {
                RejectionReasonDescription = "This hack has already been done " + maxTimesCanBeDone;
                if ( maxTimesCanBeDone > 1 )
                    RejectionReasonDescription += " times, which is the maximum per unit.";
                else
                    RejectionReasonDescription += " time, which is the maximum per unit.";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            FInt cost = Type.GetHackPointCostForTarget( Target );
            if ( cost > HackerFaction.StoredHacking )
            {
                RejectionReasonDescription = "This hack costs more hacking points than you have.";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            RejectionReasonDescription = "";
            return Hackable.CanBeHacked;
        }
        #endregion

        public abstract void AddToDynamicDescription( ArcenDoubleCharacterBuffer buffer, GameEntity_Squad target, GameEntity_Squad hackerOrNull,
            Planet planet, Faction hackerFaction, HackingType hackingType );

        #region GetDynamicDescription
        private static ArcenDoubleCharacterBuffer buffer = new ArcenDoubleCharacterBuffer( "Hacking_HackSelfUnit_Base-GetDynamicDescription" );
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            if ( target == null )
                return string.Empty;

            AddToDynamicDescription( buffer, target, hackerOrNull, planet, hackerFaction, hackingType );

            int numberOfTimesHacked = this.GetNumberOfTimesHacked( target, hackingType );
            int maxTimesHacked = this.GetNumberOfTimesCanBeHacked( target, hackingType );

            buffer.Add( "\n<color=#96afe0>Hacked " ).Add( numberOfTimesHacked ).Add( " out of " ).Add( maxTimesHacked ).Add( " possible times.</color>" );

            return buffer.GetStringAndResetForNextUpdate();
        }
        #endregion

        public sealed override bool GetIsHackOfSelfUnit()
        {
            return true;
        }
    }

    public abstract class Hacking_SelfUnitWithSubSelection_Base : Hacking_HackSelfUnit_Base
    {
        public abstract bool DoSuccessfulCompletionSubLogic( GameEntity_Squad ChosenTargetOrNull, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event );

        #region DoSuccessfulCompletionLogic_Extra for Hacking_SelfUnitWithSubSelection_Base
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            return this.DoSuccessfulCompletionSubLogic( Target, Hacker, Context, type, Event );
        }
        #endregion

        #region GetMinAndMaxCostToHackForSidebar for Hacking_SelfUnitWithSubSelection_Base
        public override void GetMinAndMaxCostToHackForSidebar( GameEntity_Squad Target, Planet planet, Faction hackerFaction, HackingType Type, out FInt MinCost, out FInt MaxCost )
        {
            Planet plan = Target == null ? planet : Target.Planet;
            List<SafeSquadWrapper> eligibleTargets = GameEntity_Squad.GetTemporarySquadList( "Hacking_SelfUnitWithSubSelection_Base-GetMinAndMaxCostToHackForSidebar-eligibleTargets", 90f );
            if ( eligibleTargets == null ) //blocked for teardown/shutdown; bail
            {
                MinCost = FInt.Zero;
                MaxCost = FInt.Zero;
                return;
            }

            try
            {
                HackingUtils.GetListOfTargetsForHackForPlanet( eligibleTargets, plan, hackerFaction, Type );

                FInt minCost, maxCost;
                HackingUtils.GetMinAndMaxHackingPointCostsForTargetList( eligibleTargets, Type, out minCost, out maxCost );
                MinCost = minCost;
                MaxCost = maxCost;
            }
            catch ( System.Threading.ThreadAbortException )  //simply return
            {
                MinCost = FInt.Zero;
                MaxCost = FInt.Zero;
            }
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Exception in GetMinAndMaxCostToHackForSidebar for selfhacking base: " + e, Verbosity.ShowAsError );
                MinCost = FInt.Zero;
                MaxCost = FInt.Zero;
            }

            GameEntity_Squad.ReleaseTemporarySquadList( eligibleTargets );
        }
        #endregion

        public abstract void WriteAnyDynamicDescriptionSubData( ArcenDoubleCharacterBuffer buffer, List<SafeSquadWrapper> eligibleTargets, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType );

        #region AddToDynamicDescription for Hacking_SelfUnitWithSubSelection_Base
        public override void AddToDynamicDescription( ArcenDoubleCharacterBuffer buffer, GameEntity_Squad target, GameEntity_Squad hackerOrNull,
            Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            Planet plan = target == null ? planet : target.Planet;
            List<SafeSquadWrapper> eligibleTargets = GameEntity_Squad.GetTemporarySquadList( "Hacking_SelfUnitWithSubSelection_Base-AddToDynamicDescription-eligibleTargets", 10f );
            if ( eligibleTargets == null ) //blocked for teardown/shutdown; bail
                return;
            HackingUtils.GetListOfTargetsForHackForPlanet( eligibleTargets, plan, hackerFaction, hackingType );

            FInt minCost, maxCost;
            HackingUtils.GetMinAndMaxHackingPointCostsForTargetList( eligibleTargets, hackingType, out minCost, out maxCost );

            buffer.StartColor( "9cbad3" );
            if ( eligibleTargets.Count > 0 )
            {
                buffer.Add( eligibleTargets.Count > 1 ? "Will target one of: " : "Will target: " );
                int index = 0;
                foreach ( SafeSquadWrapper wrap in eligibleTargets )
                {
                    GameEntity_Squad eligibleTarget = wrap.GetSquad();
                    if ( eligibleTarget == null )
                        continue;
                    if ( index > 0 )
                    {
                        buffer.Add( ", " );
                    }
                    buffer.Add( eligibleTarget.TypeData.DisplayName );
                    index++;
                }
                buffer.Add( "." );
                if ( eligibleTargets.Count > 1 )
                {
                    buffer.Add( "  You will be able to choose your target after initiating the hack." );
                    if ( minCost.IntValue != maxCost.IntValue )
                        buffer.Add( "  The costs in hacking points vary from " ).Add( minCost.IntValue ).Add( " to " ).Add( maxCost.IntValue ).Add( " depending on your choice." );
                }
            }
            else
            {
                buffer.Add( "Huh!  No eligible targets on " ).Add( (plan == null ? "null" : plan.Name) ).Add( "?  This is almost certainly a bug.  " );
            }
            buffer.EndColor();

            this.WriteAnyDynamicDescriptionSubData( buffer, eligibleTargets, hackerOrNull, planet, hackerFaction, hackingType );

            GameEntity_Squad.ReleaseTemporarySquadList( eligibleTargets );
        }
        #endregion

        public override bool GetDoesHackRequireASinglularChoiceFromASubmenu()
        {
            return true;
        }

        public abstract SubItemDropdownWriter GetSubItemDropdownWriter();

        public delegate void SubItemDropdownWriter( ArcenDoubleCharacterBuffer buffer, GameEntity_Squad CurrentOption, Faction hackerFaction, HackingType hackingType );

        public abstract SubItemButtonWriter GetSubItemButtonWriter();

        public delegate void SubItemButtonWriter( ArcenDoubleCharacterBuffer buffer, GameEntity_Squad CurrentOption, Faction hackerFaction, HackingType hackingType );

        private ArcenCachedExternalTypeDirect type_bCustomHackingOptionButton = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bCustomHackingOptionButton ) );

        #region AddAllButtonsForSingularChoiceInSubMenu for Hacking_SelfUnitWithSubSelection_Base
        private static List<SafeSquadWrapper> listOfItemsLastUsedToPopulateCustomButton = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Hacking_SelfUnitWithSubSelection_Base-listOfItemsLastUsedToPopulateCustomButton" );
        private static Hacking_SelfUnitWithSubSelection_Base Implementation = null;
        private static HackCustomButtonListInfo Info = new HackCustomButtonListInfo();
        private static SubItemDropdownWriter LastSubItemDropdownWriter = null;
        private static SubItemButtonWriter LastSubItemButtonWriter = null;
        public override void AddAllButtonsForSingularChoiceInSubMenu( GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, Faction HackerFaction, HackingType HackType, ArcenUI_SetOfCreateElementDirectives Set,
            ref float runningY, UnityEngine.Rect firstBounds, float rowBuffer, AddHackButtonToWindow WindowAdder )
        {
            Implementation = this;
            LastSubItemDropdownWriter = this.GetSubItemDropdownWriter();
            LastSubItemButtonWriter = this.GetSubItemButtonWriter();
            Info.Update( TargetIfShip, TargetIfPlanet, HackerFaction, HackType );

            List<SafeSquadWrapper> eligibleTargets = GameEntity_Squad.GetTemporarySquadList( "Hacking_SelfUnitWithSubSelection_Base-AddAllButtonsForSingularChoiceInSubMenu-eligibleTargets", 10f );
            if ( eligibleTargets == null ) //blocked for teardown/shutdown; bail
                return;
            HackingUtils.GetListOfTargetsForHackForPlanet( eligibleTargets, TargetIfShip == null ? TargetIfPlanet : TargetIfShip.Planet, HackerFaction, HackType );
            listOfItemsLastUsedToPopulateCustomButton.Clear();
            for ( int i = 0; i < eligibleTargets.Count; i++ )
            {
                listOfItemsLastUsedToPopulateCustomButton.Add( eligibleTargets[i] );
                //this part is the same for any hack
                HackingUtils.PopulateOneCustomHackingOptionButton( Set, ref runningY, ref firstBounds, rowBuffer, WindowAdder,
                    //this part differs for each hack type, telling it how to tell each option apart
                    string.Empty, i, i,
                    //and this part is also different for each hack type, pointing to its own unique button implementation.
                    //they can all be called bCustomHackingOptionButton, since they are a subclass of the hacking implementation class.
                    type_bCustomHackingOptionButton );
            }
            GameEntity_Squad.ReleaseTemporarySquadList( eligibleTargets );
        }
        #endregion

        #region GetTargetFromElement
        public static GameEntity_Squad GetTargetFromElement( ArcenUI_Element element )
        {
            if ( element == null || listOfItemsLastUsedToPopulateCustomButton == null )
                return null;
            if ( element.CreatedByCodeDirective == null )
                return null;
            int index = element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            if ( index < 0 || index >= listOfItemsLastUsedToPopulateCustomButton.Count )
                return null;
            return listOfItemsLastUsedToPopulateCustomButton[index].GetSquad();
        }
        #endregion

        #region bCustomHackingOptionButton
        public class bCustomHackingOptionButton : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                GameEntity_Squad target = GetTargetFromElement( this.Element );
                if ( target == null )
                {
                    Buffer.Add( "<color=#c74639>Null Target</color>" );
                    return;
                }

                if ( !this.GetCanHackForThisItem( target ) )
                {
                    Buffer.Add( "<color=#c74639>Not Possible:</color> " );
                    Buffer.Add( target.TypeData.DisplayName );
                }
                else
                    Buffer.Add( target.TypeData.DisplayName );

                FInt hackingCost = Info.HackingType.GetHackPointCostForTarget( target );

                Buffer.Add(" ").AddHacking(hackingCost.IntValue, true);

                if ( LastSubItemButtonWriter != null )
                    LastSubItemButtonWriter( tooltipBuffer, target, Info.HackFaction, Info.HackingType );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                GameEntity_Squad target = GetTargetFromElement( this.Element );
                if ( target == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                if ( !this.GetCanHackForThisItem( target ) )
                    return MouseHandlingResult.PlayClickDeniedSound;

                bool doAreYouSurePrompt = GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) && !InputCaching.CalculateHoldAndSuppressTechUpgradePrompt();
                if ( doAreYouSurePrompt )
                {
                    GameEntity_Squad hacker = HackingUtils.GetPreferredHacker( Info.TargetShip, Info.HackingType, Info.TargetPlanet, true );
                    if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                        return MouseHandlingResult.PlayClickDeniedSound;
                    ModalPopupData.CreateAndLogYesNoStyle( DoHack, null, "Are you sure", "Are you sure you would like to do the hack " + Info.HackingType.DisplayName +
                        " for " + target.GetTypeDisplayNameSafe() + "?\n \n" + "<color=#888888>To disable this prompt, go into Game Settings, under the Game tab, and toggle this to OFF.  Or just hold down " +
                        InputActionTypeDataTable.GetActionByName_FairlySlow( "SuppressTechUpgradePrompt" ).GetHumanReadableKeyCombo() + " when clicking the upgrade button to skip it once.</color>", "Yes, Hack", "No, Do Not" );
                }
                else
                    DoHack();


                return MouseHandlingResult.None;
            }

            public void DoHack()
            {
                GameEntity_Squad target = GetTargetFromElement( this.Element );
                if ( target == null )
                    return;
                GameEntity_Squad hacker = HackingUtils.GetPreferredHacker( Info.TargetShip, Info.HackingType, Info.TargetPlanet, true );
                if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                    return;

                HackingUtils.TryDoHack( ref lastRejectionReason, target, Info.TargetPlanet, Info.HackingType,
                                        string.Empty, -1, null );
            }

            private string lastRejectionReason = string.Empty;
            public bool GetCanHackForThisItem( GameEntity_Squad target )
            {
                GameEntity_Squad hacker = HackingUtils.CalculateHackerForHack( target, Info.TargetPlanet, Info.HackingType, false ); //this is handled above, in terms of true cases
                if ( hacker == null )
                {
                    lastRejectionReason = "No hacker available at the moment";
                    return false;
                }
                return Implementation.GetCanBeHacked( target, hacker,
                    Info.TargetPlanet, hacker.GetFactionOrNull_Safe(), Info.HackingType,
                    this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString, -1, out lastRejectionReason ) == Hackable.CanBeHacked;
            }

            private static ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Hacking_SelfUnitWithSubSelection_Base-bCustomHackingOptionButton-tooltipBuffer" );
            public override void HandleMouseover()
            {
                GameEntity_Squad target = GetTargetFromElement( this.Element );
                if ( target == null )
                    Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "Null Target, this is a bug." );
                else
                {
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();

                    tooltipBuffer.Add( "<b><u>Hack: " ).Add( Info.HackingType.DisplayName ).Add( "</u></b>\n" );

                    if ( LastSubItemDropdownWriter != null )
                        LastSubItemDropdownWriter( tooltipBuffer, target, Info.HackFaction, Info.HackingType );

                    EntityText.GetTooltip( tooltipBuffer, target, target.FleetMembership, target.TypeData, -1,
                        target.GetFactionOrNull_Safe(), target.CurrentMarkLevel, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None, 1f, false );

                    if ( !this.GetCanHackForThisItem( target ) )
                        tooltipBuffer.Add( "\n\n<color=#c74639>Cannot choose this option: " + this.lastRejectionReason + ".\n</color>" );
                    else
                    {
                        if ( GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) )
                        {
                            tooltipBuffer.Add( "\n\n<color=#3f6c9e>Hold </color><color=#4486d1>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "SuppressTechUpgradePrompt" ) )
                                .Add( "</color> <color=#3f6c9e>to suppress the 'Are you sure' prompt for a given hack.</color>  " );
                        }
                    }

                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
                }
            }
        }
        #endregion
    }
}
