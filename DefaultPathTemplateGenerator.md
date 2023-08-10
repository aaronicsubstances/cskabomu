# Default Path Template Generator

## Introduction

The [DefaultPathTemplateGenerator](https://github.com/aaronicsubstances/cskabomu/blob/main/src/Kabomu/Mediator/Path/DefaultPathTemplateGenerator.cs) class provides the default path template generation algorithm used in the Kabomu.Mediator framework. Inspired by [Routing in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-6.0),
it contains concepts similar to ASP.NET Core routing concepts of templates, catch-all parameters,
default values, and constraints.

This class generates path templates out of CSV specifications.
One notable difference between those specs and ASP.NET Core routing templates is that
path segments with default values are treated almost like optional path segments.
They can be only be specified indirectly by
generating all possible routing templates with and without segments which can be optional or have a default values,
from shortest to longest.
Default values are then specified separately for storage in a dictionary.

As an example, an ASP.NET Core routing template of "{controller=Home}/{action=Index}/{id?}" corresponds to the CSV below:
```
/,//controller,//controller//action,//controller//action//id
defaults:,controller,Home,action,Index
```

Another ASP.NET Core routing template of "blog/{article:minlength(10)}/\*\*path" corresponds to:
```
/blog//article///path
check:article,minlength,10
```

Another difference is that the CSV specs do not allow for separators between path segment expressions other
than forward slashes. So the ASP.NET Core routing template of "{country}-{region}" does not have a direct translation.

## Default Path Template Specification

Unless a custom [IPathTemplateGenerator](https://github.com/aaronicsubstances/cskabomu/blob/main/src/Kabomu/Mediator/Path/IPathTemplateGenerator.cs) implementation has been put into the context of
a [MediatorQuasiWebApplication](https://github.com/aaronicsubstances/cskabomu/blob/main/src/Kabomu/Mediator/MediatorQuasiWebApplication.cs) instance, by default the second and third arguments of
the extension method Path(this IRegistry registry, string spec, object options, params Handler[] handlers) of the
[HandlerUtils](https://github.com/aaronicsubstances/cskabomu/blob/main/src/Kabomu/Mediator/Handling/HandlerUtils.cs)
class, correspond to the arguments of the
*DefaultPathTemplateGenerator.Parse(string spec, object options)* method.

The DefaultPathTemplateGenerator.Parse() method takes a string of CSV data as its first spec argument, and takes an optional second options argument. If the options argument is not null, then it must either be an instance of [DefaultPathTemplateMatchOptions](https://github.com/aaronicsubstances/cskabomu/blob/main/src/Kabomu/Mediator/Path/DefaultPathTemplateMatchOptions.cs) class,
or a dictionary (instance of IDictionary\<string, DefaultPathTemplateMatchOption\>).

Each row in the CSV spec argument is of one of these formats:
   - an empty row. Useful for visually sectioning parts of a CSV spec.
   - first column starts with forward slash ("/") or slash for short. Each column including the first
must be a path matching expression containing zero or more path variables.
   - first column starts with "name:". The suffix after "name:" becomes the non-unique label for the remaining columns as a group.
Such a group can be targetted if the label is present as a key in any dictionary of match options available.
the second and remaining columns must be path matching expressions.
   - first column starts with "defaults:". The second and remaining columns are interpreted as a list of alternating 
key value pairs, which will be used to populate a map of default values to be used
during matching. The keys correspond to path variables
which may be present in path matching expressions. During path matching, when a match is 
made, the matched path variables will be merged with the default values, with the default values possibly being overwritten.
   - first column starts with "check:". Then the suffix after "check:" will be taken as a 
   path variable which may be present in path matching expressions. The second column must be a key mapped to a constraint function
(implementation of [IPathConstraint](https://github.com/aaronicsubstances/cskabomu/blob/main/src/Kabomu/Mediator/Path/IPathConstraint.cs) interface)
in the *ConstraintFunctions* property of the DefaultPathTemplateGenerator instance. The
third and remaining columns are interpreted as a list of arguments which should be
stored and passed to the constraint function every time path matching or interpolation
is requested later on.
During matching and interpolation, all checks which reference a path variable
in captured path variables have to pass for the match to be
considered successful.
   - first column is empty. Then current row must not be the first row, and the
previous row must not be an empty row. In this case the first column will be treated as if its value equals that of the
nearest non-empty first column above current row. The rest of the columns will be processed accordingly.

A path matching expression is either a single slash, or a concatenation of at least one of the following
segment expressions:
   - single slash followed by one or more non-slash characters: indicates a literal path segment expression
   - double slash followed by one or more non-slash characters: indicates a single path segment expression.
The non-slash characters become a path variable key
   - triple slash followed by one or more non-slash characters: is a wild card segment expression for matching zero
or more path segments. The non-slash characters become a path variable key

Within a path matching expression,
   - all non-slash characters for a literal, single or wild card segment expressions will be trimmed of
surrounding whitespace.
   - path variables must be unique
   - at most only 1 wild card segment expression may be present
   - empty path segments are matched by single path segment expressions if and only if the path variable key following the double slash is begins or ends with whitespace. Note that wild card segment expressions always match empty path segments.

## Matching and Interpolation of Paths

If no options are specified for the DefaultPathTemplateGenerator.Parse(),
then the [IPathTemplate](https://github.com/aaronicsubstances/cskabomu/tree/main/src/Kabomu/Mediator/Path/IPathTemplate.cs)
instance it returns uses the *IPathTemplate.Match()* method to match a path. By default path matching is done regardless
of whether it starts or ends with slashes, case of path is ignored,
and any value captured from a single path segment expression is
unescaped of URL percent encodings after capture.
If options are specified, then they are used to modify this default behaviour, either for all path matching expressions (if options object is a single DefaultPathTemplateMatchOptions
instance) or for specific named path matching expressions (if options object is a
dictionary, in which case the names are looked up among the keys of the dictionary).
Note that values captured from
wild card segment expressions are never unescaped of any URL percent encodings regardless
of options.

Matching is done by the Path() methods of the HandlerUtils class on the
*UnboundRequestTarget* property of the top instance of the
[IPathMatchResult](https://github.com/aaronicsubstances/cskabomu/blob/main/src/Kabomu/Mediator/Path/IPathMatchResult.cs)
stack stored in the quasi web context. That stack starts out by default with an instance 
whose *UnboundRequestTarget* property equals the target of the request being
processed. And any time a new IPathMatchResult is generated by one of the Path()
methods of the HandlerUtils class, it is pushed on to the stack.

The DefaultPathTemplateMatchOptions class has a counterpart in
[DefaultPathTemplateFormatOptions](https://github.com/aaronicsubstances/cskabomu/blob/main/src/Kabomu/Mediator/Path/DefaultPathTemplateFormatOptions.cs), which is the type of the acceptable options argument of the *IPathTemplate.InterpolateAll()* and *IPathTemplate.Interpolate()* methods of
the instance returned by DefaultPathTemplateGenerator.Parse(). The InterpolateAll() method
generates all possible paths and may return an empty list. The Interpolate() method
picks the shortest path returned by InterpolateAll(), and fails if no path could be
generated.

## Examples

See [DefaultPathTemplateInternalTest1](https://github.com/aaronicsubstances/cskabomu/blob/main/test/Kabomu.Tests/Mediator/Path/DefaultPathTemplateInternalTest1.cs)
for examples of default path matching.

And for examples of default path interpolation see [DefaultPathTemplateInternalTest2](https://github.com/aaronicsubstances/cskabomu/blob/main/test/Kabomu.Tests/Mediator/Path/DefaultPathTemplateInternalTest2.cs) and
[DefaultPathTemplateInternalTest3](https://github.com/aaronicsubstances/cskabomu/blob/main/test/Kabomu.Tests/Mediator/Path/DefaultPathTemplateInternalTest3.cs).
