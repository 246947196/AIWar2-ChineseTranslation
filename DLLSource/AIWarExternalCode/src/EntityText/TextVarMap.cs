using System;
using System.Linq;
using Arcen.Universal;
using Arcen.AIW2.Core;
using Sys=System.Collections.Generic;

namespace Arcen.AIW2.External
{
    #region Related Types
    public enum VarMode
    {
        Normal,
        Open,
        Close,
    }

    public struct FormatArgs
    {
        public ArcenCharacterBufferBase Buffer;
        
        public GetStyle_Method GetStyle;
        public EvalCondition_Method EvalCond;
        public AppendVarValue_Method AppendVar;
        
        public object Args;
        
        public ObjectList Caller;
        
        public int CallerIndex;
        
        public Log Logger;
        
        public static FormatArgs Alloc(
            ArcenCharacterBufferBase buffer=null, 
            AppendVarValue_Method append=null, 
            GetStyle_Method getStyle=null, 
            EvalCondition_Method evalCond=null,
            object args=null, 
            ObjectList caller=null,
            Log logger=null)
        {
            return new FormatArgs()
            {
                Buffer = buffer,
                GetStyle = getStyle,
                EvalCond = evalCond,
                AppendVar = append,
                Args = args,
                Caller = caller,
                CallerIndex = 0,
                Logger = logger,
            };
        }
    }
    
    public struct MapVar
    {
        public string Name;
        public TextVarMap Parent_Map;
        
        public string Condition;
        
        public string Value;
        public string Map_Name;
        public TextVarMap Map_Obj;
        
        public string Style_Name;
        public TextStyle Style_Obj;
        //public ArcenXMLElement XmlStyle;

        public TextStyle Get_Style(FormatArgs args)
        {
            TextStyle style = this.Style_Obj;
            int debugstage = 0;
            try
            {
                debugstage = 100;
                if (style == null)
                {
                    debugstage = 101;
                    if (this.Style_Name != null) 
                    {
                        debugstage = 102;
                        style = TextStyle.Get(this.Style_Name);
                        
                        debugstage = 103;
                        if (style != null)
                        {
                            debugstage = 104;
                            this.Style_Obj = style;
                            debugstage = 105;
                            this.Parent_Map.Vars[this.Name] = this;
                            debugstage = 106;
                            args.Logger?.AppendFormat("TextVarMap '{0}' Var '{1}' Style_Obj assigned '{2}' from global.\n", 
                                                      Parent_Map.InternalName, this.Name, this.Style_Obj.InternalName);
                            debugstage = 107;
                        }
                    }
                }
                
                debugstage = 200; 
                if (style == null)
                {
                    debugstage = 201;
                    if (args.CallerIndex > 1)
                    {
                        debugstage = 202;
                        args.CallerIndex--;
                        var caller = args.Caller[args.CallerIndex] as TextVarMap;
                        
                        debugstage = 203;
                        MapVar var;
                        if ( caller.Vars.TryGetValue( this.Name, out var ) )
                        {
                            debugstage = 204;
                            style = var.Get_Style( args );
                            
                            debugstage = 205;
                            if (style != null)
                            {
                                debugstage = 206;
                                this.Style_Obj = style;
                                debugstage = 207;
                                this.Parent_Map.Vars[this.Name] = this;
                                debugstage = 208;
                                args.Logger?.AppendFormat("TextVarMap '{0}' Var '{1}' Style_Obj assigned '{2}' from caller '{3}'.\n", 
                                                      Parent_Map.InternalName, this.Name, this.Style_Obj.InternalName, caller.InternalName);
                            }
                        }
                        
                        args.CallerIndex++;
                    }
                }

                debugstage = 300;
                
                // This only occurs on the final result not when asking higher up maps to try resolving it
                // rather we pass whatever it was resolved to (or our own thing) through this final GetStyle method.
                if (args.CallerIndex == (args.Caller.Count-1) &&
                    args.GetStyle != null)
                {
                    debugstage = 301;
                    style  = args.GetStyle(this.Style_Name, style, args.Args);
                    
                    debugstage = 302;
                    if (style != null)
                        return style;
                }
                
                debugstage = 400;
                return this.Style_Obj;
            }
            catch (Exception e)
            {
                LOG.Err("Exception at debugstage {0}:\n{1}", debugstage, e);
                return this.Style_Obj;
            }
        }
        
        public TextVarMap Get_Map(FormatArgs args)
        {
            if (this.Map_Obj == null)
            {
                for (int i = args.CallerIndex; i >= 0; i--)
                {
                    var caller = args.Caller[i] as TextVarMap;
                    if (caller.LocalMaps.TryGetValue(this.Map_Name, out this.Map_Obj))
                    {
                        args.Logger?.AppendFormat("TextVarMap '{0}' Var '{1}' Map_Obj assigned '{2}' from caller '{3}'.\n", 
                                                  Parent_Map.InternalName, this.Name, this.Map_Obj.InternalName, caller.InternalName);
                        
                        this.Parent_Map.Vars[this.Name] = this;
                        
                        return this.Map_Obj;
                    }
                }
                
                this.Map_Obj = TextVarMap.Get(this.Map_Name);
                if (this.Map_Obj != null)
                {
                    args.Logger?.AppendFormat("TextVarMap '{0}' Var '{1}' Map_Obj assigned '{2}' from global.\n", 
                                                  Parent_Map.InternalName, this.Name, this.Map_Obj.InternalName);
                    
                    this.Parent_Map.Vars[this.Name] = this;
                    
                    return this.Map_Obj;
                }
            }
            
            return this.Map_Obj;
        }
        
        public bool Skip(FormatArgs args)
        {
            if (args.EvalCond != null && 
                !string.IsNullOrEmpty(this.Condition))
            {
                bool show = args.EvalCond(this.Condition, args.Args);
                if (!show)
                    return true;
            }
            
            return false;
        }
    }
    #endregion
    
    #region TextVarMap
    
    public class TextVarMap : ArcenDynamicTableRow, IConcurrentPoolable<TextVarMap>, IProtectedListable
    {
        #region Static
        
        public static TextVarMap Inline_Ship_Format;
        public static TextVarMap Inline_Ship_Format_Building;
        public static TextVarMap Inline_Ship_Format_Target;
        public static TextVarMap Larger_Ship_Format;
        public static TextVarMap Ship_Values;
        public static TextVarMap HackerFleet_Values;
        
        public static TextVarMap Hack_Line_Label_Format; // also an inline ship, in essence
        public static TextVarMap Hack_Item_Label_Format; // stub
        public static TextVarMap Hack_ConfirmPrompt_Format;
        public static TextVarMap Hack_ConfirmPrompt_SuppressMessage;
        
        public static TextVarMap Tooltip_Header_Ship_Format;
        
        public static TextVarMap Build_Stat_Row;
        public static TextVarMap Stat_NA_Format;
        public static TextVarMap Stat_Conjunction;
        
        public static TextVarMap Base_Wrap;
        public static TextVarMap Parenthetical;
        public static TextVarMap InParenthesis;
        public static TextVarMap InBrackets;
        public static TextVarMap Build_Req_Status;
        
        public static TextVarMap InputAction;
        
        public static TextVarMap Tooltip_Hotkeys_Footer_Line;
        
        public static TextVarMap Tooltip_Weapon_Line_Format;
        public static TextVarMap Tooltip_DroneGun_Weapon_Line_Format;
        public static TextVarMap Weapon_Activity_Format;
        public static TextVarMap Salvo_Format;
        public static TextVarMap System_Targeting_Format;
        
        public static TextVarMap Progenitor_Format;
        
        public static TextVarMap Quickstart_Category_Format;
        public static TextVarMap Quickstart_Format;
        
        public static TextVarMap Wave_Note_Format;
        public static TextVarMap Attack_Note_Format;
        
        public static bool log = false;
        
        public static TextVarMap Get(string name)
        {
            var res = TextVarMapTable.Instance.GetRowByName(name, LookupSwapAllowed.No, true);
            return res;
        }

        #endregion

        #region Data

        public enum ElementType
        {
            Text,
            Var,
        }
        
        public class Element
        {
            public ElementType Type;
            public string Text;
            public VarMode Mode;
            public MapVar Map;
        }
        
        public class Line
        {
            public string Text;
            public Sys.List<Element> Elements = new Sys.List<Element>();
        }
        
        public struct Condition
        {
            public string Name;
            public bool IsNegated;
        }
        
        public TextVarMap Parent;
        public string _condition;
        public Sys.List<Condition> Conditions = new Sys.List<Condition>();
        public Sys.List<Line> Lines = new Sys.List<Line>();
        public Sys.Dictionary<string,Line> LinesById = new Sys.Dictionary<string,Line>();
        public Sys.Dictionary<string,MapVar> Vars = new Sys.Dictionary<string,MapVar>();
        public Sys.Dictionary<string,TextStyle> LocalStyles = new Sys.Dictionary<string,TextStyle>();
        public Sys.Dictionary<string,TextVarMap> LocalMaps = new Sys.Dictionary<string,TextVarMap>();
        private readonly bool IsUnpooled;

        #endregion

        #region Methods

        public void AddVarReplace( FormatArgs args )
        {
            args.Logger?.AppendFormat("map {0} addvarreplace\n", this.InternalName);
            
            if (this.Conditions != null && args.EvalCond != null)
            {
                foreach (var con in this.Conditions)
                {
                    bool istrue = args.EvalCond(con.Name, args.Args);
                    if (con.IsNegated)
                        istrue = !istrue;
                    
                    args.Logger?.AppendFormat("{0} cond {1} is {2}\n", this.InternalName, con, istrue);
                    
                    if (!istrue)
                        return;
                }
            }
            
            if ( args.Caller == null )
            {
                args.Caller = ObjectList.GetTemporary( "TextVarMap.Caller", 5.0f );
                if ( args.Caller == null ) //blocked for teardown/shutdown; bail
                    return;
            }
            args.Caller.Add( this );
            if (args.Caller.Count > 1)
                args.CallerIndex++;
            
            foreach (var line in this.Lines)
            {
                AddVarReplace( line, args );
            }
            
            if (args.Caller.Count > 1)
                args.CallerIndex--;
            args.Caller.RemoveAt(args.Caller.Count-1);
            if (args.Caller.Count == 0)
            {
                ObjectList.ReleaseTemporary(args.Caller);
                args.Caller = null;
            }
        }

        public void AddVarReplace( Line Line, FormatArgs args )
        {
            var log = args.Logger;
            log?.AppendFormat("line addvarreplace : \"{0}\"\n", Line.Text);
            
            int debugstage = 0;
            try
            {
                var Buffer = args.Buffer;
             
                debugstage = 100;
                foreach (var e in Line.Elements)
                {
                    debugstage = 101;

                    if (e.Type == TextVarMap.ElementType.Text)
                    {
                        debugstage = 200;
                        Buffer.Add(e.Text);
                        log?.AppendFormat("text element : \"{0}\"\n", e.Text);
                    }
                    else 
                    if (e.Type == TextVarMap.ElementType.Var)
                    {
                        debugstage = 300;
                        
                        var vmap = e.Map;
                        bool istrue = !vmap.Skip(args);
                        log?.AppendFormat("var element : \"{0}\" cond={1} skip={2}\n", vmap.Name, vmap.Condition.OrNull(), !istrue);
                        if (!istrue)
                            continue;

                        using (var s = vmap.Get_Style(args))
                        {
                            debugstage = 330;
                            if (e.Mode == VarMode.Open)
                            {
                                s?.Open(Buffer, false, true);
                                continue;
                            }
                            
                            debugstage = 340;
                            if (e.Mode == VarMode.Close)
                            {
                                debugstage = 341;
                                s?.Close(Buffer, true, true);
                                
                                continue;
                            }
                            
                            debugstage = 350;
                            if (e.Mode == VarMode.Normal)
                            {
                                debugstage = 351;
                                s?.Open(Buffer, false, true);
                                
                                debugstage = 352;
                                if (!string.IsNullOrEmpty(vmap.Map_Name))
                                {
                                    debugstage = 353;
                                    var map = vmap.Get_Map(args);
                                    //log?.AppendFormat(" element : \"{0}\" cond={1} skip={2}\n", vmap.Name, vmap.Condition.OrNull(), !istrue);

                                    if (map != null)
                                    {
                                        debugstage = 356;
                                        map.AddVarReplace( args );
                                    }
                                    else
                                    {
                                        debugstage = 360;
                                        Buffer.Add(e.Text);
                                    }
                                    
                                    debugstage = 370;
                                    s?.Close(Buffer, true, true);
                                    
                                    continue;
                                }
                                
                                debugstage = 400;
                                if (!string.IsNullOrEmpty(vmap.Value))
                                {
                                    debugstage = 401;
                                    Buffer.Add(vmap.Value);
                                    debugstage = 402;
                                    s?.Close(Buffer, true, true);
                                    
                                    continue;
                                }

                                debugstage = 500;
                                if (args.AppendVar == null)
                                {
                                    debugstage = 501;
                                    Buffer.Add(e.Text);
                                }
                                else
                                {
                                    debugstage = 502;
                                    
                                    var a = Buffer.Builder.Length;
                                    args.AppendVar(e.Text, s, Buffer, args.Args);
                                    var b = Buffer.Builder.Length;
                                    log?.AppendFormat("var {0} AppendVar wrote \"{1}\"\n", vmap.Name, Buffer.Builder.SubStr(a, b-a));
                                }

                                debugstage = 503;
                                s?.Close(Buffer, true, true);
                            }
                        }
                    }
                }
                
                //debugstage = 600;
                //args.Caller.RemoveAt(args.Caller.Count-1);
                
                //debugstage = 605;
                //if (args.Caller.Count == 0)
                //{
                //    debugstage = 606;
                //    ObjectList.ReleaseTemporary(args.Caller);
                //    args.Caller = null;
                //}
                                    
                debugstage = 1000;
            }
            catch ( Exception e )
            {
                LOG.Err( "exception in AddVarReplace debugstage={0}\n{1}", debugstage, e );
            }
        }
        
        public void Setup()
        {
            if (log) LOG.Msg("{0}.Setup called.", this.InternalName.OrNull());
            
            foreach (var pair in Vars.ToArray())
            {
                var Var = pair.Value;
                if (Var.Style_Obj == null)
                {
                    // then look up the parent heirarchy for any same named var
                    // that does have a StyleObj or Style-Name.
                    if (Var.Style_Name == null)
                    {
                        
                    }
                    // look for local style then up the parent heirarchy
                    // for one with this name
                    else
                    {
                        
                    }
                }
                
                if (Var.Map_Obj == null)
                {
                    // then look up the parent heirarchy for any same named var
                    // that does have a MapObj or Map-Name.
                    if (Var.Map_Name == null)
                    {
                        
                    }
                    // look for local map then up the parent heirarchy
                    // for one with this name
                    else
                    {
                        for (var map = this; map != null; map = map.Parent)
                        {
                            if (this.LocalMaps.TryGetValue(Var.Map_Name, out Var.Map_Obj))
                            {
                                // done
                                break;
                            }
                        }
                    }
                }
            }

            foreach (var pair in LocalMaps.ToArray())
            {
                var map = pair.Value;
                map.Setup();
            }

            Conditions.Clear();
            if (!string.IsNullOrEmpty(this._condition))
            {
                var terms = _condition.Split(',');
                foreach (var i in terms)
                {
                    if (!string.IsNullOrWhiteSpace(i))
                    {
                        var c = new Condition();
                        c.Name = i;
                        
                        if (i.StartsWith("!"))
                        {
                            c.IsNegated = true;
                            c.Name = i.TrimStart('!');
                        }
                        
                        Conditions.Add(c);
                    }
                }
            }
            
            foreach (var line in Lines)
            {
                PreprocessLine(line);
            }
            
            if (!string.IsNullOrEmpty(InternalName))
            {
                var field = typeof(TextVarMap).GetField(InternalName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase );
                if (field != null)
                {
                    if (log) LOG.Msg("Set TextVarMap.{0}", InternalName);
                    field.SetValue(null, this);
                }
            }
            
            if (log) LOG.Msg("done {0}.Setup", this.InternalName.OrNull());
        }
        
        private void PreprocessLine(Line line)
        {
            Log log = null;
            if (TextVarMap.log) log = Log.Yes;
            
            log?
                .Append("PreprocessLine called.\n")
                .Append("  ").Append(line.Text).Append("\n");

            line.Elements.Clear();
            
            if (EntityText.DumpNextTooltipText != EntityText.DebugAction.Null &&
                log == null)
            {
                log = Log.Yes;
            }

            var format = line.Text;
            
            if (string.IsNullOrEmpty(format))
            {
                log?.Append("  Is empty.\n");
                goto done;
            }
            
            int cur = 0;

            while (cur < format.Length)
            {
                int nex = format.IndexOf("{", cur);
                if (nex == -1)
                {
                    log?.Append("  No '{' found.\n");
                    log?.Append("  Appending '").Append(format, cur, format.Length-cur);
                    
                    var e = new Element()
                    {
                        Type = ElementType.Text,
                        Text = format.Substring(cur, format.Length-cur),
                    };
                    line.Elements.Add(e);

                    goto done;
                }
            
                log?.Append("  Found '{' ").Append( nex - cur ).Append( " characters later.\n");
                if (nex-cur > 0)
                {
                    log?.Append("  Appending '").Append(format, cur, nex - cur).Append("'\n");
                    {
                        var e = new Element()
                        {
                            Type = ElementType.Text,
                            Text = format.Substring(cur, nex - cur),
                        };
                        line.Elements.Add(e);
                    }
                }

                cur = nex + 1;
                
                VarMode mode = VarMode.Normal;
                if (format[cur] == ':')
                {
                    mode = VarMode.Close;
                    cur++;
                }

                int close = format.IndexOf("}", cur);
                if (close == -1)
                {
                    log?.Append("  No closing '}' ");
                    if (format.Length-nex > 0)
                    {
                        log?.Append("  Appending '").Append(format, nex, format.Length-nex);
                        
                        var e = new Element()
                        {
                            Type = ElementType.Text,
                            Text = format.Substring(nex, format.Length-nex),
                        };
                        line.Elements.Add(e);
                    }
                    goto done;
                }
                
                nex = close + 1;
                
                if (format[close-1] == ':')
                {
                    mode = VarMode.Open;
                    close--;
                }

                string varname = format.Substring(cur, close-cur);

                MapVar var;
                if (!this.Vars.TryGetValue(varname, out var))
                {
                    var = new MapVar()
                    {
                        Name = varname,
                        Parent_Map = this,
                        Style_Name = varname,
                    };

                    this.Vars[varname] = var;
                }

                // add it
                log?.Append("  Adding '").AppendFormat("{0}.{1}", var.Name, mode ).Append("'\n");
                {
                    var e = new Element()
                    {
                        Type = ElementType.Var,
                        Text = varname,
                        Mode = mode,
                        Map = var,
                    };
                    line.Elements.Add(e);
                }
                
                cur = nex;
            }
            
            done:
            
            log?.Flush();
            
            return;
        }
        
        public void Clear()
        {
            if (TextVarMap.log) LOG.Msg("TextVarMap.Clear() called for '{0}' from:\n{1}", this.InternalName, LOG.StackTrace(8));
            
            this.Parent = null;
            this._condition = null;
            this.Conditions.Clear();
            this.Lines.Clear();
            this.LinesById.Clear();
            this.Vars.Clear();

            foreach (var pair in LocalStyles)
            {
                var s = pair.Value;
                s?.ReturnToPool();
            }
            this.LocalStyles.Clear();
            
            foreach (var pair in LocalMaps)
            {
                var s = pair.Value;
                s?.ReturnToPool();
            }
            this.LocalMaps.Clear();
        }

        #endregion

        #region Pooling
        private static ReferenceTracker RefTracker;
        private TextVarMap() 
            : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "VarStyle" );
            RefTracker.IncrementObjectCount();
        }
        private TextVarMap(bool untracked) 
            : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            IsUnpooled = true;
        }

        private static ConcurrentPool<TextVarMap> Pool = new ConcurrentPool<TextVarMap>( "VarStyle", 99999,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new TextVarMap(); } );

        public static TextVarMap GetFromPoolOrCreate()
        {
            var map = Pool.GetFromPoolOrCreate();
            if (map != null)
                map.Clear();
            return map;
        }
        
        public static TextVarMap GetUnpooled()
        {
            return new TextVarMap(false);
        }

        public override void ReturnToPool()
        {
            if (IsUnpooled)
                return;
            
            Pool.ReturnToPool( this );
        }

        protected override void InnerSetToDefaults()
        {
            this.Clear();
        }
        
        
        #endregion
    }

    #endregion

    #region TextVarMapTable

    public class TextVarMapTable : ArcenDynamicTable<TextVarMap>
    {
        public static TextVarMapTable Instance;
        public TextVarMapTable() : base( "TextVarMaps", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        protected override void PrepForCompleteReloadLater()
        {
            if (TextVarMap.log) LOG.Msg("TextVarMapTable.PrepForCompleteReloadLater() called.");
            
            var members = typeof(TextVarMap).Members(MemberType.Field, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase );
            foreach (var mem in members)
            {
                if (TextVarMap.log) LOG.Msg("Cleared TextVarMap.{0}", mem.Name);
                mem.SetValue(null, null);
            }
            
            this.Rows.Clear(true);
        }

        public override void Initialize()
        {
            if (TextVarMap.log) LOG.Msg("TextVarMapTable.Initialize() called.");

            base.Initialize();
            
            SetupRows();
        }

        public override void ReloadSelectData()
        {
            if (TextVarMap.log) LOG.Msg("TextVarMapTable.ReloadSelectData() called.");
            
            foreach ( var r in Rows )
                r.Clear();

            base.ReloadSelectData();
            
            SetupRows();
        }
        
        private void SetupRows()
        {
            if (TextVarMap.log) LOG.Msg("TextVarMapTable.SetupRows() called from:\n{0}", LOG.StackTrace());
            
            foreach (var r in Rows)
            {
                r.Setup();
            }
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, TextVarMap Map )
        {
            ProcessElement(Data, Map);
            return DelReturn.Continue;
        }

        public override DelReturn NodeSelectReProcessor( ArcenXMLElement Data, TextVarMap Map )
        {
            ProcessElement(Data, Map);
            return DelReturn.Continue;
        }

        private void ProcessElement( ArcenXMLElement Data, TextVarMap Map )
        {
            //LOG.Msg("ProcessElement for {0}: {1}", Map.InternalName, Data.LimitedInnerText);
            
            Data.Fill("cond", ref Map._condition, false);
            
            foreach ( ArcenXMLElement e in Data.ChildrenOfType_AndParentsAndPartials( "line" ) )
                {
                    string id = string.Empty;
                    e.Fill( "id", ref id, true );
                    string text = string.Empty;
                    e.Fill( "text", ref text, true );

                    //LOG.Msg("element 'line': {0}; id={1}, text={2}", e.LimitedInnerText, id, text);

                    TextVarMap.Line line;
                    if (!Map.LinesById.TryGetValue(id, out line))
                    {
                        //LOG.Msg("is a new line");
                        line = new TextVarMap.Line();
                        Map.Lines.Add(line);
                        Map.LinesById[id] = line;
                    }

                    text = text.Replace("\\n", "\n");
                    text = text.Replace("\\t", "\t");
                    line.Text = text;
                }
            
            
            foreach ( ArcenXMLElement e in Data.ChildrenOfType_AndParentsAndPartials( "map" ) )
                {
                    var name = e.GetString("name", null, false);
                    if (string.IsNullOrEmpty(name))
                    {
                        LOG.Err("Error: map '{0}' has inner <map> without required 'name' attribute.", Map.InternalName);
                        continue;
                    }

                    if (Map.LocalMaps.ContainsKey(name))
                    {
                        LOG.Err("Error: map '{0}' has inner <map name=\"{1}\"> more than once.", Map.InternalName, name);
                        continue;
                    }

                    var inner_map = TextVarMap.GetUnpooled();
                    inner_map.OriginalXmlData = e;
                    inner_map.InternalName = Map.InternalName + "_" + name + "_Map_auto";
                    inner_map.Parent = Map;

                    if (TextVarMap.log) LOG.Msg("TextVarMap '{0}' LocalMaps[{1}] assigned {2}.", Map.InternalName, name, inner_map.InternalName);

                    Map.LocalMaps[name] = inner_map;

                    ProcessElement(e, inner_map);
                }
            
            foreach ( ArcenXMLElement e in Data.ChildrenOfType_AndParentsAndPartials( "var_map" ) )
                {
                    string varname = null;
                    e.Fill( "var", ref varname, true );
                    string stylename = null;
                    e.Fill( "style", ref stylename, false );
                    string value = null;
                    e.Fill( "value", ref value, false );
                    string cond = null;
                    e.Fill( "cond", ref cond, false );

                    string value_map_name = null;
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (value.StartsWith("{") &&
                            value.EndsWith("}"))
                        {
                            value_map_name = value.Trim('{', '}');
                        }
                    }

                    var Var = new MapVar()
                    {
                        Name = varname,
                        Parent_Map = Map,

                        Condition = cond,

                        Value = value,
                        Map_Name = value_map_name,

                        Style_Name = stylename,
                    };

                    foreach ( ArcenXMLElement e2 in e.ChildrenOfType_AndParentsAndPartials( "style" ) )
                        {
                            var inner_style = TextStyle.GetUnpooled();
                            inner_style.OriginalXmlData = e2;
                            inner_style.InternalName = Map.InternalName + "_" + varname + "Var_Style_auto";

                            Map.LocalStyles[inner_style.InternalName] = inner_style;

                            if (TextVarMap.log)
                                LOG.Msg("TextVarMap '{0}' LocalStyles[{1}] assigned for Var '{2}'.",
                                    Map.InternalName, inner_style.InternalName, Var.Name);

                            TextStyleTable.Instance.ProcessElement(e2, inner_style);
                            inner_style.Setup();

                            Var.Style_Obj = inner_style;

                            break;
                        }

                    if (Var.Map_Name != null)
                    {
                        if (Map.LocalMaps.TryGetValue( Var.Map_Name, out Var.Map_Obj ))
                        {
                            if (TextVarMap.log)
                                LOG.Msg("TextVarMap '{0}' Var '{1}' with map name '{2}' assigned from LocalMaps.",
                                    Map.InternalName, Var.Name, Var.Map_Name);
                        }
                    }

                    Map.Vars[varname] = Var;
                }
        }

        public override TextVarMap GetNewRowFromPool()
        {
            return TextVarMap.GetFromPoolOrCreate();
        }
    }
    
    public class TextVarMapTable_Hooks : IArcenExternalCodeHookHandler
    {
        void IArcenExternalCodeHookHandler.HandleExternalHook( object MainObject, object SecondaryObject, object[] AdditionalObjects,
            ArcenExternalCodeHook Hook, ArcenSimContextBase Context )
        {
            if (Hook.InternalName == "ReloadSelectData")
            {
                TextVarMapTable.Instance.ReloadSelectData();
            }
        }  
    }

    #endregion

    #region Extensions
    
    public delegate TextStyle GetStyle_Method(string name, TextStyle style, object args);
    public delegate void AppendVarValue_Method(string name, TextStyle style, ArcenCharacterBufferBase buffer, object args);
    public delegate bool EvalCondition_Method(string cond, object args);
    
    public static partial class Extensions
    {
        public static ArcenCharacterBufferBase AddVarReplace(this ArcenCharacterBufferBase buffer, TextVarMap map )
        {
            map?.AddVarReplace(FormatArgs.Alloc(buffer));
            return buffer;
        }
        
        public static ArcenCharacterBufferBase AddVarReplace(this ArcenCharacterBufferBase buffer, TextVarMap map, int val )
        {
            map?.AddVarReplace(FormatArgs.Alloc(buffer, append:(a,b,c,d)=>c.Add((int)val), args:val));
            return buffer;
        }
        
        public static ArcenCharacterBufferBase AddVarReplace(this ArcenCharacterBufferBase buffer, TextVarMap map, FInt val )
        {
            map?.AddVarReplace(FormatArgs.Alloc(buffer, append:(a,b,c,d)=>c.Add((FInt)val), args:val));
            return buffer;
        }
        
        public static ArcenCharacterBufferBase AddVarReplace(this ArcenCharacterBufferBase buffer, TextVarMap map, float val )
        {
            map?.AddVarReplace(FormatArgs.Alloc(buffer, append:(a,b,c,d)=>c.Add((float)val), args:val));
            return buffer;
        }
        
        public static ArcenCharacterBufferBase AddVarReplace(this ArcenCharacterBufferBase buffer, TextVarMap map, string val)
        {
            map?.AddVarReplace(FormatArgs.Alloc(buffer, append:(a,b,c,d)=>c.Add((string)d), args:val));
            return buffer;
        }

        public static ArcenCharacterBufferBase AddVarReplace(this ArcenCharacterBufferBase buffer, TextVarMap map, AppendVarValue_Method appendVar)
        {
            map?.AddVarReplace(FormatArgs.Alloc(buffer, append:appendVar));
            return buffer;
        }
        
        public static ArcenCharacterBufferBase AddVarReplace(this ArcenCharacterBufferBase buffer, TextVarMap map, AppendVarValue_Method appendVar, object args)
        {
            map?.AddVarReplace(FormatArgs.Alloc(buffer, append:appendVar, args:args));
            return buffer;
        }
        
        public static ArcenCharacterBufferBase AddVarReplace(this ArcenCharacterBufferBase buffer, TextVarMap map, EvalCondition_Method evalCond, AppendVarValue_Method appendVar, object args)
        {
            map?.AddVarReplace(FormatArgs.Alloc(buffer, append:appendVar, evalCond:evalCond, args:args));
            return buffer;
        }

        public static ArcenCharacterBufferBase AddVarReplaceParams(this ArcenCharacterBufferBase buffer, TextVarMap map, params AppendVarValue_Method[] appendVar)
        {
            if (map == null)
                return buffer;
            
            int counter = 0;
            void Append(string _name, TextStyle _style, ArcenCharacterBufferBase _buffer, object _args)
            {
                if (counter >= appendVar.Length)
                    throw new IndexOutOfRangeException();
                
                var method = appendVar[counter];
                if (method != null)
                    method(null, null, _buffer, _args);
                counter++;
            }
            
            map.AddVarReplace(FormatArgs.Alloc(buffer, append:Append));
            
            return buffer;
        }
    }

    #endregion
}
