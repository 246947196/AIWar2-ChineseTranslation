using Arcen.Universal;
using System;

using System.Text;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// Does not need to be pooled, etc.  This is ONLY ever used for the local player faction, and for visual pathfinding on the map.
    /// This isn't something that should ever be used by some other faction, or for anything but the UI.  This thing is heavy as heck!
    /// </summary>
    public class ProjectedLocalPlayerMultiPathData
    {
        private readonly ArcenTwoDimensionalFlexibleLists<bool> internalList;

        public static int TotalocalPlayerMultiPathDatasEver = 1;

        private static ReferenceTracker RefTracker;
        public ProjectedLocalPlayerMultiPathData()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "ProjectedLocalPlayerMultiPathData" );
            RefTracker.IncrementObjectCount();

            System.Threading.Interlocked.Add( ref TotalocalPlayerMultiPathDatasEver, 1 );

            //Chris says: any more than 300 planets will blow up now, but this is more efficient as a way to initialize and reuse
            this.internalList = ArcenTwoDimensionalFlexibleLists<bool>.Create_WillNeverBeGCed( false, 300, 300, "Squad-ProjectedLocalPlayerMultiPathData" ); // World_AIW2.Instance.CurrentGalaxy.GetCountOfTotalPlanetsDestroyedAndOtherwise() );
        }

        public void Reset()
        {
            this.internalList.ClearTo( false );
        }

        public void AddConnection( Planet Left, Planet Right )
        {
            if ( Left == null || Right == null )
                return;

            if ( Left.Index >= this.internalList.Height )
                this.internalList.EnsureThereAreEnoughEntriesToFindIndex( Left.Index );
            if ( Right.Index >= this.internalList.Height )
                this.internalList.EnsureThereAreEnoughEntriesToFindIndex( Right.Index );
            this.internalList.Set( Left.Index, Right.Index, true );
            this.internalList.Set( Right.Index, Left.Index, true );
        }

        public bool GetAreConnected( Planet Left, Planet Right )
        {
            if ( Left == null || Right == null )
                return false;

            if ( Left.Index >= this.internalList.Height )
                this.internalList.EnsureThereAreEnoughEntriesToFindIndex( Left.Index );
            if ( Right.Index >= this.internalList.Height )
                this.internalList.EnsureThereAreEnoughEntriesToFindIndex( Right.Index );

            return this.internalList.Get( Left.Index, Right.Index );
        }
    }

    public static class SquadExtensions
    {
        public static Planet GetDestinationPlanet_LocalPlayerOnly( this GameEntity_Squad Me, ProjectedLocalPlayerMultiPathData LocalPlayerLookup )
        {
            //Sometimes we need to know where a ship is going to wind up after it executes all its wormhole moves
            //This function returns that final destination (or the ship's current planet, if there are no wormhole commands).

            //If the ProjectedMultiPathData is passed in then fill that structure in, otherwise ignore it;
            //that structure is used for the UI code to display the intended path for ships
            Planet finalDestinationPlanet = Me.Planet;
            if ( Me.Orders != null && Me.Orders.GetQueuedOrderCount() > 0 )
            {
                Planet newPlanet = null;
                for ( int i = 0; i < Me.Orders.GetQueuedOrderCount(); i++ )
                {
                    EntityOrder order = Me.Orders.GetQueuedOrderAtIndex_OrNull( i );
                    if ( order.TypeData != null && order.TypeData.Type == EntityOrderType.Wormhole )
                    {
                        //if we have orders already, make sure those orders are reflected in the lookup
                        newPlanet = World_AIW2.Instance.GetPlanetByIndex( order.RelatedPlanetIndex );
                        if ( LocalPlayerLookup != null )
                            LocalPlayerLookup.AddConnection( finalDestinationPlanet, newPlanet );
                        finalDestinationPlanet = newPlanet;
                    }
                }
            }

            return finalDestinationPlanet;
        }
    }
}
