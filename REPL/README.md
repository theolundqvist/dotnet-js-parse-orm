## Setup

Uses <a href="https://github.com/waf/CSharpRepl" target="_blank"><abbr title="Github/CSharpRepl">CSharpRepl</abbr></a> to create an interactive C# environment.

Install CSharpRepl (Unix)

```
dotnet tool install -g csharprepl
```

Update CSharpRepl (Unix)

```
dotnet tool update -g csharprepl
```

## Run

Run in terminal with the following command:

```
csharprepl -r "../Bridgestars/bin/Release/netstandard2.0/Bridgestars.dll" Init.csx
```

"-r" is an argument for passing Assembly files.
Init.csx can be modified to your liking. Right now it prompts the user for email and password to set up firebase.

Or without loading the Bridgestars dll
`csharprepl`

## Tip

Create an alias for opening the Bridgestars REPL:

```
alias bridgestarsREPL="csharprepl -r \"/{Path to project folder}/Bridgestars/bin/Release/netstandard2.0/Bridgestars.dll\" {Path to project folder}/REPL/Init.csx"
```
