using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    //A ZA has several states
    //First, there's the 'initial expansion phase', where it takes a certain number of planets for its Territory
    //Once it has taken its full Territory, it will just chill out. When Chilling Out, the ZA just sits in its territory and doesn't do anything unless attacked. If attacked it will kill all enemies in its Territory, then resume chilling out
    //Every so often, the ZA will enter Expansion Mode, where it will spawn Pioneers. Each Pioneer allows it to capture a planet by building a spawner
    //These new planets are not added to the Territory; if you snipe their spawner then they'll just leave the planet, though they will defend it
    //When all the Pioneers are killed, the ZA will resume chilling out
    //In a multi-ZA game, if one ZA captures enough planets then the other ZAs will unite against it. This is called Civil War mode. ZAs leave civil war once enough spawners are killed. During the Civil War, the ZAs will focus on killing eachother

    //If the ZA is being attacked or attacking (ie if anyone attacks a ZA planet, or the ZA is in Expansion Mode or Civil War mode) then its said to be in "War Footing"

    public sealed class ZenithArchitraveFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        //Set immediately before the expansion-planet sorts so the comparisons can be non-capturing
        //static delegates.  [ThreadStatic] because faction planning runs on background threads.
        [ThreadStatic] private static Faction cb_zaDeepFaction;
        public ZenithArchitraveFactionBaseInfo BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
        }

        protected override void Cleanup() 
        {
            this.BaseInfo = null;

            //almost certainly does not matter
            WorkingRandomSpawnerList.Clear();
            PossibleExpansionPlanets.Clear();
            PreferredExpansionPlanets.Clear();
            UnassignedShips.Clear();
            UnassignedShipsByPlanet.Clear();
            Ships.Clear();
            LRPSpawners.Clear();
            LRPSpawnersWithSpace.Clear();
            LRPWarpingInSpawners.Clear();
            LRPWarpingInSpawnerPlanets.Clear();
            LRPPioneers.Clear();

            //might mater a little
            AvailableFireteams.Clear();
            TeamsAimedAtPlanet.Clear();
        }
        public readonly ProtectedValDictionary<Planet, FireteamRegiment> TeamsAimedAtPlanet = ProtectedValDictionary<Planet, FireteamRegiment>.Create_WillNeverBeGCed( 100, "ZenithArchitraveFactionDeepInfo-TeamsAimedAtPlanet" );

        protected override int MinimumSecondsBetweenLongRangePlannings => 3;

        private void UpdateWarFooting( ArcenHostOnlySimContext Context )
        {
            //figure out if we are in War Footing or not

            bool wasAtWar = this.BaseInfo.IsInWarFooting;
            this.BaseInfo.IsInWarFooting = CheckForWarFooting( Context );
            if ( wasAtWar != this.BaseInfo.IsInWarFooting  &&
                 World_AIW2.Instance.GetIsHostAnyShouldPrepareToSendNewEntitiesToClients() )
            {
                //if we've just changed our war status, sync us to MP clients to make sure they see the correct behaviour/notifications ASAP
                World_AIW2.Instance.OnServer_FactionsToFastBlastToClients.Enqueue( this.AttachedFaction );
            }

            if ( !wasAtWar && this.BaseInfo.IsInWarFooting )
                BaseInfo.TimeEnteredWarFooting = World_AIW2.Instance.GameSecond;
            if ( !this.BaseInfo.IsInWarFooting )
                BaseInfo.TimeEnteredWarFooting = -1;
        }
        public override void UpdatePlanetInfluence_HostOnly( ArcenHostOnlySimContext Context )
        {
            //reset the faction Influences for this one
            List<Planet> planetsInfluenced = Planet.GetTemporaryPlanetList( "ZA-UpdatePlanetInfluence_HostOnly-planetsInfluenced", 10f );
            if ( planetsInfluenced == null ) //blocked for teardown/shutdown; bail
                return;

            List<SafeSquadWrapper> spawners = this.BaseInfo.Spawners.GetDisplayList();
            for ( int i = 0; i < spawners.Count; i++ )
                planetsInfluenced.AddIfNotAlreadyIn( spawners[i].Planet );
            foreach ( GameEntity_Squad warpingSpawner in this.BaseInfo.WarpingInSpawners.DisplaySquads() )
            {
                planetsInfluenced.AddIfNotAlreadyIn( warpingSpawner.Planet );
            }

            AttachedFaction.SetInfluenceForPlanetsToList( planetsInfluenced );
            Planet.ReleaseTemporaryPlanetList( planetsInfluenced );
        }

        private int NumCivilWarParticipants()
        {
            //go through all the ZAs and see how many have the civil war enabled.
            int count = 0;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction faction = World_AIW2.Instance.Factions[i];
                if ( faction.SpecialFactionData.InternalName != "ZenithArchitrave" )
                    continue;
                if ( faction.GetBoolValueForCustomFieldOrDefaultValue( "CivilWarEnabled", true ) )
                    count++;
            }
            return count;
        }

        public override void SeedStartingEntities_EarlyMajorFactionClaimsOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            //TODO: add an option to force these to be further apart if possible
            //jcf: generator does this, without additional args needed

            StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, AttachedFaction, SpecialEntityType.None, "ZenithArchitravePortalOriginal", SeedingType.HardcodedCount, 1,
                                                    MapGenCountPerPlanet.One, MapGenSeedStyle.FullUseByFaction, 7, 7, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal );

        }
        private FInt GetOverallPowerLevelOfEnemies( Faction faction )
        {
            FInt sum = FInt.Zero;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {

                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;
                if ( faction == otherFaction )
                    continue;
                if ( !faction.GetIsHostileTowards( otherFaction ) )
                    continue; //only for hostile factions
                sum += otherFaction.OverallPowerLevel;
            }
            return sum;

        }
        private void UpdateAllegiance( ArcenSimContextAnyStatus Context )
        {
            //Default rule is "Hostile to all except Zenith Trader", friendly to players in the PlayersArchitraveIsFriendlyToward list
            //Different rules apply during Civil War or Expansion mode
            string allegiance = AttachedFaction.BaseInfo.Allegiance;
            bool localDebug = false;
            bool civilWarRetreatMode = false;
            if ( BaseInfo.CivilWarEnemies.Count == 0 && !BaseInfo.ShouldOtherArchitravesAttackMe &&
                 (BaseInfo.TimeLastCivilWarEnded > 0 && (World_AIW2.Instance.GameSecond - BaseInfo.TimeLastCivilWarEnded <= BaseInfo.TimeAfterCivilWarForRetreat)) )
                civilWarRetreatMode = true;
            if ( BaseInfo.CivilWarEnemies.Count > 0 || BaseInfo.ShouldOtherArchitravesAttackMe ||
                 civilWarRetreatMode ) //don't go back to normal allegiances for a bit after the civil war, to let our ships retreat w/o destroying the AI
            {
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "UpdateAllegiance: civil war path. civilWarRetreatMode " + civilWarRetreatMode + ". TimeLastCivilWarEnded " + BaseInfo.TimeLastCivilWarEnded + " and now its " + World_AIW2.Instance.GameSecond + " idx " + AttachedFaction.FactionIndex, Verbosity.DoNotShow );

                //in civil war, we hate the "too strong" dysons and players, and allied to the AIs
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( AttachedFaction == otherFaction )
                        continue;
                    if ( otherFaction.Type == FactionType.NaturalObject )
                        continue;
                    if ( otherFaction.SpecialFactionData != null && otherFaction.SpecialFactionData.InternalName == "ZenithArchitrave" )
                    {
                        //this is the ZA settings during a civil war.
                        if ( civilWarRetreatMode )
                        {
                            //If a civil war has just ended,
                            //keep the civil-war hostilities during the retreat for maximum carnage
                            AttachedFaction.MakeHostileTo( otherFaction );
                            otherFaction.MakeHostileTo( AttachedFaction );
                            continue;
                        }
                        ZenithArchitraveFactionBaseInfo gData = otherFaction.TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                        if ( BaseInfo.ShouldOtherArchitravesAttackMe )
                        {
                            //I am a big ZA, I am always hostile to all other ZAs
                            AttachedFaction.MakeHostileTo( otherFaction );
                            otherFaction.MakeHostileTo( AttachedFaction );
                        }
                        else if ( gData.ShouldOtherArchitravesAttackMe )
                        {
                            //I am a small ZA, and I want to take down the big ones
                            AttachedFaction.MakeHostileTo( otherFaction );
                            otherFaction.MakeHostileTo( AttachedFaction );
                        }
                        else
                        {
                            //I am a small ZA and the other ZA is also small, so we are allied against the big ones
                            AttachedFaction.MakeFriendlyTo( otherFaction );
                            otherFaction.MakeFriendlyTo( AttachedFaction );
                        }
                    }
                    else if ( AttachedFaction.BaseInfo.Allegiance == otherFaction.BaseInfo.Allegiance &&
                         AttachedFaction.BaseInfo.Allegiance != "Hostile To All" )
                    {
                        //if we are on the same team
                        AttachedFaction.MakeFriendlyTo( otherFaction );
                        otherFaction.MakeFriendlyTo( AttachedFaction );
                    }
                    else if ( otherFaction.SpecialFactionData != null && otherFaction.SpecialFactionData.InternalName == "ZenithTrader" )
                    {
                        AttachedFaction.MakeFriendlyTo( otherFaction );
                        otherFaction.MakeFriendlyTo( AttachedFaction );
                    }
                    else if ( BaseInfo.CivilWarEnemies.Contains( otherFaction.FactionIndex ) ||
                         otherFaction.SpecialFactionData != null &&
                         (otherFaction.SpecialFactionData.InternalName == "ZenithMiners" ||
                           otherFaction.SpecialFactionData.InternalName == "DarkZenith" ||
                           otherFaction.SpecialFactionData.InternalName == "DarkSpire" ||
                           otherFaction.SpecialFactionData.InternalName == "ZenithDysonSphere") )
                    {
                        AttachedFaction.MakeHostileTo( otherFaction );
                        otherFaction.MakeHostileTo( AttachedFaction );
                        continue;
                    }
                    else if ( otherFaction.SpecialFactionData != null &&
                         otherFaction.SpecialFactionData.AlliedToAIByDefault )
                    {
                        AttachedFaction.MakeFriendlyTo( otherFaction );
                        otherFaction.MakeFriendlyTo( AttachedFaction );
                        continue;
                    }
                    else if ( BaseInfo.PlayerAllied )
                    {
                        if ( otherFaction.Type == FactionType.Player ||
                             FactionUtilityMethods.Instance.IsFactionAlliedToAnyPlayer( otherFaction ) )
                        {
                            AttachedFaction.MakeFriendlyTo( otherFaction );
                            otherFaction.MakeFriendlyTo( AttachedFaction );
                            continue;
                        }
                    }
                    else
                    {
                        switch ( otherFaction.Type )
                        {
                            case FactionType.Player:
                                if ( !BaseInfo.PlayerAllied )
                                {
                                    AttachedFaction.MakeHostileTo( otherFaction );
                                    otherFaction.MakeHostileTo( AttachedFaction );
                                }
                                break;
                            case FactionType.AI:
                                AttachedFaction.MakeFriendlyTo( otherFaction );
                                otherFaction.MakeFriendlyTo( AttachedFaction );
                                break;
                            case FactionType.SpecialFaction:
                                AttachedFaction.MakeFriendlyTo( otherFaction );
                                otherFaction.MakeFriendlyTo( AttachedFaction );
                                break;
                        }
                    }
                }
            }
            else
            {
                if ( localDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine( "UpdateAllegiance: non-civil-war path. TimeLastCivilWarEnded " + BaseInfo.TimeLastCivilWarEnded + " and now its " + World_AIW2.Instance.GameSecond, Verbosity.DoNotShow );
                //non-civil war. Friendly to our Team
                if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Red" ) )
                {
                    BaseInfo.MinorFactionAllied = true;
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( AttachedFaction.ToString() + " is on team red", Verbosity.DoNotShow );
                    AllegianceHelper.AllyThisFactionToMinorFactionTeam( AttachedFaction, "Minor Faction Team Red" );
                }
                else if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Blue" ) )
                {
                    BaseInfo.MinorFactionAllied = true;
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( AttachedFaction.ToString() + " is on team blue", Verbosity.DoNotShow );
                    AllegianceHelper.AllyThisFactionToMinorFactionTeam( AttachedFaction, "Minor Faction Team Blue" );
                }
                else if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Green" ) )
                {
                    BaseInfo.MinorFactionAllied = true;
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( AttachedFaction.ToString() + " is on team green", Verbosity.DoNotShow );
                    AllegianceHelper.AllyThisFactionToMinorFactionTeam( AttachedFaction, "Minor Faction Team Green" );
                }
                else if ( ArcenStrings.Equals( allegiance, "Friendly To Players" ) )
                {
                    BaseInfo.PlayerAllied = true;
                    AllegianceHelper.AllyThisFactionToHumans( AttachedFaction );
                    if ( localDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( AttachedFaction.ToString() + " is player-allied", Verbosity.DoNotShow );
                }
                //Now handle everyone else. Hostile to the AI, Hostile to everyone else except players with truces and their allied factions
                for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    if ( AttachedFaction == otherFaction )
                        continue;
                    if ( otherFaction.Type == FactionType.NaturalObject )
                        continue;

                    if ( otherFaction.Type == FactionType.NaturalObject )
                    {
                        AttachedFaction.MakeNeutralTo( otherFaction );
                        otherFaction.MakeNeutralTo( AttachedFaction );
                        continue;
                    }

                    if ( otherFaction.SpecialFactionData != null )
                    {
                        //"Player Allied" means "You set the ZA to be a player's ally during the game lobby"
                        if ( BaseInfo.PlayerAllied )
                        {
                            //ally to players and to some factions that are "integral" to players
                            //for example, solo arks and fallen spire. Starkelp TODO: add civilian industries to this
                            if ( otherFaction.Type == FactionType.Player ||
                                 FactionUtilityMethods.Instance.IsFactionAlliedToAnyPlayer( otherFaction ) )
                            {
                                AttachedFaction.MakeFriendlyTo( otherFaction );
                                otherFaction.MakeFriendlyTo( AttachedFaction );
                                continue;
                            }
                            if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( otherFaction ) ||
                                 otherFaction.SpecialFactionData.InternalName == "FallenSpire" )
                            {
                                AttachedFaction.MakeFriendlyTo( otherFaction );
                                otherFaction.MakeFriendlyTo( AttachedFaction );
                                continue;
                            }
                        }
                        //then the normal cases
                        if ( otherFaction.SpecialFactionData.InternalName == "ZenithTrader" )
                        {
                            AttachedFaction.MakeFriendlyTo( otherFaction );
                            otherFaction.MakeFriendlyTo( AttachedFaction );
                            continue;
                        }
                        if ( otherFaction.Type == FactionType.AI ||
                             otherFaction.SpecialFactionData.AlliedToAIByDefault )
                        {
                            AttachedFaction.MakeHostileTo( otherFaction );
                            otherFaction.MakeHostileTo( AttachedFaction );
                        }
                        else if ( otherFaction.Type == FactionType.Player )
                        {
                            if ( BaseInfo.PlayersArchitraveIsFriendlyToward.Count > 0 )
                            {
                                //if any player is friendly to the ZA, all spire and champiosn are friendly too.
                                if ( NecromancerEmpireFactionBaseInfo.GetIsThisANecromancerFaction( otherFaction ) ||
                                     otherFaction.SpecialFactionData.InternalName == "FallenSpire" )
                                {
                                    AttachedFaction.MakeFriendlyTo( otherFaction );
                                    otherFaction.MakeFriendlyTo( AttachedFaction );
                                    continue;
                                }
                            }
                            if ( BaseInfo.PlayersArchitraveIsFriendlyToward.Contains( otherFaction.FactionIndex ) && BaseInfo.Pioneers.Count == 0 )
                            {
                                //if the player is friends with this architrave and there aren't pioneers
                                AttachedFaction.MakeFriendlyTo( otherFaction );
                                otherFaction.MakeFriendlyTo( AttachedFaction );
                            }
                            else
                            {
                                //always hostile to players when it has pioneers (or if not truced)
                                AttachedFaction.MakeHostileTo( otherFaction );
                                otherFaction.MakeHostileTo( AttachedFaction );
                            }
                        }
                        else
                        {
                            for ( int j = 0; j < BaseInfo.PlayersArchitraveIsFriendlyToward.Count; j++ )
                            {
                                //this should at most be the number of player factions in the game
                                Faction trucedPlayerFaction = World_AIW2.Instance.GetFactionByIndex( BaseInfo.PlayersArchitraveIsFriendlyToward[j] );
                                if ( otherFaction.GetIsFriendlyTowards( trucedPlayerFaction ) )
                                {
                                    //player allies can avoid being killed by the ZA, but they aren't allowed to build there
                                    AttachedFaction.MakeFriendlyTo( otherFaction );
                                    otherFaction.MakeFriendlyTo( AttachedFaction );
                                }
                            }
                        }
                    }
                }
            }
        }

        public bool HaveLoadedData = false;
        //Stuff from External Constants
        int AttritionInterval; //how quickly extra ships attrition during peacetime

        int CivilWarBonusAttackStartTime; //when a civil war has been going on "too long", other ZAs get bonus attacks against the overly-strong ZAs
        int CivilWarBonusAttackBaseStrength;
        int CivilWarBonusAttackInterval;
        FInt CivilWarBonusAttackStrengthMultiplier;
        int QuiesceTime;

        private void LoadCustomDataIfNeeded()
        {
            if ( this.HaveLoadedData )
                return;
            this.HaveLoadedData = true;
            AttritionInterval = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_ZenithArchitrave_AttritionInterval" );
            CivilWarBonusAttackStartTime = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_ZenithArchitrave_CivilWarBonusAttackStartTime" );
            CivilWarBonusAttackBaseStrength = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_ZenithArchitrave_CivilWarBonusAttackBaseStrength" );
            CivilWarBonusAttackInterval = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_ZenithArchitrave_CivilWarBonusAttackInterval" );
            CivilWarBonusAttackStrengthMultiplier = ExternalConstants.Instance.GetCustomFInt_Slow( "custom_FInt_ZenithArchitrave_CivilWarBonusAttackStrengthMultiplier" );
            QuiesceTime = ExternalConstants.Instance.GetCustomInt32_Slow( "custom_int_ZenithArchitrave_QuiesceTime" );
        }
        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;
            //            tracing = true; //for debug
            if ( AttachedFaction.MinFireteamStrength == -1 )
                AttachedFaction.MinFireteamStrength = 2000;
            if ( AttachedFaction.MaxFireteamStrength == -1 )
                AttachedFaction.MaxFireteamStrength = 7000;
            LoadCustomDataIfNeeded();
            UpdateQuiesce( Context );
            DoInitialStrikeIfNecessary( Context );
            UpdateTerritory( Context );
            UpdateWarFooting( Context );
            if ( World_AIW2.Instance.GameSecond < 3 )
                return; //there can be problems about the short term counting code not having info at the very beginning of the game
            UpdateAllegiance( Context );
            SpawnPioneersIfNecessary( Context );
            CheckForCivilWar( Context );
            HandleCivilWarBonusAttacks( Context );
            HandleCivilWarDecay( Context );
            ProduceSpawners( Context );
            ProduceDefenses( Context );
            UpdateMetalReserves( Context );
            UpdateStrengthLimits( Context );
            ProduceShipsIfNecessary( Context );
            RemovePioneersDuringCivilWar_Sim( Context );
            UpdateSpawnersAndTurrets( Context );

            HandleJournalEntries( Context );
            UpdatePlanetBGs();
            //After doing all the work, drop the metal reserves to the max if necessary
            //This way if the income is significantly > the max then we don't cut things off badly
            if ( BaseInfo.MetalReserves > BaseInfo.Difficulty.MaxMetalReserves )
                BaseInfo.MetalReserves = BaseInfo.Difficulty.MaxMetalReserves;
            if ( BaseInfo.GolemMetalReserves > BaseInfo.Difficulty.MaxGolemMetalReserves )
                BaseInfo.GolemMetalReserves = BaseInfo.Difficulty.MaxGolemMetalReserves;
            if ( BaseInfo.DefensiveMetalReserves > BaseInfo.Difficulty.MaxMetalReserves )
                BaseInfo.DefensiveMetalReserves = BaseInfo.Difficulty.MaxMetalReserves;

            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }

        public void UpdateQuiesce( ArcenHostOnlySimContext Context )
        {
            //if we've been hacked by the player to be Quiesced, see if we're done being quiesced
            if ( !BaseInfo.IsQuiesced )
                return;

            if ( BaseInfo.QuiesceEndTime == -1 )
                BaseInfo.QuiesceEndTime = World_AIW2.Instance.GameSecond + QuiesceTime;
            if ( World_AIW2.Instance.GameSecond >= BaseInfo.QuiesceEndTime )
                BaseInfo.IsQuiesced = false;

        }
        public void RemovePioneersDuringCivilWar_Sim( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host
            if ( BaseInfo.CivilWarEnemies.Count == 0 )
                return;
            int range = 1000;
            List<SafeSquadWrapper> spawners = this.BaseInfo.Spawners.GetDisplayList();
            List<SafeSquadWrapper> pioneers = this.BaseInfo.Pioneers.GetDisplayList();
            for ( int i = 0; i < pioneers.Count; i++ )
            {
                GameEntity_Squad pioneer = pioneers[i].GetSquad();
                if ( pioneer == null )
                    continue;
                for ( int j = 0; j < spawners.Count; j++ )
                {
                    GameEntity_Squad spawner = spawners[j].GetSquad();
                    if ( spawner == null )
                        continue;
                    if ( pioneer.Planet != spawner.Planet )
                        continue;
                    if ( Mat.DistanceBetweenPointsImprecise( pioneer.WorldLocation, spawner.WorldLocation ) < range )
                        pioneer.Despawn( Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
                }
            }
        }

        private void UpdatePlanetBGs()
        {
            for ( int i = 0; i < BaseInfo.Territory.Count; i++ )
            {
                Planet plan = null;
                try
                {
                    plan = BaseInfo.Territory[i];
                }
                catch { continue; }
                if ( plan != null )
                {
                    plan.SpaceBox_TagMustMatch = "ZenithArchitrave";
                    if ( plan.IsZenithArchitraveHome )
                        plan.Planet_TagMustMatch = "ZenithArchitrave_HomePlanet";
                }
            }
        }

        public void HandleJournalEntries( ArcenHostOnlySimContext Context )
        {
            //If appropriate, emit journal entries
            if ( !ArcenNetworkAuthority.GetIsHostMode() )
                return;
            bool allExplored = true;
            bool anyExplored = false;
            for ( int i = 0; i < BaseInfo.Territory.Count; i++ )
            {
                if ( BaseInfo.Territory[i].IntelLevel > PlanetIntelLevel.Unexplored )
                {
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_ZenithArchitrave_FirstFoundTerritory", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    anyExplored = true;
                }
                else
                    allExplored = false;
            }
            if ( allExplored && BaseInfo.Portal.Display.GetSquad() != null ) //portal can be null
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_ZenithArchitrave_AllExplored", string.Empty, AttachedFaction, null, BaseInfo.Portal.Display.Planet, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            if ( anyExplored )
            {
                if ( BaseInfo.Pioneers.Count > 0 )
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_ZenithArchitrave_PioneerSpawn", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                if ( BaseInfo.CivilWarEnemies.Count > 0 || BaseInfo.ShouldOtherArchitravesAttackMe ) //I think this check is overkill, but just in case
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_ZenithArchitrave_CivilWarStart", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
            Faction firstPlayerFaction = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( BaseInfo.PlayersArchitraveIsFriendlyToward.Contains( firstPlayerFaction.FactionIndex ) )
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_ZenithArchitrave_TruceStart", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                if ( BaseInfo.CivilWarEnemies.Count > 0 )
                {
                    BaseInfo.IsBetrayingTruce = true;
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_ZenithArchitrave_TruceBrokenCivilWar", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                }
                if ( BaseInfo.Pioneers.Count > 0 )
                {
                    BaseInfo.IsBetrayingTruce = true;
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "ZO_ZenithArchitrave_TruceBrokenExpansion", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                }
                if ( BaseInfo.CivilWarEnemies.Count == 0 && BaseInfo.Pioneers.Count == 0 && !BaseInfo.ShouldOtherArchitravesAttackMe &&
                     BaseInfo.IsBetrayingTruce )
                {
                    //we were betraying our truce before, but now we are resuming it
                    BaseInfo.IsBetrayingTruce = false;
                    World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Zenith Architrave</color> has resumed its truce with you.", ChatType.LogToCentralChat, null );
                }
            }


        }

        public void UpdateStrengthLimits( ArcenHostOnlySimContext Context )
        {
            //iterate over all the spawners in the galaxy and figure out the total strength allowed we have
            //In peace, we have a straightforward calculation. In war footing the rule is "Double peace, then
            //increase as the war goes longer."
            List<SafeSquadWrapper> spawners = this.BaseInfo.Spawners.GetDisplayList();
            int strength = 0;
            for ( int i = 0; i < spawners.Count; i++ )
                strength += BaseInfo.GetAllowedPeaceStrengthForSpawner( spawners[i].GetSquad(), BaseInfo.Difficulty );

            BaseInfo.TotalAllowedStrengthInPeace = strength;
            BaseInfo.TotalAllowedStrengthInWarFooting = strength * 2;
            if ( BaseInfo.IsInWarFooting )
            {
                int timeAtWar = World_AIW2.Instance.GameSecond - BaseInfo.TimeEnteredWarFooting;
                int units = timeAtWar / BaseInfo.Difficulty.WarIntervalForStrengthIncrease;
                if ( units > 0 )
                {
                    BaseInfo.TotalAllowedStrengthInWarFooting += (BaseInfo.TotalAllowedStrengthInWarFooting * BaseInfo.Difficulty.AllowedStrengthIncreaseMultiplierPerInterval * units).IntValue;
                }
            }
        }
        public void DoInitialStrikeIfNecessary( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //don't even try this on clients

            if ( BaseInfo.HasDoneInitialStrike && BaseInfo.Spawners.Count > 0 )
                return;
            BaseInfo.HasDoneInitialStrike = true;
            List<SafeSquadWrapper> spawners = this.BaseInfo.Spawners.GetDisplayList();
            //this logic is largely borrowed from the Nanocaust. The goal is to dramatically weaken the AI forces on the ZA
            //initial planets to bring them online more quickly. Note there is a DF option if I use an ArcenLessLinkedList for Spawners;
            //maybe I should do that
            for ( int i = 0; i < spawners.Count; i++ )
            {
                GameEntity_Squad spawner = spawners[i].GetSquad();
                if ( spawner == null )
                    continue;
                //First spawn some bonus units, then devastate the AI defenses
                int shipsToSummon = 4;
                for ( int j = 0; j < shipsToSummon; j++ )
                {
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ArchitraveTierTwo" );
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( spawner.PlanetFaction, entityData, 4,
                                                spawner.GetFactionLooseFleetOrNull_Safe(), 0, spawner.WorldLocation, Context, "ZA-InitialStrike" );  //is fine, main sim thread
                    newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;//this throws off the calculations
                    newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread

                }
                int defensesToBuild = 5;
                for ( int j = 0; j < defensesToBuild; j++ )
                {
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ArchitraveDefensiveStructure" );
                    ArcenPoint spawnLocation = spawner.Planet.GetSafePlacementPoint_AroundEntity( Context, entityData, spawner, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 150 ) );

                    GameEntity_Squad.CreateNew_ReturnNullIfMPClient( spawner.PlanetFaction, entityData, 4,
                                                spawner.GetFactionLooseFleetOrNull_Safe(), 0, spawnLocation, Context, "ZA-InitialStrike" );  //is fine, main sim thread
                }

                PlanetFaction pFaction = spawner.Planet.GetPlanetFactionForFaction( AttachedFaction );
                foreach ( PlanetFaction otherFaction in pFaction.RelatedFactions( FactionRelationship.FactionsThatAreHostileTowardsMe ) )
                {
                    foreach ( GameEntity_Squad entity in otherFaction.Entities.Squads( EntityRollupType.BlocksEnemyClaimFlows ) )
                    {
                        //should prevent AIP from being incurred, or other on-death effects.  Just get rid of me!
                        if ( entity.GetFactionTypeSafe() != FactionType.Player )
                            entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.ClearingOtherNPCUnits );
                    }
                    foreach ( GameEntity_Squad entity in otherFaction.Entities.Squads( EntityRollupType.NormalPlanetNastyPick ) )
                    {
                        //should prevent AIP from being incurred, or other on-death effects.  Just get rid of me!
                        if ( entity.GetFactionTypeSafe() != FactionType.Player )
                            entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.ClearingOtherNPCUnits );
                    }
                    foreach ( GameEntity_Squad entity in otherFaction.Entities.Squads( EntityRollupType.MobileCombatants ) )
                    {
                        //should prevent AIP from being incurred, or other on-death effects.  Just get rid of me!
                        if ( entity.GetFactionTypeSafe() != FactionType.Player )
                            entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.ClearingOtherNPCUnits );
                    }
                    foreach ( GameEntity_Squad entity in otherFaction.Entities.Squads( EntityRollupType.ForReinforcementType_Turret ) )
                    {
                        //should prevent AIP from being incurred, or other on-death effects.  Just get rid of me!
                        if ( entity.GetFactionTypeSafe() != FactionType.Player )
                            entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.ClearingOtherNPCUnits );
                    }
                    foreach ( GameEntity_Squad entity in otherFaction.Entities.Squads( EntityRollupType.ForReinforcementType_NonTurretDefense ) )
                    {
                        //should prevent AIP from being incurred, or other on-death effects.  Just get rid of me!
                        if ( entity.GetFactionTypeSafe() != FactionType.Player )
                            entity.SetToBeRemovedAtEndOfThisFrameForReason( InstancedRendererDeactivationReason.ClearingOtherNPCUnits );
                    }
                }
            }
        }
        public void UpdateMetalReserves( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host
            //figure out how much metal we have to work with! Metal is for building ships.
            //this relies on some non-trivial math to figure out how the various ZA income modifiers apply
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-UpdateMetalReserves-trace", 10f ) : null;
            int income = BaseInfo.Difficulty.BaseMetalIncome;
            int defensiveIncome = BaseInfo.Difficulty.BaseDefensiveMetalIncome;
            if ( BaseInfo.IsQuiesced )
            {
                if ( tracing )
                    tracingBuffer.Add( "We are quiesced, so no income\n" );
                return;
            }
            if ( BaseInfo.CivilWarEnemies.Count > 0 || BaseInfo.ShouldOtherArchitravesAttackMe )
                income = BaseInfo.Difficulty.BaseMetalIncomeCivilWar;
            //Update the ZA income for when the ZA is hacked.
            if ( BaseInfo.IsBeingHacked )
                income *= 2;
            if ( tracing )
                tracingBuffer.Add( " Update Metal Reserves: base income " + income ).Add( " at " ).Add( World_AIW2.Instance.GameSecond ).Add( ". BeingHacked " + BaseInfo.IsBeingHacked + "\n" );
            BaseInfo.AppliedModifiers_ForUI = "Here's how we calculated the above income: Base Income " + income + ".\n";
            if ( BaseInfo.IsInWarFooting )
            {
                int timeAtWar = World_AIW2.Instance.GameSecond - BaseInfo.TimeEnteredWarFooting;
                //first scan all the available modifiers to see if any need to be added to the list
                //adding them to the AppliedModifiers list (or updating that list)
                for ( int i = 0; i < BaseInfo.Difficulty.IncomeModifiers.Count; i++ )
                {
                    ArchitraveIncomeModifier modifier = BaseInfo.Difficulty.IncomeModifiers[i];
                    if ( !DoesModifierApply( modifier ) )
                        continue;

                    bool foundModifier = false;
                    for ( int j = 0; j < BaseInfo.AppliedModifiers.Count; j++ )
                    {
                        if ( BaseInfo.AppliedModifiers[j].Name == modifier.Name )
                        {
                            foundModifier = true;
                            break;
                        }
                    }
                    if ( !foundModifier )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "Adding : " + modifier.ToString() + " to the modifier list" );
                        BaseInfo.AppliedModifiers.Add( modifier );
                    }
                }
                //then actually apply those modifiers to update the income

                for ( int i = BaseInfo.AppliedModifiers.Count - 1; i >= 0; i-- )
                {
                    ArchitraveIncomeModifier modifier = BaseInfo.AppliedModifiers[i];
                    if ( !DoesModifierApply( modifier ) )
                    {
                        //looks like this modifier no longer applies (maybe we conquered all our Territory but are still at war, so we should remove the "Without Full Territory" modifier.
                        BaseInfo.AppliedModifiers.RemoveAt( i );
                        if ( tracing )
                            tracingBuffer.Add( "Removing : " + modifier.ToString() + " from the modifier list (no longer applies)" );
                        continue;
                    }
                    if ( modifier.TimeInterval == -1 ) //this is just a flat modification; not based on TimesApplied
                        modifier.TimesApplied = 1;
                    if ( timeAtWar % modifier.TimeInterval == 0 )
                    {
                        modifier.TimesApplied++;
                        BaseInfo.AppliedModifiers[i] = modifier; //update the value in the list, not just the local copy
                        if ( tracing ) 
                            tracingBuffer.Add( "Just updated Times Applied for : " + modifier.ToString() + "." );
                    }
                    if ( modifier.TimesApplied == 0 )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "Skipping : " + modifier.ToString() + ", it hasn't been applied yet" );
                        continue;
                    }
                    int previousincome = income;
                    //now we apply the modifier
                    if ( modifier.AdditiveIncrease > 0 )
                    {
                        income += modifier.AdditiveIncrease * modifier.TimesApplied;
                    }
                    if ( modifier.MultiplicativeIncrease > FInt.Zero )
                    {
                        income = income + ((income * modifier.MultiplicativeIncrease).IntValue * modifier.TimesApplied);
                    }
                    if ( modifier.Multiplier > FInt.Zero )
                    {
                        FInt mult = modifier.Multiplier;
                        for ( int j = 0; j < modifier.TimesApplied; j++ )
                            mult *= modifier.Multiplier;

                        income = (income * mult).IntValue;
                    }
                    if ( !String.IsNullOrEmpty( modifier.UnitTag ) )
                        throw new Exception( "Unit tag not implemented for ZA income modifier" );
                    if ( tracing )
                        tracingBuffer.Add( "\tAfter applying modifier " ).Add( modifier.ToString() ).Add( " our income is updated; " ).Add( previousincome ).Add( " --> " ).Add( income ).Add( "\n" );
                    BaseInfo.AppliedModifiers_ForUI += "\t" + modifier.ToStringForDisplay() + "\n";
                }
            }
            else
            {
                //not at war; clear our modifiers
                BaseInfo.AppliedModifiers.Clear();
            }

            BaseInfo.IncomeLastSecond = income;
            BaseInfo.MetalReserves += income;

            BaseInfo.DefensiveMetalReserves += defensiveIncome; //defensive metal reserves don't get bigger if in war

            if ( this.BaseInfo.IsInWarFooting && //we are at war AND
                 (BaseInfo.CivilWarEnemies.Count > 0 || //we are in a civil war  OR
                   BaseInfo.ShouldOtherArchitravesAttackMe ||
                   BaseInfo.OverallPowerLevelOfEnemies >= FInt.FromParts( 4, 000 )) ) //our enemies are pretty strong
                BaseInfo.GolemMetalReserves += income; //then we can build golems

            if ( tracing )
                tracingBuffer.Add( " New metal regular : " + BaseInfo.MetalReserves + " golem metal: " + BaseInfo.GolemMetalReserves + "\n" );
            if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        private bool DoesModifierApply( ArchitraveIncomeModifier modifier )
        {
            //used by the Income code
            if ( !BaseInfo.IsInWarFooting )
                return false;
            if ( modifier.FallenSpireCities > 0 )
            {
                int cities = FallenSpireFactionBaseInfo.Instance == null ? 0 : FallenSpireFactionBaseInfo.Instance.SpireCities.Count;
                if ( cities == 0 || cities < modifier.FallenSpireCities )
                    return false;
            }
            if ( modifier.CivilWarOffensiveOnly &&
                 BaseInfo.CivilWarEnemies.Count <= 0 )
                return false;
            if ( modifier.CivilWarDefensive && !BaseInfo.ShouldOtherArchitravesAttackMe )
                return false;
            if ( (BaseInfo.CivilWarEnemies.Count > 0 || BaseInfo.ShouldOtherArchitravesAttackMe)
                 && !(modifier.CivilWarDefensive || modifier.CivilWarOffensiveOnly) ) //if we are in the civil war, only civil war modifiers apply
                return false;
            if ( modifier.WithoutFullTerritory && BaseInfo.Spawners.Count >= BaseInfo.MaxTerritorySize )
                return false;
            if ( modifier.ControlsLessThanXPlanets > 0 &&
                 BaseInfo.Spawners.Count >= modifier.ControlsLessThanXPlanets )
                return false;
            return true;
        }
        public void SpawnPioneersIfNecessary( ArcenHostOnlySimContext Context )
        {
            //if it's time, spawn pioneers
            if ( BaseInfo.IsQuiesced )
            {
                if ( BaseInfo.PioneerSpawnTime > 0 )
                {
                    BaseInfo.TimesPioneersInterrupted++; //Track whether pioneers were planning on spawning soon
                }
                BaseInfo.PioneerSpawnTime = -1;
                return;
            }
            if ( BaseInfo.Pioneers.Count > 0 || BaseInfo.CivilWarEnemies.Count > 0 )
            {
                if ( BaseInfo.CivilWarEnemies.Count > 0 && BaseInfo.PioneerSpawnTime > 0 )
                {
                    BaseInfo.TimesPioneersInterrupted++;  //Track whether pioneers were planning on spawning soon and were interrupted
                }
                BaseInfo.PioneerSpawnTime = -1;
                return;
            }
            if ( BaseInfo.IsInWarFooting )
            {
                if ( BaseInfo.PioneerSpawnTime > 0 )
                {
                    BaseInfo.PioneerSpawnTime = -1;
                    BaseInfo.TimesPioneersInterrupted++;
                }
                return; //no pioneers until I've cleaned things up
            }
            if ( BaseInfo.PioneerSpawnTime > 0 )
                BaseInfo.PioneerSpawnTimeElapsed++; //we count every second that passes waiting for pioneers, so if we are interrupted we can not lose too much time
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-SpawnPioneersIfNecessary-trace", 10f ) : null;

            if ( BaseInfo.PioneerSpawnTime == -1 )
            {
                if ( BaseInfo.Territory.Count >= BaseInfo.MaxTerritorySize )
                {
                    //only spawn pioneers once we're at our full basic Territory size
                    BaseInfo.PioneerSpawnTime = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.PeaceTimeBeforePioneers; //default time
                    int oldInterrupted = BaseInfo.TimesPioneersInterrupted;
                    if ( BaseInfo.TimesPioneersInterrupted > 0 )
                    {
                        //if we had been interrupted previo\us times we were trying to make pioneers, spawn them more quickly now
                        //(otherwise you can permanently delay Pioneers by attacking every 10 minutes or so)
                        int minTime = 60;

                        int remainingTime = BaseInfo.Difficulty.PeaceTimeBeforePioneers - BaseInfo.PioneerSpawnTimeElapsed;
                        int secondsPerInterruption = 60;
                        if ( BaseInfo.TimesPioneersInterrupted >= 4 )
                            secondsPerInterruption += 30;
                        if ( BaseInfo.TimesPioneersInterrupted >= 8 )
                            secondsPerInterruption += 30;

                        remainingTime -= BaseInfo.TimesPioneersInterrupted * secondsPerInterruption;
                        if ( remainingTime < minTime )
                            remainingTime = minTime;
                        BaseInfo.PioneerSpawnTime = World_AIW2.Instance.GameSecond + remainingTime;
                    }
                    if ( AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) )
                        BaseInfo.PioneerSpawnTime = World_AIW2.Instance.GameSecond + 60;

                    if ( tracing )
                        tracingBuffer.Add( "We will be Spawning pioneers at " + BaseInfo.PioneerSpawnTime + " seconds in! war footing " + BaseInfo.IsInWarFooting + ", we were interrupted " + oldInterrupted + " times \n" );
                }
                if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return; //we've updated the pioneer spawn time, or it's not appropriate for us to spawn them
            }

            if ( BaseInfo.PioneerSpawnTime <= World_AIW2.Instance.GameSecond )
            {
                //In civil war mode, the ZA spawns fewer pioneers, since a lot of their gameplay impact is in the Civil Wars
                int pioneersToSpawn = 1;
                if ( BaseInfo.CivilWarEnabled )
                {
                    pioneersToSpawn = BaseInfo.Difficulty.ExcessSpawnersToTriggerOtherArchitravesToAttackMe / 2;
                    if ( pioneersToSpawn < 1 )
                        pioneersToSpawn = 1;
                }
                else
                {
                    pioneersToSpawn = Math.Min(6, BaseInfo.Spawners.Count / 2 );
                    if ( pioneersToSpawn < 2 )
                        pioneersToSpawn = 2; //min of 2 for non-civil-war case
                }
                if ( tracing )
                    tracingBuffer.Add( "Spawning " ).Add( pioneersToSpawn ).Add( "pioneers now (at " + BaseInfo.PioneerSpawnTime + "). Civil War Enabled? " + BaseInfo.CivilWarEnabled +"\n" );
                BaseInfo.PioneerSpawnTime = -1;
                BaseInfo.PioneerSpawnTimeElapsed = 0; ;
                BaseInfo.TimesPioneersInterrupted = 0;

                List<SafeSquadWrapper> spawners = this.BaseInfo.Spawners.GetDisplayList();
                for ( int i = 0; i < pioneersToSpawn; i++ )
                {
                    GameEntity_Squad spawner = spawners[Context.RandomToUse.Next( 0, spawners.Count )].GetSquad();
                    if ( spawner == null )
                        continue;
                    PlanetFaction pFaction = spawner.Planet.GetPlanetFactionForFaction( AttachedFaction );
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ArchitravePioneer" );
                    if ( entityData == null )
                        throw new Exception( "No ArchitravePioneer defined in XML" );
                    GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, spawner.CurrentMarkLevel,
                                                                          pFaction.Faction.LooseFleet, 0, spawner.WorldLocation, Context, "ZA-Pioneer" );  //is fine, main sim thread
                    if ( entity != null )
                    {
                        entity.ShouldNotBeConsideredAsThreatToHumanTeam = true;//this throws off the calculations
                        entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                    }
                }
            }
            if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }

        public void HandleCivilWarDecay( ArcenHostOnlySimContext Context )
        {
            //We have a problem where civil wars continue to not end properly, this time apparently due to some combination of Castra-provided invulnerability for spawners,
            //and the fact that the golems are all just in multiphase and thus cannot shoot spawners.
            //So if we started the civil war and we are outnumbered "enough" on a planet then the spawner will attrition rapidly

            if ( !BaseInfo.CivilWarEnabled )
                return;
            if ( !BaseInfo.ShouldOtherArchitravesAttackMe || FactionUtilityMethods.Instance.GetNumZenithArchitraves() <= 1 )
                return;
            //okay, there's a civil war and they are gunning for me
            List<SafeSquadWrapper> spawners = this.BaseInfo.Spawners.GetDisplayList();
            for ( int i = 0; i < spawners.Count; i++ )
            {
                GameEntity_Squad spawner = spawners[i].GetSquad();
                if ( spawner == null )
                    continue;
                var factionData = spawner.Planet.GetStanceDataForFaction( AttachedFaction );
                StrengthData_PlanetFaction_Stance hostileStrengthData = factionData[FactionStance.Hostile];
                StrengthData_PlanetFaction_Stance selfStrengthData = factionData[FactionStance.Self];
                if ( hostileStrengthData.TotalStrength > selfStrengthData.TotalStrength * 20 )
                {
                    int damageToTake = (int)(((float)5 / 100) * (spawner.GetMaxHullPoints()));
                    spawner.TakeDamageDirectly( damageToTake, null, null, DamageSource.BeingScrapped, Context );
                }
            }
        }
        public void HandleCivilWarBonusAttacks( ArcenHostOnlySimContext Context )
        {
            //if we've been in the civil war too long, the various ZAs start dropping bonus attacks,
            //which are basically mini-Wormhole Invasions against ZAs which are too strong.
            //This is a cool mechanism, and its also a fail-safe against civil wars taking too long
            //This code is run from the point of view of a ZA which is Too Strong
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //don't even try this on clients
            int debugCode = 0;
            try
            {
                debugCode = 100;
                if ( !BaseInfo.CivilWarEnabled )
                    return;
                if ( !BaseInfo.ShouldOtherArchitravesAttackMe || FactionUtilityMethods.Instance.GetNumZenithArchitraves() <= 1 )
                    return;

                debugCode = 200;
                int warTime = World_AIW2.Instance.GameSecond - BaseInfo.WhenIBeganCivilWar;
                if ( warTime < CivilWarBonusAttackStartTime )
                    return;//war must have been going on for a while
                if ( warTime % CivilWarBonusAttackInterval == 0 )
                {
                    debugCode = 300;
                    int numBonusAttacks = warTime / CivilWarBonusAttackInterval;
                    //ArcenDebugging.ArcenDebugLogSingleLine("war time: " + warTime + " interval " + CivilWarBonusAttackInterval + " when I began civil war: " + BaseInfo.WhenIBeganCivilWar + " and current time: " + World_AIW2.Instance.GameSecond + " lets go", Verbosity.DoNotShow );
                    FInt multiplier = FInt.One;
                    for ( int i = 0; i < numBonusAttacks; i++ )
                        multiplier *= CivilWarBonusAttackStrengthMultiplier;
                    int strikeStrength = CivilWarBonusAttackBaseStrength + (CivilWarBonusAttackBaseStrength * multiplier).IntValue;

                    //pick a spawner of ours not in our territory and an enemy ZA
                    //create strikeStrength ships of that enemy ZA on this planet
                    Faction enemyFaction = FactionUtilityMethods.Instance.GetRandomZAHostileToMe( AttachedFaction, Context );
                    if ( enemyFaction == null )
                        throw new Exception( "Could not find hostile ZA, though I am large enough and there are multiple ZAs" );
                    debugCode = 350;
                    //pick one of my spawners that's not in my Territory
                    if ( BaseInfo.NonTerritorySpawners.Count == 0 )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine(AttachedFaction.GetDisplayName() + " idx " + AttachedFaction.FactionIndex + " am a civil war target? " + BaseInfo.ShouldOtherArchitravesAttackMe + " and I have " + BaseInfo.Spawners.Count + " spawners", Verbosity.DoNotShow );
                        throw new Exception("I apparently have no NonTerritorySpawners?!");
                    }
                    List<SafeSquadWrapper> nonTerritorySpawners = this.BaseInfo.NonTerritorySpawners.GetDisplayList();
                    GameEntity_Squad spawner = nonTerritorySpawners[Context.RandomToUse.Next( 0, nonTerritorySpawners.Count )].GetSquad();
                    if ( spawner == null )
                        return;
                    debugCode = 400;
                    //now create the units. Lets also make a new "Architrave Wormhole" object too, cause it will be fun
                    //TODO: if necessary, make an Architrave Wormhole for this Architrave
                    //then in stage3 we will remove the wormholes if the spawner is dead
                    //Actually spawn the units
                    PlanetFaction neutralFaction = spawner.Planet.GetFirstFactionOfType( FactionType.NaturalObject );
                    GameEntity_Squad warpPoint = null;
                    debugCode = 500;
                    foreach ( GameEntity_Squad entity in neutralFaction.Entities.Squads( "ArchitraveWarpPoint" ) )
                    {
                        warpPoint = entity;
                        break;
                    }
                    debugCode = 600;
                    if ( warpPoint == null )
                    {
                        debugCode = 700;
                        GameEntityTypeData warpEntityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ArchitraveWarpPoint" );
                        ArcenPoint spawnLocation = spawner.Planet.GetSafePlacementPoint_AroundEntity( Context, warpEntityData, spawner, FInt.FromParts( 0, 200 ), FInt.FromParts( 0, 550 ) );
                        GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( neutralFaction, warpEntityData, 1,
                                                                                 neutralFaction.Faction.LooseFleet, 0, spawnLocation, Context, "ZA-CivilWarBonus" );  //is fine, main sim thread
                        newEntity.DespawnsInXSeconds = 30; //these last only a brief amount of time, to let the ZA units through.
                        warpPoint = newEntity;
                    }
                    //ArcenDebugging.ArcenDebugLogSingleLine("spawning " + strikeStrength + " strength (" + CivilWarBonusAttackBaseStrength + " + " + CivilWarBonusAttackBaseStrength + " * " + multiplier  +") on " + spawner.GetPlanetName_Safe() + " as Civil War reduction effort. Using " + warpPoint.ToStringWithPlanet() + " and spawning units of faction " + enemyFaction.FactionIndex, Verbosity.DoNotShow );
                    debugCode = 800;
                    PlanetFaction pFaction = spawner.Planet.GetPlanetFactionForFaction( enemyFaction );
                    while ( strikeStrength > 0 )
                    {
                        debugCode = 900;
                        GameEntityTypeData entityData = null;
                        int random = Context.RandomToUse.Next( 0, 100 );
                        if ( random < 20 )
                            entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ArchitraveTierZero" );
                        if ( random < 70 )
                            entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ArchitraveTierOne" );
                        else
                            entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ArchitraveTierTwo" );
                        debugCode = 1000;
                        strikeStrength -= entityData.CostForAIToPurchase;
                        GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, (byte)Context.RandomToUse.Next( 1, 5 ),
                                                                                 pFaction.Faction.LooseFleet, 0, warpPoint.WorldLocation, Context, "ZA-CivilWarBonus" );  //is fine, main sim thread
                        if ( newEntity != null )
                        {
                            newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;//this throws off the calculations
                            newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in HandleCivilWarBonusAttacks debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }

        public void CheckForCivilWar( ArcenHostOnlySimContext Context )
        {
            //see if any other ZA is strong enough to trigger a civil war
            bool wasInCivilWar = (BaseInfo.CivilWarEnemies.Count >= 1 || BaseInfo.WasJustTriggeringOtherZAsToAttackMe);
            BaseInfo.CivilWarEnemies.Clear();
            if ( BaseInfo.IsQuiesced )
                return;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-CheckForCivilWar-trace", 10f ) : null;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( AttachedFaction == otherFaction )
                    continue;
                if ( otherFaction.SpecialFactionData.InternalName == "ZenithArchitrave" )
                {
                    ZenithArchitraveFactionBaseInfo otherZAData = otherFaction.TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                    if ( tracing )
                        tracingBuffer.Add( "Checking ZA faction " ).Add( otherFaction.FactionIndex ).Add( " which has " ).Add( otherZAData.Spawners.Count ).Add( " spawners, and " + otherZAData.WarpingInSpawners.Count + " warping in.\n" );
                    if ( otherZAData.ShouldOtherArchitravesAttackMe )
                    {
                        BaseInfo.CivilWarEnemies.Add( otherFaction.FactionIndex );
                        if ( tracing )
                            tracingBuffer.Add( "\tDeclaring war!\n" );
                    }
                }
            }

            if ( (BaseInfo.CivilWarEnemies.Count == 0 && !BaseInfo.ShouldOtherArchitravesAttackMe) && wasInCivilWar )
            {
                BaseInfo.TimeLastCivilWarEnded = World_AIW2.Instance.GameSecond;
                BaseInfo.PioneerSpawnTime = World_AIW2.Instance.GameSecond + BaseInfo.TimeAfterCivilWarForRetreat + BaseInfo.TimeAfterCivilWarForRetreat / 10;
            }
            if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        public void ProduceDefenses( ArcenHostOnlySimContext Context )
        {
            //build defensive structures
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //don't even try this on clients

            //iterate over all spawners and make a few defensive/utility structures if desired
            int debugCode = 0;
            GameEntity_Squad spawner = null;

            try
            {
                List<SafeSquadWrapper> spawners = this.BaseInfo.Spawners.GetDisplayList();
                for ( int i = 0; i < spawners.Count; i++ )
                {
                    spawner = spawners[i].GetSquad();
                    if ( spawner == null )
                        continue;
                    ZenithArchitravePerUnitBaseInfo data = spawner.CreateExternalBaseInfo<ZenithArchitravePerUnitBaseInfo>( "ZenithArchitravePerUnitBaseInfo" );
                    int buildDelay = 20;
                    if ( data.TimeLastBuiltDefensiveStructure > World_AIW2.Instance.GameSecond - buildDelay )
                    {
                        //being able to spam defensive structures during a battle can be very frustrating, since
                        //the defensive structures can provide invulnerability
                        continue;
                    }
                    bool planetUnderAttack = false;
                    if ( spawner.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength >
                         spawner.PlanetFaction.DataByStance[FactionStance.Self].TotalStrength )
                        planetUnderAttack = true;
                    int currentDefensiveStructures = 0;
                    int currentUtilityStructures = 0;
                    foreach ( GameEntity_Squad entity in spawner.PlanetFaction.Entities.Squads( "ArchitraveDefensiveStructure", "WarpingInArchitraveDefensiveStructure" ) )
                    {
                        currentDefensiveStructures++;
                    }
                    foreach ( GameEntity_Squad entity in spawner.PlanetFaction.Entities.Squads( "ArchitraveUtilityStructure", "WarpingInArchitraveUtilityStructure" ) )
                    {
                        currentUtilityStructures++;
                    }

                    int allowedDefensiveStructures = spawner.CurrentMarkLevel * BaseInfo.Difficulty.DefensiveStructuresPerMarkLevel;

                    if ( currentDefensiveStructures < allowedDefensiveStructures )
                    {
                        int retries = 10;
                        while ( retries-- > 0 )
                        {
                            GameEntityTypeData entityData = GetNextZATurretToSpawn(spawner.Planet, Context);
                            if ( entityData == null )
                                throw new Exception( "No ArchitraveDefensiveStructures defined in XML" );

                            GameEntityTypeData warpingEntityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WarpingInArchitraveDefensiveStructure" );
                            if ( entityData == null || warpingEntityData == null )
                                throw new Exception( "No ArchitraveDefensiveStructures defined in XML" );
                            if ( entityData.CostForAIToPurchase > BaseInfo.DefensiveMetalReserves )
                                continue;
                            BaseInfo.DefensiveMetalReserves -= entityData.CostForAIToPurchase;
                            PlanetFaction pFaction = spawner.PlanetFaction;
                            ArcenPoint spawnLocation = spawner.Planet.GetSafePlacementPoint_AroundEntity( Context, entityData, spawner, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 350 ) );
                            data.TimeLastBuiltDefensiveStructure = World_AIW2.Instance.GameSecond;

                            if ( planetUnderAttack )
                            {
                                //spawn the defensive structure directly, no warp in. This is battle time!
                                //Lore justification: It's much more "expensive" to warp in fully built units, so the ZA only does it when its critical
                                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, spawner.CurrentMarkLevel,
                                                                                         pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "ZA-Defenses" );  //is fine, main sim thread

                                if ( newEntity != null && newEntity.TypeData.GetHasTag( "ArchitraveCastra" ) )
                                {
                                    //This is a DoubleBufferedConcurrentList thing.  It is threadsafe
                                    BaseInfo.Castra.AddToDisplayList( newEntity );
                                }
                                break;
                            }
                            else
                            {
                                //this is peaceful, take the slow-warp path
                                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, warpingEntityData, spawner.CurrentMarkLevel,
                                                                                     pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "ZA-Defenses" );  //is fine, main sim thread
                                if ( newEntity.TypeData.GetHasTag("ArchitraveCastra") )
                                {
                                    //This is a DoubleBufferedConcurrentList thing.  It is threadsafe
                                    BaseInfo.Castra.AddToDisplayList( newEntity );
                                }
                                newEntity.TransformsIntoAfterTime = entityData.InternalName;
                                newEntity.SecondsTillTransformation = (Int16)Context.RandomToUse.Next( 30, 120 );
                                break; //only one defensive structure per sim-step
                            }
                        }
                    }
                    int allowedUtilityStructures = spawner.CurrentMarkLevel * BaseInfo.Difficulty.UtilityStructuresPerMarkLevel;

                    if ( currentUtilityStructures < allowedUtilityStructures && !planetUnderAttack )
                    {
                        int retries = 10;
                        while ( retries-- > 0 )
                        {
                            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ArchitraveUtilityStructure" );
                            GameEntityTypeData warpingEntityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "WarpingInArchitraveUtilityStructure" );
                            if ( entityData == null || warpingEntityData == null )
                                throw new Exception( "No ArchitraveUtilityStructures defined in XML" );
                            if ( entityData.CostForAIToPurchase > BaseInfo.DefensiveMetalReserves )
                                continue;
                            BaseInfo.DefensiveMetalReserves -= entityData.CostForAIToPurchase;
                            PlanetFaction pFaction = spawner.PlanetFaction;

                            //pick a random adjacent planet, then build the utility structure between the wormhole to that planet
                            //and the spawner
                            Planet otherPlanet = spawner.Planet.GetRandomNeighbor( false, Context );
                            GameEntity_Other wormhole = spawner.Planet.GetWormholeTo( otherPlanet );

                            ArcenPoint spawnLocation = spawner.Planet.GetSafePlacementPoint_AroundEntity( Context, entityData, wormhole, FInt.FromParts( 0, 025 ), FInt.FromParts( 0, 075 ) );
                            GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, warpingEntityData, spawner.CurrentMarkLevel,
                                                                                     pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "ZA-Defenses" );  //is fine, main sim thread
                            if ( newEntity != null )
                            {
                                newEntity.TransformsIntoAfterTime = entityData.InternalName;
                                newEntity.SecondsTillTransformation = (Int16)Context.RandomToUse.Next( 30, 120 );
                            }
                            break; //only one defensive structure per sim-step
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception code in ProduceDefenses " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }

        public void ProduceSpawners( ArcenHostOnlySimContext Context )
        {
            //If we are allowed to make a new Spawner, do it
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //don't even try this on clients

            //Iterate over all Pioneers and Territory see if they can build
            GameEntity_Squad entity = null;
            PlanetFaction pFaction = null;
            int debugCode = 0;
            try
            {
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRowByName( "WarpingInZenithArchitraveSpawner" );
                
                debugCode = 100;
                if ( BaseInfo.ShouldOtherArchitravesAttackMe || BaseInfo.CivilWarEnemies.Count > 0 )
                    return; //don't build if we are in a civil war
                bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-ProduceSpawners-trace", 10f ) : null;

                List<SafeSquadWrapper> spawners = this.BaseInfo.Spawners.GetDisplayList();
                List<SafeSquadWrapper> pioneers = this.BaseInfo.Pioneers.GetDisplayList();
                for ( int i = 0; i < pioneers.Count; i++ )
                {
                    debugCode = 200;
                    entity = pioneers[i].GetSquad();
                    if ( entity == null )
                        continue;
                    pFaction = entity.PlanetFaction;
                    bool skipPlanet = false;
                    for ( int j = 0; j < spawners.Count; j++ )
                    {
                        if ( entity.Planet == spawners[j].Planet )
                        {
                            //don't build another spawner, we have have one
                            skipPlanet = true;
                            break;
                        }
                    }
                    if ( skipPlanet )
                        continue;
                    foreach ( GameEntity_Squad warpingSpawner in this.BaseInfo.WarpingInSpawners.DisplaySquads() )
                    {
                        if ( entity.Planet == warpingSpawner.Planet )
                        {
                            //don't build another spawner, we have have one
                            skipPlanet = true;
                            break;
                        }
                    }

                    if ( entity.Planet.GetControllingFactionType() == FactionType.Player ||
                         entity.Planet.GetControllingFactionType() == FactionType.AI &&
                         entity.Planet.GetControllingFaction().GetIsHostileTowards(AttachedFaction) )
                        skipPlanet = true;
                    if ( entity.Planet.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "ZenithArchitrave" )
                        skipPlanet = true; //if we are owned by another ZA, don't build here

                    if ( skipPlanet )
                        continue;

                    debugCode = 299;

                    debugCode = 300;
                    if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength <
                         pFaction.DataByStance[FactionStance.Self].TotalStrength / 3 )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "Spawning a spawner on " + entity.GetPlanetName_Safe() + ", pioneers path\n" );
                        ArcenPoint spawnLocation = entity.Planet.GetSafePlacementPoint_AroundEntity( Context, entityData, entity, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 150 ) );
                        GameEntity_Squad spawner = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                                               pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "ZA-Spawners" );  //is fine, main sim threa
                        if ( !BaseInfo.PlanetsEverTaken.Contains( spawner.Planet ) )
                            BaseInfo.PlanetsEverTaken.AddButRejectIfNull( spawner.Planet );
                        if ( entity.ExtraStackedSquadsInThis > 0 )
                            entity.AddOrSetExtraStackedSquadsInThis( -1, false );
                        else
                            entity.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );

                        spawner.TransformsIntoAfterTime = GameEntityTypeDataTable.Instance.GetRowByName( "ZenithArchitraveSpawner" ).InternalName;
                        spawner.SecondsTillTransformation = (Int16) (BaseInfo.Difficulty.BaseSpawnerWarpInTime + Context.RandomToUse.Next( 0, BaseInfo.Difficulty.SpawnerWarpInTimeVariance ) );

                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            if ( entity.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                            {
                                PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                                if ( chatHandlerOrNull != null )
                                    chatHandlerOrNull.PlanetToView = entity.Planet;

                                World_AIW2.Instance.QueueChatMessageOrCommand( "The " + AttachedFaction.StartFactionColourForLog() + "Architrave</color> has conquered " +
                                    entity.GetPlanetName_Safe(), ChatType.LogToCentralChat, chatHandlerOrNull );
                            }
                        }

                        //This is a DoubleBufferedConcurrentList thing.  It is threadsafe
                        BaseInfo.WarpingInSpawners.AddToDisplayList( spawner );

                        //spawn some extra defensive structures too
                        int turretsToSpawn = 2;
                        for ( int j = 0; j < turretsToSpawn; j++ )
                        {
                            entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ArchitraveDefensiveStructure" );
                            if ( entityData == null )
                                throw new Exception( "No ArchitraveDefensiveStructures defined in XML" );

                            spawnLocation = spawner.Planet.GetSafePlacementPoint_AroundEntity( Context, entityData, spawner, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 150 ) );
                            GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                        pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "ZA-Spawners" );  //is fine, main sim threa
                        }

                        foreach ( GameEntity_Squad defendingEntity in spawner.PlanetFaction.Entities.Squads() )
                        {
                            //all units on this planet will now guard the warping in spawner
                            //the LRP code will not assign any of them to new fireteams until the spawner is fully built
                            defendingEntity.FireteamId = -1;
                        }
                    }
                }
                debugCode = 400;
                //Iterate over all planets in our territory and see if we outnumber our enemies and
                //don't have a spawner
                for ( int i = 0; i < BaseInfo.Territory.Count; i++ )
                {
                    debugCode = 500;
                    Planet planet = BaseInfo.Territory[i];
                    pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                    bool skipPlanet = false;
                    debugCode = 510;
                    for ( int j = 0; j < spawners.Count; j++ )
                    {
                        debugCode = 520;
                        if ( planet == spawners[j].Planet )
                        {
                            debugCode = 530;
                            //don't build another spawner, we have have one
                            skipPlanet = true;
                            break;
                        }
                    }
                    if ( skipPlanet )
                    {
                        continue;
                    }

                    foreach ( GameEntity_Squad warpingSpawner in this.BaseInfo.WarpingInSpawners.DisplaySquads() )
                    {
                        debugCode = 520;
                        if ( planet == warpingSpawner.Planet )
                        {
                            debugCode = 530;
                            //don't build another spawner, we have have one
                            skipPlanet = true;
                            break;
                        }
                    }

                    if ( planet.GetControllingFaction().GetIsHostileTowards(AttachedFaction) )
                        skipPlanet = true; //make sure we kill the AI command station
                    if ( skipPlanet )
                        continue;

                    debugCode = 599;

                    debugCode = 600;
                    if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength <
                         (pFaction.DataByStance[FactionStance.Self].TotalStrength + pFaction.DataByStance[FactionStance.Friendly].TotalStrength) / 10 )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "Spawning a spawner on " + planet.Name + ", Territory path\n" );
                        debugCode = 610;
                        ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 150 ) );
                        GameEntity_Squad spawner = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                                               pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "ZA-Spawners" );  //is fine, main sim thread
                        if ( !BaseInfo.PlanetsEverTaken.Contains( spawner.Planet ) )
                            BaseInfo.PlanetsEverTaken.AddButRejectIfNull( spawner.Planet );

                        spawner.TransformsIntoAfterTime = GameEntityTypeDataTable.Instance.GetRowByName( "ZenithArchitraveSpawner" ).InternalName;

                        spawner.SecondsTillTransformation = (Int16) (BaseInfo.Difficulty.BaseSpawnerWarpInTime + Context.RandomToUse.Next( 0, BaseInfo.Difficulty.SpawnerWarpInTimeVariance ) );
                        debugCode = 620;
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            if ( planet.IntelLevel > PlanetIntelLevel.Unexplored )
                            {
                                PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                                if ( chatHandlerOrNull != null )
                                    chatHandlerOrNull.PlanetToView = planet;

                                World_AIW2.Instance.QueueChatMessageOrCommand( "The " + AttachedFaction.StartFactionColourForLog() + "Zenith Architrave</color> has conquered " + 
                                    planet.Name, ChatType.LogToCentralChat, chatHandlerOrNull );
                            }
                        }

                        //This is a DoubleBufferedConcurrentList thing.  It is threadsafe
                        BaseInfo.WarpingInSpawners.AddToDisplayList( spawner );

                    }
                }
                if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in ProduceSpawners. debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }

        public void ProduceShipsIfNecessary( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host
            //War Footing means Pioneers, Enemies in Territory or Civil War
            //Cases:
            //   No War Footing:
            //       If there's free Strength Space inside spawners, update the PerUnit data
            //   If in War Footing:
            //       Deploy all ships inside spawners
            //       If we are under strength for War Mode, build some ships

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-ProduceShipsIfNecessary-trace", 10f ) : null;
            int debugCode = 0;
            try
            {
                debugCode = 100;
                int maxAllowedStrength = BaseInfo.TotalAllowedStrengthInPeace;
                if ( BaseInfo.IsInWarFooting )
                    maxAllowedStrength = BaseInfo.TotalAllowedStrengthInWarFooting;
                debugCode = 200;
                //First create ships inside spawners
                ProduceRegularShips( maxAllowedStrength, Context );
                debugCode = 500;
                if ( BaseInfo.IsInWarFooting )
                {
                    debugCode = 600;
                    //deploy all ships inside spawners, update PerUnit data
                    ProduceGolems( maxAllowedStrength, Context );
                    List<SafeSquadWrapper> spawners = this.BaseInfo.Spawners.GetDisplayList();
                    for ( int i = 0; i < spawners.Count; i++ )
                    {
                        debugCode = 700;
                        GameEntity_Squad spawner = spawners[i].GetSquad();
                        if ( spawner == null )
                            continue;
                        PlanetFaction pFaction = spawner.Planet.GetPlanetFactionForFaction( AttachedFaction );
                        ZenithArchitravePerUnitBaseInfo data = spawner.CreateExternalBaseInfo<ZenithArchitravePerUnitBaseInfo>( "ZenithArchitravePerUnitBaseInfo" );

                        foreach ( KeyValuePair<GameEntityTypeData, int> kv in data.ShipsInside )
                        {
                            debugCode = 710;
                            if ( tracing )
                                tracingBuffer.Add( "\tIn war footing, deploying " + kv.Value + " " + kv.Key.GetDisplayName() + " from " + spawner.ToStringWithPlanet() + ".\n" );
                            for ( int j = 0; j < kv.Value; j++ )
                            {
                                ArcenPoint spawnLocation = spawner.Planet.GetSafePlacementPoint_AroundEntity( Context, kv.Key, spawner, FInt.FromParts( 0, 005 ), FInt.FromParts( 0, 030 ) );

                                GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, kv.Key, data.MarkLevelForShips,
                                                                                      pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "ZA-GeneralShips" );  //is fine, main sim thread
                                if ( entity != null )
                                {
                                    entity.ShouldNotBeConsideredAsThreatToHumanTeam = true;//this throws off the calculations
                                    entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                                }
                            }
                        }
                        data.ShipsInside.Clear();
                    }
                }
                debugCode = 800;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit error in ProduceShipsIfNecessary debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        
        public void ProduceRegularShips( int maxAllowedStrength, ArcenHostOnlySimContext Context )
        {
            //build normal ships. Ships are created inside the spawner
            //then the spawner will launch the ships if in War Footing
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-ProduceRegularShips-trace", 10f ) : null;

            int debugCode = 0;
            try
            {
                int shipsLeftToCreate = 20;
                debugCode = 100;
                while ( BaseInfo.MetalReserves > 0 &&
                        shipsLeftToCreate > 0 &&
                        ( BaseInfo.CurrentTotalStrength < maxAllowedStrength ||
                          BaseInfo.CurrentNonGolemStrength < maxAllowedStrength / 2 ) ) //if we have too much strength in golems, make normal ships too, since golems only hit things in multiphase
                {
                    debugCode = 300;
                    GameEntity_Squad spawner = GetSpawnerForNewShipsOrNull( Context);
                    if ( spawner == null )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\tCould not find a spawner to produce new ships.\n" );
                        break;
                    }
                    ZenithArchitravePerUnitBaseInfo data = spawner.CreateExternalBaseInfo<ZenithArchitravePerUnitBaseInfo>( "ZenithArchitravePerUnitBaseInfo" );
                    if ( tracing )
                        tracingBuffer.Add("\tWe are trying to create ships for " + spawner.ToStringWithPlanet() ).Add(". We have " + BaseInfo.MetalReserves + " metal.\n");
                    debugCode = 410;
                    //if this spawner has space put a ship in it, then update CurrentTotalStrength and MetalReserves
                    int percentSpireShip = 35;
                    GameEntityTypeData entityData = null;
                    if ( AttachedFaction.HasObtainedSpireDebris && Context.RandomToUse.Next( 0, 100 ) < percentSpireShip )
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, data.TagForShipsIncludingSpire );
                    else
                        entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, data.TagForShips );
                    debugCode = 420;
                    if ( entityData == null )
                        throw new Exception( "Null entityData in ProduceRegularShips. Tags " + data.TagForShips + ", " + data.TagForShipsIncludingSpire );
                    // if ( BaseInfo.MetalReserves < entityData.CostForAIToPurchase )
                    // {
                    //     if ( tracing )
                    //         tracingBuffer.Add("\t\tWe can't afford a " + entityData.GetDisplayName() + "; it costs " + entityData.CostForAIToPurchase +".\n");

                    //     shipsLeftToCreate--;
                    //     continue;
                    // }
                    debugCode = 430;
                    if ( data.ShipsInside.ContainsKey( entityData ) )
                        data.ShipsInside[entityData]++;
                    else
                        data.ShipsInside[entityData] = 1;
                    debugCode = 440;
                    GameEntityTypeData.MarkLevelStats markLevelStats = entityData.MarkStatsFor( data.MarkLevelForShips );
                    BaseInfo.CurrentTotalStrength += markLevelStats.StrengthPerSquad_CalculatedWithNullFleetMembership;

                    // in case we need strength per squad, here's how we get it
                    //                    GameEntityTypeData.MarkLevelStats markLevelStatsForMetalCost = entityData.MarkStatsFor( 1 );
                    //                    if ( BaseInfo.MetalReserves < markLevelStatsForMetalCost.StrengthPerSquad )

                    BaseInfo.MetalReserves -= entityData.CostForAIToPurchase;
                    debugCode = 450;
                    if ( tracing )
                        tracingBuffer.Add( "\tCreating a " + entityData.GetDisplayName() + " on " + spawner.GetPlanetName_Safe() + ". There's " + BaseInfo.MetalReserves + " metal left. This ship cost " + entityData.CostForAIToPurchase + " metal.\n" );
                    if ( BaseInfo.MetalReserves <= 0 || (BaseInfo.CurrentTotalStrength < maxAllowedStrength &&
                                                           shipsLeftToCreate-- > 0) )
                        break;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in ProduceRegularShips debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        private static readonly List<SafeSquadWrapper> WorkingRandomSpawnerList = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ZenithArchitraveFactionDeepInfo-WorkingRandomSpawnerList" );
        public GameEntity_Squad GetSpawnerForNewShipsOrNull(ArcenHostOnlySimContext Context )
        {
            WorkingRandomSpawnerList.Clear();
            List<SafeSquadWrapper> spawners = this.BaseInfo.Spawners.GetDisplayList();
            for ( int i = 0; i < spawners.Count; i++ )
            {
                GameEntity_Squad spawner = spawners[i].GetSquad();
                if ( spawner == null )
                    continue;
                ZenithArchitravePerUnitBaseInfo data = spawner.TryGetExternalBaseInfoAs<ZenithArchitravePerUnitBaseInfo>();
                int allowedStrengthInSpawner = BaseInfo.GetAllowedPeaceStrengthForSpawner( spawner, BaseInfo.Difficulty );
                if ( data.GetStrengthInside() > allowedStrengthInSpawner )
                    continue;
                WorkingRandomSpawnerList.Add(spawner);
            }
            if ( WorkingRandomSpawnerList.Count == 0 )
                return null;
            return WorkingRandomSpawnerList[Context.RandomToUse.Next(0, WorkingRandomSpawnerList.Count)].GetSquad();
        }
        public void ProduceGolems( int maxAllowedStrength, ArcenHostOnlySimContext Context )
        {
            //Golems!
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-ProduceGolems-trace", 10f ) : null;

            int debugCode = 0;
            try
            {
                if ( BaseInfo.Portal.Display.GetSquad() == null )
                {
                    return;
                }
                int shipsLeftToCreate = 10;
                debugCode = 100;
                while ( BaseInfo.GolemMetalReserves > 0 &&
                        shipsLeftToCreate > 0 &&
                        ( BaseInfo.CivilWarEnemies.Count != 0 ||
                          BaseInfo.CurrentTotalStrength < maxAllowedStrength )) //if we are not in civil war mode, our golems are capped by CurrentTotalStrength
                {
                    debugCode = 300;
                    GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ArchitraveWarGolem" );
                    // if ( tracing )
                    //     tracingBuffer.Add("Considering building " + entityData.GetDisplayName() + " at cost " + entityData.CostForAIToPurchase + " current reserves " + BaseInfo.GolemMetalReserves + "\n");
                    if ( BaseInfo.GolemMetalReserves < entityData.CostForAIToPurchase )
                    {
                        shipsLeftToCreate--;
                        continue;
                    }
                    BaseInfo.GolemMetalReserves -= entityData.CostForAIToPurchase;
                    debugCode = 430;
                    if ( tracing )
                        tracingBuffer.Add( "\tCreating a " + entityData.GetDisplayName() + " on " + BaseInfo.Portal.Display.GetPlanetName_Safe() + ". There's " + BaseInfo.GolemMetalReserves + " metal left. This ship cost " + entityData.CostForAIToPurchase + " metal.\n" );
                    PlanetFaction pFaction = BaseInfo.Portal.Display.PlanetFaction;
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                     pFaction.Faction.LooseFleet, 0, BaseInfo.Portal.Display.WorldLocation, Context, "ZA-Golems" );  //is fine, main sim thread
                    newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true;//this throws off the calculations
                    newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread

                    if ( BaseInfo.GolemMetalReserves <= 0 ||
                         shipsLeftToCreate-- > 0 )
                        break;

                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in ProduceGolems debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }

        }
        

        public void UpdateSpawnersAndTurrets( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host
            List<SafeSquadWrapper> spawners = this.BaseInfo.Spawners.GetDisplayList();
            //Spawners can level up and build turrets
            for ( int i = 0; i < spawners.Count; i++ )
            {
                GameEntity_Squad spawner = spawners[i].GetSquad();
                if ( spawner == null )
                    continue;
                int upgradeTime = BaseInfo.GetSpawnerUpgradeTime( spawner );
                if ( upgradeTime == -1 )
                    continue;
                if ( spawner.CurrentMarkLevel >= 7 )
                    continue; //caps at 7
                if ( World_AIW2.Instance.GameSecond > upgradeTime )
                {
                    //Upgrade this, and maybe give it some turrets too?
                    spawner.SetCurrentMarkLevel( (byte)(spawner.CurrentMarkLevel + 1) );
                    if ( spawner.CurrentMarkLevel >= 2 && spawner.CurrentMarkLevel <= 4 )
                    {
                        ZenithArchitravePerUnitBaseInfo data = spawner.GetExternalBaseInfoAs<ZenithArchitravePerUnitBaseInfo>();
                        data.TagForShips = "ArchitraveTierOne";
                        data.TagForShipsIncludingSpire = "ArchitraveTierOneWithSpire";
                        data.MarkLevelForShips++;
                    }
                    else if ( spawner.CurrentMarkLevel > 4 )
                    {
                        ZenithArchitravePerUnitBaseInfo data = spawner.GetExternalBaseInfoAs<ZenithArchitravePerUnitBaseInfo>();
                        data.TagForShips = "ArchitraveTierTwo";
                        data.TagForShipsIncludingSpire = "ArchitraveTierTwoWithSpire";
                        data.MarkLevelForShips++;
                    }
                    else if ( spawner.CurrentMarkLevel > 6 )
                    {
                        ZenithArchitravePerUnitBaseInfo data = spawner.GetExternalBaseInfoAs<ZenithArchitravePerUnitBaseInfo>();
                        data.MarkLevelForShips++;
                        data.TagForShips = "ArchitraveTierThree";
                        data.TagForShipsIncludingSpire = "ArchitraveTierThreeWithSpire";
                    }
                    PlanetFaction pFaction = spawner.Planet.GetPlanetFactionForFaction( AttachedFaction );
                    int turretsToSpawn = 2;
                    for ( int j = 0; j < turretsToSpawn; j++ )
                    {
                        GameEntityTypeData entityData = GetNextZATurretToSpawn(spawner.Planet, Context);
                        if ( entityData == null )
                            throw new Exception( "No ArchitraveDefensiveStructures defined in XML" );

                        ArcenPoint spawnLocation = spawner.Planet.GetSafePlacementPoint_AroundEntity( Context, entityData, spawner, FInt.FromParts( 0, 050 ), FInt.FromParts( 0, 150 ) );
                        GameEntity_Squad newTurret = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, 1,
                                                                                                      pFaction.Faction.LooseFleet, 0, spawnLocation, Context, "ZA-UpdateSpawners" );  //is fine, main sim threa
                        if ( newTurret.TypeData.GetHasTag("ArchitraveCastra") )
                        {
                            //This is a DoubleBufferedConcurrentList thing.  It is threadsafe
                            BaseInfo.Castra.AddToDisplayList( newTurret );
                        }
                    }

                }
            }
        }
        public GameEntityTypeData GetNextZATurretToSpawn(Planet planet, ArcenHostOnlySimContext Context )
        {
            GameEntityTypeData data;
            int retries = 10;
            do{
                //at most one castra per planet. Castras may develop other rules someday, we'll see...
                data = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ArchitraveDefensiveStructure" );
                if ( data.GetHasTag("ArchitraveCastra") )
                {
                    foreach ( GameEntity_Squad castra in this.BaseInfo.Castra.DisplaySquads() )
                    {
                        if ( castra.Planet == planet )
                        {
                            data = null;
                            break;
                        }
                    }
                }
            }while ( data == null && retries-- > 0);
            return data;
        }
        public void UpdateTerritory( ArcenHostOnlySimContext Context )
        {
            //if at max territory, noop
            //If we don't have enemies in our territory right now and
            //we aren't at max territory and we don't have  TimeForNextPlanetInTerritory
            //   Set TimeForNextPlanetInTerritory
            //If we are past TimeForNextPlanetInTerritory,
            //   Unset TimeForNextPlanetinTerritory
            //   Add a new planet to territory
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-UpdateTerritory-trace", 10f ) : null;

            if ( BaseInfo.IsQuiesced || BaseInfo.IsInWarFooting || BaseInfo.Territory.Count >= BaseInfo.MaxTerritorySize )
            {
                if ( tracing )
                    tracingBuffer.Add( AttachedFaction.GetDisplayName() + " index " + AttachedFaction.FactionIndex + ". Not allowed to expand territory. War footing " + BaseInfo.IsInWarFooting + " quiesced " + BaseInfo.IsQuiesced + " and territory " + BaseInfo.Territory.Count + "\n" );
                if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return;
            }
            //we know we're not quiesced or in war mode now, and we are below MaxTerritorySize
            if ( BaseInfo.TimeForNextPlanetInTerritory == -1 && BaseInfo.Spawners.Count == BaseInfo.Territory.Count )
            {
                //only add new territory once we've conquered our existing territory
                BaseInfo.TimeForNextPlanetInTerritory = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.TimeBetweenTerritoryIncrease;
                if ( AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue( "DebugMode", false ) )
                    BaseInfo.TimeForNextPlanetInTerritory = World_AIW2.Instance.GameSecond + 10; //debug mode

                if ( tracing )
                    tracingBuffer.Add( "Next planet will be added to territory at " + BaseInfo.TimeForNextPlanetInTerritory + " seconds in (now " + World_AIW2.Instance.GameSecond + " + " + BaseInfo.Difficulty.TimeBetweenTerritoryIncrease + ")\n" );

            }
            if ( BaseInfo.TimeForNextPlanetInTerritory != -1 &&
                 World_AIW2.Instance.GameSecond >= BaseInfo.TimeForNextPlanetInTerritory )
            {
                Planet newPlanet = GetNextPlanetInTerritory( Context );
                if ( newPlanet == null )
                {
                    //we've run out of planets to expand to, so we're done here. Maybe we're stuck on the end of a snake map or something
                    if ( tracing )
                        tracingBuffer.Add( "No planets to expand to; we're done expanding.\n" );

                    BaseInfo.MaxTerritorySize = BaseInfo.Territory.Count;
                    if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    return;
                }
                if ( tracing )
                    tracingBuffer.Add( "Adding planet " + newPlanet.Name + " to territory at " + World_AIW2.Instance.GameSecond + " seconds in. Trigger time " + BaseInfo.TimeForNextPlanetInTerritory + "\n" );
                BaseInfo.Territory.AddButRejectIfNull( newPlanet ); //we will update the Territory structure from TerritoryIdx next sim-step
                BaseInfo.TimeForNextPlanetInTerritory = -1;
            }
            if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }

        public readonly List<Planet> PossibleExpansionPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "ZenithArchitraveFactionDeepInfo-PossibleExpansionPlanets" );
        public readonly List<Planet> PreferredExpansionPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "ZenithArchitraveFactionDeepInfo-PreferredExpansionPlanets" );
        private Planet GetNextPlanetInTerritory( ArcenHostOnlySimContext Context )
        {
            //called by UpdateTerritory
            //Rules are
            //  must be adjacent to existing territory
            //  can't be a King planet or adjacent to a King planet
            //  prefer a planet adjacent to our home spawner
            //  prefer the weakest planet
            PossibleExpansionPlanets.Clear();
            PreferredExpansionPlanets.Clear();
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-GetNextPlanetInTerritory-trace", 10f ) : null;
            if ( tracing )
                tracingBuffer.Add( "GetNextPlanetInTerritory: " );

            List<SafeSquadWrapper> spawners = this.BaseInfo.Spawners.GetDisplayList();
            for ( int i = 0; i < spawners.Count; i++ )
            {
                GameEntity_Squad spawner = spawners[i].GetSquad();
                if ( spawner == null )
                    continue;
                if ( spawner.Planet.HasPlanetBeenDestroyed )
                    continue;
                if ( tracing )
                    tracingBuffer.Add( " Checking spawner " + spawner.ToStringWithPlanet() + " for likely neighbors.\n" );
                foreach ( Planet neighbor in spawner.Planet.LinkedNeighbors( false ) )
                {
                    if ( tracing )
                        tracingBuffer.Add( "\tChecking " + neighbor.Name + ".\n" );
                    if ( neighbor.IsPlanetToBeDestroyed || neighbor.HasPlanetBeenDestroyed )
                        continue;
                    if ( FactionUtilityMethods.Instance.IsPlanetNearKing( neighbor, 2, true ) )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\tDiscarding " + neighbor.Name + ", too near a stationary king.\n" );
                        continue;
                    }
                    if ( neighbor.TypeData.Type == PlanetType.Nomad && !World_AIW2.Instance.CurrentGalaxy.IsNomadGalaxy )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\tDiscarding " + neighbor.Name + ", its a nomad.\n" );
                        continue;
                    }
                    if ( neighbor.PopulationType == PlanetPopulationType.AIBastionWorld )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\tDiscarding " + neighbor.Name + ", its a bastion.\n" );
                        continue;
                    }
                    for ( int j = 0; j < spawners.Count; j++ )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\tChecking spawner " + spawners[j].ToStringWithPlanet() + " to see if it overlaps\n" );

                        if ( spawners[j].Planet == neighbor )
                            continue;
                    }
                    if ( BaseInfo.Territory.Contains( neighbor ) )
                        continue;
                    if ( neighbor.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "ZenithArchitrave" )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\t\tRemoving " + neighbor.Name + " since it is owned by another ZA\n" );
                        continue;
                    }
                    if ( neighbor.GetControllingOrInfluencingFaction().SpecialFactionData.ExcludeFromZATerritory )
                    {
                        //This is mostly for Dyson-style factions
                        if ( tracing )
                            tracingBuffer.Add( "\t\t\tRemoving " + neighbor.Name + " since it is owned by an invalid faction: " + neighbor.GetControllingOrInfluencingFaction().GetDisplayName() ).Add("\n");
                        continue;
                    }
                    for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
                    {
                        Faction playerfaction = World_AIW2.Instance.Factions[j];
                        if ( playerfaction.Type != FactionType.Player )
                            continue;
                        bool foundSpireCity = false;
                        foreach ( GameEntity_Squad entity in playerfaction.Squads( EntityRollupType.CityCenter ) )
                        {
                            if ( !entity.TypeData.GetHasTag( "SpireCity" ) )
                                continue;
                            if ( entity.Planet == neighbor )
                            {
                                foundSpireCity = true;
                                break;
                            }
                        }
                        if ( foundSpireCity )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "\t\t\tRemoving " + neighbor.Name + " since it has a Spire City\n" );
                            continue;
                        }
                    }

                    if ( tracing )
                        tracingBuffer.Add( "\t\t\tAdding " + neighbor.Name + " to possible\n" );
                    PossibleExpansionPlanets.Add( neighbor );

                    if ( FactionUtilityMethods.Instance.IsPlanetNearKing( neighbor, 2, false ) )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\tDiscarding " + neighbor.Name + ", too near a mobile king.\n" );
                        continue;
                    }

                    if ( spawner.TypeData.GetHasTag( "ZenithArchitravePortal" ) )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\t\t\tAdding " + neighbor.Name + " to preferred\n" );
                        PreferredExpansionPlanets.Add( neighbor );
                    }
                }
            }
            if ( PossibleExpansionPlanets.Count == 0 )
                return null;

            for ( int i = PossibleExpansionPlanets.Count - 1; i >= 0; i-- )
            {
                //make sure no other Architrave has this planet
                //this is a bit heavy of a loop, but it's called infrequently and
                //there should never be that many architraves
                Planet thisPlanet = PossibleExpansionPlanets[i];
                bool foundOverlap = false;
                for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[j];
                    if ( otherFaction == AttachedFaction ||
                         otherFaction.SpecialFactionData.InternalName != "ZenithArchitrave" )
                        continue;
                    ZenithArchitraveFactionBaseInfo otherData = otherFaction.TryGetExternalBaseInfoAs<ZenithArchitraveFactionBaseInfo>();
                    if ( otherData.Territory.Contains( thisPlanet ) )
                    {
                        foundOverlap = true;
                        break;
                    }
                }
                if ( foundOverlap )
                    PossibleExpansionPlanets.Remove( thisPlanet );
            }
            if ( PossibleExpansionPlanets.Count == 0 )
                return null;

            if ( PreferredExpansionPlanets.Count > 0 )
            {
                cb_zaDeepFaction = AttachedFaction;
                PreferredExpansionPlanets.Sort( static delegate ( Planet Left, Planet Right )
                {
                    int leftHostileStrength = Left.GetPlanetFactionForFaction( cb_zaDeepFaction ).DataByStance[FactionStance.Hostile].TotalStrength;
                    int rightHostileStrength = Right.GetPlanetFactionForFaction( cb_zaDeepFaction ).DataByStance[FactionStance.Hostile].TotalStrength;
                    return leftHostileStrength.CompareTo( rightHostileStrength );
                } );
                if ( tracing ) tracingBuffer.Add( "\tChoosing preferred\n" );
                return PreferredExpansionPlanets[0];
            }
            cb_zaDeepFaction = AttachedFaction;
            PossibleExpansionPlanets.Sort( static delegate ( Planet Left, Planet Right )
            {
                int leftHostileStrength = Left.GetPlanetFactionForFaction( cb_zaDeepFaction ).DataByStance[FactionStance.Hostile].TotalStrength;
                int rightHostileStrength = Right.GetPlanetFactionForFaction( cb_zaDeepFaction ).DataByStance[FactionStance.Hostile].TotalStrength;
                if ( Left.GetControllingOrInfluencingFaction().Type == FactionType.Player &&
                     Right.GetControllingOrInfluencingFaction().Type != FactionType.Player )
                    return 1;
                if ( Right.GetControllingOrInfluencingFaction().Type == FactionType.Player &&
                     Left.GetControllingOrInfluencingFaction().Type != FactionType.Player )
                    return -1;

                return leftHostileStrength.CompareTo( rightHostileStrength );
            } );
            if ( tracing )
            {
                tracingBuffer.Add( "\tChoosing possible from list: \n" );
                for ( int i = 0; i < PossibleExpansionPlanets.Count; i++ )
                {
                    tracingBuffer.Add( "\t" ).Add( PossibleExpansionPlanets[i].Name ).Add( "\n" );
                }
            }
            return PossibleExpansionPlanets[0];
        }
        public bool CheckForWarFooting( ArcenSimContextAnyStatus Context )
        {
            //This checks for A. civil war
            //B. Pioneers (or warping in spawners)
            //C. Enemies in territory (note that here "territory" means "planets with spawners", since pioneers might expanded us

            //Also updates IsBelowMaxTerritory

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-CheckForWarFooting-trace", 10f ) : null;

            if ( BaseInfo.Territory.Count < BaseInfo.MaxTerritorySize )
                BaseInfo.IsBelowMaxTerritory = true;
            else
                BaseInfo.IsBelowMaxTerritory = false;

            if ( BaseInfo.IsQuiesced )
            {
                if ( tracing )
                    tracingBuffer.Add( "We are Quiesced\n" );
                if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return false;
            }

            List<SafeSquadWrapper> pioneers = this.BaseInfo.Pioneers.GetDisplayList();
            if ( pioneers.Count > 0 || BaseInfo.CivilWarEnemies.Count > 0 )
            {
                if ( tracing )
                    tracingBuffer.Add( "We are in war footing, path A. Pioneers: " + pioneers.Count + " civil war enemies " + BaseInfo.CivilWarEnemies.Count + "\n" );
                if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return true;
            }
            if ( BaseInfo.WarpingInSpawners.Count > 0 )
            {
                if ( tracing )
                    tracingBuffer.Add( "We are in war footing, path B. WarpingInSpawners: " + BaseInfo.WarpingInSpawners.Count +"\n" );
                if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return true;
            }

            if ( BaseInfo.ShouldOtherArchitravesAttackMe )
            {
                if ( tracing )
                    tracingBuffer.Add( "We are in war footing, civil war path A1\n" );
                if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return true;
            }
            List<SafeSquadWrapper> spawners = this.BaseInfo.Spawners.GetDisplayList();
            for ( int i = 0; i < spawners.Count; i++ )
            {
                Planet planet = spawners[i].Planet;
                PlanetFaction pFaction = spawners[i].Planet.GetPlanetFactionForFaction( AttachedFaction );
                if ( !Context.IsLongRangePlanning )
                {
                    if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > pFaction.DataByStance[FactionStance.Self].TotalStrength / 100 ||
                         pFaction.DataByStance[FactionStance.Hostile].TotalStrength >= 2000 )
                    {
                        if ( tracing )
                        {
                            tracingBuffer.Add( "War footing check B, on " + planet.Name + ".. enemy strength " + pFaction.DataByStance[FactionStance.Hostile].TotalStrength + " my strength " + pFaction.DataByStance[FactionStance.Self].TotalStrength + "." );
                            tracingBuffer.Add( " we are in war footing\n" );
                        }
                        if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                        if ( tracing )
                        {
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }
                        return true;
                    }
                }
                else
                {
                    EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( AttachedFaction );
                    StrengthData_PlanetFaction_Stance selfData = myFactionData[FactionStance.Self];
                    StrengthData_PlanetFaction_Stance hostileData = myFactionData[FactionStance.Hostile];
                    if ( hostileData.TotalStrength > selfData.TotalStrength / 100 )
                    {
                        if ( tracing )
                        {
                            tracingBuffer.Add( "War footing check C, on " + planet.Name + ".\n" );
                            tracingBuffer.Add( " we are in war footing\n" );
                        }
                        if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                        if ( tracing )
                        {
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }
                        return true;
                    }
                }
            }
            if ( BaseInfo.WarpingInSpawners.Count > 0 )
            {                
                if ( tracing )
                {
                    Planet planet = null;
                    foreach ( GameEntity_Squad warpingSpawner in this.BaseInfo.WarpingInSpawners.DisplaySquads() )
                    {
                        planet = warpingSpawner.Planet;
                        break;
                    }
                    tracingBuffer.Add( "War footing check B1, warping in spawner on " + planet.Name + ".." );
                    tracingBuffer.Add( " we are in war footing\n" );
                }
                if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return true;
            }

            for ( int i = 0; i < BaseInfo.Territory.Count; i++ )
            {
                Planet planet = BaseInfo.Territory[i];
                if ( planet.GetControllingOrInfluencingFaction().GetIsHostileTowards(AttachedFaction ) )
                {
                    if ( tracing )
                    {
                        tracingBuffer.Add( "War footing check E1, on " + planet.Name + " an enemy faction owns me." );
                        tracingBuffer.Add( " we are in war footing\n" );
                    }
                    if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    return true;
                }
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                if ( !Context.IsLongRangePlanning )
                {
                    if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > pFaction.DataByStance[FactionStance.Self].TotalStrength / 5 )
                    {
                        if ( tracing )
                        {
                            tracingBuffer.Add( "War footing check E2, on " + planet.Name + ". enemy strength " + pFaction.DataByStance[FactionStance.Hostile].TotalStrength + " my strength " + pFaction.DataByStance[FactionStance.Self].TotalStrength / 5 + "." );
                            tracingBuffer.Add( " we are in war footing\n" );
                        }
                        if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                        if ( tracing )
                        {
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }
                        return true;
                    }
                    bool foundEnemyGuardPost = false;
                    foreach ( GameEntity_Squad guardPost in planet.Squads( EntityRollupType.ReinforcementLocations ) )
                    {
                        switch (guardPost.TypeData.SpecialType)
                        {
                            case SpecialEntityType.GuardPost:
                            case SpecialEntityType.DireGuardPost:
                                break;
                            default:
                                continue; //Data centers and other things can also be reinforcement locations
                        }
                        if ( guardPost.GetIsHostileTowards_Safe( AttachedFaction ) )
                        {
                            foundEnemyGuardPost = true;
                            break;
                        }
                    }
                    if ( foundEnemyGuardPost )
                    {
                        if ( tracing )
                        {
                            tracingBuffer.Add( "War footing check E3, enemy guard post detected." );
                            tracingBuffer.Add( " we are in war footing\n" );
                        }
                        if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                        if ( tracing )
                        {
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }
                        return true;
                    }
                }
                else
                {
                    EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( AttachedFaction );
                    StrengthData_PlanetFaction_Stance selfData = myFactionData[FactionStance.Self];
                    StrengthData_PlanetFaction_Stance hostileData = myFactionData[FactionStance.Hostile];
                    if ( hostileData.TotalStrength > selfData.TotalStrength / 3 )
                    {
                        if ( tracing )
                        {
                            tracingBuffer.Add( "War footing check F, on " + planet.Name + ".\n" );
                            tracingBuffer.Add( " we are in war footing\n" );
                        }
                        if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                        if ( tracing )
                        {
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }

                        return true;
                    }
                    bool foundEnemyGuardPost = false;
                    foreach ( GameEntity_Squad guardPost in planet.Squads( EntityRollupType.ReinforcementLocations ) )
                    {
                        if ( guardPost.GetIsHostileTowards_Safe( AttachedFaction ) )
                        {
                            foundEnemyGuardPost = true;
                            break;
                        }
                    }
                    if ( foundEnemyGuardPost )
                    {
                        if ( tracing )
                        {
                            tracingBuffer.Add( "War footing check G, enemy guard post detected." );
                            tracingBuffer.Add( " we are in war footing\n" );
                        }
                        if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                        if ( tracing )
                        {
                            tracingBuffer.ReturnToPool();
                            tracingBuffer = null;
                        }

                        return true;
                    }

                }
            }
            if ( tracing )
                tracingBuffer.Add( "We are not in war footing, LRP " + Context.IsLongRangePlanning + "\n" );
            if ( tracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }

            return false;
        }

        //Long Range Planning (LRP) starts here

        public static readonly List<SafeSquadWrapper> UnassignedShips = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ZenithArchitraveFactionDeepInfo-UnassignedShips" );
        public static readonly DictionaryOfLists<Planet, SafeSquadWrapper> UnassignedShipsByPlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 60, "ZenithArchitraveFactionDeepInfo-UnassignedShipsByPlanet" );
        public static readonly List<SafeSquadWrapper> Ships = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ZenithArchitraveFactionDeepInfo-Ships" );
        public static readonly List<SafeSquadWrapper> LRPSpawners = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ZenithArchitraveFactionDeepInfo-LRPSpawners" );
        public static readonly List<SafeSquadWrapper> LRPSpawnersWithSpace = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ZenithArchitraveFactionDeepInfo-LRPSpawnersWithSpace" );
        public static readonly List<SafeSquadWrapper> LRPWarpingInSpawners = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ZenithArchitraveFactionDeepInfo-LRPWarpingInSpawners" );
        public static readonly List<Planet> LRPWarpingInSpawnerPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "ZenithArchitraveFactionDeepInfo-LRPWarpingInSpawnerPlanets" );

        public static readonly List<SafeSquadWrapper> LRPPioneers = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ZenithArchitraveFactionDeepInfo-LRPPioneers" );

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            int debugCode = 0;
            PerFactionPathCache pathingCacheData = null;
            bool tracing = false;
            bool fireteamTracing = false;
            ArcenCharacterBuffer tracingBuffer = null;
            try
            {
                debugCode = 5;
                if ( this.BaseInfo == null || this.BaseInfo.Territory.Count == 0 )
                    return; //if we aren't initialized, don't do anything
                 #region Tracing
                debugCode = 10;
                tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
                fireteamTracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam ) && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
                tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
                if ( tracing &&  tracingBuffer == null )
                    ArcenDebugging.ArcenDebugLogSingleLine("we are confused about the tracing buffer; its null but tracing is on", Verbosity.DoNotShow );
                debugCode = 20;
                #endregion
                pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();

                //first iterate over and clean up stale data (ships list and strength)
                debugCode = 100;
                UnassignedShips.Clear();
                UnassignedShipsByPlanet.Clear();
                Ships.Clear();
                LRPSpawners.Clear();
                LRPWarpingInSpawners.Clear();
                LRPWarpingInSpawnerPlanets.Clear();
                LRPSpawnersWithSpace.Clear();
                LRPPioneers.Clear();
                TeamsAimedAtPlanet.Clear();
                debugCode = 200;
                if ( tracing )
                    tracingBuffer.Add( "ZA " + AttachedFaction.FactionIndex + "\n" );
                foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
                {
                    debugCode = 300;
                    team.DeepInfo.Reset();
                }
                debugCode = 400;
                //its possible for this to get triggered by units retreating from a civil war,
                //so if that keeps happening then you should increase the time for ZA units to retreat
                FactionUtilityMethods.Instance.FlushUnitsFromReinforcementPointsOnAllRelevantPlanets( AttachedFaction, Context, 5f );
                if ( !BaseInfo.IsInWarFooting )
                {
                    debugCode = 500;
                    //The war footing path is pretty unique; everyone goes back to a spawner, no fireteams
                    if ( tracing && tracingBuffer != null )
                        tracingBuffer.Add( "ZA " + AttachedFaction.FactionIndex + " No longer in war footing; everyone go back to a spawner and chill if there are no enemies on the planet" );
                    foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
                    {
                        //disband all fireteams, we're at peace
                        FireteamUtility.DisbandFireteam( this.AttachedFaction, BaseInfo.Teams, team, Context );
                    }

                    ReturnAllShipsToSpawners( Context, pathingCacheData );
                    #region Tracing
                    if ( tracing || fireteamTracing ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " " + AttachedFaction.FactionIndex + " " ).Add( " Long Range Planning trace ends at " ).Add( World_AIW2.Instance.GameSecond ).Add( ", Peace Path\n" );
                    if ( tracing || fireteamTracing ) { ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow ); }
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    if ( pathingCacheData != null )
                        pathingCacheData.ReturnToPool();
                    #endregion
                    return;
                }
                debugCode = 600;
                //If we are not in pace, iterate over all our units and make some lists
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "WarpingInZenithArchitraveSpawner" ) )
                {
                    debugCode = 700;
                    LRPWarpingInSpawners.Add( entity );
                    LRPWarpingInSpawnerPlanets.Add( entity.Planet );
                }
                debugCode = 800;
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "ZenithArchitraveSpawner" ) )
                {
                    debugCode = 900;
                    LRPSpawners.Add( entity );
                    ZenithArchitravePerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<ZenithArchitravePerUnitBaseInfo>();
                    if ( data != null && data.GetStrengthInside() < BaseInfo.GetAllowedPeaceStrengthForSpawner( entity, BaseInfo.Difficulty ) )
                    {
                        debugCode = 1000;
                        LRPSpawnersWithSpace.Add( entity );
                    }
                }
                debugCode = 1100;
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    debugCode = 1200;
                    if ( !entity.TypeData.IsMobile )
                        continue;
                    if ( entity.TypeData.GetHasTag( "ArchitravePioneer" ) )
                    {
                        LRPPioneers.Add( entity );
                        if ( BaseInfo.ShouldOtherArchitravesAttackMe || BaseInfo.CivilWarEnemies.Count > 0 )
                            continue; //in civil war, pioneers are despawned
                    }
                    debugCode = 1300;
                    if ( entity.FireteamId < 0 )
                    {
                        debugCode = 1400;
                        var factionData = entity.Planet.GetStanceDataForFaction( AttachedFaction );
                        if ( factionData != null )
                        {
                            StrengthData_PlanetFaction_Stance hostileStrengthData = factionData[FactionStance.Hostile];
                            if ( hostileStrengthData.TotalStrength > 0 )
                                continue; //if we are on a histole planet, don't bother joining a fireteam just fight
                        }
                        debugCode = 1500;
                        if ( LRPWarpingInSpawnerPlanets.Count > 0 &&
                             LRPWarpingInSpawnerPlanets.Contains( entity.Planet ) &&
                             !entity.TypeData.GetHasTag( "ArchitravePioneer" ) )
                        {
                            //we are on a planet with a warping in spawner, so do not join a fireteam (we are defending).
                            //Pioneers are exempt; they can continue to attack (since often there are other ships that aren't defending)
                            continue;
                        }
                        debugCode = 1600;
                        UnassignedShips.Add( entity );
                        UnassignedShipsByPlanet[entity.Planet].Add( entity );
                    }
                    else
                    {
                        debugCode = 1700;
                        Fireteam team = FireteamBaseUtility.GetFireteamById( BaseInfo.Teams, entity.FireteamId );
                        if ( team == null ) entity.FireteamId = -1; //something happened to the fireteam, so lets find a new one next LRP stage
                        else
                            team.DeepInfo.AddUnit( entity );
                    }
                }
                debugCode = 1800;
                RemovePioneersDuringCivilWar_LRP( Context, pathingCacheData ); //if we are in civil war, this will return the pioneers to spawners
                                                                               //This is the War Footing Path; we returned early from all other paths. 
                FireteamUtility.UpdateFireteams( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, FInt.One );
                FireteamUtility.UpdateRegiments( AttachedFaction, Context, pathingCacheData, BaseInfo.Teams, TeamsAimedAtPlanet, tracingBuffer, AttachedFaction.MinFireteamStrength, true );
                debugCode = 1900;
                for ( int i = 0; i < UnassignedShips.Count; i++ )
                    AssignUnitToFireteam( UnassignedShips[i].GetSquad(), Context, pathingCacheData );
                if ( AttachedFaction.NumFireteams != BaseInfo.Teams.GetItemCount() )
                    AttachedFaction.NumFireteams = BaseInfo.Teams.GetItemCount();
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit Exception in ZA LRP. debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            finally
            {
                if ( pathingCacheData != null )
                    pathingCacheData.ReturnToPool();
                #region Tracing
                //the tracingBuffer can be null, since we might be coming from the return; statement in the Pace path
                if ( tracing && tracingBuffer != null ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " " + AttachedFaction.FactionIndex ).Add( " LongRangePlanning trace ends at " ).Add( World_AIW2.Instance.GameSecond ).Add( " War Path.\n" );
                if ( tracing && tracingBuffer != null ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing && tracingBuffer != null )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }

        }
        public void RemovePioneersDuringCivilWar_LRP( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( BaseInfo.CivilWarEnemies.Count == 0 )
                return;
            for ( int i = 0; i < LRPPioneers.Count; i++ )
            {
                GameEntity_Squad entity = LRPPioneers[i].GetSquad();
                if ( entity == null )
                    continue;

                if ( entity.Orders != null &&
                     entity.Orders.GetQueuedOrderCount() > 0 )
                    continue;
                EntityOrder order = entity.Orders.GetLastQueuedOrder_OrNull();
                if ( order.TypeData != null )
                {
                    if ( order.TypeData.Type == EntityOrderType.Wormhole )
                        continue;
                }

                SendShipToSpawner( entity, Context, PathCacheData );
            }
        }
        public void HandlePlayerAntagonizedArchitraveLRP( ArcenLongTermIntermittentPlanningContext Context )
        {
            //This faction was originally a variant of the Dyson Sphere, so it had some logic for the Antagonizer in
            //this is no longer a thing, but keeping the logic just in case I might want to do something like that again

            //All Architrave Ships just bum rush the antagonizer from wherever they are
            //for ( int i = 0; i < UnassignedShipsByPlanet.Count; i++ )
            // {
            //     KeyValuePair<Planet, List<SafeSquadWrapper> > pair = UnassignedShipsByPlanet.GetPairByIndex( MetaDatai );
            //     //Check if we are on an antagonizer planet; if so, focus down the antagonizer.
            //     //Note this allows for multiple antagonizers
            //     bool onAntagonizerPlanet = false;
            //     for ( int j = 0; j < LRPPlayerAntagonizers.Count; j++ )
            //     {
            //         if ( pair.Key == LRPPlayerAntagonizers[j].Planet )
            //         {
            //             //We are on an antagonizer planet, so try to kill that antagonizer.
            //             onAntagonizerPlanet = true;
            //             break;
            //         }
            //     }
            //     if ( onAntagonizerPlanet )
            //         continue; //just fight everything. Hopefully we're in Attacker_Full already
            //     //We are not on an antagonizer planet;
            //     //check if we have any ships on this planet without orders to fly to an antagonizer.
            //     for ( int j = 0; j < LRPPlayerAntagonizers.Count; j++ )
            //     {
            //         bool foundShipWithoutTarget = false;
            //         for ( int k = 0; k < pair.Value.Count; k++ )
            //         {
            //             if ( pair.Value[k].CalculateNextHopPlanetIndex_Safe() < 0 )
            //                 foundShipWithoutTarget = true;
            //         }
            //         if ( foundShipWithoutTarget )
            //         {
            //             List<Planet> pathToTarget = List<Planet>.Create_WillNeverBeGCed( 500 );
            //             pathToTarget = faction.FindPath( pair.Key, LRPPlayerAntagonizers[0].Planet, Context );

            //             GameCommand command = GameCommand.Create ( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
            //             command.RelatedString = "ArchitraveAntagonizer";
            //             for ( int k = 0; k < pair.Value.Count; k++ )
            //                 command.RelatedEntityIDs.Add( pair.Value[k].PrimaryKeyID );
            //             for ( int k = 0; k < pathToTarget.Count; k++ )
            //                 command.RelatedIntegers.Add( pathToTarget[k].Index );
            //             World_AIW2.Instance.QueueGameCommand( command, false );
            //         }
            //     }
        }
        public void ReturnAllShipsToSpawners( ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            //If this ship is out of combat on a planet, recall it to a spawner.
            //This isn't the most efficient code, but it's run infrequently (only immediately after a war)
            foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
            {
                if ( entity.TypeData.GetHasTag( "ZenithArchitraveSpawner" ) )
                {
                    ZenithArchitravePerUnitBaseInfo data = entity.TryGetExternalBaseInfoAs<ZenithArchitravePerUnitBaseInfo>();
                    if ( data.GetStrengthInside() < BaseInfo.GetAllowedPeaceStrengthForSpawner( entity, BaseInfo.Difficulty ) )
                        LRPSpawners.Add( entity ); //make sure we have space
                    continue;
                }
                if ( !entity.TypeData.IsMobile )
                    continue;
                //if ( entity.CalculateNextHopPlanetIndex_Safe() != entity.GetPlanetIndexSafe() ) //we are a ship that's not going anywhere at the moment
                //   continue;
                var factionData = entity.Planet.GetStanceDataForFaction( AttachedFaction );
                StrengthData_PlanetFaction_Stance hostileStrengthData = factionData[FactionStance.Hostile];
                if ( entity.Orders != null &&
                     entity.Orders.GetQueuedOrderCount() > 0 )
                {
                    EntityOrder order = entity.Orders.GetLastQueuedOrder_OrNull();
                    if ( order.TypeData != null && order.TypeData.Type == EntityOrderType.Wormhole )
                    {
                        //so we are en route to a planet; also check that the target planet is "Home"
                        Planet destplanet = World_AIW2.Instance.GetPlanetByIndex( order.RelatedPlanetIndex );
                        for ( int i = 0; i < LRPSpawners.Count; i++ )
                        {
                            if ( LRPSpawners[i].Planet == destplanet )
                            {
                                continue; //if going somewhere, keep going
                            }
                        }
                    }
                }

                Ships.Add( entity );

            }
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-ReturnAllShipsToSpawners-trace", 10f ) : null;
            if ( tracing )
                tracingBuffer.Add( "There are " + LRPSpawners.Count + " spawners and " + Ships.Count + " ships to be put inside them\n" );
            for ( int i = 0; i < Ships.Count; i++ )
            {
                SendShipToSpawner( Ships[i].GetSquad(), Context, PathCacheData );
            }
        }

        private void SendShipToSpawner( GameEntity_Squad entity, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( entity == null )
                return;
            GameEntity_Squad spawner = null;
            //int closestHops = 999;
            int debugCode = 0;
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-SendShipToSpawner-trace", 10f ) : null;
            if ( LRPSpawners.Count == 0 )
                return; //we have no spawners. Unlikely?

            try
            {
                debugCode = 100;
                //first try going to a spawner with space; if none can be found, just go to a random spawner
                if ( LRPSpawnersWithSpace.Count > 0 )
                {
                    for ( int i = 0; i < LRPSpawnersWithSpace.Count; i++ )
                    {
                        debugCode = 200;
                        debugCode = 100;
                        if ( LRPSpawnersWithSpace[i].Planet == entity.Planet )
                        {
                            debugCode = 300;
                            spawner = LRPSpawnersWithSpace[i].GetSquad();
                            if ( spawner != null )
                                break;
                        }
                    }
                    debugCode = 400;
                    if ( spawner == null )
                        spawner = LRPSpawnersWithSpace[Context.RandomToUse.Next( 0, LRPSpawnersWithSpace.Count )].GetSquad();
                }
                if ( spawner == null )
                {
                    //okay, no room in the inn. To the stable with you!
                    debugCode = 500;
                    for ( int i = 0; i < LRPSpawners.Count; i++ )
                    {
                        debugCode = 100;
                        if ( LRPSpawners[i].Planet == entity.Planet )
                        {
                            spawner = LRPSpawners[i].GetSquad();
                            if ( spawner != null )
                                break;
                        }
                    }
                }
                debugCode = 600;
                if ( spawner == null )
                    spawner = LRPSpawners[Context.RandomToUse.Next( 0, LRPSpawners.Count )].GetSquad();
                debugCode = 700;
                int range = 1000;
                //we could batch these for performance if desired
                if ( entity.Planet == spawner.Planet &&
                     Mat.DistanceBetweenPointsImprecise( entity.WorldLocation, spawner.WorldLocation ) > range )
                {
                    if ( tracing )
                        tracingBuffer.Add( entity.ToStringWithPlanet() + " is going to " + spawner.ToStringWithPlanet() + ", same planet path\n" );

                    debugCode = 800;
                    GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
                    moveCommand.PlanetOrderWasIssuedFrom = entity.Planet.Index;
                    moveCommand.RelatedPoints.Add( spawner.WorldLocation );
                    moveCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
                }
                else
                {
                    if ( tracing )
                        tracingBuffer.Add( entity.ToStringWithPlanet() + " is going to " + spawner.ToStringWithPlanet() + ", distant planet path\n" );

                    debugCode = 900;
                    PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "ZenithArchitraveSendShipToSpawner", entity.Planet, spawner.Planet, PathingMode.Default, Context, PathCacheData );
                    if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                    {
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
                        debugCode = 1000;
                        command.RelatedString = "MilDys_GoToSpawner";
                        command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                        command.ToBeQueued = false;
                        for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in SendShipToSpawner debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
        }

        //prefer close ones that you can get to safely
        private readonly ArcenLessLinkedList<Fireteam> AvailableFireteams = ArcenLessLinkedList<Fireteam>.Create_WillNeverBeGCed( "ZenithArchitraveFactionDeepInfo-AvailableFireteams" );
        private void AssignUnitToFireteam( GameEntity_Squad entity, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( entity == null )
                return;

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam ) && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-AssignUnitToFireteam-trace", 10f ) : null;

            AvailableFireteams.Clear();
            //bool debug = false;
            if ( this.BaseInfo.Teams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                team.StrengthToBringOnline = AttachedFaction.MinFireteamStrength + Context.RandomToUse.Next( 0, AttachedFaction.MaxFireteamStrength - AttachedFaction.MinFireteamStrength );
                if ( BaseInfo.Pioneers.Count <= 0 && BaseInfo.CivilWarEnemies.Count == 0 )
                    team.SuicideMission = true;
                this.BaseInfo.Teams.AddIfNotAlreadyIn( team );
            }
            int maxHops = 10;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( BaseInfo.Teams ) )
            {
                if ( team.status == FireteamStatus.Disbanded ||
                        team.status == FireteamStatus.ReadyToAttack ||
                        team.status == FireteamStatus.Attacking )
                    continue; //if a fireteam is ready to fight, don't send more ships to that team
                if ( team.status == FireteamStatus.Staging && //if a fireteam is already "pretty strong", don't give it more
                     team.DeepInfo.TeamStrength > AttachedFaction.MaxFireteamStrength )
                    continue;
                if ( team.DeepInfo.TeamStrength > AttachedFaction.MaxFireteamStrength * 2 && team.DeepInfo.ShipsInFireteam.Count > 20 ) //if this is much stronger than usual, don't make it even stronger
                    continue;

                if ( team.DeepInfo.CurrentPlanet == null )
                {
                    //this is a brand new fireteam, so it's totally safe.
                    AvailableFireteams.AddIfNotAlreadyIn( team );
                    continue;
                }
                if ( AvailableFireteams.GetItemCount() >= 4 ) //if we already have a lot of possible fireteams to use, don't keep looking
                    break;

                Int16 hops = 0;
                int dangerOfTeam = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, entity.Planet, team.DeepInfo.CurrentPlanet, true, out hops );
                if ( dangerOfTeam < 40000 ) //let units wander through pretty dangerous spots (40 strength)
                {
                    if ( hops < maxHops )
                        AvailableFireteams.AddIfNotAlreadyIn( team );
                }
            }


            //Base algorithm: if any of our "safe" fireteams are "below strength" then just add to one of those fireteams.
            //If all our fireteams are "Strong Enough" then randomly choose to reinforce an existing one or create a new one
            //We require "safe teams" for the case where it's an octopus map with a Spawner cut off from the rest of the galaxy

            bool stopProcesssing = false;
            foreach ( Fireteam team in Fireteam.LiveTeamsIn( AvailableFireteams ) )
            {
                if ( team.DeepInfo.TeamStrength < team.StrengthToBringOnline )
                {
                    team.DeepInfo.AddUnit( entity );
                    team.DeepInfo.IdentifyCurrentPlanet(); //just in case this unit is the first unit or something
                    entity.FireteamId = team.FireTeamID;
                    if ( tracing )
                        tracingBuffer.Add( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path A\n" );
                    stopProcesssing = true;
                    break;
                }
            }
            if ( stopProcesssing )
                return;

            int percentNewTeam = 40;
            if ( AvailableFireteams.GetItemCount() >= 3 )
                percentNewTeam = 0;
            if ( Context.RandomToUse.Next( 0, 100 ) < percentNewTeam || AvailableFireteams.GetItemCount() == 0 )
            {
                Fireteam team = Fireteam.CreateNewWithIDFromList( BaseInfo.Teams );
                team.StrengthToBringOnline = AttachedFaction.MinFireteamStrength + Context.RandomToUse.Next( 0, AttachedFaction.MaxFireteamStrength - AttachedFaction.MinFireteamStrength );
                if ( BaseInfo.Pioneers.Count <= 0 && BaseInfo.CivilWarEnemies.Count == 0 )
                    team.SuicideMission = true;
                int numShipsForConcentratingEfforts = 5; //before we're too strong, best to concentrate our forces
                if ( BaseInfo.Teams.GetItemCount() < numShipsForConcentratingEfforts )
                    team.PercentBestTarget = 100;
                else if ( this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget > 0 )
                    team.PercentBestTarget = this.AttachedFaction.SpecialFactionData.FireteamPercentBestTarget;

                else
                    team.PercentBestTarget = 65;
                team.PercentDistanceBestTarget = 45; //marauders often get far-flung empires
                team.PreferredMaxDistance = 5;
                team.DeepInfo.AddUnit( entity );
                entity.FireteamId = team.FireTeamID;

                team.DeepInfo.IdentifyCurrentPlanet(); //in case this unit is the first unit
                BaseInfo.Teams.AddIfNotAlreadyIn( team );
                if ( tracing )
                    tracingBuffer.Add( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path B: new fireteam. Percent New Fireateam: " + percentNewTeam + "\n" );
                return;
            }

            {
                Fireteam team = AvailableFireteams.GetRandom( Context.RandomToUse );
                team.DeepInfo.AddUnit( entity );
                entity.FireteamId = team.FireTeamID;
                if ( tracing )
                    tracingBuffer.Add( "Adding " + entity.ToString() + " to fireteam " + team.FireTeamID + " path C\n" );
                team.DeepInfo.IdentifyCurrentPlanet(); //just in case this unit is the first unit or something
            }
            if ( tracing )
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );
        }

        public override GameEntity_Squad GetFireteamRetreatPoint_OnBackgroundNonSimThread_Subclass( Planet CurrentPlanetForFireteam, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            int currentDanger = -1;
            GameEntity_Squad retreatPoint = null;
            for ( int i = 0; i < LRPSpawners.Count; i++ )
            {
                GameEntity_Squad spawner = LRPSpawners[i].GetSquad();
                if ( spawner == null )
                    continue;
                Int16 hops = 0;
                int danger = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForFireteam, spawner.Planet, true, out hops );
                if ( danger < currentDanger || currentDanger == -1 )
                {
                    retreatPoint = spawner;
                    currentDanger = danger;
                }
                if ( danger == 0 )
                    break;
            }
            return retreatPoint;
        }

        public override void GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass( bool DefenseMode, Planet CurrentPlanetForFireteam, 
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, List<FireteamTarget> PreferredTargets, List<FireteamTarget> FallbackTargets, object TeamObj )
        {
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam )&& Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-GetFireteamPreferredAndFallbackTargets_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            FInt falloffForDistance = FInt.FromParts( 0, 050 );
            Fireteam team = (Fireteam)TeamObj;
            
            if ( BaseInfo.CivilWarEnemies.Count > 0 )
                GetPreferredCivilWarArchitraveTargets( PreferredTargets, team, Context );

            if ( PreferredTargets.Count == 0 )
                GetPreferredArchitraveTargets( PreferredTargets, team, Context );
            //We do a two-stage check here. First we cull the targets whose defenses are much stronger than usual.
            //then we sort targets by how hard it is to get there
            //Currently we don't do fallback targets
            if ( FallbackTargets != null || FallbackTargets.Count == 0 )
            {
                FallbackTargets.Sort( static delegate ( FireteamTarget Left, FireteamTarget Right )
                {
                    int lDifficulty = Left.dangerOfPath;
                    int rDifficulty = Right.dangerOfPath;
                    return lDifficulty.CompareTo( rDifficulty );
                } );
            }

            bool debug = false;
            if ( debug && tracing )
            {
                tracingBuffer.Add( "Getting lurk/target Preferred Targets\n" );
                for ( int i = 0; i < PreferredTargets.Count; i++ )
                    tracingBuffer.Add( "\t" ).Add( PreferredTargets[i].GetPlanetName_Safe() ).Add( " difficulty " ).Add( PreferredTargets[i].dangerOfPath ).Add( " \n" );
                tracingBuffer.Add( "Getting lurk/target Fallback Targets\n" );
                for ( int i = 0; i < FallbackTargets.Count; i++ )
                    tracingBuffer.Add( "\t" ).Add( FallbackTargets[i].GetPlanetName_Safe() ).Add( "\n" );
            }
            if ( tracing )
                FactionUtilityMethods.Instance.FinishTracing( tracingBuffer );

        }
        public void GetPreferredCivilWarArchitraveTargets( List<FireteamTarget> ListToFill, Fireteam team, ArcenLongTermIntermittentPlanningContext Context )
        {
            ListToFill.Clear();
            for ( int i = 0; i < BaseInfo.CivilWarEnemies.Count; i++ )
            {
                Faction faction = World_AIW2.Instance.GetFactionByIndex( BaseInfo.CivilWarEnemies[i] );
                if ( faction == null )
                    continue;
                if ( !faction.GetIsHostileTowards( AttachedFaction ) ) //this shouldn't be possible, but just in case
                    continue;

                foreach ( GameEntity_Squad entity in faction.Squads( EntityRollupType.GrantsMinorFactionPlanetControl ) )
                {
                    if ( Fireteam.IsThisAWinningBattle( AttachedFaction, Context, entity.Planet, 2 ) )
                         continue;  //if we are already attacking and comfortably winning, don't bother sending more units
                     ListToFill.Add( new FireteamTarget( entity ) );
                 }
            }
            
        }
        public void GetPreferredArchitraveTargets( List<FireteamTarget> ListToFill, Fireteam team, ArcenLongTermIntermittentPlanningContext Context )
        {
            ListToFill.Clear();
            bool TerritoryOnly = false;
            if ( LRPPioneers.Count == 0 && BaseInfo.CivilWarEnemies.Count == 0 )
                TerritoryOnly = true;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null )
                    continue;
                if ( !otherFaction.GetIsHostileTowards( AttachedFaction ) )
                    continue;
                if ( BaseInfo.CivilWarEnemies.Count > 0 &&
                     !BaseInfo.CivilWarEnemies.Contains( otherFaction.FactionIndex ) )
                    continue; //if in civil war, only target other Architraves

                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.GrantsMinorFactionPlanetControl ) )
                {
                    //can go after minor factions
                    if ( Fireteam.IsThisAWinningBattle( AttachedFaction, Context, entity.Planet, 2 ) )
                         continue;  //if we are already attacking and comfortably winning, don't bother sending more units
                    if ( TerritoryOnly && !BaseInfo.IsPlanetInTerritory( entity.Planet ) )
                         continue;
                     ListToFill.Add( new FireteamTarget( entity ) );
                 }
                foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.CommandStation ) )
                {
                     if ( Fireteam.IsThisAWinningBattle( AttachedFaction, Context, entity.Planet, 2 ) )
                         continue; //if we are already attacking and comfortably winning, don't bother sending more units
                    if ( TerritoryOnly && !BaseInfo.IsPlanetInTerritory( entity.Planet ) )
                         continue;

                     ListToFill.Add( new FireteamTarget( entity.Planet ) );
                 }
            }
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                Faction controller = planet.GetControllingOrInfluencingFaction();
                if ( TerritoryOnly )
                {
                    //only fight in our territory (or other planets we've conquered)
                    if ( controller != AttachedFaction && !BaseInfo.Territory.Contains( planet ) )
                        continue;
                }
                if ( team.DeepInfo.ShouldPrioritizeUndefendedPlanets && planet.GetControllingOrInfluencingFaction() != AttachedFaction &&
                     Fireteam.IsThisAWinningBattle( AttachedFaction, Context, planet, 2 ) ) //if we have pioneers then we like winning battles

                    ListToFill.Add( new FireteamTarget( planet ) );
                if ( Fireteam.IsThisAWinningBattle( AttachedFaction, Context, planet, 2 ) )
                    continue;
                if ( planet.GetControllingFactionType() == FactionType.NaturalObject )
                {
                    Faction influencingFaction = World_AIW2.Instance.GetFactionByIndex( planet.PrimaryInfluencingFaction );
                    if ( influencingFaction != null && BaseInfo.CivilWarEnemies.Count > 0 &&
                         !BaseInfo.CivilWarEnemies.Contains( influencingFaction.FactionIndex ) )
                        continue; //if in civil war, only target other Architraves

                    if ( influencingFaction == null || !influencingFaction.GetIsFriendlyTowards( AttachedFaction ) )
                        ListToFill.Add( new FireteamTarget( planet ) );
                }
                if ( controller == AttachedFaction )
                {
                    //are there enemies attacking us?
                    EnumIndexedArray<FactionStance,StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( AttachedFaction );
                    int hostileStrength = myFactionData[FactionStance.Hostile].TotalStrength;
                    int myStrength = myFactionData[FactionStance.Self].TotalStrength +
                        myFactionData[FactionStance.Friendly].TotalStrength;
                    if ( hostileStrength > myStrength / 2 )
                        ListToFill.Add( new FireteamTarget( planet ) );
                }
            }
        }
        public override Planet GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass( Planet TargetPlanet, int TeamStrength, Planet CurrentPlanetForTeam, 
            ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            Planet bestPlanet = null;
            int dangerOfPathFromBestPlanet = -1;
            int distanceFromBestPlanet = 9999;
            Int16 hopsFromBestPlanet = 9999;
            int unused = 0;
            //this logic partially cribbed from IndependentAIFleet.cs::Helper_DoTargetFindingSweep

            if ( TargetPlanet == null )
                throw new Exception( "No target planet set in get lurk planet?!" );
            //int debugCode = 0;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Fireteam ) && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.ZenithArchitrave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "ZA-GetFireteamLurkPlanet_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            bool preferUnwatchedPlanets = false;
            if ( tracing )
                tracingBuffer.Add( "Getting a lurk planet. Target planet " + TargetPlanet.Name ).Add( ". " ).Add( AttachedFaction.BaseInfo.Allegiance ).Add( "\n" );
            foreach ( Planet.PlanetAtHopDistance _phd in TargetPlanet.PlanetsWithinXHops_NoFilters( -1 ) )
            {
                Planet planet = _phd.Planet;
                Int16 Distance = _phd.Hops;
                if ( planet == TargetPlanet )
                    continue;

                int planetDefensiveStrength = Fireteam.GetPlanetDefensiveStrength( planet, AttachedFaction, true, ref unused,
                                                                          FInt.Zero, FInt.Zero );
                if ( planet.GetControllingFaction().GetIsHostileTowards( AttachedFaction ) && planetDefensiveStrength > TeamStrength / 10 )
                    continue;

                //Don't path through any particularly dangerous planets
                Int16 hops = 0;
                int totalDifficultyOfPathToLurkPlanet = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, CurrentPlanetForTeam, planet, true, out hops );
                if ( totalDifficultyOfPathToLurkPlanet >= TeamStrength * 5 ) //as long as they only outnumber us 5:1, let's go!
                    continue;

                int totalDifficultyOfPathToTarget = Fireteam.GetDangerOfPath( AttachedFaction, Context, PathCacheData, planet, TargetPlanet, true, out hops );
                if ( preferUnwatchedPlanets && planet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
                    totalDifficultyOfPathToTarget *= 2; //penalized watched planets if desired

                if ( totalDifficultyOfPathToTarget < 0 )
                    totalDifficultyOfPathToTarget = 0; //pathing through allied planets is basically the same

                if ( tracing )
                    tracingBuffer.Add( "\tConsidering " + planet.Name + " danger of path " + totalDifficultyOfPathToTarget + " distance " + Distance + " intel " + planet.IntelLevel ).Add( "\n" );

                if ( dangerOfPathFromBestPlanet > totalDifficultyOfPathToTarget ||
                     dangerOfPathFromBestPlanet == -1 )
                {
                    if ( tracing )
                        tracingBuffer.Add( "\t" + planet.Name + " is now the lurk location; path A" ).Add( "\n" );
                    dangerOfPathFromBestPlanet = totalDifficultyOfPathToTarget;
                    distanceFromBestPlanet = Distance;
                    hopsFromBestPlanet = hops;
                    bestPlanet = planet;
                }

                if ( dangerOfPathFromBestPlanet == totalDifficultyOfPathToTarget &&
                     (distanceFromBestPlanet > Distance ||
                       hopsFromBestPlanet > hops) )
                {
                    if ( tracing )
                        tracingBuffer.Add( "\t" + planet.Name + " is now the lurk location; path B" ).Add( "\n" );
                    dangerOfPathFromBestPlanet = totalDifficultyOfPathToTarget;
                    distanceFromBestPlanet = Distance;
                    hopsFromBestPlanet = hops;
                    bestPlanet = planet;
                }

                if ( hopsFromBestPlanet <= 3 && dangerOfPathFromBestPlanet <= TeamStrength / 10 )
                    break; //we found a good lurk within easy striking distance of the planet, so exit now
            }
            return bestPlanet;
        }
    }
}
