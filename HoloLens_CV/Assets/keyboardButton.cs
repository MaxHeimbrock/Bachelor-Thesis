using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class keyboardButton : MonoBehaviour {

    Vector3 origin;
    Renderer renderer;
    Collider collider;
    AudioSource sound;

    int timer = 20;

    enum State { start, pressed };

    State state = new State();

    // Use this for initialization
    void Start()
    {
        renderer = this.GetComponent<Renderer>();
        collider = this.GetComponent<Collider>();
        sound = this.GetComponent<AudioSource>();
        renderer.material.color = Color.black;

        origin = this.transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void CorrectPosition()
    {
        this.transform.localPosition = Vector3.Lerp(this.transform.localPosition, origin, 0.1f);
    }

}