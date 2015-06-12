#ifndef BUTTERLOWPASS_H
#define BUTTERLOWPASS_H

class ButterLowpass {
private:
    //static const int _size = 5;
    static const int _size = 6;
    //static const int _size = 7;
    //static const int _size = 8;
    double _x[_size]; // input
    double _y[_size]; // output
    int _n; // current array index

public:
    ButterLowpass();
    float filter(float sample);
};

#endif // BUTTERLOWPASS_H
