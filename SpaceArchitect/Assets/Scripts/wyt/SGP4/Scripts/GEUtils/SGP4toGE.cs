using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SGP4;
using System.Linq;

public class SGP4toGE 
{

    private SGP4SatData sgp4Data;


    public SGP4toGE(string name, string line1, string line2)
    {
        // new sat data object
        sgp4Data = new SGP4SatData();
        // options
        char opsmode = SGP4utils.OPSMODE_IMPROVED; // OPSMODE_IMPROVED
        SGP4unit.Gravconsttype gravconsttype = SGP4unit.Gravconsttype.wgs72;

        // read in data and ini SGP4 data
        bool result1 = SGP4utils.readTLEandIniSGP4(name, line1, line2, opsmode, gravconsttype, sgp4Data);
        //Debug.Log("TLE " +sgp4Data.LogString());

        if (!result1) {
            Debug.Log("Error Reading / Ini Data, error code: " + sgp4Data.error);
        }
    }

    // Copy constructor for cloning orbits
    public SGP4toGE(SGP4toGE copyFrom)
    {
        sgp4Data = new SGP4SatData(copyFrom.sgp4Data);
    }

    public SGP4SatData GetSatData()
    {
        return sgp4Data;
    }


    /// <summary>
    /// GE routine to intialize a satellite record (satrec) using orbit information in an OrbitUniversal. 
    /// 
    /// The SGP code that was ported expects intital conditions from a TLE. This is a copy of that routine, with the TLE parsing code stripped out.
    /// </summary>
    /// <param name="satName"></param>
    /// <param name="orbitU"></param>
    /// <param name="opsmode"></param>
    /// <param name="whichconst"></param>
    /// <param name="satrec"></param>
    /// <returns></returns>
    public SGP4toGE(string satName, OrbitUniversal orbitU)
    {
        GravityEngine ge = GravityEngine.Instance();
        //const double xpdotp = 1440.0 / (2.0 * Mathd.PI);  // 229.1831180523293
        char opsmode = SGP4utils.OPSMODE_IMPROVED; // OPSMODE_IMPROVED

        //double sec;

        //int year = 0;
        //int mon, day, hr, minute;//, nexp, ibexp;

        // new sat data object
        sgp4Data = new SGP4SatData();

        sgp4Data.error = 0;

        sgp4Data.name = satName;

        sgp4Data.classification = "X";
        sgp4Data.satnum = 12345;

        // SEG -- save gravity  - moved to SGP4unit.sgp4init for consistancy
        //satrec.gravconsttype = whichconst;

        // get variables from the two lines
        sgp4Data.line1 = "fromOrbitUniveral";
        sgp4Data.line2 = "fromOrbitUniveral";

        // ---- find no, ndot, nddot ----
        // GE: no from TLE is mean motion in revs/day
        // sgp4Data.no = sgp4Data.no / xpdotp; //* rad/min
        //sgp4Data.nddot = sgp4Data.nddot * System.Math.Pow(10.0, sgp4Data.nexp);
        sgp4Data.bstar = orbitU.sgp4_bstar;


        // do the other way (derive no from a)
        //satrec.a = System.Math.Pow(satrec.no * tumin, (-2.0 / 3.0));
        double[] temp = SGP4unit.getgravconst(SGP4unit.Gravconsttype.wgs72);
        double tumin = temp[0];
        double radiusearthkm = temp[2];

        // ---- convert to sgp4 units ----
        double orbitU_a = (1.0/ge.lengthScale) *  orbitU.GetMajorAxis(); // SI -> km
        sgp4Data.a = orbitU_a/radiusearthkm; // SGP4 used a as a fraction of Earth radius
        tumin = temp[0];
        sgp4Data.no = 1 / (System.Math.Pow(sgp4Data.a, 3.0 / 2.0) * tumin);

        //sgp4Data.ndot = orbitU.sgp4_ndot;
        //sgp4Data.nddot = orbitU.sgp4_nddot;

        // ---- find standard orbital elements ----
        sgp4Data.inclo = orbitU.inclination * Mathd.Deg2Rad;
        sgp4Data.nodeo = orbitU.omega_uc * Mathd.Deg2Rad;
        sgp4Data.argpo = orbitU.omega_lc * Mathd.Deg2Rad;
        sgp4Data.ecco = orbitU.eccentricity;

        double Eanom = OrbitUtils.ConvertTrueAnomolytoE(orbitU.phase * Mathd.Deg2Rad, orbitU.eccentricity);
        sgp4Data.mo = OrbitUtils.ConvertEtoMeanAnomoly(Eanom, orbitU.eccentricity);

        //sgp4Data.mo = orbitU.phase * Mathd.Deg2Rad;

        sgp4Data.alta = sgp4Data.a * (1.0 + sgp4Data.ecco) - 1.0;
        sgp4Data.altp = sgp4Data.a * (1.0 - sgp4Data.ecco) - 1.0;

        // ----------------------------------------------------------------
        // find sgp4epoch time of element set
        // remember that sgp4 uses units of days from 0 jan 1950 (sgp4epoch)
        // and minutes from the epoch (time)
        // ----------------------------------------------------------------

        // ---------------- temp fix for years from 1957-2056 -------------------
        // --------- correct fix will occur when year is 4-digit in tle ---------
        //if (sgp4Data.epochyr < 57) {
        //    year = sgp4Data.epochyr + 2000;
        //} else {
        //    year = sgp4Data.epochyr + 1900;
        //}

        // computes the m/d/hr/min/sec from year and epoch days
        //SGP4utils.MDHMS mdhms = SGP4utils.Days2mdhms(year, sgp4Data.epochdays);
        //mon = mdhms.mon;
        //day = mdhms.day;
        //hr = mdhms.hr;
        //minute = mdhms.minute;
        //sec = mdhms.sec;


        // NB: record y/m/d
        //sgp4Data.year = ge.startTimeYear;
        //sgp4Data.month = ge.startTimeMonth;
        //sgp4Data.day = ge.startTimeDay;

        sgp4Data.jdsatepoch = ge.GetTimeAsJulianDate(orbitU.GetStartTime());

        // ---------------- initialize the orbit at sgp4epoch -------------------
        bool result = SGP4unit.sgp4init(SGP4unit.Gravconsttype.wgs72, opsmode, sgp4Data.satnum,
                sgp4Data.jdsatepoch - 2433281.5, sgp4Data.bstar,
                sgp4Data.ecco, sgp4Data.argpo, sgp4Data.inclo, sgp4Data.mo, sgp4Data.no,
                sgp4Data.nodeo, sgp4Data);

        if (!result) {
            Debug.LogErrorFormat("Error in sgp4init for {0} error={1} ({2}) a={3} e={4} i={5}", 
                orbitU.gameObject.name, sgp4Data.error, SGP4unit.Sgp4Error(sgp4Data.error),
                sgp4Data.argpo, sgp4Data.ecco, sgp4Data.inclo);
        }
    }

    /// <summary>
    /// Reinitialize an existing satellite data record with a new r, v. Used mainly when a
    /// satrec initialed via TLE undergoes an impulse change or a maneuver. 
    /// </summary>
    /// <param name="r"></param>
    /// <param name="v"></param>
    /// 

    // Only change V by this amount during position LERP in OrbitUniversal
    private const double V_PRECISION = 0.001; 
    public double ReInitFromRV(Vector3d r, Vector3d v, double t0, double mu)
    {
        GravityEngine ge = GravityEngine.Instance();
        //const double xpdotp = 1440.0 / (2.0 * Mathd.PI);  // 229.1831180523293
        char opsmode = SGP4utils.OPSMODE_IMPROVED; // OPSMODE_IMPROVED


        //int year = 0;
        //int mon, day, hr, minute;//, nexp, ibexp;

        // new sat data object
        // sgp4Data = new SGP4SatData();

        sgp4Data.error = 0;

        // name stays the same

        // SEG -- save gravity  - moved to SGP4unit.sgp4init for consistancy
        //satrec.gravconsttype = whichconst;

        // get variables from the two lines
        sgp4Data.line1 = "ReinitFromRV";
        sgp4Data.line2 = "ReinitFromRV";

        OrbitUtils.OrbitElements oe = OrbitUtils.RVtoCOE(r, v, null, mu, relativePos:true, ecc_threshold: 1E-9);

        // ---- find no, ndot, nddot ----
        // GE: no from TLE is mean motion in revs/day
        // sgp4Data.no = sgp4Data.no / xpdotp; //* rad/min
        // sgp4Data.nddot = sgp4Data.nddot * System.Math.Pow(10.0, sgp4Data.nexp);

        // bstar stays the same
        // sgp4Data.bstar = orbitU.sgp4_bstar;
        if (oe.typeOrbit == OrbitUtils.OrbitElements.TypeOrbit.CIRCULAR_EQUATORIAL) {
            oe.raan = 0.0;
            oe.argp = 0.0;
            // phase is truelon
            oe.nu = oe.truelon;
        } else if (oe.typeOrbit == OrbitUtils.OrbitElements.TypeOrbit.ELLIPTICAL_EQUATORIAL) {
            oe.raan = 0.0;
            oe.argp = oe.lonper;
        } else if (oe.typeOrbit == OrbitUtils.OrbitElements.TypeOrbit.CIRCULAR_INCLINED) {
            oe.argp = 0.0;
            oe.nu = oe.arglat;
        }
#pragma warning disable 162     // disable unreachable code warning
        if (GravityEngine.DEBUG) {
            Debug.LogFormat("Update SGP4 FROM jdsepoch={0} a={1} ecco={2} argpo={3} inclo={4} mo={5} no={6} nodeo={7}\nr={8} v={9}",
                sgp4Data.jdsatepoch - 2433281.5,
                sgp4Data.a, sgp4Data.ecco, sgp4Data.argpo, sgp4Data.inclo, sgp4Data.mo, sgp4Data.no,
                sgp4Data.nodeo, r, v);
            Debug.LogFormat("  COE={0}", oe.ToString());
        }
#pragma warning restore 162        // apply an impulse to the indicated NBody       //double sec;

        // do the other way (derive no from a)
        //satrec.a = System.Math.Pow(satrec.no * tumin, (-2.0 / 3.0));
        double[] temp = SGP4unit.getgravconst(sgp4Data.gravconsttype);
        double tumin = temp[0];
        double radiusearthkm = temp[2];

        // ---- convert to sgp4 units ----
        double orbitU_a = (1.0 / ge.lengthScale) * oe.a; // SI -> km
        sgp4Data.a = orbitU_a / radiusearthkm; // SGP4 used a as a fraction of Earth radius
        tumin = temp[0];
        double no = 1 / (System.Math.Pow(sgp4Data.a, 3.0 / 2.0) * tumin);

        // This seemed like a good idea based on the paper but does not improve t=0 accuracy.
        //double n0Test = SGP4unit.BrowerMeanToTLEMean(sgp4Data, oe.a, oe.incl, oe.ecc);
        //Debug.LogWarningFormat("Brower no={0} vs {1}", n0Test, no);
        //no = n0Test;

        //sgp4Data.ndot = orbitU.sgp4_ndot;
        //sgp4Data.nddot = orbitU.sgp4_nddot;


        double eanom = OrbitUtils.ConvertTrueAnomolytoE(oe.nu, oe.ecc);
        double mo = OrbitUtils.ConvertEtoMeanAnomoly(eanom, oe.ecc) % (2.0 * System.Math.PI);

        sgp4Data.jdsatepoch = ge.GetTimeAsJulianDate(t0);

        // ---------------- initialize the orbit at sgp4epoch -------------------
        bool result = SGP4unit.sgp4init(SGP4unit.Gravconsttype.wgs72, opsmode, sgp4Data.satnum,
                sgp4Data.jdsatepoch - 2433281.5, sgp4Data.bstar,
                oe.ecc, oe.argp, oe.incl, mo, no, oe.raan, sgp4Data);

#pragma warning disable 162     // disable unreachable code warning
        if (GravityEngine.DEBUG) {
            Debug.LogFormat("Update SGP4 TO jdsepoch={0} a={1} km ecco={2} argpo={3} inclo={4} mo={5} no={6} nodeo={7}",
                sgp4Data.jdsatepoch - 2433281.5,
                sgp4Data.a * radiusearthkm, sgp4Data.ecco, sgp4Data.argpo, sgp4Data.inclo, sgp4Data.mo, sgp4Data.no,
                sgp4Data.nodeo);
        }
#pragma warning restore 162        // apply an impulse to the indicated NBody
        // Consistency check. Evolve this for zero and see what r, v we get back 
        (int err_test, Vector3d r_test, Vector3d v_test) = SGP4toRVatTime(sgp4Data.jdsatepoch);
        if (ge.xzOrbits) {
            r_test = XZPlane.PhysicsToUnity(r_test);
            v_test = XZPlane.PhysicsToUnity(v_test);
        }
        double blendTime = Vector3d.Distance(r_test, r) / (V_PRECISION * v.magnitude);
#pragma warning disable 162     // disable unreachable code warning
        if (GravityEngine.DEBUG) {
            Debug.LogFormat("Test err={0} dR={1} dV={2} blendTime={3}", 
                err_test, Vector3d.Distance(r_test, r), Vector3d.Distance(v_test, v), blendTime);
        }      
#pragma warning restore 162        // apply an impulse to the indicated NBody

        if (!result) {
            Debug.LogErrorFormat("Error in sgp4init reinit. err_test={0}", err_test );
        }
        return blendTime; 
    }

    /// <summary>
    /// Cache some data in the OU for the case where a user begins with a TLE description and then decides to customize
    /// by switching the input type to something else. Better UX, worse code (sigh)
    /// </summary>
    /// <param name="orbitU"></param>
    public void UpdateSGP4AuxData(OrbitUniversal orbitU)
    {
        orbitU.sgp4_bstar = sgp4Data.bstar;
        orbitU.pkepler_ndot = sgp4Data.ndot;
        orbitU.pkepler_nddot = sgp4Data.nddot;
    }


    /// <summary>
    /// Propagate the satellite to the Julian date provided and return the r,v in the current GE internal units.
    /// </summary>
    /// <param name="propJD"></param>
    /// <returns>errorCode</returns>

    public const int PROP_ERROR_DECAY = 2;
    public const int PROP_ERROR_NEGTIME = 3;

    public static string SatPropErrorMsg(int err)
    {
        string s = "unknown code";
        switch(err) {
            case PROP_ERROR_DECAY:
                s = "Satellite has decayed";
                break;

            case PROP_ERROR_NEGTIME:
                s = "Evolve time before satellite epoch time. Is GE start time too early?";
                break;
        }
        return s;
    }

    /// <summary>
    /// Evolve using SGP4. 
    /// 
    /// SGP errors are passed through
    ///         //*                   1 - mean elements, ecc >= 1.0 or ecc< -0.001 or a < 0.95 er
    //*                   2 - mean motion less than 0.0
    //*                   3 - pert elements, ecc< 0.0  or  ecc> 1.0
    //*                   4 - semi-latus rectum < 0.0
    //*                   5 - epoch elements are sub-orbital
    //*                   6 - satellite has decayed
    //                    7 - bad TLE data
    //              GE Error code
    //                    8 - time to evolve to is before Satellite epoch
    /// </summary>
    /// <param name="propJD"></param>
    /// <returns></returns>
    public (int, Vector3d, Vector3d) SGP4toRVatTime(double propJD)
    {
        Vector3d r = Vector3d.zero;
        Vector3d v = Vector3d.zero;
        int error = 0;

        double minutesSinceEpoch = (propJD - sgp4Data.jdsatepoch) * 24.0 * 60.0;
        if (minutesSinceEpoch < 0) {
            Debug.LogError("Error in Sat Prop. result=" + sgp4Data.ErrorString(sgp4Data.error));
            return (8, r, v);
        }
        double[] pos = new double[3];
        double[] vel = new double[3];

        //Debug.LogFormat("satepoch={0} propJD={1} delta={2} minSince={3}",
        //    sgp4Data.jdsatepoch, propJD, (propJD - sgp4Data.jdsatepoch), minutesSinceEpoch);

        bool result = SGP4unit.sgp4(sgp4Data, minutesSinceEpoch, pos, vel);
        if (!result) {
            Debug.Log("Error in Sat Prop. result=" + sgp4Data.ErrorString(sgp4Data.error));
            return (sgp4Data.error, r, v);
        }
        r = new Vector3d(pos[0], pos[1], pos[2]);
        v = new Vector3d(vel[0], vel[1], vel[2]);
        // results from SGP4 come back in km and km/sec
        r = r * GravityEngine.Instance().lengthScale;
        v = v * 3600.0; // convert to km/hr
        v = v * GravityScaler.GetVelocityScale();
        return (error, r, v);
    }

    /// <summary>
    /// Code to retreive the internal COE from the inner workings of the SGP4 code. 
    /// e.g. the initial argp will be modified by the secular perturbations
    /// </summary>
    /// <param name="propJD"></param>
    /// <returns></returns>
    public OrbitUtils.OrbitElements SGP4ToCOEAtTime(double propJD)
    {
        OrbitUtils.OrbitElements oe = new OrbitUtils.OrbitElements();
        Vector3d r = Vector3d.zero;
        Vector3d v = Vector3d.zero;

        double minutesSinceEpoch = (propJD - sgp4Data.jdsatepoch) * 24.0 * 60.0;
        if (minutesSinceEpoch < 0) {
            return null;
        }
        double[] pos = new double[3];
        double[] vel = new double[3];

        // Choice: C&P SGP4 evolver and export a bunch of stuff, or make some internal vars
        return oe;
    }

    public string TLEDateString()
    {

        return string.Format("{0}-{1}-{2} (Y-M-D)", sgp4Data.year, sgp4Data.month, sgp4Data.day);
    }


    public static string errString(int error)
    {
        string errString = "ok";
        switch(error) {
            case 1:
                errString = "Error Reading / Ini Data";
                break;
            case 2:
                errString = "Error in Sat Prop";
                break;
            case 3:
                errString = "GE Start date before satellite TLE date";
                break;

        }
        return errString;
    }

    /// <summary>
    /// Export a COE and date plus some additional info as a TLE string. 
    /// 
    /// Ref: https://celestrak.org/NORAD/documentation/tle-fmt.php
    /// 
    /// AAAAAAAAAAAAAAAAAAAAAAAA
    /// 1 NNNNNU NNNNNAAA NNNNN.NNNNNNNN +.NNNNNNNN +NNNNN-N +NNNNN-N N NNNNN
    /// 2 NNNNN NNN.NNNN NNN.NNNN NNNNNNN NNN.NNNN NNN.NNNN NN.NNNNNNNNNNNNNN
    /// </summary>
    /// <param name="oe"></param>
    /// <param name="epochYear"></param>
    /// <param name="epochDayWithFractionalPart"></param>
    /// <param name="name"></param>
    /// <param name="satelliteNumber"></param>
    /// <param name="launchYear"></param>
    /// <param name="launchNum"></param>
    /// <param name="launchPiece"></param>
    /// <returns></returns>


    public struct TLEAuxInfo
    {
        public string name;
        public int satelliteNumber;
        public string classification;
        public string internationDesignator;
        public double meanMotionD1;
        public double meanMotionD2;
        public double bstar;
        public long elementNumber;
        public long revNoAtEpoch;
        public int ephemerisType; 

        public TLEAuxInfo(string name)
        {
            this.name = name;
            satelliteNumber = 0;
            classification = "U";
            internationDesignator = "01234567";
            meanMotionD1 = 0.0;
            meanMotionD2 = 0.0;
            bstar = 0.0;
            elementNumber = 0;
            revNoAtEpoch = 0;
            ephemerisType = 0;
        }

        public TLEAuxInfo(SGP4SatData satData)
        {
            this.name = satData.name;
            satelliteNumber = satData.satnum;
            classification = satData.classification;
            internationDesignator = satData.intldesg;
            meanMotionD1 = satData.ndot;
            meanMotionD2 = satData.nddot;
            bstar = satData.bstar;
            elementNumber = satData.elnum;
            revNoAtEpoch = satData.revnum;
            ephemerisType = satData.numb;
        }
    }

    // This is a C&P of SGP4SatData adapted for a COE and auxInfo struct
    public static (string, string) ExportAsTLE(OrbitUtils.OrbitElements oe, TLEAuxInfo auxInfo, int epochYear, double epochDayWithFractionalPart)
    {
        double[] temp5 = SGP4unit.getgravconst(SGP4unit.Gravconsttype.wgs72);//, tumin, mu, radiusearthkm, xke, j2, j3, j4, j3oj2 );
        double radiusearthkm = temp5[2];
        double mu = temp5[1];
        double tumin = temp5[0];

        string line1 = "1 ";
        const double xpdotp = 1440.0 / (2.0 * System.Math.PI);  // 229.1831180523293


        line1 += string.Format("{0,5}{1,1} ", auxInfo.satelliteNumber, auxInfo.classification);
        line1 += string.Format("{0,8} ", auxInfo.internationDesignator);
        line1 += string.Format("{0:00}{1:000.00000000} ", epochYear % 100, epochDayWithFractionalPart);
        // Caqn only have an integer part
        double ndot = auxInfo.meanMotionD1;
        if (ndot >= 0)
            line1 += " ";
        else
            line1 += "-";
        ndot *= (xpdotp * 1440.0);
        // can only be fractional
        ndot = ndot - (int)(ndot);
        // undo unit conversion done in SGP4utils and trim minus so length is constant
        line1 += string.Format("{0:.00000000} ", ndot ).Replace("-", "");
        double nddot = auxInfo.meanMotionD2;
        if (nddot >= 0)
            line1 += "+";
        else
            line1 += "-";
        // need in scientific mode, but without the "E"
        // exponent will always be negative or zero, must put in a dash
        nddot = nddot * (xpdotp * 1440.0 * 1440.0);
        nddot = nddot - (int)(nddot);
        string nddotString = string.Format("{0:.00000E0}", nddot);
        // drop leading minus sign if present
        if (nddotString.StartsWith("-")) {
            nddotString = nddotString.Substring(1);
        }
        line1 += string.Format("{0} ", nddotString.Replace("E-", "-").Replace("E", "-").Replace(".", ""));

        string bstarString = string.Format(" {0:.00000E0}", auxInfo.bstar).Replace("E", "-").Replace(".", "");
        line1 += string.Format("{0} ", bstarString.Replace("--", "-").Replace(".", ""));
        line1 += string.Format("{0,1} {1:0000}", auxInfo.ephemerisType, auxInfo.elementNumber);
        line1 += CheckSum(line1);

        // Line2
        // Java code ignore checksum
        string line2 = "2 ";
        double R2D = Mathd.Rad2Deg;
        line2 += string.Format("{0,5} ", auxInfo.satelliteNumber);
        line2 += string.Format("{0:000.0000} ", oe.incl * R2D);
        line2 += string.Format("{0:000.0000} ", oe.raan * R2D);
        line2 += string.Format("{0:.0000000} ", oe.ecc).Replace(".", ""); // decimal point is implied
        line2 += string.Format("{0:000.0000} ", oe.argp * R2D);

        // Mean anomoly
        double E = OrbitUtils.ConvertTrueAnomolytoE(oe.nu, oe.ecc);
        double M = OrbitUtils.ConvertEtoMeanAnomoly(E, oe.ecc);
        line2 += string.Format("{0:000.0000} ", M * R2D);
        // MEAN MOTION (revs per day)
        double orbitU_a = (1.0 / GravityEngine.Instance().lengthScale) * oe.a; // SI -> km
        double a = orbitU_a / radiusearthkm; // SGP4 used a as a fraction of Earth radius
        // do inverse of TLE read:
        //            satrec.no = satrec.no / xpdotp; //* rad/min
        //            satrec.a = System.Math.Pow(satrec.no * tumin, (-2.0 / 3.0));

        double no = Mathd.Sqrt(1 / (a * a * a)) / tumin;
        line2 += string.Format("{0:00.00000000}", no * xpdotp);
        line2 += string.Format("{0:00000}", auxInfo.revNoAtEpoch);
        line2 += CheckSum(line2);
        // Java code ignore checksum
        return (line1, line2);
    }

    public static (string, string) ExportAsTLE(OrbitPredictor orbitPred, TLEAuxInfo auxInfo, int epochYear, double epochDayWithFractionalPart)
    {
        double[] temp5 = SGP4unit.getgravconst(SGP4unit.Gravconsttype.wgs72);//, tumin, mu, radiusearthkm, xke, j2, j3, j4, j3oj2 );
        double radiusearthkm = temp5[2];
        double mu = temp5[1];
        double tumin = temp5[0];

        string line1 = "1 ";
        const double xpdotp = 1440.0 / (2.0 * System.Math.PI);  // 229.1831180523293


        line1 += string.Format("{0,5}{1,1} ", auxInfo.satelliteNumber, auxInfo.classification);
        line1 += string.Format("{0,8} ", auxInfo.internationDesignator);
        line1 += string.Format("{0:00}{1:000.00000000} ", epochYear % 100, epochDayWithFractionalPart);
        // Caqn only have an integer part
        double ndot = auxInfo.meanMotionD1;
        if (ndot >= 0)
            line1 += " ";
        else
            line1 += "-";
        ndot *= (xpdotp * 1440.0);
        // can only be fractional
        ndot = ndot - (int)(ndot);
        // undo unit conversion done in SGP4utils and trim minus so length is constant
        line1 += string.Format("{0:.00000000} ", ndot).Replace("-", "");
        double nddot = auxInfo.meanMotionD2;
        if (nddot >= 0)
            line1 += "+";
        else
            line1 += "-";
        // need in scientific mode, but without the "E"
        // exponent will always be negative or zero, must put in a dash
        nddot = nddot * (xpdotp * 1440.0 * 1440.0);
        nddot = nddot - (int)(nddot);
        string nddotString = string.Format("{0:.00000E0}", nddot);
        // drop leading minus sign if present
        if (nddotString.StartsWith("-")) {
            nddotString = nddotString.Substring(1);
        }
        line1 += string.Format("{0} ", nddotString.Replace("E-", "-").Replace("E", "-").Replace(".", ""));

        string bstarString = string.Format(" {0:.00000E0}", auxInfo.bstar).Replace("E", "-").Replace(".", "");
        line1 += string.Format("{0} ", bstarString.Replace("--", "-").Replace(".", ""));
        line1 += string.Format("{0,1} {1:0000}", auxInfo.ephemerisType, auxInfo.elementNumber);
        line1 += CheckSum(line1);

        // Line2
        // Java code ignore checksum
        string line2 = "2 ";
        double R2D = Mathd.Rad2Deg;
        OrbitUniversal ou = orbitPred.GetOrbitUniversal();
        (double raan, double argp, double phase) = ou.SpecialOrientationPhase();
        line2 += string.Format("{0,5} ", auxInfo.satelliteNumber);
        line2 += string.Format("{0:000.0000} ", ou.inclination);
        line2 += string.Format("{0:000.0000} ", raan * R2D);
        line2 += string.Format("{0:.0000000} ", ou.eccentricity).Replace(".", ""); // decimal point is implied
        line2 += string.Format("{0:000.0000} ", argp * R2D);

        // Mean anomoly
        double E = OrbitUtils.ConvertTrueAnomolytoE(phase, ou.eccentricity);
        double M = OrbitUtils.ConvertEtoMeanAnomoly(E, ou.eccentricity);
        line2 += string.Format("{0:000.0000} ", M * R2D);
        // MEAN MOTION (revs per day)
        double orbitU_a = (1.0 / GravityEngine.Instance().lengthScale) * ou.GetMajorAxis(); // SI -> km
        double a = orbitU_a / radiusearthkm; // SGP4 used a as a fraction of Earth radius
        // do inverse of TLE read:
        //            satrec.no = satrec.no / xpdotp; //* rad/min
        //            satrec.a = System.Math.Pow(satrec.no * tumin, (-2.0 / 3.0));

        double no = Mathd.Sqrt(1 / (a * a * a)) / tumin;
        line2 += string.Format("{0:00.00000000}", no * xpdotp);
        line2 += string.Format("{0:00000}", auxInfo.revNoAtEpoch);
        line2 += CheckSum(line2);
        // Java code ignore checksum
        return (line1, line2);
    }


    public static int CheckSum(string s)
    {
        int cksum = 0; 
        for (int i=0; i < s.Length; i++) {
            if (char.IsDigit((char)s[i])) {
                cksum += s[i] - '0';
            } else if (s[i] == '-')
                cksum += 1;
        }
        return cksum % 10;
    }
}
