using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Arcen.AIW2.Core;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.AIW2.External
{
    #region Support Types
    
    public enum WriterToUse
    {
        Original,
        Experimental,
        Formatted
    }
        
    public enum WeaponActivityDetail
    {
        Nothing = 0,
        ReloadTime = 1,
        LastDamage = 2,
        Both = 3
    }
    
    public enum TextTerm
    {
        Null,
        
        Strength,
        EHP,
        DPS,
        Hull,
        Shields,
        Speed,
        MaxHull,

        Engine_gX,
        Mass_tX,
        Albedo,
        Armor_mm,
        
        Mark,
        
        AIP,
        Science,
        Hacking,
        Essence,
        Cuendillar,
        
        Metal,
        Energy,
        FuelArgon,
        FuelRadon,
        FuelXenon,
        
        Range,
        TimeOnPlanet,
        Damage,
        Damage_Exotic,
        Damage_Corrosive,
        Damage_Ion,

        System_Weapon,
        System_Stat,
        System_Other,
        
        Shots,
        Reload,

        Length,
    }

    public enum TermUse
    {
        Null        = 0,
        
        // this exists only to distinguish it from Null
        // .. it is implied all uses include color
        Color       = 1 << 0,
        Icon        = 1 << 1,
        Name        = 1 << 2,
        Abbr        = 1 << 3,
        IconAfter   = 1 << 4,
        
        Name_Abbr       = Name | Abbr,
        Icon_Name       = Icon | Name,
        Icon_Abbr       = Icon | Abbr,
        Icon_Name_Abbr  = Icon | Name | Abbr,
    }
    
    public enum TermCmp
    {
        Null = 0,
        Equals,
        NotEq,
        Greater,
        GreaterEq,
        Less,
        LessEq,
    }
    
    public struct TermRange
    {
        public TextTerm Term;
        public TermCmp Cmp;
        public float Val;
        
        public static TermRange Alloc(TextTerm term, TermCmp cmp, float val)
        {
            return new TermRange()
            {
                Term = term,
                Cmp = cmp,
                Val = val,
            };
        }
    }

    public sealed class EndStatementStyle
    {
        public readonly string value;
        public readonly bool pad;

        private EndStatementStyle(string _val, bool _pad)
        {
            value = _val;
            pad = _pad;
        }
        
        public static readonly EndStatementStyle Normal = new EndStatementStyle(".", false);
        public static readonly EndStatementStyle SpaceSpace = new EndStatementStyle("  ", false);
        public static readonly EndStatementStyle EOL = new EndStatementStyle(".", true);
        public static readonly EndStatementStyle Pad = new EndStatementStyle(null, true);
    }
    
    // Note this order is significant
    // ...it is the descending sort order for some ties
    public enum ShipIconStatus
    {
        Alive = 0,
        Crippled,
        Remains,
        BeingClaimed,
        UnderConstruction,
        InGuardPost,
        BeingTransported,
        LoadedDrone,

        Length
    }

    public struct ShipsOfStatus
    {
        public int Count;
        public ShipIconStatus Status;
    }

    public struct ShipsOfStatusCollection : IEnumerable<ShipsOfStatus>
    {
        public ShipsOfStatus Alive;
        public ShipsOfStatus Crippled;
        public ShipsOfStatus Remains;
        public ShipsOfStatus BeingClaimed;
        public ShipsOfStatus UnderConstruction;
        public ShipsOfStatus InGuardPost;
        public ShipsOfStatus BeingTransported;
        public ShipsOfStatus LoadedDrone;
        
        public static ShipsOfStatusCollection operator + (ShipsOfStatusCollection a, ShipsOfStatusCollection b)
        {
            a.Alive.Count += b.Alive.Count;
            a.Crippled.Count += b.Crippled.Count;
            a.Remains.Count += b.Remains.Count;
            a.BeingClaimed.Count += b.BeingClaimed.Count;
            a.UnderConstruction.Count += b.UnderConstruction.Count;
            a.InGuardPost.Count += b.InGuardPost.Count;
            a.BeingTransported.Count += b.BeingTransported.Count;
            a.LoadedDrone.Count += b.LoadedDrone.Count;
            
            return a;
        } 

        public int this[ ShipIconStatus status ]
        {
            get
            {
                if ( status == ShipIconStatus.Alive )
                    return Alive.Count;
                if ( status == ShipIconStatus.Crippled )
                    return Crippled.Count;
                if ( status == ShipIconStatus.Remains )
                    return Remains.Count;
                if ( status == ShipIconStatus.BeingClaimed )
                    return BeingClaimed.Count;
                if ( status == ShipIconStatus.UnderConstruction )
                    return UnderConstruction.Count;
                if ( status == ShipIconStatus.InGuardPost )
                    return InGuardPost.Count;
                if ( status == ShipIconStatus.BeingTransported )
                    return BeingTransported.Count;
                if ( status == ShipIconStatus.LoadedDrone ) return LoadedDrone.Count;

                throw new IndexOutOfRangeException( string.Format( "index {0} is not valid", status ) );
            }

            set
            {
                if ( status == ShipIconStatus.Alive )
                    Alive.Count = value;
                else if ( status == ShipIconStatus.Crippled )
                    Crippled.Count = value;
                else if ( status == ShipIconStatus.Remains )
                    Remains.Count = value;
                else if ( status == ShipIconStatus.BeingClaimed )
                    BeingClaimed.Count = value;
                else if ( status == ShipIconStatus.UnderConstruction )
                    UnderConstruction.Count = value;
                else if ( status == ShipIconStatus.InGuardPost )
                    InGuardPost.Count = value;
                else if ( status == ShipIconStatus.BeingTransported )
                    BeingTransported.Count = value;
                else if ( status == ShipIconStatus.LoadedDrone )
                    LoadedDrone.Count = value;
                else
                    throw new IndexOutOfRangeException( string.Format( "index {0} is not valid", status ) );
            }
        }

        public void Clear()
        {
            Alive.Count = 0;
            Crippled.Count = 0;
            Remains.Count = 0;
            BeingClaimed.Count = 0;
            UnderConstruction.Count = 0;
            InGuardPost.Count = 0;
            BeingTransported.Count = 0;
            LoadedDrone.Count = 0;
        }

        #region Enumerator

        public Enumerator GetEnumerator()
        {
            return new Enumerator( this );
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator( this );
        }

        IEnumerator<ShipsOfStatus> IEnumerable<ShipsOfStatus>.GetEnumerator()
        {
            return new Enumerator( this );
        }

        public struct Enumerator : IEnumerator<ShipsOfStatus>
        {
            private readonly ShipsOfStatusCollection _collection;
            private int _cur;

            internal Enumerator( ShipsOfStatusCollection collection )
            {
                _collection = collection;
                _cur = -1;
            }

            object IEnumerator.Current => Current;

            public ShipsOfStatus Current
            {
                get
                {
                    if ( _cur < 0 )
                        throw new InvalidOperationException();

                    if ( _cur == 0 )
                        return _collection.Alive;
                    if ( _cur == 1 )
                        return _collection.Crippled;
                    if ( _cur == 2 )
                        return _collection.Remains;
                    if ( _cur == 3 )
                        return _collection.BeingClaimed;
                    if ( _cur == 4 )
                        return _collection.UnderConstruction;
                    if ( _cur == 5 )
                        return _collection.InGuardPost;
                    if ( _cur == 6 )
                        return _collection.BeingTransported;
                    if ( _cur == 7 )
                        return _collection.LoadedDrone;

                    throw new InvalidOperationException();
                }
            }

            public void Reset()
            {
                _cur = -1;
            }

            public bool MoveNext()
            {
                _cur++;

                return _cur < (int) ShipIconStatus.Length;
            }

            public void Dispose()
            {
            }
        }

        #endregion
    }

    #endregion

    public static partial class EntityText
    {
        #region Config
        public struct Config
        {
            // from settings
            public bool UseIcons;
            public bool UseText;
            public bool UseFullNames;
            public bool UseShipIconAndName;
            public WeaponActivityDetail WeaponDetail;
            public bool ShowAbortCode;
            public bool ShowingCounters;
            public bool ShowTargetInfo;
            public int MaxDisplayedTargets;
            public bool ShowDebugInfo;
            public bool ShowEntityId;
            public bool ShowFleetId;
            public bool ShowLocationCoords;
            public bool ShowMetalCost;
            public bool ShowMetalOther;
            public bool ShowAnyFuel;
            public bool ShowEnergyProduction;
            public bool ShowEnergyConsumption;
            public bool ShowFireteamHistory;
            //public bool ShowIndividualFleetMemberships;
            //public int FleetMemberSortMode;
            public bool ShowTextRegions;

            // auto assigned
            public Faction LocalPlayerFaction;

            // assigned after
            // generally, arguments to the GetText call
            public float CpiBaseOffset;
            public HackingType ActiveHackAgainstUs;
            public GameEntity_Squad ActiveHackerAgainstUs;
            public Fleet OptFleet;
            public FleetMembership OptMembership;
            public int OptShipCount;
            public TooltipDetail Detail;
            public ShipExtraDetailFlags ExtraFlags;
            public FromSidebarType From;
            public Window_InGameHoverEntityInfo.Mode PanelMode;
            public bool InPopup;
            public bool ForMultipleShips;
            public string OptNameForOwner;
            public string OptColorForOwner;

            #region Unused

            /*
            public struct SquadInfo
            {
                public bool CanBeClaimed;
            }
             
            // vars precomp for squad
            public void SetSquad(GameEntity_Squad e)
            {
                var owningFactionOrNull = e.GetFactionOrNull_Safe();
                //var entityCanBeClaimed = e.HasNotYetBeenFullyClaimed && (owningFactionOrNull == null || owningFactionOrNull.Type == FactionType.NaturalObject || owningFactionOrNull.Type == FactionType.Player);
                //var isUnderConstruction = Squad.SelfBuildingMetalRemaining > FInt.Zero;
                //var isCenterpiece = Squad.FleetMembership?.Fleet?.Centerpiece.GetSquad() == Squad;
            
                #region Vars
                int hullMax;
                int shieldMax;
                int hullCurr;
                int shieldCurr;
                FInt hullPercent;
                FInt shieldPercent;
                int metalCurr;
                int energyCurr;
                int storedMetal = int.MaxValue;
                int storedEnergy = int.MaxValue;
                int storedArgon = int.MaxValue;
                int storedRadon = int.MaxValue;
                int storedXenon = int.MaxValue;

                /*
                debugstage = 1805;
                hullCurr = e.GetCurrentHullPoints();
                shieldCurr = e.GetCurrentShieldPoints();
                if ( this.InPopup )
                {
                    hullMax = e.DataForMark.BaseHullPoints;
                    shieldMax = e.DataForMark.BaseShieldPoints;
                }
                else
                {
                    hullMax = e.GetMaxHullPoints();
                    shieldMax = e.GetMaxShieldPoints();
                }

                hullPercent = FInt.Create( hullCurr, true ).ToPercent( hullMax );
                shieldPercent = FInt.Create( shieldCurr, true ).ToPercent( shieldMax );
                if ( entityCanBeClaimed )
                    metalCurr = e.DataForMark.MetalCostToClaim;
                else
                    metalCurr = e.GetMetalCost();

                energyCurr = e.GetEnergyUsage();
                
                if ( localPlayerFaction != null && (entityCanBeClaimed || localPlayerFaction.NetEnergy < 0 || panelMode == Window_InGameHoverEntityInfo.Mode.Build) )
                {
                    storedMetal = localPlayerFaction.StoredMetal.IntValue;
                    storedEnergy = localPlayerFaction.NetEnergy;
                    storedArgon = localPlayerFaction.NetFuelArgon;
                    storedRadon = localPlayerFaction.NetFuelRadon;
                    storedXenon = localPlayerFaction.NetFuelXenon;
                }
                    
                int strengthCurr = e.GetStrengthPerSquad( false );
                int strengthTotal = 0;
                string strengthMode = null;
                string strengthModeColorHex = null;
                if ( !e.IsFakeEntity && !this.InPopup )
                {
                    if ( this.OptShipCount > 1 ) 
                    {
                        strengthTotal = strengthCurr;
                        strengthCurr *= this.OptShipCount;
                        strengthMode = "1x";
                        strengthModeColorHex = "ffffff";
                    } 
                    else 
                    if ( this.OptShipCount > 1 )
                    {
                        strengthTotal = strengthCurr;
                        strengthCurr *= e.ShipCount;
                        strengthMode = "1x";
                        strengthModeColorHex = "ffffff";
                    } 
                    else
                    {
                        strengthTotal = e.GetStrengthOfContentsIfAny();
                        if ( strengthTotal > 0 )
                        {
                            strengthMode = "+T";
                            strengthModeColorHex = ColorMath.IceBlue.GetHexCode();
                        }
                    }
                }
                #endregion
            }
            */

            #endregion
        }

        public static WriterToUse Use
        {
            get
            {
                var use = (WriterToUse)GameSettings.Current.GetIntBySetting( "UseTooltipWriter" );
                if (use == WriterToUse.Formatted)
                    ArcenExternalUIUtilities.UsedMagnitudeFormating = ArcenExternalUIUtilities.Magnitudes_Lowered;
                else
                    ArcenExternalUIUtilities.UsedMagnitudeFormating = ArcenExternalUIUtilities.Magnitudes;
                
                return use;
            }
        }

        public enum DebugAction
        {
            Null,
            Dump,
            Dump_Truncate,
        }
        public static DebugAction DumpNextTooltipText;

        //private static float _SetupTime;
        private static Config _Setup;

        public static TooltipDetail Detail
        {
            get
            {
                TooltipDetail detail = TooltipDetail.SuperShort;
                if ( GameSettings.Current.GetBoolBySetting( "ShowShipAndPlanetDetailModeByDefault" ) )
                    detail = TooltipDetail.Full;
                else if ( GameSettings.Current.GetBoolBySetting( "ShowShipAndPlanetMediumModeByDefault" ) )
                    detail = TooltipDetail.Medium;
                
                if ( detail < TooltipDetail.Full &&
                     InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1() )
                {
                    detail += 1;
                }
                
                if ( detail < TooltipDetail.Full &&
                     InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key2() )
                {
                    detail += 1;
                }
                
                return detail;
            }
        }

        public static string GetTooltipScaleCurrentlyShown()
        {
            if (Window_InGameHoverEntityInfo.Instance.GetShouldDrawThisFrame() ||
                Window_InGameHoverPlanetInfo.Instance.GetShouldDrawThisFrame())
            {
                return "ShipTooltipScale";
            }
            
            return "GeneralTooltipScale";
        }

        public static int GetNeededTooltipWidth()
        {
            if (Use == WriterToUse.Original)
            {
                if (EntityText.Detail == TooltipDetail.Full)
                {
                    return 820;
                }
                
                return 700;
            }
            else
            if (Use == WriterToUse.Experimental)
            {
                bool useIcons = true;
                switch ( EntityText.Detail )
                {
                    case TooltipDetail.SuperShortBecauseShowingOtherStuff:
                    case TooltipDetail.SuperShort:
                        useIcons = GameSettings.Current.GetBoolBySetting( "Tooltip_UseIcons_Short" );
                        break;
                    case TooltipDetail.Medium:
                        useIcons = GameSettings.Current.GetBoolBySetting( "Tooltip_UseIcons_Medium" );
                        break;
                    case TooltipDetail.Full:
                        useIcons = GameSettings.Current.GetBoolBySetting( "Tooltip_UseIcons_Full" );
                        break;
                }
                
                if (!useIcons)
                {
                    return 820;
                }
                
                return 700;
            }
            else
            if (Use == WriterToUse.Formatted)
            {
                return 600;
            }
            
            return 700;
        }
        
        public static Config Setup
        {
            get
            {
                var debugstage = 0;
                var setup = new Config();
                try
                {
                    debugstage = 100;
                    //float elapsed = ArcenTime.TimeSinceStartF - _SetupTime;
                    //if (elapsed > 0.2f)
                    {
                        //_SetupTime = ArcenTime.TimeSinceStartF;

                        debugstage = 110;
                        setup.ShowingCounters = InputCaching.CalculateHoldToSeeShipStrengthsAndWeaknesses();

                        debugstage = 120;
                        setup.Detail = TooltipDetail.SuperShort;
                        if ( setup.ShowingCounters )
                            setup.Detail = TooltipDetail.SuperShortBecauseShowingOtherStuff;
                        else if ( GameSettings.Current.GetBoolBySetting( "ShowShipAndPlanetDetailModeByDefault" ) )
                            setup.Detail = TooltipDetail.Full;
                        else if ( GameSettings.Current.GetBoolBySetting( "ShowShipAndPlanetMediumModeByDefault" ) )
                            setup.Detail = TooltipDetail.Medium;

                        debugstage = 130;
                        if ( setup.Detail < TooltipDetail.Full &&
                             InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key1() )
                        {
                            setup.Detail += 1;
                        }
                        if ( setup.Detail < TooltipDetail.Full &&
                             InputCaching.CalculateShouldIncreaseShipAndPlanetTooltipDetailBy1_Key2() )
                        {
                            setup.Detail += 1;
                        }

                        debugstage = 140;
                        switch ( setup.Detail )
                        {
                            case TooltipDetail.SuperShortBecauseShowingOtherStuff:
                            case TooltipDetail.SuperShort:
                                setup.UseIcons = GameSettings.Current.GetBoolBySetting( "Tooltip_UseIcons_Short" );
                                break;
                            case TooltipDetail.Medium:
                                setup.UseIcons = GameSettings.Current.GetBoolBySetting( "Tooltip_UseIcons_Medium" );
                                break;
                            case TooltipDetail.Full:
                                setup.UseIcons = GameSettings.Current.GetBoolBySetting( "Tooltip_UseIcons_Full" );
                                break;
                            default:
                                throw new Exception( "Error: Unimplemented TooltipDetail mode: " + setup.Detail );
                        }

                        debugstage = 150;
                        setup.UseText = !setup.UseIcons;

                        debugstage = 160;
                        setup.UseFullNames = setup.Detail == TooltipDetail.Full;
                        setup.UseShipIconAndName = setup.Detail == TooltipDetail.Full &&
                                                   GameSettings.Current.GetBoolBySetting( "Tooltip_ShipNames_Full" );

                        debugstage = 170;
                        setup.WeaponDetail = (WeaponActivityDetail) GameSettings.Current.GetIntBySetting( "Tooltip_WeaponActivityDetails" );
                        setup.ShowAbortCode = GameSettings.Current.GetBoolBySetting( "Debug_WeaponAbortCode" );
                        setup.ShowTargetInfo = GameSettings.Current.GetBoolBySetting( "Debug_WeaponTargetInfo" );
                        setup.MaxDisplayedTargets = GameSettings.Current.GetIntBySetting( "Debug_WeaponTargetInfoCount" );

                        debugstage = 180;
                        setup.ShowTextRegions = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip_TextRegions" );
                        setup.ShowDebugInfo = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" );
                        setup.ShowEntityId = GameSettings.Current.GetBoolBySetting( "ShowEntityIDInHovertext" );
                        setup.ShowFleetId = GameSettings.Current.GetBoolBySetting( "Debug_WriteFleetIDInTooltips" );
                        setup.ShowLocationCoords = GameSettings.Current.GetBoolBySetting( "ShowEntityLocationInHovertext" );

                        setup.ShowFireteamHistory = GameSettings.Current.GetBoolBySetting( "ShowFireteamHistory" );
                        //setup.ShowIndividualFleetMemberships = GameSettings.Current.GetBoolBySetting( "Tooltip_ShowIndividualFleetMemberships" );
                        //setup.FleetMemberSortMode = GameSettings.Current.GetIntBySetting( "Tooltip_FleetMembershipSortPriority" );

                        setup.LocalPlayerFaction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();

                        debugstage = 190;

                        _Setup = setup;
                    }
                }
                catch ( Exception e )
                {
                    LOG.Err( "error at debugstage {0}\n{1}", debugstage, e );
                }

                return _Setup;
            }
        }
        #endregion
        
        #region GetTooltip
        
        public static bool GetTooltip( 
            ArcenCharacterBufferBase buffer, 
            GameEntity_Squad relatedSquadOrNull,
            FleetMembership MembershipBase,
            GameEntityTypeData TypeDataOrNull,
            Fleet FleetToUseOrNull, 
            string AltTextColorIfUsed,
            string AltTextInPlaceOfFleetAndOwnerOrBlank,
            int OptionalCountToShow,
            Faction ForFactionOrNull, 
            byte OptionalForMarkLevel, 
            FromSidebarType IsFromSidebarType,
            ShipExtraDetailFlags DetailFlags, 
            float PositionScaleMultiplier,
            bool IsBeingDrawnInPopupWindowRatherThanTooltip )
        {
            bool? restore = null;
            if ((DetailFlags & ShipExtraDetailFlags.PlainText) > 0)
            {
                restore = buffer.WriteAsTextFileOutput;
                buffer.WriteAsTextFileOutput = true;
            }
            
            try
            {
                if ( Use == WriterToUse.Original )
                {
                    return Window_InGameHoverEntityInfo._GetTextForEntity(
                        buffer, 
                        relatedSquadOrNull,
                        MembershipBase,
                        TypeDataOrNull,
                        FleetToUseOrNull,
                        AltTextColorIfUsed,
                        AltTextInPlaceOfFleetAndOwnerOrBlank,
                        OptionalCountToShow,
                        ForFactionOrNull,
                        OptionalForMarkLevel, IsFromSidebarType, DetailFlags, PositionScaleMultiplier,
                        IsBeingDrawnInPopupWindowRatherThanTooltip );
                }
                
                if ( Use == WriterToUse.Experimental )
                {
                    return Window_PrototypeInGameHoverEntityInfo._GetTextForEntity(
                        buffer, 
                        relatedSquadOrNull,
                        MembershipBase,
                        TypeDataOrNull,
                        FleetToUseOrNull,
                        AltTextColorIfUsed,
                        AltTextInPlaceOfFleetAndOwnerOrBlank,
                        OptionalCountToShow,
                        ForFactionOrNull,
                        OptionalForMarkLevel, IsFromSidebarType, DetailFlags, PositionScaleMultiplier,
                        IsBeingDrawnInPopupWindowRatherThanTooltip );
                }

                if ( Use == WriterToUse.Formatted )
                {
                    using ( var writer = EntityTextWriter.Get( buffer ) )
                    {
                        return writer.Write( buffer,
                            relatedSquadOrNull,
                            MembershipBase,
                            TypeDataOrNull,
                            FleetToUseOrNull,
                            AltTextColorIfUsed,
                            AltTextInPlaceOfFleetAndOwnerOrBlank,
                            OptionalCountToShow,
                            ForFactionOrNull,
                            OptionalForMarkLevel,
                            IsFromSidebarType,
                            DetailFlags,
                            PositionScaleMultiplier,
                            IsBeingDrawnInPopupWindowRatherThanTooltip );
                    }
                }
            }
            finally
            {
                if (DumpNextTooltipText != DebugAction.Null)
                {
                    var log = Log.Yes;
                    log.Dump(buffer);
                    DumpNextTooltipText = DebugAction.Null;
                }
                else
                {
                    buffer.Builder.ValidateTags(false);
                }

                if (restore != null)
                    buffer.WriteAsTextFileOutput = restore.Value;
                
                if ((DetailFlags & ShipExtraDetailFlags.PlainText) > 0)
                    buffer.RemoveAllTags();
            }
            
            return false;
        }

        public static bool GetTooltip( 
            ArcenCharacterBufferBase buffer, 
            GameEntity_Squad relatedSquadOrNull,
            FleetMembership MembershipBase,
            GameEntityTypeData TypeDataOrNull, 
            int OptionalCountToShow,
            Faction ForFactionOrNull, 
            byte OptionalForMarkLevel, 
            FromSidebarType IsFromSidebarType,
            ShipExtraDetailFlags DetailFlags, 
            float PositionScaleMultiplier,
            bool IsBeingDrawnInPopupWindowRatherThanTooltip )
        {
            bool? restore = null;
            if ((DetailFlags & ShipExtraDetailFlags.PlainText) > 0)
            {
                restore = buffer.WriteAsTextFileOutput;
                buffer.WriteAsTextFileOutput = true;
            }
            
            try
            {
                if ( Use == WriterToUse.Original )
                {
                    return Window_InGameHoverEntityInfo._GetTextForEntity( 
                        buffer, 
                        relatedSquadOrNull, 
                        MembershipBase,
                        TypeDataOrNull, 
                        OptionalCountToShow, 
                        ForFactionOrNull,
                        OptionalForMarkLevel, 
                        IsFromSidebarType, 
                        DetailFlags, 
                        PositionScaleMultiplier,
                        IsBeingDrawnInPopupWindowRatherThanTooltip );
                }
                
                if ( Use == WriterToUse.Experimental )
                {
                    return Window_PrototypeInGameHoverEntityInfo._GetTextForEntity( 
                        buffer, 
                        relatedSquadOrNull,
                        MembershipBase, 
                        TypeDataOrNull, 
                        OptionalCountToShow, 
                        ForFactionOrNull,
                        OptionalForMarkLevel, 
                        IsFromSidebarType, 
                        DetailFlags, 
                        PositionScaleMultiplier,
                        IsBeingDrawnInPopupWindowRatherThanTooltip );
                }

                if ( Use == WriterToUse.Formatted )
                {
                    using (var writer = EntityTextWriter.Get(buffer))
                    {
                        return writer.Write(
                            buffer,
                            relatedSquadOrNull,
                            MembershipBase,
                            TypeDataOrNull,
                            null,
                            "ffffff",
                            string.Empty,
                            OptionalCountToShow,
                            ForFactionOrNull,
                            OptionalForMarkLevel,
                            IsFromSidebarType,
                            DetailFlags,
                            PositionScaleMultiplier,
                            IsBeingDrawnInPopupWindowRatherThanTooltip );
                    }
                }
            }
            finally
            {
                if (DumpNextTooltipText != DebugAction.Null)
                {
                    buffer.Dump(Log.Yes);
                    DumpNextTooltipText = DebugAction.Null;
                }
                else
                {
                    buffer.Builder.ValidateTags(false);
                }
                                        
                if (restore != null)
                    buffer.WriteAsTextFileOutput = restore.Value;
                
                if ((DetailFlags & ShipExtraDetailFlags.PlainText) > 0)
                    buffer.RemoveAllTags();
            }

            return false;
        }

        #endregion
        
        #region WriteSystem
        
        public static void WriteSystem( ArcenDoubleCharacterBuffer buffer, EntitySystem system, Config config )
        {
            if (Use == WriterToUse.Original)
            {
                Window_InGameHoverEntityInfo._WriteSystemInfo( 
                    buffer, 
                    system.ParentEntity.TypeData.SystemTypes.IndexOf( system.TypeData ),
                    system.ParentEntity,
                    system.ParentEntity.FleetMembership,
                    null, null, 
                    system.ParentEntity.TypeData, 
                    system.ParentEntity.FleetMembership.EffectiveMark, 
                    false, false, TooltipDetail.Full, true, false );
            }
            else
            if (Use == WriterToUse.Experimental)
            {
                Window_PrototypeInGameHoverEntityInfo.WriteSystemInfo( 
                    buffer, 
                    system.ParentEntity.TypeData.SystemTypes.IndexOf( system.TypeData ),
                    system.ParentEntity,
                    system.ParentEntity.FleetMembership,
                    null, null,
                    system.ParentEntity.TypeData, 
                    system.ParentEntity.FleetMembership.EffectiveMark, 
                    false, false, TooltipDetail.Full, true, 0, false, false, 0, true, true );
            }
            else
            if (Use == WriterToUse.Formatted)
            {
                using (var writer = EntityTextWriter.Get(buffer))
                {
                    writer.Squad = system.ParentEntity;
                    writer.WriteSystem(system);
                }
            }
        }

        #endregion

        #region GetHasContentsToView
        public static string GetHasContentsToView(GameEntity_Squad squad)
        {
            if ( squad == null )
                return null;

            if ( squad.TypeData.HackableForCommandStationsAndBattleStations_DSSStyle )
            {
                if ( squad.ShipGrantsList != null && squad.ShipGrantsList.Count > 0 )
                    return "these choices";
            }
            
            if ( squad.TypeData.GrantsStuffToBeAddedToPlayerFleets )
            {
                if ( squad.ShipGrantsList != null && squad.ShipGrantsList.Count > 0 )
                    return "these choices";
            }
            
            if ( squad.TypeData.IsFleetLeader )
            {
                return "this fleet";
            }
            
            if ( squad.AIReinforcementPointContents != null && squad.AIReinforcementPointContents.Count > 0 )
            {
                return "the contents";
            }
            
            if (squad.TypeData.GetHasTag( OutguardBeaconStateForPlanet.OutguardBeaconTag))
            {
                return "these choices";
            }
            
            return null;
        }

        #endregion

        #region ShowContents
        
        public static void ShowContents(GameEntity_Squad obj)
        {
            if ( obj == null )
                return;

            if ( obj.TypeData.IsFleetLeader && 
                 !obj.IsFakeEntity &&
                 obj.PlanetFaction != null && 
                 obj.GetIsLocalFaction_Safe() )
            {
                //if we C-clicked the fleet leader of a squad we own, then open the fleet panel instead.
                Window_FleetManagementSidebarPopout.Instance.Open( obj.GetFleetOrNull_Safe() );
                
                return;
            }

            EntityText.ShowDetails(
                    0.25f, 2f,
                    "Details of Ship Line(s)", "Close",
                    (b)=>
                    {
                        EntityText.GetContents(b, obj);
                        return true;
                    } );
        }
        
        public static void ShowContents(OutguardInfo obj)
        {
            EntityText.ShowDetails(
                    0.25f, 2f,
                    "Details of Outguard Group", "Close",
                    (b)=>
                    {
                        EntityText.GetContents(b, obj);
                        return true;
                    } );
        }

        public static void ShowDetails( 
            float SecondsBetweenUpdates, float SecondsBeforeUpdateToNothing, 
            string Title, string Button, 
            Window_ModalSelfUpdatingTextWindowBase.UpdaterDelegate Update )
        {
            if (Use == WriterToUse.Original || Use == WriterToUse.Experimental)
            {
                Window_ModalSelfUpdatingTextWindow_Wide.Instance.Open( 
                        SecondsBetweenUpdates, SecondsBeforeUpdateToNothing, 
                        Title, 
                        Button,
                        (b)=>
                        {
                            Update(b);
                            return true;
                        });
                
                return;
            }
            
            Window_ModalSelfUpdatingTextWindow.Instance.Open( 
                        SecondsBetweenUpdates, SecondsBeforeUpdateToNothing, 
                        Title, 
                        Button,
                        (b)=>
                        {
                            Update(b);
                            return true;
                        });
        }

        public static void GetContents(ArcenCharacterBufferBase buffer, GameEntity_Squad obj)
        {
            if (Use == WriterToUse.Original ||
                Use == WriterToUse.Experimental)
            {
                Window_InGameHoverEntityInfo._WriteDetailsOfAllShipContents(buffer, obj);
                return;
            }
            
            if ( Use == WriterToUse.Formatted )
            {
                using (var writer = EntityTextWriter.Get(buffer))
                {
                    writer.Write_Contents(obj);
                }
            }
        }
        
        public static void GetContents(ArcenCharacterBufferBase buffer, OutguardInfo obj)
        {
            if (Use == WriterToUse.Original ||
                Use == WriterToUse.Experimental)
            {
                Window_InGameHoverEntityInfo._WriteDetailsOfAnOutguardGroupContents(buffer, obj);
                return;
            }
            
            if ( Use == WriterToUse.Formatted )
            {
                using (var writer = EntityTextWriter.Get(buffer))
                {
                    writer.Write_Contents(obj);
                }
            }
        }
        
        #endregion
        
        #region Write_Tooltip_Hotkeys_Footer

        public static void Write_Tooltip_Hotkeys_Footer( 
            ArcenCharacterBufferBase buffer,
            bool ShowSuppressionText,
            bool ShowDetailLevelText,
            string ShowClickForMoreInfoText )
        {
            if (Use == WriterToUse.Original ||
                Use == WriterToUse.Experimental)
            {
                Window_InGameHoverEntityInfo._WriteTooltipDetailHotkeysFooter(buffer, ShowSuppressionText, ShowDetailLevelText, ShowClickForMoreInfoText);
                return;
            }
            
            var detailLevel = EntityText.Detail;

            if (detailLevel == TooltipDetail.SuperShortBecauseShowingOtherStuff)
                return;

            void AppendVar(string _name, TextStyle _style, ArcenCharacterBufferBase _buffer, object args)
            {
                var detail = EntityText.Detail;
                
                if (_name == "Detail")
                {
                    if (ShowDetailLevelText)
                    {
                        if (detail == TooltipDetail.SuperShort)
                            _buffer.AddColor("BRIEF", "#7486d1");
                        else
                        if (detail == TooltipDetail.Medium)
                            _buffer.AddColor("MEDIUM", "#dd9f45");
                        else
                        if (detail == TooltipDetail.Full)
                            _buffer.AddColor("DETAILED", "#dd44eb");
                    }
                    
                    return;
                }
                
                if (_name == "ToSeeContents")
                {
                    if ( !string.IsNullOrWhiteSpace( ShowClickForMoreInfoText ) )
                    {
                        var key = InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "HoldAndClickToViewDetailsOfContents" );
                        _buffer
                            .Add("查看 ")
                            .Add( ShowClickForMoreInfoText, TextStyle.Brighter )
                            .Add("按住 ")
                            .AddVarReplace(TextVarMap.InputAction, key)
                            .Add("+")
                            .AddVarReplace(TextVarMap.InputAction, "Click")
                            .Add("。");
                    }
                            
                    return;
                }
                
                if (_name == "ToIncreaseDetail")
                {
                    if (detail < TooltipDetail.Full && ShowDetailLevelText)
                    {
                        var key1 = InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" );
                        var key2 = InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key2" );
                        buffer
                            .Add("关于").Add("更多详情", TextStyle.Brighter)//.Add(".");
                            .Add("按住一个/两个 ")
                            .AddVarReplace(TextVarMap.InputAction, key1)
                            .Add(" 或 ")
                            .AddVarReplace(TextVarMap.InputAction, key2)
                            .Add("。");
                    }

                    return;
                }
                
                if (_name == "ToHide")
                {
                    
                    return;
                }
                    
                if (_name == "DlcMod")
                {
                    //buffer.AddDlcMod(Source, TextStyle.)
                    return;
                }
            }
            
            buffer.AddVarReplace(TextVarMap.Tooltip_Hotkeys_Footer_Line, AppendVar);
        }

        #endregion
        
        #region Fake Entity

        private static readonly ConcurrentQueue<GameEntity_Squad> _fakes =
            ConcurrentQueue<GameEntity_Squad>.Create_WillNeverBeGCed( "EntityText._fakes" );

        private static readonly ConcurrentQueue<Planet> _fakePlanets =
            ConcurrentQueue<Planet>.Create_WillNeverBeGCed( "EntityText._fakePlanets" );

        public static GameEntity_Squad GetFakeEntity( FleetMembership mem )
        {
            if (mem == null)
                throw new ArgumentNullException("mem");
            
            Faction faction = mem.Fleet.Faction;
            GameEntityTypeData type = mem.TypeData;
            int marklevel = mem.EffectiveMark;
            
            Planet planet = null;
            foreach ( GameEntity_Squad e in mem.Fleet.Entities )
            {
                planet = e.Planet;
                break;
            }
            
            var squad = GetFakeEntity( type, marklevel, faction, planet );
            squad.SetFleetMembership(mem, "Fake");
            
            return squad;
        }

        public static GameEntity_Squad GetFakeEntity( GameEntityTypeData type, int marklevel = -1, Faction faction = null, Planet planet = null, bool log=false )
        {
            if (type == null)
                throw new ArgumentNullException("type");
            
            GameEntity_Squad squad;
            if ( !_fakes.TryDequeue( out squad ) )
            {
                squad = GameEntity_Squad.CreateNew_ForFakeInUITooltipsOnlyDoNotUseThisAnywhereElse();
                squad.SetPlanetFaction( PlanetFaction.CreateFake(), "Fake" );
            }
 
            var p = squad.PlanetFaction;
            p.Planet = planet;
            p.Faction = faction;
            
            squad.SetInfoForFakeInUITooltipsOnlyDoNotUseThisAnywhereElse( type, (byte)marklevel, null, null );
            
            squad.SetPlanetFaction(p, "Fake");
            squad.SetPlanet(planet, "Fake");
            
            if ( type.StartsAtHull > 0 )
            {
                squad.HullPointsLost = squad.GetMaxHullPoints() - type.StartsAtHull;
            }

            if ( type.StartsAtShields >= 0 )
            {
                squad.ShieldPointsLost = squad.GetMaxShieldPoints() - type.StartsAtShields;
            }
            
            if (log)
            {
                LOG.Msg("GetFakeEntity({0}, {1}, {2}, {3}) returning faction={4} planet={5}", 
                    type.InternalName, marklevel, faction.OrNull(), planet.OrNull(),
                    squad.GetFactionOrNull_Safe().OrNull(), squad.Planet.OrNull());
            }

            return squad;
        }

        public static void ReleaseFakeEntity( GameEntity_Squad e )
        {
            if (e == null)
                return;
            
            var p = e.PlanetFaction;
            p.Planet = null;
            p.Faction = null;
            
            e.SetFleetMembership(null, "Fake");
            e.SetPlanet(null, "Fake");
            
            e.HullPointsLost = 0;
            e.ShieldPointsLost = 0;
            
            _fakes.Enqueue( e );
        }

        #endregion

        #region GetFormatForTerm
        
        public static FixedTextFormatingStats GetFormatForTerm( TextTerm term )
        {
            switch ( term )
            {
                case TextTerm.Null:
                    return ArcenExternalUIUtilities.Null;
                case TextTerm.Strength:
                    return ArcenExternalUIUtilities.Strength;
                case TextTerm.EHP:
                    return ArcenExternalUIUtilities.EHP;
                case TextTerm.DPS:
                    return ArcenExternalUIUtilities.DamagePerSecond;
                case TextTerm.AIP:
                    return ArcenExternalUIUtilities.AIP;
                case TextTerm.Hull:
                    return ArcenExternalUIUtilities.Hull;
                case TextTerm.Shields:
                    return ArcenExternalUIUtilities.Shield;
                case TextTerm.Speed:
                    return ArcenExternalUIUtilities.Speed;
                case TextTerm.Engine_gX:
                    return ArcenExternalUIUtilities.Engine;
                case TextTerm.Mass_tX:
                    return ArcenExternalUIUtilities.Mass;
                case TextTerm.Albedo:
                    return ArcenExternalUIUtilities.Albedo;
                case TextTerm.Armor_mm:
                    return ArcenExternalUIUtilities.Armor;
                case TextTerm.Metal:
                    return ArcenExternalUIUtilities.Metal;
                case TextTerm.Energy:
                    return ArcenExternalUIUtilities.Energy;
                case TextTerm.Science:
                    return ArcenExternalUIUtilities.Science;
                case TextTerm.Hacking:
                    return ArcenExternalUIUtilities.Hacking;
                case TextTerm.Essence:
                    return ArcenExternalUIUtilities.Essence;
                case TextTerm.Cuendillar:
                    return ArcenExternalUIUtilities.Cuendillar;
                case TextTerm.FuelArgon:
                    return ArcenExternalUIUtilities.Argon;
                case TextTerm.FuelRadon:
                    return ArcenExternalUIUtilities.Radon;
                case TextTerm.FuelXenon:
                    return ArcenExternalUIUtilities.Xenon;
                case TextTerm.Damage:
                    return ArcenExternalUIUtilities.Damage;
                case TextTerm.Damage_Exotic:
                    return ArcenExternalUIUtilities.ExoticDamage;
                case TextTerm.Damage_Corrosive:
                    return ArcenExternalUIUtilities.CorrosiveDamage;
                case TextTerm.Damage_Ion:
                    return ArcenExternalUIUtilities.IonDamage;
                case TextTerm.Range:
                    return ArcenExternalUIUtilities.Range;
                case TextTerm.TimeOnPlanet:
                    return ArcenExternalUIUtilities.TimeOnPlanet;
                case TextTerm.Shots:
                    return ArcenExternalUIUtilities.Shots;
                case TextTerm.Reload:
                    return ArcenExternalUIUtilities.Reload;
                case TextTerm.Mark:
                    return ArcenExternalUIUtilities.Mark;
                default:
                    throw new Exception( "Error: Could not find format for '" + term + "'" );
            }
        }
        
        #endregion
        
        #region Other, Helpers (todo: move to buffer extension)
        
        public static void AddSingleValueStrengthOnly( ArcenCharacterBufferBase buffer, int Strength )
        {
            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, Strength, true, true );
        }
        
        public static Color GetProportionalStrengthColor(float ratio)
        {
            //returns a color that indicates how much of the total available strength this unit has
            Color lowHealthFleet = ColorMath.FromRGB(  246, 50, 50 );
            Color highHealthFleet = ColorMath.FromRGB(  75, 244, 170 );
            return Color.Lerp(lowHealthFleet, highHealthFleet, ratio);
        }
        
        public static void WriteHullOrShieldsNumber( ArcenCharacterBufferBase buffer, int Number )
        {
            if ( Number >= 1000000 )
                buffer.AddFixedDecimal( ( Number / 1000000f ), 2 ).Add( "m" );
            else if ( Number >= 10000 )
                buffer.Add( (int)System.Math.Round( Number / 1000f ) ).Add( "k" );
            else
                buffer.AddNumberMoreReadable( Number );
        }

        #endregion
    }

    #region DropdownFiller
    public class UseTooltipWriter_DropdownFiller : IDropdownFiller
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

        private readonly Universal.List<IArcenUI_Dropdown_Option> cachedOptions = Universal.List<IArcenUI_Dropdown_Option>.Create_WillNeverBeGCed( 20, "EntityText.DropdownFiller.cachedOptions" );
        public Universal.List<IArcenUI_Dropdown_Option> GetListOfDropdownOptions()
        {
            if ( cachedOptions.Count == 0 )
            {
                cachedOptions.Add( new IntBasedDropdownOption( 0, "Vanilla" ) );
                cachedOptions.Add( new IntBasedDropdownOption( 1, "Experimental" ) );
                cachedOptions.Add( new IntBasedDropdownOption( 2, "Formatted" ) );
            }
            return cachedOptions;
        }
    }
    #endregion
}