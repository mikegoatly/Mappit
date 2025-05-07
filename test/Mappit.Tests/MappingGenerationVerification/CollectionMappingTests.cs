using Xunit;

namespace Mappit.Tests.MappingGenerationVerification
{
    public class CollectionMappingTests
    {
        [Fact]
        public void MappingCollections_ShouldCopyDataSuccessfully()
        {
            var source = new CollectionSource
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

            var expected = new CollectionTarget
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

            var mapper = new CollectionMap();
            var result = mapper.Map(source);

            Assert.NotNull(result);
            Assert.Equivalent(expected, result);
        }
    }

    [Mappit]
    public partial class CollectionMap
    {
        //public partial DictionaryTarget Map(DictionarySource source);
        public partial CollectionTarget Map(CollectionSource source);
        public partial CollectionEntityMapped Map(CollectionEntity source);
    }

    public record CollectionEntity(int Id);
    public record CollectionEntityMapped(int Id);

    public class CollectionSource
    {
        // Basic lists
#pragma warning disable CA1002 // Do not expose generic lists
        public required List<string> ConcreteList { get; init; }
#pragma warning restore CA1002 // Do not expose generic lists
#pragma warning disable CA1819 // Properties should not return arrays
        public required string[] ConcreteArray { get; init; }
#pragma warning restore CA1819 // Properties should not return arrays
        public required IList<string> IListInterface { get; init; }
        public required ICollection<string> ICollectionInterface { get; init; }
        public required IEnumerable<string> IEnumerableInterface { get; init; }
        public required IReadOnlyList<string> IReadOnlyListInterface { get; init; }
        public required IReadOnlyCollection<string> IReadOnlyCollectionInterface { get; init; }
        public required ISet<string> ISetInterface { get; init; }
        public required IReadOnlySet<string> IReadOnlySetInterface { get; init; }
        public required HashSet<string> ConcreteSet { get; init; }

        // Lists with mapped type
#pragma warning disable CA1819 // Properties should not return arrays
        public required CollectionEntity[] EntityArray { get; init; }
#pragma warning restore CA1819 // Properties should not return arrays
        public required IReadOnlyList<CollectionEntity> CollectionEntities { get; init; }

        // Additional lists
        public required IList<string> AdditionalIListInterface { get; init; }
        public required IEnumerable<string> AdditionalIEnumerableInterface { get; init; }
        public required HashSet<string> AdditionalHashSet { get; init; }

    }

    public class CollectionTarget
    {
        // Map basic lists as-is
#pragma warning disable CA1002 // Do not expose generic lists
        public required List<string> ConcreteList { get; init; }
#pragma warning restore CA1002 // Do not expose generic lists
#pragma warning disable CA1819 // Properties should not return arrays
        public required string[] ConcreteArray { get; init; }
#pragma warning restore CA1819 // Properties should not return arrays
        public required IList<string> IListInterface { get; init; }
        public required ICollection<string> ICollectionInterface { get; init; }
        public required IEnumerable<string> IEnumerableInterface { get; init; }
        public required IReadOnlyList<string> IReadOnlyListInterface { get; init; }
        public required IReadOnlyCollection<string> IReadOnlyCollectionInterface { get; init; }
        public required ISet<string> ISetInterface { get; init; }
        public required IReadOnlySet<string> IReadOnlySetInterface { get; init; }
        public required HashSet<string> ConcreteSet { get; init; }

        // Lists with mapped type
#pragma warning disable CA1819 // Properties should not return arrays
        public required CollectionEntityMapped[] EntityArray { get; init; }
#pragma warning restore CA1819 // Properties should not return arrays
        public required IReadOnlyList<CollectionEntityMapped> CollectionEntities { get; init; }

        // Map additional lists to other list types
#pragma warning disable CA1819 // Properties should not return arrays
        public required string[] AdditionalIListInterface { get; init; }
#pragma warning restore CA1819 // Properties should not return arrays
#pragma warning disable CA1002 // Do not expose generic lists
        public required List<string> AdditionalIEnumerableInterface { get; init; }
#pragma warning restore CA1002 // Do not expose generic lists
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