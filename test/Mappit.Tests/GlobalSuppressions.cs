// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Unit tests use underscores in names")]
[assembly: SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "A number of type names mention Enum because that's part of the test")]
[assembly: SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Use of interfaces in tests rather than concrete types ensures that interface generation is done correctly")]
[assembly: SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Not relevant in unit tests")]
