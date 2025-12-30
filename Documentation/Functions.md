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

You also can embed parameters to the function in order to receive external arguments:

```
float sumThenMult(int x, int y, float factor)
{
    return (x + y) * factor;
}




var result = sumThenMult(10, 50, 5); # 300
```

If the function body is one-statement long, you can use the `=>` operator to simplify.
If the function is not `void`, `=>` will return the expression after it. If it's `void`, the expression will be evaluated, but its resultant value will be ignored.

```
float pi2() => 3.14159 * 2;
```