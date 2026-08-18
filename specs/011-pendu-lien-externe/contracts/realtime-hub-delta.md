# Delta Hub temps réel : Pendu et Lien externe

Deux nouvelles méthodes de hub, deux nouveaux événements diffusés. Aucune méthode existante n'est
modifiée.

## `ProposerLettrePendu(boardId, etapeId, lettre)`

**Appelant** : tout participant du board (facilitateur inclus, research.md#1 ; cohérent avec
l'absence de restriction de rôle sur Météo/ROTI).

**Comportement** :
- Rejette (HubException) si l'étape n'est pas un mini-jeu "pendu" actif, ou si la partie est déjà
  terminée (`EtatPendu != "EnCours"`).
- Si la lettre a déjà été proposée (par n'importe qui), l'appel est un no-op silencieux (FR-006) —
  pas d'erreur, pas de diffusion supplémentaire.
- Sinon, enregistre la proposition, recalcule l'état de la partie, diffuse `LettrePenduProposee` à
  tout le groupe du board.

**Événement diffusé** : `LettrePenduProposee`
```json
{
  "etapeId": "...",
  "lettre": "R",
  "correcte": true,
  "motMasquePendu": ["R", null, "T", null, ...],
  "lettresProposeesPendu": [{ "lettre": "R", "correcte": true, "nomAffiche": "Alex" }],
  "essaisRestantsPendu": 6,
  "maxEssaisPendu": 6,
  "etatPendu": "EnCours",
  "motCompletPendu": null
}
```

## `DefinirLienExterne(boardId, etapeId, nom, url)`

**Appelant** : facilitateur uniquement (research.md#5 ; rejeté avec HubException sinon, cohérent
avec `ChangeTheme`).

**Comportement** :
- Rejette si l'étape n'est pas un mini-jeu "lien-externe" actif.
- Rejette si `nom` est vide, ou si `url` n'est pas une adresse HTTPS valide (research.md#6).
- Sinon, enregistre/remplace `LienExterneNom`/`LienExterneUrl`, diffuse `LienExterneDefini` à tout
  le groupe du board.

**Événement diffusé** : `LienExterneDefini`
```json
{ "etapeId": "...", "nom": "Gartic Phone", "url": "https://garticphone.com/..." }
```
