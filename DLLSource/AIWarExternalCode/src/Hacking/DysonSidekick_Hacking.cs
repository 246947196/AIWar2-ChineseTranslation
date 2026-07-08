using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Hacking_DismantleStronghold : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked(GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription)
        {
            // if ( HackerOrNull != null &&
            //      Target.PlanetFaction.Faction != HackerOrNull.PlanetFaction.Faction )
            // {
            //     RejectionReasonDescription = "You must already own this structure";
            //     return Hackable.NeverCanBeHacked_Hide;
            // }
            DysonSidekickFactionBaseInfo bInfo = Target.PlanetFaction.Faction.GetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
            int majorStrongholdsFound = 0;
            foreach ( GameEntity_Squad entity in bInfo.Strongholds.DisplaySquads() )
            {
                if ( entity.TypeData.GetHasTag("DysonMajorStronghold"))
                    majorStrongholdsFound++;
            }
            if ( majorStrongholdsFound <= 1 &&
                 Target.TypeData.GetHasTag("DysonMajorStronghold"))
            {
                RejectionReasonDescription = "You can't dismantle your only flagship.";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event ){
            DysonSidekickFactionBaseInfo bInfo = Target.PlanetFaction.Faction.GetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
            bInfo.DismantleStronghold( Target, Context);
            return true;
        }

    }
    public class Hacking_GrantModulePoints : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked(GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription)
        {
            if ( Target.ModulePoints > 100 )
            {
                //ModulePoints is a byte, so just make sure we don't overflow
                RejectionReasonDescription = "Too many module unlocks";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event ){

            Target.ModulePoints++;
            return true;
        }

    }
    public class Hacking_ChooseEpistyleProduction : BaseHackingImplementation
    {
        //Allows the Dark Zenith Sidekick player to tell an Epistyle to build something in particular
        public override Hackable GetCanBeHacked(GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription)
        {
            RejectionReasonDescription = "";
            if ( Target.PlanetFaction.Faction != HackerFaction )
            {
                RejectionReasonDescription = "You must own this structure";
                return Hackable.NeverCanBeHacked_Hide;
            }
            DarkZenithSidekickFactionBaseInfo dInfo = Target.PlanetFaction.Faction.TryGetExternalBaseInfoAs<DarkZenithSidekickFactionBaseInfo>();
            if ( dInfo == null )
                return Hackable.NeverCanBeHacked_Hide;

            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool GetDoesHackRequireASinglularChoiceFromASubmenu()
        {
            return true;
        }

        private ArcenCachedExternalTypeDirect type_bCustomHackingOptionButton = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bCustomHackingOptionButton ) );
        private static readonly List<DZResourceConversion> listOfItemsLastUsedToPopulateCustomButton = List<DZResourceConversion>.Create_WillNeverBeGCed( 60, "Hacking_ChooseEpistyleProduction-listOfItemsLastUsedToPopulateCustomButton" );
        private static Hacking_ChooseEpistyleProduction Implementation = null;
        private static HackCustomButtonListInfo Info = new HackCustomButtonListInfo();
        public override void AddAllButtonsForSingularChoiceInSubMenu( GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, Faction HackerFaction,
                                                                      HackingType HackType, ArcenUI_SetOfCreateElementDirectives Set,
                                                                      ref float runningY, UnityEngine.Rect firstBounds, float rowBuffer, AddHackButtonToWindow WindowAdder )
        {
            Implementation = this;
            DarkZenithPerUnitBaseInfo data = TargetIfShip.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
            Info.Update( TargetIfShip, TargetIfPlanet, HackerFaction, HackType );
            listOfItemsLastUsedToPopulateCustomButton.Clear();
            int count = 0;
            DarkZenithSidekickFactionBaseInfo dInfo = HackerFaction.GetExternalBaseInfoAs<DarkZenithSidekickFactionBaseInfo>();
            for ( int i = 0; i < data.ConversionBag.InternalListSize; i++ )
            {
                DZResourceConversion conversion = data.ConversionBag.GetInternalListItemAtIndex(i);
                 if ( dInfo != null &&
                     !dInfo.ShouldShowEpistyleChoice( conversion))
                 {
                     continue;
                 }

                listOfItemsLastUsedToPopulateCustomButton.Add( conversion );
                //this part is the same for any hack
                HackingUtils.PopulateOneCustomHackingOptionButton( Set, ref runningY, ref firstBounds, rowBuffer, WindowAdder,
                                                                   //this part differs for each hack type, telling it how to tell each option apart
                                                                   string.Empty, count, count,
                                                                   //and this part is also different for each hack type, pointing to its own unique button implementation.
                                                                   //they can all be called bCustomHackingOptionButton, since they are a subclass of the hacking implementation class.
                                                                   type_bCustomHackingOptionButton );
                count++;
            }
        }
        #region bCustomHackingOptionButton
        public class bCustomHackingOptionButton : ButtonAbstractBase
        {
           #region GetTechUpgradeFromElement
           public static DZResourceConversion GetResourceConversionFromElement( ArcenUI_Element element )
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
                int debugCode = 0;
                try{
                    debugCode = 100;
                    DZResourceConversion conversion = GetResourceConversionFromElement( this.Element );
                    if ( conversion == null )
                    {
                        Buffer.Add( "<color=#c74639>Null Resource Conversion</color>" );
                        return;
                    }
                    debugCode = 200;
                    if ( !this.GetCanHackForThisItem( conversion ) )
                    {
                        Buffer.Add( "<color=#c74639>不可行：</color> " );
                    }
                    debugCode = 300;
                    Buffer.Add("<size=80%>");
                    conversion.ToHackBuffer( Buffer );
                    Buffer.Add("</size>");
                }
                catch( Exception e)
                {
                    ArcenDebugging.LogSingleLine("Hit exception in GetTextToShowFromVolatile " + e.ToString() + " " + debugCode, Verbosity.DoNotShow );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                DZResourceConversion conversion = GetResourceConversionFromElement( this.Element );
                if ( conversion == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                if ( !this.GetCanHackForThisItem( conversion ) )
                  return MouseHandlingResult.PlayClickDeniedSound;

                DoHack( input );

                return MouseHandlingResult.None;
            }

            public void DoHack( MouseHandlingInput input )
            {
                DZResourceConversion conversion = GetResourceConversionFromElement( this.Element );
                if ( conversion == null )
                    return;
                if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                    return;
                //Send a GameCommand
                bool continueBuilding = input.RightButtonClicked;
                bool highPriority = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1();
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.UpdateEpistyleProduction], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedEntityIDs.Add( Info.TargetShip.PrimaryKeyID );
                command.RelatedString = conversion.InternalName;
                command.RelatedBools.Add( continueBuilding );
                command.RelatedBools.Add( highPriority );
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                Window_HackChoicesSidebarPopout.Instance.Close();
            }

            private string lastRejectionReason = string.Empty;
            public bool GetCanHackForThisItem( DZResourceConversion item )
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
                    if ( faction == null ||
                         faction.Type != FactionType.Player) //I saw some weird errors where the ExternalBaseInfo was from the Neutral Faction
                    {
                        lastRejectionReason = "No faction found";
                        return false;
                    }
                    debugCode = 130;
                    if ( item == null )
                    {
                        throw new Exception("How is item null?");
                    }
                    debugCode = 250;
                    DarkZenithSidekickFactionBaseInfo dInfo = faction.GetExternalBaseInfoAs<DarkZenithSidekickFactionBaseInfo>();
                    if ( dInfo == null )
                    {
                        lastRejectionReason = "No Faction BaseInfo";
                        return false;
                    }

                    if ( !dInfo.IsBuildPossible( item.Cost ) )
                    {
                        lastRejectionReason = "We don't have access to a required resource";
                        return false;
                    }
                    if ( !dInfo.ShouldShowEpistyleChoice( item ) )
                    {
                        //Note: this shouldn't actually be possible since those shouldn't be shown,
                        //but in case we remove that restriction this will be helpful
                        lastRejectionReason = "We are missing pre-reqs for this upgrade (or it has already been unlocked)";
                        return false;
                    }

                    debugCode = 500;
                    return Implementation.GetCanBeHacked( Info.TargetShip, hacker,
                                                          Info.TargetPlanet, hacker.GetFactionOrNull_Safe(), Info.HackingType,
                                                          this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString, -1, out lastRejectionReason ) == Hackable.CanBeHacked;
                } catch(Exception e)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit excpetion in choose epistyle GetCanHackForThisItem debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
                }

                return false;
            }

            private static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Hacking_ChooseEpistyleProduction-bCustomHackingOptionButton-tooltipBuffer" );
            public override void HandleMouseover()
            {
                int debugCode = 0;
                DZResourceConversion conversion = GetResourceConversionFromElement( this.Element );
                try{
                    debugCode = 100;
                    if ( conversion == null )
                    {
                        Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "Null DZResourceConversion, this is a bug." );
                        return;
                    }
                    else
                    {
                        if (!this.GetCanHackForThisItem(conversion))
                        { 
                            tooltipBuffer.Add( "\n\n<color=#c74639>无法选择该选项： " + this.lastRejectionReason + "。</color>\n" );
                            conversion.ToBuffer( tooltipBuffer );
                        }
                        else
                        { 
                            debugCode = 200;
                            Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                            debugCode = 300;
                            tooltipBuffer.Add( "<b><u>黑客： " ).Add( Info.HackingType.DisplayName ).Add( "</u></b>\n" );
                            tooltipBuffer.Add("更新此门楣以建造以下内容： ");
                            conversion.ToBuffer( tooltipBuffer );
                            //tooltipBuffer.Add("internal: " + conversion.InternalName);
                            if ( conversion.Description != null )
                            {
                                tooltipBuffer.Add("<size=80%>");
                                tooltipBuffer.Add(conversion.Description, "ffa1ff").Add("\n");
                                tooltipBuffer.Add("</size>");
                            }
                            if (Info.TargetShip != null)
                            {
                                DarkZenithPerUnitBaseInfo data = Info.TargetShip.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>("DarkZenithPerUnitBaseInfo");
                                tooltipBuffer.Add("当前资源： ");
                                foreach ( KeyValuePair<DZResource, int> kv in data.Inventory )
                                {
                                    debugCode = 10;
                                    if ( kv.Value != 0 )
                                    {
                                        debugCode = 20;
                                        tooltipBuffer.Add( kv.Value.ToString(), DarkZenithFactionBaseInfo.ResourceColour[kv.Key] ).Add( " " );
                                    }
                                }
                                tooltipBuffer.Add("\n");
                                if (data.NextConversion != null)
                                { 
                                    tooltipBuffer.Add("当前建造中： ");
                                    data.NextConversion.ToBuffer( tooltipBuffer );
                                }
                            }
                            tooltipBuffer.Add("\n\n");
                            tooltipBuffer.Add("左键点击", "996f4c").Add(" 将使门楣建造此内容，之后该阵营将继续自动选择。\n");
                            tooltipBuffer.Add("右键点击", "996f4c").Add(" 将使门楣持续建造此选项，直到玩家给出新指令。\n");
                            tooltipBuffer.Add( "按住 <color=#996f4c>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" ) ).Add( "</color> 设为高优先级。\n" );
                        }
                        Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
                    }
                } catch( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in mouseover for a transformation: " + conversion.ToString() + ". debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
                }
            }
        }
        #endregion

    }
    public class Hacking_ChooseFlagshipForEpistyle : BaseHackingImplementation
    {
        //Allows the Dark Zenith Sidekick player to tell an Epistyle to build something in particular
        public override Hackable GetCanBeHacked(GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription)
        {
            RejectionReasonDescription = "";
            if ( Target.PlanetFaction.Faction != HackerFaction )
            {
                RejectionReasonDescription = "You must own this structure";
                return Hackable.NeverCanBeHacked_Hide;
            }
            DarkZenithSidekickFactionBaseInfo dInfo = Target.PlanetFaction.Faction.GetExternalBaseInfoAs<DarkZenithSidekickFactionBaseInfo>();
            if ( dInfo == null )
                return Hackable.NeverCanBeHacked_Hide;
            DarkZenithPerUnitBaseInfo dzPerUnitInfo = Target.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
            if ( !dzPerUnitInfo.CanBuildOffensiveUnits )
            {
                RejectionReasonDescription = "This epistyle doesn't build combat ships";
                return Hackable.NeverCanBeHacked_Hide;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool GetDoesHackRequireASinglularChoiceFromASubmenu()
        {
            return true;
        }

        private ArcenCachedExternalTypeDirect type_bCustomHackingOptionButton = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bCustomHackingOptionButton ) );
        private static readonly List<GameEntity_Squad> listOfItemsLastUsedToPopulateCustomButton = List<GameEntity_Squad>.Create_WillNeverBeGCed( 60, "Hacking_ChooseFlagshipForEpistyle-listOfItemsLastUsedToPopulateCustomButton" );
        private static Hacking_ChooseFlagshipForEpistyle Implementation = null;
        private static HackCustomButtonListInfo Info = new HackCustomButtonListInfo();
        public override void AddAllButtonsForSingularChoiceInSubMenu( GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, Faction HackerFaction,
                                                                      HackingType HackType, ArcenUI_SetOfCreateElementDirectives Set,
                                                                      ref float runningY, UnityEngine.Rect firstBounds, float rowBuffer, AddHackButtonToWindow WindowAdder )
        {
            Implementation = this;
            DarkZenithPerUnitBaseInfo data = TargetIfShip.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
            Info.Update( TargetIfShip, TargetIfPlanet, HackerFaction, HackType );
            listOfItemsLastUsedToPopulateCustomButton.Clear();
            int count = 0;
            DarkZenithSidekickFactionBaseInfo dInfo = HackerFaction.GetExternalBaseInfoAs<DarkZenithSidekickFactionBaseInfo>();
            List<SafeSquadWrapper> flagships = dInfo.Flagships.GetDisplayList();
            for (int i = 0; i < flagships.Count; i++)
            {
                GameEntity_Squad flagship = flagships[i].GetSquad();
                if (flagship == null)
                    continue;

                listOfItemsLastUsedToPopulateCustomButton.Add( flagship );
                //this part is the same for any hack
                HackingUtils.PopulateOneCustomHackingOptionButton( Set, ref runningY, ref firstBounds, rowBuffer, WindowAdder,
                                                                   //this part differs for each hack type, telling it how to tell each option apart
                                                                   string.Empty, count, count,
                                                                   //and this part is also different for each hack type, pointing to its own unique button implementation.
                                                                   //they can all be called bCustomHackingOptionButton, since they are a subclass of the hacking implementation class.
                                                                   type_bCustomHackingOptionButton );
                count++;
            }
        }
        #region bCustomHackingOptionButton
        public class bCustomHackingOptionButton : ButtonAbstractBase
        {
           #region GetTechUpgradeFromElement
           public static GameEntity_Squad GetFlagshipFromElement( ArcenUI_Element element )
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
                int debugCode = 0;
                try{
                    debugCode = 100;
                    GameEntity_Squad flagship = GetFlagshipFromElement( this.Element );
                    if ( flagship == null )
                    {
                        Buffer.Add( "<color=#c74639>No Flagship</color>" );
                        return;
                    }
                    debugCode = 200;
                    if ( !this.GetCanHackForThisItem( flagship ) )
                    {
                        Buffer.Add( "<color=#c74639>不可行：</color> " );
                    }
                    debugCode = 300;
                    Buffer.Add("<size=80%>");
                    Buffer.Add(flagship.TypeData.GetDisplayName(), "a1a1ff").Add( " on ").Add( flagship.Planet.Name, "a1a1a1");
                    Buffer.Add("</size>");
                }
                catch( Exception e)
                {
                    ArcenDebugging.LogSingleLine("Hit exception in GetTextToShowFromVolatile " + e.ToString() + " " + debugCode, Verbosity.DoNotShow );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                GameEntity_Squad flagship = GetFlagshipFromElement( this.Element );
                if ( flagship == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                if ( !this.GetCanHackForThisItem( flagship ) )
                  return MouseHandlingResult.PlayClickDeniedSound;

                DoHack( input, flagship );

                return MouseHandlingResult.None;
            }

            public void DoHack( MouseHandlingInput input, GameEntity_Squad flagship )
            {
                if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                    return;
                //Send a GameCommand
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.UpdateEpistyleProduction], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedEntityIDs.Add( Info.TargetShip.PrimaryKeyID );
                command.RelatedEntityIDs.Add( flagship.PrimaryKeyID );
                bool resetValue = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1();
                command.RelatedBools.Add( resetValue );

                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                Window_HackChoicesSidebarPopout.Instance.Close();
            }

            private string lastRejectionReason = string.Empty;
            public bool GetCanHackForThisItem( GameEntity_Squad item )
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
                    if ( faction == null ||
                         faction.Type != FactionType.Player) //I saw some weird errors where the ExternalBaseInfo was from the Neutral Faction
                    {
                        lastRejectionReason = "No faction found";
                        return false;
                    }
                    debugCode = 130;
                    if ( item == null )
                    {
                        throw new Exception("How is item null?");
                    }
                    debugCode = 250;
                    DarkZenithSidekickFactionBaseInfo dInfo = faction.GetExternalBaseInfoAs<DarkZenithSidekickFactionBaseInfo>();
                    if ( dInfo == null )
                    {
                        lastRejectionReason = "No Faction BaseInfo";
                        return false;
                    }

                    debugCode = 500;
                    return Implementation.GetCanBeHacked( Info.TargetShip, hacker,
                                                          Info.TargetPlanet, hacker.GetFactionOrNull_Safe(), Info.HackingType,
                                                          this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString, -1, out lastRejectionReason ) == Hackable.CanBeHacked;
                } catch(Exception e)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit excpetion in choose epistyle GetCanHackForThisItem debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
                }

                return false;
            }

            private static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Hacking_ChooseEpistyleProduction-bCustomHackingOptionButton-tooltipBuffer" );
            public override void HandleMouseover()
            {
                int debugCode = 0;
                GameEntity_Squad flagship = GetFlagshipFromElement( this.Element );
                try{
                    debugCode = 100;
                    if ( flagship == null )
                    {
                        Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "Null flagship, this is a bug." );
                        return;
                    }
                    else
                    {
                        if (!this.GetCanHackForThisItem(flagship))
                        { 
                            tooltipBuffer.Add( "\n\n<color=#c74639>Cannot choose this option: " + this.lastRejectionReason + ".</color>\n" );
                        }
                        else
                        { 
                            debugCode = 200;
                            Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                            debugCode = 300;
                            tooltipBuffer.Add( "<b><u>黑客： " ).Add( Info.HackingType.DisplayName ).Add( "</u></b>\n" );
                            tooltipBuffer.Add("更新此门楣以集结舰船至： ");
                            tooltipBuffer.Add(flagship.TypeData.GetDisplayName() , "a1ffa1").Add( " on ").Add( flagship.Planet.Name, "ffa1a1");
                            Fleet fleet = flagship.FleetMembership.Fleet;
                            tooltipBuffer.Add("\n").Add("快捷键 ").Add( fleet.TiedToKeybindIndexOneIndexed, "ff23ff" );
                        }
                        tooltipBuffer.Add( "\n按住 <color=#996f4c>" ).Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" ) ).Add( "</color> 使此门楣恢复默认行为，集结至最近的旗舰。\n" );
                        Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
                    }
                } catch( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in mouseover for a flagship: " + flagship.ToStringWithPlanet() + ". debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
                }
            }
        }
        #endregion

    }
    public class Hacking_FimbulwinterPlanet : BaseHackingImplementation
    {
        //Allows the Dark Zenith Sidekick player to tell an Epistyle to build something in particular
        public override Hackable GetCanBeHacked(GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription)
        {
            RejectionReasonDescription = "";
            DarkZenithSidekickFactionBaseInfo dInfo = null;
            dInfo = HackerFaction.TryGetExternalBaseInfoAs<DarkZenithSidekickFactionBaseInfo>();
            if ( dInfo == null )
                return Hackable.NeverCanBeHacked_Hide;
            if ( planet == null )
            {
                RejectionReasonDescription = "This planet is null";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( planet.IsFimbulwintered )
            {
                RejectionReasonDescription = "This planet is already Fimbulwintered";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( planet.GetControllingOrInfluencingFaction().Type == FactionType.AI )
            {
                RejectionReasonDescription = "A player must own this planet";
                return Hackable.NeverCanBeHacked_Hide;
            }
            bool foundHjarn = false;
            foreach ( GameEntity_Squad hjarn in dInfo.Hjarnum.DisplaySquads() )
            {
                if ( hjarn.Planet == planet )
                {
                    foundHjarn = true;
                    break;
                }
            }
            if ( foundHjarn )
            {
                RejectionReasonDescription = "This planet has a Hjarn";
                return Hackable.NeverCanBeHacked_Hide;
            }

            if ( planet.GetControllingOrInfluencingFaction().Type != FactionType.Player )
            {
                RejectionReasonDescription = "A player must own this planet";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {

            GameEntityTypeData typedata = GameEntityTypeDataTable.Instance.GetRowByName( "DZHjarn" );
            if ( typedata == null )
            throw new Exception("Could not find DZHjarn");

            ArcenPoint destinationPoint = planet.GetSafePlacementPoint_AroundEntity( Context, typedata, Hacker, FInt.FromParts( 0, 025 ), FInt.FromParts( 0, 500 ) );
            PlanetFaction pFaction = Hacker.PlanetFaction;

            GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typedata, 1,
                pFaction.Faction.LooseFleet, 0, destinationPoint, Context, "DarkZenith-FimbulwinterHack" );


            return true;
        }


    }
    public class Hacking_ClaimFlagship : BaseHackingImplementation
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
                RejectionReasonDescription = "You have already claimed this structure";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( Target.PlanetFaction.Faction.GetIsHostileTowards( HackerFaction ))
            {
                RejectionReasonDescription = "Structure is owned by an enemy ";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( Target.Planet.GetControllingFaction().GetIsHostileTowards( HackerFaction ))
            {
                RejectionReasonDescription = "Planet is owned by a hostile faction ";
                return Hackable.NeverBeHacked_ButStillShow;
            }


            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            Target.PlanetFaction.SwitchToFaction( Target, Hacker.PlanetFaction, false, "Flagship Was Hacked" );
            if ( Target.TypeData.GetHasTag("AntiScourgeLeader"))
            {

                GameEntityTypeData bane = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "ScourgeBaneFlagship" );
                GameEntity_Squad newFlagship = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( Hacker.PlanetFaction, bane, 1,
                    null, 0, Target.WorldLocation, Context, "ScourgeBane-SpawnFlagship" );
                Target.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
                ScourgePerUnitBaseInfo data = newFlagship.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
                return true;
            }
            if ( Target.TypeData.GetHasTag("ScourgeFlagship"))
            {
                ScourgePerUnitBaseInfo data = Target.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
            }
            Target.HasNotYetBeenFullyClaimed = false;
            Target.HullPointsLost = 0;
            return true;
        }
    }
    public class Hacking_ClaimSocketIncreaser : BaseHackingImplementation
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
                RejectionReasonDescription = "You have already claimed this structure";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( Target.Planet.GetControllingFaction().GetIsHostileTowards( HackerFaction ))
            {
                RejectionReasonDescription = "Planet is owned by a hostile faction ";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            if ( GetCityCenter(planet, HackerFaction) == null )
            {
                RejectionReasonDescription = "You must have build a suitable structure to enhance here ";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            //We need to figure out the CityCenter (if any) for the hacking faction
            GameEntity_Squad city = GetCityCenter( planet, Hacker.PlanetFaction.Faction);
            GameEntity_Squad socketIncreaser = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( city.PlanetFaction, Target.TypeData, 1,
                city.FleetMembership.Fleet, 0, Target.WorldLocation, Context, "Necromancer-HackedAmplifier" );
            Target.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );

            return true;
        }
        private GameEntity_Squad GetCityCenter(Planet planet, Faction faction)
        {
            GameEntity_Squad city = null;
            foreach ( GameEntity_Squad entity in faction.Squads( EntityRollupType.CityCenter ) )
            {
                if ( entity.Planet == planet )
                {
                    city = entity;
                    break;
                }
            }
            return city;
        }
    }
    public class Hacking_ClaimMoon : BaseHackingImplementation
    {
        //Note: check if this also works as the non-dyson-sidekick version, if so merge them
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            //return "\nIn particular, this hack will upgrade <color=#a1ffa1>" + target.FleetMembership.Fleet.GetName() + "</color> to mark level " + (target.CurrentMarkLevel + 1) + ".";
            return "";
        }
        public override bool GetDoesHackRequireASinglularChoiceFromASubmenu()
        {
            return true;
        }

        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( Target.PlanetFaction.Faction == HackerFaction )
            {
                RejectionReasonDescription = "You have already claimed this structure";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( HackerFaction == null )
                HackerFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( Target.PlanetFaction.Faction.GetIsHostileTowards(HackerFaction) )
            {
                RejectionReasonDescription = "This moon is still hostile to you (" + Target.ToStringWithPlanetAndOwner() +"), you must defeat it first";
                return Hackable.NeverCanBeHacked_Hide;
            }

            if ( Target.Planet.GetControllingFaction().Type == FactionType.AI )
            {
                RejectionReasonDescription = "Planet is owned by a hostile faction ";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        private ArcenCachedExternalTypeDirect type_bCustomHackingOptionButton = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bCustomHackingOptionButton ) );
        private static readonly List<GameEntityTypeData> listOfItemsLastUsedToPopulateCustomButton = List<GameEntityTypeData>.Create_WillNeverBeGCed( 60, "Hacking_ClaimMoon-listOfItemsLastUsedToPopulateCustomButton" );
        private static Hacking_ClaimMoon Implementation = null;
        private static HackCustomButtonListInfo Info = new HackCustomButtonListInfo();
        public override void AddAllButtonsForSingularChoiceInSubMenu( GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, Faction HackerFaction,
                                                                      HackingType HackType, ArcenUI_SetOfCreateElementDirectives Set,
                                                                      ref float runningY, UnityEngine.Rect firstBounds, float rowBuffer, AddHackButtonToWindow WindowAdder )
        {
            Implementation = this;
            Info.Update( TargetIfShip, TargetIfPlanet, HackerFaction, HackType );
            listOfItemsLastUsedToPopulateCustomButton.Clear();
            int count = 0;
            if ( DysonSidekickFactionBaseInfo.GetIsThisADysonFaction( HackerFaction ) )
            {
                GameEntityTypeData moon = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "DysonZenithMoon" );
                listOfItemsLastUsedToPopulateCustomButton.Add( moon );
                HackingUtils.PopulateOneCustomHackingOptionButton( Set, ref runningY, ref firstBounds, rowBuffer, WindowAdder,
                    //this part differs for each hack type, telling it how to tell each option apart
                    string.Empty, count, count,
                    //and this part is also different for each hack type, pointing to its own unique button implementation.
                    //they can all be called bCustomHackingOptionButton, since they are a subclass of the hacking implementation class.
                    type_bCustomHackingOptionButton );
                count++;
                moon = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "DysonTemplarMoon" );
                listOfItemsLastUsedToPopulateCustomButton.Add( moon );
                HackingUtils.PopulateOneCustomHackingOptionButton( Set, ref runningY, ref firstBounds, rowBuffer, WindowAdder,
                    //this part differs for each hack type, telling it how to tell each option apart
                    string.Empty, count, count,
                    //and this part is also different for each hack type, pointing to its own unique button implementation.
                    //they can all be called bCustomHackingOptionButton, since they are a subclass of the hacking implementation class.
                    type_bCustomHackingOptionButton );
                count++;
                moon = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "DysonNeinzulMoon" );
                listOfItemsLastUsedToPopulateCustomButton.Add( moon );
                HackingUtils.PopulateOneCustomHackingOptionButton( Set, ref runningY, ref firstBounds, rowBuffer, WindowAdder,
                    //this part differs for each hack type, telling it how to tell each option apart
                    string.Empty, count, count,
                    //and this part is also different for each hack type, pointing to its own unique button implementation.
                    //they can all be called bCustomHackingOptionButton, since they are a subclass of the hacking implementation class.
                    type_bCustomHackingOptionButton );
                count++;
                moon = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "DysonSpireMoon" );
                HackingUtils.PopulateOneCustomHackingOptionButton( Set, ref runningY, ref firstBounds, rowBuffer, WindowAdder,
                    //this part differs for each hack type, telling it how to tell each option apart
                    string.Empty, count, count,
                    //and this part is also different for each hack type, pointing to its own unique button implementation.
                    //they can all be called bCustomHackingOptionButton, since they are a subclass of the hacking implementation class.
                    type_bCustomHackingOptionButton );
                listOfItemsLastUsedToPopulateCustomButton.Add( moon );
                count++;
            }
            else if ( DarkZenithSidekickFactionBaseInfo.GetIsThisADZFaction( HackerFaction ) )
            {
                GameEntityTypeData moon = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "DarkZenithMoon" );
                listOfItemsLastUsedToPopulateCustomButton.Add( moon );
                HackingUtils.PopulateOneCustomHackingOptionButton( Set, ref runningY, ref firstBounds, rowBuffer, WindowAdder,
                    //this part differs for each hack type, telling it how to tell each option apart
                    string.Empty, count, count,
                    //and this part is also different for each hack type, pointing to its own unique button implementation.
                    //they can all be called bCustomHackingOptionButton, since they are a subclass of the hacking implementation class.
                    type_bCustomHackingOptionButton );
                count++;
            }
            else
            {
                //Spire sidekick
                GameEntityTypeData moon = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound( "SpireSidekickMoon" );
                listOfItemsLastUsedToPopulateCustomButton.Add( moon );
                HackingUtils.PopulateOneCustomHackingOptionButton( Set, ref runningY, ref firstBounds, rowBuffer, WindowAdder,
                                                                   //this part differs for each hack type, telling it how to tell each option apart
                                                                   string.Empty, count, count,
                                                                   //and this part is also different for each hack type, pointing to its own unique button implementation.
                                                                   //they can all be called bCustomHackingOptionButton, since they are a subclass of the hacking implementation class.
                    type_bCustomHackingOptionButton );
                count++;
            }
        }
        #region bCustomHackingOptionButton
        public class bCustomHackingOptionButton : ButtonAbstractBase
        {
           #region GetTechUpgradeFromElement
           public static GameEntityTypeData GetMoonFromElement( ArcenUI_Element element )
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
                int debugCode = 0;
                try{
                    debugCode = 100;
                    GameEntityTypeData moonData = GetMoonFromElement( this.Element );
                    if ( moonData == null )
                    {
                        Buffer.Add( "<color=#c74639>Null moon</color>" );
                        return;
                    }
                    debugCode = 200;
                    debugCode = 300;
                    Buffer.Add("<size=80%>");
                    Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    Buffer.AddShipIconInline(moonData, localFaction );
                    Buffer.Add( moonData.GetDisplayName() );
                    Buffer.Add("</size>");
                }
                catch( Exception e)
                {
                    ArcenDebugging.LogSingleLine("Hit exception in GetTextToShowFromVolatile " + e.ToString() + " " + debugCode, Verbosity.DoNotShow );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                GameEntityTypeData moonData = GetMoonFromElement( this.Element );
                if ( moonData == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                if ( !this.GetCanHackForThisItem( moonData ) )
                  return MouseHandlingResult.PlayClickDeniedSound;

                DoHack( input, moonData );

                return MouseHandlingResult.None;
            }

            public void DoHack( MouseHandlingInput input, GameEntityTypeData moonData )
            {
                if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                    return;
                //Send a GameCommand

                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.ClaimMoon], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedEntityIDs.Add( Info.TargetShip.PrimaryKeyID );
                command.RelatedString = moonData.InternalName;
                command.RelatedFactionIndex = World_AIW2.Instance.GetPlayerFactionForUIOrNull().FactionIndex;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                Window_HackChoicesSidebarPopout.Instance.Close();
            }

            private string lastRejectionReason = string.Empty;
            public bool GetCanHackForThisItem( GameEntityTypeData item )
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
                    //Note: this commented out logic assumed Info.TargetShip is the hacker, but it is actually the moon we are claiming
                    // GameEntity_Squad hacker = Info.TargetShip; //this is handled above, in terms of true cases
                    // if ( hacker == null )
                    // {
                    //     lastRejectionReason = "No hacker found";
                    //     return false;
                    // }
                    // debugCode = 115;
                    // PlanetFaction pFaction = hacker.PlanetFaction;
                    // if ( pFaction == null )
                    // {
                    //     lastRejectionReason = "No pfaction found";
                    //     return false;
                    // }
                    // debugCode = 120;
                    // Faction faction = pFaction.Faction;
                    // if ( faction == null ||
                    //      faction.Type != FactionType.Player) //I saw some weird errors where the ExternalBaseInfo was from the Neutral Faction
                    // {
                    //     if ( faction == null )
                    //     lastRejectionReason = "No faction found";
                    //     else
                    //     lastRejectionReason = "not player faction found: " + faction.ToString();
                    //     return false;
                    // }
                    debugCode = 130;
                    if ( item == null )
                    {
                        throw new Exception("How is item null?");
                    }
                    debugCode = 250;
                    // DarkZenithSidekickFactionBaseInfo dInfo = faction.GetExternalBaseInfoAs<DarkZenithSidekickFactionBaseInfo>();
                    // if ( dInfo == null )
                    // {
                    //     lastRejectionReason = "No Faction BaseInfo";
                    //     return false;
                    // }

                    // if ( !dInfo.IsBuildPossible( item.Cost ) )
                    // {
                    //     lastRejectionReason = "We don't have access to a required resource";
                    //     return false;
                    // }
                    // if ( !dInfo.ShouldShowEpistyleChoice( item ) )
                    // {
                    //     //Note: this shouldn't actually be possible since those shouldn't be shown,
                    //     //but in case we remove that restriction this will be helpful
                    //     lastRejectionReason = "We are missing pre-reqs for this upgrade (or it has already been unlocked)";
                    //     return false;
                    // }

                    debugCode = 500;
                    return Implementation.GetCanBeHacked( Info.TargetShip, null,
                                                          Info.TargetPlanet, null, Info.HackingType,
                                                          this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString, -1, out lastRejectionReason ) == Hackable.CanBeHacked;
                } catch(Exception e)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit excpetion in choose epistyle GetCanHackForThisItem debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
                }

                return false;
            }

            private static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Hacking_ClaimMoon-bCustomHackingOptionButton-tooltipBuffer" );
            public override void HandleMouseover()
            {
                int debugCode = 0;
                GameEntityTypeData moonData = GetMoonFromElement( this.Element );
                try{
                    debugCode = 100;
                    if ( moonData == null )
                    {
                        Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "Null Moon, this is a bug." );
                        return;
                    }
                    else
                    {
                        if (!this.GetCanHackForThisItem(moonData))
                        { 
                            tooltipBuffer.Add( "\n\n<color=#c74639>无法选择该选项： " + this.lastRejectionReason + "。</color>\n" );
                        }
                        else
                        { 
                            debugCode = 200;
                            Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                            debugCode = 300;
                            tooltipBuffer.Add( "<b><u>黑客： " ).Add( Info.HackingType.DisplayName ).Add( "</u></b>\n" );
                            tooltipBuffer.Add("这将占领此卫星并将其转变为 ").Add(moonData.GetDisplayName(), "a1ffa1").Add("。\n");

                            debugCode = 400;
                            if ( !this.GetCanHackForThisItem( moonData ) )
                            {
                                tooltipBuffer.Add( "<color=#c74639>不可行：</color> " );
                            }
                            debugCode = 500;
                            debugCode = 600;
                            if ( moonData != null && moonData.TexEmbedSprite_Icon != null )
                            {
                                debugCode = 700;
                                //Write unit tooltip if appropriate
                                byte markLevel = localFaction.GetGlobalMarkLevelForShipLine( moonData );
                                tooltipBuffer.Add("\n");
                                EntityText.GetTooltip( tooltipBuffer, null, null, moonData, 1,
                                    localFaction, Info.TargetShip.CurrentMarkLevel, FromSidebarType.Sidebar_MultipleUnits,
                                    ShipExtraDetailFlags.BuildInfo | ShipExtraDetailFlags.AnyGrantHackInfo | ShipExtraDetailFlags.WindowHackChoicesSidebarPopoutForData, 1f, false );

                                EntityText.Write_Tooltip_Hotkeys_Footer( tooltipBuffer, false, true, null );
                            }
                        }
                        Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
                    }
                } catch( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in mouseover for a transformation: " + moonData.ToString() + ". debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
                }
            }
        }
        #endregion
    }
    public class Hacking_HackForDZResource : BaseHackingImplementation
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            //return "\nIn particular, this hack will upgrade <color=#a1ffa1>" + target.FleetMembership.Fleet.GetName() + "</color> to mark level " + (target.CurrentMarkLevel + 1) + ".";
            return "";
        }
        // public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        // {
        //     if ( Target.PlanetFaction.Faction == HackerFaction )
        //     {
        //         RejectionReasonDescription = "You own this structure";
        //         return Hackable.NeverCanBeHacked_Hide;
        //     }
        //     return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        // }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            //We need to figure out the CityCenter (if any) for the hacking faction
            DarkZenithPerUnitBaseInfo data = Hacker.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
            if ( Target.TypeData.GetHasTag("OctironSpawner"))
            {
                data.Inventory[DZResource.Metal] += 3000;
            }
            else if( Target.TypeData.GetHasTag("ThaumiteSpawner"))
            {
                data.Inventory[DZResource.Green] += 200;
            }
            else if( Target.TypeData.GetHasTag("CheloniumSpawner"))
            {
                data.Inventory[DZResource.Blue] += 200;
            }
            else if( Target.TypeData.GetHasTag("AlkahestSpawner"))
            {
                data.Inventory[DZResource.White] += 200;
            }
            else if( Target.TypeData.GetHasTag("IzumiteSpawner"))
            {
                data.Inventory[DZResource.Red] += 150;
            }
            else if( Target.TypeData.GetHasTag("SkrithSpawner"))
            {
                data.Inventory[DZResource.Black] += 100;
            }
            else
            {
                throw new Exception("Unknown nadir spawner target");
            }

            Target.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
            return true;
        }
    }
    public class Hacking_DepositResources : BaseHackingImplementation
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            //return "\nIn particular, this hack will upgrade <color=#a1ffa1>" + target.FleetMembership.Fleet.GetName() + "</color> to mark level " + (target.CurrentMarkLevel + 1) + ".";
            return "";
        }
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( HackerOrNull != null )
            {
                DarkZenithPerUnitBaseInfo hackerData = HackerOrNull.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( !hackerData.HasAnyResourcesAtAll())
                {
                    RejectionReasonDescription = "Has no resources to transfer";
                    //return Hackable.NeverBeHacked_ButStillShow;
                    return Hackable.NeverCanBeHacked_Hide;
                }
                DarkZenithPerUnitBaseInfo targetData = Target.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( !targetData.WantsResourcesFrom( hackerData ))
                {
                    RejectionReasonDescription = "This epistyle doesn't need any of your resources";
                    return Hackable.NeverCanBeHacked_Hide;
                }
            }
            if ( Target.PlanetFaction.Faction != HackerFaction )
            {
                RejectionReasonDescription = "You must own this structure";
                return Hackable.NeverCanBeHacked_Hide;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            //Transfer the requested resources
            DarkZenithPerUnitBaseInfo hackerData = Hacker.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
            DarkZenithPerUnitBaseInfo targetData = Target.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
            string log = "";
            targetData.TransferRequestedInventoryFrom( ref hackerData, ref log );

            return true;
        }
    }
    public class Hacking_ReclaimArchitraveTerritory : BaseHackingImplementation
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            return "\nIn particular, this hack push the ZA back from <color=#a1ffa1>" + planet.Name + "</color>";
        }
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( !planet.IsZenithArchitraveTerritory)
            {
                RejectionReasonDescription = "Can only be used on ZA Territory";
                //return Hackable.NeverBeHacked_ButStillShow;
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( planet.IsZenithArchitraveHome)
            {
                RejectionReasonDescription = "Can't push the ZA out of their Homeworld";
                //return Hackable.NeverBeHacked_ButStillShow;
                return Hackable.NeverCanBeHacked_Hide;
            }
            int enemyStrength = planet.GetPlanetFactionForFaction( HackerFaction ).DataByStance[FactionStance.Hostile].TotalStrength;
            if ( enemyStrength > 10 * 1000 )
            {
                RejectionReasonDescription = "There are too many enemies here";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            //Remove the planet from the ZA's territory
            planet.IsZenithArchitraveTerritory = false;
            planet.AdditionalDescriptionTextFromFactions = "";
            foreach ( Faction faction in World_AIW2.Instance.Factions )
            {
                if ( faction.SpecialFactionData.InternalName != "ZenithArchitrave")
                {
                    continue;
                }
                ZenithArchitraveFactionBaseInfo info = faction.GetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                for ( int i = info.Territory.Count - 1; i >= 0; i-- )
                {
                    if ( info.Territory[i] == planet )
                    {
                        info.Territory.RemoveAt( i );
                    }
                }
            }
            return true;
        }
    }
    public class Hacking_TransformArmory : BaseHackingImplementation
    {
        //Allows the Dark Zenith Sidekick player to tell an Epistyle to build something in particular
        public override Hackable GetCanBeHacked(GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription)
        {
            RejectionReasonDescription = "";
            if ( Target.PlanetFaction.Faction.GetIsHostileTowards(HackerFaction))
            {
                RejectionReasonDescription = "Can only hack a vassal";
                //return Hackable.NeverBeHacked_ButStillShow;
                return Hackable.NeverCanBeHacked_Hide;
            }

            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool GetDoesHackRequireASinglularChoiceFromASubmenu()
        {
            return true;
        }

        private ArcenCachedExternalTypeDirect type_bCustomHackingOptionButton = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bCustomHackingOptionButton ) );
        private static readonly List<ScourgeTypeData> listOfItemsLastUsedToPopulateCustomButton = List<ScourgeTypeData>.Create_WillNeverBeGCed( 60, "Hacking_TransformArmory-listOfItemsLastUsedToPopulateCustomButton" );
        private static Hacking_TransformArmory Implementation = null;
        private static HackCustomButtonListInfo Info = new HackCustomButtonListInfo();
        public override void AddAllButtonsForSingularChoiceInSubMenu( GameEntity_Squad TargetIfShip, Planet TargetIfPlanet, Faction HackerFaction,
                                                                      HackingType HackType, ArcenUI_SetOfCreateElementDirectives Set,
                                                                      ref float runningY, UnityEngine.Rect firstBounds, float rowBuffer, AddHackButtonToWindow WindowAdder )
        {
            Implementation = this;
            Info.Update( TargetIfShip, TargetIfPlanet, HackerFaction, HackType );
            listOfItemsLastUsedToPopulateCustomButton.Clear();
            ScourgeInfusedHumanEmpireFactionBaseInfo baseInfo = HackerFaction.GetExternalBaseInfoAs<ScourgeInfusedHumanEmpireFactionBaseInfo>();
            List<ScourgeTypeData> list = baseInfo.UnlockedRaces.GetDisplayList();
            for ( int count = 0; count < list.Count; count++ )
            {
                ScourgeTypeData typeData = list[count];

                listOfItemsLastUsedToPopulateCustomButton.Add( typeData );
                HackingUtils.PopulateOneCustomHackingOptionButton( Set, ref runningY, ref firstBounds, rowBuffer, WindowAdder,
                                                                   //this part differs for each hack type, telling it how to tell each option apart
                                                                   string.Empty, count, count,
                                                                   //and this part is also different for each hack type, pointing to its own unique button implementation.
                                                                   //they can all be called bCustomHackingOptionButton, since they are a subclass of the hacking implementation class.
                                                                   type_bCustomHackingOptionButton );

            }
        }
        #region bCustomHackingOptionButton
        public class bCustomHackingOptionButton : ButtonAbstractBase
        {
           #region GetTechUpgradeFromElement
           public static ScourgeTypeData GetTypeDataFromElement( ArcenUI_Element element )
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
                int debugCode = 0;
                try{
                    debugCode = 100;
                    ScourgeTypeData race = GetTypeDataFromElement( this.Element );
                    if ( race == null )
                    {
                        Buffer.Add( "<color=#c74639>No race</color>" );
                        return;
                    }
                    debugCode = 200;
                    debugCode = 300;
//                    Buffer.Add("<size=80%>");
                    Buffer.Add(race.GetDisplayName(), race.color);
//                    Buffer.Add("</size>");
                }
                catch( Exception e)
                {
                    ArcenDebugging.LogSingleLine("Hit exception in GetTextToShowFromVolatile " + e.ToString() + " " + debugCode, Verbosity.DoNotShow );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                ScourgeTypeData typeData = GetTypeDataFromElement( this.Element );
                if ( typeData == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                if ( !this.GetCanHackForThisItem( typeData ) )
                  return MouseHandlingResult.PlayClickDeniedSound;

                DoHack( input, typeData );

                return MouseHandlingResult.None;
            }

            public void DoHack( MouseHandlingInput input, ScourgeTypeData typeData )
            {
                if ( Engine_Universal.CurrentPopups.Count > 0 ) //we got some sort of warning telling us we can't do this
                    return;
                //Send a GameCommand
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransformScourgeStructure], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                command.RelatedEntityIDs.Add( Info.TargetShip.PrimaryKeyID );
                //command.RelatedEntityIDs.Add( flagship.PrimaryKeyID );
                // bool resetValue = InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1();
                // command.RelatedBools.Add( resetValue );
                command.RelatedString2 = typeData.InternalName;
                World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                Window_HackChoicesSidebarPopout.Instance.Close();
            }

            private string lastRejectionReason = string.Empty;
            public bool GetCanHackForThisItem( ScourgeTypeData item )
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

                    debugCode = 130;
                    if ( item == null )
                    {
                        throw new Exception("How is item null?");
                    }
                    debugCode = 250;
                    // DarkZenithSidekickFactionBaseInfo dInfo = faction.GetExternalBaseInfoAs<DarkZenithSidekickFactionBaseInfo>();
                    // if ( dInfo == null )
                    // {
                    //     lastRejectionReason = "No Faction BaseInfo";
                    //     return false;
                    // }

                    debugCode = 500;
                    return Implementation.GetCanBeHacked( Info.TargetShip, hacker,
                                                          Info.TargetPlanet, hacker.GetFactionOrNull_Safe(), Info.HackingType,
                                                          this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTagString, -1, out lastRejectionReason ) == Hackable.CanBeHacked;
                } catch(Exception e)
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit excpetion in transform armory GetCanHackForThisItem debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
                }

                return false;
            }

            private static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Hacking_ChooseEpistyleProduction-bCustomHackingOptionButton-tooltipBuffer" );
            public override void HandleMouseover()
            {
                int debugCode = 0;
                ScourgeTypeData typeData = GetTypeDataFromElement( this.Element );
                try{
                    debugCode = 100;

                    if ( typeData == null )
                    {
                        Window_AtMouseTooltipPanelNarrow.bPanel.Instance.SetText( this.Element, "Null typeData, this is a bug." );
                        return;
                    }
                    else
                    {
                        if (!this.GetCanHackForThisItem(typeData))
                        { 
                            tooltipBuffer.Add( "\n\n<color=#c74639>Cannot choose this option: " + this.lastRejectionReason + ".</color>\n" );
                        }
                        else
                        { 
                            debugCode = 200;
                            Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                            debugCode = 300;
                            tooltipBuffer.Add( "<b><u>黑客： " ).Add( Info.HackingType.DisplayName ).Add( "</u></b>\n" );
                            tooltipBuffer.Add("将此军械库转变为 " + typeData.InternalName + " 种族。");
                            tooltipBuffer.Add("\n\n").Add( typeData.description);
                        }
                    }
                    Window_AtMouseTooltipPanelBesideSidebar.bPanel.Instance.SetText( tooltipBuffer.GetStringAndResetForNextUpdate(), "ShipTooltipScale" );
                } catch( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in mouseover for a scourge armory. debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
                }
            }
        }
        #endregion

    }
    public class Hacking_MarkupScourgeStructure : BaseHackingImplementation
    {
        //Allows the Dark Zenith Sidekick player to tell an Epistyle to build something in particular
        public override Hackable GetCanBeHacked(GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription)
        {
            RejectionReasonDescription = "";
            if ( Target.CurrentMarkLevel >= 7 )
            {
                RejectionReasonDescription = "Already mark 7";
                //return Hackable.NeverBeHacked_ButStillShow;
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( Target.PlanetFaction.Faction.GetIsHostileTowards(HackerFaction))
            {
                RejectionReasonDescription = "Can only hack a vassal";
                //return Hackable.NeverBeHacked_ButStillShow;
                return Hackable.NeverCanBeHacked_Hide;
            }
            ScourgePerUnitBaseInfo data = Target.CreateExternalBaseInfo<ScourgePerUnitBaseInfo>( "ScourgePerUnitBaseInfo" );
            if ( data.Experience < data.ExperienceForNextLevel )
            {
                RejectionReasonDescription = "Needs " + (data.ExperienceForNextLevel - data.Experience) + " more experience to level up.";
                return Hackable.NeverBeHacked_ButStillShow;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            //Markup this scourge structure
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransformScourgeStructure], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            command.RelatedEntityIDs.Add( Target.PrimaryKeyID );
            command.RelatedBools.Add(true);
            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
            return true;
        }
    }
    public class Hacking_ClaimStarbase : BaseHackingImplementation
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
                RejectionReasonDescription = "You have already claimed this structure";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( Target.PlanetFaction.Faction.GetIsHostileTowards( HackerFaction ))
            {
                RejectionReasonDescription = "Structure is owned by an enemy ";
                return Hackable.NeverCanBeHacked_Hide;
            }


            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            //CALL SpawnArmadaStarbase
            Faction hackFaction = Hacker.PlanetFaction.Faction;
            GameEntityTypeData typeData = Target.TypeData;
            ArcenPoint spawnLocation = Target.WorldLocation;
            PlanetFaction pFaction = Hacker.PlanetFaction;
            Target.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
            
            GameEntity_Squad starbase = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typeData, 1,
                pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Armada-ClaimStarbaseViaHacking" );


            Fleet newArmadaStarbaseFleet = starbase.FleetMembership.Fleet;


            newArmadaStarbaseFleet.NameRaw = "Starbase " + planet.Name;
            newArmadaStarbaseFleet.FleetQualifier = "Armada";

            ArmadaCityFleetBaseInfo armadaCityFleetInfo = newArmadaStarbaseFleet.CreateExternalBaseInfo<ArmadaCityFleetBaseInfo>("ArmadaCityFleetBaseInfo");

            armadaCityFleetInfo.CanChangeBolsteredFleet = true;
            return true;
        }
    }
    public class Hacking_DismantleStarbase : BaseHackingImplementation
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
                RejectionReasonDescription = "You have already claimed this structure";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( Target.PlanetFaction.Faction.GetIsHostileTowards( HackerFaction ))
            {
                RejectionReasonDescription = "Structure is owned by an enemy ";
                return Hackable.NeverCanBeHacked_Hide;
            }

            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            Target.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
            return true;
        }
    }
    public class Hacking_SummonTiberiumEnemy : BaseHackingImplementation
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
                RejectionReasonDescription = "You have already claimed this structure";
                return Hackable.NeverCanBeHacked_Hide;
            }
            // if ( Target.PlanetFaction.Faction.GetIsHostileTowards( HackerFaction ))
            // {
            //     RejectionReasonDescription = "Structure is owned by an enemy ";
            //     return Hackable.NeverCanBeHacked_Hide;
            // }
            // if ( Target.Planet.GetControllingFaction().GetIsHostileTowards( HackerFaction ))
            // {
            //     RejectionReasonDescription = "Planet is owned by a hostile faction ";
            //     return Hackable.NeverBeHacked_ButStillShow;
            // }


            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            //Buff the Vein
            TiberiumPerUnitBaseInfo data = Target.TryGetExternalBaseInfoAs<TiberiumPerUnitBaseInfo>();
            if ( data == null )
                return false;
            data.Points += 2000;

            GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "TiberiumAbomination" );
            ArcenPoint spawnLocation = Target.Planet.GetSafePlacementPoint_AroundEntity( Context, typeData, Target, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 150 ) );

            Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );
            if ( aiFaction == null )
                return false;
            AISentinelsFactionBaseInfo aiBaseInfo = aiFaction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
            if ( aiBaseInfo == null )
                return false;
            PlanetFaction pFaction = Target.Planet.GetPlanetFactionForFaction( aiFaction );
            GameEntity_Squad abomination = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typeData, aiFaction.CurrentGeneralMarkLevel,
                pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Armada-TiberiumAbomination" );
            if ( abomination == null )
                return false;
            abomination.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
            return true;
        }
    }
    public class Hacking_SummonScienceEnemy : BaseHackingImplementation
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
                RejectionReasonDescription = "You have already claimed this structure";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( Target.CurrentMarkLevel < 3 )
            {
                RejectionReasonDescription = "Vein is too low mark level to withstand your hacking energy; it must be at 3 or higher ";
                return Hackable.NeverBeHacked_ButStillShow;
            }

            // if ( Target.PlanetFaction.Faction.GetIsHostileTowards( HackerFaction ))
            // {
            //     RejectionReasonDescription = "Structure is owned by an enemy ";
            //     return Hackable.NeverCanBeHacked_Hide;
            // }
            // if ( Target.Planet.GetControllingFaction().GetIsHostileTowards( HackerFaction ))
            // {
            //     RejectionReasonDescription = "Planet is owned by a hostile faction ";
            //     return Hackable.NeverBeHacked_ButStillShow;
            // }


            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            //Buff the Vein
            TiberiumPerUnitBaseInfo data = Target.TryGetExternalBaseInfoAs<TiberiumPerUnitBaseInfo>();
            if ( data == null )
                return false;
            data.Points += 2000;

            //Spawn the Eschaton
            GameEntityTypeData typeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "TiberiumEschaton" );
            ArcenPoint spawnLocation = Target.Planet.GetSafePlacementPoint_AroundEntity( Context, typeData, Target, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 150 ) );

            Faction aiFaction = World_AIW2.GetRandomAIFaction( Context );
            if ( aiFaction == null )
                return false;
            AISentinelsFactionBaseInfo aiBaseInfo = aiFaction.GetExternalBaseInfoAs<AISentinelsFactionBaseInfo>();
            if ( aiBaseInfo == null )
                return false;
            PlanetFaction pFaction = Target.Planet.GetPlanetFactionForFaction( aiFaction );
            GameEntity_Squad eschaton = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typeData, aiFaction.CurrentGeneralMarkLevel,
                pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "Armada-TiberiumEscaton" );
            if ( eschaton == null )
                return false;
            eschaton.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
            return true;
        }
    }
    public class Hacking_EmpowerTiberiumVein : BaseHackingImplementation
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
                RejectionReasonDescription = "You have already claimed this structure";
                return Hackable.NeverCanBeHacked_Hide;
            }
            // if ( Target.PlanetFaction.Faction.GetIsHostileTowards( HackerFaction ))
            // {
            //     RejectionReasonDescription = "Structure is owned by an enemy ";
            //     return Hackable.NeverCanBeHacked_Hide;
            // }
            if ( Target.CurrentMarkLevel < 3 )
            {
                RejectionReasonDescription = "Vein is too low mark level to withstand your hacking energy ";
                return Hackable.NeverBeHacked_ButStillShow;
            }


            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            TiberiumPerUnitBaseInfo data = Target.TryGetExternalBaseInfoAs<TiberiumPerUnitBaseInfo>();
            if ( data == null )
                return false;
            data.PreferredUpgrade = TiberiumUpgrade.NewVein;
            data.Points += 2000;
            data.NextUpgrade = TiberiumUpgrade.None;
            ArmadaFactionBaseInfo baseInfo = Hacker.PlanetFaction.Faction.TryGetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
            baseInfo.VeinsEmpowered++;
            baseInfo.StrengthForNextEnemyAttack += baseInfo.Difficulty.BonusEnemyResponsePerVeinEmpower;
            baseInfo.StrengthForNextEnemyAttack += baseInfo.Difficulty.ScalingEnemyResponseForCumulativeVeinEmpowering * baseInfo.VeinsEmpowered;
            return true;
        }
    }
    public class Hacking_EnterMultiphase : BaseHackingImplementation
    {
        public override string GetDynamicDescription( GameEntity_Squad target, GameEntity_Squad hackerOrNull, Planet planet, Faction hackerFaction, HackingType hackingType )
        {
            //return "\nIn particular, this hack will upgrade <color=#a1ffa1>" + target.FleetMembership.Fleet.GetName() + "</color> to mark level " + (target.CurrentMarkLevel + 1) + ".";
            return "";
        }
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            // if ( Target.PlanetFaction.Faction == HackerFaction )
            // {
            //     RejectionReasonDescription = "You have already claimed this structure";
            //     return Hackable.NeverCanBeHacked_Hide;
            // }
            // if ( Target.PlanetFaction.Faction.GetIsHostileTowards( HackerFaction ))
            // {
            //     RejectionReasonDescription = "Structure is owned by an enemy ";
            //     return Hackable.NeverCanBeHacked_Hide;
            // }
            // if ( Target.CurrentMarkLevel < 3 )
            // {
            //     RejectionReasonDescription = "Vein is too low mark level to withstand your hacking energy ";
            //     return Hackable.NeverBeHacked_ButStillShow;
            // }


            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            int phaseTime = 60;
            Hacker.CurrentStateOfMatter = StateOfMatterTypeDataTable.Instance.GetRowByName("MultiPhase");
            Hacker.GameSecondWillExitStateOfMatter = World_AIW2.Instance.GameSecond + phaseTime;
            return true;
        }
    }
    public class Hacking_SearchForRelicSidekick : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            Faction fallenSpireFaction = FactionUtilityMethods.Instance.GetFallenSpireFaction();
            if ( fallenSpireFaction == null )
            {
                RejectionReasonDescription = "Fallen Spire isn't on path A";
                return Hackable.NeverCanBeHacked_Hide;
            }
            SpireSidekickFactionBaseInfo spireData = fallenSpireFaction.TryGetExternalBaseInfoAs<SpireSidekickFactionBaseInfo>();
            if ( spireData == null )
            {
                RejectionReasonDescription = "Fallen Spire isn't on path B";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( spireData.CurrentRelicSpawnPlanetIdx == -1 )
            {
                RejectionReasonDescription = "There is no Relic right now";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( spireData.CurrentRelicInSearchMode == false &&
                 spireData.CurrentRelicSpawnPlanetIdx != planet.Index )
            {
                RejectionReasonDescription = "This is not where the Relic is (and we aren't in search mode)";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( spireData.RelicOnMap )
            {
                RejectionReasonDescription = "The Relic is in the galaxy";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( spireData.PlanetsSearchedForCurrentRelic.Contains( planet.Index ) )
            {
                RejectionReasonDescription = "You have already searched here for the Relic";
                return Hackable.AlreadyHasBeenHacked_ButStillShow;
            }
            RejectionReasonDescription = string.Empty;
            return Hackable.CanBeHacked;
        }
        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            if ( planet == null )
            {
                if ( Target != null )
                    planet = Target.Planet;
            }
            if ( planet == null )
                throw new Exception( "No planet was hacked!  Could not complete hack." );

            Faction fallenSpireFaction = FactionUtilityMethods.Instance.GetFallenSpireFaction();
            if ( fallenSpireFaction == null )
                throw new Exception( "Spire isn't on. This is perplexing" );

            SpireSidekickFactionBaseInfo spireData = fallenSpireFaction.TryGetExternalBaseInfoAs<SpireSidekickFactionBaseInfo>();
            if ( spireData == null )
                throw new Exception( "Spire Global Data is null. This is perplexing" );

            if ( planet.Index == spireData.CurrentRelicSpawnPlanetIdx )
            {
                spireData.CurrentRelicSpawnPlanetIdx = -1;
                spireData.TimeForNextRelicSpawn = World_AIW2.Instance.GameSecond + SpireSidekickFactionBaseInfo.Instance.RelicSpawnInterval + Context.RandomToUse.Next( 0, SpireSidekickFactionBaseInfo.Instance.RelicSpawnIntervalRandomness );

                SpireSidekickFactionBaseInfo.Instance.CreateRelic( planet, fallenSpireFaction, Hacker.GetFactionOrNull_Safe(), Context, FInt.One, false, Engine_AIW2.Instance.CombatCenter, true );
            }
            else
            {
                PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.PlanetToView = planet;

                Planet relicPlanetOrNull = World_AIW2.Instance.GetPlanetByIndex( spireData.CurrentRelicSpawnPlanetIdx );
                World_AIW2.Instance.QueueChatMessageOrCommand( "You did not find the Relic on " + planet.Name + ". The Relic is " +
                    (relicPlanetOrNull == null ? "unknown" : relicPlanetOrNull.GetHopsTo( planet ).ToString()) + " hops away. Every planet searched this way increases the AI response when you find the Relic.",
                    ChatType.LogToCentralChat, string.Empty, chatHandlerOrNull );
                spireData.PlanetsSearchedForCurrentRelic.Add( planet.Index );
            }
            return true;
        }
    }
    public class Hacking_AnalyzeSpireSidekickDebris : BaseHackingImplementation
    {
        public override void DoOneSecondOfHackingLogic_HackSpecificLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            SpireSidekickPerUnitBaseInfo debrisData = Target.CreateExternalBaseInfo<SpireSidekickPerUnitBaseInfo>( "SpireSidekickPerUnitBaseInfo" );
            debrisData.TimeUntilDebrisVanishes += 1;
        }
    }

}
