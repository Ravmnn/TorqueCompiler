***(sketch)***

Structures are new data types the user can create to organize related data in the same place. Structures can be created the following way:

```
struct Position
{
    float x;
    float y;
    float z;
}
```

Above, `x`, `y` and `z` are fields. Fields are raw data values that a structure can have.
A structure can be created with `Structure { field: value, field: value, ... }`:

```
int main()
{
    var position = Position { x: 10, y: 50, z: 1 };

    var xyz = position.x * position.y * position.z;
}
```