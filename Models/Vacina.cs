namespace ClyvoVetApi.Models;

public class Vacina
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataAplicacao { get; set; }
    public DateTime? ProximaDose { get; set; }
    public bool Aplicada { get; set; }
    public int PetId { get; set; }
    public Pet? Pet { get; set; }
}