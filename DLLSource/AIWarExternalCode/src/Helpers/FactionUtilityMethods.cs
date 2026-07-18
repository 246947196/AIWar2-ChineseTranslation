using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /* Helper functions for various minor faction things. Started out
       as just help for the Nanocaust, and is now used in a variety of places */

    public class FactionUtilityMethods
    {
        public readonly static FactionUtilityMethods Instance = new FactionUtilityMethods();
        //TODO: figure out if I'm under attack
        // public List<Planet> findPlanetsUnderAttack()
        //   {
        //     foreach(nanocaust planet)
        //       {
        //         CombatSide myCombatSide = planet.Combat.GetSideForWorldSide( side );
        //         if(mySide.DataByStance[SideStance.Hostile].TotalStrength > minEnemyStrengthForDefenseFleet)
        //           {
        //             //this planet is under attack
        //             //Now, how do I figure out who is attacking, keeping in mind there might be multiple forces (2 humans? a human fleet following the devourer in?)
        //             for ( int i = 0; i < planet.Combat.Sides.Count; i++ )
        //               {
        //                 CombatSide otherCombatSide = planet.Combat.Sides[i];
        //                 FInt sideStrength = otherCombatSide.DataByStance[SideStance.Me].TotalStrength;
        //                 if(otherCombatSide.WorldSide.Type == WorldSideType.Player && sideStrength > retaliationMin)
        //                   {
        //                     //queue up an attack against this player later
        //                   }
        //               }
        //             //send troops to this planet to defeat the enemy forces
        //           }
        //       }
        //   }
        
        public bool ShouldFactionsThrottle()
        {
            double throttleMetric = 0.8; //after discussion with chris, this is when we start throttline
            double ratioInMilliseconds = World_AIW2.Instance.GetPerformanceRatio();
            if (ratioInMilliseconds > 0)
            {
                double ratioInSpeed = 1f / (ratioInMilliseconds);
                if (ratioInSpeed < throttleMetric)
                    return true;
            }
            
            return false;
        }
        
        public FInt GetCapRatio(Faction faction)
        {
            FInt worstCap = FInt.Zero;
            foreach (NPCShipCapType row in NPCShipCapTypeTable.Instance.Rows)
            {
                int currentCount = faction.NPCShipCountsByCapType == null ? 0 : faction.NPCShipCountsByCapType[row.RowIndexNonSim];
                int cap = faction.SpecialFactionData.NPCShipCapsByType[row.RowIndexNonSim];
                FInt output = (FInt)currentCount / (FInt)cap;
                if (output > worstCap)
                    worstCap = output;
            }
            return worstCap;
        }
        
        public string getEntitesInAIShipGroupCategory_TextDisplayOnly(AIShipGroupCategory category)
        {
            string output = "The category list has ";
            for (int j = 0; j < category.DrawBag.InternalListSize; j++)
            {
                AIShipGroup group = category.DrawBag.GetInternalListItemAtIndex(j);
                for (int k = 0; k < group.DrawBag.InternalListSize; k++)
                {
                    output += group.DrawBag.GetInternalListItemAtIndex(k) + ", ";
                }
            }
            return output;
        }

        public string getEntitesInAIShipGroup_TextDisplayOnly(AIShipGroup group)
        {
            string output = "This group has ";
            for (int j = 0; j < group.DrawBag.InternalListSize; j++)
            {
                output += group.DrawBag.GetInternalListItemAtIndex(j).InternalName + ", ";
            }

            return output;
        }

        public void getEntitesInAIShipGroupCategory(AIShipGroupCategory category, List<GameEntityTypeData> ListToFill)
        {
            ListToFill.Clear();
            for (int j = 0; j < category.DrawBag.InternalListSize; j++)
            {
                AIShipGroup group = category.DrawBag.GetInternalListItemAtIndex(j);
                for (int k = 0; k < group.DrawBag.InternalListSize; k++)
                {
                    ListToFill.Add(group.DrawBag.GetInternalListItemAtIndex(k));
                }
            }
        }
        
        public void getEntitesInAIShipGroup(AIShipGroup group, List<GameEntityTypeData> ListToFill)
        {
            ListToFill.Clear();
            for (int j = 0; j < group.DrawBag.InternalListSize; j++)
            {
                ListToFill.Add(group.DrawBag.GetInternalListItemAtIndex(j));
            }
        }
        
        public bool IsPlanetAdjacentToPlayerKing(Planet planet, out Planet kingPlanet)
        {
            bool output = false;
            kingPlanet = null;
            Planet localKingPlanet = null;
            foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
            {
                if (HasPlayerKing(neighbor))
                {
                    output = true;
                    localKingPlanet = neighbor;//you can't use an out parameter inside an anonymous method
                    break;
                }
            }
            kingPlanet = localKingPlanet;
            return output;
        }
        
        public bool HasPlayerKing(Planet planet)
        {
            bool HasPlayerKing = false;
            
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if (entity != null && entity.PlanetFaction.Faction.Type == FactionType.Player)
                    HasPlayerKing = true;
            }
            return HasPlayerKing;
        }
        public GameEntity_Squad HasAIKing(Planet planet)
        {
            GameEntity_Squad aiKing = null;

            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if (entity != null && entity.PlanetFaction.Faction.Type == FactionType.AI)
                {
                    aiKing = entity;
                    break;
                }
            }
            if ( aiKing != null )
                return aiKing;
            foreach ( GameEntity_Squad entity in planet.Squads( "AIOverlord_AnyTypeOrPhase" ) )
            {
                if (entity != null && entity.PlanetFaction.Faction.Type == FactionType.AI)
                {
                    aiKing = entity;
                    break;
                }
            }
            return aiKing;
        }
        
        public bool IsShieldBlockingWormholeToPlanet(Planet startPlanet, Planet destinationPlanet, Faction faction)
        {
            GameEntity_Other thisWormholeOrNull = startPlanet.GetWormholeTo(destinationPlanet);
            if (thisWormholeOrNull == null)
                return false; //maybe if we jump between planets while LRP is running?
            bool foundBlockingShield = false;
            foreach ( GameEntity_Squad shieldGenerator in startPlanet.Squads( EntityRollupType.ProjectsForcefield ) )
            {
                if (shieldGenerator == null)
                    continue;
                if (!shieldGenerator.GetIsHostileTowards_Safe(faction))
                    continue;
                int distanceThreshold = shieldGenerator.CalculatedCurrentShieldRadius;
                if (distanceThreshold <= 0)
                    continue;
                distanceThreshold += shieldGenerator.DataForMark.Radius; // not relevant per se, but kind of a stand-in for the radius of the retreating unit; TODO: maybe we need to have this been the largest radius of the units trying to retreat
                distanceThreshold += thisWormholeOrNull.TypeData.BaseMark.Radius;
                if (shieldGenerator.GetDistanceTo_ExpensiveAccurate(thisWormholeOrNull, RadiusCheck.IgnoreRadii, false) > distanceThreshold)
                    continue;
                foundBlockingShield = true;
                break;
            }
            return foundBlockingShield;
        }

        public bool patrolPlanet(Faction ForFaction, List<SafeSquadWrapper> entityList, ArcenHostOnlySimContext Context, int patrolDistance, float RequiredTimeBetweenCommands)
        {
            if (Context == null)
                return false; //client

            //Note that the value passed in should be some fraction of ExternalConstants.Instance.DistanceScale_GravwellRadius
            //To patrol a planet, we pick a bunch of ArcenPoints at random,
            //then send the ships through those points.
            //Tthe goal is for these points to take longer to
            //traverse than defaultPatrolTime
            //It produces a pleasantly random effect at the moment

            if (entityList.Count == 0)
            {
                return false;
            }

            //if any of the ships in the list have been given orders too recently, then don't give orders to any of them right now
            for (int i = 0; i < entityList.Count; i++)
            {
                if (ArcenTime.TimeSinceStartF - entityList[i].HostOnly_TimeWasLastGivenOrderFromLRP < RequiredTimeBetweenCommands)
                    return false;
            }

            //Note: we split ships into smaller groups so we don't have everything going together
            int patrolGroupSize = 5;

            //handle small groups
            if (entityList.Count < patrolGroupSize)
                patrolGroupSize = entityList.Count;

            List<ArcenPoint> pointsToPatrol = Mat.GetTemporaryArcenPointList("FactionUtilityMethods-pointsToPatrol", 10f);
            if ( pointsToPatrol == null ) //blocked for teardown/shutdown; bail
                return false;

            var numPatrolGroups = Math.Max(entityList.Count / patrolGroupSize, 1);
            for ( int i = 0; i < numPatrolGroups; i++ )
            {
                pointsToPatrol.Clear();
                int centerX = Engine_AIW2.Instance.CombatCenter.X;
                int centerY = Engine_AIW2.Instance.CombatCenter.Y;
                for (int j = 0; j < 10; j++)
                {
                    int xModification = Context.RandomToUse.Next(-patrolDistance, patrolDistance);
                    int yModification = Context.RandomToUse.Next(-patrolDistance, patrolDistance);
                    ArcenPoint point = ArcenPoint.Create(centerX + xModification, centerY + yModification);
                    pointsToPatrol.Add(point);
                }
                int startOfSubList = i * patrolGroupSize;
                int endOfSubList = startOfSubList + patrolGroupSize - 1;
                if (endOfSubList >= entityList.Count)
                    endOfSubList = entityList.Count - 1;

                patrolPlanetHelper( ForFaction, pointsToPatrol, entityList, startOfSubList, endOfSubList, Context
                    , 0f ); //allow any amount of time, since we checked it above
            }

            Mat.ReleaseTemporaryArcenPointList(pointsToPatrol);

            for (int i = 0; i < entityList.Count; i++)
            {
                entityList[i].SetHostOnly_TimeWasLastGivenOrderFromLRP(ArcenTime.TimeSinceStartF);
            }
            return true;
        }

        public Faction GetFallenSpireFaction()//this one is okay, because we assume there's only one
        {
            for (int i = 0; i < World_AIW2.Instance.Factions.Count; i++)
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if (otherFaction.Type == FactionType.Player)
                {
                    //for the spire infused empire
                    PlayerTypeData playerType = otherFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
                    if (playerType != null && playerType.CountsAsSpireFaction)
                        return otherFaction;
                }
                if (otherFaction.SpecialFactionData.InternalName != "FallenSpire")
                    continue;
                return otherFaction;
            }
            return null;
        }
        public Faction GetArmadaFaction()//this one is okay, because we assume there's only one
        {
            for (int i = 0; i < World_AIW2.Instance.Factions.Count; i++)
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if (otherFaction.Type != FactionType.Player)
                    continue;
                PlayerTypeData playerType = otherFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( playerType != null && playerType.GetHasTag("ArmadaEmpire"))
                    return otherFaction;
            }
            return null;
        }
        public Faction GetApkalluFaction()//this one is okay, because we assume there's only one
        {
            for (int i = 0; i < World_AIW2.Instance.Factions.Count; i++)
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if (otherFaction.Type != FactionType.Player)
                    continue;
                PlayerTypeData playerType = otherFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( playerType != null && playerType.GetHasTag("ApkalluSidekick"))
                    return otherFaction;
                if ( playerType != null && playerType.GetHasTag("ApkalluInfusedEmpire"))
                    return otherFaction;

            }
            return null;
        }

        public Faction GetTiberiumFaction()//this one is okay, because we assume there's only one
        {
            for (int i = 0; i < World_AIW2.Instance.Factions.Count; i++)
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if (otherFaction.SpecialFactionData.InternalName == "TiberiumInfestation")
                    return otherFaction;
                if (otherFaction.SpecialFactionData.InternalName == "TiberiumInfestationForArmada")
                    return otherFaction;
            }
            return null;
        }

        public bool IsImperialSpireActive()//this one is okay, because we assume there's only one
        {
            int debugCode = 0;
            try{
                debugCode = 100;
                for (int i = 0; i < World_AIW2.Instance.Factions.Count; i++)
                {
                    Faction otherFaction = World_AIW2.Instance.Factions[i];
                    debugCode = 200;
                    if (otherFaction.Type == FactionType.Player)
                    {
                        //for the spire infused empire and spire sidekick
                        debugCode = 300;
                        PlayerTypeData playerType = otherFaction.PlayerTypeDataOrNull_ModeratelyExpensive;
                        if (playerType != null && playerType.CountsAsSpireFaction)
                        {
                            debugCode = 400;
                            if ( playerType.GetHasTag("SpireSidekick"))
                            {
                                SpireSidekickFactionBaseInfo sideData = otherFaction.GetExternalBaseInfoAs<SpireSidekickFactionBaseInfo>();
                                if (sideData.ImperialFleetActive)
                                    return true;
                                return false;
                            }
                            debugCode = 500;
                            FallenSpireFactionBaseInfo data = otherFaction.GetExternalBaseInfoAs<FallenSpireFactionBaseInfo>();
                            if (data.ImperialFleetActive)
                                return true;
                        }
                    }
                    debugCode = 600;
                    if (otherFaction.SpecialFactionData.InternalName != "FallenSpire")
                        continue;
                    debugCode = 700;
                    FallenSpireFactionBaseInfo odata = otherFaction.GetExternalBaseInfoAs<FallenSpireFactionBaseInfo>();
                    if (odata.ImperialFleetActive)
                        return true;
                }
            } catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine("Hit exception in IsImperialSpireActive debugCode " + debugCode + " " + e.ToString(), Verbosity.DoNotShow );
            }
            return false;
        }

        public int GetFallenSpireCities()
        {
            Faction faction = this.GetFallenSpireFaction();
            if (faction == null)
                return 0;
            PlayerTypeData playerType = faction.PlayerTypeDataOrNull_ModeratelyExpensive;
            if ( playerType != null && playerType.GetHasTag("SpireSidekick"))
            {
                SpireSidekickFactionBaseInfo sideData = faction.GetExternalBaseInfoAs<SpireSidekickFactionBaseInfo>();
                return Math.Max(sideData.SpireCities.Count, 0);
            }
            FallenSpireFactionBaseInfo data = faction.GetExternalBaseInfoAs<FallenSpireFactionBaseInfo>();
            return Math.Max(data.SpireCities.Count, 0);
        }

        public Faction GetZenithMinerFaction() //this one is okay, because we assume there's only one
        {
            for (int i = 0; i < World_AIW2.Instance.Factions.Count; i++)
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if (otherFaction.SpecialFactionData.InternalName != "ZenithMiners")
                    continue;
                return otherFaction;
            }
            return null;
        }

        public int GetNumZenithArchitraves()
        {
            int count = 0;
            for (int i = 0; i < World_AIW2.Instance.Factions.Count; i++)
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if (otherFaction.SpecialFactionData.InternalName != "ZenithArchitrave")
                    continue;
                count++;
            }
            return count;
        }
        
        public Faction GetRandomZAHostileToMe(Faction faction, ArcenHostOnlySimContext Context)
        {
            List<Faction> internalWorkingFactionList = Faction.GetTemporaryFactionList("FactionUtilityMethods-GetRandomZAHostileToMe-internalWorkingFactionList", 10f);

            if (internalWorkingFactionList == null)
                internalWorkingFactionList = List<Faction>.Create_WillNeverBeGCed(300, "FactionUtilityMethods-internalWorkingFactionList");
            else
                internalWorkingFactionList.Clear();

            for (int i = 0; i < World_AIW2.Instance.Factions.Count; i++)
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if (otherFaction == faction)
                    continue;
                if (otherFaction.SpecialFactionData.InternalName != "ZenithArchitrave")
                    continue;
                if (faction.GetIsHostileTowards(otherFaction))
                    internalWorkingFactionList.Add(otherFaction);
            }
            if (internalWorkingFactionList.Count == 0)
            {
                Faction.ReleaseTemporaryFactionList(internalWorkingFactionList);
                return null;
            }
            Faction ret = internalWorkingFactionList[Context.RandomToUse.Next(0, internalWorkingFactionList.Count)];
            Faction.ReleaseTemporaryFactionList(internalWorkingFactionList);
            return ret;
        }

        public bool patrolPlanetHelper(Faction ForFaction, List<ArcenPoint> pointsToPatrol, List<SafeSquadWrapper> entityList,
            int startEntityList, int endEntityList, ArcenHostOnlySimContext Context, float RequiredTimeBetweenCommands)
        {
            if (entityList.Count <= 0 || pointsToPatrol.Count <= 0)
                return false;

            //if any of the ships in the list have been given orders too recently, then don't give orders to any of them right now
            for (int i = 0; i < entityList.Count; i++)
            {
                if (ArcenTime.TimeSinceStartF - entityList[i].HostOnly_TimeWasLastGivenOrderFromLRP < RequiredTimeBetweenCommands)
                    return false;
            }

            bool result = false;
            //this helper function queues up the actual commands. It takes extra integer arguments for which elements of the entityList to
            //use. This technique allows us to have a giant entityList split up effectively, since otherwise it can just look like a giant chain
            for (int i = 0; i < pointsToPatrol.Count; i++)
            {
                GameCommand command = null;
                for ( int j = startEntityList; j <= endEntityList && j < entityList.Count; j++ )
                {
                    if (command == null)
                    {
                        command = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCPatrol], GameCommandSource.AnythingElse);
                        command.ToBeQueued = true;
                        if (i == 0)
                            command.ToBeQueued = false;
                        command.RelatedPoints.Add(pointsToPatrol[i]);
                    }
                    command.RelatedEntityIDs.Add(entityList[j].GetPrimaryKeyID());
                    entityList[j].SetHostOnly_TimeWasLastGivenOrderFromLRP(ArcenTime.TimeSinceStartF);
                }
                if (command != null)
                {
                    bool playAudioEffectForCommand = false;
                    if (command.RelatedEntityIDs.Count > 0)
                    {
                        World_AIW2.Instance.QueueGameCommand(ForFaction, command, playAudioEffectForCommand);
                        result = true;
                    }
                    else
                        command.ReturnToPool();
                }
            }
            return result;
        }
        
        public int strengthOfEntity(GameEntity_Squad entity)
        {
            if (entity == null)
                return 0;
            return entity.GetStrengthOfSelfAndContents();
        }
        
        public FInt strengthOfList(List<SafeSquadWrapper> list)
        {

            FInt str = FInt.FromParts(0, 0);
            if (list == null)
                return str;
            for (int i = 0; i < list.Count; i++)
            {
                str += strengthOfEntity(list[i].GetSquad());
            }
            return str;
        }

        #region isPlanetOnList
        public bool isPlanetOnList(List<Planet> list, Planet element)
        {
            if (list == null || element == null)
                return false;
            return list.Contains(element);
        }

        public bool isPlanetOnList(ArcenLessLinkedList<Planet> list, Planet element)
        {
            if (list == null || element == null)
                return false;
            return list.Contains(element);
        }
        #endregion

        public void DespawnCuendillarAsteroidOnPlanet(Planet planet, ArcenHostOnlySimContext Context)
        {
            GameEntity_Squad asteroid = GetAsteroidOnPlanetOrNull(planet);
            GameEntity_Squad planetoid = GetPlanetoidOnPlanetOrNull(planet);
            if (asteroid == null && planetoid == null)
                throw new Exception("No asteroid or planetoid to be found on " + planet.Name);
            if ( asteroid != null )
                asteroid.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
            if ( planetoid != null )
                planetoid.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut);
            
        }
        public GameEntity_Squad GetRandomPlanetoidOrNull(ArcenHostOnlySimContext Context)
        {
            GameEntity_Squad output = null;
            GameEntity_Squad backupOutput = null;
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "CuendillarPlanetoid" ) )
            {
                bool onAIPlanet = (entity.Planet.GetControllingOrInfluencingFaction().Type == FactionType.AI);
                backupOutput = entity;
                if ( backupOutput != null && onAIPlanet )
                    backupOutput = entity; //our backup should always be on an AI planet if possible
                if ( entity.Planet.GetControllingOrInfluencingFaction().Type != FactionType.AI )
                    continue; //prefer planetoids on AI planets
                if (Context.RandomToUse.Next(0, 100) > 50 )
                {
                    output = entity;
                    break;
                }
            }
            if ( output == null )
                output = backupOutput;
            return output;
        }
        public GameEntity_Squad GetAsteroidOnPlanetOrNull ( Planet planet )
        {
            GameEntity_Squad output = null;
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "CuendillarAsteroid" ) )
            {
                if ( entity.Planet == planet )
                {
                    output = entity;
                    break;
                }
            }
            return output;
        }
        public GameEntity_Squad GetPlanetoidOnPlanetOrNull ( Planet planet )
        {
            GameEntity_Squad output = null;
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "CuendillarPlanetoid" ) )
            {
                if ( entity.Planet == planet )
                {
                    output = entity;
                    break;
                }
            }
            return output;
        }

        public GameEntity_Squad GetReaperChrysalisOnPlanetOrNull ( Planet planet )
        {
            GameEntity_Squad output = null;
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "ReaperChrysalis" ) )
            {
                if ( entity.Planet == planet )
                {
                    output = entity;
                    break;
                }
            }
            return output;
        }
        public GameEntity_Squad GetDysonSphereOnPlanetOrNull ( Planet planet )
        {
            GameEntity_Squad output = null;
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "DysonSphere" ) )
            {
                if ( entity.Planet == planet )
                {
                    output = entity;
                    break;
                }
            }
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "DysonSidekickSphere" ) )
            {
                if ( entity.Planet == planet )
                {
                    output = entity;
                    break;
                }
            }

            return output;
        }

        public GameEntity_Squad GetReaperGatewayOnPlanetOrNull ( Planet planet )
        {
            GameEntity_Squad output = null;
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "ReaperGateway" ) )
            {
                if ( entity.Planet == planet )
                {
                    output = entity;
                    break;
                }
            }
            return output;
        }

        public GameEntity_Squad GetAICuendillarDrillOnPlanetOrNull ( Planet planet )
        {
            GameEntity_Squad output = null;
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "AICuendillarDrill" ) )
            {
                if ( entity.Planet == planet )
                {
                    output = entity;
                    break;
                }
            }
            return output;
        }
        public GameEntity_Squad GetCuendillarDrillOnPlanetOrNull ( Planet planet )
        {
            GameEntity_Squad output = null;
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "DysonDrill" ) )
            {
                if ( entity.Planet == planet )
                {
                    output = entity;
                    break;
                }
            }
            return output;
        }

        public bool DoesPlanetHaveScourgeSpawner(Planet planet)
        {
            bool foundSpawner = false;
            foreach ( GameEntity_Squad entity in planet.Squads( "ScourgeSpawner" ) )
            {
                foundSpawner = true;
            }
            return foundSpawner;
        }
        public bool DoesPlanetHaveTiberiumVein(Planet planet)
        {
            bool foundVein = false;
            foreach ( GameEntity_Squad entity in planet.Squads( "TiberiumVein" ) )
            {
                foundVein = true;
            }
            return foundVein;
        }

        public bool DoesPlanetHaveMetalTerminus(Planet planet)
        {
            bool foundTerminus = false;
            foreach ( GameEntity_Squad entity in planet.Squads( "DZMetalTerminus" ) )
            {
                foundTerminus = true;
            }
            return foundTerminus;
        }

        public bool DoesPlanetHaveAlliedKing( Faction faction, Planet planet )
        {
            bool foundKing = false;
            foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( entity.Planet == planet && entity.GetIsFriendlyTowards_Safe( faction ) )
                {
                    foundKing = true;
                    break;
                }
            }
            return foundKing;
        }
        //human king failures is used for debugging purposes only
        int HumanKingFailures = 0;
        public GameEntity_Squad findKing( Faction faction = null )
        {
            GameEntity_Squad king = null;
            if ( World_AIW2.Instance.IsOutsideOfNormalGameplay )
                return king;
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( faction != null )
                {
                    //if an optional Faction is passed in, return the king of that faction
                    if ( entity.PlanetFaction.Faction != faction )
                        continue;
                }
                king = entity;
            }
            return king;
        }
        public bool IsKingPhased( Faction faction )
        {
            if ( World_AIW2.Instance.IsOutsideOfNormalGameplay )
                return false;
            GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( faction );
            if ( king == null )
                return false;
            if ( king.CurrentStateOfMatter == StateOfMatterTypeDataTable.Instance.DefaultRow )
                return false;
            return true;
        }

        public GameEntity_Squad findDireGuardPost( Faction faction, ArcenHostOnlySimContext Context = null )
        {
            //This is used for the imperial spire
            GameEntity_Squad dire = null;
            if ( World_AIW2.Instance.IsOutsideOfNormalGameplay )
                return dire;
            GameEntity_Squad backup = null;
            foreach ( GameEntity_Squad entity in faction.Squads( EntityRollupType.AIPOnDeath ) )
            {
                //we want any dire guard post
                if ( entity.TypeData.SpecialType != SpecialEntityType.DireGuardPost )
                    continue;
                if ( backup == null )
                    backup = entity;
                if ( Context.RandomToUse.Next(0, 100) > 20 )
                {
                    dire = entity;
                    break;
                }
            }
            if ( dire == null )
                dire = backup;
            return dire;
        }
        public Faction GetNearestAIFaction( Planet startPlanet )
        {
            //Here 'nearest' means "which AI faction has the closer king to the start planet"
            if ( World_AIW2.Instance.AIFactions.Count <= 0 )
                return null;
            if ( startPlanet == null )
                return null;

            Faction output = null;
            int shortestDistance = -1;
            for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
            {
                Faction fac = World_AIW2.Instance.AIFactions[i];
                if ( fac.FactionIsDefeated )
                    continue;
                int currentDistance = startPlanet.GetHopsTo( FactionUtilityMethods.Instance.findKing( fac).Planet);
                if ( shortestDistance == -1 || currentDistance < shortestDistance )
                {
                    output = fac;
                }
            }

            return output;
        }
        public Planet GetKingKillerTarget( Planet startPlanet, ArcenHostOnlySimContext Context )
        {
            Faction nextFaction = null;
            //Used by the king-killer ships for the imperial spire
            //If we are starting on a player homeworld, pick a random AI and fly toward it
            //if we already have a target, continue toward the closest 
            if ( HasPlayerKing( startPlanet ))
            {
                nextFaction = World_AIW2.GetRandomAIFaction( Context );
            }
            else
                nextFaction = FactionUtilityMethods.Instance.GetNearestAIFaction( startPlanet );

            if ( nextFaction == null )
                return null;
            if ( FactionUtilityMethods.Instance.IsKingPhased( nextFaction ))
            {
                //if the king is in solo-phase, prefer to go elsewhere
                int attempts = 3;
                while ( attempts-- > 0 )
                {
                    nextFaction = World_AIW2.GetRandomAIFaction( Context );
                    if ( !FactionUtilityMethods.Instance.IsKingPhased( nextFaction ))
                        break;
                }
            }

            GameEntity_Squad dire = FactionUtilityMethods.Instance.findDireGuardPost( nextFaction, Context );
            if ( dire != null )
                return dire.Planet;
            GameEntity_Squad king = FactionUtilityMethods.Instance.findKing( nextFaction );
            if ( king != null && king.CurrentStateOfMatter == StateOfMatterTypeDataTable.Instance.DefaultRow )
                return king.Planet;
            return null;
        }
        public GameEntity_Squad GetNearestFlagshipToPlanet_OrNull( Faction faction, Planet planet, ArcenHostOnlySimContext Context, List<SafeSquadWrapper> WorkingFlagshipList, bool omitMoon = false, bool omitKing = false )
        {
            WorkingFlagshipList.Clear();
            int bestHops = -1;
            foreach ( Fleet fleet in World_AIW2.Instance.Fleets( faction, FleetStatus.CenterpieceMustLiveOrLooseFleet ) )
            {
                GameEntity_Squad flagship = fleet.Centerpiece.GetSquad();
                if ( flagship == null )
                {
                    continue;
                }
                if ( omitMoon && flagship.TypeData.GetHasTag("DarkZenithMoon"))
                {
                    continue;
                }
                if ( omitKing && flagship.TypeData.PlayerLosesIfAnyDie )
                {
                    continue;
                }
                int hops = planet.GetHopsTo( flagship.Planet );
                if ( bestHops == -1 || hops < bestHops )
                {
                    WorkingFlagshipList.Clear();
                    WorkingFlagshipList.Add(flagship);
                    bestHops = hops;
                    continue;
                }
                if ( hops == bestHops )
                {
                    WorkingFlagshipList.Add(flagship);
                    continue;
                }
            }
            if ( WorkingFlagshipList.Count == 0 )
            {
                //this should never actually be null, but better safe than sorry
                return null;
            }

            return WorkingFlagshipList[Context.RandomToUse.Next( 0, WorkingFlagshipList.Count )].GetSquad();

        }
        public Planet GetNearestFlagshipToKing_OrNullIfNone( Faction faction, ArcenHostOnlySimContext Context )
        {
            GameEntity_Squad king = this.findKing( faction );
            if ( king == null )
                return null;
            GameEntity_Squad nearestFlagship = null;
            int nearestHopsToKing = 999;
            Faction neutralFaction = World_AIW2.Instance.GetNeutralFaction();
            foreach ( GameEntity_Squad entity in neutralFaction.Squads( EntityRollupType.MobileFleetFlagships ) )
            {
                if ( entity.TypeData.SpecialType != SpecialEntityType.MobileStrikeCombatFleetFlagship )
                    continue;
                int hops = entity.Planet.GetHopsTo( king.Planet );
                if ( hops < nearestHopsToKing )
                {
                    nearestFlagship = entity;
                    nearestHopsToKing = hops;
                }
            }
            if ( nearestFlagship == null )
                return null;
            return nearestFlagship.Planet;
        }
        public Planet GetNearestARSToKing_OrNullIfNone( Faction faction, ArcenHostOnlySimContext Context )
        {
            GameEntity_Squad king = this.findKing( faction );
            if ( king == null )
                return null;
            GameEntity_Squad nearestARS = null;
            int nearestHopsToKing = 999;
            Faction neutralFaction = World_AIW2.Instance.GetNeutralFaction();
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "ARS" ) )
            {
                int hops = entity.Planet.GetHopsTo( king.Planet );
                if ( hops < nearestHopsToKing )
                {
                    nearestARS = entity;
                    nearestHopsToKing = hops;
                }
            }
            if ( nearestARS == null )
            {
                foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( "ARSExpert" ) )
                {
                    int hops = entity.Planet.GetHopsTo( king.Planet );
                    if ( hops < nearestHopsToKing )
                    {
                        nearestARS = entity;
                        nearestHopsToKing = hops;
                    }
                }
            }
            if ( nearestARS == null )
                return null;
            return nearestARS.Planet;
        }
        public void findAllHumanKings( List<SafeSquadWrapper> listToFill )
        {
            listToFill.Clear();

            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( entity.GetFactionTypeSafe() != FactionType.Player )
                    continue;
                listToFill.Add( entity );
            }
        }
        public bool DoesPlanetHaveHumanKing( Planet planet )
        {
            bool foundKing = false;
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( entity.Planet == planet )
                {
                    foundKing = true;
                    break;
                }
            }
            return foundKing;
        }
        public int countHumanKings( )
        {
            int count = 0;
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( entity.GetFactionTypeSafe() != FactionType.Player )
                    continue;
                count++;
            }
            return count;
        }
        #region findNearestHumanKing
        public GameEntity_Squad findNearestHumanKing( Planet FromPlanet )
        {
            List<SafeSquadWrapper> internalWorkingSquadsList = GameEntity_Squad.GetTemporarySquadList( "FactionUtil-findNearestHumanKing-internalWorkingSquadsList", 10f );
            if ( internalWorkingSquadsList == null ) //blocked for teardown/shutdown; bail
                return null;

            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( entity.GetFactionTypeSafe() != FactionType.Player )
                    continue;
                internalWorkingSquadsList.Add( entity );
            }

            if ( FromPlanet == null || internalWorkingSquadsList.Count <= 1 )
            {
                if ( internalWorkingSquadsList.Count <= 0 )
                {
                    GameEntity_Squad.ReleaseTemporarySquadList( internalWorkingSquadsList );
                    return null;
                }
                GameEntity_Squad ret = internalWorkingSquadsList[0].GetSquad();
                GameEntity_Squad.ReleaseTemporarySquadList( internalWorkingSquadsList );
                return ret;
            }

            int bestDistanceSoFar = 999;
            GameEntity_Squad bestKing = null;
            int currentHops = 0;

            foreach ( SafeSquadWrapper wrap in internalWorkingSquadsList )
            {
                GameEntity_Squad king = wrap.GetSquad();
                if ( king == null )
                    continue;
                currentHops = king.Planet.GetHopsTo( FromPlanet );
                if ( currentHops < bestDistanceSoFar || bestKing == null )
                {
                    bestKing = king;
                    bestDistanceSoFar = currentHops;
                }
            }

            GameEntity_Squad.ReleaseTemporarySquadList( internalWorkingSquadsList );
            return bestKing;
        }
        #endregion

        public int NumFriendlyLivingAIFactions( Faction faction )
        {
            int num = 0;
            for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
            {
                //note that this loop will count the current faction (since its friendly to itself)
                Faction aiFaction = World_AIW2.Instance.AIFactions[i];
                if ( aiFaction.GetIsHostileTowards( faction ) )
                    continue;
                if ( aiFaction.FactionIsDefeated )
                    continue;
                num++;
            }
            return num;
        }
        public Planet findHumanKing( bool ThrowErrorIfNotFound )
        {
            return findHumanKing_Inner( null, ThrowErrorIfNotFound );
        }

        public Planet findHumanKingForFaction( Faction faction, bool ThrowErrorIfNotFound )
        {
            return findHumanKing_Inner( faction, ThrowErrorIfNotFound );
        }
        private Planet findHumanKing_Inner( Faction factionOrNull, bool ThrowErrorIfCannotFind )
        {
            bool debug = false;
            Planet kingPlanet = null;
            if ( World_AIW2.Instance.IsOutsideOfNormalGameplay )
                return kingPlanet;
            if ( Engine_AIW2.Instance.IsTestChamber )
                return null;
            if ( factionOrNull != null )
            {
                if ( factionOrNull.Type != FactionType.Player && factionOrNull.Type != FactionType.AI )
                {
                    if ( ThrowErrorIfCannotFind )
                        ArcenDebugging.ArcenDebugLogSingleLine( "Requested to find the king of " + factionOrNull.GetDisplayName(), Verbosity.ShowAsError );
                }
            }
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( factionOrNull != null )
                {
                    //if an optional Faction is passed in, return the king of that faction
                    if ( entity.PlanetFaction.Faction != factionOrNull )
                        continue;
                }
                if ( entity.GetFactionTypeSafe() == FactionType.Player )
                {
                    kingPlanet = entity.Planet;
                }
            }
            if ( kingPlanet == null )
            {
                //this typically indicates the game is over
                if ( World.Instance.ConclusionType != CampaignConclusionType.Lost && HumanKingFailures > 1 && ThrowErrorIfCannotFind )
                {
                    //on multiplayer clients, sync data may be a bit off, and in that situation we should just silently accept that there's no king to be found at the moment.
                    //the host will take care of any important logic, and we'll hear about orders to faction units from the host itself, anyway.
                    //in the meantime, the sync data should be corrected within 2-4 seconds, and the next time we'll be able to give orders ourselves, too.
                    if ( ArcenNetworkAuthority.IsClient )
                        return null;

                    string factionList = "Factions In Game: ";

                    for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
                    {
                        Faction fac = World_AIW2.Instance.Factions[j];
                        factionList += "\n" + j + ": ";
                        factionList += fac.GetDisplayName();
                        if ( fac.Type == FactionType.Player )
                        {
                            factionList += " (Players:";
                            if ( !fac.Config.IsFactionControlledByAnyPlayer )
                                factionList += " No controlling accounts";
                            else
                            {
                                for ( int k = 0; k < fac.Config.CountOfPlayersFactionControllingFaction; k++ )
                                {
                                    factionList += " " + fac.Config.GetAccountOfControllingPlayerAtIndex( k )?.Username;
                                }
                            }
                            factionList += ")";
                        }
                    }

                    string str = "No specific faction requested.";
                    if ( factionOrNull != null )
                        str = "Requested faction " + factionOrNull.GetDisplayName();
                    throw new Exception( "FactionUtilityMethods::findHumanKing: No human king found, but game has not been won! At least one Sim-Step has happened since the King died.  " +
                        str + "\n" + factionList );

                }
                HumanKingFailures++;
            }
            else if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "human king on " + kingPlanet.Name, Verbosity.DoNotShow );
            return kingPlanet;
        }

        public Planet findAIKing( bool ThrowErrorIfCannotFind )
        {
            bool debug = false;
            Planet kingPlanet = null;

            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                if ( entity.GetFactionTypeSafe() != FactionType.Player )
                    kingPlanet = entity.Planet;
            }
            if ( kingPlanet == null )
            {
                if ( World.Instance.ConclusionType == CampaignConclusionType.NotConcluded )
                {
                    //on multiplayer clients, sync data may be a bit off, and in that situation we should just silently accept that there's no king to be found at the moment.
                    //the host will take care of any important logic, and we'll hear about orders to faction units from the host itself, anyway.
                    //in the meantime, the sync data should be corrected within 2-4 seconds, and the next time we'll be able to give orders ourselves, too.
                    if ( ArcenNetworkAuthority.IsClient )
                        return null;
                    if ( ThrowErrorIfCannotFind )
                        ArcenDebugging.ArcenDebugLogSingleLine( "findAIKing: No AI king found, but game has not been won!", Verbosity.ShowAsError );
                }
            }
            else if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "AI king on " + kingPlanet.Name, Verbosity.DoNotShow );
            return kingPlanet;
        }

         public Planet findFirstAIKing( bool ThrowErrorIfCannotFind )
        {
            bool debug = false;
            Planet kingPlanet = null;

            foreach (var ai in World_AIW2.Instance.AIFactions)
            {
                foreach ( GameEntity_Squad e in ai.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                        kingPlanet = e.Planet;
                        break;
                    }

                if (kingPlanet != null)
                    break;
            }
            
            if ( kingPlanet == null )
            {
                if ( World.Instance.ConclusionType == CampaignConclusionType.NotConcluded )
                {
                    //on multiplayer clients, sync data may be a bit off, and in that situation we should just silently accept that there's no king to be found at the moment.
                    //the host will take care of any important logic, and we'll hear about orders to faction units from the host itself, anyway.
                    //in the meantime, the sync data should be corrected within 2-4 seconds, and the next time we'll be able to give orders ourselves, too.
                    if ( ArcenNetworkAuthority.IsClient )
                        return null;
                    if ( ThrowErrorIfCannotFind )
                        ArcenDebugging.ArcenDebugLogSingleLine( "findAIKing: No AI king found, but game has not been won!", Verbosity.ShowAsError );
                }
            }
            else if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "AI king on " + kingPlanet.Name, Verbosity.DoNotShow );

            return kingPlanet;
        }

        public bool IsPlanetNearKing( Planet sourceplanet, Int16 hops ) => IsPlanetNearKing( sourceplanet, hops, false );
        public bool IsPlanetNearKing( Planet sourceplanet, Int16 hops, bool stationaryOnly )
        {
            bool output = false;
            foreach ( Planet.PlanetAtHopDistance _phd in sourceplanet.PlanetsWithinXHops_NoFilters( hops ) )
            {
                Planet planet = _phd.Planet;
                foreach ( GameEntity_Squad king in planet.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( stationaryOnly && king.TypeData.IsMobile )
                        continue;

                    output = true;

                    break;
                }
                if ( output == true )
                    break;
            }
            return output;
        }

        public ArcenPoint GetRandomMetalGeneratorOnPlanet( Planet planet, ArcenHostOnlySimContext Context, List<ArcenPoint> WorkingMetalGeneratorList )
        {
            //Gets the Location of a Metal Generator
            WorkingMetalGeneratorList.Clear();
            foreach ( GameEntity_Squad generator in planet.Squads( "MetalGenerator" ) )
            {
                WorkingMetalGeneratorList.Add( generator.WorldLocation );
            }
            if ( WorkingMetalGeneratorList.Count == 0 )
            {
                //this can happen on planets Ravaged by zenith miners
                AngleDegrees angle = AngleDegrees.Create( (float)Context.RandomToUse.Next( 1, 360 ) );
                ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
                //float warpInMultiplier = 0.9f;
                int random = Context.RandomToUse.Next( 10, 90 );
                ArcenPoint chosenSpotLocation = center.GetPointAtAngleAndDistance( angle, (int)(planet.GravWellSize.DistanceScale_GravwellRadius * random) / 100 );
                return chosenSpotLocation;
            }
            return WorkingMetalGeneratorList[Context.RandomToUse.Next( 0, WorkingMetalGeneratorList.Count )];
        }
        public GameEntity_Squad GetRandomMetalGeneratorOnPlanet( Planet planet, ArcenHostOnlySimContext Context, List<SafeSquadWrapper> WorkingMetalGeneratorList, bool mustBePlayerOwned )
        {
            //Gets the GameEntity_Squad for a Metal Generator
            WorkingMetalGeneratorList.Clear();
            foreach ( GameEntity_Squad generator in planet.Squads( "MetalGenerator" ) )
            {
                if ( mustBePlayerOwned &&
                     generator.GetFactionTypeSafe() != FactionType.Player )
                    continue;
                WorkingMetalGeneratorList.Add( generator );
            }
            if ( WorkingMetalGeneratorList.Count == 0 )
                return null;
            return WorkingMetalGeneratorList[Context.RandomToUse.Next( 0, WorkingMetalGeneratorList.Count )].GetSquad();
        }
        public ArcenPoint GetRandomWormholeOnPlanet( Planet planet, ArcenHostOnlySimContext Context, List<ArcenPoint> WorkingWormholeList )
        {
            WorkingWormholeList.Clear();
            foreach ( GameEntity_Other wormhole in planet.Others( OtherSpecialEntityType.Wormhole ) )
            {
                WorkingWormholeList.Add( wormhole.WorldLocation );
            }
            if ( WorkingWormholeList.Count == 0 )
                return Engine_AIW2.Instance.CombatCenter;
            int random = Context.RandomToUse.Next( 0, WorkingWormholeList.Count );
            ArcenPoint point = WorkingWormholeList[random];
            return point;
        }

        public FInt GetCurrentAIP()
        {
            if ( GlobalAIWorldBaseInfo.Instance == null )
                return FInt.Zero;
            return GlobalAIWorldBaseInfo.Instance.AIProgress_Effective;
        }

        public Faction GetRandomAIFactionWithDifficulty( ArcenHostOnlySimContext Context, byte difficulty )
        {
            List<Faction> internalWorkingFactionList = Faction.GetTemporaryFactionList( "FactionUtilityMethods-GetRandomAIFactionWithDifficulty-internalWorkingFactionList", 10f );
            if ( internalWorkingFactionList == null ) //blocked for teardown/shutdown; bail
                return null;

            Faction firstFaction = null;
            for ( int j = 0; j < World_AIW2.Instance.AIFactions.Count; j++ )
            {
                Faction faction = World_AIW2.Instance.AIFactions[j];
                if ( faction.Type == FactionType.AI &&
                     faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant() >= difficulty )
                {
                    if ( firstFaction == null ) //don't instantite a list if we only have one AI faction
                        firstFaction = faction;
                    else
                    {
                        internalWorkingFactionList.Add( faction );
                    }
                }
            }
            if ( internalWorkingFactionList.Count <= 0 )
            {
                Faction.ReleaseTemporaryFactionList( internalWorkingFactionList );
                return firstFaction;
            }
            Faction ret = internalWorkingFactionList[Context.RandomToUse.Next( 0, internalWorkingFactionList.Count )];
            Faction.ReleaseTemporaryFactionList( internalWorkingFactionList );
            return ret;
        }

        public int GetHighestAIDifficulty()
        {
            int highestDifficulty = -1;
            foreach ( Faction faction in World_AIW2.Instance.Factions )
            {
                if ( faction.Type != FactionType.AI )
                    continue;

                int difficulty = faction.BaseInfo.GetDifficultyOrdinal_OrNegativeOneIfNotRelevant();

                if ( difficulty > highestDifficulty )
                    highestDifficulty = difficulty;
            }
            return highestDifficulty;
        }

        public AIDifficulty GetHighestAIDifficulty_AsDifficulty()
        {
            int diff = this.GetHighestAIDifficulty();
            if ( diff < 0 )
                return null;
            return AIDifficultyTable.Instance.GetRowByOrdinal( (byte)diff );
        }

        public Faction GetNomadPlanetFaction() //this one is okay, because we assume there's only one
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction.SpecialFactionData.InternalName == "NomadPlanets" )
                    return otherFaction;
            }
            return null;
        }
        public Faction GetTemplarFaction() //this one is okay, because we assume there's only one
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction.SpecialFactionData.InternalName == "Templar" ||
                     otherFaction.SpecialFactionData.InternalName =="TemplarWithoutNecromancer")
                    return otherFaction;
            }
            return null;
        }
        public Faction GetReapersFaction() //this one is okay, because we assume there's only one
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction.SpecialFactionData.InternalName == "Reapers" ||
                     otherFaction.SpecialFactionData.InternalName == "ReapersWithoutDyson" )
                    return otherFaction;
            }
            return null;
        }
        public Faction GetMalwareForApkalluFaction() //this one is okay, because we assume there's only one
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction.SpecialFactionData.InternalName == "MalwareForApkallu" )
                    return otherFaction;
                if ( otherFaction.SpecialFactionData.InternalName == "MalwareForApkalluEmpire" )
                    return otherFaction;

            }
            return null;
        }

        public Faction GetDysonSidekickFaction() //just return the first one; this is a bit crude, but useful for the Reapers. Doesn't have to be perfect
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction fac = World_AIW2.Instance.Factions[i];
                if ( fac.Type != FactionType.Player )
                    continue;
                PlayerTypeData playerTypeData = fac.PlayerTypeDataOrNull_ModeratelyExpensive;
                if ( playerTypeData == null )
                    continue;

                if (playerTypeData.GetHasTag("DysonSidekick"))
                    return fac;
            }
            return null;
        }

        public Faction GetWormholeInvasionFaction() //this one is okay, because we assume there's only one
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction.SpecialFactionData.InternalName == "WormholeInvasion" )
                    return otherFaction;
            }
            return null;
        }

        public Faction GetMaddenedElderlingsFaction() //this one is okay, because we assume there's only one
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction.SpecialFactionData.InternalName == "MaddenedElderlings" )
                    return otherFaction;
            }
            return null;
        }
        public int HostileElderlingsWithThisPlanetInTerritory( Faction faction, Planet planet, bool mustBeTracked )
        {
            int elderlingCount = 0;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( !otherFaction.GetIsHostileTowards( faction ) )
                    continue;
                if ( !otherFaction.SpecialFactionData.LurableElderling )
                    continue;
                ElderlingsFactionBaseInfo baseInfo = otherFaction.GetExternalBaseInfoAs<ElderlingsFactionBaseInfo>();
                if ( baseInfo.ElderlingsPerPlanet[planet] == 0 )
                    continue; //no elderlings have this planet in their territory
                List<SafeSquadWrapper> elderlings = baseInfo.Elderlings.GetDisplayList();
                for ( int j = 0; j < elderlings.Count; j++ )
                {
                    GameEntity_Squad elderling = elderlings[j].GetSquad();
                    if ( elderling == null )
                        continue;
                    ElderlingsPerUnitBaseInfo data = elderling.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();
                    if ( mustBeTracked && !data.TrackedByPlayer )
                        continue;
                    if ( data.Territory.Contains( planet ) )
                    {
                        elderlingCount++;
                    }
                }
            }
            return elderlingCount;
        }
        public void LureHostileElderlings( Faction faction, Planet planet, bool lureSetting, bool mustBeTracked )
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( !otherFaction.GetIsHostileTowards( faction ) )
                    continue;
                if ( !otherFaction.SpecialFactionData.LurableElderling )
                    continue;
                ElderlingsFactionBaseInfo baseInfo = otherFaction.GetExternalBaseInfoAs<ElderlingsFactionBaseInfo>();
                if ( baseInfo.ElderlingsPerPlanet[planet] == 0 )
                    continue; //no elderlings have this planet in their territory
                List<SafeSquadWrapper> elderlings = baseInfo.Elderlings.GetDisplayList();
                for ( int j = 0; j < elderlings.Count; j++ )
                {
                    GameEntity_Squad elderling = elderlings[j].GetSquad();
                    if ( elderling == null )
                        continue;
                    ElderlingsPerUnitBaseInfo data = elderling.TryGetExternalBaseInfoAs<ElderlingsPerUnitBaseInfo>();
                    if ( mustBeTracked && !data.TrackedByPlayer )
                        continue;
                    if ( data.Territory.Contains( planet ) )
                    {
                        if ( data.LurePlanet == planet && !lureSetting )
                            data.LurePlanet = null;
                        if ( data.LurePlanet != planet && lureSetting )
                            data.LurePlanet = planet;
                    }
                }
            }
        }
        public bool AnyHumanPlayers()
        {
            foreach ( var playerType in PlayerTypeDataTable.Instance.Rows )
            {
                if ( playerType.CurrentFactionsInThisGame.Count <= 0 )
                    continue;
                
                if (playerType.GetHasTag("HumanEmpire"))
                    return true;
            }
            
            return false;
        }
        public bool AnyNonHumanPlayers()
        {
            foreach ( var playerType in PlayerTypeDataTable.Instance.Rows )
            {
                if ( playerType.CurrentFactionsInThisGame.Count <= 0 )
                    continue;
                
                if (!playerType.GetHasTag("HumanEmpire"))
                    return true;
            }
            
            return false;
        }
        public bool OnlyNonHumanPlayers()
        {
            foreach ( var playerType in PlayerTypeDataTable.Instance.Rows )
            {
                if ( playerType.CurrentFactionsInThisGame.Count <= 0 )
                    continue;
                
                if (playerType.GetHasTag("HumanEmpire"))
                    return false;
            }
            
            return true;
        }
        public bool AnyNecromancerFactions()
        {
            foreach ( PlayerTypeData playerType in PlayerTypeDataTable.Instance.Rows )
            {
                if ( playerType.CurrentFactionsInThisGame.Count <= 0 )
                    continue;
                switch (playerType.InternalName )
                {
                    case "NecromancerSidekick":
                    case "NecromancerEmpire":
                      return true;
                }
            }
            return false;
        }
        public bool OnlyNecromancerFactions()
        {
            foreach ( PlayerTypeData playerType in PlayerTypeDataTable.Instance.Rows )
            {
                if ( playerType.CurrentFactionsInThisGame.Count <= 0 )
                    continue;
                switch (playerType.InternalName )
                {
                    case "NecromancerSidekick":
                    case "NecromancerEmpire":
                      continue;
                    default:
                      return false; //Not a necromancer!
                }
            }
            return true;
        }
        public int GetDifficultyFromNecromancerSettings( Faction faction)
        {
            int highestDifficulty = -1;
            foreach ( PlayerTypeData playerType in PlayerTypeDataTable.Instance.Rows )
            {
                if ( playerType.CurrentFactionsInThisGame.Count <= 0 )
                    continue;
                if ( playerType.InternalName != "NecromancerSidekick" &&
                     playerType.InternalName != "NecromancerEmpire" )
                    continue;
                for ( int i = 0; i < playerType.CurrentFactionsInThisGame.Count; i++ )
                {
                    Faction otherFaction = playerType.CurrentFactionsInThisGame[i];
                    ConfigurationForFaction cfg = otherFaction.Config;
                    string requestedDiffStr = String.Empty;
                    int diff = 0;
                    if ( faction.SpecialFactionData.InternalName == "Templar" )
                        requestedDiffStr = cfg.GetStringValueForCustomFieldOrDefaultValue( "Templar_Difficulty", true );
                    else
                        requestedDiffStr = cfg.GetStringValueForCustomFieldOrDefaultValue( "Elderling_Difficulty", true );
                    if ( requestedDiffStr == "Matches Strongest AI" )
                        diff = GetHighestAIDifficulty();
                    else
                        diff = Int32.Parse(requestedDiffStr);
                    if ( highestDifficulty <= diff )
                        highestDifficulty = diff;
                }
            }
            return highestDifficulty;
        }
        public int GetDifficultyFromDysonSidekickSettings( Faction faction)
        {
            int highestDifficulty = -1;
            foreach ( PlayerTypeData playerType in PlayerTypeDataTable.Instance.Rows )
            {
                if ( playerType.CurrentFactionsInThisGame.Count <= 0 )
                    continue;
                if ( playerType.InternalName != "DysonSidekick" &&
                     playerType.InternalName != "DysonEmpire" )
                    continue;
                for ( int i = 0; i < playerType.CurrentFactionsInThisGame.Count; i++ )
                {
                    Faction otherFaction = playerType.CurrentFactionsInThisGame[i];
                    ConfigurationForFaction cfg = otherFaction.Config;
                    string requestedDiffStr = String.Empty;
                    int diff = 0;
                    if ( faction.SpecialFactionData.InternalName == "Reapers" )
                        requestedDiffStr = cfg.GetStringValueForCustomFieldOrDefaultValue( "DysonSidekickIntensity", true );
                    if ( requestedDiffStr == "Matches Strongest AI" )
                        diff = GetHighestAIDifficulty();
                    else
                        diff = Int32.Parse(requestedDiffStr);
                    if ( highestDifficulty <= diff )
                        highestDifficulty = diff;
                }
            }
            return highestDifficulty;
        }
        public int GetDifficultyFromArmadaSettings( Faction faction)
        {
            int highestDifficulty = -1;
            foreach ( PlayerTypeData playerType in PlayerTypeDataTable.Instance.Rows )
            {
                if ( playerType.CurrentFactionsInThisGame.Count <= 0 )
                    continue;
                if ( playerType.InternalName != "ArmadaEmpire" )
                    continue;
                for ( int i = 0; i < playerType.CurrentFactionsInThisGame.Count; i++ )
                {
                    Faction otherFaction = playerType.CurrentFactionsInThisGame[i];
                    ConfigurationForFaction cfg = otherFaction.Config;
                    string requestedDiffStr = String.Empty;
                    int diff = 0;
                    if ( faction.SpecialFactionData.InternalName == "TiberiumInfestationForArmada" )
                        requestedDiffStr = cfg.GetStringValueForCustomFieldOrDefaultValue( "ArmadaDifficulty", true );
                    if ( requestedDiffStr == "Matches Strongest AI" )
                        diff = GetHighestAIDifficulty();
                    else
                        diff = Int32.Parse(requestedDiffStr);
                    if ( highestDifficulty <= diff )
                        highestDifficulty = diff;
                }
            }
            return highestDifficulty;
        }
        public int GetDifficultyFromApkalluSettings( Faction faction)
        {
            int highestDifficulty = -1;
            foreach ( PlayerTypeData playerType in PlayerTypeDataTable.Instance.Rows )
            {
                if ( playerType.CurrentFactionsInThisGame.Count <= 0 )
                    continue;
                if ( playerType.InternalName != "ApkalluSidekick" && playerType.InternalName != "ApkalluInfusedEmpire")
                    continue;
                for ( int i = 0; i < playerType.CurrentFactionsInThisGame.Count; i++ )
                {
                    Faction otherFaction = playerType.CurrentFactionsInThisGame[i];
                    ConfigurationForFaction cfg = otherFaction.Config;
                    string requestedDiffStr = String.Empty;
                    int diff = 0;
                    if ( faction.SpecialFactionData.InternalName == "MalwareForApkallu" || faction.SpecialFactionData.InternalName == "MalwareForApkalluEmpire")
                        requestedDiffStr = cfg.GetStringValueForCustomFieldOrDefaultValue( "Malware_Difficulty", true );
                    if ( requestedDiffStr == "Matches Strongest AI" )
                        diff = GetHighestAIDifficulty();
                    else
                        diff = Int32.Parse(requestedDiffStr);
                    if ( highestDifficulty <= diff )
                        highestDifficulty = diff;
                }
            }
            return highestDifficulty;
        }
        public Faction GetStrongestNecromancerFactionOnPlanet(Planet planet)
        {
            Faction output = null;
            if ( planet == null )
                return output;
            int currentStrongest = 0;
            foreach ( PlayerTypeData playerType in PlayerTypeDataTable.Instance.Rows )
            {
                if ( playerType.CurrentFactionsInThisGame.Count <= 0 )
                    continue;
                switch (playerType.InternalName )
                {
                    case "NecromancerSidekick":
                    case "NecromancerEmpire":
                        break;
                    default:
                        continue; //skip!
                }
                for ( int i = 0; i < playerType.CurrentFactionsInThisGame.Count; i++ )
                {
                    Faction otherFaction = playerType.CurrentFactionsInThisGame[i];
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( otherFaction );
                    if ( pFaction.DataByStance[FactionStance.Self].TotalStrength > currentStrongest )
                    {
                        output = otherFaction;
                        currentStrongest = pFaction.DataByStance[FactionStance.Self].TotalStrength;
                    }
                }
            }
            return output;
        }
        public GameEntity_Squad GetNecropolisForPlanetOrNull(Planet planet, Faction faction)
        {
            GameEntity_Squad output = null;
            NecromancerEmpireFactionBaseInfo baseInfo = faction.GetExternalBaseInfoAs<NecromancerEmpireFactionBaseInfo>();
            if ( baseInfo == null )
                return output;

            foreach ( GameEntity_Squad necropolis in baseInfo.Necropoleis.DisplaySquads() )
            {
                if ( necropolis.Planet == planet )
                {
                    output = necropolis;
                    break;
                }
            }
            return output;
        }
        public Faction GetNearestNecromancerFactionToThisPlanet(Planet planet, ArcenHostOnlySimContext Context)
        {
            if ( planet == null )
                return null;
            
            Faction result = null;
            int closest = 999;
            foreach ( GameEntity_Squad e in World_AIW2.Instance.Squads( "NecromancerNecropolis" ) )
            {
                int d = planet.GetHopsTo(e.Planet);
                if (d < closest)
                {
                    closest = d;
                    result = e.GetFactionOrNull_Safe();
                }
            }

            return result;
        }

        public bool AnyDysonSidekickFactions()
        {
            foreach ( PlayerTypeData playerType in PlayerTypeDataTable.Instance.Rows )
            {
                if ( playerType.CurrentFactionsInThisGame.Count <= 0 )
                    continue;
                switch (playerType.InternalName )
                {
                    case "DysonSidekick":
                    case "DysonEmpire":
                      return true;
                }
            }
            return false;
        }

        public Faction GetAIReservesFaction() //this one is okay, because we assume there's only one
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction.SpecialFactionData.InternalName == "AIReserves" )
                    return otherFaction;
            }
            return null;
        }

        public AIReservesFactionBaseInfo GetAIReservesFactionBaseInfo() //this one is okay, because we assume there's only one
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction.SpecialFactionData.InternalName == "AIReserves" )
                    return otherFaction.GetExternalBaseInfoAs<AIReservesFactionBaseInfo>();
            }
            return null;
        }

        public WormholeInvasionFactionBaseInfo GetWormholeInvasionFactionBaseInfo() //this one is okay, because we assume there's only one
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction.SpecialFactionData.InternalName == "WormholeInvasion" )
                    return otherFaction.GetExternalBaseInfoAs<WormholeInvasionFactionBaseInfo>();
            }
            return null;
        }

        public Faction GetHumanResistanceFightersFaction() //this one is okay, because we assume there's only one
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction.SpecialFactionData.InternalName == "HumanResistanceFighters" )
                    return otherFaction;
            }
            return null;
        }
        public HumanResistanceFighterFactionBaseInfo GetHumanResistanceFightersBaseInfo() //this one is okay, because we assume there's only one
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction.SpecialFactionData.InternalName == "HumanResistanceFighters" )
                    return otherFaction.GetExternalBaseInfoAs<HumanResistanceFighterFactionBaseInfo>();
            }
            return null;
        }

        public Faction GetDarkSpireFaction() //this one is okay, because we assume there's only one
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction.SpecialFactionData.InternalName == "DarkSpire" )
                    return otherFaction;
            }
            return null;
        }

        public DarkSpireFactionBaseInfo GetDarkSpireFactionBaseInfo() //this one is okay, because we assume there's only one
        {
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction.SpecialFactionData.InternalName == "DarkSpire" )
                    return otherFaction.GetExternalBaseInfoAs<DarkSpireFactionBaseInfo>();
            }
            return null;
        }

        public int GetPlayerDifficultyToRouteToPlanet( Planet TargetPlanet )
        {
            //Get the cost to go to a planet. This is used only for the UI, so use this Non-Sim mechanism
            //where we calculate the values on the Sim thread and only look at it from the UI thread. See GetColorForLinkBetweenPlanets for
            //another version of this
            Dictionary<int, int> lookup = LocalPlayerWorldBaseInfo.Instance.NonSim_CostToPlanet;
            if ( lookup == null )
                return 0; //this can be null sometimes at game load
            return lookup[TargetPlanet.Index];
        }

        public int GetNumPlanetsControlledByAllies( Faction faction )
        {
            int numPlanets = 0;

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                Faction controllingFaction = planet.GetControllingOrInfluencingFaction();
                if ( controllingFaction.GetIsFriendlyTowards( faction ) )
                    numPlanets++;
            }
            return numPlanets;
        }
        public Planet FindStrongestEnemyPlanetOrNull( Faction faction )
        {
            Planet bestPlanet = null;
            int bestPlanetStrength = 0;

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                Faction controllingFaction = planet.GetControllingOrInfluencingFaction();
                if ( !controllingFaction.GetIsHostileTowards( faction ) )
                    continue;

                PlanetFaction pFaction = planet.GetPlanetFactionForFaction( faction );
                int totalEnemyStrength = pFaction.DataByStance[FactionStance.Hostile].TotalStrength;

                if ( totalEnemyStrength > bestPlanetStrength || bestPlanet == null )
                {
                    bestPlanet = planet;
                    bestPlanetStrength = totalEnemyStrength;
                }
            }
            return bestPlanet;
        }
        public void TachyonBlastPlanet( Planet targetPlanet, Faction faction, ArcenSimContextAnyStatus Context, bool ShowMessage = true )
        {
            //this function decloaks all hostile ships on a planet.
            //This is done for cases where a minor faction has crushed all the major defenses of a planet,
            //but there are some cloaked things hiding out, which is messing with the logic
            PlanetFaction pFaction = targetPlanet.GetPlanetFactionForFaction( faction );

            int numShipsDecloaked = 0;
            int numPlayerShipsDecloaked = 0;
            foreach ( PlanetFaction otherFaction in pFaction.RelatedFactions( FactionRelationship.FactionsThatAreHostileTowardsMe ) )
            {
                foreach ( GameEntity_Squad entity in otherFaction.Entities.Squads( EntityRollupType.HasAnyInternalCloakingAbility ) )
                {
                    if ( entity.GetMatches_SemiSlow( EntityRollupType.MobileFleetFlagships ) )
                        continue; //make sure the marauders or someone else don't tachyon blast a flagship
                    if ( !entity.UnitForciblyDecloaked && //we haven't already decloaked this unit
                         entity.SecondsSpentAsRemains <= 0 && //and this unit is not remains
                         !entity.GetHasBeenDestroyed() ) //this check is probably just for badger's paranoia
                    {
                        //Nota Bene: going through a wormhole clears the "unit forcibly decloaked" flag
                        entity.UnitForciblyDecloaked = true;
                        numShipsDecloaked++;
                        if (  entity.GetFactionTypeSafe() == FactionType.Player &&
                              (entity.TypeData.IsMobileCombatant || entity.FleetMembership.Fleet.Category != FleetCategory.PlayerPlanetaryCommand ) ) //only warn the player for combat units or units attached to a mobile fleet
                            numPlayerShipsDecloaked++;
                    }
                }
            }
            if ( (numPlayerShipsDecloaked > 0 ||
                 numShipsDecloaked > 10) &&
                 targetPlanet.IntelLevel >= PlanetIntelLevel.CurrentlyWatched )
            {
                Faction controllingFaction = targetPlanet.GetControllingFaction();
                if ( ArcenNetworkAuthority.GetIsHostMode() && ShowMessage )
                {
                    PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                    if ( chatHandlerOrNull != null )
                        chatHandlerOrNull.PlanetToView = targetPlanet;

                    World_AIW2.Instance.QueueChatMessageOrCommand( "The <color=#" + faction.FactionCenterColor.ColorHexBrighter + "> " + faction.GetDisplayName() +
                        " </color> are flooding <color=#" + controllingFaction.FactionCenterColor.ColorHexBrighter + ">" + targetPlanet.Name + "</color> with tachyon radiation to reveal cloaked enemies!",
                        ChatType.LogToCentralChat, chatHandlerOrNull );
                }
            }
        }
        public bool GetIsKingThreatened( Faction faction )
        {
            GameEntity_Squad king = null;
            foreach ( GameEntity_Squad entity in faction.Squads( EntityRollupType.KingUnitsOnly ) )
            {
                king = entity;
                break;
            }
            if ( king == null )
                return false;

            Planet kingPlanet = king.Planet;
            bool enemiesOnNearbyPlanets = false;
            StrengthData_PlanetFaction_Stance hostileData = kingPlanet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Hostile];
            StrengthData_PlanetFaction_Stance selfData = kingPlanet.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Self];
            if ( hostileData.TotalStrength > selfData.TotalStrength / 5 )
                enemiesOnNearbyPlanets = true;

            foreach ( Planet neighbor in kingPlanet.LinkedNeighbors( false ) )
            {
                hostileData = neighbor.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Hostile];
                selfData = neighbor.GetPlanetFactionForFaction( faction ).DataByStance[FactionStance.Self];
                if ( hostileData.TotalStrength > selfData.TotalStrength / 5 )
                {
                    enemiesOnNearbyPlanets = true;
                    break;
                }
            }
            return enemiesOnNearbyPlanets;
        }
        public void CalculateAvailableExogalacticUnits( FInt level, DrawBag<GameEntityTypeData> bag ) //note: since this is on the heap, by ref means nothing anyway
        {
            bag.Clear();
            if ( level >= 1 )
            {
                GameEntityTypeDataTable.Instance.AddAllRowsWithTagToBag( ref bag, "ExtragalacticWarTier1" );
            }
            if ( level >= 2 )
            {
                GameEntityTypeDataTable.Instance.AddAllRowsWithTagToBag( ref bag, "ExtragalacticWarTier2" );
            }
            if ( level >= 3 )
            {
                GameEntityTypeDataTable.Instance.AddAllRowsWithTagToBag( ref bag, "ExtragalacticWarTier3" );
            }
            if ( level >= 4 )
            {
                GameEntityTypeDataTable.Instance.AddAllRowsWithTagToBag( ref bag, "ExtragalacticWarTier4" );
            }
            if ( level >= 5 )
            {
                GameEntityTypeDataTable.Instance.AddAllRowsWithTagToBag( ref bag, "ExtragalacticWarTier5" );
            }
        }
        public bool IsPlayerPlanetWithinXHops( Planet planet, Int16 minhops, ArcenHostOnlySimContext Context )
        {
            bool foundPlanet = false;
            foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( (Int16)(minhops - 1) ) )
            {
                Planet otherPlanet = _phd.Planet;
                if ( otherPlanet.GetControllingFactionType() == FactionType.Player )
                {
                    foundPlanet = true;
                    break;
                }
            }
            return foundPlanet;
        }
        public bool IsPlayerPlanetAtLeastXHopsAway( Planet planet, Int16 maxhops, ArcenHostOnlySimContext Context )
        {
            bool foundPlanet = false;
            foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( (Int16)(maxhops - 1) ) )
            {
                Planet otherPlanet = _phd.Planet;
                if ( otherPlanet.GetControllingFactionType() == FactionType.Player )
                {
                    foundPlanet = true;
                    break;
                }
            }
            if ( !foundPlanet )
                return true;
            return false;
        }
        public Int16 GetHopsToPlayerPlanet( Planet planet, ArcenHostOnlySimContext Context )
        {
            if ( planet.GetControllingFactionType() == FactionType.Player )
                return 0;
            Int16 hops = -1;
            foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( -1 ) )
            {
                Planet otherPlanet = _phd.Planet;
                Int16 distance = _phd.Hops;
                if ( otherPlanet.GetControllingFactionType() == FactionType.Player )
                {
                    hops = distance;
                    break;
                }
            }
            return hops;
        }
        public void SerializeDictionary( SerMetaData MetaData, Dictionary<DZResource, int> dict, ArcenSerializationBuffer Buffer )
        {
            SerializeDictionary( MetaData, dict, Buffer, "Unknown Dict" );
        }
        public void SerializeDictionary( SerMetaData MetaData, Dictionary<DZResource, int> dict, ArcenSerializationBuffer Buffer, string DictName )
        {
            if ( dict == null )
            {
                Buffer.AddByte( MetaData, ReadStyleByte.VeryOftenZero, 0, DictName );
                return;
            }
            Buffer.AddByte( MetaData, ReadStyleByte.VeryOftenZero, (byte)dict.Count, DictName );
            foreach ( KeyValuePair<DZResource, int> kv in dict )
            {
                Buffer.AddByte( MetaData, ReadStyleByte.Normal, (byte)kv.Key, "DictKey" );
                Buffer.AddInt32( MetaData, ReadStyle.NonNeg, kv.Value, "DictVal" );
            }
        }
        public void DeserializeDictionary( SerMetaData MetaData, Dictionary<DZResource, int> dict, ArcenDeserializationBuffer Buffer )
        {
            DeserializeDictionary( MetaData, dict, Buffer, "Unknown Dict" );
        }
        public void DeserializeDictionary( SerMetaData MetaData, Dictionary<DZResource, int> dict, ArcenDeserializationBuffer Buffer, string DictName )
        {
            byte count = Buffer.ReadByte( MetaData, ReadStyleByte.VeryOftenZero, DictName );
            for ( int i = 0; i < count; i++ )
            {
                DZResource resource = (DZResource)Buffer.ReadByte( MetaData, ReadStyleByte.Normal, "DictKey" );
                dict[resource] = Buffer.ReadInt32( MetaData, ReadStyle.NonNeg, "DictVal" );
            }
        }
        public void PrintDZDictionaryForTerminus( Dictionary<DZResource, int> dict, ref ArcenCharacterBufferBase Buffer, DZResource primaryResource )
        {
            int debugCode = 0;
            if ( dict == null )
                return;
            try
            {
                if ( dict.ContainsKey( primaryResource ) && dict[primaryResource] != 0 )
                {
                    Buffer.Add( "This Terminus has " ).Add( dict[primaryResource].ToString(), DarkZenithFactionBaseInfo.ResourceColour[primaryResource] ).Add( " " ).Add( DarkZenithFactionBaseInfo.ResourceFancyName[primaryResource], DarkZenithFactionBaseInfo.ResourceColour[primaryResource] ).Add( " ready to be picked up by a Transport." ).Add( "\n" );
                }
                else
                    Buffer.Add( "This Terminus has " ).Add( "no " + DarkZenithFactionBaseInfo.ResourceFancyName[primaryResource], DarkZenithFactionBaseInfo.ResourceColour[primaryResource] ).Add( " created at this time." ).Add( "\n" );
                if ( primaryResource != DZResource.Metal )
                    Buffer.Add( "This Terminus is using the following to create new resources: " );
                foreach ( KeyValuePair<DZResource, int> kv in dict )
                {
                    debugCode = 10;
                    if ( kv.Value != 0 && kv.Key != primaryResource )
                    {
                        debugCode = 20;
                        Buffer.Add( kv.Value.ToString(), DarkZenithFactionBaseInfo.ResourceColour[kv.Key] ).Add( " " );
                    }
                }
                Buffer.Add( "\n" );
                debugCode = 30;
            }
            catch ( Exception )
            {
                if ( debugCode > 0 ) { }
                //this is used in the UI code, so it can race and that's okay
            }
        }
        public void PrintDZDictionary( Dictionary<DZResource, int> dict, ref ArcenCharacterBufferBase Buffer, string title )
        {
            //this is used for printing to a buffer
            if ( !String.IsNullOrEmpty( title ) )
                Buffer.Add( title ).Add( ": " );
            int debugCode = 0;
            if ( dict == null )
                return;
            try
            {
                foreach ( KeyValuePair<DZResource, int> kv in dict )
                {
                    debugCode = 10;
                    if ( kv.Value != 0 )
                    {
                        debugCode = 20;
                        Buffer.Add( kv.Value.ToString(), DarkZenithFactionBaseInfo.ResourceColour[kv.Key] ).Add( " " );
                    }
                }
                debugCode = 30;
                if ( !String.IsNullOrEmpty( title ) )
                    Buffer.Add( "\n" );
            }
            catch ( Exception )
            {
                if ( debugCode > 0 ) { }
                //this is used in the UI code, so it can race and that's okay
            }
        }
        public FInt GetOverallPowerLevelOfPlayersAndAlliedFactions()
        {
            FInt powerLevel = FInt.Zero;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction faction = World_AIW2.Instance.Factions[i];
                if ( faction == null )
                    continue;
                if ( faction.Type == FactionType.Player )
                    powerLevel += faction.OverallPowerLevel;
                if ( faction.BaseInfo.Allegiance == "对玩家友好" )
                    powerLevel += faction.OverallPowerLevel;
            }
            return powerLevel;
        }
        public bool DoesPlanetHaveVengeanceGenerator( Planet planet )
        {
            DarkSpireFactionBaseInfo dsdata = FactionUtilityMethods.Instance.GetDarkSpireFactionBaseInfo();
            if ( dsdata == null )
                return false;
            if ( dsdata.DarkSpirePlanets.DisplayContains(planet)) // 
            {
                return true;
            }
            return false;
        }

        public bool DoesPlanetHaveAIEye( Planet planet )
        {
            if ( planet.GetControllingOrInfluencingFaction().Type != FactionType.AI )
                return false;
            bool hasEye = false;
            Faction factionForEyeCheck = planet.GetControllingFaction();
            foreach ( GameEntity_Squad entity in factionForEyeCheck.Squads( "Eye" ) )
            {
                if ( entity.Planet == planet )
                {
                    hasEye = true;
                    break;
                }
            }
            return hasEye;
        }
        public FInt GetOverallPowerLevelOfEnemies( Faction faction )
        {
            FInt powerLevel = FInt.Zero;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.Factions[i];
                if ( otherFaction == null || faction == otherFaction )
                    continue;
                if ( !faction.GetIsHostileTowards( otherFaction ) )
                    continue;
                if ( otherFaction.Type == FactionType.AI && otherFaction.FactionIsDefeated )
                    continue; //don't count dead AIs
                powerLevel += otherFaction.OverallPowerLevel;
            }
            return powerLevel;
        }
        public bool IsFactionAlliedToAnyPlayer( Faction faction )
        {
            if ( faction.Type == FactionType.Player )
                return true; //vacuously true
            if ( faction.SpecialFactionData.AlwaysFriendlyToPlayers )
                return true;

            for ( int j = 0; j < World_AIW2.Instance.Factions.Count; j++ )
            {
                if ( World_AIW2.Instance.Factions[j] == null )
                    continue;
                if ( World_AIW2.Instance.Factions[j].Type == FactionType.Player && faction.GetIsFriendlyTowards( World_AIW2.Instance.Factions[j] ) )
                    return true;
            }
            return false;
        }
        public int GetActiveVassalMissionCount( Faction faction, VassalMissionType missionType )
        {
            int output = 0;
            for ( int i = 0; i < faction.Missions.Count; i++ )
            {
                if ( faction.Missions[i].Type == missionType )
                {
                    output++;
                }
            }
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.Missions.Count > 0 )
                {
                    //are there any missions here?
                    VassalMission mission = planet.GetMissionForFaction( faction, missionType );
                    if ( mission == null )
                        continue; //someone has a mission but not us

                    output++;
                }
            }
            return output;
        }

        public void GetActiveVassalMissions( Faction faction, VassalMissionType missionType, List<VassalMission> ListToFill )
        {
            ListToFill.Clear();
            for ( int i = 0; i < faction.Missions.Count; i++ )
            {
                if ( faction.Missions[i].Type == missionType )
                {
                    ListToFill.Add( faction.Missions[i] );
                }
            }

            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                if ( planet.Missions.Count > 0 )
                {
                    //are there any missions here?
                    VassalMission mission = planet.GetMissionForFaction( faction, missionType );
                    ListToFill.Add(mission);
                }
            }
        }

        public bool Helper_RaidSpecificPlanet( List<SafeSquadWrapper> ships, Planet originPlanet, Faction faction, Galaxy galaxy, Planet threatplanet,
            bool IgnorePathCosts, ArcenLongTermIntermittentPlanningContextBase Context, PerFactionPathCache PathCacheData, float RequiredTimeBetweenCommands )
        {
            return Helper_RaidSpecificPlanet( ships, originPlanet, faction, galaxy, threatplanet, IgnorePathCosts, Context, PathCacheData,
                BaseGameCommand.Code.SetWormholePath_UtilRaidSpecific, RequiredTimeBetweenCommands );
        }

        public bool Helper_RaidSpecificPlanet( List<SafeSquadWrapper> ships, Planet originPlanet, Faction faction, Galaxy galaxy, Planet threatplanet, 
            bool IgnorePathCosts, ArcenLongTermIntermittentPlanningContextBase Context, PerFactionPathCache PathCacheData,
            BaseGameCommand.Code SpecificRaidCommand, float RequiredTimeBetweenCommands )
        {
            if ( Context == null )
                return false; //client

            if ( ships.Count <= 0 )
                return false;

            //if any of the ships in the list have been given orders too recently, then don't give orders to any of them right now
            for ( int i = 0; i < ships.Count; i++ )
            {
                if ( ArcenTime.TimeSinceStartF - ships[i].HostOnly_TimeWasLastGivenOrderFromLRP < RequiredTimeBetweenCommands )
                    return false;
            }
            

            /* Set up the data to let us get from any planet to any other given planet */
            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "Helper_RaidSpecificPlanet", originPlanet, threatplanet, PathingMode.Default, Context, PathCacheData );
            if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
            {
                for ( int i = 0; i < ships.Count; i++ )
                {
                    ships[i].SetHostOnly_TimeWasLastGivenOrderFromLRP( ArcenTime.TimeSinceStartF );
                }
                GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[SpecificRaidCommand], GameCommandSource.AnythingElse );
                command.RelatedString = "Util_RaidSpecific";
                for ( int k = 0; k < ships.Count; k++ )
                    command.RelatedEntityIDs.Add( ships[k].GetPrimaryKeyID() );
                for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                    command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                World_AIW2.Instance.QueueGameCommand( faction, command, false );
                return true;
            }
            else
                return false;
        }

        #region IsACoreAISubFaction
        public bool IsACoreAISubFaction( Faction faction )//does not trigger on "shared" AI factions such as anti-player zombies, Astro Trains, Instigators, AI Reserves, etc...
        {
            if ( faction == null )
            {
                return false;
            }
            if ( faction.SpecialFactionData == null )
            {
                return false;
            }
            switch ( faction.SpecialFactionData.InternalName )
            {
                //case "AI": AI is the parent faction, not the subfactions
                case "AIWarden":
                case "HunterFleet":
                case "PraetorianGuard":
                case "AICrossPlanetAttacker":
                case "AIRelentlessWave":
                case "AIBorderAggression":
                    return true; //these all have a GetFactionParent() that is AI / AISentinels
            }
            return false;
        }
        #endregion

        #region IsAIWarden
        public bool IsAIWarden( Faction faction )
        {
            if ( faction == null )
            {
                return false;
            }
            if ( faction.SpecialFactionData == null )
            {
                return false;
            }
            switch ( faction.SpecialFactionData.InternalName )
            {
                case "AIWarden":
                    return true;
            }
            return false;
        }
        #endregion

        #region IsPraetorianGuard
        public bool IsPraetorianGuard( Faction faction )
        {
            if ( faction == null )
            {
                return false;
            }
            if ( faction.SpecialFactionData == null )
            {
                return false;
            }
            switch ( faction.SpecialFactionData.InternalName )
            {
                case "PraetorianGuard":
                    return true;
            }
            return false;
        }
        #endregion

        #region IsHunter
        public bool IsHunter( Faction faction )
        {
            if ( faction == null )
            {
                return false;
            }
            if ( faction.SpecialFactionData == null )
            {
                return false;
            }
            switch ( faction.SpecialFactionData.InternalName )
            {
                case "HunterFleet":
                    return true;
            }
            return false;
        }
        #endregion
        #region GetAlliedPlanetsConnectedToThis
        public static void GetAlliedPlanetsConnectedToThis( List<Planet> listToFill, Planet planet, Faction faction, ArcenHostOnlySimContext Context)
        {
            listToFill.Clear();
            foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops( -1,
                delegate ( Planet secondaryPlanet )
                {
                    if ( secondaryPlanet.GetControllingOrInfluencingFaction().GetIsHostileTowards( faction ) )
                        return PropogationEvaluation.No;
                    var pFaction = secondaryPlanet.GetStanceDataForFaction( faction );
                    if ( pFaction[FactionStance.Hostile].TotalStrength * 2 >=
                         pFaction[FactionStance.Self].TotalStrength + pFaction[FactionStance.Friendly].TotalStrength )
                        return PropogationEvaluation.No;
                    return PropogationEvaluation.Yes;
                } ) )
            {
                Planet otherPlanet = _phd.Planet;
                var pFaction = otherPlanet.GetStanceDataForFaction( faction );
                if ( otherPlanet.GetControllingOrInfluencingFaction().GetIsHostileTowards( faction ) )
                    continue;
                if ( pFaction[FactionStance.Hostile].TotalStrength * 2 >=
                     pFaction[FactionStance.Self].TotalStrength + pFaction[FactionStance.Friendly].TotalStrength )
                    continue;
                if ( otherPlanet.GetControllingOrInfluencingFaction() != faction )
                    continue;
                listToFill.Add(otherPlanet);
            }
        }
        #endregion
        public static void GetPlanetsWithWarpGates( List<Planet> listToFill, Faction faction, ArcenHostOnlySimContext Context )
        {
            listToFill.Clear();
            foreach ( GameEntity_Squad entity in faction.Squads( EntityRollupType.WarpEntryPoints ) )
            {
                listToFill.Add(entity.Planet);
            }
        }
        public static GameEntity_Squad GetRandomWarpGate( List<GameEntity_Squad> listToFill, Faction faction, ArcenHostOnlySimContext Context )
        {
            listToFill.Clear();
            foreach ( GameEntity_Squad entity in faction.Squads( EntityRollupType.WarpEntryPoints ) )
            {
                listToFill.Add(entity);
            }
            if ( listToFill.Count == 0 )
                return null;
            return listToFill[Context.RandomToUse.Next(0, listToFill.Count)];
        }

        public static Planet FindSuitablePlanet( ArcenHostOnlySimContext Context, Int16 minHopsFromHumanPlanet, Int16 maxHopsFromHumanPlanet, byte preferredMarkUnderX, bool aiMustOwnPlanet )
        {
            bool debug = false;
            Galaxy galaxy = World_AIW2.Instance.CurrentGalaxy;
            int allowedRetries = 6; //was 100, and that's likely to break the game in the late game.
            int retries = 0;

            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "Finding a suitable planet for a dyson antagonizer. Default params: minHopsFromHumanPlanet " + minHopsFromHumanPlanet + " maxHopsFromHumanPlanet " + maxHopsFromHumanPlanet + " preferredMarkUnderX " + preferredMarkUnderX, Verbosity.DoNotShow );

            List<Planet> findSuitablePlanet_WorkingList = Planet.GetTemporaryPlanetList( "DysonUtil-FindSuitablePlanet-findSuitablePlanet_WorkingList", 10f );
            if ( findSuitablePlanet_WorkingList == null ) //blocked for teardown/shutdown; bail
                return null;

            do
            {
                if ( debug )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "retry " + retries + " minHopsFromHumanPlanet " + minHopsFromHumanPlanet + " maxHopsFromHumanPlanet " + maxHopsFromHumanPlanet + " preferredMarkUnderX " + preferredMarkUnderX, Verbosity.DoNotShow );
                }
                if ( retries == 1 )
                {
                    //if the first attempt failed, expand the hop radius
                    minHopsFromHumanPlanet--;
                    maxHopsFromHumanPlanet++;
                }
                else if ( retries == 2 )
                {
                    //if the second attempt fails, allow for more marks
                    preferredMarkUnderX++;
                }
                else if ( retries > 2 )
                {
                    //for higher retry levels, just make it work
                    minHopsFromHumanPlanet--;
                    maxHopsFromHumanPlanet++;
                    preferredMarkUnderX++;
                }
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    if ( planet.IntelLevel == PlanetIntelLevel.Unexplored )
                        continue;
                    if ( aiMustOwnPlanet && planet.GetControllingFactionType() != FactionType.AI )
                        continue;
                    if ( planet.MarkLevelForAIOnly.Ordinal >= preferredMarkUnderX )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its mark level is " + planet.MarkLevelForAIOnly.Ordinal, Verbosity.DoNotShow );

                        continue;
                    }
                    bool foundKing = false;
                    foreach ( GameEntity_Squad entity in planet.Squads( EntityRollupType.KingUnitsOnly ) )
                    {
                        if ( entity != null )
                        {
                            foundKing = true;
                        }
                        break;
                    }
                    if ( foundKing )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because it is a king planet", Verbosity.DoNotShow );
                        continue;
                    }

                    bool adjacentKing = false;
                    foreach ( Planet neighbor in planet.LinkedNeighbors( false ) )
                    {
                        foreach ( GameEntity_Squad entity in neighbor.Squads( EntityRollupType.KingUnitsOnly ) )
                        {
                            if ( entity != null )
                                adjacentKing = true;
                            break;
                        }
                        if ( adjacentKing )
                            break;
                    }
                    if ( adjacentKing == true )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because its adjacent to a king planet", Verbosity.DoNotShow );
                        continue;
                    }

                    if ( planet.GetControllingPlanetFaction().DataByStance[FactionStance.Hostile].TotalStrength > FInt.Zero )
                    {
                        if ( debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because there are enemies on it", Verbosity.DoNotShow );

                        continue;
                    }
                    if ( minHopsFromHumanPlanet > 0 )
                    {
                        //this planet must not be too close to a player planet
                        bool foundPlayerPlanetWithinMinHops = false;
                        foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( minHopsFromHumanPlanet ) )
                        {
                            Planet otherPlanet = _phd.Planet;
                            if ( otherPlanet.GetControllingFactionType() == FactionType.Player )
                                foundPlayerPlanetWithinMinHops = true;
                        }
                        if ( foundPlayerPlanetWithinMinHops )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because it within " + minHopsFromHumanPlanet + " hops from a human planet", Verbosity.DoNotShow );
                            continue;
                        }
                    }
                    if ( maxHopsFromHumanPlanet > 0 )
                    {
                        //this planet can't be too far from a player planet
                        bool foundPlayerPlanetWithinMaxHops = false;
                        foreach ( Planet.PlanetAtHopDistance _phd in planet.PlanetsWithinXHops_NoFilters( maxHopsFromHumanPlanet ) )
                        {
                            Planet otherPlanet = _phd.Planet;
                            if ( otherPlanet.GetControllingFactionType() == FactionType.Player )
                                foundPlayerPlanetWithinMaxHops = true;
                        }
                        if ( !foundPlayerPlanetWithinMaxHops )
                        {
                            if ( debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Omitting " + planet.Name + " because it too far from a human planet; max hops " + maxHopsFromHumanPlanet, Verbosity.DoNotShow );
                            continue;
                        }
                    }
                    findSuitablePlanet_WorkingList.Add( planet );
                }
                retries++;
            } while ( findSuitablePlanet_WorkingList.Count == 0 && retries < allowedRetries );
            if ( findSuitablePlanet_WorkingList.Count == 0 )
            {
                Planet.ReleaseTemporaryPlanetList( findSuitablePlanet_WorkingList );
                return null;
            }
            Planet ret = findSuitablePlanet_WorkingList[Context.RandomToUse.Next( 0, findSuitablePlanet_WorkingList.Count )];
            Planet.ReleaseTemporaryPlanetList( findSuitablePlanet_WorkingList );
            return ret;
        }

        public static Faction GetFactionByName( string name )
        {
            Faction returnFaction = null;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                //this code is mostly here because I was originally going to not let the death of Dark Spire ships
                //give the vengeance generators energy
                //Now it's here as an example in case I need it
                Faction faction = World_AIW2.Instance.Factions[i];
                if ( faction.SpecialFactionData.InternalName == name )
                    returnFaction = faction;
            }
            return returnFaction;
        }
        public FInt GetOverallPowerLevelForGeneralHuman( Faction faction )
        {
            FInt newResult = FInt.Zero;
            //The player overall power is calculated on 4 axes; Planets Controlled, Total Fleet Strength, Officer Fleets, and Science
            //Each axis is worth 0.25 (1.0 is where the extragalactic war kicks in
            FInt totalOfficerFleetLevel = FInt.Zero;
            FInt totalStrength = FInt.Zero;
            bool foundMarkVOfficer = false;
            int totalOfficers = 0;
            foreach ( Fleet fleet in World_AIW2.Instance.Fleets( faction, FleetStatus.CenterpieceMustLiveOrLooseFleet ) )
            {
                GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                if ( centerpiece == null )
                    continue;
                if ( centerpiece.PlanetFaction.Faction != faction )
                    continue;
                if ( centerpiece.GetMatches_SemiSlow( EntityRollupType.MobileFleetFlagships ) ||
                     centerpiece.GetMatches_SemiSlow( EntityRollupType.MobileCombatFlagships ) )
                {
                    totalStrength += fleet.GetMaxStrengthOfFleet_ForUIOnly( false );
                }
                if ( centerpiece.TypeData.SpecialType == SpecialEntityType.MobileOfficerCombatFleetFlagship )
                {
                    //TODO: don't count fallen spire fleets, since the power there is tracked in the FallenSpire power level itself
                    totalOfficerFleetLevel += centerpiece.CurrentMarkLevel;
                    totalOfficers++;
                    if ( centerpiece.CurrentMarkLevel >= 5 )
                        foundMarkVOfficer = true;
                }
            }
            FInt officerFleetComponent = FInt.Zero;
            if ( foundMarkVOfficer )
                officerFleetComponent += FInt.FromParts( 0, 050 );
            if ( totalOfficers > 3 )
                officerFleetComponent += FInt.FromParts( 0, 150 );
            if ( totalOfficers > 6 )
                officerFleetComponent += FInt.FromParts( 0, 075 );

            FInt totalFleetStrengthComponent = FInt.Zero;
            totalStrength /= 1000; //adjust the total strength by a factor of 1000, to match the UI
            if ( totalStrength > 400 )
                totalFleetStrengthComponent = (totalStrength - 400) / 500;
            if ( totalFleetStrengthComponent > FInt.FromParts( 0, 250 ) )
            {
                totalFleetStrengthComponent = FInt.FromParts( 0, 250 );
            }
            //compute the science component. Note that this can go "just a bit" over 0.25
            int scienceForFullEffect = 35000;
            FInt scienceRatio = (FInt)faction.GetTotalSpentScience() / (FInt)scienceForFullEffect;
            if ( scienceRatio > FInt.FromParts( 1, 200 ) )
                scienceRatio = FInt.FromParts( 1, 200 );
            FInt scienceComponent = FInt.FromParts( 0, 250 ) * scienceRatio;

            int hackingForFullEffect = 350;
            FInt hackingRatio = (FInt)faction.GetTotalSpentHacking() / (FInt)hackingForFullEffect;
            if ( hackingRatio > FInt.One )
                hackingRatio = FInt.One;
            FInt hackingComponent = FInt.FromParts( 0, 250 ) * hackingRatio;
            newResult = officerFleetComponent + totalFleetStrengthComponent + scienceComponent + hackingComponent;

            int difficulty = FactionUtilityMethods.Instance.GetHighestAIDifficulty();
            if ( difficulty < 7 && faction.OverallPowerLevel >= FInt.One )
                newResult = FInt.FromParts( 0, 900 ); //at difficulties lower than 7, never allow the player to trigger an extragalactic war unit without allies

            //and crank up the power level for harshness
            newResult *= AIWar2GalaxySettingTable.GetIsIntValueFromSettingByName_DuringGame( "PowerLevelMultiplier" );
            return newResult;
        }
        public void FinishTracing( ArcenCharacterBuffer tracingBuffer )
        {
            if ( tracingBuffer == null )
                return;
            if ( !tracingBuffer.GetIsEmpty() )
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
            tracingBuffer.ReturnToPool();
            tracingBuffer = null;
        }

        public Faction GetFactionBlockingVictoryOrNull()
        {
            bool trace = GameSettings.Current.GetBoolBySetting( "Debug_LogVictoryBlocking" );
            
            var player = World_AIW2.Instance.GetFirstPlayerFactionOrNull();
            if ( player == null )
                return null;
            
            foreach ( var faction in World_AIW2.Instance.Factions )
            {
                if ( faction.GetIsHostileTowards( player ) == false )
                {
                    continue;
                }
                
                if ( faction.Type == FactionType.AI &&
                     AIWar2GalaxySettingQuickAccess.DefeatAllAIs == false )
                {
                    continue;
                }
                
                if ( faction.Type == FactionType.Player ||
                     faction.Type == FactionType.NaturalObject )
                {
                    continue;
                }
                
                if ( faction.Type == FactionType.SpecialFaction &&
                     AIWar2GalaxySettingQuickAccess.DefeatAllMinorFactions == false &&
                     faction.SpecialFactionData.MustDefeated == false )
                {
                    continue;
                }
                 
                if ( faction.CheckBlocksVictory() )
                {
                    if ( trace ) ArcenDebugging.SingleLineQuickDebug( "Blocking victory due to " + faction.GetDisplayName() + " (" + faction.Type + ", " + faction.SpecialFactionData.DefeatCondition + ")" );
                    return faction;
                }
            }

            if ( AIWar2GalaxySettingQuickAccess.HomeworldsAreSafe )
            {
                foreach ( Faction faction in World_AIW2.Instance.AllPlayerFactions )
                {
                    GameEntity_Squad king = faction.GetFactionKing();
                    if ( king != null && !king.GetHasBeenDestroyed() && king.PlanetFaction.DataByStance[FactionStance.Hostile].TotalStrength > 0 )
                    {
                        if ( trace ) ArcenDebugging.SingleLineQuickDebug( "Blocking victory due to Homeworld " + king.Planet.Name + " not being safe" );
                        return faction;
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// Deploys a guard post's contents if it has any ships remaining. Returns true if deployment occurred,
        /// false if the post was already empty. Only counts non-empty posts against caller's rate limit.
        /// </summary>
        public bool TryDeployReinforcementContents( GameEntity_Squad entity, ArcenHostOnlySimContext Context )
        {
            var contents = entity.AIReinforcementPointContents;
            if ( contents == null )
                return false;
            for ( int i = 0; i < contents.Count; i++ )
            {
                var pair = contents[i];
                if ( pair != null && pair.RightItem > 0 )
                {
                    entity.DeployAIReinforcementContents( Context );
                    return true;
                }
            }
            return false;
        }
    }
}
