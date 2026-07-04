using Arcen.AIW2.Core;
using System;

using System.Text;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class Scenario_TestChamber : BaseScenario
    {
        protected override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap_Inner()
        {
            //nothing to do here
        }

        public static Scenario_TestChamber Instance;
        public Scenario_TestChamber() { Instance = this; }

        public override bool GetShouldSkipGameSetup()
        {
            return true;
        }
        
        public override void DoOnFirstUnpauseLogic_FromGameSetup_HostOnly( bool IsFromMapGen )
        {
            base.DoOnFirstUnpauseLogic_FromGameSetup_HostOnly( IsFromMapGen );

            /*
            foreach ( var fac in World_AIW2.Instance.Factions )
            { 
                AllegianceHelper.MakeAIHostileToOtherAIs(fac);
            }
            */
        }
        public override void HandleAdditionalFactionAdds( List<ConfigurationForFaction> CurrentFactions )
        {
            /*
            var setting = AIWar2GalaxySettingTable.Instance.GetRowByName("AICivilWar");
            World_AIW2.Instance.Setup.SetBoolBySetting( setting, true );
            
            var dataToAdd = SpecialFactionDataTable.Instance.GetRowByName("AI");
            if ( dataToAdd == null )
                throw new Exception("Could not find AI faction");
            ConfigurationForFaction config = ConfigurationForFaction.Create( CurrentFactions.Count, "AI2", dataToAdd );
            //config.SetCustomFieldValue("Allegiance", "Hostile To All");
            config.ShouldNeverBeRetainedInLobby = true;
            config.FactionCenterColor = TeamColorDefinitionTable.Instance.GetRandomRow();
            
            CurrentFactions.Add( config );
            */
        }
    }
}
