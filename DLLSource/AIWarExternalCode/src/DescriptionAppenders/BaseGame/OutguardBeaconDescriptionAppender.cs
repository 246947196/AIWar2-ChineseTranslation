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
                Buffer.Add( "浣犲繀椤荤牬瑙ｆ淇℃爣浠ユ縺娲诲畠骞朵笌浠讳綍澶栧崼缁勯€氫俊銆? );
            }
            
            if ( availableGroups.Count == 0 )
            {
                Buffer.Add( " 閫氳繃姝や俊鏍囨病鏈夊彲鐢ㄧ殑澶栧崼缁勩€? );
                
                return;
            }
            
            Buffer.Add( " 浠ヤ笅澶栧崼缁勫彲閫氳繃姝や俊鏍囪仈绯伙細" );
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
