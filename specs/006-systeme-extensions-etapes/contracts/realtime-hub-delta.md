# Contract Delta: Hub temps réel (restructuration de specs/001-retro-board-base)

## `AvancerEtape` (remplace `CloseBoard`, `research.md#4`)

| Méthode | Paramètres | Autorisation | Rejet si |
|---------|-----------|---------------|----------|
| `AvancerEtape` | `boardId` | Facilitateur uniquement (FR-005) | appelant non-facilitateur ; board déjà `Cloture` |

**Comportement** : clôt l'étape active (`Statut = Terminee`). S'il existe une étape suivante
(`Ordre` immédiatement supérieur), elle passe à `Active` et l'événement `EtapeChangee` est diffusé.
Sinon, `Board.Statut = Cloture` et l'événement `BoardClosed` est diffusé (inchangé,
specs/001-retro-board-base). Pour un board à une seule étape, ce second cas s'applique toujours —
comportement identique à l'ancien `CloseBoard` (FR-014).

`CloseBoard` est supprimé : son comportement est un cas particulier d'`AvancerEtape`.

## `ChangeTheme`, `Vote`, `RemoveVote`, `AddPostIt`, `EditPostIt`, `MovePostIt`, `DeletePostIt` (portée révisée)

Signatures inchangées. Rejetés (`HubException`) si l'étape active du board n'est pas de type
"Colonnes et post-its" — ces méthodes opèrent désormais sur l'étape active plutôt que sur le board
directement (`research.md#2`). Les quotas de vote (`MaxVotesParParticipant`) sont comptés par
étape, pas cumulés sur le board (`data-model.md`).

## `ImportWorkItems`, `ExportPostIt` (specs/005-azure-devops-boards, résolution révisée)

Signatures inchangées (`boardId` [, `postItId`]). Résolvent désormais l'étape "Colonnes et
post-its" active du board plutôt que le board directement (`research.md#5`). Rejetés si l'étape
active n'est pas de ce type.

## `RepondreMiniJeu` (nouvelle méthode, US2)

| Méthode | Paramètres | Autorisation | Rejet si |
|---------|-----------|---------------|----------|
| `RepondreMiniJeu` | `boardId`, `etapeId`, `reponse` (dépend du `TypeInterne` — ex: `humeur` pour "meteo-equipe") | Tout participant | étape introuvable, pas de type `MiniJeu`, ou non `Active` |

**Comportement** : enregistre ou remplace la réponse du participant pour cette étape. Diffuse
`ReponseMiniJeuChangee` (`{ etapeId, participantId, nomAffiche, reponse }`) au groupe.

## `RepondrePollPersonnalise` (nouvelle méthode, US3)

| Méthode | Paramètres | Autorisation | Rejet si |
|---------|-----------|---------------|----------|
| `RepondrePollPersonnalise` | `boardId`, `etapeId`, `optionId` | Tout participant | étape introuvable, pas de type `PollPersonnalise`, non `Active`, ou `optionId` n'appartient pas à cette étape |

**Comportement** : enregistre ou remplace la réponse du participant (FR-011, upsert par
`(EtapeId, ParticipantId)`). Diffuse `ReponsePollPersonnaliseChangee`
(`{ etapeId, decompteParOption: [{ optionId, decompte }] }`) à tout le groupe — sans exposer qui a
répondu quoi aux autres participants, cohérent avec `MonVoteChanged` (specs/001-retro-board-base)
qui ne diffuse le détail individuel qu'à l'appelant.

## `EtapeChangee` (nouvel événement Serveur → Groupe)

| Événement | Payload | Déclenché par |
|-----------|---------|----------------|
| `EtapeChangee` | `{ etapePrecedenteId, nouvelleEtapeId }` | `AvancerEtape`, quand une étape suivante existe |

Le client resynchronise l'état complet du board via `GET /api/boards/{boardId}` à réception
(même stratégie que `ThemeChanged`, specs/001-retro-board-base — une étape porte potentiellement
un sous-état complet, plus sûr de resynchroniser que de patcher).
