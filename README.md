# RPUnity Toolkit
A collection of scripts, editor scripts, utilities to improve Unity workflow and somewhat patch Unity's mistakes. Feel free to modify the contents for your usage.

# Requirements
1. Unity Editor version (at least): 2020.3.1f16.
2. API compatibility version: .NET 4.0 (Project Settings -> Player -> Configuration -> API compatibility version -> Set to .NET 4.0). This might not be required in the far future, and there might be alternate to currently using API (RuntimeBinder, etc...).  
3. Dependencies for this package are not needed (for now, some might be required in the future. Ex: DOTween, Newtonsoft.Json, etc...)

# Installation
You can install this by using one of the two following ways:

1. Installation via Package Manager and git URL:  
  1.1. Obtains the installation URL via HTTP  
  1.2. Use Unity's Package Manager  
  1.3. Find add button (usually with the plus sign) -> Add package from git URL... -> paste installation URL  
  1.4. Click "Add"  
  
2. Download or Clone the repository into project Assets folder

# Drawbacks
1. No Light mode support for UI (yet?)  
2. Some feature might not be stable and might have some bugs. If something odd occured, open an issue.  
3. Might need some extra works to increase performance.  

# Questions and answers (that nobody ask)
## 1. Q: I updated the package via Git URL, and my Intellisense cannot detect any class of it/undefined references?
Answer: It's best to regenerate the project files. In Unity Editor, go to Edits -> Preferences -> External Tools -> Click on the "Regenerate Project Files" button.

# Acknowledge
This is one man project, so everything might not be perfect, but feedbacks, bug reports are highly welcome. Original version owned by RealityProgrammer, I'm owned by my mother, therefore this whole project is owned by my ancestors. Clone, modification are allowed by the MIT license.
