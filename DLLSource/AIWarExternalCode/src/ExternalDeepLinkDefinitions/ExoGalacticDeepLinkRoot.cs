using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public abstract class ExoGalacticDeepLinkRoot : IExternalDeepLink
    {
        public static ExoGalacticDeepLinkRoot Instance;

        public abstract void SendExoGalacticAttack( ExoOptions options, ArcenHostOnlySimContext Context );
    }
}
