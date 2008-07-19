
EXTRA_DIST =  

FRONTEND_GNOME_DESKTOP_SOURCE = smuxi-frontend-gnome.desktop

# Warning: This is an automatically generated file, do not edit!

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:CONFIG_NINI,UI_GNOME,GTK_SHARP_2_8"

ASSEMBLY = ../../bin/release/smuxi-frontend-gnome.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../bin/release/smuxi-engine.dll \
	../../bin/release/smuxi-common.dll \
	../../bin/release/smuxi-frontend.dll
BUILD_DIR = ../../bin/release

LOG4NET_DLL_SOURCE=../../lib/log4net.dll
SMUXI_ENGINE_DLL_MDB=
NINI_DLL_SOURCE=../../lib/Nini.dll
SMUXI_ENGINE_DLL_SOURCE=../../bin/release/smuxi-engine.dll
SMUXI_FRONTEND_DLL_MDB=
SMUXI_FRONTEND_DLL_SOURCE=../../bin/release/smuxi-frontend.dll
SMUXI_COMMON_DLL_SOURCE=../../bin/release/smuxi-common.dll

endif

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug -define:DEBUG "-define:DEBUG,TRACE,LOG4NET,CONFIG_NINI,UI_GNOME,GTK_SHARP_2_8"

ASSEMBLY = ../../bin/debug/smuxi-frontend-gnome.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../bin/debug/smuxi-engine.dll \
	../../bin/debug/smuxi-common.dll \
	../../bin/debug/smuxi-frontend.dll
BUILD_DIR = ../../bin/debug

LOG4NET_DLL_SOURCE=../../lib/log4net.dll
SMUXI_ENGINE_DLL_MDB_SOURCE=../../bin/debug/smuxi-engine.dll.mdb
SMUXI_ENGINE_DLL_MDB=$(BUILD_DIR)/smuxi-engine.dll.mdb
NINI_DLL_SOURCE=../../lib/Nini.dll
SMUXI_ENGINE_DLL_SOURCE=../../bin/debug/smuxi-engine.dll
SMUXI_FRONTEND_DLL_MDB_SOURCE=../../bin/debug/smuxi-frontend.dll.mdb
SMUXI_FRONTEND_DLL_MDB=$(BUILD_DIR)/smuxi-frontend.dll.mdb
SMUXI_FRONTEND_DLL_SOURCE=../../bin/debug/smuxi-frontend.dll
SMUXI_COMMON_DLL_SOURCE=../../bin/debug/smuxi-common.dll

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=.resources.dll

PROGRAMFILES = \
	$(SMUXI_ENGINE_DLL_MDB) \
	$(SMUXI_ENGINE_DLL) \
	$(SMUXI_FRONTEND_DLL_MDB) \
	$(SMUXI_FRONTEND_DLL) \
	$(SMUXI_COMMON_DLL)  

LINUX_DESKTOPAPPLICATIONS = \
	$(FRONTEND_GNOME_DESKTOP_DESKTOP)  

BINARIES = \
	$(FRONTEND_GNOME)  

all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_DESKTOPAPPLICATIONS) $(BINARIES) 

FILES = \
	AboutDialog.cs \
	AssemblyInfo.cs \
	CrashDialog.cs \
	EngineManagerDialog.cs \
	Entry.cs \
	FirstStartDruid.cs \
	Frontend.cs \
	GnomeUI.cs \
	Main.cs \
	MainWindow.cs \
	EngineDruid.cs \
	NotImplementedMessageDialog.cs \
	Notebook.cs \
	SplashScreenWindow.cs \
	Preferences/ChannelFilterListView.cs \
	ChatViewManager.cs \
	Preferences/ServerListView.cs \
	Preferences/ServerView.cs \
	Preferences/PreferencesDialog.cs \
	Chats/ChatView.cs \
	Chats/GroupChatView.cs \
	Chats/PersonChatView.cs \
	Chats/ProtocolChatView.cs \
	Chats/SessionChatView.cs \
	gtk-gui/generated.cs \
	QuickConnectDialog.cs \
	SteticPreferencesDialog.cs \
	gtk-gui/Smuxi.Frontend.Gnome.SteticPreferencesDialog.cs \
	gtk-gui/Smuxi.Frontend.Gnome.QuickConnectDialog.cs \
	FindGroupChatDialog.cs \
	gtk-gui/Smuxi.Frontend.Gnome.FindGroupChatDialog.cs 

DATA_FILES = \
	smuxi-frontend-gnome.desktop

RESOURCES = \
	gtk-gui/gui.stetic \
	$(top_srcdir)/images/about.png \
	$(top_srcdir)/images/splashscreen.png \
	$(top_srcdir)/glade/smuxi-frontend-gnome.glade \
	$(top_srcdir)/images/connect.png \
	$(top_srcdir)/images/edit.png \
	$(top_srcdir)/images/group-chat.svg \
	$(top_srcdir)/images/person-chat.svg \
	$(top_srcdir)/images/protocol-chat.svg \
	$(top_srcdir)/images/session-chat.svg 

EXTRAS = \
	smuxi-frontend-gnome.in \
	smuxi-frontend-gnome.pc.in

REFERENCES =  \
	System.Runtime.Remoting \
	System \
	Mono.Posix \
	System.Drawing \
	$(GNOME_SHARP_20_LIBS) \
	$(GLIB_SHARP_20_LIBS) \
	$(GLADE_SHARP_20_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(LOG4NET_LIBS)

DLL_REFERENCES = 

CLEANFILES = $(LINUX_DESKTOPAPPLICATIONS) $(PROGRAMFILES) $(BINARIES) 

include $(top_srcdir)/Makefile.include

SMUXI_ENGINE_DLL = $(BUILD_DIR)/smuxi-engine.dll
FRONTEND_GNOME = $(BUILD_DIR)/smuxi-frontend-gnome
FRONTEND_GNOME_DESKTOP = $(BUILD_DIR)/smuxi-frontend-gnome.desktop
FRONTEND_GNOME_PC = $(BUILD_DIR)/smuxi-frontend-gnome.pc
SMUXI_FRONTEND_DLL = $(BUILD_DIR)/smuxi-frontend.dll
SMUXI_COMMON_DLL = $(BUILD_DIR)/smuxi-common.dll

$(eval $(call emit-deploy-wrapper,FRONTEND_GNOME,smuxi-frontend-gnome,x))
$(eval $(call emit-deploy-wrapper,FRONTEND_GNOME_PC,smuxi-frontend-gnome.pc))
$(eval $(call emit-deploy-target,FRONTEND_GNOME_DESKTOP))
$(eval $(call emit-deploy-target,SMUXI_ENGINE_DLL_MDB))
$(eval $(call emit-deploy-target,SMUXI_ENGINE_DLL))
$(eval $(call emit-deploy-target,SMUXI_FRONTEND_DLL_MDB))
$(eval $(call emit-deploy-target,SMUXI_FRONTEND_DLL))
$(eval $(call emit-deploy-target,SMUXI_COMMON_DLL))


$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(build_resx_resources) : %.resources: %.resx
	resgen2 '$<' '$@'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(dir $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
