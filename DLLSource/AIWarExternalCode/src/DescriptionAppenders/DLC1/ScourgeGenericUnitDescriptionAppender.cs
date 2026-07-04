using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ScourgeGenericUnitDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            int debugCode = 0;
            try{
                bool debug = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" ) || (Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Scourge ));
                if ( RelatedEntityOrNull == null || RelatedEntityOrNull.TypeData.GetHasTag( "WarpingInScourgeFortress" ) || RelatedEntityOrNull.TypeData.GetHasTag( "WarpingInScourgeArmory" ) || RelatedEntityOrNull.TypeData.GetHasTag( "WarpingInScourgeSpawner" ) ) //no bonus info about warping in units
                    return;
                debugCode = 100;
                ScourgePerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<ScourgePerUnitBaseInfo>();
                if ( data == null )
                    return;
                Faction facOrNull = RelatedEntityOrNull.GetFactionOrNull_Safe();
                if ( facOrNull == null )
                {
                    Buffer.Add( "Please unpause the game to view additional information" );
                    return;
                }
                if ( RelatedEntityTypeData.KillsToTriggerTransformation > 0 )
                {
                    debugCode = 140;
                    //for the scourge infused empire
                    if ( RelatedEntityTypeData.TransformAfterKills == null )
                        throw new Exception("Nothing is given to transform into");
                    Buffer.Add(" This fleet has killed ").Add( data.UnitsKilled, "a1ffa1" ).Add(" units; after it kills ").Add( RelatedEntityTypeData.KillsToTriggerTransformation, "ffa1a1" ).Add(" then we will transform into ").Add(RelatedEntityTypeData.TransformAfterKills.GetDisplayName()).Add(". ");
                    return;
                }
                if ( facOrNull.Type == FactionType.Player)
                    return; //if this is owned by a player, it's a flagship so bail out after the KillsToTriggerTransformation code
                debugCode = 200;
                Fireteam team = null;
                bool isVassal = false;
                if ( facOrNull.SpecialFactionData.InternalName == "ScourgeVassal" )
                {
                    debugCode = 210;
                    ScourgeVassalFactionBaseInfo gdata = facOrNull.TryGetExternalBaseInfoAs<ScourgeVassalFactionBaseInfo>();
                    team = FireteamBaseUtility.GetFireteamById( gdata.Teams, RelatedEntityOrNull.FireteamId );
                    isVassal = true;
                }
                else
                {
                    debugCode = 220;
                    ScourgeFactionBaseInfo gdata = facOrNull.TryGetExternalBaseInfoAs<ScourgeFactionBaseInfo>();
                    team = FireteamBaseUtility.GetFireteamById( gdata.Teams, RelatedEntityOrNull.FireteamId );
                }
                debugCode = 230;
                if ( RelatedEntityTypeData.GetHasTag("ScourgeSeed"))
                    return;
                if ( RelatedEntityTypeData.GetHasTag("ScourgeFlower"))
                {

                    debugCode = 240;
                    ScourgeVassalFactionBaseInfo gdata = facOrNull.TryGetExternalBaseInfoAs<ScourgeVassalFactionBaseInfo>();
                    int transformTime = RelatedEntityOrNull.GameSecondCreated + gdata.Difficulty.FlowerGrowTime -  World_AIW2.Instance.GameSecond;
                    Buffer.Add("This flower will transform in ").Add(transformTime.ToString(), ArcenExternalUIUtilities.GetColorForNomadMoveTime(transformTime) ).Add(" seconds.");
                    return;
                }
                debugCode = 300;
                Buffer.Add( "This is a " );
                if ( RelatedEntityOrNull.TypeData.IsMobile )
                    Buffer.Add( "unit" );
                else
                    Buffer.Add( "structure" );
                debugCode = 400;
                if ( data.IsOffToUpgrade )
                {
                    Buffer.Add( " that is heading for an armory to upgrade. " );
                }
                else
                {
                    debugCode = 500;
                    if ( data.Experience >= data.ExperienceForNextLevel )
                    {
                        if ( RelatedEntityOrNull.TypeData.IsMobile )
                            Buffer.Add( " that is eligible to upgrade at an Armory." );
                        else
                            Buffer.Add( " that can be upgraded by a Builder." );
                    }
                    else
                        Buffer.Add( " that requires " ).Add( (data.ExperienceForNextLevel - data.Experience).IntValue, "a1ffa1" ).Add( " more experience to level up. " );
                }
                debugCode = 600;
                if ( data.StoredMetal > FInt.Zero )
                    Buffer.Add( " This unit has " ).Add( data.StoredMetal.IntValue, "10ff10" ).Add( " stored metal." );
                if ( data.MustEvolveBeforeJoiningFireteam && !(data.IsHybrid || data.IsEvolved) )
                    Buffer.Add( " This unit must Evolve before joining a fireteam. " );
                if ( data.NextMustBeAnUpgrade )
                    Buffer.Add( " It must upgrade a building before it can build a new building." );
                debugCode = 700;
                if ( data.ScourgeTypeId != -1 )
                {
                    debugCode = 800;
                    ScourgeTypeData typedata = ScourgeTypeDataTable.Instance.GetRowById( data.ScourgeTypeId );
                    Buffer.Add( " " + typedata.ToString() + ". " );
                }
                if ( isVassal && RelatedEntityOrNull.TypeData.GetHasTag("ScourgeSummoner"))
                {
                    Buffer.Add("\n").Add("This structure is a summoner, attracing all friendly ships");
                }
                if ( RelatedEntityOrNull.TypeData.GetHasTag("ScourgeVassalArmory") && isVassal )
                {
                    ScourgeVassalFactionBaseInfo gdata = facOrNull.TryGetExternalBaseInfoAs<ScourgeVassalFactionBaseInfo>();
                    Buffer.Add("\n").Add("Armories can produce racial warriors at mark ").Add( gdata.Difficulty.RequiredLevelForArmoriesToEvolveWarriors, "a1ffa1" ).Add(", ID " + data.ScourgeTypeId);
                    Buffer.Add("\n").Add("Armories can produce hybrid warriors at mark ").Add( gdata.Difficulty.RequiredLevelForArmoriesToHybridizeWarriors, "a1ffa1" ).Add(" if you have unlocked tech level 2.");
                }
                debugCode = 900;
                if ( data.IsAssignedToMission )
                {
                    Buffer.Add( "\nThis unit is assigned to a mission: ");
                    if ( data.MyMission == null )
                        Buffer.Add(" (null)");
                    else
                        Buffer.Add(data.MyMission.ToStringForDisplay() );
                }
                debugCode = 1000;
                // if ( GameSettings.Current.GetBoolBySetting( "ShowFireteamHistory" ) && team != null && team.History.Count > 0 )
                // {
                //     Buffer.Add("\nFireteam History:\n ");
                //     for ( int i = team.History.Count - 1; i >= 0; i-- )
                //     {
                //         Buffer.Add("\t" + team.History[i] +"\n");
                //     }
                // }
                if ( debug )
                {
                    debugCode = 110;
                    if ( data.MustBuildNextOnFarFlungPlanetIdx != -1 )
                        Buffer.Add( " must build next on far flung planet " + World_AIW2.Instance.GetPlanetByIndex( data.MustBuildNextOnFarFlungPlanetIdx ).Name + "." );
                    if ( data.MetalIncomeLastSecond_ForUI > FInt.Zero )
                        Buffer.Add( " Metal income last second: " ).Add( data.MetalIncomeLastSecond_ForUI.ReadableString, "10ffdd" ).Add( ".\n" );
                    if ( RelatedEntityOrNull.TypeData.GetHasTag( "ScourgeWarrior" ) )
                    {
                        Buffer.Add( " Fireteam id " ).Add( RelatedEntityOrNull.FireteamId, "10ffdd" ).Add( " is " );
                        if ( team != null )
                        {
                            Buffer.Add( team.status.ToString(), "aaaaff" ).Add( "." );
                            if ( team.DefenseMode )
                                Buffer.Add( " This is a " ).Add( "Defense Fleet", "44dd44" ).Add( "." );
                            if ( team.Target != null )
                                Buffer.Add( " Target: " ).Add( team.Target.ToStringWithPlanet(), "ffaaaa" ).Add( ". " );
                            else if ( team.TargetPlanet != null )
                                Buffer.Add( " Target planet: " ).Add( team.TargetPlanet.Name, "ff2244" ).Add( ". " );
                        }
                    }

                }
            } catch ( Exception e)
            {
                ArcenDebugging.LogSingleLine("caught exception " + e + " debugCode " + debugCode, Verbosity.DoNotShow );
            }
            return;
        }
    }
}
