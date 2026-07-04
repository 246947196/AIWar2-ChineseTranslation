using System;

using System.Linq;
using System.Text;
using Arcen.Universal;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public static class CentralVars
    {
        public static bool GetShouldNotRunGameStyleLogic()
        {
            if ( Engine_AIW2.Instance.CurrentGameViewMode == GameViewMode.ModalMenu )
                return true;
            if ( World_AIW2.Instance == null )
                return true;
            if ( ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() )
                return true; //we're shutting things off, so yeah don't run game-style logic
            return false;
        }

        /*
         * Use the below flags to turn off parts of the sim that use different amounts of weight,
         * while still letting the game "run" in the sense of turning over internal turns, etc.
         * Depending on what all you turn off, it may not actually do anything, but the point is
         * to find out where the performance hit is.
         * */
        
        //main sim itself
        public static bool DEBUG_TURN_OFF_MAIN_SIM_EXECUTION = false;

        public static bool DEBUG_TURN_OFF_WORLD_STEP_LOGIC = false;
        public static bool DEBUG_TURN_OFF_RE_EVAL_UNIT_ORDERS = false;
        public static bool DEBUG_TURN_OFF_SYTEM_STEP = false;
        public static bool DEBUG_TURN_OFF_WORMHOLE_TRAVERSAL = false;
        public static bool DEBUG_TURN_OFF_COMBAT_PER_SECOND_PLANET_LOGIC = false;
        public static bool DEBUG_TURN_OFF_COPY_SHORT_TERM_FRAME_PLANNING_TO_SIM = false;

        //main sim itself - combat step sections
        public static bool DEBUG_TURN_OFF_COMBAT_STEP_AT_PLANETS = false;
        public static bool DEBUG_TURN_OFF_COMBAT_CENTRAL_STUFF_PER_STEP_LOGIC = false;
        public static bool DEBUG_TURN_OFF_COMBAT_NATURAL_OBJECT_PER_STEP_LOGIC = false;
        public static bool DEBUG_TURN_OFF_COMBAT_SHIP_PER_STEP_LOGIC = false;
        public static bool DEBUG_TURN_OFF_COMBAT_SHOT_MOVEMENT = false;


        //short term planning context
        public static bool DEBUG_TURN_OFF_SHORT_TERM_PLANNNING_ALL = false;
        public static bool DEBUG_TURN_OFF_STRENGTH_COUNTING = false;
        public static bool DEBUG_TURN_OFF_MOVEMENT_PLANNING = false;
        public static bool DEBUG_TURN_OFF_METAL_FLOW_PLAN = false;
        public static bool DEBUG_TURN_OFF_PROTECTION_PLAN = false;
        public static bool DEBUG_TURN_OFF_AREABOOST_PLAN = false;
        public static bool DEBUG_TURN_OFF_TACHYON_PLAN = false;
        public static bool DEBUG_TURN_OFF_TRACTOR_PLAN = false;
        public static bool DEBUG_TURN_OFF_GRAVITY_PLAN = false;
        public static bool DEBUG_TURN_OFF_SHORT_TERM_DISABLED_CHECKS = false;

        //long term intermittent planning
        public static bool DEBUG_TURN_OFF_LONG_TERM_INTERMITTENT = false;

        //long term continuous planning
        public static bool DEBUG_TURN_OFF_DECOL_PLAN = false;
        public static bool DEBUG_TURN_OFF_TARGET_PLAN = false;
        public static bool DEBUG_TURN_OFF_STACK_PLAN = false;
        public static bool DEBUG_TURN_OFF_PER_SEC_NON_SIM_PLAN = false;
    }
}
