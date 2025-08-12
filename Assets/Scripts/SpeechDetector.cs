using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SpeechDetector : MonoBehaviour
{
    [Header("Speech Detection Settings")]
    [SerializeField] private float speechThreshold = 0.02f; // minimum volume to consider as potential speech
    [SerializeField] private float speechStartThreshold = 0.05f; // volume needed to start speech detection
    [SerializeField] private float speechEndThreshold = 0.01f; // volume below which speech is considered ended
    [SerializeField] private float minSpeechDuration = 0.3f; // minimum duration to consider as speech (seconds)
    [SerializeField] private float maxSilenceDuration = 0.5f; // max silence before ending speech detection (seconds)
    
    [Header("Frequency Analysis")]
    [SerializeField] private bool useFrequencyAnalysis = true;
    [SerializeField] private float minSpeechFrequency = 80f; // Hz - typical human speech starts around 85Hz
    [SerializeField] private float maxSpeechFrequency = 8000f; // Hz - most speech content is below 8kHz
    [SerializeField] private int fftSize = 1024; // FFT window size for frequency analysis
    
    [Header("Smoothing")]
    [SerializeField] private int smoothingWindowSize = 5; // number of samples to smooth over
    [SerializeField] private float detectionSensitivity = 1.0f; // multiplier for detection sensitivity
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool drawFrequencySpectrum = false;
    
    // Events
    public System.Action OnSpeechStarted;
    public System.Action OnSpeechEnded;
    public System.Action<float> OnSpeechIntensityChanged; // 0-1 intensity of detected speech
    
    // Private variables
    private MicrophoneVolumeMonitor micMonitor;
    private bool isSpeechDetected = false;
    private float speechStartTime = 0f;
    private float lastSpeechTime = 0f;
    private Queue<float> volumeHistory = new Queue<float>();
    private Queue<float> speechFrequencyHistory = new Queue<float>();
    
    // Frequency analysis
    private float[] spectrumData;
    private float[] windowFunction;
    private float currentSpeechFrequencyRatio = 0f;
    
    // Properties
    public bool IsSpeechDetected => isSpeechDetected;
    public float CurrentSpeechIntensity { get; private set; } = 0f;
    public float SpeechDuration => isSpeechDetected ? Time.time - speechStartTime : 0f;
    public float SpeechFrequencyRatio => currentSpeechFrequencyRatio;
    
    void Start()
    {
        InitializeSpeechDetection();
    }
    
    void InitializeSpeechDetection()
    {
        // Get reference to microphone monitor
        micMonitor = GetComponent<MicrophoneVolumeMonitor>();
        if (micMonitor == null)
        {
            micMonitor = FindObjectOfType<MicrophoneVolumeMonitor>();
        }
        
        if (micMonitor == null)
        {
            Debug.LogError("SpeechDetector requires a MicrophoneVolumeMonitor component!");
            enabled = false;
            return;
        }
        
        // Subscribe to volume changes
        micMonitor.OnVolumeChanged += ProcessVolumeForSpeech;
        
        // Initialize frequency analysis
        if (useFrequencyAnalysis)
        {
            spectrumData = new float[fftSize];
            InitializeWindowFunction();
        }
        
        if (showDebugInfo)
        {
            Debug.Log("Speech detection initialized");
        }
    }
    
    void InitializeWindowFunction()
    {
        // Create Hamming window for better frequency analysis
        windowFunction = new float[fftSize];
        for (int i = 0; i < fftSize; i++)
        {
            windowFunction[i] = 0.54f - 0.46f * Mathf.Cos(2f * Mathf.PI * i / (fftSize - 1));
        }
    }
    
    void ProcessVolumeForSpeech(float volume)
    {
        // Apply sensitivity multiplier
        volume *= detectionSensitivity;
        
        // Add to volume history for smoothing
        volumeHistory.Enqueue(volume);
        if (volumeHistory.Count > smoothingWindowSize)
        {
            volumeHistory.Dequeue();
        }
        
        // Calculate smoothed volume
        float smoothedVolume = volumeHistory.Average();
        
        // Perform frequency analysis if enabled
        float speechFrequencyScore = 1f; // default to 1 if not using frequency analysis
        if (useFrequencyAnalysis && micMonitor.IsRecording)
        {
            speechFrequencyScore = AnalyzeSpeechFrequencies();
            speechFrequencyHistory.Enqueue(speechFrequencyScore);
            if (speechFrequencyHistory.Count > smoothingWindowSize)
            {
                speechFrequencyHistory.Dequeue();
            }
            currentSpeechFrequencyRatio = speechFrequencyHistory.Average();
        }
        
        // Combine volume and frequency analysis
        float combinedScore = smoothedVolume * speechFrequencyScore;
        
        // Update speech intensity
        CurrentSpeechIntensity = Mathf.Clamp01(combinedScore / speechStartThreshold);
        OnSpeechIntensityChanged?.Invoke(CurrentSpeechIntensity);
        
        // Speech detection logic
        if (!isSpeechDetected)
        {
            // Check if speech is starting
            if (combinedScore > speechStartThreshold)
            {
                StartSpeechDetection();
            }
        }
        else
        {
            // Update speech timing
            if (combinedScore > speechEndThreshold)
            {
                lastSpeechTime = Time.time;
            }
            
            // Check if speech should end
            float silenceDuration = Time.time - lastSpeechTime;
            float currentSpeechDuration = Time.time - speechStartTime;
            
            if (silenceDuration > maxSilenceDuration || combinedScore < speechEndThreshold)
            {
                // Only end speech if minimum duration was met
                if (currentSpeechDuration >= minSpeechDuration)
                {
                    EndSpeechDetection();
                }
                else if (silenceDuration > maxSilenceDuration * 2) // Give extra time for short speech
                {
                    EndSpeechDetection();
                }
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Volume: {smoothedVolume:F3}, FreqScore: {speechFrequencyScore:F3}, " +
                     $"Combined: {combinedScore:F3}, Speech: {isSpeechDetected}, " +
                     $"Intensity: {CurrentSpeechIntensity:F3}");
        }
    }
    
    float AnalyzeSpeechFrequencies()
    {
        if (micMonitor?.AudioSource?.clip == null) return 0f;
        
        // Get spectrum data
        AudioListener.GetSpectrumData(spectrumData, 0, FFTWindow.Hamming);
        
        if (drawFrequencySpectrum && showDebugInfo)
        {
            DrawSpectrum();
        }
        
        // Calculate frequency bins
        float sampleRate = micMonitor.AudioSource.clip.frequency;
        float freqPerBin = sampleRate / 2f / fftSize;
        
        // Find speech frequency range bins
        int minBin = Mathf.FloorToInt(minSpeechFrequency / freqPerBin);
        int maxBin = Mathf.FloorToInt(maxSpeechFrequency / freqPerBin);
        maxBin = Mathf.Min(maxBin, spectrumData.Length - 1);
        
        // Calculate energy in speech frequency range vs total energy
        float speechEnergy = 0f;
        float totalEnergy = 0f;
        
        for (int i = 0; i < spectrumData.Length; i++)
        {
            float energy = spectrumData[i] * spectrumData[i];
            totalEnergy += energy;
            
            if (i >= minBin && i <= maxBin)
            {
                speechEnergy += energy;
            }
        }
        
        // Return ratio of speech frequencies to total energy
        return totalEnergy > 0f ? speechEnergy / totalEnergy : 0f;
    }
    
    void DrawSpectrum()
    {
        // Simple spectrum visualization in console (for debugging)
        string spectrum = "Spectrum: ";
        int displayBins = 20;
        int step = spectrumData.Length / displayBins;
        
        for (int i = 0; i < displayBins; i++)
        {
            float avg = 0f;
            for (int j = 0; j < step && (i * step + j) < spectrumData.Length; j++)
            {
                avg += spectrumData[i * step + j];
            }
            avg /= step;
            
            int barHeight = Mathf.RoundToInt(avg * 100f);
            spectrum += barHeight.ToString("D2") + " ";
        }
        
        Debug.Log(spectrum);
    }
    
    void StartSpeechDetection()
    {
        isSpeechDetected = true;
        speechStartTime = Time.time;
        lastSpeechTime = Time.time;
        
        OnSpeechStarted?.Invoke();
        
        if (showDebugInfo)
        {
            Debug.Log("Speech detection STARTED");
        }
    }
    
    void EndSpeechDetection()
    {
        float speechDuration = Time.time - speechStartTime;
        isSpeechDetected = false;
        CurrentSpeechIntensity = 0f;
        
        OnSpeechEnded?.Invoke();
        
        if (showDebugInfo)
        {
            Debug.Log($"Speech detection ENDED (duration: {speechDuration:F2}s)");
        }
    }
    
    // Public methods for runtime adjustment
    public void SetSpeechThreshold(float threshold)
    {
        speechStartThreshold = Mathf.Max(0.001f, threshold);
        speechEndThreshold = speechStartThreshold * 0.5f; // End threshold is half of start
    }
    
    public void SetSensitivity(float sensitivity)
    {
        detectionSensitivity = Mathf.Clamp(sensitivity, 0.1f, 5f);
    }
    
    public void SetMinSpeechDuration(float duration)
    {
        minSpeechDuration = Mathf.Max(0.1f, duration);
    }
    
    public void EnableFrequencyAnalysis(bool enable)
    {
        useFrequencyAnalysis = enable;
        if (enable && spectrumData == null)
        {
            spectrumData = new float[fftSize];
            InitializeWindowFunction();
        }
    }
    
    // Force end current speech detection
    public void ForceEndSpeech()
    {
        if (isSpeechDetected)
        {
            EndSpeechDetection();
        }
    }
    
    // Get detailed speech statistics
    public SpeechStats GetSpeechStats()
    {
        return new SpeechStats
        {
            IsSpeaking = isSpeechDetected,
            SpeechDuration = SpeechDuration,
            SpeechIntensity = CurrentSpeechIntensity,
            FrequencyRatio = currentSpeechFrequencyRatio,
            SmoothedVolume = volumeHistory.Count > 0 ? volumeHistory.Average() : 0f
        };
    }
    
    void OnDestroy()
    {
        if (micMonitor != null)
        {
            micMonitor.OnVolumeChanged -= ProcessVolumeForSpeech;
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugInfo || !Application.isPlaying) return;
        
        // Visual indicator of speech detection
        Gizmos.color = isSpeechDetected ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.up, Vector3.one * 0.5f);
        
        // Speech intensity indicator
        Gizmos.color = Color.yellow;
        Vector3 intensityScale = Vector3.one * (0.2f + CurrentSpeechIntensity * 0.8f);
        Gizmos.DrawCube(transform.position + Vector3.up * 1.5f, intensityScale);
    }
}