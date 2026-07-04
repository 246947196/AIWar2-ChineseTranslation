using System;

using System.Linq;
using System.Text;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /*
      Some Telia are seeded on the map at the beginning. Each Telium starts with one Harvester that harvests metal.
      When enough metal gets harvested the Telium will either release a bunch of spores or make another Harvester.
      Spores are cloaked and travel at random around the map. When Spores from multiple Telia wind up on the same planet
      they are consumed to create a Telium.

      Spores have a limited lifetime.

      If a Telium builds enough Harvester then some of them will Enrage and go attack either a player or ai homeworld.
      If a Telium dies then all its harvesters enrage.

      Each Spore, Telium and Macrophage track a bunch of data.
    */

    // Base Macrophage Class
    public abstract class MacrophageFactionDeepInfoBase : ExternalFactionDeepInfoRoot
    {
        //TEACHING_MOMENT: I have sealed this property in order to avoid any classes that inherit from this from making a mistake
        //It's imperative that the normal, enraged, and tamed macrophage all share the same name, which will mean they never run
        //at the same time.  The CalculateGatheringPoints_NotThreadsafe() meethod is run from the LRP, which is great, but we cannot
        //have two kinds of macrophage running that at once.  They must take turns, and this ensures that they do.

        public MacrophageFactionBaseInfoCore BaseInfoCore;
        public sealed override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfoCore = this.AttachedFaction.GetExternalBaseInfoAs<MacrophageFactionBaseInfoCore>();
            this.SubDoAnyInitializationImmediatelyAfterFactionAssigned();
        }
        public abstract void SubDoAnyInitializationImmediatelyAfterFactionAssigned();

        //a list of gathering point planets for our Spores to path to, update whenever a Telium is created or destroyed
        protected bool GatheringPointsUpdated;
        protected readonly DoubleBufferedList<Planet> SporeGatheringPoints = DoubleBufferedList<Planet>.Create_WillNeverBeGCed( 300, "Macrophage-SporeGatheringPoints" );
        // Holds a list of all our spores using their planet as a key, used to simplify spore counting for the purpose of Telia spawning.

        // Whenever we load a save; we want to reset our above statics so we aren't carrying data we don't want between saves.
        protected sealed override void Cleanup()
        {
            BaseInfoCore = null;

            GatheringPointsUpdated = false;
            SporeGatheringPoints.Clear();
            workingTeliumPlanets.Clear();
            working_AllTeliaWithSporesOnPlanet.Clear();
            working_RegularTeliaWithSporesOnPlanet.Clear();

            this.SubCleanup();
        }
        protected abstract void SubCleanup();

        //This method can't be threadsafe, because it's updating a double-buffered list.
        //What do I mean by threadsafe?  It can't be called by two threads at the same time.
        //It can be called by any thread, but it should never be called from two different threads,
        //since they might overlap each other.
        //
        //Update: these used to be static, which was a lot more problematic.  Now they are instance methods and variables,
        //so actually they probably ARE threadsafe except when there are multiple factions of the exact same type.
        //Still best not to tempt fate, though.
        private readonly List<Planet> workingTeliumPlanets = List<Planet>.Create_WillNeverBeGCed( 500, "MacrophageFactionDeepInfoBase-workingTeliumPlanets" );
        public void CalculateGatheringPoints_NotThreadsafe( Faction faction, ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData )
        {
            workingTeliumPlanets.Clear();

            // Find any planet that is midway between our existing Telia.
            // Start by getting all of our planets with Telia into a list.
            // This list of factions includes enraged, tamed, normal, etc.
            for ( int x = 0; x < MacrophageFactionBaseInfoCore.AllMacrophageFactionsOfAnyTypes.Count; x++ )
            {
                MacrophageFactionBaseInfoCore info = MacrophageFactionBaseInfoCore.AllMacrophageFactionsOfAnyTypes[x];
                if ( info.Telia.Count == 0 )
                    continue;
                List<SafeSquadWrapper> teliaList = info.Telia.GetDisplayList();
                for ( int y = 0; y < teliaList.Count; y++ )
                {
                    GameEntity_Squad telia = teliaList[y].GetSquad();
                    if ( telia == null )
                        continue;
                    Planet workingPlanet = telia.Planet;
                    if ( !workingTeliumPlanets.Contains( workingPlanet ) )
                        workingTeliumPlanets.Add( workingPlanet );
                }
            }

            //prepare to build our list on this thread, but we're not disturbing the displaylist on any other threads
            SporeGatheringPoints.ClearConstructionListForStartingConstruction();

            // Finally, add every single midway point between our planets together (wow this is a bit expensive, but okay)
            for ( int y = 0; y < workingTeliumPlanets.Count; y++ )
            {
                Planet workingPlanet = workingTeliumPlanets[y];
                for ( int z = y + 1; z < workingTeliumPlanets.Count; z++ )
                {
                    Planet otherPlanet = workingTeliumPlanets[z];
                    PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache( faction, "CalculateGatheringPoints_NotThreadsafe", workingPlanet, otherPlanet, PathingMode.Default, Context, PathCacheData );
                    if ( pathCache != null && pathCache.PathToReadOnly.Count > 0 )
                    {
                        Planet midPlanet = pathCache.PathToReadOnly[pathCache.PathToReadOnly.Count / 2];
                        SporeGatheringPoints.AddToConstructionListIfNotAlreadyIn( midPlanet );
                    }
                }
            }

            //now we have our list, flip it from construction to display.  Any thread can use it now.
            //Any thread in the middle of using the old list will be able to finish using that until we next call this method, so no worries.
            //If you call this every sim frame, that means the other thread needs to finish using the list in 100ms.  If it's every second,
            //then it has between 200ms and 1 second to complete (5x speed means 200ms per second)
            SporeGatheringPoints.SwitchConstructionToDisplay();
        }

        private readonly List<SafeSquadWrapper> working_AllTeliaWithSporesOnPlanet = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 60, "MacrophageFactionDeepInfoBase-working_AllTeliaWithSporesOnPlanet" );
        private readonly List<SafeSquadWrapper> working_RegularTeliaWithSporesOnPlanet = List<SafeSquadWrapper>.Create_WillNeverBeGCed( 60, "MacrophageFactionDeepInfoBase-working_RegularTeliaWithSporesOnPlanet" );
        public void SpawnTeliumIfAble( ArcenHostOnlySimContext Context )
        {
            if ( Context == null )
                return; //client

            int debugStage = 0;
            try
            {
                debugStage = 100;
                if ( !MacrophageFactionBaseInfoCore.CrossFaction_SporesPerPlanet_CanRun )
                    return; //this prevents more than one faction from running this logic between Stage2 calls
                MacrophageFactionBaseInfoCore.CrossFaction_SporesPerPlanet_CanRun = false;

                debugStage = 500;
                Faction tamedFaction = MacrophageTamedFactionBaseInfo.Instance.AttachedFaction;

                debugStage = 1100;
                //loop over each planet, one by one, and see if they need any spawning to happen
                for ( Int16 planetIndex = 0; planetIndex < MacrophageFactionBaseInfoCore.CrossFaction_SporesPerPlanet.GetCountOfLists(); planetIndex++ )
                {
                    debugStage = 1200;
                    Planet planet = World_AIW2.Instance.GetPlanetByIndex( planetIndex );
                    if ( planet == null || planet.HasPlanetBeenDestroyed )
                        continue; //only look at planets that exist and are not destroyed

                    debugStage = 1400;
                    List<SafeSquadWrapper> existingSporesOnPlanet = MacrophageFactionBaseInfoCore.CrossFaction_SporesPerPlanet[planetIndex].GetDisplayList();
                    //Check for spores from multiple Telia on one planet, and if so then call SpawnTeliumOnPlanet
                    //and destroy all the spores on that planet
                    working_AllTeliaWithSporesOnPlanet.Clear();
                    working_RegularTeliaWithSporesOnPlanet.Clear();
                    bool tamedSporeOnPlanet = false;

                    debugStage = 2100;
                    for ( int j = 0; j < existingSporesOnPlanet.Count; j++ )
                    {
                        debugStage = 2200;
                        GameEntity_Squad spore = existingSporesOnPlanet[j].GetSquad();
                        if ( spore == null )
                            continue;
                        debugStage = 2300;
                        MacrophagePerSporeBaseInfo sData = spore.TryGetExternalBaseInfoAs<MacrophagePerSporeBaseInfo>();

                        if ( !tamedSporeOnPlanet && spore.GetFactionOrNull_Safe() == tamedFaction )
                            tamedSporeOnPlanet = true;

                        if ( sData != null )
                        {
                            GameEntity_Squad workingTelium = World_AIW2.Instance.GetEntityByID_Squad( sData.TeliumID );
                            if ( workingTelium != null )
                            {
                                if ( !working_AllTeliaWithSporesOnPlanet.Contains( workingTelium ) )
                                {
                                    working_AllTeliaWithSporesOnPlanet.Add( workingTelium );
                                    //we can easily check the faction base data from the squad.
                                    MacrophageFactionBaseInfoCore telimFactionInfo = workingTelium.GetFactionBaseInfoOrNullAs_Safe<MacrophageFactionBaseInfoCore>();
                                    //if this is not enraged or tamed, then add it to the regular list
                                    if ( !telimFactionInfo.IsEnraged && !telimFactionInfo.IsTamed )
                                        working_RegularTeliaWithSporesOnPlanet.Add( workingTelium );
                                    if ( MacrophageFactionBaseInfoCore.debug )
                                        ArcenDebugging.ArcenDebugLogSingleLine( planet.Name + " spore " + j + " teliumId " + sData.TeliumID + " is new, add its telium to the list", Verbosity.DoNotShow );
                                }
                                else if ( MacrophageFactionBaseInfoCore.debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( planet.Name + " spore " + j + " teliumId " + sData.TeliumID + " is NOT new, don't its telium to the list", Verbosity.DoNotShow );
                            }
                            else if ( MacrophageFactionBaseInfoCore.debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( sData.TeliumID + " telium is not found", Verbosity.DoNotShow );
                        }
                    }

                    debugStage = 4100;
                    //okay, we've filled out our spore collections based on ownership
                    //working_AllTeliaWithSporesOnPlanet which is every telia, no filters
                    //working_RegularTeliaWithSporesOnPlanet which is every non tamed, non enraged telia
                    //we've also figured out if there's a tamed spore on this planet

                    //now we're going to loop over every macrophage faction -- any infestations, tamed, and enraged -- and find out
                    //how many telia exist on this planet from any of them
                    int teliaCountOnThisPlanet = 0;
                    foreach ( MacrophageFactionBaseInfoCore macroCore in MacrophageFactionBaseInfoCore.AllMacrophageFactionsOfAnyTypes )
                    {
                        debugStage = 4400;
                        Dictionary<Planet, int> teliaPerPlanet = macroCore.TeliaPlanets.GetDisplayDict();
                        if ( teliaPerPlanet.ContainsKey( planet ) )
                            teliaCountOnThisPlanet += teliaPerPlanet[planet];
                    }

                    //now that we know how many telia have spores on this planet, if we have enough unique spores here, we'll consume all of them to create a new telium
                    //attempt to create a Tamed one first, which will take priority if there is even a single Tamed spore here

                    debugStage = 5100;
                    if ( working_AllTeliaWithSporesOnPlanet.Count >= BaseInfoCore.NumDifferentTeliaRequired )
                    {
                        debugStage = 5200;
                        if ( MacrophageFactionBaseInfoCore.debug )
                            ArcenDebugging.ArcenDebugLogSingleLine( "We have spores from " + working_AllTeliaWithSporesOnPlanet.Count + " different telia on " + planet.Name + ", so spawn a new Telia", Verbosity.DoNotShow );
                        bool didSpawn = false;
                        if ( tamedSporeOnPlanet && teliaCountOnThisPlanet < BaseInfoCore.TeliaPerPlanetHigh ) // Always treat the Tamed as intensity 10.
                        {
                            debugStage = 5400;
                            if ( MacrophageFactionBaseInfoCore.debug )
                                ArcenDebugging.ArcenDebugLogSingleLine( "Our new Telia is being built for the Tamed subfaction on " + planet.Name, Verbosity.DoNotShow );
                            //We don't need any special parameters to say "be a tamed telium."  we just call the spawn telium method
                            //on the tamed faction itself.  If we wanted to do enraged, we could do the same.  Who knows what faction we 
                            //are calling this from right now (we could check, but we don't seem to care).  Either way, this handles the tamed just fine.

                            MacrophageTamedFactionDeepInfo.Instance.SpawnTeliumOnPlanet( Context, planet ); // At least one tamed spore, spawn a tamed telium.
                            didSpawn = true;
                        }
                        else
                        {
                            debugStage = 6100;
                            // No Tamed, select a random regular (not enraged) Telia that has a Spore here and is below its planetary Telium cap here, and spawn a Telium for its faction.
                            // (Note this gets run once per LRP for each faction -- enraged, tamed, and possibly multiples of regular.  Hopefully that's what is intended.)
                            // We're going to use working_RegularTeliaWithSporesOnPlanet specifically so that we don't spawn any tamed or enraged ones.
                            while ( working_RegularTeliaWithSporesOnPlanet.Count > 0 )
                            {
                                debugStage = 6200;
                                int workingIndex = Context.RandomToUse.Next( working_RegularTeliaWithSporesOnPlanet.Count );
                                GameEntity_Squad workingTelium = working_RegularTeliaWithSporesOnPlanet[workingIndex].GetSquad();
                                if ( workingTelium == null )
                                {
                                    working_RegularTeliaWithSporesOnPlanet.RemoveAt( workingIndex );
                                    continue;
                                }
                                working_RegularTeliaWithSporesOnPlanet.RemoveAt( workingIndex );
                                if ( MacrophageFactionBaseInfoCore.debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "Our new Telia is being built for a regular subfaction. The Telia " + workingTelium.PrimaryKeyID + " was chosen as priority.", Verbosity.DoNotShow );

                                //we can easily check the faction deep data from the squad, too!
                                MacrophageFactionDeepInfoBase telimFactionDeepInfo = workingTelium.GetFactionDeepInfoOrNullAs_Safe<MacrophageFactionDeepInfoBase>();

                                if ( teliaCountOnThisPlanet < telimFactionDeepInfo.BaseInfoCore.TeliaPerPlanet )
                                {
                                    telimFactionDeepInfo.SpawnTeliumOnPlanet( Context, planet );
                                    if ( MacrophageFactionBaseInfoCore.debug )
                                        ArcenDebugging.ArcenDebugLogSingleLine( "We have successfully spawnt the new Telium on " + planet.Name, Verbosity.DoNotShow );
                                    didSpawn = true;
                                    break;
                                }
                                else if ( MacrophageFactionBaseInfoCore.debug )
                                    ArcenDebugging.ArcenDebugLogSingleLine( "We have failed to spawn the new Telium. Removing the invalid Telium and trying again.", Verbosity.DoNotShow );
                            }
                        }
                        debugStage = 7100;
                        if ( didSpawn )
                        {
                            debugStage = 7200;
                            // Spores are an all you can eat buffet for now. We don't want to leave any extra lying around to stack up and potentially cause Telia to be instantly rebuilt.
                            // Some experimentation determined this to be the best option, as otherwise 
                            // having dozens of spores on a planet could make popping all the Telia that keep popping back up very time consuming for the Player.
                            for ( int k = 0; k < existingSporesOnPlanet.Count; k++ )
                                existingSporesOnPlanet[k].Despawn( Context, true, InstancedRendererDeactivationReason.ThereWereTooManyOfMe );
                            // Now that we're finished, the fact that CrossFaction_SporesPerPlanet_CanRun  is false
                            // means that other Macrophage factions cannot rerun this function again this second.
                            // We reset CrossFaction_SporesPerPlanet_CanRun to true every second in Stage2, so we can acquire Spores from every Macrophage subfaction before processing.
                            return;
                        }
                    }
                    else if ( MacrophageFactionBaseInfoCore.debug )
                    {
                        debugStage = 8100;
                        ArcenDebugging.ArcenDebugLogSingleLine( planet.Name + " has " + existingSporesOnPlanet.Count + " spores with " + working_AllTeliaWithSporesOnPlanet.Count + " diffferent telia represented, and we need " + BaseInfoCore.NumDifferentTeliaRequired, Verbosity.DoNotShow );
                    }
                }
                debugStage = 8100;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "SpawnTeliumIfAble error at debugStage " + debugStage +
                    "\n" + e, Verbosity.ShowAsError );
            }
        }

        public void SpawnTeliumOnPlanet( ArcenHostOnlySimContext Context, Planet planet )
        {
            bool debug = false;
            GameEntityTypeData entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag( Context, MacrophageFactionBaseInfo.TeliumTag );
            if ( entityData == null )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "BUG: no macrophagetelium defined for spawning", Verbosity.DoNotShow );
                return;
            }
            if ( debug )
                ArcenDebugging.ArcenDebugLogSingleLine( "FLAGFLAG Spawning a new telium on " + planet.Name, Verbosity.DoNotShow );
            ArcenPoint spawnLocation = planet.GetSafePlacementPointAroundPlanetCenter( Context, entityData, FInt.FromParts( 0, 150 ), FInt.FromParts( 0, 300 ) );
            PlanetFaction pFaction = planet.GetPlanetFactionForFaction( AttachedFaction );
            GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, entityData, entityData.MarkFor( pFaction ),
                pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "Macrophage-Telium" );
            entity.CreateExternalBaseInfo<MacrophagePerTeliumBaseInfo>( "MacrophagePerTeliumBaseInfo" );
            //TEACHING_MOMENT: the MacrophagePerTeliumBaseInfo does need to be created, but nothing needs to be initialized.
            //The Cleanup() command has already set everything to the defaults.  We don't need to track a "UniqueID," because
            //the entity that is the telium has a PrimaryKeyID that will do just fine.  On the MacrophagePerTeliumBaseInfo object,
            //you can always call something like tData.AttachedSquad and get the ID, just like you see happening here for the faction AttachedFaction.
            //There's never a need to index or store relationships between external data and the thing it is attached to -- they are bidirectionally linked until death.
            //When the objects die, they clear themselves and go back to their respective pools, now separated.

            if ( planet.IntelLevel > PlanetIntelLevel.Unexplored ) //no warning if you don't have vision
            {
                PlanetViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<PlanetViewChatHandlerBase>( "PlanetGeneralFocus" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.PlanetToView = planet;

                World_AIW2.Instance.QueueChatMessageOrCommand( AttachedFaction.StartFactionColourForLog() + "Macrophage</color> Telium spawned on " + planet.Name + "!",
                    ChatType.LogToCentralChat, "ArkChiefOfStaff_TeliumSpawn", chatHandlerOrNull );
            }
            //update our spore gathering points
            GatheringPointsUpdated = false;
        }
    }
}
