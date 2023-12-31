using Microsoft.AspNetCore.Mvc;
using backendCsharp.Models;
using System;
using Npgsql;

namespace backendCsharp.Controllers {

    [Route("[controller]")]
    [ApiController]

    public class UsersController : ControllerBase {

        public List<UserModel> ConsultarUsuarios(int? id) {

            Validate validator = new Validate();

            using var connection = new NpgsqlConnection(validator.ObtendoConfig());
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

            Validate validator = new Validate();

            using (NpgsqlConnection  connection = new NpgsqlConnection(validator.ObtendoConfig())) {
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

            using (NpgsqlConnection  connection = new NpgsqlConnection(validator.ObtendoConfig())) {
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

            return Ok(ConsultarUsuarios(0));
        }

        [HttpGet("{id}")]
        public ActionResult<List<UserModel>> GetById(int id) {

            return Ok(ConsultarUsuarios(id));
        }

        [HttpDelete("{id}")]
        public ActionResult<List<MessageModel>> Delete(int id) {

            try {
                DeletarUsuario(id);
                return Ok();

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }
    }
}