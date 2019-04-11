using Joint.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Joint.Govern.ViewModels
{
    public class SettingsViewModel
    {
        public string StatusMessage { get; set; }
        public CommonSettingsViewModel Common { get; set; }
        public WeChatSettingsViewModel WeChat { get; set; }

        public void LoadFrom(SettingService settings)
        {
            Common?.LoadFrom(settings);
            WeChat?.LoadFrom(settings);
        }
        public void StoreTo(SettingService settings)
        {
            Common?.StoreTo(settings);
            WeChat?.StoreTo(settings);
        }
    }
    public class SettingsViewModelBase<T> where T : class
    {
        private static Dictionary<string, PropertyInfo> settingProperties;
        public string StatusMessage { get; set; }
        static SettingsViewModelBase()
        {
            if (settingProperties == null)
            {
                settingProperties = (from p in typeof(T).GetProperties()
                                     let attrs = p.GetCustomAttributes(typeof(SettingsKeyAttribute), false)
                                     where attrs.Length > 0
                                     let key = (attrs[0] as SettingsKeyAttribute).Key
                                     select new { p, key })
                                 .ToDictionary(pk => pk.key, pk => pk.p);
            }
        }

        public void LoadFrom(SettingService settings)
        {
            foreach (var kp in settingProperties)
            {
                var key = kp.Key;
                var pinfo = kp.Value;

                var value = settings[key];
                pinfo.SetValue(this, value);
            }
        }

        public void StoreTo(SettingService settings)
        {
            foreach (var kp in settingProperties)
            {
                var key = kp.Key;
                var pinfo = kp.Value;

                var value = pinfo.GetValue(this)?.ToString() ?? "";
                settings[key] = value;
            }
        }
    }

    public class CommonSettingsViewModel : SettingsViewModelBase<CommonSettingsViewModel>
    {
        //[Required(ErrorMessage = "请输入uniFLOW REST服务地址")]
        [Display(Name = "uniFLOW REST服务地址")]
        [SettingsKey(Key = SettingsKey.UniflowServiceURL)]
        public string UniflowServiceURL { get; set; }

        [Required(ErrorMessage = "uniFLOW服务器Host")]
        [Display(Name = "uniFLOW服务器Host")]
        [SettingsKey(Key = SettingsKey.UniflowDBHost)]
        public string UniflowDBHost { get; set; }

        [Required(ErrorMessage = "请输入uniFLOW REST服务密钥")]
        [Display(Name = "uniFLOW REST服务密钥")]
        [SettingsKey(Key = SettingsKey.UniflowServiceEncryptKey)]
        public string UniflowServiceEncryptKey { get; set; }

        [Required(ErrorMessage = "请输入HOT目录路径")]
        [Display(Name = "HOT目录路径")]
        [SettingsKey(Key = SettingsKey.UniflowServiceTaskTargetPath)]
        public string UniflowServiceTaskTargetPath { get; set; }

        [Required(ErrorMessage = "请输入访问HOT目录的用户名")]
        [Display(Name = "访问HOT目录用户名")]
        [SettingsKey(Key = SettingsKey.UniflowUncUser)]
        public string UniflowUncUser { get; set; }

        [Required(ErrorMessage = "请输入访问HOT目录的用户密码")]
        [Display(Name = "访问HOT目录用户密码")]
        [SettingsKey(Key = SettingsKey.UniflowUncPassword)]
        public string UniflowUncPassword { get; set; }

    }

    public class WeChatSettingsViewModel : SettingsViewModelBase<WeChatSettingsViewModel>
    {
        //[Display(Name = "微信AppId")]
        //[SettingsKey(Key = SettingsKey.WeChatWxAppId)]
        //public string WeChatWxAppId { get; set; }

        //[Display(Name = "微信Secret")]
        //[SettingsKey(Key = SettingsKey.WeChatWxSecret)]
        //public string WeChatWxSecret { get; set; }

        [Required(ErrorMessage = "请输入企业微信AppId")]
        [Display(Name = "企业微信CorpId")]
        [SettingsKey(Key = SettingsKey.WxWorkAppId)]
        public string WxWorkAppId { get; set; }

        [Required(ErrorMessage = "请输入企业微信Secret")]
        [Display(Name = "企业微信Secret")]
        [SettingsKey(Key = SettingsKey.WxWorkSecret)]
        public string WxWorkSecret { get; set; }

        [Required(ErrorMessage = "请输入企业微信AgentId")]
        [Display(Name = "企业微信AgentId")]
        [SettingsKey(Key = SettingsKey.WxWorkAgentId)]
        public string WxWorkAgentId { get; set; }

        [Required(ErrorMessage = "请输入企业微信后台打印机SN编号")]
        [Display(Name = "企业微信后台打印机SN编号")]
        [SettingsKey(Key = SettingsKey.WxWorkIOTPrinterSN)]
        public string WxWorkIOTPrinterSN { get; set; }

        [Required(ErrorMessage = "请输入企业微信后台打印机Secret")]
        [Display(Name = "企业微信后台打印机Secret")]
        [SettingsKey(Key = SettingsKey.WxWorkIOTSecret)]
        public string WxWorkIOTSecret { get; set; }
    }
}
