namespace backendCsharp.Models {

    public class UserModel {

        public int? Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public bool Admin { get; set; }

        public long? iat { get; set; }
        public long? exp { get; set; }
        public string? Token { get; set; }

        public UserModel() {
            Name = string.Empty;
            Email = string.Empty;
        }

    }
}