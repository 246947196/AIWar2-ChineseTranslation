using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public abstract class GameEntityDescriptionAppenderBase : IGameEntityDescriptionAppender
    {
        public abstract void AddToDescriptionBuffer( GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer );
        
        //this is unlikely to be needed, but just in case!
        public virtual void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap() { }
    }
}
