# Quickstart: Nouveaux mini-jeux — Pendu et Lien externe

Guide de validation une fois l'implémentation en place (voir `tasks.md`).

## Scénario de validation bout-en-bout

Correspond aux User Stories P1 → P2 de [spec.md](./spec.md).

1. **Pendu, mot masqué (P1)** — Composer une étape "Pendu" avec le mot "RETROSPECTIVE". Une fois
   active, vérifier que tous les participants voient le mot masqué (structure visible, aucune
   lettre en clair).
2. **Pendu, lettre correcte (P1)** — Proposer une lettre présente dans le mot depuis un compte
   participant. Vérifier que toutes ses occurrences se révèlent pour tous les participants.
3. **Pendu, lettre incorrecte (P1)** — Proposer une lettre absente. Vérifier que le nombre d'essais
   restants diminue pour tous.
4. **Pendu, lettre déjà proposée (P1)** — Reproposer une lettre déjà essayée. Vérifier qu'aucun
   essai n'est consommé.
5. **Pendu, victoire (P1)** — Compléter le mot. Vérifier l'affichage de la victoire.
6. **Pendu, défaite (P1)** — Épuiser les essais avant de compléter le mot. Vérifier l'affichage de
   la défaite et la révélation du mot complet.
7. **Lien externe, attente (P2)** — Composer une étape "Lien externe" sans la configurer. Une fois
   active, vérifier l'état d'attente explicite pour les participants.
8. **Lien externe, configuration en direct (P2)** — En tant que facilitateur, renseigner un nom et
   une URL HTTPS valide. Vérifier que tous les participants voient immédiatement le lien.
9. **Lien externe, URL invalide (P2)** — Tenter une URL non-HTTPS. Vérifier le rejet avec message
   d'erreur explicite.
10. **Lien externe, ouverture (P2)** — Cliquer sur le lien affiché. Vérifier l'ouverture dans un
    nouvel onglet, sans perturber le board ScrumMaster.
11. **Non-régression** — Vérifier que les mini-jeux "Météo d'équipe" et "ROTI" continuent de
    fonctionner exactement comme avant.

## Résultat attendu

Tous les scénarios ci-dessus passent sans intervention technique manuelle, validant SC-001 à
SC-004 de la spécification, et confirmant l'absence de régression sur specs/006 et specs/008.
