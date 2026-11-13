using System;
using UnityEngine;
using UnityEngine.Events;

namespace PremiersGestes.Scenario
{
    /// <summary>
    /// Chef d'orchestre du scénario : connaît l'étape en cours, passe à la suivante
    /// et prévient les autres systèmes. Un seul ScenarioManager, placé dans la scène Main.
    /// </summary>
    [DefaultExecutionOrder(-100)] // S'initialise avant les autres scripts, pour qu'ils le trouvent.
    public class ScenarioManager : MonoBehaviour
    {
        /// <summary>Accès depuis n'importe quel script : ScenarioManager.Instance</summary>
        public static ScenarioManager Instance { get; private set; }

        [Tooltip("Étape au lancement de l'application.")]
        [SerializeField] private ScenarioStepId startStep = ScenarioStepId.Welcome;

        [Tooltip("Appelé à chaque changement d'étape. Permet de brancher des objets dans l'Inspector, sans code.")]
        [SerializeField] private UnityEvent<ScenarioStepId> onStepChanged = new UnityEvent<ScenarioStepId>();

        /// <summary>Pour les scripts : ScenarioManager.Instance.StepChanged += MaMethode;</summary>
        public event Action<ScenarioStepId> StepChanged;

        /// <summary>L'étape en cours.</summary>
        public ScenarioStepId CurrentStep { get; private set; }

        /// <summary>Vrai quand on est arrivé à la dernière étape.</summary>
        public bool IsFinished => CurrentStep == ScenarioStepId.End;

        private void Awake()
        {
            // Il ne doit y avoir qu'un seul chef d'orchestre.
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[Scénario] Un deuxième ScenarioManager a été trouvé : il est ignoré.", this);
                enabled = false;
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Dans Start (et pas Awake) : les autres systèmes se sont déjà abonnés dans leur OnEnable.
            GoToStep(startStep);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Va directement à une étape et prévient tout le monde.</summary>
        public void GoToStep(ScenarioStepId step)
        {
            CurrentStep = step;
            Debug.Log($"[Scénario] Étape : {step}");
            StepChanged?.Invoke(step);
            onStepChanged.Invoke(step);
        }

        /// <summary>
        /// Termine une étape, seulement si c'est bien l'étape en cours.
        /// Exemple (téléphone) : ScenarioManager.Instance.TryCompleteStep(ScenarioStepId.CallForHelp);
        /// Évite qu'une action faite trop tôt ou trop tard fasse sauter une étape.
        /// </summary>
        public bool TryCompleteStep(ScenarioStepId step)
        {
            if (step != CurrentStep)
            {
                Debug.Log($"[Scénario] « {step} » ignoré : l'étape en cours est {CurrentStep}.");
                return false;
            }
            CompleteCurrentStep();
            return true;
        }

        /// <summary>Passe à l'étape suivante (sauf si le scénario est terminé).</summary>
        public void CompleteCurrentStep()
        {
            if (IsFinished)
            {
                Debug.Log("[Scénario] Le scénario est déjà terminé.");
                return;
            }
            GoToStep(CurrentStep + 1);
        }

        /// <summary>Recommence l'étape en cours (bouton du menu au poignet).</summary>
        public void RestartStep() => GoToStep(CurrentStep);

        /// <summary>Retour au tout début (bouton du menu au poignet).</summary>
        public void BackToHome() => GoToStep(ScenarioStepId.Welcome);

        // --- Tests sans casque : clic droit sur le composant dans l'Inspector, en mode Play ---
        [ContextMenu("Test : étape suivante")]
        private void DebugNextStep() => CompleteCurrentStep();

        [ContextMenu("Test : recommencer l'étape")]
        private void DebugRestartStep() => RestartStep();

        [ContextMenu("Test : retour à l'accueil")]
        private void DebugBackToHome() => BackToHome();
    }
}
