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
            buffer.Add( "摧毁每一个 " ).Add( "AI 霸主", ObjectiveColors.AIP ).Add( " 即可结束战争，而至少有一个仍然隐藏着。" );
            buffer.Add( "\n\n你知道它在那里，但不知道在哪里；甚至不知道还剩多少。" ).Add( "进一步侦察", ObjectiveColors.Hint ).Add( " 以找到它。" );
            buffer.Add( "\n\n当你找到时：清除相邻 " ).Add( "堡垒星球", BastionWorldColor )
                .Add( " 上的 " ).Add( "严厉守卫哨站", ObjectiveColors.Keyword ).Add( " 以使霸主 " ).Add( "脆弱", ObjectiveColors.Reward ).Add( "。你还将面对 AI 强大的防御 " ).Add( "禁卫军", PraetorianGuardColor ).Add( "。" );
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
            buffer.Add( "摧毁每一个 " ).Add( "AI 霸主", aiColor ).Add( " 即可结束战争。此霸主掌控着 " )
                .Add( king.GetPlanetName_Safe(), king.Planet.GetControllingFaction().FactionCenterColor.ColorHexBrighter ).Add( "。" );

            buffer.Add( "\n\n它是 " ).Add( "无敌的", ObjectiveColors.AIP ).Add( "，直到你拆除其外部防御：" );

            buffer.Add( "\n\n" );
            GameEntityTypeData dgp = KingObjectivesGenerator.GetDireGuardPostRepresentative();
            if ( dgp != null )
                buffer.AddShipIconInline( dgp, king.PlanetFaction.Faction, TextStyle.Ship_Sprite_Ency ).Add( " " );
            buffer.Add( "严厉守卫哨站", ObjectiveColors.Keyword ).Add( "\n\n" );
            buffer.Add( "摧毁其星球边界上的 " ).Add( "堡垒星球", KingObjectivesGenerator.BastionWorldColor )
                .Add( " 上的这些以使霸主 " ).Add( "脆弱", ObjectiveColors.Reward ).Add( "。" );

            buffer.Add( "\n\n你还必须面对 " ).Add( "禁卫军", KingObjectivesGenerator.PraetorianGuardColor )
                .Add( "，他们会誓死保卫霸主。" );

            buffer.Add( "\n\n" ).Add( "点击此处前往。", ObjectiveColors.Hint );
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
