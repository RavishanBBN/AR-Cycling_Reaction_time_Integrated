using UnityEngine;

public abstract class SpawnObject : MonoBehaviour
{
    //ATTRIBUTES
    public Camera userCamera; //Reference to the camera (which is used to get the current position and rotation).
    public float distanceBase = 40;
    public float distanceOffset = 10;
    public float xDisplacement = 0;
    public float yDisplacement = 0;
    private float distanceUntilSpawnObject;
    private Vector2 userInitialPosition;
    private Vector3 objectSpawnDisplacement = Vector3.forward * 15; //Vector3 so it can be added to the user's position.
    private GameObject currentObject;
    private GameObject previousObject;
    private Vector3 userPositionTracker;
    [SerializeField] private AudioSource audioSource;


    //METHODS
    protected abstract GameObject spawnObject(Vector3 position, Quaternion rotation);

    private GameObject CreateObject(Vector3 position, Quaternion rotation)
    {
        audioSource.Play();

        return spawnObject(position, rotation);
    }

    private float dotProduct2(Vector2 a, Vector2 b)
    {
        return a.x * b.x + a.y * b.y;
    }


    private float crossProduct2(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }


    private Vector2 unitVector2(Vector2 v)
    {
        return v / v.magnitude;
    }


    private Vector2 getVector2(Vector3 vector)
    {
        return new Vector2(vector.x, vector.z);
    }


    private float getUserDistance()
    {
        //x and z coordinates for Vector3, x and y coordinates for Vector2
        Vector2 displacementVector = new Vector2(userCamera.transform.position.x - userInitialPosition.x,
                                                 userCamera.transform.position.z - userInitialPosition.y);
        return displacementVector.magnitude;
    }


    private float getRandomDistance()
    {
        return Random.Range(distanceBase - distanceOffset, distanceBase + distanceOffset);
    }


    private Vector3 getMovementVector()
    {
        Vector3 userPosition = userCamera.transform.position;
        Vector3 movementVector = new Vector3(userPosition.x - userPositionTracker.x,
                                             userPositionTracker.y - userPosition.y,
                                             userPosition.z - userPositionTracker.z);

        return movementVector;
    }


    private Vector2 getRelativeVector(Vector2 referenceVector, Vector2 relativeVector)
    {
        Vector2 forwardVector = new Vector2(0, 1);

        Vector2 a = forwardVector; //Absolute forward
        Vector2 b = relativeVector; //Absolute displacement
        Vector2 c = referenceVector; //Relative forward

        return new Vector2(
                           dotProduct2(a, b) / (a.magnitude * b.magnitude) * c.x -
                           crossProduct2(a, b) / (a.magnitude * b.magnitude) * c.y,
                           crossProduct2(a, b) / (a.magnitude * b.magnitude) * c.x +
                           dotProduct2(a, b) / (a.magnitude * b.magnitude) * c.y
                          );
    }


    private float getFacingAngle(Vector2 referenceVector)
    {
        Vector2 forwardVector = new Vector2(0, 1);
        return Mathf.Acos(dotProduct2(forwardVector, referenceVector) /
                          (forwardVector.magnitude * referenceVector.magnitude)) * 180 / Mathf.PI;
    }


    private void initialiseSignDisplacement()
    {
        userInitialPosition = new Vector2(userCamera.transform.position.x, userCamera.transform.position.z);
        distanceUntilSpawnObject = getRandomDistance();
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        objectSpawnDisplacement = Vector3.right * xDisplacement + Vector3.forward * 15;
        initialiseSignDisplacement();
        currentObject =
            CreateObject(userCamera.transform.position + objectSpawnDisplacement + Vector3.up * yDisplacement,
                         transform.rotation);
    }


    // Update is called once per frame
    void Update()
    {
        if (getUserDistance() >= distanceUntilSpawnObject)
        {
            Vector2 referenceVector = unitVector2(getVector2(getMovementVector()));
            Vector2 relativeSpawnDisplacementUnit2 =
                getRelativeVector(referenceVector, getVector2(objectSpawnDisplacement));
            Vector3 relativeSpawnDisplacementUnit =
                new Vector3(relativeSpawnDisplacementUnit2.x, 0, relativeSpawnDisplacementUnit2.y);
            Vector3 relativeSpawnDisplacement = relativeSpawnDisplacementUnit * objectSpawnDisplacement.magnitude;
            Destroy(previousObject);
            previousObject = currentObject;
            initialiseSignDisplacement();

            Quaternion objectRotation = Quaternion.Euler(0, -getFacingAngle(referenceVector), 0);
            currentObject =
                CreateObject(userCamera.transform.position + relativeSpawnDisplacement + Vector3.up * yDisplacement,
                             objectRotation);
        }

        Vector3 userPosition = userCamera.transform.position;
        userPositionTracker.x = userPosition.x;
        userPositionTracker.y = userPosition.y;
        userPositionTracker.z = userPosition.z;
    }
}