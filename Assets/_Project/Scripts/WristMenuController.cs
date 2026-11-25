using PremiersGestes.Interface;
using PremiersGestes.Scenario;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

/// <summary>
/// Menu au poignet gauche.
/// - Il s'affiche seulement quand le poignet est tourné vers le visage ET que le joueur le regarde.
/// - Ses boutons changent selon l'étape du scénario (« Commencer » seulement à l'accueil).
/// - « Accueil » demande une confirmation (Oui / Non) avant de tout recommencer.
/// - Les boutons sont reliés par ce script, grâce à leur nom : inutile de remplir « On Click () » dans l'Inspector.
/// </summary>
public class WristMenuController : MonoBehaviour
{
    [Header("Références")]
    public GameObject wristMenu;
    public InputActionReference toggleAction;
    public Transform mainCamera;

    [Header("Affichage automatique")]
    public bool automaticVisibility = true;
    public bool invertFacing = false;
    [Range(0f, 1f)]
    public float facingThreshold = 0.5f;

    [Tooltip("Le joueur doit regarder le menu : angle maximum entre la direction du regard et le menu (en degrés).")]
    [Range(5f, 90f)]
    public float angleRegardMax = 30f;

    [Tooltip("Durée (en secondes) pendant laquelle la condition doit rester vraie, ou fausse, avant d'afficher ou de cacher le menu. Évite qu'il clignote.")]
    [Range(0f, 1f)]
    public float delaiAffichage = 0.25f;

    [Header("Vibration quand le menu apparaît")]
    [Range(0f, 1f)]
    public float forceVibration = 0.3f;
    public float dureeVibration = 0.08f;

    // Éléments du menu, trouvés par leur nom (voir Awake)
    private Button boutonCommencer;
    private Button boutonRecommencer;
    private Button boutonAccueil;
    private Button boutonRepeter;
    private GameObject fenetreConfirmation;
    private Transform cibleRegard;
    private HapticImpulsePlayer vibreur;
    private float tempsCondition;

    private void Awake()
    {
        if (wristMenu == null)
        {
            Debug.LogWarning("[Menu poignet] Le champ « Wrist Menu » est vide.", this);
            return;
        }

        boutonCommencer = Relier("BoutonCommencer", Commencer);
        boutonRecommencer = Relier("BoutonRecommencer", RestartStep);
        boutonAccueil = Relier("BoutonAccueil", DemanderRetourAccueil);
        boutonRepeter = Relier("BoutonRepeter", RepeterConsigne);
        Relier("BoutonOui", ConfirmerRetourAccueil);
        Relier("BoutonNon", FermerConfirmation);
        fenetreConfirmation = TrouverObjet("FenetreConfirmation");

        // Le joueur regarde le panneau (Canvas), pas le point d'attache sur la manette
        Canvas canvas = wristMenu.GetComponentInChildren<Canvas>(true);
        cibleRegard = canvas != null ? canvas.transform : wristMenu.transform;

        // La vibration se trouve sur la manette qui porte le menu (Left Controller)
        vibreur = wristMenu.GetComponentInParent<HapticImpulsePlayer>(true);
    }

    private void OnEnable()
    {
        if (toggleAction != null && toggleAction.action != null)
            toggleAction.action.performed += OnTogglePressed;
        if (ScenarioManager.Instance != null)
            ScenarioManager.Instance.StepChanged += OnStepChanged;
    }

    private void OnDisable()
    {
        if (toggleAction != null && toggleAction.action != null)
            toggleAction.action.performed -= OnTogglePressed;
        if (ScenarioManager.Instance != null)
            ScenarioManager.Instance.StepChanged -= OnStepChanged;
    }

    private void Start()
    {
        FermerConfirmation();
        MettreAJourBoutons();
        if (automaticVisibility && wristMenu != null)
            wristMenu.SetActive(false); // Caché au départ : il apparaît quand on regarde son poignet
    }

    private void Update()
    {
        // Touche de test : F7 (M est déjà le bouton « menu » du simulateur XR)
        if (Keyboard.current != null && Keyboard.current.f7Key.wasPressedThisFrame)
            ToggleMenu();

        if (!automaticVisibility || wristMenu == null || mainCamera == null)
            return;

        bool doitEtreVisible = PoignetTourneVersLeVisage() && JoueurRegardeLeMenu();
        if (doitEtreVisible == wristMenu.activeSelf)
        {
            tempsCondition = 0f;
            return;
        }

        // On attend un peu avant de changer, pour éviter le clignotement
        tempsCondition += Time.deltaTime;
        if (tempsCondition >= delaiAffichage)
        {
            tempsCondition = 0f;
            Afficher(doitEtreVisible);
        }
    }

    // --- Conditions d'affichage ---

    private bool PoignetTourneVersLeVisage()
    {
        Vector3 direction = (mainCamera.position - wristMenu.transform.position).normalized;
        float facing = Vector3.Dot(wristMenu.transform.forward, direction);
        if (invertFacing)
            facing = -facing;
        return facing > facingThreshold;
    }

    private bool JoueurRegardeLeMenu()
    {
        Vector3 versMenu = cibleRegard.position - mainCamera.position;
        return Vector3.Angle(mainCamera.forward, versMenu) <= angleRegardMax;
    }

    private void Afficher(bool visible)
    {
        wristMenu.SetActive(visible);
        if (visible)
        {
            MettreAJourBoutons();
            if (vibreur != null && forceVibration > 0f)
                vibreur.SendHapticImpulse(forceVibration, dureeVibration);
        }
        else
        {
            FermerConfirmation(); // On ne laisse pas la question ouverte quand le menu se cache
        }
    }

    // --- Boutons selon l'étape ---

    private void OnStepChanged(ScenarioStepId step)
    {
        FermerConfirmation();
        MettreAJourBoutons();
    }

    private void MettreAJourBoutons()
    {
        if (ScenarioManager.Instance == null) return;

        ScenarioStepId etape = ScenarioManager.Instance.CurrentStep;
        bool accueil = etape == ScenarioStepId.Welcome;
        bool fin = etape == ScenarioStepId.End;

        Montrer(boutonCommencer, accueil);              // Seulement avant de commencer
        Montrer(boutonRecommencer, !accueil && !fin);   // Pendant le scénario
        Montrer(boutonAccueil, !accueil);               // Une fois le scénario commencé
        Montrer(boutonRepeter, true);                   // Toujours
    }

    private static void Montrer(Button bouton, bool visible)
    {
        if (bouton != null)
            bouton.gameObject.SetActive(visible);
    }

    // --- Actions des boutons ---

    private void OnTogglePressed(InputAction.CallbackContext context)
    {
        ToggleMenu();
    }

    /// <summary>Affiche ou cache le menu à la main (seulement si l'affichage automatique est désactivé).</summary>
    public void ToggleMenu()
    {
        if (wristMenu != null && !automaticVisibility)
            Afficher(!wristMenu.activeSelf);
    }

    public void Commencer()
    {
        if (ScenarioManager.Instance != null) ScenarioManager.Instance.StartScenario();
    }

    public void RestartStep()
    {
        Debug.Log("Demande : recommencer l'étape");
        if (ScenarioManager.Instance != null) ScenarioManager.Instance.RestartStep();
    }

    public void RepeterConsigne()
    {
        Debug.Log("Demande : répéter la consigne");
        if (GuideVocal.Instance != null) GuideVocal.Instance.Repeter();
    }

    /// <summary>Bouton « Accueil » : demande d'abord une confirmation.</summary>
    public void DemanderRetourAccueil()
    {
        if (fenetreConfirmation != null)
            fenetreConfirmation.SetActive(true);
        else
            BackToHome(); // Pas de fenêtre de confirmation dans ce menu : on y va directement
    }

    public void ConfirmerRetourAccueil()
    {
        FermerConfirmation();
        BackToHome();
    }

    public void FermerConfirmation()
    {
        if (fenetreConfirmation != null)
            fenetreConfirmation.SetActive(false);
    }

    public void BackToHome()
    {
        Debug.Log("Demande : retour à l'accueil");
        if (ScenarioManager.Instance != null) ScenarioManager.Instance.BackToHome();
    }

    // --- Recherche des éléments par leur nom ---

    private Button Relier(string nom, UnityAction action)
    {
        foreach (Button bouton in wristMenu.GetComponentsInChildren<Button>(true))
        {
            if (bouton.name != nom) continue;
            bouton.onClick = new Button.ButtonClickedEvent(); // Remplace les liens faits dans l'Inspector
            bouton.onClick.AddListener(action);
            return bouton;
        }
        Debug.LogWarning($"[Menu poignet] Bouton « {nom} » introuvable dans {wristMenu.name}.", this);
        return null;
    }

    private GameObject TrouverObjet(string nom)
    {
        foreach (Transform enfant in wristMenu.GetComponentsInChildren<Transform>(true))
        {
            if (enfant.name == nom) return enfant.gameObject;
        }
        return null;
    }
}
