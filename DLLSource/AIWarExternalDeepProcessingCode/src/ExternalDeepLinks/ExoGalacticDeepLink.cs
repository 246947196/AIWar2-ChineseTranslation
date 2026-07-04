using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class ExoGalacticDeepLink : ExoGalacticDeepLinkRoot
    {
        public ExoGalacticDeepLink()
        {
            ExoGalacticDeepLinkRoot.Instance = this;
        }

        public override void SendExoGalacticAttack( ExoOptions options, ArcenHostOnlySimContext Context )
        {
            if ( Context == null )
            {
                options.ReturnToPool();
                return; //client
            }
            ExoGalacticAttackManager.SendExoGalacticAttack( options, Context );
        }
    }
}
