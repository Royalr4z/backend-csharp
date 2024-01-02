namespace backendCsharp.Models {

    public class MessageModel {

        public int? Id { get; set; }
        public string? Date { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }

        public MessageModel() {
            Name = string.Empty;
            Email = string.Empty;
            Subject = string.Empty;
            Content = string.Empty;
        }

    }
}