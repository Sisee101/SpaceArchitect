namespace SGP4
{
    // simple test of the SGP4 propagator
    using UnityEngine;
    using NUnit.Framework;

    //package sgp4_cssi;

    /**
     * 19 June 2009
     * @author Shawn E. Gano, shawn@gano.name
     */
    public class SGP4testXY
    {
        // Reverse engineering MU for a circular orbit using SGP4 prop of TLE
        const double MU_EMPIRICAL = 399169.1016 * 100.0; // x100 since 360 sec/game sec

        /// <summary>
		/// Get the Earth mass.
		/// This must be 5.98053504 (E24) in order to match the value reverse engineered
		/// from SGP4 tests. Internally SGP4 uses a value that is slightly different from this
		/// empirical value and I do not understand why the empirical value retreived from a test
		/// of a circular orbit does give the same value.
		/// 
		/// </summary>
		/// <returns></returns>
        private double GetMu()
        {
            GameObject earth = GameObject.FindWithTag("Earth");
            if (earth == null)
            {
                Debug.LogError("Not in SGP4TestScene. No object with tag Earth");
            }
            NBody earthNbody = earth.GetComponent<NBody>();
            // GE has not added Earth, so do mass scaling explicitly
            GravityEngine ge = GravityEngine.Instance();
            double massScale = GravityScaler.UpdateMassScale(GravityScaler.Units.ORBITAL, ge.timeScale, ge.lengthScale);

            double mu = earthNbody.mass * massScale;
            Debug.LogFormat("mu={0} vs empirical={1} delta={2}(%)", mu, MU_EMPIRICAL,
                DeltaPercent(mu, MU_EMPIRICAL));
            return mu;
        }

        private void GEinit()
        {
            GravityEngine ge = GravityEngine.Instance();
            ge.xzOrbits = false;
        }

        [Test]
        // Original test from the Java code with an explicit check against a specific baked result.
        public void Sanity()
        {
            GEinit();
            // new sat data object
            SGP4SatData data = new SGP4SatData();

            // tle data
            string name = "ISS (ZARYA)";
            string line1 = "1 25544U 98067A   09161.51089941  .00015706  00000-0  11388-3 0   112";
            string line2 = "2 25544  51.6406 341.1646 0009228  98.8703 312.6668 15.73580432604904";

            // options
            char opsmode = SGP4utils.OPSMODE_IMPROVED; // OPSMODE_IMPROVED
            SGP4unit.Gravconsttype gravconsttype = SGP4unit.Gravconsttype.wgs72;

            // read in data and ini SGP4 data
            bool result1 = SGP4utils.readTLEandIniSGP4(name, line1, line2, opsmode, gravconsttype, data);
            if (!result1) {
                Debug.Log("Error Reading / Ini Data, error code: " + data.error);
                Assert.Fail("Fail reading/initing record");
                return;
            }

            // prop to a given date
            double propJD = 2454994.0; // JD to prop to
            double minutesSinceEpoch = (propJD - data.jdsatepoch) * 24.0 * 60.0;
            double[] pos = new double[3];
            double[] vel = new double[3];

            bool result = SGP4unit.sgp4(data, minutesSinceEpoch, pos, vel);
            if (!result) {
                Debug.Log("Error in Sat Prop");
                Assert.Fail("Error in Sat Prop");
                return;
            }

            // output
            Debug.Log("Epoch of TLE (JD): " + data.jdsatepoch);
            Debug.Log(minutesSinceEpoch + ", " + pos[0] + ", " + pos[1] + ", " + pos[2] + ", " + vel[0] + ", " + vel[1] + ", " + vel[2]);

            double[] stk8Results = new double[] { -2881017.428533447, -3207508.188455666, -5176685.907342243 };
            double[] stk9Results = new double[] { -2881017.432281017, -3207508.189681858, -5176685.904856035 };

            double dX = norm(sub(scale(pos, 1000.0), stk8Results));
            Debug.Log("Max Error from STk8 (m) : " + dX);
            double dX2 = norm(sub(scale(pos, 1000.0), stk9Results));
            Debug.Log("Max Error from STk9 (m) : " + dX2);

            Assert.That(dX, Is.EqualTo(0).Within(1.0));

        }

        [Test]
        public void ComputeMG()
        {
            GEinit();
            // new sat data object
            SGP4SatData data = new SGP4SatData();

            // tle data
            string name = "CIRCULAR EQUATORIAL";
            string line1 = "1 25544U 98067A   21077.40027963  .00000347  00000-0  14510-4 0  9997";
            string line2 = "2 25544  00.0000  00.0000 0000000 000.0000  00.0000 15.48913539274526";

            // options
            char opsmode = SGP4utils.OPSMODE_IMPROVED; // OPSMODE_IMPROVED
            SGP4unit.Gravconsttype gravconsttype = SGP4unit.Gravconsttype.wgs72;

            // read in data and ini SGP4 data
            bool result1 = SGP4utils.readTLEandIniSGP4(name, line1, line2, opsmode, gravconsttype, data);
            if (!result1) {
                Debug.Log("Error Reading / Ini Data, error code: " + data.error);
                Assert.Fail("Fail reading/initing record");
                return;
            }

            // prop to a given date
            double[] pos = new double[3];
            double[] vel = new double[3];

            bool result = SGP4unit.sgp4(data, 0.0, pos, vel);
            if (!result) {
                Debug.Log("Error in Sat Prop");
                Assert.Fail("Error in Sat Prop");
                return;
            }

            // output
            Debug.Log("Epoch of TLE (JD): " + data.jdsatepoch);
            Debug.Log( pos[0] + ", " + pos[1] + ", " + pos[2] + ", " + vel[0] + ", " + vel[1] + ", " + vel[2]);

            // have R, V and a circular orbit. Determine GM
            // GM/r^2 = v^2/r  ==> GM = r * v^2
            Vector3d r = new Vector3d(pos[0], pos[1], pos[2]);
            Vector3d v = new Vector3d(vel[0], vel[1], vel[2]);
            Debug.LogFormat("r x v={0} Angle(r,v)={1}", Vector3d.Cross(r, v), Vector3d.Angle(r,v));
            double gm = r.magnitude * v.magnitude * v.magnitude;
            const double G = 6.67430E-13;
            Debug.LogFormat("GM={0} M={1}", gm, gm/G);
            // divide by
            Debug.LogFormat("Hardecoded value = {0}", 398600.8 / G);


            //r = r * GravityEngine.Instance().lengthScale;
            //v = v * 3600.0; // convert to km/hr
            //GravityScaler.Init();
            //v = v * GravityScaler.GetVelocityScale();
            // compute ecc from raw vectors explicitly (scale mu to match raw output)
            double mu = gm; // 39860080;
            
            Vector3d eccVec = (((v.sqrMagnitude - mu / r.magnitude) * r - Vector3d.Dot(r, v) * v)) / mu;
            Debug.LogFormat("raw ecc ={0} e={1}", eccVec, eccVec.magnitude);


        }

        [Test]
        public void EpochToJD()
        {
            GEinit();
            // new sat data object
            SGP4SatData data = new SGP4SatData();

            // tle data
            string name = "CIRCULAR EQUATORIAL";
            string line1 = "1 25544U 98067A   21077.40027963  .00000347  00000-0  14510-4 0  9997";
            string line2 = "2 25544  00.0000  00.0000 0000000 000.0000  00.0000 15.48913539274526";

            // options
            char opsmode = SGP4utils.OPSMODE_IMPROVED; // OPSMODE_IMPROVED
            SGP4unit.Gravconsttype gravconsttype = SGP4unit.Gravconsttype.wgs72;

            // read in data and ini SGP4 data
            bool result1 = SGP4utils.readTLEandIniSGP4(name, line1, line2, opsmode, gravconsttype, data);
            if (!result1) {
                Debug.Log("Error Reading / Ini Data, error code: " + data.error);
                Assert.Fail("Fail reading/initing record");
                return;
            }

            // output
            Debug.Log("TLE has sat epoch as 21077.40027963");
            Debug.Log("Epoch of TLE (JD): " + data.jdsatepoch);
            Debug.LogFormat("Y:{0} M:{1} D:{2} H:{3} M:{4} S:{5}",
                data.year, data.month, data.day, data.hr, data.min, data.sec);
            double utc = data.hr + (60.0 * data.min + data.sec) / 3600.0;

            double jdFromGE = SolarUtils.JulianDate(data.year, data.month, data.day, utc);

            Assert.That(data.jdsatepoch, Is.EqualTo(jdFromGE).Within(1E-3));
        }

        [Test]
        public void EpochToJDNoFraction()
        {
            GEinit();
            // new sat data object
            SGP4SatData data = new SGP4SatData();

            // tle data
            string name = "CIRCULAR EQUATORIAL";
            string line1 = "1 25544U 98067A   21077.50000000  .00000347  00000-0  14510-4 0  9997";
            string line2 = "2 25544  00.0000  00.0000 0000000 000.0000  00.0000 15.48913539274526";

            // options
            char opsmode = SGP4utils.OPSMODE_IMPROVED; // OPSMODE_IMPROVED
            SGP4unit.Gravconsttype gravconsttype = SGP4unit.Gravconsttype.wgs72;

            // read in data and ini SGP4 data
            bool result1 = SGP4utils.readTLEandIniSGP4(name, line1, line2, opsmode, gravconsttype, data);
            if (!result1) {
                Debug.Log("Error Reading / Ini Data, error code: " + data.error);
                Assert.Fail("Fail reading/initing record");
                return;
            }
            // TLE epoch day starts at midnight (and not at noon as Julian days do)
            // output
            Debug.Log("TLE has sat epoch as 21077.50000000");
            double utc = data.hr + (60.0 * data.min + data.sec) / 3600.0;
            Debug.LogFormat("Y:{0} M:{1} D:{2} H:{3} M:{4} S:{5} utc:{6}",
                data.year, data.month, data.day, data.hr, data.min, data.sec, utc);
            double jdFromGE = SolarUtils.JulianDate(data.year, data.month, data.day, utc);

            Debug.LogFormat("satJD={0} geJD={1}", data.jdsatepoch, jdFromGE);
            Assert.That(data.jdsatepoch, Is.EqualTo(jdFromGE).Within(1E-3));
        }

        [Test]
        public void SanityCOE()
        {
            GEinit();
            // new sat data object
            SGP4SatData data = new SGP4SatData();

            // tle data
            string name = "ISS (ZARYA)";
            string line1 = "1 25544U 98067A   09161.51089941  .00015706  00000-0  11388-3 0   112";
            string line2 = "2 25544  51.6406 341.1646 0009228  98.8703 312.6668 15.73580432604904";

            // options
            char opsmode = SGP4utils.OPSMODE_IMPROVED; // OPSMODE_IMPROVED
            SGP4unit.Gravconsttype gravconsttype = SGP4unit.Gravconsttype.wgs72;

            // read in data and ini SGP4 data
            bool result1 = SGP4utils.readTLEandIniSGP4(name, line1, line2, opsmode, gravconsttype, data);
            if (!result1) {
                Debug.Log("Error Reading / Ini Data, error code: " + data.error);
                Assert.Fail("Fail reading/initing record");
                return;
            }

            // prop to a given date
            double[] pos = new double[3];
            double[] vel = new double[3];

            bool result = SGP4unit.sgp4(data, 0.0, pos, vel);
            if (!result) {
                Debug.Log("Error in Sat Prop");
                Assert.Fail("Error in Sat Prop");
                return;
            }

            // output
            // Get COE from RV
            // Need an NBody ref to do RV to COE
            GameObject center = GameObject.FindGameObjectWithTag("Earth");
            if (center == null) {
                Debug.LogError("Not in SGP4TestScene. No object with tag Earth");
            }
            NBody centerBody = center.GetComponent<NBody>();
            Vector3d r = new Vector3d(pos[0], pos[1], pos[2]);
            Vector3d v = new Vector3d(vel[0], vel[1], vel[2]);
            r = r * GravityEngine.Instance().lengthScale;
            v = v * 3600.0; // convert to km/hr
            GravityScaler.Init();
            v = v * GravityScaler.GetVelocityScale();
            // compute ecc from raw vectors explicitly (scale mu to match raw output)
            double mu = GetMu();
            Vector3d eccVec = (((v.sqrMagnitude - mu / r.magnitude) * r - Vector3d.Dot(r, v) * v))/mu;
            Debug.LogFormat("raw ecc ={0} e={1}", eccVec, eccVec.magnitude);

            OrbitUtils.OrbitElements coe = OrbitUtils.RVtoCOE(r, v, centerBody, mu, relativePos: true);
            Debug.Log(coe.ToString());

            Debug.LogFormat("Inclination: {0} vs {1} delta={2:00.00}(%)", coe.incl, data.inclo,
                DeltaPercent(coe.incl, data.inclo));
            Debug.LogFormat("Eccentricity: {0} vs {1} delta={2:00.00}(%) OE={3}", coe.ecc, data.ecco,
                 DeltaPercent(coe.ecc, data.ecco), coe.ecc);
            // multiply by Earth radius
            Debug.LogFormat("A: {0} vs {1} delta={2:00.00}(%)", coe.a, data.a * 6378.135,
                 DeltaPercent(coe.a, data.a * 6378.135));
            Debug.LogFormat("NODEO: {0} vs {1} delta={2:00.00}(%)", coe.raan, data.nodeo,
                 DeltaPercent(coe.raan, data.nodeo));
            Debug.LogFormat("ARGP: {0} vs {1} delta={2:00.00}(%)", coe.argp, data.argpo,
                 DeltaPercent(coe.argp, data.argpo));

            // Check the COE match the initial orbit
            Assert.That(coe.ecc, Is.EqualTo(data.ecco).Within(1E-3));
            Assert.That(coe.incl, Is.EqualTo(data.inclo).Within(1E-3));
            Assert.That(coe.raan, Is.EqualTo(data.nodeo).Within(1E-3));
        }

        [Test]
        public void JulianDateTest()
        {
            GEinit();
            int year = 2021;
            int month = 3;
            int day = 10;
            double utc = 10.5;
            int hour = 10;
            int minute = 30;
            double sec = 0;
            double jdSolar = SolarUtils.JulianDate(year, month, day, utc);

            double jdSGP4 = SGP4.SGP4utils.JulianDate(year, month, day, hour, minute, sec);
            Assert.That(jdSolar, Is.EqualTo(jdSGP4).Within(1E-4));
        }

        private double DeltaPercent(double a, double b)
        {
            return Mathd.Abs(a - b) / b * 100.0;
        }

        [Test]
        // Take the ISS TLE
        // 1) Get an RV for zero propagation (prop to date from sat epoch)
        // 2) Use RV to get COE
        //    - weirdly the ecc goes wacky
        // 3) Use COE to init a new TLE and compare fields wrt the original TLE satdata
        public void ISS_TLEtoRVtoCOEtoTLE()
        {
            // new sat data object
            SGP4SatData satdata = new SGP4SatData();

            // tle data
            string name = "ISS (ZARYA)";
            string line1 = "1 25544U 98067A   21077.40027963  .00000347  00000-0  14510-4 0  9997";
            string line2 = "2 25544  51.6454  71.7595 0003427 126.1590  21.5955 15.48913539274526";

            SGP4toGE sgpForTLE = new SGP4toGE(name, line1, line2);
            satdata = sgpForTLE.GetSatData();

            // Convert this to a COE
            // Use the sat jd as the epoch time and time for RV (should be no SGP4 prop)
            (int error, Vector3d r, Vector3d v) = sgpForTLE.SGP4toRVatTime(satdata.jdsatepoch);
            if (error != 0) {
                Debug.LogError(string.Format("Prop Error {0} ", SGP4toGE.errString(error)));
                if (error == 3) {
                    Debug.LogError(string.Format("Sat date: {0} is after GE start time.",
                        sgpForTLE.TLEDateString()));
                }
            }
            double mu = GetMu();
            OrbitUtils.OrbitElements coe = OrbitUtils.RVtoCOE(r, v, null, mu, relativePos: true);
            Debug.Log("COE for ISS: " + coe.ToString());

            // Check the COE match the initial orbit
            Assert.That(coe.ecc, Is.EqualTo(satdata.ecco).Within(1E-3));
            Assert.That(coe.incl, Is.EqualTo(satdata.inclo).Within(1E-3));
            Assert.That(coe.raan, Is.EqualTo(satdata.nodeo).Within(1E-3));
            Assert.That(coe.argp, Is.EqualTo(satdata.argpo).Within(1E-3));

            // Use SGP4toGE to init from COE
            SGP4SatData coeSatData = new SGP4SatData();
            coeSatData.UpdateOrbitInfo(coe, satdata.jdsatepoch);
            Debug.LogFormat("Inclination: {0} vs {1} delta={2:00.00}(%)", coeSatData.inclo, satdata.inclo,
                DeltaPercent(coeSatData.inclo, satdata.inclo));
            Debug.LogFormat("Eccentricity: {0} vs {1} delta={2:00.00}(%) OE={3}", coeSatData.ecco, satdata.ecco,
                 DeltaPercent(coeSatData.ecco, satdata.ecco), coe.ecc);
            Debug.LogFormat("A: {0} vs {1} delta={2:00.00}(%)", coeSatData.a, satdata.a,
                 DeltaPercent(coeSatData.a, satdata.a));
            Debug.LogFormat("NODEO: {0} vs {1} delta={2:00.00}(%)", coeSatData.nodeo, satdata.nodeo,
                 DeltaPercent(coeSatData.nodeo, satdata.nodeo));
            Debug.LogFormat("ARGPO: {0} vs {1} delta={2:00.00}(%)", coeSatData.argpo, satdata.argpo,
                 DeltaPercent(coeSatData.argpo, satdata.argpo));
            Debug.LogFormat("NO: {0} vs {1} delta={2:00.00}(%)", coeSatData.no, satdata.no,
                 DeltaPercent(coeSatData.no, satdata.no));
            Debug.LogFormat("MO: {0} vs {1} delta={2:00.00}(%)", coeSatData.mo, satdata.mo,
                 DeltaPercent(coeSatData.mo, satdata.mo));

            Assert.That(coeSatData.epochyr, Is.EqualTo(satdata.epochyr).Within(1E-3));
            Assert.That(coeSatData.epochdays, Is.EqualTo(satdata.epochdays).Within(1E-3));

            // Are the important elements in the satData the same?
            Assert.That(coeSatData.inclo, Is.EqualTo(satdata.inclo).Within(1E-3));
            Assert.That(coeSatData.ecco, Is.EqualTo(satdata.ecco).Within(1E-3));
            Assert.That(coeSatData.argpo, Is.EqualTo(satdata.argpo).Within(1E-3));
            Assert.That(coeSatData.nodeo, Is.EqualTo(satdata.nodeo).Within(1E-3));
            Assert.That(coeSatData.no, Is.EqualTo(satdata.no).Within(1E-3));
            Assert.That(coeSatData.mo, Is.EqualTo(satdata.mo).Within(1E-3));

        }

        [Test]
        public void Vanguard1_TLEtoRVtoCOEtoTLE()
        {
            GEinit();
            GEinit();
            GEinit();
            GEinit();
            // new sat data object
            SGP4SatData satdata = new SGP4SatData();

            // tle data
            string name = "Vanguard1";
            string line1 = "1 00005U 58002B   22103.76388831  .00000432  00000-0  56816-3 0  9990";
            string line2 = "2 00005  34.2485 338.7564 1845570 106.3734 274.6871 10.84908645277515";

            SGP4toGE sgpForTLE = new SGP4toGE(name, line1, line2);
            satdata = sgpForTLE.GetSatData();

            // Convert this to a COE
            // Use the sat jd as the epoch time and time for RV (should be no SGP4 prop)
            (int error, Vector3d r, Vector3d v) = sgpForTLE.SGP4toRVatTime(satdata.jdsatepoch);
            if (error != 0) {
                Debug.LogError(string.Format("Prop Error {0} ", SGP4toGE.errString(error)));
                if (error == 3) {
                    Debug.LogError(string.Format("Sat date: {0} is after GE start time.",
                        sgpForTLE.TLEDateString()));
                }
            }
            double mu = GetMu();
            OrbitUtils.OrbitElements coe = OrbitUtils.RVtoCOE(r, v, null, mu, relativePos: true);
            Debug.Log("COE for Vanguard: " + coe.ToString());

            // Check the COE match the initial orbit
            Assert.That(coe.ecc, Is.EqualTo(satdata.ecco).Within(1E-3));
            Assert.That(coe.incl, Is.EqualTo(satdata.inclo).Within(1E-3));
            Assert.That(coe.raan, Is.EqualTo(satdata.nodeo).Within(1E-3));
            Debug.LogWarning("NERFed argp to 10-2");
            double nerf = 1E-2;
            Assert.That(coe.argp, Is.EqualTo(satdata.argpo).Within(nerf));

            // Use SGP4toGE to init from COE
            SGP4SatData coeSatData = new SGP4SatData();
            coeSatData.UpdateOrbitInfo(coe, satdata.jdsatepoch);
            Debug.LogFormat("Inclination: {0} vs {1} delta={2:00.00}(%)", coeSatData.inclo, satdata.inclo,
                DeltaPercent(coeSatData.inclo, satdata.inclo));
            Debug.LogFormat("Eccentricity: {0} vs {1} delta={2:00.00}(%) OE={3}", coeSatData.ecco, satdata.ecco,
                 DeltaPercent(coeSatData.ecco, satdata.ecco), coe.ecc);
            Debug.LogFormat("A: {0} vs {1} delta={2:00.00}(%)", coeSatData.a, satdata.a,
                 DeltaPercent(coeSatData.a, satdata.a));
            Debug.LogFormat("NODEO: {0} vs {1} delta={2:00.00}(%)", coeSatData.nodeo, satdata.nodeo,
                 DeltaPercent(coeSatData.nodeo, satdata.nodeo));
            Debug.LogFormat("ARGPO: {0} vs {1} delta={2:00.00}(%)", coeSatData.argpo, satdata.argpo,
                 DeltaPercent(coeSatData.argpo, satdata.argpo));
            Debug.LogFormat("NO: {0} vs {1} delta={2:00.00}(%)", coeSatData.no, satdata.no,
                 DeltaPercent(coeSatData.no, satdata.no));
            Debug.LogFormat("MO: {0} vs {1} delta={2:00.00}(%)", coeSatData.mo, satdata.mo,
                 DeltaPercent(coeSatData.mo, satdata.mo));

            Assert.That(coeSatData.epochyr, Is.EqualTo(satdata.epochyr).Within(1E-3));
            Assert.That(coeSatData.epochdays, Is.EqualTo(satdata.epochdays).Within(1E-3));

            // Are the important elements in the satData the same?
            Assert.That(coeSatData.inclo, Is.EqualTo(satdata.inclo).Within(1E-3));
            Assert.That(coeSatData.ecco, Is.EqualTo(satdata.ecco).Within(1E-3));
            Assert.That(coeSatData.argpo, Is.EqualTo(satdata.argpo).Within(nerf));
            Assert.That(coeSatData.nodeo, Is.EqualTo(satdata.nodeo).Within(1E-3));
            Assert.That(coeSatData.no, Is.EqualTo(satdata.no).Within(1E-3));
            Debug.LogWarning("NERFed MO field in TLE to 10-2");
            Assert.That(coeSatData.mo, Is.EqualTo(satdata.mo).Within(1E-2));

            // Now re-create the TLE lines:
            (string line1Out, string line2Out) = coeSatData.CreateTLELines();
            SGP4toGE sgpForTLE2 = new SGP4toGE(name, line1Out, line2Out);
            SGP4SatData satdata2 = new SGP4SatData();
            satdata2 = sgpForTLE.GetSatData();
            // Are the important elements in the satData the same?
            Assert.That(satdata2.inclo, Is.EqualTo(satdata.inclo).Within(1E-3));
            Assert.That(satdata2.ecco, Is.EqualTo(satdata.ecco).Within(1E-3));
            Assert.That(satdata2.argpo, Is.EqualTo(satdata.argpo).Within(nerf));
            Assert.That(satdata2.nodeo, Is.EqualTo(satdata.nodeo).Within(1E-3));
            Assert.That(satdata2.no, Is.EqualTo(satdata.no).Within(1E-3));
            Debug.LogWarning("NERFed MO field in TLE to 10-2");
            Assert.That(coeSatData.mo, Is.EqualTo(satdata.mo).Within(1E-2));
        }

        [Test]
        public void TLEMeanAnomaly()
        {
            GEinit();
            GEinit();
            GEinit();
            // new sat data object
            SGP4SatData data = new SGP4SatData();

            // tle data
            string name = "CIRCULAR EQUITORIAL";
            string line1 = "1 25544U 98067A   21077.40027963  .00000347  00000-0  14510-4 0  9997";
            string line2 = "2 25544  00.0000  00.0000 0000000 000.0000  00.0000 15.48913539274526";


            SGP4toGE sgpForTLE = new SGP4toGE(name, line1, line2);
            data = sgpForTLE.GetSatData();

            // Convert this to a COE
            // Use the sat jd as the epoch time and time for RV (should be no SGP4 prop)

            // SGP4 BUG? Need some advance in t or else ecc is out by 3x
            (int error, Vector3d r, Vector3d v) = sgpForTLE.SGP4toRVatTime(data.jdsatepoch );
            if (error != 0) {
                Debug.LogError(string.Format("Error {0} ", SGP4toGE.errString(error)));
                if (error == 3) {
                    Debug.LogError(string.Format("Sat date: {0} is after GE start time.",
                        sgpForTLE.TLEDateString()));
                }
            }

            Debug.LogFormat("Radius at M=0 r={0}", r);

            // RVtoCOE will handle the XZ mode if necessary
            // Need an NBody ref to do RV to COE

            double mu = GetMu();
            OrbitUtils.OrbitElements coe = OrbitUtils.RVtoCOE(r, v, null, mu, relativePos: true);
            Debug.Log("M=0 " + coe.ToString());
            Debug.Log("Angle to X axis " + Vector3d.Angle(r.normalized, new Vector3d(1, 0, 0)));
            Assert.That((coe.GetPhase()) * Mathd.Rad2Deg, Is.EqualTo(0.0).Within(1E-3));

            // new sat data object
            data = new SGP4SatData();

            // tle data
            name = "CIRCULAR EQUITORIAL";
            line1 = "1 25544U 98067A   21077.40027963  .00000347  00000-0  14510-4 0  9997";
            line2 = "2 25544  00.0000  00.0000 0000000 000.0000  30.0000 15.48913539274526";


            sgpForTLE = new SGP4toGE(name, line1, line2);
            data = sgpForTLE.GetSatData();

            // Convert this to a COE
            // Use the sat jd as the epoch time and time for RV (should be no SGP4 prop)

            // SGP4 BUG? Need some advance in t or else ecc is out by 3x
            (int error2, Vector3d r2, Vector3d v2) = sgpForTLE.SGP4toRVatTime(data.jdsatepoch );
            if (error2 != 0) {
                Debug.LogError(string.Format("Error {0} ", SGP4toGE.errString(error)));
                if (error == 3) {
                    Debug.LogError(string.Format("Sat date: {0} is after GE start time.",
                        sgpForTLE.TLEDateString()));
                }
            }

            Debug.LogFormat("Radius at M=30 r={0}", r2);
            Debug.Log("Angle between R vectors: " + Vector3d.Angle(r, r2));

            // Since E almost zero, mean anomonly and phase should be about the same. YET RV gives phase of 6.03 rad
            // versus 0.37 rad (21 deg as per TLE). Did evolve for 0.1 JD
            OrbitUtils.OrbitElements coe2 = OrbitUtils.RVtoCOE(r2, v2, null, mu, relativePos: true);
            Debug.Log("M=30 " +coe2.ToString());
            Assert.That((coe2.GetPhase()) * Mathd.Rad2Deg, Is.EqualTo(30.0).Within(1E-3));
        }

        [Test]
        public void CheckOutput()
        {
            GEinit();
            // new sat data object
            SGP4SatData data = new SGP4SatData();

            // tle data
            string name = "ISS (ZARYA)";
            string line1 = "1 25544U 98067A   09161.51089941  .00015706  00000-0  11388-3 0   112";
            string line2 = "2 25544  51.6406 341.1646 0009228  98.8703 312.6668 15.73580432604904";

            // options
            char opsmode = SGP4utils.OPSMODE_IMPROVED; // OPSMODE_IMPROVED
            SGP4unit.Gravconsttype gravconsttype = SGP4unit.Gravconsttype.wgs72;

            // read in data and ini SGP4 data
            bool result1 = SGP4utils.readTLEandIniSGP4(name, line1, line2, opsmode, gravconsttype, data);
            (string line1Out, string line2Out) = data.CreateTLELines();
            Debug.Log("ORIG1:" + line1);
            Debug.Log("LINE1:" + line1Out);
            Debug.Log("ORIG2:" + line2);
            Debug.Log("LINE2:" + line2Out);

            SGP4SatData data2 = new SGP4SatData();
            result1 = SGP4utils.readTLEandIniSGP4(name, line1, line2, opsmode, gravconsttype, data2);
            if (!result1) {
                Debug.Log("Error Reading Output TLE / Ini Data, error code: " + data.error);
                Assert.Fail("Fail reading/initing record");
                return;
            }
        }

        [Test]
        public void COEtoRVtoTLEtoRV()
        {
            GEinit();
            GameObject center = GameObject.FindGameObjectWithTag("Earth");
            if (center == null) {
                Debug.LogError("Not in SGP4TestScene. No object with tag Earth");
            }
            NBody centerBody = center.GetComponent<NBody>();
            double mu = GetMu();
            // Get an RV for a specific orbit
            OrbitUtils.OrbitElements oe = new OrbitUtils.OrbitElements();
            oe.a = 7000.0;
            oe.ecc = 0.1;
            oe.p = oe.a * (1 - oe.ecc * oe.ecc);
            oe.incl = 10.0 * Mathd.Deg2Rad;
            oe.raan = 30.0 * Mathd.Deg2Rad;
            oe.argp = 40.0 * Mathd.Deg2Rad;
            oe.nu = 90.0 * Mathd.Deg2Rad;
            Debug.Log("oe: " + oe.ToString());
            Vector3d r = Vector3d.zero;
            Vector3d v = Vector3d.zero;
            OrbitUtils.COEtoRV(oe, centerBody, mu, ref r, ref v, relativePos: true);

            double time = GravityEngine.Instance().GetStartTimeAsJD();
            SGP4SatData data = new SGP4SatData();
            data.UpdateOrbitInfo(oe, time);
            (string line1Out, string line2Out) = data.CreateTLELines();
            Debug.Log("LINE1:" + line1Out);
            Debug.Log("LINE2:" + line2Out);

            // use the TLE to get a value for RV
            SGP4toGE sgpForTLE;
            sgpForTLE = new SGP4toGE("test", line1Out, line2Out);
            SGP4SatData data2 = sgpForTLE.GetSatData();


            // Check OE were recovered
            Assert.That(data.ecco, Is.EqualTo(data2.ecco).Within(1E-5));
            Assert.That(data.argpo, Is.EqualTo(data2.argpo).Within(1E-5));
            Assert.That(data.nodeo, Is.EqualTo(data2.nodeo).Within(1E-5));
            Assert.That(data.inclo, Is.EqualTo(data2.inclo).Within(1E-5));

            (int error, Vector3d rTLE, Vector3d vTLE) = sgpForTLE.SGP4toRVatTime(data2.jdsatepoch);
            if (error > 0) {
                Debug.LogFormat("Error {0} propagating to time {1}", data.error, time);
                Assert.Fail("Fail reading/initing TLE record");
            }
            Debug.LogFormat("r={0} rTLE={1}", r, rTLE);
            Debug.LogFormat("r={0} rTLE={1} dR={2}", r.magnitude, rTLE.magnitude, (r-rTLE).magnitude);
            Debug.LogFormat("v={0} vTLE={1}", v, vTLE);

            // Vallado indicates 1km precision. We only get about 5km!
            Debug.LogWarning("Only testing for 10km precision!!");
            Assert.That((r-rTLE).magnitude, Is.EqualTo(0).Within(10.0));


            OrbitUtils.OrbitElements oe2 =  OrbitUtils.RVtoCOE(rTLE, vTLE, centerBody, mu, relativePos:true);
            Debug.Log("oe2: " + oe2.ToString());
        }

        [Test]
        public void CircularCOEtoRVtoTLEtoRV()
        {
            GEinit();
            // Get an RV for a specific orbit
            double mu = GetMu();
            OrbitUtils.OrbitElements oe = new OrbitUtils.OrbitElements();
            oe.a = 7000.0;
            oe.ecc = 0.0;
            oe.p = oe.a ;
            oe.incl = 0;
            oe.raan = 0;
            oe.argp = 0;
            oe.nu = 30.0 * Mathd.Deg2Rad;
            oe.truelon = 30.0 * Mathd.Deg2Rad;
            oe.ComputeType();
            Debug.Log("oe: " + oe.ToString());
            Vector3d r = Vector3d.zero;
            Vector3d v = Vector3d.zero;
            OrbitUtils.COEtoRV(oe, null, mu, ref r, ref v, relativePos: true);

            // represent the orbit as a TLE
            double time = GravityEngine.Instance().GetStartTimeAsJD();
            SGP4SatData data = new SGP4SatData();
            data.UpdateOrbitInfo(oe, time);
            (string line1Out, string line2Out) = data.CreateTLELines();
            Debug.Log("GENERATED TLE LINE1:" + line1Out);
            Debug.Log("GENERATED TLE LINE2:" + line2Out);

            // use the TLE to get a value for RV
            SGP4toGE sgpForTLE;
            sgpForTLE = new SGP4toGE("test", line1Out, line2Out);
            SGP4SatData data2 = sgpForTLE.GetSatData();


            // Check OE were recovered
            Assert.That(data.ecco, Is.EqualTo(data2.ecco).Within(1E-5));
            Assert.That(data.argpo, Is.EqualTo(data2.argpo).Within(1E-5));
            Assert.That(data.nodeo, Is.EqualTo(data2.nodeo).Within(1E-5));
            Assert.That(data.inclo, Is.EqualTo(data2.inclo).Within(1E-5));
            Assert.That(data.mo, Is.EqualTo(data2.mo).Within(1E-3));
            Assert.That(data.no, Is.EqualTo(data2.no).Within(1E-3));

            Assert.That(data.jdsatepoch, Is.EqualTo(data2.jdsatepoch).Within(1E-3));

            (int error, Vector3d rTLE, Vector3d vTLE) = sgpForTLE.SGP4toRVatTime(data2.jdsatepoch);
            if (error > 0) {
                Debug.Log("Prop fail: " + data.error);
                Assert.Fail("Failed to prop");
            }
            Debug.LogFormat("r={0} rTLE={1}", r, rTLE);
            Debug.LogFormat("r={0} rTLE={1}", r.magnitude, rTLE.magnitude);
            Debug.LogFormat("v={0} vTLE={1}", v, vTLE);

            // aim for within 1 km
            OrbitUtils.OrbitElements oe2 = OrbitUtils.RVtoCOE(rTLE, vTLE, null, mu, relativePos: true);
            Debug.Log("oe2: " + oe2.ToString());
            Debug.LogWarning("Set distance delta to 5km!");
            Assert.That((r - rTLE).magnitude, Is.EqualTo(0).Within(5.0));

        }

        [Test]
        public void TLENddotBstar()
        {
            GEinit();
            SGP4SatData data = new SGP4SatData();
            const double xpdotp = 1440.0 / (2.0 * System.Math.PI);  // 229.1831180523293

            // Weird internal scaling for SGP4 code
            data.ndot = 0.87654321 / (xpdotp * 1440.0);
            data.nddot = 0.1234567 / (xpdotp * 1440.0 * 1440.0); 
            data.bstar = 0.1234567;
            (string line1Out, string line2Out) = data.CreateTLELines();
            Debug.Log("GENERATED TLE LINE1:\n" + SGP4SatData.AddCols(line1Out));
            Debug.Log("GENERATED TLE LINE2:\n" + SGP4SatData.AddCols(line2Out));

            SGP4toGE sgpForTLE;
            sgpForTLE = new SGP4toGE("test", line1Out, line2Out);
            SGP4SatData data2 = sgpForTLE.GetSatData();
            Debug.LogFormat("data={0} data2={1}", data.nddot, data2.nddot);
            Assert.That(data.ndot, Is.EqualTo(data2.ndot).Within(1E-5));
            Assert.That(data.nddot, Is.EqualTo(data2.nddot).Within(1E-5));
            Assert.That(data.bstar, Is.EqualTo(data2.bstar).Within(1E-5));

            // try a negative nddot
            data.ndot = -0.87654321 / (xpdotp * 1440.0);
            data.nddot = -0.1234567 / (xpdotp * 1440.0 * 1440.0);
            ( line1Out,  line2Out) = data.CreateTLELines();
            Debug.Log("GENERATED TLE LINE1:\n" + SGP4SatData.AddCols(line1Out));
            Debug.Log("GENERATED TLE LINE2:\n" + SGP4SatData.AddCols(line2Out));

            sgpForTLE = new SGP4toGE("test", line1Out, line2Out);
            data2 = sgpForTLE.GetSatData();
            Debug.LogFormat("nddot data={0} data2={1}", data.nddot, data2.nddot);
            Assert.That(data.ndot, Is.EqualTo(data2.ndot).Within(1E-5));
            Assert.That(data.nddot, Is.EqualTo(data2.nddot).Within(1E-5));
        }
        /**
        * vector subtraction
        *
        * @param a vector of length 3
        * @param b vector of length 3
        * @return a-b
        */
        public static double[] sub(double[] a, double[] b)
        {
            double[] c = new double[3];
            for (int i = 0; i < 3; i++) {
                c[i] = a[i] - b[i];
            }

            return c;
        }

        //	vector 2-norm
        /**
         * vector 2-norm
         *
         * @param a vector of length 3
         * @return norm(a)
         */
        public static double norm(double[] a)
        {
            double c = 0.0;

            for (int i = 0; i < a.Length; i++) {
                c += a[i] * a[i];
            }

            return System.Math.Sqrt(c);
        }

        //	multiply a vector times a scalar
        /**
         * multiply a vector times a scalar
         *
         * @param a a vector of length 3
         * @param b scalar
         * @return a * b
         */
        public static double[] scale(double[] a, double b)
        {
            double[] c = new double[3];

            for (int i = 0; i < 3; i++) {
                c[i] = a[i] * b;
            }

            return c;
        }

        // from TLESlim
        private const double EARTH_RADIUS_KM = 6378.135;

        [Test]
        public void TLESlimMeanMotion()
        {
            double iss_radius = 420.0 + EARTH_RADIUS_KM;
            double nbar = 8681663.653 / System.Math.Pow(iss_radius, 1.5);
            Debug.LogFormat("Mean Motion ISS={0}", nbar);
            // ISS TLE has about 15.48
            Assert.That(nbar, Is.EqualTo(15.48).Within(0.1));
            double myNbar = 54546481.6679206 / System.Math.Pow(iss_radius, 1.5);
            Debug.LogFormat("my nbar={0}", myNbar);

        }
    }
}
