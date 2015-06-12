#ifndef BUTTERLOWPASS_H
#define BUTTERLOWPASS_H

class ButterLowpass {
private:
    static const int _m = 10;
    float _x[_m]; // input
    float _y[_m]; // output
    int _n; // current array index

public:
    ButterLowpass();
    float filter(float sample);
};

#endif // BUTTERLOWPASS_H
