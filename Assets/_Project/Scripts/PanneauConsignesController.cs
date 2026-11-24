using PremiersGestes.Scenario;
using TMPro;
using UnityEngine;

/// <summary>
/// Affiche sur le panneau la consigne de l'étape en cours.
/// Écoute le ScenarioManager : le texte change tout seul à chaque étape.
/// </summary>
public class PanneauConsignesController : MonoBehaviour
{
    public TMP_Text texteConsigne;

    [Tooltip("Une consigne par étape, dans l'ordre : Accueil, Sécuriser, Vérifier, Alerter, RCR, DEA, Fin.")]
    [TextArea(2, 4)]
    public string[] consignesParEtape =
    {
        "Bienvenue ! Entraîne-toi : téléporte-toi, saisis un objet et appuie sur un bouton.",
        "Une personne s'est effondrée. Vérifie d'abord que la zone est sans danger.",
        "Approche-toi et tape sur l'épaule de la victime. Réagit-elle ?",
        "Appelle le 911 avec le téléphone, puis envoie un passant chercher le DEA.",
        "Commence les compressions : appuie fort et vite au centre de la poitrine.",
        "Pose les électrodes du DEA et suis ses instructions.",
        "Les ambulanciers arrivent. Bravo ! Regarde ton bilan."
    };

    private void OnEnable()
    {
        if (ScenarioManager.Instance == null) return; // Pas de scénario (ex. scène Sandbox) : texte fixe
        ScenarioManager.Instance.StepChanged += OnStepChanged;
        OnStepChanged(ScenarioManager.Instance.CurrentStep); // Affiche tout de suite l'étape en cours
    }

    private void OnDisable()
    {
        if (ScenarioManager.Instance != null)
            ScenarioManager.Instance.StepChanged -= OnStepChanged;
    }

    private void OnStepChanged(ScenarioStepId step)
    {
        int index = (int)step;
        if (index < consignesParEtape.Length)
            AfficherConsigne(consignesParEtape[index]);
    }

    public void AfficherConsigne(string consigne)
    {
        if (texteConsigne != null)
            texteConsigne.text = consigne;
    }
}
