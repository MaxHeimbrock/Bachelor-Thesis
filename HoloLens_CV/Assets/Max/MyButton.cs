using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyButton : MonoBehaviour {

    //Vector3 origin = new Vector3(0, 0, -0.5f);
    Vector3 origin = new Vector3(0, 0, -0.7f);
    Renderer renderer;
    Collider collider;
    AudioSource sound;

    enum State {start, pressed};

    State state = new State();

    // Use this for initialization
    void Start () {
        renderer = this.GetComponent<Renderer>();
        collider = this.GetComponent<Collider>();
        sound = this.GetComponent<AudioSource>();
        renderer.material.color = Color.black;
    }
	
	// Update is called once per frame
	void Update () {
        if (state == State.start && this.transform.localPosition.z > 0)
        {
            state = State.pressed;
            renderer.material.color = Color.red;
            collider.enabled = !collider.enabled;
            sound.Play();
        }

        if (state == State.pressed)
        {
            CorrectPosition();
            if (this.transform.localPosition.z < -0.65f)
            {
                state = State.start;
                renderer.material.color = Color.black;
                collider.enabled = !collider.enabled;
            }
        }

        if (this.transform.localPosition.z < -0.72f)
            CorrectPosition();
	}

    void CorrectPosition()
    {
        this.transform.localPosition = Vector3.Lerp(this.transform.localPosition, origin, 0.1f);
    }
}