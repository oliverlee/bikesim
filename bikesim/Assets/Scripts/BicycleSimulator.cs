using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using UnityEngine;


public class Sensor {
    public double steerAngle, steerRate, wheelRate, sampleTime;
    public Sensor() : this(0.0, 0.0, 0.0, 0.0) { }
    public Sensor(double delta, double deltad, double thetad, double dt) {
        steerAngle = delta;
        steerRate = deltad;
        wheelRate = thetad;
        sampleTime = dt;
    }
    public void Update(double delta, double deltad, double thetad, double dt) {
        steerAngle = delta;
        steerRate = deltad;
        wheelRate = thetad;
        sampleTime = dt;
    }
}

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
    private bool _valid; // if _state is valid
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

    public BicycleSimulator(BicycleParam param) {
        _param = param;

        _valid = true;
        _lastSensor = new Sensor();
        _state = new State();
        _sensor = new UdpSensor();
        _actuator = new UdpActuator();
        _feedbackTorque = 0.0;
    }

    public void Start() {
        _actuator.Start();
        _sensor.Start();
    }

    public void Stop() {
        _sensor.Stop();
        _actuator.Stop();
    }

    public void UpdateSteerAngleRateWheelRate(float steerAngle,
            float steerRate, float wheelRate, float samplePeriod) {
        _lastSensor.Update(steerAngle, steerRate, wheelRate, samplePeriod);
        UpdateVParameters();
    }

    public void UpdateNetworkSensor(float dt) {
        _lastSensor = _sensor.sensor;
        _lastSensor.sampleTime = dt;
        UpdateVParameters();
    }

    private void UpdateVParameters() {
        _v = -_lastSensor.wheelRate * _param.rearRadius;
        _Kv = K(_v);
        _Cv = C(_v);
        _valid = false;
    }

    public double GetFeedbackTorque() {
        if (!_valid) {
            Simulate();
        }
        return _feedbackTorque;
    }

    public double GetWheelRate() {
        return _lastSensor.wheelRate;
    }

    public State GetState() {
        if (!_valid) {
            Simulate();
        }
        return _state;
    }

    private void Simulate() {
        IntegrateState();
        EstimateFeedbackTorque();
        _valid = true;
    }

    private Matrix<double> C(double v) {
        return v*_param.C1;
    }

    private Matrix<double> K(double v) {
        return _param.g*_param.K0 + v*v*_param.K2;
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

        // Use the most recent measured handlebar steer angle and rate
        _state.steerRate = _lastSensor.steerRate;
        _state.steer = _lastSensor.steerAngle;
        // Since y[1], y[3] are set to zero, steer states do not change
        _state.vector = Integrator.RungeKutta4(f, _state.vector, 0,
                _lastSensor.sampleTime);
    }

    private void EstimateFeedbackTorque() {
        _feedbackTorque = -(_param.MM[1, 0]*_qd[0] +
                           _Cv[1, 0]*_qd[2] +
                           _Cv[1, 1]*_qd[3] + // TODO: use _state steer rate?
                           _Kv[1, 0]*_state.lean +
                           _Kv[1, 1]*_state.steer);
        _actuator.SetTorque(_feedbackTorque);
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
}
