#include <string.h>
#include "medianlowpass.h"
#include "quicksort.h"


MedianLowpass::MedianLowpass(): _x{0.0f}, _n{0} { }

float MedianLowpass::filter(float sample) {
    _x[_n] = sample;
    _n = (_n + 1) % _size;

    double x[_size];
    memcpy(x, _x, sizeof(x));
    quicksort(x, _size);
    return x[_size/2 + _size%2];
}
