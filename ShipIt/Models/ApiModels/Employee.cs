﻿using ShipIt.Models.DataModels;
using System;
using System.Text;

namespace ShipIt.Models.ApiModels
{
    public class Employee
    {
        public string Name { get; set; }
        public int WarehouseId { get; set; }
        public EmployeeRole role { get; set; }
        public string ext { get; set; }

        public Employee(EmployeeDataModel dataModel)
        {
            Name = dataModel.Name;
            WarehouseId = dataModel.WarehouseId;
            role = MapDatabaseRoleToApiRole(dataModel.Role);
            ext = dataModel.Ext;
        }

        private EmployeeRole MapDatabaseRoleToApiRole(string databaseRole)
        {
            if (databaseRole == DataBaseRoles.Cleaner)
            {
                return EmployeeRole.CLEANER;
            }

            if (databaseRole == DataBaseRoles.Manager)
            {
                return EmployeeRole.MANAGER;
            }

            if (databaseRole == DataBaseRoles.OperationsManager)
            {
                return EmployeeRole.OPERATIONS_MANAGER;
            }

            if (databaseRole == DataBaseRoles.Picker)
            {
                return EmployeeRole.PICKER;
            }

            throw new ArgumentOutOfRangeException("DatabaseRole");
        }

        //Empty constructor needed for Xml serialization
        public Employee()
        {
        }

        public override string ToString()
        {
            return new StringBuilder()
                    .AppendFormat($"name: {Name}, ")
                    .AppendFormat($"warehouseId: {WarehouseId}, ")
                    .AppendFormat($"role: {role}, ")
                    .AppendFormat($"ext: {ext}")
                    .ToString();
        }
    }
}