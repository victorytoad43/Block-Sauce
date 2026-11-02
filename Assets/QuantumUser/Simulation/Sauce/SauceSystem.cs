using Photon.Deterministic;
using Quantum.Physics3D;

namespace Quantum
{
    /// <summary>
    /// Dipping and sauce effects (docs/PLAN.md, Sauce system). A block reaching into a vat's sauce is dipped: the sauce
    /// surface is recorded as a plane in the block's local space, keeping the deepest the block went before it came
    /// out, up to three dips per block (a fourth replaces the shallowest). A sauce's strength is its deepest dip's
    /// fraction over SauceConfig.FullStrengthFraction. Stack sauce: a stacked block shoves rivals away from its tower.
    /// Slip sauce: the block takes a low-friction physics material, so what rests on it, or it on, slides off.
    /// </summary>
    public unsafe class SauceSystem : SystemMainThread
    {
        private const int MaxDips = 3;

        public override void Update(Frame frame)
        {
            if (frame.TryGetSingleton(out Match match) == false)
                return;

            SauceConfig config = frame.FindAsset(frame.FindAsset(match.Mode).Sauce);
            if (config == null)
                return;

            DipBlocksInVats(frame, config);
            EndDips(frame, config);
            ShoveRivals(frame, config);
        }

        /// <summary>
        /// Finds the blocks reaching into each vat's sauce and records or deepens their dip there.
        /// </summary>
        private static void DipBlocksInVats(Frame frame, SauceConfig config)
        {
            var vats = frame.Filter<SauceVat, Transform3D>();
            while (vats.NextUnsafe(out EntityRef vat, out SauceVat* sauce, out Transform3D* vatTransform))
            {
                FP halfHeight = sauce->SurfaceHeight / 2;
                FPVector3 center = vatTransform->Position + vatTransform->Rotation * (FPVector3.Up * halfHeight);
                Shape3D volume = Shape3D.CreateBox(new FPVector3(sauce->Size.X / 2, halfHeight, sauce->Size.Y / 2));
                FP surfaceY = vatTransform->Position.Y + sauce->SurfaceHeight;

                HitCollection3D hits = frame.Physics3D.OverlapShape(center, vatTransform->Rotation, volume, -1,
                    QueryOptions.HitDynamics | QueryOptions.HitKinematics);
                for (int i = 0; i < hits.Count; i++)
                {
                    EntityRef block = hits[i].Entity;
                    if (hits[i].IsTrigger || frame.Has<Block>(block) == false)
                        continue;

                    Dip(frame, config, block, vat, sauce->Type, surfaceY);
                }
            }
        }

        /// <summary>
        /// Records this tick of a block's dip: a new dip if it just entered the vat, otherwise the deepest plane so far.
        /// </summary>
        private static void Dip(Frame frame, SauceConfig config, EntityRef block, EntityRef vat, SauceType type, FP surfaceY)
        {
            if (frame.Unsafe.TryGetPointer(block, out SauceCoat* coat) == false)
            {
                frame.Add(block, new SauceCoat { PlainMaterial = frame.Get<PhysicsCollider3D>(block).Material });
                coat = frame.Unsafe.GetPointer<SauceCoat>(block);
            }

            if (coat->InVatFrame == frame.Number)
                return;

            Transform3D* transform = frame.Unsafe.GetPointer<Transform3D>(block);
            FPVector3 normal = FPQuaternion.Inverse(transform->Rotation) * FPVector3.Up;
            FP offset = surfaceY - transform->Position.Y;
            FP fraction = GetDippedFraction(frame.Get<PhysicsCollider3D>(block).Shape.Box.Extents, normal, offset);
            SauceDip dip = new SauceDip { Normal = normal, Offset = offset, Fraction = fraction, Type = type };

            FixedArray<SauceDip> dips = coat->Dips;
            if (coat->InVat != vat)
            {
                coat->InVat = vat;
                coat->ActiveDip = (byte)(coat->DipCount < MaxDips ? coat->DipCount++ : GetShallowestDip(coat));
                dips[coat->ActiveDip] = dip;
                frame.Events.BlockDipStarted(block, vat, type);
            }
            else if (fraction >= dips[coat->ActiveDip].Fraction)
            {
                dips[coat->ActiveDip] = dip;
            }

            coat->InVatFrame = frame.Number;
        }

        /// <summary>
        /// Ends the dip of every block that has been out of its vat for DipEndSeconds (so an edge bobbing across the
        /// surface stays one dip): a graze is dropped, and the sauces' strengths
        /// and the block's physics material follow the new coat.
        /// </summary>
        private static void EndDips(Frame frame, SauceConfig config)
        {
            var coats = frame.Filter<SauceCoat>();
            while (coats.NextUnsafe(out EntityRef block, out SauceCoat* coat))
            {
                if (coat->InVat == EntityRef.None
                    || frame.Number - coat->InVatFrame <= config.DipEndSeconds * frame.SessionConfig.UpdateFPS)
                    continue;

                coat->InVat = EntityRef.None;
                FixedArray<SauceDip> dips = coat->Dips;
                SauceDip dip = dips[coat->ActiveDip];
                if (dip.Fraction < config.MinDipFraction)
                {
                    RemoveDip(coat, coat->ActiveDip);
                }
                else
                {
                    frame.Events.BlockDipEnded(block, dip.Type, coat->ActiveDip, dip.Fraction);
                }

                UpdateStrengths(frame, config, block, coat);
            }
        }

        /// <summary>
        /// Shoves every rival standing near a stacked Stack-sauced block away from it, harder the stronger the sauce,
        /// then waits out the cooldown before that block shoves again.
        /// </summary>
        private static void ShoveRivals(Frame frame, SauceConfig config)
        {
            var coats = frame.Filter<SauceCoat, Block, Transform3D>();
            while (coats.NextUnsafe(out EntityRef block, out SauceCoat* coat, out Block* owned, out Transform3D* transform))
            {
                if (coat->StackStrength <= FP._0 || owned->Stack == StackState.Loose || owned->Owner.IsValid == false
                    || coat->StackCooldown.IsRunning(frame))
                    continue;

                byte team = frame.TryGet(block, out Team blockTeam) ? blockTeam.Id : TeamIds.None;
                FP speedChange = FPMath.Lerp(config.StackMinSpeedChange, config.StackMaxSpeedChange, coat->StackStrength);
                bool shoved = false;

                var players = frame.Filter<Player, Transform3D, PhysicsBody3D>();
                while (players.NextUnsafe(out EntityRef player, out Player* _, out Transform3D* playerTransform,
                           out PhysicsBody3D* body))
                {
                    if (frame.TryGet(player, out Team playerTeam) && playerTeam.Id == team)
                        continue;

                    FPVector3 away = playerTransform->Position - transform->Position;
                    away.Y = FP._0;
                    if (away.SqrMagnitude > config.StackRadius * config.StackRadius || frame.Has<RagdollOwner>(player))
                        continue;

                    away = away.SqrMagnitude > FP._0 ? away.Normalized : FPVector3.Forward;
                    FPVector3 impulse = (away + FPVector3.Up * config.StackUpBias) * (speedChange * body->Mass);
                    frame.Signals.ApplyKnockback(player, impulse, block);
                    shoved = true;
                }

                if (shoved)
                {
                    coat->StackCooldown = FrameTimer.FromSeconds(frame, config.StackCooldownSeconds);
                    frame.Events.StackSaucePulse(block, transform->Position, config.StackRadius, coat->StackStrength);
                }
            }
        }

        /// <summary>
        /// Coats a block as if it had just been dipped level to <paramref name="fraction"/> of its height, through the
        /// same coat, strength and event path as a real dip. For dev tools and modes that hand out sauced blocks.
        /// </summary>
        public static void ApplyDip(Frame frame, SauceConfig config, EntityRef block, SauceType type, FP fraction)
        {
            if (frame.Unsafe.TryGetPointer(block, out SauceCoat* coat) == false)
            {
                frame.Add(block, new SauceCoat { PlainMaterial = frame.Get<PhysicsCollider3D>(block).Material });
                coat = frame.Unsafe.GetPointer<SauceCoat>(block);
            }

            FPVector3 normal = FPQuaternion.Inverse(frame.Get<Transform3D>(block).Rotation) * FPVector3.Up;
            FPVector3 extents = frame.Get<PhysicsCollider3D>(block).Shape.Box.Extents;
            FP reach = FPMath.Abs(normal.X) * extents.X + FPMath.Abs(normal.Y) * extents.Y + FPMath.Abs(normal.Z) * extents.Z;
            fraction = FPMath.Clamp01(fraction);

            int slot = coat->DipCount < MaxDips ? coat->DipCount++ : GetShallowestDip(coat);
            FixedArray<SauceDip> dips = coat->Dips;
            dips[slot] = new SauceDip { Normal = normal, Offset = (2 * fraction - 1) * reach, Fraction = fraction, Type = type };
            frame.Events.BlockDipEnded(block, type, (byte)slot, fraction);
            UpdateStrengths(frame, config, block, coat);
        }

        /// <summary>
        /// Returns the share of a box's height, measured along <paramref name="normal"/>, below the plane
        /// Dot(normal, p) = <paramref name="offset"/>.
        /// </summary>
        private static FP GetDippedFraction(FPVector3 extents, FPVector3 normal, FP offset)
        {
            FP reach = FPMath.Abs(normal.X) * extents.X + FPMath.Abs(normal.Y) * extents.Y + FPMath.Abs(normal.Z) * extents.Z;
            return reach > FP._0 ? FPMath.Clamp01((offset + reach) / (2 * reach)) : FP._0;
        }

        private static int GetShallowestDip(SauceCoat* coat)
        {
            FixedArray<SauceDip> dips = coat->Dips;
            int shallowest = 0;
            for (int i = 1; i < coat->DipCount; i++)
            {
                if (dips[i].Fraction < dips[shallowest].Fraction)
                {
                    shallowest = i;
                }
            }

            return shallowest;
        }

        private static void RemoveDip(SauceCoat* coat, int index)
        {
            FixedArray<SauceDip> dips = coat->Dips;
            for (int i = index; i < coat->DipCount - 1; i++)
            {
                dips[i] = dips[i + 1];
            }

            coat->DipCount--;
            dips[coat->DipCount] = default;
        }

        /// <summary>
        /// Sets each sauce's strength from its deepest dip, and gives the block the Slip material for that strength (or
        /// its own material back).
        /// </summary>
        private static void UpdateStrengths(Frame frame, SauceConfig config, EntityRef block, SauceCoat* coat)
        {
            FP stack = FP._0;
            FP slip = FP._0;
            FixedArray<SauceDip> dips = coat->Dips;
            for (int i = 0; i < coat->DipCount; i++)
            {
                FP strength = FPMath.Clamp01(dips[i].Fraction / config.FullStrengthFraction);
                if (dips[i].Type == SauceType.Stack)
                {
                    stack = FPMath.Max(stack, strength);
                }
                else if (dips[i].Type == SauceType.Slip)
                {
                    slip = FPMath.Max(slip, strength);
                }
            }

            coat->StackStrength = stack;
            coat->SlipStrength = slip;

            PhysicsCollider3D* collider = frame.Unsafe.GetPointer<PhysicsCollider3D>(block);
            int steps = config.SlipMaterials != null ? config.SlipMaterials.Length : 0;
            if (slip <= FP._0 || steps == 0)
            {
                collider->Material = coat->PlainMaterial;
                return;
            }

            int step = System.Math.Min(System.Math.Max(FPMath.CeilToInt(slip * steps) - 1, 0), steps - 1);
            collider->Material = config.SlipMaterials[step];
        }
    }
}
