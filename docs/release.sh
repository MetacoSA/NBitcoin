#!/bin/sh
set -e

export VSINSTALLDIR="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community"
export VisualStudioVersion="15.0"

SOURCE_DIR=$PWD
TEMP_REPO_DIR=$PWD/../NBitcoin-gh-pages
docfx ./docs/docfx.json


echo "Removing temporary doc directory $TEMP_REPO_DIR"
rm -rf $TEMP_REPO_DIR
mkdir $TEMP_REPO_DIR

echo "Cloning the repo with the gh-pages branch"
git clone $(git config --get remote.origin.url) --branch gh-pages $TEMP_REPO_DIR 2> /dev/null

echo "Clear repo directory"
cd $TEMP_REPO_DIR
git rm -r --ignore-unmatch *

echo "Copy documentation into the repo"
cp -r $SOURCE_DIR/docs/_site/* .

echo "Push the new docs to the remote branch"
git add . -A 2> /dev/null
git commit -m "Update generated documentation" 2> /dev/null
git push origin gh-pages 2> /dev/null