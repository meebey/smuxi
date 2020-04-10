TARGET_DIR = $(top_builddir)/bin/$(PROFILE)
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = @MCS@
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:CONFIG_NINI" -define:DB4O,DB4O_8_0

ASSEMBLY = ../../bin/release/smuxi-engine.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../../bin/release/smuxi-common.dll
BUILD_DIR = ../../bin/release

SMUXI_COMMON_DLL_SOURCE=../../bin/release/smuxi-common.dll

endif

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = @MCS@
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug -define:DEBUG "-define:DEBUG,TRACE,LOG4NET,CONFIG_NINI" -define:DB4O,DB4O_8_0

ASSEMBLY = ../../bin/debug/smuxi-engine.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../../bin/debug/smuxi-common.dll
BUILD_DIR = ../../bin/debug

SMUXI_COMMON_DLL_SOURCE=../../bin/debug/smuxi-common.dll

endif

if BUNDLE_LOG4NET
LOG4NET_DLL_SOURCE=../../lib/log4net.dll
LOG4NET_REF_LIBS=-r:$(LOG4NET_DLL_SOURCE)
else
LOG4NET_REF_LIBS=$(LOG4NET_LIBS)
endif

if BUNDLE_NINI_BLOB
NINI_DLL_SOURCE=../../lib/Nini.dll
NINI_REF_LIBS=-r:$(NINI_DLL_SOURCE)
else
NINI_REF_LIBS=$(NINI_LIBS)
endif

AL=al2
SATELLITE_ASSEMBLY_NAME=.resources.dll

PROGRAMFILES = \
	$(SMUXI_COMMON_DLL)

LINUX_PKGCONFIG = \
	$(ENGINE_PC)  

all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES = \
	../AssemblyVersion.cs \
	AssemblyInfo.cs \
	CertificateValidator.cs \
	CommandModel.cs \
	Engine.cs \
	FrontendManager.cs \
	IFrontendUI.cs \
	PermanentRemoteObject.cs \
	PresenceStatus.cs \
	Session.cs \
	SessionManager.cs \
	TextColor.cs \
	TextColorContrast.cs \
	TextColorPalettes.cs \
	TextColorTools.cs \
	MessageBuffers/Db4oMessageBuffer.cs \
	MessageBuffers/IMessageBuffer.cs \
	MessageBuffers/ListMessageBuffer.cs \
	MessageBuffers/MessageBufferBase.cs \
	MessageBuffers/MessageBufferPersistencyType.cs \
	MessageBuffers/SqliteMessageBuffer.cs \
	Messages/FeedMessageBuilder.cs \
	Messages/MessageBuilder.cs \
	Messages/MessageModel.cs \
	Messages/MessagePartModel.cs \
	Messages/TextMessagePartModel.cs \
	Messages/UrlMessagePartModel.cs \
	Messages/ImageMessagePartModel.cs \
	Messages/MessageType.cs \
	Messages/Dto/MessageDtoModelV1.cs \
	Messages/Dto/MessageDtoModelV2.cs \
	Chats/ChatType.cs \
	Chats/ChatModel.cs \
	Chats/GroupChatModel.cs \
	Chats/PersonChatModel.cs \
	Chats/SessionChatModel.cs \
	Chats/ProtocolChatModel.cs \
	Config/FrontendConfig.cs \
	Config/NotificationAreaIconMode.cs \
	Config/UserConfig.cs \
	Config/Config.cs \
	Config/ServerModel.cs \
	Config/ServerListController.cs \
	Config/UserListController.cs \
	Config/FilterListController.cs \
	Config/FilterModel.cs \
	Config/ProxySettings.cs \
	Config/ProxyType.cs \
	Config/EntrySettings.cs \
	Config/MessageBuilderSettings.cs \
	Config/MessagePatternListController.cs \
	Config/MessagePatternModel.cs \
	Protocols/ProtocolManagerBase.cs \
	Protocols/ProtocolManagerFactory.cs \
	Protocols/ProtocolManagerInfoModel.cs \
	Protocols/ProtocolManagerInfoAttribute.cs \
	Protocols/IProtocolManager.cs \
	Persons/PersonModel.cs \
	Persons/ContactModel.cs \
	Hooks/HookRunner.cs \
	Hooks/Commands/HookCommand.cs \
	Hooks/Commands/ProtocolManagerHookCommand.cs \
	Hooks/Commands/SessionHookCommand.cs \
	Hooks/Environments/ChatHookEnvironment.cs \
	Hooks/Environments/CommandHookEnvironment.cs \
	Hooks/Environments/HookEnvironment.cs \
	Hooks/Environments/MessageHookEnvironment.cs \
	Hooks/Environments/PersonHookEnvironment.cs \
	Hooks/Environments/ProtocolManagerHookEnvironment.cs

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	smuxi-engine.pc.in 

REFERENCES =  \
	System \
	System.Core \
	System.Data \
	System.Runtime.Remoting \
	System.Runtime.Serialization \
	System.Web \
	System.Xml \
	Mono.Posix \
	Mono.Data.Sqlite \
	$(NINI_REF_LIBS) \
	$(LOG4NET_REF_LIBS) \
	$(DB4O_LIBS) \
	$(DB4O_INCLUDED_LIBS)

DLL_REFERENCES = \
	$(BUILD_DIR)/ServiceStack.Text.dll

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

ENGINE_PC = $(BUILD_DIR)/smuxi-engine.pc
SMUXI_COMMON_DLL = $(BUILD_DIR)/smuxi-common.dll

$(eval $(call emit-deploy-wrapper,ENGINE_PC,smuxi-engine.pc))
$(eval $(call emit-deploy-target,SMUXI_COMMON_DLL))

if BUNDLE_NINI
NINI_DLL = $(BUILD_DIR)/Nini.dll
NINI_DLL_SOURCE = $(NINI_LIBS)
PROGRAMFILES += $(NINI_DLL)
$(eval $(call emit-deploy-target,NINI_DLL))
endif

if BUNDLE_DB4O
DB4O_INCLUDED_LIBS := $(foreach file, $(DB4O_FILES), $(TARGET_DIR)/$(file))
PROGRAMFILES += $(DB4O_INCLUDED_LIBS)
endif

$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(build_resx_resources) : %.resources: %.resx
	resgen2 '$<' '$@'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(dir $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
