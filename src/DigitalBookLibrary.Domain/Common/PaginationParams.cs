namespace DigitalBookLibrary.Domain.Common
{
    /// <summary>
    /// Base paging request. <see cref="PageSize"/> is clamped to <see cref="MaxPageSize"/> to protect
    /// the server from unbounded queries. Entity-specific query option types extend this with their
    /// own filter/sort fields.
    /// </summary>
    public class PaginationParams
    {
        public const int MaxPageSize = 50;

        private int _pageNumber = 1;
        private int _pageSize = 10;

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 10 : (value > MaxPageSize ? MaxPageSize : value);
        }
    }
}
