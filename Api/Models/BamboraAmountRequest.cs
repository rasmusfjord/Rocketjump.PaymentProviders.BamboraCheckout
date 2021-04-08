using Newtonsoft.Json;

namespace Rocketjump.PaymentProviders.BamboraCheckout.Api.Models
{
    public class BamboraAmountRequest
    {
        [JsonProperty("amount")]
        public int Amount { get; set; }
    }
}
