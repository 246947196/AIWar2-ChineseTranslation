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
                Buffer.Add( "你必须破解此信标以激活它并与任何外卫组通信。" );
            }
            
            if ( availableGroups.Count == 0 )
            {
                Buffer.Add( " 通过此信标没有可用的外卫组。" );
                
                return;
            }
            
            Buffer.Add( " 以下外卫组可通过此信标联系：" );
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
