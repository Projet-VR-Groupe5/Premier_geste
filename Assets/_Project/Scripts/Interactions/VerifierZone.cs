using PremiersGestes.Scenario;
using UnityEngine;

namespace PremiersGestes.Interactions
{
    /// <summary>
    /// Étape « Sécuriser la zone » (SecureArea) : le joueur doit regarder chaque danger possible autour de lui.
    /// Quand tous les points ont été regardés un court instant, l'étape est terminée.
    /// </summary>
    public class VerifierZone : MonoBehaviour
    {
        [Tooltip("Les endroits à regarder (route, vélo, flaque, câble…). Des objets vides placés dans la scène.")]
        public Transform[] pointsARegarder;

        [Tooltip("Optionnel : un objet par point, affiché quand ce point a été vu (coche, contour vert…). Même ordre que les points.")]
        public GameObject[] indicateursVus;

        [Tooltip("La caméra du joueur. Vide = la caméra principale (Main Camera).")]
        public Transform cameraJoueur;

        [Tooltip("Angle maximum entre le regard et le point (en degrés).")]
        [Range(5f, 30f)]
        public float angleRegard = 15f;

        [Tooltip("Temps pendant lequel il faut regarder un point (en secondes).")]
        [Range(0.1f, 2f)]
        public float dureeRegard = 0.5f;

        private float[] tempsRegarde;
        private bool[] vu;

        private void Awake()
        {
            int n = pointsARegarder != null ? pointsARegarder.Length : 0;
            tempsRegarde = new float[n];
            vu = new bool[n];
            Reinitialiser();
        }

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
            if (step == ScenarioStepId.SecureArea) Reinitialiser(); // Au début ou si on recommence l'étape
        }

        private void Update()
        {
            if (ScenarioManager.Instance == null || ScenarioManager.Instance.CurrentStep != ScenarioStepId.SecureArea) return;

            Transform cam = cameraJoueur;
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            if (cam == null || vu.Length == 0) return;

            bool tousVus = true;
            for (int i = 0; i < vu.Length; i++)
            {
                if (vu[i] || pointsARegarder[i] == null) continue;

                Vector3 versPoint = pointsARegarder[i].position - cam.position;
                if (Vector3.Angle(cam.forward, versPoint) <= angleRegard)
                {
                    tempsRegarde[i] += Time.deltaTime;
                    if (tempsRegarde[i] >= dureeRegard) MarquerVu(i);
                }
                else
                {
                    tempsRegarde[i] = 0f;
                }

                if (!vu[i]) tousVus = false;
            }

            if (tousVus) ScenarioManager.Instance.TryCompleteStep(ScenarioStepId.SecureArea);
        }

        private void MarquerVu(int i)
        {
            vu[i] = true;
            if (indicateursVus != null && i < indicateursVus.Length && indicateursVus[i] != null)
                indicateursVus[i].SetActive(true);
            Debug.Log($"[Sécuriser] Point vu : {pointsARegarder[i].name}");
        }

        private void Reinitialiser()
        {
            for (int i = 0; i < vu.Length; i++)
            {
                vu[i] = false;
                tempsRegarde[i] = 0f;
                if (indicateursVus != null && i < indicateursVus.Length && indicateursVus[i] != null)
                    indicateursVus[i].SetActive(false);
            }
        }

        [ContextMenu("Test : tout regarder")]
        private void TestToutRegarder()
        {
            for (int i = 0; i < vu.Length; i++) MarquerVu(i);
        }
    }
}