# $Id$
# $URL$
# $Rev$
# $Author$
# $Date$

CSC=mcs
#AOT=mono --aot -O=all
AOT=true
ENGINE_DEFINES=CONFIG_NINI
GNOME_DEFINES=UI_GNOME,GTK_SHARP_2,CONFIG_NINI
GTK_DEFINES=UI_GTK,GTK_SHARP_2,CONFIG_NINI
SMARTIRC_DLL=Meebey.SmartIrc4net.dll
SMARTIRC_DIR=../bin
LOG4NET_DLL=log4net.dll
LOG4NET_DIR=../bin
NINI_DLL=Nini.dll
NINI_DIR=../bin
SERVER_LIB=System.Runtime.Remoting
ENGINE_LIB=$(SMARTIRC_DIR)/$(SMARTIRC_DLL),$(NINI_DIR)/$(NINI_DLL)
TEST_LIB=System.Runtime.Remoting
GTK_LIB=$(NINI_DIR)/$(NINI_DLL),System.Runtime.Remoting
GNOME_LIB=$(NINI_DIR)/$(NINI_DLL),System.Runtime.Remoting
GTK_PACKAGES=gtk-sharp-2.0,glade-sharp-2.0
GNOME_PACKAGES=gnome-sharp-2.0,gtk-sharp-2.0,glade-sharp-2.0
RESOURCES = \
../images/splashscreen.png \
../images/about.png \
../images/connect.png \
../images/edit.png \
../glade/preferences.glade
RESOURCES_BUILD = $(foreach res,$(RESOURCES), $(addprefix /resource:,$(res)),$(notdir $(res)))
ENGINE_TARGET=smuxi-engine.dll
SERVER_TARGET=smuxi-server.exe
TEST_TARGET=smuxi-test.exe
GTK_TARGET=smuxi-gtk.exe
GNOME_TARGET=smuxi-gnome.exe

default: smuxi-engine smuxi-server smuxi-gtk smuxi-gnome

debug: \
	bin/mono/debug/$(ENGINE_TARGET) \
	bin/mono/debug/$(SERVER_TARGET) \
	bin/mono/debug/$(TEST_TARGET) \
	bin/mono/debug/$(GTK_TARGET) \
	bin/mono/debug/$(GNOME_TARGET) \

release: \
	bin/mono/release/$(ENGINE_TARGET) \
	bin/mono/release/$(SERVER_TARGET) \
	bin/mono/release/$(GTK_TARGET) \
	bin/mono/release/$(GNOME_TARGET) \

smuxi-engine: bin/mono/debug/$(ENGINE_TARGET)
bin/mono/debug/$(ENGINE_TARGET): Engine/*.cs
	$(CSC) /debug /define:$(ENGINE_DEFINES),TRACE,LOG4NET /target:library /r:$(ENGINE_LIB),../bin/$(LOG4NET_DLL) /out:../bin/mono/debug/$(ENGINE_TARGET) $^
	$(AOT) ../bin/mono/debug/$(ENGINE_TARGET)
	cp $(LOG4NET_DIR)/$(LOG4NET_DLL) ../bin/mono/debug
	cp $(SMARTIRC_DIR)/$(SMARTIRC_DLL) ../bin/mono/debug
	cp $(NINI_DIR)/$(NINI_DLL) ../bin/mono/debug

bin/mono/release/$(ENGINE_TARGET): Engine/*.cs
	$(CSC) /define:$(ENGINE_DEFINES) /target:library /r:$(ENGINE_LIB) /out:../bin/mono/release/$(ENGINE_TARGET) $^
	$(AOT) ../bin/mono/release/$(ENGINE_TARGET)
	cp $(SMARTIRC_DIR)/$(SMARTIRC_DLL) ../bin/mono/release
	cp $(NINI_DIR)/$(NINI_DLL) ../bin/mono/release

smuxi-server: bin/mono/debug/$(SERVER_TARGET)
bin/mono/debug/$(SERVER_TARGET): Server/*.cs
	$(CSC) /debug /define:TRACE,LOG4NET /target:exe /r:$(SERVER_LIB),../bin/mono/debug/$(ENGINE_TARGET),../bin/$(LOG4NET_DLL) /out:../bin/mono/debug/$(SERVER_TARGET) $^
	$(AOT) ../bin/mono/debug/$(SERVER_TARGET)
	cp $(LOG4NET_DIR)/$(LOG4NET_DLL) ../bin/mono/debug

bin/mono/release/$(SERVER_TARGET): Server/*.cs
	$(CSC) /target:exe /r:$(SERVER_LIB),../bin/mono/release/$(ENGINE_TARGET) /out:../bin/mono/release/$(SERVER_TARGET) $^
	$(AOT) ../bin/mono/release/$(SERVER_TARGET)

smuxi-test: bin/mono/debug/$(TEST_TARGET)
bin/mono/debug/$(TEST_TARGET): Frontend-Test/*.cs
	$(CSC) /debug /define:TRACE,LOG4NET /target:exe /r:$(TEST_LIB),../bin/mono/debug/$(ENGINE_TARGET),../bin/$(LOG4NET_DLL) /out:../bin/mono/debug/$(TEST_TARGET) $^
	$(AOT) ../bin/mono/debug/$(TEST_TARGET)
	cp $(LOG4NET_DIR)/$(LOG4NET_DLL) ../bin/mono/debug

smuxi-gtk: bin/mono/debug/$(GTK_TARGET)
bin/mono/debug/$(GTK_TARGET): Frontend-GtkGnome/*.cs
	$(CSC) /debug /define:$(GTK_DEFINES),TRACE,LOG4NET /nowarn:0169 /target:exe /r:$(GTK_LIB),../bin/mono/debug/$(ENGINE_TARGET),../bin/$(LOG4NET_DLL) /pkg:$(GTK_PACKAGES) $(RESOURCES_BUILD) /out:../bin/mono/debug/$(GTK_TARGET) $^
	$(AOT) ../bin/mono/debug/$(GTK_TARGET)
	cp $(LOG4NET_DIR)/$(LOG4NET_DLL) ../bin/mono/debug

bin/mono/release/$(GTK_TARGET): Frontend-GtkGnome/*.cs
	$(CSC) /define:$(GTK_DEFINES) /nowarn:0169 /target:exe /r:$(GTK_LIB),../bin/mono/release/$(ENGINE_TARGET) /pkg:$(GTK_PACKAGES) $(RESOURCES_BUILD) /out:../bin/mono/release/$(GTK_TARGET) $^
	$(AOT) ../bin/mono/release/$(GTK_TARGET)

smuxi-gnome: bin/mono/debug/$(GNOME_TARGET)
bin/mono/debug/$(GNOME_TARGET): Frontend-GtkGnome/*.cs
	$(CSC) /debug /define:$(GNOME_DEFINES),TRACE,LOG4NET /nowarn:0169 /target:exe /r:$(GNOME_LIB),../bin/mono/debug/$(ENGINE_TARGET),../bin/$(LOG4NET_DLL) /pkg:$(GNOME_PACKAGES) $(RESOURCES_BUILD) /out:../bin/mono/debug/$(GNOME_TARGET) $^
	$(AOT) ../bin/mono/debug/$(GNOME_TARGET)
	cp $(LOG4NET_DIR)/$(LOG4NET_DLL) ../bin/mono/debug

bin/mono/release/$(GNOME_TARGET): Frontend-GtkGnome/*.cs
	$(CSC) /define:$(GNOME_DEFINES) /nowarn:0169 /target:exe /r:$(GNOME_LIB),../bin/mono/release/$(ENGINE_TARGET) /pkg:$(GNOME_PACKAGES) $(RESOURCES_BUILD) /out:../bin/mono/release/$(GNOME_TARGET) $^
	$(AOT) ../bin/mono/release/$(GNOME_TARGET)

clean:
	rm -f ../bin/mono/debug/*.mdb
	rm -f ../bin/mono/debug/*.exe.so
	rm -f ../bin/mono/debug/*.dll.so
	rm -f ../bin/mono/debug/$(ENGINE_TARGET)
	rm -f ../bin/mono/debug/$(SERVER_TARGET)
	rm -f ../bin/mono/debug/$(GTK_TARGET)
	rm -f ../bin/mono/debug/$(GNOME_TARGET)
	rm -f ../bin/mono/release/*.mdb
	rm -f ../bin/mono/release/*.exe.so
	rm -f ../bin/mono/release/*.dll.so
	rm -f ../bin/mono/release/$(ENGINE_TARGET)
	rm -f ../bin/mono/release/$(SERVER_TARGET)
	rm -f ../bin/mono/release/$(GTK_TARGET)
	rm -f ../bin/mono/release/$(GNOME_TARGET)

.PHONY: all smuxi-engine gnome-debug gnome-release gtk-debug gtk-release clean
