using Newtonsoft.Json;

namespace Rocketjump.PaymentProviders.BamboraCheckout.Api.Models
{
    public class BamboraCustomer
    {
        [JsonProperty("email")]
        public string Email { get; set; }
    }
}
