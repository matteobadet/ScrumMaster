# Contract: Hub temps réel (SignalR)

Route : `/hubs/retro-board`. Chaque board correspond à un groupe SignalR (nom de groupe =
`BoardId`). Toute méthode ci-dessous suppose que le client a préalablement rejoint le groupe via
`JoinBoard`. Voir décision technique dans [research.md](../research.md#1-mécanisme-de-temps-réel).

## Méthodes Client → Serveur

| Méthode | Paramètres | Autorisation | Rejet si |
|---------|-----------|---------------|----------|
| `JoinBoard` | `boardId`, `participantId` | Tout participant existant | `participantId` inconnu du board |
| `AddPostIt` | `boardId`, `colonneId`, `texte` | Tout participant | `texte` vide (FR-015) ; board `Cloture` (FR-016) |
| `EditPostIt` | `boardId`, `postItId`, `texte` | Auteur du post-it uniquement (FR-005) | `texte` vide ; auteur ≠ appelant ; board `Cloture` |
| `MovePostIt` | `boardId`, `postItId`, `colonneId` | Auteur du post-it uniquement | `colonneId` hors du thème du board ; board `Cloture` |
| `DeletePostIt` | `boardId`, `postItId` | Auteur du post-it uniquement | auteur ≠ appelant ; board `Cloture` |
| `Vote` | `boardId`, `postItId` | Tout participant | quota `MaxVotesParParticipant` atteint (FR-008) ; déjà voté pour ce post-it ; board `Cloture` |
| `RemoveVote` | `boardId`, `postItId` | Tout participant (son propre vote) | aucun vote existant à retirer |
| `ChangeTheme` | `boardId`, `themeId` ou `themePersonnalise` | Facilitateur uniquement (FR-013) | appelant non-facilitateur ; colonnes vides (FR-015) ; board `Cloture` |
| `CloseBoard` | `boardId` | Facilitateur uniquement (FR-013) | appelant non-facilitateur ; déjà clôturé |

Toute violation d'autorisation ou de validation lève une `HubException` avec un message
utilisateur ; le client affiche l'erreur sans modifier son état local (pas de mise à jour
optimiste avant confirmation serveur, conforme à la résolution "dernière écriture gagnante" de
research.md).

## Événements Serveur → Groupe

Diffusés à tous les membres du groupe `BoardId` (y compris l'auteur de l'action, pour rester
source de vérité unique côté client) :

| Événement | Payload | Déclenché par |
|-----------|---------|----------------|
| `ParticipantJoined` | `{ participantId, nomAffiche, role, votesRestants }` | `JoinBoard` |
| `PostItAdded` | `{ postIt }` | `AddPostIt` |
| `PostItUpdated` | `{ postItId, texte }` | `EditPostIt` |
| `PostItMoved` | `{ postItId, colonneId }` | `MovePostIt` |
| `PostItDeleted` | `{ postItId }` | `DeletePostIt` |
| `VoteChanged` | `{ postItId, nombreVotes }` | `Vote` / `RemoveVote` |
| `ThemeChanged` | `{ theme, colonnes }` | `ChangeTheme` |
| `BoardClosed` | `{ boardId }` | `CloseBoard` — le client passe le board en lecture seule (désactive les contrôles de mutation) |

## Reconnexion (User Story 2, scénario 3)

À la reconnexion automatique du client SignalR, le client réappelle `JoinBoard`, puis effectue un
`GET /api/boards/{boardId}` (REST) pour resynchroniser son état local avant de reprendre le flux
d'événements — évite de reconstruire l'état uniquement à partir d'événements potentiellement
manqués pendant la coupure.
