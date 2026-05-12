namespace ClyvoVetApi.Models;

public class Pet
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Raca { get; set; } = string.Empty;
    public DateTime DataNascimento { get; set; }
    public int TutorId { get; set; }
    public Tutor? Tutor { get; set; }
    public ICollection<Consulta> Consultas { get; set; } = new List<Consulta>();
    public ICollection<Vacina> Vacinas { get; set; } = new List<Vacina>();
}