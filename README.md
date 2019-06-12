# StreamCompare by NeoSmart Technologies

`StreamCompare` is a .NET library and NuGet package for efficient comparison of `Stream` objects,
checking for the equality of the data they contain. `StreamCompare` is compatible with .NET Standard
1.3 and above.

## What's this for?

A `Stream` in .NET is somewhat of an ornery object given that it typically abstracts away IO
operations that may or may not be otherwise directly accessible. If you need to check if two files
or network resources are byte-for-byte equivalent, strategies like hashing and digests [only make
sense](http://neosmart.net/blog/2019/compare-streams-nuget/) if you plan on comparing against the
same file more than (at least) once. If you have a one-off comparison to make between two files or
streams, it's always more efficient to simply compare their contents byte-by-byte until you find a
mismatch (or don't).

`StreamCompare` does this for you, and abstracts away all the edge cases and performance pitfalls.
We've strived to make it as simple and straight-forward to use as possible.

Read more about `StreamCompare` in [the official release
article](http://neosmart.net/blog/2019/compare-streams-nuget/).

## License and authorship

`StreamCompare` is released to the general public under the terms of the MIT public license in hopes
that it is helpful in writing more efficient and sound code. `StreamCompare` was developed by
Mahmoud Al-Qudsi of NeoSmart Technologies, factored out of code we've written in the past for some
of our IO-heavy projects.

## Installation

`StreamCompare` is [available on NuGet](https://www.nuget.org/packages/StreamCompare/) and may be
installed by executing the following in the Visual Studio Package Manager:

```
Install-Package StreamCompare
```

## Usage

`StreamCompare` is extremely straightforward to use. The `new
NeoSmart.StreamCompare.StreamCompare()` creates a new `StreamCompare` instance that may be used to
compare two `Stream`s. Reuse of `StreamCompare` objects is encouraged where possible (a single
`StreamCompare` instance should be used by one and only one thread at a time).

### `StreamCompare`

This is the core object used for comparison of `Stream` objects for equality. It may (should) be
reused to minimize buffer allocations where possible.

#### `StreamCompare()`

Creates a new `StreamCompare` object with the default settings. The static
`StreamCompare.DefaultBufferSize` property indicates what buffer size should be used.

#### `StreamCompare(uint BufferSize)`

Creates a new `StreamCompare` object with the specified buffer size.

#### `StreamCompare.AreEqualAsync(Stream stream1, Stream stream2, CancellationToken cancel, bool? forceLengthCompare)`

Compares two `Stream` instances for bytewise equality. The comparison aborts when the first
difference between the two streams is encountered. Streams are read in parallel where/when possible.

* `Stream stream1`: The first `Stream` instance to read from for comparison
* `Stream stream2`: The second `Stream` resource to read from for comparison
* `CancellationToken cancel`: The optional `CancellationToken` to be used to terminate IO
  operations. `CancellationToken.None` is used if this is not provided.
* `bool? forceLengthCompare`: Tri-state determining whether or not `Stream.Length` is first checked
  for equality between the two `Stream` instances. When not set or set to `null`, a heuristic is
  used to guess whether or not `Stream.Length` can be safely accessed to short-circuit stream
  comparison when the `Stream` instances differ in length.

### `FileCompare`

`FileCompare` is a convenience wrapper around `StreamCompare` that can be used to compare two files
for equality via their paths. It may (should) be reused to minimize buffer allocations where possible.

#### `FileCompare.AreEqualAsync(string path1, string path2, CancellationToken cancel)`

The main entry point for comparing the contents of two files. It contains some optimizations for
short-circuiting stream comparison in cases where the equality of the files can be determined
without opening them for read.

* `string path1`: The absolute or relative path to the first file to compare
* `string path2`: The absolute or relative path to the second file to compare
* `CancellationToken cancel`: The optional `CancellationToken` to use for IO operations for early
  termination. When not supplied, `CancellationToken.None` is used instead.

