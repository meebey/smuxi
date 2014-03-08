TARGET_DIR = $(top_builddir)/bin/$(PROFILE)
BIN_WRAPPER = smuxi-message-buffer
ASSEMBLY_NAME = smuxi-message-buffer
ASSEMBLY_FILENAME = $(ASSEMBLY_NAME).exe
ASSEMBLY_TARGET = $(TARGET_DIR)/$(ASSEMBLY_FILENAME)

SOURCES = \
	$(top_srcdir)/src/AssemblyVersion.cs \
	AssemblyInfo.cs \
	Main.cs

REFERENCES = \
	System \
	System.Core \
	Mono.Posix \
	$(LOG4NET_LIBS)

DLL_REFERENCES = \
	$(TARGET_DIR)/smuxi-common.dll \
	$(TARGET_DIR)/smuxi-engine.dll \
	$(TARGET_DIR)/ServiceStack.Text.dll

SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES))

BINARIES = $(BIN_WRAPPER)

# automake magic variables
EXTRA_DIST = $(SOURCES_BUILD) $(man_MANS)
CLEANFILES = $(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb

pkgapp_DATA = $(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb
man_MANS = smuxi-message-buffer.1

include $(top_srcdir)/Makefile.include

all: $(ASSEMBLY_TARGET)

$(ASSEMBLY_TARGET): $(SOURCES) $(DLL_REFERENCES)
	$(CSC) $(CSC_FLAGS) $(MSG_BUFFER_COMPILER_FLAGS) $(build_references_ref) -target:exe -out:$@ $(SOURCES_BUILD)
