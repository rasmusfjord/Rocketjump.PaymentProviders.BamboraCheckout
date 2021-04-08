using Newtonsoft.Json;

namespace Rocketjump.PaymentProviders.BamboraCheckout.Api.Models
{
    public class BamboraResponse
    {
        [JsonProperty("meta")]
        public BamboraResponseMetaData Meta { get; set; }
    }
}
