using System;

using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;

public static class WaveUtils
{
    public static int GetCountOfAllWaves()
    {
        int countOfWaves = 0;
        foreach ( Faction faction in World_AIW2.Instance.Factions )
        {
            if ( faction.Type != FactionType.AI )
                continue;

            ProtectedList<PlannedWave> queuedWaves = faction.GetAISentinelsCoreData()?.WaveList;
            if ( queuedWaves != null )
                countOfWaves += queuedWaves.Count;
        }
        return countOfWaves;
    }

    public static void ClearAllWaves()
    {
        foreach ( Faction faction in World_AIW2.Instance.Factions )
        {
            if ( faction.Type != FactionType.AI )
                continue;

            ProtectedList<PlannedWave> queuedWaves = faction.GetAISentinelsCoreData()?.WaveList;
            if ( queuedWaves != null )
                queuedWaves.Clear( true );
        }
    }

    //Read-only foreach sibling of the (now-retired) DFKnownWavesAgainstHumanWorlds.
    //Yields each PlannedWave that is being alerted to the player, after performing the
    //same NonSimIsAgainstAHumanHomeworld / NonSimPlanetName side-effects the old wrapper
    //did before invoking the processor. Zero allocations, so callers can use a plain
    //foreach with outer locals in scope instead of a capturing delegate.
    public static KnownWavesAgainstHumanWorldsEnumerable KnownWavesAgainstHumanWorlds => new KnownWavesAgainstHumanWorldsEnumerable();

    public readonly struct KnownWavesAgainstHumanWorldsEnumerable
    {
        public Enumerator GetEnumerator() => new Enumerator();

        public struct Enumerator
        {
            private int factionIndex;
            private int waveIndex;
            private ProtectedList<PlannedWave> currentWaveList;
            private PlannedWave current;
            public PlannedWave Current => this.current;
            public bool MoveNext()
            {
                try
                {
                    var factions = World_AIW2.Instance.Factions;
                    int factionCount = factions.Count;
                    while ( true )
                    {
                        //if we have a wave list, advance through it
                        if ( this.currentWaveList != null )
                        {
                            int waveCount = this.currentWaveList.Count;
                            while ( this.waveIndex < waveCount )
                            {
                                PlannedWave wave = this.currentWaveList[this.waveIndex++];
                                if ( wave == null || !wave.playerBeingAlerted )
                                    continue;
                                wave.NonSimIsAgainstAHumanHomeworld = false;
                                if ( !wave.isActuallyACrossPlanetAttack )
                                {
                                    Planet wavePlanetOrNull = World_AIW2.Instance.GetPlanetByIndex( wave.targetPlanetIdx );
                                    wave.NonSimIsAgainstAHumanHomeworld =
                                        ( wavePlanetOrNull != null &&
                                          wavePlanetOrNull.PopulationType == PlanetPopulationType.HumanHomeworld );
                                    if ( wave.NonSimPlanetName.Length == 0 )
                                        wave.NonSimPlanetName = ( wavePlanetOrNull == null ? "null" : wavePlanetOrNull.Name );
                                }
                                this.current = wave;
                                return true;
                            }
                            this.currentWaveList = null;
                            this.waveIndex = 0;
                        }

                        //find the next faction with a non-empty wave list
                        while ( this.factionIndex < factionCount )
                        {
                            Faction faction = factions[this.factionIndex++];
                            if ( faction == null || faction.Type != FactionType.AI )
                                continue;
                            var coreData = faction.TryGetAISentinelsCoreData();
                            if ( coreData == null )
                                continue;
                            ProtectedList<PlannedWave> queuedWaves = coreData.WaveList;
                            if ( queuedWaves == null || queuedWaves.Count <= 0 )
                                continue;
                            this.currentWaveList = queuedWaves;
                            this.waveIndex = 0;
                            break;
                        }
                        if ( this.currentWaveList == null )
                        {
                            this.current = null;
                            return false;
                        }
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.LogSingleLine( "Hit exception in KnownWavesAgainstHumanWorlds enumerator: " + e, Verbosity.DoNotShow );
                    this.current = null;
                    return false;
                }
            }
        }
    }

    public static ArcenCharacterBufferBase WriteWaveStrength( this ArcenCharacterBufferBase buffer, AbstractWaveBase wave )
    {
        Faction SendingFaction = World_AIW2.Instance.GetFactionByIndex( wave.SendingFactionIndex );
        if ( wave.isActuallyACrossPlanetAttack )
        {
            buffer.Add( "跨星攻击\n", SendingFaction.FactionCenterColor.ColorHexBrighter );
            buffer.Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon );
            buffer.Add( "???" );
        }
        else
        {
            int strengthAsInt = wave.CalculateStrengthOfWave( SendingFaction );
            buffer.Add( ArcenExternalUIUtilities.GUI_StrengthTextColorAndIcon );
            ArcenExternalUIUtilities.WriteRoundedNumberWithSuffix( buffer, strengthAsInt, true, true );
        }

        return buffer;
    }

    public static ArcenCharacterBufferBase WriteWaveSecondsRemaining( this ArcenCharacterBufferBase buffer, AbstractWaveBase wave )
    {
        int secondsRemaining = wave.gameTimeInSecondsForLaunchWave - World_AIW2.Instance.GameSecond;
        if ( secondsRemaining > 0 ) 
            return buffer.AddSecondsRemaining( secondsRemaining );
        else
            return buffer.Add( "即将" );
    }
}
