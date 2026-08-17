# Data Model: Historique des boards par équipe

Aucune nouvelle entité ni migration (research.md#3). Ce document décrit uniquement la forme de
donnée exposée par le nouvel endpoint.

## BoardSummaire (résultat calculé)

Projection directe de l'entité `Board` existante (`backend/src/ScrumMaster.Api/Models/Board.cs`),
sans aucun champ additionnel.

| Champ | Type | Source |
|---|---|---|
| `Id` | `Guid` | `Board.Id` |
| `Iteration` | `string` | `Board.Iteration` |
| `Statut` | `string` | `Board.Statut` (`"Actif"` ou `"Cloture"`) |
| `DateCreation` | `DateTimeOffset` | `Board.DateCreation` |

La liste renvoyée est triée par `DateCreation` décroissante (FR-003) et filtrée par `AreaPath`
(FR-001) ; une équipe inconnue ou sans board renvoie une liste vide (FR-005).
