using Microsoft.AspNetCore.Mvc;
using backendCsharp.Models;

namespace backendCsharp.Controllers {

    [Route("[controller]")]
    [ApiController]

    public class SignupController : ControllerBase {
    
        [HttpGet]
        public ActionResult<List<UserModel>> Get() {

            return Ok("signup");
        }
    }

    [Route("[controller]")]
    [ApiController]
    
    public class SigninController : ControllerBase {

        [HttpGet]
        public ActionResult<List<UserModel>> Get() {

            return Ok("signin");
        }
    }

    [Route("[controller]")]
    [ApiController]
    
    public class ValidateTokenController : ControllerBase {
    
        [HttpGet]
        public ActionResult<List<UserModel>> Get() {

            return Ok("validadetoken");
        }
    }
}