﻿using System.Collections.Generic;
using System.Web.Mvc;
using Mediachase.Commerce;
using Mediachase.Commerce.Pricing;

namespace EPiServer.Reference.Commerce.Site.Features.Product.Models
{
    public class FashionProductViewModel
    {
        public IEnumerable<PriceViewModel> Prices { get; set; }
        public FashionProduct Product { get; set; }
        public FashionVariant Variation { get; set; }
        public IList<SelectListItem> Colors { get; set; }
        public IList<SelectListItem> Sizes { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
        public IList<string> Images { get; set; }
    }
}