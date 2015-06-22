#include "butterlowpass.h"

/*
 * This file is autogenerated. Please do not modify directly as changes may be
 * overwritten.
 */

namespace {
    // Butter lowpass filter
    // order: 4
    // cutoff freq: 5.0
    // sample freq: 50.0
    const double a[] = { 1.0,-2.36951300718,2.31398841442,-1.05466540588,0.187379492368 };
    const double b[] = { 0.00482434335772,0.0192973734309,0.0289460601463,0.0192973734309,0.00482434335772 };
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