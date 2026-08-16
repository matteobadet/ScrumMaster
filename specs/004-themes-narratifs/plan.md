# Implementation Plan: Thèmes de Rétrospective Narratifs

**Branch**: `004-themes-narratifs` | **Date**: 2026-08-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-themes-narratifs/spec.md`

## Summary

Étendre l'entité `Thème` déjà existante (specs/001-retro-board-base) avec deux attributs
facultatifs — une icône/emoji et un texte de contexte libre — disponibles aussi bien pour un
thème prédéfini que pour un thème personnalisé saisi par le facilitateur. Approche technique :
extension additive du modèle de données, des DTOs de transport déjà existants (REST + SignalR) et
des composants frontend d'édition/affichage du thème ; aucun nouvel endpoint, aucune nouvelle
méthode de hub, aucune nouvelle dépendance.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (backend) ; TypeScript 5 / React 18 (frontend) — inchangé par
rapport à specs/001-retro-board-base

**Primary Dependencies**: Aucune nouvelle dépendance — réutilise ASP.NET Core Web API, SignalR, EF
Core + Npgsql, React déjà en place

**Storage**: PostgreSQL — extension de la table `Themes` existante (deux colonnes nullable), même
base `scrummaster`, migration EF Core additive

**Testing**: xUnit + `Microsoft.AspNetCore.Mvc.Testing` (backend) ; Vitest + React Testing Library
(frontend) — mêmes outils que specs/001-retro-board-base

**Target Platform**: Inchangé — conteneurs Linux sur le cluster k3s existant

**Project Type**: Application web (frontend React SPA + backend ASP.NET Core API/SignalR) —
extension de l'existant, pas de nouveau projet

**Performance Goals**: Ajout d'icône/contexte en moins d'1 minute (SC-001) ; propagation du
changement de thème en cours de session dans le même délai que les mutations déjà temps réel de
specs/001-retro-board-base (quelques secondes)

**Constraints**: Champs facultatifs uniquement — aucune migration de contenu requise pour les
boards et thèmes existants (FR-005, SC-003) ; pas de bibliothèque d'icônes ni d'upload de fichier
(voir `research.md#1`)

**Scale/Scope**: Extension ciblée de 2 champs sur 1 entité déjà existante, propagés à travers 3
DTOs et 2 composants frontend déjà en place — aucun changement d'échelle par rapport à
specs/001-retro-board-base

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Statut | Justification |
|----------|--------|----------------|
| I. Développement piloté par les spécifications | PASS | Ce plan fait suite à `spec.md` (validé) et `/speckit-clarify` ; aucune implémentation n'a démarré avant ce document. |
| II. Stack technique standardisée | PASS | ASP.NET Core + React + PostgreSQL, sans écart, aucune nouvelle dépendance. |
| III. MVP avant tout | PASS | Reste dans le périmètre Phase 1 (board de rétrospective) de la roadmap MVP — n'anticipe pas les Phases 2-4. |
| IV. Multi-tenant par conception | PASS | N'affecte pas le scoping par Area Path déjà en place ; `Thème` n'est pas une entité tenant-scopée dans specs/001 et le reste ici. |
| V. Isolation du déploiement partagé | PASS | Migration EF Core additive sur la base `scrummaster` existante ; aucun manifeste k8s modifié. |
| VI. Évolutivité sans sur-ingénierie | PASS | Icône en texte libre plutôt qu'une bibliothèque d'icônes à maintenir (`research.md#1`) ; pas d'entité séparée pour l'habillage du thème (`research.md#2`). |

Aucune violation à justifier — la section Complexity Tracking reste vide.

**Re-check post Phase 1 (design)** : `data-model.md` confirme que `Icone`/`Contexte` sont deux
colonnes nullable sur `Theme`, sans nouvelle entité ni jointure (Principe VI) ; `contracts/`
étendent les DTOs et événements déjà existants sans nouvel endpoint ni méthode (Principe II/III) ;
aucun champ tenant-scopé n'est introduit (Principe IV, sans changement par rapport à specs/001).
Tous les principes restent PASS après conception détaillée.

## Project Structure

### Documentation (this feature)

```text
specs/004-themes-narratifs/
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
│   ├── Models/Theme.cs                 # + Icone, Contexte (nullable)
│   ├── Dtos/                           # ThemeSummaryDto, ThemePersonnaliseDto, ThemeRefDto + icone/contexte
│   ├── Services/BoardService.cs        # ResolveThemeAsync/CopyTheme : validation longueur + propagation
│   ├── Data/ThemeSeeder.cs             # inchangé (icone/contexte restent null pour les thèmes seedés existants)
│   └── Data/Migrations/                # nouvelle migration additive (2 colonnes nullable sur Themes)
└── tests/ScrumMaster.Api.Tests/        # tests d'intégration existants étendus (création/changement de thème)

frontend/
└── src/
    ├── types.ts                        # ThemeSummary, ThemePersonnalise, ThemeRef, ThemeSelection + icone/contexte
    ├── components/ThemeEditor.tsx      # champs de saisie icône/contexte pour le thème personnalisé
    └── pages/BoardPage.tsx             # affichage icône (en-tête) + bloc contexte (introduction du board)
```

**Structure Decision**: Extension pure de la structure "Application web" déjà en place
(specs/001-retro-board-base) — aucun nouveau projet, aucun nouveau répertoire de premier niveau.
Les modifications se limitent aux fichiers listés ci-dessus.

## Complexity Tracking

> Aucune violation de la Constitution Check — section laissée vide intentionnellement.
