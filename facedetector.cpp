#include "facedetector.h"

string type2str(int type) {
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

Facedetector::Facedetector()
{
    //create Backgroundsubstractor
    mBGSubtractor = new BackgroundSubtractorMOG2();

    //setup OSC Connection
    oscOutputBuffer = new char[oscBufferSize];
    oscTransmitSocket = new UdpTransmitSocket( IpEndpointName( oscAddr, oscPort ) );

    osc::OutboundPacketStream p( oscOutputBuffer, oscBufferSize );
    p << osc::BeginBundleImmediate
      << osc::BeginMessage( "/start" )
      << osc::EndMessage
      << osc::EndBundle;
    oscTransmitSocket->Send( p.Data(), p.Size() );
}


Facedetector::~Facedetector(){

    cout << "Cleanup Facedetector..." << endl;
    //Cleanup OSC
    //send escaped face ID with OSC
    osc::OutboundPacketStream p( oscOutputBuffer, oscBufferSize );
    p << osc::BeginBundleImmediate
      << osc::BeginMessage( "/end" )
      << osc::EndMessage
      << osc::EndBundle;
    oscTransmitSocket->Send( p.Data(), p.Size() );
    delete[] oscOutputBuffer;
    delete oscTransmitSocket;

}


bool Facedetector::loadFrontCascade(char* cascade){

    return mFrontCascade.load(cascade);

}

bool Facedetector::loadProfileCascade(char* cascade){
    return mProfileCascade.load(cascade);
}


Mat Facedetector::detect(Mat& frame){

    Mat frame_gray, frame_resized;

    // resize frame
    if( frame.cols > detectionSize.width){
        double scale = double(detectionSize.width) / frame.cols;
        resize(frame, frame_resized, Size(detectionSize.width, frame.rows * scale));
    } else {
        frame.copyTo(frame_resized);
    }

    //convert to grayscale, equalize histogram
    cvtColor( frame_resized, frame_gray, CV_BGR2GRAY );
    equalizeHist( frame_gray, frame_gray );

    //background subtraction
    if( bgSubtraction ){
        mBGSubtractor->operator()(frame_resized, mBGMask);
        Mat tmp;
        frame_gray.copyTo(tmp, mBGMask);
        tmp.copyTo(frame_gray);

        if( debug ){
            imshow("BGMask", mBGMask);
            waitKey(10);
        }
    }

    //delete escaped faces
    for(unsigned int i = 0; i<mFaces.size(); i++){
        if( !mFaces[i].track(mPrevGray, frame_gray) ){
            cout << "delete element with id " << mFaces[i].id() << endl;

            //send escaped face ID with OSC
            osc::OutboundPacketStream p( oscOutputBuffer, oscBufferSize );
            p << osc::BeginBundleImmediate
              << osc::BeginMessage( "/deleteface" )
              << mFaces[i].id() << osc::EndMessage
              << osc::EndBundle;
            oscTransmitSocket->Send( p.Data(), p.Size() );

            //get duration time
            int dur = mFaces[i].duration();
            cout << "Face " << mFaces[i].id() << " was " << dur << "s detected" << endl;

            //delete element
            mFaces[i].release();
            mFaces.erase(mFaces.begin() + i);
            i--;
        }
    }

    //face detection
    vector<Rect> rects;
    mFrontCascade.detectMultiScale(frame_gray, rects, 1.1, 12, 0|CV_HAAR_SCALE_IMAGE, Size(24, 24), Size(200, 200));
    addFaces(rects, frame_gray, Face::FRONT);
    rects.clear();
    mProfileCascade.detectMultiScale(frame_gray, rects, 1.1, 12, 0|CV_HAAR_SCALE_IMAGE, Size(34, 20), Size(200, 200));
    addFaces(rects, frame_gray, Face::PROFILE);

    //send FaceList with OSC
    for(unsigned int i=0; i<mFaces.size(); i++){
        osc::OutboundPacketStream p( oscOutputBuffer, oscBufferSize );
        p << osc::BeginBundleImmediate
          << osc::BeginMessage( "/facelist" )
          << mFaces[i].id()
          << mFaces[i].center().x
          << mFaces[i].center().y
          << mFaces[i].motionVec().x
          << mFaces[i].motionVec().y
          << osc::EndMessage
          << osc::EndBundle;
        oscTransmitSocket->Send( p.Data(), p.Size() );
    }

    //draw faces
    drawFaces(frame_resized);


    //show equalized grayscale image
    if( debug ){
        imshow("Grayscale", frame_gray);
        waitKey(10);
    }

    //save last frame
    swap(frame_gray, mPrevGray);

    return frame_resized;
}

vector<Face> Facedetector::getFaces(){

    return mFaces;

}


void Facedetector::drawFaces(Mat& frame){

    for( size_t i = 0; i < mFaces.size(); i++ ){
        mFaces[i].draw(frame, distance(mFaces[i].rect()));
    }

}

Face::FaceDistance Facedetector::distance(Rect r){
    if( r.width < middleArea)
        return Face::FAR;
    else if( r.width < nearArea )
        return Face::MIDDLE;
    else
        return Face::NEAR;
}

void Facedetector::addFaces(vector<Rect> rects, Mat& frame, Face::FaceType type){

    for(unsigned int i=0; i<rects.size(); i++){

        bool add = true;
        for(unsigned j=0; j<mFaces.size(); j++){
            if( mFaces[j].isSimilar(rects[i])){
                mFaces[j].update(rects[i], frame, type);
                add = false;
                break;
            }
        }

        if( add ){
            Face f = Face(rects[i], frame, type);
            mFaces.push_back(f);

            //send new ID with OSC
            osc::OutboundPacketStream p( oscOutputBuffer, oscBufferSize );
            p << osc::BeginBundleImmediate
              << osc::BeginMessage( "/newface" )
              << f.id() << osc::EndMessage
              << osc::EndBundle;
            oscTransmitSocket->Send( p.Data(), p.Size() );
        }
    }
}
