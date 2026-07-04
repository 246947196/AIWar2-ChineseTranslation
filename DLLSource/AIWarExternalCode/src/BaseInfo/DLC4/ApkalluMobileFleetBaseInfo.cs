using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class ApkalluMobileFleetBaseInfo : FleetMetricsBaseInfo, IFleetTransforms
    {
        //Serialized
        //---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------

        //Non-Serialized
        //---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------------------------

        public ApkalluMobileFleetBaseInfo()
        {
            Cleanup();
        }

        protected override void Cleanup()
        {
            base.Cleanup();
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "ApkalluMobileFleetBaseInfo" );
            SerializeAllMetrics( MetaData, Buffer, SerializationCmdType );
        }

        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            Buffer.WriteHeaderStringToLogIfLoggingActive( "ApkalluMobileFleetBaseInfo" );
            DeserializeAllMetrics( MetaData, Buffer, SerializationCmdType );
        }

        public override void AddToTooltipForFleet( ArcenCharacterBufferBase buffer, TooltipDetail detailLevel )
        {
            if ( detailLevel >= TooltipDetail.Full )
                AppendFleetMetricsTooltip( buffer, BuildCompositionSnapshot() );
        }

        #region IFleetTransforms
        string IFleetTransforms.DisplayName { get { return "Flagship"; } }
        string IFleetTransforms.NoTransformsText { get { return "No flagship forms available. Breach Malware Nexuses to unlock forms."; } }

        bool IFleetTransforms.HasAnyTransforms
        {
            get
            {
                ApkalluFactionBaseInfo apkalluInfo = this.AttachedFleet?.Faction?.TryGetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
                if ( apkalluInfo == null ) return false;
                for ( int i = 0; i < apkalluInfo.CompletedBreaches.Count; i++ )
                    if ( apkalluInfo.CompletedBreaches[i]?.UnlockFlagshipForm != null ) return true;
                return false;
            }
        }

        private static readonly List<ApkalluFlagshipFormTarget> _unlockedFormsBuffer =
            List<ApkalluFlagshipFormTarget>.Create_WillNeverBeGCed( 8, "ApkalluMobileFleetBaseInfo-UnlockedForms" );

        System.Collections.Generic.IEnumerable<IFleetTransformTarget> IFleetTransforms.TypesCanSwitchTo
        {
            get
            {
                _unlockedFormsBuffer.Clear();
                ApkalluFactionBaseInfo apkalluInfo = this.AttachedFleet?.Faction?.TryGetExternalBaseInfoAs<ApkalluFactionBaseInfo>();
                if ( apkalluInfo != null )
                {
                    for ( int i = 0; i < apkalluInfo.CompletedBreaches.Count; i++ )
                    {
                        MalwareBreach breach = apkalluInfo.CompletedBreaches[i];
                        if ( breach?.UnlockFlagshipForm == null )
                            continue;
                        _unlockedFormsBuffer.Add( new ApkalluFlagshipFormTarget( breach ) );
                    }
                }
                return _unlockedFormsBuffer;
            }
        }

        GameCommand IFleetTransforms.CreateTransformCommand( GameCommandSource source, GameEntity_Squad centerpiece, string InternalName )
        {
            GameCommand command = GameCommand.Create( BaseGameCommand.CommandsByCode[BaseGameCommand.Code.TransformApkalluFlagship], source );
            command.RelatedEntityIDs.Add( centerpiece.PrimaryKeyID );
            command.RelatedString2 = InternalName;
            return command;
        }

        GameEntityTypeData IFleetTransforms.GetTypeDataForName( string InternalName )
        {
            MalwareBreach breach = MalwareBreachTable.Instance.GetRowByName( InternalName );
            return breach?.UnlockFlagshipForm;
        }

        void IFleetTransforms.GetTooltip( ArcenCharacterBufferBase buffer )
        {
            buffer.Add( "Spend ResourceOne to change the form of this flagship. Unlock forms by completing Malware Nexus Breaches." );
        }
        #endregion

        public class ApkalluFlagshipFormTarget : IFleetTransformTarget
        {
            private readonly MalwareBreach Breach;

            public ApkalluFlagshipFormTarget( MalwareBreach breach )
            {
                Breach = breach;
            }

            public GameEntityTypeData TypeData { get { return Breach.UnlockFlagshipForm; } }
            public string DisplayName { get { return Breach.UnlockFlagshipForm?.DisplayName ?? Breach.DisplayName; } }
            public string InternalName { get { return Breach.name; } }
            public int HackingCost { get { return 0; } }
            public int ResourceOneCost { get { return Breach.FlagshipFormTransformCostResourceOne; } }

            public bool CanTransformInto( Fleet fleet, out string InvalidReason )
            {
                GameEntity_Squad centerpiece = fleet.Centerpiece.GetSquad();
                if ( centerpiece != null && centerpiece.TypeData == Breach.UnlockFlagshipForm )
                {
                    InvalidReason = "Flagship is already in this form.";
                    return false;
                }
                if ( centerpiece != null && centerpiece.CurrentMarkLevel < Breach.FlagshipFormTransformMinMarkLevel )
                {
                    InvalidReason = "Requires flagship mark level " + Breach.FlagshipFormTransformMinMarkLevel + ".";
                    return false;
                }
                Faction faction = fleet.Faction;
                if ( faction != null && faction.StoredFactionResourceOne < Breach.FlagshipFormTransformCostResourceOne )
                {
                    InvalidReason = "Insufficient ResourceOne (need " + Breach.FlagshipFormTransformCostResourceOne + ").";
                    return false;
                }
                InvalidReason = null;
                return true;
            }
        }
    }
}
