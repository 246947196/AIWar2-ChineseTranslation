using System;
using UnityEngine;
using Arcen.Universal;
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal.Sprites;

namespace Arcen.AIW2.ExternalVisualization
{
    public interface IRelatedEntity
    {
        GameEntity_Base GetEntityRelatedTo();
    }
}
