using Photon.Deterministic;
using Quantum.Physics3D;

namespace Quantum
{
    /// <summary>
    /// Dipping and sauce effects (docs/PLAN.md, Sauce system). A block reaching into a vat's sauce is dipped: the sauce
    /// surface is recorded as a plane in the block's local space, keeping the deepest the block went before it came
    /// out, up to three dips per block (a fourth replaces the shallowest). A sauce's strength is its deepest dip's
    /// fraction over SauceConfig.FullStrengthFraction. The two sauces are opposites, both through the block's physics
    /// material: Stack sauce makes it sticky, so it grips the ground instead of sliding and glues itself to the blocks it
    /// touches (a stiff joint each) until a hard hit or a grab tears it free; Slip sauce makes it slippery, so what rests
    /// on it, or it on, slides off. Where a block has both, the stronger wins by the difference.
    /// </summary>
    public unsafe class SauceSystem : SystemMainThread, ISignalOnCollisionEnter3D
    {
        /// <summary>
        /// The user tag of a glue joint, apart from the grab's <see cref="EGrabJointTag"/> values.
        /// </summary>
        public const int GlueJointTag = 16;

        private const int MaxDips = 3;

        public override void Update(Frame frame)
        {
            SauceConfig config = GetConfig(frame);
            if (config == null)
                return;

            DipBlocksInVats(frame, config);
            EndDips(frame, config);
            RemoveBrokenGlue(frame, config);
            GlueTouchingBlocks(frame, config);
        }

        /// <summary>
        /// Tears a glued block free of the blocks it is glued to when it is hit hard enough for the glue's strength.
        /// Joints are only disabled here, inside the physics step; <see cref="RemoveBrokenGlue"/> removes them.
        /// </summary>
        void ISignalOnCollisionEnter3D.OnCollisionEnter3D(Frame frame, CollisionInfo3D info)
        {
            if (frame.Has<Block>(info.Entity) == false
                || frame.Unsafe.TryGetPointer(info.Entity, out PhysicsBody3D* body) == false)
                return;

            SauceConfig config = GetConfig(frame);
            if (config == null)
                return;

            FPVector3 otherVelocity = info.IsStatic == false
                && frame.Unsafe.TryGetPointer(info.Other, out PhysicsBody3D* otherBody) ? otherBody->Velocity : FPVector3.Zero;
            FP intoSpeed = FPMath.Abs(FPVector3.Dot(body->Velocity - otherVelocity, info.ContactNormal));
            if (intoSpeed < config.GlueMinBreakSpeed)
                return;

            var coats = frame.Filter<SauceCoat, PhysicsJoints3D>();
            while (coats.NextUnsafe(out EntityRef owner, out SauceCoat* coat, out PhysicsJoints3D* joints))
            {
                FP breakSpeed = FPMath.Lerp(config.GlueMinBreakSpeed, config.GlueMaxBreakSpeed,
                    FPMath.Clamp01(coat->StackStrength - coat->SlipStrength));
                if (intoSpeed < breakSpeed || joints->TryGetJoints(frame, out Joint3D* buffer, out int count) == false)
                    continue;

                for (int i = 0; i < count; i++)
                {
                    if (buffer[i].UserTag == GlueJointTag && (owner == info.Entity || buffer[i].ConnectedEntity == info.Entity))
                    {
                        buffer[i].Enabled = false;
                    }
                }
            }
        }

        private static SauceConfig GetConfig(Frame frame)
        {
            if (frame.TryGetSingleton(out Match match) == false)
                return null;

            return frame.FindAsset(frame.FindAsset(match.Mode).Sauce);
        }

        /// <summary>
        /// Removes glue that is torn (disabled by a hit, or pulled further apart than the glue stretches), that a pickup
        /// pulls apart (either block held), that has lost its sauce (Slip now stronger), or whose other block is gone.
        /// </summary>
        private static void RemoveBrokenGlue(Frame frame, SauceConfig config)
        {
            var coats = frame.Filter<SauceCoat, PhysicsJoints3D>();
            while (coats.NextUnsafe(out EntityRef block, out SauceCoat* coat, out PhysicsJoints3D* joints))
            {
                if (joints->TryGetJoints(frame, out Joint3D* buffer, out int count) == false)
                    continue;

                bool keep = coat->StackStrength > coat->SlipStrength && IsHeld(frame, block) == false;
                for (int i = count - 1; i >= 0; i--)
                {
                    if (buffer[i].UserTag != GlueJointTag)
                        continue;

                    EntityRef other = buffer[i].ConnectedEntity;
                    bool otherGone = other != EntityRef.None && (frame.Exists(other) == false || IsHeld(frame, other));
                    if (keep == false || buffer[i].Enabled == false || otherGone
                        || GetStretch(frame, block, buffer[i]) > config.GlueTearDistance)
                    {
                        joints->RemoveAt(frame, i);
                    }
                }
            }
        }

        /// <summary>
        /// Glues each loose Stack-sauced block, nearly at rest, to the ground or anything static it touches, and to every
        /// block touching it that isn't already glued to it and is nearly at rest against it.
        /// </summary>
        private static void GlueTouchingBlocks(Frame frame, SauceConfig config)
        {
            var coats = frame.Filter<SauceCoat, Transform3D, PhysicsBody3D, PhysicsCollider3D>();
            while (coats.NextUnsafe(out EntityRef block, out SauceCoat* coat, out Transform3D* transform,
                       out PhysicsBody3D* body, out PhysicsCollider3D* collider))
            {
                if (coat->StackStrength <= coat->SlipStrength || IsHeld(frame, block))
                    continue;

                FP maxSpeedSqr = config.GlueMaxSpeed * config.GlueMaxSpeed;
                Shape3D reach = Shape3D.CreateBox(collider->Shape.Box.Extents + FPVector3.One * config.GlueReach);
                HitCollection3D hits = frame.Physics3D.OverlapShape(transform->Position, transform->Rotation, reach, -1,
                    QueryOptions.HitStatics | QueryOptions.HitDynamics | QueryOptions.HitKinematics);
                for (int i = 0; i < hits.Count; i++)
                {
                    if (hits[i].IsTrigger)
                        continue;

                    if (hits[i].IsStatic)
                    {
                        if (body->Velocity.SqrMagnitude <= maxSpeedSqr && IsGlued(frame, block, EntityRef.None) == false)
                        {
                            Glue(frame, config, block, *transform, EntityRef.None, transform->Position);
                        }

                        continue;
                    }

                    EntityRef other = hits[i].Entity;
                    if (other == block || frame.Has<Block>(other) == false || IsHeld(frame, other)
                        || IsGlued(frame, block, other) || IsGlued(frame, other, block))
                        continue;

                    if ((frame.Get<PhysicsBody3D>(other).Velocity - body->Velocity).SqrMagnitude > maxSpeedSqr)
                        continue;

                    FPVector3 otherPosition = frame.Get<Transform3D>(other).Position;
                    Glue(frame, config, block, *transform, other, (transform->Position + otherPosition) / 2);
                }
            }
        }

        /// <summary>
        /// Joins a block where it is to another block, or to the world when <paramref name="other"/> is none, with a
        /// joint at <paramref name="contact"/> (for two blocks, the midpoint between their centers, on the face they
        /// share) that holds them together and lets them bend only by <see cref="SauceConfig.GlueAngleDegrees"/>.
        /// </summary>
        private static void Glue(Frame frame, SauceConfig config, EntityRef block, Transform3D transform, EntityRef other,
            FPVector3 contact)
        {
            FPQuaternion inverse = FPQuaternion.Inverse(transform.Rotation);
            FPVector3 twist = inverse * (contact - transform.Position);
            twist = twist.SqrMagnitude > FP._0 ? twist.Normalized : FPQuaternion.Inverse(transform.Rotation) * FPVector3.Down;
            FPVector3 swing = FPVector3.Cross(twist, FPMath.Abs(twist.Y) < FP._0_50 ? FPVector3.Up : FPVector3.Right).Normalized;
            FP limit = config.GlueAngleDegrees * FP.Deg2Rad;

            // A world joint's connected anchor is a world position; a block's is in that block's space.
            FPVector3 connectedAnchor = other == EntityRef.None
                ? contact
                : FPQuaternion.Inverse(frame.Get<Transform3D>(other).Rotation) * (contact - frame.Get<Transform3D>(other).Position);
            Joint3D joint = CharacterJoint3D.CreateJoint(frame, block, twist, swing, -limit, limit, limit, limit, other,
                inverse * (contact - transform.Position), connectedAnchor);
            joint.UserTag = GlueJointTag;

            if (frame.Has<PhysicsJoints3D>(block) == false)
            {
                frame.Add<PhysicsJoints3D>(block);
            }

            frame.Unsafe.GetPointer<PhysicsJoints3D>(block)->AddJoint(frame, ref joint);
        }

        /// <summary>
        /// Frees a block of all glue, its own and other blocks' glue to it. Call it before moving a block by hand
        /// (a respawn, a dev command): physics runs before this system, so the glue would snap a moved block back first.
        /// Not safe inside physics callbacks.
        /// </summary>
        public static void ReleaseGlue(Frame frame, EntityRef block)
        {
            var coats = frame.Filter<SauceCoat, PhysicsJoints3D>();
            while (coats.NextUnsafe(out EntityRef owner, out SauceCoat* _, out PhysicsJoints3D* joints))
            {
                if (joints->TryGetJoints(frame, out Joint3D* buffer, out int count) == false)
                    continue;

                for (int i = count - 1; i >= 0; i--)
                {
                    if (buffer[i].UserTag == GlueJointTag && (owner == block || buffer[i].ConnectedEntity == block))
                    {
                        joints->RemoveAt(frame, i);
                    }
                }
            }
        }

        /// <summary>
        /// Returns how far apart, in meters, a glue joint's two anchor points are in the world.
        /// </summary>
        private static FP GetStretch(Frame frame, EntityRef block, Joint3D joint)
        {
            Transform3D transform = frame.Get<Transform3D>(block);
            FPVector3 anchor = transform.Position + transform.Rotation * joint.Anchor;
            FPVector3 connected = joint.ConnectedAnchor;
            if (joint.ConnectedEntity != EntityRef.None && frame.TryGet(joint.ConnectedEntity, out Transform3D other))
            {
                connected = other.Position + other.Rotation * joint.ConnectedAnchor;
            }

            return FPVector3.Distance(anchor, connected);
        }

        /// <summary>
        /// Returns whether <paramref name="block"/> carries a glue joint to <paramref name="other"/>.
        /// </summary>
        private static bool IsGlued(Frame frame, EntityRef block, EntityRef other)
        {
            if (frame.Unsafe.TryGetPointer(block, out PhysicsJoints3D* joints) == false
                || joints->TryGetJoints(frame, out Joint3D* buffer, out int count) == false)
                return false;

            for (int i = 0; i < count; i++)
            {
                if (buffer[i].UserTag == GlueJointTag && buffer[i].ConnectedEntity == other)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns how many blocks <paramref name="block"/> carries glue joints to (glue to it from other Stack-sauced
        /// blocks is on them).
        /// </summary>
        public static int GetGlueCount(Frame frame, EntityRef block)
        {
            if (frame.Unsafe.TryGetPointer(block, out PhysicsJoints3D* joints) == false
                || joints->TryGetJoints(frame, out Joint3D* buffer, out int count) == false)
                return 0;

            int glued = 0;
            for (int i = 0; i < count; i++)
            {
                if (buffer[i].UserTag == GlueJointTag)
                {
                    glued++;
                }
            }

            return glued;
        }

        private static bool IsHeld(Frame frame, EntityRef block)
        {
            return frame.TryGet(block, out Grabbable grabbable) && grabbable.Holder != EntityRef.None;
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
        /// Sets each sauce's strength from its deepest dip, and gives the block the sticky (Stack) or slippery (Slip)
        /// material for whichever sauce is stronger, stepped by how much stronger, or its own material back.
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

            FP net = stack - slip;
            AssetRef<PhysicsMaterial>[] materials = net > FP._0 ? config.StackMaterials : config.SlipMaterials;
            PhysicsCollider3D* collider = frame.Unsafe.GetPointer<PhysicsCollider3D>(block);
            collider->Material = GetSteppedMaterial(materials, FPMath.Abs(net), coat->PlainMaterial);
        }

        /// <summary>
        /// Returns the material for <paramref name="strength"/> from weakest-to-strongest steps, or
        /// <paramref name="plain"/> at no strength.
        /// </summary>
        private static AssetRef<PhysicsMaterial> GetSteppedMaterial(AssetRef<PhysicsMaterial>[] steps, FP strength,
            AssetRef<PhysicsMaterial> plain)
        {
            int count = steps != null ? steps.Length : 0;
            if (strength <= FP._0 || count == 0)
                return plain;

            int step = System.Math.Min(System.Math.Max(FPMath.CeilToInt(strength * count) - 1, 0), count - 1);
            return steps[step];
        }
    }
}
