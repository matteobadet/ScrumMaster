# Feature Specification: Poll d'Utilité de Réunion

**Feature Branch**: `002-poll-utilite-reunion`

**Created**: 2026-08-16

**Status**: Draft

**Input**: User description: "Poll d'utilité de réunion envoyé par le bot Teams dans le channel de l'équipe, pour sonder si la mêlée ou la rétrospective du jour est jugée utile et doit avoir lieu. Cette feature couvre uniquement l'envoi du poll, la collecte des votes et la détermination/affichage du résultat (la réunion doit avoir lieu ou non) — elle ne couvre PAS l'envoi d'invitations Teams pour la réunion, qui fait l'objet d'une feature séparée ultérieure. S'appuie sur le Bot Framework SDK. Contexte : ScrumMaster est un outil multi-équipes (chaque équipe identifiée par un Area Path Azure DevOps) ; ce poll doit être envoyé dans le channel Teams propre à l'équipe concernée."

## Clarifications

### Session 2026-08-16

- Q: Le poll est-il envoyé automatiquement selon un horaire configuré, ou déclenché manuellement
  par un membre de l'équipe ? → A: Déclenchement manuel, via une commande adressée au bot dans le
  channel.
- Q: Quelles options de réponse sont proposées aux membres ? → A: Oui/Non simple ("Utile" / "Pas
  nécessaire").
- Q: Quelle règle détermine, à partir des votes reçus, si la réunion doit avoir lieu ? → A: La
  réunion est maintenue dès qu'au moins un vote "Utile" a été exprimé ; elle n'est jugée non
  nécessaire que si tous les votes exprimés sont "Pas nécessaire" (aucun vote "Utile").
- Q: Comment un poll se clôture-t-il, pour passer de "ouvert" à "clos" et faire apparaître le
  résultat ? → A: Clôture manuelle par un membre de l'équipe, via une commande adressée au bot
  (symétrique à l'ouverture).
- Q: Concrètement, comment un membre "adresse une commande au bot" pour déclencher le poll,
  clôturer le poll, ou associer le channel (P1) ? → A: Un message texte reconnu par le bot dans le
  channel (ex: mentionner le bot suivi d'un mot-clé).
- Q: Qui peut déclencher l'envoi d'un poll pour l'équipe (FR-003) ? → A: N'importe quel membre de
  l'équipe présent dans le channel.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Associer le channel Teams de l'équipe (Priority: P1)

Le facilitateur associe le channel Teams de son équipe à l'Area Path de cette équipe, afin que les
polls d'utilité de réunion soient envoyés au bon endroit.

**Why this priority**: Sans cette association, le système ne sait pas où envoyer les polls ; c'est
un prérequis incontournable à toute autre partie de la feature.

**Independent Test**: Peut être testé en réalisant l'association pour une équipe, puis en vérifiant
qu'un poll déclenché pour cette équipe arrive bien dans le channel associé (et dans aucun autre).

**Acceptance Scenarios**:

1. **Given** une équipe (Area Path) sans channel Teams associé, **When** le facilitateur adresse au
   bot, dans le channel visé, la commande textuelle d'association avec l'Area Path de l'équipe,
   **Then** ce channel devient la destination des polls de cette équipe.
2. **Given** une équipe déjà associée à un channel, **When** le facilitateur adresse la commande
   d'association dans un autre channel, **Then** les polls suivants sont envoyés au nouveau
   channel.

---

### User Story 2 - Recevoir le poll et voter (Priority: P2)

Un membre de l'équipe reçoit dans le channel Teams un poll demandant si la mêlée ou la
rétrospective du jour est jugée utile, et y répond.

**Why this priority**: C'est le cœur de la valeur de la feature — sonder l'équipe avant la
réunion — mais suppose que l'association équipe/channel (P1) existe déjà.

**Independent Test**: Peut être testé en déclenchant un poll pour une équipe déjà associée à un
channel, puis en votant depuis plusieurs comptes Teams membres de ce channel.

**Acceptance Scenarios**:

1. **Given** une équipe associée à un channel Teams, **When** un membre adresse au bot la commande
   de déclenchement pour cette équipe, **Then** un message de poll apparaît dans le channel
   indiquant le type de réunion concerné (mêlée ou rétrospective).
2. **Given** un poll ouvert dans le channel, **When** un membre y répond, **Then** son vote est
   enregistré et visible comme tel par ce membre.
3. **Given** un membre a déjà voté sur un poll ouvert, **When** il change sa réponse, **Then** son
   vote précédent est remplacé par le nouveau.

---

### User Story 3 - Consulter le résultat du poll (Priority: P3)

Une fois le poll clos, l'équipe voit dans le channel si la réunion est jugée utile et doit avoir
lieu, ou non.

**Why this priority**: Complète la boucle de valeur (sonder puis décider), mais suppose déjà un
poll fonctionnel avec des votes (P2).

**Independent Test**: Peut être testé en clôturant un poll ayant reçu des votes (via la commande de
clôture) et en vérifiant qu'un résultat cohérent avec les votes s'affiche dans le channel.

**Acceptance Scenarios**:

1. **Given** un poll ouvert ayant reçu des votes, **When** un membre de l'équipe adresse au bot la
   commande de clôture, **Then** le poll passe au statut clos et n'accepte plus de vote.
2. **Given** un poll clos ayant reçu des votes, **When** le résultat est calculé, **Then** un
   message visible par tout le channel indique si la réunion doit avoir lieu ou non.
3. **Given** un poll clos sans aucun vote reçu, **When** le résultat est calculé, **Then** le
   système retient la réunion comme devant avoir lieu par défaut (voir Assumptions).

---

### Edge Cases

- Que se passe-t-il si un déclenchement de poll est tenté pour une équipe sans channel Teams
  associé (P1 non réalisé) ?
- Que se passe-t-il si un membre tente de voter après la clôture du poll ?
- Comment le système distingue-t-il deux polls déclenchés le même jour pour la même équipe (ex:
  mêlée le matin, rétrospective l'après-midi) ?
- Que se passe-t-il si un membre quitte le channel Teams après avoir voté ?
- Que se passe-t-il si personne ne clôture jamais un poll ouvert ? (voir Assumptions — pas de
  clôture automatique dans ce MVP, le poll reste ouvert indéfiniment)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système DOIT permettre à un facilitateur d'associer un channel Teams à l'Area
  Path de son équipe via une commande textuelle adressée au bot dans ce channel (ex: mentionner
  le bot suivi d'un mot-clé et de l'Area Path).
- **FR-002**: Le système DOIT permettre de changer le channel Teams associé à une équipe, par la
  même commande adressée dans le nouveau channel.
- **FR-003**: Le système DOIT permettre à n'importe quel membre de l'équipe présent dans le
  channel de déclencher manuellement l'envoi du poll via une commande textuelle adressée au bot
  (pas de déclenchement automatique planifié dans cette feature).
- **FR-004**: Le système DOIT permettre à n'importe quel membre de l'équipe de clôturer
  manuellement un poll ouvert via une commande textuelle adressée au bot (pas de clôture
  automatique dans cette feature).
- **FR-005**: Le système DOIT envoyer le poll dans le channel Teams associé à l'équipe concernée,
  en indiquant le type de réunion visé (mêlée ou rétrospective).
- **FR-006**: Le système DOIT proposer exactement deux options de réponse : "Utile" et "Pas
  nécessaire".
- **FR-007**: Le système DOIT permettre à un membre de modifier son vote tant que le poll est
  ouvert.
- **FR-008**: Le système DOIT empêcher tout vote une fois le poll clos.
- **FR-009**: Le système DOIT retenir la réunion comme devant avoir lieu dès qu'au moins un vote
  "Utile" a été exprimé parmi les votes reçus ; le résultat n'est "réunion non nécessaire" que si
  tous les votes exprimés sont "Pas nécessaire" (et qu'au moins un vote a été exprimé, voir
  Assumptions pour le cas sans aucun vote).
- **FR-010**: Le système DOIT afficher le résultat du poll (réunion maintenue ou non) de façon
  visible par tous les membres du channel une fois le poll clos.
- **FR-011**: Le système DOIT associer chaque poll à une occurrence de réunion précise (équipe,
  type de réunion, date) pour ne pas mélanger les votes de réunions différentes.
- **FR-012**: Le système DOIT afficher, pour chaque vote, le nom du membre l'ayant exprimé (pas
  d'anonymat, cohérent avec le board de rétrospective — voir specs/001-retro-board-base).

### Key Entities *(include if feature involves data)*

- **Configuration Teams de l'équipe**: associe une Équipe (Area Path, voir
  specs/001-retro-board-base) à un channel Teams destinataire des polls.
- **Poll d'utilité**: représente le sondage pour une occurrence de réunion donnée ; possède un
  type de réunion (mêlée/rétrospective), une équipe, une date, un statut (ouvert/clos), et un
  résultat une fois clos.
- **Vote d'utilité**: réponse d'un membre à un poll, associée à son identité Teams et modifiable
  tant que le poll est ouvert.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un poll apparaît dans le channel Teams de l'équipe concernée en moins d'1 minute
  après son déclenchement.
- **SC-002**: Un membre peut voter en moins de 10 secondes depuis la réception du poll dans Teams.
- **SC-003**: Le résultat du poll est visible par tous les membres du channel sans action
  supplémentaire de leur part une fois le poll clos.
- **SC-004**: Une équipe peut configurer l'association à son channel Teams et recevoir son premier
  poll sans assistance technique externe.

## Assumptions

- Le facilitateur qui réalise l'association équipe/channel (P1) est le même rôle "Facilitateur"
  déjà établi dans specs/001-retro-board-base.
- Si aucun vote n'est reçu avant la clôture d'un poll, la réunion est retenue comme devant avoir
  lieu par défaut (choix conservateur : en l'absence de signal contraire, la réunion n'est pas
  annulée).
- Un seul poll actif à la fois par équipe et par occurrence de réunion (type + date) ; deux polls
  pour deux types de réunion différents le même jour (mêlée et rétrospective) sont deux occurrences
  distinctes.
- Un poll reste ouvert indéfiniment tant qu'aucun membre ne le clôture manuellement ; il n'y a pas
  de clôture automatique par durée ou par horaire dans ce MVP.
- L'historique des polls passés (consultation, statistiques) n'est pas couvert par cette feature ;
  seul le poll courant et son résultat immédiat le sont.
- Cette feature ne déclenche aucune action automatique sur le résultat (ex: annulation
  d'invitation, notification supplémentaire) au-delà de l'affichage du résultat dans le channel —
  ces actions relèvent de la feature ultérieure sur les invitations.
