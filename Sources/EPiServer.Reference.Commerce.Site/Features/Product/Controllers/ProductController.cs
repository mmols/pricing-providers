using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Castle.Core.Internal;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Core;
using EPiServer.Filters;
using EPiServer.Reference.Commerce.Site.Features.Product.Models;
using EPiServer.Reference.Commerce.Site.Features.Shared.Extensions;
using EPiServer.Reference.Commerce.Site.Features.Shared.Services;
using EPiServer.Reference.Commerce.Site.Infrastructure.Facades;
using EPiServer.Web.Mvc;
using EPiServer.Web.Routing;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Pricing;
using EPiServer.Reference.Commerce.Site.Features.Market.Services;
using EPiServer.Security;
using Mediachase.Commerce.Security;

namespace EPiServer.Reference.Commerce.Site.Features.Product.Controllers
{
    public class ProductController : ContentController<FashionProduct>
    {
        private readonly IPromotionEntryService _promotionEntryService;
        private readonly IContentLoader _contentLoader;
        private readonly IPriceService _priceService;
        private readonly ICurrentMarket _currentMarket;
        private readonly ICurrencyService _currencyservice;
        private readonly IRelationRepository _relationRepository;
        private readonly AppContextFacade _appContext;
        private readonly UrlResolver _urlResolver;
        private readonly FilterPublished _filterPublished;
        private readonly CultureInfo _preferredCulture;
        private readonly bool _isInEditMode;
        private PromotionHelperFacade _promotionHelper;

        public ProductController(
            IPromotionEntryService promotionEntryService,
            IContentLoader contentLoader,
            IPriceService priceService,
            ICurrentMarket currentMarket,
            CurrencyService currencyservice,
            IRelationRepository relationRepository,
            AppContextFacade appContext,
            UrlResolver urlResolver,
            FilterPublished filterPublished,
            Func<CultureInfo> preferredCulture,
            Func<bool> isInEditMode)
        {
            _promotionEntryService = promotionEntryService;
            _contentLoader = contentLoader;
            _priceService = priceService;
            _currentMarket = currentMarket;
            _currencyservice = currencyservice;
            _relationRepository = relationRepository;
            _appContext = appContext;
            _urlResolver = urlResolver;
            _preferredCulture = preferredCulture();
            _isInEditMode = isInEditMode();
            _filterPublished = filterPublished;
        }

        [HttpGet]
        public ActionResult Index(FashionProduct currentContent, string variationCode = "", bool quickview = false)
        {
            var variations = GetVariations(currentContent).ToList();
            if (_isInEditMode && !variations.Any())
            {
                var productWithoutVariation = new FashionProductViewModel
                {
                    Product = currentContent,
                    Images = currentContent.GetAssets<IContentImage>(_contentLoader, _urlResolver)
                };
                return Request.IsAjaxRequest() ? PartialView("ProductWithoutVariation", productWithoutVariation) : (ActionResult)View("ProductWithoutVariation", productWithoutVariation);
            }
            FashionVariant variation;
            if (!TryGetFashionVariant(variations, variationCode, out variation))
            {
                return HttpNotFound();
            }

            var market = _currentMarket.GetCurrentMarket();
            var currency = _currencyservice.GetCurrentCurrency();

            var priceFilters = new List<CustomerPricing>();

            priceFilters.Add(CustomerPricing.AllCustomers);

            var currentContact = PrincipalInfo.Current.Principal.GetCustomerContact();
            if (currentContact != null)
            {
                priceFilters.Add(new CustomerPricing(CustomerPricing.PriceType.PriceGroup, currentContact.EffectiveCustomerGroup));
                priceFilters.Add(new CustomerPricing(CustomerPricing.PriceType.UserName, PrincipalInfo.Current.Name));
            }

            var prices = _priceService.GetPrices(market.MarketId, DateTime.Now,
                new CatalogKey(_appContext.ApplicationId, variation.Code),
                new PriceFilter { Currencies = new Currency[] { currency }, CustomerPricing = priceFilters });

            var priceViewModels = prices.Select(price => new PriceViewModel()
            {
                Quantity = price.MinQuantity,
                Price = price.UnitPrice,
                DiscountPrice = GetDiscountPrice(variation, price, market, currency)
            }).OrderBy(x => x.Quantity);



            var viewModel = new FashionProductViewModel
            {
                Prices = priceViewModels,
                Product = currentContent,
                Variation = variation,
                Colors = variations
                    .Where(x => x.Size != null && x.Size == variation.Size)
                    .Select(x => new SelectListItem
                    {
                        Selected = false,
                        Text = x.Color,
                        Value = x.Color
                    })
                    .ToList(),
                Sizes = variations
                    .Where(x => x.Color != null && x.Color == variation.Color)
                    .Select(x => new SelectListItem
                    {
                        Selected = false,
                        Text = x.Size,
                        Value = x.Size
                    })
                    .ToList(),
                Color = variation.Color,
                Size = variation.Size,
                Images = variation.GetAssets<IContentImage>(_contentLoader, _urlResolver)
            };

            if (quickview)
            {
                return PartialView("Quickview", viewModel);
            }

            return Request.IsAjaxRequest() ? PartialView(viewModel) : (ActionResult)View(viewModel);
        }

        [HttpPost]
        public ActionResult SelectVariant(FashionProduct currentContent, string color, string size)
        {
            var variations = GetVariations(currentContent);

            FashionVariant variation;
            if (!TryGetFashionVariantByColorAndSize(variations, color, size, out variation))
            {
                return HttpNotFound();
            }

            return RedirectToAction("Index", new { variationCode = variation.Code });
        }

        private IEnumerable<FashionVariant> GetVariations(FashionProduct currentContent)
        {
            return _contentLoader
                .GetItems(currentContent.GetVariants(_relationRepository), _preferredCulture)
                .Cast<FashionVariant>()
                .Where(v => v.IsAvailableInCurrentMarket(_currentMarket) && !_filterPublished.ShouldFilter(v));
        }

        private static bool TryGetFashionVariant(IEnumerable<FashionVariant> variations, string variationCode, out FashionVariant variation)
        {
            variation = !string.IsNullOrEmpty(variationCode) ?
                variations.FirstOrDefault(x => x.Code == variationCode) :
                variations.FirstOrDefault();

            return variation != null;
        }

        private static bool TryGetFashionVariantByColorAndSize(IEnumerable<FashionVariant> variations, string color, string size, out FashionVariant variation)
        {
            variation = variations.FirstOrDefault(x =>
                x.Color.Equals(color, StringComparison.OrdinalIgnoreCase) &&
                x.Size.Equals(size, StringComparison.OrdinalIgnoreCase));

            return variation != null;
        }

        private IPriceValue GetDefaultPrice(FashionVariant variation, IMarket market, Currency currency)
        {
            return _priceService.GetDefaultPrice(
                market.MarketId,
                DateTime.Now,
                new CatalogKey(_appContext.ApplicationId, variation.Code),
                currency);
        }

        private Money GetDiscountPrice(EntryContentBase content, IPriceValue defaultPrice, IMarket market, Currency currency)
        {
            if (defaultPrice == null)
            {
                return new Money(0, currency);
            }

            if (_promotionHelper == null)
            {
                _promotionHelper = new PromotionHelperFacade();
            }

            _promotionHelper.Reset();

            return _promotionEntryService.GetDiscountPrice(defaultPrice, content, currency, _promotionHelper).UnitPrice;
        }
    }
}
