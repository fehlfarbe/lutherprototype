#include "logger.h"

Logger::Logger(string filename)
{
    struct stat buffer;
    int i = 0;

    ostringstream stream;
    stream << filename;

    while( stat (stream.str().c_str(), &buffer) == 0){
        stream.clear();
        stream.str("");

        stream << i++ << "_" << filename;
        cout << stream.str() << endl;
    }


    mOutStream.open(stream.str().c_str(), std::ofstream::out | std::ofstream::app);
    mOutStream << "id, start, end, time" << endl;

}

Logger::~Logger(){

    mOutStream.close();

}

void Logger::log(const char *str){
    mOutStream << str << endl;
}
