# Data Model: Point de sprint (stats Azure DevOps)

Aucune nouvelle table ni entité persistée — le point de sprint est calculé à la demande à partir de
données lues en direct depuis Azure DevOps (voir research.md#4 et spec.md Key Entities). Ce
document décrit uniquement les formes de données calculées (DTOs applicatifs), pas un modèle EF
Core.

## PointDeSprint (résultat calculé)

| Champ | Type | Description |
|---|---|---|
| `Iteration` | `string` | Chemin de l'Iteration du board (déjà stocké sur `Board.Iteration`). |
| `RepartitionParType` | `IReadOnlyList<RepartitionType>` | Une entrée par bucket de type présent (Task / User Story / Autres) — un bucket absent des work items de l'Iteration n'apparaît pas (US1 Acceptance Scenario 2 / US2 Acceptance Scenario 2). |
| `TotalPlanifie` | `int` | Nombre total de work items non `Removed` dans l'Iteration (tous types confondus). |
| `TotalTermine` | `int` | Nombre de work items en catégorie d'état `Completed` (tous types confondus). |

## RepartitionType

| Champ | Type | Description |
|---|---|---|
| `Type` | `string` | `"Task"`, `"UserStory"`, ou `"Autres"` (research.md#2). |
| `AFaire` | `int` | Work items de ce type en catégorie d'état `Proposed`. |
| `EnCours` | `int` | Work items de ce type en catégorie d'état `InProgress` ou `Resolved`. |
| `Termine` | `int` | Work items de ce type en catégorie d'état `Completed`. |

Les work items en catégorie `Removed` ne sont comptés dans aucun des trois buckets d'état
(research.md#1) — un type dont tous les work items sont `Removed` n'apparaît pas dans
`RepartitionParType`.

## Types internes Azure DevOps (non exposés à l'API)

- `AzureDevOpsWorkItemSummary` (étendu, research.md#3) : `Id`, `Titre` (déjà existant), `Type`
  (`System.WorkItemType`), `Etat` (`System.State`).
- `AzureDevOpsEtatCategorie` : mapping `Etat → Categorie` (`Proposed`/`InProgress`/`Resolved`/
  `Completed`/`Removed`) pour un type de work item donné, lu via
  `AzureDevOpsClient.ObtenirEtatsAsync` (research.md#1).
