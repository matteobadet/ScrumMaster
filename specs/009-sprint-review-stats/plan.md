# Implementation Plan: Point de sprint (stats Azure DevOps)

**Branch**: `009-sprint-review-stats` | **Date**: 2026-08-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/009-sprint-review-stats/spec.md`

## Summary

Ajouter un panneau "Point de sprint" consultable à tout moment sur la page d'un board, affichant la
répartition des work items de l'Iteration du board par état (à faire/en cours/terminé) et par type
(Task/User Story/Autres), ainsi qu'un taux de complétion global. Calculé à la demande depuis
l'API Azure DevOps via la configuration déjà en place (specs/005-azure-devops-boards), sans aucune
nouvelle authentification ni donnée persistée.

## Technical Context

**Language/Version**: C# / .NET 8 (backend), TypeScript / React 19 (frontend) — inchangé.

**Primary Dependencies**: ASP.NET Core, EF Core 8, Npgsql (backend, réutilisés, aucune nouvelle
dépendance) ; React + Vite (frontend, réutilisés).

**Storage**: N/A pour cette feature — aucune nouvelle table, lecture à la demande depuis l'API REST
Azure DevOps (voir research.md#4).

**Testing**: xUnit + `Microsoft.AspNetCore.Mvc.Testing` + `StubAzureDevOpsHandler` (déjà en place
pour specs/005-azure-devops-boards, réutilisé pour simuler les réponses `states` et `workitems`).

**Target Platform**: identique au reste du projet (k3s, Traefik).

**Project Type**: web application (backend + frontend), extension de l'existant.

**Performance Goals**: répartition affichée en moins de 5 secondes après ouverture (SC-001) — un
seul aller-retour WIQL+batch (déjà existant) plus un aller-retour `states` par type distinct présent
(typiquement 1 à 3 appels), pas de pagination nécessaire à l'échelle d'un sprint.

**Constraints**: aucune nouvelle donnée sensible (PAT déjà chiffré, réutilisé tel quel) ; lecture
seule stricte (FR-009, pas d'appel d'écriture Azure DevOps).

**Scale/Scope**: un sprint = quelques dizaines de work items au plus — pas de pagination ni de
cache nécessaires pour ce MVP.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Statut | Justification |
|---|---|---|
| I. Développement piloté par les spécifications | PASS | spec.md validé avant ce plan ; aucune clarification bloquante restée sans réponse (défauts documentés en Assumptions, cohérents avec l'existant). |
| II. Stack technique standardisée | PASS | ASP.NET Core (C#) backend, React frontend, PostgreSQL — aucun écart ; aucune nouvelle donnée persistée n'ajoute de dépendance à PostgreSQL au-delà de l'existant. |
| III. MVP avant tout | PASS | S'appuie sur la Phase 3 (Azure DevOps, specs/005) déjà livrée ; n'anticipe aucune phase ultérieure. |
| IV. Multi-tenant par conception | PASS | Scope systématiquement par `Board.AreaPath` → `ConfigurationAzureDevOps` (déjà tenant-scopé, aucune nouvelle entité à faire respecter cette contrainte). |
| V. Isolation du déploiement partagé | PASS | Aucun changement d'infrastructure/déploiement ; extension de code applicatif uniquement. |
| VI. Évolutivité sans sur-ingénierie | PASS | Pas de mapping de types configurable, pas de persistance/historique — périmètre minimal correspondant exactement aux 3 user stories (research.md#2). |

**Re-check post Phase 1 design**: PASS — le design (endpoint REST unique, extension ciblée du
client Azure DevOps existant, aucune nouvelle table) ne fait apparaître aucune violation nouvelle.

## Project Structure

### Documentation (this feature)

```text
specs/009-sprint-review-stats/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── rest-api-delta.md
└── tasks.md              # Phase 2 output (/speckit-tasks command)
```

### Source Code (repository root)

```text
backend/
├── src/ScrumMaster.Api/
│   ├── AzureDevOps/
│   │   ├── AzureDevOpsClient.cs         # MODIFIÉ : ListerWorkItemsAsync enrichi (Type/Etat), + ObtenirEtatsAsync
│   │   └── AzureDevOpsDtos.cs           # MODIFIÉ : AzureDevOpsWorkItemSummary enrichi, + DTOs d'états
│   ├── Dtos/
│   │   └── PointDeSprintDtos.cs         # NOUVEAU : PointDeSprintDto, RepartitionTypeDto
│   ├── Services/
│   │   └── AzureDevOpsBoardService.cs   # MODIFIÉ : ObtenirPointDeSprintAsync (nouvelle méthode)
│   └── Controllers/
│       └── BoardsController.cs          # MODIFIÉ : GET /api/boards/{boardId}/point-de-sprint
└── tests/ScrumMaster.Api.Tests/
    └── PointDeSprintTests.cs            # NOUVEAU

frontend/
└── src/
    ├── types.ts                          # MODIFIÉ : PointDeSprint, RepartitionType
    ├── services/boardsApi.ts             # MODIFIÉ : obtenirPointDeSprint
    ├── components/
    │   └── PointDeSprintPanel.tsx        # NOUVEAU
    └── pages/BoardPage.tsx               # MODIFIÉ : bouton + panneau, visible à tout participant
```

**Structure Decision**: Extension du backend/frontend existants (option "Web application" — déjà en
place pour tout le projet). Aucune nouvelle table EF Core, aucune migration nécessaire.

## Complexity Tracking

*Aucune violation de la Constitution — section non applicable.*
