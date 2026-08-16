# Quickstart: Rappel de Réunion Teams

Guide de validation une fois l'implémentation en place (voir `tasks.md`). Complète le
[quickstart.md](../002-poll-utilite-reunion/quickstart.md) de la feature poll d'utilité.

## Prérequis

- Feature 002 (poll d'utilité de réunion) déployée et fonctionnelle, avec une équipe déjà associée
  à un channel Teams (commande `associer`).
- Pour un test local sans tenant Teams réel : Bot Framework Emulator, ou les tests automatisés via
  `TestAdapter` (voir `specs/002-poll-utilite-reunion/research.md#5`).

## Scénario de validation bout-en-bout

Correspond aux User Stories P1 → P2 de [spec.md](./spec.md).

1. **Rappel automatique après "réunion maintenue" (P1)** — Déclencher un poll (`sonder mêlée`),
   voter "Utile" depuis un compte, puis clôturer (`clore mêlée`). Vérifier que la carte de
   résultat "maintenue" apparaît, suivie immédiatement d'un message de rappel dans le même
   channel.
2. **Pas de rappel après "pas nécessaire"** — Déclencher un nouveau poll (ex: `sonder rétro`),
   voter uniquement "Pas nécessaire", puis clôturer. Vérifier qu'aucun message de rappel n'est
   envoyé après la carte de résultat.
3. **Rappel manuel sans poll (P2)** — Sur une équipe associée, sans déclencher aucun poll, envoyer
   `rappeler mêlée`. Vérifier qu'un message de rappel apparaît dans le channel.
4. **Rejet sur channel non associé** — Depuis un channel où aucune équipe n'est associée, envoyer
   `rappeler mêlée`. Vérifier le message d'erreur invitant à utiliser `associer` d'abord.
5. **Doublon rejeté (manuel après manuel)** — Envoyer `rappeler mêlée` une seconde fois le même
   jour pour la même équipe. Vérifier le message d'erreur indiquant qu'un rappel a déjà été
   envoyé aujourd'hui.
6. **Doublon silencieux (manuel puis automatique)** — Après le scénario 3 (rappel manuel déjà
   envoyé pour "mêlée" aujourd'hui), déclencher puis clôturer un poll "mêlée" avec au moins un
   vote "Utile". Vérifier que la carte de résultat "maintenue" apparaît normalement, mais qu'aucun
   second message de rappel n'est envoyé (silencieux, aucune erreur affichée).

## Résultat attendu

Tous les scénarios ci-dessus passent sans intervention technique manuelle, validant SC-001 à
SC-004 de la spécification.
