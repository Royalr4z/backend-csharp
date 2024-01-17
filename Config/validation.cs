using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DotNetEnv;

namespace backendCsharp.Config {

    public class Validate {

        public void existsOrError(string value, string msg) {
    
            if(string.IsNullOrEmpty(value)) {
                throw new Exception(msg);
            }

            // Se value for uma string e estiver vazia após remover espaços em branco
            if (value is string && string.IsNullOrWhiteSpace((string)value)) {
                throw new Exception(msg);
            }

        }

        public void existsIntOrError(int value, string msg) {
    
            if (value == 0) {
                throw new Exception(msg);
            }
        }

        public void notExistsOrError(string value, string msg) {
            try {
                existsOrError(value, msg);
            } catch(Exception) {
                return;
            }
            throw new Exception(msg);
        }

        public void equalsOrError(string valueA, string valueB, string msg) {
            if (valueA != valueB) {
                throw new Exception(msg);
            }
        }

        public void ValidateEmail(string value, string msg) {

            string regexPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
            Regex regex = new Regex(regexPattern);
            
            if (!regex.IsMatch(value)) {
                throw new Exception(msg);
            }
        }

        public bool ValidatingToken(string token, bool verifyAdmin = false) {

            Env.Load("./.env");

            string AUTH_SECRET = Environment.GetEnvironmentVariable("AUTH_SECRET") ?? "";

            try {

                var tokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(AUTH_SECRET))
                };

                // Decodificar e validar o token JWT
                var tokenHandler = new JwtSecurityTokenHandler();
                SecurityToken securityToken;

                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);

                // Verificar se o token expirou
                if (securityToken is JwtSecurityToken jwtSecurityToken) {
                    var expirationDate = jwtSecurityToken.ValidTo;
                    var currentDate = DateTime.UtcNow;

                    if (expirationDate < currentDate) {
                        // Token expirou
                        return false;
                    }
                }

                if (verifyAdmin) {
                    
                    // Acessar dados do payload
                    var identity = principal.Identity as ClaimsIdentity;
                    var admin = bool.Parse(identity?.FindFirst("admin")?.Value);

                    if (admin) {
                        return true;
                    } else {
                        return false;
                    }

                } else {

                    return true; // o token é válido
                }
    
            } catch {

                return false; // o token é inválido
            }
        }

    }
}