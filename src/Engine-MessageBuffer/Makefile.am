TARGET_DIR = $(top_builddir)/bin/$(PROFILE)
BIN_WRAPPER = smuxi-message-buffer
ASSEMBLY_NAME = smuxi-message-buffer
ASSEMBLY_FILENAME = $(ASSEMBLY_NAME).exe
ASSEMBLY_CONFIG_SOURCE = $(ASSEMBLY_FILENAME).config
ASSEMBLY_CONFIG = $(TARGET_DIR)/$(ASSEMBLY_CONFIG_SOURCE)
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

PROGRAMFILES = $(ASSEMBLY_CONFIG)

BINARIES = $(BIN_WRAPPER)

# automake magic variables
EXTRA_DIST = $(SOURCES_BUILD) $(man_MANS) $(ASSEMBLY_CONFIG_SOURCE)
CLEANFILES = \
	$(PROGRAMFILES) \
	$(BINARIES) \
	$(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb

pkgapp_DATA = $(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb
man_MANS = smuxi-message-buffer.1

include $(top_srcdir)/Makefile.include

all: $(ASSEMBLY_TARGET)

$(eval $(call emit-deploy-target,ASSEMBLY_CONFIG))

$(ASSEMBLY_TARGET): $(SOURCES) $(DLL_REFERENCES)
	$(CSC) $(CSC_FLAGS) $(MSG_BUFFER_COMPILER_FLAGS) $(build_references_ref) -target:exe -out:$(ASSEMBLY_TARGET) $(SOURCES_BUILD)
