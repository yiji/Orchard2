using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orchard.Users.Models;
using Orchard.Users.ViewModels;
using Orchard.Users.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using YesSql;
using Orchard.MPUnovo.Models;
using Orchard.MPUnovo.Indexes;
using OdooRpc.CoreCLR.Client.Models;
using OdooRpc.CoreCLR.Client;
using OdooRpc.CoreCLR.Client.Models.Parameters;
using Newtonsoft.Json.Linq;
using MimeKit;
using MailKit.Net.Smtp;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Text;
using Orchard.MPUnovo.Components;

namespace Orchard.MPUnovo.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger _logger;
        private readonly string _externalCookieScheme;
        private readonly YesSql.ISession _session;
        private readonly UserManager<User> _userManager;
        private readonly OdooConnectionInfo _odooConnection;

        public AccountController(
            IUserService userService,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            YesSql.ISession session,
            IOptions<IdentityCookieOptions> identityCookieOptions,
            ILogger<AccountController> logger,
            OdooConnectionInfo odooConnection)
        {
            _signInManager = signInManager;
            _session = session;
            _userService = userService;
            _userManager = userManager;
            _externalCookieScheme = identityCookieOptions.Value.ExternalCookieAuthenticationScheme;
            _logger = logger;
            _odooConnection = odooConnection;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string partnerCode = null, string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.Authentication.SignOutAsync(_externalCookieScheme);

            //如果带了partnerCode参数就看用户是否存在
            ViewBag.PartnerCode = "";
            if (partnerCode != null)
            {
                try
                {
                    byte[] bytes = Convert.FromBase64String(partnerCode);
                    string trueCode = Encoding.UTF8.GetString(bytes);


                    var user = await _userManager.FindByNameAsync(trueCode);
                    if (user != null)
                    {
                        bool hasPassword = await _userManager.HasPasswordAsync(user);
                        if (!hasPassword)
                        {
                            //发送修改密码邮件，加入安全码，防止个别供应商伪造看别的供应商
                            // Send an email with this link
                            var vertifyCode = await _userManager.GeneratePasswordResetTokenAsync(user);
                            var callbackUrl = Url.Action("ResetPassword", "Account",
                                new { userId = user.Id, code = WebUtility.UrlEncode(vertifyCode) }, protocol: HttpContext.Request.Scheme);

                            bool success = await EmailHelper.SendEmail(user.Email, "密码重置", $"您好，由于这是您第一次登录系统，请点击此链接重置密码: <a href='{callbackUrl}'>重置密码</a>");


                            //return RedirectToAction("ChangePassword");
                            ViewBag.Message = $"由于这是您第一次登录系统，请查看{user.Email}邮件,重置密码登录！";
                            return View("Error");
                        }
                        ViewBag.PartnerCode = user.UserName;
                    }
                }
                catch
                {
                    //return RedirectToAction("ChangePassword");
                    ViewBag.Message = $"请从询价邮件中点击链接登录，如果您记得您的供应商编码也可直接<a href='/Login'>登录</a>";
                    return View("Error");
                }
            }
            ViewData["ReturnUrl"] = returnUrl;
            Random r = new Random();
            string code = r.Next(100000, 1000000).ToString();
            ViewBag.VerificationCode = code;
            HttpContext.Session.SetString("verificationcode", code);
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> MobileLogin(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.Authentication.SignOutAsync(_externalCookieScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string verificationcode, string returnUrl = null)
        {
            try
            {
                if (HttpContext.Session.GetString("verificationcode") != verificationcode)
                {
                    ModelState.AddModelError(string.Empty, "验证码错误！");
                    //return View(model);
                }
                else
                {
                    ViewData["ReturnUrl"] = returnUrl;
                    if (ModelState.IsValid)
                    {
                        //判断用户是否存在，及是否有密码，代码创建用户时没有密码
                        var user = await _userManager.FindByNameAsync(model.UserName);
                        if (user == null)
                        {
                            //用户不存在去erp判断用户是否存在
                            OdooRpcClient odooRpcClient = new OdooRpcClient(_odooConnection);
                            await odooRpcClient.Authenticate();

                            if (odooRpcClient.SessionInfo.IsLoggedIn)
                            {

                                //UnovoParameters unovoParam = new UnovoParameters("unovo.interface", @"{""method"": ""supplier_partner_validate"", ""value"": {""ref"": ""S00000012""}}");
                                JObject o = new JObject();
                                o["method"] = "supplier_partner_validate";

                                JObject child = new JObject();
                                child["ref"] = model.UserName;
                                o["value"] = child;
                                UnovoParameters unovoParam = new UnovoParameters("unovo.interface", o.ToString());
                                var txt = await odooRpcClient.ExecuteIsExist(unovoParam);
                                JToken json = JToken.Parse(txt);
                                if (json["valid"].ToString() == "1")
                                {
                                    byte[] bts = Encoding.UTF8.GetBytes(model.UserName);
                                    string pCode = Convert.ToBase64String(bts);

                                    return RedirectToAction("CreatePartner", new { partnerCode = pCode });
                                    #region 新建用户
                                    ////新建用户
                                    //string email = json["data"]["partner"][0]["email"].Value<string>();
                                    //var newUser = new User { UserName = model.UserName, Email = email };
                                    //var identity = await _userManager.CreateAsync(newUser);
                                    ////if (await _userService.CreateUserAsync(user, "",//该方法不能创建空email，空密码的用户
                                    ////    (key, message) =>
                                    ////    ModelState.AddModelError(key, message))
                                    ////    )
                                    ////{

                                    ////}
                                    //if (identity.Succeeded)
                                    //{
                                    //    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
                                    //    // Send an email with this link
                                    //    //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                                    //    //var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = WebUtility.UrlEncode(code) }, protocol: HttpContext.Request.Scheme);
                                    //    //await _emailSender.SendEmailAsync(model.Email, "Confirm your account",
                                    //    //    $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>link</a>");
                                    //    var userGet = await _userManager.FindByNameAsync(newUser.UserName);
                                    //    PartnerInfo partner = new PartnerInfo();
                                    //    partner.UserId = userGet.Id;
                                    //    partner.PartnerCode = model.UserName;
                                    //    _session.Save(partner);
                                    //    //await _signInManager.SignInAsync(user, isPersistent: false);
                                    //    _logger.LogInformation(3, "User created a new account with password.");

                                    //    //发送修改密码邮件，加入安全码，防止个别供应商伪造看别的供应商
                                    //    // Send an email with this link
                                    //    var code = await _userManager.GeneratePasswordResetTokenAsync(userGet);
                                    //    var callbackUrl = Url.Action("ResetPassword", "Account",
                                    //        new { userId = userGet.Id, code = WebUtility.UrlEncode(code) }, protocol: HttpContext.Request.Scheme);
                                    //    //await _emailSender.SendEmailAsync(model.Email, "Reset Password",
                                    //    //   $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");

                                    //    #region MyRegion
                                    //    //var message = new MimeMessage();
                                    //    //message.From.Add(new MailboxAddress("上海联寓智能科技有限公司", "tzjiangfei@163.com"));
                                    //    //message.To.Add(new MailboxAddress("供应商", email));
                                    //    //message.Subject = "首次登录密码重置";

                                    //    //message.Body = new TextPart("html")
                                    //    //{
                                    //    //    Text = $"您好，由于这是您第一次登录系统，请点击此链接重置密码: <a href='{callbackUrl}'>重置密码</a>"
                                    //    //};

                                    //    //using (var client = new SmtpClient())
                                    //    //{
                                    //    //    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                                    //    //    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                                    //    //    client.Connect("smtp.163.com", 25, false);

                                    //    //    // Note: since we don't have an OAuth2 token, disable
                                    //    //    // the XOAUTH2 authentication mechanism.
                                    //    //    client.AuthenticationMechanisms.Remove("XOAUTH2");

                                    //    //    // Note: only needed if the SMTP server requires authentication
                                    //    //    client.Authenticate("tzjiangfei", "!#%zmtch&");

                                    //    //    client.Send(message);
                                    //    //    client.Disconnect(true);
                                    //    //} 
                                    //    #endregion
                                    //    bool success =await EmailHelper.SendEmail(email, "首次登录密码重置", $"您好，由于这是您第一次登录系统，请点击此链接重置密码: <a href='{callbackUrl}'>重置密码</a>");


                                    //    return RedirectToAction("ChangePassword"); 

                                    //}
                                    #endregion
                                }
                                else
                                {
                                    ModelState.AddModelError("UserName", "该用户不存在");
                                    //return View(model);
                                }
                            }
                            else
                            {
                                ModelState.AddModelError(string.Empty, "登录失败");
                                //return View(model);
                            }
                        }
                        else
                        {
                            if (!await _userManager.HasPasswordAsync(user))
                            {
                                ModelState.AddModelError(string.Empty, "首次登录请重置密码");
                            }

                            // This doesn't count login failures towards account lockout
                            // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                            var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
                            if (result.Succeeded)
                            {
                                _logger.LogInformation(1, "User logged in.");
                                return RedirectToLocal(returnUrl);
                            }
                            //if (result.RequiresTwoFactor)
                            //{
                            //    return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                            //}
                            //if (result.IsLockedOut)
                            //{
                            //    _logger.LogWarning(2, "User account locked out.");
                            //    return View("Lockout");
                            //}
                            else
                            {
                                //ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                                ModelState.AddModelError(string.Empty, "登录失败，请检查密码");
                            }
                        }
                    }
                }
                // If we got this far, something failed, redisplay form
                Random r = new Random();
                string vcode = r.Next(100000, 1000000).ToString();
                ViewBag.VerificationCode = vcode;
                HttpContext.Session.SetString("verificationcode", vcode);
                return View(model);

            }
            catch (Exception ex)
            {
                return Content(ex.Message);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            //HttpClient client = new HttpClient();

            //// Add a new Request Message
            //HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, "http://www.baidu.com");

            //// Add our custom headers
            //Dictionary<string, string> RequestHeader = new Dictionary<string, string>();
            //if (RequestHeader != null)
            //{
            //    foreach (var item in RequestHeader)
            //    {

            //        requestMessage.Headers.Add(item.Key, item.Value);

            //    }
            //}

            //// Add request body
            //requestMessage.Content = new StringContent("{\"name\":\"John Doe\",\"age\":33}", Encoding.UTF8, "application/json");

            //// Send the request to the server
            //HttpResponseMessage response = await client.SendAsync(requestMessage);

            //// Get the response
            //string responseString = await response.Content.ReadAsStringAsync();


            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new User { UserName = model.UserName, Email = model.Email };
                if (await _userService.CreateUserAsync(user, model.Password, (key, message) => ModelState.AddModelError(key, message)))
                {
                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
                    // Send an email with this link
                    //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    //var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = WebUtility.UrlEncode(code) }, protocol: HttpContext.Request.Scheme);
                    //await _emailSender.SendEmailAsync(model.Email, "Confirm your account",
                    //    $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>link</a>");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation(3, "User created a new account with password.");
                    return RedirectToLocal(returnUrl);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation(4, "User logged out.");

            return Redirect("~/");
        }

        //
        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                //if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                if (user == null)
                {
                    // Don't reveal that the user does not exist or is not confirmed.
                    ViewBag.Message = "抱歉，当前邮箱在我们ERP系统中没有对应用户，请联系我们！";
                    return View("ForgotPasswordConfirmation");
                }

                // Send an email with this link
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action(nameof(ResetPassword), "Account",
                    new { userId = user.Id, code = WebUtility.UrlEncode(code) }, protocol: HttpContext.Request.Scheme);
                //await _emailSender.SendEmailAsync(model.Email, "Reset Password",
                //   $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");

                //var message = new MimeMessage();
                //message.From.Add(new MailboxAddress("上海联寓智能科技有限公司", "tzjiangfei@163.com"));
                //message.To.Add(new MailboxAddress("供应商", model.Email));
                //message.Subject = "首次登录密码重置";

                //message.Body = new TextPart("html")
                //{
                //    Text = $"请点击此链接重置密码: <a href='{callbackUrl}'>重置密码</a>"
                //};

                //using (var client = new SmtpClient())
                //{
                //    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                //    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                //    client.Connect("smtp.163.com", 25, false);

                //    // Note: since we don't have an OAuth2 token, disable
                //    // the XOAUTH2 authentication mechanism.
                //    client.AuthenticationMechanisms.Remove("XOAUTH2");

                //    // Note: only needed if the SMTP server requires authentication
                //    client.Authenticate("tzjiangfei", "!#%zmtch&");

                //    client.Send(message);
                //    client.Disconnect(true);
                //}
                bool success = await EmailHelper.SendEmail(model.Email, "密码重置", $"请点击此链接重置密码: <a href='{callbackUrl}'>重置密码</a>");
                if (success)
                {
                    ViewBag.Message = "重置密码邮件已发送，请查看邮件！";
                }
                else
                {
                    ViewBag.Message = "重置密码邮件发送失败！";
                }
                return View("ForgotPasswordConfirmation");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string userId, string code = null)
        {
            try
            {
                bool b = false;
                ResetPasswordViewModel model = new ResetPasswordViewModel();
                code = WebUtility.UrlDecode(code);
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    b = await _userManager.VerifyUserTokenAsync(user, "Default", "ResetPassword", code);
                    model.Code = WebUtility.UrlEncode(code);
                    ViewBag.Name = user.UserName;

                    //用户不存在去erp判断用户是否存在
                    OdooRpcClient odooRpcClient = new OdooRpcClient(_odooConnection);
                    await odooRpcClient.Authenticate();
                    if (odooRpcClient.SessionInfo.IsLoggedIn)
                    {

                        JObject o = new JObject();
                        o["method"] = "supplier_partner_form_get";

                        JObject child = new JObject();
                        child["is_company"] = true;
                        child["supplier"] = true;
                        child["ref"] = user.UserName;
                        o["value"] = child;

                        string jsonTxt = o.ToString();


                        UnovoParameters unovoParam = new UnovoParameters("unovo.interface", jsonTxt);
                        var txt = await odooRpcClient.ExecuteIsExist(unovoParam);
                        JToken json = JToken.Parse(txt);
                        string exist = json["message"].ToString();
                        if (exist == "succ")
                        {
                            //int counts= json["data"]["counts"].Value<int>();
                            foreach (JToken token in json["data"]["partner"].Children())
                            {
                                ViewBag.CompanyName = token["name"].ToString();
                                break;
                            }
                        }
                    }
                }
                else
                {
                    ViewBag.Message = "用户不存在";
                }
                return b ? View(model) : View("Error");
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.Message;
                return View("Error");
            }
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByNameAsync(model.Name);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            model.Code = WebUtility.UrlDecode(model.Code);
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(user.UserName);
                string partnerCode = Convert.ToBase64String(bytes);

                return RedirectToAction(nameof(ResetPasswordConfirmation), new { partnerCode = partnerCode });
            }
            //AddErrors(result);
            foreach (var error in result.Errors)
            {
                if (error.Code == "InvalidToken")
                {
                    ModelState.AddModelError(string.Empty, "重置链接已过期，请再次请求重置密码邮件");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation(string partnerCode)
        {
            ViewBag.PartnerCode = partnerCode;
            return View();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userService.GetAuthenticatedUserAsync(User);

                if (await _userService.ChangePasswordAsync(user, model.CurrentPassword, model.Password, (key, message) =>
                {
                    if (key == "Password" && message == "Passwords must have at least one digit ('0'-'9').")
                    {
                        ModelState.AddModelError(key, "密码必须至少要有一个数字");
                    }
                    else if (key == "Password" && message == "Passwords must have at least one lowercase ('a'-'z').")
                    {
                        ModelState.AddModelError(key, "密码必须至少要有一个小写字母");
                    }
                    else if (key == "Password" && message == "Passwords must have at least one uppercase('A'-'Z').")
                    {
                        ModelState.AddModelError(key, "密码必须至少要有一个大写字母");
                    }
                    else if (key == "Password" && message == "Passwords must have at least one non letter or digit character.")
                    {
                        ModelState.AddModelError(key, "密码必须至少有一个非字母或数字字符");
                    }
                    else if (key == "Password" && message == "Passwords must be at least 6 characters.")
                    {
                        ModelState.AddModelError(key, "密码必须至少6位");
                    }
                    else if (key == "CurrentPassword" && message == "Incorrect password.")
                    {
                        ModelState.AddModelError(key, "当前密码不正确");
                    }
                    else if (key == "Password" && message == "PasswordMismatch")
                    {
                        ModelState.AddModelError(key, "当前密码不正确");
                    }
                    else
                    {
                        ModelState.AddModelError(key, message);
                    }

                }))
                {
                    return RedirectToLocal(Url.Action("ChangePasswordConfirmation"));
                }
            }
            else
            {

                if (ModelState["PasswordConfirmation"].Errors.Count > 0)
                {
                    ModelState["PasswordConfirmation"].Errors.Clear();
                    ModelState.AddModelError("PasswordConfirmation", "确认密码不匹配");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ChangePasswordConfirmation()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return Redirect("~/");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> CreatePartner(string partnerCode)
        {
            try
            {

                byte[] bytes = Convert.FromBase64String(partnerCode);
                string trueCode = Encoding.UTF8.GetString(bytes);
                //判断供应商是否存在
                var count = await _session.QueryAsync<PartnerInfo, PartnerInfoIndex>(item => item.PartnerCode == trueCode).Count();
                if (count > 0)
                {
                    return RedirectToAction("Login", new { partnerCode = partnerCode });
                }
                else
                {
                    //用户不存在去erp判断用户是否存在
                    OdooRpcClient odooRpcClient = new OdooRpcClient(_odooConnection);
                    await odooRpcClient.Authenticate();

                    if (odooRpcClient.SessionInfo.IsLoggedIn)
                    {
                        string param = "{\"method\": \"supplier_partner_validate\", \"value\": {\"ref\": \"" + trueCode + "\"}}";
                        UnovoParameters unovoParam = new UnovoParameters("unovo.interface", param);
                        var txt = await odooRpcClient.ExecuteIsExist(unovoParam);
                        JToken json = JToken.Parse(txt);
                        string exist = json["message"].ToString();
                        if (exist == "succ")
                        {
                            string email = json["data"]["partner"][0]["email"].Value<string>();
                            var user = new User { UserName = trueCode, Email = email };
                            var identity = await _userManager.CreateAsync(user);
                            //if (await _userService.CreateUserAsync(user, "",//该方法不能创建空email，空密码的用户
                            //    (key, message) =>
                            //    ModelState.AddModelError(key, message))
                            //    )
                            //{

                            //}
                            if (identity.Succeeded)
                            {
                                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
                                // Send an email with this link
                                //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                                //var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = WebUtility.UrlEncode(code) }, protocol: HttpContext.Request.Scheme);
                                //await _emailSender.SendEmailAsync(model.Email, "Confirm your account",
                                //    $"Please confirm your account by clicking this link: <a href='{callbackUrl}'>link</a>");
                                var userGet = await _userManager.FindByNameAsync(user.UserName);
                                PartnerInfo partner = new PartnerInfo();
                                partner.UserId = userGet.Id;
                                partner.PartnerCode = trueCode;
                                _session.Save(partner);
                                //await _signInManager.SignInAsync(user, isPersistent: false);
                                _logger.LogInformation(3, "User created a new account with password.");

                                //发送修改密码邮件，加入安全码，防止个别供应商伪造看别的供应商
                                // Send an email with this link
                                var code = await _userManager.GeneratePasswordResetTokenAsync(userGet);
                                var callbackUrl = Url.Action("ResetPassword", "Account",
                                    new { userId = userGet.Id, code = WebUtility.UrlEncode(code) }, protocol: HttpContext.Request.Scheme);
                                //await _emailSender.SendEmailAsync(model.Email, "Reset Password",
                                //   $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");

                                //var message = new MimeMessage();
                                //message.From.Add(new MailboxAddress("上海联寓智能科技有限公司", "tzjiangfei@163.com"));
                                //message.To.Add(new MailboxAddress("供应商", email));
                                //message.Subject = "首次登录密码重置";

                                //message.Body = new TextPart("html")
                                //{
                                //    Text = $"您好，由于这是您第一次登录系统，请点击此链接重置密码: <a href='{callbackUrl}'>link</a>"
                                //};

                                //using (var client = new SmtpClient())
                                //{
                                //    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                                //    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                                //    client.Connect("smtp.163.com", 25, false);

                                //    // Note: since we don't have an OAuth2 token, disable
                                //    // the XOAUTH2 authentication mechanism.
                                //    client.AuthenticationMechanisms.Remove("XOAUTH2");

                                //    // Note: only needed if the SMTP server requires authentication
                                //    client.Authenticate("tzjiangfei", "!#%zmtch&");

                                //    client.Send(message);
                                //    client.Disconnect(true);
                                //}

                                bool success = await EmailHelper.SendEmail(email, "密码重置", $"您好，由于这是您第一次登录系统，请点击此链接重置密码: <a href='{callbackUrl}'>重置密码</a>");


                                //return RedirectToAction("ChangePassword");
                                ViewBag.Message = $"由于这是您第一次登录系统，请查看{email}邮件,重置密码登录！";
                                return View("Error");
                            }
                        }
                        return Content("不存此供应商");
                    }
                    else
                    {
                        return Content("ERP Login failed");
                    }


                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return View();
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SyncPartners()
        {
            try
            {

                //用户不存在去erp判断用户是否存在
                OdooRpcClient odooRpcClient = new OdooRpcClient(_odooConnection);
                await odooRpcClient.Authenticate();
                if (odooRpcClient.SessionInfo.IsLoggedIn)
                {
                    //JArray array = new JArray();
                    //array.Add("Manual text");
                    //array.Add(new DateTime(2000, 5, 23));
                    int pageIndex = 0;
                    int pageSize = 80;

                    JObject o = new JObject();
                    o["method"] = "supplier_partner_list_get";

                    JObject child = new JObject();
                    child["is_company"] = true;
                    child["supplier"] = true;
                    child["page_no"] = pageIndex;
                    child["page_size"] = pageSize;
                    o["value"] = child;

                    string jsonTxt = o.ToString();

                    //string param = "{\"method\":\"supplier_partner_list_get\",\" value\":{\"is_company\": true,\"supplier\":true,\"page_no\": 0,\"page_size\": 80}}";
                    while (true)
                    {
                        UnovoParameters unovoParam = new UnovoParameters("unovo.interface", jsonTxt);
                        var txt = await odooRpcClient.ExecuteIsExist(unovoParam);
                        JToken json = JToken.Parse(txt);
                        string exist = json["message"].ToString();
                        if (exist == "succ")
                        {
                            //int counts= json["data"]["counts"].Value<int>();
                            foreach (JToken token in json["data"]["partner"].Children())
                            {

                                if (token["email"].ToString().ToLower() != "false")
                                {
                                    string partnerCode = token["ref"].ToString();
                                    string email = token["email"].Value<string>();

                                    //如果用户存在则判断邮箱是否改变，不存在就新增
                                    var userExist = await _userManager.FindByNameAsync(partnerCode);
                                    if (userExist != null)
                                    {
                                        if (userExist.Email != email)
                                        {
                                            await _userManager.SetEmailAsync(userExist, email);
                                        }
                                    }
                                    else
                                    {
                                        var user = new User { UserName = partnerCode, Email = email };
                                        var identity = await _userManager.CreateAsync(user);

                                        if (identity.Succeeded)
                                        {
                                            var userGet = await _userManager.FindByNameAsync(user.UserName);
                                            PartnerInfo partner = new PartnerInfo();
                                            partner.UserId = userGet.Id;
                                            partner.PartnerCode = email;
                                            _session.Save(partner);
                                            //await _signInManager.SignInAsync(user, isPersistent: false);
                                            _logger.LogInformation(3, "User created a new account with password.");
                                        }
                                    }

                                }
                            }
                            if (json["data"]["has_more"].ToString() == "1")
                            {
                                pageIndex++;
                                child["page_no"] = pageIndex;
                                jsonTxt = o.ToString();
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                return Content("Success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Content(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Info()
        {
            try
            {
                PartnerViewMode model = new PartnerViewMode();
                //用户不存在去erp判断用户是否存在
                OdooRpcClient odooRpcClient = new OdooRpcClient(_odooConnection);
                await odooRpcClient.Authenticate();
                if (odooRpcClient.SessionInfo.IsLoggedIn)
                {

                    JObject o = new JObject();
                    o["method"] = "supplier_partner_form_get";

                    JObject child = new JObject();
                    child["is_company"] = true;
                    child["supplier"] = true;
                    child["ref"] = User.Identity.Name;
                    o["value"] = child;

                    string jsonTxt = o.ToString();


                    UnovoParameters unovoParam = new UnovoParameters("unovo.interface", jsonTxt);
                    var txt = await odooRpcClient.ExecuteIsExist(unovoParam);
                    JToken json = JToken.Parse(txt);
                    string exist = json["message"].ToString();
                    if (exist == "succ")
                    {
                        //int counts= json["data"]["counts"].Value<int>();
                        foreach (JToken token in json["data"]["partner"].Children())
                        {
                            model.PartnerCode = token["ref"].ToString();
                            model.CompanyName = token["name"].ToString();
                            model.Address = token["street"].ToString();
                            break;
                        }
                    }


                    JObject t = new JObject();
                    t["method"] = "supplier_partner_list_get";

                    JObject tChild = new JObject();
                    tChild["is_company"] = false;
                    tChild["parent_id"] = User.Identity.Name;
                    t["value"] = tChild;

                    jsonTxt = t.ToString();

                    unovoParam = new UnovoParameters("unovo.interface", jsonTxt);
                    txt = await odooRpcClient.ExecuteIsExist(unovoParam);
                    json = JToken.Parse(txt);
                    exist = json["message"].ToString();
                    if (exist == "succ")
                    {
                        //int counts= json["data"]["counts"].Value<int>();
                        foreach (JToken token in json["data"]["partner"].Children())
                        {
                            PartnerContactUser u = new PartnerContactUser();
                            u.Name = token["name"].ToString();
                            u.Phone = token["mobile"].ToString();
                            u.Email = token["email"].ToString();
                            model.Contacts.Add(u);
                        }
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Content(ex.Message);
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
