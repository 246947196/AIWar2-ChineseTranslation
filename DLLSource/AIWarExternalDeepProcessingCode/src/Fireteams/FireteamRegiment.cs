using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class FireteamRegiment : ConcurrentPoolable<FireteamRegiment>, IProtectedListable
    {
        //When deciding whether to attack, we get all the Fireteams aimed at a given planet,
        //in order to decide whether to attack
        public readonly ArcenLessLinkedList<Fireteam> teams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "FireteamRegiment-teams" );
        public readonly ArcenLessLinkedList<Fireteam> stagingteams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "FireteamRegiment-stagingteams" );
        public bool hasDefensiveFleets;
        public int myTotalStrength; //this includes ships not on the planet yet
        public int availableStrength; //strength on this planet or on an immediately adjacent planet
        public int stagingStrength; //this is all fireteams whose units are staging toward this planet
        public int strengthOnTarget;
        public int totalEnemyStrength;
        public int totalEnemyStrengthLiteral;
        public int nonGuardEnemyStrength;
        public int guardEnemyStrength;
        public int netEnemyStrength;
        public int dangerOfPathOnly;
        public int mobileEnemyStrength;
        public bool targetHasForcefield;
        public bool declareVictoryEarly; // this is for unkillable factions like the Dyson Sphere
        public int HighestMarkUnit;

        //Counter-awareness (E): the regiment's combined anti-bin strength (sum of member teams' profiles) and the
        //resulting offensive matchup multiplier vs the target's defender composition. CapabilityFactor 1.0 = neutral,
        //>1 = our comp counters theirs (so we can commit sooner), <1 = mismatch. Clamped to [Min, Max].
        public readonly int[] RegimentAntiMass = new int[CompositionBins.MassBinCount];
        public readonly int[] RegimentAntiArmor = new int[CompositionBins.ArmorBinCount];
        public readonly int[] RegimentAntiAlbedo = new int[CompositionBins.AlbedoBinCount];
        public readonly int[] RegimentAntiEnergy = new int[CompositionBins.EnergyBinCount];
        public int RegimentAntiShield;
        public int RegimentAntiZombify;
        public FInt CapabilityFactor;
        private static readonly FInt CapabilityFactorMin = FInt.FromParts( 0, 600 );
        private static readonly FInt CapabilityFactorMax = FInt.FromParts( 1, 600 );

        public Planet TargetPlanet;
        public GameEntity_Squad TargetEntity;
        public bool EnemyStrengthAllInReinforcementPoints;
        public FInt MyStrengthMultiplierForStrengthCalculation; //determined from fireteams
        public FInt EnemyStrengthMultiplierForStrengthCalculation; //determined from fireteams
        public int AttackingSpeed; //when a fireteam regiment attacks, all its members go at the same speed
        public bool ExtraCautiousAgainstPlayers; //just copied from the Fireteams
        public bool HasSuicidalFireteams;

        private readonly Dictionary<Planet, int> workingPlanetsForRegiment = Dictionary<Planet, int>.Create_WillNeverBeGCed( 100, "FireteamRegiment-workingPlanetsForRegiment" );
        private bool isGettingMostCommonPlanetForRegiment = false;

        //Anything added above must be reset in SetToDefaults!

        public void SetToDefaults()
        {
            teams.Clear();
            stagingteams.Clear();

            this.hasDefensiveFleets = false;
            this.myTotalStrength = 0;
            this.availableStrength = 0;
            this.stagingStrength = 0;
            this.strengthOnTarget = 0;
            this.totalEnemyStrength = 0;
            this.totalEnemyStrengthLiteral = 0;
            this.declareVictoryEarly = false;
            this.nonGuardEnemyStrength = 0;
            this.guardEnemyStrength = 0;
            this.netEnemyStrength = 0;
            this.dangerOfPathOnly = 0;
            this.mobileEnemyStrength = 0;
            this.targetHasForcefield = false;
            this.HighestMarkUnit = 0;
            for ( int b = 0; b < RegimentAntiMass.Length; b++ )
                RegimentAntiMass[b] = 0;
            for ( int b = 0; b < RegimentAntiArmor.Length; b++ )
                RegimentAntiArmor[b] = 0;
            for ( int b = 0; b < RegimentAntiAlbedo.Length; b++ )
                RegimentAntiAlbedo[b] = 0;
            for ( int b = 0; b < RegimentAntiEnergy.Length; b++ )
                RegimentAntiEnergy[b] = 0;
            this.RegimentAntiShield = 0;
            this.RegimentAntiZombify = 0;
            this.CapabilityFactor = FInt.One;

            this.TargetPlanet = null;
            this.TargetEntity = null;
            this.EnemyStrengthAllInReinforcementPoints = false;
            this.MyStrengthMultiplierForStrengthCalculation = FInt.One;
            this.EnemyStrengthMultiplierForStrengthCalculation = FInt.One;
            this.AttackingSpeed = 0;
            this.ExtraCautiousAgainstPlayers = false;
            this.HasSuicidalFireteams = false;

            workingPlanetsForRegiment.Clear();
            isGettingMostCommonPlanetForRegiment = false;
        }

        public void Add( Fireteam team )
        {
            if ( this.MyStrengthMultiplierForStrengthCalculation != team.MyStrengthMultiplierForStrengthCalculation )
                this.MyStrengthMultiplierForStrengthCalculation = team.MyStrengthMultiplierForStrengthCalculation;
            if ( this.EnemyStrengthMultiplierForStrengthCalculation != team.EnemyStrengthMultiplierForStrengthCalculation )
                this.EnemyStrengthMultiplierForStrengthCalculation = team.EnemyStrengthMultiplierForStrengthCalculation;
            if ( team.status == FireteamStatus.Staging && !stagingteams.Contains( team ) )
                stagingteams.AddIfNotAlreadyIn( team );
            else if ( team.status == FireteamStatus.ReadyToAttack || team.status == FireteamStatus.Attacking &&
                      !teams.Contains( team ) )
                teams.AddIfNotAlreadyIn( team );
            else if ( team.status != FireteamStatus.Staging && team.status != FireteamStatus.ReadyToAttack && team.status != FireteamStatus.Attacking )
                throw new Exception( "Tried to add " + team.ToString() + " to a regiment, which doesn't make sense" );

        }

        public void calculateAttackingSpeed( Faction faction )
        {
            //This must be set before we call Attack, so we attack with the right speed
            int totalSpeedForFireteams = 0;
            int teamsCounted = 0;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( teams ) )
            {
                if ( team.PreferredSpeed > 0 )
                {
                    totalSpeedForFireteams += team.PreferredSpeed;
                    teamsCounted++;
                }
                else
                    team.PreferredSpeed = 0;
            }

            if ( totalSpeedForFireteams <= 0 ) //we are not attacking
                return;

            //"A bit faster than the average unit"
            if ( teamsCounted > 0 )
            {
                this.AttackingSpeed = totalSpeedForFireteams / teamsCounted;
                this.AttackingSpeed += this.AttackingSpeed / 10;
                if ( this.AttackingSpeed < 0 )
                    this.AttackingSpeed = 0;
            }
            else
                this.AttackingSpeed = 0;
        }
        public bool doesTargetHaveForcefield()
        {
            bool hasFF = false;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( teams ) )
            {
                if ( team.DeepInfo.TargetProtectedByForcefield )
                {
                    hasFF = true;
                    break;
                }
            }
            return hasFF;
        }

        public void calculateAvailableStrength( Faction faction, FInt strengthMultiplier )
        {
            //make sure to include additional allied forces on the planet
            int debugCode = 0;
            try
            {
                debugCode = 100;
                this.availableStrength = 0;
                this.strengthOnTarget = 0;
                this.stagingStrength = 0;
                this.myTotalStrength = 0;
                for ( int b = 0; b < RegimentAntiMass.Length; b++ )
                    RegimentAntiMass[b] = 0;
                for ( int b = 0; b < RegimentAntiArmor.Length; b++ )
                    RegimentAntiArmor[b] = 0;
                for ( int b = 0; b < RegimentAntiAlbedo.Length; b++ )
                    RegimentAntiAlbedo[b] = 0;
                for ( int b = 0; b < RegimentAntiEnergy.Length; b++ )
                    RegimentAntiEnergy[b] = 0;
                this.RegimentAntiShield = 0;
                this.RegimentAntiZombify = 0;
                debugCode = 200;
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( teams ) )
                {
                    debugCode = 300;
                    myTotalStrength += team.DeepInfo.TeamStrength;
                    for ( int b = 0; b < RegimentAntiMass.Length; b++ )
                        RegimentAntiMass[b] += team.DeepInfo.TeamAntiMass[b];
                    for ( int b = 0; b < RegimentAntiArmor.Length; b++ )
                        RegimentAntiArmor[b] += team.DeepInfo.TeamAntiArmor[b];
                    for ( int b = 0; b < RegimentAntiAlbedo.Length; b++ )
                        RegimentAntiAlbedo[b] += team.DeepInfo.TeamAntiAlbedo[b];
                    for ( int b = 0; b < RegimentAntiEnergy.Length; b++ )
                        RegimentAntiEnergy[b] += team.DeepInfo.TeamAntiEnergy[b];
                    RegimentAntiShield += team.DeepInfo.TeamAntiShield;
                    RegimentAntiZombify += team.DeepInfo.TeamAntiZombify;
                    if ( team.status == FireteamStatus.ReadyToAttack ||
                         team.status == FireteamStatus.Staging )
                    {
                        availableStrength += team.DeepInfo.LurkingStrength;
                        stagingStrength += team.DeepInfo.TeamStrength - team.DeepInfo.LurkingStrength;
                    }
                    else
                        availableStrength += team.DeepInfo.TeamStrength;
                    if ( team.DeepInfo.CurrentPlanet == this.TargetPlanet )
                        strengthOnTarget += team.DeepInfo.TeamStrength;
                }
                debugCode = 400;
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( stagingteams ) )
                {
                    debugCode = 500;
                    stagingStrength += team.DeepInfo.TeamStrength;
                }
                debugCode = 600;
                availableStrength = (strengthMultiplier * availableStrength).IntValue;
                int adjustment = 0;
                int friendlyWaiters = 0;
                if ( this.TargetPlanet != null )
                {
                    debugCode = 700;
                    //adjustment is for my or allied ships on the planet
                    //friendlyWaiters is for allied factions waiting to attack the planet
                    var factionData = this.TargetPlanet.GetStanceDataForFaction( faction );
                    if ( factionData != null )
                    {
                        adjustment = factionData[FactionStance.Self].TotalStrength + factionData[FactionStance.Friendly].TotalStrength + factionData[FactionStance.Self].IncomingStrength + factionData[FactionStance.Friendly].IncomingStrength;
                        friendlyWaiters = factionData[FactionStance.Self].WaitingStrength + factionData[FactionStance.Friendly].WaitingStrength;
                    }
                }
                debugCode = 800;
                availableStrength += adjustment + friendlyWaiters;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in Fireteam::calculateAvailableStrength debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            //            ArcenDebugging.ArcenDebugLogSingleLine("\tAvailable strength 3: " + availableStrength, Verbosity.DoNotShow );
        }

        public void calculateEnemyStrength( Faction faction, Planet planet, ArcenLongTermIntermittentPlanningContextBase ContextOrNull, PerFactionPathCache PathCacheData, bool tracing, ArcenCharacterBuffer tracingBuffer )
        {
            int debugCode = 0;
            try
            {
                debugCode = 100;
                //TODO: move these multipliers into the Fireteam itself
                FInt MyStrengthMultiplierForStrengthCalculationLowMark = MyStrengthMultiplierForStrengthCalculation - FInt.FromParts( 0, 200 );
                //int defensiveStrength;
                FInt MultiplierForNoForcefieldsOnTarget = FInt.FromParts( 0, 750 );
                bool includeAlliedStrength = true;
                FInt AttackRemoteShipDangerBaseDivisor = Fireteam.FullBattleRemoteShipDangerBaseDivisor;
                FInt AttackRemoteShipDivisorIncreaseRate = Fireteam.FullBattleRemoteShipDivisorIncreaseRate;
                EnemyStrengthAllInReinforcementPoints = false;
                debugCode = 200;
                if ( planet.HasPlanetBeenDestroyed )
                    return; //this can happen very briefly after a planet is destroyed, if a fireteam was targeting that planet.
                var pFaction = planet.GetStanceDataForFaction( faction );
                if ( pFaction == null )
                {

                    ArcenDebugging.ArcenDebugLogSingleLine( "failed to find long range planning data on " + planet.Name + " for " + faction.GetDisplayName(), Verbosity.DoNotShow );
                    return; //without this data we can't compute enemy strength, and dereferencing pFaction below would NRE
                }

                this.totalEnemyStrengthLiteral = pFaction[FactionStance.Hostile].TotalStrength;
                Faction planetControllingFaction = planet.GetControllingOrInfluencingFaction();
                if ( planetControllingFaction.GetIsHostileTowards( faction ) &&
                     planetControllingFaction.SpecialFactionData.NotKillableByNormalMeans )
                    this.declareVictoryEarly = true; //if the planet is owned by an unkillable faction, just declare victory early
                if ( TargetEntity != null &&
                     TargetEntity.PlanetFaction.Faction.SpecialFactionData.NotKillableByNormalMeans )
                    this.declareVictoryEarly = true; //if the target is an unkillable faction, just declare victory early
                if ( planet.DoesPlanetHaveUnkillableEnemy( faction ))
                    this.declareVictoryEarly = true;
                debugCode = 250;
                if ( this.teams.GetItemCount() > 0 && !this.teams.GetFirst().Contained.DeepInfo.TargetProtectedByForcefield )
                {
                    debugCode = 300;
                    //this fireteam has a specific Target that it wants to kill (like a GCA or economic command station),
                    //and the target isn't protected by a forcefield, so be extra aggressive about sniping it
                    AttackRemoteShipDangerBaseDivisor = Fireteam.TargetSnipeRemoteShipDangerBaseDivisor;
                    AttackRemoteShipDivisorIncreaseRate = Fireteam.TargetSnipeRemoteShipDivisorIncreaseRate;
                }
                else
                {
                    debugCode = 400;
                    //we are either going after a forcefielded target or just trying to take the planet outright
                    if ( planet.GetControllingFactionType() == FactionType.Player &&
                         planet.GetControllingFaction().GetIsHostileTowards( faction ) &&
                         this.ExtraCautiousAgainstPlayers )
                    {
                        //if we're attacking a player, be extra cautious because of how quickly transports can move player ships
                        //around on defense
                        AttackRemoteShipDangerBaseDivisor = Fireteam.FullBattleRemoteShipDangerBaseDivisorPlayerOnly;
                        AttackRemoteShipDivisorIncreaseRate = Fireteam.FullBattleRemoteShipDivisorIncreaseRatePlayerOnly;
                    }
                }
                debugCode = 500;

                if ( planet.GetControllingFaction().GetIsHostileTowards( faction ) )
                {
                    debugCode = 600;
                    //only attack the player or AI when we are a good amount stronger.
                    if ( planet.GetControllingFactionType() == FactionType.Player )
                        EnemyStrengthMultiplierForStrengthCalculation += FInt.FromParts( 0, 500 );
                    else if ( planet.GetControllingFactionType() == FactionType.AI )
                        EnemyStrengthMultiplierForStrengthCalculation += FInt.FromParts( 0, 050 );
                }
                debugCode = 700;
                Planet mostCommonPlanetForRegiment = getMostCommonPlanetForRegiment();
                bool includeDestination = true;
                short ignored = 1;
                if ( mostCommonPlanetForRegiment != null && TargetPlanet != null && ContextOrNull != null )
                    this.dangerOfPathOnly = Fireteam.GetDangerOfPath( faction, ContextOrNull, PathCacheData, mostCommonPlanetForRegiment, TargetPlanet, !includeDestination, out ignored );
                else
                    this.dangerOfPathOnly = -1;
                debugCode = 800;
                this.netEnemyStrength = Fireteam.GetPlanetDefensiveStrength( planet, faction, includeAlliedStrength, ref this.mobileEnemyStrength, AttackRemoteShipDangerBaseDivisor,
                                                                             AttackRemoteShipDivisorIncreaseRate );

                this.totalEnemyStrength = Fireteam.GetPlanetDefensiveStrength( planet, faction, !includeAlliedStrength, ref this.mobileEnemyStrength, AttackRemoteShipDangerBaseDivisor,
                                                                               AttackRemoteShipDivisorIncreaseRate );
                debugCode = 900;
                if ( planet.GetDataByStanceForFaction( faction, FactionStance.Hostile ).StrengthInReinforcementPoints > 0 && //if anything in reinforcement points
                     planet.GetDataByStanceForFaction( faction, FactionStance.Hostile ).StrengthInReinforcementPoints >= //and there's nothing else
                     planet.GetDataByStanceForFaction( faction, FactionStance.Hostile ).TotalStrength - 800 )
                {
                    debugCode = 1000;
                    EnemyStrengthAllInReinforcementPoints = true;
                }
                debugCode = 1100;
                this.guardEnemyStrength = planet.GetDataByStanceForFaction( faction, FactionStance.Hostile ).GuardStrength;
                this.totalEnemyStrength = (this.totalEnemyStrength * EnemyStrengthMultiplierForStrengthCalculation).IntValue;
                if ( !this.doesTargetHaveForcefield() )
                    this.totalEnemyStrength = (this.totalEnemyStrength * MultiplierForNoForcefieldsOnTarget).IntValue;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in Fireteam::calculateEnemyStrength debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        public void CalculateCapabilityFactor( Faction faction, Planet planet, bool tracing, ArcenCharacterBuffer tracingBuffer )
        {
            //Compare our combined anti-bin strength against the target's defender composition to get an offensive
            //matchup multiplier: the expected outgoing damage multiplier averaged over their mass/armor mix. 1.0 is
            //a neutral matchup; >1 means our weapons counter their defenders (so we can afford to strike sooner).
            this.CapabilityFactor = FInt.One;
            try
            {
                if ( this.myTotalStrength <= 0 || planet == null || planet.HasPlanetBeenDestroyed )
                    return;
                var pFaction = planet.GetStanceDataForFaction( faction );
                if ( pFaction == null )
                    return;
                StrengthData_PlanetFaction_Stance hostile = pFaction[FactionStance.Hostile];
                FInt massFactor = ComputeAxisMatchupFactor( RegimentAntiMass, hostile.MassStrength_Light, hostile.MassStrength_Medium, hostile.MassStrength_Heavy );
                FInt armorFactor = ComputeAxisMatchupFactor( RegimentAntiArmor, hostile.ArmorStrength_Low, hostile.ArmorStrength_Mid, hostile.ArmorStrength_High );
                FInt albedoFactor = ComputeAxisMatchupFactor( RegimentAntiAlbedo, hostile.AlbedoStrength_Dark, hostile.AlbedoStrength_Mid, hostile.AlbedoStrength_Bright );
                FInt energyFactor = ComputeAxisMatchupFactor( RegimentAntiEnergy, hostile.EnergyStrength_Low, hostile.EnergyStrength_Mid, hostile.EnergyStrength_High );
                FInt shieldBypassFactor = ComputeShieldBypassFactor( hostile.TurretStrength, hostile.TotalStrength );
                FInt zombifyFactor = ComputeZombifyFactor( hostile.MassStrength_Light, hostile.TotalStrength );
                //Each axis factor is >= 1 (our profiles are bonus-only). Combine additively so neutral axes (factor 1.0)
                //don't dilute a real counter on another axis, and fold mass+energy into one "size" signal since they're
                //correlated (big ships have both) -- otherwise an anti-big regiment would be double-credited for size.
                FInt sizeFactor = (massFactor + energyFactor) / FInt.FromParts( 2, 0 );
                FInt combined = FInt.One + (sizeFactor - FInt.One) + (armorFactor - FInt.One) + (albedoFactor - FInt.One) + (shieldBypassFactor - FInt.One) + (zombifyFactor - FInt.One);
                if ( combined < CapabilityFactorMin )
                    combined = CapabilityFactorMin;
                if ( combined > CapabilityFactorMax )
                    combined = CapabilityFactorMax;
                this.CapabilityFactor = combined;
                if ( tracing && tracingBuffer != null )
                    tracingBuffer.Add( "\tcapability factor for " + (TargetPlanet != null ? TargetPlanet.Name : "?") + ": " + this.CapabilityFactor + " (size " + sizeFactor + " [mass " + massFactor + ", energy " + energyFactor + "], armor " + armorFactor + ", albedo " + albedoFactor + ", shieldBypass " + shieldBypassFactor + ", zombify " + zombifyFactor + ")\n" );
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in CalculateCapabilityFactor " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        private FInt ComputeAxisMatchupFactor( int[] anti, int d0, int d1, int d2 )
        {
            //factor = Σ_bin (defenderFractionInBin) × (ourAntiStrengthForBin / ourTotalStrength); == 1.0 when we have
            //no relevant bonuses (anti == total in every bin) or when the target has no defenders on this axis.
            int total = d0 + d1 + d2;
            if ( total <= 0 || this.myTotalStrength <= 0 )
                return FInt.One;
            FInt factor = FInt.Zero;
            factor += ((FInt)d0 / (FInt)total) * ((FInt)anti[0] / (FInt)this.myTotalStrength);
            factor += ((FInt)d1 / (FInt)total) * ((FInt)anti[1] / (FInt)this.myTotalStrength);
            factor += ((FInt)d2 / (FInt)total) * ((FInt)anti[2] / (FInt)this.myTotalStrength);
            return factor;
        }

        private FInt ComputeShieldBypassFactor( int turretStrength, int totalDefenderStrength )
        {
            //factor = 1.0 + (our avg bypass fraction) × (turret fraction of defenders)
            //== 1.0 when we have no shield-bypassing weapons, or the planet has no turrets.
            if ( turretStrength <= 0 || totalDefenderStrength <= 0 || this.myTotalStrength <= 0 || this.RegimentAntiShield <= 0 )
                return FInt.One;
            FInt avgBypassFraction = (FInt)RegimentAntiShield / (FInt)this.myTotalStrength;
            FInt turretFraction = (FInt)turretStrength / (FInt)totalDefenderStrength;
            return FInt.One + avgBypassFraction * turretFraction;
        }

        private FInt ComputeZombifyFactor( int lightMassStrength, int totalDefenderStrength )
        {
            //factor = 1.0 + (fraction of regiment with zombify weapons) × (light-mass fraction of defenders)
            //== 1.0 when we have no zombify weapons, or the planet has no light-mass defenders.
            if ( lightMassStrength <= 0 || totalDefenderStrength <= 0 || this.myTotalStrength <= 0 || this.RegimentAntiZombify <= 0 )
                return FInt.One;
            FInt avgZombifyFraction = (FInt)RegimentAntiZombify / (FInt)this.myTotalStrength;
            FInt lightMassFraction = (FInt)lightMassStrength / (FInt)totalDefenderStrength;
            return FInt.One + avgZombifyFraction * lightMassFraction;
        }

        private Planet getMostCommonPlanetForRegiment()
        {
            if ( this.isGettingMostCommonPlanetForRegiment )
                ArcenDebugging.ArcenDebugLog( "BUG: called getMostCommonPlanetForRegiment() again for a fireteam regiment before the last call finished.  Will break both calls.", Verbosity.ShowAsError );
            this.isGettingMostCommonPlanetForRegiment = true;
            Planet output = null;
            try
            {
                workingPlanetsForRegiment.Clear();
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( teams ) )
                {
                    workingPlanetsForRegiment[team.DeepInfo.CurrentPlanet]++;
                }
                int mostCommonPlanetCount = 0;

                foreach ( KeyValuePair<Planet, int> pair in workingPlanetsForRegiment )
                {
                    if ( pair.Value > mostCommonPlanetCount )
                    {
                        mostCommonPlanetCount = pair.Value;
                        output = pair.Key;
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in getMostCommonPlanetForRegiment: " + e, Verbosity.ShowAsError );
            }
            finally
            {
                this.isGettingMostCommonPlanetForRegiment = false;
            }
            return output;
        }

        public int GetTotalEnemyStrengthForRegiment( Faction faction )
        {
            return this.totalEnemyStrengthLiteral;
        }

        public bool HasWonBattle( Faction faction )
        {
            if ( this.TargetEntity != null &&
                 !this.TargetEntity.PlanetFaction.Faction.SpecialFactionData.NotKillableByNormalMeans )
            {
                //we wanted to kill a specific unit and its not dead.
                //If we can't really kill the faction that this Entity (lets say the Dyson Sphere)
                //then we will ignore this check and just rely on the other checks
                return false; 
            }
            if ( this.totalEnemyStrengthLiteral <= 0 && this.strengthOnTarget > 0 ) //no enemies
            {
                return true;
            }
            if ( this.TargetPlanet != null && this.TargetPlanet.HasPlanetBeenDestroyed )
                return true; //if the target planet is dead, no point in looking further at this

            if ( this.totalEnemyStrength <= 300 && this.strengthOnTarget > 2000 && this.teams.GetFirst().Contained.Target == null )
            {
                return true; //very weak enemies, overwhelming force and the actual target is dead
            }
            if ( this.declareVictoryEarly )
            {
                //We need to declare victory early against some factions like the Dyson Sphere,
                //otherwise our ships will get stuck
                if ( this.totalEnemyStrengthLiteral <= 20 * 1000 && this.strengthOnTarget > this.totalEnemyStrengthLiteral )
                {
                    return true;
                }
            }
            // if ( this.TargetPlanet != null )
            //     ArcenDebugging.ArcenDebugLogSingleLine("\tHasWonBattle for ships attacking " + this.TargetPlanet.Name + ". All in reinforcementpoints " + this.EnemyStrengthAllInReinforcementPoints + " enemy strength " + this.totalEnemyStrength + " strength on target " + this.strengthOnTarget, Verbosity.DoNotShow );

            if ( this.EnemyStrengthAllInReinforcementPoints ) //if the AIs forces are all hiding in reinforcement points
            {
                if ( (this.totalEnemyStrengthLiteral <= this.strengthOnTarget / 3) ) //and we outnumber them heavily on this planet, then the enemy ships are too afraid to come out
                {
                    return true;
                }
            }

            if ( this.TargetPlanet != null )
            {
                PlanetFaction pFaction = TargetPlanet.GetPlanetFactionForFaction( faction );
                Faction influencerOrNull = TargetPlanet.GetFactionWithSpecialInfluenceHere();
                if ( influencerOrNull != null && influencerOrNull.GetIsFriendlyTowards( faction ) && this.totalEnemyStrength < this.strengthOnTarget / 5 )
                {
                    return true;
                }
            }
            return false;
        }


        public string ToDebugString()
        {
            //in case we need it
            ArcenCharacterBuffer output = ArcenCharacterBuffer.GetFromPoolOrCreate( "FireteamRegiment-ToDebugString" );
            output.Add( "There are " ).Add( this.teams.GetItemCount(), "a1ffa1" ).Add( " teams (" ).Add( this.availableStrength, "a1ffa1" ).Add( ") waiting to attack " ).Add( TargetPlanet.Name, "ffa1a1" ).Add( " (" ).Add( this.totalEnemyStrength, "a1ffa1" ).Add( ")" ).Add( ". There are " ).Add( this.stagingteams.GetItemCount() ).Add( " staging fireteams with strength " ).Add( this.stagingStrength, "a1ffa1" );
            return output.ToStringAndReturnToPool();
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private FireteamRegiment()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "FireteamRegiments" );
            RefTracker.IncrementObjectCount();

            this.SetToDefaults();
        }

        public static FireteamRegiment GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        private static ConcurrentPool<FireteamRegiment> Pool = new ConcurrentPool<FireteamRegiment>( "FireteamRegiments", 30000,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new FireteamRegiment(); } );

        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        public override void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            this.SetToDefaults();
        }

        public void DoBeforeRemoveOrClear()
        {
            this.SetToDefaults();
            this.ReturnToPool();
        }
        #endregion
    }
}
