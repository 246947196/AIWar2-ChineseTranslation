using System;
using System.Reflection;
using Arcen.Universal;
using Arcen.AIW2.Core;
using System.Text;
using System.Linq;
using Sys=System.Collections.Generic;

namespace Arcen.AIW2.External
{
    public class TextStyle : ArcenDynamicTableRow, IConcurrentPoolable<TextStyle>, IProtectedListable, ITimeBasedPoolable<TextStyle>, IDisposable
    {
        #region Types
        private enum PoolType
        {
            NotPooled,
            TempPool,
            RowPool,
        }
        #endregion
        
        #region Static Data

        public static TextStyle TooltipBase;
        public static TextStyle Tooltip_Hotkeys_Footer;
        public static TextStyle DlcMod_Full;
        public static TextStyle DlcMod_Abbrev;
        public static TextStyle DlcMod_Abbrev_Small;
        public static TextStyle DlcMod_Abbrev_Smaller;
        public static TextStyle DlcMod_Abbrev_Medium;
        public static TextStyle DlcMod_Ency;
        public static TextStyle Newline_NoLabel;
        public static TextStyle Hack_Name;
        
        public static TextStyle Color_Active;
        public static TextStyle Color_Inactive;
        public static TextStyle Color_DoesNotApply;
        public static TextStyle Color_Count;
        public static TextStyle Color_Gray;
        public static TextStyle Color_Green;
        public static TextStyle Color_Devour;
        public static TextStyle Color_Infest;
        public static TextStyle Color_Warn;
        public static TextStyle Exmplkawejr;
            
        public static TextStyle RegularText;
        public static TextStyle BasicText;
        public static TextStyle SecondaryText;

        public static TextStyle Major_Label_Bad;
        public static TextStyle Major_Label_Good;
        public static TextStyle Conjunc;
        public static TextStyle Decimal;
        public static TextStyle Multiply;
        public static TextStyle Range;
        public static TextStyle Number;
        public static TextStyle Number_Units;
        public static TextStyle Compare_Symbol;
        public static TextStyle Fraction;
        public static TextStyle Fraction_Gray;
        public static TextStyle MinutesAndSeconds;
        public static TextStyle MinutesAndSeconds_Units;
        public static TextStyle MinutesAndSeconds_Spacer;
        public static TextStyle Emphasis;
        public static TextStyle Brighter;
        public static TextStyle Separator;

        public static TextStyle TextTerm_Sprite;
        public static TextStyle TextTerm_Sprite_Smaller;
        public static TextStyle Ship_Sprite;
        public static TextStyle Ship_Sprite_Smaller;
        public static TextStyle Ship_Sprite_Ency;
        
        public static TextStyle Inline_Techs;
        public static TextStyle UserAction;
        public static TextStyle WarnText;
        public static TextStyle DarkText;
        public static TextStyle GreenText;
        public static TextStyle Req_Met;
        public static TextStyle Req_Unmet;
        
        public static TextStyle JustBold;
        public static TextStyle Sub;
        public static TextStyle Sub2;
        public static TextStyle Empty;
        
        public static TextStyle PlayerType_Name;

        public static TextStyle Status_Block;
        public static TextStyle Status_Line;
        public static TextStyle Behavior_Label;
        public static TextStyle Orders_Label;
        public static TextStyle Buffs_Label;
        public static TextStyle Debuffs_Label;

        public static TextStyle System_Pad;
        public static TextStyle System_Line;
        public static TextStyle System_Line_NoNew;
        public static TextStyle System_Sub_Lines;
        public static TextStyle System_Line2;
        public static TextStyle System_Label;
        public static TextStyle System_Label2;
        
        public static TextStyle System_Line2_Reapply;
        public static TextStyle SystemTargets_Sub_Lines;
        public static TextStyle SystemTargets_Line;
        public static TextStyle SystemTargets_Label;
        public static TextStyle Attr_Line;
        public static TextStyle Attr_Label;
        public static TextStyle Attr_Sub_Lines;
        public static TextStyle Attr_Line2;
        public static TextStyle Attr_Label2;
        public static TextStyle Attr_Pad;
        
        public static TextStyle Assist_Item;
        public static TextStyle Assist_Label;
        
        public static TextStyle Stat_Block;
        public static TextStyle Stat_Pad;
        public static TextStyle Stat_Line;
        
        public static TextStyle Desc_Block;
        public static TextStyle Desc_Pad;
        
        public static TextStyle Fleet_Member_Line;
        public static TextStyle Fleet_Members_Inline;
        
        public static TextStyle Pad_Small;
        public static TextStyle Pad_Large;
        
        public static TextStyle Sidebar_Button_Tooltip_Header;
        public static TextStyle Sidebar_Button_Tooltip_Body;
        
        public static TextStyle SettingTooltip;
        public static TextStyle SettingTooltip_Line;
        public static TextStyle SettingTooltip_Label;
        public static TextStyle SettingTooltip_Desc;
        public static TextStyle Setting_Regular;
        public static TextStyle Setting_Indented;
        public static TextStyle Setting_Hidden;
        public static TextStyle Color_Setting_Fixed;
        public static TextStyle Color_Setting_Changed;
        public static TextStyle Color_Setting_Default;
        public static TextStyle Color_Setting_ShowIf;
        public static TextStyle AlteredBy_Campaign;
        
        public static TextStyle Hack_Menu_Prompt;
        
        public static TextStyle Hack_General;
        public static TextStyle Hack_Header;
        public static TextStyle Hack_Line;
        public static TextStyle Hack_Prompt_General;
        public static TextStyle Hack_Prompt_Line;
        public static TextStyle Hack_Prompt_Header;
        public static TextStyle Hack_Prompt_Label;
        
        public static TextStyle Hacker_Needed_Type;
        public static TextStyle Hack_Against;
        public static TextStyle Hack_Penalty;
        public static TextStyle Hack_Bonus;
        public static TextStyle Hack_Inactive_Modifier;
        
        public static TextStyle Note_Sprite;
        public static TextStyle Note_Key;
        
        public static TextStyle View_Contents_Line_Main;
        public static TextStyle View_Contents_Line_Second;
        
        public static TextStyle CustomSystem_ActionIfUsed_None;
        public static TextStyle CustomSystem_ActionIfUsed_Activate;
        public static TextStyle CustomSystem_ActionIfUsed_Cancel;
        public static TextStyle CustomSystem_ActionIfUsed_Target;
        public static TextStyle CustomSystem_SystemStatus_Cooldown;
        public static TextStyle CustomSystem_SystemStatus_Ready;
        public static TextStyle CustomSystem_SystemStatus_On;

        public static TextStyle Color_Quickstart;
        public static TextStyle Color_Quickstart_Cleared;
        public static TextStyle Color_Quickstart_Selected;
        
        public static TextStyle Ion;
        
        #endregion

        #region Fields
        
        public struct Element
        {
            public string Name;
            public string Value;
        }
        public List<Element> Elements = List<Element>.Create_WillNeverBeGCed(10, "TextStyle.Elements", 10);

        public string Link;
        public string Color;
        public string Size;
        public bool Bold;
        public bool Italic;
        public bool Strikethrough;
        public bool Underline;
        public string LineHeight;
        public string Indent;
        public string LineIndent;
        public string Align;
        public string Margin;
        public string MarginLeft;
        public string MarginRight;
        public string CSpacing;
        public string MSpace;
        public bool Lowercase;
        public bool Uppercase;
        public bool Smallcaps;
        public bool Subscript;
        public bool Superscript;
        public string VOffset;
        public string Font;
        public string Width;
        public string Space;
        public string Position;
        public bool Nobr;
        public string Mark;
        public string Pad;
        public string Leading;
        public string Trailing;
        public string Text;
        public TextCaps Caps;
        public string Sprite;
        public bool SpriteTint;
        public string SpriteSize;
        public string SpriteColor;

        #endregion

        public void CopyFrom( TextStyle Style )
        {
            if (Style == null)
                return;
            
            //LOG.Msg("{0}.CopyFrom( {1} ) called.", InternalName, Style.InternalName);
            //LOG.Msg("");
            //LOG.Msg("{0}=", InternalName);
            //LOG.Msg(ObjToStr.Format(this, ObjToStr.Style.MembersOnlyMultiline));
            
            var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly;
            var members = this.Members(MemberType.Field, flags);
            
            foreach (var mem in members)
            {
                if (mem.GetDataType().IsClass)
                {
                    //LOG.Msg("Skip member {0} because IsClass.", mem.Name);
                    continue;
                }
                
                //LOG.Msg("Copy member {0}.", mem.Name);
                mem.SetValue(this, mem.GetValue(Style));
            }
            
            //LOG.Msg("");
            //LOG.Msg("{0}=", InternalName);
            //LOG.Msg(ObjToStr.Format(this, ObjToStr.Style.MembersOnlyMultiline));
        }
        
        private void AddElement(string name, bool value)
        {
            if (value == false)
                return;
            
            var e = new Element()
            {
                Name = name,
            };
            Elements.Add(e);
        }

        private void AddElement(string name, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            
            var e = new Element()
            {
                Name = name,
                Value = value,
            };
            Elements.Add(e);
        }
        
        public void Setup()
        {
            Elements.Clear();
            
            if (string.IsNullOrWhiteSpace(InternalName))
                LOG.Err("Expected InternalName to be set but was not, called from:\n" + LOG.StackTrace());
            
            Link = $"\"{InternalName}\"";
            
            AddElement("link", Link?.LeadingAndTrailing("\""));
            AddElement("color", Color?.Leading("#"));
            AddElement("size", Size);
            AddElement("b", Bold);
            AddElement("i", Italic);
            AddElement("u", Underline);
            AddElement("s", Strikethrough);
            AddElement("line-height", LineHeight);
            AddElement("indent", Indent);
            AddElement("line-indent", LineIndent);
            AddElement("align", Align?.LeadingAndTrailing("\""));
            AddElement("margin", Margin);
            AddElement("margin-left", MarginLeft);
            AddElement("margin-right", MarginRight);
            AddElement("cspace", CSpacing);
            AddElement("mspace", MSpace);
            AddElement("lowercase", Lowercase);
            AddElement("uppercase", Uppercase);
            AddElement("smallcaps", Smallcaps);
            AddElement("sub", Subscript);
            AddElement("sup", Superscript);
            AddElement("voffset", VOffset);
            AddElement("font", Font?.LeadingAndTrailing("\""));
            AddElement("width", Width);
            AddElement("space", Space);
            AddElement("pos", Position);
            AddElement("nobr", Nobr);
            AddElement("mark", Mark);
            
            if (!string.IsNullOrEmpty(Sprite))
            { 
                string args = "";
                if (SpriteTint)
                    args += " tint=1";
                if (!string.IsNullOrEmpty(SpriteSize))
                    args += " size=" + SpriteSize;
                if (!string.IsNullOrEmpty(SpriteColor))
                    args += " color=" + SpriteColor.Leading("#");

                string txt = Sprite.LeadingAndTrailing("\"");
                if (!string.IsNullOrEmpty(args))
                    txt += args;
                
                //LOG.Msg("sprite= {0}", txt);
                AddElement("sprite", txt);
            }
            
            if (!string.IsNullOrEmpty(Leading) && Nobr)
            {
                
            }

            var field = typeof(TextStyle).GetField(
                InternalName,
                System.Reflection.BindingFlags.Static | 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.IgnoreCase );
            
            if (field != null)
                field.SetValue(null, this);
        }

        public static TextStyle Get( string name )
        {
            return TextStyleTable.Instance.GetRowByName( name, LookupSwapAllowed.No, true );
        }

        public void Clear()
        {
            var temp = this.InternalName;
            InnerSetToDefaults();
            this.InternalName = temp;
        }
        
        public ArcenCharacterBufferBase Open(ArcenCharacterBufferBase buffer, bool is_pad_empty=true, bool no_link=true)
        {
            no_link = true;
            //if (!nolink)
                //Open("link", Elements.Find(e=>e.Name=="link").Value, buffer);
            
            bool leading_handled = false;
            if (this.Nobr && 
                !string.IsNullOrEmpty(this.Leading) &&
                !buffer.Builder.IsNewLine(is_pad_empty))
            {
                if (string.IsNullOrWhiteSpace(this.Leading))
                {
                    buffer.Add(this.Leading);
                    leading_handled = true;
                }
            }
            
            if (!buffer.WriteAsTextFileOutput)
            {
                for (int i = 0; i < Elements.Count; i++)
                {
                    var e = Elements[i];
                    if (e.Name == "link" && no_link)
                        continue;
                    if (e.Name == "nobr" && 
                        !string.IsNullOrEmpty(this.Leading) && 
                        string.IsNullOrWhiteSpace(this.Leading))
                    {
                        
                    }
                        
                    Open(e.Name, e.Value, buffer);
                }
            }

            if (!leading_handled)
            {
                if (!string.IsNullOrEmpty(this.Leading) &&
                    !buffer.Builder.IsNewLine(is_pad_empty))
                {
                    buffer.Add(this.Leading);
                }
            }
            
            if (this.Caps != TextCaps.Null)
                buffer.InCase(this.Caps);
            
            if (!string.IsNullOrEmpty(this.Text))
                buffer.Add(this.Text);

            return buffer;
        }
        
        private void Open(string name, string value, ArcenCharacterBufferBase buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (name == null)
                throw new ArgumentNullException("name");
            
            //if (string.IsNullOrEmpty(value))
                //return;
            
            buffer
                .Add("<")
                .Add(name, TextCaps.Normal);
            if (!string.IsNullOrEmpty(value))
            {
                buffer.Add("=").Add(value, TextCaps.Normal);
            }
            
            buffer.Add(">");
        }
        
        public ArcenCharacterBufferBase Close(ArcenCharacterBufferBase buffer, bool remove_empty=true, bool nolink=false)
        {
            nolink = true;
            
            if (this.Caps != TextCaps.Null)
                buffer.EndCase();
            
            if (!buffer.WriteAsTextFileOutput)
            {
                for (int i = Elements.Count-1; i >= 0; i--)
                {
                    var e = Elements[i];
                    if (e.Name == "link" && nolink)
                        continue;
                    
                    ValidateClose(e.Name, buffer, remove_empty);
                }
            }
            
            if (remove_empty)
            {
                if (!string.IsNullOrEmpty(this.Leading))
                {
                    buffer.Builder.TrimEnd(this.Leading);
                }
            }

            return buffer;
        }
        
        [ThreadStatic]
        private static Sys.List<TextBlock> _WorkingTagList;
        
        private static Sys.List<TextBlock> WorkingTagList
        {
            get
            {
                if (_WorkingTagList == null)
                    _WorkingTagList = new Sys.List<TextBlock>();
                return _WorkingTagList;
            }
        }
        
        private static readonly SubStr Margin_Str = new SubStr("margin");
        
        private enum IsValid
        {
            NotOpen,
            Invalid,
            Valid
        }
        
        private IsValid ValidateClose(string name, ArcenCharacterBufferBase buffer, bool remove_empty)
        {
            int debugstage = 0;
            try 
            {
                var builder = buffer.Builder;

                debugstage = 100;
                
                if (name == "space" ||
                    name == "pos" ||
                    name == "sprite")
                {
                    return IsValid.Valid;
                }
                
                debugstage = 110;
                
                Log log = null;

                again:
                
                debugstage = 120;
                IsValid valid = IsValid.NotOpen;
                log?.AppendFormat("\n{0}.Close(\"{1}\") : called.\n~~~~~~~~~~\n\n", this.InternalName, name);
                
                debugstage = 130;
                WorkingTagList.Clear();
                
                debugstage = 140;
                bool empty = true;
                int idx = -1;
                int counter = 1;
                foreach (var b in builder.EnumerateBlocks(builder.Length, -1))
                {
                    debugstage = 150;
                    
                    log?.AppendFormat("Block #{0}: {1}\n", counter, b.DebugDisplay());
                    counter++;
                    
                    debugstage = 160;
                    
                    log?.Flush();
                    
                    debugstage = 170;
                    
                    if (b.Type == TextBlock.BlockType.Null)
                        break;
                    
                    debugstage = 180;
                    
                    if ( b.Type == TextBlock.BlockType.Text )
                    {
                        debugstage = 190;
                        
                        if (!b.IsEmpty(false))
                            empty = false;
                            //empty = true;
                            //valid = IsValid.Empty;
                        //else
                            //valid = IsValid.Valid;
                        
                        debugstage = 200;
                            
                        continue;
                    }
                            
                    debugstage = 210;
                    
                    if (b.Type == TextBlock.BlockType.Tag_Solo)
                        continue;
                    
                    debugstage = 220;
                    
                    if (b.Type == TextBlock.BlockType.Tag_Close)
                    {
                        WorkingTagList.Add(b);
                        continue;
                    }
                    
                    debugstage = 230;
                    
                    if (b.Type == TextBlock.BlockType.Tag_Open)
                    {
                        debugstage = 240;
                        
                        if (WorkingTagList.Count > 0)
                        {
                            debugstage = 250;
                            
                            var last = WorkingTagList[WorkingTagList.Count-1];
                            if (!last.Name.Equals(b.Name))
                            {
                                debugstage = 260;
                                
                                log?.AppendFormat("{0}.Close(\"{1}\") : found an unclosed tag, opened after we were?\n{2}\nExpected:\n{3}\n\n", 
                                    this.InternalName, name, b.DebugDisplay(), last.DebugDisplay());
                                
                                debugstage = 270;
                                
                                valid = IsValid.Invalid;
                                break;
                            }
                            
                            debugstage = 280;
                            
                            WorkingTagList.PopLast();
                            
                            debugstage = 290;
                            
                            continue;
                        }
                        
                        debugstage = 300;
                        
                        if (b.Name.Equals(name))
                        {
                            debugstage = 310;
                            
                            valid = IsValid.Valid;
                            idx = b.Idx;
                            break;
                        }
                        
                        debugstage = 320;
                        
                        // well.. i guess we hit a tag that is closed later, after us??
                        // but we were opened before them??
                        // so thats wrong.
                        valid = IsValid.Invalid;
                        
                        log?.AppendFormat("{0}.Close(\"{1}\") : found an unclosed tag, opened after we were?\n{2}\n\n", 
                            this.InternalName, name, b.DebugDisplay());
                        
                        debugstage = 330;
                        
                        break;
                    }
                    
                    debugstage = 350;
                }
                
                debugstage = 400;
                
                if (valid == IsValid.NotOpen)
                {
                    debugstage = 410;
                    
                    if (log == null && 
                        EntityText.DumpNextTooltipText != EntityText.DebugAction.Null)
                    {
                        log = Log.Yes;
                        goto again;
                    }
                    
                    debugstage = 420;
                    
                    log?.AppendFormat("{0}.Close(\"{1}\") wasn't even open??.\n\n", this.InternalName, name);
                    buffer.Dump(log);
                    
                    debugstage = 430;
                }
                else 
                if (valid == IsValid.Invalid)
                {
                    debugstage = 500;
                    
                    if (log == null && 
                        EntityText.DumpNextTooltipText != EntityText.DebugAction.Null)
                    {
                        log = Log.Yes;
                        goto again;
                    }
                    
                    debugstage = 510;
                    
                    log?.AppendFormat("{0}.Close(\"{1}\") out of order.\n\n", this.InternalName, name);
                    log?.AppendContext(builder, idx, builder.Length-idx).AppendLine();
                    //log?.AppendLine("expected:");
                    //foreach (var b in WorkingTagList)
                        //log?.AppendLine(b.DebugDisplay());
                    
                    buffer.Dump(log);
                    
                    debugstage = 520;
                }
                else 
                if (valid == IsValid.Valid)
                {
                    debugstage = 600;
                    
                    if (!empty)
                    {
                        debugstage = 610;
                        
                        log?.AppendFormat("{0}.Close(\"{1}\") is not empty, keeping.\n\n", this.InternalName, name);
                            
                        if (name == "margin-left" ||
                            name == "margin-right")
                        {
                            name = "margin";
                        }
                        buffer.Add("</").Add(name, TextCaps.Normal);
                        buffer.Add(">");
                        
                        /*
                        if (name == "nobr")
                            buffer.Add("<space=0px>");
                        */
                    }
                    else
                    {
                        debugstage = 620;
                        
                        if (remove_empty)
                        {
                            debugstage = 630;
                            
                            //if (log == null)
                            //{
                            //    log = Log.Yes;    
                            //    goto again;
                            //}
                            log?.AppendFormat("{0}.Close(\"{1}\") is empty.\n", this.InternalName, name);
                                //.AppendLine(builder.ToString(idx));
                            
                            log?.Dump(buffer);

                            builder.Remove(idx, builder.Length - idx);

                            log?.AppendLine("after:")
                                .AppendLine(buffer.ToString());
                            
                            //buffer.Dump(log);
                        }
                        else
                        {
                            debugstage = 640;
                            
                            log?.AppendFormat("{0}.Close(\"{1}\") is empty, but remove not specified, keeping.\n\n", this.InternalName, name);
                            
                            buffer.Add("</").Add(name, TextCaps.Normal);
                            buffer.Add(">");
                            
                            /*
                            if (name == "nobr")
                                buffer.Add("<space=0px>");
                            */
                        }
                    }
                }

                debugstage = 1000;
                
                log?.Append(Environment.StackTrace);
                log?
                    .Append("End of Close.\n")
                    .Append("~~~~~~~~~~\n\n")
                    .Flush();

                debugstage = 1100;
                
                return valid;
            }
            catch (Exception e)
            {
                LOG.Err("error at debugstage {0} from:\n{1}", debugstage, e);
            }
            
            debugstage = 1200;
            
            return IsValid.Invalid;
        }
        
        #region Pooling
        private static ReferenceTracker RefTracker;

        private TextStyle(PoolType poolType) 
            : base( ThereShouldNeverBeAPublicOrInternalConstructorOnThese.IUnderstand )
        {
            if (poolType != PoolType.NotPooled)
            {
                if ( RefTracker == null )
                    RefTracker = new ReferenceTracker( "TextStyle" );
                RefTracker.IncrementObjectCount();
            }
            
            if (poolType == PoolType.NotPooled)
                IsUnpooled = true;
            else if (poolType == PoolType.TempPool)
                IsTemp = true;
        }

        private static ConcurrentPool<TextStyle> Pool = 
            new ConcurrentPool<TextStyle>( 
                "TextStyle.Pool", 1000,
                KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, 
                PoolBehaviorDuringShutdown.BlockAllThreads, 
                ()=>new TextStyle(PoolType.RowPool) );

        private static TimeBasedPool<TextStyle> TimedPool = 
            TimeBasedPool<TextStyle>.Create_WillNeverBeGCed( 
                "TextStyle.TimedPool", 
                3, 1, 3000, 
                KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, 
                PoolBehaviorDuringShutdown.BlockAllThreads, 
                ()=>new TextStyle(PoolType.TempPool) );

        public static TextStyle GetUnpooled()
        {
            return new TextStyle(PoolType.NotPooled);
        }
        
        public static TextStyle GetTemp()
        {
            var style = TimedPool.GetFromPoolOrCreate();
            if ( style == null ) //pool returns null while blocked for teardown/shutdown; let callers bail
                return null;
            style.InternalName = "Temp";
            return style;
        }
        
        public static TextStyle GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }

        public override void ReturnToPool()
        {
            if (IsUnpooled)
                return;
            
            if (IsTemp)
                TimedPool.ReturnToPool(this);
            else
                Pool.ReturnToPool( this );
        }

        private static ArcenTypeAnalyzer<TextStyle> typeAnalyzer;

        protected override void InnerSetToDefaults()
        {
            if ( typeAnalyzer == null )
                typeAnalyzer = new ArcenTypeAnalyzer<TextStyle>( new TextStyle(PoolType.NotPooled) );
            typeAnalyzer.ApplyDefaults( this );
        }
        #endregion

        #region ITimeBasedPoolable
        bool ITimeBasedPoolable<TextStyle>.IsInPoolAtAll
        {
            get;
            set;
        }

        bool ITimeBasedPoolable<TextStyle>.IsInQuarantine
        {
            get;
            set;
        }
        
        private static int LastUniqueIDForThisType = 0;
        private readonly int PermaUniqueID = ++LastUniqueIDForThisType;
        private readonly bool IsTemp;
        private readonly bool IsUnpooled;
        int ITimeBasedPoolable<TextStyle>.GetPermaUniqueID => PermaUniqueID;

        void ITimeBasedPoolable<TextStyle>.Debug_WriteToHistoryOfPoolStatus( string Message )
        {
        }

        string ITimeBasedPoolable<TextStyle>.Debug_GetCondensedHistoryOfPoolStatus()
        {
            return string.Empty;
        }

        void ITimeBasedPoolable<TextStyle>.DoEarlyCleanupWhenGoingIntoQuarantine_ClearIncomingPointersButNotOugoingReferences()
        {
        }

        void ITimeBasedPoolable<TextStyle>.DoMidCleanupWhenLeavingQuarantineBackIntoMainPool_ClearAsMuchAsPossibleIncludingOutgoingReferences()
        {
        }

        void ITimeBasedPoolable<TextStyle>.DoAnyBelatedCleanupWhenComingOutOfPool_ShouldBeVeryLittleToDo()
        {
            this.SetToDefaults();
        }
        #endregion
        
        public void Dispose()
        {
            if (this.InternalName == "Temp")
                ReturnToPool();
        }
        
        public static TextStyle GetColor(string hexColor)
        {
            var temp = GetTemp();
            temp.InternalName = "GetColor_" + hexColor;
            temp.Color = hexColor;
            temp.Setup();
            return temp;
        }
    }
    
    public class TextStyleTable : ArcenDynamicTable<TextStyle>
    {
        public static TextStyleTable Instance;
        public TextStyleTable() : base( "TextStyles", ArcenDynamicTableType.XMLDirectory, PrimaryKeyRules.Strict, SerializedBy.Name, ReloadDuringRuntime.Allow, ReadXml.InOrderOnMainThread )
        {
            Instance = this;
        }

        protected override void PrepForCompleteReloadLater()
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            
            SetupRows();
        }

        public override void ReloadSelectData()
        {
            foreach (var r in Rows)
                r.Clear();
            base.ReloadSelectData();
            
            SetupRows();
        }

        public override DelReturn NodeProcessor( ArcenXMLElement Data, TextStyle Style )
        {
            ProcessElement(Data, Style);
            return DelReturn.Continue;
        }

        public override DelReturn NodeSelectReProcessor( ArcenXMLElement Data, TextStyle Style )
        {
            ProcessElement(Data, Style);
            return DelReturn.Continue;
        }
        
        public void ProcessElement( ArcenXMLElement e, TextStyle style )
        {
            e.Fill("bold", ref style.Bold, false);
            e.Fill("italic", ref style.Italic, false);
            e.Fill("size", ref style.Size, false);
            e.Fill("color", ref style.Color, false); style.Color = style.Color?.TrimStart('#');
            e.Fill("underline", ref style.Underline, false);
            e.Fill("strikethrough", ref style.Strikethrough, false);
            e.Fill("line_height", ref style.LineHeight, false);
            e.Fill("align", ref style.Align, false);        
            e.Fill("margin", ref style.Margin, false);
            e.Fill("margin_left", ref style.MarginLeft, false);
            e.Fill("margin_right", ref style.MarginRight, false);
            e.Fill("cspace", ref style.CSpacing, false);
            e.Fill("monospace", ref style.MSpace, false);
            e.Fill("lowercase", ref style.Lowercase, false);
            e.Fill("uppercase", ref style.Uppercase, false);
            e.Fill("smallcaps", ref style.Smallcaps, false);
            e.Fill("subscript", ref style.Subscript, false);
            e.Fill("superscript", ref style.Superscript, false);
            e.Fill("voffset", ref style.VOffset, false);
            e.Fill("font", ref style.Font, false);
            e.Fill("width", ref style.Width, false);
            e.Fill("space", ref style.Space, false);
            e.Fill("pos", ref style.Position, false);
            e.Fill("nobr", ref style.Nobr, false);
            e.Fill("mark", ref style.Mark, false);
            e.Fill("pad", ref style.Pad, false);
            e.Fill("indent", ref style.Indent, false);

            if (e.GetInt32("newline", 0, false) > 0)
            {
                int num = e.GetInt32("newline", 0, false);
                if (num > 0)
                    style.Leading = string.Concat(System.Linq.Enumerable.Repeat("\n", num));
            }
            
            var str = e.GetString("leading", null, false);
            if (str != null)
            {
                if (style.Leading != null)
                {
                    LOG.Msg("Warning: {0}: newline=\"{1}\" already set but being overwritten by leading=\"{2}\"", 
                        style.InternalName, style.Leading.Replace("\n", "\\n"), str);
                }
                
                str = str.Replace("\\n", "\n");
                str = str.Replace("{", "<");
                str = str.Replace("}", ">");
                
                style.Leading = str;
            }
            
            str = e.GetString("trailing", null, false);
            if (str != null)
            {
                str = str.Replace("\\n", "\n");
                str = str.Replace("{", "<");
                str = str.Replace("}", ">");
                
                style.Trailing = str;
            }
            
            str = e.GetString("text", null, false);
            if (str != null)
            {
                str = str.Replace("\\n", "\n");
                str = str.Replace("{", "<");
                str = str.Replace("}", ">");
                
                style.Text = str;
            }

            e.FillEnum("caps", ref style.Caps, false);
            
            e.Fill("sprite", ref style.Sprite, false);
            
            e.Fill("sprite_tint", ref style.SpriteTint, false);
            
            e.Fill("sprite_size", ref style.SpriteSize, false);
            
            e.Fill("sprite_color", ref style.SpriteColor, false);
        }

        private void SetupRows()
        {
            foreach (var r in Rows)
            {
                r.Setup();
            }
            
            /*
            var flags = BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public;
            var members = typeof(TextStyle).Members(MemberType.Field, flags );
            //LOG.Msg("TextStyle has {0} static members.", members.Count);
            
            foreach (var mem in members)
            {
                var val = mem.GetValue();
                //LOG.Msg("TextStyle.{0}={1}", mem.Name, val.OrNull());
                if (val == null)
                    LOG.Msg("Warning: TextStyle.{0} was never initialized (therefore not defined in xml).", mem.Name);
            }
            */
        }
        
        public override TextStyle GetNewRowFromPool()
        {
            return TextStyle.GetFromPoolOrCreate();
        }
    }
    
    public class TextStyleTable_Hooks : IArcenExternalCodeHookHandler
    {
        void IArcenExternalCodeHookHandler.HandleExternalHook( object MainObject, object SecondaryObject, object[] AdditionalObjects,
            ArcenExternalCodeHook Hook, ArcenSimContextBase Context )
        {
            //LOG.Msg("TextStyleTable.HandleExternalHook '{0}", Hook.InternalName);
            
            if (Hook.InternalName == "ReloadSelectData")
            {
                TextStyleTable.Instance.ReloadSelectData();
            }
        }
    }
    
    public static partial class Extensions
    {
        /*
        public static ArcenCharacterBufferBase Push( this ArcenCharacterBufferBase buffer, TextStyle style )
        {
            var cur = buffer.Styles as TextStyle;
            if (cur != null)
            {
                buffer.Close(cur);
                LOG.Err("Push '{0}' invalid because '{1}' ")
                throw new Exception(string.Format("error can't push style '{0}' when '{1}' already is", style.InternalName, cur.InternalName));
            }
            
            // note: still may be null, which is fine
            if (style != null)
            {
                buffer.PushedStyle = style;
                buffer.Open(style);
            }
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase Push( this ArcenCharacterBufferBase buffer, string style_name )
        {
            var cur = buffer.PushedStyle as TextStyle;
            if (cur != null)
            {
                buffer.PushedStyle = null;
                throw new Exception(string.Format("error can't push style '{0}' when '{1}' already is", style_name, cur.InternalName));
            }
            
            // note: still may be null, which is fine
            cur = TextStyle.Get(style_name);
            if (cur != null)
            {
                buffer.PushedStyle = cur;
                buffer.Open(cur);
            }
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase Pop( this ArcenCharacterBufferBase buffer )
        {
            var cur = buffer.PushedStyle as TextStyle;
            if (cur != null)
            {
                buffer.PushedStyle = null;
                buffer.Close(cur);
            }
            
            return buffer;
        }
        */
        
        public static ArcenCharacterBufferBase Open( this ArcenCharacterBufferBase buffer, TextStyle style )
        {
            //if (style.Newline)
                //buffer.Builder.TruncateNewLine();
            
            style.Open(buffer, false, false);
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase Close( this ArcenCharacterBufferBase buffer, TextStyle style )
        {
            style.Close(buffer, true);
            return buffer;
        }
        
        public static ArcenCharacterBufferBase Close( this ArcenCharacterBufferBase buffer, TextStyle style, bool remove_empty )
        {
            style.Close(buffer, remove_empty);
            return buffer;
        }
        
        public static ArcenCharacterBufferBase Pad( this ArcenCharacterBufferBase buffer )
        {
            return buffer.Pad(TextStyle.Pad_Small);
        }
        
        public static ArcenCharacterBufferBase Pad( this ArcenCharacterBufferBase buffer, TextStyle style )
        {
            int debugstage = 0;
            try
            {
                buffer.Builder.TruncateNewLine();
                //(buffer.Builder as StringBuffer)?.TruncateNewLine();
                
                debugstage = 10;
                if (style == null)
                    return buffer;
                
                debugstage = 20;

                style.Open(buffer, false, true).NewLine();
                //buffer.Open(style).NewLine();
                
                //debugstage = 30;
                if (GameSettings.Current.GetBoolBySetting( "Debug_Tooltip_TextRegions" ))
                    buffer.Add("{pad}");
                else
                    buffer.Add(" ");
                
                debugstage = 40;
                style.Close(buffer, false, true);
                
                debugstage = 50;
            }
            catch( Exception e)
            {
                LOG.Err("debugstage {0}\n{1}", debugstage, e);
                return buffer;
            }
            
            return buffer;
        }
        
        //public static ArcenCharacterBufferBase AddFormat<T>(this ArcenCharacterBufferBase buffer, string format, T arg1)
        //{
        //    var builder = buffer.Builder;
        //    builder.AppendFormat(format, arg1);
        //    return buffer;
        //}
        
        //public static ArcenCharacterBufferBase InStyle(this ArcenCharacterBufferBase buffer, string val, string style_name)
        //{
        //    var style = TextStyle.Get("style_name");
        //    if (style == null)
        //        return buffer.Add(val);
            
        //    buffer
        //        .Open(style)
        //        .Add(val)
        //        .Close(style);
            
        //    return buffer;
        //}
        
        /// <summary>
        /// For text styles that have the text value specified as well.
        /// </summary>
        public static ArcenCharacterBufferBase Add(this ArcenCharacterBufferBase buffer, TextStyle style)
        {
            if (style == null)
                return buffer;

            buffer
                .Open(style)
                .Close(style);
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase Add(this ArcenCharacterBufferBase buffer, int val, TextStyle style)
        {
            if (style == null)
                return buffer.Add(val);
            
            buffer
                .Open(style)
                .Add(val)
                .Close(style);
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase Add(this ArcenCharacterBufferBase buffer, string val, TextStyle style)
        {
            if (style == null)
                return buffer.Add(val);
            
            buffer
                .Open(style)
                .Add(val)
                .Close(style);
            
            return buffer;
        }

        public static ArcenCharacterBufferBase Add(this ArcenCharacterBufferBase buffer, FInt val, TextStyle style)
        {
            if (style == null)
                return buffer.Add(val);
            
            buffer
                .Open(style)
                .Add(val)
                .Close(style);
            
            return buffer;
        }

        public static ArcenCharacterBufferBase AddDlcMod( this ArcenCharacterBufferBase buffer, IExternalSource source, TextStyle style=null)
        {
            if (source == null)
                return buffer;
            
            if (!source.IsExternal)
                return buffer;
            
            if (style == null)
                style = TextStyle.DlcMod_Abbrev_Small;
            
            style?.Open(buffer, false, false);
            
            if ( source.FromExpansion != null )
            {
                if (!buffer.Builder.IsNewLine())
                    buffer.Add(" ");
                
                buffer.StartColor(source.FromExpansion.ColorForDisplay).Add( source.FromExpansion.Abbreviation ).EndColor();
            }
            if ( source.FromMod != null )
            {
                if (!buffer.Builder.IsNewLine())
                    buffer.Add(" ");
                
                buffer.StartColor(source.FromMod.ColorForDisplay).Add( source.FromMod.Abbreviation ).EndColor();
            }
             
            style?.Close(buffer);
            
            return buffer;
        }
        
        public static ArcenCharacterBufferBase AddDlcMod( this ArcenCharacterBufferBase buffer, IExternalSource source, string statement, TextStyle style=null )
        {
            if (!source.IsExternal)
                return buffer;
            
            //if (EntityText.Detail > TooltipDetail.Medium)
            //{
            //    return _AddDlcMod_Full(buffer, source, statement, style);
            //}
            
            if (style == null)
                style = TextStyle.DlcMod_Abbrev_Small;
            
            buffer.BeginStatement(style);
            //style?.Open(buffer, false, false);
            
            if ( source.FromExpansion != null )
            {
                if (!buffer.Builder.IsNewLine())
                    buffer.NewLine();
                
                buffer.Add( "Exp: " ).StartColor( source.FromExpansion.ColorForDisplay ).Add( source.FromExpansion.Abbreviation ).EndColor();
            }
            if ( source.FromMod != null )
            {
                if (!buffer.Builder.IsNewLine())
                    buffer.NewLine();
                
                buffer.Add( "Mod: " ).StartColor( source.FromMod.ColorForDisplay ).Add( source.FromMod.Abbreviation ).EndColor();
            }
             
            //style?.Close(buffer);
            buffer.EndStatement(style);
            
            return buffer;
        }
        
        private static bool _AddDlcMod_Full( this ArcenCharacterBufferBase buffer, IExternalSource source, string statement, TextStyle style=null )
        {
            if (!source.IsExternal)
                return false;
            
            if (style == null)
                style = TextStyle.DlcMod_Abbrev_Small;
            style?.Open(buffer, false, false);
            
            if ( !buffer.Builder.IsNewLine() )
                buffer.NewLine();
            
            //if (!string.IsNullOrEmpty(statement))
            buffer.Add("Added by:  ");
            
            if ( source.FromExpansion != null )
            {
                if (!buffer.Builder.IsNewLine())
                    buffer.NewLine();
                
                buffer.Add( "DLC Expansion: " );
                buffer.StartColor( source.FromExpansion.ColorForDisplay ).Add( source.FromExpansion.DisplayName )
                    .Add( " (" ).Add( source.FromExpansion.Abbreviation ).Add( ")" ).EndColor();
            }
            if ( source.FromMod != null )
            {
                if (!buffer.Builder.IsNewLine())
                    buffer.NewLine();
                
                buffer.Add( "Mod: " );
                buffer.StartColor( source.FromMod.ColorForDisplay ).Add( source.FromMod.DisplayName )
                    .Add( " (" ).Add( source.FromMod.Abbreviation ).Add( ")" ).EndColor();
            }
             
            style?.Close(buffer);
            
            return true;
        }
        
        /// <summary>
        /// For appending any number of dlc/mod(s) as a colored abbreviation, space separated.
        /// </summary>
        public static ArcenCharacterBufferBase AddDlcMod( this ArcenCharacterBufferBase buffer, Sys.IEnumerable<Expansion> dlcs=null, Sys.IEnumerable<XmlMod> mods=null, TextStyle style=null )
        {
            if (!(dlcs?.Any()==true || mods?.Any()==true))
                return buffer;
            
            if (style == null)
                style = TextStyle.DlcMod_Abbrev_Small;
            
            style.Open(buffer, false, false);
            
            if (dlcs?.Any() == true)
            {
                foreach (var itr in dlcs)
                {
                    if (!buffer.Builder.IsNewLine())
                        buffer.Add(" ");
                    buffer.AddColor( itr.Abbreviation, itr.ColorForDisplay );
                }
            }
            
            if (mods?.Any() == true)
            {
                foreach (var itr in mods)
                {
                    if (!buffer.Builder.IsNewLine())
                        buffer.Add(" ");
                    buffer.AddColor( itr.Abbreviation, itr.ColorForDisplay );
                }
            }
            
            style.Close(buffer);
            
            return buffer;
        }

        public static string Truncate(this string _this, int length)
        {
            if (_this.Length > length)
            {
                return _this.Substring(0, length).TrimEnd(' ');
            }
            
            return _this;
        }
        
        public static string Leading(this string _this, string wrap)
        {
            if (string.IsNullOrEmpty(_this))
                return _this;
            
            if (!_this.StartsWith(wrap))
                _this = wrap + _this;

            return _this;
        }
        
        public static string LeadingAndTrailing(this string _this, string wrap)
        {
            if (string.IsNullOrEmpty(_this))
                return _this;
            
            if (!_this.StartsWith(wrap))
                _this = wrap + _this;
            if (!_this.EndsWith(wrap))
                _this = _this + wrap;
            
            return _this;
        }

        public static TextStyle Style( this string _this )
        {
            return TextStyle.Get(_this);
        }

        #region Unused
        
        /*
        public static void Open( this TextStyle style, ArcenCharacterBuffer buffer )
        {
            buffer.Open(style);
        }
        
        public static void Close( this TextStyle style, ArcenCharacterBuffer buffer )
        {
            buffer.Close(style);
        }
        
        public static Region Region( this ArcenCharacterBufferBase buffer )
        {
            var builder = buffer.Builder;
            return new Region()
            {
                Buffer = builder,
                Idx = builder.Count,
            };
        }

        public struct Region
        {
            public StringBuffer Buffer;
            public int Idx;

            public void Done()
            {
                int len = Buffer.Length - Idx;
                if (len == 0)
                    return;
                
                foreach (var b in Buffer.EnumerateBlocks(Idx, 1))
                {
                    if (b.Type == Block.BlockType.Text)
                    {
                        for (int i = 0; i < b.Len; i++)
                        {
                            var c = Buffer[b.Idx + i];
                            if (c == ' ' || c == '\n' || c == '{' || c == '}' || c == 'p' || c == 'a' || c == 'd')
                                continue;
                            
                            // found actual text in this region.
                            return;
                        }
                    }
                }
                
                Buffer.Remove(Idx);
            }
        }
        */

        #endregion
    }
}

