namespace ClyvoVetApi.DTOs.Response;

public record LinkDto(string Href, string Rel, string Method = "GET");

public abstract class ResourceResponseDto
{
    public List<LinkDto> Links { get; set; } = [];
}

public interface IPagedResponse
{
    System.Collections.IEnumerable Items { get; }
    int Page { get; }
    int PageSize { get; }
    int TotalPages { get; }
    List<LinkDto> Links { get; set; }
}
