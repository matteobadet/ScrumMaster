# Quickstart: Thèmes Visuels par Colonne

Guide de validation une fois l'implémentation en place (voir `tasks.md`). Complète le
[quickstart.md](../004-themes-narratifs/quickstart.md) de specs/004-themes-narratifs.

## Prérequis

- Feature 001 (board de rétrospective) et feature 004 (thèmes narratifs) déployées et
  fonctionnelles.
- Au moins un board créé **avant** cette feature (pour valider la non-régression, scénario 5).

## Scénario de validation bout-en-bout

Correspond aux User Stories P1 → P3 de [spec.md](./spec.md).

1. **Colorer les colonnes d'un thème personnalisé (P1)** — Créer un board avec un thème
   personnalisé dont chaque colonne porte une couleur de fond différente. Vérifier que chaque
   colonne du board affiche sa couleur propre.
2. **Colonne sans couleur (P1)** — Dans le même thème personnalisé, laisser une colonne sans
   couleur. Vérifier qu'elle s'affiche avec l'apparence par défaut, sans espace vide ni erreur.
3. **Illustrer les colonnes d'un thème personnalisé (P2)** — Coller une URL d'image HTTPS valide
   pour chaque colonne du thème. Vérifier que chaque colonne affiche son illustration propre.
4. **URL d'illustration invalide (P2)** — Tenter de saisir une URL non-HTTPS (ex : `http://...`)
   pour une colonne. Vérifier que la création/modification est refusée avec un message d'erreur
   explicite (FR-009).
5. **Illustration devenue inaccessible (P2)** — Une fois le board créé, vérifier que si l'URL
   d'illustration ne charge pas (lien cassé simulé), la colonne reste utilisable et le reste du
   board ne casse pas (FR-010).
6. **Thème prédéfini entièrement habillé (P3)** — À la création d'un board, choisir le thème
   prédéfini "La rétro du randonneur" sans rien configurer. Vérifier que toutes ses colonnes
   affichent immédiatement une couleur et une illustration.
7. **Non-régression sur les thèmes existants** — Ouvrir un board créé avant cette feature (thème
   sans couleur ni illustration sur ses colonnes). Vérifier qu'il s'affiche exactement comme
   avant, sans erreur ni espace vide.
8. **Changement de thème en cours de session** — Sur un board actif, changer de thème
   (mécanisme existant, specs/001-retro-board-base User Story 4) vers un thème habillé. Vérifier
   que les couleurs/illustrations de colonnes affichées passent immédiatement au nouveau thème,
   pour tous les participants connectés, sans rechargement manuel.

## Résultat attendu

Tous les scénarios ci-dessus passent sans intervention technique manuelle, validant SC-001 à
SC-004 de la spécification, et confirmant l'absence de régression sur specs/001 et specs/004.
