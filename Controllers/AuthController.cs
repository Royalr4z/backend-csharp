using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using backendCsharp.Models;
using backendCsharp.Config;
using Newtonsoft.Json;
using System.Text;
using DotNetEnv;
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
            environment env = new environment();

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

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
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

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
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

        public string authSecret() {

            Env.Load("./.env");

            string AUTH_SECRET = Environment.GetEnvironmentVariable("AUTH_SECRET") ?? "";
        
            return AUTH_SECRET;
        }

        public List<UserModel> Login(dynamic dadosObtidos) {

            Validate validator = new Validate();
            environment env = new environment();

            // Convertendo os Dados Obtidos para JSON
            string jsonString = System.Text.Json.JsonSerializer.Serialize(dadosObtidos);
            UserModel? dados = JsonConvert.DeserializeObject<UserModel>(jsonString);

            string email = dados?.Email ?? "";
            string password = dados?.Password ?? "";

            validator.existsOrError(email, @"E-mail não informado");
            validator.existsOrError(password, @"Senha não informada");

            validator.ValidateEmail(email, @"E-mail Inválido!");

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
                connection.Open();

                string sql = "SELECT * FROM users WHERE email = @Email LIMIT 1;";

                using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection)) {
                    cmd.Parameters.AddWithValue("@Email", email);

                    var users = new List<UserModel>();

                    // Obtém a representação do tempo atual em segundos desde a época de (1 de janeiro de 1970 00:00:00 UTC)
                    long now = DateTimeOffset.Now.ToUnixTimeSeconds();

                    using var reader = cmd.ExecuteReader();
                    if (!reader.HasRows) throw new Exception("Usuário inexistente");

                    while (reader.Read()) {
                        var user = new UserModel {
                            Id = reader.GetInt32(reader.GetOrdinal("id")),
                            Name = reader.GetString(reader.GetOrdinal("name")),
                            Email = reader.GetString(reader.GetOrdinal("email")),
                            Password = reader.GetString(reader.GetOrdinal("password")),
                            Admin = reader.GetBoolean(reader.GetOrdinal("admin")),
                            iat = now,
                            exp = now + (60 * 60 * 24 * 3)
                        };

                        // Comparando as senhas
                        bool isMatch = BCrypt.Net.BCrypt.Verify(password, user.Password);
                        if (!isMatch) throw new Exception("Senha incorreta");

                        if (!user.Admin) throw new Exception("Acesso negado");

                        // Remover a senha definindo-a como null
                        user.Password = null;

                        // Crie as claims para o token JWT
                        var claims = new[] {
                            new Claim("id", user.Id.ToString() ?? ""),
                            new Claim("name", user.Name ?? ""),
                            new Claim("email", user.Email ?? ""),
                            new Claim("admin", user.Admin.ToString() ?? ""),
                            new Claim("iat", user.iat.ToString() ?? ""),
                            new Claim("exp", user.exp.ToString() ?? "")
                        };

                        // Crie a chave secreta para assinar o token
                        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authSecret()));

                        // Crie as credenciais do token
                        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                        // Crie o token JWT
                        var token = new JwtSecurityToken(
                            claims: claims,
                            expires: DateTime.UtcNow.Add(TimeSpan.FromSeconds((double)(user.exp ?? 0))),
                            signingCredentials: creds
                        );

                        // Obtenha a representação do token como uma string
                        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                        user.Token = tokenString;

                        users.Add(user);

                    }

                    connection.Close();

                    return users;
                }
            }
        }


        [HttpPost]
        public IActionResult Post([FromBody] dynamic dadosObtidos) {

            try {
                return Ok(Login(dadosObtidos));

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }

        }
    }

    [Route("[controller]")]
    [ApiController]
    
    public class ValidateTokenController : ControllerBase {

        public bool ValidatingToken(dynamic dadosObtidos) {

            Env.Load("./.env");

            string AUTH_SECRET = Environment.GetEnvironmentVariable("AUTH_SECRET") ?? "";

            // Convertendo os Dados Obtidos para JSON
            string jsonString = System.Text.Json.JsonSerializer.Serialize(dadosObtidos);
            UserModel? dados = JsonConvert.DeserializeObject<UserModel>(jsonString);

            Validate validator = new Validate();

            string token = dados?.Token ?? "";

            validator.existsOrError(token, @"Token não informado");

            try {

                var tokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(AUTH_SECRET))
                };

                // Decodificar e validar o token JWT
                var tokenHandler = new JwtSecurityTokenHandler();
                SecurityToken securityToken;

                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);

                // Verificar se o token expirou
                if (securityToken is JwtSecurityToken jwtSecurityToken) {
                    var expirationDate = jwtSecurityToken.ValidTo;
                    var currentDate = DateTime.UtcNow;

                    if (expirationDate < currentDate) {
                        // Token expirou
                        return false;
                    }
                }

                return true; // o token é válido
    
            } catch {

                return false; // o token é inválido
            }
        }
    
        [HttpPost]
        public IActionResult Post([FromBody] dynamic dadosObtidos) {

            try {
                return Ok(ValidatingToken(dadosObtidos));

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }
    }
}