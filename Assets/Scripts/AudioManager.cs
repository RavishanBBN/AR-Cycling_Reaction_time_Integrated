using UnityEngine;

public class AudioManager : MonoBehaviour
{
    //ATTRIBUTES
    public AudioSource audioSource;
    public float timeBetweenAudio;
    public float timeBetweenNotificationAndAudio;
    public float audioProbability = 0.15f;
    private float timeBetweenAudioTimer = 0;
    private float timeBetweenNotificationAndAudioTimer = 0;
    private float rngCheckTimer = 0f;
    private float rngCheckDuration = 1f;



    //METHODS
    private void playAudio()
    {
        timeBetweenAudioTimer = 0;
        audioSource.Play();
    }

    
    public void OnNotificationSpawn(Notification notification)
    {
        if (notification.GetPlayAudio())
        {
            playAudio();
        }

        timeBetweenNotificationAndAudioTimer = 0;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timeBetweenAudioTimer = 0;
        timeBetweenNotificationAndAudioTimer = 0;
    }

    // Update is called once per frame
    void Update()
    {
        //Try playing random audio if applicable.
        if (timeBetweenAudioTimer >= timeBetweenAudio && timeBetweenNotificationAndAudioTimer >= timeBetweenNotificationAndAudio)
        {
            rngCheckTimer += Time.deltaTime;

            if (rngCheckTimer >= rngCheckDuration)
            {
                float rng = UnityEngine.Random.Range(0f, 1f);
                if (rng < audioProbability)
                {
                    playAudio();
                }
                rngCheckTimer = 0f;
            }
        }

        //Increment audio timers.
        timeBetweenAudioTimer += Time.deltaTime;
        timeBetweenNotificationAndAudioTimer += Time.deltaTime;
    }
}
