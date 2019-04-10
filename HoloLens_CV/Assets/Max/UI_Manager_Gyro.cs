using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Manager_Gyro : UI_Manager
{

    private enum LastGesture {None, Fist, Clap};

    public GameObject billboard;
    public HandAnchor handAnchor;
    public VideoPanel videoPanel;
    public GameObject handMesh;
    public GameObject cursor;
    public GameObject cam;

    bool billboardOn = false;
    bool meshOn = true;

    int gestureTimer = 10;

    private LastGesture gesture;

    private Renderer renderer;

    Vector3 firstFistPos;
    Vector3 firstObjectPos;
    Vector3 currentFistPos;

    float lerpSpeed = 0.1f;

    int timer = 100;

    // Use this for initialization
    void Start () {
        renderer = null;
        if (handMesh)
        {
            renderer = handMesh.GetComponent<Renderer>();
        }
    }
	
	// Update is called once per frame
	void Update () {

        //must be here
        billboard.SetActive(billboardOn);

        handMesh.SetActive(meshOn);

        // Fist is still held
        if (gesture == LastGesture.Fist)
        {
            if (gestureTimer >= 1)
                FistMovement();
            else
                FistReleased();
        }

        if (gestureTimer > 0)
            gestureTimer--;
        else
        {
            gesture = LastGesture.None;
        }

        if (renderer)
        {
            switch (gesture)
            {
                case LastGesture.None:
                    renderer.material.color = Color.white;
                    break;
                case LastGesture.Fist:
                    renderer.material.color = Color.red;
                    break;
                case LastGesture.Clap:
                    renderer.material.color = Color.blue;
                    break;
            }
        }

        /*
        timer--;
        if (timer == 0)
        {
            Debug.Log("timer abgelaufen");
            Fist();
            timer = 1;
        }
        */
    }

    public override void Clap()
    {
        if (gestureTimer > 0)
            return;
        else
        {
            gesture = LastGesture.Clap;
            // Do Something
            meshOn = !meshOn;
            Debug.Log("Clap");
            gestureTimer = 10;
        }
    }

    public override void Fist()
    {
        // Start of fist gesture
        if (gesture == LastGesture.None)
        {
            firstFistPos = handAnchor.transform.rotation.eulerAngles;            
            billboard.transform.position = cam.transform.position + cam.transform.rotation * Vector3.forward;
        }

        gesture = LastGesture.Fist;
        billboardOn = true;
        gestureTimer = 10;
        Debug.Log("Fist");
    }

    public void FistMovement()
    {
        Vector3 movedPosition = handAnchor.transform.rotation.eulerAngles - firstFistPos;
        movedPosition *= 0.01f;
        cursor.transform.localPosition = Vector3.Lerp(cursor.transform.localPosition, new Vector3(movedPosition.y, movedPosition.x, 0), lerpSpeed);
    }

    public void FistReleased()
    {
        billboardOn = false;
        cursor.transform.localPosition = Vector3.zero;
    }
}
