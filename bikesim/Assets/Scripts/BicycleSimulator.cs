using System;
using System.IO;
using System.Threading;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;


public class State {
    public double leanRate, steerRate, lean, steer, yaw, x, y, wheelAngle;
    public State() : this (0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0) { }
    public State(double phid, double deltad, double phi, double delta,
                 double psi, double x, double y, double thetaR) {
        leanRate = phid;
        steerRate = deltad;
        lean = phi;
        steer = delta;
        yaw = psi;
        this.x = x;
        this.y = y;
        wheelAngle = thetaR;
    }
    public Vector<double> vector {
        get {
            return new DenseVector(new double[] {leanRate, steerRate, lean,
                    steer, yaw, x, y, wheelAngle});
        }
        set {
            leanRate = value[0];
            steerRate = value[1];
            lean = value[2];
            steer = value[3];
            yaw = value[4];
            x = value[5];
            y = value[6];
            wheelAngle = value[7];
        }
    }
}

// This class implements the bicycle simulator equations of motion as described
// in Schwab, Recuero 2013.
public class BicycleSimulator {
    private const int _sim_period_ms = 20;
    private const int _sim_timeout_ms = 1000;

    private Sensor _lastSensor;
    private State _state;
    private double _feedbackTorque;
    private BicycleParam _param;

    private double _v;
    private Matrix<double> _Cv;
    private Matrix<double> _Kv;
    private Vector<double> _qd;

    private UdpSensor _sensor;
    private UdpActuator _actuator;
    private Thread _thread;
    private bool _shouldTerminate;
    private System.Diagnostics.Stopwatch _stopwatch;
    private long _timestamp_ms;

    public BicycleSimulator(BicycleParam param) {
        _param = param;
        _feedbackTorque = 0.0;

        _thread = new Thread(new ThreadStart(ThreadFunction));
        _thread.Name = "simulation_thread";
        _shouldTerminate = false;
        _stopwatch = new System.Diagnostics.Stopwatch();

        _lastSensor = new Sensor();
        _state = new State();
        _sensor = new UdpSensor(_stopwatch);
        _actuator = new UdpActuator(_stopwatch);
    }

    public void Start() {
        UnityEngine.Debug.Log(String.Format(
                    "starting simulation thread"));
        _stopwatch.Start();
        _actuator.Start();
        _sensor.Start();
        _thread.Start();
        _timestamp_ms = 0;
    }

    public void Stop() {
        _shouldTerminate = true;
        // wait for thread to stop
        while((_thread.ThreadState &
               (ThreadState.Stopped | ThreadState.Unstarted)) == 0 );
        _sensor.Stop();
        _actuator.Stop();
        _stopwatch.Reset();
    }

    public double feedbackTorque {
        get { return _feedbackTorque; }
    }

    public double wheelRate {
        get { return _lastSensor.wheelRate; }
    }

    public double elapsedMilliseconds {
        get { return _timestamp_ms; }
    }

    public State state {
        get { return _state; }
    }

    public Matrix<double> A {
        get {
            Matrix<double> eye2 = SparseMatrix.CreateIdentity(2);
            Matrix<double> z2 = new SparseMatrix(2);
            Matrix<double> A1 = eye2.Append(z2);
            Matrix<double> A0 = -_param.MM.Solve(_Cv.Append(_Kv));
            return A0.Stack(A1);
        }
    }

    public Vector<double> B {
        get {
            Vector<double> B0 = _param.MM.Solve(
                    new DenseVector(new double[] {0, 1}));
            return new DenseVector(new double[] {B0[0], B0[1], 0, 0});
        }
    }

    private Matrix<double> C(double v) {
        return v*_param.C1;
    }

    private Matrix<double> K(double v) {
        return _param.g*_param.K0 + v*v*_param.K2;
    }

    private void UpdateVParameters(double wheelRate) {
        _v = -(wheelRate * _param.rearRadius);
        _Kv = K(_v);
        _Cv = C(_v);
    }

    private void ThreadFunction() {
        AutoResetEvent autoEvent = new AutoResetEvent(false);
        Timer simTimer = new Timer(ThreadCallback, autoEvent, _sim_period_ms,
                _sim_period_ms);
        while (!_shouldTerminate) {
            autoEvent.WaitOne(_sim_timeout_ms, false);
        }
        simTimer.Dispose();
    }

    private void ThreadCallback(object stateInfo) {
        Simulate();
        AutoResetEvent ae = (AutoResetEvent)stateInfo;
        ae.Set();
    }

    private void Simulate() {
        UpdateSensor();
        IntegrateState();
        _feedbackTorque = EstimateFeedbackTorque();
        _actuator.SendTorque(_feedbackTorque, _state);
    }

    private void UpdateSensor() {
        _lastSensor = _sensor.sensor;
        UpdateVParameters(_lastSensor.wheelRate);
    }

    private void IntegrateState() {
        IntegratorFunction f = delegate(double t, Vector<double> y) {
            Vector<double> q = new DenseVector(
                    new double[] {y[0], y[1], y[2], y[3]});
            _qd = A*q;

            return new DenseVector(new double[] {
                    _qd[0],
                    0, // steer accel is zero
                    _qd[2],
                    0, // steer rate is zero as we use the handlebar dynamics
                    (_v*y[3] +
                     (_param.trail*y[1]*
                      Math.Cos(_param.steerAxisTilt)/_param.wheelbase)),
                    _v*Math.Cos(y[4]),
                    _v*Math.Sin(y[4]),
                    _lastSensor.wheelRate});
        };

        long dt_ms = _lastSensor.timestamp_ms - _timestamp_ms;
        if (dt_ms > 2*_sim_period_ms) {
            // Data not received for a while. Skip integration step.
            dt_ms = 0;
        } else {
            // assume the data was on time and use the nominal time step
            dt_ms = _sim_period_ms;
        }

        // Use the most recent measured handlebar steer angle and rate
        _state.steerRate = _lastSensor.steerRate;
        _state.steer = _lastSensor.steerAngle;
        // Since y[1], y[3] are set to zero, steer states do not change
        _state.vector = Integrator.RungeKutta4(f, _state.vector, 0,
                Convert.ToDouble(dt_ms)/1000);
        _timestamp_ms = _lastSensor.timestamp_ms;
    }


    private double EstimateFeedbackTorque() {
        return -(_param.MM[1, 0]*_qd[0] + // lean accel
                 _Cv[1, 0]*_qd[2] + // lean rate
                 _Cv[1, 1]*_state.steerRate +
                 _Kv[1, 0]*_state.lean +
                 _Kv[1, 1]*_state.steer);
    }
}
