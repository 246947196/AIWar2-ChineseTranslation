using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public interface IExoDataHolder
    {
        ExoData GetExoData();
    }

    public class ExoData
    {
        public FInt CurrentExoStrength;
        public FInt StrengthRequiredForNextExo;
        public Int16 NumExosSoFar;
        //For example, the Origin Faction for golems would be the BrokenGolems faction,
        //but the exo spawn faction would be an AI faction
        public Int16 FactionIndexOfOriginFaction;
        public Int16 FactionIndexOfExoSpawnFaction;
        public string ExoReasonOverride; //instead of specifying the factionIndex and including that in the warning message, you can use a string instead
        public Int16 PercentToStartWarning; //set this to > 100 if you want to not allow any warnings
        public bool IsSyncingWithCPA; //an exo can sync with a CPA
        public bool IsSyncingWithWormholeInvasion; //an exo can sync with a Wormhole Invasion (TODO)
        public int OverrideLaunchTime; //if an exo is sync'd with a CPA, it now will launch at a specific time, not a charge %
        public int LastIncome;
        public int PercentWeSyncedWithCPA; //when we sync'd with the CPA record the percentage, and just never drop below that when reporting
        public int TimeTillLaunchAtInitialSync;

        public ExoData()
        {
            Cleanup();
        }

        public void Cleanup()
        {
            this.CurrentExoStrength = FInt.Zero;
            this.StrengthRequiredForNextExo = FInt.Zero;
            this.NumExosSoFar = 0;
            this.FactionIndexOfOriginFaction = 0;
            this.FactionIndexOfExoSpawnFaction = 0;
            this.PercentToStartWarning = -1;
            this.ExoReasonOverride = "";
            this.IsSyncingWithCPA = false;
            this.OverrideLaunchTime = -1;
            this.LastIncome = -1;
            this.PercentWeSyncedWithCPA = -1;
            this.IsSyncingWithWormholeInvasion = false;
            this.TimeTillLaunchAtInitialSync = -1;
        }

        public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddFInt( MetaData, this.CurrentExoStrength, "CurrentExoStrength" );
            Buffer.AddFInt( MetaData, this.StrengthRequiredForNextExo, "StrengthRequiredForNextExo" );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, this.NumExosSoFar, "NumExosSoFar" );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, this.FactionIndexOfOriginFaction, "FactionIndexOfOriginFaction" );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, this.FactionIndexOfExoSpawnFaction, "FactionIndexOfExoSpawnFaction" );
            Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, this.PercentToStartWarning, "PercentToStartWarning" );
            Buffer.AddString_Condensed( MetaData, this.ExoReasonOverride, "ExoReasonOverride" );
            Buffer.AddBool( MetaData, this.IsSyncingWithCPA, "IsSyncingWithCPA" );
            Buffer.AddBool( MetaData, this.IsSyncingWithWormholeInvasion, "IsSyncingWithWormholeInvasion" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.LastIncome, "Last exo income" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.PercentWeSyncedWithCPA, "cpa sync percent" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.OverrideLaunchTime, "Exo override launch time" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeTillLaunchAtInitialSync, "initial sync" );
        }

        public void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.CurrentExoStrength = Buffer.ReadFInt( MetaData, "CurrentExoStrength" );
            this.StrengthRequiredForNextExo = Buffer.ReadFInt( MetaData, "StrengthRequiredForNextExo" );
            this.NumExosSoFar = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "NumExosSoFar" );
            this.FactionIndexOfOriginFaction = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "FactionIndexOfOriginFaction" );
            this.FactionIndexOfExoSpawnFaction = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "FactionIndexOfExoSpawnFaction" );
            this.PercentToStartWarning = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1, "PercentToStartWarning" );
            this.ExoReasonOverride = Buffer.ReadString_Condensed( MetaData, "ExoReasonOverride" );
            this.IsSyncingWithCPA = Buffer.ReadBool( MetaData, "IsSyncingWithCPA" );
            this.IsSyncingWithWormholeInvasion = Buffer.ReadBool( MetaData, "IsSyncingWithWormholeInvasion" );
            this.LastIncome = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Last exo income" );
            this.PercentWeSyncedWithCPA = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "cpa sync percent" );
            this.OverrideLaunchTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "Exo override launch time" );
            this.TimeTillLaunchAtInitialSync = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "initial sync" );
        }

        public FInt GetPercentageCharged()
        {
            if ( this.CurrentExoStrength < FInt.Zero )
                this.CurrentExoStrength = FInt.Zero;

            //used to report the percentage charged this exo is.
            //If we are sync'd with a CPA, lie about it to make it look like we are charging "normally",
            //but really to time so we hit 100% when we would launch the exo
            if ( this.IsSyncingWithCPA || this.IsSyncingWithWormholeInvasion )
            {
                int secondsTillLaunch = this.OverrideLaunchTime - World_AIW2.Instance.GameSecond;
                int percentageFromSyncPointToFinish = 0;
                if ( this.TimeTillLaunchAtInitialSync == -1 )
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine( "init", Verbosity.DoNotShow );
                    this.TimeTillLaunchAtInitialSync = secondsTillLaunch;
                }
                if ( this.TimeTillLaunchAtInitialSync < secondsTillLaunch )
                    this.TimeTillLaunchAtInitialSync = secondsTillLaunch; //this shouldn't be possible, but sometimes it is, so this is a bandaid
                percentageFromSyncPointToFinish = 100 - (100 * secondsTillLaunch / this.TimeTillLaunchAtInitialSync);
                int remainingPercentage = 100 - this.PercentWeSyncedWithCPA; //we can't go below PercentWeSyncedWithCPA
                int percentOfRemaining = (percentageFromSyncPointToFinish * remainingPercentage) / 100;
                int output = this.PercentWeSyncedWithCPA + percentOfRemaining;
                //ArcenDebugging.ArcenDebugLogSingleLine("percentageFromSyncPointToFinish " + percentageFromSyncPointToFinish + " secondsTillLaunch " + secondsTillLaunch + " TimeTillLaunchAtInitialSync " + this.TimeTillLaunchAtInitialSync + " remainingPercentage " + remainingPercentage + " percentOfRemaining " + percentOfRemaining + " output " + output, Verbosity.DoNotShow );
                if ( output >= 100 )
                    output = 99; //never show 100%
                if ( output < 0 )
                    output = 0;

                return (FInt)(output);
            }
            FInt percent = (this.CurrentExoStrength * 100) / this.StrengthRequiredForNextExo;
            return percent;
        }

        public void UpdateExoStrength( FInt change )
        {
            this.CurrentExoStrength += change;
            this.LastIncome = change.IntValue;
            if ( (this.IsSyncingWithCPA || this.IsSyncingWithWormholeInvasion) &&
                 this.CurrentExoStrength > this.StrengthRequiredForNextExo )
                this.CurrentExoStrength = this.StrengthRequiredForNextExo;
        }
        public void SyncExoToCPAIfAllowed( Faction faction, ArcenHostOnlySimContext Context )
        {
            //if this is a suitable exo (ie against players, and already mostly charged).
            //allow it to sync with any CPAs coming soon. Don't call this if you don't want this exo to sync with AI things
            //NOTE: If you call this you must call ResetSync after you send the sync'd

            //If you want to also allow exos to sync with anything else (waves?) then
            //just follow the pattern used for CPA and wormhole invasions. Note you must also
            //update the text in the Exo Notifier class (in Human.cs) to say what we are sync'ing with
            //for the Notification
            bool debug = false;
            if ( this.IsSyncingWithCPA || this.IsSyncingWithWormholeInvasion )
                return; //we are already sync'd with a CPA
            FInt lowcutoff = FInt.FromParts( 0, 500 );
            FInt highcutoff = FInt.FromParts( 0, 800 );
            FInt strengthRatio = this.CurrentExoStrength / this.StrengthRequiredForNextExo;
            if ( strengthRatio < lowcutoff || strengthRatio > highcutoff )
            {
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Exo not in right range; " + strengthRatio + " at " + World_AIW2.Instance.GameSecond + ".", Verbosity.DoNotShow );
                return; //we need to be "pretty damn strong" before we even think about launching early
            }

            //
            int CPASyncRange = 600;
            for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
            {
                Faction syncFaction = World_AIW2.Instance.AIFactions[i];
                if ( syncFaction.FactionIsDefeated )
                    continue;
                AISentinelsCoreData factionExternal = syncFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
                if ( factionExternal == null )
                    continue;
                if ( factionExternal.AIDifficulty.Difficulty < 7 )
                    continue; //no sync'ing with AIs at difficulty < 7
                int nextCPA = factionExternal.NextEventTime[AIBudgetType.CPA];
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "TimeForNextCPA: " + nextCPA + ", " + (nextCPA - World_AIW2.Instance.GameSecond), Verbosity.DoNotShow );
                if ( nextCPA - World_AIW2.Instance.GameSecond < CPASyncRange )
                {
                    this.PercentWeSyncedWithCPA = (this.GetPercentageCharged()).IntValue; //we never allow the percentage to drop, since that would make it obvious we sync'd
                    this.IsSyncingWithCPA = true;
                    this.OverrideLaunchTime = nextCPA + AICrossPlanetAttackerBaseInfo.CPA_DELAY_TIME; //CPAs spawn 10 minutes after the budget it spent
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Sync me. My new override launch time is " + this.OverrideLaunchTime + ", and current time is " + World_AIW2.Instance.GameSecond, Verbosity.DoNotShow );
                    return;
                }
            }
            //now also allow syncing to wormhole invasions
            //note that the Wormhole Invasion units don't appear for a while, so we need to wait for a bit into the Invasion
            int WormholeInvasionSyncRange = 600 + ExternalConstants.Instance.WormholeInvasionWarningTime;
            for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
            {
                Faction syncFaction = World_AIW2.Instance.AIFactions[i];
                if ( syncFaction.FactionIsDefeated )
                    continue;
                AISentinelsCoreData factionExternal = syncFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
                if ( factionExternal == null )
                    continue;
                if ( factionExternal.AIDifficulty.Difficulty < 7 )
                    continue; //no sync'ing with AIs at difficulty < 7
                if ( GlobalAIWorldBaseInfo.Instance.AIProgress_Effective < factionExternal.AIDifficulty.AIPUnlockWormholeInvasion )
                    continue; //if we are too low AIP to get wormhole invasions, don't bother syncing

                int nextWormholeInvasion = factionExternal.NextEventTime[AIBudgetType.WormholeInvasion];
                if ( debug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "TimeForNextWormhole: " + nextWormholeInvasion + ", " + (nextWormholeInvasion - World_AIW2.Instance.GameSecond), Verbosity.DoNotShow );

                if ( nextWormholeInvasion - World_AIW2.Instance.GameSecond < WormholeInvasionSyncRange )
                {
                    this.PercentWeSyncedWithCPA = (this.GetPercentageCharged()).IntValue; //we never allow the percentage to drop, since that would make it obvious we sync'd
                    this.IsSyncingWithWormholeInvasion = true;
                    this.OverrideLaunchTime = nextWormholeInvasion + ExternalConstants.Instance.WormholeInvasionWarningTime; //launch a bit after the wormhole invasion starts
                    if ( debug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Sync me. My new WI override launch time is " + this.OverrideLaunchTime + ", and current time is " + World_AIW2.Instance.GameSecond, Verbosity.DoNotShow );
                    return;
                }
            }
        }
        public void ResetSync()
        {
            //any faction that allows a sync with anything must call ResetSync
            //after sending any sync'd exo. It's safe to call this on every Exo if that's easier
            this.OverrideLaunchTime = -1; //just in case
            this.IsSyncingWithCPA = false;
            this.IsSyncingWithWormholeInvasion = false;
        }
        public bool ShouldLaunchExo()
        {
            if ( this.IsSyncingWithCPA && //rule for syncing
                 this.OverrideLaunchTime <= World_AIW2.Instance.GameSecond )
            {
                return true; //this is for syncing CPAs with Exos
            }
            if ( !this.IsSyncingWithCPA &&
                 this.CurrentExoStrength >= this.StrengthRequiredForNextExo )
            {
                return true;
            }

            return false;
        }
    }
}
