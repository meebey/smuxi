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
