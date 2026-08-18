# Research: Nouveaux mini-jeux — Pendu et Lien externe

## 1. Le Pendu a besoin de son propre mécanisme de réponse, pas de `RepondreMiniJeu`

**Decision**: Ajouter une méthode de hub dédiée `ProposerLettrePendu(boardId, etapeId, lettre)` et
une méthode de service dédiée (`MiniJeuService.ProposerLettrePenduAsync`), plutôt que de réutiliser
`RepondreMiniJeu`/`MiniJeuService.RepondreAsync` (Météo d'équipe, ROTI).

**Rationale**: `RepondreMiniJeu` modélise "un participant choisit UNE valeur parmi des options,
remplaçable" (research.md#4 de specs/008-roti-mini-jeu) — un upsert par `(EtapeId, ParticipantId)`.
Le Pendu est structurellement différent : c'est un journal partagé, append-only, de lettres
proposées par toute l'équipe (`(EtapeId, Lettre)`), où une lettre déjà proposée par n'importe qui
est définitivement acquise (FR-006) — il n'y a rien à "remplacer". Forcer ce mécanisme dans
`RepondreMiniJeu` (en encodant lettre+résultat dans une simple chaîne `reponse`) casserait la
sémantique déjà établie de cet endpoint pour Météo/ROTI. Deux méthodes clairement nommées, chacune
fidèle à la forme de données qu'elle sert, sont plus simples à lire et à tester séparément
(Constitution Principe VI).

## 2. Le mot du Pendu n'est jamais transmis en clair pendant la partie

**Decision**: `Etape.MotAPendu` (le mot réel) n'apparaît dans aucun DTO. Le serveur calcule à la
demande, à partir de `MotAPendu` et des lettres déjà proposées, une vue masquée
(`MotMasquePendu` : une entrée par caractère, soit le caractère visible — lettre trouvée ou
séparateur comme l'espace — soit `null` pour une lettre encore cachée). Le mot complet
(`MotCompletPendu`) n'est inclus dans le DTO qu'une fois la partie terminée (victoire ou défaite,
FR-007).

**Rationale**: FR-002 exige explicitement qu'aucun participant ne voie le mot en clair avant la fin
de la partie ; calculer la vue masquée côté serveur à chaque lecture (plutôt que de la stocker et
risquer une désynchronisation) élimine toute fuite accidentelle par une réponse API mal filtrée.

## 3. Idempotence des propositions de lettres via la clé composite de l'entité

**Decision**: `LettreProposeePendu` a pour clé primaire `(EtapeId, Lettre)` (lettre normalisée en
majuscule invariante, sans repli sur les accents — voir #4). Une tentative de proposer une lettre
déjà présente est simplement ignorée par le service (vérification d'existence avant insertion),
sans erreur ni décompte d'essai supplémentaire (FR-006, y compris pour deux participants proposant
la même lettre en même temps, Edge Case de spec.md).

**Rationale**: La contrainte d'unicité structurelle rend l'idempotence garantie par construction,
plutôt que par une logique applicative fragile à dupliquer à chaque point d'entrée.

## 4. Comparaison de lettres insensible à la casse, sensible aux accents

**Decision**: Chaque lettre (proposée ou dans le mot) est normalisée avec
`char.ToUpperInvariant()` avant comparaison/stockage — pas de suppression des accents.

**Rationale**: Correspond exactement à l'Edge Case de spec.md ("proposer 'e' ne révèle pas 'é'") ;
`ToUpperInvariant` gère la casse sans altérer les caractères accentués, contrairement à une
normalisation Unicode NFD qui les décomposerait.

## 5. "Lien externe" réutilise le pattern de configuration en direct du changement de thème

**Decision**: Ajouter une méthode de hub `DefinirLienExterne(boardId, etapeId, nom, url)`,
réservée au facilitateur, qui définit ou remplace `Etape.LienExterneNom`/`Etape.LienExterneUrl`
pendant que l'étape est active — même structure que `ChangeTheme` (`BoardService.ChangeThemeAsync`)
déjà en place pour l'étape "Colonnes et post-its". Composer l'étape "Lien externe" dans la séquence
ne nécessite aucun champ de contenu (`EtapeRequestDto` inchangé pour ce type) : seul le choix du
mini-jeu "Lien externe" dans le catalogue est requis à la composition.

**Rationale**: Découle directement de la clarification de spec.md (saisie en direct, pas à la
composition). Réutiliser le pattern déjà établi (facilitateur uniquement, mutation d'une étape déjà
active, diffusion immédiate à tous les participants) évite d'inventer un nouveau mécanisme de
configuration en direct pour ce seul cas.

## 6. Validation HTTPS de l'URL du lien externe : extraction d'un helper partagé

**Decision**: Extraire la validation HTTPS déjà écrite dans `EtapeService` (utilisée pour
l'illustration de colonne et les visuels ROTI) dans une petite méthode statique partagée,
réutilisée pour valider l'URL du lien externe (FR-014).

**Rationale**: Trois emplacements distincts (colonne, ROTI, lien externe) avec la même règle
("HTTPS syntaxiquement valide, ≤2048 caractères") justifient l'extraction — dupliquer une troisième
fois cette logique serait plus fragile que la partager (Constitution Principe VI, éviter la
duplication sans sur-abstraire).

## 7. Catalogue : deux nouvelles entrées idempotentes

**Decision**: `MiniJeuSeeder` gagne deux entrées supplémentaires ("Pendu",
`TypeInterne = "pendu"` ; "Lien externe", `TypeInterne = "lien-externe"`), suivant exactement le
même correctif idempotent-par-`TypeInterne` déjà en place (specs/007, specs/008).

**Rationale**: Continuité directe du pattern déjà validé deux fois dans ce projet.
