using UnityEngine;
using MixedReality.Toolkit;


public class Model : Notification
{
    //ATTRIBUTES
    private GameObject model;
    private Vector3 modelScale;
    private float spinningPeriod = 300;
    private RuntimeAnimatorController animatorController;



    //METHODS
    public Model(bool _playAudio, SpawnPosition _spawnPosition, GameObject _model, Vector3 _modelScale, float _spinningPeriod, RuntimeAnimatorController _animatorController)
    {
        playAudio = _playAudio;
        spawnPosition = _spawnPosition;
        model = _model;
        modelScale = _modelScale;
        spinningPeriod = _spinningPeriod;
        animatorController = _animatorController;
    }


    private void AddStatefulInteractable(GameObject modelObject)
    {
        if (modelObject.GetComponent<StatefulInteractable>() == null)
        {
            modelObject.AddComponent<StatefulInteractable>();
        }
    }

    
    private void AddCollider(GameObject modelObject)
    {
        if (modelObject.GetComponent<Collider>() == null)
        {
            modelObject.AddComponent<SphereCollider>();
        }
    }


    private void AddAnimation(GameObject modelObject)
    {
        Animator animator = modelObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = modelObject.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = animatorController;

        if (animatorController == null)
        {
            SpinningAnimation spinningAnimation = modelObject.GetComponent<SpinningAnimation>();
            if (spinningAnimation == null)
            {
                spinningAnimation = modelObject.AddComponent<SpinningAnimation>();
            }
            spinningAnimation.model = modelObject;
            spinningAnimation.SetActive(true);
            spinningAnimation.SetDuration(spinningPeriod);
        }
    }


    public override GameObject SpawnObject(Vector3 position, Quaternion rotation, Vector3 localScale)
    {
        GameObject modelObject = Instantiate(model, position + spawnPosition.GetPosition(), rotation * spawnPosition.GetRotation());
        Vector3 totalScale = new Vector3(modelScale.x * localScale.x, modelScale.y * localScale.y, modelScale.z * localScale.z);
        modelObject.transform.localScale = GetLocalScale(totalScale);
        AddStatefulInteractable(modelObject);
        AddCollider(modelObject);
        AddAnimation(modelObject);
        return modelObject;
    }
}
