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


    /*******************************
     *
     * Feature detection
     *
     ******************************/
    //convert to grayscale for corner detecttion
    Mat frame_gray;
    if( frame.channels() > 1)
        cvtColor(frame, frame_gray, CV_BGR2GRAY);
    else
        frame_gray = frame;

    //equalize histogram
    equalizeHist(frame_gray, frame_gray);

    Mat mask(frame_gray.size(), CV_8UC1);
    mask.setTo(Scalar::all(0));
    rectangle(mask, r, Scalar(255,255,255), -1);


    mTrackPoints.clear();
    goodFeaturesToTrack(frame_gray, mTrackPoints, 100, 0.01, 5, mask, 3, 0, 0.04);
    if( mTrackPoints.size() > 0)
        cornerSubPix(frame_gray, mTrackPoints, subPixWinSize, Size(-1,-1), termcrit);

    /********************************
     *
     * Camshift Histogram
     *
     ********************************/
    //Mat histimg = Mat::zeros(200, 320, CV_8UC3);
    Mat hsv, hue, backProjection;
    int hsize = 16;
    float hranges[] = {0,180};
    const float* phranges = hranges;

    cvtColor(frame, hsv, COLOR_BGR2HSV);

    int ch[] = {0, 0};
    hue.create(hsv.size(), hsv.depth());
    mixChannels(&hsv, 1, &hue, 1, ch, 1);

    Rect frame_rect = Rect(0, 0, frame.cols, frame.rows);
    Mat roi(hue, r & frame_rect), maskroi(mask, r & frame_rect);

    calcHist(&roi, 1, 0, maskroi, mHist, 1, &hsize, &phranges);
    normalize(mHist, mHist, 0, 255, CV_MINMAX);

    //draw hist
//    histimg = Scalar::all(0);
//    int binW = histimg.cols / hsize;
//    Mat buf(1, hsize, CV_8UC3);
//    for( int i = 0; i < hsize; i++ )
//        buf.at<Vec3b>(i) = Vec3b(saturate_cast<uchar>(i*180./hsize), 255, 255);
//    cvtColor(buf, buf, CV_HSV2BGR);

//    for( int i = 0; i < hsize; i++ )
//    {
//        int val = saturate_cast<int>(hist.at<float>(i)*histimg.rows/255);
//        rectangle( histimg, Point(i*binW,histimg.rows),
//                   Point((i+1)*binW,histimg.rows - val),
//                   Scalar(buf.at<Vec3b>(i)), -1, 8 );
//    }

    calcBackProject(&hue, 1, 0, mHist, backProjection, &phranges);
    backProjection &= mask;
    mTrackBox = CamShift(backProjection, r, TermCriteria( CV_TERMCRIT_EPS | CV_TERMCRIT_ITER, 10, 1 ));

//    imshow("hist", mBackProjection);
//    waitKey(100);


    updateFace(frame);
}

bool Face::isSimilar(Rect r){

//    //TODO: is middlepoint in circle?
//    int a = mRect.x - center().x,
//        b = mRect.y - center().y;
//    int radius = sqrt(double(a*a+b*b));

//    a = r.x - center().x,
//    b = r.y - center().y;
//    int distance = sqrt(double(a*a+b*b));

//    //cout << radius << ", " << distance << endl;


//    if( radius > distance)
//        return true;

    Rect intersect = r & mRect;
    if( intersect.size().area() > 0){
        return true;
    }

    return false;
}

bool Face::isSimilar(Face f){

    Rect r = f.rect() & mRect;

    if( r.area() >= mRect.area() || r.area() >= f.rect().area() )
        return true;

    return false;
}

bool Face::track(Mat& prev, Mat& curr){

    /*********************************
     *
     * Camshift
     *
     *********************************/
    Rect window = mRect;

    Mat hsv, hue, backProjection;
    float hranges[] = {0,180};
    const float* phranges = hranges;

    cvtColor(curr, hsv, COLOR_BGR2HSV);
    int ch[] = {0, 0};
    hue.create(hsv.size(), hsv.depth());
    mixChannels(&hsv, 1, &hue, 1, ch, 1);
    calcBackProject(&hue, 1, 0, mHist, backProjection, &phranges);
    //mBackProjection &= mask;

    Mat mask(curr.size(), CV_8UC1);
    mask.setTo(Scalar::all(0));
    rectangle(mask, mRect, Scalar(255,255,255), -1);
    backProjection &= mask;

    mTrackBox = CamShift(backProjection, window, TermCriteria( CV_TERMCRIT_EPS | CV_TERMCRIT_ITER, 10, 1 ));

//    imshow("Backproj" + mID, backProjection);
//    waitKey(10);


    /*********************************
     *
     * optical flow
     *
     *********************************/

    //convert to gray images
    Mat current, previous;
    if( curr.channels() > 1)
        cvtColor(curr, current, CV_BGR2GRAY);
    else
        current = curr;
    if( prev.channels() > 1)
        cvtColor(prev, previous, CV_BGR2GRAY);
    else
        previous = prev;

    //equalize histograms
    equalizeHist(current, current);
    equalizeHist(previous, previous);

    if( mTrackPoints.empty() )
        return false;

    vector<uchar> status;
    vector<float> err;
    vector<Point2f> newPoints;

    if(previous.empty())
        current.copyTo(previous);

    calcOpticalFlowPyrLK(previous, current, mTrackPoints, newPoints, status, err, winSize, 3, termcrit, 0, 0.001);

    size_t i, k;
    int notfound = 0;
    Point2f motionVec;

    //look recognized features
    for( i = k = 0; i < newPoints.size(); i++ )
    {

        if( !status[i] ){
            notfound++;
        }

        //test if movement of feature is too big
        Point2f vec = newPoints[i]-mTrackPoints[i];
        if( !mTrackBox.boundingRect().contains(mTrackPoints[i]) ){
            status[i] = 0;
            newPoints[k++] = mTrackPoints[i];
        } else {
            motionVec += vec;
            newPoints[k++] = newPoints[i];
        }
    }

    //motion vector
    motionVec.x /= 10;
    motionVec.y /= 10;
    mMotionVector = motionVec;

    //setup point lists
    newPoints.resize(k);
    swap(mTrackPoints, newPoints);
    mStatus = status;

    if( notfound > mTrackPoints.size()/2 )
        return false;


    /*********************************
     *
     * combine optical flow & camshift rects
     *
     *********************************/
    Rect r = boundingRect(mTrackPoints) & mTrackBox.boundingRect();
    //cout << r << " " << mRect << endl;
    if( r.width  > 10 && r.height > 10 )
       mRect = r;
    else
       mRect = boundingRect(mTrackPoints);

    if( mRect.width > maxWidthHeight || mRect.height > maxWidthHeight){
        Point2f mp = Point(mRect.x + mRect.width / 2, mRect.y + mRect.height / 2);
        mRect = Rect(mp.x-maxWidthHeight/2, mp.y-maxWidthHeight/2, maxWidthHeight, maxWidthHeight);
    }

    //update face
    updateFace(current);

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

    /******************************
     *
     * draw box + info
     *
     ******************************/
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

    /***************************
     *
     * draw Camshift
     *
     ***************************/
    ellipse( frame, mTrackBox, mColor, 1, CV_AA );

    //draw hist
    Rect r = Rect(0, 0, frame.cols, frame.rows);
    Rect histRect = Rect(mRect.x, mRect.y + mRect.height / 2, mRect.width / 2, mRect.height / 2);
    Mat histimg = frame(histRect & r);
    int hsize = 16;
    int binW = histimg.cols / hsize;
    Mat buf(1, hsize, CV_8UC3);
    for( int i = 0; i < hsize; i++ )
        buf.at<Vec3b>(i) = Vec3b(saturate_cast<uchar>(i*180./hsize), 255, 255);
    cvtColor(buf, buf, CV_HSV2BGR);

    for( int i = 0; i < hsize; i++ )
    {
        int val = saturate_cast<int>(mHist.at<float>(i)*histimg.rows/255);
        rectangle( histimg, Point(i*binW,histimg.rows),
                   Point((i+1)*binW,histimg.rows - val),
                   Scalar(buf.at<Vec3b>(i)), -1, 8 );
    }


    /***************************
     *
     * draw features
     *
     **************************/
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


//    ostringstream stream;
//    stream << mID;
//    imshow(stream.str(), mFace);
//    waitKey(10);
}

int Face::id(){
    return mID;
}

int Face::duration(time_t *start, time_t *end){
    time(end);
    (*start) = mStartTime;

    return ( (*end) - mStartTime );
}

Point Face::motionVec(){
    return mMotionVector;
}
