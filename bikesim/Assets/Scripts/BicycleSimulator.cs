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
    // parameters from Meijaard et al. 2007
    private const double g = 9.81;
    private const double M_phiphi = 80.81722;
    private const double M_phidelta = 2.31941332208709;
    private const double M_deltaphi = M_phidelta;
    private const double M_deltadelta = 0.29784188199686;
    private const double C1_phiphi = 0;
    private const double C1_phidelta = 33.86641391492494;
    private const double C1_deltaphi = -0.85035641456978;
    private const double C1_deltadelta = 1.68540397397560;
    private const double K0_phiphi = -80.95;
    private const double K0_phidelta = -2.59951685249872;
    private const double K0_deltaphi = K0_phidelta;
    private const double K0_deltadelta = -0.80329488458618;
    private const double K2_phiphi = 0;
    private const double K2_phidelta = 76.59734589573222;
    private const double K2_deltaphi = 0;
    private const double K2_deltadelta = 2.65431523794604;

    public const double steerAxisTilt = Math.PI/10; // rad
    public const double trail = 0.08; // m
    public const double wheelbase = 1.02; // m
    public const double rR = 0.3; // m

    // bicycle _state
    private bool _valid; // if _state is valid
    private Sensor _lastSensor;
    private State _state;
    private double _feedbackTorque;
    private Matrix<double> _MM;
    private Matrix<double> _C1;
    private Matrix<double> _K0;
    private Matrix<double> _K2;

    private double _v;
    private Matrix<double> _Cv;
    private Matrix<double> _Kv;
    private Vector<double> _qd;

    private UdpSensor _sensor;
    private UdpActuator _actuator;

    public BicycleSimulator() {
        _valid = true;
        _lastSensor = new Sensor();
        _state = new State();
        _sensor = new UdpSensor();
        _sensor.Start();
        _actuator = new UdpActuator();
        _actuator.Start();
        _feedbackTorque = 0.0;

        // matrix construction uses column major order
        _MM = new DenseMatrix(2, 2, new double[] {
            M_phiphi, M_deltaphi,
            M_phidelta, M_deltadelta,
        });
        _C1 = new DenseMatrix(2, 2, new double[] {
            C1_phiphi, C1_deltaphi,
            C1_phidelta, C1_deltadelta,
        });
        _K0 = new DenseMatrix(2, 2, new double[] {
            K0_phiphi, K0_deltaphi,
            K0_phidelta, K0_deltadelta,
        });
        _K2 = new DenseMatrix(2, 2, new double[] {
            K2_phiphi, K2_deltaphi,
            K2_phidelta, K2_deltadelta,
        });
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
        _v = -_lastSensor.wheelRate * rR;
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
        return v*_C1;
    }

    private Matrix<double> K(double v) {
        return g*_K0 + v*v*_K2;
    }

    private void IntegrateState() {
        IntegratorFunction f = delegate(double t, Vector<double> y) {
            Vector<double> q = new DenseVector(
                    new double[] {y[0], y[1], y[2], y[3]});
            _qd = A*q;

            return new DenseVector(new double[] {
                    _qd[0],
                    0, // set steer accel to zero
                    _qd[2],
                    0, // set steer rate to zero as we use the handlebar dynamics
                    _v*y[3] + trail*y[1]*Math.Cos(steerAxisTilt)/wheelbase,
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
        _feedbackTorque = -(_MM[1, 0]*_qd[0] +
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
            Matrix<double> A0 = -_MM.Solve(_Cv.Append(_Kv));
            return A0.Stack(A1);
        }
    }

    public Vector<double> B {
        get {
            Vector<double> B0 = _MM.Solve(new DenseVector(new double[] {0, 1}));
            return new DenseVector(new double[] {B0[0], B0[1], 0, 0});
        }
    }
}
