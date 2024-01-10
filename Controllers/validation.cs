using System.Text.RegularExpressions;
using DotNetEnv;

namespace backendCsharp.Controllers {

    public class Validate {

        public string ObtendoConfig() {

            Env.Load("./.env");

            string dbHost = Environment.GetEnvironmentVariable("DATABASE_HOST") ?? "";
            string dbUser = Environment.GetEnvironmentVariable("DATABASE_USER") ?? "";
            string dbPassword = Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? "";
            string dbName = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "";

            string connectionString = $"Host={dbHost};Username={dbUser};Password={dbPassword};Database={dbName}";
        
            return connectionString;
        }

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
    }
}