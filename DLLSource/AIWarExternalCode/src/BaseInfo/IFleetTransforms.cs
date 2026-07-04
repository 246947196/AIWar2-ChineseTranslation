using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

namespace Arcen.AIW2.External
{
    public static partial class FleetExensions {
        public static IFleetTransforms GetFleetTransforms(this Fleet fleet) {
            IFleetTransforms fleetTransforms = fleet.BaseInfo as IFleetTransforms;
            if (fleetTransforms != null) {
                return fleetTransforms;
            }
            GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
            FleetLeaderTypeData fleetLeaderType = centerpiece?.TypeData.FleetLeaderType;
            if (fleetLeaderType == null || fleetLeaderType.TypesCanSwitchTo.Count == 0) {
                return null;
            }
            // If the only type is ourself, then we can't actually transform.
            if (fleetLeaderType.TypesCanSwitchTo.Count == 1 && centerpiece.TypeData == fleetLeaderType.TypesCanSwitchTo[0]) {
                return null;
            }
            return FleetLeaderTransforms.GetFromPoolOrCreate(centerpiece.TypeData.FleetLeaderType);
        }
    }

    /// <summary>Interface describing a Fleet whose centerpiece can be transformed.</summary>
    ///
    /// This needs to be implented by the ExternalFleetBaseInfo attached to the fleet.
    /// There is also an adapter for FleetLeaderTypeData. You should access an implentation
    /// of this by calling the <c>GetFleetTransforms</c> extension method on the fleet.
    public interface IFleetTransforms {
        /// <summary>The name for this kind of fleet leader.</summary>
        string DisplayName { get; }
        /// <summary>Text to display if there are no available transforms</summary>
        string NoTransformsText { get; }
        /// <summary>Text for tooltip of transform button.</summary>
        void GetTooltip(ArcenCharacterBufferBase buffer);

        bool HasAnyTransforms { get; }
        System.Collections.Generic.IEnumerable<IFleetTransformTarget> TypesCanSwitchTo { get; }
        GameCommand CreateTransformCommand(GameCommandSource source, GameEntity_Squad centerpiece, string InternalName);
        GameEntityTypeData GetTypeDataForName(string InternalName);
    }

    /// <summary>Interface describing the type a Fleet centerpiece can transform into.</summary>
    public interface IFleetTransformTarget {
        GameEntityTypeData TypeData { get; }
        string DisplayName { get; }
        /// <summary>Name that will be passed to <c>IFleetTransforms.CreateTransformCommand</c> and <c>IFleetTransforms.GetTypeDataForName</c>.</summary>
        string InternalName { get; }
        int HackingCost { get; }
        int ResourceOneCost { get; }
        /// <summary>Check if the given fleet's centerpiece can currently transform into this type.</summary>
        ///
        /// This does not include the cost to transform.
        ///
        /// <param name="InvalidReason">If the transfomr is not possible, text to display to the user giving the reason.</param>
        bool CanTransformInto(Fleet fleet, out string InvalidReason);
    }

    /// <summary>Adapater from <c>FleetLeaderTypeData</c> to <c>IFleetTransforms</c>.</summary>
    public class FleetLeaderTransforms: IFleetTransforms, IBetweenXmlReloadPoolable<FleetLeaderTransforms>
    {
        private FleetLeaderTypeData Type;
        private readonly List<FleetLeaderTransformTarget> TypesToConvertTo = List<FleetLeaderTransformTarget>.Create_WillNeverBeGCed( 10, "FleetLeaderTypeTransformer-TypesToConvertTo" );

        public string DisplayName { get { return this.Type.DisplayName; } }
        public string NoTransformsText { get { return "No types to switch to!  Cannot Switch Types Right Now</color>"; } }
        public void GetTooltip(ArcenCharacterBufferBase buffer) {
            buffer.Add("You can spend hacking points to change the form of this fleet leader.");
        }

        public bool HasAnyTransforms { get { return this.TypesToConvertTo.Count > 0; } }
        public System.Collections.Generic.IEnumerable<IFleetTransformTarget> TypesCanSwitchTo {
            get { return TypesToConvertTo; }
        }
        public GameCommand CreateTransformCommand(GameCommandSource source, GameEntity_Squad centerpiece, string InternalName) {
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransformUnits], GameCommandSource.IsLiterallyFromDirectClickOfLocalPlayer );
            command.RelatedEntityIDs.Add( centerpiece.PrimaryKeyID );
            command.RelatedString2 = InternalName;
            return command;
        }
        public GameEntityTypeData GetTypeDataForName(string InternalName) {
            return GameEntityTypeDataTable.Instance.GetRowByName(InternalName);
        }

        /// <summary>Adapater from <c>GameEntityTypeData</c> to <c>IFleetTransforms</c> for <c>FleetLeaderTypeTransformer</c>.</summary>
        public class FleetLeaderTransformTarget : IFleetTransformTarget  {
            public GameEntityTypeData TypeData { get; }
            public string DisplayName { get { return this.TypeData.DisplayName; } }
            public string InternalName { get { return this.TypeData.InternalName; } }
            public int HackingCost { get { return this.TypeData.HackingCostForOtherFleetLeadersOfSameTypeToBecomeMe; } }
            public int ResourceOneCost { get { return 0; } }
            public bool CanTransformInto(Fleet fleet, out string InvalidReason) { InvalidReason = null; return true; }
            public FleetLeaderTransformTarget(GameEntityTypeData TypeData) {
                this.TypeData = TypeData;
            }
        }

        // We keep a cache of FleetLeaderTransforms for each FleetLeaderTypeData that is accessed.
        // We remove the association when a FleetLeaderTransform is returned to the pool, which
        // happens when the XML is reloaded.
        private static readonly Dictionary<FleetLeaderTypeData, FleetLeaderTransforms> Cache = Dictionary<FleetLeaderTypeData, FleetLeaderTransforms>.Create_WillNeverBeGCed( 20, "FleetLeaderTypeTransformer-cache" );

        private static readonly BetweenXmlReloadPool<FleetLeaderTransforms> Pool = BetweenXmlReloadPool<FleetLeaderTransforms>.Create_WillNeverBeGCed( "FleetLeaderTypeTransformers", 20, 10, PoolBehaviorDuringShutdown.AllowAllThreads,
             delegate { return new FleetLeaderTransforms(); } );

        public static FleetLeaderTransforms GetFromPoolOrCreate(FleetLeaderTypeData Type)
        {
            if (FleetLeaderTransforms.Cache.ContainsKey(Type)) {
                return FleetLeaderTransforms.Cache[Type];
            }
            FleetLeaderTransforms result = Pool.GetFromPoolOrCreate();
            result.Type = Type;
            foreach (GameEntityTypeData TypeData in Type.TypesCanSwitchTo) {
                result.TypesToConvertTo.Add(new FleetLeaderTransformTarget(TypeData));
            }
            Cache[Type] = result;
            return result;
        }

        void IBetweenXmlReloadPoolable<FleetLeaderTransforms>.WipeForReuseAsNewObject()
        {
            Cache.Remove(this.Type);
            this.InnerSetToDefaults();
        }

        private static ArcenTypeAnalyzer<FleetLeaderTransforms> typeAnalyzer;
        protected void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<FleetLeaderTransforms>( new FleetLeaderTransforms() );
            typeAnalyzer.ApplyDefaults( this );
        }

        private static ReferenceTracker RefTracker;
        private FleetLeaderTransforms()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker("FleetLeaderTypeTransformer");
            RefTracker.IncrementObjectCount();
        }

    }

};
