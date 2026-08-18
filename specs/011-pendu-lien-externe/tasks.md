# Tasks: Nouveaux mini-jeux — Pendu et Lien externe

**Input**: Design documents from `/specs/011-pendu-lien-externe/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, et les features
specs/006-systeme-extensions-etapes (système de mini-jeux, catalogue) et specs/008-roti-mini-jeu
(précédent le plus récent d'ajout de mini-jeu) déjà implémentées.

**Tests**: Incluses (tests d'intégration ciblés par user story, réutilisant
`TestWebApplicationFactory` comme le reste du backend).

**Organization**: Tâches groupées par user story (P1 → P2 de `spec.md`). Le catalogue/DTOs/modèle
partagés sont posés une seule fois en Foundational ; chaque mini-jeu (Pendu, Lien externe) a sa
propre méthode de hub et son propre service, car leurs mécaniques diffèrent structurellement l'une
de l'autre et de `RepondreMiniJeu` (research.md#1, #5).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Peut s'exécuter en parallèle (fichiers différents, aucune dépendance non résolue)
- **[Story]**: User story associée (US1, US2)

## Path Conventions

Extension du backend/frontend existants (voir `plan.md` — Project Structure) :
`backend/src/ScrumMaster.Api/`, `backend/tests/ScrumMaster.Api.Tests/`, `frontend/src/`.

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: Modèle, DTOs, catalogue et helper partagé — sans eux, aucune user story ne peut ni
compiler ni transporter de donnée.

**⚠️ CRITICAL**: Aucune user story ne démarre avant la fin de cette phase.

- [X] T001 [P] Créer le modèle `LettreProposeePendu` (EtapeId, Lettre, Correcte,
      ParticipantProposantId, DateProposition) dans
      `backend/src/ScrumMaster.Api/Models/LettreProposeePendu.cs` (data-model.md)
- [X] T002 Étendre `Etape` avec `MotAPendu`, `LienExterneNom`, `LienExterneUrl` (tous nullable) et
      `List<LettreProposeePendu> LettresProposeesPendu` dans
      `backend/src/ScrumMaster.Api/Models/Etape.cs` (dépend de T001)
- [X] T003 Configurer dans `ScrumMasterDbContext` le nouveau `DbSet<LettreProposeePendu>`, sa clé
      composite `(EtapeId, Lettre)` (research.md#3), et les longueurs de colonnes
      (`LienExterneUrl` ≤2048) dans `backend/src/ScrumMaster.Api/Data/ScrumMasterDbContext.cs`
      (dépend de T002)
- [X] T004 Générer et appliquer la migration EF Core additive (nouvelle table, 3 colonnes
      nullable) dans `backend/src/ScrumMaster.Api/Data/Migrations/` (dépend de T003)
- [X] T005 Ajouter au `MiniJeuSeeder` les entrées "Pendu" (`TypeInterne = "pendu"`) et "Lien
      externe" (`TypeInterne = "lien-externe"`), idempotentes par `TypeInterne` (pattern déjà en
      place) dans `backend/src/ScrumMaster.Api/Data/MiniJeuSeeder.cs` (dépend de T004 ;
      research.md#7)
- [X] T006 [P] Extraire la validation HTTPS déjà écrite dans `EtapeService` (illustration de
      colonne, visuels ROTI) dans un helper statique partagé dans
      `backend/src/ScrumMaster.Api/Services/UrlValidation.cs` (research.md#6)
- [X] T007 Faire utiliser `UrlValidation` par les appels existants (illustration de colonne,
      visuels ROTI) dans `backend/src/ScrumMaster.Api/Services/EtapeService.cs` (dépend de T006 ;
      aucun changement de comportement, refactor pur)
- [X] T008 [P] Créer `LettreProposeePenduDto`, étendre `EtapeRequestDto` (`MotPendu`) et `EtapeDto`
      (`MotMasquePendu`, `LettresProposeesPendu`, `EssaisRestantsPendu`, `MaxEssaisPendu`,
      `EtatPendu`, `MotCompletPendu`, `LienExterneNom`, `LienExterneUrl`) dans
      `backend/src/ScrumMaster.Api/Dtos/EtapeDtos.cs` (contracts/rest-api-delta.md)
- [X] T009 Adapter `EtapeService.CreerEtapeMiniJeuAsync` pour valider/stocker `MotPendu` (requis et
      non vide uniquement si le mini-jeu choisi est "pendu", rejeté sinon) dans
      `backend/src/ScrumMaster.Api/Services/EtapeService.cs` (dépend de T005, T008)
- [X] T010 Adapter `BoardService.BuildEtapeDto` pour peupler, pour une étape MiniJeu, les champs
      Pendu (mot masqué calculé à la demande depuis `MotAPendu`/`LettresProposeesPendu`, essais
      restants, état, mot complet si terminé — research.md#2) et Lien externe (nom/URL, `null` si
      non renseignés) dans `backend/src/ScrumMaster.Api/Services/BoardService.cs` (dépend de T008)
- [X] T011 [P] Étendre `frontend/src/types.ts` : `EtapeState` gagne les champs Pendu/Lien externe ;
      `EtapeRequest` gagne `motPendu` ; nouveau type `LettreProposeePendu` (dépend de T008)

**Checkpoint**: Le catalogue contient "Pendu" et "Lien externe" ; une étape Pendu composée avec un
mot est vérifiable via `GET` direct (mot masqué correct) ; une étape Lien externe se compose sans
contenu. Aucune interaction encore possible ; l'implémentation des user stories peut commencer.

---

## Phase 2: User Story 1 - Jouer une partie de Pendu en équipe (Priority: P1) 🎯 MVP

**Goal**: Composer une étape Pendu avec un mot ; une fois active, tout participant propose des
lettres jusqu'à victoire ou défaite, visible en temps réel par toute l'équipe.

**Independent Test**: Composer une étape Pendu avec un mot connu, proposer des lettres depuis
plusieurs comptes participants une fois l'étape active, vérifier la révélation/le décompte
d'essais/l'issue de partie pour tous.

### Implementation for User Story 1

- [X] T012 [P] [US1] Ajouter `MiniJeuService.ProposerLettrePenduAsync(boardId, etapeId,
      callerParticipantId, lettre)` : résout l'étape Pendu active, normalise la lettre
      (`ToUpperInvariant`, research.md#4), no-op silencieux si déjà proposée (research.md#3),
      sinon enregistre et calcule mot masqué/essais restants/état dans
      `backend/src/ScrumMaster.Api/Services/MiniJeuService.cs` (dépend de T009, T010)
- [X] T013 [US1] Ajouter `RetroBoardHub.ProposerLettrePendu(boardId, etapeId, lettre)` et diffuser
      `LettrePenduProposee` (contracts/realtime-hub-delta.md) dans
      `backend/src/ScrumMaster.Api/Hubs/RetroBoardHub.cs` (dépend de T012)

### Tests for User Story 1

- [X] T014 [P] [US1] Test d'intégration : mot masqué initial, lettre correcte révèle toutes ses
      occurrences, lettre incorrecte décrémente les essais, lettre déjà proposée est un no-op,
      victoire quand le mot est complet, défaite quand les essais sont épuisés (FR-001 à FR-007)
      dans `backend/tests/ScrumMaster.Api.Tests/PenduTests.cs` (dépend de T013)

### Frontend for User Story 1

- [X] T015 [US1] Créer `frontend/src/components/EtapeMiniJeuPendu.tsx` : affiche le mot masqué,
      un clavier de lettres A-Z (désactivant les lettres déjà proposées), les essais restants, et
      l'issue de partie (victoire/défaite avec mot complet) (dépend de T011)
- [X] T016 [US1] Adapter `frontend/src/components/EtapeSequenceEditor.tsx` : quand le mini-jeu
      choisi a pour `typeInterne` "pendu", afficher un champ "Mot à deviner" requis à la
      composition (dépend de T011)
- [X] T017 [US1] Adapter `renderEtape` (`frontend/src/pages/BoardPage.tsx`) pour router une étape
      MiniJeu "pendu" vers `EtapeMiniJeuPendu` (`invoke('ProposerLettrePendu', ...)`) ; ajouter le
      handler `LettrePenduProposee` dans `frontend/src/hooks/useRealtimeBoard.ts` pour patcher
      l'état en temps réel (dépend de T015, T013)

**Checkpoint**: User Story 1 fonctionnelle de façon autonome — une équipe peut jouer une partie de
Pendu complète.

---

## Phase 3: User Story 2 - Rediriger l'équipe vers un jeu externe (Priority: P2)

**Goal**: Le facilitateur ajoute une étape Lien externe sans contenu ; une fois active, il y
renseigne (ou modifie) en direct un nom et une URL, visibles immédiatement par tous.

**Independent Test**: Ajouter une étape Lien externe à une séquence, l'activer, y renseigner un nom
et une URL en tant que facilitateur, vérifier que chaque participant voit et peut utiliser le lien.

### Implementation for User Story 2

- [X] T018 [P] [US2] Ajouter `MiniJeuService.DefinirLienExterneAsync(boardId, etapeId,
      callerParticipantId, nom, url)` : réservé au facilitateur, valide `nom` non vide et `url`
      HTTPS via `UrlValidation` (research.md#6), enregistre/remplace
      `LienExterneNom`/`LienExterneUrl` dans
      `backend/src/ScrumMaster.Api/Services/MiniJeuService.cs` (dépend de T007, T010)
- [X] T019 [US2] Ajouter `RetroBoardHub.DefinirLienExterne(boardId, etapeId, nom, url)` et diffuser
      `LienExterneDefini` (contracts/realtime-hub-delta.md) dans
      `backend/src/ScrumMaster.Api/Hubs/RetroBoardHub.cs` (dépend de T018)

### Tests for User Story 2

- [X] T020 [P] [US2] Test d'intégration : étape sans lien renvoie un état d'attente ; le
      facilitateur définit un lien valide, diffusé à tous ; une URL non-HTTPS est rejetée ; un
      participant non-facilitateur est refusé ; le facilitateur peut modifier un lien déjà défini
      (FR-008 à FR-015) dans `backend/tests/ScrumMaster.Api.Tests/LienExterneTests.cs` (dépend de
      T019)

### Frontend for User Story 2

- [X] T021 [US2] Créer `frontend/src/components/EtapeMiniJeuLienExterne.tsx` : état d'attente
      explicite si non renseigné, sinon nom + lien cliquable (`target="_blank"`) pour tout
      participant ; formulaire de saisie/modification (nom, URL) visible uniquement au
      facilitateur (dépend de T011)
- [X] T022 [US2] Adapter `renderEtape` (`frontend/src/pages/BoardPage.tsx`) pour router une étape
      MiniJeu "lien-externe" vers `EtapeMiniJeuLienExterne`
      (`invoke('DefinirLienExterne', ...)` pour le facilitateur) ; ajouter le handler
      `LienExterneDefini` dans `frontend/src/hooks/useRealtimeBoard.ts` (dépend de T021, T019)

**Checkpoint**: Les deux user stories sont fonctionnelles ensemble.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Validation complète de la feature et non-régression sur specs/006/specs/008.

- [X] T023 Exécuter la validation `quickstart.md` de bout en bout (les 11 scénarios, y compris la
      non-régression des mini-jeux "Météo d'équipe" et "ROTI") et corriger les écarts constatés
- [X] T024 Exécuter la suite `dotnet test` complète et `npx tsc --noEmit` côté frontend

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 1)**: Aucune dépendance — démarre immédiatement, bloque les deux user
  stories (modèle, DTOs, catalogue, helper partagé)
- **User Stories (Phase 2-3)**: Dépendent de Foundational ; US1 et US2 sont indépendantes l'une de
  l'autre (fichiers backend distincts par méthode ; fichiers frontend distincts par composant),
  peuvent être développées dans n'importe quel ordre ou en parallèle
- **Polish (Phase 4)**: Dépend des deux user stories

### User Story Dependencies

- **US1 (P1)**: Démarre après Foundational — aucune dépendance fonctionnelle à US2
- **US2 (P2)**: Démarre après Foundational — aucune dépendance fonctionnelle à US1

### Parallel Opportunities

- T001, T006 [P] (nouveau modèle, nouveau helper) en parallèle dès le début du Foundational
- T008, T011 [P] (DTOs backend, types frontend) en parallèle
- T012 [P] / T018 [P] (services US1/US2) et T014 [P] / T020 [P] (tests US1/US2) en parallèle une
  fois Foundational terminé — fichiers distincts, aucune dépendance croisée
- Toute la Phase 2 (US1) et la Phase 3 (US2) peuvent être menées en parallèle par deux personnes
  différentes une fois Foundational terminé

---

## Parallel Example: Foundational

```bash
Task: "Créer LettreProposeePendu dans backend/src/ScrumMaster.Api/Models/LettreProposeePendu.cs"
Task: "Créer UrlValidation.cs dans backend/src/ScrumMaster.Api/Services/UrlValidation.cs"
```

## Parallel Example: User Stories (après Foundational)

```bash
Task: "Ajouter ProposerLettrePenduAsync dans backend/src/ScrumMaster.Api/Services/MiniJeuService.cs"
Task: "Ajouter DefinirLienExterneAsync dans backend/src/ScrumMaster.Api/Services/MiniJeuService.cs"
```

---

## Implementation Strategy

### MVP First (Foundational + User Story 1 uniquement)

1. Compléter Phase 1 (Foundational)
2. Compléter Phase 2 (User Story 1)
3. **STOP et VALIDER** : une équipe peut jouer une partie de Pendu complète
4. Démontrer / déployer si prêt

### Incremental Delivery

1. Foundational → catalogue et DTOs prêts
2. + US1 → tester (Pendu) → démontrer (MVP)
3. + US2 → tester (Lien externe) → démontrer
4. + Polish (validation quickstart complète, non-régression specs/006/specs/008)

---

## Notes

- `[P]` = fichiers différents, aucune dépendance non résolue
- Chaque story doit rester indépendamment testable (voir "Independent Test" de chaque phase)
- Committer après chaque tâche ou groupe logique de tâches
- S'arrêter à chaque checkpoint pour valider la story avant de poursuivre
