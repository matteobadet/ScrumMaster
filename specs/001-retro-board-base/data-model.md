# Data Model: Board de Rétrospective Interactif de Base

## Équipe

Identité stable d'une équipe, portée par son Area Path Azure DevOps (Constitution Principe IV —
multi-tenant par conception).

| Champ | Type | Contraintes |
|-------|------|-------------|
| AreaPath | string | Clé primaire naturelle (ex: `"Krypton"`), non vide |

Aucun autre attribut dans ce MVP (pas de configuration PAT — hors périmètre, voir feature future
"Intégration Azure DevOps Boards").

## Thème

Modèle de colonnes réutilisable, prédéfini par le système ou personnalisé par un facilitateur.

| Champ | Type | Contraintes |
|-------|------|-------------|
| Id | Guid | Clé primaire |
| Nom | string | Non vide |
| EstPredefini | bool | `true` pour les thèmes fournis par le système (ex: Start/Stop/Continue) |
| Colonnes | liste de `Colonne` | **Au moins 1 colonne requise** (FR-015) |

## Colonne

| Champ | Type | Contraintes |
|-------|------|-------------|
| Id | Guid | Clé primaire |
| ThemeId | Guid | Clé étrangère → Thème |
| Intitule | string | Non vide |
| Ordre | int | Position d'affichage, unique au sein d'un Thème |

## Board de rétrospective

| Champ | Type | Contraintes |
|-------|------|-------------|
| Id | Guid | Clé primaire ; sert aussi de jeton d'accès dans l'URL (FR-012 — le lien seul suffit, aucun secret additionnel) |
| AreaPath | string | Clé étrangère → Équipe, non vide (FR-017) |
| Iteration | string | Non vide, ex: `"Sprint-138"` (FR-017) |
| ThemeId | Guid | Clé étrangère → Thème (copie du thème choisi/personnalisé à la création — voir note) |
| Statut | enum `Actif` \| `Cloture` | Transition unique `Actif → Cloture`, pas de réouverture dans ce MVP (non demandée) |
| DateCreation | timestamp | Non modifiable après création |

**Note d'implémentation** : un Thème personnalisé (FR-003) créé pour un board donné n'est pas
partagé avec les autres boards — chaque board référence son propre jeu de colonnes, qu'il vienne
d'un thème prédéfini (copié) ou d'un thème ad hoc.

## Participant

| Champ | Type | Contraintes |
|-------|------|-------------|
| Id | Guid | Clé primaire |
| BoardId | Guid | Clé étrangère → Board ; l'identité est scopée à un seul board/session (pas de compte, Assumptions) |
| NomAffiche | string | Non vide |
| Role | enum `Facilitateur` \| `Participant` | Exactement un `Facilitateur` par board, attribué au créateur (FR-013) |

## Post-it

| Champ | Type | Contraintes |
|-------|------|-------------|
| Id | Guid | Clé primaire |
| BoardId | Guid | Clé étrangère → Board |
| ColonneId | Guid | Clé étrangère → Colonne ; doit appartenir au Thème du Board |
| Texte | string | **Non vide** (FR-015) |
| AuteurParticipantId | Guid | Clé étrangère → Participant ; seul l'auteur peut modifier/supprimer (FR-005) |
| DateCreation / DateModification | timestamp | `DateModification` mise à jour à chaque édition/déplacement |

**Règle d'état** : toute mutation (création, édition, déplacement, suppression) est refusée si
`Board.Statut == Cloture` (FR-016).

## Vote

| Champ | Type | Contraintes |
|-------|------|-------------|
| PostItId | Guid | Clé étrangère → Post-it (clé composite avec ParticipantId) |
| ParticipantId | Guid | Clé étrangère → Participant |

**Contraintes** :
- Unicité `(PostItId, ParticipantId)` — un participant vote au plus une fois pour un même post-it
  (retirer puis revoter est permis, cf. FR-009).
- Nombre total de votes actifs d'un `ParticipantId` sur un `BoardId` donné ≤
  `Board.MaxVotesParParticipant` (FR-008).
- Un participant peut voter pour son propre post-it (Assumptions — aucune restriction).

## Attribut additionnel sur Board

| Champ | Type | Contraintes |
|-------|------|-------------|
| MaxVotesParParticipant | int | Configurable par le facilitateur à la création ; défaut 3 (Assumptions) |

## Relations (résumé)

```text
Équipe (1) ──< (N) Board
Thème (1) ──< (N) Colonne
Board (1) ── (1) Thème   [référence, copié à la création]
Board (1) ──< (N) Participant
Board (1) ──< (N) Post-it
Colonne (1) ──< (N) Post-it
Participant (1) ──< (N) Post-it   [auteur]
Post-it (1) ──< (N) Vote
Participant (1) ──< (N) Vote
```

## Index / requêtes fréquentes

- Toute lecture de contenu (post-its, votes, participants) est scopée par `BoardId` — l'index
  principal de chaque table enfant est `BoardId` (ou `PostItId`/`ThemeId` selon le cas).
- `Équipe.AreaPath` sert de point d'entrée pour un futur listing "boards d'une équipe" (hors
  périmètre de ce MVP, mais anticipé par le modèle — Constitution Principe IV).
