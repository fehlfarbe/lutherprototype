#ifndef UTILS_H
#define UTILS_H

#include <iostream>
#include <sstream>
#include <stdlib.h>
#include <time.h>

#include <opencv2/core/core.hpp>

using namespace std;
using namespace cv;

class Utils
{
public:
    Utils();

    static string ocvType2String(int type);
    static string getDateTimeString(string format);
};

#endif // UTILS_H
