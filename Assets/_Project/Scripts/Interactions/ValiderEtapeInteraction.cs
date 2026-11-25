using PremiersGestes.Scenario;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PremiersGestes.Interactions
{
    /// <summary>
    /// Termine une étape du scénario quand le joueur interagit avec cet objet.
    /// Exemple : l'épaule de la victime, pour l'étape « Vérifier la victime » (CheckVictim).
    /// À placer sur un objet qui a un XR Simple Interactable (ou un XR Grab Interactable) et un Collider.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public class ValiderEtapeInteraction : MonoBehaviour
    {
        public enum Declencheur { Saisie, Gachette }

        [Tooltip("L'étape terminée par cette interaction.")]
        public ScenarioStepId etape = ScenarioStepId.CheckVictim;

        [Tooltip("Saisie : bouton de prise (grip) ou appui avec l'index (Poke). Gâchette : appui sur la gâchette en tenant l'objet.")]
        public Declencheur declencheur = Declencheur.Saisie;

        [Tooltip("Retour au joueur quand l'étape est validée : son, effet visuel, texte…")]
        public UnityEvent quandValide = new UnityEvent();

        private XRBaseInteractable interactable;

        private void Awake()
        {
            interactable = GetComponent<XRBaseInteractable>();
        }

        private void OnEnable()
        {
            if (declencheur == Declencheur.Saisie)
                interactable.selectEntered.AddListener(OnSaisie);
            else
                interactable.activated.AddListener(OnGachette);
        }

        private void OnDisable()
        {
            interactable.selectEntered.RemoveListener(OnSaisie);
            interactable.activated.RemoveListener(OnGachette);
        }

        private void OnSaisie(SelectEnterEventArgs args) => Valider();
        private void OnGachette(ActivateEventArgs args) => Valider();

        /// <summary>Valide l'étape, seulement si c'est l'étape en cours.</summary>
        public void Valider()
        {
            if (ScenarioManager.Instance == null) return;
            if (ScenarioManager.Instance.TryCompleteStep(etape))
                quandValide.Invoke();
        }

        [ContextMenu("Test : valider l'étape")]
        private void TestValider() => Valider();
    }
}