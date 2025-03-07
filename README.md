## Using .toml Configuration
To adjust the configuration with your mod, create `CustomizableMerciless.toml` in your mod directory and use the layout below:

```toml
[Multipliers]
DamageTaken = 1.6
DamageGiven = 0.8
ExpWon = 1.2
MoneyWon = 1.2
WeaknessTaken = 3
WeaknessGiven = 3
CritTechTaken = 3
CritTechGiven = 3

[Others]
GallowsExp = true
ReturnSafeRoom = false
```

As long as the user has the "Use .toml Configuration" option enabled (which is enabled by default), the .toml configuration will overwrite the Reloaded-II configuration.