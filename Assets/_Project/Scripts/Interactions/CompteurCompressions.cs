using PremiersGestes.Interface;
using PremiersGestes.Scenario;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace PremiersGestes.Interactions
{
    /// <summary>
    /// Étape « RCR » (Cpr) : compte les compressions quand le joueur pousse avec les mains sur la poitrine de la victime.
    /// Mesure la profondeur (mouvement vers le bas des manettes) et le rythme (compressions par minute).
    /// Après 30 compressions, l'étape est terminée et le résultat est envoyé au bilan.
    /// Touche de test : F8 = une compression parfaite.
    /// </summary>
    public class CompteurCompressions : MonoBehaviour
    {
        [Header("Références")]
        [Tooltip("Objet vide placé à la surface de la poitrine, au centre (entre les seins).")]
        public Transform pointCompression;
        [Tooltip("XR Origin > Camera Offset > Left Controller")]
        public Transform mainGauche;
        [Tooltip("XR Origin > Camera Offset > Right Controller")]
        public Transform mainDroite;
        [Tooltip("Optionnel : texte près de la victime qui affiche le compteur et le rythme.")]
        public TMP_Text affichage;

        [Header("Réglages")]
        public int compressionsAFaire = 30;
        [Tooltip("Distance horizontale maximale entre la main et le point de compression (en mètres).")]
        public float rayon = 0.2f;
        [Tooltip("Hauteur maximale de la main au-dessus du point (en mètres).")]
        public float hauteurMax = 0.4f;
        [Tooltip("Mouvement vers le bas minimal pour compter une compression (en mètres).")]
        public float seuil = 0.03f;
        [Tooltip("Profondeur d'une bonne compression (en mètres) : au moins 5 cm.")]
        public float profondeurCible = 0.05f;
        public float rythmeMin = 100f;
        public float rythmeMax = 120f;

        private int total;
        private int bonnes;
        private float derniereCompression = -1f;
        private float sommeIntervalles;
        private int nombreIntervalles;
        private bool enfonce;
        private float haut = float.NaN;
        private float bas;

        private void OnEnable()
        {
            if (ScenarioManager.Instance != null) ScenarioManager.Instance.StepChanged += OnStepChanged;
        }

        private void OnDisable()
        {
            if (ScenarioManager.Instance != null) ScenarioManager.Instance.StepChanged -= OnStepChanged;
        }

        private void OnStepChanged(ScenarioStepId step)
        {
            if (step == ScenarioStepId.Cpr) Reinitialiser(); // Au début ou si on recommence l'étape
            if (affichage != null) affichage.gameObject.SetActive(step == ScenarioStepId.Cpr);
        }

        private void Update()
        {
            if (ScenarioManager.Instance == null || ScenarioManager.Instance.CurrentStep != ScenarioStepId.Cpr) return;

            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
                CompterCompression(profondeurCible); // Test sans casque

            if (pointCompression == null || !MainSurLaPoitrine(out float y))
            {
                enfonce = false;
                haut = float.NaN;
                return;
            }

            if (float.IsNaN(haut))
            {
                haut = y;
                return;
            }

            if (!enfonce)
            {
                if (y > haut) haut = y;                          // La main remonte : nouveau point haut
                else if (haut - y >= seuil) { enfonce = true; bas = y; } // La main descend : compression en cours
            }
            else
            {
                if (y < bas) bas = y;                            // Point le plus bas de la compression
                else if (y - bas >= seuil * 0.5f)                // La main remonte : la compression est finie
                {
                    CompterCompression(haut - bas);
                    enfonce = false;
                    haut = y;
                }
            }
        }

        // Hauteur de la main la plus basse posée sur la poitrine
        private bool MainSurLaPoitrine(out float y)
        {
            y = float.MaxValue;
            bool trouvee = false;
            foreach (Transform main in new[] { mainGauche, mainDroite })
            {
                if (main == null) continue;
                Vector3 ecart = main.position - pointCompression.position;
                float horizontal = new Vector2(ecart.x, ecart.z).magnitude;
                if (horizontal > rayon || ecart.y < -0.15f || ecart.y > hauteurMax) continue;
                y = Mathf.Min(y, main.position.y);
                trouvee = true;
            }
            return trouvee;
        }

        private void CompterCompression(float profondeur)
        {
            float rythme = 0f;
            if (derniereCompression > 0f)
            {
                float intervalle = Time.time - derniereCompression;
                rythme = 60f / Mathf.Max(intervalle, 0.01f);
                if (intervalle < 2f) // Une pause plus longue ne compte pas dans le rythme moyen
                {
                    sommeIntervalles += intervalle;
                    nombreIntervalles++;
                }
            }
            derniereCompression = Time.time;

            total++;
            bool bonneProfondeur = profondeur >= profondeurCible;
            bool bonRythme = rythme == 0f || (rythme >= rythmeMin && rythme <= rythmeMax);
            if (bonneProfondeur && bonRythme) bonnes++;

            Vibrer(mainGauche);
            Vibrer(mainDroite);
            Afficher(rythme, bonneProfondeur);

            if (total >= compressionsAFaire) Terminer();
        }

        private void Afficher(float rythme, bool bonneProfondeur)
        {
            if (affichage == null) return;
            string conseil = !bonneProfondeur ? "Appuie plus fort !"
                : rythme > 0f && rythme < rythmeMin ? "Plus vite !"
                : rythme > rythmeMax ? "Moins vite !"
                : "Bien !";
            affichage.text = $"{total} / {compressionsAFaire}\n{(rythme > 0f ? $"{rythme:0} /min" : "")}\n{conseil}";
        }

        private void Terminer()
        {
            float rythmeMoyen = nombreIntervalles > 0 ? 60f / (sommeIntervalles / nombreIntervalles) : 0f;
            string resultat = $"{bonnes} / {total} bonnes, rythme moyen {rythmeMoyen:0} /min";
            Debug.Log($"[RCR] {resultat}");

            BilanController bilan = FindAnyObjectByType<BilanController>();
            if (bilan != null) bilan.AfficherCompressions(resultat);

            ScenarioManager.Instance.TryCompleteStep(ScenarioStepId.Cpr);
        }

        private static void Vibrer(Transform main)
        {
            if (main == null) return;
            HapticImpulsePlayer vibreur = main.GetComponent<HapticImpulsePlayer>();
            if (vibreur != null) vibreur.SendHapticImpulse(0.4f, 0.05f);
        }

        private void Reinitialiser()
        {
            total = 0;
            bonnes = 0;
            derniereCompression = -1f;
            sommeIntervalles = 0f;
            nombreIntervalles = 0;
            enfonce = false;
            haut = float.NaN;
            if (affichage != null) affichage.text = $"0 / {compressionsAFaire}";
        }

        [ContextMenu("Test : 30 compressions parfaites")]
        private void TestTrenteCompressions()
        {
            for (int i = 0; i < compressionsAFaire; i++) CompterCompression(profondeurCible);
        }
    }
}