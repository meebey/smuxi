# build with:
# nix-build --expr 'let pkgs = import <nixpkgs> { }; in pkgs.callPackage ./default.nix { gitBranch = "BRANCH_REF"; }'
{ stdenv, pkgs
, autoconf, automake, itstool, intltool, pkg-config
, glib
, gettext
, mono
, stfl
, makeWrapper, lib
, guiSupport ? true
, gtk-sharp-2_0
, gdk-pixbuf, pango # these libraries are loaded/needed at runtime
, gitBranch ? "master"
}:

stdenv.mkDerivation rec {
  pname = "smuxi";
  version = "1.2.1";
  runtimeLoaderEnvVariableName = if stdenv.isDarwin then
                                   "DYLD_FALLBACK_LIBRARY_PATH"
                                 else
                                   "LD_LIBRARY_PATH";

  src = fetchGit {
    url = "https://github.com/meebey/smuxi.git";
    ref = gitBranch;
    submodules = true;
    name = "${pname}-source-${version}-${gitBranch}";
  };
  # build-time tools/macros
  nativeBuildInputs = [ autoconf automake itstool intltool gettext pkg-config ];
  # runtime/library dependencies
  buildInputs = [
    mono
    gettext
    stfl
    makeWrapper ] ++ lib.optionals (guiSupport) [
      gtk-sharp-2_0
      # loaded at runtime by GTK#
      gdk-pixbuf pango
    ];

  preConfigure = ''
    GETTEXT_MACRO_PATH=${gettext}/share/gettext/m4
    if [ -d "$GETTEXT_MACRO_PATH" ]; then
      ACLOCAL_FLAGS="-I ${gettext}/share/gettext/m4"
    fi
    NOCONFIGURE=1 NOGIT=1 ACLOCAL_FLAGS="$ACLOCAL_FLAGS" ./autogen.sh

    # TODO: include $(git describe) in package version, but how?
    configureFlagsArray+=(
      "--with-vendor-package-version=dist-nix ${version}+${gitBranch}"
    )
  '';
  configureFlags = [
    "--disable-frontend-gnome"
    "--enable-frontend-stfl"
  ] ++ lib.optional guiSupport "--enable-frontend-gnome";
  postInstall = ''
    makeWrapper "${mono}/bin/mono" "$out/bin/smuxi-message-buffer" \
      --add-flags "$out/lib/smuxi/smuxi-message-buffer.exe" \
      --prefix ${runtimeLoaderEnvVariableName} : ${lib.makeLibraryPath [
                                                  gettext
                                                 ]}

    makeWrapper "${mono}/bin/mono" "$out/bin/smuxi-server" \
      --add-flags "$out/lib/smuxi/smuxi-server.exe" \
      --prefix ${runtimeLoaderEnvVariableName} : ${lib.makeLibraryPath [
                                                  gettext
                                                 ]}

    makeWrapper "${mono}/bin/mono" "$out/bin/smuxi-frontend-stfl" \
      --add-flags "$out/lib/smuxi/smuxi-frontend-stfl.exe" \
      --prefix ${runtimeLoaderEnvVariableName} : ${lib.makeLibraryPath [
                                                  gettext stfl
                                                 ]}

    # TODO: put into guiSupport block?
    makeWrapper "${mono}/bin/mono" "$out/bin/smuxi-frontend-gnome" \
      --add-flags "$out/lib/smuxi/smuxi-frontend-gnome.exe" \
      --prefix MONO_GAC_PREFIX : ${if guiSupport then gtk-sharp-2_0 else ""} \
      --prefix ${runtimeLoaderEnvVariableName} : ${lib.makeLibraryPath [
                                                   gettext
                                                   glib
                                                   gtk-sharp-2_0 gtk-sharp-2_0.gtk gdk-pixbuf pango
                                                  ]}

    # install log4net and nini libraries
    mkdir -p $out/lib/smuxi/
    cp -a lib/log4net.dll $out/lib/smuxi/
    cp -a lib/Nini.dll $out/lib/smuxi/

    # install GTK+ icon theme on Darwin
    ${if guiSupport && stdenv.isDarwin then "
      mkdir -p $out/lib/smuxi/icons/
      cp -a images/Smuxi-Symbolic $out/lib/smuxi/icons/
    " else ""}
  '';
  
  meta = with lib; {
    homepage = "https://smuxi.im/";
    downloadPage = "https://smuxi.im/download/";
    changelog = "https://github.com/meebey/smuxi/releases/tag/v${version}";
    description = "WRITE ME";
    platforms = platforms.unix; # Smuxi is supported on Linux, *BSD and MacOS
    license = lib.licenses.gpl2Plus;
    maintainers = [
      "Mirco Bauer <meebey@meebey.net>"
    ];
  };
}
