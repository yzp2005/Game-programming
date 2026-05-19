# What we learned: Collision, Damage and Death Event Chain
Group: 
Scripts studied: Projectile.cs, Damage.cs, Health.cs

## New concept

We learned that collision in Unity is event-driven, not manually checked by our code. Unity provides multiple built-in collision functions: **OnTriggerEnter2D()**, **OnTriggerStay2D()**, and **OnCollisionEnter2D()**. These functions are called automatically by the engine when a collision or trigger interaction occurs. In our game, these events act as the starting point of the entire damage and death process, connecting the projectile to the enemy’s health system.

## Code evidence

```csharp
// Projectile.cs
private void MoveProjectile()
    {
        // move the transform
        transform.position = transform.position + transform.up * projectileSpeed * Time.deltaTime;
    }

// Damage.cs
private void OnTriggerEnter2D(Collider2D collision)
{
    if (dealDamageOnTriggerEnter)
    {
        DealDamage(collision.gameObject);
    }
}

private void DealDamage(GameObject collisionGameObject)
{	
    ...
    collidedHealth.TakeDamage(damageAmount);
    ...
    Destroy(this.gameObject);
    ...
}

// Health.cs
public void TakeDamage(int damageAmount)
{
    if (isInvincableFromDamage || isAlwaysInvincible)
    {
        return;
    }
    else
    {
	    ...
        currentHealth -= damageAmount;
        CheckDeath();
    }
}

private void CheckDeath()
{
    if (currentHealth <= 0)
    {
        Die();
    }
}

public void Die()
{
    ...
    if (useLives)
    {
        HandleDeathWithLives();
    }
    else
    {
        HandleDeathWithoutLives();
    }      
}
```

## Event chain

**Projectile.cs:** Update() -> MoveProjectile() -> transform.position changes every frame
->
**Damage.cs:** OnTriggerEnter2D / OnTriggerStay2D / OnCollisionEnter2D -> DealDamage() -> Finds Health component and applies damage to target
->
**Health.cs:** TakeDamage() -> CheckDeath() -> Die() -> Reduces health and destroys object when health is zero

## Why this matters

This chain shows how Unity’s built-in events (like collisions) work with our custom code.Instead of writing a loop to check for collisions, we let the engine call our functions when the event happens.This makes the code modular: Projectile.cs handles movement, Damage.cs handles collision, and Health.cs handles health logic.

## Improvement idea

If bullets do not hit any targets, they will exist in the scene permanently and waste game performance. We can set a fixed survival time for bullets to realize automatic timing destruction.

```c#
// Add in Projectile.cs
public float bulletLifetime = 3f;

private void Start()
{
    // Automatically destroy bullet after specified seconds
    Destroy(gameObject, bulletLifetime);
}
```

## Sources

Unity Manual: OnTriggerEnter2D

Unity Manual: OnTriggerStay2D

Unity Manual: OnCollisionEnter2D

Unity Manual: Event function execution order

Unity Scripting API: MonoBehaviour

## Reflection

The most important thing we learned is how Unity connects events, components, and custom logic.Collision detection is not just about checking for overlaps — it is the start of a chain that includes damage, health checks, and destruction.This shows how component-based design keeps our game logic clean and reusable.



