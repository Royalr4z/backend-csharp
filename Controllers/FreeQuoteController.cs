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

    public class FreeQuoteController : ControllerBase {

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

        public List<FreeQuoteModel> ConsultarDados(int? id) {

            using var connection = new NpgsqlConnection(ObtendoConfig());
            connection.Open();

            string sql = "";

            if (id > 0) {
                sql = $"SELECT * FROM free_quote WHERE id = {id}";
            } else {
                sql = "SELECT * FROM free_quote";
            }

            using var cmd = new NpgsqlCommand(sql, connection);

            var FreeQuotes = new List<FreeQuoteModel>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                var message = new FreeQuoteModel {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Date = reader.GetString(reader.GetOrdinal("date")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Service = reader.GetString(reader.GetOrdinal("service")),
                    Message = reader.GetString(reader.GetOrdinal("message"))

                };
                FreeQuotes.Add(message);
            }

            connection.Close();

            return FreeQuotes;
        }

        public void InserindoDados(dynamic dadosObtidos) {

            Validate validator = new Validate();

            // Convertendo os Dados Obtidos para JSON
            string jsonString = System.Text.Json.JsonSerializer.Serialize(dadosObtidos);
            FreeQuoteModel dados = JsonConvert.DeserializeObject<FreeQuoteModel>(jsonString);

            string nome = dados.Name;
            string email = dados.Email;
            string service = dados.Service;
            string message = dados.Message;

            validator.existsOrError(nome, @"Nome não informado");
            validator.existsOrError(email, @"E-mail não informado");
            validator.existsOrError(service, @"Informe o Serviço");
            validator.existsOrError(message, @"Mande sua mensagem");

            validator.ValidateEmail(email, @"E-mail Inválido!");

            using (NpgsqlConnection  connection = new NpgsqlConnection(ObtendoConfig())) {
                connection.Open();

                string query = "INSERT INTO free_quote (date, name, email, service, message) VALUES (@Date, @Name, @Email, @Service, @Message)";

                // Crie um comando SQL com a query e a conexão
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                    command.Parameters.AddWithValue("@Date", ObtendoData());
                    command.Parameters.AddWithValue("@Name", nome);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Service", service);
                    command.Parameters.AddWithValue("@Message", message);

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

        public void Deletar(int id) {

            using (NpgsqlConnection  connection = new NpgsqlConnection(ObtendoConfig())) {
                connection.Open();

                string query = $"DELETE FROM free_quote WHERE id = {id}";

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
        public ActionResult<List<FreeQuoteModel>> Get() {

            return Ok(ConsultarDados(0));
        }

        [HttpGet("{id}")]
        public ActionResult<List<FreeQuoteModel>> GetById(int id) {

            return Ok(ConsultarDados(id));
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
        public ActionResult<List<FreeQuoteModel>> Delete(int id) {

            Deletar(id);
            
            return Ok();
        }

    }
}