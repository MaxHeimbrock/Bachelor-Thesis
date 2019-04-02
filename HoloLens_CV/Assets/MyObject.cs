using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyObject : MonoBehaviour {

    Renderer renderer;
    public GameObject hand;

    int fistTimer = 0;

    // Use this for initialization
    void Start () {
        renderer = this.GetComponent<Renderer>();
    }
	
	// Update is called once per frame
	void Update () {
        if (Vector3.Distance(hand.transform.position, this.transform.position) < 0.2f)
            renderer.material.color = Color.red;
        else
            renderer.material.color = Color.white;

        if (fistTimer > 0)
            fistTimer--;

        else
            transform.parent = null;
    }

    public void Fist()
    {
        if (Vector3.Distance(hand.transform.position, this.transform.position) < 0.2f)
        {
            this.transform.SetParent(hand.transform, true);
            Debug.Log("hier");
            fistTimer = 5;
        }
    }
}
