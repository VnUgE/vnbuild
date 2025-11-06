
# vnbuild
*Automatically builds and delivers packages from git-based configurable pipelines with proper indexing for web based delivery*

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
- **build**: Builds all modules and all projects within the respective modules.  
- **build-project**: Builds a specified project within a specified module.  
- **clean**: Cleans up all modules and all projects within respective modules.  
- **display**: Displays all discovered modules within current and child directories.  
- **init**: Initializes a new vnbuild configuration in the current directory.  
- **list-projects**: Lists all discovered projects within a specified module.  
- **publish**: Publishes all modules and all projects within the respective modules.  
- **test**: Runs tests on all modules and all projects within respective modules.  
- **update**: Updates the module and all projects within the respective module.  

**Always use the --help flag to get more information about a command** it will be more detailed than the information provided here.  

## Terminology

- **Module**: A self-contained git repository or major subdirectory that contains all the files needed to build one or more projects. Each module has its own configuration and may contain multiple projects.
- **Project**: An individual build target within a module. Projects can be .NET solutions, Node.js packages, or other supported types. Each project can have its own Taskfile and configuration.
- **Taskfile**: A YAML file (`Module.Taskfile.yaml` for modules, `Taskfile.yaml` for projects) that defines build, test, publish, and other automation steps. Taskfiles are discovered and executed according to scope (project, module, global).
- **Pipeline**: The sequence of steps vnbuild executes to build, test, publish, and clean your codebase. The pipeline is modular and can be customized at the module or project level.

### vnbuild Pipeline Overview

A typical vnbuild pipeline consists of the following stages, which are manually run by the user as needed. vnbuild does not enforce a strict order; you choose which stages to run and when.

- **Module Change Tracking**: vnbuild tracks changes at the module level. If any code within a module is updated, the entire module is rebuilt. Dependencies are checked at the project level, but rebuild marking happens at the module level. Modules that do not depend on any projects within an outdated module are not rebuilt.

1. **Initialization (`init`)**  
   Sets up a new vnbuild configuration in the current directory, creating necessary Taskfiles and configuration files. This step is optional—vnbuild provides safe defaults and can operate without explicit initialization.

2. **Update (`update`)**  
   Synchronizes the module’s repository, typically by pulling the latest changes from git or other sources. This step is deprecated and only needed in certain build scenarios; most users will not need to run it in the typical workflow.

3. **Build (`build`, `build-project`)**  
   Compiles all modules and projects, running the appropriate Taskfiles. Supports building all projects or targeting a specific project within a module. Generally, build and test are mutually exclusive—users typically run one or the other, not both together.

4. **Test (`test`)**  
   Runs tests defined in Taskfiles for each project and module. Exit codes are observed; failures are reported. As noted above, test and build are usually run separately.

5. **Publish (`publish`)**  
   Deploys build artifacts to output directories, web servers, or other destinations. Supports GPG signing if the `--sign` flag is used.

6. **Clean (`clean`)**  
   Removes temporary files, build artifacts, and other generated content to keep the workspace tidy.

7. **Display & List (`display`, `list-projects`)**  
   Shows discovered modules and projects, helping users understand the structure of their codebase.

### Options Within Each Stage

- Each stage can be customized via Taskfiles at the module or project level.
- Variables are automatically scoped and passed to Taskfiles, allowing for flexible and context-aware automation.
- You can override default behavior by providing custom Taskfiles or modifying existing ones.
- vnbuild observes exit codes and reports errors, making it easy to integrate with CI/CD systems or local automation.

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

## Versioning and GitVersion

vnbuild uses [GitVersion](https://gitversion.net/) to automatically calculate semantic version numbers for your modules and projects based on your repository’s git history and branching strategy. This ensures consistent, traceable, and automated versioning for all build artifacts.

**How it works:**
- When you run vnbuild commands, GitVersion is invoked to determine the current semantic version (SemVer) for the module or project.
- The calculated version is injected into build variables and used for packaging, publishing, and metadata.
- Version variables such as `BUILD_VERSION`, `SEMVER`, and `ASSEMBLY_SEMVER` are set automatically and available in your Taskfiles.

**Requirements:**
- You must have GitVersion installed globally on your system.  
  - See [GitVersion installation docs](https://gitversion.net/docs) for instructions.
  - On most systems, you can install it via `dotnet tool install --global GitVersion.Tool` or download a binary from the official site.
- vnbuild expects your repository to follow a branching model compatible with GitVersion (e.g., main/master, develop, feature/*, release/*, etc.).

**Best Practices:**
- Commit and tag releases in git to ensure accurate versioning.
- Use semantic versioning for clarity and compatibility.
- If you encounter versioning issues, check your repository’s branch structure and ensure GitVersion is up to date.


## Scopes and Project Layout

vnbuild organizes your codebase into **modules** and **projects**, each with its own scope for variables and Taskfile execution.

- **Global Scope**: Variables and tasks available everywhere in the build process.
- **Module Scope**: Each module (usually a repository or a major subdirectory) should have its own `Module.Taskfile.yaml` and configuration. Module-level variables are set automatically and are available to all projects within the module.
- **Project Scope**: Individual projects inside a module can have their own Taskfile and variables. Project-specific variables are set when running tasks for that project.

When you run a vnbuild command, it determines the scope and passes the appropriate variables to your Taskfile. This allows you to define generic build steps at the module level, while customizing behavior for specific projects as needed. For example, you might have a shared Taskfile for all C# projects in a module, but override it for a particular project with unique requirements.

Taskfiles are discovered and executed in the following order:
1. Project Taskfile (if present)
2. Module Taskfile (fallback)
3. Global defaults

This layered approach ensures flexibility and consistency across large codebases and monorepos.

## Taskfile Variables

Below is a comprehensive list of variables available to Taskfile tasks in vnbuild. These are automatically set and passed to Taskfile commands at global, module, and project scopes.

### Global Variables

- **BUILD_DIR**: The build-wide output directory for all modules.
- **SCRATCH_DIR**: The process-wide shared scratch directory.
- **OUTPUT_DIR**: The global output directory for build artifacts.
- **WORKING_DIR**: The root working directory for the build process.
- **UNIX_MS**: The Unix timestamp (milliseconds) at the start of the build process.
- **DATE**: The normalized date and time string at build start.

`SCRATCH_DIR` is scoped to a module or a project, meaning each module or project can have its own scratch directory within the global scratch directory. This allows for isolated temporary files during builds.

`OUTPUT_DIR` is scoped to a module or a project, meaning each module or project can have its own output directory within the global output directory. This allows for organized build artifacts.

### Git Variables

- **HEAD_SHA**: The SHA1 of the current HEAD commit.
- **BRANCH_NAME**: The name of the branch currently pointed to by HEAD.
- **SAFE_BRANCH_NAME**: The branch name, safe for use in file paths.

### Module Variables

- **MODULE_NAME**: The name of the module.
- **MODULE_DIR**: The root directory of the module.
- **OUTPUT_DIR**: The module's output directory.
- **SCRATCH_DIR**: The module's scratch directory.
- **BINARY_DIR**: The module's binary output directory.
- **MODULE_TASK_FILE_NAME**: The name of the module-level Taskfile (default: `Module.Taskfile.yaml`).
- **ARCHIVE_FILE_FORMAT**: The format used for source archives (e.g., `tgz`).
- **SOURCE_ARCHIVE_NAME**: The name of the source archive file (default: `archive.tgz`).

### Version Variables

- **BUILD_VERSION / VERSION**: Calculated module semantic version (SemVer).
- **SEMVER**: The module's full semantic version string (e.g., `1.2.3-alpha.1+build.1234`).
- **ASSEMBLY_SEMVER**: The module's semantic version in assembly format (e.g., `1.2.3.4`).
- **VERSION_MAJOR**: The module's major version number.
- **VERSION_MINOR**: The module's minor version number.
- **VERSION_PATCH**: The module's patch version number.
- **VERSION_BUILD**: The module's build version label (pre-release label).
- **VERSION_CI_NUMBER**: The module's CI build number (zero-padded).

### Project Variables (only available in project scope)

- **PROJECT_NAME**: The name of the project.
- **SAFE_PROJ_NAME**: The filesystem-safe project name (illegal characters replaced).
- **PROJECT_DIR**: The root directory of the project.
- **PROJECT_FILE**: The full path to the project file.
- **IS_PROJECT**: `'True'` if the scope is a project.
- **BINARY_DIR**: The binary output directory for the project.
- **SCRATCH_DIR**: The scratch directory for the project.

### Project Metadata (from project files) (only available in project scope)

- **PROJ_VERSION**: The project version string.
- **PROJ_DESCRIPTION**: The project description.
- **PROJ_AUTHOR**: The project author.
- **PROJ_COPYRIGHT**: The copyright text from the project file.
- **PROJ_COMPANY**: The company name.
- **PROJ_URL**: The project repository URL.

### .NET Project Variables (only available in .NET project scope)

- **TARGET_FRAMEWORK**: The target framework for .NET projects.
- **PROJ_ASM_NAME**: The assembly name for .NET projects.

---

All variables are available to Taskfile tasks and can be referenced using the `{{ .VARIABLE_NAME }}` syntax in your Taskfile.yaml files.

## License  
The software in this repository is licensed under the GNU GPL version 2.0 (or any later version). See the LICENSE files for more information.