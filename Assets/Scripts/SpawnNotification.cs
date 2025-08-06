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
    public float passDistance = 8;
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
    private Vector3 currentObjectPosition;
    private Vector3 userPositionTracker;
    private bool passedCurrentObject = false;
    private CsvExporter _gameObjectSpawnTimeExporter;

    [Header("Export Settings")]
    [SerializeField]
    private string exportFileName = "notification-spawn-time";

    [SerializeField] private float exportInterval = 1f;



    //METHODS
    public Sprite CreateSprite(bool playAudio, string textureLocation)
    {
        Texture texture = Resources.Load<Texture>(textureLocation);
        return new Sprite(playAudio, texture, signObject, signMaterial);
    }


    public Model CreateModel(bool playAudio, string modelLocation, Vector3 modelScale, float spinningPeriod, string animatorLocation = null)
    {
        GameObject model = Resources.Load<GameObject>(modelLocation);
        RuntimeAnimatorController animator = Resources.Load<RuntimeAnimatorController>(animatorLocation);
        return new Model(playAudio, model, modelScale, spinningPeriod, animator);
    }


    private float DotProduct2(Vector2 a, Vector2 b)
    {
        return a.x * b.x + a.y * b.y;
    }


    private float CrossProduct2(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }


    private Vector2 UnitVector2(Vector2 v)
    {
        return v / v.magnitude;
    }


    private Vector2 GetVector2(Vector3 vector)
    {
        return new Vector2(vector.x, vector.z);
    }

    
    private Vector3 GetMovementVector()
    {
        Vector3 userPosition = userCamera.transform.position;
        Vector3 movementVector = new Vector3(userPosition.x - userPositionTracker.x, userPositionTracker.y - userPosition.y, userPosition.z - userPositionTracker.z);

        return movementVector;
    }


    private Vector2 GetRelativeVector(Vector2 referenceVector, Vector2 relativeVector)
    {
        Vector2 forwardVector = new Vector2(0, 1);

        Vector2 a = forwardVector; //Absolute forward
        Vector2 b = relativeVector; //Absolute displacement
        Vector2 c = referenceVector; //Relative forward

        float dotProduct = DotProduct2(a, b);
        float crossProduct = CrossProduct2(a, b);

        return new Vector2(
            dotProduct / (a.magnitude * b.magnitude) * c.x - crossProduct / (a.magnitude * b.magnitude) * c.y,
            crossProduct / (a.magnitude * b.magnitude) * c.x + dotProduct / (a.magnitude * b.magnitude) * c.y
        );
    }


    private float GetSpawnDistance(float notificationDistance)
    {
        return Mathf.Min(notificationDistance, spawnDistance);
    }


    private float GetDistanceSinceNotification()
    {
        Vector3 userPosition = userCamera.transform.position;
        Vector2 distanceVector = new Vector2(currentObjectPosition.x - userPosition.x, currentObjectPosition.z - userPosition.z);
        return distanceVector.magnitude;
    }


    private Vector3 GetNotificationSpawnPosition(Vector2 referenceVector, float notificationDistance)
    {
        Vector2 objectSpawnDisplacement = new Vector2(0, GetSpawnDistance(notificationDistance));
        Vector2 relativeSpawnDisplacementUnit2 = GetRelativeVector(referenceVector, objectSpawnDisplacement);
        Vector3 relativeSpawnDisplacementUnit = new Vector3(relativeSpawnDisplacementUnit2.x, 0, relativeSpawnDisplacementUnit2.y);
        Vector3 relativeSpawnDisplacement = relativeSpawnDisplacementUnit * objectSpawnDisplacement.magnitude;

        return userCamera.transform.position + relativeSpawnDisplacement;
    }


    private Quaternion GetNotificationSpawnRotation(Vector2 referenceVector)
    {
        Vector2 forwardVector = new Vector2(0, 1);
        float rotation = Mathf.Atan2(
            forwardVector.x * referenceVector.y - forwardVector.y * referenceVector.x,
            forwardVector.x * referenceVector.x + forwardVector.y * referenceVector.y
        ) * Mathf.Rad2Deg;
        return Quaternion.Euler(0, -rotation, 0);
    }


    private void SpawnNotificationInstance(Notification notification, float distance)
    {
        Destroy(previousObject);
        previousObject = currentObject;

        Vector3 movementVector2 = GetVector2(GetMovementVector());
        Vector2 referenceVector = UnitVector2(movementVector2);
        if (movementVector2.magnitude == 0)
        {
            currentObject = notification.SpawnObject(new Vector3(0, 0, GetSpawnDistance(distance)), Quaternion.Euler(0, 0, 0), new Vector3(1, 1, 1));
        }
        else
        {
            currentObject = notification.SpawnObject(GetNotificationSpawnPosition(referenceVector, distance), GetNotificationSpawnRotation(referenceVector), new Vector3(1, 1, 1));
        }
        currentObjectPosition = currentObject.transform.position;
        passedCurrentObject = false;

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
        notificationLists = new List<List<(Notification, float)>>
        {
            new List<(Notification, float)>
            {
                (CreateModel(false, "Models/MacDonalds/MacDonalds", new Vector3(50, 50, 50), 5), 5),
                (CreateSprite(true, "SignImages/40_zone"), 45),
                (CreateModel(false, "Models/Cafe/Cafe", new Vector3(50, 50, 50), 5), 50),
                (CreateSprite(false, "SignImages/give_way"), 50),
                (CreateModel(true, "Models/Toilet/Toilet", new Vector3(50, 50, 50), 5), 50),
                (CreateModel(false, "Models/MacDonalds/MacDonalds", new Vector3(50, 50, 50), 5), 50),
                (CreateSprite(true, "SignImages/40_zone"), 50),
                (CreateModel(false, "Models/Cafe/Cafe", new Vector3(50, 50, 50), 5), 50),
                (CreateSprite(false, "SignImages/give_way"), 50),
                (CreateModel(true, "Models/Toilet/Toilet", new Vector3(50, 50, 50), 5), 50)
            }
        };

        notifications = notificationLists[notificationListIndex];
        notifications.Reverse(); //Reverse notification list items so that items are popped from the end (O(1) time as opposed to O(n)).
    }


    // Update is called once per frame
    void Update()
    {
        //Check if the user has passed the current object.
        if (GetDistanceSinceNotification() < passDistance)
        {
            passedCurrentObject = true;
        }

        //If the notification list is not empty.
        if (notifications.Count > 0)
        {
            //Get the last notification.
            (Notification, float) notificationData = notifications[notifications.Count - 1];
            Notification notification = notificationData.Item1;
            float distance = notificationData.Item2;

            //If the user is in range of the notification.
            if (GetDistanceSinceNotification() > distance - spawnDistance && passedCurrentObject)
            {
                //Spawn the notification.
                SpawnNotificationInstance(notification, distance);

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
        
        //Update user position tracker for getting movement vector.
        Vector3 userPosition = userCamera.transform.position;
        userPositionTracker.x = userPosition.x;
        userPositionTracker.y = userPosition.y;
        userPositionTracker.z = userPosition.z;
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