using System.Collections.Generic;

namespace OdooRpc.CoreCLR.Client.Models.Parameters
{
    public class UnovoParameters
    {
        public string Model { get; private set; }
        public OdooDomainFilter DomainFilter { get; private set; }
        public string JsonFilter { get; private set; }

        public UnovoParameters(string model)
            : this(model, new OdooDomainFilter())
        {
        }

        public UnovoParameters(string model, OdooDomainFilter domainFilter)
        {
            this.Model = model;
            this.DomainFilter = domainFilter;
        }

        public UnovoParameters(string model, string jsonFilter)
        {
            this.Model = model;
            this.JsonFilter = jsonFilter;
        }
    }
}