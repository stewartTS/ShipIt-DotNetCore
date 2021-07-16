﻿using Microsoft.AspNetCore.Mvc;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Repositories;
using System.Linq;

namespace ShipIt.Controllers
{

    [Route("employees")]
    public class EmployeeController : ControllerBase
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeController(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        [HttpGet("")]
        public EmployeeResponse Get([FromQuery] string name)
        {
            Log.Info($"Looking up employee by name: {name}");

            var employees = _employeeRepository
                .GetEmployeesByName(name)
                .Select(employee => new Employee(employee));
            
            Log.Info("Found employee: " + employees);
            return new EmployeeResponse(employees);
        }

        [HttpGet("{warehouseId}")]
        public EmployeeResponse Get([FromRoute] int warehouseId)
        {
            Log.Info(string.Format("Looking up employee by id: {0}", warehouseId));

            var employees = _employeeRepository
                .GetEmployeesByWarehouseId(warehouseId)
                .Select(e => new Employee(e));

            Log.Info($"Found employees: {employees}");

            return new EmployeeResponse(employees);
        }

        [HttpPost("")]
        public Response Post([FromBody] AddEmployeesRequest requestModel)
        {
            var employeesToAdd = requestModel.Employees;

            if (employeesToAdd.Count == 0)
            {
                throw new MalformedRequestException("Expected at least one <employee> tag");
            }

            Log.Info("Adding employees: " + employeesToAdd);

            _employeeRepository.AddEmployees(employeesToAdd);

            Log.Debug("Employees added successfully");

            return new Response() { Success = true };
        }

        [HttpDelete("")]
        public void Delete([FromBody] RemoveEmployeeRequest requestModel)
        {
            var name = requestModel.Name;
            if (name == null)
            {
                throw new MalformedRequestException("Unable to parse name from request parameters");
            }

            try
            {
                _employeeRepository.RemoveEmployee(name);
            }
            catch (NoSuchEntityException)
            {
                throw new NoSuchEntityException("No employee exists with name: " + name);
            }
        }
    }
}
