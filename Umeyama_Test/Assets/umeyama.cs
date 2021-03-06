﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class umeyama : MonoBehaviour {

    public bool debug = true;

    private static double[,] sourceArray = new double[6, 3] { { 1, 2, 3 }, { 3, 4, 5 }, { 5, 6, 7 }, { 7, 8, 9 }, { 7, 8, 1 }, { 7, 8, 9 } };
    private static double[,] destinationArray = new double[6, 3] { { 2, 3, 4 }, { 3, 4, 5 }, { 5, 6, 7 }, { 7, 8, 9 }, { 7, 8, 1 }, { 7, 8, 9 } };

    private static double[,] test1 = new double[3, 2] { { 0, 0 },{1, 0},{0, 2 } };

    private static double[,] test2 = new double[3, 2] { { 0, 0 }, { -1, 0 }, { 0, 2 } };

    private static double[,] test3 = new double[3, 3] { { 0.57215, 0.37512, 0.37551 }, { 0.23318, 0.86846, 0.98642 }, { 0.79969, 0.96778, 0.27493 } };

    private static double[,] test4 = new double[3, 3] { { 0.57215, 0.37512, 0.37551 }, { 0.23318, 0.86846, 0.98642 }, { 10.79969, 10.96778, 10.27493 } };

    // Use this for initialization
    void Start() {
        double[,] src = Accord.Math.Matrix.Transpose(test3);
        double[,] dst = Accord.Math.Matrix.Transpose(test4);        

        umeyamaFunc(test1, test2);
    }

    // Update is called once per frame
    void Update() {

    }

    public Matrix4x4 umeyamaFunc(double[,] src, double[,] dst) {

        // Das Format muss geändert werden
        double[,] X = Accord.Math.Matrix.Transpose(src);
        double[,] Y = Accord.Math.Matrix.Transpose(dst);

        int m = X.GetLength(0); // dimension
        int n = X.GetLength(1); // number of measurements

        Debug.Log("dimension = " + m);
        Debug.Log("number of points = " + n);

        // computation of mean (Eq. 34/35)
        double[] my_x = new double[m];
        for (int i = 0; i < m; i++)
            my_x[i] = Accord.Math.Matrix.Sum(Accord.Math.Matrix.GetRow(X, i)) / (double)n;

        double[] my_y = new double[m];
        for (int i = 0; i < m; i++)
            my_y[i] = Accord.Math.Matrix.Sum(Accord.Math.Matrix.GetRow(Y, i)) / (double)n;
        
        // for easier computation
        double[,] X_demean = new double[m, n];

        for (int i = 0; i < m; i++)
            for (int j = 0; j < n; j++)
                X_demean[i, j] = X[i, j] - my_x[i];

        double[,] Y_demean = new double[m, n];

        for (int i = 0; i < m; i++)
            for (int j = 0; j < n; j++)
                Y_demean[i, j] = Y[i, j] - my_y[i];

        // getting simgas (Eq. 36/37)
        double sigma_x = 0;

        for (int i = 0; i < m; i++)
            for (int j = 0; j < n; j++)
                sigma_x += Math.Pow(X_demean[i,j], 2);

        sigma_x /= n;
        
        double sigma_y = 0;

        for (int i = 0; i < n; i++)
            sigma_y += Accord.Math.Norm.Euclidean(Accord.Math.Matrix.GetColumn(Y_demean, i));

        sigma_y /= n;

        // get Matrix E (Eq. 38)
        double[,] SIGMA = Accord.Math.Matrix.DotWithTransposed(Y_demean, X_demean);        
        SIGMA = scaleMatrix(SIGMA, (1 / (double)n));

        // Weil ich die Matrix anscheinend im falschen Format verwalte
        SIGMA = Accord.Math.Matrix.Transpose(SIGMA);        

        int r = Accord.Math.Matrix.Rank(SIGMA);

        Accord.Math.Decompositions.SingularValueDecomposition svd = new Accord.Math.Decompositions.SingularValueDecomposition(SIGMA);

        double[,] U = svd.LeftSingularVectors;
        double[,] D = svd.DiagonalMatrix;
        double[,] V = svd.RightSingularVectors;
        
        // construct Matrix S (Eq. 39/40)
        double[,] S = Accord.Math.Matrix.Identity(m);

        if (r > (m - 1))
        {
            if (Accord.Math.Matrix.Determinant(SIGMA) < 0)
                S[m - 1, m - 1] = -1;
            else if (r == (m - 1))
                if (Accord.Math.Matrix.Determinant(U) * Accord.Math.Matrix.Determinant(svd.RightSingularVectors) < 0)
                    S[m - 1, m - 1] = -1;
            else
            {
                // Hier kommt müll rein
            }
        }        
        
        double[,] R = Accord.Math.Matrix.TransposeAndDot(Accord.Math.Matrix.Dot(U, S), V);
        double c = Accord.Math.Matrix.Trace(Accord.Math.Matrix.Dot(D, S)) * (1/sigma_x);
        double[] t = subtractVectors(my_y, scaleVector(Accord.Math.Matrix.Dot(R, my_x), c));

        // für 2D und 3D Lösungen
        if (debug && r == 3)
        {
            Debug.Log("R:");
            printMatrix3x3(R);
            Debug.Log("c = " + c);
            Debug.Log("t = \t" + t[0] +"\n\t" + t[1] + "\n\t" + t[2]);
        }
        
        else if (debug && r == 2)
        {
            Debug.Log("R:");
            printMatrix2x2(R);
            Debug.Log("c = " + c);
            Debug.Log("t = \t" + t[0] + "\n\t" + t[1]);
        }

        return new Matrix4x4();
    }

    /////////////////////////////////////////////////////////////
    /////////// HILFSFUNKTIONEN /////////////////////////////////
    /////////////////////////////////////////////////////////////

    private double[,] scaleMatrix(double[,] matrix, double scale)
    {
        for (int i = 0; i < matrix.GetLength(0); i++)
            for (int j = 0; j < matrix.GetLength(1); j++)
                matrix[i, j] *= scale;
        
        return matrix;
    }

    private double[] scaleVector(double[] vector, double scale)
    {
        for (int i = 0; i < vector.Length; i++)
            vector[i] *= scale;

        return vector;
    }

    private double[] subtractVectors(double[] a, double[] b)
    {
        double[] result;

        if (a.Length == b.Length)
        {
            result = new double[a.Length];
            for (int i = 0; i < a.Length; i++)
                result[i] = a[i] - b[i];

            return result;
        }

        return new double[1];
    }

    private void printMatrix3x3(double[,] matrix)
    {
        for (int i = 0; i < matrix.GetLength(0); i++)
            Debug.Log(matrix[i,0] + "," + matrix[i, 1] + "," + matrix[i, 2]);

    }

    private void printMatrix2x3(double[,] matrix)
    {
        for (int i = 0; i < matrix.GetLength(0); i++)
            Debug.Log(matrix[i, 0] + "," + matrix[i, 1] + "," + matrix[i, 2]);

    }

    private void printMatrix2x2(double[,] matrix)
    {
        Debug.Log(matrix[0, 0] + ",\t" + matrix[0, 1] +"\n" + matrix[1, 0] + ",\t" + matrix[1, 1]);
    }
}