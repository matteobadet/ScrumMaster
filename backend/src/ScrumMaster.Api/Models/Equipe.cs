namespace ScrumMaster.Api.Models;

public class Equipe
{
    public string AreaPath { get; set; } = string.Empty;

    public List<Board> Boards { get; set; } = new();
}
