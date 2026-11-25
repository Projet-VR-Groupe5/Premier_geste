# Préparation des voix du scénario

Les textes définitifs et les prises de son sont attendus. Ce tableau propose des rôles à confirmer avec l’équipe. Les crédits seront renseignés après réception et validation des prises.

| Fichier | Étape ou rôle | Proposition | Interprète confirmé |
|---|---|---|---|
| 01_Accueil.wav | Accueil | Abdelmoghite | En attente |
| 02_Securiser.wav | Sécuriser la zone | Abdelmoghite | En attente |
| 03_Verifier.wav | Vérifier la victime | Abdelmoghite | En attente |
| 04_Alerter.wav | Alerter les secours | Abdelmoghite | En attente |
| 05_RCR.wav | RCR | Abdelmoghite | En attente |
| 06_DEA.wav | DEA | Abdelmoghite | En attente |
| 07_Fin.wav | Fin | Abdelmoghite | En attente |
| Repartiteur_01.wav, Repartiteur_02.wav, etc. | Répartiteur 911 | Émile ou une autre personne volontaire | En attente |

Une même personne peut enregistrer les sept consignes pour garder un timbre et un débit cohérents. Émile valide les textes et l’ordre des répliques. Ilyas et Yohan peuvent faire une écoute de contrôle. Aina peut vérifier le niveau et l’intelligibilité lors des essais.

## Enregistrement et traitement

1. Utiliser un endroit calme et un seul microphone par voix. Garder la même distance et enregistrer quelques secondes de bruit ambiant. Lire les textes validés à un débit constant avec deux prises pour les répliques difficiles.
2. Écouter chaque prise au casque. Sélectionner les prises sans saturation, couper les reprises et les silences excessifs, et laisser une courte marge naturelle avant et après la voix.
3. Uniformiser les fichiers avec la normalisation de sonie d’Audacity ou d’un outil équivalent. Cible de travail proposée : −19 LUFS en mono, crêtes sous −1 dBFS. Ajuster au besoin après une écoute séquentielle avec le niveau de jeu prévu.
4. Exporter un fichier par consigne ou réplique en **WAV PCM 16 bits, mono, 48 kHz** dans `Assets/_Project/Audio/Voix`. Garder les prises brutes hors des Assets et respecter le nom exact de chaque fichier.
5. Vérifier le format puis écouter l’intégralité des exports. Contrôler le début et la fin des mots, le bruit, la cohérence du volume, l’ordre des répliques et la correspondance avec le texte validé.
6. Inscrire dans `CREDITS.md` le nom de l’interprète réel, les fichiers concernés et l’autorisation d’utilisation dans le projet. Garder les métadonnées Unity avec les WAV lors de l’intégration.
