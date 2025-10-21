using UnityEngine;
using MixedReality.Toolkit;


public class Model : Notification
{
    //ATTRIBUTES
    private GameObject model;
    private float spinningPeriod = 300;
    private RuntimeAnimatorController animatorController;



    //METHODS
    public Model(Vector3 _position, Vector3 _eulerRotation, Vector3 _localScale, bool _playAudio, GameObject _model, float _spinningPeriod, RuntimeAnimatorController _animatorController)
    {
        position = _position;
        eulerRotation = _eulerRotation;
        localScale = _localScale;
        playAudio = _playAudio;
        model = _model;
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


    public override GameObject SpawnObject()
    {
        GameObject modelObject = Instantiate(model, position, GetRotation());
        modelObject.transform.localScale = localScale;
        AddStatefulInteractable(modelObject);
        AddCollider(modelObject);
        AddAnimation(modelObject);
        return modelObject;
    }
}
