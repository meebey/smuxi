
# program name, used as textdomain
GETTEXT_PACKAGE = smuxi-engine
# space seperated list of languages we have translations for (*.mo)
CATALOGS = de.mo dk.mo

MSGFMT = /usr/bin/msgfmt
XGETTEXT = /usr/bin/xgettext
INTLTOOL_UPDATE = intltool-update
MSGMERGE = $(INTLTOOL_UPDATE) --gettext-package $(GETTEXT_PACKAGE) --dist
GENPOT   = $(INTLTOOL_UPDATE) --gettext-package $(GETTEXT_PACKAGE) --pot
CATOBJEXT = .mo

POTFILES = 

default: Makefile $(GETTEXT_PACKAGE).pot update-po $(CATALOGS)

update-po:
	tmpdir=$$(pwd); \
	for cat in $(CATALOGS); do \
	  cat=$$(basename $$cat); \
	  lang=$$(echo $$cat | sed 's/\$(CATOBJEXT)$$//'); \
	  echo "$$lang:"; \
	  cp $$tmpdir/$$lang.po $$tmpdir/$$lang.po.old; \
	  $(MSGMERGE) -o $$tmpdir/$$lang.new.po $$lang; \
	  rc=$$?; \
	  if [ $$rc != 0 ]; then \
	    echo "msgmerge for $$cat failed!"; \
	    rm -f $$tmpdir/$$lang.new.po; \
	    exit 1; \
	  else \
	    mv $$tmpdir/$$lang.new.po $$tmpdir/$$lang.po; \
	  fi; \
	done

install:
	for CATALOG in $(CATALOGS); do \
	  LANG=$$(echo $$CATALOG | sed 's/\$(CATOBJEXT)$$//'); \
	  mkdir -p ../../bin/mono/release/locale/$$LANG/LC_MESSAGES; \
	  mkdir -p ../../bin/mono/debug/locale/$$LANG/LC_MESSAGES; \
	  mkdir -p ../../bin/net/release/locale/$$LANG/LC_MESSAGES; \
	  mkdir -p ../../bin/net/debug/locale/$$LANG/LC_MESSAGES; \
	  cp $$CATALOG ../../bin/mono/release/locale/$$LANG/LC_MESSAGES/$(GETTEXT_PACKAGE).mo; \
	  cp $$CATALOG ../../bin/mono/debug/locale/$$LANG/LC_MESSAGES/$(GETTEXT_PACKAGE).mo; \
	  cp $$CATALOG ../../bin/net/release/locale/$$LANG/LC_MESSAGES/$(GETTEXT_PACKAGE).mo; \
	  cp $$CATALOG ../../bin/net/debug/locale/$$LANG/LC_MESSAGES/$(GETTEXT_PACKAGE).mo; \
	done

$(GETTEXT_PACKAGE).pot: $(POTFILES)
	$(GENPOT)

.SUFFIXES: .po .mo

.po.mo:
	$(MSGFMT) -o $@ $<

# POTFILES is created from POTFILES.in by stripping comments, empty lines
# and Intltool tags (enclosed in square brackets), and appending a full
# relative path to them
POTFILES: POTFILES.in
	rm -f $@
	sed -e '/^#/d' \
	    -e "s/^\[.*\]//" \
	    -e '/^[ 	]*$$/d' \
	    -e "s@.*@../../& @" < $< \
	  | sed -e '$$s/\\$$//' > $@
	chmod a-w $@

#Makefile: POTFILES=$(shell cat POTFILES)
Makefile: Makefile.in POTFILES
	rm -f $@
	sed -e "s,POTFILES = ,POTFILES = $(POTFILES)," < Makefile.in > $@
	chmod a-w $@

clean:
	rm -f POTFILES
	rm -f Makefile
	rm -f $(GETTEXT_PACKAGE).pot
	rm -f *.mo

.PHONY: default update-po clean
