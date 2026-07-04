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
                        Buffer.Add( "This structure will mark up in " ).Add( data.TimeTillmarkUp.ToString(), color ).Add( ". " );
                    }
                    if ( data.TimeTillSpawnNextConstructor > 0 )
                    {
                        string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( data.TimeTillSpawnNextConstructor );
                        Buffer.Add( "This structure is able to build new fortifications in " ).Add( data.TimeTillSpawnNextConstructor.ToString(), color ).Add( ". " );
                    }
                    if ( debug )
                        Buffer.Add( "This structure's available metal is " ).Add( data.MetalStored, "a1a1ff" ).Add( ". " );
                    Buffer.Add( "This " ).Add( RelatedEntityTypeData.GetDisplayName() ).Add( " has the following inside: " );
                    Buffer.Add( data.ShipsInside_ForUI );

                    if ( data.PlanetCastleWantsToHelp != null )
                        Buffer.Add( "This unit would like to help defend " ).Add( data.PlanetCastleWantsToHelp.Name, "a1ffa1" ).Add( ". " );
                }
                
                if ( data.DefenseMode && RelatedEntityTypeData.IsMobileCombatant )
                {
                    GameEntity_Squad castle = data.HomeCastle.GetSquad();
                    if ( castle != null )
                    {
                        Buffer.Add( "This ship is dispatched from the " ).Add( castle.TypeData.GetDisplayName(), "a1ffa1" ).Add( " on " ).Add( castle.Planet.Name, "ffa1a1" );
                        TemplarPerUnitBaseInfo castleData = castle.TryGetExternalBaseInfoAs<TemplarPerUnitBaseInfo>();
                        Planet defensePlanet = null;
                        if ( castleData != null )
                            defensePlanet = castleData.PlanetCastleWantsToHelp;
                        if ( defensePlanet != null )
                            Buffer.Add( " to defend " ).Add( defensePlanet.Name, "ffa1a1" ).Add( ". " );
                        else
                            Buffer.Add( ". " );

                    }
                }
                if ( RelatedEntityTypeData.GetHasTag( "TemplarConstructor" ) )
                {
                    Planet destPlanet = World_AIW2.Instance.GetPlanetByIndex( (short)data.PlanetIdx );
                    if ( destPlanet != null && data.UnitToBuild != null )
                    {
                        Buffer.Add( "This constructor will build a " ).Add( data.UnitToBuild.GetDisplayName(), "a1ffa1" ).Add( " on " ).Add( destPlanet.Name, "a1a1ff" ).Add( "\n" );
                    }
                    else
                    {
                        Buffer.Add( "This constructor has no build target?!\n" );
                    }
                }
                else if ( RelatedEntityTypeData.IsCombatant && RelatedEntityTypeData.IsMobileCombatant )
                    Buffer.Add( "This ship is dispatched to attack the players. " );

                if ( data.StrengthRalliedToWave > 0 && debug )
                {
                    Buffer.Add( "This ship has rallied " ).Add( data.StrengthRalliedToWave, "a1ffa1" ).Add(" strength.");
                }
            }
            catch ( Exception ) { }

            return;
        }
    }
}
