# Phase 1 Data Model: Mini-jeu ROTI

## NiveauRoti (enum)

Échelle fixe à 5 niveaux, ordonnée du moins au plus favorable (`spec.md`, Assumptions) :

```csharp
public enum NiveauRoti
{
    PerteDeTemps,
    PeuRentable,
    MoyennementRentable,
    Rentable,
    TresRentable,
}
```

## ReponseRoti (nouvelle entité)

Symétrique de `ReponseMeteoEquipe` (specs/006-systeme-extensions-etapes).

| Champ | Type | Contrainte | Notes |
|-------|------|------------|-------|
| `EtapeId` | `Guid` | FK → `Etapes.Id`, clé composite | |
| `ParticipantId` | `Guid` | FK → `Participants.Id`, clé composite | |
| `Niveau` | `NiveauRoti` | requis | |
| `DateReponse` | `DateTimeOffset` | requis | |

Clé primaire composite `(EtapeId, ParticipantId)` — upsert par cette clé (une réponse par
participant par étape, remplacée à chaque nouveau choix tant que l'étape reste active, FR-003).

## EtapeRotiVisuel (nouvelle entité)

Personnalisation facultative et sparse du visuel d'un niveau, pour une étape ROTI précise.

| Champ | Type | Contrainte | Notes |
|-------|------|------------|-------|
| `EtapeId` | `Guid` | FK → `Etapes.Id`, clé composite | |
| `Niveau` | `NiveauRoti` | clé composite | |
| `UrlIllustration` | `string` | requis, HTTPS valide, ≤2048 caractères | Même validation que `Colonne.UrlIllustration` (specs/007-themes-visuels-colonnes, research.md#3) |

Clé primaire composite `(EtapeId, Niveau)`. Aucune ligne pour un niveau non personnalisé — le
frontend applique alors le visuel par défaut (emoji, `research.md#2`).

## Etape (extension)

Gagne deux collections, au même niveau que `ReponsesMeteo` :

```csharp
public List<ReponseRoti> ReponsesRoti { get; set; } = new();
public List<EtapeRotiVisuel> VisuelsRoti { get; set; } = new();
```

## Transport (DTOs)

- **`NiveauVisuelDto(string Niveau, string UrlIllustration)`** — une personnalisation, en entrée
  (`EtapeRequestDto.RotiPersonnalisations`) comme en sortie (`EtapeDto.VisuelsRoti`).
- **`ReponseRotiDto(Guid ParticipantId, string NomAffiche, string Niveau)`** — symétrique de
  `ReponseMeteoDto`.
- **`EtapeRequestDto`** (composition d'une séquence, specs/006) : gagne
  `RotiPersonnalisations: IReadOnlyList<NiveauVisuelDto>?`, rempli uniquement si le mini-jeu choisi
  est ROTI (rejet serveur sinon, `research.md#4`).
- **`EtapeDto`** (`GET /api/boards/{boardId}`, `research.md#4` de specs/006) : gagne
  `ReponsesRoti: IReadOnlyList<ReponseRotiDto>?`, `MonNiveauRoti: string?` (uniquement si
  `asParticipantId` fourni), `VisuelsRoti: IReadOnlyList<NiveauVisuelDto>?` (uniquement les niveaux
  personnalisés).

## Migration

Migration EF Core additive : deux nouvelles tables (`ReponsesRoti`, `VisuelsRoti`), aucune colonne
modifiée sur une table existante. Aucun backfill requis — une étape ROTI n'existe jamais avant
cette feature.
