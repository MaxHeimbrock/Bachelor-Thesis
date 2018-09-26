using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class hand_controller : MonoBehaviour {
	public GameObject glove_controller;

	public GameObject wrist;

	public GameObject thumb_base;
	public GameObject thumb_pip;
	public GameObject thumb_dip;

	public GameObject index_base;
	public GameObject index_pip;
	public GameObject index_dip;

	public GameObject middle_base;
	public GameObject middle_pip;
	public GameObject middle_dip;

	public GameObject ring_base;
	public GameObject ring_pip;
	public GameObject ring_dip;

	public GameObject little_base;
	public GameObject little_pip;
	public GameObject little_dip;

	private Glove glove; 

	private Quaternion[]  rot0;
	private float[]  q;
	// Use this for initialization
	void Start () {
		
		rot0 = new Quaternion[40];
		rot0[0] = thumb_base.transform.localRotation;
		rot0[1] = thumb_base.transform.localRotation;
		rot0[2] = thumb_pip.transform.localRotation;
		rot0[3] = thumb_dip.transform.localRotation;

		rot0[4] = index_base.transform.localRotation;
		rot0[5] = index_base.transform.localRotation;
		rot0[6] = index_pip.transform.localRotation;
		rot0[7] = index_dip.transform.localRotation;

		rot0[8] = middle_base.transform.localRotation;
		rot0[9] = middle_base.transform.localRotation;
		rot0[10] = middle_pip.transform.localRotation;
		rot0[11] = middle_dip.transform.localRotation;

		rot0[12] = ring_base.transform.localRotation;
		rot0[13] = ring_base.transform.localRotation;
		rot0[14] = ring_pip.transform.localRotation;
		rot0[15] = ring_dip.transform.localRotation;

		rot0[16] = little_base.transform.localRotation;
		rot0[17] = little_base.transform.localRotation;
		rot0[18] = little_pip.transform.localRotation;
		rot0[19] = little_dip.transform.localRotation;

		rot0[20] = wrist.transform.localRotation;
		rot0[21] = wrist.transform.localRotation;

		q = new float[40];
	}
	
	// Update is called once per frame
	void Update () {
		if (glove == null) {
			glove = glove_controller.GetComponent<serial_port_receiver> ().glove;
			return;
		}

		float t = Time.fixedTime;
		//Debug.Log (t);
		// get the last received data
		if(Time.fixedTime<2.0f){
				glove.set_zero ();
		}
		if (Input.GetKey("space")){
			glove.set_zero ();
		}
		if (Input.GetKey("space") && Input.GetKey("Ctrl")){
			//glove.set_zero ();
		}

		q[0] = 0.0f;
		q[1] = 180.0f/Mathf.PI*glove.values [1];
		q[2] = 180.0f/Mathf.PI*glove.values [2]; 
		q[3] = 180.0f/Mathf.PI*glove.values [3]; 

		q [4] = 0.0f;
		q[5] = 180.0f/Mathf.PI*glove.values [9]; 
		q[6] = 180.0f/Mathf.PI*glove.values [11];  // fake !!!!
		q[7] = 180.0f/Mathf.PI*glove.values [11]; 
		//
		q[8] = 0.0f;
		q[9] =  180.0f/Mathf.PI*glove.values [17]; 
		q[10] = 180.0f/Mathf.PI*glove.values [18]; 
		q[11] = 180.0f/Mathf.PI*glove.values [19]; 

		q [12] = 0.0f;
		q[13] = 180.0f/Mathf.PI*glove.values [25]; 
		q[14] = 180.0f/Mathf.PI*glove.values [26]; 
		q[15] = 180.0f/Mathf.PI*glove.values [27]; 

		q [16] = 0.0f;
		q[17] = 180.0f/Mathf.PI*glove.values [33]; 
		q[18] = 180.0f/Mathf.PI*glove.values [34]; 
		q[19] = 180.0f/Mathf.PI*glove.values [35]; 


		//6 -- does not wortk
		//14-- back right corner board
		//22-- side lerft(thumb)
		//30-- side right(little)
		q [20] = 180.0f/Mathf.PI*(0.0f*glove.values [6] + glove.values [14] ); 
		q[21] = 180.0f/Mathf.PI*( glove.values [22] - glove.values [30]); 

		for (int i = 0; i < 40; i++) {
			if(i==0){continue;}
			if(i==4){continue;}
			if(i==8){continue;}
			if(i==12){continue;}
			if(i==16){continue;}

			if (q [i] < -5.0f) {
				q [i] = -5.0f;
			}
			if (q [i] > 90.0f) {
				q [i] = 90.0f;
			}
		}

		Quaternion rot;
		Quaternion rotSide;

		rotSide = Quaternion.AngleAxis(q[20], Vector3.down);
		rot = Quaternion.AngleAxis(q[21], Vector3.forward);
		wrist.transform.localRotation = rot0[21]*rot*rotSide;

		rotSide = Quaternion.AngleAxis(q[0], Vector3.up);
		rot = Quaternion.AngleAxis(q[1], Vector3.forward);
		thumb_base.transform.localRotation = rot0[1]*rot*rotSide;
		rot = Quaternion.AngleAxis(q[2], Vector3.down);
		thumb_pip.transform.localRotation = rot0[2]*rot;
		rot = Quaternion.AngleAxis(q[3], Vector3.down);
		thumb_dip.transform.localRotation = rot0[3]*rot;


		rotSide = Quaternion.AngleAxis(q[4], Vector3.up);
		rot = Quaternion.AngleAxis(q[5], Vector3.forward);
		index_base.transform.localRotation = rot0[5]*rot*rotSide;
		rot = Quaternion.AngleAxis(q[6], Vector3.forward);
		index_pip.transform.localRotation  = rot0[6]*rot;
		rot = Quaternion.AngleAxis(q[7], Vector3.forward);
		index_dip.transform.localRotation = rot0[7]*rot;


		rotSide = Quaternion.AngleAxis(q[8], Vector3.up);
		rot = Quaternion.AngleAxis(q[9], Vector3.forward);
		middle_base.transform.localRotation = rot0[9]*rot*rotSide;
		rot = Quaternion.AngleAxis(q[10], Vector3.forward);
		middle_pip.transform.localRotation = rot0[10]*rot;
		rot = Quaternion.AngleAxis(q[11], Vector3.forward);
		middle_dip.transform.localRotation = rot0[11]*rot;

		rotSide = Quaternion.AngleAxis(q[12], Vector3.up);
		rot = Quaternion.AngleAxis(q[13], Vector3.forward);
		ring_base.transform.localRotation = rot0[13]*rot*rotSide;
		rot = Quaternion.AngleAxis(q[14], Vector3.forward);
		ring_pip.transform.localRotation = rot0[14]*rot;
		rot = Quaternion.AngleAxis(q[15], Vector3.forward);
		ring_dip.transform.localRotation = rot0[15]*rot;

		rotSide = Quaternion.AngleAxis(q[16], Vector3.up);
		rot = Quaternion.AngleAxis(q[17], Vector3.forward);
		little_base.transform.localRotation = rot0[17]*rot*rotSide;
		rot = Quaternion.AngleAxis(q[18], Vector3.forward);
		little_pip.transform.localRotation = rot0[18]*rot;
		rot = Quaternion.AngleAxis(q[19], Vector3.forward);
		little_dip.transform.localRotation = rot0[19]*rot;

	}




	void receive_thread_fcn(){



	}


}
