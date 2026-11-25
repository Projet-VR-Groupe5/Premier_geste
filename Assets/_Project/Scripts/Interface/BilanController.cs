using PremiersGestes.Scenario;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PremiersGestes.Interface
{
    /// <summary>
    /// Écran de bilan, à placer sur le prefab PanneauBilan.
    /// - Caché pendant le scénario, il apparaît devant le joueur à l'étape Fin.
    /// - Affiche le temps total, les étapes terminées et le nombre de recommencements.
    /// - « Recommencer » relance le scénario (sans l'entraînement), « Accueil » revient au début.
    /// Les textes et les boutons sont trouvés par leur nom : TexteTemps, TexteEtapes, TexteCompressions,
    /// TexteDemonstration, BoutonRecommencer, BoutonAccueil.
    /// </summary>
    public class BilanController : MonoBehaviour
    {
        [Tooltip("Ce qu'on affiche ou cache. Vide = le Canvas enfant de cet objet.")]
        public GameObject panneau;

        [Tooltip("La caméra du joueur. Vide = la caméra principale (Main Camera).")]
        public Transform cameraJoueur;

        [Tooltip("Distance entre le joueur et le bilan quand il apparaît (en mètres).")]
        [Range(0.8f, 3f)]
        public float distance = 1.5f;

        private const int EtapesDuScenario = 5; // Sécuriser, Vérifier, Alerter, RCR, DEA

        private TMP_Text texteTemps;
        private TMP_Text texteEtapes;
        private TMP_Text texteCompressions;
        private TMP_Text texteDemonstration;

        private float debut = -1f;
        private int recommencements;
        private ScenarioStepId? derniereEtape;

        private void Awake()
        {
            if (panneau == null)
            {
                Canvas canvas = GetComponentInChildren<Canvas>(true);
                panneau = canvas != null ? canvas.gameObject : gameObject;
            }

            texteTemps = TrouverTexte("TexteTemps");
            texteEtapes = TrouverTexte("TexteEtapes");
            texteCompressions = TrouverTexte("TexteCompressions");
            texteDemonstration = TrouverTexte("TexteDemonstration");
            Relier("BoutonRecommencer", Rejouer);
            Relier("BoutonAccueil", RetourAccueil);

            if (panneau != gameObject)
                panneau.SetActive(false); // Caché jusqu'à la fin du scénario
        }

        private void OnEnable()
        {
            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.StepChanged += OnStepChanged;
        }

        private void OnDisable()
        {
            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.StepChanged -= OnStepChanged;
        }

        private void OnStepChanged(ScenarioStepId step)
        {
            bool pendantLeScenario = step != ScenarioStepId.Welcome && step != ScenarioStepId.End;

            // Nouvelle partie : retour à l'accueil, ou on rejoue après le bilan
            if (step == ScenarioStepId.Welcome || derniereEtape == ScenarioStepId.End)
            {
                debut = -1f;
                recommencements = 0;
            }

            // Même étape relancée = un recommencement (bouton « Recommencer l'étape »)
            if (pendantLeScenario && derniereEtape == step)
                recommencements++;

            // Le chronomètre part quand le scénario commence vraiment
            if (pendantLeScenario && debut < 0f)
                debut = Time.time;

            derniereEtape = step;

            if (step == ScenarioStepId.End)
                Afficher();
            else if (panneau != gameObject)
                panneau.SetActive(false);
        }

        private void Afficher()
        {
            float duree = debut < 0f ? 0f : Time.time - debut;
            int minutes = Mathf.FloorToInt(duree / 60f);
            int secondes = Mathf.FloorToInt(duree % 60f);

            Ecrire(texteTemps, $"Temps total : {minutes} min {secondes:00} s");
            Ecrire(texteEtapes, $"Étapes terminées : {EtapesDuScenario} / {EtapesDuScenario}" +
                                $"\nRecommencements : {recommencements}");
            if (texteDemonstration != null)
                texteDemonstration.gameObject.SetActive(false); // Ce sont maintenant de vraies valeurs

            PlacerDevantLeJoueur();
            panneau.SetActive(true);
        }

        /// <summary>Pour plus tard : la qualité des compressions mesurée pendant la RCR.</summary>
        public void AfficherCompressions(string resultat)
        {
            Ecrire(texteCompressions, $"Qualité des compressions : {resultat}");
        }

        public void Rejouer()
        {
            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.GoToStep(ScenarioStepId.SecureArea);
        }

        public void RetourAccueil()
        {
            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.BackToHome();
        }

        // Le bilan apparaît face au joueur, à hauteur des yeux, là où il regarde
        private void PlacerDevantLeJoueur()
        {
            Transform cam = cameraJoueur;
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            if (cam == null) return;

            Vector3 devant = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
            if (devant.sqrMagnitude < 0.001f)
                devant = Vector3.ProjectOnPlane(cam.up, Vector3.up); // Le joueur regarde droit vers le bas
            devant.Normalize();

            Vector3 position = cam.position + devant * distance + Vector3.down * 0.1f;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(devant));
        }

        private static void Ecrire(TMP_Text texte, string contenu)
        {
            if (texte != null) texte.text = contenu;
        }

        private TMP_Text TrouverTexte(string nom)
        {
            foreach (TMP_Text texte in GetComponentsInChildren<TMP_Text>(true))
            {
                if (texte.name == nom) return texte;
            }
            Debug.LogWarning($"[Bilan] Texte « {nom} » introuvable.", this);
            return null;
        }

        private void Relier(string nom, UnityAction action)
        {
            foreach (Button bouton in GetComponentsInChildren<Button>(true))
            {
                if (bouton.name != nom) continue;
                bouton.onClick = new Button.ButtonClickedEvent();
                bouton.onClick.AddListener(action);
                return;
            }
            Debug.LogWarning($"[Bilan] Bouton « {nom} » introuvable.", this);
        }
    }
}
