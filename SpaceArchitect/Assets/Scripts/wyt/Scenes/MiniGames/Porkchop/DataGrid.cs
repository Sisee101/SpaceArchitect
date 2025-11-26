using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A container class to hold data over a range of two integer variables. 
/// </summary>
public class DataGrid 
{

    public double xStart;
    public double xEnd;
    public double yStart;
    public double yEnd;

    // number of intervals from xStart to xEnd (there will be xIntervals + 1 x points)
    public int xIntervals;
    public int yIntervals;

    private double xStep;
    private double yStep;

    private double[,] data; 

    public DataGrid(double xStart, 
                        double xEnd, 
                        int xIntervals,
                        double yStart,
                        double yEnd, 
                        int yIntervals)
    {
        this.xStart = xStart;
        this.xEnd = xEnd;
        this.xIntervals = xIntervals;

        this.yStart = yStart;
        this.yEnd = yEnd;
        this.yIntervals = yIntervals;

        data = new double[xIntervals + 1, yIntervals + 1];

        xStep = (xEnd - xStart) / ((double)xIntervals);
        yStep = (yEnd - yStart) / ((double)yIntervals);

    }

    public void AddData(int xPoint, int yPoint, double value)
    {
        data[xPoint, yPoint] = value;
    }

    public double GetData(int x, int y)
    {
        return data[x, y];
    }

}
