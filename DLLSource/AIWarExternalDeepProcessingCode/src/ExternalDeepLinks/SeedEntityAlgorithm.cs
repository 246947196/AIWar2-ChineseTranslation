using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcen.AIW2.External
{
    public abstract class SeedEntityAlgorithm : IExternalDeepLink
    {
        public abstract bool Enabled { get; }
        public abstract void Seed( ArcenHostOnlySimContext Context, Galaxy Galaxy, SeedArgs Args );
    }
}
