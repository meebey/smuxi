# $Id$
# $URL$
# $Rev$
# $Author$
# $Date$

CSC=gmcs
#AOT=mono --aot -O=all
AOT=true
ENGINE_DEFINES=CONFIG_NINI
GNOME_DEFINES=UI_GNOME,GTK_SHARP_2,CONFIG_NINI
GTK_DEFINES=UI_GTK,GTK_SHARP_2,CONFIG_NINI
SMARTIRC_DLL=Meebey.SmartIrc4net.dll
SMARTIRC_DIR=../lib
LOG4NET_DLL=log4net.dll
LOG4NET_DIR=../lib
NINI_DLL=Nini.dll
NINI_DIR=../lib
POSIX_DLL=Mono.Posix.dll
POSIX_DIR=../lib
SERVER_LIB=System.Runtime.Remoting
ENGINE_LIB=$(SMARTIRC_DIR)/$(SMARTIRC_DLL),$(NINI_DIR)/$(NINI_DLL),$(POSIX_DIR)/$(POSIX_DLL)
FRONTEND_LIB=System
TEST_LIB=System.Runtime.Remoting
GTK_LIB=$(NINI_DIR)/$(NINI_DLL),$(POSIX_DIR)/$(POSIX_DLL),System.Runtime.Remoting
GNOME_LIB=$(NINI_DIR)/$(NINI_DLL),$(POSIX_DIR)/$(POSIX_DLL),System.Runtime.Remoting
GTK_PACKAGES=gtk-sharp-2.0,glade-sharp-2.0
GNOME_PACKAGES=gnome-sharp-2.0,gtk-sharp-2.0,glade-sharp-2.0
RESOURCES = \
../images/splashscreen.png \
../images/about.png \
../images/connect.png \
../images/edit.png \
../glade/preferences.glade
RESOURCES_BUILD = $(foreach res,$(RESOURCES), $(addprefix /resource:,$(res)),$(notdir $(res)))
COMMON_TARGET=smuxi-common.dll
ENGINE_TARGET=smuxi-engine.dll
SERVER_TARGET=smuxi-server.exe
FRONTEND_TARGET=smuxi-frontend.dll
TEST_TARGET=smuxi-test.exe
GTK_TARGET=smuxi-gtk.exe
GNOME_TARGET=smuxi-gnome.exe

default: smuxi-common smuxi-engine smuxi-server smuxi-frontend smuxi-gtk smuxi-gnome

debug: \
	bin/debug/$(COMMON_TARGET) \
	bin/debug/$(ENGINE_TARGET) \
	bin/debug/$(SERVER_TARGET) \
	bin/debug/$(FRONTEND_TARGET) \
	bin/debug/$(TEST_TARGET) \
	bin/debug/$(GTK_TARGET) \
	bin/debug/$(GNOME_TARGET) \

release: \
	bin/release/$(COMMON_TARGET) \
	bin/release/$(ENGINE_TARGET) \
	bin/release/$(SERVER_TARGET) \
	bin/release/$(FRONTEND_TARGET) \
	bin/release/$(GTK_TARGET) \
	bin/release/$(GNOME_TARGET) \

smuxi-common: bin/debug/$(COMMON_TARGET)
bin/debug/$(COMMON_TARGET): Common/*.cs
	$(CSC) /debug /define:TRACE,LOG4NET /target:library /r:$(LOG4NET_DIR)/$(LOG4NET_DLL) /out:../bin/debug/$(COMMON_TARGET) $^
	$(AOT) ../bin/debug/$(COMMON_TARGET)
	cp $(LOG4NET_DIR)/$(LOG4NET_DLL) ../bin/debug

smuxi-engine: bin/debug/$(ENGINE_TARGET)
bin/debug/$(ENGINE_TARGET): Engine/*.cs Engine/*/*.cs Engine/*/*/*.cs
	$(CSC) /debug /define:$(ENGINE_DEFINES),TRACE,LOG4NET /target:library /r:$(ENGINE_LIB),../bin/debug/$(COMMON_TARGET),$(LOG4NET_DIR)/$(LOG4NET_DLL) /out:../bin/debug/$(ENGINE_TARGET) $^
	$(AOT) ../bin/debug/$(ENGINE_TARGET)
	cp $(LOG4NET_DIR)/$(LOG4NET_DLL) ../bin/debug
	cp $(SMARTIRC_DIR)/$(SMARTIRC_DLL) ../bin/debug
	cp $(NINI_DIR)/$(NINI_DLL) ../bin/debug

bin/release/$(ENGINE_TARGET): Engine/*.cs
	$(CSC) /define:$(ENGINE_DEFINES) /target:library /r:$(ENGINE_LIB) /out:../bin/release/$(ENGINE_TARGET) $^
	$(AOT) ../bin/release/$(ENGINE_TARGET)
	cp $(SMARTIRC_DIR)/$(SMARTIRC_DLL) ../bin/release
	cp $(NINI_DIR)/$(NINI_DLL) ../bin/release

smuxi-server: bin/debug/$(SERVER_TARGET)
bin/debug/$(SERVER_TARGET): Server/*.cs
	$(CSC) /debug /define:TRACE,LOG4NET /target:exe /r:$(SERVER_LIB),../bin/debug/$(ENGINE_TARGET),$(LOG4NET_DIR)/$(LOG4NET_DLL) /out:../bin/debug/$(SERVER_TARGET) $^
	$(AOT) ../bin/debug/$(SERVER_TARGET)
	cp $(LOG4NET_DIR)/$(LOG4NET_DLL) ../bin/debug

bin/release/$(SERVER_TARGET): Server/*.cs
	$(CSC) /target:exe /r:$(SERVER_LIB),../bin/release/$(ENGINE_TARGET) /out:../bin/release/$(SERVER_TARGET) $^
	$(AOT) ../bin/release/$(SERVER_TARGET)

smuxi-frontend: bin/debug/$(FRONTEND_TARGET)
bin/debug/$(FRONTEND_TARGET): Frontend/*.cs
	$(CSC) /debug /define:TRACE,LOG4NET /target:library /r:$(FRONTEND_LIB),../bin/debug/$(ENGINE_TARGET),$(LOG4NET_DIR)/$(LOG4NET_DLL) /out:../bin/debug/$(FRONTEND_TARGET) $^

smuxi-test: bin/debug/$(TEST_TARGET)
bin/debug/$(TEST_TARGET): Frontend-Test/*.cs
	$(CSC) /debug /define:TRACE,LOG4NET /target:exe /r:$(TEST_LIB),../bin/debug/$(ENGINE_TARGET),$(LOG4NET_DIR)/$(LOG4NET_DLL) /out:../bin/debug/$(TEST_TARGET) $^
	$(AOT) ../bin/debug/$(TEST_TARGET)
	cp $(LOG4NET_DIR)/$(LOG4NET_DLL) ../bin/debug

smuxi-gtk: bin/debug/$(GTK_TARGET)
bin/debug/$(GTK_TARGET): Frontend-GNOME/*.cs Frontend-GNOME/*/*.cs
	$(CSC) /debug /define:$(GTK_DEFINES),TRACE,LOG4NET /nowarn:0169 /target:exe /r:$(GTK_LIB),../bin/debug/$(COMMON_TARGET),../bin/debug/$(ENGINE_TARGET),../bin/debug/$(FRONTEND_TARGET),$(LOG4NET_DIR)/$(LOG4NET_DLL) /pkg:$(GTK_PACKAGES) $(RESOURCES_BUILD) /out:../bin/debug/$(GTK_TARGET) $^
	$(AOT) ../bin/debug/$(GTK_TARGET)
	cp $(LOG4NET_DIR)/$(LOG4NET_DLL) ../bin/debug

bin/release/$(GTK_TARGET): Frontend-GNOME/*.cs Frontend-GNOME/*/*.cs
	$(CSC) /define:$(GTK_DEFINES) /nowarn:0169 /target:exe /r:$(GTK_LIB),../bin/release/$(COMMON_TARGET),../bin/release/$(ENGINE_TARGET),../bin/release/$(FRONTEND_TARGET) /pkg:$(GTK_PACKAGES) $(RESOURCES_BUILD) /out:../bin/release/$(GTK_TARGET) $^
	$(AOT) ../bin/release/$(GTK_TARGET)

smuxi-gnome: bin/debug/$(GNOME_TARGET)
bin/debug/$(GNOME_TARGET): Frontend-GNOME/*.cs Frontend-GNOME/*/*.cs
	$(CSC) /debug /define:$(GNOME_DEFINES),TRACE,LOG4NET /nowarn:0169 /target:exe /r:$(GNOME_LIB),../bin/debug/$(COMMON_TARGET),../bin/debug/$(ENGINE_TARGET),../bin/debug/$(FRONTEND_TARGET),$(LOG4NET_DIR)/$(LOG4NET_DLL) /pkg:$(GNOME_PACKAGES) $(RESOURCES_BUILD) /out:../bin/debug/$(GNOME_TARGET) $^
	$(AOT) ../bin/debug/$(GNOME_TARGET)
	cp $(LOG4NET_DIR)/$(LOG4NET_DLL) ../bin/debug

bin/release/$(GNOME_TARGET): Frontend-GNOME/*.cs Frontend-GNOME/*/*.cs
	$(CSC) /define:$(GNOME_DEFINES) /nowarn:0169 /target:exe /r:$(GNOME_LIB),../bin/release/$(COMMON_TARGET),../bin/release/$(ENGINE_TARGET),../bin/release/$(FRONTEND_TARGET) /pkg:$(GNOME_PACKAGES) $(RESOURCES_BUILD) /out:../bin/release/$(GNOME_TARGET) $^
	$(AOT) ../bin/release/$(GNOME_TARGET)

clean:
	rm -f ../bin/debug/*.mdb
	rm -f ../bin/debug/*.exe.so
	rm -f ../bin/debug/*.dll.so
	rm -f ../bin/debug/$(COMMON_TARGET)
	rm -f ../bin/debug/$(ENGINE_TARGET)
	rm -f ../bin/debug/$(SERVER_TARGET)
	rm -f ../bin/debug/$(FRONTEND_TARGET)
	rm -f ../bin/debug/$(GTK_TARGET)
	rm -f ../bin/debug/$(GNOME_TARGET)
	rm -f ../bin/release/*.mdb
	rm -f ../bin/release/*.exe.so
	rm -f ../bin/release/*.dll.so
	rm -f ../bin/release/$(COMMON_TARGET)
	rm -f ../bin/release/$(ENGINE_TARGET)
	rm -f ../bin/release/$(SERVER_TARGET)
	rm -f ../bin/release/$(FRONTEND_TARGET)
	rm -f ../bin/release/$(GTK_TARGET)
	rm -f ../bin/release/$(GNOME_TARGET)

.PHONY: all smuxi-engine gnome-debug gnome-release gtk-debug gtk-release clean
