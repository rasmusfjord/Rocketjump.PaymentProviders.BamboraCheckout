using System.Collections.Generic;

namespace Rocketjump.PaymentProviders.BamboraCheckout
{
    public class BamboraSettingsBase
    {
        public BamboraSettingsBase(IDictionary<string, string> settings)
        {
            ContinueUrl = settings["continueUrl"];
            CancelUrl = settings["cancelUrl"];
            ErrorUrl = settings["errorUrl"];
            TestMerchantNumber = settings["testMerchantNumber"];
            TestAccessKey = settings["testAccessKey"];
            TestSecretKey = settings["testSecretKey"];
            TestMd5Key = settings["testMd5Key"];
            LiveMerchantNumber = settings["liveMerchantNumber"];
            LiveAccessKey = settings["liveAccessKey"];
            LiveSecretKey = settings["liveSecretKey"];
            LiveMd5Key = settings["liveMd5Key"];
            Language = settings["language"];
            Capture = bool.Parse(settings["capture"]);
            TestMode = bool.Parse(settings["testMode"]);
            ExcludedPaymentMethods = settings["excludedPaymentMethods"];
            ExcludedPaymentGroups = settings["excludedPaymentGroups"];
            ExcludedPaymentTypes = settings["excludedPaymentTypes"];
        }

        public string ContinueUrl { get; set; }

        public string CancelUrl { get; set; }

        public string ErrorUrl { get; set; }

        public string TestMerchantNumber { get; set; }

        public string LiveMerchantNumber { get; set; }

        public string TestAccessKey { get; set; }

        public string LiveAccessKey { get; set; }

        public string TestSecretKey { get; set; }

        public string LiveSecretKey { get; set; }

        public string TestMd5Key { get; set; }

        public string LiveMd5Key { get; set; }
        public string Language { get; set; }

        public bool Capture { get; set; }

        public bool TestMode { get; set; }

        // Advanced settings

        public string ExcludedPaymentMethods { get; set; }

        public string ExcludedPaymentGroups { get; set; }

        public string ExcludedPaymentTypes { get; set; }
    }
}
