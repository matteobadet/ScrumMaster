# Tasks: Thèmes Visuels par Colonne

**Input**: Design documents from `/specs/007-themes-visuels-colonnes/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, et les features
specs/001-retro-board-base, specs/004-themes-narratifs, specs/006-systeme-extensions-etapes déjà
implémentées (réutilise `Colonne`, `Theme`, `EtapeService`, `ThemeEditor`,
`EtapeSequenceEditor`).

**Tests**: Incluses (tests d'intégration ciblés par user story, réutilisant
`TestWebApplicationFactory` comme le reste du backend).

**Organization**: Tâches groupées par user story (P1 → P3 de `spec.md`). `Couleur` et
`UrlIllustration` sont deux attributs jumeaux du même objet `Colonne` transportés par le même DTO
restructuré (`Colonnes: string[]` → `Colonnes: ColonneSummaireDto[]`) : cette restructuration
partagée est posée une seule fois en Foundational, puis chaque user story y ajoute sa propre
logique (validation, saisie, affichage) de façon indépendamment testable — même pattern que
specs/004-themes-narratifs (icône/contexte).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Peut s'exécuter en parallèle (fichiers différents, aucune dépendance non résolue)
- **[Story]**: User story associée (US1, US2, US3)

## Path Conventions

Extension du backend/frontend existants (voir `plan.md` — Project Structure) :
`backend/src/ScrumMaster.Api/`, `backend/tests/ScrumMaster.Api.Tests/`, `frontend/src/`.

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: Restructuration du modèle de données et des DTOs de colonne, partagée par les trois
user stories — sans elle, aucune ne peut ni compiler ni transporter ses données.

**⚠️ CRITICAL**: Aucune user story ne démarre avant la fin de cette phase.

- [X] T001 Étendre le modèle `Colonne` avec `Couleur` (string?, ≤30 caractères) et
      `UrlIllustration` (string?, ≤2048 caractères) dans
      `backend/src/ScrumMaster.Api/Models/Colonne.cs`
- [X] T002 Générer et appliquer la migration EF Core ajoutant les deux colonnes nullable
      correspondantes dans `backend/src/ScrumMaster.Api/Data/Migrations/` (dépend de T001) —
      migration additive sans avertissement de perte de données, appliquée avec succès sur
      `scrummaster-dev-postgres`
- [X] T003 Introduire `ColonneSummaireDto(string Intitule, string? Couleur, string?
      UrlIllustration)` et restructurer `ThemeSummaryDto.Colonnes`/`ThemePersonnaliseDto.Colonnes`
      de `IReadOnlyList<string>` vers `IReadOnlyList<ColonneSummaireDto>` dans
      `backend/src/ScrumMaster.Api/Dtos/ThemeDtos.cs` (dépend de T001 ; `research.md#4`)
- [X] T004 Étendre `ColonneDto` avec `Couleur`/`UrlIllustration` dans
      `backend/src/ScrumMaster.Api/Dtos/BoardDtos.cs` (dépend de T001)
- [X] T005 Adapter `EtapeService.ResolveThemeAsync`/`CopyTheme` pour propager `Couleur`/
      `UrlIllustration` de chaque colonne (copie depuis un thème prédéfini existant, ou
      construction depuis un `ThemePersonnaliseDto`) — propagation seule à ce stade, sans encore
      valider le contenu (validation posée par US1/US2) dans
      `backend/src/ScrumMaster.Api/Services/EtapeService.cs` (dépend de T003, T004) — fait en même
      temps que T012/T016 (une seule passe d'édition du fichier, propagation + validation)
- [X] T006 Adapter `ThemesController.GetThemes` à la nouvelle forme de `Colonnes` dans
      `backend/src/ScrumMaster.Api/Controllers/ThemesController.cs` (dépend de T003)
- [X] T007 Adapter les tests existants cassés par la restructuration de `ThemePersonnaliseDto.Colonnes`
      (`MiniJeuTests`, `EtapeSequenceTests`, `BoardClosureTests`, `ThemeChangeTests`,
      `ThemeIconeContexteTests`, `BoardsControllerTests`) dans
      `backend/tests/ScrumMaster.Api.Tests/` (dépend de T003, T005) — `BoardsControllerTests.cs`
      n'a finalement nécessité aucun changement (son seul usage était un tableau vide `[]`,
      target-typé correctement par le compilateur sans modification)
- [X] T008 [P] Restructurer `frontend/src/types.ts` : `ThemeSummary.colonnes`,
      `ThemePersonnalise.colonnes` et la variante `custom` de `ThemeSelection` passent à une liste
      d'objets `{ intitule, couleur?, urlIllustration? }` ; `ColonneState` gagne `couleur`/
      `urlIllustration` (dépend de T003, T004)
- [X] T009 Restructurer la ligne par colonne de `frontend/src/components/ThemeEditor.tsx` pour se
      lier au nouvel objet (édition de l'intitulé seul à ce stade, sans encore les champs couleur/
      URL — posés par US1/US2) dans `frontend/src/components/ThemeEditor.tsx` (dépend de T008) —
      fait en même temps que T013/T017 (une seule passe d'édition du fichier)
- [X] T010 Adapter la construction du payload `themePersonnalise.colonnes` (mapping intitulé +
      couleur + urlIllustration, filtrage des intitulés vides) dans
      `frontend/src/pages/{CreateBoardPage.tsx,BoardPage.tsx}` et
      `frontend/src/components/EtapeSequenceEditor.tsx` (dépend de T008) — logique de
      nettoyage/validation factorisée dans une fonction exportée
      `buildColonnesPersonnalisees` (`ThemeEditor.tsx`), réutilisée par les trois fichiers
      plutôt que dupliquée

**Checkpoint**: Fondations prêtes — le modèle et les DTOs transportent `Couleur`/
`UrlIllustration` de bout en bout (sans validation ni UI de saisie/affichage encore). L'implémentation
des user stories peut commencer.

---

## Phase 2: User Story 1 - Colorer chaque colonne d'un thème (Priority: P1) 🎯 MVP

**Goal**: Le facilitateur associe une couleur de fond à chaque colonne d'un thème (prédéfini ou
personnalisé) ; chaque colonne du board affiche sa couleur propre.

**Independent Test**: Créer un board avec un thème personnalisé dont chaque colonne porte une
couleur différente et vérifier que chaque colonne affiche sa couleur propre ; laisser une colonne
sans couleur et vérifier l'absence de tout espace vide ou erreur.

### Tests for User Story 1

- [X] T011 [P] [US1] Test d'intégration : un board créé avec un thème personnalisé dont les
      colonnes portent des couleurs renvoie ces couleurs via `GET /api/boards/{id}` ; une colonne
      sans couleur renvoie `null` sans erreur ; une couleur de plus de 30 caractères est rejetée
      (400) (FR-001, FR-002, FR-005) dans
      `backend/tests/ScrumMaster.Api.Tests/ThemeVisuelColonneTests.cs`

### Implementation for User Story 1

- [X] T012 [US1] Ajouter la validation de longueur de `Couleur` (≤30 caractères, rejet explicite si
      dépassée) dans `EtapeService.ResolveThemeAsync`/`CopyTheme` dans
      `backend/src/ScrumMaster.Api/Services/EtapeService.cs` (dépend de T005, séquentiel après T007
      — même fichier)
- [X] T013 [US1] Ajouter le champ de saisie de la couleur par colonne (thème personnalisé) dans
      `frontend/src/components/ThemeEditor.tsx` (dépend de T009)
- [X] T014 [US1] Afficher la couleur de fond propre à chaque colonne du board dans
      `frontend/src/components/Colonne.tsx` (dépend de T010)

**Checkpoint**: User Story 1 fonctionnelle de façon autonome.

---

## Phase 3: User Story 2 - Illustrer chaque colonne d'une image (Priority: P2)

**Goal**: Le facilitateur associe une URL d'illustration à chaque colonne d'un thème ; chaque
colonne du board affiche son image propre, chargée directement par le navigateur du participant.

**Independent Test**: Créer un board avec un thème personnalisé dont chaque colonne porte une URL
d'illustration HTTPS valide et vérifier que chaque colonne affiche son image propre ; tenter une
URL non-HTTPS et vérifier le rejet ; simuler un lien cassé et vérifier que la colonne reste
utilisable.

### Tests for User Story 2

- [X] T015 [US2] Test d'intégration : un board créé avec un thème personnalisé dont les colonnes
      portent des URLs d'illustration HTTPS valides renvoie ces URLs via `GET /api/boards/{id}` ;
      une URL non-HTTPS (ex : `http://...`) est rejetée (400, FR-009) ; une URL de plus de 2048
      caractères est rejetée (400) ; une colonne sans URL renvoie `null` sans erreur (FR-003,
      FR-004, FR-005) dans `backend/tests/ScrumMaster.Api.Tests/ThemeVisuelColonneTests.cs` (même
      fichier que T011, séquentiel)

### Implementation for User Story 2

- [X] T016 [US2] Ajouter la validation d'URL d'illustration (schéma HTTPS obligatoire si non vide,
      longueur ≤2048 caractères, rejet explicite sinon) dans
      `EtapeService.ResolveThemeAsync`/`CopyTheme` dans
      `backend/src/ScrumMaster.Api/Services/EtapeService.cs` (dépend de T012, séquentiel — même
      fichier ; `research.md#3`)
- [X] T017 [US2] Ajouter le champ de saisie de l'URL d'illustration par colonne (thème
      personnalisé), avec validation côté client pour un retour immédiat, dans
      `frontend/src/components/ThemeEditor.tsx` (dépend de T013, séquentiel — même fichier)
- [X] T018 [US2] Afficher l'illustration propre à chaque colonne du board, avec repli silencieux
      si l'image ne charge pas (FR-010) dans `frontend/src/components/Colonne.tsx` (dépend de
      T014, séquentiel — même fichier)

**Checkpoint**: User Stories 1 et 2 fonctionnelles ensemble.

---

## Phase 4: User Story 3 - Démontrer l'effet avec un thème prédéfini entièrement habillé (Priority: P3)

**Goal**: Un facilitateur peut choisir un thème prédéfini du catalogue déjà entièrement habillé
(couleur et illustration sur chaque colonne), sans configuration manuelle.

**Independent Test**: À la création d'un board, choisir le thème prédéfini "La rétro du
randonneur" sans rien configurer, et vérifier que toutes ses colonnes affichent immédiatement une
couleur et une illustration.

### Tests for User Story 3

- [X] T019 [P] [US3] Test d'intégration : `GET /api/themes` contient le thème "La rétro du
      randonneur" dont chaque colonne porte une couleur et une URL d'illustration ; un board créé
      avec ce `themeId` copie ces couleurs/URLs sur ses colonnes (FR-008) dans
      `backend/tests/ScrumMaster.Api.Tests/ThemeVisuelColonneTests.cs` (même fichier que T011/T015,
      séquentiel)

### Implementation for User Story 3

- [X] T020 [US3] Rendre `ThemeSeeder.EnsureSeededAsync` idempotent par thème (vérifie l'existence
      par `Nom` plutôt que globalement sur toute la table, pour permettre d'ajouter ce nouveau
      thème sans réinitialiser une base déjà seedée) et ajouter le thème prédéfini "La rétro du
      randonneur" (5 colonnes, chacune avec une couleur pastel et une URL d'illustration
      `placehold.co`, `research.md#5`) dans `backend/src/ScrumMaster.Api/Data/ThemeSeeder.cs`
      (dépend de T001) — vérifié sur la base de dev déjà seedée (2 thèmes existants) : le nouveau
      thème s'est ajouté sans toucher aux 2 précédents, confirmant l'idempotence par nom

**Checkpoint**: Les trois user stories sont fonctionnelles ensemble.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Validation complète de la feature et non-régression sur specs/001, specs/004,
specs/006.

- [X] T021 Exécuter la validation `quickstart.md` de bout en bout (les 8 scénarios, y compris la
      non-régression sur les thèmes/boards existants) et corriger les écarts constatés — suite
      complète : 77/77 tests passent (`dotnet test`), `npx tsc --noEmit` sans erreur ; vérifié en
      navigateur : (1-2) thème personnalisé avec couleurs/illustrations par colonne, couleurs et
      images rendues correctement (styles calculés vérifiés) ; (3) rejet immédiat côté client d'une
      URL non-HTTPS avant même la soumission, puis rejet serveur si contournée ; (4) thème prédéfini
      "La rétro du randonneur" sélectionnable et entièrement habillé sans configuration, board créé
      avec les 5 couleurs/illustrations correctement copiées ; (5) colonnes sans couleur/illustration
      (thèmes existants "Start / Stop / Continue", "Mad / Sad / Glad") inchangées, aucune régression ;
      aucune erreur console sur l'ensemble du parcours

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 1)**: Aucune dépendance — démarre immédiatement, bloque toutes les user
  stories (restructuration partagée du DTO de colonne)
- **User Stories (Phase 2-4)**: Dépendent toutes de Foundational ; US2 s'enchaîne après US1 par
  fichier (`EtapeService.cs`, `ThemeEditor.tsx`, `Colonne.tsx` partagés) mais reste fonctionnellement
  indépendante ; US3 dépend uniquement du modèle posé en Foundational (T001), pas de la logique
  de saisie US1/US2, mais nécessite T011/T015 comme fichier de test partagé
- **Polish (Phase 5)**: Dépend des trois user stories

### User Story Dependencies

- **US1 (P1)**: Démarre après Foundational — aucune dépendance fonctionnelle à US2/US3
- **US2 (P2)**: Démarre après Foundational — partage les mêmes fichiers qu'US1
  (`EtapeService.cs`, `ThemeEditor.tsx`, `Colonne.tsx`) donc s'exécute après elle sur ces fichiers,
  mais sa logique (URL) est indépendante de celle d'US1 (couleur)
- **US3 (P3)**: Démarre après Foundational — n'a besoin que du modèle de données (T001), peut donc
  être développée en parallèle d'US1/US2 côté implémentation (`ThemeSeeder.cs` est un fichier
  différent), mais son test d'intégration partage le fichier de test avec US1/US2

### Parallel Opportunities

- T008 [P] (types frontend) en parallèle de T003-T007 (backend Foundational)
- T011 [P] (test US1) peut être écrit dès T007 terminé, avant l'implémentation (TDD)
- T019 [P] (test US3) en parallèle de T011/T015 une fois le fichier de test créé
- `ThemeSeeder.cs` (US3, T020) est un fichier différent de `EtapeService.cs`/`ThemeEditor.tsx`/
  `Colonne.tsx` (US1/US2) — peut avancer en parallèle après Foundational

---

## Parallel Example: Foundational

```bash
Task: "Restructurer frontend/src/types.ts vers le nouvel objet ColonneSummaireDto"
Task: "Étendre le modèle Colonne, restructurer les DTOs et migrer la base (backend)"
```

---

## Implementation Strategy

### MVP First (Foundational + User Story 1 uniquement)

1. Compléter Phase 1 (Foundational)
2. Compléter Phase 2 (User Story 1)
3. **STOP et VALIDER** : un facilitateur peut colorer chaque colonne de son thème personnalisé
4. Démontrer / déployer si prêt

### Incremental Delivery

1. Foundational → modèle et DTOs prêts
2. + US1 → tester (couleur) → démontrer (MVP)
3. + US2 → tester (illustration) → démontrer
4. + US3 → tester (thème prédéfini habillé) → démontrer
5. + Polish (validation quickstart complète, non-régression specs/001/004/006)

---

## Notes

- `[P]` = fichiers différents, aucune dépendance non résolue
- Chaque story doit rester indépendamment testable (voir "Independent Test" de chaque phase)
- Committer après chaque tâche ou groupe logique de tâches
- S'arrêter à chaque checkpoint pour valider la story avant de poursuivre

## Suite (2026-08-17, retour utilisateur post-implémentation)

Hors des 21 tâches ci-dessus, trois correctifs/ajouts appliqués directement suite à un test manuel
en navigateur (voir spec.md, section Amendement) :

- Ajout de `Colonne.SousTitre` (≤150 caractères), même mécanisme que `Couleur`/`UrlIllustration` —
  modèle, migration additive, DTOs (paramètre optionnel pour ne pas casser les tests existants),
  validation de longueur, seed du thème "La rétro du randonneur" avec les questions directrices
  d'origine (nécessitant une correction manuelle en base de dev pour les copies de thème déjà
  créées pendant la session, la table `Colonnes` n'étant pas re-seedée automatiquement), UI de
  saisie (`ThemeEditor.tsx`) et d'affichage (`Colonne.tsx`) — 2 nouveaux tests d'intégration dans
  `ThemeVisuelColonneTests.cs`.
- Correction de contraste : le titre/sous-titre d'une colonne calcule désormais sa couleur de texte
  à partir de la luminance perçue de `Couleur` (`Colonne.tsx`), au lieu d'utiliser la couleur de
  texte fixe du thème de l'app — sans quoi certaines couleurs de colonne rendaient le titre
  illisible.
- Correction de contraste des options natives de `<select>` (`index.css`), sans lien avec
  l'habillage de colonne.

Suite complète après ces correctifs : 79/79 tests backend passent, `tsc --noEmit` sans erreur,
revérifié en navigateur (thème "La rétro du randonneur" complet avec sous-titres, dropdown
"Déplacer vers" lisible, contraste correct sur fond clair et foncé).
