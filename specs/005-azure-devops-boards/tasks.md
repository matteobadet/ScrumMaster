# Tasks: Intégration Azure DevOps Boards

**Input**: Design documents from `/specs/005-azure-devops-boards/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, et la feature
specs/001-retro-board-base déjà implémentée (réutilise `ScrumMaster.Api`, `ScrumMasterDbContext`,
`Equipe`, `PostIt`, `RetroBoardHub`).

**Tests**: Incluses (tests d'intégration ciblés par user story, avec un `HttpMessageHandler`
factice simulant l'API Azure DevOps — voir `plan.md`, section Testing).

**Organization**: Tâches groupées par user story (P1 → P4 de `spec.md`) pour permettre une
implémentation et une validation indépendantes de chacune.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Peut s'exécuter en parallèle (fichiers différents, aucune dépendance non résolue)
- **[Story]**: User story associée (US1, US2, US3, US4)

## Path Conventions

Extension du backend/frontend existants (voir `plan.md` — Project Structure) :
`backend/src/ScrumMaster.Api/`, `backend/tests/ScrumMaster.Api.Tests/`, `frontend/src/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Nouvelle dépendance requise par le chiffrement du PAT.

- [X] T001 Ajouter le paquet `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` à
      `backend/src/ScrumMaster.Api/ScrumMaster.Api.csproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Modèle de données, chiffrement du PAT et client HTTP partagés par les quatre user
stories.

**⚠️ CRITICAL**: Aucune user story ne démarre avant la fin de cette phase.

- [X] T002 [P] Créer le modèle `ConfigurationAzureDevOps` (AreaPath, Organisation, Projet,
      PatChiffre, DateConfiguration) dans
      `backend/src/ScrumMaster.Api/Models/ConfigurationAzureDevOps.cs`
- [X] T003 [P] Étendre le modèle `PostIt` avec `WorkItemSourceId` et `WorkItemExporteId`
      (int?, nullable) dans `backend/src/ScrumMaster.Api/Models/PostIt.cs`
- [X] T004 Configurer dans `ScrumMasterDbContext` le nouveau `DbSet<ConfigurationAzureDevOps>` et
      le mapping des nouveaux champs de `PostIt` dans
      `backend/src/ScrumMaster.Api/Data/ScrumMasterDbContext.cs` (dépend de T002, T003)
- [X] T005 [P] Configurer ASP.NET Core Data Protection avec persistance de l'anneau de clés dans
      PostgreSQL (`PersistKeysToDbContext`) dans `backend/src/ScrumMaster.Api/Program.cs` (dépend
      de T001)
- [X] T006 Générer et appliquer la migration EF Core (table `ConfigurationsAzureDevOps`, colonnes
      `PostIts`, table `DataProtectionKeys`) dans
      `backend/src/ScrumMaster.Api/Data/Migrations/` (dépend de T004, T005)
- [X] T007 [P] Créer le squelette `AzureDevOpsClient` (HttpClient typé, authentification Basic PAT)
      et les DTOs de désérialisation, l'enregistrer en DI dans
      `backend/src/ScrumMaster.Api/AzureDevOps/{AzureDevOpsClient.cs,AzureDevOpsDtos.cs}` et
      `backend/src/ScrumMaster.Api/Program.cs`

**Checkpoint**: Fondations prêtes — l'implémentation des user stories peut commencer.

---

## Phase 3: User Story 1 - Configurer l'accès Azure DevOps de l'équipe (Priority: P1) 🎯 MVP

**Goal**: Un membre de l'équipe configure l'organisation, le projet et un PAT Azure DevOps pour
son équipe, validés à l'enregistrement et stockés chiffrés.

**Independent Test**: Configurer l'accès pour une équipe avec un PAT valide (mocké) et vérifier
l'enregistrement sans jamais exposer le PAT ; tenter avec un PAT invalide et vérifier le rejet.

### Tests for User Story 1

- [X] T008 [P] [US1] Test d'intégration : `PUT .../azure-devops-config` avec un PAT valide (Azure
      DevOps mocké) enregistre la configuration sans jamais exposer le PAT dans la réponse ; un
      PAT invalide est rejeté (400) sans exposer le PAT dans le message d'erreur (FR-001 à FR-004)
      dans `backend/tests/ScrumMaster.Api.Tests/AzureDevOpsConfigTests.cs`

### Implementation for User Story 1

- [X] T009 [US1] Implémenter `AzureDevOpsClient.ValiderAccesAsync` (`GET .../_apis/projects/{projet}`,
      Basic Auth) dans `backend/src/ScrumMaster.Api/AzureDevOps/AzureDevOpsClient.cs` (dépend de
      T007)
- [X] T010 [US1] Implémenter `AzureDevOpsConfigService` (appel de validation, chiffrement du PAT
      via `IDataProtector`, upsert de `ConfigurationAzureDevOps`) dans
      `backend/src/ScrumMaster.Api/Services/AzureDevOpsConfigService.cs` (dépend de T009, T006)
- [X] T011 [US1] Implémenter `AzureDevOpsController` avec l'endpoint
      `PUT /api/equipes/{areaPath}/azure-devops-config` dans
      `backend/src/ScrumMaster.Api/Controllers/AzureDevOpsController.cs` (dépend de T010)
- [X] T012 [P] [US1] Créer `AzureDevOpsConfigPage.tsx` (formulaire organisation/projet/PAT, appelle
      l'endpoint, n'affiche jamais le PAT après soumission) et sa route dans
      `frontend/src/pages/AzureDevOpsConfigPage.tsx` et `frontend/src/App.tsx`
- [X] T013 [US1] Ajouter un lien vers `AzureDevOpsConfigPage` depuis `BoardPage.tsx` dans
      `frontend/src/pages/BoardPage.tsx` (dépend de T012)

**Checkpoint**: User Story 1 fonctionnelle de façon autonome.

---

## Phase 4: User Story 2 - Choisir l'Area Path et l'Iteration à la création d'un board (Priority: P2)

**Goal**: À la création d'un board, le facilitateur choisit l'Area Path parmi les équipes déjà
configurées et l'Iteration parmi les données réelles d'Azure DevOps, avec le sprint en cours
présélectionné ; repli en texte libre si non configuré ou si Azure DevOps est injoignable.

**Independent Test**: Créer un board pour une équipe configurée et vérifier la sélection guidée
avec le sprint en cours présélectionné ; créer un board pour une équipe non configurée et vérifier
le repli en texte libre.

### Tests for User Story 2

- [X] T014 [P] [US2] Test d'intégration : `GET /api/equipes/avec-azure-devops` liste les équipes
      configurées ; `GET .../iterations` renvoie la liste avec l'indicateur `enCours` correctement
      calculé à partir de dates mockées, 404 si non configurée, erreur gérée si Azure DevOps
      injoignable (FR-005, FR-005a, FR-007) dans
      `backend/tests/ScrumMaster.Api.Tests/AzureDevOpsIterationsTests.cs`

### Implementation for User Story 2

- [X] T015 [US2] Implémenter `AzureDevOpsClient.ListerIterationsAsync`
      (`.../_apis/wit/classificationnodes/iterations`, calcul de l'Iteration en cours à partir des
      dates) dans `backend/src/ScrumMaster.Api/AzureDevOps/AzureDevOpsClient.cs` (dépend de T007,
      séquentiel après T009 — même fichier)
- [X] T016 [US2] Implémenter dans `AzureDevOpsBoardService` la liste des équipes configurées et la
      récupération des Iterations dans
      `backend/src/ScrumMaster.Api/Services/AzureDevOpsBoardService.cs` (dépend de T015)
- [X] T017 [US2] Ajouter à `AzureDevOpsController` les endpoints
      `GET /api/equipes/avec-azure-devops` et `GET .../azure-devops/iterations` dans
      `backend/src/ScrumMaster.Api/Controllers/AzureDevOpsController.cs` (dépend de T016,
      séquentiel après T011 — même fichier)
- [X] T018 [P] [US2] Étendre `CreateBoardPage.tsx` : sélection guidée de l'Area Path (équipes
      configurées) et de l'Iteration (sprint en cours présélectionné) si disponible, repli en
      texte libre sinon, dans `frontend/src/pages/CreateBoardPage.tsx` (dépend de T017)

**Checkpoint**: User Stories 1 et 2 fonctionnelles ensemble.

---

## Phase 5: User Story 3 - Importer les work items du sprint comme post-its (Priority: P3)

**Goal**: Le facilitateur importe en une action les work items assignés à l'Iteration du board
comme post-its pré-remplis.

**Independent Test**: Déclencher l'import sur un board dont l'Iteration correspond à des work
items (mockés) et vérifier qu'un post-it apparaît par work item ; réimporter et vérifier l'absence
de doublon.

### Tests for User Story 3

- [X] T019 [P] [US3] Test d'intégration : `ImportWorkItems` crée un post-it par work item retourné
      (WIQL + détails mockés), ignore les work items déjà importés
      (`WorkItemSourceId`), et ne crée rien si aucun work item n'est trouvé (FR-008) dans
      `backend/tests/ScrumMaster.Api.Tests/ImportWorkItemsTests.cs`

### Implementation for User Story 3

- [X] T020 [US3] Implémenter `AzureDevOpsClient.ListerWorkItemsAsync` (requête WIQL sur
      `System.IterationPath`, puis récupération des titres) dans
      `backend/src/ScrumMaster.Api/AzureDevOps/AzureDevOpsClient.cs` (dépend de T015, séquentiel —
      même fichier)
- [X] T021 [US3] Implémenter `AzureDevOpsBoardService.ImporterWorkItemsAsync` (dédoublonnage par
      `WorkItemSourceId`, création des post-its dans la première colonne du thème) dans
      `backend/src/ScrumMaster.Api/Services/AzureDevOpsBoardService.cs` (dépend de T020,
      séquentiel après T016 — même fichier)
- [X] T022 [US3] Implémenter la méthode `ImportWorkItems` du hub (contrôle facilitateur, diffusion
      d'un `PostItAdded` par post-it créé) dans
      `backend/src/ScrumMaster.Api/Hubs/RetroBoardHub.cs` (dépend de T021)
- [X] T023 [P] [US3] Ajouter le bouton "Importer les work items" (facilitateur uniquement) dans
      `frontend/src/pages/BoardPage.tsx` (dépend de T022)

**Checkpoint**: User Stories 1 à 3 fonctionnelles ensemble.

---

## Phase 6: User Story 4 - Exporter un post-it comme nouveau work item (Priority: P4)

**Goal**: Le facilitateur exporte un post-it vers Azure DevOps comme nouveau work item, avec
protection contre le double export.

**Independent Test**: Exporter un post-it et vérifier la création d'un work item (mocké) avec le
texte du post-it comme titre ; tenter un second export du même post-it et vérifier le rejet.

### Tests for User Story 4

- [X] T024 [P] [US4] Test d'intégration : `ExportPostIt` crée un work item (réponse de création
      mockée), enregistre `WorkItemExporteId`, et rejette un second export du même post-it
      (FR-009, FR-010) dans `backend/tests/ScrumMaster.Api.Tests/ExportPostItTests.cs`

### Implementation for User Story 4

- [X] T025 [US4] Implémenter `AzureDevOpsClient.CreerWorkItemAsync`
      (`POST .../_apis/wit/workitems/$Task`, JSON Patch sur `System.Title`) dans
      `backend/src/ScrumMaster.Api/AzureDevOps/AzureDevOpsClient.cs` (dépend de T020, séquentiel —
      même fichier)
- [X] T026 [US4] Implémenter `AzureDevOpsBoardService.ExporterPostItAsync` (contrôle anti-doublon
      via `WorkItemExporteId`, création, enregistrement de l'id retourné) dans
      `backend/src/ScrumMaster.Api/Services/AzureDevOpsBoardService.cs` (dépend de T025,
      séquentiel après T021 — même fichier)
- [X] T027 [US4] Implémenter la méthode `ExportPostIt` du hub et l'événement `PostItExported` dans
      `backend/src/ScrumMaster.Api/Hubs/RetroBoardHub.cs` (dépend de T026, séquentiel après T022 —
      même fichier)
- [X] T028 [P] [US4] Ajouter l'action "Exporter vers Azure DevOps" et le badge "exporté" dans
      `frontend/src/components/PostIt.tsx`, et la gestion de l'événement `PostItExported` dans
      `frontend/src/hooks/useRealtimeBoard.ts` (dépend de T027)

**Checkpoint**: Les quatre user stories sont fonctionnelles ensemble.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validation complète de la feature.

- [X] T029 Exécuter la validation `quickstart.md` de bout en bout (projet Azure DevOps réel ou
      sandbox) et corriger les écarts constatés

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Aucune dépendance — démarre immédiatement
- **Foundational (Phase 2)**: Dépend de Setup — bloque toutes les user stories
- **User Stories (Phase 3-6)**: Dépendent toutes de Foundational ; chaque story suivante ajoute
  ses propres méthodes à `AzureDevOpsClient.cs`, `AzureDevOpsBoardService.cs`,
  `AzureDevOpsController.cs`/`RetroBoardHub.cs` de façon séquentielle (mêmes fichiers), mais reste
  fonctionnellement indépendante et testable seule après sa propre phase
- **Polish (Phase 7)**: Dépend des quatre user stories

### User Story Dependencies

- **US1 (P1)**: Démarre après Foundational — aucune dépendance fonctionnelle aux autres stories
- **US2 (P2)**: Démarre après Foundational — indépendante fonctionnellement d'US1 (une équipe non
  configurée reste sur le comportement actuel), mais partage les fichiers `AzureDevOpsClient.cs`
  et `AzureDevOpsController.cs`
- **US3 (P3)**: Démarre après Foundational — suppose qu'un board a été créé avec une Iteration
  réelle (US2) pour produire un résultat utile, mais reste testable seule (retourne simplement
  aucun post-it sinon, `research.md#5`)
- **US4 (P4)**: Démarre après Foundational — totalement indépendante des work items importés
  (US3) ; s'applique à n'importe quel post-it, importé ou non

### Parallel Opportunities

- T002, T003, T007 [P] (Foundational) en parallèle
- T005 [P] (Data Protection) en parallèle du reste de Foundational
- Les tests marqués `[P]` de chaque story en parallèle entre eux
- Le travail frontend de chaque story ([P]) peut avancer en parallèle du backend de la story
  suivante une fois son contrat (contracts/) stabilisé

---

## Parallel Example: Foundational

```bash
Task: "Créer le modèle ConfigurationAzureDevOps dans backend/src/ScrumMaster.Api/Models/ConfigurationAzureDevOps.cs"
Task: "Étendre le modèle PostIt dans backend/src/ScrumMaster.Api/Models/PostIt.cs"
Task: "Créer le squelette AzureDevOpsClient dans backend/src/ScrumMaster.Api/AzureDevOps/"
```

---

## Implementation Strategy

### MVP First (User Story 1 uniquement)

1. Compléter Phase 1 (Setup) et Phase 2 (Foundational)
2. Compléter Phase 3 (User Story 1)
3. **STOP et VALIDER** : une équipe peut configurer son accès Azure DevOps, PAT chiffré et jamais
   exposé
4. Démontrer / déployer si prêt

### Incremental Delivery

1. Setup + Foundational → fondations prêtes
2. + US1 → tester (configuration) → démontrer (MVP)
3. + US2 → tester (sélection guidée) → démontrer
4. + US3 → tester (import) → démontrer
5. + US4 → tester (export) → démontrer
6. + Polish (validation quickstart complète)

---

## Notes

- `[P]` = fichiers différents, aucune dépendance non résolue
- Chaque story doit rester indépendamment testable (voir "Independent Test" de chaque phase)
- Committer après chaque tâche ou groupe logique de tâches
- S'arrêter à chaque checkpoint pour valider la story avant de poursuivre
- Le PAT ne DOIT jamais être journalisé, même en environnement de test (FR-002) — vérifié dans
  `AzureDevOpsConfigTests.cs` (le corps de réponse et le `PatChiffre` stocké ne contiennent jamais
  la valeur en clair).

## Implémentation — notes

- Toutes les phases ont été implémentées en une seule passe.
- Écart technique découvert en cours de route : l'extension `PersistKeysToDbContext<TContext>` du
  paquet `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` vit dans le namespace racine
  `Microsoft.AspNetCore.DataProtection` (pas `.EntityFrameworkCore` comme on pourrait s'y
  attendre) — `ScrumMasterDbContext` implémente aussi `IDataProtectionKeyContext` (contrainte
  générique requise par cette méthode), ajout non prévu explicitement dans `plan.md` mais
  nécessaire au fonctionnement de `research.md#2`.
- `PostItDto` (specs/001-retro-board-base) a été étendu avec `WorkItemExporteId` (en plus des
  nouveaux endpoints/événements listés dans `contracts/`) pour que le badge "exporté" soit visible
  dès le chargement initial du board (`GET /api/boards/{id}`), pas seulement via l'événement
  `PostItExported` reçu en direct pendant la session en cours.
- Tests : `StubAzureDevOpsHandler` (nouveau, `backend/tests/ScrumMaster.Api.Tests/`) remplace le
  `HttpClient` d'`AzureDevOpsClient` dans `TestWebApplicationFactory` — aucun appel réseau réel
  dans les 12 nouveaux tests (T008, T014, T019, T024). 61/61 tests passent, aucune régression.
- T029 (validation `quickstart.md`) : les scénarios 1-2 (configuration, PAT invalide) ont été
  vérifiés dans le navigateur contre l'API Azure DevOps réelle (organisation fictive → échec de
  validation propre, sans exposition du PAT, conforme à FR-002/FR-003) ; le scénario "import
  sans configuration" a été vérifié (rejet propre, board non cassé après rechargement). Les
  scénarios 3, 5-8 (sélection guidée, import réel, export réel) nécessitent un projet Azure DevOps
  réel avec des work items existants, non disponible dans cet environnement — leur logique est
  couverte par les tests automatisés (réponses HTTP mockées) mais n'a pas été rejouée contre une
  vraie instance Azure DevOps.
- Écart d'UX **pré-existant** (non introduit par cette feature) découvert pendant la
  vérification : toute erreur d'action temps réel (`invoke` dans `useRealtimeBoard.ts`, y compris
  `ImportWorkItems`/`ExportPostIt`) remplace toute la vue du board par le seul message d'erreur,
  au lieu d'un bandeau non bloquant — affecte aussi `ChangeTheme`/`Vote`/etc. déjà existants.
  Signalé comme tâche de suivi séparée plutôt que corrigé ici (hors périmètre de cette feature).
