using UnityEngine;
using System.Collections.Generic;
using System;


public class SpawnNotification : MonoBehaviour
{
    //ATTRIBUTES
    private List<List<(Notification, float)>> notificationLists;
    private List<(Notification, float)> notifications;
    public int notificationListIndex = 0;
    public float spawnDistance = 20;
    public GameObject notificationControl;
    public Camera userCamera;
    public GameObject signObject;
    public Material signMaterial;
    public AudioSource audioSource;
    public float timeBetweenAudio;
    public float timeBetweenNotificationAndAudio;
    public float audioProbability = 0.15f;
    private float timeBetweenAudioTimer = 0;
    private float timeBetweenNotificationAndAudioTimer = 0;
    private float rngCheckTimer = 0f;
    private float rngCheckDuration = 1f;
    private GameObject currentObject;
    private GameObject previousObject;
    private List<GameObject> spawnedObjects;
    private CsvExporter _gameObjectSpawnTimeExporter;

    [Header("Export Settings")]
    [SerializeField]
    private string exportFileName = "notification-spawn-time";

    [SerializeField] private float exportInterval = 1f;



    //METHODS
    // public Sprite CreateSprite(Vector3 position, Vector3 eulerRotation, Vector3 localScale, bool playAudio, string textureLocation)
    // {
    //     Texture texture = Resources.Load<Texture>(textureLocation);
    //     return new Sprite(position, eulerRotation, localScale, playAudio, texture, signObject, signMaterial);
    // }


    // public Model CreateModel(Vector3 position, Vector3 eulerRotation, Vector3 localScale, bool playAudio, string modelLocation, float spinningPeriod, string animatorLocation = null)
    // {
    //     GameObject model = Resources.Load<GameObject>(modelLocation);
    //     RuntimeAnimatorController animator = Resources.Load<RuntimeAnimatorController>(animatorLocation);
    //     return new Model(position, eulerRotation, localScale, playAudio, model, spinningPeriod, animator);
    // }


    public Sprite CreateSprite(bool playAudio, string textureLocation)
    {
        Texture texture = Resources.Load<Texture>(textureLocation);
        return new Sprite(playAudio, texture, signObject, signMaterial);
    }


    public Model CreateModel(bool playAudio, string modelLocation, float spinningPeriod, string animatorLocation = null)
    {
        GameObject model = Resources.Load<GameObject>(modelLocation);
        RuntimeAnimatorController animator = Resources.Load<RuntimeAnimatorController>(animatorLocation);
        return new Model(playAudio, model, spinningPeriod, animator);
    }


    private void SpawnNotificationInstance(Notification notification)
    {
        Destroy(previousObject);
        previousObject = currentObject;
        currentObject = notification.SpawnObject();

        if (notification.GetPlayAudio())
        {
            playAudio();
        }

        _gameObjectSpawnTimeExporter.AddData(new GameObjectSpawnTimeDatum
        {
            TimeStamp = Time.time,
            Object = currentObject.name
        }.ToString());

        timeBetweenNotificationAndAudioTimer = 0;
    }


    private void playAudio()
    {
        timeBetweenAudioTimer = 0;
        audioSource.Play();
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // notificationLists = new List<List<Notification>>
        // {
        //     new List<Notification>
        //     {
        //         CreateModel(new Vector3(0, 1.5f, 5), new Vector3(0, 0, 0), new Vector3(50, 50, 50), false, "Models/MacDonalds/MacDonalds", 5),
        //         CreateSprite(new Vector3(0, 1.5f, 30), new Vector3(0, 0, 0), new Vector3(1, 1, 1), true, "SignImages/40_zone"),
        //         CreateModel(new Vector3(0, 1.5f, 70), new Vector3(0, 0, 0), new Vector3(50, 50, 50), false, "Models/Cafe/Cafe", 5),
        //         CreateSprite(new Vector3(0, 1.5f, 110), new Vector3(0, 0, 0), new Vector3(1, 1, 1), true, "SignImages/give_way"),
        //         CreateModel(new Vector3(0, 1.5f, 160), new Vector3(0, 0, 0), new Vector3(50, 50, 50), false, "Models/Toilet/Toilet", 5),
        //     }
        // };

        notifications = notificationLists[notificationListIndex];
        notifications.Reverse(); //Reverse notification list items so that items are popped from the end (O(1) time as opposed to O(n)).
    }


    // Update is called once per frame
    void Update()
    {
        //If the notification list is not empty.
        if (notifications.Count > 0)
        {
            //Get the last notification.
            Notification notification = notifications[notifications.Count - 1];
            
            //If the user is in range of the notification.
            if (notification.CheckSpawn(userCamera.transform.position, spawnDistance))
            {
                //Spawn the notification.
                SpawnNotificationInstance(notification);

                //Pop the notification from the list (since the notification is at the end of the list, this is O(1) time).
                notifications.RemoveAt(notifications.Count - 1);
            }
        }

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

        //Export game object spawn time data to CSV.
        _gameObjectSpawnTimeExporter.ExportRecentData();
    }


    private void Awake()
    {
        var timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var gameObjectSpawnTimeFilePath = Application.persistentDataPath + $"/{exportFileName}_{timeStamp}.csv";
        const string csvHeader = "Time (s),Object";
        _gameObjectSpawnTimeExporter = new CsvExporter(gameObjectSpawnTimeFilePath, exportInterval, csvHeader);

        Debug.Log($"Exporting notification spawned time to {gameObjectSpawnTimeFilePath}");
    }
    

    private void OnDestroy()
    {
        if (_gameObjectSpawnTimeExporter.BufferCount == 0) return;

        _gameObjectSpawnTimeExporter.ForceFlush();
    }
}


internal record GameObjectSpawnTimeDatum
{
    public float TimeStamp { get; set; }
    public string Object { get; set; }

    public override string ToString()
    {
        return $"{TimeStamp},{Object}";
    }
}