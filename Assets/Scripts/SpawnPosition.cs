using UnityEngine;

public class SpawnPosition
{
    //ATTRIBUTES
    private Vector3 position;
    private Vector3 eulerRotation;
    private Vector3 localScale;



    //METHODS
    public SpawnPosition(Vector3 _position, Vector3 _eulerRotation, Vector3 _localScale)
    {
        position = _position;
        eulerRotation = _eulerRotation;
        localScale = _localScale;
    }


    public Vector3 GetPosition()
    {
        return position;
    }


    public Quaternion GetRotation()
    {
        return Quaternion.Euler(eulerRotation);
    }


    public Vector3 GetLocalScale()
    {
        return localScale;
    }
}
