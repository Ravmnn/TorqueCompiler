# Specifications

- Imperative, Functional.
- Static Typed, strong typing system.
- Ergonomic code, with practical, but minimalistic standard library.
- Manual memory management, but the stdlib helps that process.

- Focus on composition design, interfaces. Generic programming must be supported.

- Native standard library support, allowing special syntax like `T?` to describe something like `Optional<T>`. This creates high coupling to the standard library... Maybe add this feature as metaprogramming? So programmers can have more control over it.
- Error handling will be propagated with values, exceptions don't exist. Maybe `T!` (maps to `Error<T>`) to declare that a function may return a value that may be an error?
- Allows interoperability with low-level code, from C, for example.
- Has a package manager, `Torch` maybe?:
  - Tor: Torque
  - c: Command-Line
  - h: Helper