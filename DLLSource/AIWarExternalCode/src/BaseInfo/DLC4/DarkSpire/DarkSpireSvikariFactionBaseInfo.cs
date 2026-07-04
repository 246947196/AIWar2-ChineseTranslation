using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class DarkSpireSvikariFactionBaseInfo : DarkSpireFactionBaseInfo
    {
        public bool PlayerAllied;
        public bool AIAllied;
        public bool MinorFactionAllied;

        protected override void Cleanup()
        {
            base.Cleanup();
            PlayerAllied = true;
            AIAllied = false;
            MinorFactionAllied = false;
        }

        protected override void DoRefreshFromFactionSettings()
        {
            base.DoRefreshFromFactionSettings();
            this.ConquestMode = true;
            string allegiance = this.Allegiance;
            PlayerAllied = string.IsNullOrEmpty( allegiance ) || ArcenStrings.Equals( allegiance, "Player Allied" );
            AIAllied = ArcenStrings.Equals( allegiance, "Allied To AI" );
            MinorFactionAllied = ArcenStrings.Equals( allegiance, "Minor Faction Team Red" ) ||
                                 ArcenStrings.Equals( allegiance, "Minor Faction Team Blue" ) ||
                                 ArcenStrings.Equals( allegiance, "Minor Faction Team Green" );
        }

        public override void SetStartingFactionRelationships()
        {
            base.SetStartingFactionRelationships(); // calls EnemyThisFactionToAll
            string allegiance = this.Allegiance;
            Faction faction = this.AttachedFaction;
            if ( string.IsNullOrEmpty( allegiance ) || ArcenStrings.Equals( allegiance, "Player Allied" ) )
                AllegianceHelper.AllyThisFactionToHumans( faction );
            else if ( ArcenStrings.Equals( allegiance, "Allied To AI" ) )
                AllegianceHelper.AllyThisFactionToAI( faction );
            else if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Red" ) )
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Red" );
            else if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Blue" ) )
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Blue" );
            else if ( ArcenStrings.Equals( allegiance, "Minor Faction Team Green" ) )
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "Minor Faction Team Green" );
            else
                AllegianceHelper.AllyThisFactionToHumans( faction );
        }
    }
}
