#include "quicksort.h"

namespace {
    int partition(double *arr, int size) {
        double temp;
        int pivot  = size - 1;
        int i, j;
        for(i = 0, j = -1; i < size - 1; ++i) {
            if (arr[i] < arr[pivot]) {
                ++j;
                if(i != j) {
                    temp = arr[i];
                    arr[i] = arr[j];
                    arr[j] = temp;
                }
            }
        }
        temp = arr[pivot];
        arr[pivot] = arr[j + 1];
        arr[j + 1] = temp;
        pivot = j + 1;
        return pivot;
    }
} // namespace


void quicksort(double *arr, int size) {
    if (size > 1) {
        int p = partition(arr, size);
        quicksort(arr, p);
        quicksort(arr + p + 1, size - (p - 1));
    }
    return;
}
