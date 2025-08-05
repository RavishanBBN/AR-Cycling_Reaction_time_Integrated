using UnityEngine;


public class Sprite : Notification
{
    //ATTRIBUTES
    private Texture texture;
    private GameObject signObject;
    private Material signMaterial;



    //METHODS
    public Sprite(bool _playAudio, Texture _texture, GameObject _signObject, Material _signMaterial)
    {
        playAudio = _playAudio;
        texture = _texture;
        signObject = _signObject;
        signMaterial = _signMaterial;
    }


    public override GameObject SpawnObject(Vector3 position, Quaternion rotation, Vector3 localScale)
    {
        GameObject spriteObject = Instantiate(signObject, position, rotation);
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
