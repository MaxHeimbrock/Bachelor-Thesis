using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Manager : MonoBehaviour {

    private enum LastGesture {None, Fist, Clap};

    public GameObject billboard;
    public HandAnchor handAnchor;
    public VideoPanel videoPanel;

    public GameObject light;
    public GameObject sphere;
    public GameObject cam;

    bool billboardOn = false;

    int gestureTimer = 10;

    private LastGesture gesture;

    public GameObject mesh;
    private Renderer renderer;

    Vector3 lastFistPos;
    Vector3 firstFistPos;
    Vector3 firstObjectPos;
    Vector3 currentFistPos;

    float lerpSpeed = 0.001f;

    // Use this for initialization
    void Start () {
        renderer = null;
        if (mesh)
        {
            renderer = mesh.GetComponent<Renderer>();
        }
    }
	
	// Update is called once per frame
	void Update () {

        //must be here
        billboard.SetActive(billboardOn);

        // Fist is still held
        if (gesture == LastGesture.Fist)
        {
            FistMovement();
        }

        if (gestureTimer > 0)
            gestureTimer--;
        else 
            gesture = LastGesture.None;

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
    }

    public void Clap()
    {
        if (gestureTimer > 0)
            return;
        else
        {
            gesture = LastGesture.Clap;
            // Do Something
            billboardOn = !billboardOn;            
            Debug.Log("Clap");
            gestureTimer = 10;
        }
    }

    public void Fist()
    {
        // Start of fist gesture
        if (gesture == LastGesture.None)
        {
            videoPanel.GetTrackingLocation(ref firstFistPos);
            firstObjectPos = light.transform.localPosition;
        }
            //videoPanel.GetTrackingLocation(ref lastFistPos);

        gesture = LastGesture.Fist;
        gestureTimer = 10;
        Debug.Log("Fist");

    }
    public void FistMovement()
    {
        videoPanel.GetTrackingLocation(ref currentFistPos);

        Vector3 movedPosition = currentFistPos - firstFistPos;
        movedPosition = Quaternion.Inverse(cam.transform.rotation) * movedPosition;
        light.transform.localPosition = Vector3.Lerp(light.transform.localPosition, firstObjectPos + new Vector3(movedPosition.x / 2, movedPosition.y / 2, 0), lerpSpeed);
    }

    /*
    public void FistMovement()
    {
        videoPanel.GetTrackingLocation(ref currentFistPos);

        Vector3 movedPosition = currentFistPos - lastFistPos;
        movedPosition = Quaternion.Inverse(cam.transform.rotation) * movedPosition;
        light.transform.localPosition += new Vector3(movedPosition.x/2, movedPosition.y/2, 0);
        sphere.transform.position += 0.1f * movedPosition;
        Debug.Log(sphere.transform.position);

        lastFistPos = currentFistPos;
    }*/
}
