# Quickstart: Board de Rétrospective Interactif de Base

Guide de validation manuelle une fois l'implémentation en place (voir `tasks.md`). Ne remplace pas
les tests automatisés décrits dans `research.md#5`.

## Prérequis

- .NET 8 SDK, Node.js 20+
- PostgreSQL local (ou conteneur) avec une base `scrummaster` vide
- Backend et frontend démarrés (commandes exactes définies lors de l'implémentation, ex.
  `dotnet run` dans `backend/src/ScrumMaster.Api`, `npm run dev` dans `frontend/`)

## Scénario de validation bout-en-bout

Correspond aux User Stories P1 → P4 de [spec.md](./spec.md), à exécuter dans deux onglets/
navigateurs distincts (A = facilitateur, B = participant) pour couvrir le temps réel.

1. **Créer un board (P1, SC-001)** — Onglet A : ouvrir l'application, créer un board avec Area
   Path `"Krypton"`, Iteration `"Sprint-138"`, thème par défaut, nom affiché `"Alex"`. Vérifier
   que le board s'affiche avec les colonnes du thème par défaut en moins d'1 minute. Copier le
   lien du board.
2. **Rejoindre en tant que participant (P2)** — Onglet B : ouvrir le lien copié, rejoindre avec le
   nom `"Sam"`. Vérifier que le board affiche les mêmes colonnes, sans thème modifiable visible
   (rôle `Participant`, pas `Facilitateur`).
3. **Collaboration temps réel (P2, SC-002)** — Onglet A : ajouter un post-it dans une colonne.
   Vérifier dans l'onglet B, sans rafraîchir, que le post-it apparaît en moins de 3 secondes avec
   l'auteur `"Alex"` visible. Répéter dans l'autre sens (B ajoute, A observe).
4. **Édition et déplacement** — Onglet A : déplacer son propre post-it vers une autre colonne,
   modifier son texte. Vérifier la propagation vers B. Dans l'onglet B, tenter de modifier le
   post-it d'Alex : vérifier que l'action est refusée (FR-005).
5. **Vote (P3)** — Onglet B : voter pour le post-it d'Alex, vérifier le compteur visible dans les
   deux onglets. Voter jusqu'à atteindre la limite par défaut (3) et vérifier qu'un 4e vote est
   refusé avec un message clair (FR-008). Retirer un vote et vérifier que le compteur diminue et
   qu'un nouveau vote redevient possible.
6. **Validation des entrées (FR-015)** — Tenter d'ajouter un post-it avec un texte vide : vérifier
   le refus. Tenter de créer un thème personnalisé sans colonne : vérifier le refus.
7. **Personnalisation du thème (P4)** — Onglet A : créer un second board avec un thème
   personnalisé (colonnes `["Continuer", "Arrêter", "Essayer"]`). Vérifier que le board affiche
   exactement ces colonnes.
8. **Clôture (FR-016)** — Sur le premier board, onglet A : clôturer le board. Vérifier dans les
   deux onglets que le board passe en lecture seule (impossible d'ajouter/éditer/voter), tout en
   restant consultable.
9. **Reconnexion (User Story 2, scénario 3)** — Onglet B : couper la connexion réseau quelques
   secondes puis la rétablir. Vérifier que l'affichage se resynchronise avec l'état courant sans
   perte du contenu ajouté entre-temps par A.

## Résultat attendu

Tous les scénarios ci-dessus passent sans intervention technique manuelle, validant SC-001 à
SC-005 de la spécification.
