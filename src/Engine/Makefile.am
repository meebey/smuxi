
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = @MCS@
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:CONFIG_NINI"

ASSEMBLY = ../../bin/release/smuxi-engine.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../../bin/release/smuxi-common.dll
BUILD_DIR = ../../bin/release

LOG4NET_DLL_SOURCE=../../lib/log4net.dll
NINI_DLL_SOURCE=../../lib/Nini.dll
SMUXI_COMMON_DLL_SOURCE=../../bin/release/smuxi-common.dll

endif

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = @MCS@
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug -define:DEBUG "-define:DEBUG,TRACE,LOG4NET,CONFIG_NINI"

ASSEMBLY = ../../bin/debug/smuxi-engine.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../../bin/debug/smuxi-common.dll
BUILD_DIR = ../../bin/debug

LOG4NET_DLL_SOURCE=../../lib/log4net.dll
NINI_DLL_SOURCE=../../lib/Nini.dll
SMUXI_COMMON_DLL_SOURCE=../../bin/debug/smuxi-common.dll

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=.resources.dll

PROGRAMFILES = \
	$(SMUXI_COMMON_DLL)  

LINUX_PKGCONFIG = \
	$(ENGINE_PC)  
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES = \
	$(top_srcdir)/src/AssemblyVersion.cs \
	AssemblyInfo.cs \
	CommandModel.cs \
	Engine.cs \
	FrontendManager.cs \
	IFrontendUI.cs \
	PermanentRemoteObject.cs \
	Session.cs \
	SessionManager.cs \
	TextColor.cs \
	UICommand.cs \
	UICommandContainer.cs \
	Messages/MessageModel.cs \
	Messages/MessagePartModel.cs \
	Messages/TextMessagePartModel.cs \
	Messages/UrlMessagePartModel.cs \
	Messages/ImageMessagePartModel.cs \
	Messages/MessageType.cs \
	Chats/ChatType.cs \
	Chats/ChatModel.cs \
	Chats/GroupChatModel.cs \
	Chats/PersonChatModel.cs \
	Chats/SessionChatModel.cs \
	Chats/ProtocolChatModel.cs \
	Config/FrontendConfig.cs \
	Config/ChannelFilterModel.cs \
	Config/ChannelFilterListController.cs \
	Config/NotificationAreaIconMode.cs \
	Config/UserConfig.cs \
	Config/Config.cs \
	Config/ServerModel.cs \
	Config/ServerListController.cs \
	Protocols/ProtocolManagerBase.cs \
	Protocols/ProtocolManagerFactory.cs \
	Protocols/ProtocolManagerInfoModel.cs \
	Protocols/ProtocolManagerInfoAttribute.cs \
	Protocols/IProtocolManager.cs \
	Persons/PersonModel.cs \
	Persons/ContactModel.cs

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	smuxi-engine.pc.in 

REFERENCES =  \
	System.Runtime.Remoting \
	System \
	Mono.Posix

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

ENGINE_PC = $(BUILD_DIR)/smuxi-engine.pc
SMUXI_COMMON_DLL = $(BUILD_DIR)/smuxi-common.dll

$(eval $(call emit-deploy-wrapper,ENGINE_PC,smuxi-engine.pc))
$(eval $(call emit-deploy-target,SMUXI_COMMON_DLL))


$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(build_resx_resources) : %.resources: %.resx
	resgen2 '$<' '$@'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(dir $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref) $(NINI_LIBS) $(LOG4NET_LIBS)
