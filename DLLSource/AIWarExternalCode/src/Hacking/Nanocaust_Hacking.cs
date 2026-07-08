using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class Nanocaust_Hive_Hacking : BaseHackingImplementation
    {
        public override Hackable GetCanBeHacked( GameEntity_Squad Target, GameEntity_Squad HackerOrNull, Planet planet, Faction HackerFaction, HackingType Type, string RelatedStringOrNull, int RelatedIntOrNull, out string RejectionReasonDescription )
        {
            if ( Target.TypeData.GetHasTag( "NanobotHackedHive" ) )
            {
                RejectionReasonDescription = "纳米虫群已被黑客入侵。";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( HackerOrNull != null &&
                 HackerOrNull.PlanetFaction.Faction.GetIsFriendlyTowards( Target.PlanetFaction.Faction ) )
            {
                RejectionReasonDescription = "此纳米虫群已是你的盟友。";
                return Hackable.NeverCanBeHacked_Hide;
            }
            if ( !Target.TypeData.GetHasTag( "NanobotHive" ) )
            {
                RejectionReasonDescription = "目标不是纳米机器人蜂巢。";
                return Hackable.NeverCanBeHacked_Hide;
            }
            return base.GetCanBeHacked( Target, HackerOrNull, planet, HackerFaction, Type, RelatedStringOrNull, RelatedIntOrNull, out RejectionReasonDescription );
        }

        public override void DoOneSecondOfHackingLogic_HackSpecificLogic( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            GameEntityTypeData aberrationData = GameEntityTypeDataTable.Instance.GetRowByName( "Aberration" );
            GameEntityTypeData abominationData = GameEntityTypeDataTable.Instance.GetRowByName( "Abomination" );

            PlanetFaction cFaction = Target.PlanetFaction;
            //          ArcenPoint center = Engine_AIW2.Instance.CombatCenter;
            ArcenPoint placementPoint = Target.neverWriteDirectly_worldLocation;
            int numToCreate = 1;
            /* How many ships to create? */
            if ( Hacker.ActiveHack_DurationThusFar % 10 == 0 )
                numToCreate = 4;

            /* Create ships to fight hackers */
            for ( int i = 0; i < numToCreate; i += 2 )
            {
                //all good, part of main sim
                GameEntity_Squad entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( cFaction, aberrationData, 0, cFaction.FleetUsedAtPlanet, 0, placementPoint, Context, "Hacking-NanocaustReaction" );
                if ( entity != null )
                {
                    entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                    entity.ShouldNotBeConsideredAsThreatToHumanTeam = true; //default to not counting this as threat
                }

                entity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( cFaction, abominationData, 0, cFaction.FleetUsedAtPlanet, 0, placementPoint, Context, "Hacking-NanocaustReaction" );
                if ( entity != null )
                {
                    entity.Orders.SetBehaviorDirectlyInSim( EntityBehaviorType.Attacker_Full );
                    entity.ShouldNotBeConsideredAsThreatToHumanTeam = true; //default to not counting this as threat
                }
            }
        }

        public override bool DoSuccessfulCompletionLogic_Extra( GameEntity_Squad Target, Planet planet, GameEntity_Squad Hacker, ArcenHostOnlySimContext Context, HackingType type, HackingEvent Event )
        {
            //Set the toggle in NanocaustFactionBaseInfo

            //it should probably cost hacking points. It does hurt the AI.
            //Lore can claim that the AI is monitoring the Nanobot network and
            //notices the trick you just used to get access to the Nanobots
            NanocaustFactionBaseInfo nanoBaseInfo = Target.TryGetFactionBaseInfoOrNullAs_Safe<NanocaustFactionBaseInfo>();
            if ( nanoBaseInfo == null )
            {
                if ( ArcenNetworkAuthority.DesiredStatus != DesiredMultiplayerStatus.Client )
                    ArcenDebugging.ArcenDebugLog( "Could not convert " +
                        Target.GetFactionBaseInfoOrNull_Safe().GetType().ToString() +
                        " to type NanocaustFactionBaseInfo, so hasBeenHacked was not set to true.", Verbosity.ShowAsError );
            }
            else
            {
                nanoBaseInfo.hasBeenHacked = true;
            }

            GameEntityTypeData hackedHiveData = GameEntityTypeDataTable.Instance.GetRowByName( "NanobotCenter_Hacked_Hive" );
            PlanetFaction cFaction = Target.PlanetFaction;

            //the location of the thing that is about to die, so we can put the replaced thing back where it was
            ArcenPoint placementPoint = Target.WorldLocation;
            Target.Die( Context, true );

            GameEntity_Squad hackedHive = GameEntity_Squad.CreateNew_ReturnNullIfMPClient( cFaction, hackedHiveData, 0, cFaction.FleetUsedAtPlanet, 0, placementPoint, Context, "Hacking-NanocaustHackedHive" );
            if ( ArcenNetworkAuthority.GetIsHostMode() )
            {
                SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>( "ShipGeneralFocus" );
                if ( chatHandlerOrNull != null )
                    chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create( hackedHive );

                World_AIW2.Instance.QueueChatMessageOrCommand( Target.StartFactionColourForLog_Safe() + "纳米虫群蜂巢</color> 已在 " +
                    Target.GetPlanetName_Safe() + " 上被黑客入侵", ChatType.LogToCentralChat, "ArkChiefOfStaff_NanocaustHacked", chatHandlerOrNull );
            }
            return true;
        }
    }
}
