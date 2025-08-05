using UnityEngine;
using MixedReality.Toolkit;


public class Model : Notification
{
    //ATTRIBUTES
    private GameObject model;
    private float spinningPeriod = 300;
    private RuntimeAnimatorController animatorController;



    //METHODS
    public Model(bool _playAudio, GameObject _model, float _spinningPeriod, RuntimeAnimatorController _animatorController)
    {
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


    public override GameObject SpawnObject(Vector3 position, Quaternion rotation, Vector3 localScale)
    {
        GameObject modelObject = Instantiate(model, position, rotation);
        modelObject.transform.localScale = localScale;
        AddStatefulInteractable(modelObject);
        AddCollider(modelObject);
        AddAnimation(modelObject);
        return modelObject;
    }
}
