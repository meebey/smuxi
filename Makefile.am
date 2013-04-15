# Engines
if ENABLE_ENGINE_CAMPFIRE
PO_ENGINE_CAMPFIRE = po-Engine-Campfire
endif

if ENABLE_ENGINE_IRC
PO_ENGINE_IRC = po-Engine-IRC
endif

if ENABLE_ENGINE_TWITTER
PO_ENGINE_TWITTER = po-Engine-Twitter
endif

# Frontends
if ENABLE_FRONTEND_GNOME
PO_FRONTEND_GNOME = po-Frontend-GNOME
endif

if ENABLE_FRONTEND_GNOME_IRC
PO_FRONTEND_GNOME_IRC = po-Frontend-GNOME-IRC
endif

ACLOCAL_AMFLAGS = -I .

EXTRA_DIST =	install-sh \
		expansions.m4 \
		mono.m4 \
		programs.m4 \
		LICENSE \
		README \
		CREDITS \
		PROFILING \
		FEATURES \
		BUGS \
		TODO

PODIRS =	po \
		po-Engine \
		$(PO_ENGINE_CAMPFIRE) \
		$(PO_ENGINE_IRC) \
		$(PO_ENGINE_TWITTER) \
		po-Server \
		po-Frontend \
		$(PO_FRONTEND_GNOME) \
		$(PO_FRONTEND_GNOME_IRC)

SUBDIRS = 	lib \
		src \
		$(PODIRS)

# forcely disable parallel builds
export MAKEFLAGS=-j1

update-po:
	for PODIR in $(PODIRS); do \
		echo $$PODIR; \
		$(MAKE) -C $$PODIR $@ | egrep -e '(:$$|messages)'; \
	done

update-pot:
	for PODIR in $(PODIRS); do \
		echo $$PODIR; \
		$(MAKE) -C $$PODIR clean; \
		$(MAKE) -C $$PODIR check 2>&1 | grep -v 'mismatched quotes at line' 2>&1; \
	done

update-transifex:
	tx pull -l ca,da,de,en_GB,es,fi,hr,nb_NO,pl,ru,sk,sv,tr,ur,zh_CN

mail-po: update-po
	for PODIR in $(PODIRS); do \
		echo $$PODIR; \
		podebconf-report-po \
			--deadline=+14days \
			--notdebconf \
			--package="Smuxi ($$PODIR)" \
			--utf8 \
			--from="Mirco Bauer <meebey@meebey.net>" \
			--smtp=booster.qnetp.net \
			--podir=$$PODIR \
			--langs=cs,es_AR,it,pt; \
	done

call-po: update-po
	for PODIR in $(PODIRS); do \
		echo $$PODIR; \
		podebconf-report-po \
			--deadline=+14days \
			--notdebconf \
			--package="Smuxi ($$PODIR)" \
			--utf8 \
			--from="Mirco Bauer <meebey@debian.org>" \
			--smtp=booster.qnetp.net \
			--podir=$$PODIR \
			--call; \
	done
	
LIB_DIR = $(top_builddir)/lib
BUILD_DIR = $(top_builddir)/bin/$(PROFILE)
WIN32_LIB_DIR = $(LIB_DIR)/win32
WIN32_BUILD_DIR = $(top_builddir)/bin-win32
WIN32_ICON_THEME_DIR = $(top_srcdir)/images/Faenza-Smuxi
OSX_LIB_DIR = $(LIB_DIR)/osx
OSX_ICON_THEME_DIR = $(top_srcdir)/images/Faenza-Smuxi
OSX_BUILD_DIR = $(top_builddir)/bin-osx
OSX_APP_DIR = $(OSX_BUILD_DIR)/Smuxi.app
OSX_CONTENTS_DIR = $(OSX_APP_DIR)/Contents
OSX_RESOURCES_DIR = $(OSX_CONTENTS_DIR)/Resources
OSX_BINARIES_DIR = $(OSX_CONTENTS_DIR)/MacOS
LINUX_STATIC_BUILD_DIR = $(top_builddir)/bin-linux-static

WIN32_EXE_FILES = \
	$(BUILD_DIR)/smuxi-frontend-gnome.exe \
	$(BUILD_DIR)/smuxi-server.exe

WIN32_FILES = \
	$(WIN32_EXE_FILES) \
	$(LIB_DIR)/Mono.Posix.dll \
	$(LIB_DIR)/Nini.dll \
	$(LIB_DIR)/log4net.dll \
	$(WIN32_LIB_DIR)/plink.exe \
	$(WIN32_LIB_DIR)/fixedsys500c/Fixedsys500c.ttf \
	$(BUILD_DIR)/smuxi-common.dll \
	$(BUILD_DIR)/smuxi-engine.dll \
	$(BUILD_DIR)/smuxi-engine-campfire.dll \
	$(BUILD_DIR)/smuxi-engine-irc.dll \
	$(BUILD_DIR)/smuxi-engine-twitter.dll \
	$(BUILD_DIR)/smuxi-engine-xmpp.dll \
	$(BUILD_DIR)/smuxi-frontend-gnome-irc.dll \
	$(BUILD_DIR)/smuxi-frontend.dll \
	$(BUILD_DIR)/Meebey.SmartIrc4net.dll \
	$(BUILD_DIR)/Twitterizer2.dll \
	$(BUILD_DIR)/Newtonsoft.Json.dll \
	$(BUILD_DIR)/jabber-net.dll \
	$(BUILD_DIR)/Db4objects.Db4o.dll \
	$(BUILD_DIR)/ServiceStack.Text.dll \
	$(BUILD_DIR)/ServiceStack.Interfaces.dll \
	$(BUILD_DIR)/ServiceStack.Common.dll \
	$(WIN32_LIB_DIR)/smuxi-frontend-gnome.exe.config \
	$(WIN32_LIB_DIR)/smuxi-server.exe.config

OSX_FILES = \
	$(LIB_DIR)/Nini.dll \
	$(LIB_DIR)/log4net.dll \
	$(BUILD_DIR)/smuxi-frontend-gnome.exe \
	$(BUILD_DIR)/smuxi-frontend-gnome.exe.config \
	$(BUILD_DIR)/smuxi-frontend-gnome-irc.dll \
	$(BUILD_DIR)/smuxi-server.exe \
	$(BUILD_DIR)/smuxi-common.dll \
	$(BUILD_DIR)/smuxi-engine.dll \
	$(BUILD_DIR)/smuxi-engine-campfire.dll \
	$(BUILD_DIR)/smuxi-engine-irc.dll \
	$(BUILD_DIR)/smuxi-engine-twitter.dll \
	$(BUILD_DIR)/smuxi-engine-xmpp.dll \
	$(BUILD_DIR)/smuxi-frontend.dll \
	$(BUILD_DIR)/Meebey.SmartIrc4net.dll \
	$(BUILD_DIR)/Twitterizer2.dll \
	$(BUILD_DIR)/Newtonsoft.Json.dll \
	$(BUILD_DIR)/jabber-net.dll \
	$(BUILD_DIR)/Db4objects.Db4o.dll \
	$(BUILD_DIR)/ServiceStack.Text.dll \
	$(BUILD_DIR)/ServiceStack.Interfaces.dll \
	$(BUILD_DIR)/ServiceStack.Common.dll

LINUX_STATIC_FILES = \
	$(LIB_DIR)/Mono.Posix.dll \
	$(LIB_DIR)/Nini.dll \
	$(LIB_DIR)/log4net.dll \
	$(BUILD_DIR)/smuxi-common.dll \
	$(BUILD_DIR)/smuxi-engine.dll \
	$(BUILD_DIR)/smuxi-engine-irc.dll \
	$(BUILD_DIR)/smuxi-engine-twitter.dll \
	$(BUILD_DIR)/Meebey.SmartIrc4net.dll \
	$(BUILD_DIR)/Twitterizer2.dll \
	$(BUILD_DIR)/Newtonsoft.Json.dll
		
dist-win32:
	if [ -x /usr/bin/dmcs ]; then \
		COMPILER_OVERRIDE="MCS=/usr/bin/dmcs"; \
		echo COMPILER_OVERRIDE="$$COMPILER_OVERRIDE"; \
	fi; \
	./autogen.sh $$COMPILER_OVERRIDE --without-indicate --without-messaging-menu --without-notify --without-dbus \
	  --with-db4o=included \
	  --with-vendor-package-version="dist-win32"
	$(MAKE)
	mkdir -p $(WIN32_BUILD_DIR)
	for FILE in $(WIN32_FILES); do \
		cp $$FILE $(WIN32_BUILD_DIR); \
	done
	for PODIR in $(PODIRS); do \
		$(MAKE) -C $$PODIR install itlocaledir=$(abs_top_builddir)/$(WIN32_BUILD_DIR)/locale; \
	done
	mkdir -p $(WIN32_BUILD_DIR)/icons
	cp -r $(WIN32_ICON_THEME_DIR) $(WIN32_BUILD_DIR)/icons
	makensis $(top_srcdir)/src/smuxi-win32.nsis

dist-osx:
	if [ -x /usr/bin/dmcs ]; then \
		COMPILER_OVERRIDE="MCS=/usr/bin/dmcs"; \
		echo COMPILER_OVERRIDE="$$COMPILER_OVERRIDE"; \
	fi; \
	./autogen.sh $$COMPILER_OVERRIDE --without-indicate --without-messaging-menu --without-notify --without-dbus \
	  --with-db4o=included \
	  --with-vendor-package-version="dist-osx"
	$(MAKE)
	mkdir -p $(OSX_BINARIES_DIR) $(OSX_RESOURCES_DIR)
	cp $(OSX_LIB_DIR)/Info.plist $(OSX_CONTENTS_DIR)
	cp $(OSX_LIB_DIR)/smuxi $(OSX_BINARIES_DIR)
	cp $(OSX_LIB_DIR)/smuxi.icns $(OSX_RESOURCES_DIR)
	for FILE in $(OSX_FILES); do \
		cp $$FILE $(OSX_BINARIES_DIR); \
	done
	mkdir -p $(OSX_BINARIES_DIR)/icons
	cp -r $(OSX_ICON_THEME_DIR) $(OSX_BINARIES_DIR)/icons
	-rm smuxi-osx.zip
	cd $(OSX_BUILD_DIR) && zip -r ../smuxi-osx.zip Smuxi.app

dist-linux-x86-static:
	$(MAKE)
	mkdir -p $(LINUX_STATIC_BUILD_DIR)
	mkbundle --static -o $(LINUX_STATIC_BUILD_DIR)/smuxi-server $(BUILD_DIR)/smuxi-server.exe
	for FILE in $(LINUX_STATIC_FILES); do \
		cp $$FILE $(LINUX_STATIC_BUILD_DIR); \
	done
	tar -czf smuxi-server-$(VERSION)-linux-x86-static.tar.gz $(LINUX_STATIC_BUILD_DIR)
 
run: run-gnome
run-gnome: all
	cd $(BUILD_DIR) && \
		mono --debug ./smuxi-frontend-gnome.exe -d

run-stfl: all
	cd $(BUILD_DIR) && \
		mono --debug ./smuxi-frontend-stfl.exe -d 2> smuxi-frontend-stfl.err

run-server: all
	cd $(BUILD_DIR) && \
		mono --debug ./smuxi-server.exe -d

.NOTPARALLEL:
