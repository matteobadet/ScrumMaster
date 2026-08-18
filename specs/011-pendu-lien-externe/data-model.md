# Data Model: Nouveaux mini-jeux — Pendu et Lien externe

## Extension de `Etape` (Type == MiniJeu)

| Champ | Type | Description |
|---|---|---|
| `MotAPendu` | `string?` | Le mot/l'expression à deviner (mini-jeu "pendu") — jamais transmis en clair tant que la partie n'est pas terminée (research.md#2). |
| `LienExterneNom` | `string?` | Nom du jeu externe (mini-jeu "lien-externe") — `null` tant que le facilitateur ne l'a pas renseigné. |
| `LienExterneUrl` | `string?` | URL HTTPS du jeu externe — `null` tant que non renseignée ; validée (research.md#6). |
| `LettresProposeesPendu` | `List<LettreProposeePendu>` | Journal partagé des lettres proposées pour cette étape (mini-jeu "pendu"). |

## `LettreProposeePendu` (nouvelle entité)

| Champ | Type | Description |
|---|---|---|
| `EtapeId` | `Guid` | Clé étrangère vers `Etape`. |
| `Lettre` | `char` | Lettre proposée, normalisée en majuscule invariante (research.md#4). |
| `Correcte` | `bool` | Vrai si la lettre est présente dans `MotAPendu`. |
| `ParticipantProposantId` | `Guid` | Qui a proposé cette lettre en premier. |
| `DateProposition` | `DateTimeOffset` | Horodatage de la proposition. |

**Clé primaire** : `(EtapeId, Lettre)` — garantit l'idempotence par construction (research.md#3).

## Valeurs calculées (jamais persistées)

- **Vue masquée du mot** (`MotMasquePendu`) : une entrée par caractère de `MotAPendu`, soit le
  caractère lui-même (séparateur, ou lettre déjà trouvée), soit `null` (lettre encore cachée).
- **Essais restants** (`EssaisRestantsPendu`) : `MaxEssaisPendu` (6, convention classique) moins le
  nombre de lignes `LettreProposeePendu` avec `Correcte = false` pour cette étape.
- **État de la partie** (`EtatPendu`) : `"EnCours"`, `"Victoire"` (toutes les lettres trouvées), ou
  `"Defaite"` (essais restants à zéro avant que le mot soit complet).
- **Mot complet** (`MotCompletPendu`) : uniquement calculé/exposé quand `EtatPendu != "EnCours"`
  (research.md#2).

## Transport (DTOs)

Extension de `EtapeDto` (specs/006-systeme-extensions-etapes) et `EtapeRequestDto` — voir
`contracts/rest-api-delta.md` pour le détail des champs ajoutés.

## Migration

Additive uniquement : trois nouvelles colonnes nullable sur `Etapes`, une nouvelle table
`LettresProposeesPendu`. Aucun backfill requis (les étapes existantes n'ont simplement pas ces
valeurs).
