# Publicizer

Allows access to normally private members of referenced assemblies.

## Usage

Add the package to your project and then add an item `PublicizeAssembly` for every you want to access. Optionally you can specify how far to open the assembly.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- Add the package reference -->
  <ItemGroup>
    <PackageReference Include="RedworkDE.Publicizer" Version="0.1.0" />
  </ItemGroup>

  <!-- Optionally specify defaults for how to process assemblies, when not specified for the individual assemblies -->
  <PropertyGroup>
    <!-- Make only internal members accessible -->
    <DefaultPublicizeInternal>True</DefaultPublicizeInternal>
    
    <!-- Make all members accessible, this superceedes Internal when set to True -->
    <DefaultPublicizePrivate>False</DefaultPublicizePrivate>
    
    <!-- Make readonly fields writeable -->
    <DefaultPublicizeReadonly>False</DefaultPublicizeReadonly>
    
    <!-- The default backing field for events has the same name as the event, causing a name conflict when making private fields accessible -->
    <!-- If this property is false, this conflict is resolved by not making the backing field accessible -->
    <!-- Otherwise the event is removed, allowing access you to also invoke the event from the outside -->
    <DefaultPublicizeEventBackingField>True</DefaultPublicizeEventBackingField>
    
    <!-- Make all assemblies accessible, PublicizeAssembly can still be used to specify how assemblies are processed -->
    <PublicizeAll>False</PublicizeAll>
  </PropertyGroup>

  <!-- Specify how each assembly is processed -->
  <ItemGroup>
    <!-- Make AssemblyToAccess1 accessible with the default settings -->
    <PublicizeAssemblies Include="AssemblyToAccess1" />
    
    <!-- Make AssemblyToAccess2 accessible including private and readonly members, keeping the default for EventBackingField -->
    <PublicizeAssemblies Include="AssemblyToAccess2" Private="True" Readonly="True" />
    
    <!-- Process AssemblyToAccess3 but don't make any members accessible, may be useful together with PublicizeAll -->
    <PublicizeAssemblies Include="AssemblyToAccess3" Internal="False" Private="False" Readonly="False" EventBackingField="False" />

  </ItemGroup>
</Project>
```
