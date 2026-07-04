using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /// <summary>
    //Handles the logic for a RelentlessWave; when the AI creates RelentlessWave units they are donated to the corresponding RelentlessWave faction. Each AI has a unique
    //RelentlessWave faction so they can have appropriate colours when a RelentlessWave.

    //When a RelentlessWave has units that aren't on enemy planets, it takes all of its units on a given planet and picks a weighted random target planet to go to (since there are usually a lot of Planets, we hit a lot of targets).
    //When a RelentlessWave has units on an enemy planet without a target, they pick a specific random valuable structure on a planet and attack it. Since RelentlessWave ships will trickle in, this means they will distribute well across the targets in a system.
    /// </summary>
    public abstract class AIRelentlessAndBorderAggressionFactionDeepInfoRoot : ExternalFactionDeepInfoRoot
    {
        //Set immediately before the target sorts so the comparisons can be non-capturing static
        //delegates.  [ThreadStatic] because long-range planning runs on a background thread.
        [ThreadStatic] private static AIRelentlessAndBorderAggressionFactionDeepInfoRoot cb_arbaThis;
        [ThreadStatic] private static Planet cb_arbaStartPlanet;
        [ThreadStatic] private static int cb_arbaStrength;
        //Let's go ahead and seal it here, since the two classes could potentially have LRP conflicts otherwise

        public AIRelentlessAndBorderAggressionFactionBaseInfoRoot BaseInfo;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<AIRelentlessAndBorderAggressionFactionBaseInfoRoot>();
        }

        protected override void Cleanup() 
        {
            this.BaseInfo = null;

            //might matter
            UnassignedShips.Clear();
            UnassignedShipsByPlanet.Clear();
            InCombatShipsNeedingOrders.Clear();
            WorkingMetalGeneratorList.Clear();
            playAudioEffectForCommand = false;

            this.SubCleanup();
        }
        public abstract void SubCleanup();

        protected override int MinimumSecondsBetweenLongRangePlannings => 5;
        public readonly List<SafeSquadWrapper> UnassignedShips = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "AIRelentlessAndBorderAggressionFactionDeepInfoRoot-UnassignedShips" );
        public readonly DictionaryOfLists<Planet, SafeSquadWrapper> UnassignedShipsByPlanet = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 500, "AIRelentlessAndBorderAggressionFactionDeepInfoRoot-UnassignedShipsByPlanet" );
        public readonly DictionaryOfLists<Planet, SafeSquadWrapper> InCombatShipsNeedingOrders = DictionaryOfLists<Planet, SafeSquadWrapper>.Create_WillNeverBeGCed( 100, 500, "AIRelentlessAndBorderAggressionFactionDeepInfoRoot-InCombatShipsNeedingOrders" );
        public readonly List<SafeSquadWrapper> WorkingMetalGeneratorList = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 500, "AIRelentlessAndBorderAggressionFactionDeepInfoRoot-WorkingMetalGeneratorList" );
        private bool playAudioEffectForCommand = false;
        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass( ArcenLongTermIntermittentPlanningContext Context )
        {
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.RelentlessWave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIBorderAgg-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-trace", 10f ) : null;
            int debugCode = 0;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            if ( tracing )
                tracingBuffer.Add("Starting LRP for ").Add(this.AttachedFaction.GetDisplayName()).Add(" idx ").Add(this.AttachedFaction.FactionIndex).Add("\n");
            try
            {
                int highestDifficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty();
                debugCode = 100;
                UnassignedShips.Clear();
                UnassignedShipsByPlanet.Clear();
                InCombatShipsNeedingOrders.Clear();
                int totalShips = 0;
                int totalStrength = 0;
                int shipsWithMajorOrders = 0;
                int shipsNeedingOrders = 0;
                int shipsOnHostilePlanets = 0;
                int shipsInCombatOnHostilePlanets = 0;
                int shipsInCombatOnFriendlyPlanets = 0;
                int shipsEnRouteToOtherPlanets = 0;
                //Iterate over all our units to figure out if any need orders
                foreach ( GameEntity_Squad entity in AttachedFaction.Squads() )
                {
                    if ( entity == null )
                        continue;
                    debugCode = 101;
                    totalShips++;
                    totalStrength += entity.GetStrengthOfSelfAndContents();
                    if ( entity.HasQueuedOrders() )
                    {
                        debugCode = 102;
                        //entity is doing something already (either attacking a target or en route somewhere)
                        Planet eventualDestinationOrNull = entity.Orders.GetFinalDestinationOrNull();
                        if ( eventualDestinationOrNull == null && entity.HasExplicitOrders() )
                        {
                            shipsWithMajorOrders++;
                            continue; //we are going somewhere on this planet that's not an FRD order (like we have specifically chosen a target), let this ship keep doing what it's doing
                        }
                        debugCode = 103;
                        if ( eventualDestinationOrNull != null )
                        {
                            var factionData = eventualDestinationOrNull.GetStanceDataForFaction( AttachedFaction );
                            if ( factionData[FactionStance.Hostile].TotalStrength > 0 ||
                                 eventualDestinationOrNull.GetControllingFaction().GetIsHostileTowards( AttachedFaction ) )
                            {
                                shipsEnRouteToOtherPlanets++;
                                continue; //if the planet we are going has enemies, keep going there
                            }
                        }
                    }

                    debugCode = 105;
                    Faction controllingFaction = entity.Planet.GetControllingOrInfluencingFaction();
                    if ( controllingFaction.GetIsHostileTowards( AttachedFaction ) )
                    {
                        //This entity is on an enemy planet but doesn't have orders to attack a specific valuable target
                        //see if we have anything particularly interesting we'd like to do
                        debugCode = 110;
                        var factionData = entity.Planet.GetStanceDataForFaction( AttachedFaction );
                        //Wild hives can influence a planet, but without anything the AI is allowed to shoot,
                        //which is a misuse of the influence notion to my mind. However, since others might do this too,
                        //for non-player-owned planets that are weakly held by enemies, just move on
                        shipsOnHostilePlanets++;
                        if ( controllingFaction.Type != FactionType.Player &&
                             factionData[FactionStance.Hostile].TotalStrength > 500 &&
                             factionData[FactionStance.Hostile].TotalStrength > factionData[FactionStance.Hostile].CloakedStrength) //don't get stuck on all-cloaked forces
                        {
                            shipsInCombatOnHostilePlanets++;
                            InCombatShipsNeedingOrders[entity.Planet].Add(entity);
                            continue;
                        }
                        // else
                        // {
                        //     UnassignedShips.Add( entity );
                        //     UnassignedShipsByPlanet[entity.Planet].Add( entity );
                        //     shipsNeedingOrders++;
                        // }
                    }
                    debugCode = 110;
                    //okay, we're on a friendly planet instead
                    if ( controllingFaction.GetIsFriendlyTowards( AttachedFaction ) )
                    {
                        var factionData = entity.Planet.GetStanceDataForFaction( AttachedFaction );
                        if ( factionData[FactionStance.Hostile].TotalStrength > 3000 ) //if there is at least 3 strength there that we are enemies to
                        {
                            //This entity is on an allied planet with at least 3 enemy strength there, but doesn't have orders to attack a specific valuable target
                            //So go kill some dudes
                            debugCode = 115;
                            InCombatShipsNeedingOrders[entity.Planet].Add( entity );
                            shipsInCombatOnFriendlyPlanets++;
                            continue;
                        }
                    }
                    //This unit doesn't have any active orders and isn't on an enemy planet. Find an enemy planet
                    debugCode = 120;
                    UnassignedShips.Add( entity );
                    UnassignedShipsByPlanet[entity.Planet].Add( entity );
                    shipsNeedingOrders++;
                }
                debugCode = 130;
                if ( UnassignedShips.Count == 0 && World_AIW2.Instance.GameSecond % 60 == 0 )
                {
                    debugCode = 135;
                    if ( tracing )
                        tracingBuffer.Add( "No ships in RelentlessWave.\n" );
                    return;
                }
                debugCode = 150;
                if ( tracing && totalShips > 0 )
                {
                    tracingBuffer.Add( totalShips + " ships with strength " + (totalStrength / 1000) + " in the RelentlessWave right now.\n" );
                    if ( shipsWithMajorOrders > 0 )
                        tracingBuffer.Add( " You have " + shipsWithMajorOrders + " ships with major orders right now ( ie non-frd orders).\n" );
                    if ( shipsEnRouteToOtherPlanets > 0 )
                        tracingBuffer.Add( " You have " + shipsEnRouteToOtherPlanets + " ships en route to other planets right now.\n" );
                    if ( shipsOnHostilePlanets > 0 )
                        tracingBuffer.Add( " You have " + shipsOnHostilePlanets + " ships on hostile planets (" + shipsInCombatOnHostilePlanets+" in combat).\n" );
                    if ( shipsInCombatOnFriendlyPlanets > 0 )
                        tracingBuffer.Add( " You have " + shipsInCombatOnFriendlyPlanets + " ships in combat on friendly planets.\n" );

                }

                //First, handle the ships not in combat
                //Here's the rule. First we pick our preferred planets (player or civil war AI planets), then we sort them
                //We prefer close and weak player planets.

                List<Planet> preferredTargets = Planet.GetTemporaryPlanetList( "AIBorderAgg-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-preferredTargets", 10f );
                if ( preferredTargets == null ) //blocked for teardown/shutdown; bail
                    return;
                //ie minor faction are minor faction targets
                List<Planet> fallbackTargets = Planet.GetTemporaryPlanetList( "AIBorderAgg-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-fallbackTargets", 10f );
                if ( fallbackTargets == null ) //blocked for teardown/shutdown; bail
                {
                    Planet.ReleaseTemporaryPlanetList( preferredTargets );
                    return;
                }

                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in UnassignedShipsByPlanet )
                {
                     debugCode = 200;
                     Planet startPlanet = pair.Key;
                     if ( tracing )
                         tracingBuffer.Add( pair.Value.Count + " ships on " + startPlanet.Name + " are looking for a target\n" );
                     preferredTargets.Clear();
                     fallbackTargets.Clear();
                     debugCode = 210;
                     int strength = 0;
                     for ( int i = 0; i < pair.Value.Count; i++ )
                         strength += pair.Value[i].GetStrengthOfSelfAndContents();
                     foreach ( Planet.PlanetAtHopDistance _phd in startPlanet.PlanetsWithinXHops( -1, delegate ( Planet secondaryPlanet )
                     {
                         debugCode = 230;
                         //don't path through hostile planets.
                         Faction controller = secondaryPlanet.GetControllingOrInfluencingFaction();
                         if ( controller.GetIsFriendlyTowards( AttachedFaction ) ||
                              controller.GetIsNeutralTowards( AttachedFaction ) )
                              return PropogationEvaluation.Yes;

                         if ( controller.GetIsHostileTowards( AttachedFaction ) &&
                              controller.Type == FactionType.Player )
                             return PropogationEvaluation.SelfButNotNeighbors; //don't check past human planets

                         var factionData = secondaryPlanet.GetStanceDataForFaction( AttachedFaction );
                         if ( factionData[FactionStance.Hostile].TotalStrength > 1000 &&
                              factionData[FactionStance.Hostile].TotalStrength > factionData[FactionStance.Hostile].CloakedStrength )
                             return PropogationEvaluation.SelfButNotNeighbors; //don't check past strongly controlled enemy planets (that aren't all cloaked ships)

                         return PropogationEvaluation.Yes; //this is a weakly held, NPC faction. We can choose to path through this to better  targets
                     } ) )
                     {
                         Planet planet = _phd.Planet;
                         debugCode = 220;
                         if ( IsPreferredTarget( planet, AttachedFaction, strength, Context ) )
                         {
                             preferredTargets.Add( planet );
                             continue;
                         }

                         if ( IsFallbackTarget( planet, AttachedFaction, Context ) )
                             fallbackTargets.Add( planet );
                     }
                     debugCode = 240;
                     debugCode = 250;
                     //choose the target. First try to get a preferred planet
                     Planet target = null;
                     if ( preferredTargets.Count > 0 )
                     {
                         debugCode = 260;
                         if ( tracing )
                             tracingBuffer.Add( "\tAttempting to pick a preferredTarget\n" );
                         cb_arbaThis = this;
                         cb_arbaStartPlanet = startPlanet;
                         cb_arbaStrength = strength;
                         debugCode = 270;
                         preferredTargets.Sort( static delegate ( Planet L, Planet R )
                         {
                             //To sort the planets, we factor how scary a planet is and how far it is away. We prefer nearer and weaker targets
                             //TODO if desired: also say "if this has an AIP increaser, want to attack it a bit more"
                             //that would make it a tad more evil
                             int lScore = cb_arbaThis.CalculatePlanetScore( cb_arbaStartPlanet, L, cb_arbaStrength );
                             int rScore = cb_arbaThis.CalculatePlanetScore( cb_arbaStartPlanet, R, cb_arbaStrength );
                             return rScore.CompareTo( lScore );
                         } );
                         if ( tracing )
                         {
                             tracingBuffer.Add( "\tWe have " + preferredTargets.Count + " preferred targets after sorting\n" );
                             for ( int i = 0; i < preferredTargets.Count; i++ )
                                 tracingBuffer.Add( "\t\t" + preferredTargets[i].Name + "\n" );
                         }
                         debugCode = 280;
                         for ( int i = 0; i < preferredTargets.Count; i++ )
                         {
                             int percentToUse = 50;
                             if ( highestDifficulty > 7 )
                                 percentToUse = 80; //more focus on higher difficulties
                             if ( Context.RandomToUse.Next( 0, 100 ) < percentToUse )
                             {
                                 //weighted choice
                                 target = preferredTargets[i];
                                 break;
                             }
                         }
                         if ( target == null ) //if we didn't pick one randomly, just take the best
                             target = preferredTargets[0];
                     }
                     debugCode = 300;
                     if (target == null && fallbackTargets.Count > 0)
                     {
                         debugCode = 310;
                         //this is very similar to the preferredTargets code above
                         if (tracing)
                             tracingBuffer.Add("\tAttempting to pick a fallbackTarget\n");
                         cb_arbaThis = this;
                         cb_arbaStartPlanet = startPlanet;
                         cb_arbaStrength = strength;
                         fallbackTargets.Sort(static delegate (Planet L, Planet R)
                        {
                            int lScore = cb_arbaThis.CalculatePlanetScore(cb_arbaStartPlanet, L, cb_arbaStrength);
                            int rScore = cb_arbaThis.CalculatePlanetScore(cb_arbaStartPlanet, R, cb_arbaStrength);
                            return rScore.CompareTo(lScore);
                        });
                         if (tracing)
                         {
                             tracingBuffer.Add("\tWe have " + fallbackTargets.Count + " fallback targets after sorting\n");
                             for (int i = 0; i < fallbackTargets.Count; i++)
                                 tracingBuffer.Add("\t\t" + fallbackTargets[i].Name).Add("\n");
                         }
                         for (int i = 0; i < fallbackTargets.Count; i++)
                         {
                             if (Context.RandomToUse.Next(0, 100) < 50)
                             {
                                 target = fallbackTargets[i];
                                 break;
                             }
                         }
                         if (target == null) //if we didn't pick one randomly, just take the best
                             target = fallbackTargets[0];

                         debugCode = 400;
                         if (tracing)
                         {
                                 tracingBuffer.Add("\tSending " + pair.Value.Count + " ships from " + startPlanet.Name + " to attack " + target.Name).Add(", taking a fallback path\n");
                         }
                         debugCode = 410;
                         AttackTargetPlanet(startPlanet, target, pair.Value, AttachedFaction, Context, pathingCacheData);
                     }
                     else if (target == null && fallbackTargets.Count == 0 && tracing )
                     {
                         debugCode = 420;
                         if ( tracing )
                             tracingBuffer.Add("\tUnable to find a target the ships on " + startPlanet.Name ).Add("\n");
                     }
                     //we have a target
                     if ( tracing )
                         tracingBuffer.Add("\tSending " + pair.Value.Count + " ships from " + startPlanet.Name + " to attack " + target.Name).Add(", taking a primary-target path\n");
                     AttackTargetPlanet(startPlanet, target, pair.Value, AttachedFaction, Context, pathingCacheData);
                }

                Planet.ReleaseTemporaryPlanetList( preferredTargets );
                Planet.ReleaseTemporaryPlanetList( fallbackTargets );

                debugCode = 500;
                //This is a ship on a planet controlled by our enemies. Pick a target and go after it.
                List<SafeSquadWrapper> targetSquads = GameEntity_Squad.GetTemporarySquadList( "AIBorderAgg-DoLongRangePlanning_OnBackgroundNonSimThread_Subclass-targetSquads", 10f );
                if ( targetSquads == null ) //blocked for teardown/shutdown; bail
                    return;
                var factionExternal = AttachedFaction.BaseInfo;
                Faction aiFaction = AttachedFaction.GetParentFactionOrNull();
                debugCode = 510;

                AISentinelsCoreData sentinelsExternal = null;
                if ( aiFaction != null )
                    sentinelsExternal = aiFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
                else
                {
                    GameEntity_Squad.ReleaseTemporarySquadList( targetSquads );
                    throw new Exception( "Could not find corresponding AI faction for " + AttachedFaction.GetDisplayName() );
                }
                debugCode = 600;
                foreach ( KeyValuePair<Planet, List<SafeSquadWrapper>> pair in InCombatShipsNeedingOrders )
                {
                     debugCode = 700;
                     PlanetFaction pFaction = pair.Key.GetControllingPlanetFaction();
                     Planet planet = pair.Key;
                     targetSquads.Clear();
                     if ( tracing )
                         tracingBuffer.Add( "Finding a target for " + pair.Value.Count + " ships on " + pair.Key.Name ).Add( " if necessary\n" );

                     int rand = Context.RandomToUse.Next( 0, 100 );
                     var factionData = planet.GetStanceDataForFaction( AttachedFaction );
                     int enemyStrength = factionData[FactionStance.Hostile].TotalStrength;
                     int friendlyStrength = factionData[FactionStance.Self].TotalStrength + factionData[FactionStance.Friendly].TotalStrength;

                     if ( enemyStrength < friendlyStrength / 10 ) //Relentless waves can tachyon blast planets to prevent a small number of cloaked units from disrupting things. This is particularly possible with civilian industry cloaked defensive structures
                         FactionUtilityMethods.Instance.TachyonBlastPlanet( planet, AttachedFaction, Context );


                     if ( planet.GetControllingFactionType() == FactionType.Player )
                     {
                         //This is a player planet, so lets see if we can do anything clever/sneaky
                         Planet kingPlanet;
                         if ( sentinelsExternal.AIDifficulty.AllowedToGoForPlayerHomeworld &&
                              FactionUtilityMethods.Instance.IsPlanetAdjacentToPlayerKing( planet, out kingPlanet ) )
                         {
                             //First, if we are adjacent to a player homeworld then go for that if we can
                             var neighborFactionData = kingPlanet.GetStanceDataForFaction( AttachedFaction );
                             StrengthData_PlanetFaction_Stance neighborHostileStrengthData = neighborFactionData[FactionStance.Hostile];
                             int neighborHostileStrengthTotal = neighborHostileStrengthData.TotalStrength;
                             if ( neighborHostileStrengthTotal < (friendlyStrength) / 2 )
                             {
                                 //if we are too much weaker than the AI homeworld, don't bother. Only if we might make things interesting
                                 GameEntity_Other thisWormhole = planet.GetWormholeTo( kingPlanet );
                                 bool foundBlockingShield = FactionUtilityMethods.Instance.IsShieldBlockingWormholeToPlanet( planet, kingPlanet, AttachedFaction );
                                 if ( !foundBlockingShield )
                                 {
                                     if ( tracing )
                                         tracingBuffer.Add( "\tWe are going to attack the player king on " ).Add( kingPlanet.Name ).Add( "\n" );

                                     GameCommand sneakCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_AIRaidKing], GameCommandSource.AnythingElse );
                                     debugCode = 226;
                                     for ( int j = 0; j < pair.Value.Count; j++ )
                                     {
                                         sneakCommand.RelatedEntityIDs.Add( pair.Value[j].PrimaryKeyID );
                                     }

                                     debugCode = 228;
                                     if ( sneakCommand != null && sneakCommand.RelatedEntityIDs.Count > 0 )
                                     {
                                         sneakCommand.RelatedString = "AI_WAVE_GOKING";
                                         sneakCommand.ToBeQueued = false;
                                         sneakCommand.RelatedIntegers.Add( kingPlanet.Index );
                                         World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, sneakCommand, playAudioEffectForCommand );
                                         continue;
                                     }
                                 }
                             }
                         }
                         if ( rand < sentinelsExternal.AIDifficulty.AttackPercentCommandStation )
                         {
                             debugCode = 215;
                             GameEntity_Squad commandStation = planet.GetCommandStationOrNull();
                             if ( commandStation != null )
                             {
                                 debugCode = 216;
                                 GameCommand commandStationAttackCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.Attack], GameCommandSource.AnythingElse );
                                 commandStationAttackCommand.ToBeQueued = false;

                                 commandStationAttackCommand.RelatedIntegers4.Add( commandStation.PrimaryKeyID );
                                 debugCode = 217;
                                 for ( int j = 0; j < pair.Value.Count; j++ )
                                 {
                                     commandStationAttackCommand.RelatedEntityIDs.Add( pair.Value[j].PrimaryKeyID );
                                 }
                                 debugCode = 219;
                                 World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, commandStationAttackCommand, playAudioEffectForCommand );
                                 if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Sending " + commandStationAttackCommand.RelatedEntityIDs.Count + " unit to attack command station" );
                                 commandStationAttackCommand = null;
                                 continue;
                             }
                         }
                         if ( enemyStrength > friendlyStrength )
                         {
                             //if we don't think we can comfortably win this (and remember, we are at a disadvantage when attacking a player)
                             //then see if we can bypass and find a weaker target
                             debugCode = 220;
                             rand = Context.RandomToUse.Next( 0, 100 ); //recalculate the random number
                             if ( rand < sentinelsExternal.AIDifficulty.AttackPercentFindWeakerTarget )
                             {
                                 debugCode = 221;
                                 //If this planet seems pretty tough for me, see if there are any adjacent weaker player planets and go for those
                                 //TODO: we should actually use a List here and select randomly in case there are multiple good options
                                 //We might also want to enhance Helper_RetreatThreat to incorporate this style of 'sneaking past player defenses'
                                 Planet newTarget = null;
                                 int weakestPlanetNeighborStrength = 0;
                                 foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                                 {
                                     if ( neighbor.GetControllingFactionType() != FactionType.Player )
                                         continue;
                                     debugCode = 222;
                                     var neighborFactionData = neighbor.GetStanceDataForFaction( AttachedFaction );
                                     StrengthData_PlanetFaction_Stance neighborHostileStrengthData = neighborFactionData[FactionStance.Hostile];
                                     int neighborHostileStrengthTotal = neighborHostileStrengthData.TotalStrength - friendlyStrength;
                                     if ( neighborHostileStrengthTotal > enemyStrength )
                                         continue; //not interested in more heavily defeneded planets

                                     debugCode = 223;
                                     if ( neighborHostileStrengthTotal < weakestPlanetNeighborStrength && !FactionUtilityMethods.Instance.IsShieldBlockingWormholeToPlanet( planet, neighbor, AttachedFaction ) )
                                     {
                                         weakestPlanetNeighborStrength = neighborHostileStrengthTotal;
                                         newTarget = neighbor;
                                     }
                                 }
                                 debugCode = 224;
                                 if ( newTarget != null )
                                 {
                                     debugCode = 225;
                                     if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Threat bypass to weaker planet: " + newTarget.Name ).Add( "\n" );
                                     GameCommand sneakCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_AIRaidKing], GameCommandSource.AnythingElse );
                                     for ( int j = 0; j < pair.Value.Count; j++ )
                                     {
                                         sneakCommand.RelatedEntityIDs.Add( pair.Value[j].PrimaryKeyID );
                                     }

                                     debugCode = 228;
                                     if ( sneakCommand.RelatedEntityIDs.Count > 0 )
                                     {
                                         sneakCommand.RelatedString = "AI_T_CHUNKS";
                                         sneakCommand.ToBeQueued = false;
                                         sneakCommand.RelatedIntegers.Add( newTarget.Index );
                                         World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, sneakCommand, playAudioEffectForCommand );
                                         sneakCommand = null;
                                         continue;
                                     }
                                 }
                             }
                         }
                     }
                     if ( tracing )
                         tracingBuffer.Add( "No fancy behaviours. Find some targets\n" );

                     //We haven't done any of the fancy behaviours, so the default behaviour is "Just fight"
                     //Exception: if the player has anything particularly toothsome on this planet, always kill that first
                     foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.KingUnitsOnly ) )
                     {
                          if ( !entity.PlanetFaction.Faction.GetIsHostileTowards( AttachedFaction ) )
                              continue;
                          targetSquads.Add( entity );
                          break; ;
                      }

                     if ( targetSquads.Count == 0 )
                     {
                         //if we already had a target then it's a king, so don't bother
                         foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.AIPOnDeath ) )
                         {
                              if ( !entity.PlanetFaction.Faction.GetIsHostileTowards( AttachedFaction ) )
                                  continue;

                              targetSquads.Add( entity );
                          }
                     }
                     if ( targetSquads.Count > 0 )
                     {
                         //we know we have a tasty target
                         debugCode = 610;
                         GameEntity_Squad target = targetSquads[Context.RandomToUse.Next( 0, targetSquads.Count )].GetSquad();
                         if ( target == null )
                             continue;
                         if ( tracing )
                         {
                             tracingBuffer.Add( "We are attacking " + target.ToStringWithPlanet() + ", one of " + targetSquads.Count + " target(s).\n" );
                             bool debug = false;
                             if ( tracing && debug )
                             {
                                 for ( int i = 0; i < targetSquads.Count; i++ )
                                 {
                                     tracingBuffer.Add( i + ": " + targetSquads[i].ToStringWithPlanet() + ".\n" );
                                 }
                             }
                         }

                         GameCommand attackCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.Attack], GameCommandSource.AnythingElse );
                         attackCommand.RelatedIntegers4.Add( target.PrimaryKeyID );
                         for ( int j = 0; j < pair.Value.Count; j++ )
                         {
                             attackCommand.RelatedEntityIDs.Add( pair.Value[j].PrimaryKeyID );
                         }

                         World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, attackCommand, false );
                         continue;
                     }

                     if ( rand < sentinelsExternal.AIDifficulty.AttackPercentMetalGenerator )
                     {
                         //The AI is allowed to send its fast and cloaked units after your metal generators
                         GameEntity_Squad target = FactionUtilityMethods.Instance.GetRandomMetalGeneratorOnPlanet( planet, Context, WorkingMetalGeneratorList, true );
                         if ( target == null )
                             continue;
                         GameCommand attackCommand = null;
                         for ( int j = 0; j < pair.Value.Count; j++ )
                         {
                             GameEntity_Squad entity = pair.Value[j].GetSquad();
                             if ( entity == null )
                                 continue;
                             if ( entity.GetMaxCloakingPoints() > 0 ||
                                  entity.CalculateSpeed( true ) >= 1300 )
                             {
                                 if ( attackCommand == null )
                                     attackCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.Attack], GameCommandSource.AnythingElse );
                                 attackCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                             }
                         }
                         if ( attackCommand != null )
                         {
                             if ( tracing )
                                 tracingBuffer.Add( attackCommand.RelatedEntityIDs.Count + " Fast/cloaked units are going after " + target.ToStringWithPlanet() );
                             attackCommand.RelatedIntegers4.Add( target.PrimaryKeyID );
                             World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, attackCommand, false );
                         }
                     }
                     if ( tracing )
                         tracingBuffer.Add( "Very boring; just fight very genericallys\n" );
                }

                GameEntity_Squad.ReleaseTemporarySquadList( targetSquads );
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in RelentlessWaveLogic LRP. debugCode " + debugCode + " " + e.ToString(), Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();

                if ( tracing )
                {
                    #region Tracing
                    if ( tracing && !tracingBuffer.GetIsEmpty() ) ArcenDebugging.ArcenDebugLogSingleLine( "RelentlessWaveLogic " + AttachedFaction.FactionIndex + " LRP. " + tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                }
            }
        }

        public bool IsPreferredTarget(Planet planet, Faction faction, int strength, ArcenLongTermIntermittentPlanningContext Context)
        {
            //Whether this is an enemy controlled planet. Note that we try not to overkill planets too badly
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.RelentlessWave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIBorderAgg-IsPreferredTarget-trace", 10f ) : null;
            if ( tracing  )
                tracingBuffer.Add("\tchecking " + planet.Name + " for preferred target status\n");
            Faction controllingFaction = planet.GetControllingFaction();
            if ( controllingFaction.Type != FactionType.Player ||
                 (faction.InCivilWarMode && controllingFaction.Type != FactionType.AI) )
            {
                if ( tracing )
                {
                    tracingBuffer.Add( "\t\tNot owned by primary enemy (ie player or AI on civil war). Owned by " + controllingFaction.GetDisplayName() + "\n" );
                }
                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
                return false;
            }

            if ( !controllingFaction.GetIsHostileTowards(faction) )
            {
                if ( tracing ) 
                {
                    tracingBuffer.Add("\t\t" + controllingFaction.GetDisplayName() + " is friendly\n");
                }
                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
                return false;
            }
            var factionData = planet.GetStanceDataForFaction( faction );
            int enemyStrength = factionData[FactionStance.Hostile].TotalStrength;
            int friendlyStrength = factionData[FactionStance.Friendly].TotalStrength + factionData[FactionStance.Self].TotalStrength ;
            if ( enemyStrength * 5 < friendlyStrength )
            {
                if ( tracing ) 
                {
                    tracingBuffer.Add("\t\t" + enemyStrength + " < " + (friendlyStrength * 5)+ ", so discard\n");
                }
                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
                return false; //if we outnumber 7 to 1, don't bother attacking
            }

            if ( tracing )
            {               
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
            }
            if (enemyStrength > friendlyStrength + strength)
            {
                if ( tracing ) 
                    tracingBuffer.Add("\t\twe would p;refer something easier discard\n");

                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
                return false; //we would prefer to attack something easier
            }
            return true;
        }
        public bool IsFallbackTarget(Planet planet, Faction faction, ArcenLongTermIntermittentPlanningContext Context)
        {
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.RelentlessWave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIBorderAgg-IsFallbackTarget-trace", 10f ) : null;
            Faction controllingFaction = planet.GetControllingOrInfluencingFaction();
            if ( tracing )
                tracingBuffer.Add("\tChecking fallback target status for " + planet.Name);
            if ( !controllingFaction.GetIsHostileTowards(faction) )
            {
                if ( tracing )
                {
                    tracingBuffer.Add("\t\tthis planet is friendly, invalid target");
                }
                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
                return false;
            }
                
            var factionData = planet.GetStanceDataForFaction( faction );
            int enemyStrength = factionData[FactionStance.Hostile].TotalStrength;
            int friendlyStrength = factionData[FactionStance.Friendly].TotalStrength + factionData[FactionStance.Self].TotalStrength ;
            if ( enemyStrength < friendlyStrength * 7 )
            {
                if ( tracing )
                {
                    tracingBuffer.Add( "\t\t" + enemyStrength + " < " + (friendlyStrength * 7) + ", invalid target\n" );
                }
                FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
                return false; //if we outnumber 7 to 1, don't bother attacking
            }

            if ( tracing )
            {
                tracingBuffer.Add( "\t\t is a valid fallback target\n" );
            }
            FactionUtilityMethods.Instance.FinishTracing(tracingBuffer);
            return true;
        }
        public int CalculatePlanetScore ( Planet startPlanet, Planet targetPlanet, int myStrength )
        {
            //how much do my ships on startPlanet want to attack targetPlanet?
            var factionData = targetPlanet.GetStanceDataForFaction( AttachedFaction );
            int hops = startPlanet.GetHopsTo(targetPlanet);
            int enemyStrength = factionData[FactionStance.Hostile].TotalStrength;
            int friendlyStrength = factionData[FactionStance.Friendly].TotalStrength +
                factionData[FactionStance.Self].TotalStrength + myStrength;
            int score = 0;
            if ( hops < 3 && myStrength > enemyStrength && friendlyStrength < enemyStrength )
                score = 20; //nearby target where my force is going to make a huge difference
            else if ( hops < 3 && myStrength + friendlyStrength > enemyStrength )
                score = 15;
            else if ( enemyStrength < friendlyStrength + myStrength )
                score = 10;
            else
                score = 5;
            score -= hops;
            return score;
        }
        public int CalculateSpeed(List<SafeSquadWrapper> ships, ArcenLongTermIntermittentPlanningContext Context)
        {
            //Return the speed we want these ships to use. It's "a bit faster than the average speed, and at least 500".
            //We make the speeds all a little bit different to make it less obvious to the player that we are using speed groups (since
            //ships from multiple planets will be going by at the same time, and moving at different speeds)
            if ( ships.Count == 0 )
                return 0;
            int maxSpeed = 0;
            Int64 totalSpeed = 0;
            for ( int i = 0; i < ships.Count; i++ )
            {
                if ( ships[i].DataForMark == null ) //if we see more errors here then we'll need to add a bit more thread safety
                    continue;
                int newSpeed = ships[i].DataForMark.Speed;
                totalSpeed += newSpeed;
                if ( maxSpeed < newSpeed )
                    maxSpeed = newSpeed;

            }
            int average = (Int32)( totalSpeed / (Int64)ships.Count );

            average += average / 9; //a bit faster than average
            if ( average < 500 )
                average = 500;
            average += Context.RandomToUse.Next(0, 50); //a little more randomness

            //if we only have one ship type, then they just go the speed the go, for example
            if ( average > maxSpeed )
                average = maxSpeed;

            return average;
        }
        public void AttackTargetPlanet(Planet start, Planet destination, List<SafeSquadWrapper> ships, Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            if ( ships.Count <= 0 )
                return;
            int debugCode = 0;
            bool tracing = this.tracing_longTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.RelentlessWave );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AIBorderAgg-AttackTargetPlanet-trace", 10f ) : null;
            try
            {
                debugCode = 100;
                PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "RelentlessOrBorderAttackTargetPlanet", start, destination, PathingMode.Safest, Context, PathCacheData );
                int gatheringDistance = -1;
                debugCode = 200;
                if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                {
                    debugCode = 400;
                    if ( tracing )
                        tracingBuffer.Add("\tpath count " + pathCache.PathToReadOnly.Count + " and gatheringDistance " + gatheringDistance );
                    debugCode = 500;
                    if ( pathCache.PathToReadOnly.Count > gatheringDistance || gatheringDistance == -1 )
                    {
                        debugCode = 600;
                        //if we are coming from a long ways away, fly a little faster so the RelentlessWave is less spread out
                        int speedToUse = CalculateSpeed(ships, Context);
                        GameCommand speedCommand = GameCommand.Create ( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.CreateSpeedGroup_FireteamAttack],  GameCommandSource.AnythingElse);
                        for (int j = 0; j < ships.Count; j++)
                            speedCommand.RelatedEntityIDs.Add(ships[j].PrimaryKeyID);
                        int exoGroupSpeed = speedToUse;
                        speedCommand.RelatedBool = true;
                        speedCommand.RelatedIntegers.Add(exoGroupSpeed);
                        World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, speedCommand, false );
                    }
                    debugCode = 700;
                    GameCommand command = GameCommand.Create ( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_UtilRaidSpecific], GameCommandSource.AnythingElse );
                    command.RelatedString = "RelentlessWave_Planetary_Movement";
                    for ( int k = 0; k < ships.Count; k++ )
                        command.RelatedEntityIDs.Add( ships[k].PrimaryKeyID );
                    //determine whether we are actually going to the target, or just getting close so we are ready to strike
                    debugCode = 800;
                    if ( gatheringDistance == -1 )
                    {
                        debugCode = 900;
                        if ( tracing )
                            tracingBuffer.Add("\tJust attack the planet you are on now, please\n");

                        for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                    }
                    else
                    {
                        debugCode = 1000;
                        int hopsForwardToMove = pathCache.PathToReadOnly.Count - gatheringDistance;
                        if ( hopsForwardToMove >= pathCache.PathToReadOnly.Count || hopsForwardToMove < 0 )
                            throw new Exception("huh?");
                        if ( tracing )
                            tracingBuffer.Add("\tMove forward only " + hopsForwardToMove + " hops from " + start.Name + " -> " + pathCache.PathToReadOnly[hopsForwardToMove - 1].Name + " en route to " + destination.Name + "\n");
                        debugCode = 1100;
                        for ( int k = 0; k < hopsForwardToMove; k++ )
                            command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                    }

                    World_AIW2.Instance.QueueGameCommand( this.AttachedFaction, command, false );
                }
            } catch(Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Hit exception in RelentlessWave AttackTargetPlanet debugCode " + debugCode + " " + e.ToString() , Verbosity.ShowAsError );
            }
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
            }
        }

        public abstract bool GetFactionMatchesMyInternalName( Faction faction );
    }
}
