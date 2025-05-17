using Xunit;

namespace Mappit.Tests.MappingGenerationVerification
{
    public class CollectionMappingTests
    {
        [Fact]
        public void MappingCollections_ShouldCopyDataSuccessfully()
        {
            var source = new CollectionVarietySource
            {
                ConcreteList = ["Item1", "Item2", "Item3"],
                ConcreteArray = ["Item4", "Item5"],
                IListInterface = ["Item6", "Item7"],
                ICollectionInterface = ["Item8", "Item9"],
                IEnumerableInterface = ["Item10", "Item11"],
                IReadOnlyListInterface = ["Item12", "Item13"],
                IReadOnlyCollectionInterface = ["Item14", "Item15"],
                ISetInterface = new HashSet<string> { "Item16", "Item17" },
                IReadOnlySetInterface = new HashSet<string> { "Item18", "Item19" },
                ConcreteSet = ["Item20", "Item21"],
                EntityArray = [new CollectionEntity(1), new CollectionEntity(2)],
                CollectionEntities = [new CollectionEntity(3), new CollectionEntity(4)],
                AdditionalIListInterface = ["Item22", "Item23"],
                AdditionalIEnumerableInterface = ["Item24", "Item25"],
                AdditionalHashSet = ["Item26", "Item27"]
            };

            var expected = new CollectionVarietyTarget
            {
                ConcreteList = ["Item1", "Item2", "Item3"],
                ConcreteArray = ["Item4", "Item5"],
                IListInterface = ["Item6", "Item7"],
                ICollectionInterface = ["Item8", "Item9"],
                IEnumerableInterface = ["Item10", "Item11"],
                IReadOnlyListInterface = ["Item12", "Item13"],
                IReadOnlyCollectionInterface = ["Item14", "Item15"],
                ISetInterface = new HashSet<string> { "Item16", "Item17" },
                IReadOnlySetInterface = new HashSet<string> { "Item18", "Item19" },
                ConcreteSet = ["Item20", "Item21"],
                EntityArray = [new CollectionEntityMapped(1), new CollectionEntityMapped(2)],
                CollectionEntities = [new CollectionEntityMapped(3), new CollectionEntityMapped(4)],
                AdditionalIListInterface = ["Item22", "Item23"],
                AdditionalIEnumerableInterface = ["Item24", "Item25"],
                AdditionalHashSet = new HashSet<string> { "Item26", "Item27" }
            };

            var mapper = new CollectionMapper();
            var result = mapper.Map(source);

            Assert.NotNull(result);
            Assert.Equivalent(expected, result);
        }

        [Fact]
        public void MappingDictionaries_ShouldCopyDataSuccessfully()
        {
            var source = new DictionarySource
            {
                Dictionary = new Dictionary<string, string>
                {
                    { "Key1", "Value1" },
                    { "Key2", "Value2" }
                },
                EntityDictionary = new Dictionary<string, CollectionEntity>
                {
                    { "Key3", new CollectionEntity(1) },
                    { "Key4", new CollectionEntity(2) }
                },
                EntityArrayDictionary = new Dictionary<string, CollectionEntity[]>
                {
                    { "Key5", new[] { new CollectionEntity(3), new CollectionEntity(4) } }
                },
                IDictionaryInterface = new Dictionary<string, string>
                {
                    { "Key6", "Value3" },
                    { "Key7", "Value4" }
                },
                IReadOnlyDictionaryInterface = new Dictionary<string, string>
                {
                    { "Key8", "Value5" },
                    { "Key9", "Value6" }
                }
            };

            var expected = new DictionaryTarget
            {
                Dictionary = new Dictionary<string, string>
                {
                    { "Key1", "Value1" },
                    { "Key2", "Value2" }
                },
                EntityDictionary = new Dictionary<string, CollectionEntityMapped>
                {
                    { "Key3", new CollectionEntityMapped(1) },
                    { "Key4", new CollectionEntityMapped(2) }
                },
                EntityArrayDictionary = new Dictionary<string, CollectionEntityMapped[]>
                {
                    { "Key5", new[] { new CollectionEntityMapped(3), new CollectionEntityMapped(4) } }
                },
                IDictionaryInterface = new Dictionary<string, string>
                {
                    { "Key6", "Value3" },
                    { "Key7", "Value4" }
                },
                IReadOnlyDictionaryInterface = new Dictionary<string, string>
                {
                    { "Key8", "Value5" },
                    { "Key9", "Value6" }
                }
            };

            var mapper = new CollectionMapper();
            var result = mapper.Map(source);
            Assert.NotNull(result);
            Assert.Equivalent(expected, result);
        }

        [Fact]
        public void ExplicitlyMappedCollectionTypes_MapDataCorrectly()
        {
            var source = new[]
            {
                new CollectionEntity(1),
                new CollectionEntity(2)
            };

            var expected = new[]
            {
                new CollectionEntityMapped(1),
                new CollectionEntityMapped(2)
            };

            IExplicitCollectionMappingMapper mapper = new ExplicitCollectionMappingMapper();

            Assert.Equivalent(expected, mapper.Map(source));

            // And the enumerable version
            Assert.Equivalent(expected, mapper.Map((IEnumerable<CollectionEntity>)source));
        }

        [Fact]
        public void ExplicitlyMappedDictionaryTypesWithValueMapping_MapDataCorrectly()
        {
            var source = new Dictionary<int, CollectionEntity>
            {
                { 1, new CollectionEntity(1) },
                { 2, new CollectionEntity(2) }
            };

            var expected = new Dictionary<int, CollectionEntityMapped>
            {
                { 1, new CollectionEntityMapped(1) },
                { 2, new CollectionEntityMapped(2) }
            };

            IExplicitCollectionMappingMapper mapper = new ExplicitCollectionMappingMapper();

            Assert.Equivalent(expected, mapper.Map(source));
        }

        [Fact]
        public void ExplicitlyMappedDictionaryTypesWithKeyMapping_MapDataCorrectly()
        {
            var source = new Dictionary<CollectionEntity, int>
            {
                { new CollectionEntity(1), 1 },
                { new CollectionEntity(2), 2 }
            };

            var expected = new Dictionary<CollectionEntityMapped, int>
            {
                { new CollectionEntityMapped(1), 1 },
                { new CollectionEntityMapped(2), 2 }
            };

            IExplicitCollectionMappingMapper mapper = new ExplicitCollectionMappingMapper();
            Assert.Equivalent(expected, mapper.Map(source));
        }

        [Fact]
        public void CopyingCollectionsByRef_ReturnsSameInstance()
        {
            var mapper = new CopyCollectionByRefByDefaultMapper();
            SimplifiedCollections original = CreateSampleSimplifiedCollections();

            var mapped = mapper.Map(original);

            Assert.NotSame(original, mapped);
            Assert.Same(original.Concrete, mapped.Concrete);
            Assert.Same(original.ConcreteComplex, mapped.ConcreteComplex);
            Assert.Same(original.Interfaced, mapped.Interfaced);
        }

        [Fact]
        public void CopyingDictionariesByRef_ReturnsSameInstance()
        {
            var mapper = new CopyCollectionByRefByDefaultMapper();
            SimplifiedDictionaries original = CreateSampleSimplifiedDictionaries();

            var mapped = mapper.Map(original);

            Assert.NotSame(original, mapped);
            Assert.Same(original.Concrete, mapped.Concrete);
            Assert.Same(original.ConcreteComplexKey, mapped.ConcreteComplexKey);
            Assert.Same(original.ConcreteComplexValue, mapped.ConcreteComplexValue);
            Assert.Same(original.Interfaced, mapped.Interfaced);
        }

        [Fact]
        public void CopyingCollectionsWithDeepCopy_ReturnsDeepCopies()
        {
            var mapper = new CopyCollectionByRefByDefaultMapper();
            SimplifiedCollections original = CreateSampleSimplifiedCollections();

            var mapped = mapper.MapDeepCopy(original);

            Assert.NotSame(original, mapped);
            Assert.NotSame(original.Concrete, mapped.Concrete);
            Assert.NotSame(original.ConcreteComplex, mapped.ConcreteComplex);
            Assert.NotSame(original.Interfaced, mapped.Interfaced);
        }

        [Fact]
        public void CopyingDictionariesWithDeepCopy_ReturnsDeepCopies()
        {
            var mapper = new CopyCollectionByRefByDefaultMapper();
            SimplifiedDictionaries original = CreateSampleSimplifiedDictionaries();

            var mapped = mapper.MapDeepCopy(original);

            Assert.NotSame(original, mapped);
            Assert.NotSame(original.Concrete, mapped.Concrete);
            Assert.NotSame(original.ConcreteComplexKey, mapped.ConcreteComplexKey);
            Assert.NotSame(original.ConcreteComplexValue, mapped.ConcreteComplexValue);
            Assert.NotSame(original.Interfaced, mapped.Interfaced);
        }

        [Fact]
        public void DeepCopyByDefault_CopyingCollectionsByRef_ReturnsSameInstance()
        {
            var mapper = new DeepCopyCollectionByDefaultMapper();
            SimplifiedCollections original = CreateSampleSimplifiedCollections();

            var mapped = mapper.Map(original);

            Assert.NotSame(original, mapped);
            Assert.Same(original.Concrete, mapped.Concrete);
            Assert.Same(original.ConcreteComplex, mapped.ConcreteComplex);
            Assert.Same(original.Interfaced, mapped.Interfaced);
        }

        [Fact]
        public void DeepCopyByDefault_CopyingDictionariesByRef_ReturnsSameInstance()
        {
            var mapper = new DeepCopyCollectionByDefaultMapper();
            SimplifiedDictionaries original = CreateSampleSimplifiedDictionaries();

            var mapped = mapper.Map(original);

            Assert.NotSame(original, mapped);
            Assert.Same(original.Concrete, mapped.Concrete);
            Assert.Same(original.ConcreteComplexKey, mapped.ConcreteComplexKey);
            Assert.Same(original.ConcreteComplexValue, mapped.ConcreteComplexValue);
            Assert.Same(original.Interfaced, mapped.Interfaced);
        }

        [Fact]
        public void DeepCopyByDefault_CopyingCollectionsWithDeepCopy_ReturnsDeepCopies()
        {
            var mapper = new DeepCopyCollectionByDefaultMapper();
            SimplifiedCollections original = CreateSampleSimplifiedCollections();

            var mapped = mapper.MapDeepCopy(original);

            Assert.NotSame(original, mapped);
            Assert.NotSame(original.Concrete, mapped.Concrete);
            Assert.NotSame(original.ConcreteComplex, mapped.ConcreteComplex);
            Assert.NotSame(original.Interfaced, mapped.Interfaced);
        }

        [Fact]
        public void DeepCopyByDefault_CopyingDictionariesWithDeepCopy_ReturnsDeepCopies()
        {
            var mapper = new DeepCopyCollectionByDefaultMapper();
            SimplifiedDictionaries original = CreateSampleSimplifiedDictionaries();

            var mapped = mapper.MapDeepCopy(original);

            Assert.NotSame(original, mapped);
            Assert.NotSame(original.Concrete, mapped.Concrete);
            Assert.NotSame(original.ConcreteComplexKey, mapped.ConcreteComplexKey);
            Assert.NotSame(original.ConcreteComplexValue, mapped.ConcreteComplexValue);
            Assert.NotSame(original.Interfaced, mapped.Interfaced);
        }

        private static SimplifiedCollections CreateSampleSimplifiedCollections()
        {
            return new SimplifiedCollections
            {
                Concrete = ["a", "b"],
                Interfaced = ["c", "d"],
                ConcreteComplex = [new(1), new(2)]
            };
        }

        private static SimplifiedDictionaries CreateSampleSimplifiedDictionaries()
        {
            return new SimplifiedDictionaries
            {
                Concrete = new()
                {
                    ["a"] = 1,
                    ["b"] = 2,
                },
                Interfaced = new Dictionary<string, int>()
                {
                    ["c"] = 3,
                    ["d"] = 4,
                },
                ConcreteComplexKey = new()
                {
                    [new(1)] = 1,
                    [new(2)] = 2,
                },
                ConcreteComplexValue = new()
                {
                    [1] = new(1),
                    [2] = new(2),
                }
            };
        }
    }

    [Mappit]
    public partial class CopyCollectionByRefByDefaultMapper
    {
        public partial SimplifiedCollections Map(SimplifiedCollections source);
        public partial SimplifiedDictionaries Map(SimplifiedDictionaries source);

        [DeepCopyCollectionsAndDictionaries]
        public partial SimplifiedCollections MapDeepCopy(SimplifiedCollections source);
        [DeepCopyCollectionsAndDictionaries]
        public partial SimplifiedDictionaries MapDeepCopy(SimplifiedDictionaries source);
    }

    [Mappit(DeepCopyCollectionsAndDictionaries = true)]
    public partial class DeepCopyCollectionByDefaultMapper
    {
        [DeepCopyCollectionsAndDictionaries(false)]
        public partial SimplifiedCollections Map(SimplifiedCollections source);
        [DeepCopyCollectionsAndDictionaries(false)]
        public partial SimplifiedDictionaries Map(SimplifiedDictionaries source);
        public partial SimplifiedCollections MapDeepCopy(SimplifiedCollections source);
        public partial SimplifiedDictionaries MapDeepCopy(SimplifiedDictionaries source);
    }

    public partial class CopyCollectionDeepCopyByDefaultMapper
    {

    }

    [Mappit]
    public partial class ExplicitCollectionMappingMapper
    {
        public partial CollectionEntityMapped[] Map(CollectionEntity[] source);
        public partial IReadOnlyList<CollectionEntityMapped> Map(IEnumerable<CollectionEntity> source);
        public partial IReadOnlyDictionary<int, CollectionEntityMapped> Map(IDictionary<int, CollectionEntity> source);
        public partial IReadOnlyDictionary<CollectionEntityMapped, int> Map(IDictionary<CollectionEntity, int> source);
    }

    [Mappit]
    public partial class CollectionMapper
    {
        public partial DictionaryTarget Map(DictionarySource source);
        public partial CollectionVarietyTarget Map(CollectionVarietySource source);
        public partial CollectionEntityMapped Map(CollectionEntity source);
    }

    public record CollectionEntity(int Id);
    public record CollectionEntityMapped(int Id);

    public class SimplifiedCollections
    {
        public List<string>? Concrete { get; init; }
        public IReadOnlyList<string>? Interfaced { get; init; }
        public List<CollectionEntity>? ConcreteComplex { get; init; }
    }

    public class SimplifiedDictionaries
    {
        public Dictionary<string, int>? Concrete { get; init; }
        public IReadOnlyDictionary<string, int>? Interfaced { get; init; }
        public Dictionary<int, CollectionEntity>? ConcreteComplexValue { get; init; }
        public Dictionary<CollectionEntity, int>? ConcreteComplexKey { get; init; }
    }

    public class CollectionVarietySource
    {
        // Basic lists
        public required List<string> ConcreteList { get; init; }
        public required string[] ConcreteArray { get; init; }
        public required IList<string> IListInterface { get; init; }
        public required ICollection<string> ICollectionInterface { get; init; }
        public required IEnumerable<string> IEnumerableInterface { get; init; }
        public required IReadOnlyList<string> IReadOnlyListInterface { get; init; }
        public required IReadOnlyCollection<string> IReadOnlyCollectionInterface { get; init; }
        public required ISet<string> ISetInterface { get; init; }
        public required IReadOnlySet<string> IReadOnlySetInterface { get; init; }
        public required HashSet<string> ConcreteSet { get; init; }

        // Lists with mapped type
        public required CollectionEntity[] EntityArray { get; init; }
        public required IReadOnlyList<CollectionEntity> CollectionEntities { get; init; }

        // Additional lists
        public required IList<string> AdditionalIListInterface { get; init; }
        public required IEnumerable<string> AdditionalIEnumerableInterface { get; init; }
        public required HashSet<string> AdditionalHashSet { get; init; }

    }

    public class CollectionVarietyTarget
    {
        // Map basic lists as-is
        public required List<string> ConcreteList { get; init; }
        public required string[] ConcreteArray { get; init; }
        public required IList<string> IListInterface { get; init; }
        public required ICollection<string> ICollectionInterface { get; init; }
        public required IEnumerable<string> IEnumerableInterface { get; init; }
        public required IReadOnlyList<string> IReadOnlyListInterface { get; init; }
        public required IReadOnlyCollection<string> IReadOnlyCollectionInterface { get; init; }
        public required ISet<string> ISetInterface { get; init; }
        public required IReadOnlySet<string> IReadOnlySetInterface { get; init; }
        public required HashSet<string> ConcreteSet { get; init; }

        // Lists with mapped type
        public required CollectionEntityMapped[] EntityArray { get; init; }
        public required IReadOnlyList<CollectionEntityMapped> CollectionEntities { get; init; }

        // Map additional lists to other list types
        public required string[] AdditionalIListInterface { get; init; }
        public required List<string> AdditionalIEnumerableInterface { get; init; }
        public required ISet<string> AdditionalHashSet { get; init; }
    }

    public class DictionarySource
    {
        public required Dictionary<string, string> Dictionary { get; init; }
        public required Dictionary<string, CollectionEntity> EntityDictionary { get; init; }

        public required Dictionary<string, CollectionEntity[]> EntityArrayDictionary { get; init; }

        // Interfaces
        public required IDictionary<string, string> IDictionaryInterface { get; init; }
        public required IReadOnlyDictionary<string, string> IReadOnlyDictionaryInterface { get; init; }
    }

    public class DictionaryTarget
    {
        public required Dictionary<string, string> Dictionary { get; init; }
        public required Dictionary<string, CollectionEntityMapped> EntityDictionary { get; init; }
        public required Dictionary<string, CollectionEntityMapped[]> EntityArrayDictionary { get; init; }
        // Interfaces
        public required IDictionary<string, string> IDictionaryInterface { get; init; }
        public required IReadOnlyDictionary<string, string> IReadOnlyDictionaryInterface { get; init; }
    }
}