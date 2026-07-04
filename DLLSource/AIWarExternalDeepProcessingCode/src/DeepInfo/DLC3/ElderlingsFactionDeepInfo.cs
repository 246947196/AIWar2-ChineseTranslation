using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /*  Overview
        The elderlings patrol small areas of the galaxy. Then they lay eggs, which make more elderlings that patrol nearby regions

     */
    public sealed class ElderlingsFactionDeepInfo : ExternalFactionDeepInfoRoot
    {
        //Set immediately before the WorkingPlanets sorts so the comparisons can be non-capturing
        //static delegates.  [ThreadStatic] because faction planning runs on background threads.
        [ThreadStatic] private static Dictionary<Planet, int> cb_elderlingsPerPlanet;
        [ThreadStatic] private static Dictionary<Planet, int> cb_eggsPerPlanet;
        [ThreadStatic] private static Faction cb_elderlingsDeepFaction;
        public ElderlingsFactionBaseInfo BaseInfo;
        private static readonly List<Planet> InvadablePlanets = List<Planet>.Create_WillNeverBeGCed( 500, "ElderlingsFactionDeepInfo-InvadablePlanets" );
        private static readonly List<Planet> WorkingPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "ElderlingsFactionDeepInfo-WorkingPlanets" );
        private static readonly List<SafeSquadWrapper> WorkingEntities = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 20, "ElderlingsFactionDeepInfo-WorkingEntities" );
        //private int LastTimeUpdatedTerritory = 0; //We want to limit how often we check for territory updates for an elderling; this should be done immediately after load, and its not worth serializing
        public int TerritoryUpdateInterval = 30;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<ElderlingsFactionBaseInfo>();
        }

        protected override void Cleanup()
        {
            BaseInfo = null;

            //not likely to matter much
            InvadablePlanets.Clear();
            ElderlingsLRP.Clear();
            PossiblePlanetsLRP.Clear();
            WorkingPlanets.Clear();
            WorkingEntities.Clear();
            //LastTimeUpdatedTerritory = 0;
        }

        protected override int MinimumSecondsBetweenLongRangePlannings => 10;

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host. I think a lot of what the miners do is just not client 

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Elderlings );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Elderling-DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly-trace", 10f ) : null;
            if ( this.BaseInfo.TimeLastSpawnedMaddenedEgg == -1 && this.BaseInfo.AIAllied &&
                 NecromancerEmpireFactionBaseInfo.GetNecromancerFactionCount() > 0 )
                this.BaseInfo.TimeLastSpawnedMaddenedEgg = this.BaseInfo.Difficulty.MaddenedEggInterval;

            //If we have no presence on the map, periodically rejoin our allies
            JoinAlliesIfNecessary( Context );
            InitializeOrUpdateTerritory( Context );
            HatchEggs( Context );
            HandleUnitMarkups( Context );
            HandleEggLaying( Context );
            HandleElderlingSanity( Context );
            HandleJournals( Context );
            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }
        
        public void HandleElderlingSanity( ArcenHostOnlySimContext Context )
        {
            WorkingPlanets.Clear();
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Elderlings );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Elderling-HandleElderlingSanity-trace", 10f ) : null;

            //First make sure the sanity numbers are initialized
            List<SafeSquadWrapper> elderlings = this.BaseInfo.Elderlings.GetDisplayList();
            for ( int i = 0; i < elderlings.Count; i++ )
            {
                GameEntity_Squad ship = elderlings[i].GetSquad();
                if ( ship == null )
                    continue;
                ElderlingsPerUnitBaseInfo data = ship.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();
                if ( data.SanityRemaining == -1 )
                {
                    data.SanityRemaining = this.BaseInfo.Difficulty.StartingSanity;
                }
                if ( ship.Orders.Behavior != EntityBehaviorType.Attacker_Full )
                    ship.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
            }

            //Now reduce sanity if/where appropriate
            GameCommand transferCommand = null;
            foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in BaseInfo.ElderlingsOnPlanet )
            {
                elderlings = pair.Value;
                if ( elderlings.Count < this.BaseInfo.Difficulty.ElderlingsToTriggerSanityLoss )
                    continue;
                if ( tracing )
                    tracingBuffer.Add( "Enough elderlings on " + pair.Key.Name + " to trigger sanity losses\n" );
                bool foundHighTier = false;
                bool foundMidTier = false;
                for ( int i = 0; i < elderlings.Count; i++ )
                {
                    if ( elderlings[i].TypeData.GetHasTag( "HighElderling" ) )
                    {
                        foundHighTier = true;
                        foundMidTier = true;
                        break;
                    }
                    if ( elderlings[i].TypeData.GetHasTag( "MedElderling" ) )
                    {
                        foundMidTier = true;
                    }
                }
                for ( int i = 0; i < elderlings.Count; i++ )
                {
                    GameEntity_Squad elderling = elderlings[i].GetSquad();
                    if ( elderling == null )
                        continue;
                    ElderlingsPerUnitBaseInfo data = elderling.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();
                    if ( data.SuicideMode )
                        continue; //we're already mad!
                    if ( elderling.TypeData.GetHasTag( "LowElderling" ) )
                    {
                        if ( foundHighTier )
                            data.SanityRemaining -= 2;
                        if ( foundMidTier )
                            data.SanityRemaining--;
                    }
                    if ( elderling.TypeData.GetHasTag( "MedElderling" ) )
                    {
                        if ( foundHighTier )
                            data.SanityRemaining--;
                    }
                    if ( data.SanityRemaining <= 0 )
                    {
                        //We either join the Maddened Faction (which means we fight everything at random)
                        //or we just fly off to attack this faction's enemies
                        bool joinMaddenedFaction = Context.RandomToUse.Next( 0, 100 ) < 50;
                        if ( data.SanityRemaining < -100 || //this happens if we were always intended to be mad from the hatch
                             BaseInfo.PlayerAllied)
                            joinMaddenedFaction = false;

                        data.SanityRemaining = -1;//we use PosExceptNeg1 for serializing, so just set it to -1

                        if ( joinMaddenedFaction )
                        {
                            Faction maddenedFaction = FactionUtilityMethods.Instance.GetMaddenedElderlingsFaction();
                            if ( transferCommand == null )
                            {
                                transferCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransferEntitiesToFaction], GameCommandSource.AnythingElse );
                                transferCommand.RelatedFactionIndex = maddenedFaction.FactionIndex;
                            }
                            transferCommand.RelatedEntityIDs.Add( elderling.PrimaryKeyID );
                            elderling.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread
                        }
                        else
                            data.SuicideMode = true; //this elderling is going to go rampage
                    }
                }
            }
            if ( transferCommand != null )
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, transferCommand, false );
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }

        public void InitializeOrUpdateTerritory( ArcenHostOnlySimContext Context )
        {
            List<SafeSquadWrapper> elderlings = this.BaseInfo.Elderlings.GetDisplayList();

            for ( int i = 0; i < elderlings.Count; i++ )
            {
                GameEntity_Squad elderling = elderlings[i].GetSquad();
                if ( elderling == null )
                    continue;
                ElderlingsPerUnitBaseInfo data = elderling.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
                if ( data.GetsAFreeTerritoryIfPossible )
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine("We get a free territory if possible!", Verbosity.DoNotShow );
                    UpdateTerritoryIfPossible( 1, elderling, Context, true );
                    data.GetsAFreeTerritoryIfPossible = false;
                }
                
                if ( data.Territory.Count < this.BaseInfo.Difficulty.BaseElderlingTerritorySize )
                {
                    //We are below the minimum cap (this shouldn't be lower than 3); this case is important for
                    //Minor Faction Allied Elderligns to make sure they can expand a bit
                    int planetsToAdd = this.BaseInfo.Difficulty.BaseElderlingTerritorySize - data.Territory.Count;
                    UpdateTerritoryIfPossible( planetsToAdd, elderling, Context, false );
                    continue;
                }
            }
        }

        public void HandleEggLaying( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host

            //bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Elderlings );
            //ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate("Elderling-HandleEggLaying-trace", 10f ) : null;
            int debugCode = 00;
            try
            {
                debugCode = 100;
                List<SafeSquadWrapper> elderlings = this.BaseInfo.Elderlings.GetDisplayList();
                GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ElderlingEgg" );
                if ( entityData == null )
                    throw new Exception( "Could not find egg in XML" );

                for ( int i = 0; i < elderlings.Count; i++ )
                {
                    debugCode = 200;
                    GameEntity_Squad elderling = elderlings[i].GetSquad();
                    if ( elderling == null )
                        continue;
                    ElderlingsPerUnitBaseInfo data = elderling.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
                    if ( data.SuicideMode )
                        continue; //suicidal elderlings just suicide; no more eggs
                    debugCode = 300;
                    if ( data.NextEggLayingTime > World_AIW2.Instance.GameSecond )
                        continue;

                    if ( data.PlanetForEgg == null )
                    {
                        data.PlanetForEgg = GetPlanetForEgg( elderling, Context ); //this function can return null and that's fine, it just leaves the egg planet unset
                        if ( data.PlanetForEgg != null )
                            BaseInfo.EggsPerPlanet[data.PlanetForEgg]++;
                        continue;
                    }
                    if ( data.PlanetForEgg != elderling.Planet )
                        continue; //we're not on the right planet, so nothing to do
                    if ( data.PlanetForEgg != null && elderling.Planet == data.PlanetForEgg &&
                         data.LocationToBuild == ArcenPoint.ZeroZeroPoint )
                    {
                        data.LocationToBuild = elderling.Planet.GetSafePlacementPoint_AroundEntity( Context, entityData, elderling, FInt.FromParts( 0, 250 ), FInt.FromParts( 0, 650 ) );
                        if ( data.LocationToBuild == ArcenPoint.ZeroZeroPoint )
                            ArcenDebugging.ArcenDebugLogSingleLine("failed to find someplace to build an egg for " + elderling.ToStringWithPlanetAndOwner(), Verbosity.DoNotShow );
                        continue;
                    }
                    int range = 200;
                    if ( elderling.Planet == data.PlanetForEgg &&
                         data.LocationToBuild != ArcenPoint.ZeroZeroPoint &&
                         Mat.DistanceBetweenPointsImprecise( elderling.WorldLocation, data.LocationToBuild ) < range )
                    {
                        debugCode = 400;
                        PlanetFaction pFaction = elderling.PlanetFaction;
                        GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, (byte)1,
                                                                                                      pFaction.Faction.LooseFleet, 0, elderling.WorldLocation, Context, "ElderlingEgg" );
                        newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true; //elderlings units aren't threat
                        ElderlingsPerUnitBaseInfo newData = newEntity.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
                        newData.HatchTime = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.BaseEggHatchingInterval;
                        if ( this.BaseInfo.PlayerAllied )
                            newData.TrackedByPlayer = true; //free tracking for your buddies!
                        if ( BaseInfo.ExtraStrongMode )
                            newData.HatchTime = World_AIW2.Instance.GameSecond + 40;

                        data.NextEggLayingTime = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.BaseEggSpawningInterval;
                        if ( BaseInfo.ExtraStrongMode )
                            data.NextEggLayingTime = World_AIW2.Instance.GameSecond + 180;
                        data.LocationToBuild = ArcenPoint.ZeroZeroPoint;
                        data.PlanetForEgg = null;
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in LayEggs debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
        }
        public Planet GetPlanetForEgg( GameEntity_Squad elderling, ArcenHostOnlySimContext Context )
        {
            if ( elderling == null )
                return null;
            //Finds a suitable planet for an egg

            ElderlingsPerUnitBaseInfo data = elderling.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
            if ( data == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "No elderlings per unit base info on " + elderling.ToStringWithPlanet(), Verbosity.DoNotShow );
                return null;
            }
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Elderlings );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Elderling-GetPlanetForEgg-trace", 10f ) : null;

            if ( tracing )
                tracingBuffer.Add( "Attempting to find a place for " + elderling.ToStringWithPlanet() + " to lay an egg" );

            WorkingPlanets.Clear();
            for ( int i = 0; i < data.Territory.Count; i++ )
            {
                Planet territory = data.Territory[i];
                int numElderlings = BaseInfo.ElderlingsPerPlanet[territory];
                if ( BaseInfo.ElderlingsPerPlanet[territory] + BaseInfo.EggsPerPlanet[territory] >= BaseInfo.Difficulty.MaxElderlingsPerPlanet )
                {
                    if ( tracing )
                        tracingBuffer.Add( "\tDiscarding " + territory.Name + " due to having " + BaseInfo.ElderlingsPerPlanet[territory] + " elderligs and " + BaseInfo.EggsPerPlanet[territory] + " eggs\n" );
                    continue;
                }
                PlanetFaction pFaction = territory.GetPlanetFactionForFaction( AttachedFaction );
                int hostileStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                if ( hostileStrength > 600 )
                {
                    if ( tracing )
                        tracingBuffer.Add( "\tDiscarding " + territory.Name + " due to enemy strength\n" );
                    continue;
                }
                if ( tracing )
                    tracingBuffer.Add( "\t\tAdding " + territory.Name + " to the consideration list for egg laying. It is already in the territory of " + BaseInfo.ElderlingsPerPlanet[territory] + " elderlings (and " + BaseInfo.EggsPerPlanet[territory] + " eggs), and hostile strength is " + hostileStrength + ".\n" );

                WorkingPlanets.Add( territory );
            }
            if (WorkingPlanets.Count == 0)
            {
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                return null;
            }
            cb_elderlingsPerPlanet = BaseInfo.ElderlingsPerPlanet;
            cb_eggsPerPlanet = BaseInfo.EggsPerPlanet;
            WorkingPlanets.Sort( static delegate ( Planet Left, Planet Right )
            {
                int leftTerritory = cb_elderlingsPerPlanet[Left] + cb_eggsPerPlanet[Left];
                int rightTerritory = cb_elderlingsPerPlanet[Right] + cb_eggsPerPlanet[Right];
                return leftTerritory.CompareTo( rightTerritory );
            } );
            if ( tracing )
                tracingBuffer.Add( "\tWe chose " + WorkingPlanets[0].Name + " for the next planet for an egg." );
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            return WorkingPlanets[0];
        }
        public void UpdateTerritoryIfPossible( int planetsToAdd, GameEntity_Squad elderling, ArcenHostOnlySimContext Context, bool forceCheck )
        {
            if ( elderling == null )
                return;

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Elderlings );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Elderling-UpdateTerritoryIfPossible-trace", 10f ) : null;
            int debugCode = 0;
            try
            {
                debugCode = 100;
                ElderlingsPerUnitBaseInfo data = elderling.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
                if ( data == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "No elderlings per unit base info on " + elderling.ToStringWithPlanet(), Verbosity.DoNotShow );
                    return;
                }
                if ( tracing )
                    tracingBuffer.Add( "Attempting to increase the territory for " + elderling.ToStringWithPlanet() + " by " + planetsToAdd + "\n" );
                debugCode = 200;
                if ( data.LastTerritoryUpdateTime > World_AIW2.Instance.GameSecond - TerritoryUpdateInterval &&
                     BaseInfo.AIAllied && !forceCheck )
                {
                    //Only for AI allied elderlings; they are the most performance intensive and
                    //I want the minor faction/player allied elderligns to be able to expand with their allies more quickly
                    if ( tracing )
                        tracingBuffer.Add( "\tSkipping, it's been too recent (in ").Add( (( data.LastTerritoryUpdateTime + TerritoryUpdateInterval) - World_AIW2.Instance.GameSecond) ).Add(" seconds)\n" );
                    FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
                    return; //limit how frequently we check this. Minor performance tweak
                }
                data.LastTerritoryUpdateTime = World_AIW2.Instance.GameSecond;
                if ( data.Territory.Count == 0 )
                {
                    debugCode = 300;
                    if ( tracing )
                        tracingBuffer.Add( "Territory gets elderling current planet, " + elderling.Planet.Name + "\n" );

                    data.Territory.AddButRejectIfNull( elderling.Planet );
                    planetsToAdd--;
                }
                debugCode = 400;
                for ( int i = 0; i < planetsToAdd; i++ )
                {
                    debugCode = 500;
                    WorkingPlanets.Clear();
                    for ( int j = 0; j < data.Territory.Count; j++ )
                    {
                        debugCode = 600;
                        Planet territory = data.Territory[j];
                        if ( territory == null )
                            throw new Exception( "Got a null planet in Territory of " + elderling.ToStringWithPlanet() + " ?!" );
                        debugCode = 610;
                        foreach ( Planet neighbor in territory.LinkedNeighbors( false ) )
                        {
                            debugCode = 700;
                            if ( neighbor == null || neighbor.HasPlanetBeenDestroyed )
                                continue;
                            if ( data.Territory.Contains( neighbor ) || WorkingPlanets.Contains( neighbor ) )
                                continue;
                            if ( tracing )
                                tracingBuffer.Add( "\tConsdering " + neighbor.Name + "\n" );

                            if ( BaseInfo.ElderlingsPerPlanet[neighbor] + BaseInfo.EggsPerPlanet[neighbor] >= BaseInfo.Difficulty.MaxElderlingsPerPlanet )
                            {
                                if ( tracing )
                                    tracingBuffer.Add( "\t\tDiscarding " + neighbor.Name + "; it is already in the territory of " + BaseInfo.ElderlingsPerPlanet[neighbor] + " elderlings and " + BaseInfo.EggsPerPlanet[neighbor] + " eggs \n" );
                                continue;
                            }
                            PlanetFaction pFaction = neighbor.GetPlanetFactionForFaction( AttachedFaction );
                            int friendlyStrength = pFaction.DataByStance[FactionStance.Friendly].TotalStrength + pFaction.DataByStance[FactionStance.Self].TotalStrength + elderling.GetStrengthOfSelfAndContents();
                            int hostileStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;

                            if ( friendlyStrength < hostileStrength )
                            {
                                if ( tracing )
                                    tracingBuffer.Add( "\t\tDiscarding " + neighbor.Name + "; it too scary.\n" );
                                continue;
                            }
                            if ( tracing )
                                tracingBuffer.Add( "\t\tAdding " + neighbor.Name + " to the consideration list. It is already in the territory of " + BaseInfo.ElderlingsPerPlanet[neighbor] + " elderlings (and " + BaseInfo.EggsPerPlanet[neighbor] + " eggs), and hostile strength is " + hostileStrength + ".\n" );
                            WorkingPlanets.Add( neighbor );
                        }
                        debugCode = 790;
                    }
                    debugCode = 800;
                    if ( WorkingPlanets.Count > 0 )
                    {
                        debugCode = 900;
                        cb_elderlingsDeepFaction = AttachedFaction;
                        cb_elderlingsPerPlanet = BaseInfo.ElderlingsPerPlanet;
                        cb_eggsPerPlanet = BaseInfo.EggsPerPlanet;
                        WorkingPlanets.Sort( static delegate ( Planet Left, Planet Right )
                        {
                            //First, prefer planets with fewer enemies
                            PlanetFaction lpFaction = Left.GetPlanetFactionForFaction( cb_elderlingsDeepFaction );
                            PlanetFaction rpFaction = Right.GetPlanetFactionForFaction( cb_elderlingsDeepFaction );
                            int lHostileStrength = lpFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                            int rHostileStrength = rpFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                            if ( lHostileStrength != rHostileStrength )
                                return lHostileStrength.CompareTo( rHostileStrength );

                            //Then prefer planets with fewer Elderlings already.
                            int leftTerritory = cb_elderlingsPerPlanet[Left] + cb_eggsPerPlanet[Left];
                            int rightTerritory = cb_elderlingsPerPlanet[Right] + cb_eggsPerPlanet[Right];
                            return leftTerritory.CompareTo( rightTerritory );
                        } );
                        if ( tracing )
                        {
                            tracingBuffer.Add( "\tWe chose " + WorkingPlanets[0].Name + " for the next planet in our territory." );
                        }
                        data.Territory.AddButRejectIfNull( WorkingPlanets[0] );
                        this.BaseInfo.ElderlingsPerPlanet[WorkingPlanets[0]]++;
                    }
                    debugCode = 1000;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in UpdateTerritoryIfPossible debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        public Planet GetPlanetToInvade( ArcenHostOnlySimContext Context )
        {
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                int enemyStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                int myAndAlliedStrength = pFaction.DataByStance[FactionStance.Self].TotalStrength +
                    pFaction.DataByStance[FactionStance.Friendly].TotalStrength;
                if ( enemyStrength == 0 && myAndAlliedStrength > 3000 )
                    InvadablePlanets.Add( planet );
            }
            if ( InvadablePlanets.Count == 0 )
                return null;
            Planet bestPlanet = InvadablePlanets[Context.RandomToUse.Next( 0, InvadablePlanets.Count )];
            ArcenDebugging.ArcenDebugLogSingleLine( "Elderlings found best planet to invade " + bestPlanet.Name + " from " + InvadablePlanets.Count + " options", Verbosity.DoNotShow );
            return bestPlanet;
        }
        public void HatchEggs( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Elderlings );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Elderling-HatchEggs-trace", 10f ) : null;
            int debugCode = 0;
            try
            {
                List<SafeSquadWrapper> eggs = this.BaseInfo.Eggs.GetDisplayList();
                debugCode = 100;
                for ( int i = 0; i < eggs.Count; i++ )
                {
                    debugCode = 200;
                    GameEntity_Squad egg = eggs[i].GetSquad();
                    if ( egg == null )
                        continue;
                    ElderlingsPerUnitBaseInfo data = egg.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
                    if ( data == null )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "No elderlings per unit base info on " + egg.ToStringWithPlanet(), Verbosity.DoNotShow );
                        continue;
                    }
                    debugCode = 300;
                    if ( data.HatchTime <= World_AIW2.Instance.GameSecond )
                    {
                        debugCode = 400;
                        int elderlingsToSpawn = 1;
                        if ( (this.BaseInfo.Elderlings.Count > 2 && this.BaseInfo.PlayerAllied && BaseInfo.Difficulty.PercentMultiSpawn < 100 ) &&
                             Context.RandomToUse.Next( 0, 100 ) < BaseInfo.Difficulty.PercentMultiSpawn )
                            elderlingsToSpawn++; //player-allied elderlings can't multi-spawn early in the game (unless you set the percent multi spawn to 100
                        int maddenedElderlingsToSpawn = 0;
                        if ( this.BaseInfo.TimeLastSpawnedMaddenedEgg != -1 &&
                             this.BaseInfo.TimeLastSpawnedMaddenedEgg <= World_AIW2.Instance.GameSecond )
                        {
                            maddenedElderlingsToSpawn = NecromancerEmpireFactionBaseInfo.GetNecromancerFactionCount();
                            this.BaseInfo.TimeLastSpawnedMaddenedEgg = World_AIW2.Instance.GameSecond + this.BaseInfo.Difficulty.MaddenedEggInterval;
                            ArcenDebugging.ArcenDebugLogSingleLine("Spawning some maddened elderligns! will spawn them next at " + this.BaseInfo.TimeLastSpawnedMaddenedEgg, Verbosity.DoNotShow );
                        }

                        for ( int j = 0; j < elderlingsToSpawn + maddenedElderlingsToSpawn; j++ )
                        {
                            debugCode = 500;
                            string tag = GetElderlingTag( Context );
                            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, tag );
                            if ( entityData == null )
                                throw new Exception( "Could not find elderling with tag " + tag );
                            PlanetFaction pFaction = egg.PlanetFaction;
                            if ( tracing )
                                tracingBuffer.Add( "\t spawning an Elderling now with tag " + tag + "\n" );
                            GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, (byte)1,
                                                                                                          pFaction.Faction.LooseFleet, 0, egg.WorldLocation, Context, "ElderlingEggHatch" );
                            newEntity.ShouldNotBeConsideredAsThreatToHumanTeam = true; //elderlings units aren't threat

                            ElderlingsPerUnitBaseInfo newData = newEntity.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
                            newData.NextEggLayingTime = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.BaseEggSpawningInterval;
                            if ( BaseInfo.ExtraStrongMode )
                                newData.NextEggLayingTime = World_AIW2.Instance.GameSecond + 180;
                            if ( World_AIW2.Instance.Setup.GetBoolBySetting("BonusElderlings") )
                            {
                                //spawn eggs a bit more often
                                newData.NextEggLayingTime = World_AIW2.Instance.GameSecond + (BaseInfo.Difficulty.BaseEggSpawningInterval * FInt.FromParts(0, 800)).IntValue;
                            }
                            if ( this.BaseInfo.PlayerAllied )
                                newData.TrackedByPlayer = true; //free tracking for your buddies!
                            if ( j >= elderlingsToSpawn )
                            {
                                newData.SanityRemaining = -999; //Spawn this one pre-maddened. Numbers > 100 won't join the Maddened Elderlings, but will instead just attack the player
                            }
                            
                            if ( tracing )
                                tracingBuffer.Add( "\t\tEgg Laying time: " + data.NextEggLayingTime + " extraStrong? " + BaseInfo.ExtraStrongMode + " SanityRemaining " + newData.SanityRemaining + "\n" );

                            newEntity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //is fine, main sim thread

                            newData.NumberOfTimesLeveledUp = 0;
                            if ( newEntity.TypeData.GetHasTag("MedElderling") )
                                newData.ExperienceRequired = BaseInfo.Difficulty.ExperienceRequiredToLevelUpMedTier;
                            else if ( newEntity.TypeData.GetHasTag("HighElderling") )
                                newData.ExperienceRequired = BaseInfo.Difficulty.ExperienceRequiredToLevelUpHighTier;
                            else
                                newData.ExperienceRequired = BaseInfo.Difficulty.ExperienceRequiredToLevelUpLowTier;
                            if ( World_AIW2.Instance.Setup.GetBoolBySetting("ChallengerElderlings") )
                            {
                                newData.ExperienceRequired = (newData.ExperienceRequired * BaseInfo.Difficulty.ExperienceMultiplerForChallengerPlus).IntValue;
                            }
                            if ( World_AIW2.Instance.Setup.GetBoolBySetting("BonusElderlings") )
                            {
                                newData.ExperienceRequired = (newData.ExperienceRequired * FInt.FromParts(0, 700)).IntValue;
                            }
                            debugCode = 600;
                        }
                        debugCode = 700;
                        egg.Despawn( Context, true, InstancedRendererDeactivationReason.TransformedIntoAnotherEntityType );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception while HatchEggs debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        public void HandleUnitMarkups( ArcenHostOnlySimContext Context )
        {
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                return; //only on host

            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Elderlings );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Elderling-HandleUnitMarkups-trace", 10f ) : null;
            int debugCode = 0;
            bool debug = false; //this is too verbose to be on by default
            try
            {
                List<SafeSquadWrapper> elderlings = this.BaseInfo.Elderlings.GetDisplayList();
                debugCode = 100;
                if ( tracing && debug )
                    tracingBuffer.Add( "Handling markups for " + elderlings.Count + " elderlings\n" );

                for ( int i = 0; i < elderlings.Count; i++ )
                {
                    debugCode = 200;
                    GameEntity_Squad elderling = elderlings[i].GetSquad();
                    if ( elderling == null )
                        continue;
                    ElderlingsPerUnitBaseInfo data = elderling.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
                    if ( data == null )
                    {
                        ArcenDebugging.ArcenDebugLogSingleLine( "No elderlings per unit base info on " + elderling.ToStringWithPlanet(), Verbosity.DoNotShow );
                        continue;
                    }
                    debugCode = 300;
                    if ( data.FullyUpgraded )
                        continue; //We're done upgrading
                    if ( data.ExperienceRequired > 0 )
                    {
                        data.ExperienceRequired--;
                        if ( elderling.TypeData.GetHasTag( "LowestElderling" ) )
                            data.ExperienceRequired -= 2; //feeble elderligns mark up extra fast because players get more Essence from them if they are higher level
                        if ( World_AIW2.Instance.GameSecond % 10 == 0 &&
                              World_AIW2.Instance.Setup.GetBoolBySetting("BonusElderlings") )
                        {
                            //elderlings level up a bit faster
                            data.ExperienceRequired--;
                        }

                        for ( int j = 0; j < data.Territory.Count; j++ )
                        {
                            //if there are enemies in my territory, I level up more quickly
                            Planet planet = data.Territory[j];
                            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                            int hostileStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                            if ( hostileStrength > 0 )
                                data.ExperienceRequired--;
                        }
                        if ( data.ExperienceRequired > 0 )
                            continue; //not ready to upgrade yet
                    }
                    if ( data.ExperienceRequired <= 0 &&
                         elderling.CurrentMarkLevel == 7 &&
                         elderling.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength > 2 * 000 )
                        continue; //no upgrading forms while in combat; that's cruel

                    if ( tracing && debug )
                        tracingBuffer.Add( "Consdering how we should level up elderling " + elderling ).Add( ".\n\tMark level " + elderling.CurrentMarkLevel + " and exp required: " + data.ExperienceRequired  ).Add(". Are we Low? " ).Add( elderling.TypeData.GetHasTag("LowElderling" ) ).Add(". Are we Med? ").Add( elderling.TypeData.GetHasTag("MedElderling" ) ).Add(". Are we High (cough cough) ").Add( elderling.TypeData.GetHasTag("HighElderling" ) ).Add("\n");
                    if ( elderling.CurrentMarkLevel < 7 )
                    {
                        elderling.SetCurrentMarkLevel( (byte)(elderling.CurrentMarkLevel + 1) );
                    }
                    else
                    {
                        //level up to the next Elderling form
                        GameEntityTypeData newForm = null;
                        if ( elderling.TypeData.GetHasTag( "LowElderling" ) )
                        {
                            if ( tracing && debug)
                                tracingBuffer.Add("\tChose a Med Elderling\n");
                            newForm = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "MedElderling" );
                        }
                        else if ( elderling.TypeData.GetHasTag( "MedElderling" ) )
                        {
                            if ( tracing && debug)
                                tracingBuffer.Add("\tChose a High Elderling\n");

                            newForm = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "HighElderling" );
                        }
                        else
                        {
                            //no newForm is given; we're done upgrading
                            if ( elderling.CurrentMarkLevel == 7 )
                            {
                                data.FullyUpgraded = true;
                                continue;
                            }
                        }

                        if ( newForm != null )
                        {
                            if ( tracing )
                                tracingBuffer.Add("We are going to transform into a " + newForm.GetDisplayName()).Add("\n");
                            elderling = elderling.TransformInto( Context, newForm, 1, true );
                            elderling.SetCurrentMarkLevel( (byte)(1) );
                            elderling.ShouldNotBeConsideredAsThreatToHumanTeam = true; //elderlings units aren't threat
                            //we set additional fields on this entity below
                            ElderlingsPerUnitBaseInfo newData = elderling.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
                            data.CopyTo( newData );
                            data = newData;
                        }
                    }

                    data.NumberOfTimesLeveledUp++;

                    //note the math here means that a mark 1 elderling (ie one that just transformed to a stronger form) will get to level 2 faster
                    int markLevelToUse = elderling.CurrentMarkLevel - 1;
                    int maxTerritory = BaseInfo.Difficulty.MaxTerritorySizeLowTier; 
                    if ( elderling.TypeData.GetHasTag("HighElderling") )
                    {
                        if ( elderling.CurrentMarkLevel == 7 ) {
                            data.FullyUpgraded = true;
                            data.ExperienceRequired = 0;
                        } else {
                            data.ExperienceRequired = BaseInfo.Difficulty.ExperienceRequiredToLevelUpHighTier + BaseInfo.Difficulty.ExperienceRequiredToLevelUpHighTierIncreasePerLevel * markLevelToUse;
                            if ( World_AIW2.Instance.Setup.GetBoolBySetting("ChallengerElderlings") )
                                data.ExperienceRequired = (data.ExperienceRequired * BaseInfo.Difficulty.ExperienceMultiplerForChallengerPlus).IntValue;

                        }
                        maxTerritory = BaseInfo.Difficulty.MaxTerritorySizeHighTier; 
                    }
                    else if ( elderling.TypeData.GetHasTag("MedElderling") )
                    {
                        data.ExperienceRequired = BaseInfo.Difficulty.ExperienceRequiredToLevelUpMedTier + BaseInfo.Difficulty.ExperienceRequiredToLevelUpMedTierIncreasePerLevel * markLevelToUse;
                        if ( World_AIW2.Instance.Setup.GetBoolBySetting("ChallengerElderlings") )
                            data.ExperienceRequired = (data.ExperienceRequired * BaseInfo.Difficulty.ExperienceMultiplerForChallengerPlus).IntValue;

                        maxTerritory = BaseInfo.Difficulty.MaxTerritorySizeMedTier; 
                    }
                    else
                    {
                        if ( !elderling.TypeData.GetHasTag("LowElderling") && elderling.CurrentMarkLevel == 7 ) {
                            //this is for Feeble elderlings
                            data.FullyUpgraded = true;
                            data.ExperienceRequired = 0;
                        } else {
                            data.ExperienceRequired = BaseInfo.Difficulty.ExperienceRequiredToLevelUpLowTier + BaseInfo.Difficulty.ExperienceRequiredToLevelUpLowTierIncreasePerLevel * markLevelToUse;
                            if ( World_AIW2.Instance.Setup.GetBoolBySetting("ChallengerElderlings") )
                                data.ExperienceRequired = (data.ExperienceRequired * BaseInfo.Difficulty.ExperienceMultiplerForChallengerPlus).IntValue;
                        }
                        maxTerritory = BaseInfo.Difficulty.MaxTerritorySizeLowTier; 
                        //ArcenDebugging.ArcenDebugLogSingleLine("\tC: low tier base: " + BaseInfo.Difficulty.ExperienceRequiredToLevelUpLowTier + " and mark level: " + BaseInfo.Difficulty.ExperienceRequiredToLevelUpLowTierIncreasePerLevel + " and mark level to use " + markLevelToUse, Verbosity.DoNotShow );
                    }
                    if ( this.BaseInfo.PlayerAllied )
                        data.TrackedByPlayer = true; //free tracking for your buddies!

                    if ( Context.RandomToUse.Next(0, 100) < 50 && data.Territory.Count < maxTerritory &&
                         elderling.CurrentMarkLevel >= 2 ) //make sure we don't increase our territory too soon after a form shift
                    {
                        data.GetsAFreeTerritoryIfPossible = true;
                    }

                    if ( tracing && debug )
                    {
                        tracingBuffer.Add( "\tNew mark level: " + elderling.CurrentMarkLevel ).Add( " and new experience required ").Add(data.ExperienceRequired).Add(" for " ).Add( elderling.ToString() );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during HandleUnitMarkups debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            #region Tracing
            if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            if ( tracing )
            {
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }
        private string GetElderlingTag( ArcenHostOnlySimContext Context )
        {
            //Elderlings now start lower-tier since they can cross tiers when marking up
            FInt aip = FactionUtilityMethods.Instance.GetCurrentAIP();
            if ( aip < BaseInfo.Difficulty.AIPForMedTier )
                return "LowElderling";
            if ( aip > BaseInfo.Difficulty.AIPForMedTier && aip < BaseInfo.Difficulty.AIPForHighTier )
            {
                if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                    return "LowElderling";
                else
                    return "MedElderling";
            }
            if ( aip > BaseInfo.Difficulty.AIPForHighTier )
            {
                if ( Context.RandomToUse.Next( 0, 100 ) < 20 )
                    return "LowElderling";
                else if ( Context.RandomToUse.Next( 0, 100 ) < 90 )
                    return "MedElderling";
                else
                    return "HighElderling";
            }
            return "LowElderling";
        }

        public void HandleJournals( ArcenHostOnlySimContext Context )
        {
            if ( BaseInfo.PlayerAllied && BaseInfo.Eggs.Count > 0 )
            {
                World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Elderlings_Overview_PlayerAllied", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
            }
            List<SafeSquadWrapper> elderlings = this.BaseInfo.Elderlings.GetDisplayList();
            for ( int i = 0; i < elderlings.Count; i++ )
            {
                if ( elderlings[i].GetShouldBeVisibleBasedOnPlanetIntel() )
                {
                    GameEntity_Squad entity = elderlings[i].GetSquad();
                    ElderlingsPerUnitBaseInfo data = entity.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
                    if ( data.SanityRemaining == -1 )
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Elderlings_Maddened", string.Empty, AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                }
            }
        }

        public void JoinAlliesIfNecessary( ArcenHostOnlySimContext Context )
        {
            // bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Elderlings );
            // ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Elderling-JoinAlliesIfNecessary-trace", 10f ) : null;
            if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
            {
                return; //No client stuff
            }
            if ( this.BaseInfo.Eggs.GetDisplayList().Count > 0 ||
                 this.BaseInfo.Elderlings.GetDisplayList().Count > 0 )
            {
                return;
            }
            if ( BaseInfo.TimeLastHadElderlings != -1 )
            {
                //Only do these additional checks if we are respawning. If we have never spawned before (like from a beacon) then we want to spawn ASAP
                if ( World_AIW2.Instance.GameSecond < BaseInfo.TimeLastHadElderlings + 600 )
                {
                    return;
                }
                if ( Context.RandomToUse.Next( 0, 20 ) > 1 )
                {
                    return; //Be at least a bit more unpredictable, so its not exactly 10 minutes
                }
            }
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "ElderlingEgg" );
            if ( BaseInfo.PlayerAllied || BaseInfo.AIAllied )
            {
                //This case is for Beacons
                foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( entity.GetIsFriendlyTowards_Safe( AttachedFaction ) )
                    {
                        GameEntity_Squad squad = entity.Planet.Mapgen_SeedEntity( Context, AttachedFaction, entityData, PlanetSeedingZone.MostAnywhere );
                        ElderlingsPerUnitBaseInfo data = squad.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
                        data.HatchTime = World_AIW2.Instance.GameSecond + BaseInfo.Difficulty.BaseEggHatchingInterval / 10; //hatch extra quick on beacon spawn
                    }
                }
            }
            else
            {
                //We are minor faction allied. First try to spawn with our allies. If we can't, try spawning on an empty planet
                bool allowRetries = true;
                bool mustBeWithAllies = true;
                bool hasSpawned = false;
                while ( allowRetries )
                {
                    //ArcenDebugging.ArcenDebugLogSingleLine("must we spawn with allies? " + mustBeWithAllies , Verbosity.DoNotShow );
                    foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                    {
                        //ArcenDebugging.ArcenDebugLogSingleLine("\tChecking " + planet.Name, Verbosity.DoNotShow );
                        Faction controllingFaction = planet.GetControllingOrInfluencingFaction();
                        if ( controllingFaction == null )
                            continue;
                        if ( controllingFaction.GetIsHostileTowards( AttachedFaction ) )
                        {
                            //ArcenDebugging.ArcenDebugLogSingleLine("\t\t path A; hostile owner", Verbosity.DoNotShow );
                            continue;
                        }
                        if ( mustBeWithAllies && !controllingFaction.GetIsFriendlyTowards( AttachedFaction ) )
                        {
                            //ArcenDebugging.ArcenDebugLogSingleLine("\t\t path B; no friends", Verbosity.DoNotShow );
                            continue;
                        }

                        PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                        if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength > 0 )
                        {
                            //ArcenDebugging.ArcenDebugLogSingleLine("\t\t path C; enemies!", Verbosity.DoNotShow );
                            continue;
                        }
                        if ( ArcenNetworkAuthority.GetIsHostMode() )
                        {
                            if ( planet.IntelLevel > PlanetIntelLevel.Unexplored )
                            {
                                PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                                if ( chatHandlerOrNull != null )
                                    chatHandlerOrNull.PlanetToView = planet;
                                if ( mustBeWithAllies )
                                    World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Elderlings</color> have joined the " +
                                                                                   controllingFaction.StartFactionColourForLog() + controllingFaction.GetDisplayName() + "</color> forces on " + planet.Name, ChatType.LogToCentralChat, chatHandlerOrNull );
                                else
                                    World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Elderlings</color> have appeared on " + planet.Name, ChatType.LogToCentralChat, chatHandlerOrNull );
                            }
                        }

                        GameEntity_Squad squad = planet.Mapgen_SeedEntity( Context, AttachedFaction, entityData, PlanetSeedingZone.MostAnywhere );
                        ElderlingsPerUnitBaseInfo data = squad.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
                        data.HatchTime = World_AIW2.Instance.GameSecond + 60; //spawn fast when with our friends
                        hasSpawned = true;

                        break;
                    }
                    if ( hasSpawned )
                        break;

                    if ( allowRetries && mustBeWithAllies )
                        mustBeWithAllies = false;
                    else if ( allowRetries && !mustBeWithAllies )
                        allowRetries = false;

                }
            }
            // #region Tracing
            // if ( (tracing) && !tracingBuffer.GetIsEmpty() ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " " + AttachedFaction.FactionIndex ).Add( " Join Allies trace ends at " ).Add( World_AIW2.Instance.GameSecond ).Add( "\n" );
            // if ( (tracing) && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            // if ( tracing )
            // {
            //     tracingBuffer.ReturnToPool();
            //     tracingBuffer = null;
            // }
            // #endregion
        }


        //Long Range Planning (LRP) starts here. It currently doesn't do anything, but leaving it here just in case we want it someday
        //Note that the class itself says "Never calls LRP"
        public static readonly List<SafeSquadWrapper> ElderlingsLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "ElderlingsFactionDeepInfo-ElderlingsLRP" );
        public static readonly List<Planet> PossiblePlanetsLRP = List<Planet>.Create_WillNeverBeGCed( 500, "ElderlingsFactionDeepInfo-PossiblePlanetsLRP" );
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            #region Tracing
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Elderlings );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Elderling-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            #endregion
            int debugCode = 0;

            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                debugCode = 100;
                ElderlingsLRP.Clear();
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads( "Elderling" ) )
                {
                    if ( entity.TypeData.GetHasTag("HackedElderling") )
                        continue;
                    ElderlingsLRP.Add( entity );
                }

                if ( ElderlingsLRP.Count == 0 )
                    return; //nothing to do here!
                if ( tracing )
                    tracingBuffer.Add( "We have " + ElderlingsLRP.Count + " elderlings in LRP.\n" );
                debugCode = 200;

                for ( int i = 0; i < ElderlingsLRP.Count; i++ )
                {
                    debugCode = 300;
                    GameEntity_Squad elderling = ElderlingsLRP[i].GetSquad();
                    if ( elderling == null )
                        continue;
                    ElderlingsPerUnitBaseInfo data = elderling.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
                    //Planet destPlanet = null;
                    if ( tracing )
                        tracingBuffer.Add( "Considering " + elderling.ToStringWithPlanet() + ". Queued orders? " + elderling.HasQueuedOrders() + "\n" );

                    if ( data.LurePlanet != null )
                    {
                        //we are being lured, which overrides everything
                        if ( data.LurePlanet == elderling.Planet )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "\tWe have been lured to " + data.LurePlanet.Name + "\n" );
                        }
                        else if ( data.LurePlanet != elderling.GetDestinationPlanet() )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "\tWe are being lured to " + data.LurePlanet.Name + " and are now on " + elderling.Planet.Name + ". Dispatching!\n" );
                            SendShipToPlanet( elderling, data.LurePlanet, Context, pathingCacheData );//go to the planet
                        }
                        else
                        {
                            if ( tracing )
                                tracingBuffer.Add( "\tWe are being lured to " + data.LurePlanet.Name + " and are now on " + elderling.Planet.Name + ". We think we are en route\n" );
                        }
                        continue;
                    }

                    if ( data.PlanetForEgg != null )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\tWe are in egg-laying mode; heading to " + data.PlanetForEgg.Name + "\n" );
                        if ( elderling.Planet == data.PlanetForEgg &&
                             data.LocationToBuild != ArcenPoint.ZeroZeroPoint )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "\t\tWe are on the planet! head to " + data.LocationToBuild + "\n" );

                            SendShipToLocation( elderling, data.LocationToBuild, Context );
                        }
                        if ( elderling.Planet != data.PlanetForEgg )
                        {
                            bool needsPlanetOrders = true;
                            if ( elderling.HasQueuedOrders() )
                            {
                                Planet currentDest = elderling.GetDestinationPlanet();
                                if ( currentDest == data.PlanetForEgg )
                                    needsPlanetOrders = false;
                            }
                            if ( needsPlanetOrders )
                            {
                                if ( tracing )
                                    tracingBuffer.Add( "\t\tWe are not on the planet! head to " + data.PlanetForEgg.Name + "\n" );

                                SendShipToPlanet( elderling, data.PlanetForEgg, Context, pathingCacheData );//go to the planet
                            }
                        }
                        continue;
                    }

                    if ( elderling.HasQueuedOrders() )
                    {
                        //check if there are enemies at our destination planet to fight
                        Planet currentDest = elderling.GetDestinationPlanet();
                        PlanetFaction pFaction = currentDest.GetPlanetFactionForFaction( AttachedFaction );
                        int friendlyStrength = pFaction.DataByStance[FactionStance.Friendly].TotalStrength + pFaction.DataByStance[FactionStance.Self].TotalStrength + elderling.GetStrengthOfSelfAndContents();
                        int hostileStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;

                        if ( hostileStrength > 0 && hostileStrength < friendlyStrength * 1.1 ) //if there are enemies (or the enemies are "weak enough" for our tastes), keep attacking
                        {
                            if ( tracing )
                                tracingBuffer.Add( "We are still happily heading toward " + currentDest.Name + "; there are " + hostileStrength + " enemies.\n" );

                            continue;
                        }
                    }
                    //Are there enemies here to fight?
                    {
                        PlanetFaction pFaction = elderling.Planet.GetPlanetFactionForFaction( AttachedFaction );
                        int friendlyStrength = pFaction.DataByStance[FactionStance.Friendly].TotalStrength + pFaction.DataByStance[FactionStance.Self].TotalStrength + elderling.GetStrengthOfSelfAndContents();
                        int hostileStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;

                        if ( hostileStrength > 0 && hostileStrength < friendlyStrength * 1.1 ) //if there are no enemies, or the enemies are too strong, run away
                        {
                            if ( tracing )
                                tracingBuffer.Add( "We are still happily fighting on " + elderling.Planet.Name + "; there are " + hostileStrength + " enemies.\n" );

                            continue;
                        }
                    }
                    if ( tracing )
                        tracingBuffer.Add( "Finding a destination for " + elderling.ToString() + ".\n" );

                    //In this case, we may or may not have orders, but these orders can be preempted
                    bool mustHaveHostilesOnPlanet = true;
                    Planet planet = GetNextPlanetToGoTo( elderling, mustHaveHostilesOnPlanet, Context );
                    if ( planet == null )
                    {
                        //no hostile units anywhere, so either go to a r andom other planet or to a metal generator
                        if ( tracing )
                            tracingBuffer.Add( "\tFinding a destination for " + elderling.ToString() + " path B.\n" );
                        if ( Context.RandomToUse.Next( 0, 100 ) < 50 )
                        {
                            if ( tracing )
                                tracingBuffer.Add( "\tGoing to a metal generator.\n" );

                            FactionUtilityMethods.Instance.SendUnitToRandomMetalGenerator( AttachedFaction, Context, elderling, 10f );
                            continue;
                        }
                        planet = GetNextPlanetToGoTo( elderling, !mustHaveHostilesOnPlanet, Context );

                    }
                    if ( planet == null )
                    {
                        if ( tracing )
                            tracingBuffer.Add( "\tfailed to find a dest .\n" );
                        continue;
                    }
                    PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( AttachedFaction, "ElderlingsLRP", elderling.Planet, planet, PathingMode.Default, Context, pathingCacheData );
                    if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                    {
                        GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleSpecialMission], GameCommandSource.AnythingElse );
                        debugCode = 700;
                        command.RelatedString = "Eldr_ToAttack";
                        command.RelatedEntityIDs.Add( elderling.PrimaryKeyID );
                        command.ToBeQueued = false;
                        for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );

                        if ( tracing )
                            tracingBuffer.Add( "LRP processing " + elderling.ToStringWithPlanet() ).Add( "\n" );
                    }
                }

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception during elderlings LRP debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            finally
            {
                pathingCacheData.ReturnToPool();

                #region Tracing
                if ( (tracing) && !tracingBuffer.GetIsEmpty() ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " " + AttachedFaction.FactionIndex ).Add( " LongRangePlanning trace ends at " ).Add( World_AIW2.Instance.GameSecond ).Add( "\n" );
                if ( (tracing) && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
            }
        }
        public Planet GetNextPlanetToGoTo( GameEntity_Squad elderling, bool MustHaveEnemies, ArcenLongTermIntermittentPlanningContext Context )
        {
            if ( elderling == null )
                return null;
            bool tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Elderlings );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "Elderling-GetNextPlanetToGoTo-trace", 10f ) : null;

            //we can help on friendly or neutral planets with enemies and friends
            //Planet foundPlanet = null;
            bool verboseDebug = false;
            ElderlingsPerUnitBaseInfo data = elderling.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
            PossiblePlanetsLRP.Clear();
            if ( verboseDebug && tracing )
                tracingBuffer.Add( "Finding the next planet for " + elderling.ToStringWithPlanet() + "; SuicideMode " + data.SuicideMode + "\n" );
            if ( data.SuicideMode )
            {
                //this unit is just going to attack an enemy king if possible
                foreach ( GameEntity_Squad king in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( king.PlanetFaction.Faction.GetIsHostileTowards( elderling.PlanetFaction.Faction ) )
                        PossiblePlanetsLRP.Add( king.Planet );
                }
                if ( PossiblePlanetsLRP.Count > 0 )
                {
                    if ( verboseDebug && tracing )
                        tracingBuffer.Add( "Suicidal elderling " + elderling.ToStringWithPlanet() + " is finding a king\n" );

                    return PossiblePlanetsLRP[Context.RandomToUse.Next( 0, PossiblePlanetsLRP.Count )];
                }
            }
            for ( int i = 0; i < data.Territory.Count; i++ )
            {
                Planet planet = data.Territory[i];
                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                int hostileStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;
                int friendlyStrength = pFaction.DataByStance[FactionStance.Friendly].TotalStrength + pFaction.DataByStance[FactionStance.Self].TotalStrength + elderling.GetStrengthOfSelfAndContents(); ;
                if ( verboseDebug && tracing )
                    tracingBuffer.Add( "\t " + planet.Name + " hostileStrength " + hostileStrength + " friendlyStrength " + friendlyStrength ).Add( "\n" );

                if ( MustHaveEnemies && hostileStrength == 0 )
                    continue; //no enemies, and we need enemies
                if ( hostileStrength > friendlyStrength )
                    continue;
                PossiblePlanetsLRP.Add( planet );
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            if ( PossiblePlanetsLRP.Count == 0 )
                return null;

            return PossiblePlanetsLRP[Context.RandomToUse.Next( 0, PossiblePlanetsLRP.Count )];
        }
        public void SendShipToPlanet( GameEntity_Squad entity, Planet destination, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( entity.PlanetFaction.Faction, "ElderlingsSendShipToPlanet", entity.Planet, destination, PathingMode.Safest, Context, PathCacheData );
            if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
            {
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse );
                command.RelatedString = "Elderling_Dest";
                command.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                    command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
            }
        }
        public void SendShipToLocation( GameEntity_Squad entity, ArcenPoint dest, ArcenLongTermIntermittentPlanningContext Context )
        {
            GameCommand moveCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse );
            moveCommand.PlanetOrderWasIssuedFrom = entity.Planet.Index;
            moveCommand.RelatedPoints.Add( dest );
            moveCommand.ToBeQueued = false;
            moveCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
            World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, moveCommand, false );
        }
        public override void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        {
            if ( entity == null )
                return;
            int debugStage = 0;
            try
            {

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in SpecialFaction_Elderlings.DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly stage " + debugStage + "\n" + e, Verbosity.ShowAsError );
            }
        }
        public override void DoOnFirstSightingOfFactionByPlayer( bool IsFromBeacon, GameEntity_Squad SquadSeenOrNull, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return;
            if ( !IsFromBeacon )
            {
                bool shown = false;
                if ( NecromancerEmpireFactionBaseInfo.GetNecromancerFactionCount() > 0 && SquadSeenOrNull != null )
                {
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Elderlings_Overview_Necromancer", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Elderlings_Overview_Necromancer_Tracking", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    shown = true;
                }
                if ( !shown )
                {
                    if ( !BaseInfo.PlayerAllied )
                    {
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Elderlings_Overview", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                        World_AIW2.Instance.QueueLogJournalEntryToSidebar( "NA_Elderlings_Overview_Tracking", string.Empty, this.AttachedFaction, null, null, OnClient.DoThisOnHostOnly_WillBeSentToClients );
                    }
                }
            }
        }

        public override void SeedStartingEntities_LaterEverythingElse( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            Tutorial tutorialData = World_AIW2.Instance.TutorialOrNull;
            if ( tutorialData != null ) 
                return;
            
            var allegiance = AttachedFaction.GetCommonAllegiance();
           
            Trace.For(ArcenTracingFlags.Elderlings)?
                .Msg("{0}() called for {1}. allegiance={2}", 
                     this.TypeNameAndMethod(), AttachedFaction.OrNull(), Extensions.ToString(allegiance));
            
            //This happens with beacons and we want to use the logic in JoinAlliesIfNecessary() to handle that case
            if ( World_AIW2.Instance.GameSecond > 1 )
                return; 

            var hatchTime = World_AIW2.Instance.GameSecond + (BaseInfo.Difficulty.BaseEggHatchingInterval/2);
            if ( BaseInfo.ExtraStrongMode )
                hatchTime = World_AIW2.Instance.GameSecond + 30;
            
            foreach ( GameEntity_Squad e in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( !e.GetIsFriendlyTowards_Safe( AttachedFaction ) )
                    continue;

                var squad = e.Planet.Mapgen_SeedEntity( Context, AttachedFaction, "ElderlingEgg", PlanetSeedingZone.MostAnywhere );

                var data = squad.CreateExternalBaseInfo<ElderlingsPerUnitBaseInfo>( "ElderlingsPerUnitBaseInfo" );
                data.HatchTime = hatchTime;

                if ( BaseInfo.ExtraStrongMode )
                    squad = e.Planet.Mapgen_SeedEntity( Context, AttachedFaction, "MedElderling", PlanetSeedingZone.MostAnywhere );
            }
            
            if (allegiance == CommonAllegiances.AlliedToAI)
            {
                if ( BaseInfo.Difficulty.BaseStartingEggs > 0 )
                {
                    int bonusEggs = BaseInfo.Difficulty.BaseStartingEggs;
                    
                    // challenger and above
                    if ( World_AIW2.Instance.Setup.GetBoolBySetting("ChallengerElderlings") )
                        bonusEggs += 4;
                    
                    // player requested
                    if (World_AIW2.Instance.Setup.GetBoolBySetting("BonusElderlings"))
                        bonusEggs += 2;
                    
                    StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, AttachedFaction, SpecialEntityType.None, "ElderlingEgg", SeedingType.HardcodedCount, bonusEggs,
                                                            MapGenCountPerPlanet.One, MapGenSeedStyle.SmallBad, 2, 2, PlanetSeedingZone.MostAnywhere, SeedingExpansionType.ComplicatedOriginal );
                }
                
                if ( NecromancerEmpireFactionBaseInfo.GetNecromancerFactionCount() > 0 )
                {
                    Trace.For(ArcenTracingFlags.Necromancer)?.Msg("Seeding elderlings for necromancer.");
                    
                    // Feeble elderlings are used for the necromancer; they are seeded near the player to give some extra early Essence, and to teach the mechanic.
                    int baseFeebleElderlings = NecromancerEmpireFactionBaseInfo.GetNecromancerFactionCount();
                    
                    var args = new SeedArgs()
                    {
                        FactionOrNull = AttachedFaction,
                        TagOrEmpty = "LowestElderling",
                        Count = baseFeebleElderlings,
                        SeedStyle = MapGenSeedStyle.NoChecks,
                        MinDistanceFromHumanHomeworld = 2,
                        MaxDistanceFromHumanHomeworld = 2,
                        SeedingZone = PlanetSeedingZone.MostAnywhere,
                        ExpansionStyle = SeedingExpansionType.ComplicatedOriginal,
                        DisableDistanceRescaling = true,
                    };
                    StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, args );
                    
                    // A bit further away but still nice and close.
                    args.Count = baseFeebleElderlings + 2 + BaseInfo.Difficulty.AdditionalFeebleElderlings; 
                    args.MinDistanceFromHumanHomeworld = 3;
                    args.MaxDistanceFromHumanHomeworld = 5;
                    StandardMapPopulator.Mapgen_SeedSpecialEntities( Context, galaxy, args );
                }
                else
                {
                    Trace.For(ArcenTracingFlags.Necromancer)?.Msg("Skipping seeding elderlings for necromancer.");
                }
            }
        }
    }
}
