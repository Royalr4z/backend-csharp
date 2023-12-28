using Microsoft.AspNetCore.Mvc;
using backendCsharp.Models;
using Newtonsoft.Json;
using System.Data.SqlClient;
using TimeZoneConverter;
using NodaTime;
using System;
using Npgsql;
using DotNetEnv;

namespace backendCsharp.Controllers {

    [Route("[controller]")]
    [ApiController]

    public class MessageController : ControllerBase {

        private string ObtendoData() {
            // Defina o fuso horário desejado
            string timeZoneId = "America/Sao_Paulo";

            // Obtenha o fuso horário correspondente
            DateTimeZone timeZone = DateTimeZoneProviders.Tzdb[timeZoneId];

            // Obtenha a data e hora atual no fuso horário especificado
            ZonedDateTime dateTimeBrasil = SystemClock.Instance.GetCurrentInstant().InZone(timeZone);

            // Converta para o formato desejado (por exemplo, formato ISO 8601)
            string formattedDateTime = dateTimeBrasil.ToString("dd/MM/yyyy HH:mm:ss", null);

            return formattedDateTime;
        }

        private string ObtendoConfig() {

            Env.Load("./.env");

            string dbHost = Environment.GetEnvironmentVariable("DATABASE_HOST") ?? "";
            string dbUser = Environment.GetEnvironmentVariable("DATABASE_USER") ?? "";
            string dbPassword = Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? "";
            string dbName = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "";

            string connectionString = $"Host={dbHost};Username={dbUser};Password={dbPassword};Database={dbName}";
        
            return connectionString;
        }

        public List<MessageModel> ConsultarMessagens(int? id) {

            using var connection = new NpgsqlConnection(ObtendoConfig());
            connection.Open();

            string sql = "";

            if (id > 0) {
                sql = $"SELECT * FROM message WHERE id = {id}";
            } else {
                sql = "SELECT * FROM message";
            }

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

        public void InserindoDados(dynamic dadosObtidos) {

            Validate validator = new Validate();

            // Convertendo os Dados Obtidos para JSON
            string jsonString = System.Text.Json.JsonSerializer.Serialize(dadosObtidos);
            MessageModel dados = JsonConvert.DeserializeObject<MessageModel>(jsonString);

            string nome = dados.Name;
            string email = dados.Email;
            string subject = dados.Subject;
            string content = dados.Content;

            validator.existsOrError(nome, @"Nome não informado");
            validator.existsOrError(email, @"E-mail não informado");
            validator.existsOrError(subject, @"Informe o Assunto");
            validator.existsOrError(content, @"Mande sua mensagem");

            validator.ValidateEmail(email, @"E-mail Inválido!");

            using (NpgsqlConnection  connection = new NpgsqlConnection(ObtendoConfig())) {
                connection.Open();

                string query = "INSERT INTO message (date, name, email, subject, content) VALUES (@Date, @Name, @Email, @Subject, @Content)";

                // Crie um comando SQL com a query e a conexão
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                    command.Parameters.AddWithValue("@Date", ObtendoData());
                    command.Parameters.AddWithValue("@Name", nome);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Subject", subject);
                    command.Parameters.AddWithValue("@Content", content);

                    // Execute o comando
                    int rowsAffected = command.ExecuteNonQuery();

                    // Verifique se alguma linha foi afetada (deve ser maior que 0)
                    if (rowsAffected > 0) {
                        Console.WriteLine("Dados inseridos com sucesso!");
                    } else {
                        Console.WriteLine("Falha ao inserir dados.");
                    }
                }

                connection.Close();
            }
        }

        public void DeletarMessagem(int id) {

            using (NpgsqlConnection  connection = new NpgsqlConnection(ObtendoConfig())) {
                connection.Open();

                string query = $"DELETE FROM message WHERE id = {id}";

                // Crie um comando SQL com a query e a conexão
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                    // Execute o comando
                    int rowsAffected = command.ExecuteNonQuery();

                    // Verifique se alguma linha foi afetada (deve ser maior que 0)
                    if (rowsAffected > 0) {
                        Console.WriteLine("Dados deletados com sucesso!");
                    } else {
                        Console.WriteLine("Falha ao Deletar dados.");
                    }
                }

                connection.Close();
            }
        }

        [HttpGet]
        public ActionResult<List<MessageModel>> Get() {

            return Ok(ConsultarMessagens(0));
        }

        [HttpGet("{id}")]
        public ActionResult<List<MessageModel>> GetById(int id) {

            return Ok(ConsultarMessagens(id));
        }

        [HttpPost]
        public IActionResult Post([FromBody] dynamic dadosObtidos) {

            try {
                InserindoDados(dadosObtidos);

                return Ok();

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }

        }

        [HttpDelete("{id}")]
        public ActionResult<List<MessageModel>> Delete(int id) {
            
            DeletarMessagem(id);
            
            return Ok();
        }

    }
}