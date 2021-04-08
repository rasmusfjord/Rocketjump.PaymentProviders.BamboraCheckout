using Newtonsoft.Json;

namespace Rocketjump.PaymentProviders.BamboraCheckout.Api.Models
{
    public class BamboraAvailableAmounts
    {
        [JsonProperty("capture")]
        public int Capture { get; set; }

        [JsonProperty("credit")]
        public int Credit { get; set; }
    }
}
