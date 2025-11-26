
using UnityEngine;
using System;

    /// <summary>
    /// Very basic line renderer. Passed a series of (x,y) points that are drawn
    /// in a line renderer.
    ///
    /// Can do auto-scaling.
    ///
    /// Assumes that it is located in top left corner to simplify the use of crude
    /// OnGUI labels
    /// 
    /// 
    /// </summary>
    public class LinePlotBasic : MonoBehaviour
    {
        public float xRange = 100f;
        public float yRange = 100f;

        public bool autoScaleY = true;

        public string title;
        public string subtitle;
        public string report;


        public LineRenderer axis;
        public LineRenderer line;
        public LineRenderer line2;

        private bool haveData;

        // Start is called before the first frame update
        void Start()
        {
            Vector3[] axisPoints = new Vector3[3];
            axisPoints[0] = new Vector3(0, yRange, 0);
            axisPoints[1] = new Vector3(0, 0, 0);
            axisPoints[2] = new Vector3(xRange, 0, 0);
            axis.positionCount = 3;
            axis.SetPositions(axisPoints);
        }

        /// <summary>
        /// Plot the data provided. A second curve can be optionally provided
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        public void PlotData(double[] x, double[] y, double[] x2 = null, double[] y2 = null)
        {

            haveData = true;
            // find min max of x and y for scaling
            double minX = double.MaxValue;
            double minY = double.MaxValue;

            double maxX = double.MinValue;
            double maxY = double.MinValue;


            for (int i = 0; i < x.Length; i++) {
                if (double.IsInfinity(x[i]) || double.IsNaN(x[i]))
                    continue;
                if (double.IsInfinity(y[i]) || double.IsNaN(y[i]))
                    continue;
                minY = Math.Min(minY, y[i]);
                minX = Math.Min(minX, x[i]);
                maxY = Math.Max(maxY, y[i]);
                maxX = Math.Max(maxX, x[i]);
            }
            // second line (if present)
            if (x2 != null) {
                for (int i = 0; i < x2.Length; i++) {
                    if (double.IsInfinity(x2[i]) || double.IsNaN(x2[i]))
                        continue;
                    if (double.IsInfinity(y2[i]) || double.IsNaN(y2[i]))
                        continue;
                    minY = Math.Min(minY, y2[i]);
                    minX = Math.Min(minX, x2[i]);
                    maxY = Math.Max(maxY, y2[i]);
                    maxX = Math.Max(maxX, x2[i]);
                }
            }
            Debug.LogFormat("Plot range x: {0} => {1}  y: {2} => {3}", minX, maxX, minY, maxY);
            // extend the max/min a bit so plot fits inside axis without touching them
            double deltaX = (maxX - minX);
            double rangeBuffer = 0.05;
            minX -= rangeBuffer * deltaX;
            maxX += rangeBuffer * deltaX;

            double deltaY = (maxY - minY);
            minY -= rangeBuffer * deltaY;
            maxY += rangeBuffer * deltaY;

            double scaleX = xRange / (maxX - minX);
            double scaleY = yRange / (maxY - minY);

            Vector3[] points = new Vector3[x.Length];
            int realPoints = 0;
            for (int i = 0; i < x.Length; i++) {
                if (double.IsInfinity(x[i]) || double.IsNaN(x[i]))
                    continue;
                if (double.IsInfinity(y[i]) || double.IsNaN(y[i]))
                    continue;
                double xpoint = (x[i] - minX) * scaleX;
                double ypoint = (y[i] - minY) * scaleY;
                points[realPoints++] = new Vector3((float)xpoint, (float)ypoint, 0);
            }
            line.positionCount = realPoints;
            line.SetPositions(points);
            // Second line (if present)
            if (x2 != null) {
                realPoints = 0;
                points = new Vector3[x2.Length];
                for (int i = 0; i < x2.Length; i++) {
                    if (double.IsInfinity(x2[i]) || double.IsNaN(x2[i]))
                        continue;
                    if (double.IsInfinity(y2[i]) || double.IsNaN(y2[i]))
                        continue;
                    double xpoint = (x2[i] - minX) * scaleX;
                    double ypoint = (y2[i] - minY) * scaleY;
                    points[realPoints++] = new Vector3((float)xpoint, (float)ypoint, 0);
                }
                line2.positionCount = realPoints;
                line2.SetPositions(points);
            }
        }

        void OnGUI()
        {
            if (haveData) {
                GUI.Label(new Rect((float)(xRange * 0.5), 20, 200, 40), title);
                GUI.Label(new Rect((float)(xRange * 0.5), 50, 200, 100), subtitle);
                GUI.Label(new Rect((float)(xRange * 0.8), 30, 300, 500), report);
            }
        }
    }
