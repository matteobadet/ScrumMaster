# Contract: Adaptive Cards

## Carte de poll (envoyée à l'ouverture)

Postée dans le channel lors du déclenchement (`sonder`). Contient deux boutons `Action.Submit`
qui déclenchent une activité `Invoke` (`adaptiveCard/action`) traitée par le bot.

**Contenu** :
- Titre : type de réunion (ex: "Mêlée du jour — utile ?")
- Corps : décompte courant des votes ("X Utile · Y Pas nécessaire") — recalculé et réaffiché à
  chaque mise à jour de la carte
- Boutons :
  - `Action.Submit` — data: `{ "action": "vote", "pollId": "<guid>", "reponse": "Utile" }`
  - `Action.Submit` — data: `{ "action": "vote", "pollId": "<guid>", "reponse": "PasNecessaire" }`

**Comportement au clic (FR-006, FR-007)** :
- Poll `Ouvert` → le vote de l'auteur du clic (`TurnContext.Activity.From.AadObjectId`) est
  enregistré ou remplace son vote précédent (FR-007) ; le bot répond à l'activité `Invoke` en
  renvoyant la carte mise à jour (`UpdateActivity` sur le message original), reflétant le nouveau
  décompte.
- Poll `Clos` → le vote est rejeté ; le bot répond avec un message éphémère (visible seulement par
  l'auteur du clic) indiquant que le poll est clos, la carte affichée reste inchangée pour les
  autres membres.

## Carte de résultat (envoyée à la clôture)

Postée dans le channel lors de la clôture (`clore`), en remplacement de la carte de poll ou en
complément (message distinct).

**Contenu** :
- Titre : type de réunion concerné et résultat ("Mêlée du jour : maintenue" ou "Mêlée du jour :
  pas nécessaire")
- Corps : décompte final des votes, avec le nom de chaque votant et sa réponse (FR-012 —
  affichage non anonyme)

Aucun bouton sur cette carte — le poll étant clos, plus aucune interaction n'est proposée.
