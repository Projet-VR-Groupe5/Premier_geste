
using UnityEngine;
using UnityEngine.InputSystem;

public class WristMenuController : MonoBehaviour
{
    public GameObject wristMenu;
    public InputActionReference toggleAction;
    public Transform mainCamera;

    public bool automaticVisibility = true;
    public bool invertFacing = false;
    [Range(0f, 1f)]
    public float facingThreshold = 0.5f;

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.mKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }

        if (automaticVisibility &&
            wristMenu != null &&
            mainCamera != null)
        {
            Vector3 direction =
                (mainCamera.position - wristMenu.transform.position).normalized;

            float facing = Vector3.Dot(
                wristMenu.transform.forward, direction);

            if (invertFacing)
                facing = -facing;

            wristMenu.SetActive(facing > facingThreshold);
        }
    }

    void OnEnable()
    {
        if (toggleAction != null && toggleAction.action != null)
            toggleAction.action.performed += OnTogglePressed;
    }

    void OnDisable()
    {
        if (toggleAction != null && toggleAction.action != null)
            toggleAction.action.performed -= OnTogglePressed;
    }

    void OnTogglePressed(InputAction.CallbackContext context)
    {
        ToggleMenu();
    }

    public void ToggleMenu()
    {
        if (wristMenu != null && !automaticVisibility)
            wristMenu.SetActive(!wristMenu.activeSelf);
    }

    public void RestartStep()
    {
        Debug.Log("Demande : recommencer l'etape");
    }

    public void BackToHome()
    {
        Debug.Log("Demande : retour a l'accueil");
    }
}
