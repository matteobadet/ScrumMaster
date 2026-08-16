# Quickstart: Poll d'Utilité de Réunion

Guide de validation une fois l'implémentation en place (voir `tasks.md`). Complète le
[quickstart.md](../001-retro-board-base/quickstart.md) de la feature de base.

## Prérequis

- Feature 001 (board de rétrospective) déployée et fonctionnelle, avec au moins une `Équipe`
  existante (créée via la création d'un board, `AreaPath` connu).
- Un enregistrement Azure Bot Service (App Registration Single-Tenant) provisionné manuellement :
  1. Créer une ressource "Azure Bot" dans le portail Azure, récupérer `MicrosoftAppId` et générer
     un `MicrosoftAppPassword`.
  2. Configurer le endpoint de messagerie sur `https://<domaine-scrummaster>/api/messages`.
  3. Fournir `MicrosoftAppId`/`MicrosoftAppPassword` à `ScrumMaster.Api` (variables d'environnement
     locales pour un test avec l'émulateur, Secret Kubernetes distinct pour la production — voir
     research.md#4).
- Pour un test local sans tenant Teams réel : Bot Framework Emulator, ou les tests automatisés via
  `TestAdapter` (voir research.md#5) qui ne nécessitent aucun enregistrement Azure.

## Scénario de validation bout-en-bout (avec Bot Framework Emulator ou un channel Teams réel)

Correspond aux User Stories P1 → P3 de [spec.md](./spec.md).

1. **Associer le channel (P1)** — Dans un channel où le bot est ajouté, envoyer
   `@ScrumMaster associer <AreaPath>` avec l'Area Path d'une équipe existante. Vérifier la
   confirmation du bot et que `Equipe.TeamsChannelId` est mis à jour (via une requête directe ou
   un nouvel `associer` qui doit refléter le changement).
2. **Déclencher un poll (P2)** — Envoyer `@ScrumMaster sonder mêlée`. Vérifier qu'une Adaptive
   Card apparaît dans le channel avec le titre attendu et deux boutons "Utile"/"Pas nécessaire".
3. **Voter (P2)** — Cliquer "Utile" depuis un premier compte. Vérifier que la carte se met à jour
   avec le décompte (1 Utile). Cliquer "Pas nécessaire" depuis un second compte, vérifier le
   décompte (1 Utile, 1 Pas nécessaire). Revoter avec le premier compte sur "Pas nécessaire" et
   vérifier que son vote précédent est remplacé (pas de doublon).
4. **Poll déjà ouvert** — Tenter `@ScrumMaster sonder mêlée` une seconde fois le même jour : vérifier
   le message indiquant qu'un poll est déjà en cours.
5. **Clôturer et consulter le résultat (P3)** — Envoyer `@ScrumMaster clore mêlée`. Vérifier
   qu'une carte de résultat apparaît indiquant "maintenue" (puisqu'au moins un vote "Utile" a été
   exprimé dans l'étape 3), avec le détail des votants.
6. **Vote après clôture rejeté (FR-008)** — Tenter de cliquer un bouton sur la carte de poll
   d'origine (si encore visible) : vérifier que le vote est rejeté avec un message clair.
7. **Résultat "pas nécessaire"** — Répéter les étapes 2-5 sur un nouveau poll (ex: `sonder rétro`)
   où tous les votes exprimés sont "Pas nécessaire" : vérifier que le résultat de clôture indique
   "pas nécessaire".
8. **Poll clos sans vote** — Déclencher puis clôturer immédiatement un poll sans aucun vote :
   vérifier que le résultat indique "maintenue" par défaut (Assumptions).

## Résultat attendu

Tous les scénarios ci-dessus passent sans intervention technique manuelle, validant SC-001 à
SC-004 de la spécification.
