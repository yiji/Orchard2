using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orchard.MPUnovo.Models
{
    public class QuoteInfo
    {
        /// <summary>
        /// 物料编码 产品名称 分割得到物料编码 产品名称
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
        public string inquiry_id { get; set; }
        /// <summary>
        /// 数据库
        /// </summary>
        public string DB { get; set; }
        /// <summary>
        /// 更改物料原因
        /// </summary>
        public string Reason { get; set; }

        private string materialCoding = "";
        /// <summary>
        /// 物料编码
        /// </summary>
        public string MaterialCoding
        {
            get
            {
                if (materialCoding == "" && product_id != "")
                {
                    return product_id.Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                }
                else
                {
                    return materialCoding;
                }
            }
            set
            {
                materialCoding = value;
            }
        }

        private string materialName = "";
        /// <summary>
        /// 物料名称
        /// </summary>
        public string MaterialName
        {
            get
            {
                if (materialName == "" && product_id != "")
                {
                    return product_id.Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries)[1].Trim();
                }
                else
                {
                    return materialName;
                }
            }
            set
            {
                materialName = value;
            }
        }
    }
}