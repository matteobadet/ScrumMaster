# Tasks: Thèmes de Rétrospective Narratifs

**Input**: Design documents from `/specs/004-themes-narratifs/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, et la feature
specs/001-retro-board-base déjà implémentée (réutilise `ScrumMaster.Api`, `Theme`, `BoardService`,
`ThemeEditor`).

**Tests**: Incluses (tests d'intégration ciblés par user story, réutilisant
`TestWebApplicationFactory` comme le reste du backend — voir specs/001-retro-board-base).

**Organization**: Tâches groupées par user story (P1 → P2 de `spec.md`) pour permettre une
implémentation et une validation indépendantes de chacune. Aucune Phase Setup : cette feature ne
requiert aucune nouvelle dépendance (extension pure de l'existant, voir `plan.md`).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Peut s'exécuter en parallèle (fichiers différents, aucune dépendance non résolue)
- **[Story]**: User story associée (US1, US2)

## Path Conventions

Extension du backend/frontend existants (voir `plan.md` — Project Structure) :
`backend/src/ScrumMaster.Api/`, `backend/tests/ScrumMaster.Api.Tests/`, `frontend/src/`.

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: Extension du modèle de données partagée par les deux user stories.

**⚠️ CRITICAL**: Aucune user story ne démarre avant la fin de cette phase.

- [X] T001 Étendre le modèle `Theme` avec `Icone` (string?, ≤50 caractères) et `Contexte` (string?,
      ≤500 caractères) dans `backend/src/ScrumMaster.Api/Models/Theme.cs`
- [X] T002 Générer et appliquer la migration EF Core ajoutant les deux colonnes nullable
      correspondantes dans `backend/src/ScrumMaster.Api/Data/Migrations/` (dépend de T001)

**Checkpoint**: Fondations prêtes — l'implémentation des user stories peut commencer.

---

## Phase 2: User Story 1 - Associer une icône au thème (Priority: P1) 🎯 MVP

**Goal**: Le facilitateur associe une icône/emoji à un thème (prédéfini ou personnalisé) ; elle
apparaît à côté du nom du thème dans l'en-tête du board.

**Independent Test**: Créer un board avec un thème personnalisé portant une icône et vérifier
qu'elle apparaît dans l'en-tête ; créer un board avec un thème sans icône et vérifier l'absence de
tout espace vide ou erreur.

### Tests for User Story 1

- [X] T003 [P] [US1] Test d'intégration : un board créé avec un thème personnalisé portant une
      icône renvoie cette icône via `GET /api/boards/{id}` (et via `POST /api/boards` → thème
      appliqué) ; un thème sans icône renvoie `null` sans erreur (FR-001, FR-002, FR-005) dans
      `backend/tests/ScrumMaster.Api.Tests/ThemeIconeContexteTests.cs`

### Implementation for User Story 1

- [X] T004 [US1] Ajouter `Icone` à `ThemeSummaryDto`, `ThemePersonnaliseDto` et `ThemeRefDto` dans
      `backend/src/ScrumMaster.Api/Dtos/{ThemeDtos.cs,BoardDtos.cs}` (dépend de T001)
- [X] T005 [US1] Propager `Icone` dans `BoardService.ResolveThemeAsync`/`CopyTheme` (validation de
      longueur ≤50 caractères, rejet 400 si dépassée) dans
      `backend/src/ScrumMaster.Api/Services/BoardService.cs` (dépend de T004)
- [X] T006 [US1] Exposer `Icone` dans la réponse de `ThemesController.GetThemes` dans
      `backend/src/ScrumMaster.Api/Controllers/ThemesController.cs` (dépend de T004)
- [X] T007 [P] [US1] Ajouter `icone` à `ThemeSummary`, `ThemePersonnalise`, `ThemeRef` et la
      variante `custom` de `ThemeSelection` dans `frontend/src/types.ts`
- [X] T008 [US1] Ajouter le champ de saisie de l'icône pour le thème personnalisé dans
      `frontend/src/components/ThemeEditor.tsx` (dépend de T007)
- [X] T009 [US1] Afficher l'icône du thème courant à côté de son nom dans l'en-tête du board dans
      `frontend/src/pages/BoardPage.tsx` (dépend de T007)

**Checkpoint**: User Story 1 fonctionnelle de façon autonome.

---

## Phase 3: User Story 2 - Planter le décor avec un bloc Contexte (Priority: P2)

**Goal**: Le facilitateur associe un texte de contexte libre à un thème (prédéfini ou
personnalisé) ; il apparaît en introduction du board, avant les colonnes, pour tous les
participants.

**Independent Test**: Créer un board avec un thème personnalisé portant un contexte et vérifier
qu'il apparaît en introduction du board ; tenter un contexte de plus de 500 caractères et vérifier
le rejet.

### Tests for User Story 2

- [X] T010 [US2] Test d'intégration : un board créé avec un thème personnalisé portant un contexte
      renvoie ce texte via `GET /api/boards/{id}` ; un contexte de plus de 500 caractères est
      rejeté (400) ; un thème sans contexte renvoie `null` sans erreur (FR-003, FR-004, FR-005,
      FR-008) dans `backend/tests/ScrumMaster.Api.Tests/ThemeIconeContexteTests.cs` (même fichier
      que T003 — ajout de cas de test, séquentiel après T003)

### Implementation for User Story 2

- [X] T011 [US2] Ajouter `Contexte` à `ThemeSummaryDto`, `ThemePersonnaliseDto` et `ThemeRefDto`
      dans `backend/src/ScrumMaster.Api/Dtos/{ThemeDtos.cs,BoardDtos.cs}` (dépend de T001,
      séquentiel après T004 — mêmes fichiers)
- [X] T012 [US2] Propager `Contexte` dans `BoardService.ResolveThemeAsync`/`CopyTheme` (validation
      de longueur ≤500 caractères, rejet 400 si dépassée) dans
      `backend/src/ScrumMaster.Api/Services/BoardService.cs` (dépend de T011, séquentiel après
      T005 — même fichier)
- [X] T013 [US2] Exposer `Contexte` dans la réponse de `ThemesController.GetThemes` dans
      `backend/src/ScrumMaster.Api/Controllers/ThemesController.cs` (dépend de T011, séquentiel
      après T006 — même fichier)
- [X] T014 [P] [US2] Ajouter `contexte` à `ThemeSummary`, `ThemePersonnalise`, `ThemeRef` et la
      variante `custom` de `ThemeSelection` dans `frontend/src/types.ts`
- [X] T015 [US2] Ajouter le champ de saisie du contexte (texte libre) pour le thème personnalisé
      dans `frontend/src/components/ThemeEditor.tsx` (dépend de T014, séquentiel après T008 —
      même fichier)
- [X] T016 [US2] Afficher le bloc de contexte en introduction du board, avant les colonnes, dans
      `frontend/src/pages/BoardPage.tsx` (dépend de T014, séquentiel après T009 — même fichier)

**Checkpoint**: User Stories 1 et 2 fonctionnelles ensemble.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Validation complète de la feature.

- [X] T017 Exécuter la validation `quickstart.md` de bout en bout et corriger les écarts constatés

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 1)**: Aucune dépendance — démarre immédiatement, bloque toutes les user
  stories
- **User Stories (Phase 2-3)**: Dépendent de Foundational ; US2 touche les mêmes fichiers que US1
  (mêmes DTOs, même service, mêmes composants) donc s'enchaîne après elle par fichier, mais reste
  fonctionnellement indépendante (testable et livrable sans US2 après Phase 2 seule)
- **Polish (Phase 4)**: Dépend des deux user stories

### User Story Dependencies

- **US1 (P1)**: Démarre après Foundational — aucune dépendance fonctionnelle à US2
- **US2 (P2)**: Démarre après Foundational — partage les mêmes fichiers que US1 (DTOs,
  `BoardService`, `ThemeEditor.tsx`, `BoardPage.tsx`) donc ses tâches s'exécutent après celles
  d'US1 sur ces fichiers, mais sa logique (contexte) est indépendante de celle d'US1 (icône)

### Parallel Opportunities

- T003 (test US1) peut démarrer dès T001/T002 terminés, en parallèle de T004-T006 s'il est écrit
  avant l'implémentation (TDD)
- T007 [P] (types frontend US1) en parallèle de T004-T006 (backend US1)
- T014 [P] (types frontend US2) en parallèle de T011-T013 (backend US2)

---

## Parallel Example: User Story 1

```bash
# Backend et frontend peuvent avancer en parallèle une fois les fondations posées :
Task: "Ajouter Icone aux DTOs dans backend/src/ScrumMaster.Api/Dtos/"
Task: "Ajouter icone à ThemeSummary/ThemePersonnalise/ThemeRef dans frontend/src/types.ts"
```

---

## Implementation Strategy

### MVP First (User Story 1 uniquement)

1. Compléter Phase 1 (Foundational)
2. Compléter Phase 2 (User Story 1)
3. **STOP et VALIDER** : un thème peut porter une icône, affichée dans l'en-tête du board
4. Démontrer / déployer si prêt

### Incremental Delivery

1. Foundational → fondations prêtes
2. + US1 → tester (icône) → démontrer (MVP)
3. + US2 → tester (contexte) → démontrer
4. + Polish (validation quickstart complète)

---

## Notes

- `[P]` = fichiers différents, aucune dépendance non résolue
- Chaque story doit rester indépendamment testable (voir "Independent Test" de chaque phase)
- Committer après chaque tâche ou groupe logique de tâches
- S'arrêter à chaque checkpoint pour valider la story avant de poursuivre

## Implémentation — notes

- Toutes les phases (Foundational, US1, US2, Polish) ont été implémentées en une seule passe
  (feature de taille réduite, aucune dépendance nouvelle) — voir le résumé de complétion.
- T003 et T010 ont été fusionnés avec deux cas de test supplémentaires (icône trop longue, absence
  des deux champs) dans `backend/tests/ScrumMaster.Api.Tests/ThemeIconeContexteTests.cs` (5 tests
  au total pour cette feature).
- Tests pré-existants touchés par le changement de signature de `ThemePersonnaliseDto` (ajout des
  paramètres positionnels `Icone`/`Contexte`) : `ThemeChangeTests.cs`, `BoardsControllerTests.cs`,
  `BoardClosureTests.cs` — mis à jour sans changement de comportement testé.
- T017 (validation `quickstart.md`) : exécuté via `dotnet test` (44/44) et une vérification
  manuelle dans le navigateur (création d'un board avec le thème "Les 3 petits cochons",
  icône 🐷 et contexte affichés correctement dans l'en-tête et l'introduction du board).
