# Tasks: Historique des boards par équipe

**Input**: Design documents from `/specs/010-historique-boards/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, et
specs/001-retro-board-base (entité `Board`) déjà implémentée.

**Tests**: Incluses (test d'intégration ciblé, réutilisant `TestWebApplicationFactory`).

**Organization**: Tâches groupées par user story (P1 → P2 de `spec.md`). Le Foundational pose
l'endpoint et les types partagés ; US1 démontre la consultation ; US2 ajoute la découvrabilité
(liens depuis les points d'entrée existants).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Peut s'exécuter en parallèle (fichiers différents, aucune dépendance non résolue)
- **[Story]**: User story associée (US1, US2)

## Path Conventions

Extension du backend/frontend existants (voir `plan.md` — Project Structure) :
`backend/src/ScrumMaster.Api/`, `backend/tests/ScrumMaster.Api.Tests/`, `frontend/src/`.

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: Endpoint et types partagés par les deux user stories.

**⚠️ CRITICAL**: Aucune user story ne démarre avant la fin de cette phase.

- [X] T001 [P] Ajouter `BoardSummaireDto(Guid Id, string Iteration, string Statut, DateTimeOffset
      DateCreation)` (voir data-model.md) dans `backend/src/ScrumMaster.Api/Dtos/BoardDtos.cs`
- [X] T002 Ajouter `BoardService.ListerBoardsParEquipeAsync(areaPath)` : requête `Board` filtrée par
      `AreaPath`, triée par `DateCreation` décroissante (FR-003), liste vide si aucune
      correspondance (FR-005, pas d'exception) dans
      `backend/src/ScrumMaster.Api/Services/BoardService.cs` (dépend de T001)
- [X] T003 Ajouter `GET /api/equipes/{areaPath}/boards` (route absolue, research.md#1) dans
      `backend/src/ScrumMaster.Api/Controllers/BoardsController.cs` (dépend de T002 ;
      contracts/rest-api-delta.md)
- [X] T004 [P] Ajouter le type `BoardSummary` dans `frontend/src/types.ts`
- [X] T005 [P] Ajouter `boardsApi.listerBoardsParEquipe(areaPath)` dans
      `frontend/src/services/boardsApi.ts` (dépend de T004)

**Checkpoint**: Le endpoint renvoie la liste triée, vérifiable via `GET` direct. Aucune interface de
consultation encore ; l'implémentation des user stories peut commencer.

---

## Phase 2: User Story 1 - Retrouver un board clôturé de son équipe (Priority: P1) 🎯 MVP

**Goal**: Un membre d'équipe consulte tous les boards (actifs et clôturés) d'un Area Path et rouvre
celui qui l'intéresse.

**Independent Test**: Créer plusieurs boards pour une même équipe (certains clôturés), consulter la
liste pour cette équipe, vérifier le tri et l'accès en un clic à chaque board.

### Tests for User Story 1

- [X] T006 [P] [US1] Test d'intégration : `GET /api/equipes/{areaPath}/boards` renvoie tous les
      boards de l'équipe triés du plus récent au plus ancien, avec Iteration/Statut/DateCreation
      corrects ; un Area Path sans board renvoie une liste vide (FR-001, FR-002, FR-003, FR-005)
      dans `backend/tests/ScrumMaster.Api.Tests/HistoriqueBoardsTests.cs`

### Implementation for User Story 1

- [X] T007 [US1] Créer `frontend/src/pages/BoardHistoryPage.tsx` : affiche la liste des boards de
      l'Area Path de l'URL (Iteration, date, statut), lien vers chaque board (FR-004), état vide
      explicite si la liste est vide (FR-005) (dépend de T005 ; mirroring
      `AzureDevOpsConfigPage.tsx`)
- [X] T008 [US1] Ajouter la route `/equipe/:areaPath/boards` → `BoardHistoryPage` dans
      `frontend/src/App.tsx` (dépend de T007)

**Checkpoint**: User Story 1 fonctionnelle de façon autonome — un membre qui connaît l'URL de
l'historique peut retrouver et rouvrir n'importe quel board de son équipe.

---

## Phase 3: User Story 2 - Accéder à l'historique sans connaître d'URL spécifique (Priority: P2)

**Goal**: Un lien vers l'historique de l'équipe est proposé depuis le formulaire de création de
board et depuis la page d'un board déjà ouvert.

**Independent Test**: Renseigner un Area Path sur le formulaire de création, vérifier la présence
d'un lien vers l'historique ; ouvrir un board existant, vérifier la présence du même lien.

### Implementation for User Story 2

- [X] T009 [P] [US2] Ajouter dans `frontend/src/pages/CreateBoardPage.tsx` un lien "Voir
      l'historique de cette équipe" vers `/equipe/{areaPath}/boards`, visible dès que l'Area Path
      est renseigné (FR-006)
- [X] T010 [P] [US2] Ajouter dans `frontend/src/pages/BoardPage.tsx` un lien vers
      `/equipe/{board.areaPath}/boards`, visible à tout participant (FR-006, cohérent avec le lien
      "Point de sprint" déjà non réservé au facilitateur, specs/009-sprint-review-stats)

**Checkpoint**: Les deux user stories sont fonctionnelles ensemble — l'historique est consultable et
découvrable sans connaître d'URL à l'avance.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Validation complète de la feature.

- [X] T011 Exécuter la validation `quickstart.md` de bout en bout (les 5 scénarios) et corriger les
      écarts constatés
- [X] T012 Exécuter la suite `dotnet test` complète et `npx tsc --noEmit` côté frontend

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 1)**: Aucune dépendance — démarre immédiatement, bloque les deux user
  stories (endpoint et types partagés)
- **User Stories (Phase 2-3)**: Dépendent de Foundational ; US2 dépend de la page créée par US1
  (T007/T008) pour avoir une destination à lier, mais reste fonctionnellement indépendante (sa
  valeur est la découvrabilité, pas la consultation elle-même)
- **Polish (Phase 4)**: Dépend des deux user stories

### User Story Dependencies

- **US1 (P1)**: Démarre après Foundational — aucune dépendance fonctionnelle à US2
- **US2 (P2)**: Démarre après Foundational ; techniquement a besoin que la route `/equipe/:areaPath/
  boards` existe (US1, T008) pour pointer quelque part de fonctionnel, mais les deux tâches US2
  (T009, T010) sont elles-mêmes indépendantes l'une de l'autre (fichiers différents)

### Parallel Opportunities

- T001, T004, T005 [P] (DTOs/types) en parallèle
- T009, T010 [P] (liens dans deux pages différentes) en parallèle
- T006 [P] (test US1) peut être écrit dès Foundational terminé, avant l'implémentation (TDD)

---

## Parallel Example: Foundational

```bash
Task: "Ajouter BoardSummaireDto dans backend/src/ScrumMaster.Api/Dtos/BoardDtos.cs"
Task: "Ajouter le type BoardSummary dans frontend/src/types.ts"
```

---

## Implementation Strategy

### MVP First (Foundational + User Story 1 uniquement)

1. Compléter Phase 1 (Foundational)
2. Compléter Phase 2 (User Story 1)
3. **STOP et VALIDER** : un membre d'équipe qui connaît l'URL de l'historique peut retrouver et
   rouvrir n'importe quel board de son équipe
4. Démontrer / déployer si prêt

### Incremental Delivery

1. Foundational → endpoint prêt
2. + US1 → tester (liste et accès) → démontrer (MVP)
3. + US2 → tester (découvrabilité) → démontrer
4. + Polish (validation quickstart complète)

---

## Notes

- `[P]` = fichiers différents, aucune dépendance non résolue
- Chaque story doit rester indépendamment testable (voir "Independent Test" de chaque phase)
- Committer après chaque tâche ou groupe logique de tâches
- S'arrêter à chaque checkpoint pour valider la story avant de poursuivre
