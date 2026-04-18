using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;

namespace SkyMineralsModern.Pages
{
    public class TeamModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public List<StaffInfo> StaffList { get; set; } = new List<StaffInfo>();
        public List<TeamInfo> TeamsList { get; set; } = new List<TeamInfo>();
        public string Message { get; set; } = string.Empty;

        [BindProperty]
        public NewTeamInfo NewTeam { get; set; } = new NewTeamInfo();

        public TeamModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            LoadStaff();
            LoadTeams();
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
                    LoadTeams();
                    return Page();
                }

                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                string teamId = GenerateTeamId(conn);

                string query = @"INSERT INTO Team1 (Team_id, Team_Name, Staff_id) 
                                VALUES (@Team_id, @Team_Name, @Staff_id)";

                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Team_id", teamId);
                cmd.Parameters.AddWithValue("@Team_Name", NewTeam.Team_Name);
                cmd.Parameters.AddWithValue("@Staff_id", NewTeam.Staff_id);

                cmd.ExecuteNonQuery();

                Message = "Team created successfully!";
                NewTeam = new NewTeamInfo();
                LoadTeams();
            }
            catch (Exception ex)
            {
                Message = "Error: " + ex.Message;
            }

            LoadStaff();
            return Page();
        }

        public IActionResult OnPostDelete(string id)
        {
            try
            {
                string? connString = _configuration.GetConnectionString("PostgresConnection");
                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                string query = "DELETE FROM Team1 WHERE Team_id = @id";
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

                Message = "Team deleted successfully!";
                LoadTeams();
            }
            catch (Exception ex)
            {
                Message = "Error deleting team: " + ex.Message;
                LoadTeams();
            }

            LoadStaff();
            return Page();
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
                    StaffList.Add(new StaffInfo
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

        private void LoadTeams()
        {
            TeamsList.Clear();
            try
            {
                string? connString = _configuration.GetConnectionString("PostgresConnection");
                using var conn = new NpgsqlConnection(connString);
                conn.Open();

                string query = @"SELECT t.Team_id, t.Team_Name, t.Staff_id,
                                CONCAT(s.Fname, ' ', s.Lname) as LeaderName
                                FROM Team1 t
                                JOIN Staff s ON s.Staff_id = t.Staff_id
                                ORDER BY t.Team_id";

                using var cmd = new NpgsqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    TeamsList.Add(new TeamInfo
                    {
                        Team_id = reader["Team_id"].ToString() ?? "",
                        Team_Name = reader["Team_Name"].ToString() ?? "",
                        LeaderName = reader["LeaderName"].ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                Message = "Error loading teams: " + ex.Message;
            }
        }

        private string GenerateTeamId(NpgsqlConnection conn)
        {
            string query = "SELECT COALESCE(MAX(CAST(SUBSTRING(Team_id, 4) AS INTEGER)), 0) FROM Team1";
            using var cmd = new NpgsqlCommand(query, conn);
            var result = cmd.ExecuteScalar();
            int nextId = (result == DBNull.Value ? 0 : Convert.ToInt32(result)) + 1;
            return $"TM{nextId:D3}";
        }
    }

    public class StaffInfo
    {
        public string Staff_id { get; set; } = string.Empty;
        public string Fname { get; set; } = string.Empty;
        public string Lname { get; set; } = string.Empty;
    }

    public class TeamInfo
    {
        public string Team_id { get; set; } = string.Empty;
        public string Team_Name { get; set; } = string.Empty;
        public string LeaderName { get; set; } = string.Empty;
    }

    public class NewTeamInfo
    {
        public string Team_Name { get; set; } = string.Empty;
        public string Staff_id { get; set; } = string.Empty;
    }
}