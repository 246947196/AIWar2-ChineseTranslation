using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

namespace Arcen.AIW2.External
{
    public static partial class FleetExensions {
        public static List<SafeSquadWrapper> GetFleetsThatCanBeBolstered(this Fleet fleet) {
            return (fleet.BaseInfo as IBolsteringFleet)?.FleetsThatCanBeBolstered;
        }
        public static bool GetCanBolsterAnything( this Fleet fleet ) {
            int count = fleet.GetFleetsThatCanBeBolstered()?.Count ?? 0;
            return (count > 0);
        }
    }

    public interface IBolsteringFleet {
        List<SafeSquadWrapper> FleetsThatCanBeBolstered { get; }
    }
};
