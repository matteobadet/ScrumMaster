# Contract Delta: Commandes textuelles du bot (extension de specs/002-poll-utilite-reunion)

Voir `specs/002-poll-utilite-reunion/contracts/bot-commands.md` pour le mécanisme général
(commande reconnue dans le tour de conversation courant, réponse postée via
`ITurnContext.SendActivityAsync`).

## `rappeler <mêlée|rétro>` (nouvelle commande, FR-003)

Envoie manuellement un rappel pour le type de réunion donné, pour l'équipe associée au channel
courant.

**Exemple** : `@ScrumMaster rappeler mêlée`

**Comportement** :
- Channel non associé à une équipe → réponse d'erreur invitant à utiliser `associer` d'abord
  (FR-004).
- Un rappel a déjà été envoyé aujourd'hui pour cette équipe et ce type de réunion (automatique ou
  manuel) → réponse d'erreur l'indiquant, aucun nouveau rappel enregistré (FR-008).
- Sinon → enregistrement du rappel, message de rappel posté dans le channel (FR-005, FR-006,
  FR-007 — texte uniquement, pas de carte, pas d'action de convocation).

## `clore <mêlée|rétro>` (comportement étendu, FR-001, FR-002)

Après la clôture du poll et l'envoi de la carte de résultat (comportement inchangé, voir
`specs/002-poll-utilite-reunion/contracts/bot-commands.md`) :
- Si le résultat est "réunion maintenue" **et** qu'aucun rappel n'a encore été envoyé aujourd'hui
  pour cette équipe/type de réunion → un message de rappel est posté automatiquement à la suite de
  la carte de résultat, dans le même channel.
- Si le résultat est "réunion pas nécessaire" → aucun rappel n'est envoyé (FR-002).
- Si un rappel a déjà été envoyé aujourd'hui pour cette réunion (ex: rappel manuel antérieur) →
  aucun rappel automatique supplémentaire, aucun message d'erreur (silencieux — pas de déclencheur
  humain à informer, voir Edge Cases de `spec.md`).

## Erreurs

Toute commande `rappeler` mal formée (type de réunion absent/invalide) reçoit une réponse texte du
bot expliquant l'usage attendu, sans effet de bord — cohérent avec `associer`/`sonder`/`clore`.
