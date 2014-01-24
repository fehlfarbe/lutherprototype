#include "facedetector.h"

Facedetector::Facedetector()
{
	//init
	debug = true;
    bgSubtraction = true;
    detectionSize = Size(640, 480);
    maxFaceDimension = 250;

    //ROI
    roiTop = 30;
    roiBottom = 30;

    //distance
    middleArea = 50;
    nearArea = 86;

    //OSC Settings
	oscAddr = "127.0.0.1";
    oscPort = 7000;
    oscBufferSize = 1024;

    //create Backgroundsubstractor
    mBGSub = BackgroundSubtractorMOG2();

    //setup OSC Connection
    oscOutputBuffer = new char[oscBufferSize];
    oscTransmitSocket = new UdpTransmitSocket( IpEndpointName( oscAddr, oscPort ) );

    osc::OutboundPacketStream p( oscOutputBuffer, oscBufferSize );
    p << osc::BeginBundleImmediate
      << osc::BeginMessage( "/start" )
      << osc::EndMessage
      << osc::EndBundle;
    oscTransmitSocket->Send( p.Data(), p.Size() );

    //Setup Logger
    mLog = new Logger(Utils::getDateTimeString("%Y-%m-%d").append(".csv"));
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

    //logger
    delete mLog;

}


bool Facedetector::loadFrontCascade(char* cascade){
    return mFrontCascade.load(cascade);

}

bool Facedetector::loadProfileCascade(char* cascade){
    return mProfileCascade.load(cascade);
}


Mat Facedetector::detect(Mat& frame){

    //Mat frame_gray, frame_resized;
    Mat frame_resized;

    /************************
     *
     * resize image
     *
     ***********************/
    if( frame.cols > detectionSize.width){
        double scale = double(detectionSize.width) / frame.cols;
        resize(frame, frame_resized, Size(detectionSize.width, frame.rows * scale));
    } else {
        frame.copyTo(frame_resized);
    }

    Mat frame_roi = Mat(frame_resized, Rect(0, roiTop, frame_resized.cols, frame_resized.rows-roiTop-roiBottom));

    //convert to grayscale, equalize histogram
    //cvtColor( frame_roi, frame_gray, CV_BGR2GRAY );
    //equalizeHist( frame_gray, frame_gray );


    /************************
     *
     * background subtraction
     *
     ***********************/
    if( bgSubtraction ){
        mBGSub.operator ()(frame_roi, mBGMask);
        Mat cont = mBGMask.clone();

        vector<vector<Point> > contours;
        vector<Vec4i> hierarchy;

        findContours( cont, contours, hierarchy, CV_RETR_EXTERNAL, CV_CHAIN_APPROX_NONE, Point(0, 0) );

        mFgROIs.clear();
        RNG rng(12345);
        for(unsigned int i = 0; i< contours.size(); i++ )
        {
            Rect bound = boundingRect(contours[i]);
            if( bound.width >= 25 && bound.height >= 25){
                mFgROIs.push_back(bound);
                //Scalar color = Scalar( rng.uniform(0, 255), rng.uniform(0,255), rng.uniform(0,255) );
                //drawContours( frame_resized, contours, i, color, 1, 8, hierarchy, 0, Point(0, roiTop) );
                rectangle(mBGMask, bound, Scalar(200,200,200), 1);
            }
        }

        if( debug ){
            Mat temp;
            mBGSub.getBackgroundImage(temp);
            imshow("BackgroundModel", temp);
            waitKey(1);
            imshow("BGMask", mBGMask);
            waitKey(1);
        }
    }

    /************************
     *
     * track and delete old faces
     *
     ***********************/
    for(unsigned int i = 0; i<mFaces.size(); i++){
        if( !mFaces[i].track(mPrev, frame_roi) ){
            cout << "delete element with id " << mFaces[i].id() << endl;

            //send escaped face ID with OSC
            osc::OutboundPacketStream p( oscOutputBuffer, oscBufferSize );
            p << osc::BeginBundleImmediate
              << osc::BeginMessage( "/deleteface" )
              << mFaces[i].id() << osc::EndMessage
              << osc::EndBundle;
            oscTransmitSocket->Send( p.Data(), p.Size() );

            //log time
            time_t start, end;
            int dur = mFaces[i].duration(&start, &end);
            ostringstream stream;
            stream << mFaces[i].id() << "," << start << "," << end << "," << dur;
            mLog->log(stream.str().c_str());
            cout << "Face " << mFaces[i].id() << " was " << dur << "s detected" << endl;

            //delete element
            mFaces[i].release();
            mFaces.erase(mFaces.begin() + i);
            i--;
        }
    }

    /************************
     *
     * track and duplicate faces
     *
     ***********************/
    for(unsigned int i = 0; i<mFaces.size(); i++){
        for(unsigned int j=0; j<mFaces.size(); j++){
            if( j != i){
                if(mFaces[i].isSimilar(mFaces[j])){
                    cout << mFaces[i].id() << " similar to " << mFaces[j].id() << endl;
                    Rect r;
                    if( mFaces[i].rect().area() > mFaces[j].rect().area())
                        r = mFaces[j].rect();
                    else
                        r = mFaces[i].rect();

                    mFaces[i].update(r, frame_roi);

                    //log time
                    time_t start, end;
                    int dur = mFaces[j].duration(&start, &end);
                    ostringstream stream;
                    stream << mFaces[j].id() << "," << start << "," << end << "," << dur;
                    mLog->log(stream.str().c_str());
                    cout << "Face " << mFaces[j].id() << " was " << dur << "s detected" << endl;

                    mFaces.erase(mFaces.begin() + j);
                    j--;
                }
            }
        }
    }

    /************************
     *
     * face detection
     *
     ***********************/
    vector<Rect> rects; //possible faces
    if( bgSubtraction ){
        //look for faces in ROIs
        for(unsigned int i=0; i<mFgROIs.size(); i++){
            Mat FgROI = Mat(frame_roi, mFgROIs[i]);
            Size maxSize = Size(mFgROIs[i].width, mFgROIs[i].height);

            //detect frontfaces
            mFrontCascade.detectMultiScale(FgROI, rects, 1.3, 3, 0|CV_HAAR_SCALE_IMAGE, Size(24, 24), maxSize);
            //add offset
            for(unsigned j=0; j<rects.size(); j++){
                rects[j].x += mFgROIs[i].x;
                rects[j].y += mFgROIs[i].y;
            }
            addFaces(rects, frame_roi, Face::FRONT);
            rects.clear();

            //detect profile faces
            mProfileCascade.detectMultiScale(FgROI, rects, 1.3, 3, 0|CV_HAAR_SCALE_IMAGE, Size(34, 20), maxSize);
            //add offset
            for(unsigned j=0; j<rects.size(); j++){
                rects[j].x += mFgROIs[i].x;
                rects[j].y += mFgROIs[i].y;
            }
            addFaces(rects, frame_roi, Face::PROFILE);
        }
    } else {
        //find faces in whole frame
        mFrontCascade.detectMultiScale(frame_roi, rects, 1.3, 3, 0|CV_HAAR_SCALE_IMAGE, Size(24, 24), Size(200, 200));
        addFaces(rects, frame_roi, Face::FRONT);
        rects.clear();
        mProfileCascade.detectMultiScale(frame_roi, rects, 1.3, 3, 0|CV_HAAR_SCALE_IMAGE, Size(34, 20), Size(200, 200));
        addFaces(rects, frame_roi, Face::PROFILE);
    }


    /************************
     *
     * send OSC facelist
     *
     ***********************/
    for(unsigned int i=0; i<mFaces.size(); i++){
        osc::OutboundPacketStream p( oscOutputBuffer, oscBufferSize );
        p << osc::BeginBundleImmediate
          << osc::BeginMessage( "/facelist" )
          << mFaces[i].id()
          << mFaces[i].center().x
          << mFaces[i].center().y
          << mFaces[i].motionVec().x
          << mFaces[i].motionVec().y
          << distance(mFaces[i].rect())
          << osc::EndMessage
          << osc::EndBundle;
        oscTransmitSocket->Send( p.Data(), p.Size() );
    }

    /************************
     *
     * save frame for tracking
     *
     ***********************/
    mPrev = frame_roi.clone();

    /************************
     *
     * draw faces
     *
     ***********************/
    drawFaces(frame_resized);


    //show equalized grayscale image
//    if( debug ){
//        imshow("Grayscale", frame_gray);
//        waitKey(10);
//    }


    return frame_resized;
}

vector<Face> Facedetector::getFaces(){

    return mFaces;

}


void Facedetector::drawFaces(Mat& frame){

    //set ROI
    Mat roi = frame(Rect(0, roiTop, frame.cols, frame.rows-roiTop-roiBottom));
    for( size_t i = 0; i < mFaces.size(); i++ ){
        mFaces[i].draw(roi, distance(mFaces[i].rect()));
    }

    //draw ROI
    rectangle(frame, Rect(0, roiTop, frame.cols, frame.rows-roiTop-roiBottom), Scalar(100, 100, 100));

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

        //continue if face is too big
        if(rects[i].width > maxFaceDimension || rects[i].height > maxFaceDimension)
            continue;

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
              << f.id()
              << distance(mFaces[i].rect())
              << osc::EndMessage
              << osc::EndBundle;
            oscTransmitSocket->Send( p.Data(), p.Size() );
        }
    }
}
