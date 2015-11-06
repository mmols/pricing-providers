using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Events.ChangeNotification;
using EPiServer.Framework.Cache;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Engine.Events;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Pricing;
using Castle.Core.Internal;

namespace EPiServer.Reference.Commerce.Shared.Services
{
    public class MyPriceService : IPriceService
    {

        private IMarketService _marketService;
        private ReferenceConverter _referenceConverter;
        public MyPriceService(ICatalogSystem catalogSystem, IChangeNotificationManager changeManager, ISynchronizedObjectInstanceCache objectInstanceCache, CatalogKeyEventBroadcaster broadcaster, IApplicationContext applicationContext)
        {
            _marketService = ServiceLocator.Current.GetInstance<IMarketService>();
            _referenceConverter = ServiceLocator.Current.GetInstance<ReferenceConverter>();
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }


        public IPriceValue GetDefaultPrice(MarketId market, DateTime validOn, CatalogKey catalogKey, Currency currency)
        {
            PriceFilter filter = new PriceFilter()
            {
                Quantity = new Decimal?(new Decimal(0)),
                Currencies = (IEnumerable<Mediachase.Commerce.Currency>)new Mediachase.Commerce.Currency[1]
                {
                    currency
                },
                CustomerPricing = (IEnumerable<CustomerPricing>)new CustomerPricing[1]
                {
                    CustomerPricing.AllCustomers
                }
            };

            return GetPrices(market, validOn, catalogKey, filter).FirstOrDefault();
        }

        public IEnumerable<IPriceValue> GetPrices(MarketId market, DateTime validOn, CatalogKey catalogKey, PriceFilter filter)
        {
            return GetPrices(market, validOn, new CatalogKey[] { catalogKey }, filter);
        }

        public IEnumerable<IPriceValue> GetPrices(MarketId market, DateTime validOn, IEnumerable<CatalogKey> catalogKeys, PriceFilter filter)
        {
            var quantity = filter.Quantity ?? new Decimal(0);
            return GetPrices(market, validOn, catalogKeys.Select(x => new CatalogKeyAndQuantity(x, quantity)), filter);
        }

        public IEnumerable<IPriceValue> GetPrices(MarketId marketId, DateTime validOn, IEnumerable<CatalogKeyAndQuantity> catalogKeysAndQuantities, PriceFilter filter)
        {
            var prices = new List<IPriceValue>();

            foreach (var catalogKeyAndQuantity in catalogKeysAndQuantities)
            {
                var itemPrices = GetCatalogEntryPrices(catalogKeyAndQuantity.CatalogKey).ToList();

                if (catalogKeyAndQuantity.Quantity > 0)
                {
                    itemPrices.RemoveAll(x => x.MinQuantity > catalogKeyAndQuantity.Quantity);
                }

                prices.AddRange(itemPrices);
            }

            foreach (var price in prices.ToList())
            {
                if (marketId != MarketId.Empty && price.MarketId != marketId)
                {
                    prices.Remove(price);
                    continue;
                }

                if (filter.CustomerPricing != null && filter.CustomerPricing.Any())
                {
                    if (!filter.CustomerPricing.Contains(price.CustomerPricing) && !IsPriceOverride(price.CustomerPricing))
                    {
                        prices.Remove(price);
                        continue;
                    }
                }

            }

            var returnPrices = new List<IPriceValue>();

            if (filter.ReturnCustomerPricing)
            {
                var groupedPrices = prices.GroupBy(x => new { x.CatalogKey, x.MinQuantity, x.CustomerPricing });

                groupedPrices.ForEach(priceGroup =>
                {
                    var orderedPriceGroup = priceGroup.OrderBy(x => x.UnitPrice);
                    returnPrices.Add(orderedPriceGroup.FirstOrDefault());
                });
            }
            else
            {
                var groupedPrices = prices.GroupBy(x => new { x.CatalogKey, x.MinQuantity });

                groupedPrices.ForEach(priceGroup =>
                {
                    var orderedPriceGroup = priceGroup.OrderBy(x => x.UnitPrice);

                    returnPrices.Add(orderedPriceGroup.FirstOrDefault(d => IsPriceOverride(d.CustomerPricing)) ??
                                            orderedPriceGroup.FirstOrDefault());
                });
            }

            return returnPrices.OrderBy(x => x.UnitPrice);

        }

        private bool IsPriceOverride(CustomerPricing pricing)
        {
            if (pricing == null)
            {
                return false;
            }

            return Convert.ToInt32(pricing.PriceTypeId).Equals(3);
        }


        public IEnumerable<IPriceValue> GetCatalogEntryPrices(CatalogKey catalogKey)
        {
            return GetCatalogEntryPrices(new CatalogKey[] { catalogKey });
        }

        public IEnumerable<IPriceValue> GetCatalogEntryPrices(IEnumerable<CatalogKey> catalogKeys)
        {
            var returnPrices = new List<IPriceValue>();
            foreach (var catalogKey in catalogKeys)
            {
                returnPrices.AddRange(InMemoryPriceDatabase.Prices.Where(
                    x => x.Value.CatalogKey.CatalogEntryCode == catalogKey.CatalogEntryCode).Select(x => x.Value));
            }

            return returnPrices;
        }


        public void SetCatalogEntryPrices(CatalogKey catalogKey, IEnumerable<IPriceValue> priceValues)
        {
            throw new NotImplementedException();
        }

        public void SetCatalogEntryPrices(IEnumerable<CatalogKey> catalogKeys, IEnumerable<IPriceValue> priceValues)
        {
            throw new NotImplementedException();
        }

        public void ReplicatePriceDetailChanges(IEnumerable<CatalogKey> catalogKeys, IEnumerable<IPriceValue> priceValues)
        {
            throw new NotImplementedException();
        }


    }
}
