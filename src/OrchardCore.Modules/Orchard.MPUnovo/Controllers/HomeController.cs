using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Display;
using Orchard.DeferredTasks;
using Orchard.MPUnovo.Models;
using Orchard.DisplayManagement;
using Orchard.Environment.Cache;
using Orchard.Events;
using OdooRpc.CoreCLR.Client.Models;
using OdooRpc.CoreCLR.Client;
using OdooRpc.CoreCLR.Client.Models.Parameters;
using Newtonsoft.Json;
using Orchard.Users.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using MimeKit;
using MailKit.Net.Smtp;
using Orchard.Users.Indexes;
using YesSql;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Orchard.Navigation;
using System.Linq;
using System.Net;
using System.Text;

namespace Orchard.MPUnovo.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IContentManager _contentManager;
        private readonly IEventBus _eventBus;
        private readonly ISession _session;
        private readonly ILogger _logger;
        private readonly ITagCache _tagCache;
        private readonly IContentItemDisplayManager _contentDisplay;
        private readonly IDeferredTaskEngine _deferredTaskEngine;
        private readonly UserManager<User> _userManager;
        private readonly OdooConnectionInfo _odooConnection;

        string[] dbs = new string[] { "db-lianyuplus", "db-unovo", "db-zhihui" };

        public HomeController(
            IContentManager contentManager,
            IEventBus eventBus,
            IShapeFactory shapeFactory,
            ISession session,
            ILogger<HomeController> logger,
            ITagCache tagCache,
            IContentItemDisplayManager contentDisplay,
            IDeferredTaskEngine processingQueue,
            UserManager<User> userManager,
            OdooConnectionInfo odooConnection)
        {
            _deferredTaskEngine = processingQueue;
            _session = session;
            _contentManager = contentManager;
            _eventBus = eventBus;
            Shape = shapeFactory;
            _logger = logger;
            _tagCache = tagCache;
            _contentDisplay = contentDisplay;
            _userManager = userManager;
            _odooConnection = odooConnection;
        }

        dynamic Shape { get; set; }

        public async Task<ActionResult> Index(PagerParameters pagerParameters)
        {

            try
            {
                List<QuoteInfo> lst = new List<QuoteInfo>();

                int total = 0;
                int pageSize = 50;

                for (int i = 0; i < 1; i++)
                {
                    //用户不存在去erp判断用户是否存在
                    OdooRpcClient odooRpcClient = new OdooRpcClient(_odooConnection);
                    await odooRpcClient.Authenticate();
                    if (odooRpcClient.SessionInfo.IsLoggedIn)
                    {
                        int pageIndex = 0;


                        //JArray array = new JArray();
                        //array.Add("Manual text");
                        //array.Add(new DateTime(2000, 5, 23));

                        JObject o = new JObject();
                        o["method"] = "supplier_quote_list_get";

                        JObject child = new JObject();
                        child["partner_id"] = User.Identity.Name;
                        child["state"] = "draft";
                        //child["state"] = "done";
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
                                total += Convert.ToInt32(json["total_counts"].ToString());

                                string dbName = json["dbname"].ToString();
                                int counts = json["data"]["counts"].Value<int>();
                                if (counts > 0)
                                {
                                    var tempList = JsonConvert.DeserializeObject<List<QuoteInfo>>(json["data"]["quote"].ToString());
                                    tempList.ForEach(item => item.DB = dbName);
                                    lst.AddRange(tempList);
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
                }
                var pager = new Pager(pagerParameters, pageSize);


                var pagerShape = Shape.Pager(pager).TotalItemCount(total);

                var model = new QuoteInfoViewModel
                {
                    Quotes = lst,
                    Pager = pagerShape
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Content(ex.Message);
            }
        }

        public async Task<ActionResult> Edit(string inquiryid, string prodid, string db)
        {
            try
            {
                QuoteInfo quo = null;
                _odooConnection.Database = db;
                //用户不存在去erp判断用户是否存在
                OdooRpcClient odooRpcClient = new OdooRpcClient(_odooConnection);
                await odooRpcClient.Authenticate();
                if (odooRpcClient.SessionInfo.IsLoggedIn)
                {
                    JObject o = new JObject();
                    o["method"] = "supplier_quote_form_get";

                    JObject child = new JObject();
                    child["partner_id"] = User.Identity.Name;
                    //child["state"] = "draft";
                    child["inquiry_id"] = inquiryid;
                    child["product_id"] = prodid;
                    o["value"] = child;

                    string jsonTxt = o.ToString();


                    UnovoParameters unovoParam = new UnovoParameters("unovo.interface", jsonTxt);
                    var txt = await odooRpcClient.ExecuteIsExist(unovoParam);
                    JToken json = JToken.Parse(txt);
                    string exist = json["message"].ToString();
                    if (exist == "succ")
                    {
                        quo = JsonConvert.DeserializeObject<QuoteInfo>(json["data"]["quote"][0].ToString());
                    }
                }
                if (quo != null)
                {
                    return View(quo);
                }
                return RedirectToAction("Index");
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        //public async Task<ActionResult> Edit([Bind("MaterialCoding, inquiry_id, price, spq,l_t")]QuoteInfo quote)//[Bind(Prefix = "HomeAddress", Exclude = "Country")]
        [HttpPost]
        public async Task<ActionResult> Edit(QuoteInfo quote, string db)//[Bind(Prefix = "HomeAddress", Exclude = "Country")]
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _odooConnection.Database = db;
                    //用户不存在去erp判断用户是否存在
                    OdooRpcClient odooRpcClient = new OdooRpcClient(_odooConnection);
                    await odooRpcClient.Authenticate();
                    if (odooRpcClient.SessionInfo.IsLoggedIn)
                    {

                        JObject o = new JObject();
                        o["method"] = "supplier_quote_update";

                        JObject child = new JObject();
                        child["partner_id"] = User.Identity.Name;
                        //child["state"] = "draft";
                        child["inquiry_id"] = quote.inquiry_id;
                        child["product_id"] = quote.MaterialCoding;
                        child["state"] = "draft";
                        child["price"] = quote.price;
                        child["spq"] = quote.spq;
                        child["l_t"] = quote.l_t;
                        o["value"] = child;

                        string jsonTxt = o.ToString();


                        UnovoParameters unovoParam = new UnovoParameters("unovo.interface", jsonTxt);
                        var txt = await odooRpcClient.ExecuteIsExist(unovoParam);
                        JToken json = JToken.Parse(txt);
                        string exist = json["message"].ToString();
                        if (exist == "succ")
                        {
                            return RedirectToAction("Quoted");
                        }
                    }

                    return RedirectToAction("Index");
                }
                else
                {
                    return View(quote);
                }
            }
            catch
            {
                return View(quote);
            }
        }

        /// <summary>
        /// 更改物料
        /// </summary>
        /// <param name="inquiryid"></param>
        /// <param name="prodid"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public async Task<ActionResult> Change(string inquiryid, string prodid, string db)
        {
            try
            {
                QuoteInfo quo = null;
                _odooConnection.Database = db;
                //用户不存在去erp判断用户是否存在
                OdooRpcClient odooRpcClient = new OdooRpcClient(_odooConnection);
                await odooRpcClient.Authenticate();
                if (odooRpcClient.SessionInfo.IsLoggedIn)
                {
                    JObject o = new JObject();
                    o["method"] = "supplier_quote_form_get";

                    JObject child = new JObject();
                    child["partner_id"] = User.Identity.Name;
                    //child["state"] = "draft";
                    child["inquiry_id"] = inquiryid;
                    child["product_id"] = prodid;
                    o["value"] = child;

                    string jsonTxt = o.ToString();


                    UnovoParameters unovoParam = new UnovoParameters("unovo.interface", jsonTxt);
                    var txt = await odooRpcClient.ExecuteIsExist(unovoParam);
                    JToken json = JToken.Parse(txt);
                    string exist = json["message"].ToString();
                    if (exist == "succ")
                    {
                        quo = JsonConvert.DeserializeObject<QuoteInfo>(json["data"]["quote"][0].ToString());
                    }
                }
                if (quo != null)
                {
                    ChangeInfoViewModel model = new ChangeInfoViewModel();
                    model.OldQuote = quo;
                    ReplaceQuote replaceQuote = new ReplaceQuote()
                    {
                        inquiry_id = quo.inquiry_id,
                        product_id = quo.MaterialCoding
                    };
                    model.NewQuote = replaceQuote;
                    return View(model);
                }
                return RedirectToAction("Index");
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<ActionResult> Change([Bind(Prefix = "NewQuote")]ReplaceQuote quote, string db)//[Bind(Prefix = "HomeAddress", Exclude = "Country")]
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _odooConnection.Database = db;
                    //用户不存在去erp判断用户是否存在
                    OdooRpcClient odooRpcClient = new OdooRpcClient(_odooConnection);
                    await odooRpcClient.Authenticate();
                    if (odooRpcClient.SessionInfo.IsLoggedIn)
                    {

                        JObject o = new JObject();
                        o["method"] = "supplier_quotechange_update";

                        JObject child = new JObject();
                        child["name"] = quote.name;
                        child["product_mfg_pn"] = quote.product_mfg_pn;
                        child["product_mfg_name"] = quote.product_mfg_name;
                        child["product_descript"] = quote.product_descript;
                        child["partner_id"] = User.Identity.Name;
                        child["inquiry_id"] = quote.inquiry_id;
                        child["product_id"] = quote.product_id;
                        child["price"] = quote.price;
                        child["spq"] = quote.spq;
                        child["l_t"] = quote.l_t;
                        child["reason"] = quote.Reason;
                        o["value"] = child;

                        string jsonTxt = o.ToString();

                        UnovoParameters unovoParam = new UnovoParameters("unovo.interface", jsonTxt);
                        var txt = await odooRpcClient.ExecuteIsExist(unovoParam);
                        JToken json = JToken.Parse(txt);
                        string exist = json["message"].ToString();
                        if (exist == "succ")
                        {
                            JObject obj = new JObject();
                            obj["method"] = "supplier_quote_update";


                            JObject objChild = new JObject();
                            objChild["partner_id"] = User.Identity.Name;
                            objChild["inquiry_id"] = quote.inquiry_id;
                            objChild["product_id"] = quote.product_id;

                            objChild["reason"] = quote.Reason;
                            obj["value"] = objChild;

                            jsonTxt = obj.ToString();


                            unovoParam = new UnovoParameters("unovo.interface", jsonTxt);
                            txt = await odooRpcClient.ExecuteIsExist(unovoParam);
                            json = JToken.Parse(txt);
                            exist = json["message"].ToString();
                            if (exist == "succ")
                            {
                                return RedirectToAction("Quoted");
                            }
                        }
                    }

                    return RedirectToAction("Index");
                }
                else
                {
                    return View(quote);
                }
            }
            catch
            {
                return View(quote);
            }
        }

        public async Task<ActionResult> ChangeDetail(string inquiryid, string prodid, string db)
        {
            try
            {
                QuoteInfo quo = null;
                ReplaceQuote replaceQuo = null;
                _odooConnection.Database = db;
                //用户不存在去erp判断用户是否存在
                OdooRpcClient odooRpcClient = new OdooRpcClient(_odooConnection);
                await odooRpcClient.Authenticate();
                if (odooRpcClient.SessionInfo.IsLoggedIn)
                {
                    JObject o = new JObject();
                    o["method"] = "supplier_quotechange_form_get";

                    JObject child = new JObject();
                    child["partner_id"] = User.Identity.Name;
                    child["inquiry_id"] = inquiryid;
                    child["product_id"] = prodid;
                    o["value"] = child;

                    string jsonTxt = o.ToString();


                    UnovoParameters unovoParam = new UnovoParameters("unovo.interface", jsonTxt);
                    var txt = await odooRpcClient.ExecuteIsExist(unovoParam);
                    JToken json = JToken.Parse(txt);
                    string exist = json["message"].ToString();
                    if (exist == "succ")
                    {
                        quo = JsonConvert.DeserializeObject<QuoteInfo>(json["data"]["quotechange"][0].ToString());
                        replaceQuo = JsonConvert.DeserializeObject<ReplaceQuote>(json["data"]["quotechange"][0].ToString());
                    }
                }
                if (quo != null&& replaceQuo!=null)
                {
                    ChangeInfoViewModel model = new ChangeInfoViewModel()
                    {
                        OldQuote = quo,
                        NewQuote = replaceQuo
                    };
                    return View(model);
                }
                return RedirectToAction("Index");
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 已报价
        /// </summary>
        /// <param name="pagerParameters"></param>
        /// <returns></returns>
        public async Task<ActionResult> Quoted(PagerParameters pagerParameters)
        {
            try
            {
                List<QuoteInfo> lst = new List<QuoteInfo>();

                int total = 0;
                int pageSize = 50;

                for (int i = 0; i < 1; i++)
                {
                    //用户不存在去erp判断用户是否存在
                    OdooRpcClient odooRpcClient = new OdooRpcClient(_odooConnection);
                    await odooRpcClient.Authenticate();
                    if (odooRpcClient.SessionInfo.IsLoggedIn)
                    {
                        int pageIndex = 0;


                        //JArray array = new JArray();
                        //array.Add("Manual text");
                        //array.Add(new DateTime(2000, 5, 23));

                        JObject o = new JObject();
                        o["method"] = "supplier_quote_list_get";

                        JObject child = new JObject();
                        child["partner_id"] = User.Identity.Name;
                        child["state"] = "done";
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
                                total += Convert.ToInt32(json["total_counts"].ToString());

                                string dbName = json["dbname"].ToString();
                                int counts = json["data"]["counts"].Value<int>();
                                if (counts > 0)
                                {
                                    var tempList = JsonConvert.DeserializeObject<List<QuoteInfo>>(json["data"]["quote"].ToString());
                                    tempList.ForEach(item => item.DB = dbName);
                                    lst.AddRange(tempList);
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
                }
                var pager = new Pager(pagerParameters, pageSize);


                var pagerShape = Shape.Pager(pager).TotalItemCount(total);

                var model = new QuoteInfoViewModel
                {
                    Quotes = lst,
                    Pager = pagerShape
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Content(ex.Message);
            }
        }

        public async Task<ActionResult> Detail(string inquiryid, string prodid, string db)
        {
            try
            {
                QuoteInfo quo = null;
                _odooConnection.Database = db;
                //用户不存在去erp判断用户是否存在
                OdooRpcClient odooRpcClient = new OdooRpcClient(_odooConnection);
                await odooRpcClient.Authenticate();
                if (odooRpcClient.SessionInfo.IsLoggedIn)
                {
                    JObject o = new JObject();
                    o["method"] = "supplier_quote_form_get";

                    JObject child = new JObject();
                    child["partner_id"] = User.Identity.Name;
                    child["inquiry_id"] = inquiryid;
                    child["product_id"] = prodid;
                    o["value"] = child;

                    string jsonTxt = o.ToString();


                    UnovoParameters unovoParam = new UnovoParameters("unovo.interface", jsonTxt);
                    var txt = await odooRpcClient.ExecuteIsExist(unovoParam);
                    JToken json = JToken.Parse(txt);
                    string exist = json["message"].ToString();
                    if (exist == "succ")
                    {
                        quo = JsonConvert.DeserializeObject<QuoteInfo>(json["data"]["quote"][0].ToString());
                    }
                }
                if (quo != null)
                {
                    return View(quo);
                }
                return RedirectToAction("Index");
            }
            catch
            {
                return RedirectToAction("Index");
            }
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
                    return View("ForgotPasswordConfirmation");
                }

                // Send an email with this link
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action(nameof(ResetPassword), "Home",
                    new { userId = user.Id, code = WebUtility.UrlEncode(code) }, protocol: HttpContext.Request.Scheme);
                //await _emailSender.SendEmailAsync(model.Email, "Reset Password",
                //   $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("jiang", "tzjiangfei@163.com"));
                message.To.Add(new MailboxAddress("Mrs. Chanandler Bong", "283659040@qq.com"));
                message.Subject = "How you doin'?";

                message.Body = new TextPart("html")
                {
                    Text = $"请点击此链接重置密码: <a href='{callbackUrl}'>link</a>"
                };

                using (var client = new SmtpClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    client.Connect("smtp.163.com", 25, false);

                    // Note: since we don't have an OAuth2 token, disable
                    // the XOAUTH2 authentication mechanism.
                    client.AuthenticationMechanisms.Remove("XOAUTH2");

                    // Note: only needed if the SMTP server requires authentication
                    client.Authenticate("tzjiangfei", "!#%zmtch&");

                    client.Send(message);
                    client.Disconnect(true);
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
            var user = await _userManager.FindByIdAsync(userId);
            bool b = await _userManager.VerifyUserTokenAsync(user, "Default", "ResetPassword", code);
            return b ? View("Error") : View();
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
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        public ActionResult Tag()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Tag(string tag)
        {
            _tagCache.RemoveTag(tag);
            return RedirectToAction("Tag", "Home", new { area = "Orchard.Demo" });
        }

        public async Task<ActionResult> Display(string contentItemId)
        {
            var contentItem = await _contentManager.GetAsync(contentItemId);

            if (contentItem == null)
            {
                return NotFound();
            }

            return View("Display", contentItem);
        }


        public ActionResult Raw()
        {
            return View();
        }

        public ActionResult Cache()
        {
            return View();
        }

        public string GCCollect()
        {
            GC.Collect();
            return "OK";
        }

        public ActionResult IndexError()
        {
            throw new Exception("ERROR!!!!");
        }

        public string CreateTask()
        {
            _deferredTaskEngine.AddTask(context =>
            {
                var logger = context.ServiceProvider.GetService<ILogger<HomeController>>();
                logger.LogError("Task deferred successfully");
                return Task.CompletedTask;
            });

            return "Check for logs";
        }

        public IActionResult ShapePerformance()
        {
            return View();
        }

        private OdooConnectionInfo GetConn(int i)
        {

            _odooConnection.Database = dbs[i];

            return _odooConnection;
        }
    }
}