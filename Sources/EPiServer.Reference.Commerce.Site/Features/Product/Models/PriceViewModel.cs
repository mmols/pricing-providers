using Mediachase.Commerce;

namespace EPiServer.Reference.Commerce.Site.Features.Product.Models
{
    public class PriceViewModel
    {
        private decimal _quantity;
        public decimal Quantity
        {
            get { return this._quantity; }
            set
            {
                this._quantity = value == 0 ? 1 : value;
            }
        }

        public Money Price { get; set; }

        public Money DiscountPrice { get; set; }
    }
}