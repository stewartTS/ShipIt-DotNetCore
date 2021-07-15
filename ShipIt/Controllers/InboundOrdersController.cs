using Microsoft.AspNetCore.Mvc;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShipIt.Controllers
{
    [Route("orders/inbound")]
    public class InboundOrderController : ControllerBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

        private readonly IEmployeeRepository _employeeRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IProductRepository _productRepository;
        private readonly IStockRepository _stockRepository;

        public InboundOrderController(IEmployeeRepository employeeRepository, ICompanyRepository companyRepository, IProductRepository productRepository, IStockRepository stockRepository)
        {
            _employeeRepository = employeeRepository;
            _stockRepository = stockRepository;
            _companyRepository = companyRepository;
            _productRepository = productRepository;
        }

        [HttpGet("{warehouseId}")]
        public InboundOrderResponse Get([FromRoute] int warehouseId)
        {
            Log.Info("orderIn for warehouseId: " + warehouseId);

            //queries database to get operations manager
            var operationsManager = new Employee(_employeeRepository.GetOperationsManager(warehouseId));

            Log.Debug($"Found operations manager: {operationsManager}");

            //queries database to get stock
            var allStock = _stockRepository.GetStockByWarehouseId(warehouseId).ToList();
            var allStockIds = allStock.Select(stock => stock.ProductId).ToList();
            var allProductData = _productRepository.GetProductsById(allStockIds);
            var allProducts = allProductData.Select(product => new Product(product)).ToList();

            var allProductGcps = allProducts.Select(product => product.Gcp).ToList();
            var allCompanyData = _companyRepository.GetCompanies(allProductGcps).ToList();
            var allCompanies = allCompanyData.Select(company => new Company(company)).ToList();

            var orderlinesByCompany = new Dictionary<Company, List<InboundOrderLine>>();

            foreach (var stock in allStock)
            {
                var product = allProducts.Single(product => product.Id == stock.ProductId);
                if (stock.held < product.LowerThreshold && !product.Discontinued)
                {
                    var company = allCompanies.Single(company => company.Gcp == product.Gcp);

                    var orderQuantity = Math.Max(product.LowerThreshold * 3 - stock.held, product.MinimumOrderQuantity);

                    if (!orderlinesByCompany.ContainsKey(company))
                    {
                        orderlinesByCompany.Add(company, new List<InboundOrderLine>());
                    }

                    orderlinesByCompany[company].Add(
                        new InboundOrderLine()
                        {
                            gtin = product.Gtin,
                            name = product.Name,
                            quantity = orderQuantity
                        });
                }
            }


            Log.Debug($"Constructed order lines: {orderlinesByCompany}");

            var orderSegments = orderlinesByCompany.Select(ol => new OrderSegment()
            {
                OrderLines = ol.Value,
                Company = ol.Key
            });

            Log.Info("Constructed inbound order");

            return new InboundOrderResponse()
            {
                OperationsManager = operationsManager,
                WarehouseId = warehouseId,
                OrderSegments = orderSegments
            };
        }

        [HttpPost("")]
        public void Post([FromBody] InboundManifestRequestModel requestModel)
        {
            Log.Info("Processing manifest: " + requestModel);

            var gtins = new List<string>();

            foreach (var orderLine in requestModel.OrderLines)
            {
                if (gtins.Contains(orderLine.gtin))
                {
                    throw new ValidationException(string.Format("Manifest contains duplicate product gtin: {0}", orderLine.gtin));
                }
                gtins.Add(orderLine.gtin);
            }

            var productDataModels = _productRepository.GetProductsByGtin(gtins);
            var products = productDataModels.ToDictionary(p => p.Gtin, p => new Product(p));

            Log.Debug(string.Format("Retrieved products to verify manifest: {0}", products));

            var lineItems = new List<StockAlteration>();
            var errors = new List<string>();

            foreach (var orderLine in requestModel.OrderLines)
            {
                if (!products.ContainsKey(orderLine.gtin))
                {
                    errors.Add(string.Format("Unknown product gtin: {0}", orderLine.gtin));
                    continue;
                }

                var product = products[orderLine.gtin];
                if (!product.Gcp.Equals(requestModel.Gcp))
                {
                    errors.Add(string.Format("Manifest GCP ({0}) doesn't match Product GCP ({1})",
                        requestModel.Gcp, product.Gcp));
                }
                else
                {
                    lineItems.Add(new StockAlteration(product.Id, orderLine.quantity));
                }
            }

            if (errors.Count() > 0)
            {
                Log.Debug(string.Format("Found errors with inbound manifest: {0}", errors));
                throw new ValidationException(string.Format("Found inconsistencies in the inbound manifest: {0}", string.Join("; ", errors)));
            }

            Log.Debug(string.Format("Increasing stock levels with manifest: {0}", requestModel));
            _stockRepository.AddStock(requestModel.WarehouseId, lineItems);
            Log.Info("Stock levels increased");
        }
    }
}
