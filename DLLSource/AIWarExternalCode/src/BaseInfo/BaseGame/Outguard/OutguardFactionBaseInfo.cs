using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class OutguardFactionBaseInfo : ExternalFactionBaseInfoRoot, IExternalBaseInfo_Singleton
    {
        //serialized
        public bool hasBeenInitializedHostOnly;
        //this is set by the sim to create a new outguard group on the host
        public readonly ProtectedList<OutguardSpawnRequest> QueuedRequests = ProtectedList<OutguardSpawnRequest>.Create_WillNeverBeGCed( 30, "OutguardFactionBaseInfo-queuedRequestsOnHostOnly" );

        //not serialized
        public static OutguardFactionBaseInfo Instance; //there can only ever be one of this faction at a time

        public OutguardFactionBaseInfo()
        {
            QueuedRequests.IsHardened = true;
        }

        protected override void Cleanup()
        {
            hasBeenInitializedHostOnly = false;
            QueuedRequests.Clear( true );

            Instance = null;
            hasBeenInitializedHostOnly = false; //get it to set things up properly!
        }

        #region SetStartingFactionRelationships
        public override void SetStartingFactionRelationships()
        {
            base.SetStartingFactionRelationships();
            AllegianceHelper.AllyThisFactionToHumans( this.AttachedFaction );
        }
        #endregion

        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddBool( MetaData, this.hasBeenInitializedHostOnly, "hasBeenInitializedHostOnly" );

            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.QueuedRequests.Count, "QueuedRequests.Count" );
            for ( int i = 0; i < this.QueuedRequests.Count; i++ )
                this.QueuedRequests[i].SerializeTo( MetaData, Buffer, SerializationCmdType );
        }
        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.hasBeenInitializedHostOnly = Buffer.ReadBool( MetaData, "hasBeenInitializedHostOnly" );

            int numRequests = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "QueuedRequests.Count" );
            this.QueuedRequests.DeserializeUncertainNumberOfEntriesIntoExistingList( numRequests,
                delegate { return OutguardSpawnRequest.GetFromPoolOrCreate(); },
                delegate ( OutguardSpawnRequest Request ) { Request.DeserializedIntoSelf( MetaData, Buffer, SerializationCmdType ); } );
        }

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return -1; //not relevant for outguard
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            //Chris says: these are always here, don't tell us about this
            return 0;
        }

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            Instance = this;

            //So that the displayed debug strength values apply.
            FInt currentAIP = FactionUtilityMethods.Instance.GetCurrentAIP();
            for ( int i = 0; i < OutguardGroupDataTable.Instance.Rows.Count; i++ )
                OutguardGroupDataTable.Instance.Rows[i].AdjustCountsToAIP( currentAIP );
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            //ConfigurationForFaction cfg = this.AttachedFaction.Config;
            //Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
        }
        #endregion

        #region GetShouldAttackNormallyExcludedTarget
        public override bool GetShouldAttackNormallyExcludedTarget( GameEntity_Squad Target )
        {
            if ( Target.TypeData.GetHasTag( "DysonAntagonizer" ) ||
                Target.TypeData.GetHasTag( "WarpingInDysonAntagonizer" ) )
                return true;
            return false;
        }
        #endregion

        #region AddNewGroupToBeacon_HostOnly
        public void AddNewGroupToBeacon_HostOnly( Planet planet, OutguardGroupData group )
        {
            if ( group == null )
                return;
            bool debug = false;

            if ( planet.OutguardBeaconState == null )
                planet.OutguardBeaconState = OutguardBeaconStateForPlanet.GetFromPoolOrCreate();

            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Adding group " + group.InternalName + " to planet  " + planet.Name, Verbosity.DoNotShow );
            planet.OutguardBeaconState.AddNewGroup( group );
        }
        #endregion

        #region CheatHackAllBeacons_HostOnly
        public void CheatHackAllBeacons_HostOnly()
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;

            World_AIW2.Instance.EnsureSufficientOutguardStates();
            ProtectedList<OutguardInfo> outguardStates = World_AIW2.Instance.OutguardStates;
            for ( int x = 0; x < outguardStates.Count; x++ )
            {
                OutguardInfo state = outguardStates[x];
                state.HasBeenContacted = true;
                state.WasKilled = false;
            }

            foreach ( GameEntity_Squad beacon in this.AttachedFaction.Squads( OutguardBeaconStateForPlanet.OutguardBeaconTag ) )
            {
                Faction facOrNull = beacon.GetFactionOrNull_Safe();
                if ( facOrNull == null )
                    continue;

                OutguardBeaconStateForPlanet state = World_AIW2.Instance.GetPlanetByIndex( beacon.Planet.Index )?.OutguardBeaconState;
                if ( state == null )
                    continue;

                foreach ( OutguardGroupData outguardtype in OutguardGroupDataTable.Instance.Rows )
                {
                    if ( !state.GroupsThatCanHire.Contains( outguardtype ) )
                        state.GroupsThatCanHire.Add( outguardtype );
                }

                state.DoHack();
            }
        }
        #endregion

        #region ContactOutguardGroup_NonBeaconSource
        public void ContactOutguardGroup_NonBeaconSource( string groupName )
        {
            OutguardGroupData groupData = OutguardGroupDataTable.Instance.GetRowByName( groupName );
            if ( groupData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( $"Failed to find Outguard Group Data with a group name of {groupName}.", Verbosity.ShowAsError );
                return;
            }
            World_AIW2.Instance.GetOutguardState( groupData ).HasBeenContacted = true;
        }
        #endregion

        #region LoseContactWithOutguardGroup_NonBeaconSource
        public void LoseContactWithOutguardGroup_NonBeaconSource( string groupName )
        {
            OutguardGroupData groupData = OutguardGroupDataTable.Instance.GetRowByName( groupName );
            if ( groupData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( $"Failed to find Outguard Group Data with a group name of {groupName}.", Verbosity.ShowAsError );
                return;
            }
            World_AIW2.Instance.GetOutguardState( groupData ).HasBeenContacted = false;
        }
        #endregion

        #region SetOutguardBeaconToHacked
        public void SetOutguardBeaconToHacked( GameEntity_Squad beacon, ArcenHostOnlySimContext Context, GameEntity_Squad Hacker, HackingEvent Event )
        {
            if ( Context == null )
                return; //client
            OutguardBeaconStateForPlanet state = World_AIW2.Instance.GetPlanetByIndex( beacon.Planet.Index )?.OutguardBeaconState;

            //remove any options not matching our hack!
            for ( int i = state.GroupsThatCanHire.Count - 1; i >= 0; i-- )
            {
                var group = state.GroupsThatCanHire[i];

                if ( group.InternalName != Event.RelatedStringOrNull )
                    state.GroupsThatCanHire.RemoveAt( i );
                else
                {
                    if ( !World_AIW2.Instance.GetOutguardState( group ).HasBeenContacted )
                    {
                        var cost = (FInt) group.AIPCostOnContact;
                        GlobalAIWorldBaseInfo.Instance.ChangeAIP( cost, AIPChangeReason.Hacking, beacon.TypeData, Hacker.GetFactionIndex_Safe(),
                            beacon.Planet.Index, beacon.GetFactionIndex_Safe() );
                        Event.AIPchanged += cost;
                    }
                }
            }

            GameEntityTypeData hackedBeaconData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, OutguardBeaconStateForPlanet.HackedOutguardBeaconTag );
            beacon.TransformInto( Context, hackedBeaconData, 1, false );
            state.DoHack();
        }
        #endregion

        public override void DoPerSimStepLogic_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )//since for hosts and single player the Outguard are spawned in deep info, don't preemptively delete it!
            {
                for ( int i = QueuedRequests.Count - 1; i >= 0; i-- )
                {
                    if ( QueuedRequests[i].SpawnSecond < World_AIW2.Instance.GameSecond )
                        QueuedRequests.RemoveAt( i, true );
                }
            }
        }
    }
}
