using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class umeyama : MonoBehaviour {

    public bool debug = true;

    private static double[,] sourceArray = new double[6, 3] { { 1, 2, 3 }, { 3, 4, 5 }, { 5, 6, 7 }, { 7, 8, 9 }, { 7, 8, 1 }, { 7, 8, 9 } };
    private static double[,] destinationArray = new double[6, 3] { { 2, 3, 4 }, { 3, 4, 5 }, { 5, 6, 7 }, { 7, 8, 9 }, { 7, 8, 1 }, { 7, 8, 9 } };

    private static double[,] test1 = new double[2, 3] { { 0, 1, 0 }, { 0, 0, 2 } };
    private static double[,] test2 = new double[2, 3] { { 0, -1, 0 }, { 0, 0, 2 } };

    private double[,] src = Accord.Math.Matrix.Transpose(sourceArray);
    private double[,] dst = Accord.Math.Matrix.Transpose(destinationArray);

    // Use this for initialization
    void Start() {
        umeyamaFunc(test1, test2);
    }

    // Update is called once per frame
    void Update() {

    }

    public Matrix4x4 umeyamaFunc(double[,] X, double[,] Y) {

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

        /*
        double[] src_mean = new double[m];

        for (int i = 0; i < m; i++)
        {
            double x = 0;
            for (int j = 0; j < n; j++)
                x += src[j, i];

            src_mean[i] = x / n;
        }

        double[] dst_mean = new double[m];

        for (int i = 0; i < m; i++)
        {
            double x = 0;
            for (int j = 0; j < n; j++)
                x += dst[j, i];

            dst_mean[i] = x / n;
        }
        */

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

        for (int i = 0; i < n; i++)
            sigma_x += Accord.Math.Norm.Euclidean(Accord.Math.Matrix.GetColumn(X_demean,i));

        sigma_x /= n;

        double sigma_y = 0;

        for (int i = 0; i < n; i++)
            sigma_y += Accord.Math.Norm.Euclidean(Accord.Math.Matrix.GetColumn(Y_demean, i));

        sigma_y /= n;

        // get Matrix E (Eq. 38)
        double[,] SIGMA = Accord.Math.Matrix.DotWithTransposed(X_demean,Y_demean);

        Debug.Log(SIGMA[0,0]);

        SIGMA = scaleMatrix(SIGMA, (1 / (double)n));        

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

        if (debug)
        {
            Debug.Log("R[0,0] = " + R[0,0]);
            Debug.Log("c = " + c);
            Debug.Log("t(0) = " + t[0]);
        }

        return new Matrix4x4();
    }

    private double[,] scaleMatrix(double[,] matrix, double scale)
    {
        Debug.Log(scale);
        for (int i = 0; i < matrix.GetLength(0); i++)
            for (int j = 0; j < matrix.GetLength(1); j++)
                matrix[i, j] *= scale;

        Debug.Log(matrix[0,0]);
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
}