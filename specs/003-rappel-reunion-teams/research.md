# Phase 0 Research: Rappel de Réunion Teams

## 1. Hébergement et déclenchement automatique

**Decision**: Le rappel automatique (FR-001) est déclenché en code, en synchrone, immédiatement
après un appel réussi à `PollService.CloturerAsync` dont le résultat est `ReunionMaintenue`, dans
le même tour de conversation que la commande `clore` du bot (`RetroPollBot.TraiterCloreAsync`) —
pas de job planifié, pas de messagerie proactive.

**Rationale**: Cohérent avec `specs/002-poll-utilite-reunion/research.md#1` (pas de messagerie
proactive nécessaire pour ce MVP) et le Principe VI de la constitution (pas de sur-ingénierie) : la
clôture du poll est déjà un point de code exécuté dans un tour de conversation existant, donc le
rappel peut s'y accrocher directement sans introduire d'infrastructure de tâches planifiées.

**Alternatives considered**:
- **Job planifié / worker en arrière-plan** : nécessaire seulement pour un vrai rappel différé
  dans le temps (ex: rappel envoyé peu avant l'heure de la réunion) — hors périmètre de cette
  feature (Assumptions de `spec.md` : aucune notion d'horaire capturée).

## 2. Nouveau service dédié plutôt qu'extension de `PollService`

**Decision**: Un nouveau service `RappelService` porte la logique de rappel (dédoublonnage,
enregistrement), plutôt que d'ajouter ces responsabilités à `PollService`.

**Rationale**: `RappelService` a un cycle de vie et des règles d'unicité propres (par équipe/type/
jour, indépendamment de l'existence d'un poll — voir US2 de `spec.md`) ; un rappel manuel n'a pas
besoin d'un `PollUtilite`. Coupler cette logique dans `PollService` mélangerait deux concepts
distincts (sondage vs. notification) sans bénéfice, à l'inverse du Principe VI (éviter les
couplages qui compliquent l'évolution ultérieure — ex: rappels avec horaire, feature suivante sur
les invitations Graph).

**Alternatives considered**:
- **Ajouter les méthodes à `PollService`** : rejeté — casserait l'indépendance testable de US2
  (rappel manuel sans poll) et alourdirait un service déjà focalisé sur le cycle de vie du poll.

## 3. Représentation du dédoublonnage (FR-008)

**Decision**: Une nouvelle entité `RappelEnvoye` (Id, AreaPath, TypeReunion, Date, DateEnvoi) avec
une contrainte d'unicité `(AreaPath, TypeReunion, Date)`, sur le même modèle que
`PollUtilite` (specs/002-poll-utilite-reunion/data-model.md). Un enregistrement existant pour le
jour courant bloque tout nouvel envoi (silencieux pour l'automatique, rejet explicite pour le
manuel — voir `spec.md` Edge Cases et FR-008).

**Rationale**: Réutilise un pattern déjà validé (contrainte d'unicité par équipe/type/jour) plutôt
que d'inventer un mécanisme de verrouillage ad hoc (ex: cache en mémoire, qui ne survivrait pas à
un redémarrage du pod et casserait le Principe V — service sans état persistant fiable).

**Alternatives considered**:
- **Compteur en mémoire / cache** : rejeté, non durable entre redémarrages de pod et incompatible
  avec plusieurs réplicas (non utilisé actuellement, mais à ne pas fermer la porte — Principe VI).

## 4. Commande texte du bot

**Decision**: Nouvelle commande `rappeler <mêlée|rétro>`, reconnue par `RetroPollBot` selon le même
mécanisme que `associer`/`sonder`/`clore` (texte reconnu dans `OnMessageActivityAsync`, voir
`specs/002-poll-utilite-reunion/contracts/bot-commands.md`).

**Rationale**: Cohérence avec les commandes déjà en place ; aucune nouvelle décision d'UX à
prendre, le mécanisme de reconnaissance de commande est déjà éprouvé par 3 commandes existantes.

**Alternatives considered**: Aucune — la clarification de `specs/002-poll-utilite-reunion/spec.md`
a déjà tranché sur la forme des commandes bot pour toute cette famille de fonctionnalités.
