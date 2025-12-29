Variables are memory locations used to store values, allowing the program to retrieve that value later. Using a variable is as easy as in any other language, following similar syntax to many languages, like C:

```
int number1 = 10;
int number2 = 20;
int result = number1 + number2;
```

A variable can be declared without specifying its value. If that occurs, the value will be implicitly set to `default(T)` (***sketch***). That expression returns the default value of a type. For primitive types, including pointers, that's a `0`. For structures, it'll return the structure with all fields set to their default.

Variable value assignment after its definition is supported through the expression `variable = value`,
but you can set it as `fixed` (***sketch***), so its reassignment is not allowed: `fixed int forever = 100;`.
You can optionally omit the variable type by using `var` (***sketch***) instead, making the compiler to automatically infer the type from the initial value.
