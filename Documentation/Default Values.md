***(sketch)***

You can omit the value of some declarations like variable declarations and in fields of a structure initializer. If that happens, the variable or field will be initialized with a default value of its type.
For primitive types, including pointers (all of them are just numbers), the default value is a `0`. For arrays, `0` is also the default, since arrays are just pointers. For structure types, all the fields of the structure are going to be set to their default.


```
struct Date
{
    int day;
    int month;
    int year;
}


int number; # 0
bool condition; # false (0)

Date date; # all of the structure fields are going to be initialized with default values of their types.
```

You can also explicitly get the default value for a type using the `default(T)` expression.