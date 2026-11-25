# Parcours utilisateur et règles de confort

Projet Premiers Gestes · INFO4900 · Meta Quest 3

Référence de scène inspectée : `Assets/_Project/Scenes/Main.unity`, commit `dba4940`. Ce document décrit le parcours visé et indique les raccordements encore à valider. [Tâche GitHub nº 8](https://github.com/Projet-VR-Groupe5/Premier_geste/issues/8)

## Parcours en sept étapes

| Étape | Ce que le joueur voit | Ce que le joueur entend | Action attendue et interface | Déplacement |
|---|---|---|---|---|
| 1. Accueil | Parc, panneau d’entraînement, plateforme et cube saisissable | Accueil et objectif de l’exercice, voix 01 à enregistrer | Lire le panneau, essayer la téléportation et la saisie, puis utiliser le bouton **COMMENCER**. Dans Main, une surcharge du bouton appelle déjà le gestionnaire de scénario. | Départ à l’origine du rig. Rejoindre la surface d’entraînement pour pratiquer. |
| 2. Sécuriser la zone | Parc et espace d’intervention | Consigne de sécurisation, voix 02 à enregistrer | Observer la zone et repérer les dangers selon les règles du scénario. Le panneau de consignes sert de rappel. Critère de validation à confirmer. | Observer d’abord depuis une position stable, puis se téléporter si nécessaire. |
| 3. Vérifier la victime | Personne à évaluer, indication de l’étape | Consigne de vérification, voix 03 à enregistrer | Approcher la victime et interagir avec son épaule selon le scénario. L’identité du modèle et le déclencheur restent à valider. | Prévoir une destination dégagée et à portée d’interaction. |
| 4. Alerter les secours | Téléphone et personnages du parc | Consigne d’alerte et dialogue du répartiteur, voix 04 et répliques à enregistrer | Utiliser le téléphone pour appeler le 911 et désigner un passant pour demander le DEA selon le parcours prévu. Le téléphone est présent dans Main. Valider le dialogue et l’enchaînement. | Rejoindre le téléphone si l’interaction l’exige, puis revenir auprès de la victime. Les destinations dédiées restent à fixer. |
| 5. RCR | Victime et consigne de l’étape | Consigne RCR, voix 05 à enregistrer | Effectuer les compressions prévues par la simulation. Le composant de hauteur décale verticalement le rig de −0,60 m pendant l’étape et rétablit la hauteur à la sortie. | Se positionner à côté de la victime avant les compressions. Le point précis et la portée assise restent à valider. |
| 6. DEA | Modèle DEA, interface de l’appareil prévue | Consigne DEA, voix 06 à enregistrer. Invites sonores de l’appareil à confirmer. | Suivre l’interaction de pose des électrodes et l’utilisation du DEA prévues au scénario. Confirmer le retour de l’écran et les déclencheurs. | Utiliser le DEA à portée de la victime après sa récupération. |
| 7. Fin et bilan | Panneau Bilan prévu et fin de l’intervention | Fin et transition vers le bilan, voix 07 à enregistrer | Lire le temps total, les étapes réussies et la qualité des compressions lorsque les données seront reliées. Choisir **Recommencer** ou **Accueil** après raccordement. Le prefab affiche des valeurs de démonstration. | Lire le panneau depuis une position stable. Emplacement et visibilité à la fin à valider. |

## Vue de dessus

Axes Unity X et Z. Les coordonnées sont en mètres dans Main, arrondies au centimètre. Les flèches figurent l’ordre du parcours. Le schéma est indicatif.

```text
 +Z ↑
  8 │                                  [Surface d’entraînement]
  6 │                                    SolEntrainement (0, 8)
    │
  3 │                 Téléphone      Ch42     Panneau de consignes
    │                (−2,54, 2,65) (−0,77, 2,85) (0, 2,99)
    │                                         Panneau d’entraînement (0, 3)
  1 │    DEA (−4,12, 1,48)
    │
  0 │ Ch01 (−4,91, −0,15)  Ch16 (−2,46, −0,15)  Départ (0, 0)
    └──────────────────────────────────────────────────────────→ +X

 Départ → Entraînement → Zone sûre → Victime* → Téléphone → RCR* → DEA → Bilan*
 * Point d’interaction ou emplacement à valider.
```

- `ZoneEntrainement` et `XR Origin (XR Rig)` ont tous deux leur racine à (0, 0, 0). Le point de départ correspond à la racine sérialisée du rig. La posture réelle dépend du suivi XR.
- `ZoneEntrainement/SolEntrainement` et `SolProvisoire` portent des composants `TeleportationArea` dans Main. Ce sont des surfaces. L’ancre fixe `Teleportation Anchor` est relevée dans la Sandbox à (3, 0, 3). Les points fixes finaux du parc restent à valider.
- `Decor/DEA` se trouve à X −4,12 et Z 1,48. Les trois personnages sont des instances de modèles du décor. Les crédits identifient la victime comme Bryce. Aucun lien entre le rôle de victime et l’un des trois objets nommés `Ch01_nonPBR`, `Ch16_nonPBR` et `Ch42_nonPBR` n’a été confirmé dans les références de la scène et les scripts du scénario.
- Le panneau d’entraînement et le panneau fixe de consignes sont devant le point de départ. Le menu se déplace avec le rig. Vérifier son attache au poignet gauche et son comportement avec les deux orientations de main.

## Supports dans le monde

| Support | Informations |
|---|---|
| Panneau d’entraînement | Commandes de base, instructions de pratique et accès au scénario. |
| Panneau fixe de consignes | Action attendue à l’étape courante, mise à jour par le gestionnaire de scénario. |
| Menu au poignet gauche | Rappel des étapes, commencer, répéter la consigne, recommencer et retour à l’accueil. Le dialogue de confirmation est masqué par défaut et son raccordement relève de l’intégration. |
| Téléphone | Interaction d’appel et répliques du répartiteur. Affichage d’état à valider selon l’interaction disponible. |
| Écran du DEA | Invites d’utilisation et état de l’appareil, fonctionnement à confirmer. |
| Panneau de bilan | Temps total, étapes réussies, qualité des compressions et choix de sortie. |

## Règles de confort

- Utiliser uniquement la téléportation pour les déplacements virtuels du joueur. Couper le déplacement au joystick et vérifier que les fournisseurs de déplacement à la main, de saut et de grimpe ne peuvent pas déplacer le rig.
- Utiliser des crans de **45°**, selon la consigne mise à jour par Émile, avec rotation continue désactivée. Vérifier les deux sens et les deux manettes.
- Réserver le changement de hauteur à la RCR. Vérifier la portée en position assise et debout, l’absence de collision avec la victime et le retour à la hauteur initiale après la RCR. Le prototype applique un changement immédiat de hauteur à évaluer sur casque.
- Présenter les panneaux fixes à hauteur confortable, avec un recul d’environ 1 m ou plus comme repère initial. Placer le menu à distance de lecture naturelle du poignet et vérifier qu’il reste dans le champ de vision sans torsion excessive.
- Employer des libellés courts, du texte clair sur fond sombre et des cibles séparées. Contrôler la taille apparente en vision stéréoscopique et en mouvement.
- Laisser le joueur contrôler son orientation. Éviter les secousses, les panoramiques imposés et les animations de caméra. Tester spécifiquement la transition de hauteur prévue pour la RCR.
- Afficher le menu lorsque le joueur regarde son poignet selon la logique gérée par Émile. Vérifier les déclenchements involontaires et l’accès au bouton au rayon XR.
- Synchroniser les consignes écrites et orales. Prévoir la répétition de la consigne et garder le texte visible même quand la voix est indisponible.

## Validation restante

Vérifier en Play Mode puis sur Quest 3 le parcours complet, les deux manettes, le retour à l’accueil, l’orientation du menu et la lecture du bilan en position assise et debout. Confirmer avec l’intégration les destinations fixes, l’identité de la victime, la position pour la RCR, la disponibilité des voix et l’emplacement du bilan.
