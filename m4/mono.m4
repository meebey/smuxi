AC_DEFUN([SHAMROCK_FIND_MONO_1_0_COMPILER],
[
	SHAMROCK_FIND_PROGRAM_OR_BAIL(MCS, mcs)
])

AC_DEFUN([SHAMROCK_FIND_MONO_2_0_COMPILER],
[
	SHAMROCK_FIND_PROGRAM_OR_BAIL(MCS, gmcs)
])

AC_DEFUN([SHAMROCK_FIND_MONO_2_0_COMPILER_OR_HIGHER],
[
    if pkg-config --atleast-version=4.0 mono; then
        SHAMROCK_FIND_PROGRAM(MCS, mcs)
    fi
    if pkg-config --atleast-version=2.8 mono; then
        SHAMROCK_FIND_PROGRAM(MCS, dmcs)
    fi
    if test "x$MCS" = "x" ; then
        SHAMROCK_FIND_PROGRAM(MCS, gmcs)
    fi

    if test "x$MCS" = "x" ; then
        AC_MSG_ERROR([You need to install 'dmcs' or 'gmcs'])
    fi
])

AC_DEFUN([SHAMROCK_FIND_MONO_RUNTIME],
[
	SHAMROCK_FIND_PROGRAM_OR_BAIL(MONO, mono)
])

AC_DEFUN([SHAMROCK_CHECK_MONO_MODULE],
[
	if test "x$(uname)" = "xDarwin"; then
		export PKG_CONFIG_PATH=/Library/Frameworks/Mono.framework/Versions/Current/lib/pkgconfig:$PKG_CONFIG_PATH
	fi
	PKG_CHECK_MODULES(MONO_MODULE, mono >= $1)
])

AC_DEFUN([SHAMROCK_CHECK_MONO_GAC_ASSEMBLIES],
[
    CLR_VERSIONS="2.0 3.5 4.0 4.5"
    for ASM in $(echo "$*" | cut -d, -f2- | sed 's/\,/ /g'); do
        AC_MSG_CHECKING([Mono GAC for $ASM.dll])
        found=0
        for CLR_VER in $CLR_VERSIONS; do
            if test \
                -e "$($PKG_CONFIG --variable=libdir mono)/mono/$CLR_VER/$ASM.dll" -o \
                -e "$($PKG_CONFIG --variable=prefix mono)/lib/mono/$CLR_VER/$ASM.dll"; then
                found=1
            fi
        done
        if test "x$found" = "x1"; then
            AC_MSG_RESULT([found])
        else
            AC_MSG_RESULT([not found])
            AC_MSG_ERROR([missing required Mono assembly: $ASM.dll])
        fi
    done
])

