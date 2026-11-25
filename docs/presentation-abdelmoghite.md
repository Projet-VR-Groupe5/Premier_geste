# Présentation orale · Abdelmoghite Zerane

Durée indicative de trois à quatre minutes.

## Texte à présenter

Bonjour. Je m’appelle Abdelmoghite Zerane. Dans Premiers Gestes, mon rôle était de travailler sur la locomotion, l’ergonomie et l’interface. J’ai cherché à permettre au joueur de se déplacer, de retrouver la consigne et d’atteindre les objets utiles tout en gardant son attention sur le scénario.

J’ai participé à la configuration du XR Origin, qui est le rig du joueur dans Unity. Avec XR Interaction Toolkit et OpenXR, nous utilisons des surfaces où le joueur peut se téléporter. Le joueur pointe une destination, confirme, puis change de place sans avoir à faire défiler le décor. Pour se réorienter, le prototype utilisait des crans de 30 degrés. La configuration de confort demandée maintenant est de 45 degrés, avec la rotation continue et le déplacement continu désactivés. J’ai préparé les réglages détaillés pour qu’Émile puisse les appliquer dans Main.

J’ai aussi préparé une zone d’entraînement. Elle offre une plateforme où essayer la téléportation, un cube muni d’un Rigidbody et d’un XR Grab Interactable, ainsi qu’un panneau avec un bouton Commencer. Elle donne au joueur le temps de se familiariser avec les commandes avant d’entrer dans le scénario. J’ai testé cette zone au simulateur XR sur PC pendant le prototypage. Le prefab a ensuite été fusionné et Émile l’a intégré au parc.

Pour les interfaces, le panneau de consignes guide l’action en cours, tandis que le menu au poignet fournit un rappel que le joueur peut consulter à la demande. J’ai précisé la disposition de Commencer, ajouté Répéter la consigne et préparé une fenêtre de confirmation avec Oui et Non. Elle est masquée par défaut. J’ai aussi préparé un panneau de bilan en Canvas World Space avec TextMeshPro. Les exemples affichés servent à vérifier la présentation du temps total, des étapes réussies et de la qualité des compressions. Émile établit quand les afficher et à quelles actions relier les boutons.

Un autre enjeu a été la posture pendant la RCR. Le prototype abaisse le repère de la caméra pour rapprocher la hauteur virtuelle du point d’interaction, puis rétablit la hauteur précédente à la sortie de l’étape. Il faudra vérifier le changement de hauteur, la portée des mains et la lisibilité des panneaux en position assise et debout. Sur la version des interfaces que je présente, j’ai vérifié la structure des prefabs et les références de scène. Le lancement Unity utilisé pour cette validation a été bloqué par la licence, donc le rendu et les interactions de cette version restent à essayer. Les validations sur Quest 3 relèvent d’Émile.

Enfin, j’ai documenté les sept étapes du parcours, les repères du parc et les règles de confort. J’ai préparé les noms des fichiers et le protocole d’enregistrement des voix en attendant les textes définitifs. Le projet s’appuie sur le décor d’Ilyas, les interactions de Yohan, le travail de validation et de performance d’Aina et l’intégration d’Émile. Cette répartition permet d’avancer sur la lisibilité et le confort pendant que le scénario et ses interactions sont raccordés.

## Repères pour les questions

- **Canvas World Space**. Le panneau occupe une position et une taille dans la scène, ce qui permet de le lire comme un objet du monde et de le viser avec le rayon XR.
- **Rotation par crans**. Le champ Turn Amount est sur Snap Turn Provider. Les options du Controller Input Action Manager choisissent les actions actives sur chaque manette.
- **Position RCR**. Le script règle le décalage vertical de Camera Offset. Il faut donc vérifier la hauteur en tenant compte de la posture réelle et du mode de suivi.
- **Résultats**. Le prefab de bilan affiche des exemples marqués comme tels. Le calcul des résultats et les actions des boutons font partie de l’intégration.
