using PremiersGestes.Scenario;
using UnityEngine;

namespace PremiersGestes.Interface
{
    /// <summary>
    /// Voix qui guide le joueur : à chaque étape du scénario, joue la consigne enregistrée.
    /// - Une nouvelle consigne coupe la précédente (jamais deux voix en même temps).
    /// - Repeter() rejoue la consigne de l'étape en cours (bouton « Répéter la consigne »).
    /// - S'il manque un enregistrement, rien n'est joué : le panneau de consignes reste la référence.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class GuideVocal : MonoBehaviour
    {
        /// <summary>Accès depuis n'importe quel script : GuideVocal.Instance</summary>
        public static GuideVocal Instance { get; private set; }

        [Tooltip("Une voix par étape, dans l'ordre : Accueil, Sécuriser, Vérifier, Alerter, RCR, DEA, Fin.\n" +
                 "Rempli automatiquement par le menu « Premiers Gestes → Installer le guidage dans Main ».")]
        public AudioClip[] voixParEtape = new AudioClip[7];

        [Tooltip("Petite pause avant de parler (en secondes), pour ne pas couper le son du bouton ou de l'action.")]
        [Range(0f, 2f)]
        public float delaiAvantVoix = 0.4f;

        private AudioSource source;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[Guide vocal] Un deuxième GuideVocal a été trouvé : il est ignoré.", this);
                enabled = false;
                return;
            }
            Instance = this;

            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f; // Voix de l'instructeur : entendue « dans les oreilles », pas depuis un point de la scène
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

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnStepChanged(ScenarioStepId step)
        {
            JouerEtape(step);
        }

        /// <summary>Rejoue la consigne de l'étape en cours.</summary>
        public void Repeter()
        {
            if (ScenarioManager.Instance != null)
                JouerEtape(ScenarioManager.Instance.CurrentStep);
        }

        /// <summary>Coupe la voix (par exemple quand le répartiteur du 911 parle).</summary>
        public void Arreter()
        {
            if (source != null) source.Stop();
        }

        private void JouerEtape(ScenarioStepId step)
        {
            if (source == null) return;
            source.Stop(); // La nouvelle consigne coupe toujours l'ancienne

            int index = (int)step;
            if (voixParEtape == null || index >= voixParEtape.Length || voixParEtape[index] == null)
            {
                Debug.Log($"[Guide vocal] Pas encore d'enregistrement pour l'étape {step}.");
                return;
            }

            source.clip = voixParEtape[index];
            source.PlayDelayed(delaiAvantVoix);
        }
    }
}
