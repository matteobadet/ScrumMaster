# Feature Specification: Board de Rétrospective Interactif de Base

**Feature Branch**: `001-retro-board-base`

**Created**: 2026-08-16

**Status**: Draft

**Input**: User description: "Board de rétrospective interactif de base — colonnes, post-its, vote, thème modifiable, utilisable en temps réel par plusieurs participants, pour une seule équipe (MVP, priorité avant poll d'utilité, invitations Teams, intégration Azure DevOps Boards, système d'extensions)."

## Clarifications

### Session 2026-08-16

- Q: Le board MVP doit-il être un onglet Teams intégré (SSO) ou une appli web autonome par lien ? → A: Application web autonome accessible par lien, indépendamment de Teams pour cette itération.
- Q: Tous les participants ont-ils les mêmes droits, ou existe-t-il un rôle facilitateur avec des droits exclusifs ? → A: Rôle facilitateur exclusif pour changer le thème et clôturer le board.
- Q: Les post-its affichent-ils le nom de leur auteur, ou sont-ils anonymes ? → A: Auteur visible par tous les participants.
- Q: N'importe qui disposant du lien peut-il rejoindre le board, ou faut-il une restriction d'accès supplémentaire (mot de passe, liste blanche) ? → A: Le lien seul suffit, sans mot de passe ni liste blanche.
- Q: Que deviennent les post-its et votes quand le facilitateur clôture un board ? → A: Le board passe en lecture seule (consultable par tous, plus aucune modification possible).
- Q: Un post-it vide ou un thème sans colonne sont-ils autorisés ? → A: Interdits tous les deux ; le système impose au moins 1 caractère par post-it et au moins 1 colonne par thème.
- Q: Comment un board est-il rattaché à "une équipe" dans ce MVP ? → A: Une équipe correspond à un Area Path Azure DevOps (ex: "Krypton"), identité stable à travers plusieurs boards ; chaque board porte en plus une Iteration/Sprint (ex: "Sprint-138") qui identifie son cycle. Pour ce MVP, l'Area Path et l'Iteration sont saisis comme champs texte libres par le facilitateur à la création du board, sans appel à l'API Azure DevOps (l'intégration Azure Boards en direct est une fonctionnalité ultérieure distincte).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Créer un board de rétrospective et y noter des post-its (Priority: P1)

Un facilitateur (Scrum Master ou tout membre de l'équipe) crée un nouveau board de rétrospective
pour la réunion du jour, avec un thème de colonnes par défaut, puis ajoute des post-its dans les
colonnes pour capturer des retours.

**Why this priority**: Sans cette capacité de base, aucune rétrospective ne peut être menée ;
c'est le socle sur lequel s'appuient toutes les autres user stories. Un facilitateur doit déjà
pouvoir structurer une rétro utile, même seul.

**Independent Test**: Peut être testé en créant un board avec le thème par défaut, en ajoutant,
modifiant et supprimant plusieurs post-its dans différentes colonnes sans qu'aucun autre
participant ne soit connecté, et en constatant que le contenu est conservé.

**Acceptance Scenarios**:

1. **Given** aucun board actif pour l'équipe, **When** le facilitateur crée un nouveau board,
   **Then** un board vide avec les colonnes du thème par défaut est affiché.
2. **Given** un board existant avec des colonnes, **When** un utilisateur ajoute un post-it texte
   dans une colonne, **Then** le post-it apparaît immédiatement dans cette colonne avec son
   contenu.
3. **Given** un post-it existant, **When** son auteur modifie son texte ou le supprime, **Then**
   le changement est reflété sur le board.

---

### User Story 2 - Collaborer en temps réel avec plusieurs participants (Priority: P2)

Plusieurs membres de l'équipe rejoignent le même board pendant la réunion et voient les actions
des autres (ajout, modification, déplacement, suppression de post-its) apparaître en direct, sans
avoir à rafraîchir la page.

**Why this priority**: La valeur centrale d'un outil de rétro est la collaboration synchrone
pendant la réunion ; sans temps réel, l'outil se limite à un formulaire partagé et perd son
intérêt principal.

**Independent Test**: Peut être testé en ouvrant le même board dans deux sessions distinctes et en
vérifiant qu'un post-it ajouté dans l'une apparaît dans l'autre en quelques secondes, sans
rechargement manuel.

**Acceptance Scenarios**:

1. **Given** deux participants ont le même board ouvert, **When** l'un ajoute un post-it, **Then**
   l'autre le voit apparaître sans recharger la page.
2. **Given** deux participants ont le même board ouvert, **When** l'un déplace un post-it vers une
   autre colonne, **Then** l'autre voit le post-it dans sa nouvelle colonne.
3. **Given** un participant perd puis retrouve sa connexion réseau, **When** la connexion est
   rétablie, **Then** son affichage du board se resynchronise avec l'état courant sans perte du
   contenu déjà saisi par les autres.

---

### User Story 3 - Voter sur les post-its pour prioriser la discussion (Priority: P3)

Les participants attribuent un nombre limité de votes aux post-its qu'ils jugent les plus
importants, afin que l'équipe concentre le temps de la réunion sur les sujets les plus soutenus.

**Why this priority**: Le vote structure la discussion en temps limité de la rétro ; c'est une
valeur ajoutée significative mais qui suppose déjà des post-its et une session multi-participants
fonctionnels (P1/P2).

**Independent Test**: Peut être testé en votant pour plusieurs post-its depuis différents
participants et en vérifiant que le décompte de votes par post-it est correct et visible par tous.

**Acceptance Scenarios**:

1. **Given** un board avec des post-its, **When** un participant vote pour un post-it, **Then**
   le compteur de votes de ce post-it augmente et est visible par tous les participants.
2. **Given** un participant a atteint son nombre maximal de votes, **When** il tente de voter à
   nouveau, **Then** le système l'empêche et l'informe qu'il a atteint sa limite.
3. **Given** un participant a déjà voté pour un post-it, **When** il retire son vote, **Then** le
   compteur diminue et son vote redevient disponible.

---

### User Story 4 - Personnaliser le thème (format de colonnes) du board (Priority: P4)

Avant de démarrer la réunion, le facilitateur choisit ou adapte le thème de la rétrospective (ex:
Start/Stop/Continue, Mad/Sad/Glad, ou des colonnes personnalisées) afin que le format corresponde
aux besoins de l'équipe pour cette session.

**Why this priority**: Utile pour varier les formats de rétro dans le temps, mais l'équipe peut
déjà mener une première rétrospective complète avec le thème par défaut (P1-P3) ; la
personnalisation est une amélioration, pas un prérequis.

**Independent Test**: Peut être testé en créant un board avec un thème personnalisé (colonnes et
intitulés différents du défaut) et en vérifiant que le board affiche exactement ces colonnes.

**Acceptance Scenarios**:

1. **Given** la création d'un nouveau board, **When** le facilitateur choisit un thème parmi une
   liste de thèmes prédéfinis, **Then** le board affiche les colonnes correspondant à ce thème.
2. **Given** la création d'un nouveau board, **When** le facilitateur définit manuellement les
   intitulés et le nombre de colonnes, **Then** le board affiche exactement ces colonnes
   personnalisées.

---

### Edge Cases

- Que se passe-t-il si deux participants modifient ou déplacent le même post-it au même moment ?
  (voir Assumptions — résolution par dernière écriture gagnante)
- Un post-it vide ou un thème sans colonne sont rejetés par le système (FR-015).
- L'état du board est-il conservé si tous les participants le quittent puis reviennent ?
- Que se passe-t-il si un participant tente de voter pour son propre post-it ?
- Comment le système réagit-il si le nombre de participants simultanés dépasse la capacité prévue
  pour un seul board ?
- Que se passe-t-il si un participant tente d'ajouter, modifier ou voter sur un board déjà
  clôturé ? (rejeté — le board est en lecture seule, voir FR-016)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système DOIT permettre à un utilisateur de créer un nouveau board de
  rétrospective en saisissant un Area Path (identifiant d'équipe, ex: "Krypton") et une
  Iteration/Sprint (identifiant de cycle, ex: "Sprint-138") sous forme de champs texte libres.
- **FR-002**: Le système DOIT proposer au moins un thème de colonnes par défaut (ex:
  Start/Stop/Continue) appliqué automatiquement si aucun thème n'est choisi explicitement.
- **FR-003**: Le système DOIT permettre de choisir un thème de colonnes parmi une liste de thèmes
  prédéfinis, ou de définir un jeu de colonnes personnalisé (intitulés et nombre de colonnes)
  avant que la réunion ne commence.
- **FR-004**: Le système DOIT permettre à un utilisateur d'ajouter un post-it (texte libre) dans
  une colonne du board.
- **FR-005**: Le système DOIT permettre à l'auteur d'un post-it de modifier son texte ou de le
  supprimer.
- **FR-006**: Le système DOIT permettre de déplacer un post-it d'une colonne à une autre.
- **FR-007**: Le système DOIT propager à tous les participants connectés au même board, en
  quelques secondes et sans action manuelle de leur part, tout ajout, modification, déplacement ou
  suppression de post-it effectué par un autre participant.
- **FR-008**: Le système DOIT permettre à chaque participant de voter pour un post-it, dans la
  limite d'un nombre maximal de votes défini par participant et par board.
- **FR-009**: Le système DOIT permettre à un participant de retirer un vote qu'il a précédemment
  attribué.
- **FR-010**: Le système DOIT afficher, pour chaque post-it, le nombre total de votes reçus,
  visible par tous les participants.
- **FR-011**: Le système DOIT conserver le contenu du board (colonnes, post-its, votes) après la
  fin de la réunion, pour permettre une consultation ultérieure.
- **FR-012**: Le système DOIT rendre le board accessible via une application web autonome par
  lien, indépendamment de Microsoft Teams pour cette itération ; les participants s'identifient en
  saisissant un nom affiché en rejoignant le board (voir Assumptions). L'intégration Teams (Tab
  épinglé, SSO) fera l'objet d'une fonctionnalité ultérieure distincte. Le lien du board est le
  seul mécanisme d'accès requis : aucun mot de passe ni liste blanche supplémentaire dans ce MVP.
- **FR-013**: Le système DOIT réserver à un rôle "facilitateur" (attribué à la personne qui crée
  le board) les actions suivantes : changer le thème du board et clôturer le board ; tout autre
  participant peut ajouter, modifier ou supprimer ses propres post-its et voter.
- **FR-014**: Le système DOIT afficher les post-its avec le nom de leur auteur visible par tous
  les participants (pas d'anonymat dans ce MVP).
- **FR-015**: Le système DOIT refuser la création d'un post-it dont le texte est vide, et refuser
  la création ou l'application d'un thème comportant zéro colonne.
- **FR-016**: Lorsque le facilitateur clôture un board, le système DOIT le passer en lecture
  seule : post-its et votes restent consultables par tous les participants, mais plus aucun ajout,
  modification, déplacement, suppression de post-it ni vote n'est possible.
- **FR-017**: Le système DOIT associer chaque board à un Area Path (identité d'équipe stable à
  travers plusieurs boards) et à une Iteration/Sprint (cycle propre à ce board), tous deux saisis
  en texte libre par le facilitateur à la création, sans appel à l'API Azure DevOps dans ce MVP.

### Key Entities *(include if feature involves data)*

- **Équipe**: identifiée par un Area Path Azure DevOps (ex: "Krypton") ; identité stable à travers
  plusieurs boards créés dans le temps par cette équipe.
- **Board de rétrospective**: représente une session de rétrospective pour une Iteration/Sprint
  donné (ex: "Sprint-138") d'une Équipe ; possède un thème (jeu de colonnes), un statut
  (actif/clôturé), une date de création.
- **Colonne**: fait partie du thème d'un board ; possède un intitulé et un ordre d'affichage.
- **Post-it**: contenu textuel libre (non vide) créé par un participant, rattaché à une colonne
  d'un board, possède un auteur et un décompte de votes reçus.
- **Vote**: association entre un participant et un post-it, bornée par un nombre maximal de votes
  par participant et par board.
- **Participant**: personne accédant au board pendant la session, avec un nom affiché et un rôle
  (facilitateur ou participant) ; rattaché à l'Équipe du board.
- **Thème**: modèle de colonnes réutilisable (prédéfini par le système ou personnalisé par un
  facilitateur), comportant au moins 1 colonne, appliqué à un board.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un facilitateur peut créer un board et le rendre accessible aux participants en
  moins de 1 minute.
- **SC-002**: Un post-it ajouté par un participant est visible par les autres participants
  connectés au même board en moins de 3 secondes.
- **SC-003**: Le board reste utilisable (ajout de post-its, vote) sans interruption perceptible
  pour au moins 10 participants connectés simultanément au même board.
- **SC-004**: 90% des post-its créés pendant une session de test sont toujours présents et
  correctement attribués à leur colonne à la fin de la session (pas de perte de contenu liée à la
  synchronisation temps réel).
- **SC-005**: Une équipe peut mener une rétrospective complète (création du board, ajout de
  post-its par tous les participants, vote, consultation des résultats) sans assistance technique
  externe.

## Assumptions

- Un "board" correspond à une seule session de rétrospective ; l'historique multi-sessions et la
  comparaison entre rétros successives ne sont pas couverts par ce MVP.
- Une équipe peut avoir plusieurs boards dans le temps (un par réunion) ; un seul board actif à la
  fois n'est pas une contrainte imposée par ce MVP.
- Le multi-tenant réel (plusieurs Équipes/Area Paths distincts utilisant l'outil simultanément,
  avec isolation stricte des données) n'est pas testé dans ce MVP, mais le modèle de données porte
  déjà l'Area Path comme identifiant d'équipe explicite sur chaque board, conformément au principe
  multi-tenant de la constitution du projet.
- L'Area Path et l'Iteration/Sprint sont des champs obligatoires à la création d'un board (aucune
  valeur par défaut ni board "sans équipe").
- Un participant rejoint un board en saisissant un nom affiché, sans création de compte ; ce nom
  sert d'identité pour l'attribution des post-its et des votes pour la durée de la session.
- Le nombre maximal de votes par participant et par board est configurable par le facilitateur,
  avec une valeur par défaut de 3 votes si non configurée.
- Un participant peut voter pour son propre post-it (aucune restriction n'a été demandée sur ce
  point).
- Les conflits d'édition simultanée sur un même post-it sont résolus par "dernière écriture
  gagnante" (la dernière modification reçue par le serveur prévaut), sans mécanisme de verrouillage
  collaboratif dans ce MVP.
