using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using backendCsharp.Models;
using Newtonsoft.Json;
using BCrypt;
using Npgsql;

namespace backendCsharp.Controllers {

    [Route("[controller]")]
    [ApiController]

    public class SignupController : ControllerBase {

        public string EncryptPassword(string password) {
            // Gera o salt com custo (work factor) 10
            string salt = BCrypt.Net.BCrypt.GenerateSalt(10);

            // Gera o hash da senha usando o salt
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, salt);

            return hashedPassword;
        }

        public string InserindoUsuario(dynamic dadosObtidos) {

            Validate validator = new Validate();

            // Convertendo os Dados Obtidos para JSON
            string jsonString = System.Text.Json.JsonSerializer.Serialize(dadosObtidos);
            UserModel? dados = JsonConvert.DeserializeObject<UserModel>(jsonString);

            string nome = dados?.Name ?? "";
            string email = dados?.Email ?? "";
            string password = dados?.Password ?? "";
            string confirmPassword = dados?.ConfirmPassword ?? "";

            validator.existsOrError(nome, @"Nome não informado");
            validator.existsOrError(email, @"E-mail não informado");
            validator.existsOrError(password, @"Senha não informada");
            if (password.Length < 8) throw new Exception("Senha muito curta");
            validator.existsOrError(confirmPassword, @"Confirme sua Senha");

            validator.equalsOrError(password, confirmPassword, @"Senhas não conferem");

            validator.ValidateEmail(email, @"E-mail Inválido!");

            using (NpgsqlConnection  connection = new NpgsqlConnection(validator.ObtendoConfig())) {
                connection.Open();

                string sql = "SELECT * FROM users WHERE email = @Email LIMIT 1;";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection)) {
                    cmd.Parameters.AddWithValue("@Email", email);

                    var users = new List<UserModel>();

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read()) {
                        var user = new UserModel {
                            Email = reader.GetString(reader.GetOrdinal("email")),
                        };

                        validator.notExistsOrError(user.Email, "Usuário já cadastrado");
                        
                        users.Add(user);
                    }
                }
                connection.Close();
            }

            password = EncryptPassword(password);

            using (NpgsqlConnection  connection = new NpgsqlConnection(validator.ObtendoConfig())) {
                connection.Open();

                string query = "INSERT INTO users (name, email, password, admin) VALUES (@Name, @Email, @Password, @Admin)";

                // Crie um comando SQL com a query e a conexão
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                    command.Parameters.AddWithValue("@Name", nome);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Password", password);
                    command.Parameters.AddWithValue("@Admin", false);

                    // Execute o comando
                    int rowsAffected = command.ExecuteNonQuery();

                    connection.Close();

                    // Verifique se alguma linha foi afetada (deve ser maior que 0)
                    if (rowsAffected > 0) {
                        return "Dados inseridos com sucesso!";
                    } else {
                        return "Falha ao inserir dados.";
                    }
                }

            }
            
        }
    
        [HttpPost]
        public IActionResult Post([FromBody] dynamic dadosObtidos) {

            try {
                return Ok(InserindoUsuario(dadosObtidos));

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }
    }

    [Route("[controller]")]
    [ApiController]
    
    public class SigninController : ControllerBase {

        [HttpPost]
        public IActionResult Post([FromBody] dynamic dadosObtidos) {

            try {
                return Ok("signin");

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }

        }
    }

    [Route("[controller]")]
    [ApiController]
    
    public class ValidateTokenController : ControllerBase {
    
        [HttpPost]
        public IActionResult Post([FromBody] dynamic dadosObtidos) {

            try {
                return Ok("validadetoken");

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }
    }
}