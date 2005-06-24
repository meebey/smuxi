# $Id$
# $URL$
# $Rev$
# $Author$
# $Date$

CSC=mcs
ENGINE_DEFINES=CONFIG_NINI
GNOME_DEFINES=UI_GNOME,GTK_2,CONFIG_NINI
GTK_DEFINES=UI_GTK,GTK_2,CONFIG_NINI
SMARTIRC_DLL=Meebey.SmartIrc4net.dll
SMARTIRC_DIR=../bin
LOG4NET_DLL=log4net.dll
LOG4NET_DIR=../bin
NINI_DLL=Nini.dll
NINI_DIR=../bin
SERVER_LIB=System.Runtime.Remoting
ENGINE_LIB=$(SMARTIRC_DIR)/$(SMARTIRC_DLL),$(NINI_DIR)/$(NINI_DLL)
GTK_LIB=$(NINI_DIR)/$(NINI_DLL),System.Runtime.Remoting
GNOME_LIB=$(NINI_DIR)/$(NINI_DLL),System.Runtime.Remoting
GTK_PACKAGES=gtk-sharp-2.0
GNOME_PACKAGES=gnome-sharp-2.0,gtk-sharp-2.0
RESOURCES = \
../images/splashscreen.png \
../images/about.png
RESOURCES_BUILD = $(foreach res,$(RESOURCES), $(addprefix /resource:,$(res)),$(notdir $(res)))
ENGINE_TARGET=smuxi-engine.dll
SERVER_TARGET=smuxi-server.exe
GTK_TARGET=smuxi-gtk.exe
GNOME_TARGET=smuxi-gnome.exe

all: smuxi-engine smuxi-server smuxi-gnome

smuxi-engine: bin/mono/debug/$(ENGINE_TARGET)
bin/mono/debug/$(ENGINE_TARGET): Engine/*.cs
	$(CSC) /debug /define:$(ENGINE_DEFINES),TRACE,LOG4NET /target:library /r:$(ENGINE_LIB),../bin/$(LOG4NET_DLL) /out:../bin/mono/debug/$(ENGINE_TARGET) $^
	cp $(LOG4NET_DIR)/$(LOG4NET_DLL) ../bin/mono/debug
	cp $(SMARTIRC_DIR)/$(SMARTIRC_DLL) ../bin/mono/debug
	cp $(NINI_DIR)/$(NINI_DLL) ../bin/mono/debug

smuxi-server: bin/mono/debug/$(SERVER_TARGET)
#bin/mono/debug/$(SERVER_TARGET): smuxi-engine
bin/mono/debug/$(SERVER_TARGET): Server/*.cs
	$(CSC) /debug /define:TRACE,LOG4NET /target:exe /r:$(SERVER_LIB),../bin/mono/debug/$(ENGINE_TARGET),../bin/$(LOG4NET_DLL) /out:../bin/mono/debug/$(SERVER_TARGET) $^
	cp $(LOG4NET_DIR)/$(LOG4NET_DLL) ../bin/mono/debug

smuxi-gtk: bin/mono/debug/$(GTK_TARGET)
#bin/mono/debug/$(GTK_TARGET): smuxi-engine smuxi-server
bin/mono/debug/$(GTK_TARGET): Frontend-GtkGnome/*.cs
	$(CSC) /debug /define:$(GTK_DEFINES),TRACE,LOG4NET /target:exe /r:$(GTK_LIB),../bin/mono/debug/$(ENGINE_TARGET),../bin/$(LOG4NET_DLL) /pkg:$(GTK_PACKAGES) $(RESOURCES_BUILD) /out:../bin/mono/debug/$(GTK_TARGET) $^
	cp $(LOG4NET_DIR)/$(LOG4NET_DLL) ../bin/mono/debug

smuxi-gnome: bin/mono/debug/$(GNOME_TARGET)
#bin/mono/debug/$(GNOME_TARGET): smuxi-engine smuxi-server
bin/mono/debug/$(GNOME_TARGET): Frontend-GtkGnome/*.cs
	$(CSC) /debug /define:$(GNOME_DEFINES),TRACE,LOG4NET /target:exe /r:$(GNOME_LIB),../bin/mono/debug/$(ENGINE_TARGET),../bin/$(LOG4NET_DLL) /pkg:$(GNOME_PACKAGES) $(RESOURCES_BUILD) /out:../bin/mono/debug/$(GNOME_TARGET) $^
	cp $(LOG4NET_DIR)/$(LOG4NET_DLL) ../bin/mono/debug

gnome-release: *.cs
	cp $(SMARTIRC_DIR)/bin/mono/release/$(SMARTIRC_DLL) ../bin
	$(CSC) /define:$(GNOME_DEFINES) /target:exe /r:$(LIB) /pkg:$(PACKAGES) $(RESOURCES_BUILD) /out:../bin/mono/release/$(TARGET) $^
	cp ../bin/$(SMARTIRC_DLL) ../bin/mono/release
	cp ../Glade2/*.glade ../bin/mono/release

gtk-debug: *.cs
	cp $(SMARTIRC_DIR)/bin/mono/debug/$(SMARTIRC_DLL) ../bin
	$(CSC) /debug /define:$(GTK_DEFINES),TRACE,LOG4NET /target:exe /r:$(LIB),$(GTK_LIB),../bin/$(LOG4NET_DLL) /pkg:$(PACKAGES) $(RESOURCES_BUILD) /out:../bin/mono/debug/$(TARGET) $^
	cp ../bin/$(SMARTIRC_DLL) ../bin/mono/debug
	cp $(LOG4NET_DIR)/$(LOG4NET_DLL) ../bin/mono/debug
	cp ../Glade2/*.glade ../bin/mono/debug

gtk-release: *.cs
	cp $(SMARTIRC_DIR)/bin/mono/release/$(SMARTIRC_DLL) ../bin
	$(CSC) /define:$(GTK_DEFINES) /target:exe /r:$(LIB),$(GTK_LIB) /pkg:$(PACKAGES) $(RESOURCES_BUILD) /out:../bin/mono/release/$(TARGET) $^
	cp ../bin/$(SMARTIRC_DLL) ../bin/mono/release
	cp $(NINI_DIR)/$(NINI_DLL) ../bin/mono/release
	cp ../Glade2/*.glade ../bin/mono/release

clean:
	-rm -f ../bin/mono/debug/$(ENGINE_TARGET)
	-rm -f ../bin/mono/debug/$(SERVER_TARGET)
	-rm -f ../bin/mono/debug/$(GTK_TARGET)
	-rm -f ../bin/mono/debug/$(GNOME_TARGET)
	-rm -f ../bin/mono/release/$(TARGET)

.PHONY: all smuxi-engine gnome-debug gnome-release gtk-debug gtk-release clean
