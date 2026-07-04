
using Arcen.AIW2.Core;
using Arcen.AIW2.External;
using Arcen.Universal;
using Arcen.Universal.Sprites;
using System;

using System.Text;

namespace Arcen.AIW2.External
{
    public class NanocaustNotifier : NotifierBaseDataSingleton
    {
        public static NanocaustNotifier Instance = new NanocaustNotifier();

        private static UnityEngine.Sprite sprite;
        private static bool hasInitialized = false;

        public static void InitIfNeeded()
        {
            if ( hasInitialized )
                return;
            hasInitialized = true;
            
            var dict = ExternalIconDictionaryTable.Instance.GetRowByName("Ships2");
            sprite = dict.GetGUISpriteByName("Nanobot_Hive");
        }

        public override MouseHandlingResult ClickHandler( NotifierFillData Data )
        {
            return MouseHandlingResult.DoNotPlayClickSound;
        }

        public override bool MouseoverHandler( NotifierFillData Data )
        {
            var faction = (Faction)Data.ObjectList[0];
            var timeLeft = (int)Data.Int64List[0];

            tooltipBuffer.Clear();
            tooltipBuffer.Add( " " ).AddFactionNameInItsColor( faction ).Add(" will invade in ").AddSecondsRemaining(timeLeft);

            Window_AtMouseTooltipPanelWide.bPanel.Instance.SetText( null, tooltipBuffer.GetStringAndResetForNextUpdate() );
            
            return true;
        }

        public override bool GetShouldBeHidden( NotifierFillData Data )
        {
            if (Data.ObjectList.Count == 0 || Data.ObjectList[0] == null)
                return true;

            return false;
        }

        public override bool ContentGetter( NotifierFillData Data, ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
        {
            int debugStage = -1;
            try
            {
                InitIfNeeded();

                Faction faction = Data.ObjectList.Count > 0 ? Data.ObjectList[0] as Faction : null;

                if (faction == null)
                    return false;

                var timeLeft = faction.InvasionTime - World_AIW2.Instance.GameSecond;

                if (timeLeft < 0)
                    return false;

                debugStage = 0;
                debugStage = 1;

                Image.UpdateWith( sprite, true, "NanocaustNotifier_sprite" );
                Image.SetColor( faction.FactionCenterColor.TeamColor );

                debugStage = 3;

                ArcenDoubleCharacterBuffer buffer = SubTexts[0].Text.StartWritingToBuffer();
                
                buffer.AddFactionNameInItsColor( faction );
                buffer.Add("\n");
                buffer.Add( "入侵" );

                SubTexts[0].Text.FinishWritingToBuffer();

                buffer = SubTexts[1].Text.StartWritingToBuffer();

                debugStage = 6;

                buffer.AddSecondsRemaining( timeLeft, TimeIntensity.OneMinute );

                debugStage = 13;
                SubTexts[1].Text.FinishWritingToBuffer();

            }
            catch ( Exception e )
            {
                ArcenDebugging.ArcenDebugLog( "Exception in NanocaustNotifier.ContentGetter at stage " + debugStage + ":" + e.ToString(), Verbosity.ShowAsError );
                return false;
            }
            return true;
        }
    }
}

