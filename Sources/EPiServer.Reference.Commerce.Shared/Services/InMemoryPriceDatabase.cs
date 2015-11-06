using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Castle.Core.Internal;
using EPiServer.Commerce;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Pricing;

namespace EPiServer.Reference.Commerce.Shared.Services
{
    public static class InMemoryPriceDatabase
    {
        private static ConcurrentDictionary<long, IPriceDetailValue> _pricingDatabase;

        public static ConcurrentDictionary<long, IPriceDetailValue> Prices
        {
            get
            {
                if (_pricingDatabase == null)
                {
                    _pricingDatabase = new ConcurrentDictionary<long, IPriceDetailValue>();

                    var marketService = ServiceLocator.Current.GetInstance<IMarketService>();
                    var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
                    var referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();


                    var descendents = contentLoader.GetDescendents(referenceConverter.GetRootLink());
                    var variations = contentLoader.GetItems(descendents, LanguageSelector.AutoDetect()).OfType<VariationContent>();

                    var catalogKeys = variations.Select(x => new CatalogKey(AppContext.Current.ApplicationId, x.Code));

                    Random random = new Random();

                    var markets = marketService.GetAllMarkets();

                    markets.ForEach(market =>
                    {
                        market.Currencies.ForEach(currency =>
                        {
                            catalogKeys.ForEach(catalogKey =>
                            {
                                var priceValue = new PriceDetailValue()
                                {
                                    PriceValueId = random.Next(1, Int32.MaxValue),
                                    CatalogKey = catalogKey,
                                    CustomerPricing = CustomerPricing.AllCustomers,
                                    MarketId = market.MarketId,
                                    MinQuantity = 0,
                                    UnitPrice = new Money(40m, currency),
                                    ValidFrom = DateTime.MinValue,
                                    ValidUntil = DateTime.MaxValue
                                };

                                _pricingDatabase.AddOrUpdate(priceValue.PriceValueId, priceValue, (l, value) => priceValue);

                                priceValue = new PriceDetailValue()
                                {
                                    PriceValueId = random.Next(1, Int32.MaxValue),
                                    CatalogKey = catalogKey,
                                    CustomerPricing = CustomerPricing.AllCustomers,
                                    MarketId = market.MarketId,
                                    MinQuantity = 2,
                                    UnitPrice = new Money(37m, currency),
                                    ValidFrom = DateTime.MinValue,
                                    ValidUntil = DateTime.MaxValue
                                };

                                _pricingDatabase.AddOrUpdate(priceValue.PriceValueId, priceValue, (l, value) => priceValue);

                                priceValue = new PriceDetailValue()
                                {
                                    PriceValueId = random.Next(1, Int32.MaxValue),
                                    CatalogKey = catalogKey,
                                    CustomerPricing = new CustomerPricing(CustomerPricing.PriceType.UserName, "admin"),
                                    MarketId = market.MarketId,
                                    MinQuantity = 2,
                                    UnitPrice = new Money(30m, currency),
                                    ValidFrom = DateTime.MinValue,
                                    ValidUntil = DateTime.MaxValue
                                };

                                _pricingDatabase.AddOrUpdate(priceValue.PriceValueId, priceValue, (l, value) => priceValue);

                            });
                        });

                    });
                }

                return _pricingDatabase;
            }
        }
    }
}
