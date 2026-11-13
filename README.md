# Premiers Gestes – Formation VR aux premiers secours

Application de réalité virtuelle pour **Meta Quest 3** qui permet de pratiquer les premiers gestes face à un arrêt cardiaque : sécuriser, alerter le 911, faire la RCR et utiliser un défibrillateur (DEA).

Projet de cours **INFO4900 – Réalité virtuelle et technologies immersives**.

> 📸 _Capture d'écran à ajouter (jalon 5)._

## Vidéo de démonstration

_À compléter au jalon 5 par Aina : lien vers la vidéo tournée sur le Meta Quest 3._

## Équipe et rôles

| Membre | Rôle | Compte GitHub |
|---|---|---|
| Emile Otton | Gestionnaire de projet et intégration | XxEmile |
| Yohann | Responsable des interactions VR | _à compléter_ |
| Abdelmoghite Zerane | Responsable locomotion, ergonomie et interface | _à compléter_ |
| Ilyas | Responsable environnement et systèmes | moutadiilyas |
| Aina | Responsable tests, build et performance | _à compléter_ |

## Objectif et scénario d'utilisation

**Objectif :** permettre à une personne sans formation de pratiquer, dans un environnement réaliste et sans risque, les gestes qui sauvent lors d'un arrêt cardiaque.

**Scénario :** l'utilisateur se trouve dans un parc quand une personne s'effondre. Il doit :

1. **Accueil** – apprendre dans une zone d'entraînement à se téléporter, saisir un objet et appuyer sur un bouton.
2. **Sécuriser** – vérifier que la zone est sans danger.
3. **Vérifier** – taper sur l'épaule de la victime pour voir si elle réagit.
4. **Alerter** – appeler le 911 avec le téléphone et envoyer un passant chercher le DEA.
5. **RCR** – faire les compressions thoraciques au bon rythme.
6. **DEA** – poser les électrodes au bon endroit et délivrer le choc.
7. **Fin** – arrivée des ambulanciers et bilan : temps de réaction, qualité des compressions, erreurs.

## Fonctionnalités

| # | Exigence du cours | Dans notre projet | Responsable | État |
|---|---|---|---|---|
| 1 | Environnement immersif | Parc à l'échelle réelle, victime, passants, ambulanciers | Ilyas | ⏳ |
| 2 | Locomotion | Téléportation par points fixes, rotation par crans de 30° | Abdelmoghite | ⏳ |
| 3 | Interactions VR | Téléphone (911), taper sur l'épaule, DEA et électrodes, désigner un passant | Yohann | ⏳ |
| 4 | Retour utilisateur | Sons, surbrillance, vibrations, réactions de la victime | Yohann | ⏳ |
| 5 | Interface immersive | Menu au poignet, consignes sur des objets du monde, tablette de bilan | Abdelmoghite | ⏳ |
| 6 | Gestion des états | ScenarioManager (7 étapes), points de contrôle, recommencer, retour à l'accueil | Emile | ⏳ |
| 7 | Fonctionnalité avancée | Analyse des compressions (rythme et amplitude) et suivi des mains | Aina | ⏳ |
| 8 | Stabilité sur Quest 3 | Build testé chaque semaine sur le casque | Aina | ⏳ |
| 9 | Performance et confort | Objectif 72 Hz, mesures à chaque build | Aina | ⏳ |

⏳ À faire · 🔨 En cours · ✅ Terminé

## Technologies

- **Unity 6000.6.2f1** (même version que le labo), modèle Universal 3D (URP)
- **OpenXR** avec le groupe de fonctionnalités Meta Quest
- **XR Interaction Toolkit** (avec Starter Assets) et **XR Hands**
- **Git, Git LFS et GitHub**
- **Casque :** Meta Quest 3 (Android)

_Versions des packages : à compléter (voir `Packages/manifest.json`)._

## Ouvrir le projet

**Prérequis :**

- Unity Hub avec **Unity 6000.6.2f1** et le module **Android Build Support** (OpenJDK, Android SDK & NDK)
- **Git et Git LFS**, ou **GitHub Desktop**

**Étapes :**

```text
git lfs install
git clone https://github.com/Projet-VR-Groupe5/Premier_geste.git
```

1. Dans Unity Hub : **Add** → **Add project from disk** → choisir le dossier cloné.
2. Ouvrir la scène principale : _à compléter (Assets/_Project/Scenes/Main.unity)_.

Pour la méthode de travail de l'équipe, voir le [guide rapide](docs/guide-rapide.md).

## Build sur le Meta Quest 3

_À compléter au jalon 2 par Aina : résumé de la procédure et lien vers `docs/build.md`. L'APK final sera publié dans les Releases au jalon 5._

## Architecture

_À compléter aux jalons 3 à 5 par Emile : schéma des systèmes, ScenarioManager et ses événements._

**Organisation des dossiers :**

```text
Assets/
├── _Project/              ← tout ce que l'équipe crée
│   ├── Scripts/           ← un dossier par système
│   ├── Scenes/            ← Main et les scènes Sandbox de chaque membre
│   ├── Prefabs/
│   ├── Audio/
│   ├── Materials/
│   └── ScriptableObjects/ ← les données des étapes du scénario
└── ThirdParty/            ← ressources externes (voir CREDITS.md)
```

## Les systèmes

_Chaque responsable complète sa partie au jalon 5 : ce que fait le système, les scripts principaux, comment le tester._

### Scénario et intégration – Emile
_À compléter._

### Interactions VR – Yohann
_À compléter._

### Locomotion, ergonomie et interface – Abdelmoghite
_À compléter._

### Environnement et systèmes – Ilyas
_À compléter._

### Tests, build, performance et analyse des compressions – Aina
_À compléter._

## Choix techniques

| Décision | Pourquoi |
|---|---|
| Unity 6000.6.2f1 | Même version que le labo, pour éviter les conflits |
| URP (Universal Render Pipeline) | Adapté aux performances d'un casque autonome |
| OpenXR | Recommandé par Meta pour les nouveaux projets Quest |
| Téléportation et rotation par crans | Limiter l'inconfort (cybermalaise) |
| Une scène Sandbox par membre | Les scènes Unity se fusionnent mal : on évite les conflits |
| Git LFS | Garder le dépôt léger malgré les modèles 3D et les sons |

_À compléter au fil du projet._

## Performance et confort

_À compléter au jalon 4 par Aina : tableau des mesures par build (images par seconde moyennes, pires chutes, scène concernée)._

## Limitations connues

_À compléter aux jalons 4 et 5._

## Gestion de projet

- **Jalons :** [Milestones](https://github.com/Projet-VR-Groupe5/Premier_geste/milestones)
- **Tâches :** [Issues](https://github.com/Projet-VR-Groupe5/Premier_geste/issues)
- **Tableau de tâches :** _à venir_
- **Comptes rendus de réunion :** `docs/reunions/` _(à venir)_
- **Méthode de travail :** [guide rapide](docs/guide-rapide.md)

## Crédits et utilisation de l'IA

- Ressources externes et licences : `CREDITS.md` _(à venir)_
- Utilisation de l'IA générative : `AI_USAGE.md` _(à venir)_
