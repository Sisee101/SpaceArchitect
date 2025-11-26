using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SolarDateControl : MonoBehaviour {

    public InputField yearText;
    public InputField monthText;
    public InputField dayText;

    public Text currentTime;

    [Tooltip("(optional) If not present then assume the initial time in the text fields is the start time.")]
    public SolarSystem solarSystem;

    public int startYear = 2016;
    public int startMonth = 1;
    public int startDay = 1;

    private bool clearTrail;
    private int clearTrailDelay;

    private System.DateTime startTime; 

    void Start() {
        if (solarSystem != null) {
            System.DateTime dt = SolarUtils.DateForEpoch(solarSystem.GetStartEpochTime());
            yearText.text = dt.Year.ToString();
            monthText.text = dt.Month.ToString();
            dayText.text = dt.Day.ToString();
            startTime = SolarUtils.DateForEpoch(solarSystem.GetStartEpochTime());

        }
        else {
            yearText.text = startYear.ToString();
            monthText.text = startMonth.ToString();
            dayText.text = startDay.ToString();
            startTime = new System.DateTime(startYear, startMonth, startDay);
        }
    }


    public void SetPhysTime(string dummy) {
        int year = int.Parse(yearText.text);
        int month = int.Parse(monthText.text);
        int day = int.Parse(dayText.text);
        if (solarSystem != null) {
            solarSystem.SetTime(year, month, day, 0);
        } else {
            // C# DateTime/TimeSpan to find the number of seconds to add
            System.DateTime newTime = new System.DateTime(year, month, day);
            double secsFromStart = (newTime - startTime).Ticks / (1E7);

            if (secsFromStart < 0) {
                Debug.LogError("Cannot evolve earlier than solar system start time.");
            }
            Debug.LogFormat("Secs from start={0} phys={1} newPhys={2}",
                secsFromStart,
                GravityEngine.Instance().GetPhysicalTimeDouble(),
                GravityScaler.WorldSecsToPhysTime(secsFromStart));
            GravityEngine.Instance().SetPhysicalTime(GravityScaler.WorldSecsToPhysTime(secsFromStart));
        }
        clearTrail = true;
        clearTrailDelay = 3;
    }

    private void ResetTrails() {
        foreach (TrailRenderer tr in (TrailRenderer[])Object.FindObjectsOfType(typeof(TrailRenderer))) {
            tr.Clear();
        }
    }

    void Update() {

        System.DateTime newTime = startTime + GravityScaler.GetTimeSpan(GravityEngine.Instance().GetPhysicalTimeDouble(), GravityScaler.Units.SOLAR);
        currentTime.text =  newTime.ToString("yyyy:MM:dd"); // 24h format

        // ICK! Need to wait until GE moves these objects before can clear the trail
        if (clearTrail) {
            if (clearTrailDelay-- <= 0) {
                ResetTrails();
                clearTrail = false;
            }
        }
    }
}
