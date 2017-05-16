using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orchard.MPUnovo.Models
{
    public class PartnerViewMode
    {
        public string PartnerCode { get; set; }
        public string CompanyName { get; set; }
        public string Address { get; set; }
        public List<PartnerContactUser> Contacts { get; set; } = new List<PartnerContactUser>();
    }

    public class PartnerContactUser
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
    }

}
