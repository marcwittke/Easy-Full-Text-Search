# Easy Full Text Search
Easy Full Text Search is a .NET class library to convert a user-friendly, Google-like search term into a valid Microsoft SQL Server full-text-search query. Attempts to gracefully handle all syntax errors.

# More Information
Microsoft SQL Server provides a powerful full-text search feature. However, the syntax is rather cryptic, especially for non-programmers. Moreover, there are many conditions that can cause SQL Server to throw up an error if things aren't exactly right.

Easy Full Text Search converts a user-friendly, Google-like search term to the corresponding full-text search SQL query. The class' goals are to:

- Throw no exceptions on badly formed input. Simply construct a valid query as best it can regardless of input.
- Always product a valid SQL query. One example is that SQL Server will throw an error if the first term in a query is an exclusion term. In this case, Easy Full Text Search will actually rearrange the order of the search terms within the query.

# Input Syntax
The following list shows how various input syntaxes are interpreted.

| Term | Meaning
| ---  | ---
| abc | Find inflectional forms of abc
| ~abc | Find thesaurus variations of abc
| +abc | Find exact term abc
| "abc" | Find exact term abc
| abc* | Finds words that start with abc
| -abc | Do not include results that contain inflectional forms of abc
| abc def | Find inflectional forms of both abc and def
| abc or def | Find inflectional forms of either abc or def
| &lt;abc def&gt; | Find inflectional forms of abc near def
| abc and (def or ghi) | Find inflectional forms of both abc and either def or ghi
