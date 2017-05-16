using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Orchard.MPUnovo.Components
{
    public class EmailHelper
    {
        public static async Task<bool> SendEmail(string toEmail, string subject, string content)
        {
            try
            {
                HttpClient client = new HttpClient();

                // Add a new Request Message
                toEmail= System.Net.WebUtility.UrlEncode(toEmail);
                subject = System.Net.WebUtility.UrlEncode(subject);
                content = System.Net.WebUtility.UrlEncode(content);

                string url = $"http://www.lianyuplus.com//DesktopModules/Modules-Unovo/Api/Picture/sendemail?email={toEmail}&subject={subject}&content={content}";
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

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

                // Send the request to the server
                HttpResponseMessage response = await client.SendAsync(requestMessage);

                // Get the response
                string responseString = await response.Content.ReadAsStringAsync();
                if (responseString.IndexOf("success") > -1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
