#ifndef SAMPLE_H
#define SAMPLE_H

struct Sample {
    char prefix;
    float delta;
    float deltaDot;
    char suffix;
} __attribute__((packed));

#endif // SAMPLE_H
