# Feature Specification: Système d'Extensions — Étapes de Rétrospective

**Feature Branch**: `006-systeme-extensions-etapes`

**Created**: 2026-08-16

**Status**: Draft

**Input**: User description: "Système d'extensions pour composer une rétrospective en plusieurs
étapes, sans écrire de code. Périmètre complet en trois capacités : (1) le facilitateur compose
une rétro comme une séquence de plusieurs étapes (ex : icebreaker → colonnes/post-its → vote →
actions) au lieu du board à un seul thème de colonnes actuel (specs/001-retro-board-base), et
avance le board d'une étape à la suivante pendant la session ; (2) le facilitateur peut insérer,
dans sa séquence, une étape de type "mini-jeu" choisie dans un catalogue prédéfini construit par
les développeurs (pas de mini-jeu personnalisé écrit par le facilitateur) ; (3) le facilitateur
peut insérer une étape de type "poll personnalisé" avec sa propre question et ses propres options
de réponse, affichée et répondue directement dans le board web — distinct du poll d'utilité de
réunion envoyé par le bot Teams (specs/002-poll-utilite-reunion), qui n'est pas modifié par cette
feature. Dans tous les cas, "composer sans coder" signifie que le facilitateur choisit et
configure des étapes parmi des types prédéfinis par les développeurs (comme il configure déjà un
thème personnalisé aujourd'hui), sans écrire de code ni définir la logique d'un nouveau type
d'étape."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Composer une rétro en plusieurs étapes (Priority: P1)

Le facilitateur compose, à la création d'un board, une séquence de plusieurs étapes (ex :
icebreaker, puis colonnes/post-its, puis vote, puis actions), et fait avancer le board d'une étape
à la suivante pendant la session.

**Why this priority**: C'est l'infrastructure sur laquelle reposent les deux autres capacités —
un mini-jeu ou un poll personnalisé sont eux-mêmes des types d'étapes insérables dans cette
séquence. Sans cette user story, aucune des deux autres n'a de séquence dans laquelle s'insérer.

**Independent Test**: Peut être testé en composant un board avec au moins deux étapes de type
"Colonnes et post-its" (deux thèmes différents), en vérifiant que seule la première est active au
démarrage, puis que le facilitateur peut la clôturer pour faire apparaître la seconde.

**Acceptance Scenarios**:

1. **Given** la création d'un board, **When** le facilitateur compose une séquence de plusieurs
   étapes en choisissant leur type et leur ordre, **Then** le board est créé avec cette séquence,
   la première étape étant active et visible par les participants.
2. **Given** un board avec plusieurs étapes, **When** le facilitateur clôt l'étape active,
   **Then** l'étape suivante de la séquence devient active et visible par tous les participants,
   et l'étape précédente reste consultable en lecture seule.
3. **Given** une séquence composée d'une seule étape de type "Colonnes et post-its", **When** le
   board est créé et utilisé, **Then** son comportement est identique à celui d'un board
   d'aujourd'hui (specs/001-retro-board-base) — aucune régression.
4. **Given** la dernière étape de la séquence, **When** le facilitateur la clôt, **Then** le board
   entier passe en lecture seule (équivalent à la clôture actuelle, specs/001-retro-board-base).

---

### User Story 2 - Insérer un mini-jeu dans la séquence (Priority: P2)

Le facilitateur insère, dans sa séquence d'étapes, une étape de type "mini-jeu" choisie dans un
catalogue prédéfini par les développeurs.

**Why this priority**: Ajoute une capacité ludique à la séquence déjà composable (P1), mais aucun
board ne dépend de cette capacité pour fonctionner.

**Independent Test**: Peut être testé en ajoutant une étape de type "mini-jeu" à une séquence, en
choisissant un mini-jeu du catalogue, et en vérifiant que les participants y accèdent lorsqu'elle
devient active.

**Acceptance Scenarios**:

1. **Given** la composition d'une séquence d'étapes, **When** le facilitateur ajoute une étape de
   type "mini-jeu" et choisit un mini-jeu dans le catalogue, **Then** cette étape apparaît dans la
   séquence à la position choisie.
2. **Given** une étape de type mini-jeu active, **When** un participant y accède, **Then** il voit
   l'activité interactive correspondante.

---

### User Story 3 - Insérer un poll personnalisé dans la séquence (Priority: P3)

Le facilitateur insère une étape de type "poll personnalisé" avec sa propre question et ses
propres options de réponse ; les participants y répondent directement dans le board.

**Why this priority**: Complète le catalogue de types d'étapes, indépendamment de la capacité
mini-jeu (P2).

**Independent Test**: Peut être testé en ajoutant une étape de poll personnalisé avec une question
et plusieurs options, en répondant depuis plusieurs comptes participants, et en vérifiant le
décompte affiché.

**Acceptance Scenarios**:

1. **Given** la composition d'une séquence d'étapes, **When** le facilitateur ajoute une étape de
   type "poll personnalisé" avec une question et au moins deux options de réponse, **Then** cette
   étape apparaît dans la séquence.
2. **Given** une étape de poll personnalisé active, **When** un participant y répond, **Then** sa
   réponse est enregistrée et le décompte des réponses par option est visible par tous les
   participants.
3. **Given** un participant a déjà répondu à un poll personnalisé actif, **When** il choisit une
   autre option, **Then** sa réponse précédente est remplacée (cohérent avec le mécanisme déjà
   retenu pour le poll d'utilité de réunion, specs/002-poll-utilite-reunion).

---

### Edge Cases

- Que se passe-t-il si le facilitateur tente de créer un board sans aucune étape dans sa
  séquence ? Rejeté, par analogie avec l'exigence d'au moins une colonne par thème
  (specs/001-retro-board-base FR-015) — au moins une étape est requise.
- Que se passe-t-il si un participant tente d'interagir avec une étape qui n'est pas (encore ou
  plus) active ? L'interaction est refusée ; une étape déjà terminée reste consultable en lecture
  seule uniquement.
- Que se passe-t-il si le facilitateur souhaite revenir à une étape précédente déjà terminée ?
  Hors périmètre de ce MVP — voir Assumptions (avancement strictement séquentiel).
- Que se passe-t-il si une étape de poll personnalisé se termine sans qu'aucun participant n'ait
  répondu ? L'étape se clôt normalement, le décompte affiché reste à zéro pour toutes les options.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système DOIT permettre au facilitateur de composer, à la création d'un board,
  une séquence d'une ou plusieurs étapes, chacune d'un type choisi parmi un catalogue prédéfini
  ("Colonnes et post-its", "Mini-jeu", "Poll personnalisé").
- **FR-002**: Le système DOIT rejeter la création d'un board dont la séquence ne comporte aucune
  étape.
- **FR-003**: Le système DOIT permettre au facilitateur de configurer chaque étape selon son type
  (ex : thème/colonnes pour "Colonnes et post-its", mini-jeu choisi pour "Mini-jeu", question et
  options pour "Poll personnalisé"), sans écrire de code.
- **FR-004**: Le système DOIT afficher aux participants uniquement l'étape actuellement active de
  la séquence.
- **FR-005**: Le système DOIT réserver au facilitateur, et à lui seul, la capacité de faire
  avancer le board de l'étape active à la suivante de la séquence (cohérent avec
  specs/001-retro-board-base FR-013).
- **FR-006**: Le système DOIT passer le board entier en lecture seule lorsque le facilitateur clôt
  la dernière étape de la séquence (cohérent avec specs/001-retro-board-base FR-016).
- **FR-007**: Une étape déjà terminée DOIT rester consultable en lecture seule par tous les
  participants après avoir cédé la place à la suivante.
- **FR-008**: Pour une étape de type "Colonnes et post-its", le système DOIT réutiliser sans
  changement les mécanismes déjà existants (colonnes, post-its, vote, thème —
  specs/001-retro-board-base, specs/004-themes-narratifs), scopés à cette étape précise : si une
  séquence comporte plusieurs étapes de ce type, chacune possède son propre thème et son propre
  ensemble de colonnes/post-its/votes, indépendants des autres étapes de la même séquence.
- **FR-009**: Pour une étape de type "Mini-jeu", le système DOIT proposer au facilitateur un
  catalogue de mini-jeux prédéfinis parmi lesquels choisir.
- **FR-010**: Pour une étape de type "Poll personnalisé", le système DOIT permettre au
  facilitateur de définir une question en texte libre et au moins deux options de réponse.
- **FR-011**: Pour une étape de type "Poll personnalisé" active, le système DOIT permettre à
  chaque participant de choisir une réponse parmi les options définies, et de la modifier tant que
  l'étape reste active (remplacement, pas de doublon).
- **FR-012**: Le système DOIT afficher, pour une étape de poll personnalisé, le décompte des
  réponses par option, visible par tous les participants.
- **FR-013**: Cette feature NE DOIT PAS modifier le poll d'utilité de réunion envoyé par le bot
  Teams (specs/002-poll-utilite-reunion) — les deux mécanismes de poll restent indépendants.
- **FR-014**: Un board créé avant cette feature DOIT conserver son comportement actuel, en étant
  traité comme une séquence à une seule étape de type "Colonnes et post-its" (aucune régression).

### Key Entities *(include if feature involves data)*

- **Étape**: fait partie de la séquence d'un Board de rétrospective ; possède un type ("Colonnes
  et post-its" | "Mini-jeu" | "Poll personnalisé"), un ordre dans la séquence, une configuration
  propre à son type, et un statut (à venir / active / terminée).
- **Board de rétrospective** (extension de specs/001-retro-board-base) : possède désormais une
  séquence d'une ou plusieurs Étapes, plutôt qu'un unique thème de colonnes directement ; les
  Participants restent rattachés au board dans son ensemble (un seul rejoint suffit pour toute la
  séquence), tandis que colonnes/post-its/votes deviennent rattachés à l'Étape "Colonnes et
  post-its" qui les porte (voir FR-008).
- **Poll personnalisé**: question et liste d'options de réponse, portées par une Étape de type
  "Poll personnalisé".
- **Réponse de poll personnalisé**: association entre un participant et l'option qu'il a choisie
  pour une étape de poll personnalisé donnée ; modifiable tant que l'étape reste active.
- **Mini-jeu**: référence à un type de mini-jeu du catalogue prédéfini, portée par une Étape de
  type "Mini-jeu".

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un facilitateur peut composer une séquence de plusieurs étapes et créer le board
  correspondant en moins de 5 minutes.
- **SC-002**: Les participants voient l'étape active se mettre à jour pour tous, sans rechargement
  manuel, en moins de 3 secondes après que le facilitateur l'a fait avancer.
- **SC-003**: Un board composé d'une seule étape "Colonnes et post-its" se comporte, du point de
  vue des participants, de façon identique à un board créé avant cette feature.
- **SC-004**: Un facilitateur peut ajouter une étape de poll personnalisé avec sa question et ses
  options en moins de 2 minutes.

## Assumptions

- "Composer sans coder" signifie que les types d'étapes eux-mêmes (Colonnes et post-its, Mini-jeu,
  Poll personnalisé) sont développés par l'équipe du projet ; le facilitateur choisit et configure
  des instances de ces types déjà existants, sans définir de nouveau type ni écrire de logique.
- L'avancement entre étapes est strictement séquentiel et à sens unique dans ce MVP : le
  facilitateur ne peut pas revenir à une étape précédente une fois qu'elle est terminée, cohérent
  avec la clôture définitive déjà retenue pour un board entier (specs/001-retro-board-base).
- Le contenu précis du catalogue de mini-jeux (quels jeux, combien) ne relève pas de cette
  spécification mais du plan technique et de l'implémentation ; au moins un mini-jeu doit exister
  pour que la capacité soit démontrable.
- Le "Poll personnalisé" introduit par cette feature est un mécanisme distinct et indépendant du
  poll d'utilité de réunion envoyé par le bot Teams (specs/002-poll-utilite-reunion) — les deux ne
  partagent ni données ni interface.
- Un board existant créé avant cette feature reste utilisable sans migration explicite de contenu,
  en étant traité comme une séquence à une seule étape.
