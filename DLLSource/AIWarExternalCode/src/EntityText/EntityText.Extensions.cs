using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public static partial class EntityText_Extensions
    {
        #region Data
        
        private static System.Collections.Generic.Dictionary<ArcenRejectionReason,string> ReasonDisplayNames = new System.Collections.Generic.Dictionary<ArcenRejectionReason, string>()
        {
            { ArcenRejectionReason.Unknown, "启用中" },
            { ArcenRejectionReason.ThisShipIsMalformedData, "损坏" },
            { ArcenRejectionReason.ShipPassedInWasNull, "NullShip" },
            { ArcenRejectionReason.ForMarkOfGameEntityTypeDataIsNull, "NullMark" },
            { ArcenRejectionReason.FleetIsNull, "NullFleet" },
            { ArcenRejectionReason.SystemTypeIsNull, "NullSystem" },

            { ArcenRejectionReason.CloakingNotFunctionalForGuardPostsOnNonAIPlanets, "星球未占领" },
            { ArcenRejectionReason.InUnexploredSpace, "未知星球" },
            { ArcenRejectionReason.CeasefireAtPlanet, "星球停火" },
            { ArcenRejectionReason.NonFunctionalWhenNotOnPlanetOwnedByMyFaction, "星球未占领" },

            { ArcenRejectionReason.EntityHasNotYetBeenFullyClaimed, "未占领" },
            { ArcenRejectionReason.CloakingNotFunctionalForUnOwnedShips, "未占领" },

            { ArcenRejectionReason.EntityIsInHoldFireMode, "待命" },
            { ArcenRejectionReason.EntityIsParalyzed, "瘫痪" },
            { ArcenRejectionReason.SystemIsStillOnCooldown, "冷却中" },
 
            { ArcenRejectionReason.ThisShipIsWrongStateOfMatter, "物态" },
            { ArcenRejectionReason.SystemIsToggledOff, "已关闭" },
            { ArcenRejectionReason.SystemNotFunctionalAtThisMarkLevel, "未达到等级" },
            { ArcenRejectionReason.SystemModuleIsNotEnabled, "模组关闭" },
            { ArcenRejectionReason.SystemChargeInsufficient, "未充能" },
            { ArcenRejectionReason.SystemChargeNotEqual, "未充能" },
            { ArcenRejectionReason.SystemChargeNotGreater, "充能不足" },
            { ArcenRejectionReason.SystemChargeNotLess, "充能过剩" },
        };
        
        private static System.Collections.Generic.Dictionary<CannotTransportReason,string> CannotTransportNames = new System.Collections.Generic.Dictionary<CannotTransportReason, string>()
        {
            /*
            TranportingIsFine,

            IAmAlreadyDead,
            IAmRemains,
            IAmNotMobile,
            IAmNotYetClaimed,
            IAmTractored,
            IAmParalyzed,
            IAmADroneCreator,

            CannotTransportCriticalInfrastructure,
            CannotTransportPlayerLosesIfAnyDie,
            CannotTransportPlayerWinsIfAllAreDestroyed,
            CannotTransportIsScrappingByPlayerDisallowed,
            CannotTransportCannotBeStoredInsideGuardPost,
            CannotTransportPlanetaryFleetMembers,
            CannotTransportMetalStorage,
            CannotTransportSelfAttritionsXPercentPerSecondIfParentShipNotOnPlanet,

            MissingOrDeadTransport,
            TransportNoLongerOnSamePlanet,
            WrongFactionForTransport,
            NotAMobilePlayerFleet,
            NotSameMobilePlayerFleetAsTransport,
            IAmNotInMobileFleet,

            IsReinforcementLocation,
            IsGuardian,
            IsExplicitlyBlockedFromTransporting,

            CannotFinishThisJobOnMPClient,

            Length
            */
            
            //{ CannotTransportReason.TranportingIsFine, "TranportingIsFine" },
        };
        
        private static System.Collections.Generic.Dictionary<SpecialEntityType,string> SpecialTypeDisplayNames = new System.Collections.Generic.Dictionary<SpecialEntityType, string>()
        {
            { SpecialEntityType.GuardPost, "守卫哨站" },
            { SpecialEntityType.DireGuardPost, "凶残守卫哨站" },
        };

        #endregion

        #region Enums
        
        public static string ToString( this ArcenRejectionReason val )
        {
            string name;
            if (!ReasonDisplayNames.TryGetValue(val, out name))
            {
                name = Enum.GetName(typeof(ArcenRejectionReason), val);
                ReasonDisplayNames[val] = name;
            }

            return name;
        }
      
        public static string ToString( this SpecialEntityType val )
        {
            string name;
            if (!SpecialTypeDisplayNames.TryGetValue(val, out name))
            {
                name = Enum.GetName(typeof(SpecialEntityType), val);
                SpecialTypeDisplayNames[val] = name;
            }

            return name;
        }
        
        public static string ToString( this CannotTransportReason val )
        {
            string name;
            if (!CannotTransportNames.TryGetValue(val, out name))
            {
                name = Enum.GetName(typeof(CannotTransportReason), val);
                CannotTransportNames[val] = name;
            }

            return name;
        }
        
        public static TextTerm Term(this ResourceType res)
        {
            switch (res)
            {
                case ResourceType.Metal:
                    return TextTerm.Metal;
                case ResourceType.Energy:
                    return TextTerm.Energy;
                case ResourceType.Science:
                    return TextTerm.Science;
                case ResourceType.Hacking:
                    return TextTerm.Hacking;
                case ResourceType.FuelArgon:
                    return TextTerm.FuelArgon;
                case ResourceType.FuelRadon:
                    return TextTerm.FuelRadon;
                case ResourceType.FuelXenon:
                    return TextTerm.FuelXenon;
                default:
                    throw new Exception( "Error: Could not find Term for '" + res + "'" );
            }
        }

        public static string Char(this DamageModifierComparisonType Comparison)
        {
            switch ( Comparison )
            {
                case DamageModifierComparisonType.LessThan:
                    return "<";
                case DamageModifierComparisonType.GreaterThan:
                    return ">";
                case DamageModifierComparisonType.AtMost:
                    return "≤";
                case DamageModifierComparisonType.AtLeast:
                    return "≥";
                case DamageModifierComparisonType.MultiplesOf:
                    return "×";
                case DamageModifierComparisonType.Equals:
                    return "=";
                case DamageModifierComparisonType.NotEquals:
                    return "≠";
            }
            
            return "";
        }
        
        #endregion

        #region ArcenCharacterBufferBase
        
        public static EntityTextWriter Writer( this ArcenCharacterBufferBase buffer )
        {
            return buffer.UserObj as EntityTextWriter;
        }

        /*
        public static ArcenCharacterBufferBase AddCondition( this ArcenCharacterBufferBase Buffer, DamageModifierBasedOn Stat, DamageModifierComparisonType Comparison, int ReferenceValue )
        {
            Buffer.Open(Stat, TermUse.Icon);
            if (style == null)
                style = TextStyle.Number;
            
            FInt perc = val.ToPercent(1) - 100.ToFInt();

            buffer
                .Open(style)
                .AddFormat(perc.ToInt(), "{0:+#;-#0}%")
                .Close(style);
            
            return buffer;
        }
        */
        
        public static ArcenCharacterBufferBase AddMultiplier( this ArcenCharacterBufferBase buffer, FInt val, TextStyle style=null )
        {
            if (style == null)
                style = TextStyle.Number;
            
            FInt perc = val.ToPercent(1) - 100.ToFInt();

            buffer
                .Open(style)
                .AddFormat(perc.ToInt(), "{0:+#;-#0}%")
                .Close(style);
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase BeginStatement( this ArcenCharacterBufferBase buffer )
        {
            buffer.Pad(TextStyle.Pad_Small);
            return buffer;
        }
        
        public static ArcenCharacterBufferBase EndStatement( this ArcenCharacterBufferBase buffer )
        {
            buffer.Pad(TextStyle.Pad_Small);
            return buffer;
        }
        
        public static ArcenCharacterBufferBase BeginStatement( this ArcenCharacterBufferBase buffer, TextStyle style )
        {
            buffer.Pad(TextStyle.Pad_Small);
            if (style != null)
                buffer.Open(style);
            //buffer.NewLine();
            return buffer;
        }
        
        public static ArcenCharacterBufferBase EndStatement( this ArcenCharacterBufferBase buffer, TextStyle style )
        {
            if (style != null)
                buffer.Close(style);
            buffer.Pad(TextStyle.Pad_Small);
            return buffer;
        }
        
        public static ArcenCharacterBufferBase EndStatement( this ArcenCharacterBufferBase buffer, EndStatementStyle end )
        {
            if (end != null && 
                !string.IsNullOrEmpty(end.value))
            {
                if (!buffer.Builder.IsNewLine())
                {
                    buffer.Add(end.value);
                }
            }
            
            if (end.pad)
                buffer.Pad(TextStyle.Pad_Small);

            return buffer;
        }
        
        #region unused
        /*
        public static ArcenCharacterBufferBase Add( this ArcenCharacterBufferBase buffer, GameEntity_Squad e )
        {
            // sprite
            {
                buffer.AddSprite(
                    e.TypeData.TexEmbedSprite_Icon.InternalName, 
                    e.TypeData.TexEmbedSprite_IconBorder.InternalName,
                    e.TypeData.TexEmbedSprite_IconOverlay.InternalName, 
                    e.GetFactionCenterColor().ColorHex, 
                    e.GetFactionTrimColor().ColorHex,
                    "FFFFFF");
            }
            
            // name
            {
                var name = e.FleetMembership?.GetDisplayNameForSidebar();
                if ( string.IsNullOrWhiteSpace( name ) )
                    name = e.GetTypeDisplayNameSafe();
                buffer.Add( name );
            }

            // Ship mark level and count
            {
                var markLevel = e.DataForMark?.MarkLevel;

                buffer.Add( "<sub>" );
                if ( markLevel != null )
                {
                    buffer.Add( "<color=#" ).Add( markLevel.ColorHex ).Add( ">" );
                    buffer.Add( " " ).Add( markLevel.MapDisplay );
                    buffer.Add( "</color>" );
                }

                buffer.Add( "</sub>" );

                buffer.Add( "<size=80%>" );
                {
                    if ( e.ShipCount > 1 ) buffer.Add( " x" ).Add( e.ShipCount );

                    #region unused

                    buffer.Add("<size=80%>");
                    switch (IconStatus)
                    {
                        case ShipIconStatus.InGuardPost:
                            buffer.EndColor().Add(" (GP)", "ff622b");
                            break;
                        case ShipIconStatus.LoadedDrone:
                            buffer.EndColor().Add(" (L)", "ff622b");
                            break;
                        case ShipIconStatus.BeingTransported:
                            buffer.EndColor().Add(" (T)", "59d2ff");
                            break;
                        case ShipIconStatus.Remains:
                            buffer.EndColor().Add(" (D)", "808080");
                            break;
                        case ShipIconStatus.Crippled:
                            buffer.EndColor().Add(" (D)", "808080");
                            break;
                        case ShipIconStatus.UnderConstruction:
                            buffer.EndColor().Add(" (B)", "ffd966");
                            break;
                        case ShipIconStatus.BeingClaimed:
                            buffer.EndColor().Add(" (C)", "ffd966");
                            break;
                            //case ShipIconStatus.Alive:
                            //buffer.Add( "(A)" );
                            //break;
                            //default:
                            //buffer.Add( "(?)" );
                            //break;
                    }
                    buffer.Add("</size>");

                    #endregion
                }
                buffer.Add( "</size>" );
            }

            return buffer;
        }
        */
        #endregion

        public static ArcenCharacterBufferBase Add( this ArcenCharacterBufferBase buffer, ResourceType res, TermUse use, TextStyle sprite_style_override = null )
        {
            return buffer.Add(res.Term(), use, sprite_style_override);
        }

        public static ArcenCharacterBufferBase Add( this ArcenCharacterBufferBase buffer, TextTerm term, TermUse use, TextStyle sprite_style_override = null )
        {
            buffer.Open(term, use, sprite_style_override);
            buffer.Close(term);
            return buffer;
        }
        
        public static ArcenCharacterBufferBase Add( this ArcenCharacterBufferBase buffer, string text, TextTerm term, TermUse use, TextStyle sprite_style_override = null )
        {
            buffer.Open(term, use, sprite_style_override);
            buffer.Add(text);
            buffer.Close(term);
            return buffer;
        }
        
        public static ArcenCharacterBufferBase Open( this ArcenCharacterBufferBase buffer, TextTerm term, TermUse use, TextStyle sprite_style_override = null )
        {
            int debugstage = 0;
            try
            {
                debugstage = 100;
                if (term == TextTerm.Null)
                {
                    LOG.Msg("Open TextTerm.{0} probably wasn't intended? From:\n{1}", term, LOG.StackTrace());
                    return buffer;
                }
                
                debugstage = 110;
                var format = EntityText.GetFormatForTerm(term);
                debugstage = 120;
                var writer = (buffer.UserObj as EntityTextWriter);
                debugstage = 130;
                writer.Open_Term = term;
                writer.Open_Term_Usage = use;
                
                debugstage = 140;
                buffer.StartColor(format.Color);
                
                bool icon = (use & TermUse.Icon) > 0;
                bool name = (use & TermUse.Name) > 0;
                //bool abbr = (use & TermUse.Abbr) > 0;
                
                if (icon && 
                    !format.HasSprite)
                {
                    icon = false;
                    name = true;
                }

                debugstage = 150;
                
                if (icon)
                {
                    TextStyle sprite_style = sprite_style_override;
                    if (sprite_style_override == null)
                        sprite_style = TextStyle.TextTerm_Sprite;
                
                    buffer
                        .Open(sprite_style)
                        .AddSprite(format.Sprite, format.Color)
                        .Close(sprite_style)
                        ;
                    if (format.HasAfterSprite)
                        buffer.Add(format.AfterSprite);
                }
                
                debugstage = 170;
                if (name)
                {
                    //if (icon)
                        //buffer.Add(" ");
                    
                    debugstage = 180;
                    buffer.Add(format.Name);
                }
                
                debugstage = 200;
            }
            catch (Exception e)
            {
                LOG.Err("at debugstage {0}\n{1}", debugstage, e);
            }
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase Close( this ArcenCharacterBufferBase buffer, TextTerm term )
        {
            if (term == TextTerm.Null)
            {
                LOG.Msg("Close TextTerm.{0} probably wasn't intended? From:\n{1}", term, LOG.StackTrace());
                return buffer;
            }
            
            var writer = (buffer.UserObj as EntityTextWriter);
            if (writer.Open_Term != term)
            {
                LOG.Err("Close TextTerm.{0} called when its not open ({1} is). From:\n{2}", term, writer.Open_Term, LOG.StackTrace());
                
                writer.Open_Term = TextTerm.Null;
                writer.Open_Term_Usage = TermUse.Null;
                
                return buffer;
            }
            
            var use = writer.Open_Term_Usage;
            var format = EntityText.GetFormatForTerm(term);

            if ((use & TermUse.IconAfter) > 0)
            {
                if ( format.HasSprite )
                    buffer.AddSprite(format.Sprite, format.Color);
            }

            //bool abbr = (use & TermUse.Abbr) > 0;

            //if ((use & TermUse.Abbr) > 0 &&
            //    format.HasAbbr)
            //{
            //    buffer.Add(" ").Add(format.Abbr, TextStyle.Fraction);
            //}

            buffer.EndColor();
            
            writer.Open_Term = TextTerm.Null;
            writer.Open_Term_Usage = TermUse.Null;

            return buffer;
        }

        public static void Dump( this ArcenCharacterBufferBase buffer, Log log )
        {
            if (log == null)
                return;
            
            log.AppendLine("DumpNextTooltipText:");
            log.AppendLine("----------------------");
            log.AppendLine(buffer.ToString());//.Replace("\n", "\n\\n"));
            log.AppendLine("----------------------");

            int counter = 1;
            foreach (var b in buffer.Builder.EnumerateBlocks())
            {
                log.AppendFormat("block #{0}: {1}\n----------------------\n",
                    counter, b.DebugDisplay());
                counter++;
            }
            log.AppendLine("----------------------");
            
            log.Flush();
        }
        
        public static void Dump(this Log log, ArcenCharacterBufferBase buffer)
        {
            if (log != null)
                buffer.Dump(log);
        }
        
        public static ArcenCharacterBufferBase RemoveAllTags( this ArcenCharacterBufferBase buffer )
        {
            retry:
            foreach (var b in buffer.Builder.EnumerateBlocks())
            {
                if (b.Type != TextBlock.BlockType.Text)
                {
                    buffer.Builder.Remove(b.Idx, b.Len);
                    goto retry;
                }
            }
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase NewLineIfNeeded(this ArcenCharacterBufferBase buffer)
        {
            var builder = buffer.Builder;
            if (!builder.IsNewLine())
                builder.AppendLine();
            
            return buffer;    
        }
        
        public static ArcenCharacterBufferBase AddColor(this ArcenCharacterBufferBase buffer, int text, string colorhex)
        {
            bool hasColor = !string.IsNullOrEmpty(colorhex);
            if (hasColor)
                buffer.StartColor(colorhex);
            
            buffer.Add(text);
                
            if (hasColor)
                buffer.EndColor();
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase AddColor(this ArcenCharacterBufferBase buffer, FInt text, string colorhex)
        {
            bool hasColor = !string.IsNullOrEmpty(colorhex);
            if (hasColor)
                buffer.StartColor(colorhex);
            
            buffer.Add(text);
                
            if (hasColor)
                buffer.EndColor();
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase AddColor(this ArcenCharacterBufferBase buffer, float text, string colorhex)
        {
            bool hasColor = !string.IsNullOrEmpty(colorhex);
            if (hasColor)
                buffer.StartColor(colorhex);
            
            buffer.Add(text);
                
            if (hasColor)
                buffer.EndColor();
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase AddColor(this ArcenCharacterBufferBase buffer, string text, string colorhex)
        {
            bool hasColor = !string.IsNullOrEmpty(colorhex);
            if (hasColor)
                buffer.StartColor(colorhex);
            
            buffer.Add(text);
                
            if (hasColor)
                buffer.EndColor();
            
            return buffer;
        }

        public static ArcenCharacterBufferBase Write( this ArcenCharacterBufferBase buffer, EntityTypeDrawingBag bag )
        {
            var writer = (buffer.UserObj as EntityTextWriter);
            if (writer != null)
                writer.Write(bag);
            return buffer;
        }

        public static ArcenCharacterBufferBase WriteShips( this ArcenCharacterBufferBase buffer, ShipsForDisplay ships, TextVarMap varmap=null, string delimiter=", " )
        {
            var writer = (buffer.UserObj as EntityTextWriter);
            if (writer == null)
                return buffer;
            
            writer.WriteShips(ships);
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase WriteShips( this ArcenCharacterBufferBase buffer, List<LazyLoadSquadWrapper> ships )
        {
            var writer = (buffer.UserObj as EntityTextWriter);
            if (writer == null)
                return buffer;
            
            writer.WriteShips(ships);
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase WriteShips( this ArcenCharacterBufferBase buffer, List<GameEntity_Squad> ships )
        {
            var writer = (buffer.UserObj as EntityTextWriter);
            if (writer == null)
                return buffer;
            
            writer.WriteShips(ships);
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase WriteShip( this ArcenCharacterBufferBase buffer, LazyLoadSquadWrapper ship )
        {
            var writer = (buffer.UserObj as EntityTextWriter);
            if (writer == null)
                return buffer;
            
            writer.WriteShip(ship);
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase WriteShip( this ArcenCharacterBufferBase buffer, GameEntity_Squad ship )
        {
            var writer = (buffer.UserObj as EntityTextWriter);
            if (writer == null)
                return buffer;
            
            writer.WriteShip(ship);
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase WriteSpawn( this ArcenCharacterBufferBase buffer, GameEntityTypeData type_spawned, TextStyle spawn_icon_style=null)
        {
            var writer = (buffer.UserObj as EntityTextWriter);
            if (writer == null)
                return buffer;
            
            writer.WriteSpawn(type_spawned, 1, spawn_icon_style);
            
            return buffer;
        }

        public static ArcenCharacterBufferBase AddShipIconNameFull( this ArcenCharacterBufferBase buffer, GameEntityTypeData type )
        {
            return buffer.AddShipIconInline(type, null, TextStyle.Ship_Sprite_Smaller).Add(type.GetDisplayName(), SpecialFactionDataTable.Instance.DefaultRow.DefaultFactionCenterColor.ColorHexBrighter);
        }
        
        public static ArcenCharacterBufferBase AddShipIconNameShort( this ArcenCharacterBufferBase buffer, GameEntityTypeData type )
        {
            return buffer.AddShipIconInline(type, null, TextStyle.Ship_Sprite_Smaller).Add(type.GetShortDisplayName(), SpecialFactionDataTable.Instance.DefaultRow.DefaultFactionCenterColor.ColorHexBrighter);
        }
        
        #region AddNumber
        
        public static ArcenCharacterBufferBase AddNumberRange( this ArcenCharacterBufferBase buffer, FloatRange num, TextTerm term, TermUse use )
        {
            bool hasmin = false;
            if (num.Min > num.AbsMin)
            {
                hasmin = true;
            }
            
            bool hasmax = false;
            if (num.Max != 0 && num.Max <= num.AbsMax)
            {
                hasmax = true;
            }
            
            buffer.AddNumber(
                ()=>
                {
                    // value is bound on one side
                    if (hasmin != hasmax)
                    {
                        if (hasmin)
                        {
                            buffer.Add(">").Add(ArcenExternalUIUtilities.SpaceAfterIcon).AddNumber(num.Min, null, TextStyle.Empty);
                        }
                        else
                        {
                            buffer.Add("<").Add(ArcenExternalUIUtilities.SpaceAfterIcon).AddNumber(num.Max, null, TextStyle.Empty);
                        }
                        
                        return;
                    }
                    
                    // value is bound on both sides
                    buffer.AddNumber(num.Min, null, TextStyle.Empty)
                        .Add(ArcenExternalUIUtilities.SpaceAfterIcon)
                        .Add(Text.Range_Exc)
                        .Add(ArcenExternalUIUtilities.SpaceAfterIcon).AddNumber(num.Max, null, TextStyle.Empty);
                    
                }, null, term, use, null);
            
            return buffer;
        }

        public static ArcenCharacterBufferBase AddTermRange( this ArcenCharacterBufferBase buffer, TermRange range )
        {
            int debugstage = 0;
            try
            {
                debugstage = 112;
                
                float comp_val = range.Val;

                bool max = false;
                bool perc = false;
                bool missing = false;
                bool remaining = false;
                TextTerm term = range.Term;
                
                var format = EntityText.GetFormatForTerm(term);

                debugstage = 114;
                
                bool prefix_space = false;
                string prefix = null;
                string suffix = null;
                switch ( range.Cmp )
                {
                    case TermCmp.Less:
                        prefix = "<";
                        prefix_space = true;
                        break;
                    case TermCmp.Greater:
                        prefix = ">";
                        prefix_space = true;
                        break;
                    case TermCmp.LessEq:
                        prefix = "≤";
                        prefix_space = true;
                        break;
                    case TermCmp.GreaterEq:
                        //prefix = "";// " ≥ ";
                        suffix = "+";
                        break;
                    case TermCmp.Equals:
                        prefix = "=";
                        prefix_space = true;
                        break;
                    case TermCmp.NotEq:
                        prefix = "≠";
                        prefix_space = true;
                        break;
                    default:
                        prefix = Extensions.ToString(range.Cmp);
                        break;
                }
                
                debugstage = 115;

                if (term == TextTerm.TimeOnPlanet)
                {
                    debugstage = 124;
                    
                    buffer.Add("在星球上时间 ", TextStyle.Brighter);
                    
                    buffer.Open(TextStyle.MinutesAndSeconds);
                    
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        buffer.Add(prefix, TextCaps.Normal);
                        if (prefix_space)
                            buffer.Space("0.1em");
                    }

                    buffer.AddMinutesAndSeconds(comp_val);
                    
                    if (!string.IsNullOrEmpty(suffix))
                        buffer.Add(suffix);

                    buffer.Close(TextStyle.MinutesAndSeconds);
                    
                    debugstage = 125;
                    
                    return buffer;
                }
                
                if (term == TextTerm.Mark)
                {
                    var mk = (int)comp_val;
                    var level = Balance_MarkLevelTable.Instance.RowsByOrdinal[mk];
                    
                    buffer.Space("0.05em");

                    if (prefix != null)
                    {
                        buffer.Add(prefix);
                        if (prefix_space)
                            buffer.Space("0.1em");
                    }
                    
                    debugstage = 128;
                    
                    buffer.StartColor(level.ColorHex)
                          .Add("级", TextStyle.Sub2)
                          .Add(level.MapDisplay)
                          .EndColor();
                    
                    if (suffix != null)
                        buffer.Add(suffix);
                    
                    debugstage = 129;

                    return buffer;
                }
                            
                debugstage = 126;
                
                buffer.AddNumber(
                    ()=>
                    {
                        debugstage = 127;
                        
                        buffer.Space("0.05em");
                        if (max)
                            buffer.Add("最大", TextStyle.Sub);
                            //buffer.Add("↑");
                        if (prefix != null)
                        {
                            buffer.Add(prefix);
                            if (prefix_space)
                                buffer.Space("0.1em");
                        }
                        
                        debugstage = 128;
                        
                        buffer.AddNumber(comp_val, null, TextStyle.Empty);
                        if (perc)
                            buffer.Add("%");
                        
                        if (suffix != null)
                            buffer.Add(suffix);
                        
                        debugstage = 129;
                    },
                    null, term, TermUse.Icon, null);
                
                debugstage = 130;
                
                if (missing)
                    buffer.Add(" missing");
                if (remaining)
                    buffer.Add(" remaining");

                debugstage = 133;
            }
            catch (Exception e)
            {
                LOG.Err("exception at debugstage {0}\n{1}", debugstage, e);
            }
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase Add( this ArcenCharacterBufferBase buffer, FloatRange num, TextTerm term, TermUse use )
        {
            bool hasmin = false;
            if (num.Min > num.AbsMin)
            {
                hasmin = true;
            }
            
            bool hasmax = false;
            if (num.Max != 0 && num.Max <= num.AbsMax)
            {
                hasmax = true;
            }
            
            buffer.AddNumber(
                ()=>
                {
                    // value is bound on one side
                    if (hasmin != hasmax)
                    {
                        if (hasmin)
                        {
                            buffer.Add(">").Add(ArcenExternalUIUtilities.SpaceAfterIcon).AddNumber(num.Min, null, TextStyle.Empty);
                        }
                        else
                        {
                            buffer.Add("<").Add(ArcenExternalUIUtilities.SpaceAfterIcon).AddNumber(num.Max, null, TextStyle.Empty);
                        }
                        
                        return;
                    }
                    
                    // value is bound on both sides
                    buffer.AddNumber(num.Min, null, TextStyle.Empty)
                        .Add(ArcenExternalUIUtilities.SpaceAfterIcon)
                        .Add(Text.Range_Exc)
                        .Add(ArcenExternalUIUtilities.SpaceAfterIcon).AddNumber(num.Max, null, TextStyle.Empty);
                    
                }, null, term, use, null);
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, string num, TextTerm term, TermUse use = TermUse.Color)
        {
            return buffer.AddNumber( ()=>buffer.Add(num), null, term, use, null);
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, int num, TextTerm term)
        {
            return buffer.AddNumber((float)num, null, term, TermUse.Color, null);
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, int num, TextTerm term, TermUse use)
        {
            return buffer.AddNumber((float)num, null, term, use, null);
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, int num, string prefix, TextTerm term)
        {
            return buffer.AddNumber((float)num, prefix, term, TermUse.Color, null);
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, int num, string prefix, TextTerm term, TermUse use)
        {
            return buffer.AddNumber((float)num, prefix, term, use, null);
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, int num, string prefix, TextTerm term, TermUse use, string suffix)
        {
            return buffer.AddNumber((float)num, prefix, term, use, suffix);
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, FInt num, TextTerm term)
        {
            return buffer.AddNumber(num.ToFloat(), null, term, TermUse.Color, null);
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, FInt num, TextTerm term, TermUse use)
        {
            return buffer.AddNumber(num.ToFloat(), null, term, use, null);
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, FInt num, string prefix, TextTerm term)
        {
            return buffer.AddNumber(num.ToFloat(), prefix, term, TermUse.Color, null);
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, FInt num, string prefix, TextTerm term, TermUse use)
        {
            return buffer.AddNumber(num.ToFloat(), prefix, term, use, null);
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, FInt num, string prefix, TextTerm term, TermUse use, string suffix)
        {
            return buffer.AddNumber(num.ToFloat(), prefix, term, use, suffix);
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, float num, TextTerm term)
        {
            return buffer.AddNumber(num, null, term, TermUse.Color, null);
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, float num, TextTerm term, TermUse use)
        {
            return buffer.AddNumber(num, null, term, use, null);
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, float num, string prefix, TextTerm term)
        {
            return buffer.AddNumber(num, prefix, term, TermUse.Color, null);
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, float num, string prefix, TextTerm term, TermUse use)
        {
            return buffer.AddNumber(num, prefix, term, use, null);
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, float num, string prefix, TextTerm term, TermUse use, string suffix)
        {
            return buffer.AddNumber( 
                ()=>buffer.AddNumber(num, null, TextStyle.Empty),
                prefix, term, use, suffix);
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, Action AppendValue, string prefix, TextTerm term, TermUse use, string suffix)
        {
            var format = EntityText.GetFormatForTerm(term);
            
            if (use != TermUse.Null &&
                format.HasColor)
            {
                buffer.StartColor(format.Color);
            }
            
            // if this term has no icon (range)
            // .. 
            // if asked for icon and name?
            // well, that doesn't make sense for AddNumber [AIP]5 AIP
            // so whatever.
            // ..
            // but basically replace icon and/or icon_after
            // with name
            
            if ((use & TermUse.Icon) > 0)
            {
                if ( format.HasSprite )
                {
                    buffer.AddSprite(format.Sprite, format.Color);
                    if (format.HasAfterSprite)
                        buffer.Add(format.AfterSprite);
                }
                else
                {
                    buffer.Add(format.Name, format.Color);
                    if (format.HasAfterSprite)
                        buffer.Add(format.AfterSprite);
                    
                    buffer.Add(ArcenExternalUIUtilities.SpaceAfterIcon);
                }
                    
            }

            if (!string.IsNullOrEmpty(prefix))
            {
                buffer
                    .Add(prefix, TextStyle.Compare_Symbol)
                    .Add("");
            }
            
            AppendValue();
            
            if ((use & TermUse.IconAfter) > 0 &&
                format.HasSprite)
            {
                buffer
                    .Add(" ")
                    .Add(format);
            }
            
            //if ((use & TermUse.Abbr) > 0 &&
            //    format.HasAbbr)
            //{
            //    buffer
            //        .Add(" ")
            //        .Add(format.Abbr);
            //}
            
            if ((use & TermUse.Name) > 0)
            {
                buffer
                    .Add(" ")
                    .Add(format.Name);
            }

            if (!string.IsNullOrEmpty(suffix))
            {
                buffer.Add(suffix, TextStyle.Number_Units);
            }
            
            if (use != TermUse.Null &&
                format.HasColor)
            {
                buffer.EndColor();
            }
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase AddMultiplier(this ArcenCharacterBufferBase buffer, FInt num, TextTerm term, TermUse use = TermUse.Null)
        {
            var format = EntityText.GetFormatForTerm(term);
            buffer.StartColor(format.Color);
            
            if ((use & TermUse.Icon) > 0 &&
                format.HasSprite)
            {
                buffer.AddSprite(format.Sprite, format.Color);
                
                if (format.HasAfterSprite)
                    buffer.Add(format.AfterSprite);
            }

            buffer.AddMultiplier(num, TextStyle.Empty);
            
            if ((use & TermUse.IconAfter) > 0 &&
                format.HasSprite)
            {
                buffer.AddSprite(format.Sprite, format.Color);
                
                if (format.HasAfterSprite)
                    buffer.Add(format.AfterSprite);
            }
            
            //if ((use & TermUse.Abbr) > 0 &&
            //    format.HasAbbr)
            //{
            //    buffer
            //        .Add(" ")
            //        .Add(format.Abbr);
            //}
            
            if ((use & TermUse.Name) > 0)
            {
                buffer
                    .Add(" ")
                    .Add(format.Name);
            }
            
            buffer.EndColor();
            
            return buffer;
        }

        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, int num, string suffix=null, TextStyle style=null)
        {
            buffer.AddNumber((float)num, suffix, style);
            return buffer;
        }
        
        public static ArcenCharacterBufferBase AddNumber(this ArcenCharacterBufferBase buffer, FInt num, string suffix=null, TextStyle style=null)
        {
            buffer.AddNumber(num.ToFloat(), suffix, style);
            return buffer;
        }

        // Referenced in AddNumber but not actually used currently.
        private static readonly string Color_Gratuitous = "#39FF14"; // neon green 
        //private static readonly string Color_Obscene = "#39FF14"; //
        //private static readonly string Color_Extreme = "#ffc60a"; // bright gold
        private static readonly string Color_High = "#ED12DF"; // fushia-ish
        private static readonly string Color_Mid = "#F44336"; // red-ish
        private static readonly string Color_Low = "#808080"; // gray-ish

        public static ArcenCharacterBufferBase AddNumber( this ArcenCharacterBufferBase buffer, float num, string suffix=null, TextStyle style=null, string prefix=null)
        {
            double f = 0.0f;
            string col = null;
            string units = "";
            string format = "{0:#,###}";
            bool neg = num < 0;
            if (neg)
                num = -num;
            
            //show as -
            if (num == 0)
            {
                col = Color_Low;
                units = "";
                format = "0";
            }
            else if (num < .001)
            {
                col = Color_Low;
                units = "";
                format = "<b>-</b>";
            }
            //show as ≈0
            else
            if (num < .01)
            {
                col = Color_Low;
                units = "";
                format = "<size=65%>≈</size>0";
            }
            //show as .0#
            else
            if (num < .1)
            {
                col = Color_Low;
                int temp = (num * 100).RoundDown();
                f = temp;
                format = "<u>.</u>0{0:#}";
            }
            //show as .##
            else 
            if ( num < 1 )
            {
                //show as .##
                {
                    col = Color_Low;
                    int temp = (num * 100).RoundDown();
                    units = "";
                    // .1 -> 10
                    // .11 -> 11
                    // .01 -> 1
                    if ((temp % 10) == 0)
                    {
                        temp /= 10;
                        f = temp;
                        format = "<u>.</u>{0:#}";
                    }
                    else
                    {
                        f = temp;
                        format = "<u>.</u>{0:##}";
                    }
                }
                // show as ## c
                {
                    //col = Color_Low;
                    //f = (int)(num * 100);
                    //suf = "";
                    //format = "~1";
                    
                    //return buffer.Add("<",TextStyle.LessThan).Add("1");
                }
            }
            //show as #.#
            else 
            if ( num < 10 ) 
            {
                col = Color_Low;
                f = num;
                units = "";
                format = "{0:#.#}";
            }
            // less than 1k
            else 
            if ( num < 1000 )
            {
                col = Color_Low;
                f = num;
                units = "";
                format = "{0:###}";
            }
            // no range; less than 10k
            else 
            if ( num < 10000 )
            {
                col = Color_Low;
                f = num;
                units = "";
                format = "{0:#,###}";
            }
            // no range; less than 100k
            else 
            if ( num < 100000 )
            {
                col = Color_Low;
                f = num / 1000.0;
                units = "K";
                format = "{0:###.#}";
            }
            // K range; less than 1m
            else 
            if ( num < 1000000 )
            {
                col = Color_Mid;
                f = num / 1000.0;
                units = "K";
                format = "{0:###.#}";
            }
            // M range; less than 10m
            else 
            if ( num < 10000000 )
            {
                col = Color_High;
                f = num / 1000000.0;
                //p = 1;
                format = "{0:#.#}";
                units = "M";
            }
            // M range; less than 1b
            else 
            if ( num < 1000000000 )
            {
                col = Color_Gratuitous;
                f = (int)(num / 1000000.0);
                //p = 0;
                units = "M";
            }
            // B range; less than int.MaxValue (~2.1B)
            else 
            if ( num < int.MaxValue )
            {
                col = Color_Gratuitous;
                f = num / 1000000000.0;
                //p = 1;
                format = "{0:#.#}";
                units = "B";
            }
            else
            {
                col = Color_Low;
                units = "";
                format = "<size=150%><voffset=-0.1em>∞</voffset></size>";
            }
            
            //if (col != null)
                //buffer.StartColor( col );
                    
            if (style == null)
                style = TextStyle.Number;
            
            if (style == TextStyle.Empty)
                style = null;
            
            style?.Open(buffer, false, true);

            if (neg)
                f = -f;
            
            if (!string.IsNullOrEmpty(prefix))
            {
                buffer.Add(prefix);
            }
            
            buffer.AddFormat((float)f, format);

            if (!string.IsNullOrEmpty(units) || 
                !string.IsNullOrEmpty(suffix))
            {
                buffer.Open(TextStyle.Number_Units);
                
                if (!string.IsNullOrEmpty(units))
                    buffer.Add(units);
                if (!string.IsNullOrEmpty(suffix))
                    buffer.Add(suffix);
                
                buffer.Close(TextStyle.Number_Units);
            }

            style?.Close(buffer, true, true);
            
            return buffer;
        }

        #endregion
        
        public static ArcenCharacterBufferBase AddModuleTag( this ArcenCharacterBufferBase buffer, bool if_true )
        {
            if (if_true)
            {
                buffer.Add(TextStyle.Get("ModuleTag"));
            }
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase AddModuleTag( this ArcenCharacterBufferBase buffer, EntitySystem system )
        {
            if (system?.TypeData?.IsModule??false)
            {
                buffer.Add(TextStyle.Get("ModuleTag"));
            }
            
            return buffer;
        }
        
        #endregion

        #region Squad
        
        public static string GetDisplayName( this GameEntity_Base squad, bool short_name=false)
        {
            var type = squad?.TypeData;
            if (type == null)
                return string.Empty;

            if (short_name)
                return type.GetShortDisplayName();

            return type.GetDisplayName();
        }

        /*
        public static string EntityType_DisplayName( this GameEntity_Squad squad )
        {
            if ( !string.IsNullOrEmpty( squad.TypeData.NameForCityCenter ) )
                return squad.TypeData.NameForCityCenter;

            if ( squad.TypeData.IsMobile ) return "ship";

            if ( squad.TypeData.IsCommandStation ) return "station";

            return "structure";
        }
        */
        
        public static string SocketName( this GameEntity_Squad squad, int count )
        {
            if (count > 1)
                return squad.TypeData.NameForCitySockets_Short_Plural;
            
            return squad.TypeData.NameForCitySockets_Short_Singular;
        }
        
        public static string CityName_FromSocketName( this GameEntity_Squad squad )
        {
            if ( !string.IsNullOrEmpty( squad.TypeData.NameForCityCenter ) )
                return squad.TypeData.NameForCityCenter;

            var socket_name = squad.TypeData.NameForCitySockets_Singular;
            if ( !string.IsNullOrEmpty( socket_name ) )
            {
                foreach (var type in GameEntityTypeDataTable.Instance.RowsBySpecialType[SpecialEntityType.CityCenter])
                {
                    if (type.NameForCitySockets_Singular == socket_name &&
                        !string.IsNullOrEmpty(type.NameForCityCenter))
                    {
                        return type.NameForCityCenter;
                    }
                }
            }
            
            return null;
        }
        
        public static ArcenCharacterBufferBase StartMark(this ArcenCharacterBufferBase buffer, int mark)
        {
            var row = Balance_MarkLevelTable.Instance.RowsByOrdinal[mark];
            buffer.StartColor(row.ColorHex);
            return buffer;
        }
        
        public static ArcenCharacterBufferBase EndMark(this ArcenCharacterBufferBase buffer)
        {
            //var row = Balance_MarkLevelTable.Instance.RowsByOrdinal[mark];
            buffer.EndColor();//(row.ColorHex);
            return buffer;
        }
        
        public static bool GetDps( this GameEntity_Squad squad, out FInt min, out FInt max )
        {
            min = FInt.Zero;
            max = FInt.Zero;
            for (var i = 0; i < squad.Systems.Count; i++)
            {
                var sys = squad.Systems[i];
                if (!squad.IsFakeEntity)
                {
                    if (sys.ComputeDisabledReason(ArcenRejectionReason.Unknown, false, false) != ArcenRejectionReason.Unknown)
                        continue;
                }
                
                var type = sys.TypeData;
                if (type.Category != EntitySystemCategory.Weapon)
                    continue;
                if (type.IsModule && squad.IsFakeEntity)
                    continue;
                
                min += sys.GetSalvoDps_Min();
                max += sys.GetSalvoDps_Max();
            }
            
            return min != FInt.Zero || max != FInt.Zero;
        }

        #endregion

        #region TimeToColor
        public static Color TimeToColor(this int sec, bool hueflip=false)
        {
            return TimeToColor((float)sec, hueflip);
        }
        
        public static Color TimeToColor(this float sec, bool hueflip=false)
        {
            const float h = 0;
            const float s = 100;
            const float v = 100;
            HSVColor hsv = new HSVColor(h/360.0f,s/100.0f,v/100.0f);
            if (hueflip)
                hsv.h = ((float)sec).RescaleClamp(0, 60, 55, 0) / 360.0f;
            else
                hsv.h = ((float)sec).RescaleClamp(0, 60, 0, 55) / 360.0f;
            hsv.s = ((float)sec).RescaleClamp(60, 200, 1.0f, 0.0f);
            hsv.v = ((float)sec).RescaleClamp(60, 200, 1.0f, 0.7f);
            return hsv.ToRGB();
        }
        #endregion
        
        #region Unused
        /*
        public static ArcenCharacterBufferBase Add<T>( this ArcenCharacterBufferBase buffer, TextTerm term, T value, EntityText.Config config )
        {
            var format = EntityText.GetFormatForTerm( term );

            buffer.StartColor( format.Color );
            if ( config.Detail < TooltipDetail.Medium )
                buffer.Add( format.SpriteInDefaultColor, TextStyle.TextTerm_Sprite_Sizing );
            else
                buffer.Add( format.Name );
            buffer.EndColor();

            return buffer;
        }
        */
        #endregion
    }
}