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

    public class CategoryController : ControllerBase {

        private string ObtendoConfig() {

            Env.Load("./.env");

            string dbHost = Environment.GetEnvironmentVariable("DATABASE_HOST") ?? "";
            string dbUser = Environment.GetEnvironmentVariable("DATABASE_USER") ?? "";
            string dbPassword = Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? "";
            string dbName = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "";

            string connectionString = $"Host={dbHost};Username={dbUser};Password={dbPassword};Database={dbName}";
        
            return connectionString;
        }

        public List<CategoryModel> ConsultarUsuarios(int? id) {

            using var connection = new NpgsqlConnection(ObtendoConfig());
            connection.Open();

            string sql = "";

            if (id > 0) {
                sql = $"SELECT * FROM category WHERE id = {id}";
            } else {
                sql = "SELECT * FROM category";
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
                categories.Add(category);
            }

            connection.Close();

            return categories;
        }

        public void InserindoDados(dynamic dadosObtidos) {

            Validate validator = new Validate();

            // Convertendo os Dados Obtidos para JSON
            string jsonString = System.Text.Json.JsonSerializer.Serialize(dadosObtidos);
            CategoryModel dados = JsonConvert.DeserializeObject<CategoryModel>(jsonString);

            string nome = dados.Name;
            string subtitle = dados.Subtitle;

            validator.existsOrError(nome, @"Nome não informado");
            validator.existsOrError(subtitle, @"Informe a Descrição");

            using (NpgsqlConnection  connection = new NpgsqlConnection(ObtendoConfig())) {
                connection.Open();

                string query = "INSERT INTO category (name, subtitle ) VALUES (@Name, @Subtitle)";

                // Crie um comando SQL com a query e a conexão
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                    command.Parameters.AddWithValue("@Name", nome);
                    command.Parameters.AddWithValue("@Subtitle", subtitle);

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

        private async void DeletarUsuario(int id) {


        }

        [HttpGet]
        public ActionResult<List<CategoryModel>> Get() {

            return Ok(ConsultarUsuarios(0));
        }

        [HttpGet("{id}")]
        public ActionResult<List<CategoryModel>> GetById(int id) {

            return Ok(ConsultarUsuarios(id));
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
            
            try {

                DeletarUsuario(id);
                return Ok();

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
            
        }
    }
}