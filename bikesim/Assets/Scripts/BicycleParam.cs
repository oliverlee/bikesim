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
    //// parameters from Meijaard et al. 2007
    //private const double _m_phiphi = 80.81722;
    //private const double _m_phidelta = 2.31941332208709;
    //private const double _m_deltaphi = _m_phidelta;
    //private const double _m_deltadelta = 0.29784188199686;
    //private const double _c1_phiphi = 0;
    //private const double _c1_phidelta = 33.86641391492494;
    //private const double _c1_deltaphi = -0.85035641456978;
    //private const double _c1_deltadelta = 1.68540397397560;
    //private const double _k0_phiphi = -80.95;
    //private const double _k0_phidelta = -2.59951685249872;
    //private const double _k0_deltaphi = _k0_phidelta;
    //private const double _k0_deltadelta = -0.80329488458618;
    //private const double _k2_phiphi = 0;
    //private const double _k2_phidelta = 76.59734589573222;
    //private const double _k2_deltaphi = 0;
    //private const double _k2_deltadelta = 2.65431523794604;

    //// parameters of bicycle Browser with rider Jason using yeadon model
    //// refer to: A.L. Schwab et al. "Lateral dynamics of a bicycle with a
    //// passive rider model: stability and controllability" and Jason Moore's phd
    //// dissertation.
    ////
    //// http://www.bicycle.tudelft.nl/schwab/Publications/schwab2012lateral.pdf
    //// https://moorepants.github.io/dissertation/physicalparameters.html#yeadon-method
    //private const double _m_phiphi = 102.78260126737564;
    //private const double _m_phidelta = 1.5349474185931016;
    //private const double _m_deltaphi = _m_phidelta;
    //private const double _m_deltadelta = 0.24666219580863546;
    //private const double _c1_phiphi = 0;
    //private const double _c1_phidelta = 26.39273018670496;
    //private const double _c1_deltaphi = -0.4498095401132608;
    //private const double _c1_deltadelta = 1.035419624597184;
    //private const double _k0_phiphi = -89.32195980848145;
    //private const double _k0_phidelta = -1.7415947744452318;
    //private const double _k0_deltaphi = _k0_phidelta;
    //private const double _k0_deltadelta = -0.6776962381761491;
    //private const double _k2_phiphi = 0;
    //private const double _k2_phidelta = 74.12484374632714;
    //private const double _k2_deltaphi = 0;
    //private const double _k2_deltadelta = 1.5700590282694744;

    // parameters of bicycle Stratos with rider Jason using yeadon model
    // refer to: A.L. Schwab et al. "Lateral dynamics of a bicycle with a
    // passive rider model: stability and controllability" and Jason Moore's phd
    // dissertation.
    //
    // http://www.bicycle.tudelft.nl/schwab/Publications/schwab2012lateral.pdf
    // https://moorepants.github.io/dissertation/physicalparameters.html#yeadon-method
    private const double _m_phiphi = 101.67198630701914;
    private const double _m_phidelta = 2.468161147117381;
    private const double _m_deltaphi = _m_phidelta;
    private const double _m_deltadelta = 0.2585325801368151;
    private const double _c1_phiphi = 0;
    private const double _c1_phidelta = 45.90183836032588;
    private const double _c1_deltaphi = -0.4888089303250309;
    private const double _c1_deltadelta = 1.8384415933130247;
    private const double _k0_phiphi = -93.62317973041073;
    private const double _k0_phidelta = -2.7223228332947773;
    private const double _k0_deltaphi = _k0_phidelta;
    private const double _k0_deltadelta = -0.7913852193214018;
    private const double _k2_phiphi = 0;
    private const double _k2_phidelta = 87.22961573777025;
    private const double _k2_deltaphi = 0;
    private const double _k2_deltadelta = 2.635502885292141;

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

    //public override double steerAxisTilt { get { return Math.PI/10; } }
    //public override double trail { get { return 0.08; } }
    //public override double wheelbase { get { return 1.02; } }
    //public override double rearRadius { get { return 0.3; } }

    //public override double steerAxisTilt { get { return 0.399680398707; } }
    //public override double trail { get { return 0.0685808540382; } }
    //public override double wheelbase { get { return 1.121; } }
    //public override double rearRadius { get { return 0.340958858855; } }

    public override double steerAxisTilt { get { return 0.29496064358704177; } }
    public override double trail { get { return 0.05626998711805831; } }
    public override double wheelbase { get { return 1.037; } }
    public override double rearRadius { get { return 0.338477091115578; } }
}
