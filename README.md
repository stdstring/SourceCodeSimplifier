# SourceCodeSimplifier project

Source code simplification tool intended for converting some specific constructions of C# into more simple ones for simplification of further porting process into another languages (e.g. string interpolation, object initializers etc).

## Phase 0:

1. implementation of core of SourceCodeSimplifier app **(implemented)**
1. implementation of app configuring via config file in XML format **(implemented)**
1. implementation of manage different transformers via config file **(implemented)**

## Phase 1:

1. implementation of transformer for object initializer expressions **(implemented)**
1. implementation of transformer for **nameof** expressions **(implemented)**
1. implementation of transformer for string interpolation expression **(implemented)**
1. implementation of transformer for null-conditional operators **(implemented)**
1. implementation of transformer for **out** inline variables

## Phase 2:

1. implementation of transformer for expression-bodied properties and methods