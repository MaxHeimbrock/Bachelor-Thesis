using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandAnchor : MonoBehaviour {

    int mode = 6;

    public GameObject VisionTracking;
    private VideoPanel videoProcessing;

    public UDPReceive UDP_Receive;

    Quaternion orientation;
    Vector3 position;

    public int LPF_filter_size = 6;
    private Vector3[] filter_array;
    float LPF_filter = 0.25f;

    // Use this for initialization
    void Start () {
        videoProcessing = VisionTracking.GetComponent<VideoPanel>();
    }
	
	// Update is called once per frame
	void Update () {
        orientation = UDP_Receive.GetOrientation();
        videoProcessing.GetTrackingLocation(ref position);

        //Debug.Log(orientation.eulerAngles);

        //Quaternion temp = new Quaternion(orientation.w, orientation.x, orientation.y, orientation.z);        

        // war hier auskommentiert vorhin???
        this.transform.localPosition = LowPassFilter(position);

        // This one is correct
        this.transform.rotation = new Quaternion(-orientation.z, orientation.y, -orientation.x, orientation.w) * Quaternion.AngleAxis(180, Vector3.forward);
    }

    public Vector3 LowPassFilter(Vector3 pos)
    {
        // initialize filter_array
        if (filter_array == null)
        {
            filter_array = new Vector3[LPF_filter_size];
            for (int i = 0; i < LPF_filter_size; i++)
                filter_array[i] = pos;
        }
        
        // get mean of all last accel vectors 
        Vector3 sum = Vector3.zero;

        for (int i = 0; i < LPF_filter_size; i++)
        {
            sum += filter_array[i];
        }

        sum /= LPF_filter_size;

        // push new value in filter_array
        for (int i = 0; i < LPF_filter_size - 1; i++)
            filter_array[i] = filter_array[i + 1];

        filter_array[LPF_filter_size - 1] = pos;

        return LPF_filter * sum + (1-LPF_filter) * pos;
    }
}
