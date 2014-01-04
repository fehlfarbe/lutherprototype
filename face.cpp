#include "face.h"

Face::Face(Rect r, Mat &frame, FaceType type)
{
    cout << "new face" << endl;
    update(r, frame, type);

    //set ID
    static int  counter = 0;
    mID = counter++;

    //set Starttime
    mStartTime = clock();
}

Face::~Face(){
    //cleanup
    //cout << "Cleanup Face " << mID << endl;

    mTrackPoints.clear();
    mFace.release();
}

void Face::release(){
    //close window
    ostringstream stream;
    stream << mID;
    destroyWindow(stream.str());
}

void Face::update(Rect r, Mat& frame, FaceType type){

    if( mRect.area() == 0){
        mRect = r;
    }
    else{
        //ToDo: merge
        mRect = r;
//        cout << mRect << endl;
//        mRect.x = r.x < mRect.x ? r.x : mRect.x;
//        mRect.width = r.x + r.width > mRect.x + mRect.width ?
//                    r.x + r.width : mRect.x + mRect.width;
//        mRect.y = r.y > mRect.y ? r.y : mRect.y;
//        mRect.height = r.y + r.height > mRect.y + mRect.height ?
//                    r.y + r.height : mRect.y + mRect.height;
    }

    mType = type;

    Mat mask(frame.size(), CV_8UC1);
    mask.setTo(Scalar::all(0));
    rectangle(mask, r, Scalar(255,255,255), -1);

    mTrackPoints.clear();
    goodFeaturesToTrack(frame, mTrackPoints, 100, 0.01, 5, mask, 3, 0, 0.04);
    //if( mTrackPoints.size() > 0)
    //    cornerSubPix(frame, mTrackPoints, subPixWinSize, Size(-1,-1), termcrit);


    updateFace(frame);
}

bool Face::isSimilar(Rect r){

    //TODO: is middlepoint in circle?
    int a = mRect.x - center().x,
        b = mRect.y - center().y;
    int radius = sqrt(a*a+b*b);

    a = r.x - center().x,
    b = r.y - center().y;
    int distance = sqrt(a*a+b*b);

    //cout << radius << ", " << distance << endl;


    if( radius > distance)
        return true;

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

    if( mTrackPoints.size() < 4 )
        return false;

    mRect = boundingRect(mTrackPoints);

    //update face
    updateFace(curr);


    return true;
}

Point Face::center(){

    return Point(mRect.x+mRect.width/2,
                 mRect.y+mRect.height/2);

}

Rect Face::rect(){
    return mRect;
}

void Face::draw(Mat& frame, FaceDistance dist, bool features){

    // draw box + info
    ostringstream stream;
    stream << mID << ": " << mRect.width << "x" << mRect.height << "px, " << mTrackPoints.size() << "feat";

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

        //motionVector
        Point2f motionVec = mMotionVector;
        motionVec.x += center().x;
        motionVec.y += center().y;
        line(frame, center(), motionVec, Scalar(255, 255, 0), 2);

        int a = mRect.x - center().x,
            b = mRect.y - center().y;
        int radius = sqrt(a*a+b*b);
        circle(frame, center(), radius, Scalar(100, 100, 100));

        //Rect brect = boundingRect(mTrackPoints);
        //rectangle(frame, brect, Scalar(150, 150, 150), 1);
    }
}

void Face::updateFace(Mat& frame){
    Rect r = mRect;
    r.x = r.x < 0 ? 0 : r.x;
    r.x = r.x > frame.cols ? frame.cols : r.x;
    r.y = r.y < 0 ? 0 : r.y;
    r.y = r.y > frame.rows ? frame.rows : r.y;
    mFace = Mat(frame, r);

    ostringstream stream;
    stream << mID;
    imshow(stream.str(), mFace);
    waitKey(10);
}

int Face::id(){
    return mID;
}

int Face::duration(){
    return ( clock() - mStartTime ) / (double) CLOCKS_PER_SEC;
}

Point Face::motionVec(){
    return mMotionVector;
}
