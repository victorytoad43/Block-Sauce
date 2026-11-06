using Photon.Deterministic;
using Quantum.Physics3D;

namespace Quantum
{
    /// <summary>
    /// What a bot's hands and feet do this tick, in a player's terms; <see cref="BotSystem"/> turns it into Input.
    /// </summary>
    public struct BotIntent
    {
        /// <summary>The yaw, in degrees, the bot walks toward (it is also the camera's yaw, which movement follows).</summary>
        public FP MoveYaw;
        /// <summary>0 stands still, 1 is a full stick.</summary>
        public FP MoveAmount;
        public bool Run;
        /// <summary>Swing input, in degrees this tick: X turns the held block (or the body while aiming), Y raises it.</summary>
        public FPVector2 Swing;
        public bool Grab;
        public bool Throw;
        public bool Jump;
        /// <summary>Pressed while running fast, it starts a slide; released, the slide ends standing.</summary>
        public bool Crouch;
        /// <summary>Falls flat this tick (a Klutz's trip; <see cref="BotSystem"/> knocks it down).</summary>
        public bool Trip;
    }

    /// <summary>
    /// Carries out a bot's task (docs/PLAN.md, Phase 6): walking along a path on the navmesh (<see cref="BotNavigation"/>)
    /// with simple obstacle avoidance, and the hand work of
    /// fetching, placing on a tower, throwing at a target, dunking in a vat and setting a block down, through the same input a player gives.
    /// Each task reports Done or Failed on the bot; the brain chooses the next one.
    /// </summary>
    public static unsafe class BotActions
    {
        private struct Context
        {
            public Frame Frame;
            public EntityRef Entity;
            public Bot* Bot;
            public Transform3D* Transform;
            public Grabber Grabber;
            public GrabConfig Grab;
            public BotConfig Config;
            public BotSkill Skill;
        }

        /// <summary>
        /// Works out this tick's intent for a bot in control of its body: none while it hesitates before a task.
        /// </summary>
        public static BotIntent Tick(Frame frame, EntityRef entity, Bot* bot, BotConfig config)
        {
            Grabber grabber = frame.Get<Grabber>(entity);
            Context context = new Context
            {
                Frame = frame,
                Entity = entity,
                Bot = bot,
                Transform = frame.Unsafe.GetPointer<Transform3D>(entity),
                Grabber = grabber,
                Grab = frame.FindAsset(grabber.Config),
                Config = config,
                Skill = config.GetSkill(frame, bot),
            };

            BotIntent intent = new BotIntent { MoveYaw = bot->Heading };
            if (bot->Status != BotTaskStatus.Running || bot->Task == BotTaskType.None || bot->ThinkTimer.IsRunning(frame))
            {
                if (grabber.Held != EntityRef.None)
                {
                    AimHold(ref context, ref intent, GetBodyYaw(context.Transform), config.CarryPitch);
                }

                return intent;
            }

            if (bot->TaskTimer.IsRunning(frame) == false)
            {
                bot->Status = BotTaskStatus.Failed;
                RememberUnreachable(frame, bot, config);
                return intent;
            }

            switch (bot->Task)
            {
                case BotTaskType.GoTo:
                    if (Steer(ref context, ref intent, bot->Point, FP._0_50))
                    {
                        bot->Status = BotTaskStatus.Done;
                    }
                    break;
                case BotTaskType.Fetch:
                    Fetch(ref context, ref intent);
                    RememberUnreachable(frame, bot, config);
                    break;
                case BotTaskType.PlaceOn:
                    PlaceOn(ref context, ref intent);
                    break;
                case BotTaskType.ThrowAt:
                    ThrowAt(ref context, ref intent);
                    break;
                case BotTaskType.Dunk:
                    Dunk(ref context, ref intent);
                    break;
                case BotTaskType.SlideTackle:
                    SlideTackle(ref context, ref intent);
                    break;
                case BotTaskType.Drop:
                    if (context.Grabber.Held == EntityRef.None)
                    {
                        bot->Status = BotTaskStatus.Done;
                    }
                    else if (bot->Point != default && Steer(ref context, ref intent, bot->Point, FP._0_50) == false)
                    {
                        AimHold(ref context, ref intent, intent.MoveYaw, config.CarryPitch);
                    }
                    else if (bot->StepTimer.IsRunning(frame) == false)
                    {
                        intent.Grab = true;
                        bot->StepTimer = FrameTimer.FromSeconds(frame, FP._0_25);
                    }
                    break;
            }

            if (intent.MoveAmount > FP._0)
            {
                bot->Heading = intent.MoveYaw;
            }

            return intent;
        }

        /// <summary>
        /// Walks to the target block, turns to it and presses grab until something is in hand.
        /// </summary>
        private static void Fetch(ref Context context, ref BotIntent intent)
        {
            Frame frame = context.Frame;
            Bot* bot = context.Bot;
            if (context.Grabber.Held != EntityRef.None)
            {
                bot->Target = context.Grabber.Held;
                bot->Status = BotTaskStatus.Done;
                return;
            }

            if (frame.Exists(bot->Target) == false || IsHeldByOther(frame, bot->Target, context.Entity)
                || bot->Attempts >= context.Skill.GrabAttempts)
            {
                bot->Status = BotTaskStatus.Failed;
                return;
            }

            FPVector3 target = frame.Get<Transform3D>(bot->Target).Position;
            if (FlatDistance(context.Transform->Position, target) > context.Config.GrabDistance)
            {
                Steer(ref context, ref intent, target, FP._0);
                return;
            }

            // Close: creep toward the block so the body turns to face it, and press grab once it is ahead. A grab takes
            // the nearest block in front (at chest height a tower's top is often nearer than a block on the ground), so
            // if something else would come away, first step round to the target's far side from it.
            FP yaw = YawTo(context.Transform->Position, target);
            bool facing = FPMath.Abs(AngleDelta(GetBodyYaw(context.Transform), yaw)) < 25;
            EntityRef wouldGrab = facing ? GrabSystem.PeekGrabTarget(frame, context.Entity, context.Grab) : EntityRef.None;
            bool grounded = frame.TryGet(context.Entity, out GroundedTracker tracker) && tracker.IsGrounded;

            // A block resting on a tower (a rival's block on the bot's top, or its own block lying off center there): the
            // block under it is always nearer from the ground, so jump and grab near the top of the jump, once the target
            // is what a grab would take.
            if (IsOnTopOfAnother(frame, bot->Target, target))
            {
                intent.MoveYaw = yaw;
                intent.MoveAmount = FlatDistance(context.Transform->Position, target) > FP._1 ? FP._0_20 : FP._0;
                if (facing == false)
                    return;

                if (grounded)
                {
                    intent.Jump = bot->StepTimer.IsRunning(frame) == false;
                    if (intent.Jump)
                    {
                        bot->Attempts++;
                        bot->StepTimer = FrameTimer.FromSeconds(frame, FP._0_50);
                    }
                }
                else if (wouldGrab == bot->Target)
                {
                    intent.Grab = true;
                }

                return;
            }

            if (wouldGrab != EntityRef.None && wouldGrab != bot->Target)
            {
                FPVector3 away = target - frame.Get<Transform3D>(wouldGrab).Position;
                away.Y = FP._0;
                if (away.SqrMagnitude < FP._0_01)
                {
                    away = target - context.Transform->Position;
                    away.Y = FP._0;
                }

                FPVector3 stand = target + away.Normalized * (context.Config.GrabDistance - FP._0_20);
                if (FlatDistance(context.Transform->Position, stand) > FP._0_20)
                {
                    intent.MoveYaw = YawTo(context.Transform->Position, stand);
                    intent.MoveAmount = FP._0_50;
                    return;
                }
            }

            intent.MoveYaw = yaw;
            intent.MoveAmount = FP._0_20;
            if (facing && bot->StepTimer.IsRunning(frame) == false)
            {
                intent.Grab = true;
                bot->Attempts++;
                bot->StepTimer = FrameTimer.FromSeconds(frame, FP._0_25);
            }
        }

        /// <summary>
        /// Whether <paramref name="block"/> (at <paramref name="position"/>) rests on another block, centered within a
        /// block of it: grabbing it from the ground would take the block underneath instead.
        /// </summary>
        private static bool IsOnTopOfAnother(Frame frame, EntityRef block, FPVector3 position)
        {
            // From just under the block, so the ray can't hit the block itself.
            FPVector3 below = position - FPVector3.Up * (GetHalfHeight(frame, block) + FP._0_05);
            Hit3D? hit = frame.Physics3D.Raycast(below, FPVector3.Down, FP._0_50, -1, QueryOptions.HitDynamics);
            return hit.HasValue && hit.Value.Entity != block && frame.Has<Block>(hit.Value.Entity);
        }

        /// <summary>
        /// Carries the held block to the target block and lets go once it is over the top (see BotConfig's placing
        /// settings). A low tower: walk up to it with the block just clearing the top, stopping before shoving it. A tall
        /// one, which a held block can't clear from the ground: line up close with the block raised high (clear of the
        /// tower), jump straight up, and once the block is above the top lower it to reach out over the tower, retrying
        /// a few times. Then checks the block landed on the target and the target stayed put.
        /// </summary>
        private static void PlaceOn(ref Context context, ref BotIntent intent)
        {
            Frame frame = context.Frame;
            Bot* bot = context.Bot;
            BotConfig config = context.Config;

            if (bot->Step == 3)
            {
                if (bot->StepTimer.IsRunning(frame))
                    return;

                bool landed = frame.Exists(bot->Carried) && frame.Exists(bot->Target)
                    && IsOnTop(frame, bot->Carried, bot->Target)
                    && FlatDistance(frame.Get<Transform3D>(bot->Target).Position, bot->Point) < FP._0_20;
                bot->Status = landed ? BotTaskStatus.Done : BotTaskStatus.Failed;
                return;
            }

            if (context.Grabber.Held == EntityRef.None || frame.Exists(bot->Target) == false
                || bot->Attempts >= context.Skill.PlaceAttempts)
            {
                bot->Status = BotTaskStatus.Failed;
                return;
            }

            FPVector3 position = context.Transform->Position;
            FPVector3 top = frame.Get<Transform3D>(bot->Target).Position;
            FP topSurface = top.Y + GetHalfHeight(frame, bot->Target);
            // Where the bot means to let go: the top's center, off by this placing's error.
            FPVector3 aimPoint = top + new FPVector3(bot->PlaceError.X, FP._0, bot->PlaceError.Y);
            FP yaw = YawTo(position, aimPoint);
            FP distance = FlatDistance(position, top);
            bool tall = topSurface > config.PlaceJumpAbove;
            Transform3D held = frame.Get<Transform3D>(context.Grabber.Held);
            FP offset = FlatDistance(held.Position, aimPoint);
            FP heldBottom = held.Position.Y - GetHalfHeight(frame, context.Grabber.Held);
            bool clear = heldBottom > topSurface + FP._0_05;

            // A low tower: the lowest pitch that clears its top, reaching furthest out. A tall one: raised high and close
            // to the body (clear of the tower) until the jump lifts it above the top, then lowered to reach out over it.
            FP pitch = tall == false ? GetClearingPitch(frame, context, topSurface)
                : bot->Step == 2 && clear ? config.PlaceJumpReachPitch : context.Grab.SwingPitchMax;
            AimHold(ref context, ref intent, yaw, pitch);

            if (bot->Step == 0)
            {
                // Remember where the target was, to check it wasn't knocked away.
                bot->Point = top;
                FP arrive = tall ? config.PlaceLineUpDistance + FP._0_50 : config.PlaceStandDistance + FP._0_50;
                if (Steer(ref context, ref intent, top, arrive))
                {
                    bot->Step = 1;
                }

                return;
            }

            intent.MoveYaw = yaw;
            if (bot->Step == 1)
            {
                if (tall == false)
                {
                    // Walk up to the tower, stopping before the body would shove it; back off if too close to reach
                    // over it (a bot that ends up standing on its target block can't place on it).
                    intent.MoveAmount = distance > config.PlaceStandDistance ? FP._0_33 : FP._0;
                    if (distance < config.PlaceStandDistance - FP._0_33)
                    {
                        intent.MoveYaw = yaw + 180;
                        intent.MoveAmount = FP._0_25;
                    }
                }
                else if (distance > config.PlaceLineUpDistance + FP._0_05)
                {
                    intent.MoveAmount = FP._0_25;
                }
                else if (distance < config.PlaceLineUpDistance - FP._0_05)
                {
                    intent.MoveYaw = yaw + 180;
                    intent.MoveAmount = FP._0_25;
                }
                else if (context.Grabber.SwingPitch > context.Grab.SwingPitchMax - 5
                    && frame.TryGet(context.Entity, out GroundedTracker tracker) && tracker.IsGrounded)
                {
                    intent.Jump = true;
                    bot->Attempts++;
                    NextStep(frame, bot, FP._1 + FP._0_20);
                }
            }
            else if (bot->Step == 2)
            {
                // In the air: once the block clears the top, edge in to the stand distance and no closer, pushing back
                // if the body drifts in; back to lining up if it lands without letting go.
                if (distance < config.PlaceJumpStandDistance)
                {
                    intent.MoveYaw = yaw + 180;
                    intent.MoveAmount = FP._0_50;
                }
                else
                {
                    intent.MoveAmount = clear ? config.PlaceJumpMove : FP._0;
                }

                if (bot->StepTimer.IsRunning(frame) == false)
                {
                    bot->Step = 1;
                }
            }

            if (offset < (tall ? config.PlaceJumpOffset : config.PlaceOffset) && heldBottom > topSurface - FP._0_10)
            {
                intent.Grab = true;
                bot->Carried = context.Grabber.Held;
                bot->Step = 3;
                bot->StepTimer = FrameTimer.FromSeconds(frame, FP._1);
            }
        }

        /// <summary>
        /// Runs at the target character, aiming where it will be when the bot gets there, and once the run lines up
        /// from TackleSlideDistance out, slides into it (a slide is over the body-check speed, so the hit staggers the
        /// target, which drops what it holds). Done when the slide hits; Failed when it misses or the target stops
        /// being worth tackling before the slide.
        /// </summary>
        private static void SlideTackle(ref Context context, ref BotIntent intent)
        {
            Frame frame = context.Frame;
            Bot* bot = context.Bot;
            BotConfig config = context.Config;
            EntityRef target = bot->Target;

            if (frame.Exists(target) == false)
            {
                EndTackle(ref context, false);
                return;
            }

            if (bot->Step == 1)
            {
                // Sliding, with crouch released so the slide ends standing.
                if (IsHit(frame, target))
                {
                    EndTackle(ref context, true);
                }
                else if (bot->StepTimer.IsRunning(frame) == false)
                {
                    EndTackle(ref context, false);
                }

                return;
            }

            // Running into the target before the slide is a body check: as good as a slide if it staggers it.
            if (IsHit(frame, target) && FlatDistance(context.Transform->Position, frame.Get<Transform3D>(target).Position) < 2)
            {
                bot->Carried = frame.Get<Grabber>(target).Held;
                EndTackle(ref context, true);
                return;
            }

            if (context.Grabber.Held != EntityRef.None || BotChoices.IsTackleTarget(frame, context.Entity, bot, target, config) == false)
            {
                EndTackle(ref context, false);
                return;
            }

            FPVector3 position = context.Transform->Position;
            FPVector3 targetPosition = frame.Get<Transform3D>(target).Position;
            FPVector3 targetVelocity = frame.TryGet(target, out PhysicsBody3D targetBody) ? targetBody.Velocity : FPVector3.Zero;
            targetVelocity.Y = FP._0;
            // Lead the target by about the time the run and slide take to get there.
            FP lead = FPMath.Min(FlatDistance(position, targetPosition) / 7, FP._1) * context.Skill.TackleLead;
            FPVector3 aim = targetPosition + targetVelocity * lead;
            FP distance = FlatDistance(position, aim);

            Steer(ref context, ref intent, aim, FP._0);
            intent.Run = true;
            intent.Crouch = false;

            if (distance > context.Skill.TackleSlideDistance)
                return;

            // In range: slide once running fast along the line to the target, on the ground, with no wall between.
            FPVector3 velocity = frame.Get<PhysicsBody3D>(context.Entity).Velocity;
            FP speed = FPMath.Sqrt(velocity.X * velocity.X + velocity.Z * velocity.Z);
            FP runYaw = FPMath.Atan2(velocity.X, velocity.Z) * FP.Rad2Deg;
            FP aimYaw = YawTo(position, aim);
            intent.MoveYaw = aimYaw;
            bool grounded = frame.TryGet(context.Entity, out GroundedTracker tracker) && tracker.IsGrounded;
            if (grounded == false || speed < config.TackleMinSpeed
                || FPMath.Abs(AngleDelta(runYaw, aimYaw)) > context.Skill.TackleAlignDegrees)
                return;

            FPVector3 toAim = aim - position;
            toAim.Y = FP._0;
            if (toAim.SqrMagnitude > FP._0 && frame.Physics3D.Raycast(position + FPVector3.Up * FP._1, toAim.Normalized,
                    distance, -1, QueryOptions.HitStatics).HasValue)
                return;

            intent.Crouch = true;
            intent.Jump = false;
            bot->Carried = frame.Get<Grabber>(target).Held;
            NextStep(frame, bot, config.TackleSlideSeconds);
        }

        /// <summary>
        /// Ends a slide tackle, reports how it went, and starts the wait before the next one.
        /// </summary>
        private static void EndTackle(ref Context context, bool hit)
        {
            Frame frame = context.Frame;
            Bot* bot = context.Bot;
            bot->Status = hit ? BotTaskStatus.Done : BotTaskStatus.Failed;
            bot->TackleTimer = FrameTimer.FromSeconds(frame,
                context.Config.TackleCooldownSeconds * context.Config.GetPersonality(frame, bot).TackleCooldownScale);
            if (bot->Step == 1 || hit)
            {
                bool dropped = bot->Carried != EntityRef.None && (frame.Exists(bot->Target) == false
                    || frame.Get<Grabber>(bot->Target).Held != bot->Carried);
                frame.Events.BotTackleEnded(context.Entity, bot->Target, bot->Step == 1, hit, dropped);
            }
        }

        /// <summary>
        /// Returns whether <paramref name="target"/> has been hit: staggered or knocked down.
        /// </summary>
        private static bool IsHit(Frame frame, EntityRef target)
        {
            return frame.Has<RagdollOwner>(target)
                || (frame.TryGet(target, out MovementData movement) && movement.Tier == ControlTier.Staggered);
        }

        /// <summary>
        /// Walks into throwing range of the target, turns to it, aims along the arc that lands the block on it and
        /// throws.
        /// </summary>
        private static void ThrowAt(ref Context context, ref BotIntent intent)
        {
            Frame frame = context.Frame;
            Bot* bot = context.Bot;
            BotConfig config = context.Config;

            if (bot->Step == 2 && context.Grabber.Held == EntityRef.None)
            {
                bot->Status = BotTaskStatus.Done;
                return;
            }

            if (context.Grabber.Held == EntityRef.None || frame.Exists(bot->Target) == false)
            {
                bot->Status = BotTaskStatus.Failed;
                return;
            }

            FPVector3 target = frame.Get<Transform3D>(bot->Target).Position;
            if (frame.Has<Player>(bot->Target))
            {
                // A character: at its middle, not its feet.
                target.Y += FP._1;
            }

            FP distance = FlatDistance(context.Transform->Position, target);
            FP yaw = YawTo(context.Transform->Position, target);
            FP bodyYaw = GetBodyYaw(context.Transform);

            if (bot->Step == 0)
            {
                AimHold(ref context, ref intent, yaw, context.Config.CarryPitch);
                if (distance > config.ThrowMaxRange)
                {
                    Steer(ref context, ref intent, target, config.ThrowMaxRange - FP._1);
                }
                else if (distance < config.ThrowMinRange)
                {
                    intent.MoveYaw = yaw + 180;
                    intent.MoveAmount = FP._0_50;
                }
                else
                {
                    intent.Throw = true;
                    bot->Step = 1;
                    bot->StepTimer = FrameTimer.FromSeconds(frame, context.Skill.MinAimSeconds);
                }

                return;
            }

            // Step 1: aiming. Horizontal swing turns the body to the target; vertical swing moves the aim pitch.
            intent.Throw = true;
            // Off by this throw's error, which the bot doesn't know about.
            FP yawError = AngleDelta(bodyYaw, yaw + bot->AimError.X);
            FP pitchError = GetThrowPitch(frame, context, target) + bot->AimError.Y - context.Grabber.AimPitch;
            intent.Swing = new FPVector2(
                FPMath.Clamp(yawError, -config.MaxSwingStep, config.MaxSwingStep),
                FPMath.Clamp(pitchError / context.Grab.AimPitchSensitivity, -config.MaxSwingStep, config.MaxSwingStep));

            if (bot->StepTimer.IsRunning(frame) == false && FPMath.Abs(yawError) < context.Skill.AimTolerance
                && FPMath.Abs(pitchError) < context.Skill.AimTolerance)
            {
                intent.Throw = false;
                bot->Carried = context.Grabber.Held;
                bot->Step = 2;
            }
        }

        /// <summary>
        /// Raises the block, walks up to the vat until it is over the rim, lowers it into the sauce, soaks it, lifts it
        /// back out and backs off.
        /// </summary>
        private static void Dunk(ref Context context, ref BotIntent intent)
        {
            Frame frame = context.Frame;
            Bot* bot = context.Bot;
            BotConfig config = context.Config;

            if (context.Grabber.Held == EntityRef.None || frame.Exists(bot->Target) == false
                || frame.Has<SauceVat>(bot->Target) == false)
            {
                bot->Status = BotTaskStatus.Failed;
                return;
            }

            FPVector3 vat = frame.Get<Transform3D>(bot->Target).Position;
            FP yaw = YawTo(context.Transform->Position, vat);
            FP high = context.Grab.SwingPitchMax;
            FP low = context.Grab.SwingPitchMin;
            bool timerDone = bot->StepTimer.IsRunning(frame) == false;

            switch (bot->Step)
            {
                case 0:
                    AimHold(ref context, ref intent, yaw, context.Config.CarryPitch);
                    if (Steer(ref context, ref intent, vat, config.DunkApproachDistance))
                    {
                        NextStep(frame, bot, FP._1);
                    }
                    break;
                case 1: // raise before going near the rim
                    AimHold(ref context, ref intent, yaw, high);
                    if (timerDone)
                    {
                        NextStep(frame, bot, config.DunkApproachDistance);
                    }
                    break;
                case 2: // walk in until against the cup
                    AimHold(ref context, ref intent, yaw, high);
                    intent.MoveYaw = yaw;
                    intent.MoveAmount = FP._0_33;
                    if (timerDone || FlatDistance(context.Transform->Position, vat) < FP._1 + FP._0_50)
                    {
                        NextStep(frame, bot, config.DunkLowerSeconds);
                    }
                    break;
                case 3: // lower into the sauce, still leaning in
                    AimHold(ref context, ref intent, yaw, low);
                    intent.MoveYaw = yaw;
                    intent.MoveAmount = FP._0_20;
                    if (timerDone)
                    {
                        NextStep(frame, bot, config.DunkSoakSeconds);
                    }
                    break;
                case 4: // soak
                    AimHold(ref context, ref intent, yaw, low);
                    intent.MoveYaw = yaw;
                    intent.MoveAmount = FP._0_20;
                    if (timerDone)
                    {
                        NextStep(frame, bot, config.DunkLiftSeconds);
                    }
                    break;
                case 5: // lift out
                    AimHold(ref context, ref intent, yaw, high);
                    if (timerDone)
                    {
                        NextStep(frame, bot, FP._1);
                    }
                    break;
                default: // back off
                    AimHold(ref context, ref intent, yaw, high);
                    intent.MoveYaw = yaw + 180;
                    intent.MoveAmount = FP._0_50;
                    if (timerDone)
                    {
                        bool coated = frame.TryGet(context.Grabber.Held, out SauceCoat coat) && coat.DipCount > 0;
                        bot->Status = coated ? BotTaskStatus.Done : BotTaskStatus.Failed;
                    }
                    break;
            }
        }

        /// <summary>
        /// Walks toward <paramref name="target"/>, around anything in the way, and returns true once within
        /// <paramref name="arriveDistance"/> (horizontally).
        /// </summary>
        private static bool Steer(ref Context context, ref BotIntent intent, FPVector3 target, FP arriveDistance)
        {
            Frame frame = context.Frame;
            Bot* bot = context.Bot;
            BotConfig config = context.Config;
            FPVector3 position = context.Transform->Position;
            FP distance = FlatDistance(position, target);
            if (distance <= arriveDistance)
                return true;

            // Toward the next corner of the path on the navmesh (or straight at the target without one).
            FPVector3 next = BotNavigation.NextPoint(frame, bot, position, target, out bool linkJump);
            FP yaw = YawTo(position, next);
            if (bot->DetourTimer.IsRunning(frame))
            {
                yaw = bot->DetourYaw;
            }
            else
            {
                yaw = FindClearYaw(ref context, yaw, FPMath.Min(distance, config.ProbeLength));
                CheckStuck(ref context, yaw);
            }

            intent.MoveYaw = yaw;
            intent.MoveAmount = FP._1;
            intent.Run = (context.Grabber.Held == EntityRef.None || context.Skill.RunWhileCarrying) && distance > config.RunDistance;
            // Pressed in short pulses, so a jump that falls short is tried again.
            intent.Jump = linkJump || (frame.Number % 16 < 2 && IsLedgeAhead(ref context, yaw));
            intent.Crouch = intent.Run && intent.Jump == false && distance > config.TravelSlideMinDistance
                && TryTravelSlide(ref context, yaw, next);
            MakeMistakes(ref context, ref intent);
            return false;
        }

        /// <summary>
        /// A Klutz's mistakes while on the move (the personality's chances per second): running, it now and then trips or
        /// slides without looking; carrying, it now and then lets go of the block.
        /// </summary>
        private static void MakeMistakes(ref Context context, ref BotIntent intent)
        {
            Frame frame = context.Frame;
            BotPersonality personality = context.Config.GetPersonality(frame, context.Bot);
            FP dt = frame.DeltaTime;
            bool grounded = frame.TryGet(context.Entity, out GroundedTracker tracker) && tracker.IsGrounded;
            if (intent.Run && grounded && intent.Jump == false)
            {
                if (personality.TripChance > FP._0 && frame.RNG->Next() < personality.TripChance * dt)
                {
                    intent.Trip = true;
                    frame.Events.BotMistake(context.Entity, BotMistakeType.Trip);
                    return;
                }

                if (intent.Crouch == false && personality.WildSlideChance > FP._0
                    && frame.RNG->Next() < personality.WildSlideChance * dt)
                {
                    intent.Crouch = true;
                    frame.Events.BotMistake(context.Entity, BotMistakeType.Slide);
                }
            }

            if (context.Grabber.Held != EntityRef.None && intent.Grab == false && personality.DropChance > FP._0
                && frame.RNG->Next() < personality.DropChance * dt)
            {
                intent.Grab = true;
                frame.Events.BotMistake(context.Entity, BotMistakeType.Drop);
            }
        }

        /// <summary>
        /// Now and then, while running along a long clear stretch, slides to cover ground (TravelSlideChance a second).
        /// Returns true on the tick to press crouch.
        /// </summary>
        private static bool TryTravelSlide(ref Context context, FP yaw, FPVector3 next)
        {
            Frame frame = context.Frame;
            Bot* bot = context.Bot;
            BotConfig config = context.Config;
            if (bot->Task == BotTaskType.SlideTackle || bot->TravelSlideTimer.IsRunning(frame)
                || frame.RNG->Next() >= config.TravelSlideChance * frame.DeltaTime)
                return false;

            FPVector3 position = context.Transform->Position;
            FPVector3 velocity = frame.Get<PhysicsBody3D>(context.Entity).Velocity;
            FP speed = FPMath.Sqrt(velocity.X * velocity.X + velocity.Z * velocity.Z);
            if (speed < config.TackleMinSpeed || FlatDistance(position, next) < config.TravelSlideClearance
                || FPMath.Abs(AngleDelta(FPMath.Atan2(velocity.X, velocity.Z) * FP.Rad2Deg, yaw)) > 10
                || frame.TryGet(context.Entity, out GroundedTracker tracker) == false || tracker.IsGrounded == false)
                return false;

            FPVector3 direction = FPQuaternion.AngleAxis(yaw, FPVector3.Up) * FPVector3.Forward;
            if (IsClear(ref context, position + FPVector3.Up * FP._0_50, direction, config.TravelSlideClearance) == false
                || IsClear(ref context, position + FPVector3.Up * (FP._1 + FP._0_20), direction, config.TravelSlideClearance) == false)
                return false;

            bot->TravelSlideTimer = FrameTimer.FromSeconds(frame, config.TravelSlideCooldownSeconds);
            return true;
        }

        /// <summary>
        /// Returns whether a ledge too high to step onto (a pillar's edge, the inner ring) is just ahead along
        /// <paramref name="yaw"/>, low enough to jump onto: the knee probe hits a steep static face and the chest probe
        /// is clear.
        /// </summary>
        private static bool IsLedgeAhead(ref Context context, FP yaw)
        {
            FPVector3 position = context.Transform->Position;
            FPVector3 direction = FPQuaternion.AngleAxis(yaw, FPVector3.Up) * FPVector3.Forward;
            Hit3D? knee = context.Frame.Physics3D.Raycast(position + FPVector3.Up * FP._0_50, direction,
                context.Config.LedgeProbeLength, -1, QueryOptions.HitStatics | QueryOptions.ComputeDetailedInfo);
            return knee.HasValue && knee.Value.Normal.Y < FP._0_50
                && IsClear(ref context, position + FPVector3.Up * (FP._1 + FP._0_20), direction, context.Config.LedgeProbeLength);
        }

        /// <summary>
        /// Returns the yaw nearest <paramref name="desired"/>, trying ever wider angles to each side, along which nothing
        /// solid is within <paramref name="length"/> at knee and chest height. Falls back to the desired yaw.
        /// </summary>
        private static FP FindClearYaw(ref Context context, FP desired, FP length)
        {
            FPVector3 position = context.Transform->Position;
            for (int i = 0; i < 7; i++)
            {
                int side = i % 2 == 1 ? 1 : -1;
                FP yaw = desired + side * ((i + 1) / 2) * context.Config.ProbeStepDegrees;
                FPVector3 direction = FPQuaternion.AngleAxis(yaw, FPVector3.Up) * FPVector3.Forward;
                if (IsClear(ref context, position + FPVector3.Up * FP._0_50, direction, length, false)
                    && IsClear(ref context, position + FPVector3.Up * (FP._1 + FP._0_20), direction, length))
                    return yaw;
            }

            return desired;
        }

        /// <summary>
        /// Returns whether nothing solid is within <paramref name="length"/> along a probe; with
        /// <paramref name="statics"/> false, static geometry is ignored (at knee height a static ledge is climbed, not
        /// walked round; the chest probe still sees walls).
        /// </summary>
        private static bool IsClear(ref Context context, FPVector3 origin, FPVector3 direction, FP length, bool statics = true)
        {
            QueryOptions options = QueryOptions.HitDynamics | QueryOptions.HitKinematics | QueryOptions.ComputeDetailedInfo;
            if (statics)
            {
                options |= QueryOptions.HitStatics;
            }

            HitCollection3D hits = context.Frame.Physics3D.RaycastAll(origin, direction, length, -1, options);
            for (int i = 0; i < hits.Count; i++)
            {
                EntityRef hit = hits[i].Entity;
                if (hits[i].IsTrigger || hit == context.Entity || hit == context.Grabber.Held || hit == context.Bot->Target)
                    continue;

                // Ground rising ahead is walkable, not in the way.
                if (hits[i].IsStatic && hits[i].Normal.Y > FP._0_50)
                    continue;

                return false;
            }

            return true;
        }

        /// <summary>
        /// Every StuckSeconds, checks the bot has moved StuckDistance; if not, it walks off at a right angle (a random
        /// side) for DetourSeconds.
        /// </summary>
        private static void CheckStuck(ref Context context, FP yaw)
        {
            Frame frame = context.Frame;
            Bot* bot = context.Bot;
            if (bot->ProgressTimer.IsRunning(frame))
                return;

            FPVector3 position = context.Transform->Position;
            if (FlatDistance(position, bot->ProgressPosition) < context.Config.StuckDistance)
            {
                bot->DetourYaw = yaw + (frame.RNG->Next(0, 2) == 0 ? 90 : -90);
                bot->DetourTimer = FrameTimer.FromSeconds(frame, context.Config.DetourSeconds);
            }

            bot->ProgressPosition = position;
            bot->ProgressTimer = FrameTimer.FromSeconds(frame, context.Config.StuckSeconds);
        }

        /// <summary>
        /// Swings the held block toward world <paramref name="yaw"/> at <paramref name="pitch"/>, at most MaxSwingStep a
        /// tick; swinging past the grab's body-turn threshold turns the body.
        /// </summary>
        private static void AimHold(ref Context context, ref BotIntent intent, FP yaw, FP pitch)
        {
            if (context.Grabber.Held == EntityRef.None)
                return;

            FP step = context.Config.MaxSwingStep;
            FP swingYaw = AngleDelta(GetBodyYaw(context.Transform), yaw);
            intent.Swing = new FPVector2(
                FPMath.Clamp(swingYaw - context.Grabber.SwingYaw, -step, step),
                FPMath.Clamp(pitch - context.Grabber.SwingPitch, -step, step));
        }

        /// <summary>
        /// Returns the aim pitch, in degrees, whose throw lands on <paramref name="target"/>: the low arc for the throw
        /// speed, from the held block, or 45 (the longest throw) when it is out of reach.
        /// </summary>
        private static FP GetThrowPitch(Frame frame, Context context, FPVector3 target)
        {
            FPVector3 from = frame.Get<Transform3D>(context.Grabber.Held).Position;
            FP dx = FlatDistance(from, target);
            FP dy = target.Y - from.Y;
            FP v = context.Grab.ThrowSpeed;
            FP g = FPMath.Abs(frame.SimulationConfig.Physics.Gravity.Y);
            FP v2 = v * v;
            FP discriminant = v2 * v2 - g * (g * dx * dx + 2 * dy * v2);
            FP pitch = discriminant < FP._0 || dx < FP._0_10
                ? 45
                : FPMath.Atan((v2 - FPMath.Sqrt(discriminant)) / (g * dx)) * FP.Rad2Deg;
            return FPMath.Clamp(pitch, context.Grab.AimPitchMin, context.Grab.AimPitchMax);
        }

        /// <summary>
        /// Returns the lowest swing pitch, in degrees, that holds the block's bottom PlaceClearance above
        /// <paramref name="surface"/>: the lowest is also the furthest out, so the block reaches over the tower's center
        /// from where the bot stands.
        /// </summary>
        private static FP GetClearingPitch(Frame frame, Context context, FP surface)
        {
            FP halfHeight = GetHalfHeight(frame, context.Grabber.Held);
            FP rise = surface + context.Config.PlaceClearance + halfHeight
                - (context.Transform->Position.Y + context.Grab.PivotHeight);
            FP sine = FPMath.Clamp(rise / context.Grab.HoldRadius, -FP._1, FP._1);
            FP pitch = FPMath.Asin(sine) * FP.Rad2Deg;
            return FPMath.Clamp(pitch, context.Grab.SwingPitchMin, context.Grab.SwingPitchMax);
        }

        /// <summary>
        /// After a failed fetch, has the bot leave that block alone for a while, so it doesn't walk back to a block it
        /// can't reach.
        /// </summary>
        private static void RememberUnreachable(Frame frame, Bot* bot, BotConfig config)
        {
            if (bot->Task == BotTaskType.Fetch && bot->Status == BotTaskStatus.Failed && bot->Target != EntityRef.None)
            {
                bot->Unreachable = bot->Target;
                bot->UnreachableTimer = FrameTimer.FromSeconds(frame, config.UnreachableSeconds);
            }
        }

        private static void NextStep(Frame frame, Bot* bot, FP seconds)
        {
            bot->Step++;
            bot->StepTimer = FrameTimer.FromSeconds(frame, seconds);
        }

        /// <summary>
        /// Returns whether <paramref name="block"/> rests on top of <paramref name="below"/>: above it and over its center.
        /// </summary>
        private static bool IsOnTop(Frame frame, EntityRef block, EntityRef below)
        {
            FPVector3 a = frame.Get<Transform3D>(block).Position;
            FPVector3 b = frame.Get<Transform3D>(below).Position;
            return a.Y > b.Y + GetHalfHeight(frame, below) && FlatDistance(a, b) < GetHalfHeight(frame, below) + FP._0_10;
        }

        private static bool IsHeldByOther(Frame frame, EntityRef block, EntityRef self)
        {
            return frame.TryGet(block, out Grabbable grabbable) && grabbable.Holder != EntityRef.None && grabbable.Holder != self;
        }

        private static FP GetHalfHeight(Frame frame, EntityRef block)
        {
            return frame.Get<PhysicsCollider3D>(block).Shape.Box.Extents.Y;
        }

        private static FP GetBodyYaw(Transform3D* transform)
        {
            FPVector3 forward = transform->Rotation * FPVector3.Forward;
            return FPMath.Atan2(forward.X, forward.Z) * FP.Rad2Deg;
        }

        private static FP YawTo(FPVector3 from, FPVector3 to)
        {
            return FPMath.Atan2(to.X - from.X, to.Z - from.Z) * FP.Rad2Deg;
        }

        private static FP FlatDistance(FPVector3 a, FPVector3 b)
        {
            FPVector3 d = b - a;
            return FPMath.Sqrt(d.X * d.X + d.Z * d.Z);
        }

        /// <summary>
        /// Returns <paramref name="to"/> minus <paramref name="from"/>, in degrees, wrapped to -180..180.
        /// </summary>
        private static FP AngleDelta(FP from, FP to)
        {
            FP delta = to - from;
            while (delta > 180)
            {
                delta -= 360;
            }

            while (delta < -180)
            {
                delta += 360;
            }

            return delta;
        }
    }
}
