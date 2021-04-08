using Newtonsoft.Json;

namespace Rocketjump.PaymentProviders.BamboraCheckout.Api.Models
{
    public class BamboraResponseMetaData
    {
        [JsonProperty("result")]
        public bool Result { get; set; }

        [JsonProperty("message")]
        public BamboraMessage Message { get; set; }
    }
}
