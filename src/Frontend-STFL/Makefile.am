SUBDIRS = STFL

EXTRA_DIST =  

ASSEMBLY_CONFIG_SOURCE = smuxi-frontend-stfl.exe.config
ASSEMBLY_CONFIG = $(BUILD_DIR)/smuxi-frontend-stfl.exe.config

ASSEMBLY = $(BUILD_DIR)/smuxi-frontend-stfl.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../bin/debug/smuxi-engine.dll \
	../../bin/debug/smuxi-frontend.dll \
	../../bin/debug/smuxi-common.dll
BUILD_DIR = ../../bin/debug

LOG4NET_DLL_SOURCE=../../lib/log4net.dll
SMUXI_ENGINE_DLL_MDB_SOURCE=../../bin/debug/smuxi-engine.dll.mdb
SMUXI_ENGINE_DLL_MDB=$(BUILD_DIR)/smuxi-engine.dll.mdb
NINI_DLL_SOURCE=../../lib/Nini.dll
SMUXI_ENGINE_DLL_SOURCE=../../bin/debug/smuxi-engine.dll
STFL_DLL_SOURCE=../../lib/Stfl.dll
SMUXI_FRONTEND_DLL_MDB_SOURCE=../../bin/debug/smuxi-frontend.dll.mdb
SMUXI_FRONTEND_DLL_MDB=$(BUILD_DIR)/smuxi-frontend.dll.mdb
SMUXI_FRONTEND_DLL_SOURCE=../../bin/debug/smuxi-frontend.dll
SMUXI_COMMON_DLL_SOURCE=../../bin/debug/smuxi-common.dll

AL=al2
SATELLITE_ASSEMBLY_NAME=.resources.dll

PROGRAMFILES = \
	$(SMUXI_ENGINE_DLL_MDB) \
	$(SMUXI_ENGINE_DLL) \
	$(SMUXI_FRONTEND_DLL_MDB) \
	$(SMUXI_FRONTEND_DLL) \
	$(SMUXI_COMMON_DLL) \
	$(ASSEMBLY_CONFIG)

BINARIES = \
	$(FRONTEND_STFL)  

all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES) 

FILES = \
	Main.cs \
	$(top_srcdir)/src/AssemblyVersion.cs \
	AssemblyInfo.cs \
	Frontend.cs \
	MainWindow.cs \
	StflUI.cs \
	Entry.cs \
	STFL/Form.cs \
	STFL/EventReceivedEventArgs.cs \
	STFL/KeyPressedEventArgs.cs \
	STFL/NcursesApi.cs \
	STFL/StflApi.cs \
	STFL/TextView.cs \
	STFL/Widget.cs \
	ChatView.cs \
	ChatViewManager.cs 

DATA_FILES = 

RESOURCES = \
	MainWindow.stfl 

EXTRAS = \
	smuxi-frontend-stfl.in \
	$(ASSEMBLY_CONFIG_SOURCE)

REFERENCES =  \
	System \
	Mono.Posix \
	$(LOG4NET_LIBS)

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(BINARIES) 

include $(top_srcdir)/Makefile.include

FRONTEND_STFL = $(BUILD_DIR)/smuxi-frontend-stfl
LOG4NET_DLL = $(BUILD_DIR)/log4net.dll
NINI_DLL = $(BUILD_DIR)/Nini.dll
SMUXI_ENGINE_DLL = $(BUILD_DIR)/smuxi-engine.dll
STFL_DLL = $(BUILD_DIR)/Stfl.dll
SMUXI_FRONTEND_DLL = $(BUILD_DIR)/smuxi-frontend.dll
SMUXI_COMMON_DLL = $(BUILD_DIR)/smuxi-common.dll
LOG4NET_CONFIG = $(BUILD_DIR)/smuxi-frontend-stfl.exe.config
LOG4NET_CONFIG_SOURCE = smuxi-frontend-stfl.exe.config
build_datafiles = $(LOG4NET_CONFIG)

if ENABLE_STATIC_STFL
STFLSHARP_SO_SOURCE = $(builddir)/STFL/libstflsharp.so
STFLSHARP_SO = $(BUILD_DIR)/libstflsharp.so
build_datafiles += $(STFLSHARP_SO)
endif

$(eval $(call emit-deploy-wrapper,FRONTEND_STFL,smuxi-frontend-stfl,x))
$(eval $(call emit-deploy-target,SMUXI_ENGINE_DLL_MDB))
$(eval $(call emit-deploy-target,SMUXI_ENGINE_DLL))
$(eval $(call emit-deploy-target,SMUXI_FRONTEND_DLL_MDB))
$(eval $(call emit-deploy-target,SMUXI_FRONTEND_DLL))
$(eval $(call emit-deploy-target,SMUXI_COMMON_DLL))
$(eval $(call emit-deploy-target,LOG4NET_CONFIG))
$(eval $(call emit-deploy-target,ASSEMBLY_CONFIG))
if ENABLE_STATIC_STFL
$(eval $(call emit-deploy-target,STFLSHARP_SO))
endif

$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(build_resx_resources) : %.resources: %.resx
	resgen2 '$<' '$@'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(dir $(ASSEMBLY))
	$(CSC) $(CSC_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
