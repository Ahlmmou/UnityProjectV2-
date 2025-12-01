# Voice Input Setup Guide for Gemini Chatbot

## Overview
This guide explains how to add state-of-the-art voice input functionality to your Gemini Chatbot in Unity VR.

## Features
✨ **Real-time voice recording** with microphone support  
🎯 **Google Speech-to-Text API** integration  
📊 **Visual feedback** with recording indicator and pulsing button animation  
🔇 **Silence detection** - automatically stops recording after silence  
🌍 **Multi-language support** - configure any language code  
🎨 **Modern UI** - purple/magenta themed voice button with animations  
⚡ **Error handling** - comprehensive error messages and retry logic  

## Quick Setup (5 Steps)

### 1. Add VoiceInputManager Component
Add the `VoiceInputManager` component to the same GameObject that has your `GeminiChatbotUI`:

```
1. Select your Chatbot UI GameObject in the hierarchy
2. Click "Add Component"
3. Search for "VoiceInputManager"
4. The component will auto-discover the GeminiChatConfig
```

### 2. Create Voice Input Button in Your UI
Add a new button to your chatbot UI canvas:

```
1. Right-click on your Canvas → UI → Button - TextMeshPro
2. Rename it to "VoiceInputButton"
3. Set the button text to "🎤 Voice" or just "🎤"
4. Position it next to your Send button
```

### 3. (Optional) Create Recording Indicator
Add visual feedback for recording status:

```
1. Right-click on your Canvas → UI → Text - TextMeshPro
2. Rename it to "RecordingIndicator"
3. Set initial text to "Recording..."
4. Place it above the input field
5. The script will automatically hide/show it
```

### 4. Connect References in Inspector
Select your Chatbot UI GameObject and assign:

- **Voice Input Button** → Drag your VoiceInputButton
- **Recording Indicator** → Drag your RecordingIndicator (optional)
- **Voice Input Manager** → Should auto-assign, or drag manually

### 5. Configure Voice Settings (Optional)
On the `VoiceInputManager` component, you can customize:

- **Language Code**: Default is "en-US", change to:
  - "fr-FR" for French
  - "ar-SA" for Arabic
  - "es-ES" for Spanish
  - "de-DE" for German
  - etc.
- **Max Recording Duration**: Default 30 seconds
- **Sample Rate**: Default 16000 Hz (recommended)
- **Silence Threshold**: Default 0.01 (sensitivity for detecting silence)
- **Silence Detection Duration**: Default 2 seconds (auto-stop after silence)

## How to Use (User Perspective)

1. **Click the Voice Button (🎤)** to start recording
2. **Speak your message** - you'll see:
   - Button pulsing red (recording active)
   - "Recording..." indicator with progress
3. **Click again to stop**, or wait for auto-stop after silence
4. **Wait for processing** - "Processing..." indicator shows
5. **View transcription** in input field - ready to send or edit!

## Technical Details

### API Requirements
- Uses the **same Google API key** as your Gemini chatbot
- Google Speech-to-Text API must be enabled in your Google Cloud Console
- No additional API key needed (reuses GeminiChatConfig.ApiKey)

### Enable Speech-to-Text API (if not already enabled)
1. Go to https://console.cloud.google.com/
2. Select your project (same one used for Gemini API)
3. Enable "Cloud Speech-to-Text API"
4. Your existing API key will work for both services

### Audio Processing
- Records in **LINEAR16 (WAV)** format
- Sample rate: **16000 Hz** (optimal for speech recognition)
- Converts AudioClip → WAV → Base64 for API transmission
- Supports **mono and stereo** microphones

### Silence Detection
The system intelligently detects when you've stopped speaking:
- Calculates RMS (Root Mean Square) of audio samples
- Compares against silence threshold
- Auto-stops recording after configured silence duration
- Minimum recording time prevents accidental clicks

## Customization

### Auto-Send After Transcription
In `GeminiChatbotUI.cs`, find the `OnVoiceTranscriptionComplete` method and uncomment:
```csharp
// Optionally auto-send the message (uncomment if desired)
OnSendClicked();
```

### Change Voice Button Color
In `GeminiChatbotUI.cs`, in the `ApplyModernStyle()` method:
```csharp
// Change the color here (currently purple/magenta)
btnBg.color = new Color(0.6f, 0.2f, 0.8f, 1.0f);
```

### Change Pulse Animation
In `GeminiChatbotUI.cs`, in the `PulseVoiceButton()` method:
```csharp
Color pulseColor = new Color(1f, 0.2f, 0.2f, 1f); // Change this
float pulseSpeed = 2f; // Change animation speed
```

### Add Multiple Language Support
Create a dropdown to switch languages:
```csharp
voiceInputManager.SetLanguage("fr-FR"); // Switch to French
voiceInputManager.SetLanguage("ar-SA"); // Switch to Arabic
```

### Change Microphone Device
```csharp
string[] devices = voiceInputManager.GetMicrophoneDevices();
voiceInputManager.SetMicrophoneDevice(devices[0]);
```

## Troubleshooting

### "No microphone detected"
- Ensure a microphone is connected and enabled
- Check Windows Privacy Settings → Microphone → Allow apps to access
- Unity may need to be restarted after connecting a microphone

### "API key not configured"
- Make sure GeminiChatConfig has a valid API key
- The same API key works for both Gemini and Speech-to-Text
- Check that the VoiceInputManager has a reference to the config

### "Speech recognition failed"
- Verify Speech-to-Text API is enabled in Google Cloud Console
- Check your internet connection
- Ensure you're not exceeding API quotas
- Try recording for at least 1 second

### "No speech detected"
- Speak louder or closer to the microphone
- Reduce background noise
- Lower the `silenceThreshold` value (make it more sensitive)
- Check microphone input levels in Windows Sound Settings

### Recording indicator not showing
- Make sure you've assigned the Recording Indicator in the inspector
- Check that the TextMeshPro component is on the indicator
- Verify the indicator GameObject is a child of the Canvas

### Voice button not working
- Check that the button has the onClick listener assigned
- Verify VoiceInputManager component is added
- Look for errors in the Unity Console

## Events API (For Advanced Users)

The `VoiceInputManager` exposes events you can subscribe to:

```csharp
voiceInputManager.OnRecordingStarted += () => {
    Debug.Log("Recording started!");
};

voiceInputManager.OnRecordingStopped += () => {
    Debug.Log("Recording stopped!");
};

voiceInputManager.OnRecordingProgress += (progress) => {
    Debug.Log($"Progress: {progress * 100}%");
};

voiceInputManager.OnTranscriptionComplete += (text) => {
    Debug.Log($"Transcribed: {text}");
};

voiceInputManager.OnTranscriptionError += (error) => {
    Debug.LogError($"Error: {error}");
};
```

## Performance Considerations

- **Audio file size**: ~30KB per second of recording
- **API latency**: Typically 1-3 seconds for transcription
- **Memory usage**: Minimal, audio is cleaned up after processing
- **Network**: Requires stable internet for API calls

## Best Practices

1. ✅ **Keep recordings short** (under 30 seconds)
2. ✅ **Test with different microphones** (built-in, USB, Bluetooth)
3. ✅ **Provide clear visual feedback** during recording
4. ✅ **Handle errors gracefully** with user-friendly messages
5. ✅ **Test in your VR environment** (some VR headsets have built-in mics)
6. ✅ **Cache microphone permission** (ask once, remember preference)

## VR-Specific Considerations

### Quest/Meta Headsets
- Built-in microphones are supported
- May need to enable microphone permission in Oculus settings
- Test latency - VR users expect instant feedback

### PC VR (Vive, Index, etc.)
- Headset microphones work automatically
- Desktop microphones can be used as fallback
- Check SteamVR audio settings

### Testing in Unity Editor
- Works perfectly in editor with desktop microphone
- Same code runs in VR builds without changes
- Use "Device Simulator" for mobile VR testing

## Future Enhancements (Optional)

Ideas for extending the voice input system:

1. **Push-to-talk mode** - Hold button instead of toggle
2. **Voice commands** - "Send", "Clear", "Stop" commands
3. **Waveform visualization** - Show audio levels in real-time
4. **Offline recognition** - Using Unity's built-in speech recognition
5. **Noise cancellation** - Filter background noise before sending
6. **Multi-language auto-detection** - Automatically detect spoken language
7. **Voice profiles** - Save user voice preferences

## License & Credits

This voice input system uses:
- Google Cloud Speech-to-Text API
- Unity Microphone API
- Custom WAV encoding implementation

Developed for state-of-the-art Unity VR chatbot integration.

---

**Need help?** Check the Unity Console for detailed debug logs prefixed with `[VoiceInputManager]` and `[GeminiChatbotUI]`.
