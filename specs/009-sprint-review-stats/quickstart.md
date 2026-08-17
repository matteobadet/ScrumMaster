# Quickstart: Point de sprint (stats Azure DevOps)

Guide de validation une fois l'implémentation en place (voir `tasks.md`).

## Prérequis

- Feature 005 (intégration Azure DevOps Boards) déployée et fonctionnelle : une équipe avec un
  accès Azure DevOps configuré (organisation/projet/PAT), et un board créé pour une Iteration réelle
  contenant des work items de types variés (au moins des Task et des User Story, dans des états
  différents).

## Scénario de validation bout-en-bout

Correspond aux User Stories P1 → P3 de [spec.md](./spec.md).

1. **Répartition par état (P1)** — Ouvrir le point de sprint sur un board dont l'Iteration contient
   des work items dans plusieurs états. Vérifier que les comptages "à faire / en cours / terminé"
   correspondent aux données réelles d'Azure DevOps.
2. **Iteration vide (P1)** — Ouvrir le point de sprint sur un board dont l'Iteration ne contient
   aucun work item. Vérifier un état vide explicite, sans erreur.
3. **Équipe non configurée (P1)** — Ouvrir (ou tenter d'ouvrir) le point de sprint sur un board dont
   l'équipe n'a pas d'accès Azure DevOps configuré. Vérifier que l'action est indisponible avec un
   message invitant à configurer l'accès.
4. **Distinction Task / User Story (P2)** — Sur une Iteration contenant les deux types, vérifier que
   la répartition par état est bien scindée par type, et qu'un type absent n'affiche pas de section
   vide.
5. **Taux de complétion (P3)** — Sur une Iteration avec un mélange de work items terminés et non
   terminés, vérifier que le total planifié et le total terminé affichés correspondent au calcul
   attendu (work items `Removed` exclus des deux totaux).
6. **Panneau accessible à tout moment** — Vérifier que le point de sprint reste consultable après
   la clôture du board (FR-001), et par un participant non-facilitateur (Assumptions de spec.md).

## Résultat attendu

Tous les scénarios ci-dessus passent sans intervention technique manuelle, validant SC-001 à
SC-003 de la spécification.
