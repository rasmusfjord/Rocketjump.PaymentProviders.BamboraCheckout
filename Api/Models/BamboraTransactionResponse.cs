using Newtonsoft.Json;

namespace Rocketjump.PaymentProviders.BamboraCheckout.Api.Models
{
    public class BamboraTransactionResponse : BamboraResponse
    {
        [JsonProperty("transaction")]
        public BamboraTransaction Transaction { get; set; }
    }
}
