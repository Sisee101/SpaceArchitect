namespace SGP4
{
    // runs the Verification TLEs just like Vallado's C++ code does
    //
    // 19 June 2009 Results: only one digit different in the entire file: ( for OPSMODE_IMPROVED, wgs72)
    //line 655 - only diff  x value of position:
    // all error codes and times of errors where also the same
    // value that was different (x pos line 655)
    //cpp  = -23575.69186056
    //java = -23575.69186057
    //
    // other combinations for the opsmode and gravity constants had similar results with very few
    // differences in the cpp vs java output files

    //package sgp4_cssi;

    //import java.io.BufferedReader;
    //import java.io.BufferedWriter;
    //import java.io.DataInputStream;
    //import java.io.FileInputStream;
    //import java.io.FileWriter;
    //import java.io.InputStreamReader;


    using System.IO;
    using UnityEngine;
    using NUnit.Framework;
    using System;

    /**
    *
    * @author Shawn E. Gano, shawn@gano.name
*/
    public class SGP4verification
    {
        // TODO: Could use stacktrace reflection etc. to automate this
        private const string dirPath = "/Users/musgr/Documents/Unity/gravityengine/Assets/GravityEngine/SGP4/Scripts/PortedCode/TestCode/";

        [Test]
        public void Verify()
        {
            // settings
            char opsmode = SGP4utils.OPSMODE_IMPROVED; // OPSMODE_IMPROVED
            SGP4unit.Gravconsttype gravconsttype = SGP4unit.Gravconsttype.wgs72;

            // tle verification file (with extra start, stop, timestep params on line 2)
            string verTLEfile = "sgp4-ver.tle";

            // output results to this file
            string javaResults = "java_sgp4_ver.out";

            // comparison file, cpp results
            string cppResultsFile = "tcppver.out";

            // internal variables -------------------------
            double[] ro = new double[3];
            double[] vo = new double[3];

            // get constants -------------------------------
            double[] gtt = SGP4unit.getgravconst(gravconsttype);//, tumin, mu, radiusearthkm, xke, j2, j3, j4, j3oj2 );
            double tumin = gtt[0];
            double mu = gtt[1];
            double radiusearthkm = gtt[2];
            double xke = gtt[3];
            double j2 = gtt[4];
            double j3 = gtt[5];
            double j4 = gtt[6];
            double j3oj2 = gtt[7];

            Debug.Log("======  PROPOGATING VERIFICATION TLEs ====== ");
            // open the TLE file and propogate each TLE --------------------
            try {
                // Open the file that is the first
                // command line parameter
                // FileInputStream fstream = new FileInputStream(verTLEfile);
                StreamReader inFile = new StreamReader(dirPath + verTLEfile);
                // Get the object of DataInputStream
                // DataInputStream in = new DataInputStream(fstream);
                // BufferedReader br = new BufferedReader(new InputStreamReader(in));

                // output file
                // Create file
                // FileWriter outStream = new FileWriter(javaResults);
                StreamWriter outFile = new StreamWriter(dirPath + javaResults);

                string strLine1;
                string strLine2;

                int recordNum = 0;

                //Read File Line By Line
                while ((strLine1 = inFile.ReadLine()) != null) {
                    if (!strLine1.StartsWith("#")) // ignore lines starting with #
                    {
                        // there should always be a second line
                        strLine2 = inFile.ReadLine();

                        // retrive parts of the line that contain start/stop/timestep info
                        // split on white space after normal line 2 info
                        string[] sst = strLine2.Substring(69).Trim().Split(new string[] { " " }, System.StringSplitOptions.RemoveEmptyEntries); // was "\\s +"

                        double startmfe = double.Parse(sst[0]);
                        double stopmfe = double.Parse(sst[1]);
                        double deltamin = double.Parse(sst[2]);

                        // convert the char string to sgp4 elements
                        // includes initialization of sgp4
                        SGP4SatData satrec = new SGP4SatData();
                        SGP4utils.readTLEandIniSGP4("", strLine1, strLine2, opsmode, gravconsttype, satrec);

                        outFile.Write(satrec.satnum + " xx\n");
                        // Debug.Log(" "+ satrec.satnum);
                        // call the propagator to get the initial state vector value
                        SGP4unit.sgp4(satrec, 0.0, ro, vo);

                        outFile.Write(string.Format(" {0:#######0.00000000} {1:#######0.00000000} {2:#######0.00000000} {3:#######0.00000000} {4:##0.000000000} {5:##0.000000000} {6:##0.000000000}\n",
                            satrec.t, ro[0], ro[1], ro[2], vo[0], vo[1], vo[2]));

                        double tsince = startmfe;

                        // check so the first value isn't written twice
                        if (System.Math.Abs(tsince) > 1.0e-8) {
                            tsince = tsince - deltamin;
                        }

                        // ----------------- loop to perform the propagation ----------------
                        while ((tsince < stopmfe) && (satrec.error == 0)) {
                            tsince = tsince + deltamin;

                            if (tsince > stopmfe) {
                                tsince = stopmfe;
                            }

                            SGP4unit.sgp4(satrec, tsince, ro, vo);

                            if (satrec.error > 0) {
                                Debug.Log("recordNum=" + recordNum + "  # *** error: t:= " + satrec.t + " *** code = " + satrec.ErrorString(satrec.error) + "\n");
                            }

                            if (satrec.error == 0) {

                                //double jd = satrec.jdsatepoch + tsince/1440.0;
                                //invjday( jd, year,mon,day,hr,min, sec );

                                // was " %16.8f %16.8f %16.8f %16.8f %12.9f %12.9f %12.9f"
                                outFile.Write(string.Format(" {0:#######0.00000000} {1:#######0.00000000} {2:#######0.00000000} {3:#######0.00000000} {4:##0.000000000} {5:##0.000000000} {6:##0.000000000}",
                                        tsince, ro[0], ro[1], ro[2], vo[0], vo[1], vo[2]));

                                double jd = satrec.jdsatepoch + tsince / 1440.0;
                                double[] ymd = SGP4utils.InvJday(jd); //, year,mon,day,hr,min, sec );
                                int year = (int)ymd[0];
                                int mon = (int)ymd[1];
                                int day = (int)ymd[2];
                                int hr = (int)ymd[3];
                                int min = (int)ymd[4];
                                double sec = ymd[5];

                                double[] coe = SGP4utils.rv2coe(ro, vo, mu); // , p, a, ecc, incl, node, argp, nu, m, arglat, truelon, lonper);
                                double p = coe[0];
                                double a = coe[1];
                                double ecc = coe[2];
                                double incl = coe[3];
                                double node = coe[4];
                                double argp = coe[5];
                                double nu = coe[6];
                                double m = coe[7];
                                double arglat = coe[8];
                                double truelon = coe[9];
                                double lonper = coe[10];

                                double rad = 180.0 / System.Math.PI;

                                // was string.format(" %14.6f %8.6f %10.5f %10.5f %10.5f %10.5f %10.5f %5d%3d%3d %2d:%2d:%9.6f\n"

                                outFile.Write(string.Format(" {0:########.000000} {1:##.000000} {2:#####.00000} {3:#####.00000} {4:#####.00000} {5:#####.00000} {6:#####.00000}  {7:####0} {8:##0} {9:##0} {10:#0}:{11:#0}:{12:###.000000}\n",
                                        a, ecc, incl * rad, node * rad, argp * rad, nu * rad,
                                        m * rad, year, mon, day, hr, min, sec));
                                outFile.Flush(); // make sure the write is caught up

                            } // if satrec.error == 0

                            recordNum++;

                        } // while propagating the orbit

                    } // ignore lines starting with #
                }
                //Close the input stream
                inFile.Close();
                outFile.Close();
            }
            catch (System.Exception e) {//Catch exception if any
                Debug.LogError("Error: " + e.ToString());
                Assert.Fail(e.ToString());
            }
            // -- end main prop loop -------------------------------------------


            Debug.Log("======  RUNNNING COMPARISON ====== ");
            // now compare ------------------------------------------------
            try {
                // Open the results files
                //FileInputStream fstream = new FileInputStream(javaResults);
                //DataInputStream in = new DataInputStream(fstream);
                //BufferedReader javaResultsBR = new BufferedReader(new InputStreamReader(in));
                StreamReader javaResultsBR = new StreamReader(dirPath + javaResults);

                //FileInputStream fstream2 = new FileInputStream(cppResultsFile);
                //DataInputStream in2 = new DataInputStream(fstream2);
                //BufferedReader cppResultsBR = new BufferedReader(new InputStreamReader(in2));
                StreamReader cppResultsBR = new StreamReader(dirPath + cppResultsFile);

                string cppLine = javaResultsBR.ReadLine();
                string javaLine = cppResultsBR.ReadLine();

                int line = 1;
                int lineMismatches = 0;
                do {
                    // PM - do somthing a bit less twitchy about formatting
                    string[] cppValues = cppLine.Split(new string[] { " ", ":" }, System.StringSplitOptions.RemoveEmptyEntries);
                    string[] unityValues = javaLine.Split(new string[] { " ", ":" }, System.StringSplitOptions.RemoveEmptyEntries);

                    if (cppValues.Length != unityValues.Length) {
                        string err = string.Format("line {0} number of values does not match\ncpp={1}\nuni={2}", line, cppLine, javaLine);
                        Debug.LogError(err);
                        Assert.Fail(err);
                    }
                    bool ok = true;
                    for (int i = 0; (i < cppValues.Length) && ok; i++) {
                        try {
                            double cpp = double.Parse(cppValues[i]);
                            double unity = double.Parse(unityValues[i]);
                            if (System.Math.Abs(cpp - unity) > 1E-2) {
                                ok = false;
                                lineMismatches++;
                                Debug.LogError(string.Format("Mismatch on line {0}: {1} vs {2}", line, cpp, unity));
                            }
                        }
                        catch (FormatException f) {
                            if (!cppValues[i].Equals(unityValues[i])) {
                                string err = string.Format("Failed on line {0}: parsing {1} or {2}\n{3}", 
                                    line, cppValues[i], unityValues[i], f.ToString());
                                Debug.LogError(err);
                                Assert.Fail(err);
                            }

                        }
                    }

                    //if( !cppLine.Equals(javaLine))
                    //{
                    //    lineMismatches++;


                    //    // figure out how many chars are different
                    //    int charMismatch = 0;
                    //    StringBuilder errInfo = new StringBuilder();
                    //    errInfo.Append("pos=[");
                    //    try
                    //    {
                    //        for (int i = 0; i < cppLine.Length; i++)
                    //        {
                    //            if (cppLine[i] != javaLine[i])
                    //            {
                    //                charMismatch++;
                    //                errInfo.Append((i+1) + " ");
                    //            }
                    //        }

                    //    } catch (System.Exception e)
                    //    {
                    //        errInfo.Append("(Error checking line details) ");
                    //    }
                    //    errInfo.Append("] ");

                    //    //
                    //    errInfo.Append(charMismatch + " of " +cppLine.Length +  " mismatched characters\n");
                    //    // line not equal
                    //    Debug.LogError("Line " + line + " doesn't match:  \n cpp=" + cppLine + "\ntest" + javaLine + "\n" +
                    //        errInfo.ToString() );
                    //}

                    cppLine = javaResultsBR.ReadLine();
                    javaLine = cppResultsBR.ReadLine();
                    line++;
                } while (cppLine != null && javaLine != null);

                Debug.Log("---------------------");

                if (cppLine == null && javaLine == null) {
                    Debug.Log("** Files have the same number of lines **");
                } else {
                    Debug.Log("** Files have DIFFERENT number of lines **");
                }

                Debug.Log("Total lines that don't match: " + lineMismatches);
                Assert.IsTrue(lineMismatches == 0, string.Format("There are {0} mismatches", lineMismatches));
                Debug.Log("Total number of lines in shortest file: " + (line - 1));

                javaResultsBR.Close();
                cppResultsBR.Close();

            }
            catch (System.Exception e) {
                Debug.Log("Error in comparing verification results:\n" + e.ToString());
            }

        }
    }
}
