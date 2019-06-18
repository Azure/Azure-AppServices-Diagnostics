@WHERE python
@IF %ERRORLEVEL% NEQ 0 goto pythonNotInstalled	

:runServer
@echo D | @xcopy /E /Y ..\\TrainingFactory ..\\run\\TrainingFactory
@cd ../run/TrainingFactory
@echo Starting Training server
@python run.pyw --debug=true
goto exit

:pythonNotInstalled
@echo It seems like python is not installed, please run the script src/Diagnostics.AIProjects/InstallPython.ps1 in powershell administrator mode

:exit
@pause