using System;
using Npgsql;
using DotNetEnv;

namespace backendCsharp.Data {

    public class UsuarioData {

        public void ConsultarUsuarios() {

            Env.Load("../.env");

            string dbHost = Environment.GetEnvironmentVariable("DATABASE_HOST") ?? "localhost";
            string dbUser = Environment.GetEnvironmentVariable("DATABASE_USER") ?? "postgres";
            string dbPassword = Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? " ";
            string dbName = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "nodotdb";

            string connectionString = $"Host={dbHost};Username={dbUser};Password={dbPassword};Database={dbName}";

            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            string sql = "SELECT * FROM users";
            using var cmd = new NpgsqlCommand(sql, connection);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                // Processar os resultados da consulta aqui
                Console.WriteLine(reader["users"]);
            }

            connection.Close();
        }
    }
}