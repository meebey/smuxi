VERSION = 0.5.29
DEBIAN_REVISION = 1

all: build

build:
	$(MAKE) -C src -f Makefile
	#cd po/Engine && make -f Makefile.in
	#cd po/Frontend-GNOME && make -f Makefile.in

install:
	$(MAKE) -C src -f Makefile install
	#cd po/Engine && make install
	#cd po/Frontend-GNOME && make install

clean:
	$(MAKE) -C src -f Makefile clean
	#cd po/Engine && make clean
	#cd po/Frontend-GNOME && make clean

etch-dist:
	dpkg-buildpackage -rfakeroot 
	fakeroot debian/rules clean
	sudo pbuilder --build --basetgz /var/cache/pbuilder/base-etch.tgz ../smuxi_$(VERSION)-$(DEBIAN_REVISION).dsc
	mkdir -p ../releases/$(VERSION)/debian
	cp /var/cache/pbuilder/result/smuxi*$(VERSION)*.deb ../releases/$(VERSION)/debian
	cd ../releases/$(VERSION)/debian && publish smuxi $(VERSION)

win32-dist:
	mkdir -p ../releases/$(VERSION)/win32
	cp ../releases/$(VERSION)/debian/*.deb ../releases/$(VERSION)/win32
	cd ../releases/$(VERSION)/win32 && \
		for i in *.deb; do \
			dpkg -x $$i package; \
		done; \
		cp package/usr/lib/smuxi/* .; \
		rm -rf package; \
		rm -f *.deb; \
		rm -f *.mdb;
		#cp $(SMARTIRC_DIR)/$(SMARTIRC_DLL) ../../releases/$(VERSION)/win32

