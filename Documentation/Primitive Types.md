Torque has a bunch of embedded primitive types, most of them being variations of integer sizes:
- `int8`: Signed integer of 8 bits (1 byte)
- `int16`: Signed integer of 16 bits (2 bytes)
- `int32`: Signed integer of 32 bits (4 bytes)
- `int64`: Signed integer of 64 bits (8 bytes)
- `uint8`: Unsigned integer of 8 bits (1 bytes)
- `uint16`: Unsigned integer of 16 bits (2 bytes)
- `uint32`: Unsigned integer of 32 bits (4 bytes)
- `uint64`: Unsigned integer of 64 bits (8 bytes)
- ***sketch*** `ptrsize`: Unsigned integer of size equal to any pointer, 32 bits in 32-bit architectures and 64 bits in 64-bit ones.

Floating point types are also supported:
- `float16`: Floating point number of 16 bits (2 bytes)
- `float32`: Floating point number of 16 bits (4 bytes)
- `float64`: Floating point number of 16 bits (8 bytes)

Besides all of that, a `bool` type is also supported to represent boolean types (`true` or `false`).
In the back-end of the official Torque compiler (LLVM), `bool` is implemented as single-bit size, but
that only means LLVM can pack lots of booleans in a single byte to optimize memory usage, so if you create
a single boolean value, it won't use only 1 bit of memory, it'll use 8 bits (1 byte), which is the minimum valid
memory unit that CPUs can handle.

`char` is used to represent ANSI characters.

There's also a `void` type, but it cannot be used to create values and its use should be only for
specifying that a function doesn't return, for example.

Torque also provides some shortcut aliases to better identify types:
- `byte`: `uint8`, use this when the data you want to represent makes more sense in raw binary, like
image, audio, binary files, etc.
- `int`: `int32`
- `uint`: `uint32`
- `float`: `float32`
- ***sketch*** `half`: `float16`
- ***sketch*** `double`: `float64`

Using the aliases above or not can be decided by the programmer.