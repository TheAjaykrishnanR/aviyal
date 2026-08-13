mkdir -p ./bin/publish
rm ./bin/publish/*
./Build.ps1 winexe
mv ./bin/aviyal.exe ./bin/publish/aviyal.exe
