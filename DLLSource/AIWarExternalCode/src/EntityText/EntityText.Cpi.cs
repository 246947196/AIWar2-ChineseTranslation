using Arcen.Universal;
using Arcen.AIW2.Core;
using System;
using Arcen.AIW2.External;
using System.Collections;

namespace Arcen.AIW2.External
{
    public struct CharacterPosInfo
    {
        public float CurrentPos;
        public StepConfig Config;

        public struct StepConfig
        {
            public readonly float PosPerStep;
            public readonly float PosPerStep_Twentieth;
            public readonly float PosPerStep_Tenth;
            public readonly float PosPerStep_Eighth;
            public readonly float PosPerStep_Fith;
            public readonly float PosPerStep_Quarter;
            public readonly float PosPerStep_Third;
            public readonly float PosPerStep_Half;
            public readonly float PosPerStep_TwentyPercent;
            public readonly float PosPerStep_ThirtyPercent;
            public readonly float PosPerStep_FortyPercent;
            public readonly float PosPerStep_SixtyPercent;
            public readonly float PosPerStep_SeventyPercent;
            public readonly float PosPerStep_EightyPercent;
            public readonly float PosPerStep_NinetyPercent;
            public readonly float PosPerStep_HundredTwentyFivePercent;
            public readonly float PosPerStep_HundredFiftyPercent;
            public readonly float PosPerStep_Double;
            
            public StepConfig(float posPerStep)
            {
                PosPerStep = posPerStep;
                PosPerStep_Twentieth = PosPerStep / 20;
                PosPerStep_Tenth = PosPerStep / 10;
                PosPerStep_Eighth = PosPerStep / 8;
                PosPerStep_Fith = PosPerStep / 5;
                PosPerStep_Quarter = PosPerStep / 4;
                PosPerStep_Third = PosPerStep / 3;
                PosPerStep_Half = PosPerStep / 2;
                PosPerStep_TwentyPercent = PosPerStep * 0.2f;
                PosPerStep_ThirtyPercent = PosPerStep * 0.3f;
                PosPerStep_FortyPercent = PosPerStep * 0.4f;
                PosPerStep_SixtyPercent = PosPerStep - PosPerStep_FortyPercent;
                PosPerStep_SeventyPercent = PosPerStep - PosPerStep_ThirtyPercent;
                PosPerStep_EightyPercent = PosPerStep - PosPerStep_TwentyPercent;
                PosPerStep_NinetyPercent = PosPerStep - PosPerStep_Tenth;
                PosPerStep_HundredTwentyFivePercent = PosPerStep * 1.25f;
                PosPerStep_HundredFiftyPercent = PosPerStep * 1.5f;
                PosPerStep_Double = PosPerStep * 2;
            }
        }

        public CharacterPosInfo(float posPerStep)
        {
            CurrentPos = 0;
            Config = new StepConfig(posPerStep);
        }

        #region Methods
        
        public CharacterPosInfo ResetPos()
        {
            CurrentPos = 0;
            return this;
        }
        
        public CharacterPosInfo ResetPos(float pos)
        {
            CurrentPos = pos;
            return this;
        }

        public CharacterPosInfo AddCustom(int steps)
        {
            CurrentPos += steps;
            return this;
        }

        public CharacterPosInfo AddCustomFloat( float posAmount )
        {
            CurrentPos += posAmount;
            return this;
        }

        public CharacterPosInfo AddStepFraction( float posAmount )
        {
            CurrentPos += posAmount * Config.PosPerStep;
            return this;
        }

        public CharacterPosInfo AddStep()
        {
            CurrentPos += Config.PosPerStep;
            return this;
        }

        public CharacterPosInfo AddStep_Double()
        {
            CurrentPos += Config.PosPerStep_Double;
            return this;
        }

        public CharacterPosInfo AddStep_Twentieth()
        {
            CurrentPos += Config.PosPerStep_Twentieth;
            return this;
        }

        public CharacterPosInfo AddStep_Tenth()
        {
            CurrentPos += Config.PosPerStep_Tenth;
            return this;
        }

        public CharacterPosInfo AddStep_Eighth()
        {
            CurrentPos += Config.PosPerStep_Eighth;
            return this;
        }

        public CharacterPosInfo AddStep_Fith()
        {
            CurrentPos += Config.PosPerStep_Fith;
            return this;
        }

        public CharacterPosInfo AddStep_Quarter()
        {
            CurrentPos += Config.PosPerStep_Quarter;
            return this;
        }

        public CharacterPosInfo AddStep_Third()
        {
            CurrentPos += Config.PosPerStep_Third;
            return this;
        }

        public CharacterPosInfo AddStep_Half()
        {
            CurrentPos += Config.PosPerStep_Half;
            return this;
        }

        public CharacterPosInfo AddStep_TwentyPercent()
        {
            CurrentPos += Config.PosPerStep_TwentyPercent;
            return this;
        }

        public CharacterPosInfo AddStep_ThirtyPercent()
        {
            CurrentPos += Config.PosPerStep_ThirtyPercent;
            return this;
        }

        public CharacterPosInfo AddStep_FourtyPercent()
        {
            CurrentPos += Config.PosPerStep_FortyPercent;
            return this;
        }

        public CharacterPosInfo AddStep_SixtyPercent()
        {
            CurrentPos += Config.PosPerStep_SixtyPercent;
            return this;
        }

        public CharacterPosInfo AddStep_SeventyPercent()
        {
            CurrentPos += Config.PosPerStep_SeventyPercent;
            return this;
        }

        public CharacterPosInfo AddStep_EightyPercent()
        {
            CurrentPos += Config.PosPerStep_EightyPercent;
            return this;
        }

        public CharacterPosInfo AddStep_NinetyPercent()
        {
            CurrentPos += Config.PosPerStep_NinetyPercent;
            return this;
        }

        public CharacterPosInfo AddStep_HundredTwentyFivePercent()
        {
            CurrentPos += Config.PosPerStep_HundredTwentyFivePercent;
            return this;
        }

        public CharacterPosInfo AddStep_HundredFiftyPercent()
        {
            CurrentPos += Config.PosPerStep_HundredFiftyPercent;
            return this;
        }
        
        #endregion
    }
}
