# How perform deep clone of .NET objects as fast as possible?
I have a test class (**TestClass**), which contains multiple properties including direct and indirect references to other instances of the same class. The question is simple. How fast can we perform a deep clone of this class instance? With the test data, which contains about 10 thousand instances with about 60 thousand properties, a hardcoded clone takes about 6 ms on my laptop. Is it possible to implement a generic cloning approach with a similar performance? In this case, we don't assume cycles in the object hierarchy.

With a clever use of reflection we can perform a deep clone of the same data in about 40 ms, which is about 7 times slower than hardcoded approach (see class **ObjectClonerElement**, method **Clone** and code under *Clone using pure reflection*). Not bad, but can we do any better? 

We can use the .NET compiled expressions (see for example https://docs.microsoft.com/cs-cz/dotnet/csharp/programming-guide/concepts/expression-trees/). Using this approach we can get to about 10 ms for a deep clone of the same data, which is just about 60% slower than the hardcoded approach. The code is available in class **ObjectClonerElement**, method **Clone** under *Clone based on compiled expressions*.

All the times are summarized in the following table.

**Method**|**Time[ms]**
----------|-----------:
Hardcoded            | 6
Compiled expressions | 10
Reflection           | 40
