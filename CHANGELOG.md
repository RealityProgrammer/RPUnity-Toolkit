# Changelog
All notable changes to this project will be documented in this file.  

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)

## [1.4.0] - 2020-10-16
### Added
- Serializable HashSet with fancy UI (with BitHelper class that internally used by HashSet)
- MiscellaneousUtilities.cs (keep some extension methods related to C# standard library classes and more)

### Changed
- Modified code of Serializable Dictionary Window, now cut off useless Repaint draw and Reflection calls, thus increase performance
- Change location of built-in editor resources

## [1.3.0] - 2020-10-12
### Added
- State Machine class
- Collider2D Physics Checker (with custom Editor)
- Scene Loader

### Fixed
- Incorrect serialization layout of Serializable Dictionary, caused by candidates

## [1.2.5] - 2020-10-08
### Fixed
- ShaderPropertyModifier problem which doesn't check correct attribute

### Changed
- Increase performance of Serializable Dictionary Window

## [1.2.4] - 2020-10-08
### Fixed
- Empty .meta files which trigger Unity warning message on package installation

### Changed
- Serializable Dictionary Window UI
- Namespaces of editor classes

### Added
- 2 extra editor textures

## [1.2.3] - 2020-10-06
### Added
- Custom Editors for certain Shader Property modification class

### Fixed
- Serializable Dictionary performance loss due to caching bug

## [1.2.2] - 2020-10-03
### Fixed
- Editor Assembly Definition prevent users from build the game due to incorrect setup

### Added
- Some exceptions for Interpreters

## [1.2.1] - 2020-10-02
### Fixed
- SceneReference class contains Editor only code, fixed by adding UNITY_EDITOR preprocessor directive

## [1.2.0] - 2020-10-01
### Added
- Collection of thing about Shader Property, include runtime modifier (for both UI and Renderer)
- Storage of runtime created Materials.
- Addition algorithms for AlgorithmUtilities.

### Changed
- Increased performance of EquationCalculatorInterpreter

## [1.1.0] - 2020-09-30
### Added
- Equation Calculator.  
- Some new interpreter expressions with exceptions.

## [1.0.0] - 2021-09-28
### Changed
- Initial release.  

### Added
- Serializable Dictionary and an UI interface  