TARGET_DIR = $(top_builddir)/bin/$(PROFILE)
ASSEMBLY_NAME = smuxi-engine-twitter
ASSEMBLY_FILENAME = $(ASSEMBLY_NAME).dll
ASSEMBLY_TARGET = $(TARGET_DIR)/$(ASSEMBLY_FILENAME)
#ASSEMBLY_PC = $(top_srcdir)/$(PACKAGE_NAME).pc

SOURCES =	$(top_srcdir)/src/AssemblyVersion.cs \
		AssemblyInfo.cs \
		Protocols/Twitter/TwitterProtocolManager.cs \
		Protocols/Twitter/TwitterMessageBuilder.cs

REFERENCES = 		$(LOG4NET_LIBS) \
			$(SMARTIRC4NET_LIBS) \
			System.Web \
			System.Core

DLL_REFERENCES =	$(TARGET_DIR)/Twitterizer2.dll \
			$(TARGET_DIR)/smuxi-common.dll \
			$(TARGET_DIR)/smuxi-engine.dll

SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES))

# automake magic variables
EXTRA_DIST = $(SOURCES_BUILD)
CLEANFILES = $(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb

pkgapp_DATA = $(ASSEMBLY_TARGET)

#pkgconfigdir = $(libdir)/pkgconfig
#pkgconfig_DATA = $(ASSEMBLY_PC)

include $(top_srcdir)/Makefile.include

all: $(ASSEMBLY_TARGET)

$(ASSEMBLY_TARGET): $(SOURCES) $(DLL_REFERENCES)
	$(MCS) $(CSC_FLAGS) $(build_references_ref) -target:library -out:$@ $(SOURCES_BUILD)
