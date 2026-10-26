namespace ClyvoVetApi.DTOs.Response;

public class HealthDashboardResponseDto : ResourceResponseDto
{
    public int Score { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Recomendacoes { get; set; } = [];
}
