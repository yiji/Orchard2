using Orchard.MPUnovo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Indexes;

namespace Orchard.MPUnovo.Indexes
{
    public class PartnerInfoIndex : MapIndex
    {
        public int UserId { get; set; }
        public string PartnerCode { get; set; }
    }
    public class PartnerInfoIndexProvider : IndexProvider<PartnerInfo>
    {
        public override void Describe(DescribeContext<PartnerInfo> context)
        {
            context.For<PartnerInfoIndex>()
                .Map(partner =>
                {
                    return new PartnerInfoIndex
                    {
                        UserId = partner.UserId,
                        PartnerCode = partner.PartnerCode
                    };
                });
        }
    }
}
