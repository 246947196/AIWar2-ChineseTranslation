using System;
using Arcen.AIW2.Core;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.AIW2.External
{
    public class InSceneTextRenderer_Initializer : IArcenExternalCodeHookHandler
    {
        public void HandleExternalHook( object MainObject, object SecondaryObject, object[] AdditionalObjects, ArcenExternalCodeHook Hook, ArcenSimContextBase Context )
        {
            InSceneTextRenderer.Init();
        }
    }
    
    
    public class InSceneTextRenderer : MonoBehaviour
    {
        public struct DebugString 
        {
            public GameEntity_Base entity;
            public string text;
            public Color color;
            public float eraseTime;
        }
        
        public enum Code
        {
            IsAddAction = 0,
            IsClearAction
        }
        
        public struct DebugPos
        {
            public Code code; 
            public int cat;
            public int id;
            public Vector2 pos;
            public float size;
            public Color color;
            public float eraseTime;
        }
        
        //public List<GUIStyle> Styles = List<GUIStyle>.Create_WillNeverBeGCed(10, "InSceneTextRenderer.Styles");
        public List<DebugString> Strings = List<DebugString>.Create_WillNeverBeGCed(10, "InSceneTextRenderer.Strings");
        public ConcurrentQueue<DebugString> String_Queue = ConcurrentQueue<DebugString>.Create_WillNeverBeGCed("InSceneTextRenderer.String_Queue");
        
        public List<DebugPos> Positions = List<DebugPos>.Create_WillNeverBeGCed(10, "InSceneTextRenderer.Positions");
        public ConcurrentQueue<DebugPos> Pos_Queue = ConcurrentQueue<DebugPos>.Create_WillNeverBeGCed("InSceneTextRenderer.Pos_Queue");

        public void OnGUI()
        {
            UpdateStrings();
            UpdatePositions();
        }
        
        private void UpdateStrings()
        {
            float currentTime = ArcenTime.UnpausedTimeSinceStartForVisualEffectsF;
            
            while (String_Queue.TryDequeue(out DebugString str))
            {
                int idx = Strings.FindIndex(s => s.entity == str.entity);
                if (idx != -1)
                { 
                    var obj = Strings[idx];
                    obj.eraseTime = str.eraseTime;
                    obj.color = str.color;
                    obj.text = str.text;
                    Strings[idx] = obj;

                    continue;
                }

                Strings.Add( str );
            }

            for (int i = 0; i < Strings.Count; i++)
            {
                var str = Strings[i];
                if (str.eraseTime < currentTime)
                {
                    Strings.RemoveAt(i);
                    i--;
                    continue;
                }

                // seems quite wrong
                //Vector3 pos = str.entity.WorldLocation.ToVisualMainGameCoordinates_Unity(str.entity.Planet );

                // if i zoom in enough it seems correct, stays at that same screen position though as a zoom out, which is wrong
                Vector3 pos = str.entity.WorldLocation.ToVisualMainGameCoordinates_Numerics( str.entity.Planet ).ToUnityVector3();

                // i don't see how this could be wrong, the other cameras seem more off that it
                pos = ArcenVisualOrganizer.Instance.MainViewCamera.WorldToScreenPoint( pos );
                //pos = ArcenUI.Instance.guiCamera.WorldToScreenPoint( pos );
                // this is definitely correct, needed for IMGUI stuff, from forums
                pos.y = Screen.height - pos.y;

                Rect rect = new Rect();
                rect.position = pos;

                Content.text = str.text;
                rect.size = Style.CalcSize(Content);
                
                GUI.Box(rect, str.text, Style);
                //GUI.Label(rect, str.text, style);
            }
        }

        private void UpdatePositions()
        {
            float currentTime = ArcenTime.UnpausedTimeSinceStartForVisualEffectsF;
            Planet currentPlanet = Engine_AIW2.Instance.NonSim_GetPlanetBeingCurrentlyViewed();
            
            while (Pos_Queue.TryDequeue(out DebugPos I))
            {
                if (I.code == Code.IsClearAction)
                {
                    if (I.cat == -1 && I.id == -1)
                    {
                        Positions.Clear();
                    }
                    else
                    {
                        for (int i = 0; i < Positions.Count; i++)
                        {
                            var obj = Positions[i];
                            if (I.cat != -1 && obj.cat != I.cat)
                                continue;
                            if (I.id != -1 && obj.id != I.id)
                                continue;
                            
                            Positions.RemoveAt(i);
                            i--;
                        }
                            
                    }
                }
                else
                if (I.code == Code.IsAddAction)
                {
                    int idx = Positions.FindIndex(o => o.id == I.id);
                    if (idx != -1)
                    { 
                        var obj = Positions[idx];
                        obj.eraseTime = I.eraseTime;
                        obj.color = I.color;
                        obj.pos = I.pos;
                        Positions[idx] = obj;

                        continue;
                    }

                    Positions.Add( I );
                }
                else
                {
                    throw new NotImplementedException(string.Format("Code.{0} not handled!", I.code));
                }
            }

            for (int i = 0; i < Positions.Count; i++)
            {
                var obj = Positions[i];
                
                if ( currentPlanet == null ||
                     obj.eraseTime < currentTime )
                {
                    Positions.RemoveAt(i);
                    i--;
                    continue;
                }

                // seems quite wrong
                //Vector3 pos = str.entity.WorldLocation.ToVisualMainGameCoordinates_Unity(str.entity.Planet );

                // if i zoom in enough it seems correct, stays at that same screen position though as a zoom out, which is wrong
                Vector3 pos = obj.pos.ToArcenPoint().ToVisualMainGameCoordinates_Numerics( currentPlanet ).ToUnityVector3();

                // i don't see how this could be wrong, the other cameras seem more off that it
                pos = ArcenVisualOrganizer.Instance.MainViewCamera.WorldToScreenPoint( pos );
                //pos = ArcenUI.Instance.guiCamera.WorldToScreenPoint( pos );
                // this is definitely correct, needed for IMGUI stuff, from forums
                pos.y = Screen.height - pos.y;

                float yoff = obj.size / 4;
                float xoff = obj.size / 2;
                
                var minmin = new Vector2(pos.x - xoff, pos.y - yoff);
                var maxmax = new Vector2(pos.x + xoff, pos.y + yoff);
                var minmax = new Vector2(pos.x - xoff, pos.y + yoff);
                var maxmin = new Vector2(pos.x + xoff, pos.y - yoff);
                
                //if (_white == null)
                    //_white = MakeTex(4, 4, Color.white);
                
                GuiHelper.DrawLine(minmin, maxmax, obj.color, 2);
                GuiHelper.DrawLine(minmax, maxmin, obj.color, 2);
            }
        }

        public void DrawText(GameEntity_Base e, string txt, Color col, float duration)
        {
            //ArcenDebugging.ArcenDebugLogNoDateOrAnything(string.Format("InSceneTextRenderer.Draw for {0} with text {1}", e.ToString(), txt), DebugLogDestination.ArcenDebugLog, Verbosity.DoNotShow);
            var obj = new DebugString()
            {
                entity = e,
                text = txt,
                color = col,
                eraseTime = ArcenTime.UnpausedTimeSinceStartForVisualEffectsF + duration,
            };
            String_Queue.Enqueue(obj);
        }
        
        public void DrawPos(int cat, int id, Vector2 pos, float size, Color col, float duration)
        {
            var obj = new DebugPos()
            {
                code = Code.IsAddAction,
                cat = cat,
                id = id,
                pos = pos,
                color = col,
                size = size,
                eraseTime = ArcenTime.UnpausedTimeSinceStartForVisualEffectsF + duration,
            };
            Pos_Queue.Enqueue(obj);
        }
        
        public void Clear(int cat = -1, int id = -1)
        {
            var obj = new DebugPos()
            {
                code = Code.IsClearAction,
                cat = cat,
                id = id,
            };
            Pos_Queue.Enqueue(obj);
        }

        public static InSceneTextRenderer Instance;// = new InSceneTextRenderer();
        public static GUIStyle Style;
        public static GUIContent Content;
        
        public static void Init()
        {
            if (Instance != null)
                return;
            
            Content = new GUIContent();

            var obj = ArcenVisualOrganizer.Instance.MainViewCamera.transform.parent;
            Instance = obj.gameObject.AddComponent<InSceneTextRenderer>();
            
            Style = new GUIStyle()
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
            };

            var textcol = ColorMath.HexToColor("32CD32");
            Style.normal.textColor = textcol;
            
            var bgcol = Color.black;
            bgcol.a = 0.5f;

            Style.normal.background = MakeTex( 2, 2, bgcol);
        }

        private static Texture2D MakeTex( int width, int height, Color col )
        {
            Color[] pix = new Color[width * height];
            for( int i = 0; i < pix.Length; ++i )
            {
                pix[ i ] = col;
            }
            Texture2D result = new Texture2D( width, height );
            result.SetPixels( pix );
            result.Apply();
            return result;
        }
    }
    
    public static class GuiHelper
    {
        // The texture used by DrawLine(Color)
        private static Texture2D _coloredLineTexture;
     
        // The color used by DrawLine(Color)
        private static Color _coloredLineColor;
     
        /// <summary>
        /// Draw a line between two points with the specified color and a thickness of 1
        /// </summary>
        /// <param name="lineStart">The start of the line</param>
        /// <param name="lineEnd">The end of the line</param>
        /// <param name="color">The color of the line</param>
        public static void DrawLine(Vector2 lineStart, Vector2 lineEnd, Color color)
        {
            DrawLine(lineStart, lineEnd, color, 1);
        }
     
        /// <summary>
        /// Draw a line between two points with the specified color and thickness
        /// Inspired by code posted by Sylvan
        /// http://forum.unity3d.com/threads/17066-How-to-draw-a-GUI-2D-quot-line-quot?p=407005&viewfull=1#post407005
        /// </summary>
        /// <param name="lineStart">The start of the line</param>
        /// <param name="lineEnd">The end of the line</param>
        /// <param name="color">The color of the line</param>
        /// <param name="thickness">The thickness of the line</param>
        public static void DrawLine(Vector2 lineStart, Vector2 lineEnd, Color color, int thickness)
        {
            if (_coloredLineTexture == null || _coloredLineColor != color)
            {
                _coloredLineColor = color;
                _coloredLineTexture = new Texture2D(1, 1);
                _coloredLineTexture.SetPixel(0, 0, _coloredLineColor);
                _coloredLineTexture.wrapMode = TextureWrapMode.Repeat;
                _coloredLineTexture.Apply();
            }
            DrawLineStretched(lineStart, lineEnd, _coloredLineTexture, thickness);
        }
     
        /// <summary>
        /// Draw a line between two points with the specified texture and thickness.
        /// The texture will be stretched to fill the drawing rectangle.
        /// Inspired by code posted by Sylvan
        /// http://forum.unity3d.com/threads/17066-How-to-draw-a-GUI-2D-quot-line-quot?p=407005&viewfull=1#post407005
        /// </summary>
        /// <param name="lineStart">The start of the line</param>
        /// <param name="lineEnd">The end of the line</param>
        /// <param name="texture">The texture of the line</param>
        /// <param name="thickness">The thickness of the line</param>
        public static void DrawLineStretched(Vector2 lineStart, Vector2 lineEnd, Texture2D texture, int thickness)
        {
            Vector2 lineVector = lineEnd - lineStart;
            float angle = Mathf.Rad2Deg * Mathf.Atan(lineVector.y / lineVector.x);
            if (lineVector.x < 0)
            {
                angle += 180;
            }
     
            if (thickness < 1)
            {
                thickness = 1;
            }
     
            // The center of the line will always be at the center
            // regardless of the thickness.
            int thicknessOffset = (int)Mathf.Ceil(thickness / 2);
     
            GUIUtility.RotateAroundPivot(angle,
                                         lineStart);
            GUI.DrawTexture(new Rect(lineStart.x,
                                     lineStart.y - thicknessOffset,
                                     lineVector.magnitude,
                                     thickness),
                            texture);
            GUIUtility.RotateAroundPivot(-angle, lineStart);
        }
     
        /// <summary>
        /// Draw a line between two points with the specified texture and a thickness of 1
        /// The texture will be repeated to fill the drawing rectangle.
        /// </summary>
        /// <param name="lineStart">The start of the line</param>
        /// <param name="lineEnd">The end of the line</param>
        /// <param name="texture">The texture of the line</param>
        public static void DrawLine(Vector2 lineStart, Vector2 lineEnd, Texture2D texture)
        {
            DrawLine(lineStart, lineEnd, texture, 1);
        }
     
        /// <summary>
        /// Draw a line between two points with the specified texture and thickness.
        /// The texture will be repeated to fill the drawing rectangle.
        /// Inspired by code posted by Sylvan and ArenMook
        /// http://forum.unity3d.com/threads/17066-How-to-draw-a-GUI-2D-quot-line-quot?p=407005&viewfull=1#post407005
        /// http://forum.unity3d.com/threads/28247-Tile-texture-on-a-GUI?p=416986&viewfull=1#post416986
        /// </summary>
        /// <param name="lineStart">The start of the line</param>
        /// <param name="lineEnd">The end of the line</param>
        /// <param name="texture">The texture of the line</param>
        /// <param name="thickness">The thickness of the line</param>
        public static void DrawLine(Vector2 lineStart, Vector2 lineEnd, Texture2D texture, int thickness)
        {
            Vector2 lineVector = lineEnd - lineStart;
            float angle = Mathf.Rad2Deg * Mathf.Atan(lineVector.y / lineVector.x);
            if (lineVector.x < 0)
            {
                angle += 180;
            }
     
            if (thickness < 1)
            {
                thickness = 1;
            }
     
            // The center of the line will always be at the center
            // regardless of the thickness.
            int thicknessOffset = (int)Mathf.Ceil(thickness / 2);
     
            Rect drawingRect = new Rect(lineStart.x,
                                        lineStart.y - thicknessOffset,
                                        Vector2.Distance(lineStart, lineEnd),
                                        (float) thickness);
            GUIUtility.RotateAroundPivot(angle,
                                         lineStart);
            GUI.BeginGroup(drawingRect);
            {
                int drawingRectWidth = Mathf.RoundToInt(drawingRect.width);
                int drawingRectHeight = Mathf.RoundToInt(drawingRect.height);
     
                for (int y = 0; y < drawingRectHeight; y += texture.height)
                {
                    for (int x = 0; x < drawingRectWidth; x += texture.width)
                    {
                        GUI.DrawTexture(new Rect(x,
                                                 y,
                                                 texture.width,
                                                 texture.height),
                                        texture);
                    }
                }
            }
            GUI.EndGroup();
            GUIUtility.RotateAroundPivot(-angle, lineStart);
        }
    }
}