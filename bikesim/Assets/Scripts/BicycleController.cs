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
    public void SetState(pb.BicyclePoseMessage pose) {
        x = pose.x;
        y = pose.y;
        pitch = pose.pitch;
        yaw = pose.yaw;
        lean = pose.roll;
        steer = pose.steer;
        wheelAngle = pose.rear_wheel;
    }
}

public class BicycleController : MonoBehaviour {
    public GameObject rearWheel;
    public GameObject rearFrame;
    public GameObject frontFrame;
    public GameObject frontWheel;
    public Camera camera;
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

    private ushort timestamp;
    private System.Diagnostics.Stopwatch stopwatch;

    private VizState q;
    private SerialThread serial;
    private pb.BicyclePoseMessage pose;
    private bool cameraRoll;

    // Setup the Bicycle Configuration
    void Start () {

        // Set component sizes
        const float wheelWidth = 0.01f;
        const float frameWidth = 0.05f;
        Vector3 v = new Vector3(2*rR, wheelWidth, 2*rR);
        rearWheel.transform.localScale = v;
        v = new Vector3(2*rF, wheelWidth, 2*rF);
        frontWheel.transform.localScale = v;

        // set camera offset from bicycle origin
        camera.transform.localPosition = new Vector3(0.162f, 0.0f, -1.38f);
        cameraRoll = true;

        q = new VizState();
        SetBicycleTransform(q);
        countdownInfo.text = "";

        if (GamePrefs.device != null) {
            serial = new SerialThread(GamePrefs.device, 115200);
        } else {
            serial = new SerialThread("/dev/tty.usbmodem311", 115200);
        }
        stopwatch = new System.Diagnostics.Stopwatch();
        pose = new pb.BicyclePoseMessage();

        serial.Start();
        timestamp = 0;
        stopwatch.Start();
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.R)) {
            serial.Stop();
            Restart(resetCountdownLength);
        } else if (Input.GetKeyDown(KeyCode.S)) {
            serial.Stop();
        } else if (Input.GetKeyDown(KeyCode.V)) {
            cameraRoll = !cameraRoll;
        }

        string gitsha1 = serial.gitsha1;
        if (gitsha1 == null) {
            gitsha1 = "null";
        }

        pose = serial.PopBicyclePose();
        if (pose != null) {
            q.SetState(pose);
            // There is no operand to add between ushort in C#
            const int CH_CFG_ST_RESOLUTION = 10000;
			int dt = (int)pose.timestamp - timestamp; // system clock @ 10 kHz
            if (dt < 0) {
                dt += 0xffff;
            }
            SetBicycleTransform(q);

            stateInfo.text = System.String.Format(
                "x: {0:F3}\ny: {1:F3}\npitch: {2:F3}\nyaw: {3:F3}\nroll: {4:F3}\nsteer: {5:F3}\nwheel: {6:F3}\nv: {7:F3}",
                q.x,
                q.y,
                Mathf.Rad2Deg*((q.pitch + Math.PI) % (2*Math.PI) - Math.PI),
                Mathf.Rad2Deg*((q.yaw + Math.PI) % (2*Math.PI) - Math.PI),
                Mathf.Rad2Deg*((q.lean+ Math.PI) % (2*Math.PI) - Math.PI),
                Mathf.Rad2Deg*((q.steer + Math.PI) % (2*Math.PI) - Math.PI),
                Mathf.Rad2Deg*((q.wheelAngle + Math.PI) % (2*Math.PI) - Math.PI),
                0.0); // TODO: add v to pose message
            sensorInfo.text = System.String.Format(
                "firmware {0}\npose dt:\t\t{1} us\nunity dt:\t\t{2} us\nupdate dt:\t{3} us\ncamera roll: {4}",
                gitsha1,
                (dt * 1000 * 1000 / CH_CFG_ST_RESOLUTION).ToString("D6"),
                (stopwatch.ElapsedTicks * 1000 * 1000 /
                    System.Diagnostics.Stopwatch.Frequency).ToString("D6"),
                "", //pose.computation_time.ToString("D6"), // TODO add computation_time to pose
                cameraRoll);

			timestamp = (ushort)pose.timestamp;
            stopwatch.Reset(); // .NET 2.0 doesn't have Stopwatch.Restart()
            stopwatch.Start();
        }
    }

    IEnumerator countdown(float seconds) {
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
        // the rear frame by modifying the transform of robot bicycle game
        // object.
        // Explicitly apply the yaw and lean rotations.

        //    y and z axes are switched
        transform.localPosition = new Vector3(q.x, 0.0f, -q.y);
        transform.localRotation = Quaternion.Euler(90.0f, 0, 0) *
            Quaternion.Euler(0.0f, 0.0f, -Mathf.Rad2Deg*q.yaw) *
            Quaternion.Euler(-Mathf.Rad2Deg*q.lean, 0.0f, 0.0f);

        // camera roll
        float roll = 270;
        if (!cameraRoll) {
            roll = (roll + Mathf.Rad2Deg*q.lean) % 360.0f; // fails for angles > 360
        }
        camera.transform.localRotation = Quaternion.Euler(
                camera.transform.localEulerAngles.x,
                camera.transform.localEulerAngles.y,
                roll);

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

}
