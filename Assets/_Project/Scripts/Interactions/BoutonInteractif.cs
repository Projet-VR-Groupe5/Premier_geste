
using UnityEngine;

public class BoutonInteractif : MonoBehaviour
{
    private Renderer boutonRenderer;
    private Color couleurInitiale;

    private void Start()
    {
        boutonRenderer = GetComponent<Renderer>();
        couleurInitiale = boutonRenderer.material.color;
    }

    public void ActiverBouton()
    {
        boutonRenderer.material.color = Color.green;
        Debug.Log("Bouton activé !");
    }
}
