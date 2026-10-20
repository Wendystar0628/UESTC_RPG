namespace Campus.Contracts;

public enum RegistrationKind { Service, Content, Part, Interior, Ambient, Medium, Node, Bake, Save }
public enum RegistrationLifetime { Global, Map, Transient }

/// <summary>Identity and ownership shared by every registration, including pure generation extensions.</summary>
public abstract class RegistrationAttribute(RegistrationKind kind, string id, RegistrationLifetime lifetime) : Attribute
{
    public RegistrationKind Kind { get; } = kind;
    public string Id { get; } = id;
    public string Owner { get; set; } = "";
    public string Map { get; set; } = "";
    public bool IsStub { get; set; }
    public RegistrationLifetime Lifetime { get; set; } = lifetime;
    public virtual Type? Contract => null;
    public virtual string? IdentityScope => null;
}
