using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Glove
{
    public UInt16 NB_SENSORS = 40;
    public UInt32 cnt;
    public float[] values;
    public UInt16 version;

    private UInt32[] offsets;
    private UInt32[] raw_values;
    
    // for imu testing
    private Vector3 acceleration;
    private Vector3 velocity;
    public Vector3 position;
    public Quaternion q;
    public float AccXangle;
    public float AccYangle;
    public Vector3 rotation;

    float G_Gain = 0.07f; // to get degrees per second with 2000dps http://ozzmaker.com/berryimu/

    private Vector3 acceleration_bias;
    private Vector3 gyro_bias;
    private int bias_counter = 0;
    private int bias_length = 100;

    long time0;

    public Glove()
    {
        cnt = 0;
        version = 0;
        raw_values = new UInt32[Constants.NB_SENSORS];
        offsets = new UInt32[Constants.NB_SENSORS];
        values = new float[Constants.NB_SENSORS];

        acceleration = new Vector3(0, 0, 0);
        velocity = new Vector3(0, 0, 0);
        position = new Vector3(0, 0, 5);

        time0 = 0;
}

    public void set_zero()
    {
        Debug.Log("set_zero");

        for (int i = 0; i < Constants.NB_SENSORS; i++)
        {
            offsets[i] = raw_values[i];
        }        
    }

    public void apply_packet(Packet packet)
    {
     //  version = packet.version;
     //  cnt++;
     //
     //  for (int i = 0; i < Constants.NB_SENSORS; i++)
     //      raw_values[i] = (Int64)(raw_values[i] + packet.values[i]);
     //
     //  raw_values[packet.key] = packet.value;
     //
     //  for (int i = 0; i < Constants.NB_SENSORS; i++)
     //      values[i] = 0.001f * (raw_values[i] - offsets[i]);
    }


    public void apply_ethernetJointPacket(UInt32[] newValues)
    {
        cnt++;
 
        float filter = 0.9f;

        raw_values = newValues;
 
        for (int i = 0; i < Constants.NB_SENSORS; i++)
        {
            Int64 tmp = ((Int64)newValues[i]) - ((Int64)offsets[i]);
            double tmpd = (double)tmp; // I use double here to avoid loosing to much precision
            tmpd = 0.001f * tmpd; // That should be the same scale as for the serial glove
            double filtered_value = (1.0f - filter) * tmpd + filter * values[i];
            values[i] = (float)filtered_value; // finally cut it to float, the precision should be fine at that point
            //Debug.Log(values[1]);
        }
    }

    /* taken from apply packet but didnt work
    public void apply_ethernetJointPacket(UInt32[] newValues)
    {
        cnt++;
                
        for (int i = 0; i < Constants.NB_SENSORS; i++)
        {
            raw_values[i] = (Int64)(raw_values[i] + newValues[i]);
            values[i] = 0.001f * (raw_values[i] - offsets[i]);
        }
    }
    */

    public void applyEthernetPacketIMU(Vector3 acceleration1, Vector3 gyroscope)
    {
        // testing double integration, just playing around

        long time1 = DateTime.Now.Ticks;
        Vector3 velocity1;
        Vector3 position1;

        if (bias_counter > bias_length)
        {
            acceleration1 -= acceleration_bias;
            gyroscope -= gyro_bias;

            // Range of Values is too big
            acceleration1 /= 10000000000;
            //acceleration1 /= 16384;

            //Debug.Log(acceleration1);

            TimeSpan elapsedSpan = new TimeSpan(time1 - time0);
            long delta_t_ms = elapsedSpan.Milliseconds;
            float delta_t_s = delta_t_ms / 1000;

            //Debug.Log(delta_t);

            //velocity1 = velocity + acceleration + (acceleration1 - acceleration) / 2;
            //position1 = position + velocity + (velocity1 - velocity) / 2;

            velocity1 = velocity + acceleration1 * delta_t_ms;
            position1 = position + velocity1 * delta_t_ms;

            position = position1;
            velocity = velocity1;
            acceleration = acceleration1;

            // Orientation Tests

            // X-axis
            AccXangle = (float)((Math.Atan2(acceleration1.x, acceleration1.z) + Math.PI) * (180/Math.PI)); // andere Rechnung http://ozzmaker.com/berryimu/
            // diese Rechnung korrigiert Orientierung zu 0 - 360 grad
            //AccXangle = (AccXangle*2 - 180);     

            if (AccXangle > 180)
                AccXangle -= (float)360;

            // Y-axis
            AccYangle = (float)((Math.Atan2(acceleration1.y, acceleration1.z) + Math.PI) * (180 / Math.PI)); // andere Rechnung
            // diese Rechnung korrigiert Orientierung zu 0 - 360 grad
            //AccYangle = (AccYangle * -2) + 540;

            if (AccYangle > 180)
                AccYangle -= (float)360;

            q = Quaternion.Euler(0, 0, AccYangle);

            // noch eine formel https://stackoverflow.com/questions/3755059/3d-accelerometer-calculate-the-orientation

            //double Roll = 2 * Math.Atan2(acceleration1.y, acceleration.z) * 180 / Math.PI;
            //double Pitch = 2 * Math.Atan2(-acceleration1.x, Math.Sqrt(acceleration1.y * acceleration1.y + acceleration1.z * acceleration1.z)) * 180 / Math.PI;
            //
            //Debug.Log(Pitch);
            //
            //q = Quaternion.Euler((float)Pitch, 0, 0);
            
            // eigentlich so
            rotation += (gyroscope * delta_t_ms * G_Gain) / (1000);

            //Debug.Log("gyroscope degree = " + rotation.x);
            Debug.Log("accelerometer degree = " + AccXangle);

            // complementary filter
            float filter = 0.98f;

            //rotation.x = filter * rotation.x + (1 - filter) * AccXangle;
            //rotation.y = filter * rotation.y + (1 - filter) * AccYangle;

            // TODO: Achsen sind zwischen ACC und GYRO unterschiedlich!!

            // switching axis
            float tmp = rotation.z;

            rotation.z = rotation.y;

            rotation.y = tmp;

            //q = Quaternion.Euler(rotation);
        }

        // Die ersten 1000 Messungen setzen den Bias, also die Gravitation
        else if (bias_counter == bias_length)
        {
            acceleration_bias /= 1000;
            gyro_bias /= 1000;
            bias_counter++;
            Debug.Log("Acceleration bias is " + acceleration_bias);
            Debug.Log("Gyroscope bias is " + gyro_bias);
        }
        else
        {
            // trying to get a bias for flat on table
            acceleration_bias += acceleration1;
            gyro_bias += gyroscope;

            bias_counter++;
        }

        time0 = time1;
    }

    public TrackingData GetTrackingData()
    {
        // All Dummy Values but acc for testing
        //float[] JointValues = new float[40];

        //for (int i = 0; i < JointValues.Length; i++)
            //JointValues[i] = i;

        Vector3 vel = new Vector3(1, 2, 3);
        //Vector3 acc = new Vector3(2, 4, 6);
        Matrix4x4 pose = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1));

        return new TrackingData(values, pose, vel, acceleration, 2.0);
    }

    }

static class Constants
{
    public const int NB_SENSORS = 40;
    public const bool IS_BLUETOOTH = false;
}

public class TrackingData
{

    public float[] JointValues;
    public Matrix4x4 pose;
    public Vector3 velocity;
    public Vector3 acceleration;
    public double timestamp;

    // Dummy for testing
    public TrackingData()
    {
        JointValues = new float[40];

        for (int i = 0; i < JointValues.Length; i++)
            JointValues[i] = i;

        pose = Matrix4x4.identity;

        velocity = new Vector3(1, 1, 1);
        acceleration = new Vector3(2, 3, 4);

        timestamp = 1;
    }

    // Dummy for testing
    public TrackingData(float[] values)
    {
        JointValues = values;

        pose = Matrix4x4.identity;

        velocity = new Vector3(1, 1, 1);
        acceleration = new Vector3(2, 3, 4);

        timestamp = 1;
    }

    public TrackingData(float[] JointValues, Matrix4x4 pose, Vector3 velocity, Vector3 acceleration, double timestamp)
    {
        this.JointValues = JointValues;
        this.pose = pose;
        this.velocity = velocity;
        this.acceleration = acceleration;
        this.timestamp = timestamp;
    }

    public TrackingData Copy()
    {
        return (TrackingData)this.MemberwiseClone();
    }

}