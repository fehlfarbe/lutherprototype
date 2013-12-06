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
    mBGSubtractor = new BackgroundSubtractorMOG2();

}


bool Facedetector::loadFrontCascade(char* cascade){

    return mFaceCascade.load(cascade);

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
        //frame_gray = subtractBG(frame_gray);
        //frame_gray = frame_gray & mBGMask;
//        Mat tmp;
//        frame_gray.copyTo(tmp, mBGMask);
//        tmp.copyTo(frame_gray);
//        cout << type2str(mBGMask.type()) << endl;
//        cout << type2str(tmp.type()) << endl;
//        cout << type2str(frame_gray.type()) << endl;

        if( debug ){
            imshow("BGMask", mBGMask);
            waitKey(10);
        }
    }


    for(unsigned int i = 0; i<mFaces.size(); i++){
        if( !mFaces[i].track(mPrevGray, frame_gray) ){
            mFaces.erase(mFaces.begin() + i);
            i--;
            cout << "delete elemnent" << endl;
        }
    }

    //face detection
    if( mFaces.size() < 8){
        vector<Rect> rects;
        mFaceCascade.detectMultiScale(frame_gray, rects, 1.1, 10, 0|CV_HAAR_SCALE_IMAGE, Size(24, 24), Size(200, 200));

        //rects to faces
        for(unsigned int i=0; i<rects.size(); i++){

            bool add = true;
            for(unsigned j=0; j<mFaces.size(); j++){
                if( mFaces[j].isSimilar(rects[i])){
                    mFaces[j].update(rects[i], distance(rects[i]), frame_gray);
                    add = false;
                    break;
                }
            }

            if( add ){
                Face::FaceDistance dist = distance(rects[i]);
                Face f = Face(rects[i], dist, frame_gray);
                mFaces.push_back(f);
            }
        }
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
        mFaces[i].draw(frame);
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
