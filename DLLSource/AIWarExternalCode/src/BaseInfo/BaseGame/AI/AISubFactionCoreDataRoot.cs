using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    //This sub-class exists to serve the Warden, Praetorian Guard, and Hunter.
    //No other sub-factions in the future should be based on this, in my opinion.
    //Other things like Border Aggression and CPA and Relentless Waves all have their own hierarchy and do their own thing.
    public abstract class AISubFactionCoreDataRoot : AICoreDataRoot
    {
        public AISubFactionCoreDataRoot( AISentinelsFactionBaseInfo BaseInfo ) : base( BaseInfo ) { }

        #region Core And Serialized Data
        //serialized global data
        public int CurrentTargetPlanetIndex = -1;
        public bool DisableLongRangePlanning = false;

        public sealed override void Cleanup()
        {
            CurrentTargetPlanetIndex = -1;
            DisableLongRangePlanning = false;
            SubCleanup();
        }
        protected abstract void SubCleanup();

        public void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, this.CurrentTargetPlanetIndex, "CurrentTargetPlanetIndex" );
            Buffer.AddBool( MetaData, DisableLongRangePlanning, "DisableLongRangePlanning");

            this.SubSerializeTo( MetaData, Buffer, SerializationCmdType );
        }
        protected abstract void SubSerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType );

        public void DeserializeInto( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            //TEACHING_MOMENT: Interlocked.Exchange used during deserialization??  Why would...?  (Hint: multiplayer clients)
            //Right, this seems like overkill, but this is absolutely not in this case.  The main control thread, which handles networking and the UI,
            //could be reading data from the network, in this method, on a client at any time.  On a host, this only happens when you load a game, and no BG threads are running.
            //How is this a thing? (see below)
            Interlocked.Exchange( ref this.CurrentTargetPlanetIndex, Buffer.ReadIntUltraEfficient( MetaData, UltraEfficientStyle.G_0_To_511, "CurrentTargetPlanetIndex" ) );

            if (Buffer.FromGameVersion.GetGreaterThanOrEqualTo(5,522))
                DisableLongRangePlanning = Buffer.ReadBool( MetaData, "DisableLongRangePlanning");

            this.SubDeserializeInto( MetaData, Buffer, SerializationCmdType );

            //TEACHING_MOMENT: (Part 2) Let's talk about thread timings on the client and host, in a really clear example math.  These are real numbers.
            //On the client, let's imagine we are running at 120fps, because that happens quite a bit on a reasonably strong machine.
            //There is only one "main thread" on the machine, and that runs the UI, changes talks to the network, the GPU, etc.
            //At 120fps, this means it is accomplishing all of this in 8.3 milliseconds.  Cool.  That's not enough time to do much simulating, though.
            //Therefore, we have the simulation itself running ALWAYS at a flat 10fps, kicked off from the main thread every 100ms.
            //How long does it take to run the simulation?  I have no idea!  Assuming it is less than 100ms, you will see a simulation speed of 95-105%
            //The inaccuracy (why not 100% exactly?) is driven by the frame timings of the main thread, because we can't kick off a sim thread in between those.
            //So at 120fps, we are within +- 8.3 milliseconds of that 100ms marker, but at 60fps we'd be within +- 16.7 milliseconds of the 100ms marker.
            //If a person is running at a choppy 30fps, their simulation could suffer by being by off as much as 33.3 milliseconds, which is a +- 30%, incidentally.
            //This is one reason I average the simulation speed over several seconds, and why everything uses linear interpolations (lerps) to smooth over uneven frames.
            //But hey, we're just getting started.  What about the LRP?  Well, that thread can take... I dunno.  Most start throwing warnings after lasting more than 20 seconds or so.
            //And then there's all of the "Short term planning" threads spawned by the simulation thread, which each must take well under 100ms to function correctly,
            //and then we also have the "long term continuous" threads that take "it doesn't matter" amounts of time (usually 0.5 to 5 seconds) and then start over after passing back some data.
            //To say there are a lot of threads here would be an understatement, and that's before we even get to unity's own job system, threads talking to the GPU,
            //threads involved in networking callbacks, and so on.
            //
            //The saving grace is that almost all threads touch a pretty limited set of data.  The audio threads are handed audio data and then touch that and
            //otherwise just communicate with the audio drivers.  The tractor bream short term planning thread LOOKS at all the units and scans them for "hey do you
            //have a tractor beam hitting someone, or are you being hit by someone else's tractor beam?"  But it doesn't actually change data, aside from sending a note
            //back to the main SIMULATION thread (not the true "main program" thread that runs the ui and all that other stuff) with notes on "these are the tractor beam results you wanted."
            //Heck, every time we call Parallel.DoFor or Parallel.For, we're blocking the calling thread and spawning a thread of worker threads that go and do something and then report back.
            //We do that sort of thing on the main program thread, the main simulation thread, and all sorts of background threads.
            //The nature of threading in this program is, quite very literally, fractal.  It's a tree that repeats as many times as it wants or needs to.
            //
            //TEACHING_MOMENT: (Part 3) So how can we ever touch data without all of these threads freakig out?  RULES... and also "cross-threading protection"
            //If you, as a modder programmer or faction programmer, decide you would like to start changing some model data headed to the GPU from whatever random background thread,
            //you'll introduce some immediate or intermittent cross-threading exceptions.  But with most of the parts of the actual most-core-to-the-simulation data, that doesn't happen.
            //There are a variety of methods on something like squad that read like GetFactionOrNull_Safe().  Why call that instead of squad.PlanetFaction.Faction?  Cross-threading safety.
            //It's possible that the method would return null, because the squad is in the process of being dismantled right now.  But it's not possible that it will throw an exception.
            //The most simple way to handle cross-threading exceptions is with try/catch blocks, but that introduces a very real cost.  The better way is to say
            //"PlanetFaction pFac = this.PlanetFaction.  If pFac = null, return null.  Else return pFac.Faction."  That pseudo-code uses local variables in steps to avoid cross-threading errors.
            //This code does not protect you: "If squad.PlanetFaction != null return squad.PlanetFaction.Faction"  That's ripe for an exception that is rare but persistent.
            //It happens all the time, where in the (literal) microseconds or nanoseconds between calling the if statement, getting a true, and then calling the followup code,
            //the variable is set to null and thus the if statement is too late to tell you false.  This is not theoretical, it was the bane of my coding for a goodly year.
            //The more code is accessed by a bunch of threads, the more it needs to have a ton of protection in it.  This is one reason why Core is not open-source, although not the only reason.
            //The stuff in Core is hardened against cross-threading mistakes while still allowing you direct access to variables when you need them.
            //Most of the code in External is only accessed by one or two threads, or some spawned worker threads that access data while another thread waits (Parallel.For and similar).
            //So, as complex as Exernal feels, from a threading sense it's far simpler than Core.  In cases where you have doubts, use the Interlocked class,
            //or use cross-threading-style protection for your code.
        }
        protected abstract void SubDeserializeInto( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType );
        #endregion

        protected abstract bool IsHunter { get; }
        protected abstract bool IsWarden { get; }
        protected abstract bool IsPraetorian { get; }
        protected abstract AIBudgetType MyAIBudgetType { get; }
        public abstract Faction MySpecificSubfaction { get; }
        public abstract bool ShouldHuntEnemyKingUnits { get; }
        public abstract bool ShouldCountAsThreat { get; }
        public abstract bool CanWaitForReinforcements { get; }
        public abstract bool AllowEntryIntoHostileTerritory { get; }
        public abstract bool MustCampOnNinjaBases { get; }
        public abstract bool DistanceFromKingRestricted { get; }
        public abstract FInt HostileRatio_TooDangerousToStay { get; }
        public abstract FInt HostileRatio_TooDangerousToTarget { get; }
        public abstract FInt HostileRatio_TooDangerousToGoThrough { get; }

        protected abstract void DoBudgetLoop_OnMainThreadAndPartOfSim( ArcenHostOnlySimContext Context );
        public abstract string GetBudgetName( int BudgetAsInt );
        public abstract void GetBudgetAIShipGroupCategory( int budgetTypeAsInt, AIBudgetItem budgetItem, ArcenHostOnlySimContext Context, ref DrawBag<GameEntityTypeData> bagToFill );
        public abstract Int16 GetNeverCampCloserThanXHopsToHostileTerritory();
        public abstract FInt GetOverconfidenceRatio();

        protected readonly DrawBag<GameEntityTypeData> bag = DrawBag<GameEntityTypeData>.Create_WillNeverBeGCed( 200, "AISubFactionCoreDataRoot-bag" );
        protected readonly Dictionary<SafeSquadWrapper, FInt> unitsThatCanStoreOthers = Dictionary<SafeSquadWrapper, FInt>.Create_WillNeverBeGCed( 200, "AISubFactionCoreDataRoot-unitsThatCanStoreOthers" );
        protected readonly DrawBag<SafeSquadWrapper> unitsThatCanSpawnStandalones = DrawBag<SafeSquadWrapper>.Create_WillNeverBeGCed( 200, "AISubFactionCoreDataRoot-unitsThatCanSpawnStandalones" );

        public abstract PlanetPathfinder GetPathfinderThatMustBeReleased();

        public bool HasPlayedVoiceLineForThisPlanet = false;

        #region DoPerSecondLogic_OnMainThreadAndPartOfSim_HostOnly
        public void DoPerSecondLogic_OnMainThreadAndPartOfSim_HostOnly( ArcenHostOnlySimContext Context )
        {
            #region Tracing
            bool tracing;
            if ( this.IsHunter )
                tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Hunter );
            else if ( this.IsWarden )
                tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Warden );
            else
                tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Independents );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISubFactionCore-tracing", 10f ) : null;
            if ( tracing ) tracingBuffer.Add( this.TracingName ).Add( " DoPerSecondLogic trace begins for  " + BaseInfoOfParentSentinels.AttachedFaction.GetDisplayName() );
            #endregion

            #region Make sure we still have a king
            GameEntity_Squad kingUnit = null;
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( SpecialEntityType.AIKingCommandStation ) )
            {
                if ( entity.GetFactionIndex_Safe() != BaseInfoOfParentSentinels.AttachedFaction.FactionIndex )
                    continue;
                kingUnit = entity;
                break;
            }
            if ( kingUnit == null )
            {
                foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( SpecialEntityType.AIKingMobile ) )
                {
                    if ( entity.GetFactionIndex_Safe() != BaseInfoOfParentSentinels.AttachedFaction.FactionIndex )
                        continue;
                    kingUnit = entity;
                    break;
                }
            }
            if ( kingUnit == null )
            {
                //The king is dead, so no more income, and don't join other AIs. This was essentially
                //increasing the warden/praetorian caps for subsequent AI Overlord battles too much
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "The AI is dead, so all its sub factions stop getting income anything. " );
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " DoPerSecondLogic trace ends" ).Add( " for faction " ).Add( BaseInfoOfParentSentinels.AttachedFaction.FactionIndex );
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
                return;
            }
            #endregion

            #region Try to spend any accumulated budget
            this.DoBudgetLoop_OnMainThreadAndPartOfSim( Context );
            #endregion
            #region Check for any orphan fleet ships that need to stow themselves away in a guardian
            //ships don't hide in guardians anymore. Hunter/Warden/Praetorian never go in guard posts
            #endregion
            #region Tracing
            //if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Total strength for this faction: " ).Add(totalStrength);
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " DoPerSecondLogic trace ends" ).Add( " for faction " ).Add( BaseInfoOfParentSentinels.AttachedFaction.FactionIndex );
            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            #endregion
        }
        #endregion

        #region DoSingleBudgetLoopIteration_OnMainThreadAndPartOfSim
        public FInt DoSingleBudgetLoopIteration_OnMainThreadAndPartOfSim( ArcenHostOnlySimContext Context, FInt budget, 
            int BudgetTypeAsInt, out bool noUnitsAvailable, FireteamRequiredTarget RequiredTargetOrNull = null )
        {
            ArcenDebugging.PsuedoMethodName = "DoSingleBudgetLoopIteration";
            ArcenDebugging.PsuedoLineNumber = 10;

            //TEACHING_MOMENT: MySpecificSubfaction looks like a variable, but it's actually a propery, and it calls a whole chain of other variables and properties
            //This is not actually all that particularly expensive to do on the CPU, but it's certainly a lot more than just calling a local variable like "myFaction"
            //below.  Some of these methods use "myFaction" in loops, and some of those loops are nested loops.  Even something cheap on the CPU can add up if it's
            //done enough times in one method.  So I prefer to make the local variable and just work off that, just to be on the safe side.
            //In other places, where we have things like BaseInfo, or AttachedFaction, the calls are so cheap that it's not worth it even inside nested loops.
            //That said, if we had a truly heinous amount of loop nesting in some variable havine a "Faction attachedFac = this.BaseInfo.AttachedFaction" is certainly
            //not a bad idea.  It's not required, but every few microseconds shaved off can be nice.  Look for loops or nested loops when thinking of that sort of thing.
            //Don't bother just doing that everywhere, because all you'll really accomplish is making the code longer and harder to read.
            Faction myFaction = MySpecificSubfaction;

            #region Tracing
            bool tracing;
            if ( this.IsHunter )
                tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Hunter );
	    else if ( this.IsWarden )
                tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Warden );
            else
                tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Independents );
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISubFac-DoSingleBudgetLoopIteration_OnMainThreadAndPartOfSim-tracing", 10f ) : null;
            #endregion
            
            FInt result = FInt.Zero;
            noUnitsAvailable = false;

            if ( budget <= FInt.Zero )
            {
                #region Tracing
                if ( tracing )
                {
                    tracingBuffer.Add( myFaction.GetDisplayName() ).Add( " skipping budget " ).Add( this.GetBudgetName( BudgetTypeAsInt ) );
                    if ( RequiredTargetOrNull != null )
                        tracingBuffer.Add( " against " ).Add( RequiredTargetOrNull.ToString() );
                    tracingBuffer.Add( " because budget balance " ).Add( budget.ReadableString ).Add( " is <= zero" );
                }
                if ( tracing )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion
                return result;
            }
            
            ArcenDebugging.PsuedoLineNumber = 20;
            this.bag.Clear();
            this.FillBag( BudgetTypeAsInt, this.bag, Context );
            if ( this.bag.InternalListSize == 0 )
                noUnitsAvailable = true;
            
            #region Tracing
            if ( tracing )
            {
                tracingBuffer.Add( "\tDoSingleBudgetLoopIteration " ).Add(" for ").Add(myFaction.GetDisplayName()).Add( "budget " ).Add( this.GetBudgetName( BudgetTypeAsInt ) );
                if ( RequiredTargetOrNull != null )
                    tracingBuffer.Add( " Against " ).Add( RequiredTargetOrNull.ToString() );
                tracingBuffer.Add( " has " ).Add( budget.ReadableString ).Add( " resource, so trying to spend it (bag contains " ).Add( this.bag.InternalListSize ).Add( " items)" );
            }
            #endregion
            
            ArcenDebugging.PsuedoLineNumber = 30;
            this.unitsThatCanSpawnStandalones.Clear();
            this.unitsThatCanStoreOthers.Clear();
            try
            {
                int attemptsLeft = 100;
                while ( budget > FInt.Zero && attemptsLeft > 0 )
                {

                    GameEntityTypeData typeToBuy = this.PickTypeToBuy( Context );
                    ArcenDebugging.PsuedoLineNumber = 40;
                    if ( typeToBuy == null )
                    {
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "picked null type " );
                        #endregion
                        break;
                    }
                    
                    int budgetToSpend = Math.Max( 0, typeToBuy.CostForAIToPurchase ); // it would never be < 0 anyway, but just for paranoia's sake
                    
                    #region Tracing
                    if ( tracing )
                    {
                        tracingBuffer.Add( "\n" ).Add( "\t" ).Add( "picked " ).Add( typeToBuy.GetDisplayName() ).Add( " (" ).Add( budgetToSpend ).Add( " budget cost)" );
                        if ( RequiredTargetOrNull != null && RequiredTargetOrNull.IsActive() )
                        {
                            tracingBuffer.Add( " against " + RequiredTargetOrNull );
                            ArcenDebugging.ArcenDebugLogSingleLine( "Buying a " + typeToBuy.GetDisplayName() + " against " + RequiredTargetOrNull.ToString(), Verbosity.DoNotShow );
                        }
                    }
                    #endregion

                    //if ( typeToBuy.RollupLookup[EntityRollupType.ReinforcementLocations] || typeToBuy.CannotBeStoredInsideGuardPost )
                    //the above if condition was for when we wanted to spawn fleetships inside of other units
                    ArcenDebugging.PsuedoLineNumber = 45;
                    {
                        ArcenDebugging.PsuedoLineNumber = 50;
                        if ( this.IsPraetorian /*AllBudgetSpentAtKing*/ && !this.unitsThatCanSpawnStandalones.GetHasItems() )
                        {
                            //if we must spend our budget to spawn units at the AI King (ie we are Praetorians), do that here
                            ArcenDebugging.PsuedoLineNumber = 51;
                            foreach ( GameEntity_Squad e in World_AIW2.Instance.Squads( SpecialEntityType.AIKingCommandStation ) )
                            {
                                if ( e.GetFactionIndex_Safe() != this.BaseInfoOfParentSentinels.AttachedFaction.FactionIndex )
                                    continue;

                                this.unitsThatCanSpawnStandalones.AddItem( e, 1 );
                            }

                            foreach ( GameEntity_Squad e in World_AIW2.Instance.Squads( SpecialEntityType.AIKingMobile ) )
                            {
                                if ( e.GetFactionIndex_Safe() != this.BaseInfoOfParentSentinels.AttachedFaction.FactionIndex )
                                    continue;

                                this.unitsThatCanSpawnStandalones.AddItem( e, 1 );
                            }
                        }

                        if ( this.IsWarden && !this.unitsThatCanSpawnStandalones.GetHasItems() )
                        {
                            //First check if there are any Ninja Hideouts, and use those preferentially if they are ours.  No matter who owns the planet.
                            //This is only if we're a Warden, mind.
                            foreach ( GameEntity_Squad e in myFaction.Squads( "WardenSecretNinjaHideout" ) )
                            {
                                this.unitsThatCanSpawnStandalones.AddItem( e, 1 );
                            }
                        }

                        if ( !this.unitsThatCanSpawnStandalones.GetHasItems() )
                        {
                            //if there are no Ninja Hideouts or we are not a warden, pick randomly otherwise
                            ArcenDebugging.PsuedoLineNumber = 52;
                            Planet currentTargetPlanet = World_AIW2.Instance.GetPlanetByIndex( (Int16)this.CurrentTargetPlanetIndex );

                            ArcenDebugging.PsuedoLineNumber = 53;
                            if ( currentTargetPlanet != null )
                            {
                                var myFactionData = currentTargetPlanet.GetStanceDataForFaction( myFaction );
                                if ( myFactionData[FactionStance.Hostile].TotalStrengthIncludingNonMilitary <= 0 &&
                                     myFactionData[FactionStance.Self].TotalStrength > 0 )
                                {
                                    GameEntity_Squad commandStation = currentTargetPlanet.GetCommandStationOrNull();
                                    if ( commandStation != null )
                                        this.unitsThatCanSpawnStandalones.AddItem( commandStation, 1 ); // if we're already in position at an unopposed planet, just warp the stuff in TODO: possibly require a warp gate for this, but then we need to consider the presence of a warp gate when deciding on a staging point, etc
                                }
                            }

                            if ( this.ShouldHuntEnemyKingUnits )
                            {
                                bool foundKingUnitToHunt = false;
                                foreach ( Faction otherFaction in myFaction.RelatedFactions( FactionRelationship.FactionsIAmHostileTowards ) )
                                {
                                    foreach ( GameEntity_Squad entity in otherFaction.Squads( EntityRollupType.KingUnitsOnly ) )
                                    {
                                        if ( !entity.Planet.GetControllingFaction().GetIsFriendlyTowards( myFaction ) )
                                            continue;
                                        foreach ( var ph in entity.Planet.PlanetsWithinXHops_NoFilters( 3 ) )
                                        {
                                            Planet planet = ph.Planet;
                                            Int16 hops = ph.Hops;
                                            if ( planet == entity.Planet )
                                                continue;
                                            GameEntity_Squad commandStation = planet.GetCommandStationOrNull();
                                            if ( commandStation == null || !commandStation.GetIsFriendlyTowards_Safe( myFaction ) )
                                                continue;
                                            this.unitsThatCanSpawnStandalones.AddItem( commandStation, 1 );
                                            foundKingUnitToHunt = true;
                                            break;
                                        }
                                        if ( foundKingUnitToHunt )
                                            break;
                                    }
                                    if ( foundKingUnitToHunt )
                                        break;
                                }
                            }

                            ArcenDebugging.PsuedoLineNumber = 54;
                            if ( !this.unitsThatCanSpawnStandalones.GetHasItems() )
                            {
                                ArcenDebugging.PsuedoLineNumber = 55;
                                foreach ( GameEntity_Squad e in World_AIW2.Instance.Squads( SpecialEntityType.AIKingCommandStation ) )
                                {
                                    if ( e.GetFactionIndex_Safe() != myFaction.FactionIndexOfMyParentIfIHaveOne )
                                        continue;

                                    this.unitsThatCanSpawnStandalones.AddItem( e, 1 );
                                }

                                foreach ( GameEntity_Squad e in World_AIW2.Instance.Squads( SpecialEntityType.AIKingMobile ) )
                                {
                                    if ( e.GetFactionIndex_Safe() != myFaction.FactionIndexOfMyParentIfIHaveOne )
                                        continue;

                                    this.unitsThatCanSpawnStandalones.AddItem( e, 1 );
                                }
                            }
                            
                            ArcenDebugging.PsuedoLineNumber = 56;
                            if ( !this.unitsThatCanSpawnStandalones.GetHasItems() )
                            {
                                #region Tracing
                                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "\t\t" ).Add( "could not find HQ to spawn at" );
                                #endregion
                                break;
                            }
                        }
                        
                        ArcenDebugging.PsuedoLineNumber = 60;
                        
                        GameEntity_Squad entityToSpawnAt = this.unitsThatCanSpawnStandalones.PickRandomItemAndReplace( Context.RandomToUse ).GetSquad();
                        if ( entityToSpawnAt == null )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "Warning: AISubFactionDeepInfo.DoSingleBudgetLoopIteration found case where entityToSpawnAt is null", Verbosity.Chat );
                            break;
                        }
                        
                        if ( entityToSpawnAt.Planet == null )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "Warning: AISubFactionDeepInfo.DoSingleBudgetLoopIteration found case where entityToSpawnAt.Planet is null", Verbosity.Chat );
                            break;
                        }
                        
                        PlanetFaction pFaction = entityToSpawnAt.Planet.GetPlanetFactionForFaction( myFaction );
                        if ( pFaction == null )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "Warning: AISubFactionDeepInfo.DoSingleBudgetLoopIteration found case where planetFaction is null", Verbosity.Chat );
                            break;
                        }
                        
                        Faction fac = pFaction.Faction;
                        if ( fac == null )
                        {
                            ArcenDebugging.ArcenDebugLogSingleLine( "Warning: AISubFactionDeepInfo.DoSingleBudgetLoopIteration found case where fac is null", Verbosity.Chat );
                            break;
                        }
                        
                        ArcenDebugging.PsuedoLineNumber = 61;
                        GameEntity_Squad newEntityOrNull = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( pFaction, typeToBuy, typeToBuy.MarkFor( pFaction ),
                            fac.LooseFleet, 0, entityToSpawnAt.WorldLocation, Context, "AISubFactionBudget" );
                        
                        ArcenDebugging.PsuedoLineNumber = 62;
                        if ( RequiredTargetOrNull != null &&
                             RequiredTargetOrNull.IsActive() )
                        {
                            if ( newEntityOrNull != null )
                            {
                                newEntityOrNull.FireteamSpecificationOrNull = FireteamRequiredTarget.GetFromPoolOrCreate();
                                newEntityOrNull.FireteamSpecificationOrNull.CopyFrom( RequiredTargetOrNull );
                            }
                        }
                        
                        ArcenDebugging.PsuedoLineNumber = 63;
                        int oldContentsCostForAIToPurchase = entityToSpawnAt.GetCostForAIToPurchaseOfContentsIfAny();

                        if ( newEntityOrNull != null )
                        {
                            newEntityOrNull.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full ); //part of main thread, all good
                            ArcenDebugging.PsuedoLineNumber = 64;
                            //note from Chris: not really sure why this is here, but it has been here for a long time and so I'm reluctant to remove it.
                            //for more details see WeaponMaster's notes here: https://bugtracker.arcengames.com/view.php?id=21275
                            if ( newEntityOrNull.TypeData.IsReinforcementLocation )
                                entityToSpawnAt.TransferAIReinforcementPointContentsTo( newEntityOrNull );
                            ArcenDebugging.PsuedoLineNumber = 65;
                            newEntityOrNull.ShouldNotBeConsideredAsThreatToHumanTeam = !ShouldCountAsThreat;
                        }
                        
                        ArcenDebugging.PsuedoLineNumber = 66;
                        int costForAIToPurchaseTransferred = oldContentsCostForAIToPurchase - entityToSpawnAt.GetCostForAIToPurchaseOfContentsIfAny();
                        
                        ArcenDebugging.PsuedoLineNumber = 67;
                        #region Tracing
                        if ( tracing )
                        {
                            tracingBuffer.Add( "\n" ).Add( "\t\t" ).Add( "spawned at " ).Add( entityToSpawnAt.TypeData.InternalName ).Add( " on " ).Add( entityToSpawnAt.GetPlanetName_Safe() ).Add( " and transferred " ).Add( costForAIToPurchaseTransferred ).Add( " ai purchase cost to the new unit's hangars.\n" );
                            if ( RequiredTargetOrNull != null && RequiredTargetOrNull.IsActive() )
                                tracingBuffer.Add( "\tThis unit must go after " + RequiredTargetOrNull );
                        }

                        #endregion
                        ArcenDebugging.PsuedoLineNumber = 70;
                    }
                    if ( budgetToSpend == FInt.Zero )
                        attemptsLeft--;
                    result += budgetToSpend;
                    budget -= budgetToSpend;
                }
                
                ArcenDebugging.PsuedoLineNumber = 120;
                this.bag.Clear();
                if ( tracing )
                {
                    tracingBuffer.Add( "\tDoSingleBudgetLoopIteration just spent " ).Add( result );
                    ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                
                return result;
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "DoSingleBudgetLoopIteration_OnMainThreadAndPartOfSim Error: " + e, Verbosity.ShowAsError );
                return FInt.Zero;
            }
            finally
            {
                this.unitsThatCanSpawnStandalones.Clear();
                this.unitsThatCanStoreOthers.Clear();
            }
        }
        #endregion

        #region FillBag
        /// <summary>
        /// This is for independent fleets, aka things like the Warden and the Hunter, so it's a lot more eclectic in terms of how it fills in its data.
        /// It's not remembering the AIShipGroups by planet or something, but rather just choosing at random.  At some point it might be nice for them to be
        /// a little more cohesive in their contents, but they get donations from various sources anyway, so it's not a big deal.
        /// </summary>
        public void FillBag( int budgetTypeAsInt, DrawBag<GameEntityTypeData> bagToFill, ArcenHostOnlySimContext Context )
        {
            Faction myFaction = MySpecificSubfaction;

            for ( int i = 0; i < World_AIW2.Instance.AIFactions.Count; i++ )
            {
                Faction otherFaction = World_AIW2.Instance.AIFactions[i];
                if ( !otherFaction.GetIsFriendlyTowards( myFaction ) )
                    continue;
                if ( otherFaction.SpecialFactionData.InternalName != "AI" )
                    continue;
                if ( otherFaction.FactionIndex != myFaction.FactionIndexOfMyParentIfIHaveOne )
                    continue; //the Hunter/Warden for a given AI faction is only allowed to use the unit types from that AI faction
                              //this promotes diversity amongst the hunter/wardens
                AITypeData otherFactionAIType = otherFaction.TryGetAISentinelsCoreData().SentinelInfo.AIType;
                //sAIShipGroupCategory category = null;
                AIBudgetItem budgetItem = otherFactionAIType.BudgetItems[this.MyAIBudgetType];
                bagToFill.Clear();
                this.GetBudgetAIShipGroupCategory( budgetTypeAsInt, budgetItem, Context, ref bagToFill );
                return;
            }
        }
        #endregion

        #region PickTypeToBuy
        public GameEntityTypeData PickTypeToBuy( ArcenHostOnlySimContext Context )
        {
            return this.bag.PickRandomItemAndReplace( Context.RandomToUse );
        }
        #endregion

        #region GetShouldWaitToAttack
        public bool GetShouldWaitToAttack( Planet target, FInt fleetStrength, out FInt presentFleetStrength, ArcenHostOnlySimContext Context )
        {
            Faction myFaction = MySpecificSubfaction;

            //Wait till at least half the fleet is on the target or an adgacent planet
            FInt nearbyStrength = FInt.Zero;
            EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> targetFactionData = target.GetStanceDataForFaction( myFaction );
            nearbyStrength += targetFactionData[FactionStance.Self].TotalStrength + targetFactionData[FactionStance.Friendly].TotalStrength;
            foreach ( Planet neighbor in target.LinkedNeighbors( false ) )
            {
                EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> neighborFactionData = neighbor.GetStanceDataForFaction( myFaction );
                nearbyStrength += neighborFactionData[FactionStance.Self].TotalStrength;
            }
            presentFleetStrength = nearbyStrength;

            /* The Warden/Hunter is not allowed to wait to attack at lower difficulties. Praetorian stays smart */
            if ( !this.CanWaitForReinforcements )
                return false;

            if ( presentFleetStrength >= targetFactionData[FactionStance.Hostile].TotalStrength )
                return false; //if we already outnumber the target's forces, just go
            if ( presentFleetStrength < fleetStrength / 2 )
            {
                //                ArcenDebugging.ArcenDebugLogSingleLine("Present fleet strength: " + presentFleetStrength + " total " + fleetStrength + " we should wait", Verbosity.DoNotShow );
                return true;
            }
            return false;
        }
        #endregion

        #region GetCurrentTargetPlanet
        public Planet GetCurrentTargetPlanet( Planet primaryDefensePlanet, Int16 currentTargetIndex, ArcenHostOnlySimContext Context, FInt fleetStrength )
        {
            Faction myFaction = MySpecificSubfaction;

            #region Tracing
            bool tracing = this.tracing_longTerm;
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISubFac-GetCurrentTargetPlanet-tracing", 10f ) : null;
            #endregion
            Planet currentTargetPlanet = World_AIW2.Instance.GetPlanetByIndex( currentTargetIndex );
            //If the primary defense planet (ie the AI king planet) is under attack, all Hunter and Warden fleet units converege there immediately
            var primaryFactionData = primaryDefensePlanet.GetStanceDataForFaction( myFaction );
            StrengthData_PlanetFaction_Stance friendlyDataPrimary = primaryFactionData[FactionStance.Friendly];
            StrengthData_PlanetFaction_Stance selfDataPrimary = primaryFactionData[FactionStance.Self];
            StrengthData_PlanetFaction_Stance hostileDataPrimary = primaryFactionData[FactionStance.Hostile];
            if ( hostileDataPrimary.TotalStrength >= friendlyDataPrimary.TotalStrength / 10 ) //this is an attack of at least 1/10th of the homeworld's defenses
            {
                return primaryDefensePlanet;
            }

            // if we have an existing target, but that target is now not longer "allowed", each time we have a chance of abandoning the attack.
            // This way the further away the Warden Fleet is, the more likely it is to not press home the attack.
            // Note that we will not abandon if we already outnumber the enemy
            byte ChanceOfAbandonEachLRP = 10;
            if ( currentTargetPlanet != null && !this.AllowEntryIntoHostileTerritory )
            {
                int randomNumber = Context.RandomToUse.Next( 1, 100 );
                if ( currentTargetPlanet.GetIsEitherControllerOrInfluencerHostileTo( myFaction ) ) //an enemy now controls the planet
                {
                    EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> myFactionData = currentTargetPlanet.GetStanceDataForFaction( myFaction );
                    StrengthData_PlanetFaction_Stance friendlyData = myFactionData[FactionStance.Friendly];
                    StrengthData_PlanetFaction_Stance selfData = myFactionData[FactionStance.Self];
                    StrengthData_PlanetFaction_Stance hostileData = myFactionData[FactionStance.Hostile];

                    if ( selfData.TotalStrength + friendlyData.TotalStrength < hostileData.TotalStrength &&  //if we don't outnumber the enemy right now
                         randomNumber < ChanceOfAbandonEachLRP ) //we have a chance of abandoning the attack
                        currentTargetPlanet = primaryDefensePlanet;
                }
            }

            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " Routing picking target; fleet strength = " ).Add( fleetStrength.ReadableString );
            #endregion

            Int16 bestTargetFound_Index = -1;
            FInt bestTargetFound_Danger = FInt.Zero;
            bool currentTargetIsValid = true;
            this.Helper_DoTargetFindingSweep(  primaryDefensePlanet, Context, fleetStrength, ref bestTargetFound_Index, ref bestTargetFound_Danger, DangerCheckMode.PresentAndNearby );
            if ( bestTargetFound_Index < 0 )
            {
                // looking for a camping spot, so the too-close-to-hostile-territory rule applies, as well as the Ninja Base requirement
                Int16 neverCampCloserThanXHopsToHostileTerritory = this.GetNeverCampCloserThanXHopsToHostileTerritory();
                if ( neverCampCloserThanXHopsToHostileTerritory > 0 &&
                     Helper_GetIsPlanetWithinXHopsOfHostileTerritory( currentTargetPlanet, neverCampCloserThanXHopsToHostileTerritory ) )
                    currentTargetIsValid = false;
                this.Helper_DoTargetFindingSweep( primaryDefensePlanet, Context, fleetStrength, ref bestTargetFound_Index, ref bestTargetFound_Danger, DangerCheckMode.NearbyOnly );
            }

            {
                FInt currentTargetPlanetDanger = GetDanger( DangerCheckMode.PresentAndNearby, currentTargetPlanet );

                if ( bestTargetFound_Index < 0 )
                {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Preferring current (" + currentTargetPlanet.Name + ") to best-found (" + World_AIW2.Instance.GetPlanetByIndex( bestTargetFound_Index ).Name + ") because best-found is... nothing. We didn't find anything. That's impressive." );
                    #endregion
                    bestTargetFound_Index = currentTargetPlanet.Index;
                    bestTargetFound_Danger = currentTargetPlanetDanger;
                }
                else if ( !currentTargetIsValid )
                {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Preferring best-found " + World_AIW2.Instance.GetPlanetByIndex( bestTargetFound_Index ).Name + " to current " + currentTargetPlanet.Name + " because current is not valid (probably too close to hostile territory for a camping spot)" );
                    #endregion
                }
                else if ( currentTargetPlanetDanger >= fleetStrength * this.HostileRatio_TooDangerousToStay )
                {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Preferring best-found to current because current is too dangerous: " ).Add( currentTargetPlanetDanger.ReadableString );
                    #endregion
                }
                else if ( this.IsWarden && MustCampOnNinjaBases )
                {
                    PlanetFaction planetFaction = currentTargetPlanet.GetPlanetFactionForFaction( myFaction );
                    bool hasNinjaHideout = false;
                    foreach ( GameEntity_Squad entity in planetFaction.Entities.Squads( "WardenSecretNinjaHideout" ) )
                    {
                        hasNinjaHideout = true;
                        break;
                    }
                    if ( !hasNinjaHideout )
                    {
                        EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> myFactionData = currentTargetPlanet.GetStanceDataForFaction( myFaction );
                        StrengthData_PlanetFaction_Stance hostileData = myFactionData[FactionStance.Hostile];

                        if ( hostileData.TotalStrength < 1 )
                        {
                            #region Tracing
                            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Preferring best-found to current because current has no enemies and also doesn't have a ninja base. " );
                            #endregion
                        }
                    }

                }
                else if ( bestTargetFound_Danger <= (currentTargetPlanetDanger << 1) )
                {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Preferring current (" + currentTargetPlanet.Name + ") to best-found (" + World_AIW2.Instance.GetPlanetByIndex( bestTargetFound_Index ).Name + ") because best-found's danger level is less than twice the current (this prevents hopping back and forth too frequently)" );
                    #endregion
                    bestTargetFound_Index = currentTargetPlanet.Index;
                    bestTargetFound_Danger = currentTargetPlanetDanger;
                }
            }

            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }

            return World_AIW2.Instance.GetPlanetByIndex( bestTargetFound_Index );
        }
        #endregion

        #region virtual Helper_DoTargetFindingSweep
        protected virtual void Helper_DoTargetFindingSweep( Planet primaryDefensePlanet, ArcenHostOnlySimContext Context, FInt fleetStrength, ref Int16 BestTargetFound_Index, ref FInt BestTargetFound_Danger, DangerCheckMode dangerCheckMode )
        {
            Faction myFaction = MySpecificSubfaction;

            #region Tracing
            bool tracing = this.tracing_longTerm;
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISubFac-Helper_DoTargetFindingSweep-tracing", 10f ) : null;
            #endregion
            bool allowEntryIntoHostileTerritory = this.AllowEntryIntoHostileTerritory;
            Int16 neverCampCloserThanXHopsToHostileTerritory = this.GetNeverCampCloserThanXHopsToHostileTerritory();

            Int16 bestTargetFound_Index = BestTargetFound_Index;
            FInt bestTargetFound_Danger = BestTargetFound_Danger;
            foreach ( Planet.PlanetAtHopDistance _phd in primaryDefensePlanet.PlanetsWithinXHops( -1,
                delegate ( Planet secondaryPlanet )
                {
                    if ( !allowEntryIntoHostileTerritory && secondaryPlanet.GetIsEitherControllerOrInfluencerHostileTo( myFaction ) )
                    {
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Refusing to flood into " ).Add( secondaryPlanet.Name ).Add( " because controlled by side hostile to me" );
                        #endregion
                        return PropogationEvaluation.No;
                    }
                    FInt danger = this.GetDanger( dangerCheckMode, secondaryPlanet );
                    if ( danger >= fleetStrength * this.HostileRatio_TooDangerousToTarget )
                    {
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Refusing to flood into " ).Add( secondaryPlanet.Name ).Add( " because too much danger: " ).Add( danger.ReadableString ).Add( " >= " + fleetStrength + " * " + this.HostileRatio_TooDangerousToTarget + " == " + fleetStrength * this.HostileRatio_TooDangerousToTarget );
                        #endregion
                        return PropogationEvaluation.No;
                    }
                    if ( danger >= fleetStrength * this.HostileRatio_TooDangerousToGoThrough )
                    {
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Refusing to flood through " ).Add( secondaryPlanet.Name ).Add( " because too much danger: " ).Add( danger.ReadableString );
                        #endregion
                        return PropogationEvaluation.SelfButNotNeighbors;
                    }
                    return PropogationEvaluation.Yes;
                } ) )
            {
                Planet planet = _phd.Planet;
                Int16 Distance = _phd.Hops;
                FInt friendlyStrengthTotal;
                FInt hostileStrengthActuallyOnPlanet;
                Int16 hopsFromHostileForThisPlanet = Helper_GetHopsFromHostileTerritory( planet, Context );
                Int16 hopsFromHostileForBestPlanet = Helper_GetHopsFromHostileTerritory( World_AIW2.Instance.GetPlanetByIndex( bestTargetFound_Index ), Context );

                if ( this.DistanceFromKingRestricted )
                {
                    Faction aiFaction = myFaction.GetParentFactionOrNull();
                    AITypeData aiType = aiFaction.TryGetAISentinelsCoreData().SentinelInfo.AIType;
                    if ( Distance > aiType.PraetorianRange )
                        continue;
                }
                FInt danger = GetDanger( dangerCheckMode, planet, out friendlyStrengthTotal, out hostileStrengthActuallyOnPlanet );
                if ( dangerCheckMode == DangerCheckMode.PresentAndNearby && hostileStrengthActuallyOnPlanet <= 0 )
                {
                    #region Tracing
                    // commented out because extreme spam
                    //if ( tracing ) tracingBuffer.Add( "\n" ).Add( "rejecting target " ).Add( planet.Name ).Add( " because this is the first-try sweep and there is no hostile strength actually on planet" );
                    #endregion
                    continue;
                }
                if ( dangerCheckMode == DangerCheckMode.NearbyOnly && hostileStrengthActuallyOnPlanet > 0 )
                {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "rejecting target " ).Add( planet.Name ).Add( " because this is the second-try sweep and there IS hostile strength actually on planet" );
                    #endregion
                    continue;
                }
                if ( bestTargetFound_Index >= 0 && bestTargetFound_Danger >= danger &&
                     hopsFromHostileForBestPlanet <= hopsFromHostileForThisPlanet )
                {
                    #region Tracing
                    // commented out because extreme spam
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "rejecting target " ).Add( planet.Name ).Add( " because danger lower than current best target: " ).Add( danger.ReadableString ).Add( " and distance to hostile from best " ).Add( hopsFromHostileForBestPlanet ).Add( " <= " ).Add( hopsFromHostileForThisPlanet );
                    #endregion
                    continue;
                }
                if ( dangerCheckMode == DangerCheckMode.NearbyOnly &&
                     neverCampCloserThanXHopsToHostileTerritory > 0 &&
                     Helper_GetIsPlanetWithinXHopsOfHostileTerritory( planet, neverCampCloserThanXHopsToHostileTerritory ) )
                {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "rejecting target " ).Add( planet.Name ).Add( " because is closer than " ).Add( neverCampCloserThanXHopsToHostileTerritory ).Add( " hops to hostile-owned planet" );
                    #endregion
                    continue;
                }
                if ( dangerCheckMode == DangerCheckMode.NearbyOnly &&
                     planet.GetControllingFactionType() == FactionType.AI &&
                     planet.GetControllingFaction().FactionIndex != myFaction.FactionIndexOfMyParentIfIHaveOne )
                {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "rejecting target " ).Add( planet.Name ).Add( " because it is not owned by my parent faction index " ).Add( planet.GetControllingFaction().FactionIndex ).Add( " != " ).Add( myFaction.FactionIndexOfMyParentIfIHaveOne );
                    #endregion
                    continue;
                }
                if ( dangerCheckMode == DangerCheckMode.NearbyOnly && MustCampOnNinjaBases )
                {
                    //if we are looking for camping spot, only pick ninja base planets without
                    //enemies
                    bool hasNinjaHideout = false;
                    PlanetFaction planetFaction = planet.GetPlanetFactionForFaction( myFaction );
                    foreach ( GameEntity_Squad entity in planetFaction.Entities.Squads( "WardenSecretNinjaHideout" ) )
                    {
                        hasNinjaHideout = true;
                        break;
                    }
                    if ( !hasNinjaHideout )
                    {
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "rejecting target " ).Add( planet.Name ).Add( " because it does not have a WardenSecretNinjaHideout and also has no enemies, so it's not a suitable place to camp " );
                        #endregion
                        continue;
                    }
                }
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "current best target = " ).Add( planet.Name ).Add( "; danger : " ).Add( danger.ReadableString ).Add( ". Danger check mode " ).Add( dangerCheckMode.ToString() );
                #endregion
                bestTargetFound_Index = planet.Index;
                bestTargetFound_Danger = danger;
            }
            BestTargetFound_Index = bestTargetFound_Index;
            BestTargetFound_Danger = bestTargetFound_Danger;

            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
        }
        #endregion end virtual Helper_DoTargetFindingSweep

        #region Helper_GetHopsFromHostileTerritory
        public Int16 Helper_GetHopsFromHostileTerritory( Planet planet, ArcenHostOnlySimContext Context )
        {
            Faction myFaction = MySpecificSubfaction;

            Int16 hops = 999;
            if ( planet == null || myFaction == null )
                return hops; //this can be called with an unset planet, in which case return "Max Distance" so we don't count it above
            foreach ( var ph in planet.PlanetsWithinXHops_NoFilters( -1 ) )
            {
                Planet otherPlanet = ph.Planet;
                Int16 distance = ph.Hops;
                if ( !otherPlanet.GetIsEitherControllerOrInfluencerHostileTo( myFaction ) )
                    continue;
                hops = distance;
                break;
            }
            return hops;
        }
        #endregion

        #region Helper_GetIsPlanetWithinXHopsOfHostileTerritory
        protected bool Helper_GetIsPlanetWithinXHopsOfHostileTerritory( Planet planet, Int16 neverCampCloserThanXHopsToHostileTerritory )
        {
            Faction myFaction = MySpecificSubfaction;
            if ( planet == null || myFaction == null )
                return false;

            bool result = false;
            foreach ( var ph in planet.PlanetsWithinXHops_NoFilters( (Int16)(neverCampCloserThanXHopsToHostileTerritory - 1) ) )
            {
                Planet otherPlanet = ph.Planet;
                Int16 distance = ph.Hops;
                if ( !otherPlanet.GetIsEitherControllerOrInfluencerHostileTo( myFaction ) )
                    continue;
                result = true;
                break;
            }
            return result;
        }
        #endregion

        public enum DangerCheckMode
        {
            PresentAndNearby,
            NearbyOnly,
        }

        #region GetDanger
        protected FInt GetDanger( DangerCheckMode Mode, Planet planet )
        {
            FInt dummy, dummy2;
            return this.GetDanger( Mode, planet, out dummy, out dummy2 );
        }

        protected FInt GetDanger( DangerCheckMode Mode, Planet planet, out FInt friendlyStrengthTotal, out FInt hostileStrengthActuallyOnThePlanet )
        {
            Faction myFaction = MySpecificSubfaction;

            #region Tracing
            bool tracing = this.tracing_longTerm && planet == Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
            ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISubFac-GetDanger-tracing", 10f ) : null;
            if ( tracing ) tracingBuffer.Add( "\n\t" ).Add( "computing danger of planet " ).Add( planet.Name ).Add( " in mode " ).Add( Mode.ToString() );
            #endregion
            EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> myFactionData = planet.GetStanceDataForFaction( myFaction );
            StrengthData_PlanetFaction_Stance hostileData = myFactionData[FactionStance.Hostile];

            hostileStrengthActuallyOnThePlanet = FInt.Zero;
            hostileStrengthActuallyOnThePlanet += hostileData.TotalStrength;
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n\t" ).Add( "hostileStrengthActuallyOnThePlanet += hostileData.TotalStrength => " ).Add( hostileStrengthActuallyOnThePlanet.ReadableString );
            #endregion
            friendlyStrengthTotal = FInt.Zero;
            FInt hostileStrengthTotal = FInt.Zero;
            if ( Mode == DangerCheckMode.PresentAndNearby )
            {
                friendlyStrengthTotal += myFactionData[FactionStance.Friendly].TotalStrength; // leaving out self-strength so that the fleet doesn't constantly displace itself
                friendlyStrengthTotal += myFactionData[FactionStance.Friendly].IncomingStrength;
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n\t" ).Add( "friendlyStrengthTotal += myFactionData[FactionStance.Friendly].TotalStrength + myFactionData[FactionStance.Friendly].IncomingStrength => " ).Add( friendlyStrengthTotal.ReadableString );
                #endregion
                hostileStrengthTotal += hostileData.TotalStrength;
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n\t" ).Add( "hostileStrengthTotal += hostileData.TotalStrength => " ).Add( hostileStrengthTotal.ReadableString );
                #endregion
            }

            FInt overconfidenceRatio = GetOverconfidenceRatio();
            friendlyStrengthTotal *= overconfidenceRatio;

            int divisor = 3;
            for ( int i = 1; i < hostileData.UnengagedMobileStrengthByHopCount.Length; i++ ) // start with 1 to ignore stuff on the planet itself; that's already counted
            {
                int unengagedStrengthAtHopCount = hostileData.UnengagedMobileStrengthByHopCount[i];
                int adjustedUnengagedStrength = unengagedStrengthAtHopCount / divisor;
                hostileStrengthTotal += adjustedUnengagedStrength;
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n\t" ).Add( "FInt unengagedStrengthAtHopCount = hostileData.UnengagedMobileStrengthByHopCount[" ).Add( i ).Add( "] => " ).Add( unengagedStrengthAtHopCount );
                if ( tracing ) tracingBuffer.Add( "\n\t" ).Add( "FInt adjustedUnengagedStrength = unengagedStrengthAtHopCount / " ).Add( divisor ).Add( " => " ).Add( adjustedUnengagedStrength );
                if ( tracing ) tracingBuffer.Add( "\n\t" ).Add( "hostileStrengthTotal += adjustedUnengagedStrength => " ).Add( hostileStrengthTotal.ReadableString );
                #endregion
                divisor *= 3;
            }

            FInt result = hostileStrengthTotal - friendlyStrengthTotal;
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n\t" ).Add( "result = hostileStrengthTotal - friendlyStrengthTotal => " ).Add( result.ReadableString );
            #endregion
            result = Mat.Max( result, FInt.Zero );
            #region Tracing
            if ( tracing ) tracingBuffer.Add( "\n\t" ).Add( "(final) result = Mat.Max( result, FInt.Zero ) => " ).Add( result.ReadableString );
            #endregion

            if ( tracing )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                tracingBuffer.ReturnToPool();
                tracingBuffer = null;
            }
            return result;
        }
        #endregion end GetDanger

        #region DoLongRangePlanning
        public void DoLongRangePlanning( ArcenLongTermIntermittentPlanningContextBase Context )
        {
            Faction myFaction = MySpecificSubfaction;
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();

            //This is currently only used by the Praetorians; hunter and wardens now use fireteams
            int debugStage = 0;
            Dictionary<Planet, GameCommand> routingCommandsByOrigin = Planet.GetTemporaryPlanetDictOfGameCommands( "AISubFactionCoreDataRoot-routingCommandsByOrigin", 10f );
            if ( routingCommandsByOrigin == null ) //blocked for teardown/shutdown; bail
            {
                pathingCacheData.ReturnToPool(); //release already-acquired pooled resource before bailing (finally not yet entered)
                return;
            }
            Dictionary<Planet, GameCommand> CampOnAdjacentPlanetsCommand = Planet.GetTemporaryPlanetDictOfGameCommands( "AISubFactionCoreDataRoot-CampOnAdjacentPlanetsCommand", 10f );
            if ( CampOnAdjacentPlanetsCommand == null ) //blocked for teardown/shutdown; bail
            {
                Planet.ReleaseTemporaryPlanetDictOfGameCommands( routingCommandsByOrigin ); //release already-acquired temps before bailing (finally not yet entered)
                pathingCacheData.ReturnToPool();
                return;
            }
            try
            {
                #region Tracing
                bool tracing;
                if ( this.IsHunter )
                    tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Hunter );
                if ( this.IsWarden )
                    tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Warden );
                else
                    tracing = this.tracing_shortTerm = Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Independents );
                ArcenCharacterBuffer tracingBuffer = tracing ? ArcenCharacterBuffer.GetFromPoolOrCreate( "AISubFac-DoLongRangePlanning-tracing", 10f ) : null;
                // if(faction.SpecialFactionData.IsConsideredWarden)
                //     tracing = true;
                if ( tracing ) tracingBuffer.Add( this.TracingName ).Add( " LongRangePlanning trace begins for faction " ).Add( myFaction.FactionIndex );
                #endregion

                #region find the king unit we're allied with
                debugStage = 10;
                GameEntity_Squad kingUnit = null;
                bool hasKingGoneMobile = false;
                foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( SpecialEntityType.AIKingCommandStation ) )
                {
                    if ( entity.GetFactionIndex_Safe() != myFaction.FactionIndexOfMyParentIfIHaveOne )
                        continue;
                    kingUnit = entity;
                    break;
                }
                if ( kingUnit == null )
                {
                    foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( SpecialEntityType.AIKingMobile ) )
                    {
                        if ( entity.GetFactionIndex_Safe() != myFaction.FactionIndexOfMyParentIfIHaveOne )
                            continue;
                        kingUnit = entity;
                        hasKingGoneMobile = true;
                        break;
                    }
                }
                debugStage = 20;
                if ( kingUnit == null )
                {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "The king is dead. This faction now stops doing anything" );
                    if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion
                    return;
                }
                debugStage = 30;
                Planet primaryDefensePlanet = kingUnit.Planet;
                #endregion

                FInt fleetStrength = FInt.Zero;
                foreach ( GameEntity_Squad entity in myFaction.Squads( EntityRollupType.MobileCombatants ) )
                {
                    fleetStrength += entity.GetStrengthOfSelfAndContents();
                }

                #region pick a target planet to camp out on
                if ( this.CurrentTargetPlanetIndex < 0 )
                {
                    //TEACHING_MOMENT: System.Threading.Interlocked.Exchange() can be used to set a variable of a basic type from many threads at once
                    //This is arguably NOT what is happening here, but another thread might be reading it, and this also does help with that a bit.
                    //This is, evidently, about 6x faster than using a traditional lock() statement, and doesn't seem to carry the same risk of deadlocks.
                    //In this particular method we could honestly probably get away without doing this, but this is LRP and and so we can spare the 6 nanoseconds this adds
                    //For details, see here: https://www.dotnetperls.com/interlocked  It's an interesting read, although not the article I originally found out about this tool from.
                    Interlocked.Exchange( ref this.CurrentTargetPlanetIndex, primaryDefensePlanet.Index );
                }

                Planet target = this.GetCurrentTargetPlanet( primaryDefensePlanet, (Int16)this.CurrentTargetPlanetIndex, Context, fleetStrength );
                debugStage = 40;
                if ( target == null ) // this really shouldn't happen, but in case the implementing code is broken
                {
                    debugStage = 50;
                    //Any time we change the value, we want to do it via Interlocked.  When reading it, we just read it like normal
                    Interlocked.Exchange( ref this.CurrentTargetPlanetIndex, -1 );

                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Error: GetCurrentTargetPlanet returned null, aborting trace" );
                    if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                    if ( tracing )
                    {
                        tracingBuffer.ReturnToPool();
                        tracingBuffer = null;
                    }
                    #endregion

                    return;
                }
                if ( this.CurrentTargetPlanetIndex != target.Index )
                {
                    debugStage = 60;
                    HasPlayedVoiceLineForThisPlanet = false; //we've just changed out planet

                    //TEACHING_MOMENT: Hey, we used to have a GameCommand here for setting the CurrentTargetPlanetIndex!
                    //That was really responsible of us, when we figured it would be used equally on the client and the host.
                    //In reality, the client will find out about the new CurrentTargetPlanetIndex within a second or two without a GameCommand,
                    //Because faction BaseInfo is all repeatedly shared to clients anyway.  But even more to the point,
                    //the CurrentTargetPlanetIndex is only used in LRP and host-only code, so the clients don't really have anything to DO with it.
                    //So let's save ourselves the trouble of sending a GameCommand that... does nothing of substance.

                    //Any time we change the value, we want to do it via Interlocked.  When reading it, we just read it like normal
                    Interlocked.Exchange( ref this.CurrentTargetPlanetIndex, target.Index );
                }
                #endregion
                #region route any units not on the target (or enroute to it) to it
                debugStage = 70;
                FInt presentFleetStrength = FInt.Zero;
                bool unitsShouldWaitToAttack = false;
                if ( target != primaryDefensePlanet ) //we don't wait if the king is under attack
                    unitsShouldWaitToAttack = this.GetShouldWaitToAttack( target, fleetStrength, out presentFleetStrength, Context );
                if ( hasKingGoneMobile && kingUnit.CurrentStateOfMatter.IsDefault )
                    unitsShouldWaitToAttack = false;

                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Units should wait to attack " + target.Name + ": " + unitsShouldWaitToAttack ).Add( ". total fleet: " + fleetStrength.IntValue + " present/nearby fleet: " + presentFleetStrength.IntValue );
                foreach ( GameEntity_Squad entity in myFaction.Squads( EntityRollupType.MobileCombatants ) )
                {
                    debugStage = 80;
                    bool subTracing = tracing && entity == GameEntity_Base.CurrentlyHoveredOver;
                    // if(faction.SpecialFactionData.IsConsideredWarden)
                    //     subTracing = true;
                    if ( entity.GetPlanetIndexSafe() == target.Index )
                    {
                        #region Tracing
                        if ( subTracing ) tracingBuffer.Add( "\n" ).Add( "skipping " ).Add( entity.TypeData.InternalName ).Add( " #" ).Add( entity.PrimaryKeyID ).Add( " on " ).Add( entity.GetPlanetName_Safe() ).Add( " because already on " ).Add( target.Name );
                        #endregion
                        continue;
                    }
                    debugStage = 82;
                    Planet origin = entity.Planet;
                    if ( unitsShouldWaitToAttack && origin.GetIsDirectlyLinkedTo( false, target ) )
                    {
                        #region Tracing
                        if ( subTracing ) tracingBuffer.Add( "\n" ).Add( "This unit is on " + origin.Name + " which is adjacent to " + target.Name + " but is waiting for the rest of the fleet" );
                        #endregion
                        ArcenPoint campingSpot = origin.GetCommandStationOrNull()?.WorldLocation ?? Engine_AIW2.Instance.CombatCenter;
                        int allowedDistance = origin.GravWellSize.DistanceScale_GravwellRadius / 2;
                        int currentDistance = entity.WorldLocation.GetDistanceTo( campingSpot, false );
                        if ( currentDistance <= allowedDistance )
                        {
                            continue;
                        }
                        if ( CampOnAdjacentPlanetsCommand[origin] == null )
                        {
                            GameCommand command = CampOnAdjacentPlanetsCommand[origin] = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_BaseAICamping], GameCommandSource.AnythingElse );
                            command.RelatedPoints.Add( campingSpot );
                        }
                        CampOnAdjacentPlanetsCommand[origin].RelatedEntityIDs.Add( entity.PrimaryKeyID );
                        continue;
                    }

                    if ( entity.CalculateFinalDestinationPlanetIndex_Safe() == target.Index && entity.CalculateNextHopHasARefuseToWaitOrder_Safe() && !unitsShouldWaitToAttack )
                    {
                        #region Tracing
                        if ( subTracing )
                        {
                            tracingBuffer.Add( "\n" ).Add( "skipping " ).Add( entity.TypeData.InternalName ).Add( " #" ).Add( entity.PrimaryKeyID ).Add( " on " ).Add( entity.GetPlanetName_Safe() ).Add( " because already heading to " ).Add( target.Name ).Add( " (" );
                            for ( int orderIndex = 0; orderIndex < entity.Orders.GetQueuedOrderCount(); orderIndex++ )
                            {
                                if ( orderIndex > 0 )
                                    tracingBuffer.Add( ", " );
                                EntityOrder order = entity.Orders.GetQueuedOrderAtIndex_OrNull( orderIndex );
                                if ( order.TypeData != null )
                                    order.WriteDescriptionTo( tracingBuffer );
                            }
                            tracingBuffer.Add( ")" );
                        }
                        #endregion
                        continue;
                    }
                    debugStage = 83;

                    debugStage = 84;
                    if ( routingCommandsByOrigin[origin] == null )
                    {
                        debugStage = 85;
                        if ( entity == null )
                        {
                            #region Tracing
                            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "LongRangePlanningData or similar is null!" );
                            #endregion
                            continue;
                        }
                        debugStage = 86;
                        Faction facOrNull = entity.GetFactionOrNull_Safe();
                        PathBetweenPlanetsForFaction pathCache = facOrNull == null ? null : PathingHelper.FindPathFreshOrFromCache( facOrNull, "AISubFactionLRP", origin, target, PathingMode.Default, Context, pathingCacheData );
                        debugStage = 87;
                        #region Tracing
                        if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Trying to path from " ).Add( origin.Name ).Add( " to " ).Add( target.Name ).Add( "; result.Count=" ).Add( pathCache == null ? 0 : pathCache.PathToReadOnly.Count );
                        #endregion
                        if ( pathCache == null || pathCache.PathToReadOnly.Count <= 0 )
                        {
                            #region Tracing
                            if ( tracing ) tracingBuffer.Add( "\n" ).Add( "No path found!" );
                            #endregion
                            continue;
                        }
                        debugStage = 88;
                        GameCommand command = routingCommandsByOrigin[origin] = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_AIIndFleet], GameCommandSource.AnythingElse );
                        command.RelatedString = "INDAI_GO";
                        command.RelatedBool = true; //refuse to wait
                        if ( unitsShouldWaitToAttack )
                        {
                            if ( pathCache.PathToReadOnly.Count == 1 )
                            {
                                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "We are adjacent to the target planet, but waiting for the rest of the fleet" );
                                continue;
                            }
                            else
                            {
                                if ( tracing ) tracingBuffer.Add( "\n" ).Add( "Routing to near the target planet to wait for the fleet" );
                                for ( int k = 0; k < pathCache.PathToReadOnly.Count - 1; k++ )
                                    command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                            }
                        }
                        else
                        {
                            //go directly to the planet
                            for ( int k = 0; k < pathCache.PathToReadOnly.Count; k++ )
                                command.RelatedIntegers.Add( pathCache.PathToReadOnly[k].Index );
                        }
                    }
                    debugStage = 89;
                    #region Tracing
                    if ( subTracing )
                    {
                        if ( unitsShouldWaitToAttack ) tracingBuffer.Add( "\n" ).Add( "putting " ).Add( entity.TypeData.InternalName ).Add( " #" ).Add( entity.PrimaryKeyID ).Add( " on list to go from " ).Add( origin.Name ).Add( " to near " ).Add( target.Name ).Add( " to wait for the fleet" );
                        else tracingBuffer.Add( "\n" ).Add( "putting " ).Add( entity.TypeData.InternalName ).Add( " #" ).Add( entity.PrimaryKeyID ).Add( " on list to go from " ).Add( origin.Name ).Add( " to " ).Add( target.Name );
                    }
                    #endregion
                    routingCommandsByOrigin[origin].RelatedEntityIDs.Add( entity.PrimaryKeyID );
                }
                debugStage = 90;
                foreach ( KeyValuePair<Planet, GameCommand> kv in routingCommandsByOrigin )
                {
                    debugStage = 100;
                    Planet origin = kv.Key;
                    GameCommand command = kv.Value;
                    World_AIW2.Instance.QueueGameCommand( this.MySpecificSubfaction, command, false );
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "command queued: from " ).Add( origin.Name ).Add( ". " ).Add( command.RelatedEntityIDs.Count ).Add( ":" ).Add( command.RelatedIntegers.Count ).Add( ". Eventual destination " ).Add( World_AIW2.Instance.GetPlanetByIndex( (Int16)command.RelatedIntegers.Last ).Name ).Add( " units should wait for the fleet before attacking: " ).Add( unitsShouldWaitToAttack );
                    #endregion
                }
                foreach ( KeyValuePair<Planet, GameCommand> kv in CampOnAdjacentPlanetsCommand )
                {
                    Planet planet = kv.Key;
                    GameCommand command = kv.Value;
                    World_AIW2.Instance.QueueGameCommand( this.MySpecificSubfaction, command, false );
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "camping command queued: from " ).Add( planet.Name ).Add( ". " ).Add( command.RelatedEntityIDs.Count ).Add( ":" ).Add( command.RelatedIntegers.Count ).Add( ". Are ordered to camp nearby" );
                    #endregion
                }
                #endregion
                debugStage = 110;
                #region if target planet not currently in combat,
                EnumIndexedArray<FactionStance, StrengthData_PlanetFaction_Stance> myFactionData = target.GetStanceDataForFaction( myFaction );
                int localStrength = myFactionData[FactionStance.Self].TotalStrengthIncludingNonMilitary + myFactionData[FactionStance.Friendly].TotalStrengthIncludingNonMilitary;
                if ( myFactionData[FactionStance.Hostile].TotalStrengthIncludingNonMilitary > 0 )
                {
                    PlanetFaction pFaction = target.GetPlanetFactionForFaction( myFaction );
                    Faction mostAnnoyingFaction = World_AIW2.Instance.GetFactionByIndex( pFaction.GetIndexOfMostAnnoyingFaction( Context ) );
                    if ( pFaction.Entities.SquadCount * 2 >= myFaction.GetTotalSquadCount() && //we have at least half our fleet here, so Major Battle
                         mostAnnoyingFaction != null && mostAnnoyingFaction == World_AIW2.Instance.GetLocalPlayerFactionOrNull() && //the faction we are fighting is the local player
                         localStrength >= myFactionData[FactionStance.Hostile].TotalStrengthIncludingNonMilitary && //lets only play the line if we think we're gonna win; dumb to taunt and then lose
                         !HasPlayedVoiceLineForThisPlanet &&  //we haven't already played the voice line for this planet
                         target.IntelLevel >= PlanetIntelLevel.CurrentlyWatched ) //the player can see what's going on
                    {
                        HasPlayedVoiceLineForThisPlanet = true;
                        //                    ArcenDebugging.ArcenDebugLogSingleLine(faction.ToString() + " about to play the message for 'Attacking' for planet " + target.Name +" local: "+ pFaction.Entities.Count + " total " + faction.Entities.Count + " annoying " + mostAnnoyingFaction.ToString(),  Verbosity.DoNotShow );
                        if ( this.IsWarden )
                            Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.WardenFleetArrives );
                        else if ( this.IsHunter && target.GetControllingFactionType() == FactionType.Player )
                            Engine_AIW2.Instance.PresentationLayer.PlaySoundByType( SoundPropagation.HostDirectedPlay, SFXItemType_NonPositional.HunterFleetAttacks ); //these lines seem to want the hunter fleet to be striking a player planet

                    }
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "skipping the intra-planet positioning and budget-conversion steps because " ).Add( target.Name ).Add( " is contested" );
                    #endregion
                }
                else if ( myFactionData[FactionStance.Self].TotalStrength <= 0 )
                {
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "skipping the intra-planet positioning and budget-conversion steps we don't have any strength on " ).Add( target.Name ).Add( " yet" );
                    #endregion
                }
                else
                {
                    #region position any fleet guardians there in a reasonable place
                    #region Tracing
                    if ( tracing ) tracingBuffer.Add( "\n" ).Add( "have forces on target, and there's no opposition, so checking my positioning" );
                    #endregion
                    debugStage = 120;
                    PlanetFaction planetFaction = target.GetPlanetFactionForFaction( myFaction );
                    ArcenPoint campingSpot = target.GetCommandStationOrNull()?.WorldLocation ?? Engine_AIW2.Instance.CombatCenter;
                    int allowedDistance = target.GravWellSize.DistanceScale_GravwellRadius / 5;
                    GameCommand campingCommand = null;
                    foreach ( GameEntity_Squad entity in planetFaction.Entities.Squads( EntityRollupType.MobileCombatants ) )
                    {
                        bool subTracing = tracing && entity == GameEntity_Base.CurrentlyHoveredOver;
                        int currentDistance = entity.WorldLocation.GetDistanceTo( campingSpot, false );
                        if ( currentDistance <= allowedDistance )
                        {
                            #region Tracing
                            if ( subTracing ) tracingBuffer.Add( "\n" ).Add( "skipping " ).Add( entity.TypeData.InternalName ).Add( " #" ).Add( entity.PrimaryKeyID ).Add( " because I'm already close enough to the camping spot" );
                            #endregion
                            continue;
                        }
                        int currentOrderDistance = entity.CalculateDestinationPoint_Safe().GetDistanceTo( campingSpot, false );
                        if ( currentOrderDistance <= allowedDistance )
                        {
                            #region Tracing
                            if ( subTracing ) tracingBuffer.Add( "\n" ).Add( "skipping " ).Add( entity.TypeData.InternalName ).Add( " #" ).Add( entity.PrimaryKeyID ).Add( " because my current destination point is already close enough to the camping spot" );
                            #endregion
                            continue;
                        }

                        if ( campingCommand == null )
                        {
                            campingCommand = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_BaseAICamping], GameCommandSource.AnythingElse );
                            campingCommand.RelatedPoints.Add( campingSpot );
                        }
                        #region Tracing
                        if ( subTracing ) tracingBuffer.Add( "\n" ).Add( "putting " ).Add( entity.TypeData.InternalName ).Add( " #" ).Add( entity.PrimaryKeyID ).Add( " on list for a move order to the camping spot" );
                        #endregion
                        campingCommand.RelatedEntityIDs.Add( entity.PrimaryKeyID );
                    }
                    if ( campingCommand != null )
                        World_AIW2.Instance.QueueGameCommand( this.MySpecificSubfaction, campingCommand, false );
                    #endregion
                }
                #endregion
                #region Tracing
                if ( tracing ) tracingBuffer.Add( "\n" ).Add( this.TracingName ).Add( " Long Range Planning trace ends" );
                if ( tracing ) ArcenDebugging.ArcenDebugLogSingleLine( tracingBuffer.ToString(), Verbosity.DoNotShow );
                if ( tracing )
                {
                    tracingBuffer.ReturnToPool();
                    tracingBuffer = null;
                }
                #endregion

            }
            catch ( System.Threading.ThreadAbortException ) { } //simply return
            catch ( Exception e )
            {
                if ( !ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() ) //only log if not tearing things down
                    ArcenDebugging.ArcenDebugLog( "Exception in Independent AI Fleet SubType stage " + debugStage + " \n" + e, Verbosity.ShowAsError );
            }
            finally
            {
                pathingCacheData.ReturnToPool();
                Planet.ReleaseTemporaryPlanetDictOfGameCommands( routingCommandsByOrigin );
                Planet.ReleaseTemporaryPlanetDictOfGameCommands( CampOnAdjacentPlanetsCommand );
            }
        }
        #endregion end DoLongRangePlanning
    }
}
