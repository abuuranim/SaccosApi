namespace SaccosApi.DTO
{
    public class PaginationResponse<T> where T : class
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public T Data { get; set; }

        public PaginationResponse(int totalRecords, T data, int currentPageNumber, int pageSize)
        {
            TotalRecords = totalRecords;
            Data = data;
            PageNumber = currentPageNumber;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling((double)TotalRecords / (double)PageSize);
            HasPreviousPage = PageNumber > 1;
            HasNextPage = PageNumber < TotalPages;

        }
    }
}
