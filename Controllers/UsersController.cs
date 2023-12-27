using Microsoft.AspNetCore.Mvc;
using backendCsharp.Models;
using System;
using Npgsql;
using DotNetEnv;

namespace backendCsharp.Controllers {

    [Route("[controller]")]
    [ApiController]

    public class UsersController : ControllerBase {

        private string ObtendoConfig() {

            Env.Load("./.env");

            string dbHost = Environment.GetEnvironmentVariable("DATABASE_HOST") ?? "";
            string dbUser = Environment.GetEnvironmentVariable("DATABASE_USER") ?? "";
            string dbPassword = Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? "";
            string dbName = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "";

            string connectionString = $"Host={dbHost};Username={dbUser};Password={dbPassword};Database={dbName}";
        
            return connectionString;
        }

        public List<UserModel> ConsultarUsuarios(int? id) {

            using var connection = new NpgsqlConnection(ObtendoConfig());
            connection.Open();

            string sql = "";

            if (id > 0) {
                sql = $"SELECT * FROM users WHERE id = {id}";
            } else {
                sql = "SELECT * FROM users";
            }

            using var cmd = new NpgsqlCommand(sql, connection);

            var users = new List<UserModel>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                var user = new UserModel {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Admin = reader.GetBoolean(reader.GetOrdinal("admin"))
                };
                users.Add(user);
            }

            connection.Close();

            return users;
        }

        [HttpGet]
        public ActionResult<List<UserModel>> Get() {

            return Ok(ConsultarUsuarios(0));
        }

        [HttpGet("{id}")]
        public ActionResult<List<UserModel>> GetById(int id) {

            return Ok(ConsultarUsuarios(id));
        }
    }
}