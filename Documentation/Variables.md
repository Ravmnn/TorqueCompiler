## About

Variables are memory locations used to store values, allowing the program to retrieve that value later. Using a variable is as easy as in any other language, following similar syntax to many languages, like C:

```
int number1 = 10;
int number2 = 20;
int result = number1 + number2;
```


You can optionally omit the variable type by using `let` (***sketch***) instead, making the compiler to automatically infer the type from the initial value. This feature is only allowed for function-scope variables (those that aren't fields, parameters, etc).

By default, variables are mutable, but in many cases, making them immutable is required or at least recommended. You can use `fixed` (***sketch***) to achieve that. `fixed` makes the variable to be fixed to its initial value, meaning reassignment is not allowed.

```
fixed float constant = 3.14159;
constant = 3; # error!
```
