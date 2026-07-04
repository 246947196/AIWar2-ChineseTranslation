using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public partial class Input_DebugHandler
    {
        private static float _lastLogDeleteTime = -1.0f;
        
        // clears the console file
        partial void GenericCommand1(float input)
        {
            if (input > 0.0f)
            {
                var T = ArcenTime.TimeSinceStartF;
                if ((T - _lastLogDeleteTime)< 1.0f)
                    return;
                
                _lastLogDeleteTime = T;
                File.WriteAllText(Engine_Universal.CurrentPlayerDataDirectory + "ArcenDebugLog.txt", "\n");
                
                LOG.Chat("清除日志！");
            }
        }
        
        partial void GenericCommand2(float input)
        {
            if (input > 0.0f)
            {
                bool resave_meta = GameSettings.Current.GetBoolBySetting("ResaveAllSaveMeta", false);
                resave_meta = !resave_meta;
                GameSettings.Current.SetBoolBySetting("ResaveAllSaveMeta", resave_meta);
                
                LOG.Chat("重新保存所有存档元数据状态：{0}",resave_meta ? "开启" : "关闭");
            }
        }
        
        partial void GenericCommand3(float input)
        {
            if (input > 0.0f)
            {
                Core.Stacking.Metrics.Instance.Log();
            }
        }
        partial void GenericCommand4(float input)
        {

        }
    }
}
