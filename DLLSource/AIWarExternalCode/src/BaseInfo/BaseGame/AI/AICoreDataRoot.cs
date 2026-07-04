using Arcen.AIW2.Core;
using Arcen.Universal;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    //This sub-class exists just to provide a base that Sentinels can also share without getting into
    //all the stuff that Hunter, Warden, and Praetorian do.
    //
    //We could also layer this under central data for subfactions like Relentless Waves, CPA, or Border Aggression...
    //except for those sub-factions don't actually have this kind of central data!  They keep all their data on their
    //own personal classes.
    //
    //Why the difference?  Well, I'd sayt the main reason is that those subfactions all deal only with units and ordering them around.
    //The classes that inherit from AICoreDataRoot deal not only with units, but also with budgets and that part of the heart of the AI.
    //Something like border aggression isn't budgeted-for in the same way; rather, it gets handed units and then does what it does.
    public abstract class AICoreDataRoot
    {
        public readonly AISentinelsFactionBaseInfo BaseInfoOfParentSentinels;
        public AICoreDataRoot( AISentinelsFactionBaseInfo BaseInfo )
        {
            this.BaseInfoOfParentSentinels = BaseInfo;
            Cleanup();
        }
        public abstract void Cleanup();

        #region Tracing
        protected bool tracing_shortTerm;

        protected bool tracing_longTerm;

        protected abstract string TracingNameRoot { get; }

        public string TracingName
        {
            get { return this.TracingNameRoot + " for AI Idx " + this.BaseInfoOfParentSentinels.AttachedFaction.FactionIndex; }
        }
        #endregion
    }
}
