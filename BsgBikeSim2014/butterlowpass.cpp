#include "butterlowpass.h"

namespace {
    // Butterworth lowpass filter
    // 4th order, 5/400 = 0.0125 normalized cutoff freq
//    const double a[] = {1., -3.7947911, 5.40516686, -3.42474735, 0.814406};
//    const double b[] = {2.15056874e-06, 8.60227495e-06, 1.29034124e-05,
//        8.60227495e-06, 2.15056874e-06};
    // Butterworth lowpass filter
    // 5th order, 5/400 = 0.0125 normalized cutoff freq
    const double a[] = {1., -4.74585884, 9.01543009, -8.56867595, 4.07461224,
        -0.77550491};
    const double b[] = {8.24533552e-08, 4.12266776e-07, 8.24533552e-07,
        8.24533552e-07, 4.12266776e-07, 8.24533552e-08};
    // Butterworth lowpass filter
    // 6th order, 5/400 = 0.0125 normalized cutoff freq
//    const double a[] = {1., -5.69656073, 13.52849899, -17.14406324,
//        12.22707316, -4.65313839, 0.7381904};
//    const double b[] = {3.16070139e-09, 1.89642083e-08, 4.74105209e-08,
//        6.32140278e-08, 4.74105209e-08, 1.89642083e-08, 3.16070139e-09};
    // Butterworth lowpass filter
    // 7th order, 5/400 = 0.0125 normalized cutoff freq
//    const double a[] = {1., -6.64705807, 18.94424416, -30.00841694,
//        28.53292076, -16.28480303, 5.16564013, -0.70252699};
//    const double b[] = {1.21147369e-10, 8.48031583e-10, 2.54409475e-09,
//        4.24015792e-09, 4.24015792e-09, 2.54409475e-09, 8.48031583e-10,
//        1.21147369e-10};
} // namespace

ButterLowpass::ButterLowpass(): _x{0.0f}, _y{0.0f}, _n{0} { }

float ButterLowpass::filter(float sample) {
    _x[_n] = sample;
    _y[_n] = b[0]*_x[_n];
    for (int i = 1; i < _size; ++i) {
        _y[_n] += b[i]*_x[(_n - i + _size) % _size]
            - a[i]*_y[(_n - i + _size) % _size];
    }

    float result = _y[_n];
    _n = (_n + 1) % _size;
    return result;
}
