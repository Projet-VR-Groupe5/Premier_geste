
using UnityEngine;
using UnityEngine.InputSystem;

public class KneelHeightAdjuster : MonoBehaviour
{
    public Transform cameraOffset;

    public float standingHeight = 0f;
    public float kneelingHeight = -0.6f;

    void Update()
    {
        if (Keyboard.current == null)
            return;

        // K : s'agenouiller
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            KneelDown();
        }

        // L : se relever
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            StandUp();
        }
    }

    public void KneelDown()
    {
        if (cameraOffset != null)
        {
            Vector3 position = cameraOffset.localPosition;
            position.y = kneelingHeight;
            cameraOffset.localPosition = position;
        }
    }

    public void StandUp()
    {
        if (cameraOffset != null)
        {
            Vector3 position = cameraOffset.localPosition;
            position.y = standingHeight;
            cameraOffset.localPosition = position;
        }
    }
}
