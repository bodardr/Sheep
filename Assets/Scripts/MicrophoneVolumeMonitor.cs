using UnityEngine;
using System.Collections;

public class MicrophoneVolumeMonitor : MonoBehaviour
{
    [Header("Microphone Settings")]
    [SerializeField] private string microphoneDevice = null; // null uses default microphone
    [SerializeField] private int sampleRate = 44100;
    [SerializeField] private int recordingLength = 1; // seconds of audio to keep in buffer
    
    [Header("Volume Detection")]
    [SerializeField] private float updateInterval = 0.1f; // how often to check volume (seconds)
    [SerializeField] private int sampleWindow = 128; // number of samples to analyze for volume
    [SerializeField] private bool normalizeVolume = true; // normalize to 0-1 range
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Events
    public System.Action<float> OnVolumeChanged;
    
    // Private variables
    private AudioSource audioSource;
    private AudioClip microphoneClip;
    private bool isRecording = false;
    private float currentVolumeRatio = 0f;
    private Coroutine volumeCheckCoroutine;
    
    // Properties
    public float CurrentVolumeRatio => currentVolumeRatio;
    public bool IsRecording => isRecording;
    public string[] AvailableMicrophones => Microphone.devices;
    public AudioSource AudioSource => audioSource;

    void Start()
    {
        InitializeMicrophone();
        StartVolumeMonitoring();
    }
    
    void InitializeMicrophone()
    {
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure AudioSource (mute it so we don't hear feedback)
        audioSource.mute = true;
        audioSource.loop = true;
        
        // Check if microphone is available
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone devices found!");
            return;
        }
        
        // Use specified device or default
        if (string.IsNullOrEmpty(microphoneDevice) && Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Using microphone: {microphoneDevice}");
            Debug.Log($"Available microphones: {string.Join(", ", Microphone.devices)}");
        }
    }
    
    public void StartRecording()
    {
        if (isRecording) return;
        
        // Start microphone recording
        microphoneClip = Microphone.Start(microphoneDevice, true, recordingLength, sampleRate);
        
        if (microphoneClip != null)
        {
            audioSource.clip = microphoneClip;
            audioSource.Play();
            isRecording = true;
            
            if (showDebugInfo)
                Debug.Log("Microphone recording started");
        }
        else
        {
            Debug.LogError("Failed to start microphone recording");
        }
    }
    
    public void StopRecording()
    {
        if (!isRecording) return;
        
        Microphone.End(microphoneDevice);
        audioSource.Stop();
        isRecording = false;
        currentVolumeRatio = 0f;
        
        if (showDebugInfo)
            Debug.Log("Microphone recording stopped");
    }
    
    public void StartVolumeMonitoring()
    {
        if (volumeCheckCoroutine == null)
        {
            StartRecording();
            volumeCheckCoroutine = StartCoroutine(VolumeCheckCoroutine());
        }
    }
    
    public void StopVolumeMonitoring()
    {
        if (volumeCheckCoroutine != null)
        {
            StopCoroutine(volumeCheckCoroutine);
            volumeCheckCoroutine = null;
            StopRecording();
        }
    }
    
    private IEnumerator VolumeCheckCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);
            
            if (isRecording && microphoneClip != null)
            {
                currentVolumeRatio = GetVolumeRatio();
                OnVolumeChanged?.Invoke(currentVolumeRatio);
                
                if (showDebugInfo)
                {
                    Debug.Log($"Volume Ratio: {currentVolumeRatio:F3}");
                }
            }
        }
    }
    
    private float GetVolumeRatio()
    {
        if (microphoneClip == null || !isRecording)
            return 0f;
        
        // Get current microphone position
        int micPosition = Microphone.GetPosition(microphoneDevice);
        
        if (micPosition < sampleWindow)
            return 0f;
        
        // Create array to hold audio samples
        float[] samples = new float[sampleWindow];
        
        // Get samples from the microphone clip
        int startPosition = micPosition - sampleWindow;
        microphoneClip.GetData(samples, startPosition);
        
        // Calculate RMS (Root Mean Square) for volume
        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }
        
        float rms = Mathf.Sqrt(sum / samples.Length);
        
        // Convert to decibels and normalize if requested
        if (normalizeVolume)
        {
            // Normalize to 0-1 range (adjust multiplier as needed)
            return Mathf.Clamp01(rms * 10f);
        }
        else
        {
            return rms;
        }
    }
    
    // Public method to get volume on demand (outside of interval)
    public float GetCurrentVolumeRatio()
    {
        return GetVolumeRatio();
    }
    
    // Method to change update interval at runtime
    public void SetUpdateInterval(float newInterval)
    {
        updateInterval = Mathf.Max(0.01f, newInterval);
        
        // Restart coroutine with new interval
        if (volumeCheckCoroutine != null)
        {
            StopVolumeMonitoring();
            StartVolumeMonitoring();
        }
    }
    
    // Method to change microphone device
    public void SetMicrophoneDevice(string deviceName)
    {
        bool wasRecording = isRecording;
        
        if (wasRecording)
            StopRecording();
        
        microphoneDevice = deviceName;
        
        if (wasRecording)
            StartRecording();
    }
    
    void OnDestroy()
    {
        StopVolumeMonitoring();
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            StopVolumeMonitoring();
        else if (gameObject.activeInHierarchy)
            StartVolumeMonitoring();
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            StopVolumeMonitoring();
        else if (gameObject.activeInHierarchy)
            StartVolumeMonitoring();
    }
}