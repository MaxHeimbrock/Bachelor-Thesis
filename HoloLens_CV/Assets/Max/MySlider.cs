using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MySlider : MonoBehaviour {

    float leftEnd = -0.45f;
    float rightEnd = 0.45f;

    Renderer renderer;
    Collider collider;

    public GameObject hand;

    // Use this for initialization
    void Start () {
        renderer = this.GetComponent<Renderer>();
        collider = this.GetComponent<Collider>();
    }
	
	// Update is called once per frame
	void Update () {
        if (Vector3.Distance(hand.transform.position, this.transform.position) < 0.2f)
            renderer.material.color = Color.red;
        else
            renderer.material.color = Color.white;

        if (transform.localPosition.x < leftEnd)
            transform.localPosition = new Vector3(leftEnd, 0, 0);

        if (transform.localPosition.x > rightEnd)
            transform.localPosition = new Vector3(rightEnd, 0, 0);
    }

    public void Fist()
    {
        if (Vector3.Distance(hand.transform.position, this.transform.position) < 0.2f)
        {
            float xPos = (transform.InverseTransformPoint(hand.transform.position)/5).x;

            this.transform.localPosition = Vector3.Lerp(this.transform.localPosition, new Vector3(xPos, 0, 0), 0.1f);
        }
    }
}
