# Implementation Plan: Rappel de Réunion Teams

**Branch**: `003-rappel-reunion-teams` | **Date**: 2026-08-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-rappel-reunion-teams/spec.md`

## Summary

Ajouter au bot Teams déjà existant (`RetroPollBot`, specs/002-poll-utilite-reunion) l'envoi d'un
message de rappel dans le channel de l'équipe, soit automatiquement à la suite d'une clôture de
poll dont le résultat est "réunion maintenue", soit manuellement via une nouvelle commande
`rappeler <mêlée|rétro>`. Un rappel par équipe/type de réunion/jour au maximum (FR-008), tracé par
une nouvelle entité `RappelEnvoye`. Approche technique : nouveau `RappelService` (logique de
dédoublonnage), extension de `RetroPollBot` (nouvelle commande + appel après clôture), aucune
Adaptive Card requise (message texte simple, FR-007) — aucune nouvelle dépendance.

## Technical Context

**Language/Version**: C# 12 / .NET 8 — inchangé par rapport à specs/002-poll-utilite-reunion

**Primary Dependencies**: Aucune nouvelle dépendance — réutilise Bot Framework SDK, EF Core +
Npgsql déjà en place

**Storage**: PostgreSQL — nouvelle table `RappelsEnvoyes` sur la base `scrummaster` existante,
migration EF Core additive

**Testing**: xUnit + `Microsoft.Bot.Builder.Adapters.TestAdapter` — même stratégie que
specs/002-poll-utilite-reunion/research.md#5

**Target Platform**: Inchangé — conteneurs Linux sur le cluster k3s existant, endpoint
`/api/messages` déjà exposé (specs/002-poll-utilite-reunion, Phase 6 Polish)

**Project Type**: Extension du backend `ScrumMaster.Api` existant — aucun nouveau projet

**Performance Goals**: Rappel visible dans le channel en moins d'1 minute après son déclenchement,
automatique ou manuel (SC-001, SC-002)

**Constraints**: Pas de création d'événement de calendrier Teams (Microsoft Graph), pas de gestion
de liste de participants individuels (FR-007) ; pas de messagerie proactive, le rappel automatique
s'exécute dans le même tour de conversation que la commande `clore` (research.md#1)

**Scale/Scope**: Une nouvelle entité, un nouveau service, une nouvelle commande bot, une extension
du comportement de la commande `clore` existante — périmètre comparable à une user story unique de
specs/002-poll-utilite-reunion

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Statut | Justification |
|----------|--------|----------------|
| I. Développement piloté par les spécifications | PASS | Ce plan fait suite à `spec.md` (validé) et `/speckit-clarify` ; aucune implémentation n'a démarré avant ce document. |
| II. Stack technique standardisée | PASS | ASP.NET Core + Bot Framework SDK + PostgreSQL, sans écart, aucune nouvelle dépendance. |
| III. MVP avant tout | PASS | Reste dans le périmètre Phase 2 (poll d'utilité et invitations Teams) de la roadmap MVP ; n'anticipe pas les Phases 3-4. |
| IV. Multi-tenant par conception | PASS | `RappelEnvoye` porte `AreaPath` comme attribut explicite dès le modèle de données, cohérent avec `PollUtilite`. |
| V. Isolation du déploiement partagé | PASS | Migration EF Core additive sur la base `scrummaster` existante ; aucun manifeste k8s modifié (endpoint `/api/messages` déjà exposé). |
| VI. Évolutivité sans sur-ingénierie | PASS | `RappelService` séparé de `PollService` pour ne pas coupler deux concepts distincts (`research.md#2`) ; dédoublonnage par contrainte d'unicité en base plutôt qu'un mécanisme de verrouillage ad hoc (`research.md#3`). |

Aucune violation à justifier — la section Complexity Tracking reste vide.

**Re-check post Phase 1 (design)** : `data-model.md` confirme que `RappelEnvoye` porte `AreaPath`
dès le modèle (Principe IV) et ne duplique pas la logique de `PollUtilite` (Principe VI) ;
`contracts/bot-commands-delta.md` n'introduit aucune dépendance à Microsoft Graph ni de gestion de
participants individuels (Principe III — hors périmètre MVP). Tous les principes restent PASS
après conception détaillée.

## Project Structure

### Documentation (this feature)

```text
specs/003-rappel-reunion-teams/
├── plan.md                      # This file (/speckit-plan command output)
├── research.md                  # Phase 0 output (/speckit-plan command)
├── data-model.md                # Phase 1 output (/speckit-plan command)
├── quickstart.md                # Phase 1 output (/speckit-plan command)
├── contracts/                   # Phase 1 output (/speckit-plan command)
│   └── bot-commands-delta.md
└── tasks.md                     # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── src/ScrumMaster.Api/
│   ├── Models/RappelEnvoye.cs          # nouvelle entité (Id, AreaPath, TypeReunion, Date, DateEnvoi)
│   ├── Data/ScrumMasterDbContext.cs    # + DbSet<RappelEnvoye>, contrainte d'unicité (AreaPath, TypeReunion, Date)
│   ├── Data/Migrations/                # nouvelle migration additive (table RappelsEnvoyes)
│   ├── Services/RappelService.cs       # nouveau : dédoublonnage + enregistrement (automatique et manuel)
│   ├── Program.cs                      # + enregistrement DI de RappelService (Scoped, comme PollService)
│   └── Bots/RetroPollBot.cs            # + commande "rappeler", + appel post-clôture dans TraiterCloreAsync
└── tests/ScrumMaster.Api.Tests/        # nouveaux tests d'intégration (TestAdapter), tests existants de clore étendus
```

**Structure Decision**: Extension pure du backend `ScrumMaster.Api` déjà en place
(specs/002-poll-utilite-reunion) — aucun nouveau projet, aucune nouvelle dépendance. Le bot reste
hébergé dans le même processus (research.md#1 de specs/002).

## Complexity Tracking

> Aucune violation de la Constitution Check — section laissée vide intentionnellement.
