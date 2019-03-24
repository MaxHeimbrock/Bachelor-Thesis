using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyObject : MonoBehaviour {

    Renderer renderer;
    public GameObject hand;

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
    }

    public void Fist()
    {
        if (Vector3.Distance(hand.transform.position, this.transform.position) < 0.2f)
        {
            Debug.Log("Hier");
            this.transform.SetParent(hand.transform, true);
        }
    }
}
