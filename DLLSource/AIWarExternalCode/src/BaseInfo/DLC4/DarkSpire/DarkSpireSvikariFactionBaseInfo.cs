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
            AIAllied = ArcenStrings.Equals( allegiance, "对AI友好" );
            MinorFactionAllied = ArcenStrings.Equals( allegiance, "小派系小队红" ) ||
                                 ArcenStrings.Equals( allegiance, "小派系小队蓝" ) ||
                                 ArcenStrings.Equals( allegiance, "小派系小队绿" );
        }

        public override void SetStartingFactionRelationships()
        {
            base.SetStartingFactionRelationships(); // calls EnemyThisFactionToAll
            string allegiance = this.Allegiance;
            Faction faction = this.AttachedFaction;
            if ( string.IsNullOrEmpty( allegiance ) || ArcenStrings.Equals( allegiance, "Player Allied" ) )
                AllegianceHelper.AllyThisFactionToHumans( faction );
            else if ( ArcenStrings.Equals( allegiance, "对AI友好" ) )
                AllegianceHelper.AllyThisFactionToAI( faction );
            else if ( ArcenStrings.Equals( allegiance, "小派系小队红" ) )
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "小派系小队红" );
            else if ( ArcenStrings.Equals( allegiance, "小派系小队蓝" ) )
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "小派系小队蓝" );
            else if ( ArcenStrings.Equals( allegiance, "小派系小队绿" ) )
                AllegianceHelper.AllyThisFactionToMinorFactionTeam( faction, "小派系小队绿" );
            else
                AllegianceHelper.AllyThisFactionToHumans( faction );
        }
    }
}
