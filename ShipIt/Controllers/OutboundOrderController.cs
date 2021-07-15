using Microsoft.AspNetCore.Mvc;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShipIt.Controllers
{
    [Route("orders/outbound")]
    public class OutboundOrderController : ControllerBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

        private readonly IStockRepository _stockRepository;
        private readonly IProductRepository _productRepository;

        public OutboundOrderController(IStockRepository stockRepository, IProductRepository productRepository)
        {
            _stockRepository = stockRepository;
            _productRepository = productRepository;
        }

        [HttpPost("")]
        public OutboundOrderResponse Post([FromBody] OutboundOrderRequestModel request)
        {
            Log.Info($"Processing outbound order: {request}");

            var gtins = request.OrderLines.Select(orderLine => orderLine.gtin).ToList();
            var duplicateGtins = gtins.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key).ToList();
            if (duplicateGtins.Count > 0)
                {
                    throw new ValidationException($"Outbound order request contains duplicate product gtin: {string.Join(", ", duplicateGtins)}");
                }

            var productDataModels = _productRepository.GetProductsByGtin(gtins);
            var products = productDataModels.ToDictionary(p => p.Gtin, p => new Product(p));

            var lineItems = new List<StockAlteration>();
            var productIds = new List<int>();
            var errors = new List<string>();

            foreach (var orderLine in request.OrderLines)
            {
                //checks if product in order exists in any of our warehouses.
                if (!products.ContainsKey(orderLine.gtin))
                {
                    errors.Add($"Unknown product gtin: {orderLine.gtin}");
                }
                else
                {
                    var product = products[orderLine.gtin];
                    lineItems.Add(new StockAlteration(product.Id, orderLine.quantity, product.Weight));
                    productIds.Add(product.Id);
                }
            }

            if (errors.Count > 0)
            {
                throw new NoSuchEntityException(string.Join("; ", errors));
            }

            var stock = _stockRepository.GetStockByWarehouseAndProductIds(request.WarehouseId, productIds);

            var orderLines = request.OrderLines.ToList();

            for (var i = 0; i < lineItems.Count; i++)
            {
                var lineItem = lineItems[i];
                var orderLine = orderLines[i];


                // Checks if products actually exist by Product ID in a specific warehouse.
                if (!stock.ContainsKey(lineItem.ProductId))
                {
                    errors.Add($"Product: {orderLine.gtin}, no stock held");
                    continue;
                }

                var item = stock[lineItem.ProductId];

                //Checks if the quantity is greater than the amount available.
                if (lineItem.Quantity > item.held)
                {
                    errors.Add($"Product: {orderLine.gtin}, stock held: {item.held}, stock to remove: {lineItem.Quantity}");
                }
            }

            // If there are any errors in array, exception is thrown.
            if (errors.Count > 0)
            {
                throw new InsufficientStockException(string.Join("; ", errors));
            }

            var totalWeight = 0.0;

            foreach (var item in lineItems)
            {
                totalWeight += item.Quantity * item.Weight;
            }

            var totalWeightKG = totalWeight / 1000;
            var trucksRequired = Convert.ToInt32(Math.Ceiling(totalWeightKG / 2000));

            _stockRepository.RemoveStock(request.WarehouseId, lineItems);

            return new OutboundOrderResponse(trucksRequired);


        }
    }
}