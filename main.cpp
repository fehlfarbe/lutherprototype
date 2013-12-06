#include <iostream>
#include <sys/time.h>

#include <core/core.hpp>
#include <imgproc/imgproc.hpp>
#include <video/video.hpp>
#include <highgui/highgui.hpp>
#include <facedetector.h>

using namespace std;
using namespace cv;

int main()
{

//    VideoCapture cap = VideoCapture(0);
    VideoCapture cap = VideoCapture("../input/Unbenannt.mpg");

    if( !cap.isOpened()){
        cout << "Can't open videodevice" << endl;
        return -1;
    }


    Mat frame, output;
    cap.read(frame);

    Facedetector detector = Facedetector();
    detector.bgSubtraction = false;

    if(!detector.loadFrontCascade("../lbpcascade_frontalface.xml")){
        cout << "Can't load cascade file";
        return -1;
    }
    if(!detector.loadProfileCascade("../lbpcascade_profileface.xml")){
        cout << "Can't load cascade file";
        return -1;
    }

    while(!frame.empty()){
        clock_t t = clock();
        output = detector.detect(frame);
        vector<Face> faces = detector.getFaces();

        imshow("Output", output);
        waitKey(1);

        cout << faces.size() << " faces ";
        cout << "(" << 1.0 / ((float(clock()-t)/CLOCKS_PER_SEC)) << "fps)" << endl;

        cap.read(frame);
    }

    cap.release();
    cout << "End.." << endl;
    return 0;
}

