using Arcen.AIW2.Core;
using Arcen.Universal;
using System;


using System.Text;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// TEACHING_MOMENT: Okay, so here's the deal.  We CAN'T split fireteams into BaseInfo and DeepInfo in the way that I'd really love to.  
    /// I mean, we could if I moved it into Core, and if we really re-coded the entire data structure, but overall it would not be a wise idea or a good use of time.  
    /// So the next best thing is to make a sub- object that says "Hey, seriously, don't touch this for UI purposes that are not for debugging.  You'll get errors or a mess on MP clients."
    /// 
    /// This is a pattern that is not a bad idea to copy for mods or other types complex data that need separation.
    /// The main downside of this pattern is that it is not truly idiot-proof (against ourselves, again not intending offsense), 
    /// but it does provide a strong deterrent and a clear marking.  There is nothing worse than _not knowing_ you are wandering into dangerous territory.
    /// 
    /// For practical purposes, then, how does this class work?  If it's dangerous territory, what are the rules:
    /// 
    /// 1. Code in this class MAY update serialized fields on the Fireteam class or similar.  This will work fine, as it gets translated to the MP client then.
    /// 2. Code in this class SHOULD NEVER update non-serialized fields on the Fireteam class or similar.  The MP client would be blind to whatever happened there.
    /// 3. Host-only code can call into this class for data -- serialized or not -- as well as methods.  Go nuts!
    /// 4. Client-and-host code should act like this class doesn't even exist.  This includes all UI code, unless you need to just have a debug outpu (that would be host-only).
    /// 
    /// The purpose of these rules is to make sure that the client is never breaking unexpectedly.  You don't want to have to test your code extensively in MP to know it works.
    /// It doesn't really matter who "you" are reading this, none of us want to do that.  We all want to be able to test as rapidly as possible by ourselves and trust it will work.
    /// Follow the four rules above, and you should be able to trust in that way.  If you have another class sometime that has a similar nature to its mix of data and code, then mimic this.
    /// </summary>
    public class FireteamHonoraryDeepInfo
    {
        public readonly Fireteam BaseInfo;
        public FireteamHonoraryDeepInfo( Fireteam Team )
        {
            this.BaseInfo = Team;
        }

        //serialized

        //non-serialized
        public readonly List<SafeSquadWrapper> ShipsInFireteam = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 30, "FireteamHonoraryDeepInfo-ShipsInFireteam" ); //the ships list is rebuilt every LRP step by checking the FireteamId on each unit
        public bool TargetProtectedByForcefield;
        public Planet CurrentPlanet; //for generic proximity or pathfinding safety checks, use the planet with the most ships
        public byte PercentCurrentPlanet; //what percentage of ships are on the current planet
        public int TeamStrength;
        public int LurkingStrength;
        public byte HighestMarkUnit;
        //Counter-awareness: strength-weighted outgoing damage multiplier this team brings against a target in each
        //mass/armor bin. TeamAntiMass[bin] == 危 shipStrength 脳 that ship's OutgoingDamageProfile.AntiMass[bin], so with
        //no relevant counters these equal TeamStrength (all 1.0x) and above that means real bonus coverage of that bin.
        public readonly int[] TeamAntiMass = new int[CompositionBins.MassBinCount];
        public readonly int[] TeamAntiArmor = new int[CompositionBins.ArmorBinCount];
        public readonly int[] TeamAntiAlbedo = new int[CompositionBins.AlbedoBinCount];
        public readonly int[] TeamAntiEnergy = new int[CompositionBins.EnergyBinCount];
        //Shield-bypass: 危 shipStrength 脳 ShieldBypassFraction. Unlike the bin arrays (baseline 1.0脳), baseline here is 0
        //(no bypass). Used to give a bonus against turret-heavy planets where personal shields provide real protection.
        public int TeamAntiShield;
        //Zombify: total strength of ships that have a zombification/nanocaustation death effect. Baseline 0.
        //Used to give a bonus against light-mass defenders, which are smaller and more easily converted.
        public int TeamAntiZombify;
        public bool IncludeShipsInTransit = true;
        public bool ShouldPrioritizeUndefendedPlanets; //implemented on a per-faction basis when getting targets. Currently only used by the ZA
        public bool FireteamShouldDisband = false; //generally indicates something is wrong with the composition and the units should find a new fireteam
        //used for rallying/attacking
        public ConcurrentDictionaryOfLists<Planet, SafeSquadWrapper> shipsByPlanet = 
            ConcurrentDictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( Engine_Universal.DEFAULT_CONCURRENCY_LEVEL, 20, 30, "FireteamHonoraryDeepInfo-shipsByPlanet" );

        #region InitializeToDefaults
        internal void InitializeToDefaults()
        {
            ShipsInFireteam.Clear();
            this.TeamStrength = -1;
            this.PercentCurrentPlanet = 0;
            this.LurkingStrength = -1;
            this.CurrentPlanet = null;
            this.HighestMarkUnit = 0;
            this.TargetProtectedByForcefield = false;
            this.ShouldPrioritizeUndefendedPlanets = false;
            for ( int b = 0; b < TeamAntiMass.Length; b++ )
                TeamAntiMass[b] = 0;
            for ( int b = 0; b < TeamAntiArmor.Length; b++ )
                TeamAntiArmor[b] = 0;
            for ( int b = 0; b < TeamAntiAlbedo.Length; b++ )
                TeamAntiAlbedo[b] = 0;
            for ( int b = 0; b < TeamAntiEnergy.Length; b++ )
                TeamAntiEnergy[b] = 0;
            this.TeamAntiShield = 0;
            this.TeamAntiZombify = 0;
            this.shipsByPlanet.Clear();
        }
        #endregion

        #region Ser / Deser
        internal void SerializeTo_DiskOnly( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            //nothing to do for now!  Everything we would send to disk, we are sending to the client.
        }

        internal void DeserializedIntoSelf_DiskOnly( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType, string ForDebugging_FactionName )
        {
            //nothing to do for now!  Everything we would send to disk, we are sending to the client.
        }
        #endregion

        #region IdentifyCurrentPlanet
        public void IdentifyCurrentPlanet()
        {
            BuildShipsLookup( IncludeShipsInTransit, null );

            int mostShips = 0;
            //From MSDN: The enumerator returned from the dictionary is safe to use concurrently with reads and writes to the dictionary, 
            //however it does not represent a moment-in-time snapshot of the dictionary. The contents exposed through the enumerator may 
            //contain modifications made to the dictionary after GetEnumerator was called.
            foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> kv in this.shipsByPlanet )
            {
                if ( kv.Key == null || kv.Value == null || kv.Value.Count == 0 )
                    continue;
                if ( kv.Value.Count > mostShips )
                {
                    this.CurrentPlanet = kv.Key;
                    mostShips = kv.Value.Count;
                }
            }
            if ( ShipsInFireteam.Count == 0 )
                this.PercentCurrentPlanet = 0;
            else
                this.PercentCurrentPlanet = (byte)((mostShips * 100) / ShipsInFireteam.Count);
        }
        #endregion

        #region BuildShipsLookup
        public void BuildShipsLookup( bool includeShipsInTransit, Planet destinationOrNull )
        {
            shipsByPlanet.Clear();
            //This is a helper function for moving Fireteams around, and also figuring out where its units are/threat analysis
            if ( !includeShipsInTransit && destinationOrNull == null )
                throw new Exception( "You requested to ignore ships in transit but didn't say the destination! " + this.ToString() );
            this.PurgeDeadUnits();
            int debugCode = 0;
            try
            {
                for ( int i = 0; i < this.ShipsInFireteam.Count; i++ )
                {
                    debugCode = 100;
                    GameEntity_Squad entity = this.ShipsInFireteam[i].GetSquad();
                    if ( entity == null || entity.Planet == null )
                        continue;
                    if ( !includeShipsInTransit )
                    {
                        debugCode = 200;
                        if ( entity.GetDestinationPlanet() == destinationOrNull )
                            continue;
                    }
                    debugCode = 400;
                    shipsByPlanet[entity.Planet].Add( entity );
                    debugCode = 410;
                }
            }
            catch ( ArcenPleaseStopThisThreadException )
            {
                //this is the main thread telling us to stop, guess we'll skip reporting from here
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in BuildShipsLookup debugCode " + debugCode + " exception " + e.ToString(), Verbosity.ShowAsError );
            }
        }
        #endregion

        #region DisbandAndRetreat
        public void DisbandAndRetreat( Faction faction, ArcenLongTermIntermittentPlanningContextBase ContextOrNull, PerFactionPathCache PathCacheData, GameEntity_Squad retreatDestination )
        {
            //We've lost this battle, so retreat (to an armory at the moment) and regroup into new Fireteams
            if ( retreatDestination != null && ContextOrNull != null )
            {
                BuildShipsLookup( IncludeShipsInTransit, null );
                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in this.shipsByPlanet )
                {
                    FactionUtilityMethods.Instance.Helper_RaidSpecificPlanet( pair.Value, pair.Key, faction, World_AIW2.Instance.CurrentGalaxy, 
                        retreatDestination.Planet, false, ContextOrNull, PathCacheData, 
                        BaseGameCommand.Code.SetWormholePath_UtilRaidAfterDisband, //since these are run very frequently, let's see them separately so we know if they are counting up a bunch.
                        0f ); //since the team is being disbanded, make sure that this order goes through no matter what
                }
            }
            BaseInfo.Disband( faction, ContextOrNull );
        }
        #endregion

        #region PurgeDeadUnits
        public void PurgeDeadUnits()
        {
            //For longer-running LRP threads (like the ones that typically have Fireteams,
            //it's possible for some units to be killed by the Sim between creating the ships list and
            //when it is used. Sometimes the Objects are even reused.
            //Make sure we remove any already dead units before we might give any orders https://www.youtube.com/watch?v=UcZzlPGnKdU
            for ( int i = this.ShipsInFireteam.Count - 1; i >= 0; i-- )
            {
                GameEntity_Squad ship = this.ShipsInFireteam[i].GetSquad();
                if ( ship == null )
                {
                    this.ShipsInFireteam.RemoveAt( i );
                    continue;
                }
                if ( ship.GetHasBeenDestroyed() )
                {
                    //if this unit has been killed
                    this.ShipsInFireteam.RemoveAt( i );
                    continue;
                }
                //note from Chris: it's entirely possible and valid that there might not be scourge data for a fireteam, and so just skip those.
                //this lets the scourge continue their stuff, but with GetScourgePerUnitBaseInfoExt() also being more careful in how it may return a null,
                //we wind up with the situation where it can safely return null and we don't care and it's not a bug.
                ScourgePerUnitBaseInfo data = ship.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                if ( data != null && ship.FireteamId != BaseInfo.FireTeamID )
                {
                    //if this pooled ship object has been killed and then reused by another squad
                    this.ShipsInFireteam.RemoveAt( i );
                    continue;
                }
            }
        }
        #endregion

        #region CheckIfEnoughUnitsAreLurking
        public bool CheckIfEnoughUnitsAreLurking()
        {
            //Used for updating the FireteamStatus
            int strengthNotLurking = 0;
            for ( int i = 0; i < ShipsInFireteam.Count; i++ )
            {
                GameEntity_Squad ship = ShipsInFireteam[i].GetSquad();
                if ( ship == null )
                    continue;
                if ( ship.Planet != BaseInfo.LurkPlanet )
                    strengthNotLurking += ship.GetStrengthOfSelfAndContents();
            }
            if ( this.LurkingStrength <= (this.TeamStrength - this.LurkingStrength) )
                return false;
            return true;
        }
        #endregion

        #region GetNumberOfShipsThatCouldUpgrade
        public int GetNumberOfShipsThatCouldUpgrade()
        {
            int numberOfShipsReadyToUpgrade = 0;
            for ( int i = 0; i < ShipsInFireteam.Count; i++ )
            {
                GameEntity_Squad ship = ShipsInFireteam[i].GetSquad();
                if ( ship == null )
                    continue;
                if ( ship.CurrentMarkLevel == 7 )
                    continue;
                ScourgePerUnitBaseInfo data = ship.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                if ( data != null && data.Experience >= data.ExperienceForNextLevel )
                    numberOfShipsReadyToUpgrade++;
            }
            return numberOfShipsReadyToUpgrade;
        }
        #endregion

        #region ShouldFireteamDisbandSoItCanUpgrade
        public bool ShouldFireteamDisbandSoItCanUpgrade( int requiredPercent )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client ) //don't have clients proactively disbanding fireteams!
                return false;
            //Used for updating the FireteamStatus
            if ( this.GetNumberOfShipsThatCouldUpgrade() > (ShipsInFireteam.Count * requiredPercent) / 100 )
                return true;
            return false;
        }
        #endregion

        #region Disband_JustTheDeepInfoPortion
        internal void Disband_JustTheDeepInfoPortion( Faction ForFaction, ArcenLongTermIntermittentPlanningContextBase ContextOrNull )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return;
            if ( this.ShipsInFireteam.Count == 0 )
                return;
            SetWaitingAppropriately( ForFaction, ContextOrNull ); //this only happens on the host
            int preCount = this.ShipsInFireteam.Count;
            for ( int i = this.ShipsInFireteam.Count - 1; i >= 0; i-- )
            {
                GameEntity_Squad entity = this.ShipsInFireteam[i].GetSquad();
                if ( entity == null )
                    continue;
                ScourgePerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>(); //noted from Badger that this is fine if null
                if ( data != null )
                {
                    data.FireteamId = -1;
                    entity.FireteamId = -1;
                    entity.MinorFactionStackingID = -1;
                }
            }
            this.shipsByPlanet.Clear();
            this.ShipsInFireteam.Clear();
        }
        #endregion

        #region Reset
        public void Reset()
        {
            //Done at the beginning of LRP to flush stale data
            this.TeamStrength = 0;
            this.HighestMarkUnit = 0;
            for ( int b = 0; b < TeamAntiMass.Length; b++ )
                TeamAntiMass[b] = 0;
            for ( int b = 0; b < TeamAntiArmor.Length; b++ )
                TeamAntiArmor[b] = 0;
            for ( int b = 0; b < TeamAntiAlbedo.Length; b++ )
                TeamAntiAlbedo[b] = 0;
            for ( int b = 0; b < TeamAntiEnergy.Length; b++ )
                TeamAntiEnergy[b] = 0;
            this.TeamAntiShield = 0;
            this.TeamAntiZombify = 0;
            this.ShipsInFireteam.Clear();
        }
        #endregion

        #region AddUnit
        public bool AddUnit( GameEntity_Squad entity )
        {
            //Adds a unit to a fleet
            if ( entity.CurrentMarkLevel > this.HighestMarkUnit )
                this.HighestMarkUnit = entity.CurrentMarkLevel;
            this.ShipsInFireteam.Add( entity );
            this.TeamStrength += entity.GetStrengthOfSelfAndContents();
            entity.FireteamId = BaseInfo.FireTeamID;
            if ( !BaseInfo.IsAllowedToStack )
                entity.MinorFactionStackingID = BaseInfo.FireTeamID;
            return true;
        }
        #endregion

        #region RemoveUnitIfNecessary
        public void RemoveUnitIfNecessary( GameEntity_Squad entity )
        {
            if ( entity == null )
                return;
            //Removes a unit from a fleet
            if ( ShipsInFireteam.Contains( entity ) )
            {
                ShipsInFireteam.Remove( entity );
                ScourgePerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                entity.FireteamId = -1;
                entity.MinorFactionStackingID = -1;
                if ( data != null )
                    data.FireteamId = -1;
            }
        }
        #endregion

        #region UpdateNonSerializedFields_LRP
        public void UpdateNonSerializedFields_LRP( Faction faction, ArcenCharacterBuffer tracingBuffer, ArcenLongTermIntermittentPlanningContextBase Context )
        {
            if ( Context == null )
                return; //not LRP, or on client
            int debugCode = 0;
            try{
            //This is run to make sure that we have the TargetPlanet and LurkPlanet set (after reload)
            //and to do some debug checking of the units in the Fireteam
            bool debug = false;
            bool tracing = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge );
            int teamStrength = 0;
            int lurkingStrength = 0;
            debugCode = 100;
            if ( BaseInfo.Target == null || BaseInfo.Target.TypeData == null || BaseInfo.Target.Planet == null ||
                 BaseInfo.Target.GetHasBeenDestroyed() || BaseInfo.Target.GetIsCrippled() ||
                 BaseInfo.Target.SecondsSpentAsRemains > 0 ||
                 (BaseInfo.status != FireteamStatus.Escorting && !BaseInfo.Target.GetIsHostileTowards_Safe( faction )) )
            {
                BaseInfo.Target = null; //the target seems to have died or something
            }
            if ( BaseInfo.Target != null )
                this.TargetProtectedByForcefield = BaseInfo.GetIsTargetProtectedByForcefield( faction, BaseInfo.Target );
            else
                this.TargetProtectedByForcefield = false;
            debugCode = 200;
            GameEntity_Squad againstTarget = BaseInfo.AgainstTarget;
            if ( againstTarget != null )
            {
                //if we're not hostile to the target anymore, or it died, then blank it out.
                if ( againstTarget.TypeData == null || againstTarget.Planet == null || !againstTarget.GetIsHostileTowards_Safe( faction ) )
                    BaseInfo.SpecificationOrNull.AgainstGameEntity.Clear();
            }
            debugCode = 300;
            //speed groups are only to actually land the attack in a coordinated fashion; they
            //go away if the fireteam isn't attacking, or if the fireteam is on the target planet
            if ( BaseInfo.status != FireteamStatus.Attacking ||
                 BaseInfo.status == FireteamStatus.Attacking && this.CurrentPlanet == BaseInfo.TargetPlanet )
            {
                debugCode = 400;
                try
                {
                    GameCommand destroyCommand = null;
                    for ( int i = 0; i < this.ShipsInFireteam.Count; i++ )
                    {
                        GameEntity_Squad squad = this.ShipsInFireteam[i].GetSquad();
                        if ( squad == null || squad.GroupMoveSpeed_HostOnly == null || squad.GroupMoveSpeed_HostOnly.IsDummy )
                            continue;
                        if ( destroyCommand == null )
                        {
                            destroyCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.CreateSpeedGroup_Destroy], GameCommandSource.AnythingElse );
                            destroyCommand.RelatedString = "DestroySpeedGroups"; //don't create a speed group, just destroy it!
                        }
                        //if it is in ANY speed group, remove it.  They all can be different speed groups, we don't care
                        destroyCommand.RelatedEntityIDs.Add( squad.PrimaryKeyID );
                    }
                    if ( destroyCommand != null )
                        World_AIW2.Instance.QueueGameCommand( faction, destroyCommand, false );
                }
                catch ( ArcenPleaseStopThisThreadException )
                {}
                catch ( Exception e )
                {
                    ArcenDebugging.ArcenDebugLog( "Error in clearing fireteam speed groups: " + e.ToString(), Verbosity.ShowAsError );
                }
            }

            if ( tracing && debug )
                tracingBuffer.Add( "Updating non-serializable fields for fireteam " + BaseInfo.FireTeamID + " contents: " );
            this.PurgeDeadUnits();
            debugCode = 500;
            SetWaitingAppropriately( faction, Context );
            debugCode = 600;
            int highestSpeed = 0;
            int totalSpeed = 0;
            bool shouldPrioritizeUndefendedPlanets = false;
            int shipsCounted = 0;
            for ( int b = 0; b < TeamAntiMass.Length; b++ )
                TeamAntiMass[b] = 0;
            for ( int b = 0; b < TeamAntiArmor.Length; b++ )
                TeamAntiArmor[b] = 0;
            for ( int b = 0; b < TeamAntiAlbedo.Length; b++ )
                TeamAntiAlbedo[b] = 0;
            for ( int b = 0; b < TeamAntiEnergy.Length; b++ )
                TeamAntiEnergy[b] = 0;
            this.TeamAntiShield = 0;
            this.TeamAntiZombify = 0;
            for ( int idx = 0; idx < this.ShipsInFireteam.Count; idx++ )
            {
                debugCode = 700;
                GameEntity_Squad entity = ShipsInFireteam[idx].GetSquad();
                if ( entity == null )
                    continue;
                if ( entity.TypeData.GetHasTag( "ArchitravePioneer" ) )
                    shouldPrioritizeUndefendedPlanets = true;
                if ( entity.TypeData.GetHasTag("NeverInDefensiveFireteam") && BaseInfo.DefenseMode )
                {
                    //this is a bug
                    ArcenDebugging.ArcenDebugLogSingleLine("We have " + entity.ToStringWithPlanet() + " in defensive fireteam " + BaseInfo.FireTeamID +", which is not desirable. This fireteam should disband", Verbosity.DoNotShow );
                    this.FireteamShouldDisband = true;
                }
                int entityStrength = entity.GetStrengthOfSelfAndContents();
                teamStrength += entityStrength;
                if ( BaseInfo.LurkPlanet != null &&
                     entity.Planet == BaseInfo.LurkPlanet )
                {
                    lurkingStrength += entityStrength;
                }
                //Counter-awareness: fold this ship's strength-weighted anti-bin multipliers into the team profile
                //(see OutgoingDamageProfile). A null profile (not yet built) just counts as plain baseline strength.
                OutgoingDamageProfile odp = entity.TypeData?.OutgoingDamageProfile;
                for ( int b = 0; b < TeamAntiMass.Length; b++ )
                    TeamAntiMass[b] += odp != null ? (odp.AntiMass[b] * entityStrength).IntValue : entityStrength;
                for ( int b = 0; b < TeamAntiArmor.Length; b++ )
                    TeamAntiArmor[b] += odp != null ? (odp.AntiArmor[b] * entityStrength).IntValue : entityStrength;
                for ( int b = 0; b < TeamAntiAlbedo.Length; b++ )
                    TeamAntiAlbedo[b] += odp != null ? (odp.AntiAlbedo[b] * entityStrength).IntValue : entityStrength;
                for ( int b = 0; b < TeamAntiEnergy.Length; b++ )
                    TeamAntiEnergy[b] += odp != null ? (odp.AntiEnergy[b] * entityStrength).IntValue : entityStrength;
                if ( odp != null && odp.ShieldBypassFraction > FInt.Zero )
                    TeamAntiShield += (odp.ShieldBypassFraction * entityStrength).IntValue;
                if ( odp != null && odp.HasZombifyWeapon )
                    TeamAntiZombify += entityStrength;
                if ( entity.FireteamId != BaseInfo.FireTeamID )
                    entity.FireteamId = BaseInfo.FireTeamID;
                if ( BaseInfo.SpecificationOrNull != null && BaseInfo.SpecificationOrNull.IsActive() )
                {
                    //make sure the units have the right specification;
                    //we've seen cases where the Fireteam knew what it was doing but not the units
                    if ( entity.FireteamSpecificationOrNull == null || !entity.FireteamSpecificationOrNull.IsActive() )
                    {
                        if ( entity.FireteamSpecificationOrNull == null )
                            entity.FireteamSpecificationOrNull = FireteamRequiredTarget.GetFromPoolOrCreate();
                        entity.FireteamSpecificationOrNull.CopyFrom( BaseInfo.SpecificationOrNull );
                    }
                }
                debugCode = 800;
                if ( tracing && debug )
                    tracingBuffer.Add( entity.ToStringWithPlanet() ).Add( ", " );
                if ( entity.CalculatedSpeed > 0 )
                {
                    shipsCounted++;
                    totalSpeed += entity.CalculatedSpeedWithoutSpeedGroups;
                    if ( entity.CalculatedSpeed > highestSpeed )
                        highestSpeed = entity.CalculatedSpeedWithoutSpeedGroups;
                }
                debugCode = 900;
                ScourgePerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                if ( data == null || entity.TryGetFactionBaseInfoOrNullAs_Safe<ScourgeFactionBaseInfo>() == null )
                    continue;

                if ( data.FullyInitialized && entity.FireteamId != data.FireteamId )
                    throw new Exception( "Mismatch for " + entity.ToStringWithPlanet() + " between unit's fireteam " + entity.FireteamId + " and unit-data's fireteam " + data.FireteamId );
            }
            debugCode = 1000;
            this.ShouldPrioritizeUndefendedPlanets = shouldPrioritizeUndefendedPlanets;
            this.TeamStrength = teamStrength;
            this.LurkingStrength = lurkingStrength;
            if ( Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam ) && tracingBuffer != null )
                tracingBuffer.Add( "Fireteam " + BaseInfo.FireTeamID + " counter profile: strength " + teamStrength
                    + " | AntiMass L/M/H " + TeamAntiMass[0] + "/" + TeamAntiMass[1] + "/" + TeamAntiMass[2]
                    + " | AntiArmor L/M/H " + TeamAntiArmor[0] + "/" + TeamAntiArmor[1] + "/" + TeamAntiArmor[2]
                    + " | AntiAlbedo D/M/B " + TeamAntiAlbedo[0] + "/" + TeamAntiAlbedo[1] + "/" + TeamAntiAlbedo[2]
                    + " | AntiEnergy L/M/H " + TeamAntiEnergy[0] + "/" + TeamAntiEnergy[1] + "/" + TeamAntiEnergy[2]
                    + " | AntiShield " + TeamAntiShield
                    + " | AntiZombify " + TeamAntiZombify + "\n" );
            if ( totalSpeed > 0 && shipsCounted > 0 )
            {
                int speedFinal = totalSpeed / shipsCounted;
                if ( speedFinal > highestSpeed ) //this would be some sort of overflow
                    speedFinal = highestSpeed;
                if ( speedFinal < 550 )
                    speedFinal = 550; //even slow units should move intimidatingly fast
                BaseInfo.PreferredSpeed = speedFinal; //this field is serialized, so it's fine to set it!
            }
            else
                BaseInfo.PreferredSpeed = 0;
            debugCode = 1200;
            IdentifyCurrentPlanet();
            if ( tracing && tracingBuffer != null)
                tracingBuffer.Add( "\n" );
            } catch( Exception e )
          {
              ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in UpdateNonSerializedFields_LRP debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
          }
        }
        #endregion

        #region SetWaitingAppropriately
        public void SetWaitingAppropriately( Faction Fac, ArcenLongTermIntermittentPlanningContextBase Context )
        {
            if ( Context == null )
                return; //client

            //Update this fireteam's units to have an appropriate "waiting" status.
            //If we have a Target planet then we set all our units to wait against it. If we don't have a target then we make sure all our units aren't waiting against anything
            //If a unit is "waiting" against a planet then other factions can include them in their strength calculations.
            GameCommand waitCommand = null;
            Int16 planetIdx = -1;
            for ( int i = 0; i < this.ShipsInFireteam.Count; i++ )
            {
                GameEntity_Squad ship = ShipsInFireteam[i].GetSquad();
                if ( ship == null )
                    continue;
                if ( ship.WaitingAgainstPlanetIndex != planetIdx )
                {
                    if ( waitCommand == null )
                        waitCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWaiting], GameCommandSource.AnythingElse );
                    waitCommand.RelatedEntityIDs.Add( ship.PrimaryKeyID );
                }
            }
            if ( waitCommand != null )
            {
                if ( BaseInfo.status == FireteamStatus.ReadyToAttack )
                    waitCommand.RelatedIntegers.Add( BaseInfo.TargetPlanet == null ? -1 : BaseInfo.TargetPlanet.Index ); //if we are ready to attack
                waitCommand.RelatedIntegers.Add( planetIdx );
                World_AIW2.Instance.QueueGameCommand( Fac, waitCommand, false );
            }
        }
        #endregion

        #region IsLurkPlanetSafe
        public bool IsLurkPlanetSafe( Faction faction )
        {
            bool debug = false;
            bool includeAlliedStrength = true;
            int unused = 0;
            int danger = Fireteam.GetPlanetDefensiveStrength( BaseInfo.LurkPlanet, faction, includeAlliedStrength, ref unused, FInt.Zero, FInt.Zero );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Fireteam " + BaseInfo.FireTeamID + " danger of lurking on " + BaseInfo.LurkPlanet.Name + " is " + danger + " and team strength is " + this.TeamStrength, Verbosity.DoNotShow );
            if ( danger > this.TeamStrength / 2 )
                return false;
            return true;
        }
        #endregion

        #region IsCurrentPlanetSafe
        public bool IsCurrentPlanetSafe( Faction faction )
        {
            bool debug = false;
            bool includeAlliedStrength = true;
            int unused = 0;
            int danger = Fireteam.GetPlanetDefensiveStrength( this.CurrentPlanet, faction, includeAlliedStrength, ref unused, FInt.Zero, FInt.Zero );
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Fireteam " + BaseInfo.FireTeamID + " danger of current planet " + this.CurrentPlanet.Name + " is " + danger + " and team strength is " + this.TeamStrength, Verbosity.DoNotShow );
            if ( danger > this.TeamStrength / 2 )
                return false;
            return true;
        }
        #endregion

        #region CanISafelyGetToLurkPlanet
        public bool CanISafelyGetToLurkPlanet( Faction faction, ArcenSimContextAnyStatus Context, PerFactionPathCache PathCacheData )
        {
            //bool debug = false;
            //bool includeAlliedStrength = true;
            //int unused = 0;
            Int16 hops = 0;
            int danger = Fireteam.GetDangerOfPath( faction, Context, PathCacheData, BaseInfo.LurkPlanet, this.CurrentPlanet, true, out hops );
            if ( danger > this.TeamStrength * 5 ) //as long as they only outnumber us 5:1, let's go!
                return false;
            return true;
        }
        #endregion

        #region CanISafelyGetToEscortPlanet
        public bool CanISafelyGetToEscortPlanet( Faction faction, ArcenSimContextAnyStatus Context, PerFactionPathCache PathCacheData )
        {
            Int16 hops = 0;
            if ( BaseInfo.Target == null )
                return false;
            int danger = Fireteam.GetDangerOfPath( faction, Context, PathCacheData, BaseInfo.Target.Planet, this.CurrentPlanet, true, out hops );
            if ( danger > this.TeamStrength * 3 ) //can risk some danger
                return false;
            return true;
        }
        #endregion

        #region ShouldFireteamRetreatFromCurrentPlanet
        public bool ShouldFireteamRetreatFromCurrentPlanet( Faction faction )
        {
            if ( BaseInfo.SuicideMission )
                return false;
            this.BuildShipsLookup( IncludeShipsInTransit, null );
            foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in shipsByPlanet )
            {
                int defensiveMobileStrength = 0;
                bool includeAlliedStrength = true;

                bool doesPlanetHaveAlliedKing( Planet p )
                {
                    bool result = false;
                    foreach ( GameEntity_Squad e in p.Squads( EntityRollupType.KingUnitsOnly ) )
                    {
                        if ( e.GetIsFriendlyTowards_Safe( faction ) && e.FireteamId != BaseInfo.FireTeamID )
                        {
                            result = true;
                            break;
                        }
                    }

                    return result;
                }

                if ( doesPlanetHaveAlliedKing( pair.Key ) )
                    return false; //never retreat from the king
                int enemies = Fireteam.GetPlanetDefensiveStrength( pair.Key, faction, includeAlliedStrength, ref defensiveMobileStrength, Fireteam.RetreatPathRemoteShipDangerBaseDivisor, Fireteam.RetreatPathRemoteShipDivisorIncreaseRate );
                if ( enemies > 0 )
                    return true;
            }
            return false;
        }
        #endregion

        #region GetDebugString
        public void GetDebugString( ArcenCharacterBufferBase buffer )
        {
            //For debug logging/tracing. PLEASE DUPLICATE ANY CHANGES IN THE GetStatusForDisplay() FUNCTION
            buffer.Add( "Fireteam " ).Add( BaseInfo.FireTeamID ).Add( " has " ).Add( ShipsInFireteam.Count ).Add( " units with strength " ).Add( this.TeamStrength ).Add( "." );
            buffer.Add( " Status " ).Add( Extensions.ToString(BaseInfo.status) );
            if ( BaseInfo.status == FireteamStatus.Escorting )
            {
                if ( BaseInfo.Target != null )
                    buffer.Add( "Escorting " ).Add( BaseInfo.Target.TypeData.GetDisplayName() ).Add( " on " ).Add( BaseInfo.Target.GetPlanetName_Safe() ).Add( ". " );
                else
                    buffer.Add( "Escorting nothing at the moment. " );
            }
            else if ( BaseInfo.TargetPlanet != null )
                buffer.Add( "\t target planet: " ).Add( BaseInfo.TargetPlanet.Name ).Add( ". " );
            else
                buffer.Add( "\t No target planet" );
            if ( BaseInfo.LurkPlanet != null )
            {
                buffer.Add( " lurk planet: " ).Add( BaseInfo.LurkPlanet.Name ).Add( ". We have been lurking for " ).Add( (World_AIW2.Instance.GameSecond - BaseInfo.LurkStartTime) ).Add( " seconds. " );
            }
            else
                buffer.Add( " No lurk planet. " );
            BuildShipsLookup( IncludeShipsInTransit, null );
            if ( BaseInfo.DefenseMode )
            {
                buffer.Add( "Defensive fleet. " );
                if ( BaseInfo.StepsUntilBecomesOffensive > 0 )
                    buffer.Add( " Steps until becomes offensive: " ).Add( BaseInfo.StepsUntilBecomesOffensive ).Add( ". " );
            }

            if ( BaseInfo.CloakedOnly )
                buffer.Add( "Cloaked Fleet. " );
            if ( BaseInfo.SuicideMission )
                buffer.Add( "Suicide Mission. " );
            if ( BaseInfo.UpgradedOnly )
                buffer.Add( "UpgradedOnly. " );


            buffer.Add( "Ships: " );
            bool isFirst = true;
            foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in shipsByPlanet )
            {
                if ( isFirst )
                    isFirst = false;
                else
                    buffer.Add( ", " );
                buffer.Add( pair.Key.Name ).Add( ": " ).Add( pair.Value.Count );
            }
        }
        #endregion

        //TEACHING_MOMENT: It turns out that these "display strings" are still only for the debugging info on the host.
        //They rely on information that is only generated by the LRP, and which is not serialized across the network.
        //They ALSO, happily, are not part of any general tooltips or anything like that, aside from items that are debug-only.

        #region GetStringForDisplay
        public void GetStringForDisplay( ArcenCharacterBufferBase buffer )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
            {
                buffer.Add( "Detailed fireteam data is only available on the host." );
                return;
            }

            //for logging/debugging, including in the ResourceBar with Debug Tooltips.
            if ( BaseInfo.DefenseMode )
            {
                buffer.Add( "Defensive ", "44dd44" );
            }
            if ( BaseInfo.SuicideMission )
                buffer.Add( "Suicidal ", "dddd44" );

            buffer.Add( "Fireteam " ).Add( BaseInfo.FireTeamID, "a1a1ff" ).Add( " has " ).Add( ShipsInFireteam.Count, "a1ffa1" ).Add( "(" ).Add( GetNumberOfShipsThatCouldUpgrade(), "1111ff" ).Add( ") units with strength " ).Add( this.TeamStrength, "ffa1a1" ).Add( " (lurking strength " ).Add( this.LurkingStrength, "aa33ff" ).Add( "). " );
            buffer.Add( " Status " );
            BaseInfo.GetStatusForDisplay( buffer );
            if ( BaseInfo.status == FireteamStatus.Escorting )
            {
                if ( BaseInfo.Target != null )
                    buffer.Add( "Escorting " ).Add( BaseInfo.Target.TypeData.GetDisplayName(), "a1ffa1" ).Add( " on " ).Add( BaseInfo.Target.GetPlanetName_Safe(), "a1ffa1" ).Add( ". " );
                else
                    buffer.Add( "Escorting nothing at the moment. " );
            }
            else if ( BaseInfo.TargetPlanet != null )
                buffer.Add( " target planet: " ).Add( BaseInfo.TargetPlanet.Name, "ff3443" ).Add( ". " );
            else
                buffer.Add( " No target planet. " );
            if ( BaseInfo.LurkPlanet != null )
                buffer.Add( " lurk planet: " ).Add( BaseInfo.LurkPlanet.Name, "a1a1ff" ).Add( ". " );
            else if ( BaseInfo.status != FireteamStatus.Escorting )
                buffer.Add( " No lurk planet. " );
            if ( BaseInfo.CloakedOnly )
                buffer.Add( "Cloaked fleet. ", "33ee33" );
            if ( BaseInfo.UpgradedOnly )
                buffer.Add( "UpgradedOnly. ", "44dd44" );

            BuildShipsLookup( IncludeShipsInTransit, null );

            buffer.Add( "Ships: " );
            bool isFirst = true;
            foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in shipsByPlanet )
            {
                if ( isFirst )
                    isFirst = false;
                else
                    buffer.Add( ", " );
                buffer.Add( pair.Key.Name, "a1ffa1" ).Add( ": " ).Add( pair.Value.Count, "ffa1a1" );
            }
        }
        #endregion
    }
}
