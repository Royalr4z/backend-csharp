using Microsoft.AspNetCore.Mvc;
using backendCsharp.Models;

namespace backendCsharp.Controllers {

    [Route("[controller]")]
    [ApiController]

    public class SignupController : ControllerBase {
    
        [HttpPost]
        public IActionResult Post([FromBody] dynamic dadosObtidos) {

            try {
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