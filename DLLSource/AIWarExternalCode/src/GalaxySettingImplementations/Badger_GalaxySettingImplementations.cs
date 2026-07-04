using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class GalaxySetting_AICivilWar : BaseGalaxySettingImplementation
    {
        public override void DoGameStartLogic_HostOnly( ArcenHostOnlySimContext Context, int IntValue, string StringValue )
        {
            if ( Engine_AIW2.Instance.IsTestChamber || !World_AIW2.Instance.Setup.ShouldSeedDetailsYet )
                return; // ignore 'em
            //ArcenDebugging.ArcenDebugLogSingleLine( "CIVIL_WAR_TEST: GalaxySetting_AICivilWar: " + IntValue, Verbosity.DoNotShow );
            if ( IntValue <= 0 )
                return; //it's not enabled, so don't do anything
            foreach ( GameEntity_Squad entity in World_AIW2.Instance.Squads( EntityRollupType.CivilWarWhenNoneLeft ) )
            {
                entity.Despawn(Context, true, InstancedRendererDeactivationReason.AFactionJustWarpedMeOut );
            }
            //ArcenDebugging.ArcenDebugLogSingleLine( "CIVIL_WAR_TEST: GalaxySetting_AICivilWar: Yes Start", Verbosity.DoNotShow );
            IScenarioImplementation scenarioImp = World_AIW2.Instance.GetScenarioImplementationSafe_OrNull();
            if ( scenarioImp != null )
                scenarioImp.StartCivilWar(Context);
        }
    }

    public class GalaxySetting_NomadifyGalaxy : BaseGalaxySettingImplementation
    {
        public override void DoGameStartLogic_HostOnly( ArcenHostOnlySimContext Context, int IntValue, string StringValue )
        {
            if ( Engine_AIW2.Instance.IsTestChamber || !World_AIW2.Instance.Setup.ShouldSeedDetailsYet )
                return; // ignore 'em
            if ( IntValue <= 0 )
                return; //it's not enabled, so don't do anything
            
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                planet.MakePlanetNomadic();
            }
            World_AIW2.Instance.CurrentGalaxy.IsNomadGalaxy = true;
        }
    }

    public class GalaxySetting_AllyWithAI : BaseGalaxySettingImplementation
    {
        //handles ally to ai mode
        public override void DoGameStartLogic_HostOnly( ArcenHostOnlySimContext Context, int IntValue, string StringValue )
        {
            if ( Context == null ) //client
                return;
            if ( Engine_AIW2.Instance.IsTestChamber || !World_AIW2.Instance.Setup.ShouldSeedDetailsYet )
                return; // ignore 'em
            if ( IntValue <= 0 )
                return; //it's not enabled, so don't do anything

            this.DoAlliances();
        }

        public override void DoAfterSavegameLoadLogic_HostOnly( ArcenHostOnlySimContext Context, int IntValue, string StringValue )
        {
            if ( Context == null ) //client
                return;
            if ( Engine_AIW2.Instance.IsTestChamber )
                return; // ignore 'em
            if ( IntValue <= 0 )
                return; //it's not enabled, so don't do anything

            this.DoAlliances();
        }

        public void DoAlliances()
        {
            Faction firstAIFaction = null;
            for ( int i = 0; i < World_AIW2.Instance.Factions.Count; i++ )
            {
                Faction faction = World_AIW2.Instance.Factions[i];
                if ( faction.Type == FactionType.AI )
                {
                    firstAIFaction = faction;
                    break;
                }
            }

            for (int i = 0; i < World_AIW2.Instance.Factions.Count; i++)
            {
                Faction faction = World_AIW2.Instance.Factions[i];
                if(faction.Type == FactionType.Player )
                {
                    for(int j = 0; j < World_AIW2.Instance.Factions.Count; j++)
                    {
                        if ( i == j )
                            continue;
                        Faction otherFaction = World_AIW2.Instance.Factions[j];
                        if(otherFaction.Type == FactionType.AI || 
                            FactionUtilityMethods.Instance.IsACoreAISubFaction( otherFaction ) ||
                            ( firstAIFaction != null && firstAIFaction.GetIsFriendlyTowards( faction ) ) )
                        {
                            faction.MakeFriendlyTo( otherFaction );
                            otherFaction.MakeFriendlyTo( faction );
                        }
                    }
                }
            }
            //Also grant all vision, since the player can't take AI planets
            World_AIW2.Instance.Debug_JustShowEverything = true;
            foreach ( Planet planet in World_AIW2.Instance.Planets( false ) )
            {
                planet.IntelLevel = PlanetIntelLevel.PermanentlyWatched;
                planet.GameSecondLastHadVisionBase = World_AIW2.Instance.GameSecond;
            }
        }
    }

    public class GalaxySetting_Necromancer : BaseGalaxySettingImplementation
    {
        public override bool ShouldShow
        {
            get
            {
                if (World_AIW2.Instance.InSetupPhase)
                {
                    var necro = World_AIW2.Instance.Setup.FactionConfigurations.Find(
                        (cfg) =>
                        {
                            var player = cfg.PlayerTypeDataOrNull_ModeratelyExpensive;
                            if (player == null)
                                return false;
                            
                            if (player.InternalName == "NecromancerEmpire" ||
                                player.InternalName == "NecromancerSidekick")
                            {
                                return true;
                            }
                            
                            return false;
                        });
                    
                    return necro != null;
                }
                
                return NecromancerEmpireFactionBaseInfo.GetNecromancerFactionCount() > 0;
            }
        }
    }
}
