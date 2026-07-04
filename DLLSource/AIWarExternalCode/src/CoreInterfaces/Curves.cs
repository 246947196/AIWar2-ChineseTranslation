using System;
using Arcen.Universal;
using Arcen.AIW2.Core;

namespace Arcen.AIW2.External
{
    #region Response Curve
    /// <summary>
    /// Represents a mathematical curve by a collection of sample points.
    /// The sample value at a given f(x) key position can then be evaluated.
    /// </summary>    
    public abstract class ResponseCurve<T> where T : new()
    {
        #region Data

        public struct Sample
        {
            public Sample(float key, T val)
            {
                Key = key;
                Value = val;
            }
            
            public float Key;
            public T Value;
        };

        public System.Collections.ObjectModel.ReadOnlyCollection<Sample> Samples { get; private set; }
        private readonly System.Collections.Generic.List<Sample> _samples;

        #endregion

        #region Init, Clear

        public ResponseCurve()
            : this(0)
        {
        }

        public ResponseCurve(int numSamples)
        {
            _samples = new System.Collections.Generic.List<Sample>( numSamples);
            Samples = new System.Collections.ObjectModel.ReadOnlyCollection<Sample>( _samples);
        }

        public void Clear()
        {
            _samples.Clear();
        }

        #endregion

        /// <summary>
        /// These methods must be implemented in child classes so that interpolation
        /// between different sample points can be performed.
        /// </summary>        
        #region Abstract Methods

        protected abstract T Add(T a, T b);
        protected abstract T Subtract(T a, T b);
        protected abstract T Scale(T a, float b);

        #endregion

        public void AddPoint(float key, T val)
        {
            int i = 0;
            for (; i < Samples.Count; i++)
            {
                var e = Samples[i];
                if (e.Key == key)
                {
                    System.Diagnostics.Debug.Assert(false, "ResponseCurve<T>::AddSample, Duplicate values are not allowed.");
                    return;
                }

                if (e.Key > key)
                    break;
            }

            _samples.Insert(i, new Sample(key, val));
        }

        public T Evaluate(float key)
        {        
            T result;

            if (_samples.Count == 0)
            {
                return new T();                
            }
            else
            {            
                int numSamples = _samples.Count;
                if (numSamples == 1 || key <= _samples[0].Key)                
                {
                    result = _samples[0].Value;
                }
                else if (key >= _samples[numSamples-1].Key)
                {
                    result = _samples[numSamples - 1].Value;
                }
                else
                {                
                    int i = 1;
                    while (i < (numSamples - 1) && _samples[i].Key < key)                    
                        i++;                    

                    // Interpolate between m_Samples[i-1] and m_Samples[i]                    
                    float min = _samples[i - 1].Key;                    
                    float max = _samples[i].Key;

                    System.Diagnostics.Debug.Assert(min != max, "Min/Max samples should not be equal.");

                    float t = (key - min)/(max - min);
                    result = Subtract(_samples[i].Value, _samples[i - 1].Value);
                    result = Scale(result, t);
                    result = Add(_samples[i - 1].Value, result);                    
                }
            }

            return result;
        }

        /// <summary>
        /// Change the key/value of the sample at the specified index to the new values provided.
        /// 
        /// Note: Sample will be readded to the list in a location appropriate for its new key and will
        /// not necessarily stay where it currently is.
        /// </summary>
        public int SetPoint( int idx, float key, T val )
        {            
            int i = 0;
            for (; i != _samples.Count - 1; i++)
            {
                var e = _samples[i];
                if (e.Key == key)
                {
                    System.Diagnostics.Debug.Assert(false, "ResponseCurve<T>::AddSample, Duplicate values are not allowed.");
                    return -1;
                }

                if (e.Key > key)
                    break;
            }

            _samples.RemoveAt(idx);
            _samples.Insert(i, new Sample(key, val));

            return (int)(i - _samples[0].Key);
        }    
    }

    public class ResponseCurveSingle : ResponseCurve<float>
    {
        protected override float Add(float a, float b)
        {
            return a + b;
        }

        protected override float Subtract(float a, float b)
        {
            return a - b;
        }

        protected override float Scale(float a, float b)
        {
            return a*b;
        }
    }

    #endregion

    #region Curve
    public class Curve : ArcenDynamicTableRow, IConcurrentPoolable<Curve>, IProtectedListable
    {
        #region Fields
        
        public struct Sample
        {
            public int id;
            public float t;
            public float v;
        }
        public List<Sample> Samples = List<Sample>.Create_WillNeverBeGCed(5, "Curve.Samples");

        public ResponseCurveSingle Object = new ResponseCurveSingle();

        public EasingFunction.Ease Easing;
        
        #endregion

        public Curve(object untracked)
            : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
        }
        
        public void Setup()
        {
            Object.Clear();
            
            Log log=null;
            
            log?.Msg("{0}.Setup() called; numSamples={1}", InternalName, Samples.Count);
            for (int i = 0; i < Samples.Count; i++)
            {
                var itr = Samples[i];
                log?.Msg(" s{0}: t={1}, v={2}", i, itr.t, itr.v);
                
                Object.AddPoint(itr.t, itr.v);
            }
            
            var s = Object.Samples[0];
            var e = Object.Samples[Object.Samples.Count-1];
            
            log?.Msg(" start.Key={0} start.Value={1}", s.Key, s.Value);
            log?.Msg(" end.Key={0} end.Value={1}", e.Key, e.Value);
            log?.Msg(" Eval({0})={1}, Eval({2})={3}", s.Key, Evaluate(s.Key, true), e.Key, Evaluate(e.Key, true));
        }
        
        public void Clear()
        {
            this.Object.Clear();
            this.Samples.Clear();
        }
        
        public float Evaluate(float t, bool log=false)
        {
            var s = Object.Samples[0].Key;
            var e = Object.Samples[Object.Samples.Count-1].Key;
            
            float eased = EasingFunction.GetEasingFunction(this.Easing)(s, e, t);
            float lerped = Object.Evaluate(t);
            
            if (log) LOG.Msg("{0}.Evaluate() called; s={1}, e={2}, easing={3}; t={4} => eased={5} => lerped={6}", InternalName, s, e, this.Easing, t, eased, lerped);
            
            return lerped;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private Curve() 
            : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "Curve" );
            RefTracker.IncrementObjectCount();
        }

        private static ConcurrentPool<Curve> Pool = new ConcurrentPool<Curve>( "Curve", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new Curve(); } );

        public static Curve GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        //private static ArcenTypeAnalyzer<Curve> typeAnalyzer;

        protected override void InnerSetToDefaults()
        {
            //if ( typeAnalyzer == null )
            //    typeAnalyzer = new ArcenTypeAnalyzer<Curve>( new Curve(null) );
            //typeAnalyzer.ApplyDefaults( this );
            this.Clear();
        }
        #endregion
    }
    #endregion

    #region CurveTable
    public class CurveTable : ArcenDynamicTable<Curve>
    {
        public static CurveTable Instance = new CurveTable();
        public CurveTable() : base( "Curves", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
        }

        protected override void PrepForCompleteReloadLater()
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            
            SetupRows();
        }

        public override void ReloadSelectData()
        {
            foreach (var r in Rows)
                r.Clear();
            
            base.ReloadSelectData();
            
            SetupRows();
        }

        public override DelReturn NodeProcessor( ArcenXMLElement e, Curve obj )
        {
            ProcessElement(e, obj);
            return DelReturn.Continue;
        }

        public override DelReturn NodeSelectReProcessor( ArcenXMLElement e, Curve obj )
        {
            ProcessElement(e, obj);
            return DelReturn.Continue;
        }
        
        private void ProcessElement( ArcenXMLElement e, Curve obj )
        {
            e.FillEnum<EasingFunction.Ease>("easing", ref obj.Easing, false);
            
            foreach ( ArcenXMLElement c in e.ChildrenOfType_AndParentsAndPartials( "sample" ) )
                {
                    int id = -1;
                    float t = -1;
                    float v = -1;

                    c.Fill("id", ref id, true);
                    c.Fill("t", ref t, !c.ReadingPartialRecord);
                    c.Fill("v", ref v, !c.ReadingPartialRecord);

                    int idx = obj.Samples.FindIndex( i=>i.id==id );
                    if (idx != -1)
                    {
                        var sample = obj.Samples[idx];
                        if (t != -1)
                            sample.t = t;
                        if (v != -1)
                            sample.v = v;
                        obj.Samples[idx] = sample;

                        continue;
                    }

                    {
                        var sample = new Curve.Sample()
                        {
                            id = id,
                            t = t,
                            v = v,
                        };

                        obj.Samples.Add(sample);
                    }
                }
        }

        private void SetupRows()
        {
            foreach (var r in Rows)
            {
                r.Setup();
            }
        }
        
        public override Curve GetNewRowFromPool()
        {
            return Curve.GetFromPoolOrCreate();
        }
    }
    #endregion

    #region CurveTable_Hooks
    public class CurveTable_Hooks : IArcenExternalCodeHookHandler
    {
        void IArcenExternalCodeHookHandler.HandleExternalHook( object MainObject, object SecondaryObject, object[] AdditionalObjects,
            ArcenExternalCodeHook Hook, ArcenSimContextBase Context )
        {
            //LOG.Msg("TextStyleTable.HandleExternalHook '{0}", Hook.InternalName);

            if ( Hook.InternalName == "LoadExternalData" )
            { 
                CurveTable.Instance.Initialize();
            }
            
            if ( Hook.InternalName == "ReloadSelectData")
            {
                CurveTable.Instance.ReloadSelectData();
            }
        }
    }
    #endregion
}

