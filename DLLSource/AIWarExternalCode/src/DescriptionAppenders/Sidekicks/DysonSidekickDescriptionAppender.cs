using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class DysonSidekickDescriptionAppender : GameEntityDescriptionAppenderBase
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
                if ( RelatedEntityOrNull == null )
                    return;
                SafeSquadWrapper RelatedEntityWrapper = SafeSquadWrapper.Create( RelatedEntityOrNull );
                TooltipDetail detailLevel = EntityText.Detail;
                if (RelatedEntityTypeData.GetHasTag("ReaperGateway") )
                {
                    ReapersPerUnitBaseInfo rData = RelatedEntityOrNull.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                    if (rData == null)
                    {
                        Buffer.Add("Null ReapersPerUnitData for ReaperGateway");
                        return;
                    }
                    int time = rData.GatewayNextReinforcementTime - World_AIW2.Instance.GameSecond;
                    Buffer.Add("This gateway will get reinforced in ").Add( time, "a1bb44" ).Add(" seconds.").Add("\n");
                    time = rData.GatewayNextLarvaTime - World_AIW2.Instance.GameSecond;
                    Buffer.Add("This gateway will spawn a larva in ").Add( time, "a1bb44" ).Add(" seconds.").Add("\n");
                    if (RelatedEntityOrNull.CurrentMarkLevel < 7)
                    {
                        time = rData.GatewayNextMarkupTime - World_AIW2.Instance.GameSecond;
                        Buffer.Add("This gateway will mark up in ").Add( time, "a1bb44" ).Add(" seconds.").Add("\n");
                    }
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag("ImmobileRavager")  || RelatedEntityTypeData.GetHasTag("ReaperChrysalis") )
                {
                    ReapersPerUnitBaseInfo rData = RelatedEntityOrNull.TryGetExternalBaseInfoAs<ReapersPerUnitBaseInfo>();
                    if ( rData != null )
                    {
                        if (RelatedEntityTypeData.GetHasTag("ImmobileRavager"))
                        {
                            Buffer.Add("This planet will be ravaged in ").Add( rData.SecondsTillRavage, "a1bb44" ).Add(" seconds.").Add("\n");
                            Buffer.Add("This Ravager will produce a new wave of troops in ").Add( rData.SecondsTillTroopSpawn, "4455a1" ).Add(" seconds.");
                        }
                        if (RelatedEntityTypeData.GetHasTag("ReaperChrysalis"))
                        {
                            int spawnTime = rData.ChrysalisHatchTime - World_AIW2.Instance.GameSecond;
                            if (spawnTime >= 0)
                            {
                                string color = ArcenExternalUIUtilities.GetColorForNomadMoveTime( spawnTime );
                                Buffer.Add("This will hatch in ").Add(spawnTime.ToString(), color).Add(" seconds, spawning enemies.\n");
                            }
                            else
                                Buffer.Add("This will hatch soon, spawning enemies\n");
                            Buffer.Add("This has ").Add( rData.CuendillarRemaining, "ff4444" ).Add(" cuendillar remaining.\n");

                        }
                    }

                    return;
                }
                DysonSidekickPerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<DysonSidekickPerUnitBaseInfo>();
                if ( RelatedEntityTypeData.GetHasTag("AICuendillarDrill") )
                {
                    if ( data != null && data.TimeForNextTransport != -1 )
                        Buffer.Add("A new AI Cuendillar Transport will be dispatched in ").Add( (data.TimeForNextTransport - World_AIW2.Instance.GameSecond), "0044ff" ).Add(" seconds.");
                    return;
                }
                Faction faction = RelatedEntityOrNull.PlanetFaction.Faction;
                if (RelatedEntityTypeData.GetHasTag("DysonFlagship") )
                {
                    if ( World_AIW2.Instance.Setup.GetStringBySetting("DysonAutoDefend") != "Disabled" )
                    {
                        if ( faction.UnderPlayerControl() )
                        {
                            Buffer.Add("<size=90%>This flagship is currently under player control, but if uncontrolled it will be automatically defending in ").Add( World_AIW2.Instance.Setup.GetStringBySetting("DysonAutoDefend"), "a1ffa1").Add(" mode. This modified by a Galaxy Setting.</size> ");
                        }
                        else
                            Buffer.Add("<size=90%>This flagship is set to ").Add( World_AIW2.Instance.Setup.GetStringBySetting("DysonAutoDefend"), "a1ffa1").Add(" and is playing automatically (as long as you aren't controlling the faction). This can be modified with a Galaxy Setting.</size> ");
                    }
                }

                DysonSidekickFactionBaseInfo globaldata = faction.TryGetExternalBaseInfoAs<DysonSidekickFactionBaseInfo>();
                if ( RelatedEntityTypeData.GetHasTag("BoostsGuardianCap") ||
                     RelatedEntityTypeData.GetHasTag("BoostsDireGuardianCap") )
                {
                    
                    Dictionary<Planet, int> guardianDict = globaldata.GuardiansPerPlanet.GetDisplayDict();
                    bool printedIntro = false;
                    foreach ( KeyValuePair<Planet, int> kv in guardianDict )
                    {
                        if ( !printedIntro)
                        {
                            printedIntro = true;
                            Buffer.Add("An accounting of all your empire's guardians:\n");
                        }
                        Buffer.Add("\t").Add(kv.Key.Name, "a1a1ff").Add(": ").Add( kv.Value, "a1ffa1" ).Add("\n");
                        continue;
                    }
                    return;
                }

                if ( data == null )
                {
                    //Buffer.Add(RelatedEntityOrNull.ToString() + " has no per unit");
                    return;
                }
                if ( RelatedEntityTypeData.KillsToTriggerTransformation > 0 )
                {
                    Buffer.Add(" This fleet has killed ").Add( data.UnitsKilled, "a1ffa1" ).Add(" units; after it kills ").Add( RelatedEntityTypeData.KillsToTriggerTransformation, "ffa1a1" ).Add(" then we will transform into ").Add(RelatedEntityTypeData.TransformAfterKills.GetDisplayName()).Add(". ");
                }

                if ( RelatedEntityTypeData.GetHasTag("CuendillarAsteroid") )
                {
                    Buffer.Add("This asteroid has ").Add( data.CuendillarRemaining, "a1ffa1" ).Add(" cuendillar available for mining.");
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag("CuendillarPlanetoid") )
                {
                    Buffer.Add("This planetoid has ").Add( data.CuendillarRemaining, "a1ffa1" ).Add(" cuendillar available for mining.");
                    return;
                }

                if ( globaldata == null )
                    return;

                if ( RelatedEntityTypeData.GetHasTag("DysonOverloader") )
                {
                    int secondsLeftForOverload = data.TimeTillPlanetOverloaded;
                    if ( secondsLeftForOverload == -1 )
                        Buffer.Add("This planet will be destroyed ").Add( globaldata.Difficulty.PlanetOverloadTime, "ff0000" ).Add(" seconds after the Overloader is completed.");
                    else
                    {
                        Buffer.Add("This planet will be destroyed in ").Add( secondsLeftForOverload, "ff00ff" ).Add(" seconds.");
                        Buffer.Add(" That will generate ").Add( globaldata.Difficulty.AIPForPlanetOverloading, "ff00" ).Add(" AIP as the drilling takes place. ");
                    }
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag("DysonAsteroidDrill") && data != null )
                {
                    int secondsLeftForDrill = data.TimeTillPlanetDrilled;
                    if ( secondsLeftForDrill == -1 )
                        Buffer.Add("This asteroid will be drilled ").Add( globaldata.CalculateDrillTime( RelatedEntityOrNull ), "ff0000" ).Add(" seconds after the Drill is completed.");
                    else
                    {
                        Buffer.Add("This asteroid will be completely drilled in ").Add( secondsLeftForDrill, "ff00ff" ).Add(" seconds. ");
                        if ( data.TimeForNextTransport != -1 )
                            Buffer.Add("A new Cuendillar Transport will be dispatched in ").Add( (data.TimeForNextTransport - World_AIW2.Instance.GameSecond), "0044ff" ).Add(" seconds.");
                    }
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag("AutoDefenseShip") && data != null )
                {
                    GameEntity_Squad HomeStronghold = data.HomeStronghold.GetSquad();
                    if ( HomeStronghold != null )
                    {
                        Buffer.Add("This ship is only available to defend planets near the ").Add( HomeStronghold.TypeData.GetDisplayName(), "a1ffa1" ).Add(" on ").Add(HomeStronghold.Planet.Name, "a1a1ff").Add(". ");
                    }
                }
                if ( RelatedEntityTypeData.GetHasTag("DysonDrill") && data != null )
                {
                    int secondsLeftForDrill = data.TimeTillPlanetDrilled;
                    if ( secondsLeftForDrill == -1 )
                    {
                        int time = globaldata.CalculateDrillTime( RelatedEntityOrNull );
                        Buffer.Add( "This planet will be drilled " ).Add( time.ToString(), "55b223" ).Add( " seconds after the Drill is completed, and will generate a total of " );
                    }
                    else
                    {
                        Buffer.Add("This planet will be drilled in ").Add(secondsLeftForDrill.ToString(), "ff00ff").Add(" seconds.");
                    }
                    if ( data.TimeForNextTransport != -1 )
                        Buffer.Add("A new Cuendillar Transport will be dispatched in ").Add( (data.TimeForNextTransport - World_AIW2.Instance.GameSecond).ToString(), "0044ff").Add(" seconds. ");
                    if ( RelatedEntityTypeData.GetHasTag( "DysonPlanetaryDrill" ) || RelatedEntityTypeData.GetHasTag( "DysonOverloader" ) )
                    {
                        int aip = globaldata.Difficulty.AIPForPlanetDrilling;
                        if ( RelatedEntityTypeData.GetHasTag( "DysonOverloader" ) )
                            aip = globaldata.Difficulty.AIPForPlanetOverloading;
                        Buffer.Add( " Building this will generate " ).Add( globaldata.Difficulty.AIPForPlanetDrilling.ToString(), "ff0000" ).Add( " AIP as this structure operates." );
                    }
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag("DysonStronghold") && data != null )//&& detailLevel >= TooltipDetail.Full)
                {
                    if ( data.GuardianMetal > 0 && data.DireGuardianMetal > 0 )
                    {
                        Buffer.Add("<size=80%>We have ").Add( data.GuardianMetal, "999999" ).Add(" guardian metal and ").Add( data.DireGuardianMetal, "999999" ).Add(" dire guardian metal.</size> ");  
                    }
                    else if ( data.GuardianMetal > 0 )
                        Buffer.Add("<size=80%>We have ").Add( data.GuardianMetal, "999999" ).Add(" metal to spend on a Guardian.</size> ");
                    else if ( data.DireGuardianMetal > 0 )
                        Buffer.Add("<size=80%>We have ").Add( data.DireGuardianMetal, "999999" ).Add(" metal to spend on a Dire Guardian.</size> ");
                    if ( data.DireGuardianMetal > 0 || data.GuardianMetal > 0)
                        Buffer.Add("\n");
                    if ( globaldata.GuardiansPerStronghold.GetDisplayDict()[RelatedEntityWrapper] > 0 )
                        Buffer.Add("<size=80%>Currently supporting ").Add( globaldata.GuardiansPerStronghold.GetDisplayDict()[RelatedEntityWrapper], "999999" ).Add(" of " ).Add( data.GuardianCap, "a1ffa1" ).Add(" guardians defending this stronghold.</size> ");
                    if ( globaldata.DireGuardiansPerStronghold.GetDisplayDict()[RelatedEntityWrapper] > 0 )
                        Buffer.Add("<size=80%>Currently supporting ").Add( globaldata.DireGuardiansPerStronghold.GetDisplayDict()[RelatedEntityWrapper], "999999" ).Add(" of " ).Add( data.DireGuardianCap, "a1ffa1" ).Add(" dire guardians defending this stronghold.</size> ");
                    if ( globaldata.GuardiansPerStronghold.GetDisplayDict()[RelatedEntityWrapper] > 0 ||  globaldata.DireGuardiansPerStronghold.GetDisplayDict()[RelatedEntityWrapper] > 0 )
                        Buffer.Add("\n");
                }
                if ( RelatedEntityTypeData.GetHasTag("DysonSidekickSphere") && data != null )
                {
                    if ( data.TimeTillDysonSphereWin == -1 )
                        Buffer.Add("Your sphere will come online ").Add( globaldata.Difficulty.TimeForSphereWin, "ff00ff" ).Add(" seconds after the Sphere is completed.");
                    else
                        Buffer.Add("Your sphere will come online in ").Add( data.TimeTillDysonSphereWin, "ff00ff" ).Add(" seconds.");
                    return;
                }
                if ( RelatedEntityTypeData.GetHasTag("DysonSidekickTransport")  )
                {
                    if ( data.CuendillarTransported == -1 )
                        Buffer.Add("Unknown cuendillar?");
                    else
                        Buffer.Add("This transport is bringing ").Add( data.CuendillarTransported, "0044ff" ).Add(" Cuendillar to a nearby sphere (or your HQ).");
                }

            }
            catch ( Exception ) { ArcenDebugging.ArcenDebugLogSingleLine("Whoops? exception in DysonSidekickDescriptionAppender", Verbosity.DoNotShow );}

            return;
        }
    }
}
