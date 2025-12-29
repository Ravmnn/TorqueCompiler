Torque is a language in which one of its main goals is to be readable, but that cannot be completely accomplished without the programmer's help, so inside this document, some suggestions will be gave to improve code consistency. You can call these as the "standard style guidelines".


## Name Casing

- PascalCase: files (directories included), packages, structs, interfaces.
- camelCase: variables, fields, parameters, functions.


## General Identifier Naming

Use descritive names for variables, fields, functions, structs, interfaces, files, etc. Avoid the use of too-specific acronyms. Globally used ones like "IO", "std", "dev"... are allowed.


## Variables

You can optionally omit the type of a variable definition and use the `var` specifier to automatically infer its type. This feature was meant to improve code readability by removing the pollution of types in many cases. You can decide whether to use this or not, but keep it consistent: If you decide to use it, always use it; if not, never use it. If you use it, however, keep in mind that your variables name should be even more self descriptive, since less information about the variable would be gathered at first look.

The use of `var` is recommended.


## Code Spacing

Don't be compact. Compact code is less readable because it's harder to identify the subsections of the code. The following rules are just suggestions, you don't need to follow all of them strictly, but be wise and write good code:
- at least 2 line spaces between related functions.
- at least 4 line spaces between non-related functions.
- prefer 4 line spaces between structs and interfaces. 2 line spaces are allowed if the structs or interfaces are closely related.
- maximum of 2 line spaces between sections in block-code, but prefer only 1 line. Add a line space between code that does different things.
- at least 1 line after a `package` instruction, prefer 2 lines.
- 1 line space between `unpack` instructions that are of different root packages.
- 4 lines after all `unpack` instructions.


## Comments

Whenever you add a comment to explain a section of code, check if it's not possible to refactor something in order to keep things more readable, like renaming something, spliting code into smaller functions, etc.