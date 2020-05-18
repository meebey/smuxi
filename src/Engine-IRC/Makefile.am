
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = @MCS@
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
ASSEMBLY = ../../bin/release/smuxi-engine-irc.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../../bin/release/smuxi-common.dll \
	../../bin/release/smuxi-engine.dll
BUILD_DIR = ../../bin/release

SMUXI_ENGINE_DLL_MDB=
NINI_DLL_SOURCE=../../lib/Nini.dll
SMUXI_ENGINE_DLL_SOURCE=../../bin/release/smuxi-engine.dll
SMUXI_COMMON_DLL_SOURCE=../../bin/release/smuxi-common.dll

endif

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = @MCS@
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug -define:DEBUG "-define:DEBUG,TRACE,LOG4NET"
ASSEMBLY = ../../bin/debug/smuxi-engine-irc.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../../bin/debug/smuxi-common.dll \
	../../bin/debug/smuxi-engine.dll
BUILD_DIR = ../../bin/debug

SMUXI_ENGINE_DLL_MDB_SOURCE=../../bin/debug/smuxi-engine.dll.mdb
SMUXI_ENGINE_DLL_MDB=$(BUILD_DIR)/smuxi-engine.dll.mdb
NINI_DLL_SOURCE=../../lib/Nini.dll
SMUXI_ENGINE_DLL_SOURCE=../../bin/debug/smuxi-engine.dll
SMUXI_COMMON_DLL_SOURCE=../../bin/debug/smuxi-common.dll

endif

if BUNDLE_LOG4NET
LOG4NET_DLL_SOURCE=../../lib/log4net.dll
LOG4NET_REF_LIBS=-r:$(LOG4NET_DLL_SOURCE)
else
LOG4NET_REF_LIBS=$(LOG4NET_LIBS)
endif

AL=al2
SATELLITE_ASSEMBLY_NAME=.resources.dll

PROGRAMFILES = \
	$(LOG4NET_DLL) \
	$(NINI_DLL) \
	$(SMUXI_COMMON_DLL) \
	$(SMUXI_COMMON_DLL_MDB) \
	$(SMUXI_ENGINE_DLL) \
	$(SMUXI_ENGINE_DLL_MDB)

LINUX_PKGCONFIG = \
	$(ENGINE_IRC_PC)  

all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES = \
	Protocols/Irc/IrcGroupPersonModel.cs \
	Protocols/Irc/IrcMessageBuilder.cs \
	Protocols/Irc/IrcProtocolManager.cs \
	Protocols/Irc/IrcPersonModel.cs \
	Protocols/Irc/IrcTextColor.cs \
	Protocols/Irc/SlackProtocolManager.cs \
	Config/IrcServerModel.cs \
	../AssemblyVersion.cs \
	AssemblyInfo.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	smuxi-engine-irc.pc.in 

REFERENCES =  \
	System \
	System.Core \
	Mono.Posix \
	$(LOG4NET_REF_LIBS)

DLL_REFERENCES = \
    $(BUILD_DIR)/Meebey.SmartIrc4net.dll

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

ENGINE_IRC_PC = $(BUILD_DIR)/smuxi-engine-irc.pc
SMUXI_ENGINE_DLL = $(BUILD_DIR)/smuxi-engine.dll
SMUXI_COMMON_DLL = $(BUILD_DIR)/smuxi-common.dll

$(eval $(call emit-deploy-wrapper,ENGINE_IRC_PC,smuxi-engine-irc.pc))
$(eval $(call emit-deploy-target,SMUXI_ENGINE_DLL_MDB))
$(eval $(call emit-deploy-target,SMUXI_ENGINE_DLL))
$(eval $(call emit-deploy-target,SMUXI_COMMON_DLL))


$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(build_resx_resources) : %.resources: %.resx
	resgen2 '$<' '$@'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(dir $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
