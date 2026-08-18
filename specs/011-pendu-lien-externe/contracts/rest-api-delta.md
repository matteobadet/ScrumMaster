# Delta REST API : Pendu et Lien externe

Aucun nouvel endpoint REST. `GET /api/mini-jeux` renvoie désormais 4 entrées (Météo d'équipe, ROTI,
Pendu, Lien externe). `POST /api/boards` (composition de séquence) et `GET /api/boards/{boardId}`
transportent les champs additionnels ci-dessous.

## `EtapeRequestDto` — nouveau champ

- `motPendu` (string, optionnel) : requis et non vide uniquement quand le mini-jeu choisi est
  "Pendu" (`TypeInterne = "pendu"`) ; doit contenir au moins une lettre. Rejeté (400) si fourni pour
  tout autre mini-jeu.

Aucun champ n'est ajouté pour "Lien externe" — son contenu est renseigné en direct (voir
`realtime-hub-delta.md`), pas à la composition (Clarifications de spec.md).

## `EtapeDto` — nouveaux champs (étape de type MiniJeu)

| Champ | Type | Présent quand |
|---|---|---|
| `motMasquePendu` | `string?[]` | Mini-jeu "pendu" — une entrée par caractère du mot, `null` si encore caché. |
| `lettresProposeesPendu` | `{ lettre, correcte, nomAffiche }[]` | Mini-jeu "pendu". |
| `essaisRestantsPendu` | `int?` | Mini-jeu "pendu". |
| `maxEssaisPendu` | `int?` | Mini-jeu "pendu" — toujours 6 (data-model.md), transmis pour éviter de coder la constante côté client. |
| `etatPendu` | `string?` | Mini-jeu "pendu" — `"EnCours"` \| `"Victoire"` \| `"Defaite"`. |
| `motCompletPendu` | `string?` | Mini-jeu "pendu" — présent uniquement si `etatPendu != "EnCours"`. |
| `lienExterneNom` | `string?` | Mini-jeu "lien-externe" — `null` tant que non renseigné. |
| `lienExterneUrl` | `string?` | Mini-jeu "lien-externe" — `null` tant que non renseignée. |

Ces champs sont `null` pour toute étape qui n'est pas le mini-jeu correspondant, cohérent avec le
pattern déjà établi pour Météo/ROTI (union étiquetée).
