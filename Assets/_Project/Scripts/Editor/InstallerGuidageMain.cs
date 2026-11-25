using System.Collections.Generic;
using System.Text;
using PremiersGestes.Interactions;
using PremiersGestes.Interface;
using PremiersGestes.Scenario;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PremiersGestes.EditorTools
{
    /// <summary>
    /// Menu « Premiers Gestes → Installer le guidage dans Main ».
    /// En un clic, installe ou met à jour dans la scène Main :
    /// le guide vocal (avec les voix trouvées dans Audio/Voix), le panneau de bilan,
    /// les réglages de confort (téléportation seulement) et la position du panneau de consignes.
    /// On peut le relancer sans risque (par exemple quand les voix sont ajoutées) : il ne crée rien en double.
    /// </summary>
    public static class InstallerGuidageMain
    {
        private const string CheminPanneauBilan = "Assets/_Project/Prefabs/Interface/PanneauBilan.prefab";
        private const string DossierVoix = "Assets/_Project/Audio/Voix";
        private static readonly string[] NomsVoix =
            { "01_Accueil", "02_Securiser", "03_Verifier", "04_Alerter", "05_RCR", "06_DEA", "07_Fin" };

        // Le panneau de consignes tourne autour du point de départ pour ne plus cacher le panneau d'entraînement
        private const float RotationPanneauConsignes = 50f;

        [MenuItem("Premiers Gestes/Installer le guidage dans Main")]
        private static void Installer()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Message("Arrête d'abord le mode Play, puis relance ce menu.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "Main")
            {
                Message($"Ouvre la scène Main avant de lancer ce menu (scène ouverte : {scene.name}).");
                return;
            }
            if (Trouver<ScenarioManager>(scene) == null)
            {
                Message("Aucun ScenarioManager dans la scène Main : installation annulée.");
                return;
            }

            var rapport = new StringBuilder();
            Undo.SetCurrentGroupName("Installer le guidage dans Main");
            int groupe = Undo.GetCurrentGroup();

            InstallerGuideVocal(scene, rapport);
            InstallerBilan(scene, rapport);
            AppliquerConfort(scene, rapport);
            PlacerPanneauConsignes(scene, rapport);
            VerifierMenuPoignet(scene, rapport);
            InstallerGestes(scene, rapport);

            Undo.CollapseUndoOperations(groupe);
            EditorSceneManager.MarkSceneDirty(scene);

            rapport.AppendLine().Append("Dernière étape : enregistre la scène avec Ctrl+S.");
            Debug.Log("[Installation du guidage]\n" + rapport);
            Message(rapport.ToString());
        }

        // --- 1. Guide vocal ---

        private static void InstallerGuideVocal(Scene scene, StringBuilder rapport)
        {
            GuideVocal guide = Trouver<GuideVocal>(scene);
            if (guide == null)
            {
                var objet = new GameObject("GuideVocal");
                SceneManager.MoveGameObjectToScene(objet, scene);
                Undo.RegisterCreatedObjectUndo(objet, "Créer le guide vocal");
                objet.AddComponent<AudioSource>();
                guide = objet.AddComponent<GuideVocal>();
                rapport.AppendLine("- Guide vocal : créé (objet « GuideVocal »).");
            }
            else
            {
                rapport.AppendLine("- Guide vocal : déjà présent.");
            }

            Undo.RecordObject(guide, "Voix du guide");
            System.Array.Resize(ref guide.voixParEtape, NomsVoix.Length);
            int trouvees = 0;
            for (int i = 0; i < NomsVoix.Length; i++)
            {
                AudioClip clip = ChargerVoix(NomsVoix[i]);
                if (clip == null) continue;
                guide.voixParEtape[i] = clip;
                trouvees++;
            }
            EditorUtility.SetDirty(guide);
            rapport.AppendLine($"  Voix trouvées dans {DossierVoix} : {trouvees} / {NomsVoix.Length}.");
        }

        private static AudioClip ChargerVoix(string nom)
        {
            foreach (string extension in new[] { ".wav", ".mp3", ".ogg" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{DossierVoix}/{nom}{extension}");
                if (clip != null) return clip;
            }
            return null;
        }

        // --- 2. Bilan ---

        private static void InstallerBilan(Scene scene, StringBuilder rapport)
        {
            BilanController bilan = Trouver<BilanController>(scene);
            if (bilan == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CheminPanneauBilan);
                if (prefab == null)
                {
                    rapport.AppendLine($"- Bilan : prefab introuvable ({CheminPanneauBilan}). Fais un pull de main.");
                    return;
                }

                GameObject panneau = TrouverInstance(scene, prefab);
                if (panneau == null)
                {
                    panneau = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    Undo.RegisterCreatedObjectUndo(panneau, "Créer le panneau de bilan");
                    // Rangé sur le côté : en jeu, il se place tout seul devant le joueur à la fin
                    panneau.transform.position = new Vector3(-3f, 1.5f, 3f);
                }
                bilan = Undo.AddComponent<BilanController>(panneau);
                rapport.AppendLine("- Bilan : panneau ajouté. Il apparaîtra devant le joueur à la fin du scénario.");
            }
            else
            {
                rapport.AppendLine("- Bilan : déjà présent.");
            }

            if (bilan.cameraJoueur == null && Camera.main != null)
            {
                Undo.RecordObject(bilan, "Caméra du bilan");
                bilan.cameraJoueur = Camera.main.transform;
                EditorUtility.SetDirty(bilan);
            }
        }

        private static GameObject TrouverInstance(Scene scene, GameObject prefab)
        {
            foreach (GameObject racine in scene.GetRootGameObjects())
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(racine) == prefab) return racine;
            }
            return null;
        }

        // --- 3. Confort : téléportation seulement, rotation par crans ---

        private static void AppliquerConfort(Scene scene, StringBuilder rapport)
        {
            int changements = 0;
            foreach (MonoBehaviour composant in TrouverTous<MonoBehaviour>(scene))
            {
                if (composant == null) continue; // Script manquant
                if (composant.GetType().Name != "ControllerInputActionManager") continue;
                changements += Regler(composant, "m_SmoothMotionEnabled", false); // Joystick = téléportation
                changements += Regler(composant, "m_SmoothTurnEnabled", false);   // Rotation par crans
            }
            rapport.AppendLine(changements > 0
                ? $"- Confort : déplacement continu coupé, téléportation seulement ({changements} réglage(s) modifié(s))."
                : "- Confort : déjà réglé (téléportation seulement, rotation par crans).");
        }

        private static int Regler(Object composant, string propriete, bool valeur)
        {
            var objet = new SerializedObject(composant);
            SerializedProperty champ = objet.FindProperty(propriete);
            if (champ == null || champ.boolValue == valeur) return 0;
            champ.boolValue = valeur;
            objet.ApplyModifiedProperties();
            return 1;
        }

        // --- 4. Panneau de consignes ---

        private static void PlacerPanneauConsignes(Scene scene, StringBuilder rapport)
        {
            PanneauConsignesController panneau = Trouver<PanneauConsignesController>(scene);
            if (panneau == null)
            {
                rapport.AppendLine("- Panneau de consignes : introuvable.");
                return;
            }

            Transform t = panneau.transform;
            Vector3 angles = t.localEulerAngles;
            if (Mathf.Abs(Mathf.DeltaAngle(angles.y, RotationPanneauConsignes)) < 0.1f)
            {
                rapport.AppendLine("- Panneau de consignes : déjà placé.");
                return;
            }

            Undo.RecordObject(t, "Placer le panneau de consignes");
            t.localEulerAngles = new Vector3(angles.x, RotationPanneauConsignes, angles.z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            rapport.AppendLine($"- Panneau de consignes : tourné de {RotationPanneauConsignes}° vers la droite, il ne cache plus le panneau d'entraînement.");
        }

        // --- 5. Menu au poignet (vérification seulement) ---

        private static void VerifierMenuPoignet(Scene scene, StringBuilder rapport)
        {
            WristMenuController menu = Trouver<WristMenuController>(scene);
            if (menu == null || menu.wristMenu == null)
            {
                rapport.AppendLine("- Menu au poignet : introuvable.");
                return;
            }

            bool boutonRepeter = false;
            foreach (Button bouton in menu.wristMenu.GetComponentsInChildren<Button>(true))
            {
                if (bouton.name == "BoutonRepeter") boutonRepeter = true;
            }
            rapport.AppendLine(boutonRepeter
                ? "- Menu au poignet : OK (ses boutons sont reliés automatiquement par le script)."
                : "- Menu au poignet : bouton « Répéter » absent. Fais un pull de main (PR #39).");
        }

        // --- 6. Les 5 gestes de secours (scripts de la PR nº 41) ---

        private static void InstallerGestes(Scene scene, StringBuilder rapport)
        {
            rapport.AppendLine().AppendLine("Gestes de secours :");
            InstallerZone(scene, rapport);
            Transform victime = InstallerVictime(scene, rapport);
            InstallerTelephone(scene, rapport);
            InstallerDea(scene, victime, rapport);
            AjouterReperes(scene, rapport);
        }

        // Repères visibles : sans eux, le joueur ne voit pas où regarder ou toucher dans le casque
        private static void AjouterReperes(Scene scene, StringBuilder rapport)
        {
            var jaune = new Color(1f, 0.85f, 0.1f);
            var bleu = new Color(0.2f, 0.5f, 1f);
            var rouge = new Color(1f, 0.2f, 0.2f);
            var vert = new Color(0.2f, 0.9f, 0.3f);

            int ajoutes = 0;
            ajoutes += Repere(scene, "Danger_1", "Jaune", jaune, 0.15f);
            ajoutes += Repere(scene, "Danger_2", "Jaune", jaune, 0.15f);
            ajoutes += Repere(scene, "Danger_3", "Jaune", jaune, 0.15f);
            ajoutes += Repere(scene, "Epaule", "Bleu", bleu, 0.08f);
            ajoutes += Repere(scene, "PointCompression", "Rouge", rouge, 0.06f);
            ajoutes += Repere(scene, "EmplacementDroite", "Vert", vert, 0.06f);
            ajoutes += Repere(scene, "EmplacementGauche", "Vert", vert, 0.06f);

            // Couleurs du DEA : bouton Choc rouge, voyant vert
            Colorer(scene, "BoutonChoc", "Rouge", rouge);
            Colorer(scene, "VoyantPret", "Vert", vert);

            rapport.AppendLine(ajoutes > 0
                ? $"- Repères visibles : {ajoutes} ajouté(s) (jaune = danger, bleu = épaule, rouge = poitrine, vert = électrodes)."
                : "- Repères visibles : déjà présents.");
        }

        private static int Repere(Scene scene, string cible, string nomCouleur, Color couleur, float taille)
        {
            GameObject objet = TrouverParNom(scene, cible);
            if (objet == null || objet.transform.Find("Repere") != null) return 0;

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(sphere.GetComponent<Collider>()); // Visuel seulement : ne gêne pas les interactions
            sphere.name = "Repere";
            Undo.RegisterCreatedObjectUndo(sphere, "Ajouter un repère");
            sphere.transform.SetParent(objet.transform, false);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * taille;
            sphere.GetComponent<Renderer>().sharedMaterial = MateriauRepere(nomCouleur, couleur);
            return 1;
        }

        private static void Colorer(Scene scene, string cible, string nomCouleur, Color couleur)
        {
            GameObject objet = TrouverParNom(scene, cible);
            var rendu = objet != null ? objet.GetComponent<Renderer>() : null;
            if (rendu == null) return;
            Undo.RecordObject(rendu, "Colorer " + cible);
            rendu.sharedMaterial = MateriauRepere(nomCouleur, couleur);
        }

        // Un matériau simple (non éclairé) par couleur, enregistré dans Materials/Reperes
        private static Material MateriauRepere(string nomCouleur, Color couleur)
        {
            const string dossier = "Assets/_Project/Materials/Reperes";
            string chemin = $"{dossier}/Repere_{nomCouleur}.mat";
            var materiau = AssetDatabase.LoadAssetAtPath<Material>(chemin);
            if (materiau != null) return materiau;

            if (!AssetDatabase.IsValidFolder(dossier))
                AssetDatabase.CreateFolder("Assets/_Project/Materials", "Reperes");
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            materiau = new Material(shader);
            materiau.SetColor("_BaseColor", couleur);
            materiau.color = couleur;
            AssetDatabase.CreateAsset(materiau, chemin);
            return materiau;
        }

        // Sécuriser la zone : 3 dangers à regarder autour du point de départ
        private static void InstallerZone(Scene scene, StringBuilder rapport)
        {
            if (Trouver<VerifierZone>(scene) != null)
            {
                rapport.AppendLine("- Sécuriser la zone : déjà présent.");
                return;
            }

            GameObject zone = CreerObjet("VerifierZone", null, Vector3.zero);
            var verifier = zone.AddComponent<VerifierZone>();
            verifier.pointsARegarder = new[]
            {
                CreerObjet("Danger_1", zone.transform, new Vector3(-3f, 1f, 2f)).transform,
                CreerObjet("Danger_2", zone.transform, new Vector3(3f, 1f, 2f)).transform,
                CreerObjet("Danger_3", zone.transform, new Vector3(0f, 1f, -3f)).transform,
            };
            rapport.AppendLine("- Sécuriser la zone : ajouté (3 points Danger_1 à 3, à déplacer sur de vrais dangers).");
        }

        // Vérifier la victime, RCR et emplacements des électrodes : tout est regroupé dans « InteractionsVictime »
        private static Transform InstallerVictime(Scene scene, StringBuilder rapport)
        {
            GameObject existant = TrouverParNom(scene, "InteractionsVictime");
            if (existant != null)
            {
                rapport.AppendLine("- Victime (épaule, RCR, emplacements) : déjà présent.");
                return existant.transform;
            }

            Transform modele = TrouverParNom(scene, "Victime")?.transform ?? TrouverParNom(scene, "Ch01_nonPBR")?.transform;
            Vector3 position = modele != null ? modele.position : new Vector3(-2f, 0f, 2f);
            GameObject racine = CreerObjet("InteractionsVictime", null, position);
            Transform t = racine.transform;

            // Épaule : toucher pour vérifier si la victime réagit
            GameObject epaule = CreerObjet("Epaule", t, new Vector3(0.2f, 0.2f, 0.45f));
            epaule.AddComponent<SphereCollider>().radius = 0.12f;
            epaule.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            var valider = epaule.AddComponent<ValiderEtapeInteraction>();
            valider.etape = ScenarioStepId.CheckVictim;

            // RCR : point de compression au centre de la poitrine, et compteur relié aux manettes
            GameObject point = CreerObjet("PointCompression", t, new Vector3(0f, 0.25f, 0.2f));
            var compteur = CreerObjet("RCR", t, Vector3.zero).AddComponent<CompteurCompressions>();
            compteur.pointCompression = point.transform;
            compteur.mainGauche = TrouverParNom(scene, "Left Controller")?.transform;
            compteur.mainDroite = TrouverParNom(scene, "Right Controller")?.transform;

            // Emplacements des électrodes : sous la clavicule droite, et côté gauche sous l'aisselle
            CreerEmplacement("EmplacementDroite", t, new Vector3(0.12f, 0.25f, 0.35f));
            CreerEmplacement("EmplacementGauche", t, new Vector3(-0.15f, 0.22f, 0.1f));

            rapport.AppendLine(modele != null
                ? $"- Victime : épaule, RCR et emplacements ajoutés autour de « {modele.name} ». Ajuste « InteractionsVictime » quand elle sera couchée."
                : "- Victime : modèle introuvable, gestes placés à un endroit provisoire (objet « InteractionsVictime »).");
            if (compteur.mainGauche == null || compteur.mainDroite == null)
                rapport.AppendLine("  Attention : manettes introuvables, relie « Main Gauche / Main Droite » du RCR à la main.");
            return t;
        }

        private static void CreerEmplacement(string nom, Transform parent, Vector3 position)
        {
            GameObject emplacement = CreerObjet(nom, parent, position);
            var zone = emplacement.AddComponent<SphereCollider>();
            zone.isTrigger = true;
            zone.radius = 0.1f;
            emplacement.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        }

        // Alerter : prendre le téléphone déclenche l'appel au 911
        private static void InstallerTelephone(Scene scene, StringBuilder rapport)
        {
            if (Trouver<TelephoneAppel>(scene) != null)
            {
                rapport.AppendLine("- Téléphone : déjà présent.");
                return;
            }

            GameObject telephone = TrouverParNom(scene, "Telephone_VR");
            if (telephone == null)
            {
                rapport.AppendLine("- Téléphone : « Telephone_VR » introuvable dans la scène.");
                return;
            }

            Undo.AddComponent<TelephoneAppel>(telephone); // Ajoute aussi XR Grab Interactable et Rigidbody
            var corps = telephone.GetComponent<Rigidbody>();
            if (corps != null)
            {
                corps.useGravity = false;   // Reste où on le lâche, ne tombe pas à travers le décor
                corps.isKinematic = true;
            }
            rapport.AppendLine("- Téléphone : on peut le prendre, l'appel au 911 démarre.");
        }

        // DEA : 2 électrodes à poser, analyse, puis bouton Choc
        private static void InstallerDea(Scene scene, Transform victime, StringBuilder rapport)
        {
            if (Trouver<DeaController>(scene) != null)
            {
                rapport.AppendLine("- DEA : déjà présent.");
                return;
            }

            GameObject modele = TrouverParNom(scene, "DEA");
            Vector3 position = modele != null ? modele.transform.position + new Vector3(0f, 0.15f, 0f) : new Vector3(-1.5f, 0.3f, 2f);
            GameObject racine = CreerObjet("DEAInteractif", null, position);
            Transform t = racine.transform;

            CreerCube("Electrode_1", t, new Vector3(-0.06f, 0f, 0f), new Vector3(0.08f, 0.01f, 0.12f), true);
            CreerCube("Electrode_2", t, new Vector3(0.06f, 0f, 0f), new Vector3(0.08f, 0.01f, 0.12f), true);
            GameObject bouton = CreerCube("BoutonChoc", t, new Vector3(0f, 0f, 0.12f), new Vector3(0.05f, 0.03f, 0.05f), false);
            GameObject voyant = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            voyant.name = "VoyantPret";
            voyant.transform.SetParent(t, false);
            voyant.transform.localPosition = new Vector3(0f, 0.03f, -0.1f);
            voyant.transform.localScale = Vector3.one * 0.03f;
            Object.DestroyImmediate(voyant.GetComponent<Collider>());
            voyant.SetActive(false);

            var son = racine.AddComponent<AudioSource>();
            son.playOnAwake = false;
            son.spatialBlend = 1f;

            var dea = racine.AddComponent<DeaController>();
            dea.boutonChoc = bouton.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            dea.voyantPret = voyant;
            dea.hautParleur = son;
            if (victime != null)
            {
                dea.emplacements = new[]
                {
                    victime.Find("EmplacementDroite")?.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>(),
                    victime.Find("EmplacementGauche")?.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>(),
                };
            }

            rapport.AppendLine(modele != null
                ? "- DEA : 2 électrodes, bouton Choc et voyant ajoutés sur le DEA (objet « DEAInteractif »)."
                : "- DEA : modèle « DEA » introuvable, placé à un endroit provisoire (objet « DEAInteractif »).");
        }

        private static GameObject CreerCube(string nom, Transform parent, Vector3 position, Vector3 taille, bool attrapable)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = nom;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = taille;
            if (attrapable)
            {
                cube.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                var corps = cube.GetComponent<Rigidbody>();
                if (corps != null)
                {
                    corps.useGravity = false;
                    corps.isKinematic = true;
                }
            }
            else
            {
                cube.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            }
            return cube;
        }

        private static GameObject CreerObjet(string nom, Transform parent, Vector3 position)
        {
            var objet = new GameObject(nom);
            if (parent == null)
            {
                SceneManager.MoveGameObjectToScene(objet, SceneManager.GetActiveScene());
                Undo.RegisterCreatedObjectUndo(objet, "Créer " + nom);
                objet.transform.position = position;
            }
            else
            {
                objet.transform.SetParent(parent, false);
                objet.transform.localPosition = position;
            }
            return objet;
        }

        private static GameObject TrouverParNom(Scene scene, string nom)
        {
            foreach (GameObject racine in scene.GetRootGameObjects())
            {
                foreach (Transform enfant in racine.GetComponentsInChildren<Transform>(true))
                {
                    if (enfant.name == nom) return enfant.gameObject;
                }
            }
            return null;
        }

        // --- Outils ---

        private static T Trouver<T>(Scene scene) where T : Component
        {
            foreach (GameObject racine in scene.GetRootGameObjects())
            {
                T composant = racine.GetComponentInChildren<T>(true);
                if (composant != null) return composant;
            }
            return null;
        }

        private static List<T> TrouverTous<T>(Scene scene) where T : Component
        {
            var resultats = new List<T>();
            foreach (GameObject racine in scene.GetRootGameObjects())
                resultats.AddRange(racine.GetComponentsInChildren<T>(true));
            return resultats;
        }

        private static void Message(string texte)
        {
            EditorUtility.DisplayDialog("Premiers Gestes", texte, "OK");
        }
    }
}
