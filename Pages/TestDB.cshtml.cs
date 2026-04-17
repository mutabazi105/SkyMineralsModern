using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;

namespace SkyMineralsModern.Pages
{
    public class TestDBModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public bool IsConnected { get; set; }
        public string? Database { get; set; }
        public string? ServerVersion { get; set; }
        public string? ErrorMessage { get; set; }

        public TestDBModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            try
            {
                string? connString = _configuration.GetConnectionString("PostgresConnection");

                if (string.IsNullOrEmpty(connString))
                {
                    IsConnected = false;
                    ErrorMessage = "Connection string not found";
                    return;
                }

                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                IsConnected = true;
                Database = conn.Database;
                ServerVersion = conn.PostgreSqlVersion.ToString();
            }
            catch (Exception ex)
            {
                IsConnected = false;
                ErrorMessage = ex.Message;
            }
        }
    }
}
