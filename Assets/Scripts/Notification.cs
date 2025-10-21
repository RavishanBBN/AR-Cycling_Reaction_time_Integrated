using UnityEngine;


[System.Serializable]
public abstract class Notification : Object
{
    //ATTRIBUTES
    protected bool playAudio;
    protected SpawnPosition spawnPosition;



    //METHODS
    public bool GetPlayAudio()
    {
        return playAudio;
    }


    public Vector3 GetLocalScale(Vector3 localScale)
    {
        Vector3 positionScale = spawnPosition.GetLocalScale();
        return new Vector3(localScale.x * positionScale.x, localScale.y * positionScale.y, localScale.z * positionScale.z);
    }


    public float GetPositionXDisplacement()
    {
        return spawnPosition.GetXDisplacement();
    }


    public abstract GameObject SpawnObject(Vector3 position, Quaternion rotation, Vector3 localScale);
}
