using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.IO;

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
    
    // visualization parameters
    // Values from Peterson dissertation
    private float rR = 0.3f; // m
    private float rF = 0.35f; // m
    private float cR = 0.9534570696121847f; // m
    private float ls = 0.2676445084476887f; // m
    private float cF = 0.0320714267276193f; // m

    // dependent parameters
    private float headAngle; // rad
    //private float trail; // m
    //private float wheelbase; // m

    // sensor measurements
    private float wheelRate; // rad/s
    private float steerTorque; // N-m

    private VizState q;
    private BicycleSimulator sim;
    private string filename = "test_torque_pulse.txt";
    private bool writeStateSpace;

    // Setup the Bicycle Configuration
    void Start () {
        writeStateSpace = true; // matrices A, B will be written to file once

        // Set component sizes
        const float wheelWidth = 0.01f;
        const float frameWidth = 0.05f;
        Vector3 v = new Vector3(2*rR, wheelWidth, 2*rR);
        rearWheel.transform.localScale = v;
        v = new Vector3(2*rF, wheelWidth, 2*rF);
        frontWheel.transform.localScale = v;
        v = new Vector3(cR, frameWidth, frameWidth);
        rearFrame.transform.localScale = v;
        v = new Vector3(frameWidth, frameWidth, ls);
        frontFrame.transform.localScale = v;

        headAngle = CalculateNominalPitch();

        // sensor measurements
        wheelRate = 0.0f;
        steerTorque = 0.0f;

        q = new VizState();
        q.pitch = headAngle;
        SetBicycleTransform(q);
        sim = new BicycleSimulator();

        using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
        using (StreamWriter sw = new StreamWriter(fs)) {
            sw.WriteLine("time\twheelrate\tsteertorque\tleanrate\tsteerrate\tlean\tsteer");
        }
    }

    void FixedUpdate() {
        wheelRate -= Input.GetAxis("Vertical");
        steerTorque = 10*Input.GetAxis("Horizontal");

        sim.UpdateSteerTorqueWheelRate(steerTorque, wheelRate, Time.deltaTime);
        double T_f = sim.GetFeedbackTorque();

        State s = sim.GetState();
        using (FileStream fs = new FileStream(filename, FileMode.Append, FileAccess.Write))
        using (StreamWriter sw = new StreamWriter(fs)) {
            sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                         Time.time, wheelRate, steerTorque, s.leanRate,
                         s.steerRate, s.lean, s.steer);
        }

        // write the state space matrices A, B when steer torque is first applied
        if (steerTorque != 0 && writeStateSpace) {
            using (FileStream fs = new FileStream("state_matrix.txt", FileMode.Create, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs)) {
                sw.WriteLine(sim.A);
                sw.WriteLine(sim.B);
            }
            writeStateSpace = false;
        }
    }

    void Update() {
        q.SetState(sim.GetState());
        SetConstraintPitch(q);
        
        SetBicycleTransform(q);
        sensorInfo.text = System.String.Format(
            "wheelrate: {0}\nsteertorque: {1}",
            wheelRate, steerTorque);
        stateInfo.text = System.String.Format(
            "x: {0}\ny: {1}\nlean: {2}\nyaw: {3}\nsteer: {4}",
            q.x, q.y, q.lean, q.yaw, q.steer);
    }

    void SetBicycleTransform(VizState q) {
        // Update x and y positions of the rear wheel contact, yaw and lean of the rear frame
        // by modifying the transform of root bicycle game object.
        // Explictly apply the yaw and lean rotations.
        transform.localPosition = new Vector3(q.x, 0.0f, -q.y); // y and z axes are switched
        transform.localRotation = Quaternion.Euler(90.0f, 0, 0) *
            Quaternion.Euler(0.0f, 0.0f, -Mathf.Rad2Deg*q.yaw) *
            Quaternion.Euler(-Mathf.Rad2Deg*q.lean, 0.0f, 0.0f);

        // All wheel and frame local transforms are with respect to the container game or lean frame
        //   Update rear wheel angle
        rearWheel.transform.localRotation = Quaternion.Euler(0.0f, Mathf.Rad2Deg*q.wheelAngle, 0.0f);
        rearWheel.transform.localPosition = new Vector3(0.0f, 0.0f, -rR);

        //   Update pitch of the rear frame
        rearFrame.transform.localRotation = Quaternion.Euler(0.0f, Mathf.Rad2Deg*q.pitch, 0.0f);
        //   Set rear frame origin at rear wheel position, then translate alone the frame axis
        rearFrame.transform.localPosition = rearWheel.transform.localPosition;
        rearFrame.transform.Translate(new Vector3(cR/2, 0.0f, 0.0f), rearFrame.transform);

        //   Update pitch and steer of the front frame
        frontFrame.transform.localRotation = 
            Quaternion.Euler(0.0f, Mathf.Rad2Deg*q.pitch, -Mathf.Rad2Deg*q.steer);
        //   Set front frame origin at rear frame position, then translate alone the frame axes
        frontFrame.transform.localPosition = rearFrame.transform.localPosition;
        frontFrame.transform.Translate(
            new Vector3(cR/2, 0.0f, ls/2), rearFrame.transform);

        //   Update front wheel angle
        frontWheel.transform.localRotation = frontFrame.transform.localRotation*
            Quaternion.Euler(0.0f, Mathf.Rad2Deg*q.wheelAngle, 0.0f);
        frontWheel.transform.localPosition = frontFrame.transform.localPosition;
        frontWheel.transform.Translate(
            new Vector3(cF, 0.0f, ls/2), frontFrame.transform);
    }

    private float CalculateNominalPitch() {
        float theta1 = Mathf.Atan(ls / (cR + cF));
        float dropoutLength = Mathf.Sqrt(Mathf.Pow(cR + cF, 2) + Mathf.Pow(ls, 2));
        float theta2 = Mathf.Asin((rR - rF) / dropoutLength);
        return theta1 - theta2;
    }

    private void SetConstraintPitch(VizState q) {
        Func<double, double> f0 = pitch => f(q.lean, pitch, q.steer);
        Func<double, double> df0 = pitch => df(q.lean, pitch, q.steer);

        q.pitch = System.Convert.ToSingle(
            MathNet.Numerics.RootFinding.NewtonRaphson.FindRootNearGuess(f0, df0,
                q.pitch, 0, Math.PI/2, 1e-10, 100));
    }

    // pitch angle configuration constraint
    private double f(double lean, double pitch, double steer) {
        return (rF*Math.Pow(Math.Cos(lean),
        2)*Math.Pow(Math.Cos(pitch), 2) +
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
}
