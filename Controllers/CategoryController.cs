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

    public class CategoryController : ControllerBase {

        [HttpGet]
        public ActionResult<List<CategoryModel>> Get() {

            return Ok(ConsultarCategorias(0));
        }

        [HttpGet("{id}")]
        public ActionResult<List<CategoryModel>> GetById(int id) {

            return Ok(ConsultarCategorias(id));
        }

        [HttpPost]
        public IActionResult Post([FromBody] dynamic dadosObtidos) {

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

                    return Ok(InserindoDados(dadosObtidos));
                } else {

                    return Ok("Unauthorized");
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

                    DeletarCategoria(id);
                    return Ok(null);
                } else {

                    return Ok("Unauthorized");
                }

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
            
        }

        [HttpPut("{id}")]
        public ActionResult<List<CategoryModel>> PutById([FromBody] dynamic dadosObtidos, int id) {

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

                    return Ok(AlterarCategoria(id, dadosObtidos));
                } else {

                    return Ok("Unauthorized");
                }

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }

        private dynamic ConsultarCategorias(int? id) {

            Enviro env = new Enviro();

            using var connection = new NpgsqlConnection(env.ObtendoConfig());
            connection.Open();

            string sql = "";

            if (id > 0) {
                sql = $"SELECT * FROM category WHERE id = {id}";
            } else {
                sql = "SELECT * FROM category ORDER BY id ASC;";
            }

            using var cmd = new NpgsqlCommand(sql, connection);

            var categories = new List<CategoryModel>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                var category = new CategoryModel {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Subtitle = reader.GetString(reader.GetOrdinal("subtitle"))
                };

                if (id > 0) { return category; }
                categories.Add(category);
            }

            connection.Close();

            return categories;
        }

        private IActionResult InserindoDados(dynamic dadosObtidos) {

            Validate validator = new Validate();
            Enviro env = new Enviro();

            // Convertendo os Dados Obtidos para JSON
            string jsonString = System.Text.Json.JsonSerializer.Serialize(dadosObtidos);
            CategoryModel? dados = JsonConvert.DeserializeObject<CategoryModel>(jsonString);

            string nome = dados?.Name ?? "";
            string subtitle = dados?.Subtitle ?? "";

            validator.existsOrError(nome, @"Nome não informado");
            validator.existsOrError(subtitle, @"Informe a Descrição");

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
                connection.Open();

                string query = "INSERT INTO category (name, subtitle ) VALUES (@Name, @Subtitle)";

                // Crie um comando SQL com a query e a conexão
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                    command.Parameters.AddWithValue("@Name", nome);
                    command.Parameters.AddWithValue("@Subtitle", subtitle);

                    // Execute o comando
                    int rowsAffected = command.ExecuteNonQuery();

                    connection.Close();

                    // Verifique se alguma linha foi afetada (deve ser maior que 0)
                    if (rowsAffected > 0) {
                        return null;
                    } else {
                        return StatusCode(500, "Falha ao inserir dados.");
                    }
                }

            }
        }

        private void DeletarCategoria(int id) {

            Enviro env = new Enviro();

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
                connection.Open();
                string query = $"""SELECT * FROM blogs WHERE "categoryId" = {id}""";

                // Crie um comando SQL com a query e a conexão
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                    // Execute o comando
                    int rowsAffected = command.ExecuteNonQuery();
                    using var reader = command.ExecuteReader();

                    // Verifique se alguma linha foi afetada (deve ser maior que 0)
                    if (reader.Read()) {
                        throw new Exception("Categoria possui Blogs.");
                    }
                }

                connection.Close();
            }

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
                connection.Open();
                string query = $"DELETE FROM category WHERE id = {id}";

                // Crie um comando SQL com a query e a conexão
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                    // Execute o comando
                    int rowsAffected = command.ExecuteNonQuery();

                    // Verifique se alguma linha foi afetada (deve ser maior que 0)
                    if (rowsAffected == 0) {
                        throw new Exception("Categoria não foi encontrado.");
                    }
                }

                connection.Close();
            }
        }

        private IActionResult AlterarCategoria(int id, dynamic dadosObtidos) {
    
            Validate validator = new Validate();
            Enviro env = new Enviro();

            // Convertendo os Dados Obtidos para JSON
            string jsonString = System.Text.Json.JsonSerializer.Serialize(dadosObtidos);
            CategoryModel? dados = JsonConvert.DeserializeObject<CategoryModel>(jsonString);

            string nome = dados?.Name ?? "";
            string subtitle = dados?.Subtitle ?? "";

            validator.existsOrError(nome, @"Nome não informado");
            validator.existsOrError(subtitle, @"Informe a Descrição");

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
                connection.Open();

                string query = "UPDATE category SET name = @Name, subtitle = @Subtitle WHERE id = @Id;";

                // Crie um comando SQL com a query e a conexão
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@Name", nome);
                    command.Parameters.AddWithValue("@Subtitle", subtitle);

                    // Execute o comando
                    int rowsAffected = command.ExecuteNonQuery();

                    connection.Close();

                    // Verifique se alguma linha foi afetada (deve ser maior que 0)
                    if (rowsAffected > 0) {
                        return null;
                    } else {
                        return StatusCode(500, "Falha ao Alterar dados.");
                    }
                }

            }
        }

    }
}