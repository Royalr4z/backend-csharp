using Microsoft.AspNetCore.Mvc;
using backendCsharp.Models;
using System;
using Npgsql;
using DotNetEnv;

namespace backendCsharp.Controllers {

    [Route("[controller]")]
    [ApiController]

    public class UsersController : ControllerBase {

        public string ObtendoConfig() {

            Env.Load("./.env");

            string dbHost = Environment.GetEnvironmentVariable("DATABASE_HOST") ?? "localhost";
            string dbUser = Environment.GetEnvironmentVariable("DATABASE_USER") ?? "postgres";
            string dbPassword = Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? "";
            string dbName = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "nodotdb";

            string connectionString = $"Host={dbHost};Username={dbUser};Password={dbPassword};Database={dbName}";
        
            return connectionString;
        }

        public List<UserModel> ConsultarUsuarios() {

            using var connection = new NpgsqlConnection(ObtendoConfig());
            connection.Open();

            string sql = "SELECT * FROM users";
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
        public ActionResult<List<UserModel>> BuscarTodosUsuarios() {

            return Ok(ConsultarUsuarios());
        }
    }
}