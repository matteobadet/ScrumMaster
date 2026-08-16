# Contract: Commandes textuelles du bot

Le bot reconnaît des messages adressés dans un channel Teams où il est présent (le membre
mentionne le bot suivi d'un mot-clé, ex: `@ScrumMaster sonder mêlée`). Toute commande est traitée
dans le tour de conversation courant : la réponse du bot est postée dans le même channel via
`ITurnContext.SendActivityAsync` (pas de messagerie proactive nécessaire pour ce MVP — voir
research.md#1).

## `associer <area-path>`

Associe le channel courant à l'Area Path donné (FR-001, FR-002).

**Exemple** : `@ScrumMaster associer Krypton`

**Comportement** :
- Équipe (`AreaPath`) inconnue → réponse d'erreur, aucune action (l'Équipe doit déjà exister,
  créée via specs/001-retro-board-base).
- Équipe connue → `Equipe.TeamsChannelId` mis à jour avec l'identifiant du channel courant.
  Confirmation postée dans le channel.

## `sonder <mêlée|rétro>`

Déclenche un poll d'utilité pour le type de réunion donné, pour l'équipe associée au channel
courant (FR-003).

**Exemple** : `@ScrumMaster sonder mêlée`

**Comportement** :
- Channel non associé à une équipe → réponse d'erreur invitant à utiliser `associer` d'abord.
- Un poll `Ouvert` existe déjà pour (équipe, type, date du jour) → réponse indiquant qu'un poll
  est déjà en cours, aucun nouveau poll créé.
- Sinon → création du `Poll d'utilité` (`Statut = Ouvert`), envoi de l'Adaptive Card de poll (voir
  [adaptive-cards.md](./adaptive-cards.md)) dans le channel.

## `clore <mêlée|rétro>`

Clôture le poll ouvert du jour pour le type de réunion donné, pour l'équipe associée au channel
courant (FR-004).

**Exemple** : `@ScrumMaster clore mêlée`

**Comportement** :
- Aucun poll `Ouvert` pour (équipe, type, date du jour) → réponse d'erreur.
- Poll `Ouvert` trouvé → passage à `Statut = Cloture`, `DateCloture` renseignée, calcul du
  résultat (FR-009) et envoi de la carte de résultat (voir adaptive-cards.md) dans le channel ;
  tout vote ultérieur sur ce poll est rejeté (FR-008).

## Erreurs

Toute commande mal formée (type de réunion absent/invalide, Area Path manquant pour `associer`)
reçoit une réponse texte du bot expliquant l'usage attendu, sans effet de bord.
