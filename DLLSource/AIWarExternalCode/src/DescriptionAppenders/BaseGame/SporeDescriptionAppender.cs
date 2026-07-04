using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class SporeDescriptionAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            if ( RelatedEntityOrNull == null )
                return;
            MacrophageFactionBaseInfoCore infestation = null;
            Faction facOrNull = RelatedEntityOrNull.GetFactionOrNull_Safe();
            if ( facOrNull != null )
                infestation = facOrNull.TryGetExternalBaseInfoAs<MacrophageFactionBaseInfoCore>();
            if ( infestation == null )
            {
                Buffer.Add( "MacrophageFactionBaseInfo could not be found here. This is a BUG" );
                return;
            }
            if ( infestation.Telia == null )
            {
                Buffer.Add( "Please unpause the game to view additional information about this spore" );
                return;
            }
            MacrophagePerSporeBaseInfo sDataOrNull = RelatedEntityOrNull.TryGetExternalBaseInfoAs<MacrophagePerSporeBaseInfo>();
            if ( sDataOrNull == null )
            {
                Buffer.Add( "If you have just loaded a game then please unpause it and it should fill in" );
                return;
            }
            if ( infestation.Telia == null )
                return;

            List<SafeSquadWrapper> telia = infestation.Telia.GetDisplayList();
            foreach ( SafeSquadWrapper wrap in telia )
            {
                GameEntity_Squad tSquad = wrap.GetSquad();
                if ( tSquad == null )
                    continue;
                if ( sDataOrNull.TeliumID == tSquad.PrimaryKeyID )
                    Buffer.Add( "This spore is from the telium on " + tSquad.GetPlanetName_Safe() + ". If there are spores from " + infestation.NumDifferentTeliaRequired + " different planets then they will form a new Telia. " );
            }

            if ( infestation.SporeLifespan > 0 )
            {
                int secondsLeft = (infestation.SporeLifespan + sDataOrNull.SpawnTime) - World_AIW2.Instance.GameSecond;
                Buffer.Add( $"This Spore has a limited lifetime, and will despawn soon: {(secondsLeft / 60).ToString("0")}:{(secondsLeft % 60).ToString("00")}" );
            }
        }
    }
}
