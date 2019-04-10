using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectionTest : MonoBehaviour {

    public Vector3 tracker_pos = new Vector3(110, 100, 1);

    public GameObject debugSphere;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        // Projection Test
        // P = K * (R|t) = KR | K * -R * C~

        Vector4 K_column1 = new Vector4(196.92f, 0, 0, 0);
        Vector4 K_column2 = new Vector4(0, 200.66f, 0, 0);
        Vector4 K_column3 = new Vector4(222.29f, 230.41f, 1, 0);
        Vector4 K_column4 = new Vector4(0, 0, 0, 1);

        Matrix4x4 K = new Matrix4x4(K_column1, K_column2, K_column3, K_column4);

        Matrix4x4 R = Matrix4x4.Rotate(this.transform.rotation);

        Vector4 C_Schlange = new Vector4(this.transform.position.x, this.transform.position.y, this.transform.position.z, 1);

        // Calculation

        Matrix4x4 K_R = K * R;

        Vector4 t = K * (R.inverse) * C_Schlange;

        Matrix4x4 P = new Matrix4x4(K_R.GetColumn(0), K_R.GetColumn(1), K_R.GetColumn(2), t);

        Vector4 m = new Vector4(tracker_pos.x, tracker_pos.y, 1, 0);

        Vector4 M = C_Schlange + K_R.inverse * m;

        debugSphere.transform.position = new Vector3(M.x, M.y, 1);


    }
}
