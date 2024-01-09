using Microsoft.AspNetCore.Mvc;
using backendCsharp.Models;

namespace backendCsharp.Controllers {

    [Route("[controller]")]
    [ApiController]

    public class SignupController : ControllerBase {

        public void InserindoUsuario(dynamic dadosObtidos) {

            Validate validator = new Validate();

            // Convertendo os Dados Obtidos para JSON
            string jsonString = System.Text.Json.JsonSerializer.Serialize(dadosObtidos);
            UserModel? dados = JsonConvert.DeserializeObject<UserModel>(jsonString);

            string nome = dados?.Name ?? "";
            string email = dados?.Email ?? "";
            string password = dados?.Password ?? "";
            string confirmPassword = dados?.ConfirmPassword ?? "";

            validator.existsOrError(nome, @"Nome não informado");
            validator.existsOrError(email, @"E-mail não informado");
            validator.existsOrError(password, @"Senha não informada");
            validator.existsOrError(confirmPassword, @"Confirme sua Senha");

            validator.equalsOrError(password, confirmPassword, @"Senhas não conferem");

            validator.ValidateEmail(email, @"E-mail Inválido!");
            
        }
    
        [HttpPost]
        public IActionResult Post([FromBody] dynamic dadosObtidos) {

            try {
                InserindoUsuario(dadosObtidos);
                return Ok("signup");

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }
    }

    [Route("[controller]")]
    [ApiController]
    
    public class SigninController : ControllerBase {

        [HttpPost]
        public IActionResult Post([FromBody] dynamic dadosObtidos) {

            try {
                return Ok("signin");

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }

        }
    }

    [Route("[controller]")]
    [ApiController]
    
    public class ValidateTokenController : ControllerBase {
    
        [HttpPost]
        public IActionResult Post([FromBody] dynamic dadosObtidos) {

            try {
                return Ok("validadetoken");

            } catch (Exception ex) {
                return BadRequest(ex.Message);
            }
        }
    }
}