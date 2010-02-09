STFL_STATIC_LIB = /usr/lib/libstfl.a
LIB_FILE = libstflsharp.so
OBJ_FILES = $(wildcard *.o)
CLEANFILES =  $(LIB_FILE) $(OBJ_FILES)
#libstflsharpdir = $(pkglibdir)
#libstflsharp_LTLIBRARIES = libstflsharp.la
#libstflsharp_la_SOURCES =
#libstflsharp_la_LDADD = $(OBJ_FILES)
#LIBOBJS = $(OBJ_FILES)

all: $(LIB_FILE)

$(LIB_FILE): $(OBJ_FILES) 
	$(AR) -x $(STFL_STATIC_LIB)
	$(LD) -o $(LIB_FILE) -shared -lncursesw -lpthread *.o
