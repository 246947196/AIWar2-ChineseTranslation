using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Linq;
using System.Text;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public static class ArcenExternalUIUtilities
    {
        #region FixedTextFormatingStats
        public static readonly FixedTextFormatingStats Null = new FixedTextFormatingStats( "", null, null, null);
        
        public const string SpaceAfterIcon = "<space=0.1em>";
        
        public static readonly FixedTextFormatingStats Metal = new FixedTextFormatingStats( "金属", "ccccee", "Res_Metal", SpaceAfterIcon );
        public static readonly FixedTextFormatingStats Energy = new FixedTextFormatingStats( "能量", "ffde00", "Res_Energy", SpaceAfterIcon );
        public static readonly FixedTextFormatingStats Argon = new FixedTextFormatingStats( "氩", "ff8e32", "Res_FuelArgon");
        public static readonly FixedTextFormatingStats Radon = new FixedTextFormatingStats( "氡", "be69ff", "Res_FuelRadon");
        public static readonly FixedTextFormatingStats Xenon = new FixedTextFormatingStats( "氙", "5bcbff", "Res_FuelXenon");
        public static readonly FixedTextFormatingStats Essence = new FixedTextFormatingStats( "精华", "e16cff", "Res_NecromancerEssence");
        public static readonly FixedTextFormatingStats Cuendillar = new FixedTextFormatingStats( "库恩达", "e16cff", "Res_MigrantWormhole");
        
        public static readonly FixedTextFormatingStats Science = new FixedTextFormatingStats( "科技", "7ce9ff", "Res_Science");
        public static readonly FixedTextFormatingStats Hacking = new FixedTextFormatingStats( "入侵", "3de799", "Res_Hack");


        public static readonly FixedTextFormatingStats Strength = new FixedTextFormatingStats( "战力", "ffb74b", "Res_Strength");
        public static readonly FixedTextFormatingStats AIP = new FixedTextFormatingStats( "AIP", "FF5454", "Res_AIP", SpaceAfterIcon );
        public static readonly FixedTextFormatingStats AIPReduction = new FixedTextFormatingStats( "AIPReduction", "ffbca1", "Res_AIP", SpaceAfterIcon );
        public static readonly FixedTextFormatingStats Threat = new FixedTextFormatingStats( "威胁", "FFB74B", "Res_Threat", SpaceAfterIcon );
        
        public static readonly FixedTextFormatingStats Range = new FixedTextFormatingStats( "射程", ColorMath.OrangeRed.GetHexCode() );
        
        public static readonly FixedTextFormatingStats TimeOnPlanet = new FixedTextFormatingStats( "星球停留时间", "e8f3fc", afterIcon:SpaceAfterIcon );
        
        public static readonly FixedTextFormatingStats Speed = new FixedTextFormatingStats( "速度", ColorMath.LightSkyBlue.GetHexCode(), "ShipStats_Speed", SpaceAfterIcon );
        
        public static readonly FixedTextFormatingStats EHP = new FixedTextFormatingStats( "生命", "f5fff5", "ShipStats_HullHealth", SpaceAfterIcon );
        public static readonly FixedTextFormatingStats Hull = new FixedTextFormatingStats( "船体", ColorMath.LightGreen.GetHexCode(), "ShipStats_HullHealth", SpaceAfterIcon );
        public static readonly FixedTextFormatingStats Shield = new FixedTextFormatingStats( "护盾", ColorMath.LightCyan.GetHexCode(), "ShipStats_ShieldHealth", SpaceAfterIcon );
        public static readonly FixedTextFormatingStats Cloak = new FixedTextFormatingStats( "隐形", ColorMath.PaleVioletRed.GetHexCode(), "ShipStats_Cloaking");
        public static readonly FixedTextFormatingStats Tachyon = new FixedTextFormatingStats( "快子", "ffdf72", "ShipStats_Tachyon");
        
        public static readonly FixedTextFormatingStats Armor = new FixedTextFormatingStats( "装甲", ColorMath.LighterRed.GetHexCode(), "ShipStats_Armor", SpaceAfterIcon );
        public static readonly FixedTextFormatingStats Mass = new FixedTextFormatingStats( "质量", ColorMath.LightOrange.GetHexCode(), "ShipStats_Mass", SpaceAfterIcon );
        public static readonly FixedTextFormatingStats Albedo = new FixedTextFormatingStats( "反照率", ColorMath.LightPurple.GetHexCode(), "ShipStats_Albedo", SpaceAfterIcon );
        public static readonly FixedTextFormatingStats Engine = new FixedTextFormatingStats( "引擎", ColorMath.LightWhipBlue.GetHexCode(), "ShipStats_EnginePower", SpaceAfterIcon );

        public static readonly FixedTextFormatingStats Mark = new FixedTextFormatingStats( "Mark" );
        
        public static readonly FixedTextFormatingStats Buffs = new FixedTextFormatingStats( "增益", "0ba70b");
        public static readonly FixedTextFormatingStats BuffsLight = new FixedTextFormatingStats( "增益亮", "007c00");
        public static readonly FixedTextFormatingStats Debuffs = new FixedTextFormatingStats( "减益", "a70b0b");
        public static readonly FixedTextFormatingStats DebuffsLight = new FixedTextFormatingStats( "减益亮", "7c0000");
        public static readonly FixedTextFormatingStats Immunities = new FixedTextFormatingStats( "免疫", "888888");
        public static readonly FixedTextFormatingStats ImmunitiesLight = new FixedTextFormatingStats( "免疫亮", "555555");
        public static readonly FixedTextFormatingStats Mixed = new FixedTextFormatingStats( "混合", "b7890c");
        public static readonly FixedTextFormatingStats MixedLight = new FixedTextFormatingStats( "混合亮", "947d00");
        public static readonly FixedTextFormatingStats Limits = new FixedTextFormatingStats( "限制", "3749e6");
        public static readonly FixedTextFormatingStats LimitsLight = new FixedTextFormatingStats( "限制亮", "1527b3");

        public static readonly FixedTextFormatingStats Damage = new FixedTextFormatingStats( "伤害", ColorMath.LightRed.GetHexCode(), "ShipStats_Attack");
        public static readonly FixedTextFormatingStats ExoticDamage = new FixedTextFormatingStats( "异种", "dfff72", "ShipStats_Attack");
        public static readonly FixedTextFormatingStats CorrosiveDamage = new FixedTextFormatingStats( "腐蚀", "a1ffa1", "ShipStats_Attack");
        public static readonly FixedTextFormatingStats IonDamage = new FixedTextFormatingStats( "离子", "04d9ff", "ShipStats_Attack");
        public static readonly FixedTextFormatingStats Multishot = new FixedTextFormatingStats( Text.Times, "ffdf72");
        public static readonly FixedTextFormatingStats DamagePerSecond = new FixedTextFormatingStats( "DPS", ColorMath.LightRed.GetHexCode(), "ShipStats_Attack" );
        public static readonly FixedTextFormatingStats Shots = new FixedTextFormatingStats( "射击", "ffdf72" );
        public static readonly FixedTextFormatingStats Reload = new FixedTextFormatingStats( "装弹", "ee56bb" );
        
        public static readonly string ShipLineIncreaseColor = "46cdff";
        public static readonly string DefenseLineIncreaseColor = "41ff9a";

        #endregion

        #region DLC4 check
        private static Expansion cachedDlc4Expansion;
        private static bool dlc4LookupDone;
        public static bool IsDlc4InstalledAndEnabled()
        {
            if ( !dlc4LookupDone )
            {
                cachedDlc4Expansion = ExpansionTable.Instance.GetRowByNameOrNullIfNotFound( "4_Forge_Of_Empires_Supporter" );
                dlc4LookupDone = true;
            }
            return cachedDlc4Expansion != null && cachedDlc4Expansion.IsInstalledAndEnabled;
        }
        #endregion

        #region AppendBar
        // 1-2-5 series from 100K to 100M; darker color = coarser interval = each tick represents more health
        private static readonly int[]    AppendBarIntervals    = { 100_000, 200_000, 500_000, 1_000_000, 2_000_000, 5_000_000, 10_000_000, 20_000_000, 50_000_000, 100_000_000 };
        private static readonly string[] AppendBarMarkerColors = { "cccccc", "cccccc", "aaaaaa",   "aaaaaa",   "888888",   "888888",    "666666",    "666666",    "444444",     "444444"     };
        private const int AppendBarMaxMarkers = 8;

        public static void AppendBar( ArcenCharacterBufferBase buffer, int pct, UnityEngine.Color fillColor, int maxWidth = 16, int sizePercent = 100, UnityEngine.Color? inactiveColor = null, int maxValue = 0 )
        {
            if ( sizePercent != 100 )
                buffer.Add( "<size=" + sizePercent + "%>" );
            int filled = pct * maxWidth / 100;
            if ( filled > maxWidth ) filled = maxWidth;

            // Pick the finest interval where marker count stays within the limit
            string markerColor = null;
            int markerInterval = 0;
            if ( maxValue > 0 )
            {
                for ( int t = 0; t < AppendBarIntervals.Length; t++ )
                {
                    if ( maxValue / AppendBarIntervals[t] <= AppendBarMaxMarkers )
                    {
                        markerInterval = AppendBarIntervals[t];
                        markerColor    = AppendBarMarkerColors[t];
                        break;
                    }
                }
            }

            bool hasMarkers = markerInterval > 0;
            if ( !hasMarkers )
            {
                if ( filled > 0 )
                    buffer.Add( new string( '█', filled ), fillColor );
                int empty = maxWidth - filled;
                if ( empty > 0 )
                {
                    if ( inactiveColor.HasValue )
                        buffer.Add( new string( '█', empty ), inactiveColor.Value );
                    else
                        buffer.Add( new string( '░', empty ), "444444" );
                }
            }
            else
            {
                bool[] isMarker = new bool[maxWidth];
                for ( int k = 1; (long)k * markerInterval < maxValue; k++ )
                {
                    int markerSlot = (int)System.Math.Round( (double)k * markerInterval / maxValue * maxWidth );
                    if ( markerSlot > 0 && markerSlot < maxWidth )
                        isMarker[markerSlot] = true;
                }
                for ( int i = 0; i < maxWidth; i++ )
                {
                    if ( isMarker[i] )
                        buffer.Add( "|", markerColor );
                    else if ( i < filled )
                        buffer.Add( "█", fillColor );
                    else if ( inactiveColor.HasValue )
                        buffer.Add( "█", inactiveColor.Value );
                    else
                        buffer.Add( "░", "444444" );
                }
            }

            if ( sizePercent != 100 )
                buffer.Add( "</size>" );
        }
        #endregion

        public static void AddSizeToText( ArcenCharacterBufferBase buffer, string TextToSize, float Size )
        {
            //this was for old sprites-as-fonts: just do the regular thing for now
            buffer.Add( TextToSize );
            //buffer.Add( "<size=" ).Add( Size ).Add( ">" ).Add( TextToSize ).Add( "</size>" );
        }

        public static void AddSizeAndVOssetToText( ArcenCharacterBufferBase buffer, string TextToSize, float Size, float VOffset )
        {
            //this was for old sprites-as-fonts: just do the regular thing for now
            buffer.Add( TextToSize );
            //buffer.Add( "<size=" ).Add( Size ).Add( ">" ).Add( "<voffset=" ).Add( VOffset ).Add( ">" ).Add( TextToSize ).Add( "</voffset>" ).Add( "</size>" );
        }

        #region Added for AMUTranscribedExtensions
        
        public static readonly string[] Magnitudes = new string[] { null, "k", "m", "b", "t", "p", "e", "z", "y" };
        public static readonly string[] Magnitudes_Spaced = new string[] { null, " k", " m", " b", " t", " p", " e", " z", " y" };
        public static readonly string[] Magnitudes_Lowered = new string[] { null, "<sub>k</sub>", "<sub>m</sub>", "<sub>b</sub>", "<sub>t</sub>", "<sub>p</sub>", "<sub>e</sub>", "<sub>z</sub>", "<sub>y</sub>" };
        public static readonly string[] Magnitudes_Spaced_Lowered = new string[] { null, " <sub>k</sub>", " <sub>m</sub>", " <sub>b</sub>", " <sub>t</sub>", " <sub>p</sub>", " <sub>e</sub>", " <sub>z</sub>", " <sub>y</sub>" };
        public static readonly string[] ByteHexesByIndex = new string[] {
            "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "0A", "0B", "0C", "0D", "0E", "0F",
            "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "1A", "1B", "1C", "1D", "1E", "1F",
            "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "2A", "2B", "2C", "2D", "2E", "2F",
            "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "3A", "3B", "3C", "3D", "3E", "3F",
            "40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "4A", "4B", "4C", "4D", "4E", "4F",
            "50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "5A", "5B", "5C", "5D", "5E", "5F",
            "60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "6A", "6B", "6C", "6D", "6E", "6F",
            "70", "71", "72", "73", "74", "75", "76", "77", "78", "79", "7A", "7B", "7C", "7D", "7E", "7F",
            "80", "81", "82", "83", "84", "85", "86", "87", "88", "89", "8A", "8B", "8C", "8D", "8E", "8F",
            "90", "91", "92", "93", "94", "95", "96", "97", "98", "99", "9A", "9B", "9C", "9D", "9E", "9F",
            "A0", "A1", "A2", "A3", "A4", "A5", "A6", "A7", "A8", "A9", "AA", "AB", "AC", "AD", "AE", "AF",
            "B0", "B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8", "B9", "BA", "BB", "BC", "BD", "BE", "BF",
            "C0", "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "CA", "CB", "CC", "CD", "CE", "CF",
            "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "DA", "DB", "DC", "DD", "DE", "DF",
            "E0", "E1", "E2", "E3", "E4", "E5", "E6", "E7", "E8", "E9", "EA", "EB", "EC", "ED", "EE", "EF",
            "F0", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "FA", "FB", "FC", "FD", "FE", "FF" };
        public static readonly string ColorStart = "<color=#";
        public static readonly string ColorEnd = "</color>";
        public static readonly string PosStart = "<pos=";

        //Important settings for global formating! Change this to change the appearance of AMUTranscribed number formating
        public static string[] UsedMagnitudeFormating = Magnitudes;
        public static int PreventMagnitudeFormatingIfLowerThan = 10000;

        public static string GetColorPrefix( string HexCode )
        {
            return ColorStart + HexCode + ">";
        }


        public static FixedTextFormatingStats GetFormatForResource( ResourceType Resource )
        {
            switch ( Resource )
            {
                case ResourceType.Metal:
                    return Metal;
                case ResourceType.Energy:
                    return Energy;
                case ResourceType.Science:
                    return Science;
                case ResourceType.Hacking:
                    return Hacking;
                case ResourceType.FuelArgon:
                    return Argon;
                case ResourceType.FuelRadon:
                    return Radon;
                case ResourceType.FuelXenon:
                    return Xenon;
                default:
                    throw new Exception( "Error: Could not format resource " + Resource );
            }
        }

        #endregion

        #region GetPlanetNameTooltipForLocalPlanet
        public static string GetPlanetNameTooltipForLocalPlanet()
        {
            Planet planet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
            if ( planet == null )
                return string.Empty;
            ArcenCharacterBuffer tooltipBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "ArcenExternalUIUtilities-GetPlanetNameTooltipForLocalPlanet-tooltipBuffer", 5f );
            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.GalaxyMapView )
                tooltipBuffer.Add( "您已选择：" ).Add( planet.Name );
            else
                tooltipBuffer.Add( "您正在查看：" ).Add( planet.Name );

            tooltipBuffer.Add( "\n" );
            if ( Engine_AIW2.Instance.CurrentGameViewMode != GameViewMode.GalaxyMapView )
                tooltipBuffer.Add( "点击此处切换到银河地图。" );
            else
                tooltipBuffer.Add( "点击此处切换到" ).Add( planet.Name ).Add( "的星球视图。" );
            tooltipBuffer.Add( "\n" );
            tooltipBuffer.Add("(快捷键：" + InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "ToggleGalaxyMap" ) ).Add(")");

            return tooltipBuffer.ToStringAndReturnToPool();
        }
        #endregion

        #region GetEnergyTooltip
        public static string GetEnergyTooltip()
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null )
                return string.Empty;

            ArcenCharacterBuffer tooltipBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "ArcenExternalUIUtilities-GetEnergyTooltip-tooltipBuffer", 5f );
            tooltipBuffer.Add( "可用能源。能源是为您的舰队和建筑提供动力所需的全局资源。\n\n已使用：<color=#FFDE00>" )
                .AddNumberMoreReadable( localFaction.EnergyConsumption ).Add("</color>")
                .Add( "\n总量：<color=#e59400>" )
                .AddNumberMoreReadable( localFaction.EnergyProduction ).Add("</color>");
            if ( localFaction.ExtraFreeEnergyProduction_FromOlderSave > 0 )
            {
                tooltipBuffer.Add( "\n加载旧存档获得的友好让步免费能源：<color=#e59400>" )
                    .AddNumberMoreReadable( localFaction.ExtraFreeEnergyProduction_FromOlderSave ).Add( "</color>" );
            }

            return tooltipBuffer.ToStringAndReturnToPool();
        }
        #endregion

        #region GetFuelTooltip
        public static string GetFuelTooltip( ResourceType FuelType )
        {
            Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
            if ( localFaction == null )
                return string.Empty;

            string fuelName = string.Empty;
            string fuelUse = string.Empty;
            string colorGood = string.Empty;
            string colorTotal = string.Empty;
            int consumed = 0;
            int extraConsumed = 0;
            int produced = 0;
            FInt overuseRatio = FInt.One;
            switch ( FuelType )
            {
                case ResourceType.FuelArgon:
                    fuelName = "Argon Fuel";
                    fuelUse = "Argon is a global resource required to run your main combat ships.";
                    colorGood = "ff8e32";
                    colorTotal = "eb481d";
                    consumed = localFaction.FuelArgonConsumption;
                    produced = localFaction.FuelArgonProduction;
                    extraConsumed = World_AIW2.Instance.PermaSpentArgon;
                    overuseRatio = localFaction.FuelArgonOveruseRatio;
                    break;
                case ResourceType.FuelRadon:
                    fuelName = "Radon Fuel";
                    fuelUse = "Radon is a global resource required to run your turrets and forcefields.";
                    colorGood = "be69ff";
                    colorTotal = "9622d8";
                    consumed = localFaction.FuelRadonConsumption;
                    produced = localFaction.FuelRadonProduction;
                    extraConsumed = World_AIW2.Instance.PermaSpentRadon;
                    overuseRatio = localFaction.FuelRadonOveruseRatio;
                    break;
                case ResourceType.FuelXenon:
                    fuelName = "Xenon Fuel";
                    fuelUse = "Xenon is a global resource required to run your officers, elites, and outguard.";
                    colorGood = "5bcbff";
                    colorTotal = "28a8e3";
                    consumed = localFaction.FuelXenonConsumption;
                    produced = localFaction.FuelXenonProduction;
                    extraConsumed = World_AIW2.Instance.PermaSpentXenon;
                    overuseRatio = localFaction.FuelXenonOveruseRatio;
                    break;
            }

            ArcenCharacterBuffer tooltipBuffer = ArcenCharacterBuffer.GetFromPoolOrCreate( "ArcenExternalUIUtilities-GetFuelTooltip-tooltipBuffer", 5f );
            tooltipBuffer.Add( "可用 " ).Add( fuelName ).Add( "。  " ).Add( fuelUse ).Add( "\n\n已使用：<color=#" ).Add( colorGood ).Add( ">" )
                .AddNumberMoreReadable( consumed ).Add( "</color>" )
                .Add( "\n总量：<color=#" ).Add( colorTotal ).Add( ">" )
                .AddNumberMoreReadable( produced ).Add( "</color>" );
            if ( extraConsumed > 0 )
            {
                tooltipBuffer.Add( "\n因过去操作永久消耗的额外 " ).Add( fuelName ).Add( "：<color=#" ).Add( colorGood ).Add( ">" )
                    .AddNumberMoreReadable( extraConsumed ).Add( "</color>\n<size=80%>通常永久消耗来自诸如入侵联系外卫等操作。</size>" );
            }
            if ( overuseRatio < FInt.One && consumed > 0 )
            {
                tooltipBuffer.Add( "\n<color=#ff6935>警告！目前依赖此燃料的单位的生命值、护盾和武器伤害因您的 " ).Add( fuelName ).Add( " 比率而降低，该比率为 " )
                    .AddFixedDecimal( overuseRatio.ToFloatNonSim(), 2 ).Add( " 倍正常值。</color>  " );
            }

            return tooltipBuffer.ToStringAndReturnToPool();
        }
        #endregion

        //public static string GetRoundedNumberWithSuffix( ref int NumericValue )
        //{
        //    string suffix = string.Empty;
        //    int millionsThreshold = 1000000;
        //    int thousandsThreshold = 1000;

        //    int absNumeric = NumericValue;
        //    if ( absNumeric < 0 )
        //        absNumeric = -absNumeric;

        //    if ( absNumeric >= millionsThreshold )
        //    {
        //        suffix = "m";
        //        NumericValue = (int)( NumericValue / 1000000f );
        //    }
        //    else if ( absNumeric >= thousandsThreshold )
        //    {
        //        suffix = "k";
        //        NumericValue = (int)( NumericValue / 1000f );
        //    }

        //    return suffix;
        //}

        public static ArcenCharacterBufferBase WriteRoundedNumberWithSuffix( this ArcenCharacterBufferBase buffer, int NumericValue, bool AddOneSignificantDigit, bool PretendThousandsAreOnes )
        {
            if ( NumericValue == 0 )
            {
                buffer.Add( "0" );
                return buffer;
            }
            if ( NumericValue < 0 )
            {
                buffer.Add( "-" );
                NumericValue = -NumericValue;
            }

            //All branches below previously called float.ToString("#,##0.0#") or
            //int.ToString("#,##0"), each allocating a fresh string (profiler caught
            //~172 B/7 calls per UI frame from this method alone). Switched to the
            //AddThousands / AddRoundedOneOrTwoDecimalsThousands no-alloc helpers on
            //ArcenCharacterBufferBase; output is byte-identical for all values this
            //method produces.
            if ( NumericValue >= 1000000 )
            {
                float newNumber = ( NumericValue / 1000000f );
                if ( AddOneSignificantDigit )
                    buffer.AddRoundedOneOrTwoDecimalsThousands( newNumber );
                else
                    buffer.AddThousands( (int)System.Math.Round( newNumber ) );

                if ( PretendThousandsAreOnes )
                    buffer.Add( "k" );
                else
                    buffer.Add( "m" );
            }
            else if ( NumericValue >= 1000 )
            {
                float newNumber = ( NumericValue / 1000f );
                buffer.AddThousands( (int)System.Math.Round( newNumber ) );

                if ( !PretendThousandsAreOnes )
                    buffer.Add( "k" );
            }
            else
            {
                if ( AddOneSignificantDigit || PretendThousandsAreOnes )
                {
                    float newNumber = ( NumericValue / 1000f );
                    if ( AddOneSignificantDigit )
                        buffer.AddRoundedOneOrTwoDecimalsThousands( newNumber );
                    else
                        buffer.AddThousands( (int)System.Math.Round( newNumber ) );

                    if ( !PretendThousandsAreOnes )
                        buffer.Add( "k" );
                }
                else
                    buffer.AddThousands( NumericValue );
            }

            return buffer;
        }

        public static ArcenCharacterBufferBase WriteRoundedNumberWithSuffix( this ArcenCharacterBufferBase buffer, Int64 NumericValue, bool AddOneSignificantDigit, bool PretendThousandsAreOnes )
        {
            if ( NumericValue == 0 )
            {
                buffer.Add( "0" );
                return buffer;
            }
            if ( NumericValue < 0 )
            {
                buffer.Add( "-" );
                NumericValue = -NumericValue;
            }

            //Same no-alloc treatment as the int overload above — see comment there.
            if ( NumericValue >= 1000000 )
            {
                float newNumber = ( NumericValue / 1000000f );
                if ( AddOneSignificantDigit )
                    buffer.AddRoundedOneOrTwoDecimalsThousands( newNumber );
                else
                    buffer.AddThousands( (long)System.Math.Round( newNumber ) );

                if ( PretendThousandsAreOnes )
                    buffer.Add( "k" );
                else
                    buffer.Add( "m" );
            }
            else if ( NumericValue >= 1000 )
            {
                float newNumber = ( NumericValue / 1000f );
                buffer.AddThousands( (long)System.Math.Round( newNumber ) );

                if ( !PretendThousandsAreOnes )
                    buffer.Add( "k" );
            }
            else
            {
                if ( AddOneSignificantDigit || PretendThousandsAreOnes )
                {
                    float newNumber = ( NumericValue / 1000f );
                    if ( AddOneSignificantDigit )
                        buffer.AddRoundedOneOrTwoDecimalsThousands( newNumber );
                    else
                        buffer.AddThousands( (long)System.Math.Round( newNumber ) );

                    if ( !PretendThousandsAreOnes )
                        buffer.Add( "k" );
                }
                else
                    buffer.AddThousands( NumericValue );
            }

            return buffer;
        }

        public static void ShowTooltipWide( string TextToWrite )
        {
            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, TextToWrite );
        }

        public static void WriteSideContents( PlanetFaction faction, ArcenCharacterBufferBase buffer, bool CareAboutCloaking )
        {
            //so it can be reused for the Hacking Notifier
            int smallShips = 0;
            int largeShips = 0;
            int nonCombatants = 0;
            int smallShips_Cloaked = 0;
            int largeShips_Cloaked = 0;
            int nonCombatants_Cloaked = 0;
            FInt strength = FInt.Zero;
            FInt strength_Cloaked = FInt.Zero;
            int debugCode = 0;
            try
            {
                debugCode = 100;
                foreach ( GameEntity_Squad entity in faction.Entities.Squads() )
                {
                    debugCode = 200;
                    if ( entity == null )
                        continue;
                    debugCode = 300;
                    if ( !entity.TypeData.IsCombatant )
                    {
                        debugCode = 400;
                        nonCombatants += 1 + entity.ExtraStackedSquadsInThis;
                        smallShips += entity.CalculateContentsCount( false );
                        strength += entity.GetStrengthOfContentsIfAny(); //note that unarmed guard posts and the like can still have ships inside them
                        if ( CareAboutCloaking && entity.GetCurrentCloakingPoints() > 0 )
                        {
                            nonCombatants_Cloaked += 1 + entity.ExtraStackedSquadsInThis;
                            smallShips_Cloaked += entity.CalculateContentsCount( false );
                            strength_Cloaked += entity.GetStrengthOfContentsIfAny(); //note that unarmed guard posts and the like can still have ships inside them
                        }
                        continue;
                    }
                    if ( entity.TypeData.IsLargeShip )
                    {
                        debugCode = 500;
                        largeShips += 1 + entity.ExtraStackedSquadsInThis;
                        smallShips += entity.CalculateContentsCount( false );
                        if ( CareAboutCloaking && entity.GetCurrentCloakingPoints() > 0 )
                        {
                            largeShips_Cloaked += 1 + entity.ExtraStackedSquadsInThis;
                            smallShips_Cloaked += entity.CalculateContentsCount( false );
                        }
                    }
                    else
                    {
                        debugCode = 600;
                        smallShips += 1 + entity.ExtraStackedSquadsInThis;
                        if ( CareAboutCloaking && entity.GetCurrentCloakingPoints() > 0 )
                        {
                            smallShips_Cloaked += 1 + entity.ExtraStackedSquadsInThis;
                        }
                    }
                    if ( entity != null )
                    {
                        debugCode = 700;
                        strength += entity.GetStrengthOfSelfAndContents();
                        if ( CareAboutCloaking && entity.GetCurrentCloakingPoints() > 0 )
                            strength_Cloaked += entity.GetStrengthOfSelfAndContents();
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception code " + debugCode + " in write side contents for private attack notifier. If this is hit, we probably need to just ignore this error " + e, Verbosity.DoNotShow );
            }

            if ( smallShips == 0 && largeShips == 0 && nonCombatants == 0 )
                return; //if faction has no ships here, that's ok!

            buffer.StartColor( faction.Faction.FactionCenterColor.TeamColorBrighter );
            buffer.Add( faction.Faction.GetDisplayName() );
            buffer.Add( ":</color> " );

            buffer.Add( smallShips ).Add( " 艘小型舰船" );
            if ( CareAboutCloaking && smallShips_Cloaked > 0 )
                buffer.Add( " (" ).Add( smallShips_Cloaked ).Add( " 隐形)" );
            buffer.Add( ", " );

            buffer.Add( largeShips ).Add( " 艘大型舰船" );
            if ( CareAboutCloaking && largeShips_Cloaked > 0 )
                buffer.Add( " (" ).Add( largeShips_Cloaked ).Add( " 隐形)" );
            buffer.Add( ", " );

            buffer.Add( nonCombatants ).Add( " 艘非战斗舰船" );
            if ( CareAboutCloaking && nonCombatants_Cloaked > 0 )
                buffer.Add( " (" ).Add( nonCombatants_Cloaked ).Add( " 隐形)" );
            buffer.Add( ",   " );

            buffer.Add( ArcenExternalUIUtilities.Strength );
            WriteRoundedNumberWithSuffix( buffer, strength.IntValue, true, true );
            if ( CareAboutCloaking && strength_Cloaked.IntValue > 0 )
            {
                buffer.Add( " (" );
                buffer.Add( ArcenExternalUIUtilities.Strength );
                WriteRoundedNumberWithSuffix( buffer, strength.IntValue, true, true );
                buffer.Add( " 隐形)" );
            }

            buffer.Add( "</color>\n" );
        }

        public static string GetColorForNomadMoveTime( int timeTillNextMove )
        {
            string moveTimerColor = "20ffff"; //moveTimerColor gets more red the closer the planet is to moving
            if ( timeTillNextMove < 600 )
                moveTimerColor = "60a1ff";
            if ( timeTillNextMove < 500 )
                moveTimerColor = "bba1ff";
            if ( timeTillNextMove < 400 )
                moveTimerColor = "bb80a1";
            if ( timeTillNextMove < 300 )
                moveTimerColor = "bb8080";
            if ( timeTillNextMove < 200 )
                moveTimerColor = "ee8080";
            if ( timeTillNextMove < 100 )
                moveTimerColor = "ff8080";
            if ( timeTillNextMove < 60 )
                moveTimerColor = "ff2020";
            return moveTimerColor;
        }

        #region unused
        
        //public static double GetAggressivelyRoundedNumberWithSuffix( Int64 NumericValue, out string numberFormat, out string suffix )
        //{
        //    Int64 absNumeric = NumericValue;
        //    if ( absNumeric < 0 )
        //        absNumeric = -absNumeric;

        //    //1=1
        //    //11=11
        //    //111=111
        //    if ( absNumeric < 1000 )
        //    {
        //        suffix = string.Empty;
        //        numberFormat = "0";
        //        return NumericValue;
        //    }

        //    //1,111=1.1k
        //    if ( absNumeric < 10000 )
        //    {
        //        suffix = "k";
        //        numberFormat = "0.0";
        //        return NumericValue / 1000.0;
        //    }

        //    //11,111=11k
        //    //111,111=111k
        //    if ( absNumeric < 1000000 )
        //    {
        //        suffix = "k";
        //        numberFormat = "0";
        //        return NumericValue / 1000.0;
        //    }

        //    //1,111,111=1.1m
        //    if ( absNumeric < 10000000 )
        //    {
        //        suffix = "m";
        //        numberFormat = "0.0";
        //        return NumericValue / 1000000.0;
        //    }

        //    //11,111,111=11m
        //    //111,111,111=111m
        //    if ( absNumeric < 1000000000 )
        //    {
        //        suffix = "m";
        //        numberFormat = "0";
        //        return NumericValue / 1000000.0;
        //    }

        //    //1,111,111,111=1.1b
        //    if ( absNumeric < 10000000000 )
        //    {
        //        suffix = "b";
        //        numberFormat = "0.0";
        //        return NumericValue / 1000000000.0;
        //    }

        //    //11,111,111,111=11b
        //    //111,111,111,111=111b
        //    if ( absNumeric < 1000000000000 )
        //    {
        //        suffix = "b";
        //        numberFormat = "0";
        //        return NumericValue / 1000000000.0;
        //    }

        //    //1,111,111,111,111=1.1t
        //    if ( absNumeric < 10000000000000 )
        //    {
        //        suffix = "t";
        //        numberFormat = "0.0";
        //        return NumericValue / 1000000000000.0;
        //    }

        //    //anything higher is expressed in whole trillions
        //    suffix = "t";
        //    numberFormat = "0";
        //    return NumericValue / 1000000000000.0;
        //}
        
        #endregion
        
        // !! Use the FixedTextFormatingStats object(s) directly !!
        // !! These all allocate string garbage                  !!
        #region Obsolete

        public static string StrengthTextColor => Strength.Color.TrimStart('#');
        public static string EnergyTextColor => Energy.Color.TrimStart('#');
        public static string ScienceTextColor => Science.Color.TrimStart('#');
        public static string HackingTextColor => Hacking.Color.TrimStart('#');
        public static string MetalTextColor => Metal.Color.TrimStart('#');
        public static string FuelArgonTextColor => Argon.Color.TrimStart('#');
        public static string FuelRadonTextColor => Radon.Color.TrimStart('#');
        public static string FuelXenonTextColor => Xenon.Color.TrimStart('#');
        
        public static string AIPTextColorAndIcon => AIP.ToString_Icon();
        public static string ScienceTextColorAndIcon => Science.ToString_Icon();
        public static string HackingTextColorAndIcon => Hacking.ToString_Icon();
        public static string MetalTextColorAndIcon => Metal.ToString_Icon();
        public static string EnergyTextColorAndIcon => Energy.ToString_Icon();
        public static string FuelArgonTextColorAndIcon => Argon.ToString_Icon();
        public static string FuelXenonTextColorAndIcon => Xenon.ToString_Icon();
        public static string FuelRadonTextColorAndIcon => Radon.ToString_Icon();

        
        public static string GUI_StrengthTextColorAndIcon => Strength.ToString_Icon();
        public static string GUI_StrengthTextIcon_AndDefenseLineIncreaseColor => Strength.ToString_Icon(ArcenExternalUIUtilities.DefenseLineIncreaseColor);
        public static string GUI_StrengthTextIcon_AndShipLineIncreaseColor => Strength.ToString_Icon(ArcenExternalUIUtilities.ShipLineIncreaseColor);

        public static string ByPlanet_StrengthTextColorAndIcon => Strength.ToString_Icon(geo:true);
        public static string ByPlanet_ScienceTextColorAndIcon => Science.ToString_Icon(geo:true);
        public static string ByPlanet_HackingTextColorAndIcon => Hacking.ToString_Icon(geo:true);
        public static string ByPlanet_MetalTextColorAndIcon => Metal.ToString_Icon(geo:true);
        public static string ByPlanet_EnergyTextColorAndIcon => Energy.ToString_Icon(geo:true);
        public static string ByPlanet_FuelArgonTextColorAndIcon => Argon.ToString_Icon(geo:true);
        public static string ByPlanet_FuelRadonTextColorAndIcon => Radon.ToString_Icon(geo:true);
        public static string ByPlanet_FuelXenonTextColorAndIcon => Xenon.ToString_Icon(geo:true);
        
        public static void ByPlanet_WriteStrengthIconWithColor( ArcenCharacterBufferBase Buffer, string Color )
        {
            Buffer.Add(Strength, Color, geo:true);
        }
        
        public static void GUI_WriteStrengthIconWithColor( ArcenCharacterBufferBase Buffer, string Color )
        {
            Buffer.Add(Strength, Color);
        }

        //Strength is stored in milli-units (raw / 1000 = displayed strength). Tiered precision: show
        //more for small numbers and less for big ones — magnitude under 2 -> 2 decimals, under 100 ->
        //1 decimal, 100+ -> whole number (thousands-grouped). Avoids a pointless ".00" on large values
        //while keeping small increases meaningful. Pass the RAW strength; the /1000 is handled here.
        public static ArcenCharacterBufferBase AddStrengthTiered( this ArcenCharacterBufferBase buffer, int rawStrength )
        {
            float displayed = rawStrength / 1000f;
            float magnitude = displayed < 0f ? -displayed : displayed;
            if ( magnitude < 2f )
                return buffer.AddFixedDecimal( displayed, 2 );
            if ( magnitude < 100f )
                return buffer.AddFixedDecimal( displayed, 1 );
            return buffer.AddFixedDecimalThousands( (double)displayed, 0 );
        }

        #endregion
    }
}
