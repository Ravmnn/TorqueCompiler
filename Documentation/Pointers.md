As a low level programming language, Torque has directly support for handling pointers. The syntax doesn't differs from C.

```
let number = 58;
let ptr = &number; # type is "int*"

*ptr = 10;

ptr;    # the memory address
*ptr;   # 10
number; # 10
```