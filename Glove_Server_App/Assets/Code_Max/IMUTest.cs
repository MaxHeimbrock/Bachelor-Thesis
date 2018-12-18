using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IMUTest : MonoBehaviour {

    public GameObject glove_controller;
    private Glove glove;
   
    public mode orientationMode = mode.acc;

    public Scrollbar bar;
    public float scrollspeed = 0.015f;
    public float scroll_value = 0f;

    public enum mode {acc, gyro, filtered, madgwick, madgwickFiltered};

    // Use this for initialization
	void Start () {
        //glove = glove_controller.GetComponent<EthernetGloveController>().glove;
    }
	
	// Update is called once per frame
	void Update () {
        if (glove == null)
        {
            glove = glove_controller.GetComponent<EthernetGloveController>().glove;
        }
        else
        {
            if (orientationMode == mode.acc)
            {
                this.transform.rotation = Quaternion.Inverse(glove.q_acc);
            }
            else if (orientationMode == mode.gyro)
            {
                this.transform.rotation = Quaternion.Inverse(glove.q_gyro);
            }
            else if (orientationMode == mode.filtered)
            {
                this.transform.rotation = Quaternion.Inverse(glove.q_filtered);
            }
            else if (orientationMode == mode.madgwick)
            {
                this.transform.rotation = Quaternion.Inverse(glove.q_madgwick);
                //scroll_absolute(glove.q_madgwick.eulerAngles.z);
                //scroll_relative(glove.q_madgwick.eulerAngles.z);
                scroll_relative_exp(glove.q_madgwick.eulerAngles.z);
            }
            else if (orientationMode == mode.madgwickFiltered)
            {
                this.transform.rotation = Quaternion.Inverse(glove.q_madgwick_filtered);
            }
        }
    }

    private void scroll_absolute(float z_angle)
    {
        float scroll_value;
        scroll_value = z_angle / 360;
        scroll_value *= 2;
        scroll_value += 0.5f;
        scroll_value = scroll_value % 1;
        bar.value = scroll_value;
    }

    // TODO: Exponential scrolling
    private void scroll_relative(float z_angle)
    {
        if (scroll_value < 0)
            scroll_value = 0;

        else if (scroll_value > 1)
            scroll_value = 1;

        float scroll = z_angle / 360;
        scroll += 0.5f;
        scroll = scroll % 1;
        scroll -= 0.5f;

        scroll_value += scroll * scrollspeed;
        bar.value = scroll_value;
    }

    private void scroll_relative_exp(float z_angle)
    {
        if (scroll_value < 0)
            scroll_value = 0;

        else if (scroll_value > 1)
            scroll_value = 1;

        float scroll = z_angle / 360;
        scroll += 0.5f;
        scroll = scroll % 1;
        scroll -= 0.5f;

        // TODO

        scroll_value += scroll * scrollspeed;
        bar.value = scroll_value;
    }
}
