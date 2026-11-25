using PremiersGestes.Scenario;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Abaisse la caméra pendant l'étape RCR, pour que l'utilisateur soit à hauteur de la victime,
/// qu'il soit debout ou assis. La hauteur est relative : on descend depuis la hauteur actuelle.
/// </summary>
public class KneelHeightAdjuster : MonoBehaviour
{
    public Transform cameraOffset;

    [Tooltip("De combien la caméra descend quand on est à genoux (en mètres, valeur négative).")]
    public float kneelingHeight = -0.6f;

    private bool estAGenoux;
    private float hauteurAvantGenoux;

    private void OnEnable()
    {
        if (ScenarioManager.Instance != null) ScenarioManager.Instance.StepChanged += OnStepChanged;
    }

    private void OnDisable()
    {
        if (ScenarioManager.Instance != null) ScenarioManager.Instance.StepChanged -= OnStepChanged;
    }

    // À genoux pendant la RCR, debout le reste du temps
    private void OnStepChanged(ScenarioStepId step)
    {
        if (step == ScenarioStepId.Cpr) KneelDown(); else StandUp();
    }

    void Update()
    {
        if (Keyboard.current == null) return;
        // Touches de test : F5 = s'agenouiller, F6 = se relever
        // (K et L sont le joystick du simulateur XR)
        if (Keyboard.current.f5Key.wasPressedThisFrame) KneelDown();
        if (Keyboard.current.f6Key.wasPressedThisFrame) StandUp();
    }

    public void KneelDown()
    {
        if (cameraOffset == null || estAGenoux) return;
        hauteurAvantGenoux = cameraOffset.localPosition.y;
        SetHauteur(hauteurAvantGenoux + kneelingHeight);
        estAGenoux = true;
    }

    public void StandUp()
    {
        if (cameraOffset == null || !estAGenoux) return;
        SetHauteur(hauteurAvantGenoux);
        estAGenoux = false;
    }

    private void SetHauteur(float y)
    {
        Vector3 position = cameraOffset.localPosition;
        position.y = y;
        cameraOffset.localPosition = position;
    }
}
