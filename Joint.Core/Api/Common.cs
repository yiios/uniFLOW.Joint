using System;
using System.Collections.Generic;
using System.Text;

namespace Joint.Core.Api
{
    public static class Error
    {
        public enum Codes
        {
            OK = 0,
            LoginRequired = 100,
            UserNotFound = 101,
            InvalidUserId = 102,
            InvalidUserData = 103,
            DuplicateUser = 104,
            BindNotFound = 109,
            PrinterNotFound = 201,
            PrinterNotConfigured = 202,
            EncryptError = 701,
            DecryptError = 702,
            InvalidData = 801,
            LoadObjectError = 802,
            Exception = 900,
            ExternalError = 901,
        }

        public static Dictionary<Codes, string> Messages { get; }
        = new Dictionary<Codes, string>()
        {
            [Codes.OK] = "成功",
            //[Codes.InvalidData] = "无效数据: [{0}]: {1}",
            //[Codes.Exception] = "未知错误"
        };
        public static string AsMessage(this Codes code, params object[] args)
        {
            if (Messages.ContainsKey(code))
                return string.Format(Messages[code], args);
            return code.ToString() + ":" + string.Join(",", args);
        }
        public static string AsString(this Codes code)
            => ((int)code).ToString();
        public static Codes AsCode(this string codestr)
            => Enum.Parse<Codes>(codestr);
    }

    public class BaseResponseBody
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }

    public class StatusResponse : BaseResponseBody
    {
    }


}
