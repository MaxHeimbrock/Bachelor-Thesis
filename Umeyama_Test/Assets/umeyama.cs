using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class umeyama : MonoBehaviour {

    private double[,] sourceArray = new double[6,3] { { 1, 2, 3 }, { 3, 4, 5 }, { 5, 6, 7 }, { 7, 8, 9 }, { 7, 8, 1 }, { 7, 8, 9 } };
    private double[,] destinationArray = new double[6, 3] { { 2, 3, 4 }, { 3, 4, 5 }, { 5, 6, 7 }, { 7, 8, 9 }, { 7, 8, 1 }, { 7, 8, 9 } };


    // Use this for initialization
    void Start () {    
        umeyamaFunc(sourceArray, destinationArray);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public Matrix4x4 umeyamaFunc(double[,] src, double[,] dst)
    {
        /*

        int m = src.GetLength(1); // dimension
        int n = src.GetLength(0); // number of measurements

        double[] src_mean = new double[m];

        for (int i = 0; i < m; i++)
        {
            double x = 0;
            for (int j = 0; j < n; j++)
            {
                x += src[j, i];
            }
            src_mean[i] = x / n;
        }

        double[] dst_mean = new double[m];

        for (int i = 0; i < m; i++)
        {
            double x = 0;
            for (int j = 0; j < n; j++)
            {
                x += dst[j, i];
            }
            dst_mean[i] = x / n;
        }

        double[,] src_demean = new double[n,m];

        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
                src_demean[j, i] = src[j,i] - src_mean[i];
        }

        double[,] dst_demean = new double[n, m];

        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
                dst_demean[j, i] = dst[j, i] - dst_mean[i];
        }
        
    */
        return new Matrix4x4();
    }
}