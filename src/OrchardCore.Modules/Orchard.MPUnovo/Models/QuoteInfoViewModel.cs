using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orchard.MPUnovo.Models
{
    public class QuoteInfoViewModel
    {
        public IList<QuoteInfo> Quotes { get; set; }
        public dynamic Pager { get; set; }
    }
}