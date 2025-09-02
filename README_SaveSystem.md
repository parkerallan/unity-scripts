# Save/Load System Documentation

This document explains how to use the comprehensive save/load system for your Unity game.

## System Overview

The save/load system consists of three main components:

1. **SaveManager.cs** - Core save/load functionality
2. **SaveLoadUI.cs** - User interface notifications
3. **MenuManager.cs** - Integration with game menus

## Features

### What Gets Saved
- **Player Position**: X, Y, Z coordinates and Y rotation
- **Current Scene**: Name of the scene the player is in
- **Player Health**: Current and maximum health values
- **Weapon Status**: Whether player has gun/rifle and which is active
- **Gun Ammo**: Current magazine ammo, reserve ammo, and max magazine size
- **Rifle Ammo**: Current magazine ammo, reserve ammo, and max magazine size
- **Save Metadata**: Date/time of save and total play time

### Save File Format
The system uses JSON format stored in `Application.persistentDataPath/gamesave.json`:

```json
{
    "currentScene": "Home",
    "playerPosX": 10.5,
    "playerPosY": 2.0,
    "playerPosZ": -5.2,
    "playerRotY": 45.0,
    "playerHealth": 85.0,
    "playerMaxHealth": 100.0,
    "hasGun": true,
    "hasRifle": true,
    "gunActive": true,
    "rifleActive": false,
    "gunCurrentAmmo": 15,
    "gunReserveAmmo": 120,
    "gunMaxAmmo": 20,
    "rifleCurrentAmmo": 25,
    "rifleReserveAmmo": 180,
    "rifleMaxAmmo": 30,
    "saveDateTime": "2025-09-01 14:30:25",
    "timePlayed": 1845.5
}
```

## Setup Instructions

### 1. SaveManager Setup
1. Create an empty GameObject in your scene
2. Add the `SaveManager` component
3. Configure the settings:
   - **Save File Name**: Default is "gamesave.json"
   - **Enable Debug Logs**: Check for detailed logging
   - **Auto Save Enabled**: Check to enable automatic saving
   - **Auto Save Interval**: Time in seconds between auto-saves (default: 300s = 5 minutes)

### 2. SaveLoadUI Setup (Optional but Recommended)
1. Create a UI Canvas for notifications
2. Create a Panel GameObject as a child of the Canvas
3. Add a TextMeshPro component for the notification text
4. Add an Image component for the background
5. Add the `SaveLoadUI` component to a GameObject
6. Assign the UI references:
   - **Notification Panel**: The panel GameObject
   - **Notification Text**: The TextMeshPro component
   - **Notification Background**: The Image component
7. Configure colors and timing as desired

### 3. MenuManager Integration
1. Find your existing MenuManager component
2. Assign the SaveManager reference in the "Save/Load System" section
3. The system will automatically find SaveManager if not assigned

## Usage

### Manual Save/Load
Call these methods from MenuManager or any other script:

```csharp
// Save the game
MenuManager.Instance.SaveGame();

// Load the game
MenuManager.Instance.LoadGame();

// Check if save file exists
bool hasSave = MenuManager.Instance.HasSaveFile();

// Delete save file
MenuManager.Instance.DeleteSave();
```

### Direct SaveManager Access
```csharp
// Quick save
SaveManager.Instance.QuickSave();

// Quick load
SaveManager.Instance.QuickLoad();

// Get save info without loading
SaveData info = SaveManager.Instance.GetSaveInfo();
```

### Keyboard Shortcuts
The system includes built-in keyboard shortcuts:
- **F5**: Quick Save (when not in menus)
- **F9**: Quick Load (when not in menus)

### Auto-Save
Auto-save runs automatically based on the interval setting. Players will see a yellow "Auto-Saved" notification when it occurs.

## UI Notifications

The SaveLoadUI system provides automatic notifications:
- **Green "Game Saved"**: Successful save
- **Blue "Game Loaded"**: Successful load
- **Yellow "Auto-Saved"**: Auto-save occurred
- **Red "Save Failed"**: Save operation failed
- **Red "Load Failed"**: Load operation failed
- **Red "No Save File Found"**: Attempted to load with no save file

## Technical Details

### Player Requirements
The system expects:
- Player GameObject with "Player" tag
- Target component for health data
- GunScript component for gun data (if player has gun)
- RifleScript component for rifle data (if player has rifle)
- CharacterController component (handled safely if present)

### Scene Loading
The system uses `ProgrammaticBuildingEntry.LoadScene()` for scene transitions during loading, ensuring proper player positioning and scene setup.

### Error Handling
- Comprehensive try-catch blocks prevent crashes
- Detailed debug logging for troubleshooting
- Graceful fallbacks when components are missing
- UI notifications for all error states

### Performance Considerations
- JSON serialization is lightweight and fast
- Auto-save only occurs at specified intervals
- Save operations are non-blocking
- Loading scenes is handled asynchronously

## Extending the System

### Adding New Data
To save additional data, modify the `SaveData` class:

1. Add new fields to `SaveData` class
2. Update `CollectGameData()` method to collect the data
3. Update `ApplyDataToCurrentScene()` method to apply the data

Example:
```csharp
// In SaveData class
public int playerScore;

// In CollectGameData()
ScoreManager scoreManager = FindAnyObjectByType<ScoreManager>();
if (scoreManager != null)
{
    saveData.playerScore = scoreManager.GetScore();
}

// In ApplyDataToCurrentScene()
ScoreManager scoreManager = FindAnyObjectByType<ScoreManager>();
if (scoreManager != null)
{
    scoreManager.SetScore(saveData.playerScore);
}
```

### Custom Save Locations
To save in different locations, modify the `savePath` variable in SaveManager's `Awake()` method:

```csharp
// Save to Documents folder
savePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "MyGame", saveFileName);

// Save to custom directory
savePath = Path.Combine("C:/MyGameSaves", saveFileName);
```

## Troubleshooting

### Common Issues

1. **Player not found**: Ensure Player GameObject has "Player" tag
2. **Save file not loading**: Check file permissions in persistentDataPath
3. **Scene not loading**: Verify scene name matches exactly
4. **Ammo not saving**: Ensure GunScript/RifleScript components are attached
5. **Health not saving**: Ensure Target component is attached to player

### Debug Information
Enable debug logs in SaveManager to see detailed information about:
- What data is being collected
- Where files are being saved
- Scene loading progress
- Component detection results

### Console Commands for Testing
You can test the system in the Unity console:
```csharp
// Force save
SaveManager.Instance.SaveGame();

// Force load
SaveManager.Instance.LoadGame();

// Show save location
Debug.Log(Application.persistentDataPath);
```

## Security Considerations

The save system stores data in plain text JSON. For production games, consider:
- Encrypting save data
- Validating loaded data for cheating prevention
- Cloud save integration for cross-platform play
- Multiple save slots
- Save file versioning for updates
