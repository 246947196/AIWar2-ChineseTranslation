using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public abstract class DarkZenithFactionBaseInfoRoot : ExternalFactionBaseInfoRoot
    {
        public bool HasDoneInvasionInit;
        public bool DoInvasionNow; //set here for the beacon hack
        //The DZ appears, but does not link its planets in for a few minutes after the invasion. This lets their economy get started,
        //and also doubles as a warning time for the player to prepare
        public int TimeToLinkPlanets;
        public bool HasLinkedPlanets;
        public readonly List<Planet> OriginalPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "DarkZenithFactionBaseInfoRoot-OriginalPlanets" );
        public readonly List<Int16> AllPlanetsEverTakenIdxs = List<short>.Create_WillNeverBeGCed( 80, "DarkZenithFactionBaseInfoRoot-AllPlanetsEverTakenIdxs" );
        public readonly List<DZUpgrade> CompletedUpgrades = List<DZUpgrade>.Create_WillNeverBeGCed( 30, "DarkZenithFactionBaseInfoRoot-CompletedUpgrades" );
        public static readonly List<SafeSquadWrapper> WorkingFlagshipsList = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 150, "DarkFactionBaseInfoRoot-WorkingFlagshipsList" );
        public bool IsHomeworldUnderAttack; //for Jormugandr
        public int TimesHomeworldsAttacked; //for Jormugandr
        //The Territorial Sphere is a concept that determines how the DZ expand in the early game.
        //They have to conquer and build on the territorial sphere, one hop at a time (so all 1 hop planets, then all 2 hop)
        //The intent is to make sure the DZ don't overexpand quickly and open themselves for a counter attack
        public bool HasTakenTerritorialSphere;
        public int HopsOfSphereTaken; //we have to take the sphere one planet at a time
        public int TimeForNextExo; //the DZ can have Exos thrown against it

        //Set by the player from the DZ Logistics popout to request that the next available Epistyle
        //build a Terminus of this resource type next. Consumed (reset to None) once an Epistyle commits to it.
        public DZResource RequestedTerminusResource = DZResource.None;

        //For the Fimbulwinter
        public readonly Dictionary<Planet, int> FimbulwinterEligibleTime = Dictionary<Planet, int>.Create_WillNeverBeGCed( 500, "DarkZenithFactionBaseInfoRoot-FimbulwinterEligibleTime" );

        //not serialized
        public readonly ArcenLessLinkedList<Fireteam> Teams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "DarkZenithFactionBaseInfoRoot-Teams" );
        public List <VassalMission> EconomicMissions = List<VassalMission>.Create_WillNeverBeGCed( 50, "DarkZenithFactionBaseInfoRoot-EconomicVassalMissions" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Flagships = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "DarkZenith-Flagships" );
        public bool WinterEnabled;
        public int Intensity = 0;
        public int NadirStrength = 0; //For DZ Sidekick
        public DarkZenithDifficulty Difficulty = null;

        /* Lots of Lists of units for handling later */
        public readonly DoubleBufferedList<Planet> PlanetsControlled = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 200, "DZ-PlanetsControlled" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Terminii = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "DZ-Terminii" );
        public readonly DoubleBufferedList<SafeSquadWrapper> WarpingInTerminii = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "DZ-WarpingInTerminii" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Epistyles = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "DZ-Epistyles" );
        public readonly DoubleBufferedList<SafeSquadWrapper> WarpingInEpistyles = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "DZ-WarpingInEpistyles" );
        public readonly DoubleBufferedList<SafeSquadWrapper> AllEconomicStructures = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "DZ-AllEconomicStructures" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Harvesters = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "DZ-Harvesters" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Utilities = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "DZ-Utilities" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Transports = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "DZ-Transports" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Privateers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "DZ-Privateers" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Constructors = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "DZ-Constructors" );
        public readonly DoubleBufferedList<SafeSquadWrapper> Jormugandr = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "DZ-Jormugandr" );
        public readonly DoubleBufferedList<SafeSquadWrapper> SortedHjarnum = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "DZ-SortedHjarnum" );

        public readonly DoubleBufferedList<SafeSquadWrapper> NadirBases = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "DZ-NadirBases" ); //DZ Sidekick support
        public readonly DoubleBufferedList<SafeSquadWrapper> NadirSpawners = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "DZ-NadirSpawners" ); //DZ Sidekick support
        public readonly DoubleBufferedList<SafeSquadWrapper> LooseFleetCombatShips = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "DZ-LooseFleetCombatShips" ); //DZ Sidekick support
        public readonly DoubleBufferedList<SafeSquadWrapper> UnRalliedCombatShips = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "DZ-UnRalliedCombatShips" ); //DZ Sidekick support
        //Since this gets altered during s im runtime, it must be the more-robust-but-slower DoubleBufferedConcurrentList
        public readonly DoubleBufferedConcurrentList<SafeSquadWrapper> Hjarnum = DoubleBufferedConcurrentList<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "DZ-Hjarnum" );

        public DoubleBufferedValue<bool> DZIsSuppressed = new DoubleBufferedValue<bool>( false ); //there's a beacon that invokes us

        public readonly DoubleBufferedList<Planet> EnemyPlanetsInTerritorialSphereSim = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 200, "DZ-EnemyPlanetsInTerritorialSphereSim" );
        public readonly List<Planet> InitialEnemyAdjacentPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "DarkZenithFactionBaseInfoRoot-InitialEnemyAdjacentPlanets" ); //can't be static; one for each copy of the DZ

        //The Dark Zenith is canonically only allied to the DarkAlliance,
        //and the base game won't allow otherwise
        //However, I'm adding in support for other team options
        //so modders don't have to worry about it later
        public bool DarkAlliance = false;
        public bool MinorFactionAllied = false;
        public bool JoinAlliedFactions = false;
        public bool AIAllied = false;
        public bool IsPlayer = false;
        public bool PlayerAllied = false;
        public bool InCivilWar = false;
        public bool InInitialInvasionMode = false;
        public bool CanPlayerSeeEconomy = false; //its okay if this is redone on game load. Used for journals
        public bool CanPlayerSeeJormugandr = false; //its okay if this is redone on game load. Used for journals
        public bool CanPlayerSeeFimbulwinter = false; //its okay if this is redone on game load. Used for journals
        public bool HasAnyPlayerAllies = false;
        public bool HasBuiltGolem = false;
        public bool HasCheckedForFimbuledAllies = false;

        public int ResourceExchangeDistance = 500; //used for Transports and Harvesters
        public int ResourceExchangeDistancePrivateers = 1000; //used for Transports and Harvesters
        public int MaxTerritorialSphereSize = 10;
        public int PlanetsInTerritorialSphere = -1;
        public int TimeBetweenPlanetSpawnAndLinking = 480;
        public DoubleBufferedList<Planet> UnownedPlanetsInTerritorialSphereSim = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 200, "DZ-UnownedPlanetsInTerritorialSphereSim" );

        //constants

        //can be kept between runs, these are just for the ui
        public static readonly Dictionary<DZResource, string> ResourceColour = Dictionary<DZResource, string>.Create_WillNeverBeGCed( (int)DZResource.End + 1, "DZ-ResourceColour" );
        public static readonly Dictionary<DZResource, string> ResourceFancyName = Dictionary<DZResource, string>.Create_WillNeverBeGCed( (int)DZResource.End + 1, "DZ-ResourceFancyName" );

        public DarkZenithFactionBaseInfoRoot()
        {
            Cleanup();
        }

        #region Cleanup
        protected sealed override void Cleanup()
        {
            HasDoneInvasionInit = false;
            DoInvasionNow = false;

            TimeToLinkPlanets = -1;
            HasLinkedPlanets = false;
            OriginalPlanets.Clear();
            AllPlanetsEverTakenIdxs.Clear();
            CompletedUpgrades.Clear();
            IsHomeworldUnderAttack = false;
            TimesHomeworldsAttacked = 0;

            HasTakenTerritorialSphere = false;
            HopsOfSphereTaken = 0;
            TimeForNextExo = -1;
            RequestedTerminusResource = DZResource.None;

            FimbulwinterEligibleTime.Clear();

            //not serialized
            EconomicMissions.Clear();
            Teams.Clear();
            Flagships.Clear();
            WinterEnabled = false;
            Intensity = 0;
            NadirStrength = 0;
            Difficulty = null;

            PlanetsControlled.Clear();
            Terminii.Clear();
            WarpingInTerminii.Clear();
            Epistyles.Clear();
            WarpingInEpistyles.Clear();
            AllEconomicStructures.Clear();
            Harvesters.Clear();
            Utilities.Clear();
            Transports.Clear();
            Privateers.Clear();
            Constructors.Clear();
            Jormugandr.Clear();
            Hjarnum.Clear();
            SortedHjarnum.Clear();
            NadirBases.Clear();
            NadirSpawners.Clear();
            LooseFleetCombatShips.Clear();
            UnRalliedCombatShips.Clear();

            DZIsSuppressed.Clear();

            EnemyPlanetsInTerritorialSphereSim.Clear();
            InitialEnemyAdjacentPlanets.Clear();

            DarkAlliance = false;
            MinorFactionAllied = false;
            JoinAlliedFactions = false;
            AIAllied = false;
            IsPlayer = false;
            PlayerAllied = false;
            InCivilWar = false;
            InInitialInvasionMode = false;
            CanPlayerSeeEconomy = false;
            CanPlayerSeeJormugandr = false;
            CanPlayerSeeFimbulwinter = false;
            HasAnyPlayerAllies = false;
            HasBuiltGolem = false;
            HasCheckedForFimbuledAllies = false;

            ResourceExchangeDistance = 500; //used for Transports and Harvesters
            MaxTerritorialSphereSize = 10;
            PlanetsInTerritorialSphere = -1;
            TimeBetweenPlanetSpawnAndLinking = 480;
            UnownedPlanetsInTerritorialSphereSim.Clear();

            HaveLoadedData = false; //force xml reload

            this.SubCleanup();
        }
        protected abstract void SubCleanup();
        #endregion

        #region Serialization And Deserialization
        public sealed override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "DZ Global Data" );
            FireteamBaseUtility.SerializeFireteams( MetaData, Buffer, SerializationCmdType, this.Teams );
            Buffer.AddBool( MetaData, this.HasDoneInvasionInit );
            Buffer.AddBool( MetaData, this.DoInvasionNow );
            Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.CompletedUpgrades.Count );
            for ( int i = 0; i < this.CompletedUpgrades.Count; i++ )
            {
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, this.CompletedUpgrades[i].UpgradeIndex );
            }
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.OriginalPlanets.Count );
            for ( int i = 0; i < this.OriginalPlanets.Count; i++ )
            {
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, this.OriginalPlanets[i].Index );
            }
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (Int16)this.AllPlanetsEverTakenIdxs.Count );
            for ( int i = 0; i < this.AllPlanetsEverTakenIdxs.Count; i++ )
            {
                Buffer.AddInt16( MetaData, ReadStyle.NonNeg, this.AllPlanetsEverTakenIdxs[i] );
            }
            Buffer.AddBool( MetaData, this.IsHomeworldUnderAttack );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (short)this.TimesHomeworldsAttacked );
            Buffer.AddBool( MetaData, HasTakenTerritorialSphere );
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, (short)this.HopsOfSphereTaken );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeForNextExo );
            Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, this.TimeToLinkPlanets );
            Buffer.AddBool( MetaData, HasLinkedPlanets );
            //only serialize the relevant planets (ie who have an meaningful eligible time)
            Int16 count = 0;
            if ( FimbulwinterEligibleTime != null )
            {
                foreach ( KeyValuePair<Planet, int> pair in FimbulwinterEligibleTime )
                {
                    if ( pair.Value > World_AIW2.Instance.GameSecond ) //Chris confirms: this is fine!
                        count++;
                }
            }
            Buffer.AddInt16( MetaData, ReadStyle.NonNeg, count );
            if ( count > 0 )
            {
                foreach ( KeyValuePair<Planet, int> pair in FimbulwinterEligibleTime )
                {
                    if ( pair.Value <= World_AIW2.Instance.GameSecond )
                        continue;
                    Buffer.AddInt16( MetaData, ReadStyle.PosExceptNeg1, (short)pair.Key.Index );
                    Buffer.AddInt32( MetaData, ReadStyle.PosExceptNeg1, pair.Value );
                }
            }
            Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)this.RequestedTerminusResource, "RequestedTerminusResource" );
            this.SubSerializeFactionTo( MetaData, Buffer, SerializationCmdType );
            Buffer.WriteHeaderStringToLogIfLoggingActive( "DZ Global Data end" );
        }
        protected abstract void SubSerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType );

        public sealed override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.AllPlanetsEverTakenIdxs.Clear();

            Buffer.WriteHeaderStringToLogIfLoggingActive( "DZ Global Data" );
            FireteamBaseUtility.DeserializeFireteamsAndDiscardAnyExtraLeftovers( MetaData, Buffer, SerializationCmdType, this.Teams, "dz" );
            HasDoneInvasionInit = Buffer.ReadBool( MetaData );
            DoInvasionNow = Buffer.ReadBool( MetaData );
            int numUpgrades = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg );
            CompletedUpgrades.Clear();
            for ( int i = 0; i < numUpgrades; i++ )
                CompletedUpgrades.Add( DarkZenithUpgradeTable.Instance.GetRowById( Buffer.ReadInt32( MetaData, ReadStyle.NonNeg ) ) );

            Int16 count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            this.OriginalPlanets.Clear();
            for ( int i = 0; i < count; i++ )
            {
                this.OriginalPlanets.Add( World_AIW2.Instance.GetPlanetByIndex( Buffer.ReadInt16( MetaData, ReadStyle.NonNeg ) ) );
            }

            count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            for ( int i = 0; i < count; i++ )
            {
                this.AllPlanetsEverTakenIdxs.Add( Buffer.ReadInt16( MetaData, ReadStyle.NonNeg ) );
            }

            this.IsHomeworldUnderAttack = Buffer.ReadBool( MetaData );
            this.TimesHomeworldsAttacked = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            this.HasTakenTerritorialSphere = Buffer.ReadBool( MetaData );
            this.HopsOfSphereTaken = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            this.TimeForNextExo = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            this.TimeToLinkPlanets = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            this.HasLinkedPlanets = Buffer.ReadBool( MetaData );

            count = Buffer.ReadInt16( MetaData, ReadStyle.NonNeg );
            for ( int i = 0; i < count; i++ )
            {
                short planetIdx = Buffer.ReadInt16( MetaData, ReadStyle.PosExceptNeg1 );
                this.FimbulwinterEligibleTime[World_AIW2.Instance.GetPlanetByIndex( planetIdx )] = Buffer.ReadInt32( MetaData, ReadStyle.PosExceptNeg1 );
            }
            if ( Buffer.FromGameVersion.GetGreaterThanOrEqualTo( 5, 817 ) )
                this.RequestedTerminusResource = (DZResource)Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "RequestedTerminusResource" );

            this.SubDeserializeFactionIntoSelf( MetaData, Buffer, SerializationCmdType );
            Buffer.WriteHeaderStringToLogIfLoggingActive( "DZ Global Data end" );
        }
        protected abstract void SubDeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType );
        #endregion end Serialization And Deserialization

        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant()
        {
            return Intensity;
        }

        #region Xml Constants
        //We want to have at least this many terminii for a full economy
        public int BasePlanetsControlled = 3;
        public int BaseMetalTerminii = 4;
        public int BaseGreenTerminii = 1;
        public int BaseBlueTerminii = 1;
        public int BaseWhiteTerminii = 1;
        public int PermanentBonusIncomeInterval = 10;
        public bool HaveLoadedData = false;
        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedData )
                return;
            this.HaveLoadedData = true;
            PermanentBonusIncomeInterval = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_DarkZenith_PermanentBonusIncomeInterval" );
        }
        #endregion

        #region DoFactionGeneralAggregationsPausedOrUnpaused
        protected sealed override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            LoadCustomDataIfNeeded();
            this.SubDoGeneralAggregationsPausedOrUnpaused();
        }
        protected abstract void SubDoGeneralAggregationsPausedOrUnpaused();
        #endregion

        #region DoRefreshFromFactionSettings        
        protected override void DoRefreshFromFactionSettings()
        {
            ConfigurationForFaction cfg = this.AttachedFaction.Config;
            Intensity = cfg.GetIntValueForCustomFieldOrDefaultValue( "Intensity", true );
            NadirStrength = cfg.GetIntValueForCustomFieldOrDefaultValue( "NadirStrength", false );
            Difficulty = DarkZenithDifficultyTable.Instance.GetRowByIntensity( this.Intensity, this.AttachedFaction );

            this.WinterEnabled = this.AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "EnableFimbulwinter", true );

            if ( ResourceColour.Count == 0 )
            {
                //this is for UI stuff
                ResourceColour[DZResource.Metal] = "d3d3d3";
                ResourceColour[DZResource.Green] = "10ff10";
                ResourceColour[DZResource.White] = "D7BE69";
                ResourceColour[DZResource.Blue] = "1020ff";
                ResourceColour[DZResource.Red] = "ff2010";
                ResourceColour[DZResource.Black] = "6a7a6a";
            }
            if ( ResourceFancyName.Count == 0 )
            {
                //this is for UI stuff
                ResourceFancyName[DZResource.Metal] = "Octiron";
                ResourceFancyName[DZResource.Green] = "Thaumite";
                ResourceFancyName[DZResource.White] = "Alkahest";
                ResourceFancyName[DZResource.Blue] = "Chelonium";
                ResourceFancyName[DZResource.Red] = "Izumite";
                ResourceFancyName[DZResource.Black] = "Skrith";
            }
        }
        #endregion

        public bool logTransportSim = false; //i want to be able to deactivate DZ transport logging independently

        #region DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost
        public sealed override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            HandleAlliances( Context );

            //now refresh all our lists
            PlanetsControlled.ClearConstructionListForStartingConstruction();
            Terminii.ClearConstructionListForStartingConstruction();
            WarpingInTerminii.ClearConstructionListForStartingConstruction();
            Epistyles.ClearConstructionListForStartingConstruction();
            WarpingInEpistyles.ClearConstructionListForStartingConstruction();
            AllEconomicStructures.ClearConstructionListForStartingConstruction();
            Harvesters.ClearConstructionListForStartingConstruction();
            Utilities.ClearConstructionListForStartingConstruction();
            Transports.ClearConstructionListForStartingConstruction();
            Privateers.ClearConstructionListForStartingConstruction();
            Constructors.ClearConstructionListForStartingConstruction();
            Jormugandr.ClearConstructionListForStartingConstruction();
            Hjarnum.ClearConstructionListForStartingConstruction();
            SortedHjarnum.ClearConstructionListForStartingConstruction();
            NadirBases.ClearConstructionListForStartingConstruction(); //DZ Sidekick
            NadirSpawners.ClearConstructionListForStartingConstruction(); //DZ Sidekick
            LooseFleetCombatShips.ClearConstructionListForStartingConstruction();
            UnRalliedCombatShips.ClearConstructionListForStartingConstruction();

            DZIsSuppressed.ClearConstructionValueForStartingConstruction();

            EconomicMissions.Clear();
            Flagships.Clear();
            FactionUtilityMethods.Instance.GetActiveVassalMissions( AttachedFaction, VassalMissionType.Construction, this.EconomicMissions );

            if ( this.AttachedFaction.MinFireteamStrength == -1 )
                this.AttachedFaction.MinFireteamStrength = 3000;
            if ( this.AttachedFaction.MaxFireteamStrength == -1 )
                this.AttachedFaction.MaxFireteamStrength = 5000;

            if ( this.OriginalPlanets.Count > 0 && !this.HasTakenTerritorialSphere )
            {
                //once the invasion has happened, calculate the territory
                EnemyPlanetsInTerritorialSphereSim.ClearConstructionListForStartingConstruction();
                GetEnemiesInTerritorialSphere( EnemyPlanetsInTerritorialSphereSim.GetConstructionList(), Context );
                EnemyPlanetsInTerritorialSphereSim.SwitchConstructionToDisplay();
            }
            /* Iterate over the relevant units and build the correct lists to use later. */
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "DarkZenithBeacon" ) )
            {
                Helper_CheckDZConversionList( entity );
                DZIsSuppressed.Construction = true;
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "DarkZenithFlagship" ) )
            {
                Flagships.AddToConstructionList( entity );
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "WarpingInDZTerminus" ) )
            {
                Helper_CheckDZConversionList( entity );
                WarpingInTerminii.AddToConstructionList( entity );
                AllEconomicStructures.AddToConstructionList( entity );
                PlanetsControlled.AddToConstructionListIfNotAlreadyIn( entity.Planet );
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "DZTerminus" ) )
            {
                Helper_CheckDZConversionList( entity );
                Terminii.AddToConstructionList( entity );
                AllEconomicStructures.AddToConstructionList( entity );
                PlanetsControlled.AddToConstructionListIfNotAlreadyIn( entity.Planet );
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "WarpingInDZEpistyle" ) )
            {
                Helper_CheckDZConversionList( entity );
                WarpingInEpistyles.AddToConstructionList( entity );
                AllEconomicStructures.AddToConstructionList( entity );
                PlanetsControlled.AddToConstructionListIfNotAlreadyIn( entity.Planet );
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "DZEpistyle" ) )
            {
                Helper_CheckDZConversionList( entity );
                Epistyles.AddToConstructionList( entity );
                AllEconomicStructures.AddToConstructionList( entity );
                if ( !CanPlayerSeeEconomy && entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                    CanPlayerSeeEconomy = true;
                PlanetsControlled.AddToConstructionListIfNotAlreadyIn( entity.Planet );
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "DZHarvester" ) )
            {
                Helper_CheckDZConversionList( entity );
                Harvesters.AddToConstructionList( entity );
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "DZTransport" ) )
            {
                Helper_CheckDZConversionList( entity );
                Transports.AddToConstructionList( entity );
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "DZPrivateer" ) )
            {
                Helper_CheckDZConversionList( entity );
                Privateers.AddToConstructionList( entity );
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "DZConstructor" ) )
            {
                Helper_CheckDZConversionList( entity );
                Constructors.AddToConstructionList( entity );
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "DZUtilityStructure" ) )
            {
                Utilities.AddToConstructionList( entity );
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "DZJormugandr" ) )
            {
                DarkZenithPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( !data.IsJormugandr )
                    throw new Exception( entity.ToStringWithPlanet() + "has issues." );
                if ( !CanPlayerSeeJormugandr && entity.GetShouldBeVisibleBasedOnPlanetIntel() )
                {
                    CanPlayerSeeJormugandr = true;
                }

                Helper_CheckDZConversionList( entity );
                Jormugandr.AddToConstructionList( entity );
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "DZHjarn" ) )
            {
                Hjarnum.AddToConstructionList( entity );
                SortedHjarnum.AddToConstructionList( entity );
                if ( entity.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                    CanPlayerSeeFimbulwinter = true;
            }

            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "NadirBase" ) )
            {
                NadirBases.AddToConstructionList( entity );
            }
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "NadirSpawner" ) )
            {
                NadirSpawners.AddToConstructionList( entity );
            }
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads() )
            {
                if ( !entity.TypeData.IsMobileCombatant )
                    continue;
                if ( entity.TypeData.CanGoThroughWormholes && entity.FleetMembership.Fleet == entity.PlanetFaction.Faction.LooseFleet )
                    LooseFleetCombatShips.AddToConstructionList( entity );
                DarkZenithPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                if ( data != null && !data.HasRalliedToFlagship )
                    UnRalliedCombatShips.AddToConstructionList( entity );
            }

            if ( SortedHjarnum.Count > 0 )
            {
                SortedHjarnum.SortConstructionList( delegate ( SafeSquadWrapper L, SafeSquadWrapper R )
                {
                    //sort the list for use in the UI
                    return L.GetSecondsSinceEnteringThisPlanet().CompareTo( R.GetSecondsSinceEnteringThisPlanet() );
                } );
            }

            PlanetsControlled.SwitchConstructionToDisplay();
            Terminii.SwitchConstructionToDisplay();
            WarpingInTerminii.SwitchConstructionToDisplay();
            Epistyles.SwitchConstructionToDisplay();
            Flagships.SwitchConstructionToDisplay();
            WarpingInEpistyles.SwitchConstructionToDisplay();
            AllEconomicStructures.SwitchConstructionToDisplay();
            Harvesters.SwitchConstructionToDisplay();
            Utilities.SwitchConstructionToDisplay();
            Transports.SwitchConstructionToDisplay();
            Privateers.SwitchConstructionToDisplay();
            Constructors.SwitchConstructionToDisplay();
            Jormugandr.SwitchConstructionToDisplay();
            Hjarnum.SwitchConstructionToDisplay();
            SortedHjarnum.SwitchConstructionToDisplay();
            NadirBases.SwitchConstructionToDisplay();
            NadirSpawners.SwitchConstructionToDisplay();
            LooseFleetCombatShips.SwitchConstructionToDisplay();
            UnRalliedCombatShips.SwitchConstructionToDisplay();

            DZIsSuppressed.SwitchConstructionToDisplay();

            if ( PlayerAllied && !HasBuiltGolem )
            {
                foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( "DZTierThree" ) )
                {
                    HasBuiltGolem = true;
                }
            }

            
            if ( !CanPlayerSeeFimbulwinter )
            {
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( otherFaction.SpecialFactionData.InternalName == "DarkZenith" )
                    {
                        string allegiance = otherFaction.BaseInfo.Allegiance;
                        if ( ArcenStrings.Equals( allegiance, "Friendly To Players" ) )
                        {
                            HasAnyPlayerAllies = true;
                            break;
                        }
                    }
                }

                foreach ( Planet planet in World_AIW2.Instance.CurrentGalaxy.Planets( false ) )
                {
                    if ( planet.IsFimbulwintered )
                    {
                        //we play a different message if the player has friendly DZ
                        CanPlayerSeeFimbulwinter = true;
                        break;
                    }
                }
            }
            UnownedPlanetsInTerritorialSphereSim.ClearConstructionListForStartingConstruction();
            UnownedPlanetsInSphere( UnownedPlanetsInTerritorialSphereSim.GetConstructionList(), Context, PlanetsControlled.GetDisplayList() );
            UnownedPlanetsInTerritorialSphereSim.SwitchConstructionToDisplay();

            this.SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( Context );
        }
        protected abstract void SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context );
        #endregion
        #region GetMyAssignedEconomicMission
        private void GetMyAssignedEconomicMission( GameEntity_Squad entity, DarkZenithPerUnitBaseInfo data )
        {
            for ( int i = 0; i < this.EconomicMissions.Count; i++ )
            {
                VassalMission mission = this.EconomicMissions[i];
                if ( mission.OptionalEntityHandlingMission.GetSquad() == entity )
                {
                    data.MyMission = mission;
                    return;
                }
            }
        }
        #endregion

        #region GetShouldAttackNormallyExcludedTarget
        public override bool GetShouldAttackNormallyExcludedTarget( GameEntity_Squad Target )
        {
            if ( Target.TypeData.GetHasTag( "NormalPlanetNastyPick" ) || Target.TypeData.GetHasTag( "DSAA" ) )
                return true;
            if ( (AIWar2GalaxySettingTable.GetIsBoolSettingEnabledByName_DuringGame( "MaraudersKillCommandStations" ) || !PlayerAllied)
                 && (Target.TypeData.GetHasTag( "WarpGate" ) || Target.TypeData.IsCommandStation) )
                return true;
            return false;
        }
        #endregion

        #region HandleAlliances
        public void HandleAlliances( ArcenClientOrHostSimContextCore Context )
        {
            string allegiance = this.Allegiance;
            Faction faction = this.AttachedFaction;
            if (faction.Type == FactionType.Player)
            {
                this.IsPlayer = true;
                return;
            }
            if ( string.IsNullOrEmpty( allegiance ) )
            {
                this.SetNewAllegianceIntoCoreSettings( "Dark Alliance" );
            }
            if ( ArcenStrings.Equals( allegiance, "Dark Alliance" ) )
            {
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "Dark Alliance" );
                this.DarkAlliance = true;
            }
            else if ( ArcenStrings.Equals( allegiance, "Allied To AI" ) )
            {
                AllegianceHelper.AllyThisFactionToAI( faction );
                AIAllied = true;
            }
            else if ( ArcenStrings.Equals( allegiance, "Friendly To Players" ) )
            {
                AllegianceHelper.AllyThisFactionToHumans( faction );
                PlayerAllied = true;
                this.PlayerAllied = true;
            }
            else if ( ArcenStrings.Equals( allegiance, "Civil War" ) )
            {
                AllegianceHelper.SetAlliesForScourgeCivilWar( this.AttachedFaction );
                this.InCivilWar = true;
            }

            else if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Red" ) )
            {
                MinorFactionAllied = true;
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Red" );
                this.MinorFactionAllied = true;
            }
            else if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Blue" ) )
            {
                MinorFactionAllied = true;
                this.MinorFactionAllied = true;
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Blue" );
            }
            else if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Green" ) )
            {
                MinorFactionAllied = true;
                this.MinorFactionAllied = true;
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Green" );
            }
        }
        #endregion

        #region FindInitialEnemyAdjacentPlanets
        public void FindInitialEnemyAdjacentPlanets( ArcenSimContextAnyStatus Context )
        {
            if ( InitialEnemyAdjacentPlanets.Count > 0 )
                return; //already calculated
            for ( int i = 0; i < this.OriginalPlanets.Count; i++ )
            {
                Planet planet = this.OriginalPlanets[i];
                if ( planet == null )
                    continue;
                foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                {
                    if ( neighbor == null )
                        continue;
                    if ( !this.OriginalPlanets.Contains( neighbor ) )
                    {
                        InitialEnemyAdjacentPlanets.Add( planet );
                        break;
                    }
                }
            }
        }
        #endregion

        #region UnownedPlanetsInSphere
        public void UnownedPlanetsInSphere( List<Planet> unownedPlanets, ArcenSimContextAnyStatus Context, List<Planet> myOwnedPlanets )
        {
            //we must pass the lists in since we use thing in both the Sim and LRP code, and we keep separate lists
            if ( !this.AttachedFaction.SpecialFactionData.FullInvasionMode )
            {
                return; //this is only for full invasion
            }
            FindInitialEnemyAdjacentPlanets( Context );
            unownedPlanets.Clear();
            for ( int i = 0; i < InitialEnemyAdjacentPlanets.Count; i++ )
            {
                //find any enemy planets in our TerritorialSphere; these are our required initial targets, to consolidate our hold
                Planet planet = InitialEnemyAdjacentPlanets[i];
                foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( (short)(this.HopsOfSphereTaken + 1) ) )
                {
                    Planet otherPlanet = _phd.Planet;
                    if ( planet.IsZenithArchitraveTerritory )
                        continue;
                    if ( !myOwnedPlanets.Contains( otherPlanet ) )
                        unownedPlanets.Add( otherPlanet );
                }
            }
        }
        #endregion
        #region CanPlanetBuildHarvesters and Helpers
        public bool CanPlanetBuildHarvesters(Planet planet)
        {
            //There's a cap for harvesters based on the number of metal terminii
            int numTerminii = TerminiiOnPlanet( planet, DZResource.Metal );
            int numHarvesters = HarvestersOnPlanet( planet, true );
            if (numTerminii * Difficulty.MaxHarvestersPerMetalTerminus <= numHarvesters)
            {
                return false;
            }
            return true;
        }
        public int TerminiiOnPlanet( Planet planet, DZResource resource )
        {
            int numTerminii = 0;
            List<SafeSquadWrapper> terminii = this.Terminii.GetDisplayList();
            for ( int i = 0; i < terminii.Count; i++ )
            {
                GameEntity_Squad ship = terminii[i].GetSquad();
                if ( ship == null )
                    continue;
                if ( ship.Planet != planet )
                    continue;
                DarkZenithPerUnitBaseInfo data = ship.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( resource != DZResource.None &&
                     resource != data.Resource )
                    continue;
                numTerminii++;
            }
            return numTerminii;
        }
        public int HarvestersOnPlanet( Planet planet, bool countBuildingHarvesters = true )
        {
            int numHarvesters = 0;
            List<SafeSquadWrapper> harvesters = this.Harvesters.GetDisplayList();
            for ( int i = 0; i < harvesters.Count; i++ )
            {
                if ( harvesters[i].Planet != planet )
                    continue;
                numHarvesters++;
            }
            if (countBuildingHarvesters)
            {
                List<SafeSquadWrapper> terminii = this.Terminii.GetDisplayList();
                for (int i = 0; i < terminii.Count; i++)
                {
                    GameEntity_Squad ship = terminii[i].GetSquad();
                    if (ship == null)
                    {
                        continue;
                    }
                    DarkZenithPerUnitBaseInfo data = ship.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    if (data == null)
                    {
                        continue;
                    }
                    if (data.NextConversion != null &&
                        data.NextConversion.InternalName == "Build Harvester")
                    {
                        numHarvesters++;
                    }
                }
            }
            return numHarvesters;
        }
        #endregion
        #region ShouldShowEpistyleChoice / GetEpistyleChoiceBlockReason
        public bool ShouldShowEpistyleChoice(DZResourceConversion conversion)
        {
            //This is used by the DZ Sidekick's hacking code to decide if we want to give this as an option
            //to the player
            DZUpgrade upgrade = conversion.Upgrade;
            if ( upgrade != null && !IsUpgradeAllowed( upgrade, GetVariantUpgrades()))
            {
                return false;
            }
            return true;
        }

        // Returns a human-readable reason string if this conversion is not currently executable
        // due to a missing terminus, or null if it is fine to execute.
        // Callers should first check ShouldShowEpistyleChoice; this only handles the terminus gate.
        public string GetEpistyleChoiceBlockReason( DZResourceConversion conversion )
        {
            List<SafeSquadWrapper> terminii = this.Terminii.GetDisplayList();
            foreach ( KeyValuePair<DZResource, int> kv in conversion.Cost )
            {
                if ( kv.Value <= 0 ) continue;
                bool hasTerminus = false;
                for ( int i = 0; i < terminii.Count; i++ )
                {
                    GameEntity_Squad terminus = terminii[i].GetSquad();
                    if ( terminus == null ) continue;
                    DarkZenithPerUnitBaseInfo tData = terminus.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
                    if ( tData != null && tData.Resource == kv.Key )
                    {
                        hasTerminus = true;
                        break;
                    }
                }
                if ( !hasTerminus )
                    return "No " + ResourceFancyName[kv.Key] + " terminus";
            }
            return null;
        }
        #endregion
        #region Utility functions for DZ upgrades
        public bool IsUpgradeAllowed( DZUpgrade upgrade, int numVariantUpgrades )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.DarkZenith );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "DZ-IsUpgradeAllowed-trace", 10f ) : null;
            if ( tracing )
            tracingBuffer.Add( "GetNextConversion: \tChecking Upgrade " + upgrade.ToString() + "\n" );

            if ( upgrade.SingleUpgradeOnly && (HasUpgradeBeenDone( upgrade, tracingBuffer ) || this.IsAnotherEpistyleUpgradingForMe( upgrade, tracingBuffer )) )
            {
                if ( tracing )
                    tracingBuffer.Add( "GetNextConversion: \tUpdate path, dropping because already done/is already in progress\n" );
                if ( tracing )
                    FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
                return false;
            }
            if ( upgrade.PrereqUpgrade1 > 0 )
            {
                DZUpgrade prereq = DarkZenithUpgradeTable.Instance.GetRowById( upgrade.PrereqUpgrade1 );
                if (!HasUpgradeBeenDone( prereq, tracingBuffer ) )
                {
                    if ( tracing )
                        tracingBuffer.Add( "GetNextConversion: \tUpdate path, dropping due to missing prereq 1 (" + prereq.ToString() +")\n" );
                    if ( tracing )
                        FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );

                    return false;
                }
            }
            if ( upgrade.PrereqUpgrade2 > 0 )
            {
                DZUpgrade prereq = DarkZenithUpgradeTable.Instance.GetRowById( upgrade.PrereqUpgrade2 );
                if (!HasUpgradeBeenDone( prereq, tracingBuffer ) )
                {
                    if ( tracing )
                        tracingBuffer.Add( "GetNextConversion: \tUpdate path, dropping due to missing prereq 2 (" + prereq.ToString() + ")\n" );
                    if ( tracing )
                        FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );

                    return false;
                }
            }
            if ( numVariantUpgrades < upgrade.RequiredVariantUpgrades )
            {
                if ( tracing )
                    tracingBuffer.Add( "GetNextConversion: \tUpdate path, dropping due to only having " + numVariantUpgrades + " variant upgrades, instead of the required " + upgrade.RequiredVariantUpgrades +"\n" );
                if ( tracing )
                    FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );

                return false;
            }
            //Intensity gating exists to keep low-intensity AI/minor factions from getting top-tier
            //unlocks too early; the player isn't scaled by Intensity in the same way, so it shouldn't gate them
            if ( !this.IsPlayer && this.Intensity < upgrade.RequiredIntensity )
            {
                if ( tracing )
                    tracingBuffer.Add( "GetNextConversion: \tUpdate path, dropping due to only having intensity  " + this.Intensity + " instead of the required " + upgrade.RequiredIntensity +"\n" );
                if ( tracing )
                    FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );

                return false;
            }
            if ( tracing )
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );

            return true;
        }
        public int GetVariantUpgrades()
        {
            int numVariants = 0;
            for ( int i = 0; i < this.CompletedUpgrades.Count; i++ )
            {
                if ( this.CompletedUpgrades[i].UnlockShipVariant )
                    numVariants++;
            }
            return numVariants;
        }

        public bool HasUpgradeBeenDone( DZUpgrade upgrade, ArcenCharacterBuffer buffer = null)
        {
            for ( int i = 0; i < this.CompletedUpgrades.Count; i++ )
            {
                if ( this.CompletedUpgrades[i].UpgradeIndex == upgrade.UpgradeIndex )
                {
                    if ( buffer != null )
                    buffer.Add("Found completed upgrade index " + this.CompletedUpgrades[i].UpgradeIndex + this.CompletedUpgrades[i].InternalName);
                    return true;
                }
            }
            return false;
        }
        public bool IsAnotherEpistyleUpgradingForMe( DZUpgrade upgrade, ArcenCharacterBuffer tracingBuffer = null )
        {
            List<SafeSquadWrapper> epistyles = this.Epistyles.GetDisplayList();
            for ( int i = 0; i < epistyles.Count; i++ )
            {
                GameEntity_Squad ship = epistyles[i].GetSquad();
                if ( ship == null )
                    continue;
                DarkZenithPerUnitBaseInfo data = ship.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                if ( data.NextConversion != null && data.NextConversion._UpgradeIndex > 0 &&
                     tracingBuffer != null )
                tracingBuffer.Add("Checking if " + ship.ToStringWithPlanet() + " is also trying to upgrade for " + upgrade.InternalName + " idx " + upgrade.UpgradeIndex + "; that structure is upgrading for " + data.NextConversion._UpgradeIndex );
                if ( data.NextConversion != null &&
                     data.NextConversion._UpgradeIndex == upgrade.UpgradeIndex )
                    return true;
            }
            return false;
        }

        #endregion
        #region GetPlanetsInTerritorialSphere
        public int GetPlanetsInTerritorialSphere( ArcenSimContextAnyStatus Context )
        {
            FindInitialEnemyAdjacentPlanets( Context );

            int numPlanets = 0;
            for ( int i = 0; i < InitialEnemyAdjacentPlanets.Count; i++ )
            {
                //find any enemy planets in our TerritorialSphere; these are our required initial targets, to consolidate our hold
                Planet planet = InitialEnemyAdjacentPlanets[i];
                foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( (short)(this.HopsOfSphereTaken + 1) ) )
                {
                    Planet otherPlanet = _phd.Planet;
                    if ( this.OriginalPlanets.Contains( otherPlanet ) )
                        continue;
                    numPlanets++;
                }
                if ( numPlanets > MaxTerritorialSphereSize )
                    return MaxTerritorialSphereSize;
            }
            return numPlanets;
        }
        #endregion

        #region GetEnemiesInTerritorialSphere
        public void GetEnemiesInTerritorialSphere( List<Planet> listToFill, ArcenSimContextAnyStatus Context )
        {
            FindInitialEnemyAdjacentPlanets( Context );
            try
            {
                for ( int i = 0; i < InitialEnemyAdjacentPlanets.Count; i++ )
                {
                    //find any enemy planets in our TerritorialSphere; these are our required initial targets, to consolidate our hold
                    Planet planet = InitialEnemyAdjacentPlanets[i];
                    foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops( (short)(this.HopsOfSphereTaken + 1),
                        delegate ( Planet secondaryPlanet )
                        {
                            if ( secondaryPlanet.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "ZenithDysonSphere" ||
                                 secondaryPlanet.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "AntagonizedDysonSphere" )
                                return PropogationEvaluation.No;
                            if ( secondaryPlanet.TypeData.Type == PlanetType.Nomad && !World_AIW2.Instance.CurrentGalaxy.IsNomadGalaxy )
                                return PropogationEvaluation.No;

                            if ( ZenithArchitraveFactionBaseInfo.IsPlanetInAnyZATerritory( secondaryPlanet ) )
                                return PropogationEvaluation.No;
                            if ( secondaryPlanet.GetControllingFactionType() == FactionType.Player )
                                return PropogationEvaluation.No;
                            if ( secondaryPlanet.GetControllingFactionType() == FactionType.AI &&
                                 secondaryPlanet.MarkLevelForAIOnly.Ordinal == 7 )
                                return PropogationEvaluation.No;
                            return PropogationEvaluation.Yes;
                        } ) )
                    {
                        Planet otherPlanet = _phd.Planet;
                        if ( listToFill.Count >= MaxTerritorialSphereSize )
                            continue;
                        if ( listToFill.Contains( otherPlanet ) )
                            continue;
                        if ( otherPlanet.TypeData.Type == PlanetType.Nomad && !World_AIW2.Instance.CurrentGalaxy.IsNomadGalaxy )
                            continue;
                        //omit certain things, like ZA, players, ai homeworld stuff
                        if ( ZenithArchitraveFactionBaseInfo.IsPlanetInAnyZATerritory( otherPlanet ) )
                            continue;
                        if ( otherPlanet.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "ZenithDysonSphere" ||
                          otherPlanet.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "AntagonizedDysonSphere" )
                            continue;

                        if ( otherPlanet.GetControllingFactionType() == FactionType.Player )
                            continue; //don't go after player planets like this
                        if ( otherPlanet.GetControllingFactionType() == FactionType.AI &&
                             otherPlanet.MarkLevelForAIOnly.Ordinal == 7 )
                            continue; //don't go after planets too close to the AI homeworld
                        EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> myFactionData = otherPlanet.GetStanceDataForFaction( this.AttachedFaction );

                        if ( otherPlanet.GetControllingOrInfluencingFaction().GetIsHostileTowards( this.AttachedFaction ) )
                        {
                            listToFill.Add( otherPlanet );
                            continue;
                        }
                        if ( myFactionData[FactionStance.Hostile].TotalStrength >=
                          (myFactionData[FactionStance.Self].TotalStrength + myFactionData[FactionStance.Friendly].TotalStrength) / 2 )
                            listToFill.Add( otherPlanet );
                    }
                }
            }
            catch ( Exception e )
            {
                //only care about this error if not a client.
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLogSingleLine( "Exception in GetEnemiesInTerritorialSphere: " + e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region Helper_CheckDZConversionList
        private static void Helper_CheckDZConversionList( GameEntity_Squad entity )
        {
            if ( entity == null )
                return;
            DarkZenithPerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<DarkZenithPerUnitBaseInfo>();
            if ( data != null )
            {
                if ( data.ConversionList != null )
                {
                    for ( int i = 0; i < data.ConversionList.Count; i++ )
                    {
                        if ( data.ConversionList[i] == null )
                            throw new Exception( entity.ToStringWithPlanet() + " has a non-empty conversion list with null entries" );
                    }
                }
            }
        }
        #endregion

        //some utility functions

        #region AdditionalEpistylesAllowedToBuild
        public int AdditionalEpistylesAllowedToBuild()
        {
            int additional = 0;
            for ( int i = 0; i < this.CompletedUpgrades.Count; i++ )
            {
                DZUpgrade upgrade = this.CompletedUpgrades[i];
                if ( upgrade.IncreaseEpistylePerPlanetLimit ||
                     (upgrade.PlayerEpistyleLimitOverride && this.IsPlayer ))
                    additional += upgrade.RelatedInteger1;

            }
            return additional;
        }
        #endregion

        #region AdditionalTerminiiAllowedToBuild
        public int AdditionalTerminiiAllowedToBuild()
        {
            int additional = 0;
            for ( int i = 0; i < this.CompletedUpgrades.Count; i++ )
            {
                DZUpgrade upgrade = this.CompletedUpgrades[i];
                if ( this.IsPlayer && upgrade.PlayerEpistyleLimitOverride )
                     continue;
                if ( upgrade.IncreaseTerminusPerPlanetLimit )
                     additional += upgrade.RelatedInteger1;
            }
            return additional;
        }
        #endregion

        #region AdditionalUtilityAllowedToBuild
        public int AdditionalUtilityAllowedToBuild()
        {
            int additional = 0;
            for ( int i = 0; i < this.CompletedUpgrades.Count; i++ )
            {
                DZUpgrade upgrade = this.CompletedUpgrades[i];
                if ( upgrade.IncreaseUtilityPerPlanetLimit )
                    additional += upgrade.RelatedInteger1;
            }
            return additional;
        }
        #endregion

        // Apply benefits of upgrade to the DZ faction.
        // Should be called only at the time of an upgrades completion.
        // Applying the same upgrade multiple times should only be done if it is in fact completed multiple times.
        // Does not modify CompletedUpgrades, that is the callers responsibility.
        public void ApplyThisUpgrade( DZUpgrade upgrade )
        {
            List<SafeSquadWrapper> epistyles = this.Epistyles.GetDisplayList();

            if ( upgrade.GrantEpistylesPermanentResourceIncome )
            {
                for ( int j = 0; j < epistyles.Count; j++ )
                {
                    GameEntity_Squad epistyle = epistyles[j].GetSquad();
                    if ( epistyle == null )
                        continue;
                    DarkZenithPerUnitBaseInfo eData = epistyle.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    eData.PermanentBonusIncome[upgrade.RelatedResource] += upgrade.RelatedInteger1;
                }
            }
            if ( upgrade.IncreaseEpistylePerPlanetLimit )
            {
                //this is handled in the UpdatePotentialPlanetsToBuildOn() function,
                //where we check how many Epistyles are allowd
            }
            if ( upgrade.IncreaseTerminusPerPlanetLimit )
            {
                //this is handled in the UpdatePotentialPlanetsToBuildOn() function,
                //where we check how many Terminii are allowd
            }
            if ( upgrade.IncreaseUtilityPerPlanetLimit )
            {
                //this is handled in the UpdatePotentialPlanetsToBuildOn() function,
                //where we check how many utility buildings are allowd
            }
            if ( upgrade.UnlockShipTier || upgrade.UnlockShipVariant )
            {
                for ( int j = 0; j < epistyles.Count; j++ )
                {
                    GameEntity_Squad entity = epistyles[j].GetSquad();
                    if ( entity == null )
                        continue;
                    DarkZenithPerUnitBaseInfo data = entity.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    if ( data.CanBuildOffensiveUnits )
                    {
                        string tagToAdd = upgrade.GetTagForUpgrade();
                        DarkZenithResourceConversionTable.Instance.AddOffensiveConversionsWithTagToBag( data.ConversionBag, tagToAdd, 1, AttachedFaction );
                    }
                }
            }
            if ( upgrade.UnlockMarkLevel )
            {
                if ( upgrade.RelatedInteger1 > AttachedFaction.CurrentGeneralMarkLevel )
                    AttachedFaction.CurrentGeneralMarkLevel_Base = (byte)upgrade.RelatedInteger1;
            }
        }

        //Some static helper functions

        #region GetDZResourceFromString
        public static DZResource GetDZResourceFromString( string tempString )
        {
            if ( tempString == "Black" )
                return DZResource.Black;
            if ( tempString == "White" )
                return DZResource.White;
            if ( tempString == "Green" )
                return DZResource.Green;
            if ( tempString == "Blue" )
                return DZResource.Blue;
            if ( tempString == "Red" )
                return DZResource.Red;
            if ( tempString == "Metal" )
                return DZResource.Metal;
            return DZResource.None;
        }
        #endregion

        //Some static helper functions
        #region GetShipVariantTypeFromResource
        public static string GetShipVariantTypeFromResource( DZResource resource )
        {
            if ( resource == DZResource.White )
                return "Spirited";
            if ( resource == DZResource.Green )
                return "Stout";
            if ( resource == DZResource.Blue )
                return "Fortified";
            if ( resource == DZResource.Red )
                return "Enraged";
            if ( resource == DZResource.Black )
                return "Sinister";
            throw new Exception( "Unknown Resource " + resource.ToString() );
        }
        #endregion

        #region GetRandomResourceType
        public static DZResource GetRandomResourceType( Planet planet )
        {
            //when we do the initial planet creation
            int maxNum = (int)DZResource.End;
            DZResource resource = (DZResource)(planet.Index % maxNum);
            return resource;
        }

        public static DZResource GetRandomResourceType( ArcenSimContextAnyStatus Context )
        {
            //when we do the initial planet creation
            int maxNum = (int)DZResource.End;
            return (DZResource)Context.RandomToUse.Next( 0, maxNum );
        }
        #endregion

        #region GetTerminusTagFromResource
        public static string GetTerminusTagFromResource( DZResource resource )
        {
            if ( resource == DZResource.Metal )
                return "DZMetalTerminus";
            if ( resource == DZResource.Green )
                return "DZGreenTerminus";
            if ( resource == DZResource.White )
                return "DZWhiteTerminus";
            if ( resource == DZResource.Blue )
                return "DZBlueTerminus";
            if ( resource == DZResource.Red )
                return "DZRedTerminus";
            if ( resource == DZResource.Black )
                return "DZBlackTerminus";
            throw new Exception( "Attempted to get terminus for resource " + resource.ToString() );
            //return "DZTerminus"; //generic
        }
        #endregion

        #region UpdatePowerLevel
        public override void UpdatePowerLevel()
        {
            FInt result = FInt.Zero;
            int totalPlanets = World_AIW2.Instance.CurrentGalaxy.GetCountOfNonDestroyedPlanets();
            int increment = totalPlanets / 8;
            if ( this.PlayerAllied || this.MinorFactionAllied )
            {
                //Svikari now generate som Overall Power Level, thus allowing them to help trigger exo units
                //the response is much lower since I don't want to disturb existing balance too much
                increment = totalPlanets / 6;
                int units =  this.PlanetsControlled.Count / increment;
                this.AttachedFaction.OverallPowerLevel = units * FInt.FromParts(0, 140);
                return;
            }
            if ( this.IsPlayer )
            {
                increment = totalPlanets / 5;
                int units =  this.PlanetsControlled.Count / increment;
                this.AttachedFaction.OverallPowerLevel = units * FInt.FromParts(0, 350);
                return;
            }

            if ( this.OriginalPlanets.Count == 0 )
            {
                this.AttachedFaction.OverallPowerLevel = result;
                return;
            }
            if ( !this.HasTakenTerritorialSphere )
            {
                this.AttachedFaction.OverallPowerLevel = result;
                return;
            }
            int conqueredPlanetsOverSphere = this.PlanetsControlled.Count - PlanetsInTerritorialSphere;

            if ( this.Terminii.Count == 0 || this.Epistyles.Count == 0 )
                return; // the dark zenith is dead
            if ( conqueredPlanetsOverSphere < 2 )
                return;
            int planetsToCount = conqueredPlanetsOverSphere - totalPlanets / 12;
            //if ( World_AIW2.Instance.GameSecond % 60 == 0 )
            //    ArcenDebugging.ArcenDebugLogSingleLine("incremenet " + increment + " planets over sphere: " + conqueredPlanetsOverSphere + " planets to count: " + planetsToCount, Verbosity.DoNotShow );

            result = FInt.FromParts( 2, 000 );
            while ( planetsToCount >= increment && result <= FInt.FromParts( 4, 000 ) )
            {
                result += FInt.One;
                planetsToCount -= increment;
            }
            this.AttachedFaction.OverallPowerLevel = result;
            //if ( World_AIW2.Instance.GameSecond % 60 == 0 )
            //    ArcenDebugging.ArcenDebugLogSingleLine("resulting power level: " + faction.OverallPowerLevel, Verbosity.DoNotShow );
        }
        #endregion

        #region GetFireteamById
        public override Fireteam GetFireteamById( int id )
        {
            return FireteamBaseUtility.GetFireteamById( this.Teams, id );
        }
        #endregion

        #region IsBuildPossible
        public bool IsBuildPossible( Dictionary<DZResource, int> Cost )
        {
            //Used by the sidekick to check if a particular resource conversion can be executed
            //Mostly it checks if we have enough terminii to afford the thing
            List<SafeSquadWrapper> terminii = this.Terminii.GetDisplayList();
            foreach ( KeyValuePair<DZResource, int> kv in Cost )
            {
                if ( kv.Value <= 0 )
                    continue;
                bool foundTerminus = false;
                for ( int i = 0; i < terminii.Count; i++ )
                {
                    GameEntity_Squad terminus = terminii[i].GetSquad();
                    if ( terminus == null )
                        continue;
                    DarkZenithPerUnitBaseInfo data = terminus.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    if ( data == null )
                        continue;
                    if ( data.Resource == kv.Key )
                    {
                        foundTerminus = true;
                        break;
                    }
                }
                if ( !foundTerminus )
                    return false; //we don't have a terminus to give us this resource type

            }
            return true;
        }
        #endregion

        #region GetDarkZenithSidekickUpgradesForDisplay
        public void GetDarkZenithSidekickUpgradesForDisplay(ArcenCharacterBufferBase buffer)
        {
            int debugCode = 0;
            //This is for the Sidekick, it displays when you click the Hacking menu
            try{
                debugCode = 100;
                buffer.Add( "There are " ).Add( this.CompletedUpgrades.Count, "334433" ).Add( " Upgrades:\n" );
                for ( int i = 0; i < this.CompletedUpgrades.Count; i++ )
                {
                    //The "Increase Resources" upgrades aren't worth monitoring here,
                    //When an Epistyle is created, it doesn't get any previous upgrades
                    //to motivate players to protect their old Epistyles
                    if ( this.CompletedUpgrades[i].IncreaseResourceProductionByPercentage ||
                         this.CompletedUpgrades[i].GrantEpistylesPermanentResourceIncome ) continue;
                    buffer.Add("idx ").Add(this.CompletedUpgrades[i].UpgradeIndex);
                    this.CompletedUpgrades[i].ToBuffer( ref buffer, true );
                    if ( i == this.CompletedUpgrades.Count - 1 )
                    buffer.Add( "\n" );
                }
                debugCode = 200;
            }catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in GetDarkZenithSidekickUpgradesForDisplay debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        #endregion
        #region GetDarkZenithSidekickStateForDisplay
        public void GetDarkZenithSidekickStateForDisplay( ArcenCharacterBufferBase buffer )
        {
            int debugCode = 0;
            //This is for the Sidekick, it displays when you click the Hacking menu
            try{
                List<SafeSquadWrapper> epistyles = this.Epistyles.GetDisplayList();
                List<SafeSquadWrapper> transports = this.Transports.GetDisplayList();
                List<SafeSquadWrapper> terminii = this.Terminii.GetDisplayList();
                List<SafeSquadWrapper> constructors = this.Constructors.GetDisplayList();

                debugCode = 55;
                // --- Summary header ---
                int nOffense = 0, nUtility = 0, nInfra = 0, nUpgrade = 0, nIdle = 0, nStarved = 0;
                int[] bottleneckCounts = new int[(int)DZResource.End + 1];
                for ( int i = 0; i < epistyles.Count; i++ )
                {
                    GameEntity_Squad ep = epistyles[i].GetSquad();
                    if ( ep == null ) continue;
                    DarkZenithPerUnitBaseInfo epd = ep.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    if ( epd == null ) continue;
                    if ( epd.NextConversion == null )
                    {
                        nIdle++;
                    }
                    else
                    {
                        if ( epd.NextConversion.Upgrade != null )
                            nUpgrade++;
                        else if ( epd.NextConversion.IsOffensive )
                            nOffense++;
                        else if ( epd.NextConversion.IsUtility )
                            nUtility++;
                        else
                            nInfra++;
                        if ( !epd.CanWeDoResourceConversion( epd.NextConversion ) )
                        {
                            nStarved++;
                            foreach ( KeyValuePair<DZResource, int> kv in epd.NextConversion.Cost )
                            {
                                if ( kv.Value <= 0 ) continue;
                                int have = 0;
                                epd.Inventory.TryGetValue( kv.Key, out have );
                                if ( have < kv.Value )
                                    bottleneckCounts[(int)kv.Key]++;
                            }
                        }
                    }
                }
                buffer.Add( "── Economy Summary ──────────────────\n" );
                buffer.Add( "Epistyles: " ).Add( epistyles.Count ).Add( " total" );
                if ( nOffense > 0 ) buffer.Add( "  " ).Add( nOffense ).Add( " Offense", "ffa1a1" );
                if ( nUtility > 0 ) buffer.Add( "  " ).Add( nUtility ).Add( " Utility", "22a188" );
                if ( nInfra > 0 )   buffer.Add( "  " ).Add( nInfra ).Add( " Infra", "a1a1ff" );
                if ( nUpgrade > 0 ) buffer.Add( "  " ).Add( nUpgrade ).Add( " Upgrade", "a1ffa1" );
                if ( nIdle > 0 )    buffer.Add( "  " ).Add( nIdle ).Add( " Idle", "888888" );
                buffer.Add( "\n" );
                if ( nStarved > 0 )
                {
                    buffer.Add( "Starved: " ).Add( nStarved, "ffaa44" ).Add( " Epistyle(s) waiting 鈥?" );
                    bool firstRes = true;
                    for ( int r = 1; r < (int)DZResource.End; r++ )
                    {
                        if ( bottleneckCounts[r] == 0 ) continue;
                        if ( !firstRes ) buffer.Add( ", " );
                        DZResource res = (DZResource)r;
                        buffer.Add( ResourceFancyName[res], ResourceColour[res] ).Add( " (" ).Add( bottleneckCounts[r] ).Add( ")" );
                        firstRes = false;
                    }
                    buffer.Add( "\n" );
                }
                else if ( epistyles.Count > 0 )
                {
                    buffer.Add( "All Epistyles have resources to proceed\n", "a1ffa1" );
                }
                buffer.Add( "鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€\n" );

                debugCode = 100;
                int nLoaded = 0, nTransEmpty = 0;
                for ( int i = 0; i < transports.Count; i++ )
                {
                    GameEntity_Squad t = transports[i].GetSquad();
                    if ( t == null ) continue;
                    DarkZenithPerUnitBaseInfo td = t.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    if ( td == null ) continue;
                    if ( td.HasAnyResourcesAtAll() ) nLoaded++; else nTransEmpty++;
                }
                buffer.Add( "Transports: " ).Add( this.Transports.Count );
                if ( this.Transports.Count > 0 )
                    buffer.Add( "  (" ).Add( nLoaded ).Add( " loaded / " ).Add( nTransEmpty ).Add( " empty)" );
                buffer.Add( "\n" );

                buffer.Add( "Epistyles: " ).Add( this.Epistyles.Count ).Add( ":\n" );
                debugCode = 300;
                for ( int i = 0; i < epistyles.Count; i++ )
                {
                    debugCode = 400;
                    GameEntity_Squad epistyle = epistyles[i].GetSquad();
                    if ( epistyle == null ) continue;
                    debugCode = 410;
                    DarkZenithPerUnitBaseInfo data = epistyle.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    if ( data == null ) continue;
                    debugCode = 420;
                    buffer.Add( "\t" ).Add( epistyle.Planet.Name, "cca1ff" ).Add( "  ->  " );
                    if ( data.NextConversion == null )
                    {
                        buffer.Add( "IDLE", "888888" );
                        int stall = data.TimeWeLastDidConversion >= 0 ? World_AIW2.Instance.GameSecond - data.TimeWeLastDidConversion : -1;
                        if ( stall > 0 )
                        {
                            int sm = stall / 60, ss = stall % 60;
                            buffer.Add( "  (" ).Add( sm > 0 ? sm + "m " + ss + "s" : ss + "s", "888888" ).Add( ")" );
                        }
                    }
                    else
                    {
                        buffer.Add( data.NextConversion.DisplayName, "a1ffa1" );
                        if ( data.KeepConversion ) buffer.Add( " (Locked)", "ffa1a1" );
                        if ( data.HighPriority )   buffer.Add( " (Priority)", "a1a1ff" );
                        if ( !data.CanWeDoResourceConversion( data.NextConversion ) )
                        {
                            bool firstMiss = true;
                            foreach ( KeyValuePair<DZResource, int> kv in data.NextConversion.Cost )
                            {
                                if ( kv.Value <= 0 ) continue;
                                int have = 0;
                                data.Inventory.TryGetValue( kv.Key, out have );
                                if ( have >= kv.Value ) continue;
                                if ( firstMiss ) { buffer.Add( "  [needs ", "ffaa44" ); firstMiss = false; }
                                else buffer.Add( ", " );
                                buffer.Add( ResourceFancyName[kv.Key], ResourceColour[kv.Key] )
                                      .Add( "x" ).Add( (kv.Value - have) );
                            }
                            if ( !firstMiss ) buffer.Add( "]" );
                            int stall = data.TimeWeLastDidConversion >= 0 ? World_AIW2.Instance.GameSecond - data.TimeWeLastDidConversion : -1;
                            if ( stall > 60 )
                            {
                                int sm = stall / 60, ss = stall % 60;
                                buffer.Add( "  (stalled " ).Add( sm > 0 ? sm + "m " + ss + "s" : ss + "s", "ffaa44" ).Add( ")" );
                            }
                        }
                    }
                    buffer.Add( "\n" );
                }
                debugCode = 500;
                buffer.Add( "Terminii: " ).Add( this.Terminii.Count ).Add( ":\n" );
                for ( int i = 0; i < terminii.Count; i++ )
                {
                    debugCode = 600;
                    GameEntity_Squad terminus = terminii[i].GetSquad();
                    if ( terminus == null ) continue;
                    debugCode = 610;
                    DarkZenithPerUnitBaseInfo data = terminus.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    if ( data == null ) continue;
                    debugCode = 620;
                    if ( terminus.SecondsTillTransformation > 0 )
                    {
                        debugCode = 630;
                        buffer.Add( "\t" ).Add( "Warping in on " ).Add( terminus.Planet.Name, "a1ffa1" ).Add( "\n" );
                    }
                    else
                    {
                        debugCode = 640;
                        int stock = 0;
                        data.Inventory.TryGetValue( data.Resource, out stock );
                        buffer.Add( "\t" ).Add( terminus.Planet.Name, "a1ffa1" ).Add( "  " )
                              .Add( ResourceFancyName[data.Resource], ResourceColour[data.Resource] )
                              .Add( ": " ).AddNumberMoreReadable( stock );
                        bool hasInbound = false;
                        for ( int ti = 0; ti < transports.Count; ti++ )
                        {
                            GameEntity_Squad tr = transports[ti].GetSquad();
                            if ( tr == null ) continue;
                            DarkZenithPerUnitBaseInfo trd = tr.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                            if ( trd != null && trd.Destination == terminus ) { hasInbound = true; break; }
                        }
                        if ( hasInbound )
                            buffer.Add( "  [transport inbound]", "a1ffa1" );
                        else if ( stock == 0 )
                            buffer.Add( "  [no transport]", "ffaa44" );
                        buffer.Add( "\n" );
                    }
                }

                debugCode = 700;
                buffer.Add("\n").Add( "Constructors: " ).Add( this.Constructors.Count ).Add( "\n" );
                for ( int i = 0; i < constructors.Count; i++ )
                {
                    debugCode = 800;
                    GameEntity_Squad constructor = constructors[i].GetSquad();
                    if ( constructor == null ) continue;
                    DarkZenithPerUnitBaseInfo data = constructor.CreateExternalBaseInfo<DarkZenithPerUnitBaseInfo>( "DarkZenithPerUnitBaseInfo" );
                    if ( data == null ) continue;
                    debugCode = 810;
                    Planet dest = World_AIW2.Instance.GetPlanetByIndex( data.DZConstructorTargetPlanetIndex);
                    buffer.Add("\t").Add("Constructor on ").Add(constructor.Planet.Name, "a1a1ff");
                    debugCode = 820;
                    if ( dest == null )
                    {
                        buffer.Add(" Doesn't know what it is doing.");
                    }
                    else
                    {
                        if ( data.Unit != null )
                        {
                            string color = "ffa1a1";
                            if ( data.Unit.GetHasTag("WarpingInDZTerminus"))
                            {
                                color = ResourceColour[data.Resource];
                            }
                            buffer.Add(" will build ").Add( data.Unit.GetShortDisplayName(), color);
                        }
                        if ( dest != constructor.Planet )
                             buffer.Add(" on ").Add( dest.Name, "a1ffa1");
                    }
                    buffer.Add("\n");
                }
                debugCode = 900;
                buffer.Add( "\n" );
                buffer.Add("Epistyles per planet: ").AddColor( Difficulty.MaxEpistylesPerPlanet, "a1ffa1" ).Add(" + " ).AddColor( this.AdditionalEpistylesAllowedToBuild(), "a1ffa1" ).Add("\n");
                buffer.Add("Terminii per planet: ").AddColor( Difficulty.MaxTerminiiPerPlanet, "a1ffa1" ).Add(" + " ).AddColor( this.AdditionalEpistylesAllowedToBuild(), "a1ffa1" ).Add("\n");
                buffer.Add("Harvesters per Metal Terminus: ").AddColor( Difficulty.MaxHarvestersPerMetalTerminus, "a1ffa1" ).Add("\n");
            }
            catch ( Exception e)
            {
                ArcenDebugging.LogSingleLine("Hit exception in GetDarkZenithSidekickStateForDisplay " + e.ToString() + " debugCode " + debugCode, Verbosity.DoNotShow );
            }
        }
        #endregion

        #region GetDarkZenithStateForDisplay
        public void GetDarkZenithStateForDisplay( ArcenCharacterBufferBase buffer )
        {
            //For debug for the Dark Zenith faction; the sidekick has its own logging
            if ( this.AttachedFaction.InvasionTime >= 0 )
                buffer.Add( "The invasion will start in " ).Add( (this.AttachedFaction.InvasionTime - World_AIW2.Instance.GameSecond), "a1ffa1" ).Add( " seconds (" + AttachedFaction.GetStringValueForCustomFieldOrDefaultValue( "InvasionTime", true)+"\n" );
            if ( !this.HasTakenTerritorialSphere )
                buffer.Add( "The DZ has not yet taken its Territorial Sphere; it controls " + this.HopsOfSphereTaken + " hops though.\n" );
            if ( this.TimeForNextExo > 0 )
                buffer.Add( "The AI will next send an exo against the DZ in " ).Add( (this.TimeForNextExo - World_AIW2.Instance.GameSecond), "a1ffa1" ).Add( " seconds.\n" );
            buffer.Add( "There are " + this.Jormugandr.Count + " Jormugandr.\n" );
            buffer.Add( "There are " ).Add( this.CompletedUpgrades.Count, "334433" ).Add( " Upgrades:\n" );

            for ( int i = 0; i < this.CompletedUpgrades.Count; i++ )
            {
                this.CompletedUpgrades[i].ToBuffer( ref buffer, true );
                if ( i == this.CompletedUpgrades.Count - 1 )
                    buffer.Add( "\n" );
            }
            buffer.Add( "Faction overall mark level: " ).Add( this.AttachedFaction.CurrentGeneralMarkLevel ).Add( " (+" ).Add( this.AttachedFaction.CurrentGeneralMarkLevel_Added ).Add( ")\n" );
            buffer.Add( "\n" );
            buffer.Add( "Transports: " ).Add( this.Transports.Count ).Add( "\n" );
            buffer.Add( "Epistyles: " ).Add( this.Epistyles.Count ).Add( "\n" );
            buffer.Add( "Terminii: " ).Add( this.Terminii.Count ).Add( "\n" );
            buffer.Add( "\n" );

            int totalStrength = 0;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( this.Teams ) )
            {
                totalStrength += team.DeepInfo.TeamStrength;
                if ( team.status != FireteamStatus.Disbanded )
                {
                    team.DeepInfo.GetStringForDisplay( buffer );
                    buffer.Add( "\n" );
                }
            }
            buffer.Add( "Total DZ Strength: <color=#ff0000>" + (totalStrength / 1000) + "</color>.\n\n" );
        }
        #endregion

        #region DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost
        public override void DoPerSecondNonSimNotificationUpdates_OnBackgroundNonSimThread_NonBlocking_ClientOrHost( ArcenClientOrHostSimContextCore Context, bool IsFirstCallToFactionOfThisTypeThisCycle )
        {
            //do this for the first faction of the type only, regardless of how many there are
            if ( !IsFirstCallToFactionOfThisTypeThisCycle )
                return;
            int debugCode = 0;
            try
            {
                debugCode = 100;

                if ( AttachedFaction.SpecialFactionData.FullInvasionMode &&
                      AttachedFaction.InvasionTime < World_AIW2.Instance.GameSecond &&
                      this.TimeToLinkPlanets > World_AIW2.Instance.GameSecond )
                {
                    debugCode = 200;
                    NotifierFillData fillData = NotifierFillData.GetFromPoolOrCreate();
                    fillData.eventTimeRemaining = this.TimeToLinkPlanets;
                    fillData.Faction = AttachedFaction;
                    NotificationNonSim notification = new NotificationNonSim();
                    SortedNotificationPriorityLevel priority = SortedNotificationPriorityLevel.Major;
                    if ( this.TimeToLinkPlanets - World_AIW2.Instance.GameSecond < 60 )
                        priority = SortedNotificationPriorityLevel.OMG;
                    notification.Assign( PublicDZInvasionNotifier.Instance, fillData, "", 0, "Dark Zenith Invasion", priority );
                }
                debugCode = 300;
                NotifierFillData hjarnFillData = null;
                if ( this.Hjarnum.Count > 0 )
                {
                    List<SafeSquadWrapper> hjarnum = this.SortedHjarnum.GetDisplayList();
                    for ( int j = 0; j < hjarnum.Count; j++ )
                    {
                        debugCode = 400;
                        GameEntity_Squad entity = hjarnum[j].GetSquad();
                        if ( entity == null )
                            continue;
                        debugCode = 450;
                        if ( entity.Planet.IntelLevel == PlanetIntelLevel.Unexplored )
                            continue; //no notifications
                        debugCode = 460;
                        if ( hjarnFillData == null )
                        {
                            debugCode = 470;
                            hjarnFillData = NotifierFillData.GetFromPoolOrCreate();
                            hjarnFillData.Faction = AttachedFaction;
                            hjarnFillData.EntityList.Clear();
                        }
                        debugCode = 480;
                        hjarnFillData.EntityList.Add( entity );
                    }
                    if ( hjarnFillData != null )
                    {
                        NotificationNonSim notification = new NotificationNonSim();
                        SortedNotificationPriorityLevel priority = SortedNotificationPriorityLevel.Informational;
                        notification.Assign( PublicDZHjarnNotifier.Instance, hjarnFillData, "", 0, "Fimbulwinter", priority );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in DZ notifications debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        #endregion
    }
}
