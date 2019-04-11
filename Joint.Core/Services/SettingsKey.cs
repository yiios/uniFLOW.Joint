using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Joint.Core.Services
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SettingsKeyAttribute : Attribute
    {
        public string Key { get; set; }
    }
    public static class SettingsKey
    {
        public const string SystemTempFolder = "System:TempFolder";
        public const string UniflowServiceURL = "UniflowService:Url";
        public const string UniflowServiceEncryptKey = "UniflowService:EncryptKey";
        public const string UniflowServiceTaskTargetPath = "UniflowService:TaskTargetPath";
        public const string UniflowUncUser = "UniflowService:UncUser";
        public const string UniflowUncPassword = "UniflowService:UncPassword";
        public const string UniflowDistribution = "UniflowService:Distribution";
        public const string UniflowDBHost = "UniflowService:DBHost";
        public const string WeChatWxAppId = "WeChat:Wx:AppId";
        public const string WeChatWxSecret = "WeChat:Wx:Secret";
        public const string WxWorkAppId = "WeChat:WxWork:AppId";
        public const string WxWorkSecret = "WeChat:WxWork:Secret";
        public const string WxWorkAgentId = "WeChat:WxWork:AgentId";
        public const string WxWorkIOTPrinterSN = "WeChat:WxWorkIOT:PrinterSN";
        public const string WxWorkIOTSecret = "WeChat:WxWorkIOT:Secret";




    }
}
