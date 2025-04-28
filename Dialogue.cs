using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Dialogue
{
    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText; // Text for the choice
        public int nextLineIndex; // Index of the next line in the dialogue (-1 to end dialogue)
        public UnityEngine.Events.UnityEvent onChoiceSelected; // Optional event to call a function
    }

    [System.Serializable]
    public class DialogueLine
    {
        public string speaker; // Name of the speaker
        [TextArea(3, 10)]
        public string sentence; // The sentence they say
        public Sprite characterImage; // Character image
        public bool faceRight; // Orientation: true = face right, false = face left
        public List<DialogueChoice> choices; // List of choices for this line
    }

    public List<DialogueLine> lines; // List of dialogue lines
}
