using UnityEngine;
using UnityEngine.UI;
public class PlayerSpeaker : DamageableBase
{
    [SerializeField] private SpeechDetector speechDetector;

    [SerializeField] private Image speechFill;
    [SerializeField] private Vector2 intensityToRadius;

    [SerializeField] private float dotThreshold;

    private float speechIntensity;
    private bool isSpeaking;
    private Collider[] allSensedObjects = new Collider[10];

    public float SpeechIntensity
    {
        get => speechIntensity;
        set
        {
            speechIntensity = value;
            speechFill.fillAmount = value;
        }
    }
    public override Alignment Alignment => Alignment.Ally;

    void Start()
    {
        speechDetector.OnSpeechStarted += SpeechStarted;
        speechDetector.OnSpeechEnded += SpeechStopped;
        speechDetector.OnSpeechIntensityChanged += SpeechIntensityChanged;
    }

    private void FixedUpdate()
    {
        if (isSpeaking)
        {
            var radius = Mathf.Lerp(intensityToRadius.x, intensityToRadius.y, SpeechIntensity);

            Physics.OverlapSphereNonAlloc(transform.position, radius, allSensedObjects);

            for (int i = 0; i < allSensedObjects.Length; i++)
            {
                var point = allSensedObjects[i].ClosestPoint(transform.position);
                var delta = Vector3.ProjectOnPlane(point - transform.position, Vector3.up).normalized;

                if (Vector3.Dot(delta, Vector3.forward) < dotThreshold)
                    continue;

                allSensedObjects[i].GetComponent<Sheep>();
            }

        }
    }

    private void SpeechStarted()
    {
        isSpeaking = true;
    }
    private void SpeechStopped()
    {
        isSpeaking = false;
    }

    private void SpeechIntensityChanged(float intensity)
    {
        SpeechIntensity = intensity;
    }
}