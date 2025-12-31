***(sketch)***

Arrays are different values of the same type continuosly in memory. Once an array is created, its size cannot be changed, so adding or removing items is not possible. To create one, the item type and array size must be provided:

```
var numbers = int[5];
```

The above example creates an array of `int` with 5 items. The items inside an array are initialized with their [Default Values](./Default%20Values.md), unless they are explicitly specified.

```
var numbers = int[5] { 50, 20, 100 };
# numbers is [50, 20, 100, 0, 0]
```

The array is filled with default values for the type if there's empty space.
The items inside an array can be accessed with the following expression:

```
numbers[0]; # 50
numbers[1]; # 20
numbers[4]; # 0
```