using ShipIt.Exceptions;

namespace ShipIt.Models.ApiModels
{
    public class StockAlteration
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        public float Weight { get; set; }

        public StockAlteration(int productId, int quantity)
        {
            ProductId = productId;
            Quantity = quantity;

            if (quantity < 0)
            {
                throw new MalformedRequestException("Alteration must be positive");
            }
        }

        public StockAlteration(int productId, int quantity, float weight)
        {
            ProductId = productId;
            Quantity = quantity;
            Weight = weight;

            if (quantity < 0)
            {
                throw new MalformedRequestException("Alteration must be positive");
            }
        }
    }
}