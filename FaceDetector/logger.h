#ifndef LOGGER_H
#define LOGGER_H

#include <iostream>
#include <fstream>
#include <sstream>
#include <sys/stat.h>

using namespace std;


class Logger
{
public:
    Logger(string filename);
    ~Logger();

    void log(const char *str);


private:
    ofstream mOutStream;
};

#endif // LOGGER_H
