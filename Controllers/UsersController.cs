using Microsoft.AspNetCore.Mvc;
using backendCsharp.Models;
using backendCsharp.Config;
using System;
using Npgsql;

namespace backendCsharp.Controllers {

    [Route("[controller]")]
    [ApiController]

    public class UsersController : ControllerBase {

        public List<UserModel> ConsultarUsuarios(int? id) {

            Enviro env = new Enviro();

            using var connection = new NpgsqlConnection(env.ObtendoConfig());
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

        public void DeletarUsuario(int id) {

            Enviro env = new Enviro();

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
                connection.Open();
                string query = $"""SELECT * FROM blogs WHERE "userId" = {id}""";

                // Crie um comando SQL com a query e a conexão
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                    // Execute o comando
                    int rowsAffected = command.ExecuteNonQuery();
                    using var reader = command.ExecuteReader();

                    // Verifique se alguma linha foi afetada (deve ser maior que 0)
                    if (reader.Read()) {
                        throw new Exception("Usuário possui Blogs.");
                    }
                }

                connection.Close();
            }

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
                connection.Open();
                string query = $"DELETE FROM users WHERE id = {id}";

                // Crie um comando SQL com a query e a conexão
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                    // Execute o comando
                    int rowsAffected = command.ExecuteNonQuery();

                    // Verifique se alguma linha foi afetada (deve ser maior que 0)
                    if (rowsAffected == 0) {
                        throw new Exception("Usuário não foi encontrado.");
                    }
                }

                connection.Close();
            }

        }

        [HttpGet]
        public ActionResult<List<UserModel>> Get() {

            // Obter o cabeçalho "Authorization" da solicitação
            string? authorizationHeader = HttpContext.Request.Headers?["Authorization"];

            string token = "";

            try {
                // Remover o prefixo "Bearer " para obter apenas o token
                token = authorizationHeader?.Substring("Bearer ".Length).Trim() ?? "";
            } catch {
                token = authorizationHeader ?? ""; 
            }

            Validate validator = new Validate();

            if (validator.ValidatingToken(token ?? "", true)) {

                return Ok(ConsultarUsuarios(0));
            } else {

                return Ok("Unauthorized");
            }

        }

        [HttpGet("{id}")]
        public ActionResult<List<UserModel>> GetById(int id) {

            return Ok(ConsultarUsuarios(id));
        }

        [HttpDelete("{id}")]
        public ActionResult<List<MessageModel>> Delete(int id) {

            // Obter o cabeçalho "Authorization" da solicitação
            string? authorizationHeader = HttpContext.Request.Headers["Authorization"];

            string token = "";

            try {
                // Remover o prefixo "Bearer " para obter apenas o token
                token = authorizationHeader?.Substring("Bearer ".Length).Trim() ?? "";
            } catch {
                token = authorizationHeader ?? ""; 
            }

            Validate validator = new Validate();

            try {

                if (validator.ValidatingToken(token ?? "", true)) {

                    DeletarUsuario(id);
                    return Ok();
                } else {

                    return Ok("Unauthorized");
                }

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }
    }
}