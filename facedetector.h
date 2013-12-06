#ifndef FACEDETECTOR_H
#define FACEDETECTOR_H

#include <iostream>
#include <sstream>

#include <core/core.hpp>
#include <highgui/highgui.hpp>
#include <objdetect/objdetect.hpp>
#include <opencv2/imgproc/imgproc.hpp>
#include <opencv2/video/video.hpp>
#include <opencv2/video/background_segm.hpp>
#include <opencv2/video/tracking.hpp>

#include <face.h>

using namespace std;
using namespace cv;

class Facedetector
{
public:
    Facedetector();

    bool loadFrontCascade(char* cascade);
    bool loadProfileCascade(char* cascade);

    Mat detect(Mat& frame);
    vector<Face> getFaces();


    bool debug = true;
    bool bgSubtraction = true;
    Size detectionSize = Size(640, 480);
    int middleArea = 50;
    int nearArea = 90;


private:

    void drawFaces(Mat& frame);
    Mat subtractBG(Mat& frame);
    Face::FaceDistance distance(Rect r);

    // **** Member **** //
    CascadeClassifier mFrontCascade;
    CascadeClassifier mProfileCascade;
    vector<Face> mFaces;

    //tracking
    Mat mPrevGray;


    //background subtract
    Ptr<BackgroundSubtractor> mBGSubtractor;
    Mat mBGMask;

};

#endif // FACEDETECTOR_H
