# Quickstart: Mini-jeu ROTI

Guide de validation une fois l'implémentation en place (voir `tasks.md`). Complète le
[quickstart.md](../006-systeme-extensions-etapes/quickstart.md) de specs/006-systeme-extensions-etapes.

## Prérequis

- Feature 006 (système d'extensions/étapes, mini-jeu "Météo d'équipe") déployée et fonctionnelle.
- Feature 007 (thèmes visuels par colonne) déployée — le mécanisme d'illustration par URL externe
  est réutilisé tel quel.

## Scénario de validation bout-en-bout

Correspond aux User Stories P1 → P2 de [spec.md](./spec.md).

1. **ROTI par défaut, sans configuration (P1)** — Composer une séquence avec une étape "ROTI" sans
   aucune personnalisation. Vérifier que l'étape apparaît avec les 5 niveaux, chacun illustré par
   son emoji par défaut.
2. **Répondre au ROTI (P1)** — Une fois l'étape ROTI active, répondre depuis plusieurs comptes
   participants. Vérifier que chaque réponse est prise en compte et visible par tous, sans
   rechargement manuel.
3. **Changer sa réponse (P1)** — Un participant ayant déjà répondu choisit un autre niveau.
   Vérifier que sa réponse précédente est remplacée (pas de doublon).
4. **Personnaliser un niveau (P2)** — Composer une étape ROTI en fournissant une URL d'image HTTPS
   valide pour un seul niveau. Vérifier que ce niveau affiche l'image fournie, et que les 4 autres
   affichent toujours leur emoji par défaut.
5. **URL de personnalisation invalide (P2)** — Tenter une URL non-HTTPS pour un niveau. Vérifier
   que la composition est refusée avec un message d'erreur explicite.
6. **Non-régression sur "Météo d'équipe"** — Vérifier que le mini-jeu "Météo d'équipe" déjà
   existant continue de fonctionner exactement comme avant (réponse, changement de réponse,
   affichage), sans interférence du nouveau mini-jeu ROTI.

## Résultat attendu

Tous les scénarios ci-dessus passent sans intervention technique manuelle, validant SC-001 à
SC-003 de la spécification, et confirmant l'absence de régression sur specs/006.
