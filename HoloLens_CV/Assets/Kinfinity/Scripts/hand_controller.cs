using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class hand_controller : MonoBehaviour {

    public UDPReceive UDP_Receive;

	public GameObject armature;
    public GameObject mesh;

	private Transform wrist_transform;

    private Transform thumb_base_transform;
	private Transform thumb_pip_transform;
	private Transform thumb_dip_transform;

	private Transform index_base_transform;
	private Transform index_pip_transform;
	private Transform index_dip_transform;

	private Transform middle_base_transform;
	private Transform middle_pip_transform;
	private Transform middle_dip_transform;

	private Transform ring_base_transform;
	private Transform ring_pip_transform;
	private Transform ring_dip_transform;

	private Transform little_base_transform;
	private Transform little_pip_transform;
	private Transform little_dip_transform;

	private Quaternion[]  rot0;
	private float[]  q;
    private Renderer renderer;
    private Quaternion wrist_rot0;

    float[] angleValues;
    
	// Use this for initialization
	void Start () {
		// pick up the data providers

        wrist_transform = armature.transform.Find("rh.wrist");

        // fill the armature tree
        thumb_base_transform = wrist_transform.transform.Find("rh.thumb.base");
        thumb_pip_transform  = thumb_base_transform.Find("rh.thumb.pip");
        thumb_dip_transform  = thumb_pip_transform.Find("rh.thumb.dip");

        index_base_transform = armature.transform.Find("rh.wrist/rh.index.base");
        index_pip_transform  = index_base_transform.Find("rh.index.pip");
        index_dip_transform  = index_pip_transform.Find("rh.index.dip");

        middle_base_transform = armature.transform.Find("rh.wrist/rh.middle.base");
        middle_pip_transform  = middle_base_transform.Find("rh.middle.pip");
        middle_dip_transform  = middle_pip_transform.Find("rh.middle.dip");

        ring_base_transform = armature.transform.Find("rh.wrist/rh.ring.base");
        ring_pip_transform  = ring_base_transform.Find("rh.ring.pip");
        ring_dip_transform  = ring_pip_transform.Find("rh.ring.dip");

        little_base_transform = armature.transform.Find("rh.wrist/rh.little.base");
        little_pip_transform  = little_base_transform.Find("rh.little.pip");
        little_dip_transform  = little_pip_transform.Find("rh.little.dip");

        // save the initial transformations
		rot0 = new Quaternion[40];
		rot0[0] = thumb_base_transform.localRotation;
		rot0[1] = thumb_base_transform.localRotation;
		rot0[2] = thumb_pip_transform.localRotation;
		rot0[3] = thumb_dip_transform.localRotation;

		rot0[4] = index_base_transform.localRotation;
		rot0[5] = index_base_transform.localRotation;
		rot0[6] = index_pip_transform.localRotation;
		rot0[7] = index_dip_transform.localRotation;

		rot0[8] = middle_base_transform.localRotation;
		rot0[9] = middle_base_transform.localRotation;
		rot0[10] = middle_pip_transform.localRotation;
		rot0[11] = middle_dip_transform.localRotation;

		rot0[12] = ring_base_transform.localRotation;
		rot0[13] = ring_base_transform.localRotation;
		rot0[14] = ring_pip_transform.localRotation;
		rot0[15] = ring_dip_transform.localRotation;

		rot0[16] = little_base_transform.localRotation;
		rot0[17] = little_base_transform.localRotation;
		rot0[18] = little_pip_transform.localRotation;
		rot0[19] = little_dip_transform.localRotation;

		rot0[20] = wrist_transform.localRotation;
		rot0[21] = wrist_transform.localRotation;
        wrist_rot0 = wrist_transform.localRotation;

		q = new float[40];

        renderer=null;
        if(mesh){
            renderer = mesh.GetComponent<Renderer>();
        }
	}
	
	// Update is called once per frame
	void Update () {

        angleValues = UDP_Receive.GetJointAngles();

        /*
		float t = Time.fixedTime;
		//Debug.Log (t);
		// get the last received data
        if (renderer){            
            switch(gesture_packet.gesture){
                case 1:
                renderer.material.color = Color.green;
                break;
                case 2:
                renderer.material.color = Color.red;
                break;
                case 3:
                renderer.material.color = Color.blue;
                break;
            default:
                renderer.material.color = Color.white;
                break;
            }
        }
        */

		q[0] = 0.0f;
		q[1] = 180.0f/Mathf.PI*angleValues [1];
		q[2] = 180.0f/Mathf.PI*angleValues [2]; 
		q[3] = 180.0f/Mathf.PI*angleValues [3]; 

		q[4] = 0.0f;
		q[5] = 180.0f/Mathf.PI*angleValues [9]; 
		q[6] = 180.0f/Mathf.PI*angleValues [10];  // fake !!!!
		q[7] = 180.0f/Mathf.PI*angleValues [11]; 
		//
		q[8] = 0.0f;
		q[9] =  180.0f/Mathf.PI*angleValues [17]; 
		q[10] = 180.0f/Mathf.PI*angleValues [18]; 
		q[11] = 180.0f/Mathf.PI*angleValues [19]; 

		q[12] = 0.0f;
		q[13] = 180.0f/Mathf.PI*angleValues [25]; 
		q[14] = 180.0f/Mathf.PI*angleValues [26]; 
		q[15] = 180.0f/Mathf.PI*angleValues [27]; 

		q[16] = 0.0f;
		q[17] = 180.0f/Mathf.PI*angleValues [33]; 
		q[18] = 180.0f/Mathf.PI*angleValues [34]; 
		q[19] = 180.0f/Mathf.PI*angleValues [35]; 


		//6 -- does not wort
		//14-- back right corner board
		//22-- side lerft(thumb)
		//30-- side right(little)
		q[20] = 180.0f/Mathf.PI*(0.0f*angleValues [6] + angleValues [14] ); 
		q[21] = 180.0f/Mathf.PI*(     angleValues [22] - angleValues [30]); 

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

		rotSide = Quaternion.AngleAxis(q[0], Vector3.up);
		rot = Quaternion.AngleAxis(q[1], Vector3.forward);
		thumb_base_transform.localRotation = rot0[1]*rot*rotSide;
		rot = Quaternion.AngleAxis(q[2], Vector3.down);
		thumb_pip_transform.localRotation = rot0[2]*rot;
		rot = Quaternion.AngleAxis(q[3], Vector3.down);
		thumb_dip_transform.localRotation = rot0[3]*rot;


		rotSide = Quaternion.AngleAxis(q[4], Vector3.up);
		rot = Quaternion.AngleAxis(q[5], Vector3.forward);
		index_base_transform.localRotation = rot0[5]*rot*rotSide;
		rot = Quaternion.AngleAxis(q[6], Vector3.forward);
		index_pip_transform.localRotation  = rot0[6]*rot;
		rot = Quaternion.AngleAxis(q[7], Vector3.forward);
		index_dip_transform.localRotation = rot0[7]*rot;


		rotSide = Quaternion.AngleAxis(q[8], Vector3.up);
		rot = Quaternion.AngleAxis(q[9], Vector3.forward);
		middle_base_transform.localRotation = rot0[9]*rot*rotSide;
		rot = Quaternion.AngleAxis(q[10], Vector3.forward);
		middle_pip_transform.localRotation = rot0[10]*rot;
		rot = Quaternion.AngleAxis(q[11], Vector3.forward);
		middle_dip_transform.localRotation = rot0[11]*rot;

		rotSide = Quaternion.AngleAxis(q[12], Vector3.up);
		rot = Quaternion.AngleAxis(q[13], Vector3.forward);
		ring_base_transform.localRotation = rot0[13]*rot*rotSide;
		rot = Quaternion.AngleAxis(q[14], Vector3.forward);
		ring_pip_transform.localRotation = rot0[14]*rot;
		rot = Quaternion.AngleAxis(q[15], Vector3.forward);
		ring_dip_transform.localRotation = rot0[15]*rot;

		rotSide = Quaternion.AngleAxis(q[16], Vector3.up);
		rot = Quaternion.AngleAxis(q[17], Vector3.forward);
		little_base_transform.localRotation = rot0[17]*rot*rotSide;
		rot = Quaternion.AngleAxis(q[18], Vector3.forward);
		little_pip_transform.localRotation = rot0[18]*rot;
		rot = Quaternion.AngleAxis(q[19], Vector3.forward);
		little_dip_transform.localRotation = rot0[19]*rot;

    }
}
