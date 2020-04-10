
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = @MCS@
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
ASSEMBLY = ../../bin/release/smuxi-frontend-gnome-irc.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../../bin/release/smuxi-common.dll \
	../../bin/release/smuxi-engine.dll \
	../../bin/release/smuxi-frontend-gnome.exe \
	../../bin/release/smuxi-engine-irc.dll \
	../../bin/release/smuxi-frontend.dll
BUILD_DIR = ../../bin/release

SMUXI_COMMON_DLL_SOURCE=../../bin/release/smuxi-common.dll
MEEBEY_SMARTIRC4NET_DLL_SOURCE=../../lib/Meebey.SmartIrc4net.dll
SMUXI_FRONTEND_GNOME_EXE_MDB=
SMUXI_FRONTEND_DLL_SOURCE=../../bin/release/smuxi-frontend.dll
SMUXI_ENGINE_IRC_DLL_SOURCE=../../bin/release/smuxi-engine-irc.dll
SMUXI_ENGINE_IRC_DLL_MDB=
SMUXI_ENGINE_DLL_MDB=
SMUXI_FRONTEND_DLL_MDB=
SMUXI_FRONTEND_GNOME_EXE_SOURCE=../../bin/release/smuxi-frontend-gnome.exe
NINI_DLL_SOURCE=../../lib/Nini.dll
SMUXI_ENGINE_DLL_SOURCE=../../bin/release/smuxi-engine.dll

endif

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = @MCS@
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug -define:DEBUG "-define:DEBUG,TRACE,LOG4NET"
ASSEMBLY = ../../bin/debug/smuxi-frontend-gnome-irc.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../../bin/debug/smuxi-common.dll \
	../../bin/debug/smuxi-engine.dll \
	../../bin/debug/smuxi-frontend-gnome.exe \
	../../bin/debug/smuxi-engine-irc.dll \
	../../bin/debug/smuxi-frontend.dll
BUILD_DIR = ../../bin/debug

SMUXI_COMMON_DLL_SOURCE=../../bin/debug/smuxi-common.dll
MEEBEY_SMARTIRC4NET_DLL_SOURCE=../../lib/Meebey.SmartIrc4net.dll
SMUXI_FRONTEND_GNOME_EXE_MDB_SOURCE=../../bin/debug/smuxi-frontend-gnome.exe.mdb
SMUXI_FRONTEND_GNOME_EXE_MDB=$(BUILD_DIR)/smuxi-frontend-gnome.exe.mdb
SMUXI_FRONTEND_DLL_SOURCE=../../bin/debug/smuxi-frontend.dll
SMUXI_ENGINE_IRC_DLL_SOURCE=../../bin/debug/smuxi-engine-irc.dll
SMUXI_ENGINE_IRC_DLL_MDB_SOURCE=../../bin/debug/smuxi-engine-irc.dll.mdb
SMUXI_ENGINE_IRC_DLL_MDB=$(BUILD_DIR)/smuxi-engine-irc.dll.mdb
SMUXI_ENGINE_DLL_MDB_SOURCE=../../bin/debug/smuxi-engine.dll.mdb
SMUXI_ENGINE_DLL_MDB=$(BUILD_DIR)/smuxi-engine.dll.mdb
SMUXI_FRONTEND_DLL_MDB_SOURCE=../../bin/debug/smuxi-frontend.dll.mdb
SMUXI_FRONTEND_DLL_MDB=$(BUILD_DIR)/smuxi-frontend.dll.mdb
SMUXI_FRONTEND_GNOME_EXE_SOURCE=../../bin/debug/smuxi-frontend-gnome.exe
NINI_DLL_SOURCE=../../lib/Nini.dll
SMUXI_ENGINE_DLL_SOURCE=../../bin/debug/smuxi-engine.dll

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
	$(SMUXI_COMMON_DLL) \
	$(SMUXI_FRONTEND_GNOME_EXE_MDB) \
	$(SMUXI_FRONTEND_DLL) \
	$(SMUXI_ENGINE_IRC_DLL) \
	$(SMUXI_ENGINE_IRC_DLL_MDB) \
	$(SMUXI_ENGINE_DLL_MDB) \
	$(SMUXI_FRONTEND_DLL_MDB) \
	$(SMUXI_FRONTEND_GNOME_EXE) \
	$(SMUXI_ENGINE_DLL)  

LINUX_PKGCONFIG = \
	$(FRONTEND_GNOME_IRC_PC)  

all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES = \
	../AssemblyVersion.cs \
	AssemblyInfo.cs \
	IrcGroupChatView.cs \
	IrcPersonChatView.cs \
	InviteToMenu.cs \
	CtcpMenu.cs

DATA_FILES = 

RESOURCES = 

EXTRAS =

REFERENCES =  \
	System \
	Mono.Posix \
	$(GTK_SHARP_20_LIBS)

DLL_REFERENCES = \
	$(BUILD_DIR)/Meebey.SmartIrc4net.dll

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

SMUXI_COMMON_DLL = $(BUILD_DIR)/smuxi-common.dll
MEEBEY_SMARTIRC4NET_DLL = $(BUILD_DIR)/Meebey.SmartIrc4net.dll
SMUXI_FRONTEND_DLL = $(BUILD_DIR)/smuxi-frontend.dll
SMUXI_ENGINE_IRC_DLL = $(BUILD_DIR)/smuxi-engine-irc.dll
SMUXI_FRONTEND_GNOME_EXE = $(BUILD_DIR)/smuxi-frontend-gnome.exe
NINI_DLL = $(BUILD_DIR)/Nini.dll
LOG4NET_DLL = $(BUILD_DIR)/log4net.dll
SMUXI_ENGINE_DLL = $(BUILD_DIR)/smuxi-engine.dll

$(eval $(call emit-deploy-target,SMUXI_COMMON_DLL))
$(eval $(call emit-deploy-target,SMUXI_FRONTEND_GNOME_EXE_MDB))
$(eval $(call emit-deploy-target,SMUXI_FRONTEND_DLL))
$(eval $(call emit-deploy-target,SMUXI_ENGINE_IRC_DLL))
$(eval $(call emit-deploy-target,SMUXI_ENGINE_IRC_DLL_MDB))
$(eval $(call emit-deploy-target,SMUXI_ENGINE_DLL_MDB))
$(eval $(call emit-deploy-target,SMUXI_FRONTEND_DLL_MDB))
$(eval $(call emit-deploy-target,SMUXI_FRONTEND_GNOME_EXE))
$(eval $(call emit-deploy-target,SMUXI_ENGINE_DLL))


$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(build_resx_resources) : %.resources: %.resx
	resgen2 '$<' '$@'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(dir $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref) $(LOG4NET_REF_LIBS)
