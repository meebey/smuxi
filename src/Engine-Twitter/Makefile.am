TARGET_DIR = $(top_srcdir)/bin/$(PROFILE)
ASSEMBLY_NAME = smuxi-engine-twitter
ASSEMBLY_FILENAME = $(ASSEMBLY_NAME).dll
ASSEMBLY_TARGET = $(TARGET_DIR)/$(ASSEMBLY_FILENAME)
#ASSEMBLY_PC = $(top_srcdir)/$(PACKAGE_NAME).pc
SOURCE_FILES = 	$(top_srcdir)/src/AssemblyVersion.cs \
		AssemblyInfo.cs \
		Protocols/Twitter/TwitterProtocolManager.cs
DLL_REFERENCES =	$(top_srcdir)/lib/Twitterizer.Framework/bin/Debug/Twitterizer.Framework.dll \
			$(LOG4NET_LIBS) \
			$(SMARTIRC4NET_LIBS) \
			$(TARGET_DIR)/smuxi-common.dll \
			$(TARGET_DIR)/smuxi-engine.dll

# automake magic variables
EXTRA_DIST = $(SOURCE_FILES)
CLEANFILES = $(ASSEMBLY_TARGET)

pkglib_DATA = $(ASSEMBLY_TARGET)

#pkgconfigdir = $(libdir)/pkgconfig
#pkgconfig_DATA = $(ASSEMBLY_PC)

include $(top_srcdir)/Makefile.include

all: $(ASSEMBLY_TARGET)

$(ASSEMBLY_TARGET): $(SOURCE_FILES) $(DLL_REFERENCES)
	$(MCS) $(CSC_FLAGS) $(build_references_ref) -target:library -out:$@ $(SOURCE_FILES)
