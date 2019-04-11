using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Web;
using Microsoft.Extensions.Logging;
using UniFlowGW.Services;
using System.Threading;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Net;
using System.Globalization;
using Microsoft.AspNetCore.Diagnostics;
using InvalidDataException = Joint.Core.Exceptions.InvalidDataException;
using Joint.Core.Services;
using Joint.Web.Single.Models;
using Joint.Govern.Utilities;
using Joint.Core.Utilities;

namespace Joint.Web.Single.Controllers
{
    //[WxWorkAccessFilter]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class HomeController : Controller // : BaseController
    {
        //readonly DatabaseContext _ctx;
        readonly ILogger<HomeController> _logger;

        //readonly IBackgroundTaskQueue queue;
        //UniflowController _uniflow;
        readonly SettingService settings;

        public HomeController(
            SettingService settings,
            //DatabaseContext ctx,
            //IBackgroundTaskQueue queue,
            ILogger<HomeController> logger
            //UniflowController uniflow,
            //UncHelper uncHelper
            )
        {
            //_ctx = ctx;
            //this.queue = queue;
            this.settings = settings;
            _logger = logger;
            //_uniflow = uniflow;
        }


        public async Task<ActionResult> Unlock(string data)
        {
            _logger.LogInformation(string.Format("[HomeController] [Unlock] data:{0}", data));

            var printerSN = "";
            var uniFLOWRestServiceURL = "";
            string key = settings["UniflowService:EncryptKey"];
            if (!string.IsNullOrEmpty(data))
            {
                var decryptData = "";
                try
                {
                    decryptData = EncryptUtil.Decrypt(key, data);
                }
                catch (Exception)
                {
                    return View("Error", new ErrorViewModel { Message = "二维码数据无效。" });
                }
                var parts = decryptData.Split('@');
                if (parts.Length < 4 ||
                    !Uri.IsWellFormedUriString(parts[0], UriKind.Absolute) ||
                    string.IsNullOrEmpty(parts[1]))
                {
                    return View("Error", new ErrorViewModel { Message = "二维码数据无效。" });
                }
                var validTime = int.Parse(settings["UniflowService:QRCodeValidTime"]);
                var datetime = DateTime.ParseExact(parts[2], "MMddyyyyHHmmss", CultureInfo.InvariantCulture);
                _logger.LogInformation(string.Format("[HomeController] [Unlock] QR Code=> UniFLOWRestServiceURL:{0},PrinterSN:{1}, ValidTime:{2}", parts[0], parts[1], datetime.ToLongDateString()));
                /*
                if (datetime.AddMinutes(validTime).CompareTo(DateTime.Now) < 0)
                {
                    return View("Error", new ErrorViewModel { Message = "二维码已经过期。" });
                }
                */
                uniFLOWRestServiceURL = parts[0];
                printerSN = parts[1];
                HttpContext.Session.SetCurrentPrinterSN(printerSN);
                //HttpContext.Session.SetUniFLOWRestServiceURL(uniFLOWRestServiceURL);
            }

            if (string.IsNullOrEmpty(printerSN))
                return View("Error", new ErrorViewModel { Message = "没有当前打印机，请重新扫描打印机二维码。" });

            if (string.IsNullOrEmpty(uniFLOWRestServiceURL))
                return View("Error", new ErrorViewModel { Message = "配置错误，请重新扫描打印机二维码。" });

            var bindId = HttpContext.Session.GetBindId();
            if (string.IsNullOrEmpty(bindId))
            {
                if (bool.TryParse(settings["NoLogin"], out bool noLogin) && noLogin)
                    return View("Error", new ErrorViewModel { Message = "会话已过期，请重新进入。" });

                return RedirectToAction("Login", new { backto = WebUtility.UrlEncode(Url.Action("Unlock", new { data })) });
            }
            if (bindId.IsNoLoginBind())
            {
                return View("Error", new ErrorViewModel { Message = "暂不支持打印机扫码开机。" });
            }
            var result = await _uniflow.Unlock(new UnlockRequest { UniFLOWRestServiceURL = uniFLOWRestServiceURL, BindId = bindId, Serial = printerSN });
            _logger.LogInformation(string.Format("[HomeController] [Unlock] result:{0},SN:{1},BindId:{2}", result.Value.Code, printerSN, bindId));
            ViewBag.Result = result.Value.Code == "0";
            return View();
        }

        public IActionResult Index()
        {
            var bindId = HttpContext.Session.GetBindId();
            _logger.LogInformation(string.Format("[HomeController] [Index] bindId:{0}", bindId));
            if (string.IsNullOrEmpty(bindId))
            {
                //目前只有龙信的打印服务不需要绑定LDAP帐号。
                if (bool.TryParse(settings["NoLogin"], out bool noLogin) && noLogin)
                    return View("Error", new ErrorViewModel { Message = "会话已过期，请重新进入。" });

                return RedirectToAction("Login", new { backto = WebUtility.UrlEncode(Url.Action()) });
            }
            return RedirectToAction("History");
        }

        [HttpGet]
        public IActionResult Login(string backto)
        {
            if (bool.TryParse(settings["NoLogin"], out bool noLogin) && noLogin)
                return NotFound();

            var ua = Request.Headers["User-Agent"].ToString();
            _logger.LogInformation(string.Format("[HomeController] [Login] userAgent:{0}, backto{1}", ua, backto));

            var enable = bool.TryParse(settings["WeChat:Enable"], out var value) && value;
            if (enable && Regex.IsMatch(ua, "MicroMessenger", RegexOptions.IgnoreCase)) // wechat
            {
                var isWxwork = Regex.IsMatch(ua, "wxwork", RegexOptions.IgnoreCase);
                var wxworkOauthUrl = string.Format(
                    settings["WeChat:OAuth2UrlPattern"],
                    isWxwork ? settings["WeChat:WxWork:AppId"] : settings["WeChat:Wx:AppId"],
                    WebUtility.UrlEncode(Url.Action("OAuth2Callback", "Home", new { backto }, Request.Scheme)) // calback
                                                                                                               // state
                    );
                _logger.LogInformation("[HomeController] [Login] Oauth-URL:" + wxworkOauthUrl);
                return Redirect(wxworkOauthUrl);
            }
            UserViewModel model = new UserViewModel();
            var (externId, type) = HttpContext.Session.GetExternId();
            if (!string.IsNullOrEmpty(externId))
            {
                model.UserName = externId;
            }
            return View("Bind", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(UserViewModel model, string backto)
        {
            if (bool.TryParse(settings["NoLogin"], out bool noLogin) && noLogin)
                return NotFound();


            if (!ModelState.IsValid)
            {
                return View("Bind", model);
            }
            var uniFLOWRestServiceURL = this.GetUniFLOWRestServiceURL();
            var req = new LoginPasswordRequest() { UniFLOWRestServiceURL = uniFLOWRestServiceURL, Login = model.UserName.Trim(), Password = model.Password.Trim() };
            var checkResult = _uniflow.CheckUser(req);
            _logger.LogInformation(string.Format("[HomeController] [Login] CheckUser result:{0}", checkResult.Result.Value.Code));
            if (checkResult.Result.Value.Code != "0")
            {
                ModelState.AddModelError("errorMsg", "用户名或密码错误!");
                return View("Bind", model);
            }

            var bindId = checkResult.Result.Value.BindId;
            var externId = model.UserName.Trim().ToLower();
            var type = "LDAPLogin";
            HttpContext.Session.SetExternId(externId, type);

            //var bindResult = _uniflow.Bind(
            //    new BindExternalIdRequest
            //    {
            //        UniFLOWRestServiceURL = HttpContext.Session.GetUniFLOWRestServiceURL(),
            //        ExternalId = externId,
            //        Type = type,
            //        BindId = bindId,
            //    });
            _logger.LogInformation("[HomeController] [Login] [Bind] BindResult:" + bindResult.Result.Value.Code);
            if (bindResult.Result.Value.Code != "0")
            {
                ModelState.AddModelError("errorMsg", bindResult.Result.Value.Message);
                return View("Bind", model);
            }

            HttpContext.Session.SetBindId(bindId);
            HttpContext.Session.SetLdapLoginId(model.UserName);

            if (!string.IsNullOrEmpty(backto))
                return Redirect(WebUtility.UrlDecode(backto));
            return RedirectToAction("Index");
        }

        public IActionResult OAuth2Callback(string code, string backto, string state)
        {
            _logger.LogInformation(string.Format("[HomeController] [OAuth2Callback] code:{0},state:{1}", code, state));
            var ua = Request.Headers["User-Agent"].ToString();
            string externId = "", type = "";
            if (Regex.IsMatch(ua, "MicroMessenger", RegexOptions.IgnoreCase)) // wechat
            {
                try
                {
                    var isWxWork = Regex.IsMatch(ua, "wxwork", RegexOptions.IgnoreCase);
                    if (isWxWork)
                        (externId, type) = WxWorkCallback(code);
                    else
                        (externId, type) = WxCallback(code);
                }
                catch (OAuthException ex)
                {
                    return View("Error", new ErrorViewModel { Message = ex.Message });
                }
            }
            else
            {
                return View("Error", new ErrorViewModel { Message = "不支持从该平台访问！" });
            }

            HttpContext.Session.SetExternId(externId, type);
            var uniFLOWRestServiceURL = this.GetUniFLOWRestServiceURL();
            var checkResult = _uniflow.CheckBind(
                new ExternalIdRequest { UniFLOWRestServiceURL = uniFLOWRestServiceURL, ExternalId = externId, Type = type });

            _logger.LogInformation(string.Format("[HomeController] [OAuth2Callback] [CheckBind] Result:{0},externId:{1},type:{2}", checkResult.Value.Code, externId, type));
            if (checkResult.Value.Code != "0")
            {
                return RedirectToAction("Bind", new { backto });
            }

            var bindId = checkResult.Value.BindId;
            HttpContext.Session.SetBindId(bindId);
            HttpContext.Session.SetLdapLoginId(checkResult.Value.LdapLoginId);
            if (!string.IsNullOrEmpty(backto))
                return Redirect(WebUtility.UrlDecode(backto));
            return RedirectToAction("Index");
        }

        (string externId, string type) WxCallback(string code)
        {
            string getAccessTokenURL = string.Format(
                settings["WeChat:Wx:GetTokenUrlPattern"],
                settings["WeChat:Wx:AppId"],
                settings["WeChat:Wx:Secret"],
                code);

            var resGetToken = RequestUtil.HttpGet(getAccessTokenURL);
            _logger.LogInformation("[HomeController] [WxCallback] response: " + resGetToken);
            var model = JsonHelper.DeserializeJsonToObject<AccessTokenModel>(resGetToken);

            return (model.openid, "WeChatOpenId");
        }

        (string externId, string type) WxWorkCallback(string code)
        {
            string getAccessTokenURL = string.Format(
                settings["WeChat:WxWork:GetTokenUrlPattern"],
                settings["WeChat:WxWork:AppId"],
                settings["WeChat:WxWork:Secret"]);

            var resGetToken = RequestUtil.HttpGet(getAccessTokenURL);
            string accessToken = JsonHelper.DeserializeJsonToObject<AccessTokenModel>(resGetToken).access_token;

            string userinfoURL = string.Format(
                settings["WeChat:WxWork:GetUserInfoUrlPattern"],
                accessToken,
                code);
            var resUserInfo = RequestUtil.HttpGet(userinfoURL);
            _logger.LogInformation("[HomeController] [WxWorkCallback] response: " + resUserInfo);
            var corpModel = JsonHelper.DeserializeJsonToObject<CorpModel>(resUserInfo);

            string userId = "";
            string type = "WxWorkUserID";
            if (!string.IsNullOrEmpty(corpModel.UserId))
            {
                userId = corpModel.UserId;

            }
            else if (!string.IsNullOrEmpty(corpModel.OpenId))
            {
                userId = corpModel.OpenId;
                type = "WxWorkOpenID";
            }
            else
            {
                throw new OAuthException("oAuth认证错误，请稍后重试！");
            }
            return (userId, type);
        }

        [HttpGet]
        public IActionResult Bind(string backto)
        {
            UserViewModel model = new UserViewModel();
            var (externId, type) = HttpContext.Session.GetExternId();
            if (!string.IsNullOrEmpty(externId))
            {
                model.UserName = externId;
            }
            else
            {
                return View("Error", new ErrorViewModel { Message = "请用企业微信扫描打印机二维码！" });
            }
            return View("Bind", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Bind(UserViewModel model, string backto)
        {
            if (bool.TryParse(settings["NoLogin"], out bool noLogin) && noLogin)
                return NotFound();

            var (externId, type) = HttpContext.Session.GetExternId();
            if (string.IsNullOrEmpty(externId))
            {
                return RedirectToAction("Login");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var uniFLOWRestServiceURL = this.GetUniFLOWRestServiceURL();
            var req = new LoginPasswordRequest() { UniFLOWRestServiceURL = uniFLOWRestServiceURL, Login = model.UserName.Trim(), Password = model.Password.Trim() };
            var checkResult = _uniflow.CheckUser(req);
            _logger.LogInformation(string.Format("[HomeController] [Bind] CheckUser result:{0}", checkResult.Result.Value.Code));
            if (checkResult.Result.Value.Code != "0")
            {
                ModelState.AddModelError("errorMsg", "用户名或密码错误!");
                return View(model);
            }

            var bindId = checkResult.Result.Value.BindId;

            var bindResult = _uniflow.Bind(
                new BindExternalIdRequest
                {
                    UniFLOWRestServiceURL = HttpContext.Session.GetUniFLOWRestServiceURL(),
                    ExternalId = externId,
                    Type = type,
                    BindId = bindId,
                });
            _logger.LogInformation("[HomeController] [Bind] BindResult:" + bindResult.Result.Value.Code);
            if (bindResult.Result.Value.Code != "0")
            {
                ModelState.AddModelError("errorMsg", bindResult.Result.Value.Message);
                return View(model);
            }

            HttpContext.Session.SetBindId(bindId);
            HttpContext.Session.SetLdapLoginId(model.UserName.Trim().ToLower());

            if (!string.IsNullOrEmpty(backto))
                return Redirect(WebUtility.UrlDecode(backto));
            return RedirectToAction("Index");
        }

        public IActionResult UnBind()
        {
            var bindId = HttpContext.Session.GetBindId();
            if (string.IsNullOrEmpty(bindId))
            {
                if (bool.TryParse(settings["NoLogin"], out bool noLogin) && noLogin)
                    return View("Error", new ErrorViewModel { Message = "会话已过期，请重新进入。" });

                return RedirectToAction("Login", new { backto = WebUtility.UrlEncode(Url.Action()) });
            }

            var (externId, type) = HttpContext.Session.GetExternId();
            if (string.IsNullOrEmpty(externId))
            {
                return RedirectToAction("Login", new { backto = WebUtility.UrlEncode(Url.Action()) });
            }

            try
            {
                var findUser = _ctx.ExternBindings.Where(s => s.ExternalId.ToLower() == externId.ToLower() && s.Type == type).SingleOrDefault();
                if (null != findUser)
                {
                    _ctx.ExternBindings.Remove(findUser);
                    _ctx.SaveChangesAsync();
                    _logger.LogInformation(string.Format("Remove WechatUser:{0}-{1}", findUser.BindUserId, findUser.ExternalId));
                }
                var any = _ctx.ExternBindings.Any(s => s.BindUserId.ToLower() == bindId.ToLower());
                if (!any)
                {
                    var user = _ctx.BindUsers.FirstOrDefault(u => u.BindUserId.ToLower() == bindId.ToLower());
                    _ctx.BindUsers.Remove(user);
                    _ctx.SaveChangesAsync();
                    _logger.LogInformation(string.Format("Remove Account:{0}", findUser.BindUserId));
                }
                HttpContext.Session.Remove(SessionKeys.BindId);
                HttpContext.Session.Remove(SessionKeys.ExternId);
                HttpContext.Session.Clear();
                ViewBag.Result = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                ViewBag.Result = false;
            }

            return View("UnBind");
        }

        public IActionResult History()
        {
            var bindId = HttpContext.Session.GetBindId();
            _logger.LogInformation(string.Format("[HomeController] [History] bindId:{0}", bindId));
            if (string.IsNullOrEmpty(bindId))
            {
                if (bool.TryParse(settings["NoLogin"], out bool noLogin) && noLogin)
                    return View("Error", new ErrorViewModel { Message = "会话已过期，请重新进入。" });

                return RedirectToAction("Login", new { backto = WebUtility.UrlEncode(Url.Action()) });
            }

            return View();
        }



        private string GetUniFLOWRestServiceURL()
        {
            var url = HttpContext.Session.GetUniFLOWRestServiceURL();
            if (!string.IsNullOrEmpty(url))
                return url;
            else if (!string.IsNullOrEmpty(settings[SettingsKey.UniflowServiceURL]))
            {
                return settings[SettingsKey.UniflowServiceURL];

            }
            else
            {
                throw new InvalidDataException("请先扫描打印机屏幕二维码！");
            }
        }


        #region  LX Customization

        [HttpGet("lxupload")]
        public IActionResult LXLogin(string sign)
        {
            string account = "guest";
            try
            {
                string validSignURL = string.Format(settings["LxValidSignURL"], HttpUtility.UrlEncode(sign));
                var headers = new Dictionary<string, string>()
                {
                    ["X-LONGCHAT-AppKey"] = settings["LxAppKey"],
                };
                var accountJson = RequestUtil.HttpGet(validSignURL, headers);
                _logger.LogInformation("AccountJson:" + accountJson);
                var signModel = JsonHelper.DeserializeJsonToObject<LxSignModel>(accountJson);

                if (signModel != null && signModel.data != null)
                {
                    account = signModel.data.lxAccount;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return View("Error", new ErrorViewModel { Message = "身份验证失败。" });
            }
            HttpContext.Session.SetLdapLoginId(account);
            HttpContext.Session.SetBindId(SessionKeys.NoLoginBindIdValue);

            return RedirectToAction("Index");
        }

        #endregion


        #region unused


        private IActionResult QR(string data)
        {
            _logger.LogInformation("[HomeController] [Login] [QR] data:" + data);
            if (!string.IsNullOrEmpty(data))
            {
                string key = settings["UniflowService:EncryptKey"];
                try
                {
                    data = EncryptUtil.Decrypt(key, data);
                    _logger.LogInformation("[HomeController] [Login] [QR] Decrypt data:" + data);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("Failed to decode qrcode: " + ex.Message);
                    data = null;
                }
                if (data != null)
                {

                    var parts = data.Split('@');
                    if (parts.Length < 4 ||
                        !Uri.IsWellFormedUriString(parts[0], UriKind.Absolute) ||
                        string.IsNullOrEmpty(parts[1]))
                    {
                        _logger.LogInformation("invalid BarcodeData format: " + data);
                    }
                    else
                    {
                        HttpContext.Session.SetCurrentPrinterSN(parts[1]);
                    }
                }
            }
            return RedirectToAction("Index");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        private IActionResult Result(PrintViewModel model)
        {
            var bindId = HttpContext.Session.GetBindId();
            _logger.LogInformation(string.Format("[HomeController] [Result] bindId:{0}", bindId));
            if (string.IsNullOrEmpty(bindId))
            {
                if (bool.TryParse(settings["NoLogin"], out bool noLogin) && noLogin)
                    return View("Error", new ErrorViewModel { Message = "会话已过期，请重新进入。" });

                return RedirectToAction("Login", new { backto = WebUtility.UrlEncode(Url.Action("Index")) });
            }

            if (!ModelState.IsValid)
                return View("Error", new ErrorViewModel
                {
                    Message = ModelState.First(m => m.Value.Errors.Count > 0).Value.Errors[0].ErrorMessage
                });

            var document = model.Document.FileName;
            var ext = Path.GetExtension(document).ToLower();

            var allowed = (settings["ConvertibleFileTypes"] + ";" +
                settings["ImageFileTypes"] + ";" +
                settings["DirectHandledFileTypes"]).ToLower().Split(';');

            if (!allowed.Contains(ext.ToLower()))
                return View("Error", new ErrorViewModel { Message = "Document type not supported." });

            var uploadPath = Path.GetTempFileName() + ext;
            _logger.LogInformation("Upload File Path:" + uploadPath);
            using (var outstream = System.IO.File.OpenWrite(uploadPath))
                model.Document.CopyTo(outstream);

            var temp = Path.GetTempFileName();
            var tempdoc = uploadPath;

            var convertExts = settings["ConvertibleFileTypes"].ToLower().Split(';');
            var imageExts = settings["ImageFileTypes"].ToLower().Split(';');
            var directExts = settings["DirectHandledFileTypes"].ToLower().Split(';');

            var isDirect = directExts.Contains(ext);
            var isConvert = convertExts.Contains(ext);
            var isImage = imageExts.Contains(ext);
            var template = Template.ticketPdf;

            var loginId = HttpContext.Session.GetLdapLoginId();

            var task = new PrintTask
            {
                PrintModel = new PrintTaskDetail
                {
                    Path = uploadPath,
                    RequestId = model.RequestId,
                    Document = document,
                    Copies = model.Copies,
                    Orientation = model.Orientation,
                    ColorMode = model.ColorMode,
                    PaperMode = model.PaperMode,
                    PaperSize = model.PaperSize,
                },
                Document = document,
                Status = PrintTaskStatus.Processing,
                Time = DateTime.Now,
                UserID = loginId,
            };

            try
            {
                if (isDirect)
                {
                    tempdoc += ext;
                    System.IO.File.Copy(uploadPath, tempdoc);
                }
                else if (isConvert)
                {
                    task.QueuedTask = true;
                }
                else if (isImage)
                {
                    tempdoc += ".jpg";
                    if ((ext == ".jpg" || ext == ".jpeg") && model.ColorMode == ColorMode.Color)
                        System.IO.File.Copy(uploadPath, tempdoc);
                    else if (!RunImageConvert(uploadPath, tempdoc, model.ColorMode == ColorMode.BW) ||
                        !System.IO.File.Exists(tempdoc))
                    {
                        task.Status = PrintTaskStatus.Failed;
                        task.Message = "Failed to convert document.";
                    }
                    template = Template.tickettempImage;
                }
                else
                {
                    task.Status = PrintTaskStatus.Failed;
                    task.Message = "Document type not supported.";
                }

                if (task.Status == PrintTaskStatus.Failed)
                    return View("Error", new ErrorViewModel { Message = task.Message });

                if (!task.QueuedTask)
                {
                    MoveToOutput(task.PrintModel, loginId, tempdoc, template);
                    task.Status = PrintTaskStatus.Committed;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                task.Status = PrintTaskStatus.Failed;
                task.Message = "Internal error";
                throw;
            }
            finally
            {
                _ctx.PrintTasks.Add(task);
                _ctx.SaveChanges();
            }

            if (task.QueuedTask) // wake up the queue
                queue.QueueBackgroundWorkItem(RunConvertInQueue);

            return View(task.PrintModel);
        }


        private Task RunConvertInQueue(DatabaseContext ctx, CancellationToken token)
        {
            var tasks = from task in ctx.PrintTasks
                        where task.Status == PrintTaskStatus.Processing && task.QueuedTask
                        orderby task.Time ascending
                        select task;
            foreach (var task in tasks)
            {
                var temp = Path.GetTempFileName();
                var tempdoc = temp + ".pdf";
                try
                {
                    if (!RunConvert(task.PrintModel.Path, tempdoc,
                            task.PrintModel.Orientation == Orientation.Landscape) ||
                        !System.IO.File.Exists(tempdoc))
                    {
                        task.Status = PrintTaskStatus.Failed;
                        task.Message = "Failed to convert document.";
                    }
                    else
                    {
                        MoveToOutput(task.PrintModel, task.UserID, tempdoc, Template.ticketPdf);
                        task.Status = PrintTaskStatus.Committed;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    task.Status = PrintTaskStatus.Failed;
                    task.Message = "Internal error";
                    //throw;
                }
                finally
                {
                    System.IO.File.Delete(temp);
                    ctx.SaveChanges();
                }
            }
            return Task.CompletedTask;
        }

        bool RunConvert(string source, string target, bool landscape)
        {
            var landscapearg = landscape ? "-landscape" : "-portrait";
            var processInfo = new ProcessStartInfo
            {
                FileName = settings["PdfConverter"],
                Arguments = $"\"{source}\" \"{target}\" {landscapearg}",
                CreateNoWindow = false,
            };

            try
            {
                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    var result = process.ExitCode;
                    return result == 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return false;
            }
        }

        bool RunImageConvert(string source, string target, bool grayscale)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = settings["ImageConverter"],
                    Arguments = $"\"{source}\" \"{target}\" jpg {(grayscale ? "grayscale" : "color")}",
                    CreateNoWindow = false,
                };


                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    var result = process.ExitCode;
                    return result == 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return false;
            }
        }


        private void MoveToOutput(PrintTaskDetail model, string uid, string tempdoc, string template)
        {
            Guid guid = Guid.NewGuid();
            var outdoc = guid.ToString() + Path.GetExtension(tempdoc);
            var ticket = template
                .Replace("$USERID$", uid)
                .Replace("$PATH$", outdoc)
                .Replace("$FILENAME$", System.Security.SecurityElement.Escape(Path.GetFileName(model.Document)))
                .Replace("$DISTRIBUTION$", settings[SettingsKey.UniflowDistribution])
                .Replace("$COPIES$", model.Copies.ToString())
                .Replace("$COLORMODE$", model.ColorMode.ToString())
                .Replace("$PAPERSIZE$", ((int)model.PaperSize).ToString())
                .Replace("$DUPLEX$", model.PaperMode.ToString());
            var tempxml = tempdoc + ".xml";
            System.IO.File.WriteAllText(tempxml, ticket);

            var targetPaths = settings["UniflowService:TaskTargetPath"];
            foreach (var targetPath in targetPaths.Split(';'))
            {
                var targetdoc = Path.Combine(targetPath.Trim(), outdoc);
                var targetxml = Path.Combine(targetPath.Trim(), guid.ToString() + ".xml");
                System.IO.File.Copy(tempdoc, targetdoc);
                System.IO.File.Copy(tempxml, targetxml);
            }

            System.IO.File.Delete(tempdoc);
            System.IO.File.Delete(tempxml);
        }


        #endregion

    }
}
