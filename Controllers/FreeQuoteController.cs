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

    public class FreeQuoteController : ControllerBase {

        [HttpGet]
        public ActionResult<List<FreeQuoteModel>> Get() {

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

            if (validator.ValidatingToken(token ?? "", true)) {

                return Ok(ConsultarDados(0));
            } else {

                return Unauthorized("Unauthorized");
            }
        }

        [HttpGet("{id}")]
        public ActionResult<List<FreeQuoteModel>> GetById(int id) {

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

            if (validator.ValidatingToken(token ?? "", true)) {

                return Ok(ConsultarDados(id));
            } else {

                return Unauthorized("Unauthorized");
            }
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
        public ActionResult<List<FreeQuoteModel>> Delete(int id) {

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

                    Deletar(id);
                    return Ok();
                } else {

                    return Unauthorized("Unauthorized");
                }

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }

        }

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

        private List<FreeQuoteModel> ConsultarDados(int? id) {

            Enviro env = new Enviro();

            using var connection = new NpgsqlConnection(env.ObtendoConfig());
            connection.Open();

            string sql = "";

            if (id > 0) {
                sql = $"SELECT id, date, name, email, service, message FROM free_quote WHERE id = {id}";
            } else {
                sql = "SELECT id, date, name, email, service, message FROM free_quote ORDER BY id ASC;";
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

        private string InserindoDados(dynamic dadosObtidos) {

            Validate validator = new Validate();
            Enviro env = new Enviro();

            // Convertendo os Dados Obtidos para JSON
            string jsonString = System.Text.Json.JsonSerializer.Serialize(dadosObtidos);
            FreeQuoteModel? dados = JsonConvert.DeserializeObject<FreeQuoteModel>(jsonString);

            string nome = dados?.Name ?? "";
            string email = dados?.Email ?? "";
            string service = dados?.Service ?? "";
            string message = dados?.Message ?? "";

            validator.existsOrError(nome, @"Nome não informado");
            validator.existsOrError(email, @"E-mail não informado");
            validator.existsOrError(service, @"Informe o Serviço");
            validator.existsOrError(message, @"Mande sua mensagem");

            validator.ValidateEmail(email, @"E-mail Inválido!");

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
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

        private void Deletar(int id) {

            Enviro env = new Enviro();

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
                connection.Open();

                string query = $"DELETE FROM free_quote WHERE id = {id}";

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
    }
}