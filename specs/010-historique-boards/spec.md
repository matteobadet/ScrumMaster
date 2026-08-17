# Feature Specification: Historique des boards par équipe

**Feature Branch**: `010-historique-boards`

**Created**: 2026-08-17

**Status**: Draft

**Input**: User description: "Historique des boards de rétrospective par équipe. Actuellement, un
board n'est accessible que via son URL directe (lien de partage ou lien de facilitateur) ; une
fois cette URL perdue, il n'existe aucun moyen de retrouver un board déjà créé, notamment les
boards clôturés dont on voudrait consulter les résultats plus tard (post-its, votes, réponses aux
mini-jeux). Cette fonctionnalité doit permettre, à partir de l'identité d'équipe déjà existante
(Area Path, voir specs/001-retro-board-base), de lister les boards de cette équipe (actifs et
clôturés), triés du plus récent au plus ancien, avec assez d'information pour identifier chaque
board (Iteration/Sprint, date de création, statut) et y accéder en un clic. Contexte projet :
ScrumMaster n'a aucune authentification utilisateur (le lien seul fait foi, voir
specs/001-retro-board-base) ; l'Area Path est la seule identité stable d'équipe déjà utilisée par
le reste de l'application (Equipe, configuration Azure DevOps specs/005-azure-devops-boards).
specs/001-retro-board-base indique explicitement que l'historique multi-sessions n'était pas
couvert par le MVP initial ; cette fonctionnalité comble ce manque."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Retrouver un board clôturé de son équipe (Priority: P1)

Un membre d'équipe qui a perdu le lien d'un board (ou qui n'a jamais eu que le lien de partage,
désormais inutile après clôture) saisit l'Area Path de son équipe et retrouve la liste de tous les
boards de cette équipe, triés du plus récent au plus ancien, pour rouvrir celui qui l'intéresse et
en consulter les résultats.

**Why this priority**: C'est la valeur centrale de la feature — sans elle, un board clôturé dont le
lien est perdu est définitivement inaccessible, ce qui est le problème exact rapporté.

**Independent Test**: Peut être testé en créant plusieurs boards pour une même équipe (certains
clôturés, certains actifs), puis en consultant la liste pour cette équipe et en vérifiant que tous
apparaissent, triés par date décroissante, avec assez d'information pour les distinguer et y
accéder.

**Acceptance Scenarios**:

1. **Given** une équipe ayant plusieurs boards créés à des dates différentes, **When** un membre
   consulte l'historique de cette équipe, **Then** il voit tous les boards de l'équipe (actifs et
   clôturés), triés du plus récent au plus ancien, chacun affichant son Iteration/Sprint, sa date
   de création et son statut.
2. **Given** une entrée de l'historique, **When** un membre clique dessus, **Then** il accède
   directement au board correspondant (en lecture seule si le board est clôturé, cohérent avec
   specs/001-retro-board-base).
3. **Given** un Area Path sans aucun board créé, **When** un membre consulte son historique,
   **Then** un état vide explicite est affiché plutôt qu'une erreur ou une page vide sans
   explication.

---

### User Story 2 - Accéder à l'historique sans connaître d'URL spécifique (Priority: P2)

Un membre d'équipe déjà sur le formulaire de création d'un board, ou déjà sur la page d'un board de
son équipe, accède à l'historique de son équipe en un clic, sans avoir à connaître ou deviner une
URL dédiée.

**Why this priority**: Rend la fonctionnalité découvrable au moment où elle est utile, mais
l'historique reste consultable (US1) même sans ce raccourci — un membre qui connaît déjà l'URL de
l'historique en tire toute la valeur sans cette story.

**Independent Test**: Peut être testé en renseignant un Area Path sur le formulaire de création de
board et en vérifiant qu'un accès à l'historique de cette équipe apparaît, et de même depuis la
page d'un board déjà ouvert.

**Acceptance Scenarios**:

1. **Given** le formulaire de création d'un board avec un Area Path renseigné, **When** un membre
   cherche à consulter les boards passés de son équipe, **Then** un accès à l'historique de cette
   équipe est proposé directement depuis ce formulaire.
2. **Given** la page d'un board déjà ouvert, **When** un membre cherche à consulter les autres
   boards de son équipe, **Then** un accès à l'historique de cette équipe est proposé directement
   depuis cette page.

---

### Edge Cases

- Que se passe-t-il si l'Area Path saisi ne correspond à aucune équipe connue ? Le même état vide
  explicite que pour une équipe sans board est affiché (aucune distinction technique n'est exposée
  entre "équipe inconnue" et "équipe sans board", cohérent avec l'absence d'authentification).
- Que se passe-t-il si un board est clôturé pendant que l'historique est affiché ? Son statut n'est
  pas nécessairement mis à jour en direct dans la liste déjà affichée (voir Assumptions) ; rouvrir
  ou rafraîchir l'historique reflète l'état à jour.
- Que se passe-t-il si une équipe a un très grand nombre de boards ? Hors périmètre de ce MVP (voir
  Assumptions) — tous les boards sont listés sans pagination.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système DOIT permettre de lister tous les boards (actifs et clôturés) associés à
  un Area Path donné.
- **FR-002**: Chaque entrée de la liste DOIT afficher au minimum l'Iteration/Sprint, la date de
  création, et le statut (Actif/Clôturé) du board correspondant.
- **FR-003**: La liste DOIT être triée du board le plus récent au plus ancien.
- **FR-004**: Le système DOIT permettre d'accéder au board correspondant en un clic depuis une
  entrée de la liste.
- **FR-005**: Un Area Path sans board associé (ou inconnu) DOIT afficher un état vide explicite
  plutôt qu'une erreur.
- **FR-006**: Le système DOIT proposer un accès à cet historique depuis le formulaire de création
  d'un board (une fois l'Area Path renseigné) et depuis la page d'un board déjà ouvert, sans
  nécessiter de connaître une URL spécifique.
- **FR-007**: L'accès à un board clôturé depuis l'historique DOIT respecter le comportement de
  lecture seule déjà établi (specs/001-retro-board-base) — aucune nouvelle capacité d'édition
  n'est introduite par cette feature.

### Key Entities *(include if feature involves data)*

- Aucune nouvelle entité : cette feature expose une liste filtrée des boards déjà modélisés
  (specs/001-retro-board-base), scopée par Area Path — pas de nouvelle donnée persistée.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un membre d'équipe retrouve un board clôturé de son équipe et accède à ses résultats
  en moins de 30 secondes, sans avoir conservé le lien d'origine.
- **SC-002**: Une équipe sans historique voit un état vide clair, sans confusion ni erreur
  apparente.
- **SC-003**: Une équipe ayant plusieurs boards les retrouve tous listés, dans le bon ordre
  chronologique, sans étape manuelle supplémentaire.

## Assumptions

- Aucune authentification n'est ajoutée : connaître l'Area Path suffit pour consulter l'historique
  des boards de cette équipe, cohérent avec le modèle de confiance déjà établi (le lien seul fait
  foi, specs/001-retro-board-base).
- Pas de pagination dans ce MVP — le volume de boards par équipe reste faible ; à revisiter si ça
  devient un problème réel.
- L'historique est strictement en lecture (consultation et navigation vers un board) — aucune
  suppression ni archivage manuel de boards n'est introduit par cette feature (Constitution
  Principe VI, périmètre minimal).
- La liste n'est pas mise à jour en temps réel pendant qu'elle est affichée (voir Edge Cases) —
  cohérent avec le caractère ponctuel de la consultation (comme la liste des équipes Azure DevOps
  configurées, specs/005-azure-devops-boards, qui suit le même principe).
- Cette feature comble explicitement le manque laissé ouvert par specs/001-retro-board-base
  ("l'historique multi-sessions... n'est pas couvert par ce MVP") ; elle n'introduit pas de
  comparaison entre rétros successives, qui reste hors périmètre.
