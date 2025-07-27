using UnityEngine;

public class SpinningAnimation : MonoBehaviour
{
    //ATTRIBUTES
    private float timer = 0;
    public float duration = 300;
    public GameObject model;
    private bool active;
    private float rotationIncrement;



    //METHODS
    public void SetActive(bool state)
    {
        active = state;
    }

    public void SetDuration(float period)
    {
        duration = period;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timer = 0;
        rotationIncrement = 360 / duration;
    }

    // Update is called once per frame
    void Update()
    {
        if (active)
        {
            if (timer < duration)
            {
                timer += Time.deltaTime;
            }
            else
            {
                timer = 0;
            }
            model.transform.Rotate(0, rotationIncrement * Time.deltaTime, 0);
        }
    }
}
