namespace Torque.Compiler.Target;




public readonly record struct TargetTriple
{
    public ArchitectureType Architecture { get; init; }
    public OperationalSystemType OperationalSystem { get; init; }
    public EnvironmentType Environment { get; init; }
    public VendorType Vendor { get; init; }




    public override string ToString()
        => $"{Architecture}-{Vendor}-{OperationalSystem}-{Environment}".ToLower();
}
