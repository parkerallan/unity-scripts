# Unity Game Scripts - Function Reference

List of provided functions and systems for Unity game development.  
*Box collision-based triggers for these functions are also included*

## Character Dialogue
*Examples provided below*

```csharp
// Basic dialogue functions
dialogueTrigger.StartSimpleDialogue(Name, Dialogue);
dialogueTrigger.StartMultiLineDialogue(Speakers[], Sentences[]);
dialogueTrigger.StartDialogueWithChoices(Name, Sentence, Choices[], Actions[]);

// With character images (optional)
dialogueTrigger.StartSimpleDialogue(Name, Dialogue, "Images/Characters/Guard");
dialogueTrigger.StartMultiLineDialogue(Speakers[], Sentences[], CharacterImagePaths[]);
dialogueTrigger.StartDialogueWithChoices(Name, Sentence, Choices[], Actions[], "Images/Characters/QuestGiver");

// With character images and orientation (optional)
dialogueTrigger.StartSimpleDialogue(Name, Dialogue, "Images/Characters/Guard", faceRight: false);
dialogueTrigger.StartMultiLineDialogue(Speakers[], Sentences[], CharacterImagePaths[], FaceRightFlags[]);
dialogueTrigger.StartDialogueWithChoices(Name, Sentence, Choices[], Actions[], "Images/Characters/QuestGiver", faceRight: true);

// Delayed dialogue (useful for chaining)
dialogueTrigger.StartDelayedDialogue(Name, Dialogue, delay, "Images/Characters/Boss", faceRight: false);
dialogueTrigger.StartDelayedDialogueWithChoices(Name, Sentence, Choices[], Actions[], delay, "Images/Characters/Merchant", faceRight: true);
```

## Building Entry

```csharp
buildingEntry.EnterBuilding(Scene, SpawnPoint);
```

## Items

```csharp
flashlight.SetActive(bool);
```

## Weapons

```csharp
weaponManager.DisableWeapons();
weaponManager.EnableWeapons();

weaponManager.SetObtainedGun(bool);
weaponManager.SetObtainedRifle(bool);
```

## Music System

```csharp
musicTrigger.StartSong(audioPath, looping); // looping: 1 = true, 0 = false
musicTrigger.StopMusic();
musicTrigger.PauseMusic();
musicTrigger.ResumeMusic();
musicTrigger.SetVolume(float); // 0.0 to 1.0
```

## UI Elements

```csharp
healthbarUILayer.SetActive(bool);
```

---

## Dialogue Examples

### Example 1: Simple one-line dialogue

```csharp
public void StartIntroDialogue()
{
    dialogueTrigger.StartSimpleDialogue("Narrator", "Welcome to the dungeon");
}

// With character image
public void StartIntroDialogueWithImage()
{
    dialogueTrigger.StartSimpleDialogue("Narrator", "Welcome to the dungeon", "Images/Characters/Narrator");
}
```

### Example 2: Simple dialogue

```csharp
public void StartSimpleDialogueExample()
{
    // Character facing right (default)
    dialogueTrigger.StartSimpleDialogue("Guard", "Halt! Who goes there?", "Images/Characters/Guard");
    
    // Character facing left
    dialogueTrigger.StartSimpleDialogue("Guard", "Move along!", "Images/Characters/Guard", faceRight: false);
}
```

### Example 3: Multi-line conversation

```csharp
public void StartMultiLineDialogueExample()
{
    string[] speakers = { 
        "Merchant", 
        "Player", 
        "Merchant", 
        "Player" 
    };
    
    string[] sentences = { 
        "Greetings, traveler! Care to buy some potions?",
        "What do you have for sale?",
        "I have health potions for 50 gold each!",
        "I'll take two, please."
    };
    
    // Optional: Add character images for each line
    string[] characterImages = {
        "Images/Characters/Merchant",
        "Images/Characters/Player", 
        "Images/Characters/Merchant",
        "Images/Characters/Player"
    };
    
    // Optional: Set character orientation for each line
    bool[] faceRightFlags = {
        true,   // Merchant faces right
        false,  // Player faces left (towards merchant)
        true,   // Merchant faces right
        false   // Player faces left
    };
    
    dialogueTrigger.StartMultiLineDialogue(speakers, sentences, characterImages, faceRightFlags);
}
```

### Example 4: Dialogue with choices

```csharp
public void StartChoiceDialogueExample()
{
    string[] choices = { 
        "Accept the quest", 
        "Ask for more information", 
        "Decline politely" 
    };
    
    System.Action[] actions = { 
        AcceptQuest, 
        AskForInfo, 
        DeclineQuest 
    };
    
    dialogueTrigger.StartDialogueWithChoices(
        "Quest Giver", 
        "I need someone brave to retrieve my stolen artifact. Will you help me?", 
        choices, 
        actions,
        "Images/Characters/QuestGiver"
    );
}
```

### Example 5: Boss encounter with complex choices

```csharp
public void StartBossEncounterDialogue()
{
    string[] choices = { 
        "Challenge the boss to combat", 
        "Try to steal the sword", 
        "Attempt negotiation" 
    };
    
    System.Action[] actions = { 
        StartBossFight, 
        NegotiateWithBoss 
    };
    
    dialogueTrigger.StartDialogueWithChoices(
        "Dark Lord", 
        "So, you've finally arrived. The legendary sword lies before you. What will you do, mortal?", 
        choices, 
        actions,
        "Images/Characters/DarkLord"
    );
}
```

---

## Choice Action Examples

### Accept Quest

```csharp
private void AcceptQuest()
{
    Debug.Log("GameManager: Quest accepted!");
    dialogueTrigger.StartDelayedDialogue("Quest Giver", "Excellent! The artifact is hidden in the old ruins to the north.");
    
    // Add quest to player's journal, enable quest markers, etc.
    // AddQuestToJournal("Retrieve the Lost Artifact");
}
```

### Ask for Information

```csharp
private void AskForInfo()
{
    Debug.Log("GameManager: Asking for more information");
    
    string[] speakers = { "Quest Giver", "Player", "Quest Giver" };
    string[] sentences = { 
        "The artifact is a magical crystal that was stolen by bandits.",
        "How dangerous are these bandits?",
        "They're well-armed, but I believe in your abilities!"
    };
    
    // Use coroutine to delay multi-line dialogue
    StartCoroutine(DelayedMultiLineDialogue(speakers, sentences, 0.5f));
}
```

### Decline Quest

```csharp
private void DeclineQuest()
{
    Debug.Log("GameManager: Quest declined");
    dialogueTrigger.StartDelayedDialogue("Quest Giver", "I understand. Perhaps another time...");
}
```

### Start Boss Fight

```csharp
private void StartBossFight()
{
    Debug.Log("GameManager: Starting boss fight!");
    dialogueTrigger.StartDelayedDialogue("Dark Lord", "So be it! Face me in combat!");
    
    // Start boss fight sequence
    // EnableBossHealthBar();
    // StartBattleMusic();
    // SetGameState(GameState.BossFight);
}
```

### Negotiate with Boss

```csharp
private void NegotiateWithBoss()
{
    Debug.Log("GameManager: Attempting negotiation");
    
    string[] choices = { 
        "Offer to serve the Dark Lord", 
        "Propose a trade", 
        "Challenge him to a riddle contest" 
    };
    
    System.Action[] actions = { 
        JoinDarkLord, 
        ProposeTradeWithBoss, 
        StartRiddleContest 
    };
    
    // Use delayed dialogue to avoid conflict with current dialogue
    dialogueTrigger.StartDelayedDialogueWithChoices(
        "Dark Lord", 
        "Interesting... You seek to bargain rather than fight. Very well, I'm listening.", 
        choices, 
        actions,
        0.5f
    );
}
```

### Join Dark Lord

```csharp
private void JoinDarkLord()
{
    Debug.Log("GameManager: Joining the Dark Lord!");
    dialogueTrigger.StartDelayedDialogue("Dark Lord", "Welcome to the dark side! Here, take this cursed blade as a sign of your allegiance.");
    
    // Give player dark sword, change ending path, etc.
    // GivePlayerItem("Cursed Blade");
    // SetEnding(EndingType.DarkPath);
}
```

### Propose Trade with Boss

```csharp
private void ProposeTradeWithBoss()
{
    Debug.Log("GameManager: Proposing a trade");
    dialogueTrigger.StartDelayedDialogue("Dark Lord", "A trade? Hmm... Bring me the Crown of Kings, and the sword is yours.");
    
    // Add new quest objective
    // AddQuestObjective("Retrieve the Crown of Kings");
}
```

### Start Riddle Contest

```csharp
private void StartRiddleContest()
{
    Debug.Log("GameManager: Starting riddle contest!");
    dialogueTrigger.StartDelayedDialogue("Dark Lord", "A battle of wits? How amusing! Very well...");
    
    // Start riddle mini-game
    // StartRiddleGame();
}
```

---

## Utility Methods

### Coroutine for Delayed Multi-line Dialogue

```csharp
private System.Collections.IEnumerator DelayedMultiLineDialogue(string[] speakers, string[] sentences, float delay)
{
    yield return new WaitForSeconds(delay);
    dialogueTrigger.StartMultiLineDialogue(speakers, sentences);
}
```

---

## Character Images Setup

To use character images in dialogues, place your sprite files in a **Resources** folder:

```
Assets/
├── Resources/           ← Required folder name
│   └── Images/
│       └── Characters/
│           ├── Guard.png
│           ├── Merchant.png
│           ├── QuestGiver.png
│           └── DarkLord.png
└── Scripts/
```

**Usage Examples:**
```csharp
// Simple dialogue with character image
dialogueTrigger.StartSimpleDialogue("Guard", "Halt!", "Images/Characters/Guard");

// Character facing left
dialogueTrigger.StartSimpleDialogue("Guard", "Move along!", "Images/Characters/Guard", faceRight: false);

// Multi-line with different characters and orientations
string[] speakers = {"Guard", "Player"};
string[] sentences = {"Stop right there!", "I'm just passing through."};
string[] images = {"Images/Characters/Guard", "Images/Characters/Player"};
bool[] facings = {true, false}; // Guard faces right, Player faces left
dialogueTrigger.StartMultiLineDialogue(speakers, sentences, images, facings);
```

**Notes:**
- Character images are **optional** - all dialogue functions work without them
- Character orientation is **optional** - defaults to facing right (true)  
- Images must be in a **Resources** folder for the system to find them
- Supported formats: PNG, JPG, etc. (standard Unity sprite formats)
- The system automatically handles path conversion for Resources.Load()
- `faceRight: true` = character appears on right side, `faceRight: false` = character appears on left side