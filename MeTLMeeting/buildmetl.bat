@echo off

SET build=%1
SET rev=%2
REM Defaults
SET branchname=MeTLOverLib
set buildconfig=""
SHIFT & SHIFT

IF "%rev%"=="" GOTO INVALIDPARAMS
IF "%build%"=="prod" (
	SET buildconfig=Release
)
IF "%build%"=="staging" (
	SET buildconfig=Debug
)
IF "%buildconfig%"=="" GOTO INVALIDPARAMS

REM Default option is to publish
SET buildtargets=Clean;Build;Publish

:LOOP
IF NOT "%1"=="" (
	IF "%1"=="-skippublish" (
		SET buildtargets=Clean;Build
		REM SHIFT
	)
	IF "%1"=="-skipupdate" (
		SET skipupdate=1
		REM SHIFT
	)
	IF "%1"=="-skippull" (
		SET skippull=1
		REM SHIFT
	)
	IF "%1"=="-branch" (
		SET branchname=%2
		SHIFT
	)
	SHIFT
	GOTO LOOP
)

echo.
echo Building Configuration=%buildconfig% with ApplicationRevision=%rev%.

IF DEFINED skippull GOTO UPDATE

:PULL
echo.
echo Grabbing latest from source control
hg pull

IF %errorlevel% NEQ 0 GOTO ERROR

:UPDATE
IF DEFINED skipupdate GOTO BRANCH
echo.
echo Updating to last changeset 
hg update -C

IF %errorlevel% NEQ 0 GOTO ERROR

:BRANCH
echo.
echo Changing to branch %branchname%
hg update %branchname%

IF %errorlevel% NEQ 0 GOTO ERROR

:BUILD
echo Building...
echo.

CALL "C:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\vcvarsall.bat" x86

msbuild.exe MeTL.sln /l:FileLogger,Microsoft.Build.Engine;logfile=MeTLBuildLog.log /p:Configuration=%buildconfig% /p:Platform="Any CPU" /p:ApplicationRevision=%rev% /t:%buildtargets%
IF %errorlevel% NEQ 0 GOTO ERROR

:SUCCESS
echo.
echo Done.
GOTO :EOF

:INVALIDPARAMS
echo BuildScript Help v0.3b
echo.
echo %0 staging OR prod rev [-branch name] [-skippublish] [-skipupdate] [-skippull]
echo.
echo Default branch is MeTLOverLib.
echo.
echo The following example will build a staging version with the revision 
echo number 289 using the default branch: 
echo.
echo %0 staging 289
echo.
echo -branch			Update to the specified branch name.
echo.
echo -skippublish		Clean and build the target only.
echo.
echo -skipupdate		Build using the working directory.
echo.
echo -skippull		Do not update from source control.
echo.
echo.
GOTO :EOF

:ERROR
echo There was an error with the build.