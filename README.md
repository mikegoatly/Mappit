> ⚠️ This project is in **very** early development and it should be considered a proof of concept implementation for now. The API may change, and there may be bugs. Use at your own risk.

# Mappit

Mappit is a library that allows simple mapping between two types. Instead of using reflection at runtime, code generation is used to define the mapping code.

The library errs on the side of correctness, so if types can't be fully mapped you'll get *compilation errors*, not errors at runtime.

So the benefits of Mappit are:

* Compile-time validation of mappings
* No runtime reflection
* No runtime performance overhead - mappings are pretty much what you'd write by hand

## Getting started

``` csharp
[Mappit]
public partial class Mapper
{
    // Every partial mapping method is automatically implemented by the source generator
    partial FooRepresentation Map(Foo source);
    partial BarRepresentation Map(Bar source);
}
```

You can then use:

``` csharp
var mapped = mapper.Map<FooRepresentation>(myfoo);
```

Some people like interfaces for everything, so every generated mapper also implements its own interface - the above example would have an interface `IMapper`.

If you like, you can have multiple mapper classes with different names. They will each end up with their own interface, and you can use them independently.

## Supported mappings

* Implicit property mappings (properties with matching names and compatible types)
* Implicit enum mappings where all the enum names match
* Custom property mappings 
* Custom enum value mappings
* Constructor initialization, including constructors that only cover some of the properties. Any remaining properties will be initialized via their setters.
* Control over missing properties on the target type - by default you'll get compile-time errors, but can opt in to ignore them.

### Custom Property Mapping

If you need to map properties with different names, you can use the `MapMember` attribute:

```csharp
[Mappit]
public partial class Mapper
{
    [MapMember(nameof(Foo.SourceProp), nameof(FooRepresentation.TargetProp))]
    partial FooRepresentation Map(Foo source);
}
```

This will map the `SourceProp` property of `Foo` to the `TargetProp` property of `FooRepresentation`. 

The property names are validated at compile time, so you'll get a compilation error if they don't exist or have incompatible types.

### Handling Missing Properties

By default, at the class level, Mappit will generate an error when source properties don't have matching target properties. You can control this behavior with the `IgnoreMissingPropertiesOnTarget` option at either
the class or mapping method:

```csharp
// Class level setting - default is false, but you can set it to true here
[Mappit(IgnoreMissingPropertiesOnTarget = true)]
public partial class Mapper
{
    // This mapping will ignore properties that exist in the source but not in the target
    // because of the class-level setting
    partial FooRepresentation Map(Foo source);
    
    // Override at the field level to require all properties to be mapped
    [IgnoreMissingPropertiesOnTarget(false)]
    partial BarRepresentation Map(Bar source);
}
```

## Enum Mapping

Enums with the same name and compatible values are mapped automatically. For enums with different names or values, you need to use custom enum mapping.

### Custom Enum Mapping

For enums with different values, you can define custom mappings using the `MapMember` attribute:

```csharp
public enum SourceStatus { 
    Active = 0, 
    Inactive = 1,
    Pending = 2
}

public enum TargetStatus { 
    Enabled = 0, 
    Disabled = 1,
    AwaitingConfirmation = 2
}

[Mappit]
public partial class Mapper
{
    [MapMember(nameof(SourceStatus.Active), nameof(TargetStatus.Enabled))]
    [MapMember(nameof(SourceStatus.Inactive), nameof(TargetStatus.Disabled))]
    [MapMember(nameof(SourceStatus.Pending), nameof(TargetStatus.AwaitingConfirmation))]
    partial TargetStatus Map(SourceStatus source);
}
```

If you get any of these names wrong, you'll get a compile-time error.

## Custom type mappings

If you run into limitations for a certain type, you can provide a concrete implementation for a mapping method that the 
source generator will use as-is:

```csharp
[Mappit]
public partial class CustomMappingTestMapper
{
    public WeirdModelMapped Map(WeirdModel source)
    {
        return new WeirdModelMapped { Name = new string([..source.Name.Reverse()]) };
    }
}
```

## Known limitations

* Classes containing properties with properties differing only by case are not supported.
* No support for collections or dictionaries (IEnumerable, IList, etc.) (yet!)
* Recursive object graphs won't work and your code will hang forever. I'll get to this eventually!

## Todo

* Opt in to reverse mappings
* Support for collections and dictionaries (IEnumerable, IList, etc.)
* Recursion handling