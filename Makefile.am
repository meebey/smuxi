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
		po-Frontend-GNOME \
		po-Frontend-GNOME-IRC

SUBDIRS = 	src \
		$(PODIRS)

update-po:
	for PODIR in $(PODIRS); do \
		echo $$PODIR; \
		$(MAKE) -C $$PODIR $@ | egrep -e '(:$$|messages)'; \
	done
