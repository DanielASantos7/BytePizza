using BytePizza.Domain.ValueObjects;

namespace BytePizza.Domain.Entities;

public sealed record PizzaFlavor(string Name, Money Price, FlavorFraction Fraction);
