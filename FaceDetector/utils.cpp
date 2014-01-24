#include "utils.h"

Utils::Utils()
{
}


string Utils::ocvType2String(int type) {
  string r;

  uchar depth = type & CV_MAT_DEPTH_MASK;
  uchar chans = 1 + (type >> CV_CN_SHIFT);

  switch ( depth ) {
    case CV_8U:  r = "8U"; break;
    case CV_8S:  r = "8S"; break;
    case CV_16U: r = "16U"; break;
    case CV_16S: r = "16S"; break;
    case CV_32S: r = "32S"; break;
    case CV_32F: r = "32F"; break;
    case CV_64F: r = "64F"; break;
    default:     r = "User"; break;
  }

  r += "C";
  r += (chans+'0');

  return r;
}


string Utils::getDateTimeString(string format){

    time_t t = time(0);
    char date_buff[40];
    struct tm* my_tm = localtime(&t);
    strftime(date_buff, sizeof(date_buff), format.c_str(), my_tm);
    ostringstream stream;
    stream << date_buff;

    return stream.str();
}
