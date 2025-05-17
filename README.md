> ⚠️ This project is in active early development. The API may change, and there may be bugs. I'm very interested in feedback, so please open issues 
if you find any problems or have suggestions for improvements. The goal is to make this library as useful as possible for everyone.

# Mappit

Mappit is a library that allows simple mapping between two types. Instead of using reflection at runtime, code generation is used to define the mapping code.

The library errs on the side of correctness, so if types can't be fully mapped you'll get *compilation errors*, not errors at runtime.

So the benefits of Mappit are:

* Compile-time validation of mappings - you can see the generated source code, so it's easy to reason about what's going on.
* No runtime reflection
* No runtime performance overhead - mappings are pretty much what you'd write by hand

## Supported mapping features

### Properties

* **Implicit property mappings** (properties with matching names and compatible types)
* **Custom property mappings** - mapping from one property to another property with a different name, including support for custom value transformations.
* **Implicit nullable mappings** - automatic mapping between a type with it's `Nullable<T>` counterpart. The reverse is supported too, but will be configured to throw 
  `ArgumentNullException` at runtime is the source value is null.

### Enums

* **Implicit enum mappings** where all the enum names match
* **Custom enum value mappings** - mapping from one enum member to another

### Collections and Dictionaries

* **Implicit collection mapping** - where a mapped type has a property that's a collection or dictionary, Mappit will create an implicit mapping for the collection elements or dictionary keys/values.
* **Explicit collection mapping** - registering a collection mapping in your mapper class, e.g. `IList<FooRepresentation> Map(IEnumerable<Foo> source)` will automatically create a mapping for the collection elements.
  The same applies for both keys and values of dictionary types.
* **Copying collections by reference** - by default, collections and dictionaries are copied by reference when no element mapping is required. You can opt into deep copying at 
  the class or method level.

### Type construction

* **Constructor initialization**, including constructors that only cover some of the properties. Any remaining properties will be initialized via their setters.
* **Missing properties on the target type** - by default you'll get compile-time errors, but can opt in to ignore them.
* Support for both structs and classes, including records.

## Getting started

```
nuget install Mappit
```

``` csharp
// This is the mapper class - the implementation is generated at compile time
[Mappit]
public partial class DemoMapper
{
    /// <summary>
    /// Maps a Foo object to FooRepresentation
    /// </summary>
    /// <param name="source">The source Foo object</param>
    /// <returns>A mapped FooRepresentation</returns>
    public partial IList<FooRepresentation> Map(IEnumerable<Foo> source);
}

public static class Program
{
    public static void Main()
    {
        // Create a mapper instance
        DemoMapper mapper = new DemoMapper();

        // Create source objects
        Foo[] foos = [
            new Foo(1, "Test Object", DateTime.Now, true),
            new Foo(2, "Another Object", DateTime.Now.AddDays(-1), false)
        ];

        // Map to target type using the strongly-typed method
        IList<FooRepresentation> fooRepresentations = mapper.Map(foos);

        foreach (var fooRepresentation in fooRepresentations)
        {
            Console.WriteLine($"Mapped object: Id={fooRepresentation.Id}, Name={fooRepresentation.Name}");
        }
    }
}

public record Foo(int Id, string Name, DateTime CreatedDate, bool IsActive);

// Notice the mix of properties and constructor parameters
public class FooRepresentation(int id)
{
    public int Id { get; } = id;
    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }
}
```

The generated code looks like this:

```csharp
public partial class DemoMapper : IDemoMapper
{

    // Implementation of mapping from Foo to FooRepresentation
    public global::Mappit.Examples.FooRepresentation Map(global::Mappit.Examples.Foo source)
    {
        if (source is null)
        {
            return default;
        }

        return new global::Mappit.Examples.FooRepresentation(
            source.Id)
        {
            Name = source.Name,
            CreatedDate = source.CreatedDate,
            IsActive = source.IsActive,
        };
    }

    // Implementation of mapping from IEnumerable<Foo> to IList<FooRepresentation>
    public partial global::System.Collections.Generic.IList<global::Mappit.Examples.FooRepresentation> Map(global::System.Collections.Generic.IEnumerable<global::Mappit.Examples.Foo> source)
    {
        if (source is null)
        {
            return default;
        }

        return new global::System.Collections.Generic.List<global::Mappit.Examples.FooRepresentation>(source.Select(Map));
    }
}
```

Some people like interfaces for everything, so every generated mapper also implements its own interface - the above example has a generated interface `IDempMapper`.

If you like, you can have multiple mapper classes with different names. They will each end up with their own interface, and you can use them independently.

### Custom Property Mapping

If you need to map properties with different names, you can use the `MapProperty` attribute:

```csharp
[Mappit]
public partial class Mapper
{
    [MapProperty(nameof(Foo.SourceProp), nameof(FooRepresentation.TargetProp))]
    public partial FooRepresentation Map(Foo source);
}
```

This will map the `SourceProp` property of `Foo` to the `TargetProp` property of `FooRepresentation`. 

The property names are validated at compile time, so you'll get a compilation error if they don't exist or have incompatible types.

#### Custom Value Conversion Methods

You can specify a custom conversion method for transforming property values during mapping:

```csharp
[Mappit]
public partial class Mapper
{
    // If the target name is not specified, it is assumed to be the same as the source name.
    // (You'll get a validation error if it's not!)
    [MapProperty(nameof(Foo.Name), ValueConversionMethod = nameof(CleanupName))]
    public partial FooRepresentation Map(Foo source);
    
    // Custom conversion method - must have matching parameter and return types
    private string CleanupName(string name) => name?.Trim();
}
```

The conversion method must:

1. Have a return type matching the target property type
2. Take a single parameter matching the source property type
3. Be defined within the mapper class

This feature enables:

* Custom type conversions between incompatible types
* Data transformations like string cleanup, null handling, etc.
* Keeping most mappings automatic while customizing specific properties
* Reusing the same conversion logic across multiple mappings

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
    public partial FooRepresentation Map(Foo source);
    
    // Override at the field level to require all properties to be mapped
    [IgnoreMissingPropertiesOnTarget(false)]
    public partial BarRepresentation Map(Bar source);
}
```

## Copying collections by reference

By default, Mappit will copy collections and dictionaries by reference when no element mapping is required. This means that the 
collection in your mapped object will be the same instance as the source collection. This is useful for performance reasons, especially
when dealing with large collections, but it does mean that if you modify the collection in the mapped object, it will also modify 
the source collection.

If you want to force a deep copy of the collection, you can opt into this behavior at the class level:

```csharp
[Mappit(DeepCopyCollectionsAndDictionaries = true)]
```

Or at a method level:
```csharp
[Mappit]
public partial class Mapper
{
    [DeepCopyCollectionsAndDictionaries]
    public partial FooRepresentation Map(Foo source);
}
```

If you need to deep copy most of the time, but have a few cases where you want to copy by reference, you can set the class level option to `true` 
and override it at the method level to `false`.

```csharp
[Mappit(DeepCopyCollectionsAndDictionaries = true)]
public partial class Mapper
{
    // This will copy the collection by reference, even though the class level setting is true
    [DeepCopyCollectionsAndDictionaries(false)]
    public partial FooRepresentation Map(Foo source);
}
```

## Implicit collection property mapping

If a property is a collection, array or dictionary, Mappit will implicitly map the collection elements or dictionary values to the target type. For example:

```csharp
public record Team(List<Person> People);
public record Person(string Name, int Age);

public record TeamRepresentation(List<PersonRepresentation> People);
public record PersonRepresentation(string Name, int Age);


// In order to map a team to team representation, you only need to map
// the Team and Person mappings - Mappit will handle implicitly mapping the collection
[Mappit]
public partial class Mapper
{
    public partial TeamRepresentation Map(Team source);
    public partial PersonRepresentation Map(Person source);
}

var mapper = new Mapper();
var team = new Team(new List<Person> { new Person("Alice", 30), new Person("Bob", 25) });

var teamRepresentation = mapper.Map<TeamRepresentation>(team);

Console.WriteLine(teamRepresentation.People.Count); // Outputs: 2
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
    [MapEnumValue(nameof(SourceStatus.Active), nameof(TargetStatus.Enabled))]
    [MapEnumValue(nameof(SourceStatus.Inactive), nameof(TargetStatus.Disabled))]
    [MapEnumValue(nameof(SourceStatus.Pending), nameof(TargetStatus.AwaitingConfirmation))]
    public partial TargetStatus Map(SourceStatus source);
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

## How does the source generator work?

I want to document this properly some time, but for now, here's a quick overview:

1. The source generator scans for classes with the `Mappit` attribute.
1. The basic structure of the user defined mappings is generated into a `MapperClassInfo` object.
1. A validation step is performed to ensure that the mappings are valid, producing a `ValidatedMapperClassInfo` instance. 
   This step includes checking for things like:
   * Missing properties on the source or target types
   * Incompatible types between source and target properties
   * Defining implicit mappings for nullables, collections, dictionaries, etc.
1. The `ValidatedMapperClassInfo` instance is then used to generate the actual mapping code, which is written to a file in the user's project.

## Known limitations

* Classes containing properties with properties differing only by case are not supported.
* Recursive object graphs won't work and your code will likely hang forever. I'll get to this too!

## Todo

* Recursion handling
* More complex mappings, like:
  * Object flattening - e.g. map a complex object to a simple one
  * Object expansion - e.g. map a simple object to a complex one