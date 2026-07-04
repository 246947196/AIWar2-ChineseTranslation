using System;

using System.Linq;
using System.Text;
using Arcen.Universal;
using Arcen.AIW2.Core;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class FullscreenResolutionDropdownFiller : IDropdownFiller
    {
        public int GetCurrentValue( ArcenSetting setting )
        {
            int searchForWidth = GameSettings.Current.GetInt( ArcenIntSetting_Universal.FullscreenWidth );
            int searchForHeight = GameSettings.Current.GetInt( ArcenIntSetting_Universal.FullscreenHeight );

            int match = -1;
            for ( int i = 0; i < ArcenUI.SupportedResolutions.Count; i++ )
            {
                ArcenPoint resolution = ArcenUI.SupportedResolutions[i];
                if ( resolution.X != searchForWidth )
                    continue;
                if ( resolution.Y != searchForHeight )
                    continue;
                match = i;
                break;
            }
            if ( match < 0 )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not find resolution matching " + searchForWidth + "x" + searchForHeight +
                    " out of " + ArcenUI.SupportedResolutions.Count + " options!", Verbosity.ShowAsError );
                return 0;
            }
            else
                return match;
        }

        public int GetTempValue( ArcenSetting setting )
        {
            return setting.TempValue_Int;
        }

        public void SetExtraValuesFromSpecialLogicIfWeShould( int Value )
        {
            if ( Value >= 0 && Value < ArcenUI.SupportedResolutions.Count )
            {
                ArcenPoint resolution = ArcenUI.SupportedResolutions[Value];
                GameSettings.Current.SetInt( ArcenIntSetting_Universal.FullscreenWidth, resolution.X );
                GameSettings.Current.SetInt( ArcenIntSetting_Universal.FullscreenHeight, resolution.Y );
            }
            else
                ArcenDebugging.ArcenDebugLogSingleLine( "Could not find resolution for TempValue_Int " + Value +
                    " out of " + ArcenUI.SupportedResolutions.Count + " options!", Verbosity.ShowAsError );
        }

        private readonly List<IArcenUI_Dropdown_Option> cachedOptions = List<IArcenUI_Dropdown_Option>.Create_WillNeverBeGCed( 20, "FullscreenResolutionDropdownFiller-cachedOptions" );
        public List<IArcenUI_Dropdown_Option> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 || cachedOptions.Count != ArcenUI.SupportedResolutions.Count )
            {
                cachedOptions.Clear();
                for ( int i = 0; i < ArcenUI.SupportedResolutions.Count; i++ )
                    cachedOptions.Add( new ResolutionOption( ArcenUI.SupportedResolutions[i] ) );
            }
            return cachedOptions;
        }
    }

    public class CameraMain_ZoomTypeFiller : IDropdownFiller
    {
        public int GetCurrentValue( ArcenSetting setting )
        {
            return GameSettings.Current.GetIntBySetting( setting );
        }

        public int GetTempValue( ArcenSetting setting )
        {
            return setting.TempValue_Int;
        }

        public void SetExtraValuesFromSpecialLogicIfWeShould( int Value )
        { }

        private readonly List<IArcenUI_Dropdown_Option> cachedOptions = List<IArcenUI_Dropdown_Option>.Create_WillNeverBeGCed( 20, "CameraMain_ZoomTypeFiller-cachedOptions" );
        public List<IArcenUI_Dropdown_Option> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 || cachedOptions.Count != ArcenCameraTypeTable.Instance.CameraTypesForMain.Count )
            {
                cachedOptions.Clear();
                for ( int i = 0; i < ArcenCameraTypeTable.Instance.CameraTypesForMain.Count; i++ )
                    cachedOptions.Add( new CameraTypeOption( ArcenCameraTypeTable.Instance.CameraTypesForMain[i] ) );
            }
            return cachedOptions;
        }
    }

    public class CameraGalaxy_ZoomTypeFiller : IDropdownFiller
    {
        public int GetCurrentValue( ArcenSetting setting )
        {
            return GameSettings.Current.GetIntBySetting( setting );
        }

        public int GetTempValue( ArcenSetting setting )
        {
            return setting.TempValue_Int;
        }

        public void SetExtraValuesFromSpecialLogicIfWeShould( int Value )
        { }

        private readonly List<IArcenUI_Dropdown_Option> cachedOptions = List<IArcenUI_Dropdown_Option>.Create_WillNeverBeGCed( 20, "CameraGalaxy_ZoomTypeFiller-cachedOptions" );
        public List<IArcenUI_Dropdown_Option> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 || cachedOptions.Count != ArcenCameraTypeTable.Instance.CameraTypesForGalaxyMap.Count )
            {
                cachedOptions.Clear();
                for ( int i = 0; i < ArcenCameraTypeTable.Instance.CameraTypesForGalaxyMap.Count; i++ )
                    cachedOptions.Add( new CameraTypeOption( ArcenCameraTypeTable.Instance.CameraTypesForGalaxyMap[i] ) );
            }
            return cachedOptions;
        }
    }

    public class FramerateTypeFiller : IDropdownFiller
    {
        public int GetCurrentValue( ArcenSetting setting )
        {
            return GameSettings.Current.GetIntBySetting( setting );
        }

        public int GetTempValue( ArcenSetting setting )
        {
            return setting.TempValue_Int;
        }

        public void SetExtraValuesFromSpecialLogicIfWeShould( int Value )
        { }

        private readonly List<IArcenUI_Dropdown_Option> cachedOptions = List<IArcenUI_Dropdown_Option>.Create_WillNeverBeGCed( 20, "FramerateTypeFiller-cachedOptions" );
        public List<IArcenUI_Dropdown_Option> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 || cachedOptions.Count != FramerateTypeTable.Instance.Rows.Count )
            {
                cachedOptions.Clear();
                for ( int i = 0; i < FramerateTypeTable.Instance.Rows.Count; i++ )
                    cachedOptions.Add( new FramerateTypeOption( FramerateTypeTable.Instance.Rows[i] ) );
            }
            return cachedOptions;
        }
    }

    public class DrawShipModelsCutoffFiller : IDropdownFiller
    {
        public int GetCurrentValue( ArcenSetting setting )
        {
            return GameSettings.Current.GetIntBySetting( setting );
        }

        public int GetTempValue( ArcenSetting setting )
        {
            return setting.TempValue_Int;
        }

        public void SetExtraValuesFromSpecialLogicIfWeShould( int Value )
        { }

        private readonly List<IArcenUI_Dropdown_Option> cachedOptions = List<IArcenUI_Dropdown_Option>.Create_WillNeverBeGCed( 20, "DrawShipModelsCutoffFiller-cachedOptions" );
        public List<IArcenUI_Dropdown_Option> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 )
            {
                for ( int i = 0; i < UnitDrawStyleTable.Instance.Rows.Count; i++ )
                    cachedOptions.Add( new UnitDrawStyleOption( UnitDrawStyleTable.Instance.Rows[i] ) );
            }
            return cachedOptions;
        }
    }

    public class TooltipFleetMembershipSortingPriority : IDropdownFiller
    {
        public int GetCurrentValue( ArcenSetting setting )
        {
            return GameSettings.Current.GetIntBySetting( setting );
        }

        public int GetTempValue( ArcenSetting setting )
        {
            return setting.TempValue_Int;
        }

        public void SetExtraValuesFromSpecialLogicIfWeShould( int Value )
        { }

        private readonly List<IArcenUI_Dropdown_Option> cachedOptions = List<IArcenUI_Dropdown_Option>.Create_WillNeverBeGCed( 20, "TooltipFleetMembershipSortingPriority-cachedOptions" );
        public List<IArcenUI_Dropdown_Option> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 )
            {
                cachedOptions.Add( new TooltipFleetMembershipSortingPriorityOption( "Type > Name > Strength" ) );
                cachedOptions.Add( new TooltipFleetMembershipSortingPriorityOption( "Type > Strength > Name" ) );
                cachedOptions.Add( new TooltipFleetMembershipSortingPriorityOption( "Strength > Name > Type" ) );
                cachedOptions.Add( new TooltipFleetMembershipSortingPriorityOption( "Strength > Type > Name" ) );
                cachedOptions.Add( new TooltipFleetMembershipSortingPriorityOption( "Name > Strength > Type" ) );
                cachedOptions.Add( new TooltipFleetMembershipSortingPriorityOption( "Name > Type > Strength" ) );
            }
            return cachedOptions;
        }
    }

    public class TooltipWeaponActivityDetails : IDropdownFiller
    {
        public int GetCurrentValue( ArcenSetting setting )
        {
            return GameSettings.Current.GetIntBySetting( setting );
        }

        public int GetTempValue( ArcenSetting setting )
        {
            return setting.TempValue_Int;
        }

        public void SetExtraValuesFromSpecialLogicIfWeShould( int Value )
        { }

        private readonly List<IArcenUI_Dropdown_Option> cachedOptions = List<IArcenUI_Dropdown_Option>.Create_WillNeverBeGCed( 20, "TooltipWeaponActivityDetails-cachedOptions" );
        public List<IArcenUI_Dropdown_Option> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 )
            {
                cachedOptions.Add( new TooltipFleetMembershipSortingPriorityOption( "None" ) );
                cachedOptions.Add( new TooltipFleetMembershipSortingPriorityOption( "Reload Cycle" ) );
                cachedOptions.Add( new TooltipFleetMembershipSortingPriorityOption( "Last Damage" ) );
                cachedOptions.Add( new TooltipFleetMembershipSortingPriorityOption( "All" ) );
            }
            return cachedOptions;
        }
    }

    #region ResolutionOption
    public class ResolutionOption : IArcenUI_Dropdown_Option
    {
        private ArcenPoint WidthHeight;
        private string ResolutionName = string.Empty;

        public ResolutionOption( ArcenPoint WidthHeight )
        {
            this.WidthHeight = WidthHeight;
        }

        public object GetItem()
        {
            return this.WidthHeight;
        }

        public string GetOptionNameFromVolatile()
        {
            if ( this.ResolutionName.Length <= 0 )
                this.ResolutionName = this.WidthHeight.X + "x" + this.WidthHeight.Y;
            return this.ResolutionName;
        }

        public Sprite GetOptionSprite()
        {
            return null;
        }
    }
    #endregion

    #region CameraTypeOption
    public class CameraTypeOption : IArcenUI_Dropdown_Option
    {
        private readonly ArcenCameraType Item;

        public CameraTypeOption( ArcenCameraType Item )
        {
            this.Item = Item;
        }

        public object GetItem()
        {
            return this.Item;
        }

        public string GetOptionNameFromVolatile()
        {
            ArcenCharacterBuffer displayNameBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "CameraTypeOption-displayNameBuffer" );
            displayNameBuffer.Add( "<size=85%>" );
            if ( this.Item == null )
                displayNameBuffer.Add( "None" );
            else
            {
                displayNameBuffer.Add( this.Item.GetDisplayName() );
                displayNameBuffer.AddDlcMod( this.Item );
            }

            return displayNameBuffer.ToStringAndReturnToPool();
        }

        public Sprite GetOptionSprite()
        {
            return null;
        }
    }
    #endregion

    #region FramerateTypeOption
    public class FramerateTypeOption : IArcenUI_Dropdown_Option
    {
        private readonly FramerateType Item;

        public FramerateTypeOption( FramerateType Item )
        {
            this.Item = Item;
        }

        public object GetItem()
        {
            return this.Item;
        }

        public string GetOptionNameFromVolatile()
        {
            return this.Item.InternalName;
        }

        public Sprite GetOptionSprite()
        {
            return null;
        }
    }
    #endregion

    #region UnitDrawStyleOption
    public class UnitDrawStyleOption : IArcenUI_Dropdown_Option
    {
        private readonly UnitDrawStyle Item;

        public UnitDrawStyleOption( UnitDrawStyle Item )
        {
            this.Item = Item;
        }

        public object GetItem()
        {
            return this.Item.DisplayName;
        }

        public string GetOptionNameFromVolatile()
        {
            return this.Item.InternalName;
        }

        public Sprite GetOptionSprite()
        {
            return null;
        }
    }
    #endregion

    #region TooltipFleetMembershipSortingPriorityOption
    public struct TooltipFleetMembershipSortingPriorityOption : IArcenUI_Dropdown_Option
    {
        private string Text;

        public TooltipFleetMembershipSortingPriorityOption( string Text )
        {
            this.Text = Text;
        }

        public object GetItem()
        {
            return this.Text;
        }

        public string GetOptionNameFromVolatile()
        {
            return this.Text;
        }

        public Sprite GetOptionSprite()
        {
            return null;
        }
    }
    #endregion
}
