using System.Collections;
using PremiersGestes.Scenario;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace PremiersGestes.Interactions
{
    /// <summary>
    /// Étape « DEA » (Aed) : le joueur pose les 2 électrodes sur la poitrine, le DEA analyse,
    /// puis le joueur appuie sur le bouton « Choc ». L'étape est alors terminée.
    /// À placer sur le DEA. Les électrodes sont des XR Grab Interactable ; leurs emplacements sur la poitrine
    /// sont des XR Socket Interactor.
    /// </summary>
    public class DeaController : MonoBehaviour
    {
        [Tooltip("Les 2 emplacements des électrodes sur la poitrine (XR Socket Interactor) : sous la clavicule droite, et côté gauche sous l'aisselle.")]
        public XRSocketInteractor[] emplacements;

        [Tooltip("Le bouton « Choc » du DEA (XR Simple Interactable). On peut aussi appeler Choc() depuis un bouton d'interface.")]
        public XRBaseInteractable boutonChoc;

        [Tooltip("Optionnel : voyant allumé quand le DEA est prêt pour le choc.")]
        public GameObject voyantPret;

        [Tooltip("Haut-parleur du DEA (AudioSource, son 3D).")]
        public AudioSource hautParleur;
        [Tooltip("« Analyse en cours. Ne touchez pas le patient. »")]
        public AudioClip annonceAnalyse;
        [Tooltip("« Choc conseillé. Écartez-vous et appuyez sur le bouton. »")]
        public AudioClip annonceChoc;

        [Tooltip("Durée de l'analyse (en secondes).")]
        public float dureeAnalyse = 3f;

        private bool pret;
        private Coroutine analyse;

        private void OnEnable()
        {
            if (boutonChoc != null) boutonChoc.selectEntered.AddListener(OnBoutonChoc);
            if (ScenarioManager.Instance != null) ScenarioManager.Instance.StepChanged += OnStepChanged;
        }

        private void OnDisable()
        {
            if (boutonChoc != null) boutonChoc.selectEntered.RemoveListener(OnBoutonChoc);
            if (ScenarioManager.Instance != null) ScenarioManager.Instance.StepChanged -= OnStepChanged;
        }

        private void Start() => Reinitialiser();

        private void OnStepChanged(ScenarioStepId step) => Reinitialiser();

        private void Update()
        {
            if (ScenarioManager.Instance == null || ScenarioManager.Instance.CurrentStep != ScenarioStepId.Aed) return;

            bool electrodesPosees = ElectrodesPosees();
            if (electrodesPosees && !pret && analyse == null)
                analyse = StartCoroutine(Analyser());
            else if (!electrodesPosees && (pret || analyse != null))
                Reinitialiser(); // Une électrode a été retirée : on recommence l'analyse
        }

        private bool ElectrodesPosees()
        {
            if (emplacements == null || emplacements.Length == 0) return false;
            foreach (XRSocketInteractor emplacement in emplacements)
            {
                if (emplacement == null || !emplacement.hasSelection) return false;
            }
            return true;
        }

        private IEnumerator Analyser()
        {
            Annoncer(annonceAnalyse);
            yield return new WaitForSeconds(dureeAnalyse);
            analyse = null;
            pret = true;
            if (voyantPret != null) voyantPret.SetActive(true);
            Annoncer(annonceChoc);
        }

        private void OnBoutonChoc(SelectEnterEventArgs args) => Choc();

        /// <summary>Bouton « Choc » : seulement quand le DEA est prêt.</summary>
        public void Choc()
        {
            if (!pret || ScenarioManager.Instance == null || ScenarioManager.Instance.CurrentStep != ScenarioStepId.Aed)
            {
                Debug.Log("[DEA] Pas encore prêt : pose les 2 électrodes et attends la fin de l'analyse.");
                return;
            }
            Debug.Log("[DEA] Choc délivré.");
            ScenarioManager.Instance.TryCompleteStep(ScenarioStepId.Aed);
        }

        private void Annoncer(AudioClip annonce)
        {
            if (hautParleur == null || annonce == null) return;
            hautParleur.Stop();
            hautParleur.clip = annonce;
            hautParleur.Play();
        }

        private void Reinitialiser()
        {
            if (analyse != null) StopCoroutine(analyse);
            analyse = null;
            pret = false;
            if (voyantPret != null) voyantPret.SetActive(false);
            if (hautParleur != null) hautParleur.Stop();
        }

        [ContextMenu("Test : électrodes posées et analyse terminée")]
        private void TestPret()
        {
            Reinitialiser();
            pret = true;
            if (voyantPret != null) voyantPret.SetActive(true);
        }

        [ContextMenu("Test : choc")]
        private void TestChoc() => Choc();
    }
}