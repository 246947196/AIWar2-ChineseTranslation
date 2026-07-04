using Arcen.Universal;
using System;

using System.Text;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    public abstract class PlanetPathfinder : ArcenPathfinder<Planet>
    {
        public Faction GetFaction()
        {
            return this.Faction;
        }

        protected override int GetAccurateCrowFliesDistanceBetweenNodes( Planet left, Planet right )
        {
            return left.GetHopsTo( right );
        }

        protected override string GetDebugLogString( Planet node )
        {
            return "#" + node.Index + "(" + node.Name + ")";
        }

        protected override NodePassability CalculateIsNodePassable_Slow( Planet node )
        {
            PlanetFaction planetFaction = node.GetPlanetFactionForFaction( this.Faction );
            //this untested code should hopefully prevent the player from routing ships through an unexplored planet
            if ( this.Faction.Type == FactionType.Player && node.IntelLevel <= PlanetIntelLevel.Unexplored || node.HasPlanetBeenDestroyed )
                return NodePassability.NEVER_PASSABLE;
            if ( planetFaction.GetPlanetFactionBooleanFlag( PlanetFactionBooleanFlag.DoNotPathThrough ) )
                return NodePassability.ONLY_PASSABLE_FOR_ORIGIN;
            return NodePassability.ALWAYS_PASSABLE;
        }

        protected override int HeuristicCostEstimate( Planet Origin, Planet Target )
        {
            return Origin.GetHopsTo( Target );
        }

        protected override void SetDebugText( Planet node, string CostToGetHere, string GuessCostToGetToTarget )
        {
            node.DebugText = CostToGetHere + "|" + GuessCostToGetToTarget;
        }

        protected override void MidWipeForReuseAsNewObject()
        {
            this.Faction = null;
            this.SubWipeForReuseAsNewObject();
        }
        protected abstract void SubWipeForReuseAsNewObject(); //make sure anything that inherits from this is either abstract or pooled
    }

    public abstract class PlanetConservativePathfinderBase : PlanetPathfinder
    {
        protected abstract StrengthData_PlanetFaction_Stance GetStanceData( Planet node, FactionStance stance );

        protected override int HeuristicCostEstimate( Planet Origin, Planet Target )
        {
            StrengthData_PlanetFaction_Stance selfData = GetStanceData( Origin, FactionStance.Self );
            StrengthData_PlanetFaction_Stance friendlyData = GetStanceData( Origin, FactionStance.Friendly );
            StrengthData_PlanetFaction_Stance hostileData = GetStanceData( Origin, FactionStance.Hostile );
            int friendlyStrength = selfData.TotalStrength + friendlyData.TotalStrength;
            int hostileStrength = hostileData.TotalStrength;

            int uncounteredHostileStrength = Math.Max( 0, hostileStrength - friendlyStrength );

            return Origin.GetHopsTo( Target ) + uncounteredHostileStrength; // so generally the hostile strength will drown out distance considerations
        }
    }
}
