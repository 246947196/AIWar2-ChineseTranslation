using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public enum AIPChangeReason : byte
    {
        None,
        InitialValue,
        AutoIncrease,
        EntityDeath,
        Hacking,
        EntityClaim,
        FleetConcentration,
        Debug,
        RiskAnalyzer,
        PlanetCapture,
        FailedHacking,
        PlanetDrilling,
        FactionEscalation,
        Length
    }

    public class AIPChange : ConcurrentPoolable<AIPChange>, IProtectedListable
    {
        public int GameSecond;
        public FInt Change;
        public Int16 PrimaryFactionIndex;
        public Int16 SecondaryFactionIndex;
        public Int16 PlanetIndex;
        public AIPChangeReason Reason;
        public GameEntityTypeData RelatedEntityTypeData;
        public GameEntityTypeData SecondaryRelatedEntityTypeData;
        public FInt Floor;
        public FInt ResultingAIP;

        //IMPORTANT: any additions to the above need to have a clear entry in SetDefaults!

        #region Pooling
        private static ReferenceTracker RefTracker;
        private AIPChange()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "AIPChange" );
            RefTracker.IncrementObjectCount();
            SetDefaults();
        }

        private static readonly ConcurrentPool<AIPChange> Pool = new ConcurrentPool<AIPChange>( "AIPChange", 3000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new AIPChange(); } );

        public static AIPChange GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public AIPChange CreateNewForPool()
        {
            return new AIPChange();
        }

        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        public override void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            SetDefaults();
        }

        //IProtectedListable
        public void DoBeforeRemoveOrClear()
        {
            Pool.ReturnToPool( this );
        }
        #endregion

        private void SetDefaults()
        {
            this.GameSecond = 0;
            this.Change = FInt.Zero;
            this.GameSecond = 0;
            this.PrimaryFactionIndex = 0;
            this.PrimaryFactionIndex = 0;
            this.PlanetIndex = 0;
            this.Reason = AIPChangeReason.None;
            this.Floor = FInt.Zero;
            this.ResultingAIP = FInt.Zero;
        }

        public static AIPChange Create( FInt Change, AIPChangeReason Reason, GameEntityTypeData RelatedEntityTypeData, Int16 PrimaryFactionIndex, Int16 PlanetIndex, FInt Floor, Int16 SecondaryFactionIndex )
        {
            AIPChange result = GetFromPoolOrCreate();
            result.GameSecond = World_AIW2.Instance.GameSecond;
            result.Change = Change;
            result.Reason = Reason;
            result.RelatedEntityTypeData = RelatedEntityTypeData;
            result.PlanetIndex = PlanetIndex;
            result.PrimaryFactionIndex = PrimaryFactionIndex;
            result.SecondaryFactionIndex = SecondaryFactionIndex;
            result.Floor = Floor;
            result.ResultingAIP = FactionUtilityMethods.Instance.GetCurrentAIP();
            //            ArcenDebugging.ArcenDebugLogSingleLine("Adding new AIP change of " + result.Change + " floor " + result.Floor, Verbosity.DoNotShow );
            return result;
        }
        public static AIPChange Create( FInt Change, AIPChangeReason Reason, GameEntityTypeData RelatedEntityTypeData, GameEntityTypeData OtherRelatedEntityTypeData, Int16 PrimaryFactionIndex, Int16 PlanetIndex, FInt Floor, Int16 SecondaryFactionIndex )
        {
            AIPChange result = GetFromPoolOrCreate();
            result.GameSecond = World_AIW2.Instance.GameSecond;
            result.Change = Change;
            result.Reason = Reason;
            result.RelatedEntityTypeData = RelatedEntityTypeData;
            result.SecondaryRelatedEntityTypeData = OtherRelatedEntityTypeData;
            result.PlanetIndex = PlanetIndex;
            result.PrimaryFactionIndex = PrimaryFactionIndex;
            result.SecondaryFactionIndex = SecondaryFactionIndex;
            result.Floor = Floor;
            result.ResultingAIP = FactionUtilityMethods.Instance.GetCurrentAIP();
            //            ArcenDebugging.ArcenDebugLogSingleLine("Adding new AIP change of " + result.Change + " floor " + result.Floor, Verbosity.DoNotShow );
            return result;
        }

        public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.GameSecond, "AIPChangeHistory-GameSecond" );
            Buffer.AddFInt( MetaData, this.Change, "AIPChangeHistory-Change" );
            Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)this.Reason, "AIPChangeHistory-Reason" );
            GameEntityTypeDataTable.Instance.SerializeByIndex( MetaData, this.RelatedEntityTypeData, Buffer, "RelatedEntityTypeData" );
            GameEntityTypeDataTable.Instance.SerializeByIndex( MetaData, this.SecondaryRelatedEntityTypeData, Buffer, "SecondaryRelatedEntityTypeData" );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, this.PlanetIndex, "PlanetIndex" );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, this.PrimaryFactionIndex, "PrimaryFactionIndex" );
            Buffer.AddFInt( MetaData, this.Floor, "AIPChangeHistory-Floor" );
            Buffer.AddFInt( MetaData, this.ResultingAIP, "AIPChangeHistory-ResultingAIP" );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, this.SecondaryFactionIndex, "AIPChangeHistory-SecondaryFactionIndex" );
        }

        public void DeserializedIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.GameSecond = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "AIPChangeHistory-GameSecond" );
            this.Change = Buffer.ReadFInt( MetaData, "AIPChangeHistory-Change" );
            this.Reason = (AIPChangeReason)Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "AIPChangeHistory-Reason" );
            this.RelatedEntityTypeData = GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, "RelatedEntityTypeData" );
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 3, 794 ) )
                this.SecondaryRelatedEntityTypeData = GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, "SecondaryRelatedEntityTypeData" );
            this.PlanetIndex = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "PlanetIndex" );
            this.PrimaryFactionIndex = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "PrimaryFactionIndex" );
            this.Floor = Buffer.ReadFInt( MetaData, "AIPChangeHistory-Floor" );
            this.ResultingAIP = Buffer.ReadFInt( MetaData, "AIPChangeHistory-ResultingAIP" );
            this.SecondaryFactionIndex = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "AIPChangeHistory-SecondaryFactionIndex" );
        }

        public string GetDescription()
        {
            string entityTypeName = this.RelatedEntityTypeData == null ? "null" : this.RelatedEntityTypeData.GetDisplayName();
            string reasonText = string.Empty;
            switch ( this.Reason )
            {
                case AIPChangeReason.InitialValue:
                    reasonText = "初始值";
                    break;
                case AIPChangeReason.AutoIncrease:
                    reasonText = "自动增长";
                    break;
                case AIPChangeReason.EntityDeath:
                    reasonText = "摧毁{0}";
                    break;
                case AIPChangeReason.EntityClaim:
                    reasonText = "占领{0}";
                    break;
                case AIPChangeReason.Hacking:
                    reasonText = "骇入{0}";
                    break;
                case AIPChangeReason.FleetConcentration:
                    reasonText = "舰队在{0}过度集中";
                    break;
                case AIPChangeReason.Debug:
                    reasonText = "调试{0}";
                    break;
                case AIPChangeReason.RiskAnalyzer:
                    reasonText = "风险分析器";
                    break;
                case AIPChangeReason.PlanetCapture:
                    reasonText = "占领星球"; //we don't have the plaent linked
                    break;
                case AIPChangeReason.FailedHacking:
                    reasonText = "骇入{0}失败";
                    break;
                case AIPChangeReason.PlanetDrilling:
                    reasonText = "钻探{0}";
                    break;
                case AIPChangeReason.FactionEscalation:
                    reasonText = "派系升级";
                    break;

                default:
                    reasonText = "BUG: 未知 AIPChangeReason " + this.Reason;
                    break;
            }

            string reason = Engine_Universal.SafeFormat( reasonText, entityTypeName );
            if ( this.Change >= 0 )
                return Engine_Universal.SafeFormat( "<color=#ff0000>+{1} AIP</color> 来自{0}", reason, this.Change.ToDoubleNonSim().ToString( "#,##0.##" ) );
            else
                return Engine_Universal.SafeFormat( "<color=#00ff00>-{1} AIP</color> 来自{0}", reason, (-this.Change.ToDoubleNonSim()).ToString( "#,##0.##" ) );
        }

        public override void AppendStateForInterfaceDisplay( ArcenCharacterBufferBase buffer )
        {
            AppendStateForInterfaceDisplay_OverrideAIP( buffer, this.Change );
        }

        public void AppendStateForInterfaceDisplay_OverrideAIP( ArcenCharacterBufferBase buffer, FInt overrideAIP )
        {
            int debugCode = 0;
            bool drawFaction = true;
            bool drawPlanet = true;
            bool drawEntity = true;
            bool drawBasicReason = true;
            try
            {
                debugCode = 5;
                buffer.Add( "在 " ).AddHoursAndMinutes( this.GameSecond ).Add( " 时 AIP 变化 " );
                if ( overrideAIP > FInt.Zero )
                    buffer.Add( "<color=#ff0000>+" ).Add( overrideAIP ).Add( "</color>" );
                else
                    buffer.Add( "<color=#ffbca1>" ).Add( overrideAIP ).Add( "</color>" );
                
                debugCode = 40;
                if ( this.Reason == AIPChangeReason.EntityDeath )
                {
                    debugCode = 41;
                    if ( this.PrimaryFactionIndex != -1 )
                    {
                        debugCode = 42;
                        Faction primaryfaction = World_AIW2.Instance.GetFactionByIndex( this.PrimaryFactionIndex );
                        Faction secondaryfaction = World_AIW2.Instance.GetFactionByIndex( this.SecondaryFactionIndex );
                        string factionName = primaryfaction.GetDisplayName();
                        buffer.Add( " 来自 <color=#").Add(primaryfaction.FactionCenterColor.ColorHexBrighter).Add("> ").Add(factionName).Add("</color>击杀 " );
                        if ( secondaryfaction != null )
                            buffer.Add( "<color=#").Add(secondaryfaction.FactionCenterColor.ColorHexBrighter).Add(">").Add(this.RelatedEntityTypeData.GetDisplayName()).Add("</color>" );
                        else
                            buffer.Add( this.RelatedEntityTypeData.GetDisplayName() );
                        drawFaction = false;
                    }
                    else
                        buffer.Add( " 因 ").Add(this.RelatedEntityTypeData.GetDisplayName() ).Add( " 死亡" );
                    drawBasicReason = false;
                    debugCode = 43;
                    drawEntity = false;
                }
                debugCode = 50;
                if ( this.Reason == AIPChangeReason.Hacking )
                {
                    debugCode = 51;
                    if ( this.RelatedEntityTypeData != null )
                    {
                        buffer.Add( " 来自骇入 ").Add(this.RelatedEntityTypeData.GetDisplayName() );
                        if ( this.SecondaryRelatedEntityTypeData != null )
                            buffer.Add(" 以解锁 " ).Add( this.SecondaryRelatedEntityTypeData.GetDisplayName() );
                    }
                    else
                        buffer.Add( " 来自科学骇入 " ); //science hacking is the only path that allows for hacking AIP but doesn't have a GameEntity associated. If that changes, this logic will need to be improved
                    drawBasicReason = false;
                    drawEntity = false;
                }
                if ( this.Reason == AIPChangeReason.PlanetCapture )
                {
                    Planet planet = World_AIW2.Instance.GetPlanetByIndex( this.PlanetIndex );
                    if ( planet == null )
                    {
                        buffer.Add( " 因占领星球 " );
                        drawBasicReason = false;
                    }
                    else
                    {
                        buffer.Add( " 因占领 " );
                        Faction secondaryfaction = World_AIW2.Instance.GetFactionByIndex( this.SecondaryFactionIndex );
                        if ( secondaryfaction == null )
                            secondaryfaction = World_AIW2.Instance.GetNeutralFaction();
                        buffer.Add( "<color=#").Add(secondaryfaction.FactionCenterColor.ColorHexBrighter).Add(">").Add(planet.Name).Add("</color>" );
                        drawBasicReason = false;
                        drawPlanet = false;
                    }
                }

                debugCode = 60;
                if ( this.Reason == AIPChangeReason.RiskAnalyzer )
                {
                    debugCode = 61;
                    Faction faction = World_AIW2.Instance.GetFactionByIndex( this.PrimaryFactionIndex );
                    if ( faction == null )
                        ArcenDebugging.ArcenDebugLogSingleLine( "BUG: faction is not set in aip change for risk analyzers", Verbosity.ShowAsError );
                    else
                    {
                        buffer.Add( " 来自 <color=#").Add(faction.FactionCenterColor.ColorHexBrighter).Add(">风险分析器</color>发射。" );
                        drawBasicReason = false;
                    }
                    debugCode = 62;
                    drawFaction = false;
                }
                debugCode = 70;
                if ( this.Reason == AIPChangeReason.EntityClaim )
                {
                    debugCode = 71;
                    Faction faction = World_AIW2.Instance.GetFactionByIndex( this.PrimaryFactionIndex );
                    if ( faction == null )
                        faction = World_AIW2.Instance.GetPlayerFactionForUIOrNull();
                    string color = faction.FactionCenterColor.ColorHexBrighter;
                    buffer.Add( " 来自 <color=#").Add(color).Add(">占领</color> ").Add(this.RelatedEntityTypeData.GetDisplayName() );
                    drawBasicReason = false;
                }
                if ( this.Reason == AIPChangeReason.InitialValue )
                {
                    buffer.Add( "。反抗AI的战斗开始了！" );
                    drawBasicReason = false;
                }
                if(this.Reason == AIPChangeReason.AutoIncrease)
                {
                    buffer.Add( " 来自自动增长。" );
                    drawBasicReason = false;
                }
                if ( this.Reason == AIPChangeReason.PlanetDrilling )
                {
                    Planet planet = World_AIW2.Instance.GetPlanetByIndex( this.PlanetIndex );
                    if ( planet != null )
                        buffer.Add( " 来自钻探" );
                    drawBasicReason = false;
                    drawEntity = false;
                }
                if ( this.Reason == AIPChangeReason.FactionEscalation )
                {
                    Faction faction = World_AIW2.Instance.GetFactionByIndex( this.PrimaryFactionIndex );
                    if ( faction != null )
                        buffer.Add( " 来自 <color=#" ).Add( faction.FactionCenterColor.ColorHexBrighter ).Add( ">" ).Add( faction.GetDisplayName() ).Add( "</color>升级。" );
                    else
                        buffer.Add( " 来自派系升级。" );
                    drawBasicReason = false;
                    drawFaction = false;
                }

                if ( drawBasicReason )
                {
                    debugCode = 40;
                    buffer.Add( " 因 ").Add( Extensions.ToString(this.Reason) ); //this is a default
                }

                debugCode = 30;
                if ( drawEntity && this.RelatedEntityTypeData != null )
                {
                    //this is a default; it may be overwritten by the this.Reason processing
                    debugCode = 31;
                    buffer.Add( " 相关于 ").Add(this.RelatedEntityTypeData.GetDisplayName() );
                }

                debugCode = 20;
                if ( drawPlanet && this.PlanetIndex != -1 )
                {
                    //this is a default; it may be overwritten by the this.Reason processing
                    debugCode = 21;
                    Planet planet = World_AIW2.Instance.GetPlanetByIndex( this.PlanetIndex );
                    buffer.Add( " 在 <color=#a1ffa1>").Add(planet.Name).Add("</color>" );
                }

                debugCode = 10;
                if ( drawFaction && this.PrimaryFactionIndex != -1 )
                {
                    //this is a default; it may be overwritten by the this.Reason processing
                    debugCode = 11;
                    Faction faction = World_AIW2.Instance.GetFactionByIndex( this.PrimaryFactionIndex );
                    string factionName = faction.GetDisplayName();
                    buffer.Add( "。由 <color=#").Add(faction.FactionCenterColor.ColorHexBrighter).Add("> ").Add(factionName).Add("</color>造成。" );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "AIPChange.ToString() debug code " + debugCode + " exception " + e.ToString(), Verbosity.DoNotShow );
            }
        }
    }
}
