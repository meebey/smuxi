
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:NET_2_0"

ASSEMBLY = ../../bin/release/smuxi-common.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = ../../bin/release

LOG4NET_DLL_SOURCE=../../lib/log4net.dll

endif

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:DEBUG,TRACE,LOG4NET,NET_2_0"

ASSEMBLY = ../../bin/debug/smuxi-common.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = ../../bin/debug

LOG4NET_DLL_SOURCE=../../lib/log4net.dll

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=.resources.dll

PROGRAMFILES = \
	$(LOG4NET_DLL)  

LINUX_PKGCONFIG = \
	$(COMMON_PC)  


	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES = \
	AssemblyInfo.cs \
	ITraceable.cs \
	Trace.cs \
	MD5.cs \
	FastSerializer.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	common.pc.in 

REFERENCES =  \
	System

DLL_REFERENCES =  \
	../../lib/log4net.dll

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

LOG4NET_DLL = $(BUILD_DIR)/log4net.dll
COMMON_PC = $(BUILD_DIR)/common.pc

$(eval $(call emit-deploy-target,LOG4NET_DLL))
$(eval $(call emit-deploy-wrapper,COMMON_PC,common.pc))


$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(build_resx_resources) : %.resources: %.resx
	resgen2 '$<' '$@'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(dir $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
