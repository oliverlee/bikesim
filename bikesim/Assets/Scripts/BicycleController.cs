using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.IO;
using XInputDotNetPure;


public class VizState {
    public float x, y, pitch, lean, yaw, wheelAngle, steer;
    public VizState(State q) {
        x = System.Convert.ToSingle(q.x);
        y = System.Convert.ToSingle(q.y);
        pitch = 0.0f;
        lean = System.Convert.ToSingle(q.lean);
        yaw = System.Convert.ToSingle(q.yaw);
        wheelAngle = System.Convert.ToSingle(q.wheelAngle);
        steer = System.Convert.ToSingle(q.steer);
    }
    public VizState() {
        x = 0.0f;
        y = 0.0f;
        pitch = 0.0f;
        lean = 0.0f;
        yaw = 0.0f;
        wheelAngle = 0.0f;
        steer = 0.0f;
    }
    public void SetState(State q) {
        x = System.Convert.ToSingle(q.x);
        y = System.Convert.ToSingle(q.y);
        lean = System.Convert.ToSingle(q.lean);
        yaw = System.Convert.ToSingle(q.yaw);
        wheelAngle = System.Convert.ToSingle(q.wheelAngle);
        steer = System.Convert.ToSingle(q.steer);
    }
}

public class BicycleController : MonoBehaviour {
    public GameObject rearWheel;
    public GameObject rearFrame;
    public GameObject frontFrame;
    public GameObject frontWheel;
    public Text sensorInfo;
    public Text stateInfo;
    public Text countdownInfo;
    public float resetCountdownLength;

    // visualization parameters
    // Values from Peterson dissertation
    private float rR = 0.3f; // m
    private float rF = 0.35f; // m
    private float cR = 0.9534570696121847f; // m
    private float ls = 0.2676445084476887f; // m
    private float cF = 0.0320714267276193f; // m

    // dependent parameters
    private float headAngle; // rad

    private VizState q;
    private BicycleSimulator sim;
    private bool stopSim;

    // Setup the Bicycle Configuration
    void Start () {
        stopSim = false;

        // Set component sizes
        const float wheelWidth = 0.01f;
        const float frameWidth = 0.05f;
        Vector3 v = new Vector3(2*rR, wheelWidth, 2*rR);
        rearWheel.transform.localScale = v;
        v = new Vector3(2*rF, wheelWidth, 2*rF);
        frontWheel.transform.localScale = v;

        headAngle = CalculateNominalPitch();

        q = new VizState();
        q.pitch = headAngle;
        SetBicycleTransform(q);
        sim = new BicycleSimulator(new BenchmarkParam());
        sim.Start();
        countdownInfo.text = "";
    }
    
    void Update() {
        if (Input.GetKeyDown(KeyCode.R)) {
            sim.Stop();
            Restart(resetCountdownLength);
        }
        if (stopSim) {
            return;
        }
        q.SetState(sim.state);
        try {
            SetConstraintPitch(q);
        }
        catch (MathNet.Numerics.NonConvergenceException) {
            stopSim = true;
            sim.Stop();
            Restart(resetCountdownLength);
        }

        SetBicycleTransform(q);
        sensorInfo.text = System.String.Format(
            "speed: {0}\nsteertorque: {1}\nsim time: {2}",
            sim.wheelRate * rR * 3.6 * -1, // rad/s -> km/hr
            sim.feedbackTorque,
            sim.elapsedMilliseconds/1000);
        stateInfo.text = System.String.Format(
            "x: {0}\ny: {1}\nlean: {2}\nyaw: {3}\nsteer: {4}",
            q.x, q.y, q.lean, q.yaw, q.steer);
    }
    
    IEnumerator countdown(float seconds) {
        sim.Stop();
        countdownInfo.text = System.String.Format(
            "Restarting in: {0}", seconds);
        float dt = 0.01f; // s
        while (seconds > 0) {
            yield return new WaitForSeconds(dt);
            seconds -= dt;
            countdownInfo.text = System.String.Format(
                "Restarting in: {0:0.00}", seconds);

        }
        countdownInfo.text = "";
        Start();
    }
    
    void Restart(float seconds) {
        StartCoroutine(countdown(seconds));
    }
    
    void SetBicycleTransform(VizState q) {
        // Update x and y positions of the rear wheel contact, yaw and lean of
        // the rear frame by modifying the transform of root bicycle game
        // object.
        // Explictly apply the yaw and lean rotations.

        //    y and z axes are switched
        transform.localPosition = new Vector3(q.x, 0.0f, -q.y);
        transform.localRotation = Quaternion.Euler(90.0f, 0, 0) *
            Quaternion.Euler(0.0f, 0.0f, -Mathf.Rad2Deg*q.yaw) *
            Quaternion.Euler(-Mathf.Rad2Deg*q.lean, 0.0f, 0.0f);

        // All wheel and frame local transforms are with respect to the
        // container game or lean frame
        //   Update rear wheel angle
        rearWheel.transform.localRotation =
            Quaternion.Euler(0.0f, Mathf.Rad2Deg*q.wheelAngle, 0.0f);
        rearWheel.transform.localPosition = new Vector3(0.0f, 0.0f, -rR);

        //   Update pitch of the rear frame
        rearFrame.transform.localRotation =
            Quaternion.Euler(0.0f, Mathf.Rad2Deg*q.pitch, 0.0f);
        //   Set rear frame origin at rear wheel position, then translate alone
        //   the frame axis
        rearFrame.transform.localPosition = rearWheel.transform.localPosition;
        rearFrame.transform.Translate(new Vector3(cR/2, 0.0f, 0.0f),
                rearFrame.transform);

        //   Update pitch and steer of the front frame
        frontFrame.transform.localRotation =
            Quaternion.Euler(0.0f, Mathf.Rad2Deg*q.pitch,
                    -Mathf.Rad2Deg*q.steer);
        //   Set front frame origin at rear frame position, then translate
        //   alone the frame axes
        frontFrame.transform.localPosition = rearFrame.transform.localPosition;
        frontFrame.transform.Translate(
            new Vector3(cR/2, 0.0f, ls/2), rearFrame.transform);

        //   Update front wheel angle
        frontWheel.transform.localRotation =
            frontFrame.transform.localRotation *
            Quaternion.Euler(0.0f, Mathf.Rad2Deg*q.wheelAngle, 0.0f);
        frontWheel.transform.localPosition =
            frontFrame.transform.localPosition;
        frontWheel.transform.Translate(
            new Vector3(cF, 0.0f, ls/2), frontFrame.transform);
    }

    private float CalculateNominalPitch() {
        float theta1 = Mathf.Atan(ls / (cR + cF));
        float dropoutLength =
            Mathf.Sqrt(Mathf.Pow(cR + cF, 2) + Mathf.Pow(ls, 2));
        float theta2 = Mathf.Asin((rR - rF) / dropoutLength);
        return theta1 - theta2;
    }

    private void SetConstraintPitch(VizState q) {
        Func<double, double> f0 = pitch => f(q.lean, pitch, q.steer);
        Func<double, double> df0 = pitch => df(q.lean, pitch, q.steer);

        q.pitch = System.Convert.ToSingle(
            MathNet.Numerics.RootFinding.NewtonRaphson.FindRootNearGuess(f0,
                df0, q.pitch, 0, Math.PI/2, 1e-10, 100));
    }

    // pitch angle configuration constraint
    private double f(double lean, double pitch, double steer) {
        return (rF*Math.Pow(Math.Cos(lean), 2)*Math.Pow(Math.Cos(pitch), 2) +
        (cF*Math.Sqrt(Math.Pow(Math.Sin(lean)*Math.Sin(steer) -
        Math.Sin(pitch)*Math.Cos(lean)*Math.Cos(steer), 2) +
        Math.Pow(Math.Cos(lean), 2)*Math.Pow(Math.Cos(pitch), 2)) +
        rF*(Math.Sin(lean)*Math.Sin(steer) -
        Math.Sin(pitch)*Math.Cos(lean)*Math.Cos(steer)))*(Math.Sin(lean)*Math.Sin(steer)
        - Math.Sin(pitch)*Math.Cos(lean)*Math.Cos(steer)) +
        Math.Sqrt(Math.Pow(Math.Sin(lean)*Math.Sin(steer) -
        Math.Sin(pitch)*Math.Cos(lean)*Math.Cos(steer), 2) +
        Math.Pow(Math.Cos(lean), 2)*Math.Pow(Math.Cos(pitch),
        2))*(-cR*Math.Sin(pitch) + ls*Math.Cos(pitch) -
        rR)*Math.Cos(lean))/Math.Sqrt(Math.Pow(Math.Sin(lean)*Math.Sin(steer)
        - Math.Sin(pitch)*Math.Cos(lean)*Math.Cos(steer), 2) +
        Math.Pow(Math.Cos(lean), 2)*Math.Pow(Math.Cos(pitch), 2));
    }

    // derivative of f wrt to pitch
    private double df(double lean, double pitch, double steer) {
        return -(cF*Math.Cos(pitch)*Math.Cos(steer) +
        cR*Math.Cos(pitch) + ls*Math.Sin(pitch) +
        rF*Math.Sin(lean)*Math.Sin(steer)*Math.Cos(pitch)*Math.Cos(steer)/Math.Sqrt(Math.Pow(Math.Sin(lean),
        2)*Math.Pow(Math.Sin(pitch), 2)*Math.Pow(Math.Sin(steer), 2) +
        Math.Pow(Math.Sin(lean), 2)*Math.Pow(Math.Sin(steer), 2) -
        Math.Pow(Math.Sin(lean), 2) -
        2*Math.Sin(lean)*Math.Sin(pitch)*Math.Sin(steer)*Math.Cos(lean)*Math.Cos(steer)
        - Math.Pow(Math.Sin(pitch), 2)*Math.Pow(Math.Sin(steer), 2) +
        1) + rF*Math.Sin(pitch)*Math.Pow(Math.Sin(steer),
        2)*Math.Cos(lean)*Math.Cos(pitch)/Math.Sqrt(Math.Pow(Math.Sin(lean),
        2)*Math.Pow(Math.Sin(pitch), 2)*Math.Pow(Math.Sin(steer), 2) +
        Math.Pow(Math.Sin(lean), 2)*Math.Pow(Math.Sin(steer), 2) -
        Math.Pow(Math.Sin(lean), 2) -
        2*Math.Sin(lean)*Math.Sin(pitch)*Math.Sin(steer)*Math.Cos(lean)*Math.Cos(steer)
        - Math.Pow(Math.Sin(pitch), 2)*Math.Pow(Math.Sin(steer), 2) +
        1))*Math.Cos(lean);
    }

    public void OnApplicationQuit() {
        sim.Stop();
    }
}
