# Easy Full Text Search
Easy Full Text Search is a .NET class library to convert a user-friendly, Google-like search term into a valid Microsoft SQL Server full-text-search query. Attempts to gracefully handle all syntax errors.

# More Information
Microsoft SQL Server provides a powerful full-text search feature. However, the syntax is rather cryptic, especially for non-programmers. Moreover, there are many conditions that can cause SQL Server to throw up an error if things aren't exactly right.

Easy Full Text Search converts a user-friendly, Google-like search term to the corresponding full-text search SQL query. Its goal is to never throw exceptions on badly formed input. It attempts to simply construct a valid query as best it can, regardless of the input.

# Input Syntax
The following list shows how various input syntaxes are interpreted.

| Term | Meaning
| ---- | ----
| abc | Find inflectional forms of abc.
| ~abc | Find thesaurus variations of abc.
| +abc | Find exact term abc.
| "abc" | Find exact term abc.
| abc* | Finds words that start with abc.
| -abc | Do not include results that contain inflectional forms of abc.
| abc def | Find inflectional forms of both abc and def.
| abc or def | Find inflectional forms of either abc or def.
| &lt;abc def&gt; | Find inflectional forms of abc near def.
| abc and (def or ghi) | Find inflectional forms of both abc and either def or ghi.

# Prevent SQL Server Errors
Another goal of Easy Full Text Search is to always produce a valid SQL query. While the expression tree may be properly constructed, it may represent a query that is not supported by SQL Server. After constructing the expression tree, the code traverse the tree and takes steps to correct any conditions that would cause SQL Server to throw an error

| Term | Action Taken
| ---- | ----
| NOT term1 AND term2 | Subexpressions swapped.
| NOT term1 | Expression discarded.
| NOT term1 AND NOT term2 | Expression discarded if node is grouped (parenthesized) or is the root node; otherwise, the parent node may contain another subexpression that will make this one valid.
| term1 OR NOT term2 | Expression discarded.
| term1 NEAR NOT term2 | NEAR conjunction changed to AND.
