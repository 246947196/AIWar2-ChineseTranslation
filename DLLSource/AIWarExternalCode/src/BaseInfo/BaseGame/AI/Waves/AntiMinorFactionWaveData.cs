using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public interface IAntiMinorFactionWaveDataHolder
    {
        AntiMinorFactionWaveData GetAntiMinorFactionWaveData();
    }

    public class AntiMinorFactionWaveData
    {
        //Minor factions that want the AI to send waves against them should use this
        //to track data about the wave.
        public FInt currentWaveBudget;
        public FInt maxBudget;
        //Waves can be triggered on a Time basis (every X minutes) or a
        //Strength basis (whenever the budget reaches 10K); both are supported
        public FInt strengthForNextWave;
        public int timeForNextWave;

        public AntiMinorFactionWaveData()
        {
            Cleanup();
        }

        public void Cleanup()
        {
            currentWaveBudget = FInt.Zero;
            maxBudget = FInt.Zero;

            strengthForNextWave = FInt.Zero;
            timeForNextWave = 0;
        }

        public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddFInt( MetaData, this.currentWaveBudget, "currentWaveBudget" );
            Buffer.AddFInt( MetaData, this.maxBudget, "maxBudget" );
            Buffer.AddFInt( MetaData, this.strengthForNextWave, "strengthForNextWave" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.timeForNextWave, "timeForNextWave" );
        }

        public void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            currentWaveBudget = Buffer.ReadFInt( MetaData, "currentWaveBudget" );
            maxBudget = Buffer.ReadFInt( MetaData, "maxBudget" );
            strengthForNextWave = Buffer.ReadFInt( MetaData, "strengthForNextWave" );
            timeForNextWave = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "timeForNextWave" );
        }

        //Queues an AI wave of appropriate strength against this minor faction
        //returns the budget spent on the wave
        public static int QueueWave( Faction targetFaction, ArcenHostOnlySimContext Context, int budget, bool allowReconquest = false )
        {
            //First pick an AI faction
            Faction AIFaction = World_AIW2.GetRandomAIFaction( Context );
            AISentinelsFactionBaseInfo factionExternal = AIFaction.TryGetAISentinelsCoreData();
            if ( budget < 3000 )
                budget = 3000; //make sure we send something, and it doesn't have to be super well balanced here since it's against minor factions

            PlannedWaveOptions options = PlannedWaveOptions.CreateAntiMinorFactionWave( allowReconquest, FInt.Zero, targetFaction );
            PlannedWave wave = factionExternal.PlanWave_OrGetNull( Context, budget, options );
            options.ReturnToPool();

            if ( wave != null )
            {
                factionExternal.WaveList.Add( wave );
                return wave.aiCostBudgetForWave;
            }
            return 0;
        }
    }
}
