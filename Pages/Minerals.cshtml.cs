using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;

namespace SkyMineralsModern.Pages
{
    public class MineralsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public List<Mineral> MineralsList { get; set; } = new List<Mineral>();
        public string Message { get; set; } = string.Empty;

        [BindProperty]
        public NewMineral NewMineral { get; set; } = new NewMineral();

        public MineralsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            LoadMinerals();
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
                    return Page();
                }

                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                string query = "INSERT INTO Minerals (Type) VALUES (@Type)";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Type", NewMineral.Type);

                cmd.ExecuteNonQuery();

                Message = "Mineral added successfully!";
                NewMineral = new NewMineral();
                LoadMinerals();
            }
            catch (Exception ex)
            {
                Message = "Error: " + ex.Message;
            }

            return Page();
        }

        public IActionResult OnPostDelete(int id)
        {
            try
            {
                string? connString = _configuration.GetConnectionString("PostgresConnection");

                if (string.IsNullOrEmpty(connString))
                {
                    Message = "Connection string not configured";
                    LoadMinerals();
                    return Page();
                }

                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                string query = "DELETE FROM Minerals WHERE Minls_code = @id";
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

                Message = "Mineral deleted successfully!";
                LoadMinerals();
            }
            catch (Exception ex)
            {
                Message = "Error deleting mineral: " + ex.Message;
                LoadMinerals();
            }

            return Page();
        }

        private void LoadMinerals()
        {
            MineralsList.Clear();
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

                string query = "SELECT Minls_code, Type FROM Minerals ORDER BY Minls_code";

                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    MineralsList.Add(new Mineral
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
    }

    public class Mineral
    {
        public int Minls_code { get; set; }
        public string Type { get; set; } = string.Empty;
    }

    public class NewMineral
    {
        public string Type { get; set; } = string.Empty;
    }
}