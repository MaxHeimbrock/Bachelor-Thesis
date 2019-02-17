using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Manager_CV : UI_Manager {

    public bool debugging = false;

    private enum LastGesture {None, Fist, Clap};
    private enum ColorEnum {Red, Blue, Yellow, Green, None};

    ColorEnum colorSelected = ColorEnum.None;
    ColorEnum lastColorSelected = ColorEnum.None;
    ColorEnum colorClicked = ColorEnum.None;

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

    private Renderer handRenderer;

    public GameObject redButton;
    private Renderer redRenderer;
    private Color redStartColor;
    public GameObject blueButton;
    private Renderer blueRenderer;
    private Color blueStartColor;
    public GameObject yellowButton;
    private Renderer yellowRenderer;
    private Color yellowStartColor;
    public GameObject greenButton;
    private Renderer greenRenderer;
    private Color greenStartColor;

    // between 0 and 1
    float colorBlend = 0;

    float clickColorBlend = 0;

    Vector3 firstFistPos;
    Vector3 currentFistPos;

    float lerpSpeed = 0.1f;

    int timer = 100;

    Vector3 lastHandPos;
    Vector3 thisHandPos;
    bool handVisible;

    // Use this for initialization
    void Start () {
        handRenderer = null;
        if (handMesh)
        {
            handRenderer = handMesh.GetComponent<Renderer>();
        }

        redRenderer = redButton.GetComponent<Renderer>();
        redStartColor = redRenderer.material.color;
        blueRenderer = blueButton.GetComponent<Renderer>();
        blueStartColor = blueRenderer.material.color;
        yellowRenderer = yellowButton.GetComponent<Renderer>();
        yellowStartColor = yellowRenderer.material.color;
        greenRenderer = greenButton.GetComponent<Renderer>();
        greenStartColor = greenRenderer.material.color;
    }
	
	// Update is called once per frame
	void Update () {

        thisHandPos = videoPanel.GetLocalTrackingLocation();

        //Debug.Log(thisHandPos); 

        if (Math.Abs(thisHandPos.x) < 0.33 && Math.Abs(thisHandPos.y) < 0.23)
            handVisible = true;
        else
            handVisible = false;

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

        if (debugging)
        {
            timer--;
            if (timer <= 0)
            {
                Debug.Log("timer abgelaufen");
                Fist();
            }
        }
        

        if (colorClicked != ColorEnum.None)
            Clicked();
    }

    bool IsHandVisible()
    {
        videoPanel.GetTrackingLocation(ref thisHandPos);

        if (thisHandPos == lastHandPos)
            return false;

        lastHandPos = thisHandPos;

        return true;
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
        if (gesture == LastGesture.None && handVisible == true)
        {
            videoPanel.GetTrackingLocation(ref firstFistPos);
            billboard.transform.position = cam.transform.position + cam.transform.rotation * Vector3.forward * 0.7f;
            gesture = LastGesture.Fist;
            billboardOn = true;
            Debug.Log("Fist started");
        }

            gestureTimer = 10;

    }
    public void FistMovement()
    {
        videoPanel.GetTrackingLocation(ref currentFistPos);

        if (debugging)
        {
            currentFistPos = new Vector3(+0.038f, 0, 0);

            if (timer < -40)
                currentFistPos = new Vector3(+0.008f, 0.04f, 0);
        }
        

        Vector3 movedPosition = currentFistPos - firstFistPos;
        movedPosition = Quaternion.Inverse(cam.transform.rotation) * movedPosition;
        cursor.transform.localPosition = Vector3.Lerp(cursor.transform.localPosition, new Vector3(movedPosition.x / 2, movedPosition.y / 2, 0), lerpSpeed);

        // save magnitude for later
        float magnitude = cursor.transform.localPosition.magnitude;
        Vector3 pos = cursor.transform.localPosition;

        if (pos.y - Math.Abs(pos.x) > 0)
            colorSelected = ColorEnum.Blue;

        if (pos.y + Math.Abs(pos.x) < 0)
            colorSelected = ColorEnum.Red;

        if (pos.x - Math.Abs(pos.y) > 0)
            colorSelected = ColorEnum.Green;

        if (pos.x + Math.Abs(pos.y) < 0)
            colorSelected = ColorEnum.Yellow;

        if (magnitude >= 0.045f)
        {
            cursor.transform.localPosition = cursor.transform.localPosition.normalized * 0.045f;
            colorClicked = colorSelected;
        }

        if (colorClicked == ColorEnum.None)
            ChangeColors(magnitude);
    }

    public void FistReleased()
    {
        billboardOn = false;
        cursor.transform.localPosition = Vector3.zero;
        colorSelected = ColorEnum.None;
        lastColorSelected = ColorEnum.None;
        colorBlend = 0;
    }

    public void ChangeColors(float magnitude)
    {
        if (magnitude >= 0.012f)
            colorBlend += magnitude * 0.9f;
        else
        {
            // still in center
            colorSelected = ColorEnum.None;
            colorBlend -= 0.05f;
        }

        // reset
        if (colorSelected != lastColorSelected || colorSelected == ColorEnum.None)
        {
            redRenderer.material.color = redStartColor;
            blueRenderer.material.color = blueStartColor;
            greenRenderer.material.color = greenStartColor;
            yellowRenderer.material.color = yellowStartColor;
            colorBlend = 0;
        }


        switch (colorSelected)
        {
            case ColorEnum.Blue:
                blueRenderer.material.color = Color.Lerp(blueStartColor, Color.blue, colorBlend);
                redRenderer.material.color = Color.Lerp(redStartColor, Color.white, colorBlend);
                yellowRenderer.material.color = Color.Lerp(yellowStartColor, Color.white, colorBlend);
                greenRenderer.material.color = Color.Lerp(greenStartColor, Color.white, colorBlend);
                break;

            case ColorEnum.Red:
                blueRenderer.material.color = Color.Lerp(blueStartColor, Color.white, colorBlend);
                redRenderer.material.color = Color.Lerp(redStartColor, Color.red, colorBlend);
                yellowRenderer.material.color = Color.Lerp(yellowStartColor, Color.white, colorBlend);
                greenRenderer.material.color = Color.Lerp(greenStartColor, Color.white, colorBlend);
                break;

            case ColorEnum.Yellow:
                blueRenderer.material.color = Color.Lerp(blueStartColor, Color.white, colorBlend);
                redRenderer.material.color = Color.Lerp(redStartColor, Color.white, colorBlend);
                yellowRenderer.material.color = Color.Lerp(yellowStartColor, Color.yellow, colorBlend);
                greenRenderer.material.color = Color.Lerp(greenStartColor, Color.white, colorBlend);
                break;

            case ColorEnum.Green:
                blueRenderer.material.color = Color.Lerp(blueStartColor, Color.white, colorBlend);
                redRenderer.material.color = Color.Lerp(redStartColor, Color.white, colorBlend);
                yellowRenderer.material.color = Color.Lerp(yellowStartColor, Color.white, colorBlend);
                greenRenderer.material.color = Color.Lerp(greenStartColor, Color.green, colorBlend);
                break;
        }

        //Debug.Log(colorBlend);

        if (colorBlend >= 1)
            colorClicked = colorSelected;

        lastColorSelected = colorSelected;
    }

    private void Clicked()
    {
        clickColorBlend += 0.06f;

        switch (colorClicked)
        {
            case ColorEnum.Blue:

                redRenderer.material.color = Color.Lerp(Color.white, Color.blue, clickColorBlend);
                yellowRenderer.material.color = Color.Lerp(Color.white, Color.blue, clickColorBlend);
                greenRenderer.material.color = Color.Lerp(Color.white, Color.blue, clickColorBlend);
                break;

            case ColorEnum.Red:
                blueRenderer.material.color = Color.Lerp(Color.white, Color.red, clickColorBlend);

                yellowRenderer.material.color = Color.Lerp(Color.white, Color.red, clickColorBlend);
                greenRenderer.material.color = Color.Lerp(Color.white, Color.red, clickColorBlend);
                break;

            case ColorEnum.Yellow:
                blueRenderer.material.color = Color.Lerp(Color.white, Color.yellow, clickColorBlend);
                redRenderer.material.color = Color.Lerp(Color.white, Color.yellow, clickColorBlend);

                greenRenderer.material.color = Color.Lerp(Color.white, Color.yellow, clickColorBlend);
                break;

            case ColorEnum.Green:
                blueRenderer.material.color = Color.Lerp(Color.white, Color.green, clickColorBlend);
                redRenderer.material.color = Color.Lerp(Color.white, Color.green, clickColorBlend);
                yellowRenderer.material.color = Color.Lerp(Color.white, Color.green, clickColorBlend);

                break;
        }

        if (clickColorBlend >= 1)
        {
            colorClicked = ColorEnum.None;
            clickColorBlend = 0;

            //Do Something

            FistReleased();
        }

    }
}
