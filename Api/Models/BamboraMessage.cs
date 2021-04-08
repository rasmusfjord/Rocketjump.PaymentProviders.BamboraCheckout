using Newtonsoft.Json;

namespace Rocketjump.PaymentProviders.BamboraCheckout.Api.Models
{
    public class BamboraMessage
    {
        [JsonProperty("enduser")]
        public string EndUser { get; set; }

        [JsonProperty("merchant")]
        public string Merchant { get; set; }
    }
}
