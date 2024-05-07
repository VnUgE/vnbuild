# vnbuild
*Automatically builds and deploys binaries from git-based configurable pipelines with proper indexing for web based deployments*

## Introduction
I built this tool for repeatable builds, defined by the "code" in a repo via a command line interface, that includes integration with MSBuild solutions and projects, along with leaf projects defined by a `package.json` file. It needed to work well with multiple projects per repo (aka module). Next, it needed to publish the packages produced by the build step where they could be easily shared via a website. Finally I didn't want to be forced to use a huge CI system or someone else's servers to build my code. VNBuild was born in the winter of 2022 and has had incremental updates since. 

> [!WARNING]
> This tool relies on Typin, which has not been actively developed in multiple years.

## Install
Follow the links below for software downloads and extended documentation. Releases are gzip tar archives that have sh256 sums and pgp signatures, along with complete source code and repository archive.

**[Builds and Source](https://www.vaughnnugent.com/resources/software/modules/vnbuild)** Download the latest package for your operating system and architecture.  
**[Docs and Articles](https://www.vaughnnugent.com/resources/software/articles?tags=docs,_vnbuild)** Read the documentation and articles to get started.

(Fun fact: This project publishes itself!)

## Basic Commands
- **update**: Updates the module and all projects within the module.  
- **build**: Builds the module and all projects within the module.  
- **publish**: Publishes the module and all projects within the module.  
- **clean**: Cleans up the module and all projects within the module.  
- **test**: Runs tests on the module and all projects within the module.  

**Always use the --help flag to get more information about a command** it will be more detailed than the information provided here.  

## Terminology
- **Module**: A self-contained git repository that has all of the necessary files to build a project or multiple projects as a single unit.  
- **Project**: A single build target within a module. A module can have multiple projects.  

### Repo update

### Building

### Publishing

#### GPG Signing
If the `--sign` flag is set during a publish command, the files produced by a single project will individually be signed, and the signature files located in the same output directory.

### Cleaning

## Taskfile.dev
vnbuild uses [Taskfile.dev](https://taskfile.dev) (installed on your machine) to actually execute the build steps within a module.

### Taskfiles
In the top level of your module, you must include a file named `Module.Taskfile.yaml`. This taskfile will be responsible for running tasks at a module level. It has the same functions any project-level taskfile does, but gets called first, and it's error codes will be observed. This file will also be responsible for updating the module's repository via a named task `update`.  

You may **optionally** have one or more `Taskfile.yaml` file(s) that will be called at a project level for every discovered project within the module. This file will be responsible for running tasks at a project level.  

#### How Task is used
For example, when you run `vnbuild build`:
1. vnbuild will look for a `Module.Taskfile.yaml` file in the root of the module.
2. Task will be executed (with -t) to run the Module.Taskfile.yaml file's build command within the module's root directory. This command is rquired, and its return code will be observed.
3. vnbuild will then execute Task process in the directory of each project found in the module.
4. Task searches up the directory tree for a `Taskfile.yaml` file (similar to git) and executes the task named 'build' if a taskfile is found. The results of this command are observed.

The same process is followed for `vnbuild publish` and `vnbuild clean` commands.

Most projects (of the same programming language) within a "monorepo" have similar build/publish steps, so I often have a single Taskfile.yaml in the root of the module with "generic" steps, if any project needs to be treated differently, I will add a modified Taskfile.yaml to that project's directory. In the case of C# modules, building with solution files can be mutch faster than building each project manually, so in that case, your Module.Taskfile.yaml should handle that, same with a large CMake project as well.

### Named tasks
vnbuild will execute named tasks within the Taskfile. The following tasks are required:

- **update**: Task the runs a repository sync operation. Only called during an `update` command. (Only available within Module.Taskfile.yaml)
- **build**: The task that actually builds the project. Only called during a `build` command.
- **postbuild_success**: Called after a successful build task finished. Only called during a `build` command.
- **postbuild_failure**: Called after a failed build task finished. Only called during a `build` command.
- **test**: A task that runs tests on the project. Only called during a `test` command. (all exit codes are observed, nonzero exit codes are considered a failure)
- **publish**: A task the runs publish operations such as copying files to a deployment directory or adding to a directory. Only called during a `publish` command.
- **clean**: A task that cleans up any temporary files or directories created during the build process. Only called during a `clean` command.

### Variables

#### Global
BUILD_DIR - the build wide .build output directory  
SCRATCH_DIR - the process wide shared scratch directory  
UNIX_MS - the unix (ms) timestamp at the start of the build process  
DATE - full normalized date and time string  
HEAD_SHA - the sha1 of the current head pointer  
BRANCH_NAME - the name of the branch currently pointed to by HEAD  

#### Module
MODULE_NAME - the name of the module solution  
OUTPUT_DIR - the module's output directory  
MODULE_DIR - the root dir of the module  
SOLUTION_FILE_NAME - The name of the solution file (including the extension)  
ARCHIVE_FILE_NAME - The git archive target file name  
FULL_ARCHIVE_FILE_NAME - The full file path to the desired git archive  
ARCHIVE_FILE_FORMAT - git archive format type  
BUILD_VERSION - Calculated module semver  

#### Project
PROJECT_NAME - the name of the project  
PROJECT_DIR - the root dir of the project  
IS_PROJECT - 'True' or undefined if the call is from a project scope  
SAFE_PROJ_NAME - The filesystem safe project name (removes any illegal filesystem characters and replaces them with hyphens)

#### Available from .x_proj/package.json files
PROJ_VERSION - the version string  
PROJ_DESCRIPTION - the description string  
PROJ_AUTHOR - the author string  
PROJ_COPYRIGHT - the copyright text from the project file  
PROJ_COMPANY - The company name  
RPOJ_URL - the project repository url  
BINARY_DIR - relative directory of the binary output (set by package)  

## License  
The software in this repository is licensed under the GNU GPL version 2.0 (or any later version). See the LICENSE files for more information. 