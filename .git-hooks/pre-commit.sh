#!/bin/bash
FILES=$
( -git 
$diff
--cached
--name-only 
--diff-filter 
$=ACM"*.cs"
) if [
-n "$FILES" 
] then ".dotnet format 
./bitwarden-server
.sln
--no-restore 
--include"
$FILES
 echo $FILES
 | xargs git add fi
