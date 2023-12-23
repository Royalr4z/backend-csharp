using Microsoft.AspNetCore.Mvc;
using backendCsharp.Models;
using Newtonsoft.Json;
using System;
using Npgsql;
using DotNetEnv;

namespace backendCsharp.Controllers {

    [Route("[controller]")]
    [ApiController]

    public class MessageController : ControllerBase {

        private string ObtendoConfig() {

            Env.Load("./.env");

            string dbHost = Environment.GetEnvironmentVariable("DATABASE_HOST") ?? "";
            string dbUser = Environment.GetEnvironmentVariable("DATABASE_USER") ?? "";
            string dbPassword = Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? "";
            string dbName = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "";

            string connectionString = $"Host={dbHost};Username={dbUser};Password={dbPassword};Database={dbName}";
        
            return connectionString;
        }

        public List<MessageModel> ConsultarMessagens () {

            using var connection = new NpgsqlConnection(ObtendoConfig());
            connection.Open();

            string sql = "SELECT * FROM message";
            using var cmd = new NpgsqlCommand(sql, connection);

            var Messages = new List<MessageModel>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                var message = new MessageModel {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Date = reader.GetString(reader.GetOrdinal("date")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Subject = reader.GetString(reader.GetOrdinal("subject")),
                    Content = reader.GetString(reader.GetOrdinal("content"))

                };
                Messages.Add(message);
            }

            connection.Close();

            return Messages;
        }

        [HttpGet]
        public ActionResult<List<MessageModel>> BuscarTodasMessagens() {

            return Ok(ConsultarMessagens());
        }

        [HttpPost]
        public IActionResult Post([FromBody] dynamic dadosObtidos) {

            Validate validator = new Validate();

            // Convertendo os Dados Obtidos para JSON
            string jsonString = System.Text.Json.JsonSerializer.Serialize(dadosObtidos);
            MessageModel dados = JsonConvert.DeserializeObject<MessageModel>(jsonString);

            string nome = dados.Name;
            string email = dados.Email;
            string subject = dados.Subject;
            string content = dados.Content;

            try {
                validator.existsOrError(nome, @"Nome não informado");
                validator.existsOrError(email, @"E-mail não informado");
                validator.existsOrError(subject, @"Informe o Assunto");
                validator.existsOrError(content, @"Mande sua mensagem");

                validator.ValidateEmail(email, @"E-mail Inválido!");

                return Ok();

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }

        }
    }
}