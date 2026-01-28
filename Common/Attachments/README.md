# Attachments Module

Read Ability.cs, GlobalAbility.cs. It allows you to use most of the ModPlayer hooks, so you don't need to create a ModPlayer class. You need to register the ability in the main Mod class.

An example is in Content/Abilities/ExampleAbility.cs

Prefer having most code being done in 1 or 2 files within Content/Abilities

HUD can be done using PlayerHudApi.cs and PlayerHudElement.cs which provides a charging bar and text, though minimalism is recommended - only use the text to display transient information.

Projectiles can be done using Common/Base3DProjectile.cs which provides you Common/AnimationContext.cs to help with animation.
