param([string]$isDemo="")

$env:DYLD_PRINT_LIBRARIES=1

if($isDemo -ne "demo") 
{
    &g++ -dynamiclib -finput-charset=UTF-8 -framework Cocoa -x objective-c++ -current_version 1.0  -o libchromely.dylib chromely_mac.mm -mmacosx-version-min="10.11"
    if($LASTEXITCODE -gt 0) 
    {
        exit $LASTEXITCODE
    }
    Copy-Item -Force -Path "libchromely.dylib" -Destination "../Chromely/Native/MacCocoa"
    Copy-Item -Force -Path "libchromely.dylib" -Destination "../../Chromely.XamMac/bin/Debug/Chromely.XamMac.app/Contents/MonoBundle"
}
else
{
    &g++ -dynamiclib -finput-charset=UTF-8 -framework Cocoa -x objective-c++ -current_version 1.0  -o libchromely.dylib chromely_mac.mm -mmacosx-version-min="10.11"
    
    if($LASTEXITCODE -gt 0) 
    {
        exit $LASTEXITCODE
    }
    &g++ -v -framework Cocoa -x objective-c++ -L. -lchromely -o demo main.mm
    ./demo
}

