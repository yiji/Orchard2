using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Senparc.Weixin.QY;
using Senparc.Weixin.QY.Entities;
using Senparc.Weixin.XmlUtility;
using Tencent;

namespace Orchard.MPUnovo.Api
{
    public class ApiController : Controller
    {
        /// <summary>
        /// 在网站没有提供Token（或传入为null）的情况下的默认Token，建议在网站中进行配置。
        /// </summary>
        public const string Token = "EB9E8Ul7a";
        /// <summary>
        /// 在网站没有提供EncodingAESKey（或传入为null）的情况下的默认Token，建议在网站中进行配置。
        /// </summary>
        public const string EncodingAESKey = "paypdi598MOWw1qPnqgyVMs7oPMlPFgxESwnwTkrkfR";
        /// <summary>
        /// 在网站没有提供CorpId（或传入为null）的情况下的默认Token，建议在网站中进行配置。
        /// </summary>
        public const string CorpId = "wxbbb74b7ae58de87d";

        public ApiController()
        {

        }

        [HttpGet]
        public IActionResult Index(PostModel postModel, string echostr)
        {
            return Content(Signature.VerifyURL(Token, EncodingAESKey, CorpId, postModel.Msg_Signature, postModel.Timestamp, postModel.Nonce, echostr));

        }

        [ActionName("Index")]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult PostMsg(PostModel postModel)
        {
            try
            {
                MemoryStream stream = new MemoryStream();
                Request.Body.CopyToAsync(stream);
                // convert stream to string  
                //stream.Position = 0;
                //StreamReader reader = new StreamReader(stream);
                //string text = reader.ReadToEnd();
                //var messageHandler = new MyQYMessageHandller<MessageContext<IRequestMessageBase, IResponseMessageBase>>(stream, postModel);
                //messageHandler.Execute();//执行微信处理过程
                //return Content(messageHandler.ResponseDocument.ToString());
                //return Content(text);

                var postDataDocument = XmlUtility.Convert(stream);
                IEnumerable<XElement> lst = postDataDocument.Root.Elements();
                XElement MsgSignature = postDataDocument.Root.Element("ToUserName");
                XElement TimeStamp = postDataDocument.Root.Element("AgentID");
                XElement Encrypt = postDataDocument.Root.Element("Encrypt");

                WXBizMsgCrypt msgCrype = new WXBizMsgCrypt(Token, EncodingAESKey, CorpId);
                string msgXml = null;
                var result = msgCrype.DecryptMsg(postModel.Msg_Signature, postModel.Timestamp, postModel.Nonce, postDataDocument.ToString(), ref msgXml);
                var decryptDoc = XDocument.Parse(msgXml);//完成解密
                return Content(decryptDoc.ToString());
            }
            catch
            {
                return Content("");
            }
        }
    }
}
