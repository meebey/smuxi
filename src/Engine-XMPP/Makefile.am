TARGET_DIR = $(top_builddir)/bin/$(PROFILE)
ASSEMBLY_NAME = smuxi-engine-xmpp
ASSEMBLY_FILENAME = $(ASSEMBLY_NAME).dll
ASSEMBLY_TARGET = $(TARGET_DIR)/$(ASSEMBLY_FILENAME)

SOURCES = \
	$(top_srcdir)/src/AssemblyVersion.cs \
	AssemblyInfo.cs \
	Config/XmppServerModel.cs \
	Protocols/Xmpp/XmppGroupChatModel.cs \
	Protocols/Xmpp/XmppProtocolManager.cs \
	Config/XmppPersonModel.cs \
	Protocols/Xmpp/IQ/OwnMessage.cs

SMARTIRC4NET_LIBS = $(foreach file, $(SMARTIRC4NET_FILES), $(top_builddir)/$(file))
SMARTIRC4NET_DLL_SOURCE = $(SMARTIRC4NET_LIBS)
SMARTIRC4NET_DLL = $(foreach file, $(SMARTIRC4NET_FILES), $(BUILD_DIR)/$(shell basename $(file)))
SMARTIRC4NET_DLL_MDB_SOURCE = $(foreach file, $(SMARTIRC4NET_DLL_SOURCE), $(file).mdb)
SMARTIRC4NET_DLL_MDB = $(foreach file, $(SMARTIRC4NET_DLL), $(file).mdb)

REFERENCES = \
	System \
	Mono.Posix \
	$(SMARTIRC4NET_LIBS) \
	$(LOG4NET_LIBS)

DLL_REFERENCES = \
	$(TARGET_DIR)/smuxi-common.dll \
	$(TARGET_DIR)/smuxi-engine.dll \
	$(TARGET_DIR)/agsXMPP.dll

SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES))

# automake magic variables
EXTRA_DIST = $(SOURCES_BUILD)
CLEANFILES = $(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb

pkgapp_DATA = $(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb

include $(top_srcdir)/Makefile.include

all: $(ASSEMBLY_TARGET)

$(ASSEMBLY_TARGET) $(ASSEMBLY_TARGET).mdb: $(SOURCES) $(DLL_REFERENCES)
	$(CSC) $(CSC_FLAGS) $(build_references_ref) -target:library -out:$@ $(SOURCES_BUILD)
