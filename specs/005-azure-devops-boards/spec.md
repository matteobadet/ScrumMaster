# Feature Specification: Intégration Azure DevOps Boards

**Feature Branch**: `005-azure-devops-boards`

**Created**: 2026-08-16

**Status**: Draft

**Input**: User description: "Intégration Azure DevOps Boards en lecture/écriture pour
ScrumMaster (Phase 3 de la roadmap MVP de la constitution), avec un périmètre complet couvrant
trois capacités : (1) à la création d'un board de rétrospective (specs/001-retro-board-base),
l'Area Path et l'Iteration saisis par le facilitateur sont validés et autocomplétés depuis Azure
DevOps au lieu d'être du texte libre non vérifié ; (2) les work items du sprint courant (backlog,
bugs) peuvent être importés comme post-its pré-remplis sur le board au démarrage de la
rétrospective ; (3) les post-its créés pendant la rétrospective (ex: actions à mener) peuvent être
exportés vers Azure DevOps comme nouveaux work items. L'authentification à l'API Azure DevOps se
fait via un Personal Access Token (PAT) configuré une fois par équipe (Area Path), et non par
utilisateur individuel — le PAT doit être stocké chiffré at-rest et ne jamais apparaître en clair
dans les logs, messages d'erreur ou réponses API (contrainte déjà actée dans la constitution du
projet). Contexte : ScrumMaster est un outil multi-équipes, chaque équipe déjà identifiée par un
Area Path Azure DevOps stable (voir specs/001-retro-board-base) ; cette feature remplace la saisie
libre d'Area Path/Iteration par une intégration réelle à l'API Azure DevOps, sans quoi les board de
rétrospective actuels ne valident jamais ces champs contre le vrai système."

## Clarifications

### Session 2026-08-16

- Q: Qui doit pouvoir configurer ou remplacer le PAT Azure DevOps d'une équipe ? → A: Aucun
  contrôle de rôle supplémentaire — même modèle de confiance que le reste de l'application (ex:
  "associer channel" de specs/002-poll-utilite-reunion, sans authentification).
- Q: Comment l'Area Path et l'Iteration sont-ils proposés au facilitateur à la création du board,
  quand l'équipe est configurée ? → A: Liste déroulante / sélection guidée parmi les valeurs
  réelles d'Azure DevOps (le facilitateur choisit, ne tape plus) ; l'Iteration correspondant au
  sprint actuellement en cours est présélectionnée/suggérée par défaut dans la liste.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Configurer l'accès Azure DevOps de l'équipe (Priority: P1)

Un membre de l'équipe configure, une fois pour son équipe, l'accès à l'organisation et au projet
Azure DevOps correspondants (via un Personal Access Token), afin que les autres capacités de la
feature puissent fonctionner. Cohérent avec le reste de l'application (aucune authentification,
voir specs/001-retro-board-base Assumptions), aucun contrôle de rôle supplémentaire ne restreint
qui peut réaliser cette configuration.

**Why this priority**: Prérequis incontournable — sans accès configuré, aucune validation, import
ou export n'est possible. C'est le pendant, pour cette feature, de l'association channel/équipe de
specs/002-poll-utilite-reunion.

**Independent Test**: Peut être testé en configurant l'accès pour une équipe avec un PAT valide,
puis en vérifiant que la configuration est enregistrée (sans jamais afficher le PAT en clair), et
en testant le rejet d'un PAT invalide.

**Acceptance Scenarios**:

1. **Given** une équipe sans configuration Azure DevOps, **When** un membre de l'équipe saisit
   l'organisation, le projet et un PAT valide, **Then** la configuration est enregistrée et
   confirmée, sans jamais réafficher le PAT en clair par la suite.
2. **Given** un PAT invalide ou insuffisamment permissif, **When** un membre de l'équipe tente de
   l'enregistrer, **Then** la configuration est rejetée avec un message d'erreur explicite, sans
   exposer le PAT dans ce message.
3. **Given** une équipe déjà configurée, **When** un membre de l'équipe enregistre un nouveau PAT,
   **Then** l'ancien PAT est remplacé et n'est plus utilisé pour les appels suivants.

---

### User Story 2 - Choisir l'Area Path et l'Iteration parmi les données réelles à la création d'un board (Priority: P2)

Lorsqu'une équipe a configuré son accès Azure DevOps, le facilitateur qui crée un board choisit
l'Area Path et l'Iteration dans une liste guidée par les données réelles d'Azure DevOps, au lieu
de les saisir en texte libre sans contrôle ; l'Iteration correspondant au sprint actuellement en
cours est présélectionnée par défaut pour lui éviter de la chercher.

**Why this priority**: Cœur de la valeur de la feature — remplace un champ actuellement non
vérifié (specs/001-retro-board-base, FR-017) par une sélection guidée et fiable, sans laquelle
l'import et l'export (P3, P4) ne seraient pas rattachables avec confiance à la bonne itération.

**Independent Test**: Peut être testé en créant un board pour une équipe déjà configurée, en
vérifiant que l'Area Path et l'Iteration proposés au choix correspondent aux données réelles
d'Azure DevOps, que le sprint en cours est présélectionné, et que la création reste possible même
si la récupération de ces données échoue.

**Acceptance Scenarios**:

1. **Given** une équipe avec un accès Azure DevOps configuré, **When** le facilitateur crée un
   board, **Then** il choisit l'Area Path et l'Iteration parmi des listes reflétant les données
   réelles de l'équipe dans Azure DevOps, plutôt que de les saisir en texte libre.
2. **Given** une équipe avec un accès Azure DevOps configuré et un sprint actuellement en cours,
   **When** le facilitateur ouvre le formulaire de création du board, **Then** l'Iteration
   correspondant au sprint en cours est présélectionnée par défaut dans la liste, sans action
   supplémentaire de sa part.
3. **Given** une équipe sans accès Azure DevOps configuré, **When** le facilitateur crée un board,
   **Then** l'Area Path et l'Iteration restent saisissables en texte libre, comme aujourd'hui
   (aucune régression, aucun blocage).
4. **Given** une équipe configurée dont l'accès Azure DevOps échoue au moment de la création
   (organisation injoignable, permissions insuffisantes), **When** le facilitateur crée un board,
   **Then** la création reste possible via une saisie en texte libre de repli, accompagnée d'un
   avertissement indiquant que les données réelles n'ont pas pu être récupérées.

---

### User Story 3 - Importer les work items du sprint comme post-its (Priority: P3)

Au démarrage d'une rétrospective, le facilitateur importe les work items assignés à l'Iteration du
board comme post-its pré-remplis, pour éviter de ressaisir manuellement le contenu déjà connu
d'Azure DevOps.

**Why this priority**: Apporte une valeur additionnelle significative mais suppose que la
validation (P2) et l'accès configuré (P1) existent déjà ; le board reste pleinement utilisable
sans cette capacité.

**Independent Test**: Peut être testé en créant un board pour une itération contenant des work
items, en déclenchant l'import, et en vérifiant qu'un post-it est créé par work item importé, avec
son titre comme contenu.

**Acceptance Scenarios**:

1. **Given** un board dont l'Iteration correspond à des work items existants dans Azure DevOps,
   **When** le facilitateur déclenche l'import, **Then** un post-it est créé pour chaque work item
   assigné à cette itération, avec le titre du work item comme contenu.
2. **Given** une itération sans aucun work item assigné, **When** le facilitateur déclenche
   l'import, **Then** aucun post-it n'est créé et aucune erreur n'est affichée.

---

### User Story 4 - Exporter un post-it comme nouveau work item (Priority: P4)

Après la rétrospective, le facilitateur exporte un post-it (typiquement une action à mener) vers
Azure DevOps sous forme de nouveau work item, pour que le suivi se poursuive dans l'outil habituel
de l'équipe.

**Why this priority**: Referme la boucle de valeur de la feature (import puis export), mais
suppose que les capacités précédentes (P1-P3) existent déjà ; une équipe peut tirer de la valeur
de ScrumMaster sans jamais exporter.

**Independent Test**: Peut être testé en exportant un post-it depuis un board configuré, et en
vérifiant qu'un nouveau work item apparaît dans Azure DevOps avec le texte du post-it comme titre,
et qu'un second export du même post-it est empêché.

**Acceptance Scenarios**:

1. **Given** un post-it sur un board dont l'équipe a un accès Azure DevOps configuré, **When** le
   facilitateur exporte ce post-it, **Then** un nouveau work item est créé dans Azure DevOps avec
   le texte du post-it comme titre, et le post-it est visuellement marqué comme exporté.
2. **Given** un post-it déjà exporté, **When** le facilitateur tente de l'exporter à nouveau,
   **Then** l'action est empêchée (pas de second work item créé pour le même post-it).

---

### Edge Cases

- Que se passe-t-il si le PAT enregistré expire ou est révoqué après la configuration initiale
  (User Story 1) ? Toute tentative d'appel à Azure DevOps échoue avec un message d'erreur clair,
  invitant à reconfigurer l'accès ; les capacités qui ne nécessitent pas Azure DevOps (board,
  post-its, vote — specs/001-retro-board-base) continuent de fonctionner normalement.
- Que se passe-t-il si un facilitateur tente d'importer ou d'exporter sans accès Azure DevOps
  configuré pour son équipe ? L'action est indisponible/désactivée, avec un message invitant à
  configurer l'accès d'abord (User Story 1).
- Que se passe-t-il si l'équipe est configurée mais qu'aucun sprint n'est actuellement en cours
  (aucune Iteration active à la date du jour) ? La liste des Iterations reste proposée au choix,
  mais aucune présélection par défaut n'est faite (FR-005a ne s'applique que s'il existe un sprint
  en cours).
- Que se passe-t-il si l'équipe est configurée mais que la récupération échoue uniquement pour les
  Iterations (Area Path récupéré avec succès) ? Le repli en texte libre (FR-007) s'applique
  indépendamment pour chaque liste qui n'a pas pu être récupérée.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système DOIT permettre à un membre de l'équipe de configurer, pour cette équipe,
  l'organisation Azure DevOps, le projet, et un Personal Access Token (PAT) ; aucun contrôle de
  rôle supplémentaire ne restreint qui peut réaliser cette configuration (cohérent avec l'absence
  d'authentification du reste de l'application, voir specs/001-retro-board-base Assumptions et
  specs/002-poll-utilite-reunion).
- **FR-002**: Le PAT DOIT être stocké chiffré at-rest et ne DOIT jamais apparaître en clair dans
  les logs applicatifs, messages d'erreur, ou réponses API (contrainte de la constitution du
  projet).
- **FR-003**: Le système DOIT valider un PAT au moment de son enregistrement (appel réel à Azure
  DevOps) et rejeter l'enregistrement s'il est invalide ou insuffisamment permissif.
- **FR-004**: Le système DOIT permettre de remplacer le PAT d'une équipe déjà configurée ; l'ancien
  PAT ne DOIT plus être utilisé après remplacement.
- **FR-005**: Lorsqu'un accès Azure DevOps est configuré pour l'équipe, le système DOIT proposer,
  à la création d'un board, une sélection guidée de l'Area Path et de l'Iteration parmi les
  données réelles d'Azure DevOps, plutôt qu'une saisie en texte libre.
- **FR-005a**: Le système DOIT présélectionner par défaut, dans la liste des Iterations proposées,
  celle correspondant au sprint actuellement en cours pour l'équipe, lorsqu'il en existe un.
- **FR-006**: Le système DOIT permettre la création d'un board avec un Area Path/Iteration en
  texte libre lorsque l'équipe n'a pas d'accès Azure DevOps configuré (aucune régression par
  rapport à specs/001-retro-board-base FR-017).
- **FR-007**: Le système NE DOIT PAS bloquer la création d'un board lorsque la récupération des
  données réelles depuis Azure DevOps échoue (organisation injoignable, permissions insuffisantes)
  ; le système DOIT alors proposer une saisie en texte libre de repli, accompagnée d'un
  avertissement, plutôt que de bloquer la création du board.
- **FR-008**: Le système DOIT permettre au facilitateur d'importer, en une action, les work items
  assignés à l'Iteration du board comme post-its, un post-it par work item, avec le titre du work
  item comme contenu du post-it.
- **FR-009**: Le système DOIT permettre au facilitateur d'exporter un post-it comme nouveau work
  item dans Azure DevOps, avec le texte du post-it comme titre du work item créé.
- **FR-010**: Le système DOIT empêcher l'export en double d'un même post-it déjà exporté.
- **FR-011**: Les capacités d'import et d'export DOIVENT être réservées au facilitateur du board,
  cohérent avec les autres actions structurantes déjà réservées à ce rôle
  (specs/001-retro-board-base FR-013).
- **FR-012**: Le système NE DOIT PAS mettre à jour ou clôturer des work items existants dans Azure
  DevOps — seule la création de nouveaux work items (export) est couverte par cette feature.

### Key Entities *(include if feature involves data)*

- **Configuration Azure DevOps de l'équipe**: associe une Équipe (Area Path, voir
  specs/001-retro-board-base) à une organisation et un projet Azure DevOps, et à un PAT chiffré ;
  une seule configuration active par équipe.
- **Post-it** (extension de specs/001-retro-board-base) : peut porter une référence au work item
  Azure DevOps dont il est issu (import) et/ou au work item créé lorsqu'il a été exporté — sert
  uniquement à empêcher le double export (FR-010), pas à afficher un historique de synchronisation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un facilitateur peut configurer l'accès Azure DevOps de son équipe en moins de 2
  minutes.
- **SC-002**: L'Area Path et l'Iteration proposés à la création d'un board reflètent les données
  réelles d'Azure DevOps en moins de 5 secondes, pour une équipe configurée.
- **SC-003**: Un facilitateur peut importer les work items d'un sprint comme post-its en moins
  d'1 minute, sans ressaisie manuelle de leur contenu.
- **SC-004**: Un post-it exporté apparaît comme nouveau work item dans Azure DevOps en moins d'1
  minute.
- **SC-005**: Une équipe sans accès Azure DevOps configuré peut toujours créer un board sans aucun
  blocage ni dégradation par rapport au comportement actuel.

## Assumptions

- L'authentification à Azure DevOps se fait par PAT configuré une fois par équipe (Area Path),
  pas par utilisateur individuel — cohérent avec Équipe comme identité stable déjà établie
  (specs/001-retro-board-base).
- Les post-its importés (User Story 3) ne contiennent que le titre du work item, sans description
  détaillée ni lien de traçabilité affiché dans l'interface — garder le périmètre minimal
  (Constitution Principe VI, pas de sur-ingénierie).
- Le work item créé lors d'un export (User Story 4) est d'un type générique ("Task"), présent dans
  les modèles de processus standard d'Azure DevOps (Basic, Agile, Scrum, CMMI) ; le choix du type
  de work item n'est pas configurable dans ce MVP.
- L'échec de validation contre Azure DevOps (organisation injoignable, PAT expiré) n'empêche
  jamais la création ou l'utilisation normale d'un board — dégradation gracieuse plutôt que blocage
  (voir Edge Cases).
- Import et export sont des actions ponctuelles déclenchées manuellement par le facilitateur ; il
  n'y a pas de synchronisation continue ou automatique dans ce MVP.
- Cette feature ne couvre pas la mise à jour ou la clôture de work items existants dans Azure
  DevOps (FR-012) — seule la création de nouveaux work items via l'export est dans le périmètre.
