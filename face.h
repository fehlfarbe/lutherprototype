#ifndef FACE_H
#define FACE_H

#include <iostream>
#include <sstream>
#include <sys/time.h>


#include <opencv2/core/core.hpp>
#include <opencv2/highgui/highgui.hpp>
#include <opencv2/video/tracking.hpp>

using namespace cv;
using namespace std;

class Face
{
public:
    enum FaceDistance { FAR, MIDDLE, NEAR, UNKNOWN };
    enum FaceType { FRONT, PROFILE };

    Face(Rect r, Mat& frame, Face::FaceType type = FRONT);
    ~Face();
    void release();

    //getter
    Point center();
    Point motionVec();
    Rect rect();
    int     id();
    int     duration();


    void    draw(Mat& frame, FaceDistance dist = UNKNOWN, bool features = true);
    void    update(Rect r, Mat& frame, FaceType type = FRONT);

    int middleDistance;
    int nearDistance;
    bool isSimilar(Rect r);
    bool track(Mat& prev, Mat &curr);
    void show(int ms = 10);



private:

    int         mID;
    clock_t     mStartTime;
    Rect        mRect;
    FaceType    mType;
    Mat         mFace;

    void updateFace(Mat& frame);

    //tracking
    Size subPixWinSize;
    Size winSize;
    TermCriteria termcrit;
    vector<Point2f> mTrackPoints;
    Point2f mMotionVector;
};

#endif // FACE_H
