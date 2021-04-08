using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Rocketjump.PaymentProviders.BamboraCheckout.Api;
using Rocketjump.PaymentProviders.BamboraCheckout.Api.Models;
using TeaCommerce.Api.Common;
using TeaCommerce.Api.Infrastructure.Logging;
using TeaCommerce.Api.Models;
using TeaCommerce.Api.Services;
using TeaCommerce.Api.Web.PaymentProviders;

namespace Rocketjump.PaymentProviders.BamboraCheckout
{
    // https://developer.bambora.com/europe/checkout/getting-started/checkout-settings#filter-payment-methods

    [PaymentProvider("Bambora Checkout")]
    public class BamboraCheckout : APaymentProvider
    {
        public override bool SupportsRetrievalOfPaymentStatus
        {
            get { return true; }
        }

        public override bool SupportsCapturingOfPayment
        {
            get { return true; }
        }

        public override bool SupportsRefundOfPayment
        {
            get { return true; }
        }

        public override bool SupportsCancellationOfPayment
        {
            get { return true; }
        }

        public override IDictionary<string, string> DefaultSettings
        {
            get
            {
                Dictionary<string, string> defaultSettings = new Dictionary<string, string>();
                defaultSettings["continueUrl"] =
                    ""; //The URL to continue to after this provider has done processing. eg: /continue/
                defaultSettings["cancelUrl"] =
                    ""; //The URL to return to if the payment attempt is canceled. eg: /cancel/
                defaultSettings["errorUrl"] = ""; //The URL to return to if the payment attempt errors. eg: /error/
                defaultSettings["testMerchantNumber"] = ""; //Your Bambora Merchant Number for test transactions.
                defaultSettings["testAccessKey"] = ""; //The test API Access Key obtained from the Bambora portal.
                defaultSettings["testSecretKey"] = ""; //The test API Secret Key obtained from the Bambora portal.
                defaultSettings["testMd5Key"] = ""; //The test MD5 hashing key obtained from the Bambora portal.
                defaultSettings["liveMerchantNumber"] = ""; //Your Bambora Merchant Number for live transactions.
                defaultSettings["liveAccessKey"] = ""; //The live API Access Key obtained from the Bambora portal.
                defaultSettings["liveSecretKey"] = ""; //The live API Secret Key obtained from the Bambora portal.
                defaultSettings["liveMd5Key"] = ""; //The live MD5 hashing key obtained from the Bambora portal.
                defaultSettings["language"] =
                    ""; //Set the language to use for the payment portal. Can be 'en-GB', 'da-DK', 'sv-SE' or 'nb-NO'.
                defaultSettings["capture"] =
                    "0"; //"Flag indicating whether to immediately capture the payment, or whether to just authorize the payment for later (manual) capture."
                defaultSettings["testMode"] = "0"; //"Set whether to process payments in test mode."
                defaultSettings["excludedPaymentMethods"] =
                    ""; //"Comma separated list of Payment Method IDs to exclude."
                defaultSettings["excludedPaymentGroups"] = ""; //"Comma separated list of Payment Group IDs to exclude."
                defaultSettings["excludedPaymentTypes"] = "0"; //"Comma separated list of Payment Type IDs to exclude."
                return defaultSettings;
            }
        }

        public override PaymentHtmlForm GenerateHtmlForm(Order order, string teaCommerceContinueUrl,
            string teaCommerceCancelUrl,
            string teaCommerceCallBackUrl, string teaCommerceCommunicationUrl, IDictionary<string, string> settings)
        {
            //currency
            Currency currency = CurrencyService.Instance.Get(order.StoreId, order.CurrencyId);
            if (!Iso4217CurrencyCodes.ContainsKey(currency.IsoCode))
            {
                throw new Exception("You must specify an ISO 4217 currency code for the " + currency.Name +
                                    " currency");
            }

            var currencyCode = currency.IsoCode.ToUpperInvariant();
            var amount =
                Convert.ToInt32(order.TotalPrice.Value.WithVat *
                                100M); //(order.TotalPrice.Value.WithVat * 100M);//.ToString("0", CultureInfo.InvariantCulture);

            var bamboraSettings = new BamboraSettingsBase(settings);

            var clientConfig = GetBamboraClientConfig(bamboraSettings);
            var client = new BamboraClient(clientConfig);


            var checkoutSessionRequest = new BamboraCreateCheckoutSessionRequest
            {
                InstantCaptureAmount = bamboraSettings.Capture ? amount : 0,
                Customer = new BamboraCustomer
                {
                    Email = order.PaymentInformation.Email
                },
                Order = new BamboraOrder
                {
                    Id = BamboraSafeOrderId(order.CartNumber),
                    Amount = amount,
                    Currency = currencyCode
                },
                Urls = new BamboraUrls
                {
                    Accept = teaCommerceContinueUrl,
                    Cancel = teaCommerceCancelUrl,
                    Callbacks = new[]
                    {
                        new BamboraUrl {Url = teaCommerceCallBackUrl}
                    }
                },
                PaymentWindow = new BamboraPaymentWindow
                {
                    Id = 1,
                    Language = bamboraSettings.Language
                }
            };

            // Exclude payment methods
            if (!string.IsNullOrWhiteSpace(bamboraSettings.ExcludedPaymentMethods))
            {
                checkoutSessionRequest.PaymentWindow.PaymentMethods = bamboraSettings.ExcludedPaymentMethods
                    .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => new BamboraPaymentFilter
                    { Id = x.Trim(), Action = BamboraPaymentFilter.Actions.Exclude })
                    .ToArray();
            }

            // Exclude payment groups
            if (!string.IsNullOrWhiteSpace(bamboraSettings.ExcludedPaymentGroups))
            {
                checkoutSessionRequest.PaymentWindow.PaymentGroups = bamboraSettings.ExcludedPaymentGroups
                    .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => new BamboraPaymentFilter
                    { Id = x.Trim(), Action = BamboraPaymentFilter.Actions.Exclude })
                    .ToArray();
            }

            // Exclude payment types
            if (!string.IsNullOrWhiteSpace(bamboraSettings.ExcludedPaymentTypes))
            {
                checkoutSessionRequest.PaymentWindow.PaymentTypes = bamboraSettings.ExcludedPaymentTypes
                    .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => new BamboraPaymentFilter
                    { Id = x.Trim(), Action = BamboraPaymentFilter.Actions.Exclude })
                    .ToArray();
            }

            var checkoutSession = client.CreateCheckoutSession(checkoutSessionRequest);

            if (checkoutSession.Meta.Result)
                return new PaymentHtmlForm()
                {
                    Action = checkoutSession.Url,
                    Method = HtmlFormMethodAttribute.Get
                };

            LoggingService.Instance.Warn<BamboraCheckout>("BamboraCheckout GenerateHtmlForm, " +
                                                          checkoutSession.Meta.Message.Merchant);
            throw new ApplicationException(checkoutSession.Meta.Message.EndUser);

        }


        public override CallbackInfo ProcessCallback(Order order, HttpRequest request,
            IDictionary<string, string> settings)
        {
            CallbackInfo callbackInfo = null;
            try
            {
                var bamboraSettings = new BamboraSettingsBase(settings);

                var clientConfig = GetBamboraClientConfig(bamboraSettings);
                var client = new BamboraClient(clientConfig);

                if (client.ValidateRequest(request))
                {
                    var txnId = request.QueryString["txnid"];
                    var orderId = request.QueryString["orderid"];
                    var amount = int.Parse("0" + request.QueryString["amount"]);
                    var txnFee = int.Parse("0" + request.QueryString["txnfee"]);
                    var paymenttype = request.QueryString["paymenttype"];
                    string cardnopostfix = request.QueryString["cardno"];
                    // Validate params
                    if (!string.IsNullOrWhiteSpace(txnId)
                        && !string.IsNullOrWhiteSpace(orderId)
                        && orderId == BamboraSafeOrderId(order.CartNumber)
                        && amount > 0)
                    {
                        // Fetch the transaction details so that we can work out
                        // the status of the transaction as the querystring params
                        // are not enough on their own
                        var transactionResp = client.GetTransaction(txnId);
                        if (transactionResp.Meta.Result)
                        {
                            callbackInfo = new CallbackInfo(
                                AmountFromMinorUnits(amount + txnFee),
                                transactionResp.Transaction.Id,
                                !bamboraSettings.Capture ? PaymentState.Authorized : PaymentState.Captured,
                                paymenttype,
                                cardnopostfix
                            );

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error<BamboraCheckout>(
                    "BamboraCheckout(" + order.CartNumber + ") - Process callback", ex);
                throw ex;
            }

            return callbackInfo;
        }

        public override ApiInfo GetStatus(Order order, IDictionary<string, string> settings)
        {
            ApiInfo apiInfo = null;

            try
            {
                var bamboraSettings = new BamboraSettingsBase(settings);

                var clientConfig = GetBamboraClientConfig(bamboraSettings);
                var client = new BamboraClient(clientConfig);

                var transactionResp = client.GetTransaction(order.TransactionInformation.TransactionId);
                if (transactionResp.Meta.Result)
                {
                    apiInfo = new ApiInfo(
                        transactionResp.Transaction.Id.ToString(CultureInfo.InvariantCulture),
                        GetPaymentStatus(transactionResp.Transaction));

                }

            }
            catch (Exception exp)
            {
                LoggingService.Instance.Error<BamboraCheckout>("Bambora - FetchPaymentStatus", exp);
            }

            return apiInfo;
        }

        public override ApiInfo CapturePayment(Order order, IDictionary<string, string> settings)
        {
            ApiInfo apiInfo = null;

            try
            {
                var bamboraSettings = new BamboraSettingsBase(settings);

                var clientConfig = GetBamboraClientConfig(bamboraSettings);
                var client = new BamboraClient(clientConfig);

                var transactionResp = client.CaptureTransaction(order.TransactionInformation.TransactionId, new BamboraAmountRequest
                {
                    Amount = (int)AmountToMinorUnits(order.TransactionInformation.AmountAuthorized.Value)
                });

                if (transactionResp.Meta.Result)
                {
                    apiInfo = new ApiInfo(
                        order.TransactionInformation.TransactionId,
                        PaymentState.Captured);

                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error<BamboraCheckout>("Bambora - CapturePayment", ex);

            }

            return apiInfo;


        }


        public override ApiInfo RefundPayment(Order order, IDictionary<string, string> settings)
        {
            ApiInfo apiInfo = null;

            try
            {
                var bamboraSettings = new BamboraSettingsBase(settings);

                var clientConfig = GetBamboraClientConfig(bamboraSettings);
                var client = new BamboraClient(clientConfig);

                var transactionResp = client.CreditTransaction(order.TransactionInformation.TransactionId, new BamboraAmountRequest
                {
                    Amount = (int)AmountToMinorUnits(order.TransactionInformation.AmountAuthorized.Value)
                });
                if (transactionResp.Meta.Result)
                {
                    apiInfo = new ApiInfo(
                        order.TransactionInformation.TransactionId,
                        PaymentState.Refunded);

                }

            }
            catch (Exception exp)
            {
                LoggingService.Instance.Error<BamboraCheckout>("Bambora - RefundPayment", exp);

            }

            return apiInfo;
        }

        public override ApiInfo CancelPayment(Order order, IDictionary<string, string> settings)
        {
            ApiInfo apiInfo = null;

            try
            {
                var bamboraSettings = new BamboraSettingsBase(settings);

                var clientConfig = GetBamboraClientConfig(bamboraSettings);
                var client = new BamboraClient(clientConfig);

                var transactionResp = client.DeleteTransaction(order.TransactionInformation.TransactionId);
                if (transactionResp.Meta.Result)
                {
                    apiInfo = new ApiInfo(
                        order.TransactionInformation.TransactionId,
                        PaymentState.Cancelled);

                }

            }
            catch (Exception exp)
            {
                LoggingService.Instance.Error<BamboraCheckout>("Bambora - CancelPayment", exp);

            }

            return apiInfo;
        }

        /**/

        public override string GetContinueUrl(Order order, IDictionary<string, string> settings)
        {
            settings.MustNotBeNull("settings");
            settings.MustContainKey("continueUrl", "settings");

            return settings["continueUrl"];
        }

        public override string GetCancelUrl(Order order, IDictionary<string, string> settings)
        {
            settings.MustNotBeNull("settings");
            settings.MustContainKey("cancelUrl", "settings");

            return settings["cancelUrl"];
        }

        protected BamboraClientConfig GetBamboraClientConfig(BamboraSettingsBase settings)
        {
            BamboraClientConfig config;

            if (settings.TestMode)
            {
                config = new BamboraClientConfig
                {
                    AccessKey = settings.TestAccessKey,
                    MerchantNumber = settings.TestMerchantNumber,
                    SecretKey = settings.TestSecretKey,
                    MD5Key = settings.TestMd5Key
                };
            }
            else
            {
                config = new BamboraClientConfig
                {
                    AccessKey = settings.LiveAccessKey,
                    MerchantNumber = settings.LiveMerchantNumber,
                    SecretKey = settings.LiveSecretKey,
                    MD5Key = settings.LiveMd5Key
                };
            }

            var apiKey = GenerateApiKey(config.AccessKey, config.MerchantNumber, config.SecretKey);

            config.Authorization = "Basic " + apiKey;

            return config;
        }

        protected string BamboraSafeOrderId(string orderId)
        {
            return Regex.Replace(orderId, "[^a-zA-Z0-9]", "");
        }

        private string GenerateApiKey(string accessToken, string merchantNumber, string secretToken)
        {
            var unencodedApiKey = $"{accessToken}@{merchantNumber}:{secretToken}";
            var unencodedApiKeyAsBytes = Encoding.UTF8.GetBytes(unencodedApiKey);
            return Convert.ToBase64String(unencodedApiKeyAsBytes);
        }

        protected static string Base64Encode(string plainText) => Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));

        protected static string Base64Decode(string base64EncodedData) => Encoding.UTF8.GetString(Convert.FromBase64String(base64EncodedData));

        protected static long AmountToMinorUnits(Decimal amount) => Convert.ToInt64(Math.Round(amount * 100M, MidpointRounding.AwayFromZero));

        protected static Decimal AmountFromMinorUnits(long minorUnits) => (Decimal)minorUnits / 100M;


        protected PaymentState GetPaymentStatus(BamboraTransaction transaction)
        {
            if (transaction.Total.Credited > 0)
                return PaymentState.Refunded;

            if (transaction.Total.Declined > 0)
                return PaymentState.Cancelled;

            if (transaction.Total.Captured > 0)
                return PaymentState.Captured;

            if (transaction.Total.Authorized > 0)
                return PaymentState.Authorized;

            return PaymentState.Initialized;
        }
    }
}
