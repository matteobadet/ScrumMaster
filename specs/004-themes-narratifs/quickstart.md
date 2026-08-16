# Quickstart: Thèmes de Rétrospective Narratifs

Guide de validation une fois l'implémentation en place (voir `tasks.md`). Complète le
[quickstart.md](../001-retro-board-base/quickstart.md) de la feature de base.

## Prérequis

- Feature 001 (board de rétrospective) déployée et fonctionnelle.

## Scénario de validation bout-en-bout

Correspond aux User Stories P1 → P2 de [spec.md](./spec.md).

1. **Icône sur un thème personnalisé (P1)** — Créer un board avec un thème personnalisé portant
   une icône (ex: "🐷", nom "Les 3 petits cochons", colonnes "Paille"/"Bois"/"Briques"). Vérifier
   que l'icône apparaît à côté du nom du thème dans l'en-tête du board.
2. **Absence d'icône** — Créer un board avec un thème prédéfini existant (ex: "Start / Stop /
   Continue", sans icône). Vérifier que l'en-tête n'affiche aucun espace vide ni erreur.
3. **Contexte sur un thème personnalisé (P2)** — Créer un board avec un thème personnalisé portant
   un texte de contexte. Vérifier que ce texte apparaît en introduction du board, avant les
   colonnes, pour tous les participants qui rejoignent.
4. **Contexte trop long** — Tenter de créer un board avec un contexte de plus de 500 caractères.
   Vérifier le rejet (400) avec un message explicite.
5. **Changement de thème en cours de session** — Sur un board déjà ouvert, le facilitateur change
   de thème (mécanisme existant, specs/001 User Story 4) vers un thème personnalisé portant une
   icône et un contexte différents. Vérifier que l'en-tête et le bloc de contexte affichés à tous
   les participants connectés se mettent à jour immédiatement.
6. **Icône sur un thème prédéfini** — Ajouter une icône/un contexte à un thème du catalogue (via
   le seed ou la persistance directe) et créer un board avec ce thème prédéfini. Vérifier que
   l'icône et le contexte apparaissent, identiques à ceux du thème personnalisé (US1/US2
   n'introduisent pas de traitement différent selon l'origine du thème — clarification de
   `spec.md`).

## Résultat attendu

Tous les scénarios ci-dessus passent sans intervention technique manuelle, validant SC-001 à
SC-003 de la spécification.
