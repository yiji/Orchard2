using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Orchard.MPUnovo.Models
{
    public class ChangeInfoViewModel
    {
        public QuoteInfo OldQuote { get; set; }
        public ReplaceQuote NewQuote { get; set; }
    }

    public class ReplaceQuote
    {
        /// <summary>
        /// 物料名称
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "物料名称必填", MinimumLength = 1)]
        public string name { get; set; }

        /// <summary>
        /// 物料型号
        /// </summary>
        public string product_mfg_pn { get; set; }
        /// <summary>
        /// 品牌
        /// </summary>
        public string product_mfg_name { get; set; }

        public string product_descript { get; set; }
        /// <summary>
        /// 老的物料编码
        /// </summary>
        public string product_id { get; set; } = "";
        /// <summary>
        /// 交货周数
        /// </summary>
        public int l_t { get; set; }
        /// <summary>
        /// 起订量
        /// </summary>
        public decimal spq { get; set; }
        public decimal price { get; set; }
        public string partner_id { get; set; }
        /// <summary>
        /// 老的询价单号
        /// </summary>
        public string inquiry_id { get; set; }
        /// <summary>
        /// 数据库
        /// </summary>
        public string DB { get; set; }
        /// <summary>
        /// 物料变更原因
        /// </summary>
        public string Reason { get; set; }

        public IList<SelectListItem> AvailableReasons
        {
            get
            {
                List<SelectListItem> lst = new List<SelectListItem>();
                lst.Add(new SelectListItem { Text = "产品变更通知", Value = "pcn" });
                lst.Add(new SelectListItem { Text = "产品终止生产", Value = "eol" });
                lst.Add(new SelectListItem { Text = "设计更改通知", Value = "dcn" });
                return lst;
            }
        }

    }
}