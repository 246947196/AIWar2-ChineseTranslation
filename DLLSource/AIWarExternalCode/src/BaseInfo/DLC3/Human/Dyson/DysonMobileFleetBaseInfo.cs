using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DysonSidekickMobileFleetBaseInfo : FleetMetricsBaseInfo, IFleetTransforms
    {
        //Serialized
        //---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------
        public readonly List<DysonUpgrade> DysonCompletedUpgrades = List<DysonUpgrade>.Create_WillNeverBeGCed( 30, "DysonMobileFleetBaseInfo-DysonCompletedUpgrades" );

        //Non-Serialized
        //---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------
        //These are used for auto-defend mode
        public bool NeedsToRebuild; //I've taken losses and need to go to a shipyard
        public bool NeedsToRetreat; //My planet has too many enemies
        public bool NeedsToDefendKing; //needs to defend our allied king
        public DysonSidekickMobileFleetBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            DysonCompletedUpgrades.Clear();
            this.ResetAggregateUpgradeData();
        }

        public void ResetAggregateUpgradeData()
        {
        }

        public void SwitchAggregateUpgradeDataToDisplay() {
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "DysonSidekickMobileFleetBaseInfo" );

            if ( this.DysonCompletedUpgrades == null )
            {
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, 0, "DysonUpgradeIndices" );
            }
            else
            {
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.DysonCompletedUpgrades.Count, "DysonUpgradeIndices" );
                for ( int i = 0; i < this.DysonCompletedUpgrades.Count; i++ )
                    Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.DysonCompletedUpgrades[i].Index, "DysonUpgradeIndex" );
            }

            SerializeAllMetrics( MetaData, Buffer, SerializationCmdType );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "DysonSidekickMobileFleetBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "DysonSidekickMobileFleetBaseInfo Ext", TrackerStyle.ByTypeOnly );

            int count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "DysonUpgradeIndices" );
            this.DysonCompletedUpgrades.Clear();
            for ( int i = 0; i < count; i++ )
                this.DysonCompletedUpgrades.Add( DysonUpgradeTable.Instance.GetRowByIndex( (Int32)Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "DysonUpgradeIndex" ) ) );

            DeserializeAllMetrics( MetaData, Buffer, SerializationCmdType );
        }


        public override void AddToTooltipForFleet( ArcenCharacterBufferBase buffer, TooltipDetail detailLevel )
        {
            if ( detailLevel >= TooltipDetail.Full )
                AppendFleetMetricsTooltip( buffer, BuildCompositionSnapshot() );
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
            if ( this.DysonCompletedUpgrades != null &&
                 this.DysonCompletedUpgrades.Count > 0 )
            {
                //process fleet specific upgrades
                for ( int i = 0; i < this.DysonCompletedUpgrades.Count; i++ )
                {
                    DysonUpgrade upgrade = this.DysonCompletedUpgrades[i];
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
            DysonSidekickFactionBaseInfo dysonFac = null;
            if ( fac != null )
                dysonFac = fac.GetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
            if ( dysonFac != null && dysonFac.DysonCompletedUpgrades.Count > 0 )
            {
                //process faction wide updates
                for ( int i = 0; i < dysonFac.DysonCompletedUpgrades.Count; i++ )
                {
                    DysonUpgrade upgrade = dysonFac.DysonCompletedUpgrades[i];
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
                DysonSidekickFactionBaseInfo baseInfo = this.AttachedFleet.Faction.TryGetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
                return baseInfo.AvailableBlueprints.GetDisplayList().Count > 0;
            }
        }
        System.Collections.Generic.IEnumerable<IFleetTransformTarget> IFleetTransforms.TypesCanSwitchTo {
            get {
                DysonSidekickFactionBaseInfo baseInfo = this.AttachedFleet.Faction.TryGetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
                return baseInfo.AvailableBlueprints.GetDisplayList();
            }
        }

        GameCommand IFleetTransforms.CreateTransformCommand(GameCommandSource source, GameEntity_Squad centerpiece, string InternalName) {
            //GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransformDysonfleet], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            //command.RelatedEntityIDs.Add( centerpiece.PrimaryKeyID );
            //command.RelatedString2 = InternalName;
            //return command;
            return null;
        }

        GameEntityTypeData IFleetTransforms.GetTypeDataForName(string InternalName) {
            DysonUpgrade upgrade = DysonUpgradeTable.Instance.GetRowByName(InternalName);
            return GameEntityTypeDataTable.Instance.GetRowByName(upgrade?.RelatedShip.InternalName);
        }

        void IFleetTransforms.GetTooltip(ArcenCharacterBufferBase buffer)
        {
            buffer.Add("您可以花费精华点数来改变这艘旗舰的形态。");
            DysonSidekickFactionBaseInfo baseInfo = this.AttachedFleet.Faction.TryGetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
            if (baseInfo.AvailableBlueprints.GetDisplayList().Count == 0) {
                buffer.NewLine();
                buffer.Add("您可以通过裂隙或使用变形长老黑客技术找到蓝图。");
            }
        }
        #endregion
    }
}
