> ⚠️ This project is in **very** early development and it should be considered a proof of concept implementation for now. The API may change, and there may be bugs. Use at your own risk.

# Mappit

Mappit is a library that allows simple mapping between two types. Instead of using reflection at runtime, code generation is used to define the mapping code.

The library errs on the side of correctness, so if types can't be fully mapped you'll get *compilation errors*, not errors at runtime.

## Getting started

``` csharp
public partial class Mapper : MappingBase
{
    private TypeMapping<Foo, FooRepresentation> foo;
}
```

You can then use:

``` csharp
var mapped = mapper.Map<FooRepresentation>(myfoo);
```

`MappingBase` also implements the interface `IMapper` so you can configure that in your DI container.

## Supported mappings

* Simple property mappings (properties with matching names and compatible types)
* Enum mappings with compile-time validation
* Custom property mappings 
* Custom enum value mappings
* Constructor initialization, including constructors that only cover some of the properties. Any remaining properties will be initialized via their setters.

## Property Mapping

By default, Mappit maps properties with the same name and type. Properties that don't have matching names or compatible types in the destination type are ignored.

### Custom Property Mapping

If you need to map properties with different names, you can use the `MapProperty` attribute:

```csharp
public partial class Mapper : MappingBase
{
    [MapProperty(nameof(Foo.SourceProp), nameof(FooRepresentation.TargetProp))]
    private TypeMapping<Foo, FooRepresentation> foo;
}
```

This will map the `SourceProp` property of `Foo` to the `TargetProp` property of `FooRepresentation`. 

The property names are validated at compile time, so you'll get a compilation error if they don't exist or have incompatible types. The error will point to the exact location of the problematic property name in your code.

## Enum Mapping

Enums with the same name and compatible values are mapped automatically. For enums with different names or values, you need to use custom enum mapping.

### Custom Enum Mapping

For enums with different values, you can define custom mappings using the `MapEnumValue` attribute:

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

public partial class Mapper : MappingBase
{
    [MapEnumValue(nameof(SourceStatus.Active), nameof(TargetStatus.Enabled))]
    [MapEnumValue(nameof(SourceStatus.Inactive), nameof(TargetStatus.Disabled))]
    [MapEnumValue(nameof(SourceStatus.Pending), nameof(TargetStatus.AwaitingConfirmation))]
    private TypeMapping<SourceModel, TargetModel> sourceToTarget;
}
```

All enum values are validated at compile time, ensuring correctness:
- If a source or target enum value doesn't exist, you'll get a compilation error
- The error will point to the exact attribute argument that contains the invalid value
- Mappings without explicit custom values use the direct numeric cast for the underlying enum values

## Custom type mappings

If you run into limitations for a certain type, you can always define your own `TypeMapping`:

```csharp
public partial class CustomMappingTestMapper : MapperBase
{
    protected override void InitializeCustomMappings()
    {
        RegisterMapping(new WeirdMapping());
    }

    private class WeirdMapping : TypeMapping<WeirdModel, WeirdOtherModel>
    {
        public override WeirdModel Map(WeirdModel source)
        {
            return new WeirdModel { Name = new string([..source.Name.Reverse()]) };
        }
    }
}
```

## Compile-time Safety

Mappit focuses on correctness through compile-time validation:

- All property names and types are validated
- All enum values are checked to ensure they exist
- Errors are reported with precise source location information
- Compilation fails if any mapping would be invalid at runtime

## How Mappit Works

1. You define mappings as generic fields
2. The source generator analyzes your code at compile time
3. It generates mapping implementations based on your definitions
4. The generated code handles the actual property mappings

The source generator approach means:
- No reflection at runtime for better performance
- No reliance on convention over configuration
- Explicit mapping definitions with compile-time checking
- Clear error messages attached to the exact location of issues