# Feature Specification: Point de sprint (stats Azure DevOps)

**Feature Branch**: `009-sprint-review-stats`

**Created**: 2026-08-17

**Status**: Draft

**Input**: User description: "Tableau de bord \"Point de sprint\" s'appuyant sur l'intégration
Azure DevOps Boards déjà existante (specs/005-azure-devops-boards). Pour une équipe ayant
configuré son accès Azure DevOps (PAT), le facilitateur doit pouvoir consulter, pendant ou en
préparation de la rétrospective, des statistiques intéressantes sur le sprint/l'itération du
board : par exemple la répartition des work items (Task/User Story) par état (à faire, en cours,
terminé), le nombre de Task/US complétées vs planifiées, et d'autres indicateurs utiles pour faire
un point d'équipe sur comment s'est déroulé le sprint. Contexte projet : specs/005-azure-devops-
boards a déjà mis en place la configuration Azure DevOps par équipe (organisation/projet/PAT
chiffré), la sélection guidée de l'Area Path et de l'Iteration à la création d'un board, l'import
de work items comme post-its, et l'export de post-its comme nouveaux work items — mais aucune
fonctionnalité de statistiques/tableau de bord n'existe encore. Cette nouvelle fonctionnalité doit
réutiliser la configuration Azure DevOps déjà en place (pas de nouvelle authentification), et
s'appuyer sur l'Area Path/Iteration déjà associés au board."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consulter la répartition des work items par état (Priority: P1)

Pour un board dont l'équipe a un accès Azure DevOps configuré, un participant (facilitateur ou
non) ouvre le panneau "Point de sprint" et voit combien de work items de l'Iteration du board sont
à faire, en cours, ou terminés, pour se faire une idée rapide de l'avancement du sprint sans ouvrir
Azure DevOps.

**Why this priority**: C'est la statistique la plus immédiatement utile pour "faire un point
d'équipe" — sans elle, la feature n'apporte aucune valeur au-delà de ce qu'offre déjà
specs/005-azure-devops-boards (import/export de post-its).

**Independent Test**: Peut être testé en ouvrant le point de sprint sur un board dont l'Iteration
contient des work items dans différents états, et en vérifiant que le nombre de work items par état
affiché correspond aux données réelles d'Azure DevOps.

**Acceptance Scenarios**:

1. **Given** un board dont l'équipe a un accès Azure DevOps configuré et dont l'Iteration contient
   des work items dans plusieurs états, **When** un participant ouvre le point de sprint, **Then**
   il voit le nombre de work items par état (à faire / en cours / terminé).
2. **Given** une Iteration sans aucun work item, **When** un participant ouvre le point de sprint,
   **Then** il voit un état vide explicite plutôt qu'une erreur ou un panneau vide sans explication.
3. **Given** une équipe sans accès Azure DevOps configuré pour son board, **When** un participant
   tente d'ouvrir le point de sprint, **Then** l'action est indisponible avec un message invitant à
   configurer l'accès (cohérent avec specs/005-azure-devops-boards Edge Cases).

---

### User Story 2 - Distinguer Task et User Story dans les statistiques (Priority: P2)

Un participant consultant le point de sprint distingue, pour chaque indicateur, ce qui concerne les
Tasks de ce qui concerne les User Stories, car ce sont deux niveaux de suivi différents pour
l'équipe (le travail détaillé vs. la valeur livrée).

**Why this priority**: Affine la valeur de US1 mais celle-ci reste utile même sans cette
distinction (un compte global par état est déjà exploitable) ; peut être livré séparément.

**Independent Test**: Peut être testé sur une Iteration contenant à la fois des Tasks et des User
Stories dans des états variés, en vérifiant que les compteurs par état sont bien scindés par type
de work item.

**Acceptance Scenarios**:

1. **Given** une Iteration contenant des Tasks et des User Stories, **When** un participant
   consulte le point de sprint, **Then** la répartition par état est présentée séparément pour les
   Tasks et pour les User Stories.
2. **Given** une Iteration ne contenant qu'un seul des deux types (ex: uniquement des Tasks),
   **When** un participant consulte le point de sprint, **Then** seul le type présent est affiché,
   sans section vide pour le type absent.

---

### User Story 3 - Voir le taux de complétion planifié vs réalisé (Priority: P3)

Un participant consultant le point de sprint voit, en un coup d'œil, quelle proportion des work
items planifiés pour ce sprint ont été effectivement terminés, pour évaluer si l'équipe a tenu ses
engagements de sprint.

**Why this priority**: Complète les deux stories précédentes avec un indicateur de synthèse, mais
suppose que la répartition détaillée (US1) existe déjà — un facilitateur peut déjà "faire un point"
avec seulement US1/US2.

**Independent Test**: Peut être testé en comparant, pour une Iteration donnée, le nombre total de
work items à celui des work items terminés, et en vérifiant que le taux affiché correspond au
calcul attendu.

**Acceptance Scenarios**:

1. **Given** une Iteration avec un mélange de work items terminés et non terminés, **When** un
   participant consulte le point de sprint, **Then** il voit le nombre de work items terminés
   rapporté au nombre total planifié pour cette Iteration.

---

### Edge Cases

- Que se passe-t-il si la récupération des work items depuis Azure DevOps échoue au moment de
  l'ouverture du point de sprint (organisation injoignable, PAT expiré) ? Le panneau affiche un
  message d'erreur explicite invitant à réessayer ou à reconfigurer l'accès (cohérent avec
  specs/005-azure-devops-boards), sans bloquer le reste du board.
- Que se passe-t-il si le board a été créé avec un Area Path/Iteration en texte libre (équipe non
  configurée à l'origine, ou repli de secours FR-007 de specs/005-azure-devops-boards) ? Le point
  de sprint reste indisponible pour ce board, avec le même message que pour une équipe non
  configurée.
- Que se passe-t-il si un work item n'a ni Task ni User Story comme type (autre type du modèle de
  processus Azure DevOps, ex: Bug, Feature) ? Il est compté dans une catégorie "Autres" distincte,
  plutôt qu'ignoré silencieusement ou mal classé.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système DOIT permettre à tout participant d'un board dont l'équipe a un accès
  Azure DevOps configuré d'ouvrir un panneau "Point de sprint", accessible à tout moment tant que
  le board existe, indépendamment de la séquence d'étapes ou du statut du board.
- **FR-002**: Le système DOIT afficher, dans ce panneau, le nombre de work items de l'Iteration du
  board réparti par état (à faire / en cours / terminé).
- **FR-003**: Le système DOIT distinguer, dans cette répartition, les work items de type Task de
  ceux de type User Story.
- **FR-004**: Le système DOIT regrouper dans une catégorie distincte les work items dont le type
  n'est ni Task ni User Story, plutôt que de les ignorer ou de les classer arbitrairement.
- **FR-005**: Le système DOIT afficher le nombre de work items terminés rapporté au nombre total de
  work items planifiés pour l'Iteration du board.
- **FR-006**: Le système NE DOIT PAS proposer le point de sprint pour un board dont l'équipe n'a
  pas d'accès Azure DevOps configuré, ou dont l'Area Path/Iteration a été saisi en texte libre sans
  correspondre à des données Azure DevOps réelles ; un message explicite invite alors à configurer
  l'accès.
- **FR-007**: Le système DOIT afficher un message d'erreur explicite, sans bloquer le reste du
  board, lorsque la récupération des données Azure DevOps échoue au moment de l'ouverture du point
  de sprint.
- **FR-008**: Le système DOIT afficher un état vide explicite lorsque l'Iteration du board ne
  contient aucun work item, plutôt qu'un panneau vide sans explication.
- **FR-009**: Le point de sprint DOIT être une consultation en lecture seule — il NE DOIT créer,
  modifier, ni clôturer aucun work item dans Azure DevOps (cohérent avec specs/005-azure-devops-
  boards FR-012).

### Key Entities *(include if feature involves data)*

- **Point de sprint**: vue calculée à la demande à partir des work items de l'Iteration associée au
  board (via la configuration Azure DevOps de l'équipe, specs/005-azure-devops-boards) — n'est pas
  une donnée persistée par ScrumMaster, uniquement une lecture agrégée d'Azure DevOps au moment de
  la consultation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un participant peut consulter la répartition par état des work items du sprint en
  moins de 5 secondes après avoir ouvert le point de sprint.
- **SC-002**: Le point de sprint reflète l'état réel des work items dans Azure DevOps au moment de
  la consultation (pas de donnée périmée provenant d'un cache obsolète).
- **SC-003**: Une équipe sans accès Azure DevOps configuré ne voit aucune dégradation de son usage
  actuel du board (aucun blocage, cohérent avec specs/005-azure-devops-boards SC-005).

## Assumptions

- Le point de sprint est un panneau consultable à tout moment sur la page du board (pas une
  nouvelle étape de la séquence), indépendamment du statut du board ou de l'étape active —
  cohérent avec son caractère informatif et non structurant (pas d'action à déclencher au bon
  moment de la rétro, contrairement aux mini-jeux ou aux colonnes).
- Le point de sprint est visible par tout participant du board, pas seulement le facilitateur —
  cohérent avec le principe déjà établi que seules les actions structurantes (import/export de
  work items, changement de thème) sont réservées au facilitateur ; la simple consultation reste
  ouverte à tous, comme les colonnes et post-its.
- Les états "à faire / en cours / terminé" sont une catégorisation générique regroupant les états
  réels du modèle de processus Azure DevOps de l'équipe (ex: New/Active/Closed pour Basic,
  New/Active/Resolved/Closed pour Agile) — le mapping exact par modèle de processus est un détail
  d'implémentation, pas une décision produit.
- Le point de sprint n'est pas mis à jour en temps réel automatiquement (pas de rafraîchissement
  automatique en tâche de fond) — un participant qui veut des données à jour rouvre/rafraîchit le
  panneau, cohérent avec le caractère ponctuel des interactions Azure DevOps déjà établi (import et
  export sont des actions manuelles, pas une synchronisation continue, specs/005-azure-devops-
  boards Assumptions).
- Aucun historique de sprints passés n'est conservé par cette feature — seule l'Iteration associée
  au board courant est couverte (garder le périmètre minimal, Constitution Principe VI).
- Le point de sprint réutilise la configuration Azure DevOps déjà stockée pour l'équipe (PAT,
  organisation, projet) — aucune nouvelle authentification ni nouveau PAT n'est introduit.
