namespace ScrumMaster.Api.Models;

/// <summary>
/// Trace qu'un rappel de réunion (automatique ou manuel) a été envoyé pour une équipe, un type de
/// réunion et un jour donné — sert uniquement à appliquer la règle de non-doublon (FR-008), pas à
/// afficher un historique. Voir specs/003-rappel-reunion-teams.
/// </summary>
public class RappelEnvoye
{
    public Guid Id { get; set; }

    public string AreaPath { get; set; } = string.Empty;

    public Equipe? Equipe { get; set; }

    public TypeReunion TypeReunion { get; set; }

    public DateOnly Date { get; set; }

    public DateTimeOffset DateEnvoi { get; set; }
}
