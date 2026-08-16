# Implementation Plan: Board de Rétrospective Interactif de Base

**Branch**: `001-retro-board-base` | **Date**: 2026-08-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-retro-board-base/spec.md`

## Summary

Construire une application web autonome (accessible par lien, sans intégration Teams pour cette
itération) permettant à un facilitateur de créer un board de rétrospective (thème de colonnes,
Area Path + Iteration en champs libres) et à plusieurs participants de collaborer en temps réel :
ajout/édition/déplacement/suppression de post-its, vote borné, clôture en lecture seule par le
facilitateur. Approche technique : backend ASP.NET Core exposant une API REST (CRUD board/thème)
et un hub SignalR (diffusion temps réel des mutations), frontend React consommant les deux,
persistance PostgreSQL dédiée.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (backend) ; TypeScript 5 / React 18 (frontend)

**Primary Dependencies**: ASP.NET Core Web API, SignalR (temps réel), Entity Framework Core +
Npgsql (accès PostgreSQL) ; React, Vite (bundler/dev server), `@microsoft/signalr` (client SignalR)

**Storage**: PostgreSQL — base dédiée `scrummaster` sur l'instance Postgres existante du cluster
(nouvelle base, aucun schéma partagé avec SkillForge, conformément à la Constitution Principe V)

**Testing**: xUnit + `Microsoft.AspNetCore.Mvc.Testing` (backend, y compris tests d'intégration du
hub SignalR) ; Vitest + React Testing Library (frontend)

**Target Platform**: Conteneurs Linux sur le cluster k3s existant (VPS Ubuntu), exposés via
Traefik (ingress) avec certificat cert-manager ; navigateur web desktop moderne côté client

**Project Type**: Application web (frontend React SPA + backend ASP.NET Core API/SignalR)

**Performance Goals**: Propagation d'une mutation de post-it à tous les participants connectés en
moins de 3s (SC-002) ; création d'un board en moins de 1 min (SC-001)

**Constraints**: Aucun appel à l'API Azure DevOps dans ce MVP (FR-017 — Area Path/Iteration en
texte libre) ; aucune intégration Teams (Tab/SSO) dans ce MVP (FR-012) ; déploiement isolé du
namespace/DB SkillForge (Constitution Principe V) ; budget ressources VPS partagé (pas de
dimensionnement dédié à grande échelle)

**Scale/Scope**: MVP mono-équipe en usage réel ; le modèle de données porte déjà l'Area Path comme
identifiant d'équipe (Constitution Principe IV) ; jusqu'à 10 participants connectés simultanément
par board (SC-003) ; un board par Iteration/Sprint, plusieurs boards dans le temps par équipe

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Statut | Justification |
|----------|--------|----------------|
| I. Développement piloté par les spécifications | PASS | Ce plan fait suite à `spec.md` (validé) et `clarify` ; aucune implémentation n'a démarré avant ce document. |
| II. Stack technique standardisée | PASS | ASP.NET Core + React + PostgreSQL, sans écart. |
| III. MVP avant tout | PASS | Périmètre strictement limité au board de base (P1-P4 de la spec) ; poll, invitations, Azure Boards, extensions explicitement exclus. |
| IV. Multi-tenant par conception | PASS | `Board` et `Équipe` portent l'Area Path comme attribut explicite dès le modèle de données (voir `data-model.md`) ; toute requête sera scopée par Area Path. |
| V. Isolation du déploiement partagé | PASS (à honorer en implémentation) | Base PostgreSQL dédiée `scrummaster` distincte de SkillForge ; manifests Kustomize sous `k8s/` propres à ce projet (voir Project Structure). Aucun manifeste SkillForge n'est modifié par cette feature. |
| VI. Évolutivité sans sur-ingénierie | PASS | Le nombre de colonnes par thème est piloté par les données (aucune valeur codée en dur) ; aucune API de plugin n'est conçue ici. |

Aucune violation à justifier — la section Complexity Tracking reste vide.

**Re-check post Phase 1 (design)** : `data-model.md` confirme que `Équipe.AreaPath` et
`Board.AreaPath` portent l'identifiant multi-tenant dès le modèle (Principe IV) ; `contracts/`
n'introduit aucune dépendance à Azure DevOps ni à Teams (Principes II/III) ; `k8s/` reste séparé
de SkillForge (Principe V, à honorer lors de l'implémentation des manifests). Tous les principes
restent PASS après conception détaillée.

## Project Structure

### Documentation (this feature)

```text
specs/001-retro-board-base/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   ├── rest-api.md
│   └── realtime-hub.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── src/
│   └── ScrumMaster.Api/
│       ├── Controllers/       # Endpoints REST (boards, thèmes)
│       ├── Hubs/               # RetroBoardHub (SignalR)
│       ├── Models/             # Entités de domaine (Board, Colonne, PostIt, Vote, Participant, Theme, Equipe)
│       ├── Data/                # DbContext EF Core + migrations PostgreSQL
│       └── Services/           # Logique métier (validation post-it/thème, gestion des votes, clôture)
└── tests/
    └── ScrumMaster.Api.Tests/  # xUnit — tests unitaires + tests d'intégration (API + hub SignalR)

frontend/
├── src/
│   ├── components/             # Board, Colonne, PostIt, VoteCounter, ThemeEditor
│   ├── pages/                  # CreateBoardPage, BoardPage
│   ├── hooks/                  # useRealtimeBoard (connexion SignalR)
│   └── services/               # Client API REST + client SignalR
└── tests/                      # Vitest + React Testing Library

k8s/
├── base/                       # Manifests Kustomize communs (Deployment, Service, backend+frontend)
└── overlays/
    └── production/             # Ingress Traefik + certificat cert-manager, config spécifique VPS
```

**Structure Decision**: Option "Application web" (frontend + backend séparés). Le backend
ASP.NET Core sert à la fois l'API REST et le hub SignalR dans un seul processus/projet
(`ScrumMaster.Api`) pour limiter la complexité opérationnelle du MVP. Le frontend React est un
projet Vite indépendant, servi statiquement (build) derrière Traefik. Les manifests `k8s/` de ce
projet sont strictement séparés de ceux de SkillForge (autre dépôt/déploiement), conformément à la
Constitution Principe V.

## Complexity Tracking

> Aucune violation de la Constitution Check — section laissée vide intentionnellement.
