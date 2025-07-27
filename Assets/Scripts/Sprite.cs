using UnityEngine;


public class Sprite : Notification
{
    //ATTRIBUTES
    public Texture texture;
    public GameObject signObject;
    public Material signMaterial;



    //METHODS
    public Sprite(Vector3 _position, Vector3 _eulerRotation, Vector3 _localScale, Texture _texture, GameObject _signObject, Material _signMaterial)
    {
        position = _position;
        eulerRotation = _eulerRotation;
        localScale = _localScale;
        texture = _texture;
        signObject = _signObject;
        signMaterial = _signMaterial;
    }


    public override GameObject SpawnObject()
    {
        GameObject spriteObject = Instantiate(signObject, position, GetRotation());
        spriteObject.transform.localScale = localScale;
        signMaterial.mainTexture = texture;

        MeshRenderer imageMeshRenderer = spriteObject.GetComponent<MeshRenderer>();
        if (imageMeshRenderer != null)
        {
            imageMeshRenderer.material = signMaterial;
        }

        return spriteObject;
    }
}
