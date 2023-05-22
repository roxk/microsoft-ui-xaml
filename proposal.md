# Proposal: Support using WinUI 3 without XAML markup 

  

## Summary 

Today, using WinUI 3 without XAML markup (i.e. without any `.xaml` files) requires workaround. While XAML markup is awesome, there are some scenarios where code-only usage is necessary, such as [react-native-windows](https://microsoft.github.io/react-native-windows/). This proposal aims to eliminate these workaround, and light-up code-only usage of WinUI 3. 

  

While this proposal would use WinUI 2 as examples, this proposal does _NOT_ propose any changes to WinUI 2 (System XAML). This proposal targets only WinUI 3. 

  

## Context and Motivation 

It is proven that WinUI is tightly coupled with XAML markup. In system XAML Islands, workarounds are required to instantiate [just a `ProgressRing` in `muxc`](https://github.com/microsoft/react-native-windows/pull/8331). In WinUI 3, [`App.xaml`](https://github.com/microsoft/react-native-windows/blob/3e7d58e99b162ffd987e44f821ba790dee714041/vnext/template/cs-app-WinAppSDK/MyApp/App.xaml#L4) is required even though react-native-windows always instantiates controls in code. Without such a `.xaml` file, the runtime would fail to instantiate e.g. [MUXC.NavigationViewPaneDisplayMode](https://github.com/microsoft/microsoft-ui-xaml/issues/5704). 

  

All of these workaround involves how a type is resolved by XAML runtime. For the sake of brevity this proposal would not dive into the details. The TLDR is that these workarounds are necessary because most of the heavy lifting of resolving a type is expected to be done by XAML Compiler (XC), on `.xaml` files. Not having `.xaml` files means no work is done by XC, and the result is a hard crash when instantiating certain types at runtime. 

  

It is unfortunate because WinUI classes themselves fundamentally have no dependency on `.xaml` files whatsoever. The missing link to make code-only usage of WinUI work is only the gluing code required for WinRT activation to work. The gluing code is absent in code-only scenarios because WinUI (rightfully) assumes `.xaml` files are always present, by design. This proposal advocates that WinUI 3 officially support code-only usage, allowing those gluing code to function without any `.xaml` files. 

  

It is expected XC, relevant APIs's implementation, and relevant tooling, would have to adapt accordingly. Depending on the requirement, it is possible that there would be no API (class or build system) changes. 

  

## Rationale 

* The use case of code-only consumption of WinUI are well-documented (e.g. React Native for windows) 

* Removing a huge amount of workarounds needed simply to make code-only WinUI 3 work improves the WinUI 3 developer experience and reputation 

* Most of the code to make it work is already there, it's just the assumption that `.xaml` files (and XC) is always present that is holding WinUI 3 back in supporting this use case 

* Making code-only usage work would allow other community efforts to thrive. e.g. Rust's `windows` crate could start to rely on the fact that WinUI types lookup is always handled by WinUI 3 itself _via code only_ 

  

## Scope 

| Capability | Priority | 

| :---------- | :------- | 

| This proposal will allow developers to use WinUI 3 without any `.xaml` files, in supported languages (i.e. C++ and C#) | Must | 

| This proposal will allow developers to choose "WinUI 3 without XAML" template from template list | Could | 

| This proposal will allow developers to use WinUI 3 code-only in build systems other than MSBuild*1 | Should | 

| This proposal will allow developers to use WinUI 2 without any `.xaml` files | Won't | 

| This proposal need to specifically cater to community effort such as rust's `windows` crate*2 | Won't | 

  

*Note 1: This bullet point is only here to re-assure that the WinUI 3 team only needs to support MSBuild. However, I'd expect the underlying technology to make it work (e.g. a command line option passed to XC, or a preprocessor definition, to not be coded in such a way that it explicitly depends on MSBuild). Therefore, other build systems should theoretically also work. 

*Note 2: This bullet point is only here to re-assure that the WinUI 3 team is not responsible for supporting any langauges other than C++ and C#, but I'd expect that a working implementation of this proposal would allow such community efforts to just work™️ since, if code-only usage works in C++, it should work in rust (everything is just COM after all). 

  

## Important Notes 

This proposal is intentionally vague on the exact API design because, in an ideal situation, there should be no API changes, at least for C++ and C# developers. Instantiating an `App` and inserting a few `TextBox` and `muxc:ProgressRing` should just work. However, I'm aware that implementation trade-off may have to be made when allowing code-only usage of WinUI 3, so an on-off toggle might be warranted. I prefer to put these discussions under Open Questions. 

  

## Open Questions 

- Is anything else missing to allow code-only usage of WinUI 3? That is, is "`.xaml` files are always used" the only assumption that is holding the use-case back? 

- Is there any practical performance/runtime trade off that has to be made when allowing code-only usage of WinUI 3? 

- Should code-only usage just work by default, or should it be gated behind a toggle? For example, a command line option, and thus a MSBuild project property? 

 

 