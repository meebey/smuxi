# $Id: Makefile 72 2005-06-24 15:19:19Z meebey $
# $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Makefile $
# $Rev: 72 $
# $Author: meebey $
# $Date: 2005-06-24 17:19:19 +0200 (Fri, 24 Jun 2005) $

CSC=mcs
TARGET=BidirectionalTCPChannel.dll

all: $(TARGET)

$(TARGET): *.cs
	$(CSC) /target:library /r:System.Runtime.Remoting /out:$(TARGET) $^

clean:
	rm -f $(TARGET)

.PHONY: all clean
