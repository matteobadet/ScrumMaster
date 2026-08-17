# Phase 0 Research: Système d'Extensions — Étapes de Rétrospective

Cette feature est la plus invasive de la roadmap : elle transforme un board d'un unique thème de
colonnes (specs/001-retro-board-base) en une séquence d'étapes typées. Les décisions ci-dessous
visent délibérément le design le plus simple qui satisfasse `spec.md`, conformément à la
Constitution Principe VI — ni système de chargement dynamique de plugins, ni isolation d'exécution,
ni abstraction générique pour des types d'étapes hypothétiques non demandés.

## 1. Modélisation d'`Étape` : colonnes nullable plutôt que sous-classes/tables séparées

**Decision**: `Étape` est une seule entité portant un `Type` (enum à 3 valeurs fixes) et des
colonnes nullable propres à chaque type (`ThemeId` pour "Colonnes et post-its",
`MiniJeuCatalogueId` pour "Mini-jeu", `Question` pour "Poll personnalisé"), plutôt qu'une hiérarchie
de sous-types ou une table de jointure par type.

**Rationale**: Le catalogue de types est fixe et fermé (3 valeurs, définies dans `spec.md` — pas
un mécanisme ouvert où de nouveaux types seraient ajoutés sans toucher au code). Une union
étiquetée simple (colonnes nullable + `Type`) reflète honnêtement cette réalité sans construire
d'abstraction polymorphe (interface `IEtapeConfig`, table de jointure par type) qui n'apporterait
de valeur que si de nouveaux types apparaissaient dynamiquement — ce qui est explicitement hors
périmètre (Assumptions de `spec.md` : les types sont développés par l'équipe du projet, pas par un
mécanisme d'extension au runtime).

**Alternatives considered**:
- **Table de configuration séparée par type** (`ConfigurationColonnesEtPostIts`,
  `ConfigurationMiniJeu`, `ConfigurationPollPersonnalise`, chacune liée 1:1 à `Étape`) : plus
  "propre" formellement, mais 3 jointures systématiques pour lire une étape, sans bénéfice réel
  vu le nombre fixe et petit de types.
- **Hiérarchie de classes / interface `IEtapeConfig`** : anticipe un mécanisme de plugin
  polymorphe que `spec.md` exclut explicitement (pas de nouveau type sans toucher au code).

## 2. Portée des colonnes/post-its/votes : de `Board` à `Étape`

**Decision**: `PostIt.BoardId` devient `PostIt.EtapeId` (clé étrangère vers l'`Étape` de type
"Colonnes et post-its" qui la porte). Le quota de votes (`Board.MaxVotesParParticipant`) reste un
réglage unique au niveau du board, mais le **décompte des votes déjà utilisés** par un participant
est désormais calculé par étape (`Vote` → `PostIt` → `Étape`), pas cumulé sur tout le board.

**Rationale**: Découle directement de la clarification actée dans `spec.md` (FR-008) : deux
étapes "Colonnes et post-its" dans une même séquence ont des post-its et votes indépendants. Sans
ce changement, deux étapes de ce type partageraient un même quota de votes, ce qui n'aurait pas de
sens si leurs colonnes (et donc leurs post-its) sont elles-mêmes indépendantes.

**Alternatives considered**: Garder `PostIt.BoardId` et un quota de votes global cumulé sur tout le
board — rejeté, car incohérent avec l'indépendance déjà actée des étapes du même type.

## 3. Migration des boards existants (compatibilité, FR-014)

**Decision**: La migration EF Core qui introduit `Étape` inclut une étape de **backfill de
données** (pas seulement de schéma) : pour chaque `Board` existant, création d'une `Étape` unique
(Type = "Colonnes et post-its", Ordre = 0, `ThemeId` = l'ancien `Board.ThemeId`, Statut = "Active"
si `Board.Statut` = Actif sinon "Terminée"), puis rattachement de tous les `PostIt` existants de ce
board à cette nouvelle étape (`EtapeId`).

**Rationale**: FR-014 exige qu'un board créé avant cette feature reste utilisable sans migration
de contenu perceptible pour ses utilisateurs — cela ne peut être garanti que par un vrai backfill
de données au moment de la migration de schéma, pas seulement par une valeur par défaut côté code.

**Alternatives considered**: Traiter l'absence d'`Étape` comme un cas particulier dans le code
applicatif (board "legacy" sans étape) — rejeté : complexifierait durablement tout le code de
lecture du board pour un cas transitoire, alors qu'un backfill ponctuel à la migration l'élimine
définitivement.

## 4. Consolidation de `CloseBoard` en `AvancerEtape`

**Decision**: La méthode de hub `CloseBoard` (specs/001-retro-board-base) est remplacée par
`AvancerEtape(boardId)` : elle clôt l'étape active ; s'il existe une étape suivante dans la
séquence, elle l'active et diffuse `EtapeChangee` ; sinon, elle clôture le board entier et diffuse
`BoardClosed` (événement inchangé). Pour un board à une seule étape (tous les boards créés avant
cette feature, et tout nouveau board à une étape), le comportement observable est identique à
l'ancien `CloseBoard` (FR-014, SC-003).

**Rationale**: Évite de maintenir deux actions distinctes ("avancer" et "clôturer") qui se
recouvrent presque entièrement — clôturer le board n'est jamais qu'"avancer" depuis la dernière
étape. Un seul point d'entrée simplifie le frontend (un seul bouton "étape suivante / clôturer" qui
change de libellé selon la position dans la séquence).

**Alternatives considered**: Garder `CloseBoard` séparé et ajouter `AvancerEtape` uniquement pour
les transitions intermédiaires — rejeté, complexifie inutilement le frontend (deux boutons à
distinguer) pour une distinction qui n'a pas de valeur fonctionnelle propre.

## 5. Intégration avec l'import/export Azure DevOps (specs/005-azure-devops-boards)

**Decision**: `ImportWorkItems`/`ExportPostIt` (specs/005-azure-devops-boards) conservent leur
signature (`boardId` [, `postItId`]) mais résolvent désormais en interne l'étape "Colonnes et
post-its" **actuellement active** du board, et opèrent sur son `EtapeId` plutôt que sur le
`BoardId` directement. Rejeté (`DomainValidationException`) si l'étape active n'est pas de type
"Colonnes et post-its".

**Rationale**: Minimise l'impact sur une feature déjà livrée — le contrat externe (signatures des
méthodes de hub) ne change pas, seule la résolution interne s'adapte à la nouvelle scoping par
étape. Un import/export n'a de sens que sur une étape qui a des post-its.

**Alternatives considered**: Ajouter un paramètre `etapeId` explicite aux méthodes existantes —
rejeté, casse inutilement le contrat déjà en production pour un besoin que la résolution implicite
(étape active) couvre entièrement.

## 6. Catalogue de mini-jeux : une entrée pour ce MVP

**Decision**: `MiniJeuCatalogue` est une table simple (Id, Nom, `TypeInterne`, Description),
seedée par un `MiniJeuSeeder` (même pattern que `ThemeSeeder`, specs/001-retro-board-base) avec une
seule entrée pour ce MVP : "Météo d'équipe" (`TypeInterne = "meteo-equipe"`) — chaque participant
choisit une humeur (ensoleillé/nuageux/pluvieux/orageux), affichée agrégée à tous. `TypeInterne`
est la clé que le frontend utilise pour choisir quel composant afficher.

**Rationale**: FR-009/Assumptions de `spec.md` n'exigent qu'au moins un mini-jeu démontrable ; "Météo
d'équipe" est le plus simple à construire (un choix parmi 4 options, agrégé — même structure
qu'un vote) tout en étant un icebreaker réel et utile. Le catalogue reste une table de données
(pas un mécanisme de chargement dynamique) : ajouter un futur mini-jeu nécessite un nouveau
composant frontend + une ligne de seed, pas une nouvelle release du moteur d'étapes.

**Alternatives considered**: Construire plusieurs mini-jeux dès ce MVP — rejeté, au-delà du
périmètre demandé (Assumptions : "au moins un mini-jeu").

## 7. Poll personnalisé : réutilise le pattern d'upsert de vote (specs/002-poll-utilite-reunion)

**Decision**: `ReponsePollPersonnalise` a une contrainte d'unicité `(EtapeId, ParticipantId)` — un
nouveau choix remplace le précédent plutôt que d'en créer un second, même mécanisme d'upsert que
`VoteUtilite` (specs/002-poll-utilite-reunion/data-model.md).

**Rationale**: FR-011 exige explicitement ce comportement ("la modifier tant que l'étape reste
active... remplacement, pas de doublon") ; le pattern est déjà éprouvé dans le projet.

**Alternatives considered**: Aucune — le comportement demandé est identique à un mécanisme déjà
implémenté.

## 8. Transport : REST inchangé pour la lecture, SignalR étendu pour les nouvelles interactions

**Decision**: `GET /api/boards/{boardId}` continue de porter l'état complet du board, mais son
contenu change de forme (une liste d'`Étapes`, chacune avec son propre sous-état, plutôt que des
champs `theme`/`colonnes`/`postIts` au premier niveau). Les nouvelles interactions temps réel
(avancer d'étape, répondre à un mini-jeu, répondre à un poll personnalisé) sont de nouvelles
méthodes du hub existant (`RetroBoardHub`), au même titre que `ChangeTheme`/`Vote`.

**Rationale**: Cohérent avec la séparation déjà établie (REST pour l'état complet/hors session,
SignalR pour toute mutation) ; pas de nouveau canal de transport à introduire.

**Alternatives considered**: Aucune — extension directe du pattern déjà en place.
