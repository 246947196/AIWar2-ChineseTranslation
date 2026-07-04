using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class NecromancerMobileFleetBaseInfo : FleetMetricsBaseInfo, IFleetTransforms
    {
        //Serialized
        //---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------
        public readonly List<NecromancerUpgrade> NecromancerCompletedUpgrades = List<NecromancerUpgrade>.Create_WillNeverBeGCed( 30, "NecromancerMobileFleetBaseInfo-NecromancerCompletedUpgrades" );
        public int BonusSkeletonsEarned = 0;
        public int BonusWightsEarned = 0;
        public int BonusMummiesEarned = 0;

        public readonly Dictionary<GameEntityTypeData, int> ShipLinesRaised = Dictionary<GameEntityTypeData, int>.Create_WillNeverBeGCed( 6, "NecromancerMobileFleetBaseInfo-PercentSkeletonType" );

        //Non-Serialized
        //---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------

        public readonly DoubleBufferedValue<int> NumSkeletonsInFleet = new DoubleBufferedValue<int>(0);
        public readonly DoubleBufferedValue<int> NumWightsInFleet = new DoubleBufferedValue<int>(0);
        public readonly DoubleBufferedValue<int> NumMummiesInFleet = new DoubleBufferedValue<int>(0);

        public readonly DoubleBufferedValue<int> SkeletonSoftCap = new DoubleBufferedValue<int>(0);
        public readonly DoubleBufferedValue<int> WightSoftCap = new DoubleBufferedValue<int>(0);

        public readonly DoubleBufferedValue<int> BonusSkeletonPercent = new DoubleBufferedValue<int>(0);
        public readonly DoubleBufferedValue<int> BonusWightPercent = new DoubleBufferedValue<int>(0);
        public readonly DoubleBufferedValue<int> BonusMummyPercent = new DoubleBufferedValue<int>(0);

        public readonly DoubleBufferedDictionary<string, int> PercentSkeletonType = DoubleBufferedDictionary<string, int>.Create_WillNeverBeGCed( 6, "NecromancerMobileFleetBaseInfo-PercentSkeletonType" );
        public readonly DoubleBufferedDictionary<string, int> PercentWightType = DoubleBufferedDictionary<string, int>.Create_WillNeverBeGCed( 6, "NecromancerMobileFleetBaseInfo-PercentWightType" );
        public readonly DoubleBufferedDictionary<string, int> PercentMummyType = DoubleBufferedDictionary<string, int>.Create_WillNeverBeGCed( 6, "NecromancerMobileFleetBaseInfo-PercentMummyType" );

        public int PercentSkeletonTypeExcess = 0;
        public int PercentWightTypeExcess = 0;
        public int PercentMummyTypeExcess = 0;

        //These are used for auto-defend mode
        public bool NeedsToRebuild; //I've taken losses and need to go to a shipyard
        public bool NeedsToRetreat; //My planet has too many enemies
        public bool NeedsToDefendKing; //needs to defend our allied king

        public NecromancerMobileFleetBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            NecromancerCompletedUpgrades.Clear();
            ShipLinesRaised.Clear();
            BonusSkeletonsEarned = 0;
            BonusWightsEarned = 0;
            BonusMummiesEarned = 0;
            this.ResetAggregateUpgradeData();
        }

        public void ResetAggregateUpgradeData()
        {
            this.NumSkeletonsInFleet.ClearConstructionValueForStartingConstruction();
            this.NumWightsInFleet.ClearConstructionValueForStartingConstruction();
            this.NumMummiesInFleet.ClearConstructionValueForStartingConstruction();

            this.SkeletonSoftCap.ClearConstructionValueForStartingConstruction();
            this.WightSoftCap.ClearConstructionValueForStartingConstruction();

            this.BonusSkeletonPercent.ClearConstructionValueForStartingConstruction();
            this.BonusWightPercent.ClearConstructionValueForStartingConstruction();
            this.BonusMummyPercent.ClearConstructionValueForStartingConstruction();

            this.PercentSkeletonType.ClearConstructionDictForStartingConstruction();
            this.PercentWightType.ClearConstructionDictForStartingConstruction();
            this.PercentMummyType.ClearConstructionDictForStartingConstruction();
        }

        public void SwitchAggregateUpgradeDataToDisplay() {
            this.NumSkeletonsInFleet.SwitchConstructionToDisplay();
            this.NumWightsInFleet.SwitchConstructionToDisplay();
            this.NumMummiesInFleet.SwitchConstructionToDisplay();

            this.SkeletonSoftCap.SwitchConstructionToDisplay();
            this.WightSoftCap.SwitchConstructionToDisplay();

            this.BonusSkeletonPercent.SwitchConstructionToDisplay();
            this.BonusWightPercent.SwitchConstructionToDisplay();
            this.BonusMummyPercent.SwitchConstructionToDisplay();

            this.PercentSkeletonType.SwitchConstructionToDisplay();
            this.PercentWightType.SwitchConstructionToDisplay();
            this.PercentMummyType.SwitchConstructionToDisplay();
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "NecromancerMobileFleetBaseInfo" );

            if ( this.NecromancerCompletedUpgrades == null )
            {
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, 0, "NecromancerUpgradeIndices" );
            }
            else
            {
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.NecromancerCompletedUpgrades.Count, "NecromancerUpgradeIndices" );
                for ( int i = 0; i < this.NecromancerCompletedUpgrades.Count; i++ )
                    Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.NecromancerCompletedUpgrades[i].Index, "NecromancerUpgradeIndex" );
            }
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.BonusSkeletonsEarned, "BonusSkeletonsEarned" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.BonusWightsEarned, "BonusWightsEarned" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.BonusMummiesEarned, "BonusMummiesEarned" );

            SerializeAllMetrics( MetaData, Buffer, SerializationCmdType );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "NecromancerMobileFleetBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "NecromancerMobileFleetBaseInfo Ext", TrackerStyle.ByTypeOnly );

            int count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "NecromancerUpgradeIndices" );
            this.NecromancerCompletedUpgrades.Clear();
            for ( int i = 0; i < count; i++ )
                this.NecromancerCompletedUpgrades.Add( NecromancerUpgradeTable.Instance.GetRowByIndex( (Int32)Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "NecromancerUpgradeIndex" ) ) );

            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 3, 764 ) ) //3763_GuardPostConstruction^M
            {
                BonusSkeletonsEarned = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "BonusSkeletonsEarned" );
                BonusWightsEarned = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "BonusWightsEarned" );
                BonusMummiesEarned = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "BonusMummiesEarned" );
            }

            DeserializeAllMetrics( MetaData, Buffer, SerializationCmdType );

            Buffer.StopTrackerByName( "NecromancerMobileFleetBaseInfo Ext" );

            if ( Buffer.FromGameVersion.GetLessThan( 5, 010 ) ) {
                RebuildUpgrades();
            }
        }

        /// <summary>Fix NecromancerCompletedUpgrades for save games before 5.009</summary>
        /// In older versions, that field was not cleared between games, so could contain
        /// garbage. However, we can recreate that information based on data on the faction.
        private void RebuildUpgrades()
        {
            NecromancerEmpireFactionBaseInfo necroFac = this.AttachedFleet?.Faction?.TryGetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            if (necroFac == null) {
                ArcenDebugging.ArcenDebugLog("Couldn't find Necromancer Faction info when trying to fix per-fleet upgrade information. ", Verbosity.ShowAsError);
                return;
            }
            this.NecromancerCompletedUpgrades.Clear();
            foreach (NecromancerUpgradeEvent upgradeEvent in necroFac.NecromancerHistory) {
                if (upgradeEvent.RelatedFleetId == this.AttachedFleet.FleetID && upgradeEvent.Upgrade != null) {
                    this.NecromancerCompletedUpgrades.Add(upgradeEvent.Upgrade);
                }
            }
        }

        public void NormalizeNecromancyRatios()
        {
            this.PercentSkeletonTypeExcess = NormalizeRatios(this.PercentSkeletonType.Construction);
            this.PercentWightTypeExcess = NormalizeRatios(this.PercentWightType.Construction);
            this.PercentMummyTypeExcess = NormalizeRatios(this.PercentMummyType.Construction);
        }
 
        private static int NormalizeRatios(DoubleBufferedDictionary<string, int>.ConstructionData PercentType)
        {
            int totalPercent = 0;
            //if the percent chance of getting any ship type is over 100, scale
            //everything appropriately
            foreach ( KeyValuePair<string, int> kv in PercentType )
            {
                totalPercent += kv.Value;
            }
            if ( totalPercent > 100 )
            {
                FInt factor = (FInt)totalPercent / 100;
                foreach ( KeyValuePair<string, int> kv in PercentType )
                {
                    PercentType[kv.Key] = (kv.Value / factor).IntValue;
                }
                return totalPercent - 100;
            } else {
                return 0;
            }
        }

        private static readonly Dictionary<GameEntityTypeData, GameEntityTypeData> canonicalTypeCache =
            Dictionary<GameEntityTypeData, GameEntityTypeData>.Create_WillNeverBeGCed( 8, "NecromancerMobileFleetBaseInfo-canonicalTypeCache" );

        /// <summary>
        /// Bodyguard ship types (tag NecromancerBodyguard) are merged with their base type for
        /// display purposes. E.g. PossessedWightBodyguard 鈫?PossessedWight.
        /// Returns the base GameEntityTypeData if found, otherwise the original.
        /// </summary>
        protected override GameEntityTypeData GetCanonicalType( GameEntityTypeData type )
        {
            GameEntityTypeData cached;
            if ( canonicalTypeCache.TryGetValue( type, out cached ) ) return cached;
            GameEntityTypeData result;
            const string suffix = "Bodyguard";
            if ( !type.GetHasTag( "NecromancerBodyguard" ) || !type.InternalName.EndsWith( suffix ) )
                result = type;
            else
            {
                string name = type.InternalName;
                GameEntityTypeData baseType = GameEntityTypeDataTable.Instance.GetRowByNameOrNullIfNotFound(
                    name.Substring( 0, name.Length - suffix.Length ) );
                result = baseType ?? type;
            }
            canonicalTypeCache[type] = result;
            return result;
        }

        public override void AddToTooltipForFleet( ArcenCharacterBufferBase buffer, TooltipDetail detailLevel )
        {
            bool fullDetail = detailLevel >= TooltipDetail.Full;
            if (fullDetail) {
                buffer.NewLine();
            }
            float skeletonRatio = (float)this.NumSkeletonsInFleet.Display / this.SkeletonSoftCap.Display;
            UnityEngine.Color skeletonColor = EntityText.GetProportionalStrengthColor( skeletonRatio );
            buffer.Add( "This fleet has " ).Add( this.NumSkeletonsInFleet.Display.ToString(), skeletonColor ).Add( "/" ).Add( this.SkeletonSoftCap.Display.ToString(), skeletonColor ).Add( " skeletons");
            if ( fullDetail ) {
                buffer.Add( ".\n");
                Dictionary<string, int> PercentSkeletonType = this.PercentSkeletonType.GetDisplayDict();
                if ( PercentSkeletonType.Count != 0 ) {
                    buffer.Add("Skeleton Ratios:");
                    if (PercentSkeletonTypeExcess > 0) {
                        buffer.Add(" (excess ").Add(PercentSkeletonTypeExcess).Add("%)");
                    }
                    buffer.NewLine();
                    DisplayTypePercentages(buffer, PercentSkeletonType);
                }
                if ( this.BonusSkeletonPercent.Display > 0 ) {
                    buffer.Add( "You have a " ).Add( this.BonusSkeletonPercent.Display, "ffa1a1" ).Add( "% chance of getting additional skeletons whenever you get a skeleton.\n" );
                }
                if ( this.BonusSkeletonsEarned > 0 )
                    buffer.Add( "You have earned " ).Add( this.BonusSkeletonsEarned, "a1a1ff" ).Add( " bonus skeletons.\n" );
                buffer.Add( "This fleet has " );
            }

            float wightRatio = (float)this.NumWightsInFleet.Display / this.WightSoftCap.Display;
            UnityEngine.Color wightColor = EntityText.GetProportionalStrengthColor( wightRatio );
            if ( !fullDetail ) {
                buffer.Add(" and ");
            }
            buffer.Add( this.NumWightsInFleet.Display.ToString(), wightColor ).Add( "/" ).Add( this.WightSoftCap.Display.ToString(), wightColor ).Add( " wights. " );

            if ( fullDetail ) {
                Dictionary<string, int> PercentWightType = this.PercentWightType.GetDisplayDict();
                if ( PercentWightType.Count != 0 ) {
                    buffer.Add("\nWight Ratios:");
                    if (PercentWightTypeExcess > 0) {
                        buffer.Add(" (excess ").Add(PercentWightTypeExcess).Add("%)");
                    }
                    buffer.NewLine();
                    DisplayTypePercentages(buffer, PercentWightType);
                }
                if ( this.BonusWightPercent.Display > 0 )
                    buffer.Add( "You have a " ).Add( this.BonusWightPercent.Display, "ffa1a1" ).Add( "% chance of getting additional wights whenever you get a wight.\n" );
                if ( this.BonusWightsEarned > 0 )
                    buffer.Add( "You have earned " ).Add( this.BonusWightsEarned, "a1a1ff" ).Add( " bonus wights.\n" );
            }
            if ( fullDetail ) {
                Dictionary<string, int> PercentMummyType = this.PercentMummyType.GetDisplayDict();
                if ( PercentMummyType.Count != 0 ) {
                    buffer.Add("Mummy Ratios:");
                    if (PercentMummyTypeExcess > 0) {
                        buffer.Add(" (excess ").Add(PercentMummyTypeExcess).Add("%)");
                    }
                    buffer.NewLine();
                    DisplayTypePercentages(buffer, PercentMummyType);
                }
                if ( this.BonusMummyPercent.Display > 0 )
                    buffer.Add( "You have a " ).Add( this.BonusMummyPercent.Display, "ffa1a1" ).Add( "% chance of getting additional mummies whenever you get a mummy.\n" );
                if ( this.BonusMummiesEarned > 0 )
                    buffer.Add( "You have earned " ).Add( this.BonusMummiesEarned, "a1a1ff" ).Add( " bonus mummies.\n" );

            }

            if ( fullDetail ) {
                if ( ShipLinesRaised.Count > 0 )
                {
                    buffer.Add( "\nFleet Kill Count (since last game load):\n" );
                    foreach ( KeyValuePair<GameEntityTypeData, int> kv in ShipLinesRaised )
                    {
                        buffer.Add( "This fleet's " ).Add( kv.Key.DisplayName + "s", "a1ffa1" ).Add( " have slain " ).Add( kv.Value, "ffa1a1" ).Add( " foes.\n" );
                    }
                }
            }


        }

        public static void DisplayTypePercentages(ArcenCharacterBufferBase buffer, Dictionary<string, int> percentageTypes)
        {
            foreach ( KeyValuePair<string, int> pair in percentageTypes )
            {
                buffer.Add(" - ").Add( pair.Value, "3344ff" ).Add("% of ").Add(pair.Key);
                buffer.NewLine();
            }
        }

        public override int AddToGetBaseSquadCapWithAdditions( int CapSoFar, FleetMembership FMem )
        {
            if (CapSoFar == 0) {
                return CapSoFar;
            }
            if ( FMem == null )
                return CapSoFar;
            Fleet fleet = this.AttachedFleet;
            if ( fleet == null )
                return CapSoFar;
            if ( this.NecromancerCompletedUpgrades != null &&
                 this.NecromancerCompletedUpgrades.Count > 0 )
            {
                //process fleet specific upgrades
                for ( int i = 0; i < this.NecromancerCompletedUpgrades.Count; i++ )
                {
                    NecromancerUpgrade upgrade = this.NecromancerCompletedUpgrades[i];
                    if ( upgrade.ShipForCapIncrease == null )
                        continue;
                    //ArcenDebugging.ArcenDebugLogSingleLine("processing fleet upgrade " + upgrade.ToString() + " in fleet code", Verbosity.DoNotShow );
                    if ( upgrade.ShipForCapIncrease == FMem.TypeData )
                    {
                        CapSoFar += upgrade.CapIncrease;
                        //ArcenDebugging.ArcenDebugLogSingleLine("\tupdated base to " + CapSoFar, Verbosity.DoNotShow );
                    }
                }
            }
            Faction fac = fleet.Faction;
            NecromancerEmpireFactionBaseInfo necroFac = null;
            if ( fac != null )
                necroFac = fac.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            if ( necroFac != null && necroFac.NecromancerCompletedUpgrades.Count > 0 )
            {
                //process faction wide updates
                for ( int i = 0; i < necroFac.NecromancerCompletedUpgrades.Count; i++ )
                {
                    NecromancerUpgrade upgrade = necroFac.NecromancerCompletedUpgrades[i];
                    //ArcenDebugging.ArcenDebugLogSingleLine("processing faction upgrade " + upgrade.ToString() + " in fleet code", Verbosity.DoNotShow );
                    if ( upgrade.ShipForCapIncrease == null )
                        continue;
                    if ( upgrade.ShipForCapIncrease == FMem.TypeData )
                    {
                        CapSoFar += upgrade.CapIncrease;
                        //ArcenDebugging.ArcenDebugLogSingleLine("\tupdated base to " + CapSoFar, Verbosity.DoNotShow );
                    }
                }

            }

            return CapSoFar;
        }

        #region IFleetTransforms
        string IFleetTransforms.DisplayName { get { return "Flagship"; } }
        public string NoTransformsText { get { return "No flagship blueprints available."; } }

        bool IFleetTransforms.HasAnyTransforms {
            get {
                NecromancerEmpireFactionBaseInfo baseInfo = this.AttachedFleet.Faction.TryGetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
                return baseInfo.AvailableBlueprints.GetDisplayList().Count > 0;
            }
        }
        System.Collections.Generic.IEnumerable<IFleetTransformTarget> IFleetTransforms.TypesCanSwitchTo {
            get {
                NecromancerEmpireFactionBaseInfo baseInfo = this.AttachedFleet.Faction.TryGetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
                return baseInfo.AvailableBlueprints.GetDisplayList();
            }
        }

        GameCommand IFleetTransforms.CreateTransformCommand(GameCommandSource source, GameEntity_Squad centerpiece, string InternalName) {
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransformNecrofleet], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            command.RelatedEntityIDs.Add( centerpiece.PrimaryKeyID );
            command.RelatedString2 = InternalName;
            return command;
        }

        GameEntityTypeData IFleetTransforms.GetTypeDataForName(string InternalName) {
            NecromancerUpgrade upgrade = NecromancerUpgradeTable.Instance.GetRowByName(InternalName);
            return GameEntityTypeDataTable.Instance.GetRowByName(upgrade?.RelatedShip.InternalName);
        }

        void IFleetTransforms.GetTooltip(ArcenCharacterBufferBase buffer)
        {
            buffer.Add("You can spend essence points to change the form of this flagship.");
            NecromancerEmpireFactionBaseInfo baseInfo = this.AttachedFleet.Faction.TryGetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            if (baseInfo.AvailableBlueprints.GetDisplayList().Count == 0) {
                buffer.NewLine();
                buffer.Add("You can find blueprints from rifts or by using the transform elderling hack.");
            }
        }
        #endregion
    }
}
