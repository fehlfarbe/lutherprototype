from simpleOSC import initOSCServer, setOSCHandler, closeOSC, startOSCServer
import sys



def newface(addr, tags, data, source):
    print "New face with ID %s detected" % data

def deleteface(addr, tags, data, source):
    print "Face with ID %s escaped" % data

def facelist(addr, tags, data, source):
    print "Face %s position: x:%s y:%s, motion: x:%sy:%s" % (data[0], data[1], data[2], data[3], data[4])

def end(addr, tags, data, source):
	closeOSC()
	print "Bye"
	sys.exit()


if __name__ == '__main__':

	initOSCServer('127.0.0.1', 7000)
	setOSCHandler('/newface', newface)
	setOSCHandler('/deleteface', deleteface)
	setOSCHandler('/facelist', facelist)
	setOSCHandler('/end', end)
	startOSCServer()

	while True:
	    try:
		pass
	    except KeyboardInterrupt:
		closeOSC()
		print "Bye"
		sys.exit()
