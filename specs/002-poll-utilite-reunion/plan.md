# Implementation Plan: Poll d'Utilité de Réunion

**Branch**: `002-poll-utilite-reunion` | **Date**: 2026-08-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-poll-utilite-reunion/spec.md`

## Summary

Étendre le bot Teams de ScrumMaster (Bot Framework SDK, déjà validé en constitution) pour
permettre à une équipe d'associer son channel Teams à son Area Path, de déclencher un poll
d'utilité (mêlée ou rétrospective) par commande textuelle, de voter via une Adaptive Card
("Utile" / "Pas nécessaire", modifiable), et de clôturer le poll par commande pour faire
apparaître le résultat : la réunion est maintenue dès qu'au moins un vote "Utile" a été exprimé.
Approche technique : ajout d'un endpoint Bot Framework (`/api/messages`) au projet
`ScrumMaster.Api` existant (specs/001-retro-board-base), sans nouveau service ni frontend.

## Technical Context

**Language/Version**: C# 12 / .NET 8 — même projet `ScrumMaster.Api` que specs/001-retro-board-base

**Primary Dependencies**: Microsoft Bot Framework SDK
(`Microsoft.Bot.Builder`, `Microsoft.Bot.Builder.Integration.AspNet.Core`), `AdaptiveCards` (.NET)
pour la construction des cartes de poll ; Entity Framework Core (déjà en place)

**Storage**: PostgreSQL — même base `scrummaster` dédiée (specs/001-retro-board-base), nouvelles
tables pour les polls/votes et une colonne ajoutée sur `Equipe`

**Testing**: xUnit + `Microsoft.Bot.Builder.Adapters.TestAdapter` (simulation d'activités Bot
Framework sans tenant Teams réel), cohérent avec l'approche de test de specs/001-retro-board-base

**Target Platform**: Même déploiement k3s que specs/001-retro-board-base ; le endpoint bot est
exposé via le même Ingress/Service `scrummaster-api`, sur le chemin `/api/messages`

**Project Type**: Extension du backend existant (aucun nouveau projet, aucun frontend impliqué —
cette feature est intégralement pilotée par le bot Teams)

**Performance Goals**: Poll visible dans le channel en moins d'1 minute après déclenchement
(SC-001) ; vote possible en moins de 10 secondes depuis la réception (SC-002)

**Constraints**: Nécessite un enregistrement Azure Bot Service (App Registration) — ressource
Azure externe au cluster k3s, provisionnée manuellement (voir quickstart.md) ; les identifiants du
bot (`MicrosoftAppId`/`MicrosoftAppPassword`) sont fournis via un Secret Kubernetes distinct du
Secret de connexion PostgreSQL existant ; aucune donnée de cette feature n'est exposée via l'API
REST existante (feature pilotée uniquement par le bot)

**Scale/Scope**: MVP mono-équipe réelle, cohérent avec specs/001-retro-board-base ; le modèle de
données scope déjà chaque poll/vote par Area Path (Constitution Principe IV)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Statut | Justification |
|----------|--------|----------------|
| I. Développement piloté par les spécifications | PASS | Ce plan fait suite à `spec.md` (validé) et `/speckit-clarify` ; aucune implémentation n'a démarré avant ce document. |
| II. Stack technique standardisée | PASS | ASP.NET Core + Bot Framework SDK (explicitement prévu par la Constitution) + PostgreSQL, sans écart. |
| III. MVP avant tout | PASS | Cette feature correspond à la Phase 2 de l'ordre de construction (poll avant invitations/Azure Boards/extensions). Elle ne couvre volontairement que le poll, les invitations faisant l'objet d'une feature 003 séparée — un fractionnement plus fin de la Phase 2, mais qui respecte l'esprit du principe : aucune fonctionnalité de Phase 3 (Azure Boards) ou 4 (extensions) n'est commencée. |
| IV. Multi-tenant par conception | PASS | `Poll d'utilité` et `Vote d'utilité` sont scopés par Area Path dès le modèle de données (voir `data-model.md`), comme `Board`/`PostIt` en feature 001. |
| V. Isolation du déploiement partagé | PASS (à honorer en implémentation) | Même base `scrummaster` et même Service `scrummaster-api` que specs/001 (pas de nouveau composant à isoler) ; le Secret des identifiants Bot Framework est distinct du Secret de connexion PostgreSQL et reste dans le namespace `scrummaster`, séparé de SkillForge. |
| VI. Évolutivité sans sur-ingénierie | PASS | Déclenchement/clôture manuels retenus en clarify — pas de scheduler ni d'infrastructure de tâches planifiées construite sans besoin avéré. |

Aucune violation à justifier — la section Complexity Tracking reste vide.

**Re-check post Phase 1 (design)** : `data-model.md` confirme le scoping par Area Path pour
`PollUtilite`/`VoteUtilite` (Principe IV) ; `contracts/` ne modifie ni n'expose l'API REST
existante (Principe V — aucune interférence avec les endpoints de specs/001) ; aucun nouveau
service ni scheduler n'est introduit (Principe VI). Tous les principes restent PASS après
conception détaillée.

## Project Structure

### Documentation (this feature)

```text
specs/002-poll-utilite-reunion/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   ├── bot-commands.md
│   └── adaptive-cards.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── src/
│   └── ScrumMaster.Api/
│       ├── Bots/
│       │   └── RetroPollBot.cs        # ActivityHandler : commandes texte + actions de carte
│       ├── Cards/
│       │   └── PollCardBuilder.cs     # Construction des Adaptive Cards (poll, résultat)
│       ├── Models/
│       │   ├── Equipe.cs               # Étendu : + TeamsChannelId (nullable)
│       │   ├── PollUtilite.cs          # Nouveau
│       │   └── VoteUtilite.cs          # Nouveau
│       ├── Services/
│       │   └── PollService.cs         # Association channel, déclenchement, vote, clôture
│       ├── Data/
│       │   └── ScrumMasterDbContext.cs # Étendu : DbSet PollUtilite/VoteUtilite
│       └── Program.cs                  # Étendu : enregistrement Bot Framework + mapping /api/messages
└── tests/
    └── ScrumMaster.Api.Tests/
        └── (nouveaux tests utilisant TestAdapter)
```

**Structure Decision**: Extension pure du backend existant (`backend/src/ScrumMaster.Api`,
specs/001-retro-board-base) — aucun nouveau projet, aucun frontend. Le bot partage le même
`ScrumMasterDbContext`, la même base de données et le même Service/Ingress Kubernetes que l'API
REST/hub SignalR déjà en place ; seul un nouveau chemin (`/api/messages`) est ajouté à l'Ingress
de production (specs/001-retro-board-base, k8s/overlays/production/ingress.yaml) lors de
l'implémentation.

## Complexity Tracking

> Aucune violation de la Constitution Check — section laissée vide intentionnellement.
