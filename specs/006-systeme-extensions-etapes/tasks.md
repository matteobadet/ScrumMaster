# Tasks: Système d'Extensions — Étapes de Rétrospective

**Input**: Design documents from `/specs/006-systeme-extensions-etapes/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, et les features
specs/001-retro-board-base, specs/002-poll-utilite-reunion, specs/004-themes-narratifs,
specs/005-azure-devops-boards déjà implémentées.

**Tests**: Incluses (tests d'intégration ciblés par user story), plus une exigence transversale de
**non-régression** sur les tests déjà existants (specs/001, 004, 005) cassés par la restructuration
Foundational — voir T016.

**Organization**: Contrairement aux features précédentes, la restructuration centrale (`Étape`
remplaçant `Board.ThemeId`/`PostIt.BoardId`) est un tout indivisible : elle doit être complète et
cohérente avant qu'aucune user story ne puisse compiler ou s'exécuter. La Phase Foundational est
donc volumineuse par nécessité, pas par choix d'organisation — elle livre à elle seule la
garantie de non-régression FR-014 (board mono-étape inchangé). Les user stories ajoutent ensuite,
par-dessus ces fondations, la composition explicite multi-étapes (US1) puis les deux nouveaux
types d'étape (US2, US3).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Peut s'exécuter en parallèle (fichiers différents, aucune dépendance non résolue)
- **[Story]**: User story associée (US1, US2, US3)

## Path Conventions

Restructuration du backend/frontend existants (voir `plan.md` — Project Structure) :
`backend/src/ScrumMaster.Api/`, `backend/tests/ScrumMaster.Api.Tests/`, `frontend/src/`.

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: Restructuration complète du modèle `Étape`, migration avec backfill, et adaptation de
tout le code existant qui référence l'ancien modèle (`Board.ThemeId`, `PostIt.BoardId`,
`CloseBoard`). Livre la garantie de non-régression FR-014 : un board mono-étape se comporte
exactement comme avant cette feature.

**⚠️ CRITICAL**: Aucune user story ne démarre avant la fin de cette phase — rien ne compile
autrement.

- [X] T001 [P] Créer le modèle `Étape` (Type, Ordre, Statut, `ThemeId?`, `MiniJeuCatalogueId?`,
      `Question?`) dans `backend/src/ScrumMaster.Api/Models/Etape.cs`
- [X] T002 [P] Créer le modèle `MiniJeuCatalogue` (Id, Nom, TypeInterne, Description) dans
      `backend/src/ScrumMaster.Api/Models/MiniJeuCatalogue.cs`
- [X] T003 [P] Créer le modèle `ReponseMeteoEquipe` (EtapeId, ParticipantId, Humeur, DateReponse)
      dans `backend/src/ScrumMaster.Api/Models/ReponseMeteoEquipe.cs`
- [X] T004 [P] Créer le modèle `OptionPollPersonnalise` (Id, EtapeId, Texte, Ordre) dans
      `backend/src/ScrumMaster.Api/Models/OptionPollPersonnalise.cs`
- [X] T005 [P] Créer le modèle `ReponsePollPersonnalise` (EtapeId, ParticipantId, OptionId,
      DateReponse) dans `backend/src/ScrumMaster.Api/Models/ReponsePollPersonnalise.cs`
- [X] T006 Modifier `Board` : retirer `ThemeId`/`Theme`, ajouter `List<Etape> Etapes` dans
      `backend/src/ScrumMaster.Api/Models/Board.cs` (dépend de T001)
- [X] T007 Modifier `PostIt` : renommer `BoardId` en `EtapeId` dans
      `backend/src/ScrumMaster.Api/Models/PostIt.cs` (dépend de T001)
- [X] T008 Configurer dans `ScrumMasterDbContext` les nouveaux `DbSet`, le mapping d'`Étape`
      (contrainte d'ordre unique par board), la FK `PostIt.EtapeId`, et les contraintes d'unicité
      `(EtapeId, ParticipantId)` de `ReponseMeteoEquipe`/`ReponsePollPersonnalise` dans
      `backend/src/ScrumMaster.Api/Data/ScrumMasterDbContext.cs` (dépend de T001-T007)
- [X] T009 [P] Créer `MiniJeuSeeder` (seed "Météo d'équipe", `TypeInterne = "meteo-equipe"`) dans
      `backend/src/ScrumMaster.Api/Data/MiniJeuSeeder.cs`, appelé depuis `Program.cs` comme
      `ThemeSeeder` (dépend de T002)
- [X] T010 Générer la migration EF Core, puis y ajouter manuellement le **backfill de données**
      (`research.md#3`) : une `Étape` par `Board` existant (Type = ColonnesEtPostIts, Ordre = 0,
      `ThemeId` = ancien `Board.ThemeId`, Statut selon `Board.Statut`), puis rattachement de tous
      les `PostIt` existants à cette étape, dans
      `backend/src/ScrumMaster.Api/Data/Migrations/` (dépend de T008) — migration scaffoldée
      réordonnée manuellement (CreateTable des nouvelles tables → backfill SQL → drop/rename de
      l'ancien schéma) pour que le backfill s'exécute avant la suppression des colonnes source ;
      appliquée avec succès sur `scrummaster-dev-postgres`
- [X] T011 Créer `EtapeService` : validation d'une séquence (≥1 étape, ≥2 options par étape
      `PollPersonnalise`), résolution de l'étape active d'un board, `AvancerEtapeAsync` (clôt
      l'étape active, active la suivante ou clôture le board si aucune) dans
      `backend/src/ScrumMaster.Api/Services/EtapeService.cs` (dépend de T010)
- [X] T012 Réviser `BoardService.CreateBoardAsync` pour construire une séquence d'étapes (par
      défaut une seule "Colonnes et post-its" si `themeId`/`themePersonnalise` sont fournis sans
      séquence explicite — FR-014) ; réviser `GetBoardStateAsync` pour renvoyer `Etapes[]` au lieu
      de `theme`/`colonnes`/`postIts` au premier niveau dans
      `backend/src/ScrumMaster.Api/Services/BoardService.cs`,
      `backend/src/ScrumMaster.Api/Dtos/BoardDtos.cs` (dépend de T011)
- [X] T013 Réviser `PostItService` et `VoteService` : portée par `EtapeId` au lieu de `BoardId`,
      comptage des votes utilisés par étape (`data-model.md`) dans
      `backend/src/ScrumMaster.Api/Services/{PostItService.cs,VoteService.cs}` (dépend de T012)
- [X] T014 Réviser `RetroBoardHub` : `CloseBoard` → `AvancerEtape` (diffuse `EtapeChangee` ou
      `BoardClosed` selon `research.md#4`) ; `ChangeTheme`/`AddPostIt`/`EditPostIt`/`MovePostIt`/
      `DeletePostIt`/`Vote`/`RemoveVote` rejettent si l'étape active n'est pas de type
      "Colonnes et post-its" dans `backend/src/ScrumMaster.Api/Hubs/RetroBoardHub.cs` (dépend de
      T013)
- [X] T015 Réviser `AzureDevOpsBoardService` (specs/005-azure-devops-boards) : `ImporterWorkItemsAsync`/
      `ExporterPostItAsync` résolvent désormais l'étape "Colonnes et post-its" active du board
      (`research.md#5`) dans `backend/src/ScrumMaster.Api/Services/AzureDevOpsBoardService.cs`
      (dépend de T014)
- [X] T016 Adapter les tests existants cassés par la restructuration (`PostItTests`, `VoteTests`,
      `ThemeChangeTests`, `BoardClosureTests`, `PollBotAssociationTests`/`PollTriggerTests`/
      `PollVoteTests`/`PollClosureTests` [helpers `CreateBot`], `RappelAutomatiqueTests`/
      `RappelManuelTests`, `ThemeIconeContexteTests`, `ImportWorkItemsTests`, `ExportPostItTests`)
      dans `backend/tests/ScrumMaster.Api.Tests/` — `CloseBoard` → `AvancerEtape`, accès aux
      champs déplacés sous `Etapes[0]` (dépend de T015) — 8 fichiers cassés par la compilation
      (`BoardClosureTests`, `BoardsControllerTests`, `ExportPostItTests`,
      `RetroBoardHubRealtimeTests`, `RetroBoardHubTests`, `ThemeChangeTests`,
      `ThemeIconeContexteTests`, `VoteTests`) corrigés ; les tests Poll/Rappel/Import/Azure DevOps
      compilaient déjà sans changement ; suite complète : 61/61 tests passent
      (`dotnet test backend/tests/ScrumMaster.Api.Tests`)
- [X] T017 [P] Adapter `frontend/src/types.ts` : `BoardState.etapes[]` remplace `theme`/`colonnes`/
      `postIts` au premier niveau (dépend de T012)
- [X] T018 [P] Adapter minimalement `frontend/src/pages/BoardPage.tsx` et
      `frontend/src/pages/CreateBoardPage.tsx` pour fonctionner avec un board mono-étape
      (rebranchés sur `etapes[0]`, `AvancerEtape` au lieu de `CloseBoard`) — même expérience
      qu'aujourd'hui, sans composition explicite (dépend de T017) — `CreateBoardPage.tsx` inchangé
      (DTO `CreateBoardRequest` déjà rétrocompatible) ; `BoardPage.tsx` rebranché sur `etapes[0]` ;
      vérifié en navigateur (création de board, ajout de post-it, vote, clôture via
      `AvancerEtape`) sans erreur console

**Checkpoint**: Fondations prêtes — un board mono-étape (comportement par défaut) fonctionne de
bout en bout exactement comme avant cette feature (FR-014). L'implémentation des user stories peut
commencer.

---

## Phase 2: User Story 1 - Composer une rétro en plusieurs étapes (Priority: P1) 🎯 MVP

**Goal**: Le facilitateur compose explicitement une séquence de plusieurs étapes à la création
d'un board, et la fait avancer pendant la session.

**Independent Test**: Composer un board avec au moins deux étapes "Colonnes et post-its" (thèmes
différents), vérifier que seule la première est active au démarrage, puis que
`AvancerEtape` fait apparaître la seconde et rend la première consultable en lecture seule.

### Tests for User Story 1

- [X] T019 [P] [US1] Test d'intégration : `POST /api/boards` avec une séquence explicite de
      plusieurs étapes crée le board avec la première étape active ; `AvancerEtape` active la
      suivante et diffuse `EtapeChangee` ; l'étape précédente reste consultable en lecture seule ;
      une séquence vide est rejetée (FR-001, FR-002, FR-004, FR-007) dans
      `backend/tests/ScrumMaster.Api.Tests/EtapeSequenceTests.cs`

### Implementation for User Story 1

- [X] T020 [US1] Étendre `CreateBoardRequest`/`BoardService.CreateBoardAsync` pour accepter une
      liste `Etapes` explicite (types mixtes) dans
      `backend/src/ScrumMaster.Api/{Dtos/BoardDtos.cs,Services/BoardService.cs}` (dépend de T012 ;
      complète la construction déjà posée en Foundational pour le cas mono-étape implicite) — déjà
      posé lors de la restructuration Foundational (`EtapeService.ConstruireSequenceAsync`
      gérait déjà les types mixtes) ; correction apportée ici : une séquence explicite mais vide
      (`etapes: []`) est désormais rejetée (FR-002, `contracts/rest-api-delta.md`) au lieu de
      silencieusement retomber sur le comportement mono-étape (qui reste réservé au cas `etapes`
      omis/`null`, FR-014)
- [X] T021 [US1] Compléter `AvancerEtape` (`EtapeService`, `RetroBoardHub`) pour le cas
      multi-étapes réel : activer l'étape suivante et diffuser `EtapeChangee` (déjà posé en
      Foundational T014 pour le seul cas "dernière étape") dans
      `backend/src/ScrumMaster.Api/{Services/EtapeService.cs,Hubs/RetroBoardHub.cs}` (dépend de
      T020) — déjà général dès la restructuration Foundational (`AvancerEtapeAsync` active
      systématiquement l'étape suivante par `Ordre` si elle existe, sinon clôture le board) ;
      validé par T019 (`EtapeSequenceTests.cs`, 3/3 tests passent)
- [X] T022 [P] [US1] `frontend/src/pages/CreateBoardPage.tsx` : composer une séquence d'étapes
      (ajouter/retirer, choisir le type et sa configuration par étape) (dépend de T021) — nouveau
      composant `frontend/src/components/EtapeSequenceEditor.tsx` (ajout/retrait/réordonnancement,
      configuration par type) ; toggle "étape unique" (comportement historique inchangé) / "séquence"
      dans `CreateBoardPage.tsx` ; nouvel endpoint `GET /api/mini-jeux`
      (`Controllers/MiniJeuxController.cs`, non prévu dans le plan initial mais nécessaire pour
      peupler le sélecteur de mini-jeu) exposé via `boardsApi.getMiniJeux`
- [X] T023 [P] [US1] `frontend/src/pages/BoardPage.tsx` : afficher l'étape active selon son type,
      bouton "étape suivante / clôturer" (`AvancerEtape`), consultation en lecture seule des
      étapes déjà terminées (dépend de T021) — aperçu de la séquence, rendu par type via un
      switch (`renderEtape`), bouton dynamique ("Étape suivante" / "Clôturer le board" selon la
      position dans la séquence), étapes `Terminee` affichées en lecture seule repliable (`<details>`) ;
      bug découvert et corrigé pendant la vérification navigateur : le handler `BoardClosed` ne
      mettait à jour que `board.statut`, laissant le `statut` de la dernière étape figé à `Active`
      côté client (alors que le serveur la marque `Terminee`) — remplacé par une resynchronisation
      complète, sur le même modèle que `EtapeChangee`

**Checkpoint**: User Story 1 fonctionnelle de façon autonome.

---

## Phase 3: User Story 2 - Insérer un mini-jeu dans la séquence (Priority: P2)

**Goal**: Le facilitateur insère une étape "Mini-jeu" (Météo d'équipe) ; les participants y
répondent pendant qu'elle est active.

**Independent Test**: Composer un board avec une étape "Mini-jeu", répondre depuis plusieurs
comptes participants, vérifier que chaque réponse est prise en compte et qu'un participant peut la
changer.

### Tests for User Story 2

- [X] T024 [P] [US2] Test d'intégration : `RepondreMiniJeu` enregistre/remplace la réponse d'un
      participant pour l'étape active de type `MiniJeu` ; rejette si l'étape n'est pas active ou
      n'est pas de ce type (FR-003, FR-009) dans
      `backend/tests/ScrumMaster.Api.Tests/MiniJeuTests.cs`

### Implementation for User Story 2

- [X] T025 [US2] Implémenter `MiniJeuService.RepondreAsync` (upsert `ReponseMeteoEquipe` par
      `(EtapeId, ParticipantId)`) dans `backend/src/ScrumMaster.Api/Services/MiniJeuService.cs`
      (dépend de T021)
- [X] T026 [US2] Implémenter `RetroBoardHub.RepondreMiniJeu` et l'événement
      `ReponseMiniJeuChangee` dans `backend/src/ScrumMaster.Api/Hubs/RetroBoardHub.cs` (dépend de
      T025)
- [X] T027 [P] [US2] `frontend/src/components/EtapeMiniJeuMeteo.tsx` : choix d'humeur, affichage
      agrégé des réponses (dépend de T026)

**Checkpoint**: User Stories 1 et 2 fonctionnelles ensemble.

---

## Phase 4: User Story 3 - Insérer un poll personnalisé dans la séquence (Priority: P3)

**Goal**: Le facilitateur insère une étape "Poll personnalisé" (question + options) ; les
participants y répondent, avec décompte visible par tous.

**Independent Test**: Composer une étape de poll personnalisé, répondre depuis plusieurs comptes,
vérifier le décompte par option et le remplacement d'une réponse précédente.

### Tests for User Story 3

- [X] T028 [P] [US3] Test d'intégration : composer une étape `PollPersonnalise` (question + ≥2
      options) ; `RepondrePollPersonnalise` enregistre/remplace une réponse, décompte correct par
      option, rejet si `optionId` n'appartient pas à l'étape (FR-010, FR-011, FR-012) dans
      `backend/tests/ScrumMaster.Api.Tests/PollPersonnaliseTests.cs`

### Implementation for User Story 3

- [X] T029 [US3] Implémenter `PollPersonnaliseService.RepondreAsync` (upsert
      `ReponsePollPersonnalise` par `(EtapeId, ParticipantId)`, calcul du décompte par option)
      dans `backend/src/ScrumMaster.Api/Services/PollPersonnaliseService.cs` (dépend de T021)
- [X] T030 [US3] Implémenter `RetroBoardHub.RepondrePollPersonnalise` et l'événement
      `ReponsePollPersonnaliseChangee` dans `backend/src/ScrumMaster.Api/Hubs/RetroBoardHub.cs`
      (dépend de T029, séquentiel après T026 — même fichier)
- [X] T031 [P] [US3] `frontend/src/components/EtapePollPersonnalise.tsx` : composition
      question/options (`CreateBoardPage.tsx`), réponse et décompte (`BoardPage.tsx`) (dépend de
      T030)

**Checkpoint**: Les trois user stories sont fonctionnelles ensemble.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Validation complète de la feature et de la non-régression sur specs/001, 004, 005.

- [X] T032 Exécuter la validation `quickstart.md` de bout en bout (les 8 scénarios, y compris la
      non-régression d'un board mono-étape et de l'import/export Azure DevOps) et corriger les
      écarts constatés — scénarios 1/2/7 (composer, avancer, clôture finale) vérifiés en navigateur
      de bout en bout sur un board à 3 étapes (Colonnes et post-its → Mini-jeu → Poll personnalisé),
      ce qui a révélé et permis de corriger le bug `BoardClosed` (T023) ; scénario 3 (mono-étape)
      vérifié en navigateur (Foundational, T018) et par `BoardClosureTests`/`VoteTests`/
      `ThemeChangeTests`/`RetroBoardHubTests` ; scénario 4 (mini-jeu) vérifié en navigateur
      (1 participant) et par `MiniJeuTests` (changement de réponse, rejet hors étape active) ;
      scénario 5 (poll personnalisé) vérifié en navigateur et par `PollPersonnaliseTests`
      (2 participants simultanés, décompte, remplacement de réponse) ; scénario 6 (board
      pré-existant) validé via l'application réussie du backfill de migration (T010) sur
      `scrummaster-dev-postgres` plutôt que via un board littéralement créé avant cette feature
      (environnement de dev recréé pendant la session) ; scénario 8 (import/export Azure DevOps)
      couvert par `ImportWorkItemsTests`/`ExportPostItTests`, déjà adaptés et passants (T016), non
      rejoué manuellement en navigateur faute de configuration Azure DevOps de test disponible dans
      cette session — suite complète : 70/70 tests passent
      (`dotnet test backend/tests/ScrumMaster.Api.Tests`), `npx tsc --noEmit` sans erreur côté
      frontend

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 1)**: Aucune dépendance — démarre immédiatement, bloque tout le reste
  (restructuration atomique, rien ne compile avant sa fin)
- **User Stories (Phase 2-4)**: Dépendent toutes de Foundational ; US2 et US3 sont indépendantes
  l'une de l'autre mais partagent `RetroBoardHub.cs` (s'enchaînent par fichier) ; US1 est un
  prérequis pratique pour démontrer US2/US3 (il faut une séquence à plusieurs étapes pour y
  insérer un mini-jeu ou un poll), bien que le code d'US2/US3 ne dépende pas fonctionnellement du
  frontend d'US1
- **Polish (Phase 5)**: Dépend des trois user stories

### User Story Dependencies

- **US1 (P1)**: Démarre après Foundational — apporte la composition explicite multi-étapes
- **US2 (P2)**: Démarre après Foundational (T021) — nécessite une séquence à plusieurs étapes
  (US1) pour être démontrée de bout en bout, mais son code (service, méthode de hub, composant) est
  indépendant de celui d'US1
- **US3 (P3)**: Démarre après Foundational (T021) — même relation qu'US2

### Parallel Opportunities

- T001-T005 [P] (nouveaux modèles) en parallèle
- T009 [P] (seeder) en parallèle du reste de Foundational une fois T002 fait
- T017-T018 [P] (frontend minimal) en parallèle du backend une fois les contrats stabilisés (T012)
- Les tests marqués `[P]` de chaque story en parallèle entre eux
- Le frontend de chaque story ([P]) peut avancer en parallèle du backend de la story suivante

---

## Parallel Example: Foundational

```bash
Task: "Créer le modèle Étape dans backend/src/ScrumMaster.Api/Models/Etape.cs"
Task: "Créer le modèle MiniJeuCatalogue dans backend/src/ScrumMaster.Api/Models/MiniJeuCatalogue.cs"
Task: "Créer le modèle ReponseMeteoEquipe dans backend/src/ScrumMaster.Api/Models/ReponseMeteoEquipe.cs"
Task: "Créer le modèle OptionPollPersonnalise dans backend/src/ScrumMaster.Api/Models/OptionPollPersonnalise.cs"
Task: "Créer le modèle ReponsePollPersonnalise dans backend/src/ScrumMaster.Api/Models/ReponsePollPersonnalise.cs"
```

---

## Implementation Strategy

### MVP First (Foundational + User Story 1 uniquement)

1. Compléter Phase 1 (Foundational) — garantit déjà la non-régression FR-014
2. Compléter Phase 2 (User Story 1)
3. **STOP et VALIDER** : un facilitateur peut composer et parcourir une séquence de plusieurs
   étapes "Colonnes et post-its"
4. Démontrer / déployer si prêt

### Incremental Delivery

1. Foundational → non-régression garantie, fondations prêtes
2. + US1 → tester (composition et navigation) → démontrer (MVP)
3. + US2 → tester (mini-jeu) → démontrer
4. + US3 → tester (poll personnalisé) → démontrer
5. + Polish (validation quickstart complète, non-régression specs/001/004/005)

---

## Notes

- `[P]` = fichiers différents, aucune dépendance non résolue
- Chaque story doit rester indépendamment testable (voir "Independent Test" de chaque phase)
- Committer après chaque tâche ou groupe logique de tâches
- S'arrêter à chaque checkpoint pour valider la story avant de poursuivre
- Vu l'ampleur de la Phase Foundational, exécuter `dotnet build`/`dotnet test` fréquemment pendant
  T006-T016 plutôt qu'une seule fois à la fin — la restructuration touche trop de fichiers pour
  diagnostiquer les erreurs a posteriori efficacement
