run_sensor_check.bat ...
 
@echo off
 
:: Execute the programs
bad_pixel_detect.exe .\
dirtdetect.exe .\
 
:: Define the output folders
set PIX_DIR=output_pix
set DIRT_DIR=output_dirt
 
echo  .
echo  .
:: Check for non-zero sized text files in output_pix
echo Checking for bad pixels...
for %%f in (%PIX_DIR%\*.txt) do (
    if %%~zf NEQ 0 (
        set "filename=%%~nxf"
        setlocal enabledelayedexpansion
        set "filename=!filename:_badpix_report.txt=!"
        echo Bad Pixels Detected: !filename!
        endlocal
    )
)
 
echo  .
echo  .
:: Check for non-zero sized text files in output_dirt
echo Checking for dirt...
for %%f in (%DIRT_DIR%\*.txt) do (
    if %%~zf NEQ 0 (
        set "filename=%%~nxf"
        setlocal enabledelayedexpansion
        set "filename=!filename:.txt=!"
        echo Dirt Detected: !filename!
        endlocal
    )
)
echo  .
echo  .
echo Process complete.
pause