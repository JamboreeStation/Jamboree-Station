// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage.Components;
using Content.Shared.Weapons.Melee;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.IntegrationTests.Tests.Weapons;

public sealed class HeavyAttackTests : InteractionTest
{
    private const string WeaponPrototype = "HeavyAttackTestWeapon";
    private const string TargetPrototype = "HeavyAttackTestTarget";

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: HeavyAttackTestWeapon
  components:
  - type: Item
  - type: MeleeWeapon
    attackRate: 1
    canHeavyAttack: true
    damage:
      types:
        Blunt: 10

- type: entity
  id: HeavyAttackTestTarget
  components:
  - type: Damageable
    damageContainer: Inorganic
  - type: Physics
    bodyType: Static
  - type: Fixtures
    fixtures:
      target:
        shape:
          !type:PhysShapeCircle
          radius: 0.2
        hard: false
        layer:
        - MobLayer
";

    [Test]
    public async Task HeavyAttackRepeatedTargetDamagesOnce()
    {
        var weapon = ToServer(await PlaceInHands(WeaponPrototype));
        await SetCombatMode(true);
        var target = ToServer(await SpawnTarget(TargetPrototype));

        await Server.WaitAssertion(() =>
        {
            var damage = SEntMan.GetComponent<DamageableComponent>(target);
            Assert.That(damage.TotalDamage.Value, Is.Zero);

            Attack(weapon, target);
            var baseline = damage.TotalDamage;
            Assert.That(baseline.Value, Is.GreaterThan(0), "A normal heavy attack must actually deal damage.");

            Attack(weapon, target, target, target);
            Assert.That(damage.TotalDamage - baseline, Is.EqualTo(baseline),
                "Repeating a target in one wideswing must not apply its damage multiple times.");
        });
    }

    [Test]
    public async Task HeavyAttackDistinctTargetsEachTakeNormalDamage()
    {
        var weapon = ToServer(await PlaceInHands(WeaponPrototype));
        await SetCombatMode(true);
        var first = ToServer(await SpawnTarget(TargetPrototype));
        var second = ToServer(await Spawn(TargetPrototype));

        await Server.WaitAssertion(() =>
        {
            var firstDamage = SEntMan.GetComponent<DamageableComponent>(first);
            var secondDamage = SEntMan.GetComponent<DamageableComponent>(second);
            Assert.That(firstDamage.TotalDamage.Value, Is.Zero);
            Assert.That(secondDamage.TotalDamage.Value, Is.Zero);

            Attack(weapon, first);
            var baseline = firstDamage.TotalDamage;
            Assert.That(baseline.Value, Is.GreaterThan(0), "A normal heavy attack must actually deal damage.");

            Attack(weapon, first, second);
            Assert.Multiple(() =>
            {
                Assert.That(firstDamage.TotalDamage - baseline, Is.EqualTo(baseline));
                Assert.That(secondDamage.TotalDamage, Is.EqualTo(baseline),
                    "Deduplication must preserve distinct entities, even at the same coordinates.");
            });
        });
    }

    [Test]
    public async Task HeavyAttackEmptyAndOutOfRangeTargetsDoNotDamage()
    {
        var weapon = ToServer(await PlaceInHands(WeaponPrototype));
        await SetCombatMode(true);
        var target = ToServer(await SpawnTarget(TargetPrototype));

        await Server.WaitAssertion(() =>
        {
            var farTarget = SEntMan.SpawnEntity(TargetPrototype,
                SEntMan.GetCoordinates(TargetCoords).Offset(new Vector2(10, 0)));
            var damage = SEntMan.GetComponent<DamageableComponent>(target);
            var farDamage = SEntMan.GetComponent<DamageableComponent>(farTarget);

            Attack(weapon, target);
            var baseline = damage.TotalDamage;
            Assert.That(baseline.Value, Is.GreaterThan(0), "Establish that this attacker can land a heavy attack.");

            // Exercise both an empty input and a nonempty input that server range validation removes.
            Attack(weapon);
            Assert.That(damage.TotalDamage, Is.EqualTo(baseline));
            Assert.That(farDamage.TotalDamage.Value, Is.Zero);

            Attack(weapon, farTarget, farTarget);
            Assert.That(damage.TotalDamage, Is.EqualTo(baseline));
            Assert.That(farDamage.TotalDamage.Value, Is.Zero);
        });
    }

    [Test]
    public async Task HeavyAttackLimitsDistinctTargetsToMaxTargets()
    {
        var weapon = ToServer(await PlaceInHands(WeaponPrototype));
        await SetCombatMode(true);
        var baselineTarget = ToServer(await SpawnTarget(TargetPrototype));
        var targets = new EntityUid[SharedMeleeWeaponSystem.MaxTargets + 1];
        for (var i = 0; i < targets.Length; i++)
        {
            targets[i] = ToServer(await Spawn(TargetPrototype));
        }

        await Server.WaitAssertion(() =>
        {
            Attack(weapon, baselineTarget);
            var baseline = SEntMan.GetComponent<DamageableComponent>(baselineTarget).TotalDamage;
            Assert.That(baseline.Value, Is.GreaterThan(0), "A normal heavy attack must actually deal damage.");

            Attack(weapon, targets);
            var damaged = 0;
            foreach (var target in targets)
            {
                var damage = SEntMan.GetComponent<DamageableComponent>(target).TotalDamage;
                if (damage.Value == 0)
                    continue;

                Assert.That(damage, Is.EqualTo(baseline), "Every hit must deal one normal heavy attack's damage.");
                damaged++;
            }

            Assert.That(damaged, Is.EqualTo(SharedMeleeWeaponSystem.MaxTargets),
                "The server must cap distinct targets without depending on set iteration order.");
        });
    }

    [Test]
    public async Task HeavyAttackTargetSetDeserializationCollapsesDuplicateEntries()
    {
        await Server.WaitAssertion(() =>
        {
            var serializer = Server.ResolveDependency<IRobustSerializer>();
            var first = new NetEntity(1);
            var second = new NetEntity(2);
            using var stream = new MemoryStream();

            // NetSerializer's HashSetSerializer payload is a type-tagged array, not a direct array.
            // Encode duplicates directly so a HashSet cannot remove them before they reach the reader.
            serializer.Serialize(stream, new[] { first, first, second, first, second });
            stream.Position = 0;
            serializer.DeserializeDirect<HashSet<NetEntity>>(stream, out var targets);

            Assert.That(targets, Is.EquivalentTo(new[] { first, second }));
            Assert.That(stream.Position, Is.EqualTo(stream.Length), "The reader must consume every encoded entry.");
        });
    }

    private void Attack(EntityUid weaponUid, params EntityUid[] targets)
    {
        var melee = SEntMan.System<SharedMeleeWeaponSystem>();
        var weapon = SEntMan.GetComponent<MeleeWeaponComponent>(weaponUid);

        // Bypass pickup/previous-attack cooldowns, with exactly one swing due at the current time.
        weapon.NextAttack = STiming.CurTime;
        Assert.That(melee.AttemptHeavyAttack(SPlayer, weaponUid, weapon,
            new List<EntityUid>(targets), SEntMan.GetCoordinates(TargetCoords)), Is.True,
            "The attack must be accepted, not silently rejected by combat mode or cooldown checks.");
        Assert.That(weapon.NextAttack, Is.GreaterThan(STiming.CurTime));
    }
}
