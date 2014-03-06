EXTRA_DIST = $(man_MANS)
man_MANS = smuxi-message-buffer.1

TARGET_DIR = $(top_builddir)/bin/$(PROFILE)
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

# automake magic variables
EXTRA_DIST = $(SOURCES_BUILD)
CLEANFILES = $(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb

pkgapp_DATA = $(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb

include $(top_srcdir)/Makefile.include

all: $(ASSEMBLY_TARGET)

$(ASSEMBLY_TARGET): $(SOURCES) $(DLL_REFERENCES)
	$(CSC) $(CSC_FLAGS) $(MSG_BUFFER_COMPILER_FLAGS) $(build_references_ref) -target:exe -out:$@ $(SOURCES_BUILD)
