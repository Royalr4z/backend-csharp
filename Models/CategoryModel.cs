namespace backendCsharp.Models {

    public class CategoryModel {

        public int? Id { get; set; }
        public string Name { get; set; }
        public string Subtitle { get; set; }

        public CategoryModel() {
            Name = string.Empty;
            Subtitle = string.Empty;
        }

    }
}