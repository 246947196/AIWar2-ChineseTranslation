using Arcen.AIW2.Core;
using System;


using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public enum FireteamStatus : byte
    {
        Disbanded, //this team is gone (all the ships died? The fireteam was too weak after a battle so we disbanded it so they could make new friends? etc)
        Assembling, //this team is having units assigned to it to strengthen it for combat. It's either too weak to fight yet, or is taking reinforcements while waiting for a good target
        Staging, //heading to the Lurk planet
        ReadyToAttack, //ready to attack
        Attacking, //https://www.youtube.com/watch?v=EE8kOv26BuQ
        Escorting, //Following a particular unit around
    }
    public class Fireteam : FireteamBase, ArcenLessLinkedList<Fireteam>.IContainable, ITimeBasedPoolable<Fireteam>, IProtectedListable
    {
        //This is the basic organizational unit of the scourge military
        //it's modeled vaguely after the now deprectated FrenzyFleet once used by the Nanocaust
        //NOTA BENE: Fireteams are LongRangePlanning only (except for where the scourge units upgrade themselves, which is probably a bad idea)

        #region FireTeamID
        private int _fireteamID; // a unique identifier
        public int FireTeamID
        {
            get { return _fireteamID; }
        }
        public void SetFireTeamID( int NewValue )
        {
            _fireteamID = NewValue;
        }
        #endregion

        //Any variables MUST be wiped back to blank on DoEarlyCleanupWhenGoingBackIntoPool()!
        //That means private and public ones, internal, protected, whatever

        public FireteamStatus status;
        public GameEntity_Squad Target  ; //this is the thing we want to kill
        public Planet TargetPlanet; //this is the planet we want to attack.
        public bool DefenseMode; //some Fireteams will be on defense only duty
        public bool CloakedOnly; //this fireteam contains only cloaked ships
        public bool UpgradedOnly; //this fleet contains only evolved or hybrid units
        public bool SuicideMission; //a Fireteam can be directed to suicide a target to distract the player while another attack hits home
        public Planet LurkPlanet; //the planet we are hiding on before we strike
        public int StrengthToBringOnline; //a fireteam won't be sent into action until it's at least this strong
        public int LurkStartTime;
        public int TimeInState;
        public bool MustCampOnWardenFleetBase; //for warden fireteams
        public Int16 PercentBestTarget; //when choosing a target, the a high number here makes the Fireteams for this faction concentrate their actions
        public Int16 PercentDistanceBestTarget; //set optionally; this percentage, typically lower than BestTarget, is used for deciding if we want to go for a distant target. Currently only for marauders
        public Int16 PreferredMaxDistance; //defines "distant target" for PercentDistanceBestTarget
        public readonly ProtectedList<HistoryItem> History = ProtectedList<HistoryItem>.Create_WillNeverBeGCed( 30, "Fireteam-History" );
        public FInt MyStrengthMultiplierForStrengthCalculation;
        public FInt EnemyStrengthMultiplierForStrengthCalculation;
        public bool IsAllowedToStack; //some factions allow their units in fireteams to freely stack with other fireteams. Scourge do not
        public bool NoDeathballing;
        public int DeathballingThreshold;
        public int PreferredSpeed; //average speed of the units in a fireteam

        // Some Fireteams have required targets. So far this is only for the Hunter Fleet.
        private FireteamRequiredTarget _specificationOrNull = null;
        public FireteamRequiredTarget SpecificationOrNull
        {
            get { return _specificationOrNull; }
            set
            {
                if ( _specificationOrNull == value )
                    return; //no change;
                if ( _specificationOrNull != null )
                    _specificationOrNull.ReturnToPool(); //prevent a leak
                _specificationOrNull = value;
            }
        }

        //Some factions might want to have fleets that start in Defense Mode
        //and then go to offense (Dark Zenith in particular)
        public int StepsUntilBecomesOffensive;
        public bool ExtraCautiousAgainstPlayers; //for high-level hunter fleets

        //TEACHING_MOMENT: This DeepInfo is "honorary," and in its class you can see the detailed rules for how to use it.
        public readonly FireteamHonoraryDeepInfo DeepInfo;

        //Not serialized
        public bool WorkingHasBeenUpdatedThisDeserializationPass = false; //this is just used as part of the deserialization flow control

        #region AgainstFaction
        public Faction AgainstFaction
        {
            get
            {
                if ( this.SpecificationOrNull != null )
                    return this.SpecificationOrNull.AgainstFaction;
                return null;
            }
        }
        #endregion
        #region AgainstTarget
        public GameEntity_Squad AgainstTarget
        {
            get
            {
                if ( this.SpecificationOrNull != null )
                    return this.SpecificationOrNull.AgainstGameEntity.GetSquad();
                return null;
            }
        }
        #endregion

        #region Pooling
        private static readonly ReferenceTracker fireteamRefTracker = new ReferenceTracker( "Fireteams" );

        private static int LastUniqueIDForFireteams = 0;

        private Fireteam()
        {
            if ( fireteamRefTracker != null )
                fireteamRefTracker.IncrementObjectCount();

            DeepInfo = new FireteamHonoraryDeepInfo( this );

            this.NonSimTrueUniqueID = System.Threading.Interlocked.Increment( ref LastUniqueIDForFireteams );
        }

        private static readonly TimeBasedPool<Fireteam> fireteamPool = TimeBasedPool<Fireteam>.Create_WillNeverBeGCed( "Fireteam", 3, 20, 30000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new Fireteam(); } );

        public Fireteam CreateNewForPool()
        {
            return new Fireteam();
        }

        public void ReturnToPool()
        {
            fireteamPool.ReturnToPool( this );
        }

        #region Debug Helpers
        public readonly int NonSimTrueUniqueID;
        public int GetPermaUniqueID
        {
            get { return this.NonSimTrueUniqueID; }
        }

        private string poolHistory = string.Empty; //do not reset this on cleanup!  This is left as it is on purpose

        public void Debug_WriteToHistoryOfPoolStatus( string Message )
        {
            if ( Message != null && Message.Length > 0 )
                this.poolHistory += Message;
        }

        public string Debug_GetCondensedHistoryOfPoolStatus()
        {
            return this.poolHistory;
        }
        #endregion Debug Helpers

        /// <summary>
        /// There are some links that we want to go ahead and destroy early, because keeping them might cause problems
        /// And not keeping them is unlikely to cause any errors
        /// </summary>
        public void DoEarlyCleanupWhenGoingIntoQuarantine_ClearIncomingPointersButNotOugoingReferences()
        {
            InitializeToDefaults_EarlyPart();
        }

        /// <summary>
        /// This happens on a background thread that blocks nothing else, so maximum performance from doing it here
        /// Also, it's been long enough that we can safely clear TypeData, which otherwise would lead to exceptions.
        /// It has been between 40 and 59 seconds since any shot was put in quarantine by the time this is hit.
        /// </summary>
        public void DoMidCleanupWhenLeavingQuarantineBackIntoMainPool_ClearAsMuchAsPossibleIncludingOutgoingReferences()
        {
            InitializeToDefaults_MidPart();
        }

        /// <summary>
        /// Nothing to do by this point!
        /// </summary>
        public void DoAnyBelatedCleanupWhenComingOutOfPool_ShouldBeVeryLittleToDo()
        {
        }

        public bool IsInPoolAtAll { get; set; }
        public bool IsInQuarantine { get; set; }

        //IProtectedListable
        public void DoBeforeRemoveOrClear()
        {
            fireteamPool.ReturnToPool( this );
        }
        #endregion

        //every variable of fireteams should be in either early or mid parts!

        #region InitializeToDefaults_EarlyPart
        public void InitializeToDefaults_EarlyPart()
        {
            //early parts in particular should remove us from containers that have us
            for ( int i = this.containers.Count - 1; i >= 0; i-- )
                this.containers[i].RemoveMe( true );
            this.containers.Clear();
        }
        #endregion

        #region InitializeToDefaults_MidPart
        public void InitializeToDefaults_MidPart()
        {
            //all of the rest of things should really be done later, so that we don't get any funky results
            this.History.Clear( true );

            this.status = FireteamStatus.Assembling;
            this.Target = null;
            this.TargetPlanet = null;
            this.DefenseMode = false;
            this.CloakedOnly = false;
            this.UpgradedOnly = false;
            this.SuicideMission = false;
            this.LurkPlanet = null;
            this.StrengthToBringOnline = 1000;
            this.LurkStartTime = -1;
            this.TimeInState = 0;
            this.MustCampOnWardenFleetBase = false;
            this.PercentDistanceBestTarget = -1;
            this.PercentBestTarget = 50;
            this.MyStrengthMultiplierForStrengthCalculation = FInt.One;
            this.EnemyStrengthMultiplierForStrengthCalculation = FInt.One;
            this.IsAllowedToStack = true;
            this.NoDeathballing = true;
            this.DeathballingThreshold = -1;
            this.PreferredSpeed = 0;
            if ( this.SpecificationOrNull != null )
                this.SpecificationOrNull.ReturnToPool();
            this.SpecificationOrNull = null;
            this.StepsUntilBecomesOffensive = 0;
            this.ExtraCautiousAgainstPlayers = false;
            this.PreferredMaxDistance = -1;
            this.WorkingHasBeenUpdatedThisDeserializationPass = false;

            //private, internal, protected, etc
            this._fireteamID = -1;

            this.DeepInfo.InitializeToDefaults();
        }
        #endregion

        #region CreateNewWithIDFromList
        public static Fireteam CreateNewWithIDFromList( ArcenLessLinkedList<Fireteam> Teams )
        {
            Fireteam result = fireteamPool.GetFromPoolOrCreate();
            result.InitializeToDefaults_EarlyPart();
            result.InitializeToDefaults_MidPart();
            result.SetFireTeamID( FireteamBaseUtility.GetNextFireteamId( Teams ) );
            return result;
        }
        #endregion

        #region Ser / Deser
        public static Fireteam DeserializeNewFrom_Pooled( SerMetaData MetaData, int id, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType, string ForDebugging_FactionName )
        {
            Fireteam result = fireteamPool.GetFromPoolOrCreate();
            result.InitializeToDefaults_EarlyPart();
            result.InitializeToDefaults_MidPart();
            result.DeserializedIntoSelf( MetaData, id, Buffer, SerializationCmdType, ForDebugging_FactionName );
            return result;
        }
        public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            int debugStage = 1;
            try
            {
                debugStage = 1000;
                Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.Q_ˉ1_To_1ˌ048ˌ575, this.FireTeamID, "FireTeamID" );
                Buffer.WriteHeaderStringToLogIfLoggingActive( "Fireteam Data" );
                Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.Target == null ? -1 : this.Target.PrimaryKeyID, "TargetId" );
                Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, this.TargetPlanet == null ? -1 : this.TargetPlanet.Index, "TargetPlanet" );
                Buffer.AddBool( MetaData, this.DefenseMode, "DefenseMode" );
                if (!SerializationCmdType.GetIsNetworkType())
                {
                    Buffer.AddBool( MetaData, this.CloakedOnly, "CloakedOnly" );
                    Buffer.AddBool(MetaData, this.UpgradedOnly, "UpgradedOnly");
                }
                Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, this.LurkPlanet == null ? -1 : this.LurkPlanet.Index, "LurkPlanet" );
                Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)this.status, "FireTeamStatus" );
                if (!SerializationCmdType.GetIsNetworkType())
                {
                    Buffer.AddBool( MetaData, this.SuicideMission, "SuicideMission" );
                    Buffer.AddInt32(MetaData, ReadStyle.NonNeg, this.StrengthToBringOnline, "StrengthToBringOnline");
                    Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.LurkStartTime, "LurkStartTime" );
                    Buffer.AddInt16(MetaData, ReadStyle.PosExceptNeg1, this.PercentBestTarget, "PercentBestTarget");
                    Buffer.AddInt16(MetaData, ReadStyle.PosExceptNeg1, this.PercentDistanceBestTarget, "PercentDistanceBestTarget");
                    Buffer.AddInt16(MetaData, ReadStyle.PosExceptNeg1, this.PreferredMaxDistance, "PreferredMaxDistance");
                }
                debugStage = 2000;

                //We don't need to track very much of it for it to be useful
                while ( this.History.Count > 2 )
                    this.History.RemoveAt( this.History.Count - 1, true );
                Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)this.History.Count, "History.Count" );
                for ( int i = 0; i < this.History.Count; i++ )
                    this.History[i].SerializeTo( MetaData, Buffer, SerializationCmdType );

                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.TimeInState, "TimeInState" );
                if (!SerializationCmdType.GetIsNetworkType())
                {
                    Buffer.AddBool(MetaData, this.MustCampOnWardenFleetBase, "MustCampOnWardenFleetBase");
                    Buffer.AddFInt(MetaData, this.MyStrengthMultiplierForStrengthCalculation, "MyStrengthMultiplierForStrengthCalculation");
                    Buffer.AddFInt(MetaData, this.EnemyStrengthMultiplierForStrengthCalculation, "EnemyStrengthMultiplierForStrengthCalculation");
                    Buffer.AddBool(MetaData, this.IsAllowedToStack, "IsAllowedToStack");
                    Buffer.AddBool(MetaData, this.NoDeathballing, "NoDeathballing");
                    Buffer.AddInt16(MetaData, ReadStyle.PosExceptNeg1, (short)this.DeathballingThreshold, "DeathballingThreshold");
                }

                if ( this.PreferredSpeed < 0 )
                    this.PreferredSpeed = 0;
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (short)this.PreferredSpeed, "PreferredSpeed" );
                Buffer.AddBool( MetaData, this.SpecificationOrNull != null, "SpecificationIsNotNull" ); //adds a boolean for the deserialization logic
                debugStage = 3000;
                if ( this.SpecificationOrNull != null )
                    this.SpecificationOrNull.SerializeTo( MetaData, Buffer, SerializationCmdType );
                debugStage = 4000;
                if (!SerializationCmdType.GetIsNetworkType())
                {
                    Buffer.AddInt32(MetaData, ReadStyle.PosExceptNeg1, this.StepsUntilBecomesOffensive, "StepsUntilBecomesOffensive");
                    Buffer.AddBool(MetaData, this.ExtraCautiousAgainstPlayers, "ExtraCautiousAgainstPlayers");
                    this.DeepInfo.SerializeTo_DiskOnly(MetaData, Buffer, SerializationCmdType);
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Exception serialzing a fireteam at stage " + debugStage + ", " + e, Verbosity.ShowAsError );
            }
        }

        public void DeserializedIntoSelf( SerMetaData MetaData, int FireTeamID, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType, string ForDebugging_FactionName )
        {
            this.SetFireTeamID( FireTeamID );
            Buffer.WriteHeaderStringToLogIfLoggingActive( "Fireteam Data" );
            Buffer.ActivateOrAddTrackerByNameIfTracking( "Fireteam Data", TrackerStyle.ByTypeOnly );
            //Chris says: we are going to do this outside of here so that we can find existing ones to match if need be
            //this.FireTeamID = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "FireTeamID" );
            this.Target = World_AIW2.Instance.GetEntityByID_Squad( Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "TargetId" ) );
            this.TargetPlanet = World_AIW2.Instance.GetPlanetByIndex( (Int16)Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, "TargetPlanet" ) );
            this.DefenseMode = Buffer.ReadBool( MetaData, "DefenseMode" );

            if (!SerializationCmdType.GetIsNetworkType())
            {
                this.CloakedOnly = Buffer.ReadBool( MetaData, "CloakedOnly" );
                this.UpgradedOnly = Buffer.ReadBool(MetaData, "UpgradedOnly");
            }
            this.LurkPlanet = World_AIW2.Instance.GetPlanetByIndex( (Int16)Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, "LurkPlanet" ) );
            this.status = (FireteamStatus)Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "FireTeamStatus" );
            if (!SerializationCmdType.GetIsNetworkType())
            {
                this.SuicideMission = Buffer.ReadBool( MetaData, "SuicideMission" );
                this.StrengthToBringOnline = Buffer.ReadInt32(MetaData, ReadStyle.NonNeg, "StrengthToBringOnline");
                this.LurkStartTime = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1, "LurkStartTime" );
                this.PercentBestTarget = Buffer.ReadInt16(MetaData, ReadStyle.PosExceptNeg1, "PercentBestTarget");
                this.PercentDistanceBestTarget = Buffer.ReadInt16(MetaData, ReadStyle.PosExceptNeg1, "PercentDistanceBestTarget");
                this.PreferredMaxDistance = Buffer.ReadInt16(MetaData, ReadStyle.PosExceptNeg1, "PreferredMaxDistance");
            }
            int count = Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "History.Count" );
            this.History.Clear( true ); //just in case
            for ( int i = 0; i < count; i++ )
                this.History.Add( HistoryItem.DeserializeFrom_NewStyle( MetaData, Buffer, SerializationCmdType ) );
            this.TimeInState = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "TimeInState" );
            if (!SerializationCmdType.GetIsNetworkType())
            {
                this.MustCampOnWardenFleetBase = Buffer.ReadBool(MetaData, "MustCampOnWardenFleetBase");
                MyStrengthMultiplierForStrengthCalculation = Buffer.ReadFInt(MetaData, "MyStrengthMultiplierForStrengthCalculation");
                EnemyStrengthMultiplierForStrengthCalculation = Buffer.ReadFInt(MetaData, "EnemyStrengthMultiplierForStrengthCalculation");
                this.IsAllowedToStack = Buffer.ReadBool(MetaData, "IsAllowedToStack");
                this.NoDeathballing = Buffer.ReadBool(MetaData, "NoDeathballing");
                this.DeathballingThreshold = Buffer.ReadInt16(MetaData, ReadStyle.PosExceptNeg1, "DeathballingThreshold");
            }
            this.PreferredSpeed = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg, "PreferredSpeed" );
            if ( Buffer.ReadBool( MetaData, "SpecificationIsNotNull" ) )
            {
                if ( this.SpecificationOrNull == null )
                    this.SpecificationOrNull = FireteamRequiredTarget.GetFromPoolOrCreate();
                this.SpecificationOrNull.DeserializedIntoSelf( MetaData, Buffer, SerializationCmdType );
            }
            if (!SerializationCmdType.GetIsNetworkType())
            {
                this.StepsUntilBecomesOffensive = Buffer.ReadInt32(MetaData, ReadStyle.PosExceptNeg1, "StepsUntilBecomesOffensive");
                this.ExtraCautiousAgainstPlayers = Buffer.ReadBool(MetaData, "ExtraCautiousAgainstPlayers");
                this.DeepInfo.DeserializedIntoSelf_DiskOnly(MetaData, Buffer, SerializationCmdType, ForDebugging_FactionName);
            }

            Buffer.StopTrackerByName( "Fireteam Data" );

            if ( World.Debug_WriteDeserializationFireteamData )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Fireteam " + this.FireTeamID + " with status " + this.status + " time in state " + this.TimeInState + " for faction " + ForDebugging_FactionName + ", no idea how many ships.", Verbosity.DoNotShow );
            }
        }
        #endregion

        #region HistoryItem
        public class HistoryItem : ConcurrentPoolable<HistoryItem>, IProtectedListable
        {
            //Any items added in here need to be cleared in DoEarlyCleanupWhenGoingBackIntoPool()!
            public HistoryItemType Type;
            public bool DefenseMode;
            public bool EscortMode;
            public int GameSecond;
            public string RelatedString;
            public Int16 PlanetIndex1;
            public Int16 PlanetIndex2;
            public Int16 PlanetIndex3;

            #region Pooling
            private static readonly ReferenceTracker historyRefTracker = new ReferenceTracker( "FireteamHistoryItems" );

            private HistoryItem()
            {
                if ( historyRefTracker != null )
                    historyRefTracker.IncrementObjectCount();
            }

            private static readonly ConcurrentPool<HistoryItem> historyPool = new ConcurrentPool<HistoryItem>( "FireteamHistoryItem", 30000,
                KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new HistoryItem(); } );

            public HistoryItem CreateNewForPool()
            {
                return new HistoryItem();
            }

            public void ReturnToPool()
            {
                historyPool.ReturnToPool( this );
            }

            public override void DoAnyBelatedCleanupWhenComingOutOfPool()
            {

            }

            public override void DoEarlyCleanupWhenGoingBackIntoPool()
            {
                this.Type = HistoryItemType.Unknown;
                this.DefenseMode = false;
                this.EscortMode = false;
                this.GameSecond = 0;
                this.RelatedString = string.Empty;
                this.PlanetIndex1 = -1;
                this.PlanetIndex2 = -1;
                this.PlanetIndex3 = -1;
            }

            //IProtectedListable
            public void DoBeforeRemoveOrClear()
            {
                historyPool.ReturnToPool( this );
            }
            #endregion

            #region Creation Methods
            public static HistoryItem Create_SinglePlanetRelated( HistoryItemType Type, bool DefenseMode, Planet RelatedPlanet )
            {
                HistoryItem result = historyPool.GetFromPoolOrCreate();
                result.Type = Type;
                result.DefenseMode = DefenseMode;
                result.PlanetIndex1 = (Int16)(RelatedPlanet == null ? -1 : RelatedPlanet.Index);
                result.GameSecond = World_AIW2.Instance.GameSecond;
                return result;
            }
            public static HistoryItem Create_EscortRelated( HistoryItemType Type, string GuardedShipName )
            {
                HistoryItem result = historyPool.GetFromPoolOrCreate();
                result.Type = Type;
                result.EscortMode = true;
                result.RelatedString = GuardedShipName;
                result.GameSecond = World_AIW2.Instance.GameSecond;
                return result;
            }
            public static HistoryItem Create_DoublePlanetRelated( HistoryItemType Type, bool DefenseMode, Planet RelatedPlanet1, Planet RelatedPlanet2 )
            {
                HistoryItem result = historyPool.GetFromPoolOrCreate();
                result.Type = Type;

                result.DefenseMode = DefenseMode;
                result.PlanetIndex1 = (Int16)(RelatedPlanet1 == null ? -1 : RelatedPlanet1.Index);
                result.PlanetIndex2 = (Int16)(RelatedPlanet2 == null ? -1 : RelatedPlanet2.Index);
                result.GameSecond = World_AIW2.Instance.GameSecond;
                return result;
            }
            public static HistoryItem Create_TriplePlanetRelated( HistoryItemType Type, bool DefenseMode, Planet RelatedPlanet1, Planet RelatedPlanet2, Planet RelatedPlanet3 )
            {
                HistoryItem result = historyPool.GetFromPoolOrCreate();
                result.Type = Type;
                result.DefenseMode = DefenseMode;
                result.PlanetIndex1 = (Int16)(RelatedPlanet1 == null ? -1 : RelatedPlanet1.Index);
                result.PlanetIndex2 = (Int16)(RelatedPlanet2 == null ? -1 : RelatedPlanet2.Index);
                result.PlanetIndex3 = (Int16)(RelatedPlanet3 == null ? -1 : RelatedPlanet3.Index);
                result.GameSecond = World_AIW2.Instance.GameSecond;
                return result;
            }
            #endregion

            #region Ser / Deser
            public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
            {
                Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)this.Type, "HistoryItemType" );
                Buffer.AddBool( MetaData, this.DefenseMode, "DefenseMode" );
                Buffer.AddBool( MetaData, this.EscortMode, "EscortMode" );
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.GameSecond, "HistoryItem-GameSecond" );
                switch ( this.Type )
                {
                    case HistoryItemType.LurkWithWardenFleetBase:
                    case HistoryItemType.DefensiveFleetWaitingInGeneral:
                    case HistoryItemType.SuicideAttack:
                        Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, this.PlanetIndex1, "HistoryItem-PlanetIndex1" );
                        break;
                    case HistoryItemType.RetreatFromLurkLocation:
                    case HistoryItemType.RetreatFromTravelToLurkLocation:
                    case HistoryItemType.LurkingAgainstAnotherPlanet:
                    case HistoryItemType.AttackFromPlanetToPlanet:
                    case HistoryItemType.DefensiveFleetAttackFromPlanetToPlanet:
                    case HistoryItemType.DefensiveFleetWaitingWithInfrastructure:
                    case HistoryItemType.LeaveToHelpAllies:
                        Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, this.PlanetIndex1, "HistoryItem-PlanetIndex1" );
                        Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, this.PlanetIndex2, "HistoryItem-PlanetIndex2" );
                        break;
                    case HistoryItemType.StagingToLurkPlanet:
                        Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, this.PlanetIndex1, "HistoryItem-PlanetIndex1" );
                        Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, this.PlanetIndex2, "HistoryItem-PlanetIndex2" );
                        Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, this.PlanetIndex3, "HistoryItem-PlanetIndex3" );
                        break;
                    case HistoryItemType.EscortShip:
                        Buffer.AddString_Condensed( MetaData, this.RelatedString, "EscortedShip" );
                        break;

                    default:
                        throw new Exception( "No SerializeTo defined for HistoryItemType " + this.Type );
                }
            }

            public static HistoryItem DeserializeFrom_NewStyle( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
            {
                HistoryItem result = historyPool.GetFromPoolOrCreate();
                int attempts = 500;
                while ( result == null && attempts-- > 0 )
                    result = historyPool.GetFromPoolOrCreate();

                if ( result == null )
                    throw new Exception( "Null HistoryItem in result in fireteam history deserialization." );

                result.Type = (HistoryItemType)Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "HistoryItemType" );
                result.DefenseMode = Buffer.ReadBool( MetaData, "DefenseMode" );
                result.EscortMode = Buffer.ReadBool( MetaData, "EscortMode" );

                result.GameSecond = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "HistoryItem-GameSecond" );
                switch ( result.Type )
                {
                    case HistoryItemType.LurkWithWardenFleetBase:
                    case HistoryItemType.DefensiveFleetWaitingInGeneral:
                    case HistoryItemType.SuicideAttack:
                        result.PlanetIndex1 = (Int16)Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, "HistoryItem-PlanetIndex1" );
                        break;
                    case HistoryItemType.RetreatFromLurkLocation:
                    case HistoryItemType.RetreatFromTravelToLurkLocation:
                    case HistoryItemType.LurkingAgainstAnotherPlanet:
                    case HistoryItemType.AttackFromPlanetToPlanet:
                    case HistoryItemType.DefensiveFleetAttackFromPlanetToPlanet:
                    case HistoryItemType.DefensiveFleetWaitingWithInfrastructure:
                    case HistoryItemType.LeaveToHelpAllies:
                        result.PlanetIndex1 = (Int16)Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, "HistoryItem-PlanetIndex1" );
                        result.PlanetIndex2 = (Int16)Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, "HistoryItem-PlanetIndex2" );
                        break;
                    case HistoryItemType.StagingToLurkPlanet:
                        result.PlanetIndex1 = (Int16)Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, "HistoryItem-PlanetIndex1" );
                        result.PlanetIndex2 = (Int16)Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, "HistoryItem-PlanetIndex2" );
                        result.PlanetIndex3 = (Int16)Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, "HistoryItem-PlanetIndex3" );
                        break;
                    case HistoryItemType.EscortShip:
                        result.RelatedString = Buffer.ReadString_Condensed( MetaData, "EscortedShip" );
                        break;
                    default:
                        throw new Exception( "No DeserializeFrom_NewStyle defined for HistoryItemType " + result.Type );
                }
                return result;
            }
            #endregion

            #region GetPlanetNames
            private string GetPlanetName1()
            {
                Planet planet = World_AIW2.Instance.GetPlanetByIndex( this.PlanetIndex1 );
                if ( planet == null )
                    return "???";
                return planet.Name;
            }

            private string GetPlanetName2()
            {
                Planet planet = World_AIW2.Instance.GetPlanetByIndex( this.PlanetIndex2 );
                if ( planet == null )
                    return "???";
                return planet.Name;
            }

            private string GetPlanetName3()
            {
                Planet planet = World_AIW2.Instance.GetPlanetByIndex( this.PlanetIndex3 );
                if ( planet == null )
                    return "???";
                return planet.Name;
            }
            #endregion

            #region GetTimestamp
            private string GetTimestamp()
            {
                return Engine_Universal.ToHoursAndMinutesString( this.GameSecond );
            }
            #endregion

            #region GetMyColoredName
            private string GetMyColoredName( Fireteam ForFireteam )
            {
                if ( ForFireteam == null )
                    return "Fireteam <color=#19ffdd>???</color>";
                if ( this.DefenseMode )
                    return "<color=#44dd44>Defensive</color> Fireteam <color=#19ffdd>" + ForFireteam.FireTeamID + "</color>";
                if ( this.EscortMode )
                    return "<color=#44dd44>Escorting</color> Fireteam <color=#19ffdd>" + ForFireteam.FireTeamID + "</color>";

                return "Fireteam <color=#19ffdd>" + ForFireteam.FireTeamID + "</color>";
            }
            #endregion

            public string ToDisplayString( Fireteam ForFireteam )
            {
                switch ( this.Type )
                {
                    case HistoryItemType.LurkWithWardenFleetBase:
                        return this.GetMyColoredName( ForFireteam ) + " is off to lurk on " + this.GetPlanetName1() +
                            " with a warden fleet base at " + this.GetTimestamp();
                    case HistoryItemType.RetreatFromLurkLocation:
                        return this.GetMyColoredName( ForFireteam ) + "'s Lurk Planet, " + this.GetPlanetName1() +
                            " has become unsafe; retreat to " + this.GetPlanetName2() + " at " + this.GetTimestamp();
                    case HistoryItemType.RetreatFromTravelToLurkLocation:
                        return this.GetMyColoredName( ForFireteam ) + " can no longer get to its Lurk Planet, " + this.GetPlanetName1() +
                            ". Retreat to " + this.GetPlanetName2() + " at " + this.GetTimestamp();
                    case HistoryItemType.LurkingAgainstAnotherPlanet:
                        return this.GetMyColoredName( ForFireteam ) + " is lurking on " + this.GetPlanetName1() + ", and ready to attack " +
                            this.GetPlanetName2() + " at " + this.GetTimestamp();
                    case HistoryItemType.AttackFromPlanetToPlanet:
                        return this.GetMyColoredName( ForFireteam ) + " on " + this.GetPlanetName1() + " is now Attacking <color=#ff0000>" +
                            this.GetPlanetName2() + "</color> at " + this.GetTimestamp();
                    case HistoryItemType.DefensiveFleetAttackFromPlanetToPlanet:
                        return this.GetMyColoredName( ForFireteam ) + " on " + this.GetPlanetName1() + " is now Attacking <color=#ff0000>" +
                            this.GetPlanetName2() + "</color> at " + this.GetTimestamp();
                    case HistoryItemType.StagingToLurkPlanet:
                        return this.GetMyColoredName( ForFireteam ) + " is now Staging from " + this.GetPlanetName1() + " to attack <color=#aa2244>" +
                            this.GetPlanetName2() + "</color>. It will lurk on <color=#884488>" + this.GetPlanetName3() + "</color>. At " + this.GetTimestamp();
                    case HistoryItemType.EscortShip:
                        return this.GetMyColoredName( ForFireteam ) + " is now escorting a " + this.RelatedString + " at " + this.GetTimestamp();

                    case HistoryItemType.DefensiveFleetWaitingWithInfrastructure:
                        return this.GetMyColoredName( ForFireteam ) + " is going to wait on " + this.GetPlanetName1() + " with some infrastructure at " + this.GetTimestamp();
                    case HistoryItemType.DefensiveFleetWaitingInGeneral:
                        return this.GetMyColoredName( ForFireteam ) + " is going to wait on " + this.GetPlanetName1() + " at " + this.GetTimestamp();
                    case HistoryItemType.LeaveToHelpAllies:
                        return this.GetMyColoredName( ForFireteam ) + " is setting off from " + this.GetPlanetName1() + " to help allies on <color=#884488>" +
                            this.GetPlanetName2() + "</color> at " + this.GetTimestamp();
                    case HistoryItemType.SuicideAttack:
                        return this.GetMyColoredName( ForFireteam ) + " is suiciding into " + this.GetPlanetName1() + " at " + this.GetTimestamp();
                    default:
                        throw new Exception( "No ToDisplayString defined for HistoryItemType " + this.Type );
                }
            }
        }
        #endregion

        #region HistoryItemType
        public enum HistoryItemType : byte
        {
            Unknown = 0,
            LurkWithWardenFleetBase,
            RetreatFromLurkLocation,
            RetreatFromTravelToLurkLocation,
            LurkingAgainstAnotherPlanet,
            StagingToLurkPlanet,
            AttackFromPlanetToPlanet,
            DefensiveFleetWaitingWithInfrastructure,
            DefensiveFleetWaitingInGeneral,
            SuicideAttack,
            LeaveToHelpAllies,
            DefensiveFleetAttackFromPlanetToPlanet,
            EscortShip,
        }
        #endregion

        #region Disband
        public void Disband( Faction ForFaction, ArcenLongTermIntermittentPlanningContextBase ContextOrNull )
        {
            //This fireteam is going away; typically because most of its ships are ready to upgrade.
            //Once no longer in a fireteam, each ship will run the "upgrade self if necessary" logic independently
            //NOTE: mutate our own state and tear down the DeepInfo BEFORE returning to the pool. Once ReturnToPool
            //is called the object can be cleaned up/handed back out, so writing to it afterward is order-fragile.
            this.status = FireteamStatus.Disbanded;
            //            ArcenDebugging.ArcenDebugLogSingleLine("Disbanding " + this.id + " with " + this.ships.Count + " ships.", Verbosity.DoNotShow );
            this.TargetPlanet = null;

            this.DeepInfo.Disband_JustTheDeepInfoPortion( ForFaction, ContextOrNull );

            this.ReturnToPool();
        }
        #endregion

        #region GetIsTargetProtectedByForcefield
        public bool GetIsTargetProtectedByForcefield( Faction faction, GameEntity_Squad target )
        {
            if ( target == null ) //if there are no forcefields, assume we have to fight the whole planet
                return true;

            Planet planet = target.Planet;
            bool foundBlockingShield = false;
            //This code is borrowed from IsShieldBlockingWormholeToPlanet
            foreach ( GameEntity_Squad shieldGenerator in planet.Squads( EntityRollupType.ProjectsForcefield ) )
            {
                if ( !shieldGenerator.GetIsFriendlyTowards_Safe( target.PlanetFaction.Faction ) )
                    continue; //only count forcefields friendly to the target
                int distanceThreshold = shieldGenerator.CalculatedCurrentShieldRadius;
                if ( distanceThreshold <= 0 )
                    continue;
                distanceThreshold += shieldGenerator.DataForMark.Radius; // not relevant per se, but kind of a stand-in for the radius of the retreating unit; TODO: maybe we need to have this been the largest radius of the units trying to retreat
                if ( shieldGenerator.GetDistanceTo_ExpensiveAccurate( target, RadiusCheck.IgnoreRadii, false ) > distanceThreshold )
                    continue;
                foundBlockingShield = true;
                break;
            }
            return foundBlockingShield;
        }
        #endregion

        #region GetTargetEntityIfValid
        public GameEntity_Squad GetTargetEntityIfValid( Faction faction )
        {
            if ( this.Target == null )
                return null;

            if ( this.Target.TypeData == null || this.Target.Planet == null )
            {
                this.Target = null; //the target seems to have died or something
                return null;
            }
            if ( !this.Target.GetIsHostileTowards_Safe( faction ) )
            {
                this.Target = null;
                return null; //this is rather confusing; the target entity isn't hostile to me
            }
            if ( this.Target.GetIsCrippled() ||
                 this.Target.SecondsSpentAsRemains > 0 )
            {
                this.Target = null;
                return null; //the target is crippled or is remains
            }
            return this.Target;
        }
        #endregion

        #region static CalculateMobileDefenses
        public static int CalculateMobileDefenses( Faction faction, Planet planet, FInt baseMobileDangerDivisor, FInt divisorIncreaseRate )
        {
            int nearbyUnengagedHostileMobileStrength = 0;
            FInt divisor = baseMobileDangerDivisor;
            var factionData = planet.GetStanceDataForFaction( faction );
            StrengthData_PlanetFaction_Stance hostileStrengthData = factionData[FactionStance.Hostile];

            for ( int j = 1; j < hostileStrengthData.UnengagedMobileStrengthByHopCount.Length; j++, divisor *= divisorIncreaseRate ) // starting with 1 to ignore what's on this planet itself
            {
                int initialValue = (hostileStrengthData.UnengagedMobileStrengthByHopCount[j] / divisor).IntValue;
                nearbyUnengagedHostileMobileStrength += initialValue;
            }
            return nearbyUnengagedHostileMobileStrength;
        }
        #endregion

        #region static GetLengthOfPath
        public static int GetMyStrengthOnPlanet( Planet planet, Faction faction )
        {
            EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( faction );
            StrengthData_PlanetFaction_Stance selfData = myFactionData[FactionStance.Self];
            StrengthData_PlanetFaction_Stance friendlyData = myFactionData[FactionStance.Friendly];
            return selfData.TotalStrength + friendlyData.TotalStrength;
        }
        #endregion

        #region static GetLengthOfPath
        public static int GetPlanetDefensiveStrength( Planet planet, Faction faction, bool IncludeAlliedStrength, ref int defensiveMobileStrength, FInt baseMobileDangerDivisor, FInt divisorIncreaseRate )
        {
            EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( faction );
            StrengthData_PlanetFaction_Stance hostileData = myFactionData[FactionStance.Hostile];

            FInt hostileStrengthActuallyOnThePlanet = FInt.Zero;
            FInt hostileRemoteMobileStrength = FInt.Zero;
            hostileStrengthActuallyOnThePlanet += hostileData.TotalStrength;
            defensiveMobileStrength = hostileData.MobileStrength;
            if ( IncludeAlliedStrength )
            {
                StrengthData_PlanetFaction_Stance selfData = myFactionData[FactionStance.Self];
                StrengthData_PlanetFaction_Stance friendlyData = myFactionData[FactionStance.Friendly];
                hostileStrengthActuallyOnThePlanet -= selfData.TotalStrength;
                hostileStrengthActuallyOnThePlanet -= friendlyData.TotalStrength;
            }
            if ( baseMobileDangerDivisor > FInt.Zero )
            {
                //bool updateGlobalCounter = false;
                hostileRemoteMobileStrength = (FInt)CalculateMobileDefenses( faction, planet, baseMobileDangerDivisor, divisorIncreaseRate );
                //Now check adjacent planets for defensive mobile strength, then update the hostile strength
                //to account for that. We don't always want to include the remote strength,
                //note that for the player, we count mobile defensive strength more heavily (since the player tends to have
                //a lot of mobile fleets for defense)

                //I need to include the mobile strength for each planet in a data structure so my fireteams know
                //whether some mobile strength can be ignored (since it's being relied on to defend multiple points)
                //So first I iterate over the TeamsAimedAtPlanet planets, then for each of the targets I
                //generate the mobile strength of the target and the nearby adjacent planets for each, updating the number of itmes
                //each planet is counted in PlanetsWithMobileStrengthCounted.
                //Then I repeat the current code to generate the overall defensive strength, and for each nearby planet with its mobile strength
                //counted > 1 times, we divide the mobile strength additionally by the number of times that planet was counted
            }
            return (hostileStrengthActuallyOnThePlanet + hostileRemoteMobileStrength).IntValue;
        }
        #endregion

        #region DiscardCurrentObjectives
        public void DiscardCurrentObjectives()
        {
            this.TargetPlanet = null;
            this.LurkPlanet = null;
            this.status = FireteamStatus.Assembling;
            this.History.Clear( true ); //this gets really cluttered in logging. Can be changed if we need detail here
        }
        #endregion

        //here are some C#-only constants for danger calculations for fireteams
        public static FInt RetreatPathRemoteShipDangerBaseDivisor = FInt.Zero;
        public static FInt RetreatPathRemoteShipDivisorIncreaseRate = FInt.FromParts( 3, 00 );
        public static FInt RoutingPastRemoteShipDangerBaseDivisor = FInt.FromParts( 2, 000 );
        public static FInt RoutingPastRemoteShipDivisorIncreaseRate = FInt.FromParts( 5, 000 );
        //for the attack path, we either need to beat the defenses (conquest/neuter) or snipe the target
        public static FInt FullBattleRemoteShipDangerBaseDivisor = FInt.FromParts( 2, 000 );
        public static FInt FullBattleRemoteShipDivisorIncreaseRate = FInt.FromParts( 2, 000 );
        public static FInt FullBattleRemoteShipDangerBaseDivisorPlayerOnly = FInt.FromParts( 1, 200 ); //if attacking a player, be extra cautious
        public static FInt FullBattleRemoteShipDivisorIncreaseRatePlayerOnly = FInt.FromParts( 2, 000 );//if attacking a player, be extra cautious

        public static FInt TargetSnipeRemoteShipDangerBaseDivisor = FInt.FromParts( 5, 000 );
        public static FInt TargetSnipeRemoteShipDivisorIncreaseRate = FInt.FromParts( 2, 000 );

        #region static GetLengthOfPath
        public static int GetLengthOfPath( Faction faction, ArcenSimContextAnyStatus Context, Planet source, Planet destination, PerFactionPathCache PathCacheData )
        {
            //this probably should never be called; instead use the hops value from GetDangerOfPath()
            PathBetweenPlanetsForFaction pathCacheForDangerCalculation = PathingHelper.FindPathFreshOrFromCache( faction, "FireteamGetLengthOfPath", source, destination, PathingMode.Default, Context, PathCacheData );
            if ( pathCacheForDangerCalculation == null )
                return 0;
            return pathCacheForDangerCalculation.PathToReadOnly.Count;
        }
        #endregion

        #region static IsThisAWinningBattle
        public static bool IsThisAWinningBattle( Faction faction, ArcenSimContextAnyStatus Context, Planet planet, int outnumberingFactor = 3, bool requireSelfStrength = true )
        {
            try
            {
                EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( faction );
                StrengthData_PlanetFaction_Stance hostileData = myFactionData[FactionStance.Hostile];

                StrengthData_PlanetFaction_Stance selfData = myFactionData[FactionStance.Self];
                StrengthData_PlanetFaction_Stance friendlyData = myFactionData[FactionStance.Friendly];
                int selfStrength = selfData.TotalStrength;
                int alliedStrength = friendlyData.TotalStrength;
                // if ( hostileData.TotalStrength == 0 )
                //     return true;
                int selfStrengthRequired = 0;
                if ( requireSelfStrength ) //Badger notes (7/15/20) I'm not sure why selfStrength is really required, but leaving it in for now because things work
                    selfStrengthRequired = 100;
                if ( (selfStrength >= selfStrengthRequired && hostileData.TotalStrength < (selfStrength + alliedStrength) / outnumberingFactor) )
                    return true;
            }
            catch { }
            return false;
        }
        #endregion

        #region MakeSurePathDoesNotIncludePlanet
        public static bool DoesPathIncludePlanet( Faction faction, ArcenSimContextAnyStatus Context, PerFactionPathCache PathCacheData, Planet source, Planet destination, Planet toCheckForInclusion )
        {
            //Used to make sure that getting to a Lurk planet doesn't take us through the destination
            PathBetweenPlanetsForFaction pathCacheForAvoidanceCalculation = PathingHelper.FindPathFreshOrFromCache( faction, "FireteamPathInclusionCheck", source, destination, PathingMode.Default, Context, PathCacheData );
            if ( pathCacheForAvoidanceCalculation == null )
                return false;
            for ( int i = 0; i < pathCacheForAvoidanceCalculation.PathToReadOnly.Count; i++ )
            {
                if ( pathCacheForAvoidanceCalculation.PathToReadOnly[i] == toCheckForInclusion )
                    return true;
            }
            return false;
        }
        #endregion

        #region static GetDangerOfPath
        public static int GetDangerOfPath( Faction faction, ArcenSimContextAnyStatus Context, PerFactionPathCache PathCacheData, Planet source, Planet destination, bool includeDestination, out Int16 hops )
        {
            PathBetweenPlanetsForFaction pathCacheForDangerCalculation = PathingHelper.FindPathFreshOrFromCache( faction, "FireteamGetDangerOfPath", source, destination, PathingMode.Default, Context, PathCacheData );
            if ( pathCacheForDangerCalculation == null )
            {
                hops = 0;
                return 0;
            }
            hops = (Int16)pathCacheForDangerCalculation.PathToReadOnly.Count;
            int totalDifficultyOfPath = 0;
            if ( source == destination )
                return 0;
            if ( pathCacheForDangerCalculation.PathToReadOnly.Count <= 0 )
                return 999999999; //we don't actually have a legal path to this location, so return a very large number
            for ( int i = 0; i < pathCacheForDangerCalculation.PathToReadOnly.Count; i++ )
            {
                if ( !includeDestination && pathCacheForDangerCalculation.PathToReadOnly[i] == destination )
                    break;
                int unused = 0;
                bool includeAlliedStrength = true;
                int danger = Fireteam.GetPlanetDefensiveStrength( pathCacheForDangerCalculation.PathToReadOnly[i], faction, includeAlliedStrength, ref unused, RoutingPastRemoteShipDangerBaseDivisor, RoutingPastRemoteShipDivisorIncreaseRate );
                if ( danger < 0 )
                    danger = 0; //we can just ignore allied forces for this calculation
                totalDifficultyOfPath += danger;
            }
            return totalDifficultyOfPath;
        }
        #endregion

        [NotForDumping]
        private readonly List<ArcenLessLinkedList<Fireteam>.ArcenLinkedListItem> containers = List<ArcenLessLinkedList<Fireteam>.ArcenLinkedListItem>.Create_WillNeverBeGCed( 20, "Fireteam-containers" );
        public List<ArcenLessLinkedList<Fireteam>.ArcenLinkedListItem> GetContainers()
        {
            return containers;
        }

        public int GetUniqueID()
        {
            return this.FireTeamID;
        }

        #region LiveTeamsIn (allocation-free struct-enumerable; retired the old Fireteam.DF in favor of this)
        // foreach ( Fireteam team in Fireteam.LiveTeamsIn( Teams ) ) { ... } is the zero-alloc replacement
        // for Fireteam.DF( Teams, delegate {...} ): no capturing closure, no delegate. It reproduces
        // DF's behavior exactly -- it prunes any null/Disbanded team in place (returning it to the pool)
        // as it walks, and yields only live teams. Mapping the old DelReturn body:
        //   DelReturn.Continue          -> just continue/fall through the loop body
        //   DelReturn.Break             -> break;
        //   DelReturn.RemoveAndContinue -> call the enumerator's RemoveCurrent() then continue (see below)
        // For the RemoveAndContinue case, iterate manually so you can reach the enumerator:
        //   var e = Fireteam.LiveTeamsIn( Teams ).GetEnumerator();
        //   while ( e.MoveNext() ) { Fireteam team = e.Current; ...; if ( remove ) { e.RemoveCurrent(); continue; } }
        public static LiveLinkedFireteams LiveTeamsIn( ArcenLessLinkedList<Fireteam> List ) => new LiveLinkedFireteams( List );

        public readonly struct LiveLinkedFireteams
        {
            private readonly ArcenLessLinkedList<Fireteam> list;
            internal LiveLinkedFireteams( ArcenLessLinkedList<Fireteam> List ) { this.list = List; }
            public Enumerator GetEnumerator() => new Enumerator( list );

            public struct Enumerator
            {
                private ArcenLessLinkedList<Fireteam>.Enumerator inner;
                private Fireteam current;
                internal Enumerator( ArcenLessLinkedList<Fireteam> List )
                {
                    inner = List == null ? default : List.GetEnumerator();
                    current = null;
                }
                public Fireteam Current => current;
                public bool MoveNext()
                {
                    while ( inner.MoveNext() )
                    {
                        Fireteam team = inner.Current;
                        if ( team == null || team.status == FireteamStatus.Disbanded )
                        {
                            //fireteam-specific cleanup that DF did: prune null/disbanded in place
                            if ( team != null )
                                team.ReturnToPool();
                            inner.RemoveCurrent();
                            continue;
                        }
                        current = team;
                        return true;
                    }
                    current = null;
                    return false;
                }
                //Equivalent of returning DelReturn.RemoveAndContinue from the old DF body.
                public void RemoveCurrent()
                {
                    if ( current != null )
                        current.ReturnToPool();
                    inner.RemoveCurrent();
                }
            }
        }

        // Sibling for the List<Fireteam> overload of DF: that overload skips (but does NOT prune)
        // null/Disbanded teams and forbids RemoveAndContinue, so this just yields the live ones.
        public static LiveListFireteams LiveTeamsIn( List<Fireteam> List ) => new LiveListFireteams( List );

        public readonly struct LiveListFireteams
        {
            private readonly List<Fireteam> list;
            internal LiveListFireteams( List<Fireteam> List ) { this.list = List; }
            public Enumerator GetEnumerator() => new Enumerator( list );

            public struct Enumerator
            {
                private readonly List<Fireteam> list;
                private int index;
                private Fireteam current;
                internal Enumerator( List<Fireteam> List ) { this.list = List; this.index = -1; this.current = null; }
                public Fireteam Current => current;
                public bool MoveNext()
                {
                    if ( list == null )
                        return false;
                    while ( ++index < list.Count )
                    {
                        Fireteam team = list[index];
                        if ( team == null || team.status == FireteamStatus.Disbanded )
                            continue;
                        current = team;
                        return true;
                    }
                    current = null;
                    return false;
                }
            }
        }
        #endregion

        #region GetStatusForDisplay
        public void GetStatusForDisplay( ArcenCharacterBufferBase buffer )
        {
            string color = "110000";
            if ( this.status == FireteamStatus.Assembling )
                color = "4444dd";
            else if ( this.status == FireteamStatus.Staging )
                color = "884488";
            else if ( this.status == FireteamStatus.ReadyToAttack )
                color = "aa2244";
            else if ( this.status == FireteamStatus.Attacking )
                color = "ff0000";
            else if ( this.status == FireteamStatus.Escorting )
                color = "2288aa";

            buffer.Add( this.status.ToString(), color );
            if ( this.SuicideMission )
                buffer.Add( "This fireteam will press home its attack regardless of numbers." );
            try
            {
                if ( this.status == FireteamStatus.Escorting && this.Target != null )
                    buffer.Add( " the " ).Add( this.Target.TypeData.GetDisplayName() ).Add( " on " ).Add( this.Target.GetPlanetName_Safe() );
                                
            }
            catch { }
            if ( this.PreferredSpeed > 0 )
                buffer.Add( ". Fireteam Preferred Speed " ).Add( this.PreferredSpeed, "d3d3d3" );
        }
        #endregion

        #region GetSpecificationForDisplay
        public void GetSpecificationForDisplay( ArcenCharacterBufferBase buffer )
        {
            //for logging/debugging
            try
            {
                if ( this.SpecificationOrNull != null && this.SpecificationOrNull.IsActive() )
                {
                    if ( this.AgainstFaction != null )
                        buffer.Add( "Against " ).Add( this.AgainstFaction.GetDisplayName(), this.AgainstFaction.FactionCenterColor.ColorHexBrighter );
                    else if ( !String.IsNullOrEmpty( this.SpecificationOrNull.AgainstFactionAllegiance ) )
                        buffer.Add( "Against " ).Add( this.SpecificationOrNull.AgainstFactionAllegiance, "204020" );
                    else if ( this.AgainstTarget != null )
                        buffer.Add( "Against " ).Add( this.AgainstTarget.TypeData.GetDisplayName() ).Add( " on " ).Add( this.AgainstTarget.GetPlanetName_Safe() );
                    buffer.Add( ". " );
                }
            }
            catch { } //this can have threading issues, since it's for the UI
        }
        #endregion

        #region ToString
#pragma warning disable 0809
        [Obsolete( "Please call GetDebugString() instead!", true )]
        public sealed override string ToString()
        {
            throw new Exception( "Called ToString() method on Fireteam" );
        }
#pragma warning restore 0809
        #endregion

        public void GetDebugString( ArcenCharacterBufferBase buffer )
        {
            this.DeepInfo.GetDebugString( buffer );
        }

        #region Temporary Lists
        private static RapidAntiLeakPool<List<Fireteam>> InnerPoolFor_FireteamList = RapidAntiLeakPool<List<Fireteam>>.Create_WillNeverBeGCed(
            "PoolFor_TempFireteamList", 10000, KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads,
            delegate { return List<Fireteam>.Create_WillNeverBeGCed( 30, "TempFireteamList" ); } );

        public static List<Fireteam> GetTemporaryFireteamList( string Name, float SecondsAfterWhichToDeclareLeak )
        {
            List<Fireteam> list = InnerPoolFor_FireteamList.GetFromPoolOrCreate( Name, SecondsAfterWhichToDeclareLeak );
            if ( list == null ) //pool returns null while blocked for teardown/shutdown; let callers bail
                return null;
            list.Clear();
            return list;
        }

        public static void ReleaseTemporaryFireteamList( List<Fireteam> list )
        {
            InnerPoolFor_FireteamList.ReturnToPool( list );
        }
        #endregion

        #region Temporary FireteamStatusDictOfInts
        private static RapidAntiLeakPool<Dictionary<FireteamStatus, int>> InnerPoolFor_FireteamStatusDictOfInts = RapidAntiLeakPool<Dictionary<FireteamStatus, int>>.Create_WillNeverBeGCed(
            "PoolFor_TempFireteamStatusDictOfInts", 10000, KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads,
            delegate { return Dictionary<FireteamStatus, int>.Create_WillNeverBeGCed( 30, "TempFireteamStatusDictOfInts" ); } );

        public static Dictionary<FireteamStatus,int> GetTemporaryFireteamStatusDictOfInts( string Name, float SecondsAfterWhichToDeclareLeak )
        {
            Dictionary<FireteamStatus,int> list = InnerPoolFor_FireteamStatusDictOfInts.GetFromPoolOrCreate( Name, SecondsAfterWhichToDeclareLeak );
            if ( list == null ) //pool returns null while blocked for teardown/shutdown; let callers bail
                return null;
            list.Clear();
            return list;
        }

        public static void ReleaseTemporaryFireteamStatusDictOfInts( Dictionary<FireteamStatus, int> list )
        {
            InnerPoolFor_FireteamStatusDictOfInts.ReturnToPool( list );
        }
        #endregion
    }
}
