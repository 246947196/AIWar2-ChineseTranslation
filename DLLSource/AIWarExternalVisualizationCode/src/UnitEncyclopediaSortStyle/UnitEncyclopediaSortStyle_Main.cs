using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;
using Arcen.AIW2.External;
using UnityEngine;

namespace Arcen.AIW2.ExternalVisualization
{
    public class UnitEncyclopediaSortStyle_Main : IUnitEncyclopediaSortStyleImplementation
    {
        public bool ShouldShowShortNameInsteadOfFullDisplayName( UnitEncyclopediaSortStyle Style )
        {
            switch ( Style.InternalName )
            {
                case "ByShortName":
                    return true;
            }
            return false;
        }

        public void AddTextIfNeeded( UnitEncyclopediaSortStyle Style, GameEntityTypeData TypeData, ArcenDoubleCharacterBuffer buffer )
        {
            switch ( Style.InternalName )
            {
                case "ByLineStrength":
                    buffer.AddStrength_Truncated( TypeData.LineStrength(), true ).Add( "<pos=90>" );
                    break;
                case "ByStrengthValue":
                    buffer.AddStrength_Truncated( TypeData.StrengthValue(), true ).Add( "<pos=90>" );
                    break;
                case "ByArmor":
                    buffer.AddArmor( TypeData.Armor_mm, true ).Add( "<pos=70>" );
                    break;
                case "ByAlbedo":
                    buffer.AddAlbedo( TypeData.MarkStatsFor( TypeData.EncyclopediaOnly_MarkLevel.Display ).Albedo, true ).Add( "<pos=70>" );
                    break;
                case "ByEngine":
                    buffer.AddEngine( TypeData.Engine_gx, true ).Add( "<pos=70>" );
                    break;
                case "ByMass":
                    buffer.AddMass( TypeData.Mass_tX, true ).Add( "<pos=70>" );
                    break;
                case "BySpeed":
                    buffer.AddSpeed_MoreReadable( TypeData.MarkStatsFor( TypeData.EncyclopediaOnly_MarkLevel.Display ).Speed, true ).Add( "<pos=80>" );
                    break;
                case "ByLineEnergy":
                    buffer.AddEnergy_Truncated( TypeData.LineEnergy(), true ).Add( "<pos=100>" );
                    break;
                case "ByLineMetal":
                    buffer.AddMetal_Truncated( TypeData.LineMetal(), true ).Add( "<pos=100>" );
                    break;
                case "ByLineHull":
                    buffer.AddHull_Truncated( TypeData.LineHull(), true ).Add( "<pos=100>" );
                    break;
                case "ByLineShields":
                    buffer.AddShield_Truncated( TypeData.LineShields(), true ).Add( "<pos=100>" );
                    break;
                case "ByLineHealth":
                    buffer.AddHull_Truncated( TypeData.LineHealth(), true ).Add( "<pos=100>" );
                    break;
                case "ByHealthValue":
                    buffer.AddHull_Truncated( TypeData.HealthValue(), true ).Add( "<pos=100>" );
                    break;
                case "ByLineDps":
                    buffer.AddDamage_Truncated( TypeData.LineDps(), true ).Add( "<pos=100>" );
                    break;
                case "ByDpsValue":
                    buffer.AddDamage_Truncated( TypeData.DpsValue(), true ).Add( "<pos=100>" );
                    break;
                case "ByAICost":
                    buffer.AddMetal_Truncated( TypeData.AICost(), true ).Add( "<pos=100>" );
                    break;
                case "ByAIValue":
                    buffer.AddMetal_Truncated( TypeData.AIValue(), true ).Add( "<pos=100>" );
                    break;
            }
        }

        public void SortAllCategories( UnitEncyclopediaSortStyle Style )
        {
            ThematicGroup group = Window_UnitEncyclopedia.Instance.CurrentCategory;
            if ( group == null )
                return;

            //lock (group.ResortedUnitTypesForEncyclopedia)
            {
                group.ResortedUnitTypesForEncyclopedia.ClearConstructionListForStartingConstruction();

                foreach ( GameEntityTypeData typeData in group.SortedUnitTypes )
                    group.ResortedUnitTypesForEncyclopedia.AddToConstructionList( typeData );

                switch ( Style.InternalName )
                {
                    case "ByName":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    
                    case "ByShortName":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Left.DisplayNameForSidebar.CompareTo( Right.DisplayNameForSidebar );
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    
                    case "ByLineStrength":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Right.LineStrength().CompareTo( Left.LineStrength() );
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    
                    case "ByStrengthValue":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Right.StrengthValue().CompareTo( Left.StrengthValue() );
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    
                    case "ByArmor":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Right.Armor_mm.CompareTo( Left.Armor_mm );
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    
                    case "ByAlbedo":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Right.MarkStatsFor( Right.EncyclopediaOnly_MarkLevel.Display ).Albedo.CompareTo( Left.MarkStatsFor( Left.EncyclopediaOnly_MarkLevel.Display ).Albedo );
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    
                    case "ByEngine":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Right.Engine_gx.CompareTo( Left.Engine_gx );
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    case "ByMass":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Right.Mass_tX.CompareTo( Left.Mass_tX );
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    
                    case "BySpeed":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Right.MarkStatsFor( Right.EncyclopediaOnly_MarkLevel.Display ).Speed.CompareTo( Left.MarkStatsFor( Left.EncyclopediaOnly_MarkLevel.Display ).Speed );
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    
                    case "ByLineEnergy":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Right.LineEnergy().CompareTo( Left.LineEnergy() );
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    
                    case "ByLineMetal":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Right.LineMetal().CompareTo( Left.LineMetal() );
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    
                    case "ByLineHull":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Right.LineHull().CompareTo( Left.LineHull() );
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    
                    case "ByLineShields":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Right.LineShields().CompareTo( Left.LineShields() );
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    
                    case "ByLineHealth":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Right.LineHealth().CompareTo( Left.LineHealth() );
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    
                    case "ByHealthValue":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Right.HealthValue().CompareTo( Left.HealthValue() );
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    
                    case "ByLineDps":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Right.LineDps().CompareTo( Left.LineDps() );
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    case "ByDpsValue":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Right.DpsValue().CompareTo(Left.DpsValue());
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    
                    case "ByAICost":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Right.AICost().CompareTo(Left.AICost());
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    
                    case "ByAIValue":
                        group.ResortedUnitTypesForEncyclopedia.SortConstructionList_Stable( delegate ( GameEntityTypeData Left, GameEntityTypeData Right )
                        {
                            int val = Right.AIValue().CompareTo(Left.AIValue());
                            if ( val != 0 )
                                return val;
                            return Left.DisplayName.CompareTo( Right.DisplayName );
                        } );
                        break;
                    
                    default:
                        throw new Exception( "Setup error!  No sort implemented for '" + Style.InternalName + "'" );
                }

                group.ResortedUnitTypesForEncyclopedia.SwitchConstructionToDisplay();
            }
        }
    }
}