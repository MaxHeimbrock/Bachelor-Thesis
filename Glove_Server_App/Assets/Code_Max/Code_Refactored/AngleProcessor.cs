using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public abstract class AngleProcessor {

    const int NB_VALUES_GLOVE = 40;

    protected float[] angles;
    protected UInt32[] offsets;
    protected UInt32[] raw_values;

    public AngleProcessor()
    {
        raw_values = new UInt32[NB_VALUES_GLOVE];
        offsets = new UInt32[NB_VALUES_GLOVE];
        angles = new float[NB_VALUES_GLOVE];
    }

    // Integrates the values from joint sensors to real angles
    public abstract void ProcessAngles(UInt32[] Values);

    public float[] GetAngles()
    {
        return angles;
    }

    public void SetZero()
    {
        Debug.Log("set_zero");

        for (int i = 0; i < Constants.NB_SENSORS; i++)
        {
            offsets[i] = raw_values[i];
        }
    }
}

public class EthernetAngleProcessor : AngleProcessor
{
    private float filter = 0.9f;
    

    public override void ProcessAngles(uint[] jointValues)
    {
        float sum = 0;

        raw_values = jointValues;

        for (int i = 0; i < Constants.NB_SENSORS; i++)
        {
            Int64 tmp = ((Int64)jointValues[i]) - ((Int64)offsets[i]);
            double tmpd = (double)tmp; // I use double here to avoid loosing to much precision
            tmpd = 0.00001f * tmpd; // That should be the same scale as for the serial glove
            double filtered_value = (1.0f - filter) * tmpd + filter * angles[i];

            angles[i] = (float)filtered_value; // finally cut it to float, the precision should be fine at that point
            sum += angles[i];
            //Debug.Log(angles[1]);
        }

        if (sum > 20f)
            Debug.Log("fist");

       
    }
}
