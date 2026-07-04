using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class OutguardBeaconDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public static readonly List<OutguardGroupData> availableGroups = List<OutguardGroupData>.Create_WillNeverBeGCed( 60, "OutguardBeaconDescriptionAppender-availableGroups" );
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;

            availableGroups.Clear();
            bool HasBeenHacked = false;
            OutguardBeaconStateForPlanet.GetAvailableGroupsForBeaconOnPlanet( RelatedEntityOrNull.Planet, availableGroups, ref HasBeenHacked );
            //Faction controllingFaction = RelatedEntityOrNull.Planet.GetControllingFaction();
            
            if ( !HasBeenHacked )
            {
                Buffer.Add( "You must hack this beacon to activate it and communicate with any Outguard Groups." );
            }
            
            if ( availableGroups.Count == 0 )
            {
                Buffer.Add( " There are no Outguard Groups available via this beacon." );
                
                return;
            }
            
            Buffer.Add( " The following Outguard Groups can be contacted at this beacon: " );
            int i = 0;
            for ( ; i < availableGroups.Count; i++ )
            {
                OutguardGroupData group = availableGroups[i];
                if (i > 0)
                    Buffer.Add(", ");
                
                Buffer.Add( group.GetShortDisplayName(), TextStyle.Brighter );
            }
            
            Buffer.Add(".");
        }
    }
}
