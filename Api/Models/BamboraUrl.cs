using Newtonsoft.Json;

namespace Rocketjump.PaymentProviders.BamboraCheckout.Api.Models
{
    public class BamboraUrl
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
