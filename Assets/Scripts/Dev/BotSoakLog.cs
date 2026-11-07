using System.Collections.Generic;
using System.Text;
using Quantum;
using UnityEngine;

namespace BlockSauce
{
    /// <summary>
    /// Dev builds and the editor only (docs/PLAN.md, Bot Enhancement): counts what each player does in a match from the
    /// simulation's events and, as the round ends, writes one line to the log, so all-bot soaks can be read back:
    /// <c>[BotSoak] match=3 seconds=92 winner=4 | slot=0 bot=1 personality=Wrecker skill=Hard counted=2 ...</c>.
    /// Per player: counted blocks at the end and where the best tower stands, rival blocks fetched, placings landed and tried, throws, slide tackles (and hits), staggers and
    /// knockdowns caused and taken, slides, and a Klutz's mistakes by kind. View only: it never touches the simulation.
    /// </summary>
    public class BotSoakLog : MonoBehaviour
    {
        private class Stats
        {
            public int Placed, Places, Throws, Fetches, Raids, Tackles, Hits, StaggersCaused, StaggersTaken, DownsCaused, DownsTaken, Slides;
            public readonly int[] Mistakes = new int[5];
        }

        private readonly Dictionary<int, Stats> _stats = new Dictionary<int, Stats>();
        // The last hit each character took, so a knockdown that follows can be put down to whoever caused it.
        private readonly Dictionary<EntityRef, EntityRef> _lastHitBy = new Dictionary<EntityRef, EntityRef>();
        private int _playStartTick;
        private int _matches;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var host = new GameObject("BotSoakLog") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(host);
            host.AddComponent<BotSoakLog>();
        }
#endif

        private void OnEnable()
        {
            QuantumEvent.Subscribe<EventMatchStateChanged>(this, OnMatchState);
            QuantumEvent.Subscribe<EventBotTaskEnded>(this, OnTaskEnded);
            QuantumEvent.Subscribe<EventBotTackleEnded>(this, e => { Stats s = For(e.Game, e.Bot); if (s != null) { s.Tackles++; s.Hits += e.Hit ? 1 : 0; } });
            QuantumEvent.Subscribe<EventBotMistake>(this, e => { Stats s = For(e.Game, e.Bot); if (s != null) s.Mistakes[(int)e.Type]++; });
            QuantumEvent.Subscribe<EventSlideStarted>(this, e => { Stats s = For(e.Game, e.Entity); if (s != null) s.Slides++; });
            QuantumEvent.Subscribe<EventKnockedBack>(this, OnKnockedBack);
            QuantumEvent.Subscribe<EventRagdollChanged>(this, OnRagdoll);
        }

        private void OnDisable()
        {
            QuantumEvent.UnsubscribeListener(this);
        }

        private void OnMatchState(EventMatchStateChanged e)
        {
            if (e.State == MatchState.Playing)
            {
                _stats.Clear();
                _lastHitBy.Clear();
                _playStartTick = e.Tick;
            }
            else if (e.State == MatchState.RoundEnd)
            {
                Write(e.Game, e.Winner, e.Tick);
            }
        }

        private void OnTaskEnded(EventBotTaskEnded e)
        {
            Stats s = For(e.Game, e.Bot);
            if (s == null)
                return;

            bool done = e.Status == BotTaskStatus.Done;
            switch (e.Task)
            {
                case BotTaskType.PlaceOn:
                    s.Places++;
                    s.Placed += done ? 1 : 0;
                    break;
                case BotTaskType.ThrowAt:
                    s.Throws += done ? 1 : 0;
                    break;
                case BotTaskType.Fetch:
                    s.Fetches += done ? 1 : 0;
                    Frame frame = e.Game.Frames.Predicted;
                    if (done && frame.TryGet(e.Target, out Block block) && frame.TryGet(e.Bot, out Player player)
                        && block.Owner != player.Ref)
                        s.Raids++;
                    break;
            }
        }

        private void OnKnockedBack(EventKnockedBack e)
        {
            if (e.Staggered == false)
                return;

            _lastHitBy[e.Entity] = e.Source;
            Stats target = For(e.Game, e.Entity);
            Stats source = For(e.Game, e.Source);
            if (target != null) target.StaggersTaken++;
            if (source != null) source.StaggersCaused++;
        }

        private void OnRagdoll(EventRagdollChanged e)
        {
            if (e.Active == false)
                return;

            Stats target = For(e.Game, e.Entity);
            if (target != null) target.DownsTaken++;
            if (_lastHitBy.TryGetValue(e.Entity, out EntityRef source))
            {
                Stats by = For(e.Game, source);
                if (by != null) by.DownsCaused++;
                _lastHitBy.Remove(e.Entity);
            }
        }

        /// <summary>
        /// The stats of the player whose character <paramref name="entity"/> is, or null (not a character, or no match).
        /// </summary>
        private Stats For(QuantumGame game, EntityRef entity)
        {
            Frame frame = game?.Frames.Predicted;
            if (frame == null || entity == EntityRef.None || frame.Exists(entity) == false
                || frame.TryGet(entity, out Player player) == false)
                return null;

            int slot = (int)player.Ref;
            if (_stats.TryGetValue(slot, out Stats stats) == false)
            {
                stats = new Stats();
                _stats[slot] = stats;
            }

            return stats;
        }

        private void Write(QuantumGame game, PlayerRef winner, int tick)
        {
            Frame frame = game?.Frames.Verified;
            if (frame == null)
                return;

            _matches++;
            var line = new StringBuilder();
            // Simulation seconds, so sped-up soaks read the same.
            float seconds = (tick - _playStartTick) / (float)frame.SessionConfig.UpdateFPS;
            bool suddenDeath = frame.TryGetSingleton(out Match match) && match.SuddenDeath;
            line.Append($"[BotSoak] match={_matches} seconds={seconds:F0} winner={(winner.IsValid ? (int)winner : -1)} suddenDeath={(suddenDeath ? 1 : 0)}");
            for (int slot = 0; slot < frame.MaxPlayerCount; slot++)
            {
                if (PlayerSystem.TryGetCharacter(frame, slot, out EntityRef character) == false)
                    continue;

                bool isBot = frame.TryGet(character, out Bot bot);
                _stats.TryGetValue(slot, out Stats s);
                s ??= new Stats();
                line.Append($" | slot={slot} bot={(isBot ? 1 : 0)} personality={(isBot ? bot.Personality.ToString() : "-")}");
                line.Append($" skill={(isBot ? bot.Skill.ToString() : "-")} counted={BlockTower.CountBest(frame, slot)}");
                // Where the player's best tower stands (its base block), to see which parts of a map towers win on.
                EntityRef towerBase = BlockTower.GetBestBase(frame, slot);
                if (towerBase != EntityRef.None)
                {
                    Photon.Deterministic.FPVector3 at = frame.Get<Transform3D>(towerBase).Position;
                    line.Append($" tower={at.X.AsFloat:F1},{at.Y.AsFloat:F1},{at.Z.AsFloat:F1}");
                }
                line.Append($" fetched={s.Fetches} raids={s.Raids} placed={s.Placed} places={s.Places} throws={s.Throws} tackles={s.Tackles} hits={s.Hits}");
                line.Append($" staggersCaused={s.StaggersCaused} staggersTaken={s.StaggersTaken} downsCaused={s.DownsCaused}");
                line.Append($" downsTaken={s.DownsTaken} slides={s.Slides} trips={s.Mistakes[0]} wildSlides={s.Mistakes[1]}");
                line.Append($" drops={s.Mistakes[2]} offCenter={s.Mistakes[3]} wildThrows={s.Mistakes[4]}");
            }

            Debug.Log(line.ToString());
        }
    }
}
