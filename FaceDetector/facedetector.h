#ifndef FACEDETECTOR_H
#define FACEDETECTOR_H

#include <iostream>
#include <sstream>
#include <stdlib.h>

#include <opencv2/core/core.hpp>
#include <opencv2/highgui/highgui.hpp>
#include <opencv2/objdetect/objdetect.hpp>
#include <opencv2/imgproc/imgproc.hpp>
#include <opencv2/video/video.hpp>
#include <opencv2/video/background_segm.hpp>
#include <opencv2/video/tracking.hpp>

#include <face.h>
#include <utils.h>
#include <logger.h>

#include "osc/OscOutboundPacketStream.h"
#include "ip/UdpSocket.h"

using namespace std;
using namespace cv;

class Facedetector
{
public:
    Facedetector();
    ~Facedetector();


    bool loadFrontCascade(char* cascade);
    bool loadProfileCascade(char* cascade);

    Mat detect(Mat& frame);
    vector<Face> getFaces();


    //Settings
    bool debug;
    bool bgSubtraction;
    Size detectionSize;
    int maxFaceDimension;

    //Region of Interest
    int roiTop;
    int roiBottom;

    //"distance measuring"
    int middleArea;
    int nearArea;


private:

    void drawFaces(Mat& frame);
    Mat subtractBG(Mat& frame);
    void addFaces(vector<Rect> rects, Mat& frame, Face::FaceType type);
    Face::FaceDistance distance(Rect r);

    // **** Member **** //
    CascadeClassifier mFrontCascade;
    CascadeClassifier mProfileCascade;
    vector<Face> mFaces;

    //tracking
    Mat mPrev;

    //background subtract
    BackgroundSubtractorMOG2 mBGSub;
    Mat mBGMask;
    vector<Rect> mFgROIs;

    //OSC Network
    char*    oscAddr;
    int      oscPort;
    int      oscBufferSize;
    char*    oscOutputBuffer;
    UdpTransmitSocket* oscTransmitSocket;

    //Logging
    Logger* mLog;


};

#endif // FACEDETECTOR_H
