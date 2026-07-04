using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public abstract class BaseAITypeImplementation : IAITypeImplementation
    {
        private EnumIndexedArray<AIBudgetType, FInt> workingAIPToQuasiAllocate = EnumIndexedArray<AIBudgetType, FInt>.Create_WillNeverBeGCed( true, FInt.Zero, "BaseAITypeImplementation-workingAIPToQuasiAllocate" );
        public void SetSpendingRatios( Faction Faction, AIBudgetCurrentConfiguration Budget, FInt AtAip )
        {
            workingAIPToQuasiAllocate.Clear();

            AllocateSpendingRatios_AnySituation( Faction, workingAIPToQuasiAllocate, AtAip );

            FInt planetsWorthOfAIP = AtAip / 20;
            if ( planetsWorthOfAIP < FInt.One )
                planetsWorthOfAIP = FInt.One;

            for ( AIBudgetType i = AIBudgetType.None; i < AIBudgetType.Length; i++ )
                Budget[i].BudgetPortion = workingAIPToQuasiAllocate[i] / planetsWorthOfAIP;

            if ( planetsWorthOfAIP > 5 )
                Budget[AIBudgetType.Wave].IntervalMultiplier = (FInt)2;
            else if ( planetsWorthOfAIP > 1 )
                Budget[AIBudgetType.Wave].IntervalMultiplier = FInt.One + ( planetsWorthOfAIP / 5 );
            else
                Budget[AIBudgetType.Wave].IntervalMultiplier = FInt.One;
        }

        public void AllocateSpendingRatios_AnySituation( Faction Faction, EnumIndexedArray<AIBudgetType,FInt> aipToQuasiAllocate )
        {
            AllocateSpendingRatios_AnySituation( Faction, aipToQuasiAllocate, GlobalAIWorldBaseInfo.Instance.AIProgress_Effective );
        }

        public virtual void AllocateSpendingRatios_AnySituation( Faction Faction, EnumIndexedArray<AIBudgetType,FInt> aipToQuasiAllocate, FInt aip )
        {
            FInt bottomOfStep;
            FInt topOfStep;

            EnumIndexedArray<AIBudgetType, FInt> aipRatioForStep = AIBudgetItem.GetTemporaryBudgetRatioArray( "BaseAITypeImplementation-AllocateSpendingRatios_AnySituation-aipRatioForStep", 10f );
            if ( aipRatioForStep == null ) //blocked for teardown/shutdown; bail
                return;

            /* The bottom/top of steps code here allows the spending to have different ratios depending on how much
               total AI Progress there is. Each AIP generates the same amount of income, but we allocate it differently
               depending on the AIP. Lets say I have 200 AIP as an example. So income for 50 of that AIP is handled
               in the top block. Then the income for the next 100 AIP is handled in the second block (so the second block's allocation
               ratios have more weight). Then the final 50 AIP's worth of income is handled in the third block.
            */
            bottomOfStep = FInt.Zero;
            topOfStep = FInt.FromParts( 2, 500 ); //50 AIP
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 325 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 225 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 150 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 250 ); //excess donated to reinforcements
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 050 ); //excess donated to hunter
            // no point in donating to Reconquest since reconquest doesn't unlock till later
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = FInt.FromParts( 7, 500 ); //150 AIP
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 025 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 075 );
            aipRatioForStep[AIBudgetType.HunterFleet] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 100 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = FInt.FromParts( 12, 000 ); //240 AIP
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 325 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 075 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 150 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 150 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.HunterFleet] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 050 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = FInt.FromParts( 30, 000 ); //600 AIP
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 250 );
            aipRatioForStep[AIBudgetType.BorderAggression] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 125 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 025 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            bottomOfStep = topOfStep;
            topOfStep = (FInt)( -1 );
            aipRatioForStep[AIBudgetType.Wave] = FInt.FromParts( 0, 200 );
            aipRatioForStep[AIBudgetType.Reinforcement] = FInt.FromParts( 0, 050 );
            aipRatioForStep[AIBudgetType.CPA] = FInt.FromParts( 0, 375 );
            aipRatioForStep[AIBudgetType.Warden] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.PraetorianGuard] = FInt.FromParts( 0, 150 );
            aipRatioForStep[AIBudgetType.Reconquest] = FInt.FromParts( 0, 100 );
            aipRatioForStep[AIBudgetType.WormholeInvasion] = FInt.FromParts( 0, 050 );
            AllocateAIPWithinStep( aipToQuasiAllocate, aipRatioForStep, bottomOfStep, topOfStep, aip );

            AIBudgetItem.ReleaseTemporaryBudgetRatioArray( aipRatioForStep );
        }

        protected static void AllocateAIPWithinStep( EnumIndexedArray<AIBudgetType,FInt> aipToQuasiAllocate, EnumIndexedArray<AIBudgetType,FInt> aipRatioForStep, FInt bottomOfStep, FInt topOfStep, FInt aip )
        {
            FInt planetsWorthOfAIP = aip / 20;
            if ( planetsWorthOfAIP <= bottomOfStep )
                return;
            FInt aipForThisStep = planetsWorthOfAIP - bottomOfStep;
            if ( topOfStep >= 0 )
                aipForThisStep = Mat.Min( aipForThisStep, ( topOfStep - bottomOfStep ) );
            for ( AIBudgetType i = AIBudgetType.None; i < AIBudgetType.Length; i++ )
                aipToQuasiAllocate[i] += ( aipForThisStep * aipRatioForStep[i] );
        }

        private static int DelegateHelper_result;
        private static Faction DelegateHelper_faction;
        //private static Planet DelegateHelper_planet;
        public int GetRaidDesirability( Faction Faction, Planet planet )
        {
            DelegateHelper_result = 0;
            if ( Faction == null || planet == null )
                return DelegateHelper_result;

            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( entity.TypeData.SpecialType == SpecialEntityType.HumanHomeCommand )
                    DelegateHelper_result += 10000;
            }

            DelegateHelper_faction = Faction;
            //DelegateHelper_planet = planet;
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.Claimables ) )
            {
                if ( !entity.GetIsHostileTowards_Safe( DelegateHelper_faction ) )
                    continue;
                int energyProduced = entity.GetFullyMultipliedEnergyToProduce().IntValue;
                if ( energyProduced > 0 )
                    DelegateHelper_result += (energyProduced * 100 );
                int fuelProduced = entity.GetFullyMultipliedFuelArgonToProduce().IntValue + entity.GetFullyMultipliedFuelRadonToProduce().IntValue + entity.GetFullyMultipliedFuelXenonToProduce().IntValue;
                if ( fuelProduced > 0 )
                    DelegateHelper_result += (fuelProduced * 500 );
                if ( entity.DataForMark.MetalCostToClaim > 0 )
                    DelegateHelper_result += 10000; //Chris notes: this is a hacky sort of thing that is left over from pre-0.900 days, just converted to a direct number now.
            }
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.EnergyProducers ) )
            {
                //any player planet that produces energy might be worth grabbing
                if ( !entity.GetIsHostileTowards_Safe( DelegateHelper_faction ) )
                    continue;
                DelegateHelper_result += 100;
            }
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.CriticalInfrastructure ) )
            {
                //Anything marked this is a particularly shiny target that may not be included in the other checks (Such as a Necromancer Amplifier).
                if ( !entity.GetIsHostileTowards_Safe( DelegateHelper_faction ) )
                    continue;
                DelegateHelper_result += 10000;
            }
            DelegateHelper_faction = null;
            //DelegateHelper_planet = null;

            return DelegateHelper_result;
        }

        public int GetRaidTraversalDifficulty( Faction Faction, Planet planet )
        {
            bool debug = false;
            int debugCode = 0;
            int result = ExternalConstants.Instance.Balance_BaseDifficultyForRaidTraversal; // basic difficulty cost of travelling a hop
            try{
                debugCode = 100;
                if ( planet == null )
                    return result;
                if(debug)
                    ArcenDebugging.ArcenDebugLogSingleLine("get raid traversal difficulty for " + planet.Name, Verbosity.DoNotShow );
                debugCode = 200;
                AITypeData aiType = Faction.TryGetAISentinelsCoreData().SentinelInfo.AIType;
                debugCode = 300;
                StrengthData_PlanetFaction_Stance hostileData = planet.GetPlanetFactionForFaction( Faction ).DataByStance[FactionStance.Hostile];
                StrengthData_PlanetFaction_Stance friendlyData = planet.GetPlanetFactionForFaction( Faction ).DataByStance[FactionStance.Friendly];
                StrengthData_PlanetFaction_Stance selfData = planet.GetPlanetFactionForFaction( Faction ).DataByStance[FactionStance.Self];
                debugCode = 400;
                if(debug)
                    ArcenDebugging.ArcenDebugLogSingleLine("Hostile total strength: " + hostileData.TotalStrength + " on " + planet.Name, Verbosity.DoNotShow );
                result += hostileData.TotalStrength;
                int divisor = 2;
                debugCode = 500;
                for ( int i = 1; i < hostileData.UnengagedMobileStrengthByHopCount.Length; i++ ) // start with 1 to ignore stuff on the planet itself; that's already counted
                {
                    debugCode = 600;
                    result += hostileData.UnengagedMobileStrengthByHopCount[i] / divisor;
                    divisor *= 3; //increase the backoff to make the AI a bit less cowardly
                }
                debugCode = 700;
                result = (result * aiType.RatioOfFearOfRemoteEnemies).IntValue;
                if(debug)
                    ArcenDebugging.ArcenDebugLogSingleLine("result after adjusting for hostile mobiles: " + result, Verbosity.DoNotShow );
                result -= (friendlyData.TotalStrength + selfData.TotalStrength);
                debugCode = 800;
                if(result <= 0)
                    result = 0;
                if(debug)
                    ArcenDebugging.ArcenDebugLogSingleLine("result after adjusting for my strength: " + result + " my strength " + (friendlyData.TotalStrength + selfData.TotalStrength), Verbosity.DoNotShow );
            } catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in GetRaidTraversalDifficulty debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return result;
        }

        public virtual AIDefensePlacer GetAIDefensePlacerForPlanet( Faction faction, Planet planet, ArcenHostOnlySimContext Context )
        {
            var seed = Context.RandomToUse.GetCurrentSeed();
            Context.RandomToUse.ReinitializeWithSeed( planet.Index + World_AIW2.Instance.Setup.MapConfig.Seed );
            var result =  AIDefensePlacerTable.Instance.Rows[Context.RandomToUse.Next( 0, AIDefensePlacerTable.Instance.Rows.Count )];
            Context.RandomToUse.ReinitializeWithSeed(seed);
            return result;
        }
                
        public virtual void AssignDefenseValuesTo_HostOnly( List<Planet> planets, ArcenHostOnlySimContext Context )
        {
            if ( Context == null ) //client
                return;

            int debugStage = 0;
            try
            {
                if ( ArcenNetworkAuthority.DesiredStatus == DesiredMultiplayerStatus.Client )
                    return;
                if ( planets.Count <= 0 )
                {
                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( "No planets in list to AssignDefenseValuesTo_HostOnly!" );
                    return;
                }

                int strongestAI = FactionUtilityMethods.Instance.GetHighestAIDifficulty();

                if ( World_AIW2.Instance.GetIsTutorial() )
                {
                    if ( MapgenLogger.IsActive )
                        MapgenLogger.Log( "Tutorial-Style AssignDefenseValuesTo_HostOnly" );

                    #region Make Sure All The GuardPostAndCommandPlacers Are Set
                    for ( int i = 0; i < planets.Count; i++ )
                    {
                        Planet planet = planets[i];
                        planet.SetGuardPostAndCommandPlacerFromPlanetStats( strongestAI, Context );
                    }
                    #endregion
                    return;
                }

                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "Non-Tutorial-Style AssignDefenseValuesTo_HostOnly" );

                debugStage = 1000;
                bool markLevelDebug = false;
                #region set ambient, max, min levels
                int ambientMin = 3;
                //int ambientMax = 5;
                int max = 7;
                int min = 1;
                #endregion

                debugStage = 2000;
                //Check for the strongest AI in the game; for particularly low values we will
                //seed a lot more low-mark planets near the player
                bool doExtraMark3Ring = false;
                debugStage = 3000;

                #region set everything to ambientMin
                List<Planet> humanHomeworlds = Planet.GetTemporaryPlanetList( "BaseAITypeImp-AssignDefenseValuesTo_HostOnly-humanHomeworlds", 10f );
                if ( humanHomeworlds == null ) //blocked for teardown/shutdown; bail
                    return;
                List<Planet> aiHomeworlds = Planet.GetTemporaryPlanetList( "BaseAITypeImp-AssignDefenseValuesTo_HostOnly-aiHomeworlds", 10f );
                if ( aiHomeworlds == null ) //blocked for teardown/shutdown; bail
                {
                    Planet.ReleaseTemporaryPlanetList( humanHomeworlds ); //release already-acquired temp before bailing
                    return;
                }

                if ( MapgenLogger.IsActive )
                    MapgenLogger.Log( "strongestAI: " + strongestAI );

                foreach ( Planet planet in planets )
                {
                    //planet.AfterMapGenDebugReport += "\nSTART NEW AssignDefenseValuesTo_HostOnly";
                    //planet.AfterMapGenDebugReport += "\nOriginalHopsToHumanHomeworld: " + planet.OriginalHopsToHumanHomeworld;
                    if ( planet.PopulationType == PlanetPopulationType.HumanHomeworld ||
                         planet.PopulationType == PlanetPopulationType.ArkEmpireHumanHomeworld )
                    {
                        humanHomeworlds.Add( planet );
                        //planet.AfterMapGenDebugReport += "\nIs Human Homeworld";
                    }
                    else if ( planet.PopulationType == PlanetPopulationType.AIHomeworld )
                    {
                        aiHomeworlds.Add( planet );
                        //planet.AfterMapGenDebugReport += "\nIs AI Homeworld";
                    }

                    if ( planet.Mapgen_HasMarkLevelAlreadyBeenSet )
                        continue;
                    planet.Mapgen_WorkingMarkLevel = ambientMin;
                }
                if ( humanHomeworlds.Count == 0 && aiHomeworlds.Count == 0 )
                {
                    if ( MapgenLogger.IsActive )
                    {
                        MapgenLogger.Log( "No human homeworlds or AI homeworlds in the following list:" );
                        foreach ( Planet planet in planets )
                            MapgenLogger.Log( planet.Name + " (" + planet.PopulationType + ") " + planet.Mapgen_WorkingMarkLevel );
                    }

                    Planet.ReleaseTemporaryPlanetList( humanHomeworlds );
                    Planet.ReleaseTemporaryPlanetList( aiHomeworlds );
                    return;
                }
                #endregion

                debugStage = 4000;
                if ( MapgenLogger.IsActive )
                {
                    MapgenLogger.Log( "aiHomeworlds: " + aiHomeworlds.Count );
                    MapgenLogger.Log( "humanHomeworlds: " + humanHomeworlds.Count );
                }

                //Basic overview: a loop runs over all the player homeworlds, then sets the planets near them to be Low mark
                //A similar loop runs over all the ai homeworld and sets the planets near them to be High mark
                //Finally, all the other planets are randomly assigned based on these percentages
                byte percentMark2 = 5;
                byte percentMark3 = 25;
                byte percentMark4 = 35;
                byte percentMark5 = 30;
                byte percentMark6 = 5;
                if ( strongestAI <= 5 )
                {
                    percentMark2 += 10;
                    percentMark3 += 5;
                    percentMark4 -= 5;
                    percentMark5 -= 5;
                    percentMark6 -= 5;
                }
                if ( strongestAI >= 9 )
                {
                    percentMark6 += 5;
                    percentMark2 = 0;
                }

                debugStage = 5000;
                #region set each human homeworld to have a certain number of mkI neighbors, and then a ring of 2s around those, then a ring of 3s around those, and alternating 3s and 4s around that
                {
                    int numberOfMark1s = (planets.Count / 20) - 1;
                    if ( numberOfMark1s < 2 )
                        numberOfMark1s = 2;
                    if ( strongestAI == 1 )
                        numberOfMark1s *= 6;
                    else if ( strongestAI <= 3 )
                        numberOfMark1s *= 4;
                    else if ( strongestAI <= 5 )
                        numberOfMark1s *= 2;
                    if ( strongestAI <= 5 )
                        doExtraMark3Ring = true;

                    List<Planet> mark1s = Planet.GetTemporaryPlanetList( "BaseAITypeImp-AssignDefenseValuesTo_HostOnly-mark1s", 10f );
                    if ( mark1s == null ) //blocked for teardown/shutdown; bail
                    {
                        Planet.ReleaseTemporaryPlanetList( humanHomeworlds ); //release already-acquired temps before bailing
                        Planet.ReleaseTemporaryPlanetList( aiHomeworlds );
                        return;
                    }
                    List<Planet> mark2s = Planet.GetTemporaryPlanetList( "BaseAITypeImp-AssignDefenseValuesTo_HostOnly-mark2s", 10f );
                    if ( mark2s == null ) //blocked for teardown/shutdown; bail
                    {
                        Planet.ReleaseTemporaryPlanetList( humanHomeworlds ); //release already-acquired temps before bailing
                        Planet.ReleaseTemporaryPlanetList( aiHomeworlds );
                        Planet.ReleaseTemporaryPlanetList( mark1s );
                        return;
                    }
                    List<Planet> mark3s = Planet.GetTemporaryPlanetList( "BaseAITypeImp-AssignDefenseValuesTo_HostOnly-mark3s", 10f );
                    if ( mark3s == null ) //blocked for teardown/shutdown; bail
                    {
                        Planet.ReleaseTemporaryPlanetList( humanHomeworlds ); //release already-acquired temps before bailing
                        Planet.ReleaseTemporaryPlanetList( aiHomeworlds );
                        Planet.ReleaseTemporaryPlanetList( mark1s );
                        Planet.ReleaseTemporaryPlanetList( mark2s );
                        return;
                    }
                    List<Planet> adjacentPlanets = Planet.GetTemporaryPlanetList( "BaseAITypeImp-AssignDefenseValuesTo_HostOnly-adjacentPlanets", 10f );
                    if ( adjacentPlanets == null ) //blocked for teardown/shutdown; bail
                    {
                        Planet.ReleaseTemporaryPlanetList( humanHomeworlds ); //release already-acquired temps before bailing
                        Planet.ReleaseTemporaryPlanetList( aiHomeworlds );
                        Planet.ReleaseTemporaryPlanetList( mark1s );
                        Planet.ReleaseTemporaryPlanetList( mark2s );
                        Planet.ReleaseTemporaryPlanetList( mark3s );
                        return;
                    }

                    bool next2NeighborIs4 = false;
                    debugStage = 5100;

                    for ( int i = 0; i < humanHomeworlds.Count; i++ )
                    {
                        debugStage = 5200;
                        #region Do For Within 1 Hops Of Player Homeworlds
                        foreach ( Planet.PlanetAtHopDistance _phd in humanHomeworlds[i].PlanetsWithinXHops_NoFilters( 1 ) )
                        {
                            Planet planet = _phd.Planet;
                            debugStage = 5300;
                            if ( planet.Mapgen_HasMarkLevelAlreadyBeenSet )
                                continue;
                            debugStage = 5400;
                            if ( humanHomeworlds.Contains( planet ) )
                            {
                                debugStage = 5500;
                                if ( markLevelDebug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Player " + i + " homeworld path. Planet " + planet.Index + " " + planet.Name + " is  player homeworld " + i + ", so set to mark 1. doExtraMark3Ring " + doExtraMark3Ring + " strongestAI " + strongestAI, Verbosity.DoNotShow );
                                planet.Mapgen_WorkingMarkLevel = min;
                                planet.Mapgen_HasMarkLevelAlreadyBeenSet = true;
                                planet.Mapgen_WorkingWasSetByPlayerOrAIHomeworldCode = true;
                            }
                            else
                            {
                                debugStage = 5600;
                                adjacentPlanets.Add( planet );
                            }
                        }

                        debugStage = 5700;
                        ArcenArrays.Randomize( adjacentPlanets, Context.RandomToUse );
                        ArcenArrays.Randomize( adjacentPlanets, Context.RandomToUse );
                        ArcenArrays.Randomize( adjacentPlanets, Context.RandomToUse );

                        debugStage = 5800;
                        foreach ( Planet planet in adjacentPlanets )
                        {
                            debugStage = 5900;
                            if ( planet.Mapgen_HasMarkLevelAlreadyBeenSet )
                                continue;
                            debugStage = 6000;
                            if ( mark1s.Count < numberOfMark1s )
                            {
                                debugStage = 6100;
                                //planet.AfterMapGenDebugReport += "\nMark 1 because less than " + mark1s.Count + " < " + numberOfMark1s;
                                planet.Mapgen_WorkingMarkLevel = min;
                                planet.Mapgen_WorkingWasSetByPlayerOrAIHomeworldCode = true;
                                planet.Mapgen_HasMarkLevelAlreadyBeenSet = true;
                                if ( markLevelDebug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Player " + i + " homeworld path. Planet " + planet.Index + " " + planet.Name + " is near homeworld and there are only " + mark1s.Count + " of a total of " + numberOfMark1s + " allowed, so set to mark 1 ", Verbosity.DoNotShow );
                                mark1s.Add( planet );
                            }
                            else
                            {
                                debugStage = 6400;
                                //planet.AfterMapGenDebugReport += "\nMark 2 because adjacent and ran out of room for mark 1s";
                                planet.Mapgen_WorkingMarkLevel = 2;
                                planet.Mapgen_WorkingWasSetByPlayerOrAIHomeworldCode = true;
                                planet.Mapgen_HasMarkLevelAlreadyBeenSet = true;
                            }
                        }
                        #endregion
                    }

                    debugStage = 6500;
                    for ( int i = 0; i < humanHomeworlds.Count; i++ )
                    {
                        debugStage = 6600;
                        foreach ( Planet.PlanetAtHopDistance _phd in humanHomeworlds[i].PlanetsWithinXHops_NoFilters( -1 ) )
                        {
                            Planet planet = _phd.Planet;
                            if ( planet.Mapgen_HasMarkLevelAlreadyBeenSet )
                                continue;

                            bool foundMark1 = false;
                            bool foundMark2 = false;
                            bool foundMark3 = false;
                            debugStage = 6700;
                            foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                            {
                                if ( mark1s.Contains( neighbor ) || neighbor == humanHomeworlds[i] )
                                {
                                    foundMark1 = true;
                                    break;
                                }
                                if ( mark2s.Contains( neighbor ) )
                                    foundMark2 = true;
                                if ( mark3s.Contains( neighbor ) )
                                    foundMark3 = true;
                            }
                            debugStage = 6800;
                            if ( foundMark1 )
                            {
                                debugStage = 6810;
                                //planet.AfterMapGenDebugReport += "\nMark 2 because found adjacent mark 1";
                                planet.Mapgen_WorkingMarkLevel = 2;
                                if ( markLevelDebug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Player " + i + " homeworld path. Planet " + planet.Index + " " + planet.Name + " is adjacent to mark1, so set it to  " + planet.Mapgen_WorkingMarkLevel, Verbosity.DoNotShow );
                                mark2s.Add( planet );
                                planet.Mapgen_WorkingWasSetByPlayerOrAIHomeworldCode = true;
                                planet.Mapgen_HasMarkLevelAlreadyBeenSet = true;
                            }
                            else if ( foundMark2 && doExtraMark3Ring )
                            {
                                debugStage = 6820;
                                //this check happens only if we are adding in extra mark 3 planets (easier game)
                                //planet.AfterMapGenDebugReport += "\nMark 3 because found adjacent mark 2 and doExtraMark3Ring";
                                planet.Mapgen_WorkingMarkLevel = 3;
                                if ( markLevelDebug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Player " + i + " homeworld path. Planet " + planet.Index + " " + planet.Name + " is adjacent to mark2, so set it to  " + planet.Mapgen_WorkingMarkLevel, Verbosity.DoNotShow );
                                mark3s.Add( planet );
                                planet.Mapgen_WorkingWasSetByPlayerOrAIHomeworldCode = true;
                                planet.Mapgen_HasMarkLevelAlreadyBeenSet = true;
                            }
                            else if ( foundMark3 || (!doExtraMark3Ring && foundMark2) )
                            {
                                debugStage = 6830;
                                int targetMark = next2NeighborIs4 ? 4 : 3;
                                next2NeighborIs4 = !next2NeighborIs4;
                                planet.Mapgen_WorkingMarkLevel = targetMark;
                                //planet.AfterMapGenDebugReport += "\nMark " + targetMark + " because found adjacent mark 3 and !doExtraMark3Ring and found adjacent mark 2, and next2NeighborIs4: " + !next2NeighborIs4;
                                if ( markLevelDebug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Player " + i + " homeworld path. Planet " + planet.Index + " " + planet.Name + " is near homeworld other path  " + planet.Mapgen_WorkingMarkLevel, Verbosity.DoNotShow );
                                planet.Mapgen_WorkingWasSetByPlayerOrAIHomeworldCode = true;
                                planet.Mapgen_HasMarkLevelAlreadyBeenSet = true;
                            }
                        }
                    }

                    Planet.ReleaseTemporaryPlanetList( mark1s );
                    Planet.ReleaseTemporaryPlanetList( mark2s );
                    Planet.ReleaseTemporaryPlanetList( mark3s );
                    Planet.ReleaseTemporaryPlanetList( adjacentPlanets );
                }
                #endregion

                debugStage = 7000;

                #region set ai homeworld to have a certain number of mk7 AIBastion neighbors, and then a ring of 6s around those, and alternating 5s and 4s around that
                {
                    for ( int i = 0; i < aiHomeworlds.Count; i++ )
                    {
                        debugStage = 7100;
                        Faction aiFaction = World_AIW2.Instance.GetFactionByIndex( aiHomeworlds[i].InitialOwningAIFactionIndex );
                        debugStage = 7200;
                        AISentinelsCoreData aiSentinelData = aiFaction.TryGetAISentinelsCoreData()?.SentinelInfo;
                        if ( aiSentinelData == null )
                        {
                            Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "AssignDefenseValuesTo_HostOnly: GetSentinelsExternal was null on faction " +
                                aiFaction.GetDisplayName() + " (index " + aiFaction.FactionIndex + ")" );

                            Planet.ReleaseTemporaryPlanetList( humanHomeworlds );
                            Planet.ReleaseTemporaryPlanetList( aiHomeworlds );
                            return;
                        }
                        debugStage = 7300;
                        int numberOfMark7AIBasitions = aiSentinelData.AIType.NumberOfBastionWorlds;

                        debugStage = 7400;

                        List<Planet> mark6s = Planet.GetTemporaryPlanetList( "BaseAITypeImp-AssignDefenseValuesTo_HostOnly-mark6s", 10f );
                        if ( mark6s == null ) //blocked for teardown/shutdown; bail
                        {
                            Planet.ReleaseTemporaryPlanetList( humanHomeworlds ); //release already-acquired temps before bailing
                            Planet.ReleaseTemporaryPlanetList( aiHomeworlds );
                            return;
                        }
                        List<Planet> mark7s = Planet.GetTemporaryPlanetList( "BaseAITypeImp-AssignDefenseValuesTo_HostOnly-mark7s", 10f );
                        if ( mark7s == null ) //blocked for teardown/shutdown; bail
                        {
                            Planet.ReleaseTemporaryPlanetList( humanHomeworlds ); //release already-acquired temps before bailing
                            Planet.ReleaseTemporaryPlanetList( aiHomeworlds );
                            Planet.ReleaseTemporaryPlanetList( mark6s );
                            return;
                        }

                        bool next6NeighborIs4 = false;
                        debugStage = 7500;
                        foreach ( Planet.PlanetAtHopDistance _phd in aiHomeworlds[i].PlanetsWithinXHops_NoFilters( -1 ) )
                        {
                            Planet planet = _phd.Planet;
                            Int16 distance = _phd.Hops;
                            if ( planet.Mapgen_HasMarkLevelAlreadyBeenSet && planet.Mapgen_WorkingMarkLevel >= 6 )
                                continue;
                            if ( planet.PopulationType == PlanetPopulationType.AIBastionWorld )
                                continue;
                            if ( planet.MapGen_IsFullyUsedByAFaction && planet.MapGen_FullyUsingFaction != aiFaction)
                                continue;

                            if ( aiHomeworlds.Contains( planet ) )
                            {
                                if ( markLevelDebug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "AI homeworld " + i + " path. Planet " + planet.Index + " " + planet.Name + " is an ai homeworld, so set to mark 7 ", Verbosity.DoNotShow );
                                planet.Mapgen_WorkingMarkLevel = max;
                                planet.Mapgen_WorkingWasSetByPlayerOrAIHomeworldCode = true;
                                planet.Mapgen_HasMarkLevelAlreadyBeenSet = true;
                            }
                            else if ( mark7s.Count < numberOfMark7AIBasitions )
                            {
                                if ( markLevelDebug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "AI homeworld " + i + " path. Planet " + planet.Index + " " + planet.Name + " set to mark 7 at distance " + distance + " from ai homeworld", Verbosity.DoNotShow );
                                planet.Mapgen_WorkingMarkLevel = max;
                                planet.PopulationType = PlanetPopulationType.AIBastionWorld;
                                planet.MapGen_FullyUsingFaction = aiFaction;
                                planet.MapGen_IsFullyUsedByAFaction = true;
                                planet.GravWellSize = World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, planet.PopulationType );
                                planet.InitialOwningAIFactionIndex = aiFaction.FactionIndex;
                                mark7s.Add( planet );
                                planet.Mapgen_WorkingWasSetByPlayerOrAIHomeworldCode = true;
                                planet.Mapgen_HasMarkLevelAlreadyBeenSet = true;
                            }
                            else
                            {
                                bool foundMark7 = false;
                                bool foundMark6 = false;
                                foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                                {
                                    if ( mark7s.Contains( neighbor ) || neighbor == aiHomeworlds[i] )
                                    {
                                        foundMark7 = true;
                                        break;
                                    }
                                    if ( mark6s.Contains( neighbor ) )
                                        foundMark6 = true;
                                }
                                if ( foundMark7 )
                                {
                                    planet.Mapgen_WorkingMarkLevel = 6;
                                    if ( markLevelDebug )
                                        ArcenDebugging.ArcenDebugLogSingleLine( "AI homeworld " + i + " path. Planet " + planet.Index + " " + planet.Name + " set to mark 6 (mark 7 neighbor) at distance " + distance + " from ai homeworld", Verbosity.DoNotShow );
                                    mark6s.Add( planet );
                                    planet.Mapgen_WorkingWasSetByPlayerOrAIHomeworldCode = true;
                                    planet.Mapgen_HasMarkLevelAlreadyBeenSet = true;
                                }
                                else if ( foundMark6 )
                                {
                                    int targetMark = next6NeighborIs4 ? 4 : 5;
                                    next6NeighborIs4 = !next6NeighborIs4;
                                    planet.Mapgen_WorkingMarkLevel = targetMark;
                                    if ( markLevelDebug )
                                        ArcenDebugging.ArcenDebugLogSingleLine( "AI homeworld " + i + " path. Planet " + planet.Index + " " + planet.Name + " set to " + planet.Mapgen_WorkingMarkLevel + " mark 6 neighbor at distance " + distance + " from ai homeworld", Verbosity.DoNotShow );
                                    planet.Mapgen_WorkingWasSetByPlayerOrAIHomeworldCode = true;
                                    planet.Mapgen_HasMarkLevelAlreadyBeenSet = true;
                                }
                            }
                        }


                        Planet.ReleaseTemporaryPlanetList( mark6s );
                        Planet.ReleaseTemporaryPlanetList( mark7s );
                    }
                }
                #endregion

                debugStage = 8000;

                #region Calculate All Non-Assigned Planets
                for ( int i = 0; i < planets.Count; i++ )
                {
                    Planet planet = planets[i];
                    if ( planet.Mapgen_WorkingWasSetByPlayerOrAIHomeworldCode || planet.Mapgen_HasMarkLevelAlreadyBeenSet )
                        continue;
                    //planet.AfterMapGenDebugReport += "\nMark level set to larger group because non-assigned planet";
                    int random = Context.RandomToUse.Next( 0, 100 );
                    if ( random < percentMark2 )
                        planet.Mapgen_WorkingMarkLevel = 2;
                    else if ( random < percentMark2 + percentMark3 )
                        planet.Mapgen_WorkingMarkLevel = 3;
                    else if ( random < percentMark2 + percentMark3 + percentMark4 )
                        planet.Mapgen_WorkingMarkLevel = 4;
                    else if ( random < percentMark2 + percentMark3 + percentMark4 + percentMark5 )
                        planet.Mapgen_WorkingMarkLevel = 5;
                    else
                        planet.Mapgen_WorkingMarkLevel = 6;

                    if ( markLevelDebug )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Planet " + planet.Index + " " + planet.Name + " is ambient (" + planet.Mapgen_WorkingMarkLevel + "). WorkingBool1 " + planet.Mapgen_WorkingBool1 + " bool2 " + planet.Mapgen_WorkingBool2 + " bool3 " + planet.Mapgen_WorkingWasSetByPlayerOrAIHomeworldCode + " this means we found a non-ambient", Verbosity.DoNotShow );
                }
                #endregion

                debugStage = 10000;

                Planet.ReleaseTemporaryPlanetList( humanHomeworlds );
                Planet.ReleaseTemporaryPlanetList( aiHomeworlds );

                AssignDefenseValuesTo_HostOnly_AssignActualMarkLevelsToPlanets( planets );

                debugStage = 12000;

                #region Make Sure All The GuardPostAndCommandPlacers Are Set
                for ( int i = 0; i < planets.Count; i++ )
                {
                    Planet planet = planets[i];
                    planet.SetGuardPostAndCommandPlacerFromPlanetStats( strongestAI, Context );
                }
                #endregion
            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    Engine_AIW2.Instance.LogErrorDuringCurrentMapGen( "AssignDefenseValuesTo_HostOnly exception at debugStage " + debugStage + ", exception: " + e );
            }
        }

        protected static void AssignDefenseValuesTo_HostOnly_AssignActualMarkLevelsToPlanets( List<Planet> planets )
        {
            bool markLevelDebug = false;
            bool statsDebug = false;
            if ( markLevelDebug )
                ArcenDebugging.ArcenDebugLogSingleLine("**********", Verbosity.DoNotShow );

            List<int> counter = Mat.GetTemporaryIntList( "BaseAITypeImplementation-counter", 10f );
            if ( counter == null ) //blocked for teardown/shutdown; bail
                return;

            for (int i = 0; i < 10; i++)
            {
                counter.Add(0);
            }
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "assign mark levels for: " + planets.Count );

            #region assign mark level objects
            for ( int i = 0; i < planets.Count; i++ )
            {
                Planet planet = planets[i];
                planet.MarkLevelForAIOnly = Balance_MarkLevelTable.Instance.RowsByOrdinal[planet.Mapgen_WorkingMarkLevel];
                if ( MapgenLogger.IsActive && planet.Mapgen_WorkingMarkLevel <= 0 )
                    MapgenLogger.Log( "bum mark level for: " + planet.Name );
                if ( planet.MarkLevelForAIOnly == null )
                    planet.MarkLevelForAIOnly = Balance_MarkLevelTable.Instance.RowsByOrdinal[1];
                if ( markLevelDebug )
                    ArcenDebugging.ArcenDebugLogSingleLine("Planet " + planet.Index + " " + planet.Name + " workingint " + planet.Mapgen_WorkingMarkLevel + " mark " + planet.MarkLevelForAIOnly.Ordinal, Verbosity.DoNotShow );
                counter[planet.Mapgen_WorkingMarkLevel]++;
            }
            #endregion
            if ( markLevelDebug || statsDebug )
            {
                for(int i = 1; i <= 7; i++)
                    ArcenDebugging.ArcenDebugLogSingleLine("There are " + counter[i] + " mark " + i + " planets", Verbosity.DoNotShow );
            }
            if ( MapgenLogger.IsActive )
            {
                for ( int i = 1; i <= 7; i++ )
                    MapgenLogger.Log( "There are " + counter[i] + " mark " + i + " planets" );
            }

            Mat.ReleaseTemporaryIntList( counter );
        }

        public virtual void SeedStartingEntitiesForAIType(Faction faction, ArcenHostOnlySimContext Context )
        {
            return;
        }

        public virtual void DoOnAnyDeathLogic_MyFactionUnitsOnly_HostOnly( GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull, ArcenHostOnlySimContext Context )
        { 
        }

        public virtual void CalculateCoreImportance( ref float importanceWithAdjustment, GameEntity_Squad attackerEntity, EntitySystem attackerSystem, EntitySystemTypeData attackerSystemTypeData, GameEntity_Squad defenderEntity, NonSimTargetPlanningInfo defenderTargetingInfo, ArcenCharacterBuffer traceBuffer )
        {
        }

        public virtual void AdjustWaveOptions( Faction attachedFaction, ArcenHostOnlySimContext context, ref int numUnitTypes, ref int maxGuardianTypes, PlannedWaveOptions options, Planet planetToUseForSpawningTypes )
        {
        }
    }
}
