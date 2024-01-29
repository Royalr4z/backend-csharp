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

    public class BlogsController : ControllerBase {

        [HttpGet]
        public ActionResult<List<ResponseModel>> Get() {

            return Ok(ConsultarBlogs());
        }

        [HttpGet("{id}")]
        public ActionResult<List<ResponseModel>> GetById(int id) {

            return Ok(ConsultarBlogsById(id));
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
        public ActionResult<List<BlogsModel>> Delete(int id) {
            
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

                    DeletarBlog(id);
                    return Ok();
                } else {

                    return Ok("Unauthorized");
                }

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
            
        }

        private BlogsModel ConsultarBlogsById(int id) {

            Enviro env = new Enviro();

            using var connection = new NpgsqlConnection(env.ObtendoConfig());
            connection.Open();
            
            string sql = $@"SELECT blogs.*, users.name AS userName, category.name AS categoryName
                FROM blogs INNER JOIN users ON blogs.""userId"" = users.id
                INNER JOIN category ON blogs.""categoryId"" = category.id
                WHERE blogs.id = {id};";

            using var cmd = new NpgsqlCommand(sql, connection);

            var blog = new BlogsModel {
                    Id = 0,
                    Date = "",
                    Title = "",
                    Subtitle = "",
                    ImageUrl = "",
                    Content = "",
                    UserId = 0,
                    UserName = "",
                    CategoryId = 0,
                    CategoryName = "",
            };

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                blog = new BlogsModel {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Date = reader.GetString(reader.GetOrdinal("date")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Subtitle = reader.GetString(reader.GetOrdinal("subtitle")),
                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("imageUrl")) ? "" : reader.GetString(reader.GetOrdinal("imageUrl")),
                    Content = reader.GetString(reader.GetOrdinal("content")),
                    UserId = reader.GetInt32(reader.GetOrdinal("userId")),
                    UserName = reader.GetString(reader.GetOrdinal("userName")),
                    CategoryId = reader.GetInt32(reader.GetOrdinal("categoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("categoryName")),
                };
            }

            return blog;
        }

        private ResponseModel ConsultarBlogs() {

            // Acessando os parâmetros da consulta
            int page, limit;

            int.TryParse(HttpContext.Request.Query["page"], out page);
            int.TryParse(HttpContext.Request.Query["limit"], out limit);

            // Definindo valores padrão se a conversão falhar
            page = Math.Max(page, 1);
            limit = (limit == 0) ? 100 : Math.Min(limit, 1000);

            int offset = (page - 1) * limit;

            Enviro env = new Enviro();

            using var connection = new NpgsqlConnection(env.ObtendoConfig());
            connection.Open();

            string sql = $@"SELECT blogs.*, users.name AS userName, category.name AS categoryName
                FROM blogs INNER JOIN users ON blogs.""userId"" = users.id
                INNER JOIN category ON blogs.""categoryId"" = category.id
                ORDER BY blogs.id ASC
                OFFSET @Offset LIMIT @Limit;";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Offset", offset);
            cmd.Parameters.AddWithValue("@Limit", limit);

            var blogs = new List<BlogsModel>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                var blog = new BlogsModel {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Date = reader.GetString(reader.GetOrdinal("date")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Subtitle = reader.GetString(reader.GetOrdinal("subtitle")),
                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("imageUrl")) ? null : reader.GetString(reader.GetOrdinal("imageUrl")),
                    Content = reader.GetString(reader.GetOrdinal("content")),
                    UserId = reader.GetInt32(reader.GetOrdinal("userId")),
                    UserName = reader.GetString(reader.GetOrdinal("userName")),
                    CategoryId = reader.GetInt32(reader.GetOrdinal("categoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("categoryName")),
                };
                blogs.Add(blog);
            }

            connection.Close();

            static int GetTotalBlogCount(string connectionString) {
                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString)){

                    connection.Open();

                    string query = "SELECT COUNT(*) FROM blogs;";
                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                        object countResult = command.ExecuteScalar();

                        int totalCount = countResult != null ? Convert.ToInt32(countResult) : 0;
                        return totalCount;
                    }
                }
            }

            connection.Close();

            int totalCount = GetTotalBlogCount(env.ObtendoConfig());

            var pagination = new PaginationModel {
                Page = page,
                Limit = limit,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / limit),
            };

            var response = new ResponseModel(blogs, pagination);

            return response;
        }

        private IActionResult InserindoDados(dynamic dadosObtidos) {

            Validate validator = new Validate();
            Enviro env = new Enviro();

            // Convertendo os Dados Obtidos para JSON
            string jsonString = System.Text.Json.JsonSerializer.Serialize(dadosObtidos);
            BlogsModel? dados = JsonConvert.DeserializeObject<BlogsModel>(jsonString);

            string date = dados?.Date ?? "";
            string title = dados?.Title ?? "";
            string subtitle = dados?.Subtitle ?? "";
            string imageUrl = dados?.ImageUrl ?? "";
            string content = dados?.Content ?? "";

            int userId = dados?.UserId ?? 0;
            int categoryId = dados?.CategoryId ?? 0;

            validator.existsOrError(date, @"Data não informada");
            validator.existsOrError(title, @"Informe o Título!");
            validator.existsOrError(subtitle, @"Informe o Descrição!");
            validator.existsOrError(content, @"Coloque o Conteúdo!");
            validator.existsIntOrError(userId, @"Informe o Usuário!");
            validator.existsIntOrError(categoryId, @"Informe a Categoria!");

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
                connection.Open();

                string query = $@"INSERT INTO blogs (date, title, subtitle, ""imageUrl"", content, ""userId"", ""categoryId"")
                                 VALUES (@Date, @Title, @Subtitle, @ImageUrl, @Content, @UserId, @CategoryId)";

                // Crie um comando SQL com a query e a conexão
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                    command.Parameters.AddWithValue("@Date", date);
                    command.Parameters.AddWithValue("@Title", title);
                    command.Parameters.AddWithValue("@Subtitle", subtitle);
                    command.Parameters.AddWithValue("@ImageUrl", imageUrl);
                    command.Parameters.AddWithValue("@Content", content);
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@CategoryId", categoryId);

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

        private void DeletarBlog(int id) {

            Enviro env = new Enviro();

            using (NpgsqlConnection  connection = new NpgsqlConnection(env.ObtendoConfig())) {
                connection.Open();
                string query = $"DELETE FROM blogs WHERE id = {id}";

                // Crie um comando SQL com a query e a conexão
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                    // Execute o comando
                    int rowsAffected = command.ExecuteNonQuery();

                    // Verifique se alguma linha foi afetada (deve ser maior que 0)
                    if (rowsAffected == 0) {
                        throw new Exception("Blog não foi encontrado.");
                    }
                }

                connection.Close();
            }
        }
    }

    [Route("blogs/category")]
    [ApiController]
    
    public class BlogsCategoryController : ControllerBase {
        
        [HttpGet]
        public ActionResult<List<ResponseModel>> Get() {
            // Acessando os parâmetros da consulta
            int page, limit;
            string category = HttpContext.Request?.Query["category"] ?? "";

            int.TryParse(HttpContext.Request.Query["page"], out page);
            int.TryParse(HttpContext.Request.Query["limit"], out limit);

            // Definindo valores padrão se a conversão falhar
            page = Math.Max(page, 1);
            limit = (limit == 0) ? 100 : Math.Min(limit, 1000);

            int offset = (page - 1) * limit;

            Enviro env = new Enviro();

            using var connection = new NpgsqlConnection(env.ObtendoConfig());
            connection.Open();

            string sql;
            
            sql = $@"SELECT blogs.*, users.name AS userName, category.name AS categoryName
                    FROM blogs
                    INNER JOIN users ON blogs.""userId"" = users.id
                    INNER JOIN category ON blogs.""categoryId"" = category.id";

            if (!string.IsNullOrEmpty(category)) {
                sql += $" WHERE category.name = @Category";
            }

            sql += " ORDER BY blogs.id ASC OFFSET @Offset LIMIT @Limit;";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Offset", offset);
            cmd.Parameters.AddWithValue("@Limit", limit);

            if (!string.IsNullOrEmpty(category)) {
                cmd.Parameters.AddWithValue("@Category", category);
            }

            var blogs = new List<BlogsModel>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                var blog = new BlogsModel {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Date = reader.GetString(reader.GetOrdinal("date")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Subtitle = reader.GetString(reader.GetOrdinal("subtitle")),
                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("imageUrl")) ? "" : reader.GetString(reader.GetOrdinal("imageUrl")),
                    Content = reader.GetString(reader.GetOrdinal("content")),
                    UserId = reader.GetInt32(reader.GetOrdinal("userId")),
                    UserName = reader.GetString(reader.GetOrdinal("userName")),
                    CategoryId = reader.GetInt32(reader.GetOrdinal("categoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("categoryName")),
                };
                blogs.Add(blog);
            }

            connection.Close();

            static int GetTotalBlogCount(string connectionString) {
                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString)){

                    connection.Open();

                    string query = "SELECT COUNT(*) FROM blogs;";
                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                        object countResult = command.ExecuteScalar();

                        int totalCount = countResult != null ? Convert.ToInt32(countResult) : 0;
                        return totalCount;
                    }
                }
            }

            connection.Close();

            int totalCount = GetTotalBlogCount(env.ObtendoConfig());

            var pagination = new PaginationModel {
                Page = page,
                Limit = limit,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / limit),
            };

            var response = new ResponseModel(blogs, pagination);

            return Ok(response);
        }
    }

    [Route("blogs/OrderBy")]
    [ApiController]
    
    public class BlogsOrderbyController : ControllerBase {
        
        [HttpGet]
        public ActionResult<List<ResponseModel>> Get() {
            // Acessando os parâmetros da consulta
            int page, limit;

            page = 1;
            limit = 3;

            int offset = (page - 1) * limit;

            Enviro env = new Enviro();

            using var connection = new NpgsqlConnection(env.ObtendoConfig());
            connection.Open();

            string sql;
            
            sql = $@"SELECT blogs.*, users.name AS userName, category.name AS categoryName
                    FROM blogs
                    INNER JOIN users ON blogs.""userId"" = users.id
                    INNER JOIN category ON blogs.""categoryId"" = category.id
                    ORDER BY blogs.id DESC
                    OFFSET @Offset LIMIT @Limit;";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@Offset", offset);
            cmd.Parameters.AddWithValue("@Limit", limit);

            var blogs = new List<BlogsModel>();

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) {
                var blog = new BlogsModel {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Date = reader.GetString(reader.GetOrdinal("date")),
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    Subtitle = reader.GetString(reader.GetOrdinal("subtitle")),
                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("imageUrl")) ? "" : reader.GetString(reader.GetOrdinal("imageUrl")),
                    Content = reader.GetString(reader.GetOrdinal("content")),
                    UserId = reader.GetInt32(reader.GetOrdinal("userId")),
                    UserName = reader.GetString(reader.GetOrdinal("userName")),
                    CategoryId = reader.GetInt32(reader.GetOrdinal("categoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("categoryName")),
                };
                blogs.Add(blog);
            }

            connection.Close();

            static int GetTotalBlogCount(string connectionString) {
                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString)){

                    connection.Open();

                    string query = "SELECT COUNT(*) FROM blogs;";
                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection)) {

                        object countResult = command.ExecuteScalar();

                        int totalCount = countResult != null ? Convert.ToInt32(countResult) : 0;
                        return totalCount;
                    }
                }
            }

            connection.Close();

            int totalCount = GetTotalBlogCount(env.ObtendoConfig());

            var pagination = new PaginationModel {
                Page = page,
                Limit = limit,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / limit),
            };

            var response = new ResponseModel(blogs, pagination);

            return Ok(response);
        }
    }
}