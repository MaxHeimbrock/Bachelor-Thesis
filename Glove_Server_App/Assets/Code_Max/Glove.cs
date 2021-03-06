﻿using AHRS;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Glove : MonoBehaviour
{
    // for imu testing
    private Vector3 acceleration = Vector3.zero;
    private Vector3 velocity;
    private Vector3 gyro_current_rotation = new Vector3(0,0,0);
    private Vector3 filtered_rotation = new Vector3(0, 0, 0);
    private Vector3 filtered_rotation_q = new Vector3(0, 0, 0);
    public float filter = 0.98f;
    public Quaternion q_acc;
    public Quaternion q_gyro;
    public Quaternion q_filtered;
    public Quaternion q_madgwick;
    public Quaternion q_mahony;
    public Quaternion q_madgwick_filtered;
    public Quaternion q_acc_first_pose;

    float G_Gain = 0.07f; // to get degrees per second with 2000dps http://ozzmaker.com/berryimu/
    float Accel_Factor = 16384.0f;

    private Vector3 acceleration_bias;
    private Vector3 gyro_bias;
    private int bias_counter = 0;
    private int bias_length = 100;

    // Calibration with Adnane
    private Vector3 acc_bias_adnane = new Vector3(0.0397429f, -0.0665699f, -0.024349f);
    private Vector3 gyro_bias_adnane = new Vector3(-0.903895f, 0.44357f, -0.429229f);
    private Matrix4x4 gyro_correction_matrix = new Matrix4x4(new Vector4(0.864768f, -0.532357234176f, 0.0513571014144f, 0), new Vector4(0.515507329899f, 0.863067f, -0.0330214612602f, 0), new Vector4(-0.03419220999f, 0.060746958228f, 1.00266f, 0), new Vector4(0, 0, 0, 1));

    private MadgwickAHRS madgwickARHS = new MadgwickAHRS(0.00005f);
    private MahonyAHRS mahonyARHS = new MahonyAHRS(0.00005f);
    bool first_pose = true;

    long time0;
    long time1;
    long elapsedTimeServer;

    long elapsedTimeGlove_microseconds;
    long elapsedTimeGlove_milliseconds = 0;

    long last_timestamp_microseconds;
    float elapsed_seconds_glove = 0;
    
    public int timestamp0 = 0;
    public int timestamp1 = 0;

    public float acc_threshold = 0.005f;
    public float LPF_filter = 0.25f;
    public int LPF_filter_size = 50;
    private Vector3[] filter_array;

    // Interactions
    public float clap_threshold = 1.90f;
    public float clap_before_threshold = 0.40f;

    private bool fist_detection_activated = false;
    public float fist_threshold = 20f;    

    public enum Gesture {Clap, Fist };

    public IMUTest imuTest;

    public UDPSend UDP_Send;

    public Glove()
    {
        acceleration = new Vector3(0, 0, 0);
        velocity = new Vector3(0, 0, 0);

        time0 = 0;
        elapsedTimeServer = 0;
        elapsedTimeGlove_microseconds = 0;
    }    

    public void applyEthernetPacketIMU(Vector3 acceleration1, Vector3 gyroscope, int timestamp)
    {   
        long time1 = DateTime.Now.Ticks;

        if (bias_counter > bias_length)
        {
            // delta t in seconds or miliseconds
            TimeSpan elapsedSpan = new TimeSpan(time1 - time0);
            long delta_t_ms = elapsedSpan.Milliseconds;
            float delta_t_s = delta_t_ms / 1000f;

            float delta_t_s_imu = GetTime(timestamp);

            // TODO: Bias nötig?
            gyroscope = CorrectGyro(gyroscope);
            acceleration1 = CorrectAccel(acceleration1);

            detect_clap(acceleration1);

            acceleration1 = thresholdAcc(acceleration1, acceleration, acc_threshold);
            acceleration1 = lowPassFilter(acceleration1);

            Vector3 angleFromAcc = CalcAngleFromAcc(acceleration1);
            q_acc = Quaternion.Euler(new Vector3(angleFromAcc.y, 0, angleFromAcc.x));
            //Debug.Log(angleFromAcc);

            gyro_current_rotation += IntegrateGyro(gyroscope, delta_t_s_imu);
            q_gyro = Quaternion.Euler(new Vector3(gyro_current_rotation.y, -gyro_current_rotation.z, gyro_current_rotation.x));
            //Debug.Log(gyro_current_rotation);

            FilterRotation(gyroscope, delta_t_s_imu, angleFromAcc, filter);
            q_filtered = Quaternion.Euler(new Vector3(filtered_rotation.y, -filtered_rotation.z, filtered_rotation.x));
            
            // Give initial pose for madgwick and mahony from accelerometer angles --> best if glove is relatively flat on table
            if (first_pose)
            {
                q_acc_first_pose = q_acc;
                first_pose = false;
            }

            madgwickARHS.Update(gyroscope.x, gyroscope.y, gyroscope.z, acceleration1.x, acceleration1.y, acceleration1.z);
            q_madgwick = new Quaternion(madgwickARHS.Quaternion[0], madgwickARHS.Quaternion[1], madgwickARHS.Quaternion[3], -madgwickARHS.Quaternion[2]);
            // zusätzlich noch um 180° zur x-Achse rotiert
            q_madgwick *= Quaternion.AngleAxis(180, Vector3.right);
            q_madgwick *= q_acc_first_pose;

            mahonyARHS.Update(gyroscope.x, gyroscope.y, gyroscope.z, acceleration1.x, acceleration1.y, acceleration1.z);
            q_mahony = new Quaternion(mahonyARHS.Quaternion[0], mahonyARHS.Quaternion[1], mahonyARHS.Quaternion[3], -mahonyARHS.Quaternion[2]);
            // zusätzlich noch um 180° zur x-Achse rotiert
            q_mahony *= Quaternion.AngleAxis(180, Vector3.right);
            // zusätzliche Rotationen für die Hololens
            //q_mahony *= Quaternion.AngleAxis(180, Vector3.up);
            q_mahony *= q_acc_first_pose;

            FilterRotationQuaternion(q_madgwick, delta_t_s_imu, angleFromAcc, filter);
            q_madgwick_filtered = Quaternion.Euler(new Vector3(filtered_rotation_q.y, -filtered_rotation_q.z, filtered_rotation_q.x));
        }
        
        // get bias
        else if (bias_counter == bias_length)
        {
            // Mittelwert berechnen und Gravitation behalten
            acceleration_bias /= bias_length;
            acceleration_bias -= new Vector3(0, 0, -Accel_Factor);
            acceleration_bias /= Accel_Factor;
            gyro_bias /= bias_length;
            gyro_bias *= G_Gain;

            bias_counter++;
            Debug.Log("Gyroscope bias is " + gyro_bias);
            Debug.Log("Acceleration bias is " + acceleration_bias);
        }
        else
        {
            // trying to get a bias for flat on table
            acceleration_bias += acceleration1;
            gyro_bias += gyroscope;

            bias_counter++;
        }      

        timestamp0 = timestamp1;
        time0 = time1;

        acceleration = acceleration1;
    }

    #region IMU calc functions

    // corrects gyroscope measurement with bias and G_Gain
    Vector3 CorrectGyro(Vector3 gyro)
    {
        gyro *= G_Gain;
        gyro -= gyro_bias_adnane;
        Vector4 gyro_temp = new Vector4(gyro.x, gyro.y, gyro.z, 0);
        gyro_temp = gyro_correction_matrix.MultiplyVector(gyro_temp);
        //return new Vector3(gyro_temp.x, gyro_temp.y, gyro_temp.z);
        return gyro;
    }

    // corrects acceleromter measurement with bias and Accel_Factor
    Vector3 CorrectAccel(Vector3 acc)
    {
        acc /= Accel_Factor;
        acc -= acc_bias_adnane;
        return acc;
    }

    // detect Claps: accel in positive z when all past accel values in filter_array show no accel -> clap detected
    public void detect_clap(Vector3 acc)
    {
        if (filter_array != null)
        {
            float filter_array_sum_z = 0;

            for (int i = 0; i < LPF_filter_size; i++)
            {
                filter_array_sum_z += Math.Abs(filter_array[i].z);
            }

            filter_array_sum_z /= LPF_filter_size;

            if (acc.z > clap_threshold && filter_array_sum_z < clap_before_threshold)
            {
                Debug.Log("Clap");

                imuTest.clapDetected();

                //UDP_Send.sendGesture(Gesture.Clap);
            }                
        }
    }

    // If difference between old and new accel data is below t, use old data again
    Vector3 thresholdAcc(Vector3 acc1, Vector3 acc0, float t)
    {
        //Debug.Log(Math.Abs(acc1.x - acc0.x));

        if (Math.Abs(acc1.x - acc0.x) < t)
            acc1.x = acc0.x;
        if (Math.Abs(acc1.y - acc0.y) < t)
            acc1.y = acc0.y;
        if (Math.Abs(acc1.z - acc0.z) < t)
            acc1.z = acc0.z;

        return acc1;
    }

    Vector3 lowPassFilter(Vector3 acc)
    {
        // initialize filter_array
        if (filter_array == null)
        {
            filter_array = new Vector3[LPF_filter_size];
            for (int i = 0; i < LPF_filter_size; i++)
                filter_array[i] = acc;
        }

        acc = thresholdAcc(acc, filter_array[LPF_filter_size - 1], acc_threshold);

        Vector3 sum = Vector3.zero;

        for (int i = 0; i < LPF_filter_size; i++)
        {
            sum += filter_array[i];
        }

        sum /= LPF_filter_size;

        // push new value in filter_array
        for (int i = 0; i < LPF_filter_size - 1; i++)
            filter_array[i] = filter_array[i + 1];

        filter_array[LPF_filter_size - 1] = acc;

        return sum;
    }

    // Local space IMU -> left handed, z up
    Vector3 CalcAngleFromAcc(Vector3 acc)
    {
        // TODO: Hier kommt es zu einem Flip

        float rot_x = 0;
        float rot_y = 0;

        // X-axis - http://ozzmaker.com/berryimu/ // https://stackoverflow.com/questions/3755059/3d-accelerometer-calculate-the-orientation
        rot_x = (float)((Math.Atan2(acc.y, acc.z) + Math.PI) * (180 / Math.PI));
        //rot_x = -(float)(Math.Atan2(acc.y, Math.Sqrt(acc.x * acc.x + acc.z * acc.z)) * (180 / Math.PI));
            

        // diese Rechnung korrigiert Orientierung zu -180 bis 180 grad
        if (rot_x > 180)
            rot_x -= (float)360;

        // Y-axis - http://ozzmaker.com/berryimu/ // https://stackoverflow.com/questions/3755059/3d-accelerometer-calculate-the-orientation
        //rot_y = (float)(Math.Atan2(-acc.x, Math.Sqrt(acc.y * acc.y + acc.z * acc.z)) * (180 / Math.PI));
        rot_y = (float)((Math.Atan2(acc.z, acc.x) + Math.PI * 1/2) * (180 / Math.PI));
        //rot_y = 0;

        // diese Rechnung korrigiert Orientierung zu -180 bis 180 grad
        if (rot_y > 180)
            rot_y -= (float)360;

        return new Vector3(rot_x, rot_y, 0);
    }

    // Local space IMU -> left handed, z up
    Vector3 IntegrateGyro(Vector3 gyro, float delta_t_s)
    {
        //Debug.Log(delta_t_s);
        return (gyro * delta_t_s);
    }

    // Local space IMU -> left handed, z up
    void FilterRotation(Vector3 gyro, float delta_t_s, Vector3 acc_angles, float filter)
    {
        Vector3 integrated_gyro = IntegrateGyro(gyro, delta_t_s);

        // Acc is only trustable till around 45° right now - still a flip happens
        if (acc_angles.x < 20)
            filtered_rotation.x = filter * (filtered_rotation.x + integrated_gyro.x) + (1 - filter) * acc_angles.x;
        else
            filtered_rotation.x = filtered_rotation.x + integrated_gyro.x;
        if (acc_angles.y < 20)
            filtered_rotation.y = filter * (filtered_rotation.y + integrated_gyro.y) + (1 - filter) * acc_angles.y;
        else
            filtered_rotation.y = filtered_rotation.y + integrated_gyro.y;

        filtered_rotation.z = filtered_rotation.z + integrated_gyro.z;
    }

    void FilterRotationQuaternion(Quaternion q_madgwick, float delta_t_s, Vector3 acc_angles, float filter)
    {
        filter = 0.98f;

        // Acc is only trustable till around 45° right now - still a flip happens
        if (acc_angles.x < 20)
            filtered_rotation_q.x = filter * q_madgwick.eulerAngles.x + (1 - filter) * acc_angles.x;
        else
            filtered_rotation_q.x = q_madgwick.eulerAngles.x;
        if (acc_angles.y < 20)
            filtered_rotation_q.y = filter * q_madgwick.eulerAngles.y + (1 - filter) * acc_angles.y;
        else
            filtered_rotation_q.y = q_madgwick.eulerAngles.y;

        filtered_rotation_q.z = q_madgwick.eulerAngles.z;
    }

    #endregion
    
    private long GetTime()
    {
        time1 = DateTime.Now.Ticks;

        TimeSpan elapsedSpan = new TimeSpan(time1 - time0);
        long delta_t_ms = elapsedSpan.Milliseconds;
        float delta_t_s = delta_t_ms / 1000f;

        elapsedTimeServer += delta_t_ms;
        
        //Debug.Log(elapsedTimeServer);

        time0 = time1;

        return 0;
    }

    private float GetTime(int timestamp_in_ticks)
    {

        long this_timestamp_microseconds = timestamp_in_ticks * 39;

        long delta_time_microseconds = 0;

        if (last_timestamp_microseconds != 0)
            delta_time_microseconds = this_timestamp_microseconds - last_timestamp_microseconds;

        float delta_time_seconds = ((float)delta_time_microseconds / (1000 * 1000));

        if (delta_time_seconds < 0)
            delta_time_seconds = 0.0033f;

        elapsed_seconds_glove += delta_time_seconds;

        // this is it - just get delta time
        //Debug.Log(elapsed_seconds_glove);

        last_timestamp_microseconds = this_timestamp_microseconds;

        return delta_time_seconds;
    }
}

static class Constants
{
    public const int NB_SENSORS = 40;
    public const bool IS_BLUETOOTH = false;
    internal static int NB_VALUES_GLOVE;
}
 