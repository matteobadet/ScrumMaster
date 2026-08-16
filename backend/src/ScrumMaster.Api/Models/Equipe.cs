namespace ScrumMaster.Api.Models;

public class Equipe
{
    public string AreaPath { get; set; } = string.Empty;

    /// <summary>Identifiant de la conversation/channel Teams associé (specs/002-poll-utilite-reunion).</summary>
    public string? TeamsChannelId { get; set; }

    public List<Board> Boards { get; set; } = new();
}
