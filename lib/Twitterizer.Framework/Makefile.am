OUTPUT_DIR = $(top_builddir)/bin/$(PROFILE)
OBJECT_DIR = $(OUTPUT_DIR)/obj
SOURCE_PATTERNS = *.cs */*.cs
SOURCE_FILES = $(foreach pattern, $(SOURCE_PATTERNS), $(wildcard $(srcdir)/$(pattern)))
ASSEMBLY_NAME = Twitterizer.Framework.dll
ASSEMBLY_TARGET = $(OUTPUT_DIR)/$(ASSEMBLY_NAME)
KEY_FILE = $(srcdir)/Twitterizer.Framework.snk
BUILD_FILE = $(srcdir)/Twitterizer.Framework.vs2008.csproj
XBUILD_FLAGS = /p:Configuration=Debug /p:NoWarn=0168 /p:OutputPath=$(abspath $(OUTPUT_DIR)) /p:BaseIntermediateOutputPath=$(abspath $(OBJECT_DIR))/

# magic automake variables
if ENABLE_ENGINE_TWITTER
pkglib_DATA = $(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb
endif

EXTRA_DIST = $(SOURCE_FILES) $(KEY_FILE) $(BUILD_FILE)
# end of magic

$(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb: $(KEY_FILE) $(BUILD_FILE) $(SOURCE_FILES)
	$(XBUILD) $(XBUILD_FLAGS) $(BUILD_FILE)

if ENABLE_ENGINE_TWITTER
clean-local:
	$(XBUILD) $(XBUILD_FLAGS) /t:Clean $(BUILD_FILE)
endif
