using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class TempCollectionHelper
    {
        ///TEACHING_MOMENT: What's up with these temp collections, and how can you have this sort of thing for yourself?  And why?
        ///This game uses collections that are not free to be thrown to the garbage collector.  Once you instantiate one, it keeps track of it forever.
        ///If you keep on declaring Lists and Dictionaries and so forth and then just letting them fall out of scope, the GC will NOT clean up after you.
        ///It will be a memory leak, in other words.
        ///--------------------------------------------------------------
        ///Why?
        ///Lots of reasons, but one is to be able to do a full memory dump of the game, and thus detect other kinds of memory leaks (like adding to a list repeatedly without clearing).
        ///Overall, the general wisdom for Unity is to do pooling as much as possible in the first place, and not rely overly on their substandard garbage collector.
        ///That's where these "temp collections" come in.
        ///These all use RapidAntiLeakPool, from which you can check out a list from any thread.  
        ///You then specify the name of your checkout (that will be what pops up if you forget to clean up properly after yourself),
        ///and you also specify a number of seconds after which it will complain that there's a memory leak.  Usually 5-10 seconds is fine.
        ///Too fast and you might get some false reports on slow computers running long-running processes.  Too long (30-60 seconds) and it's harder to test.
        ///After you have checked out a collection for use on a thread, then must put it back after you are done.
        ///--------------------------------------------------------------
        ///Why not use ThreadStatic?
        ///ThreadStatic was something that we used a lot in earlier versions of the game, and I was extolling the virtues of that language feature.
        ///It really is great!  But it only works well when you have strictly managed threads, and each one uses the same data structures repeatedly.
        ///We don't do that anymore, as the newer and better way of multithreading is to use the Task framework, which uses hundreds or thousands of threads.
        ///The consequence of the task framework is that, when combined with ThreadStatic, it means there are tons of orphaned collections all over RAM.
        ///It's not a memory leak per se... but it kind of is.
        ///The end effect is that, over time, players will see an unexplained rise in RAM of 0.5 to 3 GB, depending on which and how many factions are in use.
        ///Since you won't see this in brief testing periods of 5-10 minutes, it's one of those things that really only strikes end users.
        ///--------------------------------------------------------------
        ///What if your temp-collection combo isn't here?
        ///There are a number of this sort of things on ArcenStrings, Mat, GameEntityTypeData, Planet, Faction, GameEntity_Squad, and others, to start with.
        ///These temp-collections are defined externally mostly because they are more esoteric in nature.
        ///You can easily define your own temp collection helper method for your code, or add a temp-collection to an existing class (see AIBudget).
        ///Your temp-collections will work just as well as those set up here.

        #region Temporary DZResourceIntDicts
        private static RapidAntiLeakPool<Dictionary<DZResource, int>> InnerPoolFor_DZResourceIntDict = RapidAntiLeakPool<Dictionary<DZResource, int>>.Create_WillNeverBeGCed(
            "PoolFor_TempDZResourceIntDict", 10000, KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads,
            delegate { return Dictionary<DZResource, int>.Create_WillNeverBeGCed( 30, "TempDZResourceIntDict" ); } );

        public static Dictionary<DZResource, int> GetTemporaryDZResourceIntDict( string Name, float SecondsAfterWhichToDeclareLeak )
        {
            Dictionary<DZResource, int> dict = InnerPoolFor_DZResourceIntDict.GetFromPoolOrCreate( Name, SecondsAfterWhichToDeclareLeak );
            if ( dict == null ) //pool returns null while blocked for teardown/shutdown; let callers bail
                return null;
            dict.Clear();
            return dict;
        }

        public static void ReleaseTemporaryDZResourceIntDict( Dictionary<DZResource, int> dict )
        {
            InnerPoolFor_DZResourceIntDict.ReturnToPool( dict );
        }
        #endregion

        #region Temporary HackingTypeFIntDicts
        private static RapidAntiLeakPool<Dictionary<HackingType, FInt>> InnerPoolFor_HackingTypeFIntDict = RapidAntiLeakPool<Dictionary<HackingType, FInt>>.Create_WillNeverBeGCed(
            "PoolFor_TempHackingTypeFIntDict", 10000, KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads,
            delegate { return Dictionary<HackingType, FInt>.Create_WillNeverBeGCed( 30, "TempHackingTypeFIntDict" ); } );

        public static Dictionary<HackingType, FInt> GetTemporaryHackingTypeFIntDict( string Name, float SecondsAfterWhichToDeclareLeak )
        {
            Dictionary<HackingType, FInt> dict = InnerPoolFor_HackingTypeFIntDict.GetFromPoolOrCreate( Name, SecondsAfterWhichToDeclareLeak );
            if ( dict == null ) //pool returns null while blocked for teardown/shutdown; let callers bail
                return null;
            dict.Clear();
            return dict;
        }

        public static void ReleaseTemporaryHackingTypeFIntDict( Dictionary<HackingType, FInt> dict )
        {
            InnerPoolFor_HackingTypeFIntDict.ReturnToPool( dict );
        }
        #endregion

        #region Temporary KVPairListPlanetWaveCompToPlanets
        private static RapidAntiLeakPool<List<KeyValuePair<PlanetWaveComposition, Planet>>> InnerPoolFor_KVPairListPlanetWaveCompToPlanet = RapidAntiLeakPool<List<KeyValuePair<PlanetWaveComposition, Planet>>>.Create_WillNeverBeGCed(
            "PoolFor_TempKVPairListPlanetWaveCompToPlanet", 10000, KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads,
            delegate { return List<KeyValuePair<PlanetWaveComposition, Planet>>.Create_WillNeverBeGCed( 30, "TempKVPairListPlanetWaveCompToPlanet" ); } );

        public static List<KeyValuePair<PlanetWaveComposition, Planet>> GetTemporaryKVPairListPlanetWaveCompToPlanet( string Name, float SecondsAfterWhichToDeclareLeak )
        {
            List<KeyValuePair<PlanetWaveComposition, Planet>> dict = InnerPoolFor_KVPairListPlanetWaveCompToPlanet.GetFromPoolOrCreate( Name, SecondsAfterWhichToDeclareLeak );
            if ( dict == null ) //pool returns null while blocked for teardown/shutdown; let callers bail
                return null;
            dict.Clear( true );
            return dict;
        }

        public static void ReleaseTemporaryKVPairListPlanetWaveCompToPlanet( List<KeyValuePair<PlanetWaveComposition, Planet>> dict )
        {
            InnerPoolFor_KVPairListPlanetWaveCompToPlanet.ReturnToPool( dict );
        }
        #endregion

        #region Temporary ListOfPlanetCompositions
        private static RapidAntiLeakPool<List<PlanetWaveComposition>> InnerPoolFor_ListOfPlanetComposition = RapidAntiLeakPool<List<PlanetWaveComposition>>.Create_WillNeverBeGCed(
            "PoolFor_TempListOfPlanetComposition", 10000, KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads,
            delegate { return List<PlanetWaveComposition>.Create_WillNeverBeGCed( 30, "TempListOfPlanetComposition" ); } );

        public static List<PlanetWaveComposition> GetTemporaryListOfPlanetComposition( string Name, float SecondsAfterWhichToDeclareLeak )
        {
            List<PlanetWaveComposition> dict = InnerPoolFor_ListOfPlanetComposition.GetFromPoolOrCreate( Name, SecondsAfterWhichToDeclareLeak );
            if ( dict == null ) //pool returns null while blocked for teardown/shutdown; let callers bail
                return null;
            dict.Clear( true );
            return dict;
        }

        public static void ReleaseTemporaryListOfPlanetComposition( List<PlanetWaveComposition> dict )
        {
            InnerPoolFor_ListOfPlanetComposition.ReturnToPool( dict );
        }
        #endregion
    }
}