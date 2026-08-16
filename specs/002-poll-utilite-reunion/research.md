# Phase 0 Research: Poll d'Utilité de Réunion

## 1. Hébergement du bot Teams

**Decision**: Étendre le projet ASP.NET Core existant `ScrumMaster.Api` (specs/001-retro-board-base)
avec un endpoint Bot Framework (`/api/messages`), plutôt que créer un service séparé.

**Rationale**: Conforme à la Constitution Principe II (pas de nouvelle techno/langage) et
Principe VI (pas de sur-ingénierie) — un seul service à construire, tester et déployer, comme le
hub SignalR déjà ajouté au même projet en feature 001. Le volume attendu (polls occasionnels par
équipe) ne justifie pas un service dédié.

**Alternatives considered**:
- **Azure Function dédiée au bot** : isolerait le traitement des activités Bot Framework, mais
  duplique l'accès aux données (`ScrumMasterDbContext`, entités `Équipe`) déjà dans
  `ScrumMaster.Api` ; complexité de déploiement supplémentaire non justifiée pour ce MVP.

## 2. Mécanisme de vote (interaction utilisateur)

**Decision**: Le poll est envoyé comme une **Adaptive Card** avec deux boutons `Action.Submit`
("Utile" / "Pas nécessaire"). Un clic déclenche une activité `Invoke`
(`adaptiveCard/action`) traitée par le bot pour enregistrer ou mettre à jour le vote, puis la
carte est mise à jour en place (`UpdateActivity`) pour refléter le décompte courant.

**Rationale**: Mécanisme natif et fiable pour les interactions Teams — contrairement aux
commandes de déclenchement/clôture/association (texte reconnu, décision actée en clarify), le
vote bénéficie d'un contrôle dédié qui élimine toute ambiguïté de saisie (faute de frappe,
formulation différente) et permet de mettre à jour le message existant plutôt que d'empiler des
messages à chaque vote (moins de bruit dans le channel).

**Alternatives considered**:
- **Réponse en texte libre** ("Utile"/"Pas nécessaire" tapé par l'utilisateur) : rejeté, plus
  fragile à interpréter et ne permet pas de mise à jour en place du message de poll.

## 3. Persistance de l'association équipe ↔ channel Teams

**Decision**: Ajouter une colonne nullable `TeamsChannelId` directement sur l'entité `Equipe`
existante (specs/001-retro-board-base/data-model.md), plutôt qu'une table de configuration
séparée.

**Rationale**: FR-001/FR-002 décrivent une relation 1:1 (un channel courant par équipe, sans
historique requis — voir Assumptions de la spec). Une colonne simple évite une jointure
supplémentaire pour l'opération la plus fréquente : retrouver l'équipe correspondant au channel
d'où provient une commande entrante.

**Alternatives considered**:
- **Table `ConfigurationTeamsEquipe` séparée** : utile si un historique des associations passées
  était requis ; explicitement hors périmètre (Assumptions), donc rejetée pour ce MVP.

## 4. Authentification du bot (Azure Bot Service)

**Decision**: Enregistrement Azure Bot Service standard (App Registration Single-Tenant),
`MicrosoftAppId`/`MicrosoftAppPassword` fournis à `ScrumMaster.Api` via une variable
d'environnement/Secret Kubernetes dédiée, distincte du Secret de connexion PostgreSQL existant.
Le provisionnement de la ressource Azure Bot elle-même est une tâche d'infrastructure manuelle
(hors code applicatif), documentée dans `quickstart.md`.

**Rationale**: Pattern standard du Bot Framework SDK, conforme à la décision déjà actée dans la
Constitution ("Contraintes techniques additionnelles — Intégration Teams").

**Alternatives considered**:
- **Managed Identity côté Azure Bot** : évite un secret partagé mais complexifie l'enregistrement
  initial ; écarté pour ce premier MVP de bot, réévaluable plus tard.

## 5. Stratégie de test du bot

**Decision**: Tests d'intégration utilisant `Microsoft.Bot.Builder.Adapters.TestAdapter`, qui
simule l'envoi d'activités (messages texte pour les commandes, activités `Invoke` pour les clics
de carte) et permet d'inspecter les réponses générées par le bot, sans dépendance à un tenant
Teams réel.

**Rationale**: Outil standard fourni par le Bot Framework SDK pour ce type de test ; cohérent avec
l'approche déjà en place (xUnit + tests d'intégration via `WebApplicationFactory`) en
specs/001-retro-board-base.
