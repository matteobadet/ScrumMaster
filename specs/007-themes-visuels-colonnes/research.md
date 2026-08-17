# Phase 0 Research: Thèmes Visuels par Colonne

## 1. Représentation de la couleur de colonne

**Decision**: Champ texte libre (`Couleur`, string nullable), saisi par le facilitateur comme une
valeur CSS valide (hexadécimal `#rrggbb` ou nom de couleur CSS), sans palette imposée ni gérée par
le système. Longueur maximale 30 caractères (large marge pour un hex ou un nom de couleur CSS).

**Rationale**: Cohérent avec le traitement déjà retenu pour l'icône de thème (specs/004,
research.md#1) : texte libre plutôt que composant/bibliothèque à maintenir. Une palette imposée
demanderait de définir et faire évoluer un catalogue de couleurs côté système — sur-conception
non justifiée par la spec (Constitution Principe VI).

**Alternatives considered**:
- Palette fermée de couleurs prédéfinies (enum) : rejetée — ajoute une gestion de catalogue sans
  bénéfice fonctionnel demandé par la spec, et empêche de reproduire fidèlement des couleurs
  pastel spécifiques comme celles de la capture d'écran de référence.
- Sélecteur de couleur uniquement (pas de saisie hex manuelle) : traité comme un détail d'UI côté
  frontend (le champ reste une string CSS en base et sur le réseau), pas une décision de plan.

## 2. Source et chargement de l'illustration

**Decision**: L'illustration est une URL HTTPS fournie par le facilitateur, pointant vers une
image déjà hébergée par un tiers. Le backend stocke uniquement la chaîne d'URL (`UrlIllustration`,
string nullable) et ne la récupère, ne la met en cache et ne la proxifie jamais côté serveur.
Chaque navigateur participant charge l'image directement via une balise `<img src>` standard,
exactement comme n'importe quelle image externe référencée sur une page web.

**Rationale**: Résolu explicitement en clarification (voir spec.md, Session 2026-08-17) — le
facilitateur colle une URL plutôt que de téléverser un fichier. Ce choix :
- évite d'introduire une infrastructure de stockage de fichiers/objet/CDN (Constitution Principe
  V, isolation du déploiement partagé — aucune nouvelle dépendance d'infrastructure) ;
- élimine toute surface SSRF côté serveur, puisque le backend ne fait jamais de requête HTTP vers
  l'URL fournie (pas de récupération, pas de proxy, pas de génération de miniature) ;
- reste cohérent avec Constitution Principe VI (pas de sur-ingénierie) : pas de système d'upload,
  de validation de contenu, ni de modération à construire pour une fonctionnalité d'habillage
  visuel facultative.

**Alternatives considered**:
- Upload de fichier stocké par ScrumMaster : rejeté par l'utilisateur en clarification — aurait
  nécessité un stockage objet/CDN (nouvelle infrastructure), une validation de format/taille, et
  des considérations de modération de contenu hors périmètre de cette feature.
- Proxy/miniature générée côté serveur à partir de l'URL fournie : rejeté — réintroduirait une
  récupération HTTP côté serveur (surface SSRF) que le choix de l'URL externe visait justement à
  éviter, pour un bénéfice (contrôle du format affiché) non demandé par la spec.

## 3. Validation de l'URL d'illustration

**Decision**: Validation syntaxique uniquement, côté serveur (et dupliquée côté client pour un
retour immédiat) : l'URL doit être une adresse absolue valide dont le schéma est `https`. Aucune
vérification que l'URL pointe effectivement vers une image (nécessiterait une récupération côté
serveur, explicitement évitée — voir #2) : ce cas est documenté comme limitation connue dans
spec.md (Edge Cases).

**Rationale**: FR-009 exige un rejet explicite des URLs non-HTTPS. Restreindre au schéma `https`
élimine par construction le vecteur XSS classique des URLs `javascript:`/`data:` dans un attribut
`src`, sans avoir besoin d'une bibliothèque de sanitisation dédiée. Le rendu React d'une valeur de
chaîne dans un attribut JSX (`<img src={url}>`) échappe déjà nativement la valeur, donc aucun
risque d'injection HTML indépendamment de cette validation.

**Alternatives considered**:
- Liste blanche de domaines d'hébergement d'images autorisés (ex: seulement Unsplash, Imgur) :
  rejetée — trop restrictive pour l'usage réel (le facilitateur doit pouvoir utiliser n'importe
  quelle image déjà hébergée, ex: son propre Drive, Confluence, etc.), non demandée par la spec.

## 4. Forme des DTOs de colonne (rupture de contrat mineure)

**Decision**: `ThemePersonnaliseDto.Colonnes` et `ThemeSummaryDto.Colonnes` passent de
`IReadOnlyList<string>` à une liste d'objets `{ Intitule, Couleur?, UrlIllustration? }`. C'est un
changement de forme (breaking change) du contrat REST existant, appliqué directement plutôt que
versionné.

**Rationale**: ScrumMaster n'expose pas d'API publique versionnée — le seul consommateur de ces
DTOs est le frontend React de premier parti, mis à jour dans le même changement. Ce projet a déjà
appliqué ce principe à plusieurs reprises (ex: restructuration `BoardStateDto` en
specs/006-systeme-extensions-etapes) sans mécanisme de versionnage, cohérent avec Constitution
Principe VI (pas de sur-ingénierie — un mécanisme de versionnage d'API serait disproportionné pour
un unique client de premier parti).

**Alternatives considered**:
- Ajouter `Couleurs`/`UrlsIllustrations` comme listes parallèles séparées, `Colonnes` restant
  `string[]` : rejetée — complique l'association index-à-index côté client et serveur sans
  bénéfice, alors qu'un objet par colonne est directement plus lisible et moins fragile.

## 5. Contenu du thème prédéfini entièrement habillé (FR-008/US3)

**Decision**: Ajouter au catalogue de seed (`ThemeSeeder`) un nouveau thème prédéfini "La rétro du
randonneur" (5 colonnes, chacune avec une couleur pastel et une URL d'illustration), inspiré de la
capture d'écran de référence fournie par l'utilisateur. Les illustrations pointent vers des images
générées par un service de placeholder déterministe (`https://placehold.co/`), avec une couleur de
fond et un court texte/emoji par colonne, plutôt que vers de vraies photographies tierces.

**Rationale**: Un thème de démonstration seedé par le système doit rester disponible de façon
fiable et durable (pas de lien mort après quelques mois) et ne pose aucune question de
droits/licence sur une photographie tierce réelle. `placehold.co` sert exactement ce cas d'usage
(images de test déterministes, gratuites, sans compte requis) et respecte la contrainte HTTPS
(`research.md#3`). Un facilitateur souhaitant de vraies illustrations reste libre d'utiliser
n'importe quelle URL pour son propre thème personnalisé (US2) — cette limitation ne concerne que
le contenu du thème seedé par l'équipe projet.

**Alternatives considered**:
- Héberger de vraies illustrations dans `frontend/src/assets/` et les servir depuis le frontend
  (URL relative) : rejetée — contredirait la décision #2 (l'illustration doit être une URL externe
  comme n'importe quelle colonne, y compris pour le thème seedé, pour rester sur le même mécanisme
  que celui utilisé par un facilitateur) et ajouterait des assets binaires versionnés dans le
  dépôt pour un thème de démonstration.
- Ne pas fournir de thème entièrement habillé (US3 hors périmètre) : rejeté — FR-008 l'exige
  explicitement, pour que la capacité soit démontrable sans configuration manuelle.
