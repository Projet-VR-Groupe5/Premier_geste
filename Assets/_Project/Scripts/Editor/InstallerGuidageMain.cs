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
            CoucherVictime(scene, rapport);
            AnimerTemoins(scene, rapport);
            BrancherVoix911(scene, rapport);
            AlignerParc(scene, rapport);
            AjusterTaillesEtSol(scene, rapport);
            PlacerGestesSurLeCorps(scene, rapport);
            AjouterReperes(scene, rapport);
        }

        // --- 9. Zones des gestes placées sur le squelette de la victime ---

        private static void PlacerGestesSurLeCorps(Scene scene, StringBuilder rapport)
        {
            GameObject victime = TrouverParNom(scene, "Victime");
            GameObject gestes = TrouverParNom(scene, "InteractionsVictime");
            if (victime == null || gestes == null) return;

            Transform hanches = Os(victime, HumanBodyBones.Hips, "Hips");
            Transform poitrine = Os(victime, HumanBodyBones.Chest, "Spine1");
            Transform hautPoitrine = Os(victime, HumanBodyBones.UpperChest, "Spine2");
            Transform cou = Os(victime, HumanBodyBones.Neck, "Neck");
            Transform epauleDroite = Os(victime, HumanBodyBones.RightUpperArm, "RightArm");
            Transform epauleGauche = Os(victime, HumanBodyBones.LeftUpperArm, "LeftArm");
            if (hanches == null || poitrine == null || hautPoitrine == null || cou == null || epauleDroite == null || epauleGauche == null)
            {
                rapport.AppendLine("- Zones sur le corps : squelette de la victime introuvable, positions inchangées.");
                return;
            }

            Vector3 devant = victime.transform.forward;                         // Vers le ciel : elle est couchée sur le dos
            Vector3 versPieds = (hanches.position - cou.position).normalized;   // Le long du corps, vers les pieds

            int deplacees = 0;
            // Bas du sternum, à la surface de la poitrine : là où on pose les mains pour la RCR
            deplacees += PlacerMonde(gestes.transform, "PointCompression",
                Vector3.Lerp(poitrine.position, hautPoitrine.position, 0.4f) + devant * 0.11f);
            // Épaule droite : là où on tape pour vérifier si la victime réagit
            deplacees += PlacerMonde(gestes.transform, "Epaule", epauleDroite.position + devant * 0.06f);
            // Électrode 1 : sous la clavicule droite
            deplacees += PlacerMonde(gestes.transform, "EmplacementDroite",
                Vector3.Lerp(hautPoitrine.position, epauleDroite.position, 0.5f) + versPieds * 0.05f + devant * 0.1f);
            // Électrode 2 : côté gauche, sous l'aisselle
            deplacees += PlacerMonde(gestes.transform, "EmplacementGauche",
                epauleGauche.position + versPieds * 0.15f + devant * 0.04f);

            rapport.AppendLine(deplacees > 0
                ? $"- Zones sur le corps : {deplacees} placée(s) sur le squelette (poitrine, épaule droite, électrodes)."
                : "- Zones sur le corps : déjà bien placées.");
        }

        // Os du squelette : d'abord par l'avatar Humanoid, sinon par le nom Mixamo (ex. « mixamorig12:Spine1 »)
        private static Transform Os(GameObject modele, HumanBodyBones os, string nomMixamo)
        {
            Animator animateur = modele.GetComponent<Animator>();
            if (animateur != null && animateur.avatar != null && animateur.avatar.isHuman)
            {
                Transform trouve = animateur.GetBoneTransform(os);
                if (trouve != null) return trouve;
            }
            foreach (Transform enfant in modele.GetComponentsInChildren<Transform>(true))
            {
                if (enfant.name == nomMixamo || enfant.name.EndsWith(":" + nomMixamo)) return enfant;
            }
            return null;
        }

        private static int PlacerMonde(Transform parent, string nom, Vector3 position)
        {
            Transform enfant = parent.Find(nom);
            if (enfant == null || Vector3.Distance(enfant.position, position) < 0.01f) return 0;
            Undo.RecordObject(enfant, "Placer " + nom);
            enfant.position = position;
            return 1;
        }

        // --- 7 bis. Alignement du parc : l'allée du parc à la hauteur du sol du joueur ---

        // Forme 3D du parc (sans collisions) : on mesure sa surface directement sur ses triangles
        private class MaillageParc
        {
            public Vector3[] sommets; // En coordonnées du monde
            public int[] triangles;
            public Bounds limites;
        }

        private static List<MaillageParc> maillagesParc;

        private static void PreparerParc(Scene scene)
        {
            maillagesParc = new List<MaillageParc>();
            GameObject parc = TrouverParNom(scene, "parco con fontana");
            if (parc == null) return;

            foreach (MeshFilter filtre in parc.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh maillage = filtre.sharedMesh;
                if (maillage == null) continue;
                Matrix4x4 versMonde = filtre.transform.localToWorldMatrix;
                Vector3[] locaux = maillage.vertices;
                var monde = new Vector3[locaux.Length];
                for (int i = 0; i < locaux.Length; i++) monde[i] = versMonde.MultiplyPoint3x4(locaux[i]);
                if (monde.Length == 0) continue;
                var limites = new Bounds(monde[0], Vector3.zero);
                foreach (Vector3 point in monde) limites.Encapsulate(point);
                maillagesParc.Add(new MaillageParc { sommets = monde, triangles = maillage.triangles, limites = limites });
            }
        }

        // Hauteur de la plus haute surface plate du parc sous le point (x, z), en dessous de « plafond »
        private static float? HauteurParc(float x, float z, float plafond)
        {
            if (maillagesParc == null) return null;
            float meilleure = float.NegativeInfinity;
            foreach (MaillageParc m in maillagesParc)
            {
                if (x < m.limites.min.x || x > m.limites.max.x || z < m.limites.min.z || z > m.limites.max.z) continue;
                Vector3[] s = m.sommets;
                int[] tri = m.triangles;
                for (int i = 0; i + 2 < tri.Length; i += 3)
                {
                    Vector3 a = s[tri[i]], b = s[tri[i + 1]], c = s[tri[i + 2]];
                    if (x < Mathf.Min(a.x, b.x, c.x) || x > Mathf.Max(a.x, b.x, c.x)) continue;
                    if (z < Mathf.Min(a.z, b.z, c.z) || z > Mathf.Max(a.z, b.z, c.z)) continue;
                    Vector3 normale = Vector3.Cross(b - a, c - a);
                    if (normale.sqrMagnitude < 1e-12f || Mathf.Abs(normale.normalized.y) < 0.7f) continue; // Pas une surface plate

                    float d = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                    if (Mathf.Abs(d) < 1e-9f) continue;
                    float l1 = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / d;
                    float l2 = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / d;
                    float l3 = 1f - l1 - l2;
                    if (l1 < 0f || l2 < 0f || l3 < 0f) continue; // Le point n'est pas au-dessus de ce triangle

                    float y = l1 * a.y + l2 * b.y + l3 * c.y;
                    if (y < plafond && y > meilleure) meilleure = y;
                }
            }
            return float.IsNegativeInfinity(meilleure) ? (float?)null : meilleure;
        }

        private static void AlignerParc(Scene scene, StringBuilder rapport)
        {
            GameObject decor = TrouverParNom(scene, "Decor");
            PreparerParc(scene);
            if (decor == null || maillagesParc.Count == 0)
            {
                rapport.AppendLine("- Parc : modèle « parco con fontana » introuvable, hauteur inchangée.");
                return;
            }

            var origine = Trouver<Unity.XR.CoreUtils.XROrigin>(scene);
            Vector3 depart = origine != null ? origine.transform.position : Vector3.zero;
            float solJoueur = depart.y;

            float? allee = HauteurParc(depart.x, depart.z, solJoueur + 0.5f);
            if (allee == null)
            {
                rapport.AppendLine("- Parc : pas d'allée trouvée sous le point de départ, hauteur inchangée.");
                return;
            }

            float ecart = allee.Value - solJoueur;
            if (Mathf.Abs(ecart) < 0.02f)
            {
                rapport.AppendLine("- Parc : déjà aligné sur le sol du joueur.");
                return;
            }

            Undo.RecordObject(decor.transform, "Aligner le parc");
            decor.transform.position -= new Vector3(0f, ecart, 0f); // Tout le décor bouge ensemble
            Physics.SyncTransforms();
            PreparerParc(scene); // Nouvelle position du parc pour les mesures suivantes
            rapport.AppendLine($"- Parc : {(ecart < 0f ? "remonté" : "descendu")} de {Mathf.Abs(ecart):0.00} m, l'allée est maintenant au niveau du sol du joueur.");
        }

        // --- 8. Tailles réelles et sol : victime posée sur le sol, téléphone et DEA à la bonne taille ---

        private const float TailleTelephone = 0.16f; // Un smartphone : environ 16 cm
        private const float TailleDea = 0.30f;       // Un vrai défibrillateur : environ 30 cm

        private static void AjusterTaillesEtSol(Scene scene, StringBuilder rapport)
        {
            Physics.SyncTransforms();

            GameObject victime = TrouverParNom(scene, "Victime");
            if (victime != null)
                rapport.AppendLine(PoserAuSol(victime)
                    ? "- Victime : posée sur le sol (elle n'est plus enterrée)."
                    : "- Victime : déjà sur le sol.");

            int temoinsPoses = 0;
            foreach (string nom in new[] { "Temoin1", "Temoin2" })
            {
                GameObject temoin = TrouverParNom(scene, nom);
                if (temoin != null && PoserAuSol(temoin)) temoinsPoses++;
            }
            if (temoinsPoses > 0) rapport.AppendLine($"- Témoins : {temoinsPoses} reposé(s) sur l'allée.");

            GameObject telephone = TrouverParNom(scene, "Telephone_VR");
            if (telephone != null)
            {
                bool taille = Redimensionner(telephone, TailleTelephone);
                bool sol = PoserAuSol(telephone);
                rapport.AppendLine(taille || sol
                    ? $"- Téléphone : ramené à environ {TailleTelephone * 100f:0} cm et posé sur ce qu'il y a dessous."
                    : "- Téléphone : taille et position déjà bonnes.");
            }

            GameObject dea = TrouverParNom(scene, "DEA");
            if (dea != null)
            {
                bool taille = Redimensionner(dea, TailleDea);
                bool sol = PoserAuSol(dea);
                bool dessus = PlacerSurLeDea(scene, dea);
                rapport.AppendLine(taille || sol || dessus
                    ? $"- DEA : ramené à environ {TailleDea * 100f:0} cm, posé au sol, électrodes et bouton Choc replacés dessus."
                    : "- DEA : taille et position déjà bonnes.");
            }
        }

        // Change l'échelle pour que la plus grande dimension vaille « cible » (sans bouger le centre)
        private static bool Redimensionner(GameObject objet, float cible)
        {
            if (!Limites(objet, out Bounds avant)) return false;
            float taille = Mathf.Max(avant.size.x, avant.size.y, avant.size.z);
            if (taille <= 0f || Mathf.Abs(taille - cible) / cible < 0.2f) return false; // Déjà la bonne taille

            Transform t = objet.transform;
            Undo.RecordObject(t, "Redimensionner " + objet.name);
            t.localScale *= cible / taille;
            if (Limites(objet, out Bounds apres))
            {
                Vector3 decalage = avant.center - apres.center;
                t.position += new Vector3(decalage.x, 0f, decalage.z); // Reste au même endroit
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            Physics.SyncTransforms();
            return true;
        }

        // Pose le point le plus bas de l'objet sur ce qu'il y a juste en dessous (sol, banc, table…)
        private static bool PoserAuSol(GameObject objet)
        {
            if (!Limites(objet, out Bounds limites)) return false;
            float ecart = limites.min.y - HauteurSous(objet, limites);
            if (Mathf.Abs(ecart) < 0.03f) return false; // Déjà posé

            Transform t = objet.transform;
            Undo.RecordObject(t, "Poser " + objet.name);
            t.position -= new Vector3(0f, ecart, 0f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            Physics.SyncTransforms();
            return true;
        }

        // Hauteur de la surface sous l'objet : la plus haute entre la forme du parc et les collisions
        // (sol du joueur, tables…) ; à défaut, le sol du joueur (Y = 0)
        private static float HauteurSous(GameObject objet, Bounds limites)
        {
            float plafond = limites.min.y + 0.3f; // Au-dessus : ce n'est pas une surface sous l'objet
            float? meilleure = HauteurParc(limites.center.x, limites.center.z, plafond);

            var depart = new Vector3(limites.center.x, limites.max.y + 0.05f, limites.center.z);
            RaycastHit[] touches = Physics.RaycastAll(depart, Vector3.down, limites.size.y + 5f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(touches, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit touche in touches)
            {
                Transform t = touche.collider.transform;
                if (t.IsChildOf(objet.transform)) continue;                    // L'objet lui-même
                if (t.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null) continue; // Le joueur
                if (touche.point.y > plafond) continue;
                if (meilleure == null || touche.point.y > meilleure.Value) meilleure = touche.point.y;
                break;
            }
            return meilleure ?? 0f;
        }

        // Électrodes, bouton Choc et voyant sur le dessus du DEA
        private static bool PlacerSurLeDea(Scene scene, GameObject dea)
        {
            GameObject pieces = TrouverParNom(scene, "DEAInteractif");
            if (pieces == null || !Limites(dea, out Bounds limites)) return false;

            var cible = new Vector3(limites.center.x, limites.max.y + 0.01f, limites.center.z);
            if (Vector3.Distance(pieces.transform.position, cible) < 0.02f) return false;
            Undo.RecordObject(pieces.transform, "Placer les pièces du DEA");
            pieces.transform.position = cible;
            return true;
        }

        // Boîte englobante précise de la forme visible (sans les repères ni les gestes ajoutés)
        private static bool Limites(GameObject objet, out Bounds limites)
        {
            limites = new Bounds();
            bool trouve = false;
            foreach (Renderer rendu in objet.GetComponentsInChildren<Renderer>(true))
            {
                if (EstAjoute(rendu.transform, objet.transform)) continue;
                foreach (Vector3 point in Sommets(rendu))
                {
                    if (!trouve) { limites = new Bounds(point, Vector3.zero); trouve = true; }
                    else limites.Encapsulate(point);
                }
            }
            return trouve;
        }

        // Ignore ce que l'outil a ajouté dans le modèle : repères, InteractionsVictime, DEAInteractif
        private static bool EstAjoute(Transform t, Transform racine)
        {
            for (Transform courant = t; courant != null && courant != racine; courant = courant.parent)
            {
                if (courant.name == "Repere" || courant.name == "InteractionsVictime" || courant.name == "DEAInteractif") return true;
            }
            return false;
        }

        // Sommets en coordonnées du monde (forme réelle, y compris un personnage couché)
        private static IEnumerable<Vector3> Sommets(Renderer rendu)
        {
            var points = new List<Vector3>();
            try
            {
                if (rendu is SkinnedMeshRenderer peau && peau.sharedMesh != null)
                {
                    var maillage = new Mesh();
                    peau.BakeMesh(maillage, true); // Échelle incluse : il reste la position et la rotation
                    Transform t = peau.transform;
                    foreach (Vector3 v in maillage.vertices) points.Add(t.position + t.rotation * v);
                    Object.DestroyImmediate(maillage);
                }
                else if (rendu.TryGetComponent(out MeshFilter filtre) && filtre.sharedMesh != null)
                {
                    Matrix4x4 versMonde = rendu.transform.localToWorldMatrix;
                    foreach (Vector3 v in filtre.sharedMesh.vertices) points.Add(versMonde.MultiplyPoint3x4(v));
                }
            }
            catch (System.Exception)
            {
                points.Clear();
            }

            if (points.Count == 0)
            {
                // Secours : la boîte calculée par Unity (un peu moins précise)
                Bounds b = rendu.bounds;
                points.Add(b.min);
                points.Add(b.max);
            }
            return points;
        }

        // --- 7. Travail d'Ilyas (PR nº 44) : victime couchée, témoins animés, voix du 911 ---

        // Pose de la victime dans la scène d'Ilyas : sur le dos, la tête vers +X
        private static readonly Vector3 PoseCouchee = new Vector3(-90f, 0f, -90f);

        private static void CoucherVictime(Scene scene, StringBuilder rapport)
        {
            GameObject victime = TrouverParNom(scene, "Victime");
            if (victime == null) victime = TrouverParNom(scene, "Ch01_nonPBR");
            if (victime == null)
            {
                rapport.AppendLine("- Victime couchée : modèle introuvable (Victime ou Ch01_nonPBR).");
                return;
            }

            Transform t = victime.transform;
            bool dejaCouchee = Vector3.Angle(t.up, Vector3.up) > 45f;
            if (!dejaCouchee)
            {
                Undo.RecordObject(t, "Coucher la victime");
                t.localEulerAngles = PoseCouchee;
                t.localPosition += new Vector3(0f, 0.14f, 0f); // Ne s'enfonce pas dans le sol
                PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            }
            Renommer(victime, "Victime");

            // Les gestes deviennent des enfants de la victime : ils la suivent si elle bouge
            GameObject gestes = TrouverParNom(scene, "InteractionsVictime");
            bool dejaAlignes = gestes != null && gestes.transform.parent == t;
            if (gestes != null && !dejaAlignes)
            {
                Transform g = gestes.transform;
                Undo.SetTransformParent(g, t, "Placer les gestes sur la victime");
                Undo.RecordObject(g, "Placer les gestes sur la victime");
                g.localPosition = Vector3.zero;
                g.localRotation = Quaternion.identity;
                Vector3 echelle = t.lossyScale;
                g.localScale = new Vector3(1f / echelle.x, 1f / echelle.y, 1f / echelle.z);

                // Positions dans le repère du corps : X = côté droit, Y = vers la tête, Z = devant (vers le ciel une fois couchée)
                Placer(g, "Epaule", new Vector3(0.18f, 1.42f, 0.05f));
                Placer(g, "PointCompression", new Vector3(0f, 1.25f, 0.13f));
                Placer(g, "EmplacementDroite", new Vector3(0.1f, 1.35f, 0.12f));
                Placer(g, "EmplacementGauche", new Vector3(-0.17f, 1.18f, 0.07f));
                Placer(g, "RCR", Vector3.zero);
            }

            rapport.AppendLine(dejaCouchee && dejaAlignes
                ? "- Victime couchée : déjà fait."
                : "- Victime couchée sur le dos (pose d'Ilyas) ; épaule, poitrine et électrodes placées sur son corps (à ajuster si besoin).");
        }

        private static void Placer(Transform parent, string nom, Vector3 position)
        {
            Transform enfant = parent.Find(nom);
            if (enfant == null) return;
            Undo.RecordObject(enfant, "Placer " + nom);
            enfant.localPosition = position;
            enfant.localRotation = Quaternion.identity;
        }

        private static void AnimerTemoins(Scene scene, StringBuilder rapport)
        {
            var controleur = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Project/Animation/Temoin1.controller");
            if (controleur == null)
            {
                rapport.AppendLine("- Témoins : animation Temoin1 introuvable (Assets/_Project/Animation).");
                return;
            }

            int animes = Animer(scene, "Temoin1", "Ch16_nonPBR", controleur) + Animer(scene, "Temoin2", "Ch42_nonPBR", controleur);
            rapport.AppendLine(animes > 0
                ? $"- Témoins : {animes} animé(s) avec l'animation Idle d'Ilyas (renommés Temoin1 et Temoin2)."
                : "- Témoins : déjà animés.");
        }

        private static int Animer(Scene scene, string nouveauNom, string ancienNom, RuntimeAnimatorController controleur)
        {
            GameObject temoin = TrouverParNom(scene, nouveauNom);
            if (temoin == null) temoin = TrouverParNom(scene, ancienNom);
            if (temoin == null) return 0;
            Renommer(temoin, nouveauNom);

            Animator animateur = temoin.GetComponent<Animator>();
            if (animateur == null || animateur.runtimeAnimatorController == controleur) return 0;
            Undo.RecordObject(animateur, "Animer " + nouveauNom);
            animateur.runtimeAnimatorController = controleur;
            animateur.applyRootMotion = false; // Reste sur place
            PrefabUtility.RecordPrefabInstancePropertyModifications(animateur);
            return 1;
        }

        private static void BrancherVoix911(Scene scene, StringBuilder rapport)
        {
            TelephoneAppel telephone = Trouver<TelephoneAppel>(scene);
            var voix = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Voix911.mp3");
            if (telephone == null || voix == null)
            {
                rapport.AppendLine("- Voix du 911 : téléphone ou fichier Voix911.mp3 introuvable.");
                return;
            }

            bool vide = telephone.repliques == null || telephone.repliques.Length == 0
                        || System.Array.TrueForAll(telephone.repliques, replique => replique == null);
            if (!vide)
            {
                rapport.AppendLine("- Voix du 911 : le téléphone a déjà ses répliques.");
                return;
            }

            Undo.RecordObject(telephone, "Voix du 911");
            telephone.repliques = new[] { voix };
            EditorUtility.SetDirty(telephone);
            rapport.AppendLine("- Voix du 911 : Voix911.mp3 (Ilyas) jouée par le téléphone quand on décroche.");
        }

        private static void Renommer(GameObject objet, string nom)
        {
            if (objet.name == nom) return;
            Undo.RecordObject(objet, "Renommer " + nom);
            objet.name = nom;
            PrefabUtility.RecordPrefabInstancePropertyModifications(objet);
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
