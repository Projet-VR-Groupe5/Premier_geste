using System.Collections.Generic;
using System.Text;
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
