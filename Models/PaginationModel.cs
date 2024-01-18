namespace backendCsharp.Models {

    public class PaginationModel {

        public int? Page { get; set; }
        public int? Limit { get; set; }
        public int? TotalCount { get; set; }
        public int? TotalPages { get; set; }

    }

    public class ResponseModel {

        public dynamic Blogs { get; set; }
        public dynamic Pagination { get; set; }

        public ResponseModel(dynamic blogs, dynamic pagination) {

            Blogs = blogs;
            Pagination = pagination;
        }
    }
}