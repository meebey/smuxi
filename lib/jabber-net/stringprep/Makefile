#--------------------------------------------------------------------------
# Copyrights
#
# Portions created by or assigned to Cursive Systems, Inc. are
# Copyright (c) 2002-2004 Cursive Systems, Inc.  All Rights Reserved.  Contact
# information for Cursive Systems, Inc. is available at
# http://www.cursive.net/.
#
# License
#
# Jabber-Net can be used under either JOSL or the GPL.
# See LICENSE.txt for details.
# --------------------------------------------------------------------------

# The purpose of this Makefile is to facilitate mono builds.

SOURCES = \
$(CURDIR)/AssemblyInfo.cs \
$(CURDIR)/Nameprep.cs \
$(CURDIR)/Plain.cs \
$(CURDIR)/Profile.cs \
$(CURDIR)/steps/BidiStep.cs \
$(CURDIR)/steps/MapStep.cs \
$(CURDIR)/steps/NFKCStep.cs \
$(CURDIR)/steps/ProfileStep.cs \
$(CURDIR)/steps/ProhibitStep.cs \
$(CURDIR)/steps/ResourceLoader.cs \
$(CURDIR)/unicode/Combining.cs \
$(CURDIR)/unicode/Compose.cs \
$(CURDIR)/unicode/Decompose.cs \
$(CURDIR)/unicode/ResourceLoader.cs \
$(CURDIR)/XmppNode.cs \
$(CURDIR)/XmppResource.cs

RESOURCES = \
-resource:$(CURDIR)/steps/RFC3454.resources \
-resource:$(CURDIR)/unicode/Unicode.resx

SYSTEM_REFERENCES = -r:System.dll

MCS_OPTIONS =   -lib:$(CURDIR)/bin/debug -g \
		-define:DEBUG

ASSEMBLIES =

all: bin/debug/stringprep.dll

bin/debug/stringprep.dll: $(SOURCES)
	-mkdir -p bin/debug
	cd bin/debug && mcs $(MCS_OPTIONS) -target:library \
	-out:"stringprep.dll" $(RESOURCES) $(SYSTEM_REFERENCES) \
	$(SOURCES) $(ASSEMBLIES) 

clean:
	rm -rf bin