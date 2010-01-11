ASSEMBLY_TARGET = bin/Debug/Twitterizer.Framework.dll

pkglib_DATA = $(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb

CLEANFILES = obj/Twitterizer.Framework.vs2008.csproj.FilesWrittenAbsolute.txt

all: $(ASSEMBLY_TARGET)

$(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb:
	$(XBUILD) Twitterizer.Framework.vs2008.csproj

clean:
	$(XBUILD) /t:Clean Twitterizer.Framework.vs2008.csproj
