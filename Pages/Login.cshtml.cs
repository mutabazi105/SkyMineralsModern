using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;

namespace SkyMineralsModern.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _configuration;

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public LoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            try
            {
                string? connString = _configuration.GetConnectionString("PostgresConnection");

                if (string.IsNullOrEmpty(connString))
                {
                    Message = "Connection string not configured";
                    return Page();
                }

                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                string query = "SELECT Staff_id, Role FROM Staff WHERE Username = @username AND Password = @password AND Lock_unlock = 'unlock'";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", Username);
                cmd.Parameters.AddWithValue("@password", Password);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string staffId = reader["Staff_id"].ToString() ?? "";
                    string role = reader["Role"].ToString() ?? "";

                    HttpContext.Session.SetString("StaffId", staffId);
                    HttpContext.Session.SetString("Role", role);

                    // Redirect based on role
                    if (role == "Administrator")
                    {
                        return RedirectToPage("/Homeadmin");
                    }
                    else if (role == "Manager")
                    {
                        return RedirectToPage("/Manager");
                    }
                    else
                    {
                        return RedirectToPage("/Index");
                    }
                }
                else
                {
                    Message = "Invalid username or password";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                Message = "Error: " + ex.Message;
                return Page();
            }
        }
    }
}