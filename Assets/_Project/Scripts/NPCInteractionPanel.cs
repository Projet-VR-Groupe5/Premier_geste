
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCInteractionPanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panelRoot;

    [Header("UI Elements")]
    public TMP_Text characterTitle;
    public Transform buttonContainer;
    public Button buttonPrefab;

    private NPCInteractable currentNPC;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Show(NPCInteractable npc)
    {
        if (npc == null)
        {
            Debug.LogWarning("NPC introuvable !");
            return;
        }

        if (panelRoot == null ||
            characterTitle == null ||
            buttonContainer == null ||
            buttonPrefab == null)
        {
            Debug.LogError(
                "Une reference du NPCInteractionPanel manque !"
            );
            return;
        }

        currentNPC = npc;

        panelRoot.SetActive(true);

        characterTitle.text = npc.characterName;

        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        if (npc.options == null || npc.options.Length == 0)
        {
            Debug.LogWarning("Aucune interaction configuree !");
            return;
        }

        for (int i = 0; i < npc.options.Length; i++)
        {
            int index = i;

            Button newButton =
                Instantiate(buttonPrefab, buttonContainer);

            TMP_Text buttonText =
                newButton.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
            {
                buttonText.text =
                    npc.options[index].optionName;
            }

            newButton.onClick.AddListener(() =>
            {
                if (currentNPC != null)
                    currentNPC.ExecuteOption(index);

                Hide();
            });
        }

        Debug.Log("Menu affiche pour : " + npc.characterName);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        currentNPC = null;
    }
}
