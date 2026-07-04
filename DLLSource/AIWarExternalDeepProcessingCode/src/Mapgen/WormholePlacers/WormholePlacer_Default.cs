using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Linq;
using System.Text;

namespace Arcen.AIW2.External
{
    public interface IWormholePlacer
    {
        ArcenPoint GetPointForWormhole( ArcenHostOnlySimContext Context, Planet ThisPlanet, Planet PlanetThatWormholeWillGoTo );
    }

    public class WormholePlacer_Default : IWormholePlacer
    {
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "WormholePlacer_Defaults" );
        public WormholePlacer_Default()
        {
            RefTracker.IncrementObjectCount();
        }

        public readonly FInt MinimumWormholeDistanceMultiplier;
        public readonly FInt MaximumWormholeDistanceMultiplier;

        public WormholePlacer_Default( FInt MinimumWormholeDistanceMultiplier, FInt MaximumWormholeDistanceMultiplier )
        {
            this.MinimumWormholeDistanceMultiplier = MinimumWormholeDistanceMultiplier;
            this.MaximumWormholeDistanceMultiplier = MaximumWormholeDistanceMultiplier;
        }

        public ArcenPoint GetPointForWormhole( ArcenHostOnlySimContext Context, Planet ThisPlanet, Planet PlanetThatWormholeWillGoTo )
        {
            int wormholeRadiusMin = ( ThisPlanet.GravWellSize.DistanceScale_GravwellRadius * this.MinimumWormholeDistanceMultiplier ).IntValue;
            int wormholeRadiusMax = (ThisPlanet.GravWellSize.DistanceScale_GravwellRadius * this.MaximumWormholeDistanceMultiplier).IntValue;
            int wormholeRadius = Context.RandomToUse.Next( wormholeRadiusMin, wormholeRadiusMin );
            
            AngleDegrees angleToNeighbor = ThisPlanet.GalaxyLocation.GetAngleToDegrees( PlanetThatWormholeWillGoTo.GalaxyLocation );
            ArcenPoint desiredWormholePoint = Engine_AIW2.Instance.CombatCenter.GetPointAtAngleAndDistance( angleToNeighbor, wormholeRadius );

            int largerIndex = Math.Max( ThisPlanet.Index, PlanetThatWormholeWillGoTo.Index );
            int smallerIndex = Math.Min( ThisPlanet.Index, PlanetThatWormholeWillGoTo.Index );
            int seed = ( largerIndex << 16 ) + smallerIndex;
            Context.RandomToUse.ReinitializeWithSeed( seed );

            //We can't just use this precise point because sometimes wormholes can essentially overlap
            ArcenPoint outputPoint = ThisPlanet.GetSafePlacementPoint_AroundDesiredPointVicinity( Context, GameEntityTypeDataTable.Instance.DefaultWormholeType, desiredWormholePoint, FInt.FromParts( 0, 005 ), FInt.FromParts( 0, 025 ) );

            return outputPoint;
        }
    }
}
