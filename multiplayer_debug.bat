echo off 
pushd ".\bin\Debug\net8.0-windows\"
start "" ".\ViMG3.exe" --windowpos 0 0 --mode TheIsland --netmode Server -p -c
timeout /t 1
start "" ".\ViMG3.exe" --windowpos 648 0 --mode TheIsland --netmode Client -p
popd