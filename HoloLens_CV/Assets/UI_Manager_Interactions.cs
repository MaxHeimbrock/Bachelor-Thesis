using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Manager_Interactions : UI_Manager {

    private enum LastGesture { None, Fist, Clap };

    public MySlider slider;
    public MyObject myObject;
    public GameObject handMesh;
    private Renderer handRenderer;

    int gestureTimer = 10;

    bool handVisible;
    bool meshOn = true;
    private LastGesture gesture;

    public override void Clap()
    {
        if (gesture != LastGesture.None)
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
        slider.Fist();
        myObject.Fist();
        gestureTimer = 10;
        gesture = LastGesture.Fist;
    }

    // Use this for initialization
    void Start () {
        handRenderer = null;
        if (handMesh)
        {
            handRenderer = handMesh.GetComponent<Renderer>();
        }
    }
	
	// Update is called once per frame
	void Update () {
        if (gestureTimer > 0)
            gestureTimer--;
        else
            gesture = LastGesture.None;

        handMesh.SetActive(meshOn);

        if (handRenderer)
        {
            switch (gesture)
            {
                case LastGesture.None:
                    handRenderer.material.color = Color.white;
                    break;
                case LastGesture.Fist:
                    handRenderer.material.color = Color.red;
                    break;
                case LastGesture.Clap:
                    handRenderer.material.color = Color.blue;
                    break;
            }
        }
    //Fist();
    }
}
