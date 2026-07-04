using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class DarkSpireSvikariFactionDeepInfo : DarkSpireFactionDeepInfo
    {
        protected override GameEntityTypeData GetNewVGEntityData( ArcenHostOnlySimContext Context )
        {
            return GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, "VengeanceGeneratorSvikariSpawn" );
        }

        public override void SeedStartingEntities_EarlyMajorFactionClaimsOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapTypeData mapType )
        {
            // No mapgen seeding; VG spawns via SpawnInitialVGIfNecessary at game second 3
        }

        private DarkSpireSvikariFactionBaseInfo SvikariBaseInfo => BaseInfo as DarkSpireSvikariFactionBaseInfo;

        private int lastVGSpawnAttemptSecond = -1;
        private const int VGRespawnIntervalSeconds = 1200; // 20 minutes

        protected override void Cleanup()
        {
            base.Cleanup();
            lastVGSpawnAttemptSecond = -1;
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            base.DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly( Context );

            if ( BaseInfo == null )
                return;
            if ( BaseInfo.DarkSpirePlanets.Count == 0 )
            {
                int gameSecond = World_AIW2.Instance.GameSecond;
                bool isFirstAttempt = lastVGSpawnAttemptSecond < 0 && gameSecond >= 10;
                bool isRetry = lastVGSpawnAttemptSecond >= 0 && gameSecond >= lastVGSpawnAttemptSecond + VGRespawnIntervalSeconds;
                if ( isFirstAttempt || isRetry )
                    SpawnInitialVGIfNecessary( Context );
            }
        }

        public override void UpdatePlanetInfluence_HostOnly( ArcenHostOnlySimContext Context )
        {
            List<Planet> planetsInfluenced = Planet.GetTemporaryPlanetList( "DarkSpireSvikari-UpdatePlanetInfluence_HostOnly-planetsInfluenced", 10f );
            if ( planetsInfluenced == null )
                return;
            List<SafeSquadWrapper> vgs = BaseInfo.VGs.GetDisplayList();
            for ( int i = 0; i < vgs.Count; i++ )
                planetsInfluenced.AddIfNotAlreadyIn( vgs[i].Planet );
            AttachedFaction.SetInfluenceForPlanetsToList( planetsInfluenced );
            Planet.ReleaseTemporaryPlanetList( planetsInfluenced );
        }

        private void SpawnInitialVGIfNecessary( ArcenHostOnlySimContext Context )
        {
            lastVGSpawnAttemptSecond = World_AIW2.Instance.GameSecond;
            DarkSpireSvikariFactionBaseInfo svikari = SvikariBaseInfo;
            if ( svikari == null )
                return;

            GameEntityTypeData entityData = GetNewVGEntityData( Context );
            if ( entityData == null )
                return;
            Planet targetPlanet = null;

            if ( svikari.PlayerAllied || svikari.AIAllied )
            {
                foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                    if ( entity.GetIsFriendlyTowards_Safe( AttachedFaction ) )
                    {
                        targetPlanet = entity.Planet;
                        break;
                    }
                }
            }
            else // MinorFactionAllied
            {
                foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
                {
                    Faction influencingFaction = planet.GetControllingOrInfluencingFaction();
                    if ( !influencingFaction.GetIsFriendlyTowards( AttachedFaction ) )
                        continue;
                    PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
                    if ( pFaction.DataByStance[FactionStance.Hostile].TotalStrength < 5 * 1000 )
                    {
                        targetPlanet = planet;
                        break;
                    }
                }
            }
            if ( targetPlanet == null )
                return;
            ArcenPoint spawnLocation = targetPlanet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 200 ), FInt.FromParts( 0, 650 ) );
            PlanetFaction pFactionForSpawn = targetPlanet.GetPlanetFactionForFaction( AttachedFaction );
            GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFactionForSpawn, entityData, entityData.MarkFor( pFactionForSpawn ),
                pFactionForSpawn.FleetUsedAtPlanet, 0, spawnLocation, Context, "DarkSpireSvikariInitialVG" );
        }
    }
}
