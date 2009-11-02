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
		po-Engine-IRC \
		po-Server \
		po-Frontend \
		po-Frontend-GNOME \
		po-Frontend-GNOME-IRC

SUBDIRS = 	src \
		lib \
		$(PODIRS)

update-po:
	for PODIR in $(PODIRS); do \
		echo $$PODIR; \
		$(MAKE) -C $$PODIR $@ | egrep -e '(:$$|messages)'; \
	done

mail-po: update-po
	for PODIR in $(PODIRS); do \
		echo $$PODIR; \
		podebconf-report-po \
			--nolanguageteam \
			--deadline=7days \
			--notdebconf \
			--package="Smuxi ($$PODIR)" \
			--utf8 \
			--from="Mirco Bauer <meebey@meebey.net>" \
			--smtp=booster.qnetp.net \
			--podir=$$PODIR; \
	done

LIB_DIR = $(top_builddir)/lib
WIN32_LIB_DIR = $(LIB_DIR)/win32
BUILD_DIR = $(top_builddir)/bin/$(PROFILE)
WIN32_BUILD_DIR = $(top_builddir)/bin-win32

WIN32_EXE_FILES = \
	$(BUILD_DIR)/smuxi-frontend-gnome.exe \
	$(BUILD_DIR)/smuxi-server.exe

WIN32_FILES = \
	$(WIN32_EXE_FILES) \
	$(LIB_DIR)/Meebey.SmartIrc4net.dll \
	$(LIB_DIR)/Mono.Posix.dll \
	$(LIB_DIR)/Nini.dll \
	$(LIB_DIR)/log4net.dll \
	$(WIN32_LIB_DIR)/MonoPosixHelper.dll \
	$(WIN32_LIB_DIR)/zlib1.dll \
	$(WIN32_LIB_DIR)/plink.exe \
	$(WIN32_LIB_DIR)/fixedsys500c/Fixedsys500c.ttf \
	$(BUILD_DIR)/smuxi-common.dll \
	$(BUILD_DIR)/smuxi-engine.dll \
	$(BUILD_DIR)/smuxi-engine-irc.dll \
	$(BUILD_DIR)/smuxi-frontend-gnome-irc.dll \
	$(BUILD_DIR)/smuxi-frontend.dll \
	$(WIN32_LIB_DIR)/smuxi-frontend-gnome.exe.config \
	$(WIN32_LIB_DIR)/smuxi-server.exe.config
		
dist-win32:
	mkdir -p $(WIN32_BUILD_DIR)
	for FILE in $(WIN32_FILES); do \
		cp $$FILE $(WIN32_BUILD_DIR); \
	done
	for FILE in $(WIN32_EXE_FILES); do \
		wine ../releases/win32/build-env/CorFlags.exe /nologo /32BIT+ $(WIN32_BUILD_DIR)/$$(basename $$FILE); \
	done
	for PODIR in $(PODIRS); do \
		$(MAKE) -C $$PODIR install itlocaledir=$(abs_top_builddir)/$(WIN32_BUILD_DIR)/locale; \
	done
	makensis $(top_srcdir)/src/smuxi-win32.nsis
