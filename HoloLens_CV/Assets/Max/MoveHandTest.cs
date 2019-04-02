using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveHandTest : MonoBehaviour {

    public Vector3 direction;
    public Vector3 rotation;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        //this.transform.position = Vector3.Lerp(this.transform.position, this.transform.position + new Vector3(0, 0, speed), 0.1f);
        this.transform.position += direction;
        this.transform.rotation *= Quaternion.Euler(rotation);
    }
}
