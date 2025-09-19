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
    public GameObject audioControl;
    private AudioManager audioManager;
    private float distanceToSpawnObject;
    private float initialLeftOverDistance;
    private Vector3 userInitialPosition;
    private float previousSpeedNormalised;
    private GameObject currentObject;
    private GameObject previousObject;
    private Vector3 userPositionTracker;
    private CsvExporter _gameObjectSpawnTimeExporter;
    private CsvExporter _debugExporter;
    // private CsvExporter _speedometerExporter;
    private float debugWriteTimer = 0f;
    private float debugWriteDuration = 1f;

    [Header("Export Settings")]
    [SerializeField]
    private string exportNotificationFileName = "notification-spawn-time";
    private string exportDebugFileName = "debug-data";
    // private string exportSpeedometerFileName = "speedometer";

    [SerializeField] private float exportInterval = 1f;

    private SpawnPosition leftPosition = new SpawnPosition(new Vector3(-1.5f, 0, 0), new Vector3(0, 0, 0), new Vector3(1, 1, 1));
    private SpawnPosition topPosition = new SpawnPosition(new Vector3(0, 2.5f, 0), new Vector3(0, 0, 0), new Vector3(1, 1, 1));



    //METHODS
    public Sprite CreateSprite(bool playAudio, SpawnPosition spawnPosition, string textureLocation)
    {
        Texture texture = Resources.Load<Texture>(textureLocation);
        return new Sprite(playAudio, spawnPosition, texture, signObject, signMaterial);
    }


    public Model CreateModel(bool playAudio, SpawnPosition spawnPosition, string modelLocation, Vector3 modelScale, float spinningPeriod, string animatorLocation = null)
    {
        GameObject model = Resources.Load<GameObject>(modelLocation);
        RuntimeAnimatorController animator = Resources.Load<RuntimeAnimatorController>(animatorLocation);
        return new Model(playAudio, spawnPosition, model, modelScale, spinningPeriod, animator);
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


    private float GetUserDistance()
    {
        //x and z coordinates for Vector3, x and y coordinates for Vector2
        Vector2 displacementVector = new Vector2(userCamera.transform.position.x - userInitialPosition.x,
                                                 userCamera.transform.position.z - userInitialPosition.z);
        return displacementVector.magnitude;
    }


    private Vector3 GetNotificationSpawnPosition(Vector2 referenceVector)
    {
        Vector2 objectSpawnDisplacement = new Vector2(0, spawnDistance);
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


    private float GetNextDistance()
    {
        if (notifications.Count > 0)
        {
            (Notification, float) newNotificationData = notifications[notifications.Count - 1];
            return newNotificationData.Item2;
        }
        else
        {
            return Mathf.Infinity;
        }
    }


    private void SpawnNotificationInstance(Notification notification, float distance)
    {
        Destroy(previousObject);
        previousObject = currentObject;

        Vector2 movementVector2 = GetVector2(GetMovementVector());
        float movementSpeed = movementVector2.magnitude;
        Vector2 referenceVector = UnitVector2(movementVector2);
        Vector3 notificationPosition;
        Quaternion notificationRotation;

        if (movementSpeed == 0)
        {
            notificationPosition = new Vector3(0, 1.5f, Mathf.Min(distance, spawnDistance));
            notificationRotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            notificationPosition = GetNotificationSpawnPosition(referenceVector);
            notificationRotation = GetNotificationSpawnRotation(referenceVector);
        }

        currentObject = notification.SpawnObject(notificationPosition, notificationRotation, new Vector3(1, 1, 1));

        _gameObjectSpawnTimeExporter.AddData(new GameObjectSpawnTimeDatum
        {
            TimeStamp = Time.time,
            Object = currentObject.name
        }.ToString());

        _debugExporter.AddData(new DebugNotificationDatum
        {
            DistanceFromPreviousObject = GetUserDistance(),
            DistanceToSpawnObject = distanceToSpawnObject,
            UserPosition = userCamera.transform.position,
            CurrentObjectPosition = currentObject != null ? currentObject.transform.position : new Vector3(0, 0, 0),
            PreviousObjectPosition = previousObject != null ? previousObject.transform.position : new Vector3(0, 0, 0),
            MovementVector = GetMovementVector(),
            NotificationPosition = notificationPosition,
            NotificationRotation = notificationRotation,
            NotificationsRemaining = notifications.Count,
        }.ToString());

        userInitialPosition = new Vector3(userCamera.transform.position.x, userCamera.transform.position.y, userCamera.transform.position.z);

        audioManager.OnNotificationSpawn(notification);
    }


    private void SpawnNextNotification()
    {
        if (notifications.Count > 0)
        {
            (Notification, float) notificationData = notifications[notifications.Count - 1];
            Notification notification = notificationData.Item1;
            float distance = notificationData.Item2;
            SpawnNotificationInstance(notification, distance);
            notifications.RemoveAt(notifications.Count - 1);
            float nextDistance = GetNextDistance();
            distanceToSpawnObject = nextDistance - initialLeftOverDistance;
            if (nextDistance < initialLeftOverDistance)
            {
                initialLeftOverDistance -= nextDistance;
            }
            else
            {
                initialLeftOverDistance = 0;
            }
        }
    }


    private void SimulateTeleport()
    {
        Vector3 randomPosition = new Vector3(
            UnityEngine.Random.Range(-400, 400),
            0,
            UnityEngine.Random.Range(-400, 400)
        );

        userCamera.transform.position = randomPosition;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioManager = audioControl.GetComponent<AudioManager>();

        notificationLists = new List<List<(Notification, float)>>
        {
            new List<(Notification, float)>
            {
                (CreateModel(true, leftPosition, "Models/waterfountain/WaterFountain", new Vector3(50, 50, 50), 5), 5),
                (CreateModel(true, leftPosition, "Models/Toilet/Toilet", new Vector3(50, 50, 50), 5), 65),
                (CreateSprite(true, topPosition, "SignImages/dangerous_intersection"), 195),
                (CreateModel(true, leftPosition, "Models/Cafe/Cafe", new Vector3(50, 50, 50), 5), 32),
                (CreateSprite(true, leftPosition, "SignImages/school_of_biological_science"), 100),
                (CreateModel(true, topPosition, "Models/waterfountain/WaterFountain", new Vector3(1, 1, 1), 5, "Models/WoodenSpinningTop/WoodenSpinningTopAnimatorController"), 145),
                (CreateSprite(true, leftPosition, "SignImages/bus_loop"), 181),
                (CreateSprite(true, leftPosition, "SignImages/watch_for_pedestrians"), 108),
                (CreateModel(true, leftPosition, "Models/Sushi/almonroe", new Vector3(50, 50, 50), 5), 86),
                (CreateSprite(true, leftPosition, "SignImages/library"), 163),
                (CreateSprite(true, topPosition, "Models/Donut/donut"), 138),
                (CreateSprite(true, leftPosition, "SignImages/give_way"), 155),
            }

            /*
            5 - Maccas
            65 - Toilet
            195 - Dangerous intersection
            32 - Cafe
            100 - School of biological science
            145 - Spinning top
            181 - Monash bus loop
            108 - Watch for pedestrian
            86 - Maccas
            163 - Library
            138 - 40 zone
            155 - Give way
            */
        };

        notifications = notificationLists[notificationListIndex];
        notifications.Reverse(); //Reverse notification list items so that items are popped from the end (O(1) time as opposed to O(n)).

        (Notification, float) firstNotificationData = notifications[notifications.Count - 1];
        float firstNotificationDistance = firstNotificationData.Item2;

        if (firstNotificationDistance < spawnDistance)
        {
            initialLeftOverDistance = spawnDistance - firstNotificationDistance;
            SpawnNextNotification();
        }
        else
        {
            distanceToSpawnObject = firstNotificationDistance;
        }
    }


    // Update is called once per frame
    void Update()
    {
        // //Simulate teleport.
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     SimulateTeleport();
        // }

        //Detect teleportation.
        Vector3 movementVector3 = GetMovementVector();
        Vector2 movementVector2 = GetVector2(movementVector3);
        float movementSpeed = movementVector2.magnitude;
        float movementSpeedNormalised = movementSpeed / Time.deltaTime;
        // bool teleported = false;
        if ((movementSpeedNormalised > Mathf.Max(previousSpeedNormalised, 2.5f) * 10 && previousSpeedNormalised > 0) || (movementSpeedNormalised > 100 && previousSpeedNormalised == 0))
        {
            userInitialPosition += movementVector3;
            if (previousObject != null) { previousObject.transform.position += movementVector3; }
            if (currentObject != null) { currentObject.transform.position += movementVector3; }
            // teleported = true;
        }
        else
        {
            previousSpeedNormalised = movementSpeedNormalised;
        }

        // _speedometerExporter.AddData(new SpeedometerDatum
        // {
        //     MovementSpeed = movementSpeed,
        //     PreviousSpeed = previousSpeedNormalised,
        //     UserPosition = userCamera.transform.position,
        //     Teleported = teleported
        // }.ToString());

        //If the notification list is not empty.
        if (notifications.Count > 0)
        {
            //If the user is in range of the notification.
            if (GetUserDistance() >= distanceToSpawnObject)
            {
                SpawnNextNotification();
            }
        }

        //Step debug data.
        if (debugWriteTimer < debugWriteDuration)
        {
            debugWriteTimer += Time.deltaTime;
        }
        else
        {
            debugWriteTimer = 0f;
            _debugExporter.AddData(new DebugStepDatum
            {
                DistanceFromPreviousObject = GetUserDistance(),
                DistanceToSpawnObject = distanceToSpawnObject,
                UserPosition = userCamera.transform.position,
                CurrentObjectPosition = currentObject != null ? currentObject.transform.position : new Vector3(0, 0, 0),
                PreviousObjectPosition = previousObject != null ? previousObject.transform.position : new Vector3(0, 0, 0),
            }.ToString());
        }

        //Export game object spawn time data to CSV.
        _gameObjectSpawnTimeExporter.ExportRecentData();
        _debugExporter.ExportRecentData();
        // _speedometerExporter.ExportRecentData();

        //Update user position tracker for getting movement vector.
        Vector3 userPosition = userCamera.transform.position;
        userPositionTracker.x = userPosition.x;
        userPositionTracker.y = userPosition.y;
        userPositionTracker.z = userPosition.z;
    }


    private void Awake()
    {
        var timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        var gameObjectSpawnTimeFilePath = Application.persistentDataPath + $"/{exportNotificationFileName}_{timeStamp}.csv";
        const string csvHeaderNotification = "Time (s),Object";
        _gameObjectSpawnTimeExporter = new CsvExporter(gameObjectSpawnTimeFilePath, exportInterval, csvHeaderNotification);

        var debugDataFilePath = Application.persistentDataPath + $"/{exportDebugFileName} _{timeStamp}.csv";
        const string csvHeaderDebug = "Distance from previous object (m),Distance to spawn object (m),User position,Current object position,Previous object position,Movement vector,Notification position, Notification rotation,Notifications remaining";
        _debugExporter = new CsvExporter(debugDataFilePath, exportInterval, csvHeaderDebug);

        // var speedometerPath = Application.persistentDataPath + $"/{exportSpeedometerFileName} _{timeStamp}.csv";
        // const string csvHeaderSpeedometer = "Movement speed (m/frame),Movement speed (m/s),User position,Teleported";
        // _speedometerExporter = new CsvExporter(speedometerPath, exportInterval, csvHeaderSpeedometer);

        Debug.Log($"Exporting notification spawned time to {gameObjectSpawnTimeFilePath}");
        Debug.Log($"Exporting debug data to {debugDataFilePath}");
        // Debug.Log($"Exporting speedometer data to {speedometerPath}");
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


internal record DebugStepDatum
{
    public float DistanceFromPreviousObject;
    public float DistanceToSpawnObject;
    public Vector3 UserPosition;
    public Vector3 CurrentObjectPosition;
    public Vector3 PreviousObjectPosition;


    private string Vector3ToCSV(Vector3 vector)
    {
        return $"{vector.x} {vector.y} {vector.z}";
    }

    public override string ToString()
    {
        return $"{DistanceFromPreviousObject},{DistanceToSpawnObject},{Vector3ToCSV(UserPosition)},{Vector3ToCSV(CurrentObjectPosition)},{Vector3ToCSV(PreviousObjectPosition)}";
    }
}


internal record DebugNotificationDatum
{
    public float DistanceFromPreviousObject;
    public float DistanceToSpawnObject;
    public Vector3 UserPosition;
    public Vector3 CurrentObjectPosition;
    public Vector3 PreviousObjectPosition;
    public Vector3 MovementVector;
    public Vector3 NotificationPosition;
    public Quaternion NotificationRotation;
    public int NotificationsRemaining;


    private string Vector3ToCSV(Vector3 vector)
    {
        return $"{vector.x} {vector.y} {vector.z}";
    }

    private string QuaternionToCSV(Quaternion quaternion)
    {
        return $"{quaternion.x} {quaternion.y} {quaternion.z} {quaternion.w}";
    }

    public override string ToString()
    {
        return $"{DistanceFromPreviousObject},{DistanceToSpawnObject},{Vector3ToCSV(UserPosition)},{Vector3ToCSV(CurrentObjectPosition)},{Vector3ToCSV(PreviousObjectPosition)},{Vector3ToCSV(MovementVector)},{Vector3ToCSV(NotificationPosition)},{QuaternionToCSV(NotificationRotation)},{NotificationsRemaining}";
    }
}


internal record SpeedometerDatum
{
    public float MovementSpeed;
    public float PreviousSpeed;
    public Vector3 UserPosition;
    public bool Teleported;

    private string Vector3ToCSV(Vector3 vector)
    {
        return $"{vector.x} {vector.y} {vector.z}";
    }

    public override string ToString()
    {
        return $"{MovementSpeed},{MovementSpeed / Time.deltaTime},{PreviousSpeed},{PreviousSpeed / Time.deltaTime},{Vector3ToCSV(UserPosition)},{Teleported}";
    }
}