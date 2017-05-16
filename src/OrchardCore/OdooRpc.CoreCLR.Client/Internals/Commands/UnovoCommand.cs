using System;
using System.Threading.Tasks;
using OdooRpc.CoreCLR.Client.Models;
using OdooRpc.CoreCLR.Client.Models.Parameters;
using JsonRpc.CoreCLR.Client.Interfaces;
using System.Dynamic;
using System.Collections.Generic;

namespace OdooRpc.CoreCLR.Client.Internals.Commands
{
    internal class UnovoCommand : OdooAbstractCommand
    {
        public UnovoCommand(IJsonRpcClient rpcClient)
            : base(rpcClient)
        {
        }

        public Task<string> ExecuteCount(OdooSessionInfo sessionInfo, UnovoParameters unovoParams)
        {
            return InvokeRpc<string>(sessionInfo, CreateUnovoRequest(sessionInfo, "test_args", unovoParams, null));
        }

        //public Task<T> Execute<T>(OdooSessionInfo sessionInfo, OdooSearchParameters searchParams, OdooPaginationParameters pagParams)
        //{
        //    return InvokeRpc<T>(sessionInfo, CreateSearchRequest(sessionInfo, "search", searchParams, null, pagParams));
        //}

        //public Task<T> Execute<T>(OdooSessionInfo sessionInfo, OdooSearchParameters searchParams, OdooFieldParameters fieldParams, OdooPaginationParameters pagParams)
        //{
        //    return InvokeRpc<T>(sessionInfo, CreateSearchRequest(sessionInfo, "search_read", searchParams, fieldParams, pagParams));
        //}

        public Task<string> ExecuteIsExist(OdooSessionInfo sessionInfo, UnovoParameters unovoParams)
        {
            return InvokeRpc<string>(sessionInfo, CreateUnovoRequest(sessionInfo, "erp_open_common_fnct", unovoParams, null));
        }

        private OdooRpcRequest CreateUnovoRequest(OdooSessionInfo sessionInfo, string method, UnovoParameters unovoParams, OdooFieldParameters fieldParams)
        {
            List<object> requestArgs = new List<object>(
                new object[]
                {
                    sessionInfo.Database,
                    sessionInfo.UserId,
                    sessionInfo.Password,
                    unovoParams.Model,
                    method,
                    //unovoParams.JsonFilter
                    new object[]
                    {
                        unovoParams.JsonFilter
                    }
                }
            );

            dynamic searchOptions = new ExpandoObject();
            bool useSearchOptions = false;
            if (fieldParams != null && fieldParams.Count > 0)
            {
                searchOptions.fields = fieldParams.ToArray();
                useSearchOptions = true;
            }

            if (useSearchOptions)
            {
                requestArgs.Add(searchOptions);
            }

            return new OdooRpcRequest()
            {
                service = "object",
                method = "execute_kw",
                args = requestArgs.ToArray(),
                context = sessionInfo.UserContext
            };
        }
    }
}

