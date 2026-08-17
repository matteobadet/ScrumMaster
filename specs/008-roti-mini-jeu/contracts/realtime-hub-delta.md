# Contract Delta: Hub SignalR (extension de specs/006-systeme-extensions-etapes)

Aucune nouvelle méthode de hub, aucun nouvel événement. `RepondreMiniJeu(boardId, etapeId,
reponse)` accepte désormais aussi le nom d'un `NiveauRoti` (ex: `"TresRentable"`) comme valeur de
`reponse`, quand l'étape ciblée est de type ROTI — au même titre qu'un nom de `HumeurMeteo` pour
"Météo d'équipe" ; le service résout le `TypeInterne` du mini-jeu de l'étape pour savoir quel enum
parser (`research.md#4`). Une valeur ne correspondant pas à un niveau valide est refusée (message
d'erreur explicite via `HubException`, cohérent avec le comportement déjà en place pour "Météo
d'équipe").

`ReponseMiniJeuChangee` (`etapeId, participantId, nomAffiche, reponse`) reste inchangé — `reponse`
transporte simplement le nom du niveau ROTI choisi au lieu du nom de l'humeur météo.
