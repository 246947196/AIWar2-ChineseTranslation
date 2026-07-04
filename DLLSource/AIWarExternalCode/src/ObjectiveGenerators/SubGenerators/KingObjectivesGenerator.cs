using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public static class KingObjectivesGenerator
    {
        /// <summary>
        /// How many AI kings are left alive?
        /// </summary>
        public static void CheckForKingObjectives_BackgroundThread_ClientOrHost()
        {
            try
            {
                bool hasDoneOneForOnesCannotFind = false;
                foreach ( GameEntity_Squad king in World_AIW2.Instance.Squads( EntityRollupType.KingUnitsOnly ) )
                {
                        if ( king.TypeData.SpecialType != SpecialEntityType.AIKingCommandStation &&
                            king.TypeData.SpecialType != SpecialEntityType.AIKingMobile )
                            continue;

                        if ( king.Planet.IntelLevel > PlanetIntelLevel.Unexplored )
                        {
                            #region Tell Me To Kill The One I Can See
                            ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                            objective.SetHook( "DestroyEnemyKing" );
                            objective.DisplayNameBase = "Destroy ";
                            objective.RelatedEntity1 = king;
                            ObjectiveCategory.AddActualObjective( objective );
                            #endregion
                        }
                        else
                        {
                            #region Tell Me To Find The One I Cannot See
                            if ( !hasDoneOneForOnesCannotFind )
                            {
                                hasDoneOneForOnesCannotFind = true;
                                ActualObjective objective = ActualObjective.GetFromPoolOrCreate();
                                objective.SetHook( "FindEnemyKing" );
                                ObjectiveCategory.AddActualObjective( objective );
                            }
                            #endregion
                        }

                    }
            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLogSingleLine( "Error in KingObjectivesGenerator generation: " + e, Verbosity.ShowAsError );
            }
        }

        // Thematic accent colors for the Overlord tooltip (place / martial), kept as literals like
        // other faction/category accents rather than the semantic palette.
        internal const string BastionWorldColor = "ffb060";
        internal const string PraetorianGuardColor = "ff8866";

        // A representative Dire Guard Post type, used only for its icon — the dire guard posts that
        // ring an Overlord are a family of weapon variants, so any one stands in for the group.
        private static GameEntityTypeData direGuardPostRepresentative;
        private static bool direGuardPostLookupDone;
        internal static GameEntityTypeData GetDireGuardPostRepresentative()
        {
            if ( !direGuardPostLookupDone )
            {
                direGuardPostLookupDone = true;
                for ( int i = 0; i < GameEntityTypeDataTable.Instance.Rows.Count; i++ )
                {
                    GameEntityTypeData row = GameEntityTypeDataTable.Instance.Rows[i];
                    if ( row != null && row.SpecialType == SpecialEntityType.DireGuardPost )
                    {
                        direGuardPostRepresentative = row;
                        break;
                    }
                }
            }
            return direGuardPostRepresentative;
        }

        // Shared "you haven't found it yet" copy, used both for the dedicated find objective and the
        // null-entity case of the destroy objective.
        internal static void AppendFindOverlordText( ArcenDoubleCharacterBuffer buffer )
        {
            buffer.Add( "Destroying every " ).Add( "AI Overlord", ObjectiveColors.AIP ).Add( " ends the war, and at least one is still hidden from you." );
            buffer.Add( "\n\nYou know it is out there, but not where; or even how many remain. " ).Add( "Scout further", ObjectiveColors.Hint ).Add( " to find it." );
            buffer.Add( "\n\nWhen you do: clear the " ).Add( "Dire Guard Posts", ObjectiveColors.Keyword ).Add( " on the adjacent " ).Add( "Bastion worlds", BastionWorldColor )
                .Add( " to make the Overlord " ).Add( "vulnerable", ObjectiveColors.Reward ).Add( ". You will also be opposed by the AI's powerful defensive " ).Add( "Praetorian Guard", PraetorianGuardColor ).Add( "." );
        }
    }

    public class DestroyEnemyKing : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            GameEntity_Squad king = Objective.RelatedEntity1;
            if ( king == null )
            {
                KingObjectivesGenerator.AppendFindOverlordText( buffer );
                return;
            }

            string aiColor = king.GetFactionCenterColorHexBrighter_Safe();
            buffer.AddObjectiveEntityHeader( king, aiColor );
            buffer.Add( "Destroying every " ).Add( "AI Overlord", aiColor ).Add( " ends the war. This one holds " )
                .Add( king.GetPlanetName_Safe(), king.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( "." );

            buffer.Add( "\n\nIt is " ).Add( "invulnerable", ObjectiveColors.AIP ).Add( " until you tear down its outer defenses:" );

            buffer.Add( "\n\n" );
            GameEntityTypeData dgp = KingObjectivesGenerator.GetDireGuardPostRepresentative();
            if ( dgp != null )
                buffer.AddShipIconInline( dgp, king.PlanetFaction.Faction, TextStyle.Ship_Sprite_Ency ).Add( " " );
            buffer.Add( "Dire Guard Posts", ObjectiveColors.Keyword ).Add( "\n\n" );
            buffer.Add( "Destroy these on the " ).Add( "Bastion worlds", KingObjectivesGenerator.BastionWorldColor )
                .Add( " bordering its planet to make the Overlord " ).Add( "vulnerable", ObjectiveColors.Reward ).Add( "." );

            buffer.Add( "\n\nYou must also face the " ).Add( "Praetorian Guard", KingObjectivesGenerator.PraetorianGuardColor )
                .Add( " that will defend the Overlord to the death." );

            buffer.Add( "\n\n" ).Add( "Click here to go there now.", ObjectiveColors.Hint );
        }
    }

    public class FindEnemyKing : IObjectiveHookManager
    {
        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public MouseHandlingResult ClickHandler( ActualObjective Objective )
        {
            return MouseHandlingResult.None;
        }

        public void TooltipHandler( ArcenDoubleCharacterBuffer buffer, ActualObjective Objective )
        {
            KingObjectivesGenerator.AppendFindOverlordText( buffer );
        }
    }
}
