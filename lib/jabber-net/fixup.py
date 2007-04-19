# --------------------------------------------------------------------------
# Copyrights
#
# Portions created by or assigned to Cursive Systems, Inc. are
# Copyright (c) 2002-2007 Cursive Systems, Inc.  All Rights Reserved.  Contact
# information for Cursive Systems, Inc. is available at
# http://www.cursive.net/.
#
# License
#
# Jabber-Net can be used under either JOSL or the GPL.
# See LICENSE.txt for details.
# --------------------------------------------------------------------------

import sys

import os
from os.path import join
import re

COPY = re.compile(r"Copyright \(c\) 200[0-9]-200[0-9] Cursive Systems")

def fixup(f):
    # print f
    try:
        bak = f + ".bak"
        os.rename(f, bak)
        fh = open(f, 'w')
        bh = open(bak, 'r')
        cr = False
        for line in bh:
            line = line.rstrip()
            line = line.expandtabs(4)
            if not cr:
                cl = COPY.sub("Copyright (c) 2002-2007 Cursive Systems", line)
                if cl != line:
                    line = cl
                    cr = True
            fh.write(line + "\n")
        fh.close()
        bh.close()
        if not cr:
            print "NO COPYRIGHT for: " + f
            
    except IOError:
        print "IOError for: " + f
        
for root, dirs, files in os.walk(os.getcwd()):
    for f in files:
        if f.endswith('.cs'):
            fixup( join(root, f) )
            
    if 'CVS' in dirs:
        dirs.remove('CVS')  # don't visit CVS directories
