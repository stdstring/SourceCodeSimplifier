# SourceCodeSimplifier project

Source code simplification tool intended for converting some specific constructions of C# into more simple ones for simplification of further porting process into another languages (e.g. string interpolation, object initializers etc).

## Phase 0:

1. implementation of core of SourceCodeSimplifier app
1. implementation of app configuring via config file in XML format
1. implementation of manage different transformers via config file

## Phase 1:

1. implementation of transformer for object initializer expressions
1. implementation of transformer for **nameof** expressions
1. implementation of transformer for string interpolation expression
1. implementation of transformer for null-conditional operators
1. implementation of transformer for expression-bodied properties and methods
1. implementation of transformer for **out** inline variables
