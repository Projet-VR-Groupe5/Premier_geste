using System.Collections;
using PremiersGestes.Interface;
using PremiersGestes.Scenario;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PremiersGestes.Interactions
{
    /// <summary>
    /// Étape « Alerter » (CallForHelp) : quand le joueur prend le téléphone, le répartiteur du 911 parle
    /// depuis le téléphone (son 3D), puis l'étape est terminée.
    /// À placer sur le téléphone (Telephone_VR), avec un XR Grab Interactable, un Rigidbody et un Collider.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class TelephoneAppel : MonoBehaviour
    {
        [Tooltip("Répliques du répartiteur, dans l'ordre (Repartiteur_01 à Repartiteur_04).")]
        public AudioClip[] repliques;

        [Tooltip("Haut-parleur du téléphone. Vide = l'AudioSource de cet objet (ajoutée si besoin).")]
        public AudioSource hautParleur;

        [Tooltip("Pause entre deux répliques (en secondes).")]
        [Range(0f, 3f)]
        public float pauseEntreRepliques = 0.6f;

        private XRGrabInteractable grab;
        private Coroutine appel;

        private void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            if (hautParleur == null) hautParleur = GetComponent<AudioSource>();
            if (hautParleur == null) hautParleur = gameObject.AddComponent<AudioSource>();
            hautParleur.playOnAwake = false;
            hautParleur.spatialBlend = 1f; // Son 3D : la voix vient du téléphone
        }

        private void OnEnable()
        {
            grab.selectEntered.AddListener(OnPris);
            if (ScenarioManager.Instance != null) ScenarioManager.Instance.StepChanged += OnStepChanged;
        }

        private void OnDisable()
        {
            grab.selectEntered.RemoveListener(OnPris);
            if (ScenarioManager.Instance != null) ScenarioManager.Instance.StepChanged -= OnStepChanged;
        }

        private void OnPris(SelectEnterEventArgs args) => Decrocher();

        // Si on change d'étape (ou si on la recommence), l'appel s'arrête
        private void OnStepChanged(ScenarioStepId step) => Raccrocher();

        public void Decrocher()
        {
            if (appel != null || ScenarioManager.Instance == null) return;
            if (ScenarioManager.Instance.CurrentStep != ScenarioStepId.CallForHelp) return;
            appel = StartCoroutine(Appel());
        }

        private IEnumerator Appel()
        {
            if (GuideVocal.Instance != null) GuideVocal.Instance.Arreter(); // Une seule voix à la fois

            bool aParle = false;
            if (repliques != null)
            {
                foreach (AudioClip replique in repliques)
                {
                    if (replique == null) continue;
                    hautParleur.clip = replique;
                    hautParleur.Play();
                    aParle = true;
                    yield return new WaitForSeconds(replique.length + pauseEntreRepliques);
                }
            }
            if (!aParle) yield return new WaitForSeconds(1f); // Pas encore d'enregistrement : on valide quand même

            appel = null;
            if (ScenarioManager.Instance != null)
                ScenarioManager.Instance.TryCompleteStep(ScenarioStepId.CallForHelp);
        }

        private void Raccrocher()
        {
            if (appel != null) StopCoroutine(appel);
            appel = null;
            hautParleur.Stop();
        }

        [ContextMenu("Test : décrocher")]
        private void TestDecrocher() => Decrocher();
    }
}