using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;

namespace SkyMineralsModern.Pages
{
    public class ReportsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1);
        public DateTime EndDate { get; set; } = DateTime.Now;
        
        public double TotalQuantity { get; set; }
        public double TotalValue { get; set; }
        public double TotalProduction { get; set; }
        public int ProductionCount { get; set; }
        
        public List<SalesByMineralItem> SalesByMineral { get; set; } = new List<SalesByMineralItem>();
        public List<RecentSaleItem> RecentSales { get; set; } = new List<RecentSaleItem>();
        public List<RecentProductionItem> RecentProduction { get; set; } = new List<RecentProductionItem>();
        
        public string Message { get; set; } = string.Empty;

        public ReportsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet(DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue) StartDate = startDate.Value;
            if (endDate.HasValue) EndDate = endDate.Value;
            
            LoadReports();
        }

        private void LoadReports()
        {
            try
            {
                string? connString = _configuration.GetConnectionString("PostgresConnection");
                if (string.IsNullOrEmpty(connString))
                {
                    Message = "Connection string not configured";
                    return;
                }

                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                // Get total sales
                string salesQuery = @"SELECT COALESCE(SUM(Quantity_sold), 0) as TotalQty, 
                                             COALESCE(SUM(Quantity_sold * 100), 0) as TotalVal 
                                      FROM Minerals_sold 
                                      WHERE Sales_date BETWEEN @StartDate AND @EndDate";
                using var salesCmd = new NpgsqlCommand(salesQuery, conn);
                salesCmd.Parameters.AddWithValue("@StartDate", StartDate);
                salesCmd.Parameters.AddWithValue("@EndDate", EndDate);
                using var reader = salesCmd.ExecuteReader();
                if (reader.Read())
                {
                    TotalQuantity = Convert.ToDouble(reader["TotalQty"]);
                    TotalValue = Convert.ToDouble(reader["TotalVal"]);
                }
                reader.Close();

                // Get total production
                string prodQuery = @"SELECT COALESCE(SUM(Quantity_mined), 0) as TotalProd, 
                                            COUNT(*) as ProdCount 
                                     FROM Productivity 
                                     WHERE Issue_date BETWEEN @StartDate AND @EndDate";
                using var prodCmd = new NpgsqlCommand(prodQuery, conn);
                prodCmd.Parameters.AddWithValue("@StartDate", StartDate);
                prodCmd.Parameters.AddWithValue("@EndDate", EndDate);
                using var prodReader = prodCmd.ExecuteReader();
                if (prodReader.Read())
                {
                    TotalProduction = Convert.ToDouble(prodReader["TotalProd"]);
                    ProductionCount = Convert.ToInt32(prodReader["ProdCount"]);
                }
                prodReader.Close();

                // Sales by mineral
                string mineralQuery = @"SELECT m.Type, COALESCE(SUM(s.Quantity_sold), 0) as Qty
                                        FROM Minerals m
                                        LEFT JOIN Minerals_sold s ON 1=1
                                        WHERE s.Sales_date BETWEEN @StartDate AND @EndDate OR s.Sales_date IS NULL
                                        GROUP BY m.Type
                                        ORDER BY Qty DESC";
                using var mineralCmd = new NpgsqlCommand(mineralQuery, conn);
                mineralCmd.Parameters.AddWithValue("@StartDate", StartDate);
                mineralCmd.Parameters.AddWithValue("@EndDate", EndDate);
                using var mineralReader = mineralCmd.ExecuteReader();
                while (mineralReader.Read())
                {
                    SalesByMineral.Add(new SalesByMineralItem
                    {
                        MineralType = mineralReader["Type"].ToString() ?? "",
                        Quantity = Convert.ToDouble(mineralReader["Qty"])
                    });
                }
                mineralReader.Close();

                // Recent sales
                string recentSalesQuery = @"SELECT s.Sales_date, m.Type as MineralType, s.Quantity_sold, c.Name as CounterName
                                            FROM Minerals_sold s
                                            JOIN Minerals m ON m.Minls_code = 1
                                            JOIN Counter c ON c.Counter_code = s.Counter_code
                                            ORDER BY s.Sales_date DESC LIMIT 10";
                using var recentSalesCmd = new NpgsqlCommand(recentSalesQuery, conn);
                using var recentSalesReader = recentSalesCmd.ExecuteReader();
                while (recentSalesReader.Read())
                {
                    RecentSales.Add(new RecentSaleItem
                    {
                        Sales_date = Convert.ToDateTime(recentSalesReader["Sales_date"]),
                        MineralType = recentSalesReader["MineralType"].ToString() ?? "",
                        Quantity_sold = Convert.ToDouble(recentSalesReader["Quantity_sold"]),
                        CounterName = recentSalesReader["CounterName"].ToString() ?? ""
                    });
                }
                recentSalesReader.Close();

                // Recent production
                string recentProdQuery = @"SELECT p.Issue_date, m.Type as MineralType, p.Quantity_mined, 
                                                  CONCAT(s.Fname, ' ', s.Lname) as StaffName
                                           FROM Productivity p
                                           JOIN Minerals m ON m.Minls_code = p.Minls_code
                                           JOIN Staff s ON s.Staff_id = p.Staff_id
                                           ORDER BY p.Issue_date DESC LIMIT 10";
                using var recentProdCmd = new NpgsqlCommand(recentProdQuery, conn);
                using var recentProdReader = recentProdCmd.ExecuteReader();
                while (recentProdReader.Read())
                {
                    RecentProduction.Add(new RecentProductionItem
                    {
                        Issue_date = Convert.ToDateTime(recentProdReader["Issue_date"]),
                        MineralType = recentProdReader["MineralType"].ToString() ?? "",
                        Quantity_mined = Convert.ToDouble(recentProdReader["Quantity_mined"]),
                        StaffName = recentProdReader["StaffName"].ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Message = "Error loading reports: " + ex.Message;
            }
        }
    }

    public class SalesByMineralItem
    {
        public string MineralType { get; set; } = string.Empty;
        public double Quantity { get; set; }
    }

    public class RecentSaleItem
    {
        public DateTime Sales_date { get; set; }
        public string MineralType { get; set; } = string.Empty;
        public double Quantity_sold { get; set; }
        public string CounterName { get; set; } = string.Empty;
    }

    public class RecentProductionItem
    {
        public DateTime Issue_date { get; set; }
        public string MineralType { get; set; } = string.Empty;
        public double Quantity_mined { get; set; }
        public string StaffName { get; set; } = string.Empty;
    }
}