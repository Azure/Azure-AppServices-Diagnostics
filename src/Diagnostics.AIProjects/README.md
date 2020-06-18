
## Installing python with required packages (one time)
### Quick Install
Run the Scripts/InstallPython.ps1 script in powershell. If you get a "cannot be loaded because running scripts is disabled on this system" error, what you need to do is:
Run powershell as administrator and run the below command
```Set-ExecutionPolicy RemoteSigned
```

Then run the Scripts/InstallPython.ps1 script in the same powershell session. This should do it generally.

---
### If it does not (manual install)
#### First Step : Install python
Download python setup from https://www.python.org/ftp/python/3.6.4/python-3.6.4-amd64.exe and install on your system. Make sure to select add to path option while installing.

To verify installed python go to command prompt and run command
```python```
or 

```python --version```
Also verify if you can run pip python package manager by running command
```pip```
If you face issues like command pip not found, look at your environment path variable and hunt down the path to python (let's call it PYTHON_PATH). Add a new value to PATH which is PYTHON_PATH\Scripts (this is the folder where pip executable resides).

#### Second Step : Create Development Environment
Go to the folder on your machine where you can allow python to do all its package stuff and open it in command prompt. (Recommended folder is Diagnostics.AIProjects\Scripts)
Run
```python -m venv searchenv```
This will create a development environment called searchenv in the folder.
Now we need to activate the environment.
```.\searchenv\Scripts\activate```
After this you will see a (searchenv) prepended to your command prompt. You are in the environment!

#### Third Step: Install all the required packages
While the searchenv is activated, go to the app folder i.e. Diagnostics.AIProjects\SearchAPI and run
```pip install -r requirements.txt```

---
## Running the server
To run the server, first we need to activate the searchenv. So while you are in **Diagnostics.AIProjects** folder, go to command prompt and run
```.\Scripts\searchenv\Scripts\activate```
Once you are in the environment, navigate to the **SearchAPI** folder i.e. the app folder and run
```python run.py```
That's it!
