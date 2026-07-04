using System;
using Arcen.Universal;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public static class ShipSpawning
    {
        public static void Spawn(
            ArcenHostOnlySimContext context, 
            Planet onPlanet, 
            Faction forFaction, 
            Fleet inFleet,
            ArcenPoint atLocation,
            Dictionary<GameEntityTypeData,int> typeCounts,
            string debugtext = "ShipSpawning.Spawn",
            List<GameEntity_Squad> optSpawnedShips = null)
        {
            var planetfac = onPlanet.GetPlanetFactionForFaction(forFaction);
            int maxStacks;
            if ( planetfac.Faction.Type == FactionType.Player )
                maxStacks = AIWar2GalaxySettingQuickAccess.StackingCutoffPlayers;
            else
                maxStacks = AIWar2GalaxySettingQuickAccess.StackingCutoffNPCs;
            
            void SpawnShip_Stacked(GameEntityTypeData type, int count)
            {
                int debugstage = 0;
                try
                {
                    debugstage = 1000;

                    int stacksRemaining = count;
                    int countOfEntities = Math.Max( Math.Min( maxStacks - planetfac.Entities.GetCountFromListOfEntitiesByEntityType( type ), stacksRemaining ), 1 );

                    GameEntity_Squad squad;
                    int currentStacks;

                    debugstage = 2000;

                    for ( int i = 0; i < countOfEntities; i++ )
                    {
                        if ( type.CannotBeStacked )
                        {
                            currentStacks = 1;
                        } 
                        else
                        {
                            currentStacks = stacksRemaining / (countOfEntities - i);
                        }

                        stacksRemaining -= currentStacks;
                        
                        debugstage = 3000;

                        var loc = planetfac.Planet.GetSafePlacementPoint(context, type, atLocation, 200, 2000, null);

                        squad = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( planetfac, type, forFaction.CurrentGeneralMarkLevel, planetfac.FleetUsedAtPlanet, 0, loc, context, debugtext );
                        if (squad != null)
                        {
                            if (optSpawnedShips != null)
                                optSpawnedShips.Add(squad);
                            squad.AddOrSetExtraStackedSquadsInThis( (short) (currentStacks - 1), true );
                            
                            // debatable if this is appropriate or not
                            squad.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                        }

                        debugstage = 4000;
                    }
                } 
                catch ( Exception e )
                {
                    throw new Exception( "Error in ShipSpawning.Spawn() at debugstage " + debugstage + ": " + e.Message );
                }
            }

            foreach ( KeyValuePair<GameEntityTypeData, int> __tc in typeCounts )
            {
                GameEntityTypeData type = __tc.Key;
                int count = __tc.Value;
                SpawnShip_Stacked(type, count);
            }
        }
    }
}
