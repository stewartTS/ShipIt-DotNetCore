using Npgsql;
using ShipIt.Models.ApiModels;
using ShipIt.Models.DataModels;
using System.Collections.Generic;
using System.Linq;

namespace ShipIt.Repositories
{
    public interface ICompanyRepository
    {
        int GetCount();
        CompanyDataModel GetCompany(string gcp);
        IEnumerable<CompanyDataModel> GetCompanies(List<string> gcps);
        void AddCompanies(IEnumerable<Company> companies);
    }

    public class CompanyRepository : RepositoryBase, ICompanyRepository
    {
        public int GetCount()
        {
            var CompanyCountSQL = "SELECT COUNT(*) FROM gcp";
            return (int)QueryForLong(CompanyCountSQL);
        }

        public CompanyDataModel GetCompany(string gcp)
        {
            var sql =
                "SELECT gcp_cd, gln_nm, gln_addr_02, gln_addr_03, gln_addr_04, gln_addr_postalcode, gln_addr_city, contact_tel, contact_mail " +
                "FROM gcp " +
                "WHERE gcp_cd = @gcp_cd";
            var parameter = new NpgsqlParameter("@gcp_cd", gcp);
            var noProductWithIdErrorMessage = $"No companies found with gcp: {gcp}";
            return base.RunSingleGetQuery(sql, reader => new CompanyDataModel(reader), noProductWithIdErrorMessage, parameter);
        }

        public IEnumerable<CompanyDataModel> GetCompanies(List<string> gcps)
        {
            if(gcps.Count == 0)
            {
                return new List<CompanyDataModel>();
            }
            var sql =
                $"SELECT gcp_cd, gln_nm, gln_addr_02, gln_addr_03, gln_addr_04, gln_addr_postalcode, gln_addr_city, contact_tel, contact_mail FROM gcp WHERE gcp_cd IN ('{string.Join("','", gcps)}')";
            return base.RunGetQuery(sql, reader => new CompanyDataModel(reader), "No companies found with given gcps", null);
        }


        public void AddCompanies(IEnumerable<Company> companies)
        {
            var sql =
                "INSERT INTO gcp (gcp_cd, gln_nm, gln_addr_02, gln_addr_03, gln_addr_04, gln_addr_postalcode, gln_addr_city, contact_tel, contact_mail) " +
                "VALUES (@gcp_cd, @gln_nm, @gln_addr_02, @gln_addr_03, @gln_addr_04, @gln_addr_postalcode, @gln_addr_city, @contact_tel, @contact_mail)";

            var parametersList = new List<NpgsqlParameter[]>();
            foreach (var company in companies)
            {
                var companyDataModel = new CompanyDataModel(company);
                parametersList.Add(companyDataModel.GetNpgsqlParameters().ToArray());
            }

            base.RunTransaction(sql, parametersList);
        }
    }

}