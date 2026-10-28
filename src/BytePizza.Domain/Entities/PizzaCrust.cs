using BytePizza.Domain.ValueObjects;

namespace BytePizza.Domain.Entities;

public sealed record PizzaCrust(string Name, Money AdditionalPrice);
