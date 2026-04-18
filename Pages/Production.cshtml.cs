using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;

namespace SkyMineralsModern.Pages
{
    public class ProductionModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public List<MineralOption> MineralsList { get; set; } = new List<MineralOption>();
        public List<StaffOption> StaffList { get; set; } = new List<StaffOption>();
        public List<ProductionRecord> ProductionList { get; set; } = new List<ProductionRecord>();
        public string Message { get; set; } = string.Empty;

        [BindProperty]
        public NewProductionRecord NewProduction { get; set; } = new NewProductionRecord();

        public ProductionModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            LoadMinerals();
            LoadStaff();
            LoadProduction();
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
                    LoadStaff();
                    LoadProduction();
                    return Page();
                }

                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                string opCode = GenerateOpCode(conn);

                string query = @"INSERT INTO Productivity (Op_code, Minls_code, Staff_id, Quantity_mined, Issue_date, Status) 
                                VALUES (@Op_code, @Minls_code, @Staff_id, @Quantity_mined, NOW(), @Status)";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Op_code", opCode);
                cmd.Parameters.AddWithValue("@Minls_code", NewProduction.Minls_code);
                cmd.Parameters.AddWithValue("@Staff_id", NewProduction.Staff_id);
                cmd.Parameters.AddWithValue("@Quantity_mined", NewProduction.Quantity_mined);
                cmd.Parameters.AddWithValue("@Status", NewProduction.Status);

                cmd.ExecuteNonQuery();

                Message = "Production recorded successfully!";
                NewProduction = new NewProductionRecord();
                LoadProduction();
            }
            catch (Exception ex)
            {
                Message = "Error: " + ex.Message;
            }

            LoadMinerals();
            LoadStaff();
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
                    MineralsList.Add(new MineralOption
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

        private void LoadStaff()
        {
            StaffList.Clear();
            try
            {
                string? connString = _configuration.GetConnectionString("PostgresConnection");
                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                string query = "SELECT Staff_id, Fname, Lname FROM Staff ORDER BY Fname";
                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    StaffList.Add(new StaffOption
                    {
                        Staff_id = reader["Staff_id"].ToString() ?? "",
                        Fname = reader["Fname"].ToString() ?? "",
                        Lname = reader["Lname"].ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Message = "Error loading staff: " + ex.Message;
            }
        }

        private void LoadProduction()
        {
            ProductionList.Clear();
            try
            {
                string? connString = _configuration.GetConnectionString("PostgresConnection");
                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                string query = @"SELECT p.Op_code, p.Quantity_mined, p.Issue_date, p.Status,
                                m.Type as MineralType, CONCAT(s.Fname, ' ', s.Lname) as StaffName
                                FROM Productivity p
                                JOIN Minerals m ON m.Minls_code = p.Minls_code
                                JOIN Staff s ON s.Staff_id = p.Staff_id
                                ORDER BY p.Issue_date DESC";

                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    ProductionList.Add(new ProductionRecord
                    {
                        Op_code = reader["Op_code"].ToString() ?? "",
                        MineralType = reader["MineralType"].ToString() ?? "",
                        StaffName = reader["StaffName"].ToString() ?? "",
                        Quantity_mined = Convert.ToDouble(reader["Quantity_mined"]),
                        Issue_date = Convert.ToDateTime(reader["Issue_date"]),
                        Status = reader["Status"].ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Message = "Error loading production: " + ex.Message;
            }
        }

        private string GenerateOpCode(NpgsqlConnection conn)
        {
            string query = "SELECT COALESCE(MAX(CAST(SUBSTRING(Op_code, 3) AS INTEGER)), 0) FROM Productivity";
            using var cmd = new NpgsqlCommand(query, conn);
            var result = cmd.ExecuteScalar();
            int nextId = (result == DBNull.Value ? 0 : Convert.ToInt32(result)) + 1;
            return $"OP{nextId:D3}";
        }
    }

    // Classes specific to Production page
    public class MineralOption
    {
        public int Minls_code { get; set; }
        public string Type { get; set; } = string.Empty;
    }

    public class StaffOption
    {
        public string Staff_id { get; set; } = string.Empty;
        public string Fname { get; set; } = string.Empty;
        public string Lname { get; set; } = string.Empty;
    }

    public class ProductionRecord
    {
        public string Op_code { get; set; } = string.Empty;
        public string MineralType { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public double Quantity_mined { get; set; }
        public DateTime Issue_date { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class NewProductionRecord
    {
        public int Minls_code { get; set; }
        public string Staff_id { get; set; } = string.Empty;
        public double Quantity_mined { get; set; }
        public string Status { get; set; } = "untagged";
    }
}