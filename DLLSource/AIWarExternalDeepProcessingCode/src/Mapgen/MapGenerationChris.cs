using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class Mapgen_CrazySymmetric : IMapGenerator
    {
        public Mapgen_CrazySymmetric()
        {
        }

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {

        }

        private int numPlanetTotalToHave;
        private FInt planetSpacingFalloff;
        private int minPlanetsPerBranch;
        private int maxPlanetsPerBranch;
        private int forceSinglePlanetEvery;
        public void GenerateMapStructureOnly( Galaxy galaxy, ArcenHostOnlySimContext Context, MapConfiguration mapConfig, MapTypeData mapType )
        {
            if ( MapgenLogger.IsActive )
                MapgenLogger.Log( "GenerateMapStructureOnly: Mapgen_CrazySymmetric : " + galaxy.GetTotalPlanetCount() + " planets at start." );

            int numberOfGroups = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "NumberOfGroups" ).RelatedIntValue;
            FInt startingSpacing = FInt.CreateFromDoubleNonSim(BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "StartingSpacing" ).RelatedIntValue / 100.0f);
            planetSpacingFalloff = FInt.CreateFromDoubleNonSim(BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "PlanetSpacingFalloff" ).RelatedIntValue / 100.0f);
            minPlanetsPerBranch = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "MinPlanetsPerBranch" ).RelatedIntValue;
            maxPlanetsPerBranch = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "MaxPlanetsPerBranch" ).RelatedIntValue;
            forceSinglePlanetEvery = BadgerUtilityMethods.getSettingValueMapSettingOptionChoice_Expensive( mapConfig, "ForceSinglePlanetEvery" ).RelatedIntValue;

            AngleDegrees startingAngleOffset = AngleDegrees.Create( (float)(Context.RandomToUse.Next( -365, 365 )) );
            FInt distanceToChildren;
            int numPlanets = mapConfig.GetClampedNumberOfPlanetsForMapType( mapType );
            numPlanetTotalToHave = numPlanets;
            if ( numPlanets < 80 )
                distanceToChildren = FInt.FromParts( 400, 000 );
            else if ( numPlanets < 120 )
                distanceToChildren = FInt.FromParts( 500, 000 );
            else if ( numPlanets > 150 )
                distanceToChildren = FInt.FromParts( 550, 000 );
            else if ( numPlanets > 250 )
                distanceToChildren = FInt.FromParts( 600, 000 );
            else
                distanceToChildren = FInt.FromParts( 700, 000 );

            distanceToChildren *= startingSpacing;

            recursionDepth = 0;
            firstExtraInnerPlanet = null;
            priorExtraInnerPlanet = null;
            this.InnerGenerate( galaxy, Context, numPlanets, mapType, Engine_AIW2.Instance.GalaxyMapOnly_GalaxyCenter, (FInt)distanceToChildren, startingAngleOffset, null,
                numberOfGroups );

            if ( firstExtraInnerPlanet != null && priorExtraInnerPlanet != firstExtraInnerPlanet )
                firstExtraInnerPlanet.AddLinkTo( priorExtraInnerPlanet );

            firstExtraInnerPlanet = null;
            priorExtraInnerPlanet = null;

            BadgerUtilityMethods.makeSureGalaxyIsFullyConnected( true, galaxy );
        }

        private Planet priorExtraInnerPlanet = null;
        private Planet firstExtraInnerPlanet = null;

        static int recursionDepth = 0;
        private void InnerGenerate( Galaxy galaxy, ArcenHostOnlySimContext Context, int sizeOfSubTree, MapTypeData mapType, ArcenPoint CenterOfSubTree, 
            FInt distanceToChildren, AngleDegrees startingAngleOffset, Planet Parent, int NumberOfChildren )
        {
            PlanetType planetType = PlanetType.Normal;
            int planetsToPlace = sizeOfSubTree;

            ArcenPoint safePoint;
            int preferredDistanceFromPoint = 10;
            while ( !BadgerUtilityMethods.GetExtremelySafePointNearPoint( null, CenterOfSubTree, galaxy, 40, 10, Parent, true, 20, Context, out safePoint ) )
            {
                preferredDistanceFromPoint += 30;
                if ( preferredDistanceFromPoint > 300 )
                    return;
            }
            CenterOfSubTree = safePoint;

            if ( galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() >= numPlanetTotalToHave )
                return;
            if ( Parent != null && CenterOfSubTree.GetDistanceTo( Parent.GalaxyLocation, true ) > 1000 )
                return; //this map type sometimes gives results that are like 50,000 away from the rest.  Stop that!

            Planet center = galaxy.AddPlanet( planetType, CenterOfSubTree,
                    World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
            planetsToPlace--;

            if ( Parent != null )
            {
                Parent.AddLinkTo( center );
                //ArcenDebugging.ArcenDebugLogSingleLine( "center P Distance from parent P: " + center.GalaxyLocation.GetDistanceTo( Parent.GalaxyLocation, true ) +
                //    "   " + galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise(), Verbosity.DoNotShow );
            }

            if ( planetsToPlace <= 0 )
                return;

            AngleDegrees startingAngleOffsetForChildren = startingAngleOffset.Add( AngleDegrees.Create( (float)Context.RandomToUse.Next( 30, 60 ) ) );
            FInt distanceToChildrenForChildren = distanceToChildren / planetSpacingFalloff;

            int defaultNumChildren = NumberOfChildren;
            bool shouldHaveMoreThanOneChild = false;
            if ( recursionDepth >= 1 )
            {
                //do crazy linkages after the first level
                if ( minPlanetsPerBranch >= maxPlanetsPerBranch )
                    defaultNumChildren = minPlanetsPerBranch;
                else
                    defaultNumChildren = Context.RandomToUse.Next( minPlanetsPerBranch, maxPlanetsPerBranch );
                shouldHaveMoreThanOneChild = true;
            }
            if ( forceSinglePlanetEvery != 0 && recursionDepth % forceSinglePlanetEvery == 0 && recursionDepth >= 2 )
                defaultNumChildren = 1;

            int numberOfChildren = Math.Min( defaultNumChildren, planetsToPlace );
            AngleDegrees degreesBetweenChildren = AngleDegrees.Create( ((float)360) / numberOfChildren );
            int nodesPerChildTree = planetsToPlace / numberOfChildren;
            int extraNodesForChildTrees = planetsToPlace % numberOfChildren;
            AngleDegrees angleToNextChild = startingAngleOffset;
            if ( numberOfChildren == 3 )
                angleToNextChild -= 25;

            //if only one child but would normally have more than one, than do this
            if ( shouldHaveMoreThanOneChild && numberOfChildren == 1 )
                angleToNextChild = AngleDegrees.Create( (float)Context.RandomToUse.Next( 30, 60 ) );

            for ( int i = 0; i < numberOfChildren; i++ )
            {
                Planet extra = null;
                ArcenPoint childPoint;

                //put a bit of visual wobble into the longer lines for aesthetics
                if ( recursionDepth < 1 )
                    childPoint = CenterOfSubTree.GetPointAtAngleAndDistance( angleToNextChild + Context.RandomToUse.Next( -10, 10 ), distanceToChildren.IntValue );
                else
                    childPoint = CenterOfSubTree.GetPointAtAngleAndDistance( angleToNextChild, distanceToChildren.IntValue );

                if ( recursionDepth < 1 )
                {
                    if ( galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() >= numPlanetTotalToHave )
                        return;
                    //Add some extra hops to the inner planets
                    extra = galaxy.AddPlanet( planetType, childPoint,
                        World_AIW2.Instance.GetPlanetGravWellSizeForPlanetType( Context.RandomToUse, PlanetPopulationType.None ) );
                    extra.AddLinkTo( center );
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Extra P Distance from center P: " + center.GalaxyLocation.GetDistanceTo( extra.GalaxyLocation, true ) +
                    //    "   " + galaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise(), Verbosity.DoNotShow );
                    childPoint = CenterOfSubTree.GetPointAtAngleAndDistance( angleToNextChild, distanceToChildren.IntValue * 2 );
                    planetsToPlace--;

                    if ( recursionDepth == 0 )
                    {
                        if ( priorExtraInnerPlanet != null )
                            priorExtraInnerPlanet.AddLinkTo( extra );
                        priorExtraInnerPlanet = extra;
                        if ( firstExtraInnerPlanet == null )
                            firstExtraInnerPlanet = extra;
                    }
                }

                int nodesForThisSubTree = nodesPerChildTree;
                if ( extraNodesForChildTrees > 0 )
                {
                    nodesForThisSubTree++;
                    extraNodesForChildTrees--;
                }
                recursionDepth++;
                Planet planetToPass = center;
                if ( extra != null )
                    planetToPass = extra; //this is to make sure the extra planet we've added in will be linked correctly
                this.InnerGenerate( galaxy, Context, nodesForThisSubTree, mapType, childPoint, distanceToChildrenForChildren, startingAngleOffsetForChildren, planetToPass, NumberOfChildren );
                angleToNextChild += degreesBetweenChildren;
                recursionDepth--;
            }
        }
    }
}
