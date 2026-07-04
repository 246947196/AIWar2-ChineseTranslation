using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class TemplarAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            try
            {
                bool debug = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" ) || (Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Templar ));
                if ( RelatedEntityTypeData == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "No type data?", Verbosity.DoNotShow );
                    return;
                }

                TemplarPerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<TemplarPerUnitBaseInfo>();
                if ( data == null )
                {
                    //this is valid, if the AI has templar units (which is allowed)
                    return;
                }
                Faction faction = RelatedEntityOrNull.GetFactionOrNull_Safe();
                TemplarFactionBaseInfo globaldata = faction.TryGetExternalBaseInfoAs<TemplarFactionBaseInfo>();
                if ( RelatedEntityTypeData.GetHasTag( "TemplarRift" ) )
                {
                    //Buffer.Add( "This structure is a rift!. " );
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag( "TemplarPrimaryDefensiveStructure" ) )
                {
                    if ( data.TimeTillmarkUp > 0 )
                    {
                        string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.TimeTillmarkUp );
                        Buffer.Add( "此结构将在 " ).Add( data.TimeTillmarkUp.ToString(), color ).Add( " 秒后升级。" );
                    }
                    if ( data.TimeTillSpawnNextConstructor > 0 )
                    {
                        string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.TimeTillSpawnNextConstructor );
                        Buffer.Add( "此结构能够在 " ).Add( data.TimeTillSpawnNextConstructor.ToString(), color ).Add( " 秒后建造新的防御工事。" );
                    }
                    if ( debug )
                        Buffer.Add( "此结构可用金属为 " ).Add( data.MetalStored, "a1a1ff" ).Add( "。" );
                    Buffer.Add( "此 " ).Add( RelatedEntityTypeData.GetDisplayName() ).Add( " 内部有：" );
                    Buffer.Add( data.ShipsInside_ForUI );

                    if ( data.PlanetCastleWantsToHelp != null )
                        Buffer.Add( "此单位希望协助防御 " ).Add( data.PlanetCastleWantsToHelp.Name, "a1ffa1" ).Add( "。" );
                }
                
                if ( data.DefenseMode && RelatedEntityTypeData.IsMobileCombatant )
                {
                    GameEntity_Squad castle = data.HomeCastle.GetSquad();
                    if ( castle != null )
                    {
                        Buffer.Add( "此舰船从 " ).Add( castle.TypeData.GetDisplayName(), "a1ffa1" ).Add( " 调度，在 " ).Add( castle.Planet.Name, "ffa1a1" );
                        TemplarPerUnitBaseInfo castleData = castle.TryGetExternalBaseInfoAs<TemplarPerUnitBaseInfo>();
                        Planet defensePlanet = null;
                        if ( castleData != null )
                            defensePlanet = castleData.PlanetCastleWantsToHelp;
                        if ( defensePlanet != null )
                            Buffer.Add( " 防御 " ).Add( defensePlanet.Name, "ffa1a1" ).Add( "。" );
                        else
                            Buffer.Add( ". " );

                    }
                }
                if ( RelatedEntityTypeData.GetHasTag( "TemplarConstructor" ) )
                {
                    Planet destPlanet = World_AIW2.Instance.GetPlanetByIndex( (short)data.PlanetIdx );
                    if ( destPlanet != null && data.UnitToBuild != null )
                    {
                        Buffer.Add( "此建造者将在 " ).Add( destPlanet.Name, "a1a1ff" ).Add( " 建造 " ).Add( data.UnitToBuild.GetDisplayName(), "a1ffa1" ).Add( "\n" );
                    }
                    else
                    {
                        Buffer.Add( "此建造者没有建造目标？！\n" );
                    }
                }
                else if ( RelatedEntityTypeData.IsCombatant && RelatedEntityTypeData.IsMobileCombatant )
                    Buffer.Add( "此舰船被调度攻击玩家。" );

                if ( data.StrengthRalliedToWave > 0 && debug )
                {
                    Buffer.Add( "此舰船已集结 " ).Add( data.StrengthRalliedToWave, "a1ffa1" ).Add( " 战力。" );
                }
            }
            catch ( Exception ) { }

            return;
        }
    }
}
