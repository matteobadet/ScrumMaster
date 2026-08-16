# Phase 0 Research: Board de Rétrospective Interactif de Base

## 1. Mécanisme de temps réel

**Decision**: SignalR (ASP.NET Core), transport WebSocket avec repli automatique.

**Rationale**: SignalR est le mécanisme temps réel natif et idiomatique de la stack déjà imposée
par la Constitution (ASP.NET Core). Il fournit nativement : négociation de transport avec repli
automatique (WebSockets → Server-Sent Events → long polling) utile derrière un ingress Traefik non
garanti de supporter les WebSockets dans toutes les configurations, reconnexion automatique côté
client (couvre le scénario "perte puis reprise de connexion réseau" de la User Story 2), et un
modèle de "groupes" qui correspond exactement au besoin de scoper la diffusion des mutations à un
board donné. Un client officiel `@microsoft/signalr` existe pour React/TypeScript. C'est la
décision technique explicitement laissée ouverte par la Constitution ("le choix technique n'est
pas encore arrêté au niveau de cette constitution").

**Alternatives considered**:
- **WebSockets bruts (`System.Net.WebSockets`)** : plus léger, mais réimplémente manuellement la
  reconnexion, le groupement par board et le repli de transport déjà fournis par SignalR — rejeté
  pour un MVP, complexité non justifiée.
- **Polling HTTP court intervalle** : simple mais ne peut pas garantir SC-002 (<3s) sans solliciter
  excessivement le serveur pour 10 participants/board ; expérience moins fluide. Rejeté.
- **PostgreSQL LISTEN/NOTIFY relayé via un service séparé** : ajoute un composant d'infrastructure
  supplémentaire pour un bénéfice nul à cette échelle (10 participants/board) ; rejeté pour
  sur-ingénierie (Constitution Principe VI).

## 2. Accès aux données (ORM)

**Decision**: Entity Framework Core avec le fournisseur Npgsql (PostgreSQL).

**Rationale**: Standard de facto pour ASP.NET Core + PostgreSQL, migrations versionnées intégrées
(nécessaires pour déployer les évolutions de schéma sur le cluster k3s), support LINQ réduisant le
risque d'injection SQL pour les requêtes scopées par Area Path (Constitution Principe IV).

**Alternatives considered**:
- **Dapper** : plus performant en lecture brute mais demande d'écrire et maintenir les migrations
  SQL à la main ; complexité non justifiée pour le volume de données du MVP (un board = quelques
  dizaines de post-its).

## 3. Outillage frontend

**Decision**: React 18 + Vite (build/dev server) + TypeScript.

**Rationale**: Vite offre un démarrage et un hot-reload rapides adaptés à l'itération sur un board
interactif ; produit un bundle statique simple à servir derrière Traefik (pas de serveur Node en
production requis, contrairement à Next.js dont le rendu serveur n'apporte aucune valeur pour un
board interne authentifié par lien).

**Alternatives considered**:
- **Next.js** : apporte du SSR/routing serveur inutile ici (le board est une application cliente
  interactive derrière un lien, pas un site à indexer) ; complexité de déploiement supplémentaire
  rejetée.
- **Create React App** : outillage non maintenu activement par l'écosystème React ; écarté au
  profit de Vite.

## 4. Résolution des conflits d'édition simultanée

**Decision**: Dernière écriture gagnante au niveau serveur, chaque mutation (édition de texte,
déplacement, vote) est un message discret horodaté par le serveur au moment de sa réception ; le
serveur applique et diffuse dans l'ordre de réception, sans verrou distribué.

**Rationale**: Conforme à l'Assumption déjà actée dans `spec.md` ; un post-it est une unité de
contenu simple (pas d'édition collaborative caractère-par-caractère de type CRDT), donc un
remplacement complet du champ texte à chaque sauvegarde suffit et reste simple à tester.

**Alternatives considered**:
- **Verrouillage optimiste (ETag/version par post-it)** : ajoute une gestion d'erreurs de conflit
  côté client (retry, merge) non requise par la spec ; écarté pour ce MVP, réévaluable si des
  conflits perçus deviennent un problème réel en usage.

## 5. Stratégie de test du temps réel

**Decision**: Tests d'intégration backend avec `Microsoft.AspNetCore.Mvc.Testing` +
`HubConnectionBuilder` (client SignalR de test) simulant 2+ participants sur le même hub ; tests
frontend (Vitest) avec un mock du client SignalR pour les composants, complétés par une validation
manuelle multi-onglets décrite dans `quickstart.md`.

**Rationale**: Permet de vérifier FR-007 (propagation temps réel) sans dépendre d'un navigateur
réel en CI, tout en gardant une procédure de validation humaine simple pour le comportement
perçu (SC-002).
