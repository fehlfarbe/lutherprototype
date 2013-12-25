#include <sstream>
#include <iostream>
#include <sys/time.h>

#include <core/core.hpp>
#include <imgproc/imgproc.hpp>
#include <video/video.hpp>
#include <highgui/highgui.hpp>
#include <facedetector.h>

#include "osc/OscOutboundPacketStream.h"
#include "ip/UdpSocket.h"


#define ADDRESS "127.0.0.1"
#define PORT 7000
#define OUTPUT_BUFFER_SIZE 1024

using namespace std;
using namespace cv;


// options
bool bgSub = true;
bool writeIM = true;
const char* writeDst = "output/";

int main()
{

//    VideoCapture cap = VideoCapture(0);
    VideoCapture cap = VideoCapture("../../input/Unbenannt.mpg");

    if( !cap.isOpened()){
        cout << "Can't open videodevice" << endl;
        return -1;
    }

    int count = 0;
    Mat frame, output;
    cap.read(frame);

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

    //setup OSC connection
    UdpTransmitSocket transmitSocket( IpEndpointName( ADDRESS, PORT ) );
    char buffer[OUTPUT_BUFFER_SIZE];
    osc::OutboundPacketStream p( buffer, OUTPUT_BUFFER_SIZE );


    //Start endless loop
    while(!frame.empty()){
        count++;
        clock_t t = clock();

        //face detection
        output = detector.detect(frame);
        vector<Face> faces = detector.getFaces();

        //send OSC packets
        p.Clear();
        p << osc::BeginBundleImmediate
            << osc::BeginMessage( "/test1" )
                << true << count << (float)t << "hello world" << osc::EndMessage
            << osc::EndBundle;
        transmitSocket.Send( p.Data(), p.Size() );


        //Ouput Window
        float fps = 1.0f / ((float(clock()-t)/CLOCKS_PER_SEC));
        ostringstream stream;
        stream << faces.size() << " Faces detected (" << fps << " fps)";
        putText(output, stream.str(), Point(5, output.rows-10), 1, 1, Scalar(255, 255, 255));
        imshow("Output", output);
        waitKey(1);

        //write images
        if(writeIM){
            ostringstream filename;
            filename << writeDst << "/" << count << ".jpg";
            imwrite(filename.str(), output);
        }

        //cout << faces.size() << " faces ";
        //cout << "(" << 1.0 / ((float(clock()-t)/CLOCKS_PER_SEC)) << "fps)" << endl;

        //read next frame
        cap.read(frame);
    }

    cap.release();
    cout << "End.." << endl;

    return 0;
}

