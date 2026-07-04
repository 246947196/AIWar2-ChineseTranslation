using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using UnityEngine;

using Arcen.Universal.Sprites;

namespace Arcen.AIW2.ExternalVisualization
{
    public struct SpecialEffectRequest
    {
        public ArcenSpecialEffectGroup Group;
        public Vector3 SuperGlobal3DPosition;
        public float TimeToLive;
        public int AOESize;
    }

    public struct ShotReactionRequest
    {
        public ShotVisualizer Shot;
        public GameEntity_Squad TargetSquad;
        public GameEntity_Squad ProtectingShieldThatTookTheHitOrNull;
        public int NumberOfShipsKilled;
        public bool WasEntireSquadKilled;
    }

    public struct SoundPlaybackRequestImmediate
    {
        public SFXItem SoundClip;
        public Vector3 Unity3DLocation;
        public bool PlayAs2D;
        public bool ActuallyPlay;
    }

    public struct SoundPlaybackRequestDelayed
    {
        public SFXItem SoundClip;
        public Vector3 Unity3DLocation;
        public bool PlayAs2D;
        public float TimeAtWhichToPlay;
        public bool ActuallyPlay;
    }
}
