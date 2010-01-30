TARGET_DIR = bin/Debug
OBJECT_DIR = obj
SOURCE_PATTERNS = *.cs */*.cs
SOURCE_FILES = $(foreach pattern, $(SOURCE_PATTERNS), $(wildcard $(pattern)))
ASSEMBLY_NAME = Twitterizer.Framework.dll
ASSEMBLY_TARGET = $(TARGET_DIR)/$(ASSEMBLY_NAME)
KEY_FILE = $(srcdir)/Twitterizer.Framework.snk
BUILD_FILE = $(srcdir)/Twitterizer.Framework.vs2008.csproj

# magic automake variables
pkglib_DATA = $(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb
EXTRA_DIST = $(KEY_FILE) $(BUILD_FILE)
# end of magic

all: $(ASSEMBLY_TARGET)

$(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb: $(KEY_FILE) $(BUILD_FILE) $(SOURCE_FILES)
	$(XBUILD) /p:Configuration=Debug $(BUILD_FILE)

clean-local:
	$(XBUILD) /t:Clean $(BUILD_FILE)
