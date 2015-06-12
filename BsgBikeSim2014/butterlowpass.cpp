#include "butterlowpass.h"

namespace {
    // Butterworth lowpass filter, 4th order, 5/50 = 0.2 normalized cutoff freq
    const float a[] = {1., -2.36951301, 2.31398841, -1.05466541, 0.18737949};
    const float b[] = {0.00482434, 0.01929737, 0.02894606, 0.01929737,
        0.00482434};
} // namespace

ButterLowpass::ButterLowpass(): _x{0.0f}, _y{0.0f}, _n{0} { }

float ButterLowpass::filter(float sample) {
    _x[_n] = sample;
    _y[_n] = b[0]*_x[_n]
        + b[1]*_x[(_n-1+_m)%_m]
        + b[2]*_x[(_n-2+_m)%_m]
        + b[3]*_x[(_n-3+_m)%_m]
        + b[4]*_x[(_n-4+_m)%_m]
        - a[1]*_y[(_n-1+_m)%_m]
        - a[2]*_y[(_n-2+_m)%_m]
        - a[3]*_y[(_n-3+_m)%_m]
        - a[4]*_y[(_n-4+_m)%_m];
    float result = _y[_n];
    _n = (_n + 1) % 10;
    return result;
}
