using System;
using System.Threading;
using System.Threading.Tasks;
using Orchard.BackgroundTasks;
using Orchard.Users.Services;
using Microsoft.AspNetCore.Identity;
using Castle.Core.Logging;
using Orchard.Users.Models;
using OdooRpc.CoreCLR.Client.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using OdooRpc.CoreCLR.Client;
using OdooRpc.CoreCLR.Client.Models.Parameters;
using Newtonsoft.Json.Linq;
using Orchard.MPUnovo.Models;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

namespace Orchard.MPUnovo.Services
{
    public class UnovoBackgroundTask : IBackgroundTask
    {
        private readonly IUserService _userService;
        private readonly SignInManager<User> _signInManager;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly string _externalCookieScheme;
        private readonly YesSql.ISession _session;
        private readonly UserManager<User> _userManager;
        private readonly OdooConnectionInfo _odooConnection;
        public UnovoBackgroundTask(
            IUserService userService,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            YesSql.ISession session,
            IOptions<IdentityCookieOptions> identityCookieOptions,
            ILogger<UnovoBackgroundTask> logger,
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

        /// <summary>
        /// 此后台任务当前不适用，目前定时任务指定为一分钟间隔
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        //public async Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        //{
        //    try
        //    {
        //        //用户不存在去erp判断用户是否存在
        //        OdooRpcClient odooRpcClient = new OdooRpcClient(_odooConnection);
        //        await odooRpcClient.Authenticate();

        //        if (odooRpcClient.SessionInfo.IsLoggedIn)
        //        {
        //            Dictionary<string, string> dic = new Dictionary<string, string>();
        //            dic.Add("method", "supplier_partner_list_get");

        //            JArray array = new JArray();
        //            array.Add("Manual text");
        //            array.Add(new DateTime(2000, 5, 23));

        //            JObject o = new JObject();
        //            o["method"] = "supplier_partner_list_get";

        //            JObject child = new JObject();
        //            child["is_company"] = true;
        //            child["supplier"] = true;
        //            child["page_no"] = 0;
        //            child["page_size"] = 80;
        //            o["value"] = child;

        //            string jsonTxt = o.ToString();
        //            // {
        //            //   "MyArray": [
        //            //     "Manual text",
        //            //     "2000-05-23T00:00:00"
        //            //   ]
        //            // }


        //            string param = "{\"method\":\"supplier_partner_list_get\",\" value\":{\"is_company\": true,\"supplier\":true,\"page_no\": 0,\"page_size\": 80}}";
        //            UnovoParameters unovoParam = new UnovoParameters("unovo.interface", param);
        //            var txt = await odooRpcClient.ExecuteIsExist(unovoParam);
        //            JToken json = JToken.Parse(txt);       
        //            string exist = json["message"].ToString();
        //            if (exist == "succ")
        //            {
        //                string email = json["data"]["partner"][0]["email"].Value<string>();
        //                //var user = new User { UserName = email, Email = email };
        //                //var identity = await _userManager.CreateAsync(user);

        //                //if (identity.Succeeded)
        //                //{

        //                //    var userGet = await _userManager.FindByNameAsync(user.UserName);
        //                //    PartnerInfo partner = new PartnerInfo();
        //                //    partner.UserId = userGet.Id;
        //                //    partner.PartnerCode = email;
        //                //    _session.Save(partner);
        //                //    //await _signInManager.SignInAsync(user, isPersistent: false);
        //                //    _logger.LogInformation(3, "User created a new account with password.");


        //                //}
        //            }


        //        }
        //    }
        //    catch
        //    {

        //    }
        //    //return Task.CompletedTask;
        //}

        public Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
