# Tasks: Board de Rétrospective Interactif de Base

**Input**: Design documents from `/specs/001-retro-board-base/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Incluses (tests d'intégration ciblés par user story, alignés sur la stratégie de test
définie dans `research.md#5`) — pas de mode TDD strict imposé, mais à écrire avant l'implémentation
de la même story dans la mesure du possible.

**Organization**: Tâches groupées par user story (P1 → P4 de `spec.md`) pour permettre une
implémentation et une validation indépendantes de chacune.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Peut s'exécuter en parallèle (fichiers différents, aucune dépendance non résolue)
- **[Story]**: User story associée (US1, US2, US3, US4)

## Path Conventions

Application web (voir `plan.md` — Project Structure) : `backend/src/ScrumMaster.Api/`,
`backend/tests/ScrumMaster.Api.Tests/`, `frontend/src/`, `k8s/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialisation des projets backend, frontend et de la structure de déploiement.

- [X] T001 Créer la structure de dépôt `backend/` et `frontend/` à la racine, conforme à `plan.md`
- [X] T002 [P] Initialiser `backend/src/ScrumMaster.Api` comme projet ASP.NET Core Web API
      (.NET 8) avec un fichier de solution `ScrumMaster.sln` à la racine
- [X] T003 [P] Initialiser `backend/tests/ScrumMaster.Api.Tests` comme projet de tests xUnit
      référençant `ScrumMaster.Api`
- [X] T004 [P] Initialiser `frontend/` comme projet Vite + React + TypeScript (le scaffold Vite
      actuel installe React 19, compatible ; plan.md à mettre à jour en conséquence)
- [X] T005 [P] Configurer le linting/formatage : `.editorconfig` pour le backend, oxlint +
      Prettier pour le frontend (le template Vite fournit oxlint plutôt qu'ESLint)
- [X] T006 [P] Scaffolder `k8s/base/kustomization.yaml` (structure Kustomize vide), isolée de
      tout manifeste SkillForge (Constitution Principe V)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Modèle de données, persistance et infrastructure temps réel partagés par toutes les
user stories.

**⚠️ CRITICAL**: Aucune user story ne démarre avant la fin de cette phase.

- [X] T007 Ajouter les paquets EF Core + Npgsql à `ScrumMaster.Api` et configurer la chaîne de
      connexion PostgreSQL (base dédiée `scrummaster`) via la configuration d'environnement
- [X] T008 Créer le squelette de `ScrumMasterDbContext` dans
      `backend/src/ScrumMaster.Api/Data/ScrumMasterDbContext.cs`
- [X] T009 [P] Créer le modèle `Equipe` dans `backend/src/ScrumMaster.Api/Models/Equipe.cs`
- [X] T010 [P] Créer le modèle `Theme` dans `backend/src/ScrumMaster.Api/Models/Theme.cs`
- [X] T011 [P] Créer le modèle `Colonne` dans `backend/src/ScrumMaster.Api/Models/Colonne.cs`
- [X] T012 [P] Créer le modèle `Board` dans `backend/src/ScrumMaster.Api/Models/Board.cs`
- [X] T013 [P] Créer le modèle `Participant` dans
      `backend/src/ScrumMaster.Api/Models/Participant.cs`
- [X] T014 [P] Créer le modèle `PostIt` dans `backend/src/ScrumMaster.Api/Models/PostIt.cs`
- [X] T015 [P] Créer le modèle `Vote` dans `backend/src/ScrumMaster.Api/Models/Vote.cs`
- [X] T016 Configurer dans `ScrumMasterDbContext` les relations, la contrainte d'unicité
      `(PostItId, ParticipantId)` sur `Vote`, et les champs obligatoires (dépend de T009-T015)
- [X] T017 Générer et appliquer la migration EF Core initiale du schéma dans
      `backend/src/ScrumMaster.Api/Data/Migrations/` (dépend de T016)
- [X] T018 Semer les thèmes prédéfinis (Start/Stop/Continue, Mad/Sad/Glad) dans
      `backend/src/ScrumMaster.Api/Data/ThemeSeeder.cs` (dépend de T017)
- [X] T019 Enregistrer EF Core et SignalR, et mapper la route du hub `/hubs/retro-board` dans
      `backend/src/ScrumMaster.Api/Program.cs` (dépend de T007)
- [X] T020 [P] Créer le client REST de base dans `frontend/src/services/apiClient.ts`
- [X] T021 [P] Créer le wrapper client SignalR dans `frontend/src/services/realtimeClient.ts`

**Checkpoint**: Fondations prêtes — l'implémentation des user stories peut commencer.

---

## Phase 3: User Story 1 - Créer un board et y noter des post-its (Priority: P1) 🎯 MVP

**Goal**: Un facilitateur crée un board avec un thème (par défaut ou choisi) et gère ses propres
post-its (ajout, édition, suppression), seul, sans autre participant connecté.

**Independent Test**: Créer un board via l'UI, ajouter/éditer/supprimer plusieurs post-its dans
différentes colonnes sans qu'aucun autre participant ne soit connecté, recharger la page et
vérifier que le contenu persiste.

### Tests for User Story 1

- [X] T022 [P] [US1] Test d'intégration : `POST /api/boards` crée un board avec le facilitateur et
      applique le thème par défaut si aucun n'est choisi (FR-001, FR-002, FR-013) dans
      `backend/tests/ScrumMaster.Api.Tests/BoardsControllerTests.cs`
- [X] T023 [P] [US1] Test d'intégration : `AddPostIt`/`EditPostIt` refusent un texte vide et une
      modification par un non-auteur (FR-004, FR-005, FR-015) dans
      `backend/tests/ScrumMaster.Api.Tests/RetroBoardHubTests.cs`

### Implementation for User Story 1

- [X] T024 [US1] Implémenter `GET /api/themes` dans
      `backend/src/ScrumMaster.Api/Controllers/ThemesController.cs` (dépend de T018)
- [X] T025 [US1] Implémenter `BoardService.CreateBoard` (validation Area Path/Iteration non
      vides, application du thème par défaut) dans
      `backend/src/ScrumMaster.Api/Services/BoardService.cs` (dépend de T016)
- [X] T026 [US1] Implémenter `POST /api/boards` dans
      `backend/src/ScrumMaster.Api/Controllers/BoardsController.cs` (dépend de T025)
- [X] T027 [US1] Implémenter `GET /api/boards/{boardId}` (état complet du board) dans
      `backend/src/ScrumMaster.Api/Controllers/BoardsController.cs` (dépend de T026)
- [X] T028 [US1] Implémenter `PostItService` (ajout/édition/suppression, texte non vide, contrôle
      auteur) dans `backend/src/ScrumMaster.Api/Services/PostItService.cs` (dépend de T016)
- [X] T029 [US1] Implémenter `RetroBoardHub` avec `JoinBoard`, `AddPostIt`, `EditPostIt`,
      `DeletePostIt` dans `backend/src/ScrumMaster.Api/Hubs/RetroBoardHub.cs` (dépend de T019,
      T028)
- [X] T030 [US1] Implémenter `CreateBoardPage` (formulaire Area Path, Iteration, thème, nom
      affiché) dans `frontend/src/pages/CreateBoardPage.tsx` (dépend de T024, T026)
- [X] T031 [US1] Implémenter `BoardPage`, `Colonne` et `PostIt` (affichage, ajout/édition/
      suppression via le hub) dans `frontend/src/pages/BoardPage.tsx`,
      `frontend/src/components/Colonne.tsx`, `frontend/src/components/PostIt.tsx` (dépend de
      T027, T029, T021). Connexion SignalR gérée directement dans `BoardPage` pour ce périmètre
      US1 ; le hook dédié `useRealtimeBoard` (T038, US2) la généralisera.
- [X] T032 [US1] Ajouter la validation et l'affichage d'erreur côté client pour un post-it vide
      dans `frontend/src/components/PostIt.tsx` et `frontend/src/components/Colonne.tsx` (dépend
      de T031)

**Checkpoint**: User Story 1 fonctionnelle de façon autonome (facilitateur seul).

---

## Phase 4: User Story 2 - Collaborer en temps réel avec plusieurs participants (Priority: P2)

**Goal**: Plusieurs participants rejoignent le même board et voient les actions des autres
(ajout, modification, déplacement, suppression de post-its) apparaître en direct.

**Independent Test**: Ouvrir le même board dans deux navigateurs, ajouter/déplacer un post-it dans
l'un et vérifier son apparition dans l'autre en moins de 3 secondes sans rechargement manuel.

### Tests for User Story 2

- [ ] T033 [P] [US2] Test d'intégration : deux clients hub simulés reçoivent `PostItAdded` et
      `PostItMoved` après une mutation par l'un d'eux (FR-006, FR-007) dans
      `backend/tests/ScrumMaster.Api.Tests/RetroBoardHubRealtimeTests.cs`

### Implementation for User Story 2

- [ ] T034 [US2] Implémenter `ParticipantService.Join` (création participant, rôle `Participant`)
      dans `backend/src/ScrumMaster.Api/Services/ParticipantService.cs` (dépend de T016)
- [ ] T035 [US2] Implémenter `POST /api/boards/{boardId}/participants` dans
      `backend/src/ScrumMaster.Api/Controllers/BoardsController.cs` (dépend de T034)
- [ ] T036 [US2] Implémenter `MovePostIt` et la diffusion `ParticipantJoined`/`PostItMoved` au
      groupe dans `backend/src/ScrumMaster.Api/Hubs/RetroBoardHub.cs` (dépend de T029, T034)
- [ ] T037 [US2] Implémenter `JoinBoardPage` (saisie du nom affiché en arrivant par le lien) dans
      `frontend/src/pages/JoinBoardPage.tsx` (dépend de T035)
- [ ] T038 [US2] Implémenter le hook `useRealtimeBoard` (connexion SignalR, abonnement aux
      événements, mise à jour de l'état local) dans
      `frontend/src/hooks/useRealtimeBoard.ts` (dépend de T021, T036)
- [ ] T039 [US2] Implémenter le déplacement de post-it entre colonnes dans l'UI dans
      `frontend/src/components/Colonne.tsx` (dépend de T031, T038)
- [ ] T040 [US2] Implémenter la resynchronisation à la reconnexion (re-`GET /api/boards/{boardId}`
      + `JoinBoard`) dans `frontend/src/hooks/useRealtimeBoard.ts` (dépend de T038)

**Checkpoint**: User Stories 1 et 2 fonctionnelles ensemble.

---

## Phase 5: User Story 3 - Voter sur les post-its (Priority: P3)

**Goal**: Les participants votent pour prioriser les post-its, dans la limite d'un quota par
participant et par board.

**Independent Test**: Voter pour plusieurs post-its depuis différents participants, atteindre le
quota, retirer un vote, vérifier que les compteurs sont corrects et partagés par tous.

### Tests for User Story 3

- [ ] T041 [P] [US3] Test d'intégration : `Vote`/`RemoveVote` respectent le quota
      `MaxVotesParParticipant` et l'unicité `(PostItId, ParticipantId)` (FR-008, FR-009) dans
      `backend/tests/ScrumMaster.Api.Tests/VoteTests.cs`

### Implementation for User Story 3

- [ ] T042 [US3] Implémenter `VoteService` (vote, retrait, contrôle quota et unicité) dans
      `backend/src/ScrumMaster.Api/Services/VoteService.cs` (dépend de T016)
- [ ] T043 [US3] Implémenter `Vote` et `RemoveVote` et la diffusion `VoteChanged` dans
      `backend/src/ScrumMaster.Api/Hubs/RetroBoardHub.cs` (dépend de T029, T042)
- [ ] T044 [US3] Implémenter `VoteCounter` (bouton de vote, votes restants) dans
      `frontend/src/components/VoteCounter.tsx` (dépend de T038)
- [ ] T045 [US3] Intégrer `VoteCounter` dans `PostIt` dans
      `frontend/src/components/PostIt.tsx` (dépend de T031, T044)

**Checkpoint**: User Stories 1, 2 et 3 fonctionnelles ensemble.

---

## Phase 6: User Story 4 - Personnaliser le thème du board (Priority: P4)

**Goal**: Le facilitateur choisit ou personnalise le thème (colonnes) avant la réunion.

**Independent Test**: Créer un board avec un thème personnalisé et vérifier que les colonnes
affichées correspondent exactement à celles définies.

### Tests for User Story 4

- [ ] T046 [P] [US4] Test d'intégration : `ChangeTheme` est refusé pour un non-facilitateur et
      pour un thème sans colonne (FR-013, FR-015) dans
      `backend/tests/ScrumMaster.Api.Tests/ThemeChangeTests.cs`

### Implementation for User Story 4

- [ ] T047 [US4] Étendre `BoardService` avec `ChangeTheme` (contrôle rôle facilitateur,
      validation ≥1 colonne) dans `backend/src/ScrumMaster.Api/Services/BoardService.cs` (dépend
      de T025)
- [ ] T048 [US4] Implémenter `ChangeTheme` et la diffusion `ThemeChanged` dans
      `backend/src/ScrumMaster.Api/Hubs/RetroBoardHub.cs` (dépend de T029, T047)
- [ ] T049 [US4] Étendre `POST /api/boards` pour accepter un thème personnalisé
      (`themePersonnalise`) dans
      `backend/src/ScrumMaster.Api/Controllers/BoardsController.cs` (dépend de T026, T047)
- [ ] T050 [US4] Implémenter `ThemeEditor` (choix d'un thème prédéfini ou colonnes personnalisées)
      dans `frontend/src/components/ThemeEditor.tsx` (dépend de T030)
- [ ] T051 [US4] Intégrer le contrôle de changement de thème (visible facilitateur uniquement)
      dans `BoardPage` dans `frontend/src/pages/BoardPage.tsx` (dépend de T031, T048, T050)

**Checkpoint**: Les 4 user stories sont fonctionnelles.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Clôture de board (FR-016, transverse à toutes les stories) et mise en état
déployable.

- [ ] T052 [P] Test d'intégration : toute mutation (`AddPostIt`, `EditPostIt`, `MovePostIt`,
      `DeletePostIt`, `Vote`, `RemoveVote`, `ChangeTheme`) est refusée quand `Board.Statut =
      Cloture` (FR-016) dans `backend/tests/ScrumMaster.Api.Tests/BoardClosureTests.cs`
- [ ] T053 Implémenter `CloseBoard` (facilitateur uniquement) et la diffusion `BoardClosed` dans
      `backend/src/ScrumMaster.Api/Hubs/RetroBoardHub.cs` (dépend de T036, T043, T048)
- [ ] T054 Faire respecter le rejet des mutations quand `Board.Statut = Cloture` dans chaque
      méthode du hub dans `backend/src/ScrumMaster.Api/Hubs/RetroBoardHub.cs` (dépend de T053)
- [ ] T055 Implémenter le mode lecture seule côté UI à la réception de `BoardClosed` (désactive
      les contrôles de mutation) dans `frontend/src/pages/BoardPage.tsx` (dépend de T038, T053)
- [ ] T056 [P] Écrire les manifests `k8s/base/` complets (Deployment `ScrumMaster.Api`, Deployment
      frontend statique, Service, ConfigMap)
- [ ] T057 [P] Écrire l'overlay `k8s/overlays/production/` (Ingress Traefik + annotations
      cert-manager, Secret de connexion à la base PostgreSQL dédiée), strictement séparé des
      manifests SkillForge (Constitution Principe V)
- [ ] T058 Exécuter la validation `quickstart.md` de bout en bout (2 navigateurs) et corriger les
      écarts constatés

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Aucune dépendance — démarre immédiatement
- **Foundational (Phase 2)**: Dépend de Setup — bloque toutes les user stories
- **User Stories (Phase 3-6)**: Dépendent toutes de Foundational ; peuvent progresser en
  parallèle si plusieurs personnes y travaillent, sinon dans l'ordre P1 → P2 → P3 → P4
- **Polish (Phase 7)**: Dépend des user stories concernées par la clôture (US1-US4 doivent avoir
  leurs méthodes de mutation en place avant T053-T054)

### User Story Dependencies

- **US1 (P1)**: Démarre après Foundational — aucune dépendance à une autre story
- **US2 (P2)**: Démarre après Foundational — réutilise le hub créé par US1 (T029) mais reste
  testable indépendamment (jonction + temps réel)
- **US3 (P3)**: Démarre après Foundational — s'appuie sur le hub (T029) et le flux de jonction
  (US2) pour être testée à plusieurs participants, mais le vote lui-même est indépendant
- **US4 (P4)**: Démarre après Foundational — étend `BoardService`/`RetroBoardHub` créés par US1

### Parallel Opportunities

- T002-T006 (Setup) en parallèle
- T009-T015 (modèles d'entités) en parallèle
- T020-T021 (clients frontend de base) en parallèle
- Les tests marqués `[P]` de chaque story en parallèle entre eux
- T056-T057 (manifests k8s) en parallèle

---

## Parallel Example: User Story 1

```bash
# Tests en parallèle :
Task: "Test d'intégration POST /api/boards dans backend/tests/ScrumMaster.Api.Tests/BoardsControllerTests.cs"
Task: "Test d'intégration AddPostIt/EditPostIt/DeletePostIt dans backend/tests/ScrumMaster.Api.Tests/RetroBoardHubTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 uniquement)

1. Compléter Phase 1 (Setup) et Phase 2 (Foundational)
2. Compléter Phase 3 (User Story 1)
3. **STOP et VALIDER** : un facilitateur seul peut créer un board et gérer ses post-its
4. Démontrer / déployer si prêt

### Incremental Delivery

1. Setup + Foundational → fondations prêtes
2. + US1 → tester → démontrer (MVP)
3. + US2 → tester (collaboration temps réel) → démontrer
4. + US3 → tester (vote) → démontrer
5. + US4 → tester (personnalisation du thème) → démontrer
6. + Polish (clôture, déploiement k8s) → valider `quickstart.md` complet

---

## Notes

- `[P]` = fichiers différents, aucune dépendance non résolue
- Chaque story doit rester indépendamment testable (voir "Independent Test" de chaque phase)
- Committer après chaque tâche ou groupe logique de tâches
- S'arrêter à chaque checkpoint pour valider la story avant de poursuivre
