# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## 0.7.1
- Fix to `Convert To Relay Command` context action to support creating a method with correct signature if, one could not be derived

## 0.7.0
- Added Convert to Relay Command context action to convert to source generated RelayCommand
- Create Relay Context action to generate relay command when Source generation is not available

## 0.6.3
- Fixed issue with using Other and identifying candidates that are not direct subtypes of ObservableObject

## 0.6.2
- Fixed options naming
- Logic less restrictive as when to suggest partial properties (CommunityToolkit)
- Logic less restrictive regarding general source generated properties (CommunityToolkit)

## 0.6.1
- Fixed inconsistent naming of options from Mvvm Plugin -> Mvvm Helper

## 0.6.0
- Added options to allow configuration of ObservableObject to use when making a class observable
- Enhancements to Make Observable context action selection logic

## 0.5.0
- Added Go to view context action
- Added Go to view model context action
- Added support for generating and navigating view in Maui and WinUI solutions

## 0.4.1
- Fixed critical bug with macro

## 0.4.0
- Added Convert to observable property context action that can be applied to field decorated with the `ObservableProperty`

## 0.3.1
- Fixed bug with NotifyPropertyChangedFor and NotifyCanExecuteFor not working with fields

## 0.3.0
- Added context action for making method a relay command
- Added context action for NotifyPropertyChangedFor
- Added context action for NotifyCanExecuteFor

## 0.2.0
- Added additional community toolkit property generation actions
- Added postfix to generate community toolkit property
- Added context to help move towards partial properties in community toolkit

## 0.1.0
- Support added for CommunityToolkit Context Action related to class and properties

## 0.0.1.1
- Initial version



