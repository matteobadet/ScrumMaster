# Phase 1 Data Model: Thèmes Visuels par Colonne

## Colonne (extension)

Entité déjà existante (`backend/src/ScrumMaster.Api/Models/Colonne.cs`, specs/001-retro-board-base),
portée par un `Thème` (specs/004-themes-narratifs), lui-même porté par une `Étape` de type
"Colonnes et post-its" (specs/006-systeme-extensions-etapes). Gagne deux attributs facultatifs :

| Champ | Type | Contrainte | Notes |
|-------|------|------------|-------|
| `Couleur` | `string?` | Longueur max 30 caractères | Valeur CSS libre (hex `#rrggbb` ou nom de couleur CSS), `research.md#1`. Aucune validation de format autre que la longueur — une valeur CSS invalide échoue silencieusement au rendu navigateur, comportement standard et acceptable pour un champ d'habillage visuel facultatif. |
| `UrlIllustration` | `string?` | Longueur max 2048 caractères ; schéma `https` obligatoire si non vide | URL vers une image hébergée en dehors de ScrumMaster, `research.md#2`/`#3`. Jamais récupérée côté serveur. |

Les deux champs restent indépendants et facultatifs individuellement (FR-005). Aucune contrainte
d'unicité entre colonnes d'un même thème (une couleur ou une URL peut être répétée).

**Validation** (appliquée dans `EtapeService.ResolveThemeAsync`, au même point que la validation
déjà existante de `Icone`/`Contexte` sur `Theme`) :
- `UrlIllustration`, si fournie, DOIT être une URI absolue valide de schéma `https` (FR-009) ; sinon
  `DomainValidationException` avec message explicite.
- `UrlIllustration`, si fournie, DOIT respecter la longueur maximale (2048 caractères).
- `Couleur`, si fournie, DOIT respecter la longueur maximale (30 caractères).

## Thème (inchangé dans sa forme, propagation étendue)

Aucun nouveau champ sur `Theme` lui-même (`Icone`/`Contexte`/`Nom` restent tels que
specs/004-themes-narratifs). Seule la construction de ses `Colonnes` (via
`EtapeService.ResolveThemeAsync`/`CopyTheme`) propage désormais `Couleur`/`UrlIllustration` en plus
de `Intitule`, qu'il s'agisse d'une copie depuis un thème prédéfini (catalogue) ou d'une
construction depuis un `ThemePersonnaliseDto`.

## Transport (DTOs)

- **`ThemeSummaryDto`** (`GET /api/themes`) : `Colonnes` passe de `IReadOnlyList<string>` à
  `IReadOnlyList<ColonneSummaireDto>`, avec `ColonneSummaireDto(string Intitule, string? Couleur,
  string? UrlIllustration)` — `research.md#4`.
- **`ThemePersonnaliseDto`** (requête `POST /api/boards`, `ChangeTheme`, `EtapeRequestDto`) : même
  changement de forme pour `Colonnes` (`ColonneSummaireDto` réutilisé).
- **`ColonneDto`** (`GET /api/boards/{boardId}`, événement `ThemeChanged`) : gagne `Couleur` et
  `UrlIllustration` (tous deux `string?`), aux côtés de `Id`/`Intitule`/`Ordre` déjà existants.

## Migration

Migration EF Core additive : deux colonnes nullable (`Couleur`, `UrlIllustration`) sur la table
`Colonnes`. Aucun backfill de données requis — les colonnes existantes conservent `NULL` sur ces
deux champs et s'affichent avec l'apparence actuelle (FR-005, SC-003), à la différence de la
restructuration de specs/006 qui nécessitait un backfill.
