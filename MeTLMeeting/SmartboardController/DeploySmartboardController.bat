@echo off
echo make sure you've already "published" from within VS2008.
echo
echo (make sure that when you published, you changed:
echo SandRibbon - Properties - Publish -
echo "Installation Folder URL"
echo to: http://metl.adm.monash.edu/MeTLSmartboard/
echo Updates -
echo "Update Location"
echo to: http://metl.adm.monash.edu/MeTLSmartboard/
echo Options -
echo Product name:
echo Smartboard MeTL Input
echo Suite name:
echo {leave blank}
echo
echo Don't use this command unless you're ready to overwrite 
echo the currently deployed Clickonce on the server (metl.adm) 
echo Press Ctrl+C to exit this.  Any other keypress will continue
pause
pscp.exe -r -pw bananaman "C:\specialMeTL\MeTLMeeting\SmartboardController\publish\Application Files" deploy@refer.adm.monash.edu.au:/srv/racecarDeploy/MeTLSmartboard/
pscp.exe -pw bananaman "C:\specialMeTL\MeTLMeeting\SmartboardController\publish\index.html" deploy@refer.adm.monash.edu.au:/srv/racecarDeploy/MeTLSmartboard/
pscp.exe -pw bananaman "C:\specialMeTL\MeTLMeeting\SmartboardController\publish\MeTLSmartboardController.application" deploy@refer.adm.monash.edu.au:/srv/racecarDeploy/MeTLSmartboard/
pscp.exe -pw bananaman "C:\specialMeTL\MeTLMeeting\SmartboardController\publish\setup.exe" deploy@refer.adm.monash.edu.au:/srv/racecarDeploy/MeTLSmartboard/