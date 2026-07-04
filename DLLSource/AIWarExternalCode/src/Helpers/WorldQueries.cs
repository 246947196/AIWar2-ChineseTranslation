using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Arcen.Universal;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Sys=System.Collections.Generic;

namespace Arcen.AIW2.External
{
    #region Types

    public interface ICandidates : IEnumerable
    {
        
    }
    public interface ICandidates<T> : Sys.IEnumerable<T>, ICandidates
    {
        
    }
    
    #region Candidates
    public struct Candidates : ICandidates<object>
    {
        public Sys.IEnumerable<Sys.IEnumerable<object>> _lists;
        public Sys.IEnumerable<object> _items;
        
        /// <summary>
        /// return true if the passed item should be included in the interated results
        /// </summary>
        public Func<object,bool> _filter;

        public Sys.IEnumerator<object> GetEnumerator()
        {
            if (_lists != null)
            {
                foreach (var list in _lists)
                {
                    if (list == null)
                        continue;
                    
                    foreach (var e in list)
                    {
                        if (e == null)
                            continue;
                        
                        if (_filter != null && !_filter(e))
                            continue;
                        
                        yield return e;
                    }
                }
            }
            else if (_items != null)
            {
                foreach (var e in _items)
                {
                    if (_filter != null && !_filter(e))
                        continue;
                    
                    yield return e;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
    #endregion
    
    #region Candidates<T>
    public struct Candidates<T> : ICandidates<T>
    {
        public Sys.IEnumerable<Sys.IEnumerable<T>> _lists;
        public Sys.IEnumerable<T> _items;
        
        /// <summary>
        /// return true if the passed item should be included in the interated results
        /// </summary>
        public Func<T,bool> _filter;

        public Sys.IEnumerator<T> GetEnumerator()
        {
            if (_lists != null)
            {
                foreach (var list in _lists)
                {
                    if (list == null)
                        continue;
                    
                    foreach (var e in list)
                    {
                        if (e == null)
                            continue;
                        
                        if (_filter != null && !_filter(e))
                            continue;
                        
                        yield return e;
                    }
                }
            }
            else if (_items != null)
            {
                foreach (var e in _items)
                {
                    if (e == null)
                        continue;
                    
                    if (_filter != null && !_filter(e))
                        continue;
                    
                    yield return e;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
    #endregion
    
    #region Rate (generic)
    public struct RateInfo<T>
    {
        // This does not need to be filled in by the Delegate
        // it will be filled in internally.
        public bool IsTie;
        
        // This does not need to be filled in by the Delegate
        // it will be filled in internally.
        public T Obj;

        // The tier. Higher tier values always picked before lower.
        public int Tier;

        // The rating. Higher rating are more likely to be picked.
        // One is the default and zero is counted as one.
        public uint Rating;
    }

    /// <summary>
    /// Return true if entity is a valid choice.
    /// Fill out the EntityRating.Rating to specify its desirability.
    /// (opt) Fill out the EntityRating.Tier to control groups which should be always picked before others.
    /// </summary>
    public delegate bool RateMethod<T>(T e, out RateInfo<T> r);
    
    #endregion
    
    #region Rate (typeless)
    public struct RateInfo
    {
        // This does not need to be filled in by the Delegate
        // it will be filled in internally.
        public bool IsTie;
        
        // This does not need to be filled in by the Delegate
        // it will be filled in internally.
        public object Obj;

        // The tier. Higher tier values always picked before lower.
        public int Tier;

        // The rating. Higher rating are more likely to be picked.
        // One is the default and zero is counted as one.
        public uint Rating;
    }

    /// <summary>
    /// Return true if entity is a valid choice.
    /// Fill out the EntityRating.Rating to specify its desirability.
    /// (opt) Fill out the EntityRating.Tier to control groups which should be always picked before others.
    /// </summary>
    public delegate bool RateMethod(object o, out RateInfo r);
    
    #endregion

    #endregion

    #region Queries
    public static partial class Extensions
    {
        #region Temporary ListOfLists<object>
        private static RapidAntiLeakPool<ListOfLists<RateInfo>> InnerPoolFor_ratingListList = RapidAntiLeakPool<ListOfLists<RateInfo>>.Create_WillNeverBeGCed(
            "InnerPoolFor_ratingListList", 10, KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads,
            delegate { return ListOfLists<RateInfo>.Create_WillNeverBeGCed( 1, 200, "TempRatingListList" ); } );

        private static ListOfLists<RateInfo> GetTempRatingListList( string Name, float SecondsAfterWhichToDeclareLeak )
        {
            ListOfLists<RateInfo> list = InnerPoolFor_ratingListList.GetFromPoolOrCreate( Name, SecondsAfterWhichToDeclareLeak );
            if ( list == null ) //pool returns null while blocked for teardown/shutdown; let callers bail
                return null;
            return list;
        }

        private static void ReleaseTempEntityRatingListList( ListOfLists<RateInfo> list )
        {
            list.Clear();
            InnerPoolFor_ratingListList.ReturnToPool( list );
        }
        #endregion

        //public static Candidates<Faction> FriendsOf(this List<Faction> collection, Faction faction)
        //{
        //    return new Candidates<Faction>()
        //    {
        //        _items = collection.Where((f)=>f.GetIsFriendlyTowards(faction)),
        //    };
        //}
        
        //public static Candidates<GameEntity_Squad> Squads(this Candidates<Faction> candidates)
        //{
        //    return new Candidates<GameEntity_Squad>()
        //    {
        //        _items = candidates.SelectMany((f)=>f..Where((f)=>f.GetIsFriendlyTowards(faction)),
        //    };
        //}
        
        public static Candidates<GameEntity_Squad> Squads(this EntityCollection collection)
        {
            return new Candidates<GameEntity_Squad>()
            {
                _items = collection.EntitiesOrNull_Squad,
            };
        }
        
        private static Sys.IEnumerable<GameEntity_Squad> _Squads(this Sys.IEnumerable<EntityCollection> collections)
        {
            foreach (var c in collections)
            {
                foreach (var s in c.EntitiesOrNull_Squad)
                {
                    yield return s;
                }
            }
        }
        
        public static Candidates<GameEntity_Squad> Squads(this Sys.IEnumerable<EntityCollection> collections)
        {
            return new Candidates<GameEntity_Squad>()
            {
                _items = collections._Squads(),
            };
        }
        
        public static Candidates<GameEntity_Squad> OfRollup(this EntityCollection collection, EntityRollupType rollup)
        {
            return new Candidates<GameEntity_Squad>()
            {
                _items = collection.GetListOfEntitiesByRollupOrNull(rollup),
            };
        }
        
        public static Candidates<GameEntity_Squad> OfType(this EntityCollection collection, GameEntityTypeData type)
        {
            return new Candidates<GameEntity_Squad>()
            {
                _items = collection.GetListOfEntitiesByEntityTypeOrNull(type),
            };
        }
        
        private static Sys.IEnumerable<Sys.IEnumerable<GameEntity_Squad>> _OfRollup(Sys.IEnumerable<EntityCollection> items, EntityRollupType rollup )
        {
            foreach ( var itr in items )
            { 
                var list = itr.GetListOfEntitiesByRollupOrNull(rollup);
                if (list != null)
                    yield return list;
            }
        }
        
        public static Candidates<GameEntity_Squad> OfRollup(this Sys.IEnumerable<EntityCollection> items, EntityRollupType rollup)
        {
            var res = new Candidates<GameEntity_Squad>();
            res._lists = _OfRollup(items, rollup);
            return res;
        }

        //private static bool _InRadius(GameEntity_Squad squad)
        //{
        //    return squad.WorldLocation.GetSquareDistanceTo(pos) < radiusSqr;
        //}
        
        public static Candidates<GameEntity_Squad> InRadius(this Sys.IEnumerable<GameEntity_Squad> items, ArcenPoint pos, int radius)
        {
            Vector2 posV = pos.ToVector2();
            float radiusF = radius;
            var res = new Candidates<GameEntity_Squad>();
            res._items = items;
            res._filter = (GameEntity_Squad e) =>
            {
                if ( e == null )
                    return false;
                Vector2 epos = e.WorldLocation.ToVector2();
                float eradius = (float)e.GetRadius();
                float distSqr = (epos - posV).sqrMagnitude;
                float maxSqr = (eradius + radiusF);
                maxSqr *= maxSqr;
                return distSqr < maxSqr;
            };
            return res;
        }
        
        /*
        public static RateInfo? GetBest( this Candidates<GameEntity_Squad> candidates, RateMethod rate, ArcenCharacterBuffer tracingBuffer = null )
        {
            int debugCode = 0;
            var ratings = GetTempRatingListList("GetBest.ratings", 5);
            try
            {
                ratings.Clear();

                foreach (var e in candidates)
                {
                    RateInfo r;
                    bool valid = rate( e, out r );
                    if ( valid && r.Rating <= 0 )
                        r.Rating = 1;

                    if ( tracingBuffer != null )
                    {
                        tracingBuffer.Add( string.Format( "Entity '{0}' is t={1} r={2} : {3}\n", e.ToString(), r.Tier, r.Rating, valid ? "VALID" : "" ) );
                    }

                    if ( valid )
                    {
                        List<RateInfo> list;
                        while ( ratings.OuterListCount <= r.Tier )
                            ratings.AddInnerList();

                        list = ratings[r.Tier];

                        r.Obj = e;

                        list.Add(r);
                    }
                }

                debugCode = 100;

                for ( int i = ratings.OuterListCount - 1; i >= 0; i-- )
                {
                    var list = ratings[i];
                    if (list.Count == 0)
                        continue;

                    list.StableSort(static (a,b)=>b.Rating.CompareTo(a.Rating));
                    
                    var last = list[list.Count - 1];
                    var first = list[0];
                    
                    if (tracingBuffer != null)
                        tracingBuffer.Add(string.Format("tier '{0}' has '{1}' items rated {2}-{3}\n", list[0].Tier, list.Count, last.Rating, first.Rating));

                    first.IsTie = false;
                    if (list.Count > 1)
                    {
                        var second = list[1];

                        if (second.Rating == first.Rating)
                        {
                            if (tracingBuffer != null)
                                tracingBuffer.Add("#1 and #2 tied\n");
                            
                            first.IsTie = true;
                        }
                    }
                    
                    return first;
                }

                // guess they rejected literally everything
                return null;
            }
            catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine( string.Format("debugCode '{0}\n{1}", debugCode, e), Verbosity.DoNotShow );
            }
            finally
            {
                ReleaseTempEntityRatingListList(ratings);
            }

            return null;
        }
        */
        
        public static RateInfo? GetBest( this ICandidates candidates, RateMethod rate, ArcenCharacterBuffer tracingBuffer = null )
        {
            int debugCode = 0;
            var ratings = GetTempRatingListList("GetBest.ratings", 5);
            try
            {
                ratings.Clear();

                foreach (var e in candidates)
                {
                    RateInfo r;
                    bool valid = rate( e, out r );
                    if ( valid && r.Rating <= 0 )
                        r.Rating = 1;

                    if ( tracingBuffer != null )
                    {
                        tracingBuffer.Add( string.Format( "Entity '{0}' is t={1} r={2} : {3}\n", e.ToString(), r.Tier, r.Rating, valid ? "VALID" : "" ) );
                    }

                    if ( valid )
                    {
                        List<RateInfo> list;
                        while ( ratings.OuterListCount <= r.Tier )
                            ratings.AddInnerList();

                        list = ratings[r.Tier];

                        r.Obj = e;

                        list.Add(r);
                    }
                }

                debugCode = 100;

                for ( int i = ratings.OuterListCount - 1; i >= 0; i-- )
                {
                    var list = ratings[i];
                    if (list.Count == 0)
                        continue;

                    list.StableSort(static (a,b)=>b.Rating.CompareTo(a.Rating));
                    
                    var last = list[list.Count - 1];
                    var first = list[0];
                    
                    if (tracingBuffer != null)
                        tracingBuffer.Add(string.Format("tier '{0}' has '{1}' items rated {2}-{3}\n", list[0].Tier, list.Count, last.Rating, first.Rating));

                    first.IsTie = false;
                    if (list.Count > 1)
                    {
                        var second = list[1];

                        if (second.Rating == first.Rating)
                        {
                            if (tracingBuffer != null)
                                tracingBuffer.Add("#1 and #2 tied\n");
                            
                            first.IsTie = true;
                        }
                    }
                    
                    return first;
                }

                // guess they rejected literally everything
                return null;
            }
            catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine( string.Format("debugCode '{0}\n{1}", debugCode, e), Verbosity.DoNotShow );
            }
            finally
            {
                ReleaseTempEntityRatingListList(ratings);
            }

            return null;
        }
        
        [ThreadStatic]
        private static Sys.List<GameEntity_Squad> _temp = new Sys.List<GameEntity_Squad>();
        
        private static Sys.List<GameEntity_Squad> TempList
        {
            get
            {
                if (_temp == null)
                    _temp = new Sys.List<GameEntity_Squad>();
                else
                    _temp.Clear();
                
                return _temp;
            }
        }
        
        [ThreadStatic]
        private static Sys.List<object> _tempObj = new Sys.List<object>();
        
        private static Sys.List<object> TempObjList
        {
            get
            {
                if (_tempObj == null)
                    _tempObj = new Sys.List<object>();
                else
                    _tempObj.Clear();
                
                return _tempObj;
            }
        }
        
        public static GameEntity_Squad GetRandom( this Candidates<GameEntity_Squad> items, RandomGenerator random)
        {
            var temp = TempList;
            temp.AddRange(items);
            if (temp.Count == 0)
                return null;
            
            return temp[random.Next(temp.Count)];
        }
        
        public static object GetRandom<T>( this Sys.IEnumerable<T> items, RandomGenerator random, RateMethod rate, ArcenCharacterBuffer trace = null )
        {
            var ratings = GetTempRatingListList("IEnumerable<object>.GetRandom.ratings", 10.0f );
            int debugCode = 0;
            try
            {
                ratings.Clear();
                
                trace?.Add( string.Format( "GetRandom() called with {0} candidates.\n", items.Count()));
                        
                if (items != null)
                {
                    foreach (var itr in items)
                    {
                        RateInfo r;
                        bool valid = rate( itr, out r );
                        if ( valid && r.Rating <= 0 )
                            r.Rating = 1;
                        
                        trace?.Add( string.Format( "Item '{0}' is t={1} r={2} {3}\n", itr.OrNull(), r.Tier, r.Rating, valid ? ": VALID" : "" ) );

                        if ( valid )
                        {
                            List<RateInfo> list;
                            while ( ratings.OuterListCount <= r.Tier )
                                ratings.AddInnerList();

                            list = ratings[r.Tier];

                            uint sum = 0;
                            if (list.Count > 0)
                                sum = list[list.Count-1].Rating;

                            r.Obj = itr;
                            r.Rating += sum;

                            list.Add(r);
                        }
                    }
                }

                debugCode = 100;

                for ( int i = ratings.OuterListCount - 1; i >= 0; i-- )
                {
                    var list = ratings[i];
                    if (list.Count == 0)
                        continue;

                    var last = list[list.Count - 1];
                    trace?.Add(string.Format("Sum of ratings in tier '{0}' is '{1}'\n", last.Tier, last.Rating));

                    uint n = random.NextUint( last.Rating );
                    
                    trace?.Add(string.Format("random rolled '{0}'\n", n));

                    for ( int j = 0; j < list.Count; j++ )
                    {
                        var r = list[j];
                        if ( n <= r.Rating )
                        {
                            trace?.Add(string.Format("returning {0} (of {1} choices)\n", r.Obj.OrNull(), list.Count));
                            return r.Obj;
                        }
                    }
                    
                    trace?.Add("error no items in tier to return\n");
                }
                
                trace?.Add("No valid choices to pick between, returning null.");
                
                // guess they rejected literally every planet
                return null;
            }
            catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine( string.Format("debugCode '{0}\n{1}", debugCode, e), Verbosity.DoNotShow );
            }
            finally
            {
                ReleaseTempEntityRatingListList(ratings);
            }

            return null;
        }
        
        public static T GetFirst<T>( this ICandidates<T> candidate )
        {
            return candidate.FirstOrDefault();
        }
        
        public static Sys.IEnumerable<EntityCollection> EntityCollections( this Planet planet )
        {
            if (planet != null)
            {
                for ( int i = 0; i < planet.Factions.Count; i++ )
                {
                    var fac = planet.Factions[i];
                    if (fac != null)
                        yield return fac.Entities;
                }
            }
        }
        
        public static Sys.IEnumerable<GameEntity_Squad> Squads( this Planet planet )
        {
            foreach ( var c in planet.EntityCollections() )
            { 
                if (c == null)
                    continue;
                
                if (c.EntitiesOrNull_Squad == null)
                    continue;
                
                foreach (var e in c.EntitiesOrNull_Squad)
                {
                    if (e == null)
                        continue;
                    
                    yield return e;
                }
            }
        }
    }
    
    #endregion
}
