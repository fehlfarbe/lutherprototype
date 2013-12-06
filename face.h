#ifndef FACE_H
#define FACE_H

#include <iostream>
#include <sstream>

#include <opencv2/core/core.hpp>
#include <opencv2/highgui/highgui.hpp>
#include <opencv2/video/tracking.hpp>

using namespace cv;
using namespace std;

class Face
{
public:
    enum FaceDistance { FAR, MIDDLE, NEAR };
    enum FaceType { FRONT, PROFILE };

    Face(Rect r, Face::FaceDistance dist, Mat& frame);

    FaceDistance distance();
    Point position();
    Rect rect();

    void draw(Mat& frame, bool features = true);
    void update(Rect r, Face::FaceDistance dist, Mat& frame);

    bool isSimilar(Rect r);
    bool track(Mat& prev, Mat &curr);
    void show(int ms = 10);



private:

    Rect mRect;
    FaceDistance mDistance;

    //tracking
    Size subPixWinSize = Size(2,2);
    Size winSize = Size(10,10);
    TermCriteria termcrit = TermCriteria(CV_TERMCRIT_ITER|CV_TERMCRIT_EPS, 20, 0.03);
    vector<Point2f> mTrackPoints;
};

#endif // FACE_H
