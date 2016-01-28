TARGET_DIR = $(top_builddir)/bin/$(PROFILE)
ASSEMBLY_NAME = smuxi-engine-xmpp
ASSEMBLY_FILENAME = $(ASSEMBLY_NAME).dll
ASSEMBLY_TARGET = $(TARGET_DIR)/$(ASSEMBLY_FILENAME)

SOURCES = \
	../AssemblyVersion.cs \
	AssemblyInfo.cs \
	Config/XmppServerModel.cs \
	Protocols/Xmpp/XmppGroupChatModel.cs \
	Protocols/Xmpp/XmppProtocolManager.cs \
	Config/XmppPersonModel.cs \
	Protocols/Xmpp/FacebookProtocolManager.cs

REFERENCES = \
	System \
	System.Core \
	Mono.Posix \
	$(LOG4NET_LIBS)

DLL_REFERENCES = \
	$(TARGET_DIR)/smuxi-common.dll \
	$(TARGET_DIR)/smuxi-engine.dll \
	$(TARGET_DIR)/StarkSoftProxy.dll \
	$(TARGET_DIR)/agsxmpp.dll

SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES))

# automake magic variables
EXTRA_DIST = $(SOURCES_BUILD)
CLEANFILES = $(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb

pkgapp_DATA = $(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb

include $(top_srcdir)/Makefile.include

all: $(ASSEMBLY_TARGET)

$(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb: $(SOURCES) $(DLL_REFERENCES)
	$(CSC) $(CSC_FLAGS) $(build_references_ref) -target:library -out:$(ASSEMBLY_TARGET) $(SOURCES_BUILD)
