namespace ScrumMaster.Api.Services;

/// <summary>
/// Calcul de l'état d'une partie de Pendu (vue masquée, essais restants, issue), partagé entre la
/// lecture (`BoardService.BuildEtapeDto`) et l'écriture (`MiniJeuService.ProposerLettrePenduAsync`)
/// pour éviter toute divergence entre les deux (specs/011-pendu-lien-externe, research.md#2).
/// </summary>
public static class PenduGameState
{
    public const int MaxEssais = 6;

    public static (IReadOnlyList<string?> MotMasque, int EssaisRestants, string Etat, string? MotComplet) Calculer(
        string motAPendu,
        IEnumerable<(char Lettre, bool Correcte)> lettresProposees
    )
    {
        var liste = lettresProposees.ToList();
        var lettresCorrectes = new HashSet<char>(liste.Where(l => l.Correcte).Select(l => l.Lettre));
        var essaisUtilises = liste.Count(l => !l.Correcte);
        var essaisRestants = Math.Max(0, MaxEssais - essaisUtilises);

        var motMasque = motAPendu
            .Select(c => !char.IsLetter(c) || lettresCorrectes.Contains(char.ToUpperInvariant(c)) ? c.ToString() : null)
            .ToList();

        var victoire = !motAPendu.Where(char.IsLetter).Any(c => !lettresCorrectes.Contains(char.ToUpperInvariant(c)));
        var etat = victoire ? "Victoire"
            : essaisRestants <= 0 ? "Defaite"
            : "EnCours";
        var motComplet = etat == "EnCours" ? null : motAPendu;

        return (motMasque, essaisRestants, etat, motComplet);
    }
}
