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

    public class BlogsController : ControllerBase {

        private string ObtendoConfig() {

            Env.Load("./.env");

            string dbHost = Environment.GetEnvironmentVariable("DATABASE_HOST") ?? "";
            string dbUser = Environment.GetEnvironmentVariable("DATABASE_USER") ?? "";
            string dbPassword = Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? "";
            string dbName = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "";

            string connectionString = $"Host={dbHost};Username={dbUser};Password={dbPassword};Database={dbName}";
        
            return connectionString;
        }

        public List<BlogsModel> ConsultarBlogs(int? id) {

            using var connection = new NpgsqlConnection(ObtendoConfig());
            connection.Open();

            string sql = "";

            if (id > 0) {
                sql = @$"SELECT blogs.*, users.name AS userName, category.name AS categoryName
                FROM blogs WHERE blogs.id = {id} INNER JOIN users ON blogs.userId = users.id
                INNER JOIN category ON blogs.categoryId = category.id;";
            } else {
                sql = @"SELECT blogs.*, users.name AS userName, category.name AS categoryName
                FROM blogs INNER JOIN users ON blogs.userId = users.id
                INNER JOIN category ON blogs.categoryId = category.id;";
            }

            using var cmd = new NpgsqlCommand(sql, connection);

            var blogs = new List<BlogsModel>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                var blog = new BlogsModel {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Subtitle = reader.GetString(reader.GetOrdinal("subtitle")),
                    ImageUrl = reader.GetString(reader.GetOrdinal("imageUrl")),
                    Content = reader.GetString(reader.GetOrdinal("content")),
                    UserId = reader.GetInt32(reader.GetOrdinal("userId")),
                    UserName = reader.GetString(reader.GetOrdinal("userName")),
                    CategoryId = reader.GetInt32(reader.GetOrdinal("categoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("categoryName")),
                };
                blogs.Add(blog);
            }

            connection.Close();

            return blogs;
        }

        public void InserindoDados(dynamic dadosObtidos) {

            // Validate validator = new Validate();

            // // Convertendo os Dados Obtidos para JSON
            // string jsonString = System.Text.Json.JsonSerializer.Serialize(dadosObtidos);
            // BlogsModel dados = JsonConvert.DeserializeObject<BlogsModel>(jsonString);

            // string nome = dados.Name;
            // string subtitle = dados.Subtitle;

            // validator.existsOrError(nome, @"Nome não informado");
            // validator.existsOrError(subtitle, @"Informe a Descrição");

            // using (NpgsqlConnection  connection = new NpgsqlConnection(ObtendoConfig())) {
            //     connection.Open();

            //     string query = "INSERT INTO category (name, subtitle ) VALUES (@Name, @Subtitle)";

            //     // Crie um comando SQL com a query e a conexão
            //     using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

            //         command.Parameters.AddWithValue("@Name", nome);
            //         command.Parameters.AddWithValue("@Subtitle", subtitle);

            //         // Execute o comando
            //         int rowsAffected = command.ExecuteNonQuery();

            //     }

            //     connection.Close();
            // }
        }

        private async void DeletarBlog(int id) {


        }

        [HttpGet]
        public ActionResult<List<BlogsModel>> Get() {

            return Ok(ConsultarBlogs(0));
        }

        [HttpGet("{id}")]
        public ActionResult<List<BlogsModel>> GetById(int id) {

            return Ok(ConsultarBlogs(id));
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

                DeletarBlog(id);
                return Ok();

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
            
        }
    }
}