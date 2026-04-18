using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;

namespace SkyMineralsModern.Pages
{
    public class TaggedModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public List<TagItem> TagsList { get; set; } = new List<TagItem>();
        public string Message { get; set; } = string.Empty;

        [BindProperty]
        public NewTagItem NewTag { get; set; } = new NewTagItem();

        public TaggedModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            LoadTags();
        }

        public IActionResult OnPost()
        {
            try
            {
                string? connString = _configuration.GetConnectionString("PostgresConnection");

                if (string.IsNullOrEmpty(connString))
                {
                    Message = "Connection string not configured";
                    LoadTags();
                    return Page();
                }

                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                string tagId = GenerateTagId(conn);

                string query = @"INSERT INTO Tagged (Tag_id, Tag_number, Op_code, Quantity_tagged, Tag_date, Status) 
                                VALUES (@Tag_id, @Tag_number, 'OP001', @Quantity_tagged, NOW(), 'not sold')";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Tag_id", tagId);
                cmd.Parameters.AddWithValue("@Tag_number", NewTag.Tag_number);
                cmd.Parameters.AddWithValue("@Quantity_tagged", NewTag.Quantity_tagged);

                cmd.ExecuteNonQuery();

                Message = "Tag added successfully!";
                NewTag = new NewTagItem();
                LoadTags();
            }
            catch (Exception ex)
            {
                Message = "Error: " + ex.Message;
            }

            return Page();
        }

        private void LoadTags()
        {
            TagsList.Clear();
            try
            {
                string? connString = _configuration.GetConnectionString("PostgresConnection");
                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                string query = "SELECT Tag_id, Tag_number, Quantity_tagged, Tag_date, Status FROM Tagged ORDER BY Tag_date DESC";
                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    TagsList.Add(new TagItem
                    {
                        Tag_id = reader["Tag_id"].ToString() ?? "",
                        Tag_number = reader["Tag_number"].ToString() ?? "",
                        Quantity_tagged = Convert.ToDouble(reader["Quantity_tagged"]),
                        Tag_date = Convert.ToDateTime(reader["Tag_date"]),
                        Status = reader["Status"].ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Message = "Error loading tags: " + ex.Message;
            }
        }

        private string GenerateTagId(NpgsqlConnection conn)
        {
            string query = "SELECT COALESCE(MAX(CAST(SUBSTRING(Tag_id, 4) AS INTEGER)), 0) FROM Tagged";
            using var cmd = new NpgsqlCommand(query, conn);
            var result = cmd.ExecuteScalar();
            int nextId = (result == DBNull.Value ? 0 : Convert.ToInt32(result)) + 1;
            return $"TAG{nextId:D3}";
        }
    }

    public class TagItem
    {
        public string Tag_id { get; set; } = string.Empty;
        public string Tag_number { get; set; } = string.Empty;
        public double Quantity_tagged { get; set; }
        public DateTime Tag_date { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class NewTagItem
    {
        public string Tag_number { get; set; } = string.Empty;
        public double Quantity_tagged { get; set; }
    }
}