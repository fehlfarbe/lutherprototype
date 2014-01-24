#include <sstream>
#include <iostream>
#include <time.h>

#include <opencv2/core/core.hpp>
#include <opencv2/imgproc/imgproc.hpp>
#include <opencv2/video/video.hpp>
#include <opencv2/highgui/highgui.hpp>

#include <facedetector.h>
#include <utils.h>

using namespace std;
using namespace cv;


// options
bool bgSub = true;
bool writeIM = true;
const char* writeDst = "output/";

int main( int argc, const char* argv[] )
{

    VideoCapture *cap;
    if( argc > 1){
        cout << "Open file " << argv[1] << endl;
        cap = new VideoCapture(argv[1]);
    }
    else{
        cout << "Open Videodevice 0" << endl;
        cap = new VideoCapture(0);
    }


    if( !cap->isOpened() ){
        cout << "Can't open videodevice" << endl;
        return -1;
    }

    int count = 0;
    Mat frame, output;
    time_t start, end;
    cap->read(frame);

    // Setup detector
    Facedetector detector = Facedetector();
    detector.bgSubtraction = bgSub;

    if(!detector.loadFrontCascade("../lbpcascade_frontalface.xml")){
        cout << "Can't load cascade file";
        return -1;
    }
    if(!detector.loadProfileCascade("../lbpcascade_profileface.xml")){
        cout << "Can't load cascade file";
        return -1;
    }

    //Start endless loop
    time(&start);
    while(!frame.empty()){
        count++;

        //face detection
        output = detector.detect(frame);
        vector<Face> faces = detector.getFaces();
        //output = frame;

        //Ouput Window
        time(&end);
        double fps = double(count) / difftime(end, start);
        ostringstream stream;
        stream << faces.size() << " Faces detected (" << fps << " fps)";
        putText(output, stream.str(), Point(5, output.rows-10), 1, 1, Scalar(255, 255, 255));
        imshow("Output", output);
        if( (waitKey(10) & 255) == 'c' ){
            cout << "Abort..." << endl;
            break;
        }

        //write images
        if(writeIM){
            ostringstream filename;
            time_t t = time(0);
            filename << writeDst << "/" << t << "_" << count << ".jpg";
            imwrite(filename.str(), output);
        }

        //cout << faces.size() << " faces ";
        //cout << "(" << 1.0 / ((float(clock()-t)/CLOCKS_PER_SEC)) << "fps)" << endl;

        //read next frame
        cap->read(frame);
    }

    cap->release();
    delete cap;
    cout << "End.." << endl;

    return 0;
}

