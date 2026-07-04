
using System;
using System.Collections;
using UnityEngine;
using Arcen.AIW2.Core;
using Arcen.Universal;
using Sys=System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public partial class MovementPlanning
    {
        public Steering.Steering _steering = new Steering.Steering();
        public static Vector2 RefPos;

        public void PlanMovement_Starting()
        {
            _steering.NewFrame();
        }
        
        public void PlanMovement_Ending()
        {
            
        }
        
        public void PlanMovement_ForPlanet_Starting(Planet planet)
        {
        }
        
        public void PlanMovement_ForPlanet_Ending(Planet planet)
        {
            _steering.Prepare(planet);
        }
    }    
}

namespace Arcen.AIW2.External.Steering
{
    #region Helper Types
    
    public enum Flag : int
    {
        None            = 0,
        Displacer       = 1 << 0,
        HoldingPosition = 1 << 1,
    }
    
    public class Target
    {
        public GameEntity_Squad GameEntity;
        public Vector2 Position;
        public float MinRange;
        public float MaxRange;
        public Vector2 Vec;
        public Vector2 Dir;
        public float Dist;
    }
    
    public class Entity
    {
        public GameEntity_Squad GameEntity;
        public Vector2 Velocity;
        public Vector2 Position;
        public Vector2 NextPosition;
        public float CollideRadius;
        public float AvoidRadius;
        public float MaxSpeed;
        public float Mass;
        public int CollisionPriority;
        public int PriorityForTies;
        public int Flags;
        public Target Target = new Target();
        public Sys.List<Influence> Influences = new Sys.List<Influence>();
        public int FrameUpdated;
        public int FrameProcessed;
    }
    
    public struct Influence
    {
        public Entity Entity;
        public float Distance;
        public Vector2 Vec;
        public Vector2 Dir;
    }
    
    #endregion

    #region Steering
    
    public class Steering
    {
        private Sys.Dictionary<int, Entity> _idToEntity = new Sys.Dictionary<int, Entity>();
        private int Frame;
        private float MaxSteerRadius = 80000.0f;
        private RandomGenerator Rand = new MersenneTwister(42);
        private float SteeringRadiusScale = 1.0f;
        private float MaxSeparationForceAtThisPenetration = 50.0f;
        private float MaxSeparationForce = 50.0f;
        private Planet Planet;
        private bool IsEnabled;
        private int Steering_Category_Num = 42;
        
        public void NewFrame()
        {
            var scale = ExternalConstants.Instance.OriginalXmlData.GetFloat("steering_radius_scale", 1.0f, false);
            if (scale != SteeringRadiusScale)
            {
                SteeringRadiusScale = scale;
                _idToEntity.Clear();
            }
            
            var pen = ExternalConstants.Instance.OriginalXmlData.GetFloat("steering_max_separation_force_at_this_penetration", 50.0f, false);
            if (pen != MaxSeparationForceAtThisPenetration)
            {
                MaxSeparationForceAtThisPenetration = pen;
            }
            
            var force = ExternalConstants.Instance.OriginalXmlData.GetFloat("steering_max_separation_force", 50.0f, false);
            if (force != MaxSeparationForce)
            {
                MaxSeparationForce = pen;
            }
            
            var enabled = ExternalConstants.Instance.OriginalXmlData.GetBool("steering_enabled", false, false);
            if (enabled != IsEnabled)
            {
                IsEnabled = enabled;
            }
            
            Frame++;    
        }
        
        public void Prepare(Planet planet)
        {
            if (!IsEnabled)
                return;
            
            this.Planet = planet;
            
            InSceneTextRenderer.Instance.Clear(Steering_Category_Num);
                
            //InSceneTextRenderer.Instance.DrawPos(42, MovementPlanning.RefPos, 12.0f, Color.yellow, 0.4f);
            
            foreach ( GameEntity_Squad ge in planet.Squads() )
            {
                        var e = GetEntityFor(ge);
                        UpdatePosition(e);
                    }
            
            foreach ( GameEntity_Squad ge in planet.Squads() )
            {
                        var e = GetEntityFor(ge);
                        UpdateInfluences(e);
                    }
            
            foreach ( GameEntity_Squad ge in planet.Squads() )
            {
                        var e = GetEntityFor(ge);
                        Steer(e);
                    }
        }

        private Entity GetEntityFor( GameEntity_Squad GE )
        {
            Entity e;
            if (!_idToEntity.TryGetValue( GE.PrimaryKeyID, out e ))
            {
                e = new Entity();
                e.GameEntity = GE;
                e.Flags = (int)Flag.None;
                e.CollisionPriority = GE.TypeData.CollisionPriority;
                e.Mass = 10.0f;
                e.CollideRadius = (float)GE.GetRadius() * SteeringRadiusScale;
                e.AvoidRadius = e.CollideRadius + 500;
                e.MaxSpeed = GE.DataForMark.Speed;
                
                Rand.ReinitializeWithSeed(GE.PrimaryKeyID);
                e.PriorityForTies = Rand.Next();
                
                _idToEntity.Add( GE.PrimaryKeyID, e );
            }   

            return e;
        }
        
        private void UpdatePosition( Entity E )
        {
            if (E.FrameUpdated < Frame)
            {
                var pos = E.GameEntity.WorldLocation;
                
                var nextpos = E.GameEntity.FramePlan_Move_NextMovePoint;
                if (nextpos == ArcenPoint.ZeroZeroPoint)
                    nextpos = pos;

                //E.LastPosition = E.Position;
                E.Position = pos.ToVector2();
                E.NextPosition = nextpos.ToVector2();
                E.MaxSpeed = E.GameEntity.CalculatedSpeed;
                
                E.Velocity = E.NextPosition - E.Position;
                
                E.GameEntity._FramePlan_Move_NextMovePoint = ArcenPoint.ZeroZeroPoint;
                E.GameEntity.FramePlan_DoMove = false;
                E.GameEntity.FramePlan_Move_TouchedTarget = false;
                E.GameEntity._WorkingDestination_OnlyWriteFromMovementPlanning = ArcenPoint.ZeroZeroPoint;
                
                E.Target.Position = E.GameEntity.Orders.GetMoveTo().ToVector2();
                E.Target.MinRange = 0.0f;
                E.Target.MaxRange = 300.0f;
                
                var target = E.GameEntity.Orders.GetTargetOfAnyAttackOrder();
                if (target != null)
                {
                    E.Target.MinRange = 0.0f;
                    E.Target.MaxRange = 300.0f;
                }
                
                E.Target.GameEntity = target;
                
                E.Target.Vec = E.Target.Position - E.Position;
                var dist = E.Target.Vec.magnitude;
                if (dist > 0.1f)
                {
                    E.Target.Dir = E.Target.Vec.normalized;
                    E.Target.Dist = dist;
                }
                else
                {
                    E.Target.Dir = Vector2.zero;
                    E.Target.Dist = 0;
                }
                
                E.FrameUpdated = Frame;
            }
            //E.Velocity = Vector2.zero;
        }
        
        private void UpdateInfluences( Entity E )
        {
            //Originally went through Planet.GetEntities + the EntityPredicate/IEntityCollectionFilter/IFactionPredicate
            //machinery in WorldQueries.cs. That allocated a pooled predicate per call AND a capturing closure
            //for the inner callback — both per entity per frame. Inlined here for zero per-call alloc; the
            //predicate system in WorldQueries.cs had no other live callers and has been deleted.
            E.Influences.Clear();
            Planet planet = E.GameEntity.Planet;
            Vector2 selfPos = E.Position;
            float maxRadius = MaxSteerRadius;
            for ( int factionIndex = 0; factionIndex < planet.Factions.Count; factionIndex++ )
            {
                PlanetFaction pfac = planet.Factions[factionIndex];
                foreach ( GameEntity_Squad ge in pfac.Entities.Squads() )
                {
                    if ( ge == null )
                        continue;
                    Vector2 gePos = ge.WorldLocation.ToVector2();
                    float geRadius = (float)ge.GetRadius();
                    float distSqr = (gePos - selfPos).sqrMagnitude;
                    float maxSqr = (geRadius + maxRadius);
                    maxSqr *= maxSqr;
                    if ( distSqr >= maxSqr )
                        continue;

                    if ( ge == E.GameEntity )
                        continue;
                    Entity e = GetEntityFor(ge);
                    if ( e.CollisionPriority < E.CollisionPriority )
                        continue;
                    Vector2 vec = e.Position - selfPos;
                    float dist = vec.magnitude - (E.CollideRadius + e.CollideRadius);
                    if ( dist > 1000 )
                        continue;

                    Influence i = new Influence()
                    {
                        Entity = e,
                        Vec = vec,
                        Dir = vec.normalized,
                        Distance = dist,
                    };
                    E.Influences.Add(i);
                }
            }

            E.Influences.InsertionSort( static (a, b) => a.Distance.CompareTo(b.Distance) );
        }
        
        private void Steer( Entity E )
        {
            var GE = E.GameEntity;
            
            if (GE.GetIsSelected() == false)
                return;
            
            //var nextpos = MovementPlanning.RefPos;// E.Position;
            //E.Position = MovementPlanning.RefPos;
            
            //var forward = Vector2.zero;
            //if (E.LastPosition != Vector2.zero && E.LastPosition != E.Position)
            //{
            //    var vec = E.Position - E.LastPosition;
            //    forward = vec.normalized;
            //}
            
            float EffectiveDeltaTime = Planet.LocalDeltaTime_UnpausedOnly.ToFloatNonSim();
            
            Vector2 src = E.Position;
            Vector2 dst = E.NextPosition;
            Vector2 vel = dst - src;
            
            if (E.Influences.Count > 0)
            {
                float hitTime = float.MaxValue;
                Influence? hit = null;
                Vector2 hitNormal = Vector2.zero;
                
                foreach (var inf in E.Influences)
                {
                    // Solve for collision time (quadratic equation)
                    var a = Vector2.Dot(vel, vel);
                    var b = 2 * Vector2.Dot(vel, src - inf.Entity.Position);
                    var c = Vector2.Dot((src - inf.Entity.Position),(src - inf.Entity.Position)) - (float)Math.Pow(E.CollideRadius + inf.Entity.CollideRadius, 2);

                    var discriminant = b * b - 4 * a * c;

                    if (discriminant >= 0)
                    {
                        // Collision may occur, solve for collision time(s)
                        var t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
                        var t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);

                        // Check if collision times are positive and find the earliest one
                        if (t1 >= 0 && t1 < hitTime)
                        {
                            hitTime = t1;
                            hit = inf;
                        }
                        if (t2 >= 0 && t2 < hitTime)
                        {
                            hitTime = t2;
                            hit = inf;
                        }
                    }
                }
                
                if (hit != null)
                {
                    var hitinf = hit.Value;
                    var colpos = src + vel * (float)hitTime;
                    var colnorm = (hitinf.Entity.Position - colpos).normalized;
                    
                    InSceneTextRenderer.Instance.DrawPos( Steering_Category_Num, Core.Extensions.HashCombine( GE.PrimaryKeyID, 1 ), colpos, 10.0f, Color.yellow, 0.4f);
                    InSceneTextRenderer.Instance.DrawText( hit.Value.Entity.GameEntity, hitTime.ToString("N3"), Color.yellow, 0.4f );
                }
            }
            
            Vector2 dispdir = Vector2.zero;
            int counter = 0;
            float sum = 0;
            foreach (var inf in E.Influences)
            {
                if (inf.Distance < 0)
                {
                    counter++;
                    var mag = (-inf.Distance).Norm(E.CollideRadius);
                    sum += mag; 
                    dispdir += mag * -inf.Dir;
                }
            }
            
            if (counter > 0)
            {
                dispdir = (dispdir / sum);
                E.NextPosition += dispdir * EffectiveDeltaTime * E.MaxSpeed;
            }
            
            //var nextpos = E.GameEntity.WorldLocation.ToVector2();
            //E.Position = nextpos;
            //E.GameEntity.SetWorldLocation((int)nextpos.x, (int)nextpos.y, true, true);// = nextpos.ToArcenPoint();
            //E.GameEntity.FramePlan_DoMove = true;
            
            E.GameEntity.neverWriteDirectly_worldLocation.X = (int)E.NextPosition.x;
            E.GameEntity.neverWriteDirectly_worldLocation.Y = (int)E.NextPosition.y;
            E.GameEntity.NonSim_LastDecollisionFrameIfPositiveOrHasNotMovedIfNegative = -1;
            E.GameEntity.MovedLastFrame = true;
            E.GameEntity.TimeLastMoved = ArcenTime.TimeSinceStartF;
            
            if (GE.GetIsSelected())
            {
                //InSceneTextRenderer.Instance.DrawText(GE, "test", Color.white, 0.4f);
                InSceneTextRenderer.Instance.DrawPos(Steering_Category_Num, GE.PrimaryKeyID, E.Position, 10.0f, Color.white, 0.4f);
                
                if (E.Target.Position != Vector2.zero)
                {
                    InSceneTextRenderer.Instance.DrawPos(Steering_Category_Num, Core.Extensions.HashCombine( GE.PrimaryKeyID, 'T' ), E.Target.Position, 10.0f, Color.green, 0.4f);
                }
                
                if (E.NextPosition != Vector2.zero && E.NextPosition != E.Position)
                {
                    InSceneTextRenderer.Instance.DrawPos(Steering_Category_Num, Core.Extensions.HashCombine( GE.PrimaryKeyID, 'N' ), E.NextPosition, 10.0f, Color.green, 0.4f);
                }
                
                if (E.Target.GameEntity != null)
                {
                    InSceneTextRenderer.Instance.DrawPos(Steering_Category_Num, Core.Extensions.HashCombine( GE.PrimaryKeyID, E.Target.GameEntity.PrimaryKeyID, 'T' ), E.Target.Position, 10.0f, Color.green, 0.4f);
                }
                
                //LOG.Msg("{0} has {1} influences", e.GameEntity.ToString(), e.Influences.Count);
                if (E.Influences.Count > 0)
                {
                    foreach (var inf in E.Influences)
                    {
                        //InSceneTextRenderer.Instance.DrawPos(inf.Entity.GameEntity.PrimaryKeyID, inf.Entity.Position, 10.0f, Color.red, 0.4f);
                        //InSceneTextRenderer.Instance.DrawText(inf.Entity.GameEntity, inf.Distance.ToString("N1"), Color.white, 0.4f);//, inf.Entity.GameEntity.WorldLocation.ToVector2(), 10.0f, Color.red, 0.4f);
                    }
                }
                
                foreach (var inf in E.Influences)
                {
                    //LOG.Msg("  {0} at {1}", inf.Entity.GameEntity.ToString(), inf.Entity.Position);
                    //InSceneTextRenderer.Instance.DrawPos(inf.Entity.GameEntity.PrimaryKeyID, inf.Entity.GameEntity.WorldLocation.ToVector2(), 10.0f, Color.red, 0.4f);
                }
            }
        }
    }

    #endregion

}

namespace Arcen.AIW2.External
{
    
    public static partial class Extensions
    {
        public static float Norm(this float F, float Max)
        {
            if (F > Max)
                F = Max;
            return F / Max;
        }
    }
}
