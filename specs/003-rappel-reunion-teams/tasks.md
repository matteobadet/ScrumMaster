# Tasks: Rappel de Réunion Teams

**Input**: Design documents from `/specs/003-rappel-reunion-teams/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, et la feature
specs/002-poll-utilite-reunion déjà implémentée (réutilise `ScrumMaster.Api`,
`ScrumMasterDbContext`, `Equipe.TeamsChannelId`, `PollService`, `RetroPollBot`).

**Tests**: Incluses (tests d'intégration ciblés par user story, utilisant
`Microsoft.Bot.Builder.Adapters.TestAdapter` — voir
`specs/002-poll-utilite-reunion/research.md#5`).

**Organization**: Tâches groupées par user story (P1 → P2 de `spec.md`) pour permettre une
implémentation et une validation indépendantes de chacune.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Peut s'exécuter en parallèle (fichiers différents, aucune dépendance non résolue)
- **[Story]**: User story associée (US1, US2)

## Path Conventions

Extension du backend existant (voir `plan.md` — Project Structure) :
`backend/src/ScrumMaster.Api/`, `backend/tests/ScrumMaster.Api.Tests/`. Aucun frontend ni nouvelle
dépendance impliqués.

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: Modèle de données et squelette de service partagés par les deux user stories.

**⚠️ CRITICAL**: Aucune user story ne démarre avant la fin de cette phase.

- [X] T001 [P] Créer l'entité `RappelEnvoye` (Id, AreaPath, TypeReunion, Date, DateEnvoi) dans
      `backend/src/ScrumMaster.Api/Models/RappelEnvoye.cs`
- [X] T002 Configurer dans `ScrumMasterDbContext` le nouveau `DbSet<RappelEnvoye>` et la contrainte
      d'unicité `(AreaPath, TypeReunion, Date)` dans
      `backend/src/ScrumMaster.Api/Data/ScrumMasterDbContext.cs` (dépend de T001)
- [X] T003 Générer et appliquer la migration EF Core pour la table `RappelsEnvoyes` dans
      `backend/src/ScrumMaster.Api/Data/Migrations/` (dépend de T002)
- [X] T004 Créer le squelette `RappelService`, l'enregistrer en DI (Scoped, comme `PollService`)
      et ajouter le paramètre au constructeur de `RetroPollBot` dans
      `backend/src/ScrumMaster.Api/Services/RappelService.cs`,
      `backend/src/ScrumMaster.Api/Program.cs` et
      `backend/src/ScrumMaster.Api/Bots/RetroPollBot.cs` (dépend de T003). Les tests existants
      instanciant `RetroPollBot` directement (`PollBotAssociationTests.cs`, `PollTriggerTests.cs`,
      `PollVoteTests.cs`, `PollClosureTests.cs`) devront être mis à jour pour passer un
      `RappelService` à leur helper `CreateBot`.

**Checkpoint**: Fondations prêtes — l'implémentation des user stories peut commencer.

---

## Phase 2: User Story 1 - Rappel automatique après un poll "réunion maintenue" (Priority: P1) 🎯 MVP

**Goal**: À la clôture d'un poll dont le résultat est "réunion maintenue", un message de rappel
est posté automatiquement à la suite de la carte de résultat.

**Independent Test**: Déclencher un poll, voter "Utile", clôturer, et vérifier qu'un message de
rappel apparaît immédiatement après la carte de résultat ; répéter avec uniquement des votes "Pas
nécessaire" et vérifier l'absence de rappel.

### Tests for User Story 1

- [X] T005 [P] [US1] Test d'intégration : `clore` avec résultat "maintenue" déclenche l'envoi
      automatique d'un rappel après la carte de résultat ; `clore` avec "pas nécessaire" n'envoie
      aucun rappel (FR-001, FR-002) dans
      `backend/tests/ScrumMaster.Api.Tests/RappelAutomatiqueTests.cs`

### Implementation for User Story 1

- [X] T006 [US1] Implémenter `RappelService.EnvoyerRappelAutomatiqueSiPossibleAsync` (résolution
      équipe par channel, contrôle silencieux du doublon `(AreaPath, TypeReunion, Date)`,
      enregistrement, retourne `bool`) dans
      `backend/src/ScrumMaster.Api/Services/RappelService.cs` (dépend de T004)
- [X] T007 [US1] Dans `RetroPollBot.TraiterCloreAsync`, après l'envoi de la carte de résultat,
      appeler `RappelService.EnvoyerRappelAutomatiqueSiPossibleAsync` si `ReunionMaintenue`, et
      poster le message de rappel uniquement si l'appel retourne `true` dans
      `backend/src/ScrumMaster.Api/Bots/RetroPollBot.cs` (dépend de T006)

**Checkpoint**: User Story 1 fonctionnelle de façon autonome.

---

## Phase 3: User Story 2 - Rappel manuel indépendant d'un poll (Priority: P2)

**Goal**: Un membre déclenche manuellement l'envoi d'un rappel pour un type de réunion, sans poll
préalable, via une commande adressée au bot.

**Independent Test**: Sur une équipe déjà associée, sans déclencher de poll, envoyer
`rappeler mêlée` et vérifier qu'un message de rappel apparaît ; vérifier le rejet sur un channel
non associé et sur une tentative de doublon le même jour.

### Tests for User Story 2

- [X] T008 [P] [US2] Test d'intégration : `rappeler <type>` envoie un rappel pour une équipe
      associée sans poll préalable ; rejette si le channel n'est associé à aucune équipe ; rejette
      si un rappel a déjà été envoyé aujourd'hui pour cette équipe/type de réunion (FR-003, FR-004,
      FR-008) dans `backend/tests/ScrumMaster.Api.Tests/RappelManuelTests.cs`

### Implementation for User Story 2

- [X] T009 [US2] Implémenter `RappelService.EnvoyerRappelManuelAsync` (résolution équipe par
      channel avec rejet si introuvable, contrôle du doublon avec rejet explicite si déjà envoyé
      aujourd'hui, enregistrement) dans `backend/src/ScrumMaster.Api/Services/RappelService.cs`
      (dépend de T004, séquentiel après T006 — même fichier)
- [X] T010 [US2] Implémenter dans `RetroPollBot` le parsing de la commande
      `rappeler <mêlée|rétro>`, l'appel à `RappelService.EnvoyerRappelManuelAsync` et le message
      de confirmation ou d'erreur, et mettre à jour le message d'aide de la commande non reconnue
      pour l'inclure dans `backend/src/ScrumMaster.Api/Bots/RetroPollBot.cs` (dépend de T009,
      séquentiel après T007 — même fichier)

**Checkpoint**: User Stories 1 et 2 fonctionnelles ensemble.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Validation complète de la feature.

- [X] T011 Exécuter la validation `quickstart.md` de bout en bout (y compris le scénario croisé
      "rappel manuel puis clôture automatique" — doublon silencieux) et corriger les écarts
      constatés

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 1)**: Aucune dépendance — démarre immédiatement, bloque toutes les user
  stories
- **User Stories (Phase 2-3)**: Dépendent de Foundational ; US2 touche les mêmes fichiers que US1
  (`RappelService.cs`, `RetroPollBot.cs`) donc s'enchaîne après elle par fichier, mais reste
  fonctionnellement indépendante (testable et livrable sans US2 après Phase 2 seule)
- **Polish (Phase 4)**: Dépend des deux user stories

### User Story Dependencies

- **US1 (P1)**: Démarre après Foundational — aucune dépendance fonctionnelle à US2
- **US2 (P2)**: Démarre après Foundational — partage les mêmes fichiers que US1 donc ses tâches
  s'exécutent après celles d'US1 sur ces fichiers, mais sa logique (rappel manuel, sans poll) est
  indépendante de celle d'US1 (rappel automatique post-clôture)

### Parallel Opportunities

- T001 (modèle) peut démarrer immédiatement
- T005 [P] (test US1) peut être écrit dès T004 terminé, en parallèle de T006-T007 (TDD)
- T008 [P] (test US2) peut être écrit dès T004 terminé, en parallèle de T009-T010 (TDD)

---

## Parallel Example: Tests

```bash
# Une fois Foundational terminé, les tests des deux stories peuvent être écrits en parallèle :
Task: "Test d'intégration rappel automatique dans backend/tests/ScrumMaster.Api.Tests/RappelAutomatiqueTests.cs"
Task: "Test d'intégration rappel manuel dans backend/tests/ScrumMaster.Api.Tests/RappelManuelTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 uniquement)

1. Compléter Phase 1 (Foundational)
2. Compléter Phase 2 (User Story 1)
3. **STOP et VALIDER** : un poll clôturé "maintenue" déclenche un rappel automatique
4. Démontrer / déployer si prêt

### Incremental Delivery

1. Foundational → fondations prêtes
2. + US1 → tester (rappel automatique) → démontrer (MVP)
3. + US2 → tester (rappel manuel) → démontrer
4. + Polish (validation quickstart complète, y compris le scénario croisé de doublon)

---

## Notes

- `[P]` = fichiers différents, aucune dépendance non résolue
- Chaque story doit rester indépendamment testable (voir "Independent Test" de chaque phase)
- Committer après chaque tâche ou groupe logique de tâches
- S'arrêter à chaque checkpoint pour valider la story avant de poursuivre

## Implémentation — notes

- Toutes les phases (Foundational, US1, US2, Polish) ont été implémentées en une seule passe
  (feature de taille réduite, aucune dépendance nouvelle).
- `RappelService.EnvoyerRappelAutomatiqueSiPossibleAsync` et `EnvoyerRappelManuelAsync` résolvent
  toutes deux l'équipe par `teamsChannelId` (même signature) plutôt que l'automatique ne reçoive un
  `AreaPath` déjà résolu — évite de toucher `PollService.PollClotureResult` (non prévu dans le
  Project Structure de `plan.md`), au prix d'une résolution d'équipe redondante mais négligeable.
- 4 fichiers de test existants (`PollBotAssociationTests.cs`, `PollTriggerTests.cs`,
  `PollVoteTests.cs`, `PollClosureTests.cs`) mis à jour pour passer un `RappelService` au
  constructeur de `RetroPollBot` (T004) — aucune assertion existante cassée, le message de rappel
  supplémentaire après un `clore` "maintenue" reste simplement une activité non consommée par les
  `AssertReply` déjà en place.
- T011 (validation `quickstart.md`) : exécuté via `dotnet test` (49/49, dont les 6 scénarios du
  quickstart couverts par `RappelAutomatiqueTests.cs` et `RappelManuelTests.cs`) et une
  vérification de la migration EF Core sur la base de développement (table `RappelsEnvoyes`
  créée avec succès, contrainte d'unicité en place).
