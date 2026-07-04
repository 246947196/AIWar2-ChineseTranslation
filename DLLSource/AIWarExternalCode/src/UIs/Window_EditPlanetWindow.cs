using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_EditPlanetWindow : WindowControllerAbstractBase, IInputActionHandler
    {
        public static Window_EditPlanetWindow Instance;
        public Window_EditPlanetWindow()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
        }

        private bool IsOpen;
        private Planet forPlanet;
        private PlanetImportance startingImportance;
        public void Open( Planet ForPlanet )
        {
            if ( this.IsOpen && forPlanet == ForPlanet )
                return;
            this.IsOpen = true;
            this.forPlanet = ForPlanet;
            this.startingImportance = forPlanet.PlanetImportanceUIOnly;

            iPlanetName.Instance.SetText( this.forPlanet.Name );
            iPlanetNotes.Instance.SetText( this.forPlanet.PlayerNotes );
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( World_AIW2.Instance == null )
                return false;
            if ( World_AIW2.Instance.InSetupPhase )
                return false;
            if ( !this.IsOpen )
                return false;
            return true;
        }

        public bool GetIsOpen()
        {
            return this.IsOpen;
        }

        public void Close()
        {
            if ( !this.IsOpen )
                return;
            this.IsOpen = false;
            this.forPlanet = null;
        }

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_EditPlanetWindow.Instance != null )
                {
                    if ( !hasGlobalInitialized )
                    {
                        hasGlobalInitialized = true;
                        
                    }
                }
            }
        }

        public class bSave : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( iPlanetName.Instance.GetText().Length <= 0  )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, "星球必须有名称！",
                        "请在保存前为星球输入一个名称。其他字段为可选项。", "确定" );
                    return MouseHandlingResult.PlayClickDeniedSound;
                }
                try
                {

                    GameCommand command = GameCommand.Create( GameCommandTypeTable.CoreFunctions[CoreFunction.EditPlanet], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    try
                    {
                        command.RelatedMagnitude = (int)((dPriorityLevel.Instance.Element as ArcenUI_Dropdown).CurrentlySelectedOption as DropdownOptionPlanetImportance).Importance;
                    }
                    catch
                    {
                        command.RelatedMagnitude = (int)PlanetImportance.None;
                    }
                    command.RelatedIntegers.Add( Instance.forPlanet.Index );
                    command.RelatedString = iPlanetName.Instance.GetText();
                    command.RelatedString2 = iPlanetNotes.Instance.GetText();
                    World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );

                    #region VassalMission
                    int debugCode = 0;
                    try {
                    PlanetImportance newImportance = (PlanetImportance) command.RelatedMagnitude;
                    PlanetImportance oldImportance = Instance.forPlanet.PlanetImportanceUIOnly;
                    bool wasMission = false;
                    bool isMission = false;
                    debugCode = 100;
                    if ( oldImportance >= PlanetImportance.Fire_Low &&
                         oldImportance <= PlanetImportance.Fire_Override )
                        wasMission = true;

                    if ( newImportance >= PlanetImportance.Fire_Low &&
                         newImportance <= PlanetImportance.Fire_Override )
                        isMission = true;
                    debugCode = 200;
                    if ( wasMission )
                        command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.RemoveVassalMission], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    else
                        command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.AddVassalMission], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
                    //ArcenDebugging.ArcenDebugLogSingleLine("wasMission " + wasMission + " isMission " + isMission, Verbosity.DoNotShow );
                    if ( wasMission || isMission )
                    {
                        debugCode = 300;
                        debugCode = 310;
                        if ( command == null )
                            throw new Exception("could not create vassal mission command");
                        //priority
                        debugCode = 320;
                        if ( newImportance == PlanetImportance.Fire_Low )
                        {
                            command.RelatedMagnitude = (int)MissionPriority.Low;
                            command.RelatedString = "AttackPlanet";
                        }
                        if ( newImportance == PlanetImportance.Fire_Med )
                        {
                            command.RelatedMagnitude = (int)MissionPriority.Medium;
                            command.RelatedString = "AttackPlanet";
                        }
                        debugCode = 330;
                        if ( newImportance == PlanetImportance.Fire_High )
                        {
                            command.RelatedMagnitude = (int)MissionPriority.High;
                            command.RelatedString = "AttackPlanet";
                        }
                        if ( newImportance == PlanetImportance.Fire_Override )
                        {
                            command.RelatedMagnitude = (int)MissionPriority.Override;
                            command.RelatedString = "AttackPlanet";
                        }
                        debugCode = 400;
                        command.RelatedIntegers.Add( Instance.forPlanet.Index ); //planet
                        Faction localFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                        command.RelatedFactionIndex = localFaction.FactionIndex; //this is the faction that is getting the mission
                        command.RelatedBool = isMission; //this indicates whether we disable or enable the Mission
                        debugCode = 500;
                        bool isSuicide = false;
                        bool killCommandStation = false;
                        bool forDefense = false;
                        bool forOffense = true; //default
                        command.RelatedBools.Add( isSuicide );
                        command.RelatedBools.Add( killCommandStation );
                        command.RelatedBools.Add( forDefense );
                        command.RelatedBools.Add( forOffense );
                        debugCode = 600;
                        //Now we set the vassal factions to which we are applying this mission (either removing or adding)
                        {
                            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                            {
                                Faction fact = World_AIW2.Instance.Factions[i];
                                if ( fact.IsVassal )
                                {
                                    command.RelatedIntegers2.Add( fact.FactionIndex );
                                }
                            }
                        }
                        if ( command.RelatedIntegers2.Count > 0 )
                            World_AIW2.Instance.QueueGameCommand( World_AIW2.Instance.GetLocalPlayerFactionOrNaturalObjectsNeverNull(), command, true );
                        else
                            ArcenDebugging.ArcenDebugLogSingleLine("You attempted to give vassal orders, but there are no vassals in the game", Verbosity.DoNotShow );

                    }
                    } catch(Exception e)
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine("hit vassal mission exception " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
                    }
                    #endregion
                }
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "Edit planet save fail: " + e, Verbosity.ShowAsError );
                }

                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "保存" );
            }
        }

        public class bCancel : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {                
                Instance.Close();
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "取消" );
            }
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "编辑星球 " );
                if ( Instance.forPlanet != null )
                    Buffer.Add( Instance.forPlanet.Name );
            }
            public override void OnUpdate() { }
        }

        public class iPlanetName : InputAbstractBase
        {
            public int maxPlanetNameLen = 30;
            public static iPlanetName Instance;
            public iPlanetName() { Instance = this; }
            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if ( input.Length >= this.maxPlanetNameLen )
                    return '\0';
                if ( !ArcenStrings.GetIsSupportedChar( addedChar ) )
                    return '\0';
                return addedChar;
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.InternalName )
                {
                    case "OpenSystemMenu": //escape key
                    case "Return": //enter key
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }
        }

        public class iPlanetNotes : InputAbstractBase
        {
            public int maxPlanetNameLen = 512;
            public static iPlanetNotes Instance;
            public iPlanetNotes() { Instance = this; }
            public override char ValidateInput( string input, int charIndex, char addedChar )
            {
                if ( input.Length >= this.maxPlanetNameLen )
                    return '\0';
                if ( !ArcenStrings.GetIsSupportedChar( addedChar ) )
                    return '\0';
                return addedChar;
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.InternalName )
                {
                    case "OpenSystemMenu": //escape key
                    //case "Return": //enter key (don't do the enter key for this, because this is multi-line text!
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }
        }

        #region dPriorityLevel
        public class dPriorityLevel : DropdownAbstractBase
        {
            public static dPriorityLevel Instance;
            public dPriorityLevel()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenUI_Dropdown_Option Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                //set this locally for the current player only
                DropdownOptionPlanetImportance ItemAsType = (DropdownOptionPlanetImportance)Item;
                Window_EditPlanetWindow.Instance.startingImportance = ItemAsType.Importance;
            }

            public override void OnUpdate()
            {
                if ( Window_EditPlanetWindow.Instance == null )
                    return;

                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;
                if ( elementAsType == null )
                    return;

                PlanetImportance importanceToSelect = Window_EditPlanetWindow.Instance.startingImportance;

                bool foundMismatch = false;
                if ( importanceToSelect != PlanetImportance.Length && (elementAsType.CurrentlySelectedOption != null && ( PlanetImportance)elementAsType.CurrentlySelectedOption.GetItem() != importanceToSelect ) )
                {
                    foundMismatch = true;
                }
                else
                {
                    if ( elementAsType.GetItemCount() < 3 )
                        foundMismatch = true;
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( PlanetImportance importance = PlanetImportance.None; importance < PlanetImportance.Length; importance++ )
                    {
                        DropdownOptionPlanetImportance option = new DropdownOptionPlanetImportance( importance );
                        elementAsType.AddItem( option, importance == importanceToSelect );
                    }
                    Window_EditPlanetWindow.Instance.startingImportance = PlanetImportance.Length; //make sure not to reset it this time
                }
            }
            public override void HandleMouseover()
            {
                string mouseoverText = "选择在星球旁边显示的重要性（如有）。这些标签可以有您想要的任何含义；它们用于您自己的笔记或多人游戏通信。";
                Window_AtMouseTooltipPanelSnapToLeft.bPanel.Instance.SetText( this.Element, mouseoverText );
            }
        }

        public class DropdownOptionPlanetImportance : IArcenUI_Dropdown_Option
        {
            public PlanetImportance Importance;

            public DropdownOptionPlanetImportance( PlanetImportance Importance )
            {
                this.Importance = Importance;
            }

            public object GetItem()
            {
                return this.Importance;
            }

            public string GetOptionNameFromVolatile()
            {
                return "<color=#" + this.Importance.GetColor() + ">" + this.Importance;
            }

            public UnityEngine.Sprite GetOptionSprite()
            {
                return null;
            }
        }
        #endregion

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.InternalName )
            {
                case "OpenSystemMenu":
                    if ( Engine_Universal.CurrentPopups.Count <= 0 )
                    {
                        this.Close();
                        //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                        ArcenInput.BlockForAJustPartOfOneSecond();
                    }
                    break;
            }
        }
    }
}
