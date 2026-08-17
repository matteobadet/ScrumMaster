# Quickstart: Historique des boards par équipe

Guide de validation une fois l'implémentation en place (voir `tasks.md`).

## Scénario de validation bout-en-bout

Correspond aux User Stories P1 → P2 de [spec.md](./spec.md).

1. **Lister les boards d'une équipe (P1)** — Créer plusieurs boards pour la même équipe (Area
   Path), à des moments différents, certains clôturés. Ouvrir l'historique de cette équipe et
   vérifier que tous les boards apparaissent, triés du plus récent au plus ancien, avec Iteration,
   date de création et statut visibles.
2. **Rouvrir un board depuis l'historique (P1)** — Cliquer sur une entrée clôturée et vérifier
   l'accès direct au board en lecture seule ; cliquer sur une entrée active et vérifier l'accès
   normal.
3. **Équipe sans historique (P1)** — Consulter l'historique d'un Area Path sans aucun board (ou
   inconnu). Vérifier un état vide explicite, sans erreur.
4. **Accès depuis le formulaire de création (P2)** — Renseigner un Area Path sur le formulaire de
   création de board et vérifier qu'un lien vers l'historique de cette équipe est proposé.
5. **Accès depuis un board ouvert (P2)** — Ouvrir un board existant et vérifier qu'un lien vers
   l'historique de son équipe est proposé.

## Résultat attendu

Tous les scénarios ci-dessus passent sans intervention technique manuelle, validant SC-001 à
SC-003 de la spécification.
