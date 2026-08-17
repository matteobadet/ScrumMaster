# Tasks: Point de sprint (stats Azure DevOps)

**Input**: Design documents from `/specs/009-sprint-review-stats/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, et
specs/005-azure-devops-boards (configuration Azure DevOps, `AzureDevOpsClient`,
`AzureDevOpsBoardService`) déjà implémentée.

**Tests**: Incluses (tests d'intégration ciblés par user story, réutilisant
`TestWebApplicationFactory` et `StubAzureDevOpsHandler` comme specs/005-azure-devops-boards).

**Organization**: Tâches groupées par user story (P1 → P3 de `spec.md`). La classification par
catégorie d'état est intrinsèquement groupée par type de work item (research.md#1, #3) : le
Foundational produit donc directement la forme de donnée complète (répartition par type), et
chaque user story ajoute le test + l'affichage frontend correspondant à son critère d'acceptation,
plutôt que de refaire le calcul.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Peut s'exécuter en parallèle (fichiers différents, aucune dépendance non résolue)
- **[Story]**: User story associée (US1, US2, US3)

## Path Conventions

Extension du backend/frontend existants (voir `plan.md` — Project Structure) :
`backend/src/ScrumMaster.Api/`, `backend/tests/ScrumMaster.Api.Tests/`, `frontend/src/`.

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: Client Azure DevOps étendu, DTOs, et service de calcul partagés par les trois user
stories — sans eux, aucune ne peut ni compiler ni renvoyer de donnée.

**⚠️ CRITICAL**: Aucune user story ne démarre avant la fin de cette phase.

- [X] T001 [P] Étendre `AzureDevOpsWorkItemSummary` avec `Type`/`Etat`, ajouter les DTOs bruts de
      désérialisation de l'endpoint `states` (`AzureDevOpsWorkItemStatesDto`,
      `AzureDevOpsWorkItemStateDto` avec `Name`/`Category`) et l'enum `AzureDevOpsEtatCategorie`
      (`Proposed`/`InProgress`/`Resolved`/`Completed`/`Removed`) dans
      `backend/src/ScrumMaster.Api/AzureDevOps/AzureDevOpsDtos.cs` (research.md#1, #3)
- [X] T002 Étendre `AzureDevOpsClient.ListerWorkItemsAsync` pour demander aussi les champs
      `System.WorkItemType` et `System.State` dans
      `backend/src/ScrumMaster.Api/AzureDevOps/AzureDevOpsClient.cs` (dépend de T001 ; research.md#3)
- [X] T003 Ajouter `AzureDevOpsClient.ObtenirEtatsAsync(organisation, projet, pat, type)` appelant
      `GET .../_apis/wit/workitemtypes/{type}/states?api-version=7.1` et renvoyant un mapping
      état→catégorie dans `backend/src/ScrumMaster.Api/AzureDevOps/AzureDevOpsClient.cs` (dépend de
      T001 ; research.md#1)
- [X] T004 [P] Créer `PointDeSprintDto`/`RepartitionTypeDto` (voir data-model.md) dans
      `backend/src/ScrumMaster.Api/Dtos/PointDeSprintDtos.cs`
- [X] T005 Ajouter `AzureDevOpsBoardService.ObtenirPointDeSprintAsync(boardId,
      callerParticipantId)` : résout le board et le participant (tout rôle, sans
      `BoardClosureGuard.EnsureActif` — research.md#5), charge la configuration Azure DevOps
      (`DomainValidationException` si absente, FR-006), liste les work items de l'Iteration du
      board (champs Type/Etat), résout la catégorie d'état par type distinct présent
      (`ObtenirEtatsAsync`), exclut les work items en catégorie `Removed`, agrège en
      `RepartitionTypeDto` par type (`Task`/`UserStory`/`Autres`, research.md#2) et calcule
      `TotalPlanifie`/`TotalTermine` ; convertit `HttpRequestException` en
      `DomainUpstreamException` (FR-007) dans
      `backend/src/ScrumMaster.Api/Services/AzureDevOpsBoardService.cs` (dépend de T002, T003, T004)
- [X] T006 Ajouter `GET /api/boards/{boardId}/point-de-sprint?asParticipantId={id}` dans
      `backend/src/ScrumMaster.Api/Controllers/BoardsController.cs` (dépend de T005 ;
      contracts/rest-api-delta.md)
- [X] T007 [P] Ajouter les types `PointDeSprint`/`RepartitionType` dans `frontend/src/types.ts`
- [X] T008 [P] Ajouter `boardsApi.obtenirPointDeSprint(boardId, participantId)` dans
      `frontend/src/services/boardsApi.ts` (dépend de T007)

**Checkpoint**: Le endpoint renvoie la répartition complète (état + type + totaux) vérifiable via
`GET` direct. Aucune interface de consultation encore ; l'implémentation des user stories peut
commencer.

---

## Phase 2: User Story 1 - Consulter la répartition des work items par état (Priority: P1) 🎯 MVP

**Goal**: Un participant ouvre le point de sprint et voit le nombre de work items à faire / en
cours / terminé pour l'Iteration du board.

**Independent Test**: Ouvrir le point de sprint sur un board dont l'Iteration contient des work
items dans plusieurs états, et vérifier que les comptages par état affichés correspondent aux
données réelles d'Azure DevOps.

### Tests for User Story 1

- [X] T009 [P] [US1] Test d'intégration : `GET .../point-de-sprint` sur une Iteration avec des work
      items dans plusieurs états renvoie les comptages à faire/en cours/terminé attendus ; une
      Iteration vide renvoie `200` avec des listes/totaux vides (FR-008) ; une équipe non
      configurée renvoie `400` (FR-006) dans
      `backend/tests/ScrumMaster.Api.Tests/PointDeSprintTests.cs`

### Implementation for User Story 1

- [X] T010 [US1] Créer `frontend/src/components/PointDeSprintPanel.tsx` : affiche la répartition
      par état (à faire/en cours/terminé), état vide explicite si aucun work item, message d'erreur
      explicite si équipe non configurée ou échec Azure DevOps (dépend de T007)
- [X] T011 [US1] Ajouter dans `frontend/src/pages/BoardPage.tsx` un bouton "Point de sprint"
      toujours visible (tout participant, indépendant du statut du board et de l'étape active,
      Assumptions de spec.md) ouvrant `PointDeSprintPanel` (dépend de T008, T010)

**Checkpoint**: User Story 1 fonctionnelle de façon autonome — un participant peut consulter la
répartition par état sans configuration additionnelle.

---

## Phase 3: User Story 2 - Distinguer Task et User Story dans les statistiques (Priority: P2)

**Goal**: La répartition par état est scindée entre Tasks et User Stories.

**Independent Test**: Sur une Iteration contenant des Tasks et des User Stories dans des états
variés, vérifier que les comptages par état sont bien scindés par type de work item.

### Tests for User Story 2

- [X] T012 [P] [US2] Test d'intégration : Iteration avec Tasks et User Stories mélangés → la
      répartition par état est scindée par type ; une Iteration ne contenant qu'un seul type
      n'affiche pas de section vide pour l'autre (FR-003, FR-004) dans
      `backend/tests/ScrumMaster.Api.Tests/PointDeSprintTests.cs` (même fichier que T009,
      séquentiel)

### Implementation for User Story 2

- [X] T013 [US2] Adapter `PointDeSprintPanel.tsx` pour afficher la répartition scindée par type
      (Task / User Story / Autres) au lieu d'un total unique global (dépend de T010, séquentiel —
      même fichier)

**Checkpoint**: User Story 1 et 2 fonctionnelles ensemble — la répartition est lisible par type.

---

## Phase 4: User Story 3 - Voir le taux de complétion planifié vs réalisé (Priority: P3)

**Goal**: Le point de sprint affiche le nombre de work items terminés rapporté au total planifié.

**Independent Test**: Sur une Iteration avec un mélange de work items terminés et non terminés,
vérifier que le taux affiché correspond au calcul attendu (work items `Removed` exclus).

### Tests for User Story 3

- [X] T014 [P] [US3] Test d'intégration : Iteration avec un mélange terminé/non-terminé et un work
      item en catégorie `Removed` → `TotalPlanifie`/`TotalTermine` excluent le `Removed` (FR-005)
      dans `backend/tests/ScrumMaster.Api.Tests/PointDeSprintTests.cs` (même fichier que T009/T012,
      séquentiel)

### Implementation for User Story 3

- [X] T015 [US3] Adapter `PointDeSprintPanel.tsx` pour afficher le taux de complétion global
      (`totalTermine` / `totalPlanifie`) dans
      `frontend/src/components/PointDeSprintPanel.tsx` (dépend de T013, séquentiel — même fichier)

**Checkpoint**: Les trois user stories sont fonctionnelles ensemble.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Validation complète de la feature et non-régression sur specs/005.

- [X] T016 Exécuter la validation `quickstart.md` de bout en bout (les 6 scénarios, y compris la
      non-régression sur l'import de work items et l'export de post-its de specs/005-azure-devops-
      boards) et corriger les écarts constatés
- [X] T017 Exécuter la suite `dotnet test` complète et `npx tsc --noEmit` côté frontend

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 1)**: Aucune dépendance — démarre immédiatement, bloque les trois user
  stories (client Azure DevOps étendu, DTOs, service de calcul partagés)
- **User Stories (Phase 2-4)**: Dépendent de Foundational ; US2 et US3 s'enchaînent après US1 sur
  `PointDeSprintPanel.tsx` (même fichier) mais restent fonctionnellement indépendantes (le payload
  complet existe dès le Foundational, seul l'affichage progresse)
- **Polish (Phase 5)**: Dépend des trois user stories

### User Story Dependencies

- **US1 (P1)**: Démarre après Foundational — aucune dépendance fonctionnelle à US2/US3
- **US2 (P2)**: Démarre après Foundational — partage `PointDeSprintPanel.tsx` avec US1 (affichage)
  donc s'exécute après elle sur ce fichier, mais sa logique (scission par type) est déjà disponible
  dans le payload du Foundational
- **US3 (P3)**: Démarre après Foundational — partage `PointDeSprintPanel.tsx` avec US1/US2, même
  remarque

### Parallel Opportunities

- T001, T004 [P] (DTOs) en parallèle
- T007, T008 [P] (types/service frontend) en parallèle du backend (T001-T006)
- T009 [P], T012 [P], T014 [P] (tests par story) peuvent être écrits dès Foundational terminé, avant
  l'implémentation frontend (TDD) — mais restent dans le même fichier donc s'exécutent en séquence
  entre eux au moment de l'écriture

---

## Parallel Example: Foundational

```bash
Task: "Étendre AzureDevOpsWorkItemSummary et ajouter les DTOs d'états dans backend/src/ScrumMaster.Api/AzureDevOps/AzureDevOpsDtos.cs"
Task: "Créer PointDeSprintDto/RepartitionTypeDto dans backend/src/ScrumMaster.Api/Dtos/PointDeSprintDtos.cs"
Task: "Ajouter les types PointDeSprint/RepartitionType dans frontend/src/types.ts"
```

---

## Implementation Strategy

### MVP First (Foundational + User Story 1 uniquement)

1. Compléter Phase 1 (Foundational)
2. Compléter Phase 2 (User Story 1)
3. **STOP et VALIDER** : un participant peut consulter la répartition par état sans configuration
   additionnelle
4. Démontrer / déployer si prêt

### Incremental Delivery

1. Foundational → endpoint complet prêt (payload état+type+totaux)
2. + US1 → tester (répartition par état) → démontrer (MVP)
3. + US2 → tester (scission Task/User Story) → démontrer
4. + US3 → tester (taux de complétion) → démontrer
5. + Polish (validation quickstart complète, non-régression specs/005)

---

## Notes

- `[P]` = fichiers différents, aucune dépendance non résolue
- Chaque story doit rester indépendamment testable (voir "Independent Test" de chaque phase)
- Committer après chaque tâche ou groupe logique de tâches
- S'arrêter à chaque checkpoint pour valider la story avant de poursuivre
