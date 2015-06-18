#ifndef MEDIANLOWPASS_H
#define MEDIANLOWPASS_H

class MedianLowpass {
private:
    static const int _size = 4 + 1;
    double _x[_size]; // input
    int _n; // current array index

public:
    MedianLowpass();
    float filter(float sample);
};

#endif // MEDIANLOWPASS_H
