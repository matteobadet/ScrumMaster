# Feature Specification: Rappel de Réunion Teams

**Feature Branch**: `003-rappel-reunion-teams`

**Created**: 2026-08-16

**Status**: Draft

**Input**: User description: "Rappel/invitation Teams pour la réunion (mêlée ou rétrospective) du
jour, envoyé par le bot dans le channel Teams de l'équipe. Déclenchement automatique à la clôture
d'un poll d'utilité (specs/002-poll-utilite-reunion) dont le résultat est 'réunion maintenue' ;
déclenchement également possible manuellement par un membre de l'équipe via une commande
textuelle adressée au bot, indépendamment d'un poll. Il s'agit d'un simple message de rappel
posté dans le channel (pas de création d'événement de calendrier réel via Microsoft Graph, pas de
gestion d'une liste de participants individuels) — visible par tout le channel Teams associé à
l'équipe, en réutilisant l'association channel/équipe déjà établie dans
specs/002-poll-utilite-reunion."

## Clarifications

### Session 2026-08-16

- Q: Quand un rappel automatique (poll clôturé "maintenue") et un rappel manuel visent la même
  réunion le même jour, faut-il empêcher le doublon ou laisser les deux passer ? → A: Empêcher le
  doublon — le second rappel (automatique ou manuel) pour la même équipe, le même type de réunion
  et le même jour est bloqué ; le déclencheur manuel reçoit un message indiquant qu'un rappel a
  déjà été envoyé aujourd'hui pour cette réunion.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Rappel automatique après un poll "réunion maintenue" (Priority: P1)

Lorsqu'un poll d'utilité de réunion (specs/002-poll-utilite-reunion) est clôturé et que son
résultat est "réunion maintenue", l'équipe reçoit automatiquement, dans la foulée, un message de
rappel confirmant que la réunion du jour a bien lieu.

**Why this priority**: C'est le scénario principal qui donne sa valeur à la feature — relier le
résultat du poll à une action concrète et visible, sans effort supplémentaire de l'équipe. Sans
cette user story, le résultat du poll reste un simple affichage sans suite.

**Independent Test**: Peut être testé en clôturant un poll dont le résultat calculé est "réunion
maintenue" (au moins un vote "Utile"), et en vérifiant qu'un message de rappel apparaît dans le
channel associé, immédiatement après le message de résultat du poll.

**Acceptance Scenarios**:

1. **Given** un poll d'utilité clôturé avec le résultat "réunion maintenue", **When** la clôture
   est traitée, **Then** un message de rappel annonçant que la réunion (mêlée ou rétrospective) a
   bien lieu est posté automatiquement dans le channel Teams de l'équipe.
2. **Given** un poll d'utilité clôturé avec le résultat "réunion pas nécessaire", **When** la
   clôture est traitée, **Then** aucun message de rappel n'est envoyé.

---

### User Story 2 - Rappel manuel indépendant d'un poll (Priority: P2)

Un membre de l'équipe déclenche directement l'envoi du rappel pour un type de réunion donné, sans
qu'un poll d'utilité ait été nécessairement déclenché ou clôturé au préalable.

**Why this priority**: Complète le scénario principal pour les équipes qui ne sondent pas
systématiquement l'utilité de leur réunion, mais souhaitent tout de même un rappel visible dans le
channel. Suppose que l'association équipe/channel (déjà réalisée pour le poll, specs/002) existe.

**Independent Test**: Peut être testé en adressant au bot la commande de rappel pour une équipe
déjà associée à un channel, sans avoir déclenché aucun poll au préalable, et en vérifiant que le
message de rappel apparaît.

**Acceptance Scenarios**:

1. **Given** une équipe associée à un channel Teams, **When** un membre adresse au bot la commande
   de rappel pour un type de réunion (mêlée ou rétrospective), **Then** un message de rappel
   annonçant cette réunion apparaît dans le channel, sans dépendre de l'existence d'un poll.
2. **Given** un channel non associé à une équipe, **When** un membre adresse au bot la commande de
   rappel, **Then** la commande est rejetée avec un message expliquant qu'il faut d'abord associer
   le channel.
3. **Given** un rappel (automatique ou manuel) déjà envoyé aujourd'hui pour cette équipe et ce
   type de réunion, **When** un membre adresse au bot la commande de rappel pour la même réunion,
   **Then** la commande est rejetée avec un message indiquant qu'un rappel a déjà été envoyé
   aujourd'hui pour cette réunion.

---

### Edge Cases

- Que se passe-t-il si un rappel automatique (poll clôturé "maintenue") est déclenché pour une
  réunion pour laquelle un rappel manuel a déjà été envoyé plus tôt le même jour ? Le rappel
  automatique est simplement omis (silencieux, pas d'erreur affichée puisqu'il n'y a pas de
  déclencheur humain à informer) — voir FR-008.
- Que se passe-t-il si le type de réunion fourni dans la commande de rappel manuel est invalide ou
  absent ? Le bot répond avec un message expliquant l'usage attendu, sans effet de bord (cohérent
  avec les commandes existantes de specs/002-poll-utilite-reunion).
- Que se passe-t-il si un poll est clôturé pour une équipe dont le channel a changé entre le
  déclenchement du poll et sa clôture ? Le rappel automatique est envoyé dans le channel associé
  au moment de la clôture (comportement cohérent avec la carte de résultat du poll lui-même).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système DOIT envoyer automatiquement un message de rappel dans le channel Teams
  de l'équipe concernée immédiatement après la clôture d'un poll d'utilité
  (specs/002-poll-utilite-reunion) dont le résultat est "réunion maintenue".
- **FR-002**: Le système NE DOIT PAS envoyer de rappel automatique lorsque le résultat de clôture
  du poll est "réunion pas nécessaire".
- **FR-003**: Le système DOIT permettre à n'importe quel membre de l'équipe présent dans le
  channel de déclencher manuellement l'envoi d'un rappel pour un type de réunion donné (mêlée ou
  rétrospective) via une commande textuelle adressée au bot (ex: mentionner le bot suivi d'un
  mot-clé et du type de réunion), indépendamment de l'existence d'un poll.
- **FR-004**: Le système DOIT rejeter une commande de rappel manuel adressée depuis un channel non
  associé à une équipe, avec un message d'erreur explicite invitant à réaliser l'association
  d'abord.
- **FR-005**: Le message de rappel DOIT indiquer le type de réunion concerné (mêlée ou
  rétrospective).
- **FR-006**: Le système DOIT envoyer le rappel dans le channel Teams associé à l'équipe
  concernée, en réutilisant l'association channel/équipe déjà établie par
  specs/002-poll-utilite-reunion.
- **FR-007**: Le système NE DOIT PAS créer d'événement de calendrier Teams ni gérer de liste de
  participants individuels — le rappel est un message texte visible par tout le channel, sans
  action de convocation individuelle.
- **FR-008**: Le système DOIT empêcher l'envoi d'un second rappel (automatique ou manuel) pour une
  même équipe, un même type de réunion et un même jour, dès qu'un premier rappel (automatique ou
  manuel) a déjà été envoyé ; une tentative manuelle de déclenchement dans ce cas DOIT être
  rejetée avec un message indiquant qu'un rappel a déjà été envoyé aujourd'hui pour cette réunion.

### Key Entities *(include if feature involves data)*

- **Rappel de réunion envoyé**: trace qu'un rappel a été envoyé pour une équipe, un type de
  réunion (mêlée/rétrospective) et un jour donné, quelle que soit son origine (automatique ou
  manuelle) — sert uniquement à appliquer la règle de non-doublon (FR-008), pas à afficher un
  historique.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un rappel automatique apparaît dans le channel Teams de l'équipe concernée en moins
  d'1 minute après la clôture d'un poll dont le résultat est "réunion maintenue".
- **SC-002**: Un rappel manuel apparaît dans le channel en moins d'1 minute après la commande
  correspondante.
- **SC-003**: Aucun rappel n'est envoyé pour un poll clôturé avec le résultat "réunion pas
  nécessaire", vérifié sur 100% des scénarios de test.
- **SC-004**: Une équipe déjà associée à un channel (via specs/002-poll-utilite-reunion) peut
  recevoir un rappel sans configuration supplémentaire.

## Assumptions

- Le channel Teams de destination est celui déjà associé à l'équipe via
  specs/002-poll-utilite-reunion (FR-001/FR-002 de cette feature-là) ; cette feature ne prévoit
  aucun mécanisme d'association distinct ou supplémentaire.
- Un seul rappel (automatique ou manuel) est envoyé par équipe, type de réunion et jour (FR-008) —
  la même granularité d'unicité que le poll d'utilité (specs/002-poll-utilite-reunion), même si le
  rappel peut exister sans poll associé (US2).
- Le message de rappel n'inclut pas d'horaire précis de réunion : aucune notion d'horaire n'est
  capturée ailleurs dans le système (l'heure de la mêlée/rétro reste connue de l'équipe par ses
  propres conventions, hors périmètre de ScrumMaster).
- N'importe quel membre de l'équipe présent dans le channel peut déclencher un rappel manuel,
  cohérent avec la règle déjà retenue pour les commandes du poll (specs/002-poll-utilite-reunion) —
  pas de contrôle de rôle applicatif supplémentaire.
- Cette feature ne couvre pas la création d'événements de calendrier Teams (Microsoft Graph) ni la
  gestion d'une liste nominative de participants — explicitement hors périmètre (voir Input),
  réservé à une éventuelle feature future si le besoin se confirme.
