# Tasks: Poll d'Utilité de Réunion

**Input**: Design documents from `/specs/002-poll-utilite-reunion/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, et la feature
specs/001-retro-board-base déjà implémentée (réutilise `ScrumMaster.Api`, `ScrumMasterDbContext`,
l'entité `Equipe`).

**Tests**: Incluses (tests d'intégration ciblés par user story, utilisant
`Microsoft.Bot.Builder.Adapters.TestAdapter` — voir `research.md#5`) — pas de mode TDD strict
imposé, mais à écrire avant l'implémentation de la même story dans la mesure du possible.

**Organization**: Tâches groupées par user story (P1 → P3 de `spec.md`) pour permettre une
implémentation et une validation indépendantes de chacune.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Peut s'exécuter en parallèle (fichiers différents, aucune dépendance non résolue)
- **[Story]**: User story associée (US1, US2, US3)

## Path Conventions

Extension du backend existant (voir `plan.md` — Project Structure) :
`backend/src/ScrumMaster.Api/`, `backend/tests/ScrumMaster.Api.Tests/`. Aucun frontend impliqué.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Ajout des dépendances Bot Framework SDK au projet existant.

- [X] T001 [P] Ajouter les paquets `Microsoft.Bot.Builder`,
      `Microsoft.Bot.Builder.Integration.AspNet.Core` et `AdaptiveCards` à
      `backend/src/ScrumMaster.Api/ScrumMaster.Api.csproj`
- [X] T002 [P] Scaffolder les dossiers `backend/src/ScrumMaster.Api/Bots/` et
      `backend/src/ScrumMaster.Api/Cards/`
- [X] T003 [P] Ajouter le paquet `Microsoft.Bot.Builder` (le `TestAdapter` en fait partie, pas de
      paquet séparé) à `backend/tests/ScrumMaster.Api.Tests/ScrumMaster.Api.Tests.csproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Modèle de données et infrastructure Bot Framework partagés par toutes les user
stories.

**⚠️ CRITICAL**: Aucune user story ne démarre avant la fin de cette phase.

- [X] T004 [P] Étendre le modèle `Equipe` avec `TeamsChannelId` (string, nullable) dans
      `backend/src/ScrumMaster.Api/Models/Equipe.cs`
- [X] T005 [P] Créer le modèle `PollUtilite` (avec enums `TypeReunion` et `StatutPoll`) dans
      `backend/src/ScrumMaster.Api/Models/PollUtilite.cs`
- [X] T006 [P] Créer le modèle `VoteUtilite` (avec enum `ReponseVote`) dans
      `backend/src/ScrumMaster.Api/Models/VoteUtilite.cs`
- [X] T007 Configurer dans `ScrumMasterDbContext` les nouveaux `DbSet`, la contrainte d'unicité
      `(AreaPath, TypeReunion, Date)` sur `PollUtilite`, et `(PollId, TeamsUserId)` sur
      `VoteUtilite` dans `backend/src/ScrumMaster.Api/Data/ScrumMasterDbContext.cs` (dépend de
      T004-T006)
- [X] T008 Générer et appliquer la migration EF Core pour le nouveau schéma dans
      `backend/src/ScrumMaster.Api/Data/Migrations/` (dépend de T007)
- [X] T009 Configurer les identifiants Bot Framework (`MicrosoftAppId`/`MicrosoftAppPassword`) via
      la configuration d'environnement dans
      `backend/src/ScrumMaster.Api/appsettings.json`/`appsettings.Development.json` (voir
      research.md#4). `MicrosoftAppType=None` ajouté en Development pour permettre les tests
      locaux (Emulator) sans enregistrement Azure Bot réel ; `SingleTenant` reste le défaut en
      production (échec rapide si les identifiants ne sont pas fournis).
- [X] T010 Enregistrer les services Bot Framework et mapper l'endpoint `/api/messages` dans
      `backend/src/ScrumMaster.Api/Program.cs` (dépend de T009). `BotFrameworkAuthentication`
      n'est **pas** enregistré séparément : `CloudAdapter` construit sa propre
      `ConfigurationBotFrameworkAuthentication` depuis `IConfiguration`, et l'enregistrer en plus
      rendait ses deux constructeurs ambigus pour le conteneur DI (échec au démarrage) — bug
      trouvé et corrigé pendant l'implémentation.
- [X] T011 Créer le squelette `RetroPollBot` (`ActivityHandler`, `OnMessageActivityAsync` et
      `OnInvokeActivityAsync`) dans `backend/src/ScrumMaster.Api/Bots/RetroPollBot.cs` (dépend de
      T010)
- [X] T012 [P] Créer le squelette `PollCardBuilder` dans
      `backend/src/ScrumMaster.Api/Cards/PollCardBuilder.cs`. Un squelette `PollService` a
      également été créé à cette phase (non prévu explicitement dans le découpage initial) car
      `Program.cs` (T010) doit l'enregistrer en DI avant que T014 ne l'implémente.

**Checkpoint**: Fondations prêtes — l'implémentation des user stories peut commencer.

---

## Phase 3: User Story 1 - Associer le channel Teams de l'équipe (Priority: P1) 🎯 MVP

**Goal**: Un facilitateur associe le channel Teams de son équipe à l'Area Path de cette équipe via
une commande adressée au bot.

**Independent Test**: Envoyer la commande `associer <area-path>` dans un channel de test et
vérifier que `Equipe.TeamsChannelId` est mis à jour ; vérifier le rejet pour un Area Path inconnu.

### Tests for User Story 1

- [X] T013 [P] [US1] Test d'intégration : la commande `associer` met à jour `TeamsChannelId` pour
      une équipe existante, et rejette une Area Path inconnue (FR-001, FR-002) dans
      `backend/tests/ScrumMaster.Api.Tests/PollBotAssociationTests.cs`. Complété par un test de
      la commande non reconnue (message d'aide).

### Implementation for User Story 1

- [X] T014 [US1] Implémenter `PollService.AssocierChannelAsync` dans
      `backend/src/ScrumMaster.Api/Services/PollService.cs` (dépend de T007). Aucun contrôle de
      rôle facilitateur appliqué : contrairement au board de rétrospective (specs/001, rôle par
      session), il n'existe pas de mapping durable identité Teams ↔ rôle d'équipe dans cette
      feature — seule l'appartenance au channel Teams (contrôlée par Teams) restreint qui peut
      exécuter la commande. Documenté en commentaire dans le code ; écart assumé par rapport à
      l'Assumption de spec.md qui présupposait ce contrôle.
- [X] T015 [US1] Implémenter dans `RetroPollBot` le parsing de la commande `associer <area-path>`
      (retrait de la mention du bot, extraction de l'argument) et l'appel à `PollService`, avec
      message de confirmation ou d'erreur dans
      `backend/src/ScrumMaster.Api/Bots/RetroPollBot.cs` (dépend de T011, T014)

**Checkpoint**: User Story 1 fonctionnelle de façon autonome.

---

## Phase 4: User Story 2 - Recevoir le poll et voter (Priority: P2)

**Goal**: Un membre déclenche un poll par commande, et l'équipe vote via les boutons de l'Adaptive
Card, avec possibilité de changer son vote.

**Independent Test**: Déclencher un poll pour une équipe déjà associée, voter depuis plusieurs
comptes (y compris en changeant de réponse), et vérifier le décompte affiché sur la carte.

### Tests for User Story 2

- [X] T016 [P] [US2] Test d'intégration : `sonder <type>` crée un poll et envoie la carte, rejette
      si aucun channel n'est associé, rejette si un poll est déjà ouvert pour le jour (FR-003,
      Edge Cases) dans `backend/tests/ScrumMaster.Api.Tests/PollTriggerTests.cs`
- [X] T017 [P] [US2] Test d'intégration : un clic sur la carte enregistre le vote, un second clic
      du même membre remplace son vote précédent (FR-007), un vote sur un poll clos est rejeté
      (FR-008) dans `backend/tests/ScrumMaster.Api.Tests/PollVoteTests.cs`. Le rejet est vérifié
      via `AdaptiveCardInvokeResponse.StatusCode` (400) lu dans `InvokeResponse.Body` — le
      `InvokeResponse.Status` transport reste 200 pour toute invoke `adaptiveCard/action` traitée
      avec succès par le pipeline, qu'elle soit acceptée ou refusée au niveau applicatif.

### Implementation for User Story 2

- [X] T018 [US2] Implémenter `PollService.DeclencherPollAsync` (résolution de l'équipe par
      channel, contrôle qu'aucun poll n'est déjà ouvert pour ce type/jour, création) dans
      `backend/src/ScrumMaster.Api/Services/PollService.cs` (dépend de T007)
- [X] T019 [US2] Implémenter `PollService.VoterAsync` (upsert du vote par `(PollId, TeamsUserId)`,
      contrôle poll ouvert) dans `backend/src/ScrumMaster.Api/Services/PollService.cs` (dépend de
      T007)
- [X] T020 [US2] Implémenter `PollCardBuilder.BuildPollCard` (titre, type de réunion, décompte
      courant, boutons vote "Utile"/"Pas nécessaire" — voir contracts/adaptive-cards.md) dans
      `backend/src/ScrumMaster.Api/Cards/PollCardBuilder.cs` (dépend de T012). Écart par rapport au
      texte de la tâche : boutons `Action.Execute` (Universal Actions) et non `Action.Submit` —
      décision déjà actée dans research.md#2, qui permet la mise à jour en place de la carte via
      la réponse à l'invoke plutôt qu'un second appel proactif.
- [X] T021 [US2] Implémenter dans `RetroPollBot` le parsing de la commande
      `sonder <mêlée|rétro>`, l'appel à `PollService.DeclencherPollAsync` et l'envoi de la carte
      dans `backend/src/ScrumMaster.Api/Bots/RetroPollBot.cs` (dépend de T015, T018, T020)
- [X] T022 [US2] Implémenter dans `RetroPollBot` le traitement des activités `Invoke`
      (`adaptiveCard/action`) pour le vote et l'appel à `PollService.VoterAsync` dans
      `backend/src/ScrumMaster.Api/Bots/RetroPollBot.cs` (dépend de T019, T020, T021). Écart par
      rapport au texte de la tâche : la carte mise à jour est renvoyée comme `Value` de
      l'`AdaptiveCardInvokeResponse` (mécanisme Action.Execute standard), pas via
      `UpdateActivityAsync` — cohérent avec le choix Action.Execute de T020/research.md#2.

**Checkpoint**: User Stories 1 et 2 fonctionnelles ensemble.

---

## Phase 5: User Story 3 - Consulter le résultat du poll (Priority: P3)

**Goal**: Un membre clôture le poll par commande, et le résultat (réunion maintenue ou non)
s'affiche pour toute l'équipe.

**Independent Test**: Clôturer un poll ayant reçu des votes et vérifier que le résultat affiché
est cohérent avec la règle FR-009 ; clôturer un poll sans vote et vérifier le résultat par défaut.

### Tests for User Story 3

- [ ] T023 [P] [US3] Test d'intégration : `clore <type>` clôt le poll ouvert, calcule le résultat
      (maintenue si ≥1 vote "Utile", "pas nécessaire" sinon, maintenue par défaut si aucun vote —
      FR-009, Assumptions), et rejette si aucun poll n'est ouvert dans
      `backend/tests/ScrumMaster.Api.Tests/PollClosureTests.cs`

### Implementation for User Story 3

- [ ] T024 [US3] Implémenter `PollService.CloturerAsync` (résolution du poll ouvert par
      channel/type/jour, passage `Statut = Cloture`, calcul du résultat FR-009) dans
      `backend/src/ScrumMaster.Api/Services/PollService.cs` (dépend de T007)
- [ ] T025 [US3] Implémenter `PollCardBuilder.BuildResultCard` (résultat, décompte détaillé par
      votant avec son nom — FR-012) dans `backend/src/ScrumMaster.Api/Cards/PollCardBuilder.cs`
      (dépend de T012)
- [ ] T026 [US3] Implémenter dans `RetroPollBot` le parsing de la commande
      `clore <mêlée|rétro>`, l'appel à `PollService.CloturerAsync` et l'envoi de la carte de
      résultat dans `backend/src/ScrumMaster.Api/Bots/RetroPollBot.cs` (dépend de T015, T024,
      T025)

**Checkpoint**: Les 3 user stories sont fonctionnelles.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Exposition du endpoint bot en production et validation complète.

- [ ] T027 [P] Étendre `k8s/overlays/production/ingress.yaml` (specs/001-retro-board-base) pour
      router le chemin `/api/messages` vers le Service `scrummaster-api` existant
- [ ] T028 [P] Ajouter les identifiants Bot Framework (`MicrosoftAppId`/`MicrosoftAppPassword`) au
      Secret Kubernetes de production — distinct du Secret de connexion PostgreSQL (voir
      research.md#4) — et documenter la procédure dans `k8s/README.md`
- [ ] T029 Exécuter la validation `quickstart.md` de bout en bout (Bot Framework Emulator ou
      tests `TestAdapter`) et corriger les écarts constatés

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Aucune dépendance — démarre immédiatement
- **Foundational (Phase 2)**: Dépend de Setup — bloque toutes les user stories
- **User Stories (Phase 3-5)**: Dépendent toutes de Foundational ; US2 et US3 réutilisent le
  parsing de commande introduit par US1 (T015) mais restent testables indépendamment via
  `PollService` et le `TestAdapter`
- **Polish (Phase 6)**: Dépend des trois user stories (le endpoint `/api/messages` doit gérer
  toutes les commandes avant d'être exposé en production)

### User Story Dependencies

- **US1 (P1)**: Démarre après Foundational — aucune dépendance à une autre story
- **US2 (P2)**: Démarre après Foundational — s'appuie sur le parsing de commande introduit par
  US1 (T015) pour ajouter ses propres commandes, mais son cœur métier (`PollService`,
  `PollCardBuilder`) est indépendant
- **US3 (P3)**: Démarre après Foundational — s'appuie sur le poll créé par US2 pour être testée à
  l'échelle du parcours complet, mais la clôture et le calcul du résultat sont indépendants

### Parallel Opportunities

- T001-T003 (Setup) en parallèle
- T004-T006 (modèles d'entités) en parallèle
- Les tests marqués `[P]` de chaque story en parallèle entre eux
- T027-T028 (k8s) en parallèle

---

## Parallel Example: User Story 2

```bash
# Tests en parallèle :
Task: "Test d'intégration sonder <type> dans backend/tests/ScrumMaster.Api.Tests/PollTriggerTests.cs"
Task: "Test d'intégration vote via carte dans backend/tests/ScrumMaster.Api.Tests/PollVoteTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 uniquement)

1. Compléter Phase 1 (Setup) et Phase 2 (Foundational)
2. Compléter Phase 3 (User Story 1)
3. **STOP et VALIDER** : une équipe peut associer son channel Teams à son Area Path
4. Démontrer / déployer si prêt

### Incremental Delivery

1. Setup + Foundational → fondations prêtes
2. + US1 → tester (association) → démontrer (MVP)
3. + US2 → tester (poll + vote) → démontrer
4. + US3 → tester (clôture + résultat) → démontrer
5. + Polish (exposition production, validation quickstart complète)

---

## Notes

- `[P]` = fichiers différents, aucune dépendance non résolue
- Chaque story doit rester indépendamment testable (voir "Independent Test" de chaque phase)
- Committer après chaque tâche ou groupe logique de tâches
- S'arrêter à chaque checkpoint pour valider la story avant de poursuivre
