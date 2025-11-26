

namespace SGP4
{
    // simple test of the SGP4 propagator
    using NUnit.Framework;
    using UnityEngine;

    public class ExportTLE 
    {
        [Test]
        public void Sanity()
        {
            OrbitUtils.OrbitElements oe = new OrbitUtils.OrbitElements();
            oe.p = 100;
            oe.a = 100;
            oe.ecc = 0.0123456;
            oe.raan = 20.12345 * Mathd.Deg2Rad;
            oe.argp = 123.45678 * Mathd.Deg2Rad;
            oe.incl = 10.98765 * Mathd.Deg2Rad;
            oe.nu = 95.12345 * Mathd.Deg2Rad;
            SGP4toGE.TLEAuxInfo auxInfo = new SGP4toGE.TLEAuxInfo("test");
            auxInfo.satelliteNumber = 12345;
            auxInfo.meanMotionD1 = -0.012345678;
            auxInfo.meanMotionD2 = -0.1234E-4;
            auxInfo.bstar = 9.8876E-4;
            auxInfo.elementNumber = 0;
            auxInfo.revNoAtEpoch = 42; 
            (string tle1, string tle2) = SGP4toGE.ExportAsTLE(oe, auxInfo, 2023, 20.34);
            Debug.Log("INspect this manually:");
            Debug.Log(tle1);
            Debug.Log(tle2);
        }

        [Test]
        public void BigNdot()
        {
            OrbitUtils.OrbitElements oe = new OrbitUtils.OrbitElements();
            oe.p = 100;
            oe.a = 100;
            oe.ecc = 0.0123456;
            oe.raan = 20.12345 * Mathd.Deg2Rad;
            oe.argp = 123.45678 * Mathd.Deg2Rad;
            oe.incl = 10.98765 * Mathd.Deg2Rad;
            oe.nu = 95.12345 * Mathd.Deg2Rad;
            SGP4toGE.TLEAuxInfo auxInfo = new SGP4toGE.TLEAuxInfo("test");
            auxInfo.satelliteNumber = 12345;
            auxInfo.meanMotionD1 = -3456.012345678;
            auxInfo.meanMotionD2 = -3456.1234E-4;
            auxInfo.bstar = 9.8876E-4;
            auxInfo.elementNumber = 0;
            auxInfo.revNoAtEpoch = 42;
            (string tle1, string tle2) = SGP4toGE.ExportAsTLE(oe, auxInfo, 2023, 20.34);
            Debug.Log("INspect this manually:");
            Debug.Log(tle1);
            Debug.Log(tle2);
        }

        [Test]
        public void MeanAnomoly()
        {
            // Circular orbit M=nu
            double[] mValues = new double[] { 0, 10, 100, 359.9 };
            foreach (double m in mValues) {
                OrbitUtils.OrbitElements oe = new OrbitUtils.OrbitElements();
                oe.p = 100;
                oe.a = 100;
                oe.ecc = 0.0;
                oe.raan = 0;
                oe.argp = 0;
                oe.incl = 0;
                oe.nu = m * Mathd.Deg2Rad;
                (string tle1, string tle2) = SGP4toGE.ExportAsTLE(oe, new SGP4toGE.TLEAuxInfo("test"), 2023, 20.34);
                string mString = tle2.Substring(43, 8);
                double mFromTle = double.Parse(mString);
                Debug.LogFormat("{0} vs {1}", mString, m);
                Assert.That(m, Is.EqualTo(mFromTle).Within(1E-4));
            }
        }

        double RADIUSEARTHKM = 6378.135;

        [Test]
        public void MeanMotion()
        {
            GravityEngine ge = GravityEngine.Instance();
            ge.xzOrbits = false;
            ge.units = GravityScaler.Units.ORBITAL;
            double[] aValues = new double[] { 7000.0, 8125.0 };

            SGP4toGE.TLEAuxInfo auxInfo = new SGP4toGE.TLEAuxInfo("test");
            auxInfo.satelliteNumber = 12345;
            auxInfo.meanMotionD1 = -0.1234E-5;
            auxInfo.meanMotionD2 = -0.1234E-4;
            auxInfo.bstar = 9.8876E-4;
            auxInfo.elementNumber = 0;
            auxInfo.revNoAtEpoch = 42;

            foreach (double a in aValues) {
                OrbitUtils.OrbitElements oe = new OrbitUtils.OrbitElements();
                oe.p = a * ge.lengthScale;
                oe.a = a * ge.lengthScale;
                oe.ecc = 0.0;
                oe.raan = 0;
                oe.argp = 0;
                oe.incl = 0;
                oe.nu = 0;
                (string tle1, string tle2) = SGP4toGE.ExportAsTLE(oe, auxInfo, 2023, 20.34);
                Debug.Log(tle1);
                Debug.Log(tle2);

                // options
                SGP4SatData data = new SGP4SatData();
                char opsmode = SGP4utils.OPSMODE_IMPROVED; // OPSMODE_IMPROVED
                SGP4unit.Gravconsttype gravconsttype = SGP4unit.Gravconsttype.wgs72;

                // read in data and ini SGP4 data
                bool result1 = SGP4utils.readTLEandIniSGP4("test", tle1, tle2, opsmode, gravconsttype, data);
                if (!result1) {
                    Debug.Log("Error Reading / Ini Data, error code: " + data.error);
                    Assert.Fail("Fail reading/initing record");
                    return;
                }
                Debug.LogFormat("data.am={0} a={1}", data.a * RADIUSEARTHKM, a);
                Assert.That(data.a * RADIUSEARTHKM, Is.EqualTo(a).Within(1E-3));

            }
        }

        [Test]
        public void Checksum()
        {
            string line1 = "1 25544U 98067A   23062.59626272  .00020433  00000+0  36991-3 0  999"; // 4
            string line2 = "2 25544  51.6412 131.5712 0005952  51.3433 101.2629 15.4961180838541"; // 6"
            Assert.That(SGP4toGE.CheckSum(line1), Is.EqualTo(4));
            Assert.That(SGP4toGE.CheckSum(line2), Is.EqualTo(6));
            line1 = "1 43556U 18046C   23062.35527922  .00147797  00000+0  17440-2 0  999"; // 2
            line2 = "2 43556  51.6298 348.3808 0004778 207.3534 152.7208 15.6032730425937"; // 2
            Assert.That(SGP4toGE.CheckSum(line1), Is.EqualTo(2));
            Assert.That(SGP4toGE.CheckSum(line2), Is.EqualTo(2));
        }

        private void GEinit()
        {
            GravityEngine ge = GravityEngine.Instance();
            ge.xzOrbits = false;
        }

        const double MU_EMPIRICAL = 399169.1016 * 100.0; // x100 since 360 sec/game sec

        private double GetMu()
        {
            GameObject earth = GameObject.FindWithTag("Earth");
            if (earth == null) {
                Debug.LogError("Not in SGP4TestScene. No object with tag Earth");
            }
            NBody earthNbody = earth.GetComponent<NBody>();
            // GE has not added Earth, so do mass scaling explicitly
            GravityEngine ge = GravityEngine.Instance();
            double massScale = GravityScaler.UpdateMassScale(GravityScaler.Units.ORBITAL, ge.timeScale, ge.lengthScale);

            double mu = earthNbody.mass * massScale;
            //Debug.LogFormat("mu={0} vs empirical={1} delta={2}(%)", mu, MU_EMPIRICAL,
            //    DeltaPercent(mu, MU_EMPIRICAL));
            return mu;
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

            bool result = SGP4unit.sgp4(data, 0.0, pos, vel); // Evolve for 0 minutes
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
            Vector3d eccVec = (((v.sqrMagnitude - mu / r.magnitude) * r - Vector3d.Dot(r, v) * v)) / mu;
            Debug.LogFormat("raw ecc ={0} e={1}", eccVec, eccVec.magnitude);

            OrbitUtils.OrbitElements coe = OrbitUtils.RVtoCOE(r, v, centerBody, mu, relativePos: true);
            Debug.Log(coe.ToString());
            SGP4toGE.TLEAuxInfo auxInfo = new SGP4toGE.TLEAuxInfo(data);
            (string tle1, string tle2) = SGP4toGE.ExportAsTLE(coe, auxInfo, 2009, 161.51089941);
            Debug.Log(tle1);
            Debug.Log(tle2);
        }
    }
}