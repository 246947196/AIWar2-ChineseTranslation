using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    /// <summary>Hacking implementation that handles a popup menu.</summary>
    ///
    /// To use this, implement the abstract methods defined below, as well as any
    /// relevant methods of <c>IHackingImplementation</c> that aren't defined here.
    ///
    /// <param name="T">The type representing an option to include in the menu.</param>
    /// <param name="W">A type wrapping <c>T</c>. This will typically either be <c>T</c>, or
    ///     <c>SafeSquadWrapper</c> when <c>T</c> is <c>GameEntity_Squad</c>.
    /// </param>
    public abstract class HackingImplementation_WithMenu<T, W>: BaseHackingImplementation
    {
        /// <summary>Called to unwrap a wrapped item.</summary>
        /// <param name="found">Indicates whether the item could be unwrapped.</param>
        public abstract T GetItemFromWrapper(W wrapper, out bool found);
        /// <summary>Called to generate the list of items to show in the menu.</summary>
        public abstract void PopulateItemsToShow(List<W> listToPopulate, GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, HackingType HackType);
        /// <summary>Called to check if the given item can be hacked.</summary>
        public abstract Hackable GetCanHackForThisItem(T item, out string rejectionReason, GameEntity_Squad Target, Planet planet, HackingType Type);
        /// <summary>Called to get the display name for the item.</summary>
        /// This will be shown on the button, and in the hack confirmation popup.</summary>
        public abstract void GetDisplayNameForItem(ArcenCharacterBufferBase buffer, T item);
        /// <summary>Called to get any extra info (such as AIP/hacking cost or fleet strength) to show on the button for the item.</summary>
        public virtual void GetExtraLabelInfoForItem(ArcenCharacterBufferBase buffer, T item, GameEntity_Squad Target, Planet planet, HackingType Type) {}
        /// <summary>Called to get the body of the tooltip to show for the item.</summary>
        public abstract void GetTooltipForItem(ArcenCharacterBufferBase buffer, T item, GameEntity_Squad Target, Planet planet, HackingType Type);
        public abstract void DoHack(T item, GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, HackingType HackType);
        public virtual bool HasDetailsOfContents { get { return false; } }
        public virtual MouseHandlingResult ViewDetailsOfContents(T item)
        {
            return MouseHandlingResult.PlayClickDeniedSound;
        }

        private static List<W> listOfItemsForCanBeHacked = List<W>.Create_WillNeverBeGCed( 600, "HackingMenu-listOfItemsForCanBeHacked" );
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string rejectionReason )
        {
            try {
                listOfItemsForCanBeHacked.Clear();
                this.PopulateItemsToShow(listOfItemsForCanBeHacked, Target, planet, Type);
            } catch (Exception e ) {
                ArcenDebugging.ArcenDebugLogSingleLine( "HackingMenu-GetCanBeHacked error from PopulateItemsToShow: " + e, Verbosity.ShowAsError );
                rejectionReason = "Error finding list of options to hack.";
                return Hackable.NotSureIfHasBeenHackedHacked_Hide;
            }

            bool shouldShow = false;
            bool canHack = false;
            rejectionReason = "";
            foreach (W wrapper in listOfItemsForCanBeHacked) {
                try {
                    T item = this.GetItemFromWrapper(wrapper, out bool found);
                    if (!found) {
                        continue;
                    }
                    switch (this.GetCanHackForThisItem(item, out string itemRejectionReason, Target, planet, Type)) {
                        case Hackable.CanBeHacked:
                            shouldShow = true;
                            canHack = true;
                            break;
                        case Hackable.NeverBeHacked_ButStillShow:
                        case Hackable.NotSureIfHasBeenHackedHacked_Show:
                            shouldShow = true;
                            if (rejectionReason == "") {
                                rejectionReason = itemRejectionReason;
                            } else if (rejectionReason != itemRejectionReason && itemRejectionReason != "") {
                                rejectionReason = "Varies";
                            }
                            break;
                    }
                } catch (Exception e) {
                    ArcenDebugging.ArcenDebugLogSingleLine( "HackingMenu-GetCanBeHacked error: " + e, Verbosity.ShowAsError );
                }
            }
            if (canHack) {
                rejectionReason = "";
                return Hackable.CanBeHacked;
            } else if (shouldShow) {
                return Hackable.NeverBeHacked_ButStillShow;
            } else {
                rejectionReason = "HackingImplementation_WithMenu can never be hacked";
                return Hackable.NeverCanBeHacked_Hide;
            }
        }

        public override void GetMinAndMaxCostToHackForSidebar( GameEntity_Squad Target, Planet planet, Faction hackerFaction, HackingType Type, out FInt MinCost, out FInt MaxCost )
        {
            MinCost = FInt.Zero;
            MaxCost = FInt.Zero;
            
            int debugstage = 0;
            try 
            {
                listOfItemsForCanBeHacked.Clear();
                this.PopulateItemsToShow(listOfItemsForCanBeHacked, Target, planet, Type);
            
                debugstage = 100;
                Internal_GetMinAndMaxCostToHackForSidebar( listOfItemsForCanBeHacked, Target, planet, hackerFaction, Type, out MinCost, out MaxCost );
            } 
            catch (Exception e) 
            {
                LOG.Err("Exception in {0}() at debugstage={1}.\n{2}", this.TypeNameAndMethod(), debugstage, e);
            }
        }

        protected virtual void Internal_GetMinAndMaxCostToHackForSidebar( List<W> Items, GameEntity_Squad Target, Planet planet, Faction hackerFaction, HackingType Type, out FInt MinCost, out FInt MaxCost )
        { 
            MinCost = FInt.Zero;
            MaxCost = FInt.Zero;
        }

        public override bool GetDoesHackRequireASinglularChoiceFromASubmenu()
        {
            return true;
        }

        private ArcenCachedExternalTypeDirect type_bCustomHackingOptionButton = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bCustomHackingOptionButton ) );

        private static List<W> listOfItemsLastUsedToPopulateCustomButton = List<W>.Create_WillNeverBeGCed( 600, "HackingMenu-listOfItemsLastUsedToPopulateCustomButton" );
        private static HackCustomButtonListInfo Info = new HackCustomButtonListInfo();
        private static HackingImplementation_WithMenu<T, W> Implementation = null;
        public override void AddAllButtonsForSingularChoiceInSubMenu( GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, Faction HackerFaction, HackingType HackType, ArcenUI_SetOfCreateElementDirectives Set,
            ref float runningY, UnityEngine.Rect firstBounds, float rowBuffer, AddHackButtonToWindow WindowAdder )
        {
            Implementation = this;
            Info.Update( TargetIfShip, TargetIfPlanet, HackerFaction, HackType );

            try {
                listOfItemsLastUsedToPopulateCustomButton.Clear();
                this.PopulateItemsToShow(listOfItemsLastUsedToPopulateCustomButton, TargetIfShip, TargetIfPlanet, HackType);
                for (int i = 0; i < listOfItemsLastUsedToPopulateCustomButton.Count; i++)
                {
                    //this part is the same for any hack
                    HackingUtils.PopulateOneCustomHackingOptionButton( Set, ref runningY, ref firstBounds, rowBuffer, WindowAdder,
                        string.Empty, i, i, type_bCustomHackingOptionButton );
                }
            } catch (Exception e ) {
                ArcenDebugging.ArcenDebugLogSingleLine( "HackingMenu-AddAllButtonsForSingularChoiceInSubMenu error: " + e, Verbosity.ShowAsError );
            }
        }

        #region GetTargetFromElement
        private static T GetItemFromElement(ArcenUI_Element element, out bool found)
        {
            if ( element?.CreatedByCodeDirective == null || listOfItemsLastUsedToPopulateCustomButton == null ) {
                found = false;
                return default(T);
            }
            int index = element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            if ( index < 0 || index >= listOfItemsLastUsedToPopulateCustomButton.Count ) {
                found = false;
                return default(T);
            }
            return Implementation.GetItemFromWrapper(listOfItemsLastUsedToPopulateCustomButton[index], out found);
        }
        #endregion

        #region bCustomHackingOptionButton
        public class bCustomHackingOptionButton : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                T item = GetItemFromElement(this.Element, out bool found);
                if (!found) {
                    buffer.Add( "<color=#c74639>Null item, this is a bug.</color>" );
                    return;
                }

                if (Implementation.GetCanHackForThisItem(item, out string _, Info.TargetShip, Info.TargetPlanet, Info.HackingType) != Hackable.CanBeHacked)
                    buffer.Add( "<color=#c74639>Not Possible:</color> " );
                
                Implementation.GetDisplayNameForItem(buffer, item);
                buffer.Add(" ");
                Implementation.GetExtraLabelInfoForItem(buffer, item, Info.TargetShip, Info.TargetPlanet, Info.HackingType);
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                T item = GetItemFromElement(this.Element, out bool found);
                if (!found) {
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                if ( Implementation.HasDetailsOfContents && InputCaching.CalculateHoldAndClickToViewDetailsOfContents() ) {
                    return Implementation.ViewDetailsOfContents(item);
                }

                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null ) {
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                string lastRejectionReason = ""; // FIXME: unused
                // FIXME: HackingUtils.CalculateCanDoThisHack calls HackingMenu.GetCanBeHacked which will check all item.
                // But we check this specific item again below.
                if ( !HackingUtils.CalculateCanDoThisHack( ref lastRejectionReason, Info.TargetShip, Info.TargetPlanet, Info.HackingType ) ) {
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                if (Implementation.GetCanHackForThisItem(item, out string _, Info.TargetShip, Info.TargetPlanet, Info.HackingType) != Hackable.CanBeHacked) {
                    return MouseHandlingResult.PlayClickDeniedSound;
                }

                bool doAreYouSurePrompt = GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) && !InputCaching.CalculateHoldAndSuppressTechUpgradePrompt();
                if ( doAreYouSurePrompt ) {
                    // FIXME: Info.TargetShip
                    GameEntity_Squad hacker = HackingUtils.GetPreferredHacker( Info.TargetShip, Info.HackingType, Info.TargetPlanet, true );
                    if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                        return MouseHandlingResult.PlayClickDeniedSound;
                    ArcenCharacterBuffer buffer = ArcenCharacterBuffer.GetFromPoolOrCreate("HackingMenu-Prompt-buffer");
                    buffer.Add("Are you sure you would like to do the hack ").Add(Info.HackingType.DisplayName).Add(" for ");
                    Implementation.GetDisplayNameForItem(buffer, item);
                    buffer.Add("?\n \n" + "<color=#888888>To disable this prompt, go into Game Settings, under the Game tab, and toggle this to OFF.  Or just hold down ");
                    buffer.Add(InputActionTypeDataTable.GetActionByName_FairlySlow( "SuppressTechUpgradePrompt" ).GetHumanReadableKeyCombo());
                    buffer.Add(" when clicking the upgrade button to skip it once.</color>");
                    ModalPopupData.CreateAndLogYesNoStyle( DoHack, null, "Are you sure", buffer.ToStringAndReturnToPool(), "Yes, Hack", "No, Do Not" );
                } else {
                    DoHack();
                }

                return MouseHandlingResult.None;
            }

            public void DoHack()
            {
                T item = GetItemFromElement(this.Element, out bool found);
                if (!found) {
                    return;
                }
                if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                    return;
                Implementation.DoHack(item, Info.TargetShip, Info.TargetPlanet, Info.HackingType);
            }

            private static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer("HackingImplementation_WithMenu-bCustomHackingOptionButton-tooltipBuffer");
            public override void HandleMouseover()
            {
                T item = GetItemFromElement(this.Element, out bool found);
                if (!found)
                {
                    Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "Null Target, this is a bug." );
                    return;
                }
                
                if (EntityText.Use == WriterToUse.Formatted)
                    tooltipBuffer.BeginStatement(TextStyle.Newline_NoLabel);
                
                tooltipBuffer.Add("<b><u>Hack: ").Add( Info.HackingType.DisplayName ).Add( "</u></b>\n" );

                Implementation.GetTooltipForItem(tooltipBuffer, item, Info.TargetShip, Info.TargetPlanet, Info.HackingType);

                if (Implementation.GetCanHackForThisItem(item, out string rejectionReason, Info.TargetShip, Info.TargetPlanet, Info.HackingType) != Hackable.CanBeHacked) {
                    tooltipBuffer.Add( "\n\n<color=#c74639>Cannot choose this option: " + rejectionReason + ".</color>" );
                } else {
                    if ( GameSettings.Current.GetBoolBySetting( "UpgradeShipPrompt" ) ) {
                        tooltipBuffer.Add( "\n\n<color=#3f6c9e>Hold </color><color=#4486d1>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "SuppressTechUpgradePrompt" ) )
                            .Add( "</color> <color=#3f6c9e>to suppress the 'Are you sure' prompt for a given hack.</color>  " );
                    }
                }

                if (EntityText.Use == WriterToUse.Formatted)
                    tooltipBuffer.EndStatement(TextStyle.Newline_NoLabel);
                
                Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
            }
        }
        #endregion
    }

    public abstract class HackingImplementation_WithTargetMenu: HackingImplementation_WithMenu<GameEntity_Squad, SafeSquadWrapper>
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            Planet plan = target == null ? planet : target.Planet;
            List<SafeSquadWrapper> eligibleTargets = GameEntity_Squad.GetTemporarySquadList( "HackingImplementation_WithTargetMenu-GetDynamicDescription-eligibleTargets", 10f );
            if ( eligibleTargets == null ) //blocked for teardown/shutdown; bail
                return null;
            ArcenCharacterBuffer buffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "HackingImplementation_WithTargetMenu-buffer" );
            try
            {
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
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "HackingImplementation_WithTargetMenu-GetDynamicDescription error: " + e, Verbosity.ShowAsError );
            }
            GameEntity_Squad.ReleaseTemporarySquadList( eligibleTargets );
            return buffer.ToStringAndReturnToPool();
        }

        public override void GetMinAndMaxCostToHackForSidebar( GameEntity_Squad Target, Planet planet, Faction hackerFaction, HackingType Type, out FInt MinCost, out FInt MaxCost )
        {
            List<SafeSquadWrapper> eligibleTargets = null;
            FInt minCost = FInt.Zero;
            FInt maxCost = FInt.Zero;
            int debugstage = 0;
            try
            {
                eligibleTargets = GameEntity_Squad.GetTemporarySquadList( "HackingImplementation_WithTargetMenu-GetMinAndMaxCostToHackForSidebar-eligibleTargets", 10f );
                if ( eligibleTargets == null ) //blocked for teardown/shutdown; bail
                {
                    MinCost = minCost;
                    MaxCost = maxCost;
                    return;
                }

                debugstage = 100;
                if (Target?.Planet != null)
                    planet = Target.Planet;
                
                debugstage = 200;
                HackingUtils.GetListOfTargetsForHackForPlanet( eligibleTargets, planet, hackerFaction, Type );
                debugstage = 300;
                HackingUtils.GetMinAndMaxHackingPointCostsForTargetList( eligibleTargets, Type, out minCost, out maxCost );
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    LOG.Msg("Exception in {0}() at debugstage={1} for HackingType {2}.\n{3}", this.TypeNameAndMethod(), debugstage, Type.OrNull(), e);
            }

            MinCost = minCost;
            MaxCost = maxCost;

            GameEntity_Squad.ReleaseTemporarySquadList( eligibleTargets );
        }

        public override GameEntity_Squad GetItemFromWrapper(SafeSquadWrapper wrapper, out bool found) {
            GameEntity_Squad entity = wrapper.GetSquad();
            found = entity != null;
            return entity;
        }
        public override void PopulateItemsToShow(List<SafeSquadWrapper> listToPopulate, GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, HackingType HackType)
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull(); // FIXME: unneeded
            HackingUtils.GetListOfTargetsForHackForPlanet( listToPopulate, TargetIfShip == null ? TargetIfPlanet : TargetIfShip.Planet, localFaction, HackType );
        }

        public override void GetDisplayNameForItem(ArcenCharacterBufferBase buffer, GameEntity_Squad target)
        {
            buffer.Add( target.TypeData.DisplayName );
        }
        public override void GetExtraLabelInfoForItem(ArcenCharacterBufferBase buffer, GameEntity_Squad target, GameEntity_Squad Target, Planet planet, HackingType HackType)
        {
            if (HackType.HaveSidebarRequestHackingPointCostRange) {
                FInt hackingCost = HackType.GetHackPointCostForTarget(target);

                buffer.Add(" ");
                buffer.AddHacking(hackingCost.IntValue, true);
            }
        }
        public virtual void GetTooltipForTarget(ArcenCharacterBufferBase buffer, GameEntity_Squad target, Planet planet, HackingType Type)
        {
            EntityText.GetTooltip( buffer, target, target.FleetMembership, target.TypeData, -1,
                    target.GetFactionOrNull_Safe(), target.CurrentMarkLevel, FromSidebarType.NonSidebar_SingleUnit, ShipExtraDetailFlags.None, 1f, false );
        }
        public sealed override void GetTooltipForItem(ArcenCharacterBufferBase buffer, GameEntity_Squad target, GameEntity_Squad _, Planet planet, HackingType Type)
        {
            GetTooltipForTarget(buffer, target, planet, Type);
        }
        public override void DoHack(GameEntity_Squad item, GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, HackingType HackType)
        {
            if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                return;

            string lastRejectionReason = ""; // FIXME: unused
            HackingUtils.TryDoHack(ref lastRejectionReason, item, item.Planet, HackType,
                    string.Empty, -1, null );
        }

        public virtual Hackable GetCanHackForThisTarget(out string rejectionReason, GameEntity_Squad Target, Planet planet, HackingType Type)
        {
            rejectionReason = "";
            return Hackable.CanBeHacked;
        }
        public sealed override Hackable GetCanHackForThisItem(GameEntity_Squad item, out string rejectionReason, GameEntity_Squad Target, Planet planet, HackingType Type)
        {
            return this.GetCanHackForThisTarget(out rejectionReason, item, planet, Type);
        }
    }
}
