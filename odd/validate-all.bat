@echo off
rem TRUST.EXE validation runner. Drives the open Unity editor via Library/Trust*Request.txt files.
setlocal enabledelayedexpansion
set REPO=C:\Users\chati\Documents\Gamejam\repo
cd /d "%REPO%"

del "%REPO%\Library\Trust*Request.txt" 2>nul
del "%REPO%\Library\Trust*Result.txt" 2>nul
del "%REPO%\Library\Trust*Validation.txt" 2>nul
del "%REPO%\Library\NucleoRuntimeValidation.txt" 2>nul
> "%REPO%\Library\ValidationRun.txt" echo start

call :req Result setup "TrustResultResult.txt"
call :req Result validate "TrustResultValidation.txt"

call :req Menu play "TrustMenuResult.txt"
call :req Menu validate "TrustMenuValidation.txt"
call :req Menu stop "TrustMenuResult.txt"

call :req Tutorial play "TrustTutorialResult.txt"
call :req Tutorial validate "TrustTutorialValidation.txt"
call :req Tutorial stop "TrustTutorialResult.txt"

call :req Acceso play "TrustAccesoResult.txt"
call :req Acceso validate "AccesoValidation.txt"
call :req Acceso stop "TrustAccesoResult.txt"

call :req Vigilancia play "TrustVigilanciaResult.txt"
call :req Vigilancia validate "VigilanciaValidation.txt"
call :req Vigilancia stop "TrustVigilanciaResult.txt"

call :req Nucleo play "TrustNucleoResult.txt"
call :req Nucleo validate "NucleoValidation.txt"
call :req Nucleo runtime "NucleoRuntimeValidation.txt"
call :req Nucleo stop "TrustNucleoResult.txt"

> "%REPO%\Library\ValidationRun.txt" echo DONE
exit /b 0

:req
set NAME=%~1
set CMD=%~2
set RESULT=%~3
set FILE=%REPO%\Library\Trust%NAME%Request.txt
echo !CMD!> "%FILE%"
echo [waiting %NAME%/%CMD%] ...> "Library\ValidationRun.txt"
set /a WAIT=0
:loop
set /a WAIT+=1
if exist "%REPO%\Library\%RESULT%" (
  echo [%NAME%/%CMD%] result:
  type "%REPO%\Library\%RESULT%"
  echo.
  del "%REPO%\Library\%RESULT%" 2>nul
  exit /b 0
)
if %WAIT% GEQ 60 (
  echo [%NAME%/%CMD%] TIMEOUT waiting for %RESULT%
  exit /b 1
)
ping -n 6 127.0.0.1 > NUL
goto loop