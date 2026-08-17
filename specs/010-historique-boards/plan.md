# Implementation Plan: Historique des boards par équipe

**Branch**: `010-historique-boards` | **Date**: 2026-08-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/010-historique-boards/spec.md`

## Summary

Exposer une liste des boards (actifs et clôturés) d'une équipe, triée du plus récent au plus
ancien, pour permettre de retrouver un board dont le lien a été perdu — notamment un board clôturé
dont on veut consulter les résultats. Projection en lecture seule de l'entité `Board` existante,
filtrée par Area Path, accessible via une page dédiée et des liens depuis les points d'entrée
existants (création de board, page d'un board).

## Technical Context

**Language/Version**: C# / .NET 8 (backend), TypeScript / React 19 (frontend) — inchangé.

**Primary Dependencies**: ASP.NET Core, EF Core 8 (backend, réutilisés) ; React + Vite + React
Router (frontend, réutilisés).

**Storage**: Aucune nouvelle table — lecture directe de `Board` (déjà persisté), filtrée par
`AreaPath` (research.md#3).

**Testing**: xUnit + `Microsoft.AspNetCore.Mvc.Testing` (`TestWebApplicationFactory`), cohérent
avec le reste du backend.

**Target Platform**: identique au reste du projet (k3s, Traefik).

**Project Type**: web application (backend + frontend), extension de l'existant.

**Performance Goals**: liste affichée en moins de 5 secondes (SC-001 implique une consultation
rapide) — une seule requête EF Core indexée par `AreaPath`, pas de jointure coûteuse.

**Constraints**: aucune authentification ajoutée (research.md, Assumptions de spec.md) ; lecture
seule stricte (FR-007, aucune nouvelle capacité d'édition).

**Scale/Scope**: quelques dizaines de boards par équipe au plus — pas de pagination nécessaire pour
ce MVP (Assumptions de spec.md).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Statut | Justification |
|---|---|---|
| I. Développement piloté par les spécifications | PASS | spec.md validé avant ce plan ; aucune clarification bloquante (défauts documentés en Assumptions, cohérents avec l'existant). |
| II. Stack technique standardisée | PASS | ASP.NET Core (C#) backend, React frontend — aucun écart ; aucune nouvelle table PostgreSQL. |
| III. MVP avant tout | PASS | Comble un manque explicitement laissé ouvert par specs/001-retro-board-base (Phase 1 déjà livrée) ; n'anticipe aucune phase ultérieure. |
| IV. Multi-tenant par conception | PASS | Filtrage systématique par `Board.AreaPath`, déjà l'identifiant de tenant établi — aucune nouvelle entité à faire respecter cette contrainte. |
| V. Isolation du déploiement partagé | PASS | Aucun changement d'infrastructure/déploiement ; extension de code applicatif uniquement. |
| VI. Évolutivité sans sur-ingénierie | PASS | Pas de pagination, pas de nouveau contrôleur pour une seule route, pas de temps réel — périmètre minimal correspondant exactement aux 2 user stories (research.md). |

**Re-check post Phase 1 design**: PASS — le design (un seul endpoint, une seule page, deux liens
d'entrée) ne fait apparaître aucune violation nouvelle.

## Project Structure

### Documentation (this feature)

```text
specs/010-historique-boards/
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
│   ├── Dtos/BoardDtos.cs                # MODIFIÉ : BoardSummaireDto
│   ├── Services/BoardService.cs         # MODIFIÉ : ListerBoardsParEquipeAsync
│   └── Controllers/BoardsController.cs  # MODIFIÉ : GET /api/equipes/{areaPath}/boards
└── tests/ScrumMaster.Api.Tests/
    └── HistoriqueBoardsTests.cs         # NOUVEAU

frontend/
└── src/
    ├── types.ts                          # MODIFIÉ : BoardSummary
    ├── services/boardsApi.ts             # MODIFIÉ : listerBoardsParEquipe
    ├── pages/
    │   └── BoardHistoryPage.tsx          # NOUVEAU
    ├── pages/CreateBoardPage.tsx         # MODIFIÉ : lien vers l'historique
    ├── pages/BoardPage.tsx               # MODIFIÉ : lien vers l'historique
    └── App.tsx                           # MODIFIÉ : route /equipe/:areaPath/boards
```

**Structure Decision**: Extension du backend/frontend existants (option "Web application" — déjà en
place pour tout le projet). Aucune nouvelle table EF Core, aucune migration nécessaire.

## Complexity Tracking

*Aucune violation de la Constitution — section non applicable.*
