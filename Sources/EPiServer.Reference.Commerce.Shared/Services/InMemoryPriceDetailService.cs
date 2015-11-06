using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Pricing;

namespace EPiServer.Reference.Commerce.Shared.Services
{

    public class InMemoryPriceDetailService : IPriceDetailService
    {
        private readonly ReferenceConverter _referenceConverter;
        private readonly IContentLoader _contentLoader;
        private IRelationRepository _relationRepository;

        public InMemoryPriceDetailService(ReferenceConverter referenceConverter)
        {
            this._referenceConverter = referenceConverter;
           
            _contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            _relationRepository = ServiceLocator.Current.GetInstance<IRelationRepository>();
        }

        public IPriceDetailValue Get(long priceValueId)
        {
            return InMemoryPriceDatabase.Prices.FirstOrDefault(x => x.Key == priceValueId).Value;
        }

        public IList<IPriceDetailValue> List(ContentReference catalogContentReference)
        {

            var contentToFetchPrices = new List<EntryContentBase>();
            var catalogContentItem = _contentLoader.Get<CatalogContentBase>(catalogContentReference);

            if (catalogContentItem == null)
            {
                return Enumerable.Empty<IPriceDetailValue>().ToList();
            }
            else if (catalogContentItem is VariationContent)
            {
                contentToFetchPrices.Add(catalogContentItem as VariationContent);
            }
            else if (catalogContentItem is ProductContent)
            {
                contentToFetchPrices.Add(catalogContentItem as ProductContent);
            }
            else if (catalogContentItem is NodeContent)
            {
                contentToFetchPrices.AddRange(_contentLoader.GetChildren<EntryContentBase>(catalogContentItem.ContentLink));
            }
            else if (catalogContentItem is BundleContent)
            {
                var bundleContent = catalogContentItem as BundleContent;
                contentToFetchPrices.AddRange(_contentLoader.GetItems(bundleContent.GetEntries(_relationRepository),
                    catalogContentItem.Language).OfType<EntryContentBase>());
            }
            else if (catalogContentItem is PackageContent)
            {
                var packageContent = catalogContentItem as PackageContent;
                contentToFetchPrices.AddRange(_contentLoader.GetItems(packageContent.GetEntries(_relationRepository),
                    catalogContentItem.Language).OfType<EntryContentBase>());
            }

            //Get linked variants for any products in our fetch list and add them to the list
            var variants = new List<EntryContentBase>();
            foreach (var content in contentToFetchPrices)
            {
                variants.AddRange(GetAttachedVariants(content));
            }

            contentToFetchPrices.AddRange(variants);

            return InMemoryPriceDatabase.Prices.Where(x => contentToFetchPrices.Select(c => c.Code).Contains(x.Value.CatalogKey.CatalogEntryCode))
                    .Select(x => x.Value).ToList();
        }

        private List<EntryContentBase> GetAttachedVariants(EntryContentBase content)
        {
            var returnContents = new List<EntryContentBase>();

            if (content is ProductContent)
            {
                var productContent = content as ProductContent;
                var variations = _contentLoader.GetItems(productContent.GetVariants(_relationRepository), content.Language).OfType<EntryContentBase>();
                returnContents.AddRange(variations);
            }

            return returnContents;
        } 

        public IList<IPriceDetailValue> List(ContentReference catalogContentReference, int offset, int count, out int totalCount)
        {
            var prices = List(catalogContentReference);
            totalCount = prices.Count();
            var returnVal = prices.AsQueryable().Skip(offset).Take(count);

            return returnVal.ToList();
        }

        public IList<IPriceDetailValue> List(ContentReference catalogContentReference, MarketId marketId, PriceFilter filter, int offset, int count, out int totalCount)
        {
            var prices = List(catalogContentReference).ToList();

            foreach (var price in prices.ToList())
            {
                if (marketId != MarketId.Empty && price.MarketId != marketId)
                {
                    prices.Remove(price);
                    continue;
                }

                if (filter.Quantity != null)
                {
                    if (price.MinQuantity > filter.Quantity)
                    {
                        prices.Remove(price);
                        continue;
                    }
                }

                if (filter.CustomerPricing != null && filter.CustomerPricing.Any())
                {
                    if (!filter.CustomerPricing.Contains(price.CustomerPricing))
                    {
                        prices.Remove(price);
                        continue;
                    }
                }
            }


            totalCount = prices.Count();
            var returnVal = prices.AsQueryable().Skip(offset).Take(count);

            return returnVal.ToList();
        }

        public IList<IPriceDetailValue> Save(IEnumerable<IPriceDetailValue> priceValues)
        {
            var returnValues = new List<IPriceDetailValue>();
            foreach (var priceValue in priceValues)
            {
                if (priceValue.PriceValueId == 0)
                {
                    Random random = new Random();
                    priceValue.PriceValueId = random.Next(1, Int32.MaxValue);
                }
                returnValues.Add(InMemoryPriceDatabase.Prices.AddOrUpdate(priceValue.PriceValueId, priceValue,
                    (l, value) => priceValue));
            }

            return returnValues;
        }

        public void Delete(IEnumerable<long> priceValueIds)
        {
            foreach (var priceValueId in priceValueIds)
            {
                IPriceDetailValue deletedValue;
                InMemoryPriceDatabase.Prices.TryRemove(priceValueId, out deletedValue);
            }            
        }

        public void ReplicatePriceServiceChanges(IEnumerable<CatalogKey> catalogKeySet, IEnumerable<IPriceValue> priceValuesList)
        {
            
        }

        public bool IsReadOnly
        {
            get { return false; }
        }


    }
}
