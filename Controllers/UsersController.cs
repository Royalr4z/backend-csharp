using Microsoft.AspNetCore.Mvc;
using backendCsharp.Models;
using backendCsharp.Config;
using Newtonsoft.Json;
using System;
using Npgsql;

namespace backendCsharp.Controllers {

    [Route("[controller]")]
    [ApiController]

    public class UsersController : ControllerBase {

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

                return Unauthorized("Unauthorized");
            }

        }

        [HttpGet("{id}")]
        public ActionResult<List<UserModel>> GetById(int id) {

            return Ok(ConsultarUsuarios(id));
        }

        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] dynamic dadosObtidos) {

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

            try {

                if (validator.ValidatingToken(token ?? "", true)) {

                    return Ok(AtualizandoUsuario(id, dadosObtidos));
                } else {

                    return Unauthorized("Unauthorized");
                }
            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }

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

                    return Unauthorized("Unauthorized");
                }

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        private List<UserModel> ConsultarUsuarios(int? id) {

            Enviro env = new Enviro();

            using var connection = new NpgsqlConnection(env.ObtendoConfig());
            connection.Open();

            string sql = "";

            if (id > 0) {
                sql = $"SELECT id, name, email, admin FROM users WHERE id = {id}";
            } else {
                sql = "SELECT id, name, email, admin FROM users;";
            }

            using var cmd = new NpgsqlCommand(sql, connection);

            var users = new List<UserModel>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                var user = new UserModel();

                if (id == 0) {
                    user = new UserModel {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Email = reader.GetString(reader.GetOrdinal("email")),
                        Admin = reader.GetBoolean(reader.GetOrdinal("admin"))
                    };
                } else {
                    user = new UserModel {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Name = reader.GetString(reader.GetOrdinal("name"))
                    };
                }
                users.Add(user);
            }

            connection.Close();

            return users;
        }

        private IActionResult AtualizandoUsuario(int id, dynamic dadosObtidos) {

            static string EncryptPassword(string password) {
                // Gera o salt com custo (work factor) 10
                string salt = BCrypt.Net.BCrypt.GenerateSalt(10);

                // Gera o hash da senha usando o salt
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, salt);

                return hashedPassword;
            }

            Validate validator = new Validate();
            Enviro env = new Enviro();

            // Convertendo os Dados Obtidos para JSON
            string jsonString = System.Text.Json.JsonSerializer.Serialize(dadosObtidos);
            UserModel? dados = JsonConvert.DeserializeObject<UserModel>(jsonString);

            string nome = dados?.Name ?? "";
            string email = dados?.Email ?? "";
            string password = dados?.Password ?? "";
            string confirmPassword = dados?.ConfirmPassword ?? "";
            bool admin = dados?.Admin ?? false;

            validator.existsOrError(nome, @"Nome não informado");
            validator.existsOrError(email, @"E-mail não informado");
            validator.existsOrError(password, @"Senha não informada");
            if (password.Length < 8) throw new Exception("Senha muito curta");
            validator.existsOrError(confirmPassword, @"Confirme sua Senha");

            validator.equalsOrError(password, confirmPassword, @"Senhas não conferem");

            validator.ValidateEmail(email, @"E-mail Inválido!");

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
                connection.Open();

                string sql = "SELECT id FROM users WHERE id = @Id LIMIT 1;";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection)) {
                    cmd.Parameters.AddWithValue("@Id", id);

                    // Executar a consulta e obter o número de itens retornados
                    int count = Convert.ToInt32(cmd.ExecuteScalar());

                    if (count == 0) throw new Exception("Usuário Inexistente");

                }
                connection.Close();
            }

            password = EncryptPassword(password);

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
                connection.Open();

                string query = "UPDATE users SET name = @Name, email = @Email, password = @Password, admin = @Admin WHERE id = @Id";

                // Crie um comando SQL com a query e a conexão
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                    command.Parameters.AddWithValue("@Name", nome);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Password", password);
                    command.Parameters.AddWithValue("@Admin", admin);
                    command.Parameters.AddWithValue("@Id", id);

                    // Execute o comando
                    int rowsAffected = command.ExecuteNonQuery();

                    connection.Close();

                    // Verifique se alguma linha foi afetada (deve ser maior que 0)
                    if (rowsAffected > 0) {
                        return null;
                    } else {
                        return StatusCode(500, "Falha ao Atualizar os dados.");
                    }
                }
            }
        }

        private void DeletarUsuario(int id) {

            Enviro env = new Enviro();

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
                connection.Open();
                string query = $"SELECT * FROM blogs WHERE userid = {id}";

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
    }
}
