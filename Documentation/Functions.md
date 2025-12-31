Functions are block of codes that can be reused over and over. They can optionally return a value.
Here's how you create a function:

```
int getValue()
{
    return 7;
}
```

And here's how to call it, in this case, to get the return value:

```
int value = getValue(); # "value" is 7, the number the function returned
```

You also can embed parameters to the function in order to receive external arguments.
Parameters can have a default value in case they're not explicitly passed to the function ***(sketch)***, making the parameter optional. Optional parameters must be defined after all required parameters.

```
float sumThenMult(int x, int y, float factor = 2)
{
    return (x + y) * factor;
}




var result = sumThenMult(10, 50); # 120
```

If the function body is one-statement long, you can use the `=>` operator to simplify ***(sketch)***.
If the function is not `void`, `=>` will return the expression after it. If it's `void`, the expression will be evaluated, but its resultant value will be ignored.

```
float pi2() => 3.14159 * 2;
```