# Feature Specification: Nouveaux mini-jeux — Pendu et Lien externe

**Feature Branch**: `011-pendu-lien-externe`

**Created**: 2026-08-18

**Status**: Draft

**Input**: User description: "Ajouter deux nouveaux mini-jeux au catalogue existant (specs/006-
systeme-extensions-etapes, déjà enrichi par 'Météo d'équipe' et 'ROTI' voir specs/008-roti-mini-
jeu) : (1) un mini-jeu 'Pendu' classique où l'équipe devine collectivement un mot ou une expression
choisi par le facilitateur, lettre par lettre ; (2) un mini-jeu 'Lien externe' où le facilitateur
indique simplement un lien vers un autre outil de jeu en ligne (type Gartic Phone, Skribbl.io) et
les participants sont redirigés vers cet outil pendant la rétrospective. Contexte projet :
ScrumMaster a déjà un système d'étapes de type Mini-jeu avec un catalogue de mini-jeux prédéfinis
et un mécanisme de réponse par participant (Météo d'équipe, ROTI) ; ces deux nouveaux mini-jeux
s'insèrent dans ce même système de séquence d'étapes, mais leur mécanique interne est différente
d'un simple choix parmi des options (Pendu est un jeu partagé à état évolutif ; Lien externe n'a
aucune réponse à collecter, juste une redirection)."

## Clarifications

### Session 2026-08-18

- Q: Quand le facilitateur indique-t-il l'URL du jeu externe (Gartic Phone, Skribbl.io, etc.) vers
  laquelle rediriger l'équipe ? → A: En direct, une fois l'étape "Lien externe" déjà active pendant
  la rétrospective — comme le changement de thème en direct déjà permis pour l'étape "Colonnes et
  post-its" (specs/001-retro-board-base). Le facilitateur ajoute l'étape à la séquence sans contenu
  requis à ce stade, puis saisit (et peut modifier) le nom du jeu et l'URL une fois l'étape active.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Jouer une partie de Pendu en équipe (Priority: P1)

Le facilitateur compose une étape "Pendu" en choisissant un mot ou une courte expression à faire
deviner. Une fois cette étape active, chaque participant peut proposer des lettres ; les lettres
correctes révèlent leur position dans le mot pour toute l'équipe, les lettres incorrectes réduisent
le nombre d'essais restants partagé par l'équipe, jusqu'à ce que le mot soit entièrement deviné
(victoire) ou que les essais soient épuisés (défaite).

**Why this priority**: C'est le cœur de la demande — un mini-jeu d'équipe classique et engageant,
utilisable dès sa livraison sans dépendre de l'autre mini-jeu de cette feature.

**Independent Test**: Peut être testé en composant une étape Pendu avec un mot connu, en proposant
des lettres depuis plusieurs comptes participants une fois l'étape active, et en vérifiant que les
lettres correctes/incorrectes sont reflétées pour tous, jusqu'à la victoire ou la défaite.

**Acceptance Scenarios**:

1. **Given** une étape Pendu active avec le mot "RETROSPECTIVE", **When** un participant consulte
   l'étape, **Then** il voit le mot masqué (une case par lettre, espaces visibles s'il y en a) sans
   qu'aucun participant ne voie le mot en clair.
2. **Given** une étape Pendu active, **When** un participant propose une lettre présente dans le
   mot, **Then** toutes les occurrences de cette lettre sont révélées, visibles immédiatement par
   tous les participants.
3. **Given** une étape Pendu active, **When** un participant propose une lettre absente du mot,
   **Then** le nombre d'essais restants diminue d'une unité, visible par tous les participants.
4. **Given** une lettre déjà proposée (qu'elle ait été correcte ou non), **When** un participant (le
   même ou un autre) la propose à nouveau, **Then** la proposition est ignorée sans conséquence
   (pas de nouvel essai consommé).
5. **Given** toutes les lettres du mot révélées, **When** la dernière lettre manquante est trouvée,
   **Then** la partie affiche une victoire pour toute l'équipe.
6. **Given** le nombre d'essais restants atteignant zéro avant que le mot soit complet, **When** le
   dernier essai est consommé, **Then** la partie affiche une défaite et révèle le mot complet à
   tous.

---

### User Story 2 - Rediriger l'équipe vers un jeu externe (Priority: P2)

Le facilitateur ajoute une étape "Lien externe" à la séquence du board. Une fois cette étape
active pendant la rétrospective, le facilitateur saisit le nom et l'URL d'un outil de jeu en ligne
externe (type Gartic Phone, Skribbl.io) ; chaque participant voit alors un lien clair vers cet
outil et peut le rejoindre en un clic, sans quitter son onglet ScrumMaster.

**Why this priority**: Apporte de la variété au catalogue de mini-jeux avec un effort minimal, mais
n'a pas de dépendance avec le Pendu — chaque mini-jeu apporte sa valeur indépendamment.

**Independent Test**: Peut être testé en ajoutant une étape Lien externe à une séquence, en
l'activant, en y saisissant un nom et une URL en tant que facilitateur, et en vérifiant que chaque
participant voit et peut utiliser le lien fourni.

**Acceptance Scenarios**:

1. **Given** une étape "Lien externe" active sans lien encore renseigné, **When** un participant
   consulte l'étape, **Then** il voit un état d'attente explicite indiquant que le facilitateur n'a
   pas encore renseigné de lien.
2. **Given** une étape "Lien externe" active, **When** le facilitateur y renseigne un nom de jeu et
   une URL valide, **Then** tous les participants voient immédiatement le nom du jeu et un lien
   cliquable vers l'URL fournie.
3. **Given** le lien affiché, **When** un participant clique dessus, **Then** l'outil externe
   s'ouvre dans un nouvel onglet, sans fermer ni recharger le board ScrumMaster du participant.
4. **Given** le facilitateur saisissant une URL qui n'est pas en HTTPS, **When** il tente de la
   valider, **Then** la saisie est refusée avec un message d'erreur explicite (cohérent avec la
   validation d'URL déjà en place, specs/007-themes-visuels-colonnes).
5. **Given** un lien déjà renseigné et affiché aux participants, **When** le facilitateur le
   modifie, **Then** tous les participants voient le nouveau lien.

---

### Edge Cases

- Que se passe-t-il si le mot du Pendu contient des lettres accentuées ou en majuscules/minuscules
  mixtes ? La comparaison ignore la casse et traite une lettre accentuée comme distincte de sa
  version non accentuée (proposer "e" ne révèle pas "é").
- Que se passe-t-il si deux participants proposent la même lettre non encore essayée au même
  moment ? Un seul essai est comptabilisé, cohérent avec le caractère partagé de la partie (Edge
  Case similaire à un double-vote déjà géré ailleurs dans l'application).
- Que se passe-t-il si le facilitateur avance à l'étape suivante avant la fin d'une partie de Pendu
  (ni victoire ni défaite) ? La partie s'arrête simplement avec l'étape, comme tout autre mini-jeu
  ou poll de la séquence (aucun état "en pause" ou "reprise" n'est introduit).
- Que se passe-t-il si le facilitateur avance à l'étape suivante sans jamais avoir renseigné de lien
  externe ? L'étape se termine simplement sans avoir servi, comme n'importe quelle étape que le
  facilitateur choisit de ne pas utiliser pleinement — aucune erreur ni blocage.
- Que se passe-t-il si un participant non-facilitateur tente de renseigner ou modifier le lien
  externe ? L'action est refusée, cohérente avec les autres actions de configuration en direct déjà
  réservées au facilitateur (changement de thème, specs/001-retro-board-base FR-013).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système DOIT permettre au facilitateur de composer une étape "Pendu" avec un mot
  ou une courte expression à deviner.
- **FR-002**: Le système DOIT afficher aux participants une version masquée du mot (lettres
  cachées, structure du mot visible) une fois l'étape Pendu active, sans jamais exposer le mot en
  clair avant qu'il soit deviné ou que la partie soit perdue.
- **FR-003**: Le système DOIT permettre à tout participant de proposer une lettre pendant une étape
  Pendu active.
- **FR-004**: Une lettre correcte DOIT révéler toutes ses occurrences dans le mot, visible par tous
  les participants.
- **FR-005**: Une lettre incorrecte DOIT réduire un nombre d'essais restants partagé par l'équipe,
  visible par tous les participants.
- **FR-006**: Une lettre déjà proposée (correcte ou non) DOIT être ignorée si proposée à nouveau,
  sans consommer d'essai supplémentaire.
- **FR-007**: Le système DOIT déclarer une victoire lorsque toutes les lettres du mot ont été
  trouvées, et une défaite (avec révélation du mot complet) lorsque les essais restants atteignent
  zéro avant que le mot soit complet.
- **FR-008**: Le système DOIT permettre au facilitateur d'ajouter une étape "Lien externe" à la
  séquence du board, sans nom de jeu ni URL requis à ce stade.
- **FR-009**: Le système DOIT permettre au facilitateur, une fois l'étape "Lien externe" active, de
  saisir ou modifier le nom du jeu et son URL ; tout changement DOIT être immédiatement visible par
  tous les participants.
- **FR-010**: Le système DOIT réserver la saisie et la modification du lien externe au
  facilitateur, cohérent avec les autres actions de configuration en direct déjà réservées à ce
  rôle (specs/001-retro-board-base FR-013).
- **FR-011**: Tant qu'aucun lien n'a été renseigné, le système DOIT afficher aux participants un
  état d'attente explicite plutôt qu'un espace vide sans explication.
- **FR-012**: Le système DOIT afficher, une fois un lien renseigné, le nom du jeu et un lien
  cliquable vers l'URL fournie, pour tout participant.
- **FR-013**: Le lien vers le jeu externe DOIT s'ouvrir dans un nouvel onglet, sans interrompre la
  session ScrumMaster du participant sur son board.
- **FR-014**: Le système DOIT rejeter une URL de jeu externe qui n'est pas en HTTPS, avec un
  message d'erreur explicite (cohérent avec specs/007-themes-visuels-colonnes FR-009).
- **FR-015**: L'étape "Lien externe" NE DOIT collecter ni afficher aucune réponse ou résultat en
  provenance de l'outil externe — ScrumMaster n'a aucune visibilité sur ce qui s'y passe.

### Key Entities *(include if feature involves data)*

- **Partie de Pendu**: le mot ou l'expression choisi par le facilitateur pour une étape, l'ensemble
  des lettres déjà proposées (correctes et incorrectes), et le nombre d'essais restants partagés
  par l'équipe pour cette étape.
- **Redirection externe**: le nom du jeu et l'URL renseignés par le facilitateur, une fois l'étape
  "Lien externe" active — absents tant que le facilitateur ne les a pas saisis.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Une équipe peut mener une partie de Pendu du début (mot masqué) jusqu'à son issue
  (victoire ou défaite) en moins de 5 minutes.
- **SC-002**: Une lettre proposée par un participant est reflétée pour toute l'équipe en moins de 2
  secondes.
- **SC-003**: Un participant accède au jeu externe indiqué par le facilitateur en un seul clic
  depuis le board.
- **SC-004**: Une équipe sans intention d'utiliser ces deux mini-jeux ne voit aucune dégradation de
  son usage actuel des mini-jeux existants (Météo d'équipe, ROTI).

## Assumptions

- La partie de Pendu est un jeu partagé unique par étape (un seul mot, une progression commune)
  plutôt qu'une partie individuelle par participant — cohérent avec la pratique habituelle du Pendu
  en groupe et avec l'esprit "icebreaker d'équipe" des mini-jeux déjà existants.
- Le nombre d'essais autorisés avant défaite suit la convention classique du jeu du Pendu (6
  essais) — un détail de contenu, pas une décision produit nécessitant clarification.
- Tout participant (facilitateur inclus) peut proposer des lettres, cohérent avec l'absence de
  restriction de rôle sur la réponse aux mini-jeux existants (Météo d'équipe, ROTI).
- ScrumMaster ne stocke et n'affiche aucun résultat, score ou contenu provenant de l'outil de jeu
  externe (FR-012) — l'intégration se limite à un lien de redirection, sans API ni scraping.
- Comme pour les autres mini-jeux, aucune capacité de "recommencer une partie" n'est introduite
  dans ce MVP — une nouvelle partie de Pendu nécessite une nouvelle étape dans la séquence.
- Le facilitateur peut modifier le lien externe autant de fois qu'il le souhaite tant que l'étape
  reste active, cohérent avec le changement de thème en direct déjà permis pour l'étape "Colonnes
  et post-its" (specs/001-retro-board-base).
