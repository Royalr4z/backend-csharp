namespace backendCsharp.Models {

    public class FreeQuoteModel {

        public int? Id { get; set; }
        public string? Date { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Service { get; set; }
        public string Message { get; set; }

        public FreeQuoteModel() {
            Name = string.Empty;
            Email = string.Empty;
            Service = string.Empty;
            Message = string.Empty;
        }

    }
}