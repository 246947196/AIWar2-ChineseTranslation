using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ArmadaMobileFleetBaseInfo : FleetMetricsBaseInfo, IFleetTransforms
    {
        //Serialized
        //---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------
        public readonly List<ArmadaUpgrade> ArmadaCompletedUpgrades = List<ArmadaUpgrade>.Create_WillNeverBeGCed( 30, "ArmadaMobileFleetBaseInfo-ArmadaCompletedUpgrades" );

        //Non-Serialized
        //---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------
        //These are used for auto-defend mode
        public bool NeedsToRebuild; //I've taken losses and need to go to a shipyard
        public bool NeedsToRetreat; //My planet has too many enemies
        public bool NeedsToDefendKing; //needs to defend our allied king
        public ArmadaMobileFleetBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            ArmadaCompletedUpgrades.Clear();
            this.ResetAggregateUpgradeData();
        }

        public void ResetAggregateUpgradeData()
        {
        }

        public void SwitchAggregateUpgradeDataToDisplay() {
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "ArmadaMobileFleetBaseInfo" );

            if ( this.ArmadaCompletedUpgrades == null )
            {
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, 0, "ArmadaUpgradeIndices" );
            }
            else
            {
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.ArmadaCompletedUpgrades.Count, "ArmadaUpgradeIndices" );
                for ( int i = 0; i < this.ArmadaCompletedUpgrades.Count; i++ )
                    Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.ArmadaCompletedUpgrades[i].Index, "ArmadaUpgradeIndex" );
            }

            SerializeAllMetrics( MetaData, Buffer, SerializationCmdType );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "ArmadaMobileFleetBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "ArmadaMobileFleetBaseInfo Ext", TrackerStyle.ByTypeOnly );

            int count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "ArmadaUpgradeIndices" );
            this.ArmadaCompletedUpgrades.Clear();
            for ( int i = 0; i < count; i++ )
                this.ArmadaCompletedUpgrades.Add( ArmadaUpgradeTable.Instance.GetRowByIndex( (Int32)Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "ArmadaUpgradeIndex" ) ) );

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
            if ( this.ArmadaCompletedUpgrades != null &&
                 this.ArmadaCompletedUpgrades.Count > 0 )
            {
                //process fleet specific upgrades
                for ( int i = 0; i < this.ArmadaCompletedUpgrades.Count; i++ )
                {
                    ArmadaUpgrade upgrade = this.ArmadaCompletedUpgrades[i];
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
            ArmadaFactionBaseInfo dysonFac = null;
            if ( fac != null )
                dysonFac = fac.GetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
            if ( dysonFac != null && dysonFac.ArmadaCompletedUpgrades.Count > 0 )
            {
                //process faction wide updates
                for ( int i = 0; i < dysonFac.ArmadaCompletedUpgrades.Count; i++ )
                {
                    ArmadaUpgrade upgrade = dysonFac.ArmadaCompletedUpgrades[i];
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
                ArmadaFactionBaseInfo baseInfo = this.AttachedFleet.Faction.TryGetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
                return baseInfo.AvailableBlueprints.GetDisplayList().Count > 0;
            }
        }
        System.Collections.Generic.IEnumerable<IFleetTransformTarget> IFleetTransforms.TypesCanSwitchTo {
            get {
                ArmadaFactionBaseInfo baseInfo = this.AttachedFleet.Faction.TryGetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
                return baseInfo.AvailableBlueprints.GetDisplayList();
            }
        }

        GameCommand IFleetTransforms.CreateTransformCommand(GameCommandSource source, GameEntity_Squad centerpiece, string InternalName) {
            //GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransformArmadafleet], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            //command.RelatedEntityIDs.Add( centerpiece.PrimaryKeyID );
            //command.RelatedString2 = InternalName;
            //return command;
            return null;
        }

        GameEntityTypeData IFleetTransforms.GetTypeDataForName(string InternalName) {
            ArmadaUpgrade upgrade = ArmadaUpgradeTable.Instance.GetRowByName(InternalName);
            return GameEntityTypeDataTable.Instance.GetRowByName(upgrade?.RelatedShip.InternalName);
        }

        void IFleetTransforms.GetTooltip(ArcenCharacterBufferBase buffer)
        {
            buffer.Add("You can spend essence points to change the form of this flagship.");
            ArmadaFactionBaseInfo baseInfo = this.AttachedFleet.Faction.TryGetExternalBaseInfoAs<ArmadaFactionBaseInfo>();
            if (baseInfo.AvailableBlueprints.GetDisplayList().Count == 0) {
                buffer.NewLine();
                buffer.Add("You can find blueprints from rifts or by using the transform elderling hack.");
            }
        }
        #endregion
    }
}
