TARGET_DIR = $(top_builddir)/bin/$(PROFILE)
ASSEMBLY_NAME = smuxi-frontend-gnome-twitter
ASSEMBLY_FILENAME = $(ASSEMBLY_NAME).dll
ASSEMBLY_TARGET = $(TARGET_DIR)/$(ASSEMBLY_FILENAME)

SOURCES = \
	../AssemblyVersion.cs \
	AssemblyInfo.cs \
	TwitterGroupChatView.cs

REFERENCES = \
	System \
	System.Core \
	Mono.Posix \
	$(LOG4NET_LIBS) \
	$(GTK_SHARP_20_LIBS)

DLL_REFERENCES = \
	$(TARGET_DIR)/smuxi-common.dll \
	$(TARGET_DIR)/smuxi-engine.dll \
	$(TARGET_DIR)/smuxi-engine-twitter.dll \
	$(TARGET_DIR)/smuxi-frontend.dll \
	$(TARGET_DIR)/smuxi-frontend-gnome.exe

SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES))

# automake magic variables
EXTRA_DIST = $(SOURCES_BUILD)
CLEANFILES = $(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb

pkgapp_DATA = $(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb

include $(top_srcdir)/Makefile.include

all: $(ASSEMBLY_TARGET)

$(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb: $(SOURCES) $(DLL_REFERENCES)
	$(CSC) $(CSC_FLAGS) $(build_references_ref) -target:library -out:$(ASSEMBLY_TARGET) $(SOURCES_BUILD)
