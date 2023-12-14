// using Microsoft.AspNetCore.http;
using Microsoft.AspNetCore.Mvc;
using backendCsharp.Models;

namespace backendCsharp.Controllers {

    [Route("api/[controller]")]
    [ApiController]

    public class UsuarioController : ControllerBase {

        private static List<UsuarioModel> usuarios = new List<UsuarioModel> {
            new UsuarioModel { Id = 1, Nome = "Usuário 1" },
            new UsuarioModel { Id = 2, Nome = "Usuário 2" },
            // Adicione mais usuários conforme necessário
        };

        [HttpGet]
        public ActionResult<List<UsuarioModel>> BuscarTodosUsuarios() {

            return Ok(usuarios);
        }
    }
}