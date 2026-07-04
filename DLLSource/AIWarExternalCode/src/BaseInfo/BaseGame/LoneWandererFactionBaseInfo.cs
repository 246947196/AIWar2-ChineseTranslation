using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public abstract class LoneWandererFactionBaseInfo : ExternalFactionBaseInfoRoot
    {
        //serialized
        public int TimeLastExisted;

        //not serialized
        public readonly DoubleBufferedList<SafeSquadWrapper> LoneWanderers = DoubleBufferedList<SafeSquadWrapper>.Create_WillNeverBeGCed( 10, "LoneWanderers" );

        //constants
        public abstract int RespawnTime { get; }
        public abstract string LoneUnitTag { get; }
        public bool PlayerSawUnitDie = false;

        public LoneWandererFactionBaseInfo()
        {
            Cleanup();
        }

        //TEACHING_MOMENT: (Sealed methods and abstract submethods)
        //By sealing the usual methods, we can be sure that no sub-classes can override them.
        //if a child class were to override Cleanup(), then we would have to trust that it would
        //remember to call base.Cleanup() inside itself.  That's trust I don't care to extend, sorry -- not even to myself, over enough time.
        //If someone forgets, then we just wind up calling the child Cleanup and not calling this parent Cleanup, and we have a very invisible leak.
        //So to solve that, we take control from THIS class.  We seal the regular Cleanup method,
        //and then we declare a new abstract class that any children must implement, and WE call THAT.
        //My short term memory works just fine, and all the code is right in front of us here, so I'm unlikely to make a mistake.
        //And for any theoretical child classes that get added in the future, it's pretty hard to screw it up.  They could try calling Cleanup()
        //from inside SubCleanup(), and they'd get themselves a nice infinite loop lockup.  But short of that, they can't undo what I have decreed here.
        //This is a good way to protect yourself even from yourself, let along unfamiliar coders, when you're designing parent and child classes.
        protected sealed override void Cleanup()
        {
            this.TimeLastExisted = 0;

            this.LoneWanderers.Clear();

            this.SubCleanup();
        }

        protected abstract void SubCleanup();

        public sealed override void SerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.AddGameSecond_PosDef0( MetaData, this.TimeLastExisted, "TimeLastExisted" );
            this.SubSerializeFactionTo( MetaData, Buffer, SerializationCmdType );
        }
        protected abstract void SubSerializeFactionTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType );

        public sealed override void DeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.TimeLastExisted = Buffer.ReadGameSecond_PosDef0( MetaData, "TimeLastExisted" );
            this.SubDeserializeFactionIntoSelf( MetaData, Buffer, SerializationCmdType );
        }
        protected abstract void SubDeserializeFactionIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType );

        public sealed override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context )
        {
            //TEACHING_MOMENT: (DoubleBufferedList<> usage)
            //The DoubleBufferedList<> is actually two lists.  One that is in use, and one that is being built.
            //This lets the UI, long range planning threads, and whatever else keep using a stable display list for a long while.
            //This prevents UI flicker (like double-buffering frames!), and it also prevents exceptions on
            //long range planning threads, and whatever other background threads, unless they do something exceptionally stupid.
            //Let's talk through the timing:
            //Step 1: we call ClearConstructionListForStartingConstruction().  This clears the CONSTRUCTION list (not the one that is used for display or reading on other threads).
            //Notice what method we are in? "DoPerSecondLogic"  That means it has been one gamesecond (so as little as 200ms with frame acceleration) since we were last here at step 1.
            //This is important.  No other method should be doing anything with this list -- the display part -- that lasts more than 200ms, anyway.
            //It might repeatedly get the "current display list" over a span of several seconds, but at some point the one it gets is a new one.  Tricky us.
            //Anyway, if a background thread DOES have a really longstanding hold on the former display list, which is now our construction list,
            //then us calling ClearConstructionListForStartingConstruction() here will cause an exception on the other thread.  And that other thread can have its code fixed.
            LoneWanderers.ClearConstructionListForStartingConstruction();
            //Step 2: we fill the construction list.  Honestly, it doesn't matter how long this takes us.
            //During this entire step, any other thread is just happily showing the display list, and there's no risk
            //of them accidentally somehow being affected by what we're doing here.
            foreach ( GameEntity_Squad entity in this.AttachedFaction.Squads( this.LoneUnitTag ) )
            {
                LoneWanderers.AddToConstructionList( entity );
                GivePerStanceStanceOrdersToFoundWanderers( entity, Context );
            }
            //Step 3: we finished filling the construction list, and so now we flip this to active.
            //Please note!  This is NOT a risk for any other threads.  Let's imagine that another thread does in fact
            //have a reference to the old-display list, which has now been flipped into the construction slot.
            //That's okay!  We haven't touched that list yet.  We won't touch it untilw e get back to Step 1 and do our clear.
            //In the meantime, those other threads are using a DIRECT reference to the old display list for a little while, then
            //stopping use of that and getting a new reference to the display list... which will, from now on, be the NEW display list.
            //It's highly unlikely that we'd need a single copy of the the display list (to do operations on it) for more than a few milliseconds.
            //And, as noted up in Step 1, we have anywhere from a 200ms to a 1000ms window before there's a problem.
            //Even if another thread takes 4 realtime seconds to do its work, and is getting the display list off and on,
            //after Step 3 here, where we do the switch, it will be using the new display list instead of the old one.
            //This is the chief reason that Step 1 has to be there and can't be part of Step 3.  We need that gap.
            LoneWanderers.SwitchConstructionToDisplay();

            //Hey, there's a Count on our double buffer list!  That's always pulling from the current display list, whatever that is.
            if ( LoneWanderers.Count > 0 )
                this.TimeLastExisted = World_AIW2.Instance.GameSecond;

            this.SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( Context );
        }
        protected abstract void SubDoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost( ArcenClientOrHostSimContextCore Context );

        protected abstract void GivePerStanceStanceOrdersToFoundWanderers( GameEntity_Squad entity, ArcenClientOrHostSimContextCore Context );
    }
}
