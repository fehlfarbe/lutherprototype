#include "face.h"

Face::Face(Rect r, Mat &frame, FaceType type)
{
    //set ID
    static int  counter = 0;
    mID = counter++;

    //set Starttime
    time(&mStartTime);

	middleDistance = 0;
    nearDistance = 0;

    //set color
    RNG rng(mStartTime);
    mColor = Scalar( rng.uniform(0,255), rng.uniform(0,255), rng.uniform(0,255));

    //initialize features
	subPixWinSize = Size(5,5);
    winSize = Size(11,11);
    termcrit = TermCriteria(CV_TERMCRIT_ITER|CV_TERMCRIT_EPS, 20, 0.03);

    cout << "new face" << endl;
    update(r, frame, type);
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
    int radius = sqrt(double(a*a+b*b));

    a = r.x - center().x,
    b = r.y - center().y;
    int distance = sqrt(double(a*a+b*b));

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
    int notfound = 0;
    Point2f motionVec;

    for( i = k = 0; i < newPoints.size(); i++ )
    {

        if( !status[i] ){
            notfound++;
        }

        //test if movement of feature is too big
        Point2f vec = newPoints[i]-mTrackPoints[i];
        double dist = sqrt(vec.x*vec.x+vec.y+vec.y);
        if( dist > mRect.width || dist > mRect.height){
            status[i] = 0;
            newPoints[k++] = mTrackPoints[i];
        } else {
            motionVec += vec;
            newPoints[k++] = newPoints[i];
        }

    }

    motionVec.x /= k;
    motionVec.y /= k;
    mMotionVector = motionVec;

    newPoints.resize(k);
    swap(mTrackPoints, newPoints);
    mStatus = status;

    if( notfound > mTrackPoints.size()/2 )
        return false;

    mRect = boundingRect(mTrackPoints);

    /*
    if( mRect.width >= maxWidthHeight ){
        mRect.x = mRect.x + mRect.width / 2 - maxWidthHeight / 2;
        mRect.width = maxWidthHeight;
    }
    if( mRect.height >= maxWidthHeight ){
        mRect.y = mRect.y + mRect.height / 2 - maxWidthHeight / 2;
        mRect.height = maxWidthHeight;
    }
    */

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
    stream << "[" << mID << "] " << mRect.width << "x" << mRect.height << "px, " << mTrackPoints.size() << "feat";

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
    rectangle(frame, mRect, mColor, 2);

    //
    double pctMaxWidth = (100.0 / maxWidthHeight) * mRect.width;
    double w = mRect.width * pctMaxWidth / 100.0;
//    pctMaxWidth = (100.0 / maxWidthHeight) * mRect.height;
//    double h = mRect.height * pctMaxWidth / 100.0;
    rectangle(frame, Rect(mRect.x - 1, mRect.y + mRect.height + 2, w, 5), Scalar(c), -1);
//    rectangle(frame, Rect(mRect.x + mRect.width + 2, mRect.y - 1, 5, h), Scalar(c), -1);

    //circle(frame, Point(mRect.x, mRect.y), 8, c, -1);


    //draw features
    if( features ){
        for(unsigned int i=0; i < mTrackPoints.size(); i++){
            Scalar color = Scalar(0, 255, 0);

            //draw red circle if feature wasn't found
            if( mStatus.size() == mTrackPoints.size())
                if( !mStatus[i] )
                    color = Scalar(0, 0, 200);

            circle( frame, mTrackPoints[i], 1, color, -1, 8);
        }

        //motionVector
        Point2f motionVec = mMotionVector;
        motionVec.x += center().x;
        motionVec.y += center().y;
        line(frame, center(), motionVec, Scalar(255, 255, 0), 1);

        /*
        int a = mRect.x - center().x,
            b = mRect.y - center().y;
        int radius = sqrt(double(a*a+b*b));
        circle(frame, center(), radius, Scalar(100, 100, 100));
        */
    }
}

void Face::updateFace(Mat& frame){
    Rect r = mRect;

    //cout << r.x + r.width << "/" << frame.cols;
    r.x = r.x < 0 ? 0 : r.x;
    r.x = r.x >= frame.cols ? frame.cols-1 : r.x;
    r.width = r.x + r.width >= frame.cols ? frame.cols-1-r.x : r.width;

    r.y = r.y < 0 ? 0 : r.y;
    r.y = r.y >= frame.rows ? frame.rows-1 : r.y;
    r.height = r.y + r.height >= frame.rows ? frame.rows-1-r.y : r.height;

    mFace = Mat(frame, r);


/*    ostringstream stream;
    stream << mID;
    imshow(stream.str(), mFace);
    waitKey(10)*/;
}

int Face::id(){
    return mID;
}

int Face::duration(){
    time_t end;
    time(&end);
    return ( end - mStartTime );
}

Point Face::motionVec(){
    return mMotionVector;
}
