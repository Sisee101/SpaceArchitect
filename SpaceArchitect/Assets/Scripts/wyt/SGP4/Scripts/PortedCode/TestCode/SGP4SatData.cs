using UnityEngine;


namespace SGP4
{
    // based off of the "typedef struct elsetrec" in the CSSI's sgp4unit.h file
    // conatins all the data needed for a SGP4 propogated satellite
    // holds all initialization info, etc.

    //package sgp4_cssi;


    /**
    * 19 June 2009
    * converted to Java by:
    * @author Shawn E. Gano, shawn@gano.name
    * 
    */
    // Extensions made for GE 2021
    public class SGP4SatData
    {
        public int satnum; // changed to int SEG
        public int epochyr, epochtynumrev;
        public int error; // 0 = ok, 1= eccentricity (sgp4),   6 = satellite decay, 7 = tle data
        public char operationmode;
        public char init, method;

        public SGP4SatData()
        {

        }

        // Copy constructor at the bottom...too brutal

        // NB - keep year month day etc
        public int year, month, day, hr, min;
        public double sec;

        private const double SMALL = 1E-4;

        //    *  return code - non-zero on error.
        //*                   1 - mean elements, ecc >= 1.0 or ecc< -0.001 or a < 0.95 er
        //*                   2 - mean motion less than 0.0
        //*                   3 - pert elements, ecc< 0.0  or  ecc> 1.0
        //*                   4 - semi-latus rectum < 0.0
        //*                   5 - epoch elements are sub-orbital
        //*                   6 - satellite has decayed
        public const int OK_SGP4 = 0;
        public const int ECCENTRICITY_ERR_SGP4 = 1;
        public const int MEAN_MOTION_ERR_SGP4 = 2;
        public const int ECC_OOB_EWRR_SGP4 = 3;
        public const int SLR_LT_0_ERR_SGP4 = 4;
        public const int EPOCH_SUBO_ERR_SGP4 = 5;
        public const int DECAY_ERR_SGP4 = 6;
        public const int TLE_ERR_SGP4 = 7;
        public const int TOO_EARLY_ERR_SGP4 = 8;

        public string ErrorString(int errorNum)
        {
            string s = "unknown";
            switch (errorNum) {
                case 0:
                    s = "ok";
                    break;
                case 1:
                    s = "eccentricity (sgp4)";
                    break;
                case 2:
                    s = "mean motion less than 0.0";
                    break;
                case 3:
                    s = "pert elements, ecc< 0.0  or  ecc> 1.0";
                    break;
                case 4:
                    s = "semi-latus rectum < 0.0";
                    break;
                case 5:
                    s = "epoch elements are sub-orbital";
                    break;
                case 6:
                    s = "satellite decay";
                    break;
                case 7:
                    s = "tle data";
                    break;
                case 8: // GE
                    s = "time to evolve before epoch time";
                    break;
                default:
                    s = "code=" + errorNum.ToString();
                    break;

            }
            return s;
        }

        public SGP4unit.Gravconsttype gravconsttype; // gravity constants to use - SEG

        /* Near Earth */
        public int isimp;
        public double aycof, con41, cc1, cc4, cc5, d2, d3, d4,
                      delmo, eta, argpdot, omgcof, sinmao, t, t2cof, t3cof,
                      t4cof, t5cof, x1mth2, x7thm1, mdot, nodedot, xlcof, xmcof,
                      nodecf;

        /* Deep Space */
        public int irez;
        public double d2201, d2211, d3210, d3222, d4410, d4422, d5220, d5232,
                      d5421, d5433, dedt, del1, del2, del3, didt, dmdt,
                      dnodt, domdt, e3, ee2, peo, pgho, pho, pinco,
                      plo, se2, se3, sgh2, sgh3, sgh4, sh2, sh3,
                      si2, si3, sl2, sl3, sl4, gsto, xfact, xgh2,
                      xgh3, xgh4, xh2, xh3, xi2, xi3, xl2, xl3,
                      xl4, xlamo, zmol, zmos, atime, xli, xni;

        public double a, altp, alta, epochdays, jdsatepoch, nddot, ndot,
                      bstar, rcse, inclo, nodeo, ecco, argpo, mo,
                      no;

        // Extra Data added by SEG - from TLE and a name variable (and save the lines for future use)
        public string name = "", line1 = "", line2 = "";
        public bool tleDataOk;
        public string classification, intldesg;
        public int nexp, ibexp, numb; // numb is the second number on line 1
        public long elnum, revnum;

        // NBP - add some fields for tracking internal calculation
        public double _xinc, _xnode, _su, _el, _am, _mrt, _argpp;


        public void UpdateOrbitInfo(OrbitUniversal orbitU, double timeJD)
        {
            GravityEngine ge = GravityEngine.Instance();

            // do the other way (derive no from a)
            //satrec.a = System.Math.Pow(satrec.no * tumin, (-2.0 / 3.0));
            double[] temp = SGP4unit.getgravconst(SGP4unit.Gravconsttype.wgs72);
            double tumin = temp[0];
            double radiusearthkm = temp[2];

            // ---- convert to sgp4 units ----
            double orbitU_a = (1.0 / ge.lengthScale) * orbitU.GetMajorAxis(); // SI -> km
            a = orbitU_a / radiusearthkm; // SGP4 used a as a fraction of Earth radius
            no = System.Math.Pow(a, -3.0 / 2.0) / tumin;

            // ---- find standard orbital elements ----
            inclo = orbitU.inclination * Mathd.Deg2Rad;
            nodeo = orbitU.omega_uc * Mathd.Deg2Rad;
            argpo = orbitU.omega_lc * Mathd.Deg2Rad;
            double Eanom = OrbitUtils.ConvertTrueAnomolytoE(orbitU.phase * Mathd.Deg2Rad, orbitU.eccentricity);
            mo = OrbitUtils.ConvertEtoMeanAnomoly(Eanom, orbitU.eccentricity);

            alta = a * (1.0 + ecco) - 1.0;
            altp = a * (1.0 - ecco) - 1.0;

            jdsatepoch = timeJD;
            UpdateEpochYearDay();

        }

        public void UpdateOrbitInfo(Vector3d r, Vector3d v, double timeJD, NBody centerNbody)
        {
            double mu = GravityEngine.Instance().GetPhysicsMass(centerNbody);
            Debug.LogWarning("Fix scaling");
            OrbitUtils.OrbitElements oe = OrbitUtils.RVtoCOE(r, v, null, mu, true);
            UpdateOrbitInfo(oe, timeJD);
        }

        public void UpdateOrbitInfo(OrbitUtils.OrbitElements orbitE, double timeJD)
        {
            GravityEngine ge = GravityEngine.Instance();

            // do the other way (derive no from a)
            //satrec.a = System.Math.Pow(satrec.no * tumin, (-2.0 / 3.0));
            double[] temp = SGP4unit.getgravconst(SGP4unit.Gravconsttype.wgs72);
            double tumin = temp[0];
            double radiusearthkm = temp[2];

            // ---- convert to sgp4 units ----
            double orbitU_a = (1.0 / ge.lengthScale) * orbitE.a; // SI -> km
            a = orbitU_a / radiusearthkm; // SGP4 used a as a fraction of Earth radius
            tumin = temp[0];
            no = System.Math.Pow(a, -3.0 / 2.0) / tumin;

            ecco = orbitE.ecc;
             // ---- find standard orbital elements ----
            inclo = orbitE.incl;
            nodeo = orbitE.raan;
            if (orbitE.typeOrbit == OrbitUtils.OrbitElements.TypeOrbit.CIRCULAR_INCLINED) {
                argpo = orbitE.arglat;
            } else if (orbitE.typeOrbit == OrbitUtils.OrbitElements.TypeOrbit.CIRCULAR_EQUATORIAL) {
                argpo = orbitE.arglat;
            } else {
                argpo = orbitE.argp;
            }
            double Eanom = OrbitUtils.ConvertTrueAnomolytoE(orbitE.GetPhase() , orbitE.ecc);
            mo = OrbitUtils.ConvertEtoMeanAnomoly(Eanom, orbitE.ecc);

            alta = a * (1.0 + ecco) - 1.0;
            altp = a * (1.0 - ecco) - 1.0;

            jdsatepoch = timeJD;
            UpdateEpochYearDay();

        }

        private void UpdateEpochYearDay()
        {
            double[] dateInfo = SGP4utils.InvJday(jdsatepoch);
            year = (int) dateInfo[0];
            month = (int)dateInfo[1];
            day = (int)dateInfo[2];
            epochyr = year % 100;
            double jdDay0 = SGP4utils.JulianDate(year, 1, 0, 0, 0, 0);
            epochdays = jdsatepoch - jdDay0;
        }

        public static (int yr00, double day)  EpochYearDay(double jd)
        {
            double[] dateInfo = SGP4utils.InvJday(jd);
            int year = (int)dateInfo[0];
            int month = (int)dateInfo[1];
            int day = (int)dateInfo[2];
            int epochyr = year % 100;
            double jdDay0 = SGP4utils.JulianDate(year, 1, 0, 0, 0, 0);
            double epochdays = jd - jdDay0;
            return (epochyr, epochdays);
        }

        public string LogString()
        {
            string s = "SGP4 Details:\n";
            s += string.Format("mo={0:0.000E0} no={1:0.000E0} a={2:0.000E0}\njdsatepoch={3}",
                mo, no, a,
                jdsatepoch);
            return s;
        }

        const double rad2deg =  180.0/System.Math.PI;         

        /// <summary>
        /// Return the classic TLE format for that "punch card" look and feel.
        /// </summary>
        /// <returns></returns>
        public (string, string) CreateTLELines()
        {
            string line1 = "1 ";
            const double xpdotp = 1440.0 / (2.0 * System.Math.PI);  // 229.1831180523293


            line1 += string.Format("{0,5}{1,1} ", satnum, classification);
            line1 += string.Format("{0,8} ", intldesg);
            line1 += string.Format("{0:00}{1:000.00000000} ", epochyr, epochdays);

            if (ndot >= 0)
                line1 += " ";
            else
                line1 += "-";
            // undo unit conversion done in SGP4utils and trim minus so length is constant
            line1 += string.Format("{0:.00000000} ", ndot * (xpdotp * 1440.0)).Replace("-","");

            if (nddot >= 0)
                line1 += "+";
            else
                line1 += "-";
            // need in scientific mode, but without the "E"
            // exponent will always be negative or zero, must put in a dash
            string nddotString = string.Format("{0:.00000E0}", nddot * (xpdotp * 1440.0 * 1440.0));
            // drop leading minus sign if present
            if (nddotString.StartsWith("-")) {
                nddotString = nddotString.Substring(1);
            }
            line1 += string.Format("{0} ", nddotString.Replace("E-", "-").Replace("E","-").Replace(".",""));
            string bstarString = string.Format(" {0:.00000E0}", bstar ).Replace("E", "-").Replace(".","");
            line1 += string.Format("{0} ", bstarString.Replace("E-", "-").Replace(".",""));
            line1 += string.Format("{0,1} {1:0000}", numb, elnum);
            // Java code ignore checksum

            string line2 = "2 ";
            line2 += string.Format("{0,5} ", satnum);
            line2 += string.Format("{0:000.0000} ", inclo * rad2deg);
            line2 += string.Format("{0:000.0000} ", nodeo * rad2deg);
            line2 += string.Format("{0:.0000000} ", ecco).Replace(".",""); // decimal point is implied
            line2 += string.Format("{0:000.0000} ", argpo * rad2deg);
            line2 += string.Format("{0:000.0000} ", mo * rad2deg);
            line2 += string.Format("{0:00.00000000}", no * xpdotp);
            line2 += string.Format("{0:00000}", revnum);
            // Java code ignore checksum
            return (line1, line2);
        }

        // icky globals for secant - beware
        private static SGP4SatData satData;
        private static double el_target;
        public static double EpExpression(double ecco)
        {
            double temp = 1.0 / (satData._am * (1.0 - ecco * ecco));
            double axnl = ecco * System.Math.Cos(satData._argpp);
            double aynl = ecco * System.Math.Sin(satData._argpp) + temp * satData.aycof;
            double el2 = axnl * axnl + aynl * aynl;
            return el_target - System.Math.Sqrt(el2);
        }

        public static double SolveForEcc(SGP4SatData data, double ecco1, double ecco2, double el)
        {
            satData = data;
            el_target = el;
            double root = SecantRootFind.Secant(EpExpression, ecco1, ecco2);
            return root;
        }

        /// <summary>
        /// The Unity console does not have a fixed width font BUT if open log in Notepad then this can be useful
        /// </summary>
        /// <param name="tleLine"></param>
        /// <returns></returns>
        public static string AddCols(string tleLine)
        {
            return ".........1.........2.........3.........4.........5.........6.........\n" +
                   "1234567890123456789012345678901234567890123456789012345678901234567890\n" +
                   tleLine;
        }

        public SGP4SatData(SGP4SatData copyFrom)
        {
            // oh, the horror
            satnum = copyFrom.satnum;
            epochyr = copyFrom.epochyr;
            epochtynumrev = copyFrom.epochtynumrev;
            error = copyFrom.error;
            operationmode = copyFrom.operationmode;
            init = copyFrom.init;
            method = copyFrom.method;
            year = copyFrom.year;
            month = copyFrom.month;
            day = copyFrom.day;
            // yay python
            isimp = copyFrom.isimp;
            aycof = copyFrom.aycof;
            con41 = copyFrom.con41;
            cc1 = copyFrom.cc1;
            cc4 = copyFrom.cc4;
            cc5 = copyFrom.cc5;
            d2 = copyFrom.d2;
            d3 = copyFrom.d3;
            d4 = copyFrom.d4;
            delmo = copyFrom.delmo;
            eta = copyFrom.eta;
            argpdot = copyFrom.argpdot;
            omgcof = copyFrom.omgcof;
            sinmao = copyFrom.sinmao;
            t2cof = copyFrom.t2cof;
            t3cof = copyFrom.t3cof;
            t4cof = copyFrom.t4cof;
            t5cof = copyFrom.t5cof;
            x1mth2 = copyFrom.x1mth2;
            x7thm1 = copyFrom.x7thm1;
            mdot = copyFrom.mdot;
            nodedot = copyFrom.nodedot;
            xlcof = copyFrom.xlcof;
            xmcof = copyFrom.xmcof;
            nodecf = copyFrom.nodecf;
            irez = copyFrom.irez;
            d2201 = copyFrom.d2201;
            d2211 = copyFrom.d2211;
            d3210 = copyFrom.d3210;
            d3222 = copyFrom.d3222;
            d4410 = copyFrom.d4410;
            d4422 = copyFrom.d4422;
            d5220 = copyFrom.d5220;
            d5232 = copyFrom.d5232;
            d5421 = copyFrom.d5421;
            d5433 = copyFrom.d5433;
            dedt = copyFrom.dedt;
            del1 = copyFrom.del1;
            del2 = copyFrom.del2;
            del3 = copyFrom.del3;
            didt = copyFrom.didt;
            dmdt = copyFrom.dmdt;
            dnodt = copyFrom.dnodt;
            domdt = copyFrom.domdt;
            e3 = copyFrom.e3;
            ee2 = copyFrom.ee2;
            peo = copyFrom.peo;
            pgho = copyFrom.pgho;
            pho = copyFrom.pho;
            pinco = copyFrom.pinco;
            plo = copyFrom.plo;
            se2 = copyFrom.se2;
            se3 = copyFrom.se3;
            sgh2 = copyFrom.sgh2;
            sgh3 = copyFrom.sgh3;
            sgh4 = copyFrom.sgh4;
            sh2 = copyFrom.sh2;
            sh3 = copyFrom.sh3;
            si2 = copyFrom.si2;
            si3 = copyFrom.si3;
            sl2 = copyFrom.sl2;
            sl3 = copyFrom.sl3;
            sl4 = copyFrom.sl4;
            gsto = copyFrom.gsto;
            xfact = copyFrom.xfact;
            xgh2 = copyFrom.xgh2;
            xgh3 = copyFrom.xgh3;
            xgh4 = copyFrom.xgh4;
            xh2 = copyFrom.xh2;
            xh3 = copyFrom.xh3;
            xi2 = copyFrom.xi2;
            xi3 = copyFrom.xi3;
            xl2 = copyFrom.xl2;
            xl3 = copyFrom.xl3;
            xl4 = copyFrom.xl4;
            xlamo = copyFrom.xlamo;
            zmol = copyFrom.zmol;
            zmos = copyFrom.zmos;
            atime = copyFrom.atime;
            xli = copyFrom.xli;
            xni = copyFrom.xni;
            altp = copyFrom.altp;
            alta = copyFrom.alta;
            epochdays = copyFrom.epochdays;
            jdsatepoch = copyFrom.jdsatepoch;
            nddot = copyFrom.nddot;
            ndot = copyFrom.ndot;
            bstar = copyFrom.bstar;
            rcse = copyFrom.rcse;
            inclo = copyFrom.inclo;
            nodeo = copyFrom.nodeo;
            ecco = copyFrom.ecco;
            argpo = copyFrom.argpo;
            mo = copyFrom.mo;
            no = copyFrom.no;
            name = copyFrom.name;
            tleDataOk = copyFrom.tleDataOk;
            classification = copyFrom.classification;
            intldesg = copyFrom.intldesg;
            nexp = copyFrom.nexp;
            ibexp = copyFrom.ibexp;
            numb = copyFrom.numb;
            elnum = copyFrom.elnum;
            revnum = copyFrom.revnum;
            _xinc = copyFrom._xinc;
            _xnode = copyFrom._xnode;
            _su = copyFrom._su;
            _el = copyFrom._el;
            _am = copyFrom._am;
            _mrt = copyFrom._mrt;
            _argpp = copyFrom._argpp;

        }
    }
  
}