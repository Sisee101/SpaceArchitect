using UnityEngine;

/// <summary>
/// Minimal stub implementations for the legacy SGP4 functionality. These exist so that the
/// project can compile and run without the heavy SGP4 dependency. Any runtime use of the API
/// will log warnings to highlight that the full propagator is no longer available.
/// </summary>
namespace SGP4
{
    public static class SGP4unit
    {
        public const double RADIUSEARTHKM = 6378.135;
        public static string LogInternalCOE(SGP4SatData data) => "SGP4 disabled";
    }

    public static class SGP4utils
    {
        public static string EpochDateString(SGP4SatData data) => "SGP4 disabled";
    }

    public class SGP4SatData
    {
        public double argpo;
        public double nodeo;
        public double jdsatepoch;
        public double mo;
        public double mdot;
        public double a;
        public double ecco;
        public double inclo;
        // Legacy field names used in OrbitUniversal
        public double _el;
        public double _xnode;
        public double _argpp;
        public double _xinc;
        public double _am;
        public double _su;

        public const int ECCENTRICITY_ERR_SGP4 = -100;

        public string ErrorString(int code) => $"SGP4 disabled (code {code})";
    }
}

public class SGP4toGE
{
    private readonly SGP4.SGP4SatData satData = new SGP4.SGP4SatData();

    public SGP4toGE(string tleName, string line1, string line2)
    {
        LogUnavailable();
    }

    public SGP4toGE(string name, OrbitUniversal orbit)
    {
        LogUnavailable();
    }

    public SGP4toGE(SGP4toGE other)
    {
        if (other != null)
        {
            satData = new SGP4.SGP4SatData
            {
                argpo = other.satData.argpo,
                nodeo = other.satData.nodeo,
                jdsatepoch = other.satData.jdsatepoch,
                mo = other.satData.mo,
                mdot = other.satData.mdot,
                a = other.satData.a,
                ecco = other.satData.ecco,
                inclo = other.satData.inclo,
                _el = other.satData._el,
                _xnode = other.satData._xnode,
                _argpp = other.satData._argpp,
                _xinc = other.satData._xinc,
                _am = other.satData._am,
                _su = other.satData._su
            };
        }
    }

    public void UpdateSGP4AuxData(OrbitUniversal orbit)
    {
        LogUnavailable();
    }

    public (int error, Vector3d r, Vector3d v) SGP4toRVatTime(double timeJD)
    {
        LogUnavailable();
        return (-1, Vector3d.zero, Vector3d.zero);
    }

    public double ReInitFromRV(Vector3d r, Vector3d v, double time0, double mu)
    {
        LogUnavailable();
        return 0.0;
    }

    public SGP4.SGP4SatData GetSatData()
    {
        return satData;
    }

    public string TLEDateString()
    {
        return "SGP4 disabled";
    }

    public static string errString(int error)
    {
        return $"SGP4 disabled (code {error})";
    }

    private static void LogUnavailable()
    {
#if UNITY_EDITOR
        Debug.unityLogger?.LogWarning("SGP4", "SGP4 functionality is removed in the trimmed build.");
#endif
    }
}

