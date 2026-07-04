using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    /// <summary>
    /// Small shared helpers for objective (intel menu) tooltips. The guiding rule: anything worth
    /// an objective should be visually identifiable, so its in-game icon appears in the description.
    /// </summary>
    public static class ObjectiveText
    {
        /// <summary>
        /// Leads a tooltip with the subject entity's inline icon (tinted with that entity's own
        /// faction colors) followed by its name and a blank line, e.g. "[icon] Superterminal\n\n".
        /// The icon is raised by its sprite markup, so it needs an empty line beneath it to avoid
        /// overlapping the body text; this header provides that. Chainable; safely no-ops if the
        /// entity is missing. Call it at the very start of the tooltip, then describe the subject
        /// as "it"/"this".
        /// </summary>
        public static ArcenCharacterBufferBase AddObjectiveEntityHeader( this ArcenCharacterBufferBase buffer, GameEntity_Squad entity, string nameColor )
        {
            if ( entity != null )
                buffer.AddShipIconInline( entity, TextStyle.Ship_Sprite_Ency ).Add( " " ).Add( entity.TypeData.GetDisplayName(), nameColor ).Add( "\n\n" );
            return buffer;
        }
    }
}
