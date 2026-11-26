
using UnityEngine;

public class NPCPanelTest : MonoBehaviour
{
    public NPCInteractionPanel panel;
    public NPCInteractable testNPC;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (panel != null && testNPC != null)
            {
                panel.Show(testNPC);
                Debug.Log("Test du panneau !");
            }
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            if (panel != null)
                panel.Hide();
        }
    }
}
