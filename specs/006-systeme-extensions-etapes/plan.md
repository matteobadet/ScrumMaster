# Implementation Plan: Système d'Extensions — Étapes de Rétrospective

**Branch**: `006-systeme-extensions-etapes` | **Date**: 2026-08-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/006-systeme-extensions-etapes/spec.md`

## Summary

Transforme un board de rétrospective d'un unique thème de colonnes en une séquence d'une ou
plusieurs étapes typées ("Colonnes et post-its", "Mini-jeu", "Poll personnalisé"), composée par le
facilitateur à la création sans écrire de code. C'est la feature la plus invasive de la roadmap :
elle restructure le modèle de données central (`Board`/`PostIt`) déjà utilisé par
specs/001-retro-board-base, specs/004-themes-narratifs et specs/005-azure-devops-boards. Approche
technique : une entité `Étape` par colonnes nullable (union étiquetée simple, pas de hiérarchie
polymorphe — Constitution Principe VI), portée des post-its/votes déplacée du board à l'étape,
migration de données pour les boards existants, consolidation de `CloseBoard` en `AvancerEtape`,
deux nouvelles entités de réponse (mini-jeu, poll personnalisé) réutilisant le pattern d'upsert
déjà éprouvé par specs/002-poll-utilite-reunion.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (backend) ; TypeScript 5 / React 18 (frontend) — inchangé

**Primary Dependencies**: Aucune nouvelle dépendance — réutilise EF Core + Npgsql, SignalR, React
déjà en place

**Storage**: PostgreSQL — migration EF Core avec **backfill de données** (pas seulement de schéma,
`research.md#3`) : nouvelle table `Etapes`, `PostIts.BoardId` renommé `EtapeId`, nouvelles tables
`MiniJeuxCatalogue`, `ReponsesMeteoEquipe`, `OptionsPollPersonnalise`, `ReponsesPollPersonnalise`.

**Testing**: xUnit + `Microsoft.AspNetCore.Mvc.Testing` + `TestAdapter`-style tests SignalR déjà en
place (specs/001-retro-board-base) — tests de non-régression prioritaires sur `ChangeTheme`,
`Vote`, `AddPostIt`, `ImportWorkItems`/`ExportPostIt` (specs/005) dont la portée change.

**Target Platform**: Inchangé — conteneurs Linux sur le cluster k3s existant

**Project Type**: Restructuration du backend `ScrumMaster.Api` et du frontend React déjà en place —
aucun nouveau projet

**Performance Goals**: Changement d'étape propagé à tous les participants en moins de 3s (SC-002,
même exigence que la propagation de mutation de specs/001)

**Constraints**: Aucune régression sur un board mono-étape (FR-014, SC-003) ; migration de données
sans perte de contenu pour les boards déjà en production ; pas de système de chargement dynamique
de plugins ni d'isolation d'exécution (Constitution Principe VI, `research.md#1`)

**Scale/Scope**: 3 user stories, 1 entité restructurante (`Étape`) touchant 3 features déjà
livrées, 4 nouvelles entités de contenu, 1 migration avec backfill, consolidation de 2 méthodes de
hub en 1, 2 nouvelles méthodes de hub — le plus grand changement de surface de code de la roadmap,
mais le plus petit changement de dépendances (aucune nouvelle)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Statut | Justification |
|----------|--------|----------------|
| I. Développement piloté par les spécifications | PASS | Ce plan fait suite à `spec.md` (validé) et `/speckit-clarify` ; aucune implémentation n'a démarré avant ce document. |
| II. Stack technique standardisée | PASS | ASP.NET Core + React + PostgreSQL, sans écart, aucune nouvelle dépendance. |
| III. MVP avant tout | PASS | Phase 4 (dernière) de la roadmap MVP — démarre uniquement après les Phases 1-3 déjà implémentées. |
| IV. Multi-tenant par conception | PASS | `Étape` hérite du scoping par équipe via `Board.AreaPath`, déjà en place ; aucun nouveau point d'entrée non scopé. |
| V. Isolation du déploiement partagé | PASS | Migration EF Core (avec backfill) sur la base `scrummaster` existante ; aucun manifeste k8s modifié. |
| VI. Évolutivité sans sur-ingénierie | PASS | C'est la spécification que ce principe réservait explicitement à cette phase. Union étiquetée simple plutôt que hiérarchie polymorphe (`research.md#1`) ; catalogue de mini-jeux en table de données plutôt qu'un chargeur de plugins dynamique (`research.md#6`) ; aucune isolation d'exécution introduite. |

Aucune violation à justifier — la section Complexity Tracking reste vide.

**Re-check post Phase 1 (design)** : `data-model.md` confirme que la restructuration reste une
union étiquetée à 3 branches fixes (Principe VI) ; `contracts/` montrent que la migration touche
des contrats internes déjà en place (REST/SignalR) sans introduire de nouveau protocole ; le
backfill de données (`research.md#3`) garantit l'absence de régression sur les boards déjà en
production (Principe V — pas de perte de contenu lors d'un déploiement). Tous les principes
restent PASS après conception détaillée.

## Project Structure

### Documentation (this feature)

```text
specs/006-systeme-extensions-etapes/
├── plan.md                      # This file (/speckit-plan command output)
├── research.md                  # Phase 0 output (/speckit-plan command)
├── data-model.md                # Phase 1 output (/speckit-plan command)
├── quickstart.md                # Phase 1 output (/speckit-plan command)
├── contracts/                   # Phase 1 output (/speckit-plan command)
│   ├── rest-api-delta.md
│   └── realtime-hub-delta.md
└── tasks.md                     # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── src/ScrumMaster.Api/
│   ├── Models/
│   │   ├── Etape.cs                          # nouvelle entité (Type, Ordre, Statut, ThemeId?, MiniJeuCatalogueId?, Question?)
│   │   ├── Board.cs                          # - ThemeId, + List<Etape> Etapes
│   │   ├── PostIt.cs                         # BoardId → EtapeId
│   │   ├── MiniJeuCatalogue.cs                # nouvelle entité
│   │   ├── ReponseMeteoEquipe.cs              # nouvelle entité (spécifique au mini-jeu seedé)
│   │   ├── OptionPollPersonnalise.cs          # nouvelle entité
│   │   └── ReponsePollPersonnalise.cs         # nouvelle entité
│   ├── Data/
│   │   ├── ScrumMasterDbContext.cs           # nouveaux DbSet, mapping Étape, contraintes d'unicité
│   │   ├── MiniJeuSeeder.cs                   # nouveau, même pattern que ThemeSeeder
│   │   └── Migrations/                        # migration avec backfill (research.md#3)
│   ├── Services/
│   │   ├── EtapeService.cs                    # nouveau : composition de séquence, AvancerEtape
│   │   ├── BoardService.cs                    # CreateBoardAsync construit la séquence d'étapes
│   │   ├── PostItService.cs, VoteService.cs   # portée révisée : EtapeId au lieu de BoardId
│   │   ├── MiniJeuService.cs                  # nouveau : RepondreMiniJeu (Météo d'équipe)
│   │   ├── PollPersonnaliseService.cs         # nouveau : RepondrePollPersonnalise
│   │   └── AzureDevOpsBoardService.cs         # resolution révisée : étape active (specs/005)
│   └── Hubs/RetroBoardHub.cs                  # CloseBoard → AvancerEtape, + RepondreMiniJeu/RepondrePollPersonnalise
└── tests/ScrumMaster.Api.Tests/                # tests de non-régression + nouveaux tests par story

frontend/
└── src/
    ├── types.ts                                # BoardState.etapes[] remplace theme/colonnes/postIts
    ├── pages/
    │   ├── CreateBoardPage.tsx                 # composition de séquence (liste d'étapes typées)
    │   └── BoardPage.tsx                       # rendu conditionnel par type d'étape active + navigation entre étapes
    └── components/
        ├── EtapeColonnesEtPostIts.tsx          # renommage/extraction du rendu board actuel (specs/001)
        ├── EtapeMiniJeuMeteo.tsx                # nouveau
        └── EtapePollPersonnalise.tsx            # nouveau
```

**Structure Decision**: Restructuration du backend/frontend déjà en place — aucun nouveau projet.
`Étape` devient le point d'articulation central ; les composants React existants pour
colonnes/post-its (specs/001) sont conservés et simplement rattachés à une étape plutôt qu'au
board directement.

## Complexity Tracking

> Aucune violation de la Constitution Check — section laissée vide intentionnellement. La taille
> du changement (Scale/Scope ci-dessus) est élevée, mais chaque décision individuelle
> (`research.md`) choisit délibérément l'option la plus simple satisfaisant `spec.md`.
