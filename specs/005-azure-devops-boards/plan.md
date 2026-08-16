# Implementation Plan: Intégration Azure DevOps Boards

**Branch**: `005-azure-devops-boards` | **Date**: 2026-08-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/005-azure-devops-boards/spec.md`

## Summary

Connecter ScrumMaster à l'API REST Azure DevOps pour quatre capacités séquencées : (1) configurer
un accès par équipe (organisation, projet, PAT chiffré) ; (2) proposer l'Area Path et l'Iteration
à la création d'un board via une sélection guidée par les données réelles, avec repli en texte
libre si l'équipe n'est pas configurée ou si Azure DevOps est injoignable ; (3) importer les work
items de l'Iteration comme post-its ; (4) exporter un post-it comme nouveau work item. Approche
technique : `HttpClient` typé vers l'API REST Azure DevOps (pas de SDK officiel), chiffrement du
PAT via ASP.NET Core Data Protection avec anneau de clés persisté en base, nouveaux endpoints REST
pour la configuration/sélection guidée, nouvelles méthodes du hub SignalR existant pour
import/export (mutations de contenu temps réel).

## Technical Context

**Language/Version**: C# 12 / .NET 8 (backend) ; TypeScript 5 / React 18 (frontend) — inchangé

**Primary Dependencies**: `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` (nouvelle
dépendance, chiffrement du PAT — `research.md#2`) ; réutilise `HttpClient` (déjà enregistré,
specs/002-poll-utilite-reunion), EF Core + Npgsql, SignalR déjà en place. Aucun SDK Azure DevOps
officiel (`research.md#1`).

**Storage**: PostgreSQL — nouvelle table `ConfigurationsAzureDevOps`, deux colonnes nullable sur
`PostIts`, nouvelle table `DataProtectionKeys` (anneau de clés), migrations EF Core additives sur
la base `scrummaster` existante.

**Testing**: xUnit + `Microsoft.AspNetCore.Mvc.Testing`, avec un `HttpMessageHandler` factice pour
simuler l'API Azure DevOps dans les tests d'intégration (pas d'appel réseau réel en test) — même
stratégie de double technique que `TestAdapter` pour le Bot Framework (specs/002-poll-utilite-reunion).

**Target Platform**: Inchangé — conteneurs Linux sur le cluster k3s existant

**Project Type**: Extension du backend `ScrumMaster.Api` et du frontend React déjà en place —
aucun nouveau projet

**Performance Goals**: Sélection guidée (Area Path/Iteration) répondant en moins de 5s (SC-002) ;
import/export en moins d'1 minute (SC-003, SC-004)

**Constraints**: Le PAT ne DOIT jamais apparaître en clair dans les logs, messages d'erreur ou
réponses API (FR-002, contrainte de la constitution) ; aucun blocage de la création de board en
cas d'échec Azure DevOps (FR-007) ; import/export réservés au facilitateur (FR-011) ; pas de mise
à jour/clôture de work items existants (FR-012)

**Scale/Scope**: 4 user stories, une nouvelle entité, extension de 2 entités existantes
(`PostIt`), 3 nouveaux endpoints REST, 2 nouvelles méthodes de hub — périmètre le plus large de la
roadmap jusqu'ici (comparable à specs/002-poll-utilite-reunion en volume, mais avec une nouvelle
dépendance externe HTTP)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Statut | Justification |
|----------|--------|----------------|
| I. Développement piloté par les spécifications | PASS | Ce plan fait suite à `spec.md` (validé) et `/speckit-clarify` (2 questions résolues) ; aucune implémentation n'a démarré avant ce document. |
| II. Stack technique standardisée | PASS | ASP.NET Core + React + PostgreSQL, sans écart. Seule nouvelle dépendance : `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`, une extension officielle du framework standardisé, pas un écart de stack. |
| III. MVP avant tout | PASS | Correspond exactement à la Phase 3 de la roadmap MVP de la constitution ; ne commence qu'après les Phases 1-2 (board, poll/invitations/thèmes/rappel) déjà implémentées. |
| IV. Multi-tenant par conception | PASS | `ConfigurationAzureDevOps` porte `AreaPath` comme clé, cohérent avec `Equipe`/`PollUtilite`/`RappelEnvoye` déjà scopés par équipe. |
| V. Isolation du déploiement partagé | PASS | Migrations EF Core additives sur la base `scrummaster` existante ; aucun manifeste k8s SkillForge modifié. Le PAT est un secret applicatif stocké chiffré en base, distinct des Secrets Kubernetes déjà utilisés pour la connexion DB et Bot Framework. |
| VI. Évolutivité sans sur-ingénierie | PASS | `HttpClient` typé plutôt que SDK complet (`research.md#1`) ; Data Protection plutôt que chiffrement maison (`research.md#2`) ; sélection guidée limitée aux équipes déjà configurées plutôt qu'un sélecteur Organisation/Projet à 3 niveaux (`research.md#3`). |

Aucune violation à justifier — la section Complexity Tracking reste vide.

**Re-check post Phase 1 (design)** : `data-model.md` confirme que `ConfigurationAzureDevOps` reste
scopée par `AreaPath` (Principe IV) et que l'extension de `PostIt` (2 colonnes nullable) ne casse
aucune donnée existante (Principe V — migration additive uniquement) ; `contracts/` réutilisent
les mécanismes REST/SignalR déjà établis par specs/001-retro-board-base sans nouveau protocole
(Principe VI). Tous les principes restent PASS après conception détaillée.

## Project Structure

### Documentation (this feature)

```text
specs/005-azure-devops-boards/
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
│   │   ├── ConfigurationAzureDevOps.cs      # nouvelle entité (AreaPath, Organisation, Projet, PatChiffre, DateConfiguration)
│   │   └── PostIt.cs                        # + WorkItemSourceId, WorkItemExporteId (nullable)
│   ├── Data/
│   │   ├── ScrumMasterDbContext.cs          # + DbSet<ConfigurationAzureDevOps>, mapping PostIt étendu
│   │   └── Migrations/                       # migrations additives (table config, colonnes PostIt, DataProtectionKeys)
│   ├── AzureDevOps/
│   │   ├── AzureDevOpsClient.cs             # HttpClient typé : valider PAT, lister Area Paths/Iterations, WIQL, créer work item
│   │   └── AzureDevOpsDtos.cs               # DTOs de désérialisation des réponses Azure DevOps
│   ├── Services/
│   │   ├── AzureDevOpsConfigService.cs      # US1 : configurer/remplacer l'accès (chiffrement PAT, appel de validation)
│   │   └── AzureDevOpsBoardService.cs       # US2-US4 : sélection guidée, import, export
│   ├── Controllers/
│   │   └── AzureDevOpsController.cs         # PUT config, GET équipes configurées, GET iterations
│   ├── Hubs/RetroBoardHub.cs                # + ImportWorkItems, ExportPostIt
│   └── Program.cs                            # + DataProtection (persistance EF Core), + services DI
└── tests/ScrumMaster.Api.Tests/              # tests d'intégration avec HttpMessageHandler factice pour Azure DevOps

frontend/
└── src/
    ├── types.ts                              # + types config Azure DevOps, extension ThemeSelection/BoardState/PostItState
    ├── pages/
    │   ├── AzureDevOpsConfigPage.tsx         # nouvelle page : configurer l'accès de l'équipe (US1)
    │   └── CreateBoardPage.tsx               # + sélection guidée Area Path/Iteration si équipe configurée, repli texte libre sinon
    ├── components/PostIt.tsx                 # + action "Exporter vers Azure DevOps", badge "exporté"
    └── pages/BoardPage.tsx                    # + action "Importer les work items" (facilitateur), lien vers la config Azure DevOps
```

**Structure Decision**: Extension du backend `ScrumMaster.Api` et du frontend React déjà en place.
Nouveau sous-dossier `AzureDevOps/` dans le backend pour isoler le client HTTP externe et ses DTOs
de désérialisation, à l'écart des `Services/` métier — limite le couplage entre la logique domaine
(spécifique à ScrumMaster) et le format des réponses Azure DevOps (susceptible d'évoluer
indépendamment).

## Complexity Tracking

> Aucune violation de la Constitution Check — section laissée vide intentionnellement.
