using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Basic 3x3 Matrix manipulation code. 
/// </summary>
public class Matrix3 
{
    public static Vector3d MatrixTimesVector(ref double[,] M, ref Vector3d v) {
            double x = M[0, 0] * v.x + M[0, 1] * v.y + M[0, 2] * v.z;
            double y = M[1, 0] * v.x + M[1, 1] * v.y + M[1, 2] * v.z;
            double z = M[2, 0] * v.x + M[2, 1] * v.y + M[2, 2] * v.z;
            return new Vector3d(x, y, z);
    }

    public static double Determinant(ref double[,] a) {
       return (a[0, 0] * a[1, 1] * a[2, 2] - a[0, 0] * a[1, 2] * a[2, 1] -
                    a[1, 0] * a[0, 1] * a[2, 2] + a[2, 0] * a[0, 1] * a[1, 2] +
                    a[1, 0] * a[0, 2] * a[2, 1] - a[2, 0] * a[0, 2] * a[1, 1]);
    }

    public static void MatrixInverse(ref double[,] a, ref double[,] Minv) {
        // From Maple
        double D = (a[0, 0] * a[1, 1] * a[2, 2] - a[0, 0] * a[1, 2] * a[2, 1] - 
                    a[1, 0] * a[0, 1] * a[2, 2] + a[2, 0] * a[0, 1] * a[1, 2] + 
                    a[1, 0] * a[0, 2] * a[2, 1] - a[2, 0] * a[0, 2] * a[1, 1]);
        if (Mathd.Abs(D) < 1E-6) {
            Debug.LogWarning("Matrix not invertable");
        }
        Minv[0, 0] = (a[1, 1] * a[2, 2] - a[1, 2] * a[2, 1]) / D;
        Minv[0, 1] = -(a[0, 1] * a[2, 2] - a[0, 2] * a[2, 1]) /D;
        Minv[0, 2] = (a[0, 1] * a[1, 2] - a[0, 2] * a[1, 1]) / D;
        Minv[1, 0] = -(a[1, 0] * a[2, 2] - a[1, 2] * a[2, 0]) /D;
        Minv[1, 1] = (a[0, 0] * a[2, 2] - a[0, 2] * a[2, 0]) / D;
        Minv[1, 2] = -(a[0, 0] * a[1, 2] - a[0, 2] * a[1, 0]) /D;
        Minv[2, 0] = (a[1, 0] * a[2, 1] - a[1, 1] * a[2, 0]) / D;
        Minv[2, 1] = -(a[0, 0] * a[2, 1] - a[0, 1] * a[2, 0]) /D;
        Minv[2, 2] = (a[0, 0] * a[1, 1] - a[0, 1] * a[1, 0]) / D;
    }

    public static void LogMatrix(ref double[,] a, string s) {
        string s2 = string.Format("Matrix: {0}\n", s);
        for (int i = 0; i < 3; i++)
            s2 += string.Format("{0}   {1}   {2}\n", a[i, 0], a[i, 1], a[i, 2]);
        Debug.Log(s2);
    }
}
