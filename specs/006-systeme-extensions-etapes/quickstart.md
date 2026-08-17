# Quickstart: Système d'Extensions — Étapes de Rétrospective

Guide de validation une fois l'implémentation en place (voir `tasks.md`). Complète le
[quickstart.md](../001-retro-board-base/quickstart.md) de la feature de base.

## Prérequis

- Feature 001 (board de rétrospective) et feature 004 (thèmes narratifs) déployées et
  fonctionnelles.
- Au moins un board créé **avant** cette feature (pour valider la migration/rétrocompatibilité,
  scénario 6).

## Scénario de validation bout-en-bout

Correspond aux User Stories P1 → P3 de [spec.md](./spec.md).

1. **Composer une séquence (P1)** — Créer un board avec une séquence de 3 étapes : "Météo
   d'équipe" (mini-jeu), puis "Colonnes et post-its" (thème au choix), puis "Poll personnalisé"
   ("On garde la mêlée du matin ?", options Oui/Non). Vérifier que seule la première étape
   ("Météo d'équipe") est active et visible à la création.
2. **Avancer d'étape** — Faire avancer le board (`AvancerEtape`). Vérifier que la première étape
   devient consultable en lecture seule, et que la deuxième ("Colonnes et post-its") devient
   active pour tous les participants connectés, sans rechargement manuel.
3. **Board mono-étape inchangé** — Créer un board sans spécifier de séquence explicite (comme
   aujourd'hui). Vérifier que son comportement (post-its, vote, clôture) est identique à un board
   d'avant cette feature.
4. **Mini-jeu (P2)** — Sur l'étape "Météo d'équipe" active, répondre depuis plusieurs comptes
   participants. Vérifier que chaque réponse est prise en compte et visible par tous, et qu'un
   participant peut changer son choix.
5. **Poll personnalisé (P3)** — Faire avancer le board jusqu'à l'étape "Poll personnalisé".
   Répondre depuis plusieurs comptes, vérifier le décompte par option, et qu'un second choix
   remplace le précédent pour un même participant.
6. **Board existant inchangé (FR-014)** — Ouvrir un board créé avant cette feature (via son lien
   existant). Vérifier qu'il apparaît comme une séquence à une seule étape "Colonnes et post-its"
   contenant tous ses post-its existants, sans perte de contenu.
7. **Clôture finale** — Faire avancer le board depuis sa dernière étape. Vérifier que le board
   entier passe en lecture seule (comportement équivalent à la clôture actuelle,
   specs/001-retro-board-base).
8. **Import/export Azure DevOps toujours fonctionnels (specs/005)** — Sur l'étape "Colonnes et
   post-its" active d'une équipe configurée, importer des work items et exporter un post-it.
   Vérifier qu'ils s'appliquent à cette étape précise (pas aux autres étapes du board).

## Résultat attendu

Tous les scénarios ci-dessus passent sans intervention technique manuelle, validant SC-001 à
SC-004 de la spécification, et confirmant l'absence de régression sur specs/001, specs/004 et
specs/005.
