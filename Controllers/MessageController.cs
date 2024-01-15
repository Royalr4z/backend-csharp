using Microsoft.AspNetCore.Mvc;
using backendCsharp.Models;
using backendCsharp.Config;
using Newtonsoft.Json;
using System.Data.SqlClient;
using TimeZoneConverter;
using NodaTime;
using System;
using Npgsql;

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

        public List<MessageModel> ConsultarMessagens(int? id) {

            environment env = new environment();

            using var connection = new NpgsqlConnection(env.ObtendoConfig());
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

        public string InserindoDados(dynamic dadosObtidos) {

            Validate validator = new Validate();
            environment env = new environment();

            // Convertendo os Dados Obtidos para JSON
            string jsonString = System.Text.Json.JsonSerializer.Serialize(dadosObtidos);
            MessageModel? dados = JsonConvert.DeserializeObject<MessageModel>(jsonString);

            string nome = dados?.Name ?? "";
            string email = dados?.Email ?? "";
            string subject = dados?.Subject ?? "";
            string content = dados?.Content ?? "";

            validator.existsOrError(nome, @"Nome não informado");
            validator.existsOrError(email, @"E-mail não informado");
            validator.existsOrError(subject, @"Informe o Assunto");
            validator.existsOrError(content, @"Mande sua mensagem");

            validator.ValidateEmail(email, @"E-mail Inválido!");

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
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

        public void DeletarMessagem(int id) {

            environment env = new environment();

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
                connection.Open();

                string query = $"DELETE FROM message WHERE id = {id}";

                // Crie um comando SQL com a query e a conexão
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                    // Execute o comando
                    int rowsAffected = command.ExecuteNonQuery();

                    // Verifique se alguma linha foi afetada (deve ser maior que 0)
                    if (rowsAffected == 0) {
                        throw new Exception("Falha ao Deletar dados.");
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
                return Ok(InserindoDados(dadosObtidos));

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }

        }

        [HttpDelete("{id}")]
        public ActionResult<List<MessageModel>> Delete(int id) {

            try {
                DeletarMessagem(id);
                return Ok();

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }

        }

    }
}