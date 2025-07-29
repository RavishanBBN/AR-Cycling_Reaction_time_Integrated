using UnityEngine;


[System.Serializable]
public abstract class Notification : Object
{
    //ATTRIBUTES
    protected Vector3 position;
    protected Vector3 eulerRotation;
    protected Vector3 localScale;
    protected bool playAudio;



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


    public bool GetPlayAudio()
    {
        return playAudio;
    }


    public bool CheckSpawn(Vector3 userPosition, float spawnDistance)
    {
        Vector3 displacementVector = new Vector3(position.x - userPosition.x, 0, position.z - userPosition.z);
        return displacementVector.magnitude <= spawnDistance;
    }


    public abstract GameObject SpawnObject();
}
