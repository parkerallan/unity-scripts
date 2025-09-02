# Save/Load Button Integration Guide

This guide shows you how to connect the save/load system with UI buttons in your pause menu and main menu.

## Button Setup in Unity Editor

### 1. Main Menu Buttons

#### Continue Button (Load Game)
1. **Find/Create the Button**: In your MainMenu, find or create a button named "ContinueButton"
2. **Set Button Text**: Change the text to "Continue" or "Load Game"
3. **Configure Button Component**:
   - Click the button GameObject
   - In the Inspector, find the `Button` component
   - In the `On Click ()` section, click the `+` button
   - Drag your **MenuManager** GameObject into the object field
   - In the dropdown, select `MenuManager > OnContinueButtonClicked()`

#### New Game Button (Enhanced)
1. **Find your New Game Button**
2. **Update Button Component**:
   - In the `On Click ()` section, click the `+` button
   - Drag your **MenuManager** GameObject into the object field
   - In the dropdown, select `MenuManager > OnNewGameButtonClicked()`

### 2. Pause Menu Buttons

#### Save Game Button
1. **Create/Find Save Button**: In your PauseMenu, create a button named "SaveButton"
2. **Set Button Text**: Change the text to "Save Game"
3. **Configure Button Component**:
   - In the `On Click ()` section, click the `+` button
   - Drag your **MenuManager** GameObject into the object field
   - In the dropdown, select `MenuManager > OnSaveButtonClicked()`

#### Load Game Button
1. **Create/Find Load Button**: In your PauseMenu, create a button named "LoadButton"
2. **Set Button Text**: Change the text to "Load Game"
3. **Configure Button Component**:
   - In the `On Click ()` section, click the `+` button
   - Drag your **MenuManager** GameObject into the object field
   - In the dropdown, select `MenuManager > OnLoadButtonClicked()`

## Button Naming Convention

For automatic button state management, name your buttons exactly as follows:
- **"ContinueButton"** - Main menu continue/load button
- **"LoadButton"** - Pause menu load button
- **"SaveButton"** - Pause menu save button
- **"NewGameButton"** - Main menu new game button

## Automatic Button State Management

The system automatically:
- **Enables/Disables Continue Button**: Based on save file existence
- **Enables/Disables Load Button**: Based on save file existence
- **Always Enables Save Button**: Players can always save
- **Always Enables New Game Button**: Players can always start fresh

## Visual Setup Example

### Main Menu Layout
```
MainMenu (GameObject)
├── TitleText
├── NewGameButton        → OnNewGameButtonClicked()
├── ContinueButton       → OnContinueButtonClicked() [Auto-disabled if no save]
├── SettingsButton       → OpenSettingsMenu()
├── CreditsButton        → OpenCreditsMenu()
└── ExitButton           → OpenExitConfirmationModal()
```

### Pause Menu Layout
```
PauseMenu (GameObject)
├── ResumeButton         → ClosePauseMenu()
├── SaveButton           → OnSaveButtonClicked()
├── LoadButton           → OnLoadButtonClicked() [Auto-disabled if no save]
├── SettingsButton       → OpenSettingsMenu()
└── ExitToMainButton     → [Your existing method]
```

## Step-by-Step Button Connection

### For Each Button:
1. **Select the Button** in the hierarchy
2. **Find the Button Component** in Inspector
3. **Scroll to On Click ()** section
4. **Click the + button** to add a new event
5. **Drag MenuManager** from hierarchy to the object field
6. **Click the dropdown** (currently shows "No Function")
7. **Navigate to MenuManager** and select the appropriate method:
   - `OnSaveButtonClicked()` for Save buttons
   - `OnLoadButtonClicked()` for Load buttons
   - `OnContinueButtonClicked()` for Continue buttons
   - `OnNewGameButtonClicked()` for New Game buttons

## Advanced Button Configuration

### Custom Button Styling Based on Save State

You can enhance the visual feedback by checking save file status:

```csharp
// In your UI script
void Start()
{
    MenuManager menuManager = FindAnyObjectByType<MenuManager>();
    bool hasSave = menuManager.HasSaveFile();
    
    // Style continue button based on save existence
    Button continueBtn = GameObject.Find("ContinueButton").GetComponent<Button>();
    if (hasSave)
    {
        continueBtn.interactable = true;
        // Set normal colors
    }
    else
    {
        continueBtn.interactable = false;
        // Set grayed out colors
    }
}
```

### Button Text Updates

You can dynamically update button text:

```csharp
// Show save date on continue button
SaveData saveInfo = SaveManager.Instance.GetSaveInfo();
if (saveInfo != null)
{
    continueButtonText.text = $"Continue ({saveInfo.saveDateTime})";
}
```

## Testing the Integration

### Test Save Button:
1. Start the game
2. Move around, change health/ammo
3. Press Escape to open pause menu
4. Click "Save Game" button
5. Should see green "Game Saved" notification

### Test Load Button:
1. With a save file existing
2. Press Escape to open pause menu
3. Click "Load Game" button
4. Should see blue "Game Loaded" notification
5. Player should return to saved position

### Test Continue Button:
1. From main menu with save file existing
2. Click "Continue" button
3. Should load directly into saved game state

### Test Button States:
1. **With Save File**: Continue and Load buttons should be enabled
2. **Without Save File**: Continue and Load buttons should be grayed out
3. **After Deleting Save**: Buttons should automatically update to disabled

## Troubleshooting Button Issues

### Button Not Responding:
1. Check that MenuManager is assigned in the button's On Click event
2. Verify the correct method is selected (OnSaveButtonClicked, etc.)
3. Ensure MenuManager GameObject is active in the scene

### Button Not Enabling/Disabling:
1. Check button naming matches exactly: "ContinueButton", "LoadButton"
2. Verify UpdateMenuButtonStates() is called in MenuManager.Start()
3. Check that SaveManager is assigned to MenuManager

### Save/Load Not Working:
1. Verify SaveManager is in the scene
2. Check console for error messages
3. Ensure Player has "Player" tag
4. Verify Target, GunScript, RifleScript components on player

### Visual Issues:
1. Check button Interactable checkbox in Inspector
2. Verify Button component is present (not just Image)
3. Check button's Navigation settings if keyboard navigation is needed

## Button Event Flow

```
User Clicks Save Button
    ↓
OnSaveButtonClicked() called
    ↓
SaveGame() method executed
    ↓
SaveManager.SaveGame() called
    ↓
JSON file written to disk
    ↓
SaveLoadUI shows "Game Saved" notification
```

```
User Clicks Load Button
    ↓
OnLoadButtonClicked() called
    ↓
LoadGame() method executed
    ↓
SaveManager.LoadGame() called
    ↓
Scene loaded with saved data
    ↓
Player positioned and stats restored
    ↓
SaveLoadUI shows "Game Loaded" notification
```

## Integration with Existing Menu System

The save/load buttons work seamlessly with your existing menu system:
- **Respects pause state**: Game properly pauses/resumes
- **Menu navigation**: Works with Escape key navigation
- **Error handling**: Shows appropriate notifications for failures
- **State management**: Properly manages menu visibility during loading

Your existing menu methods like `OpenSettingsMenu()`, `ClosePauseMenu()`, etc. remain unchanged and fully compatible.
