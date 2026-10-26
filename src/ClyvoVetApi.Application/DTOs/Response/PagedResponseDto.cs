namespace ClyvoVetApi.DTOs.Response;

public class PagedResponseDto<T> : IPagedResponse
{
    public IEnumerable<T> Data { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;
    public List<LinkDto> Links { get; set; } = [];
    System.Collections.IEnumerable IPagedResponse.Items => Data;
}
