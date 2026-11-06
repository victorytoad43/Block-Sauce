using System.Collections.Generic;
using Photon.Deterministic;
using Quantum.Physics3D;

namespace Quantum
{
    /// <summary>
    /// One scripted placing on the block bench: a tower of <see cref="Base"/> blocks, then a top block let go on it,
    /// either appearing at the let-go spot or carried in the way a player's hold carries it.
    /// </summary>
    public struct BlockBenchTrial
    {
        /// <summary>Blocks already in the tower before the top block comes (1 to 4).</summary>
        public int Base;
        /// <summary>Base blocks placed one by one through the place code (true) or spawned exactly at rest (false).</summary>
        public bool BuildBase;
        /// <summary>How far each base block above the first is off the one below (meters, x and z).</summary>
        public FPVector2 BaseOffset;
        /// <summary>Seconds after the base is up before the top block comes (counting takes 3 s; counted blocks sleep).</summary>
        public FP BaseWait;
        /// <summary>Wake the base's bodies just before the top block comes.</summary>
        public bool WakeBase;
        /// <summary>The top block's let-go spot, off the tower's top center (meters, x and z).</summary>
        public FPVector2 Offset;
        /// <summary>Degrees the top block is turned from the tower about up.</summary>
        public FP Yaw;
        /// <summary>Let-go height above resting on the tower (meters).</summary>
        public FP Gap;
        /// <summary>0: the top block appears at the let-go spot. Otherwise it is held (no gravity, velocity chase like
        /// a player's hold) and carried in from 1.2 m to the side at this speed (m/s).</summary>
        public FP CarrySpeed;
        /// <summary>While carried: how far below the let-go spot the hand aims at the end (meters), pressing the block
        /// into the tower the way a player holding it too low does.</summary>
        public FP Press;
        /// <summary>While carried: seconds held at the end before letting go.</summary>
        public FP HoldSeconds;
        /// <summary>The place's squaring and lift onto the block below (<c>SettlePlacedOnBlockBelow</c>).</summary>
        public bool Square;
        /// <summary>A sauce on every block, dipped to <see cref="SauceFraction"/> of its height.</summary>
        public SauceType Sauce;
        public FP SauceFraction;
        /// <summary>Seconds recorded after the let-go.</summary>
        public FP Record;
        /// <summary>Above 0: after recording, a block flies into the tower at this speed (m/s) at
        /// <see cref="HitLevel"/>, and the fall is recorded for 3 s more.</summary>
        public FP HitSpeed;
        public int HitLevel;
        /// <summary>Where along the tower's face the hit lands, sideways from the middle (meters).</summary>
        public FP HitSide;
        /// <summary>Keeps every bench block awake (no sleeping, including the counted-block sleep).</summary>
        public bool NoSleep;
        /// <summary>Above 0: every bench block's angular drag (the block's own is 0.5).</summary>
        public FP AngularDrag;

        /// <summary>
        /// A third block let go squarely 2 cm above a two-block tower built through the place code, after the tower has
        /// counted, recorded for 5 s.
        /// </summary>
        public static BlockBenchTrial Default => new BlockBenchTrial
        {
            Base = 2,
            BuildBase = true,
            BaseWait = 5,
            Gap = (FP)2 / 100,
            HoldSeconds = FP._0_25,
            Square = true,
            SauceFraction = FP._0_75,
            Record = 5,
            HitLevel = 1,
        };
    }

    /// <summary>
    /// Test-only (Blocks phase): runs the trials an editor script puts in <see cref="Trials"/> at the BlockBench entity
    /// in the BlockBench dev scene, one after another, and reports each with a <see cref="EventBlockBenchTrialEnded"/>.
    /// Placing goes through <see cref="GrabSystem.SetDown"/> and <see cref="GrabSystem.DetachHeld"/>, the code a player's
    /// place runs. The bench sets the match playing so stacks settle, count and sleep as in a match (its mode needs a
    /// tower too tall to win). Does nothing without a BlockBench entity or trials.
    /// </summary>
    public unsafe class BlockBenchSystem : SystemMainThreadFilter<BlockBenchSystem.Filter>
    {
        /// <summary>The trials to run (editor scripts set it; read every tick, so it must not change mid-run).</summary>
        public static List<BlockBenchTrial> Trials;
        /// <summary>Set to start <see cref="Trials"/> from the first; the bench clears it.</summary>
        public static bool Restart;

        private const int Idle = 0, Build = 1, Wait = 2, Carry = 3, Record = 4, Hit = 5;
        private static readonly FP BuildInterval = (FP)6 / 10;
        private static readonly FP CarryDistance = (FP)12 / 10;
        private static readonly FP HitSeconds = 3;
        private static readonly FP FallenSlide = (FP)4 / 10;
        private static readonly FP FallenDrop = (FP)2 / 10;

        public struct Filter
        {
            public EntityRef Entity;
            public Transform3D* Transform;
            public BlockBench* Bench;
        }

        public override void Update(Frame frame, ref Filter filter)
        {
            if (Trials == null)
                return;

            BlockBench* bench = filter.Bench;
            if (Restart)
            {
                Restart = false;
                bench->Trial = 0;
                BeginTrial(frame, ref filter);
            }

            if (bench->Step == Idle || bench->Trial >= Trials.Count)
                return;

            KeepPlaying(frame);
            BlockBenchTrial trial = Trials[bench->Trial];
            bench->StepTime += frame.DeltaTime;

            switch (bench->Step)
            {
                case Build: UpdateBuild(frame, ref filter, trial); break;
                case Wait: UpdateWait(frame, ref filter, trial); break;
                case Carry: UpdateCarry(frame, ref filter, trial); break;
                case Record: UpdateRecord(frame, ref filter, trial); break;
                case Hit: UpdateHit(frame, ref filter, trial); break;
            }
        }

        private static void BeginTrial(Frame frame, ref Filter filter)
        {
            BlockBench* bench = filter.Bench;
            for (int i = 0; i < bench->Blocks.Length; i++)
            {
                if (frame.Exists(bench->Blocks[i]))
                {
                    frame.Destroy(bench->Blocks[i]);
                }

                bench->Blocks[i] = EntityRef.None;
            }

            FPVector3 origin = filter.Transform->Position;
            Hit3D? ground = frame.Physics3D.Raycast(origin + FPVector3.Up * 5, FPVector3.Down, 20, -1, QueryOptions.HitStatics);
            bench->GroundY = ground.HasValue ? ground.Value.Point.Y : origin.Y;
            bench->Placed = 0;
            bench->Pushed = FP._0;
            bench->BaseSlide = FP._0;
            bench->TopSlide = FP._0;
            bench->MaxTilt = FP._0;
            bench->TopSpeed = FP._0;
            bench->FallenBeforeHit = 0;
            SetStep(bench, Build);
        }

        private static void UpdateBuild(Frame frame, ref Filter filter, BlockBenchTrial trial)
        {
            BlockBench* bench = filter.Bench;
            while (bench->Placed < trial.Base)
            {
                int i = bench->Placed;
                if (trial.BuildBase && i > 0 && bench->StepTime < BuildInterval * i)
                    return;

                FPVector3 position = TowerPoint(frame, ref filter, i) + Offset3(trial.BaseOffset) * i;
                EntityRef block = Spawn(frame, ref filter, trial, i, position, FPQuaternion.Identity);
                if (trial.BuildBase && i > 0)
                {
                    Transform3D* transform = frame.Unsafe.GetPointer<Transform3D>(block);
                    transform->Position += FPVector3.Up * trial.Gap;
                    GrabSystem.SetDown(frame, block, trial.Square);
                }

                bench->Placed++;
            }

            SetStep(bench, Wait);
        }

        private static void UpdateWait(Frame frame, ref Filter filter, BlockBenchTrial trial)
        {
            BlockBench* bench = filter.Bench;
            if (bench->StepTime < trial.BaseWait)
                return;

            for (int i = 0; i < trial.Base; i++)
            {
                bench->Start[i] = frame.Get<Transform3D>(bench->Blocks[i]).Position;
                if (trial.WakeBase)
                {
                    frame.Unsafe.GetPointer<PhysicsBody3D>(bench->Blocks[i])->WakeUp();
                }
            }

            // The let-go spot: on the tower's actual top block, as a player lines up on what they see.
            Transform3D top = frame.Get<Transform3D>(bench->Blocks[trial.Base - 1]);
            FPVector3 target = top.Position + Offset3(trial.Offset) + FPVector3.Up * (BlockSize(frame, bench->Blocks[0]) + trial.Gap);
            FPQuaternion rotation = GetYaw(top.Rotation) * FPQuaternion.AngleAxis(trial.Yaw, FPVector3.Up);

            if (trial.CarrySpeed <= FP._0)
            {
                EntityRef block = Spawn(frame, ref filter, trial, trial.Base, target, rotation);
                LetGo(frame, ref filter, trial, block);
                return;
            }

            bench->CarryTo = target - FPVector3.Up * trial.Press;
            bench->CarryFrom = new FPVector3(target.X - CarryDistance, target.Y + FP._0_25, target.Z);
            EntityRef held = Spawn(frame, ref filter, trial, trial.Base, bench->CarryFrom, rotation);

            frame.Unsafe.GetPointer<Grabbable>(held)->Holder = filter.Entity;
            PhysicsBody3D* body = frame.Unsafe.GetPointer<PhysicsBody3D>(held);
            body->WakeUp();
            body->AllowSleeping = false;
            body->GravityScale = FP._0;
            SetStep(bench, Carry);
        }

        private static void UpdateCarry(Frame frame, ref Filter filter, BlockBenchTrial trial)
        {
            BlockBench* bench = filter.Bench;
            EntityRef held = bench->Blocks[trial.Base];
            GrabConfig config = frame.FindAsset(bench->Grab);

            FPVector3 path = bench->CarryTo - bench->CarryFrom;
            FP length = path.Magnitude;
            FP travelled = FPMath.Min(length, trial.CarrySpeed * bench->StepTime);
            FPVector3 hand = bench->CarryFrom + (length > FP._0 ? path / length * travelled : FPVector3.Zero);
            if (config.HoldRestsOnBlockBelow)
            {
                hand = GrabSystem.LimitToRestOnBlockBelow(frame, held, hand);
            }

            // The hold's chase (GrabSystem.UpdateHeld) with a carrier standing still.
            Transform3D* transform = frame.Unsafe.GetPointer<Transform3D>(held);
            PhysicsBody3D* body = frame.Unsafe.GetPointer<PhysicsBody3D>(held);
            FPVector3 velocity = (hand - transform->Position) * config.HoldFollowGain;
            if (velocity.SqrMagnitude > config.HoldMaxFollowSpeed * config.HoldMaxFollowSpeed)
            {
                velocity = velocity.Normalized * config.HoldMaxFollowSpeed;
            }

            body->Velocity = velocity;
            body->AngularVelocity = FPVector3.Zero;

            bench->Pushed = FPMath.Max(bench->Pushed, MaxSlide(frame, bench, 0, trial.Base));

            if (bench->StepTime >= length / trial.CarrySpeed + trial.HoldSeconds)
            {
                LetGo(frame, ref filter, trial, held);
            }
        }

        private static void LetGo(Frame frame, ref Filter filter, BlockBenchTrial trial, EntityRef block)
        {
            GrabSystem.SetDown(frame, block, trial.Square);
            GrabSystem.DetachHeld(frame, block);
            ApplyBodyOverrides(frame, block, trial);
            filter.Bench->Start[trial.Base] = frame.Get<Transform3D>(block).Position;
            SetStep(filter.Bench, Record);
        }

        private static void UpdateRecord(Frame frame, ref Filter filter, BlockBenchTrial trial)
        {
            BlockBench* bench = filter.Bench;
            Measure(frame, bench, trial);
            if (bench->StepTime < trial.Record)
                return;

            bench->FallenBeforeHit = CountFallen(frame, bench, trial);
            if (trial.HitSpeed <= FP._0)
            {
                Finish(frame, ref filter, trial);
                return;
            }

            // A block flying level into the tower from 2.5 m out, lifted to arrive at the hit level's height.
            FPVector3 aim = TowerPoint(frame, ref filter, trial.HitLevel) + FPVector3.Forward * trial.HitSide;
            FP flight = (FP)25 / 10 / trial.HitSpeed;
            FP gravity = -frame.SimulationConfig.Physics.Gravity.Y;
            EntityRef hitter = Spawn(frame, ref filter, trial, trial.Base + 1, aim + FPVector3.Left * ((FP)25 / 10), FPQuaternion.Identity);
            frame.Unsafe.GetPointer<Block>(hitter)->Owner = PlayerRef.None;
            frame.Unsafe.GetPointer<PhysicsBody3D>(hitter)->Velocity = new FPVector3(trial.HitSpeed, gravity * flight * FP._0_50, FP._0);
            SetStep(bench, Hit);
        }

        private static void UpdateHit(Frame frame, ref Filter filter, BlockBenchTrial trial)
        {
            if (filter.Bench->StepTime >= HitSeconds)
            {
                Finish(frame, ref filter, trial);
            }
        }

        private static void Finish(Frame frame, ref Filter filter, BlockBenchTrial trial)
        {
            BlockBench* bench = filter.Bench;
            int counted = 0;
            for (int i = 0; i <= trial.Base; i++)
            {
                if (frame.TryGet(bench->Blocks[i], out Block block) && block.Stack == StackState.Counted)
                {
                    counted++;
                }
            }

            frame.Events.BlockBenchTrialEnded(bench->Trial, bench->Pushed, bench->BaseSlide, bench->TopSlide,
                MaxSlide(frame, bench, 0, trial.Base), MaxSlide(frame, bench, trial.Base, trial.Base + 1), bench->MaxTilt,
                bench->TopSpeed, CountFallen(frame, bench, trial), bench->FallenBeforeHit, counted);

            bench->Trial++;
            if (bench->Trial < Trials.Count)
            {
                BeginTrial(frame, ref filter);
            }
            else
            {
                SetStep(bench, Idle);
            }
        }

        private static void Measure(Frame frame, BlockBench* bench, BlockBenchTrial trial)
        {
            bench->BaseSlide = FPMath.Max(bench->BaseSlide, MaxSlide(frame, bench, 0, trial.Base));
            bench->TopSlide = FPMath.Max(bench->TopSlide, MaxSlide(frame, bench, trial.Base, trial.Base + 1));
            for (int i = 0; i <= trial.Base; i++)
            {
                bench->MaxTilt = FPMath.Max(bench->MaxTilt, Tilt(frame.Get<Transform3D>(bench->Blocks[i]).Rotation));
            }

            bench->TopSpeed = FPMath.Max(bench->TopSpeed, frame.Get<PhysicsBody3D>(bench->Blocks[trial.Base]).Velocity.Magnitude);
        }

        /// <summary>
        /// The largest horizontal distance any of blocks [from, to) has moved from its recorded start.
        /// </summary>
        private static FP MaxSlide(Frame frame, BlockBench* bench, int from, int to)
        {
            FP max = FP._0;
            for (int i = from; i < to; i++)
            {
                if (frame.Exists(bench->Blocks[i]) == false)
                    continue;

                FPVector3 moved = frame.Get<Transform3D>(bench->Blocks[i]).Position - bench->Start[i];
                moved.Y = FP._0;
                max = FPMath.Max(max, moved.Magnitude);
            }

            return max;
        }

        /// <summary>
        /// Tower blocks (the base and the top block) that have slid off or dropped from their level.
        /// </summary>
        private static int CountFallen(Frame frame, BlockBench* bench, BlockBenchTrial trial)
        {
            int fallen = 0;
            FP size = BlockSize(frame, bench->Blocks[0]);
            for (int i = 0; i <= trial.Base; i++)
            {
                FPVector3 position = frame.Get<Transform3D>(bench->Blocks[i]).Position;
                FPVector3 moved = position - bench->Start[i];
                moved.Y = FP._0;
                FP expectedY = bench->GroundY + size * FP._0_50 + size * i;
                if (moved.Magnitude > FallenSlide || FPMath.Abs(position.Y - expectedY) > FallenDrop)
                {
                    fallen++;
                }
            }

            return fallen;
        }

        private static EntityRef Spawn(Frame frame, ref Filter filter, BlockBenchTrial trial, int index, FPVector3 position, FPQuaternion rotation)
        {
            BlockBench* bench = filter.Bench;
            EntityRef block = frame.Create(bench->Block);
            Transform3D* transform = frame.Unsafe.GetPointer<Transform3D>(block);
            transform->Position = position;
            transform->Rotation = rotation;
            frame.Unsafe.GetPointer<Block>(block)->Owner = (PlayerRef)0;
            ApplyBodyOverrides(frame, block, trial);
            bench->Blocks[index] = block;
            bench->Start[index] = position;

            if (trial.Sauce != SauceType.None && frame.TryGetSingleton(out Match match))
            {
                SauceConfig sauce = frame.FindAsset(frame.FindAsset(match.Mode).Sauce);
                if (sauce != null)
                {
                    SauceSystem.ApplyDip(frame, sauce, block, trial.Sauce, trial.SauceFraction);
                }
            }

            return block;
        }

        private static void ApplyBodyOverrides(Frame frame, EntityRef block, BlockBenchTrial trial)
        {
            PhysicsBody3D* body = frame.Unsafe.GetPointer<PhysicsBody3D>(block);
            if (trial.NoSleep)
            {
                body->AllowSleeping = false;
            }

            if (trial.AngularDrag > FP._0)
            {
                body->AngularDrag = trial.AngularDrag;
            }
        }

        /// <summary>
        /// Where a block at <paramref name="level"/> rests in a square tower on the bench.
        /// </summary>
        private static FPVector3 TowerPoint(Frame frame, ref Filter filter, int level)
        {
            FP size = GetModeBlockSize(frame);
            FPVector3 origin = filter.Transform->Position;
            return new FPVector3(origin.X, filter.Bench->GroundY + size * FP._0_50 + size * level, origin.Z);
        }

        private static FP GetModeBlockSize(Frame frame)
        {
            if (frame.TryGetSingleton(out Match match))
            {
                GameModeConfig mode = frame.FindAsset(match.Mode);
                if (mode != null)
                    return mode.BlockSize;
            }

            return (FP)8 / 10;
        }

        private static FP BlockSize(Frame frame, EntityRef block)
        {
            return frame.Get<PhysicsCollider3D>(block).Shape.Box.Extents.Y * 2;
        }

        private static FPVector3 Offset3(FPVector2 offset) => new FPVector3(offset.X, FP._0, offset.Y);

        private static FPQuaternion GetYaw(FPQuaternion rotation)
        {
            FPVector3 forward = rotation * FPVector3.Forward;
            return FPQuaternion.AngleAxis(FPMath.Atan2(forward.X, forward.Z) * FP.Rad2Deg, FPVector3.Up);
        }

        /// <summary>
        /// Degrees from the block's nearest-up face to straight up.
        /// </summary>
        private static FP Tilt(FPQuaternion rotation)
        {
            FP x = FPMath.Abs((rotation * FPVector3.Right).Y);
            FP y = FPMath.Abs((rotation * FPVector3.Up).Y);
            FP z = FPMath.Abs((rotation * FPVector3.Forward).Y);
            FP up = FPMath.Clamp(FPMath.Max(x, FPMath.Max(y, z)), FP._0, FP._1);
            return FPMath.Acos(up) * FP.Rad2Deg;
        }

        private static void KeepPlaying(Frame frame)
        {
            if (frame.Unsafe.TryGetPointerSingleton(out Match* match) && match->State != MatchState.Playing)
            {
                match->State = MatchState.Playing;
            }
        }

        private static void SetStep(BlockBench* bench, int step)
        {
            bench->Step = step;
            bench->StepTime = FP._0;
        }
    }
}
