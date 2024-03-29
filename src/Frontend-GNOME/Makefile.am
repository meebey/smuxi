TARGET_DIR = $(top_builddir)/bin/$(PROFILE)
EXTRA_DIST = $(WIN_ICON) $(DESKTOP_FILE).in $(man_MANS) $(appdata_in_files)
man_MANS = smuxi-frontend-gnome.1

mkdir_p = $(install_sh) -d

ICON_NAME = smuxi-frontend-gnome
ICON_NAME_PNG = $(ICON_NAME).png
ICON_SVG = $(ICON_NAME).svg
ICON_XPM = $(ICON_NAME).xpm
GROUP_CHAT_ICON_NAME = smuxi-group-chat
PERSON_CHAT_ICON_NAME = smuxi-person-chat
ICON_16 = $(top_builddir)/images/16/$(ICON_NAME_PNG)
ICON_22 = $(top_builddir)/images/22/$(ICON_NAME_PNG)
ICON_24 = $(top_builddir)/images/24/$(ICON_NAME_PNG)
ICON_32 = $(top_builddir)/images/32/$(ICON_NAME_PNG)
ICON_48 = $(top_builddir)/images/48/$(ICON_NAME_PNG)
ICON_128 = $(top_builddir)/images/128/$(ICON_NAME_PNG)
ICON_256 = $(top_builddir)/images/256/$(ICON_NAME_PNG)

WIN_ICON = $(top_srcdir)/images/icon.ico
DESKTOP_FILE = smuxi-frontend-gnome.desktop

THEME_DIR = $(datadir)/icons/hicolor
svgicondir = $(THEME_DIR)/scalable/apps
svgicon_DATA = $(ICON_SVG)
icon16dir = $(THEME_DIR)/16x16/apps
icon16_DATA = $(ICON_16)
icon22dir = $(THEME_DIR)/22x22/apps
icon22_DATA = $(ICON_22)
icon24dir = $(THEME_DIR)/24x24/apps
icon24_DATA = $(ICON_24)
icon32dir = $(THEME_DIR)/32x32/apps
icon32_DATA = $(ICON_32)
icon48dir = $(THEME_DIR)/48x48/apps
icon48_DATA = $(ICON_48)
icon128dir = $(THEME_DIR)/128x128/apps
icon128_DATA = $(ICON_128)
icon256dir = $(THEME_DIR)/256x256/apps
icon256_DATA = \
	$(ICON_256) \
	$(top_builddir)/images/256/$(GROUP_CHAT_ICON_NAME).png \
	$(top_builddir)/images/256/$(PERSON_CHAT_ICON_NAME).png

gtk_update_icon_cache = gtk-update-icon-cache -f -t $(THEME_DIR)

desktopdir = $(datadir)/applications
desktop_in_files = $(DESKTOP_FILE).in
desktop_DATA = $(DESKTOP_FILE)
build_datafiles = $(DESKTOP_FILE)

@INTLTOOL_XML_RULE@
appdatadir = $(datarootdir)/appdata
appdata_DATA = $(appdata_in_files:.xml.in=.xml)
appdata_in_files = smuxi-frontend-gnome.appdata.xml.in

FRONTEND_GNOME_EXE_CONFIG_SOURCE = smuxi-frontend-gnome.exe.config
FRONTEND_GNOME_EXE_CONFIG = $(BUILD_DIR)/smuxi-frontend-gnome.exe.config

ASSEMBLY_COMPILER_COMMAND = @CSC@
ASSEMBLY_COMPILER_FLAGS =  @CSC_FLAGS@ -noconfig -codepage:utf8 -warn:4 -optimize- -debug -define:DEBUG "-define:DEBUG,TRACE,LOG4NET,CONFIG_NINI,UI_GNOME,GTK_SHARP_2_8" -win32icon:$(WIN_ICON) @FRONTEND_GNOME_COMPILER_FLAGS@

ASSEMBLY = ../../bin/debug/smuxi-frontend-gnome.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = winexe
PROJECT_REFERENCES =  \
	../../bin/debug/smuxi-engine.dll \
	../../bin/debug/smuxi-common.dll \
	../../bin/debug/smuxi-frontend.dll
BUILD_DIR = ../../bin/debug

LOG4NET_DLL_SOURCE=../../lib/log4net.dll

if BUNDLE_LOG4NET
LOG4NET_REF_LIBS=-r:$(LOG4NET_DLL_SOURCE)
else
LOG4NET_REF_LIBS=$(LOG4NET_LIBS)
endif

SMUXI_ENGINE_DLL_MDB_SOURCE=../../bin/debug/smuxi-engine.dll.mdb
SMUXI_ENGINE_DLL_MDB=$(BUILD_DIR)/smuxi-engine.dll.mdb
NINI_DLL_SOURCE=../../lib/Nini.dll
SMUXI_ENGINE_DLL_SOURCE=../../bin/debug/smuxi-engine.dll
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
	$(SMUXI_COMMON_DLL)  \
	$(FRONTEND_GNOME_EXE_CONFIG)

LINUX_DESKTOPAPPLICATIONS = \
	$(FRONTEND_GNOME_DESKTOP_DESKTOP)  

BINARIES = \
	$(FRONTEND_GNOME)  

all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_DESKTOPAPPLICATIONS) $(BINARIES)

$(ICON_SVG):
	$(mkdir_p) $(dir $@)
	$(INSTALL) -m644 $(top_srcdir)/images/icon.svg $@
$(ICON_XPM):
	$(mkdir_p) $(dir $@)
	$(INSTALL) -m644 $(top_srcdir)/images/icon.xpm $@
$(ICON_16):
	$(mkdir_p) $(dir $@)
	$(INSTALL) -m644 $(top_srcdir)/images/icon_16x16.png $@
$(ICON_22):
	$(mkdir_p) $(dir $@)
	$(INSTALL) -m644 $(top_srcdir)/images/icon_22x22.png $@
$(ICON_24):
	$(mkdir_p) $(dir $@)
	$(INSTALL) -m644 $(top_srcdir)/images/icon_24x24.png $@
$(ICON_32):
	$(mkdir_p) $(dir $@)
	$(INSTALL) -m644 $(top_srcdir)/images/icon_32x32.png $@
$(ICON_48):
	$(mkdir_p) $(dir $@)
	$(INSTALL) -m644 $(top_srcdir)/images/icon_48x48.png $@
$(ICON_128):
	$(mkdir_p) $(dir $@)
	$(INSTALL) -m644 $(top_srcdir)/images/icon_128x128.png $@
$(ICON_256):
	$(mkdir_p) $(dir $@)
	$(INSTALL) -m644 $(top_srcdir)/images/icon_256x256.png $@
$(top_builddir)/images/256/$(GROUP_CHAT_ICON_NAME).png:
	$(mkdir_p) $(dir $@)
	$(INSTALL) -m644 $(top_srcdir)/images/group-chat_256x256.png $@
$(top_builddir)/images/256/$(PERSON_CHAT_ICON_NAME).png:
	$(mkdir_p) $(dir $@)
	$(INSTALL) -m644 $(top_srcdir)/images/person-chat_256x256.png $@

FILES = \
	../AssemblyVersion.cs \
	../AssemblyTargetFramework.cs \
	AssemblyInfo.cs \
	AboutDialog.cs \
	CrashDialog.cs \
	EngineManagerDialog.cs \
	Entry.cs \
	Frontend.cs \
	GnomeUI.cs \
	Gtk.WindowExtensions.cs \
	IndicateManager.cs \
	Main.cs \
	MainWindow.cs \
	NetworkManager.cs \
	NotImplementedMessageDialog.cs \
	Notebook.cs \
	SplashScreenWindow.cs \
	ChatTypeWidget.cs \
	ChatViewManager.cs \
	ColorConverter.cs \
	NotifyManager.cs \
	PangoTools.cs \
	Preferences/ServerListView.cs \
	Views/ChatTreeView.cs \
	Views/JoinWidget.cs \
	Views/MenuWidget.cs \
	Views/MessageTextView.cs \
	Views/PreferencesDialog.cs \
	Views/ServerWidget.cs \
	Views/ServerDialog.cs \
	Views/FilterListWidget.cs \
	Views/Chats/ChatView.cs \
	Views/Chats/GroupChatView.cs \
	Views/Chats/PersonChatView.cs \
	Views/Chats/ProtocolChatView.cs \
	Views/Chats/SessionChatView.cs \
	Views/Assistants/Engine/EngineAssistantNameWidget.cs \
	Views/Assistants/Engine/EngineAssistantCredentialsWidget.cs \
	Views/Assistants/Engine/EngineAssistantConnectionWidget.cs \
	Views/Assistants/Engine/EngineAssistantIntroWidget.cs \
	Views/Assistants/Engine/EngineAssistant.cs \
	Views/Tags/EmojiTag.cs \
	Views/Tags/LinkTag.cs \
	Views/Tags/PersonTag.cs \
	QuickConnectDialog.cs \
	SteticPreferencesDialog.cs \
	FindGroupChatDialog.cs \
	ChatFindDialog.cs \
	OpenChatDialog.cs \
	ThemeSettings.cs \
	StatusIconManager.cs \
	gtk-gui/generated.cs \
	gtk-gui/Smuxi.Frontend.Gnome.EngineAssistantConnectionWidget.cs \
	gtk-gui/Smuxi.Frontend.Gnome.EngineAssistantCredentialsWidget.cs \
	gtk-gui/Smuxi.Frontend.Gnome.EngineAssistantIntroWidget.cs \
	gtk-gui/Smuxi.Frontend.Gnome.EngineAssistantNameWidget.cs \
	gtk-gui/Smuxi.Frontend.Gnome.JoinWidget.cs \
	gtk-gui/Smuxi.Frontend.Gnome.MenuWidget.cs \
	gtk-gui/Smuxi.Frontend.Gnome.OpenChatDialog.cs \
	gtk-gui/Smuxi.Frontend.Gnome.SteticPreferencesDialog.cs \
	gtk-gui/Smuxi.Frontend.Gnome.QuickConnectDialog.cs \
	gtk-gui/Smuxi.Frontend.Gnome.FindGroupChatDialog.cs \
	gtk-gui/Smuxi.Frontend.Gnome.ChatFindDialog.cs \
	gtk-gui/Smuxi.Frontend.Gnome.ChatTypeWidget.cs \
	gtk-gui/Smuxi.Frontend.Gnome.ServerDialog.cs \
	gtk-gui/Smuxi.Frontend.Gnome.ServerWidget.cs \
	gtk-gui/Smuxi.Frontend.Gnome.FilterListWidget.cs \
	osx/AppleEvent.cs \
	osx/ApplicationEvents.cs \
	osx/Carbon.cs \
	osx/CoreFoundation.cs \
	osx/IgeMacMenu.cs

DATA_FILES = \
	$(DESKTOP_FILE) \
	$(ICON_SVG) \
	$(ICON_XPM) \
	$(ICON_16) \
	$(ICON_22) \
	$(ICON_24) \
	$(ICON_32) \
	$(ICON_48) \
	$(ICON_128) \
	$(ICON_256)

RESOURCES = \
	gtk-gui/gui.stetic \
	$(top_builddir)/images/icon_256x256.png \
	$(top_builddir)/images/group-chat_256x256.png \
	$(top_builddir)/images/person-chat_256x256.png \
	$(top_builddir)/images/protocol-chat_256x256.png \
	$(top_builddir)/images/session-chat_256x256.png \
	Interfaces/PreferencesDialog2.ui \
	Interfaces/ServerListWidget.ui

EXTRAS = \
	smuxi-frontend-gnome.in \
	$(FRONTEND_GNOME_EXE_CONFIG_SOURCE)

REFERENCES =  \
	System.Runtime.Remoting \
	System \
    System.Core \
	System.Web \
	Mono.Posix \
	$(GLIB_SHARP_20_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(GIO_SHARP_LIBS) \
	$(LOG4NET_REF_LIBS) \
	$(INDICATE_SHARP_LIBS) \
	$(MESSAGINGMENU_SHARP_LIBS) \
	$(MESSAGINGMENU_SHARP_INCLUDED_LIBS) \
	$(NOTIFY_SHARP_LIBS) \
	$(DBUS_LIBS)

DLL_REFERENCES = $(MESSAGINGMENU_SHARP_INCLUDED_LIBS) 

CLEANFILES = $(LINUX_DESKTOPAPPLICATIONS) $(PROGRAMFILES) $(BINARIES) \
	$(top_builddir)/images/256/$(GROUP_CHAT_ICON_NAME).png \
	$(top_builddir)/images/256/$(PERSON_CHAT_ICON_NAME).png \
	$(appdata_DATA)

include $(top_srcdir)/Makefile.include

SMUXI_ENGINE_DLL = $(BUILD_DIR)/smuxi-engine.dll
FRONTEND_GNOME = $(BUILD_DIR)/smuxi-frontend-gnome
FRONTEND_GNOME_DESKTOP = $(BUILD_DIR)/$(DESKTOP_FILE)
SMUXI_FRONTEND_DLL = $(BUILD_DIR)/smuxi-frontend.dll
SMUXI_COMMON_DLL = $(BUILD_DIR)/smuxi-common.dll

if WITH_MESSAGINGMENU_SHARP_INCLUDED
MESSAGINGMENU_SHARP_DLL = $(TARGET_DIR)/messagingmenu-sharp.dll
MESSAGINGMENU_SHARP_DLL_SOURCE = $(top_builddir)/lib/messagingmenu-sharp/messagingmenu-sharp.dll
MESSAGINGMENU_SHARP_DLL_CONFIG = $(MESSAGINGMENU_SHARP_DLL).config
MESSAGINGMENU_SHARP_DLL_CONFIG_SOURCE = $(MESSAGINGMENU_SHARP_DLL_SOURCE).config
$(eval $(call emit-deploy-target,MESSAGINGMENU_SHARP_DLL))
$(eval $(call emit-deploy-target,MESSAGINGMENU_SHARP_DLL_CONFIG))

MESSAGINGMENU_SHARP_INCLUDED_LIBS := $(foreach file, $(MESSAGINGMENU_SHARP_FILES), $(TARGET_DIR)/$(file))
PROGRAMFILES += $(MESSAGINGMENU_SHARP_INCLUDED_LIBS) $(MESSAGINGMENU_SHARP_INCLUDED_LIBS).config

GIO_SHARP_DLL = $(TARGET_DIR)/gio-sharp.dll
GIO_SHARP_DLL_SOURCE =  $(GIO_SHARP_LIBS)
GIO_SHARP_DLL_CONFIG = $(TARGET_DIR)/gio-sharp.dll.config
GIO_SHARP_DLL_CONFIG_SOURCE = $(GIO_SHARP_LIBS).config
$(eval $(call emit-deploy-target,GIO_SHARP_DLL))
$(eval $(call emit-deploy-target,GIO_SHARP_DLL_CONFIG))

GIO_SHARP_PROGRAM_FILES := $(foreach file, $(GIO_SHARP_FILES), $(shell file=$(TARGET_DIR)/$(file); echo $$file; config_file=$${file}.config; if [ -f "$$config_file" ]; then echo $$config_file; fi))
PROGRAMFILES += $(GIO_SHARP_DLL) $(GIO_SHARP_DLL_CONFIG)
endif

$(eval $(call emit-deploy-wrapper,FRONTEND_GNOME,smuxi-frontend-gnome,x))
$(eval $(call emit-deploy-target,FRONTEND_GNOME_DESKTOP))
$(eval $(call emit-deploy-target,FRONTEND_GNOME_EXE_CONFIG))
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

%.desktop: %.desktop.in $(INTLTOOL_MERGE) $(wildcard $(top_srcdir)/po-Frontend-GNOME/*.po)
	LC_ALL=C $(INTLTOOL_MERGE) -d -u -c $(top_builddir)/po-Frontend-GNOME/.intltool-merge-cache $(top_srcdir)/po-Frontend-GNOME $< $@

install-data-hook: update-icon-cache
uninstall-hook: update-icon-cache
update-icon-cache:
	@-if test -z "$(DESTDIR)"; then \
		echo "Updating Gtk icon cache."; \
		$(gtk_update_icon_cache); \
	else \
		echo "*** Icon cache not updated.  After (un)install, run this:"; \
		echo "***   $(gtk_update_icon_cache)"; \
	fi
