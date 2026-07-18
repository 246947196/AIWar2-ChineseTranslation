using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public enum InvasionState : byte
    {
        Suppressed, //The invasion is not allowed to happen yet
        FirstStep, //First sim step; seed the Hive, then devastate the Hive planet's defenses
        InProgress, //The Nanocaust gets special bonuses until it has captured a few extra planets
        Normal, //Normal State, no extra bonuses
    }

    public enum ReinvadeFrequencySetting
    {
        Never,
        ApproxOneHour,
        Approx30Minutes
    }

    public class NanocaustFactionBaseInfo : ExternalFactionBaseInfoRoot, IAntiMinorFactionWaveDataHolder
    {
        /* TEACHING_MOMENT: Reminder for modders: FInt is a fixed point integer. Arcen uses it
           because floating point math isn't always consistent across OS/Arch for multiplayer.
           It is critical not to use Floating point values in sim code. Always use FIint
           To create one, say FInt.FromParts( <portion greater than 1>, <decimal portion> )
           So FInt.FromParts(0, 500) == 0.5 and FInt.FromParts(100, 200) == 100.2
           Best practice suggests always making the decimal portion precisely 3 digits. */

        public bool debug = false;

        #region Serialized
        // Serialized
        public bool humanVision = false; //have the humans spotted the Nanocaust yet?
        public bool humanEncounter = false; //have the humans and the nanocaust started fighting yet? Currently unused
        public bool humanAllied = false; //whether this Nanocaust is allied to the player
        public bool aiAllied = false; //whether this Nanocaust is allied to the AI
        public bool IsInFireteamMode = false;
        public readonly ArcenLessLinkedList<Fireteam> Teams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "NanocaustFactionBaseInfo-Teams" );
        public readonly AntiMinorFactionWaveData WaveData = new AntiMinorFactionWaveData();
        public InvasionState state = InvasionState.Suppressed;
        public int numFleetsSent = 0;
        public int numHumanAttacks;
        public bool HonorTimeBasedNanocaustPlanetLimit;
        public int NanocaustInitialInvasionPlanets; //this value is only used when HonorTimeBasedNanocaustPlanetLimit is true

        public int LastAIExoTime; //the AI will periodically launch large scale attacks on the Nanocaust
        //The progress modification code is a bit of a hack, but it's intended as a workaround until Keith can work
        //his magic on things
        public int NanocaustSpecificAIP; //every time the nanocaust takes a planet, we need to adjust the progress a bit to take
                                         //that into account (otherwise the nanocaust jacks the progress up really high

        public FInt CurrentMetalStored; //Needs to be serialized
        public int NextTimeToSpend;
        public readonly Dictionary<Planet, int> LastTimePlanetHadCenter = Dictionary<Planet, int>.Create_WillNeverBeGCed( 500, "NanocaustFactionBaseInfo-LastTimePlanetHadCenter" );
        #endregion end Serialized

        #region NonSerialized
        //Nonserialized
        public static readonly DoubleBufferedList<SafeSquadWrapper> CrossFaction_AllHives = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 30, "Nanocaust-CrossFaction_AllHives" );
        private static bool working_HaveCrossFaction_AllHivesBeenClearedYetThisCycle = false;
        private static bool working_HaveCrossFaction_AllHivesBeenSwappedYetThisCycle = false;
        public static readonly List<NanocaustFactionBaseInfo> CrossFaction_AllNanocaustFactions = List<NanocaustFactionBaseInfo>.Create_WillNeverBeGCed( 8, "NanocaustFactionBaseInfo-CrossFaction_AllNanocaustFactions" );
        public DoubleBufferedValue<SafeSquadWrapper> Hive = new DoubleBufferedValue<SafeSquadWrapper>( SafeSquadWrapper.Create( null ) );
        public bool hasBeenHacked = false;
        public bool SeedNearPlayer = false;
        public int Intensity = 0;
        public bool SpawnPlanet = false;
        public ReinvadeFrequencySetting ReinvadeFrequency;

        /* Timing related fields. All times are in seconds */

        //The initialization value is the first time a frenzy happens. This value is updated
        //after each frenzy to know when to go next
        public int nanobotLifespan;

        /* These lists are used throughout the code to keep track of state */
        public readonly DoubleBufferedList<SafeSquadWrapper> NanobotCenters = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Nanocaust-nanobotCenters" );
        public readonly DoubleBufferedList<SafeSquadWrapper> WarpingInNanobotCenters = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 300, "Nanocaust-warpingInNanobotCenters" );
        /* Note that you can exceed the Strength cap due to other spawning mechanisms
           that don't care about the limit, like Bonus Strength for a fleet*/
        public readonly DoubleBufferedDictionary<Int64, FInt> StrengthPerNanobotCenter = DoubleBufferedDictionary<Int64, FInt>.Create_WillNeverBeGCed( 300, "Nanocaust-strengthPerNanobotCenter" );
        public readonly DoubleBufferedList<Planet> QuiescedPlanets = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 300, "Nanocaust-quiescedPlanets" ); //planets with a nanobot center and no uninfectedPlanets adjoining it
        public readonly DoubleBufferedList<Planet> ActivePlanets = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 300, "Nanocaust-activePlanets" ); //planets with a nanobot center and uninfectedPlanets adjoining it
        public readonly DoubleBufferedList<Planet> UninfectedPlanets = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 300, "Nanocaust-uninfectedPlanets" ); //planets without a nanobot center adjoining a planet with a nanobot center
                                                                                                           //this list is used to select potential targets for attacking.
                                                                                                           //having duplicate entries on this list is fine (it increases the likelihood
                                                                                                           //of attacking that planet
        public readonly DoubleBufferedList<Planet> InfectedPlanets = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 300, "Nanocaust-infectedPlanets" );
        #endregion end NonSerialized


        public const string NANOCAUST_TAG = "NanobotCenter"; //all nanobot centers match this
        public const string NANOCAUST_HIVE = "NanobotHive"; //this is for the initial seeding
        public const string NANOCAUST_HACKED_HIVE = "NanobotHackedHive"; //this is for the initial seeding

        #region Custom Xml Data
        private bool HaveLoadedData;
        public int secondsBetweenSimUpdates = 0;
        public int secondsBetweenInitialInvasionSpawns_IfOnlyOnePlanet = 0;
        public int secondsBetweenInitialInvasionSpawns_Otherwise = 0;

        public int timeForMarkIIUpgrade = 0;
        public int timeForMarkIIIUpgrade = 0;
        public bool EnableAttrition = true;
        public bool EnableBonusShipsForEarlyInvasion = true;
        public int humanStrengthForCapture = 0;
        public int AIStrengthForCapture = 0;
        public int maxNanobotLifespan = 0;
        public int minNanobotLifespan = 0;
        public FInt BaseWaveBudgetPerMinute = FInt.Zero;
        public FInt MinWaveSize = FInt.Zero;
        public int WaveIntervalInMinutes = 0;
        public FInt MaxStrengthMark1Center;
        public FInt MaxStrengthMark2Center;
        public FInt MaxStrengthMark3Center;
        public FInt MaxStrengthHive;
        public FInt MetalIncomePerSecondMark1Center;
        public FInt MetalIncomePerSecondMark2Center;
        public FInt MetalIncomePerSecondMark3Center;
        public FInt MetalIncomePerSecondHive;
        public FInt MaxMetalStoragePerNanobotCenter;

        public FInt multiplierIntensity1;
        public FInt multiplierIntensity2;
        public FInt multiplierIntensity3;
        public FInt multiplierIntensity4;
        public FInt multiplierIntensity5;
        public FInt multiplierIntensity6;
        public FInt multiplierIntensity7;
        public FInt multiplierIntensity8;
        public FInt multiplierIntensity9;
        public FInt multiplierIntensity10;

        public FInt initialAttackStrength;
        public int earlyGamePlayerProtection;
        public readonly List<int> maxStrengthForNanobotLevel = List<int>.Create_WillNeverBeGCed( 20, "NanocaustFactionBaseInfo-maxStrengthForNanobotLevel" );

        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedData )
                return;
            this.HaveLoadedData = true;

            this.maxStrengthForNanobotLevel.Clear();
            this.maxStrengthForNanobotLevel.Add( ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_nanocaust_maxStrengthOnHivePlanet" ) );
            this.maxStrengthForNanobotLevel.Add( ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_nanocaust_maxStrengthOnMarkOnePlanet" ) );
            this.maxStrengthForNanobotLevel.Add( ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_nanocaust_maxStrengthOnMarkTwoPlanet" ) );
            this.maxStrengthForNanobotLevel.Add( ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_nanocaust_maxStrengthOnMarkThreePlanet" ) );
            HonorTimeBasedNanocaustPlanetLimit = ExternalConstants.Instance.GetCustomBool_Slow( "custom_bool_nanocaust_timebasedplanetlimit" );
            secondsBetweenSimUpdates = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_nanocaust_secondsBetweenNanocaustSimUpdates" );
            secondsBetweenInitialInvasionSpawns_IfOnlyOnePlanet = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_nanocaust_SecondsBetweenInitialInvasionSpawns_IfOnlyOnePlanet" );
            secondsBetweenInitialInvasionSpawns_Otherwise = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_nanocaust_SecondsBetweenInitialInvasionSpawns_Otherwise" );

            timeForMarkIIUpgrade = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_nanocaust_timeForMarkIIUpgrade" );
            timeForMarkIIIUpgrade = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_nanocaust_timeForMarkIIIUpgrade" );
            EnableAttrition = ExternalConstants.Instance.GetCustomBool_Slow( "custom_bool_nanocaust_EnableAttrition" );
            EnableBonusShipsForEarlyInvasion = ExternalConstants.Instance.GetCustomBool_Slow( "custom_bool_nanocaust_EnableBonusShipsForEarlyInvasion" );
            humanStrengthForCapture = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_nanocaust_humanStrengthForCapture" );
            AIStrengthForCapture = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_nanocaust_AIStrengthForCapture" );
            maxNanobotLifespan = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_nanocaust_maxNanobotLifespan" );
            minNanobotLifespan = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_nanocaust_minNanobotLifespan" );
            BaseWaveBudgetPerMinute = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_BaseWaveBudgetPerMinute" );
            MinWaveSize = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_MinWaveSize" );
            WaveIntervalInMinutes = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_nanocaust_WaveIntervalInMinutes" );
            MaxMetalStoragePerNanobotCenter = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_MaxMetalStoragePerNanobotCenter" );

            this.MaxStrengthMark1Center = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_MaxStrengthMark1Center" );
            this.MaxStrengthMark2Center = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_MaxStrengthMark2Center" );
            this.MaxStrengthMark3Center = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_MaxStrengthMark3Center" );
            this.MaxStrengthHive = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_MaxStrengthHive" );
            this.MetalIncomePerSecondMark1Center = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_MetalIncomePerSecondMark1Center" );
            this.MetalIncomePerSecondMark2Center = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_MetalIncomePerSecondMark2Center" );
            this.MetalIncomePerSecondMark3Center = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_MetalIncomePerSecondMark3Center" );
            this.MetalIncomePerSecondHive = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_MetalIncomePerSecondHive" );
            this.multiplierIntensity1 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_multiplierIntensity1" );
            this.multiplierIntensity2 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_multiplierIntensity2" );
            this.multiplierIntensity3 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_multiplierIntensity3" );
            this.multiplierIntensity4 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_multiplierIntensity4" );
            this.multiplierIntensity5 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_multiplierIntensity5" );
            this.multiplierIntensity6 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_multiplierIntensity6" );
            this.multiplierIntensity7 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_multiplierIntensity7" );
            this.multiplierIntensity8 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_multiplierIntensity8" );
            this.multiplierIntensity9 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_multiplierIntensity9" );
            this.multiplierIntensity10 = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_multiplierIntensity10" );
            this.initialAttackStrength = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_nanocaust_initialAttackStrength" );
            this.earlyGamePlayerProtection = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_nanocaust_earlyGamePlayerProtection" );
        }
        #endregion

        public NanocaustFactionBaseInfo()
        {
            if ( this.debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "NanocaustFactionBaseInfoConstructor(): <no arguments>", Verbosity.DoNotShow );
        }

        #region Cleanup
        protected override void Cleanup()
        {
            //serialized
            humanVision = false;
            humanEncounter = false;
            humanAllied = false;
            aiAllied = false;
            IsInFireteamMode = false;
            Teams.Clear();
            WaveData.Cleanup();
            state = InvasionState.Suppressed;
            numFleetsSent = 0;
            numHumanAttacks = 0;
            HonorTimeBasedNanocaustPlanetLimit = false;
            NanocaustInitialInvasionPlanets = -1;
            LastAIExoTime = -1;
            NanocaustSpecificAIP = 0;
            CurrentMetalStored = FInt.Zero;
            NextTimeToSpend = 0;
            this.LastTimePlanetHadCenter.Clear();

            //nonserialized
            CrossFaction_AllHives.Clear();
            CrossFaction_AllNanocaustFactions.Clear();
            working_HaveCrossFaction_AllHivesBeenClearedYetThisCycle = false;
            working_HaveCrossFaction_AllHivesBeenSwappedYetThisCycle = false;
            Hive.Clear();
            hasBeenHacked = false;
            SeedNearPlayer = false;
            Intensity = 0;

            nanobotLifespan = 0;

            this.NanobotCenters.Clear();
            this.WarpingInNanobotCenters.Clear();
            this.StrengthPerNanobotCenter.Clear();
            this.ActivePlanets.Clear();
            this.QuiescedPlanets.Clear();
            this.UninfectedPlanets.Clear();
            this.InfectedPlanets.Clear();

            HaveLoadedData = false; //trigger a reload of xml
        }
        #endregion

        #region Ser / Deser
        public override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "NanocaustFactionBaseInfo" );
            //Things that need to get serialized: Fleet objects. time and strength modifiers.
            //hasHumanVision, humanEncounter, NanocaustSpecificAIP, totalPlanetsEverTaken*/
            Buffer.AddBool( MetaData, this.humanVision, "humanVision" );
            Buffer.AddBool( MetaData, this.humanEncounter, "humanEncounter" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.NanocaustSpecificAIP, "NanocaustSpecificAIP" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.numFleetsSent, "numFleetsSent" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.NanocaustInitialInvasionPlanets, "NanocaustInitialInvasionPlanets" );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.LastAIExoTime, "LastAIExoTime" );
            Buffer.AddFInt( MetaData, this.CurrentMetalStored, "CurrentMetalStored" );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.NextTimeToSpend, "NextTimeToSpend" );
            Buffer.AddBool( MetaData, this.humanAllied, "humanAllied" );
            Buffer.AddBool( MetaData, this.aiAllied, "aiAllied" );
            Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)this.state, "state" );

            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)LastTimePlanetHadCenter.Count, "LastTimePlanetHadCenter_Count" );
            foreach ( KeyValuePair<Planet, int> pair in LastTimePlanetHadCenter )
            {
                Buffer.AddPlanetIndex_Neg1ToPos( MetaData, pair.Key.Index, "planetIdx" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, (Int32)pair.Value, "planetLastTime" );
            }
            Buffer.AddBool( MetaData, this.IsInFireteamMode, "IsInFireteamMode" );
            FireteamBaseUtility.SerializeFireteams( MetaData, Buffer, SerializationCmdType, this.Teams );
            WaveData.SerializeTo( MetaData, Buffer, SerializationCmdType );
        }

        public override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "NanocaustFactionBaseInfo" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "NanocaustFactionBaseInfo Ext", TrackerStyle.ByTypeOnly );

            this.humanVision = Buffer.ReadBool( MetaData, "humanVision" );
            this.humanEncounter = Buffer.ReadBool( MetaData, "humanEncounter" );
            this.NanocaustSpecificAIP = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "NanocaustSpecificAIP" );
            this.numFleetsSent = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "numFleetsSent" );
            this.NanocaustInitialInvasionPlanets = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "NanocaustInitialInvasionPlanets" );
            this.LastAIExoTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "LastAIExoTime" );
            this.CurrentMetalStored = Buffer.ReadFInt( MetaData, "CurrentMetalStored" );
            this.NextTimeToSpend = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "NextTimeToSpend" );
            this.humanAllied = Buffer.ReadBool( MetaData, "humanAllied" );
            this.aiAllied = Buffer.ReadBool( MetaData, "aiAllied" );
            this.state = (InvasionState)Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "state" );

            LastTimePlanetHadCenter.Clear();
            int count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "LastTimePlanetHadCenter_Count" );
            for ( int i = 0; i < count; i++ )
            {
                Int16 planetIdx = Buffer.ReadPlanetIndex_Neg1ToPos( MetaData, "planetIdx" );
                Planet planet = World_AIW2.Instance.GetPlanetByIndex( planetIdx );
                LastTimePlanetHadCenter[planet] = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "planetLastTime" );
            }
            this.IsInFireteamMode = Buffer.ReadBool( MetaData, "IsInFireteamMode" );
            FireteamBaseUtility.DeserializeFireteamsAndDiscardAnyExtraLeftovers( MetaData, Buffer, SerializationCmdType, this.Teams, "nanocaust" );
            WaveData.DeserializeIntoSelf( MetaData, Buffer, SerializationCmdType );
            Buffer.StopTrackerByName( "NanocaustFactionBaseInfo Ext" );
        }
        #endregion

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return Intensity;
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI( ArcenCharacterBufferBase OptionalExplainCalculation )
        {
            DoRefreshFromFactionSettings();

            int load = 50 + (Intensity * 8);

            if ( OptionalExplainCalculation != null )
                OptionalExplainCalculation.Add( load ).Add( " 来自纳米灾变的负载" );
            return load;
        }

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            LoadCustomDataIfNeeded();       
        }
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
            Faction faction = this.AttachedFaction;
            string invasionTime = cfg.GetStringValueForCustomFieldOrDefaultValue( "InvasionTime", true );
            if ( faction.InvasionTime == -1 )
            {
                //initialize the nanocaust invasion time
                if ( invasionTime == "立即" )
                    faction.InvasionTime = 1;
                else if ( invasionTime == "游戏早期" )
                    faction.InvasionTime = 1 * (60 * 60); // 1 hr in
                else if ( invasionTime == "游戏中期" )
                    faction.InvasionTime = 2 * (60 * 60); // 2 hr in
                else if ( invasionTime == "游戏后期" )
                    faction.InvasionTime = 3 * (60 * 60); // 3 hr in
                if ( faction.InvasionTime > 1 )
                {
                    //this will be a desync on the client and host, but the host will correct the client in under 5 seconds.
                    if ( Engine_Universal.PermanentQualityRandom.Next( 0, 100 ) < 50 )
                        faction.InvasionTime += Engine_Universal.PermanentQualityRandom.Next( 0, faction.InvasionTime / 10 );
                    else
                        faction.InvasionTime -= Engine_Universal.PermanentQualityRandom.Next( 0, faction.InvasionTime / 10 );
                }
            }

            if ( AttachedFaction.MinFireteamStrength == -1 )
                AttachedFaction.MinFireteamStrength = 4000;
            if ( AttachedFaction.MaxFireteamStrength == -1 )
                AttachedFaction.MaxFireteamStrength = 7000;

            if ( !CrossFaction_AllNanocaustFactions.Contains( this ) )
                CrossFaction_AllNanocaustFactions.Add( this );

            this.SeedNearPlayer = AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "SpawnNearPlayer", true );

            this.SpawnPlanet = AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "SpawnPlanet", true );

            var str = AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "ReinvadeFrequency", true );
            if (str == "Never")
                this.ReinvadeFrequency = ReinvadeFrequencySetting.Never;
            else if (str == "~1 Hour")
                this.ReinvadeFrequency = ReinvadeFrequencySetting.ApproxOneHour;
            else if (str == "~30 Minutes")
                this.ReinvadeFrequency = ReinvadeFrequencySetting.Approx30Minutes;
        }
        #endregion

        #region SetStartingFactionRelationships
        public override void SetStartingFactionRelationships()
        {
            base.SetStartingFactionRelationships();
            Faction faction = AttachedFaction;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( faction == otherFaction )
                    continue;
                if ( otherFaction.Type == FactionType.NaturalObject )
                    continue;

                switch ( otherFaction.Type )
                {
                    case FactionType.Player:
                    case FactionType.AI:
                    case FactionType.SpecialFaction:
                        faction.MakeHostileTo( otherFaction );
                        otherFaction.MakeHostileTo( faction );
                        break;
                }
            }
        }
        #endregion

        #region GetShouldAttackNormallyExcludedTarget
        public override bool GetShouldAttackNormallyExcludedTarget( GameEntity_Squad Target )
        {
            #region Tracing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Nanocaust );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Nanocaust-GetShouldAttackNormallyExcludedTarget-trace", 10f ) : null;
            #endregion

            try
            {
                //This function is apparently called before the game starts, and the mgr isn't initialized
                //until the first PerSimStep
                if ( Target == null )
                {
                    if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }

                    return false;
                }

                //There's no principled need to kill the Warp Gate first, then the command stationary
                //anymore, but there's no harm in doing it this way still

                Faction targetControllingFaction = Target.GetFactionOrNull_Safe();

                if ( Target.TypeData.GetHasTag( "NormalPlanetNastyPick" ) || Target.TypeData.GetHasTag( "DSAA" ) )
                {
                    if ( tracing )
                    {
                        tracingBuffer.Add( "Nanocaust is allowed to kill " + Target.ToStringWithPlanet() + "\n" );
                        ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    return true;
                }

                if ( Target.TypeData.IsCommandStation &&
                     (!this.humanAllied || AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "MaraudersKillCommandStations" )) )
                {
                    if ( tracing )
                    {
                        tracingBuffer.Add( "Nanocaust is allowed to kill " + Target.ToStringWithPlanet() + ". humanAllied " + this.humanAllied + " commandStation " + AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "MaraudersKillCommandStations" ) + " \n" );
                        ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    return true;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Nanocaust GetShouldAttackNormallyExcludedTarget error:" + e, Verbosity.ShowAsError );
                return false;
            }
            if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }

            return false;
        }
        #endregion

        #region nanocaustInfectedPlanet
        public bool nanocaustInfectedPlanet( Planet planet )
        {
            if ( planet == null )
            {
                //we need this line number!
                ArcenDebugging.ArcenDebugLog( "BUG: nanocaustInfectedPlanet, the planet passed in is null somehow", Verbosity.DoNotShow );
                return false;
            }
            bool timeToReturn = false;
            bool valueToReturn = false;
            foreach ( SafeSquadWrapper centerWrap in this.NanobotCenters.GetDisplayList() )
            {
                GameEntity_Squad center = centerWrap.GetSquad();
                if ( center == null || center.HasBeenRemovedFromSim )
                    continue;
                if ( center.Planet == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "BUG: nanocaustInfectedPlanet, nanobotCenters entry[PKID " + center.PrimaryKeyID + "] has a null planet somehow", Verbosity.DoNotShow );
                    timeToReturn = true;
                    valueToReturn = false;
                    break;
                }

                if ( center.Planet == planet )
                {
                    timeToReturn = true;
                    valueToReturn = true;
                    break;
                }
            }
            if ( timeToReturn )
                return valueToReturn;

            foreach ( SafeSquadWrapper centerWrap in this.WarpingInNanobotCenters.GetDisplayList() )
            {
                GameEntity_Squad center = centerWrap.GetSquad();
                if ( center == null || center.HasBeenRemovedFromSim )
                    continue;
                if ( center.Planet == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "BUG: nanocaustInfectedPlanet, warpingInNanobotCenters entry[PKID " + center.PrimaryKeyID + "] has a null planet somehow", Verbosity.DoNotShow );
                    timeToReturn = true;
                    valueToReturn = false;
                    break;
                }

                if ( center.Planet == planet )
                {
                    timeToReturn = true;
                    valueToReturn = true;
                    break;
                }
            }
            return valueToReturn;
        }
        #endregion

        #region UpdatePowerLevel
        public override void UpdatePowerLevel()
        {
            FInt newResult = FInt.Zero;
            if ( this.InfectedPlanets.Count > 3 )
                newResult = FInt.FromParts( 0, 100 );
            else if ( this.InfectedPlanets.Count > 8 )
                newResult = FInt.FromParts( 0, 500 );
            else if ( this.InfectedPlanets.Count > 12 )
                newResult = FInt.One;
            else if ( this.InfectedPlanets.Count > 25 )
                newResult = FInt.FromParts( 2, 000 );

            this.AttachedFaction.OverallPowerLevel = newResult;
        }
        #endregion

        #region GetNanocaustStateForDisplay
        public void GetNanocaustStateForDisplay( ArcenDoubleCharacterBuffer output )
        {
            if ( this.AttachedFaction.InvasionTime > 0 && this.AttachedFaction.InvasionTime > World_AIW2.Instance.GameSecond )
            {
                output.Add( "纳米灾疫将在 " + (this.AttachedFaction.InvasionTime - World_AIW2.Instance.GameSecond) + " 秒后入侵。\n" );
            }
            if ( !this.IsInFireteamMode || this.Teams.GetItemCount() == 0 )
                return;
            output.Add( "纳米灾疫状态：" + this.state + "\n" );
            output.Add( "\n<" + this.Allegiance + "> 的纳米灾疫火队状态：\n" );
            int totalStrength = 0;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( this.Teams ) )
            {
                totalStrength += team.DeepInfo.TeamStrength;
                if ( team.status != FireteamStatus.Disbanded )
                {
                    team.DeepInfo.GetStringForDisplay( output );
                    output.Add( "\n" );
                }
            }
            output.Add( "纳米灾疫总强度：<color=#ff0000>" + (totalStrength / 1000) + "</color>。\n" );
        }
        #endregion

        #region GetFireteamById
        public override Fireteam GetFireteamById( int id )
        {
            return FireteamBaseUtility.GetFireteamById( this.Teams, id );
        }
        #endregion

        #region DoPerSecondLogic_Stage1Clearing_OnMainThreadAndPartOfSim_ClientAndHost
        public sealed override void DoPerSecondLogic_Stage1Clearing_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            if ( !working_HaveCrossFaction_AllHivesBeenClearedYetThisCycle )
            {
                //only do this on the first faction, doesn't matter which one it is.
                //doing it more times won't hurt anything, but wastes CPU
                working_HaveCrossFaction_AllHivesBeenClearedYetThisCycle = true;

                //ready these for across all factions
                CrossFaction_AllHives.ClearConstructionListForStartingConstruction();

                //go ahead and set this to be false, so that later this can be processed
                //right now it is probably true, from the last cycle.
                working_HaveCrossFaction_AllHivesBeenSwappedYetThisCycle = false;
            }
        }
        #endregion

        #region DoPerSecondLogic_Stage2APostAllFactionAggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public sealed override void DoPerSecondLogic_Stage2APostAllFactionAggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            //down below in the main Stage2Aggregating we're filling CrossFaction_AllHives.Construction
            //this is at least 3 factions (tamed, enraged, and 1+ dyson sphere factions), but could be more.
            //at this point we need to swap CrossFaction_AllHives, but we can ONLY do that once, regardless of how many factions there are
            if ( !working_HaveCrossFaction_AllHivesBeenSwappedYetThisCycle )
            {
                //this makes sure we only do it once
                working_HaveCrossFaction_AllHivesBeenSwappedYetThisCycle = true;

                //without this, the display lists will remain blank.  But it can only happen once per loop, not once per faction per loop!
                CrossFaction_AllHives.SwitchConstructionToDisplay();

                //we had better set THIS one to false now, so that next time we get back to Stage1Clearing
                //that logic will kick off properly again.
                working_HaveCrossFaction_AllHivesBeenClearedYetThisCycle = false;
            }
        }
        #endregion

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            this.InfectedPlanets.ClearConstructionListForStartingConstruction();
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.GetControllingOrInfluencingFaction() == AttachedFaction )
                    this.InfectedPlanets.AddToConstructionList( planet );
            }
            this.InfectedPlanets.SwitchConstructionToDisplay();

            this.WarpingInNanobotCenters.ClearConstructionListForStartingConstruction();
            this.NanobotCenters.ClearConstructionListForStartingConstruction();
            this.Hive.ClearConstructionValueForStartingConstruction();

            foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
            {
                if ( entity.TypeData.GetHasTag( "WarpingInNanobotCenter" ) )
                {
                    this.WarpingInNanobotCenters.AddToConstructionList( entity );
                    continue;
                }
                if ( entity.TypeData.GetHasTag( "NanobotCenter" ) )
                {
                    this.NanobotCenters.AddToConstructionList( entity );
                    if ( entity.TypeData.GetHasTag( NANOCAUST_HIVE ) || entity.TypeData.GetHasTag( NANOCAUST_HACKED_HIVE ) )
                    {
                        this.Hive.Construction = SafeSquadWrapper.Create( entity );
                        if ( entity.TypeData.GetHasTag( NANOCAUST_HACKED_HIVE ) )
                            this.hasBeenHacked = true;
                        CrossFaction_AllHives.AddToConstructionList( entity );
                    }
                }
            }

            this.WarpingInNanobotCenters.SwitchConstructionToDisplay();
            this.NanobotCenters.SwitchConstructionToDisplay();
            this.Hive.SwitchConstructionToDisplay();

            this.UpdatePlanetLists();
            this.CalculateStrengthPerNanobotCenter();
        }
        #endregion

        #region UpdatePlanetLists
        private void UpdatePlanetLists()
        {
            this.ActivePlanets.ClearConstructionListForStartingConstruction();
            this.QuiescedPlanets.ClearConstructionListForStartingConstruction();
            this.UninfectedPlanets.ClearConstructionListForStartingConstruction();
            if ( this.NanobotCenters.Count == 0 )
            {
                this.ActivePlanets.SwitchConstructionToDisplay();
                this.QuiescedPlanets.SwitchConstructionToDisplay();
                this.UninfectedPlanets.SwitchConstructionToDisplay();
                return;
            }

            //Now that I've found all the planets with constructors, see which of them have neighbors not on the list
            //for potential targets...
            foreach ( SafeSquadWrapper centerWrap in this.NanobotCenters.GetDisplayList() )
            {
                GameEntity_Squad centerSquad = centerWrap.GetSquad();
                if ( centerSquad == null || centerSquad.HasBeenRemovedFromSim )
                    continue;
                Planet planet = centerSquad.Planet;
                bool neighborNotOnList = false;
                foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                {
                    bool neighborOnList = false;
                    foreach ( SafeSquadWrapper otherCenterWrap in this.NanobotCenters.GetDisplayList() )
                    {
                        GameEntity_Squad otherCenter = otherCenterWrap.GetSquad();
                        if ( otherCenter == null || otherCenter.HasBeenRemovedFromSim )
                            continue;
                        if ( neighbor == planet )
                            continue;
                        if ( neighbor == otherCenter.Planet )
                        {
                            neighborOnList = true;
                            break;
                        }
                    }
                    if ( !neighborOnList )
                    {
                        this.UninfectedPlanets.AddToConstructionListIfNotAlreadyIn( neighbor );
                        neighborNotOnList = true;
                    }
                }
                if ( neighborNotOnList )
                    this.ActivePlanets.AddToConstructionListIfNotAlreadyIn( planet );
                else
                    this.QuiescedPlanets.AddToConstructionListIfNotAlreadyIn( planet );
            }

            this.ActivePlanets.SwitchConstructionToDisplay();
            this.QuiescedPlanets.SwitchConstructionToDisplay();
            this.UninfectedPlanets.SwitchConstructionToDisplay();
        }
        #endregion

        #region CalculateStrengthPerNanobotCenter
        private void CalculateStrengthPerNanobotCenter()
        {
            this.StrengthPerNanobotCenter.ClearConstructionDictForStartingConstruction();
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
            {
                findStrengthPerNanobotCenter( entity );
            }
            this.StrengthPerNanobotCenter.SwitchConstructionToDisplay();
        }

        private void findStrengthPerNanobotCenter( GameEntity_Squad entity )
        {
            //this function fills in strengthPerNanobotCenter lookup so we know
            //whether a given nanobot center is allowed to make more units
            bool localDebug = false;
            if ( localDebug )
                ArcenDebugging.ArcenDebugLogSingleLine( "GetPlanetsWithConstructors: scanning entity whose name is  " + entity.TypeData.InternalName, Verbosity.DoNotShow );
            if ( entity.TypeData.GetHasTag( "NanobotCenter" ) || entity.TypeData.GetHasTag( NANOCAUST_HACKED_HIVE ) || entity.TypeData.GetHasTag( NANOCAUST_HIVE ) )
                return;

            if ( entity.TypeData.IsMobileCombatant )
            {
                //entity.MinorFactionStackingID is the spawningNanobotCenter
                this.StrengthPerNanobotCenter.Construction[entity.MinorFactionStackingID] += entity.GetStrengthOfSelfAndContents();
            }
        }
        #endregion

        #region DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
            {
                UpgradeConstructors( entity );
                DoAttrition_AsPartOfMainSim( entity, Context );
            }
        }
        #endregion

        #region UpgradeConstructors
        private void UpgradeConstructors( GameEntity_Squad entity )
        {
            //Chris says: huh.  This seems to work on more than just constructors, but that's how it was coded, so I'm leaving it.

            //If a Constructor has lived long enough, upgrade it to the next version
            bool localDebug = false;
            if ( localDebug && entity.TypeData.GetDisplayName().Contains( "Nanobot Center" ) )
                ArcenDebugging.ArcenDebugLogSingleLine( "upgradeConstructors: scanning entity whose name is  " + entity.TypeData.InternalName + " alive for " + entity.GetSecondsSinceEnteringThisPlanet(), Verbosity.DoNotShow );
            if ( entity.CurrentMarkLevel == 1 && entity.GetSecondsSinceCreation() > this.timeForMarkIIUpgrade )
            {
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "UpgradeConstructors: found nanobot center Mark 1 on  " + entity.GetPlanetName_Safe() + " alive " + entity.GetSecondsSinceEnteringThisPlanet() + " seconds", Verbosity.DoNotShow );
                entity.SetCurrentMarkLevel( 2 );
            }
            if ( entity.CurrentMarkLevel == 2 && entity.GetSecondsSinceCreation() > this.timeForMarkIIIUpgrade )
            {
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "UpgradeConstructors: found nanobot center Mark II on  " + entity.GetPlanetName_Safe() + " alive " + entity.GetSecondsSinceEnteringThisPlanet() + " seconds", Verbosity.DoNotShow );
                entity.SetCurrentMarkLevel( 3 );
            }
        }
        #endregion

        #region DoAttrition_AsPartOfMainSim
        private void DoAttrition_AsPartOfMainSim( GameEntity_Squad entity, ArcenSimContextAnyStatus Context )
        {
            int AttritionPercentPerMinute = 7;
            if ( !entity.TypeData.IsMobile )
                return;
            if ( entity.Orders.Behavior != EntityBehaviorType.Attacker_Full )
                entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //make sure the ships are in attacker_full; okay, as main sim
            if ( !entity.ShouldNotBeConsideredAsThreatToHumanTeam )
                entity.ShouldNotBeConsideredAsThreatToHumanTeam = true;//this throws off the calculations
            if ( this.Hive.Display.GetSquad() == null && entity.ExtraStackedSquadsInThis > 0 )
                entity.AddOrSetExtraStackedSquadsInThis( 0, true );
            if ( entity.GetSecondsSinceCreation() > this.nanobotLifespan )
            {
                int damageToTake = (int)(((float)AttritionPercentPerMinute / 100) * (entity.GetMaxHullPoints()));
                entity.TakeDamageDirectly( damageToTake, null, null, DamageSource.BeingScrapped, Context );
            }
        }
        #endregion

        public AntiMinorFactionWaveData GetAntiMinorFactionWaveData()
        {
            return this.WaveData;
        }

        public void ResetForNextInvasion()
        {
            Cleanup();
        }
    }
}
