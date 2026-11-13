namespace PremiersGestes.Scenario
{
    /// <summary>
    /// Les sept étapes du scénario, dans l'ordre où l'utilisateur les vit.
    /// (Noms en anglais selon nos conventions, équivalent français en commentaire.)
    /// </summary>
    public enum ScenarioStepId
    {
        Welcome,      // Accueil : zone d'entraînement
        SecureArea,   // Sécuriser : vérifier que la zone est sans danger
        CheckVictim,  // Vérifier : taper sur l'épaule, la victime réagit-elle ?
        CallForHelp,  // Alerter : appeler le 911, envoyer un passant chercher le DEA
        Cpr,          // RCR : compressions thoraciques
        Aed,          // DEA : poser les électrodes, délivrer le choc
        End           // Fin : arrivée des ambulanciers et bilan
    }
}
