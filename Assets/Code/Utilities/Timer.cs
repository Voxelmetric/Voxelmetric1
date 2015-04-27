using UnityEngine;
using System.Collections;

public class Timer {

    float delay;
    float offset;

    public Timer(float delay)
    {
        this.delay = delay;
    }

    public bool isTime()
    {
        float timeSinceLastStep = Time.timeSinceLevelLoad - offset;

        if (timeSinceLastStep < delay)
            return false;

        offset = Time.timeSinceLevelLoad;

        if (timeSinceLastStep > delay * 2)
            return false;

        return true;
    }

}
