
using UnityEngine;
using TMPro;

public class PanneauConsignesController : MonoBehaviour
{
    public TMP_Text texteConsigne;

    public void AfficherConsigne(string consigne)
    {
        if (texteConsigne != null)
            texteConsigne.text = consigne;
    }
}
