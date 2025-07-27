using UnityEngine;


[System.Serializable]
public abstract class Notification : Object
{
    //ATTRIBUTES
    public Vector3 position;
    public Vector3 eulerRotation;
    public Vector3 localScale;



    //METHODS
    public Vector3 GetPosition()
    {
        return position;
    }


    public Vector3 GetEulerRotation()
    {
        return eulerRotation;
    }


    public Quaternion GetRotation()
    {
        return Quaternion.Euler(eulerRotation.x, eulerRotation.y, eulerRotation.z);
    }


    public Vector3 GetScale()
    {
        return localScale;
    }


    public bool CheckSpawn(Vector3 userPosition, float spawnDistance)
    {
        Vector3 displacementVector = new Vector3(position.x - userPosition.x, 0, position.z - userPosition.z);
        return displacementVector.magnitude <= spawnDistance;
    }


    public abstract GameObject SpawnObject();
}
