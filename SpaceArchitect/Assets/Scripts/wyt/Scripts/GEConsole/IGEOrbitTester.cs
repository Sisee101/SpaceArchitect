using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public interface IGEOrbitTester {

    bool IsDone();

    string GetSummary();

    void SetTestRange(int from, int to);

    void SetModeAll();

    void StartTesting();
    
}
