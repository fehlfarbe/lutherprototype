#include "face.h"

Face::Face(Rect r, Mat &frame, FaceType type)
{
    update(r, frame, type);
}

void Face::update(Rect r, Mat& frame, FaceType type){

    if( mRect.area() == 0){
        mRect = r;
    }
    else{
        //ToDo: merge
        mRect = r;
    }

    mType = type;

    Mat mask(frame.size(), CV_8UC1);
    mask.setTo(Scalar::all(0));
    rectangle(mask, r, Scalar(255,255,255), -1);

    mTrackPoints.clear();
    goodFeaturesToTrack(frame, mTrackPoints, 100, 0.01, 5, mask, 3, 0, 0.04);
    //if( mTrackPoints.size() > 0)
    //    cornerSubPix(frame, mTrackPoints, subPixWinSize, Size(-1,-1), termcrit);
}

bool Face::isSimilar(Rect r){

    Rect intersect = r & mRect;
    if( intersect.size().area() > 0){
        return true;
    }

    return false;
}

bool Face::track(Mat& prev, Mat& curr){

    if( mTrackPoints.empty() )
        return false;

    vector<uchar> status;
    vector<float> err;
    vector<Point2f> newPoints;

    if(prev.empty())
        curr.copyTo(prev);

    calcOpticalFlowPyrLK(prev, curr, mTrackPoints, newPoints, status, err, winSize, 3, termcrit, 0, 0.001);

    size_t i, k;
    Point2f motionVec;
    for( i = k = 0; i < newPoints.size(); i++ )
    {
        if( !status[i] )
            continue;

        motionVec += newPoints[i]-mTrackPoints[i];
        newPoints[k++] = newPoints[i];
        //circle( curr, newPoints[i], 3, Scalar(0,255,0), -1, 8);
    }
    motionVec.x /= k;
    motionVec.y /= k;
    mMotionVector = motionVec;
    newPoints.resize(k);
    swap(mTrackPoints, newPoints);

    if( mTrackPoints.empty() )
        return false;


    //cout << mTrackPoints.size() << endl;
//    Rect brect = boundingRect(mTrackPoints);
//    mRect.x = brect.x;
//    mRect.y = brect.y;
    mRect = boundingRect(mTrackPoints);;


    return true;
}

Point Face::position(){

    return Point(mRect.x+mRect.width/2,
                 mRect.y+mRect.height/2);

}

Rect Face::rect(){
    return mRect;
}

void Face::draw(Mat& frame, FaceDistance dist, bool features){

    // draw box + info
    ostringstream stream;
    stream << mRect.width << "x" << mRect.height << "px, " << mTrackPoints.size() << "feat";

    //color
    Scalar c;
    switch(dist){

    case Face::FAR:
        c = Scalar(0, 0, 200);
        break;
    case Face::MIDDLE:
        c = Scalar( 0, 200, 200);
        break;
    case Face::NEAR:
        c = Scalar(0, 200, 0);
        break;
    default:
        c = Scalar(200, 200, 200);
    }

    putText(frame, stream.str(), Point(mRect.x,mRect.y - 5), 1, 0.7, Scalar(255,255,255));
    rectangle(frame, mRect, c, 2);


    //draw features
    if( features ){
        for(unsigned int i=0; i < mTrackPoints.size(); i++){
            circle( frame, mTrackPoints[i], 1, Scalar(0,200,0), -1, 8);
        }
        Point2f motionVec = mMotionVector;
        motionVec.x += position().x;
        motionVec.y += position().y;
        line(frame, position(), motionVec, Scalar(255, 255, 0), 2);

        //Rect brect = boundingRect(mTrackPoints);
        //rectangle(frame, brect, Scalar(150, 150, 150), 1);
    }
}
