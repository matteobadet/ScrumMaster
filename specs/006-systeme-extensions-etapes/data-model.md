# Data Model: Système d'Extensions — Étapes de Rétrospective

Extension **structurelle** de `specs/001-retro-board-base/data-model.md` — `Board` perd son
`ThemeId` direct au profit d'une séquence d'`Étape`, et `PostIt` change de portée (`BoardId` →
`EtapeId`). Voir `research.md` pour la justification de chaque changement.

## Étape (nouvelle entité)

| Champ | Type | Contraintes |
|-------|------|-------------|
| Id | Guid | Clé primaire |
| BoardId | Guid | Clé étrangère → Board |
| Type | enum `ColonnesEtPostIts` \| `MiniJeu` \| `PollPersonnalise` | Non vide, fixe à la création (FR-001) |
| Ordre | int | Position dans la séquence du board, unique au sein d'un Board |
| Statut | enum `AVenir` \| `Active` \| `Terminee` | Transition unique `AVenir → Active → Terminee`, séquentielle, jamais de retour en arrière (Assumptions) ; une seule étape `Active` à la fois par board |
| ThemeId | Guid, nullable | Renseigné uniquement si `Type = ColonnesEtPostIts` (`research.md#1`) |
| MiniJeuCatalogueId | Guid, nullable | Renseigné uniquement si `Type = MiniJeu` |
| Question | string, nullable | Renseigné uniquement si `Type = PollPersonnalise` (FR-010) |

**Contrainte** : au moins une `Étape` par `Board` (FR-002).

## Board de rétrospective (extension)

| Champ | Type | Contraintes |
|-------|------|-------------|
| ~~ThemeId~~ | — | **Supprimé** — déplacé sur `Étape` (`research.md#1`) |
| Etapes | liste de `Étape` | Ordonnée par `Ordre` ; au moins un élément |

Tous les autres champs (`Id`, `AreaPath`, `Iteration`, `Statut`, `DateCreation`,
`MaxVotesParParticipant`, `Participants`) restent inchangés — `Statut` (`Actif`/`Cloture`) continue
de représenter l'état du board dans son ensemble, positionné à `Cloture` lorsque la dernière étape
de la séquence est clôturée (`research.md#4`).

## Post-it (portée modifiée)

| Champ | Type | Contraintes |
|-------|------|-------------|
| ~~BoardId~~ | — | **Renommé** en `EtapeId` (`research.md#2`) |
| EtapeId | Guid | Clé étrangère → Étape (doit être de `Type = ColonnesEtPostIts`) |

Tous les autres champs (`Id`, `ColonneId`, `Texte`, `AuteurParticipantId`, `DateCreation`,
`DateModification`, `WorkItemSourceId`, `WorkItemExporteId` — specs/005-azure-devops-boards)
restent inchangés.

**Règle de comptage de votes (révisée)** : le nombre de votes déjà utilisés par un participant est
désormais compté par étape (`Vote → PostIt.EtapeId`), comparé à `Board.MaxVotesParParticipant`
(`research.md#2`) — indépendant d'une éventuelle autre étape "Colonnes et post-its" du même board.

## Colonne, Thème, Vote, Participant

Inchangés (`specs/001-retro-board-base/data-model.md`, `specs/004-themes-narratifs/data-model.md`)
— `Colonne` reste rattachée à `Theme` (pas directement à `Étape`), atteinte via
`Étape.ThemeId → Theme → Colonnes`.

## MiniJeuCatalogue (nouvelle entité)

| Champ | Type | Contraintes |
|-------|------|-------------|
| Id | Guid | Clé primaire |
| Nom | string | Non vide |
| TypeInterne | string | Non vide, unique — clé utilisée par le frontend pour choisir le composant (`research.md#6`) |
| Description | string, nullable | — |

Seedée avec une entrée ("Météo d'équipe", `TypeInterne = "meteo-equipe"`) au premier démarrage,
même mécanisme que `ThemeSeeder` (specs/001-retro-board-base).

## ReponseMeteoEquipe (nouvelle entité, spécifique au mini-jeu "Météo d'équipe")

| Champ | Type | Contraintes |
|-------|------|-------------|
| EtapeId | Guid | Clé étrangère → Étape (`Type = MiniJeu`) ; clé composite avec ParticipantId |
| ParticipantId | Guid | Clé étrangère → Participant |
| Humeur | enum `Ensoleille` \| `Nuageux` \| `Pluvieux` \| `Orageux` | Non vide |
| DateReponse | timestamp | Mise à jour si le participant change son choix |

**Contrainte d'unicité** : `(EtapeId, ParticipantId)` — une réponse par participant et par étape,
remplaçable (même pattern que `VoteUtilite`, specs/002-poll-utilite-reunion).

## OptionPollPersonnalise (nouvelle entité)

| Champ | Type | Contraintes |
|-------|------|-------------|
| Id | Guid | Clé primaire |
| EtapeId | Guid | Clé étrangère → Étape (`Type = PollPersonnalise`) |
| Texte | string | Non vide |
| Ordre | int | Position d'affichage |

**Contrainte** : au moins deux options par étape de type `PollPersonnalise` (FR-010).

## ReponsePollPersonnalise (nouvelle entité)

| Champ | Type | Contraintes |
|-------|------|-------------|
| EtapeId | Guid | Clé étrangère → Étape ; clé composite avec ParticipantId |
| ParticipantId | Guid | Clé étrangère → Participant |
| OptionId | Guid | Clé étrangère → OptionPollPersonnalise (doit appartenir à la même Étape) |
| DateReponse | timestamp | Mise à jour si le participant change son choix |

**Contrainte d'unicité** : `(EtapeId, ParticipantId)` — remplacement, pas de doublon (FR-011,
`research.md#7`).

## Relations (résumé)

```text
Board (1) ──< (N) Étape                              [ordonnée par Ordre, ≥1]
Étape (1) ── (0..1) Theme                             [si Type = ColonnesEtPostIts]
Théme (1) ──< (N) Colonne                              [inchangé]
Étape (1) ──< (N) Post-it                              [si Type = ColonnesEtPostIts ; BoardId renommé EtapeId]
Étape (1) ── (0..1) MiniJeuCatalogue                   [si Type = MiniJeu]
Étape (1) ──< (N) ReponseMeteoEquipe                   [si MiniJeuCatalogue.TypeInterne = "meteo-equipe"]
Étape (1) ──< (N) OptionPollPersonnalise                [si Type = PollPersonnalise]
Étape (1) ──< (N) ReponsePollPersonnalise               [si Type = PollPersonnalise]
```

## Migration des données existantes

Voir `research.md#3` : la migration EF Core introduisant `Étape` DOIT inclure un backfill SQL
créant une `Étape` (Type = ColonnesEtPostIts, Ordre = 0) par `Board` existant à partir de son
ancien `ThemeId`, et re-pointer tous les `PostIt` existants vers cette étape (`EtapeId`).
