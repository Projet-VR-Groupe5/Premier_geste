
using UnityEngine;
using UnityEngine.Events;
using System;

public class NPCInteractable : MonoBehaviour
{
    public enum NPCRole
    {
        Victim,
        Doctor,
        Nurse,
        Witness,
        Civilian
    }

    [Serializable]
    public class NPCOption
    {
        public string optionName;

        [Tooltip("Nom du Trigger dans l'Animator")]
        public string animationTrigger;

        [Tooltip("Action supplementaire")]
        public UnityEvent onSelected;
    }

    [Header("Character Information")]
    public string characterName = "Personnage";
    public NPCRole role;

    [Header("Animation")]
    public Animator animator;

    [Header("Available Interactions")]
    public NPCOption[] options;

    [Header("Interaction Panel")]
    public NPCInteractionPanel interactionPanel;

    public void OpenMenu()
    {
        if (interactionPanel == null)
        {
            Debug.LogWarning(
                characterName + " : Interaction Panel manquant !"
            );
            return;
        }

        Debug.Log("Ouverture du menu : " + characterName);

        interactionPanel.Show(this);
    }

    public void ExecuteOption(int index)
    {
        if (options == null ||
            index < 0 ||
            index >= options.Length)
        {
            Debug.LogWarning("Option invalide !");
            return;
        }

        NPCOption option = options[index];

        if (animator != null &&
            !string.IsNullOrWhiteSpace(option.animationTrigger))
        {
            animator.SetTrigger(option.animationTrigger);
        }

        if (option.onSelected != null)
        {
            option.onSelected.Invoke();
        }

        Debug.Log(
            characterName + " exécute : " + option.optionName
        );
    }
}
