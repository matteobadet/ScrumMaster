# Quickstart: Intégration Azure DevOps Boards

Guide de validation une fois l'implémentation en place (voir `tasks.md`). Complète le
[quickstart.md](../001-retro-board-base/quickstart.md) de la feature de base.

## Prérequis

- Feature 001 (board de rétrospective) déployée et fonctionnelle, avec une équipe ayant déjà créé
  au moins un board (`Equipe.AreaPath` existant).
- Un projet Azure DevOps réel accessible, avec un Personal Access Token ayant les permissions de
  lecture sur les Area Paths/Iterations/work items, et d'écriture sur les work items (portée
  "Work Items: Read & Write" minimum).

## Scénario de validation bout-en-bout

Correspond aux User Stories P1 → P4 de [spec.md](./spec.md).

1. **Configurer l'accès (P1)** — Pour une équipe existante, enregistrer l'organisation, le projet
   et un PAT valide. Vérifier la confirmation, et qu'aucune réponse ni log n'affiche jamais le PAT
   en clair.
2. **Rejet d'un PAT invalide** — Tenter la même opération avec un PAT invalide. Vérifier le rejet
   avec un message d'erreur explicite, sans exposer le PAT.
3. **Sélection guidée à la création (P2)** — Créer un nouveau board pour l'équipe configurée.
   Vérifier que l'Area Path est proposé dans une liste (plutôt qu'en texte libre), et que
   l'Iteration correspondant au sprint en cours est présélectionnée.
4. **Repli en texte libre** — Créer un board pour une équipe non configurée (ou nouvelle). Vérifier
   que l'Area Path et l'Iteration restent saisissables en texte libre, sans blocage.
5. **Import de work items (P3)** — Sur un board dont l'Iteration correspond à des work items
   existants, déclencher l'import. Vérifier qu'un post-it apparaît par work item, avec son titre
   comme contenu, chez tous les participants connectés.
6. **Réimport sans doublon** — Déclencher l'import une seconde fois sur le même board. Vérifier
   qu'aucun post-it supplémentaire n'est créé pour les work items déjà importés.
7. **Export d'un post-it (P4)** — Exporter un post-it. Vérifier qu'un nouveau work item apparaît
   dans Azure DevOps avec le texte du post-it comme titre, et que le post-it est marqué comme
   exporté chez tous les participants connectés.
8. **Anti-doublon à l'export** — Tenter d'exporter à nouveau le même post-it. Vérifier que l'action
   est empêchée, sans second work item créé.

## Résultat attendu

Tous les scénarios ci-dessus passent sans intervention technique manuelle, validant SC-001 à
SC-005 de la spécification.
