using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public enum SpireCityBlockingReason
    {
        None, //we can build
        OnSamePlanetAsOtherCity,
        TooNearOtherCity,
        RelicCannotLeaveInitialPlanet,
        IsAIHomeworld,
        RelicCannotLeaveInitialPlanetButPlanetIsAlreadyCity,
        IsAIBastionWorld,
        Unknown, //the UI list was in flux which caused a null reference. This should be fleeting (1 sim step at most)
    }
}
