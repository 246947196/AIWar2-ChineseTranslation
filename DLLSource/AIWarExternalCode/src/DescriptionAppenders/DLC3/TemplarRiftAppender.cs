using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class TemplarRiftAppender : GameEntityDescriptionAppenderBase
    {
        public override void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer )
        {
            try
            {
                bool debug = GameSettings.Current.GetBoolBySetting( "Debug_Tooltip" ) || (Engine_AIW2.TraceAtAll && Engine_AIW2.TracingFlags.Has( ArcenTracingFlags.Templar ));
                if ( RelatedEntityTypeData == null )
                {
                    ArcenDebugging.ArcenDebugLogSingleLine( "No type data?", Verbosity.DoNotShow );
                    return;
                }

                TemplarPerUnitBaseInfo data = RelatedEntityOrNull.TryGetExternalBaseInfoAs<TemplarPerUnitBaseInfo>();
                if ( data == null )
                {
                    Buffer.Add( "No per unit data?" );
                    return;
                }
                Faction faction = RelatedEntityOrNull.GetFactionOrNull_Safe();
                TemplarFactionBaseInfo globaldata = faction.TryGetExternalBaseInfoAs<TemplarFactionBaseInfo>();
                if ( data.AvailableUpgrades.Count > 0 )
                {
                    Buffer.Add( "The Necromancer can get the following upgrades from hacking this:\n" );
                    for ( int i = 0; i < data.AvailableUpgrades.Count; i++ )
                    {
                        data.AvailableUpgrades[i].ToDisplayBuffer( Buffer );
                    }
                }
            }
            catch ( Exception ) { }
            return;
        }
    }
}
