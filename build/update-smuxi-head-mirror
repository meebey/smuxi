#!/bin/sh

ORIG_REPO_URL=git://git.qnetp.net/smuxi.git
ORIG_REPO_DIR=smuxi-read-only
HEAD_REPO_URL="git@gitorious.org:~meebey/smuxi/meebeys-smuxi-head-mirror.git"
HEAD_REPO_DIR=smuxi-head-mirror
if [ ! -d $ORIG_REPO_DIR ]; then
	git clone $ORIG_REPO_URL $ORIG_REPO_DIR
fi
if [ ! -d $HEAD_REPO_DIR ]; then
	(mkdir $HEAD_REPO_DIR && cd $HEAD_REPO_DIR && git init && git remote add origin $HEAD_REPO_URL)
fi
(cd $ORIG_REPO_DIR && git pull && git submodule init && git submodule update)
HEAD_MSG=$(cd $ORIG_REPO_DIR && git log --no-color --first-parent -n1 --pretty="format:%h %s")
(cd $HEAD_REPO_DIR && git rm -rfq .)
rsync -a --exclude=.git/ --delete $ORIG_REPO_DIR/ $HEAD_REPO_DIR/
(cd $HEAD_REPO_DIR && git add . && git commit -m "Update from HEAD: $HEAD_MSG" && git push origin master)

