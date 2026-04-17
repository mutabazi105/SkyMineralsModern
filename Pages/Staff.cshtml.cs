using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;

namespace SkyMineralsModern.Pages
{
    public class StaffModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public List<Staff> StaffList { get; set; } = new List<Staff>();
        public string Message { get; set; } = string.Empty;

        [BindProperty]
        public NewStaff NewStaff { get; set; } = new NewStaff();

        public StaffModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            LoadStaff();
        }

        public IActionResult OnPost()
        {
            try
            {
                string? connString = _configuration.GetConnectionString("PostgresConnection");

                if (string.IsNullOrEmpty(connString))
                {
                    Message = "Connection string not configured";
                    LoadStaff();
                    return Page();
                }

                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                // Generate new Staff ID
                string newId = GenerateStaffId(conn);

                string query = @"INSERT INTO Staff (Staff_id, Fname, Lname, Email, Phone, Sex, Username, Password, Role, Lock_unlock, Starting_date) 
                                VALUES (@Staff_id, @Fname, @Lname, @Email, @Phone, @Sex, @Username, @Password, @Role, 'unlock', NOW())";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Staff_id", newId);
                cmd.Parameters.AddWithValue("@Fname", NewStaff.Fname);
                cmd.Parameters.AddWithValue("@Lname", NewStaff.Lname);
                cmd.Parameters.AddWithValue("@Email", NewStaff.Email);
                cmd.Parameters.AddWithValue("@Phone", NewStaff.Phone);
                cmd.Parameters.AddWithValue("@Sex", NewStaff.Sex);
                cmd.Parameters.AddWithValue("@Username", NewStaff.Username);
                cmd.Parameters.AddWithValue("@Password", NewStaff.Password);
                cmd.Parameters.AddWithValue("@Role", NewStaff.Role);

                cmd.ExecuteNonQuery();

                Message = "Staff added successfully!";
                NewStaff = new NewStaff();
                LoadStaff();
            }
            catch (Exception ex)
            {
                Message = "Error: " + ex.Message;
            }

            return Page();
        }

        public IActionResult OnPostDelete(string id)
        {
            try
            {
                string? connString = _configuration.GetConnectionString("PostgresConnection");

                if (string.IsNullOrEmpty(connString))
                {
                    Message = "Connection string not configured";
                    LoadStaff();
                    return Page();
                }

                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                string query = "DELETE FROM Staff WHERE Staff_id = @id";
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

                Message = "Staff deleted successfully!";
                LoadStaff();
            }
            catch (Exception ex)
            {
                Message = "Error deleting staff: " + ex.Message;
                LoadStaff();
            }

            return Page();
        }

        private void LoadStaff()
        {
            StaffList.Clear();
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

                string query = "SELECT Staff_id, Fname, Lname, Email, Phone, Username, Role FROM Staff ORDER BY Staff_id";

                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    StaffList.Add(new Staff
                    {
                        Staff_id = reader["Staff_id"].ToString() ?? "",
                        Fname = reader["Fname"].ToString() ?? "",
                        Lname = reader["Lname"].ToString() ?? "",
                        Email = reader["Email"].ToString() ?? "",
                        Phone = reader["Phone"].ToString() ?? "",
                        Username = reader["Username"].ToString() ?? "",
                        Role = reader["Role"].ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Message = "Error loading staff: " + ex.Message;
            }
        }

        private string GenerateStaffId(NpgsqlConnection conn)
        {
            string query = "SELECT MAX(CAST(SUBSTRING(Staff_id, 4) AS INTEGER)) FROM Staff";
            using var cmd = new NpgsqlCommand(query, conn);
            var result = cmd.ExecuteScalar();
            int nextId = (result == DBNull.Value ? 0 : Convert.ToInt32(result)) + 1;
            return $"SKY{nextId:D3}";
        }
    }

    public class Staff
    {
        public string Staff_id { get; set; } = string.Empty;
        public string Fname { get; set; } = string.Empty;
        public string Lname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class NewStaff
    {
        public string Fname { get; set; } = string.Empty;
        public string Lname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Sex { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}