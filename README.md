[![Rider](https://img.shields.io/jetbrains/plugin/v/26397.svg?label=Rider&colorB=0A7BBB&style=for-the-badge&logo=rider)](https://plugins.jetbrains.com/plugin/26397)
[![ReSharper](https://img.shields.io/jetbrains/plugin/v/26396.svg?label=ReSharper&colorB=0A7BBB&style=for-the-badge&logo=resharper)](https://plugins.jetbrains.com/plugin/26396)

This a plugin to help with some MVVM tasks. This is a "learn plugin" development project. So I can't guarantee that there won't be annoying bugs in the start of this. I do though do everything to keep them at a minimum. :) 

Current overview:

## Context Actions

### Create Viewmodel

From a view (for instance a Window or Page etc.) a context action is available that will create a viewmodel and set it in the attributes of the view. For instance in an Avalonia project in a view called Somewindow.xaml a viewmodel names SomeWindowViewModel would be created and referenced through the DataType attribute

![CreateModelV2](https://github.com/user-attachments/assets/093f9ff9-784b-45b4-96a6-db982ddea21b)

### Community Toolkit

If the CommunityToolkit.Mvvm package is installed a range of context actions are available to assist in building the view model (focused on the Source Generator parts)

#### Context actions

##### Make Class Observable

When inside a class you can active the `Make Class Observable` context action to make the class inherit from ObservableObject and make the class partial. 

Unlike the actions below it is possible to call this context action if `CommunityToolkit.Mvvm` is not installed. It will only be available for classes that ends with viewModel. And you will need to install the nuget plugin manually afterwards. (My goal is to in a later version make the installation of the nuget package be done by the context action)

![image](https://github.com/user-attachments/assets/b7d4a1b7-de1d-451a-9760-2b9118483a65)

##### Make Field Observable

When this context action is applied to a field, it will be decorated with the ObservableProperty attribute and if necesarry also ensure that the containing class is Observable and partial

##### Make Property Observable

This will take a property and make it observable. There are two scenarios: 

###### New Partial Property support
If the version of CommunityToolkit.Mvvm is 8.4.0 or later and the LangVersion is set to Preview the property will be made partial and decorated with the ObservableProperty. 

###### Property from field
Otherwise the property will be converted to a field and decorated with the ObservableProperty attribute.

###### Class and Property Context actions video#

https://github.com/user-attachments/assets/ae1f35b9-e3d1-4dc3-a15e-cb3e2fd6712f

###### Create observable property

This can called from inside a class an will generate an observable property. Like above it will be either a field or a partial properties

###### Enable partial properties

If version 8.4 or larger of CommunityToolkit is installed it will try to ensure that you can use partial properties instead of field. 
**NOTE** It is also required that the dotnet version is at least 9 but that will not be updated by this operation. However a error squiggly will be displayed on all classes that implement the ObservableObject with suggestion that you should upgrade to 9

###### Create Relay

This can be called on void method or a method returning Task that zero or one parameter. It will decorate the method with the `RelayCommand` attribute and create a CanExecute method to handle CanExecute scenarios.

![Relay](https://github.com/user-attachments/assets/668adb9b-1d21-4f34-9114-e009f0b6ee03)

###### Add NotifyCanExecuteChangedFor 

This can be called on any observable property if one or more Relay commands are available

![NotifyCanExecuteChangedFor](https://github.com/user-attachments/assets/74c91371-09ee-4211-a142-4071fdf30569)

###### Add NotifyPropertyChangedFor

This can be called on any observable property and will generate a context action for all valid properties

![NotifyPropertyChangedFor](https://github.com/user-attachments/assets/edc0a2e8-f26c-4156-8f00-1a996e6c27fc)

###### Convert to partial property

This will take a field definition decorated with `ObservableProperty` like for instance

```csharp
[ObservableProperty]
private string _someField;
```

And convert it to the newer partial property format if applicable (CommunityTookit.Mvvm version larger than 8.4 and preview set). 

NB! This context action is scoped, so it can also be applied to the file, project or solution.

##### Postfix templates

###### Obsprop

Inside a statement you call the obsprop to generate an observable property. Like Make Property Observable based on project settings it will create a field or a partial property. See below for an example (that also uses the Enable Partial properties context action)

![Obsprop](https://github.com/user-attachments/assets/3047af10-e783-49fd-ae32-de5578c07b61)


