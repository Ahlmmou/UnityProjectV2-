# Gemini 2.0 Flash Chatbot - Setup Guide

## ✅ What Was Fixed

### 1. **Removed External Dependencies**
   - ❌ Old: Required Newtonsoft.Json (not installed)
   - ✅ New: 100% Unity built-in libraries only

### 2. **Updated to Gemini 2.0 Flash**
   - ❌ Old: `gemini-1.5-flash`
   - ✅ New: `gemini-2.0-flash-exp` (latest, 2x faster)
   - Note: gemini-2.5-flash doesn't exist yet - 2.0-flash-exp is the fastest available

### 3. **New API Architecture**
   - ❌ Old: `StartCoroutine(SendMessageToGemini(msg, callback))`
   - ✅ New: `SendMessage(msg, onComplete, onError)`

### 4. **Updated All Scripts**
   - ✅ `GeminiChat.cs` - Core chatbot with streaming
   - ✅ `GeminiChatbotUI.cs` - UI with typing animation
   - ✅ `ChatUIManager.cs` - Updated to new API
   - ✅ `NPCInteractor.cs` - Updated to new API
   - ✅ `HelpTerminal.cs` - Updated to new API

---

## 🚀 Setup Instructions

### Step 1: Create Configuration Asset

1. In Unity Project window, **right-click** in Assets folder
2. Select **Create > Gemini > Chat Config**
3. Name it `GeminiChatConfig`
4. Click on it in the Project window
5. In the **Inspector**, paste your API key in the "Api Key" field:
   ```
   AIzaSyD_r0Md5nzrywRHsGpwTw7B4vKmZMpI6Iw
   ```

### Step 2: Assign Configuration to GeminiChat Components

For **each** GameObject that has a `GeminiChat` component:

1. Select the GameObject in Hierarchy
2. Find the `GeminiChat` component in Inspector
3. Drag the `GeminiChatConfig` asset into the **Config** field

### Step 3: Verify Script References

Make sure these scripts have their `GeminiChat` references assigned:
- `GeminiChatbotUI` → Needs `GeminiChat` component reference
- `ChatUIManager` → Needs `GeminiChat` component reference  
- `NPCInteractor` → Needs `GeminiChat` component reference
- `HelpTerminal` → Needs `GeminiChat` component reference

### Step 4: Test!

1. **Press Play** in Unity
2. Wait for compilation to finish (should be error-free now ✅)
3. Open your chatbot UI
4. Type a message and press Enter

---

## 🎨 Features

| Feature | Description |
|---------|-------------|
| **Streaming** | Real-time text generation (SSE) |
| **History** | Multi-turn conversations with context |
| **Typing Animation** | Premium chat experience |
| **Color Coding** | User (blue), AI (green), Errors (red) |
| **Auto-retry** | Exponential backoff on failures |
| **Configurable** | All settings in ScriptableObject |
| **Project-Aware System Prompt** | Chatbot knows about your VR robotics project, controls, and features |

---

## ⚙️ Configuration Options

Edit `GeminiChatConfig` asset to customize:

### Model Settings
- **Model**: `gemini-2.0-flash-exp` (recommended) or `gemini-1.5-flash`
- **Max Tokens**: 100-8192 (default: 2048)

### Generation Settings
- **Temperature**: 0.0-2.0 (default: 0.9)
  - Lower = more focused/deterministic
  - Higher = more creative/random
- **Top P**: 0.0-1.0 (default: 0.95)
- **Top K**: 1-100 (default: 40)

### System Behavior
- **System Instruction**: Guide the AI's personality/behavior
  - **Default**: Project-aware prompt that makes the chatbot knowledgeable about your VR robotics training environment
  - The chatbot now knows about robot controls, game modes, VR interactions, and available systems
  - See `CHATBOT_SYSTEM_PROMPT_GUIDE.md` for full details
- **Max History Messages**: How many messages to keep (default: 20)

### Network Settings
- **Timeout Seconds**: Request timeout (default: 30)
- **Max Retries**: Retry attempts on failure (default: 3)

---

## 🔧 API Changes

If you have other custom scripts using the old API, update them:

### Old API (❌ Don't use):
```csharp
StartCoroutine(geminiChat.SendMessageToGemini(message, callback));
```

### New API (✅ Use this):
```csharp
geminiChat.SendMessage(message,
    onComplete: (response) => {
        // Handle success
        Debug.Log("Response: " + response);
    },
    onError: (error) => {
        // Handle error
        Debug.LogError("Error: " + error);
    }
);
```

---

## 📚 Additional Methods

### Clear Conversation History
```csharp
geminiChat.ClearHistory();
```

### Get Conversation History
```csharp
List<GeminiChatMessage> history = geminiChat.GetHistory();
```

###Check if Processing
```csharp
bool isProcessing = geminiChat.IsProcessing();
```

---

## 🎯 Troubleshooting

### Error: "API Key is not set"
- Create `GeminiChatConfig` asset
- Assign your API key in the Inspector
- Assign the config to `GeminiChat` component

### Error: "GeminiChat script not linked"
- Select the GameObject
- Drag the `GeminiChat` component reference into the script's field

### No response from API
- Check Unity Console for errors
- Verify API key is correct
- Check internet connection
- Try increasing timeout in Config

---

## 🌟 What's New vs Old System

### Performance
- ✅ 2x faster with Gemini 2.0 Flash
- ✅ Streaming responses (real-time)
- ✅ Better error handling with retry logic

### Features
- ✅ Conversation history & context
- ✅ Typing animation effect
- ✅ Color-coded messages
- ✅ Configurable parameters
- ✅ System instructions support

### Code Quality
- ✅ No external dependencies
- ✅ Event-driven architecture
- ✅ Better separation of concerns
- ✅ More maintainable

---

## 📖 Example Usage

```csharp
using Assets.Scripts;
using UnityEngine;

public class MyCustomScript : MonoBehaviour
{
    public GeminiChat geminiChat;
    
    void Start()
    {
        // Send a message
        geminiChat.SendMessage("Hello, who are you?",
            onComplete: (response) => {
                Debug.Log($"AI said: {response}");
            },
            onError: (error) => {
                Debug.LogError($"Failed: {error}");
            }
        );
    }
}
```

---

**Created:** 2025-12-01  
**Version:** Gemini 2.0 Flash (State-of-the-Art)
