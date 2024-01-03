namespace backendCsharp.Models {

    public class BlogsModel {

        public int? Id { get; set; }
        public string Date { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string ImageUrl { get; set; }
        public string Content { get; set; }
        public string UserId { get; set; }
        public string? UserName { get; set; }
        public string CategoryId { get; set; }
        public string? CategoryName { get; set; }

        public BlogsModel() {
            Date = string.Empty;
            Title = string.Empty;
            Subtitle = string.Empty;
            ImageUrl = string.Empty;
            Content = string.Empty;
        }


    }
}