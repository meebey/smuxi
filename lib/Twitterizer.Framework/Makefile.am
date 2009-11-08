ASSEMBLY_TARGET = bin/Debug/Twitterizer.Framework.dll

pkglib_DATA = $(ASSEMBLY_TARGET)

all: $(ASSEMBLY_TARGET)

$(ASSEMBLY_TARGET):
	xbuild Twitterizer.Framework.vs2008.csproj

clean:
	xbuild /t:Clean Twitterizer.Framework.vs2008.csproj
