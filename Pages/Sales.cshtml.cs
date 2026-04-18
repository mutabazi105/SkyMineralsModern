using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;

namespace SkyMineralsModern.Pages
{
    public class SalesModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public List<MineralItem> MineralsList { get; set; } = new List<MineralItem>();
        public List<CounterItem> CountersList { get; set; } = new List<CounterItem>();
        public List<SaleItem> SalesList { get; set; } = new List<SaleItem>();
        public string Message { get; set; } = string.Empty;

        [BindProperty]
        public NewSaleItem NewSale { get; set; } = new NewSaleItem();

        public SalesModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            LoadMinerals();
            LoadCounters();
            LoadSales();
        }

        public IActionResult OnPost()
        {
            try
            {
                string? connString = _configuration.GetConnectionString("PostgresConnection");

                if (string.IsNullOrEmpty(connString))
                {
                    Message = "Connection string not configured";
                    LoadMinerals();
                    LoadCounters();
                    LoadSales();
                    return Page();
                }

                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                string salesId = GenerateSalesId(conn);
                string staffId = HttpContext.Session.GetString("StaffId") ?? "SKY001";

                string query = @"INSERT INTO Minerals_sold (Sales_id, Tag_id, Tantale, Quantity_sold, Counter_code, Staff_id, Sales_date) 
                                VALUES (@Sales_id, 'TEMP001', @Tantale, @Quantity_sold, @Counter_code, @Staff_id, NOW())";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Sales_id", salesId);
                cmd.Parameters.AddWithValue("@Tantale", NewSale.Tantale);
                cmd.Parameters.AddWithValue("@Quantity_sold", NewSale.Quantity_sold);
                cmd.Parameters.AddWithValue("@Counter_code", NewSale.Counter_code);
                cmd.Parameters.AddWithValue("@Staff_id", staffId);

                cmd.ExecuteNonQuery();

                Message = "Sale recorded successfully!";
                NewSale = new NewSaleItem();
                LoadSales();
            }
            catch (Exception ex)
            {
                Message = "Error: " + ex.Message;
            }

            LoadMinerals();
            LoadCounters();
            return Page();
        }

        private void LoadMinerals()
        {
            MineralsList.Clear();
            try
            {
                string? connString = _configuration.GetConnectionString("PostgresConnection");
                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                string query = "SELECT Minls_code, Type FROM Minerals ORDER BY Type";
                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    MineralsList.Add(new MineralItem
                    {
                        Minls_code = Convert.ToInt32(reader["Minls_code"]),
                        Type = reader["Type"].ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Message = "Error loading minerals: " + ex.Message;
            }
        }

        private void LoadCounters()
        {
            CountersList.Clear();
            try
            {
                string? connString = _configuration.GetConnectionString("PostgresConnection");
                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                string query = "SELECT Counter_code, Name FROM Counter";
                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    CountersList.Add(new CounterItem
                    {
                        Counter_code = reader["Counter_code"].ToString() ?? "",
                        Name = reader["Name"].ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Message = "Error loading counters: " + ex.Message;
            }
        }

        private void LoadSales()
        {
            SalesList.Clear();
            try
            {
                string? connString = _configuration.GetConnectionString("PostgresConnection");
                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                string query = @"SELECT s.Sales_id, s.Quantity_sold, s.Tantale, s.Sales_date, 
                                m.Type as MineralType, c.Name as CounterName
                                FROM Minerals_sold s
                                JOIN Minerals m ON m.Minls_code = 1
                                JOIN Counter c ON c.Counter_code = s.Counter_code
                                ORDER BY s.Sales_date DESC LIMIT 50";

                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    SalesList.Add(new SaleItem
                    {
                        Sales_id = reader["Sales_id"].ToString() ?? "",
                        MineralType = reader["MineralType"].ToString() ?? "",
                        Quantity_sold = Convert.ToDouble(reader["Quantity_sold"]),
                        Tantale = Convert.ToDouble(reader["Tantale"]),
                        CounterName = reader["CounterName"].ToString() ?? "",
                        Sales_date = Convert.ToDateTime(reader["Sales_date"])
                    });
                }
            }
            catch (Exception ex)
            {
                Message = "Error loading sales: " + ex.Message;
            }
        }

        private string GenerateSalesId(NpgsqlConnection conn)
        {
            string query = "SELECT COALESCE(MAX(CAST(SUBSTRING(Sales_id, 4) AS INTEGER)), 0) FROM Minerals_sold";
            using var cmd = new NpgsqlCommand(query, conn);
            var result = cmd.ExecuteScalar();
            int nextId = (result == DBNull.Value ? 0 : Convert.ToInt32(result)) + 1;
            return $"SAL{nextId:D3}";
        }
    }

    // Classes specific to Sales page
    public class MineralItem
    {
        public int Minls_code { get; set; }
        public string Type { get; set; } = string.Empty;
    }

    public class CounterItem
    {
        public string Counter_code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class SaleItem
    {
        public string Sales_id { get; set; } = string.Empty;
        public string MineralType { get; set; } = string.Empty;
        public double Quantity_sold { get; set; }
        public double Tantale { get; set; }
        public string CounterName { get; set; } = string.Empty;
        public DateTime Sales_date { get; set; }
    }

    public class NewSaleItem
    {
        public int Minls_code { get; set; }
        public double Quantity_sold { get; set; }
        public double Tantale { get; set; }
        public string Counter_code { get; set; } = string.Empty;
    }
}