using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcen.AIW2.External
{
    public abstract class ExternalData_CustomSystem : CustomSystem
    {
        private CustomSystemType _type;
        public override CustomSystemType Type 
        {
            get
            {
                return _type;
            }
            set
            {
                if (value != null)
                {
                    _type = value;
                    
                    //LOG.Msg("{0}.Type is {1}", this.GetType().Name, _type.InternalName);
                    
                    if (_type != null)
                        this.OnTypeSet();
                }
            }
        }

        private EntitySystem _parent;
        public override EntitySystem Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                if ( _parent == value )
                    return;

                _parent = value;

                if ( value != null )
                    this.OnParentSet();
            }
        }
        
        private GameEntity_Base _entity;
        public override GameEntity_Base Entity
        {
            get
            {
                return _entity;
            }
            set
            {
                if ( _entity == value )
                    return;

                _entity = value;

                if ( value != null )
                    this.OnEntitySet();
            }
        }

        protected bool _on = false;
        
        public int ActivationTime = -1;
        public ArcenPoint TargetPos;
        public GameEntity_Squad TargetEntity;
        
        public bool IsOn
        {
            get
            {
                return _on;
            }
        }
        
        public bool IsOff
        {
            get
            {
                return !_on;
            }
        }
        
        public int SortOrder
        {
            get
            {
                return Type.InputNumber;
            }
        }
        
        public override int TimeLeft
        {
            get
            {
                if (_on)
                {
                    if (Type.Duration == -1)
                    {
                        return -1;
                    }
                    
                    int time = World_AIW2.Instance.GameSecond - ActivationTime;
                    int rem = Type.Duration - time;
                    
                    return rem;
                }
                else
                {
                    if ( Type.Cooldown == -1 )
                    {
                        return -1;
                    }
                    
                    if (ActivationTime == -1)
                        return -1;
                    
                    int time = World_AIW2.Instance.GameSecond - ActivationTime;
                    int rem = Type.Cooldown - time;
                    
                    return rem;
                }
            }
        }
        
        public CustomSystemStatus Status
        {
            get
            {
                if (_on)
                {
                    if (Type.Duration == -1)
                    {
                        return CustomSystemStatus.On;
                    }
                    
                    return CustomSystemStatus.OnTill;
                }
                else
                {
                    if ( Type.Cooldown == -1 )
                        return CustomSystemStatus.Ready;

                    if (ActivationTime == -1)
                        return CustomSystemStatus.Ready;
                    
                    int time = World_AIW2.Instance.GameSecond - ActivationTime;
                    int rem = Type.Cooldown - time;
                    if (rem > 0)
                    {
                        return CustomSystemStatus.Cooldown;
                    }
                    
                    return CustomSystemStatus.Ready;
                }
            }
        }

        public override bool Activate( GameEntity_Squad Target, out string Reason )
        {
            Reason = "Does not support a TargetEntity";
            return false;
        }
        
        public override bool Activate( ArcenPoint Target, out string Reason )
        {
            Reason = "Does not support a TargetPos";
            return false;
        }
        
        public override bool Activate( out string Reason )
        {
            Reason = null;
            if (_on)
            {
                Reason = "Already ON";
                return false;
            }
            
            if (Type.Cooldown != -1)
            {
                if (ActivationTime != -1)
                {
                    int time = World_AIW2.Instance.GameSecond - ActivationTime;
                    if (time < Type.Cooldown)
                    {
                        Reason = string.Format("On Cooldown for {0} sec",Type.Cooldown-time);
                        return false;
                    }
                }
            }
            
            _on = true;
            ActivationTime = World_AIW2.Instance.GameSecond;
            
            OnActivate();
            
            return true;
        }

        public override bool Deactivate( out string Reason )
        {
            Reason = null;
            if (!_on)
            {
                Reason = "Already OFF";
                return false;
            }
            
            _on = false;
            OnDeactivate();
            
            return true;
        }

        public override bool Toggle()
        {
            if (_on)
            {
                _on = false;
                OnDeactivate();
            }
            else
            {
                _on = true;
                OnActivate();
            }
            
            return _on;
        }
        
        public bool UserToggle()
        {
            int debugstage = 0;
            try
            {
                debugstage = 500;
                if (Type.Input == CustomSystemType.UseStyle.Activate)
                {
                    debugstage = 700;
                    string reason;
                    if (this.Activate(out reason) == false)
                    {
                        debugstage = 800;
                    }
                    else
                    {
                        reason = "ON";
                    }
                    
                    World_AIW2.Instance.QueueChatMessageOrCommand( 
                            string.Format("<color=#{0}>{1}:</color> {2}", 
                                TooltipColors.Grey.GetHexCode(), Type.DisplayName, reason),
                            ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                }
                else
                if (Type.Input == CustomSystemType.UseStyle.Toggle)
                {
                    debugstage = 1000;
                    bool ison = this.Toggle();
                    debugstage = 1100;
                    World_AIW2.Instance.QueueChatMessageOrCommand( 
                                string.Format("<color=#{0}>{1}:</color> {2}", 
                                    TooltipColors.Grey.GetHexCode(), Type.DisplayName, ison?"ON":"OFF"),
                                ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                }
                else
                if (Type.Input == CustomSystemType.UseStyle.Targeted)
                {
                    var targetAction = Type.TargetedInputAction;
                    if (targetAction != null)
                    {
                        if (this.Status == CustomSystemStatus.Ready)
                        {
                            var handle = targetAction.Handle;
                            
                            string reason;
                            if (handle.CanBegin(this, out reason))
                            {
                                handle.Begin(this);
                                Engine_AIW2.Instance.PendingTargetedAction = handle;
                            }
                            else 
                            if (!string.IsNullOrEmpty(reason))
                                World_AIW2.Instance.QueueChatMessageOrCommand( reason, ChatType.ShowLocallyOnly, "CannotDoThatThing", null );
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LOG.Err("error at debugstage {0}\n{1}", debugstage, e);
            }
            
            return _on;
        }

        public ExternalData_CustomSystem()
        {
        }

        public override string GetIdentifierForErrorMessages()
        {
            return this.GetType().Name;
        }
        
        protected virtual void OnParentSet() { }
        
        protected virtual void OnEntitySet() { }
        
        protected virtual void OnTypeSet() 
        { 
            //foreach (var V in Type.VarList)
            //{
            //    var resolve = V.Clone(this);
            //    VarLookup[V.Key] = resolve;
            //}
        }
        
        protected abstract void OnActivate();
        
        protected abstract void OnDeactivate();
        
        public virtual void Update(ArcenClientOrHostSimContextCore Context, FInt EffectiveDeltaTime)
        {
            //LOG.Msg("{0}.Update(); IsOn={0} ActivationTime={1} GameSec={2} Type.Duration={3} Type.Cooldown={4}", 
                //Type.InternalName, IsOn, ActivationTime, World_AIW2.Instance.GameSecond, Type.Duration, Type.Cooldown);
            
            if (IsOn)
            {
                if (ActivationTime != -1)
                {
                    var time = World_AIW2.Instance.GameSecond - ActivationTime;
                    if (time >= Type.Duration)
                    {
                        string dontcare;
                        Deactivate(out dontcare);
                    }
                }
            }
            
            if (Type.Cooldown != -1)
            {
                if (ActivationTime != -1)
                {
                    var time = World_AIW2.Instance.GameSecond - ActivationTime;
                    if (time >= Type.Cooldown)
                    {
                        string dontcare;
                        Deactivate(out dontcare);
                    }
                }
            }
        }

        public override void SerializeTo( SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            //LOG.Msg("{0} writing ActivationTime={1}", this.GetType().Name, this.ActivationTime);
            Buffer.AddInt32(MetaData, ReadStyle.PosExceptNeg1, this.ActivationTime, "ExternalData_CustomSystem.ActivationTime");
            Buffer.AddArcenPointFromCombatSpace(MetaData, this.TargetPos, "ExternalData_CustomSystem.TargetPos");
            Buffer.AddSquadPrimaryKeyID_PosDef0(MetaData, this.TargetEntity?.PrimaryKeyID??-1, "ExternalData_CustomSystem.TargetEntity");
        }
        
        public override void DeserializeIntoSelf( SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType )
        {
            this.ActivationTime = Buffer.ReadInt32(MetaData, ReadStyle.PosExceptNeg1, "ExternalData_CustomSystem.ActivationTime");
            this.TargetPos = Buffer.ReadArcenPointFromCombatSpace(MetaData, "ExternalData_CustomSystem.TargetPos");
            
            int id = Buffer.ReadSquadPrimaryKeyID_PosDef0(MetaData, "ExternalData_CustomSystem.TargetEntity");
            this.TargetEntity = World_AIW2.Instance.GetEntityByID_Squad(id);
            
            //LOG.Msg("{0} reading ActivationTime={1}", this.GetType().Name, this.ActivationTime);
        }
        
        public virtual void AppendText(ArcenCharacterBufferBase buffer)
        {
            var map = this.Type.UserData["map"] as TextVarMap;
            if (map == null)
                return;
            
            buffer.BeginStatement(TextStyle.System_Line);
            buffer.AddVarReplace(map, AppendVarValue);
            buffer.EndStatement(TextStyle.System_Line);
        }
        
        public virtual void AppendVarValue(string key, TextStyle style, ArcenCharacterBufferBase buffer, object args)
        {
            int debugstage = 0;
            try
            {
                debugstage = 100;
                if ( key.Equals("Name") )
                {
                    debugstage = 110;
                    buffer.Add( this.Type.GetDisplayName() );
                    return;
                }

                debugstage = 120;
                if (key.Equals("Duration"))
                {
                    debugstage = 130;
                    buffer.AddMinutesAndSeconds(this.Type.Duration);
                    return;
                }
                
                debugstage = 140;
                if (key.Equals("Cooldown"))
                {
                    debugstage = 150;
                    buffer.AddMinutesAndSeconds(this.Type.Cooldown);
                    
                    return;
                }
                
                if (key.Equals("Status"))
                {
                    debugstage = 160;
                    var status = this.Status;
                    if (status == CustomSystemStatus.On || status == CustomSystemStatus.OnTill)
                        buffer.Add("On", TooltipColors.CustomSystem_Status_On);
                    else 
                    if (status == CustomSystemStatus.Ready)
                        buffer.Add("Ready", TooltipColors.CustomSystem_Status_Ready);
                    else // Cooldown
                        buffer.Add("Charging", TooltipColors.CustomSystem_Status_Cooldown);

                    return;
                }
                
                if (key.Equals("Activity"))
                {
                    debugstage = 170;
                    var status = this.Status;
                    if (status == CustomSystemStatus.Cooldown || status == CustomSystemStatus.OnTill)
                    {
                        debugstage = 180;
                        buffer
                            .Close(style)
                            .Add("for ")
                            .Open(style)
                            .AddSecondsRemaining( TimeLeft, TimeIntensity.OneMinute );
                    }
                    
                    return;
                }
                
                if (key.Equals("Description"))
                {
                    debugstage = 190;
                    buffer.Add(this.Type.Description);
                    return;
                }
                
                if (key.Equals("Label"))
                {
                    debugstage = 200;
                    if (Type.Input == CustomSystemType.UseStyle.Activate)
                    {
                        buffer.Add("MANUAL-ACTIVATION");
                    }
                    else
                    if (Type.Input == CustomSystemType.UseStyle.Toggle)
                    {
                        buffer.Add("MANUAL-TOGGLE");
                    }
                    else
                    if (Type.Input == CustomSystemType.UseStyle.Targeted)
                    {
                        buffer.Add("MANUAL-TARGET");
                    }
                    
                    return;
                }
                
                if (key.Equals("Usage"))
                {
                    debugstage = 210;
                    if (Type.Input == CustomSystemType.UseStyle.Activate)
                    {
                        debugstage = 220;
                        //var line = Type.OriginalXmlData.GetString("text_usage_activate");
                        //buffer.AddVarReplace(line, Type.VarStyle, AppendVarValue, false);
                    }
                    else
                    if (Type.Input == CustomSystemType.UseStyle.Toggle)
                    {
                        //var line = Type.OriginalXmlData.GetString("text_usage_toggle");
                        //ReplaceVariables(line, buffer, false);
                    }
                    else
                    if (Type.Input == CustomSystemType.UseStyle.Targeted)
                    {
                        //var line = Type.OriginalXmlData.GetString("text_usage_targeted");
                        //ReplaceVariables(line, buffer, false);
                    }
                    
                    return;
                }
                
                if (key.Equals("Click"))
                {
                    buffer.Add("[Click]");
                    return;
                }
                
                if (key.Equals("Binding"))
                {
                    string n = null;
                    var num = Type.InputNumber;
                    if (num == 1)
                        n = "Order_CustomSystem_1";
                    else if (num == 2)
                        n = "Order_CustomSystem_1";
                    else if (num == 3)
                        n = "Order_CustomSystem_1";
                    
                    if (n != null)
                    {
                        debugstage = 250;
                        var desc = InputActionTypeDataTable.Instance.GetHumanReadableKeyComboForAction(n);
                        buffer
                            .Add("[")
                            .Add(desc)
                            .Add("]");
                    }
                    
                    return;
                }
                
                // not found
                {
                    debugstage = 260;
                    buffer.Add("{").Add(key).Add("}");
                }
                
                debugstage = 500;
            }
            catch (Exception e)
            {
                LOG.Err("exception at debugstage {0}\n{1}", debugstage, e);
            }
        }
    }
    
    public class CustomSystemTypeTable_ExternalHooks : IArcenExternalCodeHookHandler
    {
        void IArcenExternalCodeHookHandler.HandleExternalHook( object MainObject, object SecondaryObject, object[] AdditionalObjects,
            ArcenExternalCodeHook Hook, ArcenSimContextBase Context )
        {
            //LOG.Msg("CustomSystemTypeTable_ExternalHooks.HandleExternalHook '{0}", Hook.InternalName);
            
            if (Hook.InternalName == "PostAllTableInitialize")
            {
                foreach (CustomSystemType row in CustomSystemTypeTable.Instance.Rows)
                {
                    int debugstage = 0;
                    try
                    {
                        debugstage = 10;
                        
                        string val = null;
                        row.OriginalXmlData.Fill("map", ref val, false);
                        
                        debugstage = 20;
                        
                        if (val != null)
                        {
                            debugstage = 30;
                            
                            var map = TextVarMapTable.Instance.GetRowByName(val);
                            
                            debugstage = 40;
                            if (map != null)
                            {
                                debugstage = 50;
                                row.UserData["map"] = map;
                            }
                            
                            debugstage = 60;
                        }
                    }
                    catch (Exception e)
                    {
                        LOG.Err("Exception at debugstage {0} in CustomSystemTypeTable_ExternalHooks for 'PostAllTableInitialize' processing CustomSystemType '{1}':\n{2}", 
                            debugstage, row.InternalName, e);
                    }
                }
            }
        }
    }

    public static partial class Extensions
    {
        public static ArcenCharacterBufferBase Add( this ArcenCharacterBufferBase buffer, string str, int start_idx, int len )
        {
            if ((start_idx+len) > str.Length)
                throw new ArgumentOutOfRangeException();
            
            for (int i = 0; i < len; i++)
                buffer.Add(str[start_idx+i]);

            return buffer;
        }
        
        unsafe public static bool Equals( this string str, string other, int other_start_idx, int other_len )
        {
            if (str.Length != other_len)
                return false;
            if (other.Length < (other_start_idx+other_len))
            {
                throw new ArgumentOutOfRangeException(
                    string.Format("string 'other' is length {0} which is less than the passed 'other_start_idx' ({1}) + 'other_len' ({2})", 
                        other.Length, other_start_idx, other_len));
            }
            
            fixed (char* ptr_a = str)
            fixed (char* ptr_b = other)
            {
                int j = other_start_idx;
                for (int i = 0; i < other_len; i++,j++)
                {
                    if (char.ToLowerInvariant(ptr_a[i]) != char.ToLowerInvariant(ptr_b[j]))
                        return false;
                }
            }
            
            return true;
        }
    }
}
