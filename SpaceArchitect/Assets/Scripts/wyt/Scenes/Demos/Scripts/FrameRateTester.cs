using UnityEngine;
using UnityEngine.UI;

public class FrameRateTester : MonoBehaviour {

    public int targetFrameRate = 60;

    public int testStart = 0; 
	public Text frameText;
    public Text resultText;
    public AddDeleteTester addDelete;

    public GameObject prefabNoOP;
    public GameObject prefabOP;

    public string resultFile; 

    const int framesBetweenAdds = 25;

	const float TARGET_RATE = 60f;

	private int frameCount; 
	private int numBodies = 0; 

	private float fps = 60f;
	private int lowFrameCount = 0; 
	const int LOW_FRAME_LIMIT = 40;

    const float FPS_AVERAGE = 0.1f;

    private enum TestState { START, RUNNING, CLEANUP, DONE, STOP};
    private TestState testState = TestState.START;

    private struct TestCase
    {
        public string mode;
        public bool orbitP;
        public bool keplerOpt; 
        public int opInterval;
        public int result; 
    }

    private TestCase[] tests;
    private int testNum = 0;

    private float startTime;

    string results = "";
 
    void Awake()
    {
        Application.targetFrameRate = 300; // AFAP
        tests = new TestCase[] {
            new TestCase {
                mode = "massive",
                opInterval = 1,
                orbitP = true
            },
            new TestCase {
                mode = "massive",
                opInterval = 1,
                orbitP = true
            },
            new TestCase {
                mode = "massive",
                opInterval = 1,
                orbitP = true
            },
            new TestCase {
                mode = "massive",
                opInterval = 2,
                orbitP = true
            },
            new TestCase {
                mode = "massive",
                opInterval = 5,
                orbitP = true
            },
            new TestCase {
                mode = "massless",
                opInterval = 1,
                orbitP = true
            }, // 5
            new TestCase {
                mode = "massless",
                opInterval = 2,
                orbitP = true
            },
            new TestCase {
                mode = "massless",
                opInterval = 5,
                orbitP = true
            },
            new TestCase {
                mode = "kepler",
                opInterval = 1,
                keplerOpt = true,
                orbitP = true
            },
            new TestCase {
                mode = "kepler",
                opInterval = 1,
                keplerOpt = false,
                orbitP = true
            },
            new TestCase {
                mode = "kepler",
                opInterval = 2,
                keplerOpt = false,
                orbitP = true
            }, // 10
            new TestCase {
                mode = "kepler",
                opInterval = 5,
                keplerOpt = false,
                orbitP = true
            },
            new TestCase {
                mode = "massive",
                opInterval = 1,
                keplerOpt = false,
                orbitP = false
            },
            new TestCase {
                mode = "massless",
                opInterval = 1,
                keplerOpt = false,
                orbitP = false
            },
            new TestCase {
                mode = "kepler",
                opInterval = 1,
                keplerOpt = false,
                orbitP = false
            }
        };
        startTime = Time.time;
        testNum = testStart;

        // check the file name is ok
        if (resultFile != null) {
            System.IO.StreamWriter sw = new System.IO.StreamWriter(resultFile);
            sw.Close();
        }
    }

    private string TestInfo()
    {
        return string.Format("{0} op={1} opi={2} k={3} ",
                            tests[testNum].mode,
                            tests[testNum].orbitP,
                            tests[testNum].opInterval,
                            tests[testNum].keplerOpt);
    }

    // Update is called once per frame
    // System will try and keep frame rate at 60 
    // If below frame rate for 10 frames - call it
    void Update() {

        if (Input.GetKeyDown(KeyCode.N)) {
            testState = TestState.CLEANUP;
        } else if (Input.GetKeyDown(KeyCode.F)) {
            // finish
            results += "Aborted";
            testNum = tests.Length;
            testState = TestState.DONE;
        }

        switch (testState) {
            case TestState.START:
                if (Time.time > startTime) {
                    if (tests[testNum].orbitP)
                        addDelete.orbitingPrefab = prefabOP;
                    else
                        addDelete.orbitingPrefab = prefabNoOP;
                    GravityEngine.Instance().orbitPredictorInterval = tests[testNum].opInterval;
                    GravityEngine.Instance().orbitPredictorKeplerOpt = tests[testNum].keplerOpt;
                    numBodies = 0;
                    lowFrameCount = 0;
                    frameCount = 0;
                    fps = 100.0f;
                    testState = TestState.RUNNING;
                }
                break;

            case TestState.RUNNING:
                if (frameCount++ > framesBetweenAdds) {
                    addDelete.AddBody(tests[testNum].mode);
                    frameCount = 0;
                    numBodies++;
                }
                // time average the fps
                fps = FPS_AVERAGE * (1.0f / Time.deltaTime) +( 1.0f - FPS_AVERAGE) * fps;
                frameText.text = string.Format("{0} N={1} FPS={2:00.0} TGT={3}", TestInfo(), numBodies, fps, 
                    targetFrameRate);
                if (fps < (targetFrameRate - 1)) {
                    if (lowFrameCount++ > LOW_FRAME_LIMIT) {
                        results += string.Format("{0} N={1}\n", 
                            TestInfo(),
                            numBodies);
                        resultText.text = results;
                        tests[testNum].result = numBodies;
                        testState = TestState.CLEANUP;
                    }
                } else {
                    lowFrameCount = 0;
                }
                break;

            case TestState.CLEANUP:
                addDelete.RemoveAll(tests[testNum].mode);
                testNum++;
                // allow things to settle before we go again
                if (testNum < tests.Length) {
                    startTime = Time.time + 1.0f;
                    testState = TestState.START;
                } else {
                    testState = TestState.DONE;                  
                }
                break;

            case TestState.DONE:
                results += "DONE";
                resultText.text = results;
                // write results as CSV file
                if (resultFile != null) {
                    System.IO.StreamWriter sw = new System.IO.StreamWriter(resultFile);
                    sw.WriteLine("Mode,OP,OP interval,Kepler Opt,result");
                    for (int i = 0; i < tests.Length; i++) {
                        sw.WriteLine(string.Format("{0},{1},{2},{3},{4}",
                            tests[i].mode,
                            tests[i].orbitP, 
                            tests[i].opInterval,
                            tests[i].keplerOpt,
                            tests[i].result
                            ));
                    }
                    sw.Close();
                }
                testState = TestState.STOP;
                break;

            case TestState.STOP:
                break;
        }
    }
}
