# Tasks: Mini-jeu ROTI

**Input**: Design documents from `/specs/008-roti-mini-jeu/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, et les features
specs/006-systeme-extensions-etapes (mini-jeu "Météo d'équipe", mécanisme `RepondreMiniJeu`) et
specs/007-themes-visuels-colonnes (mécanisme d'illustration par URL externe) déjà implémentées.

**Tests**: Incluses (tests d'intégration ciblés par user story, réutilisant
`TestWebApplicationFactory` comme le reste du backend).

**Organization**: Tâches groupées par user story (P1 → P2 de `spec.md`). Le catalogue/DTO/service
partagés sont posés une seule fois en Foundational (symétrique du mini-jeu "Météo d'équipe" déjà
existant) ; US1 démontre le ROTI par défaut, US2 y ajoute la personnalisation par niveau.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Peut s'exécuter en parallèle (fichiers différents, aucune dépendance non résolue)
- **[Story]**: User story associée (US1, US2)

## Path Conventions

Extension du backend/frontend existants (voir `plan.md` — Project Structure) :
`backend/src/ScrumMaster.Api/`, `backend/tests/ScrumMaster.Api.Tests/`, `frontend/src/`.

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: Modèle, DTOs, service et catalogue partagés par les deux user stories — sans eux,
aucune ne peut ni compiler ni transporter de donnée ROTI.

**⚠️ CRITICAL**: Aucune user story ne démarre avant la fin de cette phase.

- [X] T001 [P] Créer l'enum `NiveauRoti` (5 niveaux, `data-model.md`) et le modèle `ReponseRoti`
      (EtapeId, ParticipantId, Niveau, DateReponse) dans
      `backend/src/ScrumMaster.Api/Models/ReponseRoti.cs`
- [X] T002 [P] Créer le modèle `EtapeRotiVisuel` (EtapeId, Niveau, UrlIllustration) dans
      `backend/src/ScrumMaster.Api/Models/EtapeRotiVisuel.cs`
- [X] T003 Étendre `Etape` avec `List<ReponseRoti> ReponsesRoti` et
      `List<EtapeRotiVisuel> VisuelsRoti` dans `backend/src/ScrumMaster.Api/Models/Etape.cs`
      (dépend de T001, T002)
- [X] T004 Configurer dans `ScrumMasterDbContext` les nouveaux `DbSet`, les clés composites
      `(EtapeId, ParticipantId)`/`(EtapeId, Niveau)` et la contrainte de longueur
      `UrlIllustration` (≤2048) dans `backend/src/ScrumMaster.Api/Data/ScrumMasterDbContext.cs`
      (dépend de T001-T003)
- [X] T005 Générer et appliquer la migration EF Core additive (2 nouvelles tables) dans
      `backend/src/ScrumMaster.Api/Data/Migrations/` (dépend de T004)
- [X] T006 Rendre `MiniJeuSeeder.EnsureSeededAsync` idempotent par `TypeInterne` (même correctif
      que `ThemeSeeder`, specs/007-themes-visuels-colonnes) et ajouter l'entrée catalogue "ROTI"
      (`TypeInterne = "roti"`) dans `backend/src/ScrumMaster.Api/Data/MiniJeuSeeder.cs` (dépend de
      T005 ; `research.md#5`)
- [X] T007 Créer `NiveauVisuelDto`, `ReponseRotiDto`, et étendre `EtapeDto`
      (`ReponsesRoti?`, `MonNiveauRoti?`, `VisuelsRoti?`) et `EtapeRequestDto`
      (`RotiPersonnalisations?`) dans `backend/src/ScrumMaster.Api/Dtos/EtapeDtos.cs` (dépend de
      T001, T002 ; `data-model.md`)
- [X] T008 Adapter `EtapeService.CreerEtapeMiniJeuAsync` pour construire `VisuelsRoti` depuis
      `RotiPersonnalisations` (validation HTTPS/longueur identique à l'illustration de colonne,
      rejet si des personnalisations sont fournies pour un mini-jeu qui n'est pas ROTI) dans
      `backend/src/ScrumMaster.Api/Services/EtapeService.cs` (dépend de T007 ; `research.md#3`)
- [X] T009 Adapter `BoardService.BuildEtapeDto` pour peupler `ReponsesRoti`/`MonNiveauRoti`/
      `VisuelsRoti` d'une étape MiniJeu dont `TypeInterne == "roti"` (aux côtés du peuplement déjà
      existant pour "Météo d'équipe") dans `backend/src/ScrumMaster.Api/Services/BoardService.cs`
      (dépend de T007)
- [X] T010 Adapter `MiniJeuService.RepondreAsync` pour résoudre le `TypeInterne` du mini-jeu de
      l'étape et aiguiller vers le bon enum/la bonne collection (`HumeurMeteo`/`ReponsesMeteo` vs
      `NiveauRoti`/`ReponsesRoti`) dans `backend/src/ScrumMaster.Api/Services/MiniJeuService.cs`
      (dépend de T001, T003 ; `research.md#4`)
- [X] T011 [P] Étendre `frontend/src/types.ts` : `EtapeState` gagne `reponsesRoti?`,
      `monNiveauRoti?`, `visuelsRoti?` ; `EtapeRequest` gagne `rotiPersonnalisations?` ; nouveau
      type `NiveauVisuel` (dépend de T007)

**Checkpoint**: Fondations prêtes — le catalogue contient "ROTI", et l'API/le hub transportent une
réponse ROTI de bout en bout (vérifiable via `GET`/`POST` directs). Aucune interface de saisie/
affichage dédiée encore ; l'implémentation des user stories peut commencer.

---

## Phase 2: User Story 1 - Évaluer le retour sur temps investi avec le visuel par défaut (Priority: P1) 🎯 MVP

**Goal**: Le facilitateur insère une étape "ROTI" sans configuration ; les participants répondent
avec le visuel par défaut (emoji), leur réponse est visible par tous et modifiable.

**Independent Test**: Insérer une étape ROTI dans une séquence sans personnalisation, répondre
depuis plusieurs comptes participants une fois l'étape active, et vérifier que chaque réponse est
prise en compte et visible par tous.

### Tests for User Story 1

- [X] T012 [P] [US1] Test d'intégration : le catalogue (`GET /api/mini-jeux`) contient "ROTI" ;
      composer une étape ROTI sans personnalisation ; `RepondreMiniJeu` enregistre/remplace la
      réponse d'un participant (niveau valide) et la rejette si le niveau n'existe pas ; `GET
      /api/boards/{id}` renvoie `reponsesRoti`/`monNiveauRoti` (FR-001 à FR-004) dans
      `backend/tests/ScrumMaster.Api.Tests/RotiTests.cs`

### Implementation for User Story 1

- [X] T013 [US1] Créer `frontend/src/components/EtapeMiniJeuRoti.tsx` : échelle à 5 niveaux avec
      emoji par défaut, réponse et affichage des réponses des participants (mirroring
      `EtapeMiniJeuMeteo.tsx`) (dépend de T011)
- [X] T014 [US1] Adapter `renderEtape` (`frontend/src/pages/BoardPage.tsx`) pour aiguiller une
      étape `MiniJeu` vers `EtapeMiniJeuMeteo` ou `EtapeMiniJeuRoti` selon
      `etape.miniJeu?.typeInterne`, au lieu du seul `EtapeMiniJeuMeteo` actuel (dépend de T013 ;
      non-régression sur "Météo d'équipe" à vérifier)

**Checkpoint**: User Story 1 fonctionnelle de façon autonome — le ROTI est utilisable sans aucune
configuration, comme "Météo d'équipe".

---

## Phase 3: User Story 2 - Personnaliser le visuel de l'échelle ROTI (Priority: P2)

**Goal**: Le facilitateur remplace, pour un ou plusieurs niveaux, le visuel par défaut par sa
propre image (URL).

**Independent Test**: Composer une étape ROTI avec une image personnalisée sur un seul niveau,
vérifier que ce niveau affiche l'image fournie tandis que les 4 autres gardent leur emoji par
défaut.

### Tests for User Story 2

- [X] T015 [P] [US2] Test d'intégration : composer une étape ROTI avec `rotiPersonnalisations` sur
      un niveau (URL HTTPS valide) ; `GET /api/boards/{id}` renvoie cette personnalisation dans
      `visuelsRoti`, les autres niveaux restent absents ; une URL non-HTTPS ou une
      `rotiPersonnalisations` fournie pour "Météo d'équipe" sont rejetées (400) (FR-005 à FR-008)
      dans `backend/tests/ScrumMaster.Api.Tests/RotiTests.cs` (même fichier que T012, séquentiel)

### Implementation for User Story 2

- [X] T016 [US2] Adapter `frontend/src/components/EtapeSequenceEditor.tsx` : quand le mini-jeu
      choisi pour une étape MiniJeu a pour `typeInterne` "roti", afficher un champ de saisie d'URL
      facultatif par niveau (validation HTTPS côté client, cohérent avec
      specs/007-themes-visuels-colonnes) (dépend de T011)
- [X] T017 [US2] Adapter `EtapeMiniJeuRoti.tsx` pour afficher l'image personnalisée d'un niveau à
      la place de l'emoji par défaut quand `visuelsRoti` la fournit, avec repli silencieux si le
      lien est cassé (cohérent avec `Colonne.tsx`, specs/007-themes-visuels-colonnes FR-010)
      (dépend de T013, séquentiel — même fichier)

**Checkpoint**: Les deux user stories sont fonctionnelles ensemble.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Validation complète de la feature et non-régression sur specs/006.

- [X] T018 Exécuter la validation `quickstart.md` de bout en bout (les 6 scénarios, y compris la
      non-régression du mini-jeu "Météo d'équipe") et corriger les écarts constatés

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 1)**: Aucune dépendance — démarre immédiatement, bloque les deux user
  stories (catalogue, modèle, DTOs, service partagés)
- **User Stories (Phase 2-3)**: Dépendent de Foundational ; US2 s'enchaîne après US1 sur
  `EtapeMiniJeuRoti.tsx` (même fichier) mais reste fonctionnellement indépendante
- **Polish (Phase 4)**: Dépend des deux user stories

### User Story Dependencies

- **US1 (P1)**: Démarre après Foundational — aucune dépendance fonctionnelle à US2
- **US2 (P2)**: Démarre après Foundational — partage `EtapeMiniJeuRoti.tsx` avec US1 (affichage)
  donc s'exécute après elle sur ce fichier, mais sa logique (personnalisation) est indépendante de
  la réponse au ROTI par défaut (US1)

### Parallel Opportunities

- T001, T002 [P] (nouveaux modèles) en parallèle
- T011 [P] (types frontend) en parallèle de T008-T010 (backend) une fois T007 posé
- T012 [P] (test US1) et T015 [P] (test US2) peuvent être écrits dès Foundational terminé, avant
  l'implémentation (TDD)

---

## Parallel Example: Foundational

```bash
Task: "Créer NiveauRoti + ReponseRoti dans backend/src/ScrumMaster.Api/Models/ReponseRoti.cs"
Task: "Créer EtapeRotiVisuel dans backend/src/ScrumMaster.Api/Models/EtapeRotiVisuel.cs"
```

---

## Implementation Strategy

### MVP First (Foundational + User Story 1 uniquement)

1. Compléter Phase 1 (Foundational)
2. Compléter Phase 2 (User Story 1)
3. **STOP et VALIDER** : un facilitateur peut insérer une étape ROTI utilisable sans configuration
4. Démontrer / déployer si prêt

### Incremental Delivery

1. Foundational → catalogue et DTOs prêts
2. + US1 → tester (ROTI par défaut) → démontrer (MVP)
3. + US2 → tester (personnalisation par niveau) → démontrer
4. + Polish (validation quickstart complète, non-régression specs/006)

---

## Notes

- `[P]` = fichiers différents, aucune dépendance non résolue
- Chaque story doit rester indépendamment testable (voir "Independent Test" de chaque phase)
- Committer après chaque tâche ou groupe logique de tâches
- S'arrêter à chaque checkpoint pour valider la story avant de poursuivre
