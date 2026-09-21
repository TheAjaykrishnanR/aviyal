mkdir -p ./bin/publish
rm ./bin/publish/*
./Build.ps1 winexe
cp ./bin/aviyal.exe ./bin/publish/aviyal.exe
cp ./bin/swda.dll ./bin/publish/swda.dll
