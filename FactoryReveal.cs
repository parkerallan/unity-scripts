using UnityEngine;

public class FactoryReveal : MonoBehaviour
{
    [Header("Objects to Control")]
    public GameObject objectToDisable;
    public GameObject objectToEnable1;
    public GameObject objectToEnable2;
    public GameObject objectToEnable3;

    [Header("Audio Settings")]
    public AudioClip revealSFX;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    
    [Header("Dialogue Settings")]
    public string speakerName = "System";
    [TextArea(3, 5)]
    public string dialogueText = "The factory has been revealed!";
    public string characterImagePath = "";
    public bool faceRight = true;
    
    private ProgrammaticDialogueTrigger dialogueTrigger;
    private DialogueManager dialogueManager;
    
    private void Start()
    {
        dialogueManager = Object.FindAnyObjectByType<DialogueManager>();
        dialogueTrigger = GetComponent<ProgrammaticDialogueTrigger>();
        if (dialogueTrigger == null)
        {
            dialogueTrigger = gameObject.AddComponent<ProgrammaticDialogueTrigger>();
        }
    }
    
    public void StartReveal()
    {
        if (objectToDisable != null)
            objectToDisable.SetActive(false);
            
        if (revealSFX != null && SFXManager.instance != null)
            SFXManager.instance.PlaySFXClip(revealSFX, transform, sfxVolume);
            
        dialogueManager.OnDialogueEnd += OnDialogueComplete;
        
        string imagePath = string.IsNullOrEmpty(characterImagePath) ? null : characterImagePath;
        dialogueTrigger.StartSimpleDialogue(speakerName, dialogueText, imagePath, faceRight);
    }
    
    private void OnDialogueComplete()
    {
        dialogueManager.OnDialogueEnd -= OnDialogueComplete;
        
        if (objectToEnable1 != null)
            objectToEnable1.SetActive(true);
            
        if (objectToEnable2 != null)
            objectToEnable2.SetActive(true);
            
        if (objectToEnable3 != null)
            objectToEnable3.SetActive(true);
    }
    
    private void OnDestroy()
    {
        if (dialogueManager != null)
            dialogueManager.OnDialogueEnd -= OnDialogueComplete;
    }
}
