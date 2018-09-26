using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackingData {

    /*
    public Vector3 position;
    public Quaternion rotation;
    public double quality;
    public TrackedObject.ButtonState buttonPress;
    public double timestamp;
    */

    public float[] JointValues;
    public Matrix4x4 pose;
    public Vector3 velocity;
    public Vector3 acceleration;
    public double timestamp;

    public TrackingData Copy() {
        return (TrackingData)this.MemberwiseClone();
    }

}