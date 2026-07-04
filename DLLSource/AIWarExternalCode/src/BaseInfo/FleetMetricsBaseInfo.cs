using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    /// Snapshot of the live composition of a fleet, keyed by ship type.
    /// Populated by FleetMetricsBaseInfo.BuildCompositionSnapshot() and passed
    /// optionally into AppendFleetMetricsTooltip to enrich per-type lines.
    public class FleetCompositionSnapshot
    {
        public readonly Dictionary<GameEntityTypeData, int>              CountByType    = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed( 6, "FleetCompositionSnapshot-CountByType" );
        public readonly Dictionary<GameEntityTypeData, long>             StrengthByType = Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 6, "FleetCompositionSnapshot-StrengthByType" );
        public readonly Dictionary<GameEntityTypeData, Balance_MarkLevel> MarkLevelByType = Dictionary<GameEntityTypeData, Balance_MarkLevel>.Create_WillNeverBeGCed( 6, "FleetCompositionSnapshot-MarkLevelByType" );
    }

    /// <summary>
    /// Base class for ExternalFleetBaseInfo subclasses that implement IFleetMetrics.
    /// This class allows players to track the effectiveness of their ships in combat.

    /// Provides shared tracking dictionaries, IFleetMetrics implementations, serialization
    /// helpers, and tooltip rendering for per-ship-line combat effectiveness metrics.
    ///
    /// Subclasses may override GetCanonicalType to remap ship types before bucketing
    /// (e.g. collapsing Necromancer bodyguard variants into their base type).
    /// </summary>
    public abstract class FleetMetricsBaseInfo : ExternalFleetBaseInfo, IFleetMetrics
    {
        public readonly Dictionary<GameEntityTypeData, long> ShipLinesDamageDealt =
            Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 6, "FleetMetricsBaseInfo-ShipLinesDamageDealt" );
        public readonly Dictionary<GameEntityTypeData, long> ShipLinesDamageAbsorbed =
            Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 6, "FleetMetricsBaseInfo-ShipLinesDamageAbsorbed" );
        public readonly Dictionary<GameEntityTypeData, long> ShipLinesCCSecondsApplied =
            Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 6, "FleetMetricsBaseInfo-ShipLinesCCSecondsApplied" );
        public readonly Dictionary<GameEntityTypeData, long> ShipLinesDamageModifierTriggered =
            Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 6, "FleetMetricsBaseInfo-ShipLinesDamageModifierTriggered" );
        public readonly Dictionary<GameEntityTypeData, long> ShipLinesNegativeModifierTriggered =
            Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 6, "FleetMetricsBaseInfo-ShipLinesNegativeModifierTriggered" );
        public readonly Dictionary<GameEntityTypeData, long> ShipLinesKills =
            Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 6, "FleetMetricsBaseInfo-ShipLinesKills" );
        public readonly Dictionary<GameEntityTypeData, long> ShipLinesLosses =
            Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 6, "FleetMetricsBaseInfo-ShipLinesLosses" );
        public readonly Dictionary<GameEntityTypeData, long> ShipLinesLossesMetal =
            Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 6, "FleetMetricsBaseInfo-ShipLinesLossesMetal" );
        public readonly Dictionary<GameEntityTypeData, long> ShipLinesDamageDealtVsMobile =
            Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 6, "FleetMetricsBaseInfo-ShipLinesDamageDealtVsMobile" );
        public readonly Dictionary<GameEntityTypeData, long> ShipLinesDamageDealtVsImmobile =
            Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 6, "FleetMetricsBaseInfo-ShipLinesDamageDealtVsImmobile" );
        public readonly Dictionary<GameEntityTypeData, long> ShipLinesShieldBypassDealt =
            Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 6, "FleetMetricsBaseInfo-ShipLinesShieldBypassDealt" );
        public readonly Dictionary<GameEntityTypeData, long> ShipLinesShieldDamageAbsorbed =
            Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 6, "FleetMetricsBaseInfo-ShipLinesShieldDamageAbsorbed" );
        public readonly Dictionary<GameEntityTypeData, long> ShipLinesOverkillDealt =
            Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 6, "FleetMetricsBaseInfo-ShipLinesOverkillDealt" );
        public readonly Dictionary<GameEntityTypeData, long> ShipLinesMetalMetabolized =
            Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 6, "FleetMetricsBaseInfo-ShipLinesMetalMetabolized" );

        // Enemy-perspective: which enemy types dealt damage to us, and which enemy types did we deal damage to.
        public readonly Dictionary<GameEntityTypeData, long> EnemyTypesDamageReceivedFrom =
            Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 6, "FleetMetricsBaseInfo-EnemyTypesDamageReceivedFrom" );
        public readonly Dictionary<GameEntityTypeData, long> EnemyTypesDamageDealtTo =
            Dictionary<GameEntityTypeData, long>.Create_WillNeverBeGCed( 6, "FleetMetricsBaseInfo-EnemyTypesDamageDealtTo" );

        private readonly List<GameEntityTypeData> sortedMetricsTypes =
            List<GameEntityTypeData>.Create_WillNeverBeGCed( 8, "FleetMetricsBaseInfo-sortedMetricsTypes" );

        private readonly FleetCompositionSnapshot cachedSnapshot = new FleetCompositionSnapshot();

        // Tune these to adjust the size of icons and text in the Fleet Effectiveness tooltip.
        private const int MetricsIconSizePct = 40;
        private const int MetricsTextSizePct = 80;

        // Reduced sizes used when there are many ship lines to display.
        private const int MetricsIconSizePct_Medium = 35;
        private const int MetricsTextSizePct_Medium = 70;
        private const int MetricsIconSizePct_Small   = 30;
        private const int MetricsTextSizePct_Small   = 60;

        // Maximum total lines printed across all ship type blocks before cutting off the tooltip.
        private const int MaxMetricsLines = 60;

        // Width to which metric labels are padded so the percentage column aligns.
        // "Dmg absorbed:" is longer and has wider characters, so it needs a smaller pad width.
        private const int MetricsLabelWidth      = 20;
        private const int MetricsLabelWidthWide  = 15;
        // "100%" is 4 chars; PadLeft ensures the % sign and bar always start at the same offset.
        private const int MetricsPctWidth        = 5;

        private int lastMetricsPlanetIndex = -1;
        private int lastPlanetCheckGameSecond = -1;

        protected override void Cleanup()
        {
            ShipLinesDamageDealt.Clear();
            ShipLinesDamageAbsorbed.Clear();
            ShipLinesCCSecondsApplied.Clear();
            ShipLinesDamageModifierTriggered.Clear();
            ShipLinesNegativeModifierTriggered.Clear();
            ShipLinesKills.Clear();
            ShipLinesLosses.Clear();
            ShipLinesLossesMetal.Clear();
            ShipLinesDamageDealtVsMobile.Clear();
            ShipLinesDamageDealtVsImmobile.Clear();
            ShipLinesShieldBypassDealt.Clear();
            ShipLinesShieldDamageAbsorbed.Clear();
            ShipLinesOverkillDealt.Clear();
            ShipLinesMetalMetabolized.Clear();
            EnemyTypesDamageReceivedFrom.Clear();
            EnemyTypesDamageDealtTo.Clear();
            lastMetricsPlanetIndex = -1;
            lastPlanetCheckGameSecond = -1;
        }

        // Throttled to at most once per game-second to avoid traversing the fleet/planet
        // pointer chain thousands of times per second during large battles.
        private void ResetMetricsIfPlanetChanged()
        {
            int curGameSecond = World_AIW2.Instance.GameSecond;
            if ( curGameSecond == lastPlanetCheckGameSecond ) return;
            lastPlanetCheckGameSecond = curGameSecond;
            int curPlanet = this.AttachedFleet?.Centerpiece.GetSquad()?.Planet?.Index ?? -1;
            if ( curPlanet == lastMetricsPlanetIndex ) return;
            ShipLinesDamageDealt.Clear();
            ShipLinesDamageAbsorbed.Clear();
            ShipLinesCCSecondsApplied.Clear();
            ShipLinesDamageModifierTriggered.Clear();
            ShipLinesNegativeModifierTriggered.Clear();
            ShipLinesKills.Clear();
            ShipLinesLosses.Clear();
            ShipLinesLossesMetal.Clear();
            ShipLinesDamageDealtVsMobile.Clear();
            ShipLinesDamageDealtVsImmobile.Clear();
            ShipLinesShieldBypassDealt.Clear();
            ShipLinesShieldDamageAbsorbed.Clear();
            ShipLinesOverkillDealt.Clear();
            ShipLinesMetalMetabolized.Clear();
            EnemyTypesDamageReceivedFrom.Clear();
            EnemyTypesDamageDealtTo.Clear();
            lastMetricsPlanetIndex = curPlanet;
        }

        /// <summary>
        /// Override to remap a ship type before it is bucketed into the metrics dictionaries.
        /// Default implementation returns the type unchanged.
        /// </summary>
        protected virtual GameEntityTypeData GetCanonicalType( GameEntityTypeData type ) { return type; }

        #region IFleetMetrics
        void IFleetMetrics.OnFleetDamageDealt( int Amount, GameEntity_Squad Target, EntitySystem AttackerSystem, bool OutgoingDamageModifierTriggered, int OverkillDamage )
        {
            if ( !ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() ) return;
            if ( AttackerSystem?.ParentEntity?.TypeData == null || Amount <= 0 ) return;
            ResetMetricsIfPlanetChanged();
            GameEntityTypeData t = GetCanonicalType( AttackerSystem.ParentEntity.TypeData );
            long prev; ShipLinesDamageDealt.TryGetValue( t, out prev );
            ShipLinesDamageDealt[t] = prev + Amount;
            if ( OverkillDamage > 0 )
            {
                long prevOverkill; ShipLinesOverkillDealt.TryGetValue( t, out prevOverkill );
                ShipLinesOverkillDealt[t] = prevOverkill + OverkillDamage;
            }
            if ( Target?.TypeData != null )
            {
                GameEntityTypeData enemyType = Target.TypeData;
                long prevEnemy; EnemyTypesDamageDealtTo.TryGetValue( enemyType, out prevEnemy );
                EnemyTypesDamageDealtTo[enemyType] = prevEnemy + Amount;
                if ( Target.TypeData.IsMobile )
                {
                    long prevM; ShipLinesDamageDealtVsMobile.TryGetValue( t, out prevM );
                    ShipLinesDamageDealtVsMobile[t] = prevM + Amount;
                }
                else
                {
                    long prevI; ShipLinesDamageDealtVsImmobile.TryGetValue( t, out prevI );
                    ShipLinesDamageDealtVsImmobile[t] = prevI + Amount;
                }
            }
            if ( OutgoingDamageModifierTriggered )
            {
                long prevMod; ShipLinesDamageModifierTriggered.TryGetValue( t, out prevMod );
                ShipLinesDamageModifierTriggered[t] = prevMod + 1;
            }
        }

        void IFleetMetrics.OnFleetDamageReceived( int Amount, int ShieldDamage, GameEntity_Squad DamagedEntity, EntitySystem AttackerSystem )
        {
            if ( !ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() ) return;
            if ( DamagedEntity?.TypeData == null || Amount <= 0 ) return;
            ResetMetricsIfPlanetChanged();
            GameEntityTypeData t = GetCanonicalType( DamagedEntity.TypeData );
            long prev; ShipLinesDamageAbsorbed.TryGetValue( t, out prev );
            ShipLinesDamageAbsorbed[t] = prev + Amount;
            if ( ShieldDamage > 0 )
            {
                long prevShield; ShipLinesShieldDamageAbsorbed.TryGetValue( t, out prevShield );
                ShipLinesShieldDamageAbsorbed[t] = prevShield + ShieldDamage;
            }
            if ( AttackerSystem?.ParentEntity?.TypeData != null )
            {
                GameEntityTypeData enemyType = AttackerSystem.ParentEntity.TypeData;
                long prevEnemy; EnemyTypesDamageReceivedFrom.TryGetValue( enemyType, out prevEnemy );
                EnemyTypesDamageReceivedFrom[enemyType] = prevEnemy + Amount;
            }
        }

        void IFleetMetrics.OnFleetCCApplied( int TotalCCSeconds, GameEntity_Squad Target, EntitySystem AttackerSystem )
        {
            if ( !ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() ) return;
            if ( AttackerSystem?.ParentEntity?.TypeData == null || TotalCCSeconds <= 0 ) return;
            ResetMetricsIfPlanetChanged();
            GameEntityTypeData t = GetCanonicalType( AttackerSystem.ParentEntity.TypeData );
            long prev; ShipLinesCCSecondsApplied.TryGetValue( t, out prev );
            ShipLinesCCSecondsApplied[t] = prev + TotalCCSeconds;
        }

        void IFleetMetrics.OnFleetKill( GameEntity_Squad KilledEntity, EntitySystem KillerSystem, int NumStacksKilled )
        {
            if ( !ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() ) return;
            if ( KillerSystem?.ParentEntity?.TypeData == null || NumStacksKilled <= 0 ) return;
            ResetMetricsIfPlanetChanged();
            GameEntityTypeData t = GetCanonicalType( KillerSystem.ParentEntity.TypeData );
            long prev; ShipLinesKills.TryGetValue( t, out prev );
            ShipLinesKills[t] = prev + NumStacksKilled;
        }

        void IFleetMetrics.OnFleetShipLost( GameEntity_Squad LostEntity, EntitySystem KillerSystem, int NumStacksKilled )
        {
            if ( !ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() ) return;
            if ( LostEntity?.TypeData == null || NumStacksKilled <= 0 ) return;
            ResetMetricsIfPlanetChanged();
            GameEntityTypeData t = GetCanonicalType( LostEntity.TypeData );
            long prev; ShipLinesLosses.TryGetValue( t, out prev );
            ShipLinesLosses[t] = prev + NumStacksKilled;
            long prevMetal; ShipLinesLossesMetal.TryGetValue( t, out prevMetal );
            ShipLinesLossesMetal[t] = prevMetal + (long)LostEntity.DataForMark.MetalCost * NumStacksKilled;
        }

        void IFleetMetrics.OnFleetDamageReducedByTargetModifier( int Amount, GameEntity_Squad Target, EntitySystem AttackerSystem )
        {
            if ( !ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() ) return;
            if ( AttackerSystem?.ParentEntity?.TypeData == null || Amount <= 0 ) return;
            ResetMetricsIfPlanetChanged();
            GameEntityTypeData t = GetCanonicalType( AttackerSystem.ParentEntity.TypeData );
            long prev; ShipLinesNegativeModifierTriggered.TryGetValue( t, out prev );
            ShipLinesNegativeModifierTriggered[t] = prev + Amount;
        }

        void IFleetMetrics.OnFleetShieldBypassed( int BypassAmount, GameEntity_Squad Target, EntitySystem AttackerSystem )
        {
            if ( !ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() ) return;
            if ( AttackerSystem?.ParentEntity?.TypeData == null || BypassAmount <= 0 ) return;
            ResetMetricsIfPlanetChanged();
            GameEntityTypeData t = GetCanonicalType( AttackerSystem.ParentEntity.TypeData );
            long prev; ShipLinesShieldBypassDealt.TryGetValue( t, out prev );
            ShipLinesShieldBypassDealt[t] = prev + BypassAmount;
        }

        void IFleetMetrics.OnFleetMetabolization( int MetalGained, GameEntity_Squad DeadEntity )
        {
            if ( !ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() ) return;
            if ( DeadEntity?.TypeData == null || MetalGained <= 0 ) return;
            ResetMetricsIfPlanetChanged();
            GameEntityTypeData t = GetCanonicalType( DeadEntity.TypeData );
            long prev; ShipLinesMetalMetabolized.TryGetValue( t, out prev );
            ShipLinesMetalMetabolized[t] = prev + MetalGained;
        }
        #endregion

        #region Metrics serialization helpers
        protected static void SerializeMetricsDict( SerMetaData MetaData, ArcenSerializationBuffer Buffer,
            Dictionary<GameEntityTypeData, long> Dict, string Label )
        {
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)Dict.Count, Label + "Count" );
            foreach ( KeyValuePair<GameEntityTypeData, long> kv in Dict )
            {
                GameEntityTypeDataTable.Instance.SerializeByIndex( MetaData, kv.Key, Buffer, Label + "Type" );
                Buffer.AddInt64( MetaData, ReadStyle.NonNeg, kv.Value, Label + "Value" );
            }
        }

        protected static void DeserializeMetricsDict( SerMetaData MetaData, ArcenDeserializationBuffer Buffer,
            Dictionary<GameEntityTypeData, long> Dict, string Label )
        {
            Dict.Clear();
            int count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, Label + "Count" );
            for ( int i = 0; i < count; i++ )
            {
                GameEntityTypeData type = GameEntityTypeDataTable.Instance.DeserializeByIndex( MetaData, Buffer, Label + "Type" );
                long value = Buffer.ReadInt64( MetaData, ReadStyle.NonNeg, Label + "Value" );
                if ( type != null )
                    Dict[type] = value;
            }
        }

        protected void SerializeAllMetrics( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            if ( SerializationCmdType.GetIsNetworkType() )
            {
                SerializeMetricsDict( MetaData, Buffer, ShipLinesDamageDealt, "DmgDealt" );
                SerializeMetricsDict( MetaData, Buffer, ShipLinesDamageAbsorbed, "DmgAbsorbed" );
                SerializeMetricsDict( MetaData, Buffer, ShipLinesCCSecondsApplied, "CCSeconds" );
                SerializeMetricsDict( MetaData, Buffer, ShipLinesDamageModifierTriggered, "DmgModTrig" );
                SerializeMetricsDict( MetaData, Buffer, ShipLinesNegativeModifierTriggered, "NegModTrig" );
                SerializeMetricsDict( MetaData, Buffer, ShipLinesKills, "Kills" );
                SerializeMetricsDict( MetaData, Buffer, ShipLinesLosses, "Losses" );
                SerializeMetricsDict( MetaData, Buffer, ShipLinesLossesMetal, "LossesMetal" );
                SerializeMetricsDict( MetaData, Buffer, ShipLinesDamageDealtVsMobile, "DmgVsMobile" );
                SerializeMetricsDict( MetaData, Buffer, ShipLinesDamageDealtVsImmobile, "DmgVsImmobile" );
                SerializeMetricsDict( MetaData, Buffer, ShipLinesShieldBypassDealt, "ShieldBypass" );
                SerializeMetricsDict( MetaData, Buffer, ShipLinesShieldDamageAbsorbed, "ShieldAbsorbed" );
                SerializeMetricsDict( MetaData, Buffer, ShipLinesOverkillDealt, "OverkillDealt" );
                SerializeMetricsDict( MetaData, Buffer, ShipLinesMetalMetabolized, "MetalMetabolized" );
            }
        }

        protected void DeserializeAllMetrics( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            if ( SerializationCmdType.GetIsNetworkType() )
            {
                DeserializeMetricsDict( MetaData, Buffer, ShipLinesDamageDealt, "DmgDealt" );
                DeserializeMetricsDict( MetaData, Buffer, ShipLinesDamageAbsorbed, "DmgAbsorbed" );
                DeserializeMetricsDict( MetaData, Buffer, ShipLinesCCSecondsApplied, "CCSeconds" );
                DeserializeMetricsDict( MetaData, Buffer, ShipLinesDamageModifierTriggered, "DmgModTrig" );
                DeserializeMetricsDict( MetaData, Buffer, ShipLinesNegativeModifierTriggered, "NegModTrig" );
                DeserializeMetricsDict( MetaData, Buffer, ShipLinesKills, "Kills" );
                DeserializeMetricsDict( MetaData, Buffer, ShipLinesLosses, "Losses" );
                DeserializeMetricsDict( MetaData, Buffer, ShipLinesLossesMetal, "LossesMetal" );
                DeserializeMetricsDict( MetaData, Buffer, ShipLinesDamageDealtVsMobile, "DmgVsMobile" );
                DeserializeMetricsDict( MetaData, Buffer, ShipLinesDamageDealtVsImmobile, "DmgVsImmobile" );
                DeserializeMetricsDict( MetaData, Buffer, ShipLinesShieldBypassDealt, "ShieldBypass" );
                DeserializeMetricsDict( MetaData, Buffer, ShipLinesShieldDamageAbsorbed, "ShieldAbsorbed" );
                DeserializeMetricsDict( MetaData, Buffer, ShipLinesOverkillDealt, "OverkillDealt" );
                DeserializeMetricsDict( MetaData, Buffer, ShipLinesMetalMetabolized, "MetalMetabolized" );
            }
        }
        #endregion

        /// <summary>
        /// Builds a snapshot of the fleet's current live ship counts and strength by type.
        /// Returns null if the fleet is not available.
        /// Skips drones, self-constructing units, fleet leaders, and zero-cap ship lines.
        /// </summary>
        protected FleetCompositionSnapshot BuildCompositionSnapshot()
        {
            Fleet fleet = this.AttachedFleet;
            if ( fleet == null )
                return null;
            FleetCompositionSnapshot snapshot = cachedSnapshot;
            snapshot.CountByType.Clear();
            snapshot.StrengthByType.Clear();
            snapshot.MarkLevelByType.Clear();
            foreach ( FleetMembership mem in fleet.MemberGroupsUnsorted_Sim )
            {
                if ( mem == null )
                    continue;
                if ( mem.TypeData.IsDrone || mem.TypeData.SelfConstructs )
                    continue;
                if ( mem.TypeData.IsFleetLeader )
                    continue;
                if ( mem.EffectiveSquadCap <= 0 )
                    continue;
                int count = mem.EffectiveSquadCap - mem.GetRemainingCap( false, -1, ExtraFromStacks.IncludePrecalc );
                if ( count > mem.EffectiveSquadCap )
                    count = mem.EffectiveSquadCap;
                if ( count <= 0 )
                    continue;
                GameEntityTypeData type = GetCanonicalType( mem.TypeData );
                int prevCount; snapshot.CountByType.TryGetValue( type, out prevCount );
                snapshot.CountByType[type] = prevCount + count;
                long prevStrength; snapshot.StrengthByType.TryGetValue( type, out prevStrength );
                snapshot.StrengthByType[type] = prevStrength + (long)mem.GetStrengthPerSquad_PlayerFleetsOnly() * count;
                if ( mem.ForMark?.MarkLevel != null && !snapshot.MarkLevelByType.ContainsKey( type ) )
                    snapshot.MarkLevelByType[type] = mem.ForMark.MarkLevel;
            }
            return snapshot;
        }


        public override void AddMetricsToTooltipForFleet( ArcenCharacterBufferBase buffer )
        {
            AppendFleetMetricsTooltip( buffer, BuildCompositionSnapshot() );
        }

        /// <summary>
        /// Appends the "Fleet Effectiveness" section to the tooltip.
        /// Call this from AddToTooltipForFleet when detailLevel is Full.
        /// Does nothing if no metrics have been recorded yet.
        /// Pass a FleetCompositionSnapshot (from BuildCompositionSnapshot) to also show
        /// live ship counts and strength per type.
        /// </summary>
        protected void AppendFleetMetricsTooltip( ArcenCharacterBufferBase buffer,
            FleetCompositionSnapshot composition = null )
        {
            if ( !ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() )
                return;
            if ( ShipLinesDamageDealt.Count == 0 && ShipLinesDamageAbsorbed.Count == 0 && ShipLinesCCSecondsApplied.Count == 0
                 && ShipLinesKills.Count == 0 && ShipLinesLosses.Count == 0 )
            {
                buffer.Add( "\nThis fleet has no Fleet Effectiveness data\n" );
                return;
            }

            Planet battlePlanet = lastMetricsPlanetIndex >= 0 ? World_AIW2.Instance.GetPlanetByIndex( (short)lastMetricsPlanetIndex ) : null;
            buffer.Add( "<size=80%>Fleet Effectiveness for the battle" );
            if ( battlePlanet != null )
                buffer.Add( " on " ).Add( battlePlanet.Name );
            buffer.Add( ":</size>\n<size=70%><color=#999999><i>(ranked by combined damage output, durability, crowd control, and utility)</i></color></size>\n" );

            long totalDealt = 0, totalAbsorbed = 0, totalCC = 0, totalKills = 0, totalBonusDamage = 0, totalShieldBypass = 0;
            foreach ( KeyValuePair<GameEntityTypeData, long> kv in ShipLinesDamageDealt ) totalDealt += kv.Value;
            foreach ( KeyValuePair<GameEntityTypeData, long> kv in ShipLinesDamageAbsorbed ) totalAbsorbed += kv.Value;
            foreach ( KeyValuePair<GameEntityTypeData, long> kv in ShipLinesCCSecondsApplied ) totalCC += kv.Value;
            foreach ( KeyValuePair<GameEntityTypeData, long> kv in ShipLinesKills ) totalKills += kv.Value;
            foreach ( KeyValuePair<GameEntityTypeData, long> kv in ShipLinesDamageModifierTriggered ) totalBonusDamage += kv.Value;
            foreach ( KeyValuePair<GameEntityTypeData, long> kv in ShipLinesShieldBypassDealt ) totalShieldBypass += kv.Value;

            List<GameEntityTypeData> allTypes = sortedMetricsTypes;
            allTypes.Clear();
            foreach ( KeyValuePair<GameEntityTypeData, long> kv in ShipLinesDamageDealt ) allTypes.Add( kv.Key );
            foreach ( KeyValuePair<GameEntityTypeData, long> kv in ShipLinesDamageAbsorbed )
            {
                if ( !ShipLinesDamageDealt.ContainsKey( kv.Key ) ) allTypes.Add( kv.Key );
            }
            foreach ( KeyValuePair<GameEntityTypeData, long> kv in ShipLinesCCSecondsApplied )
            {
                if ( !ShipLinesDamageDealt.ContainsKey( kv.Key ) && !ShipLinesDamageAbsorbed.ContainsKey( kv.Key ) )
                    allTypes.Add( kv.Key );
            }
            foreach ( KeyValuePair<GameEntityTypeData, long> kv in ShipLinesKills )
            {
                if ( !allTypes.Contains( kv.Key ) ) allTypes.Add( kv.Key );
            }
            foreach ( KeyValuePair<GameEntityTypeData, long> kv in ShipLinesLosses )
            {
                if ( !allTypes.Contains( kv.Key ) ) allTypes.Add( kv.Key );
            }
            foreach ( KeyValuePair<GameEntityTypeData, long> kv in ShipLinesMetalMetabolized )
            {
                if ( !allTypes.Contains( kv.Key ) ) allTypes.Add( kv.Key );
            }

            cb_metricsInstance = this;
            cb_totalDealt = totalDealt; cb_totalAbsorbed = totalAbsorbed; cb_totalCC = totalCC;
            cb_totalKills = totalKills; cb_totalBonusDamage = totalBonusDamage; cb_totalShieldBypass = totalShieldBypass;
            allTypes.Sort( static delegate( GameEntityTypeData a, GameEntityTypeData b )
            {
                float scoreA = cb_metricsInstance.GetEffectivenessScore( a, cb_totalDealt, cb_totalAbsorbed, cb_totalCC, cb_totalKills, cb_totalBonusDamage, cb_totalShieldBypass );
                float scoreB = cb_metricsInstance.GetEffectivenessScore( b, cb_totalDealt, cb_totalAbsorbed, cb_totalCC, cb_totalKills, cb_totalBonusDamage, cb_totalShieldBypass );
                return scoreB.CompareTo( scoreA );
            } );

            int iconSizePct, textSizePct;
            if ( allTypes.Count >= 11 )
            {
                iconSizePct = MetricsIconSizePct_Small;
                textSizePct = MetricsTextSizePct_Small;
            }
            else if ( allTypes.Count >= 8 )
            {
                iconSizePct = MetricsIconSizePct_Medium;
                textSizePct = MetricsTextSizePct_Medium;
            }
            else
            {
                iconSizePct = MetricsIconSizePct;
                textSizePct = MetricsTextSizePct;
            }

            int totalLinesCount = 0;
            for ( int i = 0; i < allTypes.Count; i++ )
            {
                totalLinesCount += AppendMetricsLine( buffer, allTypes[i], totalDealt, totalAbsorbed, totalCC, composition, iconSizePct, textSizePct );
                if ( totalLinesCount >= MaxMetricsLines )
                    break;
            }
            //buffer.Add("\n<size=60%>" + totalLinesCount +"</size>\n");
        }

        /// <summary>
        /// Compact view: one line per ship type showing only the icon and damage-dealt bar.
        /// Triggered by holding Key1 + Key2 together while hovering a fleet in the fleets sidebar.
        /// </summary>
        public void AppendFleetMetricsCompact( ArcenCharacterBufferBase buffer )
        {
            if ( !ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() )
                return;

            Faction fac = this.AttachedFleet?.Faction;
            string colorCenter = fac?.FactionCenterColor?.ColorHex ?? "FFFFFF";
            string colorTrim   = fac?.FactionTrimColor?.ColorHex   ?? "FFFFFF";

            buffer.Add( "<size=" + MetricsTextSizePct + "%>" );

            long totalDealt = 0;
            foreach ( KeyValuePair<GameEntityTypeData, long> kv in ShipLinesDamageDealt ) totalDealt += kv.Value;
            long totalAbsorbedCheck = 0;
            foreach ( KeyValuePair<GameEntityTypeData, long> kv in ShipLinesDamageAbsorbed ) totalAbsorbedCheck += kv.Value;
            if ( totalDealt == 0 && totalAbsorbedCheck == 0 )
            {
                buffer.Add( "No fleet effectiveness data yet — engage some enemies first.\n" );
                buffer.Add( "</size>" );
                return;
            }

            if ( totalDealt > 0 )
            {
                buffer.Add( "Percent of damage done by ship lines:\n" );
                List<GameEntityTypeData> dealtTypes = sortedMetricsTypes;
                dealtTypes.Clear();
                foreach ( KeyValuePair<GameEntityTypeData, long> kv in ShipLinesDamageDealt ) dealtTypes.Add( kv.Key );
                cb_metricsInstance = this;
                dealtTypes.Sort( static delegate( GameEntityTypeData a, GameEntityTypeData b )
                {
                    long da, db;
                    cb_metricsInstance.ShipLinesDamageDealt.TryGetValue( a, out da );
                    cb_metricsInstance.ShipLinesDamageDealt.TryGetValue( b, out db );
                    return db.CompareTo( da );
                } );
                for ( int i = 0; i < dealtTypes.Count; i++ )
                {
                    GameEntityTypeData type = dealtTypes[i];
                    long dealt;
                    ShipLinesDamageDealt.TryGetValue( type, out dealt );
                    if ( dealt <= 0 )
                        continue;
                    int pct = (int)(dealt * 100 / totalDealt);
                    UnityEngine.Color pctColor = EntityText.GetProportionalStrengthColor( pct / 100f );
                    buffer.AddShipIconInline( type, colorCenter, colorTrim, null, MetricsIconSizePct );
                    buffer.Add( pct.ToString( "D2" ) + "% ", pctColor );
                    ArcenExternalUIUtilities.AppendBar( buffer, pct, pctColor, 18, 70 );
                    buffer.Add( "\n" );
                }
            }

            long totalAbsorbed = totalAbsorbedCheck;
            if ( totalAbsorbed > 0 )
            {
                buffer.Add( "\nPercent of damage absorbed by ship lines:\n" );
                List<GameEntityTypeData> absorbedTypes = sortedMetricsTypes;
                absorbedTypes.Clear();
                foreach ( KeyValuePair<GameEntityTypeData, long> kv in ShipLinesDamageAbsorbed ) absorbedTypes.Add( kv.Key );
                cb_metricsInstance = this;
                absorbedTypes.Sort( static delegate( GameEntityTypeData a, GameEntityTypeData b )
                {
                    long da, db;
                    cb_metricsInstance.ShipLinesDamageAbsorbed.TryGetValue( a, out da );
                    cb_metricsInstance.ShipLinesDamageAbsorbed.TryGetValue( b, out db );
                    return db.CompareTo( da );
                } );
                for ( int i = 0; i < absorbedTypes.Count; i++ )
                {
                    GameEntityTypeData type = absorbedTypes[i];
                    long absorbed;
                    ShipLinesDamageAbsorbed.TryGetValue( type, out absorbed );
                    if ( absorbed <= 0 )
                        continue;
                    int pct = (int)(absorbed * 100 / totalAbsorbed);
                    UnityEngine.Color pctColor = EntityText.GetProportionalStrengthColor( pct / 100f );
                    buffer.AddShipIconInline( type, colorCenter, colorTrim, null, MetricsIconSizePct );
                    buffer.Add( pct.ToString( "D2" ) + "% ", pctColor );
                    ArcenExternalUIUtilities.AppendBar( buffer, pct, pctColor, 18, 70 );
                    buffer.Add( "\n" );
                }
            }

            buffer.Add( "\n<color=#d18444>Hold <color=#996f4c>" )
                  .Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key1" ) )
                  .Add( "</color> + <color=#996f4c>" )
                  .Add( InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction( "IncreaseShipAndPlanetTooltipDetailBy1_Key2" ) )
                  .Add( "</color> to see more data.</color>" );
            buffer.Add( "</size>" );
        }

        public void AppendEnemyMetricsCompact( ArcenCharacterBufferBase buffer )
        {
            if ( !ArcenExternalUIUtilities.IsDlc4InstalledAndEnabled() )
                return;

            buffer.Add( "<size=" + MetricsTextSizePct + "%>" );

            long totalReceivedFrom = 0;
            foreach ( KeyValuePair<GameEntityTypeData, long> kv in EnemyTypesDamageReceivedFrom ) totalReceivedFrom += kv.Value;
            if ( totalReceivedFrom > 0 )
            {
                buffer.Add( "Damage dealt to us by enemy type:\n" );
                List<GameEntityTypeData> receivedTypes = sortedMetricsTypes;
                receivedTypes.Clear();
                foreach ( KeyValuePair<GameEntityTypeData, long> kv in EnemyTypesDamageReceivedFrom ) receivedTypes.Add( kv.Key );
                cb_metricsInstance = this;
                receivedTypes.Sort( static delegate( GameEntityTypeData a, GameEntityTypeData b )
                {
                    long da, db;
                    cb_metricsInstance.EnemyTypesDamageReceivedFrom.TryGetValue( a, out da );
                    cb_metricsInstance.EnemyTypesDamageReceivedFrom.TryGetValue( b, out db );
                    return db.CompareTo( da );
                } );
                int receivedShown = 0;
                for ( int i = 0; i < receivedTypes.Count; i++ )
                {
                    GameEntityTypeData type = receivedTypes[i];
                    long received;
                    EnemyTypesDamageReceivedFrom.TryGetValue( type, out received );
                    if ( received <= 0 )
                        continue;
                    if ( receivedShown >= 15 )
                    {
                        buffer.Add( "  ...and " + (receivedTypes.Count - i) + " more\n" );
                        break;
                    }
                    int pct = (int)(received * 100 / totalReceivedFrom);
                    UnityEngine.Color pctColor = EntityText.GetProportionalStrengthColor( pct / 100f );
                    buffer.AddShipIconInline( type, "FF4444", "FF8888", null, MetricsIconSizePct );
                    buffer.Add( pct.ToString( "D2" ) + "% ", pctColor );
                    ArcenExternalUIUtilities.AppendBar( buffer, pct, pctColor, 18, 70 );
                    buffer.Add( "\n" );
                    receivedShown++;
                }
            }

            long totalDealtTo = 0;
            foreach ( KeyValuePair<GameEntityTypeData, long> kv in EnemyTypesDamageDealtTo ) totalDealtTo += kv.Value;
            if ( totalDealtTo > 0 )
            {
                buffer.Add( "\nDamage absorbed by enemy types:\n" );
                List<GameEntityTypeData> dealtToTypes = sortedMetricsTypes;
                dealtToTypes.Clear();
                foreach ( KeyValuePair<GameEntityTypeData, long> kv in EnemyTypesDamageDealtTo ) dealtToTypes.Add( kv.Key );
                cb_metricsInstance = this;
                dealtToTypes.Sort( static delegate( GameEntityTypeData a, GameEntityTypeData b )
                {
                    long da, db;
                    cb_metricsInstance.EnemyTypesDamageDealtTo.TryGetValue( a, out da );
                    cb_metricsInstance.EnemyTypesDamageDealtTo.TryGetValue( b, out db );
                    return db.CompareTo( da );
                } );
                int dealtShown = 0;
                for ( int i = 0; i < dealtToTypes.Count; i++ )
                {
                    GameEntityTypeData type = dealtToTypes[i];
                    long dealtTo;
                    EnemyTypesDamageDealtTo.TryGetValue( type, out dealtTo );
                    if ( dealtTo <= 0 )
                        continue;
                    if ( dealtShown >= 15 )
                    {
                        buffer.Add( "  ...and " + (dealtToTypes.Count - i) + " more\n" );
                        break;
                    }
                    int pct = (int)(dealtTo * 100 / totalDealtTo);
                    UnityEngine.Color pctColor = EntityText.GetProportionalStrengthColor( pct / 100f );
                    buffer.AddShipIconInline( type, "FF4444", "FF8888", null, MetricsIconSizePct );
                    buffer.Add( pct.ToString( "D2" ) + "% ", pctColor );
                    ArcenExternalUIUtilities.AppendBar( buffer, pct, pctColor, 18, 70 );
                    buffer.Add( "\n" );
                    dealtShown++;
                }
            }

            if ( totalReceivedFrom == 0 && totalDealtTo == 0 )
                buffer.Add( "No enemy engagement data for this battle yet.\n" );

            buffer.Add( "</size>" );
        }

        //Set immediately before the metrics sorts so the sort comparisons can be non-capturing static
        //delegates (no per-call closure allocation).  Plain static (not [ThreadStatic]) because the
        //metrics tooltip append runs on the main thread.
        private static FleetMetricsBaseInfo cb_metricsInstance;
        private static long cb_totalDealt, cb_totalAbsorbed, cb_totalCC, cb_totalKills, cb_totalBonusDamage, cb_totalShieldBypass;

        private float GetEffectivenessScore( GameEntityTypeData type, long totalDealt, long totalAbsorbed, long totalCC, long totalKills, long totalBonusDamage, long totalShieldBypass )
        {
            long dealt, absorbed, cc, kills, bonusDamage, shieldBypass, overkill, shieldAbsorbed;
            ShipLinesDamageDealt.TryGetValue( type, out dealt );
            ShipLinesDamageAbsorbed.TryGetValue( type, out absorbed );
            ShipLinesCCSecondsApplied.TryGetValue( type, out cc );
            ShipLinesKills.TryGetValue( type, out kills );
            ShipLinesDamageModifierTriggered.TryGetValue( type, out bonusDamage );
            ShipLinesShieldBypassDealt.TryGetValue( type, out shieldBypass );
            ShipLinesOverkillDealt.TryGetValue( type, out overkill );
            ShipLinesShieldDamageAbsorbed.TryGetValue( type, out shieldAbsorbed );

            float dmgPct          = totalDealt        > 0 ? (float)dealt        / totalDealt        * 100f : 0f;
            float tankPct         = totalAbsorbed     > 0 ? (float)absorbed     / totalAbsorbed     * 100f : 0f;
            float ccPct           = totalCC           > 0 ? (float)cc           / totalCC           * 100f : 0f;
            float killsPct        = totalKills        > 0 ? (float)kills        / totalKills        * 100f : 0f;
            float bonusDamagePct  = totalBonusDamage  > 0 ? (float)bonusDamage  / totalBonusDamage  * 100f : 0f;
            float shieldBypassPct = totalShieldBypass > 0 ? (float)shieldBypass / totalShieldBypass * 100f : 0f;

            // Penalize dmgPct by overkill waste: a ship overkilling half its total output loses 25% of its damage score.
            float overkillRatio    = (dealt + overkill) > 0 ? (float)overkill / (dealt + overkill) : 0f;
            float effectiveDmgPct  = dmgPct * (1f - overkillRatio * 0.5f);

            // Boost tankPct for ships absorbing with shields rather than hull (shields regen; hull doesn't).
            float shieldRatio       = absorbed > 0 ? (float)shieldAbsorbed / absorbed : 0f;
            float effectiveTankPct  = tankPct * (1f + shieldRatio * 0.2f);

            return effectiveDmgPct + effectiveTankPct + killsPct * 0.5f + ccPct / 10f + bonusDamagePct / 10f + shieldBypassPct / 10f;
        }

        // Returns the number of UI lines printed for this ship type.
        private int AppendMetricsLine( ArcenCharacterBufferBase buffer, GameEntityTypeData type,
            long totalDealt, long totalAbsorbed, long totalCC, FleetCompositionSnapshot composition,
            int iconSizePct, int textSizePct )
        {
            Faction fac = this.AttachedFleet?.Faction;
            string colorCenter = fac?.FactionCenterColor?.ColorHex ?? "FFFFFF";
            string colorTrim   = fac?.FactionTrimColor?.ColorHex   ?? "FFFFFF";
            buffer.AddShipIconInline( type, colorCenter, colorTrim, null, iconSizePct );
            buffer.Add( "<size=" + textSizePct + "%>" );
            buffer.Add( type.DisplayName );
            if ( composition != null )
            {
                Balance_MarkLevel markLevel; composition.MarkLevelByType.TryGetValue( type, out markLevel );
                if ( markLevel != null )
                    buffer.Add( " " ).StartColor( markLevel.ColorHex ).Add( markLevel.MapDisplay ).EndColor();
            }
            type.AppendFleetMetricsSpecialFeaturesLabel( buffer );
            buffer.Add( " " );
            if ( composition != null )
            {
                int count; composition.CountByType.TryGetValue( type, out count );
                long strength; composition.StrengthByType.TryGetValue( type, out strength );
                if ( count > 0 )
                    buffer.Add( "\t" ).Add( count.ToString() + "x  ", "33aaff" )
                        .Add( AbbreviateNumber( strength / 1000 ), "aaff33" )
                        .Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon );
            }
            buffer.Add( "\n" );
            long dealt, absorbed, cc, modTriggered, negModTriggered, kills, losses, lossesMetal, dealtVsMobile, dealtVsImmobile, shieldBypass, shieldAbsorbed, overkill, metalMetabolized;
            ShipLinesDamageDealt.TryGetValue( type, out dealt );
            ShipLinesDamageAbsorbed.TryGetValue( type, out absorbed );
            ShipLinesCCSecondsApplied.TryGetValue( type, out cc );
            ShipLinesDamageModifierTriggered.TryGetValue( type, out modTriggered );
            ShipLinesNegativeModifierTriggered.TryGetValue( type, out negModTriggered );
            ShipLinesKills.TryGetValue( type, out kills );
            ShipLinesLosses.TryGetValue( type, out losses );
            ShipLinesLossesMetal.TryGetValue( type, out lossesMetal );
            ShipLinesDamageDealtVsMobile.TryGetValue( type, out dealtVsMobile );
            ShipLinesDamageDealtVsImmobile.TryGetValue( type, out dealtVsImmobile );
            ShipLinesShieldBypassDealt.TryGetValue( type, out shieldBypass );
            ShipLinesShieldDamageAbsorbed.TryGetValue( type, out shieldAbsorbed );
            ShipLinesOverkillDealt.TryGetValue( type, out overkill );
            ShipLinesMetalMetabolized.TryGetValue( type, out metalMetabolized );

            if ( dealt > 0 )
            {
                int pct = totalDealt > 0 ? (int)(dealt * 100 / totalDealt) : 0;
                UnityEngine.Color pctColor = EntityText.GetProportionalStrengthColor( pct / 100f );
                buffer.Add( "\t\t" ).Add( "Dmg dealt:".PadRight( MetricsLabelWidth ), "ff6666" ).Add( (pct.ToString() + "%").PadLeft( MetricsPctWidth ) + " ", pctColor );
                //ArcenExternalUIUtilities.AppendBar( buffer, pct, pctColor ); // disabled in verbose mode
                if ( composition != null )
                {
                    long strength; composition.StrengthByType.TryGetValue( type, out strength );
                    if ( strength > 0 )
                    {
                        long dmgPerStr = dealt / strength;
                        buffer.Add( "  (" + AbbreviateNumber( dmgPerStr ) + "/str)", GetDamagePerStrengthColor( dmgPerStr ) );
                    }
                }
                if ( dealtVsMobile > 0 || dealtVsImmobile > 0 )
                {
                    int mobilePct    = dealt > 0 ? (int)(dealtVsMobile    * 100 / dealt) : 0;
                    int immobilePct  = dealt > 0 ? (int)(dealtVsImmobile  * 100 / dealt) : 0;
                    buffer.Add( "  " ).Add( mobilePct.ToString() + "%ship", "88ddff" )
                          .Add( " / " ).Add( immobilePct.ToString() + "%bldg", "ffcc88" );
                }
                buffer.Add( "\n" );
            }
            if ( absorbed > 0 )
            {
                int pct = totalAbsorbed > 0 ? (int)(absorbed * 100 / totalAbsorbed) : 0;
                UnityEngine.Color pctColor = EntityText.GetProportionalStrengthColor( pct / 100f );
                buffer.Add( "\t\t" ).Add( "Dmg absorbed:".PadRight( MetricsLabelWidthWide ), "6699ff" ).Add( (pct.ToString() + "%").PadLeft( MetricsPctWidth ) + " ", pctColor );
                if ( shieldAbsorbed > 0 )
                {
                    int shieldPct = (int)(shieldAbsorbed * 100 / absorbed);
                    int hullPct   = 100 - shieldPct;
                    buffer.Add( "  (" + shieldPct + "%shld / " + hullPct + "%hull)", "aaccff" );
                }
                buffer.Add( "\n" );
            }
            if ( cc > 0 )
            {
                int pct = totalCC > 0 ? (int)(cc * 100 / totalCC) : 0;
                UnityEngine.Color pctColor = EntityText.GetProportionalStrengthColor( pct / 100f );
                buffer.Add( "\t\t" ).Add( "CC seconds:".PadRight( MetricsLabelWidth ), "cc99ff" ).Add( (pct.ToString() + "%").PadLeft( MetricsPctWidth ) + " ", pctColor );
                buffer.Add( "  " ).Add( AbbreviateNumber( cc ), pctColor ).Add( "\n" );
            }
            if ( modTriggered > 0 )
                buffer.Add( "\t\t" ).Add( "Bonus Damage: ", "ffcc66" ).Add( AbbreviateNumber( modTriggered ) + "x", "ffcc66" ).Add( "\n" );
            if ( negModTriggered > 0 )
                buffer.Add( "\t\t" ).Add( "Reduced Damage: ", "cc8833" ).Add( AbbreviateNumber( negModTriggered ) + "x", "cc8833" ).Add( "\n" );
            if ( shieldBypass > 0 )
                buffer.Add( "\t\t" ).Add( "Shield Bypass: ", "88eeff" ).Add( AbbreviateNumber( shieldBypass ), "88eeff" ).Add( "\n" );

            int linesCount = 1; // header line (ship name)
            if ( dealt > 0 ) linesCount++;
            if ( absorbed > 0 ) linesCount++;
            if ( cc > 0 ) linesCount++;
            if ( modTriggered > 0 ) linesCount++;
            if ( negModTriggered > 0 ) linesCount++;
            if ( shieldBypass > 0 ) linesCount++;
            if ( kills > 0 || losses > 0 || metalMetabolized > 0 ) linesCount++;

            if ( kills > 0 || losses > 0 || metalMetabolized > 0 )
            {
                buffer.Add( "\t\t" );
                if ( kills > 0 )
                {
                    buffer.Add( "Kills: ", "a1ffa1" ).Add( AbbreviateNumber( kills ), "a1ffa1" );
                    if ( overkill > 0 )
                        buffer.Add( "  Overkill: ", "88cc88" ).Add( AbbreviateNumber( overkill ), "88cc88" );
                }
                if ( kills > 0 && losses > 0 )
                    buffer.Add( "  " );
                if ( losses > 0 )
                {
                    buffer.Add( "Losses: ", "ffa1a1" ).Add( AbbreviateNumber( losses ), "ffa1a1" );
                    if ( lossesMetal > 0 )
                        buffer.Add( " (" ).Add( AbbreviateNumber( lossesMetal ), ArcenExternalUIUtilities.MetalTextColor )
                            .Add( ArcenExternalUIUtilities.MetalTextColorAndIcon ).Add( ")" );
                }
                if ( metalMetabolized > 0 )
                {
                    if ( kills > 0 || losses > 0 )
                        buffer.Add( "  " );
                    buffer.Add( "Metabolized: ", ArcenExternalUIUtilities.MetalTextColor )
                          .Add( AbbreviateNumber( metalMetabolized ), ArcenExternalUIUtilities.MetalTextColor )
                          .Add( ArcenExternalUIUtilities.MetalTextColorAndIcon );
                }
                buffer.Add( "\n" );
            }
            buffer.Add( "</size>" );
            return linesCount;
        }

        private static string GetDamagePerStrengthColor( long dmgPerStr )
        {
            if ( dmgPerStr <=  50 ) return "ff4444"; // red
            if ( dmgPerStr <=  100 ) return "ff6644"; // red
            if ( dmgPerStr <=  300 ) return "ff8844"; // orange
            if ( dmgPerStr <=  600 ) return "ffdd44"; // yellow
            if ( dmgPerStr <=  900 ) return "aadd44"; // yellow-green
            if ( dmgPerStr <= 1200 ) return "44dd44"; // green
            return "44ffaa";                           // bright green
        }

        protected static string AbbreviateNumber( long value )
        {
            if ( value >= 1_000_000 ) return ( value / 1_000_000.0 ).ToString( "0.#" ) + "M";
            if ( value >= 1_000 )    return ( value / 1_000.0 ).ToString( "0.#" ) + "k";
            return value.ToString();
        }
    }
}
