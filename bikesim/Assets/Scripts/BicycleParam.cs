using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;


public abstract class BicycleParam {
    public virtual double g { get { return 9.81; }}

    public abstract Matrix<double> MM { get; }
    public abstract Matrix<double> C1 { get; }
    public abstract Matrix<double> K0 { get; }
    public abstract Matrix<double> K2 { get; }

    public abstract double steerAxisTilt { get; }
    public abstract double trail { get; }
    public abstract double wheelbase { get; }
    public abstract double rearRadius { get; }
}

public class BenchmarkParam : BicycleParam {
    // parameters from Meijaard et al. 2007
    private const double _m_phiphi = 80.81722;
    private const double _m_phidelta = 2.31941332208709;
    private const double _m_deltaphi = _m_phidelta;
    private const double _m_deltadelta = 0.29784188199686;
    private const double _c1_phiphi = 0;
    private const double _c1_phidelta = 33.86641391492494;
    private const double _c1_deltaphi = -0.85035641456978;
    private const double _c1_deltadelta = 1.68540397397560;
    private const double _k0_phiphi = -80.95;
    private const double _k0_phidelta = -2.59951685249872;
    private const double _k0_deltaphi = _k0_phidelta;
    private const double _k0_deltadelta = -0.80329488458618;
    private const double _k2_phiphi = 0;
    private const double _k2_phidelta = 76.59734589573222;
    private const double _k2_deltaphi = 0;
    private const double _k2_deltadelta = 2.65431523794604;

    private Matrix<double> _mm;
    private Matrix<double> _c1;
    private Matrix<double> _k0;
    private Matrix<double> _k2;

    public BenchmarkParam() {
        // matrix construction uses column major order
        _mm = new DenseMatrix(2, 2, new double[] {
                _m_phiphi, _m_deltaphi,
                _m_phidelta, _m_deltadelta,
        });
        _c1 = new DenseMatrix(2, 2, new double[] {
                _c1_phiphi, _c1_deltaphi,
                _c1_phidelta, _c1_deltadelta,
        });
        _k0 = new DenseMatrix(2, 2, new double[] {
                _k0_phiphi, _k0_deltaphi,
                _k0_phidelta, _k0_deltadelta,
        });
        _k2 = new DenseMatrix(2, 2, new double[] {
                _k2_phiphi, _k2_deltaphi,
                _k2_phidelta, _k2_deltadelta,
        });
    }

    public override Matrix<double> MM { get { return _mm; } }
    public override Matrix<double> C1 { get { return _c1; } }
    public override Matrix<double> K0 { get { return _k0; } }
    public override Matrix<double> K2 { get { return _k2; } }

    public override double steerAxisTilt { get { return Math.PI/10; } }
    public override double trail { get { return 0.08; } }
    public override double wheelbase { get { return 1.02; } }
    public override double rearRadius { get { return 0.3; } }
}
