namespace ScrumMaster.Api.Services;

/// <summary>
/// Validation HTTPS partagée, extraite de la règle déjà écrite pour l'illustration de colonne et
/// les visuels ROTI (specs/007-themes-visuels-colonnes, specs/008-roti-mini-jeu) — réutilisée par
/// le lien externe (specs/011-pendu-lien-externe, research.md#6). Ne récupère jamais l'URL
/// côté serveur, valide uniquement sa syntaxe.
/// </summary>
public static class UrlValidation
{
    public static void ValiderHttps(string? url, string champ, bool requis = false)
    {
        if (string.IsNullOrEmpty(url))
        {
            if (requis)
            {
                throw new DomainValidationException($"{champ} ne peut pas être vide.");
            }

            return;
        }

        if (url.Length > 2048)
        {
            throw new DomainValidationException($"{champ} ne doit pas dépasser 2048 caractères.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new DomainValidationException($"{champ} doit être une adresse HTTPS valide.");
        }
    }
}
