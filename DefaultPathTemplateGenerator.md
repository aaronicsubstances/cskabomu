# Default Path Template Generator

Provides the default path template generation algorithm used in the Kabomu.Mediator framework. Inspired by [Routing in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-6.0),
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


Each CSV row is of one of these formats:
   - an empty row. Useful for visually sectioning parts of a CSV spec.
   - first column starts with forward slash ("/") or slash for short. each column including the first
must be a path matching expression containing zero or more path variables.
   - first column starts with "name:". the suffix after "name:" becomes the non-unique label for the remaining columns as a group.
such a group can be targetted if the label is present as a key in any dictionary of match options available.
the second and remaining columns must be path matching expressions.
   - first column starts with "defaults:". the second and remaining columns are interpreted as a list of alternating 
key value pairs, which will be used to populate a map of default values. the keys correspond to path variables
which may be present in path matching expressions.
   - first column starts with "check:". then the suffix after "check:" will be taken as a path variable which may
be present in path matching expressions. the second column must be a key mapped to a constraint function in a dictionary of
such functions. the third and remaining columns are interpreted as a list of arguments which should be stored and passed to 
the constraint function every time path matching or interpolation is requested later on.
   - first column is empty. then current row must not be the first row, and the
previous row must not be an empty row. in this case the first column will be treated as if its value equals that of the
nearest non-empty first column above current row. the rest of the columns will be processed accordingly.

A path matching expression is either a single slash, or a concatenation of at least one of the following
segment expressions:
   - single slash followed by one or more non-slash characters. indicates a literal path segment expression
   - double slash followed by one or more non-slash characters. indicates a single path segment expression.
The non-slash characters become a path variable key
   - triple slash followed by one or more non-slash characters, and is a wild card segment expression for matching zero
or more path segments. The non-slash characters become a path variable key

Within a path matching expression,
   - all non-slash characters for a literal, single or wild card segment expressions will be trimmed of
surrounding whitespace.
   - path variables must be unique
   - at most only 1 wild card segment expression may be present
   - empty path segments are not matched by single path segment expressions by default.
