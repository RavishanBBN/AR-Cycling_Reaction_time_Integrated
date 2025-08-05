using UnityEngine;


[System.Serializable]
public abstract class Notification : Object
{
    //ATTRIBUTES
    protected bool playAudio;



    //METHODS
    public bool GetPlayAudio()
    {
        return playAudio;
    }


    public abstract GameObject SpawnObject(Vector3 position, Quaternion rotation, Vector3 localScale);
}
