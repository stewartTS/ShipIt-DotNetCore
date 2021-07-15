namespace ShipIt.Models.ApiModels
{
    public class OutboundOrderResponse : Response
    {
        public int TrucksRequired { get; set; }
        public OutboundOrderResponse(int trucksRequired)
        {
            TrucksRequired = trucksRequired;
        
        }

    }
}