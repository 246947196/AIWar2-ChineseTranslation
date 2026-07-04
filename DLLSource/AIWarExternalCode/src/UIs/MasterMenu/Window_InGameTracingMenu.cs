using Arcen.Universal;
using Arcen.AIW2.Core;
using System;

using UnityEngine;

namespace Arcen.AIW2.External
{
    public class Window_InGameTracingMenu : StackMenuWindowController
    {
        public static Window_InGameTracingMenu Instance;
        public Window_InGameTracingMenu()
        {
            Instance = this;
            this.OnlyShowInGame = true;
        }

        public override void OnOpen()
        {
            this.myScale = 0.8f;
            base.OnOpen();
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.Add( "追踪" );
            }
        }

        public override string GetBriefName() { return "Tracing"; }

        private static List<IArcenUI_Button_Controller> buttonControllers = List<IArcenUI_Button_Controller>.Create_WillNeverBeGCed( 68, "Window_InGameTracingMenu-buttonControllers" );

        public class bsMenuSelectionRow : ButtonSetAbstractBase
        {
            public override void OnUpdate()
            {
                Faction localFaction = World_AIW2.Instance.GetLocalPlayerFactionOrNull();
                if ( localFaction == null )
                    return;
                ArcenUI_ButtonSet elementAsType = (ArcenUI_ButtonSet)this.Element;

                if ( elementAsType.Buttons.Count <= 0 )
                {
                    buttonControllers.Clear();

                    for ( var flag = (ArcenTracingFlags)1; flag < ArcenTracingFlags.Count; flag++ )
                    {
                        bItem newButtonController = new bItem( flag );
                        buttonControllers.Add( newButtonController );
                    }

                    buttonControllers.StableSort( static (a, b) =>(a as bItem).Name.CompareTo((b as bItem).Name));

                    LayOutButtonSetAndSizeCanvasAndSelfHeight( elementAsType, buttonControllers );
                }
            }
        }

        private class bItem : ButtonAbstractBase
        {
            public readonly ArcenTracingFlags Flag;
            public readonly string Name;

            public bItem( ArcenTracingFlags Flag )
            {
                this.Flag = Flag;
                this.Name = Flag.ToString();
                this.Name = Name.Substring(Name.IndexOf('.')+1);
            }

            public bool IsEnabled
            {
                get
                {
                    return Engine_AIW2.TracingFlags.Has( this.Flag );
                }
            }

            public override bool GetShouldBeGrayedOut() { return false; }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                base.GetTextToShowFromVolatile( buffer );

                if ( IsEnabled )
                    buffer.StartColor(Color.green);
                buffer.Add( Name );
                if ( IsEnabled )
                    buffer.EndColor();
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( Engine_AIW2.TracingFlags.Has( this.Flag ) )
                    Engine_AIW2.TracingFlags.Remove( this.Flag );
                else
                    Engine_AIW2.TracingFlags.Add( this.Flag );

                Engine_AIW2.TraceAtAll = Engine_AIW2.TracingFlags.HasAny();

                return MouseHandlingResult.None;
            }
        }
    }
}
