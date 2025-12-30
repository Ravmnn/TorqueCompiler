***(sketch)***

Structures are new data types the user can create to organize related data and behavior in the same place. Structures can be created the following way:

```
struct Position
{
    float x;
    float y;
    float z = 1;
}
```

Above, `x`, `y` and `z` are fields. Fields are raw data values that a structure can have.
A field, similarly to variables, can be marked as `fixed`, making it unable to change its initial value.
A structure can be created with the expression `Structure { field: value, field: value, ... }`:
You can define an initial value to a field in its declaration, so if the structure initializer doesn't explicitly initializes that field, the specified initial value will be used. If the field doesn't have an initial value specified in its declaration, and a structure initializer doesn't initializes that same field, the [Default Value](./Default%20Values.md) for the field type will be used to initialiaze it.

```
var position = Position { x: 10, y: 50 }; # here, "z" will be 1
var otherPosition = Position {}; # "x" and "y" are 0, "z" is 1

var xyz = position.x * position.y * position.z;
```

If the value set to a field in the initializer is a variable of the same name of the field, it can be simplified this way:

```
var x = 5;
var y = 3;

var position = Position { x, y };
```

Structure can also contain methods, which are functions that belongs to a structure.
A method can access the fields of a structure and modify them directly.

```
struct Gun
{
    int bulletAmount;
    int maxBulletAmount;
}


implement Gun
{
    void reload() => bulletAmount = maxBulletAmount;
    void shoot() => bulletAmount--;

    bool isLoaded() => bulletAmount > 0;
}




var gun = Gun { maxBulletAmount: 12 };
gun.reload();

while (gun.isLoaded())
    gun.shoot();
```

Structures can also have static methods, that belongs to the structure scope instead of the structure "instance". Since Torque doesn't support constructors (a feature present in general OOP languages for initializing objects) and creating a structure from the structure initializer expression isn't always the best choice, if you want to have more control of a structure initialization, you should create a static method called `new`. Nothing special about the name, it's just a convention, follow it.

```
implement Gun
{
    static Gun new(int maxBulletAmount)
    {
        var gun = Gun { maxBulletAmount };
        gun.reload();

        return gun;
    }




    void reload() => bulletAmount = maxBulletAmount;
    void shoot() => bulletAmount--;

    bool isLoaded() => bulletAmount > 0;
}




var gun = Gun::new(12);
gun.reload();

while (gun.isLoaded())
    gun.shoot();
```