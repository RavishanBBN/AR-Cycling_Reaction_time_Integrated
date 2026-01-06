using System;
using UnityEngine;

public class ReactionTimeTracker : MonoBehaviour
{
    //ATTRIBUTES
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private float reactionTimeoutWindow = 6f; // 6 seconds window for valid reactions
    
    private CsvExporter _reactionTimeExporter;
    private float _lastButtonPressTime = -1f;
    private bool _waitingForResponse = false;
    private float _currentAudioPlayTime = -1f;
    
    [Header("Export Settings")]
    [SerializeField] private string exportFileName = "reaction-time";
    [SerializeField] private float exportInterval = 1f;

    //METHODS
    private void Awake()
    {
        var timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var reactionTimeFilePath = Application.persistentDataPath + $"/{exportFileName}_{timeStamp}.csv";
        const string csvHeader = "AudioPlayTime (s),ButtonPressTime (s),ReactionTime (ms),AudioType,ResponseType";
        
        _reactionTimeExporter = new CsvExporter(reactionTimeFilePath, exportInterval, csvHeader);
        
        Debug.Log($"Exporting reaction time data to {reactionTimeFilePath}");
    }

    private void Update()
    {
        CheckForAudioPlay();
        CheckForButtonPress();
        CheckForMissedResponse();
        
        _reactionTimeExporter.ExportRecentData();
    }

    private void CheckForAudioPlay()
    {
        if (audioManager == null) return;
        
        float audioPlayTime = audioManager.LastAudioPlayTime;
        
        // New audio has been played
        if (audioPlayTime > 0 && audioPlayTime != _currentAudioPlayTime)
        {
            _currentAudioPlayTime = audioPlayTime;
            _waitingForResponse = true;
        }
    }

    private void CheckForButtonPress()
    {
        // Detect 'A' key press from Bluetooth button
        if (Input.GetKeyDown(KeyCode.A))
        {
            float buttonPressTime = Time.time;
            
            // Check if this is a response to recent audio
            if (_waitingForResponse && (buttonPressTime - _currentAudioPlayTime) <= reactionTimeoutWindow)
            {
                // Valid reaction
                float reactionTime = (buttonPressTime - _currentAudioPlayTime) * 1000; // Convert to milliseconds
                
                _reactionTimeExporter.AddData(new ReactionTimeDatum
                {
                    AudioPlayTime = _currentAudioPlayTime,
                    ButtonPressTime = buttonPressTime,
                    ReactionTime = reactionTime,
                    AudioType = "Audio",
                    ResponseType = "ValidResponse"
                }.ToString());
                
                _waitingForResponse = false;
                _lastButtonPressTime = buttonPressTime;
                
                Debug.Log($"Valid reaction: {reactionTime:F2}ms");
            }
            else if (_currentAudioPlayTime > 0 && (buttonPressTime - _currentAudioPlayTime) > reactionTimeoutWindow)
            {
                // Late response (outside timeout window)
                float reactionTime = (buttonPressTime - _currentAudioPlayTime) * 1000;
                
                _reactionTimeExporter.AddData(new ReactionTimeDatum
                {
                    AudioPlayTime = _currentAudioPlayTime,
                    ButtonPressTime = buttonPressTime,
                    ReactionTime = reactionTime,
                    AudioType = "Audio",
                    ResponseType = "LateResponse"
                }.ToString());
                
                Debug.Log($"Late response: {reactionTime:F2}ms (outside {reactionTimeoutWindow}s window)");
            }
            else
            {
                // False positive (button pressed without recent audio)
                _reactionTimeExporter.AddData(new ReactionTimeDatum
                {
                    AudioPlayTime = _currentAudioPlayTime > 0 ? _currentAudioPlayTime : 0,
                    ButtonPressTime = buttonPressTime,
                    ReactionTime = -1,
                    AudioType = "None",
                    ResponseType = "FalsePositive"
                }.ToString());
                
                Debug.Log("False positive: Button pressed without audio");
            }
            
            _lastButtonPressTime = buttonPressTime;
        }
    }

    private void CheckForMissedResponse()
    {
        // Check if audio was played but no response within timeout window
        if (_waitingForResponse && (Time.time - _currentAudioPlayTime) > reactionTimeoutWindow)
        {
            _reactionTimeExporter.AddData(new ReactionTimeDatum
            {
                AudioPlayTime = _currentAudioPlayTime,
                ButtonPressTime = -1,
                ReactionTime = -1,
                AudioType = "Audio",
                ResponseType = "MissedResponse"
            }.ToString());
            
            _waitingForResponse = false;
            
            Debug.Log($"Missed response: No button press within {reactionTimeoutWindow}s");
        }
    }

    private void OnDestroy()
    {
        if (_reactionTimeExporter != null && _reactionTimeExporter.BufferCount > 0)
        {
            _reactionTimeExporter.ForceFlush();
        }
    }
}


internal record ReactionTimeDatum
{
    public float AudioPlayTime { get; set; }
    public float ButtonPressTime { get; set; }
    public float ReactionTime { get; set; }
    public string AudioType { get; set; }
    public string ResponseType { get; set; }

    public override string ToString()
    {
        return $"{AudioPlayTime},{ButtonPressTime},{ReactionTime:F2},{AudioType},{ResponseType}";
    }
}
